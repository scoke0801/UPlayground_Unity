namespace UPlayGround.Combat
{
    public enum CombatReactionState
    {
        None,
        Hit,
        Stun,
        Knockdown,
        Airborne,
        Grabbed,
    }

    public readonly struct ReactionDecision
    {
        public readonly bool ShouldApplyForce;
        public readonly bool ShouldEnterState;
        public readonly bool ShouldPlayCameraFeedback;
        public readonly CombatReactionState TargetState;

        public ReactionDecision(
            bool shouldApplyForce,
            bool shouldEnterState,
            bool shouldPlayCameraFeedback,
            CombatReactionState targetState)
        {
            ShouldApplyForce = shouldApplyForce;
            ShouldEnterState = shouldEnterState;
            ShouldPlayCameraFeedback = shouldPlayCameraFeedback;
            TargetState = targetState;
        }

        public static ReactionDecision None => new ReactionDecision(false, false, false, CombatReactionState.None);
    }
}
