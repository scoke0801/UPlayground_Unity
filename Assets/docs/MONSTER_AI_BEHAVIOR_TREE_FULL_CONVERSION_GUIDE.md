# 몬스터 AI Behavior Tree 완전 전환 가이드

> 작성일: 2026-05-11
> 대상 버전: Unity 6 (6000.0.60f1), URP
> 적용 범위: `EnemyBrain`, `EnemyFlyingBrain` 중심의 몬스터 의사결정을 커스텀 Behavior Tree(BT) 기반으로 완전히 전환하는 실행 계획

---

## 개요

이 문서는 현재 몬스터 AI를 C# Brain 기반 의사결정에서 커스텀 Behavior Tree 기반 의사결정으로 완전히 전환하기 위한 설계와 작업 순서를 정리한다.

핵심 목표는 다음과 같다.

- `EnemyBrain.MakeDecision()` / `EnemyFlyingBrain.MakeDecision()`에 집중된 행동 선택 로직을 BT Asset과 BT Node로 이전한다.
- 기존 몬스터 동작은 JSON 데이터로 기술하고, 이 JSON을 BT Asset으로 임포트할 수 있어야 한다.
- 기존 `EnemyActorState`, `EnemyFlying*State`, KCC 기반 이동 제어는 유지한다.
- BT는 "어떤 행동을 할지"만 결정하고, 실제 이동/공격/피격/사망 처리는 기존 State Machine이 계속 담당한다.
- `EnemyBehaviorSO`, `BehaviorPhase`, `EnemyFlyingSettingsSO`는 BT 전환 이후에도 몬스터별 튜닝 데이터로 유지한다.
- 최종적으로 `EnemyBrain` / `EnemyFlyingBrain`은 런타임 의사결정자가 아니라 BT Action/Condition에서 참조하는 Adapter 또는 Context Provider로 축소한다.

---

## 현재 구조

현재 지상 몬스터 AI는 `EnemyBrain`이 주기적으로 판단하여 `ActorMovementController.TransitionToState()`를 직접 호출한다.

```
MonsterActor
├── EnemyBrain
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

비행 몬스터는 `EnemyFlyingBrain`이 지상 전투 루프와 공중 루프를 모두 제어한다.

```
EnemyFlyingBrain
├── MakeDecision()
├── EvaluateChase()
├── OnGroundAttackFinished()
├── OnAirAttackFinished()
├── TransitionToTakeOff()
└── TransitionToDescend()
```

### 이미 구현된 BT 기반

현재 프로젝트에는 다음 BT 런타임/에디터 기반이 존재한다.

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
│   ├── Action/
│   │   ├── ExecuteEnemyAttackNode.cs
│   │   ├── TransitionEnemyStateNode.cs
│   │   └── SyncEnemyBlackboardNode.cs
│   ├── Condition/
│   │   ├── HasTargetNode.cs
│   │   ├── IsTargetInRangeNode.cs
│   │   ├── CanUseEnemySkillNode.cs
│   │   └── IsCurrentActorStateNode.cs
│   ├── Composite/
│   ├── Decorator/
│   └── Service/
│       └── SyncEnemyBlackboardService.cs
└── Editor/
```

따라서 목표는 BT 시스템 신규 제작이 아니라, 기존 몬스터 AI의 의사결정 권한을 `BehaviorTreeRunner`로 이전하는 것이다.

### 현재 JSON 유틸의 한계

`BehaviorTreeJsonUtility`는 이미 존재하지만, 현재 역할은 `BehaviorTreeAsset`을 JSON으로 Export/Import하는 것이다.

| 기능 | 현재 지원 여부 | 설명 |
|------|----------------|------|
| BT Asset -> JSON | 지원 | 노드 타입, GUID, 위치, 자식 연결, Blackboard, 노드 필드 저장 |
| JSON -> BT Asset | 지원 | `BehaviorTreeJsonData`를 읽어 `BehaviorTreeAsset` 생성 |
| 기존 `EnemyBrain` 행동 -> JSON | 미지원 | C# 조건문/확률/상태 전환 로직을 데이터화하는 변환 규칙이 없음 |
| 몬스터 행동 정의 JSON -> BT Asset | 미지원 | 사람이 작성하기 쉬운 행동 JSON을 BT 노드 그래프로 변환하는 Builder가 없음 |

따라서 완전 전환에는 `BehaviorTreeJsonUtility`를 직접 확장하기보다, 사람이 작성하기 쉬운 몬스터 행동 JSON을 BT 내부 JSON 또는 `BehaviorTreeAsset`으로 변환하는 별도 Importer가 필요하다.

---

## 목표 아키텍처

최종 구조는 다음과 같다.

```
MonsterActor
├── EnemyAIContext                 # 신규 또는 EnemyBrain 축소 버전
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

---

## 전환 원칙

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

이를 위해 `IsBlockedEnemyStateNode` 또는 `EnemyActionGuard` 계층을 추가한다.

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

---

## 신규/확장 클래스

### `EnemyAIContext`

`EnemyBrain`을 즉시 제거하지 않고, 먼저 `EnemyAIContext` 역할로 축소한다. 이름을 유지할 수도 있지만 최종 구조에서는 Brain이라는 이름보다 Context가 명확하다.

```csharp
namespace UPlayGround.Component
{
    public class EnemyAIContext : MonoBehaviour
    {
        public EnemyBehaviorSO BehaviorData { get; }
        public BehaviorPhase CurrentPhase { get; }
        public Vector3 SpawnPosition { get; }

        public float OptimalCombatDistance { get; }
        public float MinCombatDistance { get; }
        public float PersonalSpaceDistance { get; }
        public float RetreatDistance { get; }
        public float CircleDuration { get; }
        public float GuardDuration { get; }
        public bool EnablePatrol { get; }
        public bool HasGuardMotion { get; }

        public void UpdatePhase(float hpPercent);
        public bool CanUseSkill();
        public bool TryRequestAttackSlot();
        public void ReleaseGroupSlot();
        public Vector3 GetRandomPatrolPoint();
        public void NotifyAttackStarted();
        public void NotifyDefensiveAction();
    }
}
```

### `EnemyBlackboardKeys`

문자열 키 분산을 막기 위한 상수 클래스가 필요하다.

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
        public const string IsPlayerAttacking = "IsPlayerAttacking";
        public const string IsPlayerGuarding = "IsPlayerGuarding";
        public const string IsPlayerStaggered = "IsPlayerStaggered";
        public const string IsPlayerRecovering = "IsPlayerRecovering";
        public const string IsPlayerDodgingFrequently = "IsPlayerDodgingFrequently";
        public const string CanUseSkill = "CanUseSkill";
        public const string HasAttackSlot = "HasAttackSlot";
    }
}
```

### 필수 신규 노드

| 노드 | 타입 | 역할 |
|------|------|------|
| `IsBlockedEnemyStateNode` | Condition | BT가 개입하면 안 되는 현재 State 확인 |
| `IsEnemyTargetTooCloseNode` | Condition | `PersonalSpaceDistance`, `MinCombatDistance` 기준 후퇴 조건 |
| `CanUseEnemySkillNode` 확장 | Condition | 글로벌 쿨다운, 거리, 스킬 존재 여부 확인 |
| `RequestEnemyAttackSlotNode` | Action | `MonsterGroupController.RequestAttackSlot` 요청 |
| `ReleaseEnemyAttackSlotNode` | Action | 공격 종료 또는 Abort 시 슬롯 반환 |
| `TransitionEnemyStateNode` 확장 | Action | `Circle`, `Guard`, `Charge`, `Flank`, 비행 상태 전환 지원 |
| `SelectEnemySkillNode` | Action | 거리/타입/페이즈 기반 스킬 선택 |
| `SyncEnemyMemoryService` | Service | `EnemyTacticalMemory` 상태를 Blackboard에 반영 |
| `SyncEnemyPhaseService` | Service | HP 기반 페이즈 갱신 및 Blackboard 반영 |
| `SetEnemyActionDelayNode` | Action | 기존 `_nextActionDelay` 역할 이전 |
| `HasEnemyActionDelayElapsedNode` | Condition | 공격 후 의도적 대기 시간 판정 |

---

## 지상 몬스터 BT 설계

기본 지상형 BT는 다음 형태를 기준으로 한다.

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

### 공격 후 행동

기존 `EnemyBrain.DecidePostAttack(bool attackHit)`는 다음 중 하나로 이전한다.

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

---

## 비행 몬스터 BT 설계

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

### GroundCombat Subtree

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

### AirCombat Subtree

```
AirCombat
├── Sequence: 공중 공격 횟수 소진
│   └── Select Dive or Land
├── Sequence: 공중 공격 가능
│   └── Execute Aerial Skill
└── Transition Flying_AirCircle
```

### 비행형 전환 주의점

비행 상태는 `OnAirAttackFinished`, `OnDiveLanded`, `ResetAllCounters` 같은 콜백 기반 흐름이 있다. 이 로직은 한 번에 BT로 옮기지 않고 다음 순서로 이전한다.

1. `EnemyFlyingBrain`의 카운터와 튜닝값을 `EnemyFlyingAIContext`로 분리한다.
2. 지상 추격/공격/후퇴 판단만 BT로 이전한다.
3. `TakeOff`, `AirCircle`, `Dive`, `Land` 상태 콜백은 유지한다.
4. 공중 공격 횟수, 착지/급강하 선택을 BT Condition/Action으로 이전한다.
5. `EnemyFlyingBrain.MakeDecision()`을 제거한다.

---

## 데이터 전환

### 몬스터 행동 JSON 임포트

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

최종 변환 흐름은 다음과 같다.

```
Monster Behavior Json
    └── MonsterBehaviorTreeJsonImporter
        ├── JSON 검증
        ├── 기본 Blackboard 생성
        ├── Service/Condition/Action 노드 생성
        ├── Composite/Decorator 연결
        └── BehaviorTreeAsset 저장
```

기존 `BehaviorTreeJsonUtility`는 BT 에셋의 저수준 직렬화 포맷으로 유지한다. 몬스터 행동 JSON 임포터는 내부적으로 다음 둘 중 하나를 선택한다.

| 방식 | 설명 | 권장 |
|------|------|------|
| 직접 생성 | `ScriptableObject.CreateInstance<BehaviorTreeAsset>()`와 노드 생성 API로 바로 `.asset` 생성 | 1차 권장 |
| BT Json 경유 | 몬스터 행동 JSON을 `BehaviorTreeJsonData`로 변환한 뒤 `BehaviorTreeJsonUtility.ImportFromData()` 호출 | 디버깅/툴 재사용에 유리 |

1차 구현은 BT Json 경유가 안전하다. 현재 `BehaviorTreeJsonUtility.ImportFromData()`가 이미 노드 생성, Blackboard 생성, Asset 저장을 처리하기 때문이다.

### 몬스터 행동 JSON 스키마

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

### 스키마 필드

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

### 기존 Brain 로직과 JSON 매핑

| 기존 `EnemyBrain` 로직 | JSON 표현 | 생성될 BT 노드 |
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

### JSON 임포터 클래스

신규 에디터 유틸은 다음 구조로 둔다.

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

생성 경로는 다음 규칙을 사용한다.

| 입력 JSON | 출력 BT Asset |
|-----------|---------------|
| `Assets/10.Datas/AI/BehaviorTree/SourceJson/Ground/EnemyBehavior_Skeleton_Common.json` | `Assets/10.Datas/AI/BehaviorTree/Generated/BT_EnemyBehavior_Skeleton_Common.asset` |
| `Assets/10.Datas/AI/BehaviorTree/SourceJson/Flying/EnemyBehavior_Griffin.json` | `Assets/10.Datas/AI/BehaviorTree/Generated/BT_EnemyBehavior_Griffin.asset` |

### JSON Export 도구

기존 `EnemyBehaviorSO` 값을 바탕으로 초안 JSON을 생성하는 도구도 필요하다.

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

### JSON 검증 규칙

임포트 전 다음 검증을 수행한다.

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

검증 실패 메시지는 `BehaviorTreeAssetValidator`와 같은 방향으로 에디터 콘솔에 한국어로 출력한다.

### `EnemyBehaviorSO` 확장

몬스터별 BT Asset을 연결하기 위해 다음 필드를 추가한다.

```csharp
[Header("Behavior Tree")]
public BehaviorTreeAsset behaviorTree;
```

JSON을 소스 오브 트루스로 사용할 경우 다음 필드도 추가할 수 있다.

```csharp
[Header("Behavior Tree Source")]
public TextAsset behaviorJson;
public BehaviorTreeAsset behaviorTree;
```

이 경우 개발 중에는 JSON을 수정한 뒤 Importer로 BT Asset을 재생성하고, 런타임에서는 `behaviorTree`만 사용한다. 런타임에서 JSON을 파싱해 BT를 생성하지 않는다.

비행형까지 포함하려면 다음 구조도 가능하다.

```csharp
[Header("Behavior Tree")]
public BehaviorTreeAsset groundBehaviorTree;
public BehaviorTreeAsset flyingBehaviorTree;
```

초기에는 `behaviorTree` 하나만 추가하고, 트리 내부에서 지상/비행 분기를 나누는 방식이 단순하다.

### `BehaviorPhase` 확장

페이즈별 완전 다른 행동이 필요할 때만 BT override를 허용한다.

```csharp
[Header("Behavior Tree Override")]
public bool overrideBehaviorTree;
public BehaviorTreeAsset behaviorTree;
```

기본 방침은 트리 교체보다 Blackboard 값 변경이다. 페이즈마다 트리를 갈아끼우면 디버깅과 재현성이 떨어질 수 있다.

---

## 마이그레이션 단계

### 진행 상태: 2026-05-11

현재 문서 기준 Phase 2까지의 코드/데이터 반영 상태는 다음과 같다.

| 단계 | 상태 | 반영 내용 | 남은 작업 |
|------|------|-----------|-----------|
| 1단계: 안전장치 추가 | 진행 완료 | `EnemyTransitionStateType`에 `Circle`, `Guard`, `Charge`, `Flank`, `Counter` 추가. `TransitionEnemyStateNode` 전환 범위 확장. `IsBlockedEnemyStateNode`, `HasEnemyActionDelayElapsedNode`, `KeepCurrentStateNode`, `RequestEnemyAttackSlotNode` 추가. `ExecuteEnemyAttackNode`가 공격 슬롯 요청과 BT 공격 시작 통지를 수행하도록 수정 | Play Mode에서 피격/사망/공격 중 BT 개입 차단 확인 |
| 1.5단계: 몬스터 행동 JSON 파이프라인 추가 | 진행 완료 | `MonsterBehaviorTreeJsonImporter`, `EnemyBehaviorJsonExporter` 추가. 몬스터 행동 JSON 스키마, 검증, 폴더 일괄 Import 메뉴 추가. `EnemyBehavior_Skeleton_Common.json` 샘플 추가 | Unity 에디터에서 Import 메뉴 실행 후 생성 BT Asset 확인 |
| 2단계: 지상형 단순 몬스터 1종 전환 | 부분 완료 | Skeleton Common용 JSON 원본 추가. JSON에서 `BT_EnemyBehavior_Skeleton_Common.asset`을 생성할 수 있는 경로와 임포터 준비 | 실제 Generated BT Asset 생성, Skeleton 프리팹에 `BehaviorTreeRunner` 연결, 레거시 `EnemyBrain.MakeDecision()` 비활성화, Play Mode 검증 |

검증 상태:

| 항목 | 결과 |
|------|------|
| `dotnet build UPlayground.sln --no-restore` | 성공. 기존 외부 패키지/Unity 참조 경고만 존재 |
| Unity 배치모드 컴파일 | 사용자 중단으로 미완료 |
| JSON -> BT Asset 실제 생성 | 미실행 |
| Skeleton 프리팹 BT 연결 | 미진행 |

현재 상태는 "Phase 2 구현 기반 준비 완료, 실제 프리팹 전환과 Play Mode 검증 전"이다.

주의: `EnemyBehavior_Skeleton_Common.json`은 몬스터 행동 JSON이다. 기본 권장 메뉴는 `UPlayGround/Character/AI/Monster Behavior Json/Import Selected Json` 또는 `Import Folder`다. 기존 `UPlayGround/Character/AI/Behavior Tree Json/Import Json` 메뉴로 잘못 넣어도 Monster Behavior JSON을 감지하면 전용 임포터로 라우팅해 빈 BT 에셋 생성을 막는다.

### 1단계: 안전장치 추가

| 작업 | 결과 |
|------|------|
| `EnemyBlackboardKeys` 추가 | 키 문자열 통일 |
| `IsBlockedEnemyStateNode` 추가 | BT가 피격/사망/공격 상태를 덮지 않음 |
| `TransitionEnemyStateNode` 확장 | 모든 지상 상태 전환 가능 |
| `ExecuteEnemyAttackNode` 수정 | 공격 슬롯 요청과 쿨다운 기록을 우회하지 않음 |

### 1.5단계: 몬스터 행동 JSON 파이프라인 추가

| 작업 | 결과 |
|------|------|
| Monster Behavior JSON 스키마 정의 | 기존 몬스터 동작을 사람이 읽고 수정 가능한 데이터로 표현 |
| `EnemyBehaviorJsonExporter` 추가 | 기존 `EnemyBehaviorSO`에서 JSON 초안 생성 |
| `MonsterBehaviorTreeJsonImporter` 추가 | 몬스터 행동 JSON을 BT Asset으로 변환 |
| Import Folder 메뉴 추가 | 여러 몬스터 JSON을 일괄 BT Asset으로 재생성 |
| JSON 검증 추가 | 잘못된 state/action/condition을 에셋 생성 전에 차단 |

### 2단계: 지상형 단순 몬스터 1종 전환

Skeleton 계열처럼 기본 근접 행동만 필요한 몬스터를 선택한다.

현재 기준 1차 대상은 다음 JSON 원본이다.

| 구분 | 경로 |
|------|------|
| JSON 원본 | `Assets/10.Datas/AI/BehaviorTree/SourceJson/Ground/EnemyBehavior_Skeleton_Common.json` |
| 생성 대상 BT Asset | `Assets/10.Datas/AI/BehaviorTree/Generated/BT_EnemyBehavior_Skeleton_Common.asset` |
| 기존 BehaviorSO | `Assets/10.Datas/Actor/Enemy/BehaviorData/BehaviorData_skeleton_common.asset` |

에디터 메뉴:

```
UPlayGround/Character/AI/Monster Behavior Json/Import Selected Json
UPlayGround/Character/AI/Monster Behavior Json/Import Folder
UPlayGround/Character/AI/Monster Behavior Json/Export From Selected BehaviorSO
```

사용 순서:

1. `Import Selected Json`을 선택한다.
2. `Assets/10.Datas/AI/BehaviorTree/SourceJson/Ground/EnemyBehavior_Skeleton_Common.json`을 고른다.
3. 생성된 `Assets/10.Datas/AI/BehaviorTree/Generated/BT_EnemyBehavior_Skeleton_Common.asset`을 확인한다.
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

### 3단계: 기존 `EnemyBrain.MakeDecision()` 비활성화

`BehaviorTreeRunner`가 활성인 몬스터는 Brain의 의사결정을 실행하지 않는다.

```csharp
private void Update()
{
    if (_behaviorTreeRunner != null && _behaviorTreeRunner.IsRunning)
        return;

    // 레거시 의사결정
    MakeDecision();
}
```

이 단계는 과도기용이다. 모든 몬스터 전환이 끝나면 레거시 분기는 제거한다.

### 4단계: 전술 반응 이전

기존 `TryReactToPlayerState()`를 BT 조건으로 분해한다.

| 기존 조건 | BT 조건 |
|-----------|---------|
| `IsPlayerAttacking()` | `BlackboardBoolCondition(IsPlayerAttacking)` |
| `IsPlayerGuarding()` | `BlackboardBoolCondition(IsPlayerGuarding)` |
| `IsPlayerStaggered()` | `BlackboardBoolCondition(IsPlayerStaggered)` |
| `IsPlayerRecovering()` | `BlackboardBoolCondition(IsPlayerRecovering)` |
| `IsPlayerDodgingFrequently()` | `BlackboardBoolCondition(IsPlayerDodgingFrequently)` |

### 5단계: 페이즈 이전

`UpdatePhase()`는 Context에 남기고, 페이즈 결과를 Blackboard에 기록한다.

```
SyncEnemyPhaseService
├── HP Percent 계산
├── EnemyAIContext.UpdatePhase()
└── Blackboard(CurrentPhaseName, PhaseIndex, Phase Options) 갱신
```

BT는 `CurrentPhaseName`, `allowCharge`, `allowFlank`, `maxConsecutiveAttacks` 같은 값을 조건/가중치에 사용한다.

### 6단계: 비행형 전환

지상형 안정화 이후 진행한다.

| 순서 | 작업 |
|------|------|
| 1 | `EnemyFlyingBrain` 데이터를 `EnemyFlyingAIContext`로 분리 |
| 2 | `Flying_Chase`, `Flying_GroundAttack`, `Flying_Retreat`, `Flying_Circle` 전환 노드 추가 |
| 3 | 지상 루프를 BT로 이전 |
| 4 | `TakeOff` 조건을 BT로 이전 |
| 5 | `AirCircle` 공격 횟수와 `Dive`/`Land` 선택을 BT로 이전 |
| 6 | `EnemyFlyingBrain.MakeDecision()` 제거 |

### 7단계: 레거시 Brain 제거

모든 몬스터 프리팹이 BT Asset을 가지면 다음 작업을 수행한다.

- `EnemyBrain.MakeDecision()` 제거
- `EnemyFlyingBrain.MakeDecision()` 제거
- 상태 클래스가 직접 `EnemyBrain` 타입을 요구하는 생성자를 `EnemyAIContext` 기반으로 교체
- `EnemyBrain` 이름이 남아 있다면 `EnemyAIContext`로 리네임
- 프리팹에서 레거시 Brain 컴포넌트 의존성 제거 또는 Context 컴포넌트로 교체
- 몬스터별 JSON 원본과 생성된 BT Asset의 대응 관계를 `Assets/10.Datas/AI/BehaviorTree/Generated` 기준으로 정리

---

## 프리팹 셋업

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

## 검증 체크리스트

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

---

## 위험 요소

| 위험 | 대응 |
|------|------|
| BT가 공격/피격/사망 상태를 덮어씀 | `IsBlockedEnemyStateNode`를 모든 행동 분기 앞에 둔다 |
| `ExecuteEnemyAttackNode`가 그룹 슬롯을 우회 | 공격 슬롯 요청을 Action Node 또는 Context API에 통합 |
| Blackboard 문자열 오타 | `EnemyBlackboardKeys` 상수화 |
| 페이즈별 트리 교체로 디버깅 어려움 | 기본은 Blackboard 값 변경, 꼭 필요한 경우만 트리 override |
| 비행형 상태 콜백과 BT Tick 충돌 | 비행형은 지상 루프부터 단계적으로 전환 |
| 기존 State 생성자가 `EnemyBrain` 타입에 묶임 | `EnemyAIContext` 인터페이스 또는 베이스 타입으로 생성자 교체 |
| 모든 몬스터 동시 Tick 비용 | Tick interval 조정, 거리 기반 Runner Pause, Debug Mode 제한 |
| JSON과 BT Asset 불일치 | JSON을 소스 오브 트루스로 두고 Generated BT Asset은 재생성 가능 산출물로 취급 |
| 사람이 작성한 JSON 오타 | Import 전 스키마/노드/상태/Blackboard 키 검증 |
| 저수준 BT JSON과 몬스터 행동 JSON 혼동 | `BehaviorTreeJsonUtility`는 BT round-trip용, `MonsterBehaviorTreeJsonImporter`는 몬스터 행동 변환용으로 분리 |

---

## 완료 기준

BT 완전 전환은 다음 조건을 모두 만족해야 완료로 본다.

- 모든 몬스터 프리팹이 `BehaviorTreeRunner`와 BT Asset을 가진다.
- 모든 몬스터의 기존 행동은 JSON 원본으로 작성되어 있고, 해당 JSON에서 BT Asset을 재생성할 수 있다.
- `EnemyBrain.MakeDecision()`과 `EnemyFlyingBrain.MakeDecision()`이 제거된다.
- 지상형/비행형 몬스터의 행동 선택은 BT Asset에서 확인 가능하다.
- 기존 State Machine은 KCC 물리와 애니메이션 생명주기만 담당한다.
- 공격 슬롯, 페이즈, 전술 메모리, 순찰, 비행 루프가 BT 경로에서 모두 동작한다.
- Play Mode에서 BT Debug Trace로 현재 실행 노드와 실패 원인을 확인할 수 있다.
- Skeleton 계열, 원거리형, 엘리트/보스형, 비행형 대표 몬스터가 각각 수동 검증을 통과한다.
