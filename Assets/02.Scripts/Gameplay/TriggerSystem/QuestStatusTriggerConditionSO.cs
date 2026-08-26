using UnityEngine;
using UPlayGround.Data.Quest;
using UPlayGround.Manager;

namespace UPlayGround.TriggerSystem
{
    /// <summary>안정적인 문자열 퀘스트 ID로 현재 상태를 판정한다.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/트리거/조건/Quest Status")]
    public sealed class QuestStatusTriggerConditionSO : TriggerConditionSO
    {
        [Tooltip("QuestSO.questId와 같은 문자열 ID.")]
        [SerializeField] private string _questId;
        [SerializeField] private QuestStatus _expectedStatus = QuestStatus.Active;

        public override bool Evaluate(TriggerContext context)
        {
            IQuestFlowService quest = Svc.QuestFlow;
            if (quest == null || string.IsNullOrWhiteSpace(_questId))
                return false;

            return quest.GetQuestStatus(_questId) == _expectedStatus;
        }
    }
}
