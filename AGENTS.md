# AGENTS.md

This file provides guidance to Codex (Codex.ai/code) when working with code in this repository.

## 프로젝트 개요

Unity 6 (6000.0.60f1) 기반 싱글플레이 TPS 액션 게임. 1인 개발. URP 렌더링 파이프라인.
Bokusei는 기본 고정 플레이어블 캐릭터이며, 나머지는 `CharacterActorType`에 정의된 타입을 플레이어블 대상으로 확장할 수 있는 구조. 적 처치 시 `MonsterActor._recruitableAs`에 지정된 타입을 `PartyManager.UnlockCharacter`로 파티에 합류시키는 방향.

**핵심 플러그인:** Animancer Pro V8, Kinematic Character Controller (KCC), MagicaCloth2, Addressables, lilToon.

## 언어

한국어 프로젝트. 코드 주석, 커밋 메시지, 문서 모두 한국어. 사용자가 한국어로 작성하면 한국어로 응답할 것.

## 빌드 & 실행

Unity 프로젝트이므로 CLI 빌드 명령어 없음. Unity 6 (6000.0.60f1+)에서 URP로 열기. 자동화된 테스트 스위트 없음.

## 아키텍처

### 모듈화 구조 — Codex 지속 메모리

asmdef 모듈화 작업은 Phase 5 UI 모듈화와 Phase 6 자동 검증까지 완료되었다. 이후 구조 관련 작업을 시작하기 전에 반드시 다음 문서를 기준으로 삼을 것.

- 상세 온보딩: `Assets/docs/ASMDEF_MODULARIZATION_ONBOARDING.html`
- 작업 이력과 체크포인트: `Assets/docs/TODO/ASMDEF_MODULARIZATION_PLAN.md`
- 최신 프로젝트 요약: `CLAUDE.md`

현재 런타임 asmdef 경계:

- `UPlayGround.Core` — 공통 기반
- `UPlayGround.Data` — ScriptableObject, DTO, enum 등 순수 데이터
- `UPlayGround.Contracts` — `IGameService`, `Services`, `Svc`, 공용 서비스 계약
- `UPlayGround.Camera` — 카메라 런타임
- `UPlayGround.Actor` — GameActor, 상태, 전투, AI, MotionEvent 런타임
- `UPlayGround.UI` — UI 런타임과 UI 소비자 계약(`UISvc`)

이후 작업의 필수 규칙:

1. 하위 모듈, 특히 UI에 구체 `SomeManager.Instance` 의존을 새로 추가하지 않는다. 공용 기능은 `Svc`, UI 소비자 기능은 `UISvc` 또는 소비자 소유 인터페이스를 사용한다.
2. `GameManager.RegisterManager`가 매니저가 구현한 `IGameService` 계약을 `Services`에 자동 등록한다. `Services.Get<T>()`은 자동 생성하지 않으며, 등록 전 접근은 null과 최초 1회 경고를 만든다.
3. Data 모듈은 Manager, Actor, Camera, UI 구현을 참조하지 않는다.
4. `UnityEditor` 사용 코드는 모듈별 `Editor` asmdef 또는 `Assets/02.Scripts/Editor/`에 둔다. 런타임 asmdef에 Editor 코드가 들어가면 안 된다.
5. 스크립트 물리 이동 시 `.meta`를 함께 이동하여 MonoScript GUID와 프리팹 연결을 보존한다.
6. `[SerializeReference]` 기반 MotionEvent/Ultimate 타입을 이동할 때 `[MovedFrom(true, sourceAssembly: "이전 어셈블리")]`를 반드시 적용한다. 이 규칙을 어기면 이벤트와 VFX 참조가 유실될 수 있다.
7. MotionSet/Ultimate/프리팹 오류가 있는 상태에서 에셋을 저장하거나 일괄 재직렬화하지 않는다. 먼저 컴파일과 타입 매핑을 복구하고 managed reference 및 VFX null 검사를 수행한다.
8. 모듈 변경 완료 조건은 Unity 컴파일 오류 0, 런타임의 무가드 `UnityEditor` 참조 0, UI의 신규 Manager 싱글톤 참조 0, Missing Script 0, managed reference/VFX 누락 0, Play Mode 서비스 경고·예외 0, Player Build 오류 0이다.
9. 검증 과정에서 `Assets/10.Datas/` 또는 `Assets/03.Prefabs/`가 자동 변경되면 diff를 반드시 검사한다. 검증이 만든 자동 재직렬화 변경만 원복하고 사용자 데이터 변경은 보존한다.
10. 사용자가 요청하지 않는 한 커밋하지 않는다.

2026-07-17 최종 자동 검증 기준: UI/Player 프리팹 63개 Missing Script 0, MotionSet/Ultimate 에셋 1,156개 managed reference 1,638개 중 누락 0, VFX 참조 168개 중 누락 0, StandaloneWindows64 Development Boot 빌드 오류 0.

### 매니저 시스템

`GameManager`가 최상위 싱글톤으로 모든 서브 매니저를 순차 초기화. 모든 매니저는 `BaseManager<T>`(제네릭 싱글톤)를 상속하고 `IManager` 인터페이스를 구현. 생명주기: `Init → AfterInit → OnUpdate/OnFixedUpdate/OnLateUpdate → Dispose → OnSceneChanged`.

매니저 목록: InputManager, AssetManager, UIManager, CameraManager, GameObjectManager, ItemManager, InventoryManager, EventManager, GameHitStopManager, VitalOrbManager, DialogueManager, GlobalFlagManager, StoryManager, GameTimeManager, PartyManager, SceneManager, SettingsManager.

### GameActor 계층 구조

`GameActor`(추상 MonoBehaviour)가 모든 인터랙티브 오브젝트의 베이스:
- `PlayerActor` — partial class로 5개 파일 분리 (base, lifecycle, input, combat, equipment). `IDamageable` 구현.
- `MonsterActor` — 적 엔티티. `IDamageable` 구현.
- `NpcActor` — `IInteractable` 구현.
- `GatheringActor`, `ItemActor`, `VitalOrbActor`, 투사체 (`BaseProjectile` → `LinearProjectile` / `AOEProjectile`).

### 상태 머신 (핵심 패턴)

상태는 KCC의 `ICharacterController`와 긴밀히 결합. 각 상태가 `UpdateVelocity`, `UpdateRotation` 등 KCC 콜백을 오버라이드하여 상태별 물리 동작을 제어.

```
GameActorState (추상)
├── PlayerActorState → 11개 구체 상태 (Idle, GroundMove, Airborn, Attack, Charge, Dash, DashAttack, Dodge, Guard, Crouching, Hit, Death, Interaction)
├── EnemyActorState → 13개+ 지상 상태 + 9개 비행 상태 (State/Enemy/EnemyFlying/)
└── NpcActorState → Idle, Talk, Wander
```

주요 메서드: `OnEnter`, `OnExit`, `UpdateState`, `CanTransitionState(string stateName)`, `UpdateVelocity`, `UpdateRotation`, `BeforeCharacterUpdate`, `AfterCharacterUpdate`, `PostGroundingUpdate`.

### 컴포넌트 시스템

`ActorComponent`(베이스) → `PlayerActorComponent`(플레이어 전용 베이스). GameActor에 기능별 컴포넌트를 조합:

- **전투:** `PlayerCombat`, `EnemyCombat` — 공격 로직, 데미지, 스킬
- **AI:** `EnemyBrain`(의사결정), `EnemyFlyingBrain`, `EnemyDetection`(시야/거리 감지), `EnemyTacticalMemory`
- **스탯:** `PoiseStat`(강인도/경직 저항)
- **장비/파티:** `PlayerEquipment`, `PlayerSkillGauge`, `PlayerSwapBehaviour`, `CharacterModelData`
- **VFX:** `ActorColorChanger`(피격 플래시), `DissolveController`(사망 디졸브)
- **IK:** `FootIKController`

### 이동 컨트롤러

`ActorMovementController`가 KCC의 `ICharacterController`를 구현하고 상태 머신을 호스팅:
- `PlayerMovementController`, `EnemyMovementController`, `NpcMovementController`
- 이동 컨트롤러가 KCC 콜백을 현재 상태에 위임.

### 애니메이션 시스템

Animancer 기반 **MotionSet 타임라인** 구조 — 하나의 액션에 여러 애니메이션 클립을 순차 체이닝. `MotionEventExecutor`가 타임라인 기반 이벤트(히트박스 활성화, VFX, SFX) 발화. `AvatarMask`를 통한 상체/하체 레이어 분리 지원.

### 입력 시스템

Unity Input System 기반, 우선순위 `InputLayer` 레벨 사용 (HUD=0, Scene=1000, Popup=2000, System=3000, Top=10000). `InputBuffer`로 선입력 지원. 이벤트 기반 등록/해제: `RegisterInputEvent`/`UnRegisterInputEvent`.

### 데이터 아키텍처 (ScriptableObject)

모든 수치 데이터는 `Assets/10.Datas/`의 ScriptableObject로 외부화:
- `EnemyBehaviorSO` — 페이즈 기반 AI 프로필 (HP 임계값에 따른 행동 전환)
- `EnemyStatsSO`, `EnemyFlyingSettingsSO`, `PoiseSO`
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
- PlayerActor는 partial class 사용 — 플레이어 동작 수정 시 5개 파일 모두 확인 필요
- 적 비행 상태는 별도 하위 폴더: `State/Enemy/EnemyFlying/`
