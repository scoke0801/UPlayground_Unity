using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Combat
{
    [CreateAssetMenu(fileName = "CombatReactionPolicy", menuName = "UPlayGround/전투/Reaction Policy")]
    public class CombatReactionPolicySO : ScriptableObject
    {
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
