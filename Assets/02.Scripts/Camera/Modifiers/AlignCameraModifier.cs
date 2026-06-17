using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// (300) 카메라 자동 정렬. 타깃 전방을 향해 yaw를, 전투/탐색에 따라 pitch를 타이머 기반으로 보간한다.
    /// 원본: InGameCameraMode.UpdateCameraAlign + ResolveTargetForwardXZ
    /// </summary>
    public sealed class AlignCameraModifier : ICameraModifier
    {
        public int Priority => 300;

        public void Apply(ref CameraFrame frame)
        {
            CameraContext context = frame.Context;
            if (context?.Settings == null || frame.State == null) return;
            if (context.IsInputLocked || !context.IsAligning || context.Target == null) return;

            CameraSettings settings = context.Settings;
            CameraState state = frame.State;
            float deltaTime = frame.DeltaTime;
            bool isCombat = context.CombatStateProvider?.Invoke() ?? false;

            Vector3 fwd = ResolveTargetForwardXZ(context.Target);
            float targetYaw = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
            float targetPitch = isCombat ? settings.combatPitch : settings.explorePitch;

            if (context.AlignTimer <= deltaTime)
            {
                state.CurrentYaw = targetYaw;
                state.CurrentPitch = targetPitch;
                context.AlignTimer = 0f;
                context.IsAligning = false;
            }
            else
            {
                float remainingTime = Mathf.Max(context.AlignTimer, 0.001f);
                float yawStep = Mathf.Abs(Mathf.DeltaAngle(state.CurrentYaw, targetYaw)) / remainingTime * deltaTime;
                float pitchStep = Mathf.Abs(targetPitch - state.CurrentPitch) / remainingTime * deltaTime;

                state.CurrentYaw = Mathf.MoveTowardsAngle(state.CurrentYaw, targetYaw, yawStep);
                state.CurrentPitch = Mathf.MoveTowards(state.CurrentPitch, targetPitch, pitchStep);
                context.AlignTimer -= deltaTime;
            }

            float slopeOffset = context.ComputeSlopePitchOffset?.Invoke() ?? 0f;
            float dynamicMin = settings.minVerticalAngle + slopeOffset;
            state.CurrentPitch = Mathf.Clamp(state.CurrentPitch, dynamicMin, settings.maxVerticalAngle);
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
