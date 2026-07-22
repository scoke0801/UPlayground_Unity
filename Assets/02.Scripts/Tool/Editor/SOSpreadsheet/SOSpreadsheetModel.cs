using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
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
        /// <summary>마지막으로 UpdateIfRequiredOrScript를 수행한 갱신 패스 (셀마다 중복 호출 방지).</summary>
        public int lastUpdatePass = -1;

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
        /// <summary>ObjectReference 열의 참조 타입 (ObjectField 픽커 제한용, 리플렉션 실패 시 null).</summary>
        public Type objectType;
        /// <summary>Enum 열의 enum 타입 (EnumField 생성/플래그 필터용, 리플렉션 실패 시 null).</summary>
        public Type enumType;
        /// <summary>[Flags] enum 열인지 (필터를 비트 교집합으로 판정).</summary>
        public bool isFlagsEnum;
        /// <summary>Enum 열의 원시 이름 목록 (필터 저장 키).</summary>
        public string[] enumNames;
        /// <summary>Enum 열의 표시 이름 목록 (enumNames와 병렬).</summary>
        public string[] enumDisplayNames;
        /// <summary>Sprite/Texture를 썸네일로 표시할 Icon 계열 ObjectReference 열인지.</summary>
        public bool isIcon;
    }

    internal enum ColumnFilterOperator
    {
        Default,
        Contains,
        StartsWith,
        Equals,
        NotEquals,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual,
        Range,
        HasValue,
        IsEmpty,
        HasAny,
        HasAll,
        HasNone,
    }

    /// <summary>
    /// 열 하나에 걸린 값 필터. EditorWindow에 직렬화되어 도메인 리로드에도 유지된다.
    /// 조건이 비어 있으면(IsActive=false) 전체 통과로 취급한다.
    /// </summary>
    [Serializable]
    internal class ColumnFilter
    {
        public string propertyPath;
        /// <summary>문자열/참조 열: 포함 텍스트. 숫자/리스트 열: 비교식("10", ">=10", "&lt;5", "3..8").</summary>
        public string text = string.Empty;
        /// <summary>Range 연산자의 최댓값.</summary>
        public string secondText = string.Empty;
        public ColumnFilterOperator filterOperator;
        /// <summary>enum/bool 열: 허용할 값 목록 (enum은 원시 이름). 비어 있으면 전체 허용.</summary>
        public List<string> allowed = new();

        public bool IsActive => !string.IsNullOrWhiteSpace(text) || allowed.Count > 0 ||
                                filterOperator == ColumnFilterOperator.HasValue ||
                                filterOperator == ColumnFilterOperator.IsEmpty;
    }

    /// <summary>숫자 필터 비교식 파서. 잘못된 식은 valid=false → 전체 통과.</summary>
    internal struct NumericRange
    {
        public bool valid;
        public double lo, hi;
        public bool loExclusive, hiExclusive;

        public bool Contains(double v)
        {
            if (loExclusive ? v <= lo : v < lo) return false;
            if (hiExclusive ? v >= hi : v > hi) return false;
            return true;
        }

        public static NumericRange Parse(string text)
        {
            var range = new NumericRange { lo = double.NegativeInfinity, hi = double.PositiveInfinity };
            text = text?.Trim();
            if (string.IsNullOrEmpty(text))
                return range;

            int dots = text.IndexOf("..", StringComparison.Ordinal);
            if (dots >= 0)
            {
                range.valid = TryParse(text.Substring(0, dots), out range.lo)
                              & TryParse(text.Substring(dots + 2), out range.hi);
                if (!range.valid)
                {
                    range.lo = double.NegativeInfinity;
                    range.hi = double.PositiveInfinity;
                }
                return range;
            }

            if (text.StartsWith(">=", StringComparison.Ordinal))
                range.valid = TryParse(text.Substring(2), out range.lo);
            else if (text.StartsWith("<=", StringComparison.Ordinal))
                range.valid = TryParse(text.Substring(2), out range.hi);
            else if (text.StartsWith(">", StringComparison.Ordinal))
            {
                range.valid = TryParse(text.Substring(1), out range.lo);
                range.loExclusive = true;
            }
            else if (text.StartsWith("<", StringComparison.Ordinal))
            {
                range.valid = TryParse(text.Substring(1), out range.hi);
                range.hiExclusive = true;
            }
            else
            {
                string body = text.StartsWith("=", StringComparison.Ordinal) ? text.Substring(1) : text;
                if (TryParse(body, out double v))
                {
                    range.valid = true;
                    range.lo = range.hi = v;
                }
            }

            if (!range.valid)
            {
                range.lo = double.NegativeInfinity;
                range.hi = double.PositiveInfinity;
                range.loExclusive = range.hiExclusive = false;
            }
            return range;
        }

        private static bool TryParse(string s, out double v)
        {
            return double.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v);
        }
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
        /// <summary>검색 범위. __name, __path, __all 또는 특정 propertyPath.</summary>
        public string searchColumnPath = "__name";
        /// <summary>활성 열 필터 목록 (창이 소유·직렬화하는 리스트를 공유).</summary>
        public List<ColumnFilter> filters = new();
        public int pageSizeIndex = 1; // 기본 50
        public int pageIndex;

        /// <summary>정렬 열: -1 = 없음, 0 = 에셋 이름, n = 데이터 열 n-1.</summary>
        public int sortColumnIndex = -1;
        public bool sortAscending = true;
        /// <summary>행 그룹화 열. enum/bool/string 열만 사용한다.</summary>
        public string groupPropertyPath = string.Empty;

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

                    // 타입에서 제거되거나 이름이 바뀐 필드의 YAML 데이터는 에셋을 다시 저장하기 전까지
                    // SerializedObject 반복자에 남아 있을 수 있다. 기본 Inspector처럼 현재 선언된 필드만
                    // 열로 만들지 않으면 마이그레이션 전 데이터가 유령 열로 함께 노출된다.
                    if (ResolveFieldType(selected?.type, it.propertyPath) == null)
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

            // 참조/enum 열은 필드 타입을 한 번 리플렉션해 둔다 (ObjectField 픽커 제한, EnumField 생성, 플래그 필터)
            Type fieldType = null;
            if (prop.propertyType == SerializedPropertyType.ObjectReference ||
                prop.propertyType == SerializedPropertyType.Enum)
                fieldType = ResolveFieldType(selected?.type, prop.propertyPath);

            Type objectType = null;
            Type enumType = null;
            bool isFlagsEnum = false;
            string[] enumNames = null;
            string[] enumDisplayNames = null;
            if (prop.propertyType == SerializedPropertyType.ObjectReference &&
                fieldType != null && typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
            {
                objectType = fieldType;
            }
            else if (prop.propertyType == SerializedPropertyType.Enum)
            {
                if (fieldType is { IsEnum: true })
                {
                    enumType = fieldType;
                    isFlagsEnum = fieldType.IsDefined(typeof(FlagsAttribute), false);
                }
                enumNames = prop.enumNames;
                enumDisplayNames = prop.enumDisplayNames;
            }

            columns.Add(new ColumnInfo
            {
                propertyPath = prop.propertyPath,
                displayName = display,
                propType = prop.propertyType,
                hasCustomDrawer = customDrawer,
                topCut = topCut,
                summaryOnly = summaryOnly,
                isList = generic && prop.isArray,
                objectType = objectType,
                enumType = enumType,
                isFlagsEnum = isFlagsEnum,
                enumNames = enumNames,
                enumDisplayNames = enumDisplayNames,
                isIcon = IsIconField(prop, objectType),
            });
        }

        private static bool IsIconField(SerializedProperty prop, Type objectType)
        {
            if (prop.propertyType != SerializedPropertyType.ObjectReference)
                return false;

            string fieldName = prop.name ?? string.Empty;
            bool iconName = fieldName.IndexOf("icon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            fieldName.IndexOf("아이콘", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!iconName)
                return false;

            return objectType == null || typeof(Sprite).IsAssignableFrom(objectType) ||
                   typeof(Texture).IsAssignableFrom(objectType);
        }

        /// <summary>
        /// SO 타입에서 propertyPath("부모.자식" 꼴, 배열 구간 없음)를 따라 필드 타입을 찾는다.
        /// private 필드와 상속 필드를 모두 뒤지고, 실패하면 null (호출측이 폴백 처리).
        /// </summary>
        private static Type ResolveFieldType(Type ownerType, string propertyPath)
        {
            if (ownerType == null)
                return null;

            Type current = ownerType;
            foreach (string part in propertyPath.Split('.'))
            {
                FieldInfo field = null;
                for (Type t = current; t != null && field == null; t = t.BaseType)
                {
                    field = t.GetField(part,
                        BindingFlags.Instance | BindingFlags.Public |
                        BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                }
                if (field == null)
                    return null;
                current = field.FieldType;
            }
            return current;
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
            // 활성 필터의 열/비교식은 행 루프 밖에서 한 번만 해석
            var active = new List<(ColumnFilter filter, ColumnInfo column, NumericRange range)>();
            foreach (var f in filters)
            {
                if (!f.IsActive)
                    continue;
                var column = FindColumn(f.propertyPath);
                if (column == null)
                    continue;
                active.Add((f, column, NumericRange.Parse(f.text)));
            }

            bool hasSearch = !string.IsNullOrEmpty(assetSearch);
            view = rows.Where(r => MatchesSearch(r, hasSearch) && MatchesFilters(r, active)).ToList();
            ApplySort();
            ApplyGrouping();
            pageIndex = Mathf.Clamp(pageIndex, 0, Mathf.Max(0, PageCount - 1));
        }

        public ColumnInfo FindColumn(string propertyPath)
        {
            foreach (var c in columns)
            {
                if (c.propertyPath == propertyPath)
                    return c;
            }
            return null;
        }

        private bool MatchesSearch(RowEntry row, bool hasSearch)
        {
            if (!hasSearch)
                return true;

            string term = assetSearch.Trim();
            if (searchColumnPath == "__name" || string.IsNullOrEmpty(searchColumnPath))
                return row.DisplayName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
            if (searchColumnPath == "__path")
                return row.path.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;

            // 값 검색은 에셋 로드가 필요하다 (로드 후에는 캐시됨).
            var so = row.GetSerialized();
            if (so == null)
                return false;

            if (searchColumnPath != "__all")
            {
                var column = FindColumn(searchColumnPath);
                if (column == null)
                    return false;
                string value = GetValueText(column, row.GetProperty(column.propertyPath));
                return !string.IsNullOrEmpty(value) &&
                       value.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            if (row.DisplayName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            foreach (var column in columns)
            {
                string value = GetValueText(column, row.GetProperty(column.propertyPath));
                if (!string.IsNullOrEmpty(value) &&
                    value.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private static bool MatchesFilters(
            RowEntry row, List<(ColumnFilter filter, ColumnInfo column, NumericRange range)> active)
        {
            if (active.Count == 0)
                return true;
            var so = row.GetSerialized();
            if (so == null)
                return false;

            foreach (var (filter, column, range) in active)
            {
                if (!FilterMatches(filter, column, range, row.GetProperty(column.propertyPath)))
                    return false;
            }
            return true;
        }

        private static bool FilterMatches(
            ColumnFilter filter, ColumnInfo column, NumericRange range, SerializedProperty p)
        {
            if (p == null)
                return false;

            switch (column.propType)
            {
                case SerializedPropertyType.Boolean:
                    return filter.allowed.Count == 0 ||
                           filter.allowed.Contains(p.boolValue ? "True" : "False");
                case SerializedPropertyType.Enum:
                    return EnumMatches(filter, column, p);
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.Character:
                    return NumericMatches(filter, range, p.longValue);
                case SerializedPropertyType.Float:
                    return NumericMatches(filter, range, p.doubleValue);
                case SerializedPropertyType.String:
                    return TextMatches(filter, p.stringValue ?? string.Empty);
                case SerializedPropertyType.ObjectReference:
                {
                    if (filter.filterOperator == ColumnFilterOperator.HasValue)
                        return p.objectReferenceValue != null;
                    if (filter.filterOperator == ColumnFilterOperator.IsEmpty)
                        return p.objectReferenceValue == null;
                    string name = p.objectReferenceValue != null ? p.objectReferenceValue.name : "None";
                    return TextMatches(filter, name);
                }
                default:
                    // 리스트 요약 열은 요소 수로 숫자 필터
                    if (column.isList || p.isArray)
                        return NumericMatches(filter, range, p.arraySize);
                    return true;
            }
        }

        private static bool NumericMatches(ColumnFilter filter, NumericRange parsed, double value)
        {
            if (string.IsNullOrWhiteSpace(filter.text))
                return true;
            if (!double.TryParse(filter.text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture,
                    out double first))
                return !parsed.valid || parsed.Contains(value);

            switch (filter.filterOperator)
            {
                case ColumnFilterOperator.NotEquals: return !Mathf.Approximately((float)value, (float)first);
                case ColumnFilterOperator.Greater: return value > first;
                case ColumnFilterOperator.GreaterOrEqual: return value >= first;
                case ColumnFilterOperator.Less: return value < first;
                case ColumnFilterOperator.LessOrEqual: return value <= first;
                case ColumnFilterOperator.Range:
                    if (!double.TryParse(filter.secondText.Trim(), NumberStyles.Float,
                            CultureInfo.InvariantCulture, out double second))
                        return true;
                    if (first > second) (first, second) = (second, first);
                    return value >= first && value <= second;
                case ColumnFilterOperator.Equals:
                    return Math.Abs(value - first) <= double.Epsilon;
                default:
                    return !parsed.valid || parsed.Contains(value);
            }
        }

        private static bool TextMatches(ColumnFilter filter, string value)
        {
            if (string.IsNullOrWhiteSpace(filter.text))
                return true;
            string term = filter.text.Trim();
            switch (filter.filterOperator)
            {
                case ColumnFilterOperator.StartsWith:
                    return value.StartsWith(term, StringComparison.OrdinalIgnoreCase);
                case ColumnFilterOperator.Equals:
                    return string.Equals(value, term, StringComparison.OrdinalIgnoreCase);
                case ColumnFilterOperator.NotEquals:
                    return !string.Equals(value, term, StringComparison.OrdinalIgnoreCase);
                default:
                    return value.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        private static bool EnumMatches(ColumnFilter filter, ColumnInfo column, SerializedProperty p)
        {
            if (filter.allowed.Count == 0)
                return true;

            // [Flags] enum은 선택 모드에 따라 비트 조건을 적용한다.
            if (column.isFlagsEnum && column.enumType != null)
            {
                long value = p.longValue;
                long selected = 0;
                foreach (string name in filter.allowed)
                {
                    long bits;
                    try
                    {
                        bits = Convert.ToInt64(Enum.Parse(column.enumType, name), CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        continue;
                    }
                    selected |= bits;
                }
                switch (filter.filterOperator)
                {
                    case ColumnFilterOperator.HasAll: return (value & selected) == selected;
                    case ColumnFilterOperator.HasNone: return (value & selected) == 0;
                    case ColumnFilterOperator.Equals: return value == selected;
                    default: return selected == 0 ? value == 0 : (value & selected) != 0;
                }
            }

            int idx = p.enumValueIndex;
            string raw = column.enumNames != null && idx >= 0 && idx < column.enumNames.Length
                ? column.enumNames[idx]
                : p.intValue.ToString();
            bool contains = filter.allowed.Contains(raw);
            return filter.filterOperator == ColumnFilterOperator.NotEquals ? !contains : contains;
        }

        public static bool IsGroupable(ColumnInfo column)
        {
            return column != null && (column.propType == SerializedPropertyType.Enum ||
                                      column.propType == SerializedPropertyType.Boolean ||
                                      column.propType == SerializedPropertyType.String);
        }

        public string GetGroupKey(RowEntry row)
        {
            var column = FindColumn(groupPropertyPath);
            if (!IsGroupable(column))
                return string.Empty;
            var so = row.GetSerialized();
            if (so == null)
                return "(로드 실패)";
            string value = GetValueText(column, row.GetProperty(column.propertyPath));
            return string.IsNullOrEmpty(value) ? "(비어 있음)" : value;
        }

        private void ApplyGrouping()
        {
            var column = FindColumn(groupPropertyPath);
            if (!IsGroupable(column))
                return;
            // OrderBy는 안정 정렬이므로 그룹 안에서는 기존 사용자 정렬 순서를 보존한다.
            view = view.OrderBy(GetGroupKey, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>값 검색용 셀 텍스트. 검색 의미가 없는 타입(벡터/색상 등)은 빈 문자열.</summary>
        public static string GetValueText(ColumnInfo column, SerializedProperty p)
        {
            if (p == null)
                return string.Empty;

            switch (column.propType)
            {
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.Character:
                    return p.longValue.ToString();
                case SerializedPropertyType.Float:
                    return p.doubleValue.ToString("0.####", CultureInfo.InvariantCulture);
                case SerializedPropertyType.Boolean:
                    return p.boolValue ? "True" : "False";
                case SerializedPropertyType.String:
                    return p.stringValue;
                case SerializedPropertyType.Enum:
                {
                    int idx = p.enumValueIndex;
                    var names = column.enumDisplayNames;
                    return names != null && idx >= 0 && idx < names.Length
                        ? names[idx]
                        : p.intValue.ToString();
                }
                case SerializedPropertyType.ObjectReference:
                    return p.objectReferenceValue != null ? p.objectReferenceValue.name : string.Empty;
                default:
                    return string.Empty;
            }
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
