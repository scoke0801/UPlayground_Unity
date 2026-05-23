# 플레이어 행동 예측 시스템 설계 문서

> 작성일: 2026-05-23
> 대상 버전: Unity 6 (6000.0.60f1), URP
> 관련 시스템: `EnemyTacticalMemory`, `EnemyCombatDecisionEvaluator`
> 구현 상태: Phase A~D 완료, Phase E~F 미진행

---

## 0. 요약

`EnemyTacticalMemory`의 플레이어 관찰 기능을 **빈도 카운팅 → 시퀀스 패턴 인식**으로 확장한다. "최근 5초 동안 회피 5회"라는 정보만 알던 시스템이 "회피 직후 공격하는 패턴이 자주 나타난다"는 시퀀스적 정보까지 활용하도록 만든다.

ROI 중상. 보스/엘리트의 "지능적" 인상에 직접 기여. 단, 본 문서의 1차 목표는 **기존 Intent 점수에 예측 입력을 제공하는 것**이며, `Bait`(유인/페인트) Intent는 후속 확장으로 분리한다.

### 0.1 구현 완료 범위

2026-05-23 기준으로 모션 자산 없이 진행 가능한 Phase A~D를 구현 완료했다.

| Phase | 상태 | 구현 내용 |
|------|------|----------|
| Phase A | 완료 | `PlayerActionToken`, `PlayerActionTokenMapper`, `PlayerBehaviorPredictor` 추가. Ring Buffer 기반 최근 행동 기록과 Bigram 전이 카운트 기반 예측 구현 |
| Phase B | 완료 | `ActorMovementController.OnStateChanged` 이벤트 추가. `PlayerActor`가 `PlayerBehaviorPredictor`를 자동 보장하고 피격/리스폰 이벤트를 연결 |
| Phase C | 완료 | `EnemyBlackboardKeys` 예측 키 추가. `SyncEnemyBlackboardService`가 현재 타겟의 예측 결과를 Blackboard에 동기화 |
| Phase D | 완료 | `EnemyCombatDecisionEvaluator`가 예측 결과를 기존 `Punish`, `Counter`, `Pressure`, `Chase`, `Defend` 점수 보정에 반영. `IntentConditionId`, `ContinuousValueId`, `IntentEvaluationContext`에 예측 조건/연속값 입력 추가 |
| Phase E | 미진행 | `Bait` 전용 Intent/State는 페인트 모션과 캔슬 타이밍 자산이 없어 진행하지 않는다 |
| Phase F | 미진행 | `bait` 전용 가중치 엔트리는 Phase E 미진행에 따라 추가하지 않는다 |

현재 구현은 전용 `Bait` 없이도 플레이어 행동 예측을 기존 Intent 점수에 반영한다. `Bait`는 연출 신호가 필요한 기능이므로, 페인트 전용 모션 또는 공격 시작부 캔슬 가능한 모션 자산이 준비될 때까지 진행하지 않는다.

---

## 1. 현재 구현 상태

### 1.1 `EnemyTacticalMemory`가 추적하는 플레이어 행동

`Assets/02.Scripts/GameActor/Component/Enemy/EnemyTacticalMemory.cs`:

| 항목 | 윈도우 | 출력 |
|------|--------|------|
| `_playerDodgeCount` | 5초 | `IsPlayerDodgingFrequently()` (≥3회) |
| `_playerGuardCount` | 6초 | `IsPlayerGuardingFrequently()` (≥2회) |
| `_playerAttackCount` | 5초 | `IsPlayerAttackingFrequently()` |
| `_playerRecoverCount` | 7초 | `IsPlayerRecoveringFrequently()` |
| State 폴링 | 매 프레임 | `IsPlayerAttacking/Guarding/Staggered/Idle/Recovering` |

플레이어 State 변경 감지(`UpdatePlayerStateRead`)는 이미 작동하므로 시퀀스 데이터 수집 기반은 갖춰져 있다.

### 1.2 한계

- **순서 정보 없음.** 회피→공격 vs 회피→회피 vs 공격→회피를 구분 못 한다.
- **타이밍 정보 없음.** 회피 후 0.3초 이내 공격인지, 1.5초 후인지 구분 못 한다.
- **예측 불가.** "다음에 무엇을 할 가능성이 높은가"라는 질문에 답할 수 없다.
- **단일 적 단위.** 멤버 A가 학습한 정보를 멤버 B가 활용할 수 없다 (그룹 메모리는 `MONSTER_GROUP_AI_ADVANCEMENT_DESIGN.md` Phase 4가 다룸).

---

## 2. 설계 목표

1. **N개의 최근 행동을 순서대로 보관** — Ring Buffer 기반
2. **간단한 마코프 테이블 추정** — `P(다음 = X | 직전 = Y)`
3. **신뢰도 메트릭** — 예측 정확도 추적, 낮은 신뢰도에서는 사용 안 함
4. **Blackboard 노출** — 예측 결과를 BT가 읽을 수 있는 키로 노출
5. **신규 Intent `Bait` 기반** — 예측 신뢰도가 높을 때만 발동되는 페인트 Intent 추가. 단, 현재 구현에서는 진행하지 않는다.

---

## 3. 데이터 구조 설계

### 3.1 행동 토큰 enum

```csharp
public enum PlayerActionToken
{
    None = 0,
    Attack,
    HeavyAttack,
    Dodge,
    Guard,
    GuardBreak,
    Hit,             // 피격
    Recover,         // 일정 시간 무행동
    DashApproach,    // 대시 접근
    DashRetreat,     // 대시 후퇴
}
```

State 이름 → 토큰 매핑은 `EnemyTacticalMemory.GetPlayerStateName()` 분기와 일치하도록 별도 정적 매퍼:
```csharp
public static class PlayerActionTokenMapper
{
    public static PlayerActionToken FromStateName(string stateName);
}
```

### 3.2 행동 이력 엔트리

```csharp
public readonly struct PlayerActionRecord
{
    public readonly PlayerActionToken Token;
    public readonly float StartTime;
    public readonly float TimeSincePreviousAction; // 이전 토큰 시작 후 이 토큰 시작까지의 경과 시간
}
```

### 3.3 신규 컴포넌트: `PlayerBehaviorPredictor`

플레이어 GameObject에 1개 부착. 모든 적이 공유.

```csharp
public class PlayerBehaviorPredictor : MonoBehaviour
{
    [Header("기록")]
    [Tooltip("최근 보관할 행동 개수")]
    [SerializeField] private int _historyCapacity = 16;

    [Header("예측 신뢰도")]
    [Tooltip("이 회수 이상 관찰된 전이만 예측에 사용")]
    [SerializeField] private int _minTransitionsForConfidence = 4;
    [SerializeField] private float _confidenceDecayPerSecond = 0.01f;

    private readonly RingBuffer<PlayerActionRecord> _history;
    private readonly Dictionary<(PlayerActionToken, PlayerActionToken), int> _bigramCounts;
    private float _overallConfidence;

    public void NotifyAction(PlayerActionToken token);

    public PlayerActionToken PredictNext(out float confidence);
    public PlayerActionToken PredictNextAfter(PlayerActionToken token, out float confidence);
    public float ProbabilityOf(PlayerActionToken from, PlayerActionToken to);
    public float OverallConfidence => _overallConfidence;
}
```

### 3.4 관찰 경로

| 출처 | 호출 시점 |
|------|----------|
| `PlayerActor.OnStateChanged` 훅 (신규) | State 진입 시 `NotifyAction(token)` |
| `IDamageable.TakeDamage` 진입 시 | `NotifyAction(Hit)` |
| `PlayerCombat.OnAttackLanded` | 이미 존재 시 활용. 없으면 신규 hook |

`EnemyTacticalMemory.UpdatePlayerStateRead`는 그대로 두되, **신규 `PlayerBehaviorPredictor`가 메인 데이터 소스**가 된다.

### 3.5 BT Blackboard 노출

`EnemyBlackboardKeys`에 추가:
- `PredictedNextPlayerAction` (string)
- `PredictionConfidence` (float, 0~1)
- `PlayerActionLastToken` (string)
- `PlayerActionTimeSinceLast` (float)

`SyncEnemyBlackboardService`가 매 틱 적의 `Detection.CurrentTarget`에서 `PlayerBehaviorPredictor`를 찾아 위 키 동기화.

### 3.6 기존 Intent에 우선 반영

`Bait`를 추가하기 전에도 예측 결과는 기존 Intent에 바로 사용할 수 있다.

| 예측 | 기존 Intent 보정 |
|------|------------------|
| 다음 행동이 `Dodge`일 확률 높음 | `Punish` +, `Pressure` +, 느린 `Attack` - |
| 다음 행동이 `Guard`일 확률 높음 | `Pressure` +, `Flank`/`Charge` Style 후보 + |
| 다음 행동이 `Attack`일 확률 높음 | `Counter` +, `Defend` +, `Retreat` 조건부 + |
| 다음 행동이 `Recover`일 확률 높음 | `Punish` +, `Chase` + |

따라서 Phase A~C만 완료해도 `EnemyCombatDecisionEvaluator`와 `EnemyIntentWeightsSO` 조건으로 체감 개선이 가능하다.

---

## 4. 마코프 테이블 알고리즘

### 4.1 학습

`NotifyAction(token)` 호출 시:
1. 직전 토큰을 history에서 조회
2. `_bigramCounts[(prev, current)]++`
3. history에 push, 용량 초과 시 가장 오래된 엔트리 pop
4. 1초마다 `_overallConfidence -= _confidenceDecayPerSecond`로 자연 감쇠

### 4.2 예측

```csharp
public PlayerActionToken PredictNext(out float confidence)
{
    var last = _history.Count > 0 ? _history.Last.Token : PlayerActionToken.None;

    int total = 0;
    foreach (var kv in _bigramCounts)
        if (kv.Key.Item1 == last) total += kv.Value;

    if (total < _minTransitionsForConfidence)
    {
        confidence = 0f;
        return PlayerActionToken.None;
    }

    PlayerActionToken best = PlayerActionToken.None;
    int bestCount = 0;
    foreach (var kv in _bigramCounts)
    {
        if (kv.Key.Item1 == last && kv.Value > bestCount)
        {
            best = kv.Key.Item2;
            bestCount = kv.Value;
        }
    }

    confidence = (float)bestCount / total;
    return best;
}
```

### 4.3 정확도 기반 신뢰도 보정

`PredictNext` 호출 후 다음 실제 행동이 일치하면 `_overallConfidence += 0.05f`, 불일치 시 `-= 0.03f`. 0~1 클램프.

이 값이 일정 임계(예: 0.4) 미만이면 `Bait` Intent 활성화 안 함.

---

## 5. 신규 Intent: `Bait`

> 현재 결정: Phase E~F는 진행하지 않는다. `Bait`는 페인트 모션, 공격 시작부 캔슬 타이밍, 플레이어가 읽을 수 있는 명확한 연출 신호가 필요하다. 해당 모션 자산 없이 상태만 추가하면 적이 부자연스럽게 멈칫하거나 기존 공격 의도와 구분되지 않을 가능성이 높으므로 현재 완료 범위에서 제외한다.

### 5.1 추가

미진행. `EnemyActionIntent` 및 `CombatIntent` enum에 `Bait`를 추가하지 않는다.

### 5.2 의미

페인트 모션을 보여 플레이어 회피 또는 가드를 유도한 뒤, 진짜 공격을 캔슬한다. 플레이어가 회피한 직후 ~0.5초의 회복 윈도우에 Punish를 꽂는 흐름.

### 5.3 점수 가중치

`EnemyIntentWeightsSO`(별도 문서)의 `bait` 엔트리:

| 조건 | 보너스 |
|------|--------|
| `IsPlayerDodgingFrequently` | +0.30 |
| `PredictionConfidence >= 0.6` | +0.25 |
| `PredictedNextAction == Dodge` 또는 `Guard` | +0.20 |
| `InAttackRange` 그리고 `ActionDelayElapsed` | +0.15 |
| `IsPlayerAttacking` (이미 들어오는 중) | ×0.0 (적용 안 함) |

### 5.4 실행 경로

`EnemyActionResolver.FromIntent(Bait)` → 신규 `EnemyBaitState`:
- 1단계: 페인트 모션 (공격 시작 모션의 약 30% 지점에서 캔슬)
- 2단계: 플레이어가 회피 또는 가드 시 → Punish Intent 재요청
- 2단계: 플레이어가 반응하지 않으면 → 일반 Attack 진입

신규 State 추가는 본 설계의 범위에 포함되나, 모션 자산 의존도가 있으므로 **Phase 분리** 필요.

### 5.5 대안: Bait 없이 Plan/Style로 표현

초기에는 `Bait` Intent를 바로 추가하지 않고 다음 조합으로 같은 의도를 일부 표현할 수 있다.

| 목적 | 표현 |
|------|------|
| 회피 유도 | `Pressure + Charge` 또는 보스 Plan의 짧은 전진 단계 |
| 가드 유도 | `Pressure + Circle` 후 `Punish` |
| 반응 확인 | `Defend + Guard` 짧은 유지 후 `Counter` |

`Bait`는 전용 페인트 모션과 캔슬 타이밍이 준비된 뒤 추가한다. 그 전까지는 예측 결과를 `Punish`, `Counter`, `Pressure` 점수 보정에 사용한다.

---

## 6. 그룹 공유 메모리와의 관계

`MONSTER_GROUP_AI_ADVANCEMENT_DESIGN.md` Phase 4의 `MonsterGroupMemory`가 도입되면, `PlayerBehaviorPredictor`는 **플레이어 측 단일 인스턴스**로 두고, 그룹 메모리는 이를 참조한다. 두 시스템은 충돌하지 않는다.

- `PlayerBehaviorPredictor` — 사실 데이터 (시퀀스, 마코프 테이블). 플레이어 1명당 1개.
- `MonsterGroupMemory` — 그룹별 해석된 메트릭(`PlayerDodgeCountInWindow` 등). 적이 직접 읽음.
- `EnemyTacticalMemory` — 멤버 자기 자신의 행동 기록만.

---

## 7. 검증 / 테스트 시나리오

| ID | 시나리오 |
|----|---------|
| 7.1 | 플레이어가 "회피→공격" 패턴을 10회 반복. 11회째 회피 직후 `PredictNext == Attack`, `confidence >= 0.7` |
| 7.2 | 플레이어가 무작위 패턴 사용 시 `OverallConfidence`가 0.4 이하로 떨어지고 `Bait` Intent 발동 안 함 |
| 7.3 | 미진행 항목. `Bait`는 전용 모션 자산 준비 후 별도 검증 |
| 7.4 | 플레이어 사망/리스폰 시 history 초기화, 신뢰도 0으로 리셋 |

---

## 8. 신규/변경 클래스 요약

| 위치 | 변경 종류 | 비고 |
|------|----------|------|
| `Assets/02.Scripts/GameActor/Component/Player/PlayerBehaviorPredictor.cs` | 신규 | 본 설계의 중심 |
| `Assets/02.Scripts/AI/CombatDecision/PlayerActionToken.cs` | 신규 | enum + Mapper |
| `Assets/02.Scripts/AI/BehaviorTree/Runtime/EnemyBlackboardKeys.cs` | 키 추가 | 4개 |
| `Assets/02.Scripts/AI/BehaviorTree/Nodes/Service/SyncEnemyBlackboardService.cs` | 확장 | 예측 결과 동기화 |
| `Assets/02.Scripts/AI/CombatDecision/IntentEvaluationContext.cs` | 확장 | 예측 토큰/신뢰도 입력 |
| `Assets/02.Scripts/AI/CombatDecision/IntentConditionId.cs` | 확장 | 예측 조건 식별자 |
| `Assets/02.Scripts/AI/CombatDecision/ContinuousValueId.cs` | 확장 | 예측 신뢰도 연속값 |
| `Assets/02.Scripts/AI/CombatDecision/IntentConditionEvaluator.cs` | 확장 | 예측 조건/연속값 평가 |
| `Assets/02.Scripts/GameActor/Component/Enemy/EnemyCombatDecisionEvaluator.cs` | 확장 | 기존 Intent 점수 예측 보정 |
| `Assets/02.Scripts/GameActor/MovementController/ActorMovementController.cs` | 확장 | `OnStateChanged` 이벤트 |
| `Assets/02.Scripts/GameActor/Object/Player/PlayerActor.cs` | 확장 | 예측 컴포넌트 자동 보장, 피격/리스폰 연결 |
| `Assets/02.Scripts/AI/BehaviorTree/Runtime/EnemyActionIntent.cs` | 미진행 | `Bait` 미추가 |
| `Assets/02.Scripts/AI/CombatDecision/CombatIntent.cs` | 미진행 | `Bait` 미추가 |
| `Assets/02.Scripts/State/Enemy/EnemyBaitState.cs` | 미진행 | 페인트 모션 자산 필요 |
| `Assets/02.Scripts/AI/BehaviorTree/Runtime/EnemyActionResolver.cs` | 미진행 | `Bait` 분기 미추가 |

---

## 9. 작업 순서

1. **Phase A (1일)** — `PlayerActionToken` enum + Mapper, `PlayerBehaviorPredictor` 신규 컴포넌트, Ring Buffer + Bigram 학습
2. **Phase B (반일)** — `PlayerActor`에 `OnStateChanged` 이벤트 추가 또는 폴링 기반 관찰 연결
3. **Phase C (반일)** — `EnemyBlackboardKeys` 확장 + `SyncEnemyBlackboardService` 동기화
4. **Phase D (반일)** — 기존 `Punish`/`Counter`/`Pressure` Intent Weight에 예측 조건 연결
5. **Phase E (미진행)** — `Bait` enum 추가 + `EnemyBaitState` (모션 자산 의존), `EnemyActionResolver` 분기
6. **Phase F (미진행)** — `EnemyIntentWeightsSO`(선행 문서)에 `bait` 엔트리 추가, 기본값 튜닝

Phase A~D만 완료 범위로 확정한다. Phase E~F는 전용 페인트 모션 또는 캔슬 가능한 공격 모션 자산이 준비되기 전까지 진행하지 않는다.

---

## 10. 명시적 비목표

- **N-gram > 2 (3-gram 이상) 사용하지 않는다.** Bigram(`P(다음 | 직전)`)만 사용. 더 긴 시퀀스는 데이터 부족과 메모리 폭증으로 ROI 낮다.
- **학습 데이터 영속화하지 않는다.** 플레이세션마다 처음부터 학습. 영속화는 별도 마일스톤(밸런스 텔레메트리 시스템)에서 다룬다.
- **머신러닝 모델 사용하지 않는다.** 단순 카운팅 테이블.
- **전역 플레이어 모델은 본 설계 범위.** 적별 학습은 의도적으로 배제 — 적이 새로 스폰될 때마다 0부터 학습하면 의미 없음.

---

## 11. 참고

- 관련 코드: `EnemyTacticalMemory.cs`, `PlayerActor.*.cs`, `BehaviorTreeRunner`
- 관련 설계 문서:
  - `MONSTER_INTENT_WEIGHTS_EXTERNALIZATION_DESIGN.md` (`Bait` 가중치 등록)
  - `MONSTER_GROUP_AI_ADVANCEMENT_DESIGN.md` Phase 4 (그룹 공유 메모리)
