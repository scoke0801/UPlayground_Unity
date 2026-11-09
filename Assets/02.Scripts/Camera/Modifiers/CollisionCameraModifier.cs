using UnityEngine;

namespace UPlayGround.CameraSystem
{
    /// <summary>시선을 유지하고 현재 프레임의 안전한 카메라 암 길이만 적용한다.</summary>
    public sealed class CollisionCameraModifier : ICameraModifier
    {
        public int Priority => 800;

        /// <summary>입력·락온·연출 회전을 보존하며 카메라 위치를 장애물 앞에 배치한다.</summary>
        public void Apply(ref CameraFrame frame)
        {
            CameraContext context = frame.Context;
            if (context?.Settings == null || frame.State == null) return;
            float ceiling = Mathf.Max(context.Settings.maxDistance, frame.DistanceCeiling);
            float desired = Mathf.Max(0f, Mathf.Clamp(frame.Pose.Distance,
                context.Settings.minDistance, ceiling) + frame.Effects.distanceDelta);
            Vector3 direction = frame.Pose.CameraRotation * Vector3.back;
            if (context.OcclusionFader != null)
                desired = context.OcclusionFader.LimitInterior(frame.Pose.PivotPosition, direction, desired);
            float distance = context.Collision != null
                ? context.Collision.Evaluate(frame.Pose.PivotPosition, direction, desired, frame.DeltaTime)
                : desired;
            frame.Pose.CameraPosition = frame.Pose.PivotPosition + direction * distance;
            ProbeOcclusion(context, frame.Pose);
        }

        private static void ProbeOcclusion(CameraContext context, CameraPose pose)
        {
            CameraOcclusionFader fader = context.OcclusionFader;
            if (fader == null) return;

            fader.Probe(pose.CameraPosition, pose.PivotPosition);
            CameraLockOn lockOn = context.LockOn;
            if (lockOn != null && lockOn.LiveTarget != null)
                fader.Probe(pose.CameraPosition, lockOn.FocusPosition);
        }
    }
}
