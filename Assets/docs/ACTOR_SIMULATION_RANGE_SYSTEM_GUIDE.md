# 플레이어 중심 액터 시뮬레이션 범위 시스템 설계

## 개요

현재 일반 몬스터와 NPC는 플레이어와의 거리와 무관하게 상태 머신, KCC, 애니메이션을 계속 갱신한다. 적 AI 일부는 `AgentTickManager`로 통합되어 있고 `BehaviorTreeRunner`에는 타겟 거리 기반 Tick LOD가 있지만, 플레이어를 아직 감지하지 않은 원거리 몬스터와 일반 NPC의 시뮬레이션을 일괄 중단하는 기준은 없다.

이 문서는 활성 플레이어를 중심으로 일정 범위 안의 일반 몬스터와 NPC만 AI·KCC·애니메이션 시뮬레이션을 수행하도록 만드는 시스템을 설계한다.

핵심 원칙은 다음과 같다.

- 판정 기준은 카메라 가시성이 아니라 현재 조작 중인 `PlayerActor`와의 월드 거리다.
- 일반 몬스터와 NPC를 동일한 등록·거리 판정 파이프라인에서 관리한다.
- 범위 밖 액터는 GameObject 전체를 비활성화하지 않고 시뮬레이션 계층만 정지한다.
- 교전, 피격, 대화 등 진행 중인 게임플레이를 거리만으로 중단하지 않는다.
- 거리 판정은 액터마다 한 번만 수행하고, 개별 컴포넌트가 각자 플레이어 거리를 계산하지 않는다.
- 진입·이탈 거리 차이와 최소 유지 시간으로 경계 진동을 방지한다.

> 이 문서에서 `ActorSimulationManager`, `ActorSimulationParticipant`, `ActorSimulationSettingsSO` 등은 **신규 제안 타입**이다. 현재 프로젝트에 존재하는 API와 혼동하지 않는다.

---

## 목표와 비목표

### 목표

1. 범위 밖 일반 몬스터의 AI, 상태 머신, KCC, Animancer 평가 비용을 제거한다.
2. 범위 밖 NPC의 배회 상태, KCC, Animancer 평가 비용을 제거한다.
3. 플레이어 접근 시 눈에 띄는 지연 없이 안전하게 시뮬레이션을 복귀시킨다.
4. 런타임 스폰, 씬 배치, 파티 캐릭터 교체, 순간이동과 씬 전환을 모두 지원한다.
5. 거리 컬링 정책과 액터 내부 구현의 결합을 최소화한다.

### 비목표

- GameObject, 프리팹 또는 Addressable의 스트리밍·언로드
- 렌더러 LOD와 오클루전 컬링 대체
- 투사체, 드랍 아이템, 채집물의 시뮬레이션 정책 통합
- 보스전 도중 보스 시뮬레이션 정지
- 멀리 있는 NPC의 월드 스케줄을 실제 시간 단위로 계속 재현

월드 리전 스트리밍은 `Assets/docs/TODO/world-region-streaming.md`의 별도 관심사로 유지한다.

---

## 현재 업데이트 구조

### 일반 몬스터

```text
GameManager.Update
└── AgentTickManager.OnUpdate
    ├── EnemyAIController.ManagedTick
    ├── EnemyDetection.ManagedTick
    ├── BehaviorTreeRunner.ManagedTick
    └── EnemyTacticalMemory.ManagedTick

Unity Update
├── GameActor.Update
├── ActorMovementController.Update       상태 머신 UpdateState
├── ActorAnimator.Update/LateUpdate      MotionSet 및 이벤트
├── AbilitySystemComponent.Update/LateUpdate
├── PoiseStat.Update
├── MonsterBreakGauge.Update
└── MotionWarpController.Update

Unity FixedUpdate
└── KCCSimulator.FixedUpdate
    ├── 활성 Motor 전체 UpdatePhase1
    └── 활성 Motor 전체 UpdatePhase2
```

`AgentTickManager`는 MonoBehaviour Update 호출 수는 줄였지만 등록된 `IManagedTick`을 매 프레임 모두 순회한다. `EnemyDetection`의 실제 탐지 주기는 기본 0.2초이고 `BehaviorTreeRunner`의 기본 평가 주기는 0.1초지만, 타이머 확인과 등록 항목 순회는 계속 발생한다.

### NPC

NPC의 `NpcBrain`은 데이터만 제공하고, 실제 배회는 다음 경로에서 수행한다.

```text
ActorMovementController.Update
└── NpcIdleState / NpcWanderState / NpcTalkState.UpdateState

KCCSimulator.FixedUpdate
└── NpcMovementController가 연결된 Motor Phase1/Phase2

ActorAnimator.Update/LateUpdate
└── Idle / Walk / Talk Motion 평가
```

따라서 NPC에는 몬스터 AI 틱만 끄는 최적화가 적용되지 않으며, 상태 머신·KCC·애니메이션을 하나의 시뮬레이션 상태로 묶어야 한다.

---

## 적용 대상

### 기본 적용

| 액터 | 기본 정책 | 비고 |
|------|-----------|------|
| `MonsterActor`, `MonsterActorGrade.Normal` | 적용 | 일반 필드 몬스터 |
| `MonsterActor`, `MonsterActorGrade.Elite` | 설정으로 선택 | 엘리트 조우의 원거리 유지 필요 여부에 따라 결정 |
| `NpcActor` | 적용 | Idle, Wander 상태만 정지 가능 |
| `PlayerActor` | 제외 | 항상 시뮬레이션 |
| `MonsterActor`, `MonsterActorGrade.Boss` | 제외 | 보스 조우와 BossAssist 흐름 보호 |
| 기타 `GameActor` | 제외 | 별도 정책이 정의될 때까지 기존 동작 유지 |

### 강제 활성 조건

적용 대상이라도 다음 조건 중 하나가 참이면 거리 밖에서도 `Active`를 유지한다.

- 몬스터가 타겟을 보유하거나 교전 상태다.
- 공격, 피격, 가드, 스턴, 에어본, 넉다운, Grabbed, Death 연출 중이다.
- KCC Motor가 안정적으로 접지되지 않았다.
- MotionWarp, 루트 모션, 활성 히트 판정 또는 전투 Ability 실행 중이다.
- 락온, KillCam, 처형기, 대화, 퀘스트, 컷신이 액터를 명시적으로 점유한다.
- NPC가 `IsInteracting()` 상태거나 `NpcTalkState`다.
- 액터 또는 외부 시스템이 명시적 활성 임대를 보유한다.

거리 판정 코드가 모든 구체 상태 타입을 직접 나열하지 않도록, 액터가 `CanSuspendSimulation`과 활성 임대 수를 제공하는 방식으로 캡슐화한다.

---

## 상태 모델

첫 구현은 `Active`와 `Suspended` 두 단계만 사용한다.

```text
                         강제 활성 사유 발생
                  ┌──────────────────────────────┐
                  │                              ▼
              Suspended ── wake 거리 진입 ──> Active
                  ▲                              │
                  └── sleep 거리 이탈 +         │
                      안전 조건 충족 + 유지시간 ─┘
```

### Active

- 몬스터 AI와 BT 정상 틱
- `ActorMovementController.UpdateState` 정상 실행
- KCC Motor Phase 1·2 정상 실행
- Animancer 그래프와 MotionSet 이벤트 정상 실행

### Suspended

- 몬스터 `IManagedTick` 호출 생략
- `ActorMovementController.UpdateState` 호출 생략
- KCC 활성 Motor 스냅샷에서 제외
- Animancer 그래프 평가와 `ActorAnimator.Update/LateUpdate` 중단
- Transform, Collider, Renderer, 상호작용용 GameObject는 유지
- 매니저의 저빈도 거리 판정과 강제 활성 요청만 유지

`Reduced` 단계는 첫 구현의 안정성과 검증 범위를 넓히므로 제외한다. Active 범위 내 몬스터 수가 여전히 많다는 프로파일 근거가 생기면 탐지·BT 주기만 낮추는 중간 단계를 후속으로 추가한다.

---

## 거리 정책

### 제안 기본값

| 설정 | 기본값 | 설명 |
|------|-------:|------|
| `wakeDistance` | 55m | Suspended 액터가 Active로 복귀하는 거리 |
| `sleepDistance` | 65m | Active 액터가 Suspended 후보가 되는 거리 |
| `evaluationInterval` | 0.2초 | 거리 배치 평가 주기 |
| `evaluationBuckets` | 4 | 한 평가 주기의 부하를 여러 프레임에 분산 |
| `minimumActiveDuration` | 1.0초 | 복귀 직후 다시 정지되지 않는 최소 시간 |
| `unsafeRetryInterval` | 0.25초 | 범위 밖이지만 아직 정지 불가능한 액터의 재검사 주기 |
| `teleportRefreshDistance` | 20m | 플레이어 중심이 크게 이동했을 때 즉시 전체 재평가하는 거리 |

`wakeDistance < sleepDistance` 관계를 강제한다. 거리 비교는 `Vector3.sqrMagnitude`로 수행하여 제곱근 계산을 피한다.

카메라 프러스텀과 `Renderer.isVisible`은 기준으로 사용하지 않는다. 플레이어가 뒤를 돌아도 가까운 적과 NPC의 게임플레이는 계속 진행되어야 한다.

### 플레이어 기준점

기준점은 `GameObjectManager.Player`, 즉 현재 활성 `PlayerActor`의 Transform이다. 파티 교체로 활성 플레이어가 바뀌어도 다음 평가에서 자동으로 새 기준점을 사용한다.

다음 상황에서는 버킷을 무시하고 전체 대상을 즉시 재평가한다.

- Player 참조가 다른 인스턴스로 변경됨
- 플레이어가 `teleportRefreshDistance` 이상 이동
- 씬 전환 완료
- 런타임 시스템이 명시적으로 `ForceRefresh` 요청

Player가 아직 준비되지 않았다면 모든 대상은 안전하게 `Active`를 유지한다.

---

## 제안 아키텍처

```text
GameObjectManager
├── AllActors
├── OnActorRegistered
└── OnActorUnregistered
          │
          ▼
ActorSimulationManager                         신규 Manager 구현
├── 활성 Player 위치 확인
├── 거리 버킷 평가
├── 강제 활성 임대 관리
└── ActorSimulationParticipant 상태 전환 요청
          │
          ▼
ActorSimulationParticipant                     신규 Actor 소유 컴포넌트
├── 적용 대상/등급 판정
├── CanSuspendSimulation
├── ActorMovementController 게이트
├── ActorAnimator 게이트
└── KCC Motor 슬립/복귀
          │
          ├───────────────┐
          ▼               ▼
AgentTickManager      KCCSimulator
액터 단위 AI 게이트    Active Motor 스냅샷 제외
```

### `ActorSimulationManager` — 신규 제안

위치 제안:

```text
Assets/02.Scripts/Manager/Actor/ActorSimulationManager.cs
```

역할:

- `GameObjectManager.OnActorRegistered/OnActorUnregistered`를 구독한다.
- 일반 몬스터와 NPC에 연결된 `ActorSimulationParticipant`를 등록한다.
- 현재 플레이어와 각 참여자의 제곱 거리를 버킷 단위로 평가한다.
- 거리, 최소 활성 시간, 강제 활성 임대, `CanSuspendSimulation`을 합성해 상태를 결정한다.
- 씬 전환과 Dispose에서 구독 및 캐시를 정리한다.

`GameManager` 등록 순서는 `GameObjectManager`와 `PartyManager` 이후, `AgentTickManager` 이전을 권장한다.

```text
GameObjectManager
→ PartyManager
→ ...
→ ActorSimulationManager
→ AgentTickManager
```

Manager 구현이 Actor 내부의 구체 싱글톤 의존을 만들지 않도록, Actor 측은 소비자 소유 계약을 통해 현재 상태만 조회한다.

### `ActorSimulationParticipant` — 신규 제안

위치 제안:

```text
Assets/02.Scripts/GameActor/Simulation/ActorSimulationParticipant.cs
```

액터 하나의 시뮬레이션 상태 전환을 소유한다. Manager가 `Motor.enabled`, Animancer 그래프, AI 컴포넌트를 직접 개별 조작하지 않게 한다.

제안 책임:

```csharp
public enum ActorSimulationState
{
    Active,
    Suspended,
}

public interface IActorSimulationParticipant
{
    GameActor Actor { get; }
    ActorSimulationState State { get; }
    bool CanSuspendSimulation { get; }

    void SetSimulationState(ActorSimulationState state);
    IDisposable AcquireActiveLease(object owner, string reason);
}
```

위 API는 설계안이며 구현 시 Actor 모듈의 기존 asmdef 경계를 따른다. 활성 임대는 대화·퀘스트·컷신·락온 시스템이 거리보다 높은 우선순위로 액터를 깨우고 유지하는 용도다. `IDisposable` 해제로 대칭성을 보장하며, 개발 빌드에서는 owner와 reason을 진단 정보로 보존한다.

### 설정 데이터 — 신규 제안

위치 제안:

```text
Assets/02.Scripts/Data/Config/ActorSimulationSettingsSO.cs
Assets/10.Datas/Config/ActorSimulationSettings.asset
```

`ActorSimulationSettingsSO`에는 거리·주기·등급 정책만 둔다. Manager나 Actor 구현 타입을 참조하지 않아 Data 모듈 경계를 보존한다.

---

## 하위 시스템별 게이팅

### 1. AI와 BT

현재 `IManagedTick`에는 소유 액터 정보가 없으므로 `AgentTickManager`는 동일 몬스터의 AI, 탐지, BT, 전술 메모리에 대해 거리 상태를 공유할 수 없다.

권장 변경은 등록 시 소유 액터를 함께 전달하는 것이다.

```csharp
// 신규 제안 오버로드
void Register(GameActor owner, IManagedTick tick);
```

`AgentTickManager.OnUpdate`는 owner가 Suspended이면 해당 owner에 속한 모든 tick을 건너뛴다. 거리 계산은 하지 않고 `ActorSimulationManager`가 결정한 상태만 조회한다.

Suspended 전환 시 `BehaviorTreeRunner.OnDisable()`을 호출하거나 컴포넌트 `enabled`를 끄지 않는다. 현재 `OnDisable()`은 `StopTree()`를 호출하므로 복귀 시 런타임 트리 재생성과 상태 초기화가 발생한다. 대신 런타임 트리와 Blackboard를 유지한 채 tick 호출만 정지한다.

복귀 시 정책:

- BT `_tickTimer`는 즉시 1회 평가할 수 있도록 준비한다.
- `EnemyDetection`은 즉시 한 번 탐지한 뒤 기존 0.2초 주기로 돌아간다.
- `EnemyTacticalMemory`의 관찰 위치를 현재 플레이어 위치로 재기준화하여 수면 중 이동량을 한 프레임 이동으로 오인하지 않게 한다.
- 범위 밖에서 이미 타겟을 보유했다면 Suspended 진입 자체를 거부한다.

### 2. 상태 머신과 KCC

`ActorMovementController.Update()`는 Participant 상태가 Suspended이면 `UpdateState`를 호출하지 않는다. KCC 비용을 실제로 제거하려면 이것만으로 부족하며 `KCCSimulator.BuildActiveMotorSnapshot()`에서도 Suspended Motor를 제외해야 한다.

Motor 슬립은 다음 조건에서만 허용한다.

- `Motor.GroundingStatus.IsStableOnGround == true`
- 현재 속도와 대기 중 impulse가 안전 임계값 이하
- MotionWarp 비활성
- 전투/피격/대화 상태가 아님

Suspended 진입 절차:

1. 현재 Transform과 안정 접지 정보를 스냅샷으로 보관한다.
2. 상태가 Idle이 아니면 안전 상태 전환 가능 여부를 확인한다.
3. 잔여 루트 모션을 `ActorAnimator.FlushRootMotion()`으로 비운다.
4. KCC 권위 속도와 pending impulse를 정책에 맞게 정리한다.
5. Motor를 시뮬레이션 스냅샷에서 제외한다.

복귀 절차:

1. Transform을 KCC의 transient 위치·회전에 재동기화한다.
2. 지면 probe 또는 짧은 지면 스냅으로 접지를 확인한다.
3. stale root motion과 target velocity history를 초기화한다.
4. Motor를 다음 FixedUpdate의 활성 스냅샷에 포함한다.
5. 상태 머신을 마지막 상태에서 재개하거나 정책상 Idle로 재진입한다.

`Motor.enabled = false`만 직접 적용하면 공중·경사면 정지와 복귀 시 위치 보정 문제가 생긴다. KCC 제외 정책과 복귀 동기화를 `ActorMovementController` 또는 전용 어댑터가 소유해야 한다.

#### NPC 상태 정책

- `NpcIdleState`: 즉시 Suspended 가능
- `NpcWanderState`: 안정 접지 후 현재 위치에서 Suspended 가능
- `NpcTalkState`: Suspended 금지
- 복귀 시 NPC 배회 타이머는 수면 시간을 따라잡지 않고 정지했던 값에서 계속한다.
- Wander 목표점이 복귀 후 유효하지 않거나 너무 멀면 새 목표점을 선택한다.

NPC의 원거리 배회 결과까지 월드 상태에 반영할 필요가 생기면 실제 KCC 이동을 계속 돌리지 말고, 별도의 저빈도 스케줄/논리 위치 시스템으로 확장한다.

### 3. 애니메이션과 MotionSet

Manager가 `Animator.enabled` 또는 `AnimancerComponent`를 직접 조작하지 않는다. `ActorAnimator`에 시뮬레이션 일시정지 API를 추가해 Animancer 그래프, 서브 애니메이터, MotionSet 디렉터 시간, 이벤트 실행기의 소유권을 한곳에 둔다.

```csharp
// ActorAnimator 신규 제안 API
public void SetSimulationPaused(bool paused);
```

정지 가능한 시점은 Idle/Walk 같은 비전투 루프 모션으로 제한한다. 공격 MotionSet, 타임라인 이벤트, 루트 모션, Freeze/Loop 이벤트 실행 중에는 `CanSuspendSimulation`이 false를 반환한다.

Suspended 동작:

- Animancer 그래프 평가 정지
- `ActorAnimator.Update/LateUpdate`의 타임라인 진행 정지
- `MotionEventExecutor` 신규 이벤트 발화 정지
- 마지막 포즈와 Renderer는 유지
- 서브 애니메이터도 같은 상태 적용

복귀 동작:

- 그래프 평가 재개 전에 stale root motion을 폐기
- NPC/몬스터의 현재 상태에 맞는 루프 모션을 다시 확인
- 수면 시간을 MotionSet 진행 시간에 가산하지 않음
- 첫 LateUpdate에서 과거 구간의 MotionEvent가 한꺼번에 발화하지 않도록 이벤트 커서를 재기준화

`GameActor.LocalTimeScale`은 히트스톱과 슬로우 모션의 소유권이므로 시뮬레이션 정지에 사용하지 않는다. 정지 시 `LocalTimeScale = 0`으로 만드는 방식은 현재 최소값 클램프 및 복수 연출 소유권과 충돌한다.

### 4. Ability와 기타 Update

1차 범위는 AI·상태 머신·KCC·애니메이션이다. `AbilitySystemComponent`, `PoiseStat`, `MonsterBreakGauge` 등은 기존 갱신을 유지한다.

다만 실제 프로파일에서 범위 밖 액터의 나머지 Update 비용이 확인되면 2차 단계에서 다음 정책을 추가한다.

- 활성 Ability Task가 없는 Suspended 액터의 Effect/Cooldown 만료 정리 저빈도화
- Poise/Break가 기본 상태인 경우 조기 반환 또는 관리형 tick 통합
- 월드 절대 시간 기반 만료와 액터 로컬 시간 기반 회복을 명시적으로 분리

Ability 실행 중인 액터를 바로 정지시키는 것은 Task 완료, 쿨다운, GameplayEffect 이벤트 순서를 깨뜨릴 수 있으므로 1차 구현에서는 활성 임대로 보호한다.

---

## 등록과 생명주기

### 씬 배치 액터

`GameObjectManager.AfterInit()`은 씬의 모든 `GameActor`를 스캔해 `RegisterActor` 누락을 보정한다. `ActorSimulationManager`는 AfterInit 시 기존 `AllActors`를 한 번 수집하고 이후 등록 이벤트를 구독한다.

### 런타임 스폰 액터

`GameActor.Awake()`의 `ActorSvc.Objects?.RegisterActor(this)` 경로로 자동 등록한다. ActorSimulationManager가 아직 준비되지 않은 부트 순서에서도 `GameObjectManager.AllActors`를 기준으로 AfterInit 보정한다.

### 파괴와 씬 전환

- `OnActorUnregistered`에서 Participant와 활성 임대 진단 정보를 제거한다.
- 씬 전환 시 파괴된 Unity Object를 정리하고 Player 참조를 무효화한다.
- 새 씬의 Player가 준비될 때까지 등록 대상은 Active를 유지한다.
- Dispose 시 모든 Suspended 액터를 Active로 복귀시킨 뒤 구독을 해제한다. 에디터 Play Mode 종료와 스크립트 리컴파일 시 Motor/Animator가 정지 상태로 남지 않게 한다.

---

## 상태 전환 우선순위

아래 순서로 판정한다. 위 조건이 아래 조건보다 우선한다.

```text
1. Player 또는 적용 제외 액터             → Active
2. Player 참조 없음                       → Active
3. 명시적 활성 임대 존재                 → Active
4. CanSuspendSimulation == false          → Active
5. 현재 Suspended && 거리 <= wakeDistance → Active
6. 현재 Active && 거리 >= sleepDistance
   && minimumActiveDuration 경과           → Suspended
7. 그 외                                  → 현재 상태 유지
```

거리 밖이지만 `CanSuspendSimulation == false`인 액터는 매 프레임 검사하지 않고 `unsafeRetryInterval` 후 재검사한다. 강제 활성 임대 취득은 평가 주기를 기다리지 않고 즉시 Active로 전환한다.

---

## 실패 방지 규칙

### 전투

- 타겟이 있는 몬스터를 거리만으로 정지하지 않는다.
- 공격 MotionSet 중 애니메이션을 정지하지 않는다.
- 범위 이탈 직전 생성된 투사체는 ProjectileManager 정책에 따라 독립적으로 계속 진행한다.
- 공격 히트 윈도우가 열린 상태에서는 Suspended 진입을 거부한다.
- 사망 디졸브, 보상, 재스폰 등록이 끝나기 전에는 정지하지 않는다.

### NPC와 대화

- `NpcActor.IsInteracting()`이면 강제 Active다.
- 상호작용 후보 반경은 `wakeDistance`보다 작아야 한다. 플레이어가 상호작용 범위에 들어오기 전에 NPC가 먼저 깨어나야 한다.
- Dialogue/Story/Quest가 원거리 NPC를 연출 대상으로 사용할 때 활성 임대를 취득한다.
- 대화 종료 시에만 임대를 해제하고, 종료 이벤트 누락을 대비해 owner 파괴 시 자동 정리한다.

### KCC

- 공중, 움직이는 플랫폼 위, 강한 impulse 잔존 상태는 슬립 금지다.
- 복귀 프레임에 `UpdatePhase2`만 단독 실행하지 않는다. 다음 정상 FixedUpdate의 Phase 1·2 장벽에 합류한다.
- Transform과 Motor transient 상태가 다르면 Transform만 순간 이동시키지 말고 KCC 동기화 API를 사용한다.

### 애니메이션

- `ActorAnimator.Speed`를 정지 플래그로 사용하지 않는다. 이 값은 `LocalTimeScale`과 공유된다.
- 복귀 시 수면 구간의 MotionEvent를 재생하지 않는다.
- Animator 컬링 복귀 직후 stale root motion delta를 한 번 소비하지 않도록 명시적으로 flush한다.

---

## 디버그와 관측성

개발 빌드와 에디터에서는 다음 정보를 제공한다.

| 항목 | 설명 |
|------|------|
| 상태 | Active / Suspended |
| 거리 | 현재 Player와의 실제 거리 |
| 다음 임계값 | wake 또는 sleep 거리 |
| 정지 거부 사유 | Airborne, Combat, Dialogue, MotionWarp 등 |
| 활성 임대 | owner 타입과 reason |
| 마지막 전환 시각 | 상태 진동과 지연 진단 |
| 누적 수 | Active/Suspended/등록 전체 수 |

`ActorRuntimeMonitorWindow`에는 Simulation 열을 추가하고, `DebugGizmoManager`에는 wake/sleep 반경과 선택 액터의 상태 라벨을 선택적으로 표시한다. 런타임 릴리스 빌드에서는 문자열 사유와 임대 스택 추적을 제거한다.

ProfilerMarker 제안:

```text
ActorSimulation.EvaluateBuckets
ActorSimulation.TransitionToActive
ActorSimulation.TransitionToSuspended
AgentTick.Active
AgentTick.Skipped
KCCSimulator.ActiveMotors
```

---

## 구현 단계

### Phase 1 — 관측 전용

1. `ActorSimulationSettingsSO`와 Manager를 추가한다.
2. 일반 몬스터/NPC 등록과 거리 판정만 수행한다.
3. 실제 정지는 하지 않고 예상 Active/Suspended 수, 전환 사유, 거리 분포를 기록한다.
4. 대표 필드에서 거리 기본값과 예상 절감량을 확정한다.

### Phase 2 — AI 게이트

1. `AgentTickManager.Register(owner, tick)` 계약을 추가한다.
2. Suspended 일반 몬스터의 AI, Detection, BT, TacticalMemory 호출을 생략한다.
3. 복귀 즉시 탐지·BT 재평가와 TacticalMemory 재기준화를 구현한다.
4. 몬스터 전투 진입·이탈 회귀 테스트를 수행한다.

### Phase 3 — NPC와 상태 머신 게이트

1. `ActorMovementController`에 시뮬레이션 상태 게이트를 추가한다.
2. NPC Idle/Wander 정지와 Talk 강제 활성 규칙을 구현한다.
3. 원거리 NPC 접근, 상호작용, 대화 종료를 검증한다.

### Phase 4 — KCC 슬립

1. 안전한 `CanSuspendSimulation` 조건을 구현한다.
2. `KCCSimulator` 활성 Motor 스냅샷에서 Suspended Motor를 제외한다.
3. 접지·transient 상태·impulse·root motion 복귀 절차를 구현한다.
4. 평지, 경사면, 플랫폼, 공중, 넉백 상태를 각각 검증한다.

### Phase 5 — 애니메이션 게이트

1. `ActorAnimator.SetSimulationPaused`를 추가한다.
2. Animancer 그래프와 MotionEvent 커서의 정지·복귀 계약을 구현한다.
3. Idle/Walk 루프, 서브 애니메이터, 루트 모션, MotionSet 이벤트를 검증한다.

### Phase 6 — 프로파일과 튜닝

1. 몬스터/NPC 혼합 20, 50, 100개 시나리오를 측정한다.
2. Player 근처 활성 수가 같은 상태에서 전체 등록 수만 늘려 원거리 비용 증가가 억제되는지 확인한다.
3. 평가 버킷, wake/sleep 거리, 최소 활성 시간을 튜닝한다.
4. Player Build와 실제 사이클 런에서 스파이크와 메모리 할당을 확인한다.

---

## 검증 시나리오

### 기능 검증

| ID | 시나리오 | 기대 결과 |
|----|----------|-----------|
| S01 | 일반 몬스터가 sleep 거리 밖에 배치됨 | AI·상태·KCC·애니메이션 Suspended |
| S02 | 플레이어가 wake 거리 안으로 접근 | 평가 주기 내 Active, 즉시 탐지 가능 |
| S03 | 경계에서 앞뒤 이동 | 히스테리시스로 상태 진동 없음 |
| S04 | 어그로 몬스터가 sleep 거리 밖으로 추격 | 타겟 해제 또는 안전 복귀 전까지 Active |
| S05 | 공중/넉백 몬스터가 범위 이탈 | 접지·상태 종료 전까지 Active |
| S06 | NPC Idle 상태로 범위 이탈 | 안전하게 Suspended |
| S07 | NPC Wander 상태로 범위 이탈 | 접지 위치 보존 후 Suspended |
| S08 | 플레이어가 NPC에게 접근 | 상호작용 반경 진입 전에 Active |
| S09 | 대화 중 Player가 이동/텔레포트 | NPC는 활성 임대로 Active 유지 |
| S10 | 파티 활성 Player 교체 | 새 Player 위치 기준 즉시 재평가 |
| S11 | 포털/순간이동으로 20m 이상 이동 | 전체 대상 즉시 재평가 |
| S12 | 씬 전환 및 Play Mode 종료 | 정지 상태와 등록 캐시 누수 없음 |

### KCC·애니메이션 회귀

| ID | 시나리오 | 기대 결과 |
|----|----------|-----------|
| R01 | 경사면 위 Idle 몬스터 정지/복귀 | 침투, 낙하, 위치 점프 없음 |
| R02 | 움직이는 플랫폼 위 액터 | 슬립 거부 또는 안전한 플랫폼 정책 적용 |
| R03 | 루트 모션 직후 정지/복귀 | stale delta 순간 이동 없음 |
| R04 | MotionSet 이벤트 직전 범위 이탈 | 실행 중 MotionSet 때문에 슬립 거부 |
| R05 | NPC Walk 포즈에서 정지/복귀 | 이벤트 몰아치기 없이 정상 루프 복귀 |
| R06 | 히트스톱 중 범위 변화 | LocalTimeScale 소유권 손상 없음 |

### 성능 완료 기준

동일한 Player 주변 활성 수를 유지하면서 전체 등록 수를 늘렸을 때 다음을 만족해야 한다.

- 원거리 Suspended 액터 수에 비례해 `AgentTickManager.OnUpdate`가 선형 증가하지 않는다.
- `KCCSimulator.FixedUpdate` 비용은 전체 등록 Motor 수가 아니라 Active Motor 수에 주로 비례한다.
- Animator/Animancer 평가 수는 Active 액터 수에 주로 비례한다.
- 거리 배치 평가에서 프레임당 GC Alloc 0 B를 유지한다.
- Player 순간이동과 대량 복귀에서 단일 프레임 스파이크가 허용 예산을 넘지 않는다.
- 100개 혼합 액터 테스트에서 Missing Script, managed reference, VFX 참조 변경이 발생하지 않는다.

절대 밀리초 목표는 대상 하드웨어와 현재 프레임 예산을 프로파일한 뒤 확정한다. Deep Profile 결과만으로 완료 여부를 판단하지 않는다.

---

## 수정 예상 파일

### 신규

```text
Assets/02.Scripts/Data/Config/ActorSimulationSettingsSO.cs
Assets/02.Scripts/GameActor/Simulation/ActorSimulationParticipant.cs
Assets/02.Scripts/GameActor/Simulation/ActorSimulationState.cs
Assets/02.Scripts/Manager/Actor/ActorSimulationManager.cs
Assets/Tests/EditMode/ActorSimulationPolicyTests.cs
```

### 변경

```text
Assets/02.Scripts/Manager/GameManager.cs
Assets/02.Scripts/Manager/Object/GameObjectManager.cs
Assets/02.Scripts/GameActor/AI/AgentTickManager.cs
Assets/02.Scripts/GameActor/AI/BehaviorTree/Runtime/BehaviorTreeRunner.cs
Assets/02.Scripts/GameActor/Component/Enemy/EnemyAIController.cs
Assets/02.Scripts/GameActor/Component/Enemy/EnemyDetection.cs
Assets/02.Scripts/GameActor/Component/Enemy/EnemyTacticalMemory.cs
Assets/02.Scripts/GameActor/MovementController/ActorMovementController.cs
Assets/02.Scripts/GameActor/MovementController/MotionWarpController.cs
Assets/02.Scripts/GameActor/Animation/ActorAnimator.cs
Assets/02.Scripts/Manager/KCCSimulator.cs
Assets/02.Scripts/GameActor/Object/NPC/NpcActor.cs
```

실제 구현 전 각 파일의 asmdef 소속을 다시 확인한다. Manager 구현을 하위 Actor 모듈에서 직접 참조하지 않고, Actor 소비자가 소유한 계약 또는 기존 `Services` 등록 경계를 사용한다.

---

## 최종 권장안

첫 릴리스는 일반 몬스터와 NPC에 대해 Active/Suspended 2단계로 구현한다. 거리 판정은 `ActorSimulationManager`가 0.2초 주기 버킷으로 한 번만 수행하고, 액터 소유 `ActorSimulationParticipant`가 AI·상태 머신·KCC·Animancer의 안전한 정지와 복귀를 조정한다.

성능 효과가 가장 큰 KCC까지 정지하되, AI → NPC 상태 → KCC → 애니메이션 순서로 단계적으로 적용한다. 각 단계는 독립적으로 프로파일하고 기능 검증을 통과한 뒤 다음 단계로 진행한다. 게임플레이 예외는 거리보다 우선하며, 대화·전투·공중 상태처럼 중단할 수 없는 상황은 활성 임대로 명시적으로 보호한다.
