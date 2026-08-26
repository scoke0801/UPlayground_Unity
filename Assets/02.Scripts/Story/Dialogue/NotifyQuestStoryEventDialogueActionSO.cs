using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.Dialogue
{
    /// <summary>대화가 정상 종료되는 지점에서 서사 이벤트를 알려 퀘스트 목표를 갱신한다.</summary>
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
