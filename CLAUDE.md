# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

Unity 6 (6000.0.60f1) 기반 싱글플레이 TPS 액션 게임. 1인 개발. URP 렌더링 파이프라인.
**사이클형 보스 헌팅** 구조: 시드 기반 런(개발 20분/정식 40분)에서 외곽 보스 3 + 중앙 보스 1을 배치하고, 중앙 보스 처치 후 포털 정산으로 사이클을 마친다 (스펙: `Assets/docs/cycle/`). 사이클 보스 영입은 파티 합류가 아니라 **BossAssist**(장착 1마리, 지정 스킬 1회, 비이동·비어그로 서포트 소환)로 처리한다. 매핑은 `BossAssistDatabaseSO.sourceBossActorId`, 확률 롤은 `BossRecruitmentService`가 담당한다. 이 흐름과 별개로 `MonsterActor._recruitableAs`가 지정된 몬스터는 사망 시 `Svc.Party?.UnlockCharacter`로 플레이어블 캐릭터를 해금하는 기존 경로가 현재도 유효하다. 사이클 보스에서 파티 해금을 원하지 않으면 `_recruitableAs`를 `None`으로 유지한다.

**핵심 플러그인:** Animancer Pro V8, Kinematic Character Controller (KCC), MagicaCloth2, Addressables, lilToon.

## 언어

한국어 프로젝트. 코드 주석, 커밋 메시지, 문서 모두 한국어. 사용자가 한국어로 작성하면 한국어로 응답할 것.

## 빌드 & 실행

Unity 프로젝트이므로 최종 빌드와 Play Mode 검증은 Unity 6 (6000.0.60f1+)에서 URP로 수행한다. 생성된 `.csproj`가 최신이면 `dotnet build <프로젝트>.csproj --no-restore`로 asmdef별 컴파일을 보조 확인할 수 있다. 광범위한 자동 게임플레이 테스트 스위트는 없지만, `Assets/Tests/EditMode/PartyRosterServiceTests.cs`에 Core EditMode 테스트 3개가 있다.

## 아키텍처

### 어셈블리 모듈

런타임 코드는 다음 asmdef 경계로 분리되어 있다. 하위 모듈에서 구체 매니저 싱글톤을 새로 참조하지 않는다. Actor와 공용 소비자는 `Contracts`의 `Svc`/`ActorSvc`, UI는 `UISvc` 또는 소비자 모듈 소유 계약을 사용하며, Camera는 아래의 전용 런타임 어댑터 경계를 사용한다.

- `UPlayGround.Core` — 공통 기반 코드
- `UPlayGround.Data` — ScriptableObject, DTO, enum 등 순수 데이터
- `UPlayGround.Contracts` — `IGameService`, `Services`, `Svc`, 공용 서비스 계약
- `UPlayGround.Camera` — 카메라 런타임
- `UPlayGround.Actor` — GameActor, 상태, 전투, AI, MotionEvent 런타임
- `UPlayGround.UI` — UI 런타임과 UI 소비자 계약(`UISvc`)
- `UPlayGround.Data.Editor`, `UPlayGround.GameActor.Editor`, `UPlayGround.UI.Editor` — Editor 전용 코드

모듈 경계의 상세 기준은 `Assets/docs/onboarding/PROJECT_ONBOARDING_GUIDE.html`,
`Assets/docs/onboarding/ASMDEF_MODULARIZATION_ONBOARDING.html`,
`Assets/docs/Complete/ASMDEF_MODULARIZATION_PLAN.md`를 함께 확인한다.

`GameManager.RegisterManager`가 매니저가 구현한 `IGameService` 계약을 `Services`에 자동 등록한다. `Services.Get<T>()`는 등록되지 않은 계약을 최초 1회 경고하므로, 초기화 전 `Awake` 접근 경고가 발생하면 지연 조회 또는 초기화 순서를 수정한다.

Camera 모듈은 이식 가능한 런타임 경계를 위해 내부에서 `Svc.*`, `IWorldActor`, 구체 설정/전투 서비스를
직접 사용하지 않는다. 외부 연결은 `Camera/Integration/CameraRuntimeServices.cs`의
`ICameraRuntimeAdapter`를 통하고, UPlayground 구현은
`Manager/Camera/UPlayGroundCameraRuntimeAdapter.cs`에 둔다. `GameManager`는
`CameraManager` 초기화 전에 어댑터를 설정하고 종료 시 초기화 상태를 해제한다. Actor에서 기존
`CameraManager.Instance`를 사용하는 경로는 의도된 asmdef 예외지만, 새 기능은 먼저 기존 카메라
계약이나 어댑터로 표현할 수 있는지 검토한다. 새 프로젝트 이식 절차는
`Assets/docs/guide/CAMERA_MODULE_PORTABILITY_GUIDE.md`를 기준으로 한다.

`[SerializeReference]` 기반 MotionEvent/Ultimate 이벤트 클래스를 다른 어셈블리로 이동할 때는 반드시 `[MovedFrom(true, sourceAssembly: "이전 어셈블리")]`를 유지해야 한다. 누락하면 에셋의 이벤트와 VFX 참조가 역직렬화되지 않는다.

### 매니저 시스템

`GameManager`가 최상위 싱글톤으로 모든 서브 매니저를 순차 초기화. 모든 매니저는 `BaseManager<T>`(제네릭 싱글톤)를 상속하고 `IManager` 인터페이스를 구현. 생명주기: `Init → AfterInit → OnUpdate/OnFixedUpdate/OnLateUpdate → Dispose → OnSceneChanged`.

매니저 목록 (GameManager 등록 순): SaveManager, InputManager, AssetManager, SettingsManager, SoundManager, UIManager, CameraManager, GameObjectManager, PartyManager, ItemManager, InventoryManager, EventManager, GameCombatManager, GlobalFlagManager, DialogueManager, StoryManager, GameTimeManager, WorldStateManager, ActorSpawnManager, CycleRunManager, BossAssistManager, CycleRemainsManager, CycleTelemetrySession, AgentTickManager, SceneManager, InteractionRespawnManager, MonsterRespawnManager, WorldLightingManager, DebugGizmoManager(에디터 전용), CheatManager, RecipeManager, QuestManager, GameGuideManager.

히트스톱·바이탈오브·방어성공 피드백·레벨업 피드백은 별도 매니저가 아니라 `GameCombatManager` 산하 핸들러(`Manager/Handler/Combat/`)로 재편됨.

### 사이클과 보스 어시스트

`CycleRunManager`가 시드 기반 런 상태를 관리하고, `BossAssistManager`가 영입된 보스 어시스트의 장착·소환을 관리한다. `CycleRemainsManager`는 런 잔여물/정산 흐름을, `CycleTelemetrySession`은 사이클 텔레메트리를 담당한다. 사이클 보스의 `BossAssist` 영입과 `MonsterActor._recruitableAs` 기반 파티 캐릭터 해금은 서로 다른 경로이므로 혼동하지 않는다.

### GameActor 계층 구조

`GameActor`(추상 MonoBehaviour)가 액터 계층의 베이스:
- `PlayerActor` — `GameActor/Object/Player/`에 partial 7파일 분리 (base / Lifecycle / Input / Components / Combat / AnimationEvents / CycleWeight). `IDamageable` 구현.
- `MonsterActor` — 적 엔티티. `IDamageable` 구현.
- `NpcActor` — `GameActor/Object/Npc/`. `IInteractable` 구현.
- `GatheringActor`, `ItemActor`, 투사체 (`BaseProjectile` → `LinearProjectile` / `AOEProjectile`).
- `VitalOrbActor`는 `GameActor`가 아닌 별도 `MonoBehaviour`이다.

### 상태 머신 (핵심 패턴)

상태는 KCC의 `ICharacterController`와 긴밀히 결합. 각 상태가 `UpdateVelocity`, `UpdateRotation` 등 KCC 콜백을 오버라이드하여 상태별 물리 동작을 제어.

```
GameActorState (추상)
├── PlayerActorState → 23개 구체 상태 (Idle, GroundMove, Airborn, Attack, Charge, Dash, DashAttack, JumpAttack, JumpDashAttack, FinishAttack, SpecialBreakAttack, Dodge, Guard, GuardBreak, Crouching, Hit, Stun, Knockdown, Grabbed, Death, Interaction, Stop, TurnInPlace)
├── EnemyActorState → 지상·특수 상태 21개 + 비행 상태 9개 (State/Enemy/EnemyFlying/)
└── NpcActorState → Idle, Talk, Wander
```

주요 메서드: `OnEnter`, `OnExit`, `UpdateState`, `CanTransitionState(string stateName)`, `UpdateVelocity`, `UpdateRotation`, `BeforeCharacterUpdate`, `AfterCharacterUpdate`, `PostGroundingUpdate`.

### 컴포넌트 시스템

`ActorComponent`(베이스) → `PlayerActorComponent`(플레이어 전용 베이스). GameActor에 기능별 컴포넌트를 조합:

- **전투:** `PlayerCombat`(partial 6파일: base / Attack / HitDetection / Combo / Finish / Gizmo), `EnemyCombat` — 공격 로직, 데미지, 스킬
- **AI:** `EnemyAIController`가 `BehaviorTreeRunner`를 호스팅하고 `BehaviorTreeAsset`을 실행한다. `EnemyDetection`(시야/거리 감지), `EnemyTacticalMemory`, BT Blackboard/Service/Action 노드가 전투 의사결정을 구성한다.
- **스탯:** `PoiseStat`(강인도/경직 저항)
- **장비/파티:** `PlayerEquipment`, `PlayerSkillGauge`, `PlayerSwapBehaviour`, `CharacterModelData`
- **VFX:** `ActorColorChanger`(피격 플래시), `DissolveController`(사망 디졸브)
- **IK:** `FootIKController`

### 이동 컨트롤러

`ActorMovementController`가 KCC의 `ICharacterController`를 구현하고 상태 머신을 호스팅:
- `PlayerMovementController`, `EnemyMovementController`, `NpcMovementController`
- 이동 컨트롤러가 KCC 콜백을 현재 상태에 위임.
- 모션 워프는 별도 컴포넌트: `MotionWarpController.cs` + `MotionWarpTypes.cs` (같은 폴더).

### 애니메이션 시스템

Animancer 기반 **MotionSet 타임라인** 구조 — 하나의 액션에 여러 애니메이션 클립을 순차 체이닝. `MotionEventExecutor`가 타임라인 기반 이벤트(히트박스 활성화, VFX, SFX) 발화. `AvatarMask`를 통한 상체/하체 레이어 분리 지원.

### 입력 시스템

Unity Input System 기반, 우선순위 `InputLayer` 레벨 사용 (HUD=0, Scene=1000, Popup=2000, System=3000, Top=10000). `InputBuffer`로 선입력 지원. 이벤트 기반 등록/해제: `RegisterInputEvent`/`UnRegisterInputEvent`.

### 데이터 아키텍처 (ScriptableObject)

모든 수치 데이터는 `Assets/10.Datas/`의 ScriptableObject로 외부화:
- `EnemyBehaviorSO` — 페이즈 기반 AI 프로필 (HP 임계값에 따른 행동 전환)
- `ActorStatSO` — 액터 스탯 단일 소스 (구 `EnemyStatsSO`는 제거됨)
- `EnemyFlyingSettingsSO`, `PoiseSO`
- `PlayerAttackDataSO`, `EnemyAttackDataSO` — 다단 `HitPhaseData`를 포함한 공격 데이터
- `PartyConfigSO` — 시작 파티 순서와 초기 활성 캐릭터 인덱스
- `CameraShakeData`, 카메라 이펙트 SO
- `MotionSetAsset` — 애니메이션 타임라인 정의

### 주요 Enum

- `ActorType` [Flags] — Player, Monster, Obstacle, NPC, Combat, Talkable
- `CharacterActorType` — Bokusei, Honoka, Reine, LianLian, Nenmir, Sera, Inori, Hichi, Siuha, Komoe, Lili, H09
- `AttackReactionType` — Light, Hit, Heavy, KnockBack, Stun, Pull, Airborne, Knockdown, Grab
- `EnemyCombatStyle` — Melee, Ranged, Balanced, Support

## 코드 컨벤션

- 폴더 앞 숫자 접두사 (`01.Scenes`, `02.Scripts` 등)로 Unity Project 창 정렬
- 상태 이름 패턴: `{액터타입}{액션이름}State` (예: `PlayerDashState`, `EnemyChaseState`)
- 컴포넌트 이름 패턴: `{액터타입}{기능}` (예: `PlayerCombat`, `EnemyAIController`)
- 적 비행 상태는 별도 하위 폴더: `State/Enemy/EnemyFlying/`
- 네임스페이스는 `UPlayGround` 루트 아래에서 폴더 경로를 따른다 (예: `Manager/` → `UPlayGround.Manager`). 신규 파일은 네임스페이스 필수
- UI 네이밍: `UI_Base` 상속 클래스만 `UI_` 접두사, 그 외 UI 보조 클래스는 `UIXxx`
- 대형 클래스(GameManager, InputManager, GameObjectManager, CheatManager 등)는 `클래스명.기능.cs` partial 분리 패턴 사용
