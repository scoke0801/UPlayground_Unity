# 세이브 시스템 가이드

## 개요

UPlayground의 세이브 시스템은 게임의 전체 진행 상태를 JSON으로 직렬화한 뒤 **AES 암호화**하여 저장하고 복원하는 구조입니다. (세이브 포맷 버전 `2.0` = 암호화 바이너리 `.sav`)

### 저장 대상

| 대상 | 담당 매니저 | 저장 내용 |
|------|------------|-----------|
| 인벤토리 | `InventoryManager` | 보유 아이템(ID·수량·슬롯), 골드 |
| 스토리 진행도 | `StoryManager` | 진행도 수치, 완료된 스토리 ID 목록 |
| 대화/퀘스트 플래그 | `GlobalFlagManager` | `Dictionary<string, bool>` 전체 |
| 크래프팅 상태 | `RecipeManager` | 언락된 레시피, 제작 횟수, 몬스터 처치 횟수 |
| 퀘스트 진행 | `QuestManager` | 완료/진행 중 퀘스트, 목표별 카운트, 추적 상태 |
| 파티 | `PartyManager` | 보유/출전 명단, 레벨·EXP, **캐릭터별 현재 HP·스킬 게이지**, **플레이어 위치·씬·맵** |
| 월드 상태 | `WorldStateManager` | **맵별 처치된 배치 몬스터 GUID** (개별 인스턴스 단위) |

### 핵심 특징

- **최대 3슬롯**: `save_slot_0.sav` ~ `save_slot_2.sav`. `SaveManager.MAX_SLOTS`로 상한 강제(`SaveGame/LoadGame/DeleteSaveFile/GetSaveSlotInfo` 모두 범위 검증).
- **AES 암호화**: `SaveCrypto`(AES-256-CBC, 파일별 무작위 IV). 저장/로드/슬롯 메타 조회 **3경로 모두** 암호화 통과.
- **Newtonsoft.Json 직렬화**: `Dictionary` 등 복잡한 타입 지원. `Vector3/Quaternion`은 순환참조 방지를 위해 `SerializableVector3/Quaternion`으로 보관.
- **비동기 DB 타이밍 처리**: Addressable 비동기 로드 완료 전에 `LoadGame()`이 호출돼도 pending 패턴으로 안전하게 복원
- **부분 실패 보호**: Export/Import 중 특정 매니저에서 예외 발생 시 파일 쓰기를 중단하고 에러 로그 출력
- **로드 오케스트레이션**: `LoadGameToScene(slot)` — 데이터 복원(각 매니저 pending 보관) → 저장된 씬 로드 → 씬 준비 시 파티·위치·월드 상태 자동 적용. 타이틀/다른 맵에서 호출 가능.

---

## 아키텍처

```
┌─────────────────────────────────────────────┐
│            GameManager (싱글톤)               │
│  InitializeManagers() 에서 SaveManager를      │
│  가장 먼저 등록                                │
└──────────────┬──────────────────────────────┘
               │
        ┌──────▼──────┐
        │ SaveManager  │  (IManager, BaseManager<T>)  ── SaveCrypto(AES)
        └──────┬──────┘
               │  RegisterSaveable()
   ┌───────────┼───────────┬──────────┬──────────┬──────────┬───────────┐
   ▼           ▼           ▼          ▼          ▼          ▼           ▼
Inventory   Story      GlobalFlag   Recipe    Quest      Party     WorldState
Manager     Manager    Manager      Manager   Manager    Manager   Manager
(ISaveable) (ISaveable)(ISaveable)  (ISaveable)(ISaveable)(ISaveable)(ISaveable)
```

### 파일 구조

```
Assets/
├── 02.Scripts/
│   ├── Data/Save/
│   │   └── GameSaveData.cs            ← 직렬화 DTO 정의(+ HP/위치/월드/직렬화 벡터)
│   ├── Manager/Save/
│   │   ├── ISaveable.cs               ← 세이브 참여 인터페이스
│   │   ├── SaveManager.cs             ← 저장/로드 총괄 + 슬롯 상한 + 오케스트레이션
│   │   ├── SaveCrypto.cs              ← AES-256-CBC 암호화 유틸
│   │   └── WorldStateManager.cs       ← 맵별 몬스터 처치 영속화
│   ├── GameActor/Component/Common/
│   │   └── SceneEntityId.cs           ← 배치 인스턴스 고유 GUID(복제 자가치유)
│   ├── Tool/Editor/Save/
│   │   └── SceneEntityIdAssigner.cs   ← 배치 몬스터 GUID 일괄 부여 메뉴
│   └── UI/Scene/
│       └── UI_Scene_SaveSlotMenu.cs         ← 3슬롯 선택 UI(저장/로드 모드)
```

### 저장 경로

```
Application.persistentDataPath/saves/save_slot_{N}.sav   (N = 0 ~ 2, AES 암호화 바이너리)
```

---

## ISaveable 인터페이스

세이브 시스템에 참여하는 매니저는 `ISaveable`을 구현한다.

```csharp
public interface ISaveable
{
    void ExportSaveData(GameSaveData saveData);  // 현재 상태를 saveData에 기록
    void ImportSaveData(GameSaveData saveData);  // saveData에서 상태를 복원
}
```

### 등록 방법

`Init()` 내에서 `SaveManager`에 자신을 등록한다. `SaveManager`가 가장 먼저 초기화되므로 어느 매니저의 `Init()`에서 호출해도 안전하다.

```csharp
public void Init()
{
    SaveManager.Instance.RegisterSaveable(this);
    // ... 기타 초기화
}
```

---

## 저장 / 로드 사용법

```csharp
// 슬롯 0에 저장 (slot ∈ [0, MAX_SLOTS-1] = [0,2], 범위 밖은 거부)
SaveManager.Instance.SaveGame(0);

// 슬롯 0의 데이터만 복원 (현재 씬 유지 — 인게임 즉시 로드용)
bool success = SaveManager.Instance.LoadGame(0);

// 슬롯 0 로드 + 저장된 씬으로 진입 (타이틀/다른 맵에서 호출)
SaveManager.Instance.LoadGameToScene(0);

// 가장 최근 슬롯 (이어하기). 없으면 -1
int recent = SaveManager.Instance.GetMostRecentSlot();

// 세이브 파일 존재 여부 확인
bool exists = SaveManager.Instance.HasSaveFile(0);

// 슬롯 메타 정보 조회 (UI 표시용 — 날짜, 버전, 맵, 진행도)
SaveSlotInfo info = SaveManager.Instance.GetSaveSlotInfo(0);

// 전체 슬롯 메타 일괄 조회 (빈 슬롯은 null) — 슬롯 선택 UI용
SaveSlotInfo[] all = SaveManager.Instance.GetAllSlotInfos();

// 세이브 파일 삭제
SaveManager.Instance.DeleteSaveFile(0);
```

---

## GameSaveData DTO 구조

```csharp
GameSaveData
├── saveVersion     : string          // 세이브 포맷 버전 ("2.0" = 암호화)
├── saveDateTime    : string          // 저장 일시 ("yyyy-MM-dd HH:mm:ss")
├── inventory       : InventorySaveData
│   ├── gold        : int
│   └── items       : List<ItemSaveEntry> { itemId, count, slotKey }
├── story           : StorySaveData { progress, completedStories }
├── flags           : FlagSaveData { Dictionary<string,bool> }
├── recipe          : RecipeSaveData { unlockedRecipeIDs, craftCounts, monsterKills }
├── quest           : QuestSaveData { completedQuestIds, activeQuests, trackedQuestId, ... }
├── party           : PartySaveData
│   ├── members         : List<PartyMemberSaveEntry> { type, level, exp }
│   ├── roster          : List<string>
│   ├── battleOrder     : List<string>
│   ├── activeIndex     : int
│   ├── characterHealth : List<CharacterHpEntry> { type, currentHp, skillGauge }
│   ├── loadSceneName   : string                 // 로드 시 진입할 씬 에셋명
│   ├── mapId           : string                 // SceneContext.MapID
│   ├── playerPos       : SerializableVector3
│   ├── playerRot       : SerializableQuaternion
│   └── hasLocation     : bool
└── world           : WorldStateSaveData
    └── killedMonsters  : Dictionary<string, List<string>>   // mapId → 처치 GUID 목록
```

---

## 암호화 (SaveCrypto)

세이브 파일은 JSON 직렬화 후 **AES-256-CBC**로 암호화하여 `.sav` 바이너리로 저장한다.

- 파일 포맷: `[16바이트 IV][AES-CBC 암호문]` — 파일마다 무작위 IV 생성.
- 키는 빌드에 임베드(`SaveCrypto.Key`). 단일플레이 세이브 변조 방지(난독화) 수준이며, 키를 바꾸면 기존 세이브를 읽지 못한다.
- **3경로 모두** 암호화를 통과한다: `SaveGame`(쓰기), `LoadGame`(읽기), `GetSaveSlotInfo`(읽기). 새 파일 입출력을 추가할 때 이 래퍼를 빠뜨리지 말 것.

---

## 파티 HP·스킬 게이지·위치 복원 (순서 트랩 주의)

`PartyManager`가 캐릭터별 현재 HP/스킬 게이지와 플레이어 위치를 저장·복원한다.

⚠️ **복원 순서가 핵심이다.** `TryApplyPendingPartyLoad`는 레벨 반영을 위해 `RefreshGrowthStats`(풀 회복)를, 위치 복원을 위해 `PlayerActor.Respawn`(역시 풀 회복)을 호출한다. 따라서 **저장된 정확한 HP/게이지는 이 두 풀 회복 단계 이후 최종 단계에서** `RestoreCharacterHealth`/`RestoreCharacterSkillGauge`로 덮어써야 손실되지 않는다.

```
① 레벨/EXP/로스터 복원 → ② RefreshGrowthStats (풀 회복)
→ ③ Respawn(위치/회전, 풀 회복) → ④ 정확 HP·게이지 복원  ← 반드시 마지막
```

위치는 KCC와 결합돼 있어 `transform` 직접 대입이 아니라 `Respawn`(내부적으로 `motor.SetPositionAndRotation`)을 재사용한다.

---

## 맵 몬스터 처치 영속화 (WorldStateManager + SceneEntityId)

"이 특정 배치 몬스터가 죽었다"를 세션 간 유지하려면 **인스턴스 단위 안정 식별자**가 필요하다. `instanceID`는 실행마다 재생성되고 `ActorId`는 타입 단위라 부적합하다.

- **`SceneEntityId`** (컴포넌트): 씬 배치 인스턴스에 고유 GUID 부여. 비어 있으면 `OnValidate`가 자동 생성하고, **복제(Ctrl+D)는 `GlobalObjectId`로 감지해 자가 재발급**한다.
- **`SceneEntityIdAssigner`** (에디터 메뉴 `UPlayGround > World > 배치 몬스터 SceneEntityId 일괄 부여`): 열린 씬의 모든 `MonsterActor`에 일괄 부착·중복 보정.
- **`WorldStateManager`** (ISaveable): `mapId → 처치 GUID 집합` 보관.
  - 처치 기록: `MonsterActor.OnDeath` → `NotifyWorldStateKill` → `RecordKill(mapId, guid)`.
  - 적용: 씬 전환(`OnSceneChanged`) 또는 로드 직후 현재 맵의 처치 GUID에 해당하는 배치 몬스터를 제거한다.
  - `SceneEntityId`가 없는 동적 스폰 몬스터는 추적 대상이 아니다.

---

## 슬롯 선택 UI (UI_Scene_SaveSlotMenu)

`UI_Scene_SaveSlotMenu`는 저장/로드 모드를 공유하는 3슬롯 선택 패널이다.

- 포즈 메뉴 저장 버튼 → `SetMode(Save)`: 슬롯 클릭 시 `SaveGame(slot)`.
- 타이틀 Load 버튼 → `SetMode(Load)`: 슬롯 클릭 시 `LoadGameToScene(slot)`.
- 타이틀 Continue 버튼 → `GetMostRecentSlot()`을 `LoadGameToScene`(없으면 새 게임).

> **에디터 수작업 필요**: `UIKeyType`은 프리팹 키에서 자동 생성되므로, ① `SaveSlotMenu` UI 프리팹 제작 ② Addressable 키를 정확히 `"SaveSlotMenu"`(= `UI_Scene_SaveSlotMenu.UIKey`)로 등록 ③ `ID Enum Generator`로 `UIKeyType` 재생성 ④ 포즈/타이틀 버튼 연결이 필요하다. 코드는 문자열 키만 사용해 enum 없이도 동작한다.

---

## 비동기 DB 로딩과 세이브 복원 (Pending 패턴)

`ItemDatabase`와 `RecipeDatabase`는 Addressable 비동기 로드를 사용한다.  
게임 시작 시 `LoadGame()`이 DB 로드 완료 전에 호출될 수 있으므로, 각 매니저는 **pending 패턴**으로 타이밍 문제를 처리한다.

### 흐름

```
GameManager.Awake()
  └─ InitializeManagers()
       ├─ SaveManager.Init()       ← 세이브 폴더 생성
       ├─ ItemManager.Init()       ← ItemDatabase 비동기 로드 시작
       ├─ InventoryManager.Init()  ← SaveManager에 등록
       └─ RecipeManager.Init()     ← SaveManager에 등록 + RecipeDB 비동기 로드 시작

LoadGame(0) 호출 시점:
  ├─ InventoryManager.ImportSaveData()
  │    └─ _pendingLoad에 데이터 보관
  │       ItemDB가 이미 로드됐으면 즉시 ApplyPendingLoad()
  │       아직 로드 중이면 대기
  │
  └─ RecipeManager.ImportSaveData()
       └─ _pendingLoad에 데이터 보관
          DB가 이미 로드됐으면 즉시 ApplyPendingLoad()
          아직 로드 중이면 대기

ItemDatabase 로드 완료 시:
  └─ ItemManager → InventoryManager.OnItemDatabaseReady()
       ├─ _pendingLoad != null → ApplyPendingLoad() (세이브 복원)
       └─ _pendingLoad == null → MakeTestItems()    (최초 실행)

RecipeDatabase 로드 완료 시:
  └─ RecipeManager 내부
       ├─ _pendingLoad != null → ApplyPendingLoad() (세이브 복원)
       └─ _pendingLoad == null → InitUnlockStates() (최초 실행)
```

---

## 새 매니저에 세이브 기능 추가하기

### 1. `ISaveable` 구현

```csharp
public class MyManager : BaseManager<MyManager>, IManager, ISaveable
{
    private int _someValue;

    public void Init()
    {
        SaveManager.Instance.RegisterSaveable(this);
    }

    public void ExportSaveData(GameSaveData saveData)
    {
        saveData.mySection.someValue = _someValue;
    }

    public void ImportSaveData(GameSaveData saveData)
    {
        _someValue = saveData.mySection.someValue;
    }
}
```

### 2. `GameSaveData`에 섹션 추가

```csharp
public class GameSaveData
{
    // ... 기존 필드
    public MySaveData mySection = new MySaveData();
}

[Serializable]
public class MySaveData
{
    public int someValue;
}
```

### 3. 체크리스트

- [ ] `ISaveable` 인터페이스 추가
- [ ] `Init()`에서 `SaveManager.Instance.RegisterSaveable(this)` 호출
- [ ] `GameSaveData`에 섹션 DTO 추가 (기본값 초기화 필수)
- [ ] `ExportSaveData()` — 현재 런타임 상태를 DTO에 기록
- [ ] `ImportSaveData()` — DTO에서 런타임 상태 복원, null 방어 적용
- [ ] DB 로드가 비동기인 경우 pending 패턴 적용

---

## 주의사항

### null 방어
JSON에서 필드가 명시적으로 `null`이면 Newtonsoft는 DTO 기본값을 무시하고 null로 역직렬화한다. `ImportSaveData()`에서 컬렉션 타입은 반드시 null 방어 후 순회해야 한다.

```csharp
// 올바른 방법
foreach (var id in saveData.recipe.unlockedRecipeIDs ?? new List<int>())
    ...

// 위험 — items가 null이면 NullReferenceException
foreach (var entry in saveData.inventory.items)
    ...
```

### 세이브 버전 관리
`GameSaveData.saveVersion` 필드로 포맷 버전을 관리한다. `1.0`=평문 JSON(구버전), `2.0`=AES 암호화 `.sav`. 향후 DTO 구조가 변경되면 `LoadGame()` 시 버전을 확인하고 마이그레이션 로직을 추가한다.

### 저장 타이밍
`SaveGame()`은 모든 DB가 완전히 로드된 이후, 그리고 인게임에서 `PlayerActor`가 존재할 때 호출해야 정상적인 데이터가 저장된다. DB 로드 중 저장 시 인벤토리·레시피가 빈 상태로, `_player`가 없는 시점에 저장 시 HP·위치가 누락(`hasLocation=false`)된다.

### 배치 몬스터 GUID
새 게임플레이 씬을 만들거나 몬스터를 배치/복제한 뒤에는 `SceneEntityId 일괄 부여` 메뉴를 실행하고 씬을 저장해야 처치 영속화가 동작한다. (복제는 자가치유되지만, 신규 배치 일괄 발급은 메뉴로 보장)

---

## 저장 파일 예시

> 실제 파일은 AES 암호화된 `.sav` 바이너리다. 아래는 **암호화 이전의 JSON 구조** 예시.

```json
{
  "saveVersion": "2.0",
  "saveDateTime": "2026-06-15 15:30:00",
  "inventory": {
    "gold": 1500,
    "items": [
      { "itemId": 101, "count": 5, "slotKey": 0 },
      { "itemId": 205, "count": 1, "slotKey": 1 }
    ]
  },
  "story": {
    "progress": 3,
    "completedStories": ["intro_001", "village_boss"]
  },
  "flags": {
    "flags": {
      "met_npc_kaede": true,
      "door_A_opened": false
    }
  },
  "recipe": {
    "unlockedRecipeIDs": [1, 2, 5],
    "craftCounts": { "1": 3, "2": 1 },
    "monsterKills": { "101": 12, "205": 4 }
  },
  "party": {
    "roster": ["Bokusei", "Honoka"],
    "battleOrder": ["Bokusei", "Honoka"],
    "activeIndex": 0,
    "members": [
      { "type": "Bokusei", "level": 5, "exp": 120 },
      { "type": "Honoka", "level": 4, "exp": 30 }
    ],
    "characterHealth": [
      { "type": "Bokusei", "currentHp": 320.0, "skillGauge": 50.0 },
      { "type": "Honoka", "currentHp": 0.0, "skillGauge": 0.0 }
    ],
    "loadSceneName": "InGame",
    "mapId": "village_01",
    "playerPos": { "x": 12.5, "y": 0.0, "z": -8.3 },
    "playerRot": { "x": 0.0, "y": 0.7, "z": 0.0, "w": 0.7 },
    "hasLocation": true
  },
  "world": {
    "killedMonsters": {
      "village_01": ["a1b2c3d4e5f6...", "f6e5d4c3b2a1..."]
    }
  }
}
```
