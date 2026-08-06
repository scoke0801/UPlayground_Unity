#!/usr/bin/env python3
"""UPlayground Monster Behavior Rules JSON 정적 검증기."""

from __future__ import annotations

import argparse
import json
import re
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Iterable


KEYS_REL = Path("Assets/02.Scripts/GameActor/Editor/MonsterBehaviorJsonNodeKeys.cs")
IMPORTER_REL = Path("Assets/02.Scripts/GameActor/Editor/MonsterBehaviorTreeJsonImporter.cs")
FACTORY_REL = Path("Assets/02.Scripts/GameActor/Editor/MonsterBehaviorTreeJsonImporter.NodeFactory.cs")
REGISTRY_REL = Path("Assets/10.Datas/AI/BehaviorTree/BehaviorTreeEditorRegistry.json")
SOURCE_REL = Path("Assets/10.Datas/AI/BehaviorTree/SourceJson")

TOP_FIELDS = {"schemaVersion", "id", "displayName", "actorKind", "sourceBehaviorSo", "blackboard", "groups", "rules"}
GROUP_FIELDS = {"name", "priority", "when", "rules"}
RULE_FIELDS = {"name", "priority", "select", "when", "do", "choices"}
CONDITION_FIELDS = {"condition", "invert", "attackCategory", "key", "op", "value", "valueKey"}
ACTION_FIELDS = {"action", "intent", "style", "state", "attackCategory", "cooldownId", "cooldownDuration", "duration"}
CHOICE_FIELDS = {"weight", "weightKey", "action", "intent", "style", "state", "attackCategory", "cooldownId", "cooldownDuration"}

VALUE_REQUIRED = {"HasStateTag", "IsEnemyPhase", "CooldownReady", "IsSelfLowHealth", "RecentHitCountGreaterOrEqual", "ConsecutiveAttackCountLessThan", "ConsecutiveAttackCountGreaterOrEqual", "CanRevengeAfterHit", "SelectedIntent", "IsCurrentState"}
NUMERIC_VALUE_REQUIRED = {"IsSelfLowHealth", "RecentHitCountGreaterOrEqual", "ConsecutiveAttackCountLessThan", "ConsecutiveAttackCountGreaterOrEqual", "CanRevengeAfterHit"}
ALIAS_CONDITIONS = {"IsPlayerAttacking", "IsPlayerGuarding", "IsPlayerStaggered", "IsPlayerRecovering", "IsPlayerDodgingFrequently", "IsPlayerAttackingFrequently", "IsPlayerGuardingFrequently", "IsPlayerRecoveringFrequently", "RecentlyHitByPlayer", "HasAttackSlot", "IsPoiseBroken", "RecentHitCountGreaterOrEqual", "SelectedIntent"}
PROBABILITY_FIELDS = {"aggression", "reactionChance", "counterChance", "dodgeChance", "punishRecoveryChance", "antiGuardChance", "poiseRatio", "revengeChance"}
NON_NEGATIVE_FIELDS = {"circleWeight", "minRetreatCooldown", "maxComboPressureCount", "recentHitCount", "hitReactionLockTime"}


class DuplicateKeyError(ValueError):
    pass


def no_duplicate_object(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
    result: dict[str, Any] = {}
    for key, value in pairs:
        if key in result:
            raise DuplicateKeyError(f"중복 JSON key: {key}")
        result[key] = value
    return result


def block_after(text: str, marker: str) -> str:
    match = re.search(marker, text)
    if not match:
        raise ValueError(f"C# 선언을 찾을 수 없습니다: {marker}")
    start = text.find("{", match.end())
    if start < 0:
        raise ValueError(f"C# block 시작을 찾을 수 없습니다: {marker}")
    depth = 0
    for index in range(start, len(text)):
        if text[index] == "{":
            depth += 1
        elif text[index] == "}":
            depth -= 1
            if depth == 0:
                return text[start + 1:index]
    raise ValueError(f"C# block 끝을 찾을 수 없습니다: {marker}")


def constants_in_class(text: str, class_name: str) -> dict[str, str]:
    body = block_after(text, rf"\bclass\s+{re.escape(class_name)}\b")
    return dict(re.findall(r'const\s+string\s+(\w+)\s*=\s*"([^"]+)"\s*;', body))


def enum_values(project: Path, enum_name: str) -> set[str]:
    pattern = re.compile(rf"\benum\s+{re.escape(enum_name)}\b")
    for path in (project / "Assets/02.Scripts").rglob("*.cs"):
        text = path.read_text(encoding="utf-8-sig")
        if not pattern.search(text):
            continue
        body = block_after(text, rf"\benum\s+{re.escape(enum_name)}\b")
        body = re.sub(r"//.*?$|/\*.*?\*/", "", body, flags=re.MULTILINE | re.DOTALL)
        values = set()
        for part in body.split(","):
            match = re.match(r"\s*(\w+)", part)
            if match:
                values.add(match.group(1))
        return values
    raise ValueError(f"enum을 찾을 수 없습니다: {enum_name}")


def public_numeric_fields(text: str, class_name: str) -> set[str]:
    body = block_after(text, rf"\bclass\s+{re.escape(class_name)}\b")
    return set(re.findall(r"public\s+(?:float|int)\s+(\w+)\b", body))


def public_fields(text: str, class_name: str) -> set[str]:
    body = block_after(text, rf"\bclass\s+{re.escape(class_name)}\b")
    return set(re.findall(r"public\s+(?:float|int|bool|string)\s+(\w+)\b", body))


def find_project_root(start: Path) -> Path:
    start = start.resolve()
    candidates = [start, *start.parents]
    for candidate in candidates:
        if (candidate / KEYS_REL).is_file() and (candidate / IMPORTER_REL).is_file():
            return candidate
    raise FileNotFoundError("UPlayground 프로젝트 루트를 찾을 수 없습니다. --project-root를 지정하세요.")


@dataclass
class Catalog:
    conditions: set[str]
    actions: set[str]
    selects: set[str]
    scopes: dict[tuple[str, str], str]
    blackboard_fields: set[str]
    numeric_references: set[str]
    registry_keys: set[str]
    aliases: set[str]
    enums: dict[str, set[str]]


def load_catalog(project: Path) -> Catalog:
    keys_text = (project / KEYS_REL).read_text(encoding="utf-8-sig")
    importer_text = (project / IMPORTER_REL).read_text(encoding="utf-8-sig")
    factory_text = (project / FACTORY_REL).read_text(encoding="utf-8-sig")
    condition_map = constants_in_class(keys_text, "Conditions")
    action_map = constants_in_class(keys_text, "Actions")
    select_map = constants_in_class(keys_text, "SelectKinds")
    scopes: dict[tuple[str, str], str] = {}
    scope_pattern = re.compile(
        r"\[MonsterBehaviorJsonNodeKeys\.(Conditions|Actions)\.(\w+)\]\s*=\s*new\(JsonNodeActorScope\.(\w+)",
        re.MULTILINE,
    )
    for kind, identifier, scope in scope_pattern.findall(factory_text):
        values = condition_map if kind == "Conditions" else action_map
        if identifier in values:
            scopes[(kind, values[identifier])] = scope

    blackboard_fields = public_fields(importer_text, "MonsterBehaviorBlackboardJson")
    numeric_references = public_numeric_fields(importer_text, "MonsterBehaviorBlackboardJson")
    enemy_behavior_files = list((project / "Assets/02.Scripts").rglob("EnemyBehaviorSO.cs"))
    if enemy_behavior_files:
        numeric_references |= public_numeric_fields(enemy_behavior_files[0].read_text(encoding="utf-8-sig"), "EnemyBehaviorSO")

    registry = json.loads((project / REGISTRY_REL).read_text(encoding="utf-8-sig"))
    registry_keys = set()
    for entry in registry.get("enemyBlackboardDefaults", []):
        if entry.get("key"):
            registry_keys.add(entry["key"])
        registry_keys.update(alias for alias in entry.get("aliases", []) if alias)
    aliases = {entry.get("condition", "") for entry in registry.get("blackboardConditionAliases", [])}

    enum_names = ["ActorStateTag", "BlackboardComparisonType", "AbilityAttackCategory", "CombatIntent", "EnemyTransitionStateType", "FlyingEnemyTransitionStateType", "EnemyActionIntent", "EnemyActionStyle"]
    enums = {name: enum_values(project, name) for name in enum_names}
    return Catalog(set(condition_map.values()), set(action_map.values()), set(select_map.values()), scopes, blackboard_fields, numeric_references, registry_keys, aliases, enums)


@dataclass
class Report:
    path: Path
    errors: list[str] = field(default_factory=list)
    warnings: list[str] = field(default_factory=list)

    def error(self, location: str, message: str) -> None:
        self.errors.append(f"{location}: {message}")

    def warn(self, location: str, message: str) -> None:
        self.warnings.append(f"{location}: {message}")


def unknown_fields(report: Report, location: str, value: Any, allowed: set[str]) -> None:
    if not isinstance(value, dict):
        report.error(location, "object여야 합니다.")
        return
    unknown = sorted(set(value) - allowed)
    if unknown:
        report.error(location, f"지원하지 않는 필드: {', '.join(unknown)}")


def require_list(report: Report, location: str, value: Any) -> list[Any]:
    if value is None:
        return []
    if not isinstance(value, list):
        report.error(location, "배열이어야 합니다.")
        return []
    return value


def number(value: Any) -> bool:
    return isinstance(value, (int, float)) and not isinstance(value, bool)


def numeric_string(value: Any) -> bool:
    if not isinstance(value, str) or not value.strip():
        return False
    try:
        float(value)
        return True
    except ValueError:
        return False


def validate_scope(report: Report, location: str, catalog: Catalog, kind: str, name: str, actor_kind: str) -> None:
    scope = catalog.scopes.get((kind, name))
    if scope is None:
        report.error(location, f"현재 NodeFactory에서 scope를 찾을 수 없습니다: {name}")
    elif actor_kind == "Flying" and scope == "GroundOnly":
        report.error(location, f"actorKind=Flying에 지상 전용 {name}을 사용할 수 없습니다.")
    elif actor_kind == "Ground" and scope == "FlyingOnly":
        report.error(location, f"actorKind=Ground에 비행 전용 {name}을 사용할 수 없습니다.")


def validate_condition(report: Report, location: str, value: Any, catalog: Catalog, actor_kind: str) -> None:
    unknown_fields(report, location, value, CONDITION_FIELDS)
    if not isinstance(value, dict):
        return
    name = value.get("condition")
    if not isinstance(name, str) or not name:
        report.error(location, "condition이 필요합니다.")
        return
    if name not in catalog.conditions:
        report.error(location, f"알 수 없는 condition: {name}")
        return
    validate_scope(report, location, catalog, "Conditions", name, actor_kind)
    if "invert" in value and not isinstance(value["invert"], bool):
        report.error(location, "invert는 bool이어야 합니다.")
    if name in ALIAS_CONDITIONS and name not in catalog.aliases:
        report.error(location, f"BehaviorTreeEditorRegistry에 condition alias가 없습니다: {name}")
    if name in VALUE_REQUIRED and not isinstance(value.get("value"), str):
        report.error(location, f"{name}에는 문자열 value가 필요합니다.")
    if name in NUMERIC_VALUE_REQUIRED and not numeric_string(value.get("value")):
        report.error(location, f"{name}.value는 invariant 숫자 문자열이어야 합니다.")
    if name in {"DistanceLessOrEqual", "DistanceGreater"}:
        raw = value.get("value")
        if not numeric_string(raw) and raw not in catalog.numeric_references:
            report.error(location, f"거리 value가 숫자 또는 알려진 숫자 필드가 아닙니다: {raw}")
    if name == "HasStateTag" and value.get("value") not in catalog.enums["ActorStateTag"]:
        report.error(location, f"알 수 없는 ActorStateTag: {value.get('value')}")
    if name == "BlackboardCompare":
        key = value.get("key")
        if not isinstance(key, str) or not key:
            report.error(location, "BlackboardCompare.key가 필요합니다.")
        elif key not in catalog.registry_keys:
            report.warn(location, f"registry에서 Blackboard key를 찾지 못했습니다: {key}")
        op = value.get("op", "Equal") or "Equal"
        if op not in catalog.enums["BlackboardComparisonType"]:
            report.error(location, f"알 수 없는 BlackboardComparisonType: {op}")
        if not value.get("value") and not value.get("valueKey"):
            report.error(location, "BlackboardCompare에는 value 또는 valueKey가 필요합니다.")
        value_key = value.get("valueKey")
        if value_key and value_key not in catalog.registry_keys:
            report.warn(location, f"registry에서 valueKey를 찾지 못했습니다: {value_key}")
    if name == "SelectedIntent" and value.get("value") not in catalog.enums["CombatIntent"]:
        report.error(location, f"알 수 없는 CombatIntent: {value.get('value')}")
    if name == "CanActivateAbility":
        category = value.get("attackCategory")
        if category not in catalog.enums["AbilityAttackCategory"] or category == "None":
            report.error(location, f"CanActivateAbility.attackCategory가 올바르지 않습니다: {category}")


def validate_action(report: Report, location: str, value: Any, catalog: Catalog, actor_kind: str, choice: bool = False) -> None:
    unknown_fields(report, location, value, CHOICE_FIELDS if choice else ACTION_FIELDS)
    if not isinstance(value, dict):
        return
    name = value.get("action")
    if not isinstance(name, str) or not name:
        report.error(location, "action이 필요합니다.")
        return
    if name not in catalog.actions:
        report.error(location, f"알 수 없는 action: {name}")
        return
    validate_scope(report, location, catalog, "Actions", name, actor_kind)
    if name == "Transition" and value.get("state") not in catalog.enums["EnemyTransitionStateType"]:
        report.error(location, f"알 수 없는 EnemyTransitionStateType: {value.get('state')}")
    if name == "FlyingTransition" and value.get("state") not in catalog.enums["FlyingEnemyTransitionStateType"]:
        report.error(location, f"알 수 없는 FlyingEnemyTransitionStateType: {value.get('state')}")
    if name == "RequestAction":
        if value.get("intent") not in catalog.enums["EnemyActionIntent"]:
            report.error(location, f"알 수 없는 EnemyActionIntent: {value.get('intent')}")
        style = value.get("style")
        if style and style not in catalog.enums["EnemyActionStyle"]:
            report.error(location, f"알 수 없는 EnemyActionStyle: {style}")
    category = value.get("attackCategory")
    if category and category not in catalog.enums["AbilityAttackCategory"]:
        report.error(location, f"알 수 없는 AbilityAttackCategory: {category}")
    if name == "IssueAbilityTrigger" and (not category or category == "None"):
        report.error(location, "IssueAbilityTrigger에는 None이 아닌 attackCategory가 필요합니다.")
    if name == "Wait":
        if choice:
            report.error(location, "choice DTO에는 duration이 없어 Wait를 사용할 수 없습니다.")
        duration = value.get("duration", 0)
        if not number(duration) or duration < 0:
            report.error(location, "Wait.duration은 0 이상의 숫자여야 합니다.")
    cooldown_duration = value.get("cooldownDuration", 0)
    if not number(cooldown_duration) or cooldown_duration < 0:
        report.error(location, "cooldownDuration은 0 이상의 숫자여야 합니다.")
    if choice:
        weight_key = value.get("weightKey")
        weight = value.get("weight", 1)
        if weight_key:
            if weight_key not in catalog.numeric_references:
                report.error(location, f"알 수 없는 weightKey 숫자 필드: {weight_key}")
        elif not number(weight) or weight < 0:
            report.error(location, "weight는 0 이상의 숫자여야 합니다.")


def descending(values: Iterable[Any]) -> bool:
    numbers = list(values)
    return all(number(value) for value in numbers) and all(a >= b for a, b in zip(numbers, numbers[1:]))


def validate_rule(report: Report, location: str, rule: Any, catalog: Catalog, actor_kind: str) -> None:
    unknown_fields(report, location, rule, RULE_FIELDS)
    if not isinstance(rule, dict):
        return
    if not isinstance(rule.get("name"), str) or not rule["name"].strip():
        report.error(location, "rule name이 필요합니다.")
    if not isinstance(rule.get("priority"), int) or isinstance(rule.get("priority"), bool):
        report.error(location, "priority는 정수여야 합니다.")
    conditions = require_list(report, f"{location}.when", rule.get("when", []))
    actions = require_list(report, f"{location}.do", rule.get("do", []))
    choices = require_list(report, f"{location}.choices", rule.get("choices", []))
    for index, condition in enumerate(conditions):
        validate_condition(report, f"{location}.when[{index}]", condition, catalog, actor_kind)
    select = rule.get("select", "") or ""
    if select:
        if select not in catalog.selects:
            report.error(location, f"알 수 없는 select: {select}")
        if select == "WeightedRandom":
            if actions:
                report.error(location, "WeightedRandom rule의 do는 importer에서 무시되므로 비워야 합니다.")
            if not choices:
                report.error(location, "WeightedRandom rule에는 choices가 필요합니다.")
    else:
        if choices:
            report.error(location, "select가 없으면 choices가 무시되므로 비워야 합니다.")
        if not actions:
            report.error(location, "일반 rule에는 do action이 하나 이상 필요합니다.")
    for index, action in enumerate(actions):
        validate_action(report, f"{location}.do[{index}]", action, catalog, actor_kind)
    positive_choice = False
    for index, choice_value in enumerate(choices):
        validate_action(report, f"{location}.choices[{index}]", choice_value, catalog, actor_kind, choice=True)
        if isinstance(choice_value, dict) and (choice_value.get("weightKey") or (number(choice_value.get("weight", 1)) and choice_value.get("weight", 1) > 0)):
            positive_choice = True
    if select == "WeightedRandom" and choices and not positive_choice:
        report.error(location, "WeightedRandom choice의 전체 가중치가 0입니다.")


def validate_document(path: Path, project: Path, catalog: Catalog) -> tuple[Report, str | None]:
    report = Report(path)
    try:
        data = json.loads(path.read_text(encoding="utf-8-sig"), object_pairs_hook=no_duplicate_object)
    except (OSError, UnicodeError, json.JSONDecodeError, DuplicateKeyError) as exc:
        report.error("$", f"JSON을 읽을 수 없습니다: {exc}")
        return report, None
    unknown_fields(report, "$", data, TOP_FIELDS)
    if not isinstance(data, dict):
        return report, None
    if data.get("schemaVersion") != 1:
        report.error("$.schemaVersion", "현재 지원 값은 1입니다.")
    identifier = data.get("id")
    if not isinstance(identifier, str) or not identifier.strip():
        report.error("$.id", "비어 있지 않은 문자열이 필요합니다.")
        identifier = None
    elif identifier != identifier.strip() or not re.fullmatch(r"[A-Za-z0-9_.-]+", identifier):
        report.error("$.id", "영문자, 숫자, '_', '-', '.'만 사용하고 앞뒤 공백을 두지 마세요.")
    actor_kind = data.get("actorKind")
    if actor_kind not in {"Ground", "Flying"}:
        report.error("$.actorKind", "Ground 또는 Flying이어야 합니다.")
        actor_kind = "Ground"
    source = data.get("sourceBehaviorSo", "")
    if source:
        if not isinstance(source, str) or not source.startswith("Assets/") or not source.endswith(".asset"):
            report.error("$.sourceBehaviorSo", "Assets/로 시작하는 .asset 경로여야 합니다.")
        elif not (project / Path(source)).is_file():
            report.error("$.sourceBehaviorSo", f"에셋을 찾을 수 없습니다: {source}")
    blackboard = data.get("blackboard", {})
    unknown_fields(report, "$.blackboard", blackboard, catalog.blackboard_fields)
    if isinstance(blackboard, dict):
        for name in PROBABILITY_FIELDS & set(blackboard):
            value = blackboard[name]
            if not number(value) or not 0 <= value <= 1:
                report.warn(f"$.blackboard.{name}", "0..1 범위를 벗어나 importer에서 clamp됩니다.")
        for name in NON_NEGATIVE_FIELDS & set(blackboard):
            value = blackboard[name]
            if not number(value) or value < 0:
                report.warn(f"$.blackboard.{name}", "음수 값은 importer에서 0으로 보정될 수 있습니다.")
    groups = require_list(report, "$.groups", data.get("groups", []))
    root_rules = require_list(report, "$.rules", data.get("rules", []))
    if not groups and not root_rules:
        report.error("$", "groups 또는 rules가 하나 이상 필요합니다.")
    if groups and root_rules:
        report.error("$", "groups가 있으면 root rules가 무시되므로 rules를 비워야 합니다.")
    if groups and not descending(group.get("priority") if isinstance(group, dict) else None for group in groups):
        report.warn("$.groups", "priority 순서가 내림차순이 아닙니다.")
    for group_index, group in enumerate(groups):
        group_loc = f"$.groups[{group_index}]"
        unknown_fields(report, group_loc, group, GROUP_FIELDS)
        if not isinstance(group, dict):
            continue
        if not isinstance(group.get("name"), str) or not group["name"].strip():
            report.error(group_loc, "group name이 필요합니다.")
        if not isinstance(group.get("priority"), int) or isinstance(group.get("priority"), bool):
            report.error(group_loc, "priority는 정수여야 합니다.")
        group_conditions = require_list(report, f"{group_loc}.when", group.get("when", []))
        for index, condition in enumerate(group_conditions):
            validate_condition(report, f"{group_loc}.when[{index}]", condition, catalog, actor_kind)
        rules = require_list(report, f"{group_loc}.rules", group.get("rules", []))
        if not rules:
            report.error(group_loc, "group rules가 비어 있습니다.")
        if rules and not descending(rule.get("priority") if isinstance(rule, dict) else None for rule in rules):
            report.warn(f"{group_loc}.rules", "priority 순서가 내림차순이 아닙니다.")
        for rule_index, rule in enumerate(rules):
            validate_rule(report, f"{group_loc}.rules[{rule_index}]", rule, catalog, actor_kind)
    if root_rules and not descending(rule.get("priority") if isinstance(rule, dict) else None for rule in root_rules):
        report.warn("$.rules", "priority 순서가 내림차순이 아닙니다.")
    for rule_index, rule in enumerate(root_rules):
        validate_rule(report, f"$.rules[{rule_index}]", rule, catalog, actor_kind)
    return report, identifier


def collect_paths(arguments: list[str], project: Path) -> list[Path]:
    candidates = [Path(item) for item in arguments] if arguments else [project / SOURCE_REL]
    paths: list[Path] = []
    for candidate in candidates:
        resolved = candidate if candidate.is_absolute() else project / candidate
        if resolved.is_dir():
            paths.extend(resolved.rglob("*.json"))
        elif resolved.is_file():
            paths.append(resolved)
        else:
            raise FileNotFoundError(f"검증 대상을 찾을 수 없습니다: {candidate}")
    return sorted(set(path.resolve() for path in paths), key=lambda item: str(item).lower())


def main() -> int:
    parser = argparse.ArgumentParser(description="UPlayground Monster Behavior Rules JSON 정적 검증")
    parser.add_argument("paths", nargs="*", help="JSON 파일 또는 폴더. 생략하면 SourceJson 전체")
    parser.add_argument("--project-root", help="UPlayground 프로젝트 루트")
    parser.add_argument("--strict", action="store_true", help="warning도 실패로 처리")
    args = parser.parse_args()
    try:
        project = Path(args.project_root).resolve() if args.project_root else find_project_root(Path.cwd())
        catalog = load_catalog(project)
        paths = collect_paths(args.paths, project)
    except (OSError, ValueError) as exc:
        print(f"설정 오류: {exc}", file=sys.stderr)
        return 2
    if not paths:
        print("검증할 JSON이 없습니다.", file=sys.stderr)
        return 2
    reports: list[Report] = []
    ids: dict[str, Path] = {}
    for path in paths:
        report, identifier = validate_document(path, project, catalog)
        if identifier:
            collision_key = identifier.casefold()
            if collision_key in ids:
                report.error("$.id", f"다른 JSON과 생성 에셋 id가 중복됩니다: {ids[collision_key]}")
            else:
                ids[collision_key] = path
        reports.append(report)
    for report in reports:
        try:
            label = report.path.relative_to(project)
        except ValueError:
            label = report.path
        status = "FAIL" if report.errors or (args.strict and report.warnings) else "OK"
        print(f"[{status}] {label}")
        for message in report.errors:
            print(f"  ERROR: {message}")
        for message in report.warnings:
            print(f"  WARN:  {message}")
    error_count = sum(len(report.errors) for report in reports)
    warning_count = sum(len(report.warnings) for report in reports)
    print(f"검증 완료: {len(reports)}개 파일, 오류 {error_count}, 경고 {warning_count}")
    return 1 if error_count or (args.strict and warning_count) else 0


if __name__ == "__main__":
    raise SystemExit(main())
