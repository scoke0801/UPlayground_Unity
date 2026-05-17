"""
BT JSON 구조 검증 스크립트.

사용법:
    python validate_bt_json.py <bt_json_path>

검사 항목 (모두 SKILL 사용 여부와 무관하게 객관적으로 통과/실패 판정):
  - JSON parseable
  - top-level: rootGuid (string), blackboard (list), nodes (list)
  - rootGuid가 어떤 노드의 guid와 일치
  - 모든 노드 guid가 유일
  - 모든 children guid가 nodes 안의 guid로 resolve
  - 모든 node.type 짧은 클래스명이 CATALOG에 등록되어 있음
  - Composite는 자식 >= 1, Decorator는 자식 == 1
  - 각 properties[].name이 그 노드의 유효 필드명임 (오타 검출)
  - blackboard 항목의 valueType이 0~5 범위
"""
from __future__ import annotations
import json
import re
import sys
from pathlib import Path

CATALOG = {
    # composite
    "SelectorNode":              ("composite", {"_abortType"}),
    "SequenceNode":              ("composite", {"_abortType"}),
    "ParallelNode":              ("composite", {"_requireAllSuccess"}),
    "WeightedRandomSelectorNode":("composite", set()),
    # decorator
    "InverterNode":              ("decorator", set()),
    "CooldownNode":              ("decorator", {"_cooldown"}),
    "RepeatNode":                ("decorator", {"_repeatCount"}),
    "TimeoutNode":               ("decorator", {"_timeout"}),
    "ReturnSuccessNode":         ("decorator", set()),
    "ReturnFailureNode":         ("decorator", set()),
    "UntilSuccessNode":          ("decorator", set()),
    "UntilFailureNode":          ("decorator", set()),
    "GuardConditionNode":        ("decorator", {"_expectedValue"}),
    "ForceAbortNode":            ("decorator", {"_triggerOn"}),
    # action
    "WaitNode":                  ("action", {"_duration"}),
    "LogNode":                   ("action", {"_message", "_logEveryTick"}),
    "SetBlackboardValueNode":    ("action", {"_key", "_valueType", "_boolValue", "_intValue",
                                              "_floatValue", "_stringValue", "_vector3Value"}),
    "SyncEnemyBlackboardNode":   ("action", {"_hasTargetKey", "_targetKey",
                                              "_distanceKey", "_stateKey"}),
    "TransitionEnemyStateNode":  ("action", {"_targetState", "_skipIfAlreadyInState"}),
    "TransitionFlyingEnemyStateNode": ("action", {"_skipIfAlreadyInState"}),
    "ExecuteEnemyAttackNode":    ("action", set()),
    "RequestEnemyAttackSlotNode":("action", set()),
    "KeepCurrentStateNode":      ("action", set()),
    "SubtreeNode":               ("action", set()),
    "ResetFlyingCountersNode":   ("action", set()),
    "ResetFlyingAirCountersNode":("action", set()),
    "DescendFlyingNode":         ("action", set()),
    "SelectFlyingDiveSkillNode": ("action", set()),
    "RequestFlyingAttackSlotNode":("action", set()),
    # condition
    "HasTargetNode":             ("condition", {"_expectedValue"}),
    "IsTargetInRangeNode":       ("condition", {"_comparison", "_minDistance", "_maxDistance"}),
    "IsCurrentActorStateNode":   ("condition", {"_stateName", "_expectedValue"}),
    "BlackboardBoolConditionNode":("condition", {"_key", "_expectedValue"}),
    "IsEnemyPatrolEnabledNode":  ("condition", set()),
    "IsEnemyPhaseNode":          ("condition", {"_phaseName", "_phaseIndex"}),
    "CanUseEnemySkillNode":      ("condition", {"_requireTarget"}),
    "HasEnemyActionDelayElapsedNode": ("condition", set()),
    "IsBlockedEnemyStateNode":   ("condition", set()),
    "IsFlyingAirStateNode":      ("condition", set()),
    "IsFlyingGroundCombatStateNode":("condition", set()),
    "IsAirAttackLimitReachedNode":("condition", set()),
    "ShouldFlyingTakeOffNode":   ("condition", set()),
    "HasDiveSkillAvailableNode": ("condition", set()),
    "RollDiveChanceNode":        ("condition", set()),
    "FlyingCanUseSkillNode":     ("condition", set()),
    # service
    "SyncEnemyBlackboardService":("service", {"_interval", "_tickOnEnter", "_hasTargetKey",
                                                "_targetKey", "_distanceKey", "_stateKey"}),
    "SyncEnemyMemoryService":    ("service", {"_interval", "_tickOnEnter"}),
    "SyncEnemyPhaseService":     ("service", {"_interval", "_tickOnEnter"}),
}

TYPE_RE = re.compile(r"^UPlayGround\.AI\.BehaviorTree\.(?P<cls>[A-Za-z0-9_]+)\s*,\s*Assembly-CSharp\b")

# Enum value sets (case-sensitive — Enum.Parse defaults to case-sensitive in C#)
ENUM_VALUES = {
    "BTAbortType":               {"None", "Self", "LowerPriority", "Both"},
    "BlackboardValueType":       {"Bool", "Int", "Float", "String", "Vector3", "Object"},
    "FloatComparisonType":       {"LessOrEqual", "GreaterOrEqual", "Between"},
    "EnemyTransitionStateType":  {"Idle", "Patrol", "Chase", "Attack", "Retreat",
                                  "Dodge", "Circle", "Guard", "Charge", "Flank", "Counter"},
}

# Map field-name -> expected enum class for each node where it applies
FIELD_ENUM_HINTS = {
    "SelectorNode":               {"_abortType": "BTAbortType"},
    "SequenceNode":               {"_abortType": "BTAbortType"},
    "SetBlackboardValueNode":     {"_valueType": "BlackboardValueType"},
    "IsTargetInRangeNode":        {"_comparison": "FloatComparisonType"},
    "TransitionEnemyStateNode":   {"_targetState": "EnemyTransitionStateType"},
}

BOOL_VALUES = {"True", "False"}
FLOAT_RE = re.compile(r"^-?\d+(\.\d+)?([eE][+-]?\d+)?$")  # no commas allowed
INT_RE = re.compile(r"^-?\d+$")
VECTOR_WRAPPER_RE = re.compile(r'^\{\s*"value"\s*:\s*\{')


def extract_class(type_str: str) -> str | None:
    m = TYPE_RE.match(type_str or "")
    return m.group("cls") if m else None


def validate(path: Path) -> tuple[int, list[dict]]:
    """Returns (fail_count, list of {check, passed, evidence})."""
    results: list[dict] = []

    def add(check: str, passed: bool, evidence: str = ""):
        results.append({"check": check, "passed": bool(passed), "evidence": evidence})

    raw = path.read_text(encoding="utf-8")
    try:
        data = json.loads(raw)
        add("JSON parseable", True, f"{len(raw)} bytes")
    except Exception as e:
        add("JSON parseable", False, str(e))
        return 1, results

    # Top-level shape
    add("rootGuid is string", isinstance(data.get("rootGuid"), str) and bool(data["rootGuid"]),
        repr(data.get("rootGuid")))
    add("blackboard is list", isinstance(data.get("blackboard"), list),
        f"type={type(data.get('blackboard')).__name__}")
    add("nodes is list", isinstance(data.get("nodes"), list),
        f"len={len(data.get('nodes', [])) if isinstance(data.get('nodes'), list) else 'N/A'}")

    nodes = data.get("nodes", []) or []
    if not nodes:
        add("at least 1 node", False, "nodes is empty")
        return sum(1 for r in results if not r["passed"]), results
    add("at least 1 node", True, f"{len(nodes)} nodes")

    # guid uniqueness
    guids = [n.get("guid") for n in nodes]
    add("all guids non-empty", all(isinstance(g, str) and g for g in guids),
        f"empty guid count={sum(1 for g in guids if not g)}")
    add("all guids unique", len(set(guids)) == len(guids),
        f"dupes={[g for g in guids if guids.count(g) > 1]}")

    guid_set = set(guids)
    # rootGuid resolves
    add("rootGuid resolves", data.get("rootGuid") in guid_set,
        f"rootGuid={data.get('rootGuid')}")

    # blackboard valueType range
    bb = data.get("blackboard", []) or []
    bad_vt = [e for e in bb if not isinstance(e.get("valueType"), int)
              or not (0 <= e["valueType"] <= 5)]
    add("blackboard valueTypes in [0,5]", not bad_vt,
        f"bad={[(e.get('key'), e.get('valueType')) for e in bad_vt]}")

    # per-node checks
    type_failures: list[str] = []
    children_failures: list[str] = []
    composite_kid_failures: list[str] = []
    decorator_kid_failures: list[str] = []
    prop_failures: list[str] = []
    value_failures: list[str] = []

    for n in nodes:
        guid = n.get("guid", "<no-guid>")
        cls = extract_class(n.get("type", ""))
        if cls is None or cls not in CATALOG:
            type_failures.append(f"{guid}: type={n.get('type')!r} (class={cls})")
            continue
        kind, valid_fields = CATALOG[cls]

        kids = n.get("children", []) or []
        for child in kids:
            if child not in guid_set:
                children_failures.append(f"{guid} -> {child}")

        if kind == "composite" and len(kids) < 1:
            composite_kid_failures.append(f"{guid} ({cls}) has {len(kids)} children")
        if kind == "decorator" and len(kids) != 1:
            decorator_kid_failures.append(f"{guid} ({cls}) has {len(kids)} children (need 1)")

        enum_hints = FIELD_ENUM_HINTS.get(cls, {})
        for prop in (n.get("properties") or []):
            name = prop.get("name")
            value = prop.get("value", "")
            ptype = prop.get("type", "") or ""

            if name not in valid_fields:
                prop_failures.append(f"{guid} ({cls}): unknown field {name!r}")
                continue

            # Value-format checks. Look at property type string first; fall back to enum hints by field name.
            enum_class = enum_hints.get(name)
            if enum_class:
                if value not in ENUM_VALUES[enum_class]:
                    value_failures.append(
                        f"{guid} ({cls}.{name}): enum value {value!r} not in {enum_class} {sorted(ENUM_VALUES[enum_class])}")
            elif "System.Boolean" in ptype:
                if value not in BOOL_VALUES:
                    value_failures.append(f"{guid} ({cls}.{name}): bool value {value!r} must be 'True' or 'False'")
            elif "System.Int32" in ptype:
                if not INT_RE.match(str(value)):
                    value_failures.append(f"{guid} ({cls}.{name}): int value {value!r} not integer-shaped")
            elif "System.Single" in ptype:
                if not FLOAT_RE.match(str(value)):
                    value_failures.append(
                        f"{guid} ({cls}.{name}): float value {value!r} not InvariantCulture float (check decimal point and commas)")
            elif "UnityEngine.Vector" in ptype:
                if not VECTOR_WRAPPER_RE.match(str(value)):
                    value_failures.append(
                        f"{guid} ({cls}.{name}): Vector value must be JSON string shaped as '{{\"value\":{{...}}}}'")

    add("every node type in catalog", not type_failures, "; ".join(type_failures[:5]))
    add("every child guid resolves", not children_failures, "; ".join(children_failures[:5]))
    add("composites have >= 1 child", not composite_kid_failures, "; ".join(composite_kid_failures[:5]))
    add("decorators have exactly 1 child", not decorator_kid_failures, "; ".join(decorator_kid_failures[:5]))
    add("all property names are valid fields", not prop_failures, "; ".join(prop_failures[:5]))
    add("all property values pass format checks", not value_failures, "; ".join(value_failures[:5]))

    fails = sum(1 for r in results if not r["passed"])
    return fails, results


def main():
    if len(sys.argv) != 2:
        print("Usage: python validate_bt_json.py <bt_json_path>", file=sys.stderr)
        sys.exit(2)
    path = Path(sys.argv[1])
    if not path.exists():
        print(f"File not found: {path}", file=sys.stderr)
        sys.exit(2)
    fails, results = validate(path)
    width = max(len(r["check"]) for r in results)
    for r in results:
        mark = "[PASS]" if r["passed"] else "[FAIL]"
        print(f"{mark} {r['check']:<{width}}  {r['evidence']}")
    print(f"\nTotal: {len(results)} checks, {fails} failures")
    sys.exit(0 if fails == 0 else 1)


if __name__ == "__main__":
    main()
