# Behavior Tree 시스템 통합 가이드

> 최초 작성: 2026-04-28 / 통합 갱신: 2026-05-16 (2차)
> 대상 버전: Unity 6 (6000.0.60f1), URP
> 적용 범위: `Assets/02.Scripts/AI/BehaviorTree/` 커스텀 BT 런타임·에디터, `Assets/02.Scripts/GameActor/Component/Enemy/` Enemy AI, BT 기반 몬스터 행동 데이터 파이프라인
>
> 이 문서는 다음 세 문서를 통합한 단일 가이드다.
> - 구 `BEHAVIOR_TREE_REFERENCE_GAP_IMPLEMENTATION_GUIDE.md` (2026-04-28) — Behavior Designer 레퍼런스 대비 누락 기능 보강 계획
> - 구 `BEHAVIOR_TREE_AAA_REFERENCE_ANALYSIS.md` (2026-05-11) — AAA 사례와의 갭 분석, EnemyAIController 통합 방향
> - 구 `MONSTER_AI_BEHAVIOR_TREE_FULL_CONVERSION_GUIDE.md` (2026-05-11) — 몬스터 AI를 BT로 완전 전환하는 실행 계획
>
> 구성: Part 1 시스템 분석 → Part 2 BT 인프라 보강 → Part 3 몬스터 AI 전환 → Part 4 통합 방향과 위험 → 부록.

---

## 목차

- [Part 1. 시스템 분석](#part-1-시스템-분석)
- [Part 2. BT 인프라 보강 계획](#part-2-bt-인프라-보강-계획)
- [Part 3. 몬스터 AI BT 전환 계획](#part-3-몬스터-ai-bt-전환-계획)
- [Part 4. 통합 방향, 위험, 결론](#part-4-통합-방향-위험-결론)
- [부록 A. 진행 상태](#부록-a-진행-상태)
- [부록 B. 참고 출처](#부록-b-참고-출처)
- [부록 C. 최소 테스트 그래프](#부록-c-최소-테스트-그래프)

---

# Part 1. 시스템 분석

## 1.1 핵심 진단 — 두 시스템이 갈라져 있다

가장 중요한 사실: BT 인프라(`Assets/02.Scripts/AI/BehaviorTree/`, 약 30개 파일, GraphView/Inspector/Blackboard/Validator 포함)는 거의 완성되어 있지만, **실제 적 AI는 BT를 전혀 사용하지 않는다**.

근거:
- `BehaviorTreeRunner` / `BehaviorTreeAsset` 참조는 BT 폴더 내부 11개 파일에 한정 — 게임플레이 코드에서 사용처 0
- `EnemyAIController.cs` (약 811줄)은 하드코딩된 if/else + `Random.value` 기반 의사결정
- `EnemyFlyingAIController`, `MonsterGroupController`, 13+ Enemy 상태도 모두 EnemyAIController 경로
- BT용 enemy 노드(`TransitionEnemyStateNode`, `SyncEnemyBlackboardNode`, `ExecuteEnemyAttackNode`)는 준비됐지만 실제 BT 에셋 사용처가 `Assets/Test/BehaviorTree.asset` 한 개뿐이었다
- `BehaviorTreeRunner._tickInterval=0.1f`와 `EnemyAIController._decisionInterval=0.1f`가 별개로 폴링하는 이중 구조

즉 BT는 "사두고 안 쓴 인프라"이고 EnemyAIController은 "실제로 돌아가는 코드". 본 가이드의 Part 3는 이 단절을 해소하는 단계적 실행 계획이다.

## 1.2 BT 인프라 갭

### 1.2.1 AAA 사례 대비 구조 갭

| 항목 | 현 상태 | 표준 / AAA 사례 | 영향 |
|---|---|---|---|
| **Service 노드** | (구현 완료 §2.1) `SyncEnemyBlackboardNode`를 매 Tick Action으로 굴리던 우회 해결 | UE4 Service: Composite에 첨부, 백그라운드 주기 갱신 | Blackboard 갱신이 액션 노드 자리를 차지하던 문제 해소 |
| **Observer Decorator** | 없음. AbortType은 `BTCompositeNode.EnumerateConditions`로 매 Tick 트리 재귀 폴링 | Bobby Anguelov **Monitor Decorator**: 조건이 등록부에 들어가 매 Tick 1회 평가, 변화 시 abort 트리거 | 트리 깊을수록 O(n) 폴링. 적 수 늘면 누적 비용 |
| **Blackboard 키 타입 안전성** | (구현 완료 §2.1) `BlackboardKeySelector` struct + PropertyDrawer | UE: BlackboardKeySelector(에디터 드롭다운) | 오타·리네임 보호 |
| **Weighted Random Selector** | (구현 완료 §2.1) 가중 선택 Composite | "BT 정적 우선순위가 가장 큰 단점" — 가중 선택으로 보강이 표준 (GameAIPro Ch.10) | EnemyAIController의 `Random.value` 패턴을 BT로 표현 가능 |
| **Subtree / Sub-BT 참조** | (구현 완료 §2.1) `SubtreeNode` | UE: Run Behavior Tree | 보스 페이즈/그룹 패턴 재사용 가능 |
| **Tick LOD / 예산** | Runner당 `_tickInterval=0.1f`. 거리·중요도 무관 | 거리 기반 동적 틱 간격, frustum 외 stall | 화면 밖 적도 동일 비용 |
| **이중 폴링** | `BehaviorTreeRunner.tickInterval=0.1` + `EnemyAIController._decisionInterval=0.1` | — | 통합 시 한쪽으로 일원화 필요 |
| **페이즈 시스템 표현** | `EnemyBehaviorSO.phases`가 노드 파라미터를 못 바꿈 | HZD: HTN의 매크로 단위 / FromSoftware: 페이즈 = FSM 전이 + 공격풀 교체 | BT migrate 시 데이터 모델 재설계 필요 |
| **공격 풀 / 카드 시스템** | 없음. `EnemyCombat`의 스킬 리스트 + 거리 필터만 | Soulslike 보스: 5+ 공격, 페이즈마다 풀 교체, 랜덤 + 쿨다운 | 액션게임 적 행동의 핵심 모델이 BT에 부재 |
| **런타임 디버깅 시각화** | `BehaviorTreeDebugTrace` 큐 데이터는 쌓이고 있음. GraphView 노드 색 하이라이트 부분 적용 | UE: 실시간 노드 하이라이트, 블랙보드 watcher | authoring 도구 가치 절반 |

### 1.2.2 Behavior Designer 레퍼런스 대비 누락

| 누락 기능 | 현재 문제 | 우선순위 |
|-----------|-----------|----------|
| Conditional Abort | `BTAbortType` 필드만 있고 실제 재평가/중단 로직 없음 | 높음 |
| Pause/Resume | `StartTree`, `StopTree`, `RestartTree`만 있고 일시정지/재개 없음 | 높음 |
| Restart When Complete | Root가 `Success` 또는 `Failure`가 된 뒤 자동 재시작 옵션 없음 | 높음 |
| Manual Tick | 외부 시스템이 직접 tick을 호출하는 모드 없음 | 중간 |
| Parallel Abort | 실패 확정 시 다른 Running 자식 중단 없음 | 중간 |
| Breakpoint | 실행 중 특정 노드에서 멈추는 기능 없음 | 중간 |
| Disable Node | 에디터에서 특정 노드를 비활성화하고 실행에서 제외하는 기능 없음 | 중간 |
| Decorator 확장 | `Return Failure`, `Return Success`, `Until Success`, `Until Failure`, `Timeout` 등 부족 | 중간 |
| Debug Trace | tick 순서, 상태 변경, Abort 원인을 기록하는 추적 데이터 부분 구현 | 중간 |
| Error Window 고도화 | 실시간 오류 목록은 있으나 BD 수준의 상세 원인/이동 기능 부족 | 낮음 |

## 1.3 AAA 레퍼런스 핀포인트 — 우리 프로젝트에 직접 적용 가능한 것

일반론은 피하고, 스타일리시 액션 게임 컨텍스트에 바로 적용 가능한 것만 정리한다.

### 1.3.1 Bobby Anguelov — "Breaking the Cycle of Misuse"

- BT는 "복잡한 reactive 로직"에 약함 — 정적 우선순위 + 매 프레임 트리 traversal 문제
- 해법 **Monitor Decorator**: condition을 별도 register에 모아 1회 평가, 변화 시만 abort
- 또 다른 핵심: **"BT는 만능 아니다, 다른 시스템과 조합"** — Separation of Concerns (Game AI Pro2 Ch.12)
- **우리에게 의미:** AbortType 폴링 구조를 Monitor로 교체하면 트리 깊이 무관 O(monitored conditions)로 비용 고정

### 1.3.2 Unreal Engine 4 Behavior Tree (사실상 본 BT가 모방한 표준)

- **Services**: Composite에 첨부, 주기적 Blackboard 업데이트
- **Decorators with Observer Aborts**: Self / Lower Priority / Both — 우리 코드의 `BTAbortType`가 그대로 가져왔으나 **이벤트 기반이 아닌 폴링**
- **우리에게 의미:** Service 카테고리는 §2.1에서 도입 완료. Observer Abort 사양은 Phase R3에서 다룸

### 1.3.3 Dave Mark — Infinite Axis Utility (GDC 2013)

- 모든 행동에 0~1 score → 가장 높은 것 선택 (또는 weighted random in top-N)
- EnemyAIController의 `if (Random.value < ContinueAttackChance) ...` 같은 **체이닝 if-random**의 정공법 대체
- **우리에게 의미:** **UtilitySelectorNode** (자식들의 score 가져와서 weighted random) 추가만 해도 EnemyAIController의 의도를 BT로 표현 가능

### 1.3.4 Horizon Zero Dawn — Decima HTN

- 매크로(여러 액션 묶음) 단위 계획. "공격하기"가 단일 액션이 아닌 "접근 → 위치잡기 → 페이크 → 공격" 매크로
- 머신 그룹 행동(역할 분배)이 우리 `MonsterGroupController`와 컨셉적으로 닮음
- **우리에게 의미:** BT만으로는 그룹 의도 표현 한계가 분명하다는 강한 증거. 다만 현재 필요한 그룹 행동 범위에 비해 HTN 전면 도입은 얻는 것보다 잃는 것(저작 난이도·디버깅 비용)이 크다. 그룹 의도 표현은 BT 외부에 별도 컨트롤러를 두는 현재 구조 유지가 합리

### 1.3.5 Soulslike 보스 AI 패턴 (FromSoftware)

- 보스당 5+ 공격, 페이즈마다 공격 풀 변경, 랜덤 + 쿨다운 + 거리 게이트로 패턴 회피
- 가장 가까운 모델 — 우리 `EnemyBehaviorSO.phases` + `EnemyCombat.skills`가 같은 구조 지향
- **우리에게 의미:** **AttackPoolNode** (현재 페이즈의 가용 스킬 집합 → weighted random + cooldown + range filter)가 단일 노드로 추가될 가치 있음

---

# Part 2. BT 인프라 보강 계획

## 2.1 4.1 단계 — 즉시 가치 구현 (2026-05-11 완료)

다음 4개 항목은 AAA 갭 §1.2.1에서 도출되어 2026-05-11에 구현 완료되었다.

### 2.1.1 추가/변경 파일

| 분류 | 파일 | 설명 |
|---|---|---|
| 신규 (런타임) | `Runtime/BTServiceNode.cs` | Service 노드 베이스. `Interval`/`TickOnEnter` 직렬화 필드, `OnServiceEnter/Tick/Exit` 훅 |
| 신규 (런타임) | `Runtime/Blackboard/BlackboardKeySelector.cs` | 타입 필터링된 Blackboard 키 selector struct |
| 신규 (런타임) | `Nodes/Composite/WeightedRandomSelectorNode.cs` | 가중치 기반 1회 픽 + 실패 시 풀 소진 재픽 Composite |
| 신규 (런타임) | `Nodes/Action/SubtreeNode.cs` | 다른 `BehaviorTreeAsset`을 실행, 부모 Blackboard 공유 |
| 신규 (런타임) | `Nodes/Service/SyncEnemyBlackboardService.cs` | 기존 Sync Action의 Service 버전 |
| 신규 (에디터) | `Editor/BlackboardKeySelectorDrawer.cs` | BlackboardKeySelector의 인스펙터 드롭다운 PropertyDrawer |
| 수정 (런타임) | `BTNode.cs` | Initialize/Tick/Abort/ResetNode에 Composite Service 생명주기 통합 |
| 수정 (런타임) | `BTCompositeNode.cs` | `_services` 직렬화 필드 + `BeginServices/TickServices/EndServices` |
| 수정 (런타임) | `Blackboard.cs` | `BlackboardKeySelector` 오버로드 12종 추가 (Set/TryGet) |
| 수정 (런타임) | `BehaviorTreeAsset.cs` | `CloneRuntime`에 `shareBlackboardOverride` 파라미터 추가, services 클론 |
| 수정 (에디터) | `BehaviorTreeAssetValidator.cs` | Service 부착 검증, Subtree 순환 검증, WeightedRandom 가중치 검증 |
| 수정 (에디터) | `BehaviorTreeGraphView.cs` | Service 노드는 그래프 메뉴 제외, 카테고리에 Service 추가 |
| 수정 (에디터) | `BehaviorTreeInspectorView.cs` | Composite 선택 시 "+ Add Service" 드롭다운, Service 카테고리 표시 |

### 2.1.2 핵심 설계 결정

- **BlackboardKey 제네릭 포기 이유**: Unity는 `[SerializeField] BlackboardKey<bool>` 같은 제네릭 필드를 직렬화하지 않는다. `BlackboardKeySelector` (비제네릭 struct + `expectedType` 필드 + PropertyDrawer)가 핵심 이득의 90%(에디터 오타 방지, 타입 미스매치 적색 표시, Validator 키 부재 경고)를 가져오면서 string 기반 기존 API와 바이너리 호환을 유지한다.
- **Service 노드는 그래프 노드가 아니다**: UE 스타일로 Composite NodeView 본체 내부에 stacked 표시하는 것은 작업량이 4.1 범위를 초과한다. 현재는 Inspector 안에서 Add 드롭다운 + SerializeField 리스트 형태로 노출한다.
- **WeightedRandomSelector는 매 Tick 재롤하지 않는다**: `OnStart`에 1회 픽 → Running 자식이 안정적으로 끝까지 실행. Failure 시에만 남은 풀에서 재픽. 매 Tick 재롤은 행동 불안정 유발이라 의도적으로 배제.
- **Subtree는 Blackboard를 공유한다**: 별도 격리 모드는 추후 작업. 부모 트리와 동일 인스턴스를 참조 (`CloneRuntime(parentBB, shareBlackboardOverride: true)`).
- **Subtree 순환 검증**: Validator가 `HasSubtreeCycle`로 A → B → A 형태를 검출.
- **기존 string 기반 노드 마이그레이션 보류**: `SetBlackboardValueNode`, `BlackboardBoolConditionNode` 등 6개 노드는 string 키 그대로 유지. 새로 작성하는 노드부터 `BlackboardKeySelector` 사용 권장.

### 2.1.3 4.1 범위에서 의도적으로 미룬 항목

- JSON Import/Export의 Services/Subtree 직렬화 — `BehaviorTreeNodeJson.children`은 GUID 기반이지만 `services` 필드 없음. 현재는 .asset 직렬화로만 보존
- NodeView 본체에 service 카운트/요약 표시
- 기존 6개 노드의 BlackboardKeySelector 마이그레이션 (의도적 보류 — `BlackboardKeySelector`는 새 노드에서만 사용 권장. 기존 string 키 노드는 그대로 유지)
- Service 노드를 그래프 위 stacked 노드로 표현
- ~~DebugTrace에 Service tick 미반영~~ — 2026-05-16 해소. `BTServiceNode.ServiceEnter/Tick/Exit`이 `Context.DebugTrace.Record`를 호출해 트레이스 큐에 기록됨
- ~~Subtree 클론 인스턴스의 Unity Object 누수~~ — 2026-05-16 해소. `BehaviorTreeAsset.DisposeRuntime(runtimeTree)` static 헬퍼 추가 후 `BehaviorTreeRunner.StopTree`/`RestartRuntimeTree`와 `SubtreeNode.OnInitialize`/`OnDestroy`가 명시적으로 정리. RestartRuntimeTree는 이전 Blackboard를 `Clone()`해서 새 트리에 전달하므로 dangling 참조 없음

### 2.1.4 사용 가이드

#### Service 부착
1. BT 에디터에서 Composite 노드(Selector/Sequence/Parallel/WeightedRandom 등) 선택
2. Inspector 우측 패널 하단 "+ Add Service" 버튼 클릭
3. 드롭다운에서 BTServiceNode 파생 타입 선택 (현재 `SyncEnemyBlackboardService`)
4. 선택된 Composite의 `Services` 리스트에 추가됨. Interval/TickOnEnter 인스펙터에서 조정

#### WeightedRandomSelector 가중치
- 그래프 메뉴에서 "Create/Composite/WeightedRandomSelectorNode" 추가
- 자식 노드 연결 후, 노드 인스펙터에서 `_weights` 리스트를 자식 수와 동일하게 채움 (생략 시 1.0 패딩)
- 가중치 0인 항목은 균등 분포에서만 선택됨 (Total weight > 0이면 0 항목은 제외)

#### Subtree
- 재사용할 BT를 별도 `BehaviorTreeAsset`으로 작성 (예: `BT_Boss_Phase2_AttackPool.asset`)
- 부모 BT에서 "Create/Action/SubtreeNode" 추가, `Subtree Asset` 필드에 참조 지정
- **Blackboard 키 약속 필요** — 부모 BT의 Blackboard가 그대로 공유되므로 키 이름이 어긋나면 동작 안 함
- 순환 참조(A → B → A)는 Validator가 에러로 잡음

#### BlackboardKeySelector 사용 (새 노드 작성 시)
```csharp
public class MyNewNode : BTActionNode
{
    [SerializeField] private BlackboardKeySelector _targetKey = new("Target", BlackboardValueType.Object);

    protected override BTStatus OnUpdate()
    {
        if (!Context.Blackboard.TryGetObject<GameObject>(_targetKey, out var target))
            return BTStatus.Failure;
        // ...
    }
}
```
인스펙터에서 `Target Key (Object)` 드롭다운으로 Blackboard에 등록된 Object 타입 키만 표시된다.

## 2.2 Phase R1: Runner 실행 제어 보강 (구현 완료)

### 목표
Behavior Designer Behavior Manager 옵션 중 UPlayground에 필요한 실행 제어를 `BehaviorTreeRunner`에 추가한다.

### 구현 항목 (모두 `BehaviorTreeRunner.cs`에 구현 완료)

| 항목 | 설명 | 코드 위치 |
|------|------|-----------|
| `EnableBehavior()` | 정지 또는 일시정지된 BT 실행 시작 (Paused면 Resume, Stopped면 Start) | `BehaviorTreeRunner.cs:103-113` |
| `DisableBehavior(bool pause)` | `pause = true`면 PauseTree, `false`면 StopTree | `BehaviorTreeRunner.cs:115-121` |
| `PauseTree()` | tick만 멈추고 런타임 트리와 노드 상태는 보존, `OnAbort` 호출하지 않음 | `BehaviorTreeRunner.cs:123-127` |
| `ResumeTree()` | Pause 이전 Running 상태에서 이어서 실행 | `BehaviorTreeRunner.cs:129-133` |
| `TickOnce()` / `StepTick()` | Manual Tick 모드 또는 Pause 상태에서 외부 호출로 1회 tick | `BehaviorTreeRunner.cs:135-143` |
| `_restartWhenComplete` | Root가 Success/Failure가 되면 다음 tick에 자동 재시작 | `BehaviorTreeRunner.cs:16, 163-164` |
| `_resetValuesOnRestart` | 재시작 시 Blackboard 값을 기본값으로 되돌릴지 선택 | `BehaviorTreeRunner.cs:17, 100, 164` |
| `_tickMode` | `UpdateInterval`, `EveryFrame`, `Manual` 중 선택. Manual은 `Update()`에서 자동 tick 차단 | `BehaviorTreeRunner.cs:14, 52` / `BehaviorTreeRunnerMode.cs` |
| `RequestPauseFromNode(BTNode)` | Breakpoint 노드가 Tick 종료 후 Runner를 Pause 상태로 전환 요청 | `BehaviorTreeRunner.cs:145-150, 170-171` |

### 완료 조건 (모두 충족)
- `PauseTree()` 호출 시 Running 노드가 Abort되지 않는다 — `_state` 전환만 수행.
- `ResumeTree()` 호출 후 같은 런타임 트리에서 이어서 tick 된다 — `_runtimeTree` 재생성 없음.
- Manual Tick 모드에서는 `Update()`가 자동 tick 하지 않는다 — `BehaviorTreeRunner.cs:52` 조기 return.
- `Restart When Complete`가 켜진 경우 Root 완료 후 다음 tick에 `RestartRuntimeTree` 호출.

### 잔여 작업
- Pause/Resume Play Mode 검증 (부록 C.1 최소 테스트 그래프 활용).
- `RequestPauseFromNode`를 사용하는 Breakpoint UI(그래프/Inspector 토글)는 §2.6 R5 잔여 작업으로 남음.

## 2.3 Phase R2: Composite Abort/Reset 규칙 보강 (구현 완료)

### 목표
Sequence, Selector, Parallel이 Running 자식과 실패/성공 확정 상황을 명확하게 정리하도록 만든다.

### 구현 항목 (모두 완료)

| 항목 | 설명 | 코드 위치 |
|------|------|-----------|
| Running sibling 정리 | Sequence/Selector `OnStop`에서 `AbortRunningChildren()` 호출 + `_currentIndex` 리셋 | `SequenceNode.cs:54-58`, `SelectorNode.cs:57-61` |
| `OnStop` 하위 Abort 정책 | `BTCompositeNode.AbortRunningChildren(except)` 헬퍼 — `IsStarted`인 자식만 선택 Abort | `BTCompositeNode.cs:15-24` |
| Parallel 실패 시 Abort | `requireAllSuccess = true`에서 하나가 Failure면 `AbortRunningChildren(child)` 호출 후 즉시 Failure 반환 | `ParallelNode.cs:42-47` |
| Parallel 성공 시 Abort | `requireAllSuccess = false`에서 하나가 Success면 다른 Running 자식 Abort 후 즉시 Success 반환 | `ParallelNode.cs:49-57` |
| Reset 안정화 | `OnReset`에서 `_currentIndex = 0` (Sequence/Selector), `_childStatuses = null` (Parallel) | `SequenceNode.cs:49-52`, `SelectorNode.cs:52-55`, `ParallelNode.cs:84-87` |

### 완료 조건 (모두 충족)
- Parallel에서 실패가 확정되면 다른 Running 자식의 `OnAbort`가 호출된다.
- Selector가 Success로 종료된 뒤 이전 Running 자식이 다음 실행에 남지 않는다.
- Sequence/Selector가 재시작될 때 `_currentIndex`가 항상 0에서 시작한다.

### 잔여 작업
- 부록 C.2 Parallel Abort 테스트 그래프로 Play Mode 검증 필요.

## 2.4 Phase R3: Conditional Abort 구현 (기본 동작 완료 / Monitor 최적화 미진행)

### 개념 정리

| Abort Type | 의미 |
|------------|------|
| `None` | 조건 재평가 없음 |
| `Self` | 같은 Composite 내부에서 현재 실행 중인 브랜치를 조건 변화에 따라 중단 |
| `LowerPriority` | 더 오른쪽 낮은 우선순위 브랜치가 Running 중일 때 왼쪽 조건을 재평가해 중단 |
| `Both` | `Self`와 `LowerPriority`를 모두 수행 |

### 현재 구현 상태

초기 구현 5단계가 모두 코드에 반영되었다. UE/Anguelov Monitor 방식의 등록제 평가는 아직 도입하지 않았다.

| 단계 | 설명 | 코드 위치 |
|------|------|-----------|
| ① 마지막 평가 결과 저장 | `BTConditionNode.LastAbortEvaluation`, `HasAbortEvaluation` 필드 + `EvaluateForAbort()` / `EvaluateAbortChanged()` API | `BTConditionNode.cs:5-33` |
| ② Composite의 Conditional 후보 수집 | `EnumerateConditions` 재귀 탐색 (현재는 매 평가 시 재귀 폴링) | `BTCompositeNode.cs:129-142` |
| ③ Sequence/Selector tick 시 재평가 | `TryHandleConditionalAbort()`가 tick 시작 시 abort 평가 수행 | `SequenceNode.cs:60-89`, `SelectorNode.cs:63-92` |
| ④ 결과가 바뀌면 Abort + 인덱스 재계산 | `runningChild.Abort()` + `_currentIndex = 0` | `SequenceNode.cs:74-86`, `SelectorNode.cs:77-89` |
| ⑤ Debug Trace 기록 | `Context.DebugTrace.Record(..., "ConditionalAbort", ...)`로 원인 조건 노드 기록 | `SequenceNode.cs:78,86`, `SelectorNode.cs:78,86` |

`TryEvaluateSelfAbort` / `TryEvaluateLowerPriorityAbort` 보조 메서드는 `BTCompositeNode.cs:80-127`에 위치하며 `runningIndex` 기준으로 자기 브랜치와 더 높은 우선순위 형제만 평가하도록 분리되어 있다.

### 잔여 작업 (Monitor 최적화)

중장기로는 Anguelov Monitor 방식(§1.3.1)으로 폴링을 등록제 평가로 교체해 깊은 트리 비용을 O(monitored)로 고정한다. 현재 구조는 `EnumerateConditions`가 자식 트리를 재귀로 도므로 트리 깊이에 비례해 평가 비용이 누적된다 — §4.4 위험 표 참고.

### UPlayground 적용 예시

| 상황 | 권장 Abort |
|------|------------|
| Patrol 중 타겟 발견 | `LowerPriority` |
| Chase 중 타겟이 사라짐 | `Self` |
| CombatIdle 중 공격 가능 조건 충족 | `LowerPriority` |
| Attack 준비 중 타겟 사망 | `Self` |
| Retreat 중 거리가 충분히 벌어짐 | `Self` |

### 완료 조건
- `BT_EnemyGroundBasic_Test.json` 흐름에서 Patrol 중 `HasTarget`이 true가 되면 Patrol 브랜치가 중단될 수 있다. (Play Mode 검증 필요)
- Running Action이 중단될 때 `Abort()`와 `OnAbort()`가 호출된다. ✅ 코드 경로 존재
- Abort 발생 원인이 Debug Trace에 기록된다. ✅ 코드 경로 존재
- 기존 `AbortType = None` 그래프의 동작은 변경되지 않는다. ✅ `TryHandleConditionalAbort`가 `selfAbort`/`lowerPriorityAbort` 모두 false면 즉시 false 반환

## 2.5 Phase R4: Decorator 노드 확장 (완료)

### 추가 후보

| 노드 | 동작 | 상태 |
|------|------|------|
| `ReturnSuccessNode` | 자식 결과와 무관하게 자식 완료 시 Success 반환 | 완료 — `Nodes/Decorator/ReturnSuccessNode.cs` |
| `ReturnFailureNode` | 자식 결과와 무관하게 자식 완료 시 Failure 반환 | 완료 — `Nodes/Decorator/ReturnFailureNode.cs` |
| `UntilSuccessNode` | 자식이 Success가 될 때까지 반복 | 완료 — `Nodes/Decorator/UntilSuccessNode.cs` |
| `UntilFailureNode` | 자식이 Failure가 될 때까지 반복 | 완료 — `Nodes/Decorator/UntilFailureNode.cs` |
| `TimeoutNode` | 지정 시간 안에 자식이 완료되지 않으면 Failure 반환 및 자식 Abort | 완료 — `Nodes/Decorator/TimeoutNode.cs` |
| `InverterNode` | 자식 결과 반전 | 완료 — `Nodes/Decorator/InverterNode.cs` |
| `CooldownNode` | 일정 시간 안에는 자식 실행 차단 | 완료 — `Nodes/Decorator/CooldownNode.cs` |
| `RepeatNode` | N회 반복 | 완료 — `Nodes/Decorator/RepeatNode.cs` |
| `GuardConditionNode` | 지정 Blackboard bool 키가 기대값일 때만 자식 실행. 매 Tick 비교 후 어긋나면 Abort + Failure | 완료 — `Nodes/Decorator/GuardConditionNode.cs` |
| `ForceAbortNode` | 지정 Blackboard bool 키 값이 트리거로 변경되는 순간 자식 강제 Abort. 변화 이벤트 기반 | 완료 — `Nodes/Decorator/ForceAbortNode.cs` |

### Decorator 공통 규칙

| 규칙 | 설명 |
|------|------|
| 자식 수 | 정확히 1개 |
| Running 처리 | 자식이 Running이면 Decorator도 Running을 반환하는 것을 기본으로 한다 |
| 종료 처리 | Decorator가 Success/Failure로 확정되면 Running 자식은 남지 않아야 한다 |
| Reset 처리 | 반복형 Decorator는 반복 사이클마다 자식 상태를 명확히 Reset한다 |

## 2.6 Phase R5: Debug Trace, Breakpoint, Disable Node (구현 완료)

### 구현 항목

| 항목 | 설명 | 상태 |
|------|------|------|
| Debug Trace | tick 순서, 노드 GUID, 상태 변화, Abort 원인 기록 | 완료 — `BehaviorTreeDebugTrace` 큐 + `Record()` API (`BehaviorTreeRunner.cs:204-255`). Start/Tick/Stop/Abort/Disabled/Breakpoint/ConditionalAbort/ServiceEnter/ServiceTick/ServiceExit/ForceAbort 11종 이벤트 기록 |
| Breakpoint Pause (백엔드) | 노드가 `Context.RequestPause(this)`를 호출하면 Tick 종료 후 Runner가 Paused로 전환 | 완료 — `BehaviorTreeRunner.cs:145-150, 170-171` / `BehaviorTreeContext.cs:24-27` |
| Breakpoint 노드 필드 + UI 토글 | `BTNode._breakpoint` 필드 + Tick 시작 시 `RequestPause` 자동 호출 + NodeView 우클릭 메뉴 토글 | 완료 — `BTNode.cs:15, 58-62, 97-98` / `BehaviorTreeNodeView.cs:210-212, 325-332` |
| Step Tick | Pause 상태에서 1 tick만 실행 | 완료 — `BehaviorTreeRunner.StepTick()` (`BehaviorTreeRunner.cs:140-143`) |
| Disable Node | 에디터에서 특정 노드를 실행 제외 (런타임 Success로 건너뜀) | 완료 — `BTNode._disabled` + `Tick`에서 Disabled 시 Success 반환 (`BTNode.cs:14, 52-56, 85-90`) + NodeView 우클릭 토글 (`BehaviorTreeNodeView.cs:213-215, 334-339`) + Validator Warning (`BehaviorTreeAssetValidator.cs:59-60`) + NodeView opacity 0.45 시각화 |
| Runtime Blackboard View | 실행 중 Blackboard 값 표시 | 완료 — `BehaviorTreeBlackboardView.ResolveRuntimeBlackboard` + `DrawSideBySideValue`로 Asset/Runtime 양쪽 컬럼, `Runtime Only` 섹션 (`BehaviorTreeBlackboardView.cs:98-158`) |
| Last Active Path | 마지막 실행 경로 그래프 하이라이트 | 완료 — `NodeView.UpdateStateColor`가 `runtimeNode.LastStatus` 기반 색상 적용. Running(노란)/Success(초록)/Failure(빨강) (`BehaviorTreeNodeView.cs:186-205, 241-251`) |

### 완료 조건 (모두 충족)
- 노드 우클릭에서 Breakpoint를 켜고 끌 수 있다. ✅
- Breakpoint 노드가 시작되면 Runner가 Tick 종료 후 Paused로 전환된다. ✅
- Disable Node는 Validate에서 Warning으로 표시되고 런타임에서 해당 노드를 Success로 건너뛴다. ✅
- Debug Trace를 통해 최근 tick 결과를 에디터에서 확인할 수 있다. ✅

## 2.7 Phase R6: Validator와 Error Window 고도화 (부분 완료)

### 추가 검증 항목

| 검증 | 오류 수준 |
|------|-----------|
| Root 없음 | Error |
| Composite 자식 없음 | Error |
| Decorator 자식 수가 1이 아님 | Error |
| Action/Condition에 자식 연결 | Error 또는 자동 차단 |
| 순환 참조 | Error |
| 끊어진 GUID 참조 | Error |
| Blackboard Key 누락 | Error |
| 같은 자식을 여러 부모가 참조 | Error |
| Disabled Node가 필수 경로를 끊음 | Warning |
| Conditional Abort 대상 조건 없음 | Warning |

### 에디터 UX

| 기능 | 설명 | 상태 |
|------|------|------|
| 오류 클릭 이동 | 오류 항목 클릭 시 해당 노드 선택 및 프레임 | 완료 — `BehaviorTreeEditorWindow.cs:800-802` `row.RegisterCallback<MouseDownEvent>(_ => _graphView?.FocusNode(message.TargetNode))` |
| 노드 위 오류 배지 | 오류가 있는 노드 상단에 표시 | 미구현 |
| 저장 전 경고 | Error가 있으면 Export/Save 전 확인 | 미구현 |
| JSON Import 검증 | Import 직후 자동 Validate | 완료 — `BehaviorTreeJsonUtility.ImportJson`이 Monster/표준 JSON 양쪽 경로 모두에서 `BehaviorTreeAssetValidator.Validate` 호출 후 `Debug.Log/LogWarning/LogError`로 결과 출력 (Error/Warning 카운트 요약 포함) |

## 2.8 권장 구현 순서

| 순서 | Phase | 이유 | 현재 상태 |
|------|-------|------|-----------|
| 1 | R1 Runner 실행 제어 | Pause/Manual Tick/Restart가 있어야 디버깅·테스트가 쉬워진다 | 완료 |
| 2 | R2 Composite Abort/Reset | 런타임 안정성의 기반 | 완료 |
| 3 | R4 Decorator 확장 | 공식 Decorator 기준 충족, 그래프 표현력 확대 | 완료 — 10종 (Inverter/Cooldown/Repeat/ReturnSuccess/ReturnFailure/UntilSuccess/UntilFailure/Timeout/GuardCondition/ForceAbort) |
| 4 | R5 Debug Trace/Breakpoint | Conditional Abort 구현 전 관찰 도구 확보 | 완료 — DebugTrace 11종 이벤트, StepTick, Pause, Breakpoint UI, Disable Node, Blackboard 런타임 뷰, 활성 경로 시각화까지 모두 구현 |
| 5 | R3 Conditional Abort | 가장 중요하지만 디버깅 난도가 높아 기반 기능 이후 진행 | 기본 동작 완료, Monitor 최적화 미진행 (의도적 보류) |
| 6 | R6 Error Window 고도화 | 기능 안정화 후 UX 정리 | 부분 — 오류 클릭 이동/JSON Import 자동 Validate 완료, 노드 오류 배지·저장 전 경고 미구현 |

R1~R5 기반은 모두 끝났다. 남은 갈래는 ① R3 Monitor 최적화(중장기, §1.3.1 참고) ② R6 UX 정리(노드 오류 배지, 저장 전 경고). 둘 다 Phase 5 BT 전환·Phase 6 비행형보다 우선순위가 낮으므로 §4.7 권고 순서를 따라간다.

## 2.9 구현 시 주의 사항

- `BTNode`에 런타임 상태를 추가할 때 에셋 원본이 아니라 `CloneRuntime()` 결과에만 상태가 남도록 유지한다.
- Pause는 Abort와 다르다. Pause에서는 `OnAbort()`를 호출하지 않는다.
- Restart는 Stop/Start와 같지 않다. `Reset Values On Restart` 옵션에 따라 Blackboard 초기화 여부가 달라져야 한다.
- Conditional Abort는 모든 tick마다 전체 트리를 무작정 재평가하면 비용과 예측 가능성이 나빠진다. Composite 단위 재평가 목록을 두는 방식이 적합하다.
- Decorator는 자식이 2개 이상 연결되지 않도록 에디터와 Validator 양쪽에서 막는다.
- 기존 Enemy AI와 BT Runner가 같은 프리팹에서 동시에 State 전환을 시도하지 않도록 테스트 프리팹을 분리한다.

---

# Part 3. 몬스터 AI BT 전환 계획

이 부분은 BT 인프라(Part 2)를 사용해 `EnemyAIController` / `EnemyFlyingAIController` 중심의 몬스터 의사결정을 BT Asset과 BT Node로 완전히 이전하는 실행 계획이다.

핵심 목표:

- `EnemyAIController.MakeDecision()` / `EnemyFlyingAIController.MakeDecision()`의 행동 선택 로직을 BT Asset과 BT Node로 이전한다.
- 기존 몬스터 동작은 JSON 데이터로 기술하고, 이 JSON을 BT Asset으로 임포트할 수 있어야 한다.
- 기존 `EnemyActorState`, `EnemyFlying*State`, KCC 기반 이동 제어는 유지한다.
- BT는 "어떤 행동을 할지"만 결정하고, 실제 이동/공격/피격/사망 처리는 기존 State Machine이 계속 담당한다.
- `EnemyBehaviorSO`, `BehaviorPhase`, `EnemyFlyingSettingsSO`는 BT 전환 이후에도 몬스터별 튜닝 데이터로 유지한다.
- 최종적으로 `EnemyAIController` / `EnemyFlyingAIController`은 런타임 의사결정자가 아니라 BT Action/Condition에서 참조하는 Adapter 또는 Context Provider로 축소한다.

## 3.1 현재 구조

지상 몬스터 AI는 `EnemyAIController`이 주기적으로 판단하여 `ActorMovementController.TransitionToState()`를 직접 호출한다.

```
MonsterActor
├── EnemyAIController
│   ├── MakeDecision()
│   ├── HandleCombatBehavior()
│   ├── TryReactToPlayerState()
│   ├── TryInterruptCurrentState()
│   └── DecidePostAttack()
├── EnemyDetection
├── EnemyTacticalMemory
├── EnemyCombat
└── ActorMovementController
    └── EnemyActorState
```

비행 몬스터는 `EnemyFlyingAIController`이 지상 전투 루프와 공중 루프를 모두 제어한다.

```
EnemyFlyingAIController
├── MakeDecision()
├── EvaluateChase()
├── OnGroundAttackFinished()
├── OnAirAttackFinished()
├── TransitionToTakeOff()
└── TransitionToDescend()
```

### 이미 구현된 BT 기반

```
Assets/02.Scripts/AI/BehaviorTree/
├── Runtime/
│   ├── BehaviorTreeRunner.cs
│   ├── BehaviorTreeAsset.cs
│   ├── BehaviorTreeContext.cs
│   ├── BTNode.cs
│   ├── BTActionNode.cs
│   ├── BTConditionNode.cs
│   ├── BTCompositeNode.cs
│   ├── BTDecoratorNode.cs
│   ├── BTServiceNode.cs
│   └── Blackboard/
├── Nodes/
│   ├── Action/ (ExecuteEnemyAttackNode, TransitionEnemyStateNode, SyncEnemyBlackboardNode, RequestEnemyAttackSlotNode, KeepCurrentStateNode 외)
│   ├── Condition/ (HasTargetNode, IsTargetInRangeNode, CanUseEnemySkillNode, IsCurrentActorStateNode, IsBlockedEnemyStateNode, HasEnemyActionDelayElapsedNode, IsEnemyPatrolEnabledNode)
│   ├── Composite/ (Sequence/Selector/Parallel/WeightedRandomSelector)
│   ├── Decorator/
│   └── Service/ (SyncEnemyBlackboardService)
└── Editor/ (GraphView, Inspector, Validator, JsonImporter, MonsterBehaviorTreeJsonImporter, EnemyBehaviorJsonExporter)
```

목표는 BT 시스템 신규 제작이 아니라, 기존 몬스터 AI의 의사결정 권한을 `BehaviorTreeRunner`로 이전하는 것이다.

### 현재 JSON 유틸의 한계

`BehaviorTreeJsonUtility`는 이미 존재하지만, 역할은 `BehaviorTreeAsset`을 JSON으로 Export/Import하는 것이다.

| 기능 | 현재 지원 여부 | 설명 |
|------|----------------|------|
| BT Asset → JSON | 지원 | 노드 타입, GUID, 위치, 자식 연결, Blackboard, 노드 필드 저장 |
| JSON → BT Asset | 지원 | `BehaviorTreeJsonData`를 읽어 `BehaviorTreeAsset` 생성 |
| 기존 `EnemyAIController` 행동 → JSON | 미지원 | C# 조건문/확률/상태 전환 로직을 데이터화하는 변환 규칙이 없음 |
| 몬스터 행동 정의 JSON → BT Asset | (구현 완료) `MonsterBehaviorTreeJsonImporter`가 처리 |

따라서 완전 전환에는 `BehaviorTreeJsonUtility`를 직접 확장하기보다, 사람이 작성하기 쉬운 몬스터 행동 JSON을 BT 내부 JSON 또는 `BehaviorTreeAsset`으로 변환하는 별도 Importer가 필요하다. 이 Importer는 §3.6에서 구현 완료되었다.

## 3.2 목표 아키텍처

```
MonsterActor
├── EnemyAIContext                 # 신규 또는 EnemyAIController 축소 버전
│   ├── EnemyBehaviorSO
│   ├── EnemyFlyingSettingsSO
│   ├── Phase Runtime State
│   ├── Group Slot API
│   └── Patrol / Distance / Cooldown API
├── BehaviorTreeRunner
│   └── BehaviorTreeAsset
│       ├── Blackboard
│       ├── Service Node
│       ├── Condition Node
│       └── Action Node
├── EnemyDetection
├── EnemyTacticalMemory
├── EnemyCombat
└── ActorMovementController
    └── EnemyActorState / EnemyFlying State
```

### 책임 분리

| 계층 | 최종 책임 |
|------|-----------|
| `BehaviorTreeRunner` | Tick 주기 관리, BT 실행, Debug Trace |
| `BehaviorTreeAsset` | 몬스터별 행동 그래프 자산 |
| Blackboard | 타겟, 거리, 현재 상태, 페이즈, 플레이어 상태, 쿨다운 등 공유 값 |
| Service Node | Detection/Memory/Phase 값을 Blackboard에 동기화 |
| Condition Node | 조건 판정. 거리, 상태, 쿨다운, 페이즈, 플레이어 상태 확인 |
| Action Node | 기존 State로 전환하거나 Combat API 호출 |
| `EnemyAIContext` | 기존 Brain의 데이터/API 보관. BT 노드가 참조하는 안전한 Facade |
| `EnemyActorState` | KCC 이동/회전/물리 콜백, 애니메이션, 상태별 생명주기 |

## 3.3 전환 원칙

### State Machine은 유지한다

BT가 직접 위치 이동, 속도 계산, KCC 콜백을 처리하지 않는다.

올바른 방향:

```csharp
controller.TransitionToState(new EnemyChaseState(controller, context, detection));
```

피해야 할 방향:

```csharp
// BT Action에서 직접 KCC 속도를 제어하지 않는다.
motor.BaseVelocity = direction * speed;
```

### 개입 금지 상태를 모든 Action 앞에서 차단한다

BT는 다음 상태를 덮어쓰면 안 된다.

| 상태 | 이유 |
|------|------|
| `Death` | 사망 처리 중 행동 전환 금지 |
| `Hit` | 피격 리액션 보장 |
| `Grabbed` | 잡힘 상태 제어권 보장 |
| `Airborne` | 공중 피격/낙하 처리 보장 |
| `Attack` | MotionEvent 기반 공격 타임라인 보장 |
| `Counter` | 반격 타이밍 보장 |
| `Land` / `TakeOff` | 비행 상태 전환 생명주기 보장 |
| `Flying_Dive` / `Flying_GroundAttack` | 비행 공격 타임라인 보장 |

이를 위해 `IsBlockedEnemyStateNode` 또는 `EnemyActionGuard` 계층을 추가한다. (Phase 1에서 추가 완료)

### `EnemyBehaviorSO`는 삭제하지 않는다

`EnemyBehaviorSO`는 BT 전환 후에도 다음 용도로 유지한다.

| 필드 | BT 전환 후 용도 |
|------|----------------|
| `optimalCombatDistance` | 공격/추격/선회 조건 |
| `minCombatDistance` | 근접 후퇴 조건 |
| `personalSpaceDistance` | 강제 후퇴 조건 |
| `continueAttackChance` | 공격 후 연속 공격 가중치 |
| `guardChance` | Guard 선택 가중치 |
| `retreatChance` | Retreat 선택 가중치 |
| `circleDuration` | Circle Action 파라미터 |
| `guardDuration` | Guard Action 파라미터 |
| `retreatDistance` | Retreat Action 파라미터 |
| `enablePatrol` | 비전투 Patrol 분기 |
| `phases` | Blackboard 페이즈 오버라이드 |

## 3.4 신규/확장 클래스

### `EnemyAIContext` (도입 완료, Phase 5/7 확장 반영)

`EnemyAIController`을 즉시 제거하지 않고, 먼저 `EnemyAIContext` 역할로 축소한다. 현재 구현은 abstract MonoBehaviour 골격 + 페이즈/거리/순찰/전투 후처리 멤버이며, `EnemyAIController`이 상속·override한다. BT 노드는 `GetComponentCached<EnemyAIContext>()`로 접근해 클래스명에 묶이지 않는다. Phase 5에서 페이즈 멤버가 들어갔고, Phase 7의 1차 정리로 지상 Enemy State 생성자와 BT Action 노드의 `EnemyAIController` 직접 타입 요구를 `EnemyAIContext`로 교체했다.

```csharp
namespace UPlayGround.Component
{
    public abstract class EnemyAIContext : MonoBehaviour
    {
        public abstract EnemyBehaviorSO BehaviorData { get; }
        public abstract BehaviorPhase CurrentPhase { get; }
        public abstract Vector3 SpawnPosition { get; }
        public abstract bool EnablePatrol { get; }
        public abstract bool HasGuardMotion { get; }
        public abstract float HealthPercent { get; }
        public abstract float PatrolRadius { get; }
        public abstract float PatrolWaitTime { get; }
        public abstract float OptimalCombatDistance { get; }
        public abstract float MinCombatDistance { get; }
        public abstract float PersonalSpaceDistance { get; }
        public abstract float ChaseStopDistance { get; }
        public abstract float ChaseSpeedMultiplier { get; }
        public abstract float RetreatDistance { get; }
        public abstract float CircleDuration { get; }
        public abstract float GuardDuration { get; }

        public abstract bool CanUseSkill();
        public abstract bool TryRequestAttackSlot();
        public abstract void NotifyBTAttackStarted();
        public abstract void UpdatePhase(float hpPercent);
        public abstract void DecidePostAttack(bool attackHit);
        public abstract Vector3 GetRandomPatrolPoint();
        public abstract void ReleaseGroupSlot();
    }
}
```

아직 남은 것은 클래스명과 컴포넌트 책임 정리다. `EnemyAIController`은 현재 Context 구현체 + 레거시 의사결정 폴백을 겸한다.

### `EnemyBlackboardKeys` (구현 완료)

```csharp
namespace UPlayGround.AI.BehaviorTree
{
    public static class EnemyBlackboardKeys
    {
        public const string HasTarget = "HasTarget";
        public const string Target = "Target";
        public const string DistanceToTarget = "DistanceToTarget";
        public const string CurrentState = "CurrentState";
        public const string HpPercent = "HpPercent";
        public const string CurrentPhaseName = "CurrentPhaseName";
        public const string PhaseIndex = "PhaseIndex";
        public const string AllowCharge = "AllowCharge";
        public const string AllowFlank = "AllowFlank";
        public const string MaxConsecutiveAttacks = "MaxConsecutiveAttacks";
        public const string ContinueAttackChance = "ContinueAttackChance";
        public const string GuardChance = "GuardChance";
        public const string RetreatChance = "RetreatChance";
        public const string IsPlayerAttacking = "IsPlayerAttacking";
        public const string IsPlayerGuarding = "IsPlayerGuarding";
        public const string IsPlayerStaggered = "IsPlayerStaggered";
        public const string IsPlayerRecovering = "IsPlayerRecovering";
        public const string IsPlayerDodgingFrequently = "IsPlayerDodgingFrequently";
        public const string CanUseSkill = "CanUseSkill";
        public const string HasAttackSlot = "HasAttackSlot";
        public const string NextActionAllowedTime = "NextActionAllowedTime";
    }
}
```

### 필수 신규 노드

| 노드 | 타입 | 역할 | 상태 |
|------|------|------|------|
| `IsBlockedEnemyStateNode` | Condition | BT가 개입하면 안 되는 현재 State 확인 | 완료 |
| `IsEnemyTargetTooCloseNode` | Condition | `PersonalSpaceDistance`, `MinCombatDistance` 기준 후퇴 조건 | 미구현 |
| `CanUseEnemySkillNode` 확장 | Condition | 글로벌 쿨다운, 거리, 스킬 존재 여부 확인 | 완료 (`EnemyAIContext` 의존) |
| `RequestEnemyAttackSlotNode` | Action | `MonsterGroupController.RequestAttackSlot` 요청 | 완료 (`EnemyAIContext` 의존) |
| `ReleaseEnemyAttackSlotNode` | Action | 공격 종료 또는 Abort 시 슬롯 반환 | 미구현 |
| `TransitionEnemyStateNode` 확장 | Action | `Circle`, `Guard`, `Charge`, `Flank`, 비행 상태 전환 지원 | 지상형 완료 (`EnemyAIContext` 의존. 비행 상태 전환은 별도 노드 필요) |
| `ExecuteEnemyAttackNode` | Action | 공격 슬롯 요청 + 공격 시작 통지 + Attack 상태 전환 | 완료 (`EnemyAIContext` 의존) |
| `IsEnemyPatrolEnabledNode` | Condition | `EnablePatrol` 확인 | 완료 (`EnemyAIContext` 의존) |
| `IsEnemyPhaseNode` | Condition | `CurrentPhaseName` 또는 `PhaseIndex` 기준 페이즈 조건 | 완료 (Phase 5) |
| `SelectEnemySkillNode` | Action | 거리/타입/페이즈 기반 스킬 선택 | 미구현 |
| `SyncEnemyMemoryService` | Service | `EnemyTacticalMemory` 상태를 Blackboard에 반영 (5개 bool 키) | 완료 (Phase 4). 임포터가 Root Selector에 자동 부착 |
| `SyncEnemyPhaseService` | Service | HP 기반 페이즈 갱신 및 Blackboard 반영 | 완료 (Phase 5). 임포터가 Root Selector에 자동 부착 |
| `SetEnemyActionDelayNode` | Action | 기존 `_nextActionDelay` 역할 이전 | 미구현 |
| `HasEnemyActionDelayElapsedNode` | Condition | 공격 후 의도적 대기 시간 판정 | 완료 |
| `KeepCurrentStateNode` | Action | 개입 금지 상태에서 Running 반환 | 완료 |

## 3.5 지상/비행 몬스터 BT 설계

### 3.5.1 지상형 기본 BT

```
Root Selector
├── Sequence: 개입 금지 상태
│   ├── IsBlockedEnemyState
│   └── Return Running
│
├── Sequence: 타겟 없음
│   ├── Inverter(HasTarget)
│   └── Selector
│       ├── Sequence
│       │   ├── IsEnemyPatrolEnabled
│       │   └── Transition Patrol
│       └── Transition Idle
│
├── Sequence: 너무 가까움
│   ├── HasTarget
│   ├── IsEnemyTargetTooClose
│   └── Transition Retreat
│
├── Sequence: 공격 가능
│   ├── HasTarget
│   ├── HasEnemyActionDelayElapsed
│   ├── CanUseEnemySkill
│   ├── RequestEnemyAttackSlot
│   └── ExecuteEnemyAttack
│
├── Sequence: 플레이어 공격 반응
│   ├── BlackboardBoolCondition(IsPlayerAttacking)
│   └── WeightedRandomSelector
│       ├── Guard
│       └── Flank
│
├── Sequence: 사거리 밖
│   ├── HasTarget
│   ├── Inverter(IsTargetInRange)
│   └── WeightedRandomSelector
│       ├── Charge
│       ├── Flank
│       └── Chase
│
└── WeightedRandomSelector: 교전 유지
    ├── Circle
    ├── Guard
    ├── Retreat
    └── Chase
```

### 3.5.2 공격 후 행동

기존 `EnemyAIController.DecidePostAttack(bool attackHit)`는 다음 중 하나로 이전한다.

| 기존 처리 | BT 전환 방식 |
|-----------|--------------|
| 공격 적중 시 연속 공격 확률 | Blackboard에 `LastAttackHit`, `AttackChainCount` 기록 후 BT에서 조건 분기 |
| 공격 실패 시 지연 증가 | `SetEnemyActionDelayNode`로 지연 시간 기록 |
| 회피가 잦은 플레이어 대응 | `IsPlayerDodgingFrequently` 조건 + `Charge`/`Flank` 분기 |
| 기본 후속 행동 가중치 | 공격 종료 후 BT Root 재평가로 대체 |

`EnemyAttackState.OnExit` 또는 공격 종료 MotionEvent에서 다음 이벤트를 Context에 통지한다.

```csharp
context.NotifyAttackFinished(attackHit);
```

BT는 다음 Tick에서 Blackboard 값을 보고 후속 행동을 고른다.

### 3.5.3 비행형 BT

비행형은 하나의 거대한 트리보다 Subtree를 나누는 방식이 안전하다.

```
Flying Root Selector
├── Sequence: 개입 금지 상태
├── Sequence: 타겟 없음
│   └── Patrol or Land
├── Sequence: 공중 상태
│   └── Subtree AirCombat
└── Subtree GroundCombat
```

#### GroundCombat Subtree

```
GroundCombat
├── Sequence: 이륙 조건
│   ├── ShouldTakeOff
│   └── Transition Flying_TakeOff
├── Sequence: 공격 가능
│   ├── CanUseEnemySkill
│   └── Transition Flying_GroundAttack
├── Sequence: 너무 가까움
│   └── Transition Flying_Retreat
└── Transition Flying_Chase
```

#### AirCombat Subtree

```
AirCombat
├── Sequence: 공중 공격 횟수 소진
│   └── Select Dive or Land
├── Sequence: 공중 공격 가능
│   └── Execute Aerial Skill
└── Transition Flying_AirCircle
```

#### 비행형 전환 주의점

비행 상태는 `OnAirAttackFinished`, `OnDiveLanded`, `ResetAllCounters` 같은 콜백 기반 흐름이 있다. 이 로직은 한 번에 BT로 옮기지 않고 다음 순서로 이전한다.

1. `EnemyFlyingAIController`의 카운터와 튜닝값을 `EnemyFlyingAIContext`로 분리한다.
2. 지상 추격/공격/후퇴 판단만 BT로 이전한다.
3. `TakeOff`, `AirCircle`, `Dive`, `Land` 상태 콜백은 유지한다.
4. 공중 공격 횟수, 착지/급강하 선택을 BT Condition/Action으로 이전한다.
5. `EnemyFlyingAIController.MakeDecision()`을 제거한다.

## 3.6 데이터 전환 (몬스터 행동 JSON 파이프라인, 구현 완료)

### 3.6.1 몬스터 행동 JSON 임포트

기존 몬스터 동작은 C# Brain을 직접 읽어서 자동 변환하기보다, 먼저 사람이 검토 가능한 JSON 데이터로 작성한다. 이 JSON은 BT 에셋의 저장 포맷이 아니라 "몬스터 행동 의도"를 표현하는 중간 포맷이다.

```
Assets/10.Datas/AI/BehaviorTree/SourceJson/
├── Ground/
│   ├── EnemyBehavior_Skeleton_Common.json
│   ├── EnemyBehavior_Skeleton_Sword.json
│   └── EnemyBehavior_Humanoid_Melee.json
└── Flying/
    └── EnemyBehavior_Griffin.json
```

변환 흐름:

```
Monster Behavior Json
    └── MonsterBehaviorTreeJsonImporter
        ├── JSON 검증
        ├── 기본 Blackboard 생성
        ├── Service/Condition/Action 노드 생성
        ├── Composite/Decorator 연결
        └── BehaviorTreeAsset 저장
```

기존 `BehaviorTreeJsonUtility`는 BT 에셋의 저수준 직렬화 포맷으로 유지한다.

| 방식 | 설명 | 권장 |
|------|------|------|
| 직접 생성 | `ScriptableObject.CreateInstance<BehaviorTreeAsset>()`와 노드 생성 API로 바로 `.asset` 생성 | 1차 권장 |
| BT Json 경유 | 몬스터 행동 JSON을 `BehaviorTreeJsonData`로 변환한 뒤 `BehaviorTreeJsonUtility.ImportFromData()` 호출 | 디버깅/툴 재사용에 유리 |

1차 구현은 BT Json 경유가 안전하다. 현재 `BehaviorTreeJsonUtility.ImportFromData()`가 이미 노드 생성, Blackboard 생성, Asset 저장을 처리하기 때문이다.

### 3.6.2 몬스터 행동 JSON 스키마

기본 스키마는 BT 노드 타입명을 직접 노출하지 않고, 몬스터 AI 도메인 용어로 작성한다.

```json
{
  "schemaVersion": 1,
  "id": "EnemyBehavior_Skeleton_Common",
  "displayName": "Skeleton Common",
  "actorKind": "Ground",
  "sourceBehaviorSo": "Assets/10.Datas/Actor/Enemy/BehaviorData/BehaviorData_skeleton_common.asset",
  "blackboard": {
    "tickInterval": 0.1,
    "enablePatrol": true,
    "optimalCombatDistance": 2.5,
    "minCombatDistance": 1.5,
    "personalSpaceDistance": 0.8
  },
  "rules": [
    {
      "name": "BlockedState",
      "priority": 1000,
      "when": [{ "condition": "IsBlockedEnemyState" }],
      "do": [{ "action": "KeepCurrentState" }]
    },
    {
      "name": "NoTarget",
      "priority": 900,
      "when": [{ "condition": "HasTarget", "invert": true }],
      "do": [{ "action": "PatrolOrIdle" }]
    },
    {
      "name": "TooClose",
      "priority": 800,
      "when": [
        { "condition": "HasTarget" },
        { "condition": "DistanceLessOrEqual", "value": "personalSpaceDistance" }
      ],
      "do": [{ "action": "Transition", "state": "Retreat" }]
    },
    {
      "name": "Attack",
      "priority": 700,
      "when": [
        { "condition": "HasTarget" },
        { "condition": "ActionDelayElapsed" },
        { "condition": "CanUseSkill" }
      ],
      "do": [
        { "action": "RequestAttackSlot" },
        { "action": "ExecuteAttack" }
      ]
    },
    {
      "name": "OutOfRange",
      "priority": 500,
      "when": [
        { "condition": "HasTarget" },
        { "condition": "DistanceGreater", "value": "optimalCombatDistance" }
      ],
      "do": [{ "action": "Transition", "state": "Chase" }]
    },
    {
      "name": "CombatIdle",
      "priority": 100,
      "select": "WeightedRandom",
      "choices": [
        { "weightKey": "guardChance", "action": "Transition", "state": "Guard" },
        { "weightKey": "retreatChance", "action": "Transition", "state": "Retreat" },
        { "weightKey": "circleWeight", "action": "Transition", "state": "Circle" }
      ]
    }
  ]
}
```

### 3.6.3 스키마 필드

| 필드 | 설명 |
|------|------|
| `schemaVersion` | 임포터 호환성 버전 |
| `id` | 생성할 BT 에셋 이름의 기준 |
| `displayName` | 에디터 표시명 |
| `actorKind` | `Ground`, `Flying`, `Boss` 등 변환 프리셋 선택 |
| `sourceBehaviorSo` | 기존 `EnemyBehaviorSO` 경로. 기본 거리/확률값을 가져오는 기준 |
| `blackboard` | JSON에서 명시적으로 override할 기본 Blackboard 값 |
| `rules` | 우선순위 기반 행동 규칙. 임포터가 Selector/Sequence/Condition/Action 노드로 변환 |
| `priority` | Root Selector 아래 배치 순서. 값이 높을수록 먼저 평가 |
| `when` | Condition 목록. 모두 성공해야 해당 rule 실행 |
| `do` | Action 목록. Sequence로 변환 |
| `select` | `WeightedRandom` 같은 선택 방식 |
| `choices` | 가중치 기반 후보 행동 목록. 고정값은 `weight`, 데이터 키 참조는 `weightKey` 사용 |

### 3.6.4 기존 Brain 로직과 JSON 매핑

| 기존 `EnemyAIController` 로직 | JSON 표현 | 생성될 BT 노드 |
|------------------------|-----------|----------------|
| 개입 금지 상태 검사 | `IsBlockedEnemyState` | `IsBlockedEnemyStateNode` + `ReturnSuccess/Running` |
| 타겟 없음 | `HasTarget` invert | `HasTargetNode` + `TransitionEnemyStateNode(Patrol/Idle)` |
| `PersonalSpaceDistance` 강제 후퇴 | `DistanceLessOrEqual` | `IsTargetInRangeNode` + `TransitionEnemyStateNode(Retreat)` |
| 공격 가능 판단 | `ActionDelayElapsed`, `CanUseSkill` | `HasEnemyActionDelayElapsedNode`, `CanUseEnemySkillNode` |
| 그룹 슬롯 요청 | `RequestAttackSlot` | `RequestEnemyAttackSlotNode` |
| 공격 실행 | `ExecuteAttack` | `ExecuteEnemyAttackNode` |
| 사거리 밖 추격 | `DistanceGreater` | `IsTargetInRangeNode` invert + `TransitionEnemyStateNode(Chase)` |
| Guard/Circle/Retreat 확률 | `WeightedRandom` choices | `WeightedRandomSelectorNode` |
| 플레이어 공격 반응 | `IsPlayerAttacking` | `BlackboardBoolConditionNode` + Guard/Flank |
| 플레이어 가드 반응 | `IsPlayerGuarding` | Charge/Attack 분기 |
| 페이즈별 확률 | `weight`에 `guardChance` 등 키 사용 | Blackboard 기반 가중치 해석 |

### 3.6.5 JSON 임포터 클래스 (구현 완료)

```csharp
namespace UPlayGround.AI.BehaviorTree.Editor
{
    public static class MonsterBehaviorTreeJsonImporter
    {
        [MenuItem("UPlayGround/Character/AI/Monster Behavior Json/Import Selected Json")]
        public static void ImportSelectedJson();

        [MenuItem("UPlayGround/Character/AI/Monster Behavior Json/Import Folder")]
        public static void ImportFolder();

        public static BehaviorTreeAsset ImportFromMonsterBehaviorJson(
            string absoluteJsonPath,
            string outputAssetPath);
    }
}
```

생성 경로 규칙:

| 입력 JSON | 출력 BT Asset |
|-----------|---------------|
| `SourceJson/Ground/EnemyBehavior_Skeleton_Common.json` | `Generated/BT_EnemyBehavior_Skeleton_Common.asset` |
| `SourceJson/Flying/EnemyBehavior_Griffin.json` | `Generated/BT_EnemyBehavior_Griffin.asset` |

> 현 시점 실제 출력 경로는 `Assets/10.Datas/AI/BehaviorTree/` 루트로 떨어지며 파일명 접두사 `BT_`가 누락된다. 임포터 규칙을 문서와 일치시키거나 문서 경로를 실제와 맞춰야 한다(부록 A 진행 상태 참고).

### 3.6.6 JSON Export 도구 (구현 완료)

기존 `EnemyBehaviorSO` 값을 바탕으로 초안 JSON을 생성한다.

```csharp
namespace UPlayGround.AI.BehaviorTree.Editor
{
    public static class EnemyBehaviorJsonExporter
    {
        [MenuItem("UPlayGround/Character/AI/Monster Behavior Json/Export From Selected BehaviorSO")]
        public static void ExportFromSelectedBehaviorSO();
    }
}
```

이 도구는 기존 C# 로직을 완전 자동 변환하지 않는다. 대신 `EnemyBehaviorSO`의 거리/확률/순찰/페이즈 데이터를 채운 기본 JSON 템플릿을 생성한다.

```
EnemyBehaviorSO
    └── EnemyBehaviorJsonExporter
        └── Monster Behavior Json 초안
            └── 수동 검토/수정
                └── MonsterBehaviorTreeJsonImporter
                    └── BehaviorTreeAsset
```

### 3.6.7 JSON 검증 규칙

| 검증 | 실패 처리 |
|------|-----------|
| `schemaVersion` 지원 여부 | Import 중단 |
| `id` 비어 있음 | Import 중단 |
| `actorKind` 지원 여부 | Import 중단 |
| `sourceBehaviorSo` 경로 존재 여부 | 경고. JSON 값만으로 생성 가능하면 계속 |
| 알 수 없는 condition/action | Import 중단 |
| `Transition`의 `state`가 `EnemyTransitionStateType`에 없음 | Import 중단 |
| 문자열 weight 키가 Blackboard 또는 `EnemyBehaviorSO`에 없음 | Import 중단 |
| `priority` 중복 | 경고. JSON 순서로 보정 |
| 생성될 BT에 RootNode 없음 | Import 중단 |

### 3.6.8 `EnemyBehaviorSO` 확장 (구현 완료, 2026-05-16)

몬스터별 BT Asset을 연결하기 위해 다음 필드를 추가한다. Skeleton Common (`BehaviorData_skeleton_common.asset`)에는 적용 완료.

```csharp
[Header("Behavior Tree")]
public BehaviorTreeAsset behaviorTree;
```

> **현 시점 동작**: 본 필드는 forward-compatible 데이터 슬롯이며, 런타임 `EnemyAIController`은 자신의 `BehaviorTreeRunner` 컴포넌트를 직접 참조한다. 즉 프리팹의 `BehaviorTreeRunner._treeAsset`이 1차 연결점이고, `EnemyBehaviorSO.behaviorTree`는 동일 자산을 참조하도록 양쪽을 맞춰 두는 용도다. Phase 5/7에서 Context/Service가 이 SO 필드를 자동 주입에 활용하도록 확장 예정.

JSON을 소스 오브 트루스로 사용할 경우:

```csharp
[Header("Behavior Tree Source")]
public TextAsset behaviorJson;
public BehaviorTreeAsset behaviorTree;
```

개발 중에는 JSON을 수정한 뒤 Importer로 BT Asset을 재생성하고, 런타임에서는 `behaviorTree`만 사용한다. 런타임에서 JSON을 파싱해 BT를 생성하지 않는다.

비행형까지 포함하려면 다음 구조도 가능하다:

```csharp
[Header("Behavior Tree")]
public BehaviorTreeAsset groundBehaviorTree;
public BehaviorTreeAsset flyingBehaviorTree;
```

초기에는 `behaviorTree` 하나만 추가하고, 트리 내부에서 지상/비행 분기를 나누는 방식이 단순하다.

### 3.6.9 `BehaviorPhase` 확장

페이즈별 완전 다른 행동이 필요할 때만 BT override를 허용한다.

```csharp
[Header("Behavior Tree Override")]
public bool overrideBehaviorTree;
public BehaviorTreeAsset behaviorTree;
```

기본 방침은 트리 교체보다 Blackboard 값 변경이다. 페이즈마다 트리를 갈아끼우면 디버깅과 재현성이 떨어진다.

## 3.7 마이그레이션 단계

### 3.7.1 Phase 1: 안전장치 추가 (완료)

| 작업 | 결과 |
|------|------|
| `EnemyBlackboardKeys` 추가 | 키 문자열 통일 |
| `IsBlockedEnemyStateNode` 추가 | BT가 피격/사망/공격 상태를 덮지 않음 |
| `TransitionEnemyStateNode` 확장 | 모든 지상 상태 전환 가능 (Circle/Guard/Charge/Flank/Counter 포함) |
| `ExecuteEnemyAttackNode` 수정 | 공격 슬롯 요청과 쿨다운 기록을 우회하지 않음 |
| `HasEnemyActionDelayElapsedNode`, `KeepCurrentStateNode`, `RequestEnemyAttackSlotNode` 추가 | 공격 후 대기 시간/개입 금지/그룹 슬롯 처리 |

### 3.7.2 Phase 1.5: 몬스터 행동 JSON 파이프라인 추가 (완료)

| 작업 | 결과 |
|------|------|
| Monster Behavior JSON 스키마 정의 | 사람이 읽고 수정 가능한 데이터로 표현 |
| `EnemyBehaviorJsonExporter` 추가 | 기존 `EnemyBehaviorSO`에서 JSON 초안 생성 |
| `MonsterBehaviorTreeJsonImporter` 추가 | 몬스터 행동 JSON을 BT Asset으로 변환 |
| Import Folder 메뉴 추가 | 여러 몬스터 JSON을 일괄 BT Asset으로 재생성 |
| JSON 검증 추가 | 잘못된 state/action/condition을 에셋 생성 전에 차단 |

### 3.7.3 Phase 2: 지상형 단순 몬스터 1종 전환 (자산/데이터 완료 / 프리팹 연결 + Play Mode 검증 남음)

Skeleton 계열처럼 기본 근접 행동만 필요한 몬스터를 선택한다.

| 구분 | 경로 |
|------|------|
| JSON 원본 | `Assets/10.Datas/AI/BehaviorTree/SourceJson/Ground/EnemyBehavior_Skeleton_Common.json` |
| 생성 대상 BT Asset | `Assets/10.Datas/AI/BehaviorTree/Generated/BT_EnemyBehavior_Skeleton_Common.asset` (GUID `683ed5e0908c92d498a1ea33bd9f1ee2`) |
| 기존 BehaviorSO | `Assets/10.Datas/Actor/Enemy/BehaviorData/BehaviorData_skeleton_common.asset` (behaviorTree 필드에 위 BT Asset 참조 채워짐) |
| 적용 프리팹 | `Assets/03.Prefabs/Actor/Monster/MonsterActor_Skeleton_Common.prefab` (BehaviorTreeRunner 추가 + Tree Asset 연결 — 사용자 작업, 부록 A.2 참조) |

에디터 메뉴:

```
UPlayGround/Character/AI/Monster Behavior Json/Import Selected Json
UPlayGround/Character/AI/Monster Behavior Json/Import Folder
UPlayGround/Character/AI/Monster Behavior Json/Export From Selected BehaviorSO
```

사용 순서:

1. `Import Selected Json`을 선택한다.
2. `Assets/10.Datas/AI/BehaviorTree/SourceJson/Ground/EnemyBehavior_Skeleton_Common.json`을 고른다.
3. 생성된 BT Asset을 확인한다.
4. 노드가 생성되지 않았다면 기존 `Behavior Tree Json/Import Json` 메뉴를 사용한 것이 아닌지 먼저 확인한다.

Phase 2 완료 처리는 위 JSON을 Import해서 BT Asset을 생성하고, Skeleton 계열 프리팹에 `BehaviorTreeRunner`를 연결한 뒤 Play Mode에서 아래 검증 항목을 통과한 시점으로 본다.

검증 항목:

- 타겟 없음: `Idle` 또는 `Patrol`
- 타겟 획득: `Chase`
- 사거리 진입: `Attack`
- 너무 가까움: `Retreat`
- 피격 중: BT가 상태를 덮어쓰지 않음
- 사망 중: BT가 다시 `Chase`/`Attack`으로 전환하지 않음
- 공격 슬롯: 그룹 몬스터가 동시에 과도하게 공격하지 않음

### 3.7.4 Phase 3: 기존 `EnemyAIController.MakeDecision()` 비활성화 (완료)

`BehaviorTreeRunner`가 활성인 몬스터는 Brain의 의사결정을 실행하지 않는다. 실제 구현은 `EnemyAIController.Update`의 `_decisionInterval` 분기 안에서 가드한다.

```csharp
[SerializeField] private BehaviorTreeRunner _behaviorTreeRunner;

protected virtual void Awake()
{
    // ...
    _behaviorTreeRunner ??= GetComponent<BehaviorTreeRunner>();
}

protected virtual void Update()
{
    _decisionTimer += Time.deltaTime;
    _actionCooldownTimer += Time.deltaTime;

    if (_decisionTimer >= _decisionInterval)
    {
        _decisionTimer = 0f;
        if (_behaviorTreeRunner == null || !_behaviorTreeRunner.IsRunning)
            MakeDecision();
    }
    // ...
}
```

이 단계는 과도기용이다. 모든 몬스터 전환이 끝나면 레거시 분기는 제거한다.

> 2026-05-16 3차 갱신: `EnemyFlyingAIController`에도 동일한 `_behaviorTreeRunner.IsRunning` 가드를 적용했다. Runner가 실행 중이면 `Start()`의 초기 상태 전환과 `Update()`의 `MakeDecision(stateName)` 호출을 건너뛰고, 기존 공중 루프 콜백은 유지한다.

### 3.7.5 Phase 4: 전술 반응 이전 (완료)

기존 `TryReactToPlayerState()`의 5개 조건이 Blackboard로 흘러나오는 경로를 완성했다. 조건 노드는 모두 기존 `BlackboardBoolConditionNode` 1종으로 표현된다(별도 신규 노드 없음).

| 기존 조건 | Blackboard 키 | BT 조건 | JSON 매핑 |
|-----------|---------------|---------|-----------|
| `EnemyTacticalMemory.IsPlayerAttacking()` | `IsPlayerAttacking` | `BlackboardBoolCondition` | `"condition": "IsPlayerAttacking"` |
| `EnemyTacticalMemory.IsPlayerGuarding()` | `IsPlayerGuarding` | `BlackboardBoolCondition` | `"condition": "IsPlayerGuarding"` |
| `EnemyTacticalMemory.IsPlayerStaggered()` | `IsPlayerStaggered` | `BlackboardBoolCondition` | `"condition": "IsPlayerStaggered"` |
| `EnemyTacticalMemory.IsPlayerRecovering()` | `IsPlayerRecovering` | `BlackboardBoolCondition` | `"condition": "IsPlayerRecovering"` |
| `EnemyTacticalMemory.IsPlayerDodgingFrequently()` | `IsPlayerDodgingFrequently` | `BlackboardBoolCondition` | `"condition": "IsPlayerDodgingFrequently"` |

핵심 구현:

- `SyncEnemyMemoryService` 신규 — `EnemyTacticalMemory`의 5개 메서드 결과를 Blackboard의 5개 bool 키로 매 Service Tick에 기록한다. Memory 컴포넌트가 없으면 모두 false. DebugTrace에 매 갱신 결과가 한 줄로 기록된다.
- 임포터가 Root Selector에 `SyncEnemyMemoryService`를 `SyncEnemyBlackboardService`와 함께 자동 부착한다. JSON 측에서 별도 선언 불필요.
- `MonsterBehaviorTreeJsonImporter`는 이미 5개 condition을 `BlackboardBoolConditionNode`로 매핑하고 있어 JSON 스키마 변경 없음. `invert` 플래그도 그대로 지원.

JSON 사용 예 (Skeleton Common에 "플레이어 공격 반응" 규칙 추가):

```json
{
  "name": "ReactToPlayerAttack",
  "priority": 600,
  "when": [
    { "condition": "HasTarget" },
    { "condition": "IsPlayerAttacking" }
  ],
  "do": [
    { "action": "Transition", "state": "Guard" }
  ]
}
```

> 프리팹에 `EnemyTacticalMemory` 컴포넌트가 붙어 있어야 5개 키가 의미를 가진다. 부록 A에서 권장 컴포넌트로 표기됨.

### 3.7.6 Phase 5: 페이즈 이전 (완료)

`UpdatePhase()`는 Context에 남기고, 페이즈 결과를 Blackboard에 기록한다.

```
SyncEnemyPhaseService
├── HP Percent 계산
├── EnemyAIContext.UpdatePhase()
└── Blackboard(CurrentPhaseName, PhaseIndex, Phase Options) 갱신
```

BT는 `CurrentPhaseName`, `allowCharge`, `allowFlank`, `maxConsecutiveAttacks` 같은 값을 조건/가중치에 사용한다.

구현 반영:

- `EnemyAIContext`에 `BehaviorData`, `CurrentPhase`, `HealthPercent`, `UpdatePhase(float)`를 추가하고 `EnemyAIController`이 override한다.
- `SyncEnemyPhaseService` 신규. HP 비율을 읽어 `EnemyAIContext.UpdatePhase()`를 호출하고 `HpPercent`, `CurrentPhaseName`, `PhaseIndex`, `AllowCharge`, `AllowFlank`, `MaxConsecutiveAttacks`, `ContinueAttackChance`, `GuardChance`, `RetreatChance`를 Blackboard에 기록한다.
- `IsEnemyPhaseNode` 신규. 페이즈 이름 또는 인덱스 조건을 BT에서 직접 평가한다.
- `MonsterBehaviorTreeJsonImporter`가 Root Selector에 `SyncEnemyPhaseService`를 자동 부착하고 기본 Blackboard 키를 생성한다.

잔여:

- 현재 생성된 Skeleton BT Asset은 기존 자산이므로 새 Service가 자동 반영되지 않았다. JSON 원본 재임포트 또는 에디터에서 Root Service 추가가 필요하다.
- 페이즈별 공격 풀/스킬 선택(`SelectEnemySkillNode`)은 아직 미구현이다.

### 3.7.7 Phase 6: 비행형 전환 (완료)

지상형 안정화 이후 전면 이전한다. 2026-05-16 4차 갱신에서 Context 분리, 5차 갱신에서 비행 BT 노드와 JSON Importer 라우팅까지 완료.

| 순서 | 작업 |
|------|------|
| 1 | `EnemyFlyingAIController` 데이터를 `EnemyFlyingAIContext`로 분리 — **완료 (2026-05-16, 4차)**. `EnemyAIContext`와 형제 관계의 새 abstract MonoBehaviour. `EnemyFlyingAIController`이 상속하고 9개 Flying State가 `EnemyFlyingAIContext`만 의존하도록 생성자 일괄 변경 |
| 2 | `Flying_Chase`, `Flying_GroundAttack`, `Flying_Retreat`, `Flying_Circle` 전환 노드 추가 — **완료 (2026-05-16, 5차)**. `FlyingEnemyTransitionStateType` enum 10종 + `TransitionFlyingEnemyStateNode` 단일 Action으로 통합. `ResetFlyingCountersNode`/`ResetFlyingAirCountersNode`/`DescendFlyingNode`/`RequestFlyingAttackSlotNode` Action + `IsFlyingAirState`/`IsFlyingGroundCombatState`/`IsAirAttackLimitReached`/`ShouldFlyingTakeOff`/`FlyingCanUseSkill` Condition |
| 3 | 지상 루프를 BT로 이전 — **완료 (2026-05-16, 8차)**. 기존 `EnemyFlyingChase/Retreat/Circle` 상태 콜백은 BT가 참조할 카운터/저수준 상태 완료만 담당하고, 주기적 행동 선택은 제거 |
| 4 | `TakeOff` 조건을 BT로 이전 — **완료 (5차)**. `ShouldFlyingTakeOffNode` + `TransitionFlyingEnemyStateNode(TakeOff)`로 JSON 표현 가능 |
| 5 | `AirCircle` 공격 횟수와 `Dive`/`Land` 선택을 BT Condition/Action으로 이전 — **완료 (2026-05-16, 6차)**. `HasDiveSkillAvailableNode` + `RollDiveChanceNode` + `SelectFlyingDiveSkillNode`로 Dive/Land 분기를 JSON 두 룰(DescendToDive 우선, DescendToLand 폴백)로 표현 가능 |
| 6 | `EnemyFlyingAIController.MakeDecision()` 제거 — **완료 (2026-05-16, 8차)**. BT 미연결 폴백 의사결정 제거. 비행형 프리팹에 실행 중인 Runner가 없으면 경고만 남긴다 |

JSON Importer (`MonsterBehaviorTreeJsonImporter`)는 `actorKind: "Flying"` 분기 처리. 비행 actorKind에서는 `SyncEnemyMemoryService` / `SyncEnemyPhaseService`를 Root에 부착하지 않는다 (`EnemyTacticalMemory` / `EnemyAIContext` 비대상). 신규 condition: `IsCurrentState`(value=상태이름), `IsFlyingAirState`, `IsFlyingGroundCombatState`, `IsAirAttackLimitReached`, `ShouldFlyingTakeOff`, `FlyingCanUseSkill`. 신규 action: `FlyingTransition`(state=`FlyingEnemyTransitionStateType`), `FlyingPatrolOrIdle`, `ResetFlyingCounters`, `ResetFlyingAirCounters`, `DescendFlying`, `RequestFlyingAttackSlot`. 샘플: `Assets/10.Datas/AI/BehaviorTree/SourceJson/Flying/EnemyBehavior_FlyingBoss_Common.json`.

> Context 분리 설계 메모: 비행형은 `EnemyAIContext`를 상속하지 않고 형제로 둔다. 이유는 (a) `BehaviorData(EnemyBehaviorSO)`/페이즈/Guard 같은 지상 전용 추상 멤버를 비행이 의미 없이 구현해야 하고, (b) 기존 `TransitionEnemyStateNode`가 `GetComponentCached<EnemyAIContext>()`로 지상 State를 만들기 때문에 비행 액터가 상속하면 잘못된 State가 생성될 수 있다. 비행 전용 BT 노드는 `GetComponentCached<EnemyFlyingAIContext>()`로 조회한다.

### 3.7.8 Phase 7: 레거시 Brain 제거 (완료)

2026-05-16 8차 갱신에서 하드코딩 의사결정 폴백을 제거했다.

- `EnemyAIController.MakeDecision()` 및 관련 거리/확률 기반 분기 제거.
- `EnemyFlyingAIController.MakeDecision()` 및 비행형 BT 미연결 폴백 분기 제거.
- `EnemyAIController.Update()`는 전술 메모리 동기화와 BT용 타이머 갱신만 담당한다.
- `EnemyFlyingAIController`의 State 콜백은 카운터/타임스탬프 갱신만 담당한다.
- `BehaviorTreeRunner.SetTreeAsset()` 추가. 지상형은 `EnemyBehaviorSO.behaviorTree`가 있으면 런타임에 Runner를 보장하고 BT를 시작한다.
- 상태 클래스가 직접 `EnemyAIController` 타입을 요구하는 생성자를 `EnemyAIContext` 기반으로 교체 — 지상 Enemy State 1차 완료.
- `TransitionEnemyStateNode`와 `ExecuteEnemyAttackNode`는 더 이상 `EnemyAIController` 캐스팅을 요구하지 않는다.

남은 것은 이름 정리(`EnemyAIController` → 전용 Context/Adapter 명칭)와 프리팹 직렬화 정리다. 클래스명은 `MonsterActor.Brain`, 그룹 컨트롤러, 패리 처리, 에디터 도구가 아직 참조하므로 별도 호환 레이어 없이 즉시 삭제하지 않는다.

## 3.8 프리팹 셋업

최종 몬스터 프리팹 필수 컴포넌트:

| 컴포넌트 | 필수 여부 | 설명 |
|----------|-----------|------|
| `MonsterActor` | 필수 | 몬스터 본체 |
| `EnemyAIContext` | 필수 | BT 노드가 참조하는 데이터/API |
| `BehaviorTreeRunner` | 필수 | BT 실행 |
| `EnemyDetection` | 필수 | 타겟 탐지 |
| `EnemyTacticalMemory` | 권장 | 플레이어 상태 반응형 AI |
| `EnemyCombat` | 필수 | 스킬 선택/공격 |
| `ActorMovementController` | 필수 | State Machine 호스트 |
| `MonsterGroupController` | 그룹 단위 | 공격 슬롯/경보 |

`BehaviorTreeRunner` 설정:

| 필드 | 권장값 |
|------|--------|
| `_startOnEnable` | `true` |
| `_tickMode` | `UpdateInterval` |
| `_tickInterval` | `0.1` |
| `_restartWhenComplete` | `true` |
| `_resetValuesOnRestart` | `false` |
| `_debugMode` | 개발 중 `true`, 릴리즈 전 몬스터 수에 따라 조정 |

---

# Part 4. 통합 방향, 위험, 결론

## 4.1 통합 방향 비교

| 옵션 | 적합 시나리오 | 주요 위험 |
|---|---|---|
| **A. 전면 migrate** | 적·보스 다양성을 늘리고 디자이너(본인)가 데이터로 빠르게 튜닝하고 싶을 때 | 페이즈/리듬/그룹/메모리를 모두 노드로 풀어야 함 → 노드 30~50개 추가, 트리 비대화 위험 (Anguelov 경고) |
| **B. 보스/특수 적만 BT** | 잡몹은 단순하고 보스만 복잡한 패턴 — 전형적 액션게임 구조 | 두 시스템 코드 일관성 비용. 디버깅 도구 둘 |
| **C. BT 폐기 + EnemyAIController 강화** | BT 인프라 유지 비용 > 이득이라 판단 시 | BT 폴더 30+ 파일 삭제 결정. 향후 데이터 드리븐 요구 시 처음부터 다시 |

### 객관적 권고

옵션 B(보스/특수 한정 augment)가 위험도 대비 이득이 가장 균형 잡힘. 이유:

- 잡몹은 `EnemyAIController`의 페이즈/메모리/그룹 코드가 이미 동작 중 → 무리하게 BT로 옮기면 회귀 위험
- 보스/엘리트는 패턴 복잡도가 임계점 넘으면 데이터 authoring이 필요해짐 → BT가 이때 빛남
- §2.1, §2.2~2.7 항목은 옵션 B에서도 모두 의미 있음

다만 본 가이드 Part 3가 채택한 방향은 **단순 잡몹부터 단계적으로 전환하는 점진적 A** — 보스 작성 전에 인프라와 데이터 파이프라인을 검증할 필요가 있어서다. 보스 전용 augment로 갈지, 잡몹까지 모두 옮길지는 Phase 2 검증 후 재판단한다.

## 4.2 즉시 실행 권장 순서 (통합 방향 무관)

1. **BlackboardKey 타입 안전화 + Service 노드 도입** → 완료 (§2.1)
2. **WeightedRandomSelector + Subtree** → 완료 (§2.1)
3. **Monitor Decorator + Tick LOD** → Phase R3 / 추후
4. **시범 보스 1마리를 BT로 작성** → 데이터로 검증 후 옵션 B vs A 본격 판단

## 4.3 안 해도 되는 것 / 지양할 것

- **GOAP / HTN 도입** — 현재 요구하는 AI 표현 범위에 비해 저작·디버깅 비용이 과하다. 전담 AI 팀과 툴 체인을 전제로 한 도구
- **BT 안에서 페이즈 데이터까지 노드 분기로 풀기** — 디자인 의도 손상. 페이즈는 외부 SO 유지하고 BT는 phase-aware 노드 몇 개로 처리
- **현재 `EnumerateConditions` 폴링을 그대로 두고 노드 수만 늘리기** — 누적 비용 폭증
- **페이즈별 트리 교체를 기본값으로** — 디버깅과 재현성 저하

## 4.4 위험 요소

| 위험 | 대응 |
|------|------|
| BT가 공격/피격/사망 상태를 덮어씀 | `IsBlockedEnemyStateNode`를 모든 행동 분기 앞에 둔다 |
| `ExecuteEnemyAttackNode`가 그룹 슬롯을 우회 | 공격 슬롯 요청을 Action Node 또는 Context API에 통합 |
| Blackboard 문자열 오타 | `EnemyBlackboardKeys` 상수화 + `BlackboardKeySelector` |
| 페이즈별 트리 교체로 디버깅 어려움 | 기본은 Blackboard 값 변경, 꼭 필요한 경우만 트리 override |
| 비행형 상태 콜백과 BT Tick 충돌 | 비행형은 지상 루프부터 단계적으로 전환 |
| 기존 State 생성자가 `EnemyAIController` 타입에 묶임 | `EnemyAIContext` 인터페이스 또는 베이스 타입으로 생성자 교체 |
| 모든 몬스터 동시 Tick 비용 | Tick interval 조정, 거리 기반 Runner Pause, Debug Mode 제한 |
| JSON과 BT Asset 불일치 | JSON을 소스 오브 트루스로 두고 Generated BT Asset은 재생성 가능 산출물로 취급 |
| 사람이 작성한 JSON 오타 | Import 전 스키마/노드/상태/Blackboard 키 검증 |
| 저수준 BT JSON과 몬스터 행동 JSON 혼동 | `BehaviorTreeJsonUtility`는 BT round-trip용, `MonsterBehaviorTreeJsonImporter`는 몬스터 행동 변환용으로 분리 |
| ~~Subtree 클론 누수~~ | 2026-05-16 해소. `BehaviorTreeAsset.DisposeRuntime` + Runner/SubtreeNode 정리 경로 |
| Conditional Abort 전체 트리 폴링 비용 | Composite 단위 등록제 평가 또는 Monitor Decorator로 교체 |

## 4.5 검증 체크리스트

### 지상형

- [ ] 타겟 미감지 상태에서 Idle/Patrol이 정상 동작한다.
- [ ] 타겟 감지 시 Chase로 전환한다.
- [ ] 사거리 안에서 공격 스킬이 선택된다.
- [ ] 공격 중 BT Tick이 Attack 상태를 덮어쓰지 않는다.
- [ ] 피격 중 Chase/Attack으로 복귀하지 않는다.
- [ ] 사망 중 BT가 재시작되어도 행동하지 않는다.
- [ ] 그룹 공격 슬롯이 유지된다.
- [ ] 공격 실패 후 반격 창이 유지된다.
- [ ] 페이즈 전환 후 Charge/Flank/연속 공격 제한이 반영된다.
- [ ] 원본 JSON을 다시 임포트해도 동일한 BT Asset 구조가 재생성된다.
- [ ] JSON의 잘못된 state/action/condition이 Import 단계에서 차단된다.

### 비행형

- [ ] 지상 체류 시간이 끝나면 TakeOff로 전환한다.
- [ ] 공중 공격 횟수 제한이 동작한다.
- [ ] Dive 스킬이 없으면 Land로 내려온다.
- [ ] Dive 중 BT가 다른 상태로 덮어쓰지 않는다.
- [ ] 착지 후 지상 루프로 복귀한다.
- [ ] 타겟 소실 시 공중에서 안전하게 착지한다.

### BT 인프라

- [ ] Runner Pause/Resume/Manual Tick이 동작한다.
- [ ] Parallel 실패/성공 확정 시 다른 Running 자식이 Abort된다.
- [ ] Conditional Abort `Self`/`LowerPriority`/`Both`가 동작한다.
- [ ] Decorator 자식 수가 항상 1이다.
- [ ] Breakpoint 진입 시 Runner가 Pause된다.
- [ ] Validator가 잘못된 JSON Import 후 오류 위치를 표시한다.

## 4.6 완료 기준

BT 완전 전환은 다음 조건을 모두 만족해야 완료로 본다.

- 모든 몬스터 프리팹이 `BehaviorTreeRunner`와 BT Asset을 가진다.
- 모든 몬스터의 기존 행동은 JSON 원본으로 작성되어 있고, 해당 JSON에서 BT Asset을 재생성할 수 있다.
- `EnemyAIController.MakeDecision()`과 `EnemyFlyingAIController.MakeDecision()`이 제거된다.
- 지상형/비행형 몬스터의 행동 선택은 BT Asset에서 확인 가능하다.
- 기존 State Machine은 KCC 물리와 애니메이션 생명주기만 담당한다.
- 공격 슬롯, 페이즈, 전술 메모리, 순찰, 비행 루프가 BT 경로에서 모두 동작한다.
- Play Mode에서 BT Debug Trace로 현재 실행 노드와 실패 원인을 확인할 수 있다.
- Skeleton 계열, 원거리형, 엘리트/보스형, 비행형 대표 몬스터가 각각 수동 검증을 통과한다.
- Runner가 `Start`, `Stop`, `Pause`, `Resume`, `Restart`, `Manual Tick`을 지원한다.
- Conditional Abort가 `Self`, `LowerPriority`, `Both` 기준으로 동작한다.
- Breakpoint, Disable Node, Debug Trace로 Play Mode 문제를 추적할 수 있다.

## 4.7 결론

BT 인프라는 §2.1 4.1 단계로 핵심 5개 갭(Service/WeightedSelector/Subtree/BlackboardKey 타입화) 중 4개가 해소되었고, 코드 점검 결과 R1 Runner 실행 제어와 R2 Composite Abort/Reset도 이미 구현 완료, R3 Conditional Abort는 기본 평가 경로(`TryEvaluateSelfAbort`/`TryEvaluateLowerPriorityAbort`)까지 들어가 있다. 몬스터 AI 전환은 Phase 1/1.5/3/4/5 완료, Phase 6의 BT 노드화와 Phase 7 레거시 의사결정 제거까지 완료되었다. BT 노드는 더 이상 지상형 공격/전이에서 `EnemyAIController` 캐스팅을 요구하지 않으며, 플레이어 상태 반응형 분기와 HP 페이즈 Blackboard 동기화가 BT 경로에서 동작한다.

남은 핵심 차단 요소(2026-05-16 8차 갱신 기준):

- **Skeleton 프리팹 Play Mode 검증** — 자산 이동·`EnemyBehaviorSO.behaviorTree` 연결은 완료. 지상형은 런타임에 `EnemyBehaviorSO.behaviorTree`로 `BehaviorTreeRunner`를 보장하므로 수동 프리팹 컴포넌트 추가는 필수가 아니지만, 프리팹 직렬화 정리는 권장된다.
- **Generated Skeleton BT 재임포트 필요** — `MonsterBehaviorTreeJsonImporter`는 이제 `SyncEnemyPhaseService`를 자동 부착하지만, 기존 생성 BT Asset에는 자동 반영되지 않는다.
- **비행형 프리팹 BT 연결 + Play Mode 검증** — 비행형은 `EnemyFlyingSettingsSO`에 BT Asset 필드가 없으므로 프리팹의 `BehaviorTreeRunner` 연결이 필요하다.
- **컴포넌트 이름 정리** — `EnemyAIController`/`EnemyFlyingAIController` 클래스명은 Context 역할로 남아 있다. 참조 정리 후 명칭 변경 가능.
- **Monitor Decorator 부재** (Phase R3 최적화, 의도적 보류) — 기본 Conditional Abort 동작은 있으나 `EnumerateConditions` 재귀 폴링이라 트리 깊이에 비례한 비용 누적. 적 수가 늘면 부담.
- **R6 UX 잔여** — 노드 위 오류 배지, 저장 전 Error 경고 미구현.

다음 액션 순서 권고:

1. Skeleton JSON 재임포트 또는 Root Service 수동 추가로 `SyncEnemyPhaseService`를 기존 `BT_EnemyBehavior_Skeleton_Common`에 반영.
2. Skeleton을 Play Mode에서 부록 C.1/C.2/C.3 테스트 그래프 및 §4.5 지상형 체크리스트로 검증 → Phase 2/5 실제 검증 종료.
3. 비행 보스 프리팹에 `BehaviorTreeRunner` + 위 샘플 JSON 임포트 결과를 연결, Play Mode에서 BT 주도 비행 루프 검증.
4. 대표 몬스터 검증 후 `EnemyAIController`/`EnemyFlyingAIController` 명칭을 Context/Adapter 용어로 정리할지 판단.
5. 시범 보스 1마리를 BT로 작성해 옵션 A vs B를 본격 판단.
6. 부하 측정 후 필요해지면 R3 Monitor Decorator로 폴링 비용 최적화.

---

# 부록 A. 진행 상태

## A.1 2026-05-16 (8차) 기준 스냅샷

8차 갱신 핵심: Phase 7 레거시 의사결정 제거. `EnemyAIController.MakeDecision()`/`EnemyFlyingAIController.MakeDecision()`과 관련 BT 미연결 폴백을 제거했다. `EnemyAIController`은 `EnemyBehaviorSO.behaviorTree`가 있으면 런타임에 `BehaviorTreeRunner`를 보장하고 `SetTreeAsset` 후 시작한다. 비행형 State 콜백은 카운터/타임스탬프 갱신만 수행하고 다음 행동 선택은 BT가 담당한다.

7차 갱신 핵심: `MonsterBehaviorTreeJsonImporter`의 `Validate`에 `actorKind`별 노드 매핑 검사 추가. 지상 전용 condition/action(`CanUseSkill`, `Transition`, `PatrolOrIdle`, `RequestAttackSlot`, `ExecuteAttack`)을 `actorKind=Flying` JSON이 참조하면 import 단계에서 명확한 메시지로 거부. 비행 전용 노드도 동일하게 `actorKind=Ground`에서 거부. 런타임에서 `GetComponentCached<EnemyAIContext>()` null 폴백으로 조용히 실패하던 footgun을 차단.

6차 갱신 핵심: Phase 6 step 5 마무리. `EnemyFlyingAIController.TransitionToDescend`를 분해 가능한 primitives로 리팩터링 — `DiveChance` 프로퍼티, `HasDiveSkillAvailable()`, `SelectAndSetDiveSkill()`을 Context에 추가. 신규 BT 노드 3종: `HasDiveSkillAvailableNode`(Condition), `RollDiveChanceNode`(Condition), `SelectFlyingDiveSkillNode`(Action). Importer가 신규 노드를 인식. 샘플 JSON은 단일 `DescendFlying`을 두 룰(`DescendToDive` priority 960, `DescendToLand` priority 950)로 교체 — JSON에서 Selector 폴백 패턴으로 Dive 시도 후 Land 떨어짐을 표현. Brain 폴백 경로도 동일 primitives를 호출하므로 BT/비-BT 모두 동작.

5차 갱신 핵심: 비행 전용 BT 노드 + JSON Importer 라우팅 추가. 신규 enum `FlyingEnemyTransitionStateType`(10종), 신규 Action 5종(`TransitionFlyingEnemyStateNode`, `ResetFlyingCountersNode`, `ResetFlyingAirCountersNode`, `DescendFlyingNode`, `RequestFlyingAttackSlotNode`), 신규 Condition 5종(`IsFlyingAirStateNode`, `IsFlyingGroundCombatStateNode`, `IsAirAttackLimitReachedNode`, `ShouldFlyingTakeOffNode`, `FlyingCanUseSkillNode`). `EnemyFlyingAIContext`에 `CanUseSkill`/`ShouldTakeOff`/`TryRequestAttackSlot`/`NotifyBTAttackStarted`/`TransitionToDescend` 추상 메서드 추가. `MonsterBehaviorTreeJsonImporter`는 `actorKind=Flying`을 인식해 Memory/Phase Service를 부착하지 않고 비행 condition/action을 디스패치. 샘플 JSON: `Assets/10.Datas/AI/BehaviorTree/SourceJson/Flying/EnemyBehavior_FlyingBoss_Common.json`.

4차 갱신 핵심: `EnemyFlyingAIContext` abstract MonoBehaviour 신설(`EnemyAIContext`와 형제). `EnemyFlyingAIController`이 상속하고 9개 Flying State(`EnemyFlyingChase/Patrol/Circle/Retreat/TakeOff/AirCircle/Dive/Land/GroundAttackState`)의 생성자/필드 타입이 `EnemyFlyingAIContext`로 일괄 전환됨.



| 단계 | 상태 | 반영 내용 | 남은 작업 |
|------|------|-----------|-----------|
| §2.1 4.1 즉시 가치 (Service/BlackboardKeySelector/WeightedRandom/Subtree) | 완료 | 12개 파일 추가/수정. AAA §1.2.1 핵심 5개 중 4개 해소. DebugTrace에 Service tick 반영(`BTServiceNode.ServiceEnter/Tick/Exit`에서 `Record`). Subtree 클론 누수 정리(`BehaviorTreeAsset.DisposeRuntime` + Runner/SubtreeNode 정리 경로) | 기존 6개 노드 BlackboardKeySelector 마이그레이션 (의도적 보류) |
| §2.2 R1 Runner 실행 제어 | 완료 | `EnableBehavior`/`DisableBehavior`/`PauseTree`/`ResumeTree`/`TickOnce`/`StepTick`/`RequestPauseFromNode` + `_tickMode`(Manual 포함)/`_restartWhenComplete`/`_resetValuesOnRestart` 모두 `BehaviorTreeRunner.cs`에 구현 | 부록 C.1 테스트 그래프로 Play Mode 검증 |
| §2.3 R2 Composite Abort/Reset | 완료 | Sequence/Selector `OnStop`에서 `AbortRunningChildren` + `_currentIndex` 리셋, Parallel 양방향(`requireAllSuccess` true/false) Abort 전부 구현 | 부록 C.2 Parallel Abort 테스트로 Play Mode 검증 |
| §2.4 R3 Conditional Abort | 부분 | `BTConditionNode.EvaluateAbortChanged` + `BTCompositeNode.TryEvaluateSelfAbort`/`TryEvaluateLowerPriorityAbort` + Sequence/Selector `TryHandleConditionalAbort` 구현. Debug Trace 기록까지 완료 | Monitor Decorator 도입(폴링→등록제, 의도적 보류), 부록 C.3 테스트로 Play Mode 검증 |
| §2.5 R4 Decorator 확장 | 완료 | Inverter/Cooldown/Repeat/ReturnSuccess/ReturnFailure/UntilSuccess/UntilFailure/Timeout 8종 + 신규 GuardConditionNode/ForceAbortNode 추가로 10종 | — |
| §2.6 R5 Debug Trace/Breakpoint | 완료 | `BTNode._disabled`/`_breakpoint` + Tick 시 자동 호출, NodeView 우클릭 토글, BlackboardView Asset/Runtime/Runtime Only 3열 표시, NodeView `UpdateStateColor` 실시간 색상, ServiceEnter/Tick/Exit DebugTrace 기록까지 모두 구현 | — |
| §2.7 R6 Validator 고도화 | 부분 | 오류 클릭 이동(`EditorWindow.cs:800-802`), JSON Import 자동 Validate(`BehaviorTreeJsonUtility.LogValidation`) 완료 | 노드 위 오류 배지, 저장 전 Error 경고 |
| §3.7.1 Phase 1 안전장치 | 완료 | `EnemyBlackboardKeys`, `IsBlockedEnemyStateNode`, `HasEnemyActionDelayElapsedNode`, `KeepCurrentStateNode`, `RequestEnemyAttackSlotNode`, `ExecuteEnemyAttackNode` 슬롯/통지 | Play Mode 검증 |
| §3.7.2 Phase 1.5 JSON 파이프라인 | 완료 | `MonsterBehaviorTreeJsonImporter`, `EnemyBehaviorJsonExporter`, Skeleton JSON 샘플 | — |
| §3.7.3 Phase 2 Skeleton 1종 전환 | 부분 | 임포터 라우팅 + 자산 이동·리네임 + 데이터 연결 완료 — `Generated/BT_EnemyBehavior_Skeleton_Common.asset`으로 이동(GUID 유지). `EnemyBehaviorSO.behaviorTree` 필드 추가 후 `BehaviorData_skeleton_common.asset`에 위 BT Asset 참조 기록 | Skeleton 프리팹에 BehaviorTreeRunner 컴포넌트 추가 + Tree Asset 연결 (부록 A.2), Play Mode 검증 |
| §3.7.4 Phase 3 Brain 비활성화 | 완료 | `EnemyAIController.Update`의 주기적 의사결정 호출 제거. 이제 메모리 동기화와 BT용 타이머 갱신만 수행 | Play Mode에서 BT Runner 활성 시 BT만 상태 전이를 주도하는지 검증 |
| §3.7.5 Phase 4 전술 반응 이전 | 완료 | `SyncEnemyMemoryService` 신규. 임포터가 Root Selector에 자동 부착. 5개 bool 키 동기화 경로 완성. JSON 측 condition 매핑은 기존 그대로 | 프리팹에 `EnemyTacticalMemory` 부착 확인, Play Mode에서 키 갱신 검증 |
| §3.7.6 Phase 5 페이즈 이전 | 완료 | `SyncEnemyPhaseService` + `IsEnemyPhaseNode` 신규. `EnemyAIContext`에 `BehaviorData`/`CurrentPhase`/`HealthPercent`/`UpdatePhase` 추가. 임포터가 Phase Service와 기본 Blackboard 키 자동 생성 | 기존 생성 BT Asset 재임포트 또는 수동 Service 추가, Play Mode 검증 |
| §3.7.7 Phase 6 비행형 전환 | 완료 | 4차: `EnemyFlyingAIContext` 분리 + 9개 Flying State 전환. 5차: 비행 BT 노드 11종 + Importer 라우팅 + 샘플 JSON. 6차: Dive/Land 분기 BT 내재화. 8차: 비행형 `MakeDecision()` 폴백 제거 | 보스 프리팹에 `BehaviorTreeRunner` 연결 + Play Mode 검증 |
| §3.7.8 Phase 7 레거시 제거 | 완료 | 지상 Enemy State 생성자와 `TransitionEnemyStateNode`/`ExecuteEnemyAttackNode`의 `EnemyAIController` 직접 요구를 `EnemyAIContext`로 교체. 지상/비행 `MakeDecision()` 제거. 지상형은 SO의 BT Asset으로 Runner 런타임 보장 | 컴포넌트 리네임 판단, 프리팹 직렬화 정리 |
| `EnemyAIContext` 도입 | 완료 | `Assets/02.Scripts/GameActor/Component/Enemy/EnemyAIContext.cs` — abstract MonoBehaviour. `EnemyAIController`이 상속하고 페이즈/거리/순찰/공격 슬롯/공격 후처리 멤버를 `override`. BT 노드(`ExecuteEnemyAttackNode`, `RequestEnemyAttackSlotNode`, `CanUseEnemySkillNode`, `IsEnemyPatrolEnabledNode`, `TransitionEnemyStateNode`)가 `EnemyAIContext` 의존으로 전환 | 비행형 Context 분리 |

## A.2 Phase 2 후속 — 프리팹 연결 (사용자 작업)

자산/데이터 이동·연결은 2026-05-16 완료. 남은 작업은 Unity 에디터에서 프리팹 컴포넌트 추가 한 번이다.

| 항목 | 경로 | 상태 / 처리 |
|------|------|-------------|
| ~~Skeleton Common BT 이동·리네임~~ | `Assets/10.Datas/AI/BehaviorTree/EnemyBehavior_Skeleton_Common.asset` → `.../Generated/BT_EnemyBehavior_Skeleton_Common.asset` | **2026-05-16 해소** — 파일·`.meta` 함께 이동, GUID `683ed5e0908c92d498a1ea33bd9f1ee2` 유지. `Generated.meta` 폴더 메타도 신규 발급. |
| `EnemyBehaviorSO.behaviorTree` 필드 + Skeleton SO 연결 | `Assets/10.Datas/Actor/Enemy/BehaviorData/BehaviorData_skeleton_common.asset` | **2026-05-16 해소** — SO YAML에 BT Asset GUID 참조 기록. |
| Skeleton 프리팹 BehaviorTreeRunner 추가 | `Assets/03.Prefabs/Actor/Monster/MonsterActor_Skeleton_Common.prefab` | **사용자 작업.** Unity 에디터에서 프리팹 열고 `Add Component → Behavior Tree Runner` → `_treeAsset`에 `BT_EnemyBehavior_Skeleton_Common` 드래그. `_startOnEnable=true`/`_tickInterval=0.1`/`_restartWhenComplete=true` 권장. EnemyAIController.Awake가 `_behaviorTreeRunner ??= GetComponent<BehaviorTreeRunner>()`로 자동 캐싱한다. |

> 프리팹 YAML 직접 편집은 의도적으로 회피한다 — 9488줄 프리팹에 컴포넌트 fileID 발급 + `m_Component` 리스트 갱신 + 신규 YAML 도큐먼트 삽입을 Unity 검증 없이 동시에 처리해야 하므로 위험 대비 이득이 없다.

## A.3 검증 상태

| 항목 | 결과 |
|------|------|
| `dotnet build UPlayground.sln --no-restore` (2026-05-16 3차) | 성공. 오류 0개. 기존 외부 패키지/Unity 참조 경고 23개만 존재 |
| Unity 배치모드 컴파일 | 미완료 |
| JSON → BT Asset 실제 생성 | 신규 임포트는 `Generated/BT_*` 규칙 적용됨 (코드 검증). 기존 Skeleton Common BT 자산 이동·리네임 **완료** |
| `EnemyBehaviorSO.behaviorTree` 필드 + Skeleton SO 연결 | 완료 |
| Skeleton 프리팹 BT Runner 추가 | 미진행 (사용자 작업, 부록 A.2 참조) |
| Phase 3 가드 Play Mode 검증 | 미진행 (사용자 작업) |

---

# 부록 B. 참고 출처

- Bobby Anguelov — Behavior Trees: Breaking the Cycle of Misuse: https://takinginitiative.net/wp-content/uploads/2020/01/behaviortrees_breaking-the-cycle-of-misuse.pdf
- Bobby Anguelov — Separation of Concerns Architecture for AI and Animation (Game AI Pro2 Ch.12): http://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter12_Separation_of_Concerns_Architecture_for_AI_and_Animation.pdf
- Bill Merrill — Building Utility Decisions into Your Existing Behavior Tree (Game AI Pro Ch.10): http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter10_Building_Utility_Decisions_into_Your_Existing_Behavior_Tree.pdf
- Champandard & Dunstan — The Behavior Tree Starter Kit (Game AI Pro Ch.6): https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter06_The_Behavior_Tree_Starter_Kit.pdf
- Unreal Engine 4 — Behavior Tree Quick Start (Services & Observer Aborts): https://docs.unrealengine.com/4.27/en-US/InteractiveExperiences/ArtificialIntelligence/BehaviorTrees/BehaviorTreeQuickStart
- Unreal Engine 4 — Behavior Tree Decorators Reference: https://docs.unrealengine.com/4.26/en-US/InteractiveExperiences/ArtificialIntelligence/BehaviorTrees/BehaviorTreeNodeReference/BehaviorTreeNodeReferenceDecorators
- Dave Mark — GDC 2013 "Architecture Tricks: Managing Behaviors in Time, Space, and Depth": https://www.gdcvault.com/play/1018040/Architecture-Tricks-Managing-Behaviors-in
- Intrinsic Algorithm — IAUS (Infinite Axis Utility System): https://www.gameai.com/iaus.php
- Guerrilla Games — HTN Planning in Decima: https://www.guerrilla-games.com/read/htn-planning-in-decima
- Guerrilla Games — The AI of Horizon Zero Dawn: https://www.guerrilla-games.com/read/the-ai-of-horizon-zero-dawn
- The Impact of Dark Souls on Boss Design (Game Developer): https://www.gamedeveloper.com/design/the-impact-of-dark-souls-on-boss-design
- Optimizing AI NPC Behavior in Indie Games (Wayline): https://www.wayline.io/blog/optimizing-ai-npc-behavior-indie-games-unreal-unity
- Unity Behaviour Designer 설명 글: https://wlsdn629.tistory.com/entry/unity-behaviour-designer
- Opsive Decorator 공식 문서: https://opsive.com/support/documentation/behavior-designer-pro/concepts/tasks/decorator/
- Opsive Flow 문서: https://opsive.com/support/documentation/behavior-designer-pro/concepts/flow/

---

# 부록 C. 최소 테스트 그래프

## C.1 Runner 제어 테스트

```
Root Sequence
├── Log("Start")
├── Wait(3.0)
└── Log("Done")
```

검증:
- Wait 중 Pause하면 시간이 진행되어도 완료되지 않는다.
- Resume하면 남은 시간부터 이어진다.
- Restart When Complete가 켜져 있으면 Done 이후 다시 Start가 실행된다.

## C.2 Parallel Abort 테스트

```
Root Parallel(requireAllSuccess = true)
├── Wait(5.0)
└── ReturnFailureAfter(1.0)
```

검증:
- 1초 뒤 Parallel이 Failure가 된다.
- 5초 Wait 노드에 Abort가 호출된다.

## C.3 Conditional Abort 테스트

```
Root Selector(AbortType = LowerPriority)
├── Sequence_Combat
│   ├── HasTarget
│   └── Log("Combat")
└── Sequence_Patrol
    ├── Wait(10.0)
    └── Log("Patrol")
```

검증:
- Patrol Wait 중 `HasTarget`이 true가 되면 Patrol이 Abort되고 Combat가 실행된다.
