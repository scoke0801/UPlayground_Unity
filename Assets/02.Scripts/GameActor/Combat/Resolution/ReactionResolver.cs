using UPlayGround.Data;
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

        public MonsterReactionQuery(
            bool poiseBrokenNow,
            bool canPlayHitReaction,
            bool shouldEnterAirborne,
            bool canEnterKnockdown)
        {
            PoiseBrokenNow = poiseBrokenNow;
            CanPlayHitReaction = canPlayHitReaction;
            ShouldEnterAirborne = shouldEnterAirborne;
            CanEnterKnockdown = canEnterKnockdown;
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
            bool shouldPlayHitReaction = attackData != null
                                         && attackData.reactionType != AttackReactionType.None
                                         && query.CanPlayHitReaction
                                         && attackData.forceReaction;
            bool shouldApplyForce = query.PoiseBrokenNow || shouldPlayHitReaction;

            if (query.PoiseBrokenNow)
            {
                return new ReactionDecision(
                    shouldApplyForce,
                    shouldEnterState: true,
                    shouldPlayCameraFeedback: false,
                    query.CanEnterKnockdown ? CombatReactionState.Knockdown : CombatReactionState.Stun);
            }

            if (!shouldPlayHitReaction)
                return new ReactionDecision(shouldApplyForce, false, false, CombatReactionState.None);

            return new ReactionDecision(
                shouldApplyForce,
                shouldEnterState: true,
                shouldPlayCameraFeedback: false,
                ResolveTargetState(attackData, query.ShouldEnterAirborne, query.CanEnterKnockdown));
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
