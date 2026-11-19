using UnityEngine;

namespace UPlayGround.Data.Rendering
{
    /// <summary>WorldToon 셰이더군이 공유하는 툰 명암·색조 기준. 변환 도구가 생성 재질에 기록하며 런타임 전역 상태를 만들지 않는다.</summary>
    [CreateAssetMenu(fileName = "WorldToonStyleProfile", menuName = "UPlayGround/렌더링/WorldToon Style Profile")]
    public sealed class WorldToonStyleProfileSO : ScriptableObject
    {
        [Tooltip("주광을 받는 면의 색조. 주광 색과 곱해진다.")]
        public Color litTint = Color.white;

        [Tooltip("어두운 면과 그림자 영역의 직접광 색조. 밤·실내의 최소 밝기는 환경광이 담당한다.")]
        public Color shadeTint = new Color(0.45f, 0.47f, 0.6f, 1f);

        [Tooltip("명암 경계가 위치하는 N·L 값.")]
        [Range(-1f, 1f)] public float bandThreshold = 0.05f;

        [Tooltip("명암 경계의 부드러움(반폭).")]
        [Range(0.001f, 0.5f)] public float bandSoftness = 0.04f;

        [Tooltip("프로브·라이트맵 간접광 배율.")]
        [Range(0f, 2f)] public float indirectStrength = 1f;

        [Tooltip("Standard 프리셋의 추가 광원(횃불·실내) 배율.")]
        [Range(0f, 2f)] public float additionalLightStrength = 1f;
    }
}
