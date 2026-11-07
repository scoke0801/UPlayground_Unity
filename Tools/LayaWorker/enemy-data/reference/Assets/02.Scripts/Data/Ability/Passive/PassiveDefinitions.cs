using System;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Stat;

namespace UPlayGround.Data.Ability
{
    public enum PassiveActivationType
    {
        Always,
        PerfectDodge,
        PerfectGuard,
    }

    public enum PassiveScope
    {
        ActiveCharacter,
        OwnerCharacter,
        BattlePartyHighest,
    }

    public enum PassiveStackPolicy
    {
        Additive,
        HighestOnly,
    }

    public enum PassiveModifierType
    {
        LightAttackDamage,
        HeavyAttackDamage,
        SkillDamage,
        SkillCooldownDuration,
        BreakDamage,
        EquipmentGrowthRankLuck,
        ConsumableRecovery,
        CraftIngredientCost,
        ExperienceGain,
        HarmfulEffectDuration,
        BeneficialEffectDuration,
    }

    [Flags]
    public enum PassiveAbilitySlotFilter
    {
        None = 0,
        Ability = 1 << 0,
        Ultimate = 1 << 1,
        All = Ability | Ultimate,
    }

    [Serializable]
    public sealed class PassiveModifierDefinition
    {
        public PassiveModifierType modifierType;
        public ModifierType operation = ModifierType.Percent;
        public float value;
        public PassiveAbilitySlotFilter abilitySlotFilter = PassiveAbilitySlotFilter.Ability;

        public bool Matches(PlayerSkillSlot slot)
        {
            PassiveAbilitySlotFilter flag = slot == PlayerSkillSlot.Ultimate
                ? PassiveAbilitySlotFilter.Ultimate
                : PassiveAbilitySlotFilter.Ability;
            return (abilitySlotFilter & flag) != 0;
        }
    }
}
