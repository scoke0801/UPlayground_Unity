using System.Collections.Generic;
using UPlayGround.Data.Item;
using UPlayGround.Data.Save;

namespace UPlayGround.Manager
{
    public partial class InventoryManager
    {
        /// <summary>장비 옵션은 드롭 생성 때 한 번 확정하고 개별 기록으로 분리한다.</summary>
        public List<ItemSaveEntry> ResolveDropItems(IReadOnlyList<ItemInstance> items)
        {
            var resolved = new List<ItemSaveEntry>();
            if (items == null) return resolved;
            for (int i = 0; i < items.Count; i++)
            {
                ItemInstance item = items[i];
                if (item?.data == null || item.count <= 0) continue;
                int records = item.data is EquipmentSO ? item.count : 1;
                for (int index = 0; index < records; index++)
                {
                    var rolls = item.growthAttributeRolls;
                    if (item.data is EquipmentSO equipment && (rolls == null || rolls.Count == 0))
                        rolls = RollGrowthAttributes(equipment);
                    resolved.Add(new ItemSaveEntry
                    {
                        itemId = item.data.itemId, count = item.data is EquipmentSO ? 1 : item.count,
                        enhancementLevel = item.enhancementLevel,
                        growthAttributeRolls = rolls != null ? new(rolls) : new()
                    });
                }
            }
            return resolved;
        }

        /// <summary>확정된 옵션을 보존해 삽입한다. 획득 이벤트는 드롭 확정 뒤 발행한다.</summary>
        public bool TryStoreResolvedItem(ItemSaveEntry item)
        {
            if (item == null || !CanAddItem(item.itemId, item.count)) return false;
            ItemSO data = Svc.Item?.GetItemData(item.itemId);
            if (data == null || data is EquipmentSO && item.count != 1) return false;
            if (data is EquipmentSO)
            {
                AddEquipmentInstance(data, false, 0, item.enhancementLevel,
                    item.growthAttributeRolls ?? new());
            }
            else if (_itemPair.TryGetValue(item.itemId, out var existing)) existing.count += item.count;
            else PutItem(item.itemId, new ItemInstance
            {
                data = data, count = item.count, inventorySlotKey = item.itemId,
                enhancementLevel = item.enhancementLevel,
                growthAttributeRolls = item.growthAttributeRolls != null ? new(item.growthAttributeRolls) : new()
            });
            return true;
        }

        /// <summary>실제 획득 확정 뒤 기존 퀘스트·제작·인벤토리 알림을 한 번 보낸다.</summary>
        public void NotifyResolvedItemAcquired(ItemSaveEntry item)
        {
            NotifyItemCollected(item.itemId, item.count);
            RaiseInventoryChanged();
        }
    }
}
