# 노드 기반 게임 흐름 제어 시스템 설계 (FlowGraph)

> UI Toolkit + GraphView로 저작하는 **게임 흐름 제어 노드 그래프** 시스템 설계 문서.
> 현재 프로젝트에 흩어져 있는 흐름 제어 로직 — `TriggerSystem`(Source→Condition→Action), Story/Quest 진행, Dialogue 분기, Cycle 진행, Flag/Event 배선 — 을
> 하나의 **시각적 노드 그래프**로 저작·검증·디버깅할 수 있는 공용 오케스트레이션 계층을 도입한다.
> 별도 asmdef(`UPlayGround.FlowGraph` / `UPlayGround.FlowGraph.Editor`)로 분리하여 모듈 경계를 지키고, 필요한 경우 기존 `TriggerSystem` 자산을 노드 그래프로 마이그레이션한다.
>
> **구현 상태 (2026-07-21):** P0~P5 전 단계 구현 완료 — `Assets/02.Scripts/FlowGraph/`(런타임), `FlowGraph/Editor/`(에디터 창, 레이아웃은 `Assets/docs/design/flow_graph_시안.png` 기준: 노드 라이브러리/컬러 헤더/검증 패널/상태바), `Gameplay/TriggerSystem/FlowGraphTriggerBridgeNodes.cs`(TriggerAction/Condition 재활용 노드), `Editor/FlowGraphComposerConverter.cs`(TriggerComposer 변환), SubGraph 중첩 실행, GameEventRef 리플렉션 기반 OnGameEvent/Publish/WaitForGameEvent 노드, EditMode 3개 + PlayMode 수직 슬라이스 3개 테스트. Contracts에 `IGlobalFlagService`/`IQuestFlowService`/`IStoryFlowService`/`IFlowGraphService` 추가. 가이드: `Assets/docs/guide/FLOWGRAPH_SYSTEM_GUIDE.md`. FlowCanvas 레퍼런스 반영(2026-07-21): 그래프 Blackboard 선언 변수(4.4 도입 — `FlowVariableDef`+Set/Check 변수 노드+에디터 패널), 포트 드래그 노드 생성·자동 연결, 그룹(editorGroups 배선), 노드 브레이크포인트. 에디터 2차 개선(BT/Unreal/FlowCanvas 갭 분석 반영): 노드 본문 파라미터 요약(BT Summary 이식)+카테고리 색 세분화, 복붙/복제(EditorJsonUtility 딥카피), [FlowVariableName] 변수 드롭다운, SubGraph 더블클릭 진입+브레드크럼, Blackboard 패널 실시간 값. 잔여: 데이터 포트(값 배선), 리라우트/정렬/그래프 내 검색, 스텝 디버깅, Graph Toolkit 이관(13절). Unity 컴파일/에디터 검증 대기.

---

## 1. 배경 & 목표

### 1.1 현재 흐름 제어의 분산 현황

현재 "게임 흐름"을 제어하는 로직은 여러 서브시스템에 **명령형·분산 형태**로 존재한다.

| 영역 | 현재 저작 방식 | 위치 |
|------|----------------|------|
| 씬/트리거 이벤트 | `TriggerComposer`(씬 컴포넌트) = Source 1 + Condition 1 + Action 1, 재진입 정책 | `Gameplay/TriggerSystem/` |
| 조건/액션 조합 | `CompositeTriggerConditions` / `CompositeTriggerActions`(AND/OR/Sequence) | 동일 |
| 대화 분기 | `DialogueGraphSO` + `DialogueNodeSO`(문자열 ID 라우팅, GraphView 에디터) | `Data/Dialogue/`, `Editor/DialogueGraphEditor.cs` |
| 스토리 진행 | `StoryManager` + `StoryEntrySO`, `StoryProgressTriggerConditionSO` | `Manager/Story/`, `Data/Story/` |
| 퀘스트 | `QuestManager` + `QuestSO`/`QuestDatabase`, 목표/보상 데이터 | `Manager/Quest/`, `Data/Quest/` |
| 플래그 | `GlobalFlagManager`, `SetFlagTriggerActionSO`/`GlobalFlagTriggerConditionSO` | `Manager/Dialogue/`, `TriggerSystem/` |
| 이벤트 배선 | `EventManager`(enum+payload pub/sub, scene/global 테이블) | `Manager/Event/` |
| 사이클 진행 | `CycleRunManager`/`BossAssistManager`/`CycleRemainsManager` | `Manager/Cycle*` |

문제는 이들이 **"무엇을 언제 실행하는가"라는 매크로 흐름을 한눈에 볼 수 없다**는 점이다. 예를 들어
"보스 A 처치 → 플래그 세팅 → 대화 재생 → 카메라 스냅샷 → 포털 활성화 → 퀘스트 완료"라는 하나의 연출 시퀀스는
지금은 여러 `TriggerComposer`, `DialogueActionSO`, 매니저 호출에 쪼개져 있어 저작·추적·디버깅이 어렵다.

### 1.2 목표

1. **시각적 저작** — 게임 흐름(연출/진행/조건 분기)을 노드 그래프로 그리고, 실행 순서를 눈으로 확인한다.
2. **시스템 브릿지** — 노드에서 기존 매니저(Quest/Story/Dialogue/Cycle/Flag/Event/Camera/Spawn)를 안전하게 호출한다. 매니저를 재작성하지 않는다.
3. **런타임 실행 + 라이브 디버깅** — 그래프를 런타임에서 실행하고, 활성 노드/토큰 흐름을 에디터에서 하이라이트한다(BT 디버그 viz와 동일한 사용성).
4. **모듈 경계 준수** — 새 asmdef에서 구체 매니저 싱글톤을 직접 참조하지 않고 `Contracts`의 `Svc`/공용 계약을 통한다.
5. **점진 도입** — 기존 시스템을 깨지 않고 상위 오케스트레이션 계층으로 얹는다. `TriggerSystem`은 선택적 마이그레이션.

### 1.3 비목표 (Non-goals)

- **AI 행동 결정 대체 아님.** 전투 AI는 계속 `BehaviorTree`가 담당한다. FlowGraph는 매크로 게임 진행/연출 오케스트레이션 전용.
- **대화 그래프 즉시 대체 아님.** `DialogueGraphSO`는 대화 특화 저작 도구로 유지. FlowGraph는 대화를 "재생"하고 결과를 받는 노드로 연동한다(11절).
- **범용 비주얼 스크립팅(Unity Visual Scripting/Bolt 대체) 아님.** 데이터 연산 그래프가 아니라 **이벤트 구동 흐름 그래프**에 한정한다.
- **런타임 노드 편집(인게임 에디터) 아님.** 저작은 에디터 전용, 런타임은 실행+디버그 뷰만.

---

## 2. 웹 리서치 요약

### 2.1 Unity 노드 에디터 기술 선택지 (2026-07 기준)

| 기술 | 상태 | 성격 | 직렬화 | 판단 |
|------|------|------|--------|------|
| **GraphView** (`UnityEditor.Experimental.GraphView`) | 빌트인, "Experimental" 네임스페이스지만 수년간 안정적 (Shader Graph/VFX Graph/구 Visual Scripting 기반) | UI 라이브러리(노드/포트/엣지/미니맵/그룹). 실행·직렬화는 직접 구현 | 델리게이트 기반 복붙만 제공, 나머지 자체 구현 | **채택.** 프로젝트가 이미 BT·Dialogue 두 곳에서 사용 중 → 재사용 가능한 노하우/코드 존재 |
| **Graph Toolkit** (`com.unity.graphtoolkit`) | **0.1.0-exp.1 실험판.** 런타임 실행 백엔드 미포함(에디터 전용) | 차세대 프레임워크. `[Graph]` 어트리뷰트, JSON/바이너리 직렬화, undo/redo 내장 | 프레임워크 제공(우수) | **보류.** 아직 실험판·API 불안정. 1인 프로덕션에 지금 도입은 리스크. 향후 정식화 시 재평가(13절) |
| **NodeGraphProcessor** (alelievr, MIT) | 서드파티, 성숙 | `BaseGraph`/`BaseNode`/`Port`, 의존성 순서 `Process()` 실행. **데이터 처리 지향** | ScriptableObject + 노출 파라미터 | **미채택.** 데이터-풀(pull) 모델이라 이벤트-구동 흐름과 결이 다름. 외부 의존성 추가 부담 |

**결론:** 프로젝트에 이미 두 개의 GraphView 에디터(`BehaviorTreeGraphView`, `DialogueGraphEditor`)가 있고, 그룹/미니맵/블랙보드/클립보드/증분 디버그 하이라이트까지 구현된 자산이 있다.
동일한 **빌트인 GraphView** 위에 FlowGraph를 세우면 학습 곡선·의존성·유지보수 리스크가 가장 낮다. 실행 모델만 흐름 그래프에 맞게 새로 설계한다.

### 2.2 흐름 그래프 실행 모델 참고 패턴

- **토큰(펄스) 기반 실행** — 데이터-풀 방식(NodeGraphProcessor)이 아니라, 진입점에서 **실행 토큰**을 흘려보내고 각 노드가 완료되면 출력 포트로 토큰을 전달하는 **이벤트 구동 방식**. (Unreal Blueprint의 실행 핀, Unity 6 Behavior의 흐름과 동일 계열)
- **동기/비동기 노드 구분** — 즉시 완료(플래그 세팅)와 대기(대화 재생 완료까지, N초 대기, 조건 충족까지)를 노드 단위로 구분. 코루틴/`async`로 대기 노드를 표현.
- **분기/병렬** — Branch(조건), Sequence(순차), Parallel(동시 실행 후 All/Any 합류), Wait(신호/시간).
- **재진입 정책** — 기존 `TriggerRepeatPolicy`(Once/OncePerSession/Cooldown/Always) 개념을 진입점 노드에 계승.

**출처(Sources):**
- [Unity Manual — Extending the Editor with Graph Toolkit](https://docs.unity3d.com/6000.4/Documentation/Manual/gtk/gtk-index.html)
- [Graph Toolkit 0.1.0-exp.1 — Introduction / Get started coding](https://docs.unity3d.com/Packages/com.unity.graphtoolkit@0.1/manual/introduction.html)
- [alelievr/NodeGraphProcessor (MIT)](https://github.com/alelievr/NodeGraphProcessor)
- [UnityCsReference — GraphView.cs](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/GraphViewEditor/Views/GraphView.cs)

---

## 3. 현재 프로젝트 자산 분석 (재사용 대상)

새로 만들지 않고 **재사용/참고**할 기존 자산:

| 자산 | 재사용 포인트 |
|------|--------------|
| `BehaviorTreeGraphView` (+ `.Editing`/`.Clipboard`) | GraphView 셋업, 노드 검색창(`BehaviorTreeNodeSearchWindow`), 그룹(`BehaviorTreeGroupView`), 미니맵, 복붙 패턴 |
| `BehaviorTreeEditorWindow.Debug.cs` | **런타임 활성 노드 하이라이트**의 증분 diff 패턴. `project_bt_editor_debug_perf` 메모리의 교훈(매 갱신마다 전체 재스타일 금지) 그대로 계승 |
| `BehaviorTreeInspectorView` | 노드 선택 시 인스펙터 임베드 패턴 |
| `DialogueGraphSO`/`DialogueNodeSO` | 노드 그래프 데이터 모델·문자열 ID 라우팅·`editorPosition` 저장 패턴 |
| `TriggerContext` | 발화 컨텍스트·공유 데이터 채널 설계(FlowContext의 원형) |
| `TriggerConditionSO`/`TriggerActionSO` + Composite | 조건/액션 SO를 **노드로 래핑**해 재사용 가능 (마이그레이션 시 자산 재활용) |
| `EventManager` (`IGameEventObservable`/`Publisher`) | 이벤트 대기/발행 노드의 백엔드 |
| `Svc`/`Services` (Contracts) | 노드에서 매니저 접근하는 유일 경로 |

> **핵심 판단:** BT와 Dialogue가 각각 **SO-서브에셋-per-노드**(`BTNode : ScriptableObject`, `DialogueNodeSO : ScriptableObject`)를 쓴다.
> FlowGraph는 대신 **`[SerializeReference]` 다형 노드 + 단일 에셋** 방식을 권장한다(4.2). 서브에셋 고아(orphan) 관리·에셋 diff 복잡도를 피하고, 노드 타입 추가가 클래스 하나로 끝난다.
> 단, `[SerializeReference]` 노드 클래스를 다른 asmdef로 이동할 때는 CLAUDE.md 규약대로 `[MovedFrom(true, sourceAssembly:...)]`를 반드시 유지한다.

---

## 4. 제안 아키텍처

### 4.1 어셈블리 구성

```
UPlayGround.FlowGraph         (runtime asmdef)
  └ 참조: UPlayGround.Core, UPlayGround.Data, UPlayGround.Contracts
     (구체 매니저 asmdef는 참조하지 않음 — Svc/공용 계약만 사용)

UPlayGround.FlowGraph.Editor  (editor asmdef, editor-only)
  └ 참조: UPlayGround.FlowGraph, UPlayGround.Data, UPlayGround.Contracts,
          (에디터 유틸 공유가 필요하면) 기존 에디터 asmdef
```

- 브릿지 노드가 특정 시스템 계약(예: Quest 상태 조회)을 필요로 하는데 `Contracts`에 없다면, **계약을 `Contracts`에 추가**하고 매니저가 구현하게 한다. FlowGraph asmdef가 구체 매니저를 참조하도록 경계를 뚫지 않는다.
- Camera 연동은 Camera 모듈 규약(`ICameraRuntimeAdapter`)을 통해서만. FlowGraph는 카메라 스냅샷/녹화 재생을 기존 `TriggerActionSO`(예: `PlayCameraSnapshotTriggerActionSO`) 래핑 노드로 우회하는 것을 1차로 한다.

### 4.2 데이터 모델

```csharp
// UPlayGround.FlowGraph (runtime)

[CreateAssetMenu(menuName = "UPlayGround/FlowGraph/Graph", fileName = "FLOW_")]
public sealed class FlowGraphSO : ScriptableObject
{
    public string graphId;
    [SerializeReference] public List<FlowNode> nodes = new();   // 다형 노드, 단일 에셋
    public List<FlowConnection> connections = new();            // 포트 간 엣지
    public List<FlowGraphGroup> editorGroups = new();           // BT 그룹과 동형
    // 실행 진입점은 EntryNode 타입 노드들이 스스로 표식
}

[Serializable]
public sealed class FlowConnection            // outNodeId.outPort -> inNodeId.inPort
{
    public string fromNodeId; public string fromPort;
    public string toNodeId;   public string toPort;
}

[Serializable]
public abstract class FlowNode
{
    public string id = Guid.NewGuid().ToString("N");
    public Vector2 editorPosition;            // 에디터 전용
    public abstract IEnumerable<FlowPortDef> Ports { get; }   // 노드가 자기 포트 선언
}
```

- **실행 포트 vs 데이터 포트:** 1차 범위는 **실행(흐름) 포트만** 지원한다(토큰이 흐르는 핀). 노드 파라미터는 인스펙터 필드로 저작한다.
  값 배선(데이터 포트)은 과설계가 되기 쉬우므로 필요성이 확인되면 Phase 3에서 블랙보드로 도입(4.4).
- **직렬화 위험 관리:** `[SerializeReference]`는 클래스 리네임/이동 시 참조가 끊긴다. 노드 클래스에 `[MovedFrom]` 유지, 노드 카탈로그를 문서화(7절)한다.

### 4.3 런타임 실행 — FlowGraphRunner

```csharp
public sealed class FlowGraphRunner : MonoBehaviour   // 또는 매니저 소유 풀 실행기
{
    // 그래프를 로드하고 EntryNode에서 토큰을 흘려보낸다.
    // 각 노드: Enter(FlowContext) -> (동기 완료 | 코루틴/async 대기) -> 완료 포트로 토큰 전달
    // 활성 토큰 집합을 유지 → 에디터 디버그 뷰가 폴링/구독하여 하이라이트
}
```

- **FlowContext** — `TriggerContext`를 일반화한 실행 컨텍스트. 발화 원인, 관련 Actor/Group, 그래프-스코프 블랙보드, 취소 토큰을 담는다. 공유 SO에 가변 상태를 두지 않기 위해 실행마다 새 컨텍스트(기존 `TriggerContext` 설계 계승).
- **실행 소유권** — 그래프 실행기는 `GameManager` 산하 신규 경량 매니저(`FlowGraphManager`, `IManager`/`BaseManager<T>`)가 관리한다. 씬 스코프 그래프와 글로벌 그래프를 구분해 씬 전환 시 정리한다(`OnSceneChanged`).
- **대기 노드 취소** — 씬 전환/비활성화 시 실행 중 코루틴이 고착되지 않도록 `TriggerComposer.OnDisable`의 `_isExecuting` 리셋 교훈을 계승해 취소 토큰으로 정리.

### 4.4 (Phase 3 옵션) 블랙보드/데이터 포트

BT `Blackboard`와 동형의 그래프-스코프 변수 저장소. 조건 노드가 값을 읽고, 액션 노드가 값을 쓴다.
1차 릴리스에는 포함하지 않고, "값을 노드 간에 전달해야 하는" 실제 요구가 2건 이상 쌓이면 도입한다(YAGNI).

---

## 5. 노드 실행 모델 상세

```
[EntryNode(OnBossDefeated)] ──▶ [SetFlag "bossA_down"] ──▶ [PlayDialogue "DLG_bossA_after"]
                                                                     │(완료)
                                                                     ▼
                                              [PlayCameraSnapshot] ──▶ [ActivatePortal] ──▶ [CompleteQuest "Q_bossA"]
```

- 토큰이 진입점에서 출발 → 각 노드의 출력 실행 포트로 순차 전파.
- **Branch 노드**: 조건 평가 후 `True`/`False` 포트 중 하나로 토큰 전달.
- **Parallel 노드**: 여러 출력 포트로 동시에 토큰 방출, `Join(All|Any)` 노드에서 합류.
- **Wait 노드**: 시간/이벤트/조건 충족까지 토큰 보류(대기 상태) 후 통과.
- **동시 다중 토큰** 허용(여러 진입점, Parallel). 노드 별 활성 토큰 수를 디버그 뷰에서 카운트.

---

## 6. 진입점(트리거) 소스

`TriggerSourceSO` 계열을 그래프 진입점 노드로 흡수/연동한다.

| EntryNode | 대응 기존 소스 | 발화 시점 |
|-----------|----------------|-----------|
| `OnColliderEnter` / `OnColliderExit` | `ColliderEnter/ExitTriggerSourceSO` | 씬 볼륨 진입/이탈(씬 바인딩 필요, 8절) |
| `OnGroupDefeated` | `GroupDefeatedTriggerSourceSO` | 몬스터 그룹 전멸 |
| `OnGameEvent` | `EventManager` 구독 | 특정 enum 이벤트 발행 시 |
| `OnFlagChanged` | `GlobalFlagManager` | 플래그 변화 |
| `OnCyclePhase` | `CycleRunManager` | 사이클 페이즈 전이(중앙 보스 처치 등) |
| `OnQuestStatus` | `QuestManager` | 퀘스트 상태 변화 |
| `Manual/External` | API 호출 | 코드/치트에서 명시 시작 |

씬 바인딩이 필요한 소스(콜라이더)는 **씬의 프록시 컴포넌트**(`FlowGraphTriggerVolume`)가 콜라이더 이벤트를 그래프 진입점에 라우팅한다. `TriggerComposer`가 씬 콜라이더에서 발화하던 것과 동일 원리.

---

## 7. 노드 카탈로그 (초안)

시스템별 브릿지 노드. 모두 `Svc`/공용 계약을 통해 호출.

**흐름 제어(코어)**
`Entry*`, `Sequence`, `Branch(Condition)`, `Parallel`, `Join(All/Any)`, `Wait(Time)`, `Wait(Event)`, `Wait(Condition)`, `Gate(RepeatPolicy)`, `SubGraph(중첩 그래프 호출)`, `Comment/Group`.

**플래그/이벤트**
`SetFlag`, `CheckFlag(Branch)`, `PublishGameEvent`, `WaitForGameEvent`.

**대화/스토리**
`PlayDialogue(graphId)` → 완료/선택결과 포트, `AdvanceStory`, `CheckStoryProgress(Branch)`.

**퀘스트**
`StartQuest`, `CompleteQuest`, `UpdateObjective`, `GrantReward`, `CheckQuestStatus(Branch)`.

**사이클/보스**
`CheckCyclePhase(Branch)`, `TriggerCycleSettlement`, `EquipBossAssist`(주의: BossAssist 영입은 `BossRecruitmentService` 경로, 파티 해금과 혼동 금지 — CLAUDE.md).

**연출/월드**
`PlayCameraSnapshot`, `PlayDialogueCameraRecording`, `SpawnActors`, `ActivateGroup`, `ShowGuidePopup`(각각 기존 `TriggerActionSO` 래핑으로 시작).

**컨트롤/입력**
`SetInputLayer`, `LockPlayer`, `ShowHudMessage`.

> 1차 구현 우선순위: **코어 흐름 + 플래그/이벤트 + 대화 + 퀘스트**. 나머지는 기존 `TriggerActionSO`를 감싸는 범용 `RunTriggerAction` 노드 하나로 커버하고 점진 전용화.

---

## 8. 마이그레이션 전략

### 8.1 TriggerSystem → FlowGraph

기존 `TriggerComposer`(Source+Condition+Action, 1:1:1)는 FlowGraph의 **가장 단순한 특수 케이스**(Entry→Branch→Action 3노드 선형)다.

- **호환 유지:** `TriggerComposer`는 **그대로 유지**한다. 삭제하지 않는다. 단순 트리거는 계속 씬 컴포넌트로 저작 가능.
- **재사용:** 기존 `TriggerConditionSO`/`TriggerActionSO` 에셋은 `RunTriggerAction`/`EvaluateTriggerCondition` 범용 노드로 **재활용**(자산 재생성 불필요).
- **선택적 변환 도구(에디터):** 한 `TriggerComposer` → 3노드 FlowGraph로 뽑아주는 컨버터를 제공(복잡 연출을 그래프로 승격할 때만 사용).
- **판단 기준:** 2~3스텝 이하 단순 반응 = `TriggerComposer` 유지. 다단계 연출/분기/대기가 있는 매크로 시퀀스 = FlowGraph.

### 8.2 Dialogue 연동 (대체 아님)

`DialogueGraphSO`는 대화 특화 저작으로 유지. FlowGraph의 `PlayDialogue` 노드가 `DialogueManager`로 재생을 위임하고, 선택 결과를 출력 포트로 받아 이후 흐름을 분기한다. 대화 내부 분기는 Dialogue 그래프가, 대화 전후 매크로 흐름은 FlowGraph가 담당.

### 8.3 비파괴 원칙

- 매니저 API 재작성 없음. 노드는 기존 매니저를 **호출만** 한다.
- 기존 세이브/플래그/퀘스트 데이터 포맷 변경 없음.
- 롤백 용이: FlowGraph 미사용 시 기존 경로가 100% 그대로 동작.

---

## 9. 구현 단계 계획

| Phase | 범위 | 산출물 | 검증 |
|-------|------|--------|------|
| **P0** | asmdef 2개 + 데이터 모델(`FlowGraphSO`/`FlowNode`/`FlowConnection`) + 코어 흐름 노드(Entry/Sequence/Branch/Wait) | 컴파일되는 런타임 골격 | `dotnet build` per-asmdef |
| **P1** | GraphView 에디터 창(노드 생성/연결/그룹/미니맵/저장) — BT 에디터 코드 참조 | 그래프 저작 가능 | 에디터에서 그래프 왕복 저장 |
| **P2** | `FlowGraphRunner` + `FlowGraphManager`(GameManager 등록) + FlowContext + 진입점 소스 배선 | 런타임 실행 | PlayMode 수직 슬라이스 1개 |
| **P3** | 브릿지 노드(플래그/이벤트/대화/퀘스트) + `RunTriggerAction` 범용 노드 | 실사용 노드 세트 | 실제 연출 1건 그래프화 |
| **P4** | 런타임 디버그 하이라이트(활성 토큰 viz, 증분 diff) + 그래프 검증기 | 라이브 디버깅 | BT 디버그 성능 교훈 회귀 확인 |
| **P5** | TriggerComposer→FlowGraph 컨버터 + SubGraph(중첩) + 문서/온보딩 | 마이그레이션 도구 | 샘플 트리거 변환 |

각 Phase는 독립 컴파일·검증 가능하도록 쪼갠다. Phase 3까지가 MVP.

---

## 10. 어셈블리·의존성 검증 체크리스트

- [ ] `UPlayGround.FlowGraph`(runtime)가 구체 매니저 asmdef를 참조하지 않는다 — `Svc`/`Contracts`만.
- [ ] 노드가 필요로 하는 계약이 `Contracts`에 없으면 계약을 추가하고 매니저가 구현(경계 우회 금지).
- [ ] `[SerializeReference]` 노드 클래스 이동 시 `[MovedFrom(true, sourceAssembly:...)]` 부착.
- [ ] Camera 연동은 `ICameraRuntimeAdapter`/기존 카메라 `TriggerActionSO` 경유.
- [ ] `Object` 무자격 참조 금지 — static 코드는 `UnityEngine.Object` 명시(`project_object_namespace_collision` 교훈).
- [ ] 새 `CreateAssetMenu`는 `UPlayGround/FlowGraph/<Item>` 2단계 규약 준수(`project_createassetmenu_taxonomy`).

---

## 11. 리스크 & 오픈 이슈

| 리스크 | 대응 |
|--------|------|
| `[SerializeReference]` 리네임/이동 시 참조 유실 | `[MovedFrom]` 규율 + 노드 카탈로그 문서화 + 저장 시 null 노드 검증기 |
| GraphView "Experimental" API 변경 | 프로젝트가 이미 의존 중이라 신규 리스크 아님. Graph Toolkit 정식화 시 이관 재평가 |
| 그래프 실행과 기존 매니저 초기화 순서 | `FlowGraphManager`를 매니저 등록 순서 후반부에 배치, `Svc.Get<T>` 지연 조회 |
| 대기 노드 코루틴 고착(씬 전환/비활성화) | 취소 토큰 + `_isExecuting` 리셋 패턴 계승 |
| 디버그 viz 프레임 드랍(BT 전례) | 증분 diff·force-on-clear 패턴 그대로 적용, `hasFocus` 가드 금지 |
| 과설계(데이터 포트/블랙보드 조기 도입) | 1차는 실행 포트만. 블랙보드는 실수요 2건 이후 |
| Dialogue/Flow 책임 경계 모호 | "대화 내부 분기=Dialogue, 매크로 전후=Flow" 규칙 문서 고정 |

**오픈 이슈(결정 필요)**
1. 노드 직렬화: `[SerializeReference]` 단일 에셋(권장) vs 기존 관례인 SO-서브에셋. → **권장안 채택 여부**.
2. 그래프 실행 소유: 전용 `FlowGraphManager` vs 기존 `EventManager`/`StoryManager` 확장. → **신규 매니저 권장**.
3. 진입점 씬 바인딩: 프록시 컴포넌트 vs 그래프에 씬 참조 직접 보유. → **프록시 권장**(그래프 이식성).
4. `TriggerSystem` 최종 지향: 영구 병존 vs 장기 FlowGraph 흡수. → 1차는 **병존**.

---

## 12. 테스트 전략

- **EditMode:** 그래프 직렬화 왕복(저장/로드), 연결 유효성(고아 포트/순환 감지), 컨버터 정확성.
- **PlayMode 수직 슬라이스:** 진입점 발화 → 3~5노드 시퀀스 완주 → 플래그/퀘스트 상태 반영 확인(파티/Ability 테스트 관례 따름).
- **회귀:** 디버그 뷰 열린 상태 프레임 성능(BT 회귀 항목 재사용).

---

## 13. 향후: Graph Toolkit 이관 여지

Graph Toolkit(`com.unity.graphtoolkit`)이 정식(비-experimental)화되고 런타임/직렬화가 안정화되면,
FlowGraph의 **저작 프론트엔드**만 GraphView→Graph Toolkit으로 교체하는 것을 고려할 수 있다.
그래서 본 설계는 **데이터 모델(FlowGraphSO/FlowNode)과 실행기(FlowGraphRunner)를 에디터 프레임워크와 분리**해 둔다 —
에디터 교체가 런타임에 영향을 주지 않도록. 현시점 도입은 실험판 리스크로 보류.

---

## 부록 A. 참고 파일 (현 프로젝트)

- `Gameplay/TriggerSystem/TriggerComposer.cs`, `TriggerContext.cs`, `CompositeTrigger*.cs`
- `GameActor/Editor/BehaviorTreeGraphView*.cs`, `BehaviorTreeEditorWindow.Debug.cs`, `BehaviorTreeNodeSearchWindow.cs`
- `GameActor/AI/BehaviorTree/Runtime/BehaviorTreeAsset.cs` (그룹/클론/디스포즈 패턴)
- `Data/Dialogue/DialogueGraphSO.cs`, `DialogueNodeSO.cs`, `Editor/DialogueGraphEditor.cs`
- `Manager/Event/EventManager.cs`, `Manager/Dialogue/GlobalFlagManager.cs`
- `Contracts`(`Svc`/`Services`), CLAUDE.md 모듈 경계 규약
