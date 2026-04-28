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
        public BehaviorTreeValidationMessage(BehaviorTreeValidationLevel level, string message)
        {
            Level = level;
            Message = message;
        }

        public BehaviorTreeValidationLevel Level { get; }
        public string Message { get; }
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

            foreach (var node in tree.Nodes)
            {
                if (node == null)
                {
                    messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Error, "비어 있는 노드 참조가 있습니다."));
                    continue;
                }

                node.EnsureGuid();

                if (node is BTCompositeNode && node.Children.Count == 0)
                    messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Error, $"{node.DisplayName}: Composite 노드는 최소 1개 자식이 필요합니다."));

                if (node is BTDecoratorNode && node.Children.Count != 1)
                    messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Error, $"{node.DisplayName}: Decorator 노드는 정확히 1개 자식이 필요합니다."));

                foreach (var child in node.Children)
                {
                    if (child == null || !tree.Nodes.Contains(child))
                        messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Error, $"{node.DisplayName}: 끊어진 자식 노드 참조가 있습니다."));
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
                messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Warning, $"{node.DisplayName}: Root에서 연결되지 않은 노드입니다."));

            if (tree.RootNode != null && HasCycle(tree.RootNode, new HashSet<BTNode>(), new HashSet<BTNode>()))
                messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Error, "순환 참조가 있습니다."));

            if (messages.Count == 0)
                messages.Add(new BehaviorTreeValidationMessage(BehaviorTreeValidationLevel.Info, "검증 오류가 없습니다."));

            return messages;
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
