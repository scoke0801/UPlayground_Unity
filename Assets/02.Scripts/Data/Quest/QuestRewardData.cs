using System;
using System.Collections.Generic;

namespace UPlayGround.Data.Quest
{
    /// <summary>
    /// 퀘스트 완료 보상 데이터
    /// </summary>
    [Serializable]
    public class QuestRewardData
    {
        public int gold;
        public long exp;
        public List<QuestItemReward> items = new List<QuestItemReward>();
    }

    [Serializable]
    public class QuestItemReward
    {
        public int itemId;
        public int count;
    }
}
