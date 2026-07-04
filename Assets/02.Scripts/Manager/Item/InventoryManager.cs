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

    /// <summary>
    /// 한 캐릭터의 장비 슬롯 상태. 각 슬롯은 itemId(-1 = 빈칸)를 보관한다.
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

        /// <summary> 해당 itemId를 장착 중인 슬롯 수(같은 아이템이 여러 슬롯에 걸릴 수 있어 카운트). </summary>
        public int CountOf(int itemId)
        {
            if (itemId < 0) return 0;
            int n = 0;
            if (rightHand == itemId) n++;
            if (leftHand  == itemId) n++;
            if (head      == itemId) n++;
            if (chest     == itemId) n++;
            if (pants     == itemId) n++;
            if (shoes     == itemId) n++;
            if (gloves    == itemId) n++;
            return n;
        }

        /// <summary> 지정 itemId를 낀 첫 슬롯을 비우고 그 슬롯을 반환. 없으면 false. </summary>
        public bool TryRemoveFirst(int itemId, out EquipPosition freedSlot)
        {
            foreach (var slot in AllSlots)
            {
                if (Get(slot) == itemId)
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

    public class InventoryManager : BaseManager<InventoryManager>, IManager, ISaveable
    {
        private Dictionary<int, ItemInstance> _itemPair = new Dictionary<int, ItemInstance>();

        public Dictionary<int, ItemInstance> ItemDict => _itemPair;

        // 캐릭터별 장비 레지스트리 — 활성/벤치 공통 단일 소스. 활성 캐릭터의 PlayerEquipment는 이 값을 시각 반영만 한다.
        private readonly Dictionary<CharacterActorType, CharacterEquipment> _equipmentByCharacter = new();

        /// <summary> 파티원 장비 변경 시 발행 (UI 갱신용). </summary>
        public event System.Action OnPartyEquipmentChanged;

        // [TODO] Config 데이터로 별도 분리 필요
        public float MaxWeight => 3000.0f;

        /// <summary> 인벤토리 최대 슬롯 수 (UI 용량 표시용). </summary>
        public int MaxSlots => 120;

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
        /// (RefreshForCharacter에서 모델의 StartEquipItems를 넘겨 호출 — GameObjectManager 의존 회피)
        /// </summary>
        public void SeedCharacterEquipmentIfAbsent(CharacterActorType c, IReadOnlyList<EquipmentSO> startItems)
        {
            if (c == CharacterActorType.None || _equipmentByCharacter.ContainsKey(c))
                return;

            var eq = new CharacterEquipment();
            if (startItems != null)
            {
                foreach (var so in startItems)
                    if (so != null)
                        eq.Set(so.equipSlot, so.itemId);
            }
            _equipmentByCharacter[c] = eq;
        }

        /// <summary> 활성 반영용 주/보조 무기 스냅샷. </summary>
        public (int mainKey, int subKey) GetActiveWeaponSnapshot(CharacterActorType c)
        {
            var eq = GetOrSeedEntry(c);
            return eq != null ? (eq.rightHand, eq.leftHand) : (-1, -1);
        }

        // 해당 캐릭터 모델의 PlayerEquipment.StartEquipItems로 기본 장비를 채운다.
        // (모델은 비활성이어도 계층에 존재하므로 언제든 조회 가능. itemId만 읽으므로 item DB 불필요.)
        private void SeedFromModelStartItems(CharacterActorType c, CharacterEquipment eq)
        {
            var player = GameObjectManager.Instance?.Player;
            var swap = player != null ? player.GetComponent<PlayerSwapBehaviour>() : null;
            var model = swap?.GetModelData(c);
            var pe = model != null ? model.GetComponentInChildren<PlayerEquipment>(true) : null;
            if (pe == null || pe.StartEquipItems == null) return;

            foreach (var so in pe.StartEquipItems)
            {
                if (so != null)
                    eq.Set(so.equipSlot, so.itemId);
            }
        }

        private int OwnedCount(int itemId) =>
            _itemPair.TryGetValue(itemId, out var it) ? it.count : 0;

        /// <summary> 파티 전체에서 해당 itemId가 장착된 총 개수. </summary>
        public int GetEquippedCount(int itemId)
        {
            int n = 0;
            foreach (var eq in _equipmentByCharacter.Values)
                n += eq.CountOf(itemId);
            return n;
        }

        /// <summary> 아직 장착에 쓰지 않은 여유 수량. </summary>
        public int GetFreeCount(int itemId) => OwnedCount(itemId) - GetEquippedCount(itemId);

        public int GetEquippedItem(CharacterActorType c, EquipPosition slot) =>
            GetOrSeedEntry(c)?.Get(slot) ?? -1;

        /// <summary> 해당 itemId를 장착 중인 캐릭터 목록. </summary>
        public List<CharacterActorType> GetEquippingCharacters(int itemId)
        {
            var list = new List<CharacterActorType>();
            foreach (var kv in _equipmentByCharacter)
                if (kv.Value.CountOf(itemId) > 0)
                    list.Add(kv.Key);
            return list;
        }

        // 보조 무기(왼손)는 대상 캐릭터의 "유효 주 무기 타입"과 같을 때만 호환된다.
        private bool IsSubWeaponCompatible(CharacterActorType c, WeaponType subWeaponType)
        {
            if (subWeaponType == WeaponType.NoWeapon) return false;
            return GetCharacterMainWeaponType(c) == subWeaponType;
        }

        // 캐릭터의 유효 주 무기 타입: 장착된 주 무기 아이템 타입 → 없으면 모델의 빌트인 기본 무기 타입.
        // (검+방패처럼 주 무기가 빌트인인 캐릭터도 보조(방패) 장착을 허용하기 위함.)
        private WeaponType GetCharacterMainWeaponType(CharacterActorType c)
        {
            var eq = GetOrSeedEntry(c);
            if (eq != null && eq.rightHand >= 0 &&
                ItemManager.Instance?.GetItemData(eq.rightHand) is EquipmentSO mainEq)
            {
                return mainEq.weaponType;
            }

            return GetModelDefaultWeaponType(c);
        }

        private WeaponType GetModelDefaultWeaponType(CharacterActorType c)
        {
            var player = GameObjectManager.Instance?.Player;
            var swap = player != null ? player.GetComponent<PlayerSwapBehaviour>() : null;
            var model = swap?.GetModelData(c);
            return model != null ? model.defaultWeaponType : WeaponType.NoWeapon;
        }

        // 주 무기는 캐릭터 모델에 지정된 기본 주무기 타입과 같은 타입만 장착할 수 있다.
        private bool IsMainWeaponCompatible(CharacterActorType c, WeaponType weaponType)
        {
            if (weaponType == WeaponType.NoWeapon) return false;
            return GetModelDefaultWeaponType(c) == weaponType;
        }

        // 주 무기 슬롯이 바뀐 뒤, 현재 보조 무기가 "유효 주 무기 타입"과 맞지 않으면 해제한다.
        // (빌트인 주 무기 폴백 포함 — 임시 주 무기를 벗어도 빌트인과 호환되면 보조를 유지한다.)
        private void EnsureSubCompatibility(CharacterActorType c, CharacterEquipment eq)
        {
            if (eq.leftHand < 0) return;

            WeaponType mainType = GetCharacterMainWeaponType(c);
            bool keep = mainType != WeaponType.NoWeapon &&
                        ItemManager.Instance?.GetItemData(eq.leftHand) is EquipmentSO subEq &&
                        subEq.weaponType == mainType;
            if (!keep)
                eq.leftHand = -1;
        }

        // 여유분이 없을 때, 해당 itemId를 낀 다른 캐릭터(roster 순서 우선)에게서 해제해 한 copy를 확보한다.
        private bool TransferFromAnotherOwner(int itemId, CharacterActorType requester)
        {
            var roster = PartyManager.Instance?.Roster;
            if (roster != null)
            {
                foreach (var owner in roster)
                {
                    if (owner == requester) continue;
                    if (_equipmentByCharacter.TryGetValue(owner, out var oeq) &&
                        oeq.TryRemoveFirst(itemId, out var freed))
                    {
                        if (freed == EquipPosition.RightHand)
                            EnsureSubCompatibility(owner, oeq);
                        SyncActiveCharacterVisual(owner);
                        return true;
                    }
                }
            }

            // roster에 없는 소유자(예외적)까지 폴백 탐색
            foreach (var kv in _equipmentByCharacter)
            {
                if (kv.Key == requester) continue;
                if (kv.Value.TryRemoveFirst(itemId, out var freed))
                {
                    if (freed == EquipPosition.RightHand)
                        EnsureSubCompatibility(kv.Key, kv.Value);
                    SyncActiveCharacterVisual(kv.Key);
                    return true;
                }
            }
            return false;
        }

        // 대상 캐릭터가 활성이면 무기 외형을 레지스트리 값대로 동기화(방어구는 외형 반영 없음).
        private void SyncActiveCharacterVisual(CharacterActorType c)
        {
            if (c == CharacterActorType.None || c != ActiveCharacterType) return;

            var pe = GameObjectManager.Instance?.Player?.GetPlayerEquipment();
            if (pe == null) return;

            var eq = GetOrSeedEntry(c);
            pe.ApplyEquipmentSnapshot(eq.rightHand, eq.leftHand);
        }

        public InventoryActionResult TryEquipItem(int itemId) => TryEquipItem(ActiveCharacterType, itemId);

        public InventoryActionResult TryEquipItem(CharacterActorType c, int itemId)
        {
            if (c == CharacterActorType.None)
                return InventoryActionResult.Failed;
            if (!_itemPair.TryGetValue(itemId, out var item) || item.data == null)
                return InventoryActionResult.InvalidItem;
            if (item.data is not EquipmentSO equipData)
                return InventoryActionResult.NotEquippable;
            if (!CanEquipItem(c, equipData))
                return InventoryActionResult.NotEquippable;

            var eq = GetOrSeedEntry(c);
            EquipPosition slot = equipData.equipSlot;

            // 같은 슬롯에 이미 같은 아이템이면 무시
            if (eq.Get(slot) == itemId)
                return InventoryActionResult.Success;

            // 여유분이 없으면 다른 소유자에게서 이동(transfer). 실패 시 기존 슬롯 상태를 건드리지 않는다.
            if (GetFreeCount(itemId) <= 0 && !TransferFromAnotherOwner(itemId, c))
                return InventoryActionResult.Failed;

            eq.Set(slot, itemId);

            // 주 무기 교체 시 보조 무기 호환성 재검사
            if (slot == EquipPosition.RightHand)
                EnsureSubCompatibility(c, eq);

            SyncActiveCharacterVisual(c);
            OnPartyEquipmentChanged?.Invoke();
            return InventoryActionResult.Success;
        }

        public InventoryActionResult TryUnequipItem(CharacterActorType c, EquipPosition slot)
        {
            var eq = GetOrSeedEntry(c);
            if (eq == null || eq.Get(slot) < 0)
                return InventoryActionResult.Failed;

            eq.Set(slot, -1);

            // 주 무기(아이템) 해제 시 보조 무기가 유효 주 무기 타입(빌트인 폴백)과 맞지 않으면 해제
            if (slot == EquipPosition.RightHand)
                EnsureSubCompatibility(c, eq);

            SyncActiveCharacterVisual(c);
            OnPartyEquipmentChanged?.Invoke();
            return InventoryActionResult.Success;
        }

        public bool CanEquipItem(int itemId) => CanEquipItem(ActiveCharacterType, itemId);

        public bool CanEquipItem(CharacterActorType c, int itemId)
        {
            if (!_itemPair.TryGetValue(itemId, out var item) || item.data == null)
                return false;
            return item.data is EquipmentSO equipData && CanEquipItem(c, equipData);
        }

        public bool CanEquipItem(EquipmentSO equipData) => CanEquipItem(ActiveCharacterType, equipData);

        public bool CanEquipItem(CharacterActorType c, EquipmentSO equipData)
        {
            if (equipData == null || c == CharacterActorType.None)
                return false;

            switch (equipData.equipSlot)
            {
                case EquipPosition.RightHand:
                    // 주 무기: 캐릭터 고유 주무기 타입과 일치하는 장비만 장착 가능
                    return equipData.weaponType != WeaponType.NoWeapon &&
                           equipData.equipmentPrefab != null &&
                           IsMainWeaponCompatible(c, equipData.weaponType);
                case EquipPosition.LeftHand:
                    // 보조 무기: 기본 유효성 + 주 무기와 동일한 무기 타입일 때만 (쌍검↔방패 거부)
                    return equipData.weaponType != WeaponType.NoWeapon &&
                           equipData.equipmentPrefab != null &&
                           IsSubWeaponCompatible(c, equipData.weaponType);
                case EquipPosition.Head:
                case EquipPosition.Chest:
                case EquipPosition.Pants:
                case EquipPosition.Shoes:
                case EquipPosition.Gloves:
                    return ToArmorType(equipData.equipSlot) != EquipArmorType.None;
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

            if (!_itemPair.TryGetValue(itemId, out var item))
            {
                return InventoryActionResult.InvalidItem;
            }

            if (item.count < count)
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

            if (_itemPair.TryGetValue(itemId, out var existing))
            {
                // 장비는 인벤토리 슬롯 1개당 1개만 관리 — 이미 보유 중이면 수량을 늘리지 않는다(비-스택).
                if (existing.data is EquipmentSO)
                {
                    Debug.LogWarning($"[InventoryManager] 장비(ItemID {itemId})는 슬롯당 1개만 보관합니다 — 중복 획득 무시.");
                    return;
                }

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

            // 장비는 비-스택: 요청 수량과 무관하게 1개만 보관.
            int storeCount = itemData is EquipmentSO ? 1 : count;
            _itemPair[itemId] = new ItemInstance { data = itemData, count = storeCount };

            if (notifyProgress)
            {
                NotifyItemCollected(itemId, storeCount);
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
            _equipmentByCharacter.Clear();
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

            // 캐릭터별 장비 복원 (없는 캐릭터는 이후 GetOrSeedEntry가 모델 기본으로 시딩).
            _equipmentByCharacter.Clear();
            foreach (var e in _pendingLoad.equipment ?? new System.Collections.Generic.List<CharacterEquipmentSaveEntry>())
            {
                if (!System.Enum.TryParse(e.type, out CharacterActorType type) || type == CharacterActorType.None)
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

            // 파티가 이미 구성된 뒤 로드가 적용된 경우, 활성 캐릭터 외형을 복원된 장비로 재동기화.
            // (파티 구성이 이후라면 RefreshForCharacter가 로드된 레지스트리를 읽어 반영.)
            SyncActiveCharacterVisual(ActiveCharacterType);
        }

        #endregion
    }
}
