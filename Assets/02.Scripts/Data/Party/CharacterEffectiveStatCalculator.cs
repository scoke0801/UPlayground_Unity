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
            int level)
        {
            Dictionary<StatType, float> stats = PartyPowerCalculator.CalculateGrowthStats(growthData, level);
            ApplyEquipmentStats(type, stats);
            return stats;
        }

        public static Dictionary<StatType, float> Calculate(CharacterActorType type)
        {
            PartyManager party = PartyManager.Instance;
            if (party == null)
                return BuildDefaultStats();

            return Calculate(type, party.GetGrowthData(type), party.GetLevel(type));
        }

        private static void ApplyEquipmentStats(CharacterActorType type, Dictionary<StatType, float> stats)
        {
            if (type == CharacterActorType.None || stats == null)
                return;

            InventoryManager inventory = InventoryManager.Instance;
            if (inventory == null)
                return;

            List<StatModifier> modifiers = new();
            IReadOnlyList<EquipmentSO> equipment = inventory.GetEquippedEquipment(type);
            for (int i = 0; i < equipment.Count; i++)
                equipment[i]?.AddStatModifiersTo(modifiers, equipment[i]);

            foreach (StatType statType in Enum.GetValues(typeof(StatType)))
                stats[statType] = ComputeFinal(stats.TryGetValue(statType, out float baseValue)
                    ? baseValue
                    : ActorStatSO.GetDefault(statType), statType, modifiers);
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
