using UnityEngine;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 카메라 모드 간 공유되는 런타임 상태.
    /// 기존 CameraManager 필드와 동기화하면서 단계적으로 이전한다.
    /// </summary>
    public class CameraState
    {
        public float CurrentYaw;
        public float CurrentPitch;
        public float CurrentDistance;
        public float TargetDistance;
        public Vector3 CameraOffset;
        public Vector3 SmoothPosition;
        public Vector3 PositionVelocity;
        public Vector3 OffsetVelocity;

        public void Reset(
            float yaw,
            float pitch,
            float currentDistance,
            float targetDistance,
            Vector3 cameraOffset,
            Vector3 smoothPosition)
        {
            CurrentYaw = yaw;
            CurrentPitch = pitch;
            CurrentDistance = currentDistance;
            TargetDistance = targetDistance;
            CameraOffset = cameraOffset;
            SmoothPosition = smoothPosition;
            PositionVelocity = Vector3.zero;
            OffsetVelocity = Vector3.zero;
        }
    }
}
