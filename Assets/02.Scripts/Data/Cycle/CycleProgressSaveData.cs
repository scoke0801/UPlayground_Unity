using System;
using System.Collections.Generic;

namespace UPlayGround.Data.Cycle
{
    [Serializable]
    public sealed class AssistCooldownEntry { public string assistId; public float remainingSeconds; }

    [Serializable]
    public sealed class AssistDefeatCountEntry
    {
        public string assistId;
        public int defeatCount;
    }

    [Serializable]
    public sealed class AssistProgressSaveData
    {
        public List<string> roster = new();
        public string equippedAssistId;
        public List<UPlayGround.Cycle.AssistPityEntry> pity = new();
        public List<AssistDefeatCountEntry> defeatCounts = new();
        public List<AssistCooldownEntry> cooldowns = new();
        public string pendingRecruitAssistId;
    }

    [Serializable]
    public sealed class CycleHistorySaveData
    {
        public int completedCycleCount;
        public int completionSequence;
        public string lastSettlementId;
    }

    [Serializable]
    public sealed class CycleSettlementPlan
    {
        public string settlementId;
        public List<CycleItemStack> materialRewards = new();
        public int completedCycleIndex;
        public bool discardRemains;
    }
}
