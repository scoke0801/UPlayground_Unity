namespace UPlayGround.Simulation
{
    /// <summary>
    /// Unity 오브젝트와 무관한 상태 전환 정책. 거리 판정과 우선순위를 한곳에 유지한다.
    /// </summary>
    public static class ActorSimulationPolicy
    {
        public static ActorSimulationState Evaluate(
            ActorSimulationState current,
            bool hasPlayer,
            bool hasActiveLease,
            bool canSuspend,
            float distanceSquared,
            float wakeDistanceSquared,
            float sleepDistanceSquared,
            float time,
            float lastActivatedTime,
            float minimumActiveDuration,
            out ActorSimulationTransitionReason reason)
        {
            if (!hasPlayer)
                return Active(ActorSimulationTransitionReason.PlayerUnavailable, out reason);
            if (hasActiveLease)
                return Active(ActorSimulationTransitionReason.ActiveLease, out reason);
            if (!canSuspend)
                return Active(ActorSimulationTransitionReason.Unsafe, out reason);

            if (current == ActorSimulationState.Suspended)
            {
                if (distanceSquared <= wakeDistanceSquared)
                    return Active(ActorSimulationTransitionReason.WakeDistance, out reason);

                reason = ActorSimulationTransitionReason.None;
                return current;
            }

            if (time - lastActivatedTime < minimumActiveDuration)
            {
                reason = ActorSimulationTransitionReason.MinimumActiveDuration;
                return current;
            }

            if (distanceSquared >= sleepDistanceSquared)
            {
                reason = ActorSimulationTransitionReason.SleepDistance;
                return ActorSimulationState.Suspended;
            }

            reason = ActorSimulationTransitionReason.None;
            return current;
        }

        private static ActorSimulationState Active(
            ActorSimulationTransitionReason transitionReason,
            out ActorSimulationTransitionReason reason)
        {
            reason = transitionReason;
            return ActorSimulationState.Active;
        }
    }
}
