#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public class BehaviorTreeEditorWindow : EditorWindow
    {
        private BehaviorTreeAsset _tree;
        private BehaviorTreeRunner _debugRunner;
        private BehaviorTreeGraphView _graphView;
        private BehaviorTreeInspectorView _inspectorView;
        private BehaviorTreeBlackboardView _blackboardView;
        private VisualElement _validationBox;
        private ObjectField _treeField;
        private ObjectField _runnerField;

        [MenuItem("UPlayGround/AI/Behavior Tree Editor")]
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

            if (_tree == null && Selection.activeObject is BehaviorTreeAsset selectedTree)
                SetTree(selectedTree);
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            EditorApplication.update -= OnEditorUpdate;
        }

        private void ConstructLayout()
        {
            rootVisualElement.Clear();

            var toolbar = new Toolbar();

            _treeField = new ObjectField
            {
                objectType = typeof(BehaviorTreeAsset),
                allowSceneObjects = false,
                value = _tree
            };
            _treeField.style.width = 260f;
            _treeField.RegisterValueChangedCallback(evt => SetTree(evt.newValue as BehaviorTreeAsset));
            toolbar.Add(new Label("Tree"));
            toolbar.Add(_treeField);

            var createButton = new ToolbarButton(CreateTreeAsset) { text = "New" };
            toolbar.Add(createButton);

            var saveButton = new ToolbarButton(SaveTree) { text = "Save" };
            toolbar.Add(saveButton);

            var validateButton = new ToolbarButton(ValidateTree) { text = "Validate" };
            toolbar.Add(validateButton);

            toolbar.Add(new ToolbarSpacer { style = { flexGrow = 1 } });

            _runnerField = new ObjectField
            {
                objectType = typeof(BehaviorTreeRunner),
                allowSceneObjects = true,
                value = _debugRunner
            };
            _runnerField.style.width = 260f;
            _runnerField.RegisterValueChangedCallback(evt => _debugRunner = evt.newValue as BehaviorTreeRunner);
            toolbar.Add(new Label("Debug Runner"));
            toolbar.Add(_runnerField);

            rootVisualElement.Add(toolbar);

            var content = new TwoPaneSplitView(0, 640, TwoPaneSplitViewOrientation.Horizontal);
            rootVisualElement.Add(content);

            _graphView = new BehaviorTreeGraphView(this);
            content.Add(_graphView);

            var sidePanel = new TwoPaneSplitView(1, 260, TwoPaneSplitViewOrientation.Vertical);
            content.Add(sidePanel);

            _inspectorView = new BehaviorTreeInspectorView();
            sidePanel.Add(_inspectorView);

            var bottomPanel = new VisualElement();
            bottomPanel.style.flexGrow = 1;
            sidePanel.Add(bottomPanel);

            _blackboardView = new BehaviorTreeBlackboardView();
            bottomPanel.Add(_blackboardView);

            _validationBox = new VisualElement();
            _validationBox.style.marginTop = 6f;
            bottomPanel.Add(_validationBox);

            _graphView.PopulateView(_tree);
            _blackboardView.Bind(_tree);
            ValidateTree();
        }

        public void SetTree(BehaviorTreeAsset tree)
        {
            _tree = tree;
            if (_treeField != null)
                _treeField.SetValueWithoutNotify(_tree);

            _graphView?.PopulateView(_tree);
            _blackboardView?.Bind(_tree);
            _inspectorView?.UpdateSelection(null);
            ValidateTree();
        }

        public void RefreshInspector()
        {
            _inspectorView?.UpdateSelection(null);
            ValidateTree();
        }

        public void SelectNode(BTNode node)
        {
            _inspectorView?.UpdateSelection(node);
        }

        private void OnSelectionChanged()
        {
            if (Selection.activeObject is BehaviorTreeAsset selectedTree && selectedTree != _tree)
                SetTree(selectedTree);
        }

        private void OnEditorUpdate()
        {
            if (_graphView == null)
                return;

            var runtimeTree = Application.isPlaying && _debugRunner != null && _debugRunner.DebugMode
                ? _debugRunner.RuntimeTree
                : null;

            _graphView.UpdateDebugState(runtimeTree);
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
            foreach (var message in BehaviorTreeAssetValidator.Validate(_tree))
            {
                var label = new Label($"{message.Level}: {message.Message}");
                label.style.whiteSpace = WhiteSpace.Normal;
                label.style.marginBottom = 2f;
                label.style.color = message.Level switch
                {
                    BehaviorTreeValidationLevel.Error => new Color(0.95f, 0.35f, 0.35f),
                    BehaviorTreeValidationLevel.Warning => new Color(0.95f, 0.72f, 0.22f),
                    _ => new Color(0.55f, 0.85f, 0.55f)
                };
                _validationBox.Add(label);
            }
        }
    }
}
#endif
