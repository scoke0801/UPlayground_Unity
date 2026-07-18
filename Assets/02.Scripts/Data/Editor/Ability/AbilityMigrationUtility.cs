using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;

namespace UPlayGround.Data.Editor.Ability
{
    public static class AbilityMigrationUtility
    {
        public static AbilitySetSO Convert(PlayerAttackDataSO source, string folder)
        {
            if (source == null || string.IsNullOrWhiteSpace(folder)) return null;
            EnsureFolder(folder);

            var set = ScriptableObject.CreateInstance<AbilitySetSO>();
            string setPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{folder}/AbilitySet_{source.name}.asset");
            AssetDatabase.CreateAsset(set, setPath);
            Undo.RegisterCreatedObjectUndo(set, "Ability Set 생성");

            if (source.skillDefinitions != null)
            {
                for (int i = 0; i < source.skillDefinitions.Count; i++)
                {
                    PlayerSkillDefinition legacy = source.skillDefinitions[i];
                    if (legacy == null) continue;
                    GameplayAbilitySO ability = ConvertDefinition(source, legacy, folder);
                    set.playerSlots.Add(new AbilitySetSO.PlayerSlotEntry
                    {
                        slot = legacy.slot,
                        ability = ability,
                    });
                }
            }

            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            Selection.activeObject = set;
            Debug.Log(
                $"[AbilityMigration] '{source.name}'을 '{setPath}'에 변환했습니다. " +
                "기존 에셋은 변경하지 않았으며 비용/쿨다운 기본값은 반드시 비교 검토하세요.",
                set);
            return set;
        }

        private static GameplayAbilitySO ConvertDefinition(
            PlayerAttackDataSO source,
            PlayerSkillDefinition legacy,
            string folder)
        {
            var ability = ScriptableObject.CreateInstance<GameplayAbilitySO>();
            ability.name = $"GA_{source.name}_{legacy.slot}";
            ability.abilityId = $"Ability.Player.{source.name}.{legacy.slot}";
            ability.presentation.displayName = legacy.displayName;
            ability.presentation.category = legacy.slot == PlayerSkillSlot.Ultimate
                ? AbilityCategory.Ultimate
                : AbilityCategory.Attack;
            ability.cost.resourceType = legacy.slot == PlayerSkillSlot.Ultimate
                ? AbilityResourceType.UltimateEnergy
                : AbilityResourceType.None;
            ability.cost.policy = legacy.slot == PlayerSkillSlot.Ultimate
                ? AbilityCostPolicy.All
                : AbilityCostPolicy.None;
            ability.cooldown.durationSeconds = legacy.cooldownPolicy == SkillCooldownPolicy.NoCooldown
                ? 0f
                : legacy.slot == PlayerSkillSlot.Ultimate ? 12f : 3f;
            ability.cooldown.cooldownGroupId =
                $"Cooldown.Player.{source.name}.{legacy.slot}";

            if (legacy.variants != null)
                for (int i = 0; i < legacy.variants.Count; i++)
                {
                    AbilityVariantDefinition variant = ConvertVariant(legacy.variants[i], i);
                    if (variant != null && legacy.slot == PlayerSkillSlot.Ultimate)
                        variant.condition.requiresFullResource = true;
                    ability.variants.Add(variant);
                }

            string path = AssetDatabase.GenerateUniqueAssetPath(
                $"{folder}/{ability.name}.asset");
            AssetDatabase.CreateAsset(ability, path);
            Undo.RegisterCreatedObjectUndo(ability, "Gameplay Ability 생성");
            return ability;
        }

        private static AbilityVariantDefinition ConvertVariant(
            PlayerSkillVariant legacy,
            int index)
        {
            if (legacy == null) return null;
            var result = new AbilityVariantDefinition
            {
                variantId = string.IsNullOrWhiteSpace(legacy.variantName)
                    ? $"Variant_{index}"
                    : legacy.variantName,
                priority = legacy.priority,
                animKey = legacy.animKey,
                playerAttackInfo = Clone(legacy.attackInfo),
                condition = new AbilityVariantCondition
                {
                    groundCondition = legacy.condition?.groundCondition switch
                    {
                        SkillGroundCondition.Grounded => AbilityGroundCondition.Grounded,
                        SkillGroundCondition.Airborne => AbilityGroundCondition.Airborne,
                        _ => AbilityGroundCondition.Any,
                    },
                    minResource = legacy.condition?.minSkillGauge ?? 0f,
                    requiresFullResource = legacy.condition?.requiresFullSkillGauge ?? false,
                    requiredTagIds = legacy.condition?.requiredTagIds != null
                        ? new List<Gameplay.Tag.GameplayTagId>(legacy.condition.requiredTagIds)
                        : new List<Gameplay.Tag.GameplayTagId>(),
                    blockedTagIds = legacy.condition?.blockedTagIds != null
                        ? new List<Gameplay.Tag.GameplayTagId>(legacy.condition.blockedTagIds)
                        : new List<Gameplay.Tag.GameplayTagId>(),
                },
            };
            return result;
        }

        private static PlayerAttackInfo Clone(PlayerAttackInfo source)
        {
            if (source == null) return new PlayerAttackInfo();
            return JsonUtility.FromJson<PlayerAttackInfo>(JsonUtility.ToJson(source));
        }

        private static void EnsureFolder(string folder)
        {
            string[] parts = folder.Split('/');
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
