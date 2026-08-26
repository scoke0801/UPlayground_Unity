using System;
using System.Collections.Generic;

namespace UPlayGround.Data.Reward
{
    /// <summary>보상 묶음의 정적 데이터 오류를 구분한다.</summary>
    public enum RewardDataValidationResult
    {
        Valid = 0,
        NegativeGold,
        NegativeExperience,
        InvalidItem,
        DuplicateItem,
    }

    /// <summary>보상으로 지급할 아이템 ID와 수량을 정의한다.</summary>
    [Serializable]
    public class ItemRewardData
    {
        public int itemId;
        public int count;
    }

    /// <summary>퀘스트·처치·소모품이 공유하는 골드, 경험치, 아이템 보상 묶음.</summary>
    [Serializable]
    public class RewardData
    {
        public int gold;
        public long exp;
        public List<ItemRewardData> items = new();

        public bool IsEmpty => gold == 0
                               && exp == 0
                               && (items == null || items.Count == 0);

        /// <summary>서비스 상태와 무관한 보상 데이터 자체의 유효성을 검사한다.</summary>
        public RewardDataValidationResult Validate()
        {
            if (gold < 0)
                return RewardDataValidationResult.NegativeGold;
            if (exp < 0)
                return RewardDataValidationResult.NegativeExperience;
            if (items == null || items.Count == 0)
                return RewardDataValidationResult.Valid;

            var itemIds = new HashSet<int>();
            for (int i = 0; i < items.Count; i++)
            {
                ItemRewardData item = items[i];
                if (item == null || item.itemId <= 0 || item.count <= 0)
                    return RewardDataValidationResult.InvalidItem;
                if (!itemIds.Add(item.itemId))
                    return RewardDataValidationResult.DuplicateItem;
            }

            return RewardDataValidationResult.Valid;
        }
    }
}

