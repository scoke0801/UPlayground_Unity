# 몬스터 AI BT 적용 평가 및 작업 계획

> 작성일: 2026-05-20  
> 대상 버전: Unity 6 (6000.0.60f1), URP  
> 기준 문서: `Assets/docs/monster_ai_bt_design_gdd_kr.md`  
> 적용 범위: `Assets/02.Scripts/AI/BehaviorTree/`, `Assets/02.Scripts/GameActor/Component/Enemy/`, `Assets/02.Scripts/Data/Actor/Enemy/`, `Assets/10.Datas/AI/BehaviorTree/`

---

## 개요

이 문서는 `monster_ai_bt_design_gdd_kr.md`의 몬스터 AI 설계를 현재 프로젝트에 그대로 확장할지, 현재 구조를 유지할지 평가하고, 실제 적용 순서를 정리한다.

결론은 **현재 프로젝트 구조를 유지하면서 Intent / Utility 평가 계층만 얇게 추가하는 방향**이다.

현재 프로젝트는 이미 다음 기반을 갖고 있다.

- `BehaviorTreeRunner` 기반 자체 BT 런타임
- `EnemyAIContext`를 통한 BT 노드용 AI Facade
- `EnemyTacticalMemory` 기반 플레이어 행동 관찰
- `EnemyBehaviorSO` / `BehaviorPhase` 기반 페이즈 데이터
- `EnemyCombat`, `EnemyMovementController`, `EnemyActorState` 기반 실행 계층
- `MonsterGroupController` 기반 공격 슬롯 제어

따라서 새 AI 프레임워크를 만들거나 상태 머신을 제거하는 방식은 비용 대비 이득이 낮다. 문서의 방향은 **의사결정 품질 개선**으로 흡수하고, 실행은 기존 상태 / 공격 / MotionSet 구조를 계속 사용한다.

---

## 판단 요약

| 선택지 | 평가 | 결론 |
|---|---|---|
| 현재 구조 유지 | 기존 KCC 상태 머신, Animancer MotionSet, EnemyCombat과 충돌이 적다. 단계적으로 적용 가능하다. | 채택 |
| BT 완전 중심 구조로 전환 | 모든 행동을 BT 노드 중심으로 재작성해야 한다. 상태 전환, 공격 실행, 애니메이션 타이밍 검증 비용이 크다. | 보류 |
| GDD의 Intent / Utility만 추가 | 현재 블랙보드, 전술 메모리, 페이즈 구조와 잘 맞는다. 행동 선택 품질을 개선할 수 있다. | 채택 |
| 추천 추가 액션 선행 구현 | 새 모션, MotionEvent, 상태, 공격 데이터가 필요하다. AI 구조 평가와 별개로 제작 비용이 크다. | 제외 |

---

## 현재 구조 평가

### 유지해야 할 구조

```text
EnemyBehaviorSO / BehaviorPhase
↓
EnemyAIController / EnemyAIContext
↓
BehaviorTreeRunner / Blackboard
↓
BT Node
↓
EnemyMovementController / EnemyActorState
↓
EnemyCombat / MotionSet / MotionEvent
```

이 흐름은 현재 프로젝트에 맞다. 특히 상태별 KCC 물리 처리는 `EnemyActorState`가 담당하고, 공격 판정과 타이밍은 `EnemyCombat` 및 MotionEvent가 담당한다. BT는 이 실행 계층을 대체하지 않고, 어떤 상태와 공격을 선택할지만 결정하는 상위 계층으로 남겨야 한다.

### 보강해야 할 구조

현재 BT는 조건과 가중 선택을 통해 행동을 고르지만, GDD의 핵심인 `Attack`, `Punish`, `Counter`, `Pressure`, `Retreat` 같은 의도를 명시적으로 저장하지 않는다.

그 결과 다음 문제가 생긴다.

- 같은 행동이 반복될 때 Intent 단위로 억제하기 어렵다.
- 페이즈별 전투 목표 변화를 데이터로 표현하기 어렵다.
- 디버그 Trace에서 "왜 이 행동을 골랐는지"가 노드 단위로만 보인다.
- 몬스터 역할별 AI 튜닝이 BT JSON 분기 증가로 이어질 수 있다.

따라서 행동 실행 구조를 바꾸기보다 **Intent 평가 결과를 블랙보드에 기록하는 계층**을 추가한다.

---

## 적용 방향

### 핵심 원칙

```text
Code: 무엇을 할지 결정
BT: 선택된 의도를 어떻게 실행할지 구성
State / Combat: 실제 이동, 공격, 애니메이션 실행
```

### 권장 구조

```text
EnemyCombatDecisionEvaluator
↓
EvaluateEnemyCombatIntentNode
↓
Blackboard
  - SelectedIntent
  - LastIntent
  - IntentScore_Attack
  - IntentScore_Punish
  - IntentScore_Counter
  - IntentScore_Pressure
  - IntentScore_Retreat
↓
BT Execute Selected Intent
↓
기존 Transition / ExecuteAttack 노드
```

`EnemyCombatDecisionEvaluator`는 MonoBehaviour 컴포넌트로 두고, BT 노드에서는 이 컴포넌트를 호출한다. 이렇게 하면 점수 계산은 C#에서 테스트와 디버그가 쉽고, BT는 시각적 실행 흐름을 유지한다.

---

## 추가할 타입

### CombatIntent

권장 위치:

```text
Assets/02.Scripts/AI/CombatDecision/CombatIntent.cs
```

```csharp
namespace UPlayGround.AI.CombatDecision
{
    public enum CombatIntent
    {
        Attack,
        Punish,
        Counter,
        Pressure,
        Chase,
        Retreat,
        KeepDistance,
        Defend,
        Recover
    }
}
```

### EnemyCombatDecisionEvaluator

권장 위치:

```text
Assets/02.Scripts/GameActor/Component/Enemy/EnemyCombatDecisionEvaluator.cs
```

역할:

| 책임 | 설명 |
|---|---|
| 전투 상황 수집 | 거리, 타겟 존재, 플레이어 상태, 최근 피격, Poise, 페이즈 |
| Intent 점수 계산 | Attack / Punish / Counter / Pressure / Retreat 등 |
| 반복 방지 | LastIntent와 ConsecutiveIntentCount 기반 감점 |
| 확률 선택 | 최고점 고정이 아니라 상위 후보 가중 랜덤 |
| 블랙보드 기록 | 선택 의도와 점수를 BT Debug Trace에서 볼 수 있게 기록 |

### EvaluateEnemyCombatIntentNode

권장 위치:

```text
Assets/02.Scripts/AI/BehaviorTree/Nodes/Action/EvaluateEnemyCombatIntentNode.cs
```

역할:

- `EnemyCombatDecisionEvaluator`를 호출한다.
- 결과를 블랙보드에 기록한다.
- 실패 시 기존 BT 분기가 동작하도록 `Failure`를 반환한다.

---

## 블랙보드 키 확장

권장 추가 키:

| 키 | 타입 | 의미 |
|---|---|---|
| `SelectedIntent` | String | 이번 Tick 또는 행동 주기에서 선택된 Intent |
| `LastIntent` | String | 직전에 실행된 Intent |
| `ConsecutiveIntentCount` | Int | 같은 Intent 반복 횟수 |
| `IntentScore_Attack` | Float | Attack 점수 |
| `IntentScore_Punish` | Float | Punish 점수 |
| `IntentScore_Counter` | Float | Counter 점수 |
| `IntentScore_Pressure` | Float | Pressure 점수 |
| `IntentScore_Retreat` | Float | Retreat 점수 |
| `CombatRhythmPhase` | String | Observe / Pressure / CommitAttack / Disengage / ReEnter |

기존 키인 `aggression`, `reactionChance`, `counterChance`, `dodgeChance`, `punishRecoveryChance`, `antiGuardChance`, `preferredRange`, `maxComboPressureCount`는 유지한다. 새 Intent 점수 계산은 이 값을 입력으로 사용한다.

---

## BT 구조 변경안

기존 그룹 구조는 유지하고, `Evaluate Combat Intent`와 `Execute Selected Intent`만 추가한다.

```text
Root
└─ Selector
   ├─ 01 Interrupt And Target Search
   ├─ 02 Emergency Reactions
   ├─ 03 Evaluate Combat Intent
   ├─ 04 Execute Selected Intent
   ├─ 05 Positioning
   └─ 06 Fallback
```

### 03 Evaluate Combat Intent

```text
Sequence
├─ SyncEnemyBlackboard
├─ SyncEnemyMemory
├─ SyncEnemyPhase
└─ EvaluateEnemyCombatIntent
```

### 04 Execute Selected Intent

```text
Selector
├─ Intent == Punish  → ExecuteAttack(Heavy / Skill / Basic)
├─ Intent == Counter → Transition(Counter / Guard / Dodge)
├─ Intent == Attack  → RequestAttackSlot → ExecuteAttack
├─ Intent == Pressure → Transition(Circle / Flank / Chase)
├─ Intent == Retreat → Transition(Retreat / JumpBack)
└─ Intent == Chase   → Transition(Chase)
```

BT는 여전히 기존 `TransitionEnemyStateNode`, `ExecuteEnemyAttackNode`, `RequestEnemyAttackSlotNode`, `CooldownReadyNode`를 재사용한다.

---

## 데이터 확장 계획

### BehaviorPhase 확장

페이즈는 패턴 개수 증가보다 전투 목표 변화를 표현해야 한다. `BehaviorPhase`에 Intent Weight를 추가하면 페이즈별 성격을 데이터로 조정할 수 있다.

권장 필드:

```csharp
[Header("Intent Weight")]
public float attackWeight = 1f;
public float punishWeight = 1f;
public float counterWeight = 1f;
public float pressureWeight = 1f;
public float retreatWeight = 1f;
public float keepDistanceWeight = 1f;
```

예시:

| 페이즈 | 목표 | 권장 가중치 |
|---|---|---|
| Phase 1 | 학습 가능한 기본전 | Attack / Pressure 보통, Counter 낮음 |
| Phase 2 | 플레이어 습관 대응 | Punish / Counter 증가 |
| Phase 3 | 압박 극대화 | Pressure / Attack 증가, Retreat 감소 |

### EnemyBehaviorSO 확장

몬스터별 역할이 늘어날 경우 BT JSON을 계속 복제하지 말고 역할 프로필을 둔다.

권장 enum:

```csharp
public enum EnemyAIRole
{
    Melee,
    RangedSupport,
    RangedMain,
    Healer,
    Summoner
}
```

역할은 Intent 점수 보정에만 사용한다.

| 역할 | 보정 방향 |
|---|---|
| `Melee` | Attack / Counter / Pressure 우선 |
| `RangedSupport` | KeepDistance / Pressure / Retreat 우선 |
| `RangedMain` | KeepDistance / Attack / Retreat 우선 |
| `Healer` | Recover / Defend / Retreat 우선 |
| `Summoner` | Pressure / KeepDistance / Attack 우선 |

---

## 작업 계획

### 1단계: 의사결정 계층 추가

목표:

- 기존 BT와 상태 머신을 건드리지 않고 Intent 점수 계산만 추가한다.
- 디버그 가능한 최소 단위로 시작한다.

작업:

| 순서 | 작업 | 산출물 |
|---:|---|---|
| 1 | `CombatIntent` enum 추가 | `CombatIntent.cs` |
| 2 | 블랙보드 키 상수 추가 | `EnemyBlackboardKeys` |
| 3 | `EnemyCombatDecisionEvaluator` 추가 | 점수 계산 컴포넌트 |
| 4 | `EvaluateEnemyCombatIntentNode` 추가 | BT Action Node |
| 5 | 기준 BT JSON에 평가 노드 삽입 | 테스트용 SourceJson |

검증:

- 타겟 없음 상태에서 기존 Patrol / Idle 동작 유지
- 타겟 접근 시 `SelectedIntent` 기록 확인
- 플레이어 공격 / 회복 / 잦은 회피 상황에서 점수 변화 확인
- 기존 Attack / Retreat / Guard 상태 전환이 깨지지 않는지 확인

### 2단계: BT 실행 분기 정리

목표:

- 선택된 Intent를 실행하는 BT 그룹을 표준화한다.
- 몬스터별 JSON 복제를 줄인다.

작업:

| 순서 | 작업 | 산출물 |
|---:|---|---|
| 1 | `SelectedIntent` 조건 노드 추가 또는 Blackboard String 조건 재사용 | Intent 분기 가능 |
| 2 | `Execute Selected Intent` 그룹 템플릿 작성 | 기준 JSON |
| 3 | 기존 Advanced Ground Melee 샘플에 적용 | 기준 샘플 갱신 |
| 4 | BT 에디터 표시명에 Intent 키 추가 | 에디터 가독성 개선 |

검증:

- 같은 상황에서 기존 Advanced BT와 행동 결과 비교
- 공격 슬롯 요청이 Attack 실행 직전에만 발생하는지 확인
- `CooldownReady`가 Counter / Retreat / Pressure 반복을 제한하는지 확인

### 3단계: 페이즈 / 역할 데이터화

목표:

- 페이즈별 전투 목표 변화를 코드 분기 없이 데이터로 조정한다.
- 근접, 원거리, 회복, 소환 몬스터의 기본 성향을 역할 프로필로 나눈다.

작업:

| 순서 | 작업 | 산출물 |
|---:|---|---|
| 1 | `BehaviorPhase` Intent Weight 추가 | 페이즈별 점수 보정 |
| 2 | `EnemyAIRole` 추가 | 역할 보정 기준 |
| 3 | `EnemyBehaviorSO`에 역할 필드 추가 | 몬스터별 성향 데이터 |
| 4 | Evaluator에 페이즈 / 역할 보정 적용 | 통합 점수 계산 |

검증:

- Phase 1 / 2 / 3 전환 시 선택 Intent 분포가 달라지는지 확인
- 근접형과 원거리형이 같은 BT 구조를 공유하면서 다른 거리 성향을 보이는지 확인
- 기존 BehaviorData 에셋의 기본값으로 기존 동작이 크게 변하지 않는지 확인

### 4단계: Player Read 강화

목표:

- `EnemyTacticalMemory`의 플레이어 관찰 값을 Intent 점수에 안정적으로 반영한다.

작업:

| 순서 | 작업 | 산출물 |
|---:|---|---|
| 1 | 플레이어 최근 행동 카운터 정리 | Dodge / Guard / Attack / Recover |
| 2 | `LastIntent` / `ConsecutiveIntentCount` 기록 | 반복 방지 |
| 3 | 명중률 / 회피 빈도 기반 Punish 보정 | Player Read 반영 |
| 4 | Debug Trace 메시지 개선 | 선택 이유 확인 |

검증:

- 플레이어가 회피를 반복하면 Punish / Pressure 점수가 올라가는지 확인
- 플레이어가 공격을 반복하면 Counter / Defend 점수가 올라가는지 확인
- 같은 Intent 반복 시 점수가 감쇠하는지 확인

---

## 하지 않을 작업

이번 계획에서는 다음 작업을 제외한다.

| 제외 항목 | 이유 |
|---|---|
| 추천 추가 액션 구현 | 새 상태, 모션, MotionEvent, 공격 데이터가 필요하다. AI 구조 적용과 별도 제작 범위다. |
| 상태 머신 제거 | KCC 물리와 상태별 이동 처리가 이미 안정된 실행 계층이다. |
| BT 노드만으로 모든 점수 계산 | 디버그와 유지보수가 어려워지고 JSON 분기가 과도하게 늘어난다. |
| 몬스터별 BT 대량 복제 | 역할 / 페이즈 데이터로 흡수할 수 있는 차이는 데이터로 처리한다. |
| 기존 `EnemyAIController` 즉시 제거 | `EnemyAIContext`, BT 러너 주입, 페이즈, 그룹 슬롯 연결 책임이 남아 있다. |

---

## 리스크

| 리스크 | 내용 | 대응 |
|---|---|---|
| 행동 선택이 너무 자주 바뀜 | Tick마다 Intent가 바뀌면 전투가 불안정해진다. | 행동 시작 후 Lock Time 또는 `NextActionAllowedTime`을 적용한다. |
| 점수 튜닝 난이도 증가 | Intent Weight와 BT 가중치가 동시에 존재하면 원인 분석이 어렵다. | Intent 점수는 큰 방향, BT 가중치는 세부 실행 선택으로 역할을 분리한다. |
| 기존 BT JSON 호환성 | 새 키가 없으면 기본값 처리가 필요하다. | Evaluator에서 기본값을 제공하고 기존 BT는 유지한다. |
| 페이즈 데이터 증가 | `BehaviorPhase` 필드가 많아질 수 있다. | Inspector Header를 명확히 나누고 기본값은 1로 둔다. |
| 디버그 부족 | 선택 이유가 보이지 않으면 튜닝이 어렵다. | 모든 점수와 최종 선택 이유를 블랙보드 / DebugTrace에 기록한다. |

---

## 권장 최종 방향

현재 프로젝트 구조를 유지한다.

단, 현재 구조 유지가 "아무 작업도 하지 않는다"는 뜻은 아니다. AI 품질을 올리려면 다음 순서가 가장 안전하다.

```text
기존 실행 계층 유지
↓
Intent 평가 계층 추가
↓
BT 실행 분기 표준화
↓
페이즈 / 역할 데이터화
↓
Player Read 강화
```

이 방식은 KCC 상태 머신, Animancer MotionSet, EnemyCombat, BehaviorTreeRunner를 모두 살리면서 GDD의 핵심인 읽을 수 있고 대응 가능한 몬스터 AI를 단계적으로 적용할 수 있다.
