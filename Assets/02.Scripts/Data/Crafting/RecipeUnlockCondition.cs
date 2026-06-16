using System;
using UnityEngine;

namespace UPlayGround.Data.Crafting
{
    /// <summary>
    /// 레시피 언락 조건
    /// conditionType에 따라 conditionValue/conditionValue2의 의미가 달라짐
    /// </summary>
    [Serializable]
    public class RecipeUnlockCondition
    {
        [Tooltip("어느 레시피의 언락 조건인지")]
        public int recipeID;

        public UnlockConditionType conditionType;

        [Tooltip("조건 값 1 (아이템ID, 레시피ID 등). MonsterKill의 숫자 ID는 레거시 호환용.")]
        public int conditionValue;

        [Tooltip("조건 값 2 (수량, 횟수 등)")]
        public int conditionValue2;

        [Tooltip("MonsterKill 목표의 MonsterActor.ActorId. 지정 시 conditionValue보다 우선한다.")]
        public string conditionStringValue;
    }

    /// <summary>
    /// conditionValue / conditionValue2 / conditionStringValue 용도:
    ///   None        — 없음 (처음부터 언락)
    ///   MonsterKill — stringValue=ActorId(우선), value=레거시 숫자 몬스터ID, value2=처치 수 (0이면 1회)
    ///   ItemCollect — value=아이템ID, value2=수집 수량
    ///   ItemHave    — value=아이템ID, value2=소지 수량
    ///   RecipeCraft — value=레시피ID, value2=제작 횟수
    /// </summary>
    public enum UnlockConditionType
    {
        None        = 0,
        MonsterKill = 1,
        ItemCollect = 2,
        ItemHave    = 3,
        RecipeCraft = 4,
    }
}
