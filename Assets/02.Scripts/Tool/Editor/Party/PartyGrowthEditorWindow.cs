#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using UPlayGround.Data.Stat;

namespace UPlayGround.Tool.Editor.Party
{
    /// <summary>
    /// CharacterActorType별 기본 Attribute와 레벨 메타데이터를 편집하는 에디터 창.
    /// 메뉴: UPlayGround/Party/Party Growth Editor
    /// </summary>
    public class PartyGrowthEditorWindow : EditorWindow
    {
        private PartyConfigSO _config;
        private readonly Dictionary<CharacterActorType, PartyMemberGrowthSO> _growthLookup = new();
        private Vector2 _scroll;
        private Vector2 _horizontalScroll;
        private string _growthSavePath = DefaultGrowthPath;
        private string _profileSavePath = DefaultProfilePath;
        private string _statCategory = "전투";
        private bool _showH09;

        private const string DefaultGrowthPath = "Assets/10.Datas/Party/Growth";
        private const string DefaultProfilePath =
            "Assets/10.Datas/Ability/Attributes/Migrated";
        private const float RowHeight = 24f;
        private const float ColType = 110f;
        private const float ColObject = 180f;
        private const float ColSmall = 70f;
        private const float ColPower = 90f;
        private const float ColStatBase = 78f;

        private static readonly Color ColorHeader = new(0.15f, 0.15f, 0.20f);
        private static readonly Color ColorRowEven = new(0.20f, 0.20f, 0.22f);
        private static readonly Color ColorRowOdd = new(0.23f, 0.23f, 0.25f);
        private static readonly Color ColorMissing = new(0.85f, 0.55f, 0.15f);

        private static readonly (string label, AttributeId[] attributes)[] Categories =
        {
            ("생존",  new AttributeId[] { global::UPlayGround.Data.Stat.Attributes.Vital.MaxHealth, global::UPlayGround.Data.Stat.Attributes.Vital.HealthRegenRate }),
            ("전투",  new AttributeId[] { global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower, global::UPlayGround.Data.Stat.Attributes.Combat.Defense, global::UPlayGround.Data.Stat.Attributes.Combat.CritRate, global::UPlayGround.Data.Stat.Attributes.Combat.CritMultiplier }),
            ("이동",  new AttributeId[] { global::UPlayGround.Data.Stat.Attributes.Movement.MoveSpeed, global::UPlayGround.Data.Stat.Attributes.Movement.DashDistance }),
            ("강인도", new AttributeId[] { global::UPlayGround.Data.Stat.Attributes.Vital.MaxPoise, global::UPlayGround.Data.Stat.Attributes.Vital.PoiseRecoveryRate, global::UPlayGround.Data.Stat.Attributes.Vital.PoiseRecoveryDelay }),
            ("스킬",  new AttributeId[] { global::UPlayGround.Data.Stat.Attributes.Resource.GenerationMultiplier, global::UPlayGround.Data.Stat.Attributes.Combat.InvincibleDurationMultiplier }),
            ("생활",  new AttributeId[] { global::UPlayGround.Data.Stat.Attributes.Life.GatheringPower }),
            ("전체",  UPlayGroundAttributeDefaults.ProfileAttributes),
        };

        public static void Open()
        {
            var window = GetWindow<PartyGrowthEditorWindow>();
            window.titleContent = new GUIContent("Party Base Stats", EditorGUIUtility.IconContent("d_ScriptableObject Icon").image);
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

            GUILayout.Label("Profile 저장 경로", GUILayout.Width(105));
            _profileSavePath = EditorGUILayout.TextField(_profileSavePath);
            if (GUILayout.Button("...", GUILayout.Width(28)))
                BrowseSavePath(ref _profileSavePath);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawTable()
        {
            AttributeId[] attributeIds = GetVisibleAttributeIds();

            _horizontalScroll = EditorGUILayout.BeginScrollView(_horizontalScroll, false, true);
            float width = ColType + ColObject * 2f + ColSmall * 2f + ColPower
                          + attributeIds.Length * ColStatBase;
            DrawHeader(width, attributeIds);

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true), GUILayout.MinWidth(width));
            int rowIndex = 0;
            foreach (CharacterActorType type in Enum.GetValues(typeof(CharacterActorType)))
            {
                if (type == CharacterActorType.None) continue;
                if (!_showH09 && type == CharacterActorType.H09) continue;
                DrawRow(type, rowIndex++, width, attributeIds);
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader(float width, AttributeId[] attributeIds)
        {
            Rect rect = GUILayoutUtility.GetRect(width, RowHeight);
            EditorGUI.DrawRect(rect, ColorHeader);

            float x = rect.x;
            DrawHeaderCell("Character", ref x, ColType, rect);
            DrawHeaderCell("GrowthData", ref x, ColObject, rect);
            DrawHeaderCell("BaseProfile", ref x, ColObject, rect);
            DrawHeaderCell("InitLv", ref x, ColSmall, rect);
            DrawHeaderCell("Cap", ref x, ColSmall, rect);
            DrawHeaderCell("Power", ref x, ColPower, rect);

            for (int i = 0; i < attributeIds.Length; i++)
            {
                string name = attributeIds[i].Value;
                DrawHeaderCell($"{name} Base", ref x, ColStatBase, rect);
            }
        }

        private void DrawHeaderCell(string label, ref float x, float width, Rect rowRect)
        {
            GUI.Label(new Rect(x + 4, rowRect.y + 3, width - 8, rowRect.height), label, EditorStyles.boldLabel);
            x += width;
        }

        private void DrawRow(CharacterActorType type, int rowIndex, float width, AttributeId[] attributeIds)
        {
            Rect rect = GUILayoutUtility.GetRect(width, RowHeight);
            EditorGUI.DrawRect(rect, rowIndex % 2 == 0 ? ColorRowEven : ColorRowOdd);

            _growthLookup.TryGetValue(type, out var growth);
            float x = rect.x;

            GUI.Label(new Rect(x + 4, rect.y + 3, ColType - 8, rect.height), type.ToString());
            x += ColType;

            DrawGrowthCell(type, ref x, rect, growth);
            DrawBaseProfileCell(type, ref x, rect, growth);

            if (growth == null)
            {
                DrawMissingTail(ref x, rect, "GrowthData 없음");
                return;
            }

            DrawLevelFields(growth, ref x, rect);
            DrawPowerPreview(type, growth, ref x, rect);

            for (int i = 0; i < attributeIds.Length; i++)
                DrawAttributeCells(growth, attributeIds[i], ref x, rect);
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

        private void DrawBaseProfileCell(CharacterActorType type, ref float x, Rect rect, PartyMemberGrowthSO growth)
        {
            using (new EditorGUI.DisabledScope(growth == null))
            {
                Rect objectRect = new(x + 2, rect.y + 2, ColObject - 58, rect.height - 4);
                EditorGUI.BeginChangeCheck();
                var newProfile = (AttributeProfileSO)EditorGUI.ObjectField(
                    objectRect,
                    growth != null ? growth.baseProfile : null,
                    typeof(AttributeProfileSO),
                    false);
                if (EditorGUI.EndChangeCheck() && growth != null)
                {
                    Undo.RecordObject(growth, "Set Growth Base Profile");
                    growth.baseProfile = newProfile;
                    EditorUtility.SetDirty(growth);
                }

                if (GUI.Button(new Rect(x + ColObject - 32, rect.y + 2, 30, rect.height - 4), "생성") && growth != null)
                    CreateBaseProfileAsset(type, growth);
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
            long power = PartyPowerCalculator.Calculate(
                type,
                growth,
                growth.initialLevel).CombatPower;
            GUI.Label(new Rect(x + 4, rect.y + 3, ColPower - 8, rect.height), power.ToString("#,0"), EditorStyles.miniLabel);
            x += ColPower;
        }

        private void DrawAttributeCells(
            PartyMemberGrowthSO growth,
            AttributeId attributeId,
            ref float x,
            Rect rect)
        {
            AttributeProfileSO baseProfile = growth.baseProfile;
            bool hasBase = baseProfile != null;
            float baseValue = hasBase
                              && baseProfile.TryGetBaseValue(
                                  attributeId, out float profileValue)
                ? profileValue
                : UPlayGroundAttributeDefaults.Get(attributeId);

            EditorGUI.BeginDisabledGroup(!hasBase);
            EditorGUI.BeginChangeCheck();
            float newBase = EditorGUI.FloatField(new Rect(x + 2, rect.y + 2, ColStatBase - 4, rect.height - 4), baseValue);
            if (EditorGUI.EndChangeCheck() && hasBase)
            {
                Undo.RecordObject(baseProfile, "Edit Growth Base Attribute");
                baseProfile.EditorSetBaseValue(attributeId, newBase);
                EditorUtility.SetDirty(baseProfile);
            }
            EditorGUI.EndDisabledGroup();
            x += ColStatBase;
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

        private void CreateBaseProfileAsset(CharacterActorType type, PartyMemberGrowthSO growth)
        {
            if (growth == null) return;
            EnsureFolder(_profileSavePath);

            var profile = CreateInstance<AttributeProfileSO>();
            var entries = new List<AttributeProfileEntry>();
            foreach (AttributeId attributeId in UPlayGroundAttributeDefaults.ProfileAttributes)
                entries.Add(new AttributeProfileEntry(
                    attributeId,
                    UPlayGroundAttributeDefaults.Get(attributeId)));
            profile.EditorReplace(entries);

            string path = AssetDatabase.GenerateUniqueAssetPath(
                $"{_profileSavePath}/AttributeProfile_Player_{type}.asset");
            AssetDatabase.CreateAsset(profile, path);

            Undo.RecordObject(growth, "Create Growth Base Profile");
            growth.baseProfile = profile;
            EditorUtility.SetDirty(growth);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private AttributeId[] GetVisibleAttributeIds()
        {
            for (int i = 0; i < Categories.Length; i++)
            {
                if (Categories[i].label == _statCategory)
                    return Categories[i].attributes;
            }

            return Categories[1].attributes;
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
