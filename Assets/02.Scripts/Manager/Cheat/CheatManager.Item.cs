#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UPlayGround.Data.Item;

namespace UPlayGround.Manager
{
    /// <summary>CheatManager — 아이템 치트(지급/삭제). 개발 빌드 전용.</summary>
    public partial class CheatManager
    {
        /// <summary> 최대치 지급에 사용하는 기본 수량. </summary>
        public const int MaxGrantCount = 99;

        /// <summary>
        /// 지정 아이템을 인벤토리에 지급한다.
        /// 장비 아이템은 InventoryManager 정책에 따라 이미 보유 중이면 추가되지 않는다.
        /// </summary>
        public bool GrantItem(int itemId, int count)
        {
            if (count <= 0)
            {
                Debug.LogWarning($"[CheatManager] 아이템 지급 실패: 수량이 올바르지 않습니다. ({count})");
                return false;
            }

            var itemManager = ItemManager.Instance;
            var inventoryManager = InventoryManager.Instance;
            if (itemManager == null || inventoryManager == null)
            {
                Debug.LogWarning("[CheatManager] 아이템 지급 실패: ItemManager 또는 InventoryManager가 없습니다.");
                return false;
            }

            if (!itemManager.IsItemDBLoaded)
            {
                Debug.LogWarning("[CheatManager] 아이템 지급 실패: ItemDatabase가 아직 로드되지 않았습니다.");
                return false;
            }

            var itemData = itemManager.GetItemData(itemId);
            if (itemData == null)
            {
                Debug.LogWarning($"[CheatManager] 아이템 지급 실패: ItemID {itemId}를 찾을 수 없습니다.");
                return false;
            }

            int beforeCount = inventoryManager.GetItemCount(itemId);
            inventoryManager.AddItem(itemId, count);
            int afterCount = inventoryManager.GetItemCount(itemId);

            Debug.Log(
                $"[CheatManager] 아이템 지급: {itemData.itemName}({itemId}) x{count} " +
                $"보유 {beforeCount} → {afterCount}");
            Log(CheatCategory.Item, $"생성: {itemData.itemName} x{count}");
            return afterCount > beforeCount || beforeCount > 0;
        }

        public bool GrantItem(ItemIdType itemId, int count) => GrantItem((int)itemId, count);

        /// <summary> 아이템을 인벤토리에서 지정 수량만큼 제거한다. count가 보유량 이상이면 전부 제거. </summary>
        public bool DeleteItem(int itemId, int count)
        {
            var itemManager = ItemManager.Instance;
            var inventoryManager = InventoryManager.Instance;
            if (itemManager == null || inventoryManager == null || !itemManager.IsItemDBLoaded)
                return false;

            int before = inventoryManager.GetItemCount(itemId);
            if (before <= 0)
                return false;

            int removeCount = count <= 0 ? before : Mathf.Min(count, before);
            bool removed = inventoryManager.RemoveItem(itemId, removeCount);
            if (!removed)
                return false;

            var itemData = itemManager.GetItemData(itemId);
            string name = itemData != null ? itemData.itemName : itemId.ToString();
            Log(CheatCategory.Item, $"삭제: {name} x{removeCount}");
            return true;
        }

        /// <summary> 아이템을 최대치(<see cref="MaxGrantCount"/>)까지 지급한다. </summary>
        public bool GrantMax(int itemId) => GrantItem(itemId, MaxGrantCount);
    }
}
#endif
