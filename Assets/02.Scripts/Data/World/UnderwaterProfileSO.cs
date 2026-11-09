using UnityEngine;

namespace UPlayGround.Data.World
{
    /// <summary>호수 수중의 거리별 색 흡수와 수면 전환을 조정한다.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/World/Underwater Profile")]
    public sealed class UnderwaterProfileSO : ScriptableObject
    {
        public Color scatteringColor = new(0.08f, 0.26f, 0.24f, 1);
        public Vector3 extinction = new(0.16f, 0.075f, 0.06f);
        [Min(0.01f)] public float surfaceBlendDepth = 0.18f;
        [Min(1)] public float maximumVisibility = 65;
        [Range(0, 1)] public float minimumLight = 0.06f;
        [Range(0, 0.5f)] public float causticStrength = 0.12f;
        [Min(0.1f)] public float causticScale = 1.5f;
        [Min(0)] public float causticSpeed = 0.45f;

        /// <summary>수면 위에서는 꺼지고 잠긴 깊이에 따라 연속적으로 적용한다.</summary>
        public float EvaluateImmersion(float depth) =>
            Mathf.SmoothStep(0, 1, Mathf.Clamp01(depth / Mathf.Max(0.01f, surfaceBlendDepth)));
    }
}
