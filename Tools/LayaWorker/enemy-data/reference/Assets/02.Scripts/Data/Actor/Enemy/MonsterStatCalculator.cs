using System;
using System.Collections.Generic;
using UPlayGround.Data.Actor;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Stat;

namespace UPlayGround.Data.Enemy
{
    /// <summary>
    /// <see cref="MonsterScalingSO"/>로부터 (등급 × 레벨 × 난이도) 몬스터 스탯을 산출하는 순수 계산기.
    /// 런타임 버프와 장비는 포함하지 않는다.
    /// </summary>
    public static class MonsterStatCalculator
    {
        /// <summary>
        /// 등급/레벨/난이도를 반영한 전체 스탯 딕셔너리를 계산한다.
        /// difficultyOverride가 0보다 크면 SO의 difficultyMultiplier 대신 사용한다(미리보기용).
        /// </summary>
        public static Dictionary<AttributeId, float> Calculate(
            MonsterScalingSO scaling,
            MonsterActorGrade grade,
            int level,
            float difficultyOverride = 0f)
        {
            var stats = new Dictionary<AttributeId, float>();
            int cap = scaling != null ? Mathf.Max(1, scaling.levelCap) : 100;
            int clampedLevel = Mathf.Clamp(Mathf.Max(1, level), 1, cap);

            float difficulty = difficultyOverride > 0f
                ? difficultyOverride
                : (scaling != null ? Mathf.Max(0f, scaling.difficultyMultiplier) : 1f);

            MonsterScalingSO.GradeScaling? grade01 = null;
            if (scaling != null && scaling.TryGetGrade(grade, out MonsterScalingSO.GradeScaling gradeScaling))
                grade01 = gradeScaling;

            foreach (AttributeId attributeId in UPlayGroundAttributeDefaults.All)
            {
                float baseValue = scaling != null
                                  && scaling.baseProfile != null
                                  && scaling.baseProfile.TryGetBaseValue(
                                      attributeId, out float profileValue)
                    ? profileValue
                    : GetDefaultMonsterBase(attributeId);

                float leveled = ApplyGrowth(
                    scaling, attributeId, baseValue, clampedLevel, cap);
                stats[attributeId] = ApplyGradeAndDifficulty(
                    attributeId, leveled, grade01, difficulty);
            }

            return stats;
        }

        public static Dictionary<AttributeId, float> Calculate(
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
        public static Dictionary<AttributeId, float> CalculateAtLevel(
            MonsterScalingSO scaling,
            ActorDefinitionSO actor,
            int levelOverride,
            float difficultyOverride = 0f)
        {
            Dictionary<AttributeId, float> stats = Calculate(
                scaling,
                actor != null ? actor.grade : MonsterActorGrade.Normal,
                levelOverride,
                difficultyOverride);

            ApplyHumanoidWeaponVariation(stats, actor);
            return stats;
        }

        private static float ApplyGrowth(
            MonsterScalingSO scaling,
            AttributeId attributeId,
            float baseValue,
            int level,
            int cap)
        {
            if (scaling == null
                || !scaling.TryGetRule(attributeId, out MonsterStatGrowthRule rule))
                return baseValue;

            int levelDelta = Mathf.Max(0, level - 1);
            return rule.formula switch
            {
                MonsterGrowthFormula.Flat => baseValue + rule.flatPerLevel * levelDelta,
                MonsterGrowthFormula.Percent => baseValue * (1f + rule.percentPerLevel * levelDelta),
                MonsterGrowthFormula.Curve => CalculateCurveValue(baseValue, rule.curve, level, cap),
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
            AttributeId attributeId,
            float value,
            MonsterScalingSO.GradeScaling? grade,
            float difficulty)
        {
            if (attributeId == global::UPlayGround.Data.Stat.Attributes.Vital.MaxHealth)
            {
                if (grade.HasValue) value *= NonZero(grade.Value.healthMultiplier);
                return value * difficulty;
            }
            if (attributeId == global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower)
            {
                if (grade.HasValue) value *= NonZero(grade.Value.attackMultiplier);
                return value * difficulty;
            }
            if (attributeId == global::UPlayGround.Data.Stat.Attributes.Vital.MaxPoise)
            {
                if (grade.HasValue) value *= NonZero(grade.Value.poiseMultiplier);
                return value;
            }
            if (attributeId == global::UPlayGround.Data.Stat.Attributes.Movement.MoveSpeed)
            {
                if (grade.HasValue) value *= NonZero(grade.Value.moveSpeedMultiplier);
                return value;
            }
            if (attributeId == global::UPlayGround.Data.Stat.Attributes.Combat.Defense)
            {
                if (grade.HasValue)
                    value = Mathf.Clamp01(value + grade.Value.defenseAdd);
                return value;
            }
            return value;
        }

        /// <summary>배율이 0(미설정 기본값)이면 1로 본다 — 0 배율로 스탯이 사라지는 사고 방지.</summary>
        private static float NonZero(float multiplier) => multiplier > 0f ? multiplier : 1f;

        private static float GetDefaultMonsterBase(AttributeId attributeId)
        {
            if (attributeId == global::UPlayGround.Data.Stat.Attributes.Vital.MaxHealth) return 160f;
            if (attributeId == global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower) return 1f;
            if (attributeId == global::UPlayGround.Data.Stat.Attributes.Combat.Defense) return 0f;
            if (attributeId == global::UPlayGround.Data.Stat.Attributes.Vital.MaxPoise) return 90f;
            if (attributeId == global::UPlayGround.Data.Stat.Attributes.Vital.PoiseRecoveryRate) return 30f;
            if (attributeId == global::UPlayGround.Data.Stat.Attributes.Vital.PoiseRecoveryDelay) return 2f;
            if (attributeId == global::UPlayGround.Data.Stat.Attributes.Movement.MoveSpeed) return 1f;
            return UPlayGroundAttributeDefaults.Get(attributeId);
        }

        private static void ApplyHumanoidWeaponVariation(
            Dictionary<AttributeId, float> stats,
            ActorDefinitionSO actor)
        {
            if (stats == null || !IsHumanoidMonster(actor))
                return;

            WeaponType weapon = InferWeaponType(actor);
            switch (weapon)
            {
                case WeaponType.SwordShield:
                    Multiply(stats, global::UPlayGround.Data.Stat.Attributes.Vital.MaxHealth, 1.25f);
                    AddClamped01(stats, global::UPlayGround.Data.Stat.Attributes.Combat.Defense, 0.08f);
                    Multiply(stats, global::UPlayGround.Data.Stat.Attributes.Vital.MaxPoise, 1.35f);
                    Multiply(stats, global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower, 0.90f);
                    Multiply(stats, global::UPlayGround.Data.Stat.Attributes.Movement.MoveSpeed, 0.92f);
                    break;

                case WeaponType.Bow:
                case WeaponType.Staff:
                    Multiply(stats, global::UPlayGround.Data.Stat.Attributes.Vital.MaxHealth, 0.82f);
                    AddClamped01(stats, global::UPlayGround.Data.Stat.Attributes.Combat.Defense, -0.03f);
                    Multiply(stats, global::UPlayGround.Data.Stat.Attributes.Vital.MaxPoise, 0.75f);
                    Multiply(stats, global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower, 1.08f);
                    Multiply(stats, global::UPlayGround.Data.Stat.Attributes.Movement.MoveSpeed, 1.06f);
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

            string abilitySetName =
                actor.EffectiveAbilitySet != null
                    ? actor.EffectiveAbilitySet.name
                    : string.Empty;
            return $"{actor.actorId} {actor.displayName} {actor.name} {abilitySetName}";
        }

        private static void Multiply(
            Dictionary<AttributeId, float> stats,
            AttributeId attributeId,
            float multiplier)
        {
            if (stats.TryGetValue(attributeId, out float value))
                stats[attributeId] = value * multiplier;
        }

        private static void AddClamped01(
            Dictionary<AttributeId, float> stats,
            AttributeId attributeId,
            float add)
        {
            if (stats.TryGetValue(attributeId, out float value))
                stats[attributeId] = Mathf.Clamp01(value + add);
        }
    }
}
