using System.Collections.Generic;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Data.Path;
using UPlayGround.Data.Save;
using UPlayGround.Data.Item;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Party;
using UPlayGround.Economy;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 한 캐릭터의 장비 슬롯 상태. 각 슬롯은 인벤토리 슬롯 키(-1 = 빈칸)를 보관한다.
    /// 무기(주/보조)와 방어구 5부위를 모두 담으며, 방어구는 데이터만 보관(외형 미반영).
    /// </summary>
    public class CharacterEquipment
    {
        public int rightHand = -1; // 주 무기
        public int leftHand  = -1; // 보조 무기
        public int head      = -1;
        public int chest     = -1;
        public int pants     = -1;
        public int shoes     = -1;
        public int gloves    = -1;

        public int Get(EquipPosition slot) => slot switch
        {
            EquipPosition.RightHand => rightHand,
            EquipPosition.LeftHand  => leftHand,
            EquipPosition.Head      => head,
            EquipPosition.Chest     => chest,
            EquipPosition.Pants     => pants,
            EquipPosition.Shoes     => shoes,
            EquipPosition.Gloves    => gloves,
            _                       => -1
        };

        public void Set(EquipPosition slot, int itemId)
        {
            switch (slot)
            {
                case EquipPosition.RightHand: rightHand = itemId; break;
                case EquipPosition.LeftHand:  leftHand  = itemId; break;
                case EquipPosition.Head:      head      = itemId; break;
                case EquipPosition.Chest:     chest     = itemId; break;
                case EquipPosition.Pants:     pants     = itemId; break;
                case EquipPosition.Shoes:     shoes     = itemId; break;
                case EquipPosition.Gloves:    gloves    = itemId; break;
            }
        }

        /// <summary> 해당 인벤토리 슬롯 키를 장착 중인 슬롯 수. </summary>
        public int CountOf(int inventorySlotKey)
        {
            if (inventorySlotKey < 0) return 0;
            int n = 0;
            if (rightHand == inventorySlotKey) n++;
            if (leftHand  == inventorySlotKey) n++;
            if (head      == inventorySlotKey) n++;
            if (chest     == inventorySlotKey) n++;
            if (pants     == inventorySlotKey) n++;
            if (shoes     == inventorySlotKey) n++;
            if (gloves    == inventorySlotKey) n++;
            return n;
        }

        /// <summary> 지정 인벤토리 슬롯 키를 낀 첫 슬롯을 비우고 그 슬롯을 반환. 없으면 false. </summary>
        public bool TryRemoveFirst(int inventorySlotKey, out EquipPosition freedSlot)
        {
            foreach (var slot in AllSlots)
            {
                if (Get(slot) == inventorySlotKey)
                {
                    Set(slot, -1);
                    freedSlot = slot;
                    return true;
                }
            }
            freedSlot = EquipPosition.None;
            return false;
        }

        public static readonly EquipPosition[] AllSlots =
        {
            EquipPosition.RightHand, EquipPosition.LeftHand,
            EquipPosition.Head, EquipPosition.Chest, EquipPosition.Pants,
            EquipPosition.Shoes, EquipPosition.Gloves
        };
    }

    public class InventoryManager : BaseManager<InventoryManager>, IManager, ISaveable, IAsyncInitializableManager,
        UPlayGround.UI.IUIInventoryService,
        IInventoryService
    {
        private const string STARTING_INVENTORY_KEY = "StartingInventory";

        private Dictionary<int, ItemInstance> _itemPair = new Dictionary<int, ItemInstance>();
        private int _nextInventorySlotKey = int.MaxValue;
        private readonly Dictionary<int, ConsumableCooldownState> _consumableCooldowns = new();

        private readonly struct ConsumableCooldownState
        {
            public readonly float EndTime;
            public readonly float Duration;

            public ConsumableCooldownState(float endTime, float duration)
            {
                EndTime = endTime;
                Duration = duration;
            }
        }

        // itemId → 해당 아이템을 담고 있는 인벤토리 슬롯 키 목록(역인덱스).
        // 장비가 인스턴스별 슬롯 키로 쪼개지면서 itemId 조회가 _itemPair 전체 선형 스캔(O(n))이 되는 것을 막는다.
        // _itemPair 구조 변경은 반드시 PutItem/RemoveSlot/ClearItems 를 통해서만 하여 이 인덱스와 동기화한다.
        private readonly Dictionary<int, List<int>> _slotKeysByItemId = new();
        private static readonly List<int> s_emptySlotKeys = new();

        public IReadOnlyDictionary<int, ItemInstance> ItemDict => _itemPair;

        // _itemPair 에 슬롯을 추가/교체하고 역인덱스를 갱신한다. (구조 변경 단일 진입점)
        private void PutItem(int slotKey, ItemInstance instance)
        {
            if (_itemPair.TryGetValue(slotKey, out var previous))
                UnindexSlot(slotKey, previous);

            _itemPair[slotKey] = instance;
            IndexSlot(slotKey, instance);
        }

        // _itemPair 에서 슬롯을 제거하고 역인덱스를 갱신한다. 제거되면 true.
        private bool RemoveSlot(int slotKey)
        {
            if (!_itemPair.TryGetValue(slotKey, out var instance))
                return false;

            _itemPair.Remove(slotKey);
            UnindexSlot(slotKey, instance);
            return true;
        }

        private void ClearItems()
        {
            _itemPair.Clear();
            _slotKeysByItemId.Clear();
            _consumableCooldowns.Clear();
        }

        private void IndexSlot(int slotKey, ItemInstance instance)
        {
            if (instance?.data == null)
                return;

            int itemId = instance.data.itemId;
            if (!_slotKeysByItemId.TryGetValue(itemId, out var keys))
            {
                keys = new List<int>();
                _slotKeysByItemId[itemId] = keys;
            }

            if (!keys.Contains(slotKey))
                keys.Add(slotKey);
        }

        private void UnindexSlot(int slotKey, ItemInstance instance)
        {
            if (instance?.data == null)
                return;

            int itemId = instance.data.itemId;
            if (_slotKeysByItemId.TryGetValue(itemId, out var keys))
            {
                keys.Remove(slotKey);
                if (keys.Count == 0)
                    _slotKeysByItemId.Remove(itemId);
            }
        }

        // 해당 itemId 를 담고 있는 슬롯 키 목록. 없으면 빈 리스트(공유 인스턴스, 수정 금지).
        private List<int> GetSlotKeys(int itemId) =>
            _slotKeysByItemId.TryGetValue(itemId, out var keys) ? keys : s_emptySlotKeys;

        // 캐릭터별 장비 레지스트리 — 활성/벤치 공통 단일 소스. 외형은 캐릭터 모델 기본 장비를 사용한다.
        private readonly Dictionary<CharacterActorType, CharacterEquipment> _equipmentByCharacter = new();

        /// <summary> 파티원 장비 변경 시 발행 (UI 갱신용). </summary>
        public event System.Action OnPartyEquipmentChanged;

        /// <summary> 아이템 수량이 변동될 때 발행 (인벤토리/제작 UI 실시간 갱신용). </summary>
        public event System.Action OnInventoryChanged;

        /// <summary> 골드 잔액이 변동될 때 발행 (인벤토리/제작 UI 실시간 갱신용). </summary>
        public event System.Action OnGoldChanged;

        // 아이템 수량 변동 지점에서 호출 — UI가 보유 수량을 즉시 반영하도록 알린다.
        private void RaiseInventoryChanged() => OnInventoryChanged?.Invoke();

        // [TODO] Config 데이터로 별도 분리 필요
        public float MaxWeight => 3000.0f;

        /// <summary> 인벤토리 최대 슬롯 수 (UI 용량 표시용). </summary>
        public int MaxSlots => 120;

        private readonly CurrencyWallet _goldWallet = new();

        /// <summary> 보유 골드 </summary>
        public int Gold => _goldWallet.Balance;

        /// <summary>골드를 안전하게 추가하고 성공한 경우 변경 이벤트를 발행한다.</summary>
        public bool TryAddGold(int amount)
        {
            if (!_goldWallet.TryDeposit(amount))
                return false;

            OnGoldChanged?.Invoke();
            return true;
        }

        /// <summary>잔액이 충분할 때만 골드를 차감하고 변경 이벤트를 발행한다.</summary>
        public bool TrySpendGold(int amount)
        {
            if (!_goldWallet.TryWithdraw(amount))
                return false;

            OnGoldChanged?.Invoke();
            return true;
        }

        private void RestoreGold(int amount)
        {
            int previousGold = Gold;
            _goldWallet.Restore(amount);
            if (Gold != previousGold)
                OnGoldChanged?.Invoke();
        }

        // ItemDatabase 로드 완료 전에 LoadGame()이 호출될 경우 보관
        private InventorySaveData _pendingLoad;
        private StartingInventorySO _startingInventory;
        private bool _applyStartingEquipmentOnSeed;
        private bool _pendingNewGameStartingInventory;

        public void Init()
        {
            SaveManager.Instance.RegisterSaveable(this);
        }

        public void AfterInit()
        {

        }

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            try
            {
                _startingInventory = await AssetManager.Instance.LoadGlobalAsync<StartingInventorySO>(
                    STARTING_INVENTORY_KEY,
                    nameof(InventoryManager),
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                _startingInventory = null;
                Debug.LogWarning($"[InventoryManager] StartingInventory 로드 실패. 초기 아이템 지급을 건너뜁니다: {e.Message}");
            }

            if (_pendingNewGameStartingInventory)
            {
                ApplyStartingInventoryForNewGame();
                _pendingNewGameStartingInventory = false;
            }
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

            if (itemInstance.data is EquipmentSO)
            {
                AddEquipmentInstances(itemInstance.data, itemInstance.count, true);
                return;
            }

            if (_itemPair.ContainsKey(itemId) == false)
            {
                itemInstance.inventorySlotKey = itemId;
                PutItem(itemId, itemInstance);

                // TODO...  인벤토리 슬롯 지정 필요
            }
            else
            {
                _itemPair[itemId].count += itemInstance.count;
            }

            NotifyItemCollected(itemId, itemInstance.count);
            RaiseInventoryChanged();
        }

        public void RemoveItem(int itemId)
        {
            int count = GetItemCount(itemId);
            if (count > 0)
                RemoveItem(itemId, count);
        }

        /// <summary>
        /// 아이템을 count만큼 차감한다.
        /// count 이후 수량이 0 이하가 되면 인벤토리에서 제거한다.
        /// 재고 부족 시 false 반환 (차감 없음).
        /// </summary>
        public bool RemoveItem(int itemId, int count)
        {
            return TryRemoveItemInstances(itemId, count, out _);
        }

        /// <summary>
        /// 아이템을 차감하고 실제로 제거된 인스턴스를 반환한다.
        /// 제작처럼 후속 단계 실패 시 강화/랜덤 옵션까지 동일하게 롤백해야 하는 트랜잭션에서 사용한다.
        /// </summary>
        public bool TryRemoveItemInstances(int itemId, int count, out List<ItemInstance> removedItems)
        {
            removedItems = new List<ItemInstance>();
            if (count <= 0)
                return false;

            if (GetItemCount(itemId) < count)
                return false;

            if (_itemPair.TryGetValue(itemId, out var stackedItem) && stackedItem.data is not EquipmentSO)
            {
                if (stackedItem.count < count)
                    return false;

                removedItems.Add(CloneItemInstance(stackedItem, count));
                stackedItem.count -= count;

                if (stackedItem.count <= 0)
                    RemoveSlot(itemId);

                RaiseInventoryChanged();
                return true;
            }

            // 장비는 인스턴스 1개가 인벤토리 슬롯 1개를 차지한다.
            // 장착 레지스트리는 itemId 기반이므로, 장착 수량을 남긴 채 여유분만 제거한다.
            if (GetItemDataForInventory(itemId) is EquipmentSO && count > GetFreeCount(itemId))
                return false;

            int remaining = count;
            var removeKeys = new List<int>();
            var slotKeys = GetSlotKeys(itemId);
            for (int i = 0; i < slotKeys.Count; i++)
            {
                int key = slotKeys[i];
                if (IsInventorySlotEquipped(key))
                    continue;

                removeKeys.Add(key);
                remaining--;
                if (remaining <= 0)
                    break;
            }

            if (remaining > 0)
                return false;

            foreach (int key in removeKeys)
            {
                if (_itemPair.TryGetValue(key, out ItemInstance instance))
                    removedItems.Add(CloneItemInstance(instance, instance.count));
                RemoveSlot(key);
            }

            RaiseInventoryChanged();
            return true;
        }

        /// <summary>차감 영수증의 아이템을 원래 슬롯 키와 인스턴스 데이터로 복원한다.</summary>
        public void RestoreItemInstances(IEnumerable<ItemInstance> removedItems)
        {
            if (removedItems == null)
                return;

            bool restoredAny = false;
            foreach (ItemInstance removed in removedItems)
            {
                if (removed?.data == null || removed.count <= 0)
                    continue;

                if (removed.data is EquipmentSO)
                {
                    AddEquipmentInstance(
                        removed.data,
                        false,
                        removed.inventorySlotKey,
                        removed.enhancementLevel,
                        removed.growthAttributeRolls);
                }
                else if (_itemPair.TryGetValue(removed.data.itemId, out ItemInstance existing))
                {
                    existing.count += removed.count;
                }
                else
                {
                    PutItem(removed.data.itemId, CloneItemInstance(removed, removed.count));
                }

                restoredAny = true;
            }

            if (restoredAny)
                RaiseInventoryChanged();
        }

        private static ItemInstance CloneItemInstance(ItemInstance source, int count)
        {
            return new ItemInstance
            {
                data = source.data,
                count = count,
                inventorySlotKey = source.inventorySlotKey,
                enhancementLevel = source.enhancementLevel,
                growthAttributeRolls = source.growthAttributeRolls != null
                    ? new List<EquipmentGrowthAttributeRoll>(source.growthAttributeRolls)
                    : new List<EquipmentGrowthAttributeRoll>()
            };
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

            var item = GetItem(itemId);
            if (item == null)
            {
                return InventoryActionResult.InvalidItem;
            }

            if (GetItemCount(itemId) < count)
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

            if (TryGetConsumableCooldown(itemId, out _, out _))
            {
                return InventoryActionResult.OnCooldown;
            }

            InventoryActionResult applyResult = TryApplyConsumable(consumableData);
            if (applyResult != InventoryActionResult.Success)
            {
                return applyResult;
            }

            if (!UseItem(itemId, count))
            {
                return InventoryActionResult.Failed;
            }

            StartConsumableCooldown(itemId, consumableData.cooldownDuration);

            // 소모품 효과 적용과 Drink 모션 재생은 독립적이다.
            // 전투 또는 다른 행동 중에는 사용만 완료하고, 비전투 Idle 상태에서만 모션을 시작한다.
            var player = GameObjectManager.Instance?.Player;
            if (player?.CanStartConsumableUse() == true && !player.TryStartConsumableUse())
            {
                Debug.LogWarning(
                    $"[InventoryManager] 소모품 사용은 완료됐지만 Drink 상태 전환에 실패했습니다. itemId={itemId}",
                    player);
            }

            return InventoryActionResult.Success;
        }

        public bool TryGetConsumableCooldown(int itemId, out float remaining, out float duration)
        {
            if (!_consumableCooldowns.TryGetValue(itemId, out ConsumableCooldownState state))
            {
                remaining = 0f;
                duration = 0f;
                return false;
            }

            remaining = Mathf.Max(0f, state.EndTime - Time.time);
            duration = state.Duration;
            if (remaining > 0f)
                return true;

            _consumableCooldowns.Remove(itemId);
            remaining = 0f;
            return false;
        }

        private void StartConsumableCooldown(int itemId, float duration)
        {
            duration = Mathf.Max(0f, duration);
            if (duration <= 0f)
            {
                _consumableCooldowns.Remove(itemId);
                return;
            }

            _consumableCooldowns[itemId] =
                new ConsumableCooldownState(Time.time + duration, duration);
        }

        // ──────────────────────────────────────────────────────────
        #region 파티원 장비 (per-character)

        /// <summary> 현재 활성 캐릭터 타입. 무인자 equip API의 기본 대상. </summary>
        private CharacterActorType ActiveCharacterType =>
            PartyManager.Instance?.ActiveCharacterType ?? CharacterActorType.None;

        /// <summary> 캐릭터 장비 엔트리 조회(없으면 모델 시작 장비로 시딩). </summary>
        private CharacterEquipment GetOrSeedEntry(CharacterActorType c)
        {
            if (c == CharacterActorType.None) return null;
            if (_equipmentByCharacter.TryGetValue(c, out var eq)) return eq;

            eq = new CharacterEquipment();
            SeedFromModelStartItems(c, eq);
            _equipmentByCharacter[c] = eq;
            return eq;
        }

        /// <summary>
        /// 해당 캐릭터의 장비 엔트리가 없으면 주어진 시작 장비 목록으로 시딩한다.
        /// 시작 장비는 인벤토리 보유 아이템에도 1회 지급하되, 장착 외형은 변경하지 않는다.
        /// </summary>
        public void SeedCharacterEquipmentIfAbsent(CharacterActorType c, IReadOnlyList<EquipmentSO> startItems)
        {
            if (c == CharacterActorType.None || _equipmentByCharacter.ContainsKey(c))
                return;

            var eq = new CharacterEquipment();
            if (_applyStartingEquipmentOnSeed && startItems != null)
            {
                foreach (var so in startItems)
                    if (so != null)
                    {
                        int inventorySlotKey = GrantStartingEquipmentItem(so);
                        eq.Set(so.equipSlot, inventorySlotKey);
                    }
            }
            _equipmentByCharacter[c] = eq;
        }

        /// <summary> 활성 반영용 주/보조 무기 스냅샷. </summary>
        public (int mainKey, int subKey) GetActiveWeaponSnapshot(CharacterActorType c)
        {
            var eq = GetOrSeedEntry(c);
            return eq != null ? (eq.rightHand, eq.leftHand) : (-1, -1);
        }

        // 모델과 분리된 캐릭터 정의의 startingEquipment로 기본 장비를 채운다.
        // 로스터 전용 캐릭터의 3D 모델을 불필요하게 로드하지 않기 위한 데이터 경계다.
        private void SeedFromModelStartItems(CharacterActorType c, CharacterEquipment eq)
        {
            if (!_applyStartingEquipmentOnSeed)
                return;

            var definition = Svc.Party?.GetCharacterDefinition(c);
            if (definition?.startingEquipment == null) return;

            foreach (var so in definition.startingEquipment)
            {
                if (so != null)
                {
                    int inventorySlotKey = GrantStartingEquipmentItem(so);
                    eq.Set(so.equipSlot, inventorySlotKey);
                }
            }
        }

        // 시작 장비 SO를 인벤토리 보유 아이템으로 시딩한다. 장비는 비스택 정책이므로 이미 있으면 유지한다.
        private int GrantStartingEquipmentItem(EquipmentSO equipment)
        {
            if (equipment == null || equipment.itemId < 0)
                return -1;

            return AddEquipmentInstance(equipment, false);
        }

        private void ApplyStartingInventoryForNewGame()
        {
            if (_startingInventory == null || _startingInventory.items == null)
            {
                return;
            }

            foreach (var entry in _startingInventory.items)
            {
                if (entry?.item == null)
                    continue;

                AddStartingItem(entry.item, Mathf.Max(1, entry.count));
            }
        }

        private void AddStartingItem(ItemSO itemData, int count)
        {
            if (itemData == null || count <= 0)
                return;

            if (itemData is EquipmentSO)
            {
                AddEquipmentInstances(itemData, count, false);
                return;
            }

            if (_itemPair.TryGetValue(itemData.itemId, out var existing))
            {
                existing.count += count;
                return;
            }

            PutItem(itemData.itemId, new ItemInstance { data = itemData, count = count, inventorySlotKey = itemData.itemId });
        }

        private int OwnedCount(int itemId) =>
            GetItemCount(itemId);

        /// <summary> 파티 전체에서 해당 itemId가 장착된 총 개수. </summary>
        public int GetEquippedCount(int itemId)
        {
            int n = 0;
            foreach (var eq in _equipmentByCharacter.Values)
            {
                foreach (var slot in CharacterEquipment.AllSlots)
                {
                    int inventorySlotKey = eq.Get(slot);
                    if (IsInventorySlotKeyForItemId(inventorySlotKey, itemId))
                        n++;
                }
            }
            return n;
        }

        /// <summary> 아직 장착에 쓰지 않은 여유 수량. </summary>
        public int GetFreeCount(int itemId) => OwnedCount(itemId) - GetEquippedCount(itemId);

        public int GetEquippedItem(CharacterActorType c, EquipPosition slot) =>
            GetOrSeedEntry(c)?.Get(slot) ?? -1;

        public ItemInstance GetInventoryItemBySlotKey(int inventorySlotKey)
        {
            _itemPair.TryGetValue(inventorySlotKey, out var item);
            return item;
        }

        public List<EquipmentSO> GetEquippedEquipment(CharacterActorType c)
        {
            var result = new List<EquipmentSO>();
            var eq = GetOrSeedEntry(c);
            if (eq == null)
                return result;

            for (int i = 0; i < CharacterEquipment.AllSlots.Length; i++)
            {
                int inventorySlotKey = eq.Get(CharacterEquipment.AllSlots[i]);
                if (inventorySlotKey < 0)
                    continue;

                if (GetEquipmentDataBySlotKey(inventorySlotKey) is EquipmentSO equipment)
                    result.Add(equipment);
            }

            return result;
        }

        /// <summary>인스턴스별 랜덤 능력치를 포함한 현재 장착 장비 목록.</summary>
        public List<ItemInstance> GetEquippedItemInstances(CharacterActorType c)
        {
            var result = new List<ItemInstance>();
            var eq = GetOrSeedEntry(c);
            if (eq == null)
                return result;

            for (int i = 0; i < CharacterEquipment.AllSlots.Length; i++)
            {
                int inventorySlotKey = eq.Get(CharacterEquipment.AllSlots[i]);
                if (inventorySlotKey >= 0 &&
                    _itemPair.TryGetValue(inventorySlotKey, out ItemInstance instance) &&
                    instance?.data is EquipmentSO)
                    result.Add(instance);
            }
            return result;
        }

        /// <summary> 해당 인벤토리 슬롯 키를 장착 중인 캐릭터 목록. </summary>
        public List<CharacterActorType> GetEquippingCharacters(int inventorySlotKey)
        {
            var list = new List<CharacterActorType>();
            foreach (var kv in _equipmentByCharacter)
                if (kv.Value.CountOf(inventorySlotKey) > 0)
                    list.Add(kv.Key);
            return list;
        }

        // 보조 무기(왼손)는 캐릭터 모델 기본 타입이 받아들이는 보조 무기 아이템만 호환된다.
        // (검+방패→방패, 쌍검→두 번째 검) 기준은 교체 가능한 주 무기 아이템이 아니라
        // 캐릭터의 정체성인 모델 기본 타입이므로, 베이스 검을 끼워도 보조 슬롯 판정은 흔들리지 않는다.
        private bool IsSubWeaponCompatible(CharacterActorType c, WeaponType subWeaponType)
        {
            if (subWeaponType == WeaponType.NoWeapon) return false;
            return GetModelDefaultWeaponType(c).AcceptsSubWeaponItem(subWeaponType);
        }

        private WeaponType GetModelDefaultWeaponType(CharacterActorType c)
        {
            return Svc.Party?.GetCharacterDefinition(c)?.defaultWeaponType
                   ?? WeaponType.NoWeapon;
        }

        // 주 무기는 캐릭터 모델 기본 주무기 타입이 받아들이는 무기 아이템만 장착할 수 있다.
        // (동일 타입 + 검 기반 무기(검+방패·쌍검)의 기본 검 아이템 허용 — 비대칭)
        private bool IsMainWeaponCompatible(CharacterActorType c, WeaponType weaponType)
        {
            if (weaponType == WeaponType.NoWeapon) return false;
            return GetModelDefaultWeaponType(c).AcceptsMainWeaponItem(weaponType);
        }

        // 주 무기 슬롯이 바뀐 뒤, 현재 보조 무기가 캐릭터 모델 기본 타입과 맞지 않으면 해제한다.
        // 기준은 모델 기본 타입이므로 베이스 검 아이템을 끼워도 방패/두 번째 검은 유지된다.
        private void EnsureSubCompatibility(CharacterActorType c, CharacterEquipment eq)
        {
            if (eq.leftHand < 0) return;

            bool keep = GetEquipmentDataBySlotKey(eq.leftHand) is EquipmentSO subEq &&
                        IsSubWeaponCompatible(c, subEq.weaponType);
            if (!keep)
                eq.leftHand = -1;
        }

        // 여유분이 없을 때, 해당 itemId를 낀 다른 캐릭터(roster 순서 우선)에게서 해제해 한 인스턴스를 확보한다.
        private bool TransferFromAnotherOwner(int itemId, CharacterActorType requester, out int freedInventorySlotKey)
        {
            freedInventorySlotKey = -1;
            var roster = PartyManager.Instance?.Roster;
            if (roster != null)
            {
                foreach (var owner in roster)
                {
                    if (owner == requester) continue;
                    if (_equipmentByCharacter.TryGetValue(owner, out var oeq) &&
                        TryRemoveFirstEquippedItem(owner, oeq, itemId, out freedInventorySlotKey))
                    {
                        return true;
                    }
                }
            }

            // roster에 없는 소유자(예외적)까지 폴백 탐색
            foreach (var kv in _equipmentByCharacter)
            {
                if (kv.Key == requester) continue;
                if (TryRemoveFirstEquippedItem(kv.Key, kv.Value, itemId, out freedInventorySlotKey))
                {
                    return true;
                }
            }
            return false;
        }

        private bool TryRemoveFirstEquippedItem(CharacterActorType owner, CharacterEquipment eq, int itemId, out int freedInventorySlotKey)
        {
            freedInventorySlotKey = -1;
            if (eq == null)
                return false;

            foreach (var slot in CharacterEquipment.AllSlots)
            {
                int inventorySlotKey = eq.Get(slot);
                if (!IsInventorySlotKeyForItemId(inventorySlotKey, itemId))
                    continue;

                CaptureHealthSnapshot(owner, out float oldHp, out float oldMax);
                eq.Set(slot, -1);
                if (slot == EquipPosition.RightHand)
                    EnsureSubCompatibility(owner, eq);
                SyncActiveCharacterVisual(owner);
                ApplyEquipmentStats(owner, oldHp, oldMax);
                freedInventorySlotKey = inventorySlotKey;
                return true;
            }

            return false;
        }

        private void UnequipInventorySlotKey(int inventorySlotKey, CharacterActorType requester)
        {
            foreach (var kv in _equipmentByCharacter)
            {
                var eq = kv.Value;
                if (eq == null)
                    continue;

                foreach (var slot in CharacterEquipment.AllSlots)
                {
                    if (eq.Get(slot) != inventorySlotKey)
                        continue;

                    CaptureHealthSnapshot(kv.Key, out float oldHp, out float oldMax);
                    eq.Set(slot, -1);
                    if (slot == EquipPosition.RightHand)
                        EnsureSubCompatibility(kv.Key, eq);

                    if (kv.Key != requester)
                    {
                        SyncActiveCharacterVisual(kv.Key);
                        ApplyEquipmentStats(kv.Key, oldHp, oldMax);
                    }
                    return;
                }
            }
        }

        private bool IsInventorySlotEquipped(int inventorySlotKey)
        {
            foreach (var eq in _equipmentByCharacter.Values)
            {
                if (eq != null && eq.CountOf(inventorySlotKey) > 0)
                    return true;
            }

            return false;
        }

        private int FindFreeInventorySlotKey(int itemId)
        {
            var slotKeys = GetSlotKeys(itemId);
            for (int i = 0; i < slotKeys.Count; i++)
            {
                if (!IsInventorySlotEquipped(slotKeys[i]))
                    return slotKeys[i];
            }

            return -1;
        }

        private bool IsInventorySlotKeyForItemId(int inventorySlotKey, int itemId)
        {
            return _itemPair.TryGetValue(inventorySlotKey, out var item) &&
                   item?.data != null &&
                   item.data.itemId == itemId;
        }

        private EquipmentSO GetEquipmentDataBySlotKey(int inventorySlotKey)
        {
            return _itemPair.TryGetValue(inventorySlotKey, out var item)
                ? item.data as EquipmentSO
                : null;
        }

        // 장비 레지스트리는 데이터/UI/세이브 용도다. 외형은 캐릭터 모델 기본 장비를 유지한다.
        private void SyncActiveCharacterVisual(CharacterActorType c)
        {
        }

        public InventoryActionResult TryEquipItem(int itemId) => TryEquipItem(ActiveCharacterType, itemId);

        public InventoryActionResult TryEquipItem(CharacterActorType c, int itemId)
        {
            var item = GetItem(itemId);
            if (item == null || item.data == null)
                return InventoryActionResult.InvalidItem;
            if (item.data is not EquipmentSO equipData)
                return InventoryActionResult.NotEquippable;
            // 대상 슬롯을 지정하지 않으면 아이템의 기본 장착 부위로 장착한다.
            return TryEquipItem(c, itemId, equipData.equipSlot);
        }

        // 대상 슬롯을 지정해 장착한다. (쌍검 캐릭터가 검을 주/보조 손에 각각 장착하는 등)
        public InventoryActionResult TryEquipItem(CharacterActorType c, int itemId, EquipPosition targetSlot)
        {
            if (c == CharacterActorType.None)
                return InventoryActionResult.Failed;
            var item = GetItem(itemId);
            if (item == null || item.data == null)
                return InventoryActionResult.InvalidItem;
            if (item.data is not EquipmentSO equipData)
                return InventoryActionResult.NotEquippable;
            if (!CanEquipItem(c, equipData, targetSlot))
                return InventoryActionResult.NotEquippable;

            int inventorySlotKey = FindFreeInventorySlotKey(itemId);
            if (inventorySlotKey < 0 && !TransferFromAnotherOwner(itemId, c, out inventorySlotKey))
                return InventoryActionResult.Failed;

            return TryEquipInventorySlot(c, inventorySlotKey, targetSlot);
        }

        public InventoryActionResult TryEquipInventorySlot(CharacterActorType c, int inventorySlotKey) =>
            TryEquipInventorySlot(c, inventorySlotKey, GetEquipmentDataBySlotKey(inventorySlotKey)?.equipSlot ?? EquipPosition.None);

        public InventoryActionResult TryEquipInventorySlot(CharacterActorType c, int inventorySlotKey, EquipPosition targetSlot)
        {
            if (c == CharacterActorType.None)
                return InventoryActionResult.Failed;

            if (!_itemPair.TryGetValue(inventorySlotKey, out var item) || item.data == null)
                return InventoryActionResult.InvalidItem;

            if (item.data is not EquipmentSO equipData)
                return InventoryActionResult.NotEquippable;

            if (!CanEquipItem(c, equipData, targetSlot))
                return InventoryActionResult.NotEquippable;

            var eq = GetOrSeedEntry(c);
            EquipPosition slot = targetSlot;

            // 같은 슬롯에 이미 같은 장비 인스턴스면 무시
            if (eq.Get(slot) == inventorySlotKey)
                return InventoryActionResult.Success;

            CaptureHealthSnapshot(c, out float oldHp, out float oldMax);

            // 같은 장비 인스턴스가 다른 슬롯/캐릭터에 장착되어 있으면 먼저 해제해서 이동한다.
            UnequipInventorySlotKey(inventorySlotKey, c);

            eq.Set(slot, inventorySlotKey);

            // 주 무기 교체 시 보조 무기 호환성 재검사
            if (slot == EquipPosition.RightHand)
                EnsureSubCompatibility(c, eq);

            SyncActiveCharacterVisual(c);
            ApplyEquipmentStats(c, oldHp, oldMax);
            OnPartyEquipmentChanged?.Invoke();
            return InventoryActionResult.Success;
        }

        public InventoryActionResult TryUnequipItem(CharacterActorType c, EquipPosition slot)
        {
            var eq = GetOrSeedEntry(c);
            if (eq == null || eq.Get(slot) < 0)
                return InventoryActionResult.Failed;

            CaptureHealthSnapshot(c, out float oldHp, out float oldMax);

            eq.Set(slot, -1);

            // 주 무기(아이템) 해제 시 보조 무기가 유효 주 무기 타입(빌트인 폴백)과 맞지 않으면 해제
            if (slot == EquipPosition.RightHand)
                EnsureSubCompatibility(c, eq);

            SyncActiveCharacterVisual(c);
            ApplyEquipmentStats(c, oldHp, oldMax);
            OnPartyEquipmentChanged?.Invoke();
            return InventoryActionResult.Success;
        }

        private void CaptureHealthSnapshot(CharacterActorType c, out float currentHealth, out float maxHealth)
        {
            var player = GameObjectManager.Instance?.Player;
            currentHealth = player != null ? player.GetHealthForCharacter(c) : 0f;
            maxHealth = player != null ? player.GetMaxHealthForCharacter(c) : 1f;
        }

        private void ApplyEquipmentStats(CharacterActorType c, float oldHp, float oldMax)
        {
            GameObjectManager.Instance?.Player?.RefreshEquipmentStatsForCharacter(c, oldHp, oldMax);
            PartyManager.Instance?.NotifyEquipmentStatsChanged(c);
        }

        public bool CanEquipItem(int itemId) => CanEquipItem(ActiveCharacterType, itemId);

        public bool CanEquipItem(CharacterActorType c, int itemId)
        {
            var item = GetItem(itemId);
            if (item == null || item.data == null)
                return false;
            return item.data is EquipmentSO equipData && CanEquipItem(c, equipData);
        }

        public bool CanEquipItem(EquipmentSO equipData) => CanEquipItem(ActiveCharacterType, equipData);

        public bool CanEquipItem(CharacterActorType c, EquipmentSO equipData)
            => CanEquipItem(c, equipData, equipData != null ? equipData.equipSlot : EquipPosition.None);

        // 지정한 대상 슬롯에 장착 가능한지. (무기는 아이템 기본 슬롯과 무관하게 좌/우 손을 지정할 수 있다.
        //  쌍검 캐릭터는 검을 주/보조 양손에, 검+방패는 검을 주 무기·방패를 보조로 장착한다.)
        public bool CanEquipItem(CharacterActorType c, EquipmentSO equipData, EquipPosition targetSlot)
        {
            if (equipData == null || c == CharacterActorType.None)
                return false;

            switch (targetSlot)
            {
                case EquipPosition.RightHand:
                    // 주 무기: 캐릭터 고유 주무기 타입이 받아들이는 장비만 장착 가능
                    return equipData.weaponType != WeaponType.NoWeapon &&
                           equipData.HasUsableWeaponVisual &&
                           IsMainWeaponCompatible(c, equipData.weaponType);
                case EquipPosition.LeftHand:
                    // 보조 무기: 기본 유효성 + 캐릭터 모델 기본 타입이 받아들이는 보조 무기일 때만
                    return equipData.weaponType != WeaponType.NoWeapon &&
                           equipData.HasUsableWeaponVisual &&
                           IsSubWeaponCompatible(c, equipData.weaponType);
                case EquipPosition.Head:
                case EquipPosition.Chest:
                case EquipPosition.Pants:
                case EquipPosition.Shoes:
                case EquipPosition.Gloves:
                    // 방어구는 자신의 지정 부위 슬롯에만 장착 가능
                    return equipData.equipSlot == targetSlot &&
                           ToArmorType(targetSlot) != EquipArmorType.None;
                default:
                    return false;
            }
        }

        #endregion

        public InventoryActionResult TryDropItem(int itemId, int count = 1)
        {
            if (count <= 0)
            {
                return InventoryActionResult.Failed;
            }

            var item = GetItem(itemId);
            if (item == null)
            {
                return InventoryActionResult.InvalidItem;
            }

            if (GetItemCount(itemId) < count)
            {
                return InventoryActionResult.NotEnoughCount;
            }

            // 장착에 쓰이지 않은 여유분만 드롭 가능 (파티원이 낀 copy는 보호)
            if (count > GetFreeCount(itemId))
            {
                return InventoryActionResult.EquippedItem;
            }

            return RemoveItem(itemId, count)
                ? InventoryActionResult.Success
                : InventoryActionResult.Failed;
        }

        /// <summary> 파티 내 누군가 장착 중인 아이템인지. </summary>
        public bool IsEquippedItem(int itemId) => GetEquippedCount(itemId) > 0;

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

            var itemData = ItemManager.Instance.GetItemData(itemId);
            if (itemData == null)
            {
                Debug.LogWarning($"[InventoryManager] AddItem 실패 — ItemID {itemId}를 ItemDatabase에서 찾을 수 없습니다.");
                return;
            }

            if (itemData is EquipmentSO)
            {
                AddEquipmentInstances(itemData, count, notifyProgress);
                return;
            }

            if (_itemPair.TryGetValue(itemId, out var existing))
            {
                existing.count += count;
                if (notifyProgress)
                {
                    NotifyItemCollected(itemId, count);
                }
                RaiseInventoryChanged();
                return;
            }

            PutItem(itemId, new ItemInstance { data = itemData, count = count, inventorySlotKey = itemId });

            if (notifyProgress)
            {
                NotifyItemCollected(itemId, count);
            }
            RaiseInventoryChanged();
        }

        private void RestoreItem(int itemId, int count, int slotKey,
            int enhancementLevel = 0,
            List<EquipmentGrowthAttributeRoll> growthAttributeRolls = null)
        {
            if (count <= 0) return;

            var itemData = ItemManager.Instance.GetItemData(itemId);
            if (itemData == null)
            {
                Debug.LogWarning($"[InventoryManager] RestoreItem 실패 — ItemID {itemId}를 ItemDatabase에서 찾을 수 없습니다.");
                return;
            }

            if (itemData is EquipmentSO)
            {
                int firstSlotKey = slotKey != 0 ? slotKey : itemId;
                for (int i = 0; i < count; i++)
                    AddEquipmentInstance(
                        itemData,
                        false,
                        i == 0 ? firstSlotKey : 0,
                        i == 0 ? enhancementLevel : 0,
                        i == 0 ? growthAttributeRolls : null);
                return;
            }

            int key = slotKey != 0 ? slotKey : itemId;
            if (_itemPair.TryGetValue(key, out var existing))
            {
                existing.count += count;
                return;
            }

            PutItem(key, new ItemInstance { data = itemData, count = count, inventorySlotKey = key });
        }

        private void AddEquipmentInstances(ItemSO itemData, int count, bool notifyProgress, int preferredFirstSlotKey = 0)
        {
            if (itemData == null || count <= 0)
                return;

            for (int i = 0; i < count; i++)
            {
                AddEquipmentInstance(itemData, false, i == 0 ? preferredFirstSlotKey : 0);
            }

            if (notifyProgress)
            {
                NotifyItemCollected(itemData.itemId, count);
            }
            RaiseInventoryChanged();
        }

        private int AddEquipmentInstance(
            ItemSO itemData,
            bool notifyProgress,
            int preferredSlotKey = 0,
            int enhancementLevel = 0,
            List<EquipmentGrowthAttributeRoll> restoredRolls = null)
        {
            if (itemData == null)
                return -1;

            int key = CreateInventorySlotKey(itemData.itemId, preferredSlotKey);
            var instance = new ItemInstance
            {
                data = itemData,
                count = 1,
                inventorySlotKey = key,
                enhancementLevel = enhancementLevel,
                growthAttributeRolls = restoredRolls != null
                    ? new List<EquipmentGrowthAttributeRoll>(restoredRolls)
                    : RollGrowthAttributes(itemData as EquipmentSO)
            };
            PutItem(key, instance);

            if (notifyProgress)
            {
                NotifyItemCollected(itemData.itemId, 1);
                RaiseInventoryChanged();
            }

            return key;
        }

        private List<EquipmentGrowthAttributeRoll> RollGrowthAttributes(EquipmentSO equipment)
        {
            var result = new List<EquipmentGrowthAttributeRoll>();
            if (equipment == null || !equipment.grantRandomGrowthAttributes)
                return result;

            var pool = new List<AttributeId>();
            if (equipment.randomAttributePool != null && equipment.randomAttributePool.Count > 0)
            {
                for (int i = 0; i < equipment.randomAttributePool.Count; i++)
                {
                    AttributeId attributeId =
                        equipment.randomAttributePool[i].ToCoreId();
                    if (attributeId.IsValid && !pool.Contains(attributeId))
                        pool.Add(attributeId);
                }
            }
            else
            {
                pool.AddRange(GrowthAttributeCatalog.DefaultEquipmentRollIds);
            }

            if (pool.Count == 0)
            {
                pool.AddRange(GrowthAttributeCatalog.DefaultEquipmentRollIds);
            }

            int minCount = Mathf.Clamp(equipment.randomAttributeCountMin, 1, pool.Count);
            int maxCount = Mathf.Clamp(equipment.randomAttributeCountMax, minCount, pool.Count);
            int count = UnityEngine.Random.Range(minCount, maxCount + 1);
            int minRank = Mathf.Max(1, equipment.randomRankMin);
            int maxRank = Mathf.Max(minRank, equipment.randomRankMax);
            float rankUpgradeChance = Mathf.Clamp01(
                (Svc.Passives?.GetBattlePartyMultiplier(
                    PassiveModifierType.EquipmentGrowthRankLuck) ?? 1f) - 1f);

            for (int i = 0; i < count; i++)
            {
                int index = UnityEngine.Random.Range(0, pool.Count);
                int rank = UnityEngine.Random.Range(minRank, maxRank + 1);
                if (rank < maxRank && UnityEngine.Random.value < rankUpgradeChance)
                    rank++;
                result.Add(new EquipmentGrowthAttributeRoll
                {
                    attributeId = pool[index].Value,
                    rank = rank
                });
                pool.RemoveAt(index);
            }
            return result;
        }

        private int CreateInventorySlotKey(int itemId, int preferredSlotKey = 0)
        {
            if (preferredSlotKey != 0 && !_itemPair.ContainsKey(preferredSlotKey))
                return preferredSlotKey;

            if (!_itemPair.ContainsKey(itemId))
                return itemId;

            while (_nextInventorySlotKey > 0 && _itemPair.ContainsKey(_nextInventorySlotKey))
                _nextInventorySlotKey--;

            return _nextInventorySlotKey--;
        }

        private ItemSO GetItemDataForInventory(int itemId)
        {
            var slotKeys = GetSlotKeys(itemId);
            for (int i = 0; i < slotKeys.Count; i++)
            {
                if (_itemPair.TryGetValue(slotKeys[i], out var item) && item?.data != null)
                    return item.data;
            }

            return ItemManager.Instance != null ? ItemManager.Instance.GetItemData(itemId) : null;
        }

        // 매니저 미초기화 시점(씬 전환/종료 등)에도 안전하도록 가드 후 알림
        private void NotifyItemCollected(int itemId, int count)
        {
            QuestManager.Instance?.NotifyItemCollected(itemId, count);

            if (ItemManager.Instance?.GetItemData(itemId) is EquipmentSO)
                EventManager.Instance?.Send(GameMilestoneEvent.EquipmentAcquired);

            if (RecipeManager.Instance != null)
                RecipeManager.Instance.NotifyItemCollected(itemId, count);
        }

        public int GetItemCount(int itemId)
        {
            int count = 0;
            var slotKeys = GetSlotKeys(itemId);
            for (int i = 0; i < slotKeys.Count; i++)
            {
                if (_itemPair.TryGetValue(slotKeys[i], out var item) && item?.data != null)
                    count += item.count;
            }
            return count;
        }

        public bool HasItem(int itemId)
        {
            return GetItemCount(itemId) > 0;
        }

        public ItemInstance GetItem(int itemId)
        {
            var slotKeys = GetSlotKeys(itemId);
            for (int i = 0; i < slotKeys.Count; i++)
            {
                if (_itemPair.TryGetValue(slotKeys[i], out var item) && item?.data != null)
                    return item;
            }
            return null;
        }

        public float GetItemWeight(int itemId)
        {
            float weight = 0.0f;
            bool found = false;
            var slotKeys = GetSlotKeys(itemId);
            for (int i = 0; i < slotKeys.Count; i++)
            {
                if (!_itemPair.TryGetValue(slotKeys[i], out var item) || item?.data == null)
                    continue;

                weight += item.data.weight * item.count;
                found = true;
            }

            return found ? weight : -1.0f;
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

            float recoveryMultiplier = Svc.Passives?.GetActiveMultiplier(
                PassiveModifierType.ConsumableRecovery) ?? 1f;
            float recoveryAmount = consumableData.amount * Mathf.Max(0f, recoveryMultiplier);
            // 체력이 가득 차 있어도 소모품을 사용할 수 있도록 full-HP/무효과 게이트를 두지 않는다.
            // (회복량이 없어도 소비·모션은 정상 진행)
            switch (consumableData.effectType)
            {
                case ConsumableEffectType.HealFlat:
                    player.ApplyHealingEffect(recoveryAmount);
                    break;
                case ConsumableEffectType.HealPercent:
                    player.ApplyPercentHealingEffect(recoveryAmount);
                    break;
                default:
                    return InventoryActionResult.NotUsable;
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
                if (kv.Value?.data == null)
                    continue;

                saveData.inventory.items.Add(new ItemSaveEntry
                {
                    itemId = kv.Value.data.itemId,
                    count = kv.Value.count,
                    slotKey = kv.Key,
                    enhancementLevel = kv.Value.enhancementLevel,
                    growthAttributeRolls = kv.Value.growthAttributeRolls != null
                        ? new List<EquipmentGrowthAttributeRoll>(kv.Value.growthAttributeRolls)
                        : new List<EquipmentGrowthAttributeRoll>()
                });
            }

            saveData.inventory.equipment.Clear();
            foreach (var kv in _equipmentByCharacter)
            {
                var eq = kv.Value;
                saveData.inventory.equipment.Add(new CharacterEquipmentSaveEntry
                {
                    type      = kv.Key.ToString(),
                    rightHand = eq.rightHand,
                    leftHand  = eq.leftHand,
                    head      = eq.head,
                    chest     = eq.chest,
                    pants     = eq.pants,
                    shoes     = eq.shoes,
                    gloves    = eq.gloves
                });
            }
        }

        public void ImportSaveData(GameSaveData saveData)
        {
            _applyStartingEquipmentOnSeed = false;
            _pendingNewGameStartingInventory = false;
            RestoreGold(saveData.inventory.gold);
            _pendingLoad = saveData.inventory;

            // ItemDatabase가 이미 로드된 경우 즉시 복원
            if (ItemManager.Instance.IsItemDBLoaded)
                ApplyPendingLoad();
        }

        public void ResetForNewGame()
        {
            _pendingLoad = null;
            ClearItems();
            _equipmentByCharacter.Clear();
            _applyStartingEquipmentOnSeed = true;
            _pendingNewGameStartingInventory = false;
            RestoreGold(0);

            if (_startingInventory != null)
                ApplyStartingInventoryForNewGame();
            else
                _pendingNewGameStartingInventory = true;
        }

        /// <summary>
        /// ItemManager가 DB 로드 완료 후 호출한다.
        /// pending 세이브 데이터가 있으면 복원하고, 없으면 테스트 아이템을 채운다.
        /// </summary>
        public void OnItemDatabaseReady()
        {
            if (_pendingLoad != null)
                ApplyPendingLoad();
        }

        private void ApplyPendingLoad()
        {
            ClearItems();
            _nextInventorySlotKey = int.MaxValue;
            foreach (var entry in _pendingLoad.items ?? new System.Collections.Generic.List<ItemSaveEntry>())
            {
                RestoreItem(entry.itemId, entry.count, entry.slotKey,
                    entry.enhancementLevel, entry.growthAttributeRolls);
            }

            // 캐릭터별 장비 복원.
            // 로드 중에는 기본 장비 시딩을 막지만, 복원 완료 뒤 새로 생성되는 캐릭터 엔트리는 기본값을 다시 시딩한다.
            _equipmentByCharacter.Clear();
            foreach (var e in _pendingLoad.equipment ?? new System.Collections.Generic.List<CharacterEquipmentSaveEntry>())
            {
                if (!CharacterActorTypeUtility.TryParsePersistentName(
                        e.type,
                        out CharacterActorType type))
                    continue;

                _equipmentByCharacter[type] = new CharacterEquipment
                {
                    rightHand = e.rightHand,
                    leftHand  = e.leftHand,
                    head      = e.head,
                    chest     = e.chest,
                    pants     = e.pants,
                    shoes     = e.shoes,
                    gloves    = e.gloves
                };
            }

            _pendingLoad = null;
            _applyStartingEquipmentOnSeed = true;

            // 장착 데이터는 레지스트리에만 복원한다. 외형은 캐릭터 모델 기본 장비를 유지한다.
            SyncActiveCharacterVisual(ActiveCharacterType);
        }

        #endregion
    }
}
