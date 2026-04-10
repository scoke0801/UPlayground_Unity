# 세이브 시스템 가이드

## 개요

UPlayground의 세이브 시스템은 게임의 전체 진행 상태를 JSON 파일로 직렬화하여 저장하고 복원하는 구조입니다.

### 저장 대상

| 대상 | 담당 매니저 | 저장 내용 |
|------|------------|-----------|
| 인벤토리 | `InventoryManager` | 보유 아이템(ID·수량·슬롯), 골드 |
| 스토리 진행도 | `StoryManager` | 진행도 수치, 완료된 스토리 ID 목록 |
| 대화/퀘스트 플래그 | `GlobalFlagManager` | `Dictionary<string, bool>` 전체 |
| 크래프팅 상태 | `RecipeManager` | 언락된 레시피, 제작 횟수, 몬스터 처치 횟수 |

### 핵심 특징

- **다중 슬롯**: `save_slot_0.json`, `save_slot_1.json` … 슬롯 번호로 구분
- **Newtonsoft.Json 직렬화**: `Dictionary` 등 복잡한 타입 직렬화 지원
- **비동기 DB 타이밍 처리**: Addressable 비동기 로드 완료 전에 `LoadGame()`이 호출돼도 pending 패턴으로 안전하게 복원
- **부분 실패 보호**: Export/Import 중 특정 매니저에서 예외 발생 시 파일 쓰기를 중단하고 에러 로그 출력

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
        │ SaveManager  │  (IManager, BaseManager<T>)
        └──────┬──────┘
               │  RegisterSaveable()
   ┌───────────┼───────────┬──────────────┐
   │           │           │              │
   ▼           ▼           ▼              ▼
InventoryManager  StoryManager  GlobalFlagManager  RecipeManager
(ISaveable)       (ISaveable)   (ISaveable)        (ISaveable)
```

### 파일 구조

```
Assets/
├── 02.Scripts/
│   ├── Data/Save/
│   │   └── GameSaveData.cs        ← 직렬화 DTO 정의
│   └── Manager/Save/
│       ├── ISaveable.cs           ← 세이브 참여 인터페이스
│       └── SaveManager.cs         ← 저장/로드 총괄
```

### 저장 경로

```
Application.persistentDataPath/saves/save_slot_{N}.json
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
// 슬롯 0에 저장
SaveManager.Instance.SaveGame(0);

// 슬롯 0에서 로드 (성공 여부 반환)
bool success = SaveManager.Instance.LoadGame(0);

// 세이브 파일 존재 여부 확인
bool exists = SaveManager.Instance.HasSaveFile(0);

// 슬롯 메타 정보 조회 (UI 표시용 — 날짜, 버전)
SaveSlotInfo info = SaveManager.Instance.GetSaveSlotInfo(0);
// info.saveDateTime, info.saveVersion, info.filePath

// 세이브 파일 삭제
SaveManager.Instance.DeleteSaveFile(0);
```

---

## GameSaveData DTO 구조

```csharp
GameSaveData
├── saveVersion     : string          // 세이브 포맷 버전 ("1.0")
├── saveDateTime    : string          // 저장 일시 ("yyyy-MM-dd HH:mm:ss")
├── inventory       : InventorySaveData
│   ├── gold        : int
│   └── items       : List<ItemSaveEntry>
│       ├── itemId  : int
│       ├── count   : int
│       └── slotKey : int
├── story           : StorySaveData
│   ├── progress           : int
│   └── completedStories   : List<string>
├── flags           : FlagSaveData
│   └── flags       : Dictionary<string, bool>
└── recipe          : RecipeSaveData
    ├── unlockedRecipeIDs  : List<int>
    ├── craftCounts        : Dictionary<int, int>   // recipeID → 제작 횟수
    └── monsterKills       : Dictionary<int, int>   // monsterID → 처치 횟수
```

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
`GameSaveData.saveVersion` 필드로 포맷 버전을 관리한다. 향후 DTO 구조가 변경되면 `LoadGame()` 시 버전을 확인하고 마이그레이션 로직을 추가한다.

### 저장 타이밍
`SaveGame()`은 모든 DB가 완전히 로드된 이후에 호출해야 정상적인 데이터가 저장된다. DB 로드 중에 저장하면 인벤토리·레시피가 빈 상태로 기록될 수 있다.

---

## 저장 파일 예시

```json
{
  "saveVersion": "1.0",
  "saveDateTime": "2026-04-10 15:30:00",
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
  }
}
```
