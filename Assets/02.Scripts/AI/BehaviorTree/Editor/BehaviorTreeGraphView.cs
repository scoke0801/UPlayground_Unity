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
    public class BehaviorTreeGraphView : GraphView
    {
        private const string ClipboardPrefix = "UPlayGround.BTGraphClipboard:";
        private const double SaveDebounceSeconds = 0.35d;
        private readonly BehaviorTreeEditorWindow _window;
        private readonly Dictionary<BTNode, BehaviorTreeNodeView> _nodeViews = new();
        private readonly Dictionary<string, BehaviorTreeNodeView> _nodeViewsByGuid = new();
        private readonly Dictionary<BehaviorTreeEditorGroup, BehaviorTreeGroupView> _groupViews = new();
        private readonly Dictionary<string, BTStatus> _currentTickStatuses = new();
        private readonly Dictionary<string, BTNode> _runtimeNodesByGuid = new();
        private readonly BehaviorTreeNodeSearchWindow _nodeSearchWindow;
        private readonly PortDragConnectorListener _portConnectorListener;
        private BehaviorTreeAsset _tree;
        private bool _saveQueued;
        private double _nextSaveTime;
        private Port _pendingPortConnect;
        private bool _inGraphChangeUndoGroup;

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

            _nodeSearchWindow = ScriptableObject.CreateInstance<BehaviorTreeNodeSearchWindow>();
            _nodeSearchWindow.Initialize(window, this, CreateNodeAtSearchPosition, CreateNodeFromPortDrag);
            _portConnectorListener = new PortDragConnectorListener(this);
            nodeCreationRequest = context =>
            {
                _nodeSearchWindow.SetPortFilter(null);
                SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), _nodeSearchWindow);
            };
            serializeGraphElements = SerializeSelectionToClipboardData;
            canPasteSerializedData = CanPasteClipboardData;
            unserializeAndPaste = UnserializeAndPaste;
            Undo.undoRedoPerformed += RefreshPositionsAfterUndoRedo;
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                FlushPendingSave();
                Undo.undoRedoPerformed -= RefreshPositionsAfterUndoRedo;
            });

            graphViewChanged += OnGraphViewChanged;
        }

        internal PortDragConnectorListener PortConnectorListener => _portConnectorListener;

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports
                .Where(port => port.direction != startPort.direction && port.node != startPort.node)
                .ToList();
        }

        public void PopulateView(BehaviorTreeAsset tree)
        {
            _tree = tree;
            _pendingPortConnect = null;
            graphViewChanged -= OnGraphViewChanged;
            DeleteElements(graphElements.ToList());
            _nodeViews.Clear();
            _nodeViewsByGuid.Clear();
            _groupViews.Clear();
            _currentTickStatuses.Clear();

            if (_tree == null)
            {
                graphViewChanged += OnGraphViewChanged;
                return;
            }

            foreach (var group in _tree.EditorGroups.Where(group => group != null))
                AddGroupView(group);

            foreach (var node in _tree.Nodes.Where(node => node != null && node is not BTServiceNode))
                AddNodeView(node);

            foreach (var node in _tree.Nodes.Where(node => node != null && node is not BTServiceNode))
            {
                if (!_nodeViews.TryGetValue(node, out var parentView) || parentView.Output == null)
                    continue;

                foreach (var child in node.Children)
                {
                    if (child == null || !_nodeViews.TryGetValue(child, out var childView))
                        continue;

                    var edge = parentView.Output.ConnectTo(childView.Input);
                    StyleEdge(edge, false, false);
                    AddElement(edge);
                }
            }

            graphViewChanged += OnGraphViewChanged;
        }

        public void UpdateDebugState(BehaviorTreeAsset runtimeTree, BehaviorTreeDebugTrace trace = null)
        {
            _runtimeNodesByGuid.Clear();
            if (runtimeTree != null)
            {
                foreach (var node in runtimeTree.Nodes)
                {
                    if (node != null)
                        _runtimeNodesByGuid[node.Guid] = node;
                }
            }

            _currentTickStatuses.Clear();
            if (trace != null)
            {
                foreach (var record in trace.Records)
                {
                    if (record.Tick != trace.CurrentTick || string.IsNullOrWhiteSpace(record.NodeGuid))
                        continue;

                    _currentTickStatuses[record.NodeGuid] = record.Status;
                }
            }

            foreach (var pair in _nodeViews)
            {
                _runtimeNodesByGuid.TryGetValue(pair.Key.Guid, out var runtimeNode);
                var wasTicked = _currentTickStatuses.TryGetValue(pair.Key.Guid, out var tickStatus);
                pair.Value.UpdateStateColor(runtimeNode, wasTicked, tickStatus);
            }

            foreach (var edge in edges)
            {
                if (edge.output?.node is not BehaviorTreeNodeView parentView ||
                    edge.input?.node is not BehaviorTreeNodeView childView)
                {
                    continue;
                }

                _runtimeNodesByGuid.TryGetValue(parentView.Node.Guid, out var runtimeParent);
                _runtimeNodesByGuid.TryGetValue(childView.Node.Guid, out var runtimeChild);
                var running = runtimeParent is { IsStarted: true } || runtimeChild is { IsStarted: true };
                var ticked = IsNodeHighlighted(parentView.Node.Guid) && IsNodeHighlighted(childView.Node.Guid);
                StyleEdge(edge, running, ticked);
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

        public bool FocusNodeByGuid(string guid)
        {
            if (string.IsNullOrWhiteSpace(guid))
                return false;

            if (!_nodeViewsByGuid.TryGetValue(guid, out var nodeView) || nodeView?.Node == null)
                return false;

            FocusNode(nodeView.Node);
            return true;
        }

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

        private string SerializeSelectionToClipboardData(IEnumerable<GraphElement> elements)
        {
            var clipboard = new BehaviorTreeClipboardData();
            var seenNodeGuids = new HashSet<string>();
            var seenGroupGuids = new HashSet<string>();

            foreach (var element in elements)
            {
                if (element is BehaviorTreeNodeView nodeView && nodeView.Node != null)
                {
                    nodeView.Node.EnsureGuid();
                    if (seenNodeGuids.Add(nodeView.Node.Guid))
                        clipboard.nodeGuids.Add(nodeView.Node.Guid);
                }
                else if (element is BehaviorTreeGroupView groupView && groupView.Group != null)
                {
                    if (seenGroupGuids.Add(groupView.Group.Guid))
                    {
                        clipboard.groups.Add(new BehaviorTreeClipboardGroup
                        {
                            title = groupView.Group.Title,
                            rect = groupView.Group.Rect,
                            color = groupView.Group.Color
                        });
                    }
                }
            }

            return clipboard.nodeGuids.Count == 0 && clipboard.groups.Count == 0
                ? string.Empty
                : ClipboardPrefix + JsonUtility.ToJson(clipboard);
        }

        private bool CanPasteClipboardData(string data)
        {
            return _tree != null && !string.IsNullOrWhiteSpace(data) && data.StartsWith(ClipboardPrefix, StringComparison.Ordinal);
        }

        private void UnserializeAndPaste(string operationName, string data)
        {
            if (!CanPasteClipboardData(data))
                return;

            var json = data.Substring(ClipboardPrefix.Length);
            var clipboard = JsonUtility.FromJson<BehaviorTreeClipboardData>(json);
            if (clipboard == null)
                return;

            PasteClipboardData(clipboard);
        }

        private void CopySelectionToClipboard()
        {
            var serialized = SerializeSelectionToClipboardData(selection.OfType<GraphElement>());
            if (!string.IsNullOrWhiteSpace(serialized))
                EditorGUIUtility.systemCopyBuffer = serialized;
        }

        private void PasteFromClipboard()
        {
            UnserializeAndPaste("Paste BT Selection", EditorGUIUtility.systemCopyBuffer);
        }

        private void PasteClipboardData(BehaviorTreeClipboardData clipboard)
        {
            if (_tree == null)
                return;

            var sourceNodes = ResolveClipboardNodes(clipboard);
            if (sourceNodes.Count == 0 && clipboard.groups.Count == 0)
                return;

            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Paste BT Selection");
            Undo.RecordObject(_tree, "Paste BT Selection");
            var nodeMap = new Dictionary<BTNode, BTNode>();
            var sourceBounds = CalculateClipboardBounds(sourceNodes, clipboard.groups);
            var pasteOffset = GetVisibleContentBounds().center - sourceBounds.center + new Vector2(32f, 32f);

            foreach (var source in sourceNodes)
            {
                var clone = UnityEngine.Object.Instantiate(source);
                clone.name = source.name;
                clone.Guid = Guid.NewGuid().ToString("N");
                clone.DisplayName = source.DisplayName;
                clone.EditorPosition = source.EditorPosition + pasteOffset;
                clone.Children.Clear();
                CloneCompositeServices(source, clone, pasteOffset);

                Undo.RegisterCreatedObjectUndo(clone, "Paste BT Node");
                AssetDatabase.AddObjectToAsset(clone, _tree);
                _tree.Nodes.Add(clone);
                nodeMap[source] = clone;
            }

            foreach (var pair in nodeMap)
            {
                var source = pair.Key;
                var clone = pair.Value;
                foreach (var child in source.Children)
                {
                    if (child != null && nodeMap.TryGetValue(child, out var childClone))
                        clone.Children.Add(childClone);
                }

                EditorUtility.SetDirty(clone);
            }

            foreach (var groupData in clipboard.groups)
            {
                var group = new BehaviorTreeEditorGroup
                {
                    Guid = Guid.NewGuid().ToString("N"),
                    Title = groupData.title,
                    Rect = new Rect(groupData.rect.position + pasteOffset, groupData.rect.size),
                    Color = groupData.color
                };
                _tree.EditorGroups.Add(group);
            }

            SaveAsset();
            Undo.CollapseUndoOperations(undoGroup);
            PopulateView(_tree);

            ClearSelection();
            foreach (var clone in nodeMap.Values)
            {
                if (_nodeViews.TryGetValue(clone, out var nodeView))
                    AddToSelection(nodeView);
            }
            foreach (var group in _tree.EditorGroups.Where(group => clipboard.groups.Any(copied => group.Title == copied.title && group.Rect.position == copied.rect.position + pasteOffset)))
            {
                if (_groupViews.TryGetValue(group, out var groupView))
                    AddToSelection(groupView);
            }
        }

        private List<BTNode> ResolveClipboardNodes(BehaviorTreeClipboardData clipboard)
        {
            var result = new List<BTNode>();
            if (clipboard?.nodeGuids == null)
                return result;

            foreach (var guid in clipboard.nodeGuids)
            {
                var node = _tree.Nodes.FirstOrDefault(candidate => candidate != null && candidate.Guid == guid);
                if (node != null && node is not BTServiceNode)
                    result.Add(node);
            }

            return result;
        }

        private static Rect CalculateClipboardBounds(List<BTNode> nodes, List<BehaviorTreeClipboardGroup> groups)
        {
            var first = true;
            var bounds = new Rect();
            foreach (var rect in nodes.Select(node => new Rect(node.EditorPosition, new Vector2(160f, 120f)))
                         .Concat((groups ?? new List<BehaviorTreeClipboardGroup>()).Select(group => group.rect)))
            {
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

            return first ? new Rect(Vector2.zero, Vector2.one) : bounds;
        }

        private void CloneCompositeServices(BTNode source, BTNode clone, Vector2 pasteOffset)
        {
            if (source is not BTCompositeNode sourceComposite || clone is not BTCompositeNode cloneComposite)
                return;

            cloneComposite.Services.Clear();
            foreach (var service in sourceComposite.Services)
            {
                if (service == null)
                    continue;

                var serviceClone = UnityEngine.Object.Instantiate(service);
                serviceClone.name = service.name;
                serviceClone.Guid = Guid.NewGuid().ToString("N");
                serviceClone.DisplayName = service.DisplayName;
                serviceClone.EditorPosition = service.EditorPosition + pasteOffset;
                serviceClone.Children.Clear();

                Undo.RegisterCreatedObjectUndo(serviceClone, "Paste BT Service");
                AssetDatabase.AddObjectToAsset(serviceClone, _tree);
                _tree.Nodes.Add(serviceClone);
                cloneComposite.Services.Add(serviceClone);
                EditorUtility.SetDirty(serviceClone);
            }
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

        private static void StyleEdge(Edge edge, bool running, bool ticked)
        {
            if (edge?.edgeControl == null)
                return;

            var color = running
                ? BehaviorTreeEditorStyles.Running
                : ticked
                    ? BehaviorTreeEditorStyles.Paused
                    : BehaviorTreeEditorStyles.WithAlpha(BehaviorTreeEditorStyles.Composite, 0.62f);

            edge.edgeControl.inputColor = color;
            edge.edgeControl.outputColor = color;
            edge.edgeControl.edgeWidth = running ? 5 : ticked ? 4 : 2;
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

        private void SaveAsset()
        {
            SaveAsset(false);
        }

        private void SaveAsset(bool immediate)
        {
            if (_tree == null)
                return;

            EditorUtility.SetDirty(_tree);
            foreach (var node in _tree.Nodes)
            {
                if (node != null)
                    EditorUtility.SetDirty(node);
            }

            if (immediate)
            {
                _saveQueued = false;
                EditorApplication.update -= FlushDebouncedSave;
                AssetDatabase.SaveAssets();
                return;
            }

            QueueSaveAssets();
        }

        public void FlushPendingSave()
        {
            if (!_saveQueued)
                return;

            _saveQueued = false;
            EditorApplication.update -= FlushDebouncedSave;
            AssetDatabase.SaveAssets();
        }

        private void QueueSaveAssets()
        {
            _nextSaveTime = EditorApplication.timeSinceStartup + SaveDebounceSeconds;
            if (_saveQueued)
                return;

            _saveQueued = true;
            EditorApplication.update += FlushDebouncedSave;
        }

        private void FlushDebouncedSave()
        {
            if (!_saveQueued)
            {
                EditorApplication.update -= FlushDebouncedSave;
                return;
            }

            if (EditorApplication.timeSinceStartup < _nextSaveTime)
                return;

            EditorApplication.update -= FlushDebouncedSave;
            FlushPendingSave();
        }

        private static IEnumerable<Type> GetNodeTypes()
        {
            return TypeCache.GetTypesDerivedFrom<BTNode>()
                .Where(type => !type.IsAbstract && !type.IsGenericType)
                // Service는 그래프 노드로 직접 생성하지 않고 Composite Inspector를 통해 부착한다.
                .Where(type => !typeof(BTServiceNode).IsAssignableFrom(type))
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
            if (typeof(BTServiceNode).IsAssignableFrom(type))
                return "Service";
            return "Action";
        }

        public Rect GetTreeBounds()
        {
            var bounds = new Rect();
            var first = true;
            foreach (var rect in _groupViews.Values.Select(group => group.GetPosition())
                         .Concat(_nodeViews.Values.Select(node => node.GetPosition())))
            {
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
                    IsNodeHighlighted(nodeView.Node.Guid));
            }
        }

        public IEnumerable<(Rect Rect, Color Color)> GetMiniMapGroups()
        {
            foreach (var group in _tree?.EditorGroups ?? Enumerable.Empty<BehaviorTreeEditorGroup>())
            {
                yield return (group.Rect, group.Color);
            }
        }

        public IEnumerable<(Vector2 From, Vector2 To, bool Running)> GetMiniMapEdges()
        {
            foreach (var edge in edges)
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
                    IsNodeHighlighted(parentView.Node.Guid) && IsNodeHighlighted(childView.Node.Guid));
            }
        }

        private bool IsNodeHighlighted(string guid)
        {
            return !string.IsNullOrWhiteSpace(guid) && _currentTickStatuses.ContainsKey(guid);
        }

        public Rect GetVisibleContentBounds()
        {
            var scale = Mathf.Max(0.001f, viewTransform.scale.x);
            var position = viewTransform.position;
            var viewSize = layout.size;

            return new Rect(
                -position.x / scale,
                -position.y / scale,
                viewSize.x / scale,
                viewSize.y / scale);
        }

        public void CenterOnContentPosition(Vector2 contentPosition)
        {
            var scale = viewTransform.scale;
            var position = new Vector3(
                layout.width * 0.5f - contentPosition.x * scale.x,
                layout.height * 0.5f - contentPosition.y * scale.y,
                0f);

            UpdateViewTransform(position, scale);
        }

        public void ZoomAroundContentPosition(Vector2 contentPosition, float wheelDelta)
        {
            var oldScale = Mathf.Max(0.001f, viewTransform.scale.x);
            var zoomFactor = wheelDelta > 0f ? 1.12f : 0.88f;
            var newScale = Mathf.Clamp(oldScale * zoomFactor, 0.25f, 2.0f);
            var scale = new Vector3(newScale, newScale, 1f);
            var position = new Vector3(
                layout.width * 0.5f - contentPosition.x * scale.x,
                layout.height * 0.5f - contentPosition.y * scale.y,
                0f);

            UpdateViewTransform(position, scale);
        }

        private static Color GetNodeColor(BTNode node)
        {
            if (node is BTCompositeNode)
                return BehaviorTreeEditorStyles.Composite;
            if (node is BTDecoratorNode)
                return BehaviorTreeEditorStyles.Decorator;
            if (node is BTConditionNode)
                return BehaviorTreeEditorStyles.Condition;
            if (node is BTServiceNode)
                return BehaviorTreeEditorStyles.Decorator;
            return BehaviorTreeEditorStyles.Action;
        }

        [Serializable]
        private sealed class BehaviorTreeClipboardData
        {
            public List<string> nodeGuids = new();
            public List<BehaviorTreeClipboardGroup> groups = new();
        }

        [Serializable]
        private sealed class BehaviorTreeClipboardGroup
        {
            public string title;
            public Rect rect;
            public Color color;
        }
    }

    internal sealed class BehaviorTreeGroupView : GraphElement
    {
        private const float MinWidth = 220f;
        private const float MinHeight = 140f;
        private const float EdgeResizeHandleSize = 10f;
        private const float CornerResizeHandleSize = 18f;

        private readonly Action<BehaviorTreeGroupView, Rect, Vector2> _onMoved;
        private readonly Action _onChanged;
        private readonly TextField _titleField;
        private bool _suppressMoveChildren;
        private bool _isResizing;
        private Vector2 _resizeStartMouse;
        private Rect _resizeStartRect;

        public BehaviorTreeGroupView(
            BehaviorTreeEditorGroup group,
            Action<BehaviorTreeGroupView, Rect, Vector2> onMoved,
            Action onChanged)
        {
            Group = group;
            _onMoved = onMoved;
            _onChanged = onChanged;
            viewDataKey = Group.Guid;

            capabilities |= Capabilities.Movable | Capabilities.Selectable | Capabilities.Deletable;
            pickingMode = PickingMode.Position;

            style.position = Position.Absolute;
            style.borderTopWidth = 2f;
            style.borderRightWidth = 2f;
            style.borderBottomWidth = 2f;
            style.borderLeftWidth = 2f;
            style.borderTopLeftRadius = 7f;
            style.borderTopRightRadius = 7f;
            style.borderBottomLeftRadius = 7f;
            style.borderBottomRightRadius = 7f;
            ApplyColor(Group.Color);

            _titleField = CreateTitleField();
            Add(_titleField);

            Add(CreateResizeHandle(ResizeDirection.Top));
            Add(CreateResizeHandle(ResizeDirection.Right));
            Add(CreateResizeHandle(ResizeDirection.Bottom));
            Add(CreateResizeHandle(ResizeDirection.Left));
            Add(CreateResizeHandle(ResizeDirection.Top | ResizeDirection.Left));
            Add(CreateResizeHandle(ResizeDirection.Top | ResizeDirection.Right));
            Add(CreateResizeHandle(ResizeDirection.Bottom | ResizeDirection.Left));
            Add(CreateResizeHandle(ResizeDirection.Bottom | ResizeDirection.Right));

            _suppressMoveChildren = true;
            SetPosition(Group.Rect);
            _suppressMoveChildren = false;
        }

        public BehaviorTreeEditorGroup Group { get; }

        public override void SetPosition(Rect newPos)
        {
            var previousRect = Group.Rect;
            var clampedRect = new Rect(
                newPos.xMin,
                newPos.yMin,
                Mathf.Max(MinWidth, newPos.width),
                Mathf.Max(MinHeight, newPos.height));
            var delta = clampedRect.position - previousRect.position;
            var sizeChanged = !Mathf.Approximately(clampedRect.width, previousRect.width) ||
                              !Mathf.Approximately(clampedRect.height, previousRect.height);

            base.SetPosition(clampedRect);
            Group.Rect = clampedRect;

            if (!_suppressMoveChildren && !_isResizing && !sizeChanged && delta.sqrMagnitude > 0.0001f)
                _onMoved?.Invoke(this, previousRect, delta);
        }

        public void PersistPosition()
        {
            Group.Rect = GetPosition();
        }

        public void RefreshView()
        {
            _titleField.SetValueWithoutNotify(Group.Title);
            _titleField.style.backgroundColor = BehaviorTreeEditorStyles.WithAlpha(Group.Color, 0.72f);
            ApplyColor(Group.Color);

            _suppressMoveChildren = true;
            SetPosition(Group.Rect);
            _suppressMoveChildren = false;
        }

        private TextField CreateTitleField()
        {
            var field = new TextField
            {
                value = Group.Title,
                isDelayed = true
            };
            field.style.height = 28f;
            field.style.marginLeft = 0f;
            field.style.marginRight = 0f;
            field.style.marginTop = 0f;
            field.style.marginBottom = 0f;
            field.style.paddingLeft = 10f;
            field.style.paddingRight = 10f;
            field.style.unityFontStyleAndWeight = FontStyle.Bold;
            field.style.fontSize = 12f;
            field.style.color = BehaviorTreeEditorStyles.Text;
            field.style.backgroundColor = BehaviorTreeEditorStyles.WithAlpha(Group.Color, 0.72f);
            field.style.borderBottomColor = BehaviorTreeEditorStyles.WithAlpha(Color.white, 0.12f);
            field.style.borderBottomWidth = 1f;
            field.RegisterValueChangedCallback(evt =>
            {
                Group.Title = evt.newValue;
                _onChanged?.Invoke();
            });
            return field;
        }

        private VisualElement CreateResizeHandle(ResizeDirection direction)
        {
            var handle = new VisualElement();
            handle.style.position = Position.Absolute;
            handle.pickingMode = PickingMode.Position;
            ApplyResizeHandleLayout(handle, direction);

            handle.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0)
                    return;

                _isResizing = true;
                _resizeStartMouse = evt.mousePosition;
                _resizeStartRect = GetPosition();
                handle.CaptureMouse();
                evt.StopPropagation();
            });

            handle.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (!_isResizing)
                    return;

                var delta = evt.mousePosition - _resizeStartMouse;
                var rect = CalculateResizedRect(_resizeStartRect, delta, direction);
                _suppressMoveChildren = true;
                SetPosition(rect);
                _suppressMoveChildren = false;
                evt.StopPropagation();
            });

            handle.RegisterCallback<MouseUpEvent>(evt =>
            {
                if (!_isResizing || evt.button != 0)
                    return;

                _isResizing = false;
                handle.ReleaseMouse();
                PersistPosition();
                _onChanged?.Invoke();
                evt.StopPropagation();
            });

            return handle;
        }

        private static void ApplyResizeHandleLayout(VisualElement handle, ResizeDirection direction)
        {
            var horizontalEdge = direction is ResizeDirection.Top or ResizeDirection.Bottom;
            var verticalEdge = direction is ResizeDirection.Left or ResizeDirection.Right;

            if (horizontalEdge)
            {
                handle.style.height = EdgeResizeHandleSize;
                handle.style.left = CornerResizeHandleSize;
                handle.style.right = CornerResizeHandleSize;
            }
            else if (verticalEdge)
            {
                handle.style.width = EdgeResizeHandleSize;
                handle.style.top = CornerResizeHandleSize;
                handle.style.bottom = CornerResizeHandleSize;
            }
            else
            {
                handle.style.width = CornerResizeHandleSize;
                handle.style.height = CornerResizeHandleSize;
                handle.style.backgroundColor = BehaviorTreeEditorStyles.WithAlpha(Color.white, 0.12f);
            }

            if (direction.HasFlag(ResizeDirection.Left))
                handle.style.left = 0f;
            if (direction.HasFlag(ResizeDirection.Right))
                handle.style.right = 0f;
            if (direction.HasFlag(ResizeDirection.Top))
                handle.style.top = 0f;
            if (direction.HasFlag(ResizeDirection.Bottom))
                handle.style.bottom = 0f;
        }

        private static Rect CalculateResizedRect(Rect startRect, Vector2 delta, ResizeDirection direction)
        {
            var rect = startRect;

            if (direction.HasFlag(ResizeDirection.Left))
                rect.xMin = Mathf.Min(startRect.xMax - MinWidth, startRect.xMin + delta.x);
            if (direction.HasFlag(ResizeDirection.Right))
                rect.xMax = Mathf.Max(startRect.xMin + MinWidth, startRect.xMax + delta.x);
            if (direction.HasFlag(ResizeDirection.Top))
                rect.yMin = Mathf.Min(startRect.yMax - MinHeight, startRect.yMin + delta.y);
            if (direction.HasFlag(ResizeDirection.Bottom))
                rect.yMax = Mathf.Max(startRect.yMin + MinHeight, startRect.yMax + delta.y);

            return rect;
        }

        private void ApplyColor(Color color)
        {
            style.backgroundColor = color;
            style.borderTopColor = BehaviorTreeEditorStyles.WithAlpha(color, 0.95f);
            style.borderRightColor = BehaviorTreeEditorStyles.WithAlpha(color, 0.95f);
            style.borderBottomColor = BehaviorTreeEditorStyles.WithAlpha(color, 0.95f);
            style.borderLeftColor = BehaviorTreeEditorStyles.WithAlpha(color, 0.95f);
        }

        [Flags]
        private enum ResizeDirection
        {
            Top = 1,
            Right = 2,
            Bottom = 4,
            Left = 8
        }
    }
}
#endif
