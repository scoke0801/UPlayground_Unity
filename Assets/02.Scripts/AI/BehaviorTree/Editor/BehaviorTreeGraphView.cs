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
        private readonly Dictionary<BehaviorTreeEditorGroup, BehaviorTreeGroupView> _groupViews = new();
        private readonly Dictionary<string, BTStatus> _currentTickStatuses = new();
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
            _groupViews.Clear();

            if (_tree == null)
            {
                graphViewChanged += OnGraphViewChanged;
                return;
            }

            foreach (var group in _tree.EditorGroups.Where(group => group != null))
                AddGroupView(group);

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
                    StyleEdge(edge, false, false);
                    AddElement(edge);
                }
            }

            graphViewChanged += OnGraphViewChanged;
        }

        public void UpdateDebugState(BehaviorTreeAsset runtimeTree, BehaviorTreeDebugTrace trace = null)
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
                runtimeByGuid.TryGetValue(pair.Key.Guid, out var runtimeNode);
                var wasTicked = _currentTickStatuses.TryGetValue(pair.Key.Guid, out var tickStatus);
                pair.Value.UpdateStateColor(runtimeNode, wasTicked, tickStatus);
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

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (_tree == null)
            {
                base.BuildContextualMenu(evt);
                return;
            }

            var mousePosition = contentViewContainer.WorldToLocal(evt.mousePosition);
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
            return graphViewChange;
        }

        private void CreateGroup(Vector2 position)
        {
            if (_tree == null)
                return;

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

            var bounds = selectedNodes[0].GetPosition();
            foreach (var node in selectedNodes.Skip(1))
            {
                var rect = node.GetPosition();
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

        private void AddNodeView(BTNode node)
        {
            var nodeView = new BehaviorTreeNodeView(node, _tree != null ? _tree.Nodes.IndexOf(node) : -1);
            nodeView.OnSetRoot += SetRoot;
            nodeView.RegisterCallback<MouseDownEvent>(_ => _window.SelectNode(nodeView.Node));
            _nodeViews[node] = nodeView;
            AddElement(nodeView);
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

                nodeRect.position += delta;
                nodeView.SetPosition(nodeRect);
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
            foreach (var edge in edges.ToList())
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
            return BehaviorTreeEditorStyles.Action;
        }
    }

    internal sealed class BehaviorTreeGroupView : GraphElement
    {
        private const float MinWidth = 220f;
        private const float MinHeight = 140f;

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

            var resizeHandle = CreateResizeHandle();
            Add(resizeHandle);

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

        private VisualElement CreateResizeHandle()
        {
            var handle = new VisualElement();
            handle.style.position = Position.Absolute;
            handle.style.right = 0f;
            handle.style.bottom = 0f;
            handle.style.width = 18f;
            handle.style.height = 18f;
            handle.style.backgroundColor = BehaviorTreeEditorStyles.WithAlpha(Color.white, 0.14f);
            handle.style.borderTopLeftRadius = 5f;

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
                var rect = _resizeStartRect;
                rect.width = Mathf.Max(MinWidth, rect.width + delta.x);
                rect.height = Mathf.Max(MinHeight, rect.height + delta.y);
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

        private void ApplyColor(Color color)
        {
            style.backgroundColor = color;
            style.borderTopColor = BehaviorTreeEditorStyles.WithAlpha(color, 0.95f);
            style.borderRightColor = BehaviorTreeEditorStyles.WithAlpha(color, 0.95f);
            style.borderBottomColor = BehaviorTreeEditorStyles.WithAlpha(color, 0.95f);
            style.borderLeftColor = BehaviorTreeEditorStyles.WithAlpha(color, 0.95f);
        }
    }
}
#endif
