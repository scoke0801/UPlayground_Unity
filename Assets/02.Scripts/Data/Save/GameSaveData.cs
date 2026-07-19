using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Cycle;
using UPlayGround.Data.Item;

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
        public FirstTimeGuideSaveData firstTimeGuide = new FirstTimeGuideSaveData();
        public PartySaveData party = new PartySaveData();
        public WorldStateSaveData world = new WorldStateSaveData();
        public TimeSaveData time = new TimeSaveData();
        public CycleSaveData cycle = new CycleSaveData();
        public List<MonsterCodexEntrySave> monsterCodex = new List<MonsterCodexEntrySave>();
    }

    /// <summary>현재 새 게임 범위에서 누적한 몬스터 종별 도감 기록.</summary>
    [Serializable]
    public sealed class MonsterCodexEntrySave
    {
        public string actorId;
        public long killCount;
        public bool discovered;
        public int discoveredElement;
    }

    // ──────────────────────────────────────────────────────────
    // Cycle Runtime (01 단계 최소 저장 모델. 후속 스펙에서 세부 DTO 확장)

    [Serializable]
    public sealed class CycleSaveData
    {
        public int dataVersion = 1;
        public CycleRunState run = CycleRunState.CreateInactive();

        public CycleLayoutState layout;
        public List<CycleItemStack> unsettledMaterials = new List<CycleItemStack>();
        public RemainsState remains;
        public AssistProgressSaveData assists = new AssistProgressSaveData();
        public CycleHistorySaveData history = new CycleHistorySaveData();
    }

    // ──────────────────────────────────────────────────────────
    // Time (플레이 시간 + 인게임 시계)

    [Serializable]
    public class TimeSaveData
    {
        public float totalPlaySeconds;

        /// <summary> 누적 인게임 분. 음수면 미기록(구버전 세이브)으로 간주한다. </summary>
        public float totalGameMinutes = -1f;
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
        /// <summary>Humanoid 속성을 새 게임 단위로 결정하는 저장 시드.</summary>
        public int elementRandomSeed;

        // mapId → 영구 처치된 SceneEntityId GUID 목록 (보스/합류 몬스터 등 재스폰 제외 대상).
        // 구버전 세이브의 killedMonsters는 전부 영구 처치로 읽는다(호환).
        public Dictionary<string, List<string>> killedMonsters = new Dictionary<string, List<string>>();

        /// <summary> 일반 필드 몬스터의 재스폰 상태 목록. </summary>
        public List<MonsterRespawnState> respawnStates = new List<MonsterRespawnState>();

        /// <summary> 맵별 소모된 채집/파괴형 인터랙션 오브젝트 GUID 목록. </summary>
        public Dictionary<string, List<string>> consumedInteractables = new Dictionary<string, List<string>>();
    }

    /// <summary>
    /// 배치 몬스터 1개의 재스폰 상태. 런타임(WorldStateManager)과 세이브에서 같은 타입을 사용한다.
    /// </summary>
    [Serializable]
    public class MonsterRespawnState
    {
        public string mapId;
        /// <summary> SceneEntityId GUID. 재스폰 상태의 키. </summary>
        public string guid;
        public string actorId;
        public SerializableVector3 position;
        public SerializableQuaternion rotation;
        /// <summary> MonsterActorGrade 이름 문자열. </summary>
        public string grade;
        public int baseLevel = 1;

        /// <summary> true=사망 후 재스폰 대기 중, false=재스폰되어 생존 중. </summary>
        public bool waitingRespawn;
        public int respawnCount;
        public float firstKilledGameMinute;
        public float nextRespawnGameMinute;
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
        /// <summary> 캐릭터별 Ability 자원·쿨다운·지속 Effect 런타임. </summary>
        public List<CharacterAbilityRuntimeEntry> characterAbilities =
            new List<CharacterAbilityRuntimeEntry>();

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
        public bool growthInitialized;
        public int growthPoints;
        public List<GrowthInvestmentSaveEntry> growthInvestments = new List<GrowthInvestmentSaveEntry>();
    }

    [Serializable]
    public class GrowthInvestmentSaveEntry
    {
        public string attribute;
        public int rank;
    }

    [Serializable]
    public class CharacterHpEntry
    {
        public string type;
        public float currentHp;
        public float skillGauge;
    }

    [Serializable]
    public sealed class CharacterAbilityRuntimeEntry
    {
        public string type;
        public AbilityRuntimeSaveData runtime = new AbilityRuntimeSaveData();
    }

    [Serializable]
    public class InventorySaveData
    {
        public int gold;
        public List<ItemSaveEntry> items = new List<ItemSaveEntry>();

        /// <summary> 캐릭터별 장착 장비 (활성/벤치 공통). </summary>
        public List<CharacterEquipmentSaveEntry> equipment = new List<CharacterEquipmentSaveEntry>();
    }

    [Serializable]
    public class CharacterEquipmentSaveEntry
    {
        public string type;
        public int rightHand = -1;
        public int leftHand  = -1;
        public int head      = -1;
        public int chest     = -1;
        public int pants     = -1;
        public int shoes     = -1;
        public int gloves    = -1;
    }

    [Serializable]
    public class ItemSaveEntry
    {
        public int itemId;
        public int count;
        public int slotKey;
        public int enhancementLevel;
        public List<EquipmentGrowthAttributeRoll> growthAttributeRolls = new List<EquipmentGrowthAttributeRoll>();
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

    [Serializable]
    public class FirstTimeGuideSaveData
    {
        public bool combatGuideShown;
        public bool companionGuideShown;
        public bool equipmentGuideShown;
    }
}
