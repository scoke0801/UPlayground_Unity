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
            PartyMemberGrowthSO growthData)
        {
            Dictionary<AttributeId, float> stats =
                PartyPowerCalculator.CalculateBaseStats(growthData);
            ApplyEffectiveModifiers(type, stats);
            return stats;
        }

        public static Dictionary<AttributeId, float> Calculate(CharacterActorType type)
        {
            IPartyService party = Svc.Party;
            if (party == null)
                return BuildDefaultStats();

            return Calculate(type, party.GetGrowthData(type));
        }

        private static void ApplyEffectiveModifiers(
            CharacterActorType type,
            Dictionary<AttributeId, float> stats)
        {
            if (type == CharacterActorType.None || stats == null)
                return;

            List<AttributeModifierValue> modifiers = new();
            IInventoryService inventory = Svc.Inventory;
            if (inventory != null)
            {
                IReadOnlyList<ItemInstance> equipment =
                    inventory.GetEquippedItemInstances(type);
                for (int i = 0; i < equipment.Count; i++)
                {
                    ItemInstance instance = equipment[i];
                    if (instance?.data is not EquipmentSO equipmentData)
                        continue;

                    equipmentData.AddAttributeModifiersTo(modifiers);
                    AddRandomGrowthModifiers(instance, modifiers);
                }
            }

            IReadOnlyList<SkillStatModifierEntry> skillModifiers =
                Svc.Party?.GetSkillStatModifiers(type);
            if (skillModifiers != null)
                for (int i = 0; i < skillModifiers.Count; i++)
                    modifiers.Add(skillModifiers[i].ToRuntimeValue());

            // 런타임 AttributeSet은 출처와 무관하게 같은 Attribute의 Add/Percent를 합산하고
            // Multiply를 곱한 뒤 한 번에 계산한다. 장비와 스킬 트리를 따로 계산하면
            // Percent가 복리 적용되어 전투력/벤치 최대 체력이 실제 런타임과 달라진다.
            foreach (AttributeId attributeId in UPlayGroundAttributeDefaults.All)
                stats[attributeId] = ComputeFinal(
                    stats.TryGetValue(attributeId, out float baseValue)
                        ? baseValue
                        : UPlayGroundAttributeDefaults.Get(attributeId),
                    attributeId,
                    modifiers);
        }

        private static void AddRandomGrowthModifiers(
            ItemInstance instance,
            List<AttributeModifierValue> modifiers)
        {
            if (instance?.growthAttributeRolls == null)
                return;

            for (int i = 0; i < instance.growthAttributeRolls.Count; i++)
            {
                EquipmentGrowthAttributeRoll roll = instance.growthAttributeRolls[i];
                if (!roll.AttributeId.IsValid
                    || !GrowthAttributeCatalog.TryGetEquipmentFlatValuePerRank(
                        roll.AttributeId,
                        out float flatValuePerRank))
                {
                    Debug.LogError(
                        $"[CharacterEffectiveStatCalculator] " +
                        $"{roll.attributeId} 장비 옵션 수치가 없습니다.");
                    continue;
                }
                modifiers.Add(new AttributeModifierValue(
                    roll.AttributeId,
                    AttributeModifierOperation.Add,
                    flatValuePerRank * Mathf.Max(0, roll.rank)));
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
            bool hasOverride = false;
            float overrideValue = 0f;
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
                        // AttributeSetRuntime과 동일하게 Override가 있으면 다른 연산을 무시한다.
                        // AttributeModifierValue에는 우선순위 정보가 없으므로 런타임 적용 순서와
                        // 같은 목록의 마지막 값을 승자로 사용한다.
                        hasOverride = true;
                        overrideValue = modifier.Value;
                        break;
                }
            }

            return hasOverride
                ? overrideValue
                : (baseValue + flat) * (1f + percent) * multiply;
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
