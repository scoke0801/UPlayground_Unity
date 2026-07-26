using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.FlowGraph.Editor
{
    /// <summary>FlowNode 타입의 메뉴 경로/카테고리/컬러를 조회하는 에디터 공용 카탈로그.</summary>
    public static class FlowNodeCatalog
    {
        // 시안 기준 카테고리 팔레트 — 진입점=녹색, 흐름 제어=보라, 액션류=파랑, 이벤트=주황
        private static readonly Dictionary<string, Color> CategoryColors = new()
        {
            { "진입점", new Color(0.15f, 0.42f, 0.23f) },
            { "코어", new Color(0.36f, 0.29f, 0.52f) },
            { "플래그", new Color(0.10f, 0.42f, 0.38f) },
            { "대화", new Color(0.16f, 0.36f, 0.55f) },
            { "퀘스트", new Color(0.29f, 0.30f, 0.58f) },
            { "스토리", new Color(0.20f, 0.33f, 0.36f) },
            { "이벤트", new Color(0.60f, 0.38f, 0.12f) },
            { "변수", new Color(0.52f, 0.20f, 0.36f) },
            { "트리거 브릿지", new Color(0.45f, 0.30f, 0.18f) },
        };

        private static readonly Color DefaultColor = new(0.25f, 0.25f, 0.28f);

        // 카테고리 기본 아이콘 (Unity 빌트인 이름 — 해석 실패 시 조용히 아이콘 없음)
        private static readonly Dictionary<string, string> CategoryIconNames = new()
        {
            { "진입점", "PlayButton" },
            { "코어", "cs Script Icon" },
            { "플래그", "FilterByLabel" },
            { "대화", "console.infoicon" },
            { "퀘스트", "Favorite" },
            { "스토리", "TextAsset Icon" },
            { "이벤트", "console.warnicon" },
            { "변수", "ScriptableObject Icon" },
            { "트리거 브릿지", "Prefab Icon" },
        };

        private static readonly Dictionary<string, Color> ExternalCategoryColors = new();
        private static readonly Dictionary<System.Type, Texture2D> IconCache = new();
        private static readonly Dictionary<string, Texture2D> IconByNameCache = new();
        private static bool _externalStylesLoaded;

        /// <summary>
        /// 외부 asmdef의 [assembly: FlowNodeCategoryStyle] 등록을 1회 수집한다.
        /// 외부 등록이 내장 팔레트보다 우선한다.
        /// </summary>
        private static void EnsureExternalStyles()
        {
            if (_externalStylesLoaded)
                return;
            _externalStylesLoaded = true;

            foreach (Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                object[] attributes;
                try
                {
                    attributes = assembly.GetCustomAttributes(typeof(FlowNodeCategoryStyleAttribute), false);
                }
                catch
                {
                    continue; // 리플렉션 불가 어셈블리는 무시
                }

                foreach (FlowNodeCategoryStyleAttribute attr in attributes)
                {
                    if (string.IsNullOrEmpty(attr.Category))
                        continue;
                    if (ColorUtility.TryParseHtmlString(attr.HeaderColor, out Color color))
                        ExternalCategoryColors[attr.Category] = color;
                    if (!string.IsNullOrEmpty(attr.Icon))
                        CategoryIconNames[attr.Category] = attr.Icon;
                }
            }
        }

        public static string GetMenuPath(Type nodeType)
        {
            var menu = nodeType.GetCustomAttribute<FlowNodeMenuAttribute>();
            return menu?.Path ?? $"기타/{nodeType.Name}";
        }

        public static string GetCategory(Type nodeType)
        {
            string path = GetMenuPath(nodeType);
            int slash = path.IndexOf('/');
            return slash > 0 ? path.Substring(0, slash) : "기타";
        }

        public static string GetLabel(Type nodeType)
        {
            string path = GetMenuPath(nodeType);
            int slash = path.LastIndexOf('/');
            return slash >= 0 ? path.Substring(slash + 1) : path;
        }

        public static string GetSummary(Type nodeType)
        {
            return nodeType.GetCustomAttribute<FlowNodeMenuAttribute>()?.Summary ?? string.Empty;
        }

        public static IReadOnlyList<string> GetKeywords(Type nodeType)
        {
            return nodeType.GetCustomAttribute<FlowNodeMenuAttribute>()?.Keywords
                   ?? Array.Empty<string>();
        }

        public static string GetSearchLabel(Type nodeType)
        {
            string label = GetLabel(nodeType);
            string summary = GetSummary(nodeType);
            IReadOnlyList<string> keywords = GetKeywords(nodeType);
            string suffix = string.Join(" · ", new[]
            {
                summary,
                keywords.Count > 0 ? string.Join(" ", keywords) : string.Empty,
                nodeType.Name,
            }.Where(value => !string.IsNullOrWhiteSpace(value)));
            return string.IsNullOrEmpty(suffix) ? label : $"{label} — {suffix}";
        }

        public static Color GetCategoryColor(Type nodeType)
        {
            EnsureExternalStyles();

            // 1) 노드 타입 스타일 (파생 포함) → 2) 외부 카테고리 등록 → 3) 내장 팔레트
            var style = nodeType.GetCustomAttribute<FlowNodeStyleAttribute>(inherit: true);
            if (style?.HeaderColor != null
                && ColorUtility.TryParseHtmlString(style.HeaderColor, out Color typeColor))
            {
                return typeColor;
            }

            string category = GetCategory(nodeType);
            if (ExternalCategoryColors.TryGetValue(category, out Color external))
                return external;
            return CategoryColors.TryGetValue(category, out Color color) ? color : DefaultColor;
        }

        /// <summary>
        /// 노드 타입 아이콘. [FlowNodeStyle(Icon=...)] → 카테고리 기본 아이콘 순.
        /// 빌트인 아이콘 이름 또는 프로젝트 텍스처 경로를 지원하며, 해석 실패 시 null(아이콘 없음).
        /// </summary>
        public static Texture2D GetIcon(Type nodeType)
        {
            if (IconCache.TryGetValue(nodeType, out Texture2D cached))
                return cached;

            EnsureExternalStyles();

            var style = nodeType.GetCustomAttribute<FlowNodeStyleAttribute>(inherit: true);
            string iconName = style?.Icon;
            if (string.IsNullOrEmpty(iconName))
                CategoryIconNames.TryGetValue(GetCategory(nodeType), out iconName);

            Texture2D icon = ResolveIconTexture(iconName);
            IconCache[nodeType] = icon;
            return icon;
        }

        private static Texture2D ResolveIconTexture(string iconName)
        {
            if (string.IsNullOrEmpty(iconName))
                return null;
            if (IconByNameCache.TryGetValue(iconName, out Texture2D cached))
                return cached;

            Texture2D icon = null;
            try
            {
                if (iconName.IndexOf('/') >= 0)
                {
                    icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconName);
                }
                else
                {
                    icon = EditorGUIUtility.FindTexture(iconName);
                    if (icon == null)
                        icon = EditorGUIUtility.IconContent(iconName)?.image as Texture2D;
                }
            }
            catch
            {
                icon = null; // 알 수 없는 아이콘 이름은 조용히 무시
            }

            IconByNameCache[iconName] = icon;
            return icon;
        }

        /// <summary>생성 가능한(비추상) 노드 타입을 카테고리별로 정렬해 반환한다.</summary>
        public static SortedDictionary<string, List<Type>> GetNodeTypesByCategory()
        {
            var result = new SortedDictionary<string, List<Type>>();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<FlowNode>())
            {
                if (type.IsAbstract)
                    continue;

                string category = GetCategory(type);
                if (!result.TryGetValue(category, out List<Type> list))
                {
                    list = new List<Type>();
                    result[category] = list;
                }
                list.Add(type);
            }

            foreach (List<Type> list in result.Values)
                list.Sort((a, b) => string.CompareOrdinal(GetLabel(a), GetLabel(b)));
            return result;
        }

        public static List<Type> GetNodeTypes()
        {
            var result = new List<Type>();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<FlowNode>())
            {
                if (!type.IsAbstract)
                    result.Add(type);
            }
            result.Sort((a, b) => string.CompareOrdinal(GetMenuPath(a), GetMenuPath(b)));
            return result;
        }

        public static bool HasCompatiblePort(Type nodeType, FlowPortDef origin)
        {
            FlowNode node;
            try
            {
                node = Activator.CreateInstance(nodeType) as FlowNode;
            }
            catch
            {
                return false;
            }
            if (node == null)
                return false;

            foreach (FlowPortDef candidate in node.Ports)
            {
                if (FlowPortDef.AreCompatible(origin, candidate))
                    return true;
            }
            return false;
        }

        public static bool CanBridge(
            Type nodeType,
            FlowPortDef upstreamOutput,
            FlowPortDef downstreamInput)
        {
            FlowNode node;
            try
            {
                node = Activator.CreateInstance(nodeType) as FlowNode;
            }
            catch
            {
                return false;
            }
            if (node == null)
                return false;

            bool hasInput = false;
            bool hasOutput = false;
            foreach (FlowPortDef candidate in node.Ports)
            {
                hasInput |= candidate.Direction == FlowPortDirection.Input
                            && FlowPortDef.AreCompatible(upstreamOutput, candidate);
                hasOutput |= candidate.Direction == FlowPortDirection.Output
                             && FlowPortDef.AreCompatible(candidate, downstreamInput);
            }
            return hasInput && hasOutput;
        }
    }

    /// <summary>즐겨찾기와 최근 사용 노드를 사용자별 EditorPrefs에 저장한다.</summary>
    internal static class FlowNodeUsageStore
    {
        private const string FavoritesKey = "UPlayGround.FlowGraph.FavoriteNodes";
        private const string RecentsKey = "UPlayGround.FlowGraph.RecentNodes";
        private const int MaxRecentCount = 8;

        public static bool IsFavorite(Type type)
        {
            return ReadTypeNames(FavoritesKey).Contains(type.AssemblyQualifiedName);
        }

        public static void ToggleFavorite(Type type)
        {
            HashSet<string> names = ReadTypeNames(FavoritesKey);
            if (!names.Add(type.AssemblyQualifiedName))
                names.Remove(type.AssemblyQualifiedName);
            WriteTypeNames(FavoritesKey, names);
        }

        public static void RecordRecent(Type type)
        {
            var names = new List<string>(ReadOrderedTypeNames(RecentsKey));
            names.Remove(type.AssemblyQualifiedName);
            names.Insert(0, type.AssemblyQualifiedName);
            if (names.Count > MaxRecentCount)
                names.RemoveRange(MaxRecentCount, names.Count - MaxRecentCount);
            EditorPrefs.SetString(RecentsKey, string.Join("\n", names));
        }

        public static List<Type> GetFavorites()
        {
            return ResolveTypes(ReadTypeNames(FavoritesKey));
        }

        public static List<Type> GetRecents()
        {
            return ResolveTypes(ReadOrderedTypeNames(RecentsKey));
        }

        private static HashSet<string> ReadTypeNames(string key)
        {
            return new HashSet<string>(
                ReadOrderedTypeNames(key),
                StringComparer.Ordinal);
        }

        private static IEnumerable<string> ReadOrderedTypeNames(string key)
        {
            return (EditorPrefs.GetString(key, string.Empty) ?? string.Empty)
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static void WriteTypeNames(string key, IEnumerable<string> names)
        {
            EditorPrefs.SetString(
                key,
                string.Join("\n", names.OrderBy(value => value, StringComparer.Ordinal)));
        }

        private static List<Type> ResolveTypes(IEnumerable<string> names)
        {
            var result = new List<Type>();
            foreach (string name in names)
            {
                Type type = Type.GetType(name);
                if (type != null && typeof(FlowNode).IsAssignableFrom(type) && !type.IsAbstract)
                    result.Add(type);
            }
            return result;
        }
    }
}
