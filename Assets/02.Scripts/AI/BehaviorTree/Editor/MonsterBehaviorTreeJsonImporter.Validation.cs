#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UPlayGround.Data.EnumType;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public static partial class MonsterBehaviorTreeJsonImporter
    {
        private enum ActorKind { Ground, Flying }

        private static readonly HashSet<string> GroundOnlyConditions = new()
        {
            "CanUseSkill"
        };

        private static readonly HashSet<string> FlyingOnlyConditions = new()
        {
            "IsFlyingAirState", "IsFlyingGroundCombatState", "IsAirAttackLimitReached",
            "ShouldFlyingTakeOff", "FlyingCanUseSkill", "HasDiveSkillAvailable", "RollDiveChance"
        };

        private static readonly HashSet<string> GroundOnlyActions = new()
        {
            "PatrolOrIdle", "Transition", "RequestAttackSlot", "ExecuteAttack"
        };

        private static readonly HashSet<string> FlyingOnlyActions = new()
        {
            "FlyingTransition", "FlyingPatrolOrIdle", "ResetFlyingCounters",
            "ResetFlyingAirCounters", "DescendFlying", "RequestFlyingAttackSlot", "SelectFlyingDiveSkill"
        };

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
            if (string.Equals(raw, "Flying", StringComparison.OrdinalIgnoreCase))
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
            var known = condition.condition is "HasTarget" or "IsBlockedEnemyState" or "IsEnemyPhase" or "DistanceLessOrEqual"
                or "DistanceGreater" or "ActionDelayElapsed" or "CanUseSkill" or "IsPlayerAttacking"
                or "IsPlayerGuarding" or "IsPlayerStaggered" or "IsPlayerRecovering" or "IsPlayerDodgingFrequently"
                or "IsSelfLowHealth" or "HasAttackSlot" or "CooldownReady" or "RecentlyHitByPlayer"
                or "WasLastHitHeavy" or "IsPoiseBroken" or "RecentHitCountGreaterOrEqual"
                or "CanIgnoreLightHit" or "CanRevengeAfterHit"
                or "ConsecutiveAttackCountLessThan" or "ConsecutiveAttackCountGreaterOrEqual"
                // ── 비행 전용 ──
                or "IsCurrentState" or "IsFlyingAirState" or "IsFlyingGroundCombatState"
                or "IsAirAttackLimitReached" or "ShouldFlyingTakeOff" or "FlyingCanUseSkill"
                or "HasDiveSkillAvailable" or "RollDiveChance";

            if (!known)
                throw new InvalidDataException($"{ruleName}: 알 수 없는 condition입니다. {condition.condition}");

            if (condition.condition == "IsCurrentState" && string.IsNullOrWhiteSpace(condition.value))
                throw new InvalidDataException($"{ruleName}: IsCurrentState는 value(상태 이름)가 필요합니다.");

            if (condition.condition == "IsEnemyPhase" && string.IsNullOrWhiteSpace(condition.value))
                throw new InvalidDataException($"{ruleName}: IsEnemyPhase는 value(페이즈 이름 또는 인덱스)가 필요합니다.");

            if (actorKind == ActorKind.Flying && GroundOnlyConditions.Contains(condition.condition))
                throw new InvalidDataException($"{ruleName}: 지상 전용 condition '{condition.condition}'은 actorKind=Flying에서 사용할 수 없습니다. 비행 대응 노드(예: FlyingCanUseSkill)로 교체하세요.");

            if (actorKind == ActorKind.Ground && FlyingOnlyConditions.Contains(condition.condition))
                throw new InvalidDataException($"{ruleName}: 비행 전용 condition '{condition.condition}'은 actorKind=Ground에서 사용할 수 없습니다.");
        }

        private static void ValidateAction(MonsterBehaviorActionJson action, string ruleName, ActorKind actorKind)
        {
            var known = action.action is "KeepCurrentState" or "PatrolOrIdle" or "Transition"
                or "RequestAttackSlot" or "ExecuteAttack" or "Wait"
                // ── 비행 전용 ──
                or "FlyingTransition" or "FlyingPatrolOrIdle" or "ResetFlyingCounters"
                or "ResetFlyingAirCounters" or "DescendFlying" or "RequestFlyingAttackSlot"
                or "SelectFlyingDiveSkill";

            if (!known)
                throw new InvalidDataException($"{ruleName}: 알 수 없는 action입니다. {action.action}");

            if (action.action == "Transition" && !Enum.TryParse<EnemyTransitionStateType>(action.state, out _))
                throw new InvalidDataException($"{ruleName}: 알 수 없는 EnemyTransitionStateType입니다. {action.state}");

            if (action.action == "FlyingTransition" && !Enum.TryParse<FlyingEnemyTransitionStateType>(action.state, out _))
                throw new InvalidDataException($"{ruleName}: 알 수 없는 FlyingEnemyTransitionStateType입니다. {action.state}");

            if (!string.IsNullOrWhiteSpace(action.attackCategory)
                && !Enum.TryParse<EnemyAttackCategory>(action.attackCategory, true, out _))
                throw new InvalidDataException($"{ruleName}: 알 수 없는 EnemyAttackCategory입니다. {action.attackCategory}");

            if (actorKind == ActorKind.Flying && GroundOnlyActions.Contains(action.action))
                throw new InvalidDataException($"{ruleName}: 지상 전용 action '{action.action}'은 actorKind=Flying에서 사용할 수 없습니다. 비행 대응 액션(예: FlyingTransition / FlyingPatrolOrIdle / RequestFlyingAttackSlot)으로 교체하세요.");

            if (actorKind == ActorKind.Ground && FlyingOnlyActions.Contains(action.action))
                throw new InvalidDataException($"{ruleName}: 비행 전용 action '{action.action}'은 actorKind=Ground에서 사용할 수 없습니다.");
        }

        private static void ValidateChoice(MonsterBehaviorChoiceJson choice, string ruleName, ActorKind actorKind)
        {
            ValidateAction(new MonsterBehaviorActionJson { action = choice.action, state = choice.state, attackCategory = choice.attackCategory }, ruleName, actorKind);
        }
    }
}
#endif
