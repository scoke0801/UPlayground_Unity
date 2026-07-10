using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UPlayGround.Tool.Editor.SOSpreadsheet
{
    /// <summary>
    /// 프로젝트의 모든 ScriptableObject 에셋을 타입별로 모아
    /// 스프레드시트(행 = 에셋, 열 = 직렬화 필드) 형태로 조회/편집하는 창.
    /// Scriptable Sheets 스타일 구성:
    /// - 모든 행은 1줄 고정 높이. 중첩 클래스/배열은 셀 안이 아니라 열로 평탄화.
    /// - 타입 선택은 툴바의 검색 가능한 드롭다운.
    /// - 페이지네이션, 열 표시/숨김 메뉴, 열 너비 프리셋(내용맞춤/압축/채움).
    /// 메뉴: UPlayGround/SO 스프레드시트
    /// </summary>
    public class SOSpreadsheetWindow : EditorWindow
    {
        private const float ToolbarHeight = 21f;
        private const float BottomBarHeight = 21f;
        private const float RowHeight = 22f;
        private const float CellPadding = 2f;
        /// <summary>배열 하나가 만들 수 있는 최대 요소 열 수 (거대 배열로 열이 폭주하는 것 방지).</summary>
        private const int MaxElementColumns = 40;
        /// <summary>전체 열 수 상한.</summary>
        private const int MaxTotalColumns = 300;
        /// <summary>중첩 평탄화 최대 깊이.</summary>
        private const int MaxFlattenDepth = 5;

        private static readonly int[] PageSizes = { 25, 50, 100, 250, 0 }; // 0 = 전체
        private static readonly string[] PageSizeLabels = { "25", "50", "100", "250", "전체" };

        private static readonly int[] FreezeValues = { 0, 1, 2, 3, 4, 5, 6 };
        private static readonly string[] FreezeLabels = { "없음", "1열", "2열", "3열", "4열", "5열", "6열" };

        /// <summary>외부 에셋 제외 토글이 켜졌을 때 스캔에서 제외할 경로 접두사.</summary>
        private static readonly string[] ExternalPathPrefixes =
        {
            "Assets/ExternalAssets",
            "Assets/Plugins",
            "Assets/TextMesh Pro",
            "Assets/AddressableAssetsData",
            "Assets/Settings",
        };

        // ── 내부 모델 ────────────────────────────────────────────────

        /// <summary>프로젝트에서 발견된 SO 타입 하나 (정확히 일치하는 타입 기준으로 그룹핑).</summary>
        private class TypeEntry
        {
            public Type type;
            public List<string> assetPaths = new();
        }

        /// <summary>테이블의 행 하나 = 에셋 하나. 에셋/SerializedObject는 처음 그려질 때 지연 로드.</summary>
        private class RowEntry
        {
            public string path;
            public ScriptableObject asset;
            public SerializedObject serialized;
            public Dictionary<string, SerializedProperty> props;

            public string DisplayName => asset != null
                ? asset.name
                : System.IO.Path.GetFileNameWithoutExtension(path);

            public SerializedObject GetSerialized()
            {
                if (asset == null)
                {
                    asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                    serialized = null;
                }
                if (asset == null)
                    return null;

                if (serialized == null)
                {
                    serialized = new SerializedObject(asset);
                    props = new Dictionary<string, SerializedProperty>();
                }
                return serialized;
            }

            public SerializedProperty GetProperty(string propertyPath)
            {
                if (!props.TryGetValue(propertyPath, out var p))
                {
                    p = serialized.FindProperty(propertyPath);
                    props[propertyPath] = p;
                }
                return p;
            }
        }

        /// <summary>
        /// 테이블의 열 하나 = 평탄화된 직렬화 필드 하나.
        /// 커스텀 드로어/데코레이터 정보는 필드(열) 단위로 동일하므로
        /// 열 구성 시점에 한 번만 계산해 캐시한다 (매 프레임 리플렉션 방지).
        /// </summary>
        private class ColumnInfo
        {
            public string propertyPath;
            public string displayName;
            public SerializedPropertyType propType;
            public bool hasCustomDrawer;
            /// <summary>셀 위쪽에서 잘라낼 높이 ([Header]/[Space] 데코레이터 + [TextArea] 빈 라벨 줄).</summary>
            public float topCut;
            /// <summary>높이 계산 없이 1줄 PropertyField로 바로 그릴 수 있는 열인지.</summary>
            public bool fastPath;
            /// <summary>셀에 그리지 않고 요약([N] / {…})만 표시하는 복합 타입 열인지.</summary>
            public bool summaryOnly;
            /// <summary>
            /// 경로에 포함된 배열 요소 구간 (배열 경로, 요소 인덱스) 목록.
            /// 행의 배열이 짧아 셀이 비었을 때 클릭으로 배열을 늘려 칸을 만드는 데 사용.
            /// </summary>
            public (string arrayPath, int index)[] arraySegments;
        }

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
                for (int i = 0; i < _window._types.Count; i++)
                {
                    var entry = _window._types[i];
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
                if (item.id >= 0 && item.id < _window._types.Count)
                {
                    _window.SelectType(_window._types[item.id]);
                    _window.Repaint();
                }
            }
        }

        // ── 상태 ─────────────────────────────────────────────────────

        private List<TypeEntry> _types = new();
        private TypeEntry _selected;
        private List<RowEntry> _rows = new();   // 선택 타입의 전체 행
        private List<RowEntry> _view = new();   // 검색/정렬이 적용된 표시용 행
        private List<ColumnInfo> _columns = new();
        private bool _columnsTruncated;

        private MultiColumnHeader _header;
        private MultiColumnHeaderState _headerState;
        private AdvancedDropdownState _typeDropdownState = new();

        private Vector2 _tableScroll;

        [SerializeField] private string _scopeFolder = "Assets";
        [SerializeField] private bool _excludeExternal = true;
        [SerializeField] private string _selectedTypeName;
        [SerializeField] private string _assetSearch = string.Empty;
        [SerializeField] private bool _showChildren = true;
        [SerializeField] private bool _showArrays = true;
        [SerializeField] private int _pageSizeIndex = 1; // 기본 50
        /// <summary>가로 스크롤과 무관하게 항상 표시할 왼쪽 열 개수 (엑셀 틀 고정).</summary>
        [SerializeField] private int _freezeCount = 1;
        private int _pageIndex;

        private GUIStyle _nameCellStyle;
        private GUIStyle _centerLabelStyle;
        private bool _stylesReady;

        // 열 너비 자동 맞춤은 GUI 컨텍스트(스타일 측정)가 필요하므로 OnGUI 시작 시점에 처리
        private bool _autoFitPending;
        private HashSet<string> _autoFitOnly; // null이면 전체 열 대상

        // 배열 크기 편집 등으로 요소 열 수가 달라지면 다음 틱에 열 재구성
        private bool _columnsStale;

        // 열 사각형 프레임당 1회 계산용 재사용 버퍼 (행마다 GetColumnRect 반복 호출 방지)
        private Rect[] _colRectCache;

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
            Rescan();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        /// <summary>
        /// 언두/리두는 배열 크기를 바꿀 수 있다. 사라진 요소를 가리키는 캐시 프로퍼티를
        /// 그대로 쓰면 예외로 그리기가 중단되므로 전 행의 프로퍼티 캐시를 비우고,
        /// 요소 열 수가 달라졌을 수 있으니 열도 재구성한다.
        /// </summary>
        private void OnUndoRedo()
        {
            foreach (var row in _rows)
            {
                row.serialized?.Update();
                row.props?.Clear();
            }
            _columnsStale = true;
            Repaint();
        }

        // ── 스캔 / 선택 ──────────────────────────────────────────────

        /// <summary>프로젝트를 다시 스캔하고 기존 타입 선택을 복원한다.</summary>
        private void Rescan()
        {
            ScanProject();

            TypeEntry entry = null;
            if (!string.IsNullOrEmpty(_selectedTypeName))
                entry = _types.FirstOrDefault(t => t.type.AssemblyQualifiedName == _selectedTypeName);
            SelectType(entry);
        }

        /// <summary>
        /// 범위 폴더 내 모든 SO 에셋을 타입별로 수집한다.
        /// GetMainAssetTypeAtPath는 에셋을 로드하지 않으므로 수천 개여도 부담이 적다.
        /// </summary>
        private void ScanProject()
        {
            string scope = AssetDatabase.IsValidFolder(_scopeFolder) ? _scopeFolder : "Assets";
            var map = new Dictionary<Type, TypeEntry>();

            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { scope }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (_excludeExternal && IsExternalPath(path))
                    continue;

                Type type = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (type == null || !typeof(ScriptableObject).IsAssignableFrom(type))
                    continue;
                // 에디터 전용 SO(빌드 설정류 등)는 제외
                if (type.Namespace != null && type.Namespace.StartsWith("UnityEditor"))
                    continue;

                if (!map.TryGetValue(type, out var entry))
                {
                    entry = new TypeEntry { type = type };
                    map.Add(type, entry);
                }
                entry.assetPaths.Add(path);
            }

            _types = map.Values.OrderBy(e => e.type.Name, StringComparer.Ordinal).ToList();
            foreach (var e in _types)
                e.assetPaths.Sort(StringComparer.Ordinal);
        }

        private static bool IsExternalPath(string path)
        {
            foreach (string prefix in ExternalPathPrefixes)
            {
                if (path.StartsWith(prefix, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private void SelectType(TypeEntry entry)
        {
            _selected = entry;
            _selectedTypeName = entry?.type.AssemblyQualifiedName;
            _rows.Clear();
            _view.Clear();
            _columns.Clear();
            _header = null;
            _headerState = null;
            _tableScroll = Vector2.zero;
            _pageIndex = 0;

            if (entry == null)
                return;

            foreach (string path in entry.assetPaths)
                _rows.Add(new RowEntry { path = path });

            BuildColumns(preserveWidths: false);
            RequestAutoFit(null);
            ApplyFilter();
        }

        // ── 열 구성 (전체 평탄화) ────────────────────────────────────

        /// <summary>
        /// 선택 타입의 직렬화 필드를 전부 평탄화해 열로 만든다.
        /// 중첩 클래스는 "부모.자식", 배열은 "크기" 열 + "이름[i]" 요소 열로 전개된다.
        /// </summary>
        private void BuildColumns(bool preserveWidths)
        {
            // 재구성 전 열 너비 백업 (배열 크기 변화 등으로 재구성해도 기존 열 너비 유지)
            Dictionary<string, float> savedWidths = null;
            float nameWidth = 180f;
            if (preserveWidths && _headerState != null &&
                _headerState.columns.Length == _columns.Count + 1)
            {
                savedWidths = new Dictionary<string, float>();
                nameWidth = _headerState.columns[0].width;
                for (int i = 1; i < _headerState.columns.Length; i++)
                    savedWidths[_columns[i - 1].propertyPath] = _headerState.columns[i].width;
            }

            _columns.Clear();
            _columnsTruncated = false;
            _header = null;
            _headerState = null;

            SerializedObject sample = null;
            foreach (var row in _rows)
            {
                sample = row.GetSerialized();
                if (sample != null)
                    break;
            }
            if (sample == null)
                return;

            var it = sample.GetIterator();
            if (it.NextVisible(true))
            {
                do
                {
                    if (it.propertyPath == "m_Script")
                        continue;
                    AddColumnRecursive(it.Copy(), it.displayName, 0);
                }
                while (it.NextVisible(false));
            }

            var cols = new List<MultiColumnHeaderState.Column>
            {
                new()
                {
                    headerContent = new GUIContent("에셋"),
                    width = nameWidth,
                    minWidth = 100f,
                    autoResize = false,
                    allowToggleVisibility = false,
                    canSort = true,
                    headerTextAlignment = TextAlignment.Left,
                },
            };

            foreach (var col in _columns)
            {
                float width = savedWidths != null && savedWidths.TryGetValue(col.propertyPath, out float saved)
                    ? saved
                    : DefaultColumnWidth(col.propType);
                cols.Add(new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent(col.displayName, col.propertyPath),
                    width = width,
                    minWidth = 40f,
                    autoResize = false,
                    allowToggleVisibility = true,
                    canSort = IsSortable(col.propType),
                    headerTextAlignment = TextAlignment.Left,
                });
            }

            _headerState = new MultiColumnHeaderState(cols.ToArray());
            _header = new MultiColumnHeader(_headerState);
            _header.sortingChanged += _ => { ApplyFilter(); Repaint(); };
        }

        /// <summary>
        /// 필드 하나를 평탄화해 열로 추가한다.
        /// - 단순 타입 / 커스텀 드로어 타입 → 열 1개
        /// - 중첩 클래스 → 자식들을 "부모.자식" 열로 재귀 전개
        /// - 배열 → "크기" 열 + 최대 요소 수만큼 "이름[i]" 열 (요소가 구조체면 다시 전개)
        /// </summary>
        private void AddColumnRecursive(SerializedProperty prop, string display, int depth)
        {
            if (_columns.Count >= MaxTotalColumns)
            {
                _columnsTruncated = true;
                return;
            }

            bool generic = prop.propertyType == SerializedPropertyType.Generic;
            if (!generic)
            {
                AddLeafColumn(prop, display);
                return;
            }

            // 1줄로 그려지는 커스텀 드로어만 열 하나로 존중한다.
            // 여러 줄 드로어(리스트 래퍼 등)는 1줄 셀에 그릴 수 없으므로 무시하고 내부 데이터를 평탄화.
            if (HasCustomDrawer(prop) && IsSingleLineDrawn(prop))
            {
                AddLeafColumn(prop, display);
                return;
            }

            if (depth >= MaxFlattenDepth)
            {
                AddLeafColumn(prop, display);
                return;
            }

            if (prop.isArray)
            {
                if (!_showArrays)
                {
                    AddLeafColumn(prop, display);
                    return;
                }

                // 크기 열: 요소가 없어도 배열의 존재와 크기 편집이 가능하도록 항상 추가
                string sizePath = prop.propertyPath + ".Array.size";
                _columns.Add(new ColumnInfo
                {
                    propertyPath = sizePath,
                    displayName = display + " 크기",
                    propType = SerializedPropertyType.ArraySize,
                    arraySegments = ParseArraySegments(sizePath),
                });

                // 에셋마다 요소 수가 다를 수 있으므로 가장 큰 배열을 기준으로 열을 만든다.
                // 배열이 더 짧은 에셋의 셀은 "—"로 표시된다.
                var largest = FindLargestArrayProperty(prop.propertyPath, out int maxSize);
                int count = Mathf.Min(maxSize, MaxElementColumns);
                for (int i = 0; i < count; i++)
                    AddColumnRecursive(largest.GetArrayElementAtIndex(i), $"{display}[{i}]", depth + 1);
                if (maxSize > count)
                    _columnsTruncated = true;
                return;
            }

            if (!_showChildren)
            {
                AddLeafColumn(prop, display);
                return;
            }

            bool any = false;
            foreach (var child in VisibleChildren(prop))
            {
                AddColumnRecursive(child, $"{display}.{child.displayName}", depth + 1);
                any = true;
            }
            if (!any)
                AddLeafColumn(prop, display);
        }

        private void AddLeafColumn(SerializedProperty prop, string display)
        {
            bool customDrawer = HasCustomDrawer(prop);
            float topCut = ComputeTopCut(prop, customDrawer);
            bool generic = prop.propertyType == SerializedPropertyType.Generic;

            // 복합 타입 잎 열은 1줄 커스텀 드로어가 있을 때만 셀에 그리고, 나머지는 요약 표시
            // (토글 꺼짐/깊이 초과로 남은 리스트·클래스를 셀에 통째로 그리면 느리고 깨진다)
            bool summaryOnly = generic && !(customDrawer && IsSingleLineDrawn(prop));

            _columns.Add(new ColumnInfo
            {
                propertyPath = prop.propertyPath,
                displayName = display,
                propType = prop.propertyType,
                hasCustomDrawer = customDrawer,
                topCut = topCut,
                summaryOnly = summaryOnly,
                fastPath = !customDrawer && topCut <= 0f && IsSingleLineType(prop.propertyType),
                arraySegments = ParseArraySegments(prop.propertyPath),
            });
        }

        /// <summary>프로퍼티 경로에서 배열 요소 구간(".Array.data[i]")들을 왼쪽부터 추출한다.</summary>
        private static (string arrayPath, int index)[] ParseArraySegments(string path)
        {
            List<(string, int)> result = null;
            int searchFrom = 0;
            while (true)
            {
                int marker = path.IndexOf(".Array.data[", searchFrom, StringComparison.Ordinal);
                if (marker < 0)
                    break;
                int close = path.IndexOf(']', marker + 12);
                if (close < 0)
                    break;
                if (int.TryParse(path.Substring(marker + 12, close - marker - 12), out int index))
                    (result ??= new List<(string, int)>()).Add((path.Substring(0, marker), index));
                searchFrom = close + 1;
            }
            return result?.ToArray();
        }

        /// <summary>드로어가 그리는 높이가 1줄 이내인지 (열 구성 시 대표 에셋으로 한 번만 판정).</summary>
        private static bool IsSingleLineDrawn(SerializedProperty prop)
        {
            float height = EditorGUI.GetPropertyHeight(prop, GUIContent.none, true) - GetDecoratorHeight(prop);
            return height <= EditorGUIUtility.singleLineHeight + 2f;
        }

        /// <summary>
        /// 셀 위쪽에서 잘라낼 높이. 데코레이터([Header]/[Space])와
        /// [TextArea]류 드로어가 예약하는 빈 라벨 줄은 필드 단위로 일정하므로 열 구성 시 계산한다.
        /// </summary>
        private static float ComputeTopCut(SerializedProperty prop, bool customDrawer)
        {
            float cut = GetDecoratorHeight(prop);
            if (prop.propertyType == SerializedPropertyType.String && customDrawer)
            {
                float remaining = EditorGUI.GetPropertyHeight(prop, GUIContent.none, true) - cut;
                if (remaining > EditorGUIUtility.singleLineHeight + 1f)
                    cut += EditorGUIUtility.singleLineHeight;
            }
            return cut;
        }

        /// <summary>wideMode에서 항상 1줄 높이로 그려지는 타입인지 (높이 질의 생략 가능).</summary>
        private static bool IsSingleLineType(SerializedPropertyType t)
        {
            switch (t)
            {
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Boolean:
                case SerializedPropertyType.Float:
                case SerializedPropertyType.String:
                case SerializedPropertyType.Enum:
                case SerializedPropertyType.Color:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.Character:
                case SerializedPropertyType.AnimationCurve:
                case SerializedPropertyType.Gradient:
                case SerializedPropertyType.Vector2:
                case SerializedPropertyType.Vector3:
                case SerializedPropertyType.Vector4:
                case SerializedPropertyType.Vector2Int:
                case SerializedPropertyType.Vector3Int:
                case SerializedPropertyType.Quaternion:
                case SerializedPropertyType.ObjectReference:
                case SerializedPropertyType.ExposedReference:
                case SerializedPropertyType.ArraySize:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>모든 행에서 해당 경로의 배열 중 요소 수가 가장 많은 것을 찾는다.</summary>
        private SerializedProperty FindLargestArrayProperty(string path, out int maxSize)
        {
            maxSize = 0;
            SerializedProperty best = null;
            foreach (var row in _rows)
            {
                var so = row.GetSerialized();
                if (so == null)
                    continue;
                so.UpdateIfRequiredOrScript();
                var p = row.GetProperty(path);
                if (p == null || !p.isArray)
                    continue;
                if (p.arraySize > maxSize)
                {
                    maxSize = p.arraySize;
                    best = p;
                }
            }
            return best;
        }

        /// <summary>프로퍼티의 직계 가시 자식들을 순회한다 (배열에는 사용하지 않음).</summary>
        private static IEnumerable<SerializedProperty> VisibleChildren(SerializedProperty parent)
        {
            var it = parent.Copy();
            var end = it.GetEndProperty();
            if (!it.NextVisible(true))
                yield break;
            while (!SerializedProperty.EqualContents(it, end))
            {
                yield return it.Copy();
                if (!it.NextVisible(false))
                    yield break;
            }
        }

        // ── 필터 / 정렬 / 페이지 ─────────────────────────────────────

        private void ApplyFilter()
        {
            _view = string.IsNullOrEmpty(_assetSearch)
                ? new List<RowEntry>(_rows)
                : _rows.Where(r => r.DisplayName.IndexOf(_assetSearch, StringComparison.OrdinalIgnoreCase) >= 0)
                       .ToList();
            ApplySort();
            _pageIndex = Mathf.Clamp(_pageIndex, 0, Mathf.Max(0, PageCount - 1));
        }

        private int PageSize => PageSizes[Mathf.Clamp(_pageSizeIndex, 0, PageSizes.Length - 1)];

        private int PageCount
        {
            get
            {
                if (PageSize <= 0 || _view.Count == 0)
                    return 1;
                return (_view.Count + PageSize - 1) / PageSize;
            }
        }

        private int PageStart => PageSize <= 0 ? 0 : _pageIndex * PageSize;

        private int PageRowCount => PageSize <= 0
            ? _view.Count
            : Mathf.Min(PageSize, _view.Count - PageStart);

        private void ApplySort()
        {
            if (_header == null || _header.sortedColumnIndex < 0 ||
                _header.sortedColumnIndex > _columns.Count)
                return;

            int col = _header.sortedColumnIndex;
            bool ascending = _header.IsSortedAscending(col);

            if (col == 0)
            {
                _view.Sort((a, b) =>
                    string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                // 정렬 시점에만 전 행의 키를 한 번 추출 (여기서 미로드 에셋이 로드됨)
                string propPath = _columns[col - 1].propertyPath;
                var keys = new Dictionary<RowEntry, object>();
                foreach (var row in _view)
                {
                    var so = row.GetSerialized();
                    so?.UpdateIfRequiredOrScript();
                    keys[row] = so != null ? GetSortKey(row.GetProperty(propPath)) : null;
                }
                _view.Sort((a, b) => CompareKeys(keys[a], keys[b]));
            }

            if (!ascending)
                _view.Reverse();
        }

        private static object GetSortKey(SerializedProperty p)
        {
            if (p == null)
                return null;

            switch (p.propertyType)
            {
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.Character:
                case SerializedPropertyType.ArraySize:
                    return p.longValue;
                case SerializedPropertyType.Enum:
                    return (long)p.intValue;
                case SerializedPropertyType.Boolean:
                    return p.boolValue ? 1L : 0L;
                case SerializedPropertyType.Float:
                    return p.doubleValue;
                case SerializedPropertyType.String:
                    return p.stringValue;
                case SerializedPropertyType.ObjectReference:
                    return p.objectReferenceValue != null ? p.objectReferenceValue.name : string.Empty;
                default:
                    // 배열/리스트는 요소 개수로 정렬
                    return p.isArray ? (object)(long)p.arraySize : 0L;
            }
        }

        private static int CompareKeys(object a, object b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;
            if (a.GetType() == b.GetType() && a is IComparable comparable)
                return comparable.CompareTo(b);
            return string.CompareOrdinal(a.ToString(), b.ToString());
        }

        private static bool IsSortable(SerializedPropertyType t)
        {
            switch (t)
            {
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Boolean:
                case SerializedPropertyType.Float:
                case SerializedPropertyType.String:
                case SerializedPropertyType.Enum:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.Character:
                case SerializedPropertyType.ObjectReference:
                case SerializedPropertyType.ArraySize:
                case SerializedPropertyType.Generic:
                    return true;
                default:
                    return false;
            }
        }

        // ── GUI ──────────────────────────────────────────────────────

        private void OnGUI()
        {
            EnsureStyles();

            if (_autoFitPending)
            {
                _autoFitPending = false;
                AutoFitColumnWidths(_autoFitOnly);
                _autoFitOnly = null;
            }

            DrawToolbar();

            Rect table = new(
                0f, ToolbarHeight,
                position.width, position.height - ToolbarHeight - BottomBarHeight);
            DrawTablePanel(table);

            DrawBottomBar(new Rect(0f, position.height - BottomBarHeight, position.width, BottomBarHeight));

            // 배열 크기 편집으로 요소 열 수가 달라졌으면 GUI 바깥에서 열 재구성
            if (_columnsStale)
            {
                _columnsStale = false;
                EditorApplication.delayCall += () =>
                {
                    if (this == null)
                        return;
                    RebuildColumnsPreservingWidths();
                    Repaint();
                };
            }
        }

        /// <summary>기존 열 너비를 유지한 채 열을 재구성하고, 새로 생긴 열만 너비를 맞춘다.</summary>
        private void RebuildColumnsPreservingWidths()
        {
            var known = new HashSet<string>();
            foreach (var col in _columns)
                known.Add(col.propertyPath);

            BuildColumns(preserveWidths: true);

            var fresh = new HashSet<string>();
            foreach (var col in _columns)
            {
                if (!known.Contains(col.propertyPath))
                    fresh.Add(col.propertyPath);
            }
            if (fresh.Count > 0)
                RequestAutoFit(fresh);

            ApplyFilter();
        }

        private void DrawToolbar()
        {
            GUILayout.BeginArea(new Rect(0f, 0f, position.width, ToolbarHeight));
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            // 타입 선택 드롭다운 (검색 지원)
            string typeLabel = _selected != null
                ? $"{_selected.type.Name} ({_selected.assetPaths.Count})"
                : "타입 선택…";
            Rect dropRect = GUILayoutUtility.GetRect(
                new GUIContent(typeLabel), EditorStyles.toolbarDropDown,
                GUILayout.MinWidth(180f), GUILayout.MaxWidth(300f));
            if (GUI.Button(dropRect, typeLabel, EditorStyles.toolbarDropDown))
                new TypeDropdown(_typeDropdownState, this).Show(dropRect);

            if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(64f)))
                Rescan();

            using (new EditorGUI.DisabledScope(_selected == null))
            {
                if (GUILayout.Button("+ 새 에셋", EditorStyles.toolbarButton, GUILayout.Width(66f)))
                    CreateNewAsset();
            }

            if (GUILayout.Button("모두 저장", EditorStyles.toolbarButton, GUILayout.Width(64f)))
                AssetDatabase.SaveAssets();

            GUILayout.Space(8f);

            using (new EditorGUI.DisabledScope(_header == null))
            {
                if (GUILayout.Button("열 표시", EditorStyles.toolbarDropDown, GUILayout.Width(60f)))
                    ShowColumnVisibilityMenu();
                if (GUILayout.Button("너비", EditorStyles.toolbarDropDown, GUILayout.Width(50f)))
                    ShowWidthPresetMenu();
            }

            GUILayout.Space(8f);

            // 평탄화 토글 (Scriptable Sheets의 Show Children / Show Arrays)
            EditorGUI.BeginChangeCheck();
            _showChildren = GUILayout.Toggle(_showChildren, "자식 필드", EditorStyles.toolbarButton, GUILayout.Width(64f));
            _showArrays = GUILayout.Toggle(_showArrays, "배열 요소", EditorStyles.toolbarButton, GUILayout.Width(64f));
            if (EditorGUI.EndChangeCheck() && _selected != null)
            {
                BuildColumns(preserveWidths: false);
                RequestAutoFit(null);
                ApplyFilter();
            }

            GUILayout.Space(8f);

            // 틀 고정: 왼쪽부터 N개 열은 가로 스크롤과 무관하게 항상 표시
            GUILayout.Label("고정", EditorStyles.miniLabel, GUILayout.Width(26f));
            _freezeCount = EditorGUILayout.IntPopup(
                _freezeCount, FreezeLabels, FreezeValues, EditorStyles.toolbarPopup, GUILayout.Width(48f));

            GUILayout.FlexibleSpace();

            EditorGUI.BeginChangeCheck();
            _assetSearch = GUILayout.TextField(_assetSearch, EditorStyles.toolbarSearchField, GUILayout.Width(220f));
            if (EditorGUI.EndChangeCheck())
                ApplyFilter();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawBottomBar(Rect area)
        {
            GUILayout.BeginArea(area);
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            // 스캔 범위
            GUILayout.Label("범위", EditorStyles.miniLabel, GUILayout.Width(28f));
            EditorGUI.BeginChangeCheck();
            _scopeFolder = EditorGUILayout.DelayedTextField(_scopeFolder, EditorStyles.toolbarTextField, GUILayout.Width(180f));
            bool scopeChanged = EditorGUI.EndChangeCheck();

            EditorGUI.BeginChangeCheck();
            _excludeExternal = GUILayout.Toggle(_excludeExternal, "외부 제외", EditorStyles.toolbarButton, GUILayout.Width(60f));
            if (EditorGUI.EndChangeCheck() || scopeChanged)
                Rescan();

            GUILayout.Space(12f);

            if (_selected != null)
            {
                string info = $"{_view.Count}/{_rows.Count}행 · {_columns.Count}열";
                if (_columnsTruncated)
                    info += " (열 일부 생략)";
                GUILayout.Label(info, EditorStyles.miniLabel);
            }

            GUILayout.FlexibleSpace();

            // 페이지네이션
            GUILayout.Label("페이지 크기", EditorStyles.miniLabel, GUILayout.Width(62f));
            EditorGUI.BeginChangeCheck();
            _pageSizeIndex = EditorGUILayout.Popup(_pageSizeIndex, PageSizeLabels, EditorStyles.toolbarPopup, GUILayout.Width(52f));
            if (EditorGUI.EndChangeCheck())
            {
                _pageIndex = 0;
                _tableScroll = Vector2.zero;
            }

            int pageCount = PageCount;
            using (new EditorGUI.DisabledScope(_pageIndex <= 0))
            {
                if (GUILayout.Button("|◀", EditorStyles.toolbarButton, GUILayout.Width(28f)))
                    SetPage(0);
                if (GUILayout.Button("◀", EditorStyles.toolbarButton, GUILayout.Width(28f)))
                    SetPage(_pageIndex - 1);
            }

            GUILayout.Label($"{_pageIndex + 1} / {pageCount}", EditorStyles.miniLabel, GUILayout.Width(50f));

            using (new EditorGUI.DisabledScope(_pageIndex >= pageCount - 1))
            {
                if (GUILayout.Button("▶", EditorStyles.toolbarButton, GUILayout.Width(28f)))
                    SetPage(_pageIndex + 1);
                if (GUILayout.Button("▶|", EditorStyles.toolbarButton, GUILayout.Width(28f)))
                    SetPage(pageCount - 1);
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void SetPage(int page)
        {
            _pageIndex = Mathf.Clamp(page, 0, PageCount - 1);
            _tableScroll.y = 0f;
        }

        /// <summary>선택 타입의 새 에셋을 기존 에셋 폴더(없으면 범위 폴더)에 생성한다.</summary>
        private void CreateNewAsset()
        {
            if (_selected == null)
                return;

            string dir = _selected.assetPaths.Count > 0
                ? System.IO.Path.GetDirectoryName(_selected.assetPaths[0])?.Replace('\\', '/')
                : _scopeFolder;
            if (string.IsNullOrEmpty(dir) || !AssetDatabase.IsValidFolder(dir))
                dir = "Assets";

            var instance = CreateInstance(_selected.type);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{dir}/New {_selected.type.Name}.asset");
            AssetDatabase.CreateAsset(instance, path);
            AssetDatabase.SaveAssets();
            Rescan();
            EditorGUIUtility.PingObject(instance);
        }

        // ── 열 표시 / 너비 메뉴 ──────────────────────────────────────

        private void ShowColumnVisibilityMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("모두 표시"), false, () =>
            {
                _headerState.visibleColumns = Enumerable.Range(0, _headerState.columns.Length).ToArray();
                Repaint();
            });
            menu.AddSeparator(string.Empty);

            var visible = new HashSet<int>(_headerState.visibleColumns);
            for (int i = 0; i < _columns.Count; i++)
            {
                int columnIndex = i + 1;
                // GenericMenu는 '/'를 서브메뉴로 해석하므로 이름의 구분자를 치환
                string label = _columns[i].displayName.Replace("/", "∕");
                menu.AddItem(new GUIContent(label), visible.Contains(columnIndex), () =>
                {
                    ToggleColumnVisible(columnIndex);
                    Repaint();
                });
            }
            menu.ShowAsContext();
        }

        private void ToggleColumnVisible(int columnIndex)
        {
            var list = _headerState.visibleColumns.ToList();
            if (list.Contains(columnIndex))
            {
                if (list.Count > 1)
                    list.Remove(columnIndex);
            }
            else
            {
                list.Add(columnIndex);
                list.Sort();
            }
            _headerState.visibleColumns = list.ToArray();
        }

        private void ShowWidthPresetMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("내용에 맞춤"), false, () => RequestAutoFit(null));
            menu.AddItem(new GUIContent("압축 (최소 너비)"), false, () =>
            {
                foreach (int i in _headerState.visibleColumns)
                    _headerState.columns[i].width = _headerState.columns[i].minWidth;
                Repaint();
            });
            menu.AddItem(new GUIContent("창에 채움"), false, () =>
            {
                StretchColumnsToWindow();
                Repaint();
            });
            menu.ShowAsContext();
        }

        /// <summary>표시 중인 열들을 비율 유지한 채 창 너비에 맞게 늘리거나 줄인다.</summary>
        private void StretchColumnsToWindow()
        {
            float avail = position.width - 16f; // 세로 스크롤바 여유
            float total = 0f;
            foreach (int i in _headerState.visibleColumns)
                total += _headerState.columns[i].width;
            if (total <= 0f || avail <= 0f)
                return;

            float scale = avail / total;
            foreach (int i in _headerState.visibleColumns)
            {
                var col = _headerState.columns[i];
                col.width = Mathf.Max(col.minWidth, col.width * scale);
            }
        }

        // ── 테이블 ───────────────────────────────────────────────────

        private void DrawTablePanel(Rect area)
        {
            if (_selected == null)
            {
                GUI.Label(area, "툴바의 타입 드롭다운에서 ScriptableObject 타입을 선택하세요.", _centerLabelStyle);
                return;
            }
            if (_header == null)
            {
                GUI.Label(area, "에셋을 로드하지 못했습니다. 새로고침 후 다시 시도하세요.", _centerLabelStyle);
                return;
            }

            float headerHeight = _header.height;

            // Vector3 등이 좁은 폭에서 2줄로 갈라지지 않게 강제 (1줄 행 유지)
            bool oldWideMode = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true;

            int pageStart = PageStart;
            int pageRows = PageRowCount;

            // 열 사각형은 모든 행에서 동일하므로 프레임당 한 번만 계산.
            // MultiColumnHeader.GetColumnRect는 헤더의 첫 OnGUI 전에는 내부 배열이 비어
            // NullReferenceException을 던지므로, 열 너비를 직접 누적해 계산한다.
            int[] visible = _headerState.visibleColumns;
            if (_colRectCache == null || _colRectCache.Length != visible.Length)
                _colRectCache = new Rect[visible.Length];
            float stripX = 0f;
            for (int vi = 0; vi < visible.Length; vi++)
            {
                float colWidth = _headerState.columns[visible[vi]].width;
                _colRectCache[vi] = new Rect(stripX, 0f, colWidth, RowHeight);
                stripX += colWidth;
            }

            float totalWidth = stripX;

            // 틀 고정: 앞에서부터 N개 열은 가로 스크롤과 무관하게 항상 표시.
            // 고정 영역이 창 너비의 60%를 넘으면 스크롤 공간 확보를 위해 개수를 줄인다.
            int freezeCount = Mathf.Clamp(_freezeCount, 0, visible.Length);
            float freezeWidth = 0f;
            for (int vi = 0; vi < freezeCount; vi++)
                freezeWidth += _colRectCache[vi].width;
            while (freezeCount > 0 && freezeWidth > area.width * 0.6f)
            {
                freezeCount--;
                freezeWidth -= _colRectCache[freezeCount].width;
            }

            // ── 헤더: 같은 헤더를 두 클립 영역에 나눠 그린다 (고정 파트는 스크롤 0 고정) ──
            Rect scrollHeader = new(area.x + freezeWidth, area.y, area.width - freezeWidth, headerHeight);
            GUI.BeginGroup(scrollHeader);
            _header.OnGUI(new Rect(0f, 0f, scrollHeader.width, headerHeight), _tableScroll.x + freezeWidth);
            GUI.EndGroup();

            if (freezeWidth > 0f)
            {
                GUI.BeginGroup(new Rect(area.x, area.y, freezeWidth, headerHeight));
                _header.OnGUI(new Rect(0f, 0f, freezeWidth, headerHeight), 0f);
                GUI.EndGroup();
            }

            // ── 바디 (스크롤 파트): 고정 열 이후만 가로 스크롤 ──
            Rect scrollBody = new(
                area.x + freezeWidth, area.y + headerHeight,
                area.width - freezeWidth, area.height - headerHeight);
            float contentHeight = pageRows * RowHeight;
            float scrollableWidth = Mathf.Max(totalWidth - freezeWidth, 1f);
            Rect viewRect = new(0f, 0f, scrollableWidth, contentHeight);

            bool hbarVisible = scrollableWidth > scrollBody.width + 1f;
            float hbarHeight = hbarVisible ? GUI.skin.horizontalScrollbar.fixedHeight : 0f;

            _tableScroll = GUI.BeginScrollView(scrollBody, _tableScroll, viewRect);

            // 보이는 행만 그려서 대량 에셋에서도 프레임 유지 (고정 행 높이라 계산이 단순)
            int first = Mathf.Max(0, Mathf.FloorToInt(_tableScroll.y / RowHeight));
            int last = Mathf.Min(pageRows - 1, Mathf.CeilToInt((_tableScroll.y + scrollBody.height) / RowHeight));

            // 가로 스크롤 뷰포트 밖의 열은 그리지 않는다 (열이 수백 개여도 프레임 유지)
            float cullXMin = _tableScroll.x + freezeWidth;
            float cullXMax = cullXMin + scrollBody.width;

            for (int i = first; i <= last; i++)
            {
                DrawRowCells(
                    pageStart + i, i * RowHeight,
                    xShift: freezeWidth, viFrom: freezeCount, viTo: visible.Length,
                    cullXMin, cullXMax, rowWidth: scrollableWidth);
            }

            GUI.EndScrollView();

            // ── 바디 (고정 파트): 세로 스크롤만 동기화 ──
            if (freezeWidth > 0f)
            {
                Rect frozenBody = new(
                    area.x, area.y + headerHeight,
                    freezeWidth, scrollBody.height - hbarHeight);

                // 고정 영역 위에서도 휠 스크롤이 동작하도록 수동 처리
                if (Event.current.type == EventType.ScrollWheel &&
                    frozenBody.Contains(Event.current.mousePosition))
                {
                    _tableScroll.y = Mathf.Clamp(
                        _tableScroll.y + Event.current.delta.y * RowHeight,
                        0f, Mathf.Max(0f, contentHeight - frozenBody.height));
                    Event.current.Use();
                    Repaint();
                }

                GUI.BeginGroup(frozenBody);
                for (int i = first; i <= last; i++)
                {
                    DrawRowCells(
                        pageStart + i, i * RowHeight - _tableScroll.y,
                        xShift: 0f, viFrom: 0, viTo: freezeCount,
                        cullXMin: 0f, cullXMax: freezeWidth, rowWidth: freezeWidth);
                }
                GUI.EndGroup();

                // 고정/스크롤 경계선
                EditorGUI.DrawRect(
                    new Rect(area.x + freezeWidth - 1f, area.y, 1f, headerHeight + frozenBody.height),
                    new Color(0f, 0f, 0f, 0.5f));
            }

            EditorGUIUtility.wideMode = oldWideMode;
        }

        /// <summary>
        /// 행 하나의 열 구간 [viFrom, viTo)를 그린다.
        /// 틀 고정을 위해 같은 행이 고정 파트(스크롤 미적용)와 스크롤 파트로 나뉘어 두 번 호출된다.
        /// xShift = 이 파트 좌표계 원점의 열 스트립상 위치 (스크롤 파트는 고정 영역 너비만큼 밀림).
        /// </summary>
        private void DrawRowCells(
            int viewIndex, float rowY, float xShift, int viFrom, int viTo,
            float cullXMin, float cullXMax, float rowWidth)
        {
            var row = _view[viewIndex];
            Rect rowRect = new(0f, rowY, rowWidth, RowHeight);

            if (viewIndex % 2 == 1)
                EditorGUI.DrawRect(rowRect, new Color(1f, 1f, 1f, 0.03f));

            var so = row.GetSerialized();
            if (so == null)
            {
                if (viFrom == 0)
                    GUI.Label(new Rect(4f, rowRect.y, 400f, RowHeight), $"(로드 실패) {row.path}", EditorStyles.miniLabel);
                return;
            }
            so.UpdateIfRequiredOrScript();

            float cellTop = rowRect.y + (RowHeight - EditorGUIUtility.singleLineHeight) * 0.5f;

            EditorGUI.BeginChangeCheck();
            bool arraySizeEdited = false;

            int[] visible = _headerState.visibleColumns;
            for (int vi = viFrom; vi < viTo; vi++)
            {
                Rect colRect = _colRectCache[vi];
                if (colRect.xMax < cullXMin)
                    continue;
                if (colRect.x > cullXMax)
                    break; // 열은 왼쪽→오른쪽 순서이므로 이후 열은 전부 뷰포트 밖

                int ci = visible[vi];
                Rect cell = new(
                    colRect.x - xShift + CellPadding, cellTop,
                    colRect.width - CellPadding * 2f, EditorGUIUtility.singleLineHeight);

                if (ci == 0)
                {
                    DrawNameCell(cell, row);
                    continue;
                }

                var info = _columns[ci - 1];

                // 배열이 줄어든 뒤(언두 포함) 캐시된 요소 프로퍼티는 무효일 수 있으므로
                // 존재 판정을 먼저 통과한 경우에만 프로퍼티를 조회/사용한다
                MissingState missing = GetMissingState(row, info);
                var prop = missing == MissingState.Exists ? row.GetProperty(info.propertyPath) : null;
                if (prop == null)
                {
                    // 상위 배열이 짧아 아직 없는 칸: 클릭하면 배열을 늘려 칸을 만든다 (엑셀식 빈 칸 입력)
                    if (missing == MissingState.Creatable)
                    {
                        if (GUI.Button(cell, AddMissingContent, EditorStyles.centeredGreyMiniLabel))
                        {
                            TryCreateMissing(row, info);
                            GUI.changed = true;
                        }
                    }
                    else
                    {
                        GUI.Label(cell, MissingCellContent, EditorStyles.centeredGreyMiniLabel);
                    }
                    continue;
                }

                if (info.propType == SerializedPropertyType.ArraySize)
                {
                    // 크기 변경은 요소 열 수에 영향 → 다음 틱에 열 재구성
                    bool sizeButtons = cell.width >= 76f;
                    Rect sizeRect = sizeButtons
                        ? new Rect(cell.x, cell.y, cell.width - 36f, cell.height)
                        : cell;

                    EditorGUI.BeginChangeCheck();
                    int newSize = EditorGUI.DelayedIntField(sizeRect, prop.intValue);
                    if (EditorGUI.EndChangeCheck())
                    {
                        prop.arraySize = Mathf.Max(0, newSize);
                        arraySizeEdited = true;
                    }

                    if (sizeButtons)
                    {
                        Rect plus = new(sizeRect.xMax + 2f, cell.y, 17f, cell.height);
                        Rect minus = new(plus.xMax, cell.y, 17f, cell.height);
                        // GUI.Button은 GUI.changed를 올리지 않으므로 직접 표시해야 행 단위 Apply가 동작한다
                        if (GUI.Button(plus, "+", EditorStyles.miniButtonLeft))
                        {
                            prop.arraySize++;
                            arraySizeEdited = true;
                            GUI.changed = true;
                        }
                        if (GUI.Button(minus, "−", EditorStyles.miniButtonRight) && prop.arraySize > 0)
                        {
                            prop.arraySize--;
                            arraySizeEdited = true;
                            GUI.changed = true;
                        }
                    }
                    continue;
                }

                DrawCellProperty(cell, info, prop);
            }

            if (EditorGUI.EndChangeCheck())
            {
                so.ApplyModifiedProperties();
                if (arraySizeEdited)
                {
                    // 줄어든 배열의 요소를 가리키던 캐시 프로퍼티가 무효가 되므로 캐시 폐기
                    row.props.Clear();
                    _columnsStale = true;
                }
            }

            EditorGUI.DrawRect(new Rect(0f, rowRect.yMax - 1f, rowWidth, 1f), new Color(0f, 0f, 0f, 0.15f));
        }

        private static readonly GUIContent MissingCellContent = new("—");
        private static readonly GUIContent AddMissingContent =
            new("+", "클릭하면 이 칸이 생기도록 배열 크기를 늘립니다");

        private enum MissingState
        {
            Exists,      // 칸이 존재 → 프로퍼티를 그려도 안전
            Creatable,   // 배열이 짧아 없음 → 클릭으로 생성 가능
            Structural,  // 구조가 달라 배열 확장으로는 만들 수 없음
        }

        /// <summary>
        /// 경로상 배열 구간을 검사해 이 행에 칸이 실제로 존재하는지 판정한다.
        /// 캐시된 요소 프로퍼티는 배열이 줄어들면(언두 포함) 무효가 되므로,
        /// 반드시 이 판정을 통과한 뒤에만 요소 프로퍼티를 사용해야 한다.
        /// 배열 프로퍼티 자체는 경로가 안정적이라 캐시를 써도 안전하다.
        /// </summary>
        private static MissingState GetMissingState(RowEntry row, ColumnInfo col)
        {
            var segments = col.arraySegments;
            if (segments == null)
                return MissingState.Exists; // 배열 경로 아님 → GetProperty null 여부로 판정

            foreach (var (arrayPath, index) in segments)
            {
                var arr = row.GetProperty(arrayPath);
                if (arr == null || !arr.isArray)
                    return MissingState.Structural;
                if (arr.arraySize <= index)
                    return MissingState.Creatable;
            }
            return MissingState.Exists;
        }

        /// <summary>경로상 짧은 배열들을 왼쪽부터 순서대로 늘려 이 열의 칸을 만든다.</summary>
        private static void TryCreateMissing(RowEntry row, ColumnInfo col)
        {
            var segments = col.arraySegments;
            if (segments == null)
                return;

            bool changed = false;
            foreach (var (arrayPath, index) in segments)
            {
                // 앞 구간을 늘린 직후에는 캐시가 낡았을 수 있으므로 캐시를 우회해 조회
                var arr = row.serialized.FindProperty(arrayPath);
                if (arr == null || !arr.isArray)
                    break;
                if (arr.arraySize <= index)
                {
                    arr.arraySize = index + 1;
                    changed = true;
                }
            }

            // null로 캐시된 하위 경로들이 이제 존재하므로 캐시 무효화
            if (changed)
                row.props.Clear();
        }
        private static readonly GUIContent GenericSummaryContent =
            new("{…}", "인스펙터에서 편집하거나 자식 필드/배열 요소 토글을 켜세요");
        private static readonly GUIContent ArraySummaryContent =
            new(string.Empty, "인스펙터에서 편집하거나 자식 필드/배열 요소 토글을 켜세요");

        /// <summary>
        /// 셀 하나 = 프로퍼티 1줄 렌더링.
        /// 열 구성 시 캐시한 메타데이터(fastPath/topCut/summaryOnly)만 사용하므로
        /// 그리기 경로에서 높이 질의(GetPropertyHeight)와 리플렉션이 전혀 없다.
        /// 평탄화되지 않은 복합 타입(자식/배열 토글 꺼짐, 깊이 초과 등)은 요약 라벨로 표시하고,
        /// [Header]/[Space]/[TextArea]가 만드는 위쪽 여백은 클리핑으로 잘라 세로 정렬을 유지한다.
        /// </summary>
        private static void DrawCellProperty(Rect cell, ColumnInfo col, SerializedProperty prop)
        {
            // 단순 1줄 타입: 높이 계산 없이 바로 그림 (셀 대부분이 이 경로)
            if (col.fastPath)
            {
                EditorGUI.PropertyField(cell, prop, GUIContent.none, false);
                return;
            }

            if (col.summaryOnly)
            {
                if (prop.isArray)
                {
                    ArraySummaryContent.text = $"[{prop.arraySize}]";
                    GUI.Label(cell, ArraySummaryContent, EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    GUI.Label(cell, GenericSummaryContent, EditorStyles.centeredGreyMiniLabel);
                }
                return;
            }

            // 커스텀 드로어/데코레이터/[TextArea]: 위쪽 여백을 잘라내고 1줄 높이로 클리핑해 그린다.
            // 드로어에 주는 높이는 (잘라낼 여백 + 1줄)로 고정 — 셀마다 높이를 질의하지 않는다.
            GUI.BeginGroup(cell);
            EditorGUI.PropertyField(
                new Rect(0f, -col.topCut, cell.width, col.topCut + cell.height),
                prop, GUIContent.none, true);
            GUI.EndGroup();
        }

        private void DrawNameCell(Rect cell, RowEntry row)
        {
            // 핑 버튼 + 이름 편집 필드 (Scriptable Sheets처럼 표에서 바로 이름 변경)
            Rect ping = new(cell.x, cell.y, 20f, cell.height);
            if (GUI.Button(ping, new GUIContent("◎", "프로젝트 창에서 선택"), EditorStyles.miniButton))
            {
                EditorGUIUtility.PingObject(row.asset);
                Selection.activeObject = row.asset;
            }

            Rect field = new(ping.xMax + 2f, cell.y, cell.width - 22f, cell.height);
            EditorGUI.BeginChangeCheck();
            string newName = EditorGUI.DelayedTextField(field, row.DisplayName, _nameCellStyle);
            if (EditorGUI.EndChangeCheck() &&
                !string.IsNullOrWhiteSpace(newName) && newName != row.DisplayName)
            {
                string error = AssetDatabase.RenameAsset(row.path, newName);
                if (string.IsNullOrEmpty(error))
                    row.path = AssetDatabase.GetAssetPath(row.asset);
                else
                    Debug.LogWarning($"이름 변경 실패: {error}");
            }
        }

        // ── PropertyHandler 리플렉션 (커스텀 드로어/데코레이터 판별) ─

        private static System.Reflection.MethodInfo s_getHandler;
        private static System.Reflection.PropertyInfo s_hasPropertyDrawer;
        private static System.Reflection.PropertyInfo s_propertyDrawer;
        private static System.Reflection.FieldInfo s_decoratorDrawers;
        private static bool s_reflectionFailed;

        private static object GetHandlerFor(SerializedProperty prop)
        {
            if (s_reflectionFailed)
                return null;

            try
            {
                if (s_getHandler == null)
                {
                    var assembly = typeof(UnityEditor.Editor).Assembly;
                    var utility = assembly.GetType("UnityEditor.ScriptAttributeUtility");
                    s_getHandler = utility?.GetMethod(
                        "GetHandler",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                    var handlerType = assembly.GetType("UnityEditor.PropertyHandler");
                    var flags = System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance;
                    s_hasPropertyDrawer = handlerType?.GetProperty("hasPropertyDrawer", flags);
                    s_propertyDrawer = handlerType?.GetProperty("propertyDrawer", flags);
                    s_decoratorDrawers = handlerType?.GetField("m_DecoratorDrawers", flags);

                    if (s_getHandler == null || (s_hasPropertyDrawer == null && s_propertyDrawer == null))
                    {
                        s_reflectionFailed = true;
                        return null;
                    }
                }
                return s_getHandler.Invoke(null, new object[] { prop });
            }
            catch
            {
                s_reflectionFailed = true;
                return null;
            }
        }

        /// <summary>프로퍼티에 커스텀 PropertyDrawer가 붙어 있는지. 판별 실패 시 false(평탄화 진행).</summary>
        private static bool HasCustomDrawer(SerializedProperty prop)
        {
            object handler = GetHandlerFor(prop);
            if (handler == null)
                return false;

            try
            {
                if (s_hasPropertyDrawer != null)
                    return (bool)s_hasPropertyDrawer.GetValue(handler);
                return s_propertyDrawer.GetValue(handler) != null;
            }
            catch
            {
                s_reflectionFailed = true;
                return false;
            }
        }

        /// <summary>프로퍼티 앞에 붙는 데코레이터([Header]/[Space] 등)의 총 높이.</summary>
        private static float GetDecoratorHeight(SerializedProperty prop)
        {
            object handler = GetHandlerFor(prop);
            if (handler == null || s_decoratorDrawers == null)
                return 0f;

            try
            {
                if (s_decoratorDrawers.GetValue(handler) is not System.Collections.IEnumerable drawers)
                    return 0f;
                float height = 0f;
                foreach (object drawer in drawers)
                {
                    if (drawer is DecoratorDrawer decorator)
                        height += decorator.GetHeight();
                }
                return height;
            }
            catch
            {
                return 0f;
            }
        }

        // ── 열 너비 자동 맞춤 ────────────────────────────────────────

        /// <summary>다음 OnGUI에서 열 너비 자동 맞춤을 수행하도록 예약한다.</summary>
        private void RequestAutoFit(HashSet<string> onlyPaths)
        {
            if (_autoFitPending)
            {
                // 이미 전체 대상 예약이면 유지, 부분+부분이면 합집합
                if (_autoFitOnly != null)
                {
                    if (onlyPaths == null)
                        _autoFitOnly = null;
                    else
                        _autoFitOnly.UnionWith(onlyPaths);
                }
            }
            else
            {
                _autoFitPending = true;
                _autoFitOnly = onlyPaths;
            }
            Repaint();
        }

        /// <summary>
        /// 헤더 텍스트와 셀 내용(문자열/enum/참조는 실제 값 샘플)에 맞춰 열 너비를 조정한다.
        /// onlyPaths가 주어지면 해당 열만 조정한다 (재구성으로 새로 생긴 열 등).
        /// </summary>
        private void AutoFitColumnWidths(HashSet<string> onlyPaths)
        {
            if (_headerState == null || _headerState.columns.Length != _columns.Count + 1)
                return;

            // 에셋 이름 열: 가장 긴 이름에 맞춤 (핑 버튼 여유 포함)
            if (onlyPaths == null)
            {
                float nameWidth = 100f;
                int nameSamples = Mathf.Min(_rows.Count, 200);
                for (int i = 0; i < nameSamples; i++)
                {
                    float w = _nameCellStyle.CalcSize(new GUIContent(_rows[i].DisplayName)).x + 36f;
                    nameWidth = Mathf.Max(nameWidth, w);
                }
                _headerState.columns[0].width = Mathf.Clamp(nameWidth, 100f, 320f);
            }

            for (int c = 0; c < _columns.Count; c++)
            {
                var col = _columns[c];
                if (onlyPaths != null && !onlyPaths.Contains(col.propertyPath))
                    continue;

                var stateCol = _headerState.columns[c + 1];
                // 정렬 화살표 여유분 포함
                float width = EditorStyles.boldLabel.CalcSize(stateCol.headerContent).x + 24f;
                width = Mathf.Max(width, MinContentWidth(col.propType));
                width = Mathf.Max(width, SampleContentWidth(col));
                stateCol.width = Mathf.Clamp(width, 40f, 420f);
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
                case SerializedPropertyType.ArraySize: return 80f; // 크기 필드 + [+][−] 버튼
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
            int samples = Mathf.Min(_rows.Count, 30);
            for (int i = 0; i < samples; i++)
            {
                var so = _rows[i].GetSerialized();
                if (so == null)
                    continue;
                var prop = _rows[i].GetProperty(col.propertyPath);
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
                width = Mathf.Max(width, EditorStyles.label.CalcSize(new GUIContent(text)).x + padding);
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
                case SerializedPropertyType.ArraySize: return 80f;
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

        // ── 스타일 ───────────────────────────────────────────────────

        private void EnsureStyles()
        {
            if (_stylesReady)
                return;

            _nameCellStyle = new GUIStyle(EditorStyles.textField)
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold,
            };
            _centerLabelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
            };
            _stylesReady = true;
        }
    }
}
