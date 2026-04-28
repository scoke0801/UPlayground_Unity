#if UNITY_EDITOR
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
        private Label _graphTitleLabel;
        private Label _graphSubtitleLabel;
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
            rootVisualElement.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f);

            var toolbar = new Toolbar();
            toolbar.style.height = 28f;
            toolbar.style.backgroundColor = new Color(0.16f, 0.16f, 0.16f);

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

            toolbar.Add(new ToolbarButton(CreateTreeAsset) { text = "New" });
            toolbar.Add(new ToolbarButton(SaveTree) { text = "Save" });
            toolbar.Add(new ToolbarButton(ValidateTree) { text = "Validate" });
            toolbar.Add(new ToolbarButton(BehaviorTreeJsonUtility.ImportJson) { text = "Import Json" });
            toolbar.Add(new ToolbarButton(BehaviorTreeJsonUtility.ExportSelected) { text = "Export Json" });

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

            var inspectorScroll = new ScrollView();
            inspectorScroll.style.flexGrow = 1;
            _inspectorView = new BehaviorTreeInspectorView();
            inspectorScroll.Add(_inspectorView);
            sidePanel.Add(CreateSection("Node Inspector", inspectorScroll, 260f));

            _blackboardView = new BehaviorTreeBlackboardView();
            sidePanel.Add(CreateSection("Shared Variables", _blackboardView, 260f));

            _validationBox = new VisualElement();
            _validationBox.style.flexGrow = 1;
            sidePanel.Add(CreateSection("Errors", _validationBox, 120f));

            _graphView.PopulateView(_tree);
            _blackboardView.Bind(_tree);
            RefreshGraphTitle();
            ValidateTree();
        }

        private static VisualElement CreateSidePanel()
        {
            var sidePanel = new VisualElement();
            sidePanel.style.flexGrow = 1;
            sidePanel.style.minWidth = 280f;
            sidePanel.style.backgroundColor = new Color(0.17f, 0.17f, 0.17f);
            sidePanel.style.borderRightColor = new Color(0.05f, 0.05f, 0.05f);
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
            header.style.backgroundColor = new Color(0.10f, 0.10f, 0.10f, 0.78f);
            header.style.borderTopLeftRadius = 4f;
            header.style.borderTopRightRadius = 4f;
            header.style.borderBottomLeftRadius = 4f;
            header.style.borderBottomRightRadius = 4f;

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
            if (_treeField != null)
                _treeField.SetValueWithoutNotify(_tree);

            _graphView?.PopulateView(_tree);
            _blackboardView?.Bind(_tree);
            _inspectorView?.UpdateSelection(null);
            RefreshGraphTitle();
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
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.marginBottom = 4f;
                row.style.paddingLeft = 6f;
                row.style.paddingRight = 6f;
                row.style.paddingTop = 4f;
                row.style.paddingBottom = 4f;
                row.style.backgroundColor = new Color(0.14f, 0.14f, 0.14f);
                row.style.borderTopLeftRadius = 3f;
                row.style.borderTopRightRadius = 3f;
                row.style.borderBottomLeftRadius = 3f;
                row.style.borderBottomRightRadius = 3f;

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
        }

        private void RefreshGraphTitle()
        {
            if (_graphTitleLabel == null || _graphSubtitleLabel == null)
                return;

            _graphTitleLabel.text = _tree != null ? _tree.name : "No Behavior Tree";
            _graphSubtitleLabel.text = _tree != null
                ? $"Nodes {_tree.Nodes.Count}  |  Root {(_tree.RootNode != null ? _tree.RootNode.DisplayName : "None")}"
                : "Select or import a BehaviorTreeAsset";
        }
    }
}
#endif
