using UnityEngine;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// (850) 충돌 보정 *이후* 적용되는 이펙트: 위치 델타(펀치/쉐이크 평행이동)와 FOV.
    /// 원본: InGameCameraMode.EvaluatePose 라인 124-131
    /// </summary>
    public sealed class EffectPositionFovCameraModifier : ICameraModifier
    {
        public int Priority => 850;

        public void Apply(ref CameraFrame frame)
        {
            CameraContext context = frame.Context;
            if (context?.Settings == null) return;

            // 위치 델타 (원본 라인 124)
            frame.Pose.CameraPosition += frame.Effects.positionDelta;

            // FOV (원본 라인 126-131)
            float baseFOV = context.DistanceController?.BaseFOV ?? context.Settings.fovExplore;
            float fov = context.MainCamera != null ? context.MainCamera.fieldOfView : context.Settings.fovExplore;
            if (Mathf.Abs(frame.Effects.fovDelta) > 0.001f)
                fov = baseFOV + frame.Effects.fovDelta;
            else if (!context.HasActiveEffects)
                fov = baseFOV;

            frame.Pose.FieldOfView = fov;
        }
    }
}
