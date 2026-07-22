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
    public sealed class DataDomainFilter<TAsset> where TAsset : class
    {
        public DataDomainFilter(string label, Func<TAsset, bool> predicate)
        {
            Label = label;
            Predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }

        public string Label { get; }
        public Func<TAsset, bool> Predicate { get; }
    }

    /// <summary>
    /// 데이터 도메인이 공통으로 사용하는 목록/상세 master-detail 셸입니다.
    /// 도메인은 로드, 키/라벨, 상세 폼과 필요한 CRUD 동작만 구현합니다.
    /// </summary>
    public abstract class DataDomainPanel<TAsset> : IDataDomainPanel where TAsset : class
    {
        private readonly List<TAsset> _assets = new List<TAsset>();
        private readonly List<TAsset> _filteredAssets = new List<TAsset>();
        private readonly HashSet<string> _duplicateKeys = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<ToolbarToggle> _filterToggles = new List<ToolbarToggle>();

        private VisualElement _root;
        private VisualElement _detailPane;
        private ListView _listView;
        private Label _countLabel;
        private ToolbarButton _duplicateButton;
        private ToolbarButton _deleteButton;
        private ToolbarSearchField _searchField;
        private VisualElement _inlineValidationPane;
        private IReadOnlyList<DataDomainFilter<TAsset>> _filters;
        private int _selectedFilterIndex;
        private TAsset _selected;
        private bool _assetsLoaded;

        public abstract string DomainId { get; }
        public abstract string DisplayName { get; }
        public virtual Texture2D Icon => null;

        public VisualElement Root
        {
            get
            {
                if (_root == null)
                    BuildRoot();
                return _root;
            }
        }

        protected IReadOnlyList<TAsset> Assets => _assets;
        protected TAsset Selected => _selected;
        protected virtual float ListPanelWidth => 300f;
        protected virtual string CreateButtonLabel => "+ 새로 만들기";
        protected virtual bool CanCreate => false;
        protected virtual bool CanDuplicate(TAsset asset) => false;
        protected virtual bool CanDelete(TAsset asset) => false;

        protected abstract IEnumerable<TAsset> LoadAssets();
        protected abstract string KeyOf(TAsset asset);
        protected abstract string LabelOf(TAsset asset);
        protected abstract VisualElement BuildDetail(TAsset asset);

        protected virtual Sprite IconOf(TAsset asset) => null;
        protected virtual IEnumerable<DataDomainFilter<TAsset>> CreateFilters()
        {
            return Array.Empty<DataDomainFilter<TAsset>>();
        }

        protected virtual void CreateNew() { }
        protected virtual TAsset Duplicate(TAsset asset) => null;
        protected virtual bool Delete(TAsset asset) => false;
        protected virtual void AddToolbarActions(Toolbar toolbar) { }
        protected virtual void OnSelectionChanged(TAsset asset) { }
        protected virtual IEnumerable<DataAuthoringIssue> GetIssues(TAsset asset)
        {
            return Array.Empty<DataAuthoringIssue>();
        }

        public virtual void OnActivate()
        {
            RefreshAssets();
        }

        public virtual void OnDeactivate() { }

        public virtual void OnReload()
        {
            RefreshAssets();
        }

        public void SelectAsset(Object asset)
        {
            if (!(asset is TAsset typedAsset))
                return;

            if (!_assets.Contains(typedAsset))
                RefreshAssets();

            int index = _filteredAssets.IndexOf(typedAsset);
            if (index < 0)
            {
                SetFilterIndexWithoutRefresh(0);
                _searchField?.SetValueWithoutNotify(string.Empty);
                RefreshFilteredList();
                index = _filteredAssets.IndexOf(typedAsset);
            }

            if (index >= 0)
                _listView?.SetSelection(index);
        }

        public IEnumerable<DataAuthoringSearchEntry> Search(string query)
        {
            EnsureAssetsLoaded();
            string normalized = query?.Trim() ?? string.Empty;
            if (normalized.Length == 0)
                return Array.Empty<DataAuthoringSearchEntry>();

            return _assets
                .Where(asset => MatchesSearch(asset, normalized))
                .Select(asset => new DataAuthoringSearchEntry(
                    this,
                    KeyOf(asset),
                    LabelOf(asset),
                    asset,
                    IconOf(asset),
                    asset as Object))
                .ToArray();
        }

        public void SelectSearchEntry(DataAuthoringSearchEntry entry)
        {
            if (!(entry.Value is TAsset asset))
                return;

            EnsureAssetsLoaded();
            int index = _filteredAssets.IndexOf(asset);
            if (index < 0)
            {
                SetFilterIndexWithoutRefresh(0);
                _searchField?.SetValueWithoutNotify(string.Empty);
                RefreshFilteredList(asset);
                index = _filteredAssets.IndexOf(asset);
            }

            if (index >= 0)
                _listView?.SetSelection(index);
        }

        public IEnumerable<DataAuthoringValidationResult> Validate()
        {
            EnsureAssetsLoaded();
            foreach (TAsset asset in _assets)
            {
                foreach (DataAuthoringIssue issue in GetIssues(asset) ?? Array.Empty<DataAuthoringIssue>())
                {
                    yield return new DataAuthoringValidationResult(
                        this,
                        DisplayName,
                        KeyOf(asset),
                        LabelOf(asset),
                        issue,
                        asset);
                }
            }
        }

        public bool OwnsAsset(Object asset)
        {
            if (!(asset is TAsset typedAsset))
                return false;
            EnsureAssetsLoaded();
            return _assets.Contains(typedAsset);
        }

        public IEnumerable<DataAuthoringIssue> IssuesFor(Object asset)
        {
            return asset is TAsset typedAsset
                ? GetIssues(typedAsset)
                : Array.Empty<DataAuthoringIssue>();
        }

        protected void RefreshAssets(TAsset preferredSelection = null)
        {
            TAsset selection = preferredSelection != null ? preferredSelection : _selected;
            _assets.Clear();
            _assets.AddRange((LoadAssets() ?? Array.Empty<TAsset>()).Where(asset => asset != null));
            _assetsLoaded = true;
            RebuildDuplicateKeys();
            RefreshFilteredList(selection);
        }

        protected bool HasDuplicateKey(TAsset asset)
        {
            string key = asset != null ? KeyOf(asset) : null;
            return !string.IsNullOrEmpty(key) && _duplicateKeys.Contains(key);
        }

        /// <summary>
        /// 상세 바인딩 값이 바뀌었을 때 목록과 중복 키만 갱신합니다.
        /// 입력 중 상세 폼을 재생성하지 않으므로 포커스와 커서가 유지됩니다.
        /// </summary>
        protected void NotifyAssetChanged(TAsset asset)
        {
            RebuildDuplicateKeys();
            RefreshFilteredList(asset, false);
            RefreshInlineValidation(asset);
        }

        private void BuildRoot()
        {
            _filters = new[] { new DataDomainFilter<TAsset>("전체", _ => true) }
                .Concat(CreateFilters() ?? Array.Empty<DataDomainFilter<TAsset>>())
                .ToArray();

            _root = new VisualElement { name = $"{DomainId}-domain" };
            _root.style.flexGrow = 1f;
            _root.style.backgroundColor = DataAuthoringTheme.Surface;
            _root.Add(BuildToolbar());

            var body = new TwoPaneSplitView(0, ListPanelWidth, TwoPaneSplitViewOrientation.Horizontal);
            body.style.flexGrow = 1f;
            body.Add(BuildListPane());

            _detailPane = new ScrollView(ScrollViewMode.Vertical);
            _detailPane.style.flexGrow = 1f;
            _detailPane.style.paddingLeft = 12f;
            _detailPane.style.paddingRight = 14f;
            _detailPane.style.paddingTop = 12f;
            _detailPane.style.paddingBottom = 12f;
            _detailPane.style.backgroundColor = DataAuthoringTheme.SurfaceRaised;
            _detailPane.style.borderLeftWidth = 1f;
            _detailPane.style.borderLeftColor = DataAuthoringTheme.Border;
            body.Add(_detailPane);

            _root.Add(body);
            ShowEmptyDetail();
        }

        private VisualElement BuildToolbar()
        {
            var container = new VisualElement();
            container.style.flexShrink = 0f;
            container.style.backgroundColor = DataAuthoringTheme.SurfaceRaised;
            container.style.borderBottomWidth = 1f;
            container.style.borderBottomColor = DataAuthoringTheme.Border;

            var toolbar = new Toolbar();
            toolbar.style.height = 48f;
            toolbar.style.alignItems = Align.Center;
            toolbar.style.paddingLeft = 12f;
            toolbar.style.paddingRight = 10f;
            toolbar.style.backgroundColor = Color.clear;

            var createButton = new ToolbarButton(() =>
            {
                CreateNew();
                RefreshAssets();
            }) { text = CreateButtonLabel };
            createButton.SetEnabled(CanCreate);
            DataAuthoringTheme.StyleButton(createButton, true);
            toolbar.Add(createButton);

            _duplicateButton = new ToolbarButton(DuplicateSelected) { text = "복제" };
            DataAuthoringTheme.StyleButton(_duplicateButton);
            toolbar.Add(_duplicateButton);

            _deleteButton = new ToolbarButton(DeleteSelected) { text = "삭제" };
            DataAuthoringTheme.StyleButton(_deleteButton);
            _deleteButton.style.color = DataAuthoringTheme.Error;
            toolbar.Add(_deleteButton);

            AddToolbarActions(toolbar);

            var flexibleSpace = new VisualElement();
            flexibleSpace.style.flexGrow = 1f;
            toolbar.Add(flexibleSpace);

            var refreshButton = new ToolbarButton(() => RefreshAssets()) { text = "↻ 새로고침" };
            DataAuthoringTheme.StyleButton(refreshButton);
            toolbar.Add(refreshButton);
            UpdateSelectionButtons();
            container.Add(toolbar);
            return container;
        }

        private VisualElement BuildListPane()
        {
            var pane = new VisualElement();
            pane.style.minWidth = 180f;
            pane.style.backgroundColor = DataAuthoringTheme.Surface;

            var filterBar = new VisualElement();
            filterBar.style.flexDirection = FlexDirection.Row;
            filterBar.style.flexWrap = Wrap.Wrap;
            filterBar.style.paddingLeft = 10f;
            filterBar.style.paddingRight = 8f;
            filterBar.style.paddingTop = 8f;
            filterBar.style.paddingBottom = 5f;
            _filterToggles.Clear();
            for (int i = 0; i < _filters.Count; i++)
            {
                int filterIndex = i;
                var toggle = new ToolbarToggle
                {
                    text = _filters[i].Label,
                    value = i == _selectedFilterIndex
                };
                toggle.style.height = 26f;
                toggle.style.marginRight = 4f;
                toggle.style.marginBottom = 3f;
                toggle.RegisterValueChangedCallback(evt => SelectFilter(toggle, filterIndex, evt.newValue));
                _filterToggles.Add(toggle);
                filterBar.Add(toggle);
            }
            pane.Add(filterBar);

            var searchRow = new VisualElement();
            searchRow.style.flexDirection = FlexDirection.Row;
            searchRow.style.paddingLeft = 10f;
            searchRow.style.paddingRight = 10f;
            searchRow.style.paddingTop = 3f;
            searchRow.style.paddingBottom = 8f;
            _searchField = new ToolbarSearchField { tooltip = $"{DisplayName} 검색" };
            _searchField.style.height = 30f;
            _searchField.style.flexGrow = 1f;
            _searchField.RegisterValueChangedCallback(_ => RefreshFilteredList());
            searchRow.Add(_searchField);
            pane.Add(searchRow);

            _listView = new ListView
            {
                itemsSource = _filteredAssets,
                selectionType = SelectionType.Single,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                makeItem = MakeListItem,
                bindItem = BindListItem
            };
            _listView.style.flexGrow = 1f;
            _listView.selectionChanged += OnListSelectionChanged;
            pane.Add(_listView);

            _countLabel = new Label();
            _countLabel.style.height = 32f;
            _countLabel.style.paddingLeft = 12f;
            _countLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            _countLabel.style.fontSize = 10f;
            _countLabel.style.color = DataAuthoringTheme.Muted;
            _countLabel.style.borderTopWidth = 1f;
            _countLabel.style.borderTopColor = DataAuthoringTheme.Border;
            pane.Add(_countLabel);
            return pane;
        }

        private VisualElement MakeListItem()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.minHeight = 56f;
            row.style.paddingLeft = 10f;
            row.style.paddingRight = 10f;
            row.style.paddingTop = 6f;
            row.style.paddingBottom = 6f;
            row.style.borderBottomWidth = 1f;
            row.style.borderBottomColor = DataAuthoringTheme.Border;

            var icon = new Image { name = "icon", scaleMode = ScaleMode.ScaleToFit };
            icon.style.width = 38f;
            icon.style.height = 38f;
            icon.style.flexShrink = 0f;
            icon.style.marginRight = 9f;
            icon.style.backgroundColor = DataAuthoringTheme.Window;
            DataAuthoringTheme.SetBorder(icon);
            DataAuthoringTheme.Round(icon, 3f);
            row.Add(icon);

            var text = new VisualElement();
            text.style.flexGrow = 1f;
            text.style.minWidth = 0f;
            var label = new Label { name = "label" };
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.overflow = Overflow.Hidden;
            label.style.textOverflow = TextOverflow.Ellipsis;
            text.Add(label);
            var key = new Label { name = "key" };
            key.style.fontSize = 10f;
            key.style.color = DataAuthoringTheme.Muted;
            key.style.marginTop = 2f;
            text.Add(key);
            row.Add(text);

            var duplicateBadge = new Label("중복") { name = "duplicate-badge" };
            DataAuthoringTheme.StyleBadge(duplicateBadge, DataAuthoringTheme.Error);
            row.Add(duplicateBadge);

            var issueBadge = new Label { name = "issue-badge" };
            issueBadge.style.marginLeft = 4f;
            issueBadge.style.width = 22f;
            issueBadge.style.unityTextAlign = TextAnchor.MiddleCenter;
            issueBadge.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(issueBadge);
            return row;
        }

        private void BindListItem(VisualElement element, int index)
        {
            if (index < 0 || index >= _filteredAssets.Count)
                return;

            TAsset asset = _filteredAssets[index];
            Image icon = element.Q<Image>("icon");
            icon.image = null;
            icon.sprite = IconOf(asset);
            element.Q<Label>("label").text = LabelOf(asset);
            string key = KeyOf(asset);
            element.Q<Label>("key").text = string.IsNullOrWhiteSpace(key) ? string.Empty : $"ID · {key}";
            element.Q<Label>("duplicate-badge").style.display =
                HasDuplicateKey(asset) ? DisplayStyle.Flex : DisplayStyle.None;
            DataAuthoringIssue[] issues = (GetIssues(asset) ?? Array.Empty<DataAuthoringIssue>()).ToArray();
            Label issueBadge = element.Q<Label>("issue-badge");
            issueBadge.text = issues.Length > 0 ? (issues.Any(issue => issue.Severity == DataAuthoringIssueSeverity.Error) ? "●" : "▲") : "✓";
            issueBadge.style.display = DisplayStyle.Flex;
            issueBadge.style.color = issues.Length > 0 ? IssueColor(issues) : DataAuthoringTheme.Success;
            element.tooltip = asset is Object unityAsset
                ? AssetDatabase.GetAssetPath(unityAsset)
                : string.Empty;
        }

        private void SelectFilter(ToolbarToggle selectedToggle, int filterIndex, bool enabled)
        {
            if (!enabled)
            {
                selectedToggle.SetValueWithoutNotify(_selectedFilterIndex == filterIndex);
                return;
            }

            _selectedFilterIndex = filterIndex;
            foreach (ToolbarToggle toggle in _filterToggles)
                toggle.SetValueWithoutNotify(toggle == selectedToggle);
            RefreshFilteredList();
        }

        private void SetFilterIndexWithoutRefresh(int filterIndex)
        {
            _selectedFilterIndex = filterIndex;
            for (int i = 0; i < _filterToggles.Count; i++)
                _filterToggles[i].SetValueWithoutNotify(i == filterIndex);
        }

        private void RefreshFilteredList(TAsset preferredSelection = null, bool rebuildDetail = true)
        {
            if (_filters == null)
                return;

            string query = _searchField?.value?.Trim() ?? string.Empty;
            Func<TAsset, bool> filter = _filters[Mathf.Clamp(_selectedFilterIndex, 0, _filters.Count - 1)].Predicate;

            _filteredAssets.Clear();
            _filteredAssets.AddRange(_assets.Where(asset =>
                filter(asset) &&
                (query.Length == 0 || MatchesSearch(asset, query))));

            _listView?.Rebuild();
            UpdateCountLabel();

            TAsset selection = preferredSelection != null ? preferredSelection : _selected;
            int selectedIndex = selection != null ? _filteredAssets.IndexOf(selection) : -1;
            if (selectedIndex >= 0)
            {
                _listView?.SetSelectionWithoutNotify(new[] { selectedIndex });
                if (rebuildDetail)
                {
                    SetSelected(selection);
                }
                else
                {
                    _selected = selection;
                    UpdateSelectionButtons();
                }
            }
            else
            {
                _listView?.ClearSelection();
                SetSelected(null);
            }
        }

        private bool MatchesSearch(TAsset asset, string query)
        {
            return (LabelOf(asset) ?? string.Empty).IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0
                   || (KeyOf(asset) ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void OnListSelectionChanged(IEnumerable<object> selection)
        {
            SetSelected(selection.OfType<TAsset>().FirstOrDefault());
        }

        private void SetSelected(TAsset asset)
        {
            _selected = asset;
            UpdateSelectionButtons();
            UpdateCountLabel();
            RebuildDetail();
            OnSelectionChanged(asset);
        }

        private void UpdateCountLabel()
        {
            if (_countLabel == null)
                return;

            _countLabel.text = $"선택 {(_selected != null ? 1 : 0):N0}개 / 전체 {_assets.Count:N0}개 · 현재 {_filteredAssets.Count:N0}개";
        }

        private void RebuildDetail()
        {
            if (_detailPane == null)
                return;

            _detailPane.Clear();
            if (_selected == null)
            {
                ShowEmptyDetail();
                return;
            }

            VisualElement detail = BuildDetail(_selected);
            if (detail != null)
            {
                StyleDetail(detail, _selected);
                _detailPane.Add(detail);
            }
        }

        private void StyleDetail(VisualElement detail, TAsset asset)
        {
            detail.style.flexGrow = 1f;

            Toolbar header = detail.Q<Toolbar>();
            if (header != null)
            {
                header.style.minHeight = 48f;
                header.style.paddingLeft = 10f;
                header.style.paddingRight = 8f;
                header.style.backgroundColor = Color.clear;
                header.style.borderBottomWidth = 1f;
                header.style.borderBottomColor = DataAuthoringTheme.Border;
            }

            var breadcrumb = new Label($"{DisplayName}  ›  {LabelOf(asset)}");
            breadcrumb.style.fontSize = 10f;
            breadcrumb.style.color = DataAuthoringTheme.Muted;
            breadcrumb.style.marginLeft = 4f;
            breadcrumb.style.marginBottom = 4f;
            detail.Insert(0, breadcrumb);

            _inlineValidationPane = new VisualElement();
            int insertIndex = header != null ? detail.IndexOf(header) + 1 : 1;
            detail.Insert(insertIndex, _inlineValidationPane);
            RefreshInlineValidation(asset);
        }

        private void RefreshInlineValidation(TAsset asset)
        {
            if (_inlineValidationPane == null || asset == null)
                return;

            _inlineValidationPane.Clear();
            DataAuthoringIssue[] issues = (GetIssues(asset) ?? Array.Empty<DataAuthoringIssue>()).ToArray();
            _inlineValidationPane.style.display = issues.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (issues.Length == 0)
                return;

            _inlineValidationPane.style.marginTop = 10f;
            _inlineValidationPane.style.marginBottom = 5f;
            _inlineValidationPane.style.paddingLeft = 9f;
            _inlineValidationPane.style.paddingRight = 9f;
            _inlineValidationPane.style.paddingTop = 7f;
            _inlineValidationPane.style.paddingBottom = 7f;
            _inlineValidationPane.style.backgroundColor = DataAuthoringTheme.Window;
            DataAuthoringTheme.SetBorder(_inlineValidationPane);
            DataAuthoringTheme.Round(_inlineValidationPane, 4f);

            var heading = new Label($"검증 결과 · {issues.Length:N0}건");
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.marginBottom = 5f;
            _inlineValidationPane.Add(heading);

            foreach (DataAuthoringIssue issue in issues)
            {
                Color color = IssueColor(new[] { issue });
                var row = new VisualElement();
                row.style.minHeight = 30f;
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginBottom = 3f;
                row.style.paddingLeft = 8f;
                row.style.paddingRight = 8f;
                row.style.backgroundColor = new Color(color.r, color.g, color.b, 0.12f);
                DataAuthoringTheme.SetBorder(row, new Color(color.r, color.g, color.b, 0.35f));
                DataAuthoringTheme.Round(row, 3f);

                var symbol = new Label(issue.Severity == DataAuthoringIssueSeverity.Error ? "●" : "▲");
                symbol.style.width = 22f;
                symbol.style.color = color;
                row.Add(symbol);

                var message = new Label(issue.Message);
                message.style.flexGrow = 1f;
                message.style.whiteSpace = WhiteSpace.Normal;
                row.Add(message);
                _inlineValidationPane.Add(row);
            }
        }

        private void ShowEmptyDetail()
        {
            if (_detailPane == null)
                return;

            var empty = new Label($"왼쪽 목록에서 {DisplayName} 데이터를 선택하세요.");
            empty.style.unityTextAlign = TextAnchor.MiddleCenter;
            empty.style.flexGrow = 1f;
            empty.style.color = DataAuthoringTheme.Muted;
            _detailPane.Add(empty);
        }

        private void DuplicateSelected()
        {
            if (_selected == null || !CanDuplicate(_selected))
                return;

            TAsset duplicated = Duplicate(_selected);
            if (duplicated != null)
                RefreshAssets(duplicated);
        }

        private void DeleteSelected()
        {
            if (_selected == null || !CanDelete(_selected))
                return;

            if (Delete(_selected))
                RefreshAssets();
        }

        private void UpdateSelectionButtons()
        {
            _duplicateButton?.SetEnabled(_selected != null && CanDuplicate(_selected));
            _deleteButton?.SetEnabled(_selected != null && CanDelete(_selected));
        }

        private void RebuildDuplicateKeys()
        {
            _duplicateKeys.Clear();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (TAsset asset in _assets)
            {
                string key = KeyOf(asset);
                if (!string.IsNullOrEmpty(key) && !seen.Add(key))
                    _duplicateKeys.Add(key);
            }
        }

        private void EnsureAssetsLoaded()
        {
            if (!_assetsLoaded)
                RefreshAssets();
        }

        private static Color IssueColor(IEnumerable<DataAuthoringIssue> issues)
        {
            DataAuthoringIssueSeverity highest = issues
                .Select(issue => issue.Severity)
                .DefaultIfEmpty(DataAuthoringIssueSeverity.Info)
                .Max();
            return highest switch
            {
                DataAuthoringIssueSeverity.Error => new Color(1f, 0.38f, 0.32f),
                DataAuthoringIssueSeverity.Warning => new Color(1f, 0.7f, 0.25f),
                _ => new Color(0.45f, 0.75f, 1f)
            };
        }
    }
}
#endif
