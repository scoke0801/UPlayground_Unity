namespace UPlayGround.Combat
{
    public enum CombatTimelineEventType
    {
        ActionStarted,
        BeginCollision,
        EndCollision,
        HitPhaseChanged,
        ComboWindowOpened,
        ComboWindowClosed,
        MotionWarpStarted,
        MotionWarpEnded,
        ActionEnded,
    }

    public readonly struct CombatTimelineEvent
    {
        public readonly CombatTimelineEventType Type;
        public readonly int HitPhaseIndex;
        public readonly float Time;

        public CombatTimelineEvent(CombatTimelineEventType type, int hitPhaseIndex, float time)
        {
            Type = type;
            HitPhaseIndex = hitPhaseIndex;
            Time = time;
        }
    }
}
