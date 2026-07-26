#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    /// <summary>
    /// Blackboard Key 이름이 변경될 때 트리 내부 노드의 BlackboardKeySelector 필드와
    /// 레거시 string Key 필드(_key, key, *Key)를 일괄 업데이트한다.
    /// 모든 변경은 단일 Undo group으로 묶이며 Asset과 영향을 받은 노드에 dirty 표시를 남긴다.
    /// </summary>
    public static class BehaviorTreeBlackboardKeyRenamer
    {
        public readonly struct RenameResult
        {
            public RenameResult(int updatedSelectorFields, int updatedLegacyFields, int touchedNodes)
            {
                UpdatedSelectorFields = updatedSelectorFields;
                UpdatedLegacyFields = updatedLegacyFields;
                TouchedNodes = touchedNodes;
            }

            public int UpdatedSelectorFields { get; }
            public int UpdatedLegacyFields { get; }
            public int TouchedNodes { get; }
            public int TotalFieldUpdates => UpdatedSelectorFields + UpdatedLegacyFields;
        }

        public static RenameResult RenameKey(BehaviorTreeAsset tree, string oldKey, string newKey)
        {
            if (tree == null || string.IsNullOrWhiteSpace(oldKey) || string.IsNullOrWhiteSpace(newKey))
                return default;

            if (string.Equals(oldKey, newKey, StringComparison.Ordinal))
                return default;

            if (!BlackboardKeyRegistry.TryResolve(oldKey, out BlackboardKeyReference oldReference)
                || !BlackboardKeyRegistry.TryResolve(newKey, out BlackboardKeyReference newReference)
                || oldReference.StableId != newReference.StableId)
            {
                EditorUtility.DisplayDialog(
                    "Blackboard Key 변경 실패",
                    "Registry에서 동일 stableId의 canonical name/alias로 먼저 변경해야 합니다.",
                    "확인");
                return default;
            }

            var entry = tree.Blackboard?.FindEntry(oldKey);
            if (entry == null)
                return default;

            if (tree.Blackboard.FindEntry(newKey) != null)
            {
                EditorUtility.DisplayDialog("Blackboard Key 변경 실패", $"이미 '{newKey}' Key가 존재합니다. 다른 이름을 사용하세요.", "확인");
                return default;
            }

            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName($"Rename Blackboard Key '{oldKey}' → '{newKey}'");

            Undo.RecordObject(tree, "Rename Blackboard Key");
            entry.SetKeyReference(newReference);

            var touchedNodes = 0;
            var updatedSelectors = 0;
            var updatedLegacy = 0;

            foreach (var node in tree.Nodes)
            {
                if (node == null)
                    continue;

                var nodeTouched = false;
                foreach (var field in GetSerializableFields(node.GetType()))
                {
                    if (field.FieldType == typeof(BlackboardKeySelector))
                    {
                        var selector = (BlackboardKeySelector)field.GetValue(node);
                        if (!string.Equals(selector.Key, oldKey, StringComparison.Ordinal))
                            continue;

                        if (!nodeTouched)
                        {
                            Undo.RecordObject(node, "Rename Blackboard Key Reference");
                            nodeTouched = true;
                        }

                        var replaced = new BlackboardKeySelector(newReference, selector.ExpectedType);
                        field.SetValue(node, replaced);
                        updatedSelectors++;
                    }
                    else if (field.FieldType == typeof(string) && IsBlackboardKeyField(field))
                    {
                        var current = field.GetValue(node) as string;
                        if (!string.Equals(current, oldKey, StringComparison.Ordinal))
                            continue;

                        if (!nodeTouched)
                        {
                            Undo.RecordObject(node, "Rename Blackboard Key Reference");
                            nodeTouched = true;
                        }

                        field.SetValue(node, newKey);
                        updatedLegacy++;
                    }
                }

                if (nodeTouched)
                {
                    touchedNodes++;
                    EditorUtility.SetDirty(node);
                }
            }

            EditorUtility.SetDirty(tree);
            Undo.CollapseUndoOperations(undoGroup);
            return new RenameResult(updatedSelectors, updatedLegacy, touchedNodes);
        }

        public static int CountReferences(BehaviorTreeAsset tree, string key)
        {
            if (tree == null || string.IsNullOrWhiteSpace(key))
                return 0;

            var count = 0;
            foreach (var node in tree.Nodes)
            {
                if (node == null)
                    continue;

                foreach (var field in GetSerializableFields(node.GetType()))
                {
                    if (field.FieldType == typeof(BlackboardKeySelector))
                    {
                        var selector = (BlackboardKeySelector)field.GetValue(node);
                        if (string.Equals(selector.Key, key, StringComparison.Ordinal))
                            count++;
                    }
                    else if (field.FieldType == typeof(string) && IsBlackboardKeyField(field))
                    {
                        var current = field.GetValue(node) as string;
                        if (string.Equals(current, key, StringComparison.Ordinal))
                            count++;
                    }
                }
            }

            return count;
        }

        private static bool IsBlackboardKeyField(FieldInfo field)
        {
            return string.Equals(field.Name, "_key", StringComparison.Ordinal) ||
                   string.Equals(field.Name, "key", StringComparison.Ordinal) ||
                   field.Name.EndsWith("Key", StringComparison.Ordinal) ||
                   field.Name.EndsWith("_key", StringComparison.Ordinal);
        }

        private static IEnumerable<FieldInfo> GetSerializableFields(Type type)
        {
            while (type != null && type != typeof(BTNode))
            {
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    if (field.IsNotSerialized)
                        continue;

                    if (field.IsPublic || field.GetCustomAttribute<SerializeField>() != null)
                        yield return field;
                }

                type = type.BaseType;
            }
        }
    }
}
#endif
