#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public class BehaviorTreeEditorWindow : EditorWindow
    {
        private const double DebugRefreshInterval = 0.05d;
        private const int BreadcrumbMaxDepth = 12;

        private BehaviorTreeAsset _tree;
        private BehaviorTreeRunner _debugRunner;
        private BehaviorTreeGraphView _graphView;
        private BehaviorTreeInspectorView _inspectorView;
        private BehaviorTreeBlackboardView _blackboardView;
        private VisualElement _validationBox;
        private VisualElement _propertyContent;
        private VisualElement _inspectorPanel;
        private VisualElement _variablesPanel;
        private VisualElement _errorsPanel;
        private VisualElement _tracePanel;
        private VisualElement _searchPanel;
        private VisualElement _traceBox;
        private ToolbarToggle _inspectorTab;
        private ToolbarToggle _variablesTab;
        private ToolbarToggle _errorsTab;
        private ToolbarToggle _traceTab;
        private ToolbarToggle _searchTab;
        private Label _errorCountLabel;
        private Label _debugStateLabel;
        private Label _graphTitleLabel;
        private Label _graphSubtitleLabel;
        private Label _runtimeBanner;
        private VisualElement _breadcrumbBar;
        private BehaviorTreeSearchPanel _searchPanelView;
        private BehaviorTreeMiniMapView _miniMapView;
        private ToolbarToggle _miniMapToggle;
        private ObjectField _treeField;
        private ObjectField _runnerField;
        private string _lastFocusedPauseGuid;
        private double _nextDebugRefreshTime;
        private int _lastTraceVersion = -1;
        private int _lastTraceTick = -1;
        private int _lastTraceViewVersion = -1;
        private int _lastBreadcrumbTick = int.MinValue;
        private BehaviorTreeRunnerState _lastDebugState = (BehaviorTreeRunnerState)(-1);
        private BTStatus _lastExecutionStatus = (BTStatus)(-1);
        private bool _lastDebugMode;
        private bool _debugGraphWasActive;
        private bool _autoDetectedRunner;
        private PropertyTab _activeTab = PropertyTab.Inspector;

        private enum PropertyTab
        {
            Inspector,
            Variables,
            Errors,
            Trace,
            Search
        }

        [MenuItem("UPlayGround/Character/AI/Behavior Tree Editor")]
        public static void Open()
        {
            var window = GetWindow<BehaviorTreeEditorWindow>();
            window.titleContent = new GUIContent("Behavior Tree");
            window.minSize = new Vector2(900f, 560f);
            window.Show();
        }

        public static void Open(BehaviorTreeAsset tree)
        {
            Open();
            var window = GetWindow<BehaviorTreeEditorWindow>();
            window.SetTree(tree);
        }

        private void OnEnable()
        {
            ConstructLayout();
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;

            if (_tree == null && Selection.activeObject is BehaviorTreeAsset selectedTree)
                SetTree(selectedTree);

            TryAutoDetectRunner();
        }

        private void OnDisable()
        {
            _graphView?.FlushPendingSave();
            Selection.selectionChanged -= OnSelectionChanged;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnHierarchyChanged()
        {
            TryAutoDetectRunner();
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
                TryAutoDetectRunner();
        }

        private void TryAutoDetectRunner()
        {
            if (_debugRunner != null || _tree == null)
                return;

            BehaviorTreeRunner candidate = null;
            var runners = UnityEngine.Object.FindObjectsByType<BehaviorTreeRunner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var runner in runners)
            {
                if (runner == null || runner.SourceTree != _tree)
                    continue;

                candidate = runner;
                if (Selection.activeGameObject == runner.gameObject)
                    break;
            }

            if (candidate == null)
                return;

            _autoDetectedRunner = true;
            _debugRunner = candidate;
            _runnerField?.SetValueWithoutNotify(candidate);
            ResetDebugUiCache();
            _blackboardView?.SetDebugRunner(_debugRunner);
            RefreshDebugState();
        }

        private void ConstructLayout()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.backgroundColor = BehaviorTreeEditorStyles.Background;

            var toolbarRoot = new VisualElement();
            toolbarRoot.style.backgroundColor = BehaviorTreeEditorStyles.PanelAlt;
            rootVisualElement.Add(toolbarRoot);

            var operationsToolbar = new Toolbar();
            operationsToolbar.style.height = 44f;
            operationsToolbar.style.paddingLeft = 8f;
            operationsToolbar.style.paddingRight = 8f;
            operationsToolbar.style.backgroundColor = BehaviorTreeEditorStyles.PanelAlt;

            _treeField = new ObjectField
            {
                objectType = typeof(BehaviorTreeAsset),
                allowSceneObjects = false,
                value = _tree
            };
            _treeField.style.width = 260f;
            _treeField.RegisterValueChangedCallback(evt => SetTree(evt.newValue as BehaviorTreeAsset));
            operationsToolbar.Add(CreateToolbarLabel("Behavior Tree"));
            operationsToolbar.Add(_treeField);

            operationsToolbar.Add(CreateToolbarButton("Save", SaveTree, ToolbarButtonStyle.Primary));
            operationsToolbar.Add(CreateToolbarButton("New", CreateTreeAsset, ToolbarButtonStyle.Ghost));
            operationsToolbar.Add(CreateToolbarSeparator());
            operationsToolbar.Add(CreateToolbarButton("Import", BehaviorTreeJsonUtility.ImportJson, ToolbarButtonStyle.Ghost));
            operationsToolbar.Add(CreateToolbarButton("Export", BehaviorTreeJsonUtility.ExportSelected, ToolbarButtonStyle.Ghost));
            operationsToolbar.Add(CreateToolbarButton("Validate", ValidateTree, ToolbarButtonStyle.Ghost));
            operationsToolbar.Add(CreateToolbarButton("Fit All", () => _graphView?.FrameAllNodes(), ToolbarButtonStyle.Ghost));
            operationsToolbar.Add(CreateToolbarSeparator());
            operationsToolbar.Add(CreateToolbarButton("Clean Nulls", CleanNullReferences, ToolbarButtonStyle.Danger));

            operationsToolbar.Add(new ToolbarSpacer { style = { flexGrow = 1 } });

            _errorCountLabel = CreateStatusBadge("Errors 0", new Color(0.30f, 0.30f, 0.30f));
            operationsToolbar.Add(_errorCountLabel);
            _miniMapToggle = new ToolbarToggle { text = "Minimap" };
            _miniMapToggle.value = true;
            StyleToolbarToggle(_miniMapToggle, true);
            _miniMapToggle.RegisterValueChangedCallback(evt =>
            {
                StyleToolbarToggle(_miniMapToggle, evt.newValue);
                if (_miniMapView != null)
                    _miniMapView.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            });
            operationsToolbar.Add(_miniMapToggle);
            toolbarRoot.Add(operationsToolbar);

            var debugToolbar = new Toolbar();
            debugToolbar.style.height = 36f;
            debugToolbar.style.paddingLeft = 8f;
            debugToolbar.style.paddingRight = 8f;
            debugToolbar.style.backgroundColor = BehaviorTreeEditorStyles.Background;

            _runnerField = new ObjectField
            {
                objectType = typeof(BehaviorTreeRunner),
                allowSceneObjects = true,
                value = _debugRunner
            };
            _runnerField.style.width = 260f;
            _runnerField.RegisterValueChangedCallback(evt =>
            {
                _debugRunner = evt.newValue as BehaviorTreeRunner;
                _autoDetectedRunner = false;
                ResetDebugUiCache();
                _blackboardView?.SetDebugRunner(_debugRunner);
                RefreshDebugState();
            });
            debugToolbar.Add(CreateToolbarLabel("Debug Runner"));
            debugToolbar.Add(_runnerField);
            debugToolbar.Add(CreateToolbarButton("Play", () => _debugRunner?.EnableBehavior(), ToolbarButtonStyle.Success));
            debugToolbar.Add(CreateToolbarButton("Pause", () => _debugRunner?.PauseTree(), ToolbarButtonStyle.Ghost));
            debugToolbar.Add(CreateToolbarButton("Step", () => _debugRunner?.StepTick(), ToolbarButtonStyle.Ghost));
            debugToolbar.Add(CreateToolbarButton("Stop", () => _debugRunner?.StopTree(), ToolbarButtonStyle.Danger));
            debugToolbar.Add(new ToolbarSpacer { style = { flexGrow = 1 } });
            _debugStateLabel = CreateStatusBadge("Stopped", new Color(0.30f, 0.30f, 0.30f));
            debugToolbar.Add(_debugStateLabel);
            toolbarRoot.Add(debugToolbar);

            var content = new TwoPaneSplitView(0, 330, TwoPaneSplitViewOrientation.Horizontal);
            content.style.flexGrow = 1;
            rootVisualElement.Add(content);

            var sidePanel = CreateSidePanel();
            content.Add(sidePanel);

            var graphShell = CreateGraphShell();
            content.Add(graphShell);

            _graphView = new BehaviorTreeGraphView(this);
            graphShell.Add(_graphView);

            var graphHeader = CreateGraphHeader();
            graphShell.Add(graphHeader);
            _runtimeBanner = CreateRuntimeBanner();
            graphShell.Add(_runtimeBanner);
            _breadcrumbBar = CreateBreadcrumbBar();
            graphShell.Add(_breadcrumbBar);
            _miniMapView = new BehaviorTreeMiniMapView(_graphView);
            graphShell.Add(_miniMapView);

            sidePanel.Add(CreatePropertiesPanel());

            _graphView.PopulateView(_tree);
            _blackboardView.Bind(_tree);
            _blackboardView.SetDebugRunner(_debugRunner);
            RefreshGraphTitle();
            RefreshDebugState();
            ValidateTree();
        }

        private static VisualElement CreateSidePanel()
        {
            var sidePanel = new VisualElement();
            sidePanel.style.flexGrow = 1;
            sidePanel.style.minWidth = 280f;
            sidePanel.style.backgroundColor = new Color(0.075f, 0.075f, 0.10f);
            sidePanel.style.borderRightColor = new Color(0.18f, 0.18f, 0.22f);
            sidePanel.style.borderRightWidth = 1f;
            return sidePanel;
        }

        private static VisualElement CreateGraphShell()
        {
            var graphShell = new VisualElement();
            graphShell.style.flexGrow = 1;
            graphShell.style.position = Position.Relative;
            return graphShell;
        }

        private VisualElement CreateGraphHeader()
        {
            var header = new VisualElement();
            header.pickingMode = PickingMode.Ignore;
            header.style.position = Position.Absolute;
            header.style.left = 14f;
            header.style.top = 12f;
            header.style.paddingLeft = 12f;
            header.style.paddingRight = 12f;
            header.style.paddingTop = 8f;
            header.style.paddingBottom = 8f;
            header.style.backgroundColor = new Color(0.07f, 0.07f, 0.09f, 0.88f);
            header.style.borderTopLeftRadius = 8f;
            header.style.borderTopRightRadius = 8f;
            header.style.borderBottomLeftRadius = 8f;
            header.style.borderBottomRightRadius = 8f;
            header.style.borderLeftColor = new Color(0.34f, 0.48f, 0.86f);
            header.style.borderLeftWidth = 2f;

            _graphTitleLabel = new Label();
            _graphTitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _graphTitleLabel.style.fontSize = 14f;
            _graphTitleLabel.style.color = new Color(0.88f, 0.88f, 0.88f);
            header.Add(_graphTitleLabel);

            _graphSubtitleLabel = new Label();
            _graphSubtitleLabel.style.fontSize = 10f;
            _graphSubtitleLabel.style.color = new Color(0.55f, 0.68f, 0.78f);
            header.Add(_graphSubtitleLabel);

            return header;
        }

        private VisualElement CreateBreadcrumbBar()
        {
            var bar = new VisualElement();
            // 빈 영역은 GraphView로 통과되어야 SelectionDragger의 MouseUp이 정상 동작한다.
            // 내부 라벨은 default PickingMode.Position을 그대로 사용해 클릭 가능하다.
            bar.pickingMode = PickingMode.Ignore;
            bar.style.position = Position.Absolute;
            bar.style.left = 14f;
            bar.style.bottom = 12f;
            bar.style.right = 200f;
            bar.style.height = 26f;
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.alignItems = Align.Center;
            bar.style.paddingLeft = 10f;
            bar.style.paddingRight = 10f;
            bar.style.backgroundColor = new Color(0.07f, 0.07f, 0.09f, 0.92f);
            bar.style.borderTopLeftRadius = 6f;
            bar.style.borderTopRightRadius = 6f;
            bar.style.borderBottomLeftRadius = 6f;
            bar.style.borderBottomRightRadius = 6f;
            bar.style.borderLeftColor = new Color(0.36f, 0.95f, 0.52f);
            bar.style.borderLeftWidth = 2f;
            bar.style.display = DisplayStyle.None;
            return bar;
        }

        private void UpdateBreadcrumb(bool debugActive)
        {
            if (_breadcrumbBar == null)
                return;

            if (!debugActive || _debugRunner?.RuntimeTree?.RootNode == null)
            {
                _breadcrumbBar.style.display = DisplayStyle.None;
                _lastBreadcrumbTick = int.MinValue;
                return;
            }

            var tickKey = _debugRunner.DebugTrace?.CurrentTick ?? 0;
            if (tickKey == _lastBreadcrumbTick && _breadcrumbBar.style.display == DisplayStyle.Flex)
                return;

            _lastBreadcrumbTick = tickKey;

            var runtimePath = BuildBreadcrumbPath(_debugRunner.RuntimeTree.RootNode);
            _breadcrumbBar.Clear();

            if (runtimePath.Count == 0)
            {
                _breadcrumbBar.style.display = DisplayStyle.None;
                return;
            }

            _breadcrumbBar.style.display = DisplayStyle.Flex;
            for (var i = 0; i < runtimePath.Count; i++)
            {
                var node = runtimePath[i];
                var label = new Label(node.DisplayName)
                {
                    tooltip = $"{node.GetType().Name}\nGuid: {node.Guid}"
                };
                label.style.color = i == runtimePath.Count - 1
                    ? new Color(0.48f, 0.96f, 0.56f)
                    : new Color(0.78f, 0.85f, 0.92f);
                label.style.unityFontStyleAndWeight = i == runtimePath.Count - 1 ? FontStyle.Bold : FontStyle.Normal;
                label.style.fontSize = 11f;
                label.style.paddingLeft = 4f;
                label.style.paddingRight = 4f;
                var guid = node.Guid;
                label.RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.button != 0)
                        return;
                    _graphView?.FocusNodeByGuid(guid);
                    evt.StopPropagation();
                });
                _breadcrumbBar.Add(label);

                if (i < runtimePath.Count - 1)
                {
                    var sep = new Label("›");
                    sep.style.color = new Color(0.40f, 0.40f, 0.50f);
                    sep.style.unityFontStyleAndWeight = FontStyle.Bold;
                    sep.style.fontSize = 13f;
                    sep.style.paddingLeft = 2f;
                    sep.style.paddingRight = 2f;
                    _breadcrumbBar.Add(sep);
                }
            }
        }

        private static List<BTNode> BuildBreadcrumbPath(BTNode root)
        {
            var path = new List<BTNode>();
            var current = root;
            var depth = 0;
            while (current != null && current.IsRunning && depth < BreadcrumbMaxDepth)
            {
                path.Add(current);
                BTNode nextRunning = null;
                foreach (var child in current.Children)
                {
                    if (child == null || !child.IsRunning)
                        continue;
                    nextRunning = child;
                    break;
                }

                if (nextRunning == null)
                    break;
                current = nextRunning;
                depth++;
            }

            return path;
        }

        private static Label CreateRuntimeBanner()
        {
            var banner = new Label("RUNTIME MODE");
            banner.pickingMode = PickingMode.Ignore;
            banner.style.position = Position.Absolute;
            banner.style.right = 16f;
            banner.style.top = 12f;
            banner.style.height = 24f;
            banner.style.paddingLeft = 12f;
            banner.style.paddingRight = 12f;
            banner.style.unityTextAlign = TextAnchor.MiddleCenter;
            banner.style.unityFontStyleAndWeight = FontStyle.Bold;
            banner.style.fontSize = 10f;
            banner.style.color = new Color(0.48f, 0.96f, 0.56f);
            banner.style.backgroundColor = new Color(0.05f, 0.18f, 0.09f, 0.92f);
            banner.style.borderTopColor = new Color(0.22f, 0.58f, 0.28f);
            banner.style.borderRightColor = new Color(0.22f, 0.58f, 0.28f);
            banner.style.borderBottomColor = new Color(0.22f, 0.58f, 0.28f);
            banner.style.borderLeftColor = new Color(0.22f, 0.58f, 0.28f);
            banner.style.borderTopWidth = 1f;
            banner.style.borderRightWidth = 1f;
            banner.style.borderBottomWidth = 1f;
            banner.style.borderLeftWidth = 1f;
            banner.style.borderTopLeftRadius = 6f;
            banner.style.borderTopRightRadius = 6f;
            banner.style.borderBottomLeftRadius = 6f;
            banner.style.borderBottomRightRadius = 6f;
            banner.style.display = DisplayStyle.None;
            return banner;
        }

        private VisualElement CreatePropertiesPanel()
        {
            var panel = new VisualElement();
            panel.style.flexGrow = 1;
            panel.style.marginLeft = 6f;
            panel.style.marginRight = 6f;
            panel.style.marginTop = 6f;
            panel.style.marginBottom = 6f;
            panel.style.backgroundColor = new Color(0.075f, 0.075f, 0.10f);
            panel.style.borderTopColor = new Color(0.18f, 0.18f, 0.22f);
            panel.style.borderRightColor = new Color(0.18f, 0.18f, 0.22f);
            panel.style.borderBottomColor = new Color(0.18f, 0.18f, 0.22f);
            panel.style.borderLeftColor = new Color(0.18f, 0.18f, 0.22f);
            panel.style.borderTopWidth = 1f;
            panel.style.borderRightWidth = 1f;
            panel.style.borderBottomWidth = 1f;
            panel.style.borderLeftWidth = 1f;
            panel.style.borderTopLeftRadius = 8f;
            panel.style.borderTopRightRadius = 8f;
            panel.style.borderBottomLeftRadius = 8f;
            panel.style.borderBottomRightRadius = 8f;

            var tabs = new Toolbar();
            tabs.style.height = 34f;
            tabs.style.backgroundColor = new Color(0.09f, 0.09f, 0.11f);

            _inspectorTab = CreatePropertyTab("Inspector", PropertyTab.Inspector);
            _variablesTab = CreatePropertyTab("Variables", PropertyTab.Variables);
            _errorsTab = CreatePropertyTab("Errors", PropertyTab.Errors);
            _traceTab = CreatePropertyTab("Trace", PropertyTab.Trace);
            _searchTab = CreatePropertyTab("Search", PropertyTab.Search);
            tabs.Add(_inspectorTab);
            tabs.Add(_variablesTab);
            tabs.Add(_errorsTab);
            tabs.Add(_traceTab);
            tabs.Add(_searchTab);
            panel.Add(tabs);

            _propertyContent = new VisualElement();
            _propertyContent.style.flexGrow = 1;
            _propertyContent.style.marginLeft = 4f;
            _propertyContent.style.marginRight = 4f;
            _propertyContent.style.marginTop = 4f;
            _propertyContent.style.marginBottom = 4f;
            panel.Add(_propertyContent);

            var inspectorScroll = new ScrollView();
            inspectorScroll.style.flexGrow = 1;
            _inspectorView = new BehaviorTreeInspectorView(OnInspectorNodeChanged, OnInspectorGroupChanged);
            inspectorScroll.Add(_inspectorView);
            _inspectorPanel = inspectorScroll;

            _blackboardView = new BehaviorTreeBlackboardView();
            _variablesPanel = _blackboardView;

            var errorsScroll = new ScrollView();
            errorsScroll.style.flexGrow = 1;
            _validationBox = new VisualElement();
            _validationBox.style.flexGrow = 1;
            errorsScroll.Add(_validationBox);
            _errorsPanel = errorsScroll;

            var traceScroll = new ScrollView();
            traceScroll.style.flexGrow = 1;
            _traceBox = new VisualElement();
            _traceBox.style.flexGrow = 1;
            traceScroll.Add(_traceBox);
            _tracePanel = traceScroll;

            _searchPanelView = new BehaviorTreeSearchPanel(node =>
            {
                if (node == null)
                    return;
                _graphView?.FocusNode(node);
            });
            _searchPanel = _searchPanelView;

            SelectPropertyTab(_activeTab);
            return panel;
        }

        private static VisualElement CreateSection(string title, VisualElement content, float minHeight)
        {
            var section = new VisualElement();
            section.style.minHeight = minHeight;
            section.style.flexGrow = 1;
            section.style.marginLeft = 6f;
            section.style.marginRight = 6f;
            section.style.marginTop = 6f;
            section.style.marginBottom = 2f;
            section.style.backgroundColor = new Color(0.20f, 0.20f, 0.20f);
            section.style.borderTopLeftRadius = 4f;
            section.style.borderTopRightRadius = 4f;
            section.style.borderBottomLeftRadius = 4f;
            section.style.borderBottomRightRadius = 4f;

            var header = new Label(title);
            header.style.height = 24f;
            header.style.paddingLeft = 8f;
            header.style.paddingTop = 4f;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.color = new Color(0.82f, 0.82f, 0.82f);
            header.style.backgroundColor = new Color(0.13f, 0.13f, 0.13f);
            section.Add(header);

            content.style.flexGrow = 1;
            content.style.marginLeft = 4f;
            content.style.marginRight = 4f;
            content.style.marginTop = 4f;
            content.style.marginBottom = 4f;
            section.Add(content);
            return section;
        }

        public void SetTree(BehaviorTreeAsset tree)
        {
            _tree = tree;
            ResetDebugUiCache();
            if (_treeField != null)
                _treeField.SetValueWithoutNotify(_tree);

            _graphView?.PopulateView(_tree);
            _blackboardView?.Bind(_tree);
            _searchPanelView?.Bind(_tree);
            _inspectorView?.ClearSelection();
            RefreshGraphTitle();
            ValidateTree();
            TryAutoDetectRunner();
        }

        private void ResetDebugUiCache()
        {
            _lastFocusedPauseGuid = null;
            _nextDebugRefreshTime = 0d;
            _lastTraceVersion = -1;
            _lastTraceTick = -1;
            _lastTraceViewVersion = -1;
            _lastDebugState = (BehaviorTreeRunnerState)(-1);
            _lastExecutionStatus = (BTStatus)(-1);
            _lastDebugMode = false;
            _debugGraphWasActive = false;
        }

        public void RefreshInspector()
        {
            _inspectorView?.ClearSelection();
            ValidateTree();
        }

        public void SelectNode(BTNode node)
        {
            _inspectorView?.UpdateSelection(node);
            SelectPropertyTab(PropertyTab.Inspector);
        }

        public void SelectGroup(BehaviorTreeEditorGroup group)
        {
            _inspectorView?.UpdateSelection(group);
            SelectPropertyTab(PropertyTab.Inspector);
        }

        private void OnInspectorNodeChanged(BTNode node)
        {
            _graphView?.RefreshNodeView(node);
            RefreshGraphTitle();
            ValidateTree();
        }

        private void OnInspectorGroupChanged(BehaviorTreeEditorGroup group)
        {
            if (group == null || _tree == null)
                return;

            _graphView?.RefreshGroupView(group);
            EditorUtility.SetDirty(_tree);
            AssetDatabase.SaveAssets();
        }

        private void OnSelectionChanged()
        {
            if (Selection.activeObject is BehaviorTreeAsset selectedTree && selectedTree != _tree)
                SetTree(selectedTree);

            if (Selection.activeGameObject != null)
            {
                var runner = Selection.activeGameObject.GetComponentInParent<BehaviorTreeRunner>(true);
                if (runner != null && _tree != null && runner.SourceTree == _tree && _debugRunner != runner)
                {
                    _autoDetectedRunner = true;
                    _debugRunner = runner;
                    _runnerField?.SetValueWithoutNotify(runner);
                    ResetDebugUiCache();
                    _blackboardView?.SetDebugRunner(_debugRunner);
                    RefreshDebugState();
                }
            }
        }

        private void OnEditorUpdate()
        {
            if (_graphView == null)
                return;

            var debugActive = Application.isPlaying && _debugRunner != null && _debugRunner.DebugMode;
            var trace = debugActive ? _debugRunner.DebugTrace : null;
            var traceVersion = trace?.Version ?? -1;
            var traceTick = trace?.CurrentTick ?? -1;
            var debugStateChanged = HasDebugStateChanged();
            var traceChanged = traceVersion != _lastTraceVersion || traceTick != _lastTraceTick;
            var graphNeedsClear = _debugGraphWasActive && !debugActive;
            var now = EditorApplication.timeSinceStartup;

            if (!debugActive && !graphNeedsClear && !debugStateChanged)
                return;

            if (debugActive && !traceChanged && !debugStateChanged)
            {
                if (_debugRunner.State != BehaviorTreeRunnerState.Running || now < _nextDebugRefreshTime)
                    return;
            }

            _nextDebugRefreshTime = now + DebugRefreshInterval;
            _lastTraceVersion = traceVersion;
            _lastTraceTick = traceTick;
            _debugGraphWasActive = debugActive;

            var runtimeTree = debugActive ? _debugRunner.RuntimeTree : null;
            if (debugActive || graphNeedsClear || traceChanged)
            {
                _graphView.UpdateDebugState(runtimeTree, trace);
                _miniMapView?.MarkDirtyRepaint();
                _blackboardView?.MarkDirtyRepaint();
            }
            FocusBreakpointNodeIfNeeded();
            UpdateBreadcrumb(debugActive);
            RefreshDebugState();
            RefreshTraceView(traceVersion);
        }

        private void FocusBreakpointNodeIfNeeded()
        {
            if (!Application.isPlaying || _debugRunner == null || _debugRunner.State != BehaviorTreeRunnerState.Paused)
            {
                _lastFocusedPauseGuid = null;
                return;
            }

            var pauseNode = _debugRunner.PauseRequestedBy;
            if (pauseNode == null || string.IsNullOrWhiteSpace(pauseNode.Guid) || pauseNode.Guid == _lastFocusedPauseGuid)
                return;

            if (_graphView != null && _graphView.FocusNodeByGuid(pauseNode.Guid))
                _lastFocusedPauseGuid = pauseNode.Guid;
        }

        private bool HasDebugStateChanged()
        {
            if (_debugRunner == null)
                return _lastDebugState != (BehaviorTreeRunnerState)(-1) ||
                       _lastExecutionStatus != (BTStatus)(-1) ||
                       _lastDebugMode;

            return _lastDebugState != _debugRunner.State ||
                   _lastExecutionStatus != _debugRunner.ExecutionStatus ||
                   _lastDebugMode != _debugRunner.DebugMode;
        }

        private void CreateTreeAsset()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Behavior Tree 생성",
                "BT_NewBehaviorTree",
                "asset",
                "새 Behavior Tree Asset 저장 위치를 선택하세요.",
                "Assets/10.Datas");

            if (string.IsNullOrWhiteSpace(path))
                return;

            var tree = CreateInstance<BehaviorTreeAsset>();
            AssetDatabase.CreateAsset(tree, path);
            AssetDatabase.SaveAssets();
            SetTree(tree);
            EditorGUIUtility.PingObject(tree);
        }

        private void SaveTree()
        {
            if (_tree == null)
                return;

            _graphView?.FlushPendingSave();
            EditorUtility.SetDirty(_tree);
            foreach (var node in _tree.Nodes)
            {
                if (node != null)
                    EditorUtility.SetDirty(node);
            }

            AssetDatabase.SaveAssets();
            ValidateTree();
        }

        private void ValidateTree()
        {
            if (_validationBox == null)
                return;

            _validationBox.Clear();
            var messages = BehaviorTreeAssetValidator.Validate(_tree);
            var errorCount = 0;
            var warningCount = 0;

            foreach (var message in messages)
            {
                if (message.Level == BehaviorTreeValidationLevel.Error)
                    errorCount++;
                else if (message.Level == BehaviorTreeValidationLevel.Warning)
                    warningCount++;

                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.marginBottom = 4f;
                row.style.paddingLeft = 6f;
                row.style.paddingRight = 6f;
                row.style.paddingTop = 4f;
                row.style.paddingBottom = 4f;
                row.style.backgroundColor = new Color(0.09f, 0.09f, 0.11f);
                row.style.borderTopColor = new Color(0.18f, 0.18f, 0.22f);
                row.style.borderRightColor = new Color(0.18f, 0.18f, 0.22f);
                row.style.borderBottomColor = new Color(0.18f, 0.18f, 0.22f);
                row.style.borderLeftColor = new Color(0.18f, 0.18f, 0.22f);
                row.style.borderTopWidth = 1f;
                row.style.borderRightWidth = 1f;
                row.style.borderBottomWidth = 1f;
                row.style.borderLeftWidth = 1f;
                row.style.borderTopLeftRadius = 6f;
                row.style.borderTopRightRadius = 6f;
                row.style.borderBottomLeftRadius = 6f;
                row.style.borderBottomRightRadius = 6f;
                if (message.TargetNode != null)
                {
                    row.RegisterCallback<MouseDownEvent>(_ => _graphView?.FocusNode(message.TargetNode));
                }

                var icon = new Label(message.Level switch
                {
                    BehaviorTreeValidationLevel.Error => "!",
                    BehaviorTreeValidationLevel.Warning => "?",
                    _ => "i"
                });
                icon.style.width = 18f;
                icon.style.unityTextAlign = TextAnchor.MiddleCenter;
                icon.style.unityFontStyleAndWeight = FontStyle.Bold;
                icon.style.color = Color.white;
                icon.style.backgroundColor = message.Level switch
                {
                    BehaviorTreeValidationLevel.Error => new Color(0.82f, 0.18f, 0.18f),
                    BehaviorTreeValidationLevel.Warning => new Color(0.88f, 0.58f, 0.12f),
                    _ => new Color(0.18f, 0.55f, 0.82f)
                };
                icon.style.borderTopLeftRadius = 8f;
                icon.style.borderTopRightRadius = 8f;
                icon.style.borderBottomLeftRadius = 8f;
                icon.style.borderBottomRightRadius = 8f;
                row.Add(icon);

                var label = new Label(message.Message);
                label.style.whiteSpace = WhiteSpace.Normal;
                label.style.flexGrow = 1;
                label.style.marginLeft = 6f;
                label.style.color = new Color(0.82f, 0.82f, 0.82f);
                row.Add(label);

                _validationBox.Add(row);
            }

            UpdateErrorBadge(errorCount, warningCount);
        }

        private void CleanNullReferences()
        {
            if (_tree == null)
                return;

            Undo.RecordObject(_tree, "Clean Behavior Tree Null References");
            var removedCount = _tree.Nodes.RemoveAll(node => node == null);

            foreach (var node in _tree.Nodes)
            {
                if (node == null)
                    continue;

                Undo.RecordObject(node, "Clean Behavior Tree Null References");
                removedCount += node.Children.RemoveAll(child => child == null || !_tree.Nodes.Contains(child));
            }

            EditorUtility.SetDirty(_tree);
            foreach (var node in _tree.Nodes)
            {
                if (node != null)
                    EditorUtility.SetDirty(node);
            }

            AssetDatabase.SaveAssets();
            _graphView?.PopulateView(_tree);
            RefreshGraphTitle();
            ValidateTree();

            if (removedCount > 0)
                Debug.Log($"Behavior Tree null reference 정리 완료: {_tree.name}, 제거 {removedCount}개");
        }

        private void RefreshGraphTitle()
        {
            if (_graphTitleLabel == null || _graphSubtitleLabel == null)
                return;

            _graphTitleLabel.text = _tree != null ? _tree.name : "No Behavior Tree";
            _graphSubtitleLabel.text = _tree != null
                ? $"Nodes {_tree.Nodes.Count}  |  Root {(_tree.RootNode != null ? _tree.RootNode.DisplayName : "None")}  |  우클릭 생성 · 형제 노드 좌우 드래그로 실행 순서 정렬"
                : "Select or import a BehaviorTreeAsset";
        }

        private ToolbarToggle CreatePropertyTab(string text, PropertyTab tab)
        {
            var toggle = new ToolbarToggle { text = text };
            toggle.style.height = 30f;
            toggle.style.marginLeft = 2f;
            toggle.style.marginRight = 2f;
            toggle.style.color = BehaviorTreeEditorStyles.TextMuted;
            toggle.style.borderBottomWidth = 2f;
            toggle.style.borderBottomColor = Color.clear;
            toggle.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                    SelectPropertyTab(tab);
                else if (_activeTab == tab)
                    toggle.SetValueWithoutNotify(true);
            });
            return toggle;
        }

        private void SelectPropertyTab(PropertyTab tab)
        {
            _activeTab = tab;
            _inspectorTab?.SetValueWithoutNotify(tab == PropertyTab.Inspector);
            _variablesTab?.SetValueWithoutNotify(tab == PropertyTab.Variables);
            _errorsTab?.SetValueWithoutNotify(tab == PropertyTab.Errors);
            _traceTab?.SetValueWithoutNotify(tab == PropertyTab.Trace);
            _searchTab?.SetValueWithoutNotify(tab == PropertyTab.Search);
            StylePropertyTab(_inspectorTab, tab == PropertyTab.Inspector);
            StylePropertyTab(_variablesTab, tab == PropertyTab.Variables);
            StylePropertyTab(_errorsTab, tab == PropertyTab.Errors);
            StylePropertyTab(_traceTab, tab == PropertyTab.Trace);
            StylePropertyTab(_searchTab, tab == PropertyTab.Search);

            if (_propertyContent == null)
                return;

            _propertyContent.Clear();
            var content = tab switch
            {
                PropertyTab.Variables => _variablesPanel,
                PropertyTab.Errors => _errorsPanel,
                PropertyTab.Trace => _tracePanel,
                PropertyTab.Search => _searchPanel,
                _ => _inspectorPanel
            };

            if (content != null)
                _propertyContent.Add(content);

            if (tab == PropertyTab.Trace)
            {
                _lastTraceViewVersion = int.MinValue;
                RefreshTraceView(_lastTraceVersion);
            }
            else if (tab == PropertyTab.Search)
            {
                _searchPanelView?.Bind(_tree);
            }
        }

        private static void StylePropertyTab(ToolbarToggle toggle, bool active)
        {
            if (toggle == null)
                return;

            toggle.style.color = active ? BehaviorTreeEditorStyles.Text : BehaviorTreeEditorStyles.TextMuted;
            toggle.style.borderBottomColor = active ? BehaviorTreeEditorStyles.Composite : Color.clear;
            toggle.style.backgroundColor = active ? BehaviorTreeEditorStyles.PanelRaised : Color.clear;
        }

        private enum ToolbarButtonStyle
        {
            Primary,
            Ghost,
            Danger,
            Success
        }

        private static ToolbarButton CreateToolbarButton(string text, System.Action action, ToolbarButtonStyle style)
        {
            var button = new ToolbarButton(action) { text = text };
            button.style.height = 24f;
            button.style.marginLeft = 3f;
            button.style.marginRight = 3f;
            button.style.paddingLeft = 12f;
            button.style.paddingRight = 12f;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.borderTopLeftRadius = 5f;
            button.style.borderTopRightRadius = 5f;
            button.style.borderBottomLeftRadius = 5f;
            button.style.borderBottomRightRadius = 5f;

            var bg = style switch
            {
                ToolbarButtonStyle.Primary => BehaviorTreeEditorStyles.Composite,
                ToolbarButtonStyle.Danger => new Color(0.22f, 0.07f, 0.05f),
                ToolbarButtonStyle.Success => new Color(0.05f, 0.18f, 0.09f),
                _ => BehaviorTreeEditorStyles.PanelRaised
            };
            var border = style switch
            {
                ToolbarButtonStyle.Danger => new Color(0.48f, 0.16f, 0.12f),
                ToolbarButtonStyle.Success => new Color(0.20f, 0.48f, 0.26f),
                _ => BehaviorTreeEditorStyles.BorderStrong
            };
            button.style.backgroundColor = bg;
            button.style.color = style switch
            {
                ToolbarButtonStyle.Danger => new Color(0.95f, 0.42f, 0.32f),
                ToolbarButtonStyle.Success => new Color(0.48f, 0.96f, 0.56f),
                ToolbarButtonStyle.Primary => Color.white,
                _ => new Color(0.72f, 0.72f, 0.82f)
            };
            button.style.borderTopColor = border;
            button.style.borderRightColor = border;
            button.style.borderBottomColor = border;
            button.style.borderLeftColor = border;
            button.style.borderTopWidth = 1f;
            button.style.borderRightWidth = 1f;
            button.style.borderBottomWidth = 1f;
            button.style.borderLeftWidth = 1f;
            return button;
        }

        private static VisualElement CreateToolbarSeparator()
        {
            var separator = new VisualElement();
            separator.style.width = 1f;
            separator.style.height = 22f;
            separator.style.marginLeft = 5f;
            separator.style.marginRight = 5f;
            separator.style.backgroundColor = BehaviorTreeEditorStyles.BorderStrong;
            return separator;
        }

        private static Label CreateToolbarLabel(string text)
        {
            var label = new Label(text);
            label.style.marginLeft = 6f;
            label.style.marginRight = 4f;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.color = BehaviorTreeEditorStyles.TextMuted;
            return label;
        }

        private static void StyleToolbarToggle(ToolbarToggle toggle, bool active)
        {
            toggle.style.height = 24f;
            toggle.style.marginLeft = 4f;
            toggle.style.paddingLeft = 10f;
            toggle.style.paddingRight = 10f;
            toggle.style.unityFontStyleAndWeight = FontStyle.Bold;
            toggle.style.borderTopLeftRadius = 5f;
            toggle.style.borderTopRightRadius = 5f;
            toggle.style.borderBottomLeftRadius = 5f;
            toggle.style.borderBottomRightRadius = 5f;
            toggle.style.backgroundColor = active ? BehaviorTreeEditorStyles.Composite : BehaviorTreeEditorStyles.PanelRaised;
            toggle.style.color = active ? Color.white : BehaviorTreeEditorStyles.TextMuted;
            toggle.style.borderTopColor = active ? BehaviorTreeEditorStyles.Composite : BehaviorTreeEditorStyles.BorderStrong;
            toggle.style.borderRightColor = active ? BehaviorTreeEditorStyles.Composite : BehaviorTreeEditorStyles.BorderStrong;
            toggle.style.borderBottomColor = active ? BehaviorTreeEditorStyles.Composite : BehaviorTreeEditorStyles.BorderStrong;
            toggle.style.borderLeftColor = active ? BehaviorTreeEditorStyles.Composite : BehaviorTreeEditorStyles.BorderStrong;
            toggle.style.borderTopWidth = 1f;
            toggle.style.borderRightWidth = 1f;
            toggle.style.borderBottomWidth = 1f;
            toggle.style.borderLeftWidth = 1f;
        }

        private static Label CreateStatusBadge(string text, Color color)
        {
            var label = new Label(text);
            label.style.height = 20f;
            label.style.marginRight = 6f;
            label.style.paddingLeft = 8f;
            label.style.paddingRight = 8f;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = Color.white;
            label.style.backgroundColor = color;
            label.style.borderTopLeftRadius = 6f;
            label.style.borderTopRightRadius = 6f;
            label.style.borderBottomLeftRadius = 6f;
            label.style.borderBottomRightRadius = 6f;
            return label;
        }

        private void UpdateErrorBadge(int errorCount, int warningCount)
        {
            if (_errorCountLabel == null)
                return;

            _errorCountLabel.text = warningCount > 0
                ? $"Errors {errorCount}  Warnings {warningCount}"
                : $"Errors {errorCount}";
            _errorCountLabel.style.backgroundColor = errorCount > 0
                ? new Color(0.72f, 0.18f, 0.18f)
                : warningCount > 0
                    ? new Color(0.72f, 0.45f, 0.12f)
                    : new Color(0.18f, 0.50f, 0.28f);
        }

        private void RefreshDebugState()
        {
            if (_debugStateLabel == null)
                return;

            if (_debugRunner == null)
            {
                _debugStateLabel.text = "No Runner";
                _debugStateLabel.style.backgroundColor = new Color(0.30f, 0.30f, 0.30f);
                if (_runtimeBanner != null)
                    _runtimeBanner.style.display = DisplayStyle.None;
                _lastDebugState = (BehaviorTreeRunnerState)(-1);
                _lastExecutionStatus = (BTStatus)(-1);
                _lastDebugMode = false;
                return;
            }

            _debugStateLabel.text = $"{_debugRunner.State}  |  {_debugRunner.ExecutionStatus}";
            _debugStateLabel.style.backgroundColor = _debugRunner.State switch
            {
                BehaviorTreeRunnerState.Running => new Color(0.18f, 0.50f, 0.28f),
                BehaviorTreeRunnerState.Paused => new Color(0.72f, 0.45f, 0.12f),
                _ => new Color(0.30f, 0.30f, 0.30f)
            };
            if (_runtimeBanner != null)
                _runtimeBanner.style.display = Application.isPlaying && _debugRunner.DebugMode
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            _lastDebugState = _debugRunner.State;
            _lastExecutionStatus = _debugRunner.ExecutionStatus;
            _lastDebugMode = _debugRunner.DebugMode;
        }

        private void RefreshTraceView(int traceVersion)
        {
            if (_traceBox == null || _activeTab != PropertyTab.Trace)
                return;

            if (_lastTraceViewVersion == traceVersion)
                return;

            _lastTraceViewVersion = traceVersion;
            _traceBox.Clear();
            var trace = _debugRunner != null && _debugRunner.DebugMode ? _debugRunner.DebugTrace : null;
            if (trace == null || trace.Records.Count == 0)
            {
                var empty = new Label("Play Mode에서 Debug Runner를 지정하면 최근 Tick Trace가 표시됩니다.");
                empty.style.color = new Color(0.72f, 0.72f, 0.72f);
                empty.style.whiteSpace = WhiteSpace.Normal;
                _traceBox.Add(empty);
                return;
            }

            foreach (var record in trace.Records)
            {
                var row = new Label($"#{record.Tick:000} {record.EventType,-16} {record.Status,-7} {record.NodeName} [{ShortGuid(record.NodeGuid)}] {record.Detail}");
                row.style.fontSize = 10f;
                row.style.color = GetTraceColor(record);
                row.style.whiteSpace = WhiteSpace.Normal;
                row.style.marginBottom = 2f;
                row.style.paddingLeft = 4f;
                row.style.paddingRight = 4f;
                row.tooltip = "클릭하면 해당 노드로 이동합니다.";
                var nodeGuid = record.NodeGuid;
                row.RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.button != 0)
                        return;

                    _graphView?.FocusNodeByGuid(nodeGuid);
                    evt.StopPropagation();
                });
                _traceBox.Add(row);
            }
        }

        private static string ShortGuid(string guid)
        {
            if (string.IsNullOrWhiteSpace(guid))
                return "none";

            return guid.Length > 8 ? guid.Substring(0, 8) : guid;
        }

        private static Color GetTraceColor(BehaviorTreeDebugTraceRecord record)
        {
            if (record.EventType == "Breakpoint" || record.EventType == "ConditionalAbort")
                return new Color(1f, 0.72f, 0.28f);

            return record.Status switch
            {
                BTStatus.Success => new Color(0.46f, 0.86f, 0.52f),
                BTStatus.Failure => new Color(0.95f, 0.38f, 0.34f),
                BTStatus.Running => new Color(0.95f, 0.72f, 0.24f),
                _ => new Color(0.78f, 0.78f, 0.78f)
            };
        }
    }

    internal sealed class BehaviorTreeMiniMapView : IMGUIContainer
    {
        private const float HeaderHeight = 20f;
        private readonly BehaviorTreeGraphView _graphView;
        private bool _isDraggingViewport;

        public BehaviorTreeMiniMapView(BehaviorTreeGraphView graphView)
        {
            _graphView = graphView;
            pickingMode = PickingMode.Position;
            style.position = Position.Absolute;
            style.right = 12f;
            style.bottom = 12f;
            style.width = 170f;
            style.height = 118f;
            style.backgroundColor = BehaviorTreeEditorStyles.WithAlpha(BehaviorTreeEditorStyles.Panel, 0.94f);
            style.borderTopColor = BehaviorTreeEditorStyles.Composite;
            style.borderRightColor = BehaviorTreeEditorStyles.Composite;
            style.borderBottomColor = BehaviorTreeEditorStyles.Composite;
            style.borderLeftColor = BehaviorTreeEditorStyles.Composite;
            style.borderTopWidth = 1f;
            style.borderRightWidth = 1f;
            style.borderBottomWidth = 1f;
            style.borderLeftWidth = 1f;
            style.borderTopLeftRadius = 8f;
            style.borderTopRightRadius = 8f;
            style.borderBottomLeftRadius = 8f;
            style.borderBottomRightRadius = 8f;
            onGUIHandler = DrawMiniMap;
        }

        private void DrawMiniMap()
        {
            if (_graphView == null)
                return;

            var rect = new Rect(0f, 0f, resolvedStyle.width, resolvedStyle.height);
            EditorGUI.DrawRect(rect, BehaviorTreeEditorStyles.WithAlpha(BehaviorTreeEditorStyles.Panel, 0.94f));
            EditorGUI.DrawRect(new Rect(0f, 0f, rect.width, HeaderHeight), BehaviorTreeEditorStyles.PanelAlt);
            GUI.Label(new Rect(8f, 2f, rect.width - 16f, 16f), "MINIMAP", MiniMapLabelStyle());
            var fitButtonRect = new Rect(rect.width - 42f, 3f, 34f, 14f);
            GUI.Label(fitButtonRect, "FIT", MiniMapButtonStyle());

            var treeBounds = _graphView.GetTreeBounds();
            if (treeBounds.width <= 1f || treeBounds.height <= 1f)
                return;

            var mapRect = new Rect(8f, HeaderHeight + 8f, rect.width - 16f, rect.height - HeaderHeight - 16f);
            var paddedBounds = new Rect(
                treeBounds.xMin - 80f,
                treeBounds.yMin - 80f,
                treeBounds.width + 160f,
                treeBounds.height + 160f);

            HandleInput(mapRect, fitButtonRect, paddedBounds);

            Handles.BeginGUI();
            foreach (var group in _graphView.GetMiniMapGroups())
            {
                var mini = ToMini(group.Rect, paddedBounds, mapRect);
                EditorGUI.DrawRect(mini, BehaviorTreeEditorStyles.WithAlpha(group.Color, 0.52f));
                Handles.DrawSolidRectangleWithOutline(
                    mini,
                    Color.clear,
                    BehaviorTreeEditorStyles.WithAlpha(group.Color, 0.95f));
            }

            foreach (var edge in _graphView.GetMiniMapEdges())
            {
                var from = ToMini(edge.From, paddedBounds, mapRect);
                var to = ToMini(edge.To, paddedBounds, mapRect);
                Handles.DrawBezier(
                    from,
                    to,
                    new Vector2(from.x, Mathf.Lerp(from.y, to.y, 0.45f)),
                    new Vector2(to.x, Mathf.Lerp(from.y, to.y, 0.55f)),
                    edge.Running ? BehaviorTreeEditorStyles.Running : BehaviorTreeEditorStyles.WithAlpha(BehaviorTreeEditorStyles.Composite, 0.58f),
                    null,
                    edge.Running ? 2f : 1f);
            }

            foreach (var node in _graphView.GetMiniMapNodes())
            {
                var mini = ToMini(node.Rect, paddedBounds, mapRect);
                EditorGUI.DrawRect(mini, node.Running ? BehaviorTreeEditorStyles.Running : BehaviorTreeEditorStyles.WithAlpha(node.Color, 0.82f));
            }

            DrawViewportRect(paddedBounds, mapRect);
            Handles.EndGUI();
        }

        private void HandleInput(Rect mapRect, Rect fitButtonRect, Rect bounds)
        {
            var evt = Event.current;
            if (evt == null)
                return;

            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                if (fitButtonRect.Contains(evt.mousePosition))
                {
                    _graphView.FrameAllNodes();
                    evt.Use();
                    return;
                }

                if (mapRect.Contains(evt.mousePosition))
                {
                    _isDraggingViewport = true;
                    MoveGraphToMiniMapPoint(evt.mousePosition, bounds, mapRect);
                    evt.Use();
                }
            }
            else if (evt.type == EventType.MouseDrag && _isDraggingViewport && evt.button == 0)
            {
                MoveGraphToMiniMapPoint(evt.mousePosition, bounds, mapRect);
                evt.Use();
            }
            else if (evt.type == EventType.MouseUp && evt.button == 0)
            {
                _isDraggingViewport = false;
            }
            else if (evt.type == EventType.ScrollWheel && mapRect.Contains(evt.mousePosition))
            {
                var contentPosition = FromMini(evt.mousePosition, bounds, mapRect);
                _graphView.ZoomAroundContentPosition(contentPosition, -evt.delta.y);
                evt.Use();
            }
        }

        private void MoveGraphToMiniMapPoint(Vector2 miniMapPosition, Rect bounds, Rect mapRect)
        {
            var contentPosition = FromMini(miniMapPosition, bounds, mapRect);
            _graphView.CenterOnContentPosition(contentPosition);
            MarkDirtyRepaint();
        }

        private void DrawViewportRect(Rect bounds, Rect mapRect)
        {
            var visible = _graphView.GetVisibleContentBounds();
            var mini = ToMini(visible, bounds, mapRect);
            mini.xMin = Mathf.Clamp(mini.xMin, mapRect.xMin, mapRect.xMax);
            mini.yMin = Mathf.Clamp(mini.yMin, mapRect.yMin, mapRect.yMax);
            mini.xMax = Mathf.Clamp(mini.xMax, mapRect.xMin, mapRect.xMax);
            mini.yMax = Mathf.Clamp(mini.yMax, mapRect.yMin, mapRect.yMax);

            EditorGUI.DrawRect(mini, BehaviorTreeEditorStyles.WithAlpha(BehaviorTreeEditorStyles.Composite, 0.14f));
            Handles.DrawSolidRectangleWithOutline(
                mini,
                Color.clear,
                BehaviorTreeEditorStyles.Composite);
        }

        private static Rect ToMini(Rect source, Rect bounds, Rect mapRect)
        {
            var min = ToMini(source.min, bounds, mapRect);
            var max = ToMini(source.max, bounds, mapRect);
            return new Rect(min.x, min.y, Mathf.Max(3f, max.x - min.x), Mathf.Max(3f, max.y - min.y));
        }

        private static Vector2 ToMini(Vector2 source, Rect bounds, Rect mapRect)
        {
            var x = Mathf.InverseLerp(bounds.xMin, bounds.xMax, source.x);
            var y = Mathf.InverseLerp(bounds.yMin, bounds.yMax, source.y);
            return new Vector2(
                Mathf.Lerp(mapRect.xMin, mapRect.xMax, x),
                Mathf.Lerp(mapRect.yMin, mapRect.yMax, y));
        }

        private static Vector2 FromMini(Vector2 source, Rect bounds, Rect mapRect)
        {
            var x = Mathf.InverseLerp(mapRect.xMin, mapRect.xMax, Mathf.Clamp(source.x, mapRect.xMin, mapRect.xMax));
            var y = Mathf.InverseLerp(mapRect.yMin, mapRect.yMax, Mathf.Clamp(source.y, mapRect.yMin, mapRect.yMax));
            return new Vector2(
                Mathf.Lerp(bounds.xMin, bounds.xMax, x),
                Mathf.Lerp(bounds.yMin, bounds.yMax, y));
        }

        private static GUIStyle MiniMapLabelStyle()
        {
            return new GUIStyle(EditorStyles.miniBoldLabel)
            {
                normal = { textColor = BehaviorTreeEditorStyles.TextMuted },
                alignment = TextAnchor.MiddleLeft,
                fontSize = 9
            };
        }

        private static GUIStyle MiniMapButtonStyle()
        {
            return new GUIStyle(EditorStyles.miniBoldLabel)
            {
                normal = { textColor = BehaviorTreeEditorStyles.Composite },
                alignment = TextAnchor.MiddleCenter,
                fontSize = 9
            };
        }
    }
}
#endif
