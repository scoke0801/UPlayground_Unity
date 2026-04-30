#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    internal static class BehaviorTreeEditorStyles
    {
        public static readonly Color Background = new(0.06f, 0.06f, 0.07f);
        public static readonly Color Panel = new(0.075f, 0.075f, 0.10f);
        public static readonly Color PanelAlt = new(0.09f, 0.09f, 0.11f);
        public static readonly Color PanelRaised = new(0.12f, 0.12f, 0.15f);
        public static readonly Color Border = new(0.18f, 0.18f, 0.22f);
        public static readonly Color BorderStrong = new(0.23f, 0.23f, 0.29f);
        public static readonly Color Text = new(0.90f, 0.90f, 0.94f);
        public static readonly Color TextMuted = new(0.58f, 0.58f, 0.68f);
        public static readonly Color TextDim = new(0.36f, 0.36f, 0.45f);

        public static readonly Color Composite = new(0.34f, 0.48f, 0.86f);
        public static readonly Color Action = new(0.34f, 0.78f, 0.52f);
        public static readonly Color Condition = new(0.90f, 0.62f, 0.24f);
        public static readonly Color Decorator = new(0.72f, 0.42f, 0.86f);

        public static readonly Color Running = new(0.36f, 0.95f, 0.52f);
        public static readonly Color Success = new(0.30f, 0.82f, 0.42f);
        public static readonly Color Failure = new(0.92f, 0.28f, 0.22f);
        public static readonly Color Idle = new(0.29f, 0.29f, 0.37f);

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        public static Color Darken(Color color, float amount)
        {
            return new Color(color.r * amount, color.g * amount, color.b * amount, color.a);
        }

        public static Color Body(Color color)
        {
            return new Color(color.r * 0.10f + 0.055f, color.g * 0.10f + 0.055f, color.b * 0.10f + 0.065f, 1f);
        }
    }

    public class BehaviorTreeNodeView : Node
    {
        private readonly VisualElement _statusBar;
        private Label _statusLabel;
        private VisualElement _statusDot;
        private readonly Label _issueLabel;
        private readonly Color _baseBorderColor;
        private readonly int _nodeIndex;
        private VisualElement _flagsRow;
        private Label _displayNameLabel;
        private Label _categoryLabel;
        private VisualElement _paramBlock;
        private Label _commentLabel;
        private VisualElement _footer;

        public BehaviorTreeNodeView(BTNode node, int nodeIndex)
        {
            Node = node;
            _nodeIndex = nodeIndex;
            Node.EnsureGuid();
            viewDataKey = Node.Guid;
            title = "";

            style.left = Node.EditorPosition.x;
            style.top = Node.EditorPosition.y;
            style.width = 160f;
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
            titleContainer.style.minHeight = 30f;
            titleContainer.style.paddingLeft = 9f;
            titleContainer.style.paddingRight = 9f;
            titleContainer.style.paddingTop = 6f;
            titleContainer.style.paddingBottom = 5f;
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
            extensionContainer.style.paddingTop = 3f;
            extensionContainer.style.paddingBottom = 4f;
            extensionContainer.Add(CreateTitleBlock(Node, _nodeIndex, out _displayNameLabel, out _categoryLabel));
            _paramBlock = CreateParamBlock(Node);
            extensionContainer.Add(_paramBlock);
            _flagsRow = CreateEditorFlags(Node);
            extensionContainer.Add(_flagsRow);

            _issueLabel = CreateIssueLabel(Node);
            if (!string.IsNullOrWhiteSpace(_issueLabel.text))
                extensionContainer.Add(_issueLabel);

            _commentLabel = CreateCommentPreview(Node.Comment);
            _commentLabel.style.display = string.IsNullOrWhiteSpace(Node.Comment) ? DisplayStyle.None : DisplayStyle.Flex;
            extensionContainer.Add(_commentLabel);

            _footer = CreateRuntimeFooter(_nodeIndex, out _statusDot, out _statusLabel);
            extensionContainer.Add(_footer);

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

        public void RefreshView()
        {
            _displayNameLabel.text = Node.DisplayName;
            _categoryLabel.text = $"{Node.GetType().Name} · #{_nodeIndex}";
            RefreshParamBlock(_paramBlock, Node);
            RefreshEditorFlags(_flagsRow, Node);

            var issue = GetStructuralIssue(Node);
            _issueLabel.text = issue;
            _issueLabel.style.display = string.IsNullOrWhiteSpace(issue) ? DisplayStyle.None : DisplayStyle.Flex;

            _commentLabel.text = Node.Comment;
            _commentLabel.style.display = string.IsNullOrWhiteSpace(Node.Comment) ? DisplayStyle.None : DisplayStyle.Flex;
            RefreshExpandedState();
            MarkDirtyRepaint();
        }

        public void UpdateStateColor(BTNode runtimeNode)
        {
            if (runtimeNode == null || !runtimeNode.IsStarted)
            {
                _statusBar.style.backgroundColor = Node.Disabled
                    ? BehaviorTreeEditorStyles.Idle
                    : _baseBorderColor;
                SetBorderColor(Node.Disabled ? BehaviorTreeEditorStyles.Idle : _baseBorderColor);
                SetRuntimeState("IDLE", new Color(0.29f, 0.29f, 0.37f));
                style.opacity = Node.Disabled ? 0.45f : 1f;
                return;
            }

            var stateColor = runtimeNode.LastStatus switch
            {
                BTStatus.Running => BehaviorTreeEditorStyles.Running,
                BTStatus.Success => BehaviorTreeEditorStyles.Success,
                BTStatus.Failure => BehaviorTreeEditorStyles.Failure,
                _ => _baseBorderColor
            };
            _statusBar.style.backgroundColor = stateColor;
            SetBorderColor(stateColor);
            SetRuntimeState(runtimeNode.LastStatus.ToString().ToUpperInvariant(), stateColor);
            style.opacity = Node.Disabled ? 0.45f : 1f;
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
                return BehaviorTreeEditorStyles.Composite;
            if (node is BTDecoratorNode)
                return BehaviorTreeEditorStyles.Decorator;
            if (node is BTConditionNode)
                return BehaviorTreeEditorStyles.Condition;
            return BehaviorTreeEditorStyles.Action;
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

        private static VisualElement CreateTitleBlock(BTNode node, int index, out Label displayName, out Label category)
        {
            var block = new VisualElement();
            block.style.flexGrow = 1;
            block.style.flexShrink = 1;
            block.style.paddingLeft = 10f;
            block.style.paddingRight = 10f;
            block.style.paddingTop = 8f;
            block.style.paddingBottom = 5f;

            displayName = new Label(node.DisplayName);
            displayName.style.fontSize = 12f;
            displayName.style.unityFontStyleAndWeight = FontStyle.Bold;
            displayName.style.color = new Color(0.92f, 0.92f, 0.92f);
            displayName.style.whiteSpace = WhiteSpace.NoWrap;
            displayName.style.overflow = Overflow.Hidden;
            displayName.style.textOverflow = TextOverflow.Ellipsis;
            block.Add(displayName);

            category = new Label($"{node.GetType().Name} · #{index}");
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
            block.Add(CreateParamRow("role", GetChildSummary(node)));
            if (node is SequenceNode sequence)
                block.Add(CreateParamRow("abort", sequence.AbortType.ToString()));
            else if (node is SelectorNode selector)
                block.Add(CreateParamRow("abort", selector.AbortType.ToString()));
            else
                block.style.display = DisplayStyle.None;
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
