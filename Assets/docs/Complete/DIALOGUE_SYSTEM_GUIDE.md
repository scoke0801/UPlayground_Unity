# Dialogue 시스템 가이드

## 개요

ScriptableObject 기반의 그래프 대화 시스템입니다. **DialogueGraphSO** 가 노드의 컨테이너이고, **DialogueNodeSO** 가 한 노드(Talk / Choice / Condition / Event / End), **DialogueManager** 가 채널별 Runner를 보유해 그래프를 실행합니다. 보조 매니저인 **GlobalFlagManager** 는 대화·퀘스트가 공유하는 전역 bool 플래그 저장소이며, 세이브에 직렬화됩니다.

핵심 특징:

- **그래프 + 노드 분리** — 그래프는 노드 리스트만, 라우팅은 노드의 `nextNodeId`/`trueNextNodeId`/`falseNextNodeId`/`choices[].nextNodeId` 문자열 참조
- **3개 채널 (Main/System/Monologue)** 독립 Runner — Main/System은 단일 실행, Monologue는 큐로 순차 처리
- **확장 가능한 Condition / Action** — `ConditionSO.Evaluate()`, `DialogueActionSO.Execute()` 추상 메서드만 구현하면 새 분기/효과 추가
- **GlobalFlagManager** — 대화·퀘스트가 공유하는 string→bool 플래그 저장소. `ISaveable` 구현으로 세이브 자동 직렬화
- **SpeakerColorTable** — Addressables로 로드되는 화자별 색상 테이블, UI 측에서 직접 참조
- **SpeakerActorBindingTable** — Main 채널 대화 카메라가 사용할 `speakerId -> actorId` 매핑 테이블
- **노드 진입 액션** — 노드 진입 시 `eventActions[]` 가 자동 실행되어 아이템 지급/플래그 설정 등 부수 효과 처리
- **Choice 표시 조건** — 각 선택지에 `displayCondition`, `isGreyedOut` 으로 동적 표시 가능

---

## 아키텍처

```
DialogueGraphSO (SO)
├── graphId / graphName
├── startNodeId
└── List<DialogueNodeSO> nodes
        │
        ▼
DialogueNodeSO (SO)
├── nodeId (GUID, 자동 부여)
├── nodeType : { Talk, Choice, Condition, Event, End }
├── channel  : { Main, System, Monologue }
├── speakerId / dialogueText / portrait / typingSpeed / autoAdvanceDuration
├── nextNodeId / trueNextNodeId / falseNextNodeId / choices[]
├── condition : ConditionSO        (NodeType.Condition)
└── eventActions : List<DialogueActionSO>  (모든 노드에서 진입 시 실행)


DialogueManager (BaseManager<T>, IManager)
├── Dictionary<DialogueChannel, DialogueRunner> _runners
│      ├── Main      Runner — 단일 실행 (enableQueue=false)
│      ├── System    Runner — 단일 실행
│      └── Monologue Runner — 큐 실행 (enableQueue=true)
│
├── ColorTable : SpeakerColorTableSO                 (Addressables 비동기 로드)
├── SpeakerActorBindings : SpeakerActorBindingTableSO (Addressables 비동기 로드)
│
└── 이벤트
       ├── OnMainNodeEnter / OnSystemNodeEnter / OnMonologueNodeEnter
       ├── OnChoicePresented (List<ChoiceData>)
       └── OnDialogueEnd

DialogueRunner (internal)
├── 그래프 큐 + 현재 노드 추적
├── EnterNode(node)
│      ├── eventActions 일괄 Execute
│      └── nodeType 분기:
│             Talk      → NotifyNodeEnter
│             Choice    → NotifyNodeEnter + NotifyChoicePresented
│             Condition → condition.Evaluate() ? trueNext : falseNext
│             Event     → 즉시 nextNodeId로 진행
│             End       → End() (큐 다음 그래프 실행 또는 종료)


GlobalFlagManager (BaseManager<T>, IManager, ISaveable)
└── Dictionary<string, bool> _flags
       ├── GetFlag(key) / SetFlag(key, value)
       └── ExportSaveData / ImportSaveData → GameSaveData.flags
```

### 파일 구조

```
Assets/02.Scripts/
├── Manager/Dialogue/
│   ├── DialogueManager.cs              매니저 + DialogueRunner (internal)
│   └── GlobalFlagManager.cs            전역 플래그 + ISaveable
│
├── Data/Dialogue/
│   ├── DialogueGraphSO.cs              그래프 컨테이너
│   ├── DialogueNodeSO.cs               노드 + NodeType / DialogueChannel / ChoiceData
│   ├── ConditionSO.cs                  abstract Evaluate()
│   ├── DialogueActionSO.cs             abstract Execute()
│   ├── SpeakerColorTableSO.cs          화자 색상 테이블 (Addressables)
│   ├── SpeakerActorBindingTableSO.cs   화자 ID와 ActorId 매핑 테이블 (Addressables)
│   └── Editor/
│       ├── DialogueGraphEditor.cs      메뉴: UPlayGround/Story/Dialogue Graph Editor
│       └── DialogueJsonIO.cs           그래프 JSON Import/Export
│
└── UI/Dialogue/
    ├── UI_Scene_Dialogue.cs                  Main 채널 UI
    ├── UI_Scene_SystemDialogue.cs            System 채널 UI
    ├── UI_Scene_MonologueDialogue.cs         Monologue 채널 UI
    └── UIDialogueChoiceButton.cs      Choice 버튼
```

---

## 핵심 클래스

### DialogueNodeSO

| 필드 | 용도 |
|------|------|
| `nodeId` | GUID. 에디터에서 자동 부여, 직접 편집 불가 |
| `nodeType` | `Talk` / `Choice` / `Condition` / `Event` / `End` |
| `channel` | `Main` / `System` / `Monologue` — UI 채널 결정 |
| `speakerId` | SpeakerColorTable 키. Main 채널 대화 카메라에서는 `SpeakerActorBindingTableSO`를 거쳐 ActorId로 해석 |
| `dialogueText` | 대화 본문 |
| `portrait` | 초상화 Sprite |
| `typingSpeed` | 타이핑 속도(초/문자), 기본 0.04 |
| `autoAdvanceDuration` | 0이면 입력 대기, > 0이면 자동 진행 |
| `nextNodeId` | Talk/Event 분기 |
| `trueNextNodeId` / `falseNextNodeId` | Condition 분기 |
| `choices` | `List<ChoiceData>` (Choice 노드) |
| `condition` | `ConditionSO` (Condition 노드) |
| `eventActions` | 노드 진입 시 실행되는 `DialogueActionSO` 리스트 |

### ChoiceData

| 필드 | 용도 |
|------|------|
| `choiceText` | 선택지 표시 텍스트 |
| `nextNodeId` | 선택 시 이동할 노드 |
| `displayCondition` | null이면 항상 표시. `Evaluate()` 결과로 표시 여부 결정 |
| `isGreyedOut` | 조건 미충족 시 숨김 대신 비활성화 표시 |

### DialogueGraphSO

| API | 용도 |
|-----|------|
| `StartNode` | `GetNode(startNodeId)` |
| `GetNode(string id)` | 첫 호출 시 nodes → Dictionary 캐시 후 조회 |
| `InvalidateCache()` | 에디터 노드 추가/삭제 시 캐시 무효화 |

### ConditionSO (추상)

```csharp
public abstract class ConditionSO : ScriptableObject
{
    public abstract bool Evaluate();
}
```

상속 예시 (대표 위치 기준):

- `EnemySkillCondition` — `Data/Combat/`
- `RecipeUnlockCondition` — `Data/Crafting/`
- 신규 조건은 `ConditionSO`를 상속해 새 SO로 추가

### DialogueActionSO (추상)

```csharp
public abstract class DialogueActionSO : ScriptableObject
{
    public abstract void Execute();
}
```

신규 액션 예: 아이템 지급, 플래그 설정, 퀘스트 시작/완료 등을 각자 SO로 구현하고 노드의 `eventActions`에 첨부.

### DialogueManager (Public API)

| API | 시그니처 | 용도 |
|-----|----------|------|
| `StartDialogue(graph)` | `(DialogueGraphSO)` | 그래프 시작. 채널은 `graph.StartNode.channel` |
| `Advance(channel)` | `(DialogueChannel = Main)` | Talk 노드에서 다음으로 진행 (Choice 노드는 무시) |
| `SelectChoice(int index)` | — | Main 채널 Choice 노드 선택지 결정 |
| `ColorTable` | `SpeakerColorTableSO (get)` | UI에서 화자 색상 조회 (로드 전 null) |
| `SpeakerActorBindings` | `SpeakerActorBindingTableSO (get)` | Main 채널 대화 카메라용 화자-Actor 매핑 조회 (로드 전 null) |

이벤트:

| 이벤트 | 페이로드 | 발화 시점 |
|--------|----------|-----------|
| `OnMainNodeEnter` | `DialogueNodeSO` | Main 채널 노드 진입 |
| `OnSystemNodeEnter` | `DialogueNodeSO` | System 채널 노드 진입 |
| `OnMonologueNodeEnter` | `DialogueNodeSO` | Monologue 채널 노드 진입 |
| `OnChoicePresented` | `List<ChoiceData>` | Choice 노드 진입 (가시 선택지만 필터링됨) |
| `OnDialogueEnd` | — | 채널 종료 (큐가 비어 있을 때만 발화) |

### DialogueRunner (internal)

채널 한 개당 하나. `Enqueue(graph)` → `IsRunning` 분기로 즉시 Run 또는 큐 적재 (큐는 `enableQueue=true` 채널만). `EnterNode`에서 노드 타입별 처리:

```csharp
switch (node.nodeType)
{
    case NodeType.Talk:      Notify..NodeEnter;           break;  // 입력 대기
    case NodeType.Choice:    Notify..NodeEnter + Choice;  break;  // 선택 대기
    case NodeType.Condition: condition.Evaluate() 분기;   break;  // 즉시 진행
    case NodeType.Event:     nextNodeId 즉시 진행;         break;
    case NodeType.End:       End() → 큐 다음 또는 종료;   break;
}
```

> **주의:** Choice 노드의 `OnChoicePresented`는 `displayCondition` 평가가 끝난 가시 선택지만 전달. UI는 그대로 그리면 된다.

### GlobalFlagManager

| API | 용도 |
|-----|------|
| `GetFlag(string key)` | 미존재 시 false |
| `SetFlag(string key, bool value)` | 플래그 설정 |
| `LoadFlags(Dictionary<string, bool>)` | 일괄 복원 (세이브 로드 시) |
| `GetAllFlags()` | 복사본 반환 |

ISaveable:

- `Init`에서 `SaveManager.RegisterSaveable(this)` 자동 등록
- `ExportSaveData(saveData)` → `saveData.flags.flags = GetAllFlags()`
- `ImportSaveData(saveData)` → `LoadFlags(saveData.flags.flags ?? new())`

### SpeakerColorTableSO

| 항목 | 값 |
|------|-----|
| 메뉴 | `Create → UPlayGround/Dialogue/SpeakerColorTable` |
| Addressables 키 | `SpeakerColorTable` (`AddressableKey` 상수) |
| `GetColor(speakerId)` | 등록 키는 매핑 색, 미등록은 `defaultColor`(흰색) |

`OnEnable` / `OnValidate`(에디터)에서 자동으로 Dictionary 빌드.

### SpeakerActorBindingTableSO

| 항목 | 값 |
|------|-----|
| 메뉴 | `Create → UPlayGround/Dialogue/Speaker Actor Binding Table` |
| Addressables 키 | `SpeakerActorBindingTable` (`AddressableKey` 상수) |
| `TryGetActorId(speakerId, out actorId)` | 화자 ID에 대응하는 ActorId 조회 |

자동 생성/갱신 도구:

- 메뉴: `UPlayGround/Dialogue/Speaker Actor Binding Generator`
- 스캔 대상: `DialogueGraphSO.nodes`, 독립 `DialogueNodeSO`, `Assets/10.Datas/Dialogue` 하위 `.asset` YAML의 `speakerId:`
- Actor 후보: `ActorDefinitionSO.actorId/displayName/asset name`, `NpcActorSO` 에셋명/actorName
- 자동 판별: speakerId 직접 일치, `NpcActorSO.dialogueGraph` 소유자, `DLG_Npc_*`/`dlg_sub_guide*` 파일명 힌트, 부분 일치 순서
- 기본 정책: Main 채널 노드만 스캔, 기존 매핑 보존, 기존 테이블의 비스캔 항목도 유지
- 적용 시 `Assets/10.Datas/Dialogue/SpeakerActorBindingTable.asset` 생성 및 Addressables 주소 `SpeakerActorBindingTable` 등록

Main 채널 대화 노드에 진입하면 `DialogueManager`가 다음 순서로 대화 카메라 타겟을 찾는다.

1. `SpeakerActorBindingTableSO`에서 `speakerId -> actorId` 매핑 조회
2. 테이블이 없거나 항목이 없으면 `speakerId == actorId`로 폴백
3. `GameObjectManager.AllActors`에서 `ActorId`가 같은 `GameActor` 조회
4. 없으면 `ActorSpawnManager.GetSpawnedActors(actorId)` 첫 항목으로 폴백
5. 찾으면 `CameraManager.PushDialogueCamera(speaker, player)` 호출, 못 찾으면 현재 카메라 상태 유지

---

## 사용 예시

### 1. 대화 시작 / 진행 (UI 측)

```csharp
// 트리거 측 — 그래프 시작
DialogueManager.Instance.StartDialogue(graphSO);

// UI 측 — 노드 진입 구독 (UI_Scene_Dialogue.OnEnable 등)
DialogueManager.Instance.OnMainNodeEnter += OnNodeEnter;
DialogueManager.Instance.OnChoicePresented += OnChoices;
DialogueManager.Instance.OnDialogueEnd += OnEnd;

// 입력 처리 — Talk 노드 → 다음 진행
public void OnSubmit() => DialogueManager.Instance.Advance(DialogueChannel.Main);

// 입력 처리 — Choice 선택
public void OnChoiceClick(int index) => DialogueManager.Instance.SelectChoice(index);
```

### 2. 화자 색상 적용 (UI 측)

```csharp
var table = DialogueManager.Instance.ColorTable;
if (table != null)
    nameLabel.color = table.GetColor(node.speakerId);
```

### 3. 신규 ConditionSO 만들기 (플래그 기반)

```csharp
[CreateAssetMenu(menuName = "UPlayGround/Dialogue/Condition/Flag")]
public class FlagConditionSO : ConditionSO
{
    [SerializeField] private string flagKey;
    [SerializeField] private bool   expected = true;

    public override bool Evaluate()
        => GlobalFlagManager.Instance.GetFlag(flagKey) == expected;
}
```

위 SO를 노드의 `condition` 또는 `ChoiceData.displayCondition`에 할당하면 즉시 분기 / 선택지 노출 룰로 동작.

### 4. 신규 DialogueActionSO 만들기 (플래그 설정)

```csharp
[CreateAssetMenu(menuName = "UPlayGround/Dialogue/Action/SetFlag")]
public class SetFlagActionSO : DialogueActionSO
{
    [SerializeField] private string flagKey;
    [SerializeField] private bool   value = true;

    public override void Execute()
        => GlobalFlagManager.Instance.SetFlag(flagKey, value);
}
```

노드의 `eventActions`에 추가하면 노드 진입 시 자동 실행.

### 5. Monologue 큐 흐름

`Monologue` 채널은 `enableQueue=true` 이므로 실행 중 새 그래프가 들어오면 큐에 적재되어 자동 순차 실행:

```csharp
// 동시에 두 번 호출해도 두 번째는 큐에 쌓여 첫 번째 종료 후 자동 실행됨
DialogueManager.Instance.StartDialogue(monoGraphA);
DialogueManager.Instance.StartDialogue(monoGraphB);
```

> Main / System 채널은 `enableQueue=false` 이므로 실행 중 두 번째 호출은 **무시 + 경고 로그**.

### 6. 글로벌 플래그 사용

```csharp
// 퀘스트/대화 측에서 설정
GlobalFlagManager.Instance.SetFlag("met_npc_1", true);

// 다른 시스템(상점, 트리거)에서 조회
if (GlobalFlagManager.Instance.GetFlag("met_npc_1"))
    OpenSpecialShop();
```

플래그는 자동으로 세이브에 포함된다.

### 7. 대화 중 특정 포인트 바라보기

1. 씬에 빈 GameObject를 만들고 `CameraLookAtPoint`를 추가한다.
2. `Point Id`를 씬 안에서 고유하게 지정하고, Transform을 카메라가 바라볼 정확한 위치에 둔다.
3. `FocusDialogueCameraOnPointActionSO` 에셋을 만든 뒤 같은 ID를 입력한다.
4. 해당 액션을 Main 채널 Talk/Choice 노드의 `eventActions`에 연결한다.

이 큐는 현재 라인의 자동 대화 카메라에만 적용되고 다음 라인에서 자동 해제된다. Event/Condition 노드에
연결하면 다음에 표시되는 라인에 적용된다. 대화 스킵으로 지나친 라인의 큐는 폐기되며,
`cameraRecording`이 지정된 노드는 녹화 포즈가 단일 소스이므로 특정 포인트 주시를 함께 사용하지 않는다.

카메라 위치는 원래 대화 구도를 유지하고 회전만 해당 지점으로 바뀐다. 따라서 바닥이나 소품을 잠깐
보여줄 때 카메라 전체가 지점 쪽으로 내려앉지 않는다. `World Offset`은 월드 공간 보정값이다.

### 8. 특정 대사에서 삽화 표시

1. `Create → UPlayGround → 대화 → 액션 → Show Illustration`로 `ShowDialogueIllustrationActionSO` 에셋을 만든다.
2. `Illustration`에 표시할 Sprite를 지정하고, 흰색 마스크 이미지라면 `Tint`로 원하는 색을 지정한다.
3. 삽화를 보여줄 Main 채널 Talk/Choice 노드의 `eventActions`에 액션을 연결한다.
4. 같은 줄에서 시점도 바꾸려면 `FocusDialogueCameraOnPointActionSO`를 함께 연결한다.

삽화는 기존 `UI_Scene_Dialogue` 안에서 전체 화면 딤과 함께 0.12초 페이드로 표시된다. 표시 중에는 대화
패널과 별도 대화 컨트롤 바보다 높은 Canvas 순서를 사용하므로 정지·AUTO·스킵·이전 대화 버튼도 딤 뒤로
내려간다. 삽화 클릭 또는 대화 진행 입력을 받으면 대사는 넘기지 않고 삽화만 먼저 닫으며 Canvas 순서도
원래 값으로 복원한다. 그다음 입력부터 기존의 타이핑 완료·다음 대사 진행 순서가 이어진다. 현재 대사 한
줄에만 유효하며 다음 대사·대화 종료에서도 자동으로 내려간다. Event/Condition 노드에 연결하면 다음에
표시되는 라인에 적용되고, 스킵 중 지나간 액션은 최종 착지 라인에 남지 않는다.

현재 구조 추적 장면용 액션은 다음 세 개가 준비되어 있다.

- `Action_ShowLianDialogueHint.asset` — 나뭇가지에 묶인 붉은 표식
- `Action_ShowHonokaDialogueHint.asset` — 바닥의 남색 천
- `Action_ShowLianNavyMarkerHint.asset` — 리안이 방향을 남긴 남색 매듭 표식

세 에셋은 `Assets/10.Datas/Dialogue/Story/Dialogue/Config/`에 있으며,
`Assets/04.Images/UI/dialogue/`의 전용 삽화를 그대로 참조한다.

---

## 에디터 도구

### DialogueGraphEditor

| 항목 | 값 |
|------|-----|
| 메뉴 경로 | `UPlayGround/Story/Dialogue Graph Editor` |
| 보조 IO | `DialogueJsonIO.cs` (그래프 JSON Import/Export) |
| 주요 기능 | 노드 시각 편집, 노드 ID 자동 부여, choices/eventActions 편집, 시작 노드 지정 |

신규 노드 생성 시 `DialogueNodeSO.AssignNewId()` 가 호출되어 `nodeId`(GUID)가 자동 발급된다. 노드 추가/삭제 후에는 `DialogueGraphSO.InvalidateCache()` 호출이 필요.

---

## 셋업 방법

1. **SpeakerColorTable 등록**
   - `Create → UPlayGround/Dialogue/SpeakerColorTable` 로 SO 생성
   - 화자 ID + 색상 등록
   - Addressables 그룹에 추가 후 키를 `SpeakerColorTable` 로 설정 (`SpeakerColorTableSO.AddressableKey` 값과 일치)
2. **SpeakerActorBindingTable 등록** *(Main 채널 대화 카메라 사용 시)*
   - `UPlayGround/Dialogue/Speaker Actor Binding Generator` 실행
   - `테이블 생성/로드` → `미리보기 갱신` → `매핑 적용`
   - 자동 매핑되지 않은 `<미해결>` 항목은 생성된 테이블에서 수동 지정
   - `speakerId`와 `ActorId`가 같다면 등록하지 않아도 폴백으로 동작
3. **DialogueGraph 생성**
   - `Create → UPlayGround/Dialogue/Graph` → `DLG_*.asset`
4. **노드 추가**
   - 그래프 에셋 폴더 안에 `Create → UPlayGround/Dialogue/Node` 로 `Node_*.asset` 생성
   - 또는 Dialogue Graph Editor에서 시각 편집
5. **그래프 연결**
   - `startNodeId` 와 각 노드의 `nextNodeId` / `trueNextNodeId` / `falseNextNodeId` / `choices[].nextNodeId` 설정
6. **Condition / Action SO 작성**
   - `ConditionSO` / `DialogueActionSO` 상속 클래스 + `[CreateAssetMenu]` 추가
   - 인스턴스 SO를 만들고 노드에 첨부
7. **UI 프리팹 매칭**
   - 채널별 UI Key는 매니저 내부에 `Main → "MainDialogue"`, `System → "SystemDialogue"`, `Monologue → "MonologueDialogue"` 로 고정
   - UIManager에 동일 키로 등록된 UI 프리팹이 있어야 자동 표시됨
8. **GameManager 등록 확인**
   - `[14] DialogueManager`, `[13] GlobalFlagManager` 가 SaveManager 이후에 초기화되도록 순서 확인

---

## 주의 사항

- **`OnSceneChanged`에서 매니저는 아무 작업도 하지 않는다.** Runner의 진행 중 상태가 그대로 유지됨. 씬 전환 직전 `_runners[*].Clear()` 가 필요하다면 호출자 측에서 명시적으로 처리할 것.
- **Main/System 채널은 큐 없음.** 실행 중 새 그래프 호출은 경고 로그와 함께 무시. 동시 발생 가능한 시스템 알림은 호출자 측에서 직렬화 큐를 만들거나 `Monologue` 채널을 활용.
- **노드 ID는 GUID, 변경 금지.** `DialogueNodeSO.nodeId`는 한번 발급되면 절대 수정 금지. 다른 노드의 `nextNodeId` 참조가 모두 깨진다.
- **그래프 캐시 무효화.** 에디터에서 노드를 추가/삭제하면 `DialogueGraphSO.InvalidateCache()`를 호출하거나 그래프 에셋을 다시 로드해야 `_nodeMap` 캐시가 갱신된다.
- **Condition/Action은 SO 인스턴스 공유 가능.** 동일 SO를 여러 노드에 첨부 가능. 단, 인스턴스 상태(필드 변형)가 전역에 영향을 주지 않도록 stateless 또는 ScriptableObject 인스턴스별 격리를 유지.
- **SpeakerColorTable 로드 race.** Addressables 비동기 로드이므로 `ColorTable`이 부팅 직후 null일 수 있다. UI는 null 체크 후 `defaultColor` 폴백.
- **SpeakerActorBindingTable 로드 race.** Addressables 비동기 로드이므로 `SpeakerActorBindings`가 부팅 직후 null일 수 있다. 이 경우 Main 채널 대화 카메라는 `speakerId == actorId` 폴백을 사용한다.
- **세이브 호환성.** 플래그 키는 `string`이므로 자유롭게 추가/제거 가능. 다만 같은 키를 다른 의미로 재사용하지 말 것 (구 세이브 데이터와 충돌). 키 명명 규칙(`metNpc_*`, `event_*`, `quest_*`)을 합의해 관리.
- **eventActions 실행 순서.** 노드의 `eventActions[]` 는 리스트 순서대로 실행된다. 의존성이 있는 액션은 순서를 명시적으로 보장.

---

## 확장 포인트

### 신규 NodeType 추가

`enum NodeType` 에 멤버 추가 → `DialogueRunner.EnterNode` switch에 분기 추가. 라우팅 필드도 노드 SO에 추가하고 에디터에서 노출.

### Condition / Action 라이브러리

`ConditionSO`, `DialogueActionSO` 상속 클래스를 도메인별 폴더로 정리하면 편집자가 인스펙터에서 빠르게 선택 가능.

권장 표준 라이브러리:

| Condition | 의도 |
|-----------|------|
| `FlagConditionSO` | GlobalFlag 비교 |
| `QuestStateConditionSO` | 퀘스트 상태 |
| `ItemHasConditionSO` | 아이템 보유 |
| `PartyMemberConditionSO` | 파티 캐릭터 보유 |

| Action | 의도 |
|--------|------|
| `SetFlagActionSO` | 플래그 토글 |
| `GiveItemActionSO` | 아이템 지급 |
| `StartQuestActionSO` | 퀘스트 시작 |
| `PlaySfxActionSO` | 사운드 재생 |

### 다중 언어 / 로컬라이제이션

`dialogueText` / `choiceText` 를 직접 보관하지 말고 키로 보관해 런타임에 LocalizationManager에서 변환하는 패턴이 권장. 노드의 `dialogueText`를 `[LocalizationKey]` 로 지정하면 빌드 시 한 번에 추출 가능.

### UI 채널 매핑 변경

채널 ↔ UIKey 매핑은 `DialogueManager.ChannelToUIKey` 에 하드코딩되어 있다. 변경/추가가 필요하면 해당 메서드와 UIManager 등록 키를 함께 갱신.

### GlobalFlag 변경 알림

현재 `GlobalFlagManager`는 변경 이벤트를 발화하지 않는다. UI/시스템이 플래그 변화에 반응해야 하는 경우 `event Action<string, bool> OnFlagChanged` 를 추가하고 `SetFlag`에서 발화하도록 확장.
