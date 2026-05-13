#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UPlayGround.Data.Enemy;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    [Serializable]
    public class MonsterBehaviorTreeJson
    {
        public int schemaVersion = 1;
        public string id;
        public string displayName;
        public string actorKind = "Ground";
        public string sourceBehaviorSo;
        public MonsterBehaviorBlackboardJson blackboard = new();
        public List<MonsterBehaviorRuleJson> rules = new();
    }

    [Serializable]
    public class MonsterBehaviorBlackboardJson
    {
        public float tickInterval = 0.1f;
        public bool enablePatrol = true;
        public float optimalCombatDistance = -1f;
        public float minCombatDistance = -1f;
        public float personalSpaceDistance = -1f;
        public float guardChance = -1f;
        public float retreatChance = -1f;
        public float circleWeight = 1f;
    }

    [Serializable]
    public class MonsterBehaviorRuleJson
    {
        public string name;
        public int priority;
        public string select;
        public List<MonsterBehaviorConditionJson> when = new();
        public List<MonsterBehaviorActionJson> @do = new();
        public List<MonsterBehaviorChoiceJson> choices = new();
    }

    [Serializable]
    public class MonsterBehaviorConditionJson
    {
        public string condition;
        public bool invert;
        public string value;
    }

    [Serializable]
    public class MonsterBehaviorActionJson
    {
        public string action;
        public string state;
        public float duration;
    }

    [Serializable]
    public class MonsterBehaviorChoiceJson
    {
        public float weight = 1f;
        public string weightKey;
        public string action;
        public string state;
    }

    public static class MonsterBehaviorTreeJsonImporter
    {
        private const int SupportedSchemaVersion = 1;
        private const string SourceRoot = "Assets/10.Datas/AI/BehaviorTree/SourceJson";
        private const string GeneratedRoot = "Assets/10.Datas/AI/BehaviorTree/Generated";

        [MenuItem("UPlayGround/Character/AI/Monster Behavior Json/Import Selected Json")]
        public static void ImportSelectedJson()
        {
            var jsonPath = EditorUtility.OpenFilePanel("Monster Behavior Json Import", Application.dataPath, "json");
            if (string.IsNullOrWhiteSpace(jsonPath))
                return;

            var data = LoadJson(jsonPath);
            var assetPath = GetGeneratedAssetPath(data);
            var tree = ImportFromMonsterBehaviorJson(jsonPath, assetPath);
            EditorGUIUtility.PingObject(tree);
            BehaviorTreeEditorWindow.Open(tree);
        }

        [MenuItem("UPlayGround/Character/AI/Monster Behavior Json/Import Folder")]
        public static void ImportFolder()
        {
            var absoluteFolder = EditorUtility.OpenFolderPanel("Monster Behavior Json Folder Import", Application.dataPath, "");
            if (string.IsNullOrWhiteSpace(absoluteFolder))
                return;

            var importedCount = 0;
            foreach (var jsonPath in Directory.GetFiles(absoluteFolder, "*.json", SearchOption.AllDirectories))
            {
                var data = LoadJson(jsonPath);
                ImportFromMonsterBehaviorJson(jsonPath, GetGeneratedAssetPath(data));
                importedCount++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"[BT] Monster Behavior Json 폴더 Import 완료: {importedCount}개");
        }

        public static BehaviorTreeAsset ImportFromMonsterBehaviorJson(string absoluteJsonPath, string outputAssetPath)
        {
            var data = LoadJson(absoluteJsonPath);
            Validate(data, absoluteJsonPath);

            var sourceBehavior = LoadSourceBehavior(data);
            EnsureAssetDirectory(outputAssetPath);

            if (AssetDatabase.LoadAssetAtPath<BehaviorTreeAsset>(outputAssetPath) != null)
                AssetDatabase.DeleteAsset(outputAssetPath);

            var tree = ScriptableObject.CreateInstance<BehaviorTreeAsset>();
            tree.name = Path.GetFileNameWithoutExtension(outputAssetPath);
            AssetDatabase.CreateAsset(tree, outputAssetPath);

            var root = CreateNode<SelectorNode>(tree, "Root", new Vector2(0f, 0f));
            root.Services.Add(CreateNode<SyncEnemyBlackboardService>(tree, "Sync Enemy Blackboard", new Vector2(-260f, -160f)));
            tree.RootNode = root;

            AddDefaultBlackboard(tree, data, sourceBehavior);

            var orderedRules = data.rules
                .OrderByDescending(rule => rule.priority)
                .ThenBy(rule => data.rules.IndexOf(rule))
                .ToList();

            for (var i = 0; i < orderedRules.Count; i++)
            {
                var child = CreateRuleNode(tree, orderedRules[i], sourceBehavior, i);
                if (child != null)
                    root.Children.Add(child);
            }

            EditorUtility.SetDirty(tree);
            foreach (var node in tree.Nodes)
                EditorUtility.SetDirty(node);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BT] Monster Behavior Json Import 완료: {outputAssetPath}");
            return tree;
        }

        private static MonsterBehaviorTreeJson LoadJson(string absoluteJsonPath)
        {
            var json = File.ReadAllText(absoluteJsonPath);
            return JsonUtility.FromJson<MonsterBehaviorTreeJson>(json);
        }

        private static void Validate(MonsterBehaviorTreeJson data, string sourcePath)
        {
            if (data == null)
                throw new InvalidDataException($"Monster Behavior Json을 읽을 수 없습니다: {sourcePath}");

            if (data.schemaVersion != SupportedSchemaVersion)
                throw new InvalidDataException($"지원하지 않는 schemaVersion입니다: {data.schemaVersion}");

            if (string.IsNullOrWhiteSpace(data.id))
                throw new InvalidDataException("id가 비어 있습니다.");

            if (data.rules == null || data.rules.Count == 0)
                throw new InvalidDataException($"{data.id}: rules가 비어 있습니다.");

            foreach (var rule in data.rules)
            {
                if (string.IsNullOrWhiteSpace(rule.name))
                    throw new InvalidDataException($"{data.id}: name이 비어 있는 rule이 있습니다.");

                foreach (var condition in rule.when ?? new List<MonsterBehaviorConditionJson>())
                    ValidateCondition(condition, rule.name);

                foreach (var action in rule.@do ?? new List<MonsterBehaviorActionJson>())
                    ValidateAction(action, rule.name);

                foreach (var choice in rule.choices ?? new List<MonsterBehaviorChoiceJson>())
                    ValidateChoice(choice, rule.name);
            }
        }

        private static void ValidateCondition(MonsterBehaviorConditionJson condition, string ruleName)
        {
            var known = condition.condition is "HasTarget" or "IsBlockedEnemyState" or "DistanceLessOrEqual"
                or "DistanceGreater" or "ActionDelayElapsed" or "CanUseSkill" or "IsPlayerAttacking"
                or "IsPlayerGuarding" or "IsPlayerStaggered" or "IsPlayerRecovering" or "IsPlayerDodgingFrequently";

            if (!known)
                throw new InvalidDataException($"{ruleName}: 알 수 없는 condition입니다. {condition.condition}");
        }

        private static void ValidateAction(MonsterBehaviorActionJson action, string ruleName)
        {
            var known = action.action is "KeepCurrentState" or "PatrolOrIdle" or "Transition"
                or "RequestAttackSlot" or "ExecuteAttack" or "Wait";

            if (!known)
                throw new InvalidDataException($"{ruleName}: 알 수 없는 action입니다. {action.action}");

            if (action.action == "Transition" && !Enum.TryParse<EnemyTransitionStateType>(action.state, out _))
                throw new InvalidDataException($"{ruleName}: 알 수 없는 EnemyTransitionStateType입니다. {action.state}");
        }

        private static void ValidateChoice(MonsterBehaviorChoiceJson choice, string ruleName)
        {
            ValidateAction(new MonsterBehaviorActionJson { action = choice.action, state = choice.state }, ruleName);
        }

        private static BTNode CreateRuleNode(
            BehaviorTreeAsset tree,
            MonsterBehaviorRuleJson rule,
            EnemyBehaviorSO sourceBehavior,
            int index)
        {
            var sequence = CreateNode<SequenceNode>(tree, rule.name, new Vector2(260f, index * 180f));

            foreach (var condition in rule.when ?? new List<MonsterBehaviorConditionJson>())
            {
                var conditionNode = CreateConditionNode(tree, condition, sourceBehavior, index);
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
                    selector.SetWeight(selector.Children.Count - 1, ResolveWeight(choice, sourceBehavior));
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

        private static BTNode CreateConditionNode(
            BehaviorTreeAsset tree,
            MonsterBehaviorConditionJson condition,
            EnemyBehaviorSO sourceBehavior,
            int row)
        {
            BTNode node = condition.condition switch
            {
                "HasTarget" => CreateHasTargetNode(tree, !condition.invert, row),
                "IsBlockedEnemyState" => CreateNode<IsBlockedEnemyStateNode>(tree, "Is Blocked Enemy State", new Vector2(520f, row * 180f)),
                "DistanceLessOrEqual" => CreateRangeNode(tree, FloatComparisonType.LessOrEqual, condition.value, sourceBehavior, row),
                "DistanceGreater" => CreateRangeNode(tree, FloatComparisonType.GreaterOrEqual, condition.value, sourceBehavior, row),
                "ActionDelayElapsed" => CreateNode<HasEnemyActionDelayElapsedNode>(tree, "Action Delay Elapsed", new Vector2(520f, row * 180f)),
                "CanUseSkill" => CreateNode<CanUseEnemySkillNode>(tree, "Can Use Enemy Skill", new Vector2(520f, row * 180f)),
                "IsPlayerAttacking" => CreateBlackboardBoolNode(tree, EnemyBlackboardKeys.IsPlayerAttacking, !condition.invert, row),
                "IsPlayerGuarding" => CreateBlackboardBoolNode(tree, EnemyBlackboardKeys.IsPlayerGuarding, !condition.invert, row),
                "IsPlayerStaggered" => CreateBlackboardBoolNode(tree, EnemyBlackboardKeys.IsPlayerStaggered, !condition.invert, row),
                "IsPlayerRecovering" => CreateBlackboardBoolNode(tree, EnemyBlackboardKeys.IsPlayerRecovering, !condition.invert, row),
                "IsPlayerDodgingFrequently" => CreateBlackboardBoolNode(tree, EnemyBlackboardKeys.IsPlayerDodgingFrequently, !condition.invert, row),
                _ => null
            };

            return condition.invert && condition.condition is not ("HasTarget" or "IsPlayerAttacking" or "IsPlayerGuarding" or "IsPlayerStaggered" or "IsPlayerRecovering" or "IsPlayerDodgingFrequently")
                ? WrapInverter(tree, node, row)
                : node;
        }

        private static BTNode CreateActionNode(BehaviorTreeAsset tree, MonsterBehaviorActionJson action, int row)
        {
            return action.action switch
            {
                "KeepCurrentState" => CreateNode<KeepCurrentStateNode>(tree, "Keep Current State", new Vector2(820f, row * 180f)),
                "PatrolOrIdle" => CreatePatrolOrIdleNode(tree, row),
                "Transition" => CreateTransitionNode(tree, action.state, row),
                "RequestAttackSlot" => CreateNode<RequestEnemyAttackSlotNode>(tree, "Request Attack Slot", new Vector2(820f, row * 180f)),
                "ExecuteAttack" => CreateNode<ExecuteEnemyAttackNode>(tree, "Execute Attack", new Vector2(820f, row * 180f)),
                "Wait" => CreateWaitNode(tree, action.duration, row),
                _ => null
            };
        }

        private static BTNode CreateChoiceActionNode(BehaviorTreeAsset tree, MonsterBehaviorChoiceJson choice, int row, int column)
        {
            return CreateActionNode(
                tree,
                new MonsterBehaviorActionJson { action = choice.action, state = choice.state },
                row + column);
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
            int row)
        {
            var node = CreateNode<IsTargetInRangeNode>(tree, comparison.ToString(), new Vector2(520f, row * 180f));
            node.Comparison = comparison;
            var resolved = ResolveFloat(value, sourceBehavior, 0f);
            if (comparison == FloatComparisonType.LessOrEqual)
                node.MaxDistance = resolved;
            else
                node.MinDistance = resolved;

            return node;
        }

        private static BlackboardBoolConditionNode CreateBlackboardBoolNode(BehaviorTreeAsset tree, string key, bool expected, int row)
        {
            var node = CreateNode<BlackboardBoolConditionNode>(tree, key, new Vector2(520f, row * 180f));
            SetPrivateField(node, "_key", key);
            SetPrivateField(node, "_expectedValue", expected);
            return node;
        }

        private static TransitionEnemyStateNode CreateTransitionNode(BehaviorTreeAsset tree, string state, int row)
        {
            var node = CreateNode<TransitionEnemyStateNode>(tree, "Transition " + state, new Vector2(820f, row * 180f));
            node.TargetState = Enum.Parse<EnemyTransitionStateType>(state);
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
            AssetDatabase.AddObjectToAsset(node, tree);
            tree.Nodes.Add(node);
            return node;
        }

        private static void AddDefaultBlackboard(BehaviorTreeAsset tree, MonsterBehaviorTreeJson data, EnemyBehaviorSO sourceBehavior)
        {
            var blackboard = tree.Blackboard;
            blackboard.SetBool(EnemyBlackboardKeys.HasTarget, false);
            blackboard.SetFloat(EnemyBlackboardKeys.DistanceToTarget, float.MaxValue);
            blackboard.SetString(EnemyBlackboardKeys.CurrentState, "");
            blackboard.SetBool(EnemyBlackboardKeys.HasAttackSlot, false);
            blackboard.SetFloat(EnemyBlackboardKeys.NextActionAllowedTime, 0f);

            blackboard.SetBool("enablePatrol", data.blackboard.enablePatrol);
            blackboard.SetFloat("optimalCombatDistance", ResolveBlackboardValue(data.blackboard.optimalCombatDistance, sourceBehavior?.optimalCombatDistance ?? 2.5f));
            blackboard.SetFloat("minCombatDistance", ResolveBlackboardValue(data.blackboard.minCombatDistance, sourceBehavior?.minCombatDistance ?? 1.5f));
            blackboard.SetFloat("personalSpaceDistance", ResolveBlackboardValue(data.blackboard.personalSpaceDistance, sourceBehavior?.personalSpaceDistance ?? 0.8f));
            blackboard.SetFloat("guardChance", ResolveBlackboardValue(data.blackboard.guardChance, sourceBehavior?.guardChance ?? 0.25f));
            blackboard.SetFloat("retreatChance", ResolveBlackboardValue(data.blackboard.retreatChance, sourceBehavior?.retreatChance ?? 0.2f));
            blackboard.SetFloat("circleWeight", Mathf.Max(0f, data.blackboard.circleWeight));
        }

        private static float ResolveBlackboardValue(float value, float fallback)
        {
            return value >= 0f ? value : fallback;
        }

        private static float ResolveFloat(string keyOrValue, EnemyBehaviorSO sourceBehavior, float fallback)
        {
            if (string.IsNullOrWhiteSpace(keyOrValue))
                return fallback;

            if (float.TryParse(keyOrValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var numeric))
                return numeric;

            if (sourceBehavior != null)
            {
                var field = typeof(EnemyBehaviorSO).GetField(keyOrValue, BindingFlags.Instance | BindingFlags.Public);
                if (field != null && field.FieldType == typeof(float))
                    return (float)field.GetValue(sourceBehavior);
            }

            return fallback;
        }

        private static float ResolveWeight(MonsterBehaviorChoiceJson choice, EnemyBehaviorSO sourceBehavior)
        {
            return string.IsNullOrWhiteSpace(choice.weightKey)
                ? Mathf.Max(0f, choice.weight)
                : Mathf.Max(0f, ResolveFloat(choice.weightKey, sourceBehavior, 1f));
        }

        private static EnemyBehaviorSO LoadSourceBehavior(MonsterBehaviorTreeJson data)
        {
            if (string.IsNullOrWhiteSpace(data.sourceBehaviorSo))
                return null;

            var source = AssetDatabase.LoadAssetAtPath<EnemyBehaviorSO>(data.sourceBehaviorSo);
            if (source == null)
                Debug.LogWarning($"[BT] sourceBehaviorSo를 찾을 수 없습니다: {data.sourceBehaviorSo}");

            return source;
        }

        private static string GetGeneratedAssetPath(MonsterBehaviorTreeJson data)
        {
            var id = string.IsNullOrWhiteSpace(data?.id) ? "MonsterBehavior" : data.id;
            return $"{GeneratedRoot}/BT_{id}.asset";
        }

        private static void EnsureAssetDirectory(string assetPath)
        {
            var directory = Path.GetDirectoryName(assetPath);
            if (string.IsNullOrWhiteSpace(directory) || AssetDatabase.IsValidFolder(directory))
                return;

            Directory.CreateDirectory(directory);
            AssetDatabase.Refresh();
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }
    }
}
#endif
