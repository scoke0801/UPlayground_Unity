using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Manager
{
    public class ItemManager : BaseManager<ItemManager>, IManager
    {

        public void Init()
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
    }
}