namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// CameraManager가 관리하는 상위 카메라 모드.
    /// 스킬/킬캠은 InGame 내부 시퀀스로 처리하므로 모드에 포함하지 않는다.
    /// </summary>
    public enum CameraModeType
    {
        InGame,
        Free,
        Dialogue,
        Cinematic
    }
}
