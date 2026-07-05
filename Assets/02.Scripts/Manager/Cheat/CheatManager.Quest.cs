#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace UPlayGround.Manager
{
    /// <summary>CheatManager — 퀘스트 치트(수락/완료/실패/포기/추적). 개발 빌드 전용.</summary>
    public partial class CheatManager
    {
        public bool AcceptQuest(string questId, string displayName = null)
        {
            bool ok = QuestManager.Instance != null && QuestManager.Instance.AcceptQuest(questId);
            if (ok) Log(CheatCategory.Quest, $"수락: {displayName ?? questId}");
            return ok;
        }

        public bool CompleteQuest(string questId, string displayName = null)
        {
            bool ok = QuestManager.Instance != null && QuestManager.Instance.CompleteQuest(questId);
            if (ok) Log(CheatCategory.Quest, $"완료: {displayName ?? questId}");
            return ok;
        }

        public bool FailQuest(string questId, string displayName = null)
        {
            bool ok = QuestManager.Instance != null && QuestManager.Instance.FailQuest(questId);
            if (ok) Log(CheatCategory.Quest, $"실패: {displayName ?? questId}");
            return ok;
        }

        public bool AbandonQuest(string questId, string displayName = null)
        {
            bool ok = QuestManager.Instance != null && QuestManager.Instance.AbandonQuest(questId);
            if (ok) Log(CheatCategory.Quest, $"포기: {displayName ?? questId}");
            return ok;
        }

        public bool TrackQuest(string questId, string displayName = null)
        {
            bool ok = QuestManager.Instance != null && QuestManager.Instance.TrackQuest(questId);
            if (ok) Log(CheatCategory.Quest, $"추적: {displayName ?? questId}");
            return ok;
        }
    }
}
#endif
