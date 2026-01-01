using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemSO", menuName = "UP/SO/ItemSO")]
public class ItemSO : ScriptableObject
{
    public int itemId;
    public string itemName;
    public string itemDescription;
    public float weight;
    public ItemType itemType;
    public ItemRarity itemRarity;
}

// 아이템 
[System.Serializable]
public class ItemInstance
{
    public int count;
    public ItemSO data;

    public int inventorySlotKey;
}