# 몬스터 그룹 AI 고도화 설계 문서

> 작성일: 2026-05-23
> 대상 버전: Unity 6 (6000.0.60f1), URP
> 선행 시스템: `EnemyCombatDecisionEvaluator`(Intent 점수화) + `BehaviorTreeRunner`(BT) + `MonsterGroupController`(그룹 슬롯)

---

## 0. 검토 결론

> "EncounterDirector"라는 신규 개념이 `MonsterGroupController`와 별개로 필요한가?

**필요하지 않다. `MonsterGroupController`를 확장하는 방향이 옳다.**

근거:
- `MonsterGroupController`가 이미 그룹 단위 조율자 역할(슬롯 풀, 우선순위, 경보 전파, 활성화/전멸 이벤트)을 수행한다.
- `EnemyAIController.SetGroup` / `TryRequestAttackSlot` 경로로 BT와 이미 연결되어 있다.
- 동일 책임을 가진 매니저(EncounterDirector)를 새로 추가하면 그룹 단위 책임이 두 클래스로 분산되어 오히려 유지보수 비용 증가.

따라서 본 문서는 **EncounterDirector라는 신규 클래스를 만들지 않고**, `MonsterGroupController`에 누락된 조율 기능을 단계적으로 추가하는 설계를 다룬다.

### 0.1 구현 진행 상태

> 갱신일: 2026-05-23

| Phase | 상태 | 구현 범위 |
|-------|------|-----------|
| Phase 1 — Tempo Throttle | 구현 완료 | `MonsterGroupController._breatherDuration`, `_groupBreatherUntil`, `NotifyMemberAttackEnded`, `RequestAttackSlot` 브리더 차단 |
| Phase 2 — Intent 마스킹 | 구현 완료 | `GroupIntentBias`, `EnemyAIContext.CurrentGroupIntentBias`, `EnemyCombatDecisionEvaluator.ApplyGroupIntentBias` |
| Phase 3 — Aggro 적합도 평가 | 구현 완료 | 슬롯 포화 시 후보 큐에 적재하고 `_aggroDecisionInterval`마다 우선순위/적합도 기반으로 교체 |
| Phase 4 — 그룹 공유 메모리 | 구현 완료 | `MonsterGroupMemory` 추가, 그룹 관찰 카운트/적중률을 Evaluator가 우선 사용 |
| Phase 5 — Formation 슬롯 | 구현 완료 | Formation 슬롯 API, `RequestFormationSlotNode`, `EnemyCircleState`/`EnemyFlankState` 목적지 연동 |

검증:
- `dotnet build UPlayground.sln --no-restore` 통과
- 오류 0개
- 기존 패키지/외부 에셋 경고 23개 유지

---

## 1. 현재 구현 상태

### 1.1 이미 구현된 그룹 기능

| 항목 | 위치 | 비고 |
|------|------|------|
| Attack Slot Pool (Melee/Ranged 분리) | `MonsterGroupController._meleeSlotOwners` / `_rangedSlotOwners` | 인원 제한, 사망자 자동 정리 |
| Priority 기반 슬롯 경쟁 | `MemberPriority { Summon, Normal, Summoner }` | 낮은 우선순위 점유자를 밀어내고 진입 |
| 멤버 등록/해제 | `RegisterMember` / `UnregisterMember` | `Start()`에서 자식 `MonsterActor` 자동 수집 |
| 경보 전파 | `AlertGroup(Transform target)` | 미인식 멤버에게 타겟 주입 |
| 전멸 이벤트 | `OnGroupDefeated` | `GroupStoryTrigger`가 스토리 진행에 사용 |
| 외부 트리거 활성화 | `GroupSpawnTrigger` | 플레이어 진입 시 그룹 활성화 |
| BT 연동 | `RequestEnemyAttackSlotNode` → `EnemyAIController.TryRequestAttackSlot` | 슬롯 획득 여부를 Blackboard `HasAttackSlot`에 기록 |
| Tempo Throttle | `MonsterGroupController.NotifyMemberAttackEnded` | 공격 종료 후 `_breatherDuration` 동안 신규 슬롯 요청 차단 |
| Group Intent Bias | `EnemyCombatDecisionEvaluator.ApplyGroupIntentBias` | 그룹 슬롯/브리더 상태를 Intent 점수에 반영 |
| Group Blackboard Debug | `EnemyCombatDecisionEvaluator.WriteGroupDebugBlackboard` | Group* 배율/보너스/브리더/포메이션/적합도 키 기록 |
| Aggro 후보 큐 | `MonsterGroupController.ProcessAggroCandidates` | 슬롯 포화 시 후보를 큐에 보관하고 주기적으로 최적 후보를 배정 |
| Formation Gizmos | `MonsterGroupController.OnDrawGizmos` | 플레이 중 점유된 Formation 슬롯과 소유자 연결선 표시 |

### 1.2 단일 적 의사결정 인프라 (그룹과 분리되어 있음)

- `EnemyCombatDecisionEvaluator` — Intent 9종 점수화 후 Blackboard에 기록
- `EnemyTacticalMemory` — 자기 자신의 전투 기록 + 플레이어 빈도 관찰
- `EvaluateEnemyCombatIntentService` — BT 매 틱 동기화
- `EnemyActionResolver` — Intent + Style → GameActorState 생성

> Phase 2 구현 이후 그룹 조율 결과는 `GroupIntentBias`를 통해 Intent 점수에도 반영된다. 후속 작업으로 Group* Blackboard 디버그 키도 추가되어 BT 디버거/튜닝 도구에서 현재 그룹 Bias를 확인할 수 있다.

---

## 2. 갭 분석

### 2.1 갭 목록

| # | 갭 | 현재 동작 | 문제 |
|---|----|----|------|
| G1 | **Tempo Throttle 부재** | 동시에 슬롯 한계까지 공격 가능 | 누가 공격 종료 직후에도 다른 적이 즉시 공격 → "쉴 틈 없는 난타" 체감 |
| G2 | **Aggro 적합도 평가 부재** | 슬롯 first-come 점유 | 플레이어 등 뒤에 있는 적이 슬롯을 먼저 잡고 정면 적이 대기하는 부자연스러운 상황 |
| G3 | **Intent 마스킹 부재** | 슬롯 거절 = `Failure` 후 BT 다른 가지 탐색 | 슬롯 못 받은 적이 "공격은 못하지만 어떻게 압박할지"에 대한 가이드 없음 |
| G4 | **Formation 슬롯 부재** | 모두 동일 위치(플레이어 기준 거리만)로 수렴 | 두 적이 같은 각도에서 겹쳐 들어와 시각적 혼란 |
| G5 | **그룹 공유 메모리 부재** | 각 적의 `EnemyTacticalMemory` 독립 | 멤버 A가 회피 패턴을 학습해도 멤버 B는 0부터 다시 학습 |

### 2.2 우선순위

1. **G1 (Tempo Throttle)** — 슬롯 시스템 위에 시간 윈도우만 얹으면 되어 비용 최소, 체감 효과 최대
2. **G3 (Intent 마스킹)** — Intent 시스템과 직결. 슬롯 거절이 단순 Fail이 아니라 "비공격 Intent로 폴백"하도록 가이드
3. **G2 (Aggro 적합도)** — 슬롯 점유 알고리즘 자체를 적합도 기반으로 교체
4. **G5 (그룹 공유 메모리)** — 플레이어 관찰을 그룹 단위 공동 메모리로 승격
5. **G4 (Formation 슬롯)** — 공간 슬롯. 환경 쿼리 없이 "플레이어 기준 N분할 각도"만 사용 (EQS 제외 원칙 준수)

---

## 3. 확장 설계

> 본 설계는 **Phase 단위로 독립 머지 가능**해야 한다. 각 Phase는 이전 Phase 없이도 기존 동작을 망가뜨리지 않는다.

### Phase 1 — Tempo Throttle (Breather Window) [G1]

#### 목표

그룹 멤버 중 누군가의 공격 행동이 종료된 직후 `_breatherDuration` 동안 모든 멤버의 신규 슬롯 요청을 거절한다.

#### 데이터

`MonsterGroupController`에 필드 추가:
```csharp
[Header("Tempo")]
[Tooltip("멤버 공격 종료 후 그룹 전체가 공격 슬롯을 잡지 못하는 시간(초)")]
[SerializeField] private float _breatherDuration = 0.6f;

private float _groupBreatherUntil = -999f;
```

#### API 추가

```csharp
// 공격 행동이 끝난 멤버가 호출. EnemyCombat 또는 EnemyAttackState.OnExit에서 호출.
public void NotifyMemberAttackEnded(MonsterActor member)
{
    _groupBreatherUntil = Time.time + _breatherDuration;
    ReleaseAttackSlot(member);
}

public bool IsInBreatherWindow => Time.time < _groupBreatherUntil;
```

`RequestAttackSlot` 진입 직후:
```csharp
if (IsInBreatherWindow) return false;
```

#### Phase 별 차등화 (미구현)

- `EnemyBehaviorSO.phases[i]`에 `breatherDuration` 오버라이드 필드 추가 (옵션). 페이즈가 진행될수록 짧아져 압박감 ↑.

현재 구현은 `MonsterGroupController._breatherDuration` 단일 값만 사용한다. `EnemyBehaviorSO`/`BehaviorPhase` 오버라이드는 아직 추가하지 않았다.

#### 호환성

기본값 0.6초. 기존 BT 동작은 슬롯 거절 시 자동으로 Circle/KeepDistance 분기로 흐르므로 그대로 작동.

---

### Phase 2 — Intent 마스킹 (그룹 → 단일 적) [G3]

#### 목표

`EnemyCombatDecisionEvaluator`가 Intent 점수를 계산할 때 **그룹 상태**를 추가 입력으로 사용한다. 슬롯을 받지 못한 멤버의 Attack/Punish/Counter 점수를 감쇠시키고 Pressure/KeepDistance 점수를 가산한다.

#### 데이터 흐름

1. BT의 `EvaluateEnemyCombatIntentService`가 매 틱 실행
2. Evaluator가 `EnemyAIContext.Group`을 조회
3. Group이 `GetIntentBias(MonsterActor member)` 반환
4. Bias 구조체를 Intent 점수에 가산/곱연산으로 적용

#### API

```csharp
public readonly struct GroupIntentBias
{
    public readonly float AttackMultiplier;        // 0~1
    public readonly float PunishMultiplier;        // 0~1
    public readonly float CounterMultiplier;       // 0~1
    public readonly float PressureBonus;           // 가산
    public readonly float KeepDistanceBonus;       // 가산
    public readonly float RetreatBonus;            // 가산 (그룹이 위험할 때)
}

public GroupIntentBias GetIntentBias(MonsterActor member, AttackType attackType);
```

#### 결정 규칙 (초기 버전)

| 조건 | 효과 |
|------|------|
| `IsInBreatherWindow == true` | Attack/Punish/Counter ×0.3, KeepDistance +0.15 |
| 멤버가 슬롯 미점유 + 그룹 슬롯 가득 참 | Attack/Punish ×0.4, Pressure +0.20 |
| 그룹 멤버 중 1명만 생존 (혼자 남음) | Retreat +0.15 (도주 빈도 ↑, 페이즈 마지막 발악과 분리 필요) |
| 다른 멤버가 방금 Punish 성공 | 주변 멤버 Pressure +0.10, Attack ×0.7 |
| 플레이어가 특정 멤버만 반복 공격 | 해당 멤버 Retreat/Defend +, 다른 멤버 Pressure/Punish + |

#### Blackboard 키 추가 (구현 완료)

`EnemyBlackboardKeys`에 추가:
- `GroupIntentAttackMultiplier`
- `GroupIntentPunishMultiplier`
- `GroupIntentCounterMultiplier`
- `GroupIntentPressureBonus`
- `GroupIntentKeepDistanceBonus`
- `GroupIntentRetreatBonus`
- `GroupBreatherRemainingTime`
- `GroupFormationSlotIndex`
- `GroupAggroFitness`

현재 구현은 `EnemyAIContext.CurrentGroupIntentBias`를 Evaluator에 직접 전달해 점수에 반영하고, 동일한 값을 `EnemyCombatDecisionEvaluator.WriteGroupDebugBlackboard`에서 Blackboard에 기록한다. 키 원본은 `BehaviorTreeEditorRegistry.json`의 `enemyBlackboardDefaults`이며, `EnemyBlackboardKeys.generated.cs`에 생성 규칙과 동일한 식별자로 반영했다.

#### Intent/Style 책임

그룹은 개별 멤버의 State를 직접 지정하지 않는다. 그룹은 Intent 점수와 Style 후보에 Bias만 제공한다.

예:
- 슬롯이 없으면 `Attack`을 강제로 실패시키는 대신 `Pressure`/`KeepDistance` 점수를 올린다.
- Formation 슬롯을 가진 멤버는 `Pressure`가 선택됐을 때 `Circle` 또는 `Flank` Style 후보가 유리해진다.
- 공격 슬롯을 가진 멤버만 `Attack`/`Punish` Style 후보가 유효해진다.

이 원칙을 지켜야 BT가 그룹 조율 로직으로 비대해지지 않는다.

---

### Phase 3 — Aggro 적합도 평가 [G2]

#### 목표

슬롯 점유 시 first-come이 아니라 **적합도 점수가 높은 멤버에게 우선 부여**한다.

#### 적합도 함수 (예시)

```csharp
float ComputeAggroFitness(MonsterActor member, Vector3 playerPos, Vector3 playerForward)
{
    var toMember = member.transform.position - playerPos;
    var distance = toMember.magnitude;
    var angle = Vector3.Angle(playerForward, toMember.normalized); // 0=정면, 180=등뒤

    float distanceScore = Mathf.Clamp01(1f - Mathf.Abs(distance - member.AIContext.OptimalCombatDistance) / 4f);
    float frontScore    = Mathf.Clamp01(1f - angle / 180f);   // 정면일수록 높음
    float hpScore       = member.GetHealthPercent();           // 죽기 직전 멤버는 슬롯 양보
    return distanceScore * 0.5f + frontScore * 0.3f + hpScore * 0.2f;
}
```

#### 슬롯 부여 변경

원 설계:
1. 빈 슬롯이 있어도 **즉시 부여하지 않고** 후보 큐에 적재
2. 1프레임에 1회 (또는 `_aggroDecisionInterval`마다) 후보 중 적합도 최고 멤버에게 부여
3. 동률 시 우선순위 enum 사용

현재 구현:
1. 빈 슬롯은 기존처럼 즉시 점유한다.
2. 슬롯이 가득 찬 경우 요청자를 `_meleeSlotCandidates` 또는 `_rangedSlotCandidates`에 보관한다.
3. `_aggroDecisionInterval`마다 후보 큐를 정리하고 우선순위/적합도/대기 보너스가 가장 높은 후보를 선택한다.
4. 우선순위로 밀어낼 대상이 있으면 우선 교체하고, 없으면 `ComputeAggroFitness` 점수 차가 `_aggroFitnessTakeoverMargin` 이상일 때 교체한다.

빈 슬롯은 즉시 부여해 기존 BT 반응성을 유지하고, 포화 상태에서만 후보 큐를 사용한다. 따라서 단일/소수 인카운터의 즉시성은 유지하면서 군집 경쟁 상황의 슬롯 교체만 안정화한다.

#### 트레이드오프

- 즉시성 ↓ (1프레임 지연) — 보스급 1:1은 영향 거의 없음, 잡몹 군집에서만 효과
- 디버그성 ↑ — 각 멤버의 적합도 점수는 `GroupAggroFitness` Blackboard 키와 Formation Gizmos로 확인 가능

---

### Phase 4 — 그룹 공유 메모리 [G5]

#### 목표

플레이어 관찰을 그룹 단위 메모리로 승격한다. 멤버 A의 관찰이 멤버 B에게도 즉시 전달된다.

#### 새 컴포넌트: `MonsterGroupMemory`

`MonsterGroupController`와 같은 GameObject에 부착. `EnemyTacticalMemory`에서 플레이어 관찰 부분만 분리하여 그룹 레벨로 이동.

```csharp
public class MonsterGroupMemory : MonoBehaviour
{
    public int PlayerDodgeCountInWindow { get; }
    public int PlayerGuardCountInWindow { get; }
    public int PlayerAttackCountInWindow { get; }
    public int PlayerRecoverCountInWindow { get; }
    public float HitAccuracyAgainstPlayer { get; }   // 그룹 전체 누적
    public float LastHitOnGroupTime { get; }         // 멤버 누군가 피격받은 마지막 시각
    public bool IsPlayerStaggered { get; }
    public float GetSkillHitAccuracy(string skillId);
    public void NotifySkillLanded(string skillId);
    public void NotifySkillMissed(string skillId);
    // ...
}
```

#### 변경 영향

- `EnemyTacticalMemory`는 기존 API를 유지한다. 그룹 소속 적은 `MonsterGroupMemory`가 플레이어 관찰 카운트를 공유 소스로 제공한다.
- 플레이어 관찰은 `MonsterGroupMemory`에도 기록된다.
- `EnemyCombatDecisionEvaluator`가 두 메모리 모두 참조하며, 그룹 메모리가 있으면 그룹 메모리를 우선 사용한다.
- 그룹 미소속 단일 적은 자체 `EnemyTacticalMemory`가 플레이어 관찰까지 담당 (현행 동작 유지, fallback 경로)

#### 스킬별 적중률

그룹 공유 메모리는 플레이어 패턴뿐 아니라 **스킬별 적중률**도 누적한다. 전체 적중률 하나만 있으면 투사체가 계속 빗나가는 상황과 근접 공격이 잘 맞는 상황을 구분하지 못한다.

| 데이터 | 사용처 |
|--------|--------|
| `skillId -> landed/missed` | `EnemyCombat.SelectSkill` 후보 가중치 보정 |
| `recentMissCountBySkill` | 같은 스킬 반복 실패 시 일시 감쇠 |
| `recentHitCountBySkill` | 보스 Phase나 그룹 압박에서 성공 패턴 재사용 |

그룹 미소속 적은 `EnemyTacticalMemory` 내부의 동일 구조를 사용한다. 그룹 소속 적은 `MonsterGroupMemory`를 우선 조회하고, 없으면 자기 메모리로 fallback한다.

#### 마이그레이션

현재 구현에서는 기존 `EnemyTacticalMemory` API를 deprecated 처리하지 않았다. 그룹 메모리는 병렬 공유 메모리로 추가되어 외부 호출자 영향이 없다.

---

### Phase 5 — Formation 슬롯 (공간 점유) [G4]

> EQS는 사용하지 않는다. 단순한 각도 분할만 사용.

#### 목표

플레이어 주변을 N개 각도 슬롯으로 분할하고 멤버가 각 슬롯을 점유하도록 한다. 동일 슬롯에 두 멤버가 들어가지 않게 한다.

#### 데이터

```csharp
[Header("Formation")]
[SerializeField] private int _formationSlotCount = 8;   // 45도 간격
private readonly Dictionary<int, MonsterActor> _formationOwners = new();
```

#### API

```csharp
public int RequestFormationSlot(MonsterActor member, Vector3 playerPos, Vector3 playerForward);
public void ReleaseFormationSlot(MonsterActor member);
public Vector3 GetFormationSlotPosition(int slotIndex, Vector3 playerPos, float radius);
```

#### 슬롯 인덱스 계산

```
angle = Vector3.SignedAngle(playerForward, member.position - playerPos, Vector3.up);
slotIndex = Mathf.RoundToInt((angle + 180f) / (360f / _formationSlotCount)) % _formationSlotCount;
```

후보 슬롯이 비어 있으면 그대로 점유. 차 있으면 **인접 슬롯**부터 시계/반시계 양방향 탐색해 빈 곳 선택.

#### BT 통합

신규 노드 `RequestFormationSlotNode` 추가. `EnemyCircleState` / `EnemyFlankState`가 슬롯 위치를 목표로 이동.

현재 구현:
- `MonsterGroupController.TryGetFormationSlotPosition`
- `MonsterGroupController.RequestFormationSlot`
- `MonsterGroupController.ReleaseFormationSlot`
- `EnemyAIContext.TryGetFormationSlotPosition`
- `EnemyAIContext.ReleaseFormationSlot`
- `EnemyCircleState`는 Formation 슬롯 위치로 먼저 이동하고, 도착 후 기존 Circle 움직임을 유지
- `EnemyFlankState`는 Formation 슬롯 위치를 Flank 목적지로 사용

#### 회피 케이스

- 슬롯 모두 차면 점유 실패. 기존 점유자를 덮어쓰지 않고 해당 멤버는 기존 Circle/Flank 동작 또는 Pressure Intent 폴백을 사용한다.
- 멤버 사망 시 점유 해제는 `UnregisterMember` 경로 재사용.

---

## 4. 신규/변경 클래스 요약

| 위치 | 변경 종류 | 비고 |
|------|----------|------|
| `Assets/02.Scripts/GameActor/Group/MonsterGroupController.cs` | 확장 | Phase 1~5 모두 영향 |
| `Assets/02.Scripts/GameActor/Group/MonsterGroupMemory.cs` | 신규 | Phase 4 |
| `Assets/02.Scripts/GameActor/Component/Enemy/EnemyTacticalMemory.cs` | 유지 | Phase 4. 그룹 메모리는 병렬 추가 |
| `Assets/02.Scripts/GameActor/Component/Enemy/EnemyCombatDecisionEvaluator.cs` | 확장 | Phase 2. 그룹 Bias 입력 |
| `Assets/02.Scripts/GameActor/Component/Enemy/EnemyAIContext.cs` | 확장 | `CurrentGroupIntentBias` 추가 |
| `Assets/02.Scripts/GameActor/Component/Enemy/EnemyAIController.cs` | 확장 | 그룹 Bias 제공, 공격 종료 시 브리더 알림 |
| `Assets/10.Datas/AI/BehaviorTree/BehaviorTreeEditorRegistry.json` | 키 추가 | Group* Blackboard 기본값의 원본 |
| `Assets/02.Scripts/AI/BehaviorTree/Runtime/EnemyBlackboardKeys.generated.cs` | 키 생성 | Phase 2. Group* 디버그 키 |
| `Assets/02.Scripts/AI/BehaviorTree/Nodes/Action/RequestFormationSlotNode.cs` | 신규 | Phase 5 |
| `Assets/02.Scripts/Data/Actor/Enemy/EnemyBehaviorSO.cs` | 필드 추가 | Phase 1. breatherDuration 페이즈 오버라이드. 현재 미구현 |

---

## 5. 데이터 / SO 변경

### 5.1 `EnemyBehaviorSO` (미구현)

```csharp
[Header("Group Tempo")]
[Tooltip("그룹 멤버일 때 공격 종료 후 그룹 전체 브리더 윈도우 길이(초)")]
public float groupBreatherDuration = 0.6f;
```

페이즈별 오버라이드는 `BehaviorPhase` 구조에 동일 필드를 두고 `nullable` 또는 `< 0`을 "미지정"으로 처리.

### 5.2 `MonsterGroupController` Inspector

- `_maxMeleeAttackers`, `_maxRangedAttackers` (기존)
- `_breatherDuration` (Phase 1)
- `_aggroFitnessTakeoverMargin` (Phase 3)
- `_aggroDecisionInterval` (Phase 3 후보 큐 평가 간격)
- `_formationSlotCount` (Phase 5)

---

## 6. 호환성 및 마이그레이션

- 기존 씬의 `MonsterGroup` 프리팹/오브젝트는 신규 필드가 기본값으로 채워져 **현행 동작이 유지**된다.
- 그룹 미소속 단일 적은 모든 Phase에서 영향 받지 않는다 (`Group == null` 분기).
- `EnemyTacticalMemory`는 축소하지 않고 유지한다. 그룹 메모리는 그룹 소속 적에게만 우선 사용된다.
- `RequestFormationSlotNode`는 추가됐지만, `EnemyCircleState`/`EnemyFlankState`가 직접 Formation 슬롯을 요청하므로 기존 BT 에셋은 변경 없이도 Formation 효과를 받는다.

---

## 7. 검증 / 테스트 시나리오

| Phase | 검증 시나리오 |
|-------|--------------|
| 1 | 3마리 잡몹 인카운터: 한 명이 공격 종료 직후 0.6초 동안 다른 두 마리가 Attack 상태로 진입하지 않음을 BT 디버거에서 확인 |
| 2 | 슬롯 거절된 멤버의 `DecisionSelectedIntent`가 `Pressure` 또는 `KeepDistance`로 잡히는지 확인. `GroupIntent*`, `GroupBreatherRemainingTime` 키도 함께 확인 |
| 3 | 플레이어 등 뒤 멤버보다 정면 멤버가 후보 큐 평가 후 슬롯을 교체 획득하는지 확인. `GroupAggroFitness` 키로 점수 확인 |
| 4 | 멤버 A가 5초간 플레이어 회피 5회 관찰 후, 같은 그룹 멤버 B의 Punish 점수가 즉시 가산되는지 확인 |
| 5 | 4~5마리 인카운터에서 `Circle`/`Flank` 멤버 간 위치 겹침이 줄어드는지 확인. Scene Gizmos에서 점유 슬롯과 소유자 연결선 확인 |

---

## 8. 작업 순서 (권장)

1. **Phase 1** (완료) — `MonsterGroupController`에 Breather Window 추가, `EnemyAttackState.OnExit` → `EnemyAIController.ReleaseGroupSlot` → `NotifyMemberAttackEnded` 연결
2. **Phase 2** (완료) — `GroupIntentBias` 구조 + `EnemyAIContext.CurrentGroupIntentBias` + Evaluator 통합 + Blackboard Group* 디버그 키
3. **Phase 3** (완료) — 적합도 함수 + 슬롯 포화 시 후보 큐 기반 교체 + `_aggroDecisionInterval`
4. **Phase 4** (완료) — `MonsterGroupMemory` 신규, 그룹 관찰 카운트/적중률 + Evaluator 라우팅
5. **Phase 5** (완료) — Formation 슬롯 + `RequestFormationSlotNode` + `EnemyCircleState`/`EnemyFlankState` 목적지 연동

후속 후보였던 Group* Blackboard 디버그 키, Formation Gizmos, 후보 큐 기반 Aggro 결정은 구현 완료.

---

## 9. 명시적 비목표 (Out of Scope)

- **EQS (Environment Query System) 도입 안 함.** 엄폐물·고저차·낭떠러지 회피 등 공간 쿼리는 본 설계에 포함되지 않는다. Formation 슬롯도 환경 무시한 단순 각도 분할만 사용.
- **GOAP/Hierarchical BT 도입 안 함.** 다단계 계획 시스템은 별도 보스 전용 설계로 분리.
- **플레이어 N-gram 예측 모델 도입 안 함.** 그룹 공유 메모리(Phase 4)는 기존 빈도 기반 관찰 그대로 승격할 뿐, 예측기는 별도 작업.
- **그룹이 개별 State를 직접 전환하지 않는다.** 그룹은 슬롯, Bias, 공유 메모리만 제공하고 실제 실행은 각 멤버의 BT/Intent Resolver가 담당한다.

---

## 10. 참고

- 선행 문서: `Assets/docs/Complete/monster_ai_bt_design_gdd_kr.md`, `Assets/docs/Complete/MONSTER_AI_BT_APPLICATION_PLAN_GUIDE.md`
- 관련 시스템: `EnemyCombatDecisionEvaluator`, `BehaviorTreeRunner`, `EnemyActionResolver`
