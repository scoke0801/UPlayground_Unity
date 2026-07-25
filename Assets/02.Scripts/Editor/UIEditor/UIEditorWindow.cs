using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.Editor.UI
{
    /// <summary>
    /// 프로젝트 UI 제작 도구의 단일 진입점.
    /// 개별 프리팹 빌더는 메뉴를 직접 노출하지 않고 이 창을 통해 실행한다.
    /// </summary>
    public sealed class UIEditorWindow : EditorWindow
    {
        private const string MenuPath = "UPlayGround/UI 에디터";
        private const string AllCategory = "전체";

        private static readonly Color HeaderColor = new(0.12f, 0.27f, 0.38f, 1f);
        private static readonly Color SidebarColor = new(0.13f, 0.13f, 0.13f, 1f);
        private static readonly Color CardColor = new(0.18f, 0.18f, 0.18f, 1f);
        private static readonly Color CardHoverColor = new(0.21f, 0.21f, 0.21f, 1f);
        private static readonly Color SelectedColor = new(0.18f, 0.38f, 0.52f, 1f);
        private static readonly Color MutedColor = new(0.66f, 0.66f, 0.66f, 1f);
        private static readonly Color BuilderColor = new(0.35f, 0.72f, 0.90f, 1f);
        private static readonly Color EditorColor = new(0.45f, 0.82f, 0.62f, 1f);
        private static readonly Color MaintenanceColor = new(0.84f, 0.67f, 0.30f, 1f);
        private static readonly Color WarningColor = new(0.92f, 0.42f, 0.34f, 1f);

        private readonly List<ToolDefinition> _tools = new();
        private readonly List<string> _categories = new();
        private readonly List<ToolDefinition> _visibleTools = new();

        private VisualElement _categoryRoot;
        private ScrollView _toolRoot;
        private ToolbarSearchField _searchField;
        private Label _contentTitle;
        private Label _resultCount;
        private string _selectedCategory = AllCategory;
        private ToolDefinition _selectedTool;
        private bool _revealSelectionAfterRefresh;
        private bool _focusSelectionAfterRefresh;
        private double _lastDirectionalNavigationTime;
        private int _lastDirectionalNavigation;
        private bool _focusCategoryAfterRefresh;

        [MenuItem(MenuPath, false, 120)]
        public static void Open()
        {
            var window = GetWindow<UIEditorWindow>();
            window.titleContent = new GUIContent("UI 에디터", EditorGUIUtility.IconContent("d_UnityEditor.UIBuilderModule").image);
            window.minSize = new Vector2(760f, 540f);
            window.Show();
        }

        public void CreateGUI()
        {
            BuildToolDefinitions();
            BuildCategoryList();

            VisualElement root = rootVisualElement;
            root.UnregisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            root.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            root.UnregisterCallback<NavigationMoveEvent>(OnNavigationMove, TrickleDown.TrickleDown);
            root.UnregisterCallback<NavigationMoveEvent>(OnNavigationMove);
            root.Clear();
            root.focusable = true;
            root.tabIndex = 0;
            root.style.flexDirection = FlexDirection.Column;
            root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            root.RegisterCallback<KeyDownEvent>(OnKeyDown);
            root.RegisterCallback<NavigationMoveEvent>(OnNavigationMove, TrickleDown.TrickleDown);
            root.RegisterCallback<NavigationMoveEvent>(OnNavigationMove);
            root.Add(BuildHeader());
            root.Add(BuildWorkspace());

            _focusCategoryAfterRefresh = true;
            RefreshNavigation();
            RefreshTools();
            ScheduleKeyboardFocusIfNeeded();
        }

        private void OnFocus()
        {
            ScheduleKeyboardFocusIfNeeded();
        }

        private void ScheduleKeyboardFocusIfNeeded(bool force = false)
        {
            VisualElement root = rootVisualElement;
            root.schedule.Execute(() =>
            {
                if (force || root.panel?.focusController?.focusedElement == null)
                    root.Focus();
            });
        }

        private static VisualElement BuildHeader()
        {
            var header = new VisualElement();
            header.style.backgroundColor = HeaderColor;
            header.style.paddingLeft = 18f;
            header.style.paddingRight = 18f;
            header.style.paddingTop = 15f;
            header.style.paddingBottom = 14f;

            var title = new Label("UPlayGround UI 에디터");
            title.style.fontSize = 20f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(title);

            var subtitle = new Label("UI 프리팹 생성·갱신, 데이터 편집, 유지보수 작업을 한 곳에서 관리합니다.");
            subtitle.style.marginTop = 3f;
            subtitle.style.color = new Color(0.84f, 0.91f, 0.95f, 1f);
            header.Add(subtitle);
            return header;
        }

        private VisualElement BuildWorkspace()
        {
            var workspace = new VisualElement();
            workspace.style.flexDirection = FlexDirection.Row;
            workspace.style.flexGrow = 1f;
            workspace.Add(BuildSidebar());
            workspace.Add(BuildMainContent());
            return workspace;
        }

        private VisualElement BuildSidebar()
        {
            var sidebar = new VisualElement();
            sidebar.style.width = 172f;
            sidebar.style.flexShrink = 0f;
            sidebar.style.backgroundColor = SidebarColor;
            sidebar.style.paddingLeft = 8f;
            sidebar.style.paddingRight = 8f;
            sidebar.style.paddingTop = 12f;
            sidebar.style.borderRightColor = new Color(0.24f, 0.24f, 0.24f, 1f);
            sidebar.style.borderRightWidth = 1f;

            var label = new Label("도구 분류");
            label.style.marginLeft = 8f;
            label.style.marginBottom = 8f;
            label.style.fontSize = 11f;
            label.style.color = MutedColor;
            sidebar.Add(label);

            _categoryRoot = new VisualElement();
            sidebar.Add(_categoryRoot);
            return sidebar;
        }

        private VisualElement BuildMainContent()
        {
            var main = new VisualElement();
            main.style.flexGrow = 1f;
            main.style.minWidth = 0f;

            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.alignItems = Align.Center;
            toolbar.style.paddingLeft = 16f;
            toolbar.style.paddingRight = 16f;
            toolbar.style.paddingTop = 10f;
            toolbar.style.paddingBottom = 10f;
            toolbar.style.borderBottomColor = new Color(0.24f, 0.24f, 0.24f, 1f);
            toolbar.style.borderBottomWidth = 1f;

            var heading = new VisualElement();
            heading.style.flexGrow = 1f;

            _contentTitle = new Label();
            _contentTitle.style.fontSize = 15f;
            _contentTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.Add(_contentTitle);

            _resultCount = new Label();
            _resultCount.style.marginTop = 2f;
            _resultCount.style.fontSize = 10f;
            _resultCount.style.color = MutedColor;
            heading.Add(_resultCount);
            toolbar.Add(heading);

            _searchField = new ToolbarSearchField();
            _searchField.style.width = 230f;
            _searchField.tooltip = "이름과 설명에서 도구 검색";
            _searchField.RegisterValueChangedCallback(_ => RefreshTools());
            toolbar.Add(_searchField);
            main.Add(toolbar);

            _toolRoot = new ScrollView(ScrollViewMode.Vertical);
            _toolRoot.style.flexGrow = 1f;
            _toolRoot.style.paddingLeft = 16f;
            _toolRoot.style.paddingRight = 16f;
            _toolRoot.style.paddingTop = 10f;
            _toolRoot.style.paddingBottom = 16f;
            main.Add(_toolRoot);
            return main;
        }

        private void BuildCategoryList()
        {
            _categories.Clear();
            _categories.Add(AllCategory);

            foreach (ToolDefinition tool in _tools)
            {
                if (!_categories.Contains(tool.Category))
                    _categories.Add(tool.Category);
            }
        }

        private void RefreshNavigation()
        {
            _categoryRoot.Clear();
            Button selectedButton = null;

            foreach (string category in _categories)
            {
                int count = CountTools(category);
                var button = new Button(() => SelectCategory(category))
                {
                    text = $"{category}   {count}",
                };
                button.style.height = 30f;
                button.style.marginTop = 1f;
                button.style.marginBottom = 1f;
                button.style.unityTextAlign = TextAnchor.MiddleLeft;
                button.style.paddingLeft = 10f;
                button.style.borderLeftWidth = 0f;
                button.style.borderRightWidth = 0f;
                button.style.borderTopWidth = 0f;
                button.style.borderBottomWidth = 0f;
                button.style.backgroundColor = category == _selectedCategory ? SelectedColor : Color.clear;
                _categoryRoot.Add(button);
                if (category == _selectedCategory)
                    selectedButton = button;
            }

            bool focusCategory = _focusCategoryAfterRefresh;
            _focusCategoryAfterRefresh = false;
            if (focusCategory && selectedButton != null)
                selectedButton.schedule.Execute(selectedButton.Focus);
        }

        private void SelectCategory(string category)
        {
            if (_selectedCategory == category)
                return;

            _selectedCategory = category;
            _focusCategoryAfterRefresh = true;
            RefreshNavigation();
            RefreshTools();
        }

        private int CountTools(string category)
        {
            if (category == AllCategory)
                return _tools.Count;

            int count = 0;
            foreach (ToolDefinition tool in _tools)
            {
                if (tool.Category == category)
                    count++;
            }

            return count;
        }

        private void RefreshTools()
        {
            if (_toolRoot == null)
                return;

            _toolRoot.Clear();
            _visibleTools.Clear();
            string query = _searchField?.value?.Trim() ?? string.Empty;
            string currentSection = null;
            int visibleCount = 0;
            VisualElement selectedCard = null;

            foreach (ToolDefinition tool in _tools)
            {
                if (_selectedCategory != AllCategory && tool.Category != _selectedCategory)
                    continue;
                if (!tool.Matches(query))
                    continue;

                if (_selectedCategory == AllCategory && currentSection != tool.Category)
                {
                    currentSection = tool.Category;
                    _toolRoot.Add(BuildSectionHeader(currentSection));
                }

                _visibleTools.Add(tool);
                VisualElement card = BuildToolCard(tool);
                _toolRoot.Add(card);
                if (ReferenceEquals(_selectedTool, tool))
                    selectedCard = card;
                visibleCount++;
            }

            _contentTitle.text = string.IsNullOrEmpty(query) ? _selectedCategory : $"‘{query}’ 검색 결과";
            _resultCount.text = $"{visibleCount}개 도구";

            if (visibleCount == 0)
            {
                var empty = new HelpBox("조건에 맞는 UI 도구가 없습니다. 검색어나 분류를 변경해 보세요.", HelpBoxMessageType.Info);
                empty.style.marginTop = 4f;
                _toolRoot.Add(empty);
            }

            bool revealSelection = _revealSelectionAfterRefresh;
            bool focusSelection = _focusSelectionAfterRefresh;
            _revealSelectionAfterRefresh = false;
            _focusSelectionAfterRefresh = false;
            if (selectedCard != null && (revealSelection || focusSelection))
            {
                selectedCard.schedule.Execute(() =>
                {
                    if (revealSelection)
                        _toolRoot?.ScrollTo(selectedCard);
                    if (focusSelection)
                        selectedCard.Focus();
                });
            }
        }

        private static VisualElement BuildSectionHeader(string category)
        {
            var header = new Label(category);
            header.style.marginTop = 8f;
            header.style.marginBottom = 4f;
            header.style.fontSize = 12f;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.color = new Color(0.78f, 0.78f, 0.78f, 1f);
            return header;
        }

        private VisualElement BuildToolCard(ToolDefinition tool)
        {
            bool selected = ReferenceEquals(_selectedTool, tool);
            var card = new VisualElement { focusable = true };
            card.style.flexDirection = FlexDirection.Row;
            card.style.alignItems = Align.Center;
            card.style.backgroundColor = selected ? SelectedColor : CardColor;
            card.style.marginBottom = 4f;
            card.style.paddingLeft = 12f;
            card.style.paddingRight = 9f;
            card.style.paddingTop = 10f;
            card.style.paddingBottom = 10f;
            card.style.borderTopLeftRadius = 4f;
            card.style.borderTopRightRadius = 4f;
            card.style.borderBottomLeftRadius = 4f;
            card.style.borderBottomRightRadius = 4f;
            card.RegisterCallback<PointerEnterEvent>(_ =>
            {
                if (!selected)
                    card.style.backgroundColor = CardHoverColor;
            });
            card.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                if (!selected)
                    card.style.backgroundColor = CardColor;
            });
            card.RegisterCallback<ClickEvent>(_ => SelectTool(tool, true));

            var kindMark = new VisualElement();
            kindMark.style.width = 3f;
            kindMark.style.alignSelf = Align.Stretch;
            kindMark.style.marginRight = 10f;
            kindMark.style.backgroundColor = GetKindColor(tool.Kind);
            card.Add(kindMark);

            var textArea = new VisualElement();
            textArea.style.flexGrow = 1f;
            textArea.style.flexShrink = 1f;

            var titleRow = new VisualElement();
            titleRow.style.flexDirection = FlexDirection.Row;
            titleRow.style.alignItems = Align.Center;

            var title = new Label(tool.Title);
            title.style.fontSize = 13f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleRow.Add(title);

            var kindLabel = new Label(GetKindLabel(tool.Kind));
            kindLabel.style.marginLeft = 7f;
            kindLabel.style.fontSize = 9f;
            kindLabel.style.color = GetKindColor(tool.Kind);
            titleRow.Add(kindLabel);
            textArea.Add(titleRow);

            var description = new Label(tool.Description);
            description.style.marginTop = 3f;
            description.style.color = MutedColor;
            description.style.whiteSpace = WhiteSpace.Normal;
            textArea.Add(description);
            card.Add(textArea);

            var executeButton = new Button(() => Execute(tool))
            {
                text = tool.ButtonLabel,
                tooltip = tool.Description,
            };
            executeButton.style.width = 96f;
            executeButton.style.height = 26f;
            executeButton.style.marginLeft = 12f;
            card.Add(executeButton);
            return card;
        }

        private void SelectTool(ToolDefinition tool, bool focusSelection)
        {
            _selectedTool = tool;
            _focusSelectionAfterRefresh = focusSelection;
            _revealSelectionAfterRefresh = true;
            RefreshTools();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            int direction = evt.keyCode switch
            {
                KeyCode.UpArrow => -1,
                KeyCode.DownArrow => 1,
                _ => 0,
            };
            if (direction == 0 || evt.altKey || evt.ctrlKey || evt.commandKey)
                return;

            HandleDirectionalNavigation(direction);
            evt.StopImmediatePropagation();
        }

        private void OnNavigationMove(NavigationMoveEvent evt)
        {
            int direction = evt.direction switch
            {
                NavigationMoveEvent.Direction.Up => -1,
                NavigationMoveEvent.Direction.Down => 1,
                _ => 0,
            };
            if (direction == 0)
                return;

            HandleDirectionalNavigation(direction);
            evt.StopImmediatePropagation();
        }

        private void HandleDirectionalNavigation(int direction)
        {
            double now = EditorApplication.timeSinceStartup;
            if (_lastDirectionalNavigation == direction
                && now - _lastDirectionalNavigationTime < 0.03d)
            {
                return;
            }

            _lastDirectionalNavigation = direction;
            _lastDirectionalNavigationTime = now;
            MoveCategorySelection(direction);
        }

        private void MoveCategorySelection(int direction)
        {
            if (_categories.Count == 0)
                return;

            int currentIndex = Mathf.Max(0, _categories.IndexOf(_selectedCategory));
            int nextIndex = Mathf.Clamp(currentIndex + direction, 0, _categories.Count - 1);
            SelectCategory(_categories[nextIndex]);
        }

        private static string GetKindLabel(ToolKind kind)
        {
            return kind switch
            {
                ToolKind.Editor => "데이터 편집",
                ToolKind.Maintenance => "유지보수",
                ToolKind.Warning => "주의 필요",
                _ => "프리팹",
            };
        }

        private static Color GetKindColor(ToolKind kind)
        {
            return kind switch
            {
                ToolKind.Editor => EditorColor,
                ToolKind.Maintenance => MaintenanceColor,
                ToolKind.Warning => WarningColor,
                _ => BuilderColor,
            };
        }

        private static void Execute(ToolDefinition tool)
        {
            if (tool.RequiresConfirmation &&
                !EditorUtility.DisplayDialog(
                    $"{tool.Title} 실행",
                    tool.ConfirmationMessage,
                    tool.ButtonLabel,
                    "취소"))
            {
                return;
            }

            try
            {
                tool.Execute();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("UI 도구 실행 실패", $"{tool.Title} 실행 중 오류가 발생했습니다. 콘솔을 확인하세요.", "확인");
            }
        }

        private void BuildToolDefinitions()
        {
            _tools.Clear();

            Add("편집기", "가이드 팝업 데이터", "가이드 페이지의 이미지·영상·문구 데이터를 편집합니다.",
                UPlayGround.Data.UI.EditorTools.GuidePopupDataEditorWindow.Open, "열기", ToolKind.Editor, false);

            Add("HUD", "플레이어 정보 HUD", "플레이어 정보 HUD 프리팹의 구조와 직렬화 참조를 갱신합니다.",
                UPlayGround.UI.HUD.EditorTools.UIHudPlayerInfoPrefabBuilder.Build);
            Add("HUD", "전투 스킬 + 퀵슬롯", "스킬 슬롯과 퀵슬롯 HUD 프리팹을 생성하거나 갱신합니다.",
                UPlayGround.UI.HUD.EditorTools.UIHudCombatWidgetsPrefabBuilder.Build);
            Add("HUD", "인게임 시계", "월드 시간 표시 HUD 프리팹을 생성하거나 갱신합니다.",
                UPlayGround.UI.HUD.EditorTools.UIHudWorldClockPrefabBuilder.Build);
            Add("HUD", "알림", "Notification과 알림 항목 프리팹을 생성하거나 갱신합니다.",
                UPlayGround.UI.HUD.Notification.EditorTools.UINotificationPrefabBuilder.Build);
            Add("HUD", "월드 마커", "인게임 월드 마커 HUD 패널과 마커 아이콘 프리팹 초안을 생성합니다.",
                UPlayGround.UI.EditorTools.WorldMarkerUIBuilder.Build,
                requiresConfirmation: false);

            Add("화면 UI", "신규 게임 캐릭터 선택", "캐릭터 선택 카드와 메인 UI 프리팹을 생성하거나 갱신합니다.",
                UPlayGround.UI.CharacterSelect.EditorTools.UICharacterSelectPrefabBuilder.Build);
            Add("화면 UI", "동료(파티)", "파티 UI 프리팹의 계층과 직렬화 참조를 갱신합니다.",
                UPlayGround.UI.Party.EditorTools.UIPartyMenuPrefabBuilder.Build);
            Add("화면 UI", "인벤토리", "인벤토리 UI 프리팹의 계층과 직렬화 참조를 갱신합니다.",
                UPlayGround.UI.Inventory.EditorTools.UIInventoryPrefabBuilder.Build);
            Add("화면 UI", "퀘스트", "퀘스트 UI 프리팹의 계층과 직렬화 참조를 갱신합니다.",
                UPlayGround.UI.Quest.EditorTools.UIQuestMenuPrefabBuilder.Build);
            Add("화면 UI", "제작", "제작 UI 프리팹의 계층과 직렬화 참조를 갱신합니다.",
                UPlayGround.UI.Crafting.EditorTools.UICraftMenuPrefabBuilder.Build);
            Add("화면 UI", "설정", "설정 메뉴 프리팹의 계층과 직렬화 참조를 갱신합니다.",
                UPlayGround.UI.SettingMenu.EditorTools.UISettingMenuPrefabBuilder.Build);
            Add("화면 UI", "세이브 슬롯", "세이브 슬롯 UI 프리팹의 계층과 직렬화 참조를 갱신합니다.",
                UPlayGround.UI.SaveMenu.EditorTools.UISaveSlotMenuPrefabBuilder.Build);
            Add("화면 UI", "맵 패널", "맵 UI 프리팹의 패널 구조와 직렬화 참조를 갱신합니다.",
                UPlayGround.UI.Map.EditorTools.UIMapPanelsBuilder.Build);
            Add("화면 UI", "몬스터 도감", "몬스터 도감 슬롯과 메인 UI 프리팹을 생성하거나 갱신합니다.",
                UPlayGround.UI.EditorTools.UIMonsterCodexPrefabBuilder.Build);

            Add("팝업 · 시스템", "가이드 팝업", "가이드 팝업 프리팹을 생성하거나 갱신합니다.",
                UPlayGround.UI.Guide.EditorTools.UIGuidePopupPrefabBuilder.Build);
            Add("팝업 · 시스템", "부활 팝업", "부활 팝업 프리팹의 계층과 직렬화 참조를 갱신합니다.",
                UPlayGround.UI.Respawn.EditorTools.UIRespawnPopupPrefabBuilder.Build);
            Add("팝업 · 시스템", "휴식 성장", "휴식 성장 UI 프리팹을 생성하거나 갱신합니다.",
                UPlayGround.UI.Growth.EditorTools.UIRestGrowthPrefabBuilder.Build);
            Add("팝업 · 시스템", "일시정지 메뉴", "일시정지 메뉴 프리팹의 계층과 직렬화 참조를 갱신합니다.",
                UPlayGround.UI.PauseMenu.EditorTools.UIPauseMenuPrefabBuilder.Build);
            Add("팝업 · 시스템", "월드 재스폰 안내", "월드 재스폰 안내 프리팹을 생성하거나 갱신합니다.",
                UPlayGround.UI.World.EditorTools.UIWorldRespawnNoticePrefabBuilder.Build);
            Add("팝업 · 시스템", "개발 치트 패널", "개발용 치트 UI 프리팹을 생성하거나 갱신합니다.",
                UPlayGround.UI.DevCheat.EditorTools.UIDevCheatPanelPrefabBuilder.Build);

            Add("유지보수", "Scene UI 콘텐츠 연결", "비어 있는 _sceneContent 참조만 찾아 연결합니다.",
                UPlayGround.UI.EditorTools.UISceneContentBinder.BindMissing, "연결", ToolKind.Maintenance, false);
            Add("유지보수", "Scene UI 콘텐츠 강제 재연결", "기존 값을 포함해 _sceneContent 참조를 다시 탐색하고 연결합니다.",
                UPlayGround.UI.EditorTools.UISceneContentBinder.BindForce, "재연결", ToolKind.Warning, true,
                "기존 _sceneContent 참조도 새 탐색 결과로 변경합니다. 계속하시겠습니까?");
        }

        private void Add(
            string category,
            string title,
            string description,
            Action execute,
            string buttonLabel = "생성/갱신",
            ToolKind kind = ToolKind.Builder,
            bool requiresConfirmation = true,
            string confirmationMessage = "기존 UI 프리팹의 계층이나 직렬화 필드가 변경될 수 있습니다. 계속하시겠습니까?")
        {
            _tools.Add(new ToolDefinition(
                category,
                title,
                description,
                buttonLabel,
                kind,
                requiresConfirmation,
                confirmationMessage,
                execute));
        }

        private enum ToolKind
        {
            Builder,
            Editor,
            Maintenance,
            Warning,
        }

        private sealed class ToolDefinition
        {
            public string Category { get; }
            public string Title { get; }
            public string Description { get; }
            public string ButtonLabel { get; }
            public ToolKind Kind { get; }
            public bool RequiresConfirmation { get; }
            public string ConfirmationMessage { get; }
            public Action Execute { get; }

            public ToolDefinition(
                string category,
                string title,
                string description,
                string buttonLabel,
                ToolKind kind,
                bool requiresConfirmation,
                string confirmationMessage,
                Action execute)
            {
                Category = category;
                Title = title;
                Description = description;
                ButtonLabel = buttonLabel;
                Kind = kind;
                RequiresConfirmation = requiresConfirmation;
                ConfirmationMessage = confirmationMessage;
                Execute = execute;
            }

            public bool Matches(string query)
            {
                if (string.IsNullOrEmpty(query))
                    return true;

                return Category.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                       Title.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                       Description.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }
    }
}
