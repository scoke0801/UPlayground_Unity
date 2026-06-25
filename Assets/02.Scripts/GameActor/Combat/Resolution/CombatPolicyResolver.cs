using UnityEngine;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Combat
{
    public static class CombatPolicyResolver
    {
        public static bool CanGuard(CombatDefensePolicySO policy, AttackDefenseType defenseType)
            => policy == null || policy.CanGuard(defenseType);

        public static bool CanParry(CombatDefensePolicySO policy, AttackDefenseType defenseType)
            => policy == null || policy.CanParry(defenseType);

        public static bool CanPerfectDodge(CombatDefensePolicySO policy, AttackDefenseType defenseType)
            => policy == null || policy.CanPerfectDodge(defenseType);

        public static bool AllowsMonsterForceReaction(
            CombatReactionPolicySO policy,
            MonsterActorGrade grade)
        {
            CombatReactionPolicySO.GradeRule rule = policy != null ? policy.GetRule(grade) : null;
            return rule == null || rule.allowForceReaction;
        }

        public static bool RequiresPoiseBreakForMonsterState(
            CombatReactionPolicySO policy,
            MonsterActorGrade grade)
        {
            CombatReactionPolicySO.GradeRule rule = policy != null ? policy.GetRule(grade) : null;
            return rule != null && rule.requirePoiseBreakForState;
        }

        public static bool AllowsMonsterReactionState(
            CombatReactionPolicySO policy,
            MonsterActorGrade grade,
            CombatReactionState state)
        {
            CombatReactionPolicySO.GradeRule rule = policy != null ? policy.GetRule(grade) : null;
            if (rule == null)
                return true;

            return state switch
            {
                CombatReactionState.Hit => rule.allowHit,
                CombatReactionState.Stun => rule.allowStun,
                CombatReactionState.Knockdown => rule.allowKnockdown,
                CombatReactionState.Airborne => rule.allowAirborne,
                CombatReactionState.Grabbed => rule.allowGrab,
                _ => true,
            };
        }

        /// <summary>
        /// 적이 행동불능 상태에서 받는 통합 취약 배율을 산출한다.
        /// 리액션 상태 배율과 Break 노출 배율은 단일 채널로 통합되어, 동시 성립 시 더 큰 하나만 적용한다(max-wins).
        /// </summary>
        /// <param name="breakExposedMultiplier">Break 노출 중일 때의 배율(MonsterBreakGauge.DamageTakenMultiplier). 비노출이면 1.</param>
        public static float GetVulnerabilityMultiplier(
            CombatReactionPolicySO policy,
            MonsterActorGrade grade,
            CombatReactionState reactionState,
            float breakExposedMultiplier)
        {
            CombatReactionPolicySO.GradeRule rule = policy != null ? policy.GetRule(grade) : null;
            float reactionMul = ResolveReactionMultiplier(rule, reactionState);
            return Mathf.Max(1f, Mathf.Max(reactionMul, breakExposedMultiplier));
        }

        // 정책에 명시된 값이 있으면 사용(1 미만은 보너스 없음으로 클램프), 0이면 기본값으로 폴백.
        // 신규/기존 에셋 모두 일관되게 동작하도록 0을 "기본값 사용" 센티넬로 쓴다(Unity 직렬화 시 신규 필드는 0으로 역직렬화됨).
        private static float ResolveReactionMultiplier(CombatReactionPolicySO.GradeRule rule, CombatReactionState state)
        {
            float raw = rule == null ? 0f : state switch
            {
                CombatReactionState.Hit => rule.hitVulnerabilityMultiplier,
                CombatReactionState.Stun => rule.stunVulnerabilityMultiplier,
                CombatReactionState.Knockdown => rule.knockdownVulnerabilityMultiplier,
                CombatReactionState.Airborne => rule.airborneVulnerabilityMultiplier,
                CombatReactionState.Grabbed => rule.grabbedVulnerabilityMultiplier,
                _ => 0f,
            };

            return raw > 0f ? Mathf.Max(1f, raw) : DefaultReactionMultiplier(state);
        }

        /// <summary> 정책 미지정 시 사용하는 리액션 상태별 기본 취약 배율(장르 로드맵 권장치). </summary>
        public static float DefaultReactionMultiplier(CombatReactionState state)
            => state switch
            {
                CombatReactionState.Stun => 1.2f,
                CombatReactionState.Knockdown => 1.3f,
                CombatReactionState.Airborne => 1.25f,
                CombatReactionState.Grabbed => 1.1f,
                _ => 1f, // Hit / None: 일반 경직은 보너스 없음
            };
    }
}
