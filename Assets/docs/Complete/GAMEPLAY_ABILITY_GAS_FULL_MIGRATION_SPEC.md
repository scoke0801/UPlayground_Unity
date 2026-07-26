# Gameplay Ability GAS 기반 완전 마이그레이션 스펙

> 문서 버전: 0.1 Draft  
> 작성일: 2026-07-19  
> 대상 버전: Unity 6 (6000.0.60f1), 싱글플레이, URP  
> 분류: 설계서(미구현 TODO)  
> 선행 문서: `../Complete/GAMEPLAY_ABILITY_SYSTEM_SPEC.md`, `../Complete/PASSIVE_ABILITY_SYSTEM_SPEC.md`, `../guide/STAT_SYSTEM_GUIDE.md`  
> 완료 후 처리: 구현과 전체 데이터 전환이 끝나면 `Assets/docs/Complete/`로 이동하고 기존 Stat 가이드를 폐기 또는 리다이렉트한다.

> **2026-07-26 갱신 — Cue 관련 항목 전면 폐기.**
> 본 문서가 제안하는 `GameplayCueRouter`, `IGameplayCuePort`, `UPlayGroundCueAdapter`와
> Cue 관련 수용 기준은 채택하지 않는다. 기존 Cue 계층(`AbilityCueDefinition`,
> `GameplayCueDispatcher`)도 같은 날 제거했다. 근거와 대체 경로는
> `GAMEPLAY_ABILITY_SYSTEM_SPEC.md` §20을 따른다.

---

## 1. 목적

현재 UPlayground Ability 시스템은 활성화 조건, 비용, 쿨다운, 실행 수명주기, GameplayEffect, GameplayTag를 제공하지만 런타임 권위가 여러 컴포넌트에 분산되어 있다.

```text
GameActor
├─ ActorAbilitySystem          Ability 실행·쿨다운
├─ GameplayEffectController    Effect 수명·스택
├─ GameplayTagContainer        런타임 Tag
├─ ActorStatContainer          계산형 Stat
├─ PlayerActor/MonsterActor    현재 Health
├─ PoiseStat                   현재 Poise
└─ PlayerSkillGauge            현재 UltimateEnergy와 레거시 슬롯 쿨다운
```

이 분산 구조에서는 다음 문제가 생긴다.

- 하나의 전투 값에 정의, 현재값, 수정자, 저장 경로가 서로 다른 컴포넌트에 존재한다.
- 피해와 회복이 GameplayEffect를 거치지 않고 Actor 필드를 직접 변경한다.
- `GameplayEffectSO`가 고정 수치만 가지므로 Source/Target Attribute, 타격 정보, 런타임 전달값을 활용한 계산이 어렵다.
- Ability 실행의 다단 비동기 흐름을 상태 머신과 호출 코드가 직접 조정한다.
- 실행 중 Ability, Task, Effect, Tag, Attribute 변화의 원인을 한 화면에서 추적할 수 없다.
- `ActorStatContainer`, `PlayerSkillGauge`, HP 필드, `PoiseStat`이 각각 별도 저장·UI 이벤트를 제공하여 전환 코드가 중복된다.

본 스펙은 위 구조를 Unreal Gameplay Ability System의 핵심 개념에 대응하는 Unity용 구조로 완전히 이전한다.

핵심 목표:

1. `AbilitySystemComponent`를 액터 Ability 상태의 단일 집합 루트로 만든다.
2. Health, Poise, UltimateEnergy와 모든 계산형 Stat을 통합 Attribute Runtime으로 이전한다.
3. 모든 런타임 Effect 적용은 불변 정의와 가변 `GameplayEffectSpec`을 분리해 처리한다.
4. 다단 실행을 부모 Ability 수명에 종속되는 `AbilityTask`로 표현한다.
5. Ability, Task, Effect, Tag, Attribute, 계산 이력을 읽기 전용 런타임 디버거로 관찰한다.
6. 기존 Stat/Health/Poise/Gauge 직접 권위를 제거하고 GAS 기반 경로만 남긴다.
7. KCC 상태 머신, MotionSet, 기존 전투 리액션의 전문 책임은 유지하되 Ability Task와 Effect 계산의 어댑터로 연결한다.

---

## 2. “GAS 기반 완전 마이그레이션”의 정의

본 문서에서 GAS 기반은 Unreal 코드를 복제한다는 뜻이 아니다. 다음 설계 원칙을 만족하는 UPlayground용 구현을 뜻한다.

| 원칙 | 완료 조건 |
|------|----------|
| 단일 집합 루트 | 액터의 Ability, Effect, Tag, Attribute 상태가 `AbilitySystemComponent`에서 조회된다. |
| 정의/Spec/활성 인스턴스 분리 | ScriptableObject 정의, 적용 전 가변 Spec, 적용 후 Active Instance가 서로 다른 타입이다. |
| Attribute 단일 권위 | Health, Poise, UltimateEnergy, 전투 Stat의 현재값과 계산값을 Attribute Runtime만 소유한다. |
| Effect 기반 변경 | 초기화·세이브 복원을 제외한 Attribute 변경은 GameplayEffect 또는 명시적 Attribute Transaction을 거친다. |
| Task 기반 실행 | 여러 프레임에 걸친 Ability 작업은 부모 실행에 종속된 AbilityTask로 표현한다. |
| Tag 기반 상호작용 | 활성화, 차단, 취소, Effect 요구 조건과 GameplayEvent 라우팅이 Tag Query를 사용한다. |
| 관찰 가능성 | 활성 상태와 최근 계산 원인을 런타임 디버거에서 확인할 수 있다. |
| 레거시 제거 | `ActorStatContainer`, HP 직접 필드, `PlayerSkillGauge` 자원 권위, `StatModifier` 직접 적용 경로가 남지 않는다. |

단순히 기존 타입 이름을 GAS 용어로 바꾸거나 `ActorStatContainer`를 `AttributeSet`으로 개명하는 것은 완료로 인정하지 않는다.

---

## 3. 범위와 비범위

### 3.1 범위

- 실행 중 Ability/Task/Effect/Tag/Attribute 런타임 디버거
- Ability Task 수명주기와 프로젝트 Task 어댑터
- 통합 Attribute Set 정의·런타임·이벤트·저장
- `GameplayEffectSpec`, Effect Context, Magnitude Calculation, Execution Calculation
- `ActorAbilitySystem`, `GameplayEffectController`, `GameplayTagContainer`, `ActorStatContainer`의 집합 루트 통합
- 플레이어·몬스터 Health 현재값 이전
- Poise 현재값과 최대값 이전
- UltimateEnergy와 관련 저장/UI 이전
- 장비, 성장, 패시브, 소모품, 피해, 회복의 Effect 기반 전환
- Ability/Effect/Attribute 데이터 마이그레이션 에디터
- 기존 소비 코드의 읽기 계약 전환
- 저장 데이터 버전 마이그레이션
- 기존 레거시 타입과 필드의 최종 삭제

### 3.2 비범위

- KCC 이동/회전 계산 자체를 Ability System으로 이전
- MotionSet 타임라인과 MotionEvent를 범용 노드 그래프로 대체
- 피격 상태, Guard, Dodge, Knockdown 등 상태 머신의 제거
- 첫 구현 단계의 네트워크 복제, 서버 권한, 클라이언트 예측
- Unreal Blueprint와 동등한 범용 비주얼 스크립팅 에디터
- Unity Job System 또는 백그라운드 스레드에서 Ability Task 실행

네트워크는 현재 싱글플레이 범위에서 구현하지 않는다. 다만 Handle, Spec, Context, Snapshot은 향후 직렬화와 예측을 방해하지 않는 구조로 만든다.

---

## 4. 현재 구현 조사 결과

### 4.1 현재 권위 분산

| 값/상태 | 현재 정의 | 현재 런타임 권위 | 주요 변경 경로 |
|---------|-----------|------------------|----------------|
| 계산형 Stat | `ActorStatSO` | `ActorStatContainer` | `StatModifier`, 장비, GameplayEffect |
| Player Health | `ActorStatSO.MaxHealth` 일부 | `PlayerActor._maxHealth`, `_currentHealth` | `PlayerActor.Combat`, 소모품, 장비 재계산 |
| Monster Health | `ActorStatSO.MaxHealth` | `MonsterActor._maxHealth`, `_currentHealth` | `MonsterActor.TakeDamage`, Heal, SetHealth |
| Poise 최대값 | `ActorStatSO` | `ActorStatContainer` | Stat Modifier |
| Poise 현재값 | 없음 | `PoiseStat._currentPoise` | 피격, 시간 회복 |
| Break | Break 전용 SO | `MonsterBreakGauge` | 피격·노출·특수 공격 |
| UltimateEnergy | `PlayerSkillGauge` 직렬화 필드 | `PlayerSkillGauge._currentGauge` | 공격 적중, Ability 비용 |
| Ability 쿨다운 | `GameplayAbilitySO` | `AbilityCooldownRuntime`와 일부 `PlayerSkillGauge` | Commit, 캐릭터 교체 저장 |
| Effect | `GameplayEffectSO` | `GameplayEffectController` | Ability, Passive |
| Tag | Registry/enum | `GameplayTagContainer` | 상태, Ability, Effect |
| 원소 속성 | Actor Definition/Effect | `GameActor` override 목록 | GameplayEffect |

### 4.2 현재 직접 접근 문제

- `DamageResolver`가 `actor.Stats.AttackPower`, `Stats.Defense`를 직접 읽는다.
- `PlayerActor`와 `MonsterActor`가 최종 피해를 받은 뒤 `_currentHealth`를 직접 차감한다.
- `Heal`, `HealPercent`, `SetHealth`, `Respawn`이 HP 필드를 직접 변경한다.
- `PoiseStat`이 자체 Update에서 회복과 Break 상태를 관리한다.
- `PlayerSkillGauge`가 UltimateEnergy, 공격별 충전표, 슬롯 쿨다운을 함께 관리한다.
- 장비 변경은 `Stats.RemoveModifiersBySource`와 `Stats.AddModifier`를 직접 호출한 뒤 HP 비율을 수동 보존한다.
- `GameplayEffectController`의 Resource Operation은 Health와 UltimateEnergy를 프로젝트 Port로 우회 변경한다.
- UI가 Actor별 HP 이벤트, Gauge 이벤트, Effect Reader 등 여러 계약을 구독한다.

### 4.3 이미 재사용 가능한 기반

다음 구현은 폐기 대상이 아니라 새 구조로 흡수하거나 확장한다.

| 현재 기반 | 재사용 방향 |
|-----------|-------------|
| `AbilityExecutionHandle` | Ability Instance Handle로 유지 |
| `AbilityCooldownRuntime` | ASC 내부 Cooldown Store 또는 Cooldown Effect로 이전 |
| `AbilityEffectStackRuntime` | Active Effect Container의 정책 계산으로 유지 |
| `IAbilityClock` | Task/Effect/쿨다운 공용 시간 Port로 확장 |
| `IAbilityResourcePort`, `IAbilityStatPort` | 전환기 Adapter로만 사용 후 삭제 |
| `GameplayTagContainer`의 소유 Handle | ASC Tag Aggregator 내부로 흡수 |
| `GameplayEffectController`의 저장/스택/주기 처리 | Active Effect Container로 이전 |
| `AbilityDataValidator` | Attribute/Spec/Task 검증까지 확장 |
| `StatRuntimeMonitorWindow` | 통합 GAS Runtime Debugger로 대체 |
| `UPlayGroundMotionAbilityPayloadSO` | `PlayMotionAndWaitTask` 입력 데이터로 변환 |
| `UPlayGroundAbilityOwnerPorts` | UPlayground Runtime Adapter로 재구성 |

---

## 5. 목표 상위 아키텍처

```text
GameActor
└─ AbilitySystemComponent
   ├─ AbilitySpecContainer
   │  ├─ Granted Ability Spec
   │  └─ Active Ability Instance
   │     └─ AbilityTaskContainer
   ├─ ActiveGameplayEffectContainer
   │  ├─ GameplayEffectSpec
   │  └─ ActiveGameplayEffect
   ├─ GameplayTagAggregator
   ├─ AttributeSetRuntime
   │  ├─ VitalAttributeSet
   │  ├─ CombatAttributeSet
   │  ├─ MovementAttributeSet
   │  └─ AbilityResourceAttributeSet
   ├─ GameplayEventRouter
   ├─ GameplayCueRouter
   └─ AbilityDebugRecorder
```

외부 시스템은 ASC 내부 저장소를 직접 수정하지 않는다.

```text
Input / BT / Combo / GameplayEvent
                 │
                 ▼
       AbilitySystemComponent
       ├─ 활성화·Tag Query
       ├─ Cost/Cooldown EffectSpec
       └─ Ability Instance + Task
                 │
       ┌─────────┴───────────┐
       ▼                     ▼
UPlayground Task Adapter   EffectSpec Pipeline
├─ KCC 상태 요청          ├─ Context/Target
├─ MotionSet 실행         ├─ Attribute Capture
├─ 입력/이벤트 대기       ├─ Magnitude 계산
└─ Hit Event 발행         └─ Modifier/Execution
       │                     │
       └─────────┬───────────┘
                 ▼
      Attribute / Tag / Cue / Debug
```

### 5.1 집합 루트 규칙

`AbilitySystemComponent`는 다음 상태의 유일한 런타임 조회 지점이다.

- 부여된 Ability
- 활성 Ability와 실행 Handle
- 활성 Ability Task
- 활성 GameplayEffect
- 소유 GameplayTag
- Attribute Base/Current/Final 값
- 쿨다운
- 최근 GameplayEvent

`GameActor`는 호환 편의 프로퍼티를 잠시 제공할 수 있지만 최종적으로 다음 형태만 남긴다.

```csharp
// 신규 제안
public AbilitySystemComponent AbilitySystem { get; private set; }
```

다음 병렬 컴포넌트 프로퍼티는 최종 삭제한다.

```csharp
public ActorStatContainer Stats { get; }
public ActorAbilitySystem Abilities { get; }
public GameplayEffectController Effects { get; }
public GameplayTagContainer Tags { get; }
```

---

## 6. 모듈과 의존성 경계

### 6.1 목표 asmdef

```text
UPlayGround.Ability.Core
├─ Definition/
│  ├─ GameplayAbilitySO
│  ├─ GameplayEffectSO
│  ├─ AttributeSetDefinitionSO
│  ├─ AbilityTaskDefinitionSO
│  └─ TagQuery
├─ Runtime/
│  ├─ AbilitySystemRuntime
│  ├─ AbilitySpecContainer
│  ├─ AbilityTaskRuntime
│  ├─ ActiveGameplayEffectContainer
│  ├─ GameplayEffectSpec
│  ├─ AttributeSetRuntime
│  └─ GameplayTagAggregator
├─ Calculation/
│  ├─ MagnitudeCalculation
│  └─ GameplayEffectExecution
├─ Debug/
│  ├─ AbilityDebugSnapshot
│  └─ AbilityDebugEvent
└─ Ports/
   ├─ IAbilityClock
   ├─ IAbilityExecutionPort
   ├─ IGameplayEventPort
   └─ IGameplayCuePort

UPlayGround.Ability.UPlayGround
├─ GameActorAbilitySystemComponent
├─ MotionSetTaskAdapter
├─ KccStateTaskAdapter
├─ CombatHitEffectAdapter
├─ UPlayGroundAttributeBootstrap
└─ UPlayGroundCueAdapter

UPlayGround.Ability.Editor
├─ Ability Editor
├─ Attribute Set Editor
├─ EffectSpec Preview
├─ Runtime Debugger
└─ Migration/Validation
```

### 6.2 금지 의존

`UPlayGround.Ability.Core`는 다음 타입을 직접 참조하지 않는다.

- `GameActor`, `PlayerActor`, `MonsterActor`
- `PlayerCombat`, `EnemyCombat`, `DamageResolver`
- KCC와 구체 상태 클래스
- `MotionSetAsset`, `AnimKey`, `AbilityAttackInfo`
- `ActorStatSO`, `StatType`, `StatModifier`
- `PlayerSkillGauge`, `PoiseStat`, `MonsterBreakGauge`
- Manager, `Svc`, `ActorSvc`, `UISvc`
- UI, Camera 구현

프로젝트 타입 변환은 `UPlayGround.Ability.UPlayGround` Adapter가 담당한다.

---

## 7. 통합 Attribute Set

### 7.1 설계 원칙

통합 Attribute Set은 하나의 거대한 클래스만 뜻하지 않는다. 동일한 Attribute Runtime과 계산 규칙 아래 목적별 Set을 등록한다는 뜻이다.

```text
AbilitySystemComponent
└─ AttributeSetRuntime
   ├─ Vital
   │  ├─ Health
   │  ├─ MaxHealth
   │  ├─ HealthRegenRate
   │  ├─ Poise
   │  ├─ MaxPoise
   │  └─ PoiseRecoveryRate/Delay
   ├─ Combat
   │  ├─ AttackPower
   │  ├─ Defense
   │  ├─ CritRate
   │  ├─ CritMultiplier
   │  ├─ AttackSpeed
   │  └─ DamageTakenMultiplier
   ├─ Movement
   │  ├─ MoveSpeed
   │  ├─ DashDistance
   │  └─ InvincibleDuration
   └─ AbilityResource
      ├─ UltimateEnergy
      ├─ MaxUltimateEnergy
      ├─ Forte
      ├─ Concerto
      └─ SkillCharge
```

### 7.2 식별자

신규 `AttributeId`는 프로젝트 enum이 아닌 안정 문자열 ID를 감싼 값 타입으로 정의한다.

```text
Vital.Health
Vital.MaxHealth
Vital.Poise
Vital.MaxPoise
Combat.AttackPower
Combat.Defense
Movement.MoveSpeed
Resource.UltimateEnergy
Resource.MaxUltimateEnergy
Meta.IncomingDamage
Meta.IncomingHealing
Meta.IncomingPoiseDamage
```

규칙:

- 저장과 Effect 참조에는 문자열 안정 ID를 사용한다.
- 표시 이름과 로컬라이징 키는 Definition에 둔다.
- 이름 변경은 Alias 테이블과 데이터 마이그레이션 없이는 금지한다.
- Core는 UPlayground `StatType`을 알지 않는다.
- 코드 편의를 위한 생성 상수는 Registry에서 자동 생성할 수 있다.

### 7.3 신규 제안 타입

```csharp
// 개념 API. 구현 시 네임스페이스와 세부 타입을 확정한다.
public readonly struct AttributeId
{
    public string Value { get; }
}

public sealed class AttributeSetDefinitionSO : ScriptableObject
{
    public string setId;
    public List<GameplayAttributeDefinition> attributes;
}

public sealed class GameplayAttributeDefinition
{
    public AttributeId attributeId;
    public float defaultBaseValue;
    public AttributeClampPolicy clampPolicy;
    public AttributeId minAttribute;
    public AttributeId maxAttribute;
    public bool saveCurrentValue;
}

public readonly struct GameplayAttributeValue
{
    public float BaseValue { get; }
    public float CurrentValue { get; }
}
```

### 7.4 Base, Current, Modifier 의미

| 값 | 의미 | 변경 주체 |
|----|------|-----------|
| Base | 성장·레벨·영구 강화가 반영된 영구 기준값 | 초기화, 성장 적용, 세이브 복원 |
| Current | 활성 Effect Modifier를 평가한 계산 결과 또는 소모 가능한 현재 자원 | Attribute Runtime |
| Modifier | Active GameplayEffect가 제공하는 임시/무한 변경 | Effect Container |
| Meta Attribute | 한 번의 계산 입력을 전달하고 후처리 후 0으로 소비하는 값 | Execution Calculation |

계산형 Attribute:

```text
Final = Override가 있으면 우선순위가 가장 높은 Override
        아니면 (Base + ΣAdd) × (1 + ΣPercent) × ΠMultiply
```

소모형 Attribute인 Health, Poise, UltimateEnergy는 Base/Current 의미를 다음처럼 사용한다.

- Base: 저장 가능한 실제 현재 자원값
- Current: Base에 활성 Modifier가 적용된 조회값
- Max 값은 별도 Attribute로 관리한다.
- 비용, 피해, 회복은 Base를 Transaction으로 변경한다.
- 일시적으로 Current Health 자체를 배율 버프하는 Effect는 금지하고 MaxHealth 또는 피해 계산을 수정한다.

### 7.5 최대값 변경 정책

MaxHealth, MaxPoise, MaxUltimateEnergy 변경 시 각 Attribute Definition이 정책을 명시한다.

| 정책 | 동작 | 사용 예 |
|------|------|---------|
| Clamp | 현재값을 새 최대값 이하로 제한 | 일반 자원 |
| PreserveRatio | 기존 비율을 새 최대값에 적용 | 장비로 MaxHealth 변경 |
| PreserveAbsolute | 현재 절대값 유지 후 Clamp | 일시 최대치 버프 |
| FillOnIncrease | 최대값 증가분만 현재값에도 더함 | 특정 성장 보상 |
| Refill | 항상 최대값으로 채움 | 스폰 초기화 |

장비 변경에서 현재 수동으로 처리하는 “풀피 유지/비율 보존”은 `PreserveRatio` 정책으로 이동한다.

### 7.6 Attribute 변경 파이프라인

```text
Request
→ PreAttributeBaseChange
→ Base 변경
→ Modifier Aggregate 재평가
→ Clamp/Dependency 정책
→ PostAttributeChange
→ GameplayEvent/Cue/Debug Event
→ UI Reader 알림
```

불변식:

- 이벤트에는 OldBase, NewBase, OldCurrent, NewCurrent, SourceSpecHandle을 포함한다.
- 동일 Transaction 안의 여러 Attribute 변경은 원자적으로 커밋한다.
- UI는 중간 계산값이 아니라 Commit된 결과만 본다.
- 콜백 안에서 즉시 재귀 변경하지 않고 후속 Transaction을 큐에 넣는다.

### 7.7 현재 데이터 매핑

| 현재 | 목표 |
|------|------|
| `StatType.MaxHealth` | `Vital.MaxHealth` |
| `PlayerActor._currentHealth` | `Vital.Health` |
| `MonsterActor._currentHealth` | `Vital.Health` |
| `StatType.HealthRegenRate` | `Vital.HealthRegenRate` |
| `StatType.MaxPoise` | `Vital.MaxPoise` |
| `PoiseStat._currentPoise` | `Vital.Poise` |
| `StatType.PoiseRecoveryRate` | `Vital.PoiseRecoveryRate` |
| `StatType.PoiseRecoveryDelay` | `Vital.PoiseRecoveryDelay` |
| `StatType.AttackPower` | `Combat.AttackPower` |
| `StatType.Defense` | `Combat.Defense` |
| `StatType.CritRate` | `Combat.CritRate` |
| `StatType.CritMultiplier` | `Combat.CritMultiplier` |
| `StatType.AttackSpeed` | `Combat.AttackSpeed` |
| `StatType.MoveSpeed` | `Movement.MoveSpeed` |
| `StatType.DashDistance` | `Movement.DashDistance` |
| `StatType.SkillGaugeRate` | `Resource.GenerationMultiplier` |
| `PlayerSkillGauge._currentGauge` | `Resource.UltimateEnergy` |
| `PlayerSkillGauge._maxGauge` | `Resource.MaxUltimateEnergy` |
| `StatType.InvincibleDuration` | `Combat.InvincibleDurationMultiplier` |
| `StatType.GatheringPower` | `Life.GatheringPower` |

---

## 8. GameplayEffectSpec

### 8.1 세 계층 분리

```text
GameplayEffectSO
  불변 에셋 정의
        │ MakeOutgoingSpec
        ▼
GameplayEffectSpec
  적용 전에 생성되는 가변 실행 명세
        │ Apply
        ▼
ActiveGameplayEffect
  대상에 적용된 런타임 인스턴스
```

| 계층 | 소유 데이터 |
|------|-------------|
| Definition | Effect ID, Duration 정책, Modifier 정의, Tag 요구, Stack 정책, Cue |
| Spec | Definition 참조, Level, Context, SetByCaller, Source/Target Tag Snapshot, 캡처 Attribute, 계산된 Duration/Magnitude |
| Active Effect | Handle, Spec 사본, StartTime, Remaining, Period, Stack, 부여 Tag/Modifier Handle |

현재 `GameplayEffectInstance`가 Definition과 Active 상태만 가지는 구조에 Spec 단계를 추가한다.

### 8.2 Effect Context

신규 `GameplayEffectContext`는 Effect 실행 동안 전달되는 일회성 문맥이다.

```csharp
// 신규 제안
public readonly struct GameplayEffectContext
{
    public AbilitySystemHandle Instigator;
    public AbilitySystemHandle EffectCauser;
    public AbilitySystemHandle Target;
    public AbilityExecutionHandle AbilityHandle;
    public object SourceObject;
    public Vector3 Origin;
    public HitContextData Hit;
    public ulong RandomSeed;
}
```

Core는 `GameActor`, Unity Collider, `HitResult` 구체 타입을 저장하지 않는다. 프로젝트 Adapter가 필요한 데이터를 Core DTO로 변환한다.

필수 Context 데이터:

- Instigator: 피해/회복의 논리적 소유자
- EffectCauser: 투사체, 설치기 등 실제 발생 주체
- Target
- Source Ability와 실행 Handle
- Source Object 안정 식별자
- 위치/방향/충돌 데이터
- 결정적 계산용 Seed

### 8.3 SetByCaller

Ability Task나 타격 Adapter가 런타임 수치를 Spec에 넣을 수 있어야 한다.

```text
Data.Damage
Data.PoiseDamage
Data.BreakDamage
Data.HealAmount
Data.ChargeRatio
Data.ComboMultiplier
Data.HitIndex
```

규칙:

- 키는 안정 Tag 또는 안정 ID를 사용한다.
- 필수 키 누락은 0 폴백이 아니라 명시적 적용 실패로 처리한다.
- 기본값 허용 여부는 Modifier Definition이 선언한다.
- Debugger는 최종 Spec의 SetByCaller 전체를 표시한다.

### 8.4 Attribute Capture

Magnitude와 Execution은 Source/Target Attribute를 캡처할 수 있다.

| 캡처 정책 | 의미 |
|-----------|------|
| SnapshotOnCreate | Spec 생성 시 Source 값을 고정 |
| SnapshotOnApply | 대상 적용 직전 값을 고정 |
| EvaluateOnExecute | 주기 Tick 또는 실행 순간의 최신 값을 읽음 |

예:

- 공격력: 공격 발생 시점 고정이 필요하면 `SnapshotOnCreate`
- 대상 방어력: 명중 시점 기준이면 `SnapshotOnApply`
- 지속 회복의 MaxHealth 비율: 각 Tick 최신값을 원하면 `EvaluateOnExecute`

Spec은 실제 계산에 필요한 Attribute만 캡처한다. 전체 Attribute Set 복사는 금지한다.

### 8.5 Magnitude Calculation

신규 Magnitude 종류:

| 종류 | 설명 |
|------|------|
| Fixed | Definition 고정값 |
| Scalable | Level 또는 Rank Curve 기반 값 |
| SetByCaller | Spec 외부 전달값 |
| AttributeBased | Source/Target Attribute 기반 공식 |
| CustomCalculation | 등록된 순수 계산 전략 |

AttributeBased 기본 공식:

```text
Magnitude =
    (CapturedAttribute + PreAdd)
    × Coefficient
    + PostAdd
```

Custom Calculation은 다음 조건을 지킨다.

- Core 입력 DTO만 사용한다.
- Manager, Scene, UI를 참조하지 않는다.
- 같은 Spec과 Snapshot 입력에서 같은 결과를 반환한다.
- 계산 단계별 Trace를 Debug Recorder에 선택적으로 기록한다.

### 8.6 Execution Calculation

여러 Attribute를 읽고 여러 결과를 생성하는 복합 계산은 `IGameplayEffectExecution`으로 분리한다.

```csharp
// 신규 제안
public interface IGameplayEffectExecution
{
    void Execute(
        in GameplayEffectExecutionInput input,
        GameplayEffectExecutionOutput output);
}
```

첫 프로젝트 구현:

| Execution | 이전 대상 |
|-----------|-----------|
| `DamageExecution` | `DamageResolver`의 AttackPower, Defense, Crit, 원소, 취약 배율 계산 |
| `HealingExecution` | HealFlat, HealPercent, 패시브 회복 배율 |
| `PoiseDamageExecution` | `PoiseStat.TakePoiseDamage` |
| `BreakDamageExecution` | `MonsterBreakGauge.TakeBreakDamage` 중 수치 변경 부분 |
| `ResourceGainExecution` | 공격 적중 UltimateEnergy 충전 |

피해 적용 예:

```text
MotionSet Hit Event
→ CombatHitEffectAdapter
→ GE_Damage Definition으로 Outgoing Spec 생성
→ SetByCaller(Data.Damage/PoiseDamage/BreakDamage)
→ Source/Target Attribute Capture
→ DamageExecution
→ Meta.IncomingDamage 출력
→ Target AttributeSet PostExecute
→ Vital.Health 감소
→ Death/Reaction GameplayEvent
```

피해 공식의 최종 권위는 `DamageExecution`이 된다. 기존 `DamageResolver`와 Effect 계산을 동시에 적용하면 안 된다.

### 8.7 적용 파이프라인

```text
MakeOutgoingSpec
→ Context 유효성 검사
→ Source Tag Capture
→ Target 해석
→ Target Tag Capture
→ Application Requirements
→ Immunity Query
→ Attribute Capture
→ Duration/Magnitude 계산
→ Stack 해석
→ Modifier 또는 Execution 적용
→ Granted Tag/Cue 등록
→ Active Effect 저장
→ Debug Event 기록
```

실패 결과는 표준 enum으로 반환한다.

```text
InvalidDefinition
InvalidContext
MissingSetByCaller
MissingAttribute
BlockedByTag
Immune
InvalidTarget
CalculationFailed
StackRejected
```

### 8.8 Cooldown과 Cost

최종 구조에서는 Cost와 Cooldown도 GameplayEffectSpec으로 표현한다.

- Cost Effect: Instant, Owner 대상, 자원 Attribute 감소
- Cooldown Effect: Duration, Cooldown Tag 부여
- 공유 쿨다운: 동일 Cooldown Tag Query
- 쿨다운 감소 Stat: Duration Magnitude Calculation에 적용

전환 초기에는 `AbilityCooldownRuntime`을 유지할 수 있으나 Attribute/EffectSpec 수직 슬라이스 완료 후 Cooldown Effect로 이전한다.

---

## 9. Ability Task

### 9.1 목적

Ability Task는 여러 프레임에 걸쳐 진행되며 부모 Ability 실행의 수명에 종속되는 작업이다.

```text
Ability Instance
├─ Task: 상태 전환 요청
├─ Task: MotionSet 재생 후 이벤트 대기
├─ Task: Target 확정 대기
├─ Task: GameplayEvent 대기
└─ Task: Effect 적용
```

Task는 멀티스레드 작업이 아니다. Unity 메인 스레드에서 Tick/Event 기반으로 실행한다.

### 9.2 수명주기

```text
Created
→ Activating
→ Active
→ Succeeded / Failed / Cancelled
→ Ended
```

필수 규칙:

1. Task는 정확히 하나의 부모 Ability Execution Handle을 가진다.
2. 부모가 End/Cancel/Abort되면 모든 자식 Task가 같은 프레임에 종료된다.
3. Task 종료 시 입력, Tag, MotionEvent, GameplayEvent 구독을 모두 해제한다.
4. Task 완료 콜백은 최대 한 번만 발행한다.
5. Task가 실패해도 부모를 자동 종료할지는 Definition의 정책으로 결정한다.
6. Task는 Definition ScriptableObject를 수정하지 않는다.
7. Task는 다른 Task의 내부 상태를 직접 참조하지 않고 결과 Event를 통해 연결한다.

### 9.3 정의와 런타임 분리

Unity 직렬화 안정성을 위해 다음 구조를 사용한다.

```text
AbilityTaskDefinitionSO       공유/서브에셋 불변 정의
        │ CreateRuntime
        ▼
AbilityTaskInstance           실행별 일반 C# 가변 객체
```

`[SerializeReference]` 기반 임의 Task 그래프를 먼저 도입하지 않는다. MotionEvent 타입 이동과 동일한 managed reference 손실 위험을 피하기 위해 V1 Task 정의는 ScriptableObject 서브에셋을 사용한다.

### 9.4 Task 실행 모델

V1은 다음 조합을 지원한다.

- Sequence: 앞 Task 성공 후 다음 Task 실행
- ParallelAll: 모든 Task 성공 시 완료
- ParallelAny: 하나가 성공하면 나머지 취소
- BranchByTag: Tag Query 결과에 따라 분기
- BranchByEvent: 수신 Event 종류에 따라 분기
- Repeat: 명시된 최대 횟수 안에서 반복

무제한 반복과 순환 참조는 에디터 검증 오류다.

### 9.5 기본 Core Task

| Task | 역할 |
|------|------|
| `WaitDelayTask` | Clock 기반 시간 대기 |
| `WaitGameplayEventTask` | Tag와 Payload가 일치하는 Event 대기 |
| `WaitTagAddedTask` | 소유 Tag 추가 대기 |
| `WaitTagRemovedTask` | 소유 Tag 제거 대기 |
| `ApplyGameplayEffectTask` | Spec 생성 및 Self/Target 적용 |
| `SendGameplayEventTask` | 대상 ASC에 Event 발행 |
| `CommitAbilityTask` | Cost/Cooldown Commit |
| `EndAbilityTask` | 완료/취소 결과로 부모 종료 |

### 9.6 UPlayground Adapter Task

| Task | 기존 책임 |
|------|-----------|
| `RequestActorStateTask` | KCC 상태 전환 요청과 성공/실패 반환 |
| `PlayMotionAndWaitTask` | `AnimKey`, MotionSet 실행과 완료/취소 대기 |
| `WaitMotionEventTask` | Hit, ComboWindow, VFX 타임라인 Event 대기 |
| `AcquireTargetTask` | 현재 Lock-on/AI Target을 Target Data로 변환 |
| `WaitInputTask` | Input Layer를 통한 Confirm/Cancel/Release 대기 |
| `SetMotionWarpTargetTask` | MotionWarp Target 설정/정리 |
| `SpawnProjectileTask` | 투사체 생성 후 Source Ability/Spec Context 전달 |
| `ApplyHitEffectSpecTask` | `AbilityAttackInfo`를 Outgoing EffectSpec으로 변환 |

KCC와 MotionSet은 계속 최종 권위다.

- Task는 상태 전환을 “요청”하고 성공 결과를 받는다.
- Task가 KCC 속도나 회전을 직접 계산하지 않는다.
- MotionSet이 Hit 타이밍, Collision, VFX/SFX 타임라인의 최종 권위를 유지한다.
- Ability Task는 MotionSet 완료/이벤트를 기다리고 다음 흐름을 결정한다.

### 9.7 기존 Payload 마이그레이션

```text
현재
GameplayAbilitySO
→ Variant
→ UPlayGroundMotionAbilityPayloadSO
→ AnimKey + AbilityAttackInfo

목표
GameplayAbilitySO
→ AbilityTaskGraphSO
   ├─ RequestActorStateTask
   ├─ CommitAbilityTask
   ├─ PlayMotionAndWaitTask(AnimKey)
   ├─ WaitMotionEventTask(Hit)
   ├─ ApplyHitEffectSpecTask(Damage Effect)
   └─ EndAbilityTask
```

전환기에는 `LegacyMotionPayloadTask` 하나가 기존 Payload를 감싸는 것을 허용한다. 모든 Ability가 Task Graph로 변환되면 삭제한다.

---

## 10. Gameplay Event

Ability Task와 Effect 후처리를 느슨하게 연결하기 위해 Tag 기반 Event Router를 ASC에 둔다.

```csharp
// 신규 제안
public readonly struct GameplayEventData
{
    public string EventTagId;
    public AbilitySystemHandle Instigator;
    public AbilitySystemHandle Target;
    public AbilityExecutionHandle AbilityHandle;
    public GameplayEffectSpecHandle EffectSpecHandle;
    public float Magnitude;
    public object Payload;
}
```

초기 표준 Event:

```text
Event.Combat.Hit
Event.Combat.DamageApplied
Event.Combat.Healed
Event.Combat.PoiseBroken
Event.Combat.BreakExposed
Event.Actor.Death
Event.Actor.Respawn
Event.Input.Confirm
Event.Input.Cancel
Event.Motion.Completed
Event.Motion.Cancelled
```

Event는 상태 변경 권위가 아니라 알림과 Ability Trigger다. 동일 Event가 중복 적용을 일으키지 않도록 발행 지점의 단일 권위를 문서화한다.

---

## 11. 통합 런타임 디버거

### 11.1 목표

Play Mode에서 선택한 Actor의 GAS 상태와 최근 변화 원인을 한 화면에서 관찰한다.

메뉴 제안:

```text
UPlayGround/게임플레이/Ability/GAS Runtime Debugger
```

### 11.2 화면 구성

```text
┌ Actor 목록/검색 ─────┬ 선택 Actor 요약 ──────────────────────┐
│ Player               │ ASC ID / Actor ID / Frame / Time      │
│ Monster_A            │ [Abilities] [Tasks] [Effects]         │
│ Monster_B            │ [Tags] [Attributes] [Events] [Trace]  │
└──────────────────────┴───────────────────────────────────────┘
```

| 탭 | 표시 내용 |
|----|-----------|
| Overview | 활성 Ability/Task/Effect 수, 주요 Vital, 최근 실패 |
| Abilities | Granted Spec, Level, Source, 활성 횟수, Cooldown, 차단 사유 |
| Tasks | 부모 Ability, Task 타입, 상태, 시작 시간, 대기 조건, 종료 사유 |
| Effects | Definition/Spec/Active Handle, Source, Stack, 남은 시간, Period, Granted Tag |
| Tags | Explicit/Owned/Aggregated Tag, Source Handle, 참조 수 |
| Attributes | Base, Current, Modifier 목록, Clamp, 마지막 변경 Source |
| Events | GameplayEvent 시간순 기록, Instigator/Target/Payload |
| Trace | 활성화 평가, EffectSpec 계산, Attribute Transaction 단계별 Trace |

### 11.3 읽기 계약

Editor Window가 런타임 private 필드나 Manager를 직접 탐색하지 않는다.

```csharp
// 신규 제안
public interface IAbilitySystemDebugSource
{
    AbilitySystemDebugSnapshot CaptureDebugSnapshot(
        AbilityDebugCaptureOptions options);
}
```

Snapshot은 복사된 읽기 전용 DTO다.

- Debugger가 Runtime Collection을 열거하는 동안 게임 상태가 바뀌어도 예외가 없어야 한다.
- Snapshot 변경으로 Gameplay 상태를 수정할 수 없어야 한다.
- Runtime Debugger의 “Effect 제거” 같은 치트 기능은 별도 Cheat 명령으로 분리하고 기본 화면에는 두지 않는다.

### 11.4 Debug Registry

새 전역 게임 매니저는 만들지 않는다.

- `UNITY_EDITOR` 또는 `DEVELOPMENT_BUILD`에서만 활성화되는 Debug Registry를 사용한다.
- Registry는 Weak Reference와 ASC의 명시적 Register/Unregister만 수행한다.
- 게임플레이 로직은 Registry 존재 여부에 의존하지 않는다.
- Release Player Build에서는 Recorder와 Registry 코드를 제거하거나 No-op으로 컴파일한다.

### 11.5 Event Recorder

ASC별 고정 크기 Ring Buffer를 사용한다.

```text
Sequence
Frame
Time
ActorId
Category
EventType
AbilityHandle
TaskHandle
EffectHandle
AttributeId
OldValue/NewValue
Result
Source
Message
```

기록 Category:

- Ability Granted/Removed/Activated/Committed/Ended/Cancelled/Failed
- Task Started/Succeeded/Failed/Cancelled
- Effect Spec Created/Applied/Rejected/Stacked/Expired/Removed
- Tag Added/Removed
- Attribute Base Changed/Modifier Changed/Clamped
- GameplayEvent Sent/Received
- Cue Dispatched

### 11.6 계산 Trace

EffectSpec 계산은 선택적으로 다음 단계를 기록한다.

```text
GE_Damage
├─ SetByCaller Data.Damage = 120
├─ Source Combat.AttackPower = 1.25 [SnapshotOnCreate]
├─ Target Combat.Defense = 0.20 [SnapshotOnApply]
├─ ElementMultiplier = 1.50
├─ CriticalMultiplier = 1.00
└─ Meta.IncomingDamage = 180
```

Trace는 기본 비활성이다. Debugger에서 Actor 또는 Effect ID 단위로 활성화한다.

### 11.7 성능 기준

| 항목 | 기준 |
|------|------|
| Debug 비활성 Release Build | 할당과 Tick 오버헤드 0에 근접 |
| Debug 활성, 창 닫힘 | 이벤트당 고정 Ring Buffer 기록만 수행 |
| Debugger 자동 갱신 | 기본 4Hz, 사용자 조절 가능 |
| Snapshot | 필요한 탭 데이터만 선택 캡처 |
| Event Buffer | Actor당 기본 512개, 최대값 제한 |
| 문자열 | 가능한 ID 참조 사용, 표시 시점에 포맷 |

### 11.8 기존 Stat Monitor 처리

`StatRuntimeMonitorWindow`는 다음 순서로 대체한다.

1. GAS Runtime Debugger Attributes 탭에서 현재 기능을 모두 제공한다.
2. HP/Poise/Modifier 표시의 결과가 기존 Monitor와 일치하는지 검증한다.
3. 기존 메뉴에 새 Debugger를 여는 리다이렉트 안내를 한 릴리스 유지한다.
4. `StatRuntimeMonitorWindow.cs`를 삭제한다.

---

## 12. 데이터 저작과 Ability Editor 확장

### 12.1 Ability Editor 신규 탭

현재 탭에 다음을 추가 또는 재구성한다.

```text
기본 정보
활성화/Tag
Cost/Cooldown
Task Graph
EffectSpec
Cue
저장/교체
검증
```

### 12.2 Effect Editor

Effect 편집 화면은 다음 구조를 표시한다.

- Duration Magnitude
- Period Magnitude
- Application Tag Requirements
- Immunity Query
- Modifier별 Attribute와 Operation
- Magnitude Calculation 종류
- Capture Source/Target 및 Snapshot 정책
- Execution Calculation
- Granted/Removed/Blocked Tag
- Stack Key와 Stack 정책
- Cue

### 12.3 EffectSpec Preview

에디터에서 Source/Target Attribute Snapshot과 SetByCaller 값을 입력하여 적용 결과를 미리 계산한다.

결과:

- 최종 Duration/Period
- Attribute별 Modifier
- Execution Output
- Stack 결과
- 누락 Attribute/SetByCaller
- 단계별 Calculation Trace

Preview는 런타임과 같은 Core Calculator를 사용해야 한다. Editor 전용 복제 공식은 금지한다.

### 12.4 Attribute Set Editor

기능:

- Attribute Registry 조회
- Set별 기본값 편집
- 중복 ID 검사
- Min/Max 의존성 검사
- Clamp/Max-change 정책 편집
- 저장 대상 표시
- 사용 중인 Effect/Ability/장비 역참조
- 기존 `ActorStatSO`와 결과 비교

### 12.5 Task Graph V1

V1은 범용 노드 그래프보다 안전한 계층 목록 편집을 우선한다.

```text
Sequence
  1. RequestActorState
  2. CommitAbility
  3. PlayMotionAndWait
  4. ParallelAny
     - WaitMotionCompleted
     - WaitGameplayEvent(Cancel)
  5. EndAbility
```

Task 순환, 종료 없는 Wait, Commit 누락/중복, 부모 종료 후 Task 배치는 검증 오류다.

---

## 13. 기존 시스템별 완전 마이그레이션

### 13.1 ActorStatSO / ActorStatContainer

```text
ActorStatSO
→ AttributeSetDefinitionSO 또는 ActorAttributeProfileSO

ActorStatContainer
→ AbilitySystemComponent.AttributeSetRuntime

StatModifier
→ GameplayEffect Modifier + ActiveGameplayEffect
```

전환 규칙:

- 기존 `ActorStatSO`의 모든 명시값과 폴백값을 Attribute Profile로 생성한다.
- 성장 계산 결과는 Attribute Base 초기화 데이터로 변환한다.
- `Stats.SetBase` 호출은 `SetAttributeBase` 초기화 Transaction으로 전환한다.
- `Stats.AddModifier` 호출은 GameplayEffect 적용으로 전환한다.
- `RemoveModifiersBySource`는 Effect Handle 또는 Source Query 제거로 전환한다.
- 모든 소비자가 `IAttributeReader`를 사용한 뒤 `ActorStatContainer`를 삭제한다.

### 13.2 Player/Monster Health

삭제 대상:

```text
PlayerActor._maxHealth
PlayerActor._currentHealth
MonsterActor._maxHealth
MonsterActor._currentHealth
캐릭터별 _characterHealthMap
```

대체:

```text
Vital.Health
Vital.MaxHealth
CharacterAbilityRuntimeSaveData.Attributes
```

`IDamageable`은 호환을 위해 유지할 수 있지만 구현은 Attribute를 읽는다.

```csharp
public float GetCurrentHealth() =>
    AbilitySystem.Attributes.GetCurrent(AttributeIds.Vital.Health);
```

피해, Heal, HealPercent, SetHealth, Respawn은 모두 EffectSpec 또는 Attribute Restore Transaction을 사용한다.

### 13.3 PoiseStat

수치 권위:

```text
PoiseStat._currentPoise       → Vital.Poise
Stats.MaxPoise                → Vital.MaxPoise
PoiseRecoveryRate/Delay       → Vital Attribute
```

`PoiseStat`의 잔여 책임:

- PoiseBroken Event를 상태 머신 전이 정책에 전달
- 하이퍼아머 Tag와 피격 리액션 정책 연결
- UI/피드백 Adapter

수치 권위 제거 후 타입명을 `PoiseReactionController`로 변경하는 것을 권장한다.

Poise 회복은 직접 Update 대신 다음 중 하나로 표현한다.

- Break 후 Delay Task가 Periodic Recovery Effect 적용
- `GE_PoiseRecovery` Infinite/Periodic Effect
- 피격 시 Recovery Effect 제거 후 Delay Task로 재부여

### 13.4 PlayerSkillGauge

분해 대상:

| 현재 책임 | 목표 |
|-----------|------|
| UltimateEnergy 현재/최대 | Resource Attribute Set |
| 공격별 충전 | `GE_UltimateEnergyGain` EffectSpec |
| 비용 | Ability Cost Effect |
| 슬롯 쿨다운 | Cooldown Effect |
| 저장 Snapshot | ASC Runtime Save |
| UI Event | Attribute Changed Event |
| 스킬 해금 | Party/Growth의 Ability Grant 정책 유지 |

모든 소비자가 Attribute/Ability View를 사용하면 `PlayerSkillGauge`를 삭제한다.

### 13.5 장비

현재 `EquipmentSO`의 `EquipmentStatEntry`와 레거시 필드는 장비 GameplayEffect로 변환한다.

```text
Equipment Instance
→ Equipment EffectSpec
→ SourceObject = 장비 인스턴스 안정 ID
→ Infinite ActiveGameplayEffect
→ 장비 해제 시 Effect Handle 제거
```

랜덤 성장 옵션은 SetByCaller 또는 런타임 생성 Modifier Spec으로 전달한다. 장비 SO 원본을 런타임에 변경하지 않는다.

장비 MaxHealth 변경 시 Attribute 정책이 체력 비율을 처리하므로 `CaptureHealthSnapshot`과 수동 HP 보정 코드를 삭제한다.

### 13.6 성장과 레벨

- 레벨/성장으로 영구 변경되는 값은 Attribute Base를 구성한다.
- 임시 성장 보너스는 Infinite GameplayEffect로 구분한다.
- `PartyPowerCalculator`, Balance Tool은 런타임 컨테이너 대신 Attribute Profile 계산기를 사용한다.
- 런타임과 에디터가 같은 Base Attribute 생성 함수를 공유한다.

### 13.7 Passive

`PassiveAbilitySO`는 최종적으로 다음 중 하나로 변환한다.

| Passive 유형 | 목표 표현 |
|--------------|-----------|
| Always Stat 보정 | 자동 적용 Infinite GameplayEffect |
| PerfectDodge/Guard 트리거 | GameplayEvent Trigger Ability |
| 파티 최고값 | Party Adapter가 선택한 EffectSpec Grant |
| Effect Duration 보정 | Magnitude Calculation 또는 Attribute |

`PassiveAbilityController`는 전환기 Grant Adapter로 축소한 뒤, 모든 Passive가 Ability/Effect로 표현되면 삭제한다.

### 13.8 소모품

소모품은 Actor의 `Heal`을 직접 호출하지 않는다.

```text
ConsumableSO
→ GameplayEffectSO 참조
→ Outgoing Spec
→ SetByCaller(Data.HealAmount)
→ HealingExecution
```

`requireEffectiveUse`는 Spec Preview/Evaluate 결과로 실제 변화량이 0인지 적용 전에 확인한다.

### 13.9 DamageResolver와 전투 파이프라인

이전 순서:

1. 기존 `DamageResolver` 공식과 `DamageExecution` 결과를 Shadow 비교한다.
2. Combat Result에 두 결과와 차이를 기록한다.
3. 모든 테스트와 데이터에서 허용 오차 안에 들어오면 Effect 결과를 권위로 전환한다.
4. Actor HP 직접 차감을 제거한다.
5. `DamageResolver`를 삭제하거나 순수 `DamageExecution` 구현으로 이동한다.

상태 리액션, HitStop, 카메라, VFX는 피해 Attribute Transaction 결과와 GameplayEvent를 소비한다.

### 13.10 GameplayTagContainer

현재 소유 Handle과 참조 카운트 로직은 `GameplayTagAggregator`로 흡수한다.

추가 기능:

- Exact/Hierarchy Query
- All/Any/None 조합
- Source Tag와 Target Tag 구분
- Ability Block/Cancel Query
- Effect Application/Immunity Query
- Debug Source 추적

상태 머신은 전환기 Adapter로 기존 `AddTag/RemoveTag` API를 사용할 수 있다. 모든 호출이 ASC Tag Handle로 변환되면 기존 컴포넌트를 삭제한다.

### 13.11 GameplayEffectController / ActorAbilitySystem

최종적으로 별도 MonoBehaviour 두 개를 유지하지 않는다.

```text
ActorAbilitySystem
GameplayEffectController
GameplayTagContainer
ActorStatContainer
        │
        ▼
AbilitySystemComponent
├─ AbilitySpecContainer
├─ ActiveGameplayEffectContainer
├─ GameplayTagAggregator
└─ AttributeSetRuntime
```

내부 Runtime 클래스는 분리하되 Unity Component 집합 루트는 하나로 만든다.

---

## 14. 저장과 캐릭터 교체

### 14.1 신규 저장 DTO

```csharp
// 신규 제안
[Serializable]
public sealed class AbilitySystemSaveData
{
    public int version;
    public List<AttributeSaveEntry> attributes;
    public List<AbilityCooldownSaveEntry> cooldowns;
    public List<ActiveEffectSaveEntry> activeEffects;
}

[Serializable]
public sealed class AttributeSaveEntry
{
    public string attributeId;
    public float baseValue;
}
```

저장하지 않는 것:

- 계산된 Current 캐시
- Definition SO 인스턴스
- Task Instance
- 현재 프레임 Handle
- 절대 종료 시각
- Source `object`

저장하는 것:

- 저장 정책이 활성화된 Attribute Base
- 남은 Cooldown
- 저장 가능한 Active Effect의 Definition ID, 남은 시간, Stack, SetByCaller
- 캐릭터별 Ability Grant/해금에 필요한 안정 ID

### 14.2 캐릭터 교체

현재 `_characterHealthMap`, `_characterSkillMap`, `_characterSkillCooldownMap`, `_characterAbilityRuntimeMap`을 하나의 캐릭터별 `AbilitySystemSaveData`로 합친다.

교체 순서:

```text
현재 캐릭터 Ability 취소/유지 정책 평가
→ ASC Runtime Save 캡처
→ RemoveOnSwap Effect 제거
→ 새 Attribute Profile 초기화
→ 캐릭터 Save 복원
→ AbilitySet Grant
→ PersistOnPlayerActor Effect 재연결
→ Attribute/UI Event 일괄 발행
```

### 14.3 세이브 버전 마이그레이션

Legacy → GAS 매핑:

| Legacy 값 | 신규 |
|-----------|------|
| character health | `Vital.Health` Base |
| skill gauge | `Resource.UltimateEnergy` Base |
| slot cooldown 배열 | Cooldown Tag/Ability ID Entry |
| AbilityRuntime activeEffects | Active Effect Spec Save |
| Stat 성장값 | Attribute Base Profile |

구버전 세이브 로드 테스트와 왕복 저장 테스트를 필수로 둔다.

---

## 15. 단계별 구현 계획

### Phase 0 — 기준선 고정

목표: 마이그레이션 전 결과를 자동 비교할 수 있게 한다.

- 현재 Stat, HP, Poise, Gauge, Damage, Effect, Tag 동작 테스트 추가
- Player/Monster 대표 데이터 Golden Snapshot 생성
- 장비 착탈 MaxHealth 비율, 캐릭터 교체, 저장/복원 테스트 추가
- DamageResolver 입력/출력 샘플 수집
- Ability 활성화 실패, Cooldown, Effect Stack 테스트 확장

완료 게이트:

- 기존 Ability EditMode 테스트 전부 통과
- 대표 플레이어/몬스터 수치 Snapshot 확정
- 현재 저장 데이터 왕복 검증

### Phase 1 — Core Attribute Runtime

- `AttributeId`, Definition, Runtime, Modifier Aggregator 구현
- Base/Current, Clamp, Max-change 정책 구현
- Attribute Transaction과 변경 Event 구현
- 프로젝트 타입 없는 Core 테스트 작성
- 기존 `ActorStatSO` → Attribute Profile 변환 Preview 도구 작성

이 단계에서는 기존 Stat이 권위이며 새 Attribute는 Shadow 계산만 한다.

완료 게이트:

- 모든 `StatType` 매핑 존재
- Shadow 최종값이 기존 `ActorStatContainer`와 일치
- Core가 UPlayground 타입을 참조하지 않음

### Phase 2 — AbilitySystemComponent 집합 루트와 Debugger

- ASC Component와 내부 Container 구성
- 기존 Ability/Effect/Tag를 Adapter로 연결
- Debug Snapshot/Recorder/Registry 구현
- Runtime Debugger의 Ability/Effect/Tag/Attribute 탭 구현
- 기존 Stat Monitor와 병행 비교

완료 게이트:

- 선택 Actor의 기존/신규 상태가 Debugger에서 동시에 비교됨
- Debugger가 게임 상태를 수정하지 않음
- Release Build에서 Debug Recorder 비활성 확인

### Phase 3 — GameplayEffectSpec

- Context, SetByCaller, Attribute Capture 구현
- Magnitude Calculation 구현
- Active Effect Container를 Spec 기반으로 전환
- EffectSpec Preview와 Trace 구현
- Cost/Cooldown 이외의 기존 Effect를 Spec으로 전환

완료 게이트:

- 모든 GameplayEffect 적용이 Spec을 생성함
- Definition을 직접 Active Instance에 적용하는 경로 0
- 누락 SetByCaller/Attribute가 명시적 오류로 검출됨

### Phase 4 — Health/Poise/UltimateEnergy 수직 슬라이스

권장 순서:

1. UltimateEnergy
2. Poise
3. Monster Health
4. Player Health

각 Attribute는 다음 순서로 전환한다.

```text
Shadow Write
→ Legacy/New 값 비교
→ Read 권위 전환
→ Write 권위 전환
→ Legacy 필드 직렬화 제거
```

완료 게이트:

- 대상 값의 직접 필드 쓰기 0
- UI와 저장이 Attribute Event/Save를 사용
- 전환 대상별 PlayMode 수직 슬라이스 통과

### Phase 5 — Damage/Healing Execution

- `DamageExecution`, `HealingExecution`, Poise/Break Execution 구현
- HitContext → EffectContext Adapter 구현
- 기존 DamageResolver와 Shadow 비교
- 최종 피해 권위를 Effect Execution으로 전환
- Actor 직접 HP 차감과 Heal 호출 경로 제거

완료 게이트:

- 동일 입력에서 기존 공식과 허용 오차 이내
- 모든 피해/회복이 EffectSpec Handle과 Trace를 남김
- 사망/리액션/피드백 순서 회귀 없음

### Phase 6 — Ability Task

- Task Runtime과 부모 취소 보장 구현
- Wait/Event/ApplyEffect Core Task 구현
- MotionSet/KCC Adapter Task 구현
- `LegacyMotionPayloadTask`로 기존 Ability 연결
- 대표 Ability 1개를 순수 Task Graph로 전환
- 전체 플레이어/몬스터 Ability 변환

완료 게이트:

- Ability 종료 후 살아 있는 Task 0
- MotionEvent 구독 누수 0
- Task Graph 없는 레거시 Payload 실행 0

### Phase 7 — 장비/성장/패시브/소모품

- 장비 Modifier를 Infinite Effect로 전환
- 성장값을 Attribute Base Profile로 전환
- Passive를 Grant Ability/Effect로 전환
- 소모품을 GameplayEffectSpec으로 전환
- Balance/Party/UI 도구를 Attribute Reader로 전환

완료 게이트:

- `Stats.AddModifier`, `Stats.SetBase` 호출 0
- 장비/패시브가 Active Effect로 Debugger에 표시
- 소모품 직접 Heal 호출 0

### Phase 8 — 레거시 삭제

- `ActorStatContainer`, `StatModifier`, `StatType` 런타임 참조 삭제
- `ActorStatSO` 에셋 변환 후 타입 삭제
- `PlayerSkillGauge` 삭제
- HP/Poise 직접 현재값 필드 삭제
- `GameplayEffectController`, `GameplayTagContainer`, `ActorAbilitySystem` 별도 Component 삭제
- 기존 Stat Editor/Monitor/Generator 제거 또는 신규 도구로 교체
- 구 세이브 마이그레이터만 호환 계층으로 유지

완료 게이트:

- 레거시 타입 이름 전체 검색 결과가 마이그레이터/과거 문서 외 0
- 모든 프리팹 Missing Script 0
- 모든 Attribute/Ability/Effect 에셋 검증 오류 0
- Player Build 오류 0

### Phase 9 — 안정화와 문서 완료

- 전체 EditMode/PlayMode 테스트
- Player/Monster/BT/Party 교체/장비/저장 스모크
- Ability Editor와 Runtime Debugger 사용 가이드 작성
- `STAT_SYSTEM_GUIDE.md` 폐기 안내
- 본 문서를 Complete로 이동

---

## 16. 전환기 권위 규칙

이중 권위 기간을 짧게 유지한다.

| 모드 | Legacy | GAS Attribute | 허용 목적 |
|------|--------|---------------|-----------|
| LegacyAuthorityShadow | 쓰기/읽기 권위 | 결과 비교만 | Phase 1~초기 4 |
| GasAuthorityMirror | 읽기/쓰기 권위 | 권위, Legacy에 관찰용 Mirror | Attribute별 짧은 검증 |
| GasOnly | 사용 금지 | 단일 권위 | 최종 |

금지:

- 같은 프레임에 Legacy와 GAS가 서로를 양방향 동기화
- 호출 위치에 따라 서로 다른 권위를 선택
- 값 불일치 시 조용히 Legacy 값을 우선
- 이중 적용된 Modifier/Effect

불일치는 Debugger와 테스트에서 즉시 보이게 하고, 권위 전환 전 해결한다.

---

## 17. API 전환 원칙

### 17.1 읽기

현재:

```csharp
float attack = actor.Stats.AttackPower;
float health = actor.GetCurrentHealth();
float gauge = player.SkillGauge.CurrentGauge;
```

전환기:

```csharp
float attack = actor.AbilitySystem.Attributes.GetCurrent(
    AttributeIds.Combat.AttackPower);
```

공용 소비자는 구체 Component 대신 계약을 사용한다.

```csharp
public interface IAttributeReader
{
    bool TryGet(AttributeId id, out GameplayAttributeValue value);
    event Action<AttributeChangedEvent> AttributeChanged;
}
```

### 17.2 쓰기

금지:

```csharp
actor.Stats.SetBase(...);
actor.Stats.AddModifier(...);
player.Heal(...);
player.SkillGauge.AddGauge(...);
```

허용:

```text
초기화/세이브 복원 → Attribute Base Transaction
게임플레이 변경     → GameplayEffectSpec
치트                → 명시적 Cheat Attribute Transaction + Debug Event
```

### 17.3 UI

UI는 다음 읽기 모델만 사용한다.

- `IAttributeReader`
- `IAbilityRuntimeReader`
- `IGameplayEffectRuntimeReader`
- 후속 통합 `IAbilitySystemRuntimeReader`

UI가 Cooldown을 자체 차감하거나 Attribute를 직접 보정하면 안 된다.

---

## 18. 데이터 마이그레이션 도구

신규 메뉴 제안:

```text
UPlayGround/Ability/GAS Migration
```

기능:

1. `ActorStatSO` → Attribute Profile Preview
2. `EquipmentStatEntry` → Equipment GameplayEffect Preview
3. `GameplayEffectSO` V1 → Spec 기반 Definition Upgrade
4. `UPlayGroundMotionAbilityPayloadSO` → Task Graph 생성
5. Passive → Grant Ability/Effect 변환
6. 전체 참조 검사
7. Legacy 사용 코드/에셋 리포트

안전 규칙:

- Preview 없이 자동 저장하지 않는다.
- 원본과 변환 결과를 표로 비교한다.
- 새 경로에 생성한 뒤 검증하고 참조를 교체한다.
- `.meta`와 GUID를 보존해야 하는 물리 이동은 함께 처리한다.
- 오류가 있는 Ability/MotionSet/Prefab을 일괄 재직렬화하지 않는다.
- `Assets/10.Datas/`와 `Assets/03.Prefabs/` 자동 변경은 반드시 diff를 검사한다.
- 변환 완료 전 원본 에셋을 삭제하지 않는다.
- 재실행 가능한 Idempotent Migration이어야 한다.

---

## 19. 검증 규칙

### 19.1 데이터 오류

- Attribute ID 중복 또는 빈 값
- 존재하지 않는 Attribute/Tag/Effect/Task 참조
- 필수 SetByCaller 기본값 누락
- Max Attribute의 순환 의존
- Clamp Min > Max
- Task Graph 순환
- 종료 경로 없는 무한 Wait
- Commit Task 중복
- Instant Effect에 Period 또는 Active 전용 설정
- Duration Effect의 음수 Duration
- Stack Key 충돌과 호환되지 않는 Modifier
- Snapshot Capture가 존재하지 않는 Source/Target Set을 요구
- Execution Calculation 등록 누락

### 19.2 코드 경계 검사

최종 Phase에서 다음 검색 결과가 0이어야 한다.

```text
actor.Stats
ActorStatContainer
StatModifier
PlayerSkillGauge
_currentHealth
_currentPoise
GameplayEffectController
GameplayTagContainer
ActorAbilitySystem
```

예외:

- 구 세이브 마이그레이터
- 변환 도구의 입력 타입
- 완료 전환 문서

### 19.3 런타임 불변식

- 종료된 Ability에 활성 Task가 없다.
- 제거된 Effect가 Modifier/Tag Handle을 남기지 않는다.
- Attribute Current가 Clamp 범위를 벗어나지 않는다.
- 동일 Spec Handle의 Instant Effect가 중복 Commit되지 않는다.
- Definition ScriptableObject가 Play Mode에서 변경되지 않는다.
- 사망 Event는 Health가 0을 통과할 때 한 번만 발생한다.
- Cost 실패 시 Cooldown과 실행 Effect가 적용되지 않는다.
- Commit 실패 시 시작 Cue가 발행되지 않는다.
- Restore 후 절대 시간이 아닌 남은 시간이 사용된다.

---

## 20. 테스트 계획

### 20.1 Core EditMode

Attribute:

- Base/Current 분리
- Add/Percent/Multiply/Override 순서
- Max 변경 정책별 결과
- Clamp와 의존 Attribute
- Transaction 원자성
- Modifier Handle 제거

EffectSpec:

- Definition과 Spec 분리
- SetByCaller 필수값
- Source/Target Snapshot 정책
- Fixed/Scalable/AttributeBased/Custom Magnitude
- Stack/Refresh/Replace
- Period Tick
- 저장/복원

AbilityTask:

- Sequence/Parallel 성공
- 부모 취소 시 자식 전체 종료
- Event 구독 해제
- 중복 완료 방지
- Timeout/실패 전파

Tag:

- Exact/Hierarchy/All/Any/None Query
- 다중 Source 참조 카운트
- Effect 제거 시 소유 Tag만 제거

Debugger:

- Snapshot이 Runtime Collection과 분리됨
- Ring Buffer 상한
- Debug 비활성 시 No-op

### 20.2 프로젝트 EditMode

- `ActorStatSO` → Attribute Profile 수치 동등성
- 장비 Modifier → EffectSpec 동등성
- DamageResolver → DamageExecution Golden Case
- 기존 AbilitySet → Task Graph 참조 무결성
- Passive/소모품 변환 결과
- 구 세이브 → 신규 Save DTO 변환

### 20.3 PlayMode

최소 수직 슬라이스:

1. Player Ability 활성화 → Motion Task → Hit → Damage Spec → Monster Health 감소
2. Monster BT Ability → Motion Task → Player 피격 → Attribute/Cue/UI 갱신
3. 버프 적용 → Attribute 변경 → 만료 → 원복
4. 장비 착탈 → Infinite Effect 추가/제거 → Health 비율 정책
5. Poise Damage → Break Event → 회복 Effect
6. UltimateEnergy 충전 → Cost Commit → 저장/교체/복원
7. Ability Cancel → 모든 Task/Tag/임시 Effect 정리
8. Runtime Debugger Snapshot과 실제 상태 일치

### 20.4 성능

- 다수 Monster ASC의 유휴 프레임 할당 0
- Active Effect/Task가 없을 때 Tick 생략
- Attribute 변경 없는 프레임 재계산 없음
- Modifier 변경 시 영향받는 Attribute만 Dirty
- Debugger가 닫혀 있을 때 Snapshot 생성 없음

---

## 21. 완료 조건

### 21.1 기능 완료

- Ability, Effect, Tag, Attribute가 ASC에서 통합 조회된다.
- 모든 다단 Ability가 Task 또는 명시적 Legacy Adapter를 거치며 최종적으로 Adapter도 제거된다.
- 모든 Effect 적용이 `GameplayEffectSpec`을 사용한다.
- Health, Poise, UltimateEnergy, 전투 Stat이 Attribute 단일 권위를 사용한다.
- Damage/Healing/Poise 계산이 Execution Calculation을 사용한다.
- Runtime Debugger에서 전체 상태와 계산 Trace를 볼 수 있다.

### 21.2 레거시 삭제 완료

- `ActorStatContainer` 런타임 사용 0
- `StatModifier` 직접 생성 0
- `PlayerSkillGauge` 사용 0
- Player/Monster HP 직접 필드 0
- `PoiseStat` 수치 권위 0
- `DamageResolver` 독립 공식 0
- 장비/패시브/소모품 직접 Stat/Health 변경 0
- 분리된 Ability/Effect/Tag Component 0

### 21.3 프로젝트 검증

- Unity 컴파일 오류 0
- Ability Core/EditMode 테스트 오류 0
- PlayMode 수직 슬라이스 오류 0
- Ability/Effect/Attribute/Task 데이터 검증 오류 0
- Missing Script 0
- managed reference/VFX 누락 0
- Play Mode 서비스 경고·예외 0
- 캐릭터 교체와 Save/Load 회귀 0
- Standalone Player Build 오류 0

---

## 22. 위험과 대응

| 위험 | 영향 | 대응 |
|------|------|------|
| 한 번에 모든 Stat을 교체 | 회귀 원인 추적 불가 | Attribute별 Shadow → 권위 전환 |
| Legacy/GAS 이중 적용 | 피해·버프 수치 배증 | 단방향 권위 모드와 Debug 비교 |
| Task 구독 누수 | 종료 후 Hit/Input 반응 | 부모 취소 강제, Task 누수 테스트 |
| EffectSpec 과도한 범용화 | 구현 지연·저작 복잡도 | Fixed/SetByCaller/AttributeBased부터 수직 슬라이스 |
| 모든 전투 로직을 Effect로 이동 | 상태/연출 책임 혼합 | 수치 변경만 Effect, KCC/MotionSet/Reaction은 Adapter 유지 |
| Attribute ID 변경 | 세이브·에셋 참조 손상 | 안정 ID/Alias/Migration |
| 에셋 일괄 변환 | 참조/GUID 유실 | Preview, 새 경로 생성, diff/검증 후 교체 |
| Debugger의 런타임 침범 | 디버그 유무에 따른 동작 차이 | 읽기 전용 Snapshot, Release No-op |
| 매 프레임 전체 Attribute 재계산 | 다수 몬스터 성능 저하 | Dirty Attribute만 재계산 |

---

## 23. 구현 우선순위

다음 순서를 변경하지 않는다.

```text
테스트 기준선
→ Attribute Core
→ ASC 집합 루트 + Debugger
→ GameplayEffectSpec
→ Resource/Poise/Health
→ Damage/Healing Execution
→ Ability Task
→ 장비/성장/패시브/소모품
→ 레거시 삭제
```

이 순서의 이유:

- Debugger를 초기에 만들어 이후 모든 전환의 관찰 도구로 사용한다.
- Attribute와 EffectSpec이 없으면 Damage와 장비를 올바르게 이전할 수 없다.
- Ability Task를 먼저 만들면 기존 Stat/Effect 직접 호출을 Task 안에 다시 고착시킬 수 있다.
- 레거시는 소비자와 데이터가 모두 전환된 마지막 단계에서만 삭제한다.

---

## 24. 참고 자료

Unreal 공식 문서에서 차용하는 핵심 개념:

- Gameplay Ability System 개요  
  https://dev.epicgames.com/documentation/unreal-engine/gameplay-ability-system-for-unreal-engine
- GAS 구성요소와 실행/소유권/Effect 관계  
  https://dev.epicgames.com/documentation/en-us/unreal-engine/understanding-the-unreal-engine-gameplay-ability-system
- Gameplay Ability와 비동기 실행  
  https://dev.epicgames.com/documentation/unreal-engine/using-gameplay-abilities-in-unreal-engine
- Ability Task 수명주기  
  https://dev.epicgames.com/documentation/unreal-engine/gameplay-ability-tasks-in-unreal-engine
- Attribute와 Attribute Set의 Base/Current 모델  
  https://dev.epicgames.com/documentation/en-us/unreal-engine/gameplay-attributes-and-attribute-sets-for-the-gameplay-ability-system-in-unreal-engine
- GameplayEffectSpec API  
  https://dev.epicgames.com/documentation/unreal-engine/API/Plugins/GameplayAbilities/FGameplayEffectSpec
- GameplayEffectContext API  
  https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Plugins/GameplayAbilities/FGameplayEffectContext
- Gameplay Debugger  
  https://dev.epicgames.com/documentation/unreal-engine/using-the-gameplay-debugger-in-unreal-engine

UPlayground는 위 개념의 책임 분리와 관찰 가능성을 차용한다. Unreal의 네트워크 복제 구현, Blueprint VM, UObject 인스턴싱을 그대로 재현하지 않는다.
