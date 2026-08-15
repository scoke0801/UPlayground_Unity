namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// (500) CameraDistanceController에 거리/FOV base 갱신을 위임한다.
    /// 락온/전투 상태에 따른 목표 거리를 state.TargetDistance에 반영한다.
    /// 원본: InGameCameraMode.UpdateOffsetAndDistance(거리부)
    /// </summary>
    public sealed class DistanceFovCameraModifier : ICameraModifier
    {
        public int Priority => 500;

        public void Apply(ref CameraFrame frame)
        {
            CameraContext context = frame.Context;
            if (context == null || frame.State == null) return;
            if (context.IsInputLocked || context.DistanceController == null) return;

            CameraState state = frame.State;
            bool isLockOn = context.LockOn?.IsActive ?? false;
            bool isCombat = context.CombatStateProvider?.Invoke() ?? false;

            context.DistanceController.UpdateFOV(isLockOn, isCombat, context.Motion);
            float dist = context.DistanceController.EvaluateDistance(
                isLockOn,
                isCombat,
                state.TargetDistance,
                context.Motion);
            if (dist >= 0f)
                state.TargetDistance = dist;
        }
    }
}
