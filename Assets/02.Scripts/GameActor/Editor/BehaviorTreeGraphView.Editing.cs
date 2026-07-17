#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public partial class BehaviorTreeGraphView
    {
        public void RefreshNodeView(BTNode node)
        {
            if (node == null || !_nodeViews.TryGetValue(node, out var nodeView))
                return;

            nodeView.RefreshView();
        }

        public void RefreshGroupView(BehaviorTreeEditorGroup group)
        {
            if (group == null || !_groupViews.TryGetValue(group, out var groupView))
                return;

            groupView.RefreshView();
            groupView.SendToBack();
        }

        private void RefreshPositionsAfterUndoRedo()
        {
            if (_tree == null)
                return;

            foreach (var pair in _nodeViews)
            {
                var rect = pair.Value.GetPosition();
                rect.position = pair.Key.EditorPosition;
                pair.Value.SetPosition(rect);
            }

            foreach (var groupView in _groupViews.Values)
            {
                groupView.RefreshView();
                groupView.SendToBack();
            }

            MarkEdgesDirty();
            _window.RefreshInspector();
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (_tree == null)
            {
                base.BuildContextualMenu(evt);
                return;
            }

            var mousePosition = contentViewContainer.WorldToLocal(evt.mousePosition);
            evt.menu.AppendAction(
                "Edit/Copy Selection",
                _ => CopySelectionToClipboard(),
                _ => selection.OfType<BehaviorTreeNodeView>().Any() || selection.OfType<BehaviorTreeGroupView>().Any()
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendAction(
                "Edit/Paste",
                _ => PasteFromClipboard(),
                _ => CanPasteClipboardData(EditorGUIUtility.systemCopyBuffer)
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendSeparator("Edit/");
            evt.menu.AppendAction("Create/Group Box", _ => CreateGroup(mousePosition));
            evt.menu.AppendAction(
                "Create/Group Box From Selection",
                _ => CreateGroupFromSelection(),
                _ => selection.OfType<BehaviorTreeNodeView>().Any()
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendSeparator("Create/");

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

            var hasRemovals = graphViewChange.elementsToRemove != null && graphViewChange.elementsToRemove.Count > 0;
            var hasCreations = graphViewChange.edgesToCreate != null && graphViewChange.edgesToCreate.Count > 0;
            var hasMoves = graphViewChange.movedElements != null && graphViewChange.movedElements.Count > 0;

            var undoGroup = -1;
            var groupOpened = false;
            if (!_inGraphChangeUndoGroup && (hasRemovals || hasCreations || hasMoves))
            {
                _inGraphChangeUndoGroup = true;
                undoGroup = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName(BuildUndoLabel(hasRemovals, hasCreations, hasMoves));
                groupOpened = true;
            }

            try
            {
                if (graphViewChange.elementsToRemove != null)
                {
                    foreach (var element in graphViewChange.elementsToRemove)
                    {
                        if (element is Edge edge)
                            RemoveEdge(edge);

                        if (element is BehaviorTreeNodeView nodeView)
                            DeleteNode(nodeView);

                        if (element is BehaviorTreeGroupView groupView)
                            DeleteGroup(groupView);
                    }
                }

                if (graphViewChange.edgesToCreate != null)
                {
                    foreach (var edge in graphViewChange.edgesToCreate)
                        AddEdge(edge);
                }

                if (graphViewChange.movedElements != null)
                {
                    foreach (var element in graphViewChange.movedElements)
                    {
                        if (element is BehaviorTreeGroupView groupView)
                            groupView.PersistPosition();
                    }
                }

                SortChildrenByPosition();
                SaveAsset();
            }
            finally
            {
                if (groupOpened)
                {
                    Undo.CollapseUndoOperations(undoGroup);
                    _inGraphChangeUndoGroup = false;
                }
            }

            return graphViewChange;
        }

        private static string BuildUndoLabel(bool hasRemovals, bool hasCreations, bool hasMoves)
        {
            if (hasRemovals && !hasCreations && !hasMoves)
                return "Delete BT Elements";
            if (hasCreations && !hasRemovals && !hasMoves)
                return "Connect BT Nodes";
            if (hasMoves && !hasRemovals && !hasCreations)
                return "Move BT Elements";
            return "Modify BT Graph";
        }

        private void CreateGroup(Vector2 position)
        {
            if (_tree == null)
                return;

            var selectedNodes = selection.OfType<BehaviorTreeNodeView>().ToList();
            if (selectedNodes.Count > 0)
            {
                CreateGroupFromNodes(selectedNodes);
                return;
            }

            Undo.RecordObject(_tree, "Create BT Group Box");
            var group = new BehaviorTreeEditorGroup
            {
                Title = "New Group",
                Rect = new Rect(position.x, position.y, 520f, 320f),
                Color = new Color(0.05f, 0.26f, 0.07f, 0.42f)
            };

            _tree.EditorGroups.Add(group);
            AddGroupView(group);
            SaveAsset();
        }

        private void CreateGroupFromSelection()
        {
            if (_tree == null)
                return;

            var selectedNodes = selection.OfType<BehaviorTreeNodeView>().ToList();
            if (selectedNodes.Count == 0)
                return;

            CreateGroupFromNodes(selectedNodes);
        }

        private void CreateGroupFromNodes(IReadOnlyList<BehaviorTreeNodeView> selectedNodes)
        {
            var bounds = selectedNodes[0].GetPosition();
            for (var i = 1; i < selectedNodes.Count; i++)
            {
                var rect = selectedNodes[i].GetPosition();
                bounds.xMin = Mathf.Min(bounds.xMin, rect.xMin);
                bounds.yMin = Mathf.Min(bounds.yMin, rect.yMin);
                bounds.xMax = Mathf.Max(bounds.xMax, rect.xMax);
                bounds.yMax = Mathf.Max(bounds.yMax, rect.yMax);
            }

            Undo.RecordObject(_tree, "Create BT Group Box From Selection");
            var group = new BehaviorTreeEditorGroup
            {
                Title = "Selected Group",
                Rect = new Rect(bounds.xMin - 48f, bounds.yMin - 72f, bounds.width + 96f, bounds.height + 120f),
                Color = new Color(0.05f, 0.26f, 0.07f, 0.42f)
            };

            _tree.EditorGroups.Add(group);
            AddGroupView(group);
            SaveAsset();
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

        private void CreateNodeAtSearchPosition(Type type, Vector2 screenMousePosition)
        {
            if (_tree == null || type == null)
                return;

            var windowMousePosition = screenMousePosition - _window.position.position;
            var graphPosition = _window.rootVisualElement.ChangeCoordinatesTo(contentViewContainer, windowMousePosition);
            CreateNode(type, graphPosition);
        }

        private void AddNodeView(BTNode node)
        {
            var nodeView = new BehaviorTreeNodeView(node, _tree != null ? _tree.Nodes.IndexOf(node) : -1);
            nodeView.OnSetRoot += SetRoot;
            nodeView.RegisterCallback<MouseDownEvent>(_ => _window.SelectNode(nodeView.Node));
            _nodeViews[node] = nodeView;
            if (!string.IsNullOrWhiteSpace(node.Guid))
                _nodeViewsByGuid[node.Guid] = nodeView;
            OverridePortListener(nodeView.Input);
            OverridePortListener(nodeView.Output);
            AddElement(nodeView);
        }

        private void OverridePortListener(Port port)
        {
            if (port == null)
                return;

            var existing = port.edgeConnector;
            if (existing != null)
                port.RemoveManipulator(existing);

            var connector = new EdgeConnector<Edge>(_portConnectorListener);
            var field = typeof(Port).GetField("m_EdgeConnector", BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(port, connector);
            port.AddManipulator(connector);
        }

        internal void HandlePortDragDroppedOutside(Edge edge, Vector2 mousePosition)
        {
            var port = edge.output ?? edge.input;
            if (port == null || _tree == null)
                return;

            _pendingPortConnect = port;
            _nodeSearchWindow.SetPortFilter(port.direction);
            var screen = GUIUtility.GUIToScreenPoint(mousePosition);
            SearchWindow.Open(new SearchWindowContext(screen), _nodeSearchWindow);
        }

        internal void HandlePortDragNormalDrop(Edge edge)
        {
            // 표준 OnDrop 흐름 복제: graphViewChanged → AddElement
            var change = new GraphViewChange { edgesToCreate = new List<Edge> { edge } };
            if (graphViewChanged != null)
                change = graphViewChanged(change);

            if (change.edgesToCreate != null)
            {
                foreach (var created in change.edgesToCreate)
                    AddElement(created);
            }
        }

        private void CreateNodeFromPortDrag(Type type, Vector2 screenMousePosition)
        {
            if (_tree == null || type == null || _pendingPortConnect == null)
            {
                _pendingPortConnect = null;
                return;
            }

            var originPort = _pendingPortConnect;
            _pendingPortConnect = null;

            var windowMousePosition = screenMousePosition - _window.position.position;
            var graphPosition = _window.rootVisualElement.ChangeCoordinatesTo(contentViewContainer, windowMousePosition);

            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Create BT Node From Port");

            var newNode = CreateNodeReturn(type, graphPosition);
            if (newNode == null)
            {
                Undo.CollapseUndoOperations(undoGroup);
                return;
            }

            if (originPort.node is not BehaviorTreeNodeView originView)
            {
                Undo.CollapseUndoOperations(undoGroup);
                return;
            }

            if (!_nodeViews.TryGetValue(newNode, out var newView))
            {
                Undo.CollapseUndoOperations(undoGroup);
                return;
            }

            BehaviorTreeNodeView parentView;
            BehaviorTreeNodeView childView;
            if (originPort.direction == Direction.Output)
            {
                parentView = originView;
                childView = newView;
            }
            else
            {
                parentView = newView;
                childView = originView;
            }

            if (parentView.Output == null || childView.Input == null)
            {
                Undo.CollapseUndoOperations(undoGroup);
                return;
            }

            var edge = parentView.Output.ConnectTo(childView.Input);
            AddEdge(edge);
            AddElement(edge);
            StyleEdge(edge, false, false);
            SaveAsset();
            Undo.CollapseUndoOperations(undoGroup);
        }

        private BTNode CreateNodeReturn(Type type, Vector2 position)
        {
            var node = ScriptableObject.CreateInstance(type) as BTNode;
            if (node == null)
                return null;

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
            return node;
        }

        internal sealed class PortDragConnectorListener : IEdgeConnectorListener
        {
            private readonly BehaviorTreeGraphView _graphView;

            public PortDragConnectorListener(BehaviorTreeGraphView graphView)
            {
                _graphView = graphView;
            }

            public void OnDrop(GraphView graphView, Edge edge)
            {
                _graphView.HandlePortDragNormalDrop(edge);
            }

            public void OnDropOutsidePort(Edge edge, Vector2 position)
            {
                _graphView.HandlePortDragDroppedOutside(edge, position);
            }
        }

        private void AddGroupView(BehaviorTreeEditorGroup group)
        {
            var groupView = new BehaviorTreeGroupView(group, MoveNodesInsideGroup, SaveAsset);
            groupView.RegisterCallback<MouseDownEvent>(_ => _window.SelectGroup(groupView.Group));
            _groupViews[group] = groupView;
            AddElement(groupView);
            groupView.SendToBack();
        }

        private void MoveNodesInsideGroup(BehaviorTreeGroupView groupView, Rect previousGroupRect, Vector2 delta)
        {
            if (groupView == null || delta.sqrMagnitude <= 0.0001f)
                return;

            foreach (var nodeView in _nodeViews.Values)
            {
                if (selection.Contains(nodeView))
                    continue;

                var nodeRect = nodeView.GetPosition();
                if (!previousGroupRect.Contains(nodeRect.center))
                    continue;

                Undo.RecordObject(nodeView.Node, "Move BT Group Box");
                nodeRect.position += delta;
                nodeView.SetPosition(nodeRect);
                EditorUtility.SetDirty(nodeView.Node);
            }
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

            StyleEdge(edge, false, false);
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
            foreach (var edge in edges)
                edge.MarkDirtyRepaint();
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
            if (!string.IsNullOrWhiteSpace(nodeView.Node.Guid))
                _nodeViewsByGuid.Remove(nodeView.Node.Guid);
            Undo.DestroyObjectImmediate(nodeView.Node);
        }

        private void DeleteGroup(BehaviorTreeGroupView groupView)
        {
            if (_tree == null || groupView == null)
                return;

            Undo.RecordObject(_tree, "Delete BT Group Box");
            _tree.EditorGroups.Remove(groupView.Group);
            _groupViews.Remove(groupView.Group);
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
    }
}
#endif
