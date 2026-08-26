using System;
using System.Collections.Generic;
using UPlayGround.Data.Reward;

namespace UPlayGround.Data.Quest
{
    /// <summary>퀘스트 직렬화 형식을 보존하고 지급 시 공용 보상 묶음으로 변환한다.</summary>
    [Serializable]
    public class QuestRewardData
    {
        public int gold;
        public long exp;
        public List<QuestItemReward> items = new();

        public bool IsEmpty => gold == 0
                               && exp == 0
                               && (items == null || items.Count == 0);

        /// <summary>기존 퀘스트 데이터를 공용 보상 서비스 입력으로 복사한다.</summary>
        public RewardData ToRewardData()
        {
            var reward = new RewardData
            {
                gold = gold,
                exp = exp,
            };

            if (items == null)
                return reward;

            for (int i = 0; i < items.Count; i++)
            {
                QuestItemReward item = items[i];
                reward.items.Add(item == null
                    ? null
                    : new ItemRewardData
                    {
                        itemId = item.itemId,
                        count = item.count,
                    });
            }

            return reward;
        }
    }

    [Serializable]
    public class QuestItemReward
    {
        public int itemId;
        public int count;
    }
}
