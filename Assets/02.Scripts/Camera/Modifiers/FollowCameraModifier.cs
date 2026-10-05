using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 피벗 추적과 기본 회전을 계산하여 락온 구도 보정 전의 포즈를 구성한다.
    /// </summary>
    public sealed class FollowCameraModifier : ICameraModifier, ICameraModifierLifecycle
    {
        private bool _rotationInitialized;
        private float _smoothedYaw;
        private float _smoothedPitch;
        private bool _verticalTrackingInitialized;
        private float _verticalFollowVelocity;

        public int Priority => 700;

        public void OnEnter(CameraContext context, CameraModeEnterParams enterParams)
        {
            _rotationInitialized = false;
            ResetVerticalTracking();
        }

        public void OnExit(CameraContext context)
        {
            _rotationInitialized = false;
            ResetVerticalTracking();
        }

        public void Apply(ref CameraFrame frame)
        {
            CameraContext context = frame.Context;
            if (context?.Settings == null || frame.State == null) return;
            if (context.MainCamera == null || context.Target == null || context.CameraPivot == null) return;

            CameraSettings settings = context.Settings;
            CameraState state = frame.State;
            float deltaTime = frame.DeltaTime;

            // 사용자 줌을 먼저 제한하고 이후 프레이밍 단계에서 추가 거리를 합성한다.
            float effectDistance = Mathf.Clamp(state.TargetDistance, settings.minDistance, settings.maxDistance);

            // posSmoothTime / rotSmoothTime 결정 (원본 라인 116-119)
            bool isLockOn = context.LockOn?.IsActive ?? false;
            float posSmoothTime = frame.Effects.positionSmoothTimeOverride ?? settings.positionSmoothTime;
            if (!isLockOn && !frame.KeepPositionSmoothing && context.LookAtOverride == null
                && !frame.Effects.positionSmoothTimeOverride.HasValue)
                posSmoothTime = 0f;
            float rotSmoothTime = frame.Effects.rotationSmoothTimeOverride ?? settings.rotationSmoothTime;
            // 정렬(IsAligning)은 조건에 넣지 않는다. 정렬 회전은 AlignCameraModifier가 자체 이징으로 보간하므로
            // 여기서 보간을 겹치면 목표보다 뒤처지고, 정렬 완료 프레임에 보간이 꺼지며 그 지연분이 한 번에 스냅된다.
            bool useDirectFreeOrbitRotation = !isLockOn
                                              && context.LookAtOverride == null
                                              && !(context.RotationTransition?.IsActive ?? false)
                                              && !frame.Effects.rotationSmoothTimeOverride.HasValue;
            if (useDirectFreeOrbitRotation)
                rotSmoothTime = 0f;
            // 락온 회전 Modifier가 보간하므로 같은 회전에 추가 보간을 적용하지 않는다.
            if (isLockOn && context.LookAtOverride == null && !context.IsInputLocked)
                rotSmoothTime = 0f;

            // 피벗 기준 위치 (원본 EvaluateCameraPosition 라인 281-303)
            Vector3 lockOnPivotOffset = context.LookAtOverride == null && context.LockOn != null
                ? context.LockOn.EvaluatePivotOffset(deltaTime)
                : Vector3.zero;

            Vector3 pivotBase = context.LookAtOverride != null
                ? context.LookAtOverride.position + context.LookAtOverrideOffset
                : context.Target.position + state.CameraOffset + lockOnPivotOffset;

            bool useTraversalVerticalTracking = settings.enableTraversalComposition
                                                && context.Motion.IsAvailable
                                                && !isLockOn
                                                && context.LookAtOverride == null
                                                && !frame.KeepPositionSmoothing
                                                && !frame.Effects.positionSmoothTimeOverride.HasValue;
            if (useTraversalVerticalTracking)
            {
                ApplyVerticalDeadZoneTracking(state, pivotBase, context.Motion, settings, deltaTime);
            }
            else if (posSmoothTime <= 0f)
            {
                ResetVerticalTracking();
                state.SmoothPosition = pivotBase;
                state.PositionVelocity = Vector3.zero;
            }
            else if (deltaTime > 0f)
            {
                ResetVerticalTracking();
                state.SmoothPosition = Vector3.SmoothDamp(
                    state.SmoothPosition,
                    pivotBase,
                    ref state.PositionVelocity,
                    posSmoothTime, Mathf.Infinity, deltaTime);
            }

            Vector3 pivotPosition = state.SmoothPosition;
            if (context.Collision != null && context.LookAtOverride == null)
            {
                // 어깨 오프셋과 위치 보간이 벽 안이나 반대편으로 넘어가면 모든 궤도 검사가 무효가 된다.
                // 플레이어 몸 중심의 같은 높이에서 피벗까지 경로를 먼저 확보한다.
                Vector3 playerAnchor = context.Target.position + Vector3.up * state.CameraOffset.y;
                pivotPosition = context.Collision.ConstrainPivotPosition(playerAnchor, pivotPosition);
            }
            frame.PivotBase = pivotBase;

            // 실제 회전과 카메라 궤도 위치에 같은 회전을 사용한다.
            // 비락온 자유 궤도는 입력 회전을 즉시 반영해야 충돌 SphereCast도 현재 입력 방향으로 수행된다.
            // 락온 보간은 후속 락온 회전 Modifier가 담당한다.
            Quaternion cameraRotation = EvaluateCameraRotation(
                context.MainCamera,
                state,
                rotSmoothTime,
                deltaTime);
            Vector3 camDir = cameraRotation * Vector3.back;
            Vector3 cameraPosition = pivotPosition + camDir * effectDistance;

            state.CurrentDistance = state.TargetDistance;

            frame.Pose.PivotPosition = pivotPosition;
            frame.Pose.CameraPosition = cameraPosition;
            frame.Pose.CameraRotation = cameraRotation;
            frame.Pose.Yaw = state.CurrentYaw;
            frame.Pose.Pitch = state.CurrentPitch;
            frame.Pose.Distance = state.TargetDistance;
        }

        private void ApplyVerticalDeadZoneTracking(
            CameraState state,
            Vector3 pivotBase,
            CameraMotionContext motion,
            CameraSettings settings,
            float deltaTime)
        {
            if (!_verticalTrackingInitialized)
            {
                state.SmoothPosition = pivotBase;
                state.PositionVelocity = Vector3.zero;
                _verticalFollowVelocity = 0f;
                _verticalTrackingInitialized = true;
                return;
            }

            if (deltaTime <= 0f)
                return;

            Vector3 smoothPosition = state.SmoothPosition;
            float deadZone = Mathf.Max(0f, settings.verticalTrackingDeadZone);
            float verticalDelta = pivotBase.y - smoothPosition.y;
            float targetY = Mathf.Abs(verticalDelta) <= deadZone
                ? smoothPosition.y
                : pivotBase.y - Mathf.Sign(verticalDelta) * deadZone;
            float smoothTime = motion.IsGrounded
                ? settings.groundedVerticalSmoothTime
                : motion.VerticalSpeed >= 0f
                    ? settings.airborneRiseVerticalSmoothTime
                    : settings.airborneFallVerticalSmoothTime;

            smoothPosition.x = pivotBase.x;
            smoothPosition.z = pivotBase.z;
            smoothPosition.y = Mathf.SmoothDamp(
                smoothPosition.y,
                targetY,
                ref _verticalFollowVelocity,
                Mathf.Max(0.01f, smoothTime),
                Mathf.Infinity,
                deltaTime);
            state.SmoothPosition = smoothPosition;
            state.PositionVelocity = Vector3.zero;
        }

        private void ResetVerticalTracking()
        {
            _verticalTrackingInitialized = false;
            _verticalFollowVelocity = 0f;
        }

        private Quaternion EvaluateCameraRotation(
            Camera mainCamera,
            CameraState state,
            float smoothTime,
            float deltaTime)
        {
            Quaternion targetRot = Quaternion.Euler(state.CurrentPitch, state.CurrentYaw, 0f);
            if (mainCamera == null || smoothTime <= 0f)
            {
                _smoothedYaw = state.CurrentYaw;
                _smoothedPitch = state.CurrentPitch;
                _rotationInitialized = true;
                return targetRot;
            }

            if (!_rotationInitialized)
            {
                Vector3 currentEuler = mainCamera.transform.rotation.eulerAngles;
                _smoothedYaw = NormalizeAngle(currentEuler.y);
                _smoothedPitch = NormalizeAngle(currentEuler.x);
                _rotationInitialized = true;
            }

            float blend = 1f - Mathf.Exp(-Mathf.Max(deltaTime, 0f) / smoothTime);
            _smoothedYaw = NormalizeAngle(Mathf.LerpAngle(_smoothedYaw, state.CurrentYaw, blend));
            _smoothedPitch = NormalizeAngle(Mathf.LerpAngle(_smoothedPitch, state.CurrentPitch, blend));

            return Quaternion.Euler(_smoothedPitch, _smoothedYaw, 0f);
        }

        private static float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle > 180f) angle -= 360f;
            if (angle < -180f) angle += 360f;
            return angle;
        }
    }
}
