using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Combat
{
    public readonly struct PlayerReactionQuery
    {
        public readonly bool IgnoreHitReaction;
        public readonly bool CanTransitionToHit;
        public readonly bool IsAlreadyHitOrGrabbed;
        public readonly bool ShouldEnterAirborne;

        public PlayerReactionQuery(
            bool ignoreHitReaction,
            bool canTransitionToHit,
            bool isAlreadyHitOrGrabbed,
            bool shouldEnterAirborne)
        {
            IgnoreHitReaction = ignoreHitReaction;
            CanTransitionToHit = canTransitionToHit;
            IsAlreadyHitOrGrabbed = isAlreadyHitOrGrabbed;
            ShouldEnterAirborne = shouldEnterAirborne;
        }
    }

    public readonly struct MonsterReactionQuery
    {
        public readonly bool PoiseBrokenNow;
        public readonly bool CanPlayHitReaction;
        public readonly bool ShouldEnterAirborne;
        public readonly bool CanEnterKnockdown;
        public readonly MonsterActorGrade Grade;
        public readonly CombatReactionPolicySO Policy;

        public MonsterReactionQuery(
            bool poiseBrokenNow,
            bool canPlayHitReaction,
            bool shouldEnterAirborne,
            bool canEnterKnockdown,
            MonsterActorGrade grade = MonsterActorGrade.Normal,
            CombatReactionPolicySO policy = null)
        {
            PoiseBrokenNow = poiseBrokenNow;
            CanPlayHitReaction = canPlayHitReaction;
            ShouldEnterAirborne = shouldEnterAirborne;
            CanEnterKnockdown = canEnterKnockdown;
            Grade = grade;
            Policy = policy;
        }
    }

    public static class ReactionResolver
    {
        public static ReactionDecision ResolvePlayerReaction(
            in PlayerReactionQuery query,
            AttackData attackData)
        {
            if (query.IgnoreHitReaction || attackData == null)
                return ReactionDecision.None;

            bool shouldEnterReactionBlock = !query.IsAlreadyHitOrGrabbed;
            bool shouldEnterState = shouldEnterReactionBlock && query.CanTransitionToHit;

            return new ReactionDecision(
                shouldApplyForce: true,
                shouldEnterState,
                shouldPlayCameraFeedback: shouldEnterReactionBlock,
                ResolveTargetState(attackData, query.ShouldEnterAirborne, attackData.reactionType == AttackReactionType.Knockdown));
        }

        public static ReactionDecision ResolveMonsterReaction(
            in MonsterReactionQuery query,
            AttackData attackData)
        {
            // guaranteedReaction: 등급 리액션 정책을 우회해 피격 리액션을 보장한다(보스 등 강인한 적도 흔들림).
            // 단 상태 기반 제외(Death/Hit/Airborne 등 = CanPlayHitReaction)는 안전상 유지한다.
            bool guaranteed = attackData != null && attackData.guaranteedReaction;
            bool shouldPlayHitReaction = attackData != null
                                         && attackData.reactionType != AttackReactionType.None
                                         && query.CanPlayHitReaction
                                         && (guaranteed
                                             || (attackData.forceReaction
                                                 && CombatPolicyResolver.AllowsMonsterForceReaction(query.Policy, query.Grade)));
            bool shouldApplyForce = query.PoiseBrokenNow || shouldPlayHitReaction;

            if (query.PoiseBrokenNow)
            {
                CombatReactionState poiseBreakState = query.CanEnterKnockdown
                    ? CombatReactionState.Knockdown
                    : CombatReactionState.Stun;
                poiseBreakState = ApplyMonsterPolicy(query, poiseBreakState);

                return new ReactionDecision(
                    shouldApplyForce,
                    shouldEnterState: poiseBreakState != CombatReactionState.None,
                    shouldPlayCameraFeedback: false,
                    poiseBreakState);
            }

            if (!shouldPlayHitReaction)
                return new ReactionDecision(shouldApplyForce, false, false, CombatReactionState.None);

            CombatReactionState targetState = ResolveTargetState(
                attackData,
                query.ShouldEnterAirborne,
                query.CanEnterKnockdown);

            // guaranteed면 상태 허용/Poise브레이크요구 정책을 모두 우회한다.
            if (!guaranteed)
            {
                targetState = ApplyMonsterPolicy(query, targetState);

                if (CombatPolicyResolver.RequiresPoiseBreakForMonsterState(query.Policy, query.Grade))
                    targetState = CombatReactionState.None;
            }

            return new ReactionDecision(
                shouldApplyForce,
                shouldEnterState: targetState != CombatReactionState.None,
                shouldPlayCameraFeedback: false,
                targetState);
        }

        private static CombatReactionState ApplyMonsterPolicy(
            in MonsterReactionQuery query,
            CombatReactionState state)
        {
            return CombatPolicyResolver.AllowsMonsterReactionState(query.Policy, query.Grade, state)
                ? state
                : CombatReactionState.None;
        }

        private static CombatReactionState ResolveTargetState(
            AttackData attackData,
            bool shouldEnterAirborne,
            bool canEnterKnockdown)
        {
            if (shouldEnterAirborne)
                return CombatReactionState.Airborne;

            return attackData?.reactionType switch
            {
                AttackReactionType.Grab => CombatReactionState.Grabbed,
                AttackReactionType.Stun => CombatReactionState.Stun,
                AttackReactionType.Knockdown when canEnterKnockdown => CombatReactionState.Knockdown,
                _ => CombatReactionState.Hit,
            };
        }
    }
}
