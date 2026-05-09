using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.P09Builder
{
    internal sealed class WeaponTab : IBuilderTab
    {
        public string Title => "무기";

        public void Initialize(P09CharacterPrefabBuilderWindow window, P09AssetCatalog catalog) { }

        public void OnGUI(CharacterBuildConfig config, P09AssetCatalog catalog, IconResolver iconResolver)
        {
            if (config == null || catalog == null) return;

            config.UseWeaponGroup = EditorGUILayout.Toggle("그룹 사용", config.UseWeaponGroup);

            EditorGUILayout.Space();

            if (config.UseWeaponGroup)
            {
                EditorGUILayout.LabelField("무기 그룹", EditorStyles.boldLabel);
                config.WeaponGroupSo = IconGridDrawer.Draw(
                    ToReadOnly(catalog.WeaponGroups), config.WeaponGroupSo, iconResolver, columns: 4);
            }
            else
            {
                EditorGUILayout.LabelField("개별 무기 지정", EditorStyles.boldLabel);

                EditorGUILayout.LabelField("검 (Sword)", EditorStyles.miniBoldLabel);
                var previousSword = config.SwordSo;
                config.SwordSo = IconGridDrawer.Draw(
                    ToReadOnly(catalog.Swords), config.SwordSo, iconResolver);
                ClearOtherMainWeaponsIfSelected(config, previousSword, config.SwordSo, MainWeaponSlot.Sword);
                ClearSubWeaponsIfSwordUnavailable(config);

                EditorGUILayout.Space(4);

                EditorGUILayout.LabelField("보조검 (SubSword)", EditorStyles.miniBoldLabel);
                using (new EditorGUI.DisabledScope(config.SwordSo == null))
                {
                    config.SubSwordSo = IconGridDrawer.Draw(
                        ToReadOnly(catalog.SubSwords), config.SubSwordSo, iconResolver);
                }

                EditorGUILayout.Space(4);

                EditorGUILayout.LabelField("대검 (GreatSword)", EditorStyles.miniBoldLabel);
                var previousGreatSword = config.GreatSwordSo;
                config.GreatSwordSo = IconGridDrawer.Draw(
                    ToReadOnly(catalog.GreatSwords), config.GreatSwordSo, iconResolver);
                ClearOtherMainWeaponsIfSelected(config, previousGreatSword, config.GreatSwordSo, MainWeaponSlot.GreatSword);

                EditorGUILayout.Space(4);

                EditorGUILayout.LabelField("방패 (Shield)", EditorStyles.miniBoldLabel);
                using (new EditorGUI.DisabledScope(config.SwordSo == null))
                {
                    config.ShieldSo = IconGridDrawer.Draw(
                        ToReadOnly(catalog.Shields), config.ShieldSo, iconResolver);
                }

                EditorGUILayout.Space(4);

                EditorGUILayout.LabelField("활 (Bow)", EditorStyles.miniBoldLabel);
                var previousBow = config.BowSo;
                config.BowSo = IconGridDrawer.Draw(
                    ToReadOnly(catalog.Bows), config.BowSo, iconResolver);
                ClearOtherMainWeaponsIfSelected(config, previousBow, config.BowSo, MainWeaponSlot.Bow);

                EditorGUILayout.Space(4);

                EditorGUILayout.LabelField("스태프 (Staff)", EditorStyles.miniBoldLabel);
                var previousStaff = config.StaffSo;
                config.StaffSo = IconGridDrawer.Draw(
                    ToReadOnly(catalog.Staves), config.StaffSo, iconResolver);
                ClearOtherMainWeaponsIfSelected(config, previousStaff, config.StaffSo, MainWeaponSlot.Staff);

                EditorGUILayout.Space(6);

                EditorGUILayout.LabelField("창 (Spear)", EditorStyles.miniBoldLabel);
                var previousSpear = config.SpearSo;
                config.SpearSo = IconGridDrawer.Draw(
                    ToReadOnly(catalog.Spears), config.SpearSo, iconResolver);
                ClearOtherMainWeaponsIfSelected(config, previousSpear, config.SpearSo, MainWeaponSlot.Spear);

                EditorGUILayout.Space(4);

                EditorGUILayout.LabelField("쌍도끼 (DualAxe)", EditorStyles.miniBoldLabel);
                var previousDualAxe = config.DualAxeSo;
                config.DualAxeSo = IconGridDrawer.Draw(
                    ToReadOnly(catalog.DualAxes), config.DualAxeSo, iconResolver);
                ClearOtherMainWeaponsIfSelected(config, previousDualAxe, config.DualAxeSo, MainWeaponSlot.DualAxe);

                EditorGUILayout.Space(4);

                EditorGUILayout.LabelField("채찍 (Whip)", EditorStyles.miniBoldLabel);
                var previousWhip = config.WhipSo;
                config.WhipSo = IconGridDrawer.Draw(
                    ToReadOnly(catalog.Whips), config.WhipSo, iconResolver);
                ClearOtherMainWeaponsIfSelected(config, previousWhip, config.WhipSo, MainWeaponSlot.Whip);

                EditorGUILayout.Space(6);
                config.ShowArrows = EditorGUILayout.Toggle("화살 표시", config.ShowArrows);
            }
        }

        public IEnumerable<string> Validate(CharacterBuildConfig config)
        {
            yield break;
        }

        private enum MainWeaponSlot
        {
            Sword,
            GreatSword,
            Bow,
            Staff,
            Spear,
            DualAxe,
            Whip,
        }

        private static void ClearOtherMainWeaponsIfSelected(
            CharacterBuildConfig config,
            ScriptableObject previousSelection,
            ScriptableObject currentSelection,
            MainWeaponSlot selectedSlot)
        {
            if (config == null || currentSelection == null || currentSelection == previousSelection)
                return;

            if (selectedSlot != MainWeaponSlot.Sword)      config.SwordSo = null;
            if (selectedSlot != MainWeaponSlot.GreatSword) config.GreatSwordSo = null;
            if (selectedSlot != MainWeaponSlot.Bow)        config.BowSo = null;
            if (selectedSlot != MainWeaponSlot.Staff)      config.StaffSo = null;
            if (selectedSlot != MainWeaponSlot.Spear)      config.SpearSo = null;
            if (selectedSlot != MainWeaponSlot.DualAxe)    config.DualAxeSo = null;
            if (selectedSlot != MainWeaponSlot.Whip)       config.WhipSo = null;

            ClearSubWeaponsIfSwordUnavailable(config);
        }

        private static void ClearSubWeaponsIfSwordUnavailable(CharacterBuildConfig config)
        {
            if (config == null || config.SwordSo != null)
                return;

            config.SubSwordSo = null;
            config.ShieldSo = null;
        }

        private static IReadOnlyList<ScriptableObject> ToReadOnly(List<ScriptableObject> list)
        {
            return list != null
                ? (IReadOnlyList<ScriptableObject>)list
                : System.Array.Empty<ScriptableObject>();
        }
    }
}
