using UnityEngine;
using UPlayGround.Data.Quest;
using UPlayGround.Manager;

namespace UPlayGround.Dialogue
{
    [CreateAssetMenu(menuName = "UPlayGround/대화/액션/Accept Quest", fileName = "Action_Quest_Accept_")]
    public sealed class AcceptQuestDialogueActionSO : DialogueActionSO
    {
        [SerializeField] private QuestIdType _questId = QuestIdType.None;

        public override void Execute()
        {
            QuestManager.Instance?.AcceptQuest(_questId);
        }
    }

    [CreateAssetMenu(menuName = "UPlayGround/대화/액션/Complete Quest", fileName = "Action_Quest_Complete_")]
    public sealed class CompleteQuestDialogueActionSO : DialogueActionSO
    {
        [SerializeField] private QuestIdType _questId = QuestIdType.None;

        public override void Execute()
        {
            QuestManager.Instance?.CompleteQuest(_questId);
        }
    }

    [CreateAssetMenu(menuName = "UPlayGround/대화/액션/Fail Quest", fileName = "Action_Quest_Fail_")]
    public sealed class FailQuestDialogueActionSO : DialogueActionSO
    {
        [SerializeField] private QuestIdType _questId = QuestIdType.None;

        public override void Execute()
        {
            QuestManager.Instance?.FailQuest(_questId);
        }
    }

    [CreateAssetMenu(menuName = "UPlayGround/대화/액션/Use Item", fileName = "Action_Quest_UseItem_")]
    public sealed class UseQuestItemDialogueActionSO : DialogueActionSO
    {
        [SerializeField] private int _itemId;
        [SerializeField, Min(1)] private int _count = 1;

        public override void Execute()
        {
            InventoryManager.Instance?.UseItem(_itemId, _count);
        }
    }

    [CreateAssetMenu(menuName = "UPlayGround/대화/액션/Deliver Item", fileName = "Action_Quest_DeliverItem_")]
    public sealed class DeliverQuestItemDialogueActionSO : DialogueActionSO
    {
        [SerializeField] private int _npcId;
        [SerializeField] private int _itemId;
        [SerializeField, Min(1)] private int _count = 1;

        public override void Execute()
        {
            InventoryManager.Instance?.DeliverItemToQuest(_npcId, _itemId, _count);
        }
    }

    [CreateAssetMenu(menuName = "UPlayGround/대화/액션/Notify Location", fileName = "Action_Quest_NotifyLocation_")]
    public sealed class NotifyQuestLocationDialogueActionSO : DialogueActionSO
    {
        [SerializeField] private string _locationId;

        public override void Execute()
        {
            QuestManager.Instance?.NotifyLocationReached(_locationId);
        }
    }

    [CreateAssetMenu(menuName = "UPlayGround/대화/액션/Notify Story Event", fileName = "Action_Quest_NotifyStoryEvent_")]
    public sealed class NotifyQuestStoryEventDialogueActionSO : DialogueActionSO
    {
        [SerializeField] private string _eventId;

        public override void Execute()
        {
            QuestManager.Instance?.NotifyStoryEvent(_eventId);
        }
    }
}
