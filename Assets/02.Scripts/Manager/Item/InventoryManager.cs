using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UPlayGround.Data.Path;

namespace UPlayGround.Manager
{
    public class InventoryManager : BaseManager<InventoryManager>, IManager
    {
        private Dictionary<int, ItemInstance> _itemPair = new Dictionary<int, ItemInstance>();

        public Dictionary<int, ItemInstance> ItemDict => _itemPair;

        // [TODO] Config 데이터로 별도 분리 필요
        public float MaxWeight => 3000.0f;

        public void Init()
        {
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

        public void AddItem(int itemId, ItemInstance itemInstance)
        {
            if (_itemPair.ContainsKey(itemId) == false)
            {
                _itemPair.TryAdd(itemId, itemInstance);

                // TODO...  인벤토리 슬롯 지정 필요
            }
            else
            {
                _itemPair[itemId].count += 1;
            }
        }

        public void RemoveItem(int itemId)
        {
            _itemPair.Remove(itemId);
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
            ItemDatabase itemDB = ItemManager.Instance.GetItemDB();
            if (itemDB == null)
            {
                return;
            }

            foreach (var itemSO in itemDB.AllItems)
            {
                AddItem(itemSO.itemId, new ItemInstance()
                {
                    count = 1,
                    data = itemSO
                });
            }
        }
    }
}