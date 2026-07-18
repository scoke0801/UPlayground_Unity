using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Tool.Editor.SOSpreadsheet
{
    /// <summary>프로젝트에서 발견된 SO 타입 하나 (정확히 일치하는 타입 기준으로 그룹핑).</summary>
    internal class TypeEntry
    {
        public Type type;
        public List<string> assetPaths = new();
    }

    /// <summary>테이블의 행 하나 = 에셋 하나. 에셋/SerializedObject는 처음 필요할 때 지연 로드.</summary>
    internal class RowEntry
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
    /// 열 구성 시점에 한 번만 계산해 캐시한다 (셀 바인딩 경로에서 리플렉션 방지).
    /// </summary>
    internal class ColumnInfo
    {
        public string propertyPath;
        public string displayName;
        public SerializedPropertyType propType;
        public bool hasCustomDrawer;
        /// <summary>IMGUI 폴백 드로어에서 잘라낼 위쪽 높이 ([Header]/[Space] 데코레이터 + [TextArea] 라벨 줄).</summary>
        public float topCut;
        /// <summary>셀에 그리지 않고 요약(N Items / {…})만 표시하는 복합 타입 열인지. 클릭 시 상세 패널에서 편집.</summary>
        public bool summaryOnly;
        /// <summary>배열/리스트 열인지 (요약을 "N Items"로 표시).</summary>
        public bool isList;
    }

    /// <summary>
    /// 스프레드시트의 데이터 계층. 스캔 / 열 평탄화 / 필터 / 정렬 / 페이지네이션을 담당하며
    /// UI 프레임워크(IMGUI/UIToolkit)에 의존하지 않는다.
    /// 배열/리스트는 열로 전개하지 않고 "N Items" 요약 열 하나로 표시한다 (편집은 상세 패널).
    /// </summary>
    internal class SOSpreadsheetModel
    {
        /// <summary>전체 열 수 상한.</summary>
        public const int MaxTotalColumns = 300;
        /// <summary>중첩 평탄화 최대 깊이.</summary>
        public const int MaxFlattenDepth = 5;

        public static readonly int[] PageSizes = { 25, 50, 100, 250, 0 }; // 0 = 전체
        public static readonly string[] PageSizeLabels = { "25", "50", "100", "250", "전체" };

        /// <summary>외부 에셋 제외 토글이 켜졌을 때 스캔에서 제외할 경로 접두사 (필요 시 프로젝트별로 수정).</summary>
        public static readonly string[] ExternalPathPrefixes =
        {
            "Assets/ExternalAssets",
            "Assets/Plugins",
            "Assets/TextMesh Pro",
            "Assets/AddressableAssetsData",
            "Assets/Settings",
        };

        // ── 설정 ─────────────────────────────────────────────────────

        public string scopeFolder = "Assets";
        public bool excludeExternal = true;
        public bool showChildren = true;
        public string assetSearch = string.Empty;
        public int pageSizeIndex = 1; // 기본 50
        public int pageIndex;

        /// <summary>정렬 열: -1 = 없음, 0 = 에셋 이름, n = 데이터 열 n-1.</summary>
        public int sortColumnIndex = -1;
        public bool sortAscending = true;

        // ── 상태 ─────────────────────────────────────────────────────

        public List<TypeEntry> types = new();
        public TypeEntry selected;
        public List<RowEntry> rows = new();   // 선택 타입의 전체 행
        public List<RowEntry> view = new();   // 검색/정렬이 적용된 표시용 행
        public List<ColumnInfo> columns = new();
        public bool columnsTruncated;

        // ── 스캔 / 선택 ──────────────────────────────────────────────

        /// <summary>
        /// 범위 폴더 내 모든 SO 에셋을 타입별로 수집한다.
        /// GetMainAssetTypeAtPath는 에셋을 로드하지 않으므로 수천 개여도 부담이 적다.
        /// </summary>
        public void ScanProject()
        {
            string scope = AssetDatabase.IsValidFolder(scopeFolder) ? scopeFolder : "Assets";
            var map = new Dictionary<Type, TypeEntry>();

            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { scope }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (excludeExternal && IsExternalPath(path))
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

            types = map.Values.OrderBy(e => e.type.Name, StringComparer.Ordinal).ToList();
            foreach (var e in types)
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

        /// <summary>타입을 선택하고 행 목록을 초기화한다. 열 구성/필터는 호출측에서 이어서 수행한다.</summary>
        public void SelectType(TypeEntry entry)
        {
            selected = entry;
            rows.Clear();
            view.Clear();
            columns.Clear();
            columnsTruncated = false;
            pageIndex = 0;
            sortColumnIndex = -1;
            sortAscending = true;

            if (entry == null)
                return;

            foreach (string path in entry.assetPaths)
                rows.Add(new RowEntry { path = path });
        }

        // ── 열 구성 (배열 제외 전체 평탄화) ──────────────────────────

        /// <summary>
        /// 선택 타입의 직렬화 필드를 평탄화해 열로 만든다.
        /// 중첩 클래스는 "부모.자식" 열로 전개되고, 배열/리스트는 "N Items" 요약 열 하나가 된다.
        /// </summary>
        public void BuildColumns()
        {
            columns.Clear();
            columnsTruncated = false;

            SerializedObject sample = null;
            foreach (var row in rows)
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
        }

        /// <summary>
        /// 필드 하나를 평탄화해 열로 추가한다.
        /// - 단순 타입 / 1줄 커스텀 드로어 타입 → 열 1개
        /// - 배열/리스트 → 요약 열 1개 (상세 패널에서 편집)
        /// - 중첩 클래스 → 자식들을 "부모.자식" 열로 재귀 전개
        /// </summary>
        private void AddColumnRecursive(SerializedProperty prop, string display, int depth)
        {
            if (columns.Count >= MaxTotalColumns)
            {
                columnsTruncated = true;
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
            if (SOPropertyDrawerUtility.HasCustomDrawer(prop) && SOPropertyDrawerUtility.IsSingleLineDrawn(prop))
            {
                AddLeafColumn(prop, display);
                return;
            }

            // 배열/리스트는 열로 전개하지 않는다 → "N Items" 요약 열 (클릭 시 상세 패널)
            if (prop.isArray)
            {
                AddLeafColumn(prop, display);
                return;
            }

            if (depth >= MaxFlattenDepth || !showChildren)
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
            bool customDrawer = SOPropertyDrawerUtility.HasCustomDrawer(prop);
            float topCut = SOPropertyDrawerUtility.ComputeTopCut(prop, customDrawer);
            bool generic = prop.propertyType == SerializedPropertyType.Generic;

            // 복합 타입 잎 열은 1줄 커스텀 드로어가 있을 때만 셀에 그리고, 나머지는 요약 표시
            // (리스트·클래스를 셀에 통째로 그리면 느리고 깨진다 → 상세 패널에서 편집)
            bool summaryOnly = generic && !(customDrawer && SOPropertyDrawerUtility.IsSingleLineDrawn(prop));

            columns.Add(new ColumnInfo
            {
                propertyPath = prop.propertyPath,
                displayName = display,
                propType = prop.propertyType,
                hasCustomDrawer = customDrawer,
                topCut = topCut,
                summaryOnly = summaryOnly,
                isList = generic && prop.isArray,
            });
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

        public void ApplyFilter()
        {
            view = string.IsNullOrEmpty(assetSearch)
                ? new List<RowEntry>(rows)
                : rows.Where(r => r.DisplayName.IndexOf(assetSearch, StringComparison.OrdinalIgnoreCase) >= 0)
                      .ToList();
            ApplySort();
            pageIndex = Mathf.Clamp(pageIndex, 0, Mathf.Max(0, PageCount - 1));
        }

        public int PageSize => PageSizes[Mathf.Clamp(pageSizeIndex, 0, PageSizes.Length - 1)];

        public int PageCount
        {
            get
            {
                if (PageSize <= 0 || view.Count == 0)
                    return 1;
                return (view.Count + PageSize - 1) / PageSize;
            }
        }

        public int PageStart => PageSize <= 0 ? 0 : pageIndex * PageSize;

        public int PageRowCount => PageSize <= 0
            ? view.Count
            : Mathf.Min(PageSize, view.Count - PageStart);

        private void ApplySort()
        {
            if (sortColumnIndex < 0 || sortColumnIndex > columns.Count)
                return;

            if (sortColumnIndex == 0)
            {
                view.Sort((a, b) =>
                    string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                // 정렬 시점에만 전 행의 키를 한 번 추출 (여기서 미로드 에셋이 로드됨)
                string propPath = columns[sortColumnIndex - 1].propertyPath;
                var keys = new Dictionary<RowEntry, object>();
                foreach (var row in view)
                {
                    var so = row.GetSerialized();
                    so?.UpdateIfRequiredOrScript();
                    keys[row] = so != null ? GetSortKey(row.GetProperty(propPath)) : null;
                }
                view.Sort((a, b) => CompareKeys(keys[a], keys[b]));
            }

            if (!sortAscending)
                view.Reverse();
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

        public static bool IsSortable(SerializedPropertyType t)
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
                case SerializedPropertyType.Generic:
                    return true;
                default:
                    return false;
            }
        }
    }
}
