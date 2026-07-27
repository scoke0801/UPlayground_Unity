#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public static partial class MonsterBehaviorTreeJsonImporter
    {
        private const float LayoutHorizontalSpacing = 390f;
        private const float LayoutVerticalSpacing = 270f;
        private const float CompactRuleColumnSpacing = 285f;
        private const float CompactNodeVerticalSpacing = 270f;
        private const float CompactNodeHalfWidth = 112f;
        private const float CompactBranchHorizontalOffset = 18f;
        private const float CompactGroupGapX = 260f;
        private const float CompactRootGapY = 480f;
        private const float CompactCellPadding = 220f;
        private const float LayoutServiceSpacing = 130f;
        private const float LayoutRootMarginX = 140f;
        private const float LayoutRootMarginY = 120f;
        private const float LayoutServiceOffsetX = 340f;

        internal static void ApplyReadableLayout(BehaviorTreeAsset tree)
        {
            if (tree?.RootNode == null)
                return;

            if (!TryApplyGroupedDownwardLayout(tree))
                LayoutTreeTopDown(tree.RootNode, 0f, 0f, new HashSet<BTNode>());

            LayoutServices(tree);
            NormalizeLayout(tree);
        }

        private static bool TryApplyGroupedDownwardLayout(BehaviorTreeAsset tree)
        {
            var root = tree.RootNode;
            var groups = root.Children?
                .Where(child => child != null)
                .Select(CreateGroupLayoutInfo)
                .ToList();
            if (groups == null || groups.Count < 2 || groups.Any(group => group == null))
                return false;

            var totalWidth = groups.Sum(group => group.Width)
                             + (groups.Count - 1) * CompactGroupGapX;
            root.EditorPosition = new Vector2(
                totalWidth * 0.5f - CompactNodeHalfWidth,
                0f);

            var visited = new HashSet<BTNode> { root };
            var currentX = 0f;
            for (var i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                var origin = new Vector2(currentX, CompactRootGapY);
                LayoutGroupDownward(group, origin, group.Width, visited);
                currentX += group.Width + CompactGroupGapX;
            }

            return true;
        }

        private static GroupLayoutInfo CreateGroupLayoutInfo(BTNode groupNode)
        {
            if (!TryGetGeneratedGroupRuleParent(groupNode, out var ruleParent))
                return null;

            var rules = ruleParent.Children?
                .Where(child => child != null)
                .ToList() ?? new List<BTNode>();
            if (rules.Count == 0)
                return null;

            return new GroupLayoutInfo(
                groupNode,
                ruleParent,
                rules,
                Mathf.Max(1, rules.Count) * CompactRuleColumnSpacing + CompactCellPadding);
        }

        private static bool TryGetGeneratedGroupRuleParent(BTNode groupNode, out BTCompositeNode ruleParent)
        {
            ruleParent = null;
            if (groupNode is SelectorNode selector
                && selector.Children != null
                && selector.Children.Any(child => child is SequenceNode))
            {
                ruleParent = selector;
                return true;
            }

            if (groupNode is not SequenceNode sequence || sequence.Children == null || sequence.Children.Count == 0)
                return false;

            ruleParent = sequence.Children[^1] as SelectorNode;
            return ruleParent != null
                   && ruleParent.DisplayName != null
                   && ruleParent.DisplayName.EndsWith(" Rules", System.StringComparison.Ordinal);
        }

        private static void LayoutGroupDownward(
            GroupLayoutInfo group,
            Vector2 origin,
            float cellWidth,
            HashSet<BTNode> visited)
        {
            var centerX = origin.x + cellWidth * 0.5f - CompactNodeHalfWidth;
            var currentY = origin.y;

            group.GroupNode.EditorPosition = new Vector2(centerX, currentY);
            visited.Add(group.GroupNode);
            currentY += CompactNodeVerticalSpacing;

            if (group.GroupNode != group.RuleParent)
            {
                foreach (var child in group.GroupNode.Children.Where(child => child != null))
                {
                    if (child == group.RuleParent)
                    {
                        child.EditorPosition = new Vector2(centerX, currentY);
                        visited.Add(child);
                        currentY += CompactNodeVerticalSpacing;
                        continue;
                    }

                    LayoutDownwardSubtree(child, centerX, ref currentY, visited);
                }
            }

            var rulesWidth = (group.Rules.Count - 1) * CompactRuleColumnSpacing;
            var firstRuleX = centerX - rulesWidth * 0.5f;
            for (var i = 0; i < group.Rules.Count; i++)
            {
                var ruleY = currentY;
                var ruleX = firstRuleX + i * CompactRuleColumnSpacing;
                LayoutDownwardSubtree(group.Rules[i], ruleX, ref ruleY, visited);
            }
        }

        private static void LayoutDownwardSubtree(
            BTNode node,
            float centerX,
            ref float currentY,
            HashSet<BTNode> visited,
            float branchOffset = 0f)
        {
            if (node == null || !visited.Add(node))
                return;

            node.EditorPosition = new Vector2(centerX + branchOffset, currentY);
            currentY += CompactNodeVerticalSpacing;

            var children = node.Children?
                .Where(child => child != null)
                .ToList() ?? new List<BTNode>();
            for (var i = 0; i < children.Count; i++)
            {
                var offset = node is SequenceNode || children.Count == 1
                    ? branchOffset
                    : branchOffset + (i - (children.Count - 1) * 0.5f) * CompactBranchHorizontalOffset;
                LayoutDownwardSubtree(children[i], centerX, ref currentY, visited, offset);
            }
        }

        private static float LayoutTreeTopDown(BTNode node, float leftX, float y, HashSet<BTNode> visited)
        {
            if (node == null || !visited.Add(node))
                return 0f;

            var children = node.Children?.Where(child => child != null).ToList() ?? new List<BTNode>();
            if (children.Count == 0)
            {
                node.EditorPosition = new Vector2(leftX, y);
                return LayoutHorizontalSpacing;
            }

            var currentX = leftX;
            var totalWidth = 0f;
            foreach (var child in children)
            {
                var childWidth = LayoutTreeTopDown(child, currentX, y + LayoutVerticalSpacing, visited);
                currentX += childWidth;
                totalWidth += childWidth;
            }

            node.EditorPosition = new Vector2(leftX + totalWidth * 0.5f - LayoutHorizontalSpacing * 0.5f, y);
            return Mathf.Max(LayoutHorizontalSpacing, totalWidth);
        }

        private static void LayoutServices(BehaviorTreeAsset tree)
        {
            foreach (var composite in tree.Nodes.OfType<BTCompositeNode>())
            {
                var services = composite.Services?.Where(service => service != null).ToList();
                if (services == null || services.Count == 0)
                    continue;

                var startOffset = -(services.Count - 1) * LayoutServiceSpacing * 0.5f;
                for (var i = 0; i < services.Count; i++)
                {
                    services[i].EditorPosition = composite.EditorPosition + new Vector2(
                        -LayoutServiceOffsetX,
                        startOffset + i * LayoutServiceSpacing);
                }
            }
        }

        private static void NormalizeLayout(BehaviorTreeAsset tree)
        {
            var layoutNodes = tree.Nodes
                .Where(node => node != null)
                .ToList();
            if (layoutNodes.Count == 0)
                return;

            var minX = layoutNodes.Min(node => node.EditorPosition.x);
            var minY = layoutNodes.Min(node => node.EditorPosition.y);
            var offset = new Vector2(LayoutRootMarginX - minX, LayoutRootMarginY - minY);

            foreach (var node in layoutNodes)
                node.EditorPosition += offset;
        }

        private sealed class GroupLayoutInfo
        {
            public BTNode GroupNode { get; }
            public BTCompositeNode RuleParent { get; }
            public List<BTNode> Rules { get; }
            public float Width { get; }

            public GroupLayoutInfo(
                BTNode groupNode,
                BTCompositeNode ruleParent,
                List<BTNode> rules,
                float width)
            {
                GroupNode = groupNode;
                RuleParent = ruleParent;
                Rules = rules;
                Width = width;
            }
        }
    }
}
#endif
