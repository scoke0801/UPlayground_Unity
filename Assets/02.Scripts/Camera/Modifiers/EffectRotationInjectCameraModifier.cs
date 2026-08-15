namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// (600) 활성 이펙트의 회전 델타(쉐이크 등)를 state yaw/pitch에 주입한다.
    /// 위치 계산(Follow) *이전*에 적용되어야 회전 쉐이크가 실제 카메라 위치에 반영된다.
    /// 자유 궤도의 기본 피치는 입력 단계에서 극점 직전으로 제한하며, 이펙트 델타는 그 위에 가산된다.
    /// 원본: InGameCameraMode.EvaluatePose 라인 100-101
    /// </summary>
    public sealed class EffectRotationInjectCameraModifier : ICameraModifier
    {
        public int Priority => 600;

        public void Apply(ref CameraFrame frame)
        {
            if (frame.State == null) return;

            frame.State.CurrentYaw += frame.Effects.yawDelta;
            frame.State.CurrentPitch += frame.Effects.pitchDelta;
        }
    }
}
