using UnityEngine;
using UPlayGround.Data.Quest;
using UPlayGround.Manager;

namespace UPlayGround.TriggerSystem
{
    [CreateAssetMenu(menuName = "UPlayGround/Trigger/Condition/Quest Status")]
    public sealed class QuestStatusTriggerConditionSO : TriggerConditionSO
    {
        [SerializeField] private QuestIdType _questId = QuestIdType.None;
        [SerializeField] private QuestStatus _expectedStatus = QuestStatus.Active;

        public override bool Evaluate(TriggerContext context)
        {
            if (_questId == QuestIdType.None || QuestManager.Instance == null)
                return false;

            return QuestManager.Instance.GetQuestStatus(_questId) == _expectedStatus;
        }
    }
}
