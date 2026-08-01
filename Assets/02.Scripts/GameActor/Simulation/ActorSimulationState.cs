namespace UPlayGround.Simulation
{
    public enum ActorSimulationState
    {
        Active,
        Suspended,
    }

    public enum ActorSimulationTransitionReason
    {
        None,
        PlayerUnavailable,
        ActiveLease,
        Unsafe,
        WakeDistance,
        SleepDistance,
        MinimumActiveDuration,
    }
}
