# AGENTS.md

This file provides guidance to Codex (Codex.ai/code) when working with code in this repository.

## 프로젝트 개요

Unity 6 (6000.0.60f1) 기반 싱글플레이 TPS 액션 게임. 1인 개발. URP 렌더링 파이프라인.
Bokusei는 기본 고정 플레이어블 캐릭터이며, 나머지는 `CharacterActorType`에 정의된 타입을 플레이어블 대상으로 확장할 수 있는 구조.

**사이클형 보스 헌팅** 구조: 시드 기반 런(개발 20분/정식 40분)에서 외곽 보스 3 + 중앙 보스 1을 배치하고, 중앙 보스 처치 후 포털 정산으로 사이클을 마친다(스펙: `Assets/docs/cycle/`). 사이클 보스 영입은 파티 합류가 아니라 `BossAssist`로 처리한다. 이 흐름과 별개로 `MonsterActor._recruitableAs`가 지정된 몬스터는 사망 시 `Svc.Party?.UnlockCharacter`로 플레이어블 캐릭터를 해금한다. 사이클 보스에서 파티 해금을 원하지 않으면 `_recruitableAs`를 `None`으로 유지한다.

**핵심 플러그인:** Animancer Pro V8, Kinematic Character Controller (KCC), MagicaCloth2, Addressables, lilToon.

## 언어

한국어 프로젝트. 코드 주석, 커밋 메시지, 문서 모두 한국어. 사용자가 한국어로 작성하면 한국어로 응답할 것.

## 빌드 & 실행

Unity 프로젝트이므로 최종 빌드와 Play Mode 검증은 Unity 6 (6000.0.60f1+)에서 URP로 수행한다. 생성된 `.csproj`가 최신이면 `dotnet build <프로젝트>.csproj --no-restore`로 asmdef별 컴파일을 보조 확인할 수 있다. Ability 시스템에는 EditMode 14개와 PlayMode 수직 슬라이스 2개의 자동 테스트가 있으며, 파티 Core에는 `Assets/Tests/EditMode/PartyRosterServiceTests.cs`의 테스트 3개가 있다.

## 아키텍처

### 모듈화 구조 — Codex 지속 메모리

asmdef 모듈화 작업은 Phase 5 UI 모듈화와 Phase 6 자동 검증을 완료했고, Phase 7 카메라 이식 경계 구현까지 반영되었다. 카메라 변경의 Play Mode 스모크 검증은 아직 남아 있다. 이후 구조 관련 작업을 시작하기 전에 반드시 다음 문서를 기준으로 삼을 것.

- 상세 온보딩: `Assets/docs/onboarding/ASMDEF_MODULARIZATION_ONBOARDING.html`
- 프로젝트 온보딩: `Assets/docs/onboarding/PROJECT_ONBOARDING_GUIDE.html`
- 작업 이력과 체크포인트: `Assets/docs/Complete/ASMDEF_MODULARIZATION_PLAN.md`
- 카메라 이식 가이드: `Assets/docs/guide/CAMERA_MODULE_PORTABILITY_GUIDE.md`
- 최신 프로젝트 요약: `CLAUDE.md`

현재 런타임 asmdef 경계:

- `UPlayGround.Core` — 공통 기반
- `UPlayGround.Data` — ScriptableObject, DTO, enum 등 순수 데이터
- `UPlayGround.Contracts` — `IGameService`, `Services`, `Svc`, 공용 서비스 계약
- `UPlayGround.Ability.Core` — 프로젝트 타입을 참조하지 않는 Ability 실행 상태·정책·Port·쿨다운·Effect 스택 코어
- `UPlayGround.Ability.UPlayGround` — MotionSet과 플레이어 전투 Payload를 Core에 연결하는 프로젝트 어댑터
- `UPlayGround.Camera` — 카메라 런타임
- `UPlayGround.Actor` — GameActor, 상태, 전투, AI, MotionEvent 런타임
- `UPlayGround.UI` — UI 런타임과 UI 소비자 계약(`UISvc`)

이후 작업의 필수 규칙:

1. 하위 모듈에 구체 `SomeManager.Instance` 의존을 새로 추가하지 않는다. Actor와 공용 소비자는 `Svc`/`ActorSvc`, UI는 `UISvc` 또는 소비자 소유 인터페이스를 사용한다.
2. Camera 모듈 내부에서는 `Svc.*`, `IWorldActor`, 구체 설정/전투 서비스를 직접 참조하지 않는다. 외부 연결은 `Camera/Integration/CameraRuntimeServices.cs`의 `ICameraRuntimeAdapter`를 통하고, 프로젝트 구현은 `Manager/Camera/UPlayGroundCameraRuntimeAdapter.cs`에 둔다. `GameManager`는 `CameraManager`보다 먼저 어댑터를 설정하고 종료 시 해제한다.
3. Actor → `CameraManager.Instance`는 현재 asmdef상 의도된 예외지만, 새 기능에서도 바로 직접 호출하지 말고 기존 카메라 계약이나 어댑터로 표현 가능한지 먼저 검토한다.
4. `GameManager.RegisterManager`가 매니저가 구현한 `IGameService` 계약을 `Services`에 자동 등록한다. `Services.Get<T>()`은 자동 생성하지 않으며, 등록 전 접근은 null과 최초 1회 경고를 만든다.
5. Data 모듈은 Manager, Actor, Camera, UI 구현을 참조하지 않는다.
6. `UnityEditor` 사용 코드는 모듈별 `Editor` asmdef 또는 `Assets/02.Scripts/Editor/`에 둔다. 런타임 asmdef에 Editor 코드가 들어가면 안 된다.
7. 스크립트 물리 이동 시 `.meta`를 함께 이동하여 MonoScript GUID와 프리팹 연결을 보존한다.
8. `[SerializeReference]` 기반 MotionEvent/Ultimate 타입을 이동할 때 `[MovedFrom(true, sourceAssembly: "이전 어셈블리")]`를 반드시 적용한다. 이 규칙을 어기면 이벤트와 VFX 참조가 유실될 수 있다.
9. MotionSet/Ultimate/프리팹 오류가 있는 상태에서 에셋을 저장하거나 일괄 재직렬화하지 않는다. 먼저 컴파일과 타입 매핑을 복구하고 managed reference 및 VFX null 검사를 수행한다.
10. 모듈 변경 완료 조건은 Unity 컴파일 오류 0, 런타임의 무가드 `UnityEditor` 참조 0, UI의 신규 Manager 싱글톤 참조 0, Camera의 금지된 프로젝트 의존 0, Missing Script 0, managed reference/VFX 누락 0, Play Mode 서비스 경고·예외 0, Player Build 오류 0이다.
11. 검증 과정에서 `Assets/10.Datas/` 또는 `Assets/03.Prefabs/`가 자동 변경되면 diff를 반드시 검사한다. 검증이 만든 자동 재직렬화 변경만 원복하고 사용자 데이터 변경은 보존한다.
12. 사용자가 요청하지 않는 한 커밋하지 않는다.

2026-07-17 Phase 6 자동 검증 기준: UI/Player 프리팹 63개 Missing Script 0, MotionSet/Ultimate 에셋 1,156개 managed reference 1,638개 중 누락 0, VFX 참조 168개 중 누락 0, StandaloneWindows64 Development Boot 빌드 오류 0. 이후 Phase 7 카메라 경계 변경은 Camera/Actor/UI/Assembly-CSharp CLI 컴파일 오류 0까지 확인했으며, Lock-on·전투 카메라·KillCam·대화·스냅샷의 Play Mode 스모크와 Player Build 재검증은 남아 있다.

### 매니저 시스템

`GameManager`가 최상위 싱글톤으로 모든 서브 매니저를 순차 초기화. 모든 매니저는 `BaseManager<T>`(제네릭 싱글톤)를 상속하고 `IManager` 인터페이스를 구현. 생명주기: `Init → AfterInit → OnUpdate/OnFixedUpdate/OnLateUpdate → Dispose → OnSceneChanged`.

매니저 목록(`GameManager` 등록 순): SaveManager, InputManager, AssetManager, SettingsManager, SoundManager, UIManager, CameraManager, GameObjectManager, PartyManager, ItemManager, InventoryManager, EventManager, GameCombatManager, GlobalFlagManager, DialogueManager, StoryManager, GameTimeManager, WorldStateManager, ActorSpawnManager, CycleRunManager, BossAssistManager, CycleRemainsManager, CycleTelemetrySession, AgentTickManager, SceneManager, InteractionRespawnManager, MonsterRespawnManager, WorldLightingManager, DebugGizmoManager(에디터 전용), CheatManager, RecipeManager, QuestManager, GameGuideManager.

히트스톱·바이탈오브·방어성공 피드백·레벨업 피드백은 별도 매니저가 아니라 `GameCombatManager` 산하 핸들러(`Manager/Handler/Combat/`)다.

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
- 모션 워프는 별도 컴포넌트: `MotionWarpController.cs` + `MotionWarpTypes.cs`.

### 애니메이션 시스템

Animancer 기반 **MotionSet 타임라인** 구조 — 하나의 액션에 여러 애니메이션 클립을 순차 체이닝. `MotionEventExecutor`가 타임라인 기반 이벤트(히트박스 활성화, VFX, SFX) 발화. `AvatarMask`를 통한 상체/하체 레이어 분리 지원.

### 입력 시스템

Unity Input System 기반, 우선순위 `InputLayer` 레벨 사용 (HUD=0, Scene=1000, Popup=2000, System=3000, Top=10000). `InputBuffer`로 선입력 지원. 이벤트 기반 등록/해제: `RegisterInputEvent`/`UnRegisterInputEvent`.

### Gameplay Ability 시스템

플레이어 공격·스킬 데이터의 단일 소스는 `AbilitySetSO`다. 런타임 연결은 다음 경로를 따른다.

```text
CharacterModelData.abilitySet
→ ActorAbilitySystem / PlayerCombatAbilityDataView
→ GameplayAbilitySO.Variant
→ UPlayGroundMotionAbilityPayloadSO
→ AnimKey + PlayerAttackInfo
```

- `GameplayAbilitySO`는 활성화 조건, 비용, 쿨다운, Variant 선택 정책을 소유한다.
- `UPlayGroundMotionAbilityPayloadSO`는 실제 실행에 필요한 `AnimKey`와 `PlayerAttackInfo`를 소유한다.
- `PlayerCombat`과 밸런스·검증 도구는 `PlayerCombatAbilityDataView`를 통해 같은 `AbilitySetSO`를 읽는다.
- `PlayerSkillSlot`은 입력 슬롯 바인딩이며 공격 수치의 원본이 아니다.
- 제거된 `PlayerAttackDataSO`, Variant V1 직접 실행 필드, 레거시 Resolver/폴백, 일회성 마이그레이션 도구를 다시 도입하지 않는다.
- `EnemyAttackDataSO`는 몬스터 공격 데이터에만 사용한다. MotionSet 기반 공격 데이터 생성기도 현재 적 데이터 전용이다.
- 생성된 플레이어 데이터는 `Assets/10.Datas/Ability/Migrated/` 아래에 있다. 편집·전체 검증은 UI Toolkit 기반 Ability Editor를 사용한다.
- `UPlayGround.Ability.Core` 자체는 프로젝트 비의존 경계를 갖지만, `GameplayAbilitySO`/`GameplayEffectSO`/`AbilitySetSO` 정의와 Effect 수명주기 일부가 아직 Data/Actor에 있으므로 전체 시스템을 외부 재사용 가능한 독립 패키지로 간주하지 않는다.

2026-07-18 기준 데이터는 AbilitySet 8개, GameplayAbility 에셋 210개, Variant/Payload 221개다. Unity Test Runner 실제 실행 결과는 EditMode 14/14, PlayMode 2/2 통과다. 구조와 후속 독립 모듈 조건은 `Assets/docs/TODO/GAMEPLAY_ABILITY_SYSTEM_SPEC.md`를 기준으로 한다.

### 데이터 아키텍처 (ScriptableObject)

모든 수치 데이터는 `Assets/10.Datas/`의 ScriptableObject로 외부화:
- `EnemyBehaviorSO` — 페이즈 기반 AI 프로필 (HP 임계값에 따른 행동 전환)
- `ActorStatSO` — 액터 스탯 단일 소스 (구 `EnemyStatsSO`는 제거됨)
- `EnemyFlyingSettingsSO`, `PoiseSO`
- `AbilitySetSO`, `GameplayAbilitySO`, `UPlayGroundMotionAbilityPayloadSO` — 플레이어 공격·스킬 정의와 실행 Payload
- `EnemyAttackDataSO` — 몬스터용 다단 `HitPhaseData` 공격 데이터
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
- PlayerActor는 partial class 사용 — 플레이어 동작 수정 시 `GameActor/Object/Player/`의 7개 파일을 함께 확인
- 적 비행 상태는 별도 하위 폴더: `State/Enemy/EnemyFlying/`
- 네임스페이스는 `UPlayGround` 루트 아래에서 폴더 경로를 따른다. 신규 파일은 네임스페이스 필수
- UI 네이밍: `UI_Base` 상속 클래스만 `UI_` 접두사, 그 외 UI 보조 클래스는 `UIXxx`
- 대형 클래스(GameManager, InputManager, GameObjectManager, CheatManager 등)는 `클래스명.기능.cs` partial 분리 패턴 사용
