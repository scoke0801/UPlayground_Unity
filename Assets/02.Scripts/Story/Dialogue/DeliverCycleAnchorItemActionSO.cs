using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.Dialogue
{
    [CreateAssetMenu(menuName = "UPlayGround/대화/액션/Deliver Cycle Anchor Item", fileName = "Action_DeliverCycleAnchorItem_")]
    public sealed class DeliverCycleAnchorItemActionSO : DialogueActionSO
    {
        [SerializeField] private int _npcId;
        [SerializeField] private int _itemId;

        public override void Execute()
        {
            if (InventoryManager.Instance?.DeliverItemToQuest(_npcId, _itemId, 1) != true)
                Debug.LogWarning($"[DeliverCycleAnchorItemAction] 아이템 {_itemId} 전달에 실패했습니다.");
        }
    }
}
