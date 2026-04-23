# Behavior Tree 마이그레이션 계획

## 개요

현재 `EnemyBrain` / `EnemyFlyingBrain`의 imperative(명령형) 의사결정 코드를  
**ScriptableObject 기반 Behavior Tree**로 점진적으로 전환한다.

- 기존 **State Machine은 유지** — BT는 "어떤 State로 갈지"만 결정
- 새 몬스터는 **BT 에셋만 교체**해서 행동 패턴 변경 가능
- 런타임 **그래프 에디터**에서 현재 실행 중인 노드 확인 가능

---

## 현재 구조 분석

### 핵심 컴포넌트

| 컴포넌트 | 역할 | 마이그레이션 결과 |
|---|---|---|
| `EnemyBrain` | 0.1s 폴링 → `MakeDecision()` → State 전환 명령 | → `BTRunner`로 교체 |
| `EnemyFlyingBrain` | 지상/공중 루프 + 동일한 imperative 패턴 | → `BTRunner` + Flying 전용 트리 |
| `EnemyDetection` | 타겟 감지 (시야각+거리) | **유지** — Blackboard가 래핑 |
| `EnemyTacticalMemory` | 플레이어 행동 관찰 | **유지** — Blackboard가 래핑 |
| `EnemyBehaviorSO` | 수치 데이터 + `BehaviorPhase[]` | **유지** — Blackboard Params로 참조 |

### 현재 코드의 문제점

1. **확장 어려움** — 새 몬스터마다 `EnemyBrain` 상속 또는 복사 필요. `EnemyFlyingBrain`이 이미 별도 클래스로 분기됨
2. **수정 위험** — `if/else` 체인 수정 시 전체 의사결정 흐름 영향
3. **비직관적** — 우선순위 파악이 코드 독해 없이 불가능
4. **디버깅 불편** — 런타임에 어떤 분기를 탔는지 확인 수단 없음

### `MakeDecision()` 의사결정 흐름 (현재 EnemyBrain)

```
MakeDecision()
├── [차단] Death/Hit/Attack/Airborne → 패스
├── TryNonCombatSkill (힐/버프)
├── HasTarget?
│   ├── YES → HandleCombatBehavior()
│   │   ├── personalSpace 침범 → Retreat
│   │   ├── 연속공격 초과 → Retreat
│   │   ├── TryReactToPlayerState()       ← TacticalMemory 참조
│   │   │   ├── 플레이어 공격 중 → Guard / Flank
│   │   │   ├── 플레이어 가드 중 → Attack / Charge
│   │   │   ├── 플레이어 경직 중 → Attack / Chase
│   │   │   └── 플레이어 회복 중 → Charge / Chase
│   │   ├── TryInterruptCurrentState()    ← Circle/Guard/Retreat 중 갑작스러운 공격
│   │   ├── 쿨다운 중 → hold (Chase+사정거리면 즉시 공격)
│   │   ├── InAttackRange → ExecuteAttack()
│   │   └── HandleDistanceBasedBehavior()
│   │       ├── TooClose → Retreat
│   │       ├── TooFar → Charge / Flank / Chase
│   │       └── 적정거리 → Guard / Circle
│   └── NO → HandleIdleBehavior() → Patrol / Idle
```

---

## BT 시스템 설계

### 설계 원칙

> **BT는 결정 레이어만 담당한다.**  
> State Machine은 그대로 유지하고, BT의 Action 노드가 `controller.TransitionToState()`를 호출한다.

```
[BTRunner]  → 0.1s 폴링, MakeDecision() 대체
    ↓
[BehaviorTreeSO]  → 노드 평가 (EnemyBlackboard 참조)
    ↓
[BTAction 노드]  → controller.TransitionToState(new EnemyChaseState(...))
    ↓
[State Machine]  → 기존 이동/애니/물리 로직 (변경 없음)
```

---

### 노드 타입 계층

```
BTNode (abstract)
├── BTComposite : BTNode          — children 배열 보유
│   ├── BTSelector                — 첫 번째 Success 반환 (OR)
│   ├── BTSequence                — 모두 Success여야 통과 (AND)
│   └── BTRandomSelector          — 가중치 기반 랜덤 선택
├── BTDecorator : BTNode          — child 1개
│   ├── BTInverter                — 결과 반전
│   ├── BTCooldown                — 일정 시간마다만 실행 허용
│   └── BTConditionDecorator      — bool 조건 통과/차단
└── BTLeaf : BTNode
    ├── BTCondition (abstract)    — 상태 체크, Success/Failure 반환
    └── BTAction (abstract)       — State 전환, 항상 Success 반환
```

**NodeStatus**: `Success | Failure | Running`

---

### Blackboard

모든 노드가 공유하는 런타임 컨텍스트 객체.

```csharp
class EnemyBlackboard
{
    // 컴포넌트 참조
    EnemyDetection          Detection
    EnemyTacticalMemory     Memory
    EnemyCombat             Combat
    ActorMovementController Controller

    // 수치 파라미터 (EnemyBehaviorSO + BehaviorPhase 통합)
    EnemyBrainParams        Params

    // 런타임 상태
    BehaviorPhase           CurrentPhase    // 현재 HP 페이즈
    float                   TimeSinceLastAttack
    int                     ConsecutiveDefenseCount

    // 캐시 프로퍼티 (자주 참조하는 값)
    bool    HasTarget           => Detection.HasTarget
    float   DistanceToTarget    => Detection.DistanceToTarget
    string  CurrentStateName    => Controller.CurrentState?.StateName
}
```

---

### ScriptableObject 노드 구조

모든 노드는 `BTNodeSO`를 상속하는 ScriptableObject로 직렬화한다.  
트리 구조 = SO 간의 레퍼런스 참조.

```
BTNodeSO (abstract ScriptableObject)
│
├── 컴포짓
│   ├── BTSelectorSO           children: BTNodeSO[]
│   ├── BTSequenceSO           children: BTNodeSO[]
│   └── BTRandomSelectorSO     children: (BTNodeSO so, float weight)[]
│
├── 데코레이터
│   ├── BTInverterSO           child: BTNodeSO
│   └── BTCooldownSO           child: BTNodeSO, cooldown: float
│
├── 조건 노드
│   ├── BTCond_HasTargetSO
│   ├── BTCond_DistanceSO              operator(< > <=), threshold
│   ├── BTCond_PlayerStateSO           targetState enum (Attacking/Guarding/Staggered/Recovering)
│   ├── BTCond_CurrentStateSO          notInStates: string[]  (블랙리스트)
│   ├── BTCond_CanAttackSO             globalCooldown 체크
│   ├── BTCond_HasAvailableSkillSO     현재 거리에서 쓸 수 있는 스킬 존재 여부
│   ├── BTCond_OverAttackingSO         consecutive attack count >= limit
│   ├── BTCond_RandomChanceSO          probability: float (0~1)
│   ├── BTCond_HPPercentSO             적 자신 HP 체크 (operator, threshold)
│   └── BTCond_ConsecutiveDefenseSO    연속 방어 횟수 >= limit
│
└── 액션 노드
    ├── BTAction_ChaseSO
    ├── BTAction_AttackSO
    ├── BTAction_RetreatSO
    ├── BTAction_GuardSO               duration: float
    ├── BTAction_CircleSO              duration: float (random range)
    ├── BTAction_ChargeSO
    ├── BTAction_FlankSO
    ├── BTAction_PatrolSO
    └── BTAction_IdleSO
```

---

### 기존 EnemyBrain 로직의 BT 매핑

DefaultEnemy용 `BehaviorTreeSO`의 구조 전체.

```
BehaviorTreeSO [Root Selector]
│
├── [Sequence] BlockedStateGuard          ← Death/Hit/Attack... 차단
│   └── BTCond_CurrentState [NOT: Death, Hit, Attack, Counter, Airborne, Grabbed, Land]
│
├── [Sequence] NonCombatSkill             ← TryNonCombatSkill()
│   ├── BTCond_HasHealOrBuffSkill
│   └── BTAction_ExecuteNonCombatSkill
│
├── [Sequence] CombatBehavior             ← HasTarget 분기
│   ├── BTCond_HasTarget
│   └── [Selector] CombatDecision
│       │
│       ├── [Sequence] PersonalSpaceGuard ← personalSpace 침범
│       │   ├── BTCond_Distance [< personalSpace]
│       │   └── BTAction_Retreat
│       │
│       ├── [Sequence] OverAttackGuard    ← 연속공격 초과
│       │   ├── BTCond_OverAttacking
│       │   └── BTAction_Retreat
│       │
│       ├── [Selector] ReactToPlayer      ← TryReactToPlayerState()
│       │   ├── [Sequence] VsPlayerAttacking
│       │   │   ├── BTCond_PlayerState [Attacking]
│       │   │   └── [RandomSelector]
│       │   │       ├── [Sequence] DoGuard      (weight=0.5, HasGuardMotion+InRange)
│       │   │       │   ├── BTCond_HasGuardMotion
│       │   │       │   ├── BTCond_Distance [<= optimalCombat]
│       │   │       │   └── BTAction_Guard
│       │   │       └── [Sequence] DoFlank      (weight=0.4, AllowFlank+MidRange)
│       │   │           ├── BTCond_AllowFlank
│       │   │           ├── BTCond_Distance [> minCombat, <= optimalCombat*1.5]
│       │   │           └── BTAction_Flank
│       │   │
│       │   ├── [Sequence] VsPlayerGuarding
│       │   │   ├── BTCond_PlayerState [Guarding]
│       │   │   └── [Selector]
│       │   │       ├── [Sequence] AttackBreakGuard
│       │   │       │   ├── BTCond_Distance [<= maxRange]
│       │   │       │   ├── BTCond_CanAttack
│       │   │       │   └── BTAction_Attack
│       │   │       └── [Sequence] ChargeBreakGuard
│       │   │           ├── BTCond_AllowCharge
│       │   │           ├── BTCond_Distance [> optimalCombat]
│       │   │           └── BTAction_Charge
│       │   │
│       │   ├── [Sequence] VsPlayerStaggered
│       │   │   ├── BTCond_PlayerState [Staggered]
│       │   │   └── [Selector]
│       │   │       ├── [Sequence] FollowUpAttack
│       │   │       │   ├── BTCond_Distance [<= maxRange*1.3]
│       │   │       │   ├── BTCond_CanAttack
│       │   │       │   └── BTAction_Attack
│       │   │       └── BTAction_Chase
│       │   │
│       │   └── [Sequence] VsPlayerRecovering
│       │       ├── BTCond_PlayerState [Recovering]
│       │       ├── BTCond_Distance [> optimalCombat]
│       │       └── [RandomSelector]
│       │           ├── BTAction_Charge  (weight=0.3, AllowCharge)
│       │           └── BTAction_Chase   (weight=0.7)
│       │
│       ├── [Selector] InterruptIdleState ← TryInterruptCurrentState()
│       │   ├── [Sequence] CircleInterrupt
│       │   │   ├── BTCond_CurrentState [Circle]
│       │   │   ├── BTCond_Distance [<= maxRange*1.3]
│       │   │   ├── BTCond_RandomChance [0.02 + aggressionBonus]
│       │   │   ├── BTCond_HasAvailableSkill
│       │   │   └── BTAction_Attack
│       │   └── [Sequence] GuardInterrupt
│       │       ├── BTCond_CurrentState [Guard]
│       │       ├── BTCond_Distance [<= maxRange]
│       │       ├── BTCond_PlayerState [NOT Attacking]
│       │       ├── BTCond_RandomChance [0.03]
│       │       └── BTAction_Attack
│       │
│       ├── [Sequence] AttackIfReady      ← 메인 공격 판단
│       │   ├── BTCond_CanAttack
│       │   ├── BTCond_Distance [<= attackRange, <= optimalCombat*1.2]
│       │   ├── BTCond_HasAvailableSkill
│       │   └── BTAction_Attack
│       │
│       ├── [Sequence] ChaseIfFar
│       │   ├── BTCond_Distance [> optimalCombat]
│       │   └── BTAction_Chase
│       │
│       └── [Selector] DistanceBehavior   ← HandleDistanceBasedBehavior()
│           ├── [Sequence] TooClose
│           │   ├── BTCond_Distance [< minCombat]
│           │   ├── BTCond_ConsecutiveDefense [< MAX_STREAK]
│           │   └── BTAction_Retreat
│           ├── [Sequence] TooFar
│           │   ├── BTCond_Distance [> optimalCombat]
│           │   └── [RandomSelector]
│           │       ├── BTAction_Charge  (weight=chargeChance, AllowCharge+farEnough)
│           │       ├── BTAction_Flank   (weight=flankChance, AllowFlank)
│           │       └── BTAction_Chase   (weight=기본)
│           └── [RandomSelector] InRangeIdleAction
│               ├── BTAction_Guard   (weight=guardChance)
│               └── BTAction_Circle  (weight=1-guardChance)
│
└── [Selector] IdleBehavior              ← !HasTarget 분기
    ├── [Sequence] DoPatrol
    │   ├── BTCond_EnablePatrol
    │   └── BTAction_Patrol
    └── BTAction_Idle
```

---

### DecidePostAttack BT 매핑

공격 완료 후 `EnemyAttackState`가 `BTRunner.NotifyPostAttack(bool hit)`을 호출하면,  
별도 `PostAttackTreeSO`를 한 번 평가한다.

```
PostAttackTreeSO [Selector]
│
├── [Sequence] AttackHit_Staggered       ← 경직 중 연속타
│   ├── BTCond_AttackHit
│   ├── BTCond_PlayerState [Staggered]
│   ├── BTCond_Distance [<= maxRange*1.2]
│   └── BTAction_Attack
│
├── [Sequence] AttackHit_Continue        ← 연속공격 확률
│   ├── BTCond_AttackHit
│   ├── BTCond_RandomChance [continueAttackChance]
│   ├── BTCond_Distance [<= maxRange*1.2]
│   └── BTAction_Attack
│
├── [Sequence] AttackMiss_DodgingPlayer  ← 회피형 플레이어 대응
│   ├── BTCond_AttackMissed
│   ├── BTCond_PlayerDodgingFrequently
│   └── [RandomSelector]
│       ├── BTAction_Charge  (weight=chargeChance*1.5)
│       └── BTAction_Flank   (weight=flankChance*1.5)
│
└── [RandomSelector] DefaultPostAttack  ← DecidePostAttackWeighted()
    ├── BTAction_Charge   (weight=chargeChance)
    ├── BTAction_Flank    (weight=flankChance)
    ├── BTAction_Guard    (weight=guardChance)
    ├── BTAction_Retreat  (weight=retreatChance, 최근 후퇴 없을 때)
    ├── BTAction_Circle   (weight=0.25)
    └── BTAction_Chase    (weight=0.3)
```

---

### 페이즈 시스템 연동

- `BTRunner`가 HP 변화 이벤트 수신 → `blackboard.CurrentPhase` 갱신
- 페이즈별 확률값(`chargeChance`, `flankChance` 등)은 Blackboard에서 읽음
- 페이즈마다 **다른 BehaviorTreeSO**를 적용하거나, 동일 트리에서 Blackboard 값만 변경하는 방식 **모두 지원**

---

## 파일 구조

```
Assets/02.Scripts/BehaviorTree/
├── Core/
│   ├── BTNode.cs                  — abstract base
│   ├── BTComposite.cs             — Selector, Sequence, RandomSelector
│   ├── BTDecorator.cs             — Inverter, Cooldown
│   ├── BTLeaf.cs                  — Condition, Action 추상 클래스
│   ├── BTRunner.cs                — EnemyBrain 상속, MakeDecision BT로 대체
│   ├── RuntimeBlackboard.cs       — 키-값 런타임 컨텍스트 (EnemyBlackboard 대체)
│   └── BBKey.cs                   — 문자열 키 상수 모음
│
├── Nodes/
│   ├── Conditions/
│   │   ├── BTCond_HasTarget.cs
│   │   ├── BTCond_Distance.cs
│   │   ├── BTCond_PlayerState.cs
│   │   ├── BTCond_CurrentState.cs
│   │   ├── BTCond_CanAttack.cs
│   │   ├── BTCond_ActionReady.cs
│   │   ├── BTCond_HasAvailableSkill.cs
│   │   ├── BTCond_OverAttacking.cs
│   │   ├── BTCond_PersonalSpace.cs
│   │   ├── BTCond_ConsecutiveDefense.cs
│   │   ├── BTCond_RandomChance.cs
│   │   ├── BTCond_HPPercent.cs
│   │   ├── BTCond_BBBool.cs       — 범용: BB bool 키 체크 (key + invert)
│   │   └── BTCond_DistanceBB.cs   — 범용: BB 키 기반 거리 비교 (thresholdKey + multiplier)
│   └── Actions/
│       ├── BTAction_Chase.cs
│       ├── BTAction_Attack.cs
│       ├── BTAction_Retreat.cs
│       ├── BTAction_Guard.cs
│       ├── BTAction_Circle.cs
│       ├── BTAction_Charge.cs
│       ├── BTAction_Flank.cs
│       ├── BTAction_Patrol.cs
│       └── BTAction_Idle.cs
│
├── Data/                          — ScriptableObject SO 클래스
│   ├── BTNodeSO.cs                — abstract base SO
│   ├── BTCompositeSO.cs           — BTSelectorSO, BTSequenceSO, BTRandomSelectorSO
│   ├── BTDecoratorSO.cs           — BTInverterSO, BTCooldownSO
│   ├── BehaviorTreeSO.cs          — rootNode + blackboard 참조
│   ├── BlackboardKeyDefinition.cs — 키 정의 데이터 클래스 + BlackboardKeyType enum
│   └── BTBlackboardSO.cs          — 키 정의 ScriptableObject
│
└── Editor/
    ├── BehaviorTreeEditorWindow.cs — GraphView 기반 에디터 윈도우
    ├── BehaviorTreeGraphView.cs    — GraphView (PopulateView, AutoLayout, RuntimeBind)
    ├── BTNodeView.cs               — 노드 시각화 엘리먼트
    ├── BTBlackboardView.cs         — 편집/런타임 Blackboard 패널
    ├── BTJsonSerializer.cs         — BT 에셋 JSON 내보내기/불러오기
    └── BTDefaultEnemyBuilder.cs    — DefaultEnemy BT 에셋 코드 빌더
                                      (Window > BehaviorTree > Build DefaultEnemy Asset)

Assets/10.Datas/BT/               — 실제 BT 에셋 (저장 경로)
├── BT_DefaultEnemy.asset          ← 미생성 (BTDefaultEnemyBuilder로 생성 예정)
├── BT_DefaultEnemy_PostAttack.asset ← 미생성
└── BT_FlyingEnemy.asset           ← 미생성 (Step 4)
```

---

## 단계별 마이그레이션 계획

### Step 1 — BT 코어 프레임워크 구축 ✅ 완료

**목표:** `EnemyBrain` 변경 없이 새 시스템 병행 개발 가능한 기반 마련

- [x] `EnemyBrain` 7곳 접근자 확장 (`protected`/`virtual` 변경으로 `BTRunner` 상속 허용)
- [x] `BTNode`, `BTComposite`, `BTLeaf`, `BTDecorator` 추상 클래스
- [x] `BTSelector`, `BTSequence`, `BTRandomSelector` 구현
- [x] `EnemyBlackboard` 클래스 (Detection + Memory + Combat + Movement 참조, 행동 리듬 타이밍 포함)
- [x] `BTNodeSO` 계층 (SO 직렬화 구조 — `BTCompositeSO`, `BTDecoratorSO`, `BehaviorTreeSO`)
- [x] `BTRunner : EnemyBrain` (MakeDecision 오버라이드, 페이즈 프로퍼티 공개, TriggerAttack/Retreat/Circle/Chase/Idle 액션 트리거)
- [x] 기본 조건 노드 6종: `HasTarget`, `Distance`, `PlayerState`, `CurrentState`, `CanAttack`, `ActionReady`
- [x] 기본 액션 노드 5종: `Chase`, `Attack`, `Retreat`, `Circle`, `Idle`

**생성 파일 목록 (21개):**
```
BehaviorTree/Core/
  BTNode.cs, BTComposite.cs, BTDecorator.cs, BTLeaf.cs
  EnemyBlackboard.cs, BTRunner.cs
BehaviorTree/Data/
  BTNodeSO.cs, BTCompositeSO.cs, BTDecoratorSO.cs, BehaviorTreeSO.cs
BehaviorTree/Nodes/Conditions/
  BTCond_HasTarget.cs, BTCond_Distance.cs, BTCond_PlayerState.cs
  BTCond_CurrentState.cs, BTCond_CanAttack.cs, BTCond_ActionReady.cs
BehaviorTree/Nodes/Actions/
  BTAction_Chase.cs, BTAction_Attack.cs, BTAction_Retreat.cs
  BTAction_Circle.cs, BTAction_Idle.cs
```

**EnemyBrain.cs 변경 사항:**
- `_lastAttackTime` : `private` → `protected`
- `_currentPhase` : `private` → `protected`
- `_consecutiveDefensiveCount` : `private` → `protected`
- `ExecuteAttack()` : `protected` → `protected virtual`
- `DecidePostAttack()` : `public` → `public virtual`
- `TransitionRetreating()` : `private` → `protected virtual`
- `OnDefensiveAction()` : `private` → `protected virtual`
- `CanUseSkill()` : `protected` → `public`

### Step 2 — 커스텀 에디터 구현 ✅ 완료

**목표:** 트리 구조를 시각적으로 확인하고 런타임 디버그 가능

- [x] `UIElements GraphView` 기반 에디터 윈도우 (`BehaviorTreeEditorWindow`) — `Window > BehaviorTree Editor`
- [x] 노드 박스 렌더링 (`BTNodeView`) — 타입명/자식 수 표시, 컬러코딩 (Composite=파랑, Action=초록, Cond=청록, Decorator=보라)
- [x] 연결선 자동 생성 (`BTGraphView.ConnectEdges`) + 서브트리 너비 기반 자동 레이아웃
- [x] Inspector 패널 — 선택한 노드 SO 필드를 IMGUI로 편집 (플레이 중 읽기 전용)
- [x] 런타임 상태 하이라이트: Running=노란, Success=초록, Failure=빨강 (100ms 폴링)
- [x] Blackboard 패널 — HasTarget/Distance/State/Phase 값 실시간 표시
- [x] 편집 모드 ↔ 런타임 모드 자동 전환 (Hierarchy에서 BTRunner 선택 시 자동 바인딩)

**생성 파일 목록 (5개):**
```
BehaviorTree/Editor/
  BehaviorTreeEditorWindow.cs  — EditorWindow 메인 (툴바+분할레이아웃+플레이모드처리)
  BehaviorTreeGraphView.cs     — GraphView (PopulateView, AutoLayout, RuntimeBind)
  BTNodeView.cs                — Node 서브클래스 (OnSelected/OnUnselected 오버라이드)
  BTBlackboardView.cs          — Blackboard 값 표시 VisualElement
  BehaviorTreeEditor.uss       — USS 스타일시트 (노드/상태/뱃지 색상 정의)
```

**BTNode.cs 변경 사항 (런타임 추적 지원):**
- `Tick()` → 내부적으로 `TickInternal()` 호출 + `LastStatus` 갱신
- `SourceSO` 프로퍼티 추가 — `BTNodeSO.CreateAndBindNode()`가 자동 설정
- `BTNodeSO.CreateAndBindNode()` 래퍼 추가 — 재귀적으로 모든 노드에 SourceSO 바인딩
- `BTRunner.RuntimeTree`, `BTRunner.Blackboard` 프로퍼티 추가 (에디터 접근용)

### Step 3 — 기존 코드 BT 에셋으로 이전 🔄 진행 중

**목표:** DefaultEnemy 하나를 완전히 BT로 전환하며 검증

#### 3-1. 나머지 조건/액션 노드 구현

**조건 노드 (Conditions):**

| 노드 | 상태 | 비고 |
|---|---|---|
| `BTCond_HasTarget` | ✅ | Step 1 완료 |
| `BTCond_Distance` | ✅ | Step 1 완료 (LessThan/GreaterThan/Between, 하드코딩 threshold) |
| `BTCond_PlayerState` | ✅ | Step 1 완료 (Attacking/Guarding/Staggered/Recovering/DodgingFrequently) |
| `BTCond_CurrentState` | ✅ | Step 1 완료 (invert 플래그 포함) |
| `BTCond_CanAttack` | ✅ | Step 1 완료 |
| `BTCond_ActionReady` | ✅ | Step 1 완료 |
| `BTCond_HasAvailableSkill` | ✅ | Step 3 추가 |
| `BTCond_OverAttacking` | ✅ | Step 3 추가 |
| `BTCond_PersonalSpace` | ✅ | Step 3 추가 |
| `BTCond_ConsecutiveDefense` | ✅ | Step 3 추가 |
| `BTCond_RandomChance` | ✅ | Step 3 추가 |
| `BTCond_HPPercent` | ✅ | Step 3 추가 |
| `BTCond_BBBool` | ✅ | **Step 3 추가 — 범용**: BB의 임의 bool 키 체크. `key`+`invert` 설정 |
| `BTCond_DistanceBB` | ✅ | **Step 3 추가 — 범용**: BB 키 기반 거리 비교. `thresholdKey`+`multiplier` 설정 |

**액션 노드 (Actions):**

| 노드 | 상태 | 비고 |
|---|---|---|
| `BTAction_Chase` | ✅ | Step 1 완료 |
| `BTAction_Attack` | ✅ | Step 1 완료 |
| `BTAction_Retreat` | ✅ | Step 1 완료 |
| `BTAction_Circle` | ✅ | Step 1 완료 (min/maxDuration 설정 가능) |
| `BTAction_Idle` | ✅ | Step 1 완료 |
| `BTAction_Guard` | ✅ | Step 3 추가 (min/maxDuration 설정 가능) |
| `BTAction_Charge` | ✅ | Step 3 추가 |
| `BTAction_Flank` | ✅ | Step 3 추가 |
| `BTAction_Patrol` | ✅ | Step 3 추가 |

#### 3-1. Step 3 Blackboard / BTRunner 변경 사항

**`EnemyBlackboard` → `RuntimeBlackboard` 교체 완료:**
- `EnemyBlackboard.cs` 삭제
- `RuntimeBlackboard.cs` + `BBKey.cs` + `BTBlackboardSO.cs` + `BlackboardKeyDefinition.cs` 신규 생성
- 모든 노드가 `EnemyBlackboard` 대신 `RuntimeBlackboard` + BBKey 패턴 사용

**`BTRunner.MakeDecision()` Blackboard 갱신 목록:**
- Perception: `HasTarget`, `DistanceToTarget`, `CurrentStateName`
- Phase: `PhaseAllowCharge`, `PhaseAllowFlank`, `PhaseChargeChance`, `PhaseFlankChance`, `PhaseMaxConsecutiveAttacks`
- Combat Distance: `OptimalCombatDistance`, `MaxAttackRange`, `PersonalSpaceDistance`, `MinCombatDistance`, `RetreatDistance`
- State: `HasGuardMotion`
- Self Stats: `SelfHPPercent` (MonsterActor.CurrentHealth / MaxHealth)

**`BTRunner` Trigger 메서드 전체 목록:**
- `TriggerAttack()`, `TriggerRetreat()`, `TriggerCircle(float)`, `TriggerChase()`, `TriggerIdle()`
- `TriggerCharge()`, `TriggerFlank()`, `TriggerGuard(float)`, `TriggerPatrol()`
- `TriggerConsecutiveDefensiveReset()`
- `OnDefensiveAction()` 오버라이드 → BB `ConsecutiveDefensiveCount` 동기화

**신규 에디터 도구:**
- `BTJsonSerializer.cs` — BT 에셋을 JSON으로 내보내기/불러오기. `ExportTree()` / `ImportTree()` 제공
- `BTDefaultEnemyBuilder.cs` — DefaultEnemy BT 에셋을 코드로 빌드하는 MenuItem 도구  
  (`Window > BehaviorTree > Build DefaultEnemy Asset`)  
  전체 전투 트리 구조(PersonalSpaceGuard, OverAttackGuard, ReactToPlayer, InterruptIdleState, AttackIfReady, ChaseIfFar, DistanceBehavior + IdleBehavior)와 PostAttack 트리를 코드로 생성

#### 3-2. BT 에셋 생성 및 테스트

- [x] `BTCond_RandomChance`, `BTCond_HPPercent` 구현 (나머지 2종)
- [x] `BTDefaultEnemyBuilder.cs` 구현 — `Window > BehaviorTree > Build DefaultEnemy Asset` MenuItem
- [ ] MenuItem 실행하여 `Assets/10.Datas/BT/BT_DefaultEnemy.asset` 생성
- [ ] `Assets/10.Datas/BT/BT_DefaultEnemy_PostAttack.asset` 생성
- [ ] DefaultEnemy 프리팹에서 `EnemyBrain` → `BTRunner` 교체 후 BehaviorTreeSO 연결
- [ ] 플레이 테스트 — 기본 전투 루프 (Chase → Attack → Retreat/Circle) 정상 동작 확인
- [ ] 페이즈 전환 시 행동 변화 검증

### Step 4 — Flying Brain 이전 및 전체 적용

- [ ] Flying 전용 조건/액션 노드 (TakeOff, AirCircle, Dive, Land 등)
- [ ] `BT_FlyingEnemy.asset` 생성
- [ ] `EnemyFlyingBrain` → `BTRunner` 교체
- [ ] 모든 기존 몬스터 프리팹 마이그레이션

---

## 설계 결정 사항 및 트레이드오프

| 결정 | 이유 |
|---|---|
| State Machine 유지 | 이동/물리/애니 로직이 State에 긴밀히 결합되어 있어 재작성 비용이 너무 큼 |
| SO 기반 노드 | Unity 에디터 직렬화 + 인스펙터 편집 + 에셋 재사용 |
| Running 상태 지원 | 공중 루프처럼 여러 프레임에 걸친 행동 표현 가능 (선택적) |
| PostAttack 별도 트리 | 공격 후 분기가 복잡해 메인 트리와 분리하면 가독성 향상 |
| 페이즈별 동일 트리 사용 | 트리 에셋 관리 부담 감소. Blackboard의 CurrentPhase로 가중치 동적 조절 |
