using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// (800) 월드 충돌을 스프링암 길이로만 해소한다.
    /// 캐릭터는 충돌 레이어에서 제외하고 근접 표현은 ActorCameraProximityDither에 맡긴다.
    /// 바닥도 벽과 동일하게 암을 줄이며, 별도의 월드 Y 보정은 수행하지 않는다.
    /// </summary>
    public sealed class CollisionCameraModifier : ICameraModifier
    {
        public int Priority => 800;

        public void Apply(ref CameraFrame frame)
        {
            CameraContext context = frame.Context;
            if (context?.Settings == null || frame.State == null) return;

            CameraSettings settings = context.Settings;
            CameraState state = frame.State;

            Vector3 pivotPosition = frame.Pose.PivotPosition;

            // Follow 단계에서 스무딩된 실제 카메라 회전을 사용한다.
            // 누적 상태의 목표 회전을 다시 사용하면 충돌 단계에서 위치만 선행해 회전과 궤도가 불일치한다.
            Quaternion appliedRotation = frame.Pose.CameraRotation;
            Vector3 camDir = appliedRotation * Vector3.back;
            // DistanceCeiling이 설정되면(락온 거리 피팅) Follow와 동일하게 maxDistance 상한을 끌어올린다.
            float maxDistance = Mathf.Max(settings.maxDistance, frame.DistanceCeiling);
            float desiredDistance = Mathf.Max(
                Mathf.Clamp(state.TargetDistance, settings.minDistance, maxDistance)
                + frame.Effects.distanceDelta,
                0f);

            float finalDist = context.Collision != null
                ? context.Collision.Evaluate(pivotPosition, camDir, desiredDistance)
                : desiredDistance;

            frame.Pose.CameraPosition = pivotPosition + camDir * finalDist;
        }
    }
}
