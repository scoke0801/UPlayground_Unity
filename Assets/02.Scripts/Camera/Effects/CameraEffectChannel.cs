namespace UPlayGround
{
    /// <summary>
    /// 카메라 이펙트가 영향을 주는 채널 (비트 플래그)
    /// 충돌 감지 및 우선순위 처리에 사용
    /// </summary>
    [System.Flags]
    public enum CameraEffectChannel
    {
        None       = 0,
        Yaw        = 1 << 0,
        Pitch      = 1 << 1,
        Distance   = 1 << 2,
        Offset     = 1 << 3,
        FOV        = 1 << 4,
        Position   = 1 << 5,
        TimeScale  = 1 << 6,
        SmoothDamp = 1 << 7,
    }
}
