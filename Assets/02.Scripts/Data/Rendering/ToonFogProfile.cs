using UnityEngine;

namespace UPlayGround.Data.Rendering
{
    /// <summary>한 지역의 공기 밀도와 팔레트 원본이다.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/Rendering/Toon Fog Profile")]
    public sealed class ToonFogProfile : ScriptableObject
    {
        [Header("거리")]
        public bool fogEnabled = true;
        [Min(0)] public float distanceDensity = 0.0012f;
        [Min(0)] public float colorNear = 20;
        [Min(0.01f)] public float colorFar = 500;

        [Header("높이")]
        public bool heightEnabled = true;
        [Min(0)] public float heightDensity = 0.006f;
        public float baseHeight;
        [Min(0.01f)] public float heightScale = 18;
        [Min(0)] public float localDensityMultiplier = 1;

        [Header("팔레트")]
        public Gradient distanceGradient = new();
        public Color heightTint = Color.gray;

        [Header("가시성")]
        [Min(0)] public float nearClearDistance = 8;
        [Min(0.01f)] public float maxEvaluationDistance = 2000;
        [Range(0, 1)] public float maxOpacity = 0.9f;
        [Min(0)] public float regionTransitionSeconds = 1.5f;

        public int Revision { get; private set; }

        private void OnValidate()
        {
            Revision++;
        }

        /// <summary>잘못된 구간이나 밀도가 GPU로 전달되는 것을 막는다.</summary>
        public bool IsValid =>
            float.IsFinite(distanceDensity) && distanceDensity >= 0
            && float.IsFinite(heightDensity) && heightDensity >= 0
            && float.IsFinite(localDensityMultiplier) && localDensityMultiplier >= 0
            && float.IsFinite(baseHeight) && float.IsFinite(heightScale) && heightScale >= 0.01f
            && float.IsFinite(nearClearDistance) && nearClearDistance >= 0
            && float.IsFinite(maxEvaluationDistance) && maxEvaluationDistance >= nearClearDistance
            && float.IsFinite(colorNear) && colorNear >= 0 && float.IsFinite(colorFar) && colorFar > colorNear
            && float.IsFinite(maxOpacity) && maxOpacity >= 0 && maxOpacity <= 1
            && float.IsFinite(regionTransitionSeconds) && regionTransitionSeconds >= 0
            && float.IsFinite(heightTint.r) && float.IsFinite(heightTint.g) && float.IsFinite(heightTint.b)
            && distanceGradient != null;
    }
}
