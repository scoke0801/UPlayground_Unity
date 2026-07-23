using System;

namespace UPlayGround.Ability.Core
{
    public static class GameplayDataTags
    {
        public static readonly AbilityTagId Damage = new("Data.Damage");
        public static readonly AbilityTagId ResolvedDamage = new("Data.ResolvedDamage");
        public static readonly AbilityTagId DamageTakenMultiplier = new("Data.DamageTakenMultiplier");
        public static readonly AbilityTagId ElementMultiplier = new("Data.ElementMultiplier");
        public static readonly AbilityTagId CriticalMultiplier = new("Data.CriticalMultiplier");
        public static readonly AbilityTagId HealAmount = new("Data.HealAmount");
        public static readonly AbilityTagId HealPercent = new("Data.HealPercent");
        public static readonly AbilityTagId PoiseDamage = new("Data.PoiseDamage");
        public static readonly AbilityTagId BreakDamage = new("Data.BreakDamage");
    }

    public sealed class DamageExecution : IGameplayEffectExecution
    {
        public bool Execute(
            in GameplayEffectExecutionInput input,
            GameplayEffectExecutionOutput output,
            out string error)
        {
            float finalDamage;
            if (input.TryGetSetByCaller(GameplayDataTags.ResolvedDamage, out float resolved))
            {
                finalDamage = Math.Max(0f, resolved);
            }
            else
            {
                if (!input.TryGetSetByCaller(GameplayDataTags.Damage, out float baseDamage))
                {
                    error = $"필수 SetByCaller 누락: {GameplayDataTags.Damage}";
                    return false;
                }
                float attackPower = input.GetSource(AttributeIds.Combat.AttackPower);
                if (attackPower <= 0f) attackPower = 1f;
                float defense = Math.Min(Math.Max(input.GetTarget(AttributeIds.Combat.Defense), 0f), 1f);
                float damageTaken = GetOptional(input, GameplayDataTags.DamageTakenMultiplier, 1f);
                float element = GetOptional(input, GameplayDataTags.ElementMultiplier, 1f);
                float critical = Math.Max(1f, GetOptional(input, GameplayDataTags.CriticalMultiplier, 1f));
                finalDamage = Calculate(
                    baseDamage, attackPower, defense, damageTaken, element, critical);
            }

            output.AddBaseDelta(AttributeIds.Vital.Health, -finalDamage);
            error = string.Empty;
            return true;
        }

        public static float Calculate(
            float baseDamage,
            float attackPower,
            float defenseRate,
            float damageTakenMultiplier,
            float elementMultiplier,
            float criticalMultiplier)
        {
            float normalizedBase = Math.Max(0f, baseDamage);
            float finalDamage = normalizedBase
                                * Math.Max(0f, attackPower)
                                * (1f - Math.Min(Math.Max(defenseRate, 0f), 1f))
                                * Math.Max(0f, damageTakenMultiplier)
                                * Math.Max(0f, elementMultiplier)
                                * Math.Max(1f, criticalMultiplier);
            return normalizedBase > 0f ? Math.Max(1f, finalDamage) : 0f;
        }

        private static float GetOptional(
            in GameplayEffectExecutionInput input,
            AbilityTagId key,
            float fallback) => input.TryGetSetByCaller(key, out float value) ? value : fallback;
    }

    public sealed class HealingExecution : IGameplayEffectExecution
    {
        public bool Execute(
            in GameplayEffectExecutionInput input,
            GameplayEffectExecutionOutput output,
            out string error)
        {
            bool hasFlat = input.TryGetSetByCaller(GameplayDataTags.HealAmount, out float flat);
            bool hasPercent = input.TryGetSetByCaller(GameplayDataTags.HealPercent, out float percent);
            if (!hasFlat && !hasPercent)
            {
                error = $"필수 SetByCaller 누락: {GameplayDataTags.HealAmount}/{GameplayDataTags.HealPercent}";
                return false;
            }
            float amount = Math.Max(0f, flat)
                + Math.Max(0f, percent) * input.GetTarget(AttributeIds.Vital.MaxHealth);
            output.AddBaseDelta(AttributeIds.Vital.Health, amount);
            error = string.Empty;
            return true;
        }
    }

    public sealed class PoiseDamageExecution : IGameplayEffectExecution
    {
        public bool Execute(
            in GameplayEffectExecutionInput input,
            GameplayEffectExecutionOutput output,
            out string error)
        {
            if (!input.TryGetSetByCaller(GameplayDataTags.PoiseDamage, out float damage))
            {
                error = $"필수 SetByCaller 누락: {GameplayDataTags.PoiseDamage}";
                return false;
            }
            output.AddBaseDelta(AttributeIds.Vital.Poise, -Math.Max(0f, damage));
            error = string.Empty;
            return true;
        }
    }
}
