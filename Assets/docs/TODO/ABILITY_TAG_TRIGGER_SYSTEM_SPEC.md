# Ability 태그 트리거 시스템 설계서

Tag 부여/제거를 계기로 Ability를 자동 활성화하고, 활성화 가능 여부를
Tag 쿼리로 검사하는 구조를 정의한다. **언리얼 GAS를 레퍼런스로 한다.**

대상 파일: `ActorAbilitySystem.cs`, `AbilityExecution.cs`, `GameplayAbilitySO.cs`,
`AbilityDefinitions.cs`, `AbilityRuntimePorts.cs`, `AbilityCoreTypes.cs`.

---

## 1. 현재 상태

### 1.1 이미 있는 것

| 기능 | 위치 | 비고 |
| --- | --- | --- |
| 태그 게이팅(required/blocked) | `ActorAbilitySystem.Evaluate()` `:614-618` | 런타임에서 실제 평가됨 |
| Variant 선택 태그 조건 | `ActorAbilitySystem.ResolveVariant()` `:661-662` | 동일 방식 |
| 계층 매칭 | `GameplayTagAggregator.Has(tag, matchHierarchy=true)` `:126` | 기본 ON |
| 태그 변경 이벤트 | `GameplayTagAggregator.TagAdded/TagRemoved` `:78-79` | 구독처 2곳뿐 |
| 태그 대기 Task | `WaitTagTask` (`AbilityTaskRuntime.cs:510`) | **실행 중** Ability 내부 대기 |
| 태그 쿼리 타입 | `GameplayTagQuery` / `Matches()` `:41-155` | All/Any/None + 계층 |
| 이벤트 페이로드 | `GameplayEventData` (`GameplayEventRuntime.cs:26-53`) | UE `FGameplayEventData` 대응 |
| GE → 태그 부여 | `ActiveGameplayEffectContainer.cs:331-336` | `_owner.Tags.Add(...)` |
| ASC 핸들 역참조 | `AbilitySystemComponent.TryResolve()` `:118` | Instigator 핸들 → 컴포넌트 |
| 타 액터 ASC 접근 | `GameActor.AbilitySystem` (`GameActor.cs:70`) | public |

**중요:** `GameplayTagQuery`는 미사용이 아니다. Effect가 이미
`ApplicationRequirement` / `ImmunityQuery`로 쓰고 있다
(`GameplayEffectSpec.cs:345-346`). Ability만 쓰지 않는다. 새 쿼리 타입을
만들지 말고 이 선례를 따른다.

**GE → 태그 → 트리거 사슬은 이미 성립한다.** `grantedTagIds`가 들어가는
`_owner.Tags`가 트리거가 구독할 바로 그 `GameplayTagAggregator`다.

### 1.2 없는 것

- 태그 → Ability 자동 활성화 경로. `TagAdded` 구독자는 디버그 레코더와
  `WaitTagTask`뿐이고, 활성화 진입점은 전부 명시 호출이다
  (`PlayerCombat`, `PlayerAttackState`, `EnemyCombat.TryActivateAbility`).
- 조건 표현력. `requiredTagIds`(AND) + `blockedTagIds`(NONE) 두 리스트가 전부라
  "A 또는 B", "정확 일치"를 표현할 수 없다.
- 아래 UE 대조표의 ❌ 항목 전부.

### 1.3 UE GAS 대조

| UE GAS | 현재 | 목표 단계 |
| --- | --- | --- |
| `ActivationRequiredTags` / `ActivationBlockedTags` | ✅ | 1 (확장) |
| `ActivationOwnedTags` | ✅ `executionGrantedTagIds` | — |
| `AbilityTags` | ✅ `abilityTagIds` | — |
| `TriggerSource.GameplayEvent` | ❌ | 2·3 |
| `TriggerSource.OwnedTagAdded` | ❌ | 2·3 |
| `TriggerSource.OwnedTagPresent` (+ 소실 시 취소) | ❌ | 2·3 |
| `ActivateAbilityFromEvent` 페이로드 수신 | ❌ | 3 |
| `SourceRequiredTags` / `SourceBlockedTags` | ❌ | 6 |
| `TargetRequiredTags` / `TargetBlockedTags` | ❌ | 6 |
| `CancelAbilitiesWithTag` | ❌ | 7 |
| `BlockAbilitiesWithTag` | ❌ | 7 |
| `FScopedAbilityListLock` 재진입 보호 | ❌ | 3 (큐 방식) |
| `FGameplayTagQuery` 중첩 표현식 | ❌ | **범위 밖 (§9)** |
| `InstancingPolicy` | ❌ | **범위 밖 (§9)** |

---

## 2. 설계 원칙

1. **기존 482개 Ability 에셋을 마이그레이션하지 않는다.** 새 필드는 전부
   추가 필드이고, 비어 있으면 현재 동작과 100% 동일하다.
2. **Prepare → 외부 검증 → Commit 계약을 깨지 않는다.** 이건 UE에 없는
   프로젝트 고유 강점이다 (UE는 `CommitAbility` 단일 호출). "모션 해석 실패가
   현재 실행 중인 Ability를 끊지 않게" 하는 원자성이므로, UE를 레퍼런스로
   삼는다는 이유로 단순화하지 않는다. §5의 `Immediate`/`Request` 분리는
   이 계약에서 파생된 것이라 UE에 대응물이 없는 게 정상이다.
3. **트리거는 활성화 검사를 우회하지 않는다.** UE `TryActivateAbility`가
   `CanActivateAbility` 전체를 통과시키는 것과 동일하다. 트리거는 활성화
   *시도*를 유발할 뿐이다.
4. **재진입은 데이터 실수로 반드시 발생한다고 가정한다.** 런타임 가드와
   에디터 검증을 둘 다 넣는다.
5. Core asmdef(`UPlayGround.Ability.Core`)에 프로젝트 타입을 새로 넣지 않는다.

---

## 3. 데이터 정의 (전 단계 공통)

### 3.1 태그 조건

`AbilityDefinitions.cs`에 추가한다.

```csharp
public enum AbilityTagMatchMode
{
    /// <summary>하위 계층 태그도 조건을 만족시킨다. (State.Combat ← State.Combat.Attack)</summary>
    Hierarchy,
    /// <summary>태그 문자열이 정확히 일치해야 한다.</summary>
    Exact,
}

[Serializable]
public sealed class AbilityTagRequirement
{
    [Tooltip("전부 보유해야 활성화된다. (AND)")]
    public List<GameplayTag> requireAll = new();
    [Tooltip("하나라도 보유하면 활성화된다. 비어 있으면 검사하지 않는다. (OR)")]
    public List<GameplayTag> requireAny = new();
    [Tooltip("하나라도 보유하면 차단한다. (NONE)")]
    public List<GameplayTag> blockAny = new();
    public AbilityTagMatchMode matchMode = AbilityTagMatchMode.Hierarchy;

    public bool IsEmpty =>
        (requireAll?.Count ?? 0) == 0
        && (requireAny?.Count ?? 0) == 0
        && (blockAny?.Count ?? 0) == 0;
}
```

`AbilityActivationRules`는 **기존 두 리스트를 남긴 채** 필드를 추가한다.

```csharp
public sealed class AbilityActivationRules
{
    public List<GameplayTag> requiredTagIds = new();   // 유지 (레거시 = requireAll)
    public List<GameplayTag> blockedTagIds  = new();   // 유지 (레거시 = blockAny)
    public List<GameplayTag> executionGrantedTagIds = new();
    public AbilityTagRequirement ownerTagRequirement  = new();  // 1단계
    public AbilityTagRequirement sourceTagRequirement = new();  // 6단계
    public AbilityTagRequirement targetTagRequirement = new();  // 6단계
    // ... 기존 ground/target/distance 필드 그대로
}
```

`AbilityVariantCondition`에도 `ownerTagRequirement`를 추가한다.

### 3.2 트리거

```csharp
public enum AbilityTriggerSource
{
    /// <summary>소유 태그가 새로 추가되는 순간 1회.</summary>
    OwnedTagAdded,
    /// <summary>태그가 붙으면 활성화하고 태그가 사라지면 취소한다.
    /// UE OwnedTagPresent 대응. 오라·지속 상태용.</summary>
    OwnedTagPresent,
    /// <summary>GameplayEventRouter로 전달된 이벤트 태그.</summary>
    GameplayEvent,
}

public enum AbilityTriggerActivationMode
{
    /// <summary>ActorAbilitySystem이 Prepare+Commit을 직접 수행한다.
    /// concurrency == Background 인 Ability만 허용한다.</summary>
    Immediate,
    /// <summary>활성화 요청만 발행한다. PlayerCombat / EnemyCombat이
    /// 자신의 상태 전환 규칙에 따라 Prepare→전환→Commit을 수행한다.</summary>
    Request,
}

[Serializable]
public sealed class AbilityTriggerDefinition
{
    public GameplayTag triggerTag;
    public AbilityTriggerSource source = AbilityTriggerSource.OwnedTagAdded;
    public AbilityTriggerActivationMode mode = AbilityTriggerActivationMode.Immediate;
    public AbilityTagMatchMode matchMode = AbilityTagMatchMode.Exact;
    [Tooltip("같은 프레임에 여러 트리거가 걸리면 높은 값이 먼저 처리된다.")]
    public int priority;
    [Min(0f)]
    [Tooltip("트리거로 재활성화되기까지의 최소 간격. 쿨다운과 별개다. "
             + "OwnedTagPresent에는 적용되지 않는다.")]
    public float retriggerIntervalSeconds;
}
```

`GameplayAbilitySO`에 추가:

```csharp
public List<AbilityTriggerDefinition> triggers = new();
public List<GameplayTag> cancelAbilitiesWithTag = new();  // 7단계
public List<GameplayTag> blockAbilitiesWithTag  = new();  // 7단계
```

**트리거 기본 `matchMode`가 §3.1과 반대로 `Exact`인 이유:** 조건 검사는
"전투 중이면" 같은 범주 질의라 계층 매칭이 자연스럽지만, 트리거는
"정확히 이 사건"이어야 오발화가 없다. 계층 트리거는 명시적으로 켠다.

### 3.3 결과 코드

`AbilityCoreTypes.cs`의 `AbilityActivationResult`에 하나만 추가한다.

```csharp
BlockedByActiveAbility,   // 7단계: blockAbilitiesWithTag에 걸림
```

`requireAny` 실패는 기존 `MissingRequiredTag`, Source/Target 조건 실패는
기존 `MissingRequiredTag`/`BlockedByTag`를 재사용한다.
`Request` 모드에서 구독자가 없으면 기존 `StateTransitionRejected`를 쓴다.

---

## 4. 단계별 작업 계획

각 단계는 **독립적으로 컴파일·테스트 가능**하고, 그 단계까지만 적용해도
기존 동작이 깨지지 않는다. 단계 끝의 "완료 판정"을 통과해야 다음으로 넘어간다.

---

### 1단계 — 활성화 조건의 태그 쿼리화

**목표:** 태그 조건 표현력을 UE 수준으로 올린다. 트리거는 아직 없다.

**작업**

1. `AbilityDefinitions.cs`에 `AbilityTagMatchMode`, `AbilityTagRequirement` 추가.
2. `AbilityActivationRules.ownerTagRequirement`,
   `AbilityVariantCondition.ownerTagRequirement` 필드 추가.
3. `IAbilityTagPort`(`AbilityRuntimePorts.cs`)에 오버로드 추가:

   ```csharp
   bool Has(string tagId);                          // 기존 (= Hierarchy)
   bool Has(string tagId, bool matchHierarchy);     // 신규
   ```

   `UPlayGroundAbilityOwnerPorts.Has`는 `_abilitySystem.Tags.Has(id, matchHierarchy)`로
   넘긴다. `GameplayTagAggregator`가 이미 `HasExact` / `Has(tag, bool)`를 갖고
   있어 Core 변경은 인터페이스 1줄뿐이다.

4. `ActorAbilitySystem`에 `EvaluateTagRequirement(AbilityTagRequirement, IAbilityTagPort)`
   추가. 레거시 `requiredTagIds`/`blockedTagIds`는 `Hierarchy` 모드로 취급한다
   (현재 동작과 동일).
5. `Evaluate()` `:614-618`을 교체:

   ```csharp
   AbilityActivationRules activation = definition.activation ?? new AbilityActivationRules();
   AbilityTagEvaluation tagResult = EvaluateOwnerTags(activation);
   if (tagResult != AbilityTagEvaluation.Pass)
       return tagResult == AbilityTagEvaluation.MissingRequired
           ? AbilityActivationResult.MissingRequiredTag
           : AbilityActivationResult.BlockedByTag;
   ```

   `ResolveVariant()` `:661-662`도 동일하게 처리한다.

> `GameplayTagQuery.Matches()`를 직접 쓰지 않고 포트를 통하는 이유:
> `ActorAbilitySystem`은 `IAbilityTagPort` 뒤에 있어 `GameplayTagAggregator`를
> 직접 참조하지 않는다. 이 경계를 유지한다.

**테스트 (EditMode)**

- T1-1 `requireAny` 중 하나만 보유해도 통과, 하나도 없으면 `MissingRequiredTag`.
- T1-2 `matchMode = Exact`일 때 하위 계층 태그로는 조건이 충족되지 않는다.
- T1-3 레거시 `requiredTagIds`만 채운 기존 에셋의 판정 결과가 변경 전과 동일하다.

**완료 판정:** 기존 Ability 테스트 전부 통과 + T1-1~3. 에셋 변경 0건.

---

### 2단계 — 트리거 데이터 타입

**목표:** 저작 가능한 데이터만 추가한다. 런타임 동작은 아직 없다.

**작업**

1. `AbilityTriggerSource`, `AbilityTriggerActivationMode`,
   `AbilityTriggerDefinition` 추가 (§3.2).
2. `GameplayAbilitySO.triggers` 필드 추가.
3. `AbilityActivationResult.BlockedByActiveAbility` 추가 (§3.3).
   7단계까지 미사용이지만 Core enum 변경을 한 번으로 끝낸다.

**완료 판정:** 컴파일 통과. 기존 에셋 직렬화 변화 없음(빈 리스트).
`triggers`를 채워도 아직 아무 일도 일어나지 않는 것이 정상이다.

---

### 3단계 — 트리거 런타임 (핵심)

**목표:** `Immediate` 경로를 완성한다. 이 단계까지 하면 버프·오라 계열이
**켜지고 꺼진다.**

신규 파일 `ActorAbilitySystem.Triggers.cs` (partial).

#### 3-A. 인덱스

```csharp
private readonly Dictionary<string, List<TriggerEntry>> _exactTriggers = new();
private readonly List<TriggerEntry> _hierarchyTriggers = new();
private readonly Dictionary<GameplayAbilitySO, float> _lastTriggerTime = new();
private readonly Queue<PendingTrigger> _pendingTriggers = new();
private int _triggerDepth;
private bool _draining;
```

인덱스는 `SetAbilitySet()`과 임시 Ability 부여·회수(`_temporaryAbilities` 변경)
시점에 재구축한다. 매 프레임 순회하지 않는다.

`Initialize()`에서 구독하고 `Dispose()`에서 **반드시 해제한다.**

```csharp
_abilitySystem.Runtime.Tags.TagAdded    += OnTagAddedForTrigger;
_abilitySystem.Runtime.Tags.TagRemoved  += OnTagRemovedForTrigger;
_abilitySystem.Runtime.Events.EventSent += OnEventForTrigger;
```

#### 3-B. 발화 흐름

```
TagAdded(tag)
  └ 인덱스 조회 → 후보 (ability, trigger) 목록
     └ _pendingTriggers 에 enqueue          ← 콜백 안에서 즉시 실행하지 않음
        └ Drain()  (이미 _draining 이면 즉시 반환)
           └ priority 내림차순 처리
              ├ 게이트 검사 (3-C)
              ├ Immediate → TryPrepareAbility + Commit
              └ Request   → AbilityTriggerRequested 발행
```

**콜백 안에서 즉시 실행하지 않는 이유:** `Commit()`은
`AddExecutionTags()`(`:856`)로 `executionGrantedTagIds`를 부여하고, 이는 다시
`TagAdded`를 발화한다. 즉 트리거 처리 도중 `_executions` 딕셔너리가 변경된다.
UE가 `FScopedAbilityListLock`으로 푸는 문제와 같고, 큐 + `_draining` 플래그로
직렬화한다.

#### 3-C. 게이트 (순서대로)

1. **자기 발화 차단** — 트리거 태그가 그 Ability 자신의
   `executionGrantedTagIds`에 있으면 스킵. (에디터에서도 Error)
2. **중복 실행 차단** — 해당 Ability의 활성 실행이 이미 있으면 스킵.
3. **재트리거 간격** — `Time.time < _lastTriggerTime + retriggerIntervalSeconds`면
   스킵. `OwnedTagPresent`는 이 게이트를 건너뛴다(게이트 2가 이미 막는다).
4. **깊이 제한** — `_triggerDepth > MaxTriggerDepth`(4)면 스킵하고
   `Debug.LogWarning` 1회. 무한 루프의 최종 방어선.
5. **일반 활성화 검사** — `Evaluate()`. 태그 조건·쿨다운·자원·ground/target이
   전부 여기서 걸린다.

게이트 1~4는 트리거 전용이고, 5는 수동 활성화와 완전히 같은 경로다.
**"태그로 활성화"와 "태그로 활성화 가능 여부 검사"가 같은 `Evaluate()`를
통과한다** — 이 설계의 핵심이자 원칙 3의 구현이다.

#### 3-D. `OwnedTagPresent`의 취소 경로

`AbilityExecution`에 필드를 추가한다.

```csharp
public GameplayTag TriggerTag { get; internal set; }
public AbilityTriggerSource TriggerSource { get; internal set; }
public GameplayEventData? TriggerEvent { get; internal set; }
```

`TagRemoved(tag)` 처리:

```
활성 실행 중 TriggerSource == OwnedTagPresent 이고
TriggerTag 가 매칭되는 것을 찾아
  ├ Immediate 였으면 → EndExecution(handle, completed: false, "TriggerTagLost")
  └ Request  였으면 → AbilityTriggerCancelRequested 발행
```

취소도 `_pendingTriggers` 큐를 거친다. `TagRemoved` 콜백 안에서 `EndExecution`을
직접 부르면 `CleanupExecution`이 다시 태그를 제거하며 재진입한다.

> **이 항목이 3단계의 존재 이유다.** `OwnedTagPresent` 없이
> `Immediate`+`Background`만 있으면 오라를 켤 수는 있어도 끌 수가 없다.

#### 3-E. 페이로드 전달 (UE `ActivateAbilityFromEvent`)

`GameplayEvent` 트리거는 `GameplayEventData`를 `AbilityExecution.TriggerEvent`에
저장하고, 읽기 API를 노출한다.

```csharp
public bool TryGetTriggerEvent(
    AbilityExecutionHandle handle, out GameplayEventData data);
```

`GameplayEventData.Instigator`는 `AbilitySystemHandle`이므로
`AbilitySystemComponent.TryResolve()`(`:118`)로 액터를 역참조할 수 있다.
"나를 때린 대상에게 반격" 패턴이 여기서 성립한다.

#### 3-F. Immediate는 Background 전용

| | Immediate | Request |
| --- | --- | --- |
| 허용 concurrency | `Background` 전용 | 전부 |
| Prepare/Commit 주체 | `ActorAbilitySystem` | `PlayerCombat` / `EnemyCombat` |
| 상태 머신 전환 | 없음 | 필요 |
| 용도 | 버프·오라·패시브 Task Graph | 모션이 있는 반격/피니시 |

비-Background Ability는 Commit 후 모션 상태 전환이 반드시 따라와야 하는데
`ActorAbilitySystem`은 상태 머신을 소유하지 않는다. 여기서 Commit하면
"쿨다운·자원은 소모됐는데 모션이 없는" 상태가 만들어진다.
이 조합은 6단계 Validator에서 **Error**로 막는다.

**테스트 (EditMode)**

- T3-1 `OwnedTagAdded` + Immediate + Background → 태그 부여 시 실행이 Active.
- T3-2 트리거 태그가 붙어도 `blockAny` 태그를 보유하면 활성화되지 않는다.
  (트리거가 검사를 우회하지 않음을 고정)
- T3-3 `matchMode = Exact`일 때 하위 계층 태그로는 트리거되지 않는다.
- T3-4 자기 `executionGrantedTagIds`를 트리거로 삼은 Ability는 1회만 실행되고
  무한 루프하지 않는다.
- T3-5 `retriggerIntervalSeconds` 이내 재부여는 무시된다.
- T3-6 **`OwnedTagPresent`: 태그 부여 시 활성화, 태그 제거 시 Cancelled.**
- T3-7 GE가 부여한 태그로 트리거가 발화한다 (GE → 태그 → Ability 사슬).
- T3-8 `GameplayEvent` 트리거의 `TriggerEvent.Instigator`가 실행에서 읽힌다.

**완료 판정:** T3-1~8 통과. Play Mode에서 오라 Ability 하나를 태그로
켜고 끄는 것을 육안 확인.

---

### 4단계 — Request 이벤트 + EnemyCombat 구독

**목표:** 모션이 있는 Ability를 몬스터에서 트리거로 활성화한다.

```csharp
public event Action<AbilityTriggerRequest> AbilityTriggerRequested;
public event Action<AbilityExecutionHandle> AbilityTriggerCancelRequested;

public readonly struct AbilityTriggerRequest
{
    public readonly GameplayAbilitySO Ability;
    public readonly AbilityVariantDefinition Variant;
    public readonly GameplayTag TriggerTag;
    public readonly AbilityTriggerSource Source;
    public readonly GameplayEventData? TriggerEvent;
}
```

구독자가 없으면 요청을 폐기하고
`RecordActivationResult(AbilityActivationResult.StateTransitionRejected)`로
집계한다(`_activationFailureCounts`로 디버그 노출).

`EnemyCombat`은 기존 `TryActivateAbility`(`:671`)를 그대로 호출한다.
BT가 선택한 Ability와 트리거가 요청한 Ability가 경합하면 **BT 쪽이 우선**한다
(트리거는 `HasActiveAbility`면 게이트 2에서 이미 걸린다).

**테스트**

- T4-1 구독자가 없으면 실행이 생성되지 않고 `StateTransitionRejected`가 집계된다.
- T4-2 `EnemyCombat` 구독 상태에서 트리거 요청이 실제 실행으로 이어진다.

**완료 판정:** T4-1~2 + 몬스터 1종에 트리거 Ability를 붙여 Play Mode 확인.

---

### 5단계 — PlayerCombat 구독

**목표:** 플레이어 쪽 Request 경로를 연결한다.

`PlayerCombat`은 현재 상태의 `CanTransitionState` 결과에 따라 요청을
수락/거부한다. 거부는 정상 동작이며 로그를 남기지 않는다(매 프레임 발생 가능).

`AbilityTriggerCancelRequested`는 해당 실행이 구동 중인 상태를 종료시킨다.

> 이 단계는 트리거 시스템이 아니라 `PlayerCombat`의 책임 범위다.
> 상태 전환 규칙을 건드리므로 4단계와 분리해 검증한다.

**완료 판정:** Play Mode 수동 검증. 자동 테스트는 두지 않는다
(상태 머신 전환은 PlayMode 수직 슬라이스 영역).

---

### 6단계 — Source/Target 태그 조건 + 검증 + 에디터

**목표:** UE의 3자(owner/source/target) 태그 검사를 완성하고 저작 도구를 붙인다.

#### 6-A. Source/Target 조건

`Evaluate()`는 이미 `resolvedTarget`을 갖고 있으므로(`:621-627`) 추가 비용이 거의 없다.

- **Target** — `target.AbilitySystem.Tags`로 검사. 타깃이 없으면 조건을 건너뛴다
  (`targetPolicy == Required`가 이미 null을 걸러낸다).
- **Source** — 트리거로 활성화된 경우 `TriggerEvent.Instigator`를
  `AbilitySystemComponent.TryResolve()`로 역참조해 검사. 수동 활성화이거나
  Instigator가 없으면 조건을 건너뛴다.

`Evaluate()` 시그니처에 `GameplayEventData? triggerEvent = null`를 추가한다.

#### 6-B. `AbilityDataValidator` 규칙

| 검사 | 수준 |
| --- | --- |
| `triggerTag`가 비었거나 Registry 미등록 | Error |
| `Immediate` + `concurrency != Background` | Error |
| 트리거 태그가 자신의 `executionGrantedTagIds`에 포함 | Error |
| `OwnedTagPresent` + `concurrency != Background` | Error |
| 모든 `AbilityTagRequirement` 태그의 Registry 등록 여부 | Error (기존 `ValidateTagList` 재사용) |
| `requireAll`과 `blockAny`에 같은 태그 | Warning (영구 차단) |
| 같은 태그·소스 트리거가 한 AbilitySet 안에 3개 이상 | Warning |
| `cancelAbilitiesWithTag`에 자신의 `abilityTagIds`가 포함 | Error (자기 취소) |

#### 6-C. Ability Editor

`GameplayAbilityEditorWindow`의 그룹 정의(`:2553`)에 `"트리거"` 그룹을 추가하고
`triggers`를 배치한다. `"활성화"` 그룹에 3종 `TagRequirement`를 넣는다.
한글 라벨 맵(`:2762`)과 툴팁 맵(`:2808`)에도 항목을 추가한다.

> 프로젝트 규약상 데이터 필드를 추가하면 커스텀 인스펙터도 같이 갱신한다.

#### 6-D. 디버그

`AbilitySystemDebugSnapshot`에 최근 트리거 발화 N건(태그·Ability·결과)을 추가해
`AbilitySandboxWindow`에서 확인한다. `AbilityDebugRecorder`가 이미 태그 추가를
기록하므로 트리거 결과만 얹으면 된다.

**테스트**

- T6-1 타깃이 `blockAny` 태그를 보유하면 `BlockedByTag`.
- T6-2 Instigator가 `requireAll`을 만족하지 않으면 `MissingRequiredTag`.
- T6-3 Validator가 `Immediate` + 비-Background를 Error로 보고한다.

**완료 판정:** T6-1~3 + Ability Editor에서 트리거 저작·검증 가능.

---

### 7단계 — 태그 기반 Ability 취소·차단

**목표:** UE `CancelAbilitiesWithTag` / `BlockAbilitiesWithTag`.
`AbilityConcurrencyPolicy` 3-enum(`RejectNew`/`CancelExisting`/`Background`)은
"무엇을" 취소·차단할지 지정할 수 없어 전부 아니면 전무다. 트리거로 자동
활성화가 늘어나면 이 압력이 커지므로 마지막 단계로 해소한다.

**작업**

1. `Commit()`에서 `cancelAbilitiesWithTag`와 `abilityTagIds`가 매칭되는
   활성 실행을 취소한다. 자기 자신은 제외한다.
2. 차단 카운트를 유지한다.

   ```csharp
   private readonly Dictionary<GameplayTag, int> _blockedAbilityTags = new();
   ```

   `Commit()`에서 `blockAbilitiesWithTag`를 증가시키고, `EndExecution()`에서
   감소시킨다. `CleanupExecution()` 옆에 두어 취소·정상종료 양쪽에서 반드시
   해제되게 한다.
3. `Evaluate()`에 검사를 추가한다. 위치는 **`concurrency` 검사 직전**
   (`:635` 앞)이다.

   ```csharp
   if (IsBlockedByActiveAbility(definition.abilityTagIds))
       return AbilityActivationResult.BlockedByActiveAbility;
   ```

4. 취소 순서 주의: 1번의 취소가 `EndExecution`을 부르고 그 안에서 차단 카운트가
   감소하므로, **취소를 먼저 처리하고 자신의 차단 태그를 나중에 등록한다.**
   순서가 뒤바뀌면 자신이 취소한 Ability의 차단 해제가 자기 등록을 덮어쓴다.

**테스트**

- T7-1 `cancelAbilitiesWithTag`가 매칭되는 활성 Ability를 취소한다.
- T7-2 `blockAbilitiesWithTag` 활성 중에는 매칭 Ability가
  `BlockedByActiveAbility`로 거부된다.
- T7-3 차단 Ability가 **취소로** 종료돼도 차단이 해제된다 (정상 종료뿐 아니라).
- T7-4 `cancelAbilitiesWithTag`와 `blockAbilitiesWithTag`를 동시에 가진
  Ability의 Commit 후 차단 카운트가 정확하다 (4번 순서 함정).

**완료 판정:** T7-1~4 통과.

---

### 8단계 — 몬스터 피격·공격의 태그 트리거 이관 + 기존 데이터 마이그레이션

**목표:** 1~7단계로 만든 기반 위에 실제 콘텐츠를 얹는다. 몬스터 피격 리액션과
공격 개시를 태그 트리거로 전환하고, BT가 Ability 활성화 가능 여부로 분기하게
하며, 기존 에셋을 자동 마이그레이션한다.

#### 8-0. 선결 제약 (설계 전제)

**(1) 데미지 적용은 이미 GAS다.** `MonsterActor.ApplyResolvedHit`(`:211`)의
`AbilitySystem.ApplyResolvedDamage`는 `AbilitySystemComponent.cs:21-29`의
`GE_Damage` / `DamageExecution`으로 흐른다. 8단계는 데미지를 GAS로 "옮기는"
작업이 아니라 **리액션 결정을 Ability로 옮기는** 작업이다.

**(2) 피격 파이프라인의 동기 반환 계약을 깨지 않는다.**
`ReceiveHit`(`MonsterActor.cs:163`)은 `CombatResult`를 동기 반환하고 공격자가
그 값으로 히트스톱·피드백·바이탈오브를 처리한다. 반면 트리거는 재진입 방지를
위해 `_pendingTriggers` 큐로 **의도적으로 지연**된다(3-B).

따라서 다음 경계를 지킨다.

| 계층 | 담당 | 트리거 이관 |
| --- | --- | --- |
| 데미지 산출 (`ResolveHit`) | `CombatResolutionPipeline` | ❌ 유지 |
| 데미지 적용 (`ApplyResolvedDamage`) | GAS `GE_Damage` | ✅ 이미 완료 |
| Poise/Break 적용 (`OnDamaged` 전반부) | `MonsterActor` | ❌ 유지 |
| **리액션 결정·상태 전환** (`ApplyMonsterReactionState` `:516`) | `MonsterActor` switch | ✅ **이관 대상** |
| 리액션 부가 효과 (VFX/SFX/Effect) | 산재 | ✅ 이관 대상 |

`CombatResult`에 실리는 값(`ReactionDecision`, 적용 리소스)은 전부 이관 대상
**바깥**이므로 반환 계약이 유지된다.

#### 8-1. 펄스 태그

피격·공격 개시는 순간 사건이라 지속 태그가 아니다. `ActorAbilitySystem`에
1프레임 펄스 API를 추가한다.

```csharp
public void IssueTriggerPulse(GameplayTag tag, GameplayEventData? payload = null);
```

- 태그를 `Add`하고 트리거를 큐에 넣되, **`LateTick()`에서 제거한다.**
  `LateTick`은 이미 stale Prepared 정리를 하므로(`:1058`) 여기에 붙인다.
- `payload`가 있으면 `AbilityExecution.TriggerEvent`(3-E)로 전달된다.
  피격 리액션 Ability가 `HitContext`(공격자·방향·반응타입)를 읽는 통로다.
- 펄스 태그는 `OwnedTagAdded` 트리거만 받는다. `OwnedTagPresent`와 조합하면
  1프레임 뒤 즉시 취소되므로 Validator에서 **Error**로 막는다.

#### 8-2. 태그 규약

레지스트리(`Assets/Resources/GameplayTagRegistry.asset`)의 기존 규약
(`State.*` / `Motion.*` / `Combo.*`)을 따라 `Trigger.*` 루트를 신설한다.

```
Trigger.Monster.Hit                     ← 공통. 모든 피격에 발급
Trigger.Monster.Hit.Light
Trigger.Monster.Hit.Heavy
Trigger.Monster.Hit.KnockBack
Trigger.Monster.Hit.Airborne
Trigger.Monster.Hit.Knockdown
Trigger.Monster.Hit.Stun
Trigger.Monster.Hit.Grab
Trigger.Monster.Hit.PoiseBreak          ← Poise 파손과 동시 발생 시 추가 발급

Trigger.Monster.Attack                  ← 공통. 모든 공격 개시에 발급
Trigger.Monster.Attack.<AbilityId>      ← 스킬별. abilityId를 그대로 사용
```

하위 태그는 `AttackReactionType` enum과 1:1이다. 공통 태그를 함께 발급하므로
계층 매칭(`Hierarchy`)으로 "모든 피격"을, `Exact`로 "이 반응만"을 각각 저작할 수
있다.

`Trigger.Monster.Attack.<AbilityId>`의 `AbilityId`는 `abilityId`에서 최상위
분류 접두사를 뗀 형태를 쓴다(MotionKey 규약과 동일:
`Actor.Ent.Attack.1.01` → `Ent.Attack.1.01`). 즉
`Trigger.Monster.Attack.Ent.Attack.1.01`.

#### 8-3. 피격 리액션 이관 (3단계로 나눠 진행)

발급 지점은 `MonsterActor.ApplyResolvedHit`에서 `OnDamaged` **직후**다.
Poise/Break 적용이 끝난 뒤여야 리액션 Ability가 파손 여부를 조건으로 읽는다.

**8-3-A. 발급만 (동작 변화 0)**

`ApplyResolvedHit`에 `IssueTriggerPulse` 호출을 추가한다. 리액션 Ability는
아직 만들지 않는다. 기존 `ApplyMonsterReactionState` 경로가 그대로 동작한다.

- 완료 판정: `AbilitySandboxWindow`에서 피격 시 태그 펄스가 관측된다.
  전투 동작·수치 변화 0.

**8-3-B. 그림자 실행 (shadow)**

리액션 Ability(`GA_Monster_Hit_*`)를 만들되 **상태 전환은 하지 않고**
부가 효과(VFX/SFX/Effect)만 담당하게 한다. 기존 switch가 여전히 상태를
전환한다. 두 경로를 동시에 돌려 결과를 비교한다.

- 완료 판정: 기존 리액션 대비 상태 전환 결과가 100% 일치.
  중복 VFX/SFX가 없도록 기존 호출부를 Ability로 옮긴 만큼만 제거.

**8-3-C. 상태 전환 이관**

`ApplyMonsterReactionState`(`:516`)의 switch를 리액션 Ability의 `Request`
경로로 대체한다. 구독자는 `MonsterActor`다(`EnemyCombat`이 아니다 — 피격은
전투 컴포넌트의 책임이 아니다).

**전역 킬 스위치를 반드시 둔다.**

```csharp
// EnemyBehaviorSO 또는 프로젝트 설정
public bool useTagTriggeredHitReaction;   // 기본 false
```

`false`면 8-3-A의 태그 발급만 하고 기존 switch를 쓴다. 몬스터 단위로 켤 수
있어야 하며, 전 몬스터 검증이 끝나기 전에는 기본값을 바꾸지 않는다.

- 완료 판정: 몬스터 3종(일반/엘리트/보스)에서 8종 `AttackReactionType`
  전부를 구/신 경로로 비교 검증.

> **함정:** `CanPlayHitReaction`(`:501`)은 현재 상태가 Hit/Stun/Knockdown 등이면
> 리액션을 막는다. 이 게이트를 Ability의 `blockAny` 태그로 옮길 때 `State.Hit`
> 등 **기존 상태 태그를 재사용**해야 한다. 새 태그를 만들면 상태 머신이
> 부여하는 태그와 어긋난다.

#### 8-4. 공격 개시 이관

현재 `ExecuteEnemyAttackNode`(`:102`)가 직접
`TransitionToState(new EnemyAttackState(...))`를 호출한다. 이를 태그 발급으로
바꾸고, 트리거 → `Request` → `EnemyCombat` 구독(4단계) → 상태 전환으로 흐르게
한다.

```
BT: CanActivateAbilityNode 통과
 → 슬롯 예약 (TryRequestAttackSlot)
 → IssueTriggerPulse(Trigger.Monster.Attack.<AbilityId>)
 → [큐 드레인] → Evaluate() → Request 발행
 → EnemyCombat.SetCurrentAbility → EnemyAttackState
```

**두 가지 함정을 반드시 처리한다.**

1. **슬롯 누수.** 현재 노드는 `TryRequestAttackSlot()` → `ReserveAttackCategory`
   → `NotifyBTAttackStarted` → 상태 전환을 연속 실행한다(`:93-102`).
   트리거 경로에서는 예약 후 `Evaluate()`가 실패할 수 있고, 그러면 슬롯이
   점유된 채 남는다. **트리거 거부 시 슬롯을 반납**해야 한다.
   `AbilityTriggerRequested`가 소비되지 않았을 때의 콜백이 필요하다.
2. **1프레임 지연.** 큐 드레인은 같은 프레임이지만 발급 지점 이후다.
   노드의 `_attackStarted` 판정(`:33`, `:42`)이 즉시 전환을 전제하므로,
   태그 발급 후 상태 진입까지 **최대 2프레임 대기**를 허용하도록 바꾼다.
   그 안에 진입하지 못하면 슬롯을 반납하고 `Failure`.

#### 8-5. BT 노드 추가

**`CanActivateAbilityNode` (`BTConditionNode`)** — `EnemyCombat.CanActivateAbility`
(`:668`)를 그대로 감싼다. 이 메서드는 `TryEvaluateAbility` → `Evaluate()`를
거치므로 **1·6단계에서 추가한 태그 조건을 자동으로 반영한다.** 새 평가 로직이
필요 없다.

```csharp
[SerializeField] private GameplayAbilitySO _ability;
[SerializeField] private AbilityAttackCategory _category = AbilityAttackCategory.None;
// _ability 지정 시 단일 Ability 판정,
// 미지정 시 _category의 후보가 하나라도 활성화 가능한지 판정
```

`BTConditionNode`이므로 기존 conditional abort(`TryEvaluateSelfAbort`)와
자연스럽게 결합한다 — 공격 중 조건이 깨지면 상위 Selector가 회수한다.

**`IssueAbilityTriggerNode` (`BTActionNode`)** — 지정 태그를 펄스 발급한다.
`ExecuteEnemyAttackNode`를 대체하는 게 아니라, 접근·슬롯 로직은 남기고
상태 전환 부분만 이 노드로 교체한다.

> BT 저작 포맷이 둘(Rules JSON / raw BT-node JSON)이므로 두 경로 모두에
> 노드를 등록해야 한다. `generate-bt-json` 스킬의 노드 목록도 갱신한다.

#### 8-6. 마이그레이션 도구

`MonsterTagTriggerMigrationWindow` (Editor 전용).

| 작업 | 내용 |
| --- | --- |
| M1 | `Trigger.*` 태그를 레지스트리에 일괄 등록 |
| M2 | 몬스터 AbilitySet의 `aiSelectable` Ability마다 `Trigger.Monster.Attack.<AbilityId>` 트리거 자동 부여 (`Request` 모드) |
| M3 | `AttackReactionType` 8종에 대응하는 `GA_Monster_Hit_*` 리액션 Ability 생성 (`AbilityAssetFactory` 재사용) |
| M4 | BT JSON의 `ExecuteEnemyAttackNode`를 `CanActivateAbilityNode` + `IssueAbilityTriggerNode` 조합으로 변환 |
| M5 | 드라이런 리포트 출력 (변경 예정 에셋·BT 목록) |

**프로젝트 안전 규칙을 그대로 따른다** (CLAUDE.md "Editor 데이터 도구 안전 규칙").

- 기존 에셋 식별은 **GUID 정확 일치 → path 정확 일치** 순. 둘 다 유효하지
  않으면 **이름 폴백 없이 실패**시킨다.
- 예외 발생 시 해당 Undo group 전체를 `Undo.RevertAllDownToGroup`으로 롤백한다.
  일부 적용 상태를 성공처럼 collapse하지 않는다.
- **P09 빌더식 삭제·교체 경로를 쓰지 않는다.** M2~M4는 전부 기존 에셋에
  필드를 **추가**하는 in-place 연산이어야 한다.
- 드라이런(M5)을 통과하지 않으면 실행 버튼을 비활성화한다.

**롤백:** 마이그레이션 전 대상 에셋을 `Assets/10.Datas/_MigrationBackup/`에
복사하고, `AbilitySetSO`에 `tagTriggerMigrationVersion` int 필드를 두어
재실행 시 멱등성을 보장한다.

#### 8-7. 테스트

| ID | 내용 |
| --- | --- |
| T8-1 | 피격 시 `Trigger.Monster.Hit` + 반응타입 하위 태그가 발급된다 |
| T8-2 | 펄스 태그가 `LateTick` 이후 제거된다 |
| T8-3 | 펄스 태그 발급 전후로 `CombatResult` 반환값이 변하지 않는다 (8-0-(2) 계약) |
| T8-4 | `useTagTriggeredHitReaction = false`면 기존 switch 경로가 동작한다 |
| T8-5 | 리액션 Ability가 `TriggerEvent`에서 공격자를 읽는다 |
| T8-6 | `State.Hit` 보유 중에는 리액션 Ability가 재발화하지 않는다 (`CanPlayHitReaction` 등가) |
| T8-7 | `CanActivateAbilityNode`가 쿨다운 중 Ability에 Failure를 반환한다 |
| T8-8 | `CanActivateAbilityNode`가 `blockAny` 태그 보유 시 Failure를 반환한다 |
| T8-9 | 트리거 거부 시 공격 슬롯이 반납된다 (8-4 함정 1) |
| T8-10 | 태그 발급 후 2프레임 내 미진입이면 Failure + 슬롯 반납 (8-4 함정 2) |
| T8-11 | 펄스 태그 + `OwnedTagPresent` 조합을 Validator가 Error 보고 |
| T8-12 | 마이그레이션 재실행이 멱등이다 (`tagTriggerMigrationVersion`) |
| T8-13 | 마이그레이션 중 예외 시 전체 롤백된다 |

**완료 판정:** T8-1~13 통과 + 몬스터 3종 Play Mode 비교 검증 + 드라이런
리포트 검토.

#### 8-8. 진행 순서

```
8-1 (펄스 API) → 8-2 (태그 등록/M1)
  → 8-3-A (발급만)  → 8-3-B (shadow) → 8-3-C (상태 전환 이관)
  → 8-5 (BT 노드)   → 8-4 (공격 이관)
  → 8-6 (M2~M5 도구) → 전 몬스터 적용
```

8-3과 8-4는 독립이므로 병렬 가능하지만, **8-3-A/B/C 순서는 건너뛰지 않는다.**
shadow 없이 상태 전환을 이관하면 회귀를 발견할 기준선이 없다.

---

### 9단계 — 플레이어 전투의 태그 트리거 전환

**목표:** 8단계에서 몬스터로 검증한 패턴을 플레이어에 적용한다. 순서는
의도적이다 — 몬스터는 회귀해도 밸런스 문제지만, 플레이어는 조작감 문제라
되돌리기 어렵다.

#### 9-0. 몬스터와 다른 점

플레이어는 몬스터의 단순 복제가 아니다. 네 가지가 구조적으로 다르다.

| 항목 | 몬스터 (8단계) | 플레이어 (9단계) |
| --- | --- | --- |
| 활성화 구동 | BT 노드 | 입력 + `InputBuffer` 선입력 |
| 상태 태그 | 거의 없음 (신규 부여) | **이미 부여 중** |
| 콤보 분기 | `EnemyAbilitySelectionPolicy` 가중치 | `ComboRouteData` (자체 태그 평가 보유) |
| 캐릭터 교체 | 없음 | `PlayerSwapBehaviour` → AbilitySet 교체 |
| 상태 수 | 지상 21 + 비행 9 | 23 |

**(1) 상태 태그가 이미 있다 — 이게 가장 중요하다.**
`PlayerAttackState.cs:346`이 `State.Combat.Attack`을,
`PlayerDashState.cs:55`가 `State.Dash`를,
`PlayerGuardState.cs:110`이 `State.Combat.Counter`를 이미 부여·해제한다.

따라서 9단계는 **태그 어휘를 새로 만드는 작업이 아니라, 이미 있는 태그를
Ability 조건으로 소비하는 작업이다.** 새 `State.*` 태그를 만들지 말고
기존 것을 쓴다. 8단계의 `Trigger.*` 신설과는 성격이 다르다.

**(2) `ComboRouteData`에 중복된 태그 평가가 있다.**
`ComboRouteData.cs:70-73`이 `requiredTagIds`/`blockedTagIds`를 갖고
`:129-145`에서 자체 평가한다. 1단계의 `AbilityTagRequirement`와 **의미는
같은데 구현이 별개다.** 9단계에서 통합한다(9-3).

**(3) `InputBuffer` 선입력과 트리거가 이중 발화할 수 있다.**
선입력이 살아 있는 상태에서 트리거가 같은 Ability를 활성화하면 입력 1회에
실행 2회가 된다. 9-2의 핵심 위험이다.

#### 9-1. 피격 리액션 (8-3 재적용)

`PlayerActor.Combat.cs`의 `OnDamaged`(`:306`) → `ApplyPlayerReactionState`(`:362`)는
몬스터의 `OnDamaged`(`:393`) → `ApplyMonsterReactionState`(`:516`)와 **구조가
동일하다.** 8-3-A/B/C를 그대로 반복한다.

태그는 `Trigger.Player.Hit.*`로 8-2와 같은 계층을 쓴다.

플레이어 고유 게이트 두 가지를 태그로 옮긴다.

| 기존 코드 | 이관 방식 |
| --- | --- |
| `hasSuperArmor` (`:309` — 차지 1단계 이상) | `PlayerChargeState`가 `State.SuperArmor` 태그 부여 → 리액션 Ability의 `blockAny` |
| `IsStaggerImmune` (`:320`) | 동일하게 태그화 |

`State.SuperArmor`는 신규 태그다. 레지스트리 등록이 필요하다.

> **함정:** `reactionDecision.ShouldEnterState`가 참일 때
> `monsterAttacker.AIController?.Group?.NotifyPlayerEnteredHitReaction()`
> (`:369`)가 호출된다. 이건 **공격자 쪽 그룹 AI에 대한 통지**라 리액션
> Ability로 옮기면 안 된다. `ApplyResolvedHit` 계층에 남긴다.

킬 스위치는 `useTagTriggeredHitReaction`과 **별도**로 둔다
(`useTagTriggeredPlayerHitReaction`). 몬스터를 켠 채 플레이어만 끌 수 있어야
한다.

#### 9-2. 입력 기반 공격 활성화

5단계에서 `PlayerCombat`이 `AbilityTriggerRequested`를 구독한다. 9단계는
그 위에 실제 콘텐츠를 얹는다.

**이중 발화 방지가 최우선이다.** 다음 규칙을 고정한다.

1. 트리거는 **입력이 만들지 않는 활성화**만 담당한다 — 피격 반격, 저스트가드
   성공, 스왑 진입 변형 등. 일반 공격 입력 경로는 기존 그대로 둔다.
2. 트리거가 활성화한 실행은 `InputBuffer`를 **소비하지 않는다.**
   버퍼는 입력 경로만 소비한다.
3. 트리거 Ability와 입력 슬롯 Ability가 같은 에셋이면 Validator **Error**.
   같은 Ability를 두 경로로 활성화하지 않는다.

`PlayerCombat`의 구독 핸들러는 현재 상태의 `CanTransitionState` 결과로
수락/거부한다(5단계). 거부는 정상이므로 로그를 남기지 않는다.

#### 9-3. `ComboRouteData` 태그 평가 통합

`ComboRouteData.requiredTagIds`/`blockedTagIds`(`:70-73`)를
`AbilityTagRequirement`로 교체하고, 자체 평가(`:129-145`)를 삭제한 뒤
1단계의 `EvaluateTagRequirement`를 호출한다.

- 기존 두 리스트는 **남긴다.** 1단계와 같은 하위호환 방식이다.
- 통합 후 콤보 라우트에서도 `requireAny`와 `Exact` 매칭을 쓸 수 있다.
- `AbilitySetSO.comboRoutes`와 `overrideComboRoutes` 상속 경로는 건드리지 않는다.

> 이 항목은 트리거와 무관하게 **단독으로 가치가 있다.** 태그 평가 구현이
> 프로젝트에 둘 있는 상태를 없앤다. 9단계 중 위험도가 가장 낮으므로
> 먼저 진행해도 된다.

#### 9-4. 캐릭터 교체와 트리거 인덱스

`PlayerSwapBehaviour`가 캐릭터를 바꾸면 `SetAbilitySet`이 호출되고
3-A의 트리거 인덱스가 재구축된다. 여기서 두 가지를 확인한다.

1. **교체 시점에 활성 트리거 실행이 있으면 취소된다.** `SetAbilitySet`은
   이미 `CancelAllAbilities()`를 호출한다(`:99`). `OwnedTagPresent`로 켜둔
   오라가 교체와 함께 꺼지는 것이 의도인지 확인이 필요하다.
2. **벤치 캐릭터의 트리거는 발화하지 않아야 한다.** 인덱스가 활성 캐릭터의
   AbilitySet만 담으므로 자연히 성립하지만, 파티 전체에 태그를 부여하는
   경로(파티 버프 등)가 생기면 깨진다. 테스트로 고정한다.

#### 9-5. 테스트

| ID | 내용 |
| --- | --- |
| T9-1 | 플레이어 피격 시 `Trigger.Player.Hit.*` 발급, `CombatResult` 불변 |
| T9-2 | `State.SuperArmor` 보유 중 리액션 Ability가 발화하지 않는다 |
| T9-3 | `useTagTriggeredPlayerHitReaction = false`면 기존 경로 동작 |
| T9-4 | 입력 1회에 실행이 1회만 발생한다 (`InputBuffer` 이중 발화 방지) |
| T9-5 | 트리거 활성화가 `InputBuffer`를 소비하지 않는다 |
| T9-6 | 입력 슬롯과 트리거에 같은 Ability를 지정하면 Validator Error |
| T9-7 | `ComboRouteData` 통합 후 기존 라우트 판정 결과가 불변 |
| T9-8 | 콤보 라우트에서 `requireAny`가 동작한다 |
| T9-9 | 캐릭터 교체 시 이전 캐릭터의 트리거가 발화하지 않는다 |
| T9-10 | 벤치 캐릭터의 트리거가 발화하지 않는다 |

**완료 판정:** T9-1~10 + 캐릭터 3종 Play Mode 조작감 검증.
조작감은 자동 테스트로 대체할 수 없으므로 **수동 검증을 생략하지 않는다.**

#### 9-6. 진행 순서

```
9-3 (ComboRoute 통합, 위험도 최저·트리거 무관)
  → 9-1 (피격 리액션: A→B→C)
  → 9-2 (입력 기반 활성화)
  → 9-4 (교체 검증)
```

---

## 5. 단계 의존 관계

```
1 (태그 조건)  ─┐
2 (트리거 타입) ─┴→ 3 (트리거 런타임) ─┬→ 4 (Request/Enemy) → 5 (Player)
                                      └→ 6 (Source·Target/검증/에디터)
                                              └→ 7 (취소·차단)
                                                      └→ 8 (몬스터 이관·마이그레이션)
                                                              └→ 9 (플레이어 전환)
```

- **1·2는 순서 무관**하고 서로 독립이다.
- **3까지가 최소 동작 단위다.** 여기서 멈춰도 버프·오라 계열이 완결된다
  (`OwnedTagPresent`가 3단계에 있으므로 켜고 끄는 것이 모두 된다).
- **4·6은 병렬 가능**하다. 6은 3에만 의존한다.
- **5는 4 이후**다. Request 이벤트가 있어야 구독할 수 있다.
- **7은 6 이후**를 권장한다. Validator 규칙이 있어야 자기 취소를 막는다.
- **8은 4·6·7 이후**다. 공격 이관에 `Request`(4)가, 마이그레이션 안전성에
  Validator(6)가, 피격 리액션과 공격 Ability의 상호 취소에 7이 필요하다.
  5(Player)와는 무관하므로 5를 건너뛰고 8로 갈 수 있다.
- **9는 5·8 이후**다. `PlayerCombat` 구독(5)이 있어야 입력 경로를 얹을 수 있고,
  8에서 몬스터로 패턴을 검증한 뒤에 플레이어를 건드린다. 몬스터 회귀는 밸런스
  문제지만 플레이어 회귀는 조작감 문제라 되돌리기 어렵다.
  단, **9-3(ComboRoute 통합)은 1단계에만 의존**하므로 먼저 진행해도 된다.

---

## 6. 전체 테스트 목록

`Assets/Tests/EditMode/Ability/GameplayAbilitySystemTests.cs`에 추가한다.

| ID | 내용 | 단계 |
| --- | --- | --- |
| T1-1 | `requireAny` 부분 충족 통과 / 미충족 `MissingRequiredTag` | 1 |
| T1-2 | `Exact` 모드에서 하위 계층 태그 불충족 | 1 |
| T1-3 | 레거시 필드만 쓰는 에셋의 판정 결과 불변 | 1 |
| T3-1 | `OwnedTagAdded` + Immediate → Active | 3 |
| T3-2 | 트리거 발화해도 `blockAny` 보유 시 미활성화 | 3 |
| T3-3 | `Exact` 트리거가 하위 계층 태그로 발화하지 않음 | 3 |
| T3-4 | 자기 부여 태그 트리거의 무한 루프 방지 | 3 |
| T3-5 | `retriggerIntervalSeconds` 이내 재부여 무시 | 3 |
| T3-6 | `OwnedTagPresent` 부여→활성, 제거→Cancelled | 3 |
| T3-7 | GE 부여 태그로 트리거 발화 | 3 |
| T3-8 | `GameplayEvent` 트리거의 Instigator 수신 | 3 |
| T4-1 | 구독자 없는 Request → `StateTransitionRejected` | 4 |
| T4-2 | `EnemyCombat` 구독 시 실행 성립 | 4 |
| T6-1 | 타깃 `blockAny` → `BlockedByTag` | 6 |
| T6-2 | Instigator `requireAll` 미충족 → `MissingRequiredTag` | 6 |
| T6-3 | Validator가 `Immediate`+비-Background를 Error 보고 | 6 |
| T7-1 | `cancelAbilitiesWithTag` 취소 동작 | 7 |
| T7-2 | `blockAbilitiesWithTag` 차단 동작 | 7 |
| T7-3 | 취소 종료 시에도 차단 해제 | 7 |
| T7-4 | 취소·차단 동시 보유 시 카운트 정확성 | 7 |
| T8-1~13 | 몬스터 이관·마이그레이션 (§4 8-7 참조) | 8 |
| T9-1~10 | 플레이어 전환 (§4 9-5 참조) | 9 |

---

## 7. 재진입 위험 요약

트리거 도입으로 새로 생기는 순환 경로를 한곳에 모은다.

| 순환 | 방어 |
| --- | --- |
| Commit → `executionGrantedTagIds` → TagAdded → 자기 트리거 | 게이트 1 + Validator Error |
| Commit → 태그 → 다른 Ability 트리거 → Commit → … | 게이트 4 (깊이 4) |
| TagRemoved → 취소 → `CleanupExecution` → 태그 제거 → TagRemoved | 큐 직렬화 (3-D) |
| 트리거 처리 중 `_executions` 변경 | `_draining` 플래그 (3-B) |
| 7단계 취소 → `EndExecution` → 차단 카운트 감소 → 자기 등록 덮어씀 | 처리 순서 고정 (7단계 4번) |
| 8단계 리액션 Ability가 데미지를 주고 그 피격이 다시 리액션 트리거 | 게이트 4 + `State.Hit` `blockAny` (8-3-C) |
| 8단계 펄스 태그 + `OwnedTagPresent` → 1프레임 뒤 자기 취소 | Validator Error (8-1) |

---

## 8. UE 대비 의도적 차이

| 항목 | UE | 본 설계 | 이유 |
| --- | --- | --- | --- |
| 활성화 | `CommitAbility` 단일 | Prepare → 검증 → Commit | 모션 해석 실패가 진행 중 Ability를 끊지 않게 하는 원자성. 프로젝트 고유 강점이므로 유지 |
| 트리거 실행 주체 | ASC가 직접 | Immediate / Request 분리 | ASC가 상태 머신을 소유하지 않음. 위 계약에서 파생 |
| `OwnedTagRemoved` | 없음 | 없음 | 초안에 있었으나 `OwnedTagPresent`로 교체. 취소 짝이 없는 트리거는 오라를 끌 수 없다 |

---

## 9. 범위 밖

- **중첩 태그 쿼리.** UE `FGameplayTagQuery`는 표현식 트리(`AnyExprMatch` 등)지만
  실사용은 flat 컨테이너 위주이고 Core의 `GameplayTagQuery`도 flat이다.
  여기서 UE를 따라갈 실익이 없다.
- **`InstancingPolicy`.** 프로젝트의 실행은 데이터 + 핸들이라 사실상
  non-instanced이며, 이를 바꿀 동기가 없다.
- **원격 트리거.** 다른 액터의 태그를 감시하지 않는다. 액터 간 연동은
  `GameplayEventRouter`를 쓴다.
- **트리거의 비용·쿨다운 면제 옵션.** 면제가 필요하면 해당 Ability의
  `cost`/`cooldown`을 비운다. 원칙 3을 흐리는 옵션을 두지 않는다.
- **지속 효과의 트리거 대체.** 지속 상태는 기존대로 `GameplayEffectSO`가
  담당한다. 트리거는 활성화 계기일 뿐이다.
