using System.Collections.Generic;
using UnityEngine;

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

    public List<ItemSO> GetDropItemList(List<ItemDropList> itemDropList)
    {
        List<ItemSO> itemList = new List<ItemSO>();
        for (int i = 0; i < itemDropList.Count; ++i)
        {
            float randomValue = Random.Range(0.0f, 100.0f);
            if (randomValue <= itemDropList[i].rate)
            {
                itemList.Add(itemDropList[i].itemData);
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