#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
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
        public List<MonsterBehaviorRuleGroupJson> groups = new();
        public List<MonsterBehaviorRuleJson> rules = new();
    }

    [Serializable]
    public class MonsterBehaviorRuleGroupJson
    {
        public string name;
        public int priority;
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
        public float aggression = 0.5f;
        public float reactionChance = 0.35f;
        public float counterChance = 0.2f;
        public float dodgeChance = 0.3f;
        public float punishRecoveryChance = 0.35f;
        public float antiGuardChance = 0.3f;
        public float minRetreatCooldown = 1.5f;
        public int maxComboPressureCount = 3;
        public float preferredRange = -1f;
        public int recentHitCount = 0;
        public string lastHitReactionType = "";
        public float poiseRatio = 1f;
        public bool isPoiseBroken = false;
        public float hitReactionLockTime = 0f;
        public float revengeChance = 0.25f;
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
        public string attackCategory;
        public float duration;
    }

    [Serializable]
    public class MonsterBehaviorChoiceJson
    {
        public float weight = 1f;
        public string weightKey;
        public string action;
        public string state;
        public string attackCategory;
    }

    public static class MonsterBehaviorTreeJsonImporter
    {
        private const int SupportedSchemaVersion = 1;
        private const string SourceRoot = "Assets/10.Datas/AI/BehaviorTree/SourceJson";
        private const string GeneratedRoot = "Assets/10.Datas/AI/BehaviorTree/Generated";
        private const float LayoutHorizontalSpacing = 300f;
        private const float LayoutVerticalSpacing = 170f;
        private const float LayoutServiceSpacing = 110f;

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

            ImportJsonFolder(absoluteFolder, "폴더");
        }

        [MenuItem("UPlayGround/Character/AI/Monster Behavior Json/Import Selected Project Jsons")]
        public static void ImportSelectedProjectJsons()
        {
            ImportJsonFiles(GetSelectedJsonAssetPaths().Select(Path.GetFullPath), "선택 JSON");
        }

        [MenuItem("UPlayGround/Character/AI/Monster Behavior Json/Import Selected Project Jsons", true)]
        public static bool CanImportSelectedProjectJsons()
        {
            return GetSelectedJsonAssetPaths().Count > 0;
        }

        [MenuItem("UPlayGround/Character/AI/Monster Behavior Json/Import All SourceJson")]
        public static void ImportAllSourceJson()
        {
            ImportJsonFolder(Path.GetFullPath(SourceRoot), "SourceJson 전체");
        }

        public static IReadOnlyList<BehaviorTreeAsset> ImportJsonFolder(string absoluteFolder)
        {
            return ImportJsonFolder(absoluteFolder, "폴더");
        }

        public static IReadOnlyList<BehaviorTreeAsset> ImportJsonFiles(IEnumerable<string> absoluteJsonPaths)
        {
            return ImportJsonFiles(absoluteJsonPaths, "JSON 목록");
        }

        private static IReadOnlyList<BehaviorTreeAsset> ImportJsonFolder(string absoluteFolder, string label)
        {
            if (string.IsNullOrWhiteSpace(absoluteFolder) || !Directory.Exists(absoluteFolder))
            {
                Debug.LogError($"[BT] Monster Behavior Json {label} Import 실패: 폴더를 찾을 수 없습니다. {absoluteFolder}");
                return Array.Empty<BehaviorTreeAsset>();
            }

            var jsonPaths = Directory
                .GetFiles(absoluteFolder, "*.json", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

            return ImportJsonFiles(jsonPaths, label);
        }

        private static IReadOnlyList<BehaviorTreeAsset> ImportJsonFiles(IEnumerable<string> absoluteJsonPaths, string label)
        {
            var jsonPaths = absoluteJsonPaths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(Path.GetFullPath)
                .Where(path => string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (jsonPaths.Count == 0)
            {
                Debug.LogWarning($"[BT] Monster Behavior Json {label} Import 대상이 없습니다.");
                return Array.Empty<BehaviorTreeAsset>();
            }

            var importedTrees = new List<BehaviorTreeAsset>();
            var failures = new List<string>();

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var jsonPath in jsonPaths)
                {
                    try
                    {
                        var data = LoadJson(jsonPath);
                        var tree = ImportFromMonsterBehaviorJsonInternal(jsonPath, GetGeneratedAssetPath(data), false);
                        importedTrees.Add(tree);
                    }
                    catch (Exception exception)
                    {
                        failures.Add($"{jsonPath}: {exception.Message}");
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            if (importedTrees.Count > 0)
                EditorGUIUtility.PingObject(importedTrees[^1]);

            if (failures.Count > 0)
                Debug.LogError($"[BT] Monster Behavior Json {label} Import 일부 실패: 성공 {importedTrees.Count}개 / 실패 {failures.Count}개\n{string.Join("\n", failures)}");
            else
                Debug.Log($"[BT] Monster Behavior Json {label} Import 완료: {importedTrees.Count}개");

            return importedTrees;
        }

        public static BehaviorTreeAsset ImportFromMonsterBehaviorJson(string absoluteJsonPath, string outputAssetPath)
        {
            return ImportFromMonsterBehaviorJsonInternal(absoluteJsonPath, outputAssetPath, true);
        }

        private static BehaviorTreeAsset ImportFromMonsterBehaviorJsonInternal(string absoluteJsonPath, string outputAssetPath, bool saveAndRefresh)
        {
            var data = LoadJson(absoluteJsonPath);
            Validate(data, absoluteJsonPath);

            if (string.IsNullOrWhiteSpace(outputAssetPath))
                outputAssetPath = GetGeneratedAssetPath(data);

            var sourceBehavior = LoadSourceBehavior(data);
            EnsureAssetDirectory(outputAssetPath);

            var finalAssetName = Path.GetFileNameWithoutExtension(outputAssetPath);
            BehaviorTreeAsset tree = null;
            var persistedOutputAsset = false;

            try
            {
                tree = ScriptableObject.CreateInstance<BehaviorTreeAsset>();
                tree.name = finalAssetName;

                var root = CreateNode<SelectorNode>(tree, "Root", new Vector2(0f, 0f));
                root.Services.Add(CreateNode<SyncEnemyBlackboardService>(tree, "Sync Enemy Blackboard", new Vector2(-260f, -160f)));

                // 비행형은 EnemyTacticalMemory / EnemyAIContext 페이즈 모델을 쓰지 않으므로 Memory / Phase 서비스 미부착.
                var isFlying = string.Equals(data.actorKind, "Flying", StringComparison.OrdinalIgnoreCase);
                if (!isFlying)
                {
                    root.Services.Add(CreateNode<SyncEnemyMemoryService>(tree, "Sync Enemy Memory", new Vector2(-260f, -100f)));
                    root.Services.Add(CreateNode<SyncEnemyPhaseService>(tree, "Sync Enemy Phase", new Vector2(-260f, -40f)));
                }
                tree.RootNode = root;

                AddDefaultBlackboard(tree, data, sourceBehavior);

                AddJsonDefinedChildren(tree, root, data, sourceBehavior);

                ApplyTopDownLayout(tree);

                PersistGeneratedAsset(tree, outputAssetPath);
                persistedOutputAsset = true;
                tree = AssetDatabase.LoadAssetAtPath<BehaviorTreeAsset>(outputAssetPath) ?? tree;
            }
            catch
            {
                var treeAssetPath = tree != null ? AssetDatabase.GetAssetPath(tree) : null;
                if ((persistedOutputAsset || treeAssetPath == outputAssetPath)
                    && AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(outputAssetPath) != null)
                {
                    AssetDatabase.DeleteAsset(outputAssetPath);
                }
                else if (tree != null)
                {
                    for (var i = tree.Nodes.Count - 1; i >= 0; --i)
                    {
                        if (tree.Nodes[i] != null)
                            UnityEngine.Object.DestroyImmediate(tree.Nodes[i]);
                    }

                    UnityEngine.Object.DestroyImmediate(tree);
                }

                throw;
            }

            if (saveAndRefresh)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[BT] Monster Behavior Json Import 완료: {outputAssetPath}");
            return tree;
        }

        private static void PersistGeneratedAsset(BehaviorTreeAsset tree, string outputAssetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<BehaviorTreeAsset>(outputAssetPath) != null
                && !AssetDatabase.DeleteAsset(outputAssetPath))
            {
                throw new IOException($"기존 BehaviorTreeAsset을 삭제할 수 없습니다. {outputAssetPath}");
            }

            AssetDatabase.CreateAsset(tree, outputAssetPath);

            foreach (var node in tree.Nodes)
            {
                if (node == null)
                    continue;

                AssetDatabase.AddObjectToAsset(node, tree);
                EditorUtility.SetDirty(node);
            }

            EditorUtility.SetDirty(tree);
        }

        private static void ApplyTopDownLayout(BehaviorTreeAsset tree)
        {
            if (tree?.RootNode == null)
                return;

            LayoutTreeTopDown(tree.RootNode, 0f, 0f, new HashSet<BTNode>());
            LayoutServices(tree);
        }

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
                    var groupNode = CreateNode<SelectorNode>(tree, group.name, Vector2.zero);
                    var orderedRules = group.rules
                        .OrderByDescending(rule => rule.priority)
                        .ThenBy(rule => group.rules.IndexOf(rule))
                        .ToList();

                    foreach (var rule in orderedRules)
                    {
                        var child = CreateRuleNode(tree, rule, sourceBehavior, data.blackboard, row++);
                        if (child != null)
                            groupNode.Children.Add(child);
                    }

                    if (groupNode.Children.Count > 0)
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

        private static float LayoutTreeTopDown(BTNode node, float leftX, float y, HashSet<BTNode> visited)
        {
            if (node == null || !visited.Add(node))
                return 0f;

            var children = node.Children?.Where(child => child != null).ToList() ?? new List<BTNode>();
            if (children.Count == 0)
            {
                node.EditorPosition = new Vector2(leftX, y);
                return LayoutHorizontalSpacing;
            }

            var currentX = leftX;
            var totalWidth = 0f;
            foreach (var child in children)
            {
                var childWidth = LayoutTreeTopDown(child, currentX, y + LayoutVerticalSpacing, visited);
                currentX += childWidth;
                totalWidth += childWidth;
            }

            node.EditorPosition = new Vector2(leftX + totalWidth * 0.5f - LayoutHorizontalSpacing * 0.5f, y);
            return Mathf.Max(LayoutHorizontalSpacing, totalWidth);
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

        private static void LayoutServices(BehaviorTreeAsset tree)
        {
            foreach (var composite in tree.Nodes.OfType<BTCompositeNode>())
            {
                var services = composite.Services?.Where(service => service != null).ToList();
                if (services == null || services.Count == 0)
                    continue;

                var startOffset = -(services.Count - 1) * LayoutServiceSpacing * 0.5f;
                for (var i = 0; i < services.Count; i++)
                {
                    services[i].EditorPosition = composite.EditorPosition + new Vector2(
                        -LayoutHorizontalSpacing,
                        startOffset + i * LayoutServiceSpacing);
                }
            }
        }

        private static List<string> GetSelectedJsonAssetPaths()
        {
            return Selection.objects
                .Select(AssetDatabase.GetAssetPath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Where(path => string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase))
                .Where(path => AssetDatabase.LoadAssetAtPath<TextAsset>(path) != null)
                .ToList();
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

        private enum ActorKind { Ground, Flying }

        private static ActorKind ResolveActorKind(string raw)
        {
            if (string.Equals(raw, "Flying", StringComparison.OrdinalIgnoreCase))
                return ActorKind.Flying;
            return ActorKind.Ground;
        }

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

        private static void ValidateCondition(MonsterBehaviorConditionJson condition, string ruleName, ActorKind actorKind)
        {
            var known = condition.condition is "HasTarget" or "IsBlockedEnemyState" or "IsEnemyPhase" or "DistanceLessOrEqual"
                or "DistanceGreater" or "ActionDelayElapsed" or "CanUseSkill" or "IsPlayerAttacking"
                or "IsPlayerGuarding" or "IsPlayerStaggered" or "IsPlayerRecovering" or "IsPlayerDodgingFrequently"
                or "IsSelfLowHealth" or "HasAttackSlot" or "CooldownReady" or "RecentlyHitByPlayer"
                or "WasLastHitHeavy" or "IsPoiseBroken" or "RecentHitCountGreaterOrEqual"
                or "CanIgnoreLightHit" or "CanRevengeAfterHit"
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
                "IsEnemyPhase" => CreateEnemyPhaseNode(tree, condition.value, row),
                "DistanceLessOrEqual" => CreateRangeNode(tree, FloatComparisonType.LessOrEqual, condition.value, sourceBehavior, row),
                "DistanceGreater" => CreateRangeNode(tree, FloatComparisonType.GreaterOrEqual, condition.value, sourceBehavior, row),
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
                "Transition" => CreateTransitionNode(tree, action.state, row),
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
                new MonsterBehaviorActionJson { action = choice.action, state = choice.state, attackCategory = choice.attackCategory },
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

        private static TransitionFlyingEnemyStateNode CreateFlyingTransitionNode(BehaviorTreeAsset tree, string state, int row)
        {
            var node = CreateNode<TransitionFlyingEnemyStateNode>(tree, "Flying Transition " + state, new Vector2(820f, row * 180f));
            node.TargetState = Enum.Parse<FlyingEnemyTransitionStateType>(state);
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

        private static void AddDefaultBlackboard(BehaviorTreeAsset tree, MonsterBehaviorTreeJson data, EnemyBehaviorSO sourceBehavior)
        {
            var blackboard = tree.Blackboard;
            blackboard.SetBool(EnemyBlackboardKeys.HasTarget, false);
            blackboard.SetObject(EnemyBlackboardKeys.Target, null);
            blackboard.SetFloat(EnemyBlackboardKeys.DistanceToTarget, float.MaxValue);
            blackboard.SetString(EnemyBlackboardKeys.CurrentState, "");
            blackboard.SetFloat(EnemyBlackboardKeys.HpPercent, 1f);
            blackboard.SetString(EnemyBlackboardKeys.CurrentPhaseName, "");
            blackboard.SetInt(EnemyBlackboardKeys.PhaseIndex, -1);
            blackboard.SetBool(EnemyBlackboardKeys.AllowCharge, false);
            blackboard.SetBool(EnemyBlackboardKeys.AllowFlank, false);
            blackboard.SetInt(EnemyBlackboardKeys.MaxConsecutiveAttacks, 3);
            blackboard.SetFloat(EnemyBlackboardKeys.ContinueAttackChance, sourceBehavior?.continueAttackChance ?? 0.3f);
            blackboard.SetFloat(EnemyBlackboardKeys.GuardChance, sourceBehavior?.guardChance ?? 0.25f);
            blackboard.SetFloat(EnemyBlackboardKeys.RetreatChance, sourceBehavior?.retreatChance ?? 0.2f);
            blackboard.SetBool(EnemyBlackboardKeys.IsPlayerAttacking, false);
            blackboard.SetBool(EnemyBlackboardKeys.IsPlayerGuarding, false);
            blackboard.SetBool(EnemyBlackboardKeys.IsPlayerStaggered, false);
            blackboard.SetBool(EnemyBlackboardKeys.IsPlayerRecovering, false);
            blackboard.SetBool(EnemyBlackboardKeys.IsPlayerDodgingFrequently, false);
            blackboard.SetBool(EnemyBlackboardKeys.CanUseSkill, false);
            blackboard.SetBool(EnemyBlackboardKeys.HasAttackSlot, false);
            blackboard.SetFloat(EnemyBlackboardKeys.NextActionAllowedTime, 0f);
            blackboard.SetFloat(EnemyBlackboardKeys.Aggression, Mathf.Clamp01(data.blackboard.aggression));
            blackboard.SetFloat(EnemyBlackboardKeys.ReactionChance, Mathf.Clamp01(data.blackboard.reactionChance));
            blackboard.SetFloat(EnemyBlackboardKeys.CounterChance, Mathf.Clamp01(data.blackboard.counterChance));
            blackboard.SetFloat(EnemyBlackboardKeys.DodgeChance, Mathf.Clamp01(data.blackboard.dodgeChance));
            blackboard.SetFloat(EnemyBlackboardKeys.PunishRecoveryChance, Mathf.Clamp01(data.blackboard.punishRecoveryChance));
            blackboard.SetFloat(EnemyBlackboardKeys.AntiGuardChance, Mathf.Clamp01(data.blackboard.antiGuardChance));
            blackboard.SetFloat(EnemyBlackboardKeys.MinRetreatCooldown, Mathf.Max(0f, data.blackboard.minRetreatCooldown));
            blackboard.SetInt(EnemyBlackboardKeys.MaxComboPressureCount, Mathf.Max(0, data.blackboard.maxComboPressureCount));
            blackboard.SetFloat(EnemyBlackboardKeys.PreferredRange, ResolveBlackboardValue(data.blackboard.preferredRange, data.blackboard.optimalCombatDistance >= 0f ? data.blackboard.optimalCombatDistance : sourceBehavior?.optimalCombatDistance ?? 2.5f));
            blackboard.SetBool(EnemyBlackboardKeys.RecentlyHitByPlayer, false);
            blackboard.SetInt(EnemyBlackboardKeys.RecentHitCount, Mathf.Max(0, data.blackboard.recentHitCount));
            blackboard.SetString(EnemyBlackboardKeys.LastHitReactionType, data.blackboard.lastHitReactionType ?? "");
            blackboard.SetFloat(EnemyBlackboardKeys.PoiseRatio, Mathf.Clamp01(data.blackboard.poiseRatio));
            blackboard.SetBool(EnemyBlackboardKeys.IsPoiseBroken, data.blackboard.isPoiseBroken);
            blackboard.SetFloat(EnemyBlackboardKeys.HitReactionLockTime, Mathf.Max(0f, data.blackboard.hitReactionLockTime));
            blackboard.SetFloat(EnemyBlackboardKeys.RevengeChance, Mathf.Clamp01(data.blackboard.revengeChance));

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

        private static float ResolveWeight(MonsterBehaviorChoiceJson choice, EnemyBehaviorSO sourceBehavior, MonsterBehaviorBlackboardJson blackboard)
        {
            return string.IsNullOrWhiteSpace(choice.weightKey)
                ? Mathf.Max(0f, choice.weight)
                : Mathf.Max(0f, ResolveFloat(choice.weightKey, sourceBehavior, blackboard, 1f));
        }

        private static float ResolveFloat(string keyOrValue, EnemyBehaviorSO sourceBehavior, MonsterBehaviorBlackboardJson blackboard, float fallback)
        {
            if (string.IsNullOrWhiteSpace(keyOrValue))
                return fallback;

            if (float.TryParse(keyOrValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var numeric))
                return numeric;

            if (blackboard != null)
            {
                var blackboardField = typeof(MonsterBehaviorBlackboardJson).GetField(keyOrValue, BindingFlags.Instance | BindingFlags.Public);
                if (blackboardField != null)
                {
                    if (blackboardField.FieldType == typeof(float))
                        return (float)blackboardField.GetValue(blackboard);
                    if (blackboardField.FieldType == typeof(int))
                        return (int)blackboardField.GetValue(blackboard);
                }
            }

            return ResolveFloat(keyOrValue, sourceBehavior, fallback);
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
