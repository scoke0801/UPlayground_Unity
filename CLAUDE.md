# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

Unity 6 (6000.0.60f1) 기반 싱글플레이 TPS 액션 게임. 1인 개발. URP 렌더링 파이프라인.
**사이클형 보스 헌팅** 구조: 시드 기반 런(개발 20분/정식 40분)에서 외곽 보스 3 + 중앙 보스 1을 배치하고, 중앙 보스 처치 후 포털 정산으로 사이클을 마친다 (스펙: `Assets/docs/cycle/`). 처치한 보스는 파티에 합류하지 않고 **BossAssist**(장착 1마리, 지정 스킬 1회, 비이동·비어그로 서포트 소환)로 영입된다 — 매핑은 `BossAssistDatabaseSO.sourceBossActorId`, 확률 롤은 `BossRecruitmentService`. `MonsterActor._recruitableAs`는 재스폰 제외 판정용 네임드 표시로만 남아 있으며 `PartyManager.UnlockCharacter` 자동 호출은 제거됨(치트 전용).

**핵심 플러그인:** Animancer Pro V8, Kinematic Character Controller (KCC), MagicaCloth2, Addressables, lilToon.

## 언어

한국어 프로젝트. 코드 주석, 커밋 메시지, 문서 모두 한국어. 사용자가 한국어로 작성하면 한국어로 응답할 것.

## 빌드 & 실행

Unity 프로젝트이므로 CLI 빌드 명령어 없음. Unity 6 (6000.0.60f1+)에서 URP로 열기. 자동화된 테스트 스위트 없음.

## 아키텍처

### 매니저 시스템

`GameManager`가 최상위 싱글톤으로 모든 서브 매니저를 순차 초기화. 모든 매니저는 `BaseManager<T>`(제네릭 싱글톤)를 상속하고 `IManager` 인터페이스를 구현. 생명주기: `Init → AfterInit → OnUpdate/OnFixedUpdate/OnLateUpdate → Dispose → OnSceneChanged`.

매니저 목록 (GameManager 등록 순): SaveManager, InputManager, AssetManager, SettingsManager, SoundManager, UIManager, CameraManager, GameObjectManager, PartyManager, ItemManager, InventoryManager, EventManager, GameCombatManager, GlobalFlagManager, DialogueManager, StoryManager, GameTimeManager, WorldStateManager, ActorSpawnManager, AgentTickManager, SceneManager, InteractionRespawnManager, MonsterRespawnManager, WorldLightingManager, DebugGizmoManager(에디터 전용), CheatManager, RecipeManager, QuestManager.

히트스톱·바이탈오브·방어성공 피드백·레벨업 피드백은 별도 매니저가 아니라 `GameCombatManager` 산하 핸들러(`Manager/Handler/Combat/`)로 재편됨.

### GameActor 계층 구조

`GameActor`(추상 MonoBehaviour)가 모든 인터랙티브 오브젝트의 베이스:
- `PlayerActor` — `GameActor/Object/Player/`에 partial 6파일 분리 (base / Lifecycle / Input / Components / Combat / AnimationEvents). `IDamageable` 구현.
- `MonsterActor` — 적 엔티티. `IDamageable` 구현.
- `NpcActor` — `GameActor/Object/Npc/`. `IInteractable` 구현.
- `GatheringActor`, `ItemActor`, `VitalOrbActor`, 투사체 (`BaseProjectile` → `LinearProjectile` / `AOEProjectile`).

### 상태 머신 (핵심 패턴)

상태는 KCC의 `ICharacterController`와 긴밀히 결합. 각 상태가 `UpdateVelocity`, `UpdateRotation` 등 KCC 콜백을 오버라이드하여 상태별 물리 동작을 제어.

```
GameActorState (추상)
├── PlayerActorState → 24개 구체 상태 (Idle, GroundMove, Airborn, Attack, Charge, Dash, DashAttack, JumpAttack, JumpDashAttack, FinishAttack, SpecialBreakAttack, Dodge, Guard, GuardBreak, Crouching, Hit, Stun, Knockdown, Grabbed, Death, Interaction, Stop, TurnInPlace 등)
├── EnemyActorState → 13개+ 지상 상태 + 9개 비행 상태 (State/Enemy/EnemyFlying/)
└── NpcActorState → Idle, Talk, Wander
```

주요 메서드: `OnEnter`, `OnExit`, `UpdateState`, `CanTransitionState(string stateName)`, `UpdateVelocity`, `UpdateRotation`, `BeforeCharacterUpdate`, `AfterCharacterUpdate`, `PostGroundingUpdate`.

### 컴포넌트 시스템

`ActorComponent`(베이스) → `PlayerActorComponent`(플레이어 전용 베이스). GameActor에 기능별 컴포넌트를 조합:

- **전투:** `PlayerCombat`(partial 6파일: base / Attack / HitDetection / Combo / Finish / Gizmo), `EnemyCombat` — 공격 로직, 데미지, 스킬
- **AI:** `EnemyBrain`(의사결정), `EnemyFlyingBrain`, `EnemyDetection`(시야/거리 감지), `EnemyTacticalMemory`
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
- `CharacterActorType` — Bokusei, Honoka, Reine, LianLian, Nenmir, Sera, Inori, H09
- `AttackReactionType` — Light, Hit, Heavy, KnockBack, Stun, Pull, Airborne, Knockdown, Grab
- `EnemyCombatStyle` — Melee, Ranged, Balanced, Support

## 코드 컨벤션

- 폴더 앞 숫자 접두사 (`01.Scenes`, `02.Scripts` 등)로 Unity Project 창 정렬
- 상태 이름 패턴: `{액터타입}{액션이름}State` (예: `PlayerDashState`, `EnemyChaseState`)
- 컴포넌트 이름 패턴: `{액터타입}{기능}` (예: `PlayerCombat`, `EnemyBrain`)
- 적 비행 상태는 별도 하위 폴더: `State/Enemy/EnemyFlying/`
- 네임스페이스는 `UPlayGround` 루트 아래에서 폴더 경로를 따른다 (예: `Manager/` → `UPlayGround.Manager`). 신규 파일은 네임스페이스 필수
- UI 네이밍: `UI_Base` 상속 클래스만 `UI_` 접두사, 그 외 UI 보조 클래스는 `UIXxx`
- 대형 클래스(GameManager, InputManager, GameObjectManager, CheatManager 등)는 `클래스명.기능.cs` partial 분리 패턴 사용
