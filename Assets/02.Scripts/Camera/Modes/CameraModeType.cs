namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// CameraManager가 관리하는 상위 카메라 모드.
    /// 스킬/킬캠은 InGame 내부 시퀀스로 처리하되,
    /// 궁극기처럼 카메라 포즈를 완전히 점유하는 연출은 별도 모드로 처리한다.
    /// </summary>
    public enum CameraModeType
    {
        InGame,
        Free,
        Dialogue,
        Cinematic,
        CameraSnapshotSequence
    }
}
