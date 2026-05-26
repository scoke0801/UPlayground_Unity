namespace UPlayGround.Data.EnumType
{
    public enum ItemType
    {
        NONE = 0,
        EQUIPMENT,
        CONSUMABLE,
        OTHERS,
    }
    
    public static class ItemTypeExtensions
    {
        public static string ToDisplayString(this ItemType itemType)
        {
            return itemType switch
            {
                ItemType.NONE => "없음",
                ItemType.EQUIPMENT => "장비",
                ItemType.CONSUMABLE => "소비 아이템",
                ItemType.OTHERS => "기타",
                _ => "알 수 없음"
            };
        }
    }
    
    public enum ItemRarity
    {
        NONE = 0,
        COMMON,
        UNCOMMON,
        RARE,
        UNIQUE,
        LEGENDARY,
    }
    
    
    public static class ItemRarityExtensions
    {
        public static string ToDisplayString(this ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.NONE => "없음",
                ItemRarity.COMMON => "일반",
                ItemRarity.UNCOMMON => "고급",
                ItemRarity.RARE => "희귀",
                ItemRarity.UNIQUE => "유니크",
                ItemRarity.LEGENDARY => "전설",
                _ => "알 수 없음"
            };
        }

        public static string ToKeyString(this ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.NONE => "None",
                ItemRarity.COMMON => "Common",
                ItemRarity.UNCOMMON => "Uncommon",
                ItemRarity.RARE => "Rare",
                ItemRarity.UNIQUE => "Unique",
                ItemRarity.LEGENDARY => "Legendary",
                _ => "Unknown"
            };
        }
    }
}