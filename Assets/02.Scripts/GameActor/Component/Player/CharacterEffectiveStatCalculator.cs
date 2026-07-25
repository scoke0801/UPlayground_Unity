using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Stat;
using UPlayGround.Manager;
using UPlayGround.Data.Item;

namespace UPlayGround.Data.Party
{
    public static class CharacterEffectiveStatCalculator
    {
        public static Dictionary<AttributeId, float> Calculate(
            CharacterActorType type,
            PartyMemberGrowthSO growthData,
            int level,
            IReadOnlyDictionary<AttributeId, int> investments = null)
        {
            Dictionary<AttributeId, float> stats =
                PartyPowerCalculator.CalculateGrowthStats(
                    growthData, level, investments);
            ApplyEquipmentStats(type, growthData, stats);
            return stats;
        }

        public static Dictionary<AttributeId, float> Calculate(CharacterActorType type)
        {
            IPartyService party = Svc.Party;
            if (party == null)
                return BuildDefaultStats();

            return Calculate(type, party.GetGrowthData(type), party.GetLevel(type), party.GetGrowthInvestments(type));
        }

        private static void ApplyEquipmentStats(
            CharacterActorType type,
            PartyMemberGrowthSO growthData,
            Dictionary<AttributeId, float> stats)
        {
            if (type == CharacterActorType.None || stats == null)
                return;

            IInventoryService inventory = Svc.Inventory;
            if (inventory == null)
                return;

            List<AttributeModifierValue> modifiers = new();
            IReadOnlyList<ItemInstance> equipment = inventory.GetEquippedItemInstances(type);
            for (int i = 0; i < equipment.Count; i++)
            {
                ItemInstance instance = equipment[i];
                if (instance?.data is not EquipmentSO equipmentData)
                    continue;

                equipmentData.AddAttributeModifiersTo(modifiers);
                AddRandomGrowthModifiers(growthData, instance, modifiers);
            }

            foreach (AttributeId attributeId in UPlayGroundAttributeDefaults.All)
                stats[attributeId] = ComputeFinal(
                    stats.TryGetValue(attributeId, out float baseValue)
                    ? baseValue
                    : UPlayGroundAttributeDefaults.Get(attributeId),
                    attributeId,
                    modifiers);
        }

        private static void AddRandomGrowthModifiers(
            PartyMemberGrowthSO growthData,
            ItemInstance instance,
            List<AttributeModifierValue> modifiers)
        {
            if (growthData == null || instance?.growthAttributeRolls == null)
                return;

            for (int i = 0; i < instance.growthAttributeRolls.Count; i++)
            {
                EquipmentGrowthAttributeRoll roll = instance.growthAttributeRolls[i];
                if (!roll.AttributeId.IsValid
                    || !growthData.TryGetInvestmentRule(
                        roll.AttributeId,
                        out GrowthInvestmentRule rule))
                {
                    Debug.LogError(
                        $"[CharacterEffectiveStatCalculator] {growthData.name}의 " +
                        $"{roll.attributeId} 성장 규칙이 없습니다.",
                        growthData);
                    continue;
                }
                modifiers.Add(new AttributeModifierValue(
                    rule.AttributeId,
                    AttributeModifierOperation.Add,
                    rule.flatPerRank * Mathf.Max(0, roll.rank)));
            }
        }

        private static float ComputeFinal(
            float baseValue,
            AttributeId attributeId,
            List<AttributeModifierValue> modifiers)
        {
            float flat = 0f;
            float percent = 0f;
            float multiply = 1f;
            for (int i = 0; i < modifiers.Count; i++)
            {
                AttributeModifierValue modifier = modifiers[i];
                if (modifier.AttributeId != attributeId)
                    continue;

                switch (modifier.Operation)
                {
                    case AttributeModifierOperation.Add:
                        flat += modifier.Value;
                        break;
                    case AttributeModifierOperation.Percent:
                        percent += modifier.Value;
                        break;
                    case AttributeModifierOperation.Multiply:
                        multiply *= modifier.Value;
                        break;
                    case AttributeModifierOperation.Override:
                        baseValue = modifier.Value;
                        break;
                }
            }

            return (baseValue + flat) * (1f + percent) * multiply;
        }

        private static Dictionary<AttributeId, float> BuildDefaultStats()
        {
            Dictionary<AttributeId, float> stats = new();
            foreach (AttributeId attributeId in UPlayGroundAttributeDefaults.All)
                stats[attributeId] =
                    UPlayGroundAttributeDefaults.Get(attributeId);
            return stats;
        }
    }
}
