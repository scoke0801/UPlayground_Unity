using System;
using System.Collections.Generic;
using UPlayGround.AI.CombatDecision;

namespace UPlayGround.AI.Debugging
{
    [Serializable]
    public class EncounterReplay
    {
        public string actorId;
        public string actorName;
        public float startTime;
        public float endTime;
        public List<ReplayFrame> frames = new();
        public List<ReplayEvent> events = new();
    }

    [Serializable]
    public class ReplayFrame
    {
        public float t;
        public CombatIntent selectedIntent;
        public CombatIntent lastIntent;
        public int consecutiveIntentCount;
        public float[] scores;
        public float distance;
        public float preferredRange;
        public float optimalRange;
        public float healthPercent;
        public float stamina;
        public string playerState;
        public string predictedNextPlayerAction;
        public float predictionConfidence;
        public string rhythmPhase;
        public string reason;
        public bool hasAttackSlot;
        public string resolverFailureReason;
    }

    [Serializable]
    public class ReplayEvent
    {
        public float t;
        public string eventType;
        public string detail;
    }
}
