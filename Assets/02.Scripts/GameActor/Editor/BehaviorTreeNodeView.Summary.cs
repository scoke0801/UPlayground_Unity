#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public partial class BehaviorTreeNodeView
    {
        private static IEnumerable<(string Key, string Value)> GetNodeSummaryRows(BTNode node)
        {
            if (node is WeightedRandomSelectorNode weighted)
            {
                yield return ("mode", weighted.Children.Count > 0 ? "pick once, retry on fail" : "empty");
            }

            foreach (var field in GetSummaryFields(node.GetType()))
            {
                if (field.Name is "_weights" or "m_Script")
                    continue;

                if (!TryFormatField(field, node, out var key, out var value))
                    continue;

                yield return (key, value);
            }
        }

        private static IEnumerable<FieldInfo> GetSummaryFields(Type type)
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

        private static bool TryFormatField(FieldInfo field, BTNode node, out string key, out string value)
        {
            key = ToDisplayKey(field.Name);
            value = string.Empty;

            var fieldType = field.FieldType;
            var rawValue = field.GetValue(node);
            if (fieldType == typeof(bool))
                value = (bool)rawValue ? "true" : "false";
            else if (fieldType == typeof(int))
                value = rawValue.ToString();
            else if (fieldType == typeof(float))
                value = $"{(float)rawValue:0.###}";
            else if (fieldType == typeof(string))
                value = string.IsNullOrWhiteSpace(rawValue as string) ? "<empty>" : rawValue as string;
            else if (fieldType.IsEnum)
                value = rawValue.ToString();
            else if (fieldType == typeof(BlackboardKeySelector))
            {
                var selector = (BlackboardKeySelector)rawValue;
                value = selector.HasKey
                    ? $"{BehaviorTreeDisplayNameRegistry.GetBlackboardLabel(selector.Key)} ({selector.ExpectedType})"
                    : $"<unset {selector.ExpectedType}>";
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
                value = rawValue != null ? ((UnityEngine.Object)rawValue).name : "null";
            else if (typeof(IList).IsAssignableFrom(fieldType) && rawValue is IList list)
                value = $"count:{list.Count}";
            else
                return false;

            value = TrimText(value, 28);
            return true;
        }

        private static string ToDisplayKey(string fieldName)
        {
            var key = fieldName.TrimStart('_');
            return string.IsNullOrEmpty(key) ? fieldName : key;
        }

        private static string TrimText(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value;

            return value.Substring(0, Mathf.Max(0, maxLength - 1)) + "…";
        }
    }
}
#endif
