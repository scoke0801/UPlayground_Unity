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
    public partial class BehaviorTreeGraphView : GraphView
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
            CenterOnNodeView(nodeView);
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

        private void CenterOnNodeView(BehaviorTreeNodeView nodeView)
        {
            var rect = nodeView.GetPosition();
            CenterOnContentPosition(rect.center);

            nodeView.BringToFront();
            nodeView.Focus();
            nodeView.MarkDirtyRepaint();

            schedule.Execute(() =>
            {
                if (nodeView.panel == null)
                    return;

                ClearSelection();
                AddToSelection(nodeView);
                CenterOnContentPosition(nodeView.GetPosition().center);
            }).ExecuteLater(1);
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
    }
}
#endif
