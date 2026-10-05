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

            Vector3 displacement = frame.Effects.positionDelta;
            float distance = displacement.magnitude;
            if (context.Collision != null && distance > 0.0001f)
            {
                // 스프링암이 확보한 안전 위치에서 연출 이동 경로도 검사한다.
                // 조회만 사용하여 흔들림이 충돌 복귀 거리와 유지 시간에 누적되지 않게 한다.
                Vector3 direction = displacement / distance;
                distance = context.Collision.GetAvailableDistance(frame.Pose.CameraPosition, direction, distance);
                displacement = direction * distance;
            }
            frame.Pose.CameraPosition += displacement;

            float baseFOV = context.DistanceController?.BaseFOV ?? context.Settings.fovExplore;
            // 다른 채널의 연출이 남아 있어도 종료된 FOV 델타를 렌더 카메라에서 다시 읽지 않는다.
            frame.Pose.FieldOfView = baseFOV + frame.Effects.fovDelta;
        }
    }
}
