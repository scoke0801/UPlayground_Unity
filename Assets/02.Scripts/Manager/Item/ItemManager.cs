using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UPlayGround.Data.Path;
using UPlayGround.Data.Item;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.Cycle;

namespace UPlayGround.Manager
{
    public class ItemManager : BaseManager<ItemManager>, IManager, IAsyncInitializableManager,
        IItemService
    {
        private const string ITEM_DATABASE_PATH = "ItemDatabase";
        [SerializeField] private ItemDatabase _itemDatabase;
        private readonly IItemDropRandom _fallbackDropRandom = new SystemItemDropRandom();
        private IItemDropRandom _cycleDropRandom;
        private int _cycleDropRandomIndex;
        private int _cycleDropRandomSeed;

        public bool IsItemDBLoaded { get; set; } = false;

        public ItemDatabase GetItemDB() => _itemDatabase;
        
        public void Init()
        {
        }

        public UniTask InitializeAsync(CancellationToken cancellationToken) =>
            LoadItemDatabaseAsync(cancellationToken);

        public void AfterInit()
        {
            
        }

        public void Dispose()
        {
            _itemDatabase = null;
            IsItemDBLoaded = false;
            ResetCycleDropRandom();
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

        public List<ItemInstance> GetDropItemList(List<ItemDropList> itemDropList)
        {
            IItemDropRandom random = ResolveDropRandom(out ItemDropRollContext context);
            return ItemDropResolver.Resolve(
                itemDropList,
                null,
                random,
                context);
        }

        public List<ItemInstance> GetDropItemList(EnemyDropTableSO dropTable)
        {
            if (dropTable == null)
                return new List<ItemInstance>();

            IItemDropRandom random = ResolveDropRandom(out ItemDropRollContext context);
            return ItemDropResolver.Resolve(
                dropTable.dropItems,
                dropTable.weightedGroups,
                random,
                context);
        }

        private IItemDropRandom ResolveDropRandom(out ItemDropRollContext context)
        {
            ICycleRunReaderService cycle = Services.Get<ICycleRunReaderService>();
            bool isCycleActive = cycle?.IsActive ?? false;
            context = new ItemDropRollContext(isCycleActive);
            if (!isCycleActive)
            {
                ResetCycleDropRandom();
                return _fallbackDropRandom;
            }

            if (_cycleDropRandom == null
                || _cycleDropRandomIndex != cycle.CycleIndex
                || _cycleDropRandomSeed != cycle.Seed)
            {
                _cycleDropRandom = new SystemItemDropRandom(
                    cycle.CreateRandom(CycleRandomStream.Reward));
                _cycleDropRandomIndex = cycle.CycleIndex;
                _cycleDropRandomSeed = cycle.Seed;
            }

            return _cycleDropRandom;
        }

        private void ResetCycleDropRandom()
        {
            _cycleDropRandom = null;
            _cycleDropRandomIndex = 0;
            _cycleDropRandomSeed = 0;
        }

        public static ItemInstance GET_ITEM(ItemSO itemData, int count)
        {
            ItemInstance itemInstance = new ItemInstance();
            itemInstance.data = itemData;
            itemInstance.count = count;

            return itemInstance;
        }

        public ItemSO GetItemData(ItemIdType itemKey) => GetItemData((int)itemKey);

        public ItemSO GetItemData(int itemKey)
        {
            if (_itemDatabase == null)
            {
                return null;
            }

            return _itemDatabase.GetItemById(itemKey);
        }
        
        private async UniTask LoadItemDatabaseAsync(CancellationToken cancellationToken)
        {
            try
            {
                _itemDatabase = await AssetManager.Instance.LoadGlobalAsync<ItemDatabase>(
                    ITEM_DATABASE_PATH,
                    nameof(ItemManager),
                    cancellationToken);

                IsItemDBLoaded = true;
                _itemDatabase.Initialize();

                // 세이브 데이터가 있으면 복원, 없으면 테스트 아이템 생성
                InventoryManager.Instance.OnItemDatabaseReady();

                Debug.Log($"[ItemManager] ItemDatabase 로드 완료");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ItemManager] ItemDatabase 로드 실패: {e.Message}");
                throw;
            }
        }

    }
}
