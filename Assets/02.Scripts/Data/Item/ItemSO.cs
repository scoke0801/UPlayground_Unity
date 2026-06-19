using UnityEngine;
using UPlayGround.Data.EnumType;

[CreateAssetMenu(fileName = "ItemSO", menuName = "UPlayGround/아이템/Item")]
public class ItemSO : ScriptableObject
{
    [Header("Base Data")]
    public int itemId;
    public string itemName;
    public string itemDescription;
    public float weight;
    public ItemType itemType;
    public ItemRarity itemRarity;
    public Sprite icon;
}


// 아이템 
[System.Serializable]
public class ItemInstance
{
    public int count;
    public ItemSO data;

    public int inventorySlotKey;
}