using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Save
{
    [Serializable]
    public class GameSaveData
    {
        public string saveVersion = "2.0";
        public string saveDateTime;
        public InventorySaveData inventory = new InventorySaveData();
        public StorySaveData story = new StorySaveData();
        public FlagSaveData flags = new FlagSaveData();
        public RecipeSaveData recipe = new RecipeSaveData();
        public QuestSaveData quest = new QuestSaveData();
        public PartySaveData party = new PartySaveData();
        public WorldStateSaveData world = new WorldStateSaveData();
    }

    // ──────────────────────────────────────────────────────────
    // 직렬화용 벡터/회전 (Unity Vector3/Quaternion은 Newtonsoft 직렬화 시
    // 부가 프로퍼티 순환참조 문제가 있어 x/y/z/w만 보관한다.)

    [Serializable]
    public struct SerializableVector3
    {
        public float x, y, z;

        public SerializableVector3(Vector3 v) { x = v.x; y = v.y; z = v.z; }
        public Vector3 ToVector3() => new Vector3(x, y, z);
    }

    [Serializable]
    public struct SerializableQuaternion
    {
        public float x, y, z, w;

        public SerializableQuaternion(Quaternion q) { x = q.x; y = q.y; z = q.z; w = q.w; }
        public Quaternion ToQuaternion() => new Quaternion(x, y, z, w);
    }

    // ──────────────────────────────────────────────────────────
    // World (맵별 처치된 몬스터 GUID 집합)

    [Serializable]
    public class WorldStateSaveData
    {
        // mapId → 처치된 SceneEntityId GUID 목록
        public Dictionary<string, List<string>> killedMonsters = new Dictionary<string, List<string>>();
    }

    // ──────────────────────────────────────────────────────────
    // Party (보유/출전 + 캐릭터별 레벨·경험치)

    [Serializable]
    public class PartySaveData
    {
        public List<PartyMemberSaveEntry> members = new List<PartyMemberSaveEntry>();
        public List<string> roster = new List<string>();
        public List<string> battleOrder = new List<string>();
        public int activeIndex;

        /// <summary> 캐릭터별 현재 체력 (액티브/벤치 공통). </summary>
        public List<CharacterHpEntry> characterHealth = new List<CharacterHpEntry>();

        // ── 위치/씬 정보 ──
        /// <summary> 로드 시 진입할 씬 에셋명 (SceneName). </summary>
        public string loadSceneName;
        /// <summary> 저장 당시 맵 식별자 (SceneContext.MapID). 슬롯 표시·월드 상태 키. </summary>
        public string mapId;
        public SerializableVector3 playerPos;
        public SerializableQuaternion playerRot;
        /// <summary> 위치/씬 정보가 유효한지(인게임에서 저장됐는지). </summary>
        public bool hasLocation;
    }

    [Serializable]
    public class PartyMemberSaveEntry
    {
        public string type;
        public int level;
        public long exp;
    }

    [Serializable]
    public class CharacterHpEntry
    {
        public string type;
        public float currentHp;
        public float skillGauge;
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
        // monsterID(레거시 숫자 ID) → 처치 횟수
        public Dictionary<int, int> monsterKills = new Dictionary<int, int>();
        // ActorId(문자열) → 처치 횟수
        public Dictionary<string, int> monsterKillsByActorId = new Dictionary<string, int>();
        // itemID → 누적 획득 수량
        public Dictionary<int, int> itemCollectCounts = new Dictionary<int, int>();
    }

    // ──────────────────────────────────────────────────────────
    // Quest

    [Serializable]
    public class QuestSaveData
    {
        /// <summary> 완료된 퀘스트 ID 목록 </summary>
        public List<string> completedQuestIds = new List<string>();

        /// <summary> 실패한 퀘스트 ID 목록 </summary>
        public List<string> failedQuestIds = new List<string>();

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
