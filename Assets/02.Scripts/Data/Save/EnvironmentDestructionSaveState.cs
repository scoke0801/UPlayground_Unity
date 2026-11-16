using System;

namespace UPlayGround.Data.Save
{
    public enum EnvironmentCompletionPhase { Standing, Falling, Settled, Clearing, Cleared }
    public enum EnvironmentSettlementReason { None, Completed, BlockedDuringFall, BlockedInitially }

    /// <summary>누락된 구버전과 명시된 신규 버전을 구분하는 벌목 수명 기록.</summary>
    [Serializable]
    public sealed class EnvironmentCompletionSaveState
    {
        public int version;
        public EnvironmentCompletionPhase phase;
        public EnvironmentSettlementReason reason;
        public float elapsed;
        public bool hasRewardPosition;
        public SerializableVector3 rewardPosition;
        public EnvironmentCompletionSaveState Copy() => (EnvironmentCompletionSaveState)MemberwiseClone();
    }

    /// <summary>전이 중에도 복원 가능한 파손 표현 포즈. 논리 연료는 부모 상태에 한 번만 둔다.</summary>
    [Serializable]
    public sealed class EnvironmentDestructionSaveState
    {
        public bool initialized, isFalling, usedFallback;
        public float progress;
        public SerializableVector3 direction;
        public EnvironmentCompletionSaveState completion;
        public EnvironmentDestructionSaveState Copy() => new()
        { initialized = initialized, isFalling = isFalling, usedFallback = usedFallback, progress = progress, direction = direction,
            completion = completion?.Copy() };
    }
}
