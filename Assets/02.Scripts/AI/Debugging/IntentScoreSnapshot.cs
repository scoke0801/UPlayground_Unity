using System;
using UPlayGround.AI.CombatDecision;

namespace UPlayGround.AI.Debugging
{
    [Serializable]
    public readonly struct IntentScoreSnapshot
    {
        public IntentScoreSnapshot(
            float time,
            CombatIntent selectedIntent,
            CombatIntent lastIntent,
            int consecutiveIntentCount,
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
            Time = time;
            SelectedIntent = selectedIntent;
            LastIntent = lastIntent;
            ConsecutiveIntentCount = consecutiveIntentCount;
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

        public float Time { get; }
        public CombatIntent SelectedIntent { get; }
        public CombatIntent LastIntent { get; }
        public int ConsecutiveIntentCount { get; }
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

        public float GetScore(CombatIntent intent)
        {
            return intent switch
            {
                CombatIntent.Attack => AttackScore,
                CombatIntent.Punish => PunishScore,
                CombatIntent.Counter => CounterScore,
                CombatIntent.Pressure => PressureScore,
                CombatIntent.Chase => ChaseScore,
                CombatIntent.Retreat => RetreatScore,
                CombatIntent.KeepDistance => KeepDistanceScore,
                CombatIntent.Defend => DefendScore,
                CombatIntent.Recover => RecoverScore,
                _ => 0f
            };
        }
    }
}
