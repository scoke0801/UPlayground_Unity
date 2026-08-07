#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public string actorKind = MonsterBehaviorJsonNodeKeys.ActorKinds.Ground;
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
        public List<MonsterBehaviorConditionJson> when = new();
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
        public string attackCategory;
        public string abilityRole;
        public string key;
        public string op;
        public string value;
        public string valueKey;
    }

    [Serializable]
    public class MonsterBehaviorActionJson
    {
        public string action;
        public string intent;
        public string style;
        public string state;
        public string attackCategory;
        public string abilityRole;
        public string cooldownId;
        public float cooldownDuration;
        public float duration;
    }

    [Serializable]
    public class MonsterBehaviorChoiceJson
    {
        public float weight = 1f;
        public string weightKey;
        public string action;
        public string intent;
        public string style;
        public string state;
        public string attackCategory;
        public string abilityRole;
        public string cooldownId;
        public float cooldownDuration;
    }

    public static partial class MonsterBehaviorTreeJsonImporter
    {
        private const int SupportedSchemaVersion = 1;
        private const string SourceRoot = "Assets/10.Datas/AI/BehaviorTree/SourceJson";
        private const string GeneratedRoot = "Assets/10.Datas/AI/BehaviorTree/Generated";

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/비헤이비어 트리/JSON/선택 JSON 가져오기", priority = UPlayGround.AI.Editor.BehaviorTreeMenuPriority.Json + 4)]
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

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/비헤이비어 트리/JSON/폴더 가져오기", priority = UPlayGround.AI.Editor.BehaviorTreeMenuPriority.Json + 5)]
        public static void ImportFolder()
        {
            var absoluteFolder = EditorUtility.OpenFolderPanel("Monster Behavior Json Folder Import", Application.dataPath, "");
            if (string.IsNullOrWhiteSpace(absoluteFolder))
                return;

            ImportJsonFolder(absoluteFolder, "폴더");
        }

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/비헤이비어 트리/JSON/Project 선택 JSON 가져오기", priority = UPlayGround.AI.Editor.BehaviorTreeMenuPriority.Json + 6)]
        public static void ImportSelectedProjectJsons()
        {
            ImportJsonFiles(GetSelectedJsonAssetPaths().Select(Path.GetFullPath), "선택 JSON");
        }

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/비헤이비어 트리/JSON/Project 선택 JSON 가져오기", true, UPlayGround.AI.Editor.BehaviorTreeMenuPriority.Json + 6)]
        public static bool CanImportSelectedProjectJsons()
        {
            return GetSelectedJsonAssetPaths().Count > 0;
        }

        [MenuItem("Assets/UPlayGround/AI/Import Monster Behavior Json To BT", false, 2200)]
        public static void ImportSelectedProjectJsonsFromAssetMenu()
        {
            ImportSelectedProjectJsons();
        }

        [MenuItem("Assets/UPlayGround/AI/Import Monster Behavior Json To BT", true)]
        public static bool CanImportSelectedProjectJsonsFromAssetMenu()
        {
            return CanImportSelectedProjectJsons();
        }

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/비헤이비어 트리/JSON/SourceJson 전체 가져오기", priority = UPlayGround.AI.Editor.BehaviorTreeMenuPriority.Json + 7)]
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

            try
            {
                tree = ScriptableObject.CreateInstance<BehaviorTreeAsset>();
                tree.name = finalAssetName;

                var root = CreateNode<SelectorNode>(tree, "Root", new Vector2(0f, 0f));
                root.Services.Add(CreateNode<SyncEnemyBlackboardService>(tree, "Sync Enemy Blackboard", new Vector2(-260f, -160f)));

                // 비행형은 EnemyTacticalMemory / EnemyAIContext 페이즈 모델을 쓰지 않으므로 Memory / Phase 서비스 미부착.
                var isFlying = string.Equals(data.actorKind, MonsterBehaviorJsonNodeKeys.ActorKinds.Flying, StringComparison.OrdinalIgnoreCase);
                if (!isFlying)
                {
                    root.Services.Add(CreateNode<SyncEnemyMemoryService>(tree, "Sync Enemy Memory", new Vector2(-260f, -100f)));
                    root.Services.Add(CreateNode<SyncEnemyPhaseService>(tree, "Sync Enemy Phase", new Vector2(-260f, -40f)));
                    root.Services.Add(CreateNode<EvaluateEnemyCombatIntentService>(tree, "Evaluate Enemy Combat Intent", new Vector2(-260f, 20f)));
                }
                tree.RootNode = root;

                AddDefaultBlackboard(tree, data, sourceBehavior);

                AddJsonDefinedChildren(tree, root, data, sourceBehavior);

                ApplyReadableLayout(tree);

                tree = PersistGeneratedAsset(tree, outputAssetPath);
            }
            catch
            {
                DestroyTransientTree(tree);
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

        private static BehaviorTreeAsset PersistGeneratedAsset(
            BehaviorTreeAsset generatedTree,
            string outputAssetPath)
        {
            var existingTree = AssetDatabase.LoadAssetAtPath<BehaviorTreeAsset>(outputAssetPath);
            if (existingTree == null)
            {
                try
                {
                    AssetDatabase.CreateAsset(generatedTree, outputAssetPath);
                    AddNodesToAsset(generatedTree.Nodes, generatedTree);
                    EditorUtility.SetDirty(generatedTree);
                    return generatedTree;
                }
                catch
                {
                    if (AssetDatabase.LoadAssetAtPath<BehaviorTreeAsset>(outputAssetPath) != null)
                        AssetDatabase.DeleteAsset(outputAssetPath);

                    throw;
                }
            }

            var oldName = existingTree.name;
            var oldRoot = existingTree.RootNode;
            var oldNodes = existingTree.Nodes.ToList();
            var oldBlackboard = existingTree.Blackboard.Clone();
            var oldGroups = CloneEditorGroups(existingTree.EditorGroups);
            var addedNodes = new List<BTNode>();

            // 옛 노드 파괴는 되돌릴 수 없으므로 커밋 지점(try 종료) 이후로 미룬다.
            // try 안에서 파괴하면 그 뒤 예외 시 catch가 이미 파괴된 oldNodes를 복원해
            // Missing 서브에셋으로 채워진 파손 트리가 남는다.
            try
            {
                foreach (var node in generatedTree.Nodes)
                {
                    if (node == null)
                        continue;

                    AssetDatabase.AddObjectToAsset(node, existingTree);
                    addedNodes.Add(node);
                    EditorUtility.SetDirty(node);
                }

                existingTree.name = generatedTree.name;
                existingTree.RootNode = generatedTree.RootNode;
                existingTree.Nodes.Clear();
                existingTree.Nodes.AddRange(generatedTree.Nodes);
                CopyBlackboard(generatedTree.Blackboard, existingTree.Blackboard);
                CopyEditorGroups(generatedTree.EditorGroups, existingTree.EditorGroups);
                EditorUtility.SetDirty(existingTree);
            }
            catch
            {
                existingTree.name = oldName;
                existingTree.RootNode = oldRoot;
                existingTree.Nodes.Clear();
                existingTree.Nodes.AddRange(oldNodes);
                CopyBlackboard(oldBlackboard, existingTree.Blackboard);
                CopyEditorGroups(oldGroups, existingTree.EditorGroups);

                foreach (var addedNode in addedNodes)
                {
                    if (addedNode != null)
                        UnityEngine.Object.DestroyImmediate(addedNode, true);
                }

                EditorUtility.SetDirty(existingTree);

                // 임시 트리는 노드를 existingTree에 넘겼다가 회수당했으므로 여기서 정리한다.
                UnityEngine.Object.DestroyImmediate(generatedTree);
                throw;
            }

            foreach (var oldNode in oldNodes)
            {
                if (oldNode != null)
                    UnityEngine.Object.DestroyImmediate(oldNode, true);
            }

            UnityEngine.Object.DestroyImmediate(generatedTree);
            return existingTree;
        }

        private static void AddNodesToAsset(
            IEnumerable<BTNode> nodes,
            BehaviorTreeAsset owner)
        {
            foreach (var node in nodes)
            {
                if (node == null)
                    continue;

                AssetDatabase.AddObjectToAsset(node, owner);
                EditorUtility.SetDirty(node);
            }
        }

        private static void CopyBlackboard(Blackboard source, Blackboard destination)
        {
            for (var i = destination.Entries.Count - 1; i >= 0; --i)
                destination.RemoveAt(i);

            foreach (var sourceEntry in source.Entries)
            {
                if (sourceEntry == null)
                    continue;

                destination.AddEntry(sourceEntry.KeyReference, sourceEntry.ValueType);
                var destinationEntry = destination.FindEntry(sourceEntry.KeyReference);
                if (destinationEntry == null)
                    throw new InvalidOperationException(
                        $"Blackboard Key를 복사할 수 없습니다: {sourceEntry.Key}");

                destinationEntry.BoolValue = sourceEntry.BoolValue;
                destinationEntry.IntValue = sourceEntry.IntValue;
                destinationEntry.FloatValue = sourceEntry.FloatValue;
                destinationEntry.StringValue = sourceEntry.StringValue;
                destinationEntry.Vector3Value = sourceEntry.Vector3Value;
                destinationEntry.ObjectValue = sourceEntry.ObjectValue;
            }
        }

        private static List<BehaviorTreeEditorGroup> CloneEditorGroups(
            IEnumerable<BehaviorTreeEditorGroup> source)
        {
            var clones = new List<BehaviorTreeEditorGroup>();
            CopyEditorGroups(source, clones);
            return clones;
        }

        private static void CopyEditorGroups(
            IEnumerable<BehaviorTreeEditorGroup> source,
            ICollection<BehaviorTreeEditorGroup> destination)
        {
            destination.Clear();
            foreach (var group in source)
            {
                if (group == null)
                    continue;

                destination.Add(new BehaviorTreeEditorGroup
                {
                    Guid = group.Guid,
                    Title = group.Title,
                    Rect = group.Rect,
                    Color = group.Color
                });
            }
        }

        private static void DestroyTransientTree(BehaviorTreeAsset tree)
        {
            if (tree == null || !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(tree)))
                return;

            for (var i = tree.Nodes.Count - 1; i >= 0; --i)
            {
                if (tree.Nodes[i] != null
                    && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(tree.Nodes[i])))
                {
                    UnityEngine.Object.DestroyImmediate(tree.Nodes[i]);
                }
            }

            UnityEngine.Object.DestroyImmediate(tree);
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
    }
}
#endif
