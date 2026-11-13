# 적 스태미나 모델 설계 문서

> 작성일: 2026-05-23
> 대상 버전: Unity 6 (6000.0.60f1), URP
> 관련 시스템: `PoiseStat`, `EnemyCombatDecisionEvaluator`, `EnemyTacticalMemory`

---

## 0. 요약

플레이어처럼 적에게도 **스태미나 자원**을 부여한다. 공격/회피/가드 같은 적극적 행동에 코스트를 매기고, 스태미나 부족 시 Recover/Defend Intent로 강제 폴백된다.

목표는 "긴장-이완"이 자연 발생하는 전투 리듬. 적 측 매 액션의 코스트를 디자이너가 SO로 조정할 수 있게 한다.

ROI 중. 기존 `PoiseStat`과 형제 컴포넌트 형태로 추가하면 인프라 비용 작다.

---

## 1. 현재 상태

### 1.1 기존 자원 시스템

| 자원 | 위치 | 역할 |
|------|------|------|
| HP | `MonsterActor` | 사망 판정 |
| Poise | `PoiseStat` | 피격 반응 판정 (강인도) |
| 스태미나 (플레이어) | `PlayerSkillGauge` 등 | 회피/가드/스킬 사용 |

> 적에게 별도 스태미나 자원은 없다. 행동 빈도는 `EnemyAIController._nextActionDelay` 같은 시간 윈도우와 `EnemyCombatDecisionEvaluator`의 점수 페널티로만 제어된다.

### 1.2 문제

- 적이 지속적으로 공격/회피 가능. 자원 소진 개념이 없음.
- "공격 → 잠시 숨 고르기" 같은 자연 리듬이 인위적 cooldown으로만 표현됨.
- 플레이어가 적의 스태미나를 깎아 공격 타이밍을 만든다는 메커닉 부재.

---

## 2. 설계 목표

1. **`EnemyStaminaStat` 신규 컴포넌트** — `PoiseStat`과 동일 형식
2. **공격/회피/가드별 코스트 외부화** — SO로 정의
3. **Intent 점수에 자동 반영** — 스태미나 부족 시 해당 Intent 점수 감쇠
4. **회복 메커니즘** — 비전투 시 회복 ↑, 피격 시 회복 일시 정지
5. **시각화** — 적 머리 위 게이지 (선택), 디버그용 Gizmos

---

## 3. 데이터 구조

### 3.1 신규 컴포넌트: `EnemyStaminaStat`

```csharp
public class EnemyStaminaStat : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private EnemyStaminaConfigSO _config;

    public float CurrentStamina { get; private set; }
    public float MaxStamina => _config?.maxStamina ?? 100f;
    public float NormalizedStamina => MaxStamina > 0 ? CurrentStamina / MaxStamina : 0f;

    public bool IsExhausted => CurrentStamina < (_config?.exhaustionThreshold ?? 10f);
    public bool CanAfford(float cost) => CurrentStamina >= cost;

    public bool TryConsume(float cost);   // 부족 시 false, 충분 시 차감 후 true
    public void Restore(float amount);
    public void OnDamaged();              // 피격 시 회복 일시 정지 트리거

    public event Action OnExhausted;       // 0에 도달
    public event Action OnRecovered;       // exhaustionThreshold 이상으로 회복
}
```

### 3.2 신규 SO: `EnemyStaminaConfigSO`

```csharp
[CreateAssetMenu(fileName = "StaminaConfig", menuName = "UPlayGround/Enemy/Stamina Config")]
public class EnemyStaminaConfigSO : ScriptableObject
{
    [Header("자원 설정")]
    public float maxStamina = 100f;
    public float exhaustionThreshold = 10f;

    [Header("회복")]
    [Tooltip("초당 자연 회복량 (전투 중)")]
    public float regenPerSecondInCombat = 8f;
    [Tooltip("초당 자연 회복량 (비전투)")]
    public float regenPerSecondOutOfCombat = 25f;
    [Tooltip("피격 후 회복이 정지되는 시간")]
    public float regenPauseAfterHit = 1.5f;

    [Header("행동 코스트")]
    public StaminaCost attackCost = new(20f);
    public StaminaCost heavyAttackCost = new(35f);
    public StaminaCost dodgeCost = new(25f);
    public StaminaCost jumpBackCost = new(15f);
    public StaminaCost guardStartCost = new(5f);
    public StaminaCost guardPerSecondCost = new(8f);
    public StaminaCost chargeCost = new(30f);
}

[Serializable]
public class StaminaCost
{
    public float amount;
    public StaminaCost(float amount) => this.amount = amount;
}
```

### 3.3 `EnemyBehaviorSO` 변경

```csharp
[Header("Stamina")]
[Tooltip("스태미나 설정. null이면 스태미나 시스템 미사용 (기존 동작)")]
public EnemyStaminaConfigSO staminaConfig;
```

---

## 4. 코스트 적용 경로

### 4.1 공격

`EnemyAttackState.OnEnter` 또는 `EnemyCombat.ExecuteAttack` 진입 시 `TryConsume(attackCost.amount)` 호출.

부족하면 공격 캔슬 + Intent 재평가 강제 (`EnemyCombatDecisionEvaluator`에서 자동 처리).

### 4.2 회피·점프백

`EnemyDodgeState.OnEnter`, `EnemyJumpBackState.OnEnter`에서 동일 패턴.

### 4.3 가드

`EnemyGuardState`:
- 진입 시 `TryConsume(guardStartCost.amount)`
- 유지 중 매 초 `TryConsume(guardPerSecondCost.amount)`
- 부족 시 GuardBreak 또는 Idle로 전환

### 4.4 차지·플랭크

해당 State 진입 시 코스트 차감. 부족 시 Intent Resolver가 Failure 반환.

---

## 5. Intent 점수에 반영

### 5.1 새 입력값

`IntentEvaluationContext`에 추가:

```csharp
public readonly float StaminaNormalized;       // 0~1
public readonly bool  IsStaminaExhausted;
public readonly bool  CanAffordAttack;
public readonly bool  CanAffordDodge;
public readonly bool  CanAffordGuard;
```

`EnemyCombatDecisionEvaluator`가 매 틱 채워 넘김.

### 5.2 `EnemyIntentWeightsSO`에 자동 반영

`MONSTER_INTENT_WEIGHTS_EXTERNALIZATION_DESIGN.md`의 조건 enum에 다음 추가:

```csharp
IsStaminaExhausted,
CanAffordAttack,
CanAffordDodge,
CanAffordGuard,
StaminaLow,        // < 30%
```

기본 가중치 SO에 다음 보너스 추가:

| Intent | 조건 | 효과 |
|--------|------|------|
| Attack | `!CanAffordAttack` | ×0.0 |
| Dodge (Evade) | `!CanAffordDodge` | ×0.0 |
| Defend | `!CanAffordGuard` | ×0.2 |
| Recover | `IsStaminaExhausted` | +0.40 |
| KeepDistance | `StaminaLow` | +0.18 |

> 본 설계는 스태미나 컴포넌트 + 코스트 차감만 다룬다. Intent 가중치 적용은 `EnemyIntentWeightsSO` 자산 작업으로 처리.

### 5.3 Intent 선택 안정화와의 관계

스태미나가 낮아졌다고 매 틱 `Attack ↔ Recover`가 흔들리면 전투 리듬이 어색해진다. 따라서 다음 원칙을 둔다.

| 상황 | 처리 |
|------|------|
| `IsExhausted == true` | `Recover` Intent 최소 유지 시간 적용. 기본 0.8초 |
| 스태미나가 공격 코스트 직전까지 회복 | 즉시 `Attack`으로 튀지 않고 `KeepDistance` 또는 `Pressure`를 거쳐 재진입 |
| 피격/PoiseBreak | 유지 시간 무시하고 Hit/Retreat/Recover 계열로 즉시 전환 가능 |
| 보스 Plan 실행 중 | Plan Step이 요구하는 코스트를 지불할 수 없으면 Step 실패 정책(`Skip`/`AbortPlan`) 적용 |

`DecisionIntentLockedUntil` 같은 Intent 안정화 키는 `MONSTER_INTENT_WEIGHTS_EXTERNALIZATION_DESIGN.md`의 선택 안정화 규칙을 따른다.

---

## 6. 회복 메커니즘

`EnemyStaminaStat.Update`:

```csharp
float regen = _isInCombat ? _config.regenPerSecondInCombat : _config.regenPerSecondOutOfCombat;
if (Time.time - _lastHitTime < _config.regenPauseAfterHit) regen = 0f;
CurrentStamina = Mathf.Min(MaxStamina, CurrentStamina + regen * Time.deltaTime);
```

`OnDamaged()` 호출 → `_lastHitTime = Time.time`. `MonsterActor.TakeDamage` 또는 `IDamageable.TakeDamage`에서 hook 연결.

`_isInCombat` 판정은 `EnemyDetection.HasTarget`을 그대로 사용.

---

## 7. 시각화

### 7.1 디버그 Gizmos

`EnemyStaminaStat.OnDrawGizmos`:
```
머리 위 작은 막대 — 스태미나 비율
색상: 녹색(>50%) → 노란색(>20%) → 빨간색(≤20%)
```

### 7.2 인게임 UI (선택)

`EnemyHpBarUI`(있다면)와 동일 캔버스에 두 번째 막대. 본 설계의 필수 범위 아님.

---

## 8. 호환성

- `EnemyBehaviorSO.staminaConfig`가 null이면 **`EnemyStaminaStat` 컴포넌트도 부착되지 않거나 비활성** → 현행 동작 유지
- 기존 적은 SO 없이 그대로 작동
- 새 적은 SO를 만들어 끼우면 즉시 스태미나 시스템 적용
- BT 에셋 변경 불필요 (Intent 점수에 자동 반영)

---

## 9. 검증 / 테스트 시나리오

| ID | 시나리오 |
|----|---------|
| 9.1 | 적이 4회 연속 공격 후 스태미나 < 20%. 5회째에 Attack 점수가 0으로 떨어지고 Recover Intent 선택 |
| 9.2 | 비전투 진입 후 3초 내 스태미나 100% 복구 (`regenPerSecondOutOfCombat = 25f` 기준) |
| 9.3 | 가드 유지 중 매 초 스태미나 감소. 0 도달 시 자동 가드 해제 |
| 9.4 | 피격 직후 1.5초 동안 회복 정지. 1.5초 후 자동 재개 |
| 9.5 | `staminaConfig == null` 적은 영향 없이 기존대로 작동 (회귀 테스트) |

---

## 10. 신규/변경 클래스 요약

| 위치 | 변경 종류 | 비고 |
|------|----------|------|
| `Assets/02.Scripts/GameActor/Component/Enemy/EnemyStaminaStat.cs` | 신규 | 본 설계의 중심 |
| `Assets/02.Scripts/Data/Actor/Enemy/EnemyStaminaConfigSO.cs` | 신규 | SO 정의 |
| `Assets/02.Scripts/Data/Actor/Enemy/EnemyBehaviorSO.cs` | 필드 추가 | `staminaConfig` |
| `Assets/02.Scripts/GameActor/Component/Enemy/EnemyCombatDecisionEvaluator.cs` | 입력 확장 | `IntentEvaluationContext`에 스태미나 필드 |
| `Assets/02.Scripts/State/Enemy/EnemyAttackState.cs` | 코스트 차감 | OnEnter |
| `Assets/02.Scripts/State/Enemy/EnemyDodgeState.cs` | 코스트 차감 | OnEnter |
| `Assets/02.Scripts/State/Enemy/EnemyJumpBackState.cs` | 코스트 차감 | OnEnter |
| `Assets/02.Scripts/State/Enemy/EnemyGuardState.cs` | 코스트 차감 | OnEnter + UpdateState |
| `Assets/02.Scripts/State/Enemy/EnemyChargeState.cs` | 코스트 차감 | OnEnter |
| `Assets/02.Scripts/GameActor/MonsterActor.cs` | hook | TakeDamage 시 `EnemyStaminaStat.OnDamaged()` 호출 |
| `Assets/10.Datas/AI/Stamina/*.asset` | 신규 | 기본 SO 2~3개 (잡몹/엘리트/보스) |

---

## 11. 작업 순서

1. **Phase A (1일)** — `EnemyStaminaStat`, `EnemyStaminaConfigSO` 신설. `EnemyBehaviorSO`에 필드 추가
2. **Phase B (1일)** — 각 State에서 `TryConsume` 호출. 부족 시 Intent 재평가 경로 정착
3. **Phase C (반일)** — `IntentEvaluationContext`에 스태미나 입력 추가. `IntentConditionId` 4종 등록
4. **Phase D (반일)** — `EnemyIntentWeightsSO` 기본 자산에 스태미나 조건 보너스 추가 + 회귀 테스트
5. **Phase E (반일)** — Gizmos 시각화 + 디자이너 튜닝

총 3~4일. Phase A~B와 Phase D는 의존성이 있어 순차.

---

## 12. 명시적 비목표

- **플레이어의 스태미나 시스템과 통합하지 않는다.** 적/플레이어는 독립 자원.
- **스태미나 회복 가속 메커닉(보스 페이즈 등)은 본 설계 범위 밖.** SO 값 교체로 표현하면 됨.
- **스태미나 시각화 UI는 본 설계의 필수 범위 아님.** Gizmos까지만 필수.
- **스태미나 기반 그로기 상태는 Poise 시스템과 분리한다.** 본 설계는 행동 자원으로만 사용.
- **스태미나가 BT 구조를 바꾸지 않는다.** BT는 기존처럼 Intent 평가와 실행 라우팅을 담당하고, 스태미나는 점수 입력과 State 진입 코스트로만 작동한다.

---

## 13. 참고

- 관련 코드: `PoiseStat.cs`, `EnemyAttackState.cs`, `EnemyDodgeState.cs`, `EnemyGuardState.cs`
- 관련 설계 문서:
  - `MONSTER_INTENT_WEIGHTS_EXTERNALIZATION_DESIGN.md` (Intent 조건에 스태미나 입력 추가)
  - `MONSTER_GROUP_AI_ADVANCEMENT_DESIGN.md` (그룹 단위 자원 공유는 본 설계와 별개)
