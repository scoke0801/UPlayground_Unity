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
            style.backgroundColor = BehaviorTreeEditorStyles.Background;

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
                    StyleEdge(edge, false);
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

            foreach (var edge in edges.ToList())
            {
                if (edge.output?.node is not BehaviorTreeNodeView parentView ||
                    edge.input?.node is not BehaviorTreeNodeView childView)
                {
                    continue;
                }

                runtimeByGuid.TryGetValue(parentView.Node.Guid, out var runtimeParent);
                runtimeByGuid.TryGetValue(childView.Node.Guid, out var runtimeChild);
                var active = runtimeParent is { IsStarted: true } || runtimeChild is { IsStarted: true };
                StyleEdge(edge, active);
            }

            MarkEdgesDirty();
        }

        public void FrameAllNodes()
        {
            if (_nodeViews.Count == 0)
                return;

            ClearSelection();
            foreach (var nodeView in _nodeViews.Values)
                AddToSelection(nodeView);

            FrameSelection();
            ClearSelection();
        }

        public void FocusNode(BTNode node)
        {
            if (node == null || !_nodeViews.TryGetValue(node, out var nodeView))
                return;

            ClearSelection();
            AddToSelection(nodeView);
            FrameSelection();
            _window.SelectNode(node);
        }

        public void RefreshNodeView(BTNode node)
        {
            if (node == null || !_nodeViews.TryGetValue(node, out var nodeView))
                return;

            nodeView.RefreshView();
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
            var nodeView = new BehaviorTreeNodeView(node, _tree != null ? _tree.Nodes.IndexOf(node) : -1);
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

            StyleEdge(edge, false);
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

        private void MarkEdgesDirty()
        {
            foreach (var edge in edges.ToList())
                edge.MarkDirtyRepaint();
        }

        private static void StyleEdge(Edge edge, bool active)
        {
            if (edge?.edgeControl == null)
                return;

            var color = active
                ? BehaviorTreeEditorStyles.Running
                : BehaviorTreeEditorStyles.WithAlpha(BehaviorTreeEditorStyles.Composite, 0.72f);

            edge.edgeControl.inputColor = color;
            edge.edgeControl.outputColor = color;
            edge.edgeControl.edgeWidth = active ? 3 : 2;
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

        public Rect GetTreeBounds()
        {
            var bounds = new Rect();
            var first = true;
            foreach (var nodeView in _nodeViews.Values)
            {
                var rect = nodeView.GetPosition();
                if (first)
                {
                    bounds = rect;
                    first = false;
                }
                else
                {
                    bounds.xMin = Mathf.Min(bounds.xMin, rect.xMin);
                    bounds.yMin = Mathf.Min(bounds.yMin, rect.yMin);
                    bounds.xMax = Mathf.Max(bounds.xMax, rect.xMax);
                    bounds.yMax = Mathf.Max(bounds.yMax, rect.yMax);
                }
            }

            return bounds;
        }

        public IEnumerable<(Rect Rect, Color Color, bool Running)> GetMiniMapNodes()
        {
            foreach (var nodeView in _nodeViews.Values)
            {
                yield return (
                    nodeView.GetPosition(),
                    GetNodeColor(nodeView.Node),
                    nodeView.Node.IsStarted && nodeView.Node.LastStatus == BTStatus.Running);
            }
        }

        public IEnumerable<(Vector2 From, Vector2 To, bool Running)> GetMiniMapEdges()
        {
            foreach (var edge in edges.ToList())
            {
                if (edge.output?.node is not BehaviorTreeNodeView parentView ||
                    edge.input?.node is not BehaviorTreeNodeView childView)
                {
                    continue;
                }

                var parent = parentView.GetPosition();
                var child = childView.GetPosition();
                yield return (
                    new Vector2(parent.center.x, parent.yMax),
                    new Vector2(child.center.x, child.yMin),
                    parentView.Node.IsStarted || childView.Node.IsStarted);
            }
        }

        private static Color GetNodeColor(BTNode node)
        {
            if (node is BTCompositeNode)
                return BehaviorTreeEditorStyles.Composite;
            if (node is BTDecoratorNode)
                return BehaviorTreeEditorStyles.Decorator;
            if (node is BTConditionNode)
                return BehaviorTreeEditorStyles.Condition;
            return BehaviorTreeEditorStyles.Action;
        }
    }
}
#endif
