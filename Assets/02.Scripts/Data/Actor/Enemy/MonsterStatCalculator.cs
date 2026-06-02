using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using UPlayGround.Data.Stat;

namespace UPlayGround.Data.Enemy
{
    /// <summary>
    /// <see cref="MonsterScalingSO"/>로부터 (등급 × 레벨 × 난이도) 몬스터 스탯을 산출하는 순수 계산기.
    /// 런타임 버프/장비는 포함하지 않는다. <see cref="PartyPowerCalculator"/>의 몬스터판.
    /// </summary>
    public static class MonsterStatCalculator
    {
        /// <summary>
        /// 등급/레벨/난이도를 반영한 전체 스탯 딕셔너리를 계산한다.
        /// difficultyOverride가 0보다 크면 SO의 difficultyMultiplier 대신 사용한다(미리보기용).
        /// </summary>
        public static Dictionary<StatType, float> Calculate(
            MonsterScalingSO scaling,
            MonsterActorGrade grade,
            int level,
            float difficultyOverride = 0f)
        {
            var stats = new Dictionary<StatType, float>();
            int cap = scaling != null ? Mathf.Max(1, scaling.levelCap) : 100;
            int clampedLevel = Mathf.Clamp(Mathf.Max(1, level), 1, cap);

            float difficulty = difficultyOverride > 0f
                ? difficultyOverride
                : (scaling != null ? Mathf.Max(0f, scaling.difficultyMultiplier) : 1f);

            MonsterScalingSO.GradeScaling? grade01 = null;
            if (scaling != null && scaling.TryGetGrade(grade, out MonsterScalingSO.GradeScaling gradeScaling))
                grade01 = gradeScaling;

            foreach (StatType type in Enum.GetValues(typeof(StatType)))
            {
                float baseValue = scaling != null && scaling.baseStat != null
                    ? scaling.baseStat.GetBase(type)
                    : ActorStatSO.GetDefault(type);

                float leveled = ApplyGrowth(scaling, type, baseValue, clampedLevel, cap);
                stats[type] = ApplyGradeAndDifficulty(type, leveled, grade01, difficulty);
            }

            return stats;
        }

        private static float ApplyGrowth(MonsterScalingSO scaling, StatType type, float baseValue, int level, int cap)
        {
            if (scaling == null || !scaling.TryGetRule(type, out StatGrowthRule rule))
                return baseValue;

            int levelDelta = Mathf.Max(0, level - 1);
            return rule.formula switch
            {
                GrowthFormula.Flat => baseValue + rule.flatPerLevel * levelDelta,
                GrowthFormula.Percent => baseValue * (1f + rule.percentPerLevel * levelDelta),
                GrowthFormula.Curve => CalculateCurveValue(baseValue, rule.curve, level, cap),
                _ => baseValue,
            };
        }

        private static float CalculateCurveValue(float baseValue, AnimationCurve curve, int level, int cap)
        {
            if (curve == null || curve.length == 0) return baseValue;
            float normalized = cap <= 1 ? 1f : Mathf.InverseLerp(1f, cap, level);
            return baseValue * curve.Evaluate(normalized);
        }

        private static float ApplyGradeAndDifficulty(
            StatType type,
            float value,
            MonsterScalingSO.GradeScaling? grade,
            float difficulty)
        {
            switch (type)
            {
                case StatType.MaxHealth:
                    if (grade.HasValue) value *= NonZero(grade.Value.healthMultiplier);
                    return value * difficulty;

                case StatType.AttackPower:
                    if (grade.HasValue) value *= NonZero(grade.Value.attackMultiplier);
                    return value * difficulty;

                case StatType.MaxPoise:
                    if (grade.HasValue) value *= NonZero(grade.Value.poiseMultiplier);
                    return value;

                case StatType.MoveSpeed:
                    if (grade.HasValue) value *= NonZero(grade.Value.moveSpeedMultiplier);
                    return value;

                case StatType.Defense:
                    if (grade.HasValue) value = Mathf.Clamp01(value + grade.Value.defenseAdd);
                    return value;

                default:
                    return value;
            }
        }

        /// <summary>배율이 0(미설정 기본값)이면 1로 본다 — 0 배율로 스탯이 사라지는 사고 방지.</summary>
        private static float NonZero(float multiplier) => multiplier > 0f ? multiplier : 1f;
    }
}
