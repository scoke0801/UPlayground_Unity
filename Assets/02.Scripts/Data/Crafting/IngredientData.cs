using System;
using UnityEngine;

namespace UPlayGround.Data.Crafting
{
    /// <summary>
    /// 레시피에 필요한 재료 1개
    /// recipeID로 어느 레시피에 속하는지 식별
    /// </summary>
    [Serializable]
    public class IngredientData
    {
        [Tooltip("어느 레시피에 속하는 재료인지")]
        public int recipeID;

        [Tooltip("필요한 아이템의 ItemSO.itemId")]
        public int ingredientItemID;

        [Tooltip("1회 제작에 필요한 수량")]
        public int requiredQuantity = 1;
    }
}
