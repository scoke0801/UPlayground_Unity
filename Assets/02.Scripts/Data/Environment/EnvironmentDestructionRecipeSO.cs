using UnityEngine;
using UPlayGround.Core.ReactiveEnvironment;

namespace UPlayGround.Data.ReactiveEnvironment
{
    public enum EnvironmentDestructionMode { Swap, DirectionalFall }

    /// <summary>이미 저작한 파손 형상의 전환·낙하·경로 보호 정책.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/Environment/파괴 레시피")]
    public sealed class EnvironmentDestructionRecipeSO : ScriptableObject
    {
        public EnvironmentDestructionMode mode;
        [Min(0.01f)] public float fallSeconds = 1.2f;
        [Range(1, 4)] public float fallAcceleration = 1;
        [Range(1, 90)] public float fallAngle = 85;
        [Range(0.5f, 10)] public float sweepStepDegrees = 3;
        [Min(0.01f)] public float trunkLength = 4;
        [Min(0.01f)] public float trunkRadius = 0.2f;
        public LayerMask fallBlockerLayers;
        public bool changesShelter;
        public bool changesNavigation;
        public bool IsValid => mode == EnvironmentDestructionMode.Swap
            || mode == EnvironmentDestructionMode.DirectionalFall && EnvironmentReaction.IsNonNegativeFinite(fallSeconds)
            && EnvironmentReaction.IsNonNegativeFinite(fallAcceleration) && fallAcceleration >= 1 && fallAcceleration <= 4
            && fallSeconds > 0 && fallAngle > 0 && fallAngle <= 90 && sweepStepDegrees >= 0.5f && sweepStepDegrees <= 10
            && EnvironmentReaction.IsNonNegativeFinite(trunkLength) && trunkLength > 0
            && EnvironmentReaction.IsNonNegativeFinite(trunkRadius) && trunkRadius > 0 && fallBlockerLayers.value != 0;
    }
}
