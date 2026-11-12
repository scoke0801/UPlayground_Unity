using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.EnumType;

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

        [Header("World")]
        [Tooltip("바닥 드롭 시 아이콘 메달 대신 보여 줄 3D 모델. 비어 있으면 메달을 쓴다. 콜라이더·스크립트는 표시 시 제거된다.")]
        public GameObject worldPrefab;
    }


    // 아이템 
    [System.Serializable]
    public struct EquipmentGrowthAttributeRoll
    {
        [Tooltip("장비 획득 시 확정된 안정 Attribute ID")]
        [AttributeIdSelector]
        public string attributeId;
        [Min(1)] public int rank;

        public AttributeId AttributeId => new(attributeId);
    }

    [System.Serializable]
    public class ItemInstance
    {
        public int count;
        public ItemSO data;

        public int inventorySlotKey;

        [Tooltip("장비 강화 레벨. 0이면 미강화(표시 안 함).")]
        public int enhancementLevel;

        [Tooltip("장비 획득 시 확정되는 독립 Attribute 옵션과 랭크.")]
        public System.Collections.Generic.List<EquipmentGrowthAttributeRoll> growthAttributeRolls = new();
    }
}
