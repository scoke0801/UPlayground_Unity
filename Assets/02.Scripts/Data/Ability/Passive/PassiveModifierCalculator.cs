using System;
using System.Collections.Generic;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Stat;

namespace UPlayGround.Data.Ability
{
    public static class PassiveModifierCalculator
    {
        public static float CalculateMultiplier(
            CharacterPassiveSetSO set,
            PassiveModifierType type,
            PlayerSkillSlot? slot = null,
            PassiveScope? scopeA = null,
            PassiveScope? scopeB = null)
        {
            if (set?.passives == null)
                return 1f;

            float flat = 0f;
            float percent = 0f;
            float multiply = 1f;
            for (int i = 0; i < set.passives.Count; i++)
            {
                PassiveAbilitySO passive = set.passives[i];
                if (passive == null
                    || passive.activationType != PassiveActivationType.Always
                    || !MatchesScope(passive.scope, scopeA, scopeB)
                    || passive.modifiers == null)
                {
                    continue;
                }

                for (int j = 0; j < passive.modifiers.Count; j++)
                {
                    PassiveModifierDefinition modifier = passive.modifiers[j];
                    if (modifier == null || modifier.modifierType != type)
                        continue;
                    if (slot.HasValue
                        && type == PassiveModifierType.SkillCooldownDuration
                        && !modifier.Matches(slot.Value))
                    {
                        continue;
                    }

                    switch (modifier.operation)
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
            }

            return Math.Max(0f, (1f + flat) * (1f + percent) * multiply);
        }

        private static bool MatchesScope(
            PassiveScope scope,
            PassiveScope? scopeA,
            PassiveScope? scopeB)
        {
            if (!scopeA.HasValue && !scopeB.HasValue)
                return true;
            return scopeA.HasValue && scope == scopeA.Value
                   || scopeB.HasValue && scope == scopeB.Value;
        }

        public static int CalculateIngredientCost(
            int baseRequired,
            int quantity,
            float multiplier)
        {
            if (baseRequired <= 0 || quantity <= 0)
                return 0;
            float safeMultiplier = Math.Max(0.01f, multiplier);
            return Math.Max(
                1,
                (int)Math.Ceiling(baseRequired * (double)quantity * safeMultiplier));
        }

        public static long CalculateExperience(long amount, float multiplier)
        {
            if (amount <= 0)
                return 0;
            double value = amount * Math.Max(0d, multiplier);
            long result = (long)Math.Round(value, MidpointRounding.AwayFromZero);
            return Math.Max(1L, result);
        }

        public static float CalculateEffectDuration(
            float baseDuration,
            GameplayEffectPolarity polarity,
            float passiveMultiplier)
        {
            if (baseDuration <= 0f)
                return 0f;
            float safeMultiplier = Math.Max(0.01f, passiveMultiplier);
            return polarity switch
            {
                GameplayEffectPolarity.Beneficial => baseDuration * safeMultiplier,
                GameplayEffectPolarity.Harmful => baseDuration / safeMultiplier,
                _ => baseDuration,
            };
        }
    }
}
