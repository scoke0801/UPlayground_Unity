using UnityEngine;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// (790) 기본 구도 계산 뒤 연출 회전·피벗·거리를 포즈에만 합성한다.
    /// 지속 상태에 흔들림을 누적하면 데드존 추적이 연출을 이동 오차로 받아들인다.
    /// </summary>
    public sealed class EffectRotationInjectCameraModifier : ICameraModifier
    {
        public int Priority => 790;

        public void Apply(ref CameraFrame frame)
        {
            if (frame.State == null) return;

            frame.Pose.PivotPosition += frame.Effects.offsetDelta;
            frame.Pose.CameraRotation = Quaternion.AngleAxis(frame.Effects.yawDelta, Vector3.up)
                                        * frame.Pose.CameraRotation
                                        * Quaternion.AngleAxis(frame.Effects.pitchDelta, Vector3.right);
            float distance = Mathf.Max(0f, frame.Pose.Distance + frame.Effects.distanceDelta);
            frame.Pose.CameraPosition = frame.Pose.PivotPosition
                                        + frame.Pose.CameraRotation * Vector3.back * distance;
        }
    }
}
