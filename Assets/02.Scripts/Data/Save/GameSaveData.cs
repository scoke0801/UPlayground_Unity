using System;
using System.Collections.Generic;

namespace UPlayGround.Data.Save
{
    [Serializable]
    public class GameSaveData
    {
        public string saveVersion = "1.0";
        public string saveDateTime;
        public InventorySaveData inventory = new InventorySaveData();
        public StorySaveData story = new StorySaveData();
        public FlagSaveData flags = new FlagSaveData();
        public RecipeSaveData recipe = new RecipeSaveData();
        public QuestSaveData quest = new QuestSaveData();
    }

    [Serializable]
    public class InventorySaveData
    {
        public int gold;
        public List<ItemSaveEntry> items = new List<ItemSaveEntry>();
    }

    [Serializable]
    public class ItemSaveEntry
    {
        public int itemId;
        public int count;
        public int slotKey;
    }

    [Serializable]
    public class StorySaveData
    {
        public int progress;
        public List<string> completedStories = new List<string>();
    }

    [Serializable]
    public class FlagSaveData
    {
        // Dictionary<string, bool>를 그대로 직렬화 (Newtonsoft 지원)
        public Dictionary<string, bool> flags = new Dictionary<string, bool>();
    }

    [Serializable]
    public class RecipeSaveData
    {
        public List<int> unlockedRecipeIDs = new List<int>();
        // recipeID → 제작 횟수
        public Dictionary<int, int> craftCounts = new Dictionary<int, int>();
        // monsterID → 처치 횟수
        public Dictionary<int, int> monsterKills = new Dictionary<int, int>();
    }

    // ──────────────────────────────────────────────────────────
    // Quest

    [Serializable]
    public class QuestSaveData
    {
        /// <summary> 완료된 퀘스트 ID 목록 </summary>
        public List<string> completedQuestIds = new List<string>();

        /// <summary> 현재 진행 중인 퀘스트 상태 목록 </summary>
        public List<ActiveQuestSaveEntry> activeQuests = new List<ActiveQuestSaveEntry>();

        /// <summary> HUD에 추적 중인 퀘스트 ID </summary>
        public string trackedQuestId;

        /// <summary> 플레이어가 HUD 퀘스트 추적을 수동 해제했는지 여부 </summary>
        public bool questTrackingSuppressed;
    }

    [Serializable]
    public class ActiveQuestSaveEntry
    {
        public string questId;
        /// <summary> objectiveId → 현재 진행 카운트 </summary>
        public Dictionary<string, int> objectiveProgress = new Dictionary<string, int>();
    }
}
