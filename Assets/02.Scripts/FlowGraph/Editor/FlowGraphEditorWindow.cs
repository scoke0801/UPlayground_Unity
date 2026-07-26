using System;
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
        private ListView _traceList;
        private ListView _watchList;
        private ToolbarButton _debugRunnerButton;
        private ToolbarButton _continueButton;
        private ToolbarButton _stepButton;
        private ToolbarButton _stopButton;

        private readonly List<FlowValidationIssue> _issues = new();
        private readonly List<FlowTraceEvent> _traceItems = new();
        private readonly List<WatchRow> _watchRows = new();
        private readonly HashSet<string> _watchedVariables = new();
        private readonly List<FlowGraphSO> _breadcrumbs = new();
        private readonly List<FlowGraphSO> _forwardGraphs = new();
        private ToolbarButton _backButton;
        private ToolbarButton _forwardButton;
        private FlowNodeView _selectedNodeView;
        private FlowGraphRunner _debugRunner;
        private bool _debugRunnerPinned;
        private int _lastTraceVersion = -1;
        private double _nextDebugPollTime;

        private sealed class WatchRow
        {
            public string Name;
            public string Value;
            public long ContextId;
            public float Realtime;
        }

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/Flow Graph Editor")]
        public static void Open()
        {
            GetWindow<FlowGraphEditorWindow>("Flow Graph");
        }

        public static void OpenGraph(FlowGraphSO graph, string nodeId = null)
        {
            FlowGraphEditorWindow window = GetWindow<FlowGraphEditorWindow>("Flow Graph");
            window.Show();
            window.rootVisualElement.schedule.Execute(() =>
            {
                window.LoadGraph(graph);
                if (!string.IsNullOrEmpty(nodeId))
                    window._graphView?.SelectAndFrame(nodeId);
            });
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
            _forwardButton = new ToolbarButton(GoForwardGraph)
            {
                text = "→",
                tooltip = "다음 그래프로 이동",
                style = { display = DisplayStyle.None },
            };
            toolbar.Add(_forwardButton);

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
            toolbar.Add(new ToolbarButton(FlowGraphExplorerWindow.Open) { text = "탐색" });
            toolbar.Add(new ToolbarButton(RunGraph) { text = "▶ 실행" });
            _debugRunnerButton = new ToolbarButton(ShowDebugRunnerMenu) { text = "Runner: 자동" };
            toolbar.Add(_debugRunnerButton);
            _continueButton = new ToolbarButton(() => _debugRunner?.DebugContinue())
            {
                text = "계속",
                tooltip = "브레이크포인트에서 실행 계속",
            };
            _stepButton = new ToolbarButton(() => _debugRunner?.DebugStep())
            {
                text = "Step",
                tooltip = "현재 노드를 실행하고 다음 노드 앞에서 중단",
            };
            _stopButton = new ToolbarButton(() => _debugRunner?.DebugStop())
            {
                text = "중단",
                tooltip = "선택 Runner의 모든 FlowContext 취소",
            };
            toolbar.Add(_continueButton);
            toolbar.Add(_stepButton);
            toolbar.Add(_stopButton);
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

            var bottomTabs = new Toolbar();
            bottomTabs.Add(new ToolbarButton(() => SetBottomPanel("problems")) { text = "Problems" });
            bottomTabs.Add(new ToolbarButton(() => SetBottomPanel("trace")) { text = "Execution Trace" });
            bottomTabs.Add(new ToolbarButton(() => SetBottomPanel("watch")) { text = "Watches" });
            bottomTabs.Add(new ToolbarButton(ShowWatchMenu) { text = "Watch +" });
            bottomTabs.Add(new ToolbarButton(() =>
            {
                _debugRunner?.ClearTrace();
                _traceItems.Clear();
                _watchRows.Clear();
                _traceList?.RefreshItems();
                _watchList?.RefreshItems();
            }) { text = "Trace 지우기" });
            rootVisualElement.Add(bottomTabs);

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

            _traceList = new ListView(_traceItems, 20, MakeTraceRow, BindTraceRow)
            {
                style =
                {
                    height = 140,
                    minHeight = 40,
                    backgroundColor = new Color(0.18f, 0.18f, 0.18f),
                    display = DisplayStyle.None,
                },
                selectionType = SelectionType.Single,
            };
            _traceList.selectionChanged += _ =>
            {
                int index = _traceList.selectedIndex;
                if (index >= 0
                    && index < _traceItems.Count
                    && !string.IsNullOrEmpty(_traceItems[index].nodeId))
                {
                    _graphView.SelectAndFrame(_traceItems[index].nodeId);
                }
            };
            rootVisualElement.Add(_traceList);

            _watchList = new ListView(_watchRows, 20, MakeWatchRow, BindWatchRow)
            {
                style =
                {
                    height = 120,
                    minHeight = 40,
                    backgroundColor = new Color(0.18f, 0.18f, 0.18f),
                    display = DisplayStyle.None,
                },
                selectionType = SelectionType.None,
            };
            rootVisualElement.Add(_watchList);
        }

        private VisualElement MakeIssueRow()
        {
            var row = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingLeft = 6 },
            };
            row.Add(new Label { name = "severity", style = { width = 64, unityFontStyleAndWeight = FontStyle.Bold } });
            row.Add(new Label { name = "message", style = { flexGrow = 1 } });
            var fix = new Button { name = "fix", text = "빠른 수정" };
            fix.clicked += () =>
            {
                if (row.userData is FlowValidationIssue issue)
                    ApplyQuickFix(issue);
            };
            row.Add(fix);
            return row;
        }

        private void BindIssueRow(VisualElement row, int index)
        {
            if (index < 0 || index >= _issues.Count)
                return;

            FlowValidationIssue issue = _issues[index];
            var severity = row.Q<Label>("severity");
            var message = row.Q<Label>("message");
            var fix = row.Q<Button>("fix");
            row.userData = issue;

            (string text, Color color) = issue.Severity switch
            {
                FlowIssueSeverity.Error => ("Error", new Color(0.94f, 0.33f, 0.31f)),
                FlowIssueSeverity.Warning => ("Warning", new Color(0.95f, 0.76f, 0.20f)),
                _ => ("Info", new Color(0.45f, 0.66f, 0.95f)),
            };
            severity.text = text;
            severity.style.color = color;
            message.text = issue.Message;
            fix.style.display = issue.QuickFix == FlowQuickFix.None
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }

        private static VisualElement MakeTraceRow()
        {
            return new Label
            {
                style =
                {
                    paddingLeft = 6,
                    unityTextAlign = TextAnchor.MiddleLeft,
                },
            };
        }

        private void BindTraceRow(VisualElement row, int index)
        {
            if (index < 0 || index >= _traceItems.Count)
                return;
            FlowTraceEvent trace = _traceItems[index];
            string target = !string.IsNullOrEmpty(trace.nodeName)
                ? trace.nodeName
                : trace.graphId;
            string detail = trace.kind switch
            {
                FlowTraceKind.Emit => $" [{trace.port}] → {trace.valueSummary}",
                FlowTraceKind.BlackboardWrite => $" {trace.valueName} = {trace.valueSummary}",
                FlowTraceKind.Exception => $" {trace.valueSummary}",
                _ => string.Empty,
            };
            ((Label)row).text =
                $"{trace.sequence,5}  f{trace.frame,-5}  ctx:{trace.contextId,-3}  " +
                $"{trace.kind,-15}  {target}{detail}";
        }

        private static VisualElement MakeWatchRow()
        {
            return new Label
            {
                style =
                {
                    paddingLeft = 6,
                    unityTextAlign = TextAnchor.MiddleLeft,
                },
            };
        }

        private void BindWatchRow(VisualElement row, int index)
        {
            if (index < 0 || index >= _watchRows.Count)
                return;
            WatchRow watch = _watchRows[index];
            ((Label)row).text =
                $"{watch.Name} = {watch.Value}  ·  ctx:{watch.ContextId}  ·  t:{watch.Realtime:0.000}";
        }

        private void SetBottomPanel(string panel)
        {
            if (_validationList == null || _traceList == null || _watchList == null)
                return;
            _validationList.style.display = panel == "problems"
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            _traceList.style.display = panel == "trace"
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            _watchList.style.display = panel == "watch"
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        private void ShowWatchMenu()
        {
            var menu = new GenericMenu();
            if (_graph == null || _graph.variables.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("(Blackboard 변수 없음)"));
            }
            else
            {
                foreach (FlowVariableDef variable in _graph.variables)
                {
                    if (variable == null || string.IsNullOrEmpty(variable.name))
                        continue;
                    string variableName = variable.name;
                    menu.AddItem(
                        new GUIContent($"{variable.type}/{variableName}"),
                        _watchedVariables.Contains(variableName),
                        () =>
                        {
                            if (!_watchedVariables.Add(variableName))
                                _watchedVariables.Remove(variableName);
                            RebuildWatchRows();
                        });
                }
            }
            menu.ShowAsContext();
        }

        private void ShowDebugRunnerMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("자동 선택"), !_debugRunnerPinned, () =>
            {
                _debugRunnerPinned = false;
                _debugRunner = FindRunnerForGraph();
                RefreshDebugRunnerLabel();
            });
            menu.AddSeparator(string.Empty);

            bool found = false;
            foreach (FlowGraphRunner runner in FindObjectsByType<FlowGraphRunner>(FindObjectsSortMode.None))
            {
                if (runner == null || runner.Graph != _graph)
                    continue;
                found = true;
                FlowGraphRunner captured = runner;
                string path = GetTransformPath(runner.transform);
                menu.AddItem(new GUIContent(path), _debugRunnerPinned && _debugRunner == runner, () =>
                {
                    _debugRunnerPinned = true;
                    _debugRunner = captured;
                    _lastTraceVersion = -1;
                    RefreshDebugRunnerLabel();
                });
            }
            if (!found)
                menu.AddDisabledItem(new GUIContent("(실행 중 Runner 없음)"));
            menu.ShowAsContext();
        }

        private static string GetTransformPath(Transform transform)
        {
            if (transform == null)
                return "(Missing)";
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = $"{transform.name}/{path}";
            }
            return path;
        }

        private void RefreshDebugRunnerLabel()
        {
            if (_debugRunnerButton == null)
                return;
            _debugRunnerButton.text = _debugRunner == null
                ? "Runner: 없음"
                : $"Runner: {GetTransformPath(_debugRunner.transform)}";
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 그래프 로드/저장/실행

        private void LoadGraph(FlowGraphSO graph)
        {
            _forwardGraphs.Clear();
            LoadGraph(graph, keepBreadcrumbs: false);
        }

        private void LoadGraph(FlowGraphSO graph, bool keepBreadcrumbs)
        {
            if (!keepBreadcrumbs)
                _breadcrumbs.Clear();

            _graph = graph;
            _selectedNodeView = null;
            _debugRunner = null;
            _debugRunnerPinned = false;
            _lastTraceVersion = -1;
            _traceItems.Clear();
            _watchRows.Clear();
            _watchedVariables.Clear();

            SeedStartNodeIfEmpty(graph);

            if (_graphField != null)
                _graphField.SetValueWithoutNotify(graph);
            RefreshBreadcrumbLabel();
            RefreshDebugRunnerLabel();
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
            _forwardGraphs.Clear();
            LoadGraph(subGraph, keepBreadcrumbs: true);
        }

        private void GoBackToParentGraph()
        {
            if (_breadcrumbs.Count == 0)
                return;

            FlowGraphSO parent = _breadcrumbs[^1];
            _breadcrumbs.RemoveAt(_breadcrumbs.Count - 1);
            if (_graph != null)
                _forwardGraphs.Add(_graph);
            LoadGraph(parent, keepBreadcrumbs: true);
        }

        private void GoForwardGraph()
        {
            if (_forwardGraphs.Count == 0)
                return;

            FlowGraphSO next = _forwardGraphs[^1];
            _forwardGraphs.RemoveAt(_forwardGraphs.Count - 1);
            if (_graph != null)
                _breadcrumbs.Add(_graph);
            LoadGraph(next, keepBreadcrumbs: true);
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
            if (_forwardButton != null)
            {
                _forwardButton.style.display =
                    _forwardGraphs.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
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

        private void ApplyQuickFix(FlowValidationIssue issue)
        {
            if (_graph == null || issue.QuickFix == FlowQuickFix.None)
                return;

            Undo.RegisterCompleteObjectUndo(_graph, "FlowGraph 빠른 수정");
            switch (issue.QuickFix)
            {
                case FlowQuickFix.RemoveInvalidConnections:
                    RemoveInvalidConnections(_graph);
                    break;

                case FlowQuickFix.CreateDefaultEntry:
                    SeedStartNodeIfEmpty(_graph);
                    if (_graph.nodes.TrueForAll(node => node is not EntryNode))
                    {
                        _graph.nodes.Add(new ManualEntryNode
                        {
                            entryId = "start",
                            editorPosition = new Vector2(100, 150),
                        });
                    }
                    break;

                case FlowQuickFix.RemoveUnusedVariable:
                    _graph.variables.RemoveAll(variable =>
                        variable != null && variable.name == issue.Target);
                    break;
            }

            EditorUtility.SetDirty(_graph);
            _selectedNodeView = null;
            _graphView.PopulateView(_graph);
            _blackboardPanel.SetGraph(_graph);
            RefreshValidation();
        }

        private static void RemoveInvalidConnections(FlowGraphSO graph)
        {
            var seen = new HashSet<string>();
            graph.connections.RemoveAll(connection =>
            {
                if (connection == null)
                    return true;
                FlowNode from = graph.GetNode(connection.fromNodeId);
                FlowNode to = graph.GetNode(connection.toNodeId);
                if (from == null || to == null)
                    return true;
                if (!from.TryGetPort(
                        connection.fromPort,
                        FlowPortDirection.Output,
                        out FlowPortDef output)
                    || !to.TryGetPort(
                        connection.toPort,
                        FlowPortDirection.Input,
                        out FlowPortDef input)
                    || !FlowPortDef.AreCompatible(output, input))
                {
                    return true;
                }

                string key =
                    $"{connection.fromNodeId}\u001f{connection.fromPort}\u001f" +
                    $"{connection.toNodeId}\u001f{connection.toPort}";
                return !seen.Add(key);
            });
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

            DrawBreakpointSettings(node);

            // 디버그 (시안: Debug 섹션 — Play Mode에서 활성 토큰 수)
            if (Application.isPlaying && _debugRunner != null)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
                _debugRunner.ActiveNodeCounts.TryGetValue(node.id, out int activeCount);
                EditorGUILayout.LabelField("Active Tokens", activeCount.ToString());
            }
        }

        private void DrawBreakpointSettings(FlowNode node)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Breakpoint", EditorStyles.boldLabel);

            bool enabled = node.breakpoint;
            bool disabled = node.breakpointDisabled;
            int afterHits = node.breakpointAfterHits;
            string variableName = node.breakpointVariable ?? string.Empty;
            FlowVariableValue currentExpected = node.breakpointExpected ?? new FlowVariableValue();
            FlowVariableType expectedType = currentExpected.type;
            bool boolValue = currentExpected.boolValue;
            int intValue = currentExpected.intValue;
            float floatValue = currentExpected.floatValue;
            string stringValue = currentExpected.stringValue;

            var variableOptions = new List<string> { "(조건 없음)" };
            foreach (FlowVariableDef variable in _graph.variables)
            {
                if (variable != null && !string.IsNullOrEmpty(variable.name))
                    variableOptions.Add(variable.name);
            }
            int variableIndex = Math.Max(0, variableOptions.IndexOf(variableName));

            EditorGUI.BeginChangeCheck();
            enabled = EditorGUILayout.Toggle("설정", enabled);
            using (new EditorGUI.DisabledScope(!enabled))
            {
                disabled = EditorGUILayout.Toggle("일시 비활성", disabled);
                afterHits = Math.Max(0, EditorGUILayout.IntField("N번째 실행부터", afterHits));
                variableIndex = EditorGUILayout.Popup("조건 변수", variableIndex, variableOptions.ToArray());
                variableName = variableIndex <= 0 ? string.Empty : variableOptions[variableIndex];

                FlowVariableDef definition = null;
                foreach (FlowVariableDef variable in _graph.variables)
                {
                    if (variable != null && variable.name == variableName)
                    {
                        definition = variable;
                        expectedType = variable.type;
                        break;
                    }
                }

                if (definition != null)
                {
                    switch (expectedType)
                    {
                        case FlowVariableType.Bool:
                            boolValue = EditorGUILayout.Toggle("기대값", boolValue);
                            break;
                        case FlowVariableType.Int:
                            intValue = EditorGUILayout.IntField("기대값", intValue);
                            break;
                        case FlowVariableType.Float:
                            floatValue = EditorGUILayout.FloatField("기대값", floatValue);
                            break;
                        case FlowVariableType.String:
                            stringValue = EditorGUILayout.TextField("기대값", stringValue);
                            break;
                    }
                }
            }

            if (!EditorGUI.EndChangeCheck())
                return;

            Undo.RegisterCompleteObjectUndo(_graph, "FlowGraph 브레이크포인트 설정");
            node.breakpoint = enabled;
            node.breakpointDisabled = disabled;
            node.breakpointAfterHits = afterHits;
            node.breakpointVariable = variableName;
            node.breakpointExpected ??= new FlowVariableValue();
            node.breakpointExpected.type = expectedType;
            node.breakpointExpected.boolValue = boolValue;
            node.breakpointExpected.intValue = intValue;
            node.breakpointExpected.floatValue = floatValue;
            node.breakpointExpected.stringValue = stringValue;
            EditorUtility.SetDirty(_graph);
            _selectedNodeView.RefreshBreakpointMarker();
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
                _debugRunnerPinned = false;
                _traceItems.Clear();
                _watchRows.Clear();
                _graphView?.ClearDebugHighlight();
                RefreshDebugRunnerLabel();
            }
        }

        private void OnEditorUpdate()
        {
            if (!Application.isPlaying || _graph == null || _graphView == null)
                return;

            if (EditorApplication.timeSinceStartup < _nextDebugPollTime)
                return;
            _nextDebugPollTime = EditorApplication.timeSinceStartup + DebugPollInterval;

            if (!_debugRunnerPinned && (_debugRunner == null || _debugRunner.Graph != _graph))
                _debugRunner = FindRunnerForGraph();
            else if (_debugRunnerPinned && (_debugRunner == null || _debugRunner.Graph != _graph))
                _debugRunner = null;

            _graphView.UpdateDebugHighlight(_debugRunner);
            _blackboardPanel?.UpdateRuntimeValues(_debugRunner);
            RefreshTraceAndWatches();
            RefreshDebugRunnerLabel();
            bool paused = _debugRunner != null && _debugRunner.IsDebugPaused;
            _continueButton?.SetEnabled(paused);
            _stepButton?.SetEnabled(paused);
            _stopButton?.SetEnabled(_debugRunner != null && _debugRunner.ActiveContexts.Count > 0);
            if (paused && !string.IsNullOrEmpty(_debugRunner.PausedNodeId))
                _graphView.SelectAndFrame(_debugRunner.PausedNodeId);
            _inspector?.MarkDirtyRepaint(); // 블랙보드/토큰 수 실시간 갱신
        }

        private void RefreshTraceAndWatches()
        {
            if (_debugRunner == null)
            {
                if (_traceItems.Count > 0 || _watchRows.Count > 0)
                {
                    _traceItems.Clear();
                    _watchRows.Clear();
                    _traceList?.RefreshItems();
                    _watchList?.RefreshItems();
                }
                _lastTraceVersion = -1;
                return;
            }
            if (_lastTraceVersion == _debugRunner.TraceVersion)
                return;

            _lastTraceVersion = _debugRunner.TraceVersion;
            _debugRunner.GetTraceSnapshot(_traceItems);
            _traceList?.RefreshItems();
            RebuildWatchRows();
        }

        private void RebuildWatchRows()
        {
            _watchRows.Clear();
            var latest = new Dictionary<string, FlowTraceEvent>();
            foreach (FlowTraceEvent trace in _traceItems)
            {
                if (trace.kind == FlowTraceKind.BlackboardWrite
                    && !string.IsNullOrEmpty(trace.valueName)
                    && _watchedVariables.Contains(trace.valueName))
                {
                    latest[trace.valueName] = trace;
                }
            }
            foreach (string variableName in _watchedVariables)
            {
                if (latest.TryGetValue(variableName, out FlowTraceEvent trace))
                {
                    _watchRows.Add(new WatchRow
                    {
                        Name = variableName,
                        Value = trace.valueSummary,
                        ContextId = trace.contextId,
                        Realtime = trace.realtime,
                    });
                }
                else
                {
                    _watchRows.Add(new WatchRow
                    {
                        Name = variableName,
                        Value = "(아직 기록 없음)",
                    });
                }
            }
            _watchList?.RefreshItems();
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
