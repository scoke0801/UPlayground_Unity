using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Stat;
using UPlayGround.Manager;
using UPlayGround.Data.Item;

namespace UPlayGround.Data.Party
{
    public static class CharacterEffectiveStatCalculator
    {
        public static Dictionary<StatType, float> Calculate(
            CharacterActorType type,
            PartyMemberGrowthSO growthData,
            int level,
            IReadOnlyDictionary<GrowthAttributeType, int> investments = null)
        {
            Dictionary<StatType, float> stats = PartyPowerCalculator.CalculateGrowthStats(growthData, level, investments);
            ApplyEquipmentStats(type, growthData, stats);
            return stats;
        }

        public static Dictionary<StatType, float> Calculate(CharacterActorType type)
        {
            IPartyService party = Svc.Party;
            if (party == null)
                return BuildDefaultStats();

            return Calculate(type, party.GetGrowthData(type), party.GetLevel(type), party.GetGrowthInvestments(type));
        }

        private static void ApplyEquipmentStats(
            CharacterActorType type,
            PartyMemberGrowthSO growthData,
            Dictionary<StatType, float> stats)
        {
            if (type == CharacterActorType.None || stats == null)
                return;

            IInventoryService inventory = Svc.Inventory;
            if (inventory == null)
                return;

            List<StatModifier> modifiers = new();
            IReadOnlyList<ItemInstance> equipment = inventory.GetEquippedItemInstances(type);
            for (int i = 0; i < equipment.Count; i++)
            {
                ItemInstance instance = equipment[i];
                if (instance?.data is not EquipmentSO equipmentData)
                    continue;

                equipmentData.AddStatModifiersTo(modifiers, instance);
                AddRandomGrowthModifiers(growthData, instance, modifiers);
            }

            foreach (StatType statType in Enum.GetValues(typeof(StatType)))
                stats[statType] = ComputeFinal(stats.TryGetValue(statType, out float baseValue)
                    ? baseValue
                    : ActorStatSO.GetDefault(statType), statType, modifiers);
        }

        private static void AddRandomGrowthModifiers(
            PartyMemberGrowthSO growthData,
            ItemInstance instance,
            List<StatModifier> modifiers)
        {
            if (growthData == null || instance?.growthAttributeRolls == null)
                return;

            for (int i = 0; i < instance.growthAttributeRolls.Count; i++)
            {
                EquipmentGrowthAttributeRoll roll = instance.growthAttributeRolls[i];
                growthData.TryGetInvestmentRule(roll.attributeType, out GrowthInvestmentRule rule);
                modifiers.Add(new StatModifier(
                    rule.statType,
                    ModifierType.Flat,
                    rule.flatPerRank * Mathf.Max(0, roll.rank),
                    instance));
            }
        }

        private static float ComputeFinal(float baseValue, StatType type, List<StatModifier> modifiers)
        {
            float flat = 0f;
            float percent = 0f;
            float multiply = 1f;

            for (int i = 0; i < modifiers.Count; i++)
            {
                StatModifier modifier = modifiers[i];
                if (modifier.statType != type)
                    continue;

                switch (modifier.modifierType)
                {
                    case ModifierType.Flat:
                        flat += modifier.value;
                        break;
                    case ModifierType.Percent:
                        percent += modifier.value;
                        break;
                    case ModifierType.Multiply:
                        multiply *= modifier.value;
                        break;
                }
            }

            return (baseValue + flat) * (1f + percent) * multiply;
        }

        private static Dictionary<StatType, float> BuildDefaultStats()
        {
            Dictionary<StatType, float> stats = new();
            foreach (StatType type in Enum.GetValues(typeof(StatType)))
                stats[type] = ActorStatSO.GetDefault(type);
            return stats;
        }
    }
}
