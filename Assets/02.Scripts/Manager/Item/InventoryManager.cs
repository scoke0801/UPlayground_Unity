using System.Collections.Generic;
using UnityEngine;
public class InventoryManager : BaseManager<InventoryManager>, IManager
{
    Dictionary<int, ItemInstance> _itemPair = new Dictionary<int, ItemInstance>();
    
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
    
    public void AddItem(int itemId, ItemInstance itemInstance)
    {
        if (_itemPair.ContainsKey(itemId))
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

    public bool HasItem(int itemId)
    {
        return _itemPair.ContainsKey(itemId);
    }
    
    public ItemInstance GetItem(int itemId)
    {
        _itemPair.TryGetValue(itemId, out ItemInstance item);
        return item;
    }
}