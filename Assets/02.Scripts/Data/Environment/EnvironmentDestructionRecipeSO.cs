using UnityEngine;
using UPlayGround.Core.ReactiveEnvironment;

namespace UPlayGround.Data.ReactiveEnvironment
{
    public enum EnvironmentDestructionMode { Swap, DirectionalFall }
    public enum EnvironmentCompletionPolicy { PersistentRemnant, ClearAfterSettlement }

    /// <summary>이미 저작한 파손 형상의 전환·낙하·경로 보호 정책.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/Environment/파괴 레시피")]
    public sealed class EnvironmentDestructionRecipeSO : ScriptableObject
    {
        public EnvironmentDestructionMode mode;
        public EnvironmentCompletionPolicy completionPolicy;
        [Min(0)] public float settlementSeconds = 0.1f;
        [Min(0.01f)] public float blockedCollapseSeconds = 0.35f;
        [Min(0.01f)] public float clearingSeconds = 0.45f;
        [Min(0.01f)] public float fallSeconds = 1.2f;
        [Range(1, 4)] public float fallAcceleration = 1;
        [Range(1, 90)] public float fallAngle = 85;
        [Range(0.5f, 10)] public float sweepStepDegrees = 3;
        [Min(0.01f)] public float trunkLength = 4;
        [Min(0.01f)] public float trunkRadius = 0.2f;
        [Tooltip("낙하 피벗의 배율을 길이·반경에 적용한다. 기존 월드 단위 레시피는 끈다.")]
        public bool useLocalDimensions;
        public LayerMask fallBlockerLayers;
        public bool changesShelter;
        public bool changesNavigation;
        public bool IsValid => (completionPolicy == EnvironmentCompletionPolicy.PersistentRemnant
            || completionPolicy == EnvironmentCompletionPolicy.ClearAfterSettlement
            && EnvironmentReaction.IsNonNegativeFinite(settlementSeconds)
            && EnvironmentReaction.IsNonNegativeFinite(blockedCollapseSeconds) && blockedCollapseSeconds > 0
            && EnvironmentReaction.IsNonNegativeFinite(clearingSeconds) && clearingSeconds > 0)
            && (mode == EnvironmentDestructionMode.Swap
            || mode == EnvironmentDestructionMode.DirectionalFall && EnvironmentReaction.IsNonNegativeFinite(fallSeconds)
            && EnvironmentReaction.IsNonNegativeFinite(fallAcceleration) && fallAcceleration >= 1 && fallAcceleration <= 4
            && fallSeconds > 0 && fallAngle > 0 && fallAngle <= 90 && sweepStepDegrees >= 0.5f && sweepStepDegrees <= 10
            && EnvironmentReaction.IsNonNegativeFinite(trunkLength) && trunkLength > 0
            && EnvironmentReaction.IsNonNegativeFinite(trunkRadius) && trunkRadius > 0 && fallBlockerLayers.value != 0);
    }
}
