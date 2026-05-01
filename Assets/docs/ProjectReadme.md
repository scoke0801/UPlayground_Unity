# UPlayground — TPS 액션 게임 프로젝트

Unity 6 (6000.0.60f1) 기반 싱글플레이 TPS 액션 게임. 1인 개발. URP 렌더링 파이프라인.

---

## 📋 프로젝트 개요

### 플레이어블 캐릭터 / 파티 확장 구조

Bokusei는 기본 고정 플레이어블 캐릭터이며, 나머지 캐릭터는 `CharacterActorType`을 기준으로 플레이어블 대상으로 확장 가능한 구조를 지향한다.

적 처치 시 `MonsterActor`의 합류 설정(`_recruitableAs`)에 지정된 `CharacterActorType`이 있으면 `PartyManager.UnlockCharacter`를 통해 파티 슬롯에 추가된다. 실제 조작 캐릭터는 단일 `PlayerActor`를 유지하고, `PlayerSwapBehaviour`가 하위 모델(`CharacterModelData`)을 교체한 뒤 `PlayerActor.RefreshForCharacter()`로 캐릭터별 상태를 갱신한다.

| 구분 | 설명 |
|------|------|
| 기본 캐릭터 | `Bokusei` — 게임 시작 기준 고정 플레이어블 |
| 확장 대상 | `CharacterActorType`에 정의된 타입 중 `CharacterModelData`와 데이터가 준비된 캐릭터 |
| 합류 조건 | 처치한 `MonsterActor._recruitableAs`가 `None`이 아닐 때 파티에 합류 |
| 교체 방식 | 단일 `PlayerActor` + 모델 서브루트 활성/비활성 전환 |
| 현재 타입 | `Bokusei`, `Honoka`, `Reine`, `LianLian`, `Nenmir`, `Sera`, `Inori`, `H09` |

### 핵심 플러그인

| 플러그인 | 용도 |
|----------|------|
| **Animancer Pro V8** | 애니메이션 상태 머신 및 MotionSet 타임라인 |
| **Kinematic Character Controller (KCC)** | 물리 기반 캐릭터 이동 |
| **MagicaCloth2** | 천 / 머리카락 시뮬레이션 |
| **Addressables** | 에셋 비동기 로딩 (UI DB, Item DB, Recipe DB 등) |
| **lilToon** | 캐릭터 셰이더 |

---

## 🏗️ 아키텍처 개요

### 매니저 계층

```
GameManager  ─  BaseManager<GameManager> (최상위 싱글톤)
    │
    ├── 초기화 순서 (GameManager.InitializeManagers)
    │
    │  [1]  SaveManager          세이브 / 로드
    │  [2]  InputManager         Unity Input System 래퍼
    │  [3]  SettingsManager      설정 SO 로드 & 반영
    │  [4]  AssetManager         Addressables 핸들 관리
    │  [5]  UIManager            캔버스 레이어 & UI 풀
    │  [6]  CameraManager        카메라 이펙트 & 쉐이크
    │  [7]  GameObjectManager    플레이어 참조 & FX 스폰
    │  [8]  ItemManager          ItemDatabase 로드
    │  [9]  InventoryManager     인벤토리 CRUD
    │  [10] EventManager         타입 안전 이벤트 버스
    │  [11] GameCombatManager    전투 핸들러 호스트 (HitStopHandler, VitalOrbHandler)
    │  [12] GlobalFlagManager    글로벌 플래그 (퀘스트 조건)
    │  [13] DialogueManager      대화 그래프 실행
    │  [14] StoryManager         스토리 진행 관리
    │  [15] GameTimeManager      인게임 시간 흐름
    │  [16] ActorSpawnManager    ActorDatabase 기반 런타임 스폰
    │  [17] PartyManager         파티 구성, 캐릭터 해금, 교체 입력 처리
    │  [18] SceneManager         씬 전환 & 로딩 화면
    │  [19] CheatManager         개발용 치트 콘솔
    │  [20] RecipeManager        제작 레시피 관리
    │  [21] QuestManager         퀘스트 목표 추적 & 보상
```

모든 매니저는 `BaseManager<T>` (MonoBehaviour 싱글톤) 상속 + `IManager` 인터페이스 구현.

**IManager 생명주기:**
```
Init → AfterInit → OnUpdate / OnFixedUpdate / OnLateUpdate → Dispose → OnSceneChanged
```

---

### GameActor 계층

```
GameActor (abstract MonoBehaviour)
├── PlayerActor          — partial class 5개 파일 (base, lifecycle, input, combat, equipment)
│                          IDamageable 구현
├── MonsterActor         — IDamageable 구현
│   └── (확장 가능 — BossMonsterActor 등)
├── NpcActor             — IInteractable 구현
├── GatheringActor       — IInteractable 구현 (채집, 낚시)
├── ItemActor            — 드랍 픽업 오브젝트
├── VitalOrbActor        — 회복 오브
└── BaseProjectile
    ├── LinearProjectile  — 직선 투사체
    └── AOEProjectile     — 범위 투사체
```

---

### 상태 머신

`ActorMovementController`가 `ICharacterController`(KCC)를 구현하고 상태 머신을 호스팅. 이동 컨트롤러가 KCC 콜백을 현재 상태에 위임.

```
GameActorState (추상)
│
├── PlayerActorState  →  19개 구체 상태
│     Idle / GroundMove / Airborn / Stop / TurnInPlace
│     Attack / DashAttack / JumpAttack / FinishAttack / Charge
│     Dash / Dodge / Guard / GuardBreak
│     Crouching / Hit / Death / Interaction / Grabbed
│
├── EnemyActorState  →  15개 지상 상태
│     Idle / Patrol / Chase / Attack / Charge
│     Circle / Flank / Retreat / Counter / Guard
│     Hit / Airborne / Land / Grabbed / Death
│
│     + EnemyFlying 9개 상태 (State/Enemy/EnemyFlying/)
│       TakeOff / Chase / AirCircle / Circle / Retreat
│       GroundAttack / Dive / Land / Patrol
│
└── NpcActorState  →  3개 상태
      Idle / Talk / Wander
```

**주요 상태 메서드:** `OnEnter`, `OnExit`, `UpdateState`, `CanTransitionState(string)`, `UpdateVelocity`, `UpdateRotation`, `BeforeCharacterUpdate`, `AfterCharacterUpdate`, `PostGroundingUpdate`

---

### 이동 컨트롤러

| 클래스 | 대상 |
|--------|------|
| `PlayerMovementController` | 플레이어 |
| `EnemyMovementController`  | 지상 몬스터 |
| `NpcMovementController`    | NPC |

---

### 컴포넌트 시스템

`ActorComponent` → `PlayerActorComponent` (플레이어 전용 베이스)

| 컴포넌트 | 대상 | 역할 |
|----------|------|------|
| `PlayerCombat` | 플레이어 | 공격, 패리, 데미지 처리 |
| `PlayerEquipment` | 플레이어 | 무기 장착 / 교체 |
| `PlayerSkillGauge` | 플레이어 | 스킬 게이지 관리 |
| `PlayerSwapBehaviour` | 플레이어 | 단일 PlayerActor 하위 모델 교체, 캐릭터별 갱신 |
| `CharacterModelData` | 플레이어 | 모델 서브루트와 CharacterActorType 연결 |
| `FootIKController` | 플레이어 | 발 IK |
| `EnemyCombat` | 몬스터 | 공격 로직, 가드 |
| `EnemyBrain` | 지상 몬스터 | AI 의사결정, 페이즈 전환 |
| `EnemyFlyingBrain` | 비행 몬스터 | 비행 AI |
| `EnemyDetection` | 몬스터 | 시야 / 거리 감지 |
| `EnemyTacticalMemory` | 몬스터 | 전술 메모리 |
| `PoiseStat` | 공통 | 강인도 / 경직 저항 |
| `ActorColorChanger` | 공통 | 피격 플래시 |
| `DissolveController` | 공통 | 사망 디졸브 |
| `NpcBrain` | NPC | NPC AI |

---

### 애니메이션 시스템

Animancer 기반 **MotionSet 타임라인** 구조.

```
MotionSetAsset (SO)          애니메이션 타임라인 정의
    └── Motion[]             클립 시퀀스 및 이벤트 타임라인
          └── MotionEvent[]  히트박스 / VFX / SFX / 카메라 이펙트 등

MotionEventExecutor          타임라인 이벤트 발화 실행기
ActorAnimator                Animancer 래퍼 (PlayMotion 등)
PlayerActorAnimator          플레이어 전용 확장

지원 MotionEvent 타입:
  Collision / Particle / PlaySound / FootStep
  CameraEffect / CameraLookAtSocket
  TimeScale / AnimationSpeed
  SpawnProjectile / SpawnSkill
  AddForce / MotionWarp / Loop / ComboWindow
  FinishAttack / FinishSideView / FreezeEnemy
  HealSkill / HideTarget / Invincibility / DisableCollision
```

AvatarMask를 통한 상체/하체 레이어 분리 지원.

---

### 입력 시스템

Unity Input System 기반. 우선순위 레이어 구조.

| InputLayer | 우선순위 값 | 용도 |
|-----------|------------|------|
| `HUD`    | 0     | 인게임 HUD |
| `Scene`  | 1000  | 씬 내 인터랙션 |
| `Popup`  | 2000  | 팝업 UI |
| `System` | 3000  | 시스템 UI |
| `Top`    | 10000 | 최상위 (치트 콘솔 등) |

`InputBuffer`로 선입력 지원. `RegisterInputEvent` / `UnRegisterInputEvent`로 이벤트 등록/해제.

---

### UI 시스템

```
UIManager
├── CanvasLayer.HUD        (SortOrder 0)      — 인게임 HUD
├── CanvasLayer.Scene      (SortOrder 1000)   — 씬 오버레이 UI
├── CanvasLayer.Popup      (SortOrder 2000)   — 팝업 / 인벤토리
├── CanvasLayer.System     (SortOrder 3000)   — 시스템 / 설정
└── CanvasLayer.WorldSpace (SortOrder 10000)  — 월드 스페이스 HP바 등
```

모든 UI는 `UI_Base`를 상속. `UIKeyType` enum으로 ShowUI / 관리. Addressables 기반 `UIPrefabDatabase`에서 프리팹 로드.

---

### 데이터 아키텍처 (ScriptableObject)

`Assets/10.Datas/` 아래 외부화.

| SO | 경로 | 용도 |
|----|------|------|
| `ItemDatabase` | `10.Datas/Item` | 전체 아이템 DB (Addressables 키: `ItemDatabase`) |
| `ActorStatSO` | `10.Datas/Stat` | 액터 공통 전투/생존/이동 배율 스탯 |
| `EnemyStatsSO` | `10.Datas/Actor/Enemy/StatData` | 레거시 몬스터 튜닝 및 `ActorStatSO` 생성 입력 |
| `EnemyBehaviorSO` | — | 페이즈 기반 AI 프로필 |
| `EnemyAttackDataSO` | `10.Datas/Actor/Enemy/AttackData` | 다단 `HitPhaseData` 공격 데이터 |
| `EnemyFlyingSettingsSO` | — | 비행 몬스터 설정 |
| `EnemyDropTableSO` | `10.Datas/Actor/Enemy/DropTables` | 몬스터 드랍 테이블 |
| `PoiseSO` | — | 강인도 데이터 |
| `PlayerAttackDataSO` | — | 플레이어 공격 데이터 |
| `ActorDefinitionSO` | — | 액터 정의 (prefab + 필수 statData + 레거시 stats + npcData + dropTable) |
| `ActorDatabase` | — | 전체 ActorDefinitionSO 조회 테이블 |
| `PartyConfigSO` | — | 시작 파티 순서와 초기 활성 캐릭터 인덱스 |
| `InteractableActorSO` | `10.Datas/Actor/Interaction` | 채집 오브젝트 데이터 |
| `NpcActorSO` | — | NPC 데이터 (InteractableActorSO 상속). `ActorDefinitionSO.npcData`에 연결하면 `NpcActor.SetDefinition()`에서 주입 |
| `MotionSetAsset` | `10.Datas/Actor/Animation` | 애니메이션 타임라인 |
| `CameraShakeData` | — | 카메라 쉐이크 프리셋 |
| `RecipeDatabase` | — | 제작 레시피 DB (Addressables 키: `RecipeDatabase`) |
| `SettingsData` | — | 그래픽 / 오디오 / 키바인딩 설정 |

---

### 주요 Enum

| Enum | 설명 |
|------|------|
| `ActorType` [Flags] | Player, Monster, Obstacle, NPC, Combat, Talkable |
| `CharacterActorType` | None, Bokusei, Honoka, Reine, LianLian, Nenmir, Sera, Inori, H09 |
| `AttackReactionType` | Light, Hit, Heavy, KnockBack, Stun, Pull, Airborne, Knockdown, Grab |
| `MonsterActorGrade` | Normal, Elite, Boss |
| `EnemyCombatStyle` | Melee, Ranged, Balanced, Support |
| `AnimKey` | 애니메이션 클립 키 |
| `UIKeyType` | UI 프리팹 식별 키 |
| `ItemIdType` | 아이템 ID 열거형 |

---

## 📁 프로젝트 구조

```
Assets/
├── 01.Scenes/
│   └── GameLogic/InGame.unity
│
├── 02.Scripts/
│   ├── Manager/                 매니저 20종
│   │   ├── Base/                BaseManager<T>, IManager
│   │   ├── Item/                ItemManager, InventoryManager
│   │   ├── Actor/               ActorSpawnManager
│   │   ├── Party/               PartyManager
│   │   ├── Crafting/            RecipeManager
│   │   ├── Save/                SaveManager, ISaveable
│   │   ├── Cheat/               CheatManager + 에디터 콘솔
│   │   └── ...
│   │
│   ├── GameActor/
│   │   ├── Base/                GameActor.cs
│   │   ├── Object/              PlayerActor, MonsterActor, NpcActor, ItemActor, GatheringActor ...
│   │   ├── State/               상태 머신 (Player 19종, Enemy 15종, NPC 3종, EnemyFlying 9종)
│   │   ├── Component/           ActorComponent 계열 컴포넌트
│   │   ├── MovementController/  KCC 기반 이동 컨트롤러
│   │   ├── Animation/           ActorAnimator, MotionEventExecutor
│   │   └── Group/               MonsterGroupController, GroupSpawnTrigger
│   │
│   ├── UI/                      UI_Base 계열 (HUD, Inventory, Dialogue, Crafting ...)
│   │
│   └── Data/
│       ├── Actor/               ActorDefinitionSO, ActorDatabase, Enemy SO군, AnimationSO군
│       ├── Item/                ItemSO, EquipmentSO, ItemDropList
│       ├── Combat/              EnemyAttackDataSO, PlayerAttackDataSO, VitalOrbDataSO
│       ├── Crafting/            RecipeData, RecipeDatabase
│       ├── Party/               PartyConfigSO
│       ├── Dialogue/            DialogueGraphSO, DialogueNodeSO
│       ├── Camera/              CameraShakeData, CameraSettings
│       └── Enum/                프로젝트 전역 열거형
│
├── 10.Datas/                    ScriptableObject 에셋
│   ├── Actor/                   Enemy 스탯, DropTable, Animation MotionSet
│   └── Item/                    ItemSO 에셋, Equipment, ItemDatabase
│
└── docs/                        시스템 가이드 문서
```

---

## 📖 시스템별 상세 가이드 문서

### 시스템별 상세 가이드 문서

| 문서 | 설명 |
|------|------|
| [ACTOR_ID_SYSTEM_GUIDE.md](ACTOR_ID_SYSTEM_GUIDE.md) | Actor ID 시스템 — 데이터 정의, 런타임 스폰, 에디터 사용법 |
| [CRAFTING_SYSTEM_GUIDE.md](CRAFTING_SYSTEM_GUIDE.md) | 제작(Crafting) 시스템 — 레시피, 재료, 언락 조건 |
| [SAVE_SYSTEM_GUIDE.md](SAVE_SYSTEM_GUIDE.md) | 세이브/로드 시스템 |
| [UI_Base_Guide.md](UI_Base_Guide.md) | UI 베이스 시스템 — 레이어 구조, UI 생성/제거 |
| [GAMEMANAGER_README.md](GAMEMANAGER_README.md) | GameManager — 매니저 등록 및 초기화 순서 |
| [ITEM_DROP_SYSTEM_GUIDE.md](ITEM_DROP_SYSTEM_GUIDE.md) | 아이템 드랍 시스템 — 몬스터/인터랙션 드랍 테이블, 픽업 오브젝트, 에디터 도구 |
| [ITEM_DATA_SYSTEM_GUIDE.md](Complete/ITEM_DATA_SYSTEM_GUIDE.md) | 아이템 데이터 시스템 — ItemSO/EquipmentSO 구조, ItemDatabase 흐름, 데이터 자동 발급기 정의 |
| [QUEST_SYSTEM_GUIDE.md](QUEST_SYSTEM_GUIDE.md) | 퀘스트 시스템 — 목표 추적·보상·Enum 자동생성·에디터 도구 |
| [MINIMAP_SYSTEM_GUIDE.md](MINIMAP_SYSTEM_GUIDE.md) | 미니맵 시스템 — 플레이어·적·퀘스트 마커 표시, 씬 캡처 에디터 |
| [MAP_PLACEMENT_TOOL_GUIDE.md](MAP_PLACEMENT_TOOL_GUIDE.md) | 맵 배치 툴 — 씬 클릭 기반 적·NPC·포탈 프리팹 배치 |
| [ACTOR_MOTION_FALLBACK_GUIDE.md](ACTOR_MOTION_FALLBACK_GUIDE.md) | ActorAnimationMotionSet 공용 모션 — Fallback 체인으로 휴머노이드 클립 공유, 커스텀 인스펙터·Override 워크플로 |
| [ENEMY_LOCOMOTION_GUIDE.md](ENEMY_LOCOMOTION_GUIDE.md) | 몬스터 방향성 로코모션 — EnemyLocomotionHelper 8방향 분기, Walk·WalkSlow·Run 스타일, LocoMotionSetupWindow 클립 등록 |
| [PLAYER_COMBAT_WEAPON_STATE_GUIDE.md](PLAYER_COMBAT_WEAPON_STATE_GUIDE.md) | 플레이어 전투 무기 상태 연동 — 전투 진입/해제 시 무기 장착·해제 처리 설계 |
| [STAT_SYSTEM_GUIDE.md](STAT_SYSTEM_GUIDE.md) | 액터 스탯 시스템 — ActorStatSO, ActorStatContainer, Stat Data Generator 검증 정책 |
| [BEHAVIOR_TREE_IMPROVEMENT_PLAN_GUIDE.md](BEHAVIOR_TREE_IMPROVEMENT_PLAN_GUIDE.md) | Behavior Tree 개선 방안 — Behavior Designer Pro 3 레퍼런스 기반 자체 BT 개선 로드맵 |
| [BEHAVIOR_TREE_REFERENCE_GAP_IMPLEMENTATION_GUIDE.md](BEHAVIOR_TREE_REFERENCE_GAP_IMPLEMENTATION_GUIDE.md) | Behavior Tree 레퍼런스 누락 기능 구현 — Conditional Abort, Runner 제어, Decorator 확장, 디버그 기능 보강 계획 |
| [GAMEPLAY_TAG_SYSTEM_GUIDE.md](GAMEPLAY_TAG_SYSTEM_GUIDE.md) | GameplayTag 시스템 — 계층형 태그, GameplayTagRegistrySO + 자동 enum 생성, GameplayTagContainer 런타임 부착, 상태 머신 통합 |
| [EVENT_MANAGER_GUIDE.md](EVENT_MANAGER_GUIDE.md) | EventManager 타입 안전 이벤트 버스 — enum + IEventData 페어, 데이터/무데이터 오버로드, 씬 전환 자동 정리, 디버그 헬퍼 |
| [GAMEOBJECT_MANAGER_GUIDE.md](GAMEOBJECT_MANAGER_GUIDE.md) | GameObjectManager — 활성 플레이어 참조, 액터 레지스트리, FX/Item/Weapon 스폰, InteractionHandler, 글로벌 타임스케일 |
| [DIALOGUE_SYSTEM_GUIDE.md](DIALOGUE_SYSTEM_GUIDE.md) | Dialogue 시스템 — DialogueGraphSO/NodeSO, Main/System/Monologue 채널 Runner, ConditionSO/ActionSO 확장, GlobalFlagManager 세이브 연동 |
| [STORY_SYSTEM_GUIDE.md](STORY_SYSTEM_GUIDE.md) | Story 시스템 — 진행도 단조 증가, storyId 1회 트리거, StoryEntrySO Variants, StoryTriggerZone, Markdown 일괄 생성 |
| [CAMERA_SYSTEM_GUIDE.md](CAMERA_SYSTEM_GUIDE.md) | Camera 시스템 — CameraManager 오케스트레이터, LockOn/Collision/Distance/Effect/Shaker/KillCam 서브시스템, ICameraEffect 블렌딩 |
| [TIME_HITSTOP_GUIDE.md](TIME_HITSTOP_GUIDE.md) | GameTime / HitStop — id 기반 timeScale 큐(최저값 적용), Pause 우선, HitStopIntensity 프리셋, Volume 페이드, 액터 Animator 슬로우 |
| [INPUT_SYSTEM_GUIDE.md](INPUT_SYSTEM_GUIDE.md) | Input 시스템 — InputManager 콜백 라우팅, InputLayer 우선순위 차단, InputBuffer 선입력, 레이어 하락 시 Cancel 전파, 커서 스택 |

---

## 🛠️ 에디터 도구

| 메뉴 경로 | 도구 | 기능 |
|-----------|------|------|
| `UPlayGround/Item/Item Editor` | ItemEditorWindow | ItemSO 생성·편집, ID 중복 감지 |
| `UPlayGround/Item/Item Data Generator` | ItemDataGeneratorWindow | ID 대역 기반 ItemSO/EquipmentSO 자동 발급, ItemDatabase/ItemIdType 갱신 |
| `UPlayGround/Crafting/Recipe Editor` | RecipeEditorWindow | 레시피 시각 편집, 아이템 피커, CSV 내보내기 |
| `UPlayGround/Crafting/Recipe Data Generator` | RecipeDataGeneratorWindow | ItemDatabase 아이템 기반 레시피/재료/언락 조건 생성 및 RecipeIdType 갱신 |
| `UPlayGround/Drop Table Editor` | DropTableEditorWindow | 몬스터/인터랙션 드랍 테이블 통합 편집 |
| `UPlayGround/Actor/Actor Database Editor` | ActorDatabaseEditorWindow | ActorDefinitionSO DB 관리 |
| `UPlayGround/NPC/NPC Data Generator` | NpcDataGeneratorWindow | NpcActorSO 생성 및 NPC용 ActorDefinitionSO.npcData 자동 연결 |
| `Window/MotionSet Editor` | MotionSetWindow | 애니메이션 타임라인 편집 |
| `UPlayGround/Cheat Console` | CheatConsoleWindow | 개발용 치트 명령 실행 |
| `UPlayGround/Quest/Quest Editor` | QuestEditorWindow | 퀘스트 SO 생성·편집, DB 갱신, QuestIdType Enum 생성 |
| `UPlayGround/ID Enum Generator` | IdEnumGeneratorWindow | FX/UI/Actor/Quest 등 ID Enum 일괄 생성 |
| `UPlayGround/Minimap/Minimap Capture Editor` | MinimapCaptureEditorWindow | 씬 탑다운 촬영 → PNG 저장 → MinimapIconConfigSO 자동 할당 |
| `UPlayGround/Map/Map Placement Tool` | MapPlacementEditorWindow | 씬 클릭 기반 적·NPC·포탈 프리팹 배치 |
| `UPlayGround/Stat/Stat Database Editor` | StatDatabaseEditorWindow | ActorStatSO 검색·편집·비교·CSV 내보내기 |
| `UPlayGround/Stat/Stat Data Generator` | StatDataGeneratorWindow | EnemyStatsSO/PoiseSO 기반 ActorStatSO 생성, 연결, 전체 보정 |
| `UPlayGround/Stat/Stat Runtime Monitor` | StatRuntimeMonitorWindow | Play 모드 액터 스탯 및 수정자 모니터링 |
| `UPlayGround/Stat/Validate Stat Data Coverage` | StatDataGeneratorWindow | 모든 ActorDefinitionSO의 statData와 StatType 누락 검증 |

---

## 🧰 Generator Tool 목록

자동 생성/발급 도구는 기존 카테고리 메뉴를 유지하면서 `UPlayGround/Generator Tool/` 아래에서도 한 번에 접근할 수 있다.

| Generator Tool 메뉴 | 원래 메뉴 | 기능 |
|---------------------|-----------|------|
| `UPlayGround/Generator Tool/ID Enum Generator` | `UPlayGround/Util/ID Enum Generator` | FX/UI/CameraShake/Item/Recipe/Actor/Quest ID enum 생성 |
| `UPlayGround/Generator Tool/Item Data Generator` | `UPlayGround/Item/Item Data Generator` | ID 대역 기반 ItemSO/EquipmentSO 자동 발급 |
| `UPlayGround/Generator Tool/Recipe Data Generator` | `UPlayGround/Crafting/Recipe Data Generator` | ItemDatabase 기반 제작 레시피/재료/언락 조건 생성 |
| `UPlayGround/Generator Tool/Stat Data Generator` | `UPlayGround/Stat/Stat Data Generator` | EnemyStatsSO/PoiseSO 기반 ActorStatSO 생성 및 연결 |
| `UPlayGround/Generator Tool/Validate Stat Data Coverage` | `UPlayGround/Stat/Validate Stat Data Coverage` | ActorDefinitionSO의 statData/StatType 누락 검증 |
| `UPlayGround/Generator Tool/NPC Data Generator` | `UPlayGround/NPC/NPC Data Generator` | NpcActorSO와 NPC용 ActorDefinitionSO 생성 및 연결 |
| `UPlayGround/Generator Tool/Main Story Generator` | `UPlayGround/Story/Main Story Generator` | 메인 스토리 Quest/Dialogue/StoryEntry 생성 |
| `UPlayGround/Generator Tool/Sub Story Generator` | `UPlayGround/Story/Sub Story Generator` | 서브 스토리 Quest/Dialogue/StoryEntry 생성 |
| `UPlayGround/Generator Tool/Locomotion Motion Setup` | `UPlayGround/Util/Locomotion Motion Setup` | FBX 클립 기반 MotionSetAsset 일괄 생성/등록 |
| `UPlayGround/Generator Tool/Camera Shake Presets` | `UPlayGround/Camera/Generate Shake Presets` | 기본 카메라 쉐이크 프리셋 생성 |

---

## 🚀 개발 환경

- **Unity** 6 (6000.0.60f1) — URP
- **Target Frame Rate** 60fps (Application.targetFrameRate = 60)
- **KCC AutoSimulation** KCCSimulator 컴포넌트로 제어
- **빌드 / 테스트** Unity 에디터에서 직접 실행. 자동화 테스트 스위트 없음.
- **언어** C#. 주석 / 커밋 / 문서 모두 한국어.
