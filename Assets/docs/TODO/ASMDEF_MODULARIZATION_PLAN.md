# Actor / Data / Camera / UI asmdef 모듈화 작업 목록

> 작성일: 2026-07-17. 상태: **Phase 0 완료 / Phase 1~3 구현 완료, Unity 체크포인트 대기**.
> 목표: 4개 기능 모듈을 asmdef로 분리하고, 모듈 경계를 넘는 상향 참조는 인터페이스로 역전한다.
> 진행 방식: Phase 단위로 작업 → **각 Phase 끝에서 Unity 컴파일 + 스모크 확인 → git 커밋** 후 다음 Phase 착수.

---

## 배경 / 현황 실측

- `Assets/02.Scripts` ~1,040개 .cs 전부 Assembly-CSharp 단일 어셈블리 (예외: `02.Scripts/Core/UPlayGround.Core.asmdef` 하나).
- 커스텀 asmdef는 Assembly-CSharp을 **참조할 수 없음** → 모듈 내부에서 잔류 매니저를 부르는 코드는 전부 인터페이스 역전 필요.
- 커플링 실측 (2026-07 전수 조사):
  - Actor→Manager: 59파일, 16개 매니저 ~40멤버 (UIManager 8, CameraManager 6, GameObjectManager 5가 상위)
  - Actor→UI: 28파일, 10개 UI 타입 (UI_ActorHpBar 등은 **인스턴스 보유**)
  - UI→Manager: 18종 매니저 (PartyManager 79건, CheatManager 46건=치트 패널 전용, GameObjectManager 31건…)
  - Camera→Manager: 7종 / Camera→GameActor: 4파일(좁은 표면) / **Camera→UI: 0건**
  - AI 폴더↔GameActor **양방향 결합** (AI→Components 41파일, GameActor→AI 12 using) → AI 런타임은 Actor 모듈에 통합 외 대안 없음
  - UIManager·CameraManager는 물리적으로만 `Manager/`에 있는 각 모듈의 파사드
- 직렬화 함정: `Motion.cs`(MotionSetAsset)·`UltimateSequenceAsset.cs`가 `[SerializeReference]` 리스트 보유.
  SerializeReference는 타입을 **어셈블리명 포함**으로 직렬화 → 어셈블리를 옮기는 대상 타입에 `[MovedFrom(true, sourceAssembly: "Assembly-CSharp")]` 필수.
  - MotionEvent 구상 ~29개: 코드 참조가 에디터 툴뿐 → **Assembly-CSharp 잔류 가능, MovedFrom 불필요** (GameActor 쪽 참조는 전부 주석)
  - `UltimateTimelineEvent` 서브클래스 7개만 Actor 모듈행 + MovedFrom 부착
- `AttackData`(CombatData.cs:344, `public GameActor attacker;`)는 비-[Serializable] 런타임 DTO → 이동 시 직렬화 무관.
- 플러그인: Animancer(로컬 패키지)·UniTask·MagicaCloth2·MotionWarping은 asmdef 있음. DOTween은 DLL(자동 참조). **KCC만 asmdef 신설 필요**.
- 재사용 인프라: `GameManager.RegisterManager`(GameManager.cs:233)의 `_managerLookup`이 이미 타입 레지스트리. EventManager의 `IGameEventObservable/IGameEventPublisher` 인터페이스는 이미 분리돼 있음.

## 목표 의존 그래프

```
UPlayGround.Data       (순수) Enum·SO·MotionEventBase·IEventData·Input 정의
UPlayGround.Contracts  → Data, Core. Services 레지스트리·서비스 인터페이스·BaseManager/IManager·IWorldActor
UPlayGround.Camera     → Data, Contracts. CameraSystem 전체 + CameraManager(이동)
UPlayGround.Actor      → Data, Contracts, Camera. GameActor + AI 런타임 통합
UPlayGround.UI         → Data, Contracts, Camera, Actor. UI 전체 + UIManager(이동)
Assembly-CSharp        → 전 모듈 자동 참조. Manager 잔여·Cycle·Gameplay·Story·Scene·Tool·Debugging
Assembly-CSharp-Editor → Manager 참조 에디터 툴 전부 (02.Scripts/Editor/)
```

### 인터페이스 역전 원칙
- 역전은 **상향 참조 지점에만** 적용:
  - Camera→Actor(4파일) → Contracts `IWorldActor`(ActorId/Grade/transform/TryGetSocket/ActorType) + `IActorQueryService`
  - Actor→UI(28파일) → **Actor 모듈 내 정의** `IActorUIService` + 뷰 인터페이스(consumer-owned; 구현은 UIManager)
  - 모듈→잔류 Manager → Contracts 서비스 인터페이스 + 정적 `Services` 레지스트리 / `Svc` 단축 접근자
- 하향 직접 참조는 허용(무변경): Actor→Camera, UI→Actor(19파일), UI→Camera(1파일)
- 서비스 바인딩: `IGameService` 마커 + `RegisterManager`에서 리플렉션 자동 등록(Unregister 대칭 해제).
  치환 패턴: `XxxManager.Instance.Foo()` → `Svc.Xxx.Foo()`
- 이동 파일은 **네임스페이스 전부 유지** (직렬화 안전 + 호출부 무변경. CLAUDE.md 폴더-네임스페이스 규약은 신규 파일에만 적용, 기술부채로 주석 표기)

---

## Phase 0 — 사전 정리 (어셈블리 변화 없음)

- [x] unguarded `using UnityEditor;` 3건에 `#if UNITY_EDITOR` 가드 (현재도 플레이어 빌드를 깨는 버그):
  - [x] `02.Scripts/Camera/CameraShaker.cs:2`
  - [x] `02.Scripts/GameActor/State/Enemy/EnemyDeathState.cs:2`
  - [x] `02.Scripts/GameActor/State/Player/PlayerDeathState.cs:2`
- [x] KCC asmdef 신설: `ExternalAssets/Etc/KinematicCharacterController/Core/KinematicCharacterController.asmdef`
      `{ "name": "KinematicCharacterController", "autoReferenced": true }` (Examples 폴더는 Assembly-CSharp 잔류)
      - `Core/Editor/` 격리를 위해 `KinematicCharacterController.Editor.asmdef`도 함께 신설
- [x] `Manager/Base/BaseManager.cs` 미사용 Addressables using 제거 (Contracts 이동 대비)
- [x] **체크포인트**: Unity 컴파일 0 에러 → 커밋

## Phase 1 — Data 폴더 순수화 (물리 이동만, 어셈블리 불변 → 무위험)

.cs+.meta 동반 이동, 네임스페이스 유지:

- [x] MotionEvent 구상 29파일 (`Data/Event/Animation/MotionEvent_*.cs`, 베이스 MotionEvent.cs 제외) → `02.Scripts/Gameplay/MotionEvents/`
- [x] `Data/Combat/Ultimate/` 런타임 전체 → `02.Scripts/GameActor/Combat/Ultimate/`
- [x] `AttackData` 클래스(CombatData.cs:344~) 발췌 → `02.Scripts/GameActor/Combat/AttackData.cs` 신규
- [x] Data 에디터 툴 중 Manager/Components/CameraSystem 참조분 → `02.Scripts/Editor/`
      (MotionSetWindow partial 8파일, MotionEventAddPopup, MotionSetAssetEditor, UltimateSequenceEditorWindow, Data/Camera/Editor 전체)
- [x] `Data/Camera/`의 CameraSystem/CameraShaker 참조 SO 4파일 → `02.Scripts/Camera/Data/`
- [x] `Data/Dialogue/Actions/QuestDialogueActions.cs` → `02.Scripts/Story/`
- [x] `Data/Party/CharacterEffectiveStatCalculator.cs` → `02.Scripts/GameActor/Component/Player/` (매니저 접근은 Phase 4에서 서비스 치환)
- [x] enum 정의 이동 → `02.Scripts/Data/UI/`: UI의 `MinimapMarkerType`·`FloatStyle`, UIManager의 `CanvasLayer` (enum은 int 직렬화라 안전)
- [x] `02.Scripts/Input/` 순수 파일(InputDefine/InputBuffer/InputStructure/InputUtility/ComboInputTracker) → `02.Scripts/Data/Input/` (피참조 51건 무변경)
- [x] `Util/Extension.Layer.cs` → `02.Scripts/Data/UI/` (CanvasLayer+InputLayer 둘 다 Data행이므로)
- [ ] **체크포인트**: 컴파일 0 에러 + MotionSetAsset·UltimateSequenceAsset 열어 이벤트 리스트 보존 확인 + 전투 스모크 → 커밋

## Phase 2 — Data asmdef + Contracts asmdef

- [x] `02.Scripts/Data/UPlayGround.Data.asmdef` 생성 — references(UniTask/Unity.InputSystem/Addressables 등)는 grep 실측 후 확정
- [x] Data 잔류 순수 에디터 코드 → `Data/Editor/`로 집결 + `UPlayGround.Data.Editor.asmdef`(includePlatforms: ["Editor"], refs: Data).
      Manager 참조 잔재 발견 시 `02.Scripts/Editor/`로 추방
- [x] `02.Scripts/Contracts/UPlayGround.Contracts.asmdef` 신설 (refs: Data, Core):
  - [x] `Manager/Base/BaseManager.cs`·`IManager.cs` 이동 (namespace `UPlayGround.Manager` 유지)
  - [x] EventManager의 `IGameEventObserver/Observable/Publisher` 인터페이스 별도 파일 분리 이동
  - [x] 신설: `IGameService` 마커, `Services` 정적 레지스트리, `Svc` 단축 클래스
  - [x] 1차 서비스 인터페이스(Phase 3용 표면): `IInputService`, `IHitStopService`/`IVitalOrbService`, `ISettingsService`, `IGameTimeService`, `IAssetService`, `IActorQueryService`, `IWorldActor`
- [x] `GameManager.RegisterManager`(233 부근)에 IGameService 자동 바인딩 삽입 + UnregisterManager 대칭 해제
- [x] 해당 매니저들에 인터페이스 구현 선언 부착, GameActor에 `IWorldActor` 부착 (아직 Assembly-CSharp이라 즉시 컴파일 가능)
- [ ] **체크포인트**: 컴파일(Phase 1 잔재가 여기서 전부 드러남 — 이 Phase가 Phase 1의 검증기) + 부팅 로그 매니저 등록 확인 → 커밋

## Phase 3 — Camera 모듈

- [x] `Manager/CameraManager.cs` → `02.Scripts/Camera/` 이동 (네임스페이스 유지, MonoBehaviour GUID라 씬/프리팹 안전, 외부 `CameraManager.Instance` 113건 무변경)
- [x] Camera 내부 매니저 호출 ~30건 치환:
  - `GameObjectManager.Instance.Player` → `Svc.ActorQuery.PlayerTransform`
  - `AllActors` → `IWorldActor` 순회 / `GetComponentInParent<MonsterActor>()` → `GetComponentInParent<IWorldActor>()`+ActorType 판별
  - InputManager/GameCombatManager/SettingsManager/GameTimeManager/ActorSpawnManager → `Svc.*`
- [x] `02.Scripts/Camera/UPlayGround.Camera.asmdef` 생성 (refs: Data, Contracts, +UniTask/Unity.InputSystem 실측 후)
- [ ] **체크포인트**: 컴파일 + 락온/전투카메라/킬캠/대화카메라/스냅샷 각 1회 플레이 확인 → 커밋

## Phase 4 — Actor 모듈 (최대 규모)

컴파일 불가 구간 최소화 순서: ①인터페이스+상위 구현 부착 → 커밋 → ②호출부 전량 치환 → 커밋 → ③MovedFrom → ④asmdef 생성 → 체크포인트

- [x] `02.Scripts/AI/` → `02.Scripts/GameActor/AI/` 물리 이동 (양방향 결합이라 통합 필수).
      BT 에디터 등: 자기 모듈만 참조하면 `GameActor/Editor/UPlayGround.Actor.Editor.asmdef`, Manager 참조분은 `02.Scripts/Editor/`
      ※ `generate-bt-json` 스킬의 SourceJson 경로 영향 확인
- [x] 최상위 `02.Scripts/Animation/`·`Particle/` 소속 실측 후 결정
      (`Animation/`은 빈 폴더라 정리, `Particle/`은 Actor 장비/VFX 결합으로 `GameActor/Particle/` 이동)
- [x] Contracts 서비스 확장(실측 40멤버): `IPartyService`(3), `IItemService`(3), `IInventoryService`(2), `IDialogueService`(2), `IMonsterRespawnService`, `IInteractionRespawnService`, `ISoundService`.
      QuestManager/RecipeManager `NotifyMonsterKill`은 EventManager 이벤트(`MonsterKilled` 페이로드)로 전환 권장
- [x] Actor 모듈 내 `GameActor/Contracts/`에 consumer-owned 인터페이스(구현은 상위):
  - [ ] `IActorRegistryService` (GameObjectManager 구현: Player/ShowFX/SpawnItem/CreateWeapon/InteractionHandler)
  - [ ] `IActorUIService` (UIManager 구현: ShowDamageFloater(Heal)/CreateHpBar/CreateBreakInteraction/CreateDangerRing + ShowUI·GetUI 사용처는 용도별 메서드로: ShowRespawnPopup/NotifyItemAcquired/ShowInteractionBoard/ToggleInventory 등)
  - [ ] 뷰 인터페이스: `IActorHpBarView`(MonsterActor/PoiseStat/MonsterBreakGauge 실측 멤버), `IBreakInteractionView`, `IDangerRingView`, `IInteractionBoardView` — GameActor 측 보유 필드 타입 교체
- [x] Actor 내부 매니저 호출 ~130건 + UI 참조 28파일 치환. State/Player의 InputBuffer 폴링은 상태 진입 시 `Svc.Input` 캐싱.
      완료 판정: `UIManager.Instance` 등 잔존 grep 0건
- [x] MovedFrom 부착: `UltimateTimelineEvent` 서브클래스 7개(SpawnVfx/Sound/TimeScale/CameraEffect/CameraShake/DamageWindow/CustomCallback)에
      `[MovedFrom(true, sourceAssembly: "Assembly-CSharp")]` + `[SerializeReference]` GameActor 폴더 최종 스캔
      MotionEvent 직렬화 클래스 30개에도 동일한 어셈블리 이동 매핑 적용
- [x] `02.Scripts/GameActor/UPlayGround.Actor.asmdef` 생성 (refs: Data, Contracts, Camera, UniTask, Unity.InputSystem, KinematicCharacterController, Animancer, +MotionWarping/MagicaCloth2 실측 후)
- [ ] **체크포인트**: 컴파일 + **UltimateSequenceAsset events 보존 확인(MovedFrom 검증 핵심 — null/Unknown type이면 즉시 롤백)** + 전투 풀사이클(공격/피격/브레이크/궁극기/처치/HP바/데미지플로터/리스폰) → 커밋

## Phase 5 — UI 모듈

- [x] `UI/Debug/UI_DevCheatPanel.*` 9파일(+Editor 빌더) → `02.Scripts/Debugging/DevCheat/` 이동 — CheatManager 46건 인터페이스 추출 회피.
      `UI_GamePlay.cs:206`의 `GetUI<UI_DevCheatPanel>()`은 문자열 키/UI_Base 수준 토글로 치환
- [x] `Manager/UIManager.cs` → `02.Scripts/UI/` 이동 (네임스페이스 유지). `AssetManager.Instance.LoadGlobalAsync` → `Svc.Asset`. `IActorUIService` 구현 유지.
      `CreateHpBar(GameActor)` 등 Actor 파라미터는 UI→Actor 직접 참조라 무변경
- [x] Contracts 서비스 확장(UI 실측 표면): `IPartyService`(79건 — 최대, PlayerActor 노출분은 Actor 모듈 정의), `IQuestService`(26), `IGameTimeService`(25), `IDialogueService`(22), `IInputService`(22), `IActorRegistryService`(31), `IInventoryService`(14), `ICycleRunService`(13), `ISaveService`(10), `ISceneFlowService`(7), `IItemService`(7), `IBossAssistService`(6), `IRecipeService`(3).
      UIManager 자기호출 28건은 모듈 내부화로 소멸, CheatManager 46건은 이동으로 소멸, CameraManager 1건은 직접 참조
- [x] UI 내부 치환 수백 건: `(\w+Manager)\.Instance` grep → `Svc.Xxx` 일괄. 완료 판정: 잔존 0건
- [x] UI 에디터 19파일: 자기 모듈만 참조하면 `UI/Editor/UPlayGround.UI.Editor.asmdef`, Manager 참조분은 `02.Scripts/Editor/`
- [x] `02.Scripts/UI/UPlayGround.UI.asmdef` 생성 (refs: Data, Contracts, Camera, Actor, UniTask, Unity.InputSystem, UnityEngine.UI, Unity.TextMeshPro 등 실측)
- [x] **체크포인트**: 컴파일 + UI 프리팹 62개 및 Player 프리팹 Missing Script 0건 + MotionSet/Ultimate managed reference·VFX 누락 0건 확인 (커밋은 사용자 요청으로 생략)

## Phase 6 — 마무리

- [x] `Services.Get` 누락 바인딩 경고 로그로 탐지, 미사용 `Svc` 멤버 정리
- [x] 자동 회귀: 현재 인게임 씬 Play Mode 부팅/종료, 서비스 미등록 경고 0건, 런타임 예외 0건
- [ ] 수동 전체 회귀: 부팅→인게임→전투→사이클→세이브/로드
- [x] **플레이어 빌드 1회** (StandaloneWindows64 Development, Boot 씬, 오류 0)
- [x] CLAUDE.md 갱신 (모듈 구조, EnemyBrain 등 낡은 기술 수정)

## Phase 7 — Camera 이식 경계 강화

- [x] Camera 소유 런타임 포트 `ICameraRuntimeAdapter`와 안전 기본 구현 추가
- [x] Camera 내부의 `Svc.*`, `IWorldActor`, `SettingsData`, `VitalOrbTrigger` 직접 참조 제거
- [x] 입력·에셋·설정·월드 대상·시간 제어·킬캠 후처리를 포트 뒤로 역전
- [x] UPlayground 구현을 `Manager/Camera/UPlayGroundCameraRuntimeAdapter.cs` 조립 계층으로 격리
- [x] `GameManager`가 CameraManager 등록 전에 어댑터를 구성하고 종료 시 리셋
- [x] 기존 CameraManager 공개 API, 카메라 SO, 직렬화 어셈블리 유지
- [x] 이식 절차 문서: `Assets/docs/CAMERA_MODULE_PORTABILITY_GUIDE.md`
- [x] Camera / Actor / UI / Assembly-CSharp CLI 컴파일 오류 0
- [ ] Play Mode 락온/전투카메라/킬캠/대화카메라/스냅샷 스모크

---

## 리스크

| # | 리스크 | 대응 |
|---|---|---|
| 1 | **UltimateTimelineEvent MovedFrom 누락 = 궁극기 에셋 데이터 소실** | Phase 4 체크포인트에서 에셋 열람 필수, Phase별 커밋으로 롤백 보장 |
| 2 | IPartyService 79건 표면 — Phase 5가 최대 물량 | 동일 프로퍼티 반복 접근일 가능성 높아 실측 시 압축 |
| 3 | 초기화 순서: `Services.Get`은 BaseManager.Instance와 달리 자동 생성 없음 — GameManager 등록 전 Awake 접근 시 null | 부팅 로그로 탐지, 지연 조회로 수정 |
| 4 | Unity 컴파일 CLI 불가 | Phase 단위 진행, 각 체크포인트마다 에디터에서 확인 후 다음 Phase |

## 최종 완료 판정

- 플레이어 빌드 성공
- MotionSetAsset / UltimateSequenceAsset 데이터 무손실
- 전투·UI·카메라·사이클 스모크 통과
- `grep "Manager\.Instance"` 결과 `02.Scripts/{GameActor,UI,Camera,Data}` 내 잔존 0건
