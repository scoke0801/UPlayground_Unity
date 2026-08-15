using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// (700) 피벗 추적 위치(SmoothDamp)와 카메라 회전(yaw/pitch 축별 보간)을 산출한다.
    /// posSmoothTime 결정(락온/해제스무딩/LookAtOverride/이펙트 override)과 이펙트의 offset/distance 델타를
    /// 여기서 소비한다. 충돌 보정 이전의 *무충돌* 카메라 위치를 채우며, Collision(800)이 이를 덮어쓴다.
    /// 비스무딩 pivotBase는 frame.PivotBase로 Collision에 전달한다(LockOn.EvaluatePivotOffset 1회 호출 보장).
    /// 원본: InGameCameraMode.EvaluatePose 라인 107-123, 133 + EvaluateCameraPosition(Follow부) + EvaluateCameraRotation
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

            // 이펙트 오프셋 델타 (원본 라인 109) — state.CameraOffset에 직접 가산
            state.CameraOffset += frame.Effects.offsetDelta;

            // 이펙트 거리 델타 (원본 라인 107)
            // DistanceCeiling이 설정되면(락온 거리 피팅) 일반 maxDistance를 넘는 거리를 허용한다.
            float maxDistance = Mathf.Max(settings.maxDistance, frame.DistanceCeiling);
            float effectDistance = Mathf.Clamp(state.TargetDistance, settings.minDistance, maxDistance)
                                   + frame.Effects.distanceDelta;

            // posSmoothTime / rotSmoothTime 결정 (원본 라인 116-119)
            bool isLockOn = context.LockOn?.IsActive ?? false;
            float posSmoothTime = frame.Effects.positionSmoothTimeOverride ?? settings.positionSmoothTime;
            if (!isLockOn && !frame.KeepPositionSmoothing && context.LookAtOverride == null
                && !frame.Effects.positionSmoothTimeOverride.HasValue)
                posSmoothTime = 0f;
            float rotSmoothTime = frame.Effects.rotationSmoothTimeOverride ?? settings.rotationSmoothTime;
            bool useDirectFreeOrbitRotation = !isLockOn
                                              && context.LookAtOverride == null
                                              && !context.IsAligning
                                              && !(context.RotationTransition?.IsActive ?? false)
                                              && !frame.Effects.rotationSmoothTimeOverride.HasValue;
            if (useDirectFreeOrbitRotation)
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
            else
            {
                ResetVerticalTracking();
                state.SmoothPosition = Vector3.SmoothDamp(
                    state.SmoothPosition,
                    pivotBase,
                    ref state.PositionVelocity,
                    posSmoothTime);
            }

            Vector3 pivotPosition = state.SmoothPosition;
            frame.PivotBase = pivotBase;

            // 실제 회전과 카메라 궤도 위치에 같은 회전을 사용한다.
            // 비락온 자유 궤도는 입력 회전을 즉시 반영해야 충돌 SphereCast도 현재 입력 방향으로 수행된다.
            // 락온·명시적 정렬·연출 오버라이드는 각 경로의 스무딩을 유지한다.
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
                Mathf.Max(deltaTime, 0.0001f));
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
