using System.Collections;
using UnityEngine;
using UPlayGround.Data.Quest;
using UPlayGround.Manager;

namespace UPlayGround.TriggerSystem
{
    [CreateAssetMenu(menuName = "UPlayGround/Trigger/Action/Accept Quest")]
    public sealed class AcceptQuestTriggerActionSO : TriggerActionSO
    {
        [SerializeField] private QuestIdType _questId = QuestIdType.None;
        [SerializeField] private bool _consumeWhenQuestDatabasePending = false;

        public override bool CanExecute(TriggerContext context)
        {
            return _questId != QuestIdType.None && QuestManager.Instance != null;
        }

        public override bool ConsumesTrigger(TriggerContext context)
        {
            return _consumeWhenQuestDatabasePending || QuestManager.Instance == null || QuestManager.Instance.IsDBLoaded;
        }

        public override IEnumerator Execute(TriggerContext context)
        {
            QuestManager.Instance?.AcceptQuest(_questId);
            yield break;
        }
    }

    [CreateAssetMenu(menuName = "UPlayGround/Trigger/Action/Notify Location")]
    public sealed class NotifyLocationTriggerActionSO : TriggerActionSO
    {
        [SerializeField] private string _locationId;
        [SerializeField] private bool _consumeWhenQuestDatabasePending = false;

        public override bool CanExecute(TriggerContext context)
        {
            return !string.IsNullOrEmpty(_locationId) && QuestManager.Instance != null;
        }

        public override bool ConsumesTrigger(TriggerContext context)
        {
            return _consumeWhenQuestDatabasePending || QuestManager.Instance == null || QuestManager.Instance.IsDBLoaded;
        }

        public override IEnumerator Execute(TriggerContext context)
        {
            QuestManager.Instance?.NotifyLocationReached(_locationId);
            yield break;
        }
    }
}
