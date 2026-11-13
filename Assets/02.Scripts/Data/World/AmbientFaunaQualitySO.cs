using UnityEngine;

namespace UPlayGround.Data.World
{
    /// <summary>실제 할당 용량과 예상 화면 점유의 품질 단계별 상한을 보관한다.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/World/Ambient Fauna Quality")]
    public sealed class AmbientFaunaQualitySO : ScriptableObject
    {
        [System.Serializable]
        public struct Tier
        {
            [Min(1)] public int Capacity, Groups;
            [Range(0.00001f, 0.1f)] public float ScreenFraction;
        }
        public Tier[] Tiers = {
            new() { Capacity = 256, Groups = 12, ScreenFraction = 0.0005f },
            new() { Capacity = 512, Groups = 24, ScreenFraction = 0.001f },
            new() { Capacity = 1024, Groups = 32, ScreenFraction = 0.002f }
        };
        [Min(0.05f)] public float EvaluationSeconds = 0.2f;

        /// <summary>범위 밖 품질 지정을 사용 가능한 단계로 제한한다.</summary>
        public Tier GetTier(int index) => Tiers == null || Tiers.Length == 0 ? default
            : Tiers[Mathf.Clamp(index, 0, Tiers.Length - 1)];
    }
}
