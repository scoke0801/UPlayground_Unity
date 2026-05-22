#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public static class BehaviorTreeDisplayNameRegistry
    {
        public static string GetBlackboardLabel(string key)
            => EnemyBlackboardDefaultEntryRegistry.TryGetEntry(key, out var entry) ? entry.Label : key;

        public static string GetNodeTypeLabel(Type type)
        {
            if (type == null)
                return string.Empty;

            return BehaviorTreeEditorRegistryData.TryGetNodeLabel(type.Name, out var label)
                ? label
                : TrimNodeSuffix(type.Name);
        }

        public static string GetNodeTitle(BTNode node)
        {
            if (node == null)
                return string.Empty;

            if (BehaviorTreeEditorRegistryData.TryGetNodeLabel(node.GetType().Name, out var typeLabel))
                return typeLabel;

            return BehaviorTreeEditorRegistryData.TryGetNodeLabel(node.DisplayName, out var displayLabel)
                ? displayLabel
                : node.DisplayName;
        }

        public static string FormatWithRawName(string displayName, string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName) || string.Equals(displayName, rawName, StringComparison.Ordinal))
                return displayName;

            return $"{displayName} ({rawName})";
        }

        private static string TrimNodeSuffix(string name)
            => name.EndsWith("Node", StringComparison.Ordinal) ? name[..^4] : name;
    }

    internal static class BehaviorTreeEditorRegistryData
    {
        private const string RegistryPath = "Assets/10.Datas/AI/BehaviorTree/BehaviorTreeEditorRegistry.json";

        private static BehaviorTreeEditorRegistryDocument _document;
        private static Dictionary<string, string> _nodeLabels;
        private static Dictionary<string, EnemyBlackboardDefaultEntry> _blackboardEntriesByKey;
        private static Dictionary<string, EnemyBlackboardConditionAlias> _blackboardConditionAliasesByName;
        private static EnemyBlackboardDefaultEntry[] _blackboardEntries;

        public static IReadOnlyList<EnemyBlackboardDefaultEntry> BlackboardEntries
        {
            get
            {
                EnsureLoaded();
                return _blackboardEntries;
            }
        }

        public static bool TryGetNodeLabel(string key, out string label)
        {
            EnsureLoaded();
            if (!string.IsNullOrWhiteSpace(key) && _nodeLabels.TryGetValue(key, out label))
                return true;

            label = default;
            return false;
        }

        public static bool TryGetBlackboardEntry(string key, out EnemyBlackboardDefaultEntry entry)
        {
            EnsureLoaded();
            if (!string.IsNullOrWhiteSpace(key) && _blackboardEntriesByKey.TryGetValue(key, out entry))
                return true;

            entry = default;
            return false;
        }

        public static bool TryGetBlackboardConditionAlias(string conditionName, out EnemyBlackboardConditionAlias alias)
        {
            EnsureLoaded();
            if (!string.IsNullOrWhiteSpace(conditionName)
                && _blackboardConditionAliasesByName.TryGetValue(conditionName, out alias))
                return true;

            alias = default;
            return false;
        }

        private static void EnsureLoaded()
        {
            if (_document != null)
                return;

            var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(RegistryPath);
            if (textAsset == null)
            {
                Debug.LogWarning($"Behavior Tree 에디터 레지스트리 파일을 찾을 수 없습니다: {RegistryPath}");
                _document = new BehaviorTreeEditorRegistryDocument();
            }
            else
            {
                try
                {
                    _document = JsonUtility.FromJson<BehaviorTreeEditorRegistryDocument>(textAsset.text)
                        ?? new BehaviorTreeEditorRegistryDocument();
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Behavior Tree 에디터 레지스트리 파싱 실패: {RegistryPath}\n{exception}");
                    _document = new BehaviorTreeEditorRegistryDocument();
                }
            }

            BuildLookup();
        }

        private static void BuildLookup()
        {
            _nodeLabels = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var definition in _document.nodeLabels)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.key))
                    continue;

                _nodeLabels[definition.key] = definition.label ?? string.Empty;
            }

            var blackboardEntries = new List<EnemyBlackboardDefaultEntry>();
            _blackboardEntriesByKey = new Dictionary<string, EnemyBlackboardDefaultEntry>(StringComparer.Ordinal);
            foreach (var definition in _document.enemyBlackboardDefaults)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.key))
                    continue;

                var entry = definition.ToEntry();
                blackboardEntries.Add(entry);
                _blackboardEntriesByKey[entry.Key] = entry;
            }

            _blackboardEntries = blackboardEntries.ToArray();

            _blackboardConditionAliasesByName = new Dictionary<string, EnemyBlackboardConditionAlias>(StringComparer.Ordinal);
            foreach (var definition in _document.blackboardConditionAliases)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.condition) || string.IsNullOrWhiteSpace(definition.key))
                    continue;

                _blackboardConditionAliasesByName[definition.condition] = definition.ToAlias();
            }
        }
    }

    [Serializable]
    internal sealed class BehaviorTreeEditorRegistryDocument
    {
        public List<BehaviorTreeDisplayNameDefinition> nodeLabels = new();
        public List<EnemyBlackboardDefaultEntryDefinition> enemyBlackboardDefaults = new();
        public List<EnemyBlackboardConditionAliasDefinition> blackboardConditionAliases = new();
    }

    [Serializable]
    internal sealed class BehaviorTreeDisplayNameDefinition
    {
        public string key;
        public string label;
    }

    [Serializable]
    internal sealed class EnemyBlackboardDefaultEntryDefinition
    {
        public string key;
        public BlackboardValueType type;
        public string label;
        public bool boolValue;
        public int intValue;
        public float floatValue;
        public string stringValue;
        public Vector3 vector3Value;

        public EnemyBlackboardDefaultEntry ToEntry()
            => new(
                key,
                type,
                label,
                boolValue,
                intValue,
                floatValue,
                stringValue ?? string.Empty);
    }

    internal readonly struct EnemyBlackboardConditionAlias
    {
        public readonly string Key;
        public readonly BlackboardComparisonType Comparison;
        public readonly string Value;

        public EnemyBlackboardConditionAlias(string key, BlackboardComparisonType comparison, string value)
        {
            Key = key;
            Comparison = comparison;
            Value = value;
        }
    }

    [Serializable]
    internal sealed class EnemyBlackboardConditionAliasDefinition
    {
        public string condition;
        public string key;
        public BlackboardComparisonType comparison = BlackboardComparisonType.Equal;
        public string value = "true";

        public EnemyBlackboardConditionAlias ToAlias()
            => new(key, comparison, value ?? string.Empty);
    }
}
#endif
