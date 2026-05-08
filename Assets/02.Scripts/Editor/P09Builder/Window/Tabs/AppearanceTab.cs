using System.Collections.Generic;
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
                        config.FacialHairSo = IconGridDrawer.Draw(
                            ToReadOnly(catalog.FacialHairs), config.FacialHairSo, iconResolver, preferredSex: config.Sex);
                    }
                }
            }
        }

        public IEnumerable<string> Validate(CharacterBuildConfig config)
        {
            yield break;
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
    }
}
