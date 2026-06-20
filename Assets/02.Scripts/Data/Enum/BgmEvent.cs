namespace UPlayGround.Data.EnumType
{
    /// <summary>
    /// 이벤트 기반 BGM 제어 신호. 보스전 진입/종료, 스토리 연출 등에서
    /// EventManager.Send(BgmEvent.X, new BgmRequestData{...})로 발화한다.
    /// SoundManager가 Global 스코프로 구독해 처리한다.
    /// </summary>
    public enum BgmEvent
    {
        None = 0,

        /// <summary>평시(베이스) BGM을 교체한다. override 스택과 무관하게 현재 곡을 바꾼다.</summary>
        Change,

        /// <summary>현재 BGM을 임시로 덮어쓴다(보스전 진입 등). 직전 곡을 기억해 둔다.</summary>
        Override,

        /// <summary>Override를 해제하고 직전 BGM으로 복귀한다(보스전 종료 등).</summary>
        Restore,

        /// <summary>BGM을 정지한다.</summary>
        Stop,
    }
}
