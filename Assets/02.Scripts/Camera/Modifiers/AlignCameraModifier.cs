using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// (300) 카메라 자동 정렬. 타깃 전방을 향해 yaw를, 전투/탐색에 따라 pitch를 타이머 기반으로 보간한다.
    /// 원본: InGameCameraMode.UpdateCameraAlign + ResolveTargetForwardXZ
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
            if (!context.IsAligning || context.Target == null)
            {
                ResetAlignment();
                return;
            }
            if (context.IsInputLocked) return;

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

            float slopeOffset = context.ComputeSlopePitchOffset?.Invoke() ?? 0f;
            float dynamicMin = settings.minVerticalAngle + slopeOffset;
            state.CurrentPitch = Mathf.Clamp(state.CurrentPitch, dynamicMin, settings.maxVerticalAngle);
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
