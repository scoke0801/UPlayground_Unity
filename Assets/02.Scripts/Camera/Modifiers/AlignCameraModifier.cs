using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// (300) 명시적 정렬과 이동 기반 자동 리센터링을 처리한다.
    /// 수동 입력 직후에는 개입하지 않고, 접지 이동이 계속될 때만 진행 방향으로 느리게 수렴한다.
    /// </summary>
    public sealed class AlignCameraModifier : ICameraModifier, ICameraModifierLifecycle
    {
        private bool _wasAligning;
        private float _elapsed;
        private float _startYaw;
        private float _startPitch;
        private float _targetYaw;
        private float _targetPitch;
        private Transform _recenteringTarget;
        private float _movementDuration;
        private bool _isYawRecentering;

        public int Priority => 300;

        public void OnEnter(CameraContext context, CameraModeEnterParams enterParams)
        {
            ResetAlignment();
            ResetAutoRecentering();
        }

        public void OnExit(CameraContext context)
        {
            ResetAlignment();
            ResetAutoRecentering();
        }

        public void Apply(ref CameraFrame frame)
        {
            CameraContext context = frame.Context;
            if (context?.Settings == null || frame.State == null
                || context.Target == null || context.IsInputLocked)
            {
                ResetAutoRecentering();
                return;
            }

            if (_recenteringTarget != context.Target)
            {
                ResetAutoRecentering();
                _recenteringTarget = context.Target;
            }

            if (!context.IsAligning)
            {
                ResetAlignment();
                ApplyAutoRecentering(frame);
                return;
            }

            ResetAutoRecentering();
            CameraSettings settings = context.Settings;
            CameraState state = frame.State;
            float deltaTime = frame.DeltaTime;

            if (!_wasAligning)
            {
                Vector3 fwd = ResolveTargetForwardXZ(context.Target);
                bool isCombat = context.CombatStateProvider?.Invoke() ?? false;

                _elapsed = 0f;
                _startYaw = state.CurrentYaw;
                _startPitch = state.CurrentPitch;
                _targetYaw = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
                _targetPitch = isCombat ? settings.combatPitch : settings.explorePitch;
                _wasAligning = true;
            }

            float duration = Mathf.Max(settings.alignDuration, 0f);
            _elapsed += Mathf.Max(deltaTime, 0f);

            if (duration <= 0f || _elapsed >= duration)
            {
                state.CurrentYaw = _targetYaw;
                state.CurrentPitch = _targetPitch;
                context.AlignTimer = 0f;
                context.IsAligning = false;
                _wasAligning = false;
            }
            else
            {
                float t = Mathf.Clamp01(_elapsed / duration);
                float easedT = t * t * (3f - 2f * t);
                state.CurrentYaw = Mathf.LerpAngle(_startYaw, _targetYaw, easedT);
                state.CurrentPitch = Mathf.Lerp(_startPitch, _targetPitch, easedT);
                context.AlignTimer = duration - _elapsed;
            }

        }

        private void ApplyAutoRecentering(CameraFrame frame)
        {
            CameraContext context = frame.Context;
            CameraSettings settings = context.Settings;
            CameraState state = frame.State;
            CameraMotionContext motion = context.Motion;
            CameraUserPreferences preferences = CameraRuntimeServices.Adapter.UserPreferences;

            if (!settings.enableAutoRecentering
                || !preferences.MovementRecenteringEnabled
                || (context.CombatStateProvider?.Invoke() ?? false)
                || (context.RotationTransition?.IsActive ?? false)
                || !motion.IsAvailable
                || !motion.IsGrounded
                || context.LookAtOverride != null
                || (context.LockOn?.IsActive ?? false)
                || Time.unscaledTime - context.LastManualInputTime < settings.recenterInputDelay)
            {
                ResetAutoRecentering();
                return;
            }

            Vector3 planarVelocity = motion.PlanarVelocity;
            float minimumSpeed = Mathf.Max(0f, settings.recenterMinPlanarSpeed);
            if (planarVelocity.sqrMagnitude <= 0f
                || planarVelocity.sqrMagnitude < minimumSpeed * minimumSpeed)
            {
                ResetAutoRecentering();
                return;
            }

            float deltaTime = Mathf.Max(frame.DeltaTime, 0f);
            if (deltaTime <= 0f)
                return;

            float targetYaw = Mathf.Atan2(planarVelocity.x, planarVelocity.z) * Mathf.Rad2Deg;
            float yawDelta = Mathf.DeltaAngle(state.CurrentYaw, targetYaw);
            float yawError = Mathf.Abs(yawDelta);
            // 카메라 쪽으로 물러나는 이동을 따라 돌면 플레이어가 확인하던 전방을 잃는다.
            if (yawError > Mathf.Clamp(settings.recenterMaxHeadingAngle, 0f, 180f))
            {
                ResetAutoRecentering();
                return;
            }

            _movementDuration += deltaTime;
            if (_movementDuration < Mathf.Max(0f, settings.recenterMovementDelay))
                return;

            float stopAngle = Mathf.Clamp(settings.recenterYawDeadZone.x, 0f, 180f);
            float startAngle = Mathf.Clamp(settings.recenterYawDeadZone.y, stopAngle, 180f);
            _isYawRecentering = _isYawRecentering
                ? yawError > stopAngle
                : yawError > startAngle;

            float yawBlend = 1f - Mathf.Exp(
                -deltaTime / Mathf.Max(settings.recenterYawSmoothTime, 0.01f));
            float pitchBlend = 1f - Mathf.Exp(
                -deltaTime / Mathf.Max(settings.recenterPitchSmoothTime, 0.01f));
            float targetPitch = Mathf.Clamp(settings.explorePitch,
                settings.minVerticalAngle, settings.maxVerticalAngle);

            if (_isYawRecentering)
            {
                // 화면 중앙까지 계속 끌지 않고 허용 각도의 가장자리에서 멈춘다.
                float maximumStep = Mathf.Min(yawError - stopAngle,
                    Mathf.Max(0f, settings.recenterMaxYawSpeed) * deltaTime);
                state.CurrentYaw = Mathf.MoveTowardsAngle(state.CurrentYaw,
                    Mathf.LerpAngle(state.CurrentYaw, targetYaw, yawBlend), maximumStep);
                float remainingError = Mathf.Abs(Mathf.DeltaAngle(state.CurrentYaw, targetYaw));
                _isYawRecentering = remainingError > stopAngle
                    && !Mathf.Approximately(remainingError, stopAngle);
            }
            state.CurrentPitch = Mathf.MoveTowardsAngle(state.CurrentPitch,
                Mathf.LerpAngle(state.CurrentPitch, targetPitch, pitchBlend),
                Mathf.Max(0f, settings.recenterMaxPitchSpeed) * deltaTime);
        }

        private void ResetAutoRecentering()
        {
            _recenteringTarget = null;
            _movementDuration = 0f;
            _isYawRecentering = false;
        }

        private void ResetAlignment()
        {
            _wasAligning = false;
            _elapsed = 0f;
        }

        private static Vector3 ResolveTargetForwardXZ(Transform target)
        {
            if (target != null)
            {
                Vector3 targetForward = target.forward;
                targetForward.y = 0f;
                if (targetForward.sqrMagnitude > 0.001f)
                    return targetForward.normalized;
            }

            return Vector3.forward;
        }
    }
}
