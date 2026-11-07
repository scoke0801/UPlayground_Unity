"""Unity가 해석한 선택 몬스터 그래프와 판단 코드의 독립 전달 묶음을 만든다."""

from collections import Counter
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import subprocess
from copy import deepcopy


ROOT = Path(__file__).resolve().parent
PROJECT = ROOT.parents[1]
PACK = ROOT / "enemy-data"
SOURCE_FOLDERS = (
    "GameActor/AI", "GameActor/Group", "GameActor/Component/Enemy", "GameActor/State/Enemy",
    "GameActor/Animation/MotionEvents", "GameActor/Combat/Resolution", "GameActor/Gameplay/Ability",
    "Data/Actor/Enemy", "Data/AI", "Data/Ability", "Data/Combat", "Data/Gameplay", "Ability/Core", "Ability/UPlayGround",
    "GameActor/Gameplay/Tag", "GameActor/Gameplay/Effect",
)
EXTRA_SOURCES = (
    "Assets/02.Scripts/Data/Actor/ActorDefinitionSO.cs",
    "Assets/02.Scripts/Data/Actor/Animation/ActorAnimationMotionSet.cs",
    "Assets/02.Scripts/GameActor/Animation/ActorAnimator.cs",
    "Assets/02.Scripts/GameActor/Object/Monster/MonsterActor.cs",
    "Assets/02.Scripts/GameActor/Component/Common/PoiseStat.cs",
)
RULES = (
    "Assets/10.Datas/AI/BehaviorTree/SourceJson/EnemyBehavior_Golem_Juggernaut.json",
    "Assets/10.Datas/AI/BehaviorTree/SourceJson/EnemyBehavior_Ent_ForestWarden.json",
    "Assets/10.Datas/AI/BehaviorTree/BehaviorTreeEditorRegistry.json",
)


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2, allow_nan=False) + "\n", encoding="utf-8", newline="\n")


def enum_name(value):
    return value["name"] if isinstance(value, dict) and "name" in value else value


def refs(value):
    """문서 전체에서 데이터 노드 참조를 찾는다."""
    if isinstance(value, dict):
        if value.get("kind") == "data":
            yield value["id"]
        for child in value.values():
            yield from refs(child)
    elif isinstance(value, list):
        for child in value:
            yield from refs(child)


def summarize(document):
    """Actor 유효 연결과 AI 선택 공격을 원시 그래프에서 읽기 쉬운 카드로 만든다."""
    objects = document["objects"]
    result = []
    for actor in document["actors"]:
        behavior = objects[actor["behavior"]["id"]]["properties"]
        cards = []
        for ability in actor["effective_abilities"]:
            definition = objects[ability["asset"]["id"]]["properties"]
            for variant in ability["variants"]:
                if not variant.get("ai_selectable"):
                    continue
                payload = objects[variant["payload"]["id"]]["properties"]
                windows = []
                for motion_ref in variant["resolved_motions"]:
                    properties = objects[motion_ref["id"]]["properties"]
                    for key, event_type in properties.items():
                        if key.endswith(".$type") and event_type:
                            prefix = key[:-6]
                            windows.append({"type": event_type, "start": properties.get(prefix + ".startTime"),
                                            "end": properties.get(prefix + ".endTime"), "property_path": prefix})
                cards.append({
                    "ability_id": ability["ability_id"], "variant_id": variant["variant_id"],
                    "ability_ref": ability["asset"]["id"], "payload_ref": variant["payload"]["id"],
                    "motion_key": variant["motion_key"], "motion_refs": [m["id"] for m in variant["resolved_motions"]],
                    "motion_pending": variant["motion_pending"],
                    "category": enum_name(payload["attackInfo.attackCategory"]),
                    "roles": payload["attackInfo.aiRoles"], "required_level": payload["attackInfo.requiredLevel"],
                    "selection_weight": payload["attackInfo.selectionWeight"],
                    "min_distance": definition["activation.minDistance"], "max_distance": definition["activation.maxDistance"],
                    "cost": {k[5:]: v for k, v in definition.items() if k.startswith("cost.")},
                    "cooldown": {k[9:]: v for k, v in definition.items() if k.startswith("cooldown.")},
                    "hit_phases": {k: v for k, v in payload.items() if k.startswith("attackInfo.baseInfo.hitPhases")},
                    "variant_and_tag_conditions_ref": ability["asset"]["id"],
                    "extra_attack_conditions_ref": variant["payload"]["id"], "motion_events": windows,
                })
        result.append({
            "actor_id": actor["actor_id"], "grade": actor["grade"], "level": actor["level"],
            "definition_ref": actor["definition"]["id"], "behavior_ref": actor["behavior"]["id"],
            "tree_ref": behavior["behaviorTree"]["id"], "weights_ref": behavior["intentWeights"]["id"],
            "strategy_ref": behavior["combatStrategy"], "role": enum_name(behavior["aiRole"]),
            "behavior_values": {k: v for k, v in behavior.items() if not k.startswith("m_")},
            "attacks": cards,
        })
    return result


def verify_graph(document):
    if document["problems"]:
        raise ValueError("Unity export problems: " + repr(document["problems"]))
    missing = set(refs(document)) - set(document["objects"])
    if missing:
        raise ValueError("Missing data references: " + repr(sorted(missing)))
    for actor in document["actors"]:
        for ability in actor["effective_abilities"]:
            for variant in ability["variants"]:
                if variant.get("ai_selectable") and not variant.get("motion_pending") and len(variant["resolved_motions"]) != 1:
                    raise ValueError("Missing or ambiguous motion: " + ability["ability_id"])


def make_examples(actor_cards, document):
    """실측으로 오인하지 않게 표시한 완전 입력 예제와 빈 캡처 양식을 만든다."""
    from enemy_decision_request import build_request

    requests = []
    score_path = PACK / "native-score-fixtures.json"
    native_scores = {row["request_id"]: row for row in read(score_path)} if score_path.exists() else {}
    for actor in actor_cards:
        behavior = actor["behavior_values"]
        rules = next(read(PROJECT / path) for path in RULES[:2]
                     if read(PROJECT / path)["sourceBehaviorSo"] == document["objects"][actor["behavior_ref"]]["path"])
        blackboard = rules["blackboard"]
        level = document["objects"][behavior["levelTendency"]["id"]]["properties"]
        bands = [{k: v for k, v in level.items() if k.startswith(f"bands.Array.data[{index}].")}
                 for index in range(level["bands.$count"])]
        band = next(band for band in reversed(bands) if next(v for k, v in band.items() if k.endswith(".minLevel")) <= actor["level"])

        def scaled(name, scale):
            factor = next(v for k, v in band.items() if k.endswith("." + scale))
            return max(0.0, min(1.0, blackboard[name] * factor))

        for index, (name, distance, changes, intents) in enumerate([
            ("near", 1.5, {}, ["Attack", "Pressure", "Punish"]),
            ("far", 6.0, {}, ["Chase", "Pressure", "KeepDistance"]),
            ("player-recovery", 1.5, {"IsPlayerRecovering": True}, ["Punish", "Attack", "Pressure"]),
            ("player-pressure", 0.8, {"IsPlayerAttacking": True, "IsPlayerAttackingFrequently": True}, ["Counter", "Retreat", "KeepDistance"]),
            ("waiting", 2.0, {"ActionDelayElapsed": False}, ["Pressure", "KeepDistance", "Recover"]),
            ("low-health", 1.0, {"HealthPercent": 0.25, "WasHitRecently": True}, ["Retreat", "Recover", "KeepDistance"]),
        ]):
            context = {field["name"]: False if field["type"] == "Boolean" else ("None" if field["enum_values"] else 0.0)
                       for field in document["context_fields"]}
            context.update({
                "Distance": distance, "OptimalDistance": min(behavior["optimalCombatDistance"], max(a["max_distance"] for a in actor["attacks"])),
                "MinDistance": behavior["minCombatDistance"], "PersonalSpace": behavior["personalSpaceDistance"],
                "PreferredRange": blackboard["preferredRange"], "HealthPercent": 1.0,
                "Aggression": scaled("aggression", "aggressionScale"), "ReactionChance": scaled("reactionChance", "reactionScale"),
                "PunishChance": scaled("punishRecoveryChance", "punishScale"), "CounterChance": scaled("counterChance", "counterScale"),
                "RetreatChance": blackboard["retreatChance"], "GuardChance": blackboard["guardChance"],
                "CircleWeight": blackboard["circleWeight"], "MinRetreatCooldown": blackboard["minRetreatCooldown"],
                "TimeSinceRetreat": 100.0, "ActionDelayElapsed": True,
            })
            states = []
            for attack in actor["attacks"]:
                eligible = (not attack["motion_pending"] and actor["level"] >= attack["required_level"]
                            and attack["min_distance"] <= distance <= attack["max_distance"])
                states.append({"ability_id": attack["ability_id"], "variant_id": attack["variant_id"],
                               "can_activate": eligible, "runtime_eligible": eligible, "cooldown_remaining_s": 0.0,
                               "rejection_reasons": [] if eligible else ["SyntheticNominalRangeOrLevelCheck"]})
            context["HasAvailableAttack"] = any(s["runtime_eligible"] for s in states)
            context["CanUseSkill"] = any(s["runtime_eligible"] and a["category"] == "Skill" for s, a in zip(states, actor["attacks"]))
            context.update(changes)
            snapshot = {
                "schema_version": 1, "actor_id": actor["actor_id"], "source": "synthetic_fixture",
                "request_id": f"{actor['actor_id']}-{name}", "session_id": "enemy-data-contract-examples", "state_seq": index,
                "context": context, "phase_index": -1, "last_intent": "None", "consecutive_intent_count": 0,
                "runtime_level": actor["level"], "self_tags": [], "target_tags": [],
                "blackboard": {"Target.Has": True, "Target.Distance": distance, "Self.StateId": "Idle",
                               "HasAttackSlot": True, "NextActionAllowedTime": 0.0},
                "group_bias": {"AttackMultiplier": 1.0, "PunishMultiplier": 1.0, "CounterMultiplier": 1.0,
                               "PressureBonus": 0.0, "RetreatBonus": 0.0, "KeepDistanceBonus": 0.0},
                "candidate_intents": intents, "ability_states": states,
            }
            if native_scores:
                native = native_scores[snapshot["request_id"]]
                if native["context"] != context:
                    raise ValueError("Native score fixture context is stale: " + snapshot["request_id"])
                snapshot["candidate_intents"] = native["candidate_pool"]
            path = PACK / "examples" / f"{actor['actor_id']}-{name}.json"
            write(path, snapshot)
            requests.append(build_request(snapshot))
            if index == 0:
                template = deepcopy(snapshot)
                template.update(source="runtime_capture", request_id="capture-id", session_id="capture-session", state_seq=0)
                template["context"] = {key: None for key in context}
                template["candidate_intents"] = []
                for state in template["ability_states"]:
                    state.update(can_activate=None, runtime_eligible=None, cooldown_remaining_s=None, rejection_reasons=None)
                template.update(phase_index=None, last_intent=None, consecutive_intent_count=None)
                template["runtime_level"] = None
                template["blackboard"] = {key: None for key in snapshot["blackboard"]}
                template.update(self_tags=None, target_tags=None)
                template["group_bias"] = {key: None for key in snapshot["group_bias"]}
                write(PACK / "templates" / f"{actor['actor_id']}.json", template)
    with (PACK / "examples" / "requests.jsonl").open("w", encoding="utf-8", newline="\n") as stream:
        for request in requests:
            stream.write(json.dumps(request, ensure_ascii=False, separators=(",", ":")) + "\n")


def main():
    """원본을 바꾸지 않고 선택 의존성·판단 코드·관측 입력 계약을 배치한다."""
    document = read(PACK / "unity-export.json")
    verify_graph(document)
    actor_cards = summarize(document)
    write(PACK / "actors.json", actor_cards)
    files = {node["path"] for node in document["objects"].values()}
    files.update(node["script_path"] for node in document["objects"].values() if node.get("script_path"))
    for folder in SOURCE_FOLDERS:
        source = PROJECT / "Assets/02.Scripts" / folder
        if not source.is_dir():
            raise FileNotFoundError(source)
        files.update(path.relative_to(PROJECT).as_posix() for path in source.rglob("*.cs"))
    files.update(RULES)
    files.update(EXTRA_SOURCES)
    entries = []
    for relative in sorted(files):
        source = PROJECT / relative
        if not source.is_file():
            raise FileNotFoundError(relative)
        raw = source.read_bytes()
        destination = PACK / "reference" / relative
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(raw)
        entries.append({"path": destination.relative_to(PACK).as_posix(), "source": relative,
                        "bytes": len(raw), "sha256": hashlib.sha256(raw).hexdigest()})
    contract = {
        "schema_version": 1, "source": "Unity reflection of IntentEvaluationContext",
        "context_fields": document["context_fields"],
        "additional_required": {
            "phase_index": "Actual phase index, -1 only when no active phase.",
            "runtime_level": "Current EnemyCombat level, not necessarily the authored initial level.",
            "blackboard": "Capture BT Blackboard with registered names and dynamic Cooldown.* entries; core keys are mandatory.",
            "self_tags_and_target_tags": "Current GAS tags, not assumed empty from static data.",
            "last_intent": "Previous intent or None when no history.",
            "consecutive_intent_count": "Observed consecutive count, never silently defaulted.",
            "group_bias": "Attack/Punish/Counter multipliers, Pressure/Retreat/KeepDistance bonuses.",
            "candidate_intents": "2-8 intents from the caller's candidate generator; usually weighted top three.",
            "ability_states": "Runtime CanActivate result, rejection reasons and cooldown for every AI attack variant.",
            "source": "runtime_capture or synthetic_fixture. Synthetic is not a runtime validation result.",
        },
        "intent_names": document["intent_names"],
        "missing_policy": "Reject missing values. Do not infer cooldowns, slots, memory or visibility from static assets.",
    }
    write(PACK / "observation-contract.json", contract)
    make_examples(actor_cards, document)
    generated = ["unity-export.json", "actors.json", "observation-contract.json", "worker-config.json", "README.md", "MAC_WORK_ORDER.md"]
    if (PACK / "native-score-fixtures.json").exists():
        generated.append("native-score-fixtures.json")
    generated += [path.relative_to(PACK).as_posix() for directory in ("examples", "templates") for path in sorted((PACK / directory).glob("*.json*"))]
    for name in generated:
        raw = (PACK / name).read_bytes()
        entries.append({"path": name, "bytes": len(raw), "sha256": hashlib.sha256(raw).hexdigest()})
    manifest = {
        "schema_version": 1, "scope": "Golem_Normal_and_Ent_Normal_static_decision_dependencies",
        "exporter": "Unity Editor read-only object traversal; no Play Mode",
        "created_at_utc": datetime.now(timezone.utc).isoformat(),
        "source_commit": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=PROJECT, text=True).strip(),
        "source_worktree_dirty": subprocess.run(["git", "diff", "--quiet", "--", "Assets"], cwd=PROJECT).returncode != 0,
        "actor_count": len(actor_cards), "object_count": len(document["objects"]),
        "metadata_only_count": len(document["metadata_only"]),
        "metadata_only_scope": "Animation clips and visual/native references: metadata only, no original binary assets.",
        "examples_scope": "Synthetic observations; candidate pools from native score core when native-score-fixtures.json is present. No live BT/physics execution.",
        "type_counts": dict(Counter(node["type"] for node in document["objects"].values())),
        "files": entries,
    }
    write(PACK / "manifest.json", manifest)
    print(f"Packaged {len(actor_cards)} actors, {len(document['objects'])} objects, {len(entries)} files, "
          f"{sum(e['bytes'] for e in entries):,} bytes")


if __name__ == "__main__":
    main()
