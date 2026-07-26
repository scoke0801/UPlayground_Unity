#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UPlayGround.State;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public static partial class MonsterBehaviorTreeJsonImporter
    {
        private enum JsonNodeActorScope
        {
            Common,
            GroundOnly,
            FlyingOnly
        }

        private sealed class ConditionNodeDefinition
        {
            public JsonNodeActorScope Scope { get; }
            public bool HandlesInvert { get; }
            public Func<BehaviorTreeAsset, MonsterBehaviorConditionJson, EnemyBehaviorSO, MonsterBehaviorBlackboardJson, int, BTNode> Factory { get; }

            public ConditionNodeDefinition(
                JsonNodeActorScope scope,
                bool handlesInvert,
                Func<BehaviorTreeAsset, MonsterBehaviorConditionJson, EnemyBehaviorSO, MonsterBehaviorBlackboardJson, int, BTNode> factory)
            {
                Scope = scope;
                HandlesInvert = handlesInvert;
                Factory = factory;
            }
        }

        private sealed class ActionNodeDefinition
        {
            public JsonNodeActorScope Scope { get; }
            public Func<BehaviorTreeAsset, MonsterBehaviorActionJson, int, BTNode> Factory { get; }

            public ActionNodeDefinition(JsonNodeActorScope scope, Func<BehaviorTreeAsset, MonsterBehaviorActionJson, int, BTNode> factory)
            {
                Scope = scope;
                Factory = factory;
            }
        }

        private static readonly Dictionary<string, ConditionNodeDefinition> ConditionNodeDefinitions = new(StringComparer.Ordinal)
        {
            [MonsterBehaviorJsonNodeKeys.Conditions.HasTarget] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateHasTargetNode(tree, !condition.invert, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.IsBlockedEnemyState] = new(JsonNodeActorScope.Common, false, (tree, _, _, _, row) => CreateConditionLeaf<IsBlockedEnemyStateNode>(tree, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.HasStateTag] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateHasStateTagNode(tree, condition.value, !condition.invert, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.BlackboardCompare] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateBlackboardCompareNode(tree, condition, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.IsEnemyPhase] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateEnemyPhaseCompareNode(tree, condition, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.DistanceLessOrEqual] = new(JsonNodeActorScope.Common, false, (tree, condition, source, blackboard, row) => CreateRangeNode(tree, FloatComparisonType.LessOrEqual, condition.value, source, blackboard, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.DistanceGreater] = new(JsonNodeActorScope.Common, false, (tree, condition, source, blackboard, row) => CreateRangeNode(tree, FloatComparisonType.GreaterOrEqual, condition.value, source, blackboard, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.ActionDelayElapsed] = new(JsonNodeActorScope.Common, false, (tree, _, _, _, row) => CreateConditionLeaf<HasEnemyActionDelayElapsedNode>(tree, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.CanUseSkill] = new(JsonNodeActorScope.GroundOnly, false, (tree, _, _, _, row) => CreateConditionLeaf<CanUseEnemySkillNode>(tree, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.HasAttackInRange] = new(JsonNodeActorScope.GroundOnly, false, (tree, _, _, _, row) => CreateConditionLeaf<HasEnemyAttackInRangeNode>(tree, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.HasLineOfSight] = new(JsonNodeActorScope.Common, false, (tree, _, _, _, row) => CreateConditionLeaf<HasEnemyLineOfSightNode>(tree, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.IsPlayerAttacking] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateBlackboardAliasNode(tree, condition, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.IsPlayerGuarding] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateBlackboardAliasNode(tree, condition, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.IsPlayerStaggered] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateBlackboardAliasNode(tree, condition, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.IsPlayerRecovering] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateBlackboardAliasNode(tree, condition, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.IsPlayerDodgingFrequently] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateBlackboardAliasNode(tree, condition, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.IsPlayerAttackingFrequently] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateBlackboardAliasNode(tree, condition, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.IsPlayerGuardingFrequently] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateBlackboardAliasNode(tree, condition, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.IsPlayerRecoveringFrequently] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateBlackboardAliasNode(tree, condition, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.RecentlyHitByPlayer] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateBlackboardAliasNode(tree, condition, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.HasAttackSlot] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateBlackboardAliasNode(tree, condition, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.CooldownReady] = new(JsonNodeActorScope.Common, false, (tree, condition, _, _, row) => CreateCooldownReadyNode(tree, condition.value, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.IsSelfLowHealth] = new(JsonNodeActorScope.Common, false, (tree, condition, _, _, row) => CreateSelfLowHealthNode(tree, condition.value, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.WasLastHitHeavy] = new(JsonNodeActorScope.Common, false, (tree, _, _, _, row) => CreateConditionLeaf<WasLastHitHeavyNode>(tree, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.IsPoiseBroken] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateBlackboardAliasNode(tree, condition, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.RecentHitCountGreaterOrEqual] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateBlackboardAliasNode(tree, condition, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.ConsecutiveAttackCountLessThan] = new(JsonNodeActorScope.Common, false, (tree, condition, _, _, row) => CreateConsecutiveAttackCountNode(tree, condition.value, IntComparisonType.LessThan, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.ConsecutiveAttackCountGreaterOrEqual] = new(JsonNodeActorScope.Common, false, (tree, condition, _, _, row) => CreateConsecutiveAttackCountNode(tree, condition.value, IntComparisonType.GreaterOrEqual, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.CanIgnoreLightHit] = new(JsonNodeActorScope.Common, false, (tree, _, _, _, row) => CreateConditionLeaf<CanIgnoreLightHitNode>(tree, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.CanRevengeAfterHit] = new(JsonNodeActorScope.Common, false, (tree, condition, _, _, row) => CreateRevengeAfterHitNode(tree, condition.value, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.SelectedIntent] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateBlackboardAliasNode(tree, condition, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.IsCurrentState] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateIsCurrentStateNode(tree, condition.value, !condition.invert, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.IsFlyingAirState] = new(JsonNodeActorScope.FlyingOnly, false, (tree, _, _, _, row) => CreateConditionLeaf<IsFlyingAirStateNode>(tree, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.IsFlyingGroundCombatState] = new(JsonNodeActorScope.FlyingOnly, false, (tree, _, _, _, row) => CreateConditionLeaf<IsFlyingGroundCombatStateNode>(tree, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.IsAirAttackLimitReached] = new(JsonNodeActorScope.FlyingOnly, false, (tree, _, _, _, row) => CreateConditionLeaf<IsAirAttackLimitReachedNode>(tree, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.ShouldFlyingTakeOff] = new(JsonNodeActorScope.FlyingOnly, false, (tree, _, _, _, row) => CreateConditionLeaf<ShouldFlyingTakeOffNode>(tree, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.FlyingCanUseSkill] = new(JsonNodeActorScope.FlyingOnly, false, (tree, _, _, _, row) => CreateConditionLeaf<FlyingCanUseSkillNode>(tree, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.HasDiveSkillAvailable] = new(JsonNodeActorScope.FlyingOnly, false, (tree, _, _, _, row) => CreateConditionLeaf<HasDiveSkillAvailableNode>(tree, row)),
            [MonsterBehaviorJsonNodeKeys.Conditions.RollDiveChance] = new(JsonNodeActorScope.FlyingOnly, false, (tree, _, _, _, row) => CreateConditionLeaf<RollDiveChanceNode>(tree, row))
        };

        private static readonly Dictionary<string, ActionNodeDefinition> ActionNodeDefinitions = new(StringComparer.Ordinal)
        {
            [MonsterBehaviorJsonNodeKeys.Actions.KeepCurrentState] = new(JsonNodeActorScope.Common, (tree, _, row) => CreateActionLeaf<KeepCurrentStateNode>(tree, row)),
            [MonsterBehaviorJsonNodeKeys.Actions.PatrolOrIdle] = new(JsonNodeActorScope.GroundOnly, (tree, _, row) => CreatePatrolOrIdleNode(tree, row)),
            [MonsterBehaviorJsonNodeKeys.Actions.Transition] = new(JsonNodeActorScope.GroundOnly, (tree, action, row) => CreateTransitionNode(tree, action.state, row, action.cooldownId, action.cooldownDuration)),
            [MonsterBehaviorJsonNodeKeys.Actions.RequestAction] = new(JsonNodeActorScope.Common, (tree, action, row) => CreateRequestActionNode(tree, action, row)),
            [MonsterBehaviorJsonNodeKeys.Actions.RequestAttackSlot] = new(JsonNodeActorScope.GroundOnly, (tree, _, row) => CreateActionLeaf<RequestEnemyAttackSlotNode>(tree, row)),
            [MonsterBehaviorJsonNodeKeys.Actions.ExecuteAttack] = new(JsonNodeActorScope.GroundOnly, (tree, action, row) => CreateExecuteAttackNode(tree, action.attackCategory, row)),
            [MonsterBehaviorJsonNodeKeys.Actions.Wait] = new(JsonNodeActorScope.Common, (tree, action, row) => CreateWaitNode(tree, action.duration, row)),
            [MonsterBehaviorJsonNodeKeys.Actions.FlyingTransition] = new(JsonNodeActorScope.FlyingOnly, (tree, action, row) => CreateFlyingTransitionNode(tree, action.state, row)),
            [MonsterBehaviorJsonNodeKeys.Actions.FlyingPatrolOrIdle] = new(JsonNodeActorScope.FlyingOnly, (tree, _, row) => CreateFlyingPatrolOrIdleNode(tree, row)),
            [MonsterBehaviorJsonNodeKeys.Actions.ResetFlyingCounters] = new(JsonNodeActorScope.FlyingOnly, (tree, _, row) => CreateActionLeaf<ResetFlyingCountersNode>(tree, row)),
            [MonsterBehaviorJsonNodeKeys.Actions.ResetFlyingAirCounters] = new(JsonNodeActorScope.FlyingOnly, (tree, _, row) => CreateActionLeaf<ResetFlyingAirCountersNode>(tree, row)),
            [MonsterBehaviorJsonNodeKeys.Actions.DescendFlying] = new(JsonNodeActorScope.FlyingOnly, (tree, _, row) => CreateActionLeaf<DescendFlyingNode>(tree, row)),
            [MonsterBehaviorJsonNodeKeys.Actions.RequestFlyingAttackSlot] = new(JsonNodeActorScope.FlyingOnly, (tree, _, row) => CreateActionLeaf<RequestFlyingAttackSlotNode>(tree, row)),
            [MonsterBehaviorJsonNodeKeys.Actions.SelectFlyingDiveSkill] = new(JsonNodeActorScope.FlyingOnly, (tree, _, row) => CreateActionLeaf<SelectFlyingDiveSkillNode>(tree, row))
        };

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
            var sequence = CreateNode<SequenceNode>(tree, rule.name, new Vector2(SequenceColumnX, index * NodeRowHeight));

            foreach (var condition in rule.when ?? new List<MonsterBehaviorConditionJson>())
            {
                var conditionNode = CreateConditionNode(tree, condition, sourceBehavior, blackboard, index);
                if (conditionNode != null)
                    sequence.Children.Add(conditionNode);
            }

            if (rule.select == MonsterBehaviorJsonNodeKeys.SelectKinds.WeightedRandom)
            {
                var selector = CreateNode<WeightedRandomSelectorNode>(tree, rule.name + " Weighted", new Vector2(WeightedSelectorColumnX, index * NodeRowHeight));
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
            if (condition == null || string.IsNullOrWhiteSpace(condition.condition))
                return null;

            if (!ConditionNodeDefinitions.TryGetValue(condition.condition, out var definition))
                return null;

            var node = definition.Factory(tree, condition, sourceBehavior, blackboard, row);
            return condition.invert && !definition.HandlesInvert
                ? WrapInverter(tree, node, row)
                : node;
        }

        private static BTNode CreateActionNode(BehaviorTreeAsset tree, MonsterBehaviorActionJson action, int row)
        {
            if (action == null || string.IsNullOrWhiteSpace(action.action))
                return null;

            return ActionNodeDefinitions.TryGetValue(action.action, out var definition)
                ? definition.Factory(tree, action, row)
                : null;
        }
        private static BTNode CreateChoiceActionNode(BehaviorTreeAsset tree, MonsterBehaviorChoiceJson choice, int row, int column)
        {
            return CreateActionNode(
                tree,
                new MonsterBehaviorActionJson
                {
                    action = choice.action,
                    intent = choice.intent,
                    style = choice.style,
                    state = choice.state,
                    attackCategory = choice.attackCategory,
                    cooldownId = choice.cooldownId,
                    cooldownDuration = choice.cooldownDuration
                },
                row + column);
        }

        private static ExecuteEnemyAttackNode CreateExecuteAttackNode(BehaviorTreeAsset tree, string attackCategory, int row)
        {
            var node = CreateNode<ExecuteEnemyAttackNode>(tree, string.IsNullOrWhiteSpace(attackCategory) ? "Execute Attack" : $"Execute Attack {attackCategory}", ActionPosition(row));
            if (Enum.TryParse<AbilityAttackCategory>(attackCategory, true, out var parsed))
                node.AttackCategory = parsed;
            return node;
        }

        private static HasTargetNode CreateHasTargetNode(BehaviorTreeAsset tree, bool expected, int row)
        {
            var node = CreateNode<HasTargetNode>(tree, expected ? "Has Target" : "Has No Target", ConditionPosition(row));
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
            var node = CreateNode<IsTargetInRangeNode>(tree, comparison.ToString(), ConditionPosition(row));
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

        private static BlackboardCompareNode CreateBlackboardCompareNode(BehaviorTreeAsset tree, MonsterBehaviorConditionJson condition, int row)
        {
            if (!Enum.TryParse<BlackboardComparisonType>(condition.op, true, out var comparison))
                comparison = BlackboardComparisonType.Equal;

            if (condition.invert)
                comparison = InvertComparison(comparison);

            return CreateBlackboardCompareNode(tree, condition.key, comparison, condition.value, condition.valueKey, row);
        }

        private static BlackboardComparisonType InvertComparison(BlackboardComparisonType comparison)
        {
            return comparison switch
            {
                BlackboardComparisonType.Equal => BlackboardComparisonType.NotEqual,
                BlackboardComparisonType.NotEqual => BlackboardComparisonType.Equal,
                BlackboardComparisonType.Less => BlackboardComparisonType.GreaterOrEqual,
                BlackboardComparisonType.LessOrEqual => BlackboardComparisonType.Greater,
                BlackboardComparisonType.Greater => BlackboardComparisonType.LessOrEqual,
                BlackboardComparisonType.GreaterOrEqual => BlackboardComparisonType.Less,
                _ => comparison
            };
        }

        private static BlackboardComparisonType BoolComparison(bool invert)
        {
            return invert ? BlackboardComparisonType.NotEqual : BlackboardComparisonType.Equal;
        }

        private static BlackboardCompareNode CreateBlackboardAliasNode(
            BehaviorTreeAsset tree,
            MonsterBehaviorConditionJson condition,
            int row)
        {
            if (!BehaviorTreeEditorRegistryData.TryGetBlackboardConditionAlias(condition.condition, out var alias))
                throw new System.IO.InvalidDataException($"Blackboard 조건 alias를 찾을 수 없습니다. condition={condition.condition}");

            var comparison = condition.invert ? InvertComparison(alias.Comparison) : alias.Comparison;
            var value = string.IsNullOrWhiteSpace(condition.value) ? alias.Value : condition.value;
            return CreateBlackboardCompareNode(tree, alias.Key, comparison, value, condition.valueKey, row);
        }

        private static BlackboardCompareNode CreateEnemyPhaseCompareNode(
            BehaviorTreeAsset tree,
            MonsterBehaviorConditionJson condition,
            int row)
        {
            var comparison = condition.invert ? BlackboardComparisonType.NotEqual : BlackboardComparisonType.Equal;
            if (int.TryParse(condition.value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                return CreateBlackboardCompareNode(tree, EnemyBlackboardKeys.SelfPhaseIndex, comparison, condition.value, null, row);

            return CreateBlackboardCompareNode(tree, EnemyBlackboardKeys.SelfPhaseName, comparison, condition.value, null, row);
        }

        private static BlackboardCompareNode CreateBlackboardCompareNode(
            BehaviorTreeAsset tree,
            string key,
            BlackboardComparisonType comparison,
            string value,
            string valueKey,
            int row)
        {
            var right = string.IsNullOrWhiteSpace(valueKey) ? value : $"${valueKey}";
            var node = CreateNode<BlackboardCompareNode>(tree, $"{key} {comparison} {right}", ConditionPosition(row));
            node.Key = key;
            node.Comparison = comparison;
            node.Value = value;
            node.ValueKey = valueKey;
            return node;
        }

        private static HasStateTagNode CreateHasStateTagNode(BehaviorTreeAsset tree, string tagName, bool expected, int row)
        {
            if (!Enum.TryParse<ActorStateTag>(tagName, true, out var tag))
                throw new System.IO.InvalidDataException($"알 수 없는 ActorStateTag입니다. {tagName}");

            var node = CreateNode<HasStateTagNode>(tree, (expected ? "Has " : "Has No ") + tag, ConditionPosition(row));
            node.Tag = tag;
            node.ExpectedValue = expected;
            return node;
        }

        private static RequestEnemyActionNode CreateRequestActionNode(BehaviorTreeAsset tree, MonsterBehaviorActionJson action, int row)
        {
            if (!Enum.TryParse<EnemyActionIntent>(action.intent, true, out var intent))
                throw new System.IO.InvalidDataException($"알 수 없는 EnemyActionIntent입니다. {action.intent}");

            var style = EnemyActionStyle.None;
            if (!string.IsNullOrWhiteSpace(action.style)
                && !Enum.TryParse(action.style, true, out style))
                throw new System.IO.InvalidDataException($"알 수 없는 EnemyActionStyle입니다. {action.style}");

            var attackCategory = AbilityAttackCategory.None;
            if (!string.IsNullOrWhiteSpace(action.attackCategory)
                && !Enum.TryParse(action.attackCategory, true, out attackCategory))
                throw new System.IO.InvalidDataException($"알 수 없는 AbilityAttackCategory입니다. {action.attackCategory}");

            var displayName = style == EnemyActionStyle.None
                ? $"Request {intent}"
                : $"Request {intent} {style}";
            var node = CreateNode<RequestEnemyActionNode>(tree, displayName, ActionPosition(row));
            node.Intent = intent;
            node.Style = style;
            node.AttackCategory = attackCategory;
            node.CooldownId = action.cooldownId;
            node.CooldownDuration = action.cooldownDuration;
            return node;
        }

        private static TransitionEnemyStateNode CreateTransitionNode(BehaviorTreeAsset tree, string state, int row, string cooldownId = null, float cooldownDuration = 0f)
        {
            if (!Enum.TryParse<EnemyTransitionStateType>(state, out var parsed))
                throw new System.IO.InvalidDataException($"알 수 없는 EnemyTransitionStateType입니다. {state}");

            var node = CreateNode<TransitionEnemyStateNode>(tree, "Transition " + state, ActionPosition(row));
            node.TargetState = parsed;
            node.CooldownId = cooldownId;
            node.CooldownDuration = cooldownDuration;
            return node;
        }

        private static TransitionFlyingEnemyStateNode CreateFlyingTransitionNode(BehaviorTreeAsset tree, string state, int row)
        {
            if (!Enum.TryParse<FlyingEnemyTransitionStateType>(state, out var parsed))
                throw new System.IO.InvalidDataException($"알 수 없는 FlyingEnemyTransitionStateType입니다. {state}");

            var node = CreateNode<TransitionFlyingEnemyStateNode>(tree, "Flying Transition " + state, ActionPosition(row));
            node.TargetState = parsed;
            return node;
        }

        private static BTNode CreatePatrolOrIdleNode(BehaviorTreeAsset tree, int row)
        {
            var selector = CreateNode<SelectorNode>(tree, "Patrol Or Idle", ActionPosition(row));
            var patrolSequence = CreateNode<SequenceNode>(tree, "Patrol If Enabled", new Vector2(SelectorChildColumnX, row * NodeRowHeight));
            patrolSequence.Children.Add(CreateNode<IsEnemyPatrolEnabledNode>(tree, "Is Patrol Enabled", new Vector2(SelectorGrandchildColumnX, row * NodeRowHeight)));
            patrolSequence.Children.Add(CreateTransitionNode(tree, nameof(EnemyTransitionStateType.Patrol), row));

            selector.Children.Add(patrolSequence);
            selector.Children.Add(CreateTransitionNode(tree, nameof(EnemyTransitionStateType.Idle), row));
            return selector;
        }

        private static BTNode CreateFlyingPatrolOrIdleNode(BehaviorTreeAsset tree, int row)
        {
            var selector = CreateNode<SelectorNode>(tree, "Flying Patrol Or Idle", ActionPosition(row));
            var patrolSequence = CreateNode<SequenceNode>(tree, "Flying Patrol If Enabled", new Vector2(SelectorChildColumnX, row * NodeRowHeight));
            // EnemyFlyingAIContext.EnablePatrol은 별도 노드가 없으므로 Blackboard로 읽는다.
            patrolSequence.Children.Add(CreateBlackboardCompareNode(tree, EnemyBlackboardKeys.EnablePatrol, BlackboardComparisonType.Equal, "true", null, row));
            patrolSequence.Children.Add(CreateFlyingTransitionNode(tree, nameof(FlyingEnemyTransitionStateType.Patrol), row));

            selector.Children.Add(patrolSequence);
            selector.Children.Add(CreateFlyingTransitionNode(tree, nameof(FlyingEnemyTransitionStateType.Idle), row));
            return selector;
        }

        private static IsCurrentActorStateNode CreateIsCurrentStateNode(BehaviorTreeAsset tree, string stateName, bool expected, int row)
        {
            var node = CreateNode<IsCurrentActorStateNode>(tree, (expected ? "Is " : "Is Not ") + stateName, ConditionPosition(row));
            node.StateName = stateName;
            node.ExpectedValue = expected;
            return node;
        }

        private static CooldownReadyNode CreateCooldownReadyNode(BehaviorTreeAsset tree, string cooldownId, int row)
        {
            var node = CreateConditionLeaf<CooldownReadyNode>(tree, row);
            node.CooldownId = cooldownId;
            return node;
        }

        private static IsSelfLowHealthNode CreateSelfLowHealthNode(BehaviorTreeAsset tree, string value, int row)
        {
            var node = CreateConditionLeaf<IsSelfLowHealthNode>(tree, row);
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var threshold))
                node.Threshold = threshold;
            return node;
        }

        private static RecentHitCountGreaterOrEqualNode CreateRecentHitCountNode(BehaviorTreeAsset tree, string value, int row)
        {
            var node = CreateConditionLeaf<RecentHitCountGreaterOrEqualNode>(tree, row);
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
            var conditionKey = comparison == IntComparisonType.LessThan
                ? MonsterBehaviorJsonNodeKeys.Conditions.ConsecutiveAttackCountLessThan
                : MonsterBehaviorJsonNodeKeys.Conditions.ConsecutiveAttackCountGreaterOrEqual;
            var label = BehaviorTreeEditorRegistryData.TryGetNodeLabel(conditionKey, out var registered)
                ? registered
                : conditionKey;
            var node = CreateNode<ConsecutiveAttackCountNode>(tree, label, ConditionPosition(row));
            node.Comparison = comparison;
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var threshold))
                node.Threshold = threshold;
            return node;
        }

        private static CanRevengeAfterHitNode CreateRevengeAfterHitNode(BehaviorTreeAsset tree, string value, int row)
        {
            var node = CreateConditionLeaf<CanRevengeAfterHitNode>(tree, row);
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var cooldown))
                node.Cooldown = cooldown;
            return node;
        }

        private static WaitNode CreateWaitNode(BehaviorTreeAsset tree, float duration, int row)
        {
            var node = CreateActionLeaf<WaitNode>(tree, row);
            SetPrivateField(node, "_duration", Mathf.Max(0f, duration));
            return node;
        }

        private static BTNode WrapInverter(BehaviorTreeAsset tree, BTNode child, int row)
        {
            if (child == null)
                return null;

            var inverter = CreateNode<InverterNode>(tree, "Invert " + child.DisplayName, new Vector2(InverterColumnX, row * NodeRowHeight));
            inverter.Children.Add(child);
            return inverter;
        }

        private const float SequenceColumnX = 260f;
        private const float ConditionColumnX = 520f;
        private const float InverterColumnX = 500f;
        private const float WeightedSelectorColumnX = 560f;
        private const float ActionColumnX = 820f;
        private const float SelectorChildColumnX = 1080f;
        private const float SelectorGrandchildColumnX = 1320f;
        private const float NodeRowHeight = 180f;

        private static Vector2 ConditionPosition(int row) => new(ConditionColumnX, row * NodeRowHeight);
        private static Vector2 ActionPosition(int row) => new(ActionColumnX, row * NodeRowHeight);

        private static T CreateConditionLeaf<T>(BehaviorTreeAsset tree, int row) where T : BTNode
            => CreateNode<T>(tree, BehaviorTreeDisplayNameRegistry.GetNodeTypeLabel(typeof(T)), ConditionPosition(row));

        private static T CreateActionLeaf<T>(BehaviorTreeAsset tree, int row) where T : BTNode
            => CreateNode<T>(tree, BehaviorTreeDisplayNameRegistry.GetNodeTypeLabel(typeof(T)), ActionPosition(row));

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
