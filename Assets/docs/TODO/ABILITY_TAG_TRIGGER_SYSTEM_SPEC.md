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
| 태그 쿼리 타입 | `GameplayTagQuery` / `Matches()` `:41-155` | All/Any/None + 계층. Effect가 사용 중 (`GameplayEffectSpec.cs:344-345`) |
| 이벤트 페이로드 | `GameplayEventData` (`GameplayEventRuntime.cs:26-53`) | UE `FGameplayEventData` 대응 |
| GE → 태그 부여 | `ActiveGameplayEffectContainer.cs:331-336` | `_owner.Tags.Add(...)` |
| ASC 핸들 역참조 | `AbilitySystemComponent.TryResolve()` `:118` | Instigator 핸들 → 컴포넌트 |
| 타 액터 ASC 접근 | `GameActor.AbilitySystem` (`GameActor.cs:70`) | public |

**중요:** `GameplayTagQuery`는 미사용이 아니다. Effect가 이미
`ApplicationRequirement` / `ImmunityQuery`로 쓰고 있다
(`GameplayEffectSpec.cs:344-345`, 평가는 `ActiveGameplayEffectContainer.cs:100-103`).
Ability만 쓰지 않는다. 새 쿼리 타입을
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
4. `GameplayAbilitySO.cancelAbilitiesWithTag` / `blockAbilitiesWithTag` 필드 추가.
   런타임은 7단계지만 **필드는 여기서 만든다** — 6단계 Validator가 이 필드를
   검증하므로(§6-B) 2단계에 없으면 6→7 순환 의존이 생긴다.

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
private readonly Dictionary<(GameplayAbilitySO, int), float> _lastTriggerTime = new();
private readonly Queue<PendingTrigger> _pendingTriggers = new();
private int _listLockDepth;   // FScopedAbilityListLock 대응 (3-B)
```

> `_lastTriggerTime`의 키가 `(Ability, triggerIndex)`인 이유는 게이트 3(3-C)을,
> `_listLockDepth`가 `_draining`을 대체한 이유는 3-B를 참조한다.

인덱스는 `SetAbilitySet()`과 임시 Ability 부여·회수(`_temporaryAbilities` 변경)
시점에 재구축한다. 매 프레임 순회하지 않는다.

**단, 부여 판정에는 세 번째 경로가 있다.** `IsGrantedAbility()` `:786-796`은
`_temporaryAbilities` / `_abilitySet` 외에
`ResolvePlayerAbility(PlayerSkillSlot.ElementalImbue)`(`:795`)를 본다. 이 값은
`Svc.Party?.GetElementalImbueAbility(type)`(`:780`)를 **매 호출 동적 조회**하므로
`SetAbilitySet`이나 `_temporaryAbilities`와 무관하게 파티·속성 변경만으로 바뀐다.
Imbue Ability(프로젝트 기준 5종)에 트리거를 붙이면 인덱스가 낡거나 아예 등록되지
않는다.

→ **Imbue 슬롯 Ability에는 트리거를 금지하고 6단계 Validator에서 Error로 막는다.**
인덱스 재구축 계기를 파티 상태 변경까지 넓히는 것보다 제약이 단순하고, Imbue는
입력 슬롯 기반이라 트리거가 필요한 대상이 아니다.

`Initialize()`에서 구독하고, **`Dispose()`의 최상단에서 해제한다.**

```csharp
_abilitySystem.Runtime.Tags.TagAdded    += OnTagAddedForTrigger;
_abilitySystem.Runtime.Tags.TagRemoved  += OnTagRemovedForTrigger;
_abilitySystem.Runtime.Events.EventSent += OnEventForTrigger;
```

해제 위치가 중요하다. `Dispose()`(`:1069-1074`)의 첫 줄이 `CancelAllAbilities()`라
해제를 뒤에 두면 **파괴 중에 트리거가 발화한다.** 이어지는 `_executions.Clear()`가
`CleanupExecution` 없이 실행을 날리므로 애그리게이터에 태그가 잔존한다.

#### 3-B. 발화 흐름

```
TagAdded(tag)
  └ 인덱스 조회 → 후보 (ability, trigger) 목록
     └ _pendingTriggers 에 enqueue          ← 콜백 안에서 즉시 실행하지 않음
        └ _listLockDepth > 0 이면 여기서 종료 (스코프 해제 시 드레인)
           └ Drain()
              └ priority 내림차순 처리 (1회 상한 MaxDrainBudget)
                 ├ 게이트 검사 (3-C)
                 ├ Immediate → TryPrepareAbility + Commit
                 └ Request   → AbilityTriggerRequested 발행
```

**잠가야 하는 것은 트리거 큐가 아니라 "Ability 리스트 조작 구간"이다.**
초안은 `_draining` 플래그를 드레인 진입부에만 걸었는데, 이 모델로는 다음
두 결함을 막지 못한다. 둘 다 **수동 활성화 → 트리거** 방향이라 드레인 플래그가
아직 false인 상태에서 발생한다.

**(결함 1) `Commit()` 도중 낡은 상태로 재진입한다.** `Commit()`의 실행 순서는
`AddExecutionTags()`(`:305`) → `execution.State = Active`(`:314`) →
`_primaryExecution` / `_backgroundExecutions` 갱신(`:320`·`:324`)이다.
즉 태그 부여 시점에는 **실행 상태가 아직 `Prepared`이고 `_primaryExecution`도
갱신 전**이다. `PlayerCombat`/`EnemyCombat`이 부른 Commit이면 `_draining`이
false이므로 `:305`에서 드레인이 전부 동기 실행되고, 그 드레인은:

- 게이트 2(중복 실행 차단)에서 이 실행을 **보지 못한다** → 같은 Ability 중복 활성화
- `Evaluate()`(`:635`)의 `_primaryExecution != 0 && RejectNew` 판정이 **낡은 값**으로 내려간다

`EndExecution()`도 같은 결함이 있다. `_executions.Remove`(`:1082`) →
`CleanupExecution`(`:1090`, 여기서 TagRemoved 발화) →
`_backgroundExecutions.Remove` / `_primaryExecution = 0`(`:1098-1100`) 순서라,
드레인이 "딕셔너리에는 없는데 `_primaryExecution`은 살아있는" 상태를 관측한다.

**(결함 2) `CancelAllAbilities()`가 드레인 중 생긴 실행을 고아로 만든다.**

```csharp
// ActorAbilitySystem.cs:385-393
for (int i = 0; i < handles.Count; i++)
    EndExecution(handles[i], false, "AbilityCancelled");   // → TagRemoved → 드레인
_primaryExecution = 0;
_latestPreparedExecution = 0;
_backgroundExecutions.Clear();      // ← 드레인이 추가한 새 실행까지 전부 삭제
```

드레인이 `Immediate` Ability를 Commit하면 `_backgroundExecutions`에 등록되는데,
루프가 끝난 뒤 `:392`가 그 세트를 통째로 비운다. 새 실행은 `_executions`에
`Active`로 남지만 `Tick()`(`:1010`)은 `_backgroundExecutions`만 순회하므로
**영원히 종료 판정을 받지 못한다** — 타임아웃도 Task 완료 수거도 동작하지 않고
`executionGrantedTagIds`가 영구 잔존한다. 그 태그가 `blockAny`에 걸리는 Ability는
영구 활성화 불가가 된다. `SetAbilitySet()`(`:99`)이 `CancelAllAbilities()`를
첫 줄에서 부르므로 **캐릭터 교체·몬스터 정의 적용만으로 재현된다.**

**따라서 락 범위를 리스트 조작 구간 전체로 넓힌다.**

```csharp
private int _listLockDepth;

private readonly struct AbilityListLock : IDisposable   // FScopedAbilityListLock 대응
{
    // 생성 시 _listLockDepth++, Dispose 시 --. 0으로 떨어질 때 Drain() 호출.
}
```

`Commit()`, `EndExecution()`, `Abort()`, `CancelAllAbilities()`, `Dispose()`
**본문 전체**를 이 스코프로 감싼다. 트리거 콜백은 `_listLockDepth > 0`이면
큐에만 넣고 즉시 반환하며, 스코프가 0으로 풀릴 때 한 번 드레인한다.

추가로 `Commit()`에서 **`AddExecutionTags()` 호출을 `:324` 이후로 옮긴다.**
상태와 `_primaryExecution`이 확정된 뒤 태그를 부여하면 락과 무관하게도
관측 순서가 정상화된다. 락과 병행한다(둘 중 하나만 적용하지 않는다).

**예외 안전성이 필수다.** `Evaluate()` 경로의 `HasAllTags`/`HasAnyTag`
(`:926`·`:939`)와 `AddExecutionTags`(`:863`)는 미등록 태그에 대해
`EnsureRegisteredOrEmpty`(`:952-961`)로 **예외를 던진다.** 트리거 도입 후에는
이 예외가 `GameplayTagAggregator.Add`(`GameplayTagRuntime.cs:90`)의 이벤트
호출을 관통해 **태그를 부여한 쪽**(GE 적용 `ActiveGameplayEffectContainer.cs:333`,
상태 머신 `OnEnter`)까지 전파된다. GE가 부분 적용된 채 실패하거나 상태 진입이
중단된다.

- `Drain()` 본문과 락 스코프 해제를 `try/finally`로 감싸 `_listLockDepth`를
  반드시 복구한다. 누락하면 트리거 시스템이 **조용히 영구 정지**한다.
- 개별 트리거 항목 처리를 `try/catch`로 격리해, 한 Ability의 데이터 오류가
  나머지 트리거와 태그 부여자를 죽이지 않게 한다.

#### 3-C. 게이트 (순서대로)

1. **자기 발화 차단** — 트리거 태그가 그 Ability 자신의
   `executionGrantedTagIds`에 있으면 스킵. (에디터에서도 Error)
2. **중복 실행 차단** — 해당 Ability의 활성 실행이 이미 있으면 스킵.
3. **재트리거 간격** — `Time.time < _lastTriggerTime + retriggerIntervalSeconds`면
   스킵. `OwnedTagPresent`는 이 게이트를 건너뛴다(게이트 2가 이미 막는다).
   **키는 Ability가 아니라 트리거 단위다** — `retriggerIntervalSeconds`가
   `AbilityTriggerDefinition`의 필드이므로 `_lastTriggerTime`을
   `Dictionary<GameplayAbilitySO, float>`로 잡으면 트리거가 2개 이상인 Ability에서
   한 트리거의 간격이 다른 트리거를 억제한다. `(ability, triggerIndex)`로 키를 잡는다.
4. **드레인 예산** — 1회 `Drain()`에서 처리한 건수가 `MaxDrainBudget`(64)를
   넘으면 남은 큐를 버리고 `Debug.LogWarning` 1회. 무한 루프의 최종 방어선.

   > 초안은 여기에 "깊이 제한(`_triggerDepth > 4`)"을 두었으나 **큐 설계에서는
   > 발화하지 않는 죽은 방어선이다.** 콜백이 즉시 실행하지 않고 enqueue만 하므로
   > 중첩 트리거는 깊이가 아니라 **큐 길이로 평탄화된다.** 실제 폭주 형태는
   > A가 태그 X 부여 → X가 B를 트리거 → B가 태그 Y 부여 → Y가 A를 트리거인
   > 사이클이고, 이때 깊이는 0~1에 머문 채 `_pendingTriggers`가 단일 `Drain()`
   > 안에서 무한 증식해 프레임이 반환되지 않는다. 그래서 깊이가 아니라
   > **건수 예산**으로 막는다.

5. **일반 활성화 검사** — **`TryPrepareAbility()` 호출 자체**로 대신한다.
   `Evaluate()`(`:597`)는 private이고 `IsGrantedAbility` 검사를 포함하지 않는다
   (그 검사는 `TryPrepareAbility` `:245`에 있다). 게이트 5를 `Evaluate()` 선호출로
   구현하면 부여 여부를 놓치고 Variant 선택·거리 계산이 2회 실행된다.
   태그 조건·쿨다운·자원·ground/target은 이 안에서 전부 걸린다.

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

**Background 강제 타임아웃과의 충돌을 반드시 해소한다.**
`Evaluate()` `:606-608`은 Background Ability에 **양수
`backgroundMaxDurationSeconds`를 필수로 요구**하고, `Tick()` `:1039-1043`은 그
시간이 지나면 `"BackgroundTimeout"`으로 강제 종료한다. §3-F가 `Immediate`를
Background 전용으로 못박았으므로, 아무 처리도 하지 않으면 **`OwnedTagPresent`
오라는 태그가 그대로 붙어 있어도 그 시간 뒤 반드시 꺼진다.**

그리고 트리거 발화 계기는 `TagAdded`/`TagRemoved` **엣지뿐**이라
"이미 붙어 있는 태그"에 대한 재평가 계기가 없다. 즉 한 번 타임아웃되면 태그를
뗐다 다시 붙이기 전에는 복구되지 않는다.

해소: **`Tick()`이 `BackgroundTimeout`으로 종료하려는 실행이
`TriggerSource == OwnedTagPresent`이고 트리거 태그가 여전히 present면
종료하지 않고 `StartTime`을 갱신한다.** (타임아웃 면제보다 갱신이 낫다 —
태그가 사라진 프레임을 놓쳐도 다음 주기에 회수된다.)
T3-6은 `backgroundMaxDurationSeconds`를 넘겨서도 유지되는지 검증해야 한다.

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

**게이트 6 — 선점 금지 (신규).** 초안은 "BT가 선택한 Ability와 경합하면 BT가
우선한다(게이트 2에서 걸린다)"고 했으나 **거짓이다.** 게이트 2는 *같은* Ability의
중복만 막는다. BT의 Ability X와 트리거의 Ability Y는 다른 정의이므로 게이트 2를
그대로 통과하고, `Evaluate()` `:635`는 `RejectNew`만 거른다. Y가
`CancelExisting`이면 `TryPrepareAbility` `:269-270`이 **X를 취소해버린다.**

→ Request 발행 직전에 `_primaryExecution != 0`이면 요청을 폐기한다.
선점을 허용해야 하는 콘텐츠가 생기면 `AbilityTriggerDefinition`에 명시적
필드로 노출하고 기본값은 금지로 둔다.

**구독자 구현은 `SetCurrentAbility`(`:1028`)를 쓴다.** `TryActivateAbility`(`:671`)는
**private**이고, 그것만 불러서는 `_currentAbility` / `_currentSkill` /
`_currentHitPhaseIndex` / 텔레그래프 상태가 갱신되지 않는다. 이 필드들은 히트 판정과
`BuildProjectileAttackData`가 소비하므로, 갱신을 빠뜨리면 **직전 공격의 히트페이즈와
데미지로 신규 Ability가 발동한다.**

**`SetCurrentAbility`도 상태 전환은 하지 않는다.** 전환은 호출자 책임이다
(`EnemyCombat.cs:492`, `EnemyFlyingAIController.cs:347`,
`EnemyFlyingAirCircleState.cs:227`이 각각 처리). 따라서 구독 핸들러는:

```
SetCurrentAbility(ability)  →  실패면 종료
  → 공격 상태로 전환          →  실패면 CancelCurrentAbility()로 커밋 롤백
```

롤백을 빠뜨리면 §3-F가 `Immediate`에 금지한 바로 그 상태("쿨다운·자원은
소모됐는데 모션이 없는")를 Request 경로에서 재현한다.

**테스트**

- T4-1 구독자가 없으면 실행이 생성되지 않고 `StateTransitionRejected`가 집계된다.
- T4-2 `EnemyCombat` 구독 상태에서 트리거 요청이 실제 실행으로 이어진다.
- T4-3 **BT 활성 실행 중 도착한 트리거 Request가 폐기되고 기존 실행이 유지된다**
  (`CancelExisting` Ability로 검증 — 게이트 6).
- T4-4 `SetCurrentAbility` 성공 후 `_currentSkill` / `_currentHitPhaseIndex`가
  신규 Ability 기준으로 갱신된다.
- T4-5 상태 전환 실패 시 커밋이 롤백된다(쿨다운·자원 미소모).

**완료 판정:** T4-1~5 + 몬스터 1종에 트리거 Ability를 붙여 Play Mode 확인.

---

### 5단계 — PlayerCombat 구독

**목표:** 플레이어 쪽 Request 경로를 연결한다.

**초안의 전제 두 가지가 모두 틀렸으므로 재작성한다.**

**(1) `CanTransitionState`는 목적지 상태의 메서드다.**

```csharp
// ActorMovementController.cs:207
if (CurrentState != null && newState.CanTransitionState(CurrentState.StateId) == false)
```

인자는 `string stateName`이 아니라 `ActorStateId`이고, 메서드는 **목적지 상태
인스턴스**가 소유하며 *현재* 상태 ID를 받는다. "현재 상태의 `CanTransitionState`"는
방향이 뒤집힌 서술이다. 게다가 `TryTransitionToState`(`:202-214`)가 이 검사를
이미 내장하므로 **사전 판정을 따로 두지 않는다** — 수락 여부는
`TryTransitionToState`의 반환값으로 정의한다.

> CLAUDE.md의 `CanTransitionState(string stateName)` 서술도 낡았다. 별건으로 정정 대상.

**(2) `PlayerCombat`에는 임의 Ability를 활성화하는 일반 경로가 없다.**
플레이어의 슬롯 Prepare/Commit은 `PlayerAttackState.cs:844-873`에 있고,
**상태에 이미 진입한 뒤** `GetMotion()` 안에서 `HasSkillInput(i)` 폴링으로 슬롯을
고른다. `PlayerCombat`이 가진 Prepare 경로는 Ultimate 하나뿐이고(`:684-720`)
그것도 상태 진입은 `UltimateSequencePlayer.PlayPrepared`에 위임한다.

따라서 5단계의 실작업은 "구독해서 `CanTransitionState`를 본다" 한 줄이 아니라
**두 개의 신규 설계**다.

| 작업 | 내용 |
| --- | --- |
| 5-A | Ability → 목적지 상태 매핑 정의 (현재 어디에도 없음) |
| 5-B | 외부 주입형 Ability 활성화 통로를 `PlayerAttackState`에 신설. 기존 `_forcedAttackAction`(`:102-105`) 생성자 주입 패턴을 따른다 |
| 5-C | 수락 = `TryTransitionToState` 반환값. 거부 시 Prepare된 핸들의 `Abort` 책임을 구독 핸들러에 명시 |

거부는 정상 동작이며 로그를 남기지 않는다(매 프레임 발생 가능).
`AbilityTriggerCancelRequested`는 해당 실행이 구동 중인 상태를 종료시킨다.

**완료 판정:** Play Mode 수동 검증 + **PlayMode 수직 슬라이스 최소 1건.**
초안은 "자동 테스트를 두지 않는다"고 했으나
`Assets/Tests/PlayMode/Ability/GameplayAbilityVerticalSlicePlayModeTests.cs`가
이미 존재하므로 근거가 없다. 최소한 "트리거 Request → 상태 진입 → Commit" 왕복
1건은 자동화한다.

---

### 6단계 — Source/Target 태그 조건 + 검증 + 에디터

**목표:** UE의 3자(owner/source/target) 태그 검사를 완성하고 저작 도구를 붙인다.

#### 6-A. Source/Target 조건

`Evaluate()`는 이미 `resolvedTarget`을 갖고 있으므로(`:621-627`) 추가 비용이 거의 없다.

- **Target** — `target.AbilitySystem.Tags`로 검사. 타깃이 없으면 조건을 건너뛴다
  (`targetPolicy == Required`가 이미 null을 걸러낸다).
  **3단 null 검사가 필요하다** — `AbilitySystemComponent.Tags`는 `:55`에서
  `Runtime?.Tags`로 null을 반환할 수 있다. `target != null`(Unity 오버로드가 파괴
  감지) → `AbilitySystem != null` → `Tags != null`. 기존 `Evaluate` `:621-627`의
  관용구를 그대로 잇는다.
- **Source** — 트리거로 활성화된 경우 `TriggerEvent.Instigator`를
  `AbilitySystemComponent.TryResolve()`(`:118-133`)로 역참조해 검사. 이 API는
  `WeakReference` + Unity null 비교로 파괴된 컴포넌트를 걸러내므로 안전하다.
  수동 활성화이거나 Instigator가 없으면 조건을 건너뛴다.

**`Evaluate()`에만 파라미터를 추가해서는 실효가 없다.** `Evaluate`는 private이고
실제 활성화 경로는 `TryPrepareAbility`(`:250`)이며, 그 공개 진입점
4개(`:173`·`:193`·`:215`·`:235`)에는 트리거 이벤트를 받을 통로가 없다. 4·5단계
Request 구독자는 이 오버로드를 부르므로, 통로를 뚫지 않으면 **Request로 활성화된
Ability는 Source 조건이 항상 조용히 통과한다.** 같은 이유로 §3-E의
`AbilityExecution.TriggerEvent`도 채워지지 않아 "나를 때린 대상에게 반격"이
Immediate(Background 전용)에서만 되고 정작 모션이 필요한 Request에서는 안 된다.
8-3-C의 피격 리액션 이관이 전부 이 경로다.

→ 작업 범위를 다음으로 확장한다.

1. `Evaluate()` + `TryPrepareAbility` / `TryPreparePlayerSlot` **4개 오버로드
   전부**에 `GameplayEventData? triggerEvent = null` 추가.
2. `AbilityExecution` 생성자(`:274-275` 호출부)까지 전달.
3. 4단계 `AbilityTriggerRequest.TriggerEvent`를 구독자가 되돌려주는 계약 정의.

**조회 API와 활성화 API의 판정을 일치시킨다.** `EvaluateAbility`(`:164`)는
`EnemyCombat.CanActivateAbility`(`:668`)를 거쳐 BT 후보 필터링에 쓰인다. 조회가
Source 조건을 건너뛰고 활성화만 검사하면, BT가 "가능"으로 분기한 뒤 Prepare가
실패해 **몬스터가 한 틱 정지한다**(다인 전투에서 눈에 띈다). 조회 API에도 같은
인자를 전달하고, "조회 결과 == 활성화 결과" 불변식을 테스트로 고정한다.

#### 6-B. `AbilityDataValidator` 규칙

| 검사 | 수준 |
| --- | --- |
| `triggerTag`가 비었거나 Registry 미등록 | Error |
| `Immediate` + `concurrency != Background` | Error |
| **`Immediate` + Background인데 `backgroundMaxDurationSeconds <= 0`** | **Error** |
| 트리거 태그가 자신의 `executionGrantedTagIds`에 포함 | Error |
| `OwnedTagPresent` + `concurrency != Background` | Error |
| **Imbue 슬롯 Ability에 트리거 지정** | **Error** (§3-A 인덱스 갱신 불가) |
| 모든 `AbilityTagRequirement` 태그의 Registry 등록 여부 | Error (기존 `ValidateTagList` 재사용) |
| `requireAll`과 `blockAny`에 같은 태그 | Warning (영구 차단) |
| **레거시 리스트와 `ownerTagRequirement`가 동시에 채워짐** | **Warning** (한 Ability가 두 매칭 규칙으로 쪼개짐) |
| `cancelAbilitiesWithTag`에 자신의 `abilityTagIds`가 포함 | Error (자기 취소) |
| 같은 태그·소스 트리거가 한 AbilitySet 안에 3개 이상 | Warning (**`ValidateSet` 소관**) |

**배치 주의 2건.**

1. "AbilitySet 안에 3개 이상"은 단일 Ability를 보는 `ValidateAbility`(`:241`)
   범위 밖이다. `ValidateSet`(`:468`)에서 baseSet/override 해석 후 수행한다.
2. `ValidateAbility`는 `:276-280`에서 **variant가 없으면 조기 `return`한다.**
   기존 `ValidateTagList` 호출은 `:381-383`으로 그 뒤에 있다. 트리거 검증을
   같은 자리에 두면 variant 없는 에셋에서 트리거 오류가 보고되지 않는다.
   → `:255` 근처(activation 검사 블록)로 올린다.

#### 6-C. Ability Editor

**속성 매핑(`:2549-2562`)만 고쳐서는 탭이 생기지 않는다.** 탭 목록은 별도 배열이다.

```csharp
// GameplayAbilityEditorWindow.cs:466-470
string[] labels =
{
    "기본 정보", "활성화 조건", "비용/쿨다운", "Variant",
    "Effect", "저장/교체 정책", "정적 밸런스", "검증 결과",
};
```

편집 대상은 다섯 곳이다.

| 위치 | 내용 |
| --- | --- |
| `:466-470` | `labels`에 `"트리거"` 추가 (이게 없으면 버튼 자체가 안 생긴다) |
| `:2549-2562` | `GetPropertiesForTab`에 `triggers` 매핑 |
| `:2750` 근처 | `GetPropertyLabel` 한글 라벨 |
| `:2799` 근처 | `GetPropertyHelp` 툴팁 |
| `:2680`·`:2700` 근처 | 탭 도움말 2종 |

**부수 정정 3건.**

- 실제 탭 이름은 `"활성화"`가 아니라 **`"활성화 조건"`**(`:2554`)이다.
- 그 탭은 이미 `new[] { "activation" }`이고, 3종 `AbilityTagRequirement`는
  `AbilityActivationRules`의 **하위 필드**라 기본 드로어가 자동 렌더한다.
  "넣는" 작업은 불필요하고, 대신 **라벨/툴팁 맵이 중첩 필드까지 적용되는지**
  확인해야 한다(두 맵은 현재 최상위 이름에만 걸린다).
- `labels`는 **4개 에셋 타입이 공유**하므로 `"트리거"` 탭이
  `GameplayEffectSO`/`PassiveAbilitySO`/`AbilitySetSO`에도 나타나 빈 탭이 된다.
  타입별 탭 표시 조건을 함께 정한다.

> 프로젝트 규약상 데이터 필드를 추가하면 커스텀 인스펙터도 같이 갱신한다.

#### 6-D. 디버그

`AbilitySystemDebugSnapshot`에 최근 트리거 발화 N건(태그·**abilityId 문자열**·결과)을
추가하고 **`GasRuntimeDebuggerWindow`**에서 확인한다.
`AbilityDebugRecorder`가 이미 태그 추가를 기록하므로 트리거 결과만 얹으면 된다.

**정정 2건.** `AbilitySystemDebugSnapshot`은 `Ability/Core/AbilityDebugRuntime.cs`,
즉 **Core asmdef** 안에 있다. 따라서 (a) `GameplayAbilitySO`(Data asmdef)를 담으면
§2 **원칙 5**를 스스로 위반하므로 `abilityId` 문자열로 저장하고,
(b) 이 타입을 소비하는 창은 `GasRuntimeDebuggerWindow`이지 `AbilitySandboxWindow`가
아니다(후자는 이 타입을 참조하지 않는다).

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
   **위치는 `TryConsumeCost` 성공 이후, `AddExecutionTags`(`:856`) 직전으로
   고정한다.** Commit 선두에 두면 `TryConsumeCost` 실패 시 `Abort`로 빠지는데
   (`:295-302`), 이미 취소된 대상은 되살아나지 않아 **B만 죽고 A는 없는** 상태가
   된다. 이 지점 이후 Commit은 실패 반환 경로가 없으므로 취소가 원자적이다.
   §2 원칙 2(원자성)를 7단계가 깨지 않게 하는 핵심이다.
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

4. **차단 매칭은 계층으로 한다.** 딕셔너리 정확 키 조회로 구현하면 Exact가 되어,
   보스가 `Ability.Player.Skill`로 봉인해도 `abilityTagIds`가
   `Ability.Player.Skill.Fire`인 Ability는 하나도 차단되지 않는다. UE
   `BlockAbilitiesWithTag`도 계층 매칭이다. → "차단 키 각각에 대해
   `abilityTagIds` 중 하나가 `IsChildOf(key)`인지" 검사로 정의한다.
   키 수가 적으므로 선형 순회로 충분하다.
5. **`Commit()`에서 차단을 재검사한다.** 3번의 검사는 `Evaluate()` = Prepare
   시점뿐인데, Prepare와 Commit은 같은 프레임일 필요가 없다(`:289`가
   `PreparedFrame + 1`까지 허용). 그 사이 다른 Ability가 차단을 등록하면
   **차단을 통과해 실행된다.** 트리거 도입 후 이 창에서 태그가 바뀔 확률이
   구조적으로 올라간다. → `Commit()`에도 검사를 넣고 실패 시 `Abort` +
   `BlockedByActiveAbility` 반환.
6. **초기화 경로를 반드시 만든다.** `CancelAllAbilities()`는 `_executions.Count == 0`이면
   `:377-383`에서 조기 반환하고, `Dispose()`도 카운트를 건드리지 않는다.
   카운트가 1 남으면 **해당 액터의 그 태그를 가진 모든 Ability가 영구 차단**되고
   `BlockedByActiveAbility`만 반복 집계되어 추적이 매우 어렵다.
   → `CancelAllAbilities`의 **양쪽 반환 경로**와 `Dispose`에서 `Clear()`.
   감소는 `EndExecution`의 `Remove` 성공 분기 안, `CleanupExecution`(`:1090`) 옆에 둔다.
7. **`Abort()`에 Active 가드를 넣는다.** `Abort`(`:332-343`)는 상태와 무관하게
   `_executions.Remove`만 하고 `CleanupExecution`도 차단 해제도 하지 않는다.
   그런데 외부 호출부 3곳(`EnemyCombat.cs:704-708`, `PlayerAttackState.cs:865-868`,
   `PlayerCombat.cs:719`)이 Commit 실패 후 무조건 `Abort`한다. Commit이
   `AlreadyCommitted`(`:285-286`)를 반환하면 실행은 **Active**이고 태그·차단이
   이미 등록된 상태라 **영구 누수**가 된다. 7단계가 이 누수의 비용을
   "영구 차단"으로 키우므로 여기서 가드를 추가한다.

> **순서 규칙에 대한 정정:** 초안 4번은 "취소를 먼저, 자기 등록을 나중에" 하지
> 않으면 "차단 해제가 자기 등록을 덮어쓴다"고 했으나, **카운트 표현에서는
> 증감이 서로를 덮어쓸 수 없다.** 덮어쓰기는 집합/bool 표현에서만 성립하는
> 위험이다. 순서 자체는 무관하고, 진짜 제약은 위 1번(`TryConsumeCost` 이후)이다.

**테스트**

- T7-1 `cancelAbilitiesWithTag`가 매칭되는 활성 Ability를 취소한다.
- T7-2 `blockAbilitiesWithTag` 활성 중에는 매칭 Ability가
  `BlockedByActiveAbility`로 거부된다.
- T7-3 차단 Ability가 **취소로** 종료돼도 차단이 해제된다 (정상 종료뿐 아니라).
- T7-4 **Commit이 `InsufficientResource`로 실패하면 `cancelAbilitiesWithTag`
  대상이 생존한다** (1번 원자성).
- T7-5 **Prepare 이후 Commit 이전에 차단이 등록되면 Commit이 거부된다** (5번).
- T7-6 **하위 계층 `abilityTagIds`가 상위 차단 태그로 차단된다** (4번).
- T7-7 **`CancelAllAbilities` / `Dispose` 이후 차단 카운트가 0이다** (6번).
- T7-8 Active 실행에 `Abort`가 불려도 차단·태그가 누수되지 않는다 (7번).

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

**(2) 동기 제약은 존재하지만 그 근거는 히트스톱이 아니다.**

초안은 "`ReceiveHit`이 `CombatResult`를 동기 반환하고 공격자가 그 값으로
히트스톱·피드백·바이탈오브를 처리한다"고 썼는데 **사실이 아니다.** 히트스톱과
바이탈오브는 `AttackData` 경로로 흐른다.

```csharp
// CombatFeedbackDispatcher.cs:150, 175 — 인자가 AttackData다
public static void ApplyPlayerAttackHitFeedback(AttackData attackData, ...)
    ...
    GameCombatMgr?.TrySpawnVitalOrb(orbTrigger, attackData.hitPoint);
```

`CombatResult` 반환값의 실제 소비처는 데미지 플로터(`PlayerCombat.HitDetection.cs`),
투사체 패리 반사(`ProjectileRuntime.cs`의 `DefenseOutcome`), 텔레메트리
(`CombatLogRecorder`)다. `ReactionDecision`을 읽는 곳은 **텔레메트리뿐이고
게임플레이 소비자가 0명이다.**

**진짜 동기 제약은 두 가지다.**

| 제약 | 근거 |
| --- | --- |
| 사망 판정 | `CombatFeedbackDispatcher`가 히트 직후 `IDamageable.IsAlive()`를 즉시 읽어 킬 히트를 판별한다 |
| 패리 반사 | `ProjectileRuntime`이 `DefenseOutcome`을 동기로 요구한다 |

둘 다 **리액션 상태 전환보다 앞 단계**이므로, 리액션만 이관하는 결론은 유지된다.
근거만 위 표로 교체한다.

> 초안이 "트리거는 큐로 의도적으로 지연된다"고 쓴 것도 부정확하다. 3-B는
> 락 깊이 0이면 **같은 호출 스택에서 동기 실행**한다. 이 사실이 아래 8-3의
> 발급 지점 결정에 직접 영향을 준다(H2 → 8-3 참조).

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

#### 8-1. 순간 사건은 태그가 아니라 GameplayEvent로 발급한다

**초안의 "펄스 태그"는 폐기한다. 태그 refcount 의미론과 맞지 않는다.**

```csharp
// GameplayTagRuntime.cs:87-90
bool existed = _counts.TryGetValue(tag, out int count);
_counts[tag] = count + 1;
_owned.Add(value, new OwnedTag(tag, sourceType, sourceId));
if (!existed) TagAdded?.Invoke(tag);   // ← 이미 있으면 무발화
```

`TagAdded`는 **0→1 전이에서만** 발화한다. 펄스 태그를 `LateTick`에서 제거하면
그 프레임 내내 태그가 살아 있으므로, **같은 프레임의 두 번째 피격은 트리거를
발화시키지 못하고 조용히 소실된다.** 다인 전투·AOE·다중 히트박스에서 상시
발생하며, "모든 피격" 계층 매칭으로 저작한 리액션 Ability가 특히 취약하다.

→ 피격·공격 개시는 **`AbilityTriggerSource.GameplayEvent`로 발급한다.**
`GameplayEventRouter.Send`(`GameplayEventRuntime.cs:55-110`)는 상태를 갖지 않아
같은 프레임 N회 발급이 N회 발화한다. UE도 순간 사건은
`SendGameplayEventToActor`를 쓴다 — 레퍼런스와도 일치한다.

```csharp
public void IssueTriggerEvent(GameplayTag eventTag, in HitContext context);
// 내부적으로 GameplayEventData(eventTag, instigator, target, payload) 구성 후 Send
```

**태그는 조건 검사에만 쓴다.** 즉 "`Trigger.*` 태그를 발급한다"는 초안의 방향은
사건 축에서 철회하고, 태그의 역할을 §3.1 게이팅으로 한정한다. 이 분리가
UE GAS의 `TriggerSource.GameplayEvent` vs `ActivationRequiredTags` 구분과 같다.

> **비용 주의:** `GameplayEventData.Payload`는 `object`(`GameplayEventRuntime.cs:34`)라
> `HitContext`(readonly struct)를 실으면 **매 히트 박싱**된다. 다인 전투에서
> GC 압력이 된다. 8-3-B 단계에서 실측하고, 필요하면 재사용 래퍼 클래스를 둔다.

#### 8-2. 태그 규약

레지스트리(`Assets/Resources/GameplayTagRegistry.asset`)의 기존 규약
(`State.*` / `Motion.*` / `Combo.*`)을 따라 `Trigger.*` 루트를 신설한다.

**공통 태그는 발급하지 않는다.** `AbilityTagId.IsChildOf`(`GameplayTagRuntime.cs:19-21`)가
접두 매칭이고 계층 매칭이 기본값이므로, `Trigger.Monster.Hit.Light` 하나만
발급해도 `Trigger.Monster.Hit` 조건은 이미 매칭된다. 공통 태그를 따로 쏘면
중복 발화만 늘어난다. 아래 목록에서 루트 항목은 **조건 저작용 상위 노드**이지
발급 대상이 아니다.

```
Trigger.Monster.Hit                     ← 조건 저작용 (발급 안 함)
Trigger.Monster.Hit.Light
Trigger.Monster.Hit.Heavy
Trigger.Monster.Hit.KnockBack
Trigger.Monster.Hit.Airborne
Trigger.Monster.Hit.Knockdown
Trigger.Monster.Hit.Stun
Trigger.Monster.Hit.Grab
Trigger.Monster.Hit.PoiseBreak          ← Poise 파손과 동시 발생 시 추가 발급

Trigger.Monster.Attack                  ← 조건 저작용 (발급 안 함)
Trigger.Monster.Attack.<Category>       ← 카테고리별. AbilityAttackCategory 기준
```

피격 하위 태그는 `AttackReactionType` enum과 1:1이다. 계층 매칭으로 "모든 피격"을,
`Exact`로 "이 반응만"을 저작한다.

**공격 태그는 Ability별이 아니라 카테고리별이다.** 초안은
`Trigger.Monster.Attack.<AbilityId>`를 규정했으나 **발급 시점에 AbilityId를 알 수
없다.** BT 노드가 예약하는 것은 카테고리뿐이고(`ExecuteEnemyAttackNode.cs:11`·`:100`),
실제 Ability 선택은 **상태 진입 시점**의 가중치 롤이다:

```csharp
// EnemyAttackState.cs:74 (EnemyCounterState:64, EnemyFlyingGroundAttackState:57 동일)
_currentSkill = _combat.SelectAndExecuteSkill(distanceToTarget);
```

Ability별 트리거를 고집하면 `SelectWeighted`(`EnemyCombat.cs:1046`)를 BT 쪽으로
끌어올려야 하는데, 이는 위 3개 상태의 진입 계약을 바꾸는 작업이라 8단계 범위를
넘는다. **따라서 8단계는 카테고리 단위로 확정하고, Ability 선택은 지금처럼
상태에 남긴다.** "Ability별 트리거"는 명시적으로 범위 밖이다(§9 추가).

> 이 결정으로 마이그레이션 M2도 바뀐다. `aiSelectable` Ability마다 트리거를
> 부여하면 아무도 발급하지 못하는 사문(死文) 트리거가 대량 생긴다.

#### 8-3. 피격 리액션 이관 (3단계로 나눠 진행)

발급 지점은 `MonsterActor.ApplyResolvedHit`에서 **`OnDeath()` 호출 뒤**이며
`IsAlive()` 가드를 건다.

```csharp
ReactionDecision reactionDecision = OnDamaged(...);   // Poise/Break 적용 완료
if (_currentHealth <= 0)
    OnDeath();
if (IsAlive())                                        // ← 발급 지점
    IssueTriggerEvent(...);
return CombatResolutionPipeline.WithMonsterAppliedResources(...);
```

초안은 `OnDamaged` 직후로 지정했으나, 드레인이 락 깊이 0에서 동기 실행되므로
(8-0-(2) 각주) **치명타 히트에서 죽은 몬스터가 리액션 Ability를 실행한 뒤
곧바로 `OnDeath()`가 Death 상태로 덮어쓴다.** 상태 전환 2회 + 디졸브·드롭
타이밍 흔들림. Poise/Break는 `OnDamaged`에서 이미 끝나므로 발급을 뒤로 미뤄도
리액션 Ability가 파손 여부를 읽는 데 지장이 없다.

**8-3-0. 상태 태그 부여 보강 (선결 작업)**

**`State.Hit`은 재사용할 수 있는 상태가 아니다.** `GameplayTags.cs:22-24`에
정의만 있고 **부여하는 코드가 전 프로젝트에 없다.**

```csharp
// GameplayTags.cs:22-24 — 정의뿐, AddTag 호출부 0건
public static readonly GameplayTag State_Hit   = GameplayTag.CreateCodeDefined("State.Hit");
public static readonly GameplayTag State_Death = GameplayTag.CreateCodeDefined("State.Death");
public static readonly GameplayTag State_Grabbed = ...
```

실제로 태그를 부여하는 상태는 전 프로젝트에 8곳뿐이고(`State.Airborne`, `State.Jump`,
`State.Combat.Attack`, `State.Dash`, `State.Move`, `State.Sprint`,
`State.Combat.Counter`), `EnemyHitState`는 태그를 전혀 다루지 않는다.
`State.Stun` / `State.Knockdown`은 레지스트리에도 없다.

→ 8-3-C에 앞서 **Hit / Stun / Knockdown / Grabbed / Death / SpecialBreakVictim
상태에 태그 부여·해제를 넣고 레지스트리에 등록한다**(M1 확장).
이 작업 없이 리액션 Ability에 `blockAny: State.Hit`을 걸면 인게임에서 영원히
발화하지 않으며, EditMode에서 태그를 수동 주입한 T8-6은 **가짜 그린**이 된다.

**`CanPlayHitReaction`의 두 항목은 태그로 옮기지 않는다.**

```csharp
// MonsterActor.cs:501-514
if (_externalHitReactionSuppressionCount > 0) return false;      // 외부 카운터
...
return state.StateId is not (...7종...) && state.CanPlayHitReaction(hit);  // 히트 컨텍스트 인자
```

`_externalHitReactionSuppressionCount`(런타임 카운터)와
`state.CanPlayHitReaction(hit)`(히트 컨텍스트를 받는 상태별 가상 메서드)는
정적 태그로 등가 변환되지 않는다. 이 둘은 **발급 게이트로 남긴다** — 즉
`IssueTriggerEvent` 호출 자체를 이 조건으로 감싼다. 상태 ID 7종만 태그로 옮긴다.

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

> **함정 1:** `EnemyTacticalMemory`가 이중 통지된다. `MonsterActor.cs:408`이
> `NotifyTookDamage(hit, isPoiseBroken)`을, `EnemyHitState.cs:35-37`이
> `WasHitRecently(0.05f)` 가드 뒤에 다시 `NotifyTookDamage()`를 호출한다.
> 지금은 같은 프레임이라 가드가 먹지만 리액션이 한 프레임이라도 늦어지면
> 판정이 달라질 수 있다(특히 히트스톱으로 `Time.time`이 정지하는 구간).
> **8-3-B shadow 비교 항목에 `EnemyTacticalMemory` 히트 카운트를 포함한다.**
>
> **함정 2:** 카운터 반격은 `OnDamaged` `:419-425`에서 **조기 return**하므로
> `ApplyMonsterReactionState`를 타지 않는다. 발급 태그를 `hit.ReactionType`
> 기준으로 할지 `reactionDecision.TargetState` 기준으로 할지 정해야 한다 —
> 8종 `AttackReactionType`과 5종 `CombatReactionState`는 **1:1이 아니다**
> (`Pull`·`KnockBack`은 `CombatReactionState`에 없다). 8-2는
> `hit.ReactionType` 기준으로 확정하고, 카운터 히트는 별도 태그를 발급한다.

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

**대상 노드가 하나가 아니다.** `RequestEnemyActionNode.cs:145-160`이
`ExecuteEnemyAttackNode`와 **구조적으로 동일한 두 번째 공격 개시 경로**다
(슬롯 예약 → 카테고리 예약 → `NotifyBTAttackStarted` → `EnemyAttackState` 전환).
Rules JSON의 `"action": "RequestAction"`이 여기로 매핑되고, SourceJson 중
`EnemyBehavior_GroundMelee_Balanced.json`은 두 노드를 **모두** 쓴다.
8-4와 M4의 대상에 이 노드를 포함한다.

`EnemyCounterState:64` / `EnemyFlyingGroundAttackState:57` /
`EnemyFlyingAirCircleState:227`은 `SelectAndExecuteSkill`·`SetCurrentAbility`를
직접 호출하는 별도 개시 경로이며 **8단계 범위 밖으로 선언한다.**

**네 가지 함정을 처리한다.**

1. **거부 이벤트가 없다.** `Evaluate()` 실패는 로그도 이벤트도 남기지 않는다.
   T8-9(슬롯 반납)가 이를 전제하는데 3~4단계 어디에도 계약이 없다.
   → **`AbilityTriggerRejected(ability, reason)` 이벤트를 4단계에 정의한다.**
2. **슬롯 누수.** 슬롯 소유권은 **상태 수명에 묶여 있다**
   (`EnemyAttackState.cs:119` `OnExit`의 `ReleaseGroupSlot()`,
   `EnemyAIController.cs:377-383`은 Attack 상태가 아닐 때만 기회적 회수).
   트리거가 거부되면 두 경로 모두 걸리지 않아 슬롯이 `HandleTargetLost`까지
   남는다. 그룹 동시 공격 제한이 1~2슬롯이면 **그룹 전체가 정지**한다.
   → 노드가 거부 이벤트를 받아 `ReleaseGroupSlot()`을 직접 호출한다.
   직전 공격의 슬롯을 오반납하지 않도록 소유 여부를 확인한다.
3. **대기 중 재발급·재예약.** `OnUpdate`는 `_attackStarted`가 false인 한 매 틱
   `:45` 이후 전체 경로를 다시 탄다. 대기 프레임을 도입하면서 플래그를 두지
   않으면 프레임 2에서 슬롯 재예약과 **이중 발급**이 일어난다.
   → `_triggerIssued` + `_triggerFrame` 플래그로 재진입을 막는다.
4. **성공 판정 오귀속.** 현재 판정은 상태 기반이다
   (`:33` `CurrentState?.StateId == ActorStateId.Attack`). 대기 사이에
   **다른 원인**(카운터, 그룹 반응, 다른 브랜치)으로 Attack에 들어가도 자기
   트리거가 성공한 것으로 간주한다.
   → 성공 판정을 **자기가 발급한 트리거의 실행 핸들**로 바꾼다.

#### 8-5. BT 노드 추가

**`CanActivateAbilityNode` (`BTConditionNode`)** — `EnemyCombat.CanActivateAbility`
(`:668`)를 감싼다. 이 메서드는 `TryEvaluateAbility` → `Evaluate()`를 거치므로
1·6단계의 태그 조건을 자동 반영한다.

> **초안의 "conditional abort와 자연스럽게 결합한다"는 서술은 철회한다.**
> 쿨다운은 `Commit()` `:304`의 `StartCooldown`에서 시작되고 `Evaluate()` `:630-631`이
> 쿨다운을 검사하므로, **공격이 시작된 바로 다음 틱부터 이 조건은 false가 된다.**
> Sequence(abort=Self) 아래에 두면 `SequenceNode.cs:72-77`이 자기 브랜치를
> abort하는데, `runningChild.Abort()`는 BT 노드만 멈출 뿐 `EnemyAttackState`는
> 계속 돌고 슬롯도 `OnExit`까지 잡혀 있다. 매 공격마다 BT가 루트로 되돌아가는
> **진동**이 생긴다.
>
> → 이 노드를 abort 조건으로 쓰지 않는다. 쓰려면 판정을
> `IsExecuting || CanActivate`로 바꿔 실행 중인 Ability를 "가능"으로 간주해야
> 한다. 어느 쪽이든 문서에 명시하고 Validator/리뷰 체크리스트에 넣는다.

```csharp
[SerializeField] private GameplayAbilitySO _ability;
[SerializeField] private AbilityAttackCategory _category = AbilityAttackCategory.None;
// _ability 지정 시 단일 Ability 판정,
// 미지정 시 _category의 후보가 하나라도 활성화 가능한지 판정
```

**`IssueAbilityTriggerNode` (`BTActionNode`)** — 지정 카테고리 태그로
`IssueTriggerEvent`를 발급한다. `ExecuteEnemyAttackNode`를 대체하는 게 아니라,
접근·슬롯 로직은 남기고 상태 전환 부분만 이 노드로 교체한다.
8-4의 함정 3·4(재진입 플래그, 실행 핸들 기반 성공 판정)를 이 노드가 소유한다.

> BT 저작 포맷이 둘(Rules JSON / raw BT-node JSON)이므로 두 경로 모두에
> 노드를 등록해야 한다. `generate-bt-json` 스킬의 노드 목록도 갱신한다.

#### 8-6. 마이그레이션 도구

`MonsterTagTriggerMigrationWindow` (Editor 전용).

> 2026-08-09 기준 이 마이그레이션 창(`MonsterTagTriggerMigrationWindow`,
> `PlayerTagTriggerMigrationWindow`)은 일회성 도구로서 코드에서 제거되었다.
> 아래 표는 실행된 작업의 기록이며, 결과는 에셋의
> `tagTriggerMigrationVersion`과 `AbilityDataIntegrityTests`가 보증한다.

| 작업 | 내용 |
| --- | --- |
| M1 | `Trigger.*` 태그를 레지스트리에 일괄 등록 |
| M2 | 카테고리별 공격 트리거 Ability를 AbilitySet에 부여 (`Request` 모드). **`aiSelectable` Ability마다 부여하지 않는다** — 8-2에서 카테고리 단위로 확정했으므로 Ability별 부여는 아무도 발급하지 못하는 사문 트리거가 된다 |
| M3 | `AttackReactionType` 8종에 대응하는 `GA_Monster_Hit_*` 리액션 Ability 생성 (`AbilityAssetFactory` 재사용) |
| M4 | BT JSON의 `ExecuteEnemyAttackNode`를 `CanActivateAbilityNode` + `IssueAbilityTriggerNode` 조합으로 변환 |
| M5 | 드라이런 리포트 출력 (변경 예정 에셋·BT 목록) |

**프로젝트 안전 규칙을 그대로 따른다** (CLAUDE.md "Editor 데이터 도구 안전 규칙").

- 기존 에셋 식별은 **GUID 정확 일치 → path 정확 일치** 순. 둘 다 유효하지
  않으면 **이름 폴백 없이 실패**시킨다. (M2·M3에 적용)
- 예외 발생 시 해당 Undo group 전체를 `Undo.RevertAllDownToGroup`으로 롤백한다.
  일부 적용 상태를 성공처럼 collapse하지 않는다. (M2·M3에 적용)
- **in-place 필드 추가 요구는 M2·M3(SO 필드)에만 적용한다.**
- 드라이런(M5)을 통과하지 않으면 실행 버튼을 비활성화한다.

**M4(BT JSON 변환)는 위 두 규칙이 원리적으로 적용되지 않는다.**

```csharp
// MonsterBehaviorTreeJsonImporter.cs:386-393
foreach (var oldNode in oldNodes)
    if (oldNode != null)
        UnityEngine.Object.DestroyImmediate(oldNode, true);
UnityEngine.Object.DestroyImmediate(generatedTree);
```

BT 임포트는 서브에셋 노드를 전부 파괴하고 재생성한다. `DestroyImmediate(obj, true)`로
파괴된 서브에셋은 Undo 그룹으로 복원되지 않으므로 T8-13의 "예외 시 전체 롤백"이
이 경로에는 **무효**다. 다만 임포터는 이미 자체 스테이징/복구를 갖고 있고
(`:334-384`, 옛 노드 파괴를 커밋 이후로 미룸) 이는 P09 방식보다 안전하다.

→ M4의 안전 규칙을 **"임포터의 기존 스테이징 복구 계약을 따른다"**로 정정한다.

**JSON↔에셋이 1:1이 아니다.** 실측:

| 항목 | 수 |
| --- | --- |
| `BT_*.asset` | 17 |
| `SourceJson/` + `Json/` | 13 |
| 소스 JSON이 없는 에셋 | 4 |
| 대응 에셋이 없는 JSON | 2 |

소스가 없는 4개는 M4로 마이그레이션할 수 없다. **M5 리포트에 "수동 처리 대상"
섹션을 따로 만든다.**

**대상 식별에 텍스트 스캔을 쓸 수 없다.** `.asset`은 노드를 스크립트 GUID로
직렬화하므로 `grep "ExecuteEnemyAttackNode" *.asset`은 0건이다. M5 드라이런은
반드시 에셋을 로드해서 판정한다.

**롤백:** 마이그레이션 전 대상 에셋을 `Assets/10.Datas/_MigrationBackup/`에
복사하고, `AbilitySetSO`에 `tagTriggerMigrationVersion` int 필드를 두어
재실행 시 멱등성을 보장한다.

#### 8-7. 테스트

| ID | 내용 |
| --- | --- |
| T8-1 | 피격 시 반응타입 GameplayEvent가 발급된다 |
| T8-2 | **같은 프레임 2회 피격에서 이벤트가 2회 발화한다** (초안 펄스 태그의 결함 회귀 방지) |
| T8-3 | 발급 전후로 `DefenseOutcome` / `FinalDamage` / 킬 히트 `IsAlive()` 타이밍이 불변 (8-0-(2) 실제 계약) |
| T8-4 | `useTagTriggeredHitReaction = false`면 기존 switch 경로가 동작한다 |
| T8-5 | 리액션 Ability가 `TriggerEvent`에서 공격자를 읽는다 |
| T8-6 | `State.Hit` 보유 중에는 리액션 Ability가 재발화하지 않는다. **8-3-0의 태그 부여를 실제 상태 진입으로 검증한다**(수동 태그 주입 금지 — 가짜 그린 방지) |
| T8-7 | 킬 히트에서 리액션 Ability가 활성화되지 않는다 (`IsAlive()` 가드) |
| T8-8 | `_externalHitReactionSuppressionCount > 0`이면 발급되지 않는다 |
| T8-9 | `CanActivateAbilityNode`가 쿨다운 중 Ability에 Failure를 반환한다 |
| T8-10 | **실행 중인 Ability에 대해 노드가 브랜치를 abort하지 않는다** (C3 진동 방지) |
| T8-11 | 트리거 거부 시 `AbilityTriggerRejected`가 발화하고 공격 슬롯이 반납된다 |
| T8-12 | 대기 프레임 동안 슬롯 재예약·이중 발급이 없다 (8-4 함정 3) |
| T8-13 | 다른 원인으로 Attack 상태에 진입해도 노드가 성공으로 오귀속하지 않는다 (함정 4) |
| T8-14 | `RequestEnemyActionNode` 경로도 동일하게 동작한다 |
| T8-15 | 마이그레이션 재실행이 멱등이다 (`tagTriggerMigrationVersion`) |
| T8-16 | M2·M3 예외 시 전체 롤백된다 (M4는 임포터 스테이징 계약으로 별도 검증) |
| T8-17 | 소스 JSON이 없는 BT 4개가 M5 리포트의 수동 처리 섹션에 나온다 |

**완료 판정:** T8-1~17 통과 + 몬스터 3종 Play Mode 비교 검증 + 드라이런
리포트 검토. 8-3-B shadow 비교에 `EnemyTacticalMemory` 히트 카운트 포함.

#### 8-8. 진행 순서

```
8-1 (이벤트 발급 API) → 8-2 (태그 등록/M1)
  → 8-3-0 (상태 태그 부여 보강)   ← 선결. 없으면 8-3-C가 성립하지 않음
  → 8-3-A (발급만) → 8-3-B (shadow) → 8-3-C (상태 전환 이관)
  → 8-5 (BT 노드)  → 8-4 (공격 이관)
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

**초안이 트리거 담당으로 지목한 3가지는 이미 전부 구현되어 있다.**
`PlayerAttackState.OnEnter`(`:368-401`)가 7개 pending/window 플래그를 순차
소비한다 — `IsParryCounterAvailable`, `State_Combat_Counter` 태그,
`ConsumePerfectGuardCounterWindow`, `ConsumeDodgeCounterWindow`,
`ConsumeSwapEvadeCounterAttackPending`, `ConsumeSwapSpecialAttackPending`,
`ConsumeEntryAttackPending`.

즉 9-2는 새 경로를 얹는 게 아니라 **기존 7개 경로와 직접 경합한다.**
착수 전에 항목별로 대체/공존을 결정하는 표를 먼저 만든다. 이 결정 없이는
9-2가 성립하지 않는다.

**이중 발화 방지 규칙.**

1. 트리거는 **입력이 만들지 않는 활성화**만 담당한다. 일반 공격 입력 경로는
   기존 그대로 둔다.
2. **트리거 경로는 `PlayerInterruptAction` 강제 진입(`hasForcedAttack`)으로만
   상태에 들어간다.** 초안의 "트리거는 `InputBuffer`를 소비하지 않는다"는
   트리거 계층에서 강제할 수 없다 — 버퍼 소비는 활성화 경로가 아니라
   **상태 진입부**에서 일어나기 때문이다:

   ```csharp
   // PlayerAttackState.cs:353-355
   _isHeavyAttack = !hasForcedAttack
                    && PlayerAttackInputArbiter.TryConsumeAttackInput(out bool consumedHeavy)
                    && consumedHeavy;
   ```

   `hasForcedAttack`가 아니면 진입 순간 버퍼가 소비된다.
3. 트리거 Ability와 입력 슬롯 Ability가 같은 에셋이면 Validator **Error**.

수락/거부는 5단계에서 정정한 대로 `TryTransitionToState` 반환값으로 정의한다
(`CanTransitionState` 직접 호출 금지). 거부는 정상이므로 로그를 남기지 않는다.

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

1. **교체 시점에 활성 트리거 실행이 취소된다.** `SetAbilitySet`이
   `CancelAllAbilities()`를 먼저 호출하는 것은 확인함(`:99`).
   `OwnedTagPresent`로 켜둔 오라가 교체와 함께 꺼지는 것이 의도인지 확인 필요.
2. **복원이 인덱스 재구축 뒤에 온다.** 호출부가 `SetAbilitySet` 직후
   저장 상태를 복원한다:

   ```csharp
   // PlayerActor.Components.cs:113-118
   Abilities?.SetAbilitySet(data.abilitySet);          // ← 여기서 인덱스 재구축
   Abilities?.SetResourceRules(data.abilityResourceRules);
   if (_characterAbilitySystemMap.TryGetValue(data.characterType, out AbilitySystemSaveData savedState))
       Abilities?.RestoreAbilitySystemStateForCharacter(savedState);   // ← 태그·이펙트 부활
   ```

   복원이 태그를 되살리면 `OwnedTagPresent` 트리거가 **복원 도중 발화한다.**
   캐릭터 교체·세이브 로드 시 오라/패시브가 의도치 않게 자동 활성화된다.
   → **복원 중 트리거 억제 스코프**를 두고 복원 완료 후 인덱스를 재구축한다.
   (3-B의 `AbilityListLock`을 재사용할 수 있다.)

> 초안의 "벤치 캐릭터 트리거" 항목은 삭제했다. 파티원마다 ASC가 있는 게 아니라
> **단일 `PlayerActor`가 모델 데이터를 갈아끼우는** 구조라 벤치 캐릭터의
> AbilitySet은 런타임에 존재하지 않는다. 해당 테스트는 자명하게 통과하는
> 공테스트였다.

#### 9-5. 테스트

| ID | 내용 |
| --- | --- |
| T9-1 | 플레이어 피격 시 `Trigger.Player.Hit.*` 발급, `CombatResult` 불변 |
| T9-2 | `State.SuperArmor` 보유 중 리액션 Ability가 발화하지 않는다 |
| T9-3 | `useTagTriggeredPlayerHitReaction = false`면 기존 경로 동작 |
| T9-4 | 입력 1회에 실행이 1회만 발생한다 (`InputBuffer` 이중 발화 방지) |
| T9-5 | 트리거 활성화가 `InputBuffer`를 소비하지 않는다 |
| T9-6 | 입력 슬롯과 트리거에 같은 Ability를 지정하면 Validator Error |
| T9-7 | `ComboRouteData` 통합 후 기존 라우트 판정 결과가 불변. **두 소스가 같은 명시 태그를 추가하고 한쪽이 먼저 제거하는 케이스를 반드시 포함한다** (아래 주석) |
| T9-8 | 콤보 라우트에서 `requireAny`가 동작한다 |
| T9-9 | 캐릭터 교체 시 이전 캐릭터의 트리거가 발화하지 않는다 |
| T9-10 | 세이브 복원 중 `OwnedTagPresent` 트리거가 발화하지 않는다 |

> **T9-7의 함정:** `ComboRouteData.CheckTagConditions`는 프로젝트
> `GameplayTagContainer`를 읽고, `AbilityTagRequirement`는 GAS 애그리게이터를
> 읽는다. 미러링이 있어 대체로 일치하지만 **명시 태그(`AddTag(tag)`)는 GAS 쪽에서
> refcount되지 않는다** — `GameplayTagContainer.cs:60` `!_gasExplicit.ContainsKey(tag)`
> 가드 때문이다. 두 소스가 같은 태그를 넣고 한쪽이 먼저 `RemoveTag`하면
> GAS 쪽 태그가 조기 소멸한다. 9-3이 평가 주체를 GAS로 옮기면 이 비대칭이
> **처음으로 게임플레이에 노출된다.**

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
- **6-A의 Source 조건 완성은 4 이후**다. Source 조건은 Request 경로에 전달돼야
  실효가 있는데(§6-A) 그 경로를 4·5가 만든다. 6을 4보다 먼저 하면 Source 조건이
  Immediate에서만 도는 반쪽으로 머문다. 6의 나머지(Target 조건·Validator·에디터)는
  3에만 의존하므로 4와 병렬 가능하다.
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
| T3-9 | `TagRemoved`→취소→`CleanupExecution`→`TagRemoved` 순환이 종료된다 (§7 3행) | 3 |
| T3-10 | 락 스코프 중 발생한 트리거가 스코프 해제 후 1회만 처리된다 (§7 4행) | 3 |
| T3-11 | `Drain()` 중 예외가 나도 `_listLockDepth`가 복구된다 (3-B) | 3 |
| T4-1~5 | Request·게이트 6·`SetCurrentAbility`·롤백 (§4 4단계) | 4 |
| T5-1 | 트리거 Request → 상태 진입 → Commit 왕복 (PlayMode 수직 슬라이스) | 5 |
| T6-1 | 타깃 `blockAny` → `BlockedByTag` | 6 |
| T6-2 | Instigator `requireAll` 미충족 → `MissingRequiredTag` | 6 |
| T6-3 | Validator가 `Immediate`+비-Background를 Error 보고 | 6 |
| T6-4 | **조회(`EvaluateAbility`) 결과 == 활성화(`TryPrepareAbility`) 결과** | 6 |
| T6-5 | Target ASC가 null이거나 액터가 파괴돼도 예외 없이 스킵 | 6 |
| T7-1~8 | 취소·차단 (§4 7단계) | 7 |
| T8-1~13 | 몬스터 이관·마이그레이션 (§4 8-7 참조) | 8 |
| T9-1~10 | 플레이어 전환 (§4 9-5 참조) | 9 |

---

## 7. 재진입 위험 요약

트리거 도입으로 새로 생기는 순환 경로를 한곳에 모은다.

| 순환 | 방어 |
| --- | --- |
| Commit → `executionGrantedTagIds` → TagAdded → 자기 트리거 | 게이트 1 + Validator Error |
| Commit → 태그 → 다른 Ability 트리거 → Commit → … | 게이트 4 (드레인 예산 64) |
| **Commit 도중 재진입해 낡은 `_primaryExecution`/State를 관측** | **`AbilityListLock` + `AddExecutionTags` 위치 이동 (3-B)** |
| TagRemoved → 취소 → `CleanupExecution` → 태그 제거 → TagRemoved | `AbilityListLock` 큐 직렬화 (3-B·3-D) |
| **`CancelAllAbilities` 도중 생성된 실행이 `_backgroundExecutions`에서 지워져 고아** | **`AbilityListLock` (3-B 결함 2)** |
| **트리거 처리 중 예외 → 락 깊이 고착 → 시스템 무음 정지** | **`try/finally` + 항목별 `try/catch` (3-B)** |
| 7단계 취소 → Commit 실패 → 취소 대상만 사망 | 취소를 `TryConsumeCost` 이후로 고정 (7단계 1번) |
| 8단계 리액션 Ability가 데미지를 주고 그 피격이 다시 리액션 트리거 | 게이트 4 + `State.Hit` `blockAny` (8-3-0에서 태그 부여 선행) |
| 8단계 킬 히트에서 리액션 활성화 후 `OnDeath`가 상태 덮어씀 | 발급을 `OnDeath` 뒤로 + `IsAlive()` 가드 (8-3) |
| 9단계 세이브 복원이 `OwnedTagPresent`를 되살려 자동 활성화 | 복원 중 트리거 억제 스코프 (9-4) |

---

## 8. UE 대비 의도적 차이

| 항목 | UE | 본 설계 | 이유 |
| --- | --- | --- | --- |
| 활성화 | `CommitAbility` 단일 | Prepare → 검증 → Commit | 모션 해석 실패가 진행 중 Ability를 끊지 않게 하는 원자성. 프로젝트 고유 강점이므로 유지 |
| 트리거 실행 주체 | ASC가 직접 | Immediate / Request 분리 | ASC가 상태 머신을 소유하지 않음. 위 계약에서 파생 |
| `OwnedTagRemoved` | 없음 | 없음 | 초안에 있었으나 `OwnedTagPresent`로 교체. 취소 짝이 없는 트리거는 오라를 끌 수 없다 |
| 순간 사건 트리거 | `SendGameplayEventToActor` | `GameplayEvent` 소스 (8-1) | 초안의 "펄스 태그"는 `TagAdded`가 0→1에서만 발화해 같은 프레임 2회 피격을 잃는다. UE와 같은 방식으로 회귀 |
| Ability별 트리거 | 가능 | **카테고리 단위** (8-2) | Ability 선택이 BT가 아니라 상태 진입 시점의 가중치 롤이라 발급 시점에 AbilityId를 모른다 |

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
- **몬스터 Ability별 트리거.** 8-2에서 카테고리 단위로 확정했다. Ability 단위로
  가려면 `SelectWeighted`(`EnemyCombat.cs:1046`)를 BT로 끌어올려
  `EnemyAttackState`/`EnemyCounterState`/`EnemyFlyingGroundAttackState` 3곳의
  진입 계약을 바꿔야 한다. 별도 과제로 분리한다.
- **비행·카운터 공격 개시 경로.** `EnemyCounterState:64`,
  `EnemyFlyingGroundAttackState:57`, `EnemyFlyingAirCircleState:227`은
  8단계 이관 대상이 아니다.
- **지속 효과의 트리거 대체.** 지속 상태는 기존대로 `GameplayEffectSO`가
  담당한다. 트리거는 활성화 계기일 뿐이다.
