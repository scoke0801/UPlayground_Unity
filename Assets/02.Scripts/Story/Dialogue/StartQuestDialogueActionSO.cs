using UnityEngine;
using UPlayGround.Data.Quest;
using UPlayGround.Manager;

namespace UPlayGround.Dialogue
{
    /// <summary>대화가 끝난 시점에 지정 퀘스트를 수락하고 HUD 추적 대상으로 지정한다.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/대화/액션/Start Quest", fileName = "Action_StartQuest_")]
    public sealed class StartQuestDialogueActionSO : DialogueActionSO
    {
        [SerializeField] private QuestIdType _questId = QuestIdType.None;

        public override void Execute()
        {
            IQuestFlowService quest = Svc.QuestFlow;
            string questId = _questId.ToQuestId();
            if (quest == null || string.IsNullOrEmpty(questId))
                return;

            QuestStatus status = quest.GetQuestStatus(questId);
            if (status is not (QuestStatus.Active or QuestStatus.Completed))
            {
                quest.AcceptQuest(questId);
                status = quest.GetQuestStatus(questId);
            }

            if (status == QuestStatus.Active)
                quest.TrackQuest(questId);
        }
    }
}
