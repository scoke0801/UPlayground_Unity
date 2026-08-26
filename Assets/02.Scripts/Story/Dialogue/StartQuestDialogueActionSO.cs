using UnityEngine;
using UPlayGround.Data.Quest;
using UPlayGround.Manager;

namespace UPlayGround.Dialogue
{
    /// <summary>대화가 끝난 시점에 지정 퀘스트를 수락하고 HUD 추적 대상으로 지정한다.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/대화/액션/Start Quest", fileName = "Action_StartQuest_")]
    public sealed class StartQuestDialogueActionSO : DialogueActionSO
    {
        // QuestIdType의 값은 QuestDatabase의 목록 순서로 매겨져 퀘스트를 추가하면 전부 밀린다.
        // 이미 저작된 에셋이 조용히 다른 퀘스트를 가리키게 되므로, 흐름 그래프의 StartQuest 노드와 같이 문자열 ID를 쓴다.
        [Tooltip("수락할 퀘스트의 ID 문자열. QuestSO.questId와 같은 값을 쓴다.")]
        [SerializeField] private string _questId;

        public override void Execute()
        {
            IQuestFlowService quest = Svc.QuestFlow;
            string questId = _questId;
            if (quest == null || string.IsNullOrWhiteSpace(questId))
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
