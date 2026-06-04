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
    }
}
