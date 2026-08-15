using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Data.Editor
{
    /// <summary>
    /// CameraSettings 전용 커스텀 인스펙터.
    ///
    /// 50여 개의 플랫 필드를 [Header] 단위로 자동 묶어 탭/섹션으로 재배치한다.
    /// 섹션 구성은 리플렉션으로 [Header]를 읽어 만들기 때문에, CameraSettings에
    /// 필드를 추가/이동해도 에디터 코드를 손댈 필요가 없다(단일 소스).
    ///
    /// 제공 기능
    ///  - 탭 분류(기본 / 충돌·FOV / 이동·연출 / 락온 / 군중·전투)
    ///  - 한국어 헤더·툴팁 기준 검색 필터
    ///  - 카메라 불변식 실시간 검증(거리/각도 범위 등) → 상단 경고
    ///  - enable/use 불리언이 거짓이면 하위 필드 자동 비활성화(게이팅)
    ///  - 거리 관계(min·default·combat·max) 미니 바 시각화
    /// </summary>
    [CustomEditor(typeof(CameraSettings))]
    [CanEditMultipleObjects]
    public class CameraSettingsEditor : UnityEditor.Editor
    {
        // ── 탭 정의 ───────────────────────────────────────────────
        private static readonly (string name, Color color)[] _tabs =
        {
            ("기본",      new Color(0.30f, 0.55f, 0.90f)),
            ("충돌·FOV",  new Color(0.85f, 0.45f, 0.25f)),
            ("이동·연출", new Color(0.30f, 0.75f, 0.45f)),
            ("락온",      new Color(0.85f, 0.30f, 0.40f)),
            ("군중·전투", new Color(0.70f, 0.45f, 0.90f)),
        };

        // ── 섹션 모델 ─────────────────────────────────────────────
        private class Section
        {
            public string headerRaw;
            public string title;
            public int tab;
            public readonly List<FieldInfo> fields = new();
        }

        private readonly List<Section> _sections = new();
        private readonly Dictionary<string, string> _tooltips = new();   // fieldName → tooltip
        private readonly Dictionary<string, Section> _sectionByField = new();

        private int _activeTab;
        private string _search = string.Empty;
        private const string TabPrefKey = "UPlayGround.CameraSettingsEditor.Tab";

        private GUIStyle _headerStyle;
        private bool _stylesReady;

        // ── 라이프사이클 ──────────────────────────────────────────
        private void OnEnable()
        {
            BuildSections();
            _activeTab = SessionState.GetInt(TabPrefKey, 0);
        }

        private void BuildSections()
        {
            _sections.Clear();
            _tooltips.Clear();
            _sectionByField.Clear();

            // MetadataToken 정렬로 선언 순서를 보장한다(GetFields 순서는 규약상 비보장).
            var fields = typeof(CameraSettings)
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => !f.IsNotSerialized)
                .OrderBy(f => f.MetadataToken)
                .ToArray();

            Section current = null;
            foreach (var f in fields)
            {
                var header = f.GetCustomAttribute<HeaderAttribute>();
                if (header != null)
                {
                    current = new Section
                    {
                        headerRaw = header.header,
                        title = CleanHeader(header.header),
                        tab = TabFor(header.header),
                    };
                    _sections.Add(current);
                }

                if (current == null)
                {
                    // [Header] 이전 필드(현재 데이터엔 없음) 대비 안전망.
                    current = new Section { headerRaw = "기타", title = "기타", tab = 0 };
                    _sections.Add(current);
                }

                current.fields.Add(f);
                _sectionByField[f.Name] = current;

                var tip = f.GetCustomAttribute<TooltipAttribute>();
                _tooltips[f.Name] = tip != null ? tip.tooltip : string.Empty;
            }
        }

        /// <summary>헤더 텍스트로 소속 탭을 결정한다(키워드 기반, 락온 우선).</summary>
        private static int TabFor(string raw)
        {
            if (raw.Contains("락온")) return 3;
            if (raw.Contains("충돌") || raw.Contains("FOV")) return 1;
            if (raw.Contains("Look-ahead") || raw.Contains("탐색") || raw.Contains("정렬") || raw.Contains("리센터링")) return 2;
            if (raw.Contains("다수 적") || raw.Contains("몬스터") || raw.Contains("전투")) return 4;
            return 0;
        }

        private static string CleanHeader(string raw)
            => raw.Trim().Trim('=', ' ').Trim();

        private void InitStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(8, 0, 2, 2),
            };
        }

        // ── 메인 ──────────────────────────────────────────────────
        public override void OnInspectorGUI()
        {
            InitStyles();
            serializedObject.Update();

            DrawSummary();
            DrawValidation();
            DrawSearchBar();

            EditorGUILayout.Space(4);

            if (string.IsNullOrWhiteSpace(_search))
            {
                DrawTabStrip();
                EditorGUILayout.Space(4);
                DrawTab(_activeTab);
            }
            else
            {
                DrawSearchResults();
            }

            serializedObject.ApplyModifiedProperties();
        }

        // ── 요약 헤더 + 거리 바 ───────────────────────────────────
        private void DrawSummary()
        {
            var c = (CameraSettings)target;

            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField($"📷 {c.name}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                $"거리 {c.minDistance:0.0}~{c.maxDistance:0.0}  ·  FOV {c.fovExplore:0}~{c.fovCombat:0}  ·  락온 {c.lockOnRange:0}m",
                EditorStyles.miniLabel);

            DrawDistanceBar(c);
            EditorGUILayout.EndVertical();
        }

        /// <summary>min·default·combat·max 거리의 상대 관계를 막대로 표시.</summary>
        private void DrawDistanceBar(CameraSettings c)
        {
            var rect = GUILayoutUtility.GetRect(0, 26, GUILayout.ExpandWidth(true));
            rect = new Rect(rect.x + 4, rect.y + 4, rect.width - 8, rect.height - 8);
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 0.6f));

            float lo = Mathf.Min(c.minDistance, c.maxDistance, c.defaultDistance, c.lockOnDistance);
            float hi = Mathf.Max(c.minDistance, c.maxDistance, c.defaultDistance, c.lockOnDistance);
            float span = Mathf.Max(0.0001f, hi - lo);

            float X(float v) => rect.x + (v - lo) / span * rect.width;

            // min~max 구간 바
            var range = new Rect(X(c.minDistance), rect.y, Mathf.Max(2f, X(c.maxDistance) - X(c.minDistance)), rect.height);
            EditorGUI.DrawRect(range, new Color(0.30f, 0.55f, 0.90f, 0.35f));

            DrawMarker(rect, X(c.defaultDistance), new Color(0.4f, 0.9f, 0.5f), "기본");
            DrawMarker(rect, X(c.lockOnDistance), new Color(0.95f, 0.7f, 0.2f), "락온");
        }

        private void DrawMarker(Rect bar, float x, Color color, string label)
        {
            x = Mathf.Clamp(x, bar.x, bar.xMax - 1);
            EditorGUI.DrawRect(new Rect(x, bar.y, 2f, bar.height), color);
            var labelRect = new Rect(x - 14, bar.y + bar.height - 13, 30, 12);
            var style = new GUIStyle(EditorStyles.miniLabel) { fontSize = 9, normal = { textColor = color } };
            GUI.Label(labelRect, label, style);
        }

        // ── 검증 경고 ─────────────────────────────────────────────
        private void DrawValidation()
        {
            // 다중 선택 시 첫 대상 기준으로만 검증한다.
            var c = (CameraSettings)target;
            var warns = new List<string>();

            if (c.minDistance >= c.maxDistance)
                warns.Add($"minDistance({c.minDistance:0.##}) ≥ maxDistance({c.maxDistance:0.##}) — 거리 구간이 뒤집혔습니다.");
            if (c.defaultDistance < c.minDistance || c.defaultDistance > c.maxDistance)
                warns.Add($"defaultDistance({c.defaultDistance:0.##})가 [min,max] 범위를 벗어났습니다.");
            if (c.minVerticalAngle >= c.maxVerticalAngle)
                warns.Add($"minVerticalAngle({c.minVerticalAngle:0.#}) ≥ maxVerticalAngle({c.maxVerticalAngle:0.#}) — 피치 범위가 뒤집혔습니다.");
            if (c.lockOnReleaseRange < c.lockOnRange)
                warns.Add($"lockOnReleaseRange({c.lockOnReleaseRange:0.##}) < lockOnRange({c.lockOnRange:0.##}) — 코드가 lockOnRange로 폴백합니다.");
            if (c.lockOnMinOffsetAngle >= c.lockOnMaxOffsetAngle)
                warns.Add($"lockOnMinOffsetAngle({c.lockOnMinOffsetAngle:0.#}) ≥ lockOnMaxOffsetAngle({c.lockOnMaxOffsetAngle:0.#})");
            if (c.freeOrbitStartDistance >= c.freeOrbitFullDistance)
                warns.Add($"freeOrbitStartDistance({c.freeOrbitStartDistance:0.##}) ≥ freeOrbitFullDistance({c.freeOrbitFullDistance:0.##})");
            if (c.monsterSizeReference >= c.monsterSizeForMaxFOV)
                warns.Add($"monsterSizeReference({c.monsterSizeReference:0.##}) ≥ monsterSizeForMaxFOV({c.monsterSizeForMaxFOV:0.##})");
            if (c.enableLockOnFitDistance && c.lockOnFitMaxDistance < c.maxDistance)
                warns.Add($"lockOnFitMaxDistance({c.lockOnFitMaxDistance:0.##}) < maxDistance({c.maxDistance:0.##}) — 거리 피팅이 일반 max보다 가까워 효과가 없습니다.");

            if (warns.Count == 0) return;

            EditorGUILayout.Space(2);
            foreach (var w in warns)
                EditorGUILayout.HelpBox(w, MessageType.Warning);
        }

        // ── 검색 바 ───────────────────────────────────────────────
        private void DrawSearchBar()
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🔍", GUILayout.Width(20));
            _search = EditorGUILayout.TextField(_search);
            if (!string.IsNullOrEmpty(_search) && GUILayout.Button("✕", GUILayout.Width(24)))
            {
                _search = string.Empty;
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();
        }

        // ── 탭 스트립 ─────────────────────────────────────────────
        private void DrawTabStrip()
        {
            var labels = _tabs.Select(t => t.name).ToArray();
            int prev = _activeTab;
            _activeTab = GUILayout.Toolbar(_activeTab, labels);
            if (_activeTab != prev)
                SessionState.SetInt(TabPrefKey, _activeTab);

            // 활성 탭 색상 언더라인
            var line = GUILayoutUtility.GetRect(0, 3, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(line, _tabs[_activeTab].color);
        }

        private void DrawTab(int tab)
        {
            foreach (var section in _sections.Where(s => s.tab == tab))
                DrawSection(section, _tabs[tab].color);
        }

        // ── 섹션 그리기 ───────────────────────────────────────────
        private void DrawSection(Section section, Color color)
        {
            DrawSectionHeader(section.title, color);

            bool gate = true; // enable/use 불리언 게이팅 상태
            foreach (var f in section.fields)
            {
                var prop = serializedObject.FindProperty(f.Name);
                if (prop == null) continue;

                bool isGateBool = f.FieldType == typeof(bool) && IsGateName(f.Name);
                using (new EditorGUI.DisabledScope(!isGateBool && !gate))
                {
                    EditorGUILayout.PropertyField(prop, true);
                }

                if (isGateBool)
                    gate = prop.boolValue;
            }
            EditorGUILayout.Space(4);
        }

        private static bool IsGateName(string name)
            => name.StartsWith("enable", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("use", StringComparison.OrdinalIgnoreCase);

        private void DrawSectionHeader(string title, Color color)
        {
            var rect = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(color.r * 0.30f, color.g * 0.30f, color.b * 0.30f, 0.6f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 4, rect.height), color);
            GUI.Label(new Rect(rect.x + 8, rect.y, rect.width - 8, rect.height), title, _headerStyle);
        }

        // ── 검색 결과(플랫) ───────────────────────────────────────
        private void DrawSearchResults()
        {
            string q = _search.Trim();
            int hits = 0;

            foreach (var section in _sections)
            {
                var matched = section.fields
                    .Where(f => FieldMatches(f, section, q))
                    .ToList();
                if (matched.Count == 0) continue;

                DrawSectionHeader(section.title, _tabs[section.tab].color);
                foreach (var f in matched)
                {
                    var prop = serializedObject.FindProperty(f.Name);
                    if (prop == null) continue;
                    EditorGUILayout.PropertyField(prop, true);
                    hits++;
                }
                EditorGUILayout.Space(3);
            }

            if (hits == 0)
                EditorGUILayout.HelpBox($"'{q}'에 해당하는 항목이 없습니다.", MessageType.Info);
        }

        /// <summary>필드명·표시명·헤더·툴팁(한국어 포함)을 모두 대상으로 검색.</summary>
        private bool FieldMatches(FieldInfo f, Section section, string q)
        {
            var prop = serializedObject.FindProperty(f.Name);
            string display = prop != null ? prop.displayName : f.Name;
            _tooltips.TryGetValue(f.Name, out string tip);

            return Contains(f.Name, q)
                || Contains(display, q)
                || Contains(section.title, q)
                || Contains(tip, q);
        }

        private static bool Contains(string source, string q)
            => !string.IsNullOrEmpty(source)
            && source.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
