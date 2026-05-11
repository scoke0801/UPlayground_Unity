# Behavior Tree 시스템 분석 및 AAA 레퍼런스 비교

> 작성일: 2026-05-11
> 대상 버전: Unity 6 (6000.0.60f1), URP
> 적용 범위: `Assets/02.Scripts/AI/BehaviorTree/` 커스텀 BT, `Assets/02.Scripts/GameActor/Component/Enemy/EnemyBrain.cs` 외 적 AI 통합 지점
> 관련 문서:
> - `BEHAVIOR_TREE_IMPROVEMENT_PLAN_GUIDE.md` (2026-04-28) — BT 신규 구현 설계 제안 (구조적 1차 계획)
> - `BEHAVIOR_TREE_REFERENCE_GAP_IMPLEMENTATION_GUIDE.md` (2026-04-28) — BT 누락 기능 보강 계획 (실행 제어/디버깅 보강)
>
> 본 문서는 위 두 문서 이후 BT 인프라가 일정 부분 완성된 시점에서 **AAA 게임 AI 사례와 비교한 현재 갭** 및 **EnemyBrain과의 통합 방향**을 정리한다. 통합 방향 결정은 보류 상태로, 분석/제안만 담는다.

---

## 1. 핵심 진단 — 두 시스템이 갈라져 있다

가장 중요한 사실: BT 인프라(`Assets/02.Scripts/AI/BehaviorTree/`, 약 30개 파일, GraphView/Inspector/Blackboard/Validator 포함)는 거의 완성되어 있지만, **실제 적 AI는 BT를 전혀 사용하지 않는다**.

근거:
- `BehaviorTreeRunner` / `BehaviorTreeAsset` 참조는 BT 폴더 내부 11개 파일에 한정 — 게임플레이 코드에서 사용처 0
- `EnemyBrain.cs` (811줄)은 하드코딩된 if/else + `Random.value` 기반 의사결정
- `EnemyFlyingBrain`, `MonsterGroupController`, 13+ Enemy 상태도 모두 EnemyBrain 경로
- BT용 enemy 노드 3종(`TransitionEnemyStateNode`, `SyncEnemyBlackboardNode`, `ExecuteEnemyAttackNode`)은 준비됐지만 실제 BT 에셋 사용처가 `Assets/Test/BehaviorTree.asset` 한 개뿐
- `BehaviorTreeRunner._tickInterval=0.1f`와 `EnemyBrain._decisionInterval=0.1f`가 별개로 폴링하는 이중 구조

즉 BT는 "사두고 안 쓴 인프라"이고 EnemyBrain은 "실제로 돌아가는 코드". 통합 결정 전에도 BT 자체의 구조적 갭은 짚을 가치가 있다.

---

## 2. 현재 BT의 구체적 갭 (코드 근거)

| 항목 | 현 상태 | 표준 / AAA 사례 | 영향 |
|---|---|---|---|
| **Service 노드** | 없음. `SyncEnemyBlackboardNode`를 매 Tick Action으로 굴리는 우회 | UE4 Service: Composite에 첨부, 백그라운드 주기 갱신 | Blackboard 갱신이 액션 노드 자리를 차지, 조건 분기 가독성 저하 |
| **Observer Decorator** | 없음. AbortType은 `BTCompositeNode.EnumerateConditions`로 매 Tick 트리 재귀 폴링 | Bobby Anguelov **Monitor Decorator**: 조건이 등록부에 들어가 매 Tick 1회 평가, 변화 시 abort 트리거 | 트리 깊을수록 O(n) 폴링. 적 수 늘면 누적 비용 |
| **Blackboard 키 타입 안전성** | `string` 키 + 6개 기본 타입(Bool/Int/Float/String/Vector3/Object) | UE: BlackboardKeySelector(에디터 드롭다운). Enum/Generic Key Asset 패턴 | 오타·리네임 무방비, `AnimKey`/`EnemyTransitionStateType` 등 프로젝트 enum과 안 맞음 |
| **Weighted Random Selector** | 없음. Selector는 항상 위→아래 정적 우선순위 | "BT 정적 우선순위가 가장 큰 단점" — 가중 선택으로 보강이 표준 (GameAIPro Ch.10) | CLAUDE.md에 명시된 "공격적 접근 + 불확실한 전환" 설계 의도를 BT로 표현 불가. EnemyBrain의 `Random.value` 패턴을 BT로 옮길 수 없음 |
| **Subtree / Sub-BT 참조** | 없음. 에셋끼리 참조/포함 불가 | UE: Run Behavior Tree | 보스 페이즈/그룹 패턴 재사용 불가, 트리가 모놀리식 비대화 (Anguelov 경고 그대로) |
| **Tick LOD / 예산** | Runner당 `_tickInterval=0.1f`. 거리·중요도 무관 | 거리 기반 동적 틱 간격, frustum 외 stall | 화면 밖 적도 동일 비용. 다수 적 시 부하 |
| **이중 폴링** | `BehaviorTreeRunner.tickInterval=0.1` + `EnemyBrain._decisionInterval=0.1` | — | 통합 시 한쪽으로 일원화 필요 |
| **페이즈 시스템 표현** | `EnemyBehaviorSO.phases`가 노드 파라미터를 못 바꿈. HP 임계값 행동 전환을 BT로 옮기려면 페이즈별 거대한 Selector 분기 필요 | HZD: HTN의 매크로 단위 / FromSoftware: 페이즈 = FSM 전이 + 공격풀 교체 | BT migrate 시 데이터 모델 재설계 필수 |
| **공격 풀 / 카드 시스템** | 없음. `EnemyCombat`의 스킬 리스트 + 거리 필터만 | Soulslike 보스: 5+ 공격, 페이즈마다 풀 교체, 랜덤 + 쿨다운으로 패턴 회피 | 액션게임 적 행동의 핵심 모델이 BT에 없음 |
| **런타임 디버깅 시각화** | `BehaviorTreeDebugTrace` 큐 데이터는 쌓이고 있음. GraphView 노드 색 하이라이트는 미확인 | UE: 실시간 노드 하이라이트, 블랙보드 watcher | authoring 도구 가치 절반 |

---

## 3. AAA 핀포인트 — 우리 프로젝트에 직접 적용 가능한 것

일반론은 피하고, 1인 개발 + 소울라이크 액션 컨텍스트에 바로 적용 가능한 것만 정리한다.

### 3.1 Bobby Anguelov — "Breaking the Cycle of Misuse"

- BT는 "복잡한 reactive 로직"에 약함 — 정적 우선순위 + 매 프레임 트리 traversal 문제
- 해법 **Monitor Decorator**: condition을 별도 register에 모아 1회 평가, 변화 시만 abort
- 또 다른 핵심: **"BT는 만능 아니다, 다른 시스템과 조합"** — Separation of Concerns (Game AI Pro2 Ch.12)
- **우리에게 의미:** AbortType 폴링 구조를 Monitor로 교체하면 트리 깊이 무관 O(monitored conditions)로 비용 고정

### 3.2 Unreal Engine 4 Behavior Tree (사실상 본 BT가 모방한 표준)

- **Services**: Composite에 첨부, 주기적 Blackboard 업데이트 ("Detect Target" 같은 폴링이 여기에 들어감)
- **Decorators with Observer Aborts**: Self / Lower Priority / Both — 우리 코드의 `BTAbortType`가 그대로 가져왔으나 **이벤트 기반이 아닌 폴링**
- **우리에게 의미:** 노드 분류에 **Service** 카테고리를 도입해 `SyncEnemyBlackboardNode`를 적절한 자리로 옮기는 게 즉시 개선

### 3.3 Dave Mark — Infinite Axis Utility (GDC 2013)

- 모든 행동에 0~1 score → 가장 높은 것 선택 (또는 weighted random in top-N)
- EnemyBrain의 `if (Random.value < ContinueAttackChance) ...` 같은 **체이닝 if-random**의 정공법 대체
- **우리에게 의미:** BT 안에 **UtilitySelectorNode** (자식들의 score 가져와서 weighted random) 추가만 해도 EnemyBrain의 의도를 BT로 표현 가능해진다

### 3.4 Horizon Zero Dawn — Decima HTN

- 매크로(여러 액션 묶음) 단위 계획. "공격하기"가 단일 액션이 아닌 "접근 → 위치잡기 → 페이크 → 공격" 매크로
- 머신 그룹 행동(역할 분배)이 우리 `MonsterGroupController`와 컨셉적으로 닮음
- **우리에게 의미:** BT만으로는 그룹 의도 표현 한계가 분명하다는 강한 증거. 다만 1인 개발 규모에 HTN 도입은 과투자. 그룹 의도 표현은 BT 외부에 별도 컨트롤러를 두는 현재 구조 유지가 합리

### 3.5 Soulslike 보스 AI 패턴 (FromSoftware)

- 보스당 5+ 공격, 페이즈마다 공격 풀 변경, 랜덤 + 쿨다운 + 거리 게이트로 패턴 회피
- 가장 가까운 모델 — 우리 `EnemyBehaviorSO.phases` + `EnemyCombat.skills`가 같은 구조 지향
- **우리에게 의미:** **AttackPoolNode** (현재 페이즈의 가용 스킬 집합 → weighted random + cooldown + range filter)가 단일 노드로 추가될 가치 있음

---

## 4. 개선 방안 — 통합 방향과 무관하게 가치 있는 작업

다음은 어떤 통합 방향(전면 migrate / 보스 한정 augment / BT 폐기)을 택해도 가치 있는 항목들이라 **선행 가능**하다.

### 4.1 즉시 가치 (작업 비용 작음, 일관성·안전성 개선) — **2026-05-11 구현 완료**

> 본 절 4개 항목은 본 문서 작성 직후 구현되었다. 구현 세부는 §9 참고. 통합 방향(A/B/C) 결정은 여전히 보류.

1. **BlackboardKey 타입 안전화** — Unity 직렬화 한계로 제네릭 `BlackboardKey<T>` 대신 `BlackboardKeySelector` struct + PropertyDrawer 채택. 내부적으로 string 키를 보관하므로 기존 API 100% 호환, 인스펙터에서는 expectedType과 일치하는 키만 드롭다운 표시. 미스매치는 적색 + "(missing)" 표시
2. **Service 노드 카테고리 도입** — `BTServiceNode` 추가. Composite의 `BeginServices`/`TickServices`/`EndServices` 생명주기에 통합. 첫 번째 적용 사례로 `SyncEnemyBlackboardService` 추가 (기존 Action 버전 `SyncEnemyBlackboardNode`는 호환성 유지)
3. **WeightedRandomSelector** — 자식별 weight 기반 1회 픽 → 실패 시 남은 자식에서 재픽. **매 Tick 재롤하지 않음**으로 Running 자식의 안정성 보장. 가중치 누락분은 1.0으로 패딩, 모든 가중치 0이면 균등 랜덤
4. **Subtree 노드** — `BehaviorTreeAsset` 참조 1개를 가진 액션 노드. 부모 트리의 Blackboard를 **공유** (`shareBlackboardOverride: true`로 `CloneRuntime` 호출). Validator에서 순환 참조 검증

### 4.2 중기 (구조적 개선)

5. **Monitor/Observer Decorator** — Anguelov 방식. 등록제로 condition 평가 1회/Tick. 현재 `BTCompositeNode.EnumerateConditions` 재귀 제거
6. **Runner Tick LOD** — `BehaviorTreeRunner`에 `[cameraDistance, tickInterval]` 커브 추가. 멀면 0.5s, 가까우면 매 프레임
7. **이중 폴링 통합** — Runner의 tick과 EnemyBrain의 decision을 한쪽에 일원화 (어느 쪽이든 정책 결정 필요)
8. **DebugTrace의 GraphView 시각화** — 이미 데이터는 쌓이고 있음. 런타임에 Last status를 노드 색으로 노출하면 authoring 가치 급상승

### 4.3 통합 방향 결정 후에 (정책 필요)

9. **페이즈 표현 모델** — BT에서 페이즈를 어떻게 표현할지:
   - (a) 페이즈별 root selector 분기
   - (b) Blackboard.PhaseId + 가드 데코레이터
   - (c) `EnemyBehaviorSO.phases`를 노드 외부 파라미터로 유지하고 BT는 phase-agnostic
10. **AttackPoolNode** — `EnemyCombat.AttackData` 기반 페이즈 인지 weighted attack 선택기
11. **EnemyBrain 분해 매트릭스** — 811줄을 영역별(지각/거리관리/행동선택/리듬/메모리)로 쪼개서 어느 부분이 BT로 이관, 어느 부분이 컴포넌트로 잔존인지 합의

---

## 5. 통합 방향 비교 (결정 보류)

| 옵션 | 적합 시나리오 | 주요 위험 |
|---|---|---|
| **A. 전면 migrate** | 적·보스 다양성을 늘리고 디자이너(본인)가 데이터로 빠르게 튜닝하고 싶을 때 | 페이즈/리듬/그룹/메모리를 모두 노드로 풀어야 함 → 노드 30~50개 추가, 트리 비대화 위험 (Anguelov 경고) |
| **B. 보스/특수 적만 BT** | 잡몹은 단순하고 보스만 복잡한 패턴 — 전형적 액션게임 구조 | 두 시스템 코드 일관성 비용. 디버깅 도구 둘 |
| **C. BT 폐기 + EnemyBrain 강화** | BT 인프라 유지 비용 > 이득이라 판단 시 | BT 폴더 30+ 파일 삭제 결정. 향후 데이터 드리븐 요구 시 처음부터 다시 |

### 객관적 권고

옵션 B(보스/특수 한정 augment)가 위험도 대비 이득이 가장 균형 잡힘. 이유:

- 잡몹은 `EnemyBrain`의 페이즈/메모리/그룹 코드가 이미 동작 중 → 무리하게 BT로 옮기면 회귀 위험
- 보스/엘리트는 패턴 복잡도가 임계점 넘으면 데이터 authoring이 필요해짐 → BT가 이때 빛남
- 4.1·4.2 항목은 옵션 B에서도 모두 의미 있음

---

## 6. 즉시 실행 권장 순서 (통합 방향 무관)

1. **BlackboardKey 타입 안전화 + Service 노드 도입** → 기존 BT 즉시 더 쓸만해짐
2. **WeightedRandomSelector + Subtree** → 보스 BT 작성 진입로 확보
3. **Monitor Decorator + Tick LOD** → 다수 적 시나리오 대비
4. **시범 보스 1마리를 BT로 작성** → 데이터로 검증 후 옵션 B vs A 본격 판단

---

## 7. 안 해도 되는 것 / 지양할 것

- **GOAP / HTN 도입** — 1인 개발 규모에 과투자. Guerrilla급 인력 기준 도구
- **BT 안에서 페이즈 데이터까지 노드 분기로 풀기** — 디자인 의도 손상. 페이즈는 외부 SO 유지하고 BT는 phase-aware 노드 몇 개로 처리
- **현재 `EnumerateConditions` 폴링을 그대로 두고 노드 수만 늘리기** — 누적 비용 폭증

---

## 8. 결론

BT 인프라는 약 70% 완성이지만 핵심 5개(Service, Monitor, WeightedSelector, Subtree, BlackboardKey 타입화)가 빠져 있어서 EnemyBrain을 옮길 수 없는 상태다. 통합 방향 결정과 별개로 4.1·4.2 항목은 BT를 쓰기로 결정했다면 어떤 형태든 필요한 작업이다.

---

## 9. 4.1 구현 결과 (2026-05-11)

### 9.1 추가/변경된 파일

| 분류 | 파일 | 설명 |
|---|---|---|
| 신규 (런타임) | `Assets/02.Scripts/AI/BehaviorTree/Runtime/BTServiceNode.cs` | Service 노드 베이스. `Interval`/`TickOnEnter` 직렬화 필드, `OnServiceEnter/Tick/Exit` 훅 |
| 신규 (런타임) | `Assets/02.Scripts/AI/BehaviorTree/Runtime/Blackboard/BlackboardKeySelector.cs` | 타입 필터링된 Blackboard 키 selector struct |
| 신규 (런타임) | `Assets/02.Scripts/AI/BehaviorTree/Nodes/Composite/WeightedRandomSelectorNode.cs` | 가중치 기반 1회 픽 + 실패 시 풀 소진 재픽 Composite |
| 신규 (런타임) | `Assets/02.Scripts/AI/BehaviorTree/Nodes/Action/SubtreeNode.cs` | 다른 `BehaviorTreeAsset`을 실행, 부모 Blackboard 공유 |
| 신규 (런타임) | `Assets/02.Scripts/AI/BehaviorTree/Nodes/Service/SyncEnemyBlackboardService.cs` | 기존 Sync Action의 Service 버전 |
| 신규 (에디터) | `Assets/02.Scripts/AI/BehaviorTree/Editor/BlackboardKeySelectorDrawer.cs` | BlackboardKeySelector의 인스펙터 드롭다운 PropertyDrawer |
| 수정 (런타임) | `BTNode.cs` | Initialize/Tick/Abort/ResetNode에 Composite Service 생명주기 통합 |
| 수정 (런타임) | `BTCompositeNode.cs` | `_services` 직렬화 필드 + `BeginServices/TickServices/EndServices` |
| 수정 (런타임) | `Blackboard.cs` | `BlackboardKeySelector` 오버로드 12종 추가 (Set/TryGet) |
| 수정 (런타임) | `BehaviorTreeAsset.cs` | `CloneRuntime`에 `shareBlackboardOverride` 파라미터 추가, services 클론 |
| 수정 (에디터) | `BehaviorTreeAssetValidator.cs` | Service 부착 검증, Subtree 순환 검증, WeightedRandom 가중치 검증, services를 referenced에 포함 |
| 수정 (에디터) | `BehaviorTreeGraphView.cs` | Service 노드는 그래프 메뉴에서 제외 (Composite Inspector를 통해 부착), 카테고리에 Service 추가 |
| 수정 (에디터) | `BehaviorTreeInspectorView.cs` | Composite 선택 시 "+ Add Service" 드롭다운 버튼, Service 카테고리 표시 |

### 9.2 핵심 설계 결정

- **BlackboardKey 제네릭 포기 이유**: Unity는 `[SerializeField] BlackboardKey<bool>` 같은 제네릭 필드를 직렬화하지 않는다. 인스펙터 표시도 안 되고 에셋 저장도 안 됨. `BlackboardKeySelector` (비제네릭 struct + `expectedType` 필드 + PropertyDrawer)가 핵심 이득의 90%(에디터 오타 방지, 타입 미스매치 적색 표시, Validator 키 부재 경고)를 가져오면서 string 기반 기존 API와 바이너리 호환을 유지한다.
- **Service 노드는 그래프 노드가 아니다**: UE 스타일로 Composite NodeView 본체 내부에 stacked 표시하는 것은 작업량이 4.1 범위를 초과한다. 현재는 Inspector 안에서 Add 드롭다운 + SerializeField 리스트 형태로 노출한다.
- **WeightedRandomSelector는 매 Tick 재롤하지 않는다**: `OnStart`에 1회 픽 → Running 자식이 안정적으로 끝까지 실행. Failure 시에만 남은 풀에서 재픽. 매 Tick 재롤은 행동 불안정 유발이라 의도적으로 배제.
- **Subtree는 Blackboard를 공유한다**: 별도 격리 모드는 추후 작업. 부모 트리와 동일 인스턴스를 참조 (`CloneRuntime(parentBB, shareBlackboardOverride: true)`).
- **Subtree 순환 검증**: Validator가 `HasSubtreeCycle`로 A → B → A 형태를 검출.
- **기존 string 기반 노드 마이그레이션 보류**: `SetBlackboardValueNode`, `BlackboardBoolConditionNode` 등 6개 노드는 string 키 그대로 유지. 새로 작성하는 노드부터 `BlackboardKeySelector` 사용 권장. 일괄 마이그레이션은 후속 작업.

### 9.3 4.1 범위에서 의도적으로 미룬 항목

- **JSON Import/Export의 Services/Subtree 직렬화** — `BehaviorTreeNodeJson.children`은 GUID 기반이지만 `services` 필드 없음, Subtree의 `_subtreeAsset`은 SupportedPropertyTypes 미포함. 현재는 .asset 직렬화로만 보존됨. JSON 왕복이 필요해지는 시점에 보강
- **NodeView 본체에 service 카운트/요약 표시** — 그래프에서 어떤 Composite에 Service가 붙어있는지 한눈에 보이게 하는 시각 보조. body list policy의 시각화 마무리. 사용 빈도 보고 추가 여부 결정
- **기존 6개 노드의 BlackboardKeySelector 마이그레이션** — 후속 일괄 작업
- **Service 노드를 그래프 위에 떠다니는 stacked 노드로 표현** — UE 스타일 그래프 authoring 경험. 작업량 크고 4.1 범위 밖
- **DebugTrace에 Service tick 미반영** — `BTServiceNode.OnUpdate`는 호출되지 않고 `OnServiceTick`은 trace에 안 적힘. GraphView 디버그 하이라이트도 Service에는 안 들어옴. 보스 튜닝 시 보조 필요해지면 4.2에서 보강
- **Subtree 클론 인스턴스의 Unity Object 누수** — `BehaviorTreeRunner.RestartTree`가 부모 클론을 새로 만들 때 이전 SubtreeNode의 `_runtimeSubtree`는 `DestroyImmediate` 호출 없이 사라짐. 기존 `CloneRuntime` 패턴 그대로지만 Subtree 도입으로 N배 누적됨. 4.2 후속

### 9.4 사용 가이드

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
인스펙터에서 `Target Key (Object)` 드롭다운으로 Blackboard에 등록된 Object 타입 키만 표시됨.

### 9.5 다음 권장 단계 (본 문서 §6 순서 기준)

1. **시범 보스 1마리를 BT로 작성** — 4.1로 갖춰진 인프라(Subtree로 페이즈 분리, WeightedRandomSelector로 공격 풀, Service로 Blackboard 폴링)를 실제 보스 1종에 적용해 검증
2. 검증 결과를 가지고 §5의 옵션 A vs B 본격 판단
3. 4.2(Monitor Decorator, Tick LOD), 4.3(페이즈 모델, AttackPoolNode)은 옵션 결정 후 진행

---

## 부록 A — 참고 출처

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
