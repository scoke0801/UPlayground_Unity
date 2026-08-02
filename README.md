# ⚔️ UPlayground

Unity 6 기반 싱글플레이 TPS 액션 게임 개인 개발 프로젝트

<br>

## 📌 프로젝트 개요

| 항목 | 내용 |
|------|------|
| **엔진** | Unity 6 (6000.3.21f1) |
| **렌더 파이프라인** | URP (Universal Render Pipeline) |
| **장르** | 싱글플레이 3인칭 액션 |
| **개발 인원** | 1인 개발 |

<br>

## 🛠️ 사용 기술 및 에셋

### 핵심 플러그인
- **Animancer Pro V8** — 코드 기반 애니메이션 제어. MotionSet 타임라인 시스템으로 복합 모션 순차 재생 및 이벤트 처리
- **Kinematic Character Controller** — 물리 기반이 아닌 Kinematic 방식의 캐릭터 이동. State 패턴과 결합하여 정밀한 움직임 제어
- **MagicaCloth2** — 캐릭터 의상, 머리카락 등 Cloth 시뮬레이션
- **Addressables** — 런타임 에셋 로딩 및 메모리 관리

### 기타
- **lilToon** — 캐릭터 툰 셰이더

<br>

## 🏗️ 아키텍처

### 매니저 시스템

`GameManager`를 최상위로 두고, 모든 서브 매니저를 순차적으로 초기화하는 중앙 집중형 구조입니다.

```
GameManager (최상위)
├── InputManager          // Input System 기반 입력 처리, InputLayer 레벨 제어
├── AssetManager          // Addressables 에셋 로딩
├── UIManager             // UI 생성 및 레이어 관리
├── CameraManager         // 카메라 제어 및 카메라 쉐이크
├── GameObjectManager     // 오브젝트 생성, FX, 무기 관리, 인터랙션 핸들링
├── ItemManager           // 아이템 데이터 관리
├── InventoryManager      // 인벤토리 시스템
├── EventManager          // 이벤트 기반 통신
├── GameHitStopManager    // 히트스톱 연출
└── SceneManager          // 씬 전환
```

각 매니저는 `IManager` 인터페이스를 구현하여 `Init → AfterInit → OnUpdate → Dispose` 생명주기를 따릅니다.

### GameActor 계층 구조

모든 게임 오브젝트의 베이스 클래스인 `GameActor`를 중심으로 설계되었습니다.

```
GameActor (추상 클래스)
├── PlayerActor       // 플레이어 캐릭터 (partial class로 역할 분리)
├── MonsterActor      // 적 몬스터
├── GatheringActor    // 채집 오브젝트
├── ItemActor         // 필드 아이템
└── (NPC — 확장 예정)
```

`GameActor`는 소켓 시스템(`ActorSocketType`)을 통해 무기 장착점, 이펙트 위치 등을 관리합니다.

### 상태 머신 (State Machine)

`KinematicCharacterController`의 `ICharacterController`와 결합된 상태 머신으로, 각 상태가 캐릭터의 이동/회전/물리를 직접 제어합니다.

```
GameActorState (추상 베이스)
├── PlayerActorState (플레이어 베이스)
│   ├── PlayerIdleState
│   ├── PlayerGroundMoveState
│   ├── PlayerAirbornState
│   ├── PlayerAttackState
│   ├── PlayerDashState
│   ├── PlayerDodgeState
│   ├── PlayerGuardState
│   ├── PlayerHitState
│   ├── PlayerDeathState
│   ├── PlayerCrouchingState
│   └── PlayerInteractionState
│
└── Enemy States
    ├── EnemyIdleState
    ├── EnemyPatrolState
    ├── EnemyChaseState
    ├── EnemyAttackState
    ├── EnemyHitState / EnemyAirborneState
    ├── EnemyGuardState
    ├── EnemyRetreatState
    ├── EnemyCircleState
    └── EnemyDeathState
```

각 상태는 `UpdateVelocity`, `UpdateRotation` 등 KCC 콜백을 오버라이드하여 상태별 물리 동작을 정의합니다.

### 컴포넌트 기반 설계

`GameActor`에 기능별 컴포넌트를 조합하는 방식입니다.

| 컴포넌트 | 역할 |
|-----------|------|
| `ActorAnimator` | Animancer 기반 애니메이션 제어, MotionSet 타임라인 |
| `ActorMovementController` | KCC 연동 이동 제어 + 상태 머신 관리 |
| `PlayerCombat` / `EnemyCombat` | 전투 로직 (공격, 데미지, 스킬) |
| `PlayerEquipment` | 무기 장착/해제 |
| `EnemyBrain` | 적 AI 의사결정 (감지, 추적, 전투 스타일) |
| `EnemyDetection` | 플레이어 감지 (거리, 시야) |
| `ActorColorChanger` | 피격 시 머티리얼 색상 변경 |
| `DissolveController` | 사망 시 디졸브 이펙트 |

### 적 AI 시스템

`EnemyBrain`이 주기적으로 의사결정을 수행하며, 전투 스타일에 따라 행동 패턴이 달라집니다.

```
EnemyCombatStyle
├── Melee      — 거리 유지 없이 근접 돌진
├── Ranged     — 원거리 유지하며 견제
├── Balanced   — 적절한 거리에서 공수 전환
└── Support    — 힐/버프 스킬 우선 사용
```

공격 후에는 확률 기반으로 **연속 공격 / 가드 전환 / 후퇴 배회** 중 하나를 선택하여 단조롭지 않은 전투를 구현합니다.

### 애니메이션 시스템

Animancer를 활용한 **MotionSet 타임라인** 구조로, 하나의 액션에 여러 애니메이션 클립을 순차적으로 배치할 수 있습니다.

- `clipStartTime` / `clipEndTime` 으로 클립의 특정 구간만 사용
- `playbackSpeed` 로 클립별 재생 속도 조절
- `MotionEventExecutor`로 타임라인 기반 이벤트 (히트 판정, 이펙트, 사운드 등) 처리
- 상체/하체 레이어 분리 (`AvatarMask`)로 이동 중 상체 액션 재생

### 입력 시스템

Unity Input System 기반으로, `InputLayer` 레벨을 통해 UI와 게임플레이 입력을 분리합니다.

- `InputLayer`를 통한 입력 우선순위 관리 (UI가 열려있을 때 게임 입력 차단)
- 이벤트 기반 등록/해제 (`RegisterInputEvent` / `UnRegisterInputEvent`)
- `InputBuffer`로 입력 선행 입력 지원

<br>

## 📁 프로젝트 폴더 구조

```
Assets/
├── 01.Scenes/              # 씬 파일
├── 02.Scripts/             # 소스 코드
│   ├── Camera/             # 카메라 쉐이크
│   ├── Data/               # SO, Enum, Config, Event 데이터
│   ├── GameActor/          # 핵심 게임 액터 시스템
│   │   ├── Animation/      # Animancer 기반 애니메이터
│   │   ├── Base/           # GameActor 베이스 클래스
│   │   ├── Component/      # 기능별 컴포넌트 (전투, 장비, AI)
│   │   ├── Interface/      # IDamageable, IInteractable
│   │   ├── MovementController/  # KCC 연동 이동 제어
│   │   ├── Object/         # Player, Monster, Projectile 등
│   │   └── State/          # 상태 머신 (Player/Enemy)
│   ├── Input/              # 입력 정의 및 유틸리티
│   ├── Manager/            # 매니저 시스템
│   ├── UI/                 # UI 스크립트
│   └── Util/               # 확장 메서드, 유틸리티
├── 03.Prefabs/             # 프리팹
├── 05.Models/              # 3D 모델
├── 07.Animations/          # 애니메이션 클립
├── 09.InputActions/        # Input Action Asset
├── 10.Datas/               # ScriptableObject 데이터
└── ExternalAssets/         # 외부 에셋 (캐릭터, 환경, FX 등)
```

<br>

## 🎮 구현된 주요 기능

### 플레이어
- 이동 (걷기 / 달리기 / 전력질주)
- 점프 (코요테 타임, 선입력 허용)
- 웅크리기
- 회피 (무적 프레임)
- 대시 (주변 적 탐색 후 돌진)
- 일반 공격 / 강공격 콤보
- 스킬 (4슬롯)
- 가드 / 패링
- 무기 장착 / 해제
- 오브젝트 인터랙션 (채집, 아이템 획득)
- 피격 / 사망 처리 (카메라 쉐이크, 히트스톱, 피격 색상 변경)

### 적 AI
- 플레이어 감지 및 추적
- 거리 기반 전투 의사결정
- 스킬 / 일반 공격 선택
- 가드 / 후퇴 / 원형 배회
- 패트롤 행동
- 전투 스타일별 행동 패턴 (Melee, Ranged, Balanced, Support)

### 전투 시스템
- `IDamageable` 인터페이스 기반 데미지 처리
- 크리티컬 히트, 넉백
- 투사체 시스템 (Linear, AOE)
- 히트스톱 연출 (강도별)
- 카메라 쉐이크

### UI
- HUD (체력바, 전투 정보)
- 인벤토리 / 장비 시스템
- 아이템 획득 알림
- 월드 스페이스 HP 바
- 인터랙션 키 표시
- 일시정지 메뉴

<br>

## ⚙️ 실행 환경

- Unity 6 (6000.3.21f1) 이상
- Universal Render Pipeline (URP)

<br>

## 📜 라이선스
사용된 외부 에셋은 각 에셋의 라이선스를 따릅니다.

에셋은 별도 private 저장소에 관리합니다.
