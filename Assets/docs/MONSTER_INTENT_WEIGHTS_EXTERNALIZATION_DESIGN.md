# 몬스터 Intent 가중치 외부화 설계 문서

> 작성일: 2026-05-23
> 최근 갱신: 2026-05-23 (구현 진행 상태 반영)
> 대상 버전: Unity 6 (6000.0.60f1), URP
> 관련 시스템: `EnemyCombatDecisionEvaluator`, `EnemyBehaviorSO`, `BehaviorTreeRunner`

---

## 구현 진행 상태 (2026-05-23 기준)

| Phase | 목표 | 상태 | 참고 |
|-------|------|------|------|
| **A** | `IntentEvaluationContext`, `IntentConditionId`, `IntentConditionEvaluator` 신설 | ✅ 완료 | 모든 클래스 존재, 입력 모음 구조체화 완료 |
| **B** | `EnemyIntentWeightsSO` 신설, `IntentScoreComputer` 도입 | ✅ 완료 | SO 9개 Intent 엔트리 완성, 정적 클래스로 분리됨¹ |
| **C** | `IW_Default_Melee` 자산 생성 + 회귀 테스트 | ✅ 완료 | 기본값 프로파일 4개 모두 존재 |
| **D** | `IW_AggressiveMelee` / `IW_DefensiveShield` / `IW_RangedCaster` 작성 | ✅ 완료 | 4개 자산 모두 생성됨 |
| **E** | Intent 선택 안정화 보정 (`DecisionIntentLockedUntil` 등) | 🟡 부분 | 반복 패널티만 구현; 유지 보너스, 전환 비용, 최소 유지 시간 미정 |
| **F** | `EnemyActionStyleProfileSO` 도입, Archetype Style Profile | ⬜ 미진행 | 별도 마일스톤 (구현 대기 중) |
| **G** | 매직 넘버 fallback 제거, 인스펙터 미리보기 | ⬜ 미진행 | fallback 경로 (`LegacyIntentScoring`) 여전히 살아있음 (의도된 이중 경로) |

> ¹ 설계 문서에서는 `EnemyCombatDecisionEvaluator` 내부 메서드 `ComputeIntentScore`로 명시했으나, 실제 구현에서는 `IntentScoreComputer` 정적 클래스로 분리됨.

---

## 0. 요약

`EnemyCombatDecisionEvaluator.cs`에 하드코딩된 Intent 점수 가중치(약 60여 개의 매직 넘버)를 **ScriptableObject로 외부화**한다. 적 유형/역할/페이즈별로 다른 가중치 프로파일을 갈아 끼울 수 있도록 만들어, 같은 BT로 "공격적 검사", "신중한 방패병", "카운터 특화 어쌔신" 같은 성격 차이를 인스펙터에서 표현한다.

**ROI 최상.** 인프라는 이미 모두 깔려 있고, 표면적인 매직 넘버를 데이터로 옮기는 작업이라 위험도 낮고 효과 크다.

---

## 1. 현재 구현 상태

### 1.1 점수 계산 위치

`Assets/02.Scripts/GameActor/Component/Enemy/EnemyCombatDecisionEvaluator.cs:86-180`

```csharp
var attackScore = 0.10f + aggression * 0.42f;
if (inAttackRange && actionDelayElapsed) attackScore += 0.45f;
if (canUseSkill)                          attackScore += 0.08f;
if (isPlayerStaggered)                    attackScore += 0.18f;
if (isPlayerRecoveringFrequently)         attackScore += 0.10f;
if (!hasAvailableAttack || !actionDelayElapsed) attackScore *= 0.55f;
// ... (Punish, Counter, Pressure, Chase, Retreat, KeepDistance, Defend, Recover 동일 패턴)
```

### 1.2 문제점

| 문제 | 영향 |
|------|------|
| 매직 넘버 약 60개가 코드에 흩어져 있다 | 적 성격 차이를 만들려면 코드 분기 또는 SO 값 우회로 변경해야 함 |
| 변경 시 컴파일 사이클 필요 | 디자이너 튜닝 불가, 플레이테스트 반복이 느리다 |
| 적 유형/페이즈별 차이가 `EnemyBehaviorSO`의 4~5개 확률 값으로만 표현된다 | `continueAttackChance`, `guardChance`, `retreatChance` 등 — 모든 Intent의 9개 점수 곡선을 표현할 수 없음 |
| 같은 가중치 패턴이 9개 Intent에 반복된다 | 코드량 ↑, 일관성 깨지기 쉬움 |

### 1.3 이미 SO로 외부화된 인접 데이터

- `EnemyBehaviorSO.continueAttackChance / guardChance / retreatChance` — Intent 일부에 입력값으로 사용
- `EnemyBehaviorSO.phases[i]` — HP 임계값 기반 페이즈 전환
- `EnemyBehaviorSO.aiRole` — Melee/Ranged/Support 역할 enum

> 이 인프라 위에 **Intent별 점수 가중치 SO**를 얹는 형태로 자연스럽게 확장 가능하다.

---

## 2. 설계 목표

1. **모든 점수 가중치를 인스펙터 노출** — 디자이너가 컴파일 없이 튜닝
2. **역할/페이즈별 프로파일** — `Melee 잡몹`, `Ranged 캐스터`, `Support 힐러`, `Boss Phase 2 광폭화` 등
3. **코드 변경 없이 신규 적 추가** — SO 만들고 `EnemyBehaviorSO`에 참조 끼우면 끝
4. **튜닝 가시화** — Inspector에서 어떤 조건이 어떤 점수에 기여하는지 한눈에 파악
5. **현행 동작 보존** — 기본값으로 채워진 SO 하나가 기존 매직 넘버와 동일한 결과를 내야 한다
6. **Intent와 실행 Style 책임 분리** — Intent는 "무엇을 하려는가", Style은 "어떤 상태/모션으로 실행할 것인가"만 담당한다

### 2.1 Intent/Style 책임 경계

현재 `CombatIntent`와 `EnemyActionStyle`은 이미 분리되어 있으나, 일부 Intent 이름은 실행 상태와 의미가 가까워질 수 있다. 본 설계 이후에는 다음 원칙을 유지한다.

| 계층 | 책임 | 예시 |
|------|------|------|
| Intent | 전술 목적 | `Attack`, `Punish`, `Counter`, `Pressure`, `Chase`, `Retreat`, `KeepDistance`, `Defend`, `Recover` |
| Style | 실행 방식 | `Circle`, `Flank`, `Charge`, `Guard`, `Dodge`, `JumpBack`, `Dive`, `TakeOff` |
| Resolver | Intent/Style을 실제 State로 변환 | 지상 `Pressure + Flank` → `EnemyFlankState`, 비행 `Pressure + Circle` → `EnemyFlyingCircleState` |

예를 들어 `Pressure`는 "압박" 목적일 뿐이며, 근접형은 `Charge`, 민첩형은 `Flank`, 원거리형은 `KeepDistance`성 `Circle`, 비행형은 `AirCircle`로 실행할 수 있다. 따라서 신규 가중치 SO는 **Intent 점수만 계산**하고, 구체 동작 선택은 `EnemyActionResolver` 또는 별도 Style Resolver로 위임한다.

---

## 3. 데이터 구조 설계

### 3.1 신규 SO: `EnemyIntentWeightsSO`

```csharp
[CreateAssetMenu(fileName = "IntentWeights", menuName = "UPlayGround/Enemy/Intent Weights")]
public class EnemyIntentWeightsSO : ScriptableObject
{
    [Header("Intent별 가중치 프로파일")]
    public IntentWeightEntry attack;
    public IntentWeightEntry punish;
    public IntentWeightEntry counter;
    public IntentWeightEntry pressure;
    public IntentWeightEntry chase;
    public IntentWeightEntry retreat;
    public IntentWeightEntry keepDistance;
    public IntentWeightEntry defend;
    public IntentWeightEntry recover;
}

[Serializable]
public class IntentWeightEntry
{
    [Tooltip("이 Intent의 기본 점수(0~1)")]
    [Range(0f, 1f)] public float baseScore = 0.10f;

    [Tooltip("조건이 참일 때 가산되는 보너스 항목")]
    public List<ConditionBonus> bonuses = new();

    [Tooltip("조건이 참일 때 점수에 곱해지는 항목")]
    public List<ConditionMultiplier> multipliers = new();
}

[Serializable]
public class ConditionBonus
{
    [Tooltip("AND 조건들 (모두 참이어야 적용)")]
    public List<IntentConditionId> conditions = new();
    [Range(-0.5f, 0.5f)] public float amount = 0.1f;
}

[Serializable]
public class ConditionMultiplier
{
    public List<IntentConditionId> conditions = new();
    [Range(0f, 2f)] public float factor = 0.55f;
}
```

### 3.2 조건 식별자: `IntentConditionId`

기존 `EnemyCombatDecisionEvaluator`의 모든 분기 조건을 열거형으로 등록한다.

```csharp
public enum IntentConditionId
{
    InAttackRange,
    ActionDelayElapsed,
    CanUseSkill,
    HasAvailableAttack,
    IsPlayerAttacking,
    IsPlayerGuarding,
    IsPlayerStaggered,
    IsPlayerRecovering,
    IsPlayerDodgingFrequently,
    IsPlayerAttackingFrequently,
    IsPlayerGuardingFrequently,
    IsPlayerRecoveringFrequently,
    WasHitRecently,
    IsPoiseBroken,
    IsTooClose,
    IsUnderPreferredRange,
    IsOverPreferredRange,
    IsLowHealth,
    TimeSinceRetreatBelowCooldown,
    HasGuardMotion,
    // ... (Evaluator가 사용하는 조건 모두 등록)
}
```

조건 평가는 `IntentConditionEvaluator` 정적 클래스에서 단일 진입점으로 처리:

```csharp
public static bool Evaluate(IntentConditionId id, in IntentEvaluationContext ctx);
```

### 3.3 입력 컨텍스트: `IntentEvaluationContext`

`EnemyCombatDecisionEvaluator`가 매 틱 채워 넘기는 readonly struct. Blackboard·Detection·Memory·AIContext에서 모은 모든 입력값을 한 구조체에 모은다.

```csharp
public readonly struct IntentEvaluationContext
{
    public readonly float Distance;
    public readonly float OptimalDistance;
    public readonly float PersonalSpace;
    public readonly float PreferredRange;
    public readonly float Aggression;
    public readonly float ReactionChance;
    public readonly float HealthPercent;
    public readonly bool  CanUseSkill;
    public readonly bool  HasAvailableAttack;
    public readonly bool  ActionDelayElapsed;
    public readonly bool  IsPlayerAttacking;
    public readonly bool  IsPlayerGuarding;
    public readonly bool  IsPlayerStaggered;
    public readonly bool  IsPlayerRecovering;
    public readonly bool  IsPlayerDodgingFrequently;
    public readonly bool  IsPlayerAttackingFrequently;
    // ...
}
```

### 3.4 EnemyBehaviorSO 변경

```csharp
[Header("Intent Weights")]
[Tooltip("기본 Intent 가중치 SO. 페이즈에서 오버라이드되지 않으면 이걸 사용한다.")]
public EnemyIntentWeightsSO intentWeights;

[Tooltip("같은 Intent 점수를 어떤 실행 Style로 풀지 결정하는 성격 프로파일")]
public EnemyActionStyleProfileSO actionStyleProfile;

[Serializable]
public class BehaviorPhase
{
    // ... 기존 필드
    [Tooltip("이 페이즈에서 사용할 Intent 가중치 SO. null이면 EnemyBehaviorSO.intentWeights 사용.")]
    public EnemyIntentWeightsSO intentWeightsOverride;
}
```

런타임 조회 우선순위: `phase.intentWeightsOverride` → `EnemyBehaviorSO.intentWeights` → `Defaults.Fallback`.

### 3.5 신규 SO: `EnemyActionStyleProfileSO` (선택)

Intent 외부화가 안정화된 뒤 추가한다. 같은 Intent라도 몬스터 성격에 따라 다른 실행 Style을 고르기 위한 데이터다.

```csharp
[CreateAssetMenu(fileName = "ActionStyleProfile", menuName = "UPlayGround/Enemy/Action Style Profile")]
public class EnemyActionStyleProfileSO : ScriptableObject
{
    public IntentStyleEntry attack;
    public IntentStyleEntry punish;
    public IntentStyleEntry pressure;
    public IntentStyleEntry keepDistance;
    public IntentStyleEntry defend;
    public IntentStyleEntry retreat;
}

[Serializable]
public class IntentStyleEntry
{
    public List<WeightedActionStyle> candidates = new();
}

[Serializable]
public class WeightedActionStyle
{
    public EnemyActionStyle style;
    [Range(0f, 1f)] public float weight = 1f;
    public List<IntentConditionId> conditions = new();
}
```

초기 구현에서는 `EnemyActionResolver`의 기존 기본 매핑을 유지한다. Style Profile은 후속 단계에서만 사용하며, null이면 현행 동작을 그대로 따른다.

---

## 4. 점수 계산 알고리즘

`EnemyCombatDecisionEvaluator`의 9개 점수 계산 블록을 다음 단일 메서드로 교체¹:

```csharp
private float ComputeIntentScore(IntentWeightEntry weights, in IntentEvaluationContext ctx)
{
    float score = weights.baseScore;

    foreach (var bonus in weights.bonuses)
    {
        if (AllConditionsTrue(bonus.conditions, ctx))
            score += bonus.amount;
    }

    foreach (var mul in weights.multipliers)
    {
        if (AllConditionsTrue(mul.conditions, ctx))
            score *= mul.factor;
    }

    return Mathf.Max(0f, score);
}
```

> ¹ **참고:** 실제 구현에서는 이 메서드가 `IntentScoreComputer` 정적 클래스의 `Compute(IntentWeightEntry, in IntentEvaluationContext)` 메서드로 분리되어 있다. `EnemyCombatDecisionEvaluator`에서는 `IntentScoreComputer.Compute()` 호출로 점수를 산출한다. (122~151줄 참조)

### 4.1 연속값 보너스 (Aggression 같은 0~1 계수)

`baseScore = 0.10f + aggression * 0.42f` 같은 패턴은 별도 처리. `IntentWeightEntry`에 다음 추가:

```csharp
[Tooltip("Aggression(0~1) 값에 곱해져 base에 가산되는 양")]
public float aggressionInfluence = 0f;

[Tooltip("ReactionChance(0~1) 값에 곱해져 base에 가산되는 양")]
public float reactionChanceInfluence = 0f;

// ... 필요한 연속값만
```

`baseScore + aggressionInfluence * ctx.Aggression`을 최종 base로 사용.

> **참고:** 실제 구현에서는 `IntentWeightEntry`에 이러한 연속값 필드들이 통합되어 있으며, `IntentScoreComputer.Compute()` 내부에서 기본값 계산 시 참고된다.

### 4.2 Intent 선택 안정화

점수 외부화 후에는 같은 입력에서도 SO 값 변화로 Intent가 더 자주 흔들릴 수 있다. 이를 막기 위해 선택 단계에 다음 보정을 둔다. (Phase E)

| 보정 | 목적 | 상태 |
|------|------|------|
| 유지 보너스 | 직전 Intent가 아직 유효하면 짧은 시간 동안 유지 | ⬜ 미정 |
| 반복 패널티 | 같은 Intent가 과도하게 반복되면 감쇠. 현재 `DecisionConsecutiveIntentCount`와 연동 | ✅ 구현됨 (`ApplyLastIntentPenalty`) |
| 전환 비용 | `Attack → Retreat`, `Retreat → Attack` 같은 급전환은 강한 조건이 있을 때만 허용 | ⬜ 미정 |
| 최소 유지 시간 | 선택된 Intent는 기본 0.4~1.0초 유지. 피격/PoiseBreak/타겟 상실은 즉시 예외 | ⬜ 미정 |

추가 Blackboard 키 (설계된 것 — 현재 미정):

```csharp
DecisionIntentLockedUntil      // float — 미정
DecisionIntentSwitchCost       // float, 디버그용 — 미정
DecisionIntentStickinessBonus  // float, 디버그용 — 미정
```

이 보정은 Intent 점수 계산 이후, `SelectWeightedTopIntent` 직전에 적용한다.

---

## 5. 기본값 SO 자산

다음 4개 기본 SO를 `Assets/10.Datas/AI/IntentWeights/`에 배치한다. (Phase F의 `IW_Skirmisher`, `IW_Bruiser`는 별도 마일스톤에서 추가 예정)

| 자산 이름 | 목적 | 상태 |
|----------|------|------|
| `IW_Default_Melee.asset` | 현재 매직 넘버와 동일한 결과 (기준선) | ✅ 존재 |
| `IW_AggressiveMelee.asset` | Attack/Punish/Pressure 가중치 ↑, Retreat 가중치 ↓ | ✅ 존재 |
| `IW_DefensiveShield.asset` | Defend/KeepDistance 가중치 ↑↑, Counter 보너스 강화 | ✅ 존재 |
| `IW_RangedCaster.asset` | KeepDistance/Retreat 가중치 ↑, Attack은 거리 조건 강화 | ✅ 존재 |

각 자산은 인스펙터에서 보너스 리스트 형태로 직접 편집 가능.

### 5.1 AI Archetype 권장 세트 (Phase F — 별도 마일스톤)

개별 몬스터마다 BT를 복제하지 않고, Intent Weight + Action Style Profile 조합으로 성격을 만든다. 다음은 설계 당시 권장 세트이며, 실제 Phase F 구현 시 조정될 수 있다.

| Archetype | Intent Weight | Action Style Profile | 설명 |
|-----------|---------------|----------------------|------|
| Bruiser | `IW_Bruiser` (미구현) | `ASP_Bruiser` (미구현) | 높은 Poise, 근접 압박, Counter/Punish 선호 |
| Skirmisher | `IW_Skirmisher` (미구현) | `ASP_Skirmisher` (미구현) | Flank, Dodge, 짧은 교전 후 이탈 |
| Guardian | `IW_DefensiveShield` | `ASP_Guardian` (미구현) | Guard/Counter 중심 |
| RangedCaster | `IW_RangedCaster` | `ASP_RangedCaster` (미구현) | KeepDistance, 원거리 스킬, 거리 재조정 |
| FlyingHarasser | `IW_RangedCaster` 또는 전용 | `ASP_FlyingHarasser` (미구현) | 공중 유지, Dive 쿨다운 압박 |

---

## 6. 마이그레이션 절차

### 6.1 호환성 원칙

- `EnemyBehaviorSO.intentWeights`가 null이면 **현행 하드코딩 매직 넘버를 그대로 사용**한다 (Defaults.Fallback 경로 — `LegacyIntentScoring`)
- 신규 SO를 채택한 적만 새 알고리즘으로 점수 계산
- 기존 BT 에셋, 기존 적 프리팹 변경 없음

### 6.2 마이그레이션 단계 (✅ Phase A~D 완료, Phase E 부분 진행)

1. ✅ `IW_Default_Melee.asset`을 생성하고 `EnemyCombatDecisionEvaluator`의 매직 넘버와 동일한 값으로 채운다 — 완료
2. ✅ 기준 적 1마리(예: 가장 단순한 잡몹)의 `EnemyBehaviorSO.intentWeights`에 연결 — 완료
3. ✅ 점수 분포가 변경 전과 동일한지 BT Debug Trace로 확인 (검증 시나리오 7.1) — 완료
4. ✅ 나머지 적의 `EnemyBehaviorSO`에 순차 연결 — 완료
5. ⬜ 모든 적이 SO를 사용하는 것이 확인되면 매직 넘버 fallback 경로 제거 (Phase G — 별도 마일스톤)

---

## 7. 검증 / 테스트 시나리오

| ID | 시나리오 |
|----|---------|
| 7.1 | `IW_Default_Melee` 적용 전후 동일 입력 → 동일 9개 점수 출력 (회귀 테스트) |
| 7.2 | `IW_AggressiveMelee` 적용 시 동일 상황에서 Attack 점수가 기본보다 20% 이상 높음 |
| 7.3 | 페이즈 전환 직후 `intentWeightsOverride`가 활성화되어 점수 분포 즉시 변화 |
| 7.4 | 인스펙터에서 보너스 한 줄 추가 → 게임 실행 중 즉시 반영 (Edit-time only가 아니어야 함) |

---

## 8. 인스펙터 / 에디터 지원

### 8.1 기본 PropertyDrawer

`IntentWeightEntry`를 폴딩 가능한 박스로 표시. 각 보너스를 한 줄에:
```
[X] InAttackRange ∧ ActionDelayElapsed   +0.45
[X] CanUseSkill                            +0.08
```

### 8.2 Intent Score Preview (선택, 후속 작업)

`EnemyIntentWeightsSO` 인스펙터 하단에 미리보기 패널 추가:
- "거리: [슬라이더] 0~10m, HP%: [슬라이더] 0~100%" 등 주요 입력을 슬라이더로 조작
- 9개 Intent의 현재 점수를 막대그래프로 표시
- 입력 변화에 따른 Intent 선택 결과를 즉시 확인

> 이 미리보기는 별도 마일스톤. 본 설계의 필수 범위는 아니다.

---

## 9. 신규/변경 클래스 요약

| 위치 | 변경 종류 | 상태 | 비고 |
|------|----------|------|------|
| `Assets/02.Scripts/Data/Actor/Enemy/EnemyIntentWeightsSO.cs` | 신규 | ✅ 완료 | 본 설계의 중심, 9개 Intent 엔트리 |
| `Assets/02.Scripts/Data/Actor/Enemy/EnemyActionStyleProfileSO.cs` | 신규 | ⬜ 미진행 | Intent별 실행 Style 후보 (Phase F) |
| `Assets/02.Scripts/Data/Actor/Enemy/IntentConditionId.cs` | 신규 | ✅ 완료 | 조건 열거형 |
| `Assets/02.Scripts/Data/Actor/Enemy/IntentEvaluationContext.cs` | 신규 | ✅ 완료 | readonly struct |
| `Assets/02.Scripts/AI/CombatDecision/IntentConditionEvaluator.cs` | 신규 | ✅ 완료 | 조건 평가 정적 클래스 |
| `Assets/02.Scripts/AI/CombatDecision/IntentScoreComputer.cs` | 신규 | ✅ 완료 | 점수 계산 정적 클래스¹ |
| `Assets/02.Scripts/AI/CombatDecision/LegacyIntentScoring.cs` | 신규 | ✅ 완료 | SO null 시 fallback 경로 |
| `Assets/02.Scripts/GameActor/Component/Enemy/EnemyCombatDecisionEvaluator.cs` | 리팩토링 | ✅ 완료 | 9개 점수 블록 → `IntentScoreComputer.Compute()` 호출 |
| `Assets/02.Scripts/Data/Actor/Enemy/EnemyBehaviorSO.cs` | 필드 추가 | 🟡 부분 | `intentWeights` ✅ / `BehaviorPhase.intentWeightsOverride` ✅ / `actionStyleProfile` ⬜ (Phase F) |
| `Assets/10.Datas/AI/IntentWeights/IW_*.asset` | 신규 (4개) | ✅ 완료 | 기본 프로파일: Melee, AggressiveMelee, DefensiveShield, RangedCaster |

> ¹ 설계 문서에서는 `ComputeIntentScore` 메서드를 `EnemyCombatDecisionEvaluator` 내부에 배치하도록 기술했으나, 실제 구현에서는 `IntentScoreComputer` 정적 클래스로 분리되어 재사용성을 높임.

---

## 10. 작업 순서

1. **Phase A (1일)** ✅ 완료 — `IntentEvaluationContext`, `IntentConditionId`, `IntentConditionEvaluator` 신설. 매직 넘버는 그대로 두고 입력 모음만 구조체로 묶기 (리팩토링 안전판)
2. **Phase B (1일)** ✅ 완료 — `EnemyIntentWeightsSO`, `IntentWeightEntry` 추가. `IntentScoreComputer` 정적 클래스 신설¹. SO null일 때 기존 코드 그대로 사용 (이중 경로 유지)
3. **Phase C (반일)** ✅ 완료 — `IW_Default_Melee` 자산 생성 + 회귀 테스트 (7.1) 통과
4. **Phase D (반일)** ✅ 완료 — `IW_AggressiveMelee` / `IW_DefensiveShield` / `IW_RangedCaster` 자산 작성
5. **Phase E (반일)** 🟡 부분 — Intent 선택 안정화 보정 중 반복 패널티(`ApplyLastIntentPenalty`)는 구현됨. `DecisionIntentLockedUntil`, 전환 비용, 유지 보너스는 미정
6. **Phase F (별도 마일스톤)** ⬜ 미진행 — `EnemyActionStyleProfileSO` 도입, Archetype별 Style Profile 작성
7. **Phase G (별도 마일스톤)** ⬜ 미진행 — 모든 적이 SO 채택 확인 후 매직 넘버 fallback 제거. 인스펙터 미리보기 패널.

총 3.5일 + 후속 마일스톤.

> ¹ 설계 문서의 `ComputeIntentScore` 메서드는 `IntentScoreComputer.Compute()` 정적 메서드로 실장됨.

---

## 11. 명시적 비목표

- **신규 Intent 추가하지 않는다.** 본 설계는 기존 9개 Intent의 가중치만 외부화한다.
- **BT 구조를 복잡하게 만들지 않는다.** BT는 Interrupt/Acquire/Intent 평가/실행 라우팅만 담당한다.
- **Behavior Tree 노드는 변경하지 않는다.** Service/Evaluator 인터페이스 유지.
- **그룹 단위 가중치 보정은 본 설계 범위 밖.** `MONSTER_GROUP_AI_ADVANCEMENT_DESIGN.md` Phase 2에서 다룬다.

---

## 12. 참고

- 관련 코드: `EnemyCombatDecisionEvaluator.cs`, `EnemyBehaviorSO.cs`, `BehaviorPhase`
- 관련 설계 문서:
  - `MONSTER_GROUP_AI_ADVANCEMENT_DESIGN.md` (그룹 단위 Intent Bias)
  - `Assets/docs/Complete/monster_ai_bt_design_gdd_kr.md`
