# FlowGraph 시스템 가이드

> 게임 흐름(연출/진행/조건 분기)을 노드 그래프로 저작·실행·디버깅하는 오케스트레이션 계층.
> 설계 배경과 단계 계획은 `Assets/docs/TODO/node-flow-graph-system.md` 참조.

## 개요

- **시각적 흐름 저작** — "보스 처치 → 플래그 → 대화 → 퀘스트 완료" 같은 매크로 시퀀스를 노드 그래프 하나로 그린다. UI Toolkit + GraphView 기반 전용 에디터 창 제공.
- **토큰(펄스) 기반 실행** — 진입점에서 실행 토큰을 흘려보내고, 각 노드가 완료되면 출력 포트로 전달. 동기 노드는 즉시, 대기 노드(Wait/PlayDialogue)는 코루틴으로 보류.
- **시스템 브릿지** — 노드는 기존 매니저를 `Svc` 계약(`IGlobalFlagService`/`IQuestFlowService`/`IStoryFlowService`/`IDialogueService`)으로 호출만 한다. 매니저 재작성 없음.
- **단일 에셋 직렬화** — 노드는 SO-서브에셋이 아닌 `[SerializeReference]` 다형 리스트. 노드 타입 추가가 클래스 하나로 끝난다.
- **비파괴 병존** — `TriggerComposer`는 그대로 유지. 2~3스텝 단순 반응은 Composer, 다단계 연출/분기/대기는 FlowGraph.
- **계약 기반 재사용** — SubGraph는 stable ID 기반 In/Out/InOut Parameter와 부모 Blackboard Binding을 사용한다.
- **선택적 데이터 흐름** — 실행선과 타입 데이터선을 구분하며 Pure Data 노드는 소비 실행 노드가 필요할 때 Pull 평가한다.
- **실행 설명성** — Runner별 고정 용량 Trace, Watch, 조건부 Breakpoint와 Continue/Step/Stop을 제공한다.

---

## 아키텍처

```
UPlayGround.FlowGraph (runtime asmdef — Core/Data/Contracts만 참조)
┌─────────────────────────────────────────────────────────┐
│ FlowGraphSO (에셋)                                       │
│   ├─ [SerializeReference] List<FlowNode> nodes           │
│   ├─ List<FlowConnection> connections                    │
│   ├─ List<FlowVariableDef> variables                     │
│   └─ List<FlowGraphParameterDef> parameters              │
│                                                          │
│ FlowGraphRunner (씬 MonoBehaviour)                       │
│   ├─ EntryNode.Arm() ── 외부 신호 구독 (플래그/이벤트)     │
│   ├─ FireEntry() ─▶ FlowContext 생성 ─▶ 토큰 코루틴       │
│   └─ EmitToken() ─▶ 연결 따라 다음 노드로 전파             │
│                                                          │
│ FlowGraphManager (BaseManager, IFlowGraphService)        │
│   └─ graphId 색인 → Svc.FlowGraph.StartGraph(id, entry)  │
└─────────────────────────────────────────────────────────┘
        ▲ Svc 계약 호출                    ▲ 발화 라우팅
┌───────┴──────────────┐    ┌─────────────┴──────────────┐
│ GlobalFlag/Quest/    │    │ FlowGraphTriggerVolume     │
│ Story/Dialogue 매니저 │    │ (씬 콜라이더 프록시)         │
└──────────────────────┘    └────────────────────────────┘
```

실행 모델: `Entry ─▶ [노드] ─▶ [노드] ...` 토큰이 실행 포트를 따라 전파한다. Branch는 True/False 중 하나, Sequence는 "1..N" 순서 방출, Join은 합류(All/Any), SubGraph는 하위 그래프 중첩 실행이다. 데이터 포트는 토큰을 만들지 않으며 `FlowDataNode`를 소비 시점에 평가한다.

### 파일 구조

```
Assets/02.Scripts/FlowGraph/
├── UPlayGround.FlowGraph.asmdef
├── FlowGraphSO.cs              # 그래프 에셋 + FlowConnection + Validate
├── FlowNode.cs                 # 노드 베이스 + FlowPortDef + FlowNodeMenu 어트리뷰트
├── FlowContext.cs              # 실행 컨텍스트 + FlowToken
├── FlowGraphRunner.cs          # 토큰 실행기 + FlowSessionState
├── FlowGraphManager.cs         # 매니저 (IFlowGraphService)
├── FlowGraphTriggerVolume.cs   # 씬 콜라이더 프록시 + OnTriggerVolumeEntryNode
├── Nodes/
│   ├── EntryNode.cs            # EntryNode 베이스 + ManualEntryNode + FlowRepeatPolicy
│   ├── CoreNodes.cs            # Sequence/Branch/Wait/Join/Gate/Log
│   ├── SubGraphNode.cs         # 중첩 그래프 호출
│   ├── FlowConditions.cs       # FlagCondition/QuestStatusCondition/StoryProgressAtLeastCondition
│   ├── FlagNodes.cs            # OnFlagChangedEntry/SetFlag/CheckFlag
│   ├── GameEventNodes.cs       # OnGameEventEntry/Publish/WaitForGameEvent + GameEventRef
│   ├── DialogueNodes.cs        # PlayDialogue
│   └── QuestStoryNodes.cs      # StartQuest/CompleteQuest/CheckQuestStatus/SetStoryProgress
└── Editor/
    ├── UPlayGround.FlowGraph.Editor.asmdef
    ├── FlowGraphEditorWindow.cs / FlowGraphView.cs / FlowNodeView.cs
    ├── FlowNodeSearchWindow.cs
    └── GameEventRefDrawer.cs

Assets/02.Scripts/Gameplay/TriggerSystem/FlowGraphTriggerBridgeNodes.cs  # TriggerAction/Condition 재활용 노드
Assets/02.Scripts/Editor/FlowGraphComposerConverter.cs                    # TriggerComposer 변환 도구
Assets/Tests/EditMode/FlowGraph/ · Assets/Tests/PlayMode/FlowGraph/       # 자동 테스트
```

---

## 핵심 클래스

### FlowGraphSO

그래프 에셋. `CreateAssetMenu: UPlayGround/FlowGraph/Graph`, 파일명 접두사 `FLOW_`.

| 필드 | 설명 |
|------|------|
| `graphId` | 매니저 등록·조회 식별자. 비우면 에셋 이름 사용 (`ResolvedGraphId`) |
| `nodes` | `[SerializeReference]` 다형 노드 리스트 |
| `connections` | 안정 Port ID를 사용하는 `fromNodeId.fromPort → toNodeId.toPort` 실행/데이터 엣지 |
| `variables` | stable ID와 기본값을 가진 그래프 로컬 Blackboard 선언 |
| `parameters` | SubGraph 외부에 공개하는 In/Out/InOut 계약 |

```csharp
public FlowNode GetNode(string nodeId);
public void GetConnectionsFrom(string nodeId, string port, List<FlowConnection> results);
public bool Validate(List<string> errors);   // ID/포트/타입/용량/고아·중복 엣지 검출
public bool TryEvaluateDataInput<T>(...);    // 연결된 Pure Data 출력을 Pull 평가
```

### FlowNode

모든 노드의 베이스. **에셋 공유 인스턴스이므로 필드에 런타임 가변 상태 금지** — 상태는 `FlowContext.GetNodeState<T>()`(발화 스코프) 또는 `FlowGraphRunner.GetRunnerNodeState<T>()`(러너 수명 스코프)로 관리한다.

```csharp
public abstract IEnumerable<FlowPortDef> Ports { get; }     // 노드가 자기 포트 선언
public abstract IEnumerator Execute(FlowToken token);       // 완료 시 token.Emit(포트명)
```

`FlowPortDef`는 영속 `Id`, 표시명, 실행/데이터 Kind, 방향, 용량, 데이터 타입을 선언한다. 표시명 변경은 저장된 연결을 끊지 않는다. Pure Data 노드는 `FlowDataNode.TryEvaluate`를 구현하고 런타임 가변 상태나 side effect를 두지 않는다.

새 노드 타입은 `[FlowNodeMenu("카테고리/이름")]` + `[Serializable]`을 붙이면 검색창에 자동 노출된다(TypeCache 스캔 — 등록 절차 없음).

### FlowGraphRunner

씬 배치 실행기. `OnEnable`에 진입점 무장 + 매니저 등록, `OnDisable`에 구독 해제 + 전체 취소(코루틴 고착 방지).

```csharp
public bool FireEntry(EntryNode entry, Action<FlowContext> configure = null);
public bool FireManualEntries(string entryId, ...);          // entryId 비면 모든 Manual
public bool FireEntries<TEntry>(Predicate<TEntry> match, ...); // 프록시 컴포넌트용
public void SetGraph(FlowGraphSO graph, bool registerToManager = true); // 비활성 상태에서만
public IReadOnlyDictionary<string, int> ActiveNodeCounts;    // 디버그 뷰 폴링용
```

### EntryNode / FlowRepeatPolicy

| 정책 | 의미 |
|------|------|
| `Once` | 러너 인스턴스 수명 동안 1회 (씬 재로드 시 리셋) |
| `OncePerSession` | 플레이 세션 동안 1회 (`FlowSessionState`, 씬 재로드에도 유지) |
| `Cooldown` | `cooldownSeconds` 간격 제한 |
| `Always` | 무제한 |

발화 상태는 러너가 노드 id 기준으로 소유한다(에셋 오염 방지). `Once`는 세이브에 영속되지 않는다 — 영속 1회가 필요하면 플래그 게이트(`CheckFlag`→`SetFlag`)를 그래프에 넣을 것.

---

## 노드 카탈로그

| 카테고리 | 노드 | 포트 | 비고 |
|----------|------|------|------|
| 진입점 | `ManualEntryNode` | Out | 코드/치트/컨버터 발화 |
| 진입점 | `OnFlagChangedEntryNode` | Out | 플래그가 지정 값으로 **변경**될 때 |
| 진입점 | `OnGameEventEntryNode` | Out | EventManager enum 이벤트 (GameEventRef) |
| 진입점 | `OnTriggerVolumeEntryNode` | Out | `FlowGraphTriggerVolume.volumeId` 매칭 |
| 코어 | `SequenceNode` | In / 1..N | 순서 보장 방출 (Out 다중 연결 = 병렬) |
| 코어 | `BranchNode` | In / True·False | `[SerializeReference] FlowCondition` |
| 코어 | `WaitTimeNode` / `WaitConditionNode` | In / Out | 시간·조건 폴링 대기 |
| 코어 | `JoinNode` | In / Out | All(유입 연결 수만큼 대기) / Any |
| 코어 | `GateNode` | In / Out | 그래프 중간 재진입 제한 |
| 코어 | `SubGraphNode` | In / Out | 하위 그래프 Manual 진입점 실행, 완료 합류 옵션. 자기참조·깊이 8 초과 거부 |
| 코어 | `LogNode` | In / Out | 디버그 |
| 플래그 | `SetFlagNode` / `CheckFlagNode` | — | `Svc.Flags` |
| 이벤트 | `PublishGameEventNode` / `WaitForGameEventNode` | — | 리플렉션으로 enum 제네릭 호출 |
| 대화 | `PlayDialogueNode` | In / Out | `Svc.Dialogue.StartDialogue` 후 `OnDialogueEnd` 대기 |
| 퀘스트 | `StartQuestNode` / `CompleteQuestNode` / `CheckQuestStatusNode` | — | `Svc.QuestFlow` |
| 스토리 | `SetStoryProgressNode` | In / Out | `Svc.StoryFlow` |
| 변수 | `SetVariableNode` / `CheckVariableNode` / `VariableCondition` | — | 그래프 Blackboard 선언 변수 참조 (미선언 이름은 검증 경고) |
| 브릿지 | `RunTriggerActionNode` / `EvaluateTriggerConditionNode` | — | 기존 `TriggerActionSO`/`TriggerConditionSO` 에셋 재활용 |

---

## 셋업 방법

1. **그래프 에셋 생성** — 에디터 창 툴바 `새 그래프` 버튼(시작 노드 자동 포함) 또는 Project 창 우클릭 → `Create > UPlayGround > FlowGraph > Graph`. `graphId` 지정(비우면 에셋명). 빈 그래프를 열면 `▶ start` Manual 진입점이 자동 생성된다.
2. **저작** — 에셋 더블클릭(또는 메뉴 `UPlayGround > Flow Graph Editor`)으로 창 열기. 툴바 `열기 ▾` 드롭다운으로 프로젝트의 다른 그래프로 즉시 전환. 좌측 노드 라이브러리 클릭 또는 빈 곳 우클릭 검색창으로 노드 생성, 포트 드래그로 연결. 우측 패널에서 선택 노드 속성 편집. 하단 검증 패널이 진입점 부재·고아 엣지·도달 불가 노드를 상시 표시하며, 행 클릭 시 해당 노드로 포커스된다. Play Mode에서는 우측 패널 하단 Blackboard 섹션에 실행 중 플로우별 블랙보드가 표시된다.
3. **씬 배치** — 빈 GameObject에 `FlowGraphRunner` 추가, `_graph`에 에셋 연결. 활성화 시 진입점이 자동 무장된다.
4. **(콜라이더 진입점 사용 시)** 트리거 콜라이더 오브젝트에 `FlowGraphTriggerVolume` 추가 → `_runner` 연결, `_volumeId`를 그래프의 `OnTriggerVolumeEntryNode.volumeId`와 일치시킨다. `_actorFilter` 기본값은 Player.
5. **(코드 발화)** `Svc.FlowGraph.StartGraph("graphId", "entryId")` — `FlowGraphManager`는 GameManager가 QuestManager 다음 순서로 자동 등록한다.

## 사용 예시

보스 처치 연출 시퀀스:

```
[Entry: Flag [bossA_down]=true] ─▶ [PlayDialogue DLG_bossA_after] ─▶ [RunAction CAM_bossA_snapshot]
                                                                            │
                                       [CompleteQuest Q_bossA] ◀── [SetFlag portal_open] ◀┘
```

코드/치트에서 발화:

```csharp
using UPlayGround.Manager;

Svc.FlowGraph.StartGraph("FLOW_BossA_Defeat");          // 모든 Manual 진입점
Svc.FlowGraph.StartGraph("FLOW_Tutorial", "step2");     // entryId 지정
```

### TriggerComposer 변환 (선택적 승격)

Hierarchy에서 `TriggerComposer` 선택 → 우클릭 `UPlayGround > TriggerComposer → FlowGraph 변환`.
Source→Entry(콜라이더 소스는 `OnTriggerVolumeEntryNode`), Condition/Action SO는 브릿지 노드로 그대로 재활용된다. **원본 Composer는 유지되므로 중복 발화 방지를 위해 한쪽만 활성화할 것.**

---

### 에디터 UX (FlowCanvas 레퍼런스 반영)

- **Blackboard 변수** — 좌측 하단 Blackboard 패널에서 이름/타입(Bool·Int·Float·String)/기본값을 선언한다. 발화마다 `FlowContext`에 기본값 사본이 만들어지므로 실행 간 오염이 없다. `SetVariable`/`CheckVariable` 노드와 `VariableCondition`이 이름으로 참조하며, 미선언 이름은 검증 패널이 경고한다. Play Mode 값은 우측 인스펙터 하단 실행 컨텍스트 뷰에서 확인.
- **Blackboard 저작 안전성** — 변수 이름 변경 시 `SetVariable`/`CheckVariable`/`VariableCondition` 참조가 함께 변경된다. 변수 카드의 `사용 N`으로 참조 노드에 이동할 수 있고, 사용 중인 변수 삭제에는 확인 창이 표시된다. 타입 변경은 참조 값 타입에도 동기화되며, 빈 이름·중복 이름·타입 불일치는 검증 패널에서 확인한다. 검색창은 이름과 타입을 모두 필터링한다.
- **포트 드래그 생성** — 포트에서 엣지를 끌어 빈 캔버스에 놓으면 검색창이 열리고, 선택한 노드가 원점 포트와 자동 연결된다.
- **그룹** — 노드 다중 선택 후 캔버스 우클릭 → `그룹 생성`. 타이틀·멤버·위치가 에셋(`editorGroups`)에 저장된다.
- **브레이크포인트** — 노드 우클릭 → `브레이크포인트 설정`(타이틀에 빨간 점). Play Mode에서 토큰 도착 시 에디터가 일시정지(`Debug.Break`)된다.
- **노드 본문 파라미터 요약** — 노드 본문에 직렬화 필드가 key-value로 표시되어 클릭 없이 캔버스에서 그래프 로직을 읽을 수 있다 (BT Summary 이식, 최대 6행).
- **복사/붙여넣기/복제** — Ctrl+C/V/D. 선택 노드와 내부 연결이 함께 복제되며(새 id 부여), 다른 그래프로의 붙여넣기도 가능하다.
- **변수 이름 드롭다운** — `[FlowVariableName]` string 필드는 Blackboard 선언 목록 드롭다운으로 편집한다. 미선언 값은 "(미선언)"으로 표기되어 보존된다.
- **서브그래프 진입** — SubGraph 노드 더블클릭으로 하위 그래프를 열고, 툴바 `←` 버튼/브레드크럼(`상위 ▸ 하위`)으로 복귀한다.
- **블랙보드 실시간 값** — Play Mode에서 좌측 Blackboard 패널의 각 변수 옆에 실행 중 첫 컨텍스트의 현재 값이 초록색으로 표시된다.
- **검증 배지** — 문제 있는 노드 우상단에 ✕(Error)/!(Warning) 배지가 캔버스에 직접 표시된다.
- **실행 경로 시각화** — Play Mode에서 최근 실행 노드는 주황 잔광(2.5초 페이드아웃), 최근 토큰이 통과한 엣지는 두껍게 하이라이트된다. 순간 통과 노드도 경로가 보인다.
- **Wait 진행 바** — 대기 중인 Wait(Time) 노드 하단에 진행률 바가 표시된다.
- **포트 색 구분** — True=초록, False=빨강. 엣지가 포트 색을 상속해 분기 와이어도 구분된다.
- **노드 라벨/메모** — 인스펙터의 `editorLabel`로 타이틀을 사용자 이름으로 바꾸고, `editorComment`는 노드 위 말풍선으로 표시된다(실행 무관).
- **컴팩트 모드** — 툴바 `컴팩트` 토글로 본문 요약을 일괄 숨겨 큰 그래프를 조망한다.
- **진입점 실루엣** — 시작 노드는 색 외에 좌측 액센트 바로도 구분된다(색약 접근성).

---

## 주의 사항

- **노드 클래스 이동/리네임** — `[SerializeReference]` 참조가 끊긴다. 다른 어셈블리로 이동 시 `[MovedFrom(true, sourceAssembly: "...")]` 필수 (CLAUDE.md 규약).
- **노드 필드에 런타임 상태 금지** — 노드 인스턴스는 에셋 공유. Join 카운트·Gate 쿨다운처럼 상태가 필요하면 `FlowContext`/러너 스코프 상태 API를 쓴다.
- **Wait 노드와 씬 전환** — 러너 `OnDisable`이 컨텍스트를 취소하고 코루틴을 정지한다. DontDestroyOnLoad 러너에 씬 종속 연출을 넣지 말 것.
- **`Svc` 지연 조회** — 노드는 실행 시점에 서비스를 조회하므로 매니저 초기화 순서 문제는 없지만, 서비스 미등록이면 경고 후 통과(고착 방지)한다.
- **Join은 같은 FlowContext 안에서만 합류** — 서로 다른 진입점 발화(다른 컨텍스트)의 토큰은 합류하지 않는다.
- **이벤트 노드의 enum 해석** — `GameEventRef`는 타입/값을 문자열로 저장하고 런타임 리플렉션으로 해석한다. 인스펙터 드롭다운(`GameEventRefDrawer`)으로 저작해 오타를 방지할 것. enum 리네임 시 그래프도 함께 수정해야 한다.
- **디버그 뷰 성능** — 활성 노드 하이라이트는 `DebugVersion` 게이트 + 증분 diff. 노드 뷰를 매 폴링마다 전체 재스타일하는 코드를 추가하지 말 것 (BT 에디터 프레임드랍 전례).

## 확장 포인트

- **새 액션/브릿지 노드** — `FlowNode` 상속 + `[FlowNodeMenu]`. 매니저 접근은 반드시 `Contracts`의 계약으로. 계약이 없으면 `Contracts`에 추가하고 매니저가 구현한다(FlowGraph asmdef가 구체 매니저를 참조하지 않게). 다른 asmdef에서 상속해도 TypeCache 스캔으로 에디터에 자동 노출된다(등록 절차 없음, 단 런타임 asmdef에 둘 것).
- **노드 아이콘/컬러 커스터마이즈** — 노드 클래스에 `[FlowNodeStyle(Icon = "PlayButton", HeaderColor = "#2E6B3A")]`(Inherited — 베이스에 붙이면 파생 전체 적용), 카테고리 단위는 어셈블리 레벨 `[assembly: FlowNodeCategoryStyle("내카테고리", "#7A3B5E", Icon = "Favorite")]`(내장 팔레트보다 우선). Icon은 Unity 빌트인 아이콘 이름 또는 프로젝트 텍스처 경로(`Assets/...`)를 지원하며, 해석 실패 시 조용히 아이콘 없이 표시된다. 아이콘은 노드 타이틀·라이브러리·검색창에 함께 노출된다.
- **새 진입점** — `EntryNode` 상속, `Arm(runner)`에서 구독 후 `runner.StoreEntryTeardown(this, 해제동작)` 등록. 발화는 `runner.FireEntry(this)`.
- **새 조건** — `FlowCondition` 상속. `BranchNode`/`WaitConditionNode`의 `[SerializeReference]` 필드에서 자동 선택 가능(인스펙터 드롭다운은 Unity 기본 SerializeReference UI).
- **Assembly-CSharp 전용 타입이 필요한 노드** — FlowGraph asmdef 밖(예: `Gameplay/`)에 노드 클래스를 둬도 된다. `[SerializeReference]`는 어셈블리 무관 (`FlowGraphTriggerBridgeNodes.cs` 참조).

## 테스트

- EditMode `Assets/Tests/EditMode/FlowGraph/` — 직렬화 왕복, 고아 엣지 검증, 연결 조회 (3개)
- PlayMode `Assets/Tests/PlayMode/FlowGraph/` — 진입점 발화→5노드 시퀀스 완주, OnFlagChanged 발화, Once 정책 차단 (3개, 페이크 서비스 격리 실행)
