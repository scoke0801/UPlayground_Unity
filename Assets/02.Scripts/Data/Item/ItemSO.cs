using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;

namespace UPlayGround.Data.Item
{
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
    public struct EquipmentGrowthAttributeRoll
    {
        public GrowthAttributeType attributeType;
        [Min(1)] public int rank;
    }

    [System.Serializable]
    public class ItemInstance
    {
        public int count;
        public ItemSO data;

        public int inventorySlotKey;

        [Tooltip("장비 강화 레벨. 0이면 미강화(표시 안 함).")]
        public int enhancementLevel;

        [Tooltip("장비 획득 시 확정된 성장 능력치. 장착 중 캐릭터 성장 랭크와 합산된다.")]
        public System.Collections.Generic.List<EquipmentGrowthAttributeRoll> growthAttributeRolls = new();
    }
}
