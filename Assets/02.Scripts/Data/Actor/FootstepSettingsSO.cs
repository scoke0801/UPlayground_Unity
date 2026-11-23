using System;
using UnityEngine;

namespace UPlayGround.Data.Actor
{
    /// <summary>모델의 접촉 허용 범위와 발 디딤 표현 예산을 관리한다.</summary>
    [CreateAssetMenu(fileName = "FootstepSettings_", menuName = "UPlayGround/환경/Footstep Settings")]
    public sealed class FootstepSettingsSO : ScriptableObject
    {
        public FootstepSurfaceProfileSO fallbackSurface;
        public FootstepSurfaceProfileSO[] surfaces = Array.Empty<FootstepSurfaceProfileSO>();
        public LayerMask groundLayers;
        [Min(0f)] public float probeHeight = 0.6f;
        [Min(0f)] public float probeDepth = 0.8f;
        [Min(0f)] public float maximumFootHeight = 0.45f;
        [Min(0f)] public float minimumMoveSpeed = 0.2f;
        [Min(0f)] public float minimumStepInterval = 0.08f;
        [Min(0f)] public float landingStepSuppression = 0.15f;
        [Min(0f)] public float surfaceOffset = 0.015f;
        [Range(1, 8)] public int instancesPerEffect = 4;
        public Vector2 sandSoleSize;
        [Min(0f)] public float sandAnkleToSole = 0.11f;
        public float sandSoleForwardOffset = 0.055f;
        [Min(0f)] public float sandContactGap = 0.07f;
        [Min(0f)] public float sandStepContactWindow = 0.18f;
        [Min(0.01f)] public float locomotionFootSwingDistance = 0.055f;
        [Min(0f)] public float locomotionFootSettleSpeed = 0.30f;
        [Min(0.01f)] public float poseFootRelocationDistance = 0.11f;
        [Min(0.01f)] public float poseFootLiftDistance = 0.085f;
        [Min(0f)] public float poseFootSettleSpeed = 0.55f;
        [Min(0f)] public float sandLandingContactWindow = 0.23f;
    }
}
