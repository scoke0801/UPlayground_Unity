#if UNITY_EDITOR
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public partial class BehaviorTreeNodeView
    {
        private void SetBorderColor(Color color)
        {
            style.borderTopColor = color;
            style.borderRightColor = color;
            style.borderBottomColor = color;
            style.borderLeftColor = color;
        }

        private void ApplyIdleVisual()
        {
            var color = Node.Disabled ? BehaviorTreeEditorStyles.Idle : _baseBorderColor;
            _statusBar.style.height = 3f;
            _statusBar.style.backgroundColor = color;
            SetBorderColor(color);
            titleContainer.style.backgroundColor = GetHeaderColor(Node);
            extensionContainer.style.backgroundColor = GetBodyColor(Node);
            style.backgroundColor = GetBodyColor(Node);
            style.opacity = Node.Disabled ? 0.45f : 1f;
        }

        private void ApplyRuntimeVisual(string label, Color color, bool emphasized)
        {
            _statusBar.style.height = emphasized ? 7f : 3f;
            _statusBar.style.backgroundColor = color;
            SetBorderColor(color);
            SetRuntimeState(label, color);
            titleContainer.style.backgroundColor = BehaviorTreeEditorStyles.WithAlpha(color, 0.42f);
            extensionContainer.style.backgroundColor = BehaviorTreeEditorStyles.WithAlpha(color, 0.20f);
            style.backgroundColor = BehaviorTreeEditorStyles.WithAlpha(color, 0.16f);
            style.opacity = Node.Disabled ? 0.45f : 1f;
        }

        private Color GetStatusColor(BTStatus status)
        {
            return status switch
            {
                BTStatus.Running => BehaviorTreeEditorStyles.Running,
                BTStatus.Success => BehaviorTreeEditorStyles.Success,
                BTStatus.Failure => BehaviorTreeEditorStyles.Failure,
                _ => _baseBorderColor
            };
        }

        private static Color GetCategoryColor(BTNode node)
        {
            if (node is WeightedRandomSelectorNode)
                return BehaviorTreeEditorStyles.WeightedSelector;
            if (node is SequenceNode)
                return BehaviorTreeEditorStyles.Sequence;
            if (node is SelectorNode)
                return BehaviorTreeEditorStyles.Selector;
            if (node is ParallelNode)
                return BehaviorTreeEditorStyles.Parallel;
            if (node is BTCompositeNode)
                return BehaviorTreeEditorStyles.Composite;
            if (node is BTDecoratorNode)
                return BehaviorTreeEditorStyles.Decorator;
            if (node is BTConditionNode)
                return BehaviorTreeEditorStyles.Condition;
            return BehaviorTreeEditorStyles.Action;
        }

        private static string GetCategoryName(BTNode node)
        {
            if (node is SequenceNode)
                return "SEQ ALL";
            if (node is WeightedRandomSelectorNode)
                return "SEL WGT";
            if (node is SelectorNode)
                return "SEL ANY";
            if (node is ParallelNode)
                return "PARALLEL";
            if (node is BTCompositeNode)
                return "COMPOSITE";
            if (node is BTDecoratorNode)
                return "DECORATOR";
            if (node is BTConditionNode)
                return "CONDITION";
            return "ACTION";
        }

        private static string GetFlowRuleText(BTNode node)
        {
            if (node is SequenceNode)
                return "ALL children must succeed";
            if (node is WeightedRandomSelectorNode)
                return "Weighted pick until one succeeds";
            if (node is SelectorNode)
                return "First child that succeeds wins";
            if (node is ParallelNode)
                return "Children run in parallel";
            return string.Empty;
        }

        private static Color GetHeaderColor(BTNode node)
        {
            var color = GetCategoryColor(node);
            return BehaviorTreeEditorStyles.Darken(color, 0.22f);
        }

        private static Color GetBodyColor(BTNode node)
        {
            return BehaviorTreeEditorStyles.Body(GetCategoryColor(node));
        }

        private static string GetShortGuid(BTNode node)
        {
            node.EnsureGuid();
            return node.Guid.Length > 8 ? node.Guid.Substring(0, 8) : node.Guid;
        }

        private void SetRuntimeState(string label, Color color)
        {
            _statusLabel.text = label;
            _statusLabel.style.color = color;
            _statusDot.style.backgroundColor = color;
        }
    }
}
#endif
