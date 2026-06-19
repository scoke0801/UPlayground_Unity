using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UPlayGround.Data.Path;
using UPlayGround.Data.Item;

namespace UPlayGround.Manager
{
    public class ItemManager : BaseManager<ItemManager>, IManager, IAsyncInitializableManager
    {
        private const string ITEM_DATABASE_PATH = "ItemDatabase";
        [SerializeField] private ItemDatabase _itemDatabase;

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
            List<ItemInstance> itemList = new List<ItemInstance>();
            for (int i = 0; i < itemDropList.Count; ++i)
            {
                float randomValue = UnityEngine.Random.Range(0.0f, 100.0f);
                if (randomValue <= itemDropList[i].rate)
                {
                    ItemInstance itemInstance = new ItemInstance();
                    itemInstance.count = UnityEngine.Random.Range(1, itemDropList[i].maximumDropCount);
                    itemInstance.data = itemDropList[i].itemData;

                    itemList.Add(itemInstance);
                }
            }

            return itemList;
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
