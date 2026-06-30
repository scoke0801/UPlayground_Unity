using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Data.Path;
using UPlayGround.Data.Save;
using UPlayGround.Data.Item;
using UPlayGround.Data.Sound;

namespace UPlayGround.Manager
{
    public enum InventoryActionResult
    {
        Success = 0,
        InvalidItem,
        NotEnoughCount,
        NotUsable,
        NotEquippable,
        EquippedItem,
        NoEffect,
        Failed,
    }

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
        public bool          UseItem(ItemIdType itemId, int count = 1)     => UseItem((int)itemId, count);
        public bool          DeliverItemToQuest(int npcId, ItemIdType itemId, int count = 1) => DeliverItemToQuest(npcId, (int)itemId, count);
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

            NotifyItemCollected(itemId, itemInstance.count);
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
        /// 아이템 사용 처리용 공통 API. 소비 성공 시 ItemUse 퀘스트 목표를 갱신한다.
        /// </summary>
        public bool UseItem(int itemId, int count = 1)
        {
            if (count <= 0)
            {
                return false;
            }

            if (!RemoveItem(itemId, count))
            {
                return false;
            }

            QuestManager.Instance?.NotifyItemUsed(itemId, count);
            return true;
        }

        public InventoryActionResult TryUseItem(int itemId, int count = 1)
        {
            if (count <= 0)
            {
                return InventoryActionResult.Failed;
            }

            if (!_itemPair.TryGetValue(itemId, out var item))
            {
                return InventoryActionResult.InvalidItem;
            }

            if (item.count < count)
            {
                return InventoryActionResult.NotEnoughCount;
            }

            if (item.data == null || item.data.itemType != ItemType.CONSUMABLE)
            {
                return InventoryActionResult.NotUsable;
            }

            if (item.data is not ConsumableSO consumableData)
            {
                return InventoryActionResult.NotUsable;
            }

            InventoryActionResult applyResult = TryApplyConsumable(consumableData);
            if (applyResult != InventoryActionResult.Success)
            {
                return applyResult;
            }

            return UseItem(itemId, count)
                ? InventoryActionResult.Success
                : InventoryActionResult.Failed;
        }

        public InventoryActionResult TryEquipItem(int itemId)
        {
            if (!_itemPair.TryGetValue(itemId, out var item) || item.data == null)
            {
                return InventoryActionResult.InvalidItem;
            }

            if (item.data is not EquipmentSO equipData)
            {
                return InventoryActionResult.NotEquippable;
            }

            PlayerEquipChangeEvent eventData = new PlayerEquipChangeEvent()
            {
                itemKey = equipData.itemId,
                weaponType = equipData.weaponType,
                equipPosition = equipData.equipSlot,
                isEquip = true
            };

            if (EventManager.Instance == null)
            {
                return InventoryActionResult.Failed;
            }

            EventManager.Instance.Send(PlayerEvent.EquipItem, eventData);
            return eventData.handled && eventData.succeeded
                ? InventoryActionResult.Success
                : InventoryActionResult.Failed;
        }

        public InventoryActionResult TryDropItem(int itemId, int count = 1)
        {
            if (count <= 0)
            {
                return InventoryActionResult.Failed;
            }

            if (!_itemPair.TryGetValue(itemId, out var item))
            {
                return InventoryActionResult.InvalidItem;
            }

            if (item.count < count)
            {
                return InventoryActionResult.NotEnoughCount;
            }

            if (IsEquippedItem(itemId))
            {
                return InventoryActionResult.EquippedItem;
            }

            return RemoveItem(itemId, count)
                ? InventoryActionResult.Success
                : InventoryActionResult.Failed;
        }

        public bool IsEquippedItem(int itemId)
        {
            PlayerEquipment playerEquipment = GameObjectManager.Instance?.Player?.GetPlayerEquipment();
            if (playerEquipment == null)
            {
                return false;
            }

            if (playerEquipment.MainWeaponKey == itemId || playerEquipment.SubWeaponKey == itemId)
            {
                return true;
            }

            if (ItemManager.Instance.GetItemData(itemId) is not EquipmentSO equipment)
            {
                return false;
            }

            EquipArmorType armorType = ToArmorType(equipment.equipSlot);
            return armorType != EquipArmorType.None &&
                   playerEquipment.GetActiveEquipmentKey(armorType) == itemId;
        }

        /// <summary>
        /// 퀘스트 아이템 전달 공통 API. 소비 성공 시 ItemDeliver 퀘스트 목표를 갱신한다.
        /// </summary>
        public bool DeliverItemToQuest(int npcId, int itemId, int count = 1)
        {
            if (count <= 0)
            {
                return false;
            }

            if (!RemoveItem(itemId, count))
            {
                return false;
            }

            QuestManager.Instance?.NotifyItemDelivered(npcId, itemId, count);
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

        /// <summary>
        /// 제작 롤백/세이브 복원처럼 새 획득으로 처리하면 안 되는 수량 복구.
        /// </summary>
        public void RestoreItem(int itemId, int count)
        {
            if (count <= 0) return;
            AddItemInternal(itemId, count, false);
        }

        private void AddItemInternal(int itemId, int count, bool notifyProgress)
        {
            if (count <= 0) return;

            if (_itemPair.TryGetValue(itemId, out var existing))
            {
                existing.count += count;
                if (notifyProgress)
                {
                    NotifyItemCollected(itemId, count);
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

            if (notifyProgress)
            {
                NotifyItemCollected(itemId, count);
            }
        }

        // 매니저 미초기화 시점(씬 전환/종료 등)에도 안전하도록 가드 후 알림
        private void NotifyItemCollected(int itemId, int count)
        {
            SoundManager.Instance?.PlayUi(GameSoundKey.GetItem);
            QuestManager.Instance?.NotifyItemCollected(itemId, count);

            if (RecipeManager.Instance != null)
                RecipeManager.Instance.NotifyItemCollected(itemId, count);
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

        private static EquipArmorType ToArmorType(EquipPosition equipPosition)
        {
            switch (equipPosition)
            {
                case EquipPosition.Chest: return EquipArmorType.Chest;
                case EquipPosition.Head: return EquipArmorType.Head;
                case EquipPosition.Gloves: return EquipArmorType.Arm;
                case EquipPosition.Pants: return EquipArmorType.Waist;
                case EquipPosition.Shoes: return EquipArmorType.Leg;
                default: return EquipArmorType.None;
            }
        }

        private InventoryActionResult TryApplyConsumable(ConsumableSO consumableData)
        {
            if (consumableData == null || consumableData.amount <= 0f)
            {
                return InventoryActionResult.NotUsable;
            }

            var player = GameObjectManager.Instance?.Player;
            if (player == null || !player.IsAlive())
            {
                return InventoryActionResult.Failed;
            }

            float beforeHealth = player.CurrentHealth;
            switch (consumableData.effectType)
            {
                case ConsumableEffectType.HealFlat:
                    if (consumableData.requireEffectiveUse && beforeHealth >= player.MaxHealth - 0.01f)
                    {
                        return InventoryActionResult.NoEffect;
                    }
                    player.Heal(consumableData.amount);
                    break;
                case ConsumableEffectType.HealPercent:
                    if (consumableData.requireEffectiveUse && beforeHealth >= player.MaxHealth - 0.01f)
                    {
                        return InventoryActionResult.NoEffect;
                    }
                    player.HealPercent(consumableData.amount);
                    break;
                default:
                    return InventoryActionResult.NotUsable;
            }

            if (consumableData.requireEffectiveUse && player.CurrentHealth <= beforeHealth)
            {
                return InventoryActionResult.NoEffect;
            }

            return InventoryActionResult.Success;
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

        public void ResetForNewGame()
        {
            _pendingLoad = null;
            _itemPair.Clear();
            Gold = 0;

            // 신규 실행 직후 OnItemDatabaseReady와 동일하게 기본 아이템을 채운다.
            if (ItemManager.Instance != null && ItemManager.Instance.IsItemDBLoaded)
                MakeTestItems();
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
                RestoreItem(entry.itemId, entry.count);
                if (_itemPair.TryGetValue(entry.itemId, out var instance))
                    instance.inventorySlotKey = entry.slotKey;
            }
            _pendingLoad = null;
        }

        #endregion
    }
}
