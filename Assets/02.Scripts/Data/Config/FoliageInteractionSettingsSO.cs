using UnityEngine;

namespace UPlayGround.Data.Config
{
    /// <summary>풀이 액터에 밀려 눕는 연출의 감지 범위·강도·반경 설정.</summary>
    [CreateAssetMenu(
        fileName = "FoliageInteractionSettings",
        menuName = "UPlayGround/World/Foliage Interaction")]
    public sealed class FoliageInteractionSettingsSO : ScriptableObject
    {
        public const string ResourcePath = "Config/FoliageInteractionSettings";
        public const int MaxActorCount = 8;

        [Header("대상 선정")]
        [Tooltip("카메라 기준 이 거리 안의 액터만 풀에 영향을 준다.")]
        [Min(0f)] public float candidateRange = 25f;
        [Tooltip("대상 후보를 다시 고르는 간격(초). 위치와 강도는 매 프레임 갱신한다.")]
        [Min(0.05f)] public float candidateRefreshInterval = 0.25f;

        [Header("영향 범위")]
        [Tooltip("액터 중심에서 풀이 영향을 받는 반경(m).")]
        [Min(0.1f)] public float influenceRadius = 1.4f;
        [Tooltip("이동 속도(m/s)에 따른 눕힘 강도. 0~1로 해석한다.")]
        public AnimationCurve strengthBySpeed = new(
            new Keyframe(0f, 0.15f),
            new Keyframe(2f, 0.55f),
            new Keyframe(6f, 1f));
        [Tooltip("속도 변화를 부드럽게 따라가는 시간(초). 갑작스러운 튐을 막는다.")]
        [Min(0.01f)] public float strengthSmoothTime = 0.12f;

        [Header("변위")]
        [Tooltip("뿌리 마스크 1인 정점이 최대로 밀리는 거리(m). 시야 가림 방지 상한이기도 하다.")]
        [Min(0f)] public float maxBendDistance = 0.45f;
        [Tooltip("밀린 거리에 비례해 정점을 내리는 비율. 휘어 누운 느낌을 만든다.")]
        [Range(0f, 1f)] public float bendDropRatio = 0.6f;
    }
}
