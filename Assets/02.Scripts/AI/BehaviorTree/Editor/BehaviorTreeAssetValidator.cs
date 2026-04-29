#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;

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

                if (node is BTCompositeNode && node.Children.Count == 0)
                    messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Error, $"{FormatNode(node)}: Composite 노드는 최소 1개 자식이 필요합니다.", node));

                if (node is BTDecoratorNode && node.Children.Count != 1)
                    messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Error, $"{FormatNode(node)}: Decorator 노드는 정확히 1개 자식이 필요합니다. 현재 자식 수: {node.Children.Count}", node));

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
            }

            foreach (var node in tree.Nodes.Where(node => node != null && node != tree.RootNode && !referenced.Contains(node)))
                messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Warning, $"{FormatNode(node)}: Root에서 연결되지 않은 노드입니다.", node));

            if (tree.RootNode != null && HasCycle(tree.RootNode, new HashSet<BTNode>(), new HashSet<BTNode>()))
                messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Error, "순환 참조가 있습니다."));

            if (messages.Count == 0)
                messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Info, "검증 오류가 없습니다."));

            return messages;
        }

        private static string FormatNode(BTNode node)
        {
            if (node == null)
                return "null";

            node.EnsureGuid();
            var shortGuid = node.Guid.Length > 8 ? node.Guid.Substring(0, 8) : node.Guid;
            return $"{node.DisplayName} ({node.GetType().Name}, guid:{shortGuid})";
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
