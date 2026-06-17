namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 모드 진입/이탈 시점에 내부 상태를 초기화해야 하는 Modifier가 선택적으로 구현하는 인터페이스.
    /// CameraBehaviorBase가 OnEnter/OnExit에서 등록된 Modifier 중 이 인터페이스를 구현한 것에 전달한다.
    /// 대부분의 Modifier는 구현 불필요(프레임 간 보간 상태가 리셋될 필요 없는 경우).
    /// </summary>
    public interface ICameraModifierLifecycle
    {
        void OnEnter(CameraContext context, CameraModeEnterParams enterParams);
        void OnExit(CameraContext context);
    }
}
