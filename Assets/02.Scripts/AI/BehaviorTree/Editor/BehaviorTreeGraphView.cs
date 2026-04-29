#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public class BehaviorTreeGraphView : GraphView
    {
        private readonly BehaviorTreeEditorWindow _window;
        private readonly Dictionary<BTNode, BehaviorTreeNodeView> _nodeViews = new();
        private BehaviorTreeAsset _tree;

        public BehaviorTreeGraphView(BehaviorTreeEditorWindow window)
        {
            _window = window;
            style.flexGrow = 1;

            Insert(0, new GridBackground());
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            graphViewChanged += OnGraphViewChanged;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports
                .Where(port => port.direction != startPort.direction && port.node != startPort.node)
                .ToList();
        }

        public void PopulateView(BehaviorTreeAsset tree)
        {
            _tree = tree;
            graphViewChanged -= OnGraphViewChanged;
            DeleteElements(graphElements.ToList());
            _nodeViews.Clear();

            if (_tree == null)
            {
                graphViewChanged += OnGraphViewChanged;
                return;
            }

            foreach (var node in _tree.Nodes.Where(node => node != null))
                AddNodeView(node);

            foreach (var node in _tree.Nodes.Where(node => node != null))
            {
                if (!_nodeViews.TryGetValue(node, out var parentView) || parentView.Output == null)
                    continue;

                foreach (var child in node.Children)
                {
                    if (child == null || !_nodeViews.TryGetValue(child, out var childView))
                        continue;

                    var edge = parentView.Output.ConnectTo(childView.Input);
                    AddElement(edge);
                }
            }

            graphViewChanged += OnGraphViewChanged;
        }

        public void UpdateDebugState(BehaviorTreeAsset runtimeTree)
        {
            var runtimeByGuid = new Dictionary<string, BTNode>();
            if (runtimeTree != null)
            {
                foreach (var node in runtimeTree.Nodes)
                {
                    if (node != null)
                        runtimeByGuid[node.Guid] = node;
                }
            }

            foreach (var pair in _nodeViews)
            {
                runtimeByGuid.TryGetValue(pair.Key.Guid, out var runtimeNode);
                pair.Value.UpdateStateColor(runtimeNode);
            }
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (_tree == null)
            {
                base.BuildContextualMenu(evt);
                return;
            }

            var mousePosition = contentViewContainer.WorldToLocal(evt.mousePosition);
            foreach (var type in GetNodeTypes())
            {
                evt.menu.AppendAction($"Create/{GetCategory(type)}/{type.Name}", _ => CreateNode(type, mousePosition));
            }

            base.BuildContextualMenu(evt);
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            if (_tree == null)
                return graphViewChange;

            if (graphViewChange.elementsToRemove != null)
            {
                foreach (var element in graphViewChange.elementsToRemove)
                {
                    if (element is Edge edge)
                        RemoveEdge(edge);

                    if (element is BehaviorTreeNodeView nodeView)
                        DeleteNode(nodeView);
                }
            }

            if (graphViewChange.edgesToCreate != null)
            {
                foreach (var edge in graphViewChange.edgesToCreate)
                    AddEdge(edge);
            }

            SortChildrenByPosition();
            SaveAsset();
            return graphViewChange;
        }

        private void CreateNode(Type type, Vector2 position)
        {
            var node = ScriptableObject.CreateInstance(type) as BTNode;
            if (node == null)
                return;

            node.name = type.Name;
            node.DisplayName = type.Name;
            node.EditorPosition = position;
            node.EnsureGuid();

            Undo.RegisterCreatedObjectUndo(node, "Create BT Node");
            AssetDatabase.AddObjectToAsset(node, _tree);
            _tree.Nodes.Add(node);
            if (_tree.RootNode == null)
                _tree.RootNode = node;

            AddNodeView(node);
            SaveAsset();
        }

        private void AddNodeView(BTNode node)
        {
            var nodeView = new BehaviorTreeNodeView(node);
            nodeView.OnSetRoot += SetRoot;
            nodeView.RegisterCallback<MouseDownEvent>(_ => _window.SelectNode(nodeView.Node));
            _nodeViews[node] = nodeView;
            AddElement(nodeView);
        }

        private void SetRoot(BehaviorTreeNodeView nodeView)
        {
            if (_tree == null || nodeView == null)
                return;

            Undo.RecordObject(_tree, "Set BT Root");
            _tree.RootNode = nodeView.Node;
            SaveAsset();
            _window.RefreshInspector();
        }

        private void AddEdge(Edge edge)
        {
            if (edge.output?.node is not BehaviorTreeNodeView parentView ||
                edge.input?.node is not BehaviorTreeNodeView childView)
                return;

            RemoveConflictingEdges(edge, parentView, childView);

            Undo.RecordObject(parentView.Node, "Connect BT Nodes");
            foreach (var node in _tree.Nodes)
            {
                if (node == null || node == parentView.Node)
                    continue;

                Undo.RecordObject(node, "Connect BT Nodes");
                node.Children.Remove(childView.Node);
            }

            if (parentView.Node is BTDecoratorNode)
                parentView.Node.Children.Clear();

            if (!parentView.Node.Children.Contains(childView.Node))
                parentView.Node.Children.Add(childView.Node);
        }

        private void RemoveConflictingEdges(Edge newEdge, BehaviorTreeNodeView parentView, BehaviorTreeNodeView childView)
        {
            var oldEdges = edges
                .Where(edge => edge != newEdge &&
                    (edge.input == childView.Input ||
                     (parentView.Node is BTDecoratorNode && edge.output == parentView.Output)))
                .ToList();

            foreach (var oldEdge in oldEdges)
            {
                RemoveEdge(oldEdge);
                RemoveElement(oldEdge);
            }
        }

        private void RemoveEdge(Edge edge)
        {
            if (edge.output?.node is not BehaviorTreeNodeView parentView ||
                edge.input?.node is not BehaviorTreeNodeView childView)
                return;

            Undo.RecordObject(parentView.Node, "Disconnect BT Nodes");
            parentView.Node.Children.Remove(childView.Node);
        }

        private void DeleteNode(BehaviorTreeNodeView nodeView)
        {
            Undo.RecordObject(_tree, "Delete BT Node");
            foreach (var node in _tree.Nodes)
            {
                if (node != null)
                {
                    Undo.RecordObject(node, "Delete BT Node Reference");
                    node.Children.Remove(nodeView.Node);
                }
            }

            if (_tree.RootNode == nodeView.Node)
                _tree.RootNode = null;

            _tree.Nodes.Remove(nodeView.Node);
            _nodeViews.Remove(nodeView.Node);
            Undo.DestroyObjectImmediate(nodeView.Node);
        }

        private void SortChildrenByPosition()
        {
            foreach (var node in _tree.Nodes)
            {
                if (node == null || node.Children.Count <= 1)
                    continue;

                node.Children.Sort((a, b) => a.EditorPosition.x.CompareTo(b.EditorPosition.x));
            }
        }

        private void SaveAsset()
        {
            if (_tree == null)
                return;

            EditorUtility.SetDirty(_tree);
            foreach (var node in _tree.Nodes)
            {
                if (node != null)
                    EditorUtility.SetDirty(node);
            }

            AssetDatabase.SaveAssets();
        }

        private static IEnumerable<Type> GetNodeTypes()
        {
            return TypeCache.GetTypesDerivedFrom<BTNode>()
                .Where(type => !type.IsAbstract && !type.IsGenericType)
                .OrderBy(GetCategory)
                .ThenBy(type => type.Name);
        }

        private static string GetCategory(Type type)
        {
            if (typeof(BTCompositeNode).IsAssignableFrom(type))
                return "Composite";
            if (typeof(BTDecoratorNode).IsAssignableFrom(type))
                return "Decorator";
            if (typeof(BTConditionNode).IsAssignableFrom(type))
                return "Condition";
            return "Action";
        }
    }
}
#endif
