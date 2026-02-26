using UnityEngine;

namespace UPlayGround
{
    /// <summary>
    /// 프레임별 카메라 이펙트 델타를 누적하는 구조체
    /// CameraEffectManager가 모든 활성 이펙트의 Apply()를 통해 채운 뒤
    /// CameraManager가 최종 카메라 상태에 적용한다.
    /// </summary>
    public struct CameraEffectState
    {
        // 가산형 델타
        public float yawDelta;
        public float pitchDelta;
        public float distanceDelta;
        public Vector3 offsetDelta;
        public float fovDelta;
        public Vector3 positionDelta;

        // 오버라이드형 (가장 높은 Priority의 non-null 값 적용)
        public float? positionSmoothTimeOverride;
        public float? rotationSmoothTimeOverride;
    }
}
