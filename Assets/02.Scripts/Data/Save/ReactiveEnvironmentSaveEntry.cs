using System;
using UPlayGround.Core.ReactiveEnvironment;

namespace UPlayGround.Data.Save
{
    /// <summary>동일 배치의 상태·전이·드롭 확정 여부를 함께 저장한다.</summary>
    [Serializable]
    public sealed class ReactiveEnvironmentSaveEntry
    {
        public string mapId, guid, definitionId;
        public string persistenceSignature;
        public int schemaVersion = 1;
        public EnvironmentReactionState state;
        public double pendingHeat, pendingWater;
        public double pendingContinuousWater;
        public float tickRemainder;
        public bool rewardCommitted;
        public EnvironmentDestructionSaveState destruction = new();
    }
}
