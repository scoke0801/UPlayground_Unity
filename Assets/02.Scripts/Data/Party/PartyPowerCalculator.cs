using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Stat;

namespace UPlayGround.Data.Party
{
    public readonly struct PartyCombatPowerResult
    {
        public CharacterActorType CharacterType { get; }
        public int Level { get; }
        public long CombatPower { get; }
        public IReadOnlyDictionary<StatType, float> GrowthStats { get; }

        public PartyCombatPowerResult(
            CharacterActorType characterType,
            int level,
            long combatPower,
            IReadOnlyDictionary<StatType, float> growthStats)
        {
            CharacterType = characterType;
            Level = level;
            CombatPower = combatPower;
            GrowthStats = growthStats;
        }
    }

    /// <summary>
    /// 파티 성장 스탯과 전투력을 계산하는 순수 계산기.
    /// 런타임 버프, 장비, 일시 수정자는 포함하지 않는다.
    /// </summary>
    public static class PartyPowerCalculator
    {
        public static Dictionary<StatType, float> CalculateGrowthStats(
            PartyMemberGrowthSO growthData,
            int level,
            IReadOnlyDictionary<GrowthAttributeType, int> investments = null)
        {
            var stats = new Dictionary<StatType, float>();
            int clampedLevel = growthData != null
                ? Mathf.Clamp(level, 1, Mathf.Max(1, growthData.levelCap))
                : Mathf.Max(1, level);

            foreach (StatType type in Enum.GetValues(typeof(StatType)))
            {
                float baseValue = growthData?.baseStat != null
                    ? growthData.baseStat.GetBase(type)
                    : ActorStatSO.GetDefault(type);

                stats[type] = growthData != null && growthData.useAutomaticLevelGrowth
                    ? CalculateStatValue(growthData, type, baseValue, clampedLevel)
                    : baseValue;
            }

            if (growthData != null && investments != null)
            {
                foreach (var investment in investments)
                {
                    growthData.TryGetInvestmentRule(investment.Key, out GrowthInvestmentRule rule);
                    int rank = Mathf.Clamp(investment.Value, 0, Mathf.Max(1, rule.maxRank));
                    stats[rule.statType] = stats.TryGetValue(rule.statType, out float value)
                        ? value + rule.flatPerRank * rank
                        : rule.flatPerRank * rank;
                }
            }

            return stats;
        }

        public static long CalculateCombatPower(IReadOnlyDictionary<StatType, float> stats)
        {
            if (stats == null) return 0L;

            float maxHealth = Mathf.Max(0f, Get(stats, StatType.MaxHealth));
            float attackPower = Mathf.Max(0f, Get(stats, StatType.AttackPower));
            float defense = Mathf.Clamp01(Get(stats, StatType.Defense));
            float critRate = Mathf.Clamp01(Get(stats, StatType.CritRate));
            float critMultiplier = Mathf.Max(1f, Get(stats, StatType.CritMultiplier));
            float attackSpeed = Mathf.Max(0.1f, Get(stats, StatType.AttackSpeed));
            float maxPoise = Mathf.Max(0f, Get(stats, StatType.MaxPoise));
            float skillGaugeRate = Mathf.Max(0f, Get(stats, StatType.SkillGaugeRate));
            float moveSpeed = Mathf.Max(0f, Get(stats, StatType.MoveSpeed));

            float effectiveAttack = attackPower
                                    * (1f + critRate * Mathf.Max(0f, critMultiplier - 1f))
                                    * attackSpeed
                                    * skillGaugeRate;
            float effectiveHealth = maxHealth / Mathf.Max(0.1f, 1f - defense);
            float utility = maxPoise * 0.25f + Mathf.Max(0f, moveSpeed - 1f) * 100f;

            float combatPower = effectiveHealth * 0.35f
                               + effectiveAttack * 100f * 0.55f
                               + utility * 0.10f;

            return Math.Max(0L, (long)Math.Round(combatPower, MidpointRounding.AwayFromZero));
        }

        public static PartyCombatPowerResult Calculate(
            CharacterActorType characterType,
            PartyMemberGrowthSO growthData,
            int level,
            IReadOnlyDictionary<GrowthAttributeType, int> investments = null)
        {
            var stats = CalculateGrowthStats(growthData, level, investments);
            long combatPower = CalculateCombatPower(stats);
            return new PartyCombatPowerResult(characterType, Mathf.Max(1, level), combatPower, stats);
        }

        private static float CalculateStatValue(
            PartyMemberGrowthSO growthData,
            StatType type,
            float baseValue,
            int level)
        {
            if (growthData == null || !growthData.TryGetRule(type, out var rule))
                return baseValue;

            int levelDelta = Mathf.Max(0, level - 1);
            return rule.formula switch
            {
                GrowthFormula.Flat => baseValue + rule.flatPerLevel * levelDelta,
                GrowthFormula.Percent => baseValue * (1f + rule.percentPerLevel * levelDelta),
                GrowthFormula.Curve => CalculateCurveValue(baseValue, rule.curve, level, growthData.levelCap),
                _ => baseValue
            };
        }

        private static float CalculateCurveValue(float baseValue, AnimationCurve curve, int level, int levelCap)
        {
            if (curve == null || curve.length == 0) return baseValue;

            float normalized = levelCap <= 1
                ? 1f
                : Mathf.InverseLerp(1f, levelCap, level);
            return baseValue * curve.Evaluate(normalized);
        }

        private static float Get(IReadOnlyDictionary<StatType, float> stats, StatType type)
            => stats.TryGetValue(type, out float value) ? value : ActorStatSO.GetDefault(type);
    }
}
