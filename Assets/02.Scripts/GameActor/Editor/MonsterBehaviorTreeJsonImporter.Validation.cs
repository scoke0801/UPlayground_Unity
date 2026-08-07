#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UPlayGround.Data.EnumType;
using UPlayGround.State;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public static partial class MonsterBehaviorTreeJsonImporter
    {
        private enum ActorKind { Ground, Flying }

        private static void Validate(MonsterBehaviorTreeJson data, string sourcePath)
        {
            if (data == null)
                throw new InvalidDataException($"Monster Behavior Json을 읽을 수 없습니다: {sourcePath}");

            if (data.schemaVersion != SupportedSchemaVersion)
                throw new InvalidDataException($"지원하지 않는 schemaVersion입니다: {data.schemaVersion}");

            if (string.IsNullOrWhiteSpace(data.id))
                throw new InvalidDataException("id가 비어 있습니다.");

            var hasGroups = data.groups != null && data.groups.Count > 0;
            var hasRules = data.rules != null && data.rules.Count > 0;
            if (!hasGroups && !hasRules)
                throw new InvalidDataException($"{data.id}: groups 또는 rules가 필요합니다.");

            var actorKind = ResolveActorKind(data.actorKind);

            if (hasGroups)
            {
                foreach (var group in data.groups)
                {
                    if (string.IsNullOrWhiteSpace(group.name))
                        throw new InvalidDataException($"{data.id}: name이 비어 있는 group이 있습니다.");

                    if (group.rules == null || group.rules.Count == 0)
                        throw new InvalidDataException($"{data.id}: group '{group.name}'의 rules가 비어 있습니다.");

                    foreach (var condition in group.when ?? new List<MonsterBehaviorConditionJson>())
                        ValidateCondition(condition, group.name, actorKind);
                }
            }

            foreach (var rule in EnumerateJsonRules(data))
            {
                if (string.IsNullOrWhiteSpace(rule.name))
                    throw new InvalidDataException($"{data.id}: name이 비어 있는 rule이 있습니다.");

                foreach (var condition in rule.when ?? new List<MonsterBehaviorConditionJson>())
                    ValidateCondition(condition, rule.name, actorKind);

                foreach (var action in rule.@do ?? new List<MonsterBehaviorActionJson>())
                    ValidateAction(action, rule.name, actorKind);

                foreach (var choice in rule.choices ?? new List<MonsterBehaviorChoiceJson>())
                    ValidateChoice(choice, rule.name, actorKind);
            }
        }

        private static ActorKind ResolveActorKind(string raw)
        {
            if (string.Equals(raw, MonsterBehaviorJsonNodeKeys.ActorKinds.Flying, StringComparison.OrdinalIgnoreCase))
                return ActorKind.Flying;
            return ActorKind.Ground;
        }

        private static IEnumerable<MonsterBehaviorRuleJson> EnumerateJsonRules(MonsterBehaviorTreeJson data)
        {
            if (data.groups != null && data.groups.Count > 0)
            {
                foreach (var group in data.groups)
                {
                    foreach (var rule in group.rules ?? new List<MonsterBehaviorRuleJson>())
                        yield return rule;
                }

                yield break;
            }

            foreach (var rule in data.rules ?? new List<MonsterBehaviorRuleJson>())
                yield return rule;
        }

        private static void ValidateCondition(MonsterBehaviorConditionJson condition, string ruleName, ActorKind actorKind)
        {
            if (condition == null || !ConditionNodeDefinitions.TryGetValue(condition.condition, out var definition))
                throw new InvalidDataException($"{ruleName}: 알 수 없는 condition입니다. {condition?.condition}");

            if (condition.condition == MonsterBehaviorJsonNodeKeys.Conditions.HasStateTag
                && !Enum.TryParse<ActorStateTag>(condition.value, true, out _))
                throw new InvalidDataException($"{ruleName}: 알 수 없는 ActorStateTag입니다. {condition.value}");

            if (condition.condition == MonsterBehaviorJsonNodeKeys.Conditions.BlackboardCompare)
            {
                if (string.IsNullOrWhiteSpace(condition.key))
                    throw new InvalidDataException($"{ruleName}: {MonsterBehaviorJsonNodeKeys.Conditions.BlackboardCompare}는 key가 필요합니다.");

                if (!string.IsNullOrWhiteSpace(condition.op)
                    && !Enum.TryParse<BlackboardComparisonType>(condition.op, true, out _))
                    throw new InvalidDataException($"{ruleName}: 알 수 없는 BlackboardComparisonType입니다. {condition.op}");

                if (string.IsNullOrWhiteSpace(condition.value)
                    && string.IsNullOrWhiteSpace(condition.valueKey))
                    throw new InvalidDataException($"{ruleName}: {MonsterBehaviorJsonNodeKeys.Conditions.BlackboardCompare}는 value 또는 valueKey가 필요합니다.");
            }

            if (condition.condition == MonsterBehaviorJsonNodeKeys.Conditions.IsCurrentState && string.IsNullOrWhiteSpace(condition.value))
                throw new InvalidDataException($"{ruleName}: {MonsterBehaviorJsonNodeKeys.Conditions.IsCurrentState}는 value(상태 이름)가 필요합니다.");

            if (condition.condition == MonsterBehaviorJsonNodeKeys.Conditions.IsEnemyPhase && string.IsNullOrWhiteSpace(condition.value))
                throw new InvalidDataException($"{ruleName}: {MonsterBehaviorJsonNodeKeys.Conditions.IsEnemyPhase}는 value(페이즈 이름 또는 인덱스)가 필요합니다.");

            if (condition.condition == MonsterBehaviorJsonNodeKeys.Conditions.SelectedIntent && string.IsNullOrWhiteSpace(condition.value))
                throw new InvalidDataException($"{ruleName}: {MonsterBehaviorJsonNodeKeys.Conditions.SelectedIntent}는 value(CombatIntent 이름)가 필요합니다.");

            if (condition.condition
                == MonsterBehaviorJsonNodeKeys.Conditions.CanActivateAbility)
            {
                ParseAttackCategory(
                    condition.attackCategory,
                    $"{ruleName}: CanActivateAbility",
                    allowNone: false,
                    allowAny: false);
                ParseAbilityRole(
                    condition.abilityRole,
                    $"{ruleName}: CanActivateAbility");
            }

            ValidateActorScope(definition.Scope, actorKind, ruleName, "condition", condition.condition);
        }

        private static void ValidateAction(MonsterBehaviorActionJson action, string ruleName, ActorKind actorKind)
        {
            if (action == null || !ActionNodeDefinitions.TryGetValue(action.action, out var definition))
                throw new InvalidDataException($"{ruleName}: 알 수 없는 action입니다. {action?.action}");

            if (action.action == MonsterBehaviorJsonNodeKeys.Actions.Transition && !Enum.TryParse<EnemyTransitionStateType>(action.state, out _))
                throw new InvalidDataException($"{ruleName}: 알 수 없는 EnemyTransitionStateType입니다. {action.state}");

            if (action.action == MonsterBehaviorJsonNodeKeys.Actions.FlyingTransition && !Enum.TryParse<FlyingEnemyTransitionStateType>(action.state, out _))
                throw new InvalidDataException($"{ruleName}: 알 수 없는 FlyingEnemyTransitionStateType입니다. {action.state}");

            if (action.action == MonsterBehaviorJsonNodeKeys.Actions.RequestAction)
            {
                if (!Enum.TryParse<EnemyActionIntent>(action.intent, true, out _))
                    throw new InvalidDataException($"{ruleName}: 알 수 없는 EnemyActionIntent입니다. {action.intent}");

                if (!string.IsNullOrWhiteSpace(action.style)
                    && !Enum.TryParse<EnemyActionStyle>(action.style, true, out _))
                    throw new InvalidDataException($"{ruleName}: 알 수 없는 EnemyActionStyle입니다. {action.style}");
            }

            ParseAttackCategory(
                action.attackCategory,
                $"{ruleName}: {action.action}",
                allowNone: action.action
                    != MonsterBehaviorJsonNodeKeys.Actions.IssueAbilityTrigger,
                allowAny: false);
            ParseAbilityRole(
                action.abilityRole,
                $"{ruleName}: {action.action}");

            if (actorKind == ActorKind.Flying
                && !string.IsNullOrWhiteSpace(action.abilityRole))
            {
                throw new InvalidDataException(
                    $"{ruleName}: abilityRole 공격 필터는 현재 지상형 BT에서만 지원합니다.");
            }

            ValidateActorScope(definition.Scope, actorKind, ruleName, "action", action.action);
        }

        private static void ValidateActorScope(
            JsonNodeActorScope scope,
            ActorKind actorKind,
            string ruleName,
            string nodeKind,
            string nodeName)
        {
            if (actorKind == ActorKind.Flying && scope == JsonNodeActorScope.GroundOnly)
                throw new InvalidDataException($"{ruleName}: 지상 전용 {nodeKind} '{nodeName}'은 actorKind=Flying에서 사용할 수 없습니다.");

            if (actorKind == ActorKind.Ground && scope == JsonNodeActorScope.FlyingOnly)
                throw new InvalidDataException($"{ruleName}: 비행 전용 {nodeKind} '{nodeName}'은 actorKind=Ground에서 사용할 수 없습니다.");
        }

        private static void ValidateChoice(MonsterBehaviorChoiceJson choice, string ruleName, ActorKind actorKind)
        {
            ValidateAction(
                new MonsterBehaviorActionJson
                {
                    action = choice.action,
                    intent = choice.intent,
                    style = choice.style,
                    state = choice.state,
                    attackCategory = choice.attackCategory,
                    abilityRole = choice.abilityRole,
                    cooldownId = choice.cooldownId,
                    cooldownDuration = choice.cooldownDuration
                },
                ruleName,
                actorKind);
        }
    }
}
#endif
