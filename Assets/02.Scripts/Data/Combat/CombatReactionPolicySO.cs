using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Combat
{
    [CreateAssetMenu(fileName = "CombatReactionPolicy", menuName = "UPlayGround/Combat/Reaction Policy")]
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
