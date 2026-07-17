namespace UPlayGround.AI.CombatDecision
{
    public enum CombatIntent
    {
        Attack,
        Punish,
        Counter,
        Pressure,
        Chase,
        Retreat,
        KeepDistance,
        Defend,
        Recover
    }

    public readonly struct CombatIntentEvaluation
    {
        public CombatIntentEvaluation(
            CombatIntent selectedIntent,
            float attackScore,
            float punishScore,
            float counterScore,
            float pressureScore,
            float chaseScore,
            float retreatScore,
            float keepDistanceScore,
            float defendScore,
            float recoverScore,
            string rhythmPhase,
            string reason)
        {
            SelectedIntent = selectedIntent;
            AttackScore = attackScore;
            PunishScore = punishScore;
            CounterScore = counterScore;
            PressureScore = pressureScore;
            ChaseScore = chaseScore;
            RetreatScore = retreatScore;
            KeepDistanceScore = keepDistanceScore;
            DefendScore = defendScore;
            RecoverScore = recoverScore;
            RhythmPhase = rhythmPhase;
            Reason = reason;
        }

        public CombatIntent SelectedIntent { get; }
        public float AttackScore { get; }
        public float PunishScore { get; }
        public float CounterScore { get; }
        public float PressureScore { get; }
        public float ChaseScore { get; }
        public float RetreatScore { get; }
        public float KeepDistanceScore { get; }
        public float DefendScore { get; }
        public float RecoverScore { get; }
        public string RhythmPhase { get; }
        public string Reason { get; }
    }
}
