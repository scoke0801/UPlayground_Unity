using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UPlayGround.Data.Path;

namespace UPlayGround.Manager
{
    public class ItemManager : BaseManager<ItemManager>, IManager
    {
        private const string ITEM_DATABASE_PATH = "ItemDatabase";
        [SerializeField] private ItemDatabase _itemDatabase;

        public bool IsItemDBLoaded { get; set; } = false;

        public ItemDatabase GetItemDB() => _itemDatabase;
        
        public void Init()
        {
            LoadItemDatabase();
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

        public List<ItemInstance> GetDropItemList(List<ItemDropList> itemDropList)
        {
            List<ItemInstance> itemList = new List<ItemInstance>();
            for (int i = 0; i < itemDropList.Count; ++i)
            {
                float randomValue = Random.Range(0.0f, 100.0f);
                if (randomValue <= itemDropList[i].rate)
                {
                    ItemInstance itemInstance = new ItemInstance();
                    itemInstance.count = Random.Range(1, itemDropList[i].maximumDropCount);
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

        public ItemSO GetItemData(int itemKey)
        {
            if (_itemDatabase == null)
            {
                return null;
            }

            return _itemDatabase.GetItemById(itemKey);
        }
        
        private async void LoadItemDatabase()
        {
            var handle = Addressables.LoadAssetAsync<ItemDatabase>(ITEM_DATABASE_PATH);

            try
            {
                _itemDatabase = await handle.Task;

                if (_itemDatabase == null)
                {
                    Debug.LogError(
                        $"[ItemManager] ItemDatabase를 '{ITEM_DATABASE_PATH}' 경로에서 찾을 수 없습니다.");
                    return;
                }

                IsItemDBLoaded = true;
                _itemDatabase.Initialize();

                InventoryManager.Instance.MakeTestItems();
                
                Debug.Log($"[ItemManager] ItemDatabase 로드 완료");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ItemManager] ItemDatabase 로드 실패: {e.Message}");
            }
        }

    }
}