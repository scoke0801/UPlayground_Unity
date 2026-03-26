# UPlayground - Unity Action RPG Project

## 프로젝트 개요

소울라이크 스타일의 액션 RPG. 3인칭 전투 시스템, 스테이트 머신 기반 액터 제어, 다단계 차지 공격, 적 AI를 갖춘 게임.

**주요 캐릭터:** Bokusei(카타나), Honoka(쌍도끼), LianLian(채찍)

---

## 폴더 구조

```
Assets/
├── 01.Scenes/         # 씬 파일 (GameLogic/, Test/)
├── 02.Scripts/        # 모든 C# 스크립트 (281개)
│   ├── Animation/     # 애니메이션 컨트롤러
│   ├── Camera/        # 카메라 시스템
│   ├── Data/          # 데이터 구조체/SO 정의
│   ├── GameActor/     # 액터 (Player, Enemy, NPC) 핵심 로직
│   ├── Input/         # 입력 처리
│   ├── Manager/       # 싱글턴 매니저들
│   └── UI/            # UI 시스템
└── 10.Datas/          # ScriptableObject 데이터 파일 (543개)
    ├── Actor/         # 캐릭터/적 데이터, 애니메이션 모션셋
    ├── Camera/        # 카메라 이펙트/셰이크 프리셋
    ├── Item/          # 장비/무기 데이터
    └── Story/         # 다이얼로그/스토리 데이터
```

---

## 핵심 아키텍처

### 1. 계층형 스테이트 머신

모든 액터의 행동은 상태로 관리된다.

**베이스 클래스:** `GameActorState` → `PlayerActorState` / `EnemyActorState` / `NpcActorState`

**플레이어 상태 목록 (17개):**
`Idle`, `GroundMove`, `Jump`, `Airborn`, `Attack`, `HeavyAttack`, `ChargeAttack`, `DashAttack`, `JumpAttack`, `Guard`, `GuardBreak`, `Dodge`, `Dash`, `Crouch`, `Grabbed`, `Interaction`, `Death`

상태 전환은 `ActorMovementController`가 담당하며, KCC(Kinematic Character Controller)와 통합되어 있다.

**주요 파일:**
- `Assets/02.Scripts/GameActor/State/Base/GameActorState.cs`
- `Assets/02.Scripts/GameActor/MovementController/ActorMovementController.cs`
- `Assets/02.Scripts/GameActor/State/Player/Player*State.cs`

---

### 2. 컴포넌트 시스템

액터는 `GameActor`를 상속하고, 기능별 컴포넌트를 조합한다.

| 컴포넌트 | 역할 |
|----------|------|
| `PlayerCombat` | 공격 실행, 히트 판정, 콤보, 가드 |
| `PlayerEquipment` | 무기 장착/교체 |
| `PlayerSkillGauge` | 스킬 게이지 관리 |
| `EnemyBrain` | AI 의사결정 |
| `EnemyCombat` | 적 공격 실행 |
| `EnemyDetection` | 타겟 탐지 |
| `PoiseStat` | 포이즈(강인도) 시스템 |

**주요 파일:**
- `Assets/02.Scripts/GameActor/Component/Player/PlayerCombat.cs`
- `Assets/02.Scripts/GameActor/Component/Enemy/EnemyBrain.cs`
- `Assets/02.Scripts/GameActor/Component/Common/PoiseStat.cs`

---

### 3. 애니메이션 시스템 (Animancer 기반)

**계층 구조:** `MotionSet` → `Motion[]`

- **MotionSet:** 애니메이션 클립 묶음 (한 동작의 전체 시퀀스)
- **Motion:** 개별 애니메이션 클립 + 메타데이터 (재생속도, 구간, 이벤트 타임라인)

**MotionEvent 종류:**

| 이벤트 | 역할 |
|--------|------|
| `MotionEvent_BeginCollision` | 히트 판정 시작 |
| `MotionEvent_Collision` | 히트박스 활성/비활성 |
| `MotionEvent_ComboWindow` | 콤보 입력 창 열기/닫기 |
| `MotionEvent_CameraEffect` | 카메라 이펙트 트리거 |
| `MotionEvent_AddForce` | 루트모션 힘 적용 |
| `MotionEvent_FinishAttack` | 처형 공격 트리거 |
| `LoopEvent` | 무한루프 구간 정의 (차지 공격에 핵심) |

**주요 파일:**
- `Assets/02.Scripts/Data/Actor/Animation/Motion.cs`
- `Assets/02.Scripts/GameActor/Animation/ActorAnimator.cs`

---

## 전투 시스템

### 공격 데이터 구조

```
PlayerAttackDataSO (ScriptableObject)
└── AttackInfoBase
    ├── AnimKey (모션셋 키)
    └── HitPhaseData[] (히트 구간 배열)
        ├── Damage, PoiseDamage
        ├── AttackRadius, HitAngle, HeightRange
        ├── Knockback/Airborne/Pull 힘
        ├── HitParticleName (VFX)
        └── ReactionType (Hit/Heavy/Knockback/Airborne/Knockdown/Grab/Stun...)
```

**런타임 인스턴스:** `AttackData` - 공격 실행 시 생성되며 대상, 충격지점, 방향 등을 포함.

**주요 파일:**
- `Assets/02.Scripts/Data/Combat/CombatData.cs`
- `Assets/02.Scripts/Data/Combat/PlayerAttackDataSO.cs`

---

### PlayerCombat 핵심 메서드

```csharp
ExecuteAttack(bool isCombo)           // 약공격 콤보
ExecuteHeavyAttack()                  // 강공격
ExecuteChargeAttack(stage, ratio)     // 차지공격 (스테이지 + 비율)
ExecuteSkillAttack(skillIndex)        // 스킬 공격
ExecuteJumpAttack()                   // 공중 공격
ExecuteDashAttack()                   // 대시 공격
PerformHitDetection()                 // 매 프레임 히트 판정 (구체 기반)
FindAttackSnapTarget()                // 자동조준 스냅
FindFinishableTarget()                // 처형 가능 대상 탐색 (HP < 30)
```

**히트 피드백 차별화:**

| 공격 종류 | Punch 강도 | 지속시간 | 셰이크 키 |
|-----------|-----------|---------|-----------|
| 약공격 | 0.08 | 0.12s | "LiteHit" |
| 강공격/대시/점프 | 0.18 | 0.18s | "HeavyHit" |
| 스킬/차지 | 0.22 | 0.20s | "HeavyHit" |

---

### 차지 공격 흐름 (PlayerChargeState)

```
OnEnter
  → 차지 애니메이션 재생 (InfiniteLoop 이벤트 포함)
  → Loop 구간: 시간 누적 (max 1.5초)
  → chargeRatio >= stageThreshold? → 루프 탈출 → 다음 스테이지
  → 버튼 해제 또는 최대 차지 → FireChargeAttack()
  → 데미지 배율: 1.0x ~ 1.5x (chargeRatio 기반)
```

**주요 파일:** `Assets/02.Scripts/GameActor/State/Player/PlayerChargeState.cs`

---

## 적 AI 시스템

### EnemyBrain (의사결정)
- 0.1초 간격 의사결정
- HP 임계값 기반 페이즈 전환
- 행동 가중치: `continueAttackChance`, `guardChance`, `retreatChance`, `chargeChance`
- `EnemyTacticalMemory`: 플레이어 마지막 행동, 예측 위치, 거리/각도 추적

### EnemyAttackDataSO 구조
- `skillType` (Attack/Heal/Spawn/Buff/Debuff)
- `minRange` / `maxRange` - 거리 게이팅
- `cooldown` - 재사용 대기시간
- `selectionWeight` - 확률 가중치
- `conditionGroup` - 복합 활성화 조건
- `isAerialSkill`, `isDiveAttack` - 공중 공격 여부

**주요 파일:** `Assets/02.Scripts/GameActor/Component/Enemy/EnemyBrain.cs`

---

## 포이즈(강인도) 시스템

- `PoiseStat` 컴포넌트 (모든 액터 공유)
- 포이즈 = 0 → 강제 히트 상태 진입
- 회복 딜레이 + 회복 속도 설정 가능
- **하이퍼아머:** 포이즈 데미지 면역 (보스 무적 구간)
- SO에서 최대 포이즈값 설정

---

## 매니저 시스템

모두 `BaseManager<T>` 싱글턴 기반.

| 매니저 | 역할 |
|--------|------|
| `InputManager` | 입력 버퍼링, 컨텍스트 레이어 |
| `CameraManager` | 록온, 펀치/셰이크 이펙트 |
| `GameHitStopManager` | 히트스톱 (경량/중량/크리티컬) |
| `GameObjectManager` | FX 풀링 및 재생 |
| `UIManager` | 데미지 플로터, HP바, HUD |
| `EventManager` | 글로벌 이벤트 브로드캐스트 |
| `DialogueManager` | 대화 흐름 |
| `VitalOrbManager` | 회복 오브 스폰 |

---

## 이동/물리 시스템

**KCC (Kinematic Character Controller) 통합**

| 이동 타입 | 속도 |
|-----------|------|
| 걷기 | 3 m/s |
| 달리기 | 6.5 m/s |
| 전력질주 | 10 m/s |
| 크라우치 | 3 m/s |
| 대시 | 18 m/s (0.3초) |
| 회피 | 7.5 파워 |

- 상승/하강 중력 배율 개별 설정
- 점프 유예시간 지원 (코요테 타임)
- 임펄스 기반 넉백/발사 (이동 속도와 별도)
- 루트모션 블렌딩 (상태별 선택)

---

## 라이트 어택 흐름 (전체 데이터 흐름 예시)

```
1. 입력 → InputManager 버퍼 등록
2. PlayerActor.Update() → _attackInputCondition 설정
3. ActorMovementController → 현재 상태로 전달
4. PlayerAttackState → 콤보 창 확인 → PlayerCombat.ExecuteAttack()
5. PlayerCombat → AttackData 런타임 생성 → OnAttackStarted 이벤트
6. ActorAnimator → MotionSet 재생 (AnimKey 기반)
7. MotionEvent_BeginCollision → 히트 판정 활성화
8. PlayerCombat.PerformHitDetection() → 매 프레임 스피어 캐스트
9. 히트 발생 → IDamageable.TakeDamage() → 적 상태 전환
10. UIManager → 데미지 플로터 / CameraManager → 셰이크
11. MotionEvent_ComboWindow → 다음 입력 창 열기
12. 애니메이션 종료 → Idle/Move로 복귀
```

---

## 핵심 파일 빠른 참조

| 목적 | 파일 경로 |
|------|-----------|
| 플레이어 전투 전체 | `Assets/02.Scripts/GameActor/Component/Player/PlayerCombat.cs` |
| 차지 공격 상태 | `Assets/02.Scripts/GameActor/State/Player/PlayerChargeState.cs` |
| 모션/이벤트 정의 | `Assets/02.Scripts/Data/Actor/Animation/Motion.cs` |
| 전투 데이터 구조 | `Assets/02.Scripts/Data/Combat/CombatData.cs` |
| 플레이어 공격 SO | `Assets/02.Scripts/Data/Combat/PlayerAttackDataSO.cs` |
| 액터 스테이트 베이스 | `Assets/02.Scripts/GameActor/State/Base/GameActorState.cs` |
| 액터 애니메이터 | `Assets/02.Scripts/GameActor/Animation/ActorAnimator.cs` |
| 이동 컨트롤러 | `Assets/02.Scripts/GameActor/MovementController/ActorMovementController.cs` |
| 적 AI | `Assets/02.Scripts/GameActor/Component/Enemy/EnemyBrain.cs` |
| 포이즈 시스템 | `Assets/02.Scripts/GameActor/Component/Common/PoiseStat.cs` |

---

## 코딩 컨벤션

- 폴더명: 번호 접두사 사용 (`01.Scenes`, `02.Scripts`, `10.Datas`)
- 스크립트: PascalCase 클래스명, `_camelCase` private 필드
- 데이터: ScriptableObject로 설정값 외부화
- 이벤트: `On` 접두사 (예: `OnAttackStarted`, `OnTargetAcquiredExternally`)
- 애니메이션 키: `AnimKey` enum 또는 string 상수로 참조
