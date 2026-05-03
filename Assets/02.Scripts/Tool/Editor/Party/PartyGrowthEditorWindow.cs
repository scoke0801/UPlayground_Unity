#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using UPlayGround.Data.Stat;

namespace UPlayGround.Tool.Editor.Party
{
    /// <summary>
    /// CharacterActorType별 성장 데이터를 스프레드시트 형태로 편집하는 에디터 창.
    /// 메뉴: UPlayGround/Party/Party Growth Editor
    /// </summary>
    public class PartyGrowthEditorWindow : EditorWindow
    {
        private PartyConfigSO _config;
        private readonly Dictionary<CharacterActorType, PartyMemberGrowthSO> _growthLookup = new();
        private Vector2 _scroll;
        private Vector2 _horizontalScroll;
        private string _growthSavePath = DefaultGrowthPath;
        private string _statSavePath = DefaultStatPath;
        private string _statCategory = "전투";
        private int _previewLevel = 1;
        private bool _showH09;

        private const string DefaultGrowthPath = "Assets/10.Datas/Party/Growth";
        private const string DefaultStatPath = "Assets/10.Datas/Stat/Player";
        private const float RowHeight = 24f;
        private const float ColType = 110f;
        private const float ColObject = 180f;
        private const float ColSmall = 70f;
        private const float ColPower = 90f;
        private const float ColStatBase = 78f;
        private const float ColFormula = 72f;
        private const float ColGrowth = 70f;

        private static readonly Color ColorHeader = new(0.15f, 0.15f, 0.20f);
        private static readonly Color ColorRowEven = new(0.20f, 0.20f, 0.22f);
        private static readonly Color ColorRowOdd = new(0.23f, 0.23f, 0.25f);
        private static readonly Color ColorMissing = new(0.85f, 0.55f, 0.15f);

        private static readonly (string label, StatType[] types)[] Categories =
        {
            ("생존",  new[] { StatType.MaxHealth, StatType.HealthRegenRate }),
            ("전투",  new[] { StatType.AttackPower, StatType.Defense, StatType.CritRate, StatType.CritMultiplier }),
            ("이동",  new[] { StatType.MoveSpeed, StatType.DashDistance }),
            ("강인도", new[] { StatType.MaxPoise, StatType.PoiseRecoveryRate, StatType.PoiseRecoveryDelay }),
            ("스킬",  new[] { StatType.SkillGaugeRate, StatType.InvincibleDuration }),
            ("전체",  (StatType[])Enum.GetValues(typeof(StatType))),
        };

        [MenuItem("UPlayGround/Party/Party Growth Editor")]
        public static void Open()
        {
            var window = GetWindow<PartyGrowthEditorWindow>();
            window.titleContent = new GUIContent("Party Growth", EditorGUIUtility.IconContent("d_ScriptableObject Icon").image);
            window.minSize = new Vector2(980f, 520f);
            window.Show();
        }

        private void OnEnable()
        {
            LoadDefaultConfig();
            RefreshLookup();
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (_config == null)
            {
                EditorGUILayout.HelpBox("PartyConfigSO를 선택하거나 프로젝트에 PartyConfigSO를 생성해야 합니다.", MessageType.Warning);
                return;
            }

            DrawPathSettings();
            DrawTable();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUI.BeginChangeCheck();
            _config = (PartyConfigSO)EditorGUILayout.ObjectField(_config, typeof(PartyConfigSO), false, GUILayout.Width(260));
            if (EditorGUI.EndChangeCheck())
                RefreshLookup();

            if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(70)))
                RefreshLookup();

            using (new EditorGUI.DisabledScope(_config == null))
            {
                if (GUILayout.Button("누락 Growth 모두 생성", EditorStyles.toolbarButton, GUILayout.Width(130)))
                    CreateMissingGrowthAssets();
            }

            GUILayout.Space(8);
            GUILayout.Label("Stat", GUILayout.Width(28));
            string[] categoryNames = GetCategoryNames();
            int categoryIndex = Array.IndexOf(categoryNames, _statCategory);
            _statCategory = categoryNames[EditorGUILayout.Popup(Mathf.Max(0, categoryIndex), categoryNames, EditorStyles.toolbarPopup, GUILayout.Width(80))];

            GUILayout.Space(8);
            GUILayout.Label("미리보기 Lv", GUILayout.Width(70));
            _previewLevel = Mathf.Max(1, EditorGUILayout.IntField(_previewLevel, GUILayout.Width(45)));

            _showH09 = GUILayout.Toggle(_showH09, "H09 표시", EditorStyles.toolbarButton, GUILayout.Width(70));

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Config 선택", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                Selection.activeObject = _config;
                EditorGUIUtility.PingObject(_config);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPathSettings()
        {
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Growth 저장 경로", GUILayout.Width(95));
            _growthSavePath = EditorGUILayout.TextField(_growthSavePath);
            if (GUILayout.Button("...", GUILayout.Width(28)))
                BrowseSavePath(ref _growthSavePath);

            GUILayout.Label("BaseStat 저장 경로", GUILayout.Width(105));
            _statSavePath = EditorGUILayout.TextField(_statSavePath);
            if (GUILayout.Button("...", GUILayout.Width(28)))
                BrowseSavePath(ref _statSavePath);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawTable()
        {
            var statTypes = GetVisibleStatTypes();

            _horizontalScroll = EditorGUILayout.BeginScrollView(_horizontalScroll, false, true);
            float width = ColType + ColObject * 2f + ColSmall * 2f + ColPower + statTypes.Length * (ColStatBase + ColFormula + ColGrowth * 2f);
            DrawHeader(width, statTypes);

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true), GUILayout.MinWidth(width));
            int rowIndex = 0;
            foreach (CharacterActorType type in Enum.GetValues(typeof(CharacterActorType)))
            {
                if (type == CharacterActorType.None) continue;
                if (!_showH09 && type == CharacterActorType.H09) continue;
                DrawRow(type, rowIndex++, width, statTypes);
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader(float width, StatType[] statTypes)
        {
            Rect rect = GUILayoutUtility.GetRect(width, RowHeight);
            EditorGUI.DrawRect(rect, ColorHeader);

            float x = rect.x;
            DrawHeaderCell("Character", ref x, ColType, rect);
            DrawHeaderCell("GrowthData", ref x, ColObject, rect);
            DrawHeaderCell("BaseStat", ref x, ColObject, rect);
            DrawHeaderCell("InitLv", ref x, ColSmall, rect);
            DrawHeaderCell("Cap", ref x, ColSmall, rect);
            DrawHeaderCell("Power", ref x, ColPower, rect);

            for (int i = 0; i < statTypes.Length; i++)
            {
                string name = statTypes[i].ToString();
                DrawHeaderCell($"{name} Base", ref x, ColStatBase, rect);
                DrawHeaderCell("Formula", ref x, ColFormula, rect);
                DrawHeaderCell("Flat/Lv", ref x, ColGrowth, rect);
                DrawHeaderCell("%/Lv", ref x, ColGrowth, rect);
            }
        }

        private void DrawHeaderCell(string label, ref float x, float width, Rect rowRect)
        {
            GUI.Label(new Rect(x + 4, rowRect.y + 3, width - 8, rowRect.height), label, EditorStyles.boldLabel);
            x += width;
        }

        private void DrawRow(CharacterActorType type, int rowIndex, float width, StatType[] statTypes)
        {
            Rect rect = GUILayoutUtility.GetRect(width, RowHeight);
            EditorGUI.DrawRect(rect, rowIndex % 2 == 0 ? ColorRowEven : ColorRowOdd);

            _growthLookup.TryGetValue(type, out var growth);
            float x = rect.x;

            GUI.Label(new Rect(x + 4, rect.y + 3, ColType - 8, rect.height), type.ToString());
            x += ColType;

            DrawGrowthCell(type, ref x, rect, growth);
            DrawBaseStatCell(type, ref x, rect, growth);

            if (growth == null)
            {
                DrawMissingTail(ref x, rect, "GrowthData 없음");
                return;
            }

            DrawLevelFields(growth, ref x, rect);
            DrawPowerPreview(type, growth, ref x, rect);

            for (int i = 0; i < statTypes.Length; i++)
                DrawStatCells(growth, statTypes[i], ref x, rect);
        }

        private void DrawGrowthCell(CharacterActorType type, ref float x, Rect rect, PartyMemberGrowthSO growth)
        {
            Rect objectRect = new(x + 2, rect.y + 2, ColObject - 58, rect.height - 4);
            EditorGUI.BeginChangeCheck();
            var newGrowth = (PartyMemberGrowthSO)EditorGUI.ObjectField(objectRect, growth, typeof(PartyMemberGrowthSO), false);
            if (EditorGUI.EndChangeCheck())
                SetGrowthData(type, newGrowth);

            if (growth == null)
            {
                Color prev = GUI.color;
                GUI.color = ColorMissing;
                GUI.Label(new Rect(x + ColObject - 54, rect.y + 3, 22, rect.height), "없음", EditorStyles.miniLabel);
                GUI.color = prev;
            }

            if (GUI.Button(new Rect(x + ColObject - 32, rect.y + 2, 30, rect.height - 4), "생성"))
                CreateGrowthAsset(type);

            x += ColObject;
        }

        private void DrawBaseStatCell(CharacterActorType type, ref float x, Rect rect, PartyMemberGrowthSO growth)
        {
            using (new EditorGUI.DisabledScope(growth == null))
            {
                Rect objectRect = new(x + 2, rect.y + 2, ColObject - 58, rect.height - 4);
                EditorGUI.BeginChangeCheck();
                var newStat = (ActorStatSO)EditorGUI.ObjectField(objectRect, growth != null ? growth.baseStat : null, typeof(ActorStatSO), false);
                if (EditorGUI.EndChangeCheck() && growth != null)
                {
                    Undo.RecordObject(growth, "Set Growth BaseStat");
                    growth.baseStat = newStat;
                    EditorUtility.SetDirty(growth);
                }

                if (GUI.Button(new Rect(x + ColObject - 32, rect.y + 2, 30, rect.height - 4), "생성") && growth != null)
                    CreateBaseStatAsset(type, growth);
            }

            x += ColObject;
        }

        private void DrawLevelFields(PartyMemberGrowthSO growth, ref float x, Rect rect)
        {
            EditorGUI.BeginChangeCheck();
            int initialLevel = EditorGUI.IntField(new Rect(x + 2, rect.y + 2, ColSmall - 4, rect.height - 4), growth.initialLevel);
            x += ColSmall;
            int levelCap = EditorGUI.IntField(new Rect(x + 2, rect.y + 2, ColSmall - 4, rect.height - 4), growth.levelCap);
            x += ColSmall;

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(growth, "Edit Growth Level");
                growth.initialLevel = Mathf.Max(1, initialLevel);
                growth.levelCap = Mathf.Max(growth.initialLevel, levelCap);
                EditorUtility.SetDirty(growth);
            }
        }

        private void DrawPowerPreview(CharacterActorType type, PartyMemberGrowthSO growth, ref float x, Rect rect)
        {
            int level = Mathf.Clamp(_previewLevel, 1, Mathf.Max(1, growth.levelCap));
            long power = PartyPowerCalculator.Calculate(type, growth, level).CombatPower;
            GUI.Label(new Rect(x + 4, rect.y + 3, ColPower - 8, rect.height), power.ToString("#,0"), EditorStyles.miniLabel);
            x += ColPower;
        }

        private void DrawStatCells(PartyMemberGrowthSO growth, StatType statType, ref float x, Rect rect)
        {
            ActorStatSO baseStat = growth.baseStat;
            bool hasBase = baseStat != null;
            float baseValue = hasBase ? baseStat.GetBase(statType) : ActorStatSO.GetDefault(statType);

            EditorGUI.BeginDisabledGroup(!hasBase);
            EditorGUI.BeginChangeCheck();
            float newBase = EditorGUI.FloatField(new Rect(x + 2, rect.y + 2, ColStatBase - 4, rect.height - 4), baseValue);
            if (EditorGUI.EndChangeCheck() && hasBase)
            {
                Undo.RecordObject(baseStat, "Edit Growth Base Stat");
                baseStat.EditorSet(statType, newBase);
                EditorUtility.SetDirty(baseStat);
            }
            EditorGUI.EndDisabledGroup();
            x += ColStatBase;

            StatGrowthRule rule = GetOrDefaultRule(growth, statType);
            EditorGUI.BeginChangeCheck();
            GrowthFormula formula = (GrowthFormula)EditorGUI.EnumPopup(new Rect(x + 2, rect.y + 2, ColFormula - 4, rect.height - 4), rule.formula);
            x += ColFormula;
            float flat = EditorGUI.FloatField(new Rect(x + 2, rect.y + 2, ColGrowth - 4, rect.height - 4), rule.flatPerLevel);
            x += ColGrowth;
            float percent = EditorGUI.FloatField(new Rect(x + 2, rect.y + 2, ColGrowth - 4, rect.height - 4), rule.percentPerLevel);
            x += ColGrowth;

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(growth, "Edit Growth Rule");
                rule.statType = statType;
                rule.formula = formula;
                rule.flatPerLevel = flat;
                rule.percentPerLevel = percent;
                SetRule(growth, rule);
                EditorUtility.SetDirty(growth);
            }
        }

        private void DrawMissingTail(ref float x, Rect rect, string message)
        {
            Color prev = GUI.color;
            GUI.color = ColorMissing;
            GUI.Label(new Rect(x + 4, rect.y + 3, 220f, rect.height), message, EditorStyles.miniLabel);
            GUI.color = prev;
        }

        private void LoadDefaultConfig()
        {
            string[] guids = AssetDatabase.FindAssets("t:PartyConfigSO");
            if (guids.Length == 0) return;

            string preferredPath = "Assets/10.Datas/Party/PartyConfig.asset";
            _config = AssetDatabase.LoadAssetAtPath<PartyConfigSO>(preferredPath);
            if (_config != null) return;

            _config = AssetDatabase.LoadAssetAtPath<PartyConfigSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private void RefreshLookup()
        {
            _growthLookup.Clear();
            if (_config == null || _config.growthData == null) return;

            for (int i = 0; i < _config.growthData.Count; i++)
            {
                PartyMemberGrowthSO growth = _config.growthData[i];
                if (growth == null || growth.characterType == CharacterActorType.None) continue;
                _growthLookup[growth.characterType] = growth;
            }
        }

        private void SetGrowthData(CharacterActorType type, PartyMemberGrowthSO growth)
        {
            if (_config == null) return;

            Undo.RecordObject(_config, "Set Party Growth Data");
            RemoveGrowthForType(type);
            if (growth != null)
            {
                Undo.RecordObject(growth, "Set Growth Character Type");
                growth.characterType = type;
                EditorUtility.SetDirty(growth);
                _config.growthData.Add(growth);
            }

            EditorUtility.SetDirty(_config);
            RefreshLookup();
        }

        private void RemoveGrowthForType(CharacterActorType type)
        {
            _config.growthData.RemoveAll(g => g == null || g.characterType == type);
        }

        private void CreateMissingGrowthAssets()
        {
            foreach (CharacterActorType type in Enum.GetValues(typeof(CharacterActorType)))
            {
                if (type == CharacterActorType.None) continue;
                if (!_showH09 && type == CharacterActorType.H09) continue;
                if (_growthLookup.ContainsKey(type)) continue;
                CreateGrowthAsset(type, saveImmediately: false);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshLookup();
        }

        private void CreateGrowthAsset(CharacterActorType type, bool saveImmediately = true)
        {
            if (_config == null) return;
            EnsureFolder(_growthSavePath);

            var growth = CreateInstance<PartyMemberGrowthSO>();
            growth.characterType = type;
            growth.initialLevel = 1;
            growth.levelCap = 100;

            string path = AssetDatabase.GenerateUniqueAssetPath($"{_growthSavePath}/PartyMemberGrowth_{type}.asset");
            AssetDatabase.CreateAsset(growth, path);

            Undo.RecordObject(_config, "Create Party Growth Data");
            RemoveGrowthForType(type);
            _config.growthData.Add(growth);
            EditorUtility.SetDirty(_config);

            if (saveImmediately)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            RefreshLookup();
        }

        private void CreateBaseStatAsset(CharacterActorType type, PartyMemberGrowthSO growth)
        {
            if (growth == null) return;
            EnsureFolder(_statSavePath);

            var stat = CreateInstance<ActorStatSO>();
            stat.EditorFillMissing();

            string path = AssetDatabase.GenerateUniqueAssetPath($"{_statSavePath}/ActorStat_Player_{type}.asset");
            AssetDatabase.CreateAsset(stat, path);

            Undo.RecordObject(growth, "Create Growth Base Stat");
            growth.baseStat = stat;
            EditorUtility.SetDirty(growth);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static StatGrowthRule GetOrDefaultRule(PartyMemberGrowthSO growth, StatType statType)
        {
            if (growth != null && growth.TryGetRule(statType, out var rule))
                return rule;

            return new StatGrowthRule
            {
                statType = statType,
                formula = GrowthFormula.Flat,
                flatPerLevel = 0f,
                percentPerLevel = 0f,
                curve = AnimationCurve.Linear(0f, 1f, 1f, 1f),
            };
        }

        private static void SetRule(PartyMemberGrowthSO growth, StatGrowthRule rule)
        {
            for (int i = 0; i < growth.growthRules.Count; i++)
            {
                if (growth.growthRules[i].statType != rule.statType) continue;
                growth.growthRules[i] = rule;
                return;
            }

            growth.growthRules.Add(rule);
        }

        private StatType[] GetVisibleStatTypes()
        {
            for (int i = 0; i < Categories.Length; i++)
            {
                if (Categories[i].label == _statCategory)
                    return Categories[i].types;
            }

            return Categories[1].types;
        }

        private static string[] GetCategoryNames()
        {
            string[] names = new string[Categories.Length];
            for (int i = 0; i < Categories.Length; i++)
                names[i] = Categories[i].label;
            return names;
        }

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (AssetDatabase.IsValidFolder(path)) return;

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private void BrowseSavePath(ref string targetPath)
        {
            string abs = EditorUtility.OpenFolderPanel("저장 경로 선택", targetPath, "");
            if (string.IsNullOrEmpty(abs)) return;

            string projectRoot = Application.dataPath.Replace("/Assets", "");
            if (abs.StartsWith(projectRoot))
                targetPath = "Assets" + abs.Substring(projectRoot.Length + "/Assets".Length).Replace("\\", "/");
            else
                EditorUtility.DisplayDialog("경고", "프로젝트 폴더 내부 경로를 선택해야 합니다.", "확인");
        }
    }
}
#endif
