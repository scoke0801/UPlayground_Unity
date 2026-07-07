using System;
using System.Collections.Generic;
using UPlayGround.Data.Actor;
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
                    : GetDefaultMonsterBase(type);

                float leveled = ApplyGrowth(scaling, type, baseValue, clampedLevel, cap);
                stats[type] = ApplyGradeAndDifficulty(type, leveled, grade01, difficulty);
            }

            return stats;
        }

        public static Dictionary<StatType, float> Calculate(
            MonsterScalingSO scaling,
            ActorDefinitionSO actor,
            float difficultyOverride = 0f)
        {
            return CalculateAtLevel(scaling, actor, actor != null ? actor.level : 1, difficultyOverride);
        }

        /// <summary>
        /// 정의 기준이되 레벨만 오버라이드해 계산한다. 재스폰 런타임 레벨 스케일링용.
        /// 휴머노이드 무기 편차 등 정의 기반 보정을 동일하게 적용한다.
        /// Calculate 오버로드로 두지 않는 이유: int 리터럴 난이도 인자가 조용히 레벨로 바인딩되는 것을 막기 위함.
        /// </summary>
        public static Dictionary<StatType, float> CalculateAtLevel(
            MonsterScalingSO scaling,
            ActorDefinitionSO actor,
            int levelOverride,
            float difficultyOverride = 0f)
        {
            Dictionary<StatType, float> stats = Calculate(
                scaling,
                actor != null ? actor.grade : MonsterActorGrade.Normal,
                levelOverride,
                difficultyOverride);

            ApplyHumanoidWeaponVariation(stats, actor);
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

        private static float GetDefaultMonsterBase(StatType type)
        {
            return type switch
            {
                StatType.MaxHealth => 160f,
                StatType.AttackPower => 1f,
                StatType.Defense => 0f,
                StatType.MaxPoise => 90f,
                StatType.PoiseRecoveryRate => 30f,
                StatType.PoiseRecoveryDelay => 2f,
                StatType.MoveSpeed => 1f,
                _ => ActorStatSO.GetDefault(type),
            };
        }

        private static void ApplyHumanoidWeaponVariation(Dictionary<StatType, float> stats, ActorDefinitionSO actor)
        {
            if (stats == null || !IsHumanoidMonster(actor))
                return;

            WeaponType weapon = InferWeaponType(actor);
            switch (weapon)
            {
                case WeaponType.SwordShield:
                    Multiply(stats, StatType.MaxHealth, 1.25f);
                    AddClamped01(stats, StatType.Defense, 0.08f);
                    Multiply(stats, StatType.MaxPoise, 1.35f);
                    Multiply(stats, StatType.AttackPower, 0.90f);
                    Multiply(stats, StatType.MoveSpeed, 0.92f);
                    break;

                case WeaponType.Bow:
                case WeaponType.Staff:
                    Multiply(stats, StatType.MaxHealth, 0.82f);
                    AddClamped01(stats, StatType.Defense, -0.03f);
                    Multiply(stats, StatType.MaxPoise, 0.75f);
                    Multiply(stats, StatType.AttackPower, 1.08f);
                    Multiply(stats, StatType.MoveSpeed, 1.06f);
                    break;
            }
        }

        public static string GetHumanoidWeaponProfileName(ActorDefinitionSO actor)
        {
            if (!IsHumanoidMonster(actor))
                return "-";

            return InferWeaponType(actor) switch
            {
                WeaponType.SwordShield => "탱커",
                WeaponType.Bow => "원거리",
                WeaponType.Staff => "원거리",
                _ => "기본",
            };
        }

        private static bool IsHumanoidMonster(ActorDefinitionSO actor)
        {
            if (actor == null || (actor.actorType & ActorType.Monster) == 0)
                return false;

            string key = BuildSearchKey(actor);
            return key.Contains("Humanoid", StringComparison.OrdinalIgnoreCase)
                   || key.Contains("Monster", StringComparison.OrdinalIgnoreCase)
                   || key.StartsWith("Enemy_", StringComparison.OrdinalIgnoreCase);
        }

        private static WeaponType InferWeaponType(ActorDefinitionSO actor)
        {
            string key = BuildSearchKey(actor);
            if (key.Contains("SwordShield", StringComparison.OrdinalIgnoreCase))
                return WeaponType.SwordShield;
            if (key.Contains("Bow", StringComparison.OrdinalIgnoreCase))
                return WeaponType.Bow;
            if (key.Contains("Staff", StringComparison.OrdinalIgnoreCase))
                return WeaponType.Staff;
            return WeaponType.NoWeapon;
        }

        private static string BuildSearchKey(ActorDefinitionSO actor)
        {
            if (actor == null)
                return string.Empty;

            string attackName = actor.attackData != null ? actor.attackData.name : string.Empty;
            return $"{actor.actorId} {actor.displayName} {actor.name} {attackName}";
        }

        private static void Multiply(Dictionary<StatType, float> stats, StatType type, float multiplier)
        {
            if (stats.TryGetValue(type, out float value))
                stats[type] = value * multiplier;
        }

        private static void AddClamped01(Dictionary<StatType, float> stats, StatType type, float add)
        {
            if (stats.TryGetValue(type, out float value))
                stats[type] = Mathf.Clamp01(value + add);
        }
    }
}
