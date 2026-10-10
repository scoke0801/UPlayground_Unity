using UnityEngine;

namespace UPlayGround.Data.Party
{
    /// <summary>이미지의 윤곽과 독립적으로 자원이 퍼지는 순서를 결정한다.</summary>
    public enum CharacterGaugeFillMode
    {
        LeftToRight = 0,
        BottomToTop = 1,
        CenterOut = 2,
        Radial = 3,
        FlowMap = 4
    }

    /// <summary>전투 수치와 독립된 캐릭터 자원 문양 및 표시 연출.</summary>
    [CreateAssetMenu(fileName = "CharacterGauge_", menuName = "UPlayGround/UI/Character Gauge Profile")]
    public sealed class CharacterGaugeVisualProfileSO : ScriptableObject
    {
        public Sprite silhouetteSprite;
        [Tooltip("원본 투명 여백을 제외한 문양 영역입니다. 스프라이트 내부의 정규화 UV를 사용합니다.")]
        public Rect artworkUvRect = new(0f, 0f, 1f, 1f);
        [Tooltip("알파 채널로 에너지가 들어갈 영역만 제한합니다. 이미지 자체의 윤곽은 원본 알파를 사용합니다.")]
        public Texture2D fillMask;
        [Tooltip("FlowMap 방식의 R: 도달 순서, G: 흐름 위상. sRGB를 끈 선형 데이터 텍스처로 임포트합니다.")]
        public Texture2D flowMap;
        [Header("Shader Graph 충전 연출")]
        public CharacterGaugeFillMode fillMode = CharacterGaugeFillMode.CenterOut;
        [Tooltip("문양 내부 흐름의 반복 밀도. 전용 흐름 맵의 G 채널로 경로를 조절할 수 있습니다.")]
        [Range(1f, 8f)] public float flowFrequency = 3f;
        [Range(0f, 0.5f)] public float detailStrength = 0.12f;
        [Tooltip("0은 단색 문양, 1은 원본 캐릭터 이미지의 색과 명암을 보존합니다.")]
        [Range(0f, 1f)] public float imageColorInfluence = 1f;
        public CharacterGaugeVisualProfileSO fallbackProfile;
        [Tooltip("전용 문양이 준비되기 전 공용 장식을 사용 중인지 표시합니다.")]
        public bool usesPlaceholderArtwork;
        public Color baseColor = new(0.88f, 0.92f, 0.94f, 1f);
        public Color energyColor = new(0.72f, 0.96f, 0.84f, 1f);
        public Color readyColor = new(0.95f, 1f, 0.87f, 1f);
        public Vector2 referenceSize = new(430f, 36f);
        public Vector2 anchorOffset = new(0f, 39f);
        [Range(0.5f, 2f)] public float accessibilityScale = 1f;
        [Range(0.001f, 0.1f)] public float edgeWidth = 0.02f;
        [Range(0f, 0.02f)] public float noiseStrength = 0.004f;
        [Range(0f, 2f)] public float flowSpeed = 0.2f;
        [Range(0f, 1f)] public float glowIntensity = 0.2f;
        [Min(0f)] public float fillTweenDuration = 0.25f;
        [Min(0f)] public float readyPulseDuration = 0.3f;
        [Min(0f)] public float swapFadeDuration = 0.14f;

        public bool HasArtwork => silhouetteSprite != null;

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
