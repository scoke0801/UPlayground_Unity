namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// (200) 락온 타깃 기준 회전 보정. 전환 시작 시 정렬(Align)을 트리거한다.
    /// 원본: InGameCameraMode.UpdateLockOn
    /// 해제 직후 위치 스무딩 유지 로직은 LockOnReleaseSmoothingCameraModifier(660)가 담당한다.
    /// </summary>
    public sealed class LockOnCameraModifier : ICameraModifier
    {
        public int Priority => 200;

        public void Apply(ref CameraFrame frame)
        {
            CameraContext context = frame.Context;
            if (context?.LockOn == null || context.Settings == null || frame.State == null)
                return;

            CameraState state = frame.State;
            bool skipAuto = context.IsInputLocked || context.LookAtOverride != null;

            bool needAlign = context.LockOn.UpdateTransition(ref state.CurrentYaw, ref state.CurrentPitch, skipAuto);
            if (needAlign)
            {
                context.StartCameraAlign?.Invoke();
                context.IsAligning = true;
                context.AlignTimer = context.Settings.alignDuration;
            }

            context.LockOn.UpdateRotation(ref state.CurrentYaw, ref state.CurrentPitch, skipAuto);
        }
    }
}
