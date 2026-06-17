using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// (400) 카메라 피벗 오프셋을 전투/탐색 타깃으로 SmoothDamp 보간한다.
    /// 락온 중에는 속도 기반 LookAhead 오프셋을 타깃에 합산한다(LookAhead는 오프셋 타깃에 직접
    /// 합산되는 구조라 별도 Modifier로 분리하지 않고 여기서 함께 처리 — 단일 책임에서 의도적 이탈).
    /// 원본: InGameCameraMode.UpdateOffsetAndDistance(오프셋부) + ComputeLookAheadOffset
    /// LookAhead 보간 상태(_lookAheadOffset/_lookAheadVelocity)를 인스턴스로 보유한다.
    /// </summary>
    public sealed class OffsetCameraModifier : ICameraModifier
    {
        private Vector3 _lookAheadOffset;
        private Vector3 _lookAheadVelocity;

        public int Priority => 400;

        public void Apply(ref CameraFrame frame)
        {
            CameraContext context = frame.Context;
            if (context?.Settings == null || frame.State == null) return;
            if (context.IsInputLocked) return;

            CameraSettings settings = context.Settings;
            CameraState state = frame.State;
            bool isCombat = context.CombatStateProvider?.Invoke() ?? false;
            bool isLockOn = context.LockOn?.IsActive ?? false;

            Vector3 targetOffset = isCombat ? settings.combatOffset : settings.defaultOffset;
            if (isLockOn)
                targetOffset += ComputeLookAheadOffset(context, settings);
            else
                ResetLookAheadOffset();

            state.CameraOffset = Vector3.SmoothDamp(
                state.CameraOffset,
                targetOffset,
                ref state.OffsetVelocity,
                settings.offsetSmoothTime);
        }

        private Vector3 ComputeLookAheadOffset(CameraContext context, CameraSettings settings)
        {
            Vector3 targetLookAhead = Vector3.zero;
            if (settings.enableLookAhead && context.PlayerVelocityProvider != null)
            {
                Vector3 velocity = Vector3.ProjectOnPlane(context.PlayerVelocityProvider.Invoke(), Vector3.up);
                float speed = velocity.magnitude;
                if (speed > 0.01f)
                {
                    float factor = Mathf.Clamp01(speed / Mathf.Max(settings.lookAheadSpeedRef, 0.01f));
                    targetLookAhead = velocity.normalized * (factor * settings.lookAheadDistance);

                    if (context.LockOn?.IsActive ?? false)
                        targetLookAhead *= settings.lockOnLookAheadMultiplier;
                }
            }

            _lookAheadOffset = Vector3.SmoothDamp(
                _lookAheadOffset,
                targetLookAhead,
                ref _lookAheadVelocity,
                settings.lookAheadSmoothTime);

            _lookAheadOffset.y = 0f;
            return _lookAheadOffset;
        }

        private void ResetLookAheadOffset()
        {
            _lookAheadOffset = Vector3.zero;
            _lookAheadVelocity = Vector3.zero;
        }
    }
}
