# 퀘스트(Quest) 시스템 가이드

## 개요

UPlayground의 퀘스트 시스템은 **ScriptableObject 기반의 목표 추적 및 보상 지급 메커니즘**입니다.

### 핵심 특징

- **12가지 목표 타입**: 기본 8종 + 자동 조우 완료·사이클 보스 처치·사이클 루팅·자동 상호작용
- **타입 안전 API**: 자동 생성 `QuestIdType` enum으로 문자열 오타 없이 퀘스트 참조
- **세이브/로드 연동**: `ISaveable` 구현으로 완료 목록 및 진행 중 목표 카운트 영속 저장
- **EventManager 연동**: 수락·완료·목표 갱신 시 `QuestEvent`로 UI에 브로드캐스트
- **HUD / 미니맵 연동**: `UI_HudQuest`는 퀘스트 추적 HUD, `UI_Minimap`은 활성 퀘스트의 위치/NPC 목표 마커 표시 담당
- **선행 조건 지원**: 완료 필요 퀘스트 목록 + 스토리 진행도 이중 게이팅
- **비주얼 에디터**: 좌우 2패널 `QuestEditorWindow`로 QuestSO 생성·편집·DB 갱신·Enum 재생성

---

## 아키텍처

```
┌──────────────────────────────────────────────┐
│              GameManager (싱글톤)              │
└──────────────────┬───────────────────────────┘
                   │ RegisterManager
          ┌────────▼────────┐
          │   QuestManager  │  IManager, ISaveable
          │  BaseManager<T> │
          └────────┬────────┘
                   │
     ┌─────────────┼──────────────┐
     │             │              │
     ▼             ▼              ▼
QuestDatabase  InventoryManager  EventManager
(Addressable)  (보상 지급,       (QuestEvent
               ItemCollect       브로드캐스트)
               진행도 조회)
                                      │
                                      ├─→ UI_HudQuest
                                      │   (퀘스트 추적 HUD 갱신)
                                      └─→ UI_Minimap
                                          (퀘스트 마커 새로고침)

미니맵 위치 마커 흐름:
  MinimapMarkerRegistrar ──→ MinimapMarkerRegistry ──→ UI_Minimap
                                                        ↑
                         QuestManager.GetActiveQuests() ┘

외부 Notify 흐름:
  MonsterActor ──→ NotifyMonsterKill()  ──┐
  NPC 인터랙션  ──→ NotifyItemDelivered() ─┤
  PortalActor  ──→ NotifyLocationReached()─┤─→ QuestManager
  RecipeManager──→ NotifyItemCrafted()   ─┤   (목표 카운트 갱신
  StoryManager ──→ NotifyStoryProgress() ─┘    → 완료 시 보상 지급)
```

### 클래스 의존 관계

```
데이터 계층 (Assets/02.Scripts/Data/Quest/)
├── QuestObjectiveType  (enum) — 12가지 목표 타입
├── QuestObjectiveData  — 단일 목표 정의 (타입, 대상ID, 수량 등)
├── QuestRewardData     — 보상 (골드, 아이템 목록)
├── QuestStatus         (enum) — Locked / Available / Active / Completed / Failed
├── QuestSO             (ScriptableObject) — 퀘스트 정의
├── QuestDatabase       (ScriptableObject) — 전체 퀘스트 조회 테이블
├── QuestRuntimeData    — 런타임 진행 상태 (목표별 카운트)
├── QuestIdType         (enum, 자동생성) — QuestSO.questId 1:1 대응
└── QuestEventData      — QuestStateEventData / QuestObjectiveEventData

매니저 계층 (Assets/02.Scripts/Manager/Quest/)
└── QuestManager        — 수락·완료·포기·알림·보상·세이브

UI 계층
├── UI_HudQuest         — HUD 퀘스트 추적기
└── UI_Minimap          — 활성 퀘스트 ReachLocation / ItemDeliver 목표 마커 표시

미니맵 계층
├── MinimapMarkerRegistrar — 씬 오브젝트 위치를 LocationId로 등록
├── MinimapMarkerRegistry  — 등록된 LocationId → 월드 위치 조회 테이블
└── MinimapIconConfigSO    — 퀘스트/정적/액터 마커 아이콘 및 표시 옵션

에디터 도구 (Assets/02.Scripts/Data/Quest/Editor/)
├── QuestEditorWindow   — 좌우 2패널 메인 에디터 창
├── QuestSOEditor       — QuestSO 인스펙터 커스텀 에디터
└── QuestDatabaseEditor — QuestDatabase 인스펙터 커스텀 에디터
```

---

## 파일 구조

```
Assets/
├── 02.Scripts/
│   ├── Data/
│   │   ├── UI/
│   │   │   ├── MinimapIconConfigSO.cs  — 미니맵 아이콘/표시 옵션
│   │   │   └── MapConfigDatabaseSO.cs  — MapID별 미니맵 Config 조회
│   │   ├── Quest/
│   │   │   ├── QuestObjectiveType.cs   — 목표 타입 enum
│   │   │   ├── QuestObjectiveData.cs   — 목표 데이터 클래스
│   │   │   ├── QuestRewardData.cs      — 보상 데이터 클래스
│   │   │   ├── QuestStatus.cs          — 퀘스트 상태 enum
│   │   │   ├── QuestSO.cs              — 퀘스트 ScriptableObject
│   │   │   ├── QuestDatabase.cs        — 퀘스트 DB ScriptableObject
│   │   │   ├── QuestRuntimeData.cs     — 런타임 진행 상태
│   │   │   ├── QuestEventData.cs       — EventManager 이벤트 데이터
│   │   │   ├── QuestIdType.cs          — ★ 자동 생성 (직접 수정 금지)
│   │   │   └── Editor/
│   │   │       ├── QuestEditorWindow.cs    — 메인 에디터 창
│   │   │       ├── QuestSOEditor.cs        — QuestSO 인스펙터
│   │   │       └── QuestDatabaseEditor.cs  — QuestDatabase 인스펙터
│   │   ├── Enum/
│   │   │   └── QuestEventType.cs       — QuestEvent enum
│   │   └── Save/
│   │       └── GameSaveData.cs         — QuestSaveData 포함
│   │
│   ├── Manager/
│   │   └── Quest/
│   │       └── QuestManager.cs
│
│   └── UI/
│       ├── HUD/
│       │   ├── Quest/
│       │   │   └── UI_HudQuest.cs
│       │   └── Minimap/
│       │       ├── UI_Minimap.cs
│       │       ├── MinimapMarkerRegistrar.cs
│       │       ├── MinimapMarkerRegistry.cs
│       │       ├── MinimapEntityIcon.cs
│       │       └── MinimapUserMarkerSystem.cs
│       └── Scene/
│           └── Quest/
│               └── UI_QuestMenu.cs
│
└── 10.Datas/
    └── Quest/
        ├── QuestDatabase.asset         — Addressables 키: "QuestDatabase"
        └── (QuestSO *.asset 파일들)
```

---

## 핵심 데이터 클래스

### QuestObjectiveType

```csharp
public enum QuestObjectiveType
{
    ItemCollect   = 0,  // 아이템 수집 (인벤토리 보유 수량 기준)
    ItemDeliver   = 1,  // 아이템 NPC에게 전달
    ItemUse       = 2,  // 아이템 사용
    MonsterKill   = 3,  // 몬스터 처치
    StoryProgress = 4,  // 스토리 진행도 도달
    ItemCraft     = 5,  // 아이템 제작 (레시피 기준)
    ItemEnhance   = 6,  // 아이템 강화
    ReachLocation = 7,  // 목표 지점 도달
    EncounterClear = 8, // 자동 생성 조우 완료
    CycleBossDefeat = 9, // 사이클 보스 처치
    CycleLootCollect = 10, // 사이클 자동 배치 루팅 획득
    InteractionComplete = 11, // 자동 생성 상호작용 완료
}
```

### QuestObjectiveData

```csharp
[Serializable]
public class QuestObjectiveData
{
    public string objectiveId;     // 퀘스트 내 고유 ID
    [TextArea] public string description;
    public QuestObjectiveType type;

    public int targetId;           // 아이템ID / 레시피ID / 스토리 진행도 값
    public int npcId;              // ItemDeliver 전용: 전달 대상 NPC ID
    public string targetStringId;  // 위치/Actor/조우/spawn/상호작용 ID

    [Min(1)] public int requiredCount = 1;  // 달성에 필요한 수량
}
```

**타입별 사용 필드**:

| 타입 | targetId | npcId | targetStringId | requiredCount |
|------|----------|-------|----------------|---------------|
| ItemCollect | 아이템 ID | — | — | 필요 보유 수 |
| ItemDeliver | 아이템 ID | NPC ID | — | 전달 수량 |
| ItemUse | 아이템 ID | — | — | 사용 횟수 |
| MonsterKill | 레거시 숫자 ID 폴백 | — | ActorDefinition의 안정 `actorId` | 처치 수 |
| StoryProgress | 진행도 값 | — | — | (미사용) |
| ItemCraft | 레시피 ID | — | — | 제작 횟수 |
| ItemEnhance | 아이템 ID | — | — | 강화 횟수 |
| ReachLocation | — | — | 위치 ID | (미사용) |
| EncounterClear | — | — | 조우 ID, 빈 값은 전체 | 완료 수 |
| CycleBossDefeat | — | — | spawnId, 빈 값은 전체 | 처치 수 |
| CycleLootCollect | 아이템 ID, 0은 전체 | — | — | 획득 수량 |
| InteractionComplete | — | — | 상호작용 ID, 빈 값은 전체 | 완료 수 |

### QuestSO

```csharp
[CreateAssetMenu(fileName = "QuestSO", menuName = "UPlayGround/Quest/QuestSO")]
public class QuestSO : ScriptableObject
{
    [Header("기본 정보")]
    public string questId;           // 전체 DB 내 고유 ID (QuestIdType enum의 소스)
    public string questName;
    [TextArea] public string questDescription;

    [Header("선행 조건")]
    public List<string> requiredQuestIds;      // 완료해야 하는 선행 퀘스트 ID 목록
    public int requiredStoryProgress = 0;      // 필요 스토리 진행도 (0이면 조건 없음)

    [Header("목표")]
    public List<QuestObjectiveData> objectives;

    [Header("보상")]
    public QuestRewardData reward;

    [Header("설정")]
    public bool isRepeatable = false;  // 완료 후 재수락 가능
    public bool autoComplete = false;  // 모든 목표 달성 즉시 자동 완료
}
```

### QuestDatabase

```csharp
[CreateAssetMenu(fileName = "QuestDatabase", menuName = "UPlayGround/Quest/QuestDatabase")]
public class QuestDatabase : ScriptableObject
{
    // 주요 API
    public void Initialize();                          // 런타임 딕셔너리 빌드
    public QuestSO GetQuest(string questId);
    public IEnumerable<QuestSO> GetAllQuests();
    public List<QuestSO> QuestList { get; }            // 에디터 접근용

    // 에디터 전용
    public void RefreshDatabase(string folderPath);    // 폴더 스캔 → _quests 갱신
}
```

### QuestRuntimeData

```csharp
public class QuestRuntimeData
{
    public QuestSO QuestSO { get; }
    public QuestStatus Status { get; set; }

    // objectiveId → 현재 진행 카운트
    public Dictionary<string, int> ObjectiveProgress { get; }

    public bool IsObjectiveComplete(QuestObjectiveData obj);
    public bool AreAllObjectivesComplete();
    public int  AddProgress(string objectiveId, int value = 1);
    public void SetProgress(string objectiveId, int value);
}
```

### QuestIdType (자동 생성)

```csharp
// 자동 생성 파일 — 직접 수정하지 마세요.
// UPlayGround/ID Enum Generator 또는 Quest Editor → [Enum 생성]으로 재생성하세요.
namespace UPlayGround.Data.Quest
{
    public enum QuestIdType
    {
        None = 0,
        kill_10_goblins = 1,   // 예시
        deliver_herb    = 2,   // 예시
        // QuestDatabase에 등록된 QuestSO.questId로 자동 생성됨
    }

    public static class QuestIdTypeExtensions
    {
        public static string ToQuestId(this QuestIdType type) => type switch
        {
            QuestIdType.kill_10_goblins => "kill_10_goblins",
            // ...
            _ => string.Empty,
        };
    }
}
```

---

## QuestManager API

### 공개 API — QuestIdType 사용

```csharp
// 수락
bool AcceptQuest(QuestIdType questId);

// 완료 (autoComplete=false 퀘스트를 외부에서 완료 처리)
bool CompleteQuest(QuestIdType questId);

// 포기
bool AbandonQuest(QuestIdType questId);

// 상태 조회
QuestStatus         GetQuestStatus(QuestIdType questId);
bool                IsQuestActive(QuestIdType questId);
bool                IsQuestCompleted(QuestIdType questId);
QuestRuntimeData    GetActiveQuestRuntime(QuestIdType questId);

// 목록 조회
IEnumerable<QuestRuntimeData> GetActiveQuests();
List<QuestSO>                 GetAvailableQuests();  // 수락 가능 목록

bool IsDBLoaded { get; }
```

### Notify API — 외부 시스템에서 호출

| 메서드 | 파라미터 | 연결 위치 |
|--------|----------|-----------|
| `NotifyItemCollected(itemId, count)` | 아이템 ID, 수량 | InventoryManager.AddItem() 또는 아이템 픽업 액터 |
| `NotifyItemDelivered(npcId, itemId, count)` | NPC ID, 아이템 ID, 수량 | NPC 상호작용 핸들러 / 대화 액션 |
| `NotifyItemUsed(itemId, count)` | 아이템 ID, 사용 횟수 | 아이템 사용 처리 코드 |
| `NotifyMonsterKill(actorId)` | `MonsterActor.ActorId` 문자열 | MonsterActor 사망 처리 / EnemyCombat. 숫자 overload는 레거시 데이터 폴백 |
| `NotifyStoryProgress(progress)` | 진행도 값 | StoryManager.SetProgress() |
| `NotifyItemCrafted(recipeId, quantity)` | 레시피 ID, 수량 | RecipeManager.OnCraftingCompleted 구독 |
| `NotifyItemEnhanced(itemId)` | 아이템 ID | 강화 시스템 완료 처리 |
| `NotifyLocationReached(locationId)` | 위치 ID (string) | PortalActor / 트리거 존 |

### 이벤트 — EventManager 구독

```csharp
// 퀘스트 수락 / 완료 / 실패 이벤트
EventManager.Instance.Subscribe<QuestEvent, QuestStateEventData>(
    QuestEvent.QuestAccepted,
    data => Debug.Log($"퀘스트 수락: {data.QuestId}"));

EventManager.Instance.Subscribe<QuestEvent, QuestStateEventData>(
    QuestEvent.QuestCompleted,
    data => Debug.Log($"퀘스트 완료: {data.QuestId}"));

// 목표 진행도 변경 이벤트
EventManager.Instance.Subscribe<QuestEvent, QuestObjectiveEventData>(
    QuestEvent.QuestObjectiveUpdated,
    data => UpdateQuestHUD(data.QuestId, data.ObjectiveId,
                           data.CurrentCount, data.RequiredCount));
```

---

## HUD / 미니맵 연동

### UI_HudQuest

`UI_HudQuest`는 HUD 레이어에 표시되는 퀘스트 추적 UI입니다. 현재 스크립트는 `UI_Base` 생명주기 골격만 있으며, 실제 표시 로직은 아래 이벤트를 구독해 구현합니다.

| 이벤트 | 사용 목적 |
|--------|-----------|
| `QuestEvent.QuestAccepted` | 새 활성 퀘스트를 HUD 추적 목록에 추가 |
| `QuestEvent.QuestObjectiveUpdated` | 목표 설명과 `CurrentCount / RequiredCount` 진행도 갱신 |
| `QuestEvent.QuestCompleted` | 완료 연출 후 HUD 목록에서 제거 또는 완료 상태 표시 |
| `QuestEvent.QuestFailed` | 실패 상태 표시 후 제거 |

권장 동작:
- `OnShow()`에서 `QuestManager.Instance.GetActiveQuests()`로 현재 활성 퀘스트를 한 번 그린다.
- `OnShow()`에서 `EventManager` 구독, `OnHide()` 또는 `OnDispose()`에서 반드시 해제한다.
- `QuestObjectiveData.description`을 표시 텍스트로 사용하고, 목표가 완료되면 체크/흐림 상태로 갱신한다.
- `autoComplete=false` 퀘스트는 모든 목표 완료 후 `CompleteQuest()`를 호출할 버튼 또는 상호작용 안내를 별도로 제공한다.

```csharp
private void OnShowQuestHud()
{
    foreach (var runtime in QuestManager.Instance.GetActiveQuests())
        DrawOrUpdateQuest(runtime);

    EventManager.Instance.Subscribe<QuestEvent, QuestStateEventData>(
        QuestEvent.QuestAccepted, OnQuestStateChanged);
    EventManager.Instance.Subscribe<QuestEvent, QuestObjectiveEventData>(
        QuestEvent.QuestObjectiveUpdated, OnQuestObjectiveUpdated);
    EventManager.Instance.Subscribe<QuestEvent, QuestStateEventData>(
        QuestEvent.QuestCompleted, OnQuestStateChanged);
    EventManager.Instance.Subscribe<QuestEvent, QuestStateEventData>(
        QuestEvent.QuestFailed, OnQuestStateChanged);
}
```

### UI_Minimap

`UI_Minimap`은 활성 퀘스트의 위치성 목표만 미니맵에 표시합니다.

| 퀘스트 목표 타입 | LocationId 해석 | 사용 아이콘 |
|------------------|-----------------|-------------|
| `ReachLocation` | `QuestObjectiveData.targetStringId` | `MinimapIconConfigSO.questTarget` |
| `ItemDeliver` | `npc_{QuestObjectiveData.npcId}` | `MinimapIconConfigSO.questNpc` |

동작 흐름:
1. `UI_Minimap.OnShow()`에서 `QuestManager.Instance.GetActiveQuests()`를 조회한다.
2. 아직 완료되지 않은 `ReachLocation` / `ItemDeliver` 목표만 마커 후보가 된다.
3. 후보의 LocationId가 `MinimapMarkerRegistry`에 등록되어 있으면 `_questContainer`에 아이콘을 생성한다.
4. `QuestAccepted`, `QuestCompleted`, `QuestFailed` 이벤트를 받으면 전체 퀘스트 마커를 다시 만든다.
5. 씬 오브젝트가 늦게 생성되어 `MinimapMarkerRegistrar`가 추가되면, 활성 퀘스트와 LocationId가 맞는 경우 즉시 마커를 추가한다.

미니맵 마커 표시 조건:
- 현재 맵의 `MapConfigDatabaseSO`에서 `MinimapIconConfigSO`가 정상 조회되어야 한다.
- `MinimapIconConfigSO.showQuestMarkers`가 `true`여야 한다.
- `questTarget` 또는 `questNpc` 아이콘 엔트리에 `sprite`가 설정되어 있어야 한다.
- 목표 위치 오브젝트에 `MinimapMarkerRegistrar`가 있고 `LocationId`가 퀘스트 목표와 일치해야 한다.
- `QuestManager.IsDBLoaded == true`인 상태에서 활성 퀘스트를 조회할 수 있어야 한다.

### MinimapMarkerRegistrar 설정 규칙

`ReachLocation` 목표:

```csharp
// QuestObjectiveData
type = QuestObjectiveType.ReachLocation;
targetStringId = "forest_gate";

// 씬 목표 오브젝트의 MinimapMarkerRegistrar
LocationId = "forest_gate";
MarkerType = MinimapMarkerType.QuestTarget;
```

`ItemDeliver` 목표:

```csharp
// QuestObjectiveData
type = QuestObjectiveType.ItemDeliver;
npcId = 101;

// 전달 대상 NPC 또는 NPC 위치 오브젝트의 MinimapMarkerRegistrar
LocationId = "npc_101";
MarkerType = MinimapMarkerType.QuestTarget;
```

> **주의**: `MinimapMarkerType.QuestTarget`은 활성 퀘스트 조건을 만족할 때만 퀘스트 마커로 표시됩니다. 항상 보이는 고정 NPC/마을/포탈 마커는 `Npc`, `Town`, `Portal` 타입을 사용하세요.

---

## 셋업 방법

### 1. 폴더 생성

```
Assets/10.Datas/Quest/     ← QuestSO 에셋 저장 위치
```

### 2. QuestDatabase 에셋 생성

Unity 에디터에서:  
**Project 창 우클릭 → Create → UPlayGround → Quest → QuestDatabase**

생성된 `QuestDatabase.asset`을 `Assets/10.Datas/Quest/`에 저장.

### 3. Addressables 등록

`QuestDatabase.asset` 선택 → Inspector → **Addressable 체크** → 키를 **`QuestDatabase`**로 설정.

### 4. QuestSO 생성

방법 A — Quest Editor 사용 (권장):
1. 메뉴: **UPlayGround → Quest → Quest Editor**
2. `[+ 새 퀘스트]` 버튼 → Quest ID / 이름 / 저장 경로 입력 → 생성

방법 B — Project 창:
**우클릭 → Create → UPlayGround → Quest → QuestSO**

### 5. QuestSO 편집

Quest Editor에서 퀘스트 선택 후 우 패널에서 편집:

- **기본 정보**: questId(영문, 고유값), questName, 설명
- **선행 조건**: 완료 필요 퀘스트 ID 목록, 필요 스토리 진행도
- **목표**: `[+ 추가]` → 타입 선택 → 관련 필드 입력
- **보상**: 골드 + 보상 아이템 목록 (`[선택]` 버튼으로 아이템 피커 사용)
- **설정**: 반복 퀘스트 여부, 자동 완료 여부

### 6. DB 갱신

Quest Editor 툴바 → **`[DB 갱신]`** 클릭  
→ `Assets/10.Datas/Quest/` 폴더의 모든 QuestSO를 QuestDatabase에 등록

### 7. QuestIdType Enum 재생성

Quest Editor 툴바 → **`[Enum 생성]`** 클릭  
→ `Assets/02.Scripts/Data/Quest/QuestIdType.cs` 자동 생성

또는: 메뉴 **UPlayGround → ID Enum Generator** → Quest 행 `[생성]`

### 8. GameManager에 이미 등록됨

`GameManager.InitializeManagers()`에 `QuestManager.Instance`가 등록되어 있으므로 추가 작업 불필요.

### 9. HUD 퀘스트 UI 연결

HUD Canvas에 `UI_HudQuest` 프리팹/오브젝트를 배치하고, `OnShow()` 시점에 활성 퀘스트 목록과 `QuestEvent`를 기준으로 표시를 갱신하도록 구현합니다.

필수 구독 이벤트:
- `QuestAccepted`
- `QuestObjectiveUpdated`
- `QuestCompleted`
- `QuestFailed`

### 10. 미니맵 퀘스트 마커 연결

1. HUD 미니맵 프리팹의 `UI_Minimap`에 `_questContainer`, `_iconContainer`, `_playerIcon`, `_mapBackground`, `_minimapMask`, `_mapConfigDB`를 할당합니다.
2. 현재 `SceneManager.CurrentMapID`에 대응하는 `MinimapIconConfigSO`를 `MapConfigDatabaseSO`에 등록합니다.
3. `MinimapIconConfigSO.showQuestMarkers`를 켜고 `questTarget`, `questNpc` 아이콘을 설정합니다.
4. `ReachLocation` 목표 지점 또는 `ItemDeliver` 대상 NPC에 `MinimapMarkerRegistrar`를 추가합니다.
5. `LocationId`를 `targetStringId` 또는 `npc_{npcId}` 규칙에 맞춥니다.

---

## 사용 예시

### 기본 — 퀘스트 수락 및 상태 조회

```csharp
using UPlayGround.Data.Quest;
using UPlayGround.Manager;

// 퀘스트 수락
QuestManager.Instance.AcceptQuest(QuestIdType.kill_10_goblins);

// 상태 조회
QuestStatus status = QuestManager.Instance.GetQuestStatus(QuestIdType.kill_10_goblins);
if (status == QuestStatus.Active)
{
    QuestRuntimeData runtime = QuestManager.Instance.GetActiveQuestRuntime(QuestIdType.kill_10_goblins);
    foreach (var obj in runtime.QuestSO.objectives)
    {
        int current  = runtime.ObjectiveProgress[obj.objectiveId];
        int required = obj.requiredCount;
        Debug.Log($"{obj.description}: {current}/{required}");
    }
}
```

### 몬스터 처치 알림

```csharp
// MonsterActor 사망 처리 또는 EnemyCombat에서
public void OnMonsterDeath(string actorId)
{
    QuestManager.Instance.NotifyMonsterKill(actorId);
}
```

### 아이템 전달 퀘스트 (NPC 상호작용)

```csharp
// NPC 상호작용 핸들러에서
public void OnDeliverItems(int npcId, int itemId, int count)
{
    if (!InventoryManager.Instance.RemoveItem(itemId, count))
    {
        Debug.LogWarning("아이템 부족");
        return;
    }
    QuestManager.Instance.NotifyItemDelivered(npcId, itemId, count);
}
```

### 스토리 진행 연동

```csharp
// StoryManager.SetProgress() 이후
public void SetProgress(int progress)
{
    if (progress <= _currentProgress) return;
    _currentProgress = progress;
    QuestManager.Instance.NotifyStoryProgress(progress);
}
```

### 제작 완료 연동

```csharp
// RecipeManager 이벤트 구독 (초기화 시점)
RecipeManager.Instance.OnCraftingCompleted += (recipeId, quantity) =>
{
    QuestManager.Instance.NotifyItemCrafted(recipeId, quantity);
};
```

### 위치 도달 트리거 (트리거 존 컴포넌트)

```csharp
public class QuestLocationTrigger : MonoBehaviour
{
    [SerializeField] private string locationId;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        QuestManager.Instance.NotifyLocationReached(locationId);
    }
}
```

### 위치 목표 미니맵 마커 등록

```csharp
// 별도 코드 호출은 필요 없습니다.
// 목표 지점 GameObject에 MinimapMarkerRegistrar를 붙이고 Inspector에서 설정합니다.

LocationId = "forest_gate";                 // QuestObjectiveData.targetStringId와 동일
MarkerType = MinimapMarkerType.QuestTarget; // 활성 퀘스트일 때 UI_Minimap에 표시
```

### 아이템 전달 NPC 미니맵 마커 등록

```csharp
// npcId = 101인 ItemDeliver 목표라면
LocationId = "npc_101";
MarkerType = MinimapMarkerType.QuestTarget;
```

### HUD 퀘스트 추적기 이벤트 갱신

```csharp
private void OnObjectiveUpdated(QuestObjectiveEventData data)
{
    // data.QuestId, data.ObjectiveId 기준으로 HUD 항목을 찾아 진행도 갱신
    _questTracker.SetProgress(data.QuestId, data.ObjectiveId,
                              data.CurrentCount, data.RequiredCount);
}
```

### autoComplete=false 퀘스트 수동 완료

```csharp
// UI에서 퀘스트 완료 버튼 클릭 시
public void OnClickCompleteQuest(QuestIdType questId)
{
    bool ok = QuestManager.Instance.CompleteQuest(questId);
    if (!ok)
        ShowMessage("목표를 먼저 달성하세요.");
}
```

### 수락 가능 퀘스트 목록 표시

```csharp
List<QuestSO> available = QuestManager.Instance.GetAvailableQuests();
foreach (var q in available)
    Debug.Log($"[수락 가능] {q.questName}");
```

---

## 에디터 도구

### Quest Editor (메인 에디터 창)

**메뉴:** `UPlayGround → Quest → Quest Editor`

```
┌───────────────────────────────────────────────────────────────┐
│ + 새 퀘스트  복제  삭제  │전체│반복│자동완료│  검색: ___  DB갱신 Enum생성 ↺ │
├──────────────────────┬────────────────────────────────────────┤
│ 퀘스트 목록           │ 상세 편집                               │
│  ┌───────────────┐   │  ┌────────────────────────────────────┐│
│  │ [3] 고블린 사냥 │   │  │ 기본 정보                          ││
│  │ kill_goblins   │   │  │   questId / questName / 설명       ││
│  └───────────────┘   │  ├────────────────────────────────────┤│
│  ┌───────────────┐   │  │ 선행 조건                          ││
│  │ [1] 약초 전달  │   │  │   완료 필요 퀘스트 / 스토리 진행도    ││
│  │ deliver_herb   │   │  ├────────────────────────────────────┤│
│  └───────────────┘   │  │ 목표  [+ 추가]                      ││
│  ┌───────────────┐   │  │   ┌──────────────────────────────┐ ││
│  │ ⚠ ID중복 예시  │   │  │   │ [몬스터 처치] 고블린 10마리    │ ││
│  └───────────────┘   │  │   │  몬스터ID: 101 / 처치수: 10   │ ││
│                      │  │   └──────────────────────────────┘ ││
│                      │  ├────────────────────────────────────┤│
│                      │  │ 보상                                ││
│                      │  │   골드: 500 / 보상 아이템 [선택]     ││
│                      │  ├────────────────────────────────────┤│
│                      │  │ 설정                                ││
│                      │  │   반복 퀘스트 □  자동 완료 □        ││
│                      │  └────────────────────────────────────┘│
└──────────────────────┴────────────────────────────────────────┘
```

| 기능 | 설명 |
|------|------|
| **+ 새 퀘스트** | Quest ID / 이름 / 저장 경로 팝업 → QuestSO 생성 |
| **복제** | 선택된 QuestSO를 같은 폴더에 복사 (ID에 `_copy` 접미사) |
| **삭제** | 확인 다이얼로그 후 에셋 삭제 |
| **필터 탭** | 전체 / 반복 / 자동완료 필터 |
| **검색** | questId 또는 questName으로 실시간 필터 |
| **ID 중복 감지** | 같은 questId 존재 시 ⚠ 표시 + 에러 메시지 |
| **목표 카드** | 타입별 컬러 코딩 + 관련 필드만 노출 + 연결 포인트 힌트 |
| **목표 순서** | ▲ / ▼ 버튼으로 objectives 순서 변경 |
| **보상 아이템 피커** | 아이콘·이름 검색 팝업으로 아이템 선택 |
| **DB 갱신** | `Assets/10.Datas/Quest/` 폴더 스캔 → QuestDatabase 등록 |
| **Enum 생성** | QuestDatabase 기반으로 `QuestIdType.cs` 재생성 |

### QuestSOEditor (인스펙터)

QuestSO 선택 시 인스펙터에 자동 적용.

- 목표 타입별 **컬러 코딩** 카드
- 타입에 따라 **관련 필드만** 노출 (예: ItemDeliver면 NPC ID 필드 표시)
- 각 목표 하단에 **연결 포인트 힌트** 표시 (어디서 Notify를 호출해야 하는지)
- **`[Quest Editor에서 열기]`** 버튼

### QuestDatabaseEditor (인스펙터)

QuestDatabase.asset 선택 시 인스펙터에 자동 적용.

- **스캔 폴더** 경로 설정 (기본: `Assets/10.Datas/Quest`)
- **`[DB 갱신 (폴더 스캔)]`** 버튼
- **`[QuestIdType Enum 생성]`** 버튼
- **`[Quest Editor 열기]`** 버튼
- 등록된 퀘스트 수 표시

---

## 세이브 / 로드

`QuestManager`는 `ISaveable`을 구현하여 `SaveManager`에 자동 등록됩니다.

### 저장 데이터 구조

```csharp
[Serializable]
public class QuestSaveData
{
    public List<string> completedQuestIds;       // 완료된 퀘스트 ID 목록
    public List<ActiveQuestSaveEntry> activeQuests;
}

[Serializable]
public class ActiveQuestSaveEntry
{
    public string questId;
    public Dictionary<string, int> objectiveProgress;  // objectiveId → 진행 카운트
}
```

### 직렬화 전략

| 항목 | 저장 형식 | 비고 |
|------|-----------|------|
| 완료 목록 | `List<string>` | questId 문자열 저장 (enum 재생성과 무관) |
| 진행 중 목표 카운트 | `Dictionary<string, int>` | objectiveId 문자열 키 |
| 세이브 포맷 | JSON (Newtonsoft) | GameSaveData.quest 필드 |

> **주의**: 세이브는 문자열 ID 기반이므로 QuestIdType enum을 재생성해도 기존 세이브 파일이 호환됩니다.

### 런타임 퀘스트 수명주기

`QuestManager.RegisterRuntimeQuest`로 등록한 `QuestSO`는 현재 실행에만 존재하며 QuestDatabase 에셋과 일반 퀘스트 세이브에 포함하지 않는다. 완료/실패 목록, 진행 카운트, 추적 ID에서도 런타임 퀘스트 ID를 제외한다. 사이클 자동 검증 퀘스트는 저장된 `CycleLayoutState.generatedContent`에서 다시 저작하고 조우·보스·루팅·상호작용 완료 플래그로 진행 상태를 복원한다.

`UnregisterRuntimeQuests`가 현재 추적 퀘스트를 제거하면 모든 대상 삭제를 끝낸 뒤 최종 추적 대상을 한 번 계산한다. 다른 활성 퀘스트가 있으면 `QuestTracked`, 없으면 `QuestUntracked`를 정확히 한 번 발행해 HUD와 월드 마커가 오래된 런타임 퀘스트를 표시하지 않게 한다.

---

## QuestIdType Enum 재생성 흐름

```
QuestSO 생성/수정
      ↓
Quest Editor [DB 갱신]
  → QuestDatabase._quests 갱신
      ↓
Quest Editor [Enum 생성]  또는  ID Enum Generator → Quest [생성]
  → QuestIdType.cs 재생성
      ↓
코드에서 타입 안전하게 사용:
  QuestManager.Instance.AcceptQuest(QuestIdType.kill_10_goblins);
```

**내부 동작**:
- `QuestIdType.ToQuestId()` → QuestSO.questId 문자열 반환
- `QuestManager` 내부는 `Dictionary<string, QuestRuntimeData>` 유지
- 공개 API는 enum 파라미터만 수신, 내부에서 string 변환

---

## 주의 사항

### QuestIdType.None 사용 금지

`QuestIdType.None.ToQuestId()` → `string.Empty` 반환 → `AcceptQuest`에서 경고 후 `false` 반환.

```csharp
// ❌ 잘못된 사용
QuestManager.Instance.AcceptQuest(QuestIdType.None);

// ✅ 올바른 사용
QuestManager.Instance.AcceptQuest(QuestIdType.kill_10_goblins);
```

### Enum 재생성 후 코드 동기화

QuestSO의 `questId`를 변경하거나 QuestSO를 삭제하면 반드시 **Enum 재생성** 후 코드에서 사용 중인 `QuestIdType.이전이름`을 모두 갱신해야 합니다.

### autoComplete=true 주의

모든 목표 달성 즉시 보상이 자동 지급됩니다. 완료 연출(애니메이션, UI) 등이 필요한 퀘스트는 `autoComplete=false`로 설정하고 UI에서 수동으로 `CompleteQuest()`를 호출하세요.

### ItemCollect 목표 — 인벤토리 기준

`NotifyItemCollected` 호출 시 진행도를 아이템 개수로 즉시 갱신합니다. 이후 해당 아이템을 **소비하면 진행도가 감소하지 않습니다** (카운트는 고정). 소비 추적이 필요한 경우 `ItemDeliver` 타입을 사용하세요.

### 씬 전환 후 QuestManager 상태

`QuestManager.OnSceneChanged()`는 현재 아무 처리도 하지 않습니다. `_activeQuests` / `_completedQuestIds`는 씬 전환 후에도 유지됩니다.

### DB 로드 타이밍

`QuestManager.IsDBLoaded`가 `false`인 동안 `AcceptQuest()` 등은 `false` 반환합니다. QuestDatabase는 Addressables 비동기 로드이므로 게임 시작 직후 즉시 사용 불가합니다. `IsDBLoaded`를 확인하거나 DB 로드 완료 이벤트를 사용하세요.

---

## 확장 포인트

### 새로운 목표 타입 추가

1. `QuestObjectiveType` enum에 값 추가
2. `QuestManager.UpdateObjectives()` switch 분기 또는 새 `Notify*()` 메서드 추가
3. `QuestSOEditor` / `QuestEditorWindow`의 `DrawObjectiveCard()` switch에 case 추가

```csharp
// 예시: NPC 대화 완료 목표 추가
public void NotifyDialogueCompleted(string dialogueId)
{
    foreach (var runtime in _activeQuests.Values)
        foreach (var obj in runtime.QuestSO.objectives)
        {
            if (obj.type != QuestObjectiveType.DialogueComplete) continue;
            if (obj.targetStringId != dialogueId) continue;
            if (runtime.IsObjectiveComplete(obj)) continue;

            runtime.SetProgress(obj.objectiveId, obj.requiredCount);
            SendObjectiveEvent(runtime, obj);
            TryAutoComplete(runtime);
        }
}
```

### 퀘스트 실패 조건

현재 `QuestStatus.Failed`는 enum에 정의되어 있으나 자동 트리거 로직이 없습니다. 타임 리밋 등 실패 조건이 필요하면:

```csharp
public bool FailQuest(QuestIdType questId)
{
    string id = questId.ToQuestId();
    if (!_activeQuests.TryGetValue(id, out var runtime)) return false;

    runtime.Status = QuestStatus.Failed;
    _activeQuests.Remove(id);
    SendQuestEvent(QuestEvent.QuestFailed, id);
    return true;
}
```

### 퀘스트 체인 (자동 수락)

```csharp
// QuestCompleted 이벤트 구독으로 체인 구현
EventManager.Instance.Subscribe<QuestEvent, QuestStateEventData>(
    QuestEvent.QuestCompleted,
    data =>
    {
        if (data.QuestId == QuestIdType.kill_10_goblins.ToQuestId())
            QuestManager.Instance.AcceptQuest(QuestIdType.deliver_goblin_fang);
    });
```

### 퀘스트 알림 UI (HUD 추적기)

```csharp
// UI 컴포넌트에서 이벤트 구독
private void OnEnable()
{
    EventManager.Instance.Subscribe<QuestEvent, QuestObjectiveEventData>(
        QuestEvent.QuestObjectiveUpdated, OnObjectiveUpdated);
    EventManager.Instance.Subscribe<QuestEvent, QuestStateEventData>(
        QuestEvent.QuestCompleted, OnQuestCompleted);
}

private void OnObjectiveUpdated(QuestObjectiveEventData data)
{
    // HUD 퀘스트 추적기 갱신
    _questTracker.UpdateObjective(data.QuestId, data.ObjectiveId,
                                  data.CurrentCount, data.RequiredCount);
}

private void OnDisable()
{
    EventManager.Instance.Unsubscribe<QuestEvent, QuestObjectiveEventData>(
        QuestEvent.QuestObjectiveUpdated, OnObjectiveUpdated);
    EventManager.Instance.Unsubscribe<QuestEvent, QuestStateEventData>(
        QuestEvent.QuestCompleted, OnQuestCompleted);
}
```

### 미니맵 목표 타입 확장

현재 `UI_Minimap.ResolveQuestLocationId()`는 `ReachLocation`, `ItemDeliver`만 위치 마커로 변환합니다. 대화 완료, 특정 오브젝트 조사처럼 위치 표시가 필요한 목표 타입을 추가하면 이 메서드와 `GetQuestMarkerEntry()`에 매핑을 추가하세요.

```csharp
private static string ResolveQuestLocationId(QuestObjectiveData obj) => obj.type switch
{
    QuestObjectiveType.ReachLocation => obj.targetStringId,
    QuestObjectiveType.ItemDeliver   => $"npc_{obj.npcId}",
    _                               => null,
};
```

---

## 참고 자료

### 코드 경로

| 항목 | 경로 |
|------|------|
| 데이터 클래스 | `Assets/02.Scripts/Data/Quest/` |
| QuestIdType (자동생성) | `Assets/02.Scripts/Data/Quest/QuestIdType.cs` |
| 이벤트 타입 | `Assets/02.Scripts/Data/Enum/QuestEventType.cs` |
| 세이브 데이터 | `Assets/02.Scripts/Data/Save/GameSaveData.cs` |
| 매니저 | `Assets/02.Scripts/Manager/Quest/QuestManager.cs` |
| HUD 퀘스트 UI | `Assets/02.Scripts/UI/HUD/Quest/UI_HudQuest.cs` |
| 퀘스트 메뉴 UI | `Assets/02.Scripts/UI/Scene/Quest/UI_QuestMenu.cs` |
| 미니맵 HUD | `Assets/02.Scripts/UI/HUD/Minimap/UI_Minimap.cs` |
| 미니맵 마커 등록 | `Assets/02.Scripts/UI/HUD/Minimap/MinimapMarkerRegistrar.cs` |
| 미니맵 마커 레지스트리 | `Assets/02.Scripts/UI/HUD/Minimap/MinimapMarkerRegistry.cs` |
| 미니맵 아이콘 설정 | `Assets/02.Scripts/Data/UI/MinimapIconConfigSO.cs` |
| 에디터 도구 | `Assets/02.Scripts/Data/Quest/Editor/` |
| ID Enum Generator | `Assets/02.Scripts/Tool/Editor/IdEnumGeneratorWindow.cs` |
| QuestSO 에셋 | `Assets/10.Datas/Quest/` |
| QuestDatabase (Addressable) | `Assets/10.Datas/Quest/QuestDatabase.asset` → 키: `"QuestDatabase"` |

### 관련 시스템

| 시스템 | 관계 |
|--------|------|
| `SaveManager` / `ISaveable` | 퀘스트 진행 상태 영속 저장 |
| `EventManager` | 수락·완료·목표 갱신 UI 브로드캐스트 |
| `InventoryManager` | ItemCollect 진행도 조회, 보상 아이템 지급 |
| `StoryManager` | 선행 조건 스토리 진행도 확인 |
| `RecipeManager` | OnCraftingCompleted → NotifyItemCrafted 연동 |
| `UI_HudQuest` | 활성 퀘스트 목록과 목표 진행도를 HUD에 표시 |
| `UI_Minimap` | 활성 퀘스트의 ReachLocation / ItemDeliver 목표 마커 표시 |
| `MinimapMarkerRegistry` | 퀘스트 LocationId와 씬 오브젝트 월드 위치 연결 |
| `MinimapIconConfigSO` | 퀘스트 마커 표시 여부와 아이콘 설정 |
| `GlobalFlagManager` | 대화/플래그 조건과 퀘스트 조건 병행 사용 가능 |
| ID Enum Generator | QuestIdType 자동 생성 (다른 DB enum과 동일 도구) |
