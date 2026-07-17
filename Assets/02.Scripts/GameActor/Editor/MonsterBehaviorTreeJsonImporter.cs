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
        public string cooldownId;
        public float cooldownDuration;
    }

    public static partial class MonsterBehaviorTreeJsonImporter
    {
        private const int SupportedSchemaVersion = 1;
        private const string SourceRoot = "Assets/10.Datas/AI/BehaviorTree/SourceJson";
        private const string GeneratedRoot = "Assets/10.Datas/AI/BehaviorTree/Generated";

        [MenuItem("UPlayGround/비헤이비어 트리/JSON/선택 JSON 가져오기", priority = UPlayGround.AI.Editor.BehaviorTreeMenuPriority.Json + 4)]
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

        [MenuItem("UPlayGround/비헤이비어 트리/JSON/폴더 가져오기", priority = UPlayGround.AI.Editor.BehaviorTreeMenuPriority.Json + 5)]
        public static void ImportFolder()
        {
            var absoluteFolder = EditorUtility.OpenFolderPanel("Monster Behavior Json Folder Import", Application.dataPath, "");
            if (string.IsNullOrWhiteSpace(absoluteFolder))
                return;

            ImportJsonFolder(absoluteFolder, "폴더");
        }

        [MenuItem("UPlayGround/비헤이비어 트리/JSON/Project 선택 JSON 가져오기", priority = UPlayGround.AI.Editor.BehaviorTreeMenuPriority.Json + 6)]
        public static void ImportSelectedProjectJsons()
        {
            ImportJsonFiles(GetSelectedJsonAssetPaths().Select(Path.GetFullPath), "선택 JSON");
        }

        [MenuItem("UPlayGround/비헤이비어 트리/JSON/Project 선택 JSON 가져오기", true, UPlayGround.AI.Editor.BehaviorTreeMenuPriority.Json + 6)]
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

        [MenuItem("UPlayGround/비헤이비어 트리/JSON/SourceJson 전체 가져오기", priority = UPlayGround.AI.Editor.BehaviorTreeMenuPriority.Json + 7)]
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
