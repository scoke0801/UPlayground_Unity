using UPlayGround.Data.Event;

namespace UPlayGround.Data.Quest
{
    /// <summary> 퀘스트 수락/완료/실패 이벤트 데이터 </summary>
    public class QuestStateEventData : IEventData
    {
        public string QuestId;
        public string QuestName;
    }

    /// <summary> 퀘스트 목표 진행도 변경 이벤트 데이터 </summary>
    public class QuestObjectiveEventData : IEventData
    {
        public string QuestId;
        public string ObjectiveId;
        public int CurrentCount;
        public int RequiredCount;
    }
}
