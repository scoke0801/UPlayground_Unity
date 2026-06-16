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

    [CreateAssetMenu(menuName = "UPlayGround/Trigger/Action/Complete Quest")]
    public sealed class CompleteQuestTriggerActionSO : TriggerActionSO
    {
        [SerializeField] private QuestIdType _questId = QuestIdType.None;

        public override bool CanExecute(TriggerContext context)
        {
            return _questId != QuestIdType.None && QuestManager.Instance != null;
        }

        public override IEnumerator Execute(TriggerContext context)
        {
            QuestManager.Instance?.CompleteQuest(_questId);
            yield break;
        }
    }

    [CreateAssetMenu(menuName = "UPlayGround/Trigger/Action/Fail Quest")]
    public sealed class FailQuestTriggerActionSO : TriggerActionSO
    {
        [SerializeField] private QuestIdType _questId = QuestIdType.None;

        public override bool CanExecute(TriggerContext context)
        {
            return _questId != QuestIdType.None && QuestManager.Instance != null;
        }

        public override IEnumerator Execute(TriggerContext context)
        {
            QuestManager.Instance?.FailQuest(_questId);
            yield break;
        }
    }

    [CreateAssetMenu(menuName = "UPlayGround/Trigger/Action/Use Quest Item")]
    public sealed class UseQuestItemTriggerActionSO : TriggerActionSO
    {
        [SerializeField] private int _itemId;
        [SerializeField, Min(1)] private int _count = 1;

        public override bool CanExecute(TriggerContext context)
        {
            return _itemId > 0 && _count > 0 && InventoryManager.Instance != null;
        }

        public override IEnumerator Execute(TriggerContext context)
        {
            InventoryManager.Instance?.UseItem(_itemId, _count);
            yield break;
        }
    }

    [CreateAssetMenu(menuName = "UPlayGround/Trigger/Action/Deliver Quest Item")]
    public sealed class DeliverQuestItemTriggerActionSO : TriggerActionSO
    {
        [SerializeField] private int _npcId;
        [SerializeField] private int _itemId;
        [SerializeField, Min(1)] private int _count = 1;

        public override bool CanExecute(TriggerContext context)
        {
            return _npcId > 0 && _itemId > 0 && _count > 0 && InventoryManager.Instance != null;
        }

        public override IEnumerator Execute(TriggerContext context)
        {
            InventoryManager.Instance?.DeliverItemToQuest(_npcId, _itemId, _count);
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
