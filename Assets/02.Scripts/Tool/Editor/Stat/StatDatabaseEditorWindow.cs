#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Stat;

namespace UPlayGround.Tool.Editor.Stat
{
    /// <summary>
    /// 프로젝트 내 모든 ActorStatSO를 한 창에서 관리하는 에디터 창.
    /// 메뉴: UPlayGround/Stat/Stat Database Editor
    /// </summary>
    public class StatDatabaseEditorWindow : EditorWindow
    {
        // ── UI 상태 ───────────────────────────────────────────────
        private List<ActorStatSO> _allStatSOs = new();
        private ActorStatSO _selected;
        private ActorStatSO _compareTarget;
        private bool _compareMode;
        private string _searchFilter = "";
        private Vector2 _listScroll;
        private Vector2 _detailScroll;
        private string _categoryFilter = "전체";

        // ── 스타일 캐시 ───────────────────────────────────────────
        private GUIStyle _styleListItem;
        private GUIStyle _styleListItemSelected;
        private bool _stylesInitialized;

        // ── 색상 ─────────────────────────────────────────────────
        private static readonly Color ColorHeader   = new(0.15f, 0.15f, 0.20f);
        private static readonly Color ColorSelected = new(0.22f, 0.44f, 0.72f);
        private static readonly Color ColorDivider  = new(0.25f, 0.25f, 0.28f);
        private static readonly Color ColorPositive = new(0.30f, 0.80f, 0.40f);
        private static readonly Color ColorNegative = new(0.85f, 0.40f, 0.30f);

        // ── 레이아웃 상수 ────────────────────────────────────────
        private const float ListWidth   = 220f;
        private const float ItemHeight  = 28f;
        private const string DefaultSavePath = "Assets/10.Datas/Stat";

        // ── 카테고리 (ActorStatSOEditor와 동일 매핑) ─────────────
        private static readonly (string label, StatType[] types)[] _categories =
        {
            ("생존",  new[] { StatType.MaxHealth, StatType.HealthRegenRate }),
            ("전투",  new[] { StatType.AttackPower, StatType.Defense, StatType.CritRate, StatType.CritMultiplier }),
            ("이동",  new[] { StatType.MoveSpeed, StatType.DashDistance }),
            ("강인도", new[] { StatType.MaxPoise, StatType.PoiseRecoveryRate, StatType.PoiseRecoveryDelay }),
            ("스킬",  new[] { StatType.SkillGaugeRate, StatType.InvincibleDuration }),
        };

        // ── 메뉴 ─────────────────────────────────────────────────
        [MenuItem("UPlayGround/Gameplay/Stat/Stat Database Editor")]
        public static void Open()
        {
            var window = GetWindow<StatDatabaseEditorWindow>();
            window.titleContent = new GUIContent("Stat Database", EditorGUIUtility.IconContent("d_ScriptableObject Icon").image);
            window.minSize = new Vector2(720f, 480f);
            window.Show();
        }

        // ── 라이프사이클 ──────────────────────────────────────────
        private void OnEnable() => RefreshAssetList();

        private void OnGUI()
        {
            InitStyles();
            DrawToolbar();

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
            DrawListPanel();
            DrawDivider();
            DrawDetailPanel();
            EditorGUILayout.EndHorizontal();
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            _styleListItem = new GUIStyle(EditorStyles.label)
            {
                padding = new RectOffset(6, 4, 4, 4),
                fontSize = 11,
            };
            _styleListItemSelected = new GUIStyle(_styleListItem);
            _styleListItemSelected.normal.textColor = Color.white;
        }

        // ── 툴바 ─────────────────────────────────────────────────
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("새 SO 생성", EditorStyles.toolbarButton, GUILayout.Width(80)))
                CreateNewSO();

            using (new EditorGUI.DisabledScope(_selected == null))
            {
                if (GUILayout.Button("선택 SO 복제", EditorStyles.toolbarButton, GUILayout.Width(90)))
                    DuplicateSelected();
            }

            if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(70)))
                RefreshAssetList();

            GUILayout.Space(8);

            // 비교 모드
            _compareMode = GUILayout.Toggle(_compareMode, "비교 모드", EditorStyles.toolbarButton, GUILayout.Width(70));
            if (_compareMode)
            {
                _compareTarget = (ActorStatSO)EditorGUILayout.ObjectField(_compareTarget, typeof(ActorStatSO), false, GUILayout.Width(180));
            }

            GUILayout.FlexibleSpace();

            // 카테고리 필터
            string[] categoryNames = new[] { "전체", "생존", "전투", "이동", "강인도", "스킬" };
            int currentIndex = Array.IndexOf(categoryNames, _categoryFilter);
            int newIndex = EditorGUILayout.Popup(currentIndex < 0 ? 0 : currentIndex, categoryNames, EditorStyles.toolbarPopup, GUILayout.Width(80));
            _categoryFilter = categoryNames[newIndex];

            if (GUILayout.Button("CSV 내보내기", EditorStyles.toolbarButton, GUILayout.Width(90)))
                ExportCSV();

            EditorGUILayout.EndHorizontal();
        }

        // ── 목록 패널 ─────────────────────────────────────────────
        private void DrawListPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(ListWidth), GUILayout.ExpandHeight(true));

            // 헤더
            var headerRect = GUILayoutUtility.GetRect(0, 24, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(headerRect, ColorHeader);
            GUI.Label(new Rect(headerRect.x + 6, headerRect.y + 4, headerRect.width, headerRect.height), $"ActorStatSO ({_allStatSOs.Count})", EditorStyles.boldLabel);

            // 검색
            EditorGUILayout.BeginHorizontal();
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);
            EditorGUILayout.EndHorizontal();

            _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
            string lower = _searchFilter.ToLower();
            for (int i = 0; i < _allStatSOs.Count; i++)
            {
                var so = _allStatSOs[i];
                if (so == null) continue;
                if (!string.IsNullOrEmpty(lower) && !so.name.ToLower().Contains(lower))
                    continue;

                bool isSelected = so == _selected;
                var rect = GUILayoutUtility.GetRect(0, ItemHeight, GUILayout.ExpandWidth(true));

                if (isSelected)
                    EditorGUI.DrawRect(rect, ColorSelected);

                if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                {
                    _selected = so;
                    Repaint();
                }

                var labelRect = new Rect(rect.x + 8, rect.y, rect.width - 8, rect.height);
                GUI.Label(labelRect, so.name, isSelected ? _styleListItemSelected : _styleListItem);
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void DrawDivider()
        {
            var rect = GUILayoutUtility.GetRect(1, 0, GUILayout.Width(1), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(rect, ColorDivider);
        }

        // ── 상세 패널 ─────────────────────────────────────────────
        private void DrawDetailPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            if (_selected == null)
            {
                EditorGUILayout.LabelField("좌측에서 ActorStatSO를 선택하세요.", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.EndVertical();
                return;
            }

            // 헤더
            var headerRect = GUILayoutUtility.GetRect(0, 24, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(headerRect, ColorHeader);
            GUI.Label(new Rect(headerRect.x + 6, headerRect.y + 4, headerRect.width, headerRect.height), _selected.name, EditorStyles.boldLabel);

            // 컬럼 헤더
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("StatType", EditorStyles.boldLabel, GUILayout.Width(170));
            GUILayout.Label("Base", EditorStyles.boldLabel, GUILayout.Width(100));
            if (_compareMode && _compareTarget != null)
            {
                GUILayout.Label("Compare", EditorStyles.boldLabel, GUILayout.Width(100));
                GUILayout.Label("Δ", EditorStyles.boldLabel, GUILayout.Width(80));
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);

            foreach (var category in _categories)
            {
                if (_categoryFilter != "전체" && _categoryFilter != category.label) continue;

                EditorGUILayout.LabelField($"▌{category.label}", EditorStyles.miniBoldLabel);
                foreach (var type in category.types)
                    DrawDetailRow(_selected, type);
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(2);
            DrawDetailFooter();

            EditorGUILayout.EndVertical();
        }

        private void DrawDetailRow(ActorStatSO so, StatType type)
        {
            EditorGUILayout.BeginHorizontal();

            bool isExplicit = so.TryGetExplicit(type, out float value);

            // 라벨
            var prevColor = GUI.color;
            GUI.color = isExplicit ? Color.white : new Color(0.65f, 0.65f, 0.65f);
            GUILayout.Label(type.ToString(), GUILayout.Width(170));
            GUI.color = prevColor;

            // 값 입력
            float newValue = EditorGUILayout.FloatField(value, GUILayout.Width(100));

            // 비교
            if (_compareMode && _compareTarget != null)
            {
                float compareValue = _compareTarget.GetBase(type);
                GUILayout.Label(compareValue.ToString("0.##"), GUILayout.Width(100));

                float delta = value - compareValue;
                if (Mathf.Approximately(delta, 0f))
                {
                    GUILayout.Label("—", GUILayout.Width(80));
                }
                else
                {
                    GUI.color = delta > 0 ? ColorPositive : ColorNegative;
                    string sign = delta > 0 ? "+" : "";
                    GUILayout.Label($"{sign}{delta:0.##}", GUILayout.Width(80));
                    GUI.color = prevColor;
                }
            }

            // 명시 여부 / 폴백 표시
            if (!isExplicit)
                GUILayout.Label("(폴백)", EditorStyles.miniLabel, GUILayout.Width(40));

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (!Mathf.Approximately(newValue, value))
            {
                Undo.RecordObject(so, "Edit Stat");
                so.EditorSet(type, newValue);
                EditorUtility.SetDirty(so);
            }
        }

        private void DrawDetailFooter()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("누락 스탯 채우기", GUILayout.Height(22)))
            {
                Undo.RecordObject(_selected, "Fill Missing");
                _selected.EditorFillMissing();
                EditorUtility.SetDirty(_selected);
            }
            if (GUILayout.Button("Project에서 열기", GUILayout.Height(22)))
            {
                EditorGUIUtility.PingObject(_selected);
                Selection.activeObject = _selected;
            }
            EditorGUILayout.EndHorizontal();
        }

        // ── 에셋 관리 ─────────────────────────────────────────────

        private void RefreshAssetList()
        {
            _allStatSOs.Clear();
            string[] guids = AssetDatabase.FindAssets("t:ActorStatSO");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<ActorStatSO>(path);
                if (so != null) _allStatSOs.Add(so);
            }
            _allStatSOs.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
        }

        private void CreateNewSO()
        {
            EnsureSavePath();
            string path = EditorUtility.SaveFilePanelInProject("새 ActorStatSO 생성", "ActorStat_New", "asset", "저장 위치를 선택하세요.", DefaultSavePath);
            if (string.IsNullOrEmpty(path)) return;

            var so = ScriptableObject.CreateInstance<ActorStatSO>();
            so.EditorFillMissing();
            AssetDatabase.CreateAsset(so, path);
            AssetDatabase.SaveAssets();

            RefreshAssetList();
            _selected = so;
        }

        private void DuplicateSelected()
        {
            if (_selected == null) return;
            string srcPath = AssetDatabase.GetAssetPath(_selected);
            string dstPath = AssetDatabase.GenerateUniqueAssetPath(srcPath);
            if (AssetDatabase.CopyAsset(srcPath, dstPath))
            {
                AssetDatabase.SaveAssets();
                RefreshAssetList();
                _selected = AssetDatabase.LoadAssetAtPath<ActorStatSO>(dstPath);
            }
        }

        private void EnsureSavePath()
        {
            if (!Directory.Exists(DefaultSavePath))
                Directory.CreateDirectory(DefaultSavePath);
        }

        // ── CSV 내보내기 ──────────────────────────────────────────

        private void ExportCSV()
        {
            string path = EditorUtility.SaveFilePanel("CSV 내보내기", "", "StatBalance", "csv");
            if (string.IsNullOrEmpty(path)) return;

            var sb = new StringBuilder();
            sb.Append("ActorStatSO");
            foreach (StatType type in System.Enum.GetValues(typeof(StatType)))
                sb.Append($",{type}");
            sb.AppendLine();

            foreach (var so in _allStatSOs)
            {
                if (so == null) continue;
                sb.Append(so.name);
                foreach (StatType type in System.Enum.GetValues(typeof(StatType)))
                    sb.Append($",{so.GetBase(type)}");
                sb.AppendLine();
            }

            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true)); // BOM 포함 (Excel 한글)
            Debug.Log($"[StatDatabase] CSV 내보내기 완료: {path}");
            EditorUtility.RevealInFinder(path);
        }
    }
}
#endif
