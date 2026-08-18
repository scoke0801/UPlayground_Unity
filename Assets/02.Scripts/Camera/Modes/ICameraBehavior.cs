namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 상위 카메라 모드 인터페이스.
    /// 현재 단계에서는 모드 식별과 진입/이탈 수명주기만 사용하고,
    /// 포즈 계산 이전은 다음 단계에서 진행한다.
    /// </summary>
    public interface ICameraBehavior
    {
        CameraModeType ModeType { get; }
        int Priority { get; }
        bool AllowsPlayerLookInput { get; }
        bool AllowsZoomInput { get; }
        bool AllowsLockOnInput { get; }
        bool UseCollision { get; }
        bool RequiresPrimaryTarget { get; }

        void OnEnter(CameraContext context, CameraModeEnterParams enterParams);
        void OnExit(CameraContext context);
        void HandleInput(CameraContext context, float deltaTime);
        CameraPose EvaluatePose(CameraContext context, float deltaTime, CameraEffectState effectState);
    }
}
