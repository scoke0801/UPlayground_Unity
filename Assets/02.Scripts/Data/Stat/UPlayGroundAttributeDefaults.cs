using UPlayGround.Ability.Core;

namespace UPlayGround.Data.Stat
{
    /// <summary>프로젝트 Attribute의 Profile 미지정 기본값.</summary>
    public static class UPlayGroundAttributeDefaults
    {
        public static readonly AttributeId[] All =
        {
            AttributeIds.Vital.MaxHealth,
            AttributeIds.Vital.HealthRegenRate,
            AttributeIds.Combat.AttackPower,
            AttributeIds.Combat.Defense,
            AttributeIds.Combat.CritRate,
            AttributeIds.Combat.CritMultiplier,
            AttributeIds.Combat.AttackSpeed,
            AttributeIds.Movement.MoveSpeed,
            AttributeIds.Movement.DashDistance,
            AttributeIds.Vital.MaxPoise,
            AttributeIds.Vital.PoiseRecoveryRate,
            AttributeIds.Vital.PoiseRecoveryDelay,
            AttributeIds.Resource.GenerationMultiplier,
            AttributeIds.Combat.InvincibleDurationMultiplier,
            AttributeIds.Life.GatheringPower,
        };

        public static float Get(AttributeId attributeId)
        {
            if (attributeId == AttributeIds.Vital.MaxHealth) return 100f;
            if (attributeId == AttributeIds.Vital.HealthRegenRate) return 0f;
            if (attributeId == AttributeIds.Combat.AttackPower) return 1f;
            if (attributeId == AttributeIds.Combat.Defense) return 0f;
            if (attributeId == AttributeIds.Combat.CritRate) return 0f;
            if (attributeId == AttributeIds.Combat.CritMultiplier) return 1.5f;
            if (attributeId == AttributeIds.Combat.AttackSpeed) return 1f;
            if (attributeId == AttributeIds.Movement.MoveSpeed) return 1f;
            if (attributeId == AttributeIds.Movement.DashDistance) return 1f;
            if (attributeId == AttributeIds.Vital.MaxPoise) return 100f;
            if (attributeId == AttributeIds.Vital.PoiseRecoveryRate) return 40f;
            if (attributeId == AttributeIds.Vital.PoiseRecoveryDelay) return 2f;
            if (attributeId == AttributeIds.Resource.GenerationMultiplier) return 1f;
            if (attributeId == AttributeIds.Combat.InvincibleDurationMultiplier) return 1f;
            if (attributeId == AttributeIds.Life.GatheringPower) return 1f;
            return 0f;
        }
    }
}
