#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public partial class BehaviorTreeEditorWindow
    {
        private enum ToolbarButtonStyle
        {
            Primary,
            Ghost,
            Danger,
            Success
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
            operationsToolbar.Add(CreateToolbarButton("Import", AIBehaviorJsonDispatcher.ImportFromFilePanel, ToolbarButtonStyle.Ghost));
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
    }
}
#endif
