#if UNITY_EDITOR
using System;
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
        public static readonly Color Sequence = new(0.26f, 0.78f, 0.76f);
        public static readonly Color Selector = new(0.42f, 0.58f, 0.96f);
        public static readonly Color WeightedSelector = new(0.94f, 0.68f, 0.24f);
        public static readonly Color Parallel = new(0.86f, 0.38f, 0.48f);

        public static readonly Color Running = new(0.36f, 0.95f, 0.52f);
        public static readonly Color Success = new(0.30f, 0.82f, 0.42f);
        public static readonly Color Failure = new(0.92f, 0.28f, 0.22f);
        public static readonly Color Paused = new(0.96f, 0.68f, 0.25f);
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

    public partial class BehaviorTreeNodeView : Node
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
            style.width = 224f;
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

        public event Action<BehaviorTreeNodeView> OnSetRoot;

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            Node.EditorPosition = new Vector2(newPos.xMin, newPos.yMin);
        }

        public void RefreshView()
        {
            _displayNameLabel.text = BehaviorTreeDisplayNameRegistry.GetNodeTitle(Node);
            _categoryLabel.text = $"{GetCategoryName(Node)} · {Node.GetType().Name} · {Node.DisplayName} · #{_nodeIndex}";
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

        // 마지막으로 적용한 비주얼 키. 디버그 갱신은 6.6~10Hz로 전 노드를 순회하므로,
        // 실제 상태가 바뀐 노드만 스타일을 다시 쓰도록 캐싱해 대형 트리(180+ 노드)의 리페인트 폭주를 막는다.
        private int _lastVisualKey = int.MinValue;

        // 상태 라벨을 매 갱신마다 ToString().ToUpperInvariant()로 새로 할당하지 않도록 사전 캐싱.
        private static string StatusLabel(BTStatus status) => status switch
        {
            BTStatus.Running => "RUNNING",
            BTStatus.Success => "SUCCESS",
            BTStatus.Failure => "FAILURE",
            _ => status.ToString().ToUpperInvariant()
        };

        public void UpdateStateColor(BTNode runtimeNode, bool wasTickedThisFrame = false, BTStatus tickStatus = BTStatus.Failure, bool force = false)
        {
            bool started = runtimeNode != null && runtimeNode.IsStarted;
            int disabledBit = Node.Disabled ? 1 : 0;

            // 비주얼에 영향을 주는 모든 입력을 키에 인코딩한다(mode 0~1bit / status 2~3bit / disabled 4bit).
            // ApplyIdleVisual·ApplyRuntimeVisual·SetRuntimeState가 읽는 값과 1:1로 대응해야 누락 없이 안전.
            int key;
            if (!started)
                key = wasTickedThisFrame ? (1 | ((int)tickStatus << 2) | (disabledBit << 4)) : (disabledBit << 4);
            else
                key = 2 | ((int)runtimeNode.LastStatus << 2) | (disabledBit << 4);

            if (!force && key == _lastVisualKey)
                return;
            _lastVisualKey = key;

            if (!started)
            {
                if (wasTickedThisFrame)
                {
                    var tickColor = GetStatusColor(tickStatus);
                    ApplyRuntimeVisual(StatusLabel(tickStatus), tickColor, true);
                    return;
                }

                ApplyIdleVisual();
                SetRuntimeState("IDLE", BehaviorTreeEditorStyles.Idle);
                style.opacity = Node.Disabled ? 0.45f : 1f;
                return;
            }

            var stateColor = GetStatusColor(runtimeNode.LastStatus);
            ApplyRuntimeVisual(StatusLabel(runtimeNode.LastStatus), stateColor, true);
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
