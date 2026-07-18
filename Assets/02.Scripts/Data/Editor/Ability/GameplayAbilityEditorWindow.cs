using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;

namespace UPlayGround.Data.Editor.Ability
{
    public sealed class GameplayAbilityEditorWindow : EditorWindow
    {
        private readonly List<UnityEngine.Object> _assets = new();
        private readonly List<UnityEngine.Object> _filtered = new();
        private readonly List<AbilitySetScope> _setScopes = new();
        private ListView _assetList;
        private VisualElement _detail;
        private VisualElement _summary;
        private VisualElement _validation;
        private ToolbarSearchField _search;
        private ToolbarMenu _filterMenu;
        private Button _setScopeButton;
        private Label _nameLabel;
        private Label _pathLabel;
        private Button _pingButton;
        private UnityEngine.Object _selected;
        private string _filter = "전체";
        private AbilitySetSO _activeSet;
        private bool _scopeInitialized;
        private string _activeTab = "기본 정보";
        private VisualElement _main;
        private VisualElement _assetColumn;
        private VisualElement _toolbarRow;
        private VisualElement _tabsRow;
        private readonly Dictionary<string, Button> _tabButtons = new();

        private sealed class AbilitySetScope
        {
            public AbilitySetSO Set;
            public string SetName;
            public string OwnerText;
            public string Group;
            public string AssetPath;
            public string SearchText;
            public bool HasInputConnection;
            public bool HasBtConnection;
        }

        private sealed class AbilitySetScopePopup : PopupWindowContent
        {
            private readonly IReadOnlyList<AbilitySetScope> _scopes;
            private readonly AbilitySetSO _activeSet;
            private readonly Action<AbilitySetSO> _onSelected;
            private Vector2 _scroll;
            private string _search = string.Empty;
            private int _connectionFilter;
            private bool _focusSearch = true;

            public AbilitySetScopePopup(
                IReadOnlyList<AbilitySetScope> scopes,
                AbilitySetSO activeSet,
                Action<AbilitySetSO> onSelected)
            {
                _scopes = scopes;
                _activeSet = activeSet;
                _onSelected = onSelected;
            }

            public override Vector2 GetWindowSize() => new(600f, 520f);

            public override void OnGUI(Rect rect)
            {
                GUIStyle titleStyle = new(EditorStyles.boldLabel)
                {
                    fontSize = 13,
                };
                GUIStyle groupStyle = new(EditorStyles.boldLabel)
                {
                    fontSize = 11,
                    normal =
                    {
                        textColor = EditorGUIUtility.isProSkin
                            ? new Color(0.58f, 0.72f, 0.9f)
                            : new Color(0.18f, 0.36f, 0.62f),
                    },
                };

                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField("AbilitySet 선택", titleStyle);
                EditorGUILayout.LabelField(
                    "이름이나 연결 액터를 검색하세요. 분류는 데이터 타입이 아닌 연결 위치 기준입니다.",
                    EditorStyles.miniLabel);
                EditorGUILayout.Space(5f);

                GUI.SetNextControlName("AbilitySetScopeSearch");
                _search = EditorGUILayout.TextField(
                    _search,
                    EditorStyles.toolbarSearchField);
                if (_focusSearch && UnityEngine.Event.current.type == EventType.Repaint)
                {
                    EditorGUI.FocusTextInControl("AbilitySetScopeSearch");
                    _focusSearch = false;
                }

                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    DrawFilterButton("전체", 0);
                    DrawFilterButton("입력 연결", 1);
                    DrawFilterButton("BT 연결", 2);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label($"{VisibleScopeCount()}개", EditorStyles.miniLabel);
                }

                _scroll = EditorGUILayout.BeginScrollView(_scroll);
                DrawAllAbilitySetsRow();

                string lastGroup = null;
                foreach (AbilitySetScope scope in FilteredScopes())
                {
                    if (!string.Equals(lastGroup, scope.Group, StringComparison.Ordinal))
                    {
                        EditorGUILayout.Space(7f);
                        EditorGUILayout.LabelField(scope.Group, groupStyle);
                        lastGroup = scope.Group;
                    }

                    DrawScopeRow(scope);
                }
                EditorGUILayout.EndScrollView();
            }

            private void DrawFilterButton(string label, int filter)
            {
                bool selected = _connectionFilter == filter;
                bool pressed = GUILayout.Toggle(
                    selected,
                    label,
                    EditorStyles.toolbarButton,
                    GUILayout.Width(filter == 0 ? 52f : 76f));
                if (pressed && !selected)
                    _connectionFilter = filter;
            }

            private void DrawAllAbilitySetsRow()
            {
                Rect row = GUILayoutUtility.GetRect(0f, 38f, GUILayout.ExpandWidth(true));
                bool selected = _activeSet == null;
                if (selected)
                    EditorGUI.DrawRect(row, SelectionColor());

                Rect buttonRect = new(row.x + 4f, row.y, row.width - 8f, row.height);
                if (GUI.Button(buttonRect, GUIContent.none, GUIStyle.none))
                    Select(null);

                GUI.Label(
                    new Rect(row.x + 10f, row.y + 4f, row.width - 20f, 18f),
                    selected ? "✓  모든 AbilitySet" : "모든 AbilitySet",
                    EditorStyles.boldLabel);
                GUI.Label(
                    new Rect(row.x + 27f, row.y + 21f, row.width - 37f, 15f),
                    "프로젝트의 전체 Ability / Effect / Set 표시",
                    EditorStyles.miniLabel);
            }

            private void DrawScopeRow(AbilitySetScope scope)
            {
                Rect row = GUILayoutUtility.GetRect(0f, 48f, GUILayout.ExpandWidth(true));
                bool selected = scope.Set == _activeSet;
                if (selected)
                    EditorGUI.DrawRect(row, SelectionColor());
                else if (row.Contains(UnityEngine.Event.current.mousePosition))
                    EditorGUI.DrawRect(row, HoverColor());

                Rect selectRect = new(row.x + 4f, row.y, row.width - 65f, row.height);
                if (GUI.Button(selectRect, GUIContent.none, GUIStyle.none))
                    Select(scope.Set);

                string title = selected ? $"✓  {scope.SetName}" : scope.SetName;
                GUI.Label(
                    new Rect(row.x + 10f, row.y + 5f, row.width - 76f, 19f),
                    title,
                    EditorStyles.boldLabel);
                GUI.Label(
                    new Rect(row.x + 27f, row.y + 25f, row.width - 93f, 17f),
                    scope.OwnerText,
                    EditorStyles.miniLabel);

                Rect pingRect = new(row.xMax - 57f, row.y + 12f, 52f, 23f);
                if (GUI.Button(pingRect, "Ping", EditorStyles.miniButton))
                {
                    Selection.activeObject = scope.Set;
                    EditorGUIUtility.PingObject(scope.Set);
                }
            }

            private IEnumerable<AbilitySetScope> FilteredScopes()
            {
                string query = _search.Trim();
                for (int i = 0; i < _scopes.Count; i++)
                {
                    AbilitySetScope scope = _scopes[i];
                    if (_connectionFilter == 1 && !scope.HasInputConnection)
                        continue;
                    if (_connectionFilter == 2 && !scope.HasBtConnection)
                        continue;
                    if (query.Length > 0
                        && scope.SearchText.IndexOf(
                            query,
                            StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    yield return scope;
                }
            }

            private int VisibleScopeCount() => FilteredScopes().Count();

            private void Select(AbilitySetSO set)
            {
                _onSelected?.Invoke(set);
                editorWindow.Close();
            }

            private static Color SelectionColor() =>
                EditorGUIUtility.isProSkin
                    ? new Color(0.13f, 0.36f, 0.62f, 0.72f)
                    : new Color(0.45f, 0.68f, 0.92f, 0.55f);

            private static Color HoverColor() =>
                EditorGUIUtility.isProSkin
                    ? new Color(1f, 1f, 1f, 0.055f)
                    : new Color(0f, 0f, 0f, 0.045f);
        }

        private static readonly Color Bg0 = new(0.055f, 0.075f, 0.10f);
        private static readonly Color Bg1 = new(0.08f, 0.10f, 0.13f);
        private static readonly Color Bg2 = new(0.11f, 0.13f, 0.16f);
        private static readonly Color Border = new(0.22f, 0.27f, 0.32f);
        private static readonly Color Accent = new(0.18f, 0.52f, 0.92f);

        [MenuItem("UPlayGround/Ability/Ability & Effect 데이터 툴")]
        public static void Open()
        {
            GameplayAbilityEditorWindow window = GetWindow<GameplayAbilityEditorWindow>();
            window.titleContent = new GUIContent("Ability Editor");
            window.minSize = new Vector2(1050f, 650f);
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += HandleUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= HandleUndoRedo;
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.backgroundColor = Bg0;
            rootVisualElement.style.color = new Color(0.88f, 0.9f, 0.94f);
            BuildToolbar();
            BuildTabs();
            BuildMain();
            rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            RefreshAssets();
        }

        private void BuildToolbar()
        {
            var toolbar = new VisualElement();
            toolbar.style.height = 58f;
            toolbar.style.flexShrink = 0f;
            toolbar.style.backgroundColor = Bg2;
            toolbar.style.borderBottomColor = Border;
            toolbar.style.borderBottomWidth = 1f;

            var firstRowScroll = new ScrollView(ScrollViewMode.Horizontal);
            firstRowScroll.style.height = 31f;
            firstRowScroll.horizontalScrollerVisibility = ScrollerVisibility.Auto;
            firstRowScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            firstRowScroll.style.flexShrink = 0f;

            var firstRow = new Toolbar();
            _toolbarRow = firstRow;
            firstRow.style.height = 30f;
            firstRow.style.flexShrink = 0f;

            var title = new Label("선택 에셋");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginLeft = 8f;
            title.style.color = new Color(0.55f, 0.6f, 0.68f);
            firstRow.Add(title);

            _nameLabel = new Label("에셋을 선택하세요");
            _nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _nameLabel.style.marginLeft = 10f;
            _nameLabel.style.minWidth = 180f;
            _nameLabel.style.flexGrow = 1f;
            firstRow.Add(_nameLabel);

            firstRow.Add(MakeToolbarButton("새 Ability", () => CreateAsset<GameplayAbilitySO>("GA_")));
            firstRow.Add(MakeToolbarButton("새 Effect", () => CreateAsset<GameplayEffectSO>("GE_")));
            firstRow.Add(MakeToolbarButton("새 Set", () => CreateAsset<AbilitySetSO>("AbilitySet_")));
            firstRow.Add(MakeToolbarButton("전체 검증", ValidateAll));

            var delete = MakeToolbarButton("선택 삭제", DeleteSelected);
            delete.style.backgroundColor = new Color(0.45f, 0.12f, 0.12f);
            delete.style.color = new Color(1f, 0.82f, 0.82f);
            firstRow.Add(delete);

            var save = MakeToolbarButton("저장", SaveSelected);
            save.style.backgroundColor = Accent;
            save.style.color = Color.white;
            firstRow.Add(save);
            firstRowScroll.Add(firstRow);
            toolbar.Add(firstRowScroll);

            var secondRow = new VisualElement();
            secondRow.style.height = 26f;
            secondRow.style.flexDirection = FlexDirection.Row;
            secondRow.style.alignItems = Align.Center;
            secondRow.style.paddingLeft = 8f;
            secondRow.style.paddingRight = 6f;

            var fileLabel = new Label("파일");
            fileLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            fileLabel.style.color = new Color(0.55f, 0.6f, 0.68f);
            secondRow.Add(fileLabel);

            _pathLabel = new Label("-");
            _pathLabel.style.color = new Color(0.68f, 0.72f, 0.78f);
            _pathLabel.style.marginLeft = 10f;
            _pathLabel.style.flexGrow = 1f;
            _pathLabel.style.overflow = Overflow.Hidden;
            _pathLabel.style.textOverflow = TextOverflow.Ellipsis;
            secondRow.Add(_pathLabel);

            _pingButton = new Button(PingSelectedAsset) { text = "Project에서 찾기" };
            _pingButton.tooltip = "선택한 에셋을 Project 창에서 선택하고 강조 표시합니다.";
            _pingButton.style.height = 21f;
            _pingButton.style.flexShrink = 0f;
            _pingButton.SetEnabled(false);
            secondRow.Add(_pingButton);

            toolbar.Add(secondRow);
            rootVisualElement.Add(toolbar);
        }

        private void BuildTabs()
        {
            var tabScroll = new ScrollView(ScrollViewMode.Horizontal);
            tabScroll.style.height = 33f;
            tabScroll.style.flexShrink = 0f;
            tabScroll.horizontalScrollerVisibility = ScrollerVisibility.Auto;
            tabScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;

            var tabs = new VisualElement();
            _tabsRow = tabs;
            _tabButtons.Clear();
            tabs.style.flexDirection = FlexDirection.Row;
            tabs.style.height = 32f;
            tabs.style.flexShrink = 0f;
            tabs.style.backgroundColor = Bg1;
            tabs.style.borderBottomColor = Border;
            tabs.style.borderBottomWidth = 1f;

            string[] labels =
            {
                "기본 정보", "활성화 조건", "비용/쿨다운", "Variant",
                "Effect", "Cue", "저장/교체 정책", "정적 밸런스", "검증 결과",
            };
            for (int i = 0; i < labels.Length; i++)
            {
                string tab = labels[i];
                var button = new Button(() =>
                {
                    _activeTab = tab;
                    UpdateTabStyles();
                    RebuildDetail();
                }) { text = tab };
                button.tooltip = GetTabGeneralHelp(tab);
                // 클릭 후 파란 포커스 테두리가 선택 표시처럼 남지 않게 한다.
                // 활성 상태는 아래 UpdateTabStyles의 단일 표시만 사용한다.
                button.focusable = false;
                button.style.height = 27f;
                button.style.marginTop = 4f;
                button.style.marginLeft = 2f;
                button.style.flexShrink = 0f;
                _tabButtons[tab] = button;
                tabs.Add(button);
            }
            UpdateTabStyles();
            tabScroll.Add(tabs);
            rootVisualElement.Add(tabScroll);
        }

        private void UpdateTabStyles()
        {
            foreach (KeyValuePair<string, Button> pair in _tabButtons)
            {
                bool active = string.Equals(pair.Key, _activeTab, StringComparison.Ordinal);
                Button button = pair.Value;
                button.style.backgroundColor = active
                    ? new Color(0.12f, 0.22f, 0.32f)
                    : Bg2;
                button.style.color = active
                    ? Color.white
                    : new Color(0.72f, 0.76f, 0.82f);
                button.style.borderBottomColor = active ? Accent : Color.clear;
                button.style.borderBottomWidth = active ? 3f : 0f;
            }
        }

        private void BuildMain()
        {
            var mainSplit = new TwoPaneSplitView(0, 300f, TwoPaneSplitViewOrientation.Horizontal);
            _main = mainSplit;
            _main.style.flexGrow = 1f;
            _main.style.minWidth = 0f;
            _main.style.minHeight = 0f;
            _main.style.overflow = Overflow.Hidden;
            rootVisualElement.Add(_main);

            _assetColumn = BuildAssetColumn();
            mainSplit.Add(_assetColumn);

            var contentSplit = new TwoPaneSplitView(
                1,
                280f,
                TwoPaneSplitViewOrientation.Horizontal);
            contentSplit.style.flexGrow = 1f;
            contentSplit.style.minWidth = 0f;
            contentSplit.style.minHeight = 0f;
            mainSplit.Add(contentSplit);

            _detail = new ScrollView();
            _detail.style.flexGrow = 1f;
            _detail.style.minWidth = 360f;
            _detail.style.minHeight = 0f;
            _detail.style.paddingLeft = 12f;
            _detail.style.paddingRight = 12f;
            _detail.style.paddingTop = 8f;
            contentSplit.Add(_detail);

            _summary = new ScrollView();
            _summary.style.minWidth = 220f;
            _summary.style.backgroundColor = Bg1;
            _summary.style.borderLeftColor = Border;
            _summary.style.borderLeftWidth = 1f;
            _summary.style.paddingLeft = 10f;
            _summary.style.paddingRight = 10f;
            contentSplit.Add(_summary);
        }

        private VisualElement BuildAssetColumn()
        {
            var column = new VisualElement();
            column.style.width = 270f;
            column.style.flexShrink = 0f;
            column.style.minWidth = 220f;
            column.style.minHeight = 0f;
            column.style.backgroundColor = Bg1;
            column.style.borderRightColor = Border;
            column.style.borderRightWidth = 1f;

            var header = SectionHeader(
                "에셋 탐색",
                "AbilitySet 범위를 먼저 선택한 뒤 타입과 검색어로 결과를 좁힙니다.");
            column.Add(header);

            var scopeRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            scopeRow.style.paddingLeft = 6f;
            scopeRow.style.paddingRight = 6f;
            scopeRow.style.paddingTop = 6f;
            scopeRow.style.flexShrink = 0f;
            scopeRow.style.minWidth = 0f;
            scopeRow.style.overflow = Overflow.Hidden;
            var scopeLabel = new Label("AbilitySet");
            scopeLabel.tooltip = "캐릭터 프리팹에 부착된 AbilitySet과 그 Ability/Effect만 표시합니다.";
            scopeLabel.style.width = 68f;
            scopeLabel.style.flexShrink = 0f;
            scopeLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            scopeRow.Add(scopeLabel);
            _setScopeButton = new Button(OpenSetScopePopup)
            {
                text = "모든 AbilitySet  ▾",
                tooltip = "검색 가능한 목록에서 작업할 AbilitySet 범위를 선택합니다.",
            };
            _setScopeButton.style.flexGrow = 1f;
            _setScopeButton.style.flexShrink = 1f;
            _setScopeButton.style.flexBasis = 0f;
            _setScopeButton.style.minWidth = 0f;
            _setScopeButton.style.width = StyleKeyword.Auto;
            _setScopeButton.style.overflow = Overflow.Hidden;
            _setScopeButton.style.textOverflow = TextOverflow.Ellipsis;
            _setScopeButton.style.unityTextAlign = TextAnchor.MiddleLeft;
            scopeRow.Add(_setScopeButton);
            column.Add(scopeRow);

            var filters = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            filters.style.paddingLeft = 6f;
            filters.style.paddingRight = 6f;
            filters.style.paddingTop = 6f;
            filters.style.overflow = Overflow.Hidden;
            filters.style.flexShrink = 0f;
            _search = new ToolbarSearchField();
            _search.tooltip = "표시 이름, 안정 ID 또는 파일 이름으로 검색합니다.";
            _search.style.flexGrow = 1f;
            _search.style.flexShrink = 1f;
            _search.style.flexBasis = 0f;
            _search.style.minWidth = 0f;
            _search.style.width = StyleKeyword.Auto;
            _search.RegisterValueChangedCallback(_ => ApplyFilter());
            filters.Add(_search);

            _filterMenu = new ToolbarMenu { text = "전체" };
            _filterMenu.tooltip = "현재 AbilitySet 범위 안에서 에셋 타입을 좁힙니다.";
            _filterMenu.style.width = 62f;
            _filterMenu.style.minWidth = 62f;
            _filterMenu.style.maxWidth = 62f;
            _filterMenu.style.flexShrink = 0f;
            _filterMenu.style.marginLeft = 4f;
            foreach (string filter in new[] { "전체", "Ability", "Effect", "Set" })
            {
                string captured = filter;
                _filterMenu.menu.AppendAction(filter, _ =>
                {
                    _filter = captured;
                    _filterMenu.text = captured;
                    ApplyFilter();
                });
            }
            filters.Add(_filterMenu);
            column.Add(filters);

            _assetList = new ListView(_filtered, 48f, MakeAssetRow, BindAssetRow)
            {
                selectionType = SelectionType.Multiple,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                reorderable = false,
                showBorder = false,
            };
            _assetList.style.flexGrow = 1f;
            _assetList.selectionChanged += OnSelectionChanged;
            column.Add(_assetList);

            var selectionHint = new Label("Ctrl/Shift로 여러 개 선택 · Delete는 상단 '선택 삭제'");
            selectionHint.tooltip = "다중 선택 후 선택 삭제를 누르면 선택한 에셋을 한 번에 삭제합니다.";
            selectionHint.style.fontSize = 10f;
            selectionHint.style.color = new Color(0.55f, 0.6f, 0.68f);
            selectionHint.style.paddingLeft = 8f;
            selectionHint.style.paddingTop = 4f;
            selectionHint.style.paddingBottom = 5f;
            selectionHint.style.flexShrink = 0f;
            column.Add(selectionHint);

            // 마이그레이션 UI는 기능 완성 후 다시 노출한다.
            // 변환 구현은 보존하되 현재 저작 화면에서는 CRUD와 검증에 집중한다.
            return column;
        }

        private void OnRootGeometryChanged(GeometryChangedEvent evt)
        {
            float width = evt.newRect.width;
            if (_toolbarRow != null)
                _toolbarRow.style.minWidth = width;
            if (_tabsRow != null)
                _tabsRow.style.minWidth = width;
        }

        private VisualElement MakeAssetRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 7f;
            row.style.paddingRight = 5f;
            row.style.borderBottomColor = new Color(0.15f, 0.18f, 0.22f);
            row.style.borderBottomWidth = 1f;

            var icon = new Image { name = "icon", scaleMode = ScaleMode.ScaleToFit };
            icon.style.width = 34f;
            icon.style.height = 34f;
            icon.style.marginRight = 7f;
            row.Add(icon);

            var labels = new VisualElement();
            labels.style.flexGrow = 1f;
            var name = new Label { name = "name" };
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            labels.Add(name);
            var type = new Label { name = "type" };
            type.style.fontSize = 10f;
            type.style.color = new Color(0.55f, 0.6f, 0.68f);
            labels.Add(type);
            row.Add(labels);

            var badge = new Label { name = "badge" };
            badge.style.unityTextAlign = TextAnchor.MiddleCenter;
            badge.style.minWidth = 20f;
            row.Add(badge);
            return row;
        }

        private void BindAssetRow(VisualElement row, int index)
        {
            if ((uint)index >= (uint)_filtered.Count) return;
            UnityEngine.Object asset = _filtered[index];
            row.Q<Label>("name").text = GetStableId(asset);
            row.Q<Label>("type").text = asset.GetType().Name;
            row.Q<Image>("icon").image = GetIcon(asset);

            List<AbilityValidationIssue> issues = AbilityDataValidator.Validate(asset);
            int errors = issues.Count(x => x.Severity == AbilityValidationSeverity.Error);
            int warnings = issues.Count(x => x.Severity == AbilityValidationSeverity.Warning);
            Label badge = row.Q<Label>("badge");
            badge.text = errors > 0 ? $"✕ {errors}" : warnings > 0 ? $"⚠ {warnings}" : "✓";
            badge.style.color = errors > 0
                ? new Color(1f, 0.35f, 0.35f)
                : warnings > 0 ? new Color(1f, 0.75f, 0.2f) : new Color(0.35f, 0.85f, 0.55f);
        }

        private void OnSelectionChanged(IEnumerable<object> selected)
        {
            List<UnityEngine.Object> selection = selected
                .OfType<UnityEngine.Object>()
                .ToList();
            _selected = selection.FirstOrDefault();
            UpdateSelectedHeader(selection.Count);
            RebuildDetail();
        }

        private void UpdateSelectedHeader(int selectionCount = 1)
        {
            if (_nameLabel == null || _pathLabel == null) return;

            if (_selected == null)
            {
                _nameLabel.text = "에셋을 선택하세요";
                _pathLabel.text = "-";
                _pathLabel.tooltip = string.Empty;
                _pingButton?.SetEnabled(false);
                return;
            }

            string path = AssetDatabase.GetAssetPath(_selected);
            _nameLabel.text = selectionCount > 1
                ? $"{_selected.name}  (+{selectionCount - 1}개 선택)"
                : _selected.name;
            _nameLabel.tooltip = GetStableId(_selected);
            _pathLabel.text = path;
            _pathLabel.tooltip = path;
            _pingButton?.SetEnabled(!string.IsNullOrWhiteSpace(path));
        }

        private void PingSelectedAsset()
        {
            if (_selected == null) return;

            Selection.activeObject = _selected;
            EditorGUIUtility.PingObject(_selected);
            ShowNotification(new GUIContent($"Project에서 '{_selected.name}' 선택"));
        }

        private void RebuildDetail()
        {
            if (_detail == null || _summary == null) return;
            _detail.Clear();
            _summary.Clear();
            if (_selected == null)
            {
                var empty = new HelpBox(
                    "왼쪽에서 AbilitySet 범위를 선택하고 편집할 에셋을 고르세요.\n"
                    + "AbilitySet은 캐릭터의 전체 전투 구성, Ability는 개별 행동, "
                    + "Effect는 비용·버프·상태 변화를 정의합니다.",
                    HelpBoxMessageType.Info);
                empty.style.marginTop = 24f;
                _detail.Add(empty);
                return;
            }

            var serialized = new SerializedObject(_selected);
            // TrackSerializedObjectValue는 한 VisualElement가 수명 동안 하나의
            // SerializedObject만 추적할 수 있다. 탭 전환마다 재사용되는 _detail에
            // 직접 등록하지 않고, Rebuild 시 함께 폐기되는 컨테이너를 사용한다.
            var bindingRoot = new VisualElement();
            bindingRoot.Add(SectionHeader(
                $"{_activeTab} · {_selected.name}",
                GetTabHelp(_selected, _activeTab)));
            var tabHelp = new HelpBox(
                GetTabHelp(_selected, _activeTab),
                HelpBoxMessageType.Info);
            tabHelp.style.marginTop = 6f;
            bindingRoot.Add(tabHelp);
            _detail.Add(bindingRoot);

            string[] properties = GetPropertiesForTab(_selected, _activeTab);
            if (properties.Length == 0)
            {
                bindingRoot.Add(new HelpBox(
                    "이 에셋 타입에는 현재 탭에서 편집할 항목이 없습니다.",
                    HelpBoxMessageType.Info));
            }
            for (int i = 0; i < properties.Length; i++)
            {
                SerializedProperty property = serialized.FindProperty(properties[i]);
                if (property == null) continue;
                var field = new PropertyField(
                    property,
                    GetPropertyLabel(property.name));
                field.tooltip = GetPropertyHelp(property.name);
                field.style.marginTop = 5f;
                field.Bind(serialized);
                bindingRoot.Add(field);
            }

            bindingRoot.TrackSerializedObjectValue(serialized, _ =>
            {
                EditorUtility.SetDirty(_selected);
                RebuildSummary();
                RebuildValidation();
                _assetList?.RefreshItems();
            });
            RebuildSummary();
            RebuildValidation();
        }

        private void RebuildSummary()
        {
            _summary.Clear();
            _summary.Add(SectionHeader(
                "선택 요약",
                "현재 편집 중인 에셋의 핵심 값과 검증 상태를 빠르게 확인합니다."));
            AddSummary("에셋 타입", _selected?.GetType().Name ?? "-");
            AddSummary("안정 ID", GetStableId(_selected));

            if (_selected is GameplayAbilitySO ability)
            {
                AddSummary("분류", ability.presentation?.category.ToString());
                AddSummary("Variant", (ability.variants?.Count ?? 0).ToString());
                AddSummary("비용", $"{ability.cost?.resourceType} / {ability.cost?.policy}");
                AddSummary("쿨다운", $"{ability.cooldown?.durationSeconds:0.##}s");
                AddSummary("공유 그룹", ability.cooldown?.ResolveGroupId(ability.abilityId));
            }
            else if (_selected is GameplayEffectSO effect)
            {
                AddSummary("지속 타입", effect.durationType.ToString());
                AddSummary("지속 시간", $"{effect.durationSeconds:0.##}s");
                AddSummary("주기", effect.IsPeriodic ? $"{effect.periodSeconds:0.##}s" : "없음");
                AddSummary("최대 스택", effect.maxStackCount.ToString());
            }
            else if (_selected is AbilitySetSO set)
            {
                AddSummary("스킬 슬롯", (set.playerSlots?.Count ?? 0).ToString());
                AddSummary("전투 슬롯", (set.combatBindings?.Count ?? 0).ToString());
                AddSummary("차지 단계", (set.charge?.stages?.Count ?? 0).ToString());
                AddSummary("연계 라우트", (set.comboRoutes?.Count ?? 0).ToString());
            }

            _summary.Add(SectionHeader(
                "검증 상태",
                "오류는 실행을 막을 수 있고, 경고는 누락 가능성을 알려줍니다."));
            _validation = new VisualElement();
            _summary.Add(_validation);
        }

        private void RebuildValidation()
        {
            if (_validation == null) return;
            _validation.Clear();
            List<AbilityValidationIssue> issues = AbilityDataValidator.Validate(_selected);
            int errors = issues.Count(x => x.Severity == AbilityValidationSeverity.Error);
            int warnings = issues.Count(x => x.Severity == AbilityValidationSeverity.Warning);
            AddValidationLine($"✕ 오류 {errors}", new Color(1f, 0.35f, 0.35f));
            AddValidationLine($"⚠ 경고 {warnings}", new Color(1f, 0.75f, 0.2f));
            AddValidationLine($"ⓘ 정보 {issues.Count - errors - warnings}", new Color(0.35f, 0.65f, 1f));

            for (int i = 0; i < issues.Count; i++)
            {
                AbilityValidationIssue issue = issues[i];
                var box = new HelpBox(issue.Message, issue.Severity switch
                {
                    AbilityValidationSeverity.Error => HelpBoxMessageType.Error,
                    AbilityValidationSeverity.Warning => HelpBoxMessageType.Warning,
                    _ => HelpBoxMessageType.Info,
                });
                box.style.marginTop = 4f;
                _validation.Add(box);
            }
        }

        private void RefreshAssets()
        {
            _assets.Clear();
            LoadAssets<GameplayAbilitySO>();
            LoadAssets<GameplayEffectSO>();
            LoadAssets<AbilitySetSO>();
            _assets.Sort((a, b) => string.Compare(GetStableId(a), GetStableId(b), StringComparison.Ordinal));
            LoadAbilitySetScopes();
            RefreshSetScopeButton();
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            _filtered.Clear();
            string query = _search?.value?.Trim() ?? string.Empty;
            HashSet<UnityEngine.Object> scopedAssets = BuildScopedAssetSet();
            for (int i = 0; i < _assets.Count; i++)
            {
                UnityEngine.Object asset = _assets[i];
                if (scopedAssets != null && !scopedAssets.Contains(asset)) continue;
                if (!MatchesType(asset)) continue;
                if (!string.IsNullOrEmpty(query)
                    && GetStableId(asset).IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0
                    && asset.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                _filtered.Add(asset);
            }
            _assetList?.RefreshItems();
            RestoreListSelection();
        }

        private void RestoreListSelection()
        {
            if (_assetList == null) return;

            if (_filtered.Count == 0)
            {
                _assetList.ClearSelection();
                _selected = null;
                UpdateSelectedHeader(0);
                RebuildDetail();
                return;
            }

            int selectedIndex = _selected != null ? _filtered.IndexOf(_selected) : -1;
            if (selectedIndex < 0)
            {
                selectedIndex = 0;
                _selected = _filtered[0];
            }

            _assetList.SetSelection(selectedIndex);
            _assetList.ScrollToItem(selectedIndex);
            UpdateSelectedHeader();
            RebuildDetail();
        }

        private HashSet<UnityEngine.Object> BuildScopedAssetSet()
        {
            if (_activeSet == null) return null;

            var result = new HashSet<UnityEngine.Object> { _activeSet };
            foreach (GameplayAbilitySO ability in _activeSet.EnumerateAll())
            {
                if (ability == null || !result.Add(ability)) continue;
                AddEffects(result, ability.commitEffects);
                AddEffects(result, ability.endEffects);
            }
            return result;
        }

        private static void AddEffects(
            HashSet<UnityEngine.Object> result,
            List<GameplayEffectSO> effects)
        {
            if (effects == null) return;
            for (int i = 0; i < effects.Count; i++)
                if (effects[i] != null)
                    result.Add(effects[i]);
        }

        private void LoadAbilitySetScopes()
        {
            _setScopes.Clear();
            var ownersBySet = new Dictionary<AbilitySetSO, HashSet<string>>();
            var inputConnectedSets = new HashSet<AbilitySetSO>();
            var btConnectedSets = new HashSet<AbilitySetSO>();
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                MonoBehaviour[] behaviours = prefab.GetComponentsInChildren<MonoBehaviour>(true);
                for (int j = 0; j < behaviours.Length; j++)
                {
                    MonoBehaviour behaviour = behaviours[j];
                    if (behaviour == null) continue;

                    var serialized = new SerializedObject(behaviour);
                    SerializedProperty setProperty = serialized.FindProperty("abilitySet");
                    SerializedProperty characterProperty = serialized.FindProperty("characterType");
                    if (setProperty?.objectReferenceValue is not AbilitySetSO set
                        || characterProperty == null
                        || characterProperty.propertyType != SerializedPropertyType.Enum)
                        continue;

                    string owner = characterProperty.enumDisplayNames[
                        characterProperty.enumValueIndex];
                    if (!ownersBySet.TryGetValue(set, out HashSet<string> owners))
                    {
                        owners = new HashSet<string>(StringComparer.Ordinal);
                        ownersBySet.Add(set, owners);
                    }
                    owners.Add(owner);
                    inputConnectedSets.Add(set);
                }
            }

            string[] profileGuids = AssetDatabase.FindAssets(
                $"t:{nameof(MonsterActorProfileSO)}");
            for (int i = 0; i < profileGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(profileGuids[i]);
                MonsterActorProfileSO profile =
                    AssetDatabase.LoadAssetAtPath<MonsterActorProfileSO>(path);
                if (profile?.abilitySet == null) continue;

                if (!ownersBySet.TryGetValue(
                        profile.abilitySet,
                        out HashSet<string> owners))
                {
                    owners = new HashSet<string>(StringComparer.Ordinal);
                    ownersBySet.Add(profile.abilitySet, owners);
                }
                owners.Add(profile.name.Replace("MonsterProfile_", string.Empty));
                btConnectedSets.Add(profile.abilitySet);
            }

            foreach (AbilitySetSO set in _assets.OfType<AbilitySetSO>())
            {
                string ownerText = ownersBySet.TryGetValue(set, out HashSet<string> owners)
                    ? string.Join(", ", owners.OrderBy(x => x, StringComparer.Ordinal))
                    : "미부착";
                bool hasInputConnection = inputConnectedSets.Contains(set);
                bool hasBtConnection = btConnectedSets.Contains(set);
                string group = hasInputConnection && hasBtConnection
                    ? "공용 연결"
                    : hasInputConnection
                        ? "입력 연결"
                        : hasBtConnection
                            ? "BT 연결"
                            : "미부착";
                string assetPath = AssetDatabase.GetAssetPath(set);
                _setScopes.Add(new AbilitySetScope
                {
                    Set = set,
                    SetName = set.name,
                    OwnerText = ownerText,
                    Group = group,
                    AssetPath = assetPath,
                    SearchText = $"{set.name} {ownerText} {assetPath}",
                    HasInputConnection = hasInputConnection,
                    HasBtConnection = hasBtConnection,
                });
            }
            _setScopes.Sort((a, b) =>
            {
                int groupCompare = GroupOrder(a.Group).CompareTo(GroupOrder(b.Group));
                return groupCompare != 0
                    ? groupCompare
                    : string.Compare(a.SetName, b.SetName, StringComparison.Ordinal);
            });
            if (!_scopeInitialized)
            {
                _activeSet = _setScopes
                    .FirstOrDefault(x => x.HasInputConnection || x.HasBtConnection)
                    ?.Set
                    ?? _setScopes.FirstOrDefault()?.Set;
                _scopeInitialized = true;
            }
            else if (_activeSet != null && _setScopes.All(x => x.Set != _activeSet))
                _activeSet = null;
        }

        private static int GroupOrder(string group) => group switch
        {
            "공용 연결" => 0,
            "입력 연결" => 1,
            "BT 연결" => 2,
            _ => 3,
        };

        private void RefreshSetScopeButton()
        {
            if (_setScopeButton == null) return;
            AbilitySetScope activeScope = _setScopes.FirstOrDefault(x => x.Set == _activeSet);
            _setScopeButton.text = $"{activeScope?.SetName ?? "모든 AbilitySet"}  ▾";
            _setScopeButton.tooltip = activeScope == null
                ? "프로젝트의 모든 Ability 에셋을 표시합니다."
                : $"{activeScope.OwnerText}\n{activeScope.AssetPath}";
        }

        private void OpenSetScopePopup()
        {
            if (_setScopeButton == null) return;
            UnityEditor.PopupWindow.Show(
                _setScopeButton.worldBound,
                new AbilitySetScopePopup(_setScopes, _activeSet, SelectSetScope));
        }

        private void SelectSetScope(AbilitySetSO set)
        {
            _activeSet = set;
            RefreshSetScopeButton();
            ApplyFilter();
        }

        private bool MatchesType(UnityEngine.Object asset) => _filter switch
        {
            "Ability" => asset is GameplayAbilitySO,
            "Effect" => asset is GameplayEffectSO,
            "Set" => asset is AbilitySetSO,
            _ => true,
        };

        private void LoadAssets<T>() where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            for (int i = 0; i < guids.Length; i++)
            {
                T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (asset != null) _assets.Add(asset);
            }
        }

        private void CreateAsset<T>(string prefix) where T : ScriptableObject
        {
            string path = EditorUtility.SaveFilePanelInProject(
                $"{typeof(T).Name} 생성", prefix, "asset", "저장 위치를 선택하세요.",
                "Assets/10.Datas/Ability");
            if (string.IsNullOrEmpty(path)) return;
            T asset = CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            Undo.RegisterCreatedObjectUndo(asset, $"{typeof(T).Name} 생성");
            AssetDatabase.SaveAssets();
            RefreshAssets();
            Selection.activeObject = asset;
            _selected = asset;
            UpdateSelectedHeader();
            RebuildDetail();
        }

        private void SaveSelected()
        {
            IEnumerable<UnityEngine.Object> selected = _assetList?.selectedItems
                .OfType<UnityEngine.Object>()
                ?? Enumerable.Empty<UnityEngine.Object>();
            foreach (UnityEngine.Object asset in selected)
                EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            RebuildValidation();
            ShowNotification(new GUIContent("저장 및 검증 완료"));
        }

        private void DeleteSelected()
        {
            List<UnityEngine.Object> selected = _assetList?.selectedItems
                .OfType<UnityEngine.Object>()
                .Distinct()
                .ToList()
                ?? new List<UnityEngine.Object>();
            if (selected.Count == 0)
            {
                ShowNotification(new GUIContent("삭제할 에셋을 선택하세요."));
                return;
            }

            var targets = new List<(UnityEngine.Object Asset, string Path)>();
            var invalidNames = new List<string>();
            for (int i = 0; i < selected.Count; i++)
            {
                UnityEngine.Object asset = selected[i];
                string assetPath = AssetDatabase.GetAssetPath(asset);
                if (string.IsNullOrWhiteSpace(assetPath)
                    || !AssetDatabase.IsMainAsset(asset))
                {
                    invalidNames.Add(asset.name);
                    continue;
                }
                targets.Add((asset, assetPath));
            }
            if (invalidNames.Count > 0)
            {
                EditorUtility.DisplayDialog(
                    "삭제할 수 없음",
                    "프로젝트의 메인 Ability/Effect/Set 에셋만 삭제할 수 있습니다.\n\n"
                    + string.Join("\n", invalidNames),
                    "확인");
                return;
            }

            var targetPaths = new HashSet<string>(
                targets.Select(x => x.Path),
                StringComparer.Ordinal);
            List<string> references = targets
                .SelectMany(x => FindReferencingAssetPaths(x.Path))
                .Where(path => !targetPaths.Contains(path))
                .Distinct()
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();
            string targetPreview = string.Join(
                "\n",
                targets.Take(8).Select(x => $"• {x.Asset.name}"));
            if (targets.Count > 8)
                targetPreview += $"\n… 외 {targets.Count - 8}개";
            string referenceText = references.Count == 0
                ? "선택한 에셋을 영구 삭제합니다. 이 작업은 Undo로 복원할 수 없습니다."
                : $"선택한 에셋을 참조하는 외부 에셋이 {references.Count}개 있습니다.\n\n"
                  + string.Join("\n", references.Take(6))
                  + (references.Count > 6 ? "\n…" : string.Empty)
                  + "\n\n삭제하면 해당 참조가 Missing 상태가 될 수 있습니다.";

            int choice;
            if (references.Count == 0)
            {
                choice = EditorUtility.DisplayDialog(
                    $"에셋 {targets.Count}개 삭제",
                    $"{targetPreview}\n\n{referenceText}",
                    $"{targets.Count}개 삭제",
                    "취소")
                    ? 0
                    : 1;
            }
            else
            {
                choice = EditorUtility.DisplayDialogComplex(
                    $"참조된 에셋 {targets.Count}개 삭제",
                    $"{targetPreview}\n\n{referenceText}",
                    "참조 무시하고 삭제",
                    "취소",
                    "첫 참조 선택");
            }
            if (choice == 2 && references.Count > 0)
            {
                Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(references[0]);
                EditorGUIUtility.PingObject(Selection.activeObject);
                return;
            }
            if (choice != 0) return;

            _selected = null;
            _assetList?.ClearSelection();
            var failedPaths = new List<string>();
            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < targets.Count; i++)
                    if (!AssetDatabase.DeleteAsset(targets[i].Path))
                        failedPaths.Add(targets[i].Path);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            RefreshAssets();
            if (failedPaths.Count > 0)
            {
                EditorUtility.DisplayDialog(
                    "일부 삭제 실패",
                    $"{targets.Count - failedPaths.Count}개 삭제, "
                    + $"{failedPaths.Count}개 실패\n\n"
                    + string.Join("\n", failedPaths),
                    "확인");
                return;
            }

            ShowNotification(new GUIContent($"{targets.Count}개 에셋 삭제 완료"));
        }

        private static List<string> FindReferencingAssetPaths(string targetPath)
        {
            var result = new List<string>();
            string[] allPaths = AssetDatabase.GetAllAssetPaths();
            for (int i = 0; i < allPaths.Length; i++)
            {
                string candidate = allPaths[i];
                if (candidate == targetPath
                    || !candidate.StartsWith("Assets/", StringComparison.Ordinal)
                    || AssetDatabase.IsValidFolder(candidate))
                    continue;

                string extension = System.IO.Path.GetExtension(candidate);
                if (extension is not (".asset" or ".prefab" or ".unity"))
                    continue;

                string[] dependencies = AssetDatabase.GetDependencies(candidate, false);
                for (int j = 0; j < dependencies.Length; j++)
                {
                    if (!string.Equals(
                            dependencies[j], targetPath, StringComparison.Ordinal))
                        continue;
                    result.Add(candidate);
                    break;
                }
            }
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        private void ValidateAll()
        {
            List<AbilityValidationIssue> issues = AbilityDataValidator.ValidateAll();
            int errors = issues.Count(x => x.Severity == AbilityValidationSeverity.Error);
            int warnings = issues.Count(x => x.Severity == AbilityValidationSeverity.Warning);
            Debug.Log($"[AbilityValidator] 완료: 오류 {errors}, 경고 {warnings}, 전체 {issues.Count}");
            for (int i = 0; i < issues.Count; i++)
            {
                AbilityValidationIssue issue = issues[i];
                if (issue.Severity == AbilityValidationSeverity.Error)
                    Debug.LogError(issue.Message, issue.Context);
                else if (issue.Severity == AbilityValidationSeverity.Warning)
                    Debug.LogWarning(issue.Message, issue.Context);
            }
            RefreshAssets();
            RebuildValidation();
        }

        private void HandleUndoRedo()
        {
            RefreshAssets();
            RebuildDetail();
        }

        private static string[] GetPropertiesForTab(UnityEngine.Object target, string tab)
        {
            if (target is GameplayAbilitySO)
            {
                return tab switch
                {
                    "기본 정보" => new[] { "abilityId", "schemaVersion", "presentation", "abilityTagIds", "concurrency" },
                    "활성화 조건" => new[] { "activation" },
                    "비용/쿨다운" => new[] { "cost", "cooldown" },
                    "Variant" => new[] { "variants" },
                    "Effect" => new[] { "commitEffects", "endEffects" },
                    "Cue" => new[] { "cues" },
                    "저장/교체 정책" => new[] { "persistence" },
                    "정적 밸런스" => new[] { "balance" },
                    _ => Array.Empty<string>(),
                };
            }
            if (target is GameplayEffectSO)
            {
                return tab switch
                {
                    "기본 정보" => new[] { "effectId", "schemaVersion", "durationType", "durationSeconds", "periodSeconds" },
                    "Effect" => new[] { "stackingKey", "stackPolicy", "maxStackCount", "modifiers", "resourceOperations", "grantedTagIds" },
                    "저장/교체 정책" => new[] { "removalPolicy", "savePolicy" },
                    _ => Array.Empty<string>(),
                };
            }
            if (target is AbilitySetSO)
            {
                return tab switch
                {
                    "기본 정보" => new[]
                    {
                        "playerSlots",
                        "combatBindings",
                        "additionalAbilities",
                    },
                    "활성화 조건" => new[]
                    {
                        "comboRoutes",
                        "comboLinkWindow",
                    },
                    "Variant" => new[] { "charge" },
                    _ => Array.Empty<string>(),
                };
            }
            return Array.Empty<string>();
        }

        private static string GetStableId(UnityEngine.Object asset) => asset switch
        {
            GameplayAbilitySO ability => string.IsNullOrWhiteSpace(ability.abilityId) ? ability.name : ability.abilityId,
            GameplayEffectSO effect => string.IsNullOrWhiteSpace(effect.effectId) ? effect.name : effect.effectId,
            _ => asset != null ? asset.name : "-",
        };

        private static Texture GetIcon(UnityEngine.Object asset)
        {
            if (asset is GameplayAbilitySO ability && ability.presentation?.icon != null)
                return ability.presentation.icon.texture;
            return AssetPreview.GetMiniThumbnail(asset);
        }

        private static string GetTabGeneralHelp(string tab) => tab switch
        {
            "기본 정보" => "식별자, 화면 표시, 슬롯 구성 등 에셋의 기본 구조를 편집합니다.",
            "활성화 조건" => "Ability 발동 조건 또는 AbilitySet의 콤보 연결 조건을 설정합니다.",
            "비용/쿨다운" => "자원 소모 시점과 재사용 대기시간을 설정합니다.",
            "Variant" => "상황별 실행 Variant 또는 차지 단계별 Ability를 구성합니다.",
            "Effect" => "발동·종료 Effect와 Effect가 적용할 수치 변화를 설정합니다.",
            "Cue" => "Ability 실행 시 사용할 시각·청각 피드백 식별자를 설정합니다.",
            "저장/교체 정책" => "캐릭터 교체, 저장, 종료 시 유지하거나 제거할 범위를 설정합니다.",
            "정적 밸런스" => "밸런스 도구가 사용하는 기대 피해량과 메타데이터를 설정합니다.",
            "검증 결과" => "현재 에셋의 오류와 경고를 확인합니다.",
            _ => "현재 탭의 데이터를 편집합니다.",
        };

        private static string GetTabHelp(UnityEngine.Object target, string tab)
        {
            string typeGuide = target switch
            {
                AbilitySetSO => "AbilitySet은 한 캐릭터의 슬롯, 일반 공격, 차지, 콤보 구성을 묶습니다. ",
                GameplayAbilitySO => "Ability는 입력 한 번으로 실행되는 행동과 그 조건·비용을 정의합니다. ",
                GameplayEffectSO => "Effect는 지속 시간, 스택, 자원·스탯 변화를 정의합니다. ",
                _ => string.Empty,
            };
            return typeGuide + GetTabGeneralHelp(tab);
        }

        private static string GetPropertyLabel(string propertyName) => propertyName switch
        {
            "abilityId" => "Ability ID",
            "effectId" => "Effect ID",
            "schemaVersion" => "스키마 버전",
            "presentation" => "표시 정보",
            "abilityTagIds" => "Ability 태그",
            "concurrency" => "동시 실행 정책",
            "activation" => "활성화 조건",
            "cost" => "비용",
            "cooldown" => "쿨다운",
            "variants" => "실행 Variant",
            "commitEffects" => "발동 시 Effect",
            "endEffects" => "종료 시 Effect",
            "cues" => "연출 Cue",
            "persistence" => "저장·교체 정책",
            "balance" => "정적 밸런스",
            "durationType" => "지속 방식",
            "durationSeconds" => "지속 시간(초)",
            "periodSeconds" => "주기(초)",
            "stackingKey" => "스택 그룹 키",
            "stackPolicy" => "스택 정책",
            "maxStackCount" => "최대 스택",
            "modifiers" => "스탯 변경",
            "resourceOperations" => "자원 변경",
            "grantedTagIds" => "부여 태그",
            "removalPolicy" => "제거 정책",
            "savePolicy" => "저장 정책",
            "playerSlots" => "스킬 슬롯",
            "combatBindings" => "일반 공격 슬롯",
            "additionalAbilities" => "공용 Ability",
            "comboRoutes" => "콤보 연계",
            "comboLinkWindow" => "콤보 입력 허용 시간",
            "charge" => "차지 단계",
            _ => ObjectNames.NicifyVariableName(propertyName),
        };

        private static string GetPropertyHelp(string propertyName) => propertyName switch
        {
            "abilityId" or "effectId" => "저장 파일명과 별개인 런타임 고유 식별자입니다.",
            "presentation" => "이름, 설명, 아이콘, HUD 색상과 분류를 설정합니다.",
            "abilityTagIds" or "grantedTagIds" => "조건 판정과 다른 시스템 연동에 사용하는 태그입니다.",
            "concurrency" => "같은 Ability가 이미 실행 중일 때 새 요청을 처리하는 방법입니다.",
            "activation" => "필요·차단 태그와 활성화 규칙을 설정합니다.",
            "cost" => "소모 자원, 소모량, 실제 차감 시점을 설정합니다.",
            "cooldown" => "재사용 대기시간과 공유 쿨다운 그룹을 설정합니다.",
            "variants" => "조건에 따라 선택할 실제 실행 Payload 목록입니다.",
            "commitEffects" => "Ability가 확정될 때 적용할 Effect입니다.",
            "endEffects" => "Ability 실행이 끝날 때 적용할 Effect입니다.",
            "playerSlots" => "스킬 입력 슬롯과 Ability의 연결입니다.",
            "combatBindings" => "일반 공격 종류별 순차 Ability 목록입니다.",
            "additionalAbilities" => "입력 슬롯 또는 전투 슬롯과 무관하게 이 AbilitySet이 액터에게 부여할 Ability입니다. BT도 이 목록의 Ability를 활성화할 수 있습니다.",
            "comboRoutes" => "선행 공격 이후 연결 가능한 후속 Ability를 설정합니다.",
            "charge" => "차지 시간 단계별로 실행할 Ability를 설정합니다.",
            _ => "값을 변경하면 오른쪽 검증 상태가 자동으로 갱신됩니다.",
        };

        private static ToolbarButton MakeToolbarButton(string text, Action action)
        {
            var button = new ToolbarButton(action) { text = text };
            button.style.marginLeft = 3f;
            return button;
        }

        private static VisualElement SectionHeader(string text, string tooltip = null)
        {
            var label = new Label(text);
            label.tooltip = tooltip;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.backgroundColor = Bg2;
            label.style.paddingLeft = 8f;
            label.style.paddingTop = 6f;
            label.style.paddingBottom = 6f;
            label.style.borderBottomColor = Border;
            label.style.borderBottomWidth = 1f;
            return label;
        }

        private void AddSummary(string label, string value)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            row.style.paddingTop = 4f;
            row.style.paddingBottom = 4f;
            var key = new Label(label);
            key.style.width = 78f;
            key.style.color = new Color(0.55f, 0.6f, 0.68f);
            row.Add(key);
            var val = new Label(value ?? "-");
            val.style.flexGrow = 1f;
            val.style.whiteSpace = WhiteSpace.Normal;
            row.Add(val);
            _summary.Add(row);
        }

        private void AddValidationLine(string text, Color color)
        {
            var label = new Label(text);
            label.style.color = color;
            label.style.marginTop = 4f;
            _validation.Add(label);
        }
    }
}
