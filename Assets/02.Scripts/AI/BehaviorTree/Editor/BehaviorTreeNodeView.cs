#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public class BehaviorTreeNodeView : Node
    {
        private readonly VisualElement _statusBar;
        private readonly Color _baseBorderColor;

        public BehaviorTreeNodeView(BTNode node)
        {
            Node = node;
            Node.EnsureGuid();
            viewDataKey = Node.Guid;
            title = Node.DisplayName;

            style.left = Node.EditorPosition.x;
            style.top = Node.EditorPosition.y;
            style.width = 170f;
            style.borderTopWidth = 1f;
            style.borderRightWidth = 1f;
            style.borderBottomWidth = 1f;
            style.borderLeftWidth = 1f;
            style.borderTopLeftRadius = 5f;
            style.borderTopRightRadius = 5f;
            style.borderBottomLeftRadius = 5f;
            style.borderBottomRightRadius = 5f;
            style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);

            _baseBorderColor = GetCategoryColor(Node);
            SetBorderColor(_baseBorderColor);

            titleContainer.style.backgroundColor = GetTitleColor(Node);
            titleContainer.style.minHeight = 26f;
            titleContainer.style.paddingLeft = 6f;
            titleContainer.style.paddingRight = 6f;

            Input = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));
            Input.portName = "";
            inputContainer.Add(Input);
            ConfigurePortContainer(inputContainer);

            if (Node is BTCompositeNode)
                Output = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Multi, typeof(bool));
            else if (Node is BTDecoratorNode)
                Output = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Single, typeof(bool));

            if (Output != null)
            {
                Output.portName = "";
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
            _statusBar.style.height = 4;
            _statusBar.style.backgroundColor = _baseBorderColor;
            mainContainer.Insert(0, _statusBar);

            var typeLabel = new Label(Node.GetType().Name);
            typeLabel.style.fontSize = 10;
            typeLabel.style.color = new Color(0.66f, 0.66f, 0.66f);
            typeLabel.style.paddingLeft = 6f;
            typeLabel.style.paddingBottom = 4f;
            extensionContainer.Add(typeLabel);

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
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Root로 설정", _ => OnSetRoot?.Invoke(this));
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
                return new Color(0.20f, 0.58f, 0.30f);
            if (node is BTDecoratorNode)
                return new Color(0.50f, 0.48f, 0.76f);
            if (node is BTConditionNode)
                return new Color(0.70f, 0.52f, 0.20f);
            return new Color(0.32f, 0.48f, 0.68f);
        }

        private static Color GetTitleColor(BTNode node)
        {
            var color = GetCategoryColor(node);
            return new Color(color.r * 0.45f, color.g * 0.45f, color.b * 0.45f, 1f);
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
