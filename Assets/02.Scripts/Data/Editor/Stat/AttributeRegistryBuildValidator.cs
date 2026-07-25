using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UPlayGround.Data.Stat;

namespace UPlayGround.Data.Editor.Stat
{
    public sealed class AttributeRegistryBuildValidator :
        IPreprocessBuildWithReport
    {
        private static readonly HashSet<string> SerializedAssetExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".asset",
                ".prefab",
                ".unity",
            };

        private static readonly string[] SerializedDataRoots =
        {
            "Assets/01.Scenes",
            "Assets/03.Prefabs",
            "Assets/10.Datas",
            "Assets/Resources",
        };

        public int callbackOrder => -890;

        public void OnPreprocessBuild(BuildReport report)
        {
            try
            {
                ValidateProjectOrThrow();
            }
            catch (Exception exception)
            {
                throw new BuildFailedException(exception.Message);
            }
        }

        [UPlayGround.EditorTools.UPlaygroundTool(
            "UPlayGround/게임플레이/Attribute/등록 무결성 검증",
            priority = 220)]
        public static void ValidateFromMenu()
        {
            try
            {
                ValidateProjectOrThrow();
                Debug.Log("[Attribute] Registry/직렬화 데이터 검증 성공.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[Attribute] 등록 무결성 검증 실패\n{exception.Message}");
            }
        }

        public static void ValidateProjectOrThrow()
        {
            AttributeRegistrySO registry = LoadSingleRegistry();
            HashSet<string> registered = ValidateRegistry(registry);
            ValidateCodeDefinedAttributes(registered);
            ValidateSerializedAssets(registered, registry);
        }

        private static AttributeRegistrySO LoadSingleRegistry()
        {
            string[] guids =
                AssetDatabase.FindAssets("t:AttributeRegistrySO");
            if (guids.Length != 1)
                throw new InvalidOperationException(
                    $"AttributeRegistrySO는 정확히 1개여야 합니다. 현재 {guids.Length}개입니다.");
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<AttributeRegistrySO>(path)
                   ?? throw new InvalidOperationException(
                       $"AttributeRegistrySO를 로드하지 못했습니다: {path}");
        }

        private static HashSet<string> ValidateRegistry(
            AttributeRegistrySO registry)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            var stableIds = new HashSet<string>(StringComparer.Ordinal);
            var allKeys = new Dictionary<string, string>(
                StringComparer.Ordinal);
            var errors = new List<string>();
            IReadOnlyList<AttributeRegistryEntry> entries =
                registry.attributes;
            if (entries == null || entries.Count == 0)
                errors.Add("Registry가 비어 있습니다.");
            else
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    AttributeRegistryEntry entry = entries[i];
                    if (entry == null)
                    {
                        errors.Add($"{i}번 Registry 항목이 null입니다.");
                        continue;
                    }
                    ValidateKey(
                        entry.attributeId,
                        "attributeId",
                        i,
                        names,
                        errors);
                    ValidateKey(
                        entry.stableId,
                        "stableId",
                        i,
                        stableIds,
                        errors);
                    AddLookupKey(
                        entry.attributeId,
                        entry.attributeId,
                        allKeys,
                        errors);
                    if (entry.aliases != null)
                    {
                        for (int j = 0; j < entry.aliases.Count; j++)
                            AddLookupKey(
                                entry.aliases[j],
                                entry.attributeId,
                                allKeys,
                                errors);
                    }
                }

                for (int i = 0; i < entries.Count; i++)
                {
                    AttributeRegistryEntry entry = entries[i];
                    if (entry == null) continue;
                    ValidateReference(
                        entry.minimumAttributeId,
                        entry.attributeId,
                        "minimumAttributeId",
                        allKeys,
                        errors);
                    ValidateReference(
                        entry.maximumAttributeId,
                        entry.attributeId,
                        "maximumAttributeId",
                        allKeys,
                        errors);
                    ValidateReference(
                        entry.dependentResourceId,
                        entry.attributeId,
                        "dependentResourceId",
                        allKeys,
                        errors);
                }
            }

            ThrowIfAny(errors);
            return names;
        }

        private static void ValidateCodeDefinedAttributes(
            HashSet<string> registered)
        {
            var errors = new List<string>();
            ValidateCodeDefinedContainer(
                typeof(Attributes),
                registered,
                errors);
            ThrowIfAny(errors);
        }

        private static void ValidateCodeDefinedContainer(
            Type container,
            HashSet<string> registered,
            List<string> errors)
        {
            FieldInfo[] fields = container.GetFields(
                BindingFlags.Public | BindingFlags.Static);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                if (field.FieldType != typeof(AttributeReference))
                    continue;
                var reference =
                    (AttributeReference)field.GetValue(null);
                if (string.IsNullOrEmpty(reference.AttributeId))
                    continue;
                if (!registered.Contains(reference.AttributeId))
                    errors.Add(
                        $"{container.FullName}.{field.Name}: "
                        + $"미등록 코드 Attribute \"{reference.AttributeId}\"");
            }
            Type[] nested = container.GetNestedTypes(
                BindingFlags.Public);
            for (int i = 0; i < nested.Length; i++)
                ValidateCodeDefinedContainer(
                    nested[i],
                    registered,
                    errors);
        }

        private static void ValidateSerializedAssets(
            HashSet<string> registered,
            AttributeRegistrySO registry)
        {
            var resolvable = new HashSet<string>(
                registered,
                StringComparer.Ordinal);
            for (int i = 0; i < registry.attributes.Count; i++)
            {
                List<string> aliases = registry.attributes[i]?.aliases;
                if (aliases == null) continue;
                for (int j = 0; j < aliases.Count; j++)
                    if (!string.IsNullOrWhiteSpace(aliases[j]))
                        resolvable.Add(aliases[j]);
            }

            var errors = new List<string>();
            string projectRoot =
                Directory.GetParent(Application.dataPath)?.FullName
                ?? throw new InvalidOperationException(
                    "Unity 프로젝트 루트를 확인하지 못했습니다.");
            foreach (string assetRoot in SerializedDataRoots)
            {
                string absoluteRoot =
                    System.IO.Path.Combine(projectRoot, assetRoot);
                if (!Directory.Exists(absoluteRoot)) continue;
                foreach (string filePath in Directory.EnumerateFiles(
                             absoluteRoot,
                             "*",
                             SearchOption.AllDirectories))
                {
                    if (!SerializedAssetExtensions.Contains(
                            System.IO.Path.GetExtension(filePath)))
                        continue;
                    int lineNumber = 0;
                    foreach (string line in File.ReadLines(filePath))
                    {
                        lineNumber++;
                        string trimmed = line.TrimStart();
                        if (trimmed.StartsWith(
                                "- ",
                                StringComparison.Ordinal))
                            trimmed = trimmed.Substring(2).TrimStart();
                        const string privateKey = "_attributeId:";
                        const string publicKey = "attributeId:";
                        string matchedKey =
                            trimmed.StartsWith(
                                privateKey,
                                StringComparison.Ordinal)
                                ? privateKey
                                : trimmed.StartsWith(
                                    publicKey,
                                    StringComparison.Ordinal)
                                    ? publicKey
                                    : null;
                        if (matchedKey == null)
                            continue;
                        string id = trimmed
                            .Substring(matchedKey.Length)
                            .Trim()
                            .Trim('"');
                        if (string.IsNullOrEmpty(id)
                            || resolvable.Contains(id))
                            continue;
                        string relativePath = filePath
                            .Substring(projectRoot.Length + 1)
                            .Replace('\\', '/');
                        errors.Add(
                            $"{relativePath}:{lineNumber} 미등록 Attribute \"{id}\"");
                    }
                }
            }
            ThrowIfAny(errors);
        }

        private static void ValidateKey(
            string value,
            string fieldName,
            int index,
            HashSet<string> values,
            List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add(
                    $"{index}번 Registry 항목의 {fieldName}가 비어 있습니다.");
                return;
            }
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                errors.Add($"{fieldName} 앞뒤 공백: \"{value}\"");
            if (!values.Add(value))
                errors.Add($"중복 {fieldName}: \"{value}\"");
        }

        private static void AddLookupKey(
            string key,
            string owner,
            IDictionary<string, string> keys,
            List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            if (keys.TryGetValue(key, out string existing)
                && !string.Equals(existing, owner, StringComparison.Ordinal))
                errors.Add(
                    $"Attribute/alias 충돌: \"{key}\" ({existing}, {owner})");
            else
                keys[key] = owner;
        }

        private static void ValidateReference(
            string reference,
            string owner,
            string fieldName,
            IReadOnlyDictionary<string, string> keys,
            List<string> errors)
        {
            if (!string.IsNullOrWhiteSpace(reference)
                && !keys.ContainsKey(reference))
                errors.Add(
                    $"{owner}.{fieldName}: 미등록 Attribute \"{reference}\"");
        }

        private static void ThrowIfAny(List<string> errors)
        {
            if (errors.Count == 0) return;
            throw new InvalidOperationException(
                "Attribute 등록 무결성 오류:\n- "
                + string.Join("\n- ", errors));
        }
    }
}
