using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Ability.Core;
using UPlayGround.Animation;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Editor.Ability.Production;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Editor.Ability
{
    public sealed class GameplayAbilityEditorWindow : EditorWindow
    {
        private readonly List<UnityEngine.Object> _assets = new();
        private readonly List<UnityEngine.Object> _filtered = new();
        private readonly Dictionary<System.Type, int> _filteredGroupCounts = new();
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
        private Button _composeSetButton;
        private Button _duplicateButton;
        private Button _copyTabButton;
        private Button _pasteTabButton;
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
        private ScriptableObject _tabClipboard;
        private Type _tabClipboardType;
        private string _tabClipboardTab;
        private VisualElement _variantPayloadSection;
        private string _variantPayloadSignature;
        private AbilityMotionIndex _abilityMotionIndex;
        private readonly Dictionary<string, bool> _payloadFoldoutStates = new();

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
            public readonly HashSet<CharacterActorType> CharacterTypes = new();
        }

        private sealed class MotionMappingOption
        {
            public string Label;
            public MotionKey SourceKey;
            public MotionSetAsset Motion;
            public bool Inherited;
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
        private static readonly string[] AbilitySetOwnerPrefabSearchFolders =
        {
            "Assets/03.Prefabs/Actor",
            "Assets/03.Prefabs/Characters/Enemy",
        };

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/게임플레이/Ability Editor")]
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
            if (_tabClipboard != null)
                DestroyImmediate(_tabClipboard);
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
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnEditorShortcut);
            RefreshAssets();
        }

        private void BuildToolbar()
        {
            var toolbar = new VisualElement();
            toolbar.style.height = 62f;
            toolbar.style.flexShrink = 0f;
            toolbar.style.backgroundColor = Bg2;
            toolbar.style.borderBottomColor = Border;
            toolbar.style.borderBottomWidth = 1f;

            var firstRow = new VisualElement();
            firstRow.style.height = 30f;
            firstRow.style.flexShrink = 0f;
            firstRow.style.flexDirection = FlexDirection.Row;
            firstRow.style.alignItems = Align.Center;
            firstRow.style.paddingLeft = 8f;
            firstRow.style.paddingRight = 8f;

            var title = new Label("선택 에셋");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.flexShrink = 0f;
            title.style.color = new Color(0.55f, 0.6f, 0.68f);
            firstRow.Add(title);

            _nameLabel = new Label("에셋을 선택하세요");
            _nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _nameLabel.style.marginLeft = 10f;
            _nameLabel.style.width = 330f;
            _nameLabel.style.minWidth = 180f;
            _nameLabel.style.flexShrink = 1f;
            _nameLabel.style.overflow = Overflow.Hidden;
            _nameLabel.style.textOverflow = TextOverflow.Ellipsis;
            firstRow.Add(_nameLabel);

            var fileLabel = new Label("파일");
            fileLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            fileLabel.style.color = new Color(0.55f, 0.6f, 0.68f);
            fileLabel.style.marginLeft = 14f;
            fileLabel.style.flexShrink = 0f;
            firstRow.Add(fileLabel);

            _pathLabel = new Label("-");
            _pathLabel.style.color = new Color(0.68f, 0.72f, 0.78f);
            _pathLabel.style.marginLeft = 8f;
            _pathLabel.style.flexGrow = 1f;
            _pathLabel.style.minWidth = 0f;
            _pathLabel.style.overflow = Overflow.Hidden;
            _pathLabel.style.textOverflow = TextOverflow.Ellipsis;
            firstRow.Add(_pathLabel);
            toolbar.Add(firstRow);

            var actionScroll = new ScrollView(ScrollViewMode.Horizontal);
            actionScroll.style.height = 31f;
            actionScroll.horizontalScrollerVisibility = ScrollerVisibility.Auto;
            actionScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            actionScroll.style.flexShrink = 0f;

            var actions = new Toolbar();
            _toolbarRow = actions;
            actions.style.height = 30f;
            actions.style.flexShrink = 0f;
            actions.style.paddingLeft = 5f;
            actions.style.paddingRight = 5f;

            ToolbarMenu createMenu = MakeToolbarMenu(
                "＋ 생성",
                "새 에셋 생성과 Ability 양산화 도구를 엽니다.");
            createMenu.menu.AppendAction(
                "Gameplay Ability",
                _ => CreateAsset<GameplayAbilitySO>("GA_"));
            createMenu.menu.AppendAction(
                "Passive Ability",
                _ => CreateAsset<PassiveAbilitySO>("PA_"));
            createMenu.menu.AppendAction(
                "Gameplay Effect",
                _ => CreateAsset<GameplayEffectSO>("GE_"));
            createMenu.menu.AppendAction(
                "Ability Set",
                _ => CreateAsset<AbilitySetSO>("AbilitySet_"));
            createMenu.menu.AppendSeparator();
            createMenu.menu.AppendAction(
                "공용/파생 Set 구성…",
                _ => OpenSetCompositionForSelection());
            createMenu.menu.AppendAction(
                "레시피로 신규 Ability 생성…",
                _ => GameplayAbilityProductionWizardWindow.Open());
            actions.Add(createMenu);

            _composeSetButton =
                MakeToolbarButton("Set 구성", OpenSetCompositionForSelection);
            _composeSetButton.tooltip =
                "현재 선택한 Ability들로 공용 Set을 만들거나 Base Set의 파생 Set을 구성합니다.";
            actions.Add(_composeSetButton);

            _duplicateButton = MakeToolbarButton("복제", DuplicateSelected);
            _duplicateButton.tooltip =
                "선택 에셋 전체를 새 파일과 새 안정 ID로 복제합니다. (Ctrl/Cmd+D)";
            actions.Add(_duplicateButton);

            _copyTabButton = MakeToolbarButton("탭 복사", CopyActiveTab);
            _copyTabButton.tooltip =
                "현재 탭에 표시된 값만 복사합니다. (Ctrl/Cmd+Shift+C)";
            actions.Add(_copyTabButton);

            _pasteTabButton = MakeToolbarButton("붙여넣기", PasteActiveTab);
            _pasteTabButton.tooltip =
                "같은 에셋 타입의 같은 탭에 복사한 값을 붙여넣습니다. (Ctrl/Cmd+Shift+V)";
            actions.Add(_pasteTabButton);

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1f;
            spacer.style.minWidth = 10f;
            actions.Add(spacer);

            _pingButton = MakeToolbarButton("찾기", PingSelectedAsset);
            _pingButton.tooltip =
                "선택한 에셋을 Project 창에서 선택하고 강조 표시합니다.";
            actions.Add(_pingButton);

            actions.Add(MakeToolbarButton("전체 검증", ValidateAll));

            var save = MakeToolbarButton("저장", SaveSelected);
            save.style.backgroundColor = Accent;
            save.style.color = Color.white;
            actions.Add(save);

            ToolbarMenu moreMenu = MakeToolbarMenu(
                "⋯",
                "낮은 빈도 또는 주의가 필요한 작업입니다.");
            moreMenu.menu.AppendAction(
                "선택 에셋 삭제…",
                _ => DeleteSelected(),
                _ => _selected != null
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);
            actions.Add(moreMenu);
            actionScroll.Add(actions);
            toolbar.Add(actionScroll);
            rootVisualElement.Add(toolbar);
            RefreshQuickActionStates();
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
                "트리거", "Effect", "저장/교체 정책", "정적 밸런스", "검증 결과",
            };
            for (int i = 0; i < labels.Length; i++)
            {
                string tab = labels[i];
                var button = new Button(() =>
                {
                    _activeTab = tab;
                    UpdateTabStyles();
                    RebuildDetail();
                    RefreshQuickActionStates();
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
            if (_selected is not GameplayAbilitySO
                && string.Equals(_activeTab, "트리거", StringComparison.Ordinal))
                _activeTab = "기본 정보";

            foreach (KeyValuePair<string, Button> pair in _tabButtons)
            {
                bool visible = !string.Equals(
                                   pair.Key,
                                   "트리거",
                                   StringComparison.Ordinal)
                               || _selected is GameplayAbilitySO;
                bool active = string.Equals(pair.Key, _activeTab, StringComparison.Ordinal);
                Button button = pair.Value;
                button.style.display = visible
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
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
            _filterMenu.style.width = 72f;
            _filterMenu.style.minWidth = 72f;
            _filterMenu.style.maxWidth = 72f;
            _filterMenu.style.flexShrink = 0f;
            _filterMenu.style.marginLeft = 4f;
            foreach (string filter in new[] { "전체", "Ability", "Passive", "Effect", "Set" })
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

            var selectionHint = new Label("Ctrl/Shift로 여러 개 선택 · 삭제는 상단 '⋯' 메뉴");
            selectionHint.tooltip = "다중 선택 후 상단 더보기 메뉴에서 선택 에셋 삭제를 실행합니다.";
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

        private void OnEditorShortcut(KeyDownEvent evt)
        {
            bool action = evt.ctrlKey || evt.commandKey;
            if (!action)
                return;

            if (evt.keyCode == KeyCode.S)
            {
                SaveSelected();
            }
            else if (evt.keyCode == KeyCode.D && CanDuplicateSelected())
            {
                DuplicateSelected();
            }
            else if (evt.shiftKey
                     && evt.keyCode == KeyCode.C
                     && CanCopyActiveTab())
            {
                CopyActiveTab();
            }
            else if (evt.shiftKey
                     && evt.keyCode == KeyCode.V
                     && CanPasteActiveTab())
            {
                PasteActiveTab();
            }
            else
            {
                return;
            }

            evt.StopPropagation();
            evt.PreventDefault();
        }

        private VisualElement MakeAssetRow()
        {
            var wrapper = new VisualElement();
            wrapper.name = "wrapper";

            // 타입 그룹의 첫 행에만 노출되는 헤더. 나머지 행에서는 display:none.
            var groupHeader = new Label { name = "groupHeader" };
            groupHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            groupHeader.style.fontSize = 10f;
            groupHeader.style.color = new Color(0.62f, 0.78f, 0.95f);
            groupHeader.style.backgroundColor = new Color(0.13f, 0.16f, 0.21f);
            groupHeader.style.paddingLeft = 8f;
            groupHeader.style.paddingRight = 8f;
            groupHeader.style.paddingTop = 4f;
            groupHeader.style.paddingBottom = 4f;
            groupHeader.style.borderTopColor = new Color(0.28f, 0.4f, 0.55f);
            groupHeader.style.borderTopWidth = 1f;
            wrapper.Add(groupHeader);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 7f;
            row.style.paddingRight = 5f;
            row.style.paddingTop = 4f;
            row.style.paddingBottom = 4f;
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
            // 에셋 타입 대신 메모를 노출한다. 메모가 없으면 이 줄 자체를 감춘다.
            var memo = new Label { name = "memo" };
            memo.style.fontSize = 10f;
            memo.style.color = new Color(0.55f, 0.6f, 0.68f);
            memo.style.whiteSpace = WhiteSpace.NoWrap;
            memo.style.overflow = Overflow.Hidden;
            memo.style.textOverflow = TextOverflow.Ellipsis;
            labels.Add(memo);
            row.Add(labels);

            var badge = new Label { name = "badge" };
            badge.style.unityTextAlign = TextAnchor.MiddleCenter;
            badge.style.minWidth = 20f;
            row.Add(badge);

            wrapper.Add(row);
            return wrapper;
        }

        private void BindAssetRow(VisualElement wrapper, int index)
        {
            if ((uint)index >= (uint)_filtered.Count) return;
            UnityEngine.Object asset = _filtered[index];

            // 이 행이 속한 타입 그룹의 첫 항목일 때만 그룹 헤더를 노출한다.
            var groupHeader = wrapper.Q<Label>("groupHeader");
            bool isGroupStart = index == 0
                || _filtered[index - 1].GetType() != asset.GetType();
            if (isGroupStart)
            {
                _filteredGroupCounts.TryGetValue(asset.GetType(), out int count);
                groupHeader.text = $"{AssetTypeGroupLabel(asset)}  ({count})";
                groupHeader.style.display = DisplayStyle.Flex;
            }
            else
            {
                groupHeader.style.display = DisplayStyle.None;
            }

            wrapper.Q<Label>("name").text = GetStableId(asset);

            Label memo = wrapper.Q<Label>("memo");
            string memoText = GetEditorMemo(asset);
            if (string.IsNullOrWhiteSpace(memoText))
            {
                memo.text = string.Empty;
                memo.tooltip = string.Empty;
                memo.style.display = DisplayStyle.None;
            }
            else
            {
                // 목록 행은 한 줄이므로 개행은 공백으로 접어 표시한다.
                memo.text = memoText.Replace("\r", " ").Replace("\n", " ").Trim();
                memo.tooltip = memoText;
                memo.style.display = DisplayStyle.Flex;
            }

            wrapper.Q<Image>("icon").image = GetIcon(asset);

            List<AbilityValidationIssue> issues = AbilityDataValidator.Validate(asset);
            int errors = issues.Count(x => x.Severity == AbilityValidationSeverity.Error);
            int warnings = issues.Count(x => x.Severity == AbilityValidationSeverity.Warning);
            Label badge = wrapper.Q<Label>("badge");
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
                RefreshQuickActionStates();
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
            RefreshQuickActionStates();
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
            UpdateTabStyles();
            _detail.Clear();
            _summary.Clear();
            if (_selected == null)
            {
                var empty = new HelpBox(
                    "왼쪽에서 AbilitySet 범위를 선택하고 편집할 에셋을 고르세요.\n"
                    + "AbilitySet은 캐릭터의 전체 전투 구성, Ability는 개별 행동, "
                    + "Passive는 상시·조건부 능력, Effect는 버프·상태 변화를 정의합니다.",
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

            _variantPayloadSection = null;
            _variantPayloadSignature = null;
            if (_selected is GameplayAbilitySO variantAbility && _activeTab == "Variant")
            {
                _variantPayloadSection = new VisualElement();
                _variantPayloadSection.style.marginTop = 14f;
                bindingRoot.Add(_variantPayloadSection);
                RebuildVariantPayloadSection(variantAbility);
            }

            bindingRoot.TrackSerializedObjectValue(serialized, _ =>
            {
                EditorUtility.SetDirty(_selected);
                RebuildSummary();
                RebuildValidation();
                RefreshVariantPayloadSection();
                _assetList?.RefreshItems();
            });
            RebuildSummary();
            RebuildValidation();
        }

        /// <summary>
        /// Variant 목록의 Payload 구성이 바뀐 경우에만 Payload 편집 영역을 다시 만든다.
        /// 값 변경마다 재생성하면 편집 중인 필드의 포커스가 끊긴다.
        /// </summary>
        private void RefreshVariantPayloadSection()
        {
            if (_variantPayloadSection == null
                || _selected is not GameplayAbilitySO ability)
                return;
            if (string.Equals(
                    BuildVariantPayloadSignature(ability),
                    _variantPayloadSignature,
                    StringComparison.Ordinal))
                return;
            RebuildVariantPayloadSection(ability);
        }

        private static string BuildVariantPayloadSignature(GameplayAbilitySO ability)
        {
            if (ability?.variants == null) return string.Empty;

            var builder = new System.Text.StringBuilder();
            for (int i = 0; i < ability.variants.Count; i++)
            {
                AbilityVariantDefinition variant = ability.variants[i];
                builder.Append(variant?.variantId).Append('|');
                builder.Append(
                    variant?.executionPayload != null
                        ? variant.executionPayload.GetInstanceID()
                        : 0);
                builder.Append(';');
            }
            return builder.ToString();
        }

        /// <summary>
        /// Variant가 참조하는 Execution Payload 에셋을 이 창에서 직접 편집할 수 있게 그린다.
        /// 공격 Motion과 HitPhase의 권위 소스가 Payload이므로 Ability와 같은 화면에서 편집한다.
        /// </summary>
        private void RebuildVariantPayloadSection(GameplayAbilitySO ability)
        {
            if (_variantPayloadSection == null) return;

            _variantPayloadSection.Clear();
            _abilityMotionIndex = new AbilityMotionIndex();
            _variantPayloadSignature = BuildVariantPayloadSignature(ability);
            _variantPayloadSection.Add(SectionHeader(
                "Execution Payload 편집",
                "각 Variant가 참조하는 Payload 에셋을 여기서 직접 편집합니다. "
                + "공격 Motion 해석 키의 단일 소스는 attackInfo.motionKey입니다."));

            List<AbilityVariantDefinition> variants = ability?.variants;
            if (variants == null || variants.Count == 0)
            {
                _variantPayloadSection.Add(new HelpBox(
                    "실행 Variant가 없습니다. 위 목록에 Variant를 추가하세요.",
                    HelpBoxMessageType.Info));
                return;
            }

            for (int i = 0; i < variants.Count; i++)
            {
                AbilityVariantDefinition variant = variants[i];
                if (variant == null) continue;

                string variantId = string.IsNullOrWhiteSpace(variant.variantId)
                    ? $"Variant {i}"
                    : variant.variantId;
                _variantPayloadSection.Add(
                    BuildPayloadFoldout(
                        ability,
                        variant,
                        i,
                        variantId,
                        variant.executionPayload));
            }
        }

        private const string AttackInfoPropertyName = "attackInfo";

        /// <summary>
        /// AbilityAttackInfo는 실행·플레이어 조작·AI 선택·연출·방어가 한 구조체에 모여 있어
        /// 한 번에 펼치면 필드 벽이 된다. 소비자 기준으로 묶어 기본 접힘 상태로 보여준다.
        /// Fields는 AbilityAttackInfo의 직렬화 필드명과 정확히 일치해야 한다.
        /// </summary>
        private static readonly (string Title, string[] Fields, bool DefaultOpen)[]
            AttackInfoGroups =
            {
                // 모션과 공격 수치는 서로 독립적으로 편집한다. 한 그룹에 묶으면
                // 모션 교체와 밸런스 조정이 같은 화면에서 섞여 보인다.
                ("실행 · 모션", new[] { "motionKey" }, true),
                ("공격 · 히트 페이즈", new[] { "baseInfo" }, true),
                ("플레이어 조작 · 캔슬",
                    new[] { "interruptActions", "moveCancelDelayAfterLastHit" }, true),
                ("방어 대응", new[] { "defenseType" }, true),
                ("AI 선택 (BT 전용)",
                    new[]
                    {
                        "aiSelectable", "skillType", "attackCategory", "aiRoles",
                        "requiredLevel", "selectionWeight", "conditionGroup",
                    },
                    false),
                ("공중 · 급강하",
                    new[]
                    {
                        "isAerialSkill", "isDiveAttack",
                        "diveDescentSpeed", "aerialSkillWeight",
                    },
                    false),
                ("연출 · 텔레그래프와 Danger Ring",
                    new[]
                    {
                        "useTelegraph", "telegraphShape", "telegraphRadiusScale",
                        "telegraphFXKey", "useMotionEventTelegraph",
                        "telegraphAnchorType", "useTelegraphPositionForHit",
                        "useDangerRing", "dangerRingDuration", "dangerRingPrefabKey",
                    },
                    false),
            };

        /// <summary>
        /// attackInfo의 하위 필드를 관심사 그룹 Foldout으로 그린다.
        /// 그룹 표에 없는 필드는 "기타"로 모아 어떤 필드도 화면에서 사라지지 않게 한다.
        /// </summary>
        private bool BuildAttackInfoGroups(
            VisualElement parent,
            SerializedObject payloadSerialized,
            SerializedProperty attackInfo,
            string stateKey,
            GameplayAbilitySO ability,
            AbilityVariantDefinition variant)
        {
            var remaining = new List<string>();
            SerializedProperty child = attackInfo.Copy();
            SerializedProperty end = attackInfo.GetEndProperty();
            bool enterChildren = true;
            while (child.NextVisible(enterChildren)
                   && !SerializedProperty.EqualContents(child, end))
            {
                enterChildren = false;
                remaining.Add(child.name);
            }
            if (remaining.Count == 0) return false;

            bool aiSelectable =
                attackInfo.FindPropertyRelative("aiSelectable")?.boolValue ?? false;

            for (int i = 0; i < AttackInfoGroups.Length; i++)
            {
                (string title, string[] fields, bool defaultOpen) = AttackInfoGroups[i];
                var present = new List<SerializedProperty>();
                for (int f = 0; f < fields.Length; f++)
                {
                    SerializedProperty property =
                        attackInfo.FindPropertyRelative(fields[f]);
                    if (property == null) continue;
                    present.Add(property.Copy());
                    remaining.Remove(fields[f]);
                }
                if (present.Count == 0) continue;

                bool isAiGroup = title.StartsWith("AI 선택", StringComparison.Ordinal);
                VisualElement group = BuildAttackInfoGroup(
                    payloadSerialized,
                    $"{stateKey}#{title}",
                    title,
                    present,
                    defaultOpen,
                    ability,
                    variant,
                    isAiGroup && !aiSelectable
                        ? "Ai Selectable이 꺼져 있어 이 그룹은 AI 선택에 사용되지 않습니다. "
                          + "(플레이어 Ability에서는 항상 비활성)"
                        : null);
                parent.Add(group);
            }

            if (remaining.Count > 0)
            {
                var leftovers = new List<SerializedProperty>();
                for (int i = 0; i < remaining.Count; i++)
                {
                    SerializedProperty property =
                        attackInfo.FindPropertyRelative(remaining[i]);
                    if (property != null) leftovers.Add(property.Copy());
                }
                if (leftovers.Count > 0)
                {
                    parent.Add(BuildAttackInfoGroup(
                        payloadSerialized,
                        $"{stateKey}#기타",
                        "기타 (그룹 미지정)",
                        leftovers,
                        true,
                        ability,
                        variant,
                        "AttackInfoGroups 표에 없는 필드입니다. 필드를 추가했다면 표에도 반영하세요."));
                }
            }

            return true;
        }

        private VisualElement BuildAttackInfoGroup(
            SerializedObject payloadSerialized,
            string stateKey,
            string title,
            List<SerializedProperty> properties,
            bool defaultOpen,
            GameplayAbilitySO ability,
            AbilityVariantDefinition variant,
            string hint)
        {
            var group = new Foldout
            {
                text = title,
                value = !_payloadFoldoutStates.TryGetValue(stateKey, out bool expanded)
                    ? defaultOpen
                    : expanded,
            };
            group.style.marginTop = 4f;
            group.style.marginLeft = 6f;
            group.RegisterValueChangedCallback(evt =>
            {
                if (evt.target == group)
                    _payloadFoldoutStates[stateKey] = evt.newValue;
            });

            if (!string.IsNullOrEmpty(hint))
                group.Add(new HelpBox(hint, HelpBoxMessageType.Info));

            for (int i = 0; i < properties.Count; i++)
            {
                if (properties[i].name == "motionKey"
                    && ability != null
                    && variant != null)
                {
                    group.Add(BuildMotionBindingEditor(
                        payloadSerialized,
                        properties[i],
                        ability,
                        variant));
                    continue;
                }

                if (properties[i].name == "baseInfo")
                {
                    group.Add(BuildBaseInfoEditor(
                        payloadSerialized,
                        properties[i]));
                    continue;
                }

                var field = new PropertyField(properties[i]);
                field.style.marginTop = 2f;
                field.Bind(payloadSerialized);
                group.Add(field);
            }
            return group;
        }

        /// <summary>
        /// baseInfo의 하위 필드(attackType, hitPhases)를 평탄하게 그린다.
        /// Motion Key가 형제 필드로 분리된 뒤로는 baseInfo 자체를 감쌀 단계가
        /// 필요 없어, 상위 "공격 · 히트 페이즈" 그룹 아래에 바로 편다.
        /// </summary>
        private VisualElement BuildBaseInfoEditor(
            SerializedObject payloadSerialized,
            SerializedProperty baseInfo)
        {
            var container = new VisualElement();
            container.style.marginTop = 2f;

            SerializedProperty child = baseInfo.Copy();
            SerializedProperty end = baseInfo.GetEndProperty();
            bool enterChildren = true;
            while (child.NextVisible(enterChildren)
                   && !SerializedProperty.EqualContents(child, end))
            {
                enterChildren = false;

                var field = new PropertyField(child.Copy());
                field.style.marginTop = 2f;
                field.Bind(payloadSerialized);
                container.Add(field);
            }
            return container;
        }

        private VisualElement BuildMotionBindingEditor(
            SerializedObject payloadSerialized,
            SerializedProperty motionKeyProperty,
            GameplayAbilitySO ability,
            AbilityVariantDefinition variant)
        {
            var root = new VisualElement();
            root.style.marginTop = 3f;
            root.style.marginBottom = 5f;
            root.style.paddingTop = 5f;
            root.style.paddingBottom = 5f;
            root.style.paddingLeft = 6f;
            root.style.paddingRight = 6f;
            root.style.borderTopWidth = 1f;
            root.style.borderBottomWidth = 1f;
            root.style.borderLeftWidth = 1f;
            root.style.borderRightWidth = 1f;
            root.style.borderTopColor = Border;
            root.style.borderBottomColor = Border;
            root.style.borderLeftColor = Border;
            root.style.borderRightColor = Border;

            MotionKey storedKey = ReadMotionKey(motionKeyProperty);
            List<ActorAnimationMotionSet> owners =
                FindMotionOwners(ability, storedKey);
            if (owners.Count == 0)
            {
                // 드롭다운은 이미 매핑된 Key만 고를 수 있다. 대상 MotionSet을 못 찾은
                // Ability까지 드롭다운으로 막으면 아직 모션이 정해지지 않은 Ability의 Key를
                // 이 창에서 아예 저작할 수 없게 되므로, 이 경우에는 직접 입력을 연다.
                root.Add(BuildRawMotionKeyField(
                    payloadSerialized,
                    motionKeyProperty,
                    storedKey));
                root.Add(new HelpBox(
                    "이 Ability가 연결된 ActorAnimationMotionSet을 찾지 못해 Key를 직접 입력합니다. "
                    + "AbilitySet 범위를 선택하거나 Actor MotionSet의 Attack Ability Set 연결을 확인하세요.",
                    HelpBoxMessageType.Warning));
                return root;
            }

            List<string> ownerLabels = owners
                .Select(GetMotionOwnerLabel)
                .ToList();
            int ownerIndex = 0;
            for (int i = 0; i < owners.Count; i++)
            {
                if (owners[i].attackAbilitySet == _activeSet)
                {
                    ownerIndex = i;
                    break;
                }
            }

            // 대상을 고른 뒤 Key를 고르는 순서이므로 배치도 같은 순서로 둔다.
            if (owners.Count > 1)
            {
                root.Add(new HelpBox(
                    $"{owners.Count}개의 액터/무기 모션 컨텍스트가 연결되어 있습니다. "
                    + "대상을 선택하면 해당 컨텍스트에서 사용할 수 있는 Motion Key만 표시합니다.",
                    HelpBoxMessageType.Info));
            }

            var ownerDropdown = new DropdownField(
                "대상 액터 · 무기",
                ownerLabels,
                ownerIndex);
            ownerDropdown.tooltip =
                "현재 Ability를 직접 연결하거나 현재 Motion Key를 소유한 액터 모션 세트입니다.";
            root.Add(ownerDropdown);

            var keyRoot = new VisualElement();
            root.Add(keyRoot);
            RebuildMotionKeyPanel(
                keyRoot,
                payloadSerialized,
                motionKeyProperty,
                owners[ownerIndex],
                storedKey);

            ownerDropdown.RegisterValueChangedCallback(_ =>
            {
                int selectedIndex = ownerDropdown.index;
                if (selectedIndex < 0 || selectedIndex >= owners.Count)
                    return;
                RebuildMotionKeyPanel(
                    keyRoot,
                    payloadSerialized,
                    motionKeyProperty,
                    owners[selectedIndex],
                    ReadMotionKey(motionKeyProperty));
            });
            return root;
        }

        private List<ActorAnimationMotionSet> FindMotionOwners(
            GameplayAbilitySO ability,
            MotionKey key)
        {
            _abilityMotionIndex ??= new AbilityMotionIndex();
            var attachedOwners = new HashSet<ActorAnimationMotionSet>();

            IReadOnlyList<ActorAnimationMotionSet> allOwners =
                _abilityMotionIndex.Owners;
            for (int i = 0; i < allOwners.Count; i++)
            {
                ActorAnimationMotionSet owner = allOwners[i];
                if (owner?.attackAbilitySet == null)
                    continue;
                if (owner.attackAbilitySet == _activeSet
                    || owner.attackAbilitySet
                        .EnumerateAll()
                        .Any(candidate => candidate == ability))
                    attachedOwners.Add(owner);
            }

            HashSet<ActorAnimationMotionSet> result = attachedOwners;
            if (result.Count == 0)
            {
                result = new HashSet<ActorAnimationMotionSet>();
                List<ActorAnimationMotionSet> directOwners =
                    _abilityMotionIndex.FindDirectOwners(key);
                for (int i = 0; i < directOwners.Count; i++)
                    result.Add(directOwners[i]);
            }

            return result
                .OrderByDescending(owner => owner.attackAbilitySet == _activeSet)
                .ThenBy(owner => owner.attackWeaponType)
                .ThenBy(owner => owner.name, StringComparer.Ordinal)
                .ToList();
        }

        private static string GetMotionOwnerLabel(
            ActorAnimationMotionSet owner) =>
            $"{owner.attackWeaponType} · {owner.name}";

        private void RebuildMotionKeyPanel(
            VisualElement parent,
            SerializedObject payloadSerialized,
            SerializedProperty motionKeyProperty,
            ActorAnimationMotionSet owner,
            MotionKey key)
        {
            parent.Clear();
            if (owner == null)
                return;

            List<MotionMappingOption> options =
                BuildMotionKeyOptions(owner);
            if (options.Count == 0)
            {
                parent.Add(BuildRawMotionKeyField(
                    payloadSerialized,
                    motionKeyProperty,
                    key));
                parent.Add(new HelpBox(
                    "선택한 대상의 모션 세트에 매핑된 Motion Key가 없어 직접 입력합니다. "
                    + "Actor MotionSet의 Ability Motions에 Key를 먼저 추가하면 목록에서 고를 수 있습니다.",
                    HelpBoxMessageType.Warning));
                return;
            }

            bool currentKeyAvailable =
                options.Any(option => option.SourceKey == key);
            // 저장된 값을 반드시 항목으로 만들어 둔다. 이게 없으면 드롭다운이 첫 후보를
            // 선택된 것처럼 보여줘 실제 직렬화 값과 표시가 어긋난다.
            if (!currentKeyAvailable)
            {
                options.Insert(0, new MotionMappingOption
                {
                    Label = key.IsValid
                        ? $"{key} (현재 값 · 대상에서 해석 불가)"
                        : "(미지정)",
                    SourceKey = key,
                });
            }

            List<string> labels = options.Select(option => option.Label).ToList();
            int selectedIndex = options.FindIndex(
                option => option.SourceKey == key);
            if (selectedIndex < 0)
                selectedIndex = 0;

            var keyDropdown = new DropdownField(
                "Motion Key",
                labels,
                selectedIndex);
            keyDropdown.tooltip =
                $"Motion: {options[selectedIndex].Motion?.name ?? "미지정"}\n"
                + $"Key: {options[selectedIndex].SourceKey}\n"
                + "선택한 액터/무기 모션 세트에서 해석 가능한 항목입니다.";
            parent.Add(keyDropdown);

            MotionKey selectedKey = options[selectedIndex].SourceKey;
            MotionSetAsset resolved = owner.GetAbilityMotionAsset(selectedKey);

            var statusRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginTop = 3f,
                },
            };
            string inheritedSuffix =
                options[selectedIndex].Inherited ? " · fallback" : string.Empty;
            var status = new Label(resolved != null
                ? $"해석 결과: {resolved.name}{inheritedSuffix}"
                : "해석 결과: 미지정");
            status.style.flexGrow = 1f;
            status.tooltip = resolved != null
                ? AssetDatabase.GetAssetPath(resolved)
                : string.Empty;
            statusRow.Add(status);
            // 표시된 해석 결과와 같은 대상을 가리켜야 한다. 저장된 Key를 다시 읽으면
            // 버튼은 활성인데 클릭은 아무 일도 하지 않는 상태가 생긴다.
            var ping = new Button(() =>
            {
                if (resolved == null) return;
                Selection.activeObject = resolved;
                EditorGUIUtility.PingObject(resolved);
            })
            {
                text = "Project에서 보기",
            };
            ping.SetEnabled(resolved != null);
            statusRow.Add(ping);
            parent.Add(statusRow);

            if (!currentKeyAvailable)
            {
                parent.Add(new HelpBox(
                    key.IsValid
                        ? "현재 저장된 Key는 선택한 대상에서 해석되지 않습니다. "
                          + "드롭다운에서 사용 가능한 Key를 선택하세요."
                        : "Motion Key가 지정되지 않았습니다. "
                          + "드롭다운에서 사용할 Key를 선택하세요.",
                    HelpBoxMessageType.Warning));
            }

            keyDropdown.RegisterValueChangedCallback(_ =>
            {
                int optionIndex = keyDropdown.index;
                if (optionIndex < 0 || optionIndex >= options.Count)
                    return;
                MotionKey selected = options[optionIndex].SourceKey;
                if (!selected.IsValid || selected == key)
                    return;
                WriteMotionKey(
                    payloadSerialized,
                    motionKeyProperty,
                    selected);
                RebuildMotionKeyPanel(
                    parent,
                    payloadSerialized,
                    motionKeyProperty,
                    owner,
                    selected);
            });
        }

        private static List<MotionMappingOption> BuildMotionKeyOptions(
            ActorAnimationMotionSet owner)
        {
            var options = new List<MotionMappingOption>();
            var seen = new HashSet<MotionKey>();
            var visited = new HashSet<ActorAnimationMotionSet>();
            bool inherited = false;
            for (ActorAnimationMotionSet current = owner;
                 current != null && visited.Add(current);
                 current = current.fallbackMotionSet)
            {
                if (current.abilityMotions != null)
                {
                    IEnumerable<KeyValuePair<
                            MotionKey,
                            MotionSetAsset>> mappings =
                        current.abilityMotions
                            .Where(pair =>
                                pair.Key.IsValid
                                && pair.Value != null
                                && seen.Add(pair.Key))
                            .OrderBy(
                                pair => pair.Key.ToString(),
                                StringComparer.Ordinal);
                    foreach (KeyValuePair<
                                 MotionKey,
                                 MotionSetAsset> pair in mappings)
                    {
                        options.Add(new MotionMappingOption
                        {
                            Label = pair.Key.ToString(),
                            SourceKey = pair.Key,
                            Motion = pair.Value,
                            Inherited = inherited,
                        });
                    }
                }
                inherited = true;
            }
            return options;
        }

        /// <summary>
        /// 드롭다운은 이미 매핑된 Key만 고를 수 있으므로, 아직 매핑이 없는 Ability를 위해
        /// 원시 문자열 입력 경로를 남긴다. 이게 없으면 신규 Key를 이 창에서 만들 수 없다.
        /// </summary>
        private VisualElement BuildRawMotionKeyField(
            SerializedObject payloadSerialized,
            SerializedProperty motionKeyProperty,
            MotionKey key)
        {
            var field = new TextField("Motion Key")
            {
                value = key.IsValid ? key.value : string.Empty,
                isDelayed = true,
                tooltip =
                    "Actor MotionSet의 Ability Motions에 등록할 Key를 직접 입력합니다.",
            };
            field.RegisterValueChangedCallback(evt =>
            {
                var edited = new MotionKey(evt.newValue);
                if (edited == key)
                    return;
                WriteMotionKey(payloadSerialized, motionKeyProperty, edited);
            });
            return field;
        }

        private void WriteMotionKey(
            SerializedObject payloadSerialized,
            SerializedProperty motionKeyProperty,
            MotionKey key)
        {
            if (payloadSerialized?.targetObject == null
                || motionKeyProperty == null
                || !key.IsValid)
                return;

            string propertyPath = motionKeyProperty.propertyPath;
            Undo.RecordObjects(
                payloadSerialized.targetObjects,
                "Ability Motion Key 변경");
            payloadSerialized.Update();
            SerializedProperty current =
                payloadSerialized.FindProperty(propertyPath);
            SerializedProperty value =
                current?.FindPropertyRelative("value");
            if (value == null)
                return;

            value.stringValue = key.value;
            payloadSerialized.ApplyModifiedPropertiesWithoutUndo();
            foreach (UnityEngine.Object target
                     in payloadSerialized.targetObjects)
            {
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssetIfDirty(target);
            }
            RebuildValidation();
        }

        private static MotionKey ReadMotionKey(
            SerializedProperty motionKeyProperty)
        {
            if (motionKeyProperty == null)
                return default;
            string value = motionKeyProperty
                .FindPropertyRelative("value")
                ?.stringValue;
            return new MotionKey(value);
        }

        private VisualElement BuildPayloadFoldout(
            GameplayAbilitySO ability,
            AbilityVariantDefinition variant,
            int index,
            string variantId,
            AbilityExecutionPayloadSO payload)
        {
            string stateKey = $"{index}:{variantId}";
            var foldout = new Foldout
            {
                text = payload != null
                    ? $"{variantId} · {payload.name}"
                    : $"{variantId} · Payload 미지정",
                value = !_payloadFoldoutStates.TryGetValue(stateKey, out bool expanded)
                    || expanded,
            };
            foldout.style.marginTop = 6f;
            foldout.RegisterValueChangedCallback(evt =>
            {
                if (evt.target == foldout)
                    _payloadFoldoutStates[stateKey] = evt.newValue;
            });

            if (payload == null)
            {
                foldout.Add(new HelpBox(
                    "이 Variant에는 Execution Payload가 지정되지 않아 실행할 수 없습니다. "
                    + "위 Variant 목록에서 Payload를 지정하세요.",
                    HelpBoxMessageType.Warning));
                return foldout;
            }

            var header = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, marginBottom = 4f },
            };
            var pathLabel = new Label(AssetDatabase.GetAssetPath(payload))
            {
                tooltip = payload.GetType().Name,
            };
            pathLabel.style.flexGrow = 1f;
            pathLabel.style.color = new Color(0.55f, 0.6f, 0.68f);
            pathLabel.style.overflow = Overflow.Hidden;
            header.Add(pathLabel);
            var pingButton = new Button(() =>
            {
                Selection.activeObject = payload;
                EditorGUIUtility.PingObject(payload);
            })
            {
                text = "Project에서 보기",
            };
            pingButton.style.flexShrink = 0f;
            header.Add(pingButton);
            foldout.Add(header);

            var payloadSerialized = new SerializedObject(payload);
            SerializedProperty attackInfo =
                payloadSerialized.FindProperty(AttackInfoPropertyName);
            bool grouped = false;
            if (attackInfo != null)
                grouped = BuildAttackInfoGroups(
                    foldout,
                    payloadSerialized,
                    attackInfo,
                    stateKey,
                    ability,
                    variant);

            SerializedProperty iterator = payloadSerialized.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyPath == "m_Script") continue;
                // attackInfo는 위에서 관심사별 그룹으로 이미 그렸다.
                if (grouped && iterator.propertyPath == AttackInfoPropertyName) continue;

                var field = new PropertyField(iterator.Copy());
                field.style.marginTop = 4f;
                field.Bind(payloadSerialized);
                foldout.Add(field);
            }

            // TrackSerializedObjectValue는 요소당 SerializedObject 하나만 추적하므로
            // Payload마다 별도의 Foldout 요소에 등록한다.
            foldout.TrackSerializedObjectValue(payloadSerialized, _ =>
            {
                if (payload == null) return;
                EditorUtility.SetDirty(payload);
                RebuildValidation();
            });
            return foldout;
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
            else if (_selected is PassiveAbilitySO passive)
            {
                AddSummary("분류", "Passive");
                AddSummary("발동", passive.activationType.ToString());
                AddSummary("범위", passive.scope.ToString());
                AddSummary("Modifier", (passive.modifiers?.Count ?? 0).ToString());
                AddSummary("발동 Effect", (passive.triggeredEffects?.Count ?? 0).ToString());
            }
            else if (_selected is GameplayEffectSO effect)
            {
                AddSummary("표시 이름", effect.presentation?.displayName);
                AddSummary("극성", effect.polarity.ToString());
                AddSummary(
                    "HUD",
                    effect.presentation?.showInHud == true ? "표시" : "숨김");
                AddSummary(
                    "아이콘",
                    effect.presentation?.icon != null
                        ? effect.presentation.icon.name
                        : "미지정");
                AddSummary("지속 타입", effect.durationType.ToString());
                AddSummary("지속 시간", $"{effect.durationSeconds:0.##}s");
                AddSummary("주기", effect.IsPeriodic ? $"{effect.periodSeconds:0.##}s" : "없음");
                AddSummary("최대 스택", effect.maxStackCount.ToString());
            }
            else if (_selected is AbilitySetSO set)
            {
                AddSummary("Base Set", set.baseSet != null ? set.baseSet.name : "독립 Set");
                AddSummary("Override", (set.abilityOverrides?.Count ?? 0).ToString());
                AddSummary("스킬 슬롯", (set.playerSlots?.Count ?? 0).ToString());
                AddSummary("전투 슬롯", (set.combatBindings?.Count ?? 0).ToString());
                AddSummary(
                    "차지 단계",
                    (set.GetEffectiveCharge()?.stages?.Count ?? 0).ToString());
                AddSummary(
                    "연계 라우트",
                    set.GetEffectiveComboRoutes().Count.ToString());
                AddSummary("유효 Ability", set.EnumerateAll().Count().ToString());
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
            LoadAssets<PassiveAbilitySO>();
            LoadAssets<GameplayEffectSO>();
            LoadAssets<AbilitySetSO>();
            _assets.Sort((a, b) =>
            {
                int rankCompare = AssetTypeRank(a).CompareTo(AssetTypeRank(b));
                return rankCompare != 0
                    ? rankCompare
                    : string.Compare(GetStableId(a), GetStableId(b), StringComparison.Ordinal);
            });
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
                bool showAllPassives = _filter == "Passive"
                                       && asset is PassiveAbilitySO;
                if (scopedAssets != null
                    && !showAllPassives
                    && !scopedAssets.Contains(asset))
                    continue;
                if (!MatchesType(asset)) continue;
                if (!string.IsNullOrEmpty(query)
                    && GetSearchText(asset).IndexOf(
                        query, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                _filtered.Add(asset);
            }
            _filteredGroupCounts.Clear();
            for (int i = 0; i < _filtered.Count; i++)
            {
                System.Type type = _filtered[i].GetType();
                _filteredGroupCounts.TryGetValue(type, out int count);
                _filteredGroupCounts[type] = count + 1;
            }
            // 같은 itemsSource를 제자리에서 교체한 뒤 RefreshItems만 호출하면
            // DynamicHeight 가상화가 이전 항목의 높이/누적 위치 캐시를 유지할 수 있다.
            // Set 전환은 항목 수와 그룹 헤더 높이가 함께 바뀌므로 구조 전체를 재구축한다.
            _assetList?.Rebuild();
            RestoreListSelection();
            ResetAssetListScroll();
        }

        private void ResetAssetListScroll()
        {
            ScrollView scrollView = _assetList?.Q<ScrollView>();
            if (scrollView == null) return;

            scrollView.scrollOffset = Vector2.zero;
            // Rebuild의 가상화 레이아웃은 다음 패널 갱신에서 확정된다. 레이아웃 전의
            // scrollOffset 지정이 이전 범위로 다시 보정되지 않도록 갱신 후 한 번 더 맞춘다.
            _assetList.schedule.Execute(() =>
            {
                ScrollView rebuiltScrollView = _assetList?.Q<ScrollView>();
                if (rebuiltScrollView != null)
                    rebuiltScrollView.scrollOffset = Vector2.zero;
            }).ExecuteLater(0);
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
            AbilitySetScope scope = _setScopes.FirstOrDefault(x => x.Set == _activeSet);
            if (scope != null && scope.CharacterTypes.Count > 0)
            {
                foreach (CharacterPassiveDatabaseSO database
                         in LoadAssetsIncludingSubAssets<CharacterPassiveDatabaseSO>())
                {
                    foreach (CharacterActorType characterType in scope.CharacterTypes)
                    {
                        CharacterPassiveSetSO passiveSet = database.Get(characterType);
                        if (passiveSet?.passives == null) continue;
                        for (int i = 0; i < passiveSet.passives.Count; i++)
                            if (passiveSet.passives[i] != null)
                                result.Add(passiveSet.passives[i]);
                    }
                }
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
            // 프로젝트 전체 프리팹을 로드하면 창을 열 때 수천 개의 에셋과
            // MonoBehaviour를 동기 검사하게 된다. AbilitySet을 소유하는
            // 플레이어/몬스터 프리팹 경계만 검색한다.
            string[] prefabGuids = AssetDatabase.FindAssets(
                "t:Prefab",
                AbilitySetOwnerPrefabSearchFolders);
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
                var addedScope = new AbilitySetScope
                {
                    Set = set,
                    SetName = set.name,
                    OwnerText = ownerText,
                    Group = group,
                    AssetPath = assetPath,
                    SearchText = $"{set.name} {ownerText} {assetPath}",
                    HasInputConnection = hasInputConnection,
                    HasBtConnection = hasBtConnection,
                };
                if (owners != null)
                {
                    foreach (string owner in owners)
                        if (Enum.TryParse(owner, out CharacterActorType characterType)
                            && characterType != CharacterActorType.None)
                            addedScope.CharacterTypes.Add(characterType);
                }
                _setScopes.Add(addedScope);
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
            "Passive" => asset is PassiveAbilitySO,
            "Effect" => asset is GameplayEffectSO,
            "Set" => asset is AbilitySetSO,
            _ => true,
        };

        // 목록을 에셋 타입별로 묶기 위한 정렬 순서. RefreshAssets 정렬과
        // BindAssetRow의 그룹 헤더 노출 판정이 이 순서를 공유한다.
        private static int AssetTypeRank(UnityEngine.Object asset) => asset switch
        {
            GameplayAbilitySO => 0,
            PassiveAbilitySO => 1,
            GameplayEffectSO => 2,
            AbilitySetSO => 3,
            _ => 4,
        };

        private static string AssetTypeGroupLabel(UnityEngine.Object asset) => asset switch
        {
            GameplayAbilitySO => "Ability · GameplayAbilitySO",
            PassiveAbilitySO => "Passive · PassiveAbilitySO",
            GameplayEffectSO => "Effect · GameplayEffectSO",
            AbilitySetSO => "Set · AbilitySetSO",
            _ => asset != null ? asset.GetType().Name : "기타",
        };

        private void LoadAssets<T>() where T : UnityEngine.Object
        {
            foreach (T asset in LoadAssetsIncludingSubAssets<T>())
                if (!_assets.Contains(asset))
                    _assets.Add(asset);
        }

        private static IReadOnlyList<T> LoadAssetsIncludingSubAssets<T>()
            where T : UnityEngine.Object
        {
            var result = new List<T>();
            var seen = new HashSet<int>();
            var paths = new HashSet<string>(StringComparer.Ordinal);
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            for (int i = 0; i < guids.Length; i++)
                paths.Add(AssetDatabase.GUIDToAssetPath(guids[i]));

            if (typeof(T) == typeof(PassiveAbilitySO)
                || typeof(T) == typeof(CharacterPassiveSetSO)
                || typeof(T) == typeof(GameplayEffectSO))
            {
                string[] databaseGuids =
                    AssetDatabase.FindAssets($"t:{nameof(CharacterPassiveDatabaseSO)}");
                for (int i = 0; i < databaseGuids.Length; i++)
                    paths.Add(AssetDatabase.GUIDToAssetPath(databaseGuids[i]));
            }

            foreach (string path in paths)
            {
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                for (int j = 0; j < assets.Length; j++)
                {
                    if (assets[j] is T asset && seen.Add(asset.GetInstanceID()))
                        result.Add(asset);
                }
            }
            return result;
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

        private void OpenSetCompositionForSelection()
        {
            List<UnityEngine.Object> selected = _assetList?.selectedItems
                .OfType<UnityEngine.Object>()
                .Distinct()
                .ToList()
                ?? new List<UnityEngine.Object>();
            if (selected.Count == 0 && _selected != null)
                selected.Add(_selected);
            GameplayAbilityProductionWizardWindow.OpenForSelection(selected);
        }

        private void DuplicateSelected()
        {
            if (_selected is not ScriptableObject source)
            {
                ShowNotification(new GUIContent("복제할 에셋을 하나 선택하세요."));
                return;
            }

            string sourcePath = AssetDatabase.GetAssetPath(source);
            if (string.IsNullOrWhiteSpace(sourcePath)
                || !AssetDatabase.IsMainAsset(source))
            {
                EditorUtility.DisplayDialog(
                    "복제할 수 없음",
                    "Project의 메인 에셋만 복제할 수 있습니다. 데이터베이스 내부 "
                    + "서브에셋은 해당 데이터베이스에서 관리하세요.",
                    "확인");
                return;
            }

            string directory = System.IO.Path.GetDirectoryName(sourcePath)
                ?.Replace('\\', '/') ?? "Assets/10.Datas/Ability";
            string defaultName =
                $"{System.IO.Path.GetFileNameWithoutExtension(sourcePath)}_Copy";
            string destination = EditorUtility.SaveFilePanelInProject(
                $"{source.GetType().Name} 복제",
                defaultName,
                "asset",
                "복제본을 저장할 위치를 선택하세요.",
                directory);
            if (string.IsNullOrWhiteSpace(destination))
                return;
            destination = AssetDatabase.GenerateUniqueAssetPath(destination);

            if (!AssetDatabase.CopyAsset(sourcePath, destination))
            {
                EditorUtility.DisplayDialog(
                    "복제 실패",
                    $"에셋을 복제하지 못했습니다.\n{destination}",
                    "확인");
                return;
            }

            AssetDatabase.ImportAsset(destination);
            ScriptableObject clone =
                AssetDatabase.LoadAssetAtPath<ScriptableObject>(destination);
            if (clone == null)
            {
                EditorUtility.DisplayDialog(
                    "복제 실패",
                    "복제본을 다시 불러오지 못했습니다.",
                    "확인");
                return;
            }

            SerializedObject serialized = new(clone);
            string idPropertyName = clone switch
            {
                GameplayAbilitySO => "abilityId",
                PassiveAbilitySO => "passiveId",
                GameplayEffectSO => "effectId",
                _ => null,
            };
            if (idPropertyName != null)
            {
                SerializedProperty id = serialized.FindProperty(idPropertyName);
                if (id != null)
                    id.stringValue = CreateUniqueStableId(GetStableId(source));
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(clone);
            AssetDatabase.SaveAssets();

            RefreshAssets();
            _selected = clone;
            Selection.activeObject = clone;
            int cloneIndex = _filtered.IndexOf(clone);
            if (cloneIndex >= 0)
                _assetList?.SetSelection(new[] { cloneIndex });
            UpdateSelectedHeader();
            RebuildDetail();
            EditorGUIUtility.PingObject(clone);
            ShowNotification(new GUIContent($"'{clone.name}' 복제 완료"));
        }

        private string CreateUniqueStableId(string sourceId)
        {
            string root = string.IsNullOrWhiteSpace(sourceId)
                ? "Ability.Copy"
                : $"{sourceId}.Copy";
            string candidate = root;
            int suffix = 2;
            var existing = new HashSet<string>(
                _assets.Select(GetStableId),
                StringComparer.Ordinal);
            while (existing.Contains(candidate))
                candidate = $"{root}{suffix++}";
            return candidate;
        }

        private void CopyActiveTab()
        {
            if (_selected is not ScriptableObject source)
            {
                ShowNotification(new GUIContent("복사할 에셋을 선택하세요."));
                return;
            }

            string[] properties = GetPropertiesForTab(source, _activeTab);
            if (properties.Length == 0)
            {
                ShowNotification(new GUIContent("현재 탭에는 복사할 값이 없습니다."));
                return;
            }

            if (_tabClipboard != null)
                DestroyImmediate(_tabClipboard);
            _tabClipboard = Instantiate(source);
            _tabClipboard.hideFlags = HideFlags.HideAndDontSave;
            _tabClipboardType = source.GetType();
            _tabClipboardTab = _activeTab;
            RefreshQuickActionStates();
            ShowNotification(new GUIContent(
                $"{source.name} · {_activeTab} 값 복사 완료"));
        }

        private bool CanCopyActiveTab() =>
            _selected is ScriptableObject source
            && GetPropertiesForTab(source, _activeTab).Length > 0;

        private bool CanDuplicateSelected() =>
            _selected is ScriptableObject;

        private bool CanPasteActiveTab() =>
            _selected is ScriptableObject target
            && _tabClipboard != null
            && target.GetType() == _tabClipboardType
            && string.Equals(
                _activeTab,
                _tabClipboardTab,
                StringComparison.Ordinal);

        private void RefreshQuickActionStates()
        {
            _composeSetButton?.SetEnabled(_selected != null);
            _duplicateButton?.SetEnabled(CanDuplicateSelected());
            _copyTabButton?.SetEnabled(CanCopyActiveTab());
            _pasteTabButton?.SetEnabled(CanPasteActiveTab());
            _pingButton?.SetEnabled(_selected != null
                && !string.IsNullOrWhiteSpace(
                    AssetDatabase.GetAssetPath(_selected)));
        }

        private void PasteActiveTab()
        {
            if (_selected is not ScriptableObject target
                || _tabClipboard == null)
            {
                ShowNotification(new GUIContent("복사된 탭 값이 없습니다."));
                return;
            }
            if (target.GetType() != _tabClipboardType
                || !string.Equals(
                    _activeTab,
                    _tabClipboardTab,
                    StringComparison.Ordinal))
            {
                EditorUtility.DisplayDialog(
                    "붙여넣을 수 없음",
                    "탭 값은 같은 에셋 타입의 같은 탭에만 붙여넣을 수 있습니다.\n"
                    + $"복사 원본: {_tabClipboardType?.Name} / {_tabClipboardTab}\n"
                    + $"현재 대상: {target.GetType().Name} / {_activeTab}",
                    "확인");
                return;
            }

            string[] properties = GetPropertiesForTab(target, _activeTab);
            SerializedObject sourceSerialized = new(_tabClipboard);
            SerializedObject targetSerialized = new(target);
            Undo.RecordObject(target, $"{_activeTab} 값 붙여넣기");
            int copied = 0;
            for (int i = 0; i < properties.Length; i++)
            {
                SerializedProperty sourceProperty =
                    sourceSerialized.FindProperty(properties[i]);
                if (sourceProperty == null
                    || targetSerialized.FindProperty(properties[i]) == null)
                    continue;
                targetSerialized.CopyFromSerializedProperty(sourceProperty);
                copied++;
            }
            targetSerialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            RebuildDetail();
            ShowNotification(new GUIContent(
                $"{_activeTab} 값 {copied}개 붙여넣기 완료"));
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
                    "프로젝트의 메인 Ability/Passive/Effect/Set 에셋만 삭제할 수 있습니다.\n"
                    + "데이터베이스 내부 서브에셋은 해당 데이터베이스에서 관리해야 합니다.\n\n"
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
                    "기본 정보" => new[] { "abilityId", "editorMemo", "presentation", "abilityTagIds", "concurrency" },
                    "활성화 조건" => new[] { "activation" },
                    "비용/쿨다운" => new[] { "cost", "cooldown" },
                    "Variant" => new[] { "variants" },
                    "트리거" => new[] { "triggers", "cancelAbilitiesWithTag", "blockAbilitiesWithTag" },
                    "Effect" => new[] { "commitEffects", "endEffects" },
                    "저장/교체 정책" => new[] { "persistence" },
                    "정적 밸런스" => new[] { "balance" },
                    _ => Array.Empty<string>(),
                };
            }
            if (target is GameplayEffectSO)
            {
                return tab switch
                {
                    "기본 정보" => new[]
                    {
                        "effectId",
                        "editorMemo",
                        "polarity",
                        "presentation",
                        "durationType",
                        "durationSeconds",
                        "periodSeconds",
                    },
                    "Effect" => new[]
                    {
                        "grantedElement",
                        "elementPriority",
                        "ignorePassiveDurationModifiers",
                        "stackingKey",
                        "stackPolicy",
                        "maxStackCount",
                        "modifiers",
                        "grantedTagIds",
                    },
                    "저장/교체 정책" => new[] { "removalPolicy", "savePolicy" },
                    _ => Array.Empty<string>(),
                };
            }
            if (target is PassiveAbilitySO)
            {
                return tab switch
                {
                    "기본 정보" => new[]
                    {
                        "passiveId",
                        "editorMemo",
                        "presentation",
                        "characterSelectDescription",
                    },
                    "활성화 조건" => new[] { "activationType", "scope" },
                    "Effect" => new[] { "stackPolicy", "modifiers", "triggeredEffects" },
                    _ => Array.Empty<string>(),
                };
            }
            if (target is AbilitySetSO)
            {
                return tab switch
                {
                    "기본 정보" => new[]
                    {
                        "editorMemo",
                        "baseSet",
                        "abilityOverrides",
                        "playerSlots",
                        "combatBindings",
                        "additionalAbilities",
                    },
                    "활성화 조건" => new[]
                    {
                        "overrideComboRoutes",
                        "comboRoutes",
                        "overrideComboLinkWindow",
                        "comboLinkWindow",
                    },
                    "Variant" => new[] { "overrideCharge", "charge" },
                    _ => Array.Empty<string>(),
                };
            }
            return Array.Empty<string>();
        }

        private static string GetStableId(UnityEngine.Object asset) => asset switch
        {
            GameplayAbilitySO ability => string.IsNullOrWhiteSpace(ability.abilityId) ? ability.name : ability.abilityId,
            PassiveAbilitySO passive => string.IsNullOrWhiteSpace(passive.passiveId) ? passive.name : passive.passiveId,
            GameplayEffectSO effect => string.IsNullOrWhiteSpace(effect.effectId) ? effect.name : effect.effectId,
            _ => asset != null ? asset.name : "-",
        };

        private static string GetEditorMemo(UnityEngine.Object asset) => asset switch
        {
            GameplayAbilitySO ability => ability.editorMemo,
            PassiveAbilitySO passive => passive.editorMemo,
            GameplayEffectSO effect => effect.editorMemo,
            AbilitySetSO set => set.editorMemo,
            _ => null,
        };

        private static string GetSearchText(UnityEngine.Object asset)
        {
            string presentationName = asset switch
            {
                GameplayAbilitySO ability => ability.presentation?.displayName,
                PassiveAbilitySO passive => passive.presentation?.displayName,
                GameplayEffectSO effect => effect.presentation?.displayName,
                _ => null,
            };
            return $"{GetStableId(asset)} {asset?.name} {presentationName} {GetEditorMemo(asset)}";
        }

        private static Texture GetIcon(UnityEngine.Object asset)
        {
            if (asset is GameplayAbilitySO ability && ability.presentation?.icon != null)
                return ability.presentation.icon.texture;
            if (asset is PassiveAbilitySO passive && passive.presentation?.icon != null)
                return passive.presentation.icon.texture;
            if (asset is GameplayEffectSO effect && effect.presentation?.icon != null)
                return effect.presentation.icon.texture;
            return AssetPreview.GetMiniThumbnail(asset);
        }

        private static string GetTabGeneralHelp(string tab) => tab switch
        {
            "기본 정보" => "식별자, 화면 표시, 슬롯 구성 등 에셋의 기본 구조를 편집합니다.",
            "활성화 조건" => "Ability 발동 조건 또는 AbilitySet의 콤보 연결 조건을 설정합니다.",
            "비용/쿨다운" => "자원 소모 시점과 재사용 대기시간을 설정합니다.",
            "Variant" => "상황별 실행 Variant 또는 차지 단계별 Ability를 구성합니다.",
            "트리거" => "태그·Gameplay Event 기반 자동 활성화와 Ability 간 취소·차단 관계를 설정합니다.",
            "Effect" => "발동·종료 Effect와 Effect가 적용할 수치 변화를 설정합니다.",
            "저장/교체 정책" => "캐릭터 교체, 저장, 종료 시 유지하거나 제거할 범위를 설정합니다.",
            "정적 밸런스" => "밸런스 도구가 사용하는 기대 피해량과 메타데이터를 설정합니다.",
            "검증 결과" => "현재 에셋의 오류와 경고를 확인합니다.",
            _ => "현재 탭의 데이터를 편집합니다.",
        };

        private static string GetTabHelp(UnityEngine.Object target, string tab) =>
            (target, tab) switch
            {
                (GameplayAbilitySO, "기본 정보") =>
                    "런타임 고유 ID, 표시 이름·아이콘, 분류 태그와 동시 실행 정책을 편집합니다. "
                    + "ID는 저장 파일명과 별개이며 프로젝트 전체에서 중복되면 안 됩니다.",
                (GameplayAbilitySO, "활성화 조건") =>
                    "필요 태그, 차단 태그, 지상 여부 등 Prepare 전에 검사할 발동 조건을 설정합니다. "
                    + "조건 실패 시 비용과 쿨다운은 소비되지 않습니다.",
                (GameplayAbilitySO, "비용/쿨다운") =>
                    "Commit 시 소비할 자원과 재사용 대기시간·공유 쿨다운 그룹을 설정합니다. "
                    + "같은 그룹 ID를 쓰는 Ability는 쿨다운을 공유합니다.",
                (GameplayAbilitySO, "Variant") =>
                    "상황 조건에 따라 실제로 실행할 Payload를 우선순위 순으로 구성합니다. "
                    + "공격 Motion 해석 키의 단일 소스는 각 Motion Payload의 attackInfo.motionKey입니다.",
                (GameplayAbilitySO, "트리거") =>
                    "Owned Tag 변화·보유 상태 또는 Gameplay Event로 Ability를 자동 요청합니다. "
                    + "Immediate는 시간 제한이 있는 Background 실행에만 사용하고, 전투 모션은 Request를 사용하세요.",
                (GameplayAbilitySO, "Effect") =>
                    "Commit 직후와 실행 종료 시 적용할 GameplayEffect를 연결합니다. "
                    + "Variant 내부의 Owner/Target Effect와 적용 시점이 다르므로 중복 적용을 확인하세요.",
                (GameplayAbilitySO, "저장/교체 정책") =>
                    "캐릭터 교체·저장·런 종료 시 실행 상태를 유지하거나 종료할 정책을 설정합니다. "
                    + "Effect 자체의 저장 정책과 함께 검토해야 합니다.",
                (GameplayAbilitySO, "정적 밸런스") =>
                    "제작·밸런스 도구가 기대 피해와 역할을 비교할 때 쓰는 메타데이터입니다. "
                    + "실제 공격 수치는 Payload의 HitPhase가 권위 소스입니다.",
                (GameplayAbilitySO, "검증 결과") =>
                    "Ability ID, TaskGraph, Variant/Payload, Motion Key와 HitPhase 연결 오류를 확인합니다. "
                    + "오류 항목을 먼저 해결한 뒤 에셋을 양산하거나 저장하세요.",

                (AbilitySetSO, "기본 정보") =>
                    "액터에게 부여할 스킬 슬롯, 일반 공격 시퀀스와 추가 Ability를 구성합니다. "
                    + "몬스터 BT는 이 Set 안에서 aiSelectable인 Ability만 선택합니다.",
                (AbilitySetSO, "활성화 조건") =>
                    "선행 공격 이후 허용할 콤보 경로와 입력 연결 시간을 설정합니다. "
                    + "Ability 자체의 발동 조건과 별도로 Set 수준의 연결 순서를 정의합니다.",
                (AbilitySetSO, "Variant") =>
                    "차지 시간 단계별로 실행할 Ability를 연결합니다. "
                    + "여기서 Variant는 GameplayAbilitySO 내부 실행 Variant가 아니라 Set의 차지 분기입니다.",
                (AbilitySetSO, "검증 결과") =>
                    "슬롯 중복, null 참조, 콤보·차지 연결과 포함 Ability의 정합성을 확인합니다.",

                (GameplayEffectSO, "기본 정보") =>
                    "Effect 고유 ID, HUD 표시, 극성, 지속 방식과 주기를 설정합니다. "
                    + "Duration 시간과 Period 값은 런타임 Effect 수명주기에 직접 사용됩니다.",
                (GameplayEffectSO, "Effect") =>
                    "속성 부여, 스택 그룹·정책, 최대 스택, 스탯 Modifier와 부여 태그를 설정합니다. "
                    + "공유 Effect 수정 전에는 역참조 소비자를 확인하세요.",
                (GameplayEffectSO, "저장/교체 정책") =>
                    "Ability 종료·캐릭터 교체 때의 제거 정책과 세이브 데이터 포함 여부를 설정합니다.",
                (GameplayEffectSO, "검증 결과") =>
                    "지속시간·주기·스택 범위, Modifier Attribute와 저장 정책 조합을 확인합니다.",

                (PassiveAbilitySO, "기본 정보") =>
                    "Passive 고유 ID, 표시 정보와 캐릭터 선택 화면 요약을 편집합니다.",
                (PassiveAbilitySO, "활성화 조건") =>
                    "상시 적용인지 회피·가드 성공 같은 사건 기반 발동인지, 어느 캐릭터 범위에 적용할지 설정합니다.",
                (PassiveAbilitySO, "Effect") =>
                    "상시 Modifier와 조건 충족 시 적용할 GameplayEffect, 중첩 정책을 구성합니다.",
                (PassiveAbilitySO, "검증 결과") =>
                    "Passive ID, 발동 범위, Modifier와 조건부 Effect 참조의 정합성을 확인합니다.",
                _ => GetTabGeneralHelp(tab),
            };

        private static string GetPropertyLabel(string propertyName) => propertyName switch
        {
            "abilityId" => "Ability ID",
            "passiveId" => "Passive ID",
            "effectId" => "Effect ID",
            "editorMemo" => "메모",
            "polarity" => "효과 극성",
            "presentation" => "표시 정보",
            "characterSelectDescription" => "캐릭터 선택 요약",
            "activationType" => "패시브 발동 조건",
            "scope" => "적용 범위",
            "triggeredEffects" => "조건 충족 시 Effect",
            "abilityTagIds" => "Ability 태그",
            "triggers" => "자동 발동 트리거",
            "cancelAbilitiesWithTag" => "발동 시 취소할 Ability 태그",
            "blockAbilitiesWithTag" => "실행 중 차단할 Ability 태그",
            "concurrency" => "동시 실행 정책",
            "activation" => "활성화 조건",
            "cost" => "비용",
            "cooldown" => "쿨다운",
            "variants" => "실행 Variant",
            "commitEffects" => "발동 시 Effect",
            "endEffects" => "종료 시 Effect",
            "persistence" => "저장·교체 정책",
            "balance" => "정적 밸런스",
            "durationType" => "지속 방식",
            "durationSeconds" => "지속 시간(초)",
            "periodSeconds" => "주기(초)",
            "grantedElement" => "부여 속성",
            "elementPriority" => "속성 우선순위",
            "ignorePassiveDurationModifiers" => "패시브 지속시간 보정 무시",
            "stackingKey" => "스택 그룹 키",
            "stackPolicy" => "스택 정책",
            "maxStackCount" => "최대 스택",
            "modifiers" => "스탯 변경",
            "grantedTagIds" => "부여 태그",
            "removalPolicy" => "제거 정책",
            "savePolicy" => "저장 정책",
            "playerSlots" => "스킬 슬롯",
            "baseSet" => "공용 Base Set",
            "abilityOverrides" => "Ability 교체·제거",
            "combatBindings" => "일반 공격 슬롯",
            "additionalAbilities" => "공용 Ability",
            "overrideCharge" => "차지 구성 재정의",
            "overrideComboRoutes" => "콤보 라우트 재정의",
            "overrideComboLinkWindow" => "콤보 입력 시간 재정의",
            "comboRoutes" => "콤보 연계",
            "comboLinkWindow" => "콤보 입력 허용 시간",
            "charge" => "차지 단계",
            _ => ObjectNames.NicifyVariableName(propertyName),
        };

        private static string GetPropertyHelp(string propertyName) => propertyName switch
        {
            "abilityId" or "passiveId" or "effectId" => "저장 파일명과 별개인 런타임 고유 식별자입니다.",
            "presentation" => "표시 이름, 설명, 아이콘과 HUD 노출 정보를 설정합니다.",
            "polarity" => "HUD에서 버프·디버프·중립 테두리 색상을 결정합니다.",
            "characterSelectDescription" => "UI_CharacterSelect에 표시할 수치 없는 요약 설명입니다.",
            "activationType" => "상시 적용 또는 퍼펙트 회피·가드 성공 시 발동하도록 설정합니다.",
            "scope" => "활성 캐릭터, 소유 캐릭터 또는 출전 파티 최고값 정책을 설정합니다.",
            "triggeredEffects" => "조건부 패시브가 성공했을 때 적용할 GameplayEffect입니다.",
            "abilityTagIds" or "grantedTagIds" => "조건 판정과 다른 시스템 연동에 사용하는 태그입니다.",
            "triggers" => "태그 변화·현재 보유 태그·Gameplay Event에 반응할 발동 규칙입니다.",
            "cancelAbilitiesWithTag" => "Commit 성공 시 이 태그와 계층 일치하는 다른 실행을 취소합니다.",
            "blockAbilitiesWithTag" => "실행 중 이 태그와 계층 일치하는 새 Ability의 Prepare를 차단합니다.",
            "concurrency" => "같은 Ability가 이미 실행 중일 때 새 요청을 처리하는 방법입니다.",
            "activation" => "필요·차단 태그와 활성화 규칙을 설정합니다.",
            "cost" => "소모 자원, 소모량, 실제 차감 시점을 설정합니다.",
            "cooldown" => "재사용 대기시간과 공유 쿨다운 그룹을 설정합니다.",
            "variants" => "조건에 따라 선택할 실제 실행 Payload 목록입니다.",
            "commitEffects" => "Ability가 확정될 때 적용할 Effect입니다.",
            "endEffects" => "Ability 실행이 끝날 때 적용할 Effect입니다.",
            "grantedElement" => "Duration 또는 Infinite Effect가 활성화된 동안 부여할 전투 속성입니다.",
            "elementPriority" => "속성 Effect가 겹칠 때 높은 값이 우선합니다.",
            "ignorePassiveDurationModifiers" => "활성화하면 상태강화·상태이상 지속시간 패시브 보정을 적용하지 않습니다.",
            "playerSlots" => "스킬 입력 슬롯과 Ability의 연결입니다.",
            "baseSet" => "동일 타입 몬스터 등이 공유하는 공용 Set입니다. 파생 Set은 Base를 직접 수정하지 않고 Override만 소유합니다.",
            "abilityOverrides" => "Base Set의 유효 Ability를 다른 Ability로 교체하거나 파생 Set에서 제거합니다.",
            "combatBindings" => "일반 공격 종류별 순차 Ability 목록입니다.",
            "additionalAbilities" => "입력 슬롯 또는 전투 슬롯과 무관하게 이 AbilitySet이 액터에게 부여할 Ability입니다. BT도 이 목록의 Ability를 활성화할 수 있습니다.",
            "overrideCharge" => "켜면 Base Set의 차지 단계를 상속하지 않고 이 Set의 charge를 사용합니다.",
            "overrideComboRoutes" => "켜면 Base Set의 콤보 라우트를 상속하지 않고 이 Set의 목록을 사용합니다.",
            "overrideComboLinkWindow" => "켜면 Base Set의 콤보 입력 허용 시간을 이 Set의 값으로 교체합니다.",
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

        private static ToolbarMenu MakeToolbarMenu(
            string text,
            string tooltip)
        {
            var menu = new ToolbarMenu
            {
                text = text,
                tooltip = tooltip,
            };
            menu.style.marginLeft = 3f;
            menu.style.flexShrink = 0f;
            return menu;
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
