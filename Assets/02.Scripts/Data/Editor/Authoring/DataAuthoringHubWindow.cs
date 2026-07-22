#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UPlayGround.Data.Editor.Authoring
{
    /// <summary>
    /// 콘텐츠 데이터 편집 도메인을 한 창에서 전환하는 UI Toolkit 저작 허브입니다.
    /// </summary>
    public sealed partial class DataAuthoringHubWindow : EditorWindow
    {
        private const string MenuPath = "UPlayGround/게임플레이/데이터 저작 허브";
        private const string SpecPath = "Assets/docs/TODO/unified-data-authoring-editor.md";
        private const float NavigationWidth = 220f;

        private readonly List<IDataDomainPanel> _panels = new List<IDataDomainPanel>();
        private readonly List<IDataDomainPanel> _visiblePanels = new List<IDataDomainPanel>();

        private ListView _navigationList;
        private VisualElement _domainHost;
        private Label _domainCountLabel;
        private IDataDomainPanel _activePanel;
        private string _navigationQuery = string.Empty;
        private string _pendingDomainId;
        private Object _pendingAsset;

        [UPlayGround.EditorTools.UPlaygroundTool(MenuPath, priority = 120)]
        public static void ShowWindow()
        {
            Open();
        }

        public static DataAuthoringHubWindow Open(string domainId = null, Object asset = null)
        {
            var window = GetWindow<DataAuthoringHubWindow>("데이터 저작 허브");
            window.minSize = new Vector2(1040f, 620f);
            window._pendingDomainId = domainId;
            window._pendingAsset = asset;
            window.Show();
            window.Focus();
            window.ApplyPendingSelection();
            return window;
        }

        private void OnEnable()
        {
            DataAuthoringDomainRegistry.Changed += RebuildDomains;
            Undo.undoRedoPerformed += ReloadActivePanel;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            SaveDirtyPanels("도메인 리로드 또는 창 종료 전에 미저장 데이터가 자동 저장되었습니다.");
            DataAuthoringDomainRegistry.Changed -= RebuildDomains;
            Undo.undoRedoPerformed -= ReloadActivePanel;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            _activePanel?.OnDeactivate();
            UnsubscribeUnsavedChanges();
            _activePanel = null;
            _panels.Clear();
            _visiblePanels.Clear();
            hasUnsavedChanges = false;
        }

        public override void SaveChanges()
        {
            SaveDirtyPanels();
            base.SaveChanges();
            UpdateUnsavedChangesState();
        }

        public override void DiscardChanges()
        {
            foreach (IDataDomainUnsavedChanges panel in _panels.OfType<IDataDomainUnsavedChanges>())
            {
                if (panel.HasUnsavedChanges)
                    panel.DiscardChanges();
            }

            base.DiscardChanges();
            UpdateUnsavedChangesState();
        }

        private void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.flexGrow = 1f;
            rootVisualElement.style.backgroundColor = DataAuthoringTheme.Window;
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnRootKeyDown);
            rootVisualElement.Add(BuildTopBar());

            var body = new TwoPaneSplitView(0, NavigationWidth, TwoPaneSplitViewOrientation.Horizontal);
            body.style.flexGrow = 1f;
            body.Add(BuildNavigation());

            _domainHost = new VisualElement { name = "domain-host" };
            _domainHost.style.flexGrow = 1f;
            _domainHost.style.backgroundColor = DataAuthoringTheme.Surface;
            body.Add(_domainHost);
            rootVisualElement.Add(body);

            RebuildDomains();
        }

        private VisualElement BuildTopBar()
        {
            var toolbar = new VisualElement { name = "hub-header" };
            toolbar.style.height = 58f;
            toolbar.style.flexShrink = 0f;
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.alignItems = Align.Center;
            toolbar.style.paddingLeft = 14f;
            toolbar.style.paddingRight = 12f;
            toolbar.style.backgroundColor = DataAuthoringTheme.SurfaceRaised;
            toolbar.style.borderBottomWidth = 1f;
            toolbar.style.borderBottomColor = DataAuthoringTheme.Border;

            var brandIcon = new Image
            {
                image = EditorGUIUtility.IconContent("ScriptableObject Icon").image,
                scaleMode = ScaleMode.ScaleToFit
            };
            brandIcon.style.width = 30f;
            brandIcon.style.height = 30f;
            brandIcon.style.marginRight = 9f;
            toolbar.Add(brandIcon);

            var brand = new VisualElement();
            brand.style.width = 190f;
            brand.style.flexDirection = FlexDirection.Row;
            brand.style.alignItems = Align.Center;

            var title = new Label("데이터 저작 허브");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 15f;
            brand.Add(title);

            var version = new Label("v1.1");
            version.style.fontSize = 9f;
            version.style.marginLeft = 8f;
            version.style.paddingLeft = 5f;
            version.style.paddingRight = 5f;
            version.style.height = 18f;
            version.style.unityTextAlign = TextAnchor.MiddleCenter;
            version.style.color = DataAuthoringTheme.Muted;
            DataAuthoringTheme.SetBorder(version);
            DataAuthoringTheme.Round(version, 4f);
            brand.Add(version);
            toolbar.Add(brand);

            _globalSearchField = new ToolbarSearchField
            {
                tooltip = "모든 도메인의 키·이름 검색 (Ctrl+K)"
            };
            _globalSearchField.style.height = 32f;
            _globalSearchField.style.minWidth = 260f;
            _globalSearchField.style.maxWidth = 620f;
            _globalSearchField.style.flexGrow = 1f;
            _globalSearchField.style.marginLeft = 16f;
            _globalSearchField.style.marginRight = 16f;
            _globalSearchField.RegisterValueChangedCallback(evt => ShowGlobalSearch(evt.newValue));
            toolbar.Add(_globalSearchField);

            var bulkButton = new ToolbarButton(OpenSpreadsheet) { text = "대량 편집" };
            bulkButton.tooltip = "기존 SO 스프레드시트 열기";
            DataAuthoringTheme.StyleButton(bulkButton);
            toolbar.Add(bulkButton);

            _validationButton = new ToolbarButton(RunAndShowValidation) { text = "검증 실행" };
            _validationButton.tooltip = "도메인 인라인 검증과 프로젝트 전역 검증 실행";
            DataAuthoringTheme.StyleButton(_validationButton);
            toolbar.Add(_validationButton);

            var helpButton = new ToolbarButton(OpenSpecification) { text = "?" };
            helpButton.tooltip = "통합 데이터 저작 에디터 설계 문서 열기";
            helpButton.style.width = 32f;
            helpButton.style.fontSize = 15f;
            DataAuthoringTheme.StyleButton(helpButton);
            toolbar.Add(helpButton);
            return toolbar;
        }

        private void OnRootKeyDown(KeyDownEvent evt)
        {
            if ((evt.ctrlKey || evt.commandKey) && evt.keyCode == KeyCode.K)
            {
                _globalSearchField?.Focus();
                evt.StopPropagation();
            }
        }

        private VisualElement BuildNavigation()
        {
            var navigation = new VisualElement();
            navigation.style.minWidth = 150f;
            navigation.style.backgroundColor = DataAuthoringTheme.SurfaceRaised;
            navigation.style.borderRightWidth = 1f;
            navigation.style.borderRightColor = DataAuthoringTheme.Border;

            var heading = new Label("도메인");
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.fontSize = 14f;
            heading.style.paddingLeft = 14f;
            heading.style.paddingTop = 14f;
            heading.style.paddingBottom = 10f;
            navigation.Add(heading);

            var search = new ToolbarSearchField { tooltip = "도메인 검색" };
            search.style.height = 30f;
            search.style.marginLeft = 12f;
            search.style.marginRight = 12f;
            search.style.marginBottom = 8f;
            search.RegisterValueChangedCallback(evt =>
            {
                _navigationQuery = evt.newValue ?? string.Empty;
                RefreshNavigation();
            });
            navigation.Add(search);

            _navigationList = new ListView
            {
                itemsSource = _visiblePanels,
                selectionType = SelectionType.Single,
                fixedItemHeight = 48f,
                makeItem = MakeNavigationItem,
                bindItem = BindNavigationItem
            };
            _navigationList.style.flexGrow = 1f;
            _navigationList.selectionChanged += OnNavigationSelectionChanged;
            navigation.Add(_navigationList);

            _domainCountLabel = new Label();
            _domainCountLabel.style.fontSize = 10f;
            _domainCountLabel.style.color = DataAuthoringTheme.Muted;
            _domainCountLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _domainCountLabel.style.paddingTop = 8f;
            _domainCountLabel.style.paddingBottom = 10f;
            _domainCountLabel.style.borderTopWidth = 1f;
            _domainCountLabel.style.borderTopColor = DataAuthoringTheme.Border;
            navigation.Add(_domainCountLabel);
            return navigation;
        }

        private static VisualElement MakeNavigationItem()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 12f;
            row.style.paddingRight = 10f;
            row.style.borderBottomWidth = 1f;
            row.style.borderBottomColor = DataAuthoringTheme.Border;

            var icon = new Image { name = "icon", scaleMode = ScaleMode.ScaleToFit };
            icon.style.width = 24f;
            icon.style.height = 24f;
            icon.style.marginRight = 9f;
            row.Add(icon);

            var label = new Label { name = "label" };
            label.style.flexGrow = 1f;
            label.style.fontSize = 12f;
            row.Add(label);

            var badge = new Label { name = "issue-badge" };
            DataAuthoringTheme.StyleBadge(badge, DataAuthoringTheme.Error);
            row.Add(badge);
            return row;
        }

        private void BindNavigationItem(VisualElement element, int index)
        {
            if (index < 0 || index >= _visiblePanels.Count)
                return;

            IDataDomainPanel panel = _visiblePanels[index];
            element.Q<Image>("icon").image = panel.Icon;
            element.Q<Label>("label").text = panel.DisplayName;
            int issueCount = _hasValidationRun
                ? _validationResults.Count(result => ReferenceEquals(result.Panel, panel))
                : 0;
            Label issueBadge = element.Q<Label>("issue-badge");
            issueBadge.text = issueCount.ToString();
            issueBadge.style.display = issueCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            element.tooltip = panel.DomainId;
        }

        private void RebuildDomains()
        {
            if (_navigationList == null || _domainHost == null)
                return;

            string activeDomainId = _activePanel?.DomainId;
            SaveDirtyPanels("도메인 구성을 다시 불러오기 전에 미저장 데이터가 자동 저장되었습니다.");
            _activePanel?.OnDeactivate();
            UnsubscribeUnsavedChanges();
            _activePanel = null;
            _panels.Clear();
            InvalidateValidation();

            foreach (DataAuthoringDomainRegistry.Registration registration in
                     DataAuthoringDomainRegistry.GetRegistrations())
            {
                try
                {
                    IDataDomainPanel panel = registration.Factory();
                    if (panel != null)
                    {
                        _panels.Add(panel);
                        if (panel is IDataDomainUnsavedChanges unsavedChanges)
                            unsavedChanges.UnsavedChangesChanged += UpdateUnsavedChangesState;
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            RefreshNavigation();
            string domainToSelect = _pendingDomainId ?? activeDomainId;
            IDataDomainPanel preferred = _panels.FirstOrDefault(panel => panel.DomainId == domainToSelect)
                                         ?? _visiblePanels.FirstOrDefault();
            if (preferred != null)
                SelectPanel(preferred);
            else
                ShowEmptyHub();

            ApplyPendingSelection();
            UpdateUnsavedChangesState();
        }

        private void SaveDirtyPanels(string automaticSaveMessage = null)
        {
            bool savedAny = false;
            foreach (IDataDomainUnsavedChanges panel in _panels.OfType<IDataDomainUnsavedChanges>())
            {
                if (!panel.HasUnsavedChanges)
                    continue;

                savedAny |= panel.SaveChanges();
            }

            UpdateUnsavedChangesState();
            if (savedAny && !string.IsNullOrEmpty(automaticSaveMessage))
                Debug.Log($"[DataAuthoringHub] {automaticSaveMessage}");
        }

        private void UnsubscribeUnsavedChanges()
        {
            foreach (IDataDomainUnsavedChanges panel in _panels.OfType<IDataDomainUnsavedChanges>())
                panel.UnsavedChangesChanged -= UpdateUnsavedChangesState;
        }

        private void UpdateUnsavedChangesState()
        {
            hasUnsavedChanges = _panels
                .OfType<IDataDomainUnsavedChanges>()
                .Any(panel => panel.HasUnsavedChanges);
            saveChangesMessage = "데이터 저작 허브에 저장하지 않은 변경이 있습니다.";
        }

        private void RefreshNavigation()
        {
            _visiblePanels.Clear();
            _visiblePanels.AddRange(_panels.Where(panel =>
                string.IsNullOrWhiteSpace(_navigationQuery)
                || panel.DisplayName.IndexOf(_navigationQuery, StringComparison.CurrentCultureIgnoreCase) >= 0
                || panel.DomainId.IndexOf(_navigationQuery, StringComparison.OrdinalIgnoreCase) >= 0));

            _navigationList?.Rebuild();
            if (_domainCountLabel != null)
                _domainCountLabel.text = $"{_visiblePanels.Count:N0} / {_panels.Count:N0}개 도메인";

            if (_activePanel != null && !_visiblePanels.Contains(_activePanel))
            {
                IDataDomainPanel first = _visiblePanels.FirstOrDefault();
                if (first != null)
                    SelectPanel(first);
                else
                    ShowEmptyHub("검색 조건에 맞는 도메인이 없습니다.");
            }
        }

        private void OnNavigationSelectionChanged(IEnumerable<object> selection)
        {
            IDataDomainPanel panel = selection.OfType<IDataDomainPanel>().FirstOrDefault();
            if (panel != null)
                SelectPanel(panel);
        }

        private void SelectPanel(IDataDomainPanel panel)
        {
            if (panel == null || _domainHost == null)
                return;

            if (!ReferenceEquals(_activePanel, panel))
            {
                _activePanel?.OnDeactivate();
                _activePanel = panel;
                _domainHost.Clear();
                _domainHost.Add(panel.Root);
                panel.OnActivate();
            }
            else if (_domainHost.IndexOf(panel.Root) < 0)
            {
                _domainHost.Clear();
                _domainHost.Add(panel.Root);
            }

            int navigationIndex = _visiblePanels.IndexOf(panel);
            if (navigationIndex >= 0)
                _navigationList?.SetSelectionWithoutNotify(new[] { navigationIndex });
        }

        private void ApplyPendingSelection()
        {
            if (_domainHost == null || _panels.Count == 0)
                return;

            if (!string.IsNullOrEmpty(_pendingDomainId))
            {
                IDataDomainPanel panel = _panels.FirstOrDefault(candidate => candidate.DomainId == _pendingDomainId);
                if (panel != null)
                    SelectPanel(panel);
            }

            if (_pendingAsset != null)
                _activePanel?.SelectAsset(_pendingAsset);

            _pendingDomainId = null;
            _pendingAsset = null;
        }

        private void ReloadActivePanel()
        {
            _activePanel?.OnReload();
            InvalidateValidation();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode
                || state == PlayModeStateChange.ExitingPlayMode)
            {
                ReloadActivePanel();
            }
        }

        private void ShowEmptyHub(string message = null)
        {
            if (_domainHost == null)
                return;

            _activePanel?.OnDeactivate();
            _activePanel = null;
            _domainHost.Clear();

            var empty = new VisualElement();
            empty.style.flexGrow = 1f;
            empty.style.justifyContent = Justify.Center;
            empty.style.alignItems = Align.Center;

            var title = new Label(message ?? "등록된 데이터 도메인이 없습니다.");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 14f;
            empty.Add(title);

            var description = new Label("좌측에서 데이터 도메인을 선택하거나 전역 검색을 사용하세요.");
            description.style.color = DataAuthoringTheme.Muted;
            description.style.marginTop = 6f;
            empty.Add(description);
            _domainHost.Add(empty);
        }

        private static void OpenSpecification()
        {
            TextAsset specification = AssetDatabase.LoadAssetAtPath<TextAsset>(SpecPath);
            if (specification != null)
                AssetDatabase.OpenAsset(specification);
            else
                Debug.LogWarning($"데이터 저작 허브 설계 문서를 찾을 수 없습니다: {SpecPath}");
        }
    }
}
#endif
