using UnityEngine;

namespace UPlayGround.Data.Party
{
    /// <summary>전투 수치와 독립된 캐릭터 자원 문양 및 표시 연출.</summary>
    [CreateAssetMenu(fileName = "CharacterGauge_", menuName = "UPlayGround/UI/Character Gauge Profile")]
    public sealed class CharacterGaugeVisualProfileSO : ScriptableObject
    {
        public Sprite silhouetteSprite;
        public Texture2D fillMask;
        [Tooltip("R: 도달 순서, G: 흐름 위상, A: 문양 영역. 선형 색 공간으로 임포트합니다.")]
        public Texture2D flowMap;
        public CharacterGaugeVisualProfileSO fallbackProfile;
        [Tooltip("전용 문양이 준비되기 전 공용 장식을 사용 중인지 표시합니다.")]
        public bool usesPlaceholderArtwork;
        public Color baseColor = new(0.35f, 0.42f, 0.37f, 1f);
        public Color energyColor = new(0.72f, 0.96f, 0.84f, 1f);
        public Color readyColor = new(0.95f, 1f, 0.87f, 1f);
        public Vector2 referenceSize = new(240f, 60f);
        public Vector2 anchorOffset = new(0f, 42f);
        [Range(0.5f, 2f)] public float accessibilityScale = 1f;
        [Range(0.001f, 0.1f)] public float edgeWidth = 0.02f;
        [Range(0f, 0.02f)] public float noiseStrength = 0.004f;
        [Range(0f, 2f)] public float flowSpeed = 0.2f;
        [Range(0f, 1f)] public float glowIntensity = 0.2f;
        [Min(0f)] public float fillTweenDuration = 0.25f;
        [Min(0f)] public float readyPulseDuration = 0.3f;
        [Min(0f)] public float swapFadeDuration = 0.14f;

        public bool HasArtwork => silhouetteSprite != null && flowMap != null;

        /// <summary>누락 또는 순환 연결에서도 유한 단계로 공용 문양을 찾습니다.</summary>
        public CharacterGaugeVisualProfileSO ResolveArtwork()
        {
            var profile = this;
            for (int i = 0; i < 16 && profile != null; i++)
            {
                if (profile.HasArtwork) return profile;
                profile = profile.fallbackProfile;
            }
            return null;
        }
    }
}
