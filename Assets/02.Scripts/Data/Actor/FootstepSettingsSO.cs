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
    }
}
