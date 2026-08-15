using UnityEngine;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 카메라 구도 계산에 필요한 이동 스냅샷. Actor/KCC 구체 타입을 Camera 모듈에 노출하지 않는다.
    /// </summary>
    public readonly struct CameraMotionContext
    {
        public readonly bool IsAvailable;
        public readonly bool IsGrounded;
        public readonly Vector3 Velocity;
        public readonly Vector3 GroundNormal;
        public readonly Vector3 Up;

        public CameraMotionContext(
            bool isAvailable,
            bool isGrounded,
            Vector3 velocity,
            Vector3 groundNormal,
            Vector3 up)
        {
            IsAvailable = isAvailable;
            IsGrounded = isGrounded;
            Velocity = velocity;
            Up = up.sqrMagnitude > 0.0001f ? up.normalized : Vector3.up;
            GroundNormal = groundNormal.sqrMagnitude > 0.0001f
                ? groundNormal.normalized
                : Up;
        }

        public Vector3 PlanarVelocity => Vector3.ProjectOnPlane(Velocity, Up);
        public float PlanarSpeed => PlanarVelocity.magnitude;
        public float VerticalSpeed => Vector3.Dot(Velocity, Up);

        public static CameraMotionContext Unavailable => new CameraMotionContext(
            false,
            false,
            Vector3.zero,
            Vector3.up,
            Vector3.up);
    }

    /// <summary>
    /// 이동 구현이 Camera 모듈에 제공하는 소비자 소유 계약.
    /// </summary>
    public interface ICameraMotionProvider
    {
        bool TryGetCameraMotionContext(out CameraMotionContext context);
    }
}
