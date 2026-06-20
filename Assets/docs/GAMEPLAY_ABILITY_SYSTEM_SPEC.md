# Gameplay Ability System 적용 스펙

## 개요

이 문서는 UPlayground에 언리얼 Gameplay Ability System(GAS)의 핵심 개념을 적용하기 위한 설계 기준이다.
목표는 언리얼 ASC를 그대로 복제하는 것이 아니라, 현재 Unity 프로젝트의 상태 머신, MotionSet, 전투 판정, 스탯, 태그 구조 위에 **행동 실행 조건 / 비용 / 쿨다운 / 지속 효과 / 취소 규칙**을 통합하는 것이다.

현재 프로젝트에는 이미 ASC 유사 구성요소가 분산되어 있다.

| 구분 | 현재 기반 | 적용 방향 |
|------|-----------|-----------|
| 태그 | `GameplayTagContainer` | 참조 카운트 / 소스 기반 태그로 확장 |
| 스탯 | `ActorStatContainer`, `StatModifier` | AttributeSet 역할로 유지 |
| 행동 실행 | 상태 머신, `CombatActionRunner`, MotionSet | Ability는 실행을 지시하고, 상태와 MotionSet이 실제 수행 |
| 스킬 조건 | `PlayerSkillDefinition`, `SkillVariantCondition` | 초기에는 어댑터로 사용, 이후 Ability SO로 이전 |
| 피해 계산 | `CombatResolutionPipeline`, `DamageResolver`, `DefenseResolver` | 기존 파이프라인 유지, 결과를 Effect 적용으로 연결 |
| 전투 피드백 | `CombatFeedbackDispatcher`, MotionEvent | GameplayCue 개념으로 분리 가능 |

핵심 원칙:

- ASC는 **물리 이동과 애니메이션을 직접 처리하지 않는다.**
- KCC 상태 머신은 계속 이동 / 회전 / 상태별 물리의 소유자다.
- MotionSet 타임라인은 계속 애니메이션 이벤트와 충돌 판정 타이밍의 소유자다.
- ASC는 행동 가능 여부, 비용, 쿨다운, 태그, 지속 효과, 취소 관계를 통제한다.
- 싱글플레이 프로젝트이므로 언리얼 GAS의 네트워크 예측 / 복제 / 서버 권한 모델은 도입하지 않는다.

---

## 아키텍처

### 전체 흐름

```
입력 / Behavior Tree
        │
        ▼
ActorAbilitySystem.TryActivateAbility()
        │
        ├─ Required / Blocked Tag 검사
        ├─ 비용 지불 가능 여부 검사
        ├─ 쿨다운 검사
        └─ 취소 / 독점 정책 검사
        │
        ▼
GameplayAbility 실행
        │
        ├─ Player / Enemy 상태 전환 요청
        ├─ CombatActionRunner.StartAction()
        ├─ Granted Tag 부여
        └─ Cost / Cooldown Effect 적용
        │
        ▼
상태 머신 + MotionSet 실행
        │
        ├─ KCC 이동 / 회전
        ├─ MotionEvent 발화
        └─ Collision Event → CombatActionRunner
        │
        ▼
CombatResolutionPipeline
        │
        ├─ 방어 판정
        ├─ 피해 계산
        ├─ 리액션 결정
        └─ CombatResult 생성
        │
        ▼
GameplayEffect 적용
        │
        ├─ HP / Poise / Gauge 변경
        ├─ StatModifier 추가 / 제거
        ├─ GameplayTag 부여 / 제거
        └─ GameplayCue / UI / 로그 발화
```

### 책임 분리

| 레이어 | 책임 | 직접 하지 말아야 할 것 |
|--------|------|------------------------|
| `ActorAbilitySystem` | Ability 활성화, Effect 수명주기, 태그·쿨다운·비용 관리 | KCC 속도 계산, 애니메이션 직접 재생 |
| `GameplayAbilitySO` | 행동 조건, 실행 정책, 취소 정책, 비용·쿨다운 참조 | 구체 상태의 물리 처리 |
| `GameActorState` | 상태별 이동, 회전, 상태 종료 타이밍 | 비용 지불, 쿨다운 관리 |
| `CombatActionRunner` | MotionEvent 기반 액션 타임라인 상태 | Ability 조건 판정 |
| `CombatResolutionPipeline` | 방어 / 피해 / 리액션 표준 계산 | 버프 수명주기 관리 |
| `GameplayEffect` | 스탯 변경, 태그 부여, 주기 효과, 지속 시간 | 공격 판정 탐색 |

---

## 파일 구조 제안

```
Assets/02.Scripts/
├── Gameplay/
│   ├── Ability/
│   │   ├── ActorAbilitySystem.cs
│   │   ├── AbilityContext.cs
│   │   ├── AbilityExecution.cs
│   │   ├── AbilityActivationResult.cs
│   │   ├── GameplayAbilitySO.cs
│   │   ├── GameplayAbilityInstance.cs
│   │   └── GameplayAbilityPolicy.cs
│   │
│   ├── Effect/
│   │   ├── GameplayEffectSO.cs
│   │   ├── GameplayEffectSpec.cs
│   │   ├── GameplayEffectInstance.cs
│   │   ├── GameplayEffectHandle.cs
│   │   ├── GameplayEffectDurationType.cs
│   │   ├── GameplayEffectStackPolicy.cs
│   │   └── GameplayEffectModifierDefinition.cs
│   │
│   ├── Event/
│   │   ├── GameplayEventData.cs
│   │   └── GameplayEventRouter.cs
│   │
│   └── Cue/
│       ├── GameplayCueSO.cs
│       └── GameplayCueDispatcher.cs
│
├── GameActor/
│   └── Component/
│       └── Common/
│           └── ActorAbilitySystemBridge.cs    선택 사항: GameActor 초기화 연결용
│
└── Data/
    └── Ability/
        ├── PlayerAbilitySetSO.cs
        ├── EnemyAbilitySetSO.cs
        └── AbilityInputBindingSO.cs
```

데이터 에셋 경로:

```
Assets/10.Datas/
├── Ability/
│   ├── Player/
│   ├── Enemy/
│   └── Common/
└── Effect/
    ├── Buff/
    ├── Debuff/
    ├── Cost/
    └── Cooldown/
```

---

## 핵심 타입 스펙

### ActorAbilitySystem

모든 `GameActor`에 붙는 ASC 역할의 런타임 컴포넌트다.

```csharp
public sealed class ActorAbilitySystem : ActorComponent
{
    public ActorStatContainer Attributes { get; private set; }
    public GameplayTagContainer Tags { get; private set; }

    public bool TryActivateAbility(GameplayAbilitySO ability, in AbilityContext context);
    public GameplayEffectHandle ApplyEffect(GameplayEffectSpec spec);
    public void RemoveEffect(GameplayEffectHandle handle);
    public void CancelAbilitiesWithTag(GameplayTagId tagId);
    public void SendGameplayEvent(in GameplayEventData eventData);
}
```

| API | 역할 |
|-----|------|
| `TryActivateAbility` | 태그 / 비용 / 쿨다운 / 취소 정책 검사 후 Ability 실행 |
| `ApplyEffect` | Instant / Duration / Infinite Effect 적용 |
| `RemoveEffect` | 핸들 기반 Effect 제거 |
| `CancelAbilitiesWithTag` | 지정 태그를 가진 실행 중 Ability 취소 |
| `SendGameplayEvent` | 피격, 처치, 회피 성공 등 이벤트 전달 |

초기화 시 `GameActor`가 이미 보유한 `ActorStatContainer`, `GameplayTagContainer`, `CombatActionRunner`를 찾아 연결한다.

### GameplayAbilitySO

행동의 데이터 정의다. 플레이어 스킬, 회피, 가드, 적 공격, 보스 패턴을 같은 추상 개념으로 다룬다.

```csharp
public abstract class GameplayAbilitySO : ScriptableObject
{
    public GameplayTagId abilityTag;
    public List<GameplayTagId> requiredTagIds;
    public List<GameplayTagId> blockedTagIds;
    public List<GameplayTagId> grantedTagIds;

    public GameplayEffectSO costEffect;
    public GameplayEffectSO cooldownEffect;

    public AbilityCancelPolicy cancelPolicy;
    public AbilityActivationPolicy activationPolicy;

    public abstract bool CanActivate(in AbilityContext context);
    public abstract void Activate(AbilityExecution execution);
}
```

| 필드 | 의미 |
|------|------|
| `abilityTag` | Ability 식별 태그. 예: `Ability.Player.Skill1` |
| `requiredTagIds` | 모두 있어야 실행 가능 |
| `blockedTagIds` | 하나라도 있으면 실행 불가 |
| `grantedTagIds` | 실행 중 부여할 태그 |
| `costEffect` | 실행 시 즉시 적용할 비용 |
| `cooldownEffect` | 실행 성공 후 적용할 쿨다운 |
| `cancelPolicy` | 기존 Ability 취소 / 공존 규칙 |
| `activationPolicy` | 입력 즉시 / 입력 유지 / 이벤트 기반 실행 정책 |

### GameplayEffectSO

스탯 변경, 태그 부여, 지속 효과, 주기 효과를 표현한다.

```csharp
public sealed class GameplayEffectSO : ScriptableObject
{
    public GameplayEffectDurationType durationType;
    public float durationSeconds;
    public float periodSeconds;

    public List<GameplayEffectModifierDefinition> modifiers;
    public List<GameplayTagId> grantedTagIds;

    public GameplayTagId stackingKey;
    public GameplayEffectStackPolicy stackPolicy;
    public int maxStackCount;
}
```

| Duration | 의미 | 예시 |
|----------|------|------|
| `Instant` | 즉시 적용 후 종료 | 피해, 회복, 게이지 소모 |
| `Duration` | 일정 시간 유지 | 공격력 증가 10초, 독 5초 |
| `Infinite` | 명시적으로 제거될 때까지 유지 | 장비 보너스, 패시브 |

`GameplayEffect`는 내부적으로 기존 `StatModifier`를 생성해 `ActorStatContainer.AddModifier()`에 연결한다.
단, HP / Poise / SkillGauge처럼 현재값이 있는 자원은 `ActorStatContainer`의 기본 스탯과 별도 런타임 리소스 컴포넌트를 통해 처리해야 한다.

### GameplayTagContainer 확장

현재 `GameplayTagContainer`는 `HashSet<GameplayTag>` 기반이다. ASC 적용 시 다음 문제가 생긴다.

```
Effect A → State.SuperArmor 부여
Effect B → State.SuperArmor 부여
Effect A 만료 → State.SuperArmor 제거
Effect B는 아직 남아 있는데 태그가 사라짐
```

따라서 태그는 참조 카운트 방식으로 확장한다.

```csharp
private readonly Dictionary<GameplayTag, int> _tagCounts = new();

public GameplayTagHandle AddTag(GameplayTag tag, object source);
public void RemoveTag(GameplayTagHandle handle);
public void RemoveTagsBySource(object source);
```

| 기능 | 필요 이유 |
|------|-----------|
| 참조 카운트 | 여러 Effect / 상태가 같은 태그를 부여해도 안전하게 제거 |
| source 추적 | 상태 종료, Effect 만료, 캐릭터 교체 시 일괄 정리 |
| handle 제거 | 특정 적용 인스턴스만 정확히 제거 |

기존 `AddTag(GameplayTagId)` / `RemoveTag(GameplayTagId)` API는 유지하되, 내부 구현만 스택형으로 바꾼다.

---

## 기존 시스템 연동

### PlayerSkillDefinition 어댑터

초기 단계에서는 `PlayerSkillDefinition`을 즉시 제거하지 않는다. `PlayerSkillResolver.TryResolve()`를 그대로 사용해 Variant를 고르고, 선택된 결과를 Ability 실행으로 감싼다.

```
Player 입력
    │
    ▼
PlayerSkillResolver.TryResolve()
    │
    ▼
ActorAbilitySystem.TryActivateAbility()
    │
    ▼
PlayerAttackState / PlayerSkill 상태 전환
```

이 방식은 기존 스킬 데이터와 UI를 보존하면서 ASC의 비용 / 쿨다운 / 태그 검사를 점진적으로 붙일 수 있다.

### CombatResolutionPipeline 연동

피해 계산은 기존 파이프라인을 유지한다.

```
IDamageable.TakeDamage()
    │
    ▼
CombatResolutionPipeline.ResolvePlayerHit / ResolveMonsterHit
    │
    ▼
CombatResult
    │
    ▼
ActorAbilitySystem.ApplyEffect(DamageEffectSpec)
```

주의할 점:

- `DamageResolver`의 피해 공식은 중복 구현하지 않는다.
- 방어 / 회피 / 패리 판정도 기존 `DefenseResolver`를 유지한다.
- ASC는 최종 결과를 받아 리소스 변경, 태그, Cue, 로그를 연결한다.

### 상태 머신 연동

Ability는 상태를 대체하지 않는다.

예: 회피

```
DodgeAbility
├─ `State.Stunned`, `State.Dead` 차단
├─ 비용 / 쿨다운 검사
├─ `Ability.Dodge` 태그 부여
└─ PlayerDodgeState 전환 요청

PlayerDodgeState
├─ KCC 이동 / 회전 처리
├─ 무적 MotionEvent 처리
└─ 종료 시 Ability 완료 통보
```

상태 종료 시에는 `ActorAbilitySystem.EndAbility()` 같은 완료 통보가 필요하다.
이 통보가 없으면 실행 중 태그, 비용 예약, 취소 잠금이 남아 버그가 된다.

### Behavior Tree 연동

적 AI는 기존 BT 노드가 상태를 직접 만들기보다 Ability 요청을 만드는 방향으로 점진 변경한다.

기존:

```
TransitionEnemyStateNode → EnemyAttackState 생성
```

목표:

```
ExecuteEnemyAbilityNode
    ├─ Ability 후보 선택
    ├─ ActorAbilitySystem.TryActivateAbility()
    └─ 실패 사유를 Blackboard / Debug Trace에 기록
```

초기에는 공격 Ability만 연결하고, 순찰 / 추격 / 거리 유지 같은 이동 전술 상태는 기존 BT 상태 전환을 유지한다.

---

## 데이터 예시

### 플레이어 스킬 Ability

| 필드 | 값 예시 |
|------|---------|
| `abilityTag` | `Ability.Player.Skill.AbilitySlot` |
| `requiredTagIds` | `State.Grounded` |
| `blockedTagIds` | `State.Dead`, `State.Stunned`, `State.Grabbed`, `Cooldown.Player.Skill.AbilitySlot` |
| `grantedTagIds` | `Ability.Active`, `Ability.Player.Skill` |
| `costEffect` | `GE_Cost_PlayerAbilityGauge` |
| `cooldownEffect` | `GE_Cooldown_PlayerAbility` |
| 실행 | 선택된 `PlayerAttackInfo`로 `PlayerAttackState` 진입 |

### 적 공격 Ability

| 필드 | 값 예시 |
|------|---------|
| `abilityTag` | `Ability.Enemy.Attack.Melee` |
| `requiredTagIds` | 없음 |
| `blockedTagIds` | `State.Dead`, `State.Stunned`, `State.Knockdown`, `Cooldown.Enemy.Attack.Melee` |
| `grantedTagIds` | `Ability.Active`, `State.Attacking` |
| `cooldownEffect` | `GE_Cooldown_EnemyMeleeAttack` |
| 실행 | `EnemyAttackState` 진입 + `EnemyAttackInfo` 전달 |

### 버프 Effect

| 필드 | 값 예시 |
|------|---------|
| `durationType` | `Duration` |
| `durationSeconds` | `10` |
| `modifiers` | `AttackPower Percent +0.2` |
| `grantedTagIds` | `Buff.AttackUp` |
| `stackPolicy` | `RefreshDuration` |
| `maxStackCount` | `1` |

---

## 구현 단계

### Phase 1 — 태그와 Effect 기반 정리

1. `GameplayTagContainer`를 참조 카운트 / source 기반으로 확장한다.
2. `GameplayEffectSO`, `GameplayEffectSpec`, `GameplayEffectInstance`, `GameplayEffectHandle`을 추가한다.
3. `ActorStatContainer`의 `StatModifier` 적용 / 제거를 Effect 인스턴스에서 호출한다.
4. Duration / Infinite Effect 만료 처리를 `ActorAbilitySystem.Update()`에서 수행한다.

검증 기준:

- 같은 태그를 여러 소스가 부여해도 한 소스 제거로 태그가 사라지지 않는다.
- Duration Effect가 만료되면 StatModifier와 GrantedTag가 모두 제거된다.
- Infinite Effect는 핸들 제거 전까지 유지된다.

### Phase 2 — 플레이어 스킬 1개 수직 슬라이스

1. `ActorAbilitySystem`을 `PlayerActor`에 자동 부착 / 초기화한다.
2. Ability Slot 1개를 기존 `PlayerSkillResolver` 결과와 연결한다.
3. 비용 / 쿨다운을 `GameplayEffect`로 적용한다.
4. `PlayerAttackState` 또는 스킬 상태 종료 시 Ability 종료를 통보한다.

검증 기준:

- 게이지 부족 시 스킬이 실행되지 않는다.
- 실행 중 `Ability.Active` 태그가 붙고 종료 시 제거된다.
- 쿨다운 태그가 유지되는 동안 재사용할 수 없다.
- 기존 MotionSet / 충돌 판정 / 피해 계산은 그대로 동작한다.

### Phase 3 — 회피 / 가드 / 캔슬 정책 통합

1. 회피, 가드, 대시를 Ability로 감싼다.
2. `PlayerInterruptResolver`가 직접 상태 전환하기 전에 `ActorAbilitySystem`에 실행 가능 여부를 묻는다.
3. 공격 캔슬 가능 여부는 기존 `PlayerInterruptAction`과 Ability blocked tag를 함께 사용한다.

검증 기준:

- 액티브 히트 중 캔슬 금지 규칙이 유지된다.
- 후딜 이동 캔슬 규칙이 유지된다.
- 스턴 / 잡힘 / 사망 태그 중에는 회피와 가드가 차단된다.

### Phase 4 — 적 공격 Ability화

1. `EnemyAttackInfo`를 참조하는 `EnemyAttackAbilitySO`를 추가한다.
2. BT 공격 노드가 Ability 실행을 요청하도록 변경한다.
3. 적 스킬 쿨다운을 `_skillCooldowns`에서 Cooldown Effect로 점진 이전한다.

검증 기준:

- 기존 BT 선택 확률과 거리 조건이 유지된다.
- 쿨다운 중인 공격은 후보에서 제외된다.
- Debug Trace에서 실패 이유를 확인할 수 있다.

### Phase 5 — GameplayCue 분리

1. Effect / Ability 결과에서 VFX, SFX, 카메라, UI 피드백을 Cue로 발화한다.
2. 기존 `CombatFeedbackDispatcher`는 Cue 발화의 하위 구현으로 유지한다.

검증 기준:

- 피해, 회피, 패리, 브레이크, 처치 피드백이 데이터 기반으로 라우팅된다.
- 전투 계산 코드에 VFX / SFX 직접 참조가 늘어나지 않는다.

---

## 주의 사항

### ASC가 상태 머신을 잡아먹으면 안 된다

이 프로젝트의 상태는 KCC 콜백과 강하게 결합되어 있다.
Ability가 이동 속도, 회전, 접지 처리까지 직접 소유하면 기존 안정성을 잃는다.

권장 경계:

- Ability: "이 행동을 시작해도 되는가?"
- State: "이 행동 중 몸은 어떻게 움직이는가?"
- MotionSet: "언제 충돌 / 이펙트 / 사운드가 발생하는가?"

### ScriptableObject에 런타임 상태를 저장하지 않는다

`GameplayAbilitySO`, `GameplayEffectSO`는 정의 데이터만 가진다.
쿨다운 남은 시간, 스택 수, 적용자, 대상, 핸들은 반드시 런타임 인스턴스에 저장한다.

```
SO = definition
Instance = runtime state
Spec = source / target / level / captured values
Handle = later removal key
```

### 태그 제거는 반드시 소유권 기반이어야 한다

상태 진입, Effect 적용, Ability 실행이 모두 태그를 부여할 수 있다.
단순 `RemoveTag(GameplayTagId)`는 다른 시스템의 태그까지 제거할 수 있으므로, ASC 내부에서는 handle/source 기반 제거만 사용한다.

### 비용과 쿨다운 적용 타이밍을 통일한다

초기 권장 정책:

- 조건 검사 성공
- 상태 전환 가능 확인
- Ability 실행 시작
- 비용 즉시 적용
- 쿨다운 즉시 적용
- 실행 실패 시 비용 / 쿨다운 롤백

상태 전환이 실패할 수 있는 구조이므로, 비용을 먼저 차감하면 실패 시 복구가 필요하다.

### HP / Poise / Gauge는 Attribute와 Current Resource를 분리한다

`ActorStatContainer.MaxHealth`는 최대 HP다.
현재 HP, 현재 Poise, 현재 SkillGauge는 별도 런타임 값이다.

Effect 설계 시 다음을 구분한다.

| 종류 | 예시 | 저장 위치 |
|------|------|-----------|
| 최대치 / 배율 | MaxHealth, AttackPower, Defense | `ActorStatContainer` |
| 현재값 | CurrentHealth, CurrentPoise, CurrentSkillGauge | 전용 Resource 컴포넌트 |
| 변경 이벤트 | 피해, 회복, 게이지 충전 | Effect 또는 CombatResult |

---

## 확장 포인트

### Ability Set

캐릭터 / 몬스터별 Ability 목록을 묶는 SO를 둔다.

```csharp
public sealed class AbilitySetSO : ScriptableObject
{
    public List<GameplayAbilitySO> abilities;
    public List<GameplayEffectSO> passiveEffects;
}
```

활용처:

- `CharacterModelData` 교체 시 플레이어 Ability Set 교체
- `ActorDefinitionSO` 또는 몬스터 데이터에서 Enemy Ability Set 참조
- 보스 페이즈 진입 시 Ability 후보군 추가 / 제거

### Event 기반 Ability

특정 이벤트에 반응하는 Ability를 지원할 수 있다.

예:

- `Event.Hit.Received` → 반격 가능 창 열기
- `Event.Dodge.Perfect` → 회피 카운터 활성화
- `Event.Enemy.Broken` → 특수공격 입력 허용
- `Event.Kill` → 게이지 회복

### GameplayCue

전투 결과와 표현을 분리한다.

| Cue | 발화 조건 |
|-----|-----------|
| `Cue.Hit.Light` | 일반 피격 |
| `Cue.Hit.Critical` | 치명타 |
| `Cue.Defense.Parry` | 패리 성공 |
| `Cue.Dodge.Perfect` | 퍼펙트 회피 |
| `Cue.Buff.AttackUp` | 공격력 증가 적용 |
| `Cue.Cooldown.Ready` | 쿨다운 종료 |

---

## 우선순위 결론

가장 먼저 구현할 것은 거대한 Ability 프레임워크가 아니라 다음 세 가지다.

1. `GameplayTagContainer`의 참조 카운트화
2. `GameplayEffect`의 Duration / Infinite 수명주기
3. 플레이어 스킬 1개를 `ActorAbilitySystem` 경유로 실행하는 수직 슬라이스

이 세 가지가 안정되면 공격, 회피, 가드, 적 스킬, 버프 / 디버프를 같은 규칙으로 확장할 수 있다.
반대로 이 단계 없이 모든 행동을 한 번에 Ability로 이전하면 상태 머신, MotionSet, 전투 파이프라인 사이에 중복 책임이 생긴다.
