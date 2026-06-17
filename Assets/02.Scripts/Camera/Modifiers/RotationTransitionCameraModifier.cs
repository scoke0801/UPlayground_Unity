namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// (100) 외부에서 요청된 카메라 회전 전환(CameraRotationTransition)을 진행한다.
    /// 전환 완료 + UnlockOnComplete 시 입력 잠금을 해제한다.
    /// 원본: InGameCameraMode.UpdateRotationTransition
    /// </summary>
    public sealed class RotationTransitionCameraModifier : ICameraModifier
    {
        public int Priority => 100;

        public void Apply(ref CameraFrame frame)
        {
            CameraContext context = frame.Context;
            if (context?.RotationTransition == null || context.Settings == null || frame.State == null)
                return;

            context.RotationTransition.Update(
                frame.DeltaTime,
                context.Settings.minVerticalAngle,
                context.Settings.maxVerticalAngle,
                ref frame.State.CurrentYaw,
                ref frame.State.CurrentPitch);

            if (!context.RotationTransition.IsActive && context.RotationTransition.UnlockOnComplete)
            {
                context.IsInputLocked = false;
                context.RotationTransition.Cancel();
            }
        }
    }
}
