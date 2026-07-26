using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.FlowGraph.Editor
{
    /// <summary>
    /// FlowGraphSO 저작용 GraphView. 노드 생성/이동/연결/삭제를 에셋에 즉시 반영한다
    /// (BehaviorTreeGraphView 패턴 계승, 노드는 서브에셋이 아닌 [SerializeReference] 리스트).
    /// </summary>
    public sealed class FlowGraphView : GraphView
    {
        private readonly Action<FlowNodeView> _onNodeSelected;
        private FlowGraphSO _graph;
        private FlowNodeSearchWindow _searchWindow;
        private EditorWindow _hostWindow;

        public FlowGraphView(Action<FlowNodeView> onNodeSelected)
        {
            _onNodeSelected = onNodeSelected;
            ConnectorListener = new FlowEdgeConnectorListener(this);

            Insert(0, new GridBackground());
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var miniMap = new MiniMap { anchored = true };
            miniMap.SetPosition(new Rect(10, 10, 180, 120));
            Add(miniMap);

            // 창 크기 변경 시 미니맵을 우상단에 유지 (시안 위치). 캔버스가 좁으면 캔버스 안으로 클램프.
            RegisterCallback<GeometryChangedEvent>(_ =>
            {
                float x = Mathf.Max(10f, layout.width - 190f);
                miniMap.SetPosition(new Rect(x, 10, 180, 120));
            });

            graphViewChanged = OnGraphViewChanged;

            // 복사/붙여넣기/복제 (Ctrl+C/V/D — GraphView 기본 단축키에 배선)
            serializeGraphElements = OnSerializeGraphElements;
            canPasteSerializedData = data =>
                _graph != null && data != null && data.StartsWith(ClipboardPrefix, StringComparison.Ordinal);
            unserializeAndPaste = OnUnserializeAndPaste;

            style.flexGrow = 1f;
        }

        public FlowGraphSO Graph => _graph;

        /// <summary>노드/연결이 변경될 때마다 발화 — 상태바·검증 패널 갱신 게이트.</summary>
        public event Action GraphMutated;

        /// <summary>노드 뷰 포트들이 공유하는 엣지 커넥터 리스너 (드롭 아웃사이드 → 노드 생성).</summary>
        internal FlowEdgeConnectorListener ConnectorListener { get; }

        private Port _pendingConnectOrigin;
        private FlowEdgeView _pendingInsertEdge;

        public void SetupSearchWindow(EditorWindow hostWindow)
        {
            _hostWindow = hostWindow;
            _searchWindow = ScriptableObject.CreateInstance<FlowNodeSearchWindow>();
            _searchWindow.Initialize(this);
            nodeCreationRequest = context =>
            {
                if (_graph == null)
                    return;
                _pendingConnectOrigin = null; // 우클릭 생성은 자동 연결 없음
                _pendingInsertEdge = null;
                SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), _searchWindow);
            };
        }

        // ──────────────────────────────────────────────────────────
        #region 로드/저장

        public void PopulateView(FlowGraphSO graph)
        {
            _graph = graph;

            graphViewChanged = null;
            DeleteElements(graphElements.ToList());
            graphViewChanged = OnGraphViewChanged;

            if (graph == null)
                return;

            foreach (FlowNode node in graph.nodes)
            {
                if (node == null)
                    continue;
                var view = new FlowNodeView(this, node, _onNodeSelected);
                if (_compactMode)
                    view.SetCompact(true);
                AddElement(view);
            }

            foreach (FlowConnection connection in graph.connections)
            {
                FlowNodeView fromView = FindNodeView(connection.fromNodeId);
                FlowNodeView toView = FindNodeView(connection.toNodeId);
                Port fromPort = fromView?.FindPort(Direction.Output, connection.fromPort);
                Port toPort = toView?.FindPort(Direction.Input, connection.toPort);
                if (fromPort == null || toPort == null)
                    continue;

                AddElement(CreateEdgeView(fromPort, toPort));
            }

            // 그룹 복원 (멤버 추가는 데이터 역기록 없이)
            foreach (FlowGraphGroup groupData in graph.editorGroups)
            {
                var groupView = new FlowGroupView(graph, groupData);
                AddElement(groupView);

                var members = new List<GraphElement>();
                foreach (string nodeId in groupData.nodeIds)
                {
                    FlowNodeView member = FindNodeView(nodeId);
                    if (member != null)
                        members.Add(member);
                }
                groupView.AddElementsWithoutSync(members);
            }
        }

        public FlowNodeView FindNodeView(string nodeId) => GetNodeByGuid(nodeId) as FlowNodeView;

        /// <summary>블랙보드 이름/타입 변경처럼 노드 구조는 유지한 채 표시 내용만 바뀐 경우 갱신한다.</summary>
        public void RefreshNodeContents()
        {
            foreach (Node node in nodes)
            {
                if (node is not FlowNodeView view)
                    continue;
                view.RefreshTitle();
                view.RebuildSummary();
            }
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (_graph == null)
                return change;

            bool dirty = false;

            if (change.elementsToRemove != null)
            {
                RecordUndo("FlowGraph 요소 삭제");
                foreach (GraphElement element in change.elementsToRemove)
                {
                    switch (element)
                    {
                        case FlowNodeView nodeView:
                            _graph.nodes.Remove(nodeView.FlowNode);
                            _graph.connections.RemoveAll(c =>
                                c.fromNodeId == nodeView.FlowNode.id || c.toNodeId == nodeView.FlowNode.id);
                            dirty = true;
                            break;

                        case Edge edge:
                            RemoveConnection(edge);
                            dirty = true;
                            break;

                        case FlowGroupView groupView:
                            _graph.editorGroups.Remove(groupView.Data);
                            dirty = true;
                            break;
                    }
                }
            }

            if (change.edgesToCreate != null)
            {
                RecordUndo("FlowGraph 연결 생성");
                foreach (Edge edge in change.edgesToCreate)
                {
                    if (edge.output.node is FlowNodeView fromView && edge.input.node is FlowNodeView toView)
                    {
                        _graph.connections.Add(new FlowConnection
                        {
                            fromNodeId = fromView.FlowNode.id,
                            fromPort = GetPortId(edge.output),
                            toNodeId = toView.FlowNode.id,
                            toPort = GetPortId(edge.input),
                        });
                        dirty = true;
                    }
                }
            }

            if (change.movedElements != null)
            {
                RecordUndo("FlowGraph 노드 이동");
                foreach (GraphElement element in change.movedElements)
                {
                    switch (element)
                    {
                        case FlowNodeView nodeView:
                            nodeView.FlowNode.editorPosition = nodeView.GetPosition().position;
                            dirty = true;
                            break;

                        case FlowGroupView groupView:
                            groupView.SavePosition();
                            // 그룹 이동 시 멤버 노드 위치도 함께 저장
                            foreach (GraphElement member in groupView.containedElements)
                            {
                                if (member is FlowNodeView memberView)
                                    memberView.FlowNode.editorPosition = memberView.GetPosition().position;
                            }
                            dirty = true;
                            break;
                    }
                }
            }

            if (dirty)
            {
                EditorUtility.SetDirty(_graph);
                GraphMutated?.Invoke();
            }

            return change;
        }

        private void RemoveConnection(Edge edge)
        {
            if (edge.output?.node is not FlowNodeView fromView || edge.input?.node is not FlowNodeView toView)
                return;

            _graph.connections.RemoveAll(c =>
                c.fromNodeId == fromView.FlowNode.id
                && c.fromPort == GetPortId(edge.output)
                && c.toNodeId == toView.FlowNode.id
                && c.toPort == GetPortId(edge.input));
        }

        private void RecordUndo(string label)
        {
            Undo.RegisterCompleteObjectUndo(_graph, label);
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 노드 생성/연결 규칙

        public void CreateNodeAtScreenPosition(Type nodeType, Vector2 screenMousePosition)
        {
            if (_graph == null)
                return;

            Vector2 windowPos = _hostWindow != null
                ? screenMousePosition - _hostWindow.position.position
                : screenMousePosition;
            Vector2 graphPos = contentViewContainer.WorldToLocal(windowPos);

            CreateNode(nodeType, graphPos);
        }

        /// <summary>현재 뷰포트 중앙에 노드를 생성한다 (노드 라이브러리 패널 클릭 생성용).</summary>
        public void CreateNodeAtViewCenter(Type nodeType)
        {
            if (_graph == null)
                return;

            _pendingConnectOrigin = null;
            _pendingInsertEdge = null;
            Vector2 worldCenter = this.LocalToWorld(new Vector2(layout.width * 0.5f, layout.height * 0.5f));
            CreateNode(nodeType, contentViewContainer.WorldToLocal(worldCenter));
        }

        private void CreateNode(Type nodeType, Vector2 graphPosition)
        {
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(
                _pendingInsertEdge != null ? "FlowGraph 연결 사이에 노드 삽입" : "FlowGraph 노드 생성");
            RecordUndo("FlowGraph 노드 생성");
            var node = (FlowNode)Activator.CreateInstance(nodeType);
            node.editorPosition = graphPosition;
            _graph.nodes.Add(node);
            EditorUtility.SetDirty(_graph);

            var view = new FlowNodeView(this, node, _onNodeSelected);
            if (_compactMode)
                view.SetCompact(true);
            AddElement(view);

            // 기존 연결 중간에 생성했다면 원본 Edge를 두 연결로 교체한다.
            if (_pendingInsertEdge != null)
            {
                FlowEdgeView oldEdge = _pendingInsertEdge;
                _pendingInsertEdge = null;
                Port upstream = oldEdge.output;
                Port downstream = oldEdge.input;
                DeleteElements(new List<GraphElement> { oldEdge });

                Port nodeInput = view.FindCompatiblePort(upstream);
                Port nodeOutput = view.FindCompatiblePort(downstream);
                ConnectPorts(upstream, nodeInput);
                ConnectPorts(nodeOutput, downstream);
            }
            // 포트 드래그로 생성했다면 원점 포트와 자동 연결 (FlowCanvas 참조)
            else if (_pendingConnectOrigin != null)
            {
                Port origin = _pendingConnectOrigin;
                _pendingConnectOrigin = null;
                Port compatible = view.FindCompatiblePort(origin);
                if (origin.direction == Direction.Output)
                    ConnectPorts(origin, compatible);
                else
                    ConnectPorts(compatible, origin);
            }

            GraphMutated?.Invoke();
            Undo.CollapseUndoOperations(undoGroup);
        }

        /// <summary>포트 간 연결을 모델·뷰에 함께 반영한다 (중복 연결은 무시).</summary>
        internal void ConnectPorts(Port output, Port input)
        {
            if (_graph == null || output == null || input == null || output.node == input.node)
                return;
            if (output.node is not FlowNodeView fromView || input.node is not FlowNodeView toView)
                return;
            if (output is not FlowPortView outputView
                || input is not FlowPortView inputView
                || !FlowPortDef.AreCompatible(outputView.Definition, inputView.Definition))
                return;

            string outputId = GetPortId(output);
            string inputId = GetPortId(input);

            bool exists = _graph.connections.Exists(c =>
                c.fromNodeId == fromView.FlowNode.id
                && c.fromPort == outputId
                && c.toNodeId == toView.FlowNode.id
                && c.toPort == inputId);
            if (exists)
                return;

            if (output.capacity == Port.Capacity.Single)
                DeleteElements(output.connections.ToList());
            if (input.capacity == Port.Capacity.Single)
                DeleteElements(input.connections.ToList());

            RecordUndo("FlowGraph 연결 생성");
            _graph.connections.Add(new FlowConnection
            {
                fromNodeId = fromView.FlowNode.id,
                fromPort = outputId,
                toNodeId = toView.FlowNode.id,
                toPort = inputId,
            });
            AddElement(CreateEdgeView(output, input));
            EditorUtility.SetDirty(_graph);
            GraphMutated?.Invoke();
        }

        /// <summary>포트 드래그를 빈 캔버스에 드롭 — 검색창을 열고 선택된 노드를 자동 연결 대기로 만든다.</summary>
        internal void OpenSearchForPendingConnection(Edge edge, Vector2 position)
        {
            if (_graph == null || _searchWindow == null)
                return;

            _pendingConnectOrigin = edge.output ?? edge.input;
            _pendingInsertEdge = null;
            SearchWindow.Open(
                new SearchWindowContext(GUIUtility.GUIToScreenPoint(position)),
                _searchWindow);
        }

        internal bool CanCreateForPendingConnection(Type nodeType)
        {
            if (_pendingInsertEdge?.output is FlowPortView upstream
                && _pendingInsertEdge.input is FlowPortView downstream)
            {
                return FlowNodeCatalog.CanBridge(
                    nodeType,
                    upstream.Definition,
                    downstream.Definition);
            }
            if (_pendingConnectOrigin is not FlowPortView origin)
                return true;
            return FlowNodeCatalog.HasCompatiblePort(nodeType, origin.Definition);
        }

        internal void OpenInsertSearch(FlowEdgeView edge, Vector2 panelPosition)
        {
            if (_graph == null || _searchWindow == null || edge?.output == null || edge.input == null)
                return;

            _pendingConnectOrigin = null;
            _pendingInsertEdge = edge;
            SearchWindow.Open(
                new SearchWindowContext(GUIUtility.GUIToScreenPoint(panelPosition)),
                _searchWindow);
        }

        private FlowEdgeView CreateEdgeView(Port output, Port input)
        {
            var edge = new FlowEdgeView(this)
            {
                output = output,
                input = input,
            };
            output.Connect(edge);
            input.Connect(edge);
            return edge;
        }

        /// <summary>서브그래프 노드 더블클릭 → 창이 브레드크럼과 함께 하위 그래프를 연다.</summary>
        public event Action<FlowGraphSO> SubGraphOpenRequested;

        internal void RequestOpenSubGraph(FlowGraphSO subGraph)
        {
            if (subGraph != null)
                SubGraphOpenRequested?.Invoke(subGraph);
        }

        // ──────────────────────────────────────────────────────────
        #region 클립보드 (BT Clipboard 이식 — SerializeReference라 EditorJsonUtility 딥카피 방식)

        private const string ClipboardPrefix = "UPGFlowGraphClipboard:";

        /// <summary>클립보드 직렬화 캐리어. SerializeReference는 EditorJsonUtility로만 왕복된다.</summary>
        private sealed class FlowClipboardCarrier : ScriptableObject
        {
            [SerializeReference] public List<FlowNode> nodes = new();
            public List<FlowConnection> connections = new();
        }

        private string OnSerializeGraphElements(IEnumerable<GraphElement> elements)
        {
            if (_graph == null)
                return string.Empty;

            var carrier = ScriptableObject.CreateInstance<FlowClipboardCarrier>();
            try
            {
                var selectedIds = new HashSet<string>();
                foreach (GraphElement element in elements)
                {
                    if (element is FlowNodeView nodeView && selectedIds.Add(nodeView.FlowNode.id))
                        carrier.nodes.Add(nodeView.FlowNode);
                }

                if (carrier.nodes.Count == 0)
                    return string.Empty;

                foreach (FlowConnection connection in _graph.connections)
                {
                    if (selectedIds.Contains(connection.fromNodeId) && selectedIds.Contains(connection.toNodeId))
                        carrier.connections.Add(connection);
                }

                return ClipboardPrefix + EditorJsonUtility.ToJson(carrier);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(carrier);
            }
        }

        private void OnUnserializeAndPaste(string operationName, string data)
        {
            if (_graph == null || data == null || !data.StartsWith(ClipboardPrefix, StringComparison.Ordinal))
                return;

            var carrier = ScriptableObject.CreateInstance<FlowClipboardCarrier>();
            try
            {
                EditorJsonUtility.FromJsonOverwrite(data.Substring(ClipboardPrefix.Length), carrier);
                if (carrier.nodes.Count == 0)
                    return;

                RecordUndo("FlowGraph 붙여넣기");

                // 원본 중심 → 현재 뷰포트 중심으로 오프셋 (BT 패턴)
                var bounds = new Rect(carrier.nodes[0].editorPosition, Vector2.one);
                foreach (FlowNode node in carrier.nodes)
                {
                    if (node == null)
                        continue;
                    bounds.xMin = Mathf.Min(bounds.xMin, node.editorPosition.x);
                    bounds.yMin = Mathf.Min(bounds.yMin, node.editorPosition.y);
                    bounds.xMax = Mathf.Max(bounds.xMax, node.editorPosition.x);
                    bounds.yMax = Mathf.Max(bounds.yMax, node.editorPosition.y);
                }
                Vector2 worldCenter = this.LocalToWorld(new Vector2(layout.width * 0.5f, layout.height * 0.5f));
                Vector2 offset = contentViewContainer.WorldToLocal(worldCenter) - bounds.center
                                 + new Vector2(32f, 32f);

                // FromJsonOverwrite가 만든 인스턴스는 이미 딥카피 — 새 id 부여 후 그래프에 편입
                var idMap = new Dictionary<string, string>();
                var pastedIds = new List<string>();
                foreach (FlowNode node in carrier.nodes)
                {
                    if (node == null)
                        continue;

                    string newId = Guid.NewGuid().ToString("N");
                    idMap[node.id] = newId;
                    node.id = newId;
                    node.editorPosition += offset;
                    _graph.nodes.Add(node);
                    pastedIds.Add(newId);
                }

                foreach (FlowConnection connection in carrier.connections)
                {
                    if (idMap.TryGetValue(connection.fromNodeId, out string fromId)
                        && idMap.TryGetValue(connection.toNodeId, out string toId))
                    {
                        _graph.connections.Add(new FlowConnection
                        {
                            fromNodeId = fromId,
                            fromPort = connection.fromPort,
                            toNodeId = toId,
                            toPort = connection.toPort,
                        });
                    }
                }

                EditorUtility.SetDirty(_graph);
                PopulateView(_graph);
                GraphMutated?.Invoke();

                ClearSelection();
                foreach (string id in pastedIds)
                {
                    FlowNodeView view = FindNodeView(id);
                    if (view != null)
                        AddToSelection(view);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(carrier);
            }
        }

        #endregion

        /// <summary>노드 브레이크포인트 토글 (노드 우클릭 메뉴).</summary>
        internal void ToggleBreakpoint(FlowNodeView nodeView)
        {
            if (_graph == null || nodeView?.FlowNode == null)
                return;

            RecordUndo("브레이크포인트 토글");
            nodeView.FlowNode.breakpoint = !nodeView.FlowNode.breakpoint;
            nodeView.RefreshBreakpointMarker();
            EditorUtility.SetDirty(_graph);
        }

        internal void SetBreakpointDisabled(FlowNodeView nodeView, bool disabled)
        {
            if (_graph == null || nodeView?.FlowNode == null || !nodeView.FlowNode.breakpoint)
                return;

            RecordUndo(disabled ? "브레이크포인트 비활성화" : "브레이크포인트 활성화");
            nodeView.FlowNode.breakpointDisabled = disabled;
            nodeView.RefreshBreakpointMarker();
            EditorUtility.SetDirty(_graph);
        }

        /// <summary>캔버스 우클릭 메뉴 — 선택 노드로 그룹 생성 (FlowCanvas Groups 참조).</summary>
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            if (_graph == null || evt.target is not GraphView)
                return;

            Vector2 graphPosition = contentViewContainer.WorldToLocal(evt.mousePosition);
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("그룹 생성", _ => CreateGroup(graphPosition),
                selection.Count > 0
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendAction("정렬/왼쪽 맞춤", _ => AlignSelectedNodes(horizontal: true),
                SelectedNodeCount > 1 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendAction("정렬/위쪽 맞춤", _ => AlignSelectedNodes(horizontal: false),
                SelectedNodeCount > 1 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendAction("정렬/가로 간격 균등", _ => DistributeSelectedNodes(),
                SelectedNodeCount > 2 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendAction(
                "선택을 SubGraph로 추출",
                _ => ExtractSelectionToSubGraph(),
                SelectedNodeCount > 0
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);
        }

        private int SelectedNodeCount => selection.Count(item => item is FlowNodeView);

        private List<FlowNodeView> GetSelectedNodeViews()
        {
            return selection.OfType<FlowNodeView>().ToList();
        }

        private void AlignSelectedNodes(bool horizontal)
        {
            List<FlowNodeView> selected = GetSelectedNodeViews();
            if (selected.Count < 2)
                return;

            RecordUndo(horizontal ? "FlowGraph 왼쪽 맞춤" : "FlowGraph 위쪽 맞춤");
            float target = horizontal
                ? selected.Min(view => view.GetPosition().xMin)
                : selected.Min(view => view.GetPosition().yMin);
            foreach (FlowNodeView view in selected)
            {
                Rect rect = view.GetPosition();
                if (horizontal) rect.x = target;
                else rect.y = target;
                view.SetPosition(rect);
                view.FlowNode.editorPosition = rect.position;
            }
            EditorUtility.SetDirty(_graph);
            GraphMutated?.Invoke();
        }

        private void DistributeSelectedNodes()
        {
            List<FlowNodeView> selected = GetSelectedNodeViews()
                .OrderBy(view => view.GetPosition().center.x)
                .ToList();
            if (selected.Count < 3)
                return;

            RecordUndo("FlowGraph 가로 간격 균등");
            float first = selected[0].GetPosition().center.x;
            float last = selected[^1].GetPosition().center.x;
            float step = (last - first) / (selected.Count - 1);
            for (int i = 1; i < selected.Count - 1; i++)
            {
                Rect rect = selected[i].GetPosition();
                rect.center = new Vector2(first + step * i, rect.center.y);
                selected[i].SetPosition(rect);
                selected[i].FlowNode.editorPosition = rect.position;
            }
            EditorUtility.SetDirty(_graph);
            GraphMutated?.Invoke();
        }

        private void ExtractSelectionToSubGraph()
        {
            List<FlowNodeView> selectedViews = GetSelectedNodeViews();
            if (selectedViews.Count == 0 || _graph == null)
                return;

            var selectedIds = new HashSet<string>(
                selectedViews.Select(view => view.FlowNode.id));
            if (selectedViews.Any(view => view.FlowNode is EntryNode))
            {
                EditorUtility.DisplayDialog(
                    "SubGraph 추출 불가",
                    "진입점 노드는 선택 영역에 포함할 수 없습니다.",
                    "확인");
                return;
            }

            List<FlowConnection> incoming = _graph.connections
                .Where(connection =>
                    connection != null
                    && !selectedIds.Contains(connection.fromNodeId)
                    && selectedIds.Contains(connection.toNodeId))
                .ToList();
            List<FlowConnection> outgoing = _graph.connections
                .Where(connection =>
                    connection != null
                    && selectedIds.Contains(connection.fromNodeId)
                    && !selectedIds.Contains(connection.toNodeId))
                .ToList();
            List<FlowConnection> internalConnections = _graph.connections
                .Where(connection =>
                    connection != null
                    && selectedIds.Contains(connection.fromNodeId)
                    && selectedIds.Contains(connection.toNodeId))
                .ToList();

            if (incoming.Count != 1 || outgoing.Count != 1)
            {
                EditorUtility.DisplayDialog(
                    "SubGraph 추출 불가",
                    $"1차 안전 추출은 실행 입력/출력이 각각 하나여야 합니다.\n" +
                    $"현재 입력 {incoming.Count}개, 출력 {outgoing.Count}개입니다.",
                    "확인");
                return;
            }

            if (!IsConnectedSelection(incoming[0].toNodeId, selectedIds, internalConnections)
                || HasHiddenTerminal(selectedViews, internalConnections, outgoing[0]))
            {
                EditorUtility.DisplayDialog(
                    "SubGraph 추출 불가",
                    "선택 영역이 하나로 연결되어 있지 않거나 외부 출력 외의 종료 경로가 있습니다.\n" +
                    "의미 보존을 위해 선택 범위를 조정하세요.",
                    "확인");
                return;
            }

            string parentPath = AssetDatabase.GetAssetPath(_graph);
            string directory = string.IsNullOrEmpty(parentPath)
                ? "Assets"
                : Path.GetDirectoryName(parentPath)?.Replace('\\', '/') ?? "Assets";
            string path = EditorUtility.SaveFilePanelInProject(
                "SubGraph 에셋 생성",
                $"{_graph.name}_SubGraph",
                "asset",
                "선택 영역을 저장할 FlowGraph 에셋을 선택하세요.",
                directory);
            if (string.IsNullOrEmpty(path))
                return;

            FlowGraphSO child = null;
            int undoGroup = Undo.GetCurrentGroup();
            try
            {
                Undo.SetCurrentGroupName("FlowGraph SubGraph 추출");
                Undo.RegisterCompleteObjectUndo(_graph, "FlowGraph SubGraph 추출");

                child = ScriptableObject.CreateInstance<FlowGraphSO>();
                child.graphId = Path.GetFileNameWithoutExtension(path);
                var entry = new ManualEntryNode
                {
                    entryId = "start",
                    editorPosition = selectedViews
                        .Select(view => view.FlowNode.editorPosition)
                        .Aggregate(Vector2.zero, (sum, position) => sum + position)
                        / selectedViews.Count - new Vector2(220f, 0f),
                };
                child.nodes.Add(entry);
                foreach (FlowNodeView view in selectedViews)
                    child.nodes.Add(view.FlowNode);
                child.connections.Add(new FlowConnection
                {
                    fromNodeId = entry.id,
                    fromPort = FlowPort.Out,
                    toNodeId = incoming[0].toNodeId,
                    toPort = incoming[0].toPort,
                });
                child.connections.AddRange(internalConnections);

                var subNode = new SubGraphNode
                {
                    subGraph = child,
                    entryId = "start",
                    waitForCompletion = true,
                    editorPosition = selectedViews
                        .Select(view => view.FlowNode.editorPosition)
                        .Aggregate(Vector2.zero, (sum, position) => sum + position)
                        / selectedViews.Count,
                };
                AddExtractedVariableParameters(selectedViews, child, subNode);

                _graph.nodes.RemoveAll(node => node != null && selectedIds.Contains(node.id));
                _graph.nodes.Add(subNode);
                _graph.connections.RemoveAll(connection =>
                    connection != null
                    && (selectedIds.Contains(connection.fromNodeId)
                        || selectedIds.Contains(connection.toNodeId)));
                _graph.connections.Add(new FlowConnection
                {
                    fromNodeId = incoming[0].fromNodeId,
                    fromPort = incoming[0].fromPort,
                    toNodeId = subNode.id,
                    toPort = FlowPort.In,
                });
                _graph.connections.Add(new FlowConnection
                {
                    fromNodeId = subNode.id,
                    fromPort = FlowPort.Out,
                    toNodeId = outgoing[0].toNodeId,
                    toPort = outgoing[0].toPort,
                });
                foreach (FlowGraphGroup group in _graph.editorGroups)
                    group?.nodeIds?.RemoveAll(selectedIds.Contains);
                _graph.editorGroups.RemoveAll(group =>
                    group == null || group.nodeIds == null || group.nodeIds.Count == 0);

                AssetDatabase.CreateAsset(child, path);
                Undo.RegisterCreatedObjectUndo(child, "FlowGraph SubGraph 에셋 생성");
                EditorUtility.SetDirty(child);
                EditorUtility.SetDirty(_graph);
                AssetDatabase.SaveAssetIfDirty(child);
                PopulateView(_graph);
                SelectAndFrame(subNode.id);
                GraphMutated?.Invoke();
                Undo.CollapseUndoOperations(undoGroup);
            }
            catch (Exception exception)
            {
                if (child != null && AssetDatabase.Contains(child))
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(child));
                Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "SubGraph 추출 실패",
                    "변경을 완료하지 못해 부모 그래프와 생성 에셋을 원복했습니다.",
                    "확인");
            }
        }

        private static bool IsConnectedSelection(
            string startNodeId,
            HashSet<string> selectedIds,
            List<FlowConnection> internalConnections)
        {
            var reached = new HashSet<string> { startNodeId };
            var queue = new Queue<string>();
            queue.Enqueue(startNodeId);
            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                foreach (FlowConnection connection in internalConnections)
                {
                    if (connection.fromNodeId != current || !reached.Add(connection.toNodeId))
                        continue;
                    queue.Enqueue(connection.toNodeId);
                }
            }
            return reached.SetEquals(selectedIds);
        }

        private static bool HasHiddenTerminal(
            List<FlowNodeView> selectedViews,
            List<FlowConnection> internalConnections,
            FlowConnection boundaryOutput)
        {
            foreach (FlowNodeView view in selectedViews)
            {
                foreach (FlowPortDef port in view.FlowNode.Ports)
                {
                    if (port.Direction != FlowPortDirection.Output
                        || port.Kind != FlowPortKind.Execution)
                        continue;

                    bool connectedInside = internalConnections.Any(connection =>
                        connection.fromNodeId == view.FlowNode.id
                        && connection.fromPort == port.Id);
                    bool isBoundary = boundaryOutput.fromNodeId == view.FlowNode.id
                                      && boundaryOutput.fromPort == port.Id;
                    if (!connectedInside && !isBoundary)
                        return true;
                }
            }
            return false;
        }

        private void AddExtractedVariableParameters(
            List<FlowNodeView> selectedViews,
            FlowGraphSO child,
            SubGraphNode subNode)
        {
            var names = new HashSet<string>();
            foreach (FlowNodeView view in selectedViews)
            {
                switch (view.FlowNode)
                {
                    case SetVariableNode set when !string.IsNullOrEmpty(set.variableName):
                        names.Add(set.variableName);
                        break;
                    case CheckVariableNode check when !string.IsNullOrEmpty(check.variableName):
                        names.Add(check.variableName);
                        break;
                    case BranchNode branch when branch.condition is VariableCondition variable:
                        names.Add(variable.variableName);
                        break;
                    case WaitConditionNode wait when wait.condition is VariableCondition variable:
                        names.Add(variable.variableName);
                        break;
                }
            }

            foreach (string name in names)
            {
                FlowVariableDef parentVariable = _graph.GetVariable(null, name);
                if (parentVariable == null)
                    continue;
                var parameter = new FlowGraphParameterDef
                {
                    name = parentVariable.name,
                    type = parentVariable.type,
                    direction = FlowParameterDirection.InOut,
                    defaultValue = new FlowVariableValue
                    {
                        type = parentVariable.type,
                        boolValue = parentVariable.boolValue,
                        intValue = parentVariable.intValue,
                        floatValue = parentVariable.floatValue,
                        stringValue = parentVariable.stringValue,
                    },
                };
                child.parameters.Add(parameter);
                subNode.parameterBindings.Add(new FlowParameterBinding
                {
                    parameterId = parameter.id,
                    parameterName = parameter.name,
                    parentVariableId = parentVariable.id,
                    parentVariableName = parentVariable.name,
                });
            }
        }

        private void CreateGroup(Vector2 graphPosition)
        {
            RecordUndo("그룹 생성");
            var data = new FlowGraphGroup { title = "그룹", position = graphPosition };
            _graph.editorGroups.Add(data);
            EditorUtility.SetDirty(_graph);

            var groupView = new FlowGroupView(_graph, data);
            AddElement(groupView);

            foreach (ISelectable selected in selection)
            {
                if (selected is FlowNodeView nodeView)
                    groupView.AddElement(nodeView); // OnElementsAdded가 데이터에 멤버 기록
            }
        }

        /// <summary>검증 패널에서 이슈 클릭 시 해당 노드를 선택·포커스한다.</summary>
        public void SelectAndFrame(string nodeId)
        {
            FlowNodeView view = FindNodeView(nodeId);
            if (view == null)
                return;

            ClearSelection();
            AddToSelection(view);
            FrameSelection();
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports
                .Where(port =>
                    port.node != startPort.node
                    && port is FlowPortView candidate
                    && startPort is FlowPortView start
                    && FlowPortDef.AreCompatible(start.Definition, candidate.Definition))
                .ToList();
        }

        internal static string GetPortId(Port port)
        {
            return port?.userData as string ?? port?.portName;
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 런타임 디버그 하이라이트 (증분 diff — BT 디버그 성능 교훈 계승)

        private readonly HashSet<string> _lastActiveNodeIds = new();
        private int _lastDebugVersion = -1;

        private const float AfterglowDuration = 2.5f;
        private readonly HashSet<Edge> _glowingEdges = new();
        private readonly HashSet<string> _waitProgressNodeIds = new();

        /// <summary>활성 토큰이 있는 노드만 증분으로 하이라이트한다. runner=null이면 전체 해제.</summary>
        public void UpdateDebugHighlight(FlowGraphRunner runner)
        {
            if (runner == null || runner.Graph != _graph)
            {
                ClearDebugHighlight();
                return;
            }

            // 1) 활성 노드 — 증분 diff (버전 게이트)
            if (runner.DebugVersion != _lastDebugVersion)
            {
                _lastDebugVersion = runner.DebugVersion;

                foreach (string nodeId in _lastActiveNodeIds.ToList())
                {
                    if (!runner.ActiveNodeCounts.ContainsKey(nodeId))
                    {
                        _lastActiveNodeIds.Remove(nodeId);
                        FindNodeView(nodeId)?.SetDebugActive(false);
                    }
                }

                foreach (string nodeId in runner.ActiveNodeCounts.Keys)
                {
                    if (_lastActiveNodeIds.Add(nodeId))
                        FindNodeView(nodeId)?.SetDebugActive(true);
                }
            }

            // 2) 페이드 표현은 버전과 무관하게 매 폴링 갱신 (대상은 최근 기록으로 한정)
            float now = Time.realtimeSinceStartup;
            UpdateNodeAfterglow(runner, now);
            UpdateEdgeGlow(runner, now);
            UpdateWaitProgress(runner);
        }

        /// <summary>순간 통과 노드도 실행 경로가 보이도록 최근 실행 노드에 페이드아웃 잔광을 그린다.</summary>
        private void UpdateNodeAfterglow(FlowGraphRunner runner, float now)
        {
            foreach (KeyValuePair<string, float> pair in runner.LastNodeExecuteTimes)
            {
                float age = now - pair.Value;
                if (age > AfterglowDuration + 0.5f)
                    continue;
                FindNodeView(pair.Key)?.SetAfterglow(1f - Mathf.Clamp01(age / AfterglowDuration));
            }
        }

        /// <summary>최근 토큰이 통과한 엣지를 하이라이트한다 (BP 와이어 펄스의 정적 근사).</summary>
        private void UpdateEdgeGlow(FlowGraphRunner runner, float now)
        {
            foreach (Edge edge in edges)
            {
                if (edge.output?.node is not FlowNodeView fromView || edge.input?.node is not FlowNodeView toView)
                    continue;

                string key = $"{fromView.FlowNode.id}:{GetPortId(edge.output)}:{toView.FlowNode.id}";
                bool glowing = runner.LastEdgeEmitTimes.TryGetValue(key, out float emitTime)
                    && now - emitTime <= AfterglowDuration;

                if (glowing)
                {
                    float intensity = 1f - Mathf.Clamp01((now - emitTime) / AfterglowDuration);
                    Color color = Color.Lerp(edge.output.portColor, new Color(0.98f, 0.75f, 0.25f), intensity);
                    edge.edgeControl.inputColor = color;
                    edge.edgeControl.outputColor = color;
                    edge.edgeControl.edgeWidth = intensity > 0.5f ? 4 : 3;
                    _glowingEdges.Add(edge);
                }
                else if (_glowingEdges.Remove(edge))
                {
                    // 잔광 종료 — 포트 색으로 복원
                    edge.edgeControl.inputColor = edge.input.portColor;
                    edge.edgeControl.outputColor = edge.output.portColor;
                    edge.edgeControl.edgeWidth = 2;
                }
            }
        }

        /// <summary>대기 중인 WaitTime 노드에 진행 바를 그린다.</summary>
        private void UpdateWaitProgress(FlowGraphRunner runner)
        {
            var stillWaiting = new HashSet<string>();
            foreach (string nodeId in runner.ActiveNodeCounts.Keys)
            {
                FlowNodeView view = FindNodeView(nodeId);
                if (view?.FlowNode is not WaitTimeNode)
                    continue;

                for (int i = 0; i < runner.ActiveContexts.Count; i++)
                {
                    if (runner.ActiveContexts[i].TryPeekNodeState(nodeId, out WaitTimeProgressState state))
                    {
                        view.SetWaitProgress(state.Progress01);
                        stillWaiting.Add(nodeId);
                        break;
                    }
                }
            }

            // 대기가 끝난 노드의 진행 바 제거
            foreach (string nodeId in _waitProgressNodeIds)
            {
                if (!stillWaiting.Contains(nodeId))
                    FindNodeView(nodeId)?.SetWaitProgress(-1f);
            }
            _waitProgressNodeIds.Clear();
            _waitProgressNodeIds.UnionWith(stillWaiting);
        }

        public void ClearDebugHighlight()
        {
            // force-clear: 버전 게이트와 무관하게 남은 하이라이트를 확실히 지운다.
            foreach (string nodeId in _lastActiveNodeIds)
                FindNodeView(nodeId)?.SetDebugActive(false);
            _lastActiveNodeIds.Clear();
            _lastDebugVersion = -1;

            foreach (Node node in nodes)
            {
                if (node is FlowNodeView view)
                {
                    view.SetAfterglow(0f);
                    view.SetWaitProgress(-1f);
                }
            }
            foreach (Edge edge in _glowingEdges)
            {
                if (edge?.input != null && edge.output != null)
                {
                    edge.edgeControl.inputColor = edge.input.portColor;
                    edge.edgeControl.outputColor = edge.output.portColor;
                    edge.edgeControl.edgeWidth = 2;
                }
            }
            _glowingEdges.Clear();
            _waitProgressNodeIds.Clear();
        }

        // ──────────────────────────────────────────────────────────
        #region 컴팩트 모드

        private bool _compactMode;

        public bool CompactMode => _compactMode;

        /// <summary>본문 요약 일괄 숨김/표시 — 큰 그래프 조망용 (FlowCanvas compact 참조).</summary>
        public void SetCompactMode(bool compact)
        {
            _compactMode = compact;
            foreach (Node node in nodes)
            {
                if (node is FlowNodeView view)
                    view.SetCompact(compact);
            }
        }

        #endregion

        #endregion
    }

    /// <summary>연결 해제와 중간 노드 삽입 동작을 제공하는 FlowGraph 전용 Edge.</summary>
    internal sealed class FlowEdgeView : Edge
    {
        private readonly FlowGraphView _owner;

        public FlowEdgeView(FlowGraphView owner)
        {
            _owner = owner;
            RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 0 && evt.altKey)
                {
                    _owner.DeleteElements(new List<GraphElement> { this });
                    evt.StopImmediatePropagation();
                    return;
                }

                if (evt.button == 0 && evt.clickCount == 2)
                {
                    _owner.OpenInsertSearch(this, evt.mousePosition);
                    evt.StopImmediatePropagation();
                }
            });
            RegisterCallback<ContextualMenuPopulateEvent>(evt =>
            {
                evt.menu.AppendAction(
                    "연결 사이에 노드 삽입",
                    _ => _owner.OpenInsertSearch(this, evt.mousePosition));
                evt.menu.AppendAction(
                    "연결 해제",
                    _ => _owner.DeleteElements(new List<GraphElement> { this }));
                evt.menu.AppendSeparator();
            });
        }
    }
}
