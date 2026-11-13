"""추론 처리자의 참고 자료만 허용 목록으로 복사하고 원본 해시를 기록한다."""

from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import re
import subprocess


ROOT = Path(__file__).resolve().parent
PROJECT = ROOT.parents[1]
SOURCES = (
    "Assets/10.Datas/Combat/Resources/CombatBalanceTargets.asset",
    "Assets/10.Datas/Combat/Resources/CombatEncounterProfile.asset",
    "Assets/10.Datas/Combat/Resources/DefaultCombatReactionPolicy.asset",
    "Assets/10.Datas/Combat/Policy/EliteCombatReactionPolicy.asset",
    "Assets/10.Datas/Combat/Policy/BossCombatReactionPolicy.asset",
    "Assets/10.Datas/Actor/Enemy/LevelTendency/EnemyLevelTendency_Field.asset",
    "Assets/10.Datas/AI/BehaviorTree/SourceJson/EnemyBehavior_Golem_Juggernaut.json",
    "Assets/10.Datas/AI/BehaviorTree/SourceJson/EnemyBehavior_Ent_ForestWarden.json",
    "Assets/10.Datas/Ability/Migrated/PlayerKatanaAttackData/AbilitySet_PlayerKatanaAttackData.asset",
    "Assets/10.Datas/Ability/Migrated/PlayerKatanaAttackData/GA_PlayerKatanaAttackData_Light_00.asset",
    "Assets/10.Datas/Ability/Migrated/PlayerKatanaAttackData/GA_PlayerKatanaAttackData_Heavy_00.asset",
    "Assets/10.Datas/Ability/Migrated/PlayerDoubleAxeAttackData/AbilitySet_PlayerDoubleAxeAttackData.asset",
    "Assets/10.Datas/Ability/Migrated/PlayerDoubleAxeAttackData/GA_PlayerDoubleAxeAttackData_Light_00.asset",
    "Assets/10.Datas/Ability/Migrated/PlayerDoubleAxeAttackData/GA_PlayerDoubleAxeAttackData_Heavy_00.asset",
    "Assets/docs/LEVEL_GRADE_COMBAT_BALANCE_POLICY.md",
    "Assets/docs/design/combat/encounter/SWEEP_COMBAT_AND_MAC_LAYA_VALIDATION_DESIGN.md",
)


def git_output(*arguments):
    """원본 작업 트리와 커밋의 식별 정보를 읽기 전용으로 확인한다."""
    result = subprocess.run(["git", "-C", str(PROJECT), *arguments], capture_output=True, text=True)
    return result.stdout.strip() if result.returncode == 0 else None


def main():
    """원본 파일을 저장·재베이크하지 않고 스냅샷을 만든다."""
    data = ROOT / "data"
    entries = []
    for relative in SOURCES:
        source = PROJECT / relative
        raw = source.read_bytes()
        destination = data / "reference" / relative
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(raw)
        meta = source.with_name(source.name + ".meta")
        guid = re.search(r"^guid: ([0-9a-f]{32})$", meta.read_text(encoding="utf-8-sig"), re.MULTILINE) if meta.exists() else None
        blob = git_output("hash-object", "--", relative)
        head_blob = git_output("rev-parse", "HEAD:" + relative)
        entries.append({
            "copy_path": destination.relative_to(data).as_posix(), "source_path": relative,
            "source_guid": guid.group(1) if guid else None,
            "source_git_blob": blob, "source_head_blob": head_blob,
            "source_changed_from_head": blob != head_blob,
            "bytes": len(raw), "sha256": hashlib.sha256(raw).hexdigest(),
        })
    for source in sorted((data / "examples").glob("*.json*")):
        raw = source.read_bytes()
        entries.append({"copy_path": source.relative_to(data).as_posix(), "source_path": None,
                        "kind": "synthetic_protocol_fixture", "bytes": len(raw),
                        "sha256": hashlib.sha256(raw).hexdigest()})
    manifest = {
        "schema_version": 1, "created_at_utc": datetime.now(timezone.utc).isoformat(),
        "source_commit": git_output("rev-parse", "HEAD"),
        "scope": "inference_reference_only_not_resolved_combat_simulation",
        "notes": [
            "reference는 원본 바이트 복사이며 참조 GUID의 의존 에셋 전체를 포함하지 않습니다.",
            "examples는 합성 관측입니다. 런타임 실측값이나 전투 검증 결과가 아닙니다.",
            "처리자는 관측과 후보만 추론하며 참고 에셋을 자동으로 해석하거나 실행하지 않습니다.",
        ],
        "files": entries,
    }
    (data / "manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"{len(entries)}개 파일, {sum(entry['bytes'] for entry in entries):,} bytes 복사 확인")


if __name__ == "__main__":
    main()
