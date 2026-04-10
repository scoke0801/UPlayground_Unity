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
}
