using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.P09Builder
{
    internal sealed class AppearanceTab : IBuilderTab
    {
        public string Title => "외형";

        private bool _armorFoldout = true;
        private bool _hairFoldout = true;
        private bool _faceFoldout = true;
        private int _selectedArmorPresetIndex = -1;
        private static readonly Regex _facialHairNamePattern =
            new Regex(@"^(?:Male|Female|Fem)_FacialHair_(\d+)$", RegexOptions.Compiled);

        public void Initialize(P09CharacterPrefabBuilderWindow window, P09AssetCatalog catalog) { }

        public void OnGUI(CharacterBuildConfig config, P09AssetCatalog catalog, IconResolver iconResolver)
        {
            if (config == null || catalog == null) return;

            // ---------- 갑옷 ----------
            _armorFoldout = EditorGUILayout.Foldout(_armorFoldout, "갑옷", true);
            if (_armorFoldout)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    DrawArmorPresetSelector(config, catalog);
                    EditorGUILayout.Space(4);

                    foreach (var slot in BuilderArmorSlotExtensions.All)
                    {
                        EditorGUILayout.LabelField(slot.ToString(), EditorStyles.miniBoldLabel);
                        var items = GetItemsForSlot(slot, catalog);
                        var current = config.ArmorSelections != null
                            ? config.ArmorSelections.Get(slot)
                            : null;
                        var next = IconGridDrawer.Draw(items, current, iconResolver, preferredSex: config.Sex);
                        if (next != current && config.ArmorSelections != null)
                            config.ArmorSelections.Set(slot, next);
                        EditorGUILayout.Space(2);
                    }
                }
            }

            EditorGUILayout.Space();

            // ---------- 헤어 ----------
            _hairFoldout = EditorGUILayout.Foldout(_hairFoldout, "헤어", true);
            if (_hairFoldout)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.LabelField("헤어스타일", EditorStyles.miniBoldLabel);
                    config.HairStyleSo = IconGridDrawer.Draw(
                        ToReadOnly(catalog.HairStyles), config.HairStyleSo, iconResolver, preferredSex: config.Sex);

                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("헤어 색상", EditorStyles.miniBoldLabel);
                    config.HairColorSo = ColorSwatchDrawer.Draw(
                        ToReadOnly(catalog.HairColors), config.HairColorSo, iconResolver, allowNone: false);
                }
            }

            EditorGUILayout.Space();

            // ---------- 얼굴 / 피부 ----------
            _faceFoldout = EditorGUILayout.Foldout(_faceFoldout, "얼굴 / 피부", true);
            if (_faceFoldout)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    config.FaceTypeSo = EditorGUILayout.ObjectField(
                        "얼굴 타입", config.FaceTypeSo, typeof(ScriptableObject), false) as ScriptableObject;

                    config.EmotionSo = EditorGUILayout.ObjectField(
                        "표정", config.EmotionSo, typeof(ScriptableObject), false) as ScriptableObject;

                    EditorGUILayout.LabelField("눈 색상", EditorStyles.miniBoldLabel);
                    config.EyeColorSo = ColorSwatchDrawer.Draw(
                        ToReadOnly(catalog.EyeColors), config.EyeColorSo, iconResolver, allowNone: false);

                    EditorGUILayout.LabelField("피부 색상", EditorStyles.miniBoldLabel);
                    var skins = config.Sex == BuilderSex.Male
                        ? ToReadOnly(catalog.SkinColorsMale)
                        : ToReadOnly(catalog.SkinColorsFemale);
                    config.SkinColorSo = ColorSwatchDrawer.Draw(
                        skins, config.SkinColorSo, iconResolver, allowNone: false);

                    if (config.Sex == BuilderSex.Male)
                    {
                        EditorGUILayout.LabelField("수염", EditorStyles.miniBoldLabel);
                        int maxId = GetAttachedFacialHairMaxId(config);
                        if (maxId > 0)
                        {
                            config.FacialHairId = IconGridDrawer.DrawNumberedOptions(maxId, config.FacialHairId);
                            config.FacialHairSo = null;
                        }
                        else
                        {
                            config.FacialHairSo = IconGridDrawer.Draw(
                                ToReadOnly(catalog.FacialHairs), config.FacialHairSo, iconResolver, preferredSex: config.Sex);
                        }
                    }
                }
            }
        }

        public IEnumerable<string> Validate(CharacterBuildConfig config)
        {
            yield break;
        }

        private void DrawArmorPresetSelector(CharacterBuildConfig config, P09AssetCatalog catalog)
        {
            var presets = ArmorIndexPresetUtility.Build(catalog);
            if (presets.Count == 0)
            {
                EditorGUILayout.HelpBox("갑옷 프리셋을 만들 수 있는 카탈로그 데이터가 없습니다.", MessageType.Info);
                return;
            }

            int currentIndex = ArmorIndexPresetUtility.GetCurrentPresetIndex(config.ArmorSelections);
            if (currentIndex >= 0)
                _selectedArmorPresetIndex = currentIndex;

            var labels = new string[presets.Count + 1];
            labels[0] = currentIndex >= 0 ? $"현재: Armor {currentIndex:00}" : "Custom";
            for (int i = 0; i < presets.Count; i++)
                labels[i + 1] = presets[i].DisplayName;

            int popupIndex = 0;
            for (int i = 0; i < presets.Count; i++)
            {
                if (presets[i].Index == _selectedArmorPresetIndex)
                {
                    popupIndex = i + 1;
                    break;
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                int nextPopupIndex = EditorGUILayout.Popup("갑옷 프리셋", popupIndex, labels);
                if (nextPopupIndex > 0 && nextPopupIndex != popupIndex)
                {
                    var preset = presets[nextPopupIndex - 1];
                    _selectedArmorPresetIndex = preset.Index;
                    ArmorIndexPresetUtility.Apply(config.ArmorSelections, preset);
                    GUI.changed = true;
                }

                using (new EditorGUI.DisabledScope(_selectedArmorPresetIndex < 0))
                {
                    if (GUILayout.Button("적용", GUILayout.Width(52f)))
                    {
                        var preset = presets.Find(p => p.Index == _selectedArmorPresetIndex);
                        if (preset != null)
                        {
                            ArmorIndexPresetUtility.Apply(config.ArmorSelections, preset);
                            GUI.changed = true;
                        }
                    }
                }
            }
        }

        private static IReadOnlyList<ScriptableObject> GetItemsForSlot(BuilderArmorSlot slot, P09AssetCatalog catalog)
        {
            switch (slot)
            {
                case BuilderArmorSlot.Head:  return catalog.Heads;
                case BuilderArmorSlot.Chest: return catalog.Chests;
                case BuilderArmorSlot.Arm:   return catalog.Arms;
                case BuilderArmorSlot.Waist: return catalog.Waists;
                case BuilderArmorSlot.Leg:   return catalog.Legs;
                default: return System.Array.Empty<ScriptableObject>();
            }
        }

        private static IReadOnlyList<ScriptableObject> ToReadOnly(List<ScriptableObject> list)
        {
            return list != null
                ? (IReadOnlyList<ScriptableObject>)list
                : System.Array.Empty<ScriptableObject>();
        }

        private static int GetAttachedFacialHairMaxId(CharacterBuildConfig config)
        {
            string prefabPath = PathConfig.GetBasePrefabPath(BuilderSex.Male, config != null && config.UseMagicaCloth);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return 0;

            int maxId = 0;
            var transforms = prefab.GetComponentsInChildren<Transform>(includeInactive: true);
            foreach (var t in transforms)
            {
                if (t == null) continue;
                var match = _facialHairNamePattern.Match(t.name);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int id))
                    maxId = Mathf.Max(maxId, id);
            }

            return maxId;
        }
    }
}
