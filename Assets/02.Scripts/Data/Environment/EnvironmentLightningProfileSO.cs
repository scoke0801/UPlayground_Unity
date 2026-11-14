using System;
using UnityEngine;

namespace UPlayGround.Data.ReactiveEnvironment
{
    [Serializable]
    public struct EnvironmentLightningArea
    {
        public Bounds sourceBounds;
        public int surfaceLayerId;
    }

    /// <summary>지역 원본 좌표의 낙뢰 후보·단발 열량·게임 시간 간격을 저작한다.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/Environment/날씨 낙뢰")]
    public sealed class EnvironmentLightningProfileSO : ScriptableObject
    {
        public string profileId;
        public EnvironmentLightningArea[] areas = Array.Empty<EnvironmentLightningArea>();
        public Vector2 intervalGameMinutes = new(8f, 22f);
        [Min(0f)] public float heatImpulse = 2f;
        [Min(0.01f)] public float radius = 3f;
        [Min(0.01f)] public float probeDistance = 500f;
        [Min(0f)] public float surfaceOffset = 0.05f;
        [Min(0.01f)] public float maximumAdvanceMinutes = 2f;
        public LayerMask surfaceLayers;
        public LayerMask blockerLayers;
        public int randomSeed = 731;
        public GameObject contactEffect;
        [Min(1)] public int contactParticles = 12;
        [Min(1)] public int maximumContactEffects = 4;
        [Min(1f)] public float maximumPresentationDistance = 300f;

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(profileId) || areas == null || areas.Length == 0
                || !Positive(intervalGameMinutes.x) || !Positive(intervalGameMinutes.y)
                || intervalGameMinutes.y < intervalGameMinutes.x || !Positive(heatImpulse)
                || !Positive(radius) || !Positive(probeDistance) || !Positive(maximumAdvanceMinutes)
                || !float.IsFinite(surfaceOffset) || surfaceOffset < 0f
                || surfaceLayers.value == 0 || blockerLayers.value == 0 || contactParticles < 1
                || maximumContactEffects < 1 || !Positive(maximumPresentationDistance)) return false;
            foreach (var area in areas)
            {
                var b = area.sourceBounds;
                if (!Finite(b.center) || !Finite(b.size) || b.size.x <= 0f || b.size.z <= 0f || b.size.y < 0f) return false;
            }
            return true;
        }

        private static bool Positive(float value) => float.IsFinite(value) && value > 0f;
        private static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
