using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Combat
{
    [CreateAssetMenu(fileName = "CombatReactionPolicy", menuName = "UPlayGround/전투/Reaction Policy")]
    public class CombatReactionPolicySO : ScriptableObject
    {
        /// <summary>
        /// 공격 종류별 Poise 피해 배율 표. 0은 "배율 없음(1)" 센티넬이라
        /// 기존 에셋과 신규 필드는 동작이 바뀌지 않는다.
        /// </summary>
        [Serializable]
        public class PoiseEfficiencyTable
        {
            [Range(0f, 3f)] public float normalAttack;
            [Range(0f, 3f)] public float heavyAttack;
            [Range(0f, 3f)] public float jumpAttack;
            [Range(0f, 3f)] public float dashAttack;
            [Range(0f, 3f)] public float finishAttack;
            [Range(0f, 3f)] public float skillAttack;
            [Range(0f, 3f)] public float chargeAttack;

            /// <summary>센티넬 해석 전의 저작값을 반환한다.</summary>
            public float GetRaw(AttackKind kind) => kind switch
            {
                AttackKind.NormalAttack => normalAttack,
                AttackKind.HeavyAttack => heavyAttack,
                AttackKind.JumpAttack => jumpAttack,
                AttackKind.DashAttack => dashAttack,
                AttackKind.FinishAttack => finishAttack,
                AttackKind.SkillAttack => skillAttack,
                AttackKind.ChargeAttack => chargeAttack,
                _ => 0f,
            };
        }

        /// <summary>
        /// 공격 종류별 밀림 거리(m) 표. 0이면 밀지 않는다.
        /// Poise가 깨지지 않은 일반 피격에서도 "맞았다"는 정보를 주는 데이터 축이다.
        /// </summary>
        [Serializable]
        public class HitPushTable
        {
            [Min(0f)] public float normalAttack;
            [Min(0f)] public float heavyAttack;
            [Min(0f)] public float jumpAttack;
            [Min(0f)] public float dashAttack;
            [Min(0f)] public float finishAttack;
            [Min(0f)] public float skillAttack;
            [Min(0f)] public float chargeAttack;

            public float Get(AttackKind kind) => kind switch
            {
                AttackKind.NormalAttack => normalAttack,
                AttackKind.HeavyAttack => heavyAttack,
                AttackKind.JumpAttack => jumpAttack,
                AttackKind.DashAttack => dashAttack,
                AttackKind.FinishAttack => finishAttack,
                AttackKind.SkillAttack => skillAttack,
                AttackKind.ChargeAttack => chargeAttack,
                _ => 0f,
            };
        }

        [Serializable]
        public class GradeRule
        {
            public MonsterActorGrade grade = MonsterActorGrade.Normal;
            public bool requirePoiseBreakForState;
            public bool allowForceReaction = true;
            public bool allowHit = true;
            public bool allowStun = true;
            public bool allowKnockdown = true;
            public bool allowAirborne = true;
            public bool allowGrab = true;

            [Header("취약 배율 (행동불능 상태에서 받는 피해 배율)")]
            [Tooltip("0이면 기본값 사용. 1보다 작은 값은 1(보너스 없음)로 처리. Break 노출 배율은 MonsterBreakGaugeSO에서 따로 관리하며 데미지 적용 시 더 큰 쪽 하나만 반영된다.")]
            [Min(0f)] public float hitVulnerabilityMultiplier = 0f;
            [Min(0f)] public float stunVulnerabilityMultiplier = 0f;
            [Min(0f)] public float knockdownVulnerabilityMultiplier = 0f;
            [Min(0f)] public float airborneVulnerabilityMultiplier = 0f;
            [Min(0f)] public float grabbedVulnerabilityMultiplier = 0f;

            [Header("Poise 피해 효율 (공격 종류별 배율)")]
            [Tooltip("이 등급이 받는 Poise 피해에 공격 종류별로 곱하는 배율. 0이면 1(변화 없음).\n"
                     + "약공격을 낮추고 강공을 높이면 종류를 섞어 쓸 이유가 생긴다. 0.5 미만은 약공격 단독 클리어를 막으므로 피한다.")]
            public PoiseEfficiencyTable poiseEfficiency = new();

            [Header("일반 피격 반응 (Poise가 깨지지 않은 타격)")]
            [Tooltip("공격 종류별 밀림 거리(m). 0이면 밀리지 않는다. 약공은 연타 흐름이 끊기지 않게 작게, 강공·스킬은 크게 둔다.")]
            public HitPushTable hitPush = new();
            [Tooltip("켜면 피격 시 상체 플린치를 얹는다. 이동·공격 판단은 끊지 않는다.")]
            public bool useHitFlinch;
        }

        [Tooltip("등급별 피격 리액션 정책. 같은 등급이 여러 번 있으면 먼저 발견된 항목을 사용한다.")]
        public List<GradeRule> monsterGradeRules = new();

        public GradeRule GetRule(MonsterActorGrade grade)
        {
            if (monsterGradeRules == null)
                return null;

            for (int i = 0; i < monsterGradeRules.Count; i++)
            {
                GradeRule rule = monsterGradeRules[i];
                if (rule != null && rule.grade == grade)
                    return rule;
            }

            return null;
        }
    }
}
