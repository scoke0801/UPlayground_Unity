# 보스 다단계 행동 계획(Hierarchical Plan) 설계 문서

> 작성일: 2026-05-23
> 대상 버전: Unity 6 (6000.0.60f1), URP
> 관련 시스템: `BehaviorTreeRunner`, `EnemyActionResolver`, `EnemyCombatDecisionEvaluator`

---

## 0. 요약

현재 Intent 시스템은 **매 BT 틱마다 한 가지 의도를 골라 즉시 실행**하는 반응형 구조다. 보스가 보여줘야 할 **"유인 → 페인트 → 진짜 공격"** 같은 다단계 계획은 BT의 Sequence로 흉내 가능하지만, 중단/재계획 비용이 크고 BT 에셋이 비대해진다.

보스 한정으로 **Hierarchical Plan 레이어**를 BT 위에 얹는다. 각 Plan은 Intent의 순서 리스트이며, 트리거 조건이 만족되지 않을 때까지 순차 실행된다. 잡몹에는 적용하지 않는다 (과투자).

Plan은 "Phase의 대체재"가 아니다. Phase는 수치와 후보군을 바꾸는 전투 구간이고, Plan은 그 구간 안에서 실행되는 짧은 전술 시퀀스다.

---

## 1. 배경

### 1.1 현재 구조의 한계

| 한계 | 예시 |
|------|------|
| 단일 틱 단일 Intent | 보스가 "후퇴 → 거리 두고 충전 → 돌진" 흐름을 만들려면 BT 노드를 직접 연결해야 함 |
| Intent 점수 평탄화 | 직전 행동 컨텍스트가 점수에만 반영되어 "방금 페인트 모션 끝났으니까 이제 진짜 공격" 같은 강제 흐름이 어려움 |
| 재계획 비용 | BT Sequence 중 한 노드가 Failure 시 처음부터 다시 평가. "특정 조건이면 처음 두 단계는 건너뛰기" 같은 부분 재계획 불가 |
| BT 에셋 비대화 | 다단계 패턴을 BT로 표현하면 노드 수가 폭발 |

### 1.2 BT는 그대로 둔다

본 설계는 BT를 **대체하지 않는다.** Plan은 BT가 실행하는 **확장 명령**이다. Plan이 없으면 BT는 평소대로 Intent 평가 → 실행. Plan이 있으면 BT는 Plan을 우선 따르고, Plan이 끝나거나 중단되면 일반 모드로 복귀.

BT는 다음 책임만 유지한다.

1. 전환 불가/타겟 없음/하드 인터럽트 처리
2. Plan 활성 여부 확인 및 현재 Step 실행
3. Plan이 없을 때 일반 Intent 평가 서비스 실행
4. 선택된 Intent를 `EnemyActionResolver`로 라우팅

보스의 장기 전술 흐름은 BT 노드가 아니라 `BossPlanSO`에 둔다.

---

## 2. 설계 목표

1. **보스에만 적용** — 잡몹은 영향 없음
2. **BT 위의 레이어** — BT 노드 형태로 Plan을 조회·실행
3. **인스펙터 친화** — Plan은 SO로 정의, 디자이너가 단계를 편집
4. **중단·재계획 지원** — 피격, HP 변동, 그룹 상태 변화 시 Plan 중단/재선택
5. **부분 재계획** — Plan 내 특정 단계만 스킵 또는 재실행 가능

---

## 3. 데이터 구조

### 3.1 신규 SO: `BossPlanSO`

```csharp
[CreateAssetMenu(fileName = "BossPlan", menuName = "UPlayGround/Enemy/Boss Plan")]
public class BossPlanSO : ScriptableObject
{
    [Tooltip("이 Plan을 식별하는 이름. 디버그용")]
    public string planName;

    [Header("선택 조건")]
    [Tooltip("이 Plan이 후보로 평가되기 위한 조건")]
    public List<PlanTriggerCondition> triggers = new();

    [Range(0f, 1f)]
    public float baseSelectionWeight = 0.5f;

    [Header("단계")]
    public List<PlanStep> steps = new();

    [Header("중단 조건")]
    [Tooltip("실행 중 이 조건이 참이 되면 Plan 즉시 중단")]
    public List<PlanAbortCondition> abortConditions = new();

    [Header("쿨다운")]
    public float cooldownSeconds = 10f;
}
```

### 3.2 단계: `PlanStep`

```csharp
[Serializable]
public class PlanStep
{
    [Tooltip("이 단계에서 BT에 요청할 Intent")]
    public EnemyActionIntent intent;

    [Tooltip("Intent에 동반되는 Style (Idle, Patrol, Circle, Guard, Charge, Flank, Dodge, JumpBack)")]
    public EnemyActionStyle style = EnemyActionStyle.Default;

    [Tooltip("이 단계의 최대 지속 시간. 초과 시 다음 단계로 강제 진행")]
    public float maxDuration = 3f;

    [Tooltip("이 단계의 종료 조건. 충족 시 즉시 다음 단계로")]
    public List<PlanStepExitCondition> exitConditions = new();

    [Tooltip("이 단계 실패(Intent 실행 거절) 시 동작")]
    public PlanStepFailurePolicy onFailure = PlanStepFailurePolicy.AbortPlan;

    [Tooltip("이 단계가 시작되기 위한 최소 스태미나 비율. 스태미나 모델 미사용 시 무시")]
    [Range(0f, 1f)] public float minStaminaNormalized = 0f;

    [Tooltip("이 단계 중 Intent 유지 시간을 강제할지 여부")]
    public bool lockIntentDuringStep = true;
}

public enum PlanStepFailurePolicy
{
    AbortPlan,        // 전체 Plan 중단
    Skip,             // 이 단계만 건너뛰고 다음으로
    Retry,            // 동일 단계 1회 재시도
}
```

### 3.3 조건 식별자

조건은 `MONSTER_INTENT_WEIGHTS_EXTERNALIZATION_DESIGN.md`의 `IntentConditionId`와 동일한 형식으로 재사용:

```csharp
[Serializable]
public class PlanTriggerCondition
{
    public List<IntentConditionId> conditions;        // AND
    [Range(0f, 1f)] public float weightBonus;         // 충족 시 가산 가중치
}

[Serializable]
public class PlanAbortCondition
{
    public List<IntentConditionId> conditions;        // AND
}

[Serializable]
public class PlanStepExitCondition
{
    public List<IntentConditionId> conditions;        // AND
}
```

---

## 4. 신규 컴포넌트: `BossPlanRunner`

보스 GameObject에 부착. 매 BT 틱마다 BT보다 먼저 실행되어 현재 Plan 상태를 갱신한다.

```csharp
public class BossPlanRunner : MonoBehaviour
{
    [SerializeField] private List<BossPlanSO> _availablePlans;
    [SerializeField] private float _planCooldownAfterAbort = 2f;

    private BossPlanSO _currentPlan;
    private int _currentStepIndex;
    private float _currentStepStartTime;
    private float _lastPlanEndedTime;

    public bool HasActivePlan => _currentPlan != null;
    public PlanStep CurrentStep => HasActivePlan ? _currentPlan.steps[_currentStepIndex] : null;

    public void TickPlan(in IntentEvaluationContext ctx);
    public bool TrySelectNewPlan(in IntentEvaluationContext ctx);
    public void AbortCurrentPlan(string reason);
}
```

### 4.1 Plan 선택 알고리즘

`TrySelectNewPlan` 호출 시:
1. 모든 후보 SO 순회
2. `triggers` 모두 평가, 각 조건 충족 시 `weightBonus` 합산
3. 쿨다운 미경과 또는 가중치 0인 후보 제외
4. WeightedRandom으로 1개 선택

### 4.2 단계 진행 알고리즘

`TickPlan` 호출 시:
1. 현재 단계의 `exitConditions` 평가 → 충족 시 다음 단계로
2. `Time.time - _currentStepStartTime > step.maxDuration` 시 다음 단계로
3. Plan 전체의 `abortConditions` 평가 → 충족 시 `AbortCurrentPlan`
4. 마지막 단계 완료 시 `_currentPlan = null`, 쿨다운 기록

---

## 5. BT 통합

### 5.1 신규 노드: `ExecuteBossPlanStepNode` (Action)

```csharp
public class ExecuteBossPlanStepNode : BTActionNode
{
    protected override BTStatus OnUpdate()
    {
        var runner = Context.GetComponentCached<BossPlanRunner>();
        if (runner == null || !runner.HasActivePlan)
            return BTStatus.Failure;

        var step = runner.CurrentStep;
        var request = new EnemyActionRequest(step.intent, step.style);

        if (EnemyActionResolver.TryTransition(Context, request, skipIfAlreadyInState: true, out _))
            return BTStatus.Running;

        // 실행 거절 시 정책에 따라
        runner.HandleStepFailure();
        return BTStatus.Failure;
    }
}
```

### 5.2 신규 노드: `HasActiveBossPlanNode` (Condition)

```csharp
public class HasActiveBossPlanNode : BTConditionNode
{
    protected override BTStatus OnUpdate()
    {
        var runner = Context.GetComponentCached<BossPlanRunner>();
        return runner != null && runner.HasActivePlan ? BTStatus.Success : BTStatus.Failure;
    }
}
```

### 5.3 신규 노드: `TrySelectBossPlanNode` (Action)

`TrySelectNewPlan` 호출, 성공 시 Success.

### 5.4 보스 BT 구조 예시

```
Root (Selector)
├── Sequence: "Plan 모드"
│   ├── HasActiveBossPlan
│   └── ExecuteBossPlanStep
├── Sequence: "Plan 선택 시도"
│   ├── NotHasActiveBossPlan
│   ├── ShouldConsiderNewPlan (예: 적정 거리, 일정 시간 경과)
│   └── TrySelectBossPlan
└── [기존 일반 Intent 평가 서브트리]
```

> Plan이 없으면 그대로 일반 BT 흐름. Plan 진행 중에는 다른 가지로 빠지지 않음.

---

## 6. Plan 예시: "유인 후 반격"

```yaml
planName: "BaitAndCounter"
baseSelectionWeight: 0.4
triggers:
  - conditions: [IsPlayerAttackingFrequently, InAttackRange]
    weightBonus: 0.3
  - conditions: [HasGuardMotion]
    weightBonus: 0.1
steps:
  - intent: Defend
    style: Guard
    maxDuration: 1.2
    exitConditions:
      - conditions: [WasHitRecently]   # 가드 성공 시 즉시 다음 단계
  - intent: Counter
    style: Default
    maxDuration: 2.0
    exitConditions:
      - conditions: [ActionDelayElapsed]
  - intent: Pressure
    style: Charge
    maxDuration: 1.5
abortConditions:
  - conditions: [IsPoiseBroken]
  - conditions: [IsLowHealth]
cooldownSeconds: 15
```

### 6.1 Phase별 Plan 후보 예시

| Phase | Plan 후보 | 목적 |
|-------|-----------|------|
| Phase 1 | `ProbeAndPunish`, `BaitAndCounter` | 플레이어 습관 확인, 회피/가드 반응 유도 |
| Phase 2 | `ForceDodgeThenCatch`, `GuardBreakPressure` | 반복 회피/가드 패턴 응징 |
| Phase 3 | `BurstWindow`, `RetreatAndDive`, `LastStandCounter` | 짧은 폭발 구간과 큰 리스크 행동 |

Phase 전환 시 `_availablePlans`를 통째로 교체하거나, Plan Trigger에 `IsEnemyPhase` 조건을 둔다. 단순 튜닝은 페이즈별 `baseSelectionWeight` 오버라이드로 처리한다.

---

## 7. 호환성

- 잡몹·일반 적은 `BossPlanRunner`를 부착하지 않으면 영향 없음.
- 보스가 `BossPlanRunner`를 부착해도 `_availablePlans`가 비어 있으면 BT는 평소대로 작동.
- 기존 BT 에셋은 보스 BT만 새 노드 사용. 일반 적 BT는 변경 없음.
- 신규 Intent 추가 없음. 기존 9개(또는 10개 with `Bait`) 그대로 사용.
- 스태미나 모델이 도입된 보스는 Step 시작 전에 코스트/최소 스태미나를 검사한다. 실패하면 `PlanStepFailurePolicy`를 따른다.

---

## 8. 검증 / 테스트 시나리오

| ID | 시나리오 |
|----|---------|
| 8.1 | 보스가 "BaitAndCounter" Plan을 선택. 3단계가 순서대로 실행됨을 BT Debug Trace로 확인 |
| 8.2 | Plan 진행 중 보스가 피격되어 PoiseBroken 발생 → Plan 즉시 중단, 일반 Hit 흐름으로 전환 |
| 8.3 | Plan 1단계 Guard 중 플레이어가 공격 안 함 → `maxDuration` 초과 후 자동 2단계 진입 |
| 8.4 | Plan 종료 후 `cooldownSeconds` 동안 동일 Plan 재선택 안 됨 |
| 8.5 | 보스 HP 페이즈 전환 시 `_availablePlans` 후보가 갱신되도록 페이즈별 SO 분리 가능 |

---

## 9. 신규/변경 클래스 요약

| 위치 | 변경 종류 | 비고 |
|------|----------|------|
| `Assets/02.Scripts/AI/BossPlan/BossPlanSO.cs` | 신규 | Plan 정의 |
| `Assets/02.Scripts/AI/BossPlan/PlanStep.cs` | 신규 | 단계 정의 (Serializable) |
| `Assets/02.Scripts/AI/BossPlan/BossPlanRunner.cs` | 신규 | 런타임 컴포넌트 |
| `Assets/02.Scripts/AI/BehaviorTree/Nodes/Action/ExecuteBossPlanStepNode.cs` | 신규 | BT 노드 |
| `Assets/02.Scripts/AI/BehaviorTree/Nodes/Action/TrySelectBossPlanNode.cs` | 신규 | BT 노드 |
| `Assets/02.Scripts/AI/BehaviorTree/Nodes/Condition/HasActiveBossPlanNode.cs` | 신규 | BT 노드 |
| `Assets/10.Datas/AI/BossPlans/*.asset` | 신규 | Plan 자산 (보스별) |

---

## 10. 작업 순서

1. **Phase A (1일)** — `BossPlanSO`, `PlanStep` 등 데이터 구조 신설. SO 인스펙터 확인
2. **Phase B (1.5일)** — `BossPlanRunner` 신규 컴포넌트, Plan 선택/단계 진행 알고리즘
3. **Phase C (1일)** — BT 노드 3종 신설, 보스 BT 에셋에 Plan 가지 추가
4. **Phase D (1일)** — 첫 보스 Plan SO 2~3개 작성, BT Debug Trace에서 Plan 진행 추적
5. **Phase E (튜닝)** — 신규 보스 추가 시 SO만 작성하는 워크플로우 정착

총 4~5일.

---

## 11. 명시적 비목표

- **잡몹에 적용 안 함.** 본 설계는 보스 한정.
- **GOAP 정식 도입 안 함.** Plan은 사전 정의된 순서 리스트일 뿐, 동적 행동 계획 알고리즘은 사용하지 않는다.
- **HTN(Hierarchical Task Network) 정식 도입 안 함.** Plan 내 단계가 또 다른 Plan을 호출하는 재귀 구조는 본 설계 범위 밖.
- **Plan 동시 실행 안 함.** 1개의 Plan만 활성. 우선순위 큐 없음.
- **Plan 학습 안 함.** 디자이너가 SO로 정의. 런타임 학습은 별도 시스템 필요.

---

## 12. 참고

- 관련 설계 문서:
  - `MONSTER_INTENT_WEIGHTS_EXTERNALIZATION_DESIGN.md` (조건 식별자 재사용)
  - `PLAYER_BEHAVIOR_PREDICTOR_DESIGN.md` (Plan 트리거에 예측 신뢰도 사용 가능)
- 관련 코드: `EnemyActionResolver.cs`, `EnemyActionRequest.cs`, `BehaviorTreeRunner.cs`
