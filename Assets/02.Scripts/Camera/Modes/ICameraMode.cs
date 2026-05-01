namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 상위 카메라 모드 인터페이스.
    /// 현재 단계에서는 모드 식별과 진입/이탈 수명주기만 사용하고,
    /// 포즈 계산 이전은 다음 단계에서 진행한다.
    /// </summary>
    public interface ICameraMode
    {
        CameraModeType ModeType { get; }
        int Priority { get; }
        bool AllowsPlayerLookInput { get; }
        bool AllowsZoomInput { get; }
        bool AllowsLockOnInput { get; }
        bool UseCollision { get; }

        void OnEnter(CameraRuntimeContext context, CameraModeEnterParams enterParams);
        void OnExit(CameraRuntimeContext context);
        void HandleInput(CameraRuntimeContext context, float deltaTime);
        CameraRigPose EvaluatePose(CameraRuntimeContext context, float deltaTime, CameraEffectState effectState);
    }
}
