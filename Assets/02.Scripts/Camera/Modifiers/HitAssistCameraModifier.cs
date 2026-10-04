using UnityEngine;

namespace UPlayGround.CameraSystem
{
    /// <summary>피벗 추적 뒤에 타격 보정을 적용하고 충돌·가산 연출에 결과를 전달한다.</summary>
    public sealed class HitAssistCameraModifier : ICameraModifier, ICameraModifierLifecycle
    {
        public int Priority => 760;

        /// <summary>플레이 카메라 재진입 시 이전 명중을 재생하지 않는다.</summary>
        public void OnEnter(CameraContext context, CameraModeEnterParams enterParams) => context?.HitAssist.Reset();

        /// <summary>대화·연출로 전환할 때 대기 중인 자동 보정을 제거한다.</summary>
        public void OnExit(CameraContext context) => context?.HitAssist.Reset();

        /// <summary>충돌로 짧아진 암 길이와 연출 이전 FOV로 보정 여부를 판단한다.</summary>
        public void Apply(ref CameraFrame frame)
        {
            CameraContext context = frame.Context;
            if (context?.Settings == null || frame.State == null)
                return;
            CameraPose pose = frame.Pose;
            float desiredDistance = pose.Distance;
            float desiredFov = pose.FieldOfView;
            pose.Distance = Mathf.Min(desiredDistance, context.Collision?.CurrentDistance ?? desiredDistance);
            pose.CameraPosition = pose.PivotPosition + pose.CameraRotation * Vector3.back * pose.Distance;
            pose.FieldOfView = context.DistanceController?.BaseFOV ?? desiredFov;
            context.HitAssist.Apply(ref pose, context, frame.DeltaTime);
            frame.State.CurrentYaw = pose.Yaw;
            pose.Distance = desiredDistance;
            pose.FieldOfView = desiredFov;
            pose.CameraPosition = pose.PivotPosition + pose.CameraRotation * Vector3.back * desiredDistance;
            frame.Pose = pose;
        }
    }
}
