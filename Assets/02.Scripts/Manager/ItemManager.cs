using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.U2D;

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
            if (randomValue <= itemDropList[i].value)
            {
                itemList.Add(itemDropList[i].itemData);
            }
        }

        return itemList;
    }
}