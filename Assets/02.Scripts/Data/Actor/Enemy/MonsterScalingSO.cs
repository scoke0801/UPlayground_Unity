using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;

namespace UPlayGround.Data.Enemy
{
    /// <summary>
    /// 몬스터 스탯/공격 피해를 (레벨 × 등급 × 난이도) 공식으로 산출하기 위한 단일 소스 SO.
    /// 플레이어의 <see cref="PartyMemberGrowthSO"/>/<see cref="PartyPowerCalculator"/>를 미러하되,
    /// 플레이어에 없는 두 축(등급 배율, 난이도 배율)을 추가로 얹는다.
    /// 성장 규칙은 플레이어와 동일한 <see cref="StatGrowthRule"/>/<see cref="GrowthFormula"/>를 재사용한다.
    /// </summary>
    [CreateAssetMenu(fileName = "MonsterScaling_", menuName = "UPlayGround/적/Scaling")]
    public class MonsterScalingSO : ScriptableObject
    {
        /// <summary>등급별 배율. Defense는 0~1 가산, 나머지는 곱셈 배율이다.</summary>
        [Serializable]
        public struct GradeScaling
        {
            public MonsterActorGrade grade;
            [Min(0f)] public float healthMultiplier;
            [Min(0f)] public float attackMultiplier;
            [Min(0f)] public float poiseMultiplier;
            [Range(0f, 1f)] public float defenseAdd;
            [Min(0f)] public float moveSpeedMultiplier;
            [Tooltip("공격 1타 목표 최종 피해량 배율 (공격 데이터 생성기 연동용)")]
            [Min(0f)] public float attackDamageMultiplier;
        }

        [Header("L1 기준값 (Normal 등급, 레벨 1)")]
        [Tooltip("성장/등급 배율을 적용하기 전의 기준 Profile. 비워두면 Attribute 기본값을 사용한다.")]
        public AttributeProfileSO baseProfile;

        [Min(1)] public int levelCap = 100;

        [Header("레벨 성장 규칙 (Attribute별, 플레이어와 동일한 규칙 구조)")]
        public List<StatGrowthRule> growthRules = new();

        [Header("등급 배율")]
        public List<GradeScaling> gradeScalings = new();

        [Header("난이도")]
        [Tooltip("HP/공격력에 곱하는 전역 난이도 배율. 인카운터 전체를 한 번에 재조정할 때 사용.")]
        [Min(0f)] public float difficultyMultiplier = 1f;

        [Header("공격 피해 커브 (공격 데이터 생성기 연동)")]
        [Tooltip("Normal 등급 1타 목표 최종 피해량(레벨 보정 전). 공격 데이터 생성기의 base 피해로 사용된다.")]
        [Min(0f)] public float baseAttackDamage = 10f;

        public bool TryGetGrade(MonsterActorGrade grade, out GradeScaling scaling)
        {
            for (int i = 0; i < gradeScalings.Count; i++)
            {
                if (gradeScalings[i].grade != grade) continue;
                scaling = gradeScalings[i];
                return true;
            }

            scaling = default;
            return false;
        }

        public bool TryGetRule(AttributeId attributeId, out StatGrowthRule rule)
        {
            for (int i = 0; i < growthRules.Count; i++)
            {
                if (growthRules[i].AttributeId != attributeId) continue;
                rule = growthRules[i];
                return true;
            }

            rule = default;
            return false;
        }

        /// <summary>
        /// 공격 데이터 생성기가 사용할 등급별 1타 목표 최종 피해량(레벨 보정 전).
        /// 런타임 AttackPower는 별도로 곱해지므로 여기서는 공격 데이터의 등급/난이도 기준 피해만 반영한다.
        /// </summary>
        public float GetBaseAttackDamage(MonsterActorGrade grade)
        {
            float gradeMultiplier = TryGetGrade(grade, out GradeScaling g) && g.attackDamageMultiplier > 0f
                ? g.attackDamageMultiplier
                : 1f;
            return Mathf.Max(0f, baseAttackDamage) * gradeMultiplier * Mathf.Max(0f, difficultyMultiplier);
        }

        /// <summary>
        /// 명조형 액션 전투 페이싱을 기준으로 약몹은 짧게, 엘리트는 공진/경직 플레이를 요구하고,
        /// 보스는 장기전이지만 지나친 HP 벽이 되지 않도록 잡은 기본값.
        /// 생성기에서 명시적으로 적용하거나 새 Scaling 에셋을 만들 때 사용한다.
        /// </summary>
        public void ApplyActionCombatDefaults()
        {
            baseAttackDamage = 10f;

            gradeScalings = new List<GradeScaling>
            {
                new() { grade = MonsterActorGrade.Weak,   healthMultiplier = 0.45f, attackMultiplier = 0.65f, poiseMultiplier = 0.35f, defenseAdd = 0f,    moveSpeedMultiplier = 1.02f, attackDamageMultiplier = 0.65f },
                new() { grade = MonsterActorGrade.Normal, healthMultiplier = 1f,    attackMultiplier = 1f,    poiseMultiplier = 1f,    defenseAdd = 0f,    moveSpeedMultiplier = 1f,    attackDamageMultiplier = 1f },
                new() { grade = MonsterActorGrade.Elite,  healthMultiplier = 2.6f,  attackMultiplier = 1.2f,  poiseMultiplier = 2.1f,  defenseAdd = 0.04f, moveSpeedMultiplier = 1.05f, attackDamageMultiplier = 1.25f },
                new() { grade = MonsterActorGrade.Boss,   healthMultiplier = 9.5f,  attackMultiplier = 1.45f, poiseMultiplier = 4.2f,  defenseAdd = 0.08f, moveSpeedMultiplier = 1f,    attackDamageMultiplier = 1.6f },
            };

            growthRules = new List<StatGrowthRule>
            {
                new() { attributeId = AttributeIds.Vital.MaxHealth.Value, formula = GrowthFormula.Percent, percentPerLevel = 0.035f },
                new() { attributeId = AttributeIds.Combat.AttackPower.Value, formula = GrowthFormula.Percent, percentPerLevel = 0.03f },
                new() { attributeId = AttributeIds.Vital.MaxPoise.Value, formula = GrowthFormula.Percent, percentPerLevel = 0.018f },
            };
        }

        /// <summary>비어 있는 새 에셋에 합리적인 기본 등급 배율/성장 규칙을 채운다.</summary>
        public void FillDefaults()
        {
            if (gradeScalings == null || gradeScalings.Count == 0)
                ApplyActionCombatDefaults();

            if (growthRules == null || growthRules.Count == 0)
            {
                growthRules = new List<StatGrowthRule>
                {
                    new() { attributeId = AttributeIds.Vital.MaxHealth.Value, formula = GrowthFormula.Percent, percentPerLevel = 0.035f },
                    new() { attributeId = AttributeIds.Combat.AttackPower.Value, formula = GrowthFormula.Percent, percentPerLevel = 0.03f },
                    new() { attributeId = AttributeIds.Vital.MaxPoise.Value, formula = GrowthFormula.Percent, percentPerLevel = 0.018f },
                };
            }
        }
    }
}
