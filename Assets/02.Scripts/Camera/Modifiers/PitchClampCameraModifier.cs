using UnityEngine;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// (650) 슬로프 보정을 반영한 권한 있는(authoritative) pitch 클램프.
    /// 이펙트 회전 주입(600) *이후*, Follow(700) *이전*에 위치해야 한다.
    /// 원본: InGameCameraMode.EvaluatePose 라인 103-105
    /// </summary>
    public sealed class PitchClampCameraModifier : ICameraModifier
    {
        public int Priority => 650;

        public void Apply(ref CameraFrame frame)
        {
            CameraContext context = frame.Context;
            if (context?.Settings == null || frame.State == null) return;

            float slopeOffset = context.ComputeSlopePitchOffset?.Invoke() ?? 0f;
            float dynamicMin = context.Settings.minVerticalAngle + slopeOffset;
            frame.State.CurrentPitch = Mathf.Clamp(frame.State.CurrentPitch, dynamicMin, context.Settings.maxVerticalAngle);
        }
    }
}
