#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public class BehaviorTreeNodeView : Node
    {
        private readonly VisualElement _statusBar;
        private readonly Label _statusLabel;
        private readonly VisualElement _statusDot;
        private readonly Label _issueLabel;
        private readonly Color _baseBorderColor;
        private readonly int _nodeIndex;
        private VisualElement _flagsRow;

        public BehaviorTreeNodeView(BTNode node, int nodeIndex)
        {
            Node = node;
            _nodeIndex = nodeIndex;
            Node.EnsureGuid();
            viewDataKey = Node.Guid;
            title = "";

            style.left = Node.EditorPosition.x;
            style.top = Node.EditorPosition.y;
            style.width = 240f;
            style.borderTopWidth = 1.5f;
            style.borderRightWidth = 1.5f;
            style.borderBottomWidth = 1.5f;
            style.borderLeftWidth = 1.5f;
            style.borderTopLeftRadius = 8f;
            style.borderTopRightRadius = 8f;
            style.borderBottomLeftRadius = 8f;
            style.borderBottomRightRadius = 8f;
            style.backgroundColor = GetBodyColor(Node);

            _baseBorderColor = GetCategoryColor(Node);
            SetBorderColor(_baseBorderColor);

            titleContainer.Clear();
            titleContainer.style.backgroundColor = GetHeaderColor(Node);
            titleContainer.style.minHeight = 40f;
            titleContainer.style.paddingLeft = 10f;
            titleContainer.style.paddingRight = 10f;
            titleContainer.style.paddingTop = 8f;
            titleContainer.style.paddingBottom = 7f;
            titleContainer.style.flexDirection = FlexDirection.Row;
            titleContainer.style.alignItems = Align.Center;
            titleContainer.Add(CreateTypePill(Node));

            Input = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));
            Input.portName = "IN";
            inputContainer.Add(Input);
            ConfigurePortContainer(inputContainer);

            if (Node is BTCompositeNode)
            {
                Output = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Multi, typeof(bool));
                Output.portName = "CHILDREN";
            }
            else if (Node is BTDecoratorNode)
            {
                Output = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Single, typeof(bool));
                Output.portName = "CHILD";
            }

            if (Output != null)
            {
                outputContainer.Add(Output);
                ConfigurePortContainer(outputContainer);
            }

            topContainer.style.flexDirection = FlexDirection.Column;
            topContainer.style.alignItems = Align.Stretch;
            topContainer.Insert(0, inputContainer);
            topContainer.Add(titleContainer);
            if (Output != null)
                topContainer.Add(outputContainer);

            _statusBar = new VisualElement();
            _statusBar.style.height = 3;
            _statusBar.style.backgroundColor = _baseBorderColor;
            mainContainer.Insert(0, _statusBar);

            extensionContainer.style.backgroundColor = GetBodyColor(Node);
            extensionContainer.style.paddingTop = 6f;
            extensionContainer.style.paddingBottom = 6f;
            extensionContainer.Add(CreateTitleBlock(Node, _nodeIndex));
            extensionContainer.Add(CreateParamBlock(Node));
            _flagsRow = CreateEditorFlags(Node);
            extensionContainer.Add(_flagsRow);

            _issueLabel = CreateIssueLabel(Node);
            if (!string.IsNullOrWhiteSpace(_issueLabel.text))
                extensionContainer.Add(_issueLabel);

            if (!string.IsNullOrWhiteSpace(Node.Comment))
                extensionContainer.Add(CreateCommentPreview(Node.Comment));

            var footer = CreateRuntimeFooter(_nodeIndex, out _statusDot, out _statusLabel);
            extensionContainer.Add(footer);

            RefreshExpandedState();
            RefreshPorts();
            UpdateStateColor(null);
        }

        public BTNode Node { get; }
        public Port Input { get; }
        public Port Output { get; }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            Node.EditorPosition = new Vector2(newPos.xMin, newPos.yMin);
        }

        public void UpdateStateColor(BTNode runtimeNode)
        {
            if (runtimeNode == null || !runtimeNode.IsStarted)
            {
                _statusBar.style.backgroundColor = _baseBorderColor;
                SetBorderColor(_baseBorderColor);
                SetRuntimeState("IDLE", new Color(0.29f, 0.29f, 0.37f));
                return;
            }

            var stateColor = runtimeNode.LastStatus switch
            {
                BTStatus.Running => new Color(0.95f, 0.72f, 0.18f),
                BTStatus.Success => new Color(0.20f, 0.75f, 0.32f),
                BTStatus.Failure => new Color(0.88f, 0.22f, 0.22f),
                _ => _baseBorderColor
            };
            _statusBar.style.backgroundColor = stateColor;
            SetBorderColor(stateColor);
            SetRuntimeState(runtimeNode.LastStatus.ToString().ToUpperInvariant(), stateColor);
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Root로 설정", _ => OnSetRoot?.Invoke(this));
            evt.menu.AppendAction(
                Node.Breakpoint ? "Breakpoint 해제" : "Breakpoint 설정",
                _ => ToggleBreakpoint());
            evt.menu.AppendAction(
                Node.Disabled ? "Enable Node" : "Disable Node",
                _ => ToggleDisabled());
            base.BuildContextualMenu(evt);
        }

        public event Action<BehaviorTreeNodeView> OnSetRoot;

        private void SetBorderColor(Color color)
        {
            style.borderTopColor = color;
            style.borderRightColor = color;
            style.borderBottomColor = color;
            style.borderLeftColor = color;
        }

        private static Color GetCategoryColor(BTNode node)
        {
            if (node is BTCompositeNode)
                return new Color(0.34f, 0.48f, 0.86f);
            if (node is BTDecoratorNode)
                return new Color(0.72f, 0.42f, 0.86f);
            if (node is BTConditionNode)
                return new Color(0.90f, 0.62f, 0.24f);
            return new Color(0.34f, 0.78f, 0.52f);
        }

        private static string GetCategoryName(BTNode node)
        {
            if (node is BTCompositeNode)
                return "COMPOSITE";
            if (node is BTDecoratorNode)
                return "DECORATOR";
            if (node is BTConditionNode)
                return "CONDITION";
            return "ACTION";
        }

        private static Color GetHeaderColor(BTNode node)
        {
            var color = GetCategoryColor(node);
            return new Color(color.r * 0.22f, color.g * 0.22f, color.b * 0.22f, 1f);
        }

        private static Color GetBodyColor(BTNode node)
        {
            var color = GetCategoryColor(node);
            return new Color(color.r * 0.12f + 0.06f, color.g * 0.12f + 0.06f, color.b * 0.12f + 0.07f, 1f);
        }

        private static string GetShortGuid(BTNode node)
        {
            node.EnsureGuid();
            return node.Guid.Length > 8 ? node.Guid.Substring(0, 8) : node.Guid;
        }

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

        private static VisualElement CreateTitleBlock(BTNode node, int index)
        {
            var block = new VisualElement();
            block.style.flexGrow = 1;
            block.style.flexShrink = 1;
            block.style.paddingLeft = 10f;
            block.style.paddingRight = 10f;
            block.style.paddingTop = 8f;
            block.style.paddingBottom = 5f;

            var displayName = new Label(node.DisplayName);
            displayName.style.fontSize = 13f;
            displayName.style.unityFontStyleAndWeight = FontStyle.Bold;
            displayName.style.color = new Color(0.92f, 0.92f, 0.92f);
            displayName.style.whiteSpace = WhiteSpace.NoWrap;
            displayName.style.overflow = Overflow.Hidden;
            displayName.style.textOverflow = TextOverflow.Ellipsis;
            block.Add(displayName);

            var category = new Label($"{node.GetType().Name} · #{index}");
            category.style.fontSize = 10f;
            category.style.color = new Color(0.42f, 0.42f, 0.52f);
            category.style.marginTop = 3f;
            block.Add(category);

            return block;
        }

        private static VisualElement CreateParamBlock(BTNode node)
        {
            var block = new VisualElement();
            block.style.paddingLeft = 10f;
            block.style.paddingRight = 10f;
            block.style.paddingTop = 6f;
            block.style.paddingBottom = 6f;
            block.style.borderTopColor = new Color(1f, 1f, 1f, 0.06f);
            block.style.borderTopWidth = 1f;

            block.Add(CreateParamRow("guid", GetShortGuid(node)));
            block.Add(CreateParamRow("role", GetChildSummary(node)));
            if (node is SequenceNode sequence)
                block.Add(CreateParamRow("abort", sequence.AbortType.ToString()));
            else if (node is SelectorNode selector)
                block.Add(CreateParamRow("abort", selector.AbortType.ToString()));

            return block;
        }

        private static VisualElement CreateParamRow(string key, string value)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.marginBottom = 2f;

            var keyLabel = new Label(key);
            keyLabel.style.fontSize = 10f;
            keyLabel.style.color = new Color(1f, 1f, 1f, 0.40f);
            row.Add(keyLabel);

            var valueLabel = new Label(value);
            valueLabel.style.fontSize = 10f;
            valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            valueLabel.style.color = new Color(1f, 1f, 1f, 0.76f);
            valueLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            row.Add(valueLabel);
            return row;
        }

        private static Label CreatePill(string text, Color color)
        {
            var label = new Label(text);
            label.style.fontSize = 9f;
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
            label.style.marginLeft = 8f;
            label.style.marginRight = 8f;
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
            footer.style.paddingTop = 6f;
            footer.style.paddingBottom = 6f;
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

        private void SetRuntimeState(string label, Color color)
        {
            _statusLabel.text = label;
            _statusLabel.style.color = color;
            _statusDot.style.backgroundColor = color;
        }

        private static void ConfigurePortContainer(VisualElement container)
        {
            container.style.flexDirection = FlexDirection.Row;
            container.style.justifyContent = Justify.Center;
            container.style.alignItems = Align.Center;
            container.style.height = 16f;
            container.style.minHeight = 16f;
            container.style.width = Length.Percent(100);
            container.style.flexGrow = 0f;
        }
    }
}
#endif
