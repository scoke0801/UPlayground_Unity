# Behavior Tree State/Node/Blackboard 결합도 개선 계획

> 작성일: 2026-05-20  
> 완료일: 2026-05-22  
> 상태: 1차 완료  
> 대상: `Assets/02.Scripts/AI/BehaviorTree/`, `Assets/10.Datas/AI/BehaviorTree/SourceJson/`  
> 관련 문서: [BEHAVIOR_TREE_SYSTEM_GUIDE.md](../guide/BEHAVIOR_TREE_SYSTEM_GUIDE.md), [MONSTER_BT_ADVANCEMENT_GOAL_GUIDE.md](../guide/MONSTER_BT_ADVANCEMENT_GOAL_GUIDE.md), `BEHAVIOR_TREE_CODE_CLEANUP_PLAN.md`(현재 파일 없음)

---

## 완료 요약

2026-05-22 기준으로 결합도 개선 1차 작업을 완료했다.

- `RequestEnemyActionNode`, `EnemyActionRequest`, `EnemyActionIntent`, `EnemyActionStyle`, `EnemyActionResolver`를 추가해 BT가 구체 상태 대신 전술 의도를 요청하는 경로를 만들었다.
- `TransitionEnemyStateNode`는 구버전 JSON 호환용 래퍼로 축소하고, 실제 상태 선택은 `EnemyActionResolver`가 담당하도록 정리했다.
- `Target.*`, `Self.*`, `AI.*`, `Memory.*`, `Decision.*`, `Cooldown.*` 네임스페이스 블랙보드 키를 추가하고 구키와 병행 기록하도록 동기화 서비스와 기본 블랙보드를 확장했다.
- `BlackboardCompareNode`와 `BlackboardComparisonType`을 추가하고, `IsPlayerAttacking`, `RecentlyHitByPlayer`, `RecentHitCountGreaterOrEqual`, `SelectedIntent`, `IsPoiseBroken` 등 단순 조건 alias를 범용 비교 노드로 변환하도록 importer를 확장했다.
- `ActorStateTag`, `GameActorState.StateTags`, `HasStateTagNode`를 추가하고 `IsBlockedEnemyStateNode`가 `ActorStateTag.InterruptLocked`를 우선 보도록 했다.
- `SyncEnemyMemoryService`는 player read, hit memory, poise 동기화 메서드로 분리했다.
- 기준 샘플 `EnemyBehavior_Test_IntentRolePlayerRead_AllInOne.json`의 `Transition` 액션을 모두 `RequestAction` 중심으로 변환했다.
- SourceJson 전체 import를 실행해 Generated BT asset 4개를 갱신했다.

검증:

- `dotnet build UPlayground.sln --no-restore`: 성공, 오류 0개.
- Unity batch import `UPlayGround.AI.BehaviorTree.Editor.MonsterBehaviorTreeJsonImporter.ImportAllSourceJson`: 성공, SourceJson 4개 import 완료.
- Unity import 로그에서 `No script asset for BlackboardCompareNode` 경고가 재발하지 않음을 확인했다.

## 개요

현재 몬스터 BT 구조는 JSON, 노드, 상태 머신, 블랙보드 키가 서로 강하게 묶여 있다. 데이터로 행동을 표현하는 장점은 생겼지만, 새 상태나 새 판단 값을 추가할 때 다음 코드들이 함께 수정되는 문제가 있다.

- `MonsterBehaviorTreeJsonImporter.NodeFactory.cs`의 `condition` / `action` 문자열 switch
- `TransitionEnemyStateNode` / `TransitionFlyingEnemyStateNode`의 상태 생성 switch
- `EnemyTransitionStateType` / `FlyingEnemyTransitionStateType` enum
- `EnemyBlackboardKeys`의 평면 문자열 키 목록
- BT JSON의 `"Transition"`, `"state"`, `"SelectedIntent"` 같은 구체 식별자

목표는 BT가 구체 상태를 직접 고르는 구조에서 벗어나, **의도와 전술 명령을 요청하고 실제 상태 선택은 Resolver/Factory가 담당하는 구조**로 바꾸는 것이다.

---

## 1. 현재 결합 지점

### 1.1 JSON 조건/액션과 노드 클래스의 직접 매핑

`MonsterBehaviorTreeJsonImporter.NodeFactory.cs`는 JSON의 문자열을 노드 타입으로 직접 변환한다.

```json
{ "condition": "IsPlayerAttacking" }
{ "action": "Transition", "state": "Dodge" }
```

이 방식은 읽기 쉽지만, 조건/액션이 늘어날 때마다 importer switch가 커진다. 특히 `"DistanceLessOrEqual"`, `"SelectedIntent"`, `"IsCurrentState"`처럼 실제로는 블랙보드 비교나 상태 태그 비교로 일반화할 수 있는 항목도 전용 케이스로 쌓인다.

### 1.2 BT Action과 GameActorState 생성의 직접 결합

`TransitionEnemyStateNode`는 `EnemyTransitionStateType`을 실제 상태 생성자로 직접 변환한다.

```csharp
EnemyTransitionStateType.Dodge => new EnemyDodgeState(controller, context, detection)
```

이 구조에서는 새 상태를 추가할 때 BT 노드, enum, importer, 상태명 문자열 비교가 함께 변경된다. 지상/비행 상태도 별도 노드와 별도 enum으로 갈라져 있어 공통 전술 의도 표현이 어렵다.

### 1.3 Blackboard 키의 평면화

현재 블랙보드는 런타임 사실, 튜닝값, 메모리, 의사결정 결과, 쿨다운을 한 공간에 둔다.

```text
HasTarget
DistanceToTarget
aggression
SelectedIntent
recentHitCount
Cooldown.IntentCounter.ReadyTime
```

키 문자열이 짧아 사용은 쉽지만, 소유권과 갱신 주기가 드러나지 않는다. 결과적으로 어떤 키가 데이터 초기값인지, 매 Tick 동기화 값인지, 메모리 누적 값인지, 의사결정 결과인지 구분하기 어렵다.

### 1.4 상태명 기반 조건

`IsCurrentState`, `IsBlockedEnemyState`, 비행 상태 조건은 구체 상태명 또는 특정 상태 클래스 목록에 의존한다. 상태가 추가되거나 이름이 바뀌면 BT 조건도 같이 바뀐다.

### 1.5 Blackboard 동기화 코드와 조건 노드 폭증

현재 일부 동기화 코드는 `EnemyTacticalMemory`, `PoiseStat` 등의 값을 하나씩 읽어 블랙보드에 직접 기록한다.

```csharp
var memory = Context.GetComponentCached<EnemyTacticalMemory>();
var isAttacking = memory != null && memory.IsPlayerAttacking();
var isGuarding = memory != null && memory.IsPlayerGuarding();
var recentHitCount = memory?.RecentHitCount ?? 0;
var poiseRatio = poise != null ? poise.PoisePercent : 1f;

Context.Blackboard.SetBool(EnemyBlackboardKeys.IsPlayerAttacking, isAttacking);
Context.Blackboard.SetBool(EnemyBlackboardKeys.IsPlayerGuarding, isGuarding);
Context.Blackboard.SetInt(EnemyBlackboardKeys.RecentHitCount, recentHitCount);
Context.Blackboard.SetFloat(EnemyBlackboardKeys.PoiseRatio, poiseRatio);
```

이 패턴은 필드가 늘어날수록 다음 항목이 같이 증가한다.

- 동기화 코드의 지역변수와 `SetXxx` 호출
- `EnemyBlackboardKeys` 상수
- `Blackboard` 초기값
- 전용 ConditionNode (`IsPoiseBrokenNode`, `WasLastHitHeavyNode` 등)
- importer의 `condition` switch
- JSON condition 이름

모든 상태 검사가 전용 `IsXxxNode`가 되어야 하는 것은 아니다. 이미 블랙보드에 값이 존재하고 단순 비교만 하면 되는 조건은 범용 비교 노드로 통합하는 것이 맞다.

---

## 2. 목표 구조

### 2.1 BT는 상태가 아니라 의도를 출력한다

BT JSON은 구체 `GameActorState`를 직접 고르지 않고 전술 의도를 요청한다.

```json
{ "action": "RequestAction", "intent": "Evade", "style": "Dodge" }
{ "action": "RequestAction", "intent": "Attack", "attackCategory": "Heavy" }
{ "action": "RequestAction", "intent": "Reposition", "style": "Flank" }
{ "action": "RequestAction", "intent": "Defend", "style": "Guard" }
```

실제 상태 선택은 `EnemyActionResolver`가 담당한다.

```text
BT JSON
  -> RequestEnemyActionNode
  -> EnemyActionRequest
  -> EnemyActionResolver
  -> EnemyStateFactoryRegistry
  -> GameActorState
```

BT는 "지금 회피하고 싶다", "압박하고 싶다", "거리를 벌리고 싶다"까지만 표현한다. 지상형은 `EnemyDodgeState`, 비행형은 `EnemyFlyingDiveState`처럼 몬스터 타입과 현재 능력에 맞는 상태를 Resolver가 고른다.

### 2.2 상태 생성은 Factory/Registry로 분리한다

상태 생성 switch를 노드에서 제거하고, 상태 생성 책임을 별도 레이어로 옮긴다.

```csharp
public readonly struct EnemyActionRequest
{
    public EnemyActionIntent Intent { get; init; }
    public EnemyActionStyle Style { get; init; }
    public AbilityAttackCategory AttackCategory { get; init; }
    public string CooldownId { get; init; }
    public float CooldownDuration { get; init; }
}
```

```csharp
public interface IEnemyStateFactory
{
    bool CanCreate(EnemyActionRequest request, BehaviorTreeContext context);
    GameActorState Create(ActorMovementController controller, EnemyActionRequest request, BehaviorTreeContext context);
}
```

초기 구현은 내부적으로 기존 switch를 사용해도 된다. 중요한 것은 `RequestEnemyActionNode`가 직접 `new EnemyDodgeState(...)`를 호출하지 않는 것이다.

### 2.3 Blackboard 키는 네임스페이스를 가진다

새 키는 다음 네임스페이스를 따른다.

| 네임스페이스 | 예시 | 소유권 |
|---|---|---|
| `Target.*` | `Target.Has`, `Target.Object`, `Target.Distance` | Detection 동기화 |
| `Self.*` | `Self.HpPercent`, `Self.StateId`, `Self.StateTags`, `Self.PhaseIndex` | Actor/State/Phase 동기화 |
| `AI.*` | `AI.Aggression`, `AI.PreferredRange`, `AI.GuardChance` | 초기 데이터/튜닝 |
| `Memory.*` | `Memory.RecentHitCount`, `Memory.LastHitReactionType` | `EnemyTacticalMemory` |
| `Decision.*` | `Decision.SelectedIntent`, `Decision.IntentScore.Attack` | 의사결정 Service |
| `Cooldown.*` | `Cooldown.IntentCounter.ReadyTime` | 공통 쿨다운 헬퍼 |

기존 키는 바로 제거하지 않는다. 마이그레이션 기간에는 동기화 서비스가 구키와 신키를 같이 쓴다.

### 2.4 상태명 조건은 State Tag 조건으로 대체한다

구체 상태명을 비교하는 대신 상태 태그를 사용한다.

```csharp
[Flags]
public enum ActorStateTag
{
    None = 0,
    Locomotion = 1 << 0,
    Combat = 1 << 1,
    Defensive = 1 << 2,
    Airborne = 1 << 3,
    InterruptLocked = 1 << 4,
    Recovery = 1 << 5
}
```

BT 조건은 다음처럼 바뀐다.

```json
{ "condition": "HasStateTag", "value": "InterruptLocked" }
{ "condition": "HasStateTag", "value": "Airborne" }
{ "condition": "HasStateTag", "value": "Defensive", "invert": true }
```

`IsBlockedEnemyState`는 장기적으로 `HasStateTag(InterruptLocked)`의 별칭이 된다.

### 2.5 Blackboard 동기화는 Snapshot 단위로 묶는다

`EnemyTacticalMemory`, `PoiseStat`, `EnemyDetection`, `ActorMovementController`의 값을 한 메서드에서 모두 풀어쓰지 않고, 소유권 기준으로 동기화 단위를 나눈다.

1차 정리:

```csharp
SyncTargetFacts(blackboard, detection);
SyncPlayerReadMemory(blackboard, memory);
SyncHitMemory(blackboard, memory);
SyncPoise(blackboard, poise);
SyncStateFacts(blackboard, controller);
```

장기 목표:

```csharp
var snapshot = EnemyBlackboardSnapshot.From(Context);
snapshot.WriteTo(Context.Blackboard);
```

`EnemyBlackboardSnapshot`은 런타임 사실을 읽는 책임과 블랙보드에 기록하는 책임을 한 곳으로 모은다. BT 노드와 importer는 이 값들이 어디서 계산됐는지 알 필요가 없다.

권장 snapshot 분리:

| Snapshot | 입력 | 출력 네임스페이스 |
|---|---|---|
| `TargetBlackboardSnapshot` | `EnemyDetection` | `Target.*` |
| `PlayerReadBlackboardSnapshot` | `EnemyTacticalMemory` | `Memory.Player.*` |
| `HitMemoryBlackboardSnapshot` | `EnemyTacticalMemory` | `Memory.Hit.*` |
| `PoiseBlackboardSnapshot` | `PoiseStat` | `Self.Poise*` |
| `StateBlackboardSnapshot` | `ActorMovementController` | `Self.State*` |

초기 구현에서는 별도 struct까지 만들지 않고 private sync 메서드 분리만 해도 충분하다. 핵심은 동기화 코드가 필드 추가 때마다 긴 지역변수 목록으로 커지지 않게 하는 것이다.

---

## 3. JSON DSL 개선안

### 3.1 기존 alias는 유지하되 내부 표현을 일반화한다

기존 짧은 JSON은 계속 허용한다.

```json
{ "condition": "HasTarget" }
{ "condition": "DistanceLessOrEqual", "value": "optimalCombatDistance" }
```

다만 importer 내부에서는 가능한 경우 일반 조건으로 변환한다.

```json
{
  "condition": "BlackboardCompare",
  "key": "Target.Distance",
  "op": "LessOrEqual",
  "valueKey": "AI.OptimalCombatDistance"
}
```

이렇게 하면 새 비교 조건을 추가할 때마다 노드 클래스를 늘리지 않아도 된다.

### 3.2 전용 조건 노드와 범용 비교 노드 기준

모든 조건을 전용 노드로 만들지 않는다. 조건 노드 선택 기준은 다음과 같다.

| 분류 | 기준 | 예시 |
|---|---|---|
| 전용 노드 | 계산이 필요하거나 게임 규칙을 캡슐화해야 함 | `HasTarget`, `ActionDelayElapsed`, `CanUseSkill`, `HasAttackSlot`, `CooldownReady` |
| 범용 비교 노드 | 블랙보드 값 하나를 비교하면 충분함 | `Memory.Player.IsAttacking == true`, `Memory.Hit.RecentCount >= 3`, `Self.PoiseRatio <= 0.2` |
| alias | JSON 가독성을 위한 짧은 이름. 내부 구현은 범용 비교로 변환 가능 | `IsPoiseBroken`, `RecentlyHitByPlayer`, `SelectedIntent` |

예시:

```json
{ "condition": "BlackboardCompare", "key": "Memory.Hit.RecentCount", "op": "GreaterOrEqual", "value": 3 }
{ "condition": "BlackboardCompare", "key": "Self.PoiseRatio", "op": "LessOrEqual", "value": 0.2 }
{ "condition": "BlackboardCompare", "key": "Memory.Player.IsAttacking", "op": "Equal", "value": true }
```

기존 alias는 유지할 수 있다.

```json
{ "condition": "IsPoiseBroken" }
```

내부 의미:

```json
{ "condition": "BlackboardCompare", "key": "Self.IsPoiseBroken", "op": "Equal", "value": true }
```

따라서 `IsPoiseBrokenNode` 같은 전용 노드는 반드시 필요하지 않다. 단, Poise 판정이 단순 bool이 아니라 최근 피격, 경직 면역, 페이즈 보정까지 포함하는 도메인 규칙이 되면 전용 노드로 승격할 수 있다.

### 3.3 새 액션은 RequestAction을 기본으로 한다

기존:

```json
{ "action": "Transition", "state": "Dodge" }
```

권장:

```json
{
  "action": "RequestAction",
  "intent": "Evade",
  "style": "Dodge",
  "cooldownId": "RepeatedHitReaction",
  "cooldownDuration": 2.6
}
```

공격도 같은 방식으로 표현한다.

```json
{
  "action": "RequestAction",
  "intent": "Attack",
  "attackCategory": "Heavy"
}
```

`ExecuteAttack`은 당장 유지하되, 장기적으로는 `RequestAction(intent=Attack)`의 특수 케이스로 통합한다.

---

## 4. 단계별 이행 계획

### Phase D1: 어댑터 추가

목표: 기존 BT를 깨지 않고 새 요청 경로를 추가한다.

| 작업 | 설명 |
|---|---|
| `EnemyActionIntent` 추가 | `Attack`, `Punish`, `Counter`, `Pressure`, `Chase`, `Retreat`, `KeepDistance`, `Defend`, `Evade`, `Recover` |
| `EnemyActionStyle` 추가 | `None`, `Dodge`, `JumpBack`, `Guard`, `Circle`, `Flank`, `Charge`, `Dive`, `Land`, `TakeOff` |
| `EnemyActionRequest` 추가 | BT 노드가 Resolver에 넘기는 값 객체 |
| `RequestEnemyActionNode` 추가 | JSON의 `RequestAction`을 실행하는 새 Action 노드 |
| `EnemyActionResolver` 추가 | 첫 버전은 기존 `TransitionEnemyStateNode` switch 로직을 감싸도 됨 |
| importer 확장 | `"RequestAction"`을 새 노드로 변환. 기존 `"Transition"`은 그대로 유지 |

검증:

- 기존 SourceJson import 결과가 변하지 않아야 한다.
- 새 테스트 JSON 1개에서 `RequestAction`으로 `Dodge`, `Chase`, `Attack`이 동작해야 한다.

### Phase D2: Blackboard 네임스페이스 병행

목표: 구키를 유지하면서 신키를 도입한다.

| 작업 | 설명 |
|---|---|
| `EnemyBlackboardKeys` 정리 | 새 네임스페이스 키 상수 추가 |
| `SyncEnemyBlackboardService/Node` 확장 | `HasTarget`과 `Target.Has`를 같이 기록 |
| `EvaluateEnemyCombatIntentService` 확장 | `SelectedIntent`와 `Decision.SelectedIntent`를 같이 기록 |
| 쿨다운 키 헬퍼 추가 | `EnemyBlackboardKeys.CooldownReadyTime(cooldownId)`로 포맷 통합 |
| Editor 표시명 추가 | 신키도 `BehaviorTreeDisplayNameRegistry`에 한글 표시명 제공 |
| sync 메서드 분리 | player read, hit memory, poise, target facts를 별도 메서드로 분리 |
| snapshot 도입 검토 | 필드가 더 늘면 `EnemyBlackboardSnapshot` 값 객체로 승격 |

검증:

- 기존 조건 노드가 구키로 계속 동작해야 한다.
- 새 `BlackboardCompare` 조건이 신키로 동작해야 한다.

### Phase D2.5: 범용 Blackboard 조건 도입

목표: 단순 상태 검사마다 전용 `IsXxxNode`를 만들지 않도록 한다.

| 작업 | 설명 |
|---|---|
| `BlackboardCompareNode` 추가 | bool/int/float/string 비교를 하나의 조건 노드로 처리 |
| 비교 연산 enum 추가 | `Equal`, `NotEqual`, `Less`, `LessOrEqual`, `Greater`, `GreaterOrEqual` |
| JSON 필드 확장 | `condition`, `key`, `op`, `value`, `valueKey` 지원 |
| alias 변환 추가 | `IsPoiseBroken`, `RecentlyHitByPlayer`, `SelectedIntent` 등을 내부적으로 `BlackboardCompare`로 변환 |
| 기존 전용 노드 유지 | 이미 쓰이는 노드는 삭제하지 않고 호환 경로로 유지 |

전용 노드로 유지할 후보:

- `HasTarget`: Detection 컴포넌트와 타겟 object 유효성을 함께 판단할 수 있음
- `ActionDelayElapsed`: 시간 계산과 전투 템포 규칙 포함
- `CanUseSkill` / `FlyingCanUseSkill`: 스킬/모션/쿨다운/거리 조건을 포함할 여지 있음
- `HasAttackSlot`: 그룹 전투 규칙 포함
- `CooldownReady`: 키 포맷과 시간 비교를 캡슐화

범용 비교로 전환할 후보:

- `IsPlayerAttacking`
- `IsPlayerGuarding`
- `IsPlayerStaggered`
- `IsPlayerRecovering`
- `IsPlayerDodgingFrequently`
- `RecentHitCountGreaterOrEqual`
- `SelectedIntent`
- `IsPoiseBroken` (단순 bool로 쓰는 동안)

### Phase D3: State Tag 도입

목표: 상태명 직접 비교를 줄인다.

| 작업 | 설명 |
|---|---|
| `ActorStateTag` 추가 | 공통 상태 태그 enum |
| `GameActorState`에 태그 API 추가 | 기본값 `None`, 각 Enemy/Flying 상태에서 필요한 태그 override |
| `HasStateTagNode` 추가 | 현재 상태 태그 비교 |
| `IsBlockedEnemyStateNode` 내부 위임 | 가능하면 `InterruptLocked` 태그를 우선 사용하고, 구상태 호환 로직은 fallback으로 유지 |
| JSON alias 추가 | `"HasStateTag"` 조건 지원 |

검증:

- `Hit`, `Death`, `Attack`, `Guard`, `Flying_*` 상태 태그가 의도대로 기록되는지 DebugTrace/Blackboard에서 확인한다.

### Phase D4: 기존 Transition JSON 점진 변환

목표: 새 데이터는 `RequestAction`을 쓰고, 기존 데이터는 안전하게 옮긴다.

| 작업 | 설명 |
|---|---|
| 기준 JSON 1개 변환 | `EnemyBehavior_Test_IntentRolePlayerRead_AllInOne.json`부터 변환 |
| `Transition` 사용량 확인 | SourceJson에서 남은 `"action": "Transition"` 목록 추적 |
| 변환 규칙 문서화 | `Dodge -> Evade/Dodge`, `JumpBack -> Evade/JumpBack`, `Circle -> KeepDistance/Circle` |
| importer 경고 추가 검토 | 새 JSON에서 `Transition` 사용 시 warning만 표시. 에러는 금지 |

검증:

- 변환 전후 행동 우선순위와 그룹 구조가 유지되어야 한다.
- 전투 중 상태 전환 실패 시 Resolver 실패 사유가 DebugTrace에 남아야 한다.

---

## 5. 변환 매핑 초안

| 기존 state | intent | style | 비고 |
|---|---|---|---|
| `Idle` | `Recover` | `None` | 타겟 없음/대기 |
| `Patrol` | `Recover` | `None` | 순찰은 별도 `PatrolOrIdle` alias 유지 가능 |
| `Chase` | `Chase` | `None` | 거리 좁히기 |
| `Attack` | `Attack` | `None` | 직접 상태 전환보다 `ExecuteAttack`/공격 요청 선호 |
| `Retreat` | `Retreat` | `None` | 거리 벌리기 |
| `Dodge` | `Evade` | `Dodge` | 회피 |
| `JumpBack` | `Evade` | `JumpBack` | 피격/콤보 리셋 반응 |
| `Circle` | `KeepDistance` | `Circle` | 거리 유지/압박 템포 |
| `Guard` | `Defend` | `Guard` | 방어 |
| `Charge` | `Pressure` | `Charge` | 돌진 압박 |
| `Flank` | `Pressure` | `Flank` | 측면 압박 |
| `Counter` | `Counter` | `None` | 반격 |
| `Flying_Dive` | `Attack` 또는 `Evade` | `Dive` | 맥락별로 분리 필요 |
| `Flying_TakeOff` | `Evade` | `TakeOff` | 지상 위협 회피 |
| `Flying_Land` | `KeepDistance` | `Land` | 공중 루프 종료 |

---

## 6. 원칙

- 기존 `.asset` 직렬화 필드명과 노드 클래스 이름은 가능한 한 유지한다.
- 기존 JSON 스키마는 바로 폐기하지 않는다. 새 alias를 추가하고 점진 변환한다.
- BT는 의사결정 계층, 상태 머신은 실행 계층으로 역할을 분리한다.
- 지상/비행 차이는 JSON 분기가 아니라 Resolver 능력 판정으로 흡수한다.
- 블랙보드 키는 문자열 저장을 유지하되, 코드 접근은 상수/selector/schema를 통해 제한한다.
- 블랙보드 동기화는 값의 소유권과 갱신 주기 기준으로 나눈다.
- 단순 블랙보드 값 비교는 범용 조건 노드를 우선 사용하고, 전용 노드는 도메인 규칙이 있는 경우에만 추가한다.
- 하드 매핑을 한 번에 없애려 하지 않는다. 먼저 한 곳으로 모은 뒤, 다음 단계에서 데이터화한다.

---

## 7. 완료 기준

다음 조건을 만족하면 결합도 개선 1차 완료로 본다.

- [x] 새 BT JSON에서 `RequestAction`만으로 회피, 추격, 방어, 공격 요청을 표현할 수 있다.
- [x] `TransitionEnemyStateNode`는 구버전 호환 경로로만 남는다.
- [x] `EnemyActionResolver`가 상태 전환 실패 사유를 DebugTrace에 기록한다.
- [x] 새 블랙보드 키는 `Target.*`, `Self.*`, `AI.*`, `Memory.*`, `Decision.*`, `Cooldown.*` 네임스페이스를 따른다.
- [x] player read, hit memory, poise 동기화 코드가 섹션별 메서드 또는 snapshot으로 분리되어 있다.
- [x] `BlackboardCompare`로 단순 bool/int/float/string 조건을 표현할 수 있다.
- [x] `IsBlockedEnemyState` 계열 판단이 상태명 목록이 아니라 `ActorStateTag.InterruptLocked`를 우선 사용한다.
- [x] SourceJson 기준 샘플 1개 이상이 `Transition` 중심에서 `RequestAction` 중심으로 변환되어 있다.
