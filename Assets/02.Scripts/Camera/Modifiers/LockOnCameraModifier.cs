namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// (200) 락온 대상의 수명·가시성을 갱신한다. 해제 시 현재 시선을 유지한다.
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
            context.LockOn.UpdateTarget(frame.DeltaTime, skipAuto);
        }
    }
}
