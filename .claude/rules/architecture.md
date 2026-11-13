---
paths:
  - "Assets/02.Scripts/**"
  - "Assets/Tests/**"
---

# 런타임 아키텍처와 모듈 경계

코드를 읽거나 고칠 때 적용하는 구조 기준이다. Claude Code는 위 경로 파일에 접근할 때 자동으로 읽고, 다른 Agent는 AGENTS.md 안내를 따라 직접 연다. 구조 상세는 [모듈화 계획](../../Assets/docs/Complete/ASMDEF_MODULARIZATION_PLAN.md)을 본다.

## 어셈블리 모듈

- `UPlayGround.Core` — 공통 기반 코드
- `UPlayGround.Data` — ScriptableObject, DTO, enum 등 순수 데이터
- `UPlayGround.Contracts` — `IGameService`, `Services`, `Svc`, 공용 서비스 계약
- `UPlayGround.Ability.Core` — 프로젝트 타입을 참조하지 않는 Ability 실행 상태·정책·Port·쿨다운·Effect 스택 코어
- `UPlayGround.Ability.UPlayGround` — MotionSet과 플레이어 전투 Payload를 Core에 연결하는 프로젝트 어댑터
- `UPlayGround.Camera` — 카메라 런타임 (`UPlayGround.Camera.Rendering` — 가림 Fade 렌더 Feature)
- `UPlayGround.Actor` — GameActor, 상태, 전투, AI, MotionEvent 런타임
- `UPlayGround.UI` — UI 런타임과 UI 소비자 계약(`UISvc`)
- `UPlayGround.FlowGraph` — 게임 흐름 노드 그래프 런타임 (`FlowGraphSO`/`FlowNode`/`FlowGraphRunner`/`FlowGraphManager`). `Svc`/Contracts만 참조하며, 노드는 `[SerializeReference]` 다형 + 단일 에셋 방식 (스펙: [node-flow-graph-system.md](../../Assets/docs/Complete/node-flow-graph-system.md))
- `UPlayGround.Data.Editor`, `UPlayGround.GameActor.Editor`, `UPlayGround.UI.Editor`, `UPlayGround.FlowGraph.Editor` — Editor 전용 코드

## 모듈 작업 필수 규칙

1. 하위 모듈에 구체 `SomeManager.Instance` 의존을 새로 추가하지 않는다. Actor와 공용 소비자는 `Contracts`의 `Svc`/`ActorSvc`, UI는 `UISvc` 또는 소비자 모듈 소유 계약을 사용한다.
2. `GameManager.RegisterManager`가 매니저가 구현한 `IGameService` 계약을 `Services`에 자동 등록한다. `Services.Get<T>()`는 자동 생성하지 않으며 등록되지 않은 계약을 최초 1회 경고하므로, 초기화 전 `Awake` 접근 경고가 발생하면 지연 조회 또는 초기화 순서를 수정한다.
3. Data 모듈은 Manager, Actor, Camera, UI 구현을 참조하지 않는다.
4. `UnityEditor` 사용 코드는 모듈별 `Editor` asmdef 또는 `Assets/02.Scripts/Editor/`에 둔다. 런타임 asmdef에 Editor 코드가 들어가면 안 된다.
5. 스크립트 물리 이동 시 `.meta`를 함께 이동하여 MonoScript GUID와 프리팹 연결을 보존한다.
6. `[SerializeReference]` 기반 MotionEvent/Ultimate 이벤트 클래스를 다른 어셈블리로 이동할 때는 반드시 `[MovedFrom(true, sourceAssembly: "이전 어셈블리")]`를 유지한다. 누락하면 에셋의 이벤트와 VFX 참조가 역직렬화되지 않는다.
7. MotionSet/Ultimate/프리팹 오류가 있는 상태에서 에셋을 저장하거나 일괄 재직렬화하지 않는다. 먼저 컴파일과 타입 매핑을 복구하고 managed reference 및 VFX null 검사를 수행한다.
8. 모듈 변경의 검증 범위와 Player Build 실행 시점은 [테스트 작업 지침](../../Assets/docs/guide/TEST_VALIDATION_GUIDE.md)을 따른다. 해당 범위에서 Unity 컴파일 오류 0, 런타임의 무가드 `UnityEditor` 참조 0, UI의 신규 Manager 싱글톤 참조 0, Camera의 금지된 프로젝트 의존 0, Missing Script 0, managed reference/VFX 누락 0, Play Mode 서비스 경고·예외 0을 확인한다. 모듈 내부의 일반 수정만으로 전체 빌드를 요구하지 않는다.

## Camera 경계와 가림 처리

Camera 모듈은 이식 가능한 런타임 경계를 위해 내부에서 `Svc.*`, `IWorldActor`, 구체 설정/전투 서비스를 직접 사용하지 않는다. 외부 연결은 `Camera/Integration/CameraRuntimeServices.cs`의 `ICameraRuntimeAdapter`를 통하고, UPlayground 구현은 `Manager/Camera/UPlayGroundCameraRuntimeAdapter.cs`에 둔다. `GameManager`는 `CameraManager` 초기화 전에 어댑터를 설정하고 종료 시 초기화 상태를 해제한다. Actor에서 기존 `CameraManager.Instance`를 사용하는 경로는 의도된 asmdef 예외지만, 새 기능은 먼저 기존 카메라 계약이나 어댑터로 표현할 수 있는지 검토한다. 이식 절차는 [카메라 이식 가이드](../../Assets/docs/guide/CAMERA_MODULE_PORTABILITY_GUIDE.md)를 따른다.

카메라 가림은 Solid(암을 접음)와 Fade(통과 후 디더로 흐림)로 나뉜다. Fade는 `CameraOcclusionFader`가 프록시 렌더러를 만들고 `CameraOccluderFadeFeature`가 스텐실 디더로 그린다. 새 환경 오브젝트는 기본이 Fade다. Solid·'지붕 제공' 지정의 원본은 이름 규칙 에셋 `Assets/10.Datas/Camera/CameraOccluderNameRules.asset`이며 프리팹·씬에 `CameraOccluder`를 심지 않는다. 렌더러 설정과 배치 체크리스트는 [카메라 가림 가이드](../../Assets/docs/guide/CAMERA_OCCLUSION_FADE_GUIDE.md)를 따른다.

## 매니저 시스템

`GameManager`가 최상위 싱글톤으로 모든 서브 매니저를 순차 초기화한다. 모든 매니저는 `BaseManager<T>`(제네릭 싱글톤)를 상속하고 `IManager`를 구현한다. 생명주기: `Init → AfterInit → OnUpdate/OnFixedUpdate/OnLateUpdate → Dispose → OnSceneChanged`.

등록 순: SaveManager, InputManager, AssetManager, SettingsManager, SoundManager, UIManager, CameraManager, GameObjectManager, PartyManager, ActorSimulationManager, ItemManager, InventoryManager, EventManager, GameCombatManager, GlobalFlagManager, DialogueManager, StoryManager, GameTimeManager, WorldStateManager, ActorSpawnManager, AgentTickManager, SceneManager, InteractionRespawnManager, MonsterRespawnManager, WorldLightingManager, DebugGizmoManager(에디터 전용), CheatManager, RecipeManager, QuestManager, FlowGraphManager, GameGuideManager.

히트스톱·바이탈오브·방어성공 피드백·레벨업 피드백은 별도 매니저가 아니라 `GameCombatManager` 산하 핸들러(`Manager/Handler/Combat/`)다.

## GameActor 계층

- `PlayerActor` — `GameActor/Object/Player/`에 partial 8파일 (base / Lifecycle / Input / Components / Combat / AnimationEvents / Dialogue / Weight). `IDamageable`. 플레이어 동작 수정 시 partial 파일을 함께 확인한다.
- `MonsterActor` — 적 엔티티. `IDamageable`.
- `NpcActor` — `GameActor/Object/Npc/`. `IInteractable`.
- `GatheringActor`, `ItemActor`, 투사체 (`BaseProjectile` → `LinearProjectile` / `AOEProjectile`).
- `VitalOrbActor`는 `GameActor`가 아닌 별도 `MonoBehaviour`다.

## 상태 머신

상태는 KCC의 `ICharacterController`와 결합한다. 각 상태가 `UpdateVelocity`, `UpdateRotation` 등 KCC 콜백을 오버라이드해 상태별 물리 동작을 제어한다.

```
GameActorState (추상)
├── PlayerActorState → 23개 (Idle, GroundMove, Airborn, Attack, Charge, Dash, DashAttack, JumpAttack, JumpDashAttack, FinishAttack, SpecialBreakAttack, Dodge, Guard, GuardBreak, Crouching, Hit, Stun, Knockdown, Grabbed, Death, Interaction, Stop, TurnInPlace)
├── EnemyActorState → 지상·특수 21개 + 비행 9개 (State/Enemy/EnemyFlying/)
└── NpcActorState → Idle, Talk, Wander
```

주요 메서드: `OnEnter`, `OnExit`, `UpdateState`, `CanTransitionState`, `UpdateVelocity`, `UpdateRotation`, `BeforeCharacterUpdate`, `AfterCharacterUpdate`, `PostGroundingUpdate`.

## 컴포넌트·이동·애니메이션·입력

- `ActorComponent` → `PlayerActorComponent`. 전투 `PlayerCombat`(partial 6파일: base / Attack / HitDetection / Combo / Finish / Gizmo)·`EnemyCombat`, AI `EnemyAIController`(`BehaviorTreeRunner`가 `BehaviorTreeAsset` 실행)·`EnemyDetection`·`EnemyTacticalMemory`, 스탯 `PoiseStat`, 장비/파티 `PlayerEquipment`·`PlayerSkillGauge`·`PlayerSwapBehaviour`·`CharacterModelData`, VFX `ActorColorChanger`·`DissolveController`, IK `FootIKController`.
- `ActorMovementController`가 KCC `ICharacterController`를 구현하고 상태 머신을 호스팅해 콜백을 현재 상태에 위임한다 (`Player`/`Enemy`/`NpcMovementController`). 모션 워프는 같은 폴더의 `MotionWarpController.cs` + `MotionWarpTypes.cs`.
- Animancer 기반 **MotionSet 타임라인** — 한 액션에 여러 클립을 순차 체이닝하고 `MotionEventExecutor`가 히트박스·VFX·SFX 이벤트를 발화한다. `AvatarMask`로 상하체 레이어 분리.
- Unity Input System, `InputLayer` 우선순위 (HUD=0, Scene=1000, Popup=2000, System=3000, Top=10000). `InputBuffer` 선입력, `RegisterInputEvent`/`UnRegisterInputEvent`.

## 데이터(ScriptableObject)

모든 수치 데이터는 `Assets/10.Datas/`의 SO로 외부화한다.

- `EnemyBehaviorSO` — 페이즈 기반 AI 프로필 / `ActorStatSO` — 액터 스탯 단일 소스(구 `EnemyStatsSO` 제거) / `EnemyFlyingSettingsSO`, `PoiseSO`
- `AbilitySetSO`, `GameplayAbilitySO`, `UPlayGroundMotionAbilityPayloadSO` — 플레이어·몬스터 공용 공격·스킬 정의와 실행 Payload (상세: [gameplay-ability.md](gameplay-ability.md))
- `PartyConfigSO` — 출전 상한·스왑·협주 게이지·성장/스킬트리·패시브 DB 등 파티 공통 설정
- `PlayerCharacterDefinitionSO` — 캐릭터별 무기·AbilitySet·모델 주소 (`PlayerCharacterCatalogSO`가 Addressable 주소 보관). 새 게임 시작 후보는 `CharacterSelectDatabaseSO`
- `CameraShakeData`, 카메라 이펙트 SO, `MotionSetAsset`

## 주요 Enum

- `ActorType` [Flags] — Player, Monster, Obstacle, NPC, Combat, Talkable
- `CharacterActorType` — Raon, Hwarin, Reine, Lian, SeolA, Sera, YeonHoa, Yura, MyoRyeong, Myomyo, Lili, Arin (H09 미사용). 개명 전 이름(Bokusei, Honoka 등)의 세이브는 `CharacterActorTypeUtility.TryParsePersistentName`이 변환한다. 기존 스킬트리 노드 ID(`Bokusei.*`, `Honoka.*` 등)는 저장 식별자이므로 바꾸지 않는다
- `AttackReactionType` — Light, Hit, Heavy, KnockBack, Stun, Pull, Airborne, Knockdown, Grab
- `EnemyCombatStyle` — Melee, Ranged, Balanced, Support

## 기존 자동 테스트

Ability EditMode 14개·PlayMode 수직 슬라이스 2개, 파티 Core `Assets/Tests/EditMode/PartyRosterServiceTests.cs` 3개, FlowGraph EditMode 3개(`Assets/Tests/EditMode/FlowGraph/`)·PlayMode 3개(`Assets/Tests/PlayMode/FlowGraph/`). 생성된 `.csproj`가 최신이면 `dotnet build <프로젝트>.csproj --no-restore`로 asmdef별 컴파일을 보조 확인할 수 있다.
