# Story 시스템 가이드

## 개요

게임 진행도(`progress`)와 완료된 스토리 ID 집합(`completedStories`)을 관리하고, 트리거 존이나 외부 호출에서 들어오는 스토리 트리거 요청을 검증한 뒤 `DialogueManager`에 그래프를 전달하는 매니저입니다.

핵심 특징:

- **진행도 단조 증가** — `SetProgress`는 더 낮은 값을 무시. 한 번 올린 진행도는 내려가지 않음.
- **storyId 1회 트리거** — 한 번 시작된 스토리는 `_completedStories`에 즉시 등록되어 동일 storyId 중복 트리거 차단
- **진행도별 대화 분기** — `StoryEntrySO.variants[]` 로 진행도에 따라 같은 위치에서 다른 대화를 표시
- **세이브 자동 직렬화** — `ISaveable` 구현. 진행도 + 완료 스토리 ID 리스트를 직렬화
- **트리거 존 컴포넌트 제공** — `StoryTriggerZone` (Collider Trigger 기반)으로 씬에 배치만 하면 즉시 동작
- **Markdown 기반 일괄 생성** — Main/Sub Story Generator 에디터 창으로 마크다운 → Quest/Dialogue/StoryEntry 일괄 생성

---

## 아키텍처

```
StoryEntrySO (SO)
├── storyId            세이브 키 (변경 금지)
├── requiredProgress   기본 그래프 트리거 최소 진행도
├── dialogueGraph      기본 대화 그래프
└── StoryVariant[] variants
       ├── requiredProgress  해당 변형 최소 진행도
       └── dialogueGraph     변형 대화 그래프


StoryManager (BaseManager<T>, IManager, ISaveable)
├── _currentProgress : int
├── _completedStories : HashSet<string>
├── SetProgress(int)
├── TryTriggerStory(entry) ──► ResolveGraph ──► DialogueManager.StartDialogue
├── IsCompleted(storyId)
└── ImportSaveData / ExportSaveData


트리거 진입점:
StoryTriggerZone (MonoBehaviour, CapsuleCollider Trigger)
└── OnTriggerEnter(Player) → StoryManager.TryTriggerStory(_storyEntry)


진행도 갱신 진입점 (예시):
- 보스 처치 시              MonsterActor.OnDeath → StoryManager.SetProgress(N)
- 구역 진입 시              영역 트리거         → StoryManager.SetProgress(N)
- 퀘스트 보상 단계 도달 시   QuestManager        → StoryManager.SetProgress(N)
```

### 파일 구조

```
Assets/02.Scripts/
├── Manager/Story/
│   └── StoryManager.cs                매니저 + ISaveable + ResolveGraph
├── Data/Story/
│   ├── StoryEntrySO.cs                스토리 항목 + Variant
│   └── Editor/
│       ├── MainStoryGeneratorWindow.cs    메뉴: UPlayGround/Story/Main Story Generator
│       ├── SubStoryGeneratorWindow.cs     메뉴: UPlayGround/Story/Sub Story Generator
│       └── StoryGeneratorMarkdownLoader.cs  마크다운 파서 헬퍼
└── Story/
    └── StoryTriggerZone.cs            Collider Trigger 진입 시 트리거
```

---

## 핵심 클래스

### StoryEntrySO

| 필드 | 용도 |
|------|------|
| `storyId` | 세이브/식별용 고유 ID. **한번 정하면 변경 금지** (세이브 호환성) |
| `requiredProgress` | 이 스토리가 재생될 최소 게임 진행도 |
| `dialogueGraph` | 기본 대화 그래프 |
| `variants[]` | 진행도별 대체 대화. `requiredProgress` 가장 큰 값에 맞는 변형이 선택됨 |

### StoryVariant

| 필드 | 용도 |
|------|------|
| `requiredProgress` | 이 변형이 사용될 최소 진행도 |
| `dialogueGraph` | 변형 대화 그래프 |

### StoryState (ISaveable 페이로드)

```csharp
[Serializable]
public class StoryState
{
    public int progress;
    public List<string> completedStories;
}
```

### StoryManager (Public API)

| API | 시그니처 | 용도 |
|-----|----------|------|
| `CurrentProgress` | `int (get)` | 현재 진행도 |
| `SetProgress(int)` | — | 진행도 갱신. 더 낮은 값은 무시 |
| `TryTriggerStory(StoryEntrySO)` | `→ bool` | 완료 여부 + 진행도 조건 검사 후 트리거. 시작되면 true |
| `IsCompleted(string storyId)` | `→ bool` | storyId 완료 여부 |

ISaveable:

- `Init`에서 `SaveManager.RegisterSaveable(this)` 호출
- `ExportSaveData(saveData)` → `saveData.story.progress`, `saveData.story.completedStories` 채움
- `ImportSaveData(saveData)` → 위 값을 다시 적용

### StoryTriggerZone

`Collider(Is Trigger)` + `StoryEntrySO` 연결만으로 동작. 플레이어 태그(`Player`)와 일치하는 콜라이더 진입 시 `TryTriggerStory` 호출.

| 인스펙터 필드 | 용도 |
|--------------|------|
| `_storyEntry` | 트리거할 StoryEntrySO |
| `_playerTag` | 비교 대상 태그 (기본 `Player`) |

`[RequireComponent(typeof(CapsuleCollider))]` — CapsuleCollider 자동 부착. 형태에 맞춰 BoxCollider 등 다른 콜라이더가 필요하면 컴포넌트 교체 후 RequireComponent도 함께 변경.

---

## 트리거 흐름 (TryTriggerStory)

```csharp
public bool TryTriggerStory(StoryEntrySO entry)
{
    if (entry == null) return false;
    if (_completedStories.Contains(entry.storyId)) return false;     // 1) 이미 완료
    if (_currentProgress < entry.requiredProgress) return false;     // 2) 진행도 부족

    var graph = ResolveGraph(entry);                                  // 3) 변형 선택
    if (graph == null) { /* 경고 */ return false; }

    _completedStories.Add(entry.storyId);                             // 4) 즉시 완료 등록
    DialogueManager.Instance.StartDialogue(graph);                    // 5) 대화 시작
    return true;
}
```

> **중요:** 4단계에서 **대화 시작 전**에 `_completedStories`에 등록한다. 대화 도중 같은 트리거 존을 재진입해도 중복 트리거되지 않게 만드는 안전장치다. 대화가 도중에 취소되어도 storyId는 완료로 간주됨.

### ResolveGraph

```csharp
private DialogueGraphSO ResolveGraph(StoryEntrySO entry)
{
    DialogueGraphSO best = null;
    int bestReq = -1;

    foreach (var v in entry.variants)
        if (_currentProgress >= v.requiredProgress && v.requiredProgress > bestReq)
        {
            best = v.dialogueGraph;
            bestReq = v.requiredProgress;
        }

    return best != null ? best : entry.dialogueGraph;
}
```

- `variants` 중 `requiredProgress <= currentProgress` 를 만족하는 것 중 **가장 높은 requiredProgress** 의 그래프 선택
- 매칭되는 variant가 없으면 기본 `entry.dialogueGraph`로 폴백

---

## 사용 예시

### 1. 진행도 단계 정의 (예시 컨벤션)

```csharp
public static class StoryProgress
{
    public const int Start              = 0;
    public const int Tutorial_Done      = 100;
    public const int Chapter1_BossDown  = 200;
    public const int Chapter2_Open      = 300;
    public const int Final              = 1000;
}
```

진행도 값을 50/100 단위로 띄워두면 사이에 새 분기를 끼워 넣기 쉬움. 정수 직접 사용보다 상수 클래스 사용을 권장.

### 2. 진행도 갱신

```csharp
// 보스 처치 시
public void OnBossDeath()
{
    StoryManager.Instance.SetProgress(StoryProgress.Chapter1_BossDown);
}
```

### 3. 트리거 존 셋업

1. 빈 GameObject에 `StoryTriggerZone` 추가 (CapsuleCollider 자동 부착)
2. CapsuleCollider의 **Is Trigger** 체크
3. 인스펙터의 `_storyEntry`에 `Story_*.asset` 할당
4. 플레이어가 영역 진입 시 자동 발화

### 4. 진행도별 다른 대화 (Variants)

```
StoryEntrySO  Story_TownGate
├── storyId: "town_gate_intro"
├── requiredProgress: 0
├── dialogueGraph:  DLG_TownGate_Default        (초기 인사)
└── variants:
       ├── requiredProgress 200, graph: DLG_TownGate_AfterBoss   (보스 처치 후)
       └── requiredProgress 500, graph: DLG_TownGate_FinalAct    (최종장)
```

런타임:
- progress  50 → `DLG_TownGate_Default`
- progress 200 → `DLG_TownGate_AfterBoss`
- progress 500 → `DLG_TownGate_FinalAct`

> 단, **진행도 첫 번째 트리거 후 storyId가 완료**되므로, 같은 storyId의 variant를 다시 보려면 **별도의 storyId**로 분리하거나 (가장 일반적) `_completedStories`에서 명시적으로 제거하는 메커니즘이 별도로 필요.

### 5. 외부에서 직접 트리거 (스크립트)

```csharp
// 퀘스트 클리어 콜백 등에서
[SerializeField] private StoryEntrySO _afterQuestStory;

void OnQuestComplete()
{
    StoryManager.Instance.TryTriggerStory(_afterQuestStory);
}
```

### 6. 완료 여부 조회 (UI/조건)

```csharp
if (StoryManager.Instance.IsCompleted("intro_meet_npc1"))
{
    // 인트로를 본 후에만 등장하는 상점 메뉴
}
```

---

## 에디터 도구

### Story Generator

| 도구 | 메뉴 |
|------|------|
| Main Story Generator | `UPlayGround/Story/Main Story Generator` |
| Sub Story Generator | `UPlayGround/Story/Sub Story Generator` |

두 창 모두 마크다운 문서를 입력으로 받아 다음 SO를 일괄 생성:

- `StoryEntrySO`
- `DialogueGraphSO` + `DialogueNodeSO[]`
- (필요 시) `QuestSO`, `QuestObjectiveData`

마크다운 파싱은 `StoryGeneratorMarkdownLoader`에서 처리. 자세한 입력 포맷은 각 창의 도움말 / 코드 주석 참고.

또한 Generator Tool 묶음 메뉴에서도 동일 도구에 접근 가능:

- `UPlayGround/Generator Tool/Main Story Generator`
- `UPlayGround/Generator Tool/Sub Story Generator`

---

## 셋업 방법

1. **StoryEntrySO 작성**
   - `Create → UPlayGround/Story/Entry` → `Story_*.asset`
   - `storyId`, `requiredProgress`, `dialogueGraph` 채우고 (선택) `variants` 추가
2. **DialogueGraphSO 준비**
   - 기본 그래프와 variant 그래프 모두 사전 생성 (`UPlayGround/Dialogue/Graph` 참조)
3. **트리거 배치**
   - 씬에서 트리거 위치에 빈 GameObject 생성 → `StoryTriggerZone` 추가
   - Collider의 Is Trigger 체크 + 형태 조정
4. **진행도 갱신 위치 정의**
   - 보스 처치 / 구역 진입 / 퀘스트 완료 등 진행도 변화점에 `StoryManager.SetProgress(N)` 호출 코드 추가
5. **GameManager 등록 확인**
   - `[15] StoryManager` 가 `[14] DialogueManager`, `[1] SaveManager` 이후에 초기화되는지 확인 (`Init` 시 SaveManager 등록 필요)

---

## 주의 사항

- **storyId는 영구 고정.** 한 번 발급된 storyId를 변경하면 기존 세이브의 `completedStories` 와 매칭이 깨져 같은 스토리가 재생됨. 변경 대신 **새 storyId 발급**이 안전.
- **TryTriggerStory는 즉시 완료 등록.** 대화가 시작되기 전에 storyId가 등록되므로, 대화 도중 게임 종료 시에도 재트리거되지 않는다. 의도적으로 다회 트리거가 필요하면 별도 로직(완료 등록 지연)이 필요.
- **진행도는 단조 증가.** `SetProgress(낮은값)`은 무시. 디버그/치트로 진행도를 되돌리려면 직접 `_currentProgress` 필드를 수정하는 별도 API가 필요.
- **variants는 `requiredProgress > bestReq` 비교로 동률 미지원.** 같은 `requiredProgress` 의 variant 두 개를 두면 **나중 등록된 쪽**이 무시된다. 동률이 필요하면 1 차이를 두거나 ResolveGraph 룰을 변경.
- **DialogueManager 의존.** `TryTriggerStory`는 `DialogueManager.Instance` 가 초기화되어 있다고 가정한다. 매니저 초기화 순서가 `[14] Dialogue → [15] Story` 인지 확인.
- **트리거 존은 단발성.** `OnTriggerEnter`만 사용하므로 진입 후 영역에 머물러도 재발화하지 않는다. 영역에서 나갔다가 재진입 시 storyId 완료라면 무시되고, 미완료라면 다시 발화됨.
- **세이브 호환성.** 새로운 진행도 단계나 storyId를 추가해도 기존 세이브와 호환된다(없는 ID는 단순 무시). 단 storyId의 의미를 재정의하면 안 된다.

---

## 확장 포인트

### 진행도 다중 라인 (메인/사이드)

현재 `_currentProgress`는 단일 정수. 메인/사이드/캐릭터별 진행도가 필요하면 `Dictionary<string, int>` 형태로 확장하고 `SetProgress(line, value)` 시그니처로 변경.

### 변형 선택 룰 변경

`ResolveGraph` 정책을 (a) 가장 높은 requiredProgress (b) 가장 가까운 (c) 가중치 기반 등으로 교체 가능. 정책 인터페이스를 분리하면 SO 단위로 룰을 바꿀 수 있다.

### 진행도 변경 알림

현재 `SetProgress`는 이벤트를 발화하지 않는다. UI/매니저가 진행도 변화에 반응해야 하면 `event Action<int> OnProgressChanged` 추가 후 발화.

### 1회성 → N회성 스토리

기본 `_completedStories.Contains` 체크는 1회성 트리거. N회 또는 무한 트리거를 지원하려면 `StoryEntrySO`에 `repeatable` 플래그를 추가하고 TryTriggerStory에서 분기.

### 트리거 조건 확장

현재 트리거 조건은 `progress + 완료 여부` 두 가지. GlobalFlag, 시간대, 파티 구성원 등 복합 조건이 필요하면 `StoryEntrySO`에 `ConditionSO[] preconditions` 를 추가하고 모든 condition.Evaluate() 통과 시에만 트리거.

### Quest / Dialogue 통합 유틸

`MainStoryGeneratorWindow` 처럼 Markdown → SO 일괄 생성 구조를 따르면 신규 챕터 추가 시 SO 100여 개를 수동 작성하지 않고 일괄 생성 가능. 신규 마크다운 스키마를 추가하면 동일 패턴으로 확장된다.
