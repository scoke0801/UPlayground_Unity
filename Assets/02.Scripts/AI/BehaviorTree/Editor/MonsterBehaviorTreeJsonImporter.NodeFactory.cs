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
            ["HasTarget"] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateHasTargetNode(tree, !condition.invert, row)),
            ["IsBlockedEnemyState"] = new(JsonNodeActorScope.Common, false, (tree, _, _, _, row) => CreateNode<IsBlockedEnemyStateNode>(tree, "Is Blocked Enemy State", new Vector2(520f, row * 180f))),
            ["HasStateTag"] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateHasStateTagNode(tree, condition.value, !condition.invert, row)),
            ["BlackboardCompare"] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateBlackboardCompareNode(tree, condition, row)),
            ["IsEnemyPhase"] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateEnemyPhaseCompareNode(tree, condition, row)),
            ["DistanceLessOrEqual"] = new(JsonNodeActorScope.Common, false, (tree, condition, source, blackboard, row) => CreateRangeNode(tree, FloatComparisonType.LessOrEqual, condition.value, source, blackboard, row)),
            ["DistanceGreater"] = new(JsonNodeActorScope.Common, false, (tree, condition, source, blackboard, row) => CreateRangeNode(tree, FloatComparisonType.GreaterOrEqual, condition.value, source, blackboard, row)),
            ["ActionDelayElapsed"] = new(JsonNodeActorScope.Common, false, (tree, _, _, _, row) => CreateNode<HasEnemyActionDelayElapsedNode>(tree, "Action Delay Elapsed", new Vector2(520f, row * 180f))),
            ["CanUseSkill"] = new(JsonNodeActorScope.GroundOnly, false, (tree, _, _, _, row) => CreateNode<CanUseEnemySkillNode>(tree, "Can Use Enemy Skill", new Vector2(520f, row * 180f))),
            ["IsPlayerAttacking"] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateBlackboardAliasNode(tree, condition, row)),
            ["IsPlayerGuarding"] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateBlackboardAliasNode(tree, condition, row)),
            ["IsPlayerStaggered"] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateBlackboardAliasNode(tree, condition, row)),
            ["IsPlayerRecovering"] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateBlackboardAliasNode(tree, condition, row)),
            ["IsPlayerDodgingFrequently"] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateBlackboardAliasNode(tree, condition, row)),
            ["IsPlayerAttackingFrequently"] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateBlackboardAliasNode(tree, condition, row)),
            ["IsPlayerGuardingFrequently"] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateBlackboardAliasNode(tree, condition, row)),
            ["IsPlayerRecoveringFrequently"] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateBlackboardAliasNode(tree, condition, row)),
            ["RecentlyHitByPlayer"] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateBlackboardAliasNode(tree, condition, row)),
            ["HasAttackSlot"] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateBlackboardAliasNode(tree, condition, row)),
            ["CooldownReady"] = new(JsonNodeActorScope.Common, false, (tree, condition, _, _, row) => CreateCooldownReadyNode(tree, condition.value, row)),
            ["IsSelfLowHealth"] = new(JsonNodeActorScope.Common, false, (tree, condition, _, _, row) => CreateSelfLowHealthNode(tree, condition.value, row)),
            ["WasLastHitHeavy"] = new(JsonNodeActorScope.Common, false, (tree, _, _, _, row) => CreateNode<WasLastHitHeavyNode>(tree, "WasLastHitHeavy", new Vector2(520f, row * 180f))),
            ["IsPoiseBroken"] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateBlackboardAliasNode(tree, condition, row)),
            ["RecentHitCountGreaterOrEqual"] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateBlackboardAliasNode(tree, condition, row)),
            ["ConsecutiveAttackCountLessThan"] = new(JsonNodeActorScope.Common, false, (tree, condition, _, _, row) => CreateConsecutiveAttackCountNode(tree, condition.value, IntComparisonType.LessThan, row)),
            ["ConsecutiveAttackCountGreaterOrEqual"] = new(JsonNodeActorScope.Common, false, (tree, condition, _, _, row) => CreateConsecutiveAttackCountNode(tree, condition.value, IntComparisonType.GreaterOrEqual, row)),
            ["CanIgnoreLightHit"] = new(JsonNodeActorScope.Common, false, (tree, _, _, _, row) => CreateNode<CanIgnoreLightHitNode>(tree, "CanIgnoreLightHit", new Vector2(520f, row * 180f))),
            ["CanRevengeAfterHit"] = new(JsonNodeActorScope.Common, false, (tree, condition, _, _, row) => CreateRevengeAfterHitNode(tree, condition.value, row)),
            ["SelectedIntent"] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateBlackboardAliasNode(tree, condition, row)),
            ["IsCurrentState"] = new(JsonNodeActorScope.Common, true, (tree, condition, _, _, row) => CreateIsCurrentStateNode(tree, condition.value, !condition.invert, row)),
            ["IsFlyingAirState"] = new(JsonNodeActorScope.FlyingOnly, false, (tree, _, _, _, row) => CreateNode<IsFlyingAirStateNode>(tree, "Is Flying Air State", new Vector2(520f, row * 180f))),
            ["IsFlyingGroundCombatState"] = new(JsonNodeActorScope.FlyingOnly, false, (tree, _, _, _, row) => CreateNode<IsFlyingGroundCombatStateNode>(tree, "Is Flying Ground Combat State", new Vector2(520f, row * 180f))),
            ["IsAirAttackLimitReached"] = new(JsonNodeActorScope.FlyingOnly, false, (tree, _, _, _, row) => CreateNode<IsAirAttackLimitReachedNode>(tree, "Air Attack Limit Reached", new Vector2(520f, row * 180f))),
            ["ShouldFlyingTakeOff"] = new(JsonNodeActorScope.FlyingOnly, false, (tree, _, _, _, row) => CreateNode<ShouldFlyingTakeOffNode>(tree, "Should Flying Take Off", new Vector2(520f, row * 180f))),
            ["FlyingCanUseSkill"] = new(JsonNodeActorScope.FlyingOnly, false, (tree, _, _, _, row) => CreateNode<FlyingCanUseSkillNode>(tree, "Flying Can Use Skill", new Vector2(520f, row * 180f))),
            ["HasDiveSkillAvailable"] = new(JsonNodeActorScope.FlyingOnly, false, (tree, _, _, _, row) => CreateNode<HasDiveSkillAvailableNode>(tree, "Has Dive Skill Available", new Vector2(520f, row * 180f))),
            ["RollDiveChance"] = new(JsonNodeActorScope.FlyingOnly, false, (tree, _, _, _, row) => CreateNode<RollDiveChanceNode>(tree, "Roll Dive Chance", new Vector2(520f, row * 180f)))
        };

        private static readonly Dictionary<string, ActionNodeDefinition> ActionNodeDefinitions = new(StringComparer.Ordinal)
        {
            ["KeepCurrentState"] = new(JsonNodeActorScope.Common, (tree, _, row) => CreateNode<KeepCurrentStateNode>(tree, "Keep Current State", new Vector2(820f, row * 180f))),
            ["PatrolOrIdle"] = new(JsonNodeActorScope.GroundOnly, (tree, _, row) => CreatePatrolOrIdleNode(tree, row)),
            ["Transition"] = new(JsonNodeActorScope.GroundOnly, (tree, action, row) => CreateTransitionNode(tree, action.state, row, action.cooldownId, action.cooldownDuration)),
            ["RequestAction"] = new(JsonNodeActorScope.Common, (tree, action, row) => CreateRequestActionNode(tree, action, row)),
            ["RequestAttackSlot"] = new(JsonNodeActorScope.GroundOnly, (tree, _, row) => CreateNode<RequestEnemyAttackSlotNode>(tree, "Request Attack Slot", new Vector2(820f, row * 180f))),
            ["ExecuteAttack"] = new(JsonNodeActorScope.GroundOnly, (tree, action, row) => CreateExecuteAttackNode(tree, action.attackCategory, row)),
            ["Wait"] = new(JsonNodeActorScope.Common, (tree, action, row) => CreateWaitNode(tree, action.duration, row)),
            ["FlyingTransition"] = new(JsonNodeActorScope.FlyingOnly, (tree, action, row) => CreateFlyingTransitionNode(tree, action.state, row)),
            ["FlyingPatrolOrIdle"] = new(JsonNodeActorScope.FlyingOnly, (tree, _, row) => CreateFlyingPatrolOrIdleNode(tree, row)),
            ["ResetFlyingCounters"] = new(JsonNodeActorScope.FlyingOnly, (tree, _, row) => CreateNode<ResetFlyingCountersNode>(tree, "Reset Flying Counters", new Vector2(820f, row * 180f))),
            ["ResetFlyingAirCounters"] = new(JsonNodeActorScope.FlyingOnly, (tree, _, row) => CreateNode<ResetFlyingAirCountersNode>(tree, "Reset Flying Air Counters", new Vector2(820f, row * 180f))),
            ["DescendFlying"] = new(JsonNodeActorScope.FlyingOnly, (tree, _, row) => CreateNode<DescendFlyingNode>(tree, "Descend Flying", new Vector2(820f, row * 180f))),
            ["RequestFlyingAttackSlot"] = new(JsonNodeActorScope.FlyingOnly, (tree, _, row) => CreateNode<RequestFlyingAttackSlotNode>(tree, "Request Flying Attack Slot", new Vector2(820f, row * 180f))),
            ["SelectFlyingDiveSkill"] = new(JsonNodeActorScope.FlyingOnly, (tree, _, row) => CreateNode<SelectFlyingDiveSkillNode>(tree, "Select Flying Dive Skill", new Vector2(820f, row * 180f)))
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
                return CreateBlackboardCompareNode(tree, "Self.PhaseIndex", comparison, condition.value, null, row);

            return CreateBlackboardCompareNode(tree, "Self.PhaseName", comparison, condition.value, null, row);
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
            var node = CreateNode<BlackboardCompareNode>(tree, $"{key} {comparison} {right}", new Vector2(520f, row * 180f));
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

            var node = CreateNode<HasStateTagNode>(tree, (expected ? "Has " : "Has No ") + tag, new Vector2(520f, row * 180f));
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

            var attackCategory = EnemyAttackCategory.None;
            if (!string.IsNullOrWhiteSpace(action.attackCategory)
                && !Enum.TryParse(action.attackCategory, true, out attackCategory))
                throw new System.IO.InvalidDataException($"알 수 없는 EnemyAttackCategory입니다. {action.attackCategory}");

            var displayName = style == EnemyActionStyle.None
                ? $"Request {intent}"
                : $"Request {intent} {style}";
            var node = CreateNode<RequestEnemyActionNode>(tree, displayName, new Vector2(820f, row * 180f));
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
            patrolSequence.Children.Add(CreateBlackboardCompareNode(tree, "enablePatrol", BlackboardComparisonType.Equal, "true", null, row));
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
