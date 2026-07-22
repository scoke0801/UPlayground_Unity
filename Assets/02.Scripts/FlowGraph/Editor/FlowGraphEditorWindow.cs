using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.FlowGraph.Editor
{
    /// <summary>
    /// FlowGraphSO 저작 창 (레이아웃은 Assets/docs/design/flow_graph_시안.png 기준).
    /// 상단 툴바 / 좌: 노드 라이브러리, 중: 그래프 캔버스, 우: 인스펙터 / 하단: 상태바 + 검증 패널.
    /// Play Mode에서는 동일 그래프를 실행 중인 러너의 활성 토큰을 하이라이트한다.
    /// </summary>
    public sealed class FlowGraphEditorWindow : EditorWindow
    {
        private const float DebugPollInterval = 0.1f;

        private FlowGraphSO _graph;
        private FlowGraphView _graphView;
        private IMGUIContainer _inspector;
        private FlowBlackboardPanel _blackboardPanel;
        private ObjectField _graphField;
        private Label _graphNameLabel;
        private Label _statusLabel;
        private Label _countsLabel;
        private ListView _validationList;

        private readonly List<FlowValidationIssue> _issues = new();
        private readonly List<FlowGraphSO> _breadcrumbs = new();
        private ToolbarButton _backButton;
        private FlowNodeView _selectedNodeView;
        private FlowGraphRunner _debugRunner;
        private double _nextDebugPollTime;

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/Flow Graph Editor")]
        public static void Open()
        {
            GetWindow<FlowGraphEditorWindow>("Flow Graph");
        }

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceId) is not FlowGraphSO graph)
                return false;

            var window = GetWindow<FlowGraphEditorWindow>("Flow Graph");
            window.LoadGraph(graph);
            return true;
        }

        private void CreateGUI()
        {
            BuildToolbar();
            BuildContent();
            BuildBottomPanel();

            if (_graph != null)
                LoadGraph(_graph);
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        // ──────────────────────────────────────────────────────────
        #region 레이아웃 구성

        private void BuildToolbar()
        {
            var toolbar = new Toolbar();

            _backButton = new ToolbarButton(GoBackToParentGraph)
            {
                text = "←",
                tooltip = "상위 그래프로 돌아가기",
                style = { display = DisplayStyle.None },
            };
            toolbar.Add(_backButton);

            _graphNameLabel = new Label("(그래프 없음)")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    unityTextAlign = TextAnchor.MiddleLeft,
                    marginLeft = 6,
                    marginRight = 6,
                },
            };
            toolbar.Add(_graphNameLabel);

            _graphField = new ObjectField
            {
                objectType = typeof(FlowGraphSO),
                allowSceneObjects = false,
                style = { width = 200, minWidth = 90, flexShrink = 1 },
            };
            _graphField.RegisterValueChangedCallback(evt => LoadGraph(evt.newValue as FlowGraphSO));
            toolbar.Add(_graphField);

            toolbar.Add(new ToolbarButton(ShowOpenGraphMenu) { text = "열기 ▾" });
            toolbar.Add(new ToolbarButton(CreateNewGraph) { text = "새 그래프" });
            toolbar.Add(new ToolbarButton(SaveGraph) { text = "저장" });
            toolbar.Add(new ToolbarButton(RefreshValidation) { text = "검증" });
            toolbar.Add(new ToolbarButton(RunGraph) { text = "▶ 실행" });
            var compactToggle = new ToolbarToggle { text = "컴팩트", tooltip = "노드 본문 요약 일괄 숨김" };
            compactToggle.RegisterValueChangedCallback(evt => _graphView?.SetCompactMode(evt.newValue));
            toolbar.Add(compactToggle);
            toolbar.Add(new ToolbarSpacer());
            toolbar.Add(new ToolbarButton(Undo.PerformUndo) { text = "↩" });
            toolbar.Add(new ToolbarButton(Undo.PerformRedo) { text = "↪" });

            rootVisualElement.Add(toolbar);
        }

        private void BuildContent()
        {
            // minHeight 0 — 자식 최소 크기 때문에 세로 수축이 막혀 하단 패널과 겹치는 것을 방지
            var content = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexGrow = 1,
                    minHeight = 120,
                    overflow = Overflow.Hidden,
                },
            };

            _graphView = new FlowGraphView(OnNodeSelected);
            _graphView.SetupSearchWindow(this);
            _graphView.GraphMutated += OnGraphMutated;
            _graphView.SubGraphOpenRequested += OpenSubGraph;

            // 좌측 컬럼: 노드 라이브러리(상) + 블랙보드 변수 패널(하)
            var leftColumn = new VisualElement
            {
                style =
                {
                    width = 220,
                    flexShrink = 0,
                    minHeight = 0,
                    overflow = Overflow.Hidden,
                    borderRightWidth = 1,
                    borderRightColor = new Color(0.1f, 0.1f, 0.1f),
                },
            };
            var library = new FlowNodeLibraryPanel(type => _graphView.CreateNodeAtViewCenter(type));
            library.style.width = StyleKeyword.Auto;
            library.style.borderRightWidth = 0;
            library.style.flexGrow = 1;
            library.style.minHeight = 0;
            leftColumn.Add(library);
            _blackboardPanel = new FlowBlackboardPanel(OnBlackboardChanged, nodeId => _graphView.SelectAndFrame(nodeId));
            _blackboardPanel.style.flexShrink = 1;   // 작은 창에서는 라이브러리와 함께 수축
            _blackboardPanel.style.minHeight = 60;
            leftColumn.Add(_blackboardPanel);
            content.Add(leftColumn);

            var split = new TwoPaneSplitView(1, 320f, TwoPaneSplitViewOrientation.Horizontal);
            split.Add(_graphView);
            _inspector = new IMGUIContainer(DrawInspector)
            {
                style = { paddingLeft = 6, paddingRight = 6, paddingTop = 6 },
            };
            var inspectorScroll = new ScrollView { style = { minWidth = 200 } };
            inspectorScroll.Add(_inspector);
            split.Add(inspectorScroll);
            content.Add(split);

            rootVisualElement.Add(content);
        }

        private void BuildBottomPanel()
        {
            // 상태바 (시안: Valid · Nodes · Connections)
            var statusBar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    height = 22,
                    flexShrink = 0,
                    paddingLeft = 8,
                    borderTopWidth = 1,
                    borderTopColor = new Color(0.1f, 0.1f, 0.1f),
                    // 불투명 배경 — 위 콘텐츠가 겹쳐도 비쳐 보이지 않게
                    backgroundColor = new Color(0.16f, 0.16f, 0.16f),
                },
            };
            _statusLabel = new Label("—") { style = { marginRight = 16 } };
            _countsLabel = new Label(string.Empty);
            statusBar.Add(_statusLabel);
            statusBar.Add(_countsLabel);
            rootVisualElement.Add(statusBar);

            // 검증 패널 (시안: Validation 탭 — 행 클릭 시 해당 노드 포커스)
            _validationList = new ListView(_issues, 20, MakeIssueRow, BindIssueRow)
            {
                style =
                {
                    height = 96,
                    minHeight = 40,
                    flexShrink = 1, // 작은 창에서는 검증 패널이 먼저 수축
                    backgroundColor = new Color(0.18f, 0.18f, 0.18f),
                },
                selectionType = SelectionType.Single,
            };
            _validationList.selectionChanged += _ =>
            {
                if (_validationList.selectedIndex >= 0
                    && _validationList.selectedIndex < _issues.Count
                    && !string.IsNullOrEmpty(_issues[_validationList.selectedIndex].NodeId))
                {
                    _graphView.SelectAndFrame(_issues[_validationList.selectedIndex].NodeId);
                }
            };
            rootVisualElement.Add(_validationList);
        }

        private static VisualElement MakeIssueRow()
        {
            var row = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingLeft = 6 },
            };
            row.Add(new Label { name = "severity", style = { width = 64, unityFontStyleAndWeight = FontStyle.Bold } });
            row.Add(new Label { name = "message" });
            return row;
        }

        private void BindIssueRow(VisualElement row, int index)
        {
            if (index < 0 || index >= _issues.Count)
                return;

            FlowValidationIssue issue = _issues[index];
            var severity = row.Q<Label>("severity");
            var message = row.Q<Label>("message");

            (string text, Color color) = issue.Severity switch
            {
                FlowIssueSeverity.Error => ("Error", new Color(0.94f, 0.33f, 0.31f)),
                FlowIssueSeverity.Warning => ("Warning", new Color(0.95f, 0.76f, 0.20f)),
                _ => ("Info", new Color(0.45f, 0.66f, 0.95f)),
            };
            severity.text = text;
            severity.style.color = color;
            message.text = issue.Message;
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 그래프 로드/저장/실행

        private void LoadGraph(FlowGraphSO graph) => LoadGraph(graph, keepBreadcrumbs: false);

        private void LoadGraph(FlowGraphSO graph, bool keepBreadcrumbs)
        {
            if (!keepBreadcrumbs)
                _breadcrumbs.Clear();

            _graph = graph;
            _selectedNodeView = null;
            _debugRunner = null;

            SeedStartNodeIfEmpty(graph);

            if (_graphField != null)
                _graphField.SetValueWithoutNotify(graph);
            RefreshBreadcrumbLabel();
            _graphView?.PopulateView(graph);
            _blackboardPanel?.SetGraph(graph);
            RefreshValidation();
        }

        /// <summary>서브그래프 노드 더블클릭 진입 — 현재 그래프를 브레드크럼에 쌓는다.</summary>
        private void OpenSubGraph(FlowGraphSO subGraph)
        {
            if (subGraph == null || subGraph == _graph)
                return;

            if (_graph != null)
                _breadcrumbs.Add(_graph);
            LoadGraph(subGraph, keepBreadcrumbs: true);
        }

        private void GoBackToParentGraph()
        {
            if (_breadcrumbs.Count == 0)
                return;

            FlowGraphSO parent = _breadcrumbs[^1];
            _breadcrumbs.RemoveAt(_breadcrumbs.Count - 1);
            LoadGraph(parent, keepBreadcrumbs: true);
        }

        private void RefreshBreadcrumbLabel()
        {
            if (_graphNameLabel == null)
                return;

            if (_graph == null)
            {
                _graphNameLabel.text = "(그래프 없음)";
            }
            else if (_breadcrumbs.Count == 0)
            {
                _graphNameLabel.text = _graph.ResolvedGraphId;
            }
            else
            {
                var path = new System.Text.StringBuilder();
                foreach (FlowGraphSO crumb in _breadcrumbs)
                    path.Append(crumb.ResolvedGraphId).Append(" ▸ ");
                path.Append(_graph.ResolvedGraphId);
                _graphNameLabel.text = path.ToString();
            }

            if (_backButton != null)
            {
                _backButton.style.display =
                    _breadcrumbs.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>빈 그래프에는 시작(Manual 진입점) 노드를 자동 생성한다.</summary>
        private static void SeedStartNodeIfEmpty(FlowGraphSO graph)
        {
            if (graph == null || graph.nodes.Count > 0)
                return;

            graph.nodes.Add(new ManualEntryNode
            {
                entryId = "start",
                editorPosition = new Vector2(100, 150),
            });
            EditorUtility.SetDirty(graph);
        }

        /// <summary>새 FlowGraph 에셋을 생성하고 즉시 연다 (시작 노드 포함).</summary>
        private void CreateNewGraph()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "새 FlowGraph 생성", "FLOW_NewGraph", "asset", "생성할 위치를 선택하세요.");
            if (string.IsNullOrEmpty(path))
                return;

            var graph = CreateInstance<FlowGraphSO>();
            SeedStartNodeIfEmpty(graph);
            AssetDatabase.CreateAsset(graph, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(graph);
            LoadGraph(graph);
        }

        /// <summary>프로젝트의 모든 FlowGraphSO를 드롭다운으로 나열해 바로 전환한다.</summary>
        private void ShowOpenGraphMenu()
        {
            var menu = new GenericMenu();
            string[] guids = AssetDatabase.FindAssets("t:FlowGraphSO");
            if (guids.Length == 0)
                menu.AddDisabledItem(new GUIContent("(FlowGraph 에셋 없음)"));

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<FlowGraphSO>(path);
                if (asset == null)
                    continue;

                bool isCurrent = asset == _graph;
                menu.AddItem(new GUIContent(asset.name), isCurrent, () => LoadGraph(asset));
            }
            menu.ShowAsContext();
        }

        private void SaveGraph()
        {
            if (_graph == null)
                return;

            RefreshValidation();
            EditorUtility.SetDirty(_graph);
            AssetDatabase.SaveAssetIfDirty(_graph);
        }

        /// <summary>Play Mode에서 현재 그래프 러너의 Manual 진입점을 발화한다 (시안: ▶ 실행).</summary>
        private void RunGraph()
        {
            if (_graph == null)
                return;

            if (!Application.isPlaying)
            {
                ShowNotification(new GUIContent("실행은 Play Mode에서만 가능합니다."));
                return;
            }

            FlowGraphRunner runner = FindRunnerForGraph();
            if (runner == null)
            {
                ShowNotification(new GUIContent("이 그래프를 실행 중인 FlowGraphRunner가 씬에 없습니다."));
                return;
            }

            if (!runner.FireManualEntries(null))
                ShowNotification(new GUIContent("발화된 Manual 진입점이 없습니다 (재진입 정책 확인)."));
        }

        private void OnGraphMutated() => RefreshValidation();

        private void OnBlackboardChanged()
        {
            _graphView?.RefreshNodeContents();
            RefreshValidation();
        }

        private void OnNodeSelected(FlowNodeView nodeView) => _selectedNodeView = nodeView;

        private void OnUndoRedo()
        {
            if (_graph == null)
                return;
            _selectedNodeView = null;
            _graphView?.PopulateView(_graph);
            _blackboardPanel?.SetGraph(_graph);
            RefreshValidation();
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 검증/상태바

        private void RefreshValidation()
        {
            _issues.Clear();
            if (_graph != null)
                _issues.AddRange(FlowGraphValidator.Validate(_graph));
            _validationList?.RefreshItems();

            if (_statusLabel == null)
                return;

            if (_graph == null)
            {
                _statusLabel.text = "—";
                _countsLabel.text = string.Empty;
                return;
            }

            int errors = 0;
            int warnings = 0;
            foreach (FlowValidationIssue issue in _issues)
            {
                if (issue.Severity == FlowIssueSeverity.Error) errors++;
                else if (issue.Severity == FlowIssueSeverity.Warning) warnings++;
            }

            if (errors > 0)
            {
                _statusLabel.text = $"✕ Error {errors} · Warning {warnings}";
                _statusLabel.style.color = new Color(0.94f, 0.33f, 0.31f);
            }
            else if (warnings > 0)
            {
                _statusLabel.text = $"⚠ Warning {warnings}";
                _statusLabel.style.color = new Color(0.95f, 0.76f, 0.20f);
            }
            else
            {
                _statusLabel.text = "✓ Valid";
                _statusLabel.style.color = new Color(0.35f, 0.80f, 0.42f);
            }

            _countsLabel.text = $"Nodes {_graph.nodes.Count}  ·  Connections {_graph.connections.Count}";

            UpdateNodeBadges();
        }

        /// <summary>검증 결과를 노드 우상단 배지로도 표시 — 캔버스만 보고 문제 노드를 찾을 수 있게.</summary>
        private void UpdateNodeBadges()
        {
            if (_graphView == null)
                return;

            var worstByNode = new Dictionary<string, FlowIssueSeverity>();
            foreach (FlowValidationIssue issue in _issues)
            {
                if (string.IsNullOrEmpty(issue.NodeId))
                    continue;
                // enum 값이 낮을수록 심각 (Error=0 < Warning=1)
                if (!worstByNode.TryGetValue(issue.NodeId, out FlowIssueSeverity worst)
                    || issue.Severity < worst)
                {
                    worstByNode[issue.NodeId] = issue.Severity;
                }
            }

            foreach (UnityEditor.Experimental.GraphView.Node node in _graphView.nodes)
            {
                if (node is FlowNodeView view)
                {
                    view.SetValidationBadge(
                        worstByNode.TryGetValue(view.FlowNode.id, out FlowIssueSeverity severity)
                            ? severity
                            : (FlowIssueSeverity?)null);
                }
            }
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 인스펙터

        private void DrawInspector()
        {
            if (_graph == null)
            {
                EditorGUILayout.HelpBox("FlowGraph 에셋을 선택하세요.", MessageType.Info);
                return;
            }

            DrawNodeSection();
            DrawBlackboardSection();
        }

        private void DrawNodeSection()
        {
            if (_selectedNodeView == null || _selectedNodeView.FlowNode == null)
            {
                EditorGUILayout.HelpBox("노드를 선택하면 속성이 표시됩니다.", MessageType.None);
                return;
            }

            FlowNode node = _selectedNodeView.FlowNode;
            int index = _graph.nodes.IndexOf(node);
            if (index < 0)
                return;

            // 헤더 (시안: Inspector — 타이틀 + 타입 + Node ID)
            EditorGUILayout.LabelField(node.DisplayName, EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.LabelField("Type", node.GetType().Name);
                EditorGUILayout.LabelField("Node ID", node.id);
            }
            EditorGUILayout.Space(4);

            var serialized = new SerializedObject(_graph);
            SerializedProperty nodeProp = serialized
                .FindProperty("nodes")
                .GetArrayElementAtIndex(index);

            EditorGUI.BeginChangeCheck();

            SerializedProperty iterator = nodeProp.Copy();
            SerializedProperty end = nodeProp.GetEndProperty();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
            {
                enterChildren = false;
                EditorGUILayout.PropertyField(iterator, true);
            }

            if (EditorGUI.EndChangeCheck())
            {
                serialized.ApplyModifiedProperties();
                _selectedNodeView.RefreshTitle();
                _selectedNodeView.RebuildSummary();
                RefreshValidation();
            }

            // 디버그 (시안: Debug 섹션 — Play Mode에서 활성 토큰 수)
            if (Application.isPlaying && _debugRunner != null)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
                _debugRunner.ActiveNodeCounts.TryGetValue(node.id, out int activeCount);
                EditorGUILayout.LabelField("Active Tokens", activeCount.ToString());
            }
        }

        /// <summary>실행 중 플로우 컨텍스트의 블랙보드를 표시한다. 블랙보드는 발화 스코프 런타임 데이터다.</summary>
        private void DrawBlackboardSection()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Blackboard (실행 컨텍스트)", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "블랙보드는 발화마다 생성되는 런타임 데이터입니다. Play Mode에서 실행 중 플로우의 내용이 표시됩니다.",
                    MessageType.None);
                return;
            }

            if (_debugRunner == null)
            {
                EditorGUILayout.LabelField("이 그래프를 실행 중인 러너 없음", EditorStyles.miniLabel);
                return;
            }

            IReadOnlyList<FlowContext> contexts = _debugRunner.ActiveContexts;
            if (contexts.Count == 0)
            {
                EditorGUILayout.LabelField("실행 중인 플로우 없음", EditorStyles.miniLabel);
                return;
            }

            for (int i = 0; i < contexts.Count; i++)
            {
                FlowContext context = contexts[i];
                string entryName = context.Entry != null ? context.Entry.DisplayName : "(알 수 없음)";
                EditorGUILayout.LabelField(
                    $"#{i}  {entryName}  ·  토큰 {context.ActiveTokenCount}  ·  깊이 {context.Depth}",
                    EditorStyles.boldLabel);

                using (new EditorGUI.IndentLevelScope())
                {
                    bool empty = true;
                    foreach (KeyValuePair<string, object> pair in context.BlackboardEntries)
                    {
                        empty = false;
                        EditorGUILayout.LabelField(pair.Key, pair.Value?.ToString() ?? "null");
                    }
                    if (empty)
                        EditorGUILayout.LabelField("(블랙보드 비어 있음)", EditorStyles.miniLabel);
                }
            }
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 런타임 디버그 하이라이트

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                _debugRunner = null;
                _graphView?.ClearDebugHighlight();
            }
        }

        private void OnEditorUpdate()
        {
            if (!Application.isPlaying || _graph == null || _graphView == null)
                return;

            if (EditorApplication.timeSinceStartup < _nextDebugPollTime)
                return;
            _nextDebugPollTime = EditorApplication.timeSinceStartup + DebugPollInterval;

            if (_debugRunner == null || _debugRunner.Graph != _graph)
                _debugRunner = FindRunnerForGraph();

            _graphView.UpdateDebugHighlight(_debugRunner);
            _blackboardPanel?.UpdateRuntimeValues(_debugRunner);
            _inspector?.MarkDirtyRepaint(); // 블랙보드/토큰 수 실시간 갱신
        }

        private FlowGraphRunner FindRunnerForGraph()
        {
            FlowGraphRunner[] runners = FindObjectsByType<FlowGraphRunner>(FindObjectsSortMode.None);
            foreach (FlowGraphRunner runner in runners)
            {
                if (runner.Graph == _graph)
                    return runner;
            }
            return null;
        }

        #endregion
    }
}
