# 액터 시스템 아키텍처 개선 설계 문서

> 작성일: 2026-07-31
> 대상 버전: Unity 6 (6000.0.60f1), URP
> 분류: 설계서(단계 구현 중). Phase 단위로 본 문서를 갱신하고, 전체 완료 후 `docs/Complete/`로 이관한다.
> 분석 대상: `Assets/02.Scripts/GameActor/` — 390개 파일, 약 69,500 라인 (2026-07-31 기준)

## 구현 현황 (2026-07-31)

첫 구현 슬라이스에서 의존 관계상 선행되는 기반과 액터 시계 정합성을 반영했다.

| 항목 | 상태 | 반영 내용 |
|------|------|----------|
| §5-2 B2 | 완료 | 플레이어/몬스터 접지 이탈 판정을 `GameActorState`로 통합 |
| §4-1 A3 | 완료 | `BlocksExitTo` 정책으로 처형·사망 전이 잠금을 상태에 위임 |
| §4-2 A2 | 완료 | 전체 상태를 `ActorStateId`/enum 전이 가드로 이관, 문자열 가드 0건 |
| §4-3 A1 | 진행 중 | `ActorStateMachine`/`IConfigurableState<T>` 기반 추가. Player Idle/GroundMove/Airborne, Enemy Idle 캐시 적용. 직접 생성 249건 → 152건 |
| §5-1 B1 | 완료(Play Mode 검증 필요) | `ActorTime`을 방어 창, Poise, Break, 스왑 무적·카운터, 경직 내성, 차지·자동질주 판정에 적용 |
| §4-4 A4 | 완료 | `ICombatResolvable` 계약 기반 개방형 디스패치로 변경 |
| §5-3 B3 | 1차 완료 | 안 B 적용. Prop/Item 계열의 전투 비주얼 컴포넌트 자동 부착 제거 |
| §6-2 C2 | 진행 중 | enum 전이 가드 회귀 테스트 추가. Resolver 및 ActorClock 테스트는 후속 |
| §6-3 C3 | 완료 | 인터페이스 참조의 Unity fake-null 검사 및 파괴 객체 캐시 제거 |

아직 남은 범위는 인자 상태 캐시/`Configure` 이관, 플레이어 예약 액션·창 레지스트리 통합(§5-4), 대형 파일 partial 분리(§6-1), Resolver/ActorClock 자동 테스트와 Unity Play Mode·Player Build 검증이다.

---

## 1. 개요

액터 시스템(`UPlayGround.Actor`) 전반의 코드 분석 결과와 개선 방안을 정리한다.
본 문서는 **기능 추가 설계서가 아니라 구조 개선 설계서**다. 게임플레이 동작을 바꾸는 항목은 §5.1(액터 시계) 하나뿐이며, 나머지는 동작 보존 리팩터링이다.

분석 결과 액터 시스템의 **전투 해석 계층은 이미 잘 분리되어 있고, 상태 머신 계층과 컴포넌트 부착 계층에 부채가 집중**되어 있다. 따라서 본 문서는 전투 파이프라인을 재설계하지 않고, 그 주변부를 파이프라인과 같은 수준으로 끌어올리는 것을 목표로 한다.

---

## 2. 현재 구조 요약 — 보존할 자산

아래는 이번 개선에서 **재발명하거나 구조를 흔들지 않는다**. 개선안은 모두 이 자산 위에 얹는다.

| 영역 | 현재 구현 | 핵심 파일 |
|------|----------|----------|
| 전투 해석 파이프라인 | `HitRequest → HitContext → DefenseResolver → DamageResolver → ReactionResolver → CombatResult`. 액터는 계산이 아니라 **적용**만 담당 | `Combat/Resolution/CombatResolutionPipeline.cs` |
| 상태 정책 훅 | `GravityOwnership`, `ActorStateTag`, `AllowsSameTypeReentry`/`CanReenterFrom`, `GrantsInvincibility`, `SuppressesHitReaction`, `BlocksBehaviorTree` | `State/Base/GameActorState.cs:32-89` |
| KCC 콜백 위임 | 컨트롤러가 `ICharacterController` 전 콜백을 현재 상태로 위임 | `MovementController/ActorMovementController.cs:239-333` |
| GAS 단일 루트 | `AbilitySystemComponent`가 Tags/Effects/Abilities/Attributes를 소유. HP도 `Attributes.Vital.Health`로 통일 | `GameActor.cs:181-185`, `PlayerActor.cs:50-54`, `MonsterActor.cs:63-67` |
| 히트 감지 | `OverlapNonAlloc` + 스윕 보간 + `Collider→IDamageable` 캐시 + 도메인 리로드 대응 | `Combat/Detection/CombatHitDetector.cs` |
| 임펄스 합성 | KCC 권위 속도에 상태→감쇠→신규 delta 순서로 합성. damper 완전 순서 정렬로 프레임 간 순서 뒤집힘 방지 | `ActorMovementController.cs:85-101, 246-288` |
| 플레이어 방어 윈도우 | 패리/퍼펙트가드/도지/어시스트패리 반격 창이 **이미 한 컴포넌트로 통합** | `Component/Player/PlayerDefenseController.cs:46-114` |

---

## 3. 식별된 문제

| # | 문제 | 성격 | 우선순위 |
|---|------|------|---------|
| A1 | 상태 인스턴스를 매 전환마다 `new` 로 생성 (249개소) | 계약 부재 + GC | P0 |
| A2 | 문자열 기반 전이 가드 `CanTransitionState(string)` | 무음 회귀 | P0 |
| A3 | 베이스 컨트롤러가 플레이어 전용 구체 타입을 직접 참조 | 레이어링 역전 | P0 |
| A4 | `CombatResolutionPipeline`이 구체 타입 `switch`로 디스패치 | 확장 시 무음 실패 | P0 |
| B1 | `LocalTimeScale`이 일부 컴포넌트 타이머에 미반영 | **게임플레이 정합성** | P1 |
| B2 | `PlayerActorState`/`EnemyActorState` 접지 판정 코드 완전 중복 | 중복 | P1 |
| B3 | 모든 `GameActor` 파생에 전투/비주얼 컴포넌트 강제 부착 | 성능 + 레이어링 | P1 |
| B4 | `PlayerActor`의 스왑/등장/경직내성 플래그 난립 | 상태 폭발 | P1 |
| C1 | 대형 파일이 `CLAUDE.md`의 partial 분리 규약 미준수 | 가독성 | P2 |
| C2 | 액터/상태/전투 자동 테스트 부재 | 안전망 | P2 |
| C3 | 인터페이스 참조에 Unity null 비교 우회 | 잠재 버그 | P2 |

---

## 4. P0 — 상태 머신 계층 정리

이 4개 항목은 **A3 → A2 → A1 순서로 진행**한다. A1(상태 캐시)은 A2(enum 계약)가 선행되어야 안전하다.

### 4-1. A3 — 베이스 컨트롤러의 플레이어 의존 제거

**현상.** 모든 액터가 공유하는 `ActorMovementController.TransitionToState`에 플레이어 전용 분기가 하드코딩되어 있다.

```csharp
// ActorMovementController.cs:209-220
if (_currentState is PlayerFinishAttackState { IsTransitionLocked: true })
    return;

if (_currentState is PlayerDeathState
    && Actor is PlayerActor playerActor
    && !playerActor.IsAlive()
    && newState is not PlayerDeathState)
    return;
```

몬스터에 동일한 요구(예: 보스 처형 모션 중 전이 차단)가 생기면 베이스를 또 고쳐야 한다.

**제안.** 전이 거부 권한을 상태로 내린다.

```csharp
// GameActorState
/// <summary>true면 이 상태가 newState로의 이탈을 거부한다. 사망 잠금·처형 모션 잠금 등.</summary>
public virtual bool BlocksExitTo(GameActorState newState) => false;
```

- `PlayerFinishAttackState` — `IsTransitionLocked` 프로퍼티가 이미 존재(`State/Player/PlayerFinishAttackState.cs:39`). 오버라이드 한 줄이면 끝난다.
- `PlayerDeathState` — 생존 여부는 상태가 `playerActor`를 직접 물어보게 한다. `PlayerActorState.playerActor`가 이미 캐시되어 있다.
- 컨트롤러는 `if (_currentState?.BlocksExitTo(newState) == true) return;` 한 줄로 축약된다.

**동작 변화 없음.** 순수 이동.

### 4-2. A2 — `ActorStateId` enum 도입

**현상.** 전이 가드와 리액션 매핑이 문자열 기준이다.

```csharp
// GameActorState.cs:103
public abstract bool CanTransitionState(string stateName);

// EnemyActorState.cs:38-47 — StateName 문자열 switch
private static CombatReactionState MapReactionState(string stateName)
    => stateName switch { "Hit" => ..., "Stun" => ..., "Knockdown" => ..., ... };

// State/Enemy/EnemyKnockdownState.cs:47
public override bool CanTransitionState(string stateName) => stateName is "Death" or "Grabbed";

// Object/Player/PlayerActor.Combat.cs:133
bool isAttackState = MovementController.CurrentState.StateName == "Attack";

// Object/Player/PlayerActor.Combat.cs:297
MovementController.CurrentState.CanTransitionState("Hit"),
```

상태 이름을 하나 바꾸거나 오타를 내면 **컴파일은 통과하고 가드가 조용히 false**가 된다. 상태가 58개(Player 27 / Enemy 지상·특수 22 / Enemy 비행 9)이므로 사람이 추적할 수 있는 규모를 넘었다.

**제안.**

1. `ActorStateId` enum 신설 (`State/Base/ActorStateId.cs`). 액터 타입별로 값 대역을 나누되, `Hit`/`Stun`/`Knockdown`/`Grabbed`/`Death`처럼 Player·Enemy 공통 의미를 갖는 것은 **공통 값을 공유**한다. 공통 대역이 있어야 `MapReactionState`가 액터 무관하게 동작한다.
2. `GameActorState`에 `public abstract ActorStateId StateId { get; }` 추가. `StateName`은 `=> StateId.ToString()` 기본 구현으로 격하하고 **디버그 표시·BT 블랙보드 표시 전용**으로 용도를 명시한다.
3. `CanTransitionState(ActorStateId from)` 로 시그니처 변경. 기존 문자열 오버로드는 남기지 않는다(남기면 마이그레이션이 끝나지 않는다).
4. `MapReactionState`는 `ActorStateTag` 또는 `ActorStateId` 기반으로 재작성.

**마이그레이션 규모.** `CanTransitionState` 오버라이드 58개 + 호출부 3개. 기계적이지만 광범위하다. 이 단계에서 §6.2의 전이 가드 테스트를 함께 깔아 이후 작업의 안전망으로 쓴다.

### 4-3. A1 — 상태 인스턴스 소유권을 컨트롤러로

**현상.** 상태 생성이 코드 전역에 흩어져 있다.

| 패턴 | 개수 |
|------|------|
| `new Player*State(` | 128 |
| `new Enemy*State(` | 116 |
| `new Npc*State(` | 5 |
| **합계** | **249** |

```csharp
// Object/Player/PlayerActor.Combat.cs:384-396 — 피격 1회마다 상태 객체 신규 할당
MovementController.TransitionToState(new PlayerAirborneState(MovementController));
MovementController.TransitionToState(new PlayerGrabbedState(MovementController, attackData));
MovementController.TransitionToState(new PlayerStunState(MovementController, attackData));
...
```

GC 부하보다 심각한 것은 **계약 부재**다. 임의의 코드가 임의의 상태를 만들어 컨트롤러에 밀어넣을 수 있고, 상태 초기화 로직이 생성자와 `OnEnter`에 이원화되어 있다.

**제안.** `ActorStateMachine`이 상태 인스턴스를 소유한다.

```csharp
// 무인자 상태: 최초 1회 생성 후 영구 재사용
machine.Transition(ActorStateId.Idle);

// 인자 상태: 캐시된 인스턴스를 재구성 후 진입
machine.Transition(ActorStateId.Hit, in hitContext);
```

- 인자를 받는 상태는 `IConfigurableState<T> { void Configure(in T ctx); }` 를 구현하고, 머신이 `Configure` → `OnEnter` 순으로 호출한다.
- 상태별 필드 리셋을 `Configure`로 일원화한다. **재사용 전환 시 이전 실행의 잔여 필드가 남는 것이 이 작업의 유일한 실질 리스크**이므로, 각 상태의 필드를 `Configure`에서 빠짐없이 초기화하는지 파일 단위로 확인한다.
- 등록은 컨트롤러 초기화 시점에 한 번. 외부에서의 `new` 는 상태 생성자를 `internal`로 좁혀 차단한다.

**단계적 적용.** 249개소를 한 번에 바꾸지 않는다. `PlayerIdleState`/`PlayerGroundMoveState`/`PlayerAirborneState` 같은 무인자 고빈도 상태부터 캐시하고, 인자 상태는 그 다음이다.

### 4-4. A4 — 전투 파이프라인 디스패치 개방

**현상.**

```csharp
// CombatResolutionPipeline.cs:17-22
CombatResult result = victim switch
{
    PlayerActor player => ExecutePlayerHit(player, request),
    MonsterActor monster => ExecuteMonsterHit(monster, request),
    _ => default,          // ← 조용히 무시
};
```

현재 `IDamageable` 구현체는 `PlayerActor`, `MonsterActor` 둘뿐이라 지금은 동작한다. 문제는 파괴 가능 오브젝트·BossAssist 소환체·트레이닝 더미를 추가하는 순간 **데미지가 로그 한 줄 없이 사라진다**는 점이다. 파이프라인 내부는 잘 분리되어 있는데 진입점만 닫혀 있는 형태다.

**제안.** 해석 계약을 인터페이스로 승격한다.

```csharp
public interface ICombatResolvable : IDamageable
{
    bool         CanResolveHit(in HitRequest request);
    CombatResult ResolveHit(in HitRequest request);
    CombatResult ApplyResolvedHit(in HitRequest request, in CombatResult resolved);
}
```

`PlayerActor`/`MonsterActor`의 기존 `CanResolveHit`/`ApplyResolvedHit`가 이미 이 형태(`internal`)이므로 접근 제한자 조정과 `ResolveHit` 추출만 하면 된다. 파이프라인은 다음으로 축약된다.

```csharp
if (victim is not ICombatResolvable resolvable)
{
    Debug.LogError($"[CombatResolutionPipeline] {victim.GetType().Name}은 해석 계약을 구현하지 않습니다.");
    return default;
}
```

`DefenseResolver.ResolvePlayerDefense` / `DamageResolver.ResolveMonsterDamage` 같은 **타입별 해석 함수는 그대로 둔다.** 바꾸는 것은 디스패치뿐이다.

---

## 5. P1 — 정합성과 부착 구조

### 5-1. B1 — 액터 시계(`ActorClock`) 도입 ★ 유일한 동작 변경 항목

**현상.** `GameActor.LocalTimeScale` / `DeltaTime`은 히트스톱의 핵심 메커니즘이다(`Manager/Handler/Combat/GameHitStopHandler.cs:202-263`, `DefenseSuccessFeedbackHandler.cs:126-156`). 그러나 실제 소비처는 7곳뿐이다.

**이미 올바르게 반영 중인 곳 (보존):**

| 소비처 | 위치 |
|--------|------|
| `ActorAnimator` 타임라인 / 무한루프 경과 | `Animation/ActorAnimator.cs:1085, 1444` |
| `GameplayEffectController` 지속시간 | `Gameplay/Effect/GameplayEffectController.cs:480` |
| `ActorMovementController` 상태 업데이트 | `MovementController/ActorMovementController.cs:146` |
| `MotionWarpController` | `MovementController/MotionWarpController.cs:237` |
| `BaseProjectile` | `Object/Projectile/BaseProjectile.cs:71` |
| `EnemyAttackState` 접지 판정 | `State/Enemy/EnemyAttackState.cs:139` |

**반영되지 않은 곳 (문제):**

| 소비처 | 위치 | 히트스톱 중 증상 |
|--------|------|-----------------|
| `PoiseStat` 강인도 회복 | `Component/Common/PoiseStat.cs:108, 123, 127` | 정지 중에도 강인도가 회복됨 |
| `MonsterBreakGauge` 노출 타이머·재발동 쿨다운 | `Component/Enemy/MonsterBreakGauge.cs:79, 93` | 노출 창이 실질적으로 짧아짐 |
| `PlayerDefenseController` 방어·반격 창 전체 | `Component/Player/PlayerDefenseController.cs:46-114` | **패리/퍼펙트도지 창이 실질적으로 짧아짐** |
| `PlayerActor` 스왑·경직내성 창 | `Object/Player/PlayerActor.cs:107-118` | 무적/카운터 입력 창이 짧아짐 |

액터 코드 전체 기준 원시 `Time.deltaTime` 39곳, `Time.time` 119곳이다.

**영향.** 히트스톱은 **적중 시점에 발동**하고, 방어 윈도우는 **적중 직전에 열린다**. 따라서 히트스톱이 길수록 플레이어가 실제로 반응할 수 있는 프레임이 줄어든다. 히트스톱 강도를 올리면 방어 난이도가 함께 올라가는, 의도하지 않은 결합이 존재한다.

**제안.** 액터별 누적 시계를 `GameActor`에 둔다.

```csharp
/// <summary>LocalTimeScale이 누적 반영된 액터 고유 시각. 전투 윈도우 비교의 기준.</summary>
public float ActorTime { get; private set; }

private void Update() => ActorTime += Time.deltaTime * _localTimeScale;
```

전환 규칙을 명시적으로 나눈다.

- **`ActorTime` 기준으로 전환** — 게임플레이 판정: 방어/반격 창, 무적 창, 경직 내성, Poise 회복, Break 게이지, 쿨다운.
- **`Time.time` 유지** — 연출: 카메라, VFX 수명, UI 애니메이션, 사운드.

**적용 순서.** `PlayerDefenseController`가 이미 모든 방어 창을 한 컴포넌트에 모아두었으므로(§2 표 마지막 행) **여기부터 시작한다.** 필드 5개와 비교 연산 5개를 `_owner.ActorTime` 기준으로 바꾸면 체감 효과의 대부분을 얻는다. `PoiseStat`/`MonsterBreakGauge`가 그 다음이다.

**검증.** 히트스톱 프로파일의 `freezeTimeScale`을 극단값(0.05 등)으로 두고 패리 성공률이 정지 시간과 무관해지는지 Play Mode에서 확인한다. 이 항목은 수치 체감이 바뀌므로 **적용 후 방어 창 기본값(`_parryWindow` 등) 재조정이 필요할 수 있다.**

### 5-2. B2 — 상태 베이스 접지 판정 중복 제거

`PlayerActorState.ShouldTransitionToAirborne` / `CheckGroundNearby`(`State/Base/PlayerActorState.cs:56-83`)와 `EnemyActorState`의 동일 메서드(`State/Base/EnemyActorState.cs:52-80`)가 **주석까지 포함해 글자 단위로 동일**하다. 차이는 `AirborneGracePeriod` 0.2f(Player) vs 0.15f(Enemy)뿐이다.

`GameActorState`로 pull-up하고 `protected virtual float AirborneGracePeriod => 0.2f;` 만 각 베이스에 남긴다. 리스크 0이므로 **P0 착수 전 워밍업으로 먼저 처리**한다.

### 5-3. B3 — 컴포넌트 강제 부착 완화

**현상.**

```csharp
// GameActor.cs:181-194 — 모든 GameActor 파생에 무조건 부착
AbilitySystem = gameObject.GetOrAddComponent<AbilitySystemComponent>();
AbilitySystem.EnsureInitialized();
...
_colorChanger          = gameObject.GetOrAddComponent<ActorColorChanger>();
_dissolveController    = gameObject.GetOrAddComponent<DissolveController>();          // 986줄
_cameraProximityDither = gameObject.GetOrAddComponent<ActorCameraProximityDither>();  // 944줄, LateUpdate 보유
```

`GameActor` 파생은 전투 액터만이 아니다.

| 파생 클래스 | 전투 참여 | 위 컴포넌트 필요 여부 |
|------------|----------|---------------------|
| `PlayerActor` | O | 필요 |
| `MonsterActor` | O | 필요 |
| `NpcActor` | X | 디졸브·디더 정도만 |
| `GatheringActor` | X | 불필요 |
| `ItemActor` | X | 불필요 |
| `DropItemActor` | X | 불필요 |
| `RestPointActor` | X | 불필요 |

드랍 아이템 하나마다 ASC 초기화 + `LateUpdate` 메시지 디스패치가 붙는다. 사이클 런에서 드랍이 수십 개 쌓이는 상황을 감안하면 무시할 수 없다.

**제안 (택1).**

- **안 A (권장, 장기).** `CombatActor : GameActor` 중간 계층 신설. 전투/피격 비주얼 컴포넌트를 `CombatActor`로 이동하고 `PlayerActor`/`MonsterActor`만 상속. `IDamageable` 요구도 여기로 모을 수 있어 §4-4의 `ICombatResolvable`과 자연스럽게 맞물린다. 다만 프리팹 스크립트 참조가 바뀌므로 **에셋 마이그레이션 검증이 필요**하다.
- **안 B (저위험, 단기).** `protected virtual bool RequiresCombatVisuals => true;` 를 열고 Prop/Item 계열에서 `false`로 내린다. 프리팹 영향 없음. 안 A로 가기 전 중간 단계로도 유효하다.

`ActorCameraProximityDither`는 조기 반환이 있더라도 MonoBehaviour `LateUpdate` 디스패치 비용 자체는 발생하므로, 미사용 액터에서는 **컴포넌트를 붙이지 않는 것**이 핵심이다(`enabled = false`가 아니라).

### 5-4. B4 — 플레이어 타임드 플래그 정리

**현상.** `PlayerActor.cs:104-134`에 창(window) 성격의 필드가 몰려 있다.

```
_swapAssistQueued
_assistParryFallbackPending / _assistParryFallbackTime
_swapEvadeQueued / _swapEvadeTarget
_swapEvadeInvincibleEndTime / _swapEvadeCounterInputEndTime
_staggerImmuneEndTime
_entryAttackQueued / _entryAttackTarget
_isEntryAttackPending / _isSwapEvadeCounterAttackPending / _isSwapSpecialAttackPending
_isInputSuppressed
```

각각 `Time.time <= X` 패턴이 반복되고, **사망·캐릭터 스왑·씬 전환 시 이들을 일괄 리셋하는 단일 지점이 없다.** 조합 수가 곱으로 늘어 테스트도 불가능하다.

**제안.** 이미 존재하는 `PlayerDefenseController` 패턴을 확장한다. 새 개념을 발명하지 않는다.

1. **창(window)** — `PlayerDefenseController`가 방어 창 5종을 이미 `Time.time` 기준 만료 시각으로 관리한다. 이를 `enum CombatWindow → 만료 시각` 딕셔너리 기반으로 일반화하고(`Open`/`IsOpen`/`Close`/`CloseAll`), 스왑 무적·카운터 입력·경직 내성 창을 여기로 흡수한다. §5-1의 `ActorTime` 전환도 이 한 곳에서 끝난다.
2. **예약 액션** — `_is*Pending` / `_*Queued` 계열은 `enum PendingAction` 큐로 대체. `PlayerAttackState.OnEnter`의 1회 소비 로직이 그대로 매핑된다.
3. `CloseAll()` + 큐 비우기를 사망/스왑/씬 전환 경로에서 호출해 **리셋 지점을 하나로 만든다.**

§5-1과 묶어서 진행하면 작업이 겹치지 않는다.

---

## 6. P2 — 가독성과 안전망

### 6-1. C1 — 대형 파일 partial 분리

`CLAUDE.md`의 "대형 클래스는 `클래스명.기능.cs` partial 분리" 규약을 지키지 않은 파일이 남아 있다. `PlayerCombat`(6분할)·`PlayerActor`(7분할)의 선례를 따른다.

| 파일 | 라인 | 제안 분할 |
|------|------|----------|
| `Animation/ActorAnimator.cs` | 1,965 | `.Timeline` / `.Layers` / `.RootMotion` / `.LoopEvents` / `.Overlay` |
| `Component/Enemy/EnemyCombat.cs` | 1,460 | `.Attack` / `.HitDetection` / `.Decision` |
| `Component/Player/PlayerEquipment.cs` | 1,270 | `.Weapon` / `.Visual` |
| `MovementController/MotionWarpController.cs` | 1,260 | `.Targeting` / `.RootDelta` |
| `Gameplay/Ability/ActorAbilitySystem.cs` | 1,071 | `.Activation` / `.Cooldown` |

`ActorAnimator`가 최우선이다. 타임라인 시퀀싱·레이어 재생·루트모션 누적·루프 이벤트 상태 머신·외부 프리뷰가 한 파일에 섞여 있어 MotionSet 관련 수정의 리스크가 가장 높다.

### 6-2. C2 — 자동 테스트 도입

현재 `Tests/EditMode`에는 Ability 14 / FlowGraph 3 / Party 3 / Movement / AI가 있으나 **액터·상태·전투 해석 테스트가 없다.**

착수 우선순위:

1. **`DefenseResolver` / `DamageResolver` / `ReactionResolver`** — 입력이 `HitContext` + 쿼리 구조체이고 출력이 값 타입이라 거의 순수 함수다. Unity 씬 없이 EditMode에서 검증 가능하며 **투자 대비 효과가 가장 크다.**
2. **전이 가드 행렬** — §4-2에서 `ActorStateId`가 도입되면 "상태 A에서 상태 B로 전이 가능한가"를 표로 검증할 수 있다. §4-3(상태 캐시)의 안전망 역할을 한다.
3. **`ActorClock` 회귀** — §5-1 적용 후 "`LocalTimeScale`이 0.05일 때 방어 창의 실제 경과 프레임 수가 1.0일 때와 같은가".

### 6-3. C3 — 인터페이스 참조의 Unity null 비교

```csharp
// Combat/Detection/CombatHitDetector.cs:153
if (damageable == null || collected.Contains(damageable)) continue;

// Combat/Detection/CombatHitDetector.cs:183-184
IDamageable resolved = collider.GetComponent<IDamageable>()
                    ?? collider.GetComponentInParent<IDamageable>();
```

`damageable`은 **인터페이스 타입**이므로 `== null`과 `??`가 `UnityEngine.Object`의 오버로드된 `==`를 타지 않는다. 파괴된 액터가 fake-null 상태로 통과할 수 있다.

현재는 `Overlap`이 반환하는 콜라이더가 항상 당해 프레임 살아있는 인스턴스이고 캐시가 512개에서 통째로 비워지므로 실제 발현 가능성은 낮다. 다만 오브젝트 풀링으로 액터를 재사용하기 시작하면 표면화될 수 있다.

**제안.** 캐시 값을 `Component`로 보관해 Unity null 검사를 살리거나, `damageable is MonoBehaviour mb && mb == null` 검사를 명시적으로 추가한다.

### 6-4. (참고) `CameraManager.Instance` 직접 참조

액터 코드 내 60개소. `CLAUDE.md`에 **의도된 asmdef 예외로 명시**되어 있으므로 본 문서는 변경을 제안하지 않는다. 다만 신규 기능에서는 `ICameraRuntimeAdapter` 또는 기존 카메라 계약으로 표현 가능한지 먼저 검토한다는 기존 방침을 유지한다.

---

## 7. 권장 진행 순서

| 단계 | 항목 | 근거 |
|------|------|------|
| 0 | **§5-2** 접지 판정 중복 제거 | 리스크 0. 워밍업 |
| 1 | **§4-1** 베이스 탈-플레이어화 | 국소 변경. §4-3의 선행 정리 |
| 2 | **§4-2** `ActorStateId` + **§6-2.2** 전이 가드 테스트 | 기계적이나 광범위. 이후 작업의 안전망 |
| 3 | **§4-3** 상태 머신/캐시 | 2단계의 enum 계약 위에서만 안전 |
| 4 | **§5-1 + §5-4** `ActorClock` + 창/예약 정리 | 작업 범위가 겹치므로 묶어서 |
| 5 | **§4-4** 파이프라인 디스패치 개방 | 새 `IDamageable` 추가 시점 이전까지 여유 있음 |
| 6 | **§5-3** 컴포넌트 부착 완화 | 안 B → 안 A 순 |
| 7 | **§6-1, §6-3** | 상시 |

**단, §5-1(액터 시계)은 이 순서를 무시하고 먼저 검토할 가치가 있다.** 위 항목 중 유일하게 "지금 플레이하면 의도와 다르게 동작하는" 문제이며, 나머지는 전부 유지보수성 개선이기 때문이다. §5-1을 선행할 경우 `PlayerDefenseController` 한 파일만 손대는 최소 범위로 시작해 체감 변화를 먼저 확인한 뒤 확대한다.

---

## 8. 영향받는 파일 목록

### 신규
- `State/Base/ActorStateId.cs` (§4-2)
- `State/Base/ActorStateMachine.cs` (§4-3)
- `Combat/Resolution/ICombatResolvable.cs` (§4-4)
- `Component/Player/CombatWindowRegistry.cs` — 또는 `PlayerDefenseController` 확장 (§5-4)
- `Tests/EditMode/Combat/`, `Tests/EditMode/State/` (§6-2)

### 수정
| 파일 | 관련 절 |
|------|--------|
| `Base/GameActor.cs` | §5-1, §5-3 |
| `MovementController/ActorMovementController.cs` | §4-1, §4-3 |
| `State/Base/GameActorState.cs` | §4-1, §4-2, §5-2 |
| `State/Base/PlayerActorState.cs`, `State/Base/EnemyActorState.cs` | §4-2, §5-2 |
| `State/Player/*.cs` (27), `State/Enemy/*.cs` (22), `State/Enemy/EnemyFlying/*.cs` (9), `State/NPC/*.cs` | §4-2, §4-3 |
| `Combat/Resolution/CombatResolutionPipeline.cs` | §4-4 |
| `Object/Player/PlayerActor*.cs` | §4-4, §5-4 |
| `Object/Monster/MonsterActor.cs` | §4-4 |
| `Component/Player/PlayerDefenseController.cs` | §5-1, §5-4 |
| `Component/Common/PoiseStat.cs`, `Component/Enemy/MonsterBreakGauge.cs` | §5-1 |
| `Object/Prop/*.cs`, `Object/NPC/NpcActor.cs` | §5-3 |
| `Combat/Detection/CombatHitDetector.cs` | §6-3 |
| `Animation/ActorAnimator.cs` 외 대형 파일 | §6-1 |

---

## 9. 비고

- 본 문서는 **정적 코드 분석 기준**이다. §5-1의 체감 영향과 §5-3의 성능 이득은 Unity Play Mode 및 Profiler에서 실측 검증이 필요하다.
- §4-3(상태 캐시)의 실질 리스크는 GC가 아니라 **재사용 시 잔여 필드**다. 성능 개선을 근거로 서두르지 말고 상태별 `Configure` 완전성을 파일 단위로 확인한다.
- `MotionEvent`/Ultimate 이벤트 클래스를 다른 어셈블리로 옮기는 작업은 본 문서 범위에 없다. 만약 §6-1 분할 과정에서 이동이 발생하면 `[MovedFrom(true, sourceAssembly: "...")]` 유지가 필수다.
