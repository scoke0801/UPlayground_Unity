namespace UPlayGround.Data.Quest
{
    public enum QuestStatus
    {
        Locked    = 0,  // 선행 조건 미충족으로 수락 불가
        Available = 1,  // 수락 가능 상태
        Active    = 2,  // 수락 후 진행 중
        Completed = 3,  // 완료
        Failed    = 4,  // 실패
    }
}
