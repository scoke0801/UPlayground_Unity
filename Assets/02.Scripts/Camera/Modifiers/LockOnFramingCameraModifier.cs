using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>플레이어와 대상의 관계만으로 락온 각도를 계산해 벽 충돌과 추적 회전을 분리한다.</summary>
    public sealed class LockOnFramingCameraModifier : ICameraModifier, ICameraModifierLifecycle
    {
        private Transform _previousTarget;
        private float _yawVelocity;
        private float _pitchVelocity;
        public int Priority => 730;

        /// <summary>모드 진입 시 이전 대상의 회전 관성을 제거한다.</summary>
        public void OnEnter(CameraContext context, CameraModeEnterParams enterParams) => Reset();

        /// <summary>모드 종료 시 락온 추적 상태를 정리한다.</summary>
        public void OnExit(CameraContext context) => Reset();

        /// <summary>충돌로 바뀌지 않는 기준 위치에서 수평 방향과 높이차를 추적한다.</summary>
        public void Apply(ref CameraFrame frame)
        {
            CameraContext context = frame.Context;
            if (context?.Settings == null || frame.State == null || context.Target == null
                || context.LockOn == null) return;
            if (!context.LockOn.CanTrack || context.IsInputLocked || context.LookAtOverride != null
                || (context.RotationTransition?.IsActive ?? false))
            {
                Reset();
                return;
            }
            if (_previousTarget != context.LockOn.CurrentTarget)
            {
                Reset();
                _previousTarget = context.LockOn.CurrentTarget;
            }

            CameraSettings settings = context.Settings;
            CameraPose pose = frame.Pose;
            // 보정된 피벗과 실제 암 길이를 추적에 사용하면 양 벽의 접촉이 회전을 다시 바꾼다.
            Vector3 offset = context.LockOn.FocusPosition - frame.PivotBase;
            float planarDistance = new Vector2(offset.x, offset.z).magnitude;
            float targetYaw = pose.Yaw;
            float targetPitch = pose.Pitch;
            if (planarDistance > Mathf.Max(0.001f, settings.lockOnYawSingularityRadius))
            {
                targetYaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
                float preferredPitch = settings.lockOnPreferredPitch * Mathf.Deg2Rad;
                float referenceDistance = Mathf.Clamp(settings.lockOnDistance, settings.minDistance, settings.maxDistance);
                float height = offset.y - referenceDistance * Mathf.Sin(preferredPitch);
                float depth = planarDistance + referenceDistance * Mathf.Cos(preferredPitch);
                targetPitch = -Mathf.Atan2(height, Mathf.Max(0.001f, depth)) * Mathf.Rad2Deg;
            }
            else
            {
                _yawVelocity = 0f;
                _pitchVelocity = 0f;
            }
            float minimumPitch = Mathf.Clamp(settings.lockOnPitchLimits.x, -89f, 89f);
            float maximumPitch = Mathf.Clamp(settings.lockOnPitchLimits.y, minimumPitch, 89f);
            targetPitch = Mathf.Clamp(targetPitch, minimumPitch, maximumPitch);
            if (frame.DeltaTime > 0f)
            {
                pose.Yaw = Mathf.SmoothDampAngle(pose.Yaw, targetYaw, ref _yawVelocity,
                    Mathf.Max(0.01f, settings.lockOnResponseTime.x), Mathf.Max(0f, settings.lockOnMaxAngularSpeed.x), frame.DeltaTime);
                pose.Pitch = Mathf.SmoothDampAngle(pose.Pitch, targetPitch, ref _pitchVelocity,
                    Mathf.Max(0.01f, settings.lockOnResponseTime.y), Mathf.Max(0f, settings.lockOnMaxAngularSpeed.y), frame.DeltaTime);
            }
            pose.Yaw = Mathf.DeltaAngle(0f, pose.Yaw);
            pose.Pitch = Mathf.Clamp(pose.Pitch, minimumPitch, maximumPitch);
            pose.CameraRotation = Quaternion.Euler(pose.Pitch, pose.Yaw, 0f);
            pose.CameraPosition = pose.PivotPosition + pose.CameraRotation * Vector3.back * pose.Distance;
            frame.State.CurrentYaw = pose.Yaw;
            frame.State.CurrentPitch = pose.Pitch;
            frame.Pose = pose;
        }

        private void Reset()
        {
            _previousTarget = null;
            _yawVelocity = 0f;
            _pitchVelocity = 0f;
        }
    }
}
