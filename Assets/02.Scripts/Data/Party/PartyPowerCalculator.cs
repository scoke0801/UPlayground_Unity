using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Stat;

namespace UPlayGround.Data.Party
{
    public readonly struct PartyCombatPowerResult
    {
        public CharacterActorType CharacterType { get; }
        public int Level { get; }
        public long CombatPower { get; }
        public IReadOnlyDictionary<AttributeId, float> GrowthStats { get; }

        public PartyCombatPowerResult(
            CharacterActorType characterType,
            int level,
            long combatPower,
            IReadOnlyDictionary<AttributeId, float> growthStats)
        {
            CharacterType = characterType;
            Level = level;
            CombatPower = combatPower;
            GrowthStats = growthStats;
        }
    }

    /// <summary>
    /// 파티 성장 Attribute와 전투력을 계산하는 순수 계산기.
    /// 런타임 버프, 장비, 일시 수정자는 포함하지 않는다.
    /// </summary>
    public static class PartyPowerCalculator
    {
        public static Dictionary<AttributeId, float> CalculateGrowthStats(
            PartyMemberGrowthSO growthData,
            int level,
            IReadOnlyDictionary<AttributeId, int> investments = null)
        {
            var attributes = new Dictionary<AttributeId, float>();
            int clampedLevel = growthData != null
                ? Mathf.Clamp(level, 1, Mathf.Max(1, growthData.levelCap))
                : Mathf.Max(1, level);

            foreach (AttributeId attributeId in UPlayGroundAttributeDefaults.All)
            {
                float baseValue = growthData?.baseProfile != null
                                  && growthData.baseProfile.TryGetBaseValue(
                                      attributeId, out float profileValue)
                    ? profileValue
                    : UPlayGroundAttributeDefaults.Get(attributeId);

                attributes[attributeId] =
                    growthData != null && growthData.useAutomaticLevelGrowth
                        ? CalculateAttributeValue(
                            growthData, attributeId, baseValue, clampedLevel)
                        : baseValue;
            }

            if (growthData != null && investments != null)
            {
                foreach (KeyValuePair<AttributeId, int> investment in investments)
                {
                    if (!growthData.TryGetInvestmentRule(
                            investment.Key,
                            out GrowthInvestmentRule rule)
                        || !rule.AttributeId.IsValid)
                        continue;
                    int rank = Mathf.Clamp(
                        investment.Value, 0, Mathf.Max(1, rule.maxRank));
                    attributes[rule.AttributeId] =
                        attributes.TryGetValue(rule.AttributeId, out float value)
                            ? value + rule.flatPerRank * rank
                            : rule.flatPerRank * rank;
                }
            }

            return attributes;
        }

        public static long CalculateCombatPower(
            IReadOnlyDictionary<AttributeId, float> attributes)
        {
            if (attributes == null) return 0L;

            float maxHealth = Mathf.Max(
                0f, Get(attributes, global::UPlayGround.Data.Stat.Attributes.Vital.MaxHealth));
            float attackPower = Mathf.Max(
                0f, Get(attributes, global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower));
            float defense = Mathf.Clamp01(
                Get(attributes, global::UPlayGround.Data.Stat.Attributes.Combat.Defense));
            float critRate = Mathf.Clamp01(
                Get(attributes, global::UPlayGround.Data.Stat.Attributes.Combat.CritRate));
            float critMultiplier = Mathf.Max(
                1f, Get(attributes, global::UPlayGround.Data.Stat.Attributes.Combat.CritMultiplier));
            float attackSpeed = Mathf.Max(
                0.1f, Get(attributes, global::UPlayGround.Data.Stat.Attributes.Combat.AttackSpeed));
            float maxPoise = Mathf.Max(
                0f, Get(attributes, global::UPlayGround.Data.Stat.Attributes.Vital.MaxPoise));
            float generation = Mathf.Max(
                0f, Get(attributes, global::UPlayGround.Data.Stat.Attributes.Resource.GenerationMultiplier));
            float moveSpeed = Mathf.Max(
                0f, Get(attributes, global::UPlayGround.Data.Stat.Attributes.Movement.MoveSpeed));

            float effectiveAttack = attackPower
                                    * (1f + critRate * Mathf.Max(0f, critMultiplier - 1f))
                                    * attackSpeed
                                    * generation;
            float effectiveHealth = maxHealth / Mathf.Max(0.1f, 1f - defense);
            float utility = maxPoise * 0.25f
                            + Mathf.Max(0f, moveSpeed - 1f) * 100f;
            float combatPower = effectiveHealth * 0.35f
                                + effectiveAttack * 100f * 0.55f
                                + utility * 0.10f;

            return Math.Max(
                0L,
                (long)Math.Round(
                    combatPower, MidpointRounding.AwayFromZero));
        }

        public static PartyCombatPowerResult Calculate(
            CharacterActorType characterType,
            PartyMemberGrowthSO growthData,
            int level,
            IReadOnlyDictionary<AttributeId, int> investments = null)
        {
            Dictionary<AttributeId, float> attributes =
                CalculateGrowthStats(growthData, level, investments);
            return new PartyCombatPowerResult(
                characterType,
                Mathf.Max(1, level),
                CalculateCombatPower(attributes),
                attributes);
        }

        private static float CalculateAttributeValue(
            PartyMemberGrowthSO growthData,
            AttributeId attributeId,
            float baseValue,
            int level)
        {
            if (growthData == null
                || !growthData.TryGetRule(attributeId, out StatGrowthRule rule))
                return baseValue;

            int levelDelta = Mathf.Max(0, level - 1);
            return rule.formula switch
            {
                GrowthFormula.Flat =>
                    baseValue + rule.flatPerLevel * levelDelta,
                GrowthFormula.Percent =>
                    baseValue * (1f + rule.percentPerLevel * levelDelta),
                GrowthFormula.Curve =>
                    CalculateCurveValue(
                        baseValue, rule.curve, level, growthData.levelCap),
                _ => baseValue,
            };
        }

        private static float CalculateCurveValue(
            float baseValue,
            AnimationCurve curve,
            int level,
            int levelCap)
        {
            if (curve == null || curve.length == 0) return baseValue;
            float normalized = levelCap <= 1
                ? 1f
                : Mathf.InverseLerp(1f, levelCap, level);
            return baseValue * curve.Evaluate(normalized);
        }

        private static float Get(
            IReadOnlyDictionary<AttributeId, float> attributes,
            AttributeId attributeId) =>
            attributes.TryGetValue(attributeId, out float value)
                ? value
                : UPlayGroundAttributeDefaults.Get(attributeId);
    }
}
