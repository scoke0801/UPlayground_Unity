#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public static partial class MonsterBehaviorTreeJsonImporter
    {
        private static void AddJsonDefinedChildren(
            BehaviorTreeAsset tree,
            SelectorNode root,
            MonsterBehaviorTreeJson data,
            EnemyBehaviorSO sourceBehavior)
        {
            if (data.groups != null && data.groups.Count > 0)
            {
                var orderedGroups = data.groups
                    .OrderByDescending(group => group.priority)
                    .ThenBy(group => data.groups.IndexOf(group))
                    .ToList();

                var row = 0;
                foreach (var group in orderedGroups)
                {
                    var groupNode = CreateGroupNode(tree, group, sourceBehavior, data.blackboard, row);
                    var groupSelector = GetGroupRuleParent(groupNode);
                    if (groupSelector == null)
                        throw new System.IO.InvalidDataException($"{data.id}: group '{group.name}'의 rule parent를 생성할 수 없습니다.");

                    var orderedRules = group.rules
                        .OrderByDescending(rule => rule.priority)
                        .ThenBy(rule => group.rules.IndexOf(rule))
                        .ToList();

                    foreach (var rule in orderedRules)
                    {
                        var child = CreateRuleNode(tree, rule, sourceBehavior, data.blackboard, row++);
                        if (child != null)
                            groupSelector.Children.Add(child);
                    }

                    if (groupSelector.Children.Count > 0)
                        root.Children.Add(groupNode);
                }

                return;
            }

            var flatRules = data.rules ?? new List<MonsterBehaviorRuleJson>();
            var orderedFlatRules = flatRules
                .OrderByDescending(rule => rule.priority)
                .ThenBy(rule => flatRules.IndexOf(rule))
                .ToList();

            for (var i = 0; i < orderedFlatRules.Count; i++)
            {
                var child = CreateRuleNode(tree, orderedFlatRules[i], sourceBehavior, data.blackboard, i);
                if (child != null)
                    root.Children.Add(child);
            }
        }

        private static BTNode CreateRuleNode(
            BehaviorTreeAsset tree,
            MonsterBehaviorRuleJson rule,
            EnemyBehaviorSO sourceBehavior,
            MonsterBehaviorBlackboardJson blackboard,
            int index)
        {
            var sequence = CreateNode<SequenceNode>(tree, rule.name, new Vector2(260f, index * 180f));

            foreach (var condition in rule.when ?? new List<MonsterBehaviorConditionJson>())
            {
                var conditionNode = CreateConditionNode(tree, condition, sourceBehavior, blackboard, index);
                if (conditionNode != null)
                    sequence.Children.Add(conditionNode);
            }

            if (rule.select == "WeightedRandom")
            {
                var selector = CreateNode<WeightedRandomSelectorNode>(tree, rule.name + " Weighted", new Vector2(560f, index * 180f));
                for (var i = 0; i < rule.choices.Count; i++)
                {
                    var choice = rule.choices[i];
                    var action = CreateChoiceActionNode(tree, choice, index, i);
                    if (action == null)
                        continue;

                    selector.Children.Add(action);
                    selector.SetWeight(selector.Children.Count - 1, ResolveWeight(choice, sourceBehavior, blackboard));
                }

                sequence.Children.Add(selector);
            }
            else
            {
                foreach (var action in rule.@do ?? new List<MonsterBehaviorActionJson>())
                {
                    var actionNode = CreateActionNode(tree, action, index);
                    if (actionNode != null)
                        sequence.Children.Add(actionNode);
                }
            }

            return sequence.Children.Count == 0 ? null : sequence;
        }

        private static BTNode CreateGroupNode(
            BehaviorTreeAsset tree,
            MonsterBehaviorRuleGroupJson group,
            EnemyBehaviorSO sourceBehavior,
            MonsterBehaviorBlackboardJson blackboard,
            int row)
        {
            if (group.when == null || group.when.Count == 0)
                return CreateNode<SelectorNode>(tree, group.name, Vector2.zero);

            var sequence = CreateNode<SequenceNode>(tree, group.name, Vector2.zero);
            foreach (var condition in group.when)
            {
                var conditionNode = CreateConditionNode(tree, condition, sourceBehavior, blackboard, row);
                if (conditionNode != null)
                    sequence.Children.Add(conditionNode);
            }

            sequence.Children.Add(CreateNode<SelectorNode>(tree, group.name + " Rules", Vector2.zero));
            return sequence;
        }

        private static BTCompositeNode GetGroupRuleParent(BTNode groupNode)
        {
            if (groupNode is SequenceNode sequence && sequence.Children.Count > 0)
                return sequence.Children[^1] as BTCompositeNode;

            return groupNode as BTCompositeNode;
        }

        private static BTNode CreateConditionNode(
            BehaviorTreeAsset tree,
            MonsterBehaviorConditionJson condition,
            EnemyBehaviorSO sourceBehavior,
            MonsterBehaviorBlackboardJson blackboard,
            int row)
        {
            BTNode node = condition.condition switch
            {
                "HasTarget" => CreateHasTargetNode(tree, !condition.invert, row),
                "IsBlockedEnemyState" => CreateNode<IsBlockedEnemyStateNode>(tree, "Is Blocked Enemy State", new Vector2(520f, row * 180f)),
                "IsEnemyPhase" => CreateEnemyPhaseNode(tree, condition.value, row),
                "DistanceLessOrEqual" => CreateRangeNode(tree, FloatComparisonType.LessOrEqual, condition.value, sourceBehavior, blackboard, row),
                "DistanceGreater" => CreateRangeNode(tree, FloatComparisonType.GreaterOrEqual, condition.value, sourceBehavior, blackboard, row),
                "ActionDelayElapsed" => CreateNode<HasEnemyActionDelayElapsedNode>(tree, "Action Delay Elapsed", new Vector2(520f, row * 180f)),
                "CanUseSkill" => CreateNode<CanUseEnemySkillNode>(tree, "Can Use Enemy Skill", new Vector2(520f, row * 180f)),
                "IsPlayerAttacking" => CreateBlackboardBoolNode(tree, EnemyBlackboardKeys.IsPlayerAttacking, !condition.invert, row),
                "IsPlayerGuarding" => CreateBlackboardBoolNode(tree, EnemyBlackboardKeys.IsPlayerGuarding, !condition.invert, row),
                "IsPlayerStaggered" => CreateBlackboardBoolNode(tree, EnemyBlackboardKeys.IsPlayerStaggered, !condition.invert, row),
                "IsPlayerRecovering" => CreateBlackboardBoolNode(tree, EnemyBlackboardKeys.IsPlayerRecovering, !condition.invert, row),
                "IsPlayerDodgingFrequently" => CreateBlackboardBoolNode(tree, EnemyBlackboardKeys.IsPlayerDodgingFrequently, !condition.invert, row),
                "RecentlyHitByPlayer" => CreateBlackboardBoolNode(tree, EnemyBlackboardKeys.RecentlyHitByPlayer, !condition.invert, row),
                "HasAttackSlot" => CreateNode<HasAttackSlotNode>(tree, "HasAttackSlot", new Vector2(520f, row * 180f)),
                "CooldownReady" => CreateCooldownReadyNode(tree, condition.value, row),
                "IsSelfLowHealth" => CreateSelfLowHealthNode(tree, condition.value, row),
                "WasLastHitHeavy" => CreateNode<WasLastHitHeavyNode>(tree, "WasLastHitHeavy", new Vector2(520f, row * 180f)),
                "IsPoiseBroken" => CreateNode<IsPoiseBrokenNode>(tree, "IsPoiseBroken", new Vector2(520f, row * 180f)),
                "RecentHitCountGreaterOrEqual" => CreateRecentHitCountNode(tree, condition.value, row),
                "ConsecutiveAttackCountLessThan" => CreateConsecutiveAttackCountNode(tree, condition.value, IntComparisonType.LessThan, row),
                "ConsecutiveAttackCountGreaterOrEqual" => CreateConsecutiveAttackCountNode(tree, condition.value, IntComparisonType.GreaterOrEqual, row),
                "CanIgnoreLightHit" => CreateNode<CanIgnoreLightHitNode>(tree, "CanIgnoreLightHit", new Vector2(520f, row * 180f)),
                "CanRevengeAfterHit" => CreateRevengeAfterHitNode(tree, condition.value, row),
                // ── 비행 전용 ──
                "IsCurrentState" => CreateIsCurrentStateNode(tree, condition.value, !condition.invert, row),
                "IsFlyingAirState" => CreateNode<IsFlyingAirStateNode>(tree, "Is Flying Air State", new Vector2(520f, row * 180f)),
                "IsFlyingGroundCombatState" => CreateNode<IsFlyingGroundCombatStateNode>(tree, "Is Flying Ground Combat State", new Vector2(520f, row * 180f)),
                "IsAirAttackLimitReached" => CreateNode<IsAirAttackLimitReachedNode>(tree, "Air Attack Limit Reached", new Vector2(520f, row * 180f)),
                "ShouldFlyingTakeOff" => CreateNode<ShouldFlyingTakeOffNode>(tree, "Should Flying Take Off", new Vector2(520f, row * 180f)),
                "FlyingCanUseSkill" => CreateNode<FlyingCanUseSkillNode>(tree, "Flying Can Use Skill", new Vector2(520f, row * 180f)),
                "HasDiveSkillAvailable" => CreateNode<HasDiveSkillAvailableNode>(tree, "Has Dive Skill Available", new Vector2(520f, row * 180f)),
                "RollDiveChance" => CreateNode<RollDiveChanceNode>(tree, "Roll Dive Chance", new Vector2(520f, row * 180f)),
                _ => null
            };

            return condition.invert && condition.condition is not ("HasTarget" or "IsPlayerAttacking" or "IsPlayerGuarding"
                       or "IsPlayerStaggered" or "IsPlayerRecovering" or "IsPlayerDodgingFrequently"
                       or "RecentlyHitByPlayer"
                       or "IsCurrentState")
                ? WrapInverter(tree, node, row)
                : node;
        }

        private static BTNode CreateActionNode(BehaviorTreeAsset tree, MonsterBehaviorActionJson action, int row)
        {
            return action.action switch
            {
                "KeepCurrentState" => CreateNode<KeepCurrentStateNode>(tree, "Keep Current State", new Vector2(820f, row * 180f)),
                "PatrolOrIdle" => CreatePatrolOrIdleNode(tree, row),
                "Transition" => CreateTransitionNode(tree, action.state, row, action.cooldownId, action.cooldownDuration),
                "RequestAttackSlot" => CreateNode<RequestEnemyAttackSlotNode>(tree, "Request Attack Slot", new Vector2(820f, row * 180f)),
                "ExecuteAttack" => CreateExecuteAttackNode(tree, action.attackCategory, row),
                "Wait" => CreateWaitNode(tree, action.duration, row),
                // ── 비행 전용 ──
                "FlyingTransition" => CreateFlyingTransitionNode(tree, action.state, row),
                "FlyingPatrolOrIdle" => CreateFlyingPatrolOrIdleNode(tree, row),
                "ResetFlyingCounters" => CreateNode<ResetFlyingCountersNode>(tree, "Reset Flying Counters", new Vector2(820f, row * 180f)),
                "ResetFlyingAirCounters" => CreateNode<ResetFlyingAirCountersNode>(tree, "Reset Flying Air Counters", new Vector2(820f, row * 180f)),
                "DescendFlying" => CreateNode<DescendFlyingNode>(tree, "Descend Flying", new Vector2(820f, row * 180f)),
                "RequestFlyingAttackSlot" => CreateNode<RequestFlyingAttackSlotNode>(tree, "Request Flying Attack Slot", new Vector2(820f, row * 180f)),
                "SelectFlyingDiveSkill" => CreateNode<SelectFlyingDiveSkillNode>(tree, "Select Flying Dive Skill", new Vector2(820f, row * 180f)),
                _ => null
            };
        }

        private static BTNode CreateChoiceActionNode(BehaviorTreeAsset tree, MonsterBehaviorChoiceJson choice, int row, int column)
        {
            return CreateActionNode(
                tree,
                new MonsterBehaviorActionJson
                {
                    action = choice.action,
                    state = choice.state,
                    attackCategory = choice.attackCategory,
                    cooldownId = choice.cooldownId,
                    cooldownDuration = choice.cooldownDuration
                },
                row + column);
        }

        private static ExecuteEnemyAttackNode CreateExecuteAttackNode(BehaviorTreeAsset tree, string attackCategory, int row)
        {
            var node = CreateNode<ExecuteEnemyAttackNode>(tree, string.IsNullOrWhiteSpace(attackCategory) ? "Execute Attack" : $"Execute Attack {attackCategory}", new Vector2(820f, row * 180f));
            if (Enum.TryParse<EnemyAttackCategory>(attackCategory, true, out var parsed))
                node.AttackCategory = parsed;
            return node;
        }

        private static HasTargetNode CreateHasTargetNode(BehaviorTreeAsset tree, bool expected, int row)
        {
            var node = CreateNode<HasTargetNode>(tree, expected ? "Has Target" : "Has No Target", new Vector2(520f, row * 180f));
            node.ExpectedValue = expected;
            return node;
        }

        private static IsTargetInRangeNode CreateRangeNode(
            BehaviorTreeAsset tree,
            FloatComparisonType comparison,
            string value,
            EnemyBehaviorSO sourceBehavior,
            MonsterBehaviorBlackboardJson blackboard,
            int row)
        {
            var node = CreateNode<IsTargetInRangeNode>(tree, comparison.ToString(), new Vector2(520f, row * 180f));
            node.Comparison = comparison;
            var resolved = ResolveFloat(value, sourceBehavior, blackboard, 0f);
            if (comparison == FloatComparisonType.LessOrEqual)
                node.MaxDistance = resolved;
            else
            {
                node.MinDistance = resolved;
                node.MaxDistance = float.MaxValue;
            }

            return node;
        }

        private static BlackboardBoolConditionNode CreateBlackboardBoolNode(BehaviorTreeAsset tree, string key, bool expected, int row)
        {
            var node = CreateNode<BlackboardBoolConditionNode>(tree, key, new Vector2(520f, row * 180f));
            SetPrivateField(node, "_key", key);
            SetPrivateField(node, "_expectedValue", expected);
            return node;
        }

        private static TransitionEnemyStateNode CreateTransitionNode(BehaviorTreeAsset tree, string state, int row, string cooldownId = null, float cooldownDuration = 0f)
        {
            if (!Enum.TryParse<EnemyTransitionStateType>(state, out var parsed))
                throw new System.IO.InvalidDataException($"알 수 없는 EnemyTransitionStateType입니다. {state}");

            var node = CreateNode<TransitionEnemyStateNode>(tree, "Transition " + state, new Vector2(820f, row * 180f));
            node.TargetState = parsed;
            node.CooldownId = cooldownId;
            node.CooldownDuration = cooldownDuration;
            return node;
        }

        private static TransitionFlyingEnemyStateNode CreateFlyingTransitionNode(BehaviorTreeAsset tree, string state, int row)
        {
            if (!Enum.TryParse<FlyingEnemyTransitionStateType>(state, out var parsed))
                throw new System.IO.InvalidDataException($"알 수 없는 FlyingEnemyTransitionStateType입니다. {state}");

            var node = CreateNode<TransitionFlyingEnemyStateNode>(tree, "Flying Transition " + state, new Vector2(820f, row * 180f));
            node.TargetState = parsed;
            return node;
        }

        private static BTNode CreatePatrolOrIdleNode(BehaviorTreeAsset tree, int row)
        {
            var selector = CreateNode<SelectorNode>(tree, "Patrol Or Idle", new Vector2(820f, row * 180f));
            var patrolSequence = CreateNode<SequenceNode>(tree, "Patrol If Enabled", new Vector2(1080f, row * 180f));
            patrolSequence.Children.Add(CreateNode<IsEnemyPatrolEnabledNode>(tree, "Is Patrol Enabled", new Vector2(1320f, row * 180f)));
            patrolSequence.Children.Add(CreateTransitionNode(tree, nameof(EnemyTransitionStateType.Patrol), row));

            selector.Children.Add(patrolSequence);
            selector.Children.Add(CreateTransitionNode(tree, nameof(EnemyTransitionStateType.Idle), row));
            return selector;
        }

        private static BTNode CreateFlyingPatrolOrIdleNode(BehaviorTreeAsset tree, int row)
        {
            var selector = CreateNode<SelectorNode>(tree, "Flying Patrol Or Idle", new Vector2(820f, row * 180f));
            var patrolSequence = CreateNode<SequenceNode>(tree, "Flying Patrol If Enabled", new Vector2(1080f, row * 180f));
            // EnemyFlyingAIContext.EnablePatrol은 별도 노드가 없으므로 Blackboard로 읽는다.
            patrolSequence.Children.Add(CreateBlackboardBoolNode(tree, "enablePatrol", true, row));
            patrolSequence.Children.Add(CreateFlyingTransitionNode(tree, nameof(FlyingEnemyTransitionStateType.Patrol), row));

            selector.Children.Add(patrolSequence);
            selector.Children.Add(CreateFlyingTransitionNode(tree, nameof(FlyingEnemyTransitionStateType.Idle), row));
            return selector;
        }

        private static IsCurrentActorStateNode CreateIsCurrentStateNode(BehaviorTreeAsset tree, string stateName, bool expected, int row)
        {
            var node = CreateNode<IsCurrentActorStateNode>(tree, (expected ? "Is " : "Is Not ") + stateName, new Vector2(520f, row * 180f));
            node.StateName = stateName;
            node.ExpectedValue = expected;
            return node;
        }

        private static IsEnemyPhaseNode CreateEnemyPhaseNode(BehaviorTreeAsset tree, string value, int row)
        {
            var node = CreateNode<IsEnemyPhaseNode>(tree, "Is Enemy Phase " + value, new Vector2(520f, row * 180f));
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var phaseIndex))
                SetPrivateField(node, "_phaseIndex", phaseIndex);
            else
                SetPrivateField(node, "_phaseName", value);

            return node;
        }

        private static CooldownReadyNode CreateCooldownReadyNode(BehaviorTreeAsset tree, string cooldownId, int row)
        {
            var node = CreateNode<CooldownReadyNode>(tree, "CooldownReady", new Vector2(520f, row * 180f));
            node.CooldownId = cooldownId;
            return node;
        }

        private static IsSelfLowHealthNode CreateSelfLowHealthNode(BehaviorTreeAsset tree, string value, int row)
        {
            var node = CreateNode<IsSelfLowHealthNode>(tree, "IsSelfLowHealth", new Vector2(520f, row * 180f));
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var threshold))
                node.Threshold = threshold;
            return node;
        }

        private static RecentHitCountGreaterOrEqualNode CreateRecentHitCountNode(BehaviorTreeAsset tree, string value, int row)
        {
            var node = CreateNode<RecentHitCountGreaterOrEqualNode>(tree, "RecentHitCountGreaterOrEqual", new Vector2(520f, row * 180f));
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var threshold))
                node.Threshold = threshold;
            return node;
        }

        private static ConsecutiveAttackCountNode CreateConsecutiveAttackCountNode(
            BehaviorTreeAsset tree,
            string value,
            IntComparisonType comparison,
            int row)
        {
            var node = CreateNode<ConsecutiveAttackCountNode>(tree, comparison == IntComparisonType.LessThan
                ? "ConsecutiveAttackCountLessThan"
                : "ConsecutiveAttackCountGreaterOrEqual", new Vector2(520f, row * 180f));
            node.Comparison = comparison;
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var threshold))
                node.Threshold = threshold;
            return node;
        }

        private static CanRevengeAfterHitNode CreateRevengeAfterHitNode(BehaviorTreeAsset tree, string value, int row)
        {
            var node = CreateNode<CanRevengeAfterHitNode>(tree, "CanRevengeAfterHit", new Vector2(520f, row * 180f));
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var cooldown))
                node.Cooldown = cooldown;
            return node;
        }

        private static WaitNode CreateWaitNode(BehaviorTreeAsset tree, float duration, int row)
        {
            var node = CreateNode<WaitNode>(tree, "Wait", new Vector2(820f, row * 180f));
            SetPrivateField(node, "_duration", Mathf.Max(0f, duration));
            return node;
        }

        private static BTNode WrapInverter(BehaviorTreeAsset tree, BTNode child, int row)
        {
            if (child == null)
                return null;

            var inverter = CreateNode<InverterNode>(tree, "Invert " + child.DisplayName, new Vector2(500f, row * 180f));
            inverter.Children.Add(child);
            return inverter;
        }

        private static T CreateNode<T>(BehaviorTreeAsset tree, string displayName, Vector2 position) where T : BTNode
        {
            var node = ScriptableObject.CreateInstance<T>();
            node.name = displayName;
            node.DisplayName = displayName;
            node.EditorPosition = position;
            node.EnsureGuid();
            tree.Nodes.Add(node);
            return node;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }
    }
}
#endif
