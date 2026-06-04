using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UPlayGround.Data.Path;
using UPlayGround.Data.Save;
using UPlayGround.Data.Item;

namespace UPlayGround.Manager
{
    public class InventoryManager : BaseManager<InventoryManager>, IManager, ISaveable
    {
        private Dictionary<int, ItemInstance> _itemPair = new Dictionary<int, ItemInstance>();

        public Dictionary<int, ItemInstance> ItemDict => _itemPair;

        // [TODO] Config 데이터로 별도 분리 필요
        public float MaxWeight => 3000.0f;

        /// <summary> 보유 골드 </summary>
        public int Gold { get; set; } = 0;

        // ItemDatabase 로드 완료 전에 LoadGame()이 호출될 경우 보관
        private InventorySaveData _pendingLoad;

        public void Init()
        {
            SaveManager.Instance.RegisterSaveable(this);
        }

        public void AfterInit()
        {

        }

        public void Dispose()
        {
        }

        public void OnUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }

        public void OnLateUpdate()
        {
        }

        public void OnSceneChanged(string sceneType) { }

        public void          AddItem(ItemIdType itemId, int count)         => AddItem((int)itemId, count);
        public bool          RemoveItem(ItemIdType itemId, int count)      => RemoveItem((int)itemId, count);
        public void          RemoveItem(ItemIdType itemId)                 => RemoveItem((int)itemId);
        public int           GetItemCount(ItemIdType itemId)               => GetItemCount((int)itemId);
        public bool          HasItem(ItemIdType itemId)                    => HasItem((int)itemId);
        public ItemInstance  GetItem(ItemIdType itemId)                    => GetItem((int)itemId);
        public float         GetItemWeight(ItemIdType itemId)              => GetItemWeight((int)itemId);

        public void AddItem(int itemId, ItemInstance itemInstance)
        {
            if (itemInstance == null || itemInstance.count <= 0)
            {
                return;
            }

            if (_itemPair.ContainsKey(itemId) == false)
            {
                _itemPair.TryAdd(itemId, itemInstance);

                // TODO...  인벤토리 슬롯 지정 필요
            }
            else
            {
                _itemPair[itemId].count += itemInstance.count;
            }

            NotifyQuestItemCollected(itemId, itemInstance.count);
        }

        public void RemoveItem(int itemId)
        {
            _itemPair.Remove(itemId);
        }

        /// <summary>
        /// 아이템을 count만큼 차감한다.
        /// count 이후 수량이 0 이하가 되면 인벤토리에서 제거한다.
        /// 재고 부족 시 false 반환 (차감 없음).
        /// </summary>
        public bool RemoveItem(int itemId, int count)
        {
            if (!_itemPair.TryGetValue(itemId, out var item))
                return false;

            if (item.count < count)
                return false;

            item.count -= count;

            if (item.count <= 0)
                _itemPair.Remove(itemId);

            return true;
        }

        /// <summary>
        /// 아이템을 count만큼 추가한다.
        /// ItemManager에서 ItemSO를 조회하므로 ItemDatabase 로드 이후에 호출해야 한다.
        /// </summary>
        public void AddItem(int itemId, int count)
        {
            if (count <= 0) return;
            AddItemInternal(itemId, count, true);
        }

        private void AddItemInternal(int itemId, int count, bool notifyQuest)
        {
            if (count <= 0) return;

            if (_itemPair.TryGetValue(itemId, out var existing))
            {
                existing.count += count;
                if (notifyQuest)
                {
                    NotifyQuestItemCollected(itemId, count);
                }
                return;
            }

            var itemData = ItemManager.Instance.GetItemData(itemId);
            if (itemData == null)
            {
                Debug.LogWarning($"[InventoryManager] AddItem 실패 — ItemID {itemId}를 ItemDatabase에서 찾을 수 없습니다.");
                return;
            }

            _itemPair[itemId] = new ItemInstance { data = itemData, count = count };

            if (notifyQuest)
            {
                NotifyQuestItemCollected(itemId, count);
            }
        }

        // QuestManager 미초기화 시점(씬 전환/종료 등)에도 안전하도록 가드 후 알림
        private void NotifyQuestItemCollected(int itemId, int count)
        {
            if (QuestManager.Instance == null)
            {
                return;
            }

            QuestManager.Instance.NotifyItemCollected(itemId, count);
        }

        public int GetItemCount(int itemId)
        {
            return _itemPair.TryGetValue(itemId, out var item) ? item.count : 0;
        }

        public bool HasItem(int itemId)
        {
            return _itemPair.ContainsKey(itemId);
        }

        public ItemInstance GetItem(int itemId)
        {
            _itemPair.TryGetValue(itemId, out ItemInstance item);
            return item;
        }

        public float GetItemWeight(int itemId)
        {
            if (HasItem(itemId))
            {
                ItemInstance item = _itemPair[itemId];
                return item.data.weight * item.count;
            }

            return -1.0f;
        }

        public float GetTotalWeight()
        {
            float weight = 0;
            foreach (ItemInstance item in _itemPair.Values)
            {
                weight += item.data.weight * item.count;
            }

            return weight;
        }

        public void MakeTestItems()
        {
        }

        // ──────────────────────────────────────────────────────────
        #region ISaveable

        public void ExportSaveData(GameSaveData saveData)
        {
            saveData.inventory.gold = Gold;
            saveData.inventory.items.Clear();
            foreach (var kv in _itemPair)
            {
                saveData.inventory.items.Add(new ItemSaveEntry
                {
                    itemId = kv.Key,
                    count = kv.Value.count,
                    slotKey = kv.Value.inventorySlotKey
                });
            }
        }

        public void ImportSaveData(GameSaveData saveData)
        {
            Gold = saveData.inventory.gold;
            _pendingLoad = saveData.inventory;

            // ItemDatabase가 이미 로드된 경우 즉시 복원
            if (ItemManager.Instance.IsItemDBLoaded)
                ApplyPendingLoad();
        }

        /// <summary>
        /// ItemManager가 DB 로드 완료 후 호출한다.
        /// pending 세이브 데이터가 있으면 복원하고, 없으면 테스트 아이템을 채운다.
        /// </summary>
        public void OnItemDatabaseReady()
        {
            if (_pendingLoad != null)
                ApplyPendingLoad();
            else
                MakeTestItems();
        }

        private void ApplyPendingLoad()
        {
            _itemPair.Clear();
            foreach (var entry in _pendingLoad.items ?? new System.Collections.Generic.List<ItemSaveEntry>())
            {
                AddItemInternal(entry.itemId, entry.count, false);
                if (_itemPair.TryGetValue(entry.itemId, out var instance))
                    instance.inventorySlotKey = entry.slotKey;
            }
            _pendingLoad = null;
        }

        #endregion
    }
}
