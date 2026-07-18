using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.Tool.Editor.SOSpreadsheet
{
    /// <summary>
    /// 프로젝트의 모든 ScriptableObject 에셋을 타입별로 모아
    /// 스프레드시트(행 = 에셋, 열 = 직렬화 필드) 형태로 조회/편집하는 UIToolkit 창.
    /// - 모든 행은 1줄 고정 높이. 중첩 클래스는 셀 안이 아니라 열로 평탄화 (SOSpreadsheetModel).
    /// - 배열/리스트는 "N Items" 요약 셀로 표시하고, 클릭하면 우측 상세 패널에서 편집한다
    ///   (Game Data Workbench 스타일).
    /// - 값 편집은 PropertyField 바인딩으로 처리 (커스텀 드로어는 IMGUI 폴백 포함 그대로 존중).
    /// - 틀 고정은 MultiColumnListView가 지원하지 않으므로 TwoPaneSplitView 두 개의
    ///   리스트뷰로 나누고 세로 스크롤을 동기화한다.
    /// 메뉴: UPlayGround/SO 스프레드시트
    /// </summary>
    public class SOSpreadsheetWindow : EditorWindow
    {
        private const float RowHeight = 22f;
        private const string NameColumnId = "__asset";

        private static readonly int[] FreezeValues = { 0, 1, 2, 3, 4, 5, 6 };
        private static readonly string[] FreezeLabels = { "없음", "1열", "2열", "3열", "4열", "5열", "6열" };

        // ── 상태 ─────────────────────────────────────────────────────

        private SOSpreadsheetModel _model;
        private readonly List<RowEntry> _pageRows = new(); // 두 리스트뷰가 공유하는 현재 페이지 행
        private readonly Dictionary<string, float> _savedWidths = new(); // propertyPath → 열 너비
        private readonly HashSet<string> _hiddenColumns = new();
        private float _nameColumnWidth = 180f;
        private bool _syncingScroll;
        private bool _syncingSelection;
        /// <summary>표 갱신마다 증가. 같은 패스에서 행의 UpdateIfRequiredOrScript를 1회로 제한한다.</summary>
        private int _updatePass;
        private IVisualElementScheduledItem _searchDebounce;

        // 상세 패널이 보여주는 대상 (없으면 null)
        private RowEntry _detailRow;
        private ColumnInfo _detailColumn;
        /// <summary>마지막으로 상세 패널에 띄웠던 열 경로. 행 빈 영역 클릭 시 재사용한다.</summary>
        private string _lastDetailColumnPath;

        private AdvancedDropdownState _typeDropdownState = new();

        // 창 재시작/도메인 리로드 간 유지되는 설정
        [SerializeField] private string _scopeFolder = "Assets";
        [SerializeField] private bool _excludeExternal = true;
        [SerializeField] private string _selectedTypeName;
        [SerializeField] private string _assetSearch = string.Empty;
        [SerializeField] private bool _searchValues;
        [SerializeField] private List<ColumnFilter> _filters = new();
        [SerializeField] private bool _showChildren = true;
        [SerializeField] private int _pageSizeIndex = 1; // 기본 50
        /// <summary>가로 스크롤과 무관하게 항상 표시할 왼쪽 열 개수 (엑셀 틀 고정).</summary>
        [SerializeField] private int _freezeCount = 1;

        // UI 참조
        private ToolbarButton _typeButton;
        private ToolbarButton _filterButton;
        private VisualElement _filterBar;
        private ToolbarButton _newAssetButton;
        private ToolbarButton _columnsButton;
        private ToolbarButton _widthButton;
        private ToolbarButton _freezeButton;
        private ToolbarButton _pageSizeButton;
        private ToolbarButton _pageFirst, _pagePrev, _pageNext, _pageLast;
        private ToolbarToggle _showChildrenToggle;
        private ToolbarSearchField _searchField;
        private TextField _scopeField;
        private ToolbarToggle _excludeToggle;
        private Label _infoLabel;
        private Label _pageLabel;
        private VisualElement _tableHost;
        private MultiColumnListView _leftView;   // 틀 고정 파트 (없으면 null)
        private MultiColumnListView _rightView;  // 스크롤 파트 (단일 뷰일 때는 이것만 사용)
        private VisualElement _detailPanel;
        private Label _detailTitle;
        private VisualElement _detailBody;

        // ── 메뉴 ─────────────────────────────────────────────────────

        [MenuItem("UPlayGround/SO 스프레드시트")]
        public static void Open()
        {
            var win = GetWindow<SOSpreadsheetWindow>("SO 스프레드시트");
            win.minSize = new Vector2(900f, 400f);
            win.Show();
        }

        // ── 라이프사이클 ─────────────────────────────────────────────

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        public void CreateGUI()
        {
            _model = new SOSpreadsheetModel
            {
                scopeFolder = _scopeFolder,
                excludeExternal = _excludeExternal,
                assetSearch = _assetSearch,
                searchValues = _searchValues,
                filters = _filters, // 창이 직렬화하는 리스트를 그대로 공유
                showChildren = _showChildren,
                pageSizeIndex = _pageSizeIndex,
            };

            LoadStyleSheet();
            BuildLayout(rootVisualElement);

            // 에디터 기동 중 창 복원 시 CreateGUI가 EditorStyles 초기화 전에 불릴 수 있어
            // (boldLabel 접근에서 NRE) 최초 스캔은 에디터 루프가 준비된 뒤로 미룬다.
            EditorApplication.delayCall += () =>
            {
                if (this != null)
                    Rescan();
            };
        }

        /// <summary>
        /// 창 스크립트 파일 위치를 기준으로 같은 폴더의 USS를 로드한다.
        /// 하드코딩 경로가 없어 폴더째 다른 프로젝트로 옮겨도 동작한다.
        /// </summary>
        private void LoadStyleSheet()
        {
            var script = MonoScript.FromScriptableObject(this);
            string scriptPath = AssetDatabase.GetAssetPath(script);
            if (string.IsNullOrEmpty(scriptPath))
                return;
            string dir = System.IO.Path.GetDirectoryName(scriptPath)?.Replace('\\', '/');
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>($"{dir}/SOSpreadsheet.uss");
            if (sheet != null)
                rootVisualElement.styleSheets.Add(sheet);
        }

        /// <summary>
        /// 언두/리두 후 전 행의 프로퍼티 캐시를 비우고 표를 다시 바인딩한다.
        /// 배열은 열로 전개되지 않으므로 열 재구성은 필요 없다.
        /// 상세 패널이 가리키던 프로퍼티가 사라졌으면 패널을 닫는다.
        /// </summary>
        private void OnUndoRedo()
        {
            if (_model == null)
                return;
            foreach (var row in _model.rows)
            {
                row.serialized?.Update();
                row.props?.Clear();
            }
            RefreshTables();
            if (_detailRow != null)
                BindDetailPanel();
        }

        // ── 레이아웃 구성 ────────────────────────────────────────────

        private void BuildLayout(VisualElement root)
        {
            var toolbar = new Toolbar();
            root.Add(toolbar);

            _typeButton = new ToolbarButton(ShowTypeDropdown) { text = "타입 선택… ▾" };
            _typeButton.style.minWidth = 180f;
            toolbar.Add(_typeButton);

            toolbar.Add(new ToolbarButton(Rescan) { text = "새로고침" });

            _newAssetButton = new ToolbarButton(CreateNewAsset) { text = "+ 새 에셋" };
            toolbar.Add(_newAssetButton);

            toolbar.Add(new ToolbarButton(() => AssetDatabase.SaveAssets()) { text = "모두 저장" });

            toolbar.Add(new ToolbarSpacer());

            _columnsButton = new ToolbarButton(ShowColumnVisibilityMenu) { text = "열 표시 ▾" };
            toolbar.Add(_columnsButton);

            _widthButton = new ToolbarButton(ShowWidthPresetMenu) { text = "너비 ▾" };
            toolbar.Add(_widthButton);

            toolbar.Add(new ToolbarSpacer());

            // 중첩 클래스 평탄화 토글 (배열/리스트는 항상 "N Items" 요약 → 상세 패널에서 편집)
            _showChildrenToggle = new ToolbarToggle { text = "자식 필드", value = _showChildren };
            _showChildrenToggle.RegisterValueChangedCallback(evt =>
            {
                _showChildren = _model.showChildren = evt.newValue;
                RebuildColumnsFresh();
            });
            toolbar.Add(_showChildrenToggle);

            toolbar.Add(new ToolbarSpacer());

            _freezeButton = new ToolbarButton(ShowFreezeMenu);
            toolbar.Add(_freezeButton);

            var toolbarSpacer = new VisualElement();
            toolbarSpacer.AddToClassList("so-toolbar-spacer");
            toolbar.Add(toolbarSpacer);

            _searchField = new ToolbarSearchField();
            _searchField.style.width = 220f;
            _searchField.SetValueWithoutNotify(_assetSearch);
            _searchField.RegisterValueChangedCallback(evt =>
            {
                // 타이핑마다 전체 표를 다시 바인딩하지 않도록 짧게 디바운스
                _assetSearch = _model.assetSearch = evt.newValue;
                _searchDebounce ??= rootVisualElement.schedule.Execute(ApplyFiltersAndRefresh);
                _searchDebounce.ExecuteLater(250);
            });
            toolbar.Add(_searchField);

            var valueSearchToggle = new ToolbarToggle
            {
                text = "값 검색",
                value = _searchValues,
                tooltip = "켜면 에셋 이름뿐 아니라 셀 값(숫자/문자열/enum/참조 이름)에서도 검색합니다.",
            };
            valueSearchToggle.RegisterValueChangedCallback(evt =>
            {
                _searchValues = _model.searchValues = evt.newValue;
                if (!string.IsNullOrEmpty(_assetSearch))
                    ApplyFiltersAndRefresh();
            });
            toolbar.Add(valueSearchToggle);

            _filterButton = new ToolbarButton(ShowAddFilterMenu) { text = "필터 ▾" };
            toolbar.Add(_filterButton);

            // 활성 필터 칩 바 (필터가 없으면 숨김)
            _filterBar = new VisualElement();
            _filterBar.AddToClassList("so-filter-bar");
            root.Add(_filterBar);

            // 가운데 영역: 테이블 + (필요 시) 우측 상세 패널
            var content = new VisualElement();
            content.AddToClassList("so-content-row");
            root.Add(content);

            _tableHost = new VisualElement();
            _tableHost.AddToClassList("so-table-host");
            content.Add(_tableHost);

            content.Add(BuildDetailPanel());

            BuildBottomBar(root);
        }

        // ── 상세 패널 (리스트/중첩 데이터 편집) ──────────────────────

        private VisualElement BuildDetailPanel()
        {
            _detailPanel = new VisualElement();
            _detailPanel.AddToClassList("so-detail-panel");
            _detailPanel.style.display = DisplayStyle.None;

            var header = new VisualElement();
            header.AddToClassList("so-detail-header");
            _detailPanel.Add(header);

            _detailTitle = new Label();
            _detailTitle.AddToClassList("so-detail-title");
            header.Add(_detailTitle);

            var close = new Button(CloseDetailPanel) { text = "✕", tooltip = "패널 닫기" };
            close.AddToClassList("so-detail-close");
            header.Add(close);

            _detailBody = new VisualElement();
            _detailBody.AddToClassList("so-detail-body");
            _detailPanel.Add(_detailBody);
            return _detailPanel;
        }

        private void OpenDetailPanel(RowEntry row, ColumnInfo info)
        {
            if (_detailRow == row && _detailColumn == info)
                return; // 이미 같은 대상을 표시 중이면 재바인딩 생략
            _detailRow = row;
            _detailColumn = info;
            _lastDetailColumnPath = info.propertyPath;
            BindDetailPanel();
        }

        /// <summary>같은 (행, 열)을 다시 클릭하면 패널을 닫고, 아니면 그 대상으로 연다.</summary>
        private void ToggleDetailPanel(RowEntry row, ColumnInfo info)
        {
            if (_detailRow == row && _detailColumn == info)
                CloseDetailPanel();
            else
                OpenDetailPanel(row, info);
        }

        /// <summary>현재 대상(_detailRow/_detailColumn)을 패널에 다시 바인딩한다. 대상이 사라졌으면 닫는다.</summary>
        private void BindDetailPanel()
        {
            var so = _detailRow?.GetSerialized();
            var prop = _detailColumn != null ? so?.FindProperty(_detailColumn.propertyPath) : null;
            if (prop == null)
            {
                CloseDetailPanel();
                return;
            }
            so.UpdateIfRequiredOrScript();

            _detailTitle.text = $"{_detailRow.DisplayName} · {_detailColumn.displayName}";

            _detailBody.Unbind();
            _detailBody.Clear();

            ExpandRecursive(prop, 0);
            var pf = new PropertyField { label = _detailColumn.displayName };
            pf.BindProperty(prop);

            if (_detailColumn.isList)
            {
                // 리스트는 ListView 자체 스크롤을 쓰므로 바깥 ScrollView 없이 패널 높이를 꽉 채운다
                pf.AddToClassList("so-detail-list");
                _detailBody.Add(pf);
                // ListView는 바인딩 후 비동기로 생성되므로, 나타날 때까지 기다렸다가 늘린다
                pf.schedule.Execute(() => StretchDetailListView(pf))
                    .Until(() => pf.Q<ListView>() != null);
            }
            else
            {
                // 중첩 클래스는 자체 스크롤이 없으므로 ScrollView로 감싼다
                var scroll = new ScrollView();
                scroll.AddToClassList("so-detail-scroll");
                scroll.Add(pf);
                _detailBody.Add(scroll);
            }

            // 패널에서 요소를 추가/삭제하면 표의 "N Items" 요약을 갱신
            if (_detailColumn.isList)
            {
                var sizeProp = so.FindProperty(_detailColumn.propertyPath + ".Array.size");
                if (sizeProp != null)
                    pf.TrackPropertyValue(sizeProp, _ => RefreshTables());
            }

            _detailPanel.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// 상세 패널의 리스트가 자기 콘텐츠 높이만 차지하고 아래가 비지 않도록,
        /// PropertyField 안의 ListView와 그 사이 중간 컨테이너들을 패널 높이까지 늘린다.
        /// (PropertyField 내부 계층은 Unity 버전에 따라 다를 수 있어 USS 대신 코드로 처리)
        /// </summary>
        private static void StretchDetailListView(PropertyField pf)
        {
            var listView = pf.Q<ListView>();
            if (listView == null)
                return;
            listView.style.flexGrow = 1f;
            listView.style.maxHeight = new StyleLength(StyleKeyword.None);
            for (var parent = listView.parent; parent != null && parent != pf; parent = parent.parent)
                parent.style.flexGrow = 1f;
        }

        /// <summary>
        /// 패널이 접힌 상태로 열리지 않도록 대상 프로퍼티와 그 하위(배열 요소, 중첩 클래스)의
        /// 폴드아웃을 미리 펼쳐 둔다. 거대한 리스트에서 과도한 UI 생성을 막기 위해
        /// 깊이와 요소 수에 상한을 둔다.
        /// </summary>
        private static void ExpandRecursive(SerializedProperty prop, int depth)
        {
            const int maxDepth = 4;
            const int maxElements = 64;

            if (depth > maxDepth)
                return;
            prop.isExpanded = true;

            if (prop.isArray)
            {
                if (prop.propertyType == SerializedPropertyType.String)
                    return;
                int count = Mathf.Min(prop.arraySize, maxElements);
                for (int i = 0; i < count; i++)
                {
                    var element = prop.GetArrayElementAtIndex(i);
                    if (element.propertyType == SerializedPropertyType.Generic)
                        ExpandRecursive(element, depth + 1);
                }
                return;
            }

            var it = prop.Copy();
            var end = it.GetEndProperty();
            if (!it.NextVisible(true))
                return;
            while (!SerializedProperty.EqualContents(it, end))
            {
                if (it.propertyType == SerializedPropertyType.Generic)
                    ExpandRecursive(it.Copy(), depth + 1);
                if (!it.NextVisible(false))
                    break;
            }
        }

        private void CloseDetailPanel()
        {
            _detailRow = null;
            _detailColumn = null;
            _detailBody.Unbind();
            _detailBody.Clear();
            _detailPanel.style.display = DisplayStyle.None;
        }

        private void BuildBottomBar(VisualElement root)
        {
            var bar = new Toolbar();
            root.Add(bar);

            bar.Add(new Label("범위") { style = { unityTextAlign = TextAnchor.MiddleLeft, marginLeft = 4f } });

            _scopeField = new TextField { isDelayed = true };
            _scopeField.AddToClassList("so-scope-field");
            _scopeField.SetValueWithoutNotify(_scopeFolder);
            _scopeField.RegisterValueChangedCallback(evt =>
            {
                _scopeFolder = _model.scopeFolder = evt.newValue;
                Rescan();
            });
            bar.Add(_scopeField);

            _excludeToggle = new ToolbarToggle { text = "외부 제외", value = _excludeExternal };
            _excludeToggle.RegisterValueChangedCallback(evt =>
            {
                _excludeExternal = _model.excludeExternal = evt.newValue;
                Rescan();
            });
            bar.Add(_excludeToggle);

            _infoLabel = new Label();
            _infoLabel.AddToClassList("so-info-label");
            bar.Add(_infoLabel);

            var spacer = new VisualElement();
            spacer.AddToClassList("so-toolbar-spacer");
            bar.Add(spacer);

            bar.Add(new Label("페이지 크기") { style = { unityTextAlign = TextAnchor.MiddleLeft } });
            _pageSizeButton = new ToolbarButton(ShowPageSizeMenu);
            bar.Add(_pageSizeButton);

            _pageFirst = new ToolbarButton(() => SetPage(0)) { text = "|◀" };
            _pagePrev = new ToolbarButton(() => SetPage(_model.pageIndex - 1)) { text = "◀" };
            bar.Add(_pageFirst);
            bar.Add(_pagePrev);

            _pageLabel = new Label();
            _pageLabel.AddToClassList("so-page-label");
            bar.Add(_pageLabel);

            _pageNext = new ToolbarButton(() => SetPage(_model.pageIndex + 1)) { text = "▶" };
            _pageLast = new ToolbarButton(() => SetPage(_model.PageCount - 1)) { text = "▶|" };
            bar.Add(_pageNext);
            bar.Add(_pageLast);
        }

        // ── 스캔 / 선택 ──────────────────────────────────────────────

        /// <summary>프로젝트를 다시 스캔하고 기존 타입 선택을 복원한다.</summary>
        private void Rescan()
        {
            _model.ScanProject();

            TypeEntry entry = null;
            if (!string.IsNullOrEmpty(_selectedTypeName))
                entry = _model.types.FirstOrDefault(t => t.type.AssemblyQualifiedName == _selectedTypeName);
            OnTypeSelected(entry);
        }

        private void OnTypeSelected(TypeEntry entry)
        {
            // 도메인 리로드 후 같은 타입 복원 시에는 필터를 유지하고, 타입이 바뀔 때만 초기화
            bool typeChanged = _selectedTypeName != entry?.type.AssemblyQualifiedName;
            if (typeChanged)
                _filters.Clear();

            CloseDetailPanel();
            _model.SelectType(entry);
            _selectedTypeName = entry?.type.AssemblyQualifiedName;
            _savedWidths.Clear();
            _hiddenColumns.Clear();
            _nameColumnWidth = 180f;

            if (entry != null)
            {
                _model.BuildColumns();
                AutoFitWidths();
                _model.ApplyFilter();
            }

            RebuildFilterBar();
            RefreshPageRows();
            RebuildTable();
            UpdateChrome();
        }

        /// <summary>자식 필드 토글처럼 열 구성이 통째로 바뀔 때: 너비를 새로 맞춘다.</summary>
        private void RebuildColumnsFresh()
        {
            if (_model.selected == null)
                return;
            _savedWidths.Clear();
            _model.BuildColumns();
            AutoFitWidths();
            _model.ApplyFilter();
            RebuildFilterBar(); // 열 구성이 바뀌면 필터 칩의 열 참조도 다시 해석
            RefreshPageRows();
            RebuildTable();
            UpdateChrome();
        }

        // ── 테이블 구성 ──────────────────────────────────────────────

        /// <summary>표시할 열의 모델 인덱스 목록 (0 = 에셋 이름, n = 데이터 열 n-1).</summary>
        private List<int> GetVisibleColumnIndices()
        {
            var result = new List<int> { 0 }; // 이름 열은 항상 표시
            for (int i = 0; i < _model.columns.Count; i++)
            {
                if (!_hiddenColumns.Contains(_model.columns[i].propertyPath))
                    result.Add(i + 1);
            }
            return result;
        }

        private void RebuildTable()
        {
            _updatePass++;

            // 재구성 전 사용자 조정 열 너비 보존
            HarvestColumnWidths();

            _tableHost.Clear();
            _leftView = null;
            _rightView = null;

            if (_model.selected == null)
            {
                AddPlaceholder("툴바의 타입 드롭다운에서 ScriptableObject 타입을 선택하세요.");
                return;
            }
            if (_model.columns.Count == 0)
            {
                AddPlaceholder("에셋을 로드하지 못했습니다. 새로고침 후 다시 시도하세요.");
                return;
            }

            var visible = GetVisibleColumnIndices();
            int freeze = Mathf.Clamp(_freezeCount, 0, visible.Count - 1);

            if (freeze <= 0)
            {
                _rightView = CreateListView(visible);
                _tableHost.Add(_rightView);
                return;
            }

            var frozenIndices = visible.Take(freeze).ToList();
            var scrollIndices = visible.Skip(freeze).ToList();

            _leftView = CreateListView(frozenIndices);
            _rightView = CreateListView(scrollIndices);

            // 고정 파트는 자체 가로 스크롤바를 숨긴다 (폭 조정은 스플리터로)
            var leftScroll = _leftView.Q<ScrollView>();
            if (leftScroll != null)
                leftScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            float frozenWidth = 8f;
            foreach (int mi in frozenIndices)
                frozenWidth += mi == 0 ? _nameColumnWidth : SavedWidth(_model.columns[mi - 1]);
            float initialDim = Mathf.Clamp(frozenWidth, 60f, Mathf.Max(120f, position.width * 0.6f));

            var split = new TwoPaneSplitView(0, initialDim, TwoPaneSplitViewOrientation.Horizontal);
            split.style.flexGrow = 1f;
            split.Add(_leftView);
            split.Add(_rightView);
            _tableHost.Add(split);

            // 두 리스트뷰의 세로 스크롤 동기화 (레이아웃이 잡힌 뒤 훅)
            _tableHost.schedule.Execute(SetupScrollSync);

            // 행 선택 하이라이트를 고정/스크롤 파트 간 동기화
            _leftView.selectedIndicesChanged += indices => SyncSelection(_rightView, indices);
            _rightView.selectedIndicesChanged += indices => SyncSelection(_leftView, indices);
        }

        private void SyncSelection(MultiColumnListView target, IEnumerable<int> indices)
        {
            if (_syncingSelection || target == null)
                return;
            _syncingSelection = true;
            target.SetSelectionWithoutNotify(indices.ToList());
            _syncingSelection = false;
        }

        private void AddPlaceholder(string message)
        {
            var label = new Label(message);
            label.AddToClassList("so-placeholder");
            _tableHost.Add(label);
        }

        private MultiColumnListView CreateListView(List<int> modelIndices)
        {
            var view = new MultiColumnListView
            {
                fixedItemHeight = RowHeight,
                itemsSource = _pageRows,
                selectionType = SelectionType.Single, // 행 클릭 하이라이트 (넓은 표에서 행 추적용)
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                sortingMode = ColumnSortingMode.Custom,
            };
            view.style.flexGrow = 1f;

            foreach (int mi in modelIndices)
                view.columns.Add(CreateColumn(mi));

            view.columnSortingChanged += () => OnSortChanged(view);
            // 셀을 정확히 누르지 않아도 행 아무 곳(빈 영역 포함) 클릭으로 상세 패널을 열고 닫는다
            view.RegisterCallback<ClickEvent>(evt => OnTableClick(view, evt));
            return view;
        }

        /// <summary>
        /// 행의 빈 영역 클릭을 상세 패널 열기/닫기로 연결한다.
        /// 편집 컨트롤(필드/버튼/스크롤바) 위의 클릭은 본래 동작을 유지하도록 무시한다.
        /// 같은 행을 다시 클릭하면 패널을 닫는다.
        /// </summary>
        private void OnTableClick(MultiColumnListView view, ClickEvent evt)
        {
            if (evt.target is not VisualElement target || IsInteractiveElement(target, view))
                return;

            int index = RowIndexAtPosition(view, evt.position);
            if (index < 0 || index >= _pageRows.Count)
                return;

            var row = _pageRows[index];
            if (_detailRow == row)
            {
                CloseDetailPanel();
                return;
            }

            var column = ResolveDetailColumn();
            if (column != null)
                OpenDetailPanel(row, column);
        }

        /// <summary>클릭 대상이 편집/조작 컨트롤(또는 그 내부)인지. stopAt(리스트뷰)까지 부모를 거슬러 검사.</summary>
        private static bool IsInteractiveElement(VisualElement element, VisualElement stopAt)
        {
            for (var v = element; v != null && v != stopAt; v = v.parent)
            {
                if (v is Button || v is Scroller || v is IBindable)
                    return true;
            }
            return false;
        }

        /// <summary>고정 높이 행 가정으로 클릭 위치의 행 인덱스를 계산한다 (헤더/범위 밖은 -1).</summary>
        private int RowIndexAtPosition(MultiColumnListView view, Vector2 worldPosition)
        {
            var scroll = view.Q<ScrollView>();
            if (scroll == null)
                return -1;
            Vector2 local = scroll.contentContainer.WorldToLocal(worldPosition);
            if (local.y < 0f)
                return -1;
            return (int)(local.y / RowHeight);
        }

        /// <summary>
        /// 행 빈 영역 클릭으로 패널을 열 때 사용할 열:
        /// 현재 열 → 마지막으로 열었던 열 → 첫 번째 표시 중인 요약 열 순으로 고른다.
        /// </summary>
        private ColumnInfo ResolveDetailColumn()
        {
            if (_detailColumn != null)
                return _detailColumn;

            if (!string.IsNullOrEmpty(_lastDetailColumnPath))
            {
                var last = _model.FindColumn(_lastDetailColumnPath);
                if (last != null)
                    return last;
            }

            foreach (var c in _model.columns)
            {
                if (c.summaryOnly && !_hiddenColumns.Contains(c.propertyPath))
                    return c;
            }
            return null;
        }

        private Column CreateColumn(int modelIndex)
        {
            if (modelIndex == 0)
            {
                return new Column
                {
                    name = NameColumnId,
                    title = "에셋",
                    width = _nameColumnWidth,
                    minWidth = 100f,
                    resizable = true,
                    sortable = true,
                    stretchable = false,
                    optional = false, // 열 표시/숨김은 자체 메뉴로만 관리
                    makeCell = MakeNameCell,
                    bindCell = BindNameCell,
                    unbindCell = (ve, _) => ve.Unbind(),
                };
            }

            var info = _model.columns[modelIndex - 1];
            return new Column
            {
                name = info.propertyPath,
                title = info.displayName,
                width = SavedWidth(info),
                minWidth = 40f,
                resizable = true,
                sortable = SOSpreadsheetModel.IsSortable(info.propType),
                stretchable = false,
                optional = false,
                makeCell = MakeDataCell,
                bindCell = (ve, rowIndex) => BindDataCell(ve, rowIndex, info),
                unbindCell = (ve, _) => ve.Unbind(),
            };
        }

        private void SetupScrollSync()
        {
            var left = _leftView?.Q<ScrollView>();
            var right = _rightView?.Q<ScrollView>();
            if (left == null || right == null)
                return;

            left.verticalScroller.valueChanged += v =>
            {
                if (_syncingScroll) return;
                _syncingScroll = true;
                right.verticalScroller.value = v;
                _syncingScroll = false;
            };
            right.verticalScroller.valueChanged += v =>
            {
                if (_syncingScroll) return;
                _syncingScroll = true;
                left.verticalScroller.value = v;
                _syncingScroll = false;
            };
        }

        // ── 셀: 에셋 이름 ────────────────────────────────────────────

        private VisualElement MakeNameCell()
        {
            var cell = new VisualElement();
            cell.AddToClassList("so-cell");

            var ping = new Button { text = "◎", tooltip = "프로젝트 창에서 선택" };
            ping.AddToClassList("so-ping-button");
            ping.clicked += () =>
            {
                if (cell.userData is RowEntry row && row.asset != null)
                {
                    EditorGUIUtility.PingObject(row.asset);
                    Selection.activeObject = row.asset;
                }
            };
            cell.Add(ping);

            var field = new TextField { isDelayed = true };
            field.AddToClassList("so-name-field");
            field.RegisterValueChangedCallback(evt =>
            {
                if (cell.userData is RowEntry row)
                    RenameRow(row, evt.newValue, field);
            });
            cell.Add(field);
            return cell;
        }

        private void BindNameCell(VisualElement cell, int rowIndex)
        {
            var row = _pageRows[rowIndex];
            cell.userData = row;
            var field = cell.Q<TextField>();
            var so = row.GetSerialized();
            field.SetEnabled(so != null);
            field.SetValueWithoutNotify(so != null ? row.DisplayName : $"(로드 실패) {row.path}");
        }

        private void RenameRow(RowEntry row, string newName, TextField field)
        {
            if (string.IsNullOrWhiteSpace(newName) || newName == row.DisplayName)
            {
                field.SetValueWithoutNotify(row.DisplayName);
                return;
            }
            string error = AssetDatabase.RenameAsset(row.path, newName);
            if (string.IsNullOrEmpty(error))
            {
                row.path = AssetDatabase.GetAssetPath(row.asset);
            }
            else
            {
                Debug.LogWarning($"이름 변경 실패: {error}");
                field.SetValueWithoutNotify(row.DisplayName);
            }
        }

        // ── 셀: 데이터 ───────────────────────────────────────────────

        private static VisualElement MakeDataCell()
        {
            var cell = new VisualElement();
            cell.AddToClassList("so-cell");
            return cell;
        }

        /// <summary>
        /// 셀 내용은 (결측/크기/요약/프로퍼티) 종류가 같으면 컨트롤을 재사용하고,
        /// 달라졌을 때만 다시 만든다. 종류는 cell.userData에 문자열로 기록한다.
        /// </summary>
        private static T EnsureContent<T>(VisualElement cell, string kind, Func<T> create) where T : VisualElement
        {
            if (Equals(cell.userData, kind) && cell.childCount == 1 && cell[0] is T existing)
                return existing;
            cell.Clear();
            var made = create();
            cell.Add(made);
            cell.userData = kind;
            return made;
        }

        private void BindDataCell(VisualElement cell, int rowIndex, ColumnInfo info)
        {
            cell.Unbind();

            var row = _pageRows[rowIndex];
            var so = row.GetSerialized();
            var prop = so != null ? row.GetProperty(info.propertyPath) : null;
            if (prop == null)
            {
                var missingLabel = EnsureContent(cell, "missing", MakeMissingLabel);
                missingLabel.text = "—";
                return;
            }

            // 같은 갱신 패스에서 행당 1회만 동기화 (열 수만큼 반복 호출 방지, 이후는 바인딩이 추적)
            if (row.lastUpdatePass != _updatePass)
            {
                so.UpdateIfRequiredOrScript();
                row.lastUpdatePass = _updatePass;
            }

            if (info.summaryOnly)
            {
                // 리스트/중첩 데이터는 요약만 표시하고, 클릭하면 우측 상세 패널에서 편집
                var summary = EnsureContent(cell, "summary", () =>
                {
                    var b = new Button { tooltip = "클릭하면 우측 패널에서 편집, 다시 클릭하면 닫습니다" };
                    b.AddToClassList("so-summary-button");
                    b.clicked += () =>
                    {
                        if (b.userData is RowEntry r)
                            ToggleDetailPanel(r, info);
                    };
                    return b;
                });
                summary.userData = row;
                summary.text = info.isList ? FormatItemCount(prop.arraySize) : "{…}";
                return;
            }

            // 커스텀 드로어가 없는 단순 타입은 PropertyField(내부 드로어 해석·재생성 비용)보다
            // 훨씬 가벼운 타입 전용 필드로 그린다 → 페이지 전환/스크롤 체감 성능의 핵심
            if (!info.hasCustomDrawer)
            {
                var typed = EnsureTypedField(cell, info);
                if (typed is IBindable bindable)
                {
                    bindable.BindProperty(prop);
                    return;
                }
            }

            var pf = EnsureContent(cell, "prop", () => new PropertyField { label = string.Empty });
            pf.BindProperty(prop);
            // PropertyField 내부 구성은 바인딩 후에 만들어지므로 다음 틱에 드로어 보정
            if (info.hasCustomDrawer || info.topCut > 0f)
                pf.schedule.Execute(() => FixupPropertyField(pf, info));
        }

        /// <summary>
        /// 셀에 열 타입 전용 편집 필드를 만들어 재사용한다 (셀은 열에 귀속되므로 타입이 바뀌지 않는다).
        /// 전용 필드가 없는 타입이면 null → 호출측이 PropertyField로 폴백.
        /// </summary>
        private static VisualElement EnsureTypedField(VisualElement cell, ColumnInfo info)
        {
            if (Equals(cell.userData, "typed") && cell.childCount == 1)
                return cell[0];

            var made = CreateTypedField(info);
            if (made == null)
                return null;

            cell.Clear();
            cell.Add(made);
            cell.userData = "typed";
            return made;
        }

        private static VisualElement CreateTypedField(ColumnInfo info)
        {
            switch (info.propType)
            {
                case SerializedPropertyType.Integer:
                    return new IntegerField();
                case SerializedPropertyType.Boolean:
                    return new Toggle();
                case SerializedPropertyType.Float:
                    return new FloatField();
                case SerializedPropertyType.String:
                    return new TextField();
                case SerializedPropertyType.Color:
                    return new ColorField();
                case SerializedPropertyType.ObjectReference:
                    return new ObjectField
                    {
                        objectType = info.objectType ?? typeof(UnityEngine.Object),
                        allowSceneObjects = false,
                    };
                case SerializedPropertyType.LayerMask:
                    return new LayerMaskField();
                case SerializedPropertyType.Enum:
                {
                    if (info.enumType == null)
                        return null; // 타입을 못 찾으면 PropertyField 폴백
                    var defaultValue = (Enum)Enum.ToObject(info.enumType, 0);
                    if (info.isFlagsEnum)
                        return new EnumFlagsField(defaultValue);
                    return new EnumField(defaultValue);
                }
                case SerializedPropertyType.Vector2:
                    return new Vector2Field();
                case SerializedPropertyType.Vector3:
                    return new Vector3Field();
                case SerializedPropertyType.Vector4:
                    return new Vector4Field();
                case SerializedPropertyType.Vector2Int:
                    return new Vector2IntField();
                case SerializedPropertyType.Vector3Int:
                    return new Vector3IntField();
                case SerializedPropertyType.AnimationCurve:
                    return new CurveField();
                case SerializedPropertyType.Gradient:
                    return new GradientField();
                default:
                    return null;
            }
        }

        private static string FormatItemCount(int count)
        {
            return count == 1 ? "1 Item" : $"{count} Items";
        }

        private static Label MakeMissingLabel()
        {
            var l = new Label();
            l.AddToClassList("so-missing");
            return l;
        }

        /// <summary>
        /// 커스텀 드로어가 IMGUI 폴백(IMGUIContainer)으로 그려지면 [Header]/[Space]/
        /// [TextArea] 라벨 줄이 셀 위쪽을 차지한다. 열 구성 시 계산해 둔 topCut만큼
        /// 위로 밀어 올리고 셀 클리핑(overflow hidden)으로 1줄만 남긴다.
        /// UIToolkit 네이티브 드로어의 데코레이터는 USS로 숨긴다.
        /// </summary>
        private static void FixupPropertyField(PropertyField pf, ColumnInfo info)
        {
            if (info.topCut <= 0f)
                return;
            var imgui = pf.Q<IMGUIContainer>();
            if (imgui != null)
                imgui.style.marginTop = -info.topCut;
        }

        // ── 정렬 ─────────────────────────────────────────────────────

        private void OnSortChanged(MultiColumnListView view)
        {
            int index = -1;
            bool ascending = true;
            foreach (var desc in view.sortColumnDescriptions)
            {
                index = ColumnIndexById(desc.columnName);
                ascending = desc.direction == SortDirection.Ascending;
                break; // 첫 정렬 열만 사용
            }

            _model.sortColumnIndex = index;
            _model.sortAscending = ascending;
            _model.ApplyFilter();
            RefreshData();
        }

        private int ColumnIndexById(string columnName)
        {
            if (columnName == NameColumnId)
                return 0;
            for (int i = 0; i < _model.columns.Count; i++)
            {
                if (_model.columns[i].propertyPath == columnName)
                    return i + 1;
            }
            return -1;
        }

        // ── 페이지 / 갱신 ────────────────────────────────────────────

        private void SetPage(int page)
        {
            _model.pageIndex = Mathf.Clamp(page, 0, _model.PageCount - 1);
            RefreshData();
            ScrollToTop();
        }

        private void ScrollToTop()
        {
            var left = _leftView?.Q<ScrollView>();
            var right = _rightView?.Q<ScrollView>();
            if (left != null) left.scrollOffset = Vector2.zero;
            if (right != null) right.scrollOffset = Vector2.zero;
        }

        private void RefreshPageRows()
        {
            _pageRows.Clear();
            int start = _model.PageStart;
            int count = _model.PageRowCount;
            for (int i = 0; i < count; i++)
                _pageRows.Add(_model.view[start + i]);
        }

        /// <summary>필터/정렬/페이지 결과를 리스트뷰에 반영한다 (열 구조는 그대로).</summary>
        private void RefreshData()
        {
            RefreshPageRows();
            RefreshTables();
            UpdateChrome();
        }

        private void RefreshTables()
        {
            _updatePass++;
            _leftView?.RefreshItems();
            _rightView?.RefreshItems();
        }

        private void UpdateChrome()
        {
            var selected = _model.selected;
            _typeButton.text = selected != null
                ? $"{selected.type.Name} ({selected.assetPaths.Count}) ▾"
                : "타입 선택… ▾";
            _newAssetButton.SetEnabled(selected != null);
            _columnsButton.SetEnabled(selected != null && _model.columns.Count > 0);
            _widthButton.SetEnabled(selected != null && _model.columns.Count > 0);
            _freezeButton.text = $"고정: {FreezeLabels[Mathf.Clamp(_freezeCount, 0, FreezeLabels.Length - 1)]} ▾";
            _pageSizeButton.text =
                $"{SOSpreadsheetModel.PageSizeLabels[Mathf.Clamp(_pageSizeIndex, 0, SOSpreadsheetModel.PageSizeLabels.Length - 1)]} ▾";

            if (selected != null)
            {
                string info = $"{_model.view.Count}/{_model.rows.Count}행 · {_model.columns.Count}열";
                int activeFilters = _filters.Count(f => f.IsActive);
                if (activeFilters > 0)
                    info += $" · 필터 {activeFilters}개";
                if (_model.columnsTruncated)
                    info += " (열 일부 생략)";
                _infoLabel.text = info;
            }
            else
            {
                _infoLabel.text = string.Empty;
            }

            int pageCount = _model.PageCount;
            _pageLabel.text = $"{_model.pageIndex + 1} / {pageCount}";
            _pageFirst.SetEnabled(_model.pageIndex > 0);
            _pagePrev.SetEnabled(_model.pageIndex > 0);
            _pageNext.SetEnabled(_model.pageIndex < pageCount - 1);
            _pageLast.SetEnabled(_model.pageIndex < pageCount - 1);
        }

        // ── 툴바 메뉴 ────────────────────────────────────────────────

        private void ShowTypeDropdown()
        {
            new TypeDropdown(_typeDropdownState, this).Show(_typeButton.worldBound);
        }

        private void ShowColumnVisibilityMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("모두 표시"), false, () =>
            {
                _hiddenColumns.Clear();
                RebuildTable();
            });
            menu.AddSeparator(string.Empty);

            foreach (var col in _model.columns)
            {
                string path = col.propertyPath;
                // GenericMenu는 '/'를 서브메뉴로 해석하므로 이름의 구분자를 치환
                string label = col.displayName.Replace("/", "∕");
                bool visible = !_hiddenColumns.Contains(path);
                menu.AddItem(new GUIContent(label), visible, () =>
                {
                    if (!_hiddenColumns.Add(path))
                        _hiddenColumns.Remove(path);
                    RebuildTable();
                });
            }
            menu.DropDown(_columnsButton.worldBound);
        }

        private void ShowWidthPresetMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("내용에 맞춤"), false, () =>
            {
                AutoFitWidths();
                RebuildTable();
            });
            menu.AddItem(new GUIContent("압축 (최소 너비)"), false, () =>
            {
                _nameColumnWidth = 100f;
                foreach (var col in _model.columns)
                    _savedWidths[col.propertyPath] = 40f;
                RebuildTable();
            });
            menu.AddItem(new GUIContent("창에 채움"), false, () =>
            {
                StretchColumnsToWindow();
                RebuildTable();
            });
            menu.DropDown(_widthButton.worldBound);
        }

        private void ShowFreezeMenu()
        {
            var menu = new GenericMenu();
            for (int i = 0; i < FreezeValues.Length; i++)
            {
                int value = FreezeValues[i];
                menu.AddItem(new GUIContent(FreezeLabels[i]), _freezeCount == value, () =>
                {
                    _freezeCount = value;
                    RebuildTable();
                    UpdateChrome();
                });
            }
            menu.DropDown(_freezeButton.worldBound);
        }

        private void ShowPageSizeMenu()
        {
            var menu = new GenericMenu();
            for (int i = 0; i < SOSpreadsheetModel.PageSizeLabels.Length; i++)
            {
                int index = i;
                menu.AddItem(new GUIContent(SOSpreadsheetModel.PageSizeLabels[i]), _pageSizeIndex == i, () =>
                {
                    _pageSizeIndex = _model.pageSizeIndex = index;
                    _model.pageIndex = 0;
                    _model.ApplyFilter();
                    RefreshData();
                    ScrollToTop();
                });
            }
            menu.DropDown(_pageSizeButton.worldBound);
        }

        // ── 열 필터 ──────────────────────────────────────────────────

        private void ApplyFiltersAndRefresh()
        {
            _model.ApplyFilter();
            RefreshData();
        }

        /// <summary>필터를 지원하는 열인지 (값을 1차원 조건으로 비교할 수 있는 타입 + 리스트 요소 수).</summary>
        private static bool IsFilterableColumn(ColumnInfo col)
        {
            switch (col.propType)
            {
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Float:
                case SerializedPropertyType.Boolean:
                case SerializedPropertyType.String:
                case SerializedPropertyType.Enum:
                case SerializedPropertyType.ObjectReference:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.Character:
                    return true;
                default:
                    return col.isList;
            }
        }

        private void ShowAddFilterMenu()
        {
            var menu = new GenericMenu();
            if (_model.columns.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("타입을 먼저 선택하세요"));
            }
            else
            {
                foreach (var col in _model.columns)
                {
                    if (!IsFilterableColumn(col))
                        continue;
                    string path = col.propertyPath;
                    string label = col.displayName.Replace("/", "∕");
                    if (_filters.Any(f => f.propertyPath == path))
                    {
                        menu.AddDisabledItem(new GUIContent(label), true);
                        continue;
                    }
                    menu.AddItem(new GUIContent(label), false, () =>
                    {
                        _filters.Add(new ColumnFilter { propertyPath = path });
                        RebuildFilterBar();
                    });
                }
            }

            if (_filters.Count > 0)
            {
                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("필터 모두 제거"), false, () =>
                {
                    _filters.Clear();
                    RebuildFilterBar();
                    ApplyFiltersAndRefresh();
                });
            }
            menu.DropDown(_filterButton.worldBound);
        }

        private void RebuildFilterBar()
        {
            if (_filterBar == null)
                return;
            _filterBar.Clear();
            _filterBar.style.display = _filters.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            foreach (var filter in _filters)
                _filterBar.Add(MakeFilterChip(filter));
        }

        private VisualElement MakeFilterChip(ColumnFilter filter)
        {
            var chip = new VisualElement();
            chip.AddToClassList("so-filter-chip");

            var col = _model.FindColumn(filter.propertyPath);

            var nameLabel = new Label(col?.displayName ?? $"(없는 열) {filter.propertyPath}");
            nameLabel.AddToClassList("so-filter-name");
            chip.Add(nameLabel);

            if (col != null)
            {
                if (col.propType == SerializedPropertyType.Enum ||
                    col.propType == SerializedPropertyType.Boolean)
                {
                    var valueButton = new Button { text = AllowedSummary(filter, col) };
                    valueButton.AddToClassList("so-filter-value-button");
                    valueButton.clicked += () => ShowAllowedValuesMenu(filter, col, valueButton);
                    chip.Add(valueButton);
                }
                else
                {
                    bool numeric = col.propType != SerializedPropertyType.String &&
                                   col.propType != SerializedPropertyType.ObjectReference;
                    var textField = new TextField { value = filter.text, isDelayed = true };
                    textField.AddToClassList("so-filter-text");
                    textField.tooltip = numeric
                        ? "비교식: 10 / >=10 / <5 / 3..8" + (col.isList ? " (요소 수 기준)" : string.Empty)
                        : "포함 텍스트 (대소문자 무시)";
                    textField.RegisterValueChangedCallback(evt =>
                    {
                        filter.text = evt.newValue;
                        ApplyFiltersAndRefresh();
                    });
                    chip.Add(textField);
                }
            }

            var remove = new Button(() =>
            {
                _filters.Remove(filter);
                RebuildFilterBar();
                ApplyFiltersAndRefresh();
            }) { text = "✕", tooltip = "필터 제거" };
            remove.AddToClassList("so-filter-remove");
            chip.Add(remove);
            return chip;
        }

        private static string AllowedSummary(ColumnFilter filter, ColumnInfo col)
        {
            if (filter.allowed.Count == 0)
                return "전체 ▾";
            if (filter.allowed.Count == 1)
            {
                string raw = filter.allowed[0];
                if (col.enumNames != null)
                {
                    int idx = Array.IndexOf(col.enumNames, raw);
                    if (idx >= 0 && col.enumDisplayNames != null && idx < col.enumDisplayNames.Length)
                        raw = col.enumDisplayNames[idx];
                }
                return $"{raw} ▾";
            }
            return $"{filter.allowed.Count}개 ▾";
        }

        private void ShowAllowedValuesMenu(ColumnFilter filter, ColumnInfo col, Button anchor)
        {
            string[] raws = col.propType == SerializedPropertyType.Boolean
                ? new[] { "True", "False" }
                : col.enumNames ?? Array.Empty<string>();
            string[] displays = col.propType == SerializedPropertyType.Boolean
                ? raws
                : col.enumDisplayNames ?? raws;

            var menu = new GenericMenu();
            for (int i = 0; i < raws.Length; i++)
            {
                string raw = raws[i];
                string display = (i < displays.Length ? displays[i] : raw).Replace("/", "∕");
                bool on = filter.allowed.Contains(raw);
                menu.AddItem(new GUIContent(display), on, () =>
                {
                    if (on)
                        filter.allowed.Remove(raw);
                    else
                        filter.allowed.Add(raw);
                    anchor.text = AllowedSummary(filter, col);
                    ApplyFiltersAndRefresh();
                });
            }
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("전체 (선택 해제)"), filter.allowed.Count == 0, () =>
            {
                filter.allowed.Clear();
                anchor.text = AllowedSummary(filter, col);
                ApplyFiltersAndRefresh();
            });
            menu.DropDown(anchor.worldBound);
        }

        /// <summary>선택 타입의 새 에셋을 기존 에셋 폴더(없으면 범위 폴더)에 생성한다.</summary>
        private void CreateNewAsset()
        {
            var selected = _model.selected;
            if (selected == null)
                return;

            string dir = selected.assetPaths.Count > 0
                ? System.IO.Path.GetDirectoryName(selected.assetPaths[0])?.Replace('\\', '/')
                : _model.scopeFolder;
            if (string.IsNullOrEmpty(dir) || !AssetDatabase.IsValidFolder(dir))
                dir = "Assets";

            var instance = CreateInstance(selected.type);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{dir}/New {selected.type.Name}.asset");
            AssetDatabase.CreateAsset(instance, path);
            AssetDatabase.SaveAssets();
            Rescan();
            EditorGUIUtility.PingObject(instance);
        }

        // ── 열 너비 ──────────────────────────────────────────────────

        /// <summary>현재 리스트뷰에 반영된 (사용자가 드래그로 조정한) 열 너비를 백업한다.</summary>
        private void HarvestColumnWidths()
        {
            foreach (var view in new[] { _leftView, _rightView })
            {
                if (view == null)
                    continue;
                foreach (var col in view.columns)
                {
                    float width = col.width.value;
                    if (width <= 0f)
                        continue;
                    if (col.name == NameColumnId)
                        _nameColumnWidth = width;
                    else if (!string.IsNullOrEmpty(col.name))
                        _savedWidths[col.name] = width;
                }
            }
        }

        private float SavedWidth(ColumnInfo info)
        {
            return _savedWidths.TryGetValue(info.propertyPath, out float saved)
                ? saved
                : DefaultColumnWidth(info.propType);
        }

        /// <summary>
        /// 헤더 텍스트와 셀 내용(문자열/enum/참조는 실제 값 샘플)에 맞춰 열 너비를 계산한다.
        /// 결과는 _savedWidths에 기록되며 다음 RebuildTable에서 반영된다.
        /// </summary>
        private void AutoFitWidths()
        {
            // 에셋 이름 열: 가장 긴 이름에 맞춤 (핑 버튼 여유 포함)
            float nameWidth = 100f;
            int nameSamples = Mathf.Min(_model.rows.Count, 200);
            for (int i = 0; i < nameSamples; i++)
            {
                float w = MeasureLabel(_model.rows[i].DisplayName, bold: true) + 36f;
                nameWidth = Mathf.Max(nameWidth, w);
            }
            _nameColumnWidth = Mathf.Clamp(nameWidth, 100f, 320f);

            foreach (var col in _model.columns)
            {
                // 정렬 화살표 여유분 포함
                float width = MeasureLabel(col.displayName, bold: true) + 24f;
                width = Mathf.Max(width, MinContentWidth(col.propType));
                width = Mathf.Max(width, SampleContentWidth(col));
                _savedWidths[col.propertyPath] = Mathf.Clamp(width, 40f, 420f);
            }
        }

        /// <summary>
        /// 라벨 텍스트 폭 측정. 에디터 기동 직후에는 EditorStyles가 아직 초기화되지 않아
        /// 접근만으로 NRE가 나므로, 실패 시 글자 수 기반 근사치로 폴백한다.
        /// </summary>
        private static float MeasureLabel(string text, bool bold)
        {
            if (string.IsNullOrEmpty(text))
                return 0f;
            try
            {
                var style = bold ? EditorStyles.boldLabel : EditorStyles.label;
                if (style != null)
                    return style.CalcSize(new GUIContent(text)).x;
            }
            catch (NullReferenceException)
            {
            }
            return text.Length * 7.5f;
        }

        /// <summary>표시 중인 열들을 비율 유지한 채 창 너비에 맞게 늘리거나 줄인다.</summary>
        private void StretchColumnsToWindow()
        {
            HarvestColumnWidths();

            var visible = GetVisibleColumnIndices();
            float avail = position.width - 16f; // 세로 스크롤바 여유
            float total = 0f;
            foreach (int mi in visible)
                total += mi == 0 ? _nameColumnWidth : SavedWidth(_model.columns[mi - 1]);
            if (total <= 0f || avail <= 0f)
                return;

            float scale = avail / total;
            _nameColumnWidth = Mathf.Max(100f, _nameColumnWidth * scale);
            foreach (int mi in visible)
            {
                if (mi == 0)
                    continue;
                var col = _model.columns[mi - 1];
                _savedWidths[col.propertyPath] = Mathf.Max(40f, SavedWidth(col) * scale);
            }
        }

        /// <summary>타입별로 편집에 필요한 최소 내용 너비.</summary>
        private static float MinContentWidth(SerializedPropertyType t)
        {
            switch (t)
            {
                case SerializedPropertyType.Boolean: return 30f;
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Float: return 60f;
                case SerializedPropertyType.String: return 70f;
                case SerializedPropertyType.ObjectReference:
                case SerializedPropertyType.ExposedReference: return 100f;
                default: return DefaultColumnWidth(t);
            }
        }

        /// <summary>텍스트성 값(문자열/enum/오브젝트 참조)의 실제 내용 너비를 일부 행에서 샘플링한다.</summary>
        private float SampleContentWidth(ColumnInfo col)
        {
            float padding;
            switch (col.propType)
            {
                case SerializedPropertyType.String: padding = 16f; break;
                case SerializedPropertyType.Enum: padding = 26f; break;
                case SerializedPropertyType.ObjectReference: padding = 44f; break; // 아이콘 + 피커 버튼
                default: return 0f;
            }

            float width = 0f;
            int samples = Mathf.Min(_model.rows.Count, 30);
            for (int i = 0; i < samples; i++)
            {
                var so = _model.rows[i].GetSerialized();
                if (so == null)
                    continue;
                var prop = _model.rows[i].GetProperty(col.propertyPath);
                if (prop == null)
                    continue;

                string text;
                switch (col.propType)
                {
                    case SerializedPropertyType.String:
                        text = prop.stringValue;
                        break;
                    case SerializedPropertyType.Enum:
                        var names = prop.enumDisplayNames;
                        int idx = prop.enumValueIndex;
                        text = idx >= 0 && idx < names.Length ? names[idx] : string.Empty;
                        break;
                    case SerializedPropertyType.ObjectReference:
                        text = prop.objectReferenceValue != null ? prop.objectReferenceValue.name : "None";
                        break;
                    default:
                        text = null;
                        break;
                }
                if (string.IsNullOrEmpty(text))
                    continue;
                width = Mathf.Max(width, MeasureLabel(text, bold: false) + padding);
            }
            return width;
        }

        private static float DefaultColumnWidth(SerializedPropertyType t)
        {
            switch (t)
            {
                case SerializedPropertyType.Boolean: return 40f;
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Float: return 70f;
                case SerializedPropertyType.Enum:
                case SerializedPropertyType.LayerMask: return 110f;
                case SerializedPropertyType.String: return 160f;
                case SerializedPropertyType.Vector2:
                case SerializedPropertyType.Vector2Int: return 130f;
                case SerializedPropertyType.Vector3:
                case SerializedPropertyType.Vector3Int:
                case SerializedPropertyType.Quaternion: return 180f;
                case SerializedPropertyType.Vector4: return 220f;
                case SerializedPropertyType.Color:
                case SerializedPropertyType.Gradient:
                case SerializedPropertyType.AnimationCurve: return 90f;
                case SerializedPropertyType.ObjectReference:
                case SerializedPropertyType.ExposedReference: return 160f;
                default: return 110f;
            }
        }

        // ── 타입 드롭다운 ────────────────────────────────────────────

        /// <summary>툴바에서 SO 타입을 고르는 검색 가능한 드롭다운.</summary>
        private class TypeDropdown : AdvancedDropdown
        {
            private readonly SOSpreadsheetWindow _window;

            public TypeDropdown(AdvancedDropdownState state, SOSpreadsheetWindow window)
                : base(state)
            {
                _window = window;
                minimumSize = new Vector2(320f, 420f);
            }

            protected override AdvancedDropdownItem BuildRoot()
            {
                var root = new AdvancedDropdownItem("ScriptableObject 타입");
                var types = _window._model.types;
                for (int i = 0; i < types.Count; i++)
                {
                    var entry = types[i];
                    root.AddChild(new AdvancedDropdownItem(
                        $"{entry.type.Name}  ({entry.assetPaths.Count})")
                    {
                        id = i,
                    });
                }
                return root;
            }

            protected override void ItemSelected(AdvancedDropdownItem item)
            {
                var types = _window._model.types;
                if (item.id >= 0 && item.id < types.Count)
                    _window.OnTypeSelected(types[item.id]);
            }
        }
    }
}
