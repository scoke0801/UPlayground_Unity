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
        private const float LayoutServiceSpacing = 130f;
        private const float LayoutRootMarginX = 140f;
        private const float LayoutRootMarginY = 120f;
        private const float LayoutServiceOffsetX = 340f;

        private static void ApplyTopDownLayout(BehaviorTreeAsset tree)
        {
            if (tree?.RootNode == null)
                return;

            LayoutTreeTopDown(tree.RootNode, 0f, 0f, new HashSet<BTNode>());
            LayoutServices(tree);
            NormalizeLayout(tree);
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
    }
}
#endif
