# 전투 시스템 구조 개선 계획

> **보관 문서 주의:** 이 문서의 플레이어 공격 데이터 예시는 Ability 전환 이전 구조다. 현재 단일 소스는 `AbilitySetSO`이며 최신 기준은 `../TODO/GAMEPLAY_ABILITY_SYSTEM_SPEC.md`를 따른다.

> 작성일: 2026-06-03  
> 대상 버전: Unity 6 (6000.0.60f1), URP  
> 분류: 구조 개선 계획 및 구현 현황. Phase 1~8의 1차 구현은 반영되었고, MotionSet 이벤트 정밀 검증은 후속 확장 항목으로 남아 있다.  
> 관련 문서: `Assets/docs/guide/COMBAT_SYSTEM_GUIDE.md`

---

## 1. 목표

현재 전투 시스템은 `PlayerCombat`, `EnemyCombat`, `PlayerActor.TakeDamage()`, `MonsterActor.TakeDamage()`, MotionEvent, 상태 머신이 직접 맞물려 동작한다. 기능 구현 속도는 빠르지만, 전투 기능이 늘어날수록 다음 문제가 커진다.

- 공격 실행, 판정, 피해 계산, 방어 판정, 피격 반응, 카메라/VFX/UI 피드백이 한 흐름 안에서 직접 호출된다.
- `PlayerCombat`과 `EnemyCombat`이 공격 데이터 선택, 판정, 전투 상태, 피드백, 특수공격 탐색까지 많은 책임을 가진다.
- `AttackData`가 공격 정의, 런타임 인스턴스, 히트 컨텍스트, 피격 결과 전달용 필드를 모두 담는다.
- MotionEvent가 `PlayerCombat` / `EnemyCombat` 구현 세부를 직접 알고 있다.
- 데이터 검증이 부족해 MotionSet 이벤트, `hitPhaseIndex`, 공격 데이터, 텔레그래프 설정 불일치가 런타임에서야 드러난다.

본 계획의 목표는 전투 시스템을 AAA식 대형 액션 구조에 가깝게 다음 방향으로 정리하는 것이다.

1. 전투 결과 계산과 전투 연출 피드백을 분리한다.
2. 공격 정의, 공격 실행 인스턴스, 1회 히트 컨텍스트, 피해 결과를 분리한다.
3. 플레이어/몬스터가 공유할 수 있는 판정/피해/방어/리액션 계층을 만든다.
4. 기존 MotionSet + MotionEvent + ScriptableObject 데이터 주도 구조는 유지한다.
5. 전체 교체가 아니라, 현재 구현과 공존하는 점진 리팩토링으로 진행한다.

---

## 1.1 구현 반영 현황

2026-06-03 기준으로 계획의 1차 코드 구현은 기존 `AttackData`/`IDamageable.TakeDamage(AttackData)` 호환 경로를 유지하면서 완료되었다.

| Phase | 구현 상태 | 반영 내용 |
|-------|-----------|-----------|
| Phase 1 피해 계산 분리 | 완료 | `DamageResolver`, `DamageResult`로 플레이어/몬스터/특수 브레이크 피해 계산 분리 |
| Phase 2 전투 피드백 분리 | 완료 | `CombatFeedbackDispatcher`, `CombatFeedbackContext`, `CombatFeedbackProfile`로 데미지 플로터, 히트 FX, 카메라, HitStop, VitalOrb 호출 집중 |
| Phase 3 방어 판정 중앙화 | 완료 | `DefenseResolver`, `DefenseResult`로 가드, 패리, 퍼펙트 도지, 무적, `Unblockable` 판정 분리 |
| Phase 4 리액션 결정 분리 | 완료 | `ReactionResolver`, `ReactionDecision`으로 플레이어/몬스터 피격 상태 결정 분리 |
| Phase 5 CombatStateTracker 분리 | 완료 | `PlayerCombatStateTracker`로 전투 유지 시간, 위협 탐색, 상태 변화 이벤트 분리 |
| Phase 6 HitDetector 분리 | 완료 | `CombatHitDetector`, `MeleeHitShape`, `CombatHit`으로 플레이어/몬스터 근접 판정 공통화 |
| Phase 7 CombatActionRunner 도입 | 완료 | `CombatActionRunner`, `CombatActionDefinition`, `CombatActionInstance`, `CombatTimelineEvent` 추가 및 legacy MotionEvent 병행 연결 |
| Phase 8 데이터 검증 툴 | 부분 완료 | `CombatDataValidatorWindow`에서 공격 SO 기본 검증과 Markdown 리포트 저장 지원. MotionSet 이벤트와 `hitPhases` 정밀 매칭은 미구현 |

이번 구현은 "전체 교체"가 아니라 "legacy 경로와 공존하는 전환 구조"다. 따라서 `PlayerCombat`과 `EnemyCombat`은 아직 공격 데이터 선택, 콤보/쿨다운, MotionEvent 호환 API를 보유한다. 최종 구조로 가려면 runner가 실제 판정/해결 pipeline을 완전히 주도하도록 추가 마이그레이션이 필요하다.

---

## 2. 참고 구조

### Unreal Gameplay Ability System

Unreal GAS는 `AbilitySystemComponent`, `GameplayAbility`, `Attribute`, `GameplayEffect`, `GameplayCue`로 능력 실행, 수치, 효과, 연출을 분리한다.

본 프로젝트에 그대로 이식하지는 않지만 다음 원칙은 차용한다.

| GAS 개념 | 본 프로젝트 적용 방향 |
|----------|---------------------|
| Ability | 공격/스킬/특수공격을 `CombatAction` 단위로 실행 |
| Attribute | 기존 `ActorStatContainer`, HP, Poise, Break Gauge를 명확히 분리 |
| Gameplay Effect | 피해/회복/버프/디버프를 `CombatEffect` 또는 `DamageResult`로 표현 |
| Gameplay Cue | HitStop, VFX, SFX, UI, 카메라를 `CombatFeedback` 이벤트로 분리 |

참고:

- https://dev.epicgames.com/documentation/unreal-engine/gameplay-ability-system-for-unreal-engine
- https://dev.epicgames.com/documentation/unreal-engine/gameplay-ability

### Unity ECS / DOTS

Unity ECS는 Entity, Component, System을 분리해 데이터와 처리 시스템의 경계를 명확히 한다.

본 프로젝트는 MonoBehaviour/KCC/Animancer 중심이므로 전체 DOTS 전환은 하지 않는다. 대신 다음 원칙만 적용한다.

- 데이터는 가능한 한 불변 정의와 런타임 상태로 나눈다.
- 판정, 피해 계산, 리액션 결정은 상태 없는 서비스 또는 작은 시스템으로 분리한다.
- MonoBehaviour는 참조 수집, 상태 전환, Unity API 호출에 집중한다.

참고:

- https://docs.unity.cn/Packages/com.unity.entities%400.3/manual/ecs_core.html
- https://unity.com/dots

### AAA 액션 전투의 공통 요구

Spider-Man, God of War 같은 액션 게임은 세부 구현이 공개되어 있지 않지만, 공개 인터뷰와 발표에서 공통적으로 확인되는 방향은 다음과 같다.

- 하나의 공격이 애니메이션, 이동 보정, 판정, 반응, 카메라, 피드백을 함께 오케스트레이션한다.
- 플레이어 입력 반응성과 적 반응의 설득력이 중요하다.
- 데이터 기반 저작과 검증 툴이 전투 품질을 좌우한다.
- 적 타입/상태/방어 분류에 따라 같은 공격도 다른 결과를 낸다.

참고:

- https://gameinformer.com/b/features/archive/2018/04/11/how-combat-works-in-spider-man.aspx
- https://blog.playstation.com/2022/10/04/game-developers-explain-what-makes-god-of-war-2018s-combat-tick/

---

## 3. 현재 구조 요약

```
PlayerAttackState
    ├── PlayerCombat.Execute*()
    ├── ActorAnimator.PlayMotion()
    ├── MotionWarpController.SetTarget()
    └── MotionEventExecutor
            ├── BeginCollisionEvent → PlayerCombat.SetEnableCollision(true)
            ├── ComboWindowEvent → PlayerCombat.OpenComboWindow()
            └── MotionEvent_MotionWarp → PlayerCombat.BeginMotionWarp()

PlayerCombat.Update()
    ├── PerformHitDetection()
    ├── PlayerCombatStateTracker.Tick()
    └── UpdateBreakInteractionTarget()

IDamageable.TakeDamage(AttackData)
    ├── PlayerActor.TakeDamage()
    │       ├── Guard / Parry / PerfectDodge
    │       ├── HP 감소
    │       ├── UI / Camera / VFX
    │       └── State 전환
    └── MonsterActor.TakeDamage()
            ├── Guard
            ├── 공격력 / 방어율 / Break 배율
            ├── HP 감소
            ├── Poise / Break / AI Memory
            ├── UI / VFX
            └── State 전환 / 사망 / 드랍 / 합류
```

### 현재 장점

| 장점 | 설명 |
|------|------|
| MotionSet 기반 타이밍 | 판정, 콤보 창, 워프, 연출 이벤트를 애니메이션 타임라인에서 제어한다 |
| ScriptableObject 데이터 | 공격 수치와 몬스터 스킬 선택 조건이 에셋으로 분리되어 있다 |
| 공통 `IDamageable` | 플레이어/몬스터/투사체가 같은 피해 진입점을 사용할 수 있다 |
| 상태 머신과 KCC 결합 | 공격/피격/가드/이동의 물리 동작을 상태별로 제어한다 |
| Poise/Break 기반 | 경직 저항과 특수공격 기회가 이미 분리되어 있다 |

### 현재 구조적 문제

| 문제 | 구체 사례 |
|------|----------|
| 책임 집중 | `PlayerCombat`이 공격 선택, 판정, 전투 상태, 브레이크 타겟 탐색, 피드백을 모두 담당 |
| 피해 흐름 직접 결합 | `TakeDamage()` 안에서 HP, UI, AI, 상태 전환, VFX가 한 번에 처리됨 |
| 결과 객체 부재 | 방어 성공, 패리, 크리티컬, Poise Break, Break Expose 같은 결과가 명시적 객체로 남지 않음 |
| 공격 데이터 혼합 | `AttackData`가 정의 데이터와 런타임 히트 정보를 동시에 담음 |
| MotionEvent 직접 의존 | 이벤트가 `PlayerCombat`, `EnemyCombat`을 직접 찾아 호출함 |
| 검증 부족 | `hitPhaseIndex`, MotionSet 존재, 텔레그래프 설정 불일치를 사전에 잡기 어려움 |

---

## 4. 목표 아키텍처

### 4.1 계층 구조

```
입력 / AI / BT
    │
    ▼
CombatActionRunner
    ├── CombatActionDefinition
    ├── CombatActionInstance
    ├── MotionSet 재생
    └── Timeline Event 수신
            │
            ▼
CombatHitDetector
    ├── MeleeOverlap
    ├── ProjectileHit
    ├── AoeHit
    └── Hit 중복 관리
            │
            ▼
CombatResolutionPipeline
    ├── DefenseResolver
    ├── DamageResolver
    ├── ResourceApplier
    ├── ReactionResolver
    └── CombatResult 생성
            │
            ├── StateTransitionApplier
            └── CombatFeedbackDispatcher
                    ├── HitStop
                    ├── Camera
                    ├── VFX / SFX
                    ├── DamageFloater
                    └── VitalOrb / Quest / AI Memory
```

### 4.2 주요 책임

| 모듈 | 책임 |
|------|------|
| `CombatActionRunner` | 공격/스킬 하나의 실행 생명주기 관리. MotionSet 재생, 현재 phase, 취소/완료 처리 |
| `CombatHitDetector` | 근접/투사체/AOE 판정, hit target 중복 관리 |
| `DefenseResolver` | 가드, 퍼펙트 가드, 패리, 퍼펙트 도지, Unblockable 판정 |
| `DamageResolver` | 공격력, 방어율, 크리티컬, 노출 배율, 난이도 배율 계산 |
| `ResourceApplier` | HP, Poise, BreakGauge, SkillGauge 등 수치 적용 |
| `ReactionResolver` | `AttackReactionType`, Poise Break, 현재 상태 정책을 실제 상태 전환 결정으로 변환 |
| `CombatFeedbackDispatcher` | 전투 결과를 바탕으로 HitStop, Camera, VFX, SFX, UI, VitalOrb 실행 |
| `CombatStateTracker` | `IsInCombat`, 위협 탐색, 전투 상태 변화 이벤트 |
| `CombatValidation` | 공격 데이터와 MotionSet 이벤트 불일치 검증 |

---

## 5. 데이터 모델 개선

### 5.1 현재 `AttackData` 문제

현재 `AttackData`는 다음 정보를 모두 담는다.

- SO에서 온 공격 수치
- 현재 히트 페이즈 수치
- 공격자 참조
- hitPoint / hitTarget
- reaction force
- criticalMultiplier
- defenseType
- grab / forced motion

이 구조는 단순하지만, 다음 상황에서 위험해진다.

- 멀티히트 중 phase별 값이 현재 객체에 덮어써진다.
- 잔류 공격, 투사체, AOE가 원본 공격 컨텍스트를 복사해야 한다.
- 피해 결과가 객체에 남지 않아 후속 피드백이 조건을 다시 계산한다.
- 방어/패리/회피 성공처럼 "피해는 없지만 전투 결과는 있는" 케이스 표현이 약하다.

### 5.2 목표 데이터

```csharp
public sealed class CombatActionDefinition
{
    public AnimKey animKey;
    public AttackKind attackKind;
    public AttackType attackType;
    public IReadOnlyList<HitPhaseDefinition> hitPhases;
    public PlayerInterruptAction interruptActions;
}

public sealed class CombatActionInstance
{
    public GameActor owner;
    public CombatActionDefinition definition;
    public int comboIndex;
    public int currentPhaseIndex;
    public float startedTime;
    public object sourceData;
}

public readonly struct HitContext
{
    public CombatActionInstance action;
    public IDamageable target;
    public Collider hitCollider;
    public Vector3 hitPoint;
    public Vector3 attackDirection;
    public int hitPhaseIndex;
}

public readonly struct CombatResult
{
    public HitContext hit;
    public DefenseResult defense;
    public DamageResult damage;
    public ReactionDecision reaction;
    public ResourceChangeSet resources;
}
```

### 5.3 마이그레이션 원칙

초기에는 `AttackData`를 삭제하지 않는다.

1. 신규 `HitContext`, `DamageResult`, `DefenseResult`, `CombatResult`를 추가한다.
2. `AttackData` → `HitContext` 변환 어댑터를 둔다.
3. 신규 코드부터 `CombatResult`를 사용한다.
4. `PlayerActor.TakeDamage(AttackData)` / `MonsterActor.TakeDamage(AttackData)`는 호환 진입점으로 유지한다.
5. 최종적으로 `IDamageable.TakeDamage(AttackData)`를 `ReceiveHit(HitContext)` 또는 `ApplyCombatResult(CombatResult)`로 대체한다.

---

## 6. 단계별 구현 계획

## Phase 0 — 기준선 고정

목표: 구조 변경 전에 현재 동작을 문서/검증으로 고정한다.

### 작업

1. `COMBAT_SYSTEM_GUIDE.md`를 현재 코드와 계속 동기화한다.
2. 대표 전투 시나리오 체크리스트를 만든다.
3. 최소한의 Play Mode 수동 검증 시나리오를 정한다.

### 검증 시나리오

| 시나리오 | 기대 결과 |
|----------|----------|
| 플레이어 약공 1타 | 몬스터 HP 감소, FX, DamageFloater, HitStop 발생 |
| 플레이어 멀티히트 | phase별 수치 적용, 같은 phase 중복 히트 방지 |
| 몬스터 근접 공격 | 플레이어 HP 감소, 피격 상태 전환 |
| 가드 성공 | 피해 처리 대신 가드 처리 |
| 공격 중 패리 | 몬스터 스턴, 플레이어 반격 창 |
| 퍼펙트 도지 | 피해 없음, VitalOrb/HitStop |
| Poise Break | 몬스터 Stun/Knockdown |
| Break Gauge 노출 | F 프롬프트, 특수공격 진입 |

### 완료 기준

- 변경 전후 비교 가능한 체크리스트가 있다.
- `COMBAT_SYSTEM_GUIDE.md`와 현재 코드 흐름이 불일치하지 않는다.

---

## Phase 1 — 피해 계산 분리

목표: `PlayerActor.TakeDamage()`와 `MonsterActor.TakeDamage()` 안의 피해량 계산을 `DamageResolver`로 분리한다.

### 신규 후보

```text
Assets/02.Scripts/GameActor/Combat/Resolution/DamageResolver.cs
Assets/02.Scripts/GameActor/Combat/Resolution/DamageResult.cs
Assets/02.Scripts/GameActor/Combat/Resolution/CombatHitContext.cs
```

### 책임

`DamageResolver`는 다음만 계산한다.

- 기본 damage
- 공격자 `Stats.AttackPower`
- 방어자 `Stats.Defense`
- `criticalMultiplier`
- Break 노출 중 `DamageTakenMultiplier`
- 최종 피해량
- DamageFloater 스타일 힌트

### 하지 않는 것

- HP 감소 직접 적용
- UI 표시
- 상태 전환
- VFX/SFX
- AI Memory 알림

### 예상 API

```csharp
public static class DamageResolver
{
    public static DamageResult Resolve(in HitContext context, AttackData legacyAttackData);
}

public readonly struct DamageResult
{
    public readonly float BaseDamage;
    public readonly float FinalDamage;
    public readonly bool IsCritical;
    public readonly FloatStyle FloaterStyle;
}
```

### 영향 파일

| 파일 | 변경 |
|------|------|
| `PlayerActor.cs` | 플레이어 최종 피해량 계산을 `DamageResolver`로 이동 |
| `MonsterActor.cs` | 공격력/방어율/Break 배율 계산을 `DamageResolver`로 이동 |
| `CombatData.cs` | 필요 시 legacy 변환 헬퍼 추가 |

### 완료 기준

- 피해량 결과가 기존과 동일하다.
- `TakeDamage()`는 계산식이 아니라 결과 적용 중심으로 단순해진다.

---

## Phase 2 — 전투 피드백 분리

목표: HitStop, 카메라, VFX, DamageFloater, VitalOrb 호출을 `CombatFeedbackDispatcher`로 모은다.

### 신규 후보

```text
Assets/02.Scripts/GameActor/Combat/Feedback/CombatFeedbackDispatcher.cs
Assets/02.Scripts/GameActor/Combat/Feedback/CombatFeedbackContext.cs
Assets/02.Scripts/GameActor/Combat/Feedback/CombatFeedbackProfile.cs
```

### 책임

- `CombatResult` 또는 legacy `AttackData`를 받아 피드백 실행
- HitStop 강도 결정
- Camera shake / punch / FOV effect 실행
- hitParticleName 또는 reaction 기반 VFX 실행
- DamageFloater 표시
- VitalOrb 트리거
- KillCam 분기 보존

### 현재 직접 호출 제거 대상

| 현재 위치 | 제거/위임 대상 |
|-----------|---------------|
| `PlayerCombat.ApplyHitFeedback()` | HitStop, Camera, VitalOrb |
| `PlayerActor.OnDamaged()` | CameraShake, ShowFX, DamageFloater 일부 |
| `MonsterActor.OnDamaged()` | ShowFX, ColorChanger 일부 |
| `PlayerActor.OnParrySuccess()` | 일부는 Defense 피드백으로 이동 |

### 완료 기준

- 전투 피드백 정책을 한 곳에서 조정할 수 있다.
- 가드/패리/도지/일반 히트/킬 히트 피드백 분기가 명시적이다.
- `TakeDamage()`에서 카메라와 VFX 직접 호출이 줄어든다.

---

## Phase 3 — 방어 판정 중앙화

목표: 가드, 퍼펙트 가드, 패리, 퍼펙트 도지, Unblockable 처리를 `DefenseResolver`로 분리한다.

### 신규 후보

```text
Assets/02.Scripts/GameActor/Combat/Resolution/DefenseResolver.cs
Assets/02.Scripts/GameActor/Combat/Resolution/DefenseResult.cs
```

### 예상 결과 타입

```csharp
public enum DefenseOutcome
{
    None,
    Guarded,
    PerfectGuard,
    GuardBreak,
    Parried,
    PerfectDodged,
    Invincible,
    UnblockableHit,
}
```

### 처리 원칙

1. 방어 판정은 피해 계산보다 먼저 실행한다.
2. `DefenseResult`가 피해 적용 여부를 결정한다.
3. `PlayerGuardState`, `PlayerDodgeState`, `PlayerAttackState`의 상태 특수성은 resolver에 필요한 query API로 노출한다.
4. 상태 전환은 resolver가 직접 하지 않고 결과만 반환한다.

### 영향 파일

| 파일 | 변경 |
|------|------|
| `PlayerActor.cs` | `TakeDamage()`의 가드/패리/퍼펙트도지 우선순위를 `DefenseResolver` 호출로 대체 |
| `PlayerCombat.cs` | 가드 카운터 창, 패리 창 API는 유지하되 판정 호출부 단순화 |
| `PlayerGuardState.cs` | 방어 결과 적용 메서드 정리 |
| `MonsterActor.cs` | 몬스터 가드도 같은 resolver 경로로 이동 검토 |

### 주의 사항

- `AttackDefenseType.Unblockable`은 현재 데이터 분류와 Danger Ring 표현에 쓰인다. 실제 가드 불가 판정을 이 단계에서 명확히 연결한다.
- 패리 성공 시 `MonsterActor.OnParried()`로 이어지는 흐름은 유지하되, 호출 주체를 `DefenseResultApplier`로 옮긴다.

---

## Phase 4 — 리액션 결정 분리

목표: `PlayerActor.OnDamaged()`와 `MonsterActor.OnDamaged()`에 흩어진 피격 상태 전환 규칙을 `ReactionResolver`로 분리한다.

### 신규 후보

```text
Assets/02.Scripts/GameActor/Combat/Resolution/ReactionResolver.cs
Assets/02.Scripts/GameActor/Combat/Resolution/ReactionDecision.cs
Assets/02.Scripts/GameActor/Combat/Resolution/StateTransitionApplier.cs
```

### 책임

`ReactionResolver`:

- `AttackReactionType`
- `forceReaction`
- Poise Break 여부
- Break 노출 여부
- 현재 상태의 `SuppressesHitReaction`
- 현재 상태의 `CanPlayHitReaction(attackData)`
- 몬스터 등급/보스 정책

위 정보를 기반으로 다음 결정을 반환한다.

```csharp
public readonly struct ReactionDecision
{
    public readonly bool ShouldApplyForce;
    public readonly bool ShouldEnterState;
    public readonly CombatReactionState TargetState;
    public readonly Vector3 Impulse;
    public readonly float DurationOverride;
}
```

`StateTransitionApplier`:

- `CombatReactionState`를 실제 `PlayerHitState`, `EnemyStunState`, `EnemyKnockdownState` 등으로 변환한다.

### 완료 기준

- 플레이어/몬스터 피격 상태 전환 정책을 한 테이블에서 읽을 수 있다.
- 새 `AttackReactionType` 추가 시 수정 위치가 줄어든다.
- Poise와 Break 역할이 코드상으로도 분리된다.

---

## Phase 5 — CombatStateTracker 분리

목표: `PlayerCombat`에서 `IsInCombat`, 위협 탐색, `OnChangeCombatState`를 분리한다.

### 신규 후보

```text
Assets/02.Scripts/GameActor/Component/Player/PlayerCombatStateTracker.cs
```

### 이동 대상

| 현재 `PlayerCombat` 책임 | 이동 |
|-------------------------|------|
| `_combatStateDuration` | `PlayerCombatStateTracker` |
| `_threatDetectionRange` | `PlayerCombatStateTracker` |
| `_threatCheckInterval` | `PlayerCombatStateTracker` |
| `IsInCombat` | `PlayerCombatStateTracker.IsInCombat` |
| `RefreshCombatState()` | `NotifyCombatEvent()` |
| `ForceExitCombat()` | `ForceExitCombat()` |
| `OnChangeCombatState` | `PlayerCombatStateTracker.OnChangeCombatState` |

### 영향 파일

| 파일 | 변경 |
|------|------|
| `PlayerCombat.cs` | 전투 상태 관련 필드/Update 제거 |
| `PlayerActor.cs` | `IsInCombat` provider 참조 갱신 |
| `PlayerCombatWeaponStateController.cs` | `PlayerCombat.OnChangeCombatState` 대신 tracker 이벤트 구독 |
| `UI_GamePlay` 계열 | 전투 상태 참조 갱신 가능 |

### 완료 기준

- `PlayerCombat`은 공격 데이터와 판정 중심으로 축소된다.
- 전투 상태는 공격 시스템 없이도 독립 테스트 가능하다.

---

## Phase 6 — HitDetector 분리

목표: `PlayerCombat.PerformHitDetection()`과 `EnemyCombat.CheckMeleeAttackHit()`의 중복 구조를 `CombatHitDetector`로 통합한다.

### 신규 후보

```text
Assets/02.Scripts/GameActor/Combat/Detection/CombatHitDetector.cs
Assets/02.Scripts/GameActor/Combat/Detection/MeleeHitShape.cs
Assets/02.Scripts/GameActor/Combat/Detection/HitTargetRegistry.cs
```

### 책임

- OverlapSphere 판정
- 전방 각도 판정
- 높이 판정
- 자신/자식 제외
- `IDamageable.CanTakeDamage()` 필터
- 같은 phase 또는 같은 active window 중복 히트 방지
- `HitContext` 생성

### 완료 기준

- 플레이어와 몬스터 근접 판정 코드가 같은 path를 사용한다.
- 신규 AOE/잔류공격/소환 공격이 중복 판정 로직을 재사용할 수 있다.

---

## Phase 7 — CombatActionRunner 도입

목표: 공격/스킬 하나를 실행 단위로 관리하는 runner를 도입한다.

### 신규 후보

```text
Assets/02.Scripts/GameActor/Combat/Action/CombatActionRunner.cs
Assets/02.Scripts/GameActor/Combat/Action/CombatActionDefinition.cs
Assets/02.Scripts/GameActor/Combat/Action/CombatActionInstance.cs
Assets/02.Scripts/GameActor/Combat/Action/CombatTimelineEvent.cs
```

### 역할

- `PlayerAttackState`와 `EnemyAttackState`가 `CombatActionRunner.StartAction()`을 호출한다.
- Runner가 현재 action instance와 phase를 가진다.
- MotionEvent는 `CombatTimelineEvent`를 runner에 전달한다.
- Runner가 detector, resolver pipeline, feedback dispatcher를 순서대로 호출한다.

### MotionEvent 변경 방향

현재:

```text
BeginCollisionEvent
    ├── PlayerCombat.SetHitPhaseIndex()
    └── EnemyCombat.SetHitPhaseIndex()
```

목표:

```text
BeginCollisionEvent
    └── CombatActionRunner.HandleTimelineEvent(
            CombatTimelineEventType.BeginCollision,
            hitPhaseIndex)
```

### 완료 기준

- MotionEvent가 `PlayerCombat` / `EnemyCombat` 구현 세부를 몰라도 된다.
- 잔류 공격, 투사체, AOE가 같은 timeline event 모델을 재사용한다.

---

## Phase 8 — 데이터 검증 툴

목표: 전투 데이터와 MotionSet 불일치를 에디터에서 사전에 잡는다.

### 신규 후보

```text
Assets/02.Scripts/Tool/Editor/Combat/CombatDataValidatorWindow.cs
Assets/02.Scripts/Tool/Editor/Combat/CombatDataValidator.cs
```

### 검증 항목

| 항목 | 오류 조건 |
|------|----------|
| MotionSet 존재 | `AttackInfoBase.animKey`에 해당하는 MotionSet이 없음 |
| Collision 이벤트 | Melee 공격인데 `BeginCollisionEvent`가 없음 |
| hitPhaseIndex 범위 | `BeginCollisionEvent.hitPhaseIndex >= hitPhases.Count` |
| 멀티히트 누락 | `hitPhases.Count > 1`인데 이벤트가 0번만 있음 |
| Telegraph 설정 | `useMotionEventTelegraph == true`인데 `TelegraphEvent` 없음 |
| Telegraph 위치 히트 | `useTelegraphPositionForHit == true`인데 위치 예약 이벤트 없음 |
| Danger Ring | `useDangerRing == true`인데 Collision/Projectile 이벤트 타이밍 산출 실패 |
| DefenseType | `Unblockable`인데 Danger Ring/텔레그래프 표현 누락 |
| Player 폴백 | `counterAttack`, `entryAttack`, `swapSpecialAttack` 폴백 후보가 모두 없음 |
| Break | `forceBreakExpose` 또는 `breakDamage > 0`인데 타겟 몬스터에 `breakGaugeData` 없음 |

### 완료 기준

- 공격 데이터 에셋 전체를 한 번에 검증할 수 있다.
- 오류/경고가 에셋 경로와 animKey, phaseIndex를 포함해 출력된다.
- Balance Designer와 연동 가능한 CSV 또는 리포트 출력이 가능하다.

---

## 7. 최종 목표 구조

```
PlayerAttackState / EnemyAttackState
    └── CombatActionRunner.StartAction(definition)
            ├── PlayMotion(animKey)
            ├── HandleTimelineEvent()
            │       ├── BeginCollision
            │       ├── EndCollision
            │       ├── ComboWindow
            │       ├── MotionWarp
            │       └── SpecialHit
            │
            └── CombatHitDetector.Detect()
                    └── CombatResolutionPipeline.Resolve()
                            ├── DefenseResolver
                            ├── DamageResolver
                            ├── ResourceApplier
                            ├── ReactionResolver
                            ├── StateTransitionApplier
                            └── CombatFeedbackDispatcher
```

`PlayerCombat`과 `EnemyCombat`의 최종 역할은 다음처럼 축소한다.

| 클래스 | 최종 역할 |
|--------|----------|
| `PlayerCombat` | 플레이어 공격 데이터 선택, 콤보 상태, 스킬/차지/특수공격 선택 |
| `EnemyCombat` | 몬스터 스킬 선택, 쿨다운, 스킬 타겟 캐시 |
| `CombatActionRunner` | 공통 공격 실행 생명주기 |
| `CombatHitDetector` | 공통 판정 |
| `CombatResolutionPipeline` | 공통 전투 결과 계산 |
| `CombatFeedbackDispatcher` | 공통 전투 피드백 |

---

## 8. 리스크와 대응

| 리스크 | 대응 |
|--------|------|
| 한 번에 바꾸면 전투가 깨짐 | Phase 1~3은 legacy `AttackData`와 공존하는 어댑터 방식으로 진행 |
| 추상화가 과해져 1인 개발 속도가 떨어짐 | 새 계층은 실제 중복/복잡도가 있는 피해/피드백/방어/리액션부터만 도입 |
| Unity 상태 전환은 결국 구체 State 생성이 필요 | `ReactionResolver`는 결정만 하고 `StateTransitionApplier`가 Unity/상태머신 의존 처리 |
| MotionEvent 변경이 기존 데이터와 충돌 | 기존 이벤트 직접 호출 경로 유지 후 runner 경로를 옵션으로 병행 |
| 검증 툴 작성 비용 | Phase 8로 미루되, hitPhaseIndex 범위 검증만 우선 구현 가능 |
| GAS식 구조를 과도하게 모방 | 네트워크/복제/예측은 범위 밖. 싱글플레이 액션에 필요한 실행/결과/피드백 분리만 차용 |

---

## 9. 권장 구현 순서

| 순서 | Phase | 이유 |
|------|-------|------|
| 1 | Phase 1 `DamageResolver` | 가장 낮은 위험으로 `TakeDamage()` 복잡도를 줄인다 |
| 2 | Phase 2 `CombatFeedbackDispatcher` | 타격감 튜닝 위치를 중앙화한다 |
| 3 | Phase 3 `DefenseResolver` | 가드/패리/도지/Unblockable 정책을 명확히 한다 |
| 4 | Phase 4 `ReactionResolver` | 새 리액션/보스 정책 추가 비용을 줄인다 |
| 5 | Phase 5 `CombatStateTracker` | `PlayerCombat` 크기를 줄이고 발도/HUD 연동을 안정화한다 |
| 6 | Phase 6 `CombatHitDetector` | 플레이어/몬스터/잔류공격 판정 중복을 줄인다 |
| 7 | Phase 7 `CombatActionRunner` | 가장 큰 구조 변경. 앞 단계가 안정된 뒤 진행 |
| 8 | Phase 8 검증 툴 | 데이터 규모가 커지기 전에 필수화 |

---

## 10. 구현 체크리스트

### Phase 1

- [x] `DamageResult` 구조체 추가
- [ ] `HitContext` 또는 legacy context 추가
- [x] `DamageResolver.Resolve()` 추가
- [x] `PlayerActor.TakeDamage()` 피해 계산 위임
- [x] `MonsterActor.TakeDamage()` 피해 계산 위임
- [x] 기존 피해량과 동일한지 빌드 기준 검증

### Phase 2

- [x] `CombatFeedbackContext` 추가
- [x] `CombatFeedbackDispatcher` 추가
- [x] `PlayerCombat.ApplyHitFeedback()` 이관
- [x] `PlayerActor.OnDamaged()`의 카메라/VFX 일부 이관
- [x] `MonsterActor.OnDamaged()`의 VFX 일부 이관
- [x] KillCam, PlayerGuard 히트스톱 보호 분기 빌드 기준 검증

### Phase 3

- [x] `DefenseOutcome`, `DefenseResult` 추가
- [x] `DefenseResolver.Resolve()` 추가
- [x] `PlayerActor.TakeDamage()` 가드/패리/도지 분기 이관
- [x] `AttackDefenseType.Unblockable` 결과 타입 연결
- [x] `PlayerGuardState` 결과 적용 경로 유지

### Phase 4

- [x] `ReactionDecision` 추가
- [x] `ReactionResolver.Resolve()` 추가
- [x] 플레이어/몬스터 리액션 결정 분리
- [x] 상태 전환 적용 메서드 추가
- [x] Poise Break / forceReaction / SuppressesHitReaction 빌드 기준 검증

### Phase 5

- [x] `PlayerCombatStateTracker` 추가
- [x] `PlayerCombat`의 전투 상태 런타임 처리 이동
- [x] `PlayerActor.IsInCombat` 기존 `PlayerCombat` 프록시 유지
- [x] `PlayerCombatWeaponStateController` 기존 구독 API 유지
- [x] HUD 전투 상태 표시 빌드 기준 검증

### Phase 6

- [x] `MeleeHitShape` 추가
- [x] 기존 `_hitTargets`를 중복 히트 레지스트리로 유지
- [x] 플레이어 근접 판정 이관
- [x] 몬스터 근접 판정 이관
- [x] 멀티히트/중복 히트 빌드 기준 검증

### Phase 7

- [x] `CombatActionDefinition` 추가
- [x] `CombatActionInstance` 추가
- [x] `CombatActionRunner` 추가
- [x] MotionEvent → runner timeline event 병행 경로 추가
- [x] PlayerCombat 공격 시작 이벤트를 runner 병행 경로로 연결
- [x] EnemyCombat 현재 스킬 시작 이벤트를 runner 병행 경로로 연결

### Phase 8

- [x] `CombatDataValidator` 순수 검증 로직 추가
- [x] `CombatDataValidatorWindow` 추가
- [x] PlayerAttackDataSO 전체 기본 검증
- [x] AbilitySetSO 전체 기본 검증
- [ ] MotionSet 이벤트와 hitPhases 매칭 검증
- [x] Markdown 리포트 출력

---

## 11. 완료 판단 기준

### 1차 구현 완료 기준

2026-06-03 구현으로 다음 항목은 완료된 상태다.

- `PlayerCombat`과 `EnemyCombat`에서 직접 피해 계산과 피드백 실행이 제거되어 있다.
- 가드/패리/퍼펙트도지/Unblockable이 `DefenseResult`로 명시된다.
- Poise/Break/HitReaction 상태 전환이 `ReactionDecision`으로 설명 가능하다.
- MotionEvent가 `PlayerCombat` / `EnemyCombat` 직접 호출이 아니라 runner timeline event로 전달될 수 있다.
- 공격 데이터 검증 툴이 `PlayerAttackDataSO`, `AbilitySetSO`의 기본 오류/경고를 사전에 잡고 Markdown 리포트를 저장할 수 있다.

### 후속 완료 목표

다음 항목은 최종 구조로 가기 위한 후속 작업이다.

- 플레이어/몬스터의 피해 처리 흐름이 legacy `AttackData` 대신 공통 `CombatResult` 또는 동등한 결과 객체를 중심으로 동작한다.
- `CombatActionRunner`가 단순 병행 기록이 아니라 실제 판정/해결 pipeline의 주도권을 가진다.
- MotionEvent가 `PlayerCombat` / `EnemyCombat` legacy API를 직접 호출하지 않고 runner timeline event만 전달한다.
- 전투 데이터 검증 툴이 `hitPhaseIndex`와 MotionSet 이벤트 불일치를 사전에 잡는다.
