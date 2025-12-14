using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemSO", menuName = "SO/ItemSO")]
public class ItemSO : ScriptableObject
{
    public int itemId;
    public string itemName;
    public string itemDescription;
    public ItemType itemType;
    public ItemRarity itemRarity;
}
