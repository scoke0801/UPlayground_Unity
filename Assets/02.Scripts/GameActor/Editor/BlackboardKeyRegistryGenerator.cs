#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    internal static class BlackboardKeyRegistryGenerator
    {
        private const string SourcePath =
            "Assets/10.Datas/AI/BehaviorTree/BehaviorTreeEditorRegistry.json";
        private const string AssetPath =
            "Assets/Resources/BlackboardKeyRegistry.asset";
        private const string StableIdNamespace =
            "UPlayGround.BlackboardKey.v1:";

        [UPlayGround.EditorTools.UPlaygroundTool(
            "UPlayGround/생성 도구/Blackboard Key Registry 검사",
            false,
            12)]
        public static void ValidateMenu()
        {
            try
            {
                int keyCount = ValidateProjectOrThrow();
                Debug.Log($"[BlackboardKeyRegistry] 검사 완료: {keyCount} keys");
            }
            catch (Exception exception)
            {
                Debug.LogError(exception.Message);
            }
        }

        internal static int ValidateProjectOrThrow()
        {
            RegistryBuildPlan plan = BuildPlan();
            if (plan.Errors.Count > 0)
                throw new InvalidDataException(string.Join("\n", plan.Errors));
            if (plan.GeneratedStableIdCount > 0)
                throw new InvalidDataException(
                    $"{plan.GeneratedStableIdCount}개 Key에 stableId가 없습니다. "
                    + "Registry 생성 및 BT 마이그레이션을 먼저 실행하세요.");

            string[] registryGuids =
                AssetDatabase.FindAssets("t:BlackboardKeyRegistrySO");
            if (registryGuids.Length != 1)
                throw new InvalidDataException(
                    $"BlackboardKeyRegistrySO 에셋은 정확히 1개여야 합니다. "
                    + $"현재 {registryGuids.Length}개");

            string registryPath = AssetDatabase.GUIDToAssetPath(registryGuids[0]);
            if (!string.Equals(registryPath, AssetPath, StringComparison.Ordinal))
                throw new InvalidDataException(
                    $"Registry 경로가 고정 경로와 다릅니다: {registryPath}");

            BlackboardKeyRegistrySO registry =
                AssetDatabase.LoadAssetAtPath<BlackboardKeyRegistrySO>(AssetPath);
            if (registry == null)
                throw new InvalidDataException($"Registry를 로드할 수 없습니다: {AssetPath}");

            List<string> unresolved = CollectUnresolvedAssets(
                registry,
                requireStableIds: true);
            if (unresolved.Count > 0)
                throw new InvalidDataException(string.Join("\n", unresolved));

            return plan.Definitions.Count;
        }

        [UPlayGround.EditorTools.UPlaygroundTool(
            "UPlayGround/생성 도구/Blackboard Key Registry 생성 및 BT 마이그레이션",
            false,
            13)]
        public static void GenerateAndMigrateMenu()
        {
            RegistryBuildPlan plan = BuildPlan();
            if (plan.Errors.Count > 0)
                throw new InvalidDataException(string.Join("\n", plan.Errors));

            // 소스/에셋을 건드리기 전에 메모리 Registry로 전체 BT를 먼저 검사한다.
            // 미등록 문자열이 하나라도 있으면 프로젝트 데이터를 부분 변경 상태로 남기지 않는다.
            BlackboardKeyRegistrySO validationRegistry = CreateRegistry(plan);
            try
            {
                var unresolved = CollectUnresolvedAssets(
                    validationRegistry,
                    requireStableIds: false);
                if (unresolved.Count > 0)
                    throw new InvalidDataException(
                        "Blackboard Key 마이그레이션을 중단했습니다.\n"
                        + string.Join("\n", unresolved));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(validationRegistry);
            }

            WriteStableIdsToSource(plan);
            BlackboardKeyRegistrySO registry = WriteRegistryAsset(plan);
            int migrated = MigrateBehaviorTreeAssets(registry);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"[BlackboardKeyRegistry] 생성/마이그레이션 완료: "
                + $"{plan.Definitions.Count} keys, {migrated} BT assets");
        }

        private static RegistryBuildPlan BuildPlan()
        {
            TextAsset source = AssetDatabase.LoadAssetAtPath<TextAsset>(SourcePath);
            if (source == null)
                throw new FileNotFoundException($"Registry source를 찾지 못했습니다: {SourcePath}");

            var document = JsonUtility.FromJson<BehaviorTreeEditorRegistryDocument>(source.text)
                           ?? new BehaviorTreeEditorRegistryDocument();
            var plan = new RegistryBuildPlan(document);
            var stableIds = new HashSet<string>(StringComparer.Ordinal);
            var names = new HashSet<string>(StringComparer.Ordinal);
            var aliases = new HashSet<string>(StringComparer.Ordinal);

            foreach (EnemyBlackboardDefaultEntryDefinition sourceDefinition
                     in document.enemyBlackboardDefaults)
            {
                if (sourceDefinition == null || string.IsNullOrWhiteSpace(sourceDefinition.key))
                {
                    plan.Errors.Add("비어 있는 Blackboard Key 정의가 있습니다.");
                    continue;
                }

                sourceDefinition.key = sourceDefinition.key.Trim();
                if (string.IsNullOrWhiteSpace(sourceDefinition.stableId))
                {
                    sourceDefinition.stableId = CreateDeterministicStableId(sourceDefinition.key);
                    plan.GeneratedStableIdCount++;
                }
                else
                {
                    sourceDefinition.stableId = sourceDefinition.stableId.Trim();
                }

                if (sourceDefinition.stableId.Length != 32
                    || !Guid.TryParseExact(sourceDefinition.stableId, "N", out _))
                    plan.Errors.Add(
                        $"stableId는 GUID N 형식이어야 합니다: "
                        + $"{sourceDefinition.key}={sourceDefinition.stableId}");
                if (!stableIds.Add(sourceDefinition.stableId))
                    plan.Errors.Add($"stableId 중복: {sourceDefinition.stableId}");
                if (!names.Add(sourceDefinition.key) || aliases.Contains(sourceDefinition.key))
                    plan.Errors.Add($"keyName 중복: {sourceDefinition.key}");

                sourceDefinition.aliases ??= new List<string>();
                foreach (string rawAlias in sourceDefinition.aliases)
                {
                    string alias = rawAlias?.Trim();
                    if (string.IsNullOrEmpty(alias))
                        continue;
                    if (names.Contains(alias) || !aliases.Add(alias))
                        plan.Errors.Add($"Key/alias 충돌: {alias}");
                }

                plan.Definitions.Add(sourceDefinition);
            }

            return plan;
        }

        private static void WriteStableIdsToSource(RegistryBuildPlan plan)
        {
            if (plan.GeneratedStableIdCount == 0)
                return;

            string json = JsonUtility.ToJson(plan.Document, true);
            File.WriteAllText(SourcePath, json + Environment.NewLine, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(SourcePath, ImportAssetOptions.ForceUpdate);
        }

        private static BlackboardKeyRegistrySO WriteRegistryAsset(RegistryBuildPlan plan)
        {
            BlackboardKeyRegistrySO registry =
                AssetDatabase.LoadAssetAtPath<BlackboardKeyRegistrySO>(AssetPath);
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<BlackboardKeyRegistrySO>();
                AssetDatabase.CreateAsset(registry, AssetPath);
            }

            FillRegistry(registry, plan);
            BlackboardKeyRegistry.SetEditorRegistry(registry);
            EditorUtility.SetDirty(registry);
            return registry;
        }

        private static BlackboardKeyRegistrySO CreateRegistry(RegistryBuildPlan plan)
        {
            var registry = ScriptableObject.CreateInstance<BlackboardKeyRegistrySO>();
            FillRegistry(registry, plan);
            return registry;
        }

        private static void FillRegistry(
            BlackboardKeyRegistrySO registry,
            RegistryBuildPlan plan)
        {
            registry.EditorDefinitions.Clear();
            foreach (EnemyBlackboardDefaultEntryDefinition source in plan.Definitions)
            {
                var definition = new BlackboardKeyDefinition();
                definition.SetEditorData(
                    source.stableId,
                    source.key,
                    source.aliases,
                    source.label,
                    source.description,
                    source.type,
                    source.scope,
                    source.writePolicy,
                    source.required);
                registry.EditorDefinitions.Add(definition);
            }

            registry.RebuildLookup();
        }

        private static List<string> CollectUnresolvedAssets(
            BlackboardKeyRegistrySO registry,
            bool requireStableIds)
        {
            var unresolved = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:BehaviorTreeAsset"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                BehaviorTreeAsset tree =
                    AssetDatabase.LoadAssetAtPath<BehaviorTreeAsset>(path);
                if (tree?.Blackboard?.Entries == null)
                    continue;

                foreach (BlackboardEntry entry in tree.Blackboard.Entries)
                {
                    if (entry == null)
                        continue;
                    if (!registry.TryResolve(entry.Key, out _))
                        unresolved.Add($"{path}: Blackboard Key '{entry.Key}' 미등록");
                    else if (requireStableIds
                             && string.IsNullOrWhiteSpace(entry.StableId))
                        unresolved.Add(
                            $"{path}: Blackboard Key '{entry.Key}' stableId 누락");
                }

                foreach (BTNode node in tree.Nodes)
                {
                    if (node == null)
                        continue;
                    var serializedNode = new SerializedObject(node);
                    SerializedProperty iterator = serializedNode.GetIterator();
                    while (iterator.NextVisible(true))
                    {
                        if (iterator.name != "_key"
                            || iterator.propertyType != SerializedPropertyType.String)
                            continue;

                        string parentPath = GetParentPath(iterator.propertyPath);
                        if (serializedNode.FindProperty(parentPath + "._expectedType") == null)
                            continue;
                        string key = iterator.stringValue;
                        if (string.IsNullOrWhiteSpace(key))
                            continue;
                        if (!registry.TryResolve(key, out _))
                            unresolved.Add(
                                $"{path}/{node.name}: selector Key '{key}' 미등록");
                        else if (requireStableIds)
                        {
                            SerializedProperty stableIdProperty =
                                serializedNode.FindProperty(
                                    parentPath + "._stableId");
                            if (stableIdProperty == null
                                || string.IsNullOrWhiteSpace(
                                    stableIdProperty.stringValue))
                                unresolved.Add(
                                    $"{path}/{node.name}: selector Key "
                                    + $"'{key}' stableId 누락");
                        }
                    }
                }
            }

            return unresolved;
        }

        private static int MigrateBehaviorTreeAssets(
            BlackboardKeyRegistrySO registry)
        {
            int migratedAssetCount = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:BehaviorTreeAsset"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                BehaviorTreeAsset tree =
                    AssetDatabase.LoadAssetAtPath<BehaviorTreeAsset>(path);
                if (tree == null)
                    continue;

                bool changed = false;
                foreach (BlackboardEntry entry in tree.Blackboard.Entries)
                {
                    if (entry == null
                        || !registry.TryResolve(entry.Key, out BlackboardKeyReference reference)
                        || entry.KeyReference == reference && entry.StableId == reference.StableId)
                        continue;

                    entry.SetKeyReference(reference);
                    changed = true;
                }

                foreach (BTNode node in tree.Nodes)
                {
                    if (node == null)
                        continue;

                    var serializedNode = new SerializedObject(node);
                    SerializedProperty iterator = serializedNode.GetIterator();
                    bool nodeChanged = false;
                    while (iterator.NextVisible(true))
                    {
                        if (iterator.name != "_key"
                            || iterator.propertyType != SerializedPropertyType.String)
                            continue;

                        string parentPath = GetParentPath(iterator.propertyPath);
                        SerializedProperty typeProperty =
                            serializedNode.FindProperty(parentPath + "._expectedType");
                        SerializedProperty stableIdProperty =
                            serializedNode.FindProperty(parentPath + "._stableId");
                        if (typeProperty == null || stableIdProperty == null
                            || !registry.TryResolve(
                                iterator.stringValue,
                                out BlackboardKeyReference reference)
                            || stableIdProperty.stringValue == reference.StableId)
                            continue;

                        stableIdProperty.stringValue = reference.StableId;
                        iterator.stringValue = reference.KeyName;
                        nodeChanged = true;
                    }

                    if (!nodeChanged)
                        continue;
                    serializedNode.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(node);
                    changed = true;
                }

                if (!changed)
                    continue;
                EditorUtility.SetDirty(tree);
                migratedAssetCount++;
            }

            return migratedAssetCount;
        }

        private static string GetParentPath(string propertyPath)
        {
            int separator = propertyPath.LastIndexOf('.');
            return separator > 0 ? propertyPath.Substring(0, separator) : string.Empty;
        }

        private static string CreateDeterministicStableId(string keyName)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(StableIdNamespace + keyName);
            byte[] hash;
            using (SHA256 algorithm = SHA256.Create())
                hash = algorithm.ComputeHash(bytes);
            var guidBytes = new byte[16];
            Array.Copy(hash, guidBytes, guidBytes.Length);
            return new Guid(guidBytes).ToString("N");
        }

        private sealed class RegistryBuildPlan
        {
            public RegistryBuildPlan(BehaviorTreeEditorRegistryDocument document)
            {
                Document = document;
            }

            public BehaviorTreeEditorRegistryDocument Document { get; }
            public List<EnemyBlackboardDefaultEntryDefinition> Definitions { get; } = new();
            public List<string> Errors { get; } = new();
            public int GeneratedStableIdCount { get; set; }
        }
    }

    internal sealed class BlackboardKeyRegistryBuildValidator :
        IPreprocessBuildWithReport
    {
        public int callbackOrder => -900;

        public void OnPreprocessBuild(BuildReport report)
        {
            try
            {
                BlackboardKeyRegistryGenerator.ValidateProjectOrThrow();
            }
            catch (Exception exception)
            {
                throw new BuildFailedException(
                    "Blackboard Key Registry 무결성 검사 실패\n"
                    + exception.Message);
            }
        }
    }
}
#endif
