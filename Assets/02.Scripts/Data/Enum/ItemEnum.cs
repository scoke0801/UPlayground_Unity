using UnityEngine;

namespace UPlayGround.Data.EnumType
{
    public enum ItemType
    {
        NONE = 0,
        EQUIPMENT,
        CONSUMABLE,
        OTHERS,
        // 아래는 인벤토리 카테고리 세분화를 위해 추가 (기존 값 순서 유지)
        MATERIAL,   // 재료
        QUEST,      // 퀘스트 아이템
        IMPORTANT,  // 중요 아이템
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
                ItemType.MATERIAL => "재료",
                ItemType.QUEST => "퀘스트",
                ItemType.IMPORTANT => "중요 아이템",
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

        /// <summary> 등급별 대표 색(슬롯 테두리·상세 라벨 공용). </summary>
        public static Color ToColor(this ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.COMMON    => Color.white,
                ItemRarity.UNCOMMON  => new Color(0.35f, 0.90f, 0.45f),
                ItemRarity.RARE      => new Color(0.35f, 0.60f, 1.00f),
                ItemRarity.UNIQUE    => new Color(0.85f, 0.45f, 1.00f),
                ItemRarity.LEGENDARY => new Color(1.00f, 0.65f, 0.20f),
                _                    => Color.clear
            };
        }
    }
}