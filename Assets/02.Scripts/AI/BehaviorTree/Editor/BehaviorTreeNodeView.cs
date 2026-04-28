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

        public BehaviorTreeNodeView(BTNode node)
        {
            Node = node;
            Node.EnsureGuid();
            viewDataKey = Node.Guid;
            title = Node.DisplayName;

            style.left = Node.EditorPosition.x;
            style.top = Node.EditorPosition.y;

            Input = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));
            Input.portName = "";
            inputContainer.Add(Input);

            if (Node is BTCompositeNode)
                Output = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Multi, typeof(bool));
            else if (Node is BTDecoratorNode)
                Output = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Single, typeof(bool));

            if (Output != null)
            {
                Output.portName = "";
                outputContainer.Add(Output);
            }

            _statusBar = new VisualElement();
            _statusBar.style.height = 4;
            _statusBar.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            mainContainer.Insert(0, _statusBar);

            var typeLabel = new Label(Node.GetType().Name);
            typeLabel.style.fontSize = 10;
            typeLabel.style.color = new Color(0.62f, 0.62f, 0.62f);
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
                _statusBar.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
                return;
            }

            _statusBar.style.backgroundColor = runtimeNode.LastStatus switch
            {
                BTStatus.Running => new Color(0.95f, 0.72f, 0.18f),
                BTStatus.Success => new Color(0.20f, 0.75f, 0.32f),
                BTStatus.Failure => new Color(0.88f, 0.22f, 0.22f),
                _ => new Color(0.25f, 0.25f, 0.25f)
            };
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Root로 설정", _ => OnSetRoot?.Invoke(this));
            base.BuildContextualMenu(evt);
        }

        public event Action<BehaviorTreeNodeView> OnSetRoot;
    }
}
#endif
