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

        public int Priority => 300;

        public void OnEnter(CameraContext context, CameraModeEnterParams enterParams)
        {
            ResetAlignment();
        }

        public void OnExit(CameraContext context)
        {
            ResetAlignment();
        }

        public void Apply(ref CameraFrame frame)
        {
            CameraContext context = frame.Context;
            if (context?.Settings == null || frame.State == null) return;
            if (context.Target == null) return;
            if (context.IsInputLocked) return;

            if (!context.IsAligning)
            {
                ResetAlignment();
                ApplyAutoRecentering(frame);
                return;
            }

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

        private static void ApplyAutoRecentering(CameraFrame frame)
        {
            CameraContext context = frame.Context;
            CameraSettings settings = context.Settings;
            CameraState state = frame.State;
            CameraMotionContext motion = context.Motion;

            if (!settings.enableAutoRecentering
                || !motion.IsAvailable
                || !motion.IsGrounded
                || context.LookAtOverride != null
                || (context.LockOn?.IsActive ?? false)
                || Time.unscaledTime - context.LastManualInputTime < settings.recenterInputDelay)
            {
                return;
            }

            Vector3 planarVelocity = motion.PlanarVelocity;
            if (planarVelocity.magnitude < settings.recenterMinPlanarSpeed)
                return;

            CameraUserPreferences preferences = CameraRuntimeServices.Adapter.UserPreferences;
            float preferenceScale = !preferences.IsAvailable || preferences.AimAssistEnabled
                ? Mathf.Max(0f, preferences.AutoCorrectionScale)
                : 0f;
            bool isCombat = context.CombatStateProvider?.Invoke() ?? false;
            float contextScale = isCombat ? settings.combatRecenterMultiplier : 1f;
            float strength = preferenceScale * Mathf.Clamp01(contextScale);
            if (strength <= 0f)
                return;

            float deltaTime = Mathf.Max(frame.DeltaTime, 0f);
            float yawBlend = 1f - Mathf.Exp(
                -deltaTime * strength / Mathf.Max(settings.recenterYawSmoothTime, 0.01f));
            float pitchBlend = 1f - Mathf.Exp(
                -deltaTime * strength / Mathf.Max(settings.recenterPitchSmoothTime, 0.01f));
            float targetYaw = Mathf.Atan2(planarVelocity.x, planarVelocity.z) * Mathf.Rad2Deg;
            float targetPitch = isCombat ? settings.combatPitch : settings.explorePitch;

            state.CurrentYaw = Mathf.LerpAngle(state.CurrentYaw, targetYaw, yawBlend);
            state.CurrentPitch = Mathf.LerpAngle(state.CurrentPitch, targetPitch, pitchBlend);
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
