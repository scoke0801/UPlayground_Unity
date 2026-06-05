using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using UPlayGround.Data.Stat;

namespace UPlayGround.Data.Enemy
{
    /// <summary>
    /// 몬스터 스탯/공격 피해를 (레벨 × 등급 × 난이도) 공식으로 산출하기 위한 단일 소스 SO.
    /// 플레이어의 <see cref="PartyMemberGrowthSO"/>/<see cref="PartyPowerCalculator"/>를 미러하되,
    /// 플레이어에 없는 두 축(등급 배율, 난이도 배율)을 추가로 얹는다.
    /// 성장 규칙은 플레이어와 동일한 <see cref="StatGrowthRule"/>/<see cref="GrowthFormula"/>를 재사용한다.
    ///
    /// 몬스터는 런타임 레벨 스케일링을 하지 않으므로(MonsterActor가 statData를 직접 사용),
    /// 이 SO는 에디터 배치 생성기가 각 ActorDefinitionSO.statData를 bake할 때 사용한다.
    /// </summary>
    [CreateAssetMenu(fileName = "MonsterScaling_", menuName = "UPlayGround/Enemy/Monster Scaling")]
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
        [Tooltip("성장/등급 배율을 적용하기 전의 기준 스탯. 비워두면 StatType 기본값을 사용한다.")]
        public ActorStatSO baseStat;

        [Min(1)] public int levelCap = 100;

        [Header("레벨 성장 규칙 (StatType별, 플레이어와 동일한 규칙 구조)")]
        public List<StatGrowthRule> growthRules = new();

        [Header("등급 배율")]
        public List<GradeScaling> gradeScalings = new();

        [Header("난이도")]
        [Tooltip("HP/공격력에 곱하는 전역 난이도 배율. 인카운터 전체를 한 번에 재조정할 때 사용.")]
        [Min(0f)] public float difficultyMultiplier = 1f;

        [Header("공격 피해 커브 (공격 데이터 생성기 연동)")]
        [Tooltip("Normal 등급 1타 목표 최종 피해량(레벨 보정 전). 공격 데이터 생성기의 base 피해로 사용된다.")]
        [Min(0f)] public float baseAttackDamage = 12f;

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

        public bool TryGetRule(StatType type, out StatGrowthRule rule)
        {
            for (int i = 0; i < growthRules.Count; i++)
            {
                if (growthRules[i].statType != type) continue;
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

        /// <summary>비어 있는 새 에셋에 합리적인 기본 등급 배율/성장 규칙을 채운다.</summary>
        public void FillDefaults()
        {
            if (gradeScalings == null || gradeScalings.Count == 0)
            {
                gradeScalings = new List<GradeScaling>
                {
                    new() { grade = MonsterActorGrade.Weak,   healthMultiplier = 0.40f, attackMultiplier = 0.82f, poiseMultiplier = 0.55f, defenseAdd = 0.01f, moveSpeedMultiplier = 1f,   attackDamageMultiplier = 0.82f },
                    new() { grade = MonsterActorGrade.Normal, healthMultiplier = 1f,   attackMultiplier = 1f,   poiseMultiplier = 1f,   defenseAdd = 0f,    moveSpeedMultiplier = 1f,   attackDamageMultiplier = 1f },
                    new() { grade = MonsterActorGrade.Elite,  healthMultiplier = 2.05f, attackMultiplier = 1.3f, poiseMultiplier = 2.2f, defenseAdd = 0.10f, moveSpeedMultiplier = 1.1f, attackDamageMultiplier = 1.5f },
                    new() { grade = MonsterActorGrade.Boss,   healthMultiplier = 8.33f, attackMultiplier = 1.5f, poiseMultiplier = 7f,    defenseAdd = 0.20f, moveSpeedMultiplier = 1f,   attackDamageMultiplier = 2.25f },
                };
            }

            if (growthRules == null || growthRules.Count == 0)
            {
                growthRules = new List<StatGrowthRule>
                {
                    new() { statType = StatType.MaxHealth,   formula = GrowthFormula.Percent, percentPerLevel = 0.08f },
                    new() { statType = StatType.AttackPower, formula = GrowthFormula.Percent, percentPerLevel = 0.04f },
                    new() { statType = StatType.MaxPoise,    formula = GrowthFormula.Percent, percentPerLevel = 0.03f },
                };
            }
        }
    }
}
