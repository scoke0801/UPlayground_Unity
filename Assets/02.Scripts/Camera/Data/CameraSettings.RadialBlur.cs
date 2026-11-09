using UnityEngine;

namespace UPlayGround.Data
{
    public partial class CameraSettings
    {
        [Header("활공 가속 가장자리 블러")]
        public bool enableSpeedRadialBlur = true;
        [Min(0f)] public float speedRadialBlurDelay = 0.3f;
        [Range(0f, 1f)] public float speedRadialBlurSpeedThreshold = 0.35f;
        [Tooltip("진입 기준보다 낮게 설정해 속도 경계에서 블러가 반복해서 꺼지는 현상을 막는다.")]
        [Range(0f, 1f)] public float speedRadialBlurReleaseThreshold = 0.22f;
        [Tooltip("1보다 작으면 중간 가속 구간에서도 속도 효과가 분명하게 나타난다.")]
        [Range(0.25f, 2f)] public float speedRadialBlurResponseExponent = 0.65f;
        [Min(0.01f)] public float speedRadialBlurFadeIn = 0.2f;
        [Min(0.01f)] public float speedRadialBlurFadeOut = 0.15f;
        [Range(0f, 0.1f)] public float glidingRadialBlurStrength = 0.085f;
        [Tooltip("화면 높이에 대한 최대 잔상 길이. 측면 비행에서도 과도하게 늘어나지 않게 제한한다.")]
        [Range(0f, 0.15f)] public float speedRadialBlurMaxRadius = 0.065f;
        [Range(0.4f, 0.85f)] public float speedRadialBlurInnerRadius = 0.55f;
        [Range(0.85f, 1.4f)] public float speedRadialBlurOuterRadius = 0.95f;
        [Range(0f, 1f)] public float radialBlurSpeedLineScale = 0.7f;
    }
}
