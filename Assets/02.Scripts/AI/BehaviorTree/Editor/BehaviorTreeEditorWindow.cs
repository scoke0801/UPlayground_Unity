#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public partial class BehaviorTreeEditorWindow : EditorWindow
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

        [MenuItem("UPlayGround/Behavior Tree/Editor")]
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
    }
}
#endif
