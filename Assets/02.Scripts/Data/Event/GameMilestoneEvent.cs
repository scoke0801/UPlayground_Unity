namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 게임 진행 중 최초 도달 여부를 관측할 수 있는 주요 이정표 이벤트.
    /// 가이드 외에도 업적, 분석, 튜토리얼 시스템에서 같은 이벤트를 관측할 수 있다.
    /// </summary>
    public enum GameMilestoneEvent
    {
        CombatStarted = 0,
        CharacterUnlocked = 1,
        EquipmentAcquired = 2,
    }

    /// <summary>반복 세계 메인 서사의 무페이로드 오케스트레이션 이벤트.</summary>
    public enum CycleStoryEvent
    {
        FirstAnchorGateCompleted = 0,
    }
}
