using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Item.Editor
{
    public static class WeaponDefinitionGenerator
    {
        private const string DefaultSaveFolder = "Assets/10.Datas/Item/WeaponDefinition";

        [MenuItem("UPlayGround/Gameplay/Item/WeaponDefinition/Create Missing Definitions")]
        public static void CreateMissingWeaponDefinitions()
        {
            GenerateWeaponDefinitions(false);
        }

        [MenuItem("UPlayGround/Gameplay/Item/WeaponDefinition/Regenerate All Definitions")]
        public static void RegenerateAllWeaponDefinitions()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "WeaponDefinition 전체 재생성",
                "기존 WeaponDefinitionSO의 EquipStyle, MotionWeaponType, RequiresDrawStateForAttackMotion, ConstraintAliases가 기본값으로 덮어써집니다.",
                "재생성",
                "취소");

            if (!confirmed)
                return;

            GenerateWeaponDefinitions(true);
        }

        private static void GenerateWeaponDefinitions(bool overwriteExisting)
        {
            EnsureFolder(DefaultSaveFolder);

            int createdCount = 0;
            int updatedCount = 0;
            int skippedCount = 0;
            Dictionary<WeaponType, WeaponDefinitionSO> existingDefinitions = LoadExistingDefinitions();

            foreach (WeaponType weaponType in Enum.GetValues(typeof(WeaponType)))
            {
                if (weaponType == WeaponType.NoWeapon)
                    continue;

                WeaponDefinitionSO definition;
                if (!existingDefinitions.TryGetValue(weaponType, out definition) || definition == null)
                {
                    definition = ScriptableObject.CreateInstance<WeaponDefinitionSO>();
                    string path = AssetDatabase.GenerateUniqueAssetPath($"{DefaultSaveFolder}/WD_{weaponType}.asset");
                    AssetDatabase.CreateAsset(definition, path);
                    createdCount++;
                }
                else
                {
                    if (!overwriteExisting)
                    {
                        skippedCount++;
                        continue;
                    }

                    Undo.RecordObject(definition, "Update Weapon Definition");
                    updatedCount++;
                }

                ApplyPreset(definition, weaponType);
                EditorUtility.SetDirty(definition);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[WeaponDefinitionGenerator] WeaponDefinition 생성/갱신 완료. created={createdCount}, updated={updatedCount}, skipped={skippedCount}, folder={DefaultSaveFolder}");
        }

        private static Dictionary<WeaponType, WeaponDefinitionSO> LoadExistingDefinitions()
        {
            var result = new Dictionary<WeaponType, WeaponDefinitionSO>();
            string[] guids = AssetDatabase.FindAssets("t:WeaponDefinitionSO");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                WeaponDefinitionSO definition = AssetDatabase.LoadAssetAtPath<WeaponDefinitionSO>(path);
                if (definition == null || definition.weaponType == WeaponType.NoWeapon)
                    continue;

                if (!result.ContainsKey(definition.weaponType))
                    result.Add(definition.weaponType, definition);
            }

            return result;
        }

        private static void ApplyPreset(WeaponDefinitionSO definition, WeaponType weaponType)
        {
            definition.weaponType = weaponType;
            definition.equipStyle = GetEquipStyle(weaponType);
            definition.motionWeaponType = WeaponType.NoWeapon;
            definition.requiresDrawStateForAttackMotion = true;

            definition.constraintAliases.Clear();
            AddAliases(definition, GetAliases(weaponType));
        }

        private static WeaponEquipStyle GetEquipStyle(WeaponType weaponType)
        {
            switch (weaponType)
            {
                case WeaponType.Arrow:
                    return WeaponEquipStyle.SingleLeft;
                case WeaponType.SwordShield:
                    return WeaponEquipStyle.RightWithSub;
                case WeaponType.DualBlade:
                    return WeaponEquipStyle.PairedBothHands;
                default:
                    return WeaponEquipStyle.SingleRight;
            }
        }

        private static string[] GetAliases(WeaponType weaponType)
        {
            switch (weaponType)
            {
                case WeaponType.Sword:
                    return new[] { "sword" };
                case WeaponType.SwordShield:
                    return new[] { "sword", "shield" };
                case WeaponType.GreatSword:
                    return new[] { "greatsword", "great_sword", "claymore" };
                case WeaponType.Staff:
                    return new[] { "staff" };
                case WeaponType.Bow:
                    return new[] { "bow" };
                case WeaponType.Arrow:
                    return new[] { "arrow" };
                case WeaponType.Katana:
                    return new[] { "katana", "sword" };
                case WeaponType.DoubleAxe:
                    return new[] { "doubleaxe", "double_axe", "axe" };
                case WeaponType.Whip:
                    return new[] { "whip" };
                case WeaponType.Spear:
                    return new[] { "spear", "lance" };
                case WeaponType.DualBlade:
                    return new[] { "dualblade", "dual_blade", "doubleblade", "double_blade", "blade", "sword" };
                default:
                    return Array.Empty<string>();
            }
        }

        private static void AddAliases(WeaponDefinitionSO definition, IReadOnlyList<string> aliases)
        {
            for (int i = 0; i < aliases.Count; i++)
            {
                definition.constraintAliases.Add(new WeaponDefinitionSO.Alias
                {
                    value = aliases[i],
                });
            }
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);

                current = next;
            }
        }
    }
}
