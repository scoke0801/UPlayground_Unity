namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// (200) 락온 포커스를 보간한다. 대상 수명은 모드와 무관하게 CameraManager가 갱신한다.
    /// </summary>
    public sealed class LockOnCameraModifier : ICameraModifier
    {
        public int Priority => 200;

        public void Apply(ref CameraFrame frame)
        {
            CameraContext context = frame.Context;
            if (context?.LockOn == null || context.Settings == null || frame.State == null)
                return;

            bool skipAuto = context.IsInputLocked || context.LookAtOverride != null
                            || (context.RotationTransition?.IsActive ?? false);
            context.LockOn.UpdateTrackingTarget(frame.DeltaTime, skipAuto);
        }
    }
}
