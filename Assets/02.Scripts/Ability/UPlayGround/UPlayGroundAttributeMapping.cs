using System;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Stat;

namespace UPlayGround.Ability.UPlayGround
{
    /// <summary>레거시 StatType과 안정 Attribute ID 사이의 전환기 단일 매핑.</summary>
    public static class UPlayGroundAttributeMapping
    {
        public static bool TryGetAttributeId(StatType statType, out AttributeId attributeId)
        {
            attributeId = statType switch
            {
                StatType.MaxHealth => AttributeIds.Vital.MaxHealth,
                StatType.HealthRegenRate => AttributeIds.Vital.HealthRegenRate,
                StatType.AttackPower => AttributeIds.Combat.AttackPower,
                StatType.Defense => AttributeIds.Combat.Defense,
                StatType.CritRate => AttributeIds.Combat.CritRate,
                StatType.CritMultiplier => AttributeIds.Combat.CritMultiplier,
                StatType.AttackSpeed => AttributeIds.Combat.AttackSpeed,
                StatType.MoveSpeed => AttributeIds.Movement.MoveSpeed,
                StatType.DashDistance => AttributeIds.Movement.DashDistance,
                StatType.MaxPoise => AttributeIds.Vital.MaxPoise,
                StatType.PoiseRecoveryRate => AttributeIds.Vital.PoiseRecoveryRate,
                StatType.PoiseRecoveryDelay => AttributeIds.Vital.PoiseRecoveryDelay,
                StatType.SkillGaugeRate => AttributeIds.Resource.GenerationMultiplier,
                StatType.InvincibleDuration => AttributeIds.Combat.InvincibleDurationMultiplier,
                StatType.GatheringPower => AttributeIds.Life.GatheringPower,
                _ => default,
            };
            return attributeId.IsValid;
        }

        public static AttributeId GetAttributeId(StatType statType)
        {
            if (TryGetAttributeId(statType, out AttributeId attributeId)) return attributeId;
            throw new ArgumentOutOfRangeException(nameof(statType), statType, "Attribute 매핑이 없습니다.");
        }
    }
}
