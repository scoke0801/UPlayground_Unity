namespace UPlayGround.Data.EnumType
{
    /// <summary>
    /// QuestManager가 EventManager를 통해 발송하는 이벤트 타입
    /// </summary>
    public enum QuestEvent
    {
        QuestAccepted        = 0,  // 퀘스트 수락
        QuestCompleted       = 1,  // 퀘스트 완료
        QuestFailed          = 2,  // 퀘스트 실패
        QuestObjectiveUpdated = 3, // 특정 목표 진행도 변경
        QuestTracked         = 4,  // HUD 추적 퀘스트 변경
        QuestUntracked       = 5,  // HUD 추적 퀘스트 해제
    }
}
