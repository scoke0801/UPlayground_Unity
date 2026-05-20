#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public partial class BehaviorTreeNodeView
    {
        private VisualElement CreateEditorFlags(BTNode node)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.paddingLeft = 8f;
            row.style.paddingRight = 8f;
            row.style.marginBottom = 5f;
            RefreshEditorFlags(row, node);
            return row;
        }

        private static void RefreshEditorFlags(VisualElement row, BTNode node)
        {
            row.Clear();
            row.style.display = node.Breakpoint || node.Disabled ? DisplayStyle.Flex : DisplayStyle.None;

            if (node.Breakpoint)
                row.Add(CreatePill("BREAKPOINT", new Color(0.70f, 0.24f, 0.20f)));
            if (node.Disabled)
                row.Add(CreatePill("DISABLED", new Color(0.34f, 0.34f, 0.34f)));
        }

        private void ToggleBreakpoint()
        {
            Undo.RecordObject(Node, "Toggle BT Breakpoint");
            Node.Breakpoint = !Node.Breakpoint;
            EditorUtility.SetDirty(Node);
            RefreshEditorFlags(_flagsRow, Node);
            RefreshExpandedState();
        }

        private void ToggleDisabled()
        {
            Undo.RecordObject(Node, "Toggle BT Disabled");
            Node.Disabled = !Node.Disabled;
            EditorUtility.SetDirty(Node);
            RefreshEditorFlags(_flagsRow, Node);
            RefreshExpandedState();
            // Edit 모드에서는 UpdateDebugState 루프가 돌지 않아 Disabled opacity가 즉시 반영되지 않으므로 강제 갱신.
            UpdateStateColor(null);
        }

        private static Label CreateTypePill(BTNode node)
        {
            var pill = new Label(GetCategoryName(node));
            pill.style.fontSize = 9f;
            pill.style.unityFontStyleAndWeight = FontStyle.Bold;
            pill.style.letterSpacing = 1f;
            pill.style.color = new Color(0.04f, 0.04f, 0.05f);
            pill.style.backgroundColor = GetCategoryColor(node);
            pill.style.paddingLeft = 8f;
            pill.style.paddingRight = 8f;
            pill.style.paddingTop = 2f;
            pill.style.paddingBottom = 2f;
            pill.style.borderTopLeftRadius = 8f;
            pill.style.borderTopRightRadius = 8f;
            pill.style.borderBottomLeftRadius = 8f;
            pill.style.borderBottomRightRadius = 8f;
            return pill;
        }

        private static VisualElement CreateTitleBlock(BTNode node, int index, out Label displayName, out Label category)
        {
            var block = new VisualElement();
            block.style.flexGrow = 1;
            block.style.flexShrink = 1;
            block.style.paddingLeft = 10f;
            block.style.paddingRight = 10f;
            block.style.paddingTop = 8f;
            block.style.paddingBottom = 5f;

            displayName = new Label(BehaviorTreeDisplayNameRegistry.GetNodeTitle(node));
            displayName.style.fontSize = 12f;
            displayName.style.unityFontStyleAndWeight = FontStyle.Bold;
            displayName.style.color = new Color(0.92f, 0.92f, 0.92f);
            displayName.style.whiteSpace = WhiteSpace.NoWrap;
            displayName.style.overflow = Overflow.Hidden;
            displayName.style.textOverflow = TextOverflow.Ellipsis;
            block.Add(displayName);

            category = new Label($"{node.GetType().Name} · {node.DisplayName} · #{index}");
            category.style.fontSize = 10f;
            category.style.color = BehaviorTreeEditorStyles.TextDim;
            category.style.marginTop = 3f;
            block.Add(category);

            return block;
        }

        private static VisualElement CreateParamBlock(BTNode node)
        {
            var block = new VisualElement();
            block.style.paddingLeft = 10f;
            block.style.paddingRight = 10f;
            block.style.paddingTop = 5f;
            block.style.paddingBottom = 4f;
            block.style.borderTopColor = new Color(1f, 1f, 1f, 0.06f);
            block.style.borderTopWidth = 1f;

            RefreshParamBlock(block, node);
            return block;
        }

        private static void RefreshParamBlock(VisualElement block, BTNode node)
        {
            block.Clear();
            block.style.display = DisplayStyle.Flex;

            var summary = new VisualElement();
            summary.style.flexDirection = FlexDirection.Row;
            summary.style.flexWrap = Wrap.Wrap;
            summary.style.marginBottom = 4f;
            summary.Add(CreateInfoChip(GetChildSummary(node), new Color(0.18f, 0.18f, 0.22f)));
            if (node is SequenceNode sequence)
                summary.Add(CreateInfoChip($"abort {sequence.AbortType}", new Color(0.22f, 0.18f, 0.28f)));
            else if (node is SelectorNode selector)
                summary.Add(CreateInfoChip($"abort {selector.AbortType}", new Color(0.22f, 0.18f, 0.28f)));
            block.Add(summary);

            if (node is WeightedRandomSelectorNode weighted)
                block.Add(CreateWeightBlock(weighted));

            foreach (var row in GetNodeSummaryRows(node))
                block.Add(CreateParamRow(row.Key, row.Value));

            if (block.childCount <= 1 && summary.childCount == 1)
                block.style.display = DisplayStyle.None;
        }

        private static VisualElement CreateParamRow(string key, string value)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 2f;
            row.style.minHeight = 15f;

            var keyLabel = new Label(key);
            keyLabel.style.width = 74f;
            keyLabel.style.flexShrink = 0f;
            keyLabel.style.fontSize = 10f;
            keyLabel.style.color = new Color(1f, 1f, 1f, 0.40f);
            row.Add(keyLabel);

            var valueLabel = new Label(value);
            valueLabel.style.flexGrow = 1f;
            valueLabel.style.flexShrink = 1f;
            valueLabel.style.fontSize = 10f;
            valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            valueLabel.style.color = new Color(1f, 1f, 1f, 0.76f);
            valueLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            valueLabel.style.whiteSpace = WhiteSpace.NoWrap;
            valueLabel.style.overflow = Overflow.Hidden;
            valueLabel.style.textOverflow = TextOverflow.Ellipsis;
            row.Add(valueLabel);
            return row;
        }

        private static Label CreateInfoChip(string text, Color color)
        {
            var chip = new Label(text);
            chip.style.fontSize = 9f;
            chip.style.unityFontStyleAndWeight = FontStyle.Bold;
            chip.style.color = new Color(0.82f, 0.86f, 0.88f);
            chip.style.backgroundColor = color;
            chip.style.paddingLeft = 6f;
            chip.style.paddingRight = 6f;
            chip.style.paddingTop = 2f;
            chip.style.paddingBottom = 2f;
            chip.style.marginRight = 4f;
            chip.style.marginBottom = 4f;
            chip.style.borderTopLeftRadius = 7f;
            chip.style.borderTopRightRadius = 7f;
            chip.style.borderBottomLeftRadius = 7f;
            chip.style.borderBottomRightRadius = 7f;
            return chip;
        }

        private static VisualElement CreateWeightBlock(WeightedRandomSelectorNode node)
        {
            var block = new VisualElement();
            block.style.marginTop = 2f;
            block.style.marginBottom = 5f;
            block.style.paddingLeft = 6f;
            block.style.paddingRight = 6f;
            block.style.paddingTop = 5f;
            block.style.paddingBottom = 5f;
            block.style.backgroundColor = new Color(0.04f, 0.05f, 0.055f, 0.72f);
            block.style.borderTopLeftRadius = 5f;
            block.style.borderTopRightRadius = 5f;
            block.style.borderBottomLeftRadius = 5f;
            block.style.borderBottomRightRadius = 5f;

            var totalWeight = 0f;
            for (var i = 0; i < node.Children.Count; i++)
                totalWeight += node.GetWeight(i);

            var header = new Label(totalWeight > 0f ? $"Weights  total {totalWeight:0.##}" : "Weights  uniform fallback");
            header.style.fontSize = 9f;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.color = new Color(0.76f, 0.78f, 0.82f);
            header.style.marginBottom = 4f;
            block.Add(header);

            var visibleCount = Mathf.Min(node.Children.Count, 4);
            for (var i = 0; i < visibleCount; i++)
                block.Add(CreateWeightRow(node, i, totalWeight));

            if (node.Children.Count > visibleCount)
            {
                var more = new Label($"+ {node.Children.Count - visibleCount} children");
                more.style.fontSize = 9f;
                more.style.color = BehaviorTreeEditorStyles.TextDim;
                more.style.unityTextAlign = TextAnchor.MiddleRight;
                more.style.marginTop = 2f;
                block.Add(more);
            }

            return block;
        }

        private static VisualElement CreateWeightRow(WeightedRandomSelectorNode node, int index, float totalWeight)
        {
            var weight = node.GetWeight(index);
            var chance = totalWeight > 0f ? weight / totalWeight : 0f;
            var childName = node.Children[index] != null
                ? BehaviorTreeDisplayNameRegistry.GetNodeTitle(node.Children[index])
                : "null";

            var row = new VisualElement();
            row.style.marginBottom = 3f;

            var labels = new VisualElement();
            labels.style.flexDirection = FlexDirection.Row;
            labels.style.alignItems = Align.Center;
            labels.style.justifyContent = Justify.SpaceBetween;

            var nameLabel = new Label(TrimText(childName, 20));
            nameLabel.style.fontSize = 9f;
            nameLabel.style.color = new Color(0.84f, 0.86f, 0.88f);
            nameLabel.style.flexGrow = 1f;
            nameLabel.style.overflow = Overflow.Hidden;
            nameLabel.style.textOverflow = TextOverflow.Ellipsis;
            labels.Add(nameLabel);

            var valueLabel = new Label($"w {weight:0.##}  {chance:P0}");
            valueLabel.style.fontSize = 9f;
            valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            valueLabel.style.color = new Color(0.92f, 0.82f, 0.46f);
            valueLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            labels.Add(valueLabel);
            row.Add(labels);

            var track = new VisualElement();
            track.style.height = 3f;
            track.style.marginTop = 2f;
            track.style.backgroundColor = new Color(1f, 1f, 1f, 0.08f);
            track.style.borderTopLeftRadius = 2f;
            track.style.borderTopRightRadius = 2f;
            track.style.borderBottomLeftRadius = 2f;
            track.style.borderBottomRightRadius = 2f;

            var fill = new VisualElement();
            fill.style.width = Length.Percent(Mathf.Clamp01(chance) * 100f);
            fill.style.height = 3f;
            fill.style.backgroundColor = new Color(0.92f, 0.72f, 0.28f);
            fill.style.borderTopLeftRadius = 2f;
            fill.style.borderTopRightRadius = 2f;
            fill.style.borderBottomLeftRadius = 2f;
            fill.style.borderBottomRightRadius = 2f;
            track.Add(fill);
            row.Add(track);

            return row;
        }

        private static Label CreatePill(string text, Color color)
        {
            var label = new Label(text);
            label.style.fontSize = 8f;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = Color.white;
            label.style.backgroundColor = color;
            label.style.paddingLeft = 6f;
            label.style.paddingRight = 6f;
            label.style.paddingTop = 2f;
            label.style.paddingBottom = 2f;
            label.style.marginRight = 5f;
            label.style.borderTopLeftRadius = 8f;
            label.style.borderTopRightRadius = 8f;
            label.style.borderBottomLeftRadius = 8f;
            label.style.borderBottomRightRadius = 8f;
            return label;
        }

        private static Label CreateMutedLabel(string text)
        {
            var label = new Label(text);
            label.style.fontSize = 9f;
            label.style.color = new Color(0.62f, 0.62f, 0.62f);
            label.style.marginRight = 5f;
            return label;
        }

        private static string GetChildSummary(BTNode node)
        {
            if (node is BTCompositeNode)
                return $"children:{node.Children.Count}";
            if (node is BTDecoratorNode)
                return $"child:{node.Children.Count}/1";
            return "leaf";
        }

        private static Label CreateIssueLabel(BTNode node)
        {
            var issue = GetStructuralIssue(node);
            var label = new Label(issue);
            label.style.fontSize = 10f;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.color = new Color(1f, 0.88f, 0.72f);
            label.style.backgroundColor = new Color(0.42f, 0.22f, 0.08f);
            label.style.marginLeft = 10f;
            label.style.marginRight = 10f;
            label.style.marginBottom = 5f;
            label.style.paddingLeft = 6f;
            label.style.paddingRight = 6f;
            label.style.paddingTop = 3f;
            label.style.paddingBottom = 3f;
            label.style.borderTopLeftRadius = 4f;
            label.style.borderTopRightRadius = 4f;
            label.style.borderBottomLeftRadius = 4f;
            label.style.borderBottomRightRadius = 4f;
            return label;
        }

        private static string GetStructuralIssue(BTNode node)
        {
            if (node is BTCompositeNode && node.Children.Count == 0)
                return "Needs at least 1 child";
            if (node is BTDecoratorNode && node.Children.Count != 1)
                return $"Needs exactly 1 child ({node.Children.Count})";
            return string.Empty;
        }

        private static Label CreateCommentPreview(string comment)
        {
            var label = new Label(comment);
            label.style.fontSize = 10f;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.color = new Color(0.72f, 0.72f, 0.72f);
            label.style.marginLeft = 8f;
            label.style.marginRight = 8f;
            label.style.marginBottom = 5f;
            label.style.paddingLeft = 6f;
            label.style.paddingRight = 6f;
            label.style.paddingTop = 3f;
            label.style.paddingBottom = 3f;
            label.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f);
            label.style.borderTopLeftRadius = 4f;
            label.style.borderTopRightRadius = 4f;
            label.style.borderBottomLeftRadius = 4f;
            label.style.borderBottomRightRadius = 4f;
            return label;
        }

        private static VisualElement CreateRuntimeFooter(int index, out VisualElement statusDot, out Label statusLabel)
        {
            var footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.justifyContent = Justify.SpaceBetween;
            footer.style.alignItems = Align.Center;
            footer.style.paddingLeft = 10f;
            footer.style.paddingRight = 10f;
            footer.style.paddingTop = 4f;
            footer.style.paddingBottom = 5f;
            footer.style.borderTopColor = new Color(1f, 1f, 1f, 0.06f);
            footer.style.borderTopWidth = 1f;

            var state = new VisualElement();
            state.style.flexDirection = FlexDirection.Row;
            state.style.alignItems = Align.Center;

            statusDot = new VisualElement();
            statusDot.style.width = 7f;
            statusDot.style.height = 7f;
            statusDot.style.marginRight = 6f;
            statusDot.style.borderTopLeftRadius = 4f;
            statusDot.style.borderTopRightRadius = 4f;
            statusDot.style.borderBottomLeftRadius = 4f;
            statusDot.style.borderBottomRightRadius = 4f;
            statusDot.style.backgroundColor = new Color(0.29f, 0.29f, 0.37f);
            state.Add(statusDot);

            statusLabel = new Label("IDLE");
            statusLabel.style.fontSize = 9f;
            statusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            statusLabel.style.letterSpacing = 1f;
            statusLabel.style.color = new Color(0.44f, 0.44f, 0.52f);
            state.Add(statusLabel);
            footer.Add(state);

            var indexLabel = new Label($"#{index}");
            indexLabel.style.fontSize = 9f;
            indexLabel.style.color = new Color(1f, 1f, 1f, 0.28f);
            footer.Add(indexLabel);
            return footer;
        }
    }
}
#endif
