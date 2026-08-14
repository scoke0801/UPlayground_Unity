using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.Dialogue
{
    [CreateAssetMenu(menuName = "UPlayGround/대화/조건/Inventory Item", fileName = "Cond_InventoryItem_")]
    public sealed class InventoryItemConditionSO : ConditionSO
    {
        [SerializeField] private int _itemId;
        [SerializeField, Min(1)] private int _requiredCount = 1;
        [SerializeField] private bool _expectedHasEnough = true;

        public override bool Evaluate()
        {
            bool hasEnough = InventoryManager.Instance != null
                             && InventoryManager.Instance.GetItemCount(_itemId) >= Mathf.Max(1, _requiredCount);
            return hasEnough == _expectedHasEnough;
        }
    }
}
