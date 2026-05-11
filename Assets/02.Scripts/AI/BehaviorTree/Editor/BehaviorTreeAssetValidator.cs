#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public enum BehaviorTreeValidationLevel
    {
        Info,
        Warning,
        Error
    }

    public readonly struct BehaviorTreeValidationMessage
    {
        public BehaviorTreeValidationMessage(BehaviorTreeValidationLevel level, string message, BTNode targetNode = null)
        {
            Level = level;
            Message = message;
            TargetNode = targetNode;
        }

        public BehaviorTreeValidationLevel Level { get; }
        public string Message { get; }
        public BTNode TargetNode { get; }
    }

    public static class BehaviorTreeAssetValidator
    {
        public static List<BehaviorTreeValidationMessage> Validate(BehaviorTreeAsset tree)
        {
            var messages = new List<BehaviorTreeValidationMessage>();
            if (tree == null)
            {
                messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Error, "BT Asset이 선택되지 않았습니다."));
                return messages;
            }

            if (tree.RootNode == null)
                messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Error, "Root 노드가 없습니다."));

            ValidateBlackboard(tree, messages);

            for (var nodeIndex = 0; nodeIndex < tree.Nodes.Count; nodeIndex++)
            {
                var node = tree.Nodes[nodeIndex];
                if (node == null)
                {
                    messages.Add(new BehaviorTreeValidationMessage(
                        BehaviorTreeValidationLevel.Error,
                        $"{tree.name}.Nodes[{nodeIndex}]: 그래프에 표시할 수 없는 비어 있는 노드 참조입니다. 이미 삭제된 서브에셋 슬롯이 남은 상태이므로 Clean Nulls로 정리하세요."));
                    continue;
                }

                node.EnsureGuid();

                if (node.Disabled)
                    messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Warning, $"{FormatNode(node)}: Disabled 상태입니다. 런타임에서는 Success로 건너뜁니다.", node));

                ValidateBlackboardReferences(tree, node, messages);

                if (node is BTCompositeNode composite && composite.Children.Count == 0)
                    messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Error, $"{FormatNode(node)}: Composite 노드는 최소 1개 자식이 필요합니다.", node));

                if (node is BTDecoratorNode && node.Children.Count != 1)
                    messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Error, $"{FormatNode(node)}: Decorator 노드는 정확히 1개 자식이 필요합니다. 현재 자식 수: {node.Children.Count}", node));

                if (node is BTServiceNode && !IsAttachedAsService(tree, node))
                    messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Warning, $"{FormatNode(node)}: Service 노드가 어떤 Composite의 Services 리스트에도 부착되어 있지 않습니다.", node));

                if (node is BTCompositeNode compositeForServices && compositeForServices.Services != null)
                {
                    for (var serviceIndex = 0; serviceIndex < compositeForServices.Services.Count; serviceIndex++)
                    {
                        var service = compositeForServices.Services[serviceIndex];
                        if (service == null)
                        {
                            messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Error, $"{FormatNode(node)}: Services[{serviceIndex}]가 비어 있습니다. 항목을 제거하거나 Service 에셋을 지정하세요.", node));
                            continue;
                        }

                        if (!tree.Nodes.Contains(service))
                            messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Error, $"{FormatNode(node)}: Services[{serviceIndex}]가 트리 외부 노드를 참조합니다. 참조값: {FormatNode(service)}", node));
                    }
                }

                if (node is WeightedRandomSelectorNode weighted)
                {
                    var weightCount = weighted.Weights?.Count ?? 0;
                    if (weightCount > 0 && weightCount != weighted.Children.Count)
                        messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Warning, $"{FormatNode(node)}: Weights 수({weightCount})와 Children 수({weighted.Children.Count})가 다릅니다. 누락분은 1.0으로 처리됩니다.", node));
                }

                if (node is SubtreeNode subtree)
                {
                    if (subtree.SubtreeAsset == null)
                    {
                        messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Error, $"{FormatNode(node)}: Subtree Asset이 지정되지 않았습니다.", node));
                    }
                    else if (HasSubtreeCycle(subtree.SubtreeAsset, tree, new HashSet<BehaviorTreeAsset>()))
                    {
                        messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Error, $"{FormatNode(node)}: Subtree 참조가 순환 구조를 만듭니다 ({subtree.SubtreeAsset.name}).", node));
                    }
                }

                for (var childIndex = 0; childIndex < node.Children.Count; childIndex++)
                {
                    var child = node.Children[childIndex];
                    if (child == null || !tree.Nodes.Contains(child))
                    {
                        var childInfo = child == null ? "null" : FormatNode(child);
                        messages.Add(new BehaviorTreeValidationMessage(
                            BehaviorTreeValidationLevel.Error,
                            $"{FormatNode(node)}: Children[{childIndex}]에 끊어진 자식 노드 참조가 있습니다. 참조값: {childInfo}. 이 행을 클릭하면 부모 노드로 이동합니다.",
                            node));
                    }
                }
            }

            var referenced = new HashSet<BTNode>();
            foreach (var node in tree.Nodes)
            {
                if (node == null)
                    continue;

                foreach (var child in node.Children)
                {
                    if (child != null)
                        referenced.Add(child);
                }

                if (node is BTCompositeNode compositeRef && compositeRef.Services != null)
                {
                    foreach (var service in compositeRef.Services)
                    {
                        if (service != null)
                            referenced.Add(service);
                    }
                }
            }

            foreach (var node in tree.Nodes.Where(node => node != null && node != tree.RootNode && !referenced.Contains(node)))
                messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Warning, $"{FormatNode(node)}: Root에서 연결되지 않은 노드입니다.", node));

            if (tree.RootNode != null && HasCycle(tree.RootNode, new HashSet<BTNode>(), new HashSet<BTNode>()))
                messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Error, "순환 참조가 있습니다."));

            if (messages.Count == 0)
                messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Info, "검증 오류가 없습니다."));

            return messages;
        }

        private static void ValidateBlackboard(BehaviorTreeAsset tree, List<BehaviorTreeValidationMessage> messages)
        {
            var entries = tree.Blackboard?.Entries;
            if (entries == null)
                return;

            var keys = new Dictionary<string, int>();
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null)
                {
                    messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Error, $"Blackboard[{i}]: 비어 있는 Entry입니다."));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.Key))
                {
                    messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Error, $"Blackboard[{i}]: Key가 비어 있습니다."));
                    continue;
                }

                if (!keys.TryAdd(entry.Key, i))
                    messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Error, $"Blackboard Key '{entry.Key}'가 중복됩니다. 첫 위치: {keys[entry.Key]}, 중복 위치: {i}"));
            }
        }

        private static void ValidateBlackboardReferences(BehaviorTreeAsset tree, BTNode node, List<BehaviorTreeValidationMessage> messages)
        {
            foreach (var field in GetSerializableFields(node.GetType()))
            {
                if (field.FieldType == typeof(BlackboardKeySelector))
                {
                    var selector = (BlackboardKeySelector)field.GetValue(node);
                    ValidateSelector(tree, node, field.Name, selector, messages);
                }
                else if (field.FieldType == typeof(string) && IsBlackboardKeyField(field))
                {
                    ValidateLegacyStringKey(tree, node, field, messages);
                }
            }
        }

        private static void ValidateSelector(
            BehaviorTreeAsset tree,
            BTNode node,
            string fieldName,
            BlackboardKeySelector selector,
            List<BehaviorTreeValidationMessage> messages)
        {
            if (!selector.HasKey)
            {
                messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Warning, $"{FormatNode(node)}: Blackboard selector '{fieldName}'에 Key가 지정되지 않았습니다.", node));
                return;
            }

            var entry = tree.Blackboard?.FindEntry(selector.Key);
            if (entry == null)
            {
                messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Error, $"{FormatNode(node)}: Blackboard Key '{selector.Key}'를 찾을 수 없습니다. Field: {fieldName}", node));
                return;
            }

            if (entry.ValueType != selector.ExpectedType)
                messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Error, $"{FormatNode(node)}: Blackboard Key '{selector.Key}' 타입이 맞지 않습니다. 필요: {selector.ExpectedType}, 실제: {entry.ValueType}", node));
        }

        private static void ValidateLegacyStringKey(
            BehaviorTreeAsset tree,
            BTNode node,
            FieldInfo keyField,
            List<BehaviorTreeValidationMessage> messages)
        {
            var key = keyField.GetValue(node) as string;
            if (string.IsNullOrWhiteSpace(key))
                return;

            var entry = tree.Blackboard?.FindEntry(key);
            if (entry == null)
            {
                messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Error, $"{FormatNode(node)}: Blackboard Key '{key}'를 찾을 수 없습니다. Field: {keyField.Name}", node));
                return;
            }

            var valueTypeField = GetSerializableFields(node.GetType()).FirstOrDefault(field => field.Name is "_valueType" or "valueType");
            if (valueTypeField != null && valueTypeField.FieldType == typeof(BlackboardValueType))
            {
                var expectedType = (BlackboardValueType)valueTypeField.GetValue(node);
                if (entry.ValueType != expectedType)
                    messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Error, $"{FormatNode(node)}: Blackboard Key '{key}' 타입이 맞지 않습니다. 필요: {expectedType}, 실제: {entry.ValueType}", node));
            }
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

                    if (field.IsPublic || field.GetCustomAttribute<UnityEngine.SerializeField>() != null)
                        yield return field;
                }

                type = type.BaseType;
            }
        }

        private static string FormatNode(BTNode node)
        {
            if (node == null)
                return "null";

            node.EnsureGuid();
            var shortGuid = node.Guid.Length > 8 ? node.Guid.Substring(0, 8) : node.Guid;
            return $"{node.DisplayName} ({node.GetType().Name}, guid:{shortGuid})";
        }

        private static bool IsAttachedAsService(BehaviorTreeAsset tree, BTNode candidate)
        {
            if (candidate is not BTServiceNode service)
                return false;

            foreach (var node in tree.Nodes)
            {
                if (node is BTCompositeNode composite && composite.Services != null && composite.Services.Contains(service))
                    return true;
            }

            return false;
        }

        private static bool HasSubtreeCycle(BehaviorTreeAsset current, BehaviorTreeAsset root, HashSet<BehaviorTreeAsset> visited)
        {
            if (current == null)
                return false;

            if (current == root)
                return true;

            if (!visited.Add(current))
                return false;

            foreach (var node in current.Nodes)
            {
                if (node is SubtreeNode nested && nested.SubtreeAsset != null)
                {
                    if (HasSubtreeCycle(nested.SubtreeAsset, root, visited))
                        return true;
                }
            }

            return false;
        }

        private static bool HasCycle(BTNode node, HashSet<BTNode> visiting, HashSet<BTNode> visited)
        {
            if (node == null)
                return false;

            if (visiting.Contains(node))
                return true;

            if (visited.Contains(node))
                return false;

            visiting.Add(node);
            foreach (var child in node.Children)
            {
                if (HasCycle(child, visiting, visited))
                    return true;
            }

            visiting.Remove(node);
            visited.Add(node);
            return false;
        }
    }
}
#endif
