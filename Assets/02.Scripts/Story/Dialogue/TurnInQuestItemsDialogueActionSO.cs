using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.Dialogue
{
    /// <summary>
    /// 요구 아이템을 전부 가지고 있을 때만 한 번에 회수하고 진행을 기록한다.
    ///
    /// 회수와 기록을 서로 다른 액션 두 개로 저작하면, 수량이 모자란 플레이어에게서
    /// 일부만 회수된 채 진행만 기록되거나 그 반대가 되어 퀘스트가 완료 불가 상태로 남는다.
    /// 그래서 검사·회수·기록을 한 액션이 소유한다.
    /// </summary>
    [CreateAssetMenu(menuName = "UPlayGround/대화/액션/Turn In Items", fileName = "Action_Quest_TurnInItems_")]
    public sealed class TurnInQuestItemsDialogueActionSO : DialogueActionSO
    {
        [System.Serializable]
        public struct RequiredItem
        {
            public int itemId;
            [Min(1)] public int count;
        }

        [Tooltip("전부 가지고 있어야 회수가 진행된다. 하나라도 모자라면 아무것도 회수하지 않는다.")]
        [SerializeField] private RequiredItem[] _requiredItems;

        [Tooltip("회수에 성공했을 때만 알릴 진행 기록 ID. 비우면 회수만 한다.")]
        [SerializeField] private string _storyEventId;

        public override void Execute()
        {
            InventoryManager inventory = InventoryManager.Instance;
            if (inventory == null || _requiredItems == null || _requiredItems.Length == 0)
                return;

            if (!HasAllRequiredItems(inventory))
                return;

            if (!TryConsumeAll(inventory))
                return;

            if (!string.IsNullOrWhiteSpace(_storyEventId))
                QuestManager.Instance?.NotifyStoryEvent(_storyEventId);
        }

        private bool HasAllRequiredItems(InventoryManager inventory)
        {
            for (int i = 0; i < _requiredItems.Length; i++)
            {
                RequiredItem required = _requiredItems[i];
                if (inventory.GetItemCount(required.itemId) < Mathf.Max(1, required.count))
                    return false;
            }

            return true;
        }

        /// <summary> 도중에 실패하면 이미 회수한 만큼을 되돌린다. 아이템이 사라진 채 진행만 막히는 상태를 만들지 않는다. </summary>
        private bool TryConsumeAll(InventoryManager inventory)
        {
            int consumedCount = 0;
            for (int i = 0; i < _requiredItems.Length; i++)
            {
                RequiredItem required = _requiredItems[i];
                if (inventory.RemoveItem(required.itemId, Mathf.Max(1, required.count)))
                {
                    consumedCount++;
                    continue;
                }

                Debug.LogError(
                    $"[{nameof(TurnInQuestItemsDialogueActionSO)}] '{name}' 회수 중 {required.itemId} 차감에 실패해 되돌립니다.",
                    this);

                for (int rollback = 0; rollback < consumedCount; rollback++)
                {
                    RequiredItem restored = _requiredItems[rollback];
                    inventory.AddItem(restored.itemId, Mathf.Max(1, restored.count));
                }

                return false;
            }

            return true;
        }
    }
}
