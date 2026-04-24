using System;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.Search;
using UPlayGround.BehaviorTree;
using UPlayGround.Component;

namespace UPlayGround.Editor.BehaviorTree
{
    public class BehaviorTreeEditorWindow : EditorWindow
    {
        private BehaviorTreeGraphView _graphView;
        private BTBlackboardView      _blackboardView;
        private IMGUIContainer        _inspectorContainer;
        private ObjectField           _treeField;
        private Label                 _modeBadge;

        private BehaviorTreeSO _currentTreeSO;
        private BTRunner       _runtimeRunner;
        private bool           _isRuntimeMode;
        private BTNodeSO       _selectedNodeSO;
        private SerializedObject _inspectorSO;

        private const string USS_PATH = "Assets/02.Scripts/BehaviorTree/Editor/BehaviorTreeEditor.uss";
        private const double REFRESH_INTERVAL = 0.1;
        private double       _lastRefreshTime;

        // ── 윈도우 열기 ───────────────────────────────
        [MenuItem("Window/BehaviorTree Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<BehaviorTreeEditorWindow>("BehaviorTree Editor");
            window.minSize = new Vector2(800, 500);
        }

        // ── UI 구성 ───────────────────────────────────
        private void CreateGUI()
        {
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(USS_PATH);
            if (styleSheet != null)
                rootVisualElement.styleSheets.Add(styleSheet);

            BuildLayout();
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            Undo.undoRedoPerformed += OnUndoRedo;

            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
        }

        private void BuildLayout()
        {
            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;

            // GraphView를 먼저 생성 (툴바의 FrameAll 버튼이 참조)
            _graphView = new BehaviorTreeGraphView();
            _graphView.NodeSelectionChanged += OnNodeSelectionChanged;

            // ── 툴바 ─────────────────────────────────
            root.Add(BuildToolbar());

            // ── 메인 분할 뷰 ─────────────────────────
            var splitView = new TwoPaneSplitView(1, 280, TwoPaneSplitViewOrientation.Horizontal);
            splitView.style.flexGrow = 1;
            root.Add(splitView);

            // 왼쪽: GraphView
            splitView.Add(_graphView);

            // 오른쪽: Inspector + Blackboard
            var rightPanel = new TwoPaneSplitView(1, 160, TwoPaneSplitViewOrientation.Vertical);
            splitView.Add(rightPanel);

            var inspectorScroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            inspectorScroll.style.flexGrow = 1;
            _inspectorContainer = new IMGUIContainer(DrawInspectorGUI);
            _inspectorContainer.style.flexGrow = 1;
            inspectorScroll.Add(_inspectorContainer);
            rightPanel.Add(inspectorScroll);

            var bbScroll = new ScrollView(ScrollViewMode.Vertical);
            _blackboardView = new BTBlackboardView();
            bbScroll.Add(_blackboardView);
            rightPanel.Add(bbScroll);
        }

        private VisualElement BuildToolbar()
        {
            var toolbar = new VisualElement();
            toolbar.style.flexDirection     = FlexDirection.Row;
            toolbar.style.height            = 30;
            toolbar.style.backgroundColor   = new StyleColor(new Color(0.2f, 0.2f, 0.2f));
            toolbar.style.paddingLeft       = 8;
            toolbar.style.paddingRight      = 8;
            toolbar.style.alignItems        = Align.Center;
            toolbar.style.borderBottomWidth = 1;
            toolbar.style.borderBottomColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f));

            var titleLabel = new Label("BehaviorTree Editor");
            titleLabel.AddToClassList("bt-toolbar-label");
            toolbar.Add(titleLabel);

            // ── 에셋 선택 필드 + 탐색 버튼 ───────────
            _treeField = new ObjectField
            {
                objectType = typeof(BehaviorTreeSO),
            };
            _treeField.style.minWidth = 200;
            _treeField.RegisterValueChangedCallback(e =>
            {
                _currentTreeSO = e.newValue as BehaviorTreeSO;
                _graphView.PopulateView(_currentTreeSO);
                _blackboardView.SetBlackboardSO(_currentTreeSO?.blackboard);
            });
            toolbar.Add(_treeField);

            var browseBtn = new Button(OnBrowseClicked) { text = "…" };
            browseBtn.style.marginLeft  = 2;
            browseBtn.style.marginRight = 8;
            toolbar.Add(browseBtn);

            // ── 편집 버튼 그룹 ───────────────────────
            var refreshBtn = new Button(OnRefreshClicked) { text = "Refresh" };
            toolbar.Add(refreshBtn);
            toolbar.Add(new Button(() => _graphView.FrameAll()) { text = "Frame All" });

            var saveBtn = new Button(OnSaveClicked) { text = "저장 (Ctrl+S)" };
            saveBtn.style.marginLeft = 8;
            toolbar.Add(saveBtn);

            // ── JSON 버튼 그룹 ───────────────────────
            var exportBtn = new Button(OnExportJson) { text = "JSON 내보내기" };
            exportBtn.style.marginLeft = 8;
            toolbar.Add(exportBtn);

            var importBtn = new Button(OnImportJson) { text = "JSON 불러오기" };
            toolbar.Add(importBtn);

            _modeBadge = new Label("편집 모드");
            _modeBadge.AddToClassList("bt-editmode-badge");
            _modeBadge.style.marginLeft = 8;
            toolbar.Add(_modeBadge);

            return toolbar;
        }

        private void OnDestroy()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        // ── Inspector IMGUI 그리기 ────────────────────
        private void DrawInspectorGUI()
        {
            if (_inspectorSO == null || _selectedNodeSO == null) return;

            _inspectorSO.Update();
            using (new EditorGUI.DisabledScope(_isRuntimeMode && Application.isPlaying))
            {
                EditorGUILayout.LabelField(_selectedNodeSO.GetType().Name, EditorStyles.boldLabel);
                EditorGUILayout.Space(4);

                var prop = _inspectorSO.GetIterator();
                prop.NextVisible(true); // m_Script 건너뜀
                while (prop.NextVisible(false))
                    EditorGUILayout.PropertyField(prop, true);
            }
            _inspectorSO.ApplyModifiedProperties();
        }

        // ── 노드 선택 콜백 ────────────────────────────
        private void OnNodeSelectionChanged(BTNodeView view, bool selected)
        {
            if (selected)
            {
                _selectedNodeSO  = view.NodeSO;
                _inspectorSO     = new SerializedObject(view.NodeSO);
            }
            else if (_selectedNodeSO == view.NodeSO)
            {
                _selectedNodeSO = null;
                _inspectorSO    = null;
            }
        }

        // ── 플레이 모드 전환 ──────────────────────────
        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    EditorApplication.delayCall += TryBindRuntimeFromSelection;
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    ExitRuntimeMode();
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    if (_currentTreeSO != null) _graphView.PopulateView(_currentTreeSO);
                    break;
            }
        }

        // 플레이 중 Hierarchy 선택 변경
        private void OnSelectionChange()
        {
            if (Application.isPlaying)
                TryBindRuntimeFromSelection();
        }

        private void TryBindRuntimeFromSelection()
        {
            var go     = Selection.activeGameObject;
            var runner = go != null ? go.GetComponent<BTRunner>() : null;

            if (runner == null || runner.RuntimeTree == null) return;

            // 이 Runner에 연결된 SO 확인
            var runnerSO    = new SerializedObject(runner);
            var treeProp    = runnerSO.FindProperty("_behaviorTreeSO");
            var linkedTree  = treeProp?.objectReferenceValue as BehaviorTreeSO;

            if (linkedTree != null && linkedTree != _currentTreeSO)
            {
                _currentTreeSO = linkedTree;
                _treeField.SetValueWithoutNotify(linkedTree);
                _graphView.PopulateView(linkedTree);
            }

            _runtimeRunner = runner;
            _isRuntimeMode = runner.RuntimeTree != null;

            _graphView.BindRuntimeTree(runner.RuntimeTree);
            _blackboardView.SetBlackboard(runner.Blackboard);

            _modeBadge.text = _isRuntimeMode
                ? $"런타임: {runner.gameObject.name}"
                : "편집 모드";

            if (_isRuntimeMode)
            {
                _modeBadge.RemoveFromClassList("bt-editmode-badge");
                _modeBadge.AddToClassList("bt-runtime-badge");
            }
        }

        private void ExitRuntimeMode()
        {
            _runtimeRunner = null;
            _isRuntimeMode = false;
            _graphView.BindRuntimeTree(null);
            _blackboardView.SetBlackboardSO(_currentTreeSO?.blackboard);
            _modeBadge.text = "편집 모드";
            _modeBadge.RemoveFromClassList("bt-runtime-badge");
            _modeBadge.AddToClassList("bt-editmode-badge");
        }

        // ── Undo/Redo 콜백 ───────────────────────────
        private void OnUndoRedo()
        {
            if (!_isRuntimeMode && _currentTreeSO != null)
                _graphView.PopulateView(_currentTreeSO);
        }

        // ── Ctrl+S 단축키 ────────────────────────────
        private void OnKeyDown(KeyDownEvent e)
        {
            if (e.keyCode == KeyCode.S && e.ctrlKey)
            {
                OnSaveClicked();
                e.StopPropagation();
            }
        }

        // ── 버튼 ──────────────────────────────────────
        private void OnBrowseClicked()
        {
#if UNITY_6000_0_OR_NEWER
            var ctx = SearchService.CreateContext("adb", "t:BehaviorTreeSO");
            SearchService.ShowPicker(
                ctx,
                (item, cancelled) =>
                {
                    if (cancelled) return;
                    var path = AssetDatabase.GUIDToAssetPath(item.id);
                    var so   = AssetDatabase.LoadAssetAtPath<BehaviorTreeSO>(path);
                    if (so == null) return;
                    _currentTreeSO = so;
                    _treeField.SetValueWithoutNotify(so);
                    _graphView.PopulateView(so);
                    _blackboardView.SetBlackboardSO(so.blackboard);
                },
                (Action<SearchItem>)null,
                (Func<SearchItem, bool>)null,
                (System.Collections.Generic.IEnumerable<SearchItem>)null);
#else
            var path = EditorUtility.OpenFilePanelWithFilters(
                "BehaviorTreeSO 선택", "Assets", new[] { "BehaviorTreeSO", "asset" });
            if (string.IsNullOrEmpty(path)) return;
            path = "Assets" + path.Substring(Application.dataPath.Length);
            var asset = AssetDatabase.LoadAssetAtPath<BehaviorTreeSO>(path);
            if (asset == null) return;
            _currentTreeSO = asset;
            _treeField.SetValueWithoutNotify(asset);
            _graphView.PopulateView(asset);
            _blackboardView.SetBlackboardSO(asset.blackboard);
#endif
        }

        private void OnSaveClicked()
        {
            if (_currentTreeSO == null) return;
            EditorUtility.SetDirty(_currentTreeSO);
            AssetDatabase.SaveAssetIfDirty(_currentTreeSO);
            Debug.Log($"[BT Editor] 저장 완료: {_currentTreeSO.name}");
        }

        private void OnExportJson()
        {
            if (_currentTreeSO == null)
            {
                EditorUtility.DisplayDialog("JSON 내보내기", "내보낼 트리를 먼저 선택하세요.", "확인");
                return;
            }

            var savePath = EditorUtility.SaveFilePanel(
                "BT JSON 내보내기", "", _currentTreeSO.name, "json");
            if (string.IsNullOrEmpty(savePath)) return;

            var json = BTJsonSerializer.ExportTree(_currentTreeSO);
            File.WriteAllText(savePath, json, System.Text.Encoding.UTF8);
            Debug.Log($"[BT Editor] JSON 내보내기 완료: {savePath}");
        }

        private void OnImportJson()
        {
            var openPath = EditorUtility.OpenFilePanel("BT JSON 불러오기", "", "json");
            if (string.IsNullOrEmpty(openPath)) return;

            var json = File.ReadAllText(openPath, System.Text.Encoding.UTF8);

            var savePath = EditorUtility.SaveFilePanelInProject(
                "가져온 BT 저장 위치", Path.GetFileNameWithoutExtension(openPath), "asset",
                "저장할 BehaviorTreeSO 경로를 지정하세요.", "Assets");
            if (string.IsNullOrEmpty(savePath)) return;

            try
            {
                var imported = BTJsonSerializer.ImportTree(json, savePath);
                _currentTreeSO = imported;
                _treeField.SetValueWithoutNotify(imported);
                _graphView.PopulateView(imported);
                _blackboardView.SetBlackboardSO(imported.blackboard);
                Debug.Log($"[BT Editor] JSON 불러오기 완료: {savePath}");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("JSON 불러오기 실패", ex.Message, "확인");
            }
        }

        private void OnRefreshClicked()
        {
            if (_isRuntimeMode && _runtimeRunner != null)
                _graphView.BindRuntimeTree(_runtimeRunner.RuntimeTree);
            else if (_currentTreeSO != null)
                _graphView.PopulateView(_currentTreeSO);
        }

        // ── 주기적 갱신 (런타임 하이라이트) ──────────
        private void Update()
        {
            if (!_isRuntimeMode) return;
            if (EditorApplication.timeSinceStartup - _lastRefreshTime < REFRESH_INTERVAL) return;

            _lastRefreshTime = EditorApplication.timeSinceStartup;
            _graphView.RefreshRuntimeStatus();
            _blackboardView.Refresh();

            // Runner가 파괴되거나 BT가 비활성 상태면 종료
            if (_runtimeRunner == null || _runtimeRunner.RuntimeTree == null)
                ExitRuntimeMode();
        }
    }
}
