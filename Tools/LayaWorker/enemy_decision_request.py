"""실제 몬스터 데이터와 명시적인 관측 스냅샷을 Laya 요청으로 묶는다."""

import argparse
import hashlib
import json
import math
from pathlib import Path

import laya_worker as worker


PACK = worker.ROOT / "enemy-data"
GROUP_FIELDS = {"AttackMultiplier", "PunishMultiplier", "CounterMultiplier", "PressureBonus", "RetreatBonus", "KeepDistanceBonus"}
INTENT_DESCRIPTIONS = {
    "Attack": "Commit to an available attack against the current target.",
    "Punish": "Use an available opening to punish target recovery or a mistake.",
    "Counter": "Respond to observed target aggression with a counter opportunity.",
    "Pressure": "Maintain pressure through positioning instead of immediate commitment.",
    "Chase": "Close distance toward the current target.",
    "Retreat": "Disengage and create space from the current target.",
    "KeepDistance": "Maintain the preferred combat spacing.",
    "Defend": "Choose defensive behavior supported by this actor.",
    "Recover": "Take a recovery opportunity instead of immediate aggression.",
}


def read(path):
    return worker.decode_json(Path(path).read_text(encoding="utf-8-sig"))


def verify_pack(folder=PACK):
    """AI가 참고할 객체·코드 묶음의 전송 무결성을 확인한다."""
    folder = Path(folder).resolve()
    manifest = read(folder / "manifest.json")
    for entry in manifest["files"]:
        path = (folder / entry["path"]).resolve()
        if not path.is_relative_to(folder):
            raise worker.WorkerError("invalid_pack", "패키지 밖의 경로입니다.")
        raw = path.read_bytes()
        if len(raw) != entry["bytes"] or hashlib.sha256(raw).hexdigest() != entry["sha256"]:
            raise worker.WorkerError("invalid_pack", f"판단 데이터 해시 불일치: {entry['path']}")
    return manifest


def require_number(value, name, minimum=0):
    if type(value) not in (int, float) or not math.isfinite(value) or (minimum is not None and value < minimum):
        raise worker.WorkerError("invalid_snapshot", f"{name}: 유효한 수치가 필요합니다.")


def build_request(snapshot, folder=PACK):
    """미기록 상태를 추정하지 않고 실제 입력 계약과 공격 연결을 검사한다."""
    folder = Path(folder)
    contract = read(folder / "observation-contract.json")
    actors = {actor["actor_id"]: actor for actor in read(folder / "actors.json")}
    required = {"schema_version", "actor_id", "source", "request_id", "session_id", "state_seq", "context",
                "phase_index", "last_intent", "consecutive_intent_count", "group_bias", "candidate_intents", "ability_states",
                "runtime_level", "blackboard", "self_tags", "target_tags"}
    if not isinstance(snapshot, dict) or set(snapshot) != required:
        raise worker.WorkerError("invalid_snapshot", "필수 스냅샷 필드가 누락되었거나 알 수 없는 필드가 있습니다.")
    if type(snapshot["schema_version"]) is not int or snapshot["schema_version"] != 1:
        raise worker.WorkerError("invalid_snapshot", "지원하지 않는 snapshot schema입니다.")
    if not isinstance(snapshot["source"], str) or not isinstance(snapshot["actor_id"], str) or snapshot["source"] not in ("runtime_capture", "synthetic_fixture") or snapshot["actor_id"] not in actors:
        raise worker.WorkerError("invalid_snapshot", "관측 출처 또는 Actor ID가 유효하지 않습니다.")
    actor = actors[snapshot["actor_id"]]
    if type(snapshot["runtime_level"]) is not int or snapshot["runtime_level"] < 1:
        raise worker.WorkerError("invalid_snapshot", "현재 런타임 레벨을 명시해야 합니다.")
    for field in ("self_tags", "target_tags"):
        if not isinstance(snapshot[field], list) or any(not isinstance(tag, str) for tag in snapshot[field]):
            raise worker.WorkerError("invalid_snapshot", field + ": 실제 태그 문자열 목록이 필요합니다.")
    blackboard = snapshot["blackboard"]
    if not isinstance(blackboard, dict) or not {"Target.Has", "Target.Distance", "Self.StateId", "HasAttackSlot", "NextActionAllowedTime"} <= set(blackboard):
        raise worker.WorkerError("invalid_snapshot", "핵심 BT Blackboard 값이 누락되었습니다.")
    context = snapshot["context"]
    if not isinstance(context, dict) or set(context) != {field["name"] for field in contract["context_fields"]}:
        raise worker.WorkerError("invalid_snapshot", "IntentEvaluationContext의 모든 필드를 명시해야 합니다.")
    for field in contract["context_fields"]:
        value = context[field["name"]]
        if field["type"] == "Boolean":
            if type(value) is not bool:
                raise worker.WorkerError("invalid_snapshot", field["name"] + ": bool이 필요합니다.")
        elif field["enum_values"] is not None:
            if not isinstance(value, str) or value not in field["enum_values"]:
                raise worker.WorkerError("invalid_snapshot", field["name"] + ": 알 수 없는 enum입니다.")
        else:
            require_number(value, field["name"])
    for name in ("HealthPercent", "Aggression", "ReactionChance", "PunishChance", "CounterChance", "RetreatChance", "GuardChance", "PredictionConfidence"):
        if context[name] > 1:
            raise worker.WorkerError("invalid_snapshot", name + ": 0~1이어야 합니다.")
    if type(blackboard["Target.Has"]) is not bool or type(blackboard["HasAttackSlot"]) is not bool:
        raise worker.WorkerError("invalid_snapshot", "타깃·슬롯 Blackboard 값은 bool이어야 합니다.")
    require_number(blackboard["Target.Distance"], "Target.Distance")
    require_number(blackboard["NextActionAllowedTime"], "NextActionAllowedTime")
    if not isinstance(blackboard["Self.StateId"], str) or not blackboard["Self.StateId"]:
        raise worker.WorkerError("invalid_snapshot", "현재 상태 식별자가 필요합니다.")
    if blackboard["Target.Distance"] != context["Distance"]:
        raise worker.WorkerError("invalid_snapshot", "거리 입력과 Blackboard 값이 다릅니다.")
    if snapshot["last_intent"] not in contract["intent_names"] + ["None"]:
        raise worker.WorkerError("invalid_snapshot", "last_intent가 유효하지 않습니다.")
    if type(snapshot["consecutive_intent_count"]) is not int or snapshot["consecutive_intent_count"] < 0:
        raise worker.WorkerError("invalid_snapshot", "연속 의도 횟수가 유효하지 않습니다.")
    phase = snapshot["phase_index"]
    if type(phase) is not int or phase < -1 or phase >= actor["behavior_values"]["phases.$count"]:
        raise worker.WorkerError("invalid_snapshot", "해당 Actor의 phase_index가 유효하지 않습니다.")
    group = snapshot["group_bias"]
    if not isinstance(group, dict) or set(group) != GROUP_FIELDS:
        raise worker.WorkerError("invalid_snapshot", "그룹 의도 보정을 모두 명시해야 합니다.")
    for name, value in group.items():
        require_number(value, name, minimum=0 if name.endswith("Multiplier") else None)
    intents = snapshot["candidate_intents"]
    if not isinstance(intents, list) or not 2 <= len(intents) <= 8 or any(not isinstance(value, str) for value in intents):
        raise worker.WorkerError("invalid_snapshot", "호출자가 선정한 의도 후보 2~8개가 필요합니다.")
    if len(set(intents)) != len(intents) or not set(intents) <= set(contract["intent_names"]):
        raise worker.WorkerError("invalid_snapshot", "의도 후보가 중복되었거나 유효하지 않습니다.")
    attacks = {(attack["ability_id"], attack["variant_id"]): attack for attack in actor["attacks"]}
    states = snapshot["ability_states"]
    if not isinstance(states, list):
        raise worker.WorkerError("invalid_snapshot", "모든 공격 Variant의 런타임 상태 목록이 필요합니다.")
    known = set()
    compact_attacks = []
    for state in states:
        if not isinstance(state, dict) or set(state) != {"ability_id", "variant_id", "can_activate", "runtime_eligible", "cooldown_remaining_s", "rejection_reasons"}:
            raise worker.WorkerError("invalid_snapshot", "공격 상태 필드가 계약과 다릅니다.")
        if not isinstance(state["ability_id"], str) or not isinstance(state["variant_id"], str):
            raise worker.WorkerError("invalid_snapshot", "공격 식별자는 문자열이어야 합니다.")
        key = (state["ability_id"], state["variant_id"])
        if key not in attacks or key in known:
            raise worker.WorkerError("invalid_snapshot", "알 수 없거나 중복된 공격 상태입니다.")
        known.add(key)
        if type(state["can_activate"]) is not bool or type(state["runtime_eligible"]) is not bool:
            raise worker.WorkerError("invalid_snapshot", "활성화·선택 가능 여부는 bool이어야 합니다.")
        require_number(state["cooldown_remaining_s"], "cooldown_remaining_s")
        if not isinstance(state["rejection_reasons"], list) or any(not isinstance(reason, str) for reason in state["rejection_reasons"]):
            raise worker.WorkerError("invalid_snapshot", "거절 사유는 문자열 목록이어야 합니다.")
        attack = attacks[key]
        if state["runtime_eligible"] and (not state["can_activate"] or state["cooldown_remaining_s"] > 0 or state["rejection_reasons"] or attack["motion_pending"]):
            raise worker.WorkerError("invalid_snapshot", "선택 가능 표시와 실행 조건이 모순됩니다.")
        if state["runtime_eligible"] and snapshot["runtime_level"] < attack["required_level"]:
            raise worker.WorkerError("invalid_snapshot", "현재 레벨에 잠긴 공격이 선택 가능으로 표시됐습니다.")
        compact_attacks.append({"id": state["ability_id"], "variant": state["variant_id"],
                               "range": [attack["min_distance"], attack["max_distance"]],
                               "category": attack["category"], "eligible": state["runtime_eligible"],
                               "cooldown_s": state["cooldown_remaining_s"]})
    if known != set(attacks):
        raise worker.WorkerError("invalid_snapshot", "일부 AI 공격의 런타임 상태가 빠져 있습니다.")
    request = {
        "protocol_version": 1, "request_id": snapshot["request_id"], "session_id": snapshot["session_id"],
        "state_seq": snapshot["state_seq"],
        "observation": {"actor": actor["actor_id"], "source": snapshot["source"], "role": actor["role"],
                        "runtime_level": snapshot["runtime_level"], "blackboard": blackboard,
                        "self_tags": snapshot["self_tags"], "target_tags": snapshot["target_tags"],
                        "context": context, "phase_index": phase,
                        "history": [snapshot["last_intent"], snapshot["consecutive_intent_count"]],
                        "group_bias": group, "attacks": compact_attacks},
        "candidates": [{"id": intent, "description": INTENT_DESCRIPTIONS[intent]} for intent in intents],
    }
    return worker.validate_request(request)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--snapshot", type=Path)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--verify", action="store_true")
    args = parser.parse_args()
    try:
        manifest = verify_pack()
        if args.verify:
            print(worker.encode_json({"verified_files": len(manifest["files"]), "objects": manifest["object_count"]}))
            return 0
        if args.snapshot is None or args.output is None:
            parser.error("--snapshot과 --output이 필요합니다.")
        request = build_request(read(args.snapshot))
        args.output.parent.mkdir(parents=True, exist_ok=True)
        with args.output.open("x", encoding="utf-8", newline="\n") as stream:
            stream.write(worker.encode_json(request) + "\n")
        return 0
    except (worker.WorkerError, OSError, ValueError) as error:
        print(str(error))
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
