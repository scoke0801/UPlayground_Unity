using System;

namespace UPlayGround.Data.Save
{
    /// <summary>전이 중에도 복원 가능한 파손 표현 포즈. 논리 연료는 부모 상태에 한 번만 둔다.</summary>
    [Serializable]
    public sealed class EnvironmentDestructionSaveState
    {
        public bool initialized, isFalling, usedFallback;
        public float progress;
        public SerializableVector3 direction;
        public EnvironmentDestructionSaveState Copy() => new()
        { initialized = initialized, isFalling = isFalling, usedFallback = usedFallback, progress = progress, direction = direction };
    }
}
