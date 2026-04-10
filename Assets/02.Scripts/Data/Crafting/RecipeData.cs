using System;
using UnityEngine;

namespace UPlayGround.Data.Crafting
{
    /// <summary>
    /// 레시피 1개의 기본 정보
    /// </summary>
    [Serializable]
    public class RecipeData
    {
        [Header("기본 정보")]
        public int recipeID;
        public string recipeName;
        public string description;

        [Header("결과물")]
        public int resultItemID;
        public int resultQuantity = 1;

        [Header("비용")]
        public CostType costType = CostType.Gold;
        public int costAmount = 0;

        [Header("제작 설정")]
        public float castTimeSeconds = 2f;
        public CraftingCategory category;

        [Header("디버그")]
        [Tooltip("true면 조건 없이 처음부터 언락")]
        public bool isDebugUnlocked = false;
    }

    public enum CostType
    {
        Free = 0,   // 비용 없음
        Gold = 1,   // 골드 소모
    }

    public enum CraftingCategory
    {
        Consumable = 0,  // 소비 아이템 (포션, 음식)
        Equipment  = 1,  // 장비 (무기, 방어구)
        Material   = 2,  // 재료 (강화 부품)
        Special    = 3,  // 특수 아이템
    }
}
