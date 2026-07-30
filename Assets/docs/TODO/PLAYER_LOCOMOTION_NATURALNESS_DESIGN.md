# 플레이어 로코모션 자연스러움 개선 설계 문서

> 작성일: 2026-07-28
> 대상 버전: Unity 6 (6000.0.60f1), URP, KCC, Animancer Pro V8
> 레퍼런스: [UE5 Lyra Game Core Animation 분석](https://www.jaydengames.com/posts/ue5-black-magic-game-core-animation/), [ALS-Community V4 소스](https://github.com/dyanikoglu/ALS-Community), [Building a Turn in Gameplay Animation (Animotionx)](https://www.animotionx.com/en/post/building-a-turn-in-gameplay-animation-the-angle-under-tension), [Distance Matching in UE](https://dev.epicgames.com/documentation/unreal-engine/distance-matching-in-unreal-engine), [Unity Input System — Gamepad](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.8/manual/Gamepad.html), [Analyzing Core Character Movement in 3D](https://www.gamedeveloper.com/design/analyzing-core-character-movement/)
> 상태: **P0~P3 및 P4-1 코드 구현 · Unity 튜닝/스모크 검증 대기** (2026-07-29, P4-2는 원본 애니메이션 미저작으로 보류)
> 범위: 플레이어 지상 이동(Idle / GroundMove / Stop / TurnInPlace)에 한정. 공중·전투·회피는 회귀 대상일 뿐 개선 대상이 아니다.

---

## 0. 한 줄 요약

**이미 저작되어 있는 Stop 9종 · Turn 15종 클립을 코드가 호출하지 않고 있다.** 신규 에셋 없이 코드만으로 회수 가능한 자연스러움이 가장 크다. 반면 8방향 스트레이프는 플레이어용 클립이 0건이라 애니메이션 저작이 선행되어야 한다.

### 0.1 구현 체크포인트 (2026-07-29)

- P0-1~P0-3 구현: radial deadzone, 입력 크기 스무딩, 릴리즈 유예, 모션별 종료 판정, 로컬 시간축 통일.
- P0-5 구현 완료: `ActorAnimationMotionSet`에 슬롯별 `motionRootYaw` / `motionReferenceSpeed`를 추가하고 Motion Editor에 18개 슬롯 원클릭·명령행 일괄 베이크를 추가. Bokusei Katana는 Turn 15종(45/90/180°)과 Walk/Run/Sprint(1.5/3.5/6m/s) 데이터까지 채움.
- P1 구현: Run/Walk Stop 확대, 135° Turn 진입, 회전 스케일/종료 보정, 액션·벽 캔슬, 재진입 쿨다운, 루트모션 이탈 속도 시드, 속도 기반 뱅킹, 카메라 속도 EMA.
- P2/P3 구현: 가속·감속·반전 계수 분리와 기준 속도 기반 로코모션 Graph 재생속도 동기화.
- P4-1 (a) 구현: Idle 카메라 정렬 회전. P4-2는 방향 클립 저작 전까지 보류.
- 남은 작업: 30fps 스트레스 및 Play Mode 회귀, 파라미터 체감 튜닝. 다른 캐릭터/무기 세트는 콘텐츠 적용 시 같은 자동 베이크를 실행한다.

---

## 1. 배경과 문제 정의

현재 플레이어 지상 이동은 다음 한 줄로 요약된다.

> 전방 클립 1종을 100% 속도로 재생하면서, 몸통을 이동 방향으로 고정 속도로 회전시키고, 속도를 단일 지수 계수로 목표값에 수렴시킨다.

여기서 파생되는 체감 문제:

| 증상 | 직접 원인 |
|---|---|
| 급선회 시 발이 지면에 붙은 채 몸만 미끄러지듯 회전 | Turn 클립 15종이 재생되지 않음 (§2 D-1) |
| Run 중 손을 떼면 정지 모션 없이 Idle로 툭 끊김 | Stop 진입이 Sprint 전용 (§2 D-2) |
| 스틱을 반만 기울이면 발이 미끄러짐 | 실제 속도만 아날로그 반응, 클립은 항상 100% (§2 D-6) |
| 출발과 정지의 무게감이 동일 | 가속·감속이 단일 계수 (§2 D-4) |
| 스프린트 중에도 제자리 180° 즉시 회전 | 선회 속도가 이동 속도와 무관 (§2 D-5) |
| 공격 한 번이면 스프린트가 풀리고 3초 재대기 | `OnExit` 무조건 Run 리셋 (§2 D-8) |
| Idle 중 카메라를 돌려도 캐릭터가 안 돌아봄 | `UpdateRotation`이 no-op (§2 D-9) |

---

## 2. 현황 진단 — 코드 검증 결과

전 항목 실제 코드 확인 완료. `Assets/` 기준 경로.

### D-1. `PlayerTurnInPlaceState`는 호출자 없는 데드 코드 — **확인**

`PlayerTurnInPlaceState` 문자열이 프로젝트 전체에서 자기 선언부 1곳(`02.Scripts/GameActor/State/Player/PlayerTurnInPlaceState.cs:13`)에만 등장한다. `new PlayerTurnInPlaceState(...)` **0건**.

**클립 에셋은 이미 존재한다:**
`10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Katana/` 아래에
`Bokusei_Run_Turn_L45/R45/L90/R90/180`, `Bokusei_Walk_Turn_*`, `Bokusei_Sprint_Turn_*`, `Bokusei_Stand_Idle_Turn_*` — 총 15종 + Idle 5종.
매핑 정의는 `02.Scripts/Editor/LocoMotionSetupWindow.cs:63-85`.

> 즉 "클립이 없어서 못 쓴 것"이 아니라 **호출자만 빠져 있다.** 활성화 비용이 낮은 이유.

### D-2. Stop 진입이 Sprint 전용 — **확인**

```csharp
// PlayerGroundMoveState.cs:110-120
var forwardStopKey = PlayerStopState.GetStopAnimKeyForward(gameActor.MoveAnimType);
bool hasStop = gameActor.Animator.HasMotion(forwardStopKey, true);
if (hasStop && gameActor.MoveAnimType == BaseMoveAnimType.Sprint)   // ← 이 AND 절
```

`hasStop`으로 이미 클립 유무를 검사하는데 `== Sprint`가 추가 AND되어 Run/Walk는 무조건 `PlayerIdleState`로 간다(`:119`).

**Stop 클립 에셋 9종 전부 존재:** `Bokusei_Move_Stop_Running / _L45 / _R45`, `_Walking*`, `_Sprinting*` + `MotionSet/Humanoid/HumanoidFallback/Humanoid_Move_Stop_*` 폴백 9종. 매핑은 `LocoMotionSetupWindow.cs:51-61`.

부수 사실: Sprint가 켜지는 경로는 ① Sprint 키(`PlayerActor.Input.cs:90-94`) ② 자동 전환(`PlayerGroundMoveState.cs:174-179`) ③ **`PlayerDashState.cs:84` OnExit 강제 설정**. 그래서 "대시 후에만 정지 모션이 보인다"는 현상이 나타난다.

### D-3. 플레이어 8방향 로코모션 미사용 — **확인, 단 원인이 다름**

`EnemyLocomotionHelper` 참조처는 전부 Enemy 상태(`EnemyChase/Patrol/Circle/Flank/Retreat/Step/Dodge`)뿐. 플레이어는 `PlayerGroundMoveState.GetMoveAnimKey()`(`:238-250`)로 전방 1종만 재생.

**결정적 제약:** `10.Datas/.../MotionSet/Player/**/*_F_L45*.asset` — **0건**.
Stop/Turn과 달리 8방향은 **플레이어용 클립 저작이 선행 필수**다. 코드 작업이 아니다. → §6 P4로 분리, 조건부 보류.

(참고: 4방향 헬퍼는 이미 있음 — `PlayerActorState.cs:103-122 ResolveDirectionalMotionKey`, Dodge류가 사용.)

### D-4. 가속·감속이 단일 계수 — **부분 정정**

- 가속: `PlayerGroundMoveState.cs:219-222` — `StableMovementSharpness`
- 감속: `PlayerIdleState.cs:153-156` — 동일 `StableMovementSharpness`
- **선회는 별도 계수** `OrientationSharpness` (`PlayerGroundMoveState.cs:186-195`)

정확히는 **"가속·감속 2개가 한 계수를 공유, 선회는 별개"**다. 셋 다 `1 - Exp(-k·dt)` 지수 수렴이라 램프 형상을 나눌 축이 없다.

`PlayerStopState` / `PlayerTurnInPlaceState`는 이 계수를 쓰지 않고 **루트모션으로 속도를 완전 대체**한다(`PlayerStopState.cs:135-141`).

### D-5. 선회 속도가 이동 속도와 무관 — **확인**

`ActorMovementController.cs:20` `OrientationSharpness = 10`, 분기 없음. 60fps에서 `1-exp(-10×0.0167)≈0.154` → 5~6프레임이면 회전 대부분 완료. 스프린트 중 180° 즉시 회전 성립.

### D-6. 아날로그 크기가 애니 속도에 미반영 — **확인 (라인 정정)**

```csharp
// PlayerGroundMoveState.cs:212-216
Vector3 reorientedInput = Vector3.Cross(...).normalized * moveInputVector.magnitude;
Vector3 targetMovementVelocity = reorientedInput * GetMaxMovementSpeed();
```

`magnitude`(0~1)가 속도에 직접 곱해진다. 반면 `Animator.Speed` / `MotionTimelineSpeed`를 이동 속도에 맞추는 코드는 **플레이어 로코모션 경로에 전무**.

### D-7. 데드존·스무딩 부재 — **확인**

- `PlayerMovementController.cs:179-182` — `HasMoveInput() => _moveInputVector.sqrMagnitude > 0` (임계값 0)
- `SetInputs`(`:141-172`) — 카메라 기준 회전 변환만, 저역통과 없음
- 원본도 raw — `PlayerActor.Input.cs:81`

**추가 발견:** `_lookInputVector`는 `sqrMagnitude > 0f`일 때만 갱신(`:168-171`)되어 입력이 끊기면 마지막 방향이 잔존한다. `PlayerStopState` 생성자가 이 잔존값을 정지 방향으로 쓴다(`PlayerGroundMoveState.cs:115`). **데드존 도입 시 이 의존을 함께 재설계해야 한다.**

### D-8. Sprint 자동 전환 3중 결함 — **전부 확인**

| 결함 | 근거 |
|---|---|
| `Time.realtimeSinceStartup` 기반 (로컬 타임스케일·일시정지 무시) | `PlayerGroundMoveState.cs:37, :174`. 나머지 상태 머신은 `Actor.DeltaTime`(`ActorMovementController.cs:146`) |
| `OnExit`에서 무조건 Run 리셋 | `PlayerGroundMoveState.cs:49` |
| `_runTimer = float.MaxValue`로 상태 내 재발동 불가 | `:178` |

동종 버그: `PlayerMovementController.cs:121` 대시 쿨다운도 `Time.deltaTime`(LocalTimeScale 무시).

### D-9. Idle 회전 no-op — **확인**

`PlayerIdleState.cs:136-140`이 `currentRotation.normalized`만 수행. `Stand_Idle_Turn_*` 5종은 에셋도 매핑도 있으나 재생 코드 0건.

**인프라는 준비되어 있다:** `PlayerMovementController.cs:165` `_cameraForwardDirection`은 이동 입력과 무관하게 항상 갱신되고, `:87` 주석이 *"TurnInPlace 등 Idle 상태에서도 참조"* 라고 명시 — 원래 이 용도로 만들어 둔 값이 방치된 상태.

### D-10. **신규 발견 — `OnMotionSetCompleted` 오발화 (기존 버그)**

`ActorAnimator.cs:1185-1189`의 `OnMotionSetCompleted`는 특정 모션이 아니라 **"아무 MotionSet이든 완료되면"** 발화하는 전역 이벤트다. 그런데:

- `PlayerStopState.cs:53` — `OnMotionSetCompleted += TransitionToIdle`
- `PlayerTurnInPlaceState.cs:42` — `OnMotionSetCompleted += OnTurnComplete`

두 상태 모두 이걸 자기 클립의 종료 신호로 쓴다. 무기 뽑기 모션 등 **다른 MotionSet이 도중에 끝나면 Stop/Turn 클립이 재생 중인데도 즉시 다음 상태로 전환**된다.

> **Stop을 Run/Walk까지 확대하면 이 버그의 발생 빈도가 3배가 된다. P1의 선행 조건이다.**

### D-11. 프리팹 파라미터 실측

플레이어 프리팹 4종(`03.Prefabs/Actor/Player/Player.prefab` = 현행 메인, `Actor/Player_Bokusei.prefab`, 레거시 `PlayerActor_.prefab` / `Player_Actor.prefab`).

| 파라미터 | 코드 기본값 | Player.prefab | 오버라이드 |
|---|---|---|---|
| `MaxWalkMoveSpeed` | 3 | 3 | 없음 |
| `MaxRunMoveSpeed` | 6.5 | 6.5 | 없음 |
| `MaxSprintMoveSpeed` | 10 | 10 | 없음 |
| `StableMovementSharpness` | 15 | 15 | 없음 |
| `OrientationSharpness` | 10 | 10 | 없음 |
| `SprintAutoStartDelay` | 3 | 3 | 없음 |

**오버라이드가 하나도 없다.** 그리고 `MonsterActor_.prefab`, `NPC_Default.prefab`, `Monster_RootPlant_V1~V3.prefab`도 동일한 3 / 6.5 / 10 / 15 / 10. **플레이어와 몬스터가 같은 이동 파라미터를 공유**하며 아무도 튜닝한 적이 없다.

### D-12. 루트모션 이중 적용 — **위험 없음 (확인)**

`Player.prefab:241798`(ActorAnimator)과 `:241840`(Animator)의 `m_GameObject`가 동일(`4208814698171108161`) → `OnAnimatorMove`가 Unity 자동 적용을 정상적으로 가로챈다.

단 `ActorAnimator.cs:301`이 `GetComponentInChildren<AnimancerComponent>()`라, **모델 교체로 Animator가 자식으로 내려가면 즉시 깨지는 암묵 계약**이다. → §5 규약 R-7.

### D-13. **신규 발견 — TurnInPlace에 회전 수렴 로직이 없다 (차단)**

```csharp
// PlayerTurnInPlaceState.cs:95-99
public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
{
    currentRotation = currentRotation * gameActor.Animator.RootMotionStepDeltaRotation;
}
```

**순수 루트모션 누적이며 `_targetDirection`을 향한 수렴이 없다.** `_targetDirection`은 생성자에서 캡처되어 `OnEnter`의 클립 선택(`:35-38`)과 이탈 판정(`:86`)에만 쓰이고 회전에는 반영되지 않는다.

`Run_F_Turn_180` 클립의 베이크 회전량은 **고정값**(≈180°)인데 트리거 조건은 `angle >= 135°`이므로 실제 필요 각도는 **135°~180°의 임의 값**이다. 결과:

- 140° 전환 의도 → 클립이 180° 회전 → **40° 오버슈트**
- 턴 종료 후 GroundMove 복귀 시 `OrientationSharpness`가 그 40°를 되돌림 → **"돌았다가 되돌아오는" 이중 회전**

> **TurnInPlace를 활성화하면 즉시 드러나는 결함이다. P1-2의 차단 요소.**

### D-14. **신규 발견 — 루트모션 기아(starvation)로 `Motor.Velocity` 버스트**

`OnAnimatorMove`는 **렌더 프레임당 1회** 누적하고(`ActorAnimator.cs:1609-1618`), `BeginRootMotionStep`은 **물리 스텝당 1회** 소비한다(`ActorMovementController.cs:292`). `ActorAnimator.cs:214-215`의 주석은 *"OnAnimatorMove가 여러 번 호출돼도 다음 KCC 스텝에서 정확히 한 번 소비한다"* 로 **누적 초과 방향만** 다루며, **부족 방향은 처리되지 않는다.**

전제 확인: `Player.prefab:4828` `m_UpdateMode: 0` = **Normal(Update 구동)**. 즉 Animator는 Update 축, KCC는 FixedUpdate 축이다.

프레임레이트 < 물리레이트일 때(예: 30fps 렌더 / 50Hz 물리, 프레임당 ~1.67 스텝):

| 물리 스텝 | `StepPosition` | `GetRootMotionStepVelocity(dt)` |
|---|---|---|
| 1번째 | 프레임 전체 누적분 | `dt`로 나눠 **약 1.67배 과대** |
| 2번째 | **0** (OnAnimatorMove 미발화) | **0** |

**순 변위·회전량은 정확히 보존된다** (모터는 `velocity × dt = StepPosition`만큼 이동하므로 드리프트 없음). KCC Interpolate(`KCCSimulator.cs:65, 110`)가 시각적으로도 상당 부분 완화한다.

**문제는 `Motor.Velocity`를 읽는 소비자다.** `ActorMovementController.cs:104`의 `CameraVelocity`가 이 값을 그대로 노출하고 `CameraManager.cs:378-380`이 FOV·거리 연출에 쓴다. Stop/TurnInPlace 중 속도가 `0 → 1.67배 → 0`으로 진동하면 **카메라가 떨린다.** Stop을 Run/Walk까지 확대하면(P1-1) 노출 빈도가 크게 증가한다.

> `AnimatorUpdateMode.AnimatePhysics`로의 전환은 **기각.** MotionSet 디렉터가 `Actor.DeltaTime`(Update 축)으로 도는 구조(`ActorAnimator.cs:1051`)와 충돌하고 영향 범위가 프로젝트 전체다.

### D-15. **신규 발견 — 루트모션 상태 이탈 시 속도가 비결정적**

`PlayerTurnInPlaceState.cs:101-108` / `PlayerStopState.cs:135-141`은 매 스텝 평면 속도를 루트모션 값으로 **완전 대체**한다. 따라서 GroundMove로 나가는 순간의 `currentVelocity`는 **마지막 물리 스텝이 D-14의 기아 스텝이었는지에 따라 0이거나 과대값**이다.

GroundMove의 `UpdateVelocity`(`:219-222`)가 그 값에서 `Vector3.Lerp`로 출발하므로:
- 마지막이 기아 스텝 → 턴 종료 후 **정지 상태에서 재가속** (뚝 끊김)
- 마지막이 버스트 스텝 → **과속으로 튀어나감**

**같은 입력에 대해 프레임 타이밍만으로 결과가 달라진다.** D-14보다 체감이 직접적이다.

### D-16. 시간축 정합성 — **안전 확인**

우려했으나 문제가 없음을 확인한 항목:

| 항목 | 결론 |
|---|---|
| 히트스톱 중 루트모션 속도 | **정합.** `GameActor.cs:100`이 `Animator.Speed = LocalTimeScale`로 애니를 늦추고 `KCCSimulator.cs:103`이 `scaledDt = baseDt × scale`로 나누므로 `StepPosition / scaledDt`에서 **스케일이 정확히 상쇄**된다 |
| 0 나눗셈 | **없음.** `LocalTimeScale`은 0.001 하한 클램프(`GameActor.cs:97`), `GetRootMotionStepVelocity`에 1e-6 가드(`ActorAnimator.cs:1033`) |
| 루트모션 이중 적용 | **없음** (D-12) |
| MotionWarp 간섭 | **없음.** `EvaluateVelocity` 호출처는 공격 계열 상태뿐이며 로코모션은 호출하지 않는다. 워프 윈도우가 열린 채 턴으로 캔슬돼도 평가되지 않아 무해 (단 다음 공격이 stale 윈도우를 볼 가능성은 별건의 기존 이슈) |
| 순 변위 드리프트 | **없음** (D-14) |

### D-17. 입력 실행 순서 미정의

`PlayerActor.Update`(`Lifecycle.cs:119-145`)가 `SetInputs` 직후 원샷 조건을 즉시 클리어하는데, `ActorMovementController.Update`(`:140-149`)와의 실행 순서가 **정의되어 있지 않다** — `GameActor/` 하위에 `DefaultExecutionOrder` 없음, `ProjectSettings/MonoManager.asset`에 순서 항목 없음.

원샷 입력은 어느 순서든 정확히 1틱만 노출되므로 **유실은 없고**, 최대 1프레임 지연이 붙을 뿐이다. `TurnConfirmTime` 0.10s(60fps 기준 ≈6프레임)에는 무해하나, 이보다 짧은 판정 윈도우를 설계할 때는 고려해야 한다.

---

## 3. 웹 조사 근거

| 출처 | 값 | 시사점 |
|---|---|---|
| **Lyra** | Pivot = `dot(localVelocity2D, localAccel2D) < 0` (변형 구현 `< -0.5`) | 방향 반전 감지. dot −0.5 ≈ **120°** |
| **Lyra** | TurnInPlace 발동 **50°** yaw offset / Start→Cycle 게이트 60° | 이건 **정지 상태** 제자리 회전용 |
| **Lyra** | Start 탈출 = 경과 **0.15s** AND 속도 < 10uu/s, + `AutomaticRule` 안전망 (우선순위 2~3) | **상태 고착 방지 규칙이 필수 설계 요소** |
| **Lyra** | Pivot Recovery — 방향이 초기 피벗과 수직이 되면 전체 재생을 포기하고 Cycle 복귀 | 조기 탈출이 정상 동작 |
| **ALS V4** | `TurnCheckMinAngle` **45°**, `Turn180Threshold` **130°** | 130° 부근이 "전신 턴" 경계 |
| **ALS V4** | `MinAngleDelay` 0.0 / `MaxAngleDelay` **0.75s** | 각도가 작을수록 발동을 지연 |
| **ALS V4** | `TriggerPivotSpeedLimit` **200uu/s (2m/s)** | 피벗은 속도 밴드로 게이트 |
| **ALS V4** | `RotateInPlace` PlayRate **1.15 ~ 3.0** 클램프 | 재생속도 스케일에 상·하한 필수 |
| **ALS V4** | `AnimatedWalk/Run/SprintSpeed` = **150 / 350 / 600 uu/s** | **클립 기준 속도를 데이터로 보유** → 재생속도 = 실속도 / 기준속도 |
| **Animotionx** (프로덕션 애니메이터) | *"45°/90° 턴은 제어를 끊고 입력을 유예시킨다 — 뱅킹으로 처리 가능한 각도에 침습적 애니메이션을 강제하는 것"*, 대부분 프로젝트는 **180°를 주 트리거**로 | 낮은 임계값 반대 근거 |
| **Unity Input System** | Stick Deadzone 기본 min **0.125** / max **0.925**, radial 권장 | D-7 해법 |

### 3.1 TurnInPlace 임계값 결정: **135°**

**결정 근거**

1. 우리 `PlayerTurnInPlaceState`는 `OnMotionSetCompleted`까지 상태를 점유하고 루트모션으로 속도·회전을 전부 대체하는 **침습적 전신 모션**이다(`PlayerTurnInPlaceState.cs:95-108`). Animotionx가 지적한 *"제어를 끊고 입력을 유예시키는"* 형태에 정확히 해당한다.
2. 따라서 45°/90° 발동은 **조작감을 굼뜨게 만드는 순손실**이다. 이 대역은 뱅킹(속도 비례 선회 감쇠, P1-3)이 담당해야 한다.
3. 업계 임계값은 120°(Lyra dot −0.5) ~ 130°(ALS `Turn180Threshold`)에 수렴한다. 우리 클립 세트가 45/90/180 3단이고 `GetTurnAnimKey`의 분기가 `<67.5 → 45`, `<135 → 90`, `else → 180`(`PlayerTurnInPlaceState.cs:127-143`)이므로, **135°를 임계로 잡으면 180 계열만 사용**되어 웹 조사의 "180을 주 트리거로" 관례와 정확히 일치한다.

**따라서 P1의 기본값은 `TurnTriggerAngle = 135°`.** 45°/90° Turn 클립 10종은 P1에서 **의도적으로 미사용**이며, 이는 결함이 아니라 위 3항의 결론이다.

**튜닝 여지(문서화):** 임계를 **120°**로 낮추면 120~135° 구간이 90 계열 클립을 사용하게 되어 Lyra의 dot −0.5와 등가가 된다. 플레이테스트 후 조정 가능하도록 인스펙터 노출한다. **90° 미만으로는 내리지 말 것** — 위 1·2항 근거.

### 3.2 1번과 5번은 경쟁이 아니라 한 쌍

- **< 135°** → 뱅킹: 속도 비례로 감쇠된 연속 회전 (P1-3)
- **≥ 135°** → 커밋: Turn 전신 모션 (P1-2)

두 항목을 따로 넣으면 각각 반쪽짜리가 된다. **P1-2와 P1-3은 동일 Phase에서 함께 구현·튜닝한다.**

---

## 4. 설계 원칙

- **P1(원칙 1) 신규 에셋 0 우선.** 이미 저작된 Stop 9 / Turn 15를 먼저 회수한다.
- **P2 커밋 구간에는 반드시 탈출구가 있다.** Lyra의 `AutomaticRule`처럼, 모든 침습적 상태는 ① 입력 이탈 ② 하드 타임아웃 ③ 액션 캔슬 세 가지 탈출을 갖는다.
- **P3 한 번에 하나씩 켠다.** 가감속·선회·애니속도를 동시에 튜닝하면 원인 분리가 불가능하다(§8).
- **P4 소유권을 명시한다.** `Animator.Speed`, `MoveAnimType`, 회전 모드는 현재 소유자가 없다. 각 항목에 단일 소유자를 지정한다.
- **P5 몬스터를 오염시키지 않는다.** 신규 파라미터는 플레이어 전용 상태에서만 소비하고, 기존 5개 공유 파라미터 값은 건드리지 않는다.

---

## 5. 불변 규약 (구현 시 반드시 준수)

| ID | 규약 | 근거 |
|---|---|---|
| **R-1** | 새 상태 전환은 `TryTransitionToState`를 쓰고 **반환값을 확인**한다. `TransitionToState`는 void이며 같은 타입 재진입(`ActorMovementController.cs:201-207`), `PlayerFinishAttackState` 잠금(`:209-212`), 사망 잠금(`:214-220`)에서 **조용히 무시**된다. 호출측이 무조건 `return`하면 그 프레임의 애니 갱신·Sprint 타이머가 통째로 스킵된다. | `ActorMovementController.cs:191-234` |
| **R-2** | `OnEnter` 안에서 다른 상태로 전환하지 않는다. `:229` 대입 → `:232` OnEnter → `:233` 이벤트 발화 순서 때문에 `OnStateChanged` 구독자가 **(2세대 전 상태 → 최종 상태)** 쌍을 받고 중간 상태 이벤트가 유실된다. 실패는 플래그만 세우고 **다음 `UpdateState`에서 탈출**한다. | `PlayerStopState.cs:58`, `PlayerTurnInPlaceState.cs:47`이 이미 위반 |
| **R-3** | 모션 종료 판정에 **전역 `OnMotionSetCompleted`를 단독으로 쓰지 않는다.** 재생 시 반환된 `AnimancerState` 참조 대조 + 하드 타임아웃을 병행한다. | D-10 |
| **R-4** | `Animator.Speed`에 값을 쓸 때는 **반드시 `* gameActor.LocalTimeScale`을 곱한다.** `GameActor.cs:100`이 `_animator.Speed = _localTimeScale`로 무조건 덮어쓰며, `PlayerAttackState.cs:870-871`이 이 컨벤션의 기준이다. | D-10 계열 |
| **R-5** | 로코모션 속도 동기화에 **`MotionTimelineSpeed`를 쓰지 않는다.** 이 값은 모션 종료 시각과 이벤트 발화 시각까지 스케일하므로(`ActorAnimator.cs:205, :275`) Stop/Turn의 **상태 지속 시간이 이동 속도에 따라 변한다.** `Speed`를 쓰되 R-4를 지킨다. | §6 P3 |
| **R-6** | 상태 판별에 문자열 하드코딩을 새로 추가하지 않는다. `PlayerCombatWeaponStateController.cs:162`가 `"Idle" or "GroundMove" or "Stop" or "TurnInPlace"` 화이트리스트를 갖고 있고, **`:171-179`의 복귀 switch에는 `"Stop"`/`"TurnInPlace"` case가 빠져 있다**(기존 불일치 버그). `GameActorState.cs:14, 82-89`의 `ActorStateTag.Locomotion` 인프라가 이미 존재하나 미사용 — 이쪽으로 교체한다. | R-6 |
| **R-7** | `Animator`는 `ActorAnimator`와 **동일 GameObject**에 있어야 한다. `ActorAnimator.cs:301`의 `GetComponentInChildren`이 자식을 잡으면 루트모션이 이중 적용된다. `Awake`에 검증 로그를 추가한다. | D-12 |
| **R-8** | 상태 전환마다 `FlushRootMotion()`이 호출되어 **그 프레임 누적 루트모션이 버려진다**(`ActorMovementController.cs:226`). Stop/Turn 전환 빈도를 늘리면 미세 위치 스냅이 누적되므로, 전환 전 프레임의 델타 처리를 검증한다. | D-12 |
| **R-9** | `UpdateVelocity`는 상태 위임 **이후에** impulse·`_internalVelocityAdd`를 더한다(`ActorMovementController.cs:264-287`). 상태가 속도를 통째로 덮어쓰는 현재 구조(`PlayerGroundMoveState.cs:219`)를 "가속도 적분" 방식으로 바꾸면 넉백과의 상호작용이 달라진다. **넉백 회귀 테스트 필수.** | §6 P2 |
| **R-10** | 기존 공유 파라미터 5개(`MaxWalk/Run/SprintMoveSpeed`, `StableMovementSharpness`, `OrientationSharpness`)의 **코드 기본값을 바꾸면 몬스터·NPC 전 프리팹이 함께 바뀐다**(D-11). 신규 파라미터 추가는 안전(플레이어 전용 상태에서만 소비)하나, 기존 5개 튜닝은 프리팹 오버라이드 또는 프로파일 분리 후에 한다. | D-11 |
| **R-11** | **루트모션 상태 중 `Motor.Velocity`를 판정·연출의 입력으로 직접 쓰지 않는다.** Animator(Update)와 KCC(FixedUpdate)의 레이트 불일치로 물리 스텝마다 `0 ↔ 과대값`으로 진동한다(D-14). 순 변위는 보존되므로 **위치 기반 판정은 안전**하나, 속도 임계값·카메라 연출·애니 재생속도처럼 **순간 속도를 읽는 소비자는 스무딩된 값이나 이동 의도 속도를 쓴다.** | D-14 |
| **R-12** | **루트모션으로 속도를 완전 대체하는 상태는 `OnExit`에서 평면 속도를 결정적 값으로 시딩한다.** 시딩하지 않으면 이탈 속도가 마지막 물리 스텝의 기아 여부에 좌우되어 같은 입력에 다른 결과가 나온다(D-15). | D-15 |
| **R-13** | **클립이 만드는 루트모션 총량(이동거리·회전량)을 런타임 판정의 전제로 삼는 경우, 그 총량은 반드시 베이크된 데이터여야 한다.** 하드코딩하거나 런타임에 매번 추정하지 않는다. 베이크 도구는 P0-5. | D-13, P3-2 |

---

## 6. Phase별 사양

### 의존 그래프

```
P0-1 입력 데드존·스무딩 ─┬─→ P1-1 Stop 확대 ─┐
P0-2 모션 종료 판정 교체 ─┤                    ├─→ (동시 튜닝)
P0-3 시간축 통일 ─────────┤  P1-2 TurnInPlace ─┤
P0-5 루트모션 베이크 ─────┘  P1-3 뱅킹 선회 ────┘
      │                        ↑
      │   (총회전량 → P1-2 회전 수렴 B1의 필수 입력)
      └──────────────→ P3-2 기준속도 테이블 (같은 베이크 산출물)

P1-4 MoveAnimType 소유권 ───→ P1-1 (선행)
P1-5 루트모션 이탈 속도 시딩 ─→ P1-1 / P1-2 공통 (선행)

P2 가감속 분리 ──→ (P1 안정화 후 단독 튜닝)

P3-1 Speed 소유권 → P3-2 (P0-5 산출물 소비) → P3-3 재생속도 동기화

P4-1 Idle 회전 (독립)
P4-2 8방향 스트레이프 ← 애니메이션 저작 선행 (코드 아님)
```

> **정밀 검토(2026-07-28)로 변경된 순서:** 당초 P3-2에 있던 루트모션 베이크가 **P0-5로 앞당겨졌다.** P1-2의 회전 수렴(B1)이 "클립 총 회전량" 데이터를 요구하는데, 이는 P3-2의 "클립 기준 이동속도"와 **동일한 베이크 인프라의 산출물**이기 때문이다. 두 값을 한 번에 뽑는다.

---

### P0 — 기반 정리 (선행 필수)

P1을 안전하게 켜기 위한 전제 조건. 단독으로도 무해하며 개별 회귀 위험이 낮다.

#### P0-1. 입력 데드존 · 스무딩 · 릴리즈 유예

**문제.** `HasMoveInput() => sqrMagnitude > 0`(`PlayerMovementController.cs:181`)이라 스틱 드리프트가 곧 이동이고, 더 중요하게는 **스틱을 180° 급히 꺾을 때 아날로그 값이 원점을 통과하는 1~2프레임 동안 입력이 false가 된다.** 이 프레임에 Stop이 오발동하고, 다음 프레임에 입력이 돌아와 `PlayerStopState.cs:77-81`이 즉시 GroundMove로 되돌린다 → **Stop 클립 1~2프레임 깜빡임.**

> **이것이 P1-1(Stop 확대)과 P1-2(TurnInPlace)의 최대 장애물이다. 반드시 먼저 고친다.**

**사양.**

1. **에셋 측 (Unity 에디터 수작업).** `PlayerInputActions`의 `Move` 액션에 **Stick Deadzone** 프로세서를 추가한다(Axis Deadzone 아님 — radial 필수). min `0.125`, max `0.925` (Unity 기본값).
2. **코드 측 방어선.** `PlayerMovementController`에 임계값을 노출한다.
   ```
   MoveInputDeadzone = 0.15f      // HasMoveInput 판정 임계
   ```
   `HasMoveInput()` → `_moveInputVector.sqrMagnitude > MoveInputDeadzone * MoveInputDeadzone`
3. **릴리즈 유예 (코요테 윈도우).**
   ```
   MoveInputReleaseGrace = 0.08f  // 초
   ```
   `SetInputs`에서 데드존을 통과한 마지막 시각을 `_lastMoveInputTime`에 기록하고,
   `HasMoveInputBuffered()` = `HasMoveInput() || (Time - _lastMoveInputTime) < MoveInputReleaseGrace` 를 신설한다.
   **`PlayerGroundMoveState`의 정지 판정(`:108`)만** 이 버퍼 버전을 쓴다. 다른 소비자는 즉시 버전 유지(반응성 보존).
4. **크기 스무딩 (방향은 raw 유지).**
   ```
   MoveInputSmoothTime = 0.06f    // 0이면 비활성
   ```
   `SetInputs`에서 **입력 벡터의 크기(magnitude)에만** 지수 스무딩을 적용하고 **방향은 원본을 유지**한다.
   → 키보드의 0↔1 계단식 속도 점프를 제거하면서 방향 반응 지연은 0으로 유지한다. 이것이 이 설계의 핵심 선택이다.
5. **`_lookInputVector` 재설계.** 현재 `sqrMagnitude > 0f`(`:168-171`) → 데드존 통과 시에만 갱신하도록 변경. 단 `PlayerStopState`가 이 잔존값을 정지 방향으로 소비하므로(`PlayerGroundMoveState.cs:115`), **정지 방향 전용으로 `LastMoveDirection`을 별도 유지**하고 Stop 진입 시 그쪽을 넘긴다.

**수용 기준.**
- 게임패드 스틱을 놓은 채 방치 → 캐릭터가 미동도 하지 않는다.
- 스틱을 좌↔우로 빠르게 반복 왕복 → Stop 클립이 한 번도 깜빡이지 않는다.
- 키보드 W를 눌러 출발 → 속도가 계단이 아니라 램프로 오른다.
- 스틱을 반만 기울여 방향 전환 → 방향 응답에 체감 지연이 없다.

**회귀 확인.** Dodge/Dash/Attack의 방향 결정이 `MoveInputVector`/`LookInputVector`에 의존하므로(`PlayerActorState.ResolveDirectionalMotionKey`), 4방향 회피가 의도 방향으로 나가는지 확인.

#### P0-2. 모션 종료 판정 교체 (D-10 버그 수정)

**사양.**

`PlayerStopState` / `PlayerTurnInPlaceState`가 전역 `OnMotionSetCompleted`를 구독하는 구조(R-3 위반)를 교체한다.

1. `OnEnter`에서 `PlayMotion`이 반환한 `AnimancerState`를 필드에 **보관**한다.
2. 종료 판정은 다음 중 하나가 참일 때:
   - 보관한 `AnimancerState`가 여전히 현재 재생 상태이고 `NormalizedTime >= 1.0`
   - **또는** 상태 진입 후 경과 시간이 `클립 길이 × 1.5 + 0.1s`를 초과 (하드 타임아웃)
3. 전역 `OnMotionSetCompleted` 구독은 **보조 신호로만** 남기되, 발화 시 보관한 `AnimancerState`와 대조해 자기 모션이 아니면 무시한다.
4. 종료 전환은 `UpdateState` 안에서 수행한다 (R-2 — 콜백 안에서 전환 금지).

> **대안 검토.** `animState.OwnedEvents.OnEnd`(`PlayerIdleState.cs:176`에서 사용 중인 관용구)가 모션별 정확한 종료 신호다. 다만 MotionSet이 여러 클립을 체이닝하는 경우 마지막 클립 기준인지 검증이 필요하다. Stop/Turn은 단일 클립 MotionSet이므로 이쪽이 더 간결하다 — **Unity에서 단일 클립임을 확인한 뒤 `OwnedEvents.OnEnd` + 하드 타임아웃 조합을 우선 채택**하고, 체이닝이 확인되면 위 2번 방식으로 간다.

**수용 기준.** Stop 재생 도중 무기 뽑기/넣기(`Equip` 키)를 눌러도 Stop 클립이 끝까지 재생된다.

#### P0-3. 시간축 통일

**사양.** `Time.realtimeSinceStartup` / `Time.deltaTime` 사용처를 `Actor.DeltaTime`(로컬 타임스케일 반영, `ActorMovementController.cs:146`)으로 교체한다.

| 위치 | 현재 | 변경 |
|---|---|---|
| `PlayerGroundMoveState.cs:37, :174` Sprint 타이머 | `Time.realtimeSinceStartup` 절대시각 비교 | `deltaTime` 누산 (`_sprintTimer += deltaTime`) |
| `PlayerMovementController.cs:121` 대시 쿨다운 | `Time.deltaTime` | `Actor.DeltaTime` |

**수용 기준.** 일시정지 메뉴를 3초 이상 열었다 닫아도 스프린트가 자동 발동하지 않는다. 히트스톱 중 대시 쿨다운이 함께 멈춘다.

#### P0-4. (선택) 플레이어 전용 이동 프로파일 분리

**판단: P1~P3에는 불필요하다.** 신규 파라미터는 `PlayerGroundMoveState`/`PlayerIdleState`(플레이어 전용 상태)에서만 소비되므로 몬스터에 영향이 없다.

**필요해지는 시점:** 기존 공유 5개 값(속도 3종, `StableMovementSharpness`, `OrientationSharpness`)을 실제로 튜닝하기로 결정할 때(R-10).

**그때의 사양:** `ActorLocomotionProfileSO` 신설. `CreateAssetMenu` 경로는 프로젝트 규약(2단계 flat 도메인)에 따라 **`UPlayGround/Actor/Locomotion Profile`**. `ActorMovementController`가 `[SerializeField] ActorLocomotionProfileSO _locomotionProfile`을 갖고, **null이면 기존 인스펙터 필드로 폴백**해 하위호환을 보장한다.

#### P0-5. 루트모션 베이크 도구 확장 (**P1-2 / P3-2의 필수 선행**)

**필요 이유.** 두 곳이 "클립이 만드는 루트모션 총량"을 요구한다 (R-13).

| 소비처 | 필요 데이터 | 용도 |
|---|---|---|
| **P1-2 회전 수렴 (B1)** | 클립 총 회전량 `clipTotalYaw` (도) | 실제 필요 각도 / 클립 회전량 = 회전 스케일 계수 |
| **P3-2 기준 이동속도** | 클립 총 이동거리 / 길이 = `referenceClipSpeed` (m/s) | 실제 속도 / 기준 속도 = 애니 재생속도 |

**기반 인프라는 이미 있다.** `02.Scripts/Editor/MotionSetWindow.WarpBake.cs`가 모션워프용으로 루트모션을 누적 샘플링한다(`_warpBakeAccums`, `RootMotionTotal`, `WarpBakeTick`). 같은 샘플링 루프에서 **회전 누적과 이동거리 누적을 함께 산출**하면 된다.

**사양.**

1. `MotionSetWindow.WarpBake`의 샘플링에 회전 누적(`Quaternion` 누적 → 최종 yaw 각도)과 평면 이동거리 누적을 추가한다.
2. 산출 결과를 저장할 위치:
   - **`referenceClipSpeed`** → §P3-2 채택안대로 `ActorMovementController`(또는 프로파일 SO)의 3값 테이블에 기록. 에디터 버튼으로 자동 채움.
   - **`clipTotalYaw`** → Turn 모션 15종에 각각 필요하므로 3값 테이블로는 부족하다. **`ActorAnimationMotionSet`에 `SerializedDictionary<GameplayTag, float> motionRootYaw` 를 추가**한다(P3-2에서 "8방향 도달 시 필연"이라 판단했던 딕셔너리 방식을 Turn 한정으로 먼저 도입).
3. 베이크는 **에디터 전용**이며 런타임 추정 경로를 만들지 않는다 (R-13).
4. 값이 없는 클립(미베이크)에 대한 폴백을 정의한다 — 회전 스케일 1.0(= 현행 동작), 재생속도 1.0. **폴백 시 경고 로그를 남긴다.**

**수용 기준.** Turn 클립 15종과 Walk/Run/Sprint 기본 클립에 대해 베이크 버튼 1회로 값이 채워지고, 재실행 시 동일 값이 재현된다.

> **⚠️ 주의.** 메모리에 기록된 *"MotionSetEditor 루트모션 프리뷰 — Player 이중적용 리스크"* 가 이 도구와 같은 영역이다. 에디터 프리뷰 락(`ActorAnimator._externalPreviewLockCount`)은 `PlayMotion`만 막고 `OnAnimatorMove` 누적은 막지 않으므로, **베이크 중 플레이 모드가 동시에 돌지 않도록** 보장한다.

---

### P1 — 정지 / 선회 자연스러움 (핵심)

**신규 에셋 0. 이미 저작된 Stop 9종 · Turn 15종을 회수한다. 체감 효과가 가장 크다.**

#### P1-4. `MoveAnimType` 소유권 정리 (P1-1의 선행)

**문제.** `GameActor.cs:111`의 public setter를 6곳이 쓴다. 소유자가 없다.

| 위치 | 쓰기 | 문제 |
|---|---|---|
| `PlayerGroundMoveState.cs:49` (OnExit) | → Run | **무조건 리셋.** 공격·회피·점프 한 번이면 스프린트 소멸 |
| `PlayerGroundMoveState.cs:176` | → Sprint | 자동 전환 |
| `PlayerDashState.cs:84` (OnExit) | → Sprint | 대시 후 항상 스프린트 |
| `PlayerDashAttackState.cs:31` | → Run | |
| `PlayerActor.Input.cs:89` | Walk ↔ Run 토글 | |
| `PlayerActor.Input.cs:93` | Sprint ↔ Run 토글 | `StateName == "GroundMove"` 게이트 (R-6 위반) |

**사양.**

1. **`OnExit`의 무조건 Run 리셋을 조건부로 바꾼다.** `OnExit(GameActorState toState)`는 목적지를 받으므로:
   - `toState`가 로코모션 계열(Idle / Stop / TurnInPlace / GroundMove) → **`MoveAnimType` 유지**
   - 그 외(전투 / 공중 / 피격) → Run으로 강등 (기존 동작)
2. **Sprint 자동 전환 재무장.** `_runTimer = float.MaxValue`(`:178`) 대신 `bool _sprintArmed` 플래그를 쓰고, 유저가 Sprint 키로 Run으로 되돌리면 **재무장**한다.
3. `PlayerActor.Input.cs:93`의 문자열 게이트를 `ActorStateTag.Locomotion` 기반으로 교체(R-6) → Stop/TurnInPlace 중에도 Sprint 키가 먹는다.
4. 사양서 부록 A에 최종 소유권 표를 갱신 반영한다.

**수용 기준.** 스프린트 중 공격 1회 → 공격 종료 후 여전히 스프린트. 스프린트 중 Sprint 키로 해제 → 3초 후 자동 스프린트가 다시 걸린다.

#### P1-5. 루트모션 상태 이탈 속도 시딩 (P1-1 / P1-2 공통 선행)

**문제 (D-15).** `PlayerStopState` / `PlayerTurnInPlaceState`는 평면 속도를 루트모션으로 완전 대체하므로, 이탈 시점의 `currentVelocity`가 **마지막 물리 스텝의 기아 여부에 좌우된다.** 같은 입력에 다른 결과가 나온다.

**사양 (R-12).** 두 상태의 `OnExit`에서 평면 속도를 결정적 값으로 시딩한다.

```
OnExit(toState):
    if (toState 가 GroundMove)   → 평면속도 = LookInputVector * GetMaxMovementSpeed(MoveAnimType)
    if (toState 가 Idle)         → 평면속도 = 0
    그 외(공격/회피/공중 등)      → 시딩하지 않음 (목적지 상태가 자체적으로 속도를 정의)
    수직 성분은 항상 보존         → ActorVelocityUtility.ReplacePlanarPreserveVertical 재사용
```

**구현 주의.** `OnExit`은 KCC 콜백이 아니라 `TransitionToState`(`ActorMovementController.cs:225`) 안에서 호출되므로 `ref currentVelocity`에 접근할 수 없다. `ActorMovementController`에 **1회성 시드 값**을 예약해 두고, 다음 `UpdateVelocity`의 상태 위임 **직전**에 적용한 뒤 소비한다. `_internalVelocityAdd`(`:274-281`)와 달리 **가산이 아니라 평면 대체**이므로 별도 필드로 둔다.

**수용 기준.** 동일한 스틱 조작으로 턴/정지를 20회 반복했을 때, 이탈 직후 속도 편차가 프레임 타이밍과 무관하게 일정하다 (F9 HUD로 계측).

#### P1-1. Stop 상태를 Run / Walk까지 확대

**사양.**

`PlayerGroundMoveState.cs:110-121`을 다음으로 대체한다.

```
정지 판정 (HasMoveInputBuffered() == false):        // P0-1의 버퍼 버전 사용
    stopKey = GetStopAnimKey(MoveAnimType, 정지방향각)
    if (Animator.HasMotion(stopKey, true))
        → PlayerStopState(controller, MoveAnimType, LastMoveDirection)
    else if (Animator.HasMotion(GetStopAnimKeyForward(MoveAnimType), true))
        → PlayerStopState (전방 클립 폴백 — 기존 PlayerStopState.cs:45-49가 이미 처리)
    else
        → PlayerIdleState
```

즉 **`&& MoveAnimType == Sprint` AND 절을 제거**하고, 방향별 키까지 사전 검사한다. 정지 방향은 P0-1에서 신설한 `LastMoveDirection`을 넘긴다.

**추가 게이트 (속도 하한).** 아주 느린 속도에서 정지 모션은 오히려 부자연스럽다.
```
MinStopSpeed = 1.5f   // m/s. 평면 속도가 이 미만이면 Stop 생략, Idle 직행
```
근거: ALS `TriggerPivotSpeedLimit`(2m/s)과 같은 계열의 속도 밴드 게이트. Walk 최대속도 3의 절반.

**Stop 중 Sprint 강등 방지.** `PlayerStopState.cs:79`가 GroundMove로 복귀할 때 `MoveAnimType`이 이미 Run으로 리셋되어 있으면 **"스프린트 중 잠깐 멈췄다 다시 달리면 Run으로 강등"** 된다. P1-4의 1번 항목이 이를 해소하므로 **P1-4를 먼저 넣는다.**

**수용 기준.**
- Run으로 달리다 손을 떼면 정지 모션이 재생된 뒤 Idle로 간다.
- 좌/우 45° 방향으로 달리다 정지하면 L45/R45 클립이 선택된다.
- 걷다가(속도 1.5 미만 구간) 멈추면 Stop 없이 자연스럽게 Idle.
- Stop 도중 이동 입력 복귀 → 즉시 GroundMove (반응성 손실 0).
- Stop 도중 공격/대시/회피/가드 → 즉시 캔슬 (기존 `PlayerStopState.cs:83-127`이 이미 처리).

#### P1-2. TurnInPlace 활성화 — 임계 135°

**사양.**

`PlayerGroundMoveState.UpdateState`에 진입 판정을 추가한다.

**삽입 위치: 정지 판정 블록(`:108-122`) 바로 다음, Guard 판정(`:124`) 앞.**
근거 — TurnInPlace는 "이동 입력이 있는 상태"의 분기이므로 `HasMoveInput()` 통과 이후가 논리적으로 맞고, 실제로 입력이 끊긴 경우(Stop)가 우선되어야 한다.

**진입 조건 (전부 AND):**

| 조건 | 값 | 근거 |
|---|---|---|
| `Vector3.Angle(CharacterForward, MoveInputVector) >= TurnTriggerAngle` | **135°** | §3.1 |
| 평면 속도 `>= MinTurnSpeed` | **3.0 m/s** (Run 최대속도의 ~46%) | ALS `TriggerPivotSpeedLimit` 계열 속도 밴드. 걷기 중 급전환은 뱅킹이 담당 |
| 위 각도 조건이 `TurnConfirmTime` 이상 **연속 유지** | **0.10s** | ALS `MaxAngleDelay`(0.75s)의 축소판. 스틱 플릭 오발동 방지 |
| 마지막 TurnInPlace 종료 후 `TurnReentryCooldown` 경과 | **0.30s** | 핑퐁 방지 (아래) |
| `Animator.HasMotion(GetTurnAnimKey(...))` | — | 클립 없으면 진입 안 함 (R-2 준수: OnEnter에서 되돌리지 않고 사전 차단) |
| **전방 `TurnClearance` 내에 벽이 없을 것** | **1.2 m** | **B3** — 아래 |

진입 판정에 쓰는 "평면 속도"는 GroundMove(속도 구동 상태)의 값이므로 D-14의 버스트 영향을 받지 않는다. 다만 R-11에 따라 **루트모션 상태 내부에서는 이 값을 재판정에 쓰지 않는다.**

전환은 `TryTransitionToState`로 하고 반환값을 확인한다 (R-1).

**B1 (차단) — 회전 수렴.**

D-13대로 현재 `UpdateRotation`은 루트모션을 그대로 누적할 뿐 목표 방향으로 수렴하지 않아 최대 45° 오버슈트가 발생한다. **수정 없이 활성화하면 안 된다.**

```
진입 시:
    requiredYaw  = |SignedAngle(CharacterForward, _targetDirection)|
    clipTotalYaw = ActorAnimationMotionSet.motionRootYaw[선택된 Turn 키]   // P0-5 베이크
    rotationScale = clamp(requiredYaw / clipTotalYaw, TurnRotationScaleMin, TurnRotationScaleMax)

매 UpdateRotation:
    scaled = Slerp(identity, RootMotionStepDeltaRotation, rotationScale)
    currentRotation = currentRotation * scaled

종료 직전(진행도 > 0.85):
    잔여 오차를 목표 방향으로 짧은 slerp로 정리 (클램프에 걸려 남은 각도 흡수)
```

```
TurnRotationScaleMin = 0.6
TurnRotationScaleMax = 1.4
```

클램프 근거: ALS `RotateInPlace`가 PlayRate를 1.15~3.0으로 제한하는 것과 같은 취지 — 스케일이 과하면 발 접지가 눈에 띄게 미끄러진다. 트리거 각도가 135°이고 클립이 180°이므로 실제 필요 스케일은 **0.75~1.0** 구간에 들어와 클램프에 거의 닿지 않는다. 클램프에 걸린 잔여분만 마지막 slerp가 흡수한다.

> **대안 기각.** 재생속도(PlayRate) 스케일링은 발 접지를 보존하지만 **회전 총량을 바꾸지 못하므로 B1을 해결하지 못한다.** 회전 델타 스케일링이 필수다.

**B2 (차단) — 액션 캔슬 누락.**

현재 `PlayerTurnInPlaceState.UpdateState`(`:57-93`)에는 Jump / Dodge / Dash **3개뿐**이고 **Attack / HeavyAttack / Charge / Guard / Skill이 전부 없다.** Turn 클립은 0.5~1.0초이므로 그동안 `InputBuffer`의 공격 입력이 버퍼 시간(0.15~0.3초)을 넘겨 **조용히 만료**된다. 급선회 직후 공격이 씹히는 것은 액션 게임에서 치명적이다.

`PlayerGroundMoveState`의 Guard(`:124`) / Attack(`:130`) / Charge(`:137`) / HeavyAttack(`:143`) / Skill(`:158`) 판정을 **동일 순서로** 이식한다.

> **⚠️ 기존 버그를 복제하지 말 것.** `PlayerGroundMoveState.cs:130`은 `ConsumeInput()`으로 **먼저 소비한 뒤** `TryEnter`를 호출해, 진입 실패 시 입력이 증발한다. `:143`의 HeavyAttack 쪽이 `HasInput`으로 확인만 하고 TryEnter가 내부에서 소비하는 올바른 패턴이다. **TurnInPlace에는 HeavyAttack 패턴을 쓴다.**

**B3 (차단) — 벽 충돌 시 제자리 미끄러짐.**

Turn 클립에는 상당한 전방 변위가 포함된다. 벽을 향해 턴하면 KCC가 이동을 막지만 **클립은 계속 재생되고 회전은 완료**되므로 제자리에서 발이 미끄러진다. Lyra가 피벗 조건에 `AND !IsRunningIntoWall`을 명시한 이유다.

현재 `ProcessHitStabilityReport`(`ActorMovementController.cs:340-343`)와 `OnDiscreteCollisionDetected`(`:349-351`)가 **빈 구현**이라 벽 접촉을 감지할 경로 자체가 없다. `OnMovementHit`(`:329-333`)은 상태로 위임만 한다.

**사양:** 진입 조건에 전방 `SphereCast` 게이트(`TurnClearance = 1.2m`)를 추가한다. 추가로 `PlayerTurnInPlaceState`가 `OnMovementHit`을 오버라이드해 진행 방향 벽 접촉을 감지하면 **조기 종료**한다(탈출구 4번).

**핑퐁 방지 — 이 설계의 핵심 안전장치.**

`PlayerTurnInPlaceState.cs:84-91`은 입력이 목표 방향에서 **45° 벗어나면** GroundMove로 나간다. 그런데 GroundMove가 같은/다음 프레임에 다시 135° 임계를 넘겼다고 판단하면 즉시 재진입한다. **GroundMove↔TurnInPlace는 서로 다른 타입이라 `TransitionToState`의 동일 타입 재진입 가드가 이를 막지 못한다.** → Turn 클립 앞 몇 프레임만 무한 반복 재생.

3중 방어:
1. **히스테리시스** — 진입 135°, 이탈은 기존 45°가 아니라 **90°** 로 완화 (`PlayerTurnInPlaceState.cs:88`의 `> 45f` → `> TurnAbortAngle(90f)`)
2. **재진입 쿨다운** — `TurnReentryCooldown = 0.30s`. `PlayerMovementController`가 소유(상태는 매번 새로 생성되므로 상태에 둘 수 없다).
3. **최소 체류 시간** — 진입 후 `TurnMinDuration = 0.12s` 동안은 각도 이탈로 인한 조기 탈출을 금지 (Lyra Start 상태의 0.15s와 같은 계열).

**탈출구 3종 (설계 원칙 P2).**

| 탈출 | 조건 |
|---|---|
| 입력 이탈 | 목표 방향에서 90° 초과 이탈 + 최소 체류 시간 경과 (기존 `:84-91` 수정) |
| 하드 타임아웃 | 진입 후 `클립 길이 × 1.5 + 0.1s` 초과 → 무조건 GroundMove (Lyra `AutomaticRule` 대응) |
| 액션 캔슬 | 점프/회피/대시(기존 `:57-81`) **+ B2로 추가되는 공격/강공격/차지/가드/스킬** |
| **벽 접촉** | B3 — `OnMovementHit`에서 진행 방향 벽 감지 시 조기 종료 |

**클립 선택.** `GetTurnAnimKey`(`:122-144`)는 그대로 사용. 임계 135°이므로 실질적으로 180 계열만 선택된다 — §3.1의 의도된 결과.

**재생 속도 스케일 (P1 범위 밖).** 회전 총량 문제는 B1의 **회전 델타 스케일링**으로 해결하므로, 재생속도 스케일링은 P1에 넣지 않는다. 발 접지 개선 목적의 재생속도 조정은 R-4/R-5 제약 아래 P3에서 다룬다.

**수용 기준.**
- 전력질주 중 스틱을 정반대로 꺾으면 180° Turn 모션이 재생되고, 발이 지면을 딛으며 회전한다.
- **턴 종료 시점의 캐릭터 정면이 목표 방향과 ±5° 이내로 일치한다** (B1 — 오버슈트 후 되돌아오는 이중 회전이 없다).
- 90° 방향 전환에서는 Turn이 발동하지 않고 뱅킹(P1-3)으로 부드럽게 돌아간다.
- 스틱을 빠르게 좌우로 흔들어도 Turn 클립이 반복 재시작하지 않는다.
- **Turn 중 공격·가드·스킬 입력이 즉시 반영된다** (B2 — 버퍼 만료로 씹히지 않는다).
- **벽을 향해 급선회해도 제자리 발 미끄러짐이 발생하지 않는다** (B3).
- 턴 종료 후 GroundMove 복귀 속도가 프레임 타이밍과 무관하게 일정하다 (P1-5).
- Turn 클립이 없는 캐릭터(무기 타입)에서 Turn이 진입 시도조차 하지 않는다.
- `motionRootYaw` 미베이크 클립에서는 회전 스케일 1.0으로 폴백하고 경고 로그가 남는다 (P0-5).

#### P1-3. 뱅킹 — 속도 비례 선회 감쇠

**P1-2와 한 쌍이다** (§3.2). 135° 미만 전 대역을 담당한다.

**사양.** `PlayerGroundMoveState.UpdateRotation`(`:182-198`)의 고정 `OrientationSharpness`를 속도 비례로 감쇠한다.

```
speedRatio = clamp01(평면속도 / MaxSprintMoveSpeed)
sharpness  = Lerp(OrientationSharpness, OrientationSharpness * SprintOrientationScale, speedRatio)
```

신규 파라미터 (`ActorMovementController`에 추가 — 플레이어 상태에서만 소비되므로 몬스터 무영향, R-10):
```
SprintOrientationScale = 0.35f   // 최고 속도에서의 선회 계수 배율
```

효과: 정지~걷기에서는 기존과 동일한 즉각 선회, 스프린트에서는 sharpness 10 → 3.5로 감쇠되어 **큰 호를 그리며 도는 관성**이 생긴다. 그리고 이 관성 때문에 급격한 방향 전환 시 각도차가 135°를 넘어 **P1-2의 Turn이 자연스럽게 발동**한다.

**⚠️ 회귀 주의.** `ICameraVelocityProvider.CameraVelocity`(`ActorMovementController.cs:104`)가 `Motor.Velocity` 원본을 그대로 노출하고 `CameraManager.cs:378-380`이 이를 소비한다. 선회를 둔화시키면 카메라 연출(`DistanceFovCameraModifier`, `CameraDistanceController`, `FollowCameraModifier`)이 함께 반응한다. **이동 튜닝과 카메라 튜닝을 같은 세션에 하지 말 것.**

**수용 기준.** 스프린트 중 스틱을 90° 꺾으면 제자리 회전이 아니라 호를 그리며 돈다. 걷기 중에는 기존과 동일한 응답성.

#### P1-6. `CameraVelocity` 스무딩 (D-14 대응, R-11)

**문제.** Stop / TurnInPlace 중 `Motor.Velocity`가 물리 스텝마다 `0 ↔ 과대값`으로 진동하고(D-14), `CameraVelocity`가 이를 원본 그대로 노출해 **카메라 FOV·거리가 떨린다.** P1-1로 Stop 진입 빈도가 3배가 되므로 P1에서 함께 처리한다.

**사양.** `ActorMovementController.CameraVelocity`가 원본 대신 지수 이동평균(EMA)을 반환한다.

```
CameraVelocitySmoothing = 12f   // 0이면 스무딩 없음(기존 동작)

// UpdateVelocity 말미 또는 AfterCharacterUpdate에서 갱신
_smoothedCameraVelocity = Lerp(_smoothedCameraVelocity, Motor.Velocity,
                               1 - Exp(-CameraVelocitySmoothing * deltaTime));
public Vector3 CameraVelocity => _smoothedCameraVelocity;
```

**범위 한정.** 이 스무딩은 **카메라 연출 전용**이다. `PredictedVelocity`(`:80-83`)와 `Motor.Velocity` 직접 소비자는 건드리지 않는다 — 물리·판정은 원본을 써야 한다.

**수용 기준.** 30fps 상한을 걸고(`Application.targetFrameRate = 30`) Stop / TurnInPlace를 반복 실행했을 때 카메라 거리·FOV가 진동하지 않는다.

---

### P2 — 가감속 무게감

**P1이 안정화된 뒤 단독으로 켜고 튜닝한다** (설계 원칙 P3).

**사양.** 가속과 감속에 서로 다른 계수를 적용한다.

`PlayerGroundMoveState.UpdateVelocity`(`:219-222`):
```
목표속도와 현재속도의 관계로 분기:
    가속 중 (목표 속력 > 현재 속력)  → AccelerationSharpness
    감속 중 (목표 속력 < 현재 속력)  → DecelerationSharpness
    방향 전환 (내적 < 0)             → TurnDampSharpness
```

`PlayerIdleState.UpdateVelocity`(`:153-156`)는 `DecelerationSharpness` 사용.

신규 파라미터 (기본값은 **현행 15 대비 의도적 비대칭**):
```
AccelerationSharpness = 8f     // 출발을 느리게 → 무게감
DecelerationSharpness = 20f    // 정지를 빠르게 → 반응성
TurnDampSharpness     = 12f    // 방향 전환 시 감속
```

근거: 게임필 연구 관례상 **감속을 가속보다 빠르게** 두면 "무겁지만 반응성 있는" 인상이 나온다. 급정지는 자칫 부자연스러우나, 우리는 P1-1의 Stop 루트모션이 그 구간을 시각적으로 덮으므로 감속을 공격적으로 잡을 수 있다.

**기존 `StableMovementSharpness`는 남긴다.** 몬스터/NPC가 공유하는 필드이므로(R-10) 제거하지 않고, 신규 3개가 미설정(0)일 때의 폴백으로 쓴다.

**⚠️ 회귀 필수 (R-9).** 현재 `:219`가 속도를 통째로 `Vector3.Lerp` 대체하고, 그 위에 `_impulseDampers`(`ActorMovementController.cs:264-272`)와 `_pendingImpulseVelocity`(`:283-287`)가 얹힌다. **가속도 적분 방식으로 바꾸지 말고 Lerp 구조를 유지한 채 계수만 분기**하는 것이 이번 Phase의 범위다. 그래도 **넉백/Launch 회귀 테스트는 반드시 수행**한다.

**수용 기준.** 정지에서 스프린트까지 도달 시간이 눈에 띄게 길어진다. 손을 떼면 기존보다 빠르게 멎는다. 넉백 거리와 감쇠가 변경 전과 동일하다.

---

### P3 — 발 미끄러짐 제거 (애니 속도 동기화)

**세 단계를 반드시 이 순서로 진행한다.**

#### P3-1. `Animator.Speed` 소유권 명문화

**문제.** 현재 `Speed`를 쓰는 주체가 4개이고 소유자가 없다.

| 주체 | 동작 |
|---|---|
| `GameActor.cs:100` | `_animator.Speed = _localTimeScale` — **무조건 덮어씀** |
| `PlayerAttackState.cs:870-871` | `Speed = playbackScale * attackSpeed * LocalTimeScale` — 컨벤션의 기준 |
| `MotionWarpController.cs:197` (주석) | 워프 중 풋슬라이딩 저감 목적으로 같은 자원 사용 |
| `MotionEvent_AnimationSpeed` (`AnimationSpeedEvent`) | **`Execute()`가 `Debug.Log`만 하는 미구현 스텁** — 현재 충돌 없으나 구현 시 정면 충돌 |

**사양.**
1. 규약 R-4를 코드 주석으로 `ActorAnimator.Speed` 프로퍼티에 명시한다.
2. 로코모션이 `Speed`를 쓰는 구간과 전투가 쓰는 구간이 겹치지 않음을 보장한다 — 로코모션 상태 `OnExit`에서 `Speed = LocalTimeScale`로 복원(현재 로코모션에는 리셋 코드가 전무하고 공격 상태의 OnExit에만 의존).
3. `MotionEvent_AnimationSpeed`를 향후 구현할 경우 **로코모션 상태에서는 무시**하도록 명시한다.

#### P3-2. 클립 기준 이동 속도 데이터 도입

**문제.** 기준 속도를 담을 슬롯이 **어디에도 없다.**
- `MotionSetAsset`(`02.Scripts/MotionSet/Core/Data/MotionSetAsset.cs:6-18`) — 필드가 `motionSet` 1개
- `MotionSet`(`Motion.cs:265-283`) — 속도 메타 없음
- `Motion`(`Motion.cs:160-175`) — `playbackSpeed`는 **순수 배율이지 물리 단위가 아니다**
- `ActorAnimationMotionSet` — 슬롯 딕셔너리만

**사양 (채택안).** `ActorMovementController`(또는 P0-4의 프로파일 SO)에 `(BaseMoveAnimType → 클립 기준 속도 m/s)` 3값 테이블을 둔다.

```
ReferenceWalkClipSpeed   = 1.5f
ReferenceRunClipSpeed    = 3.5f
ReferenceSprintClipSpeed = 6.0f
```
(초기값은 ALS `AnimatedWalk/Run/SprintSpeed` 150/350/600 uu/s를 m 단위로 환산한 것 — **반드시 실제 클립으로 재측정하여 교체할 것.**)

**기각안과 이유:**
- `MotionSet`에 `referenceMoveSpeed` 추가 → **기각.** `MotionSet.Core` asmdef는 액터 물리를 모르는 순수 타임라인 모듈이고, 로코모션 외 모든 모션에 무의미한 필드가 생긴다.
- `ActorAnimationMotionSet`에 `SerializedDictionary<GameplayTag, float>` 추가 → **P4-2(8방향) 도달 시 필연**이나, 현 3값에는 과잉.

**측정 자동화 — P0-5로 이관됨.** 당초 이 Phase에서 다루려던 베이크 도구는 **P1-2의 회전 수렴(B1)이 같은 인프라를 요구하므로 P0-5로 앞당겨졌다.** 이 Phase는 P0-5가 산출한 `referenceClipSpeed` 값을 **소비**하기만 한다.

#### P3-3. 재생 속도 동기화

**사양.**

`PlayerGroundMoveState.UpdateState` 말미에서 매 프레임:
```
ratio = 평면속도 / GetReferenceClipSpeed(MoveAnimType)
ratio = clamp(ratio, LocomotionPlayRateMin, LocomotionPlayRateMax)
Animator.Speed = ratio * gameActor.LocalTimeScale        // ← R-4 필수
```

```
LocomotionPlayRateMin = 0.75f
LocomotionPlayRateMax = 1.25f
```

**클램프가 필수인 이유.** P2로 가속을 완만하게 만들면, 가속 구간에서 `ratio`가 0.2~0.3까지 떨어져 **클립이 슬로우모션으로 재생**된다. ALS도 `RotateInPlace`에 1.15~3.0 클램프를 둔다. `MotionTimelineSpeed`의 내장 클램프 `[0.1, 5]`(`ActorAnimator.cs:205`)는 이 목적에는 너무 넓다.

**`MotionTimelineSpeed`를 쓰지 않는 이유 (R-5).** 이 값은 모션 종료 시각과 이벤트 발화 시각까지 스케일한다(`ActorAnimator.cs:275`). Stop/TurnInPlace가 모션 종료로 상태를 끝내므로(P0-2), **상태 지속 시간이 이동 속도에 따라 변하는** 부작용이 생긴다.

**수용 기준.** 스틱을 반만 기울여 이동 시 발 접지점이 지면에 대해 미끄러지지 않는다. 가속 구간에서 클립이 슬로우모션으로 보이지 않는다. 히트스톱 중 로코모션 애니가 함께 멈춘다(R-4 검증).

---

### P4 — 확장 (조건부)

#### P4-1. Idle 카메라 정렬 회전

**사양.** `PlayerIdleState.UpdateRotation`(`:136-140`)의 no-op을, 카메라 정면(`PlayerMovementController.CameraForwardDirection`, `:87`)과의 각도가 `IdleAlignAngle`(기본 **90°**, Lyra의 50°보다 보수적)을 넘으면 그 방향으로 서서히 정렬하도록 변경한다.

**⚠️ 미해결 설계 판단.** `PlayerIdleState.cs:71-75`가 `HasMoveInput()`으로 즉시 GroundMove로 나가므로, **Idle 회전이 완료되기 전에 상태가 바뀐다.** 두 선택지:
- (a) Idle 내부 서브모드로 처리 — 상태 추가 없음, `Stand_Idle_Turn_*` 클립 미사용
- (b) 별도 `PlayerIdleTurnState` 신설 — 클립 5종 활용, 상태 1개 추가 + R-6 화이트리스트 갱신 필요

**권장: (a)를 먼저.** 클립 없이 회전만으로도 "카메라를 돌리면 캐릭터가 따라본다"는 체감의 대부분을 얻는다. (b)는 (a) 이후 별건으로 판단한다.

#### P4-2. 8방향 스트레이프 — **보류 (애니메이션 저작 선행)**

**차단 사유.** `10.Datas/.../MotionSet/Player/**/*_F_L45*.asset` — **0건.** 플레이어용 방향 클립이 존재하지 않는다. 코드 작업으로 해결할 수 없다.

**저작이 완료되면의 사양 (사전 설계):**

1. **키 선택 로직은 신규 작성 불필요.** `EnemyLocomotionHelper.GetDirectionalKey(worldVelocity, transform, style)`(`EnemyLocomotionHelper.cs:54-65`)는 정적 유틸이며 Player/Enemy 무관하게 재사용 가능하다.
2. **회전 모드의 단일 소유자는 `PlayerMovementController`.**
   - 락온 시스템은 존재하며 Camera 모듈이 소유한다: `Camera/CameraLockOn.cs`, 접근자 `CameraManager.cs:1245 GetLockOnTarget()`, 계약 `Contracts/GameServices.cs:169-170`.
   - **asmdef 장벽 없음** — `GameActor/UPlayGround.Actor.asmdef:11`이 이미 `"UPlayGround.Camera"`를 참조하고, `PlayerAttackState.cs:799`, `PlayerChargeState.cs:105`, `PlayerDashAttackState.cs:71` 등이 `CameraManager.Instance.GetLockOnTarget()`을 직접 호출하는 선례가 다수 있다.
   - 그러나 상태마다 `CameraManager.Instance`를 찌르는 코드를 더 늘리면 파티 스왑(`PartyManager.cs:1432`가 이미 상태를 검사)·테스트에서 취약해진다. **컨트롤러가 프레임당 1회 폴링해 `RotationMode { FaceMovement, FaceTarget }` + `RotationTarget`을 캐시하고, 상태는 이 프로퍼티만 읽는다.** 근거: `SetInputs`가 카메라 회전을 받아 `_cameraForwardDirection`을 캐시하는 **동일 패턴이 이미 존재**한다(`:152-165`).
3. 기준 속도 테이블이 `(방향 × 타입)` 2차원으로 확장되므로, 이 시점에는 P3-2의 기각안(`ActorAnimationMotionSet` 딕셔너리)이 채택안이 된다.

---

## 7. 파라미터 요약

| 파라미터 | 기본값 | 소유 | Phase |
|---|---|---|---|
| `MoveInputDeadzone` | 0.15 | `PlayerMovementController` | P0-1 |
| `MoveInputReleaseGrace` | 0.08s | `PlayerMovementController` | P0-1 |
| `MoveInputSmoothTime` | 0.06s | `PlayerMovementController` | P0-1 |
| `MinStopSpeed` | 1.5 m/s | `ActorMovementController` | P1-1 |
| `TurnTriggerAngle` | **135°** | `ActorMovementController` | P1-2 |
| `TurnAbortAngle` | 90° | `ActorMovementController` | P1-2 |
| `MinTurnSpeed` | 3.0 m/s | `ActorMovementController` | P1-2 |
| `TurnConfirmTime` | 0.10s | `ActorMovementController` | P1-2 |
| `TurnMinDuration` | 0.12s | `ActorMovementController` | P1-2 |
| `TurnReentryCooldown` | 0.30s | `PlayerMovementController` | P1-2 |
| `TurnClearance` | 1.2 m | `ActorMovementController` | P1-2 (B3) |
| `TurnRotationScaleMin/Max` | 0.6 / 1.4 | `ActorMovementController` | P1-2 (B1) |
| `motionRootYaw` (클립별 총 회전량) | 베이크 산출 | `ActorAnimationMotionSet` | P0-5 |
| `SprintOrientationScale` | 0.35 | `ActorMovementController` | P1-3 |
| `CameraVelocitySmoothing` | 12 | `ActorMovementController` | P1-6 |
| `AccelerationSharpness` | 8 | `ActorMovementController` | P2 |
| `DecelerationSharpness` | 20 | `ActorMovementController` | P2 |
| `TurnDampSharpness` | 12 | `ActorMovementController` | P2 |
| `ReferenceWalk/Run/SprintClipSpeed` | 1.5 / 3.5 / 6.0 m/s | `ActorMovementController` | P3-2 |
| `LocomotionPlayRateMin/Max` | 0.75 / 1.25 | `ActorMovementController` | P3-3 |
| `IdleAlignAngle` | 90° | `ActorMovementController` | P4-1 |

**기존 값은 변경하지 않는다** (R-10): `MaxWalk/Run/SprintMoveSpeed` 3 / 6.5 / 10, `StableMovementSharpness` 15, `OrientationSharpness` 10.

---

## 8. 검증 방법

**Phase마다 하나씩만 켜고 측정한다.** 특히 P1-3(선회 감쇠)과 P2(가속 완화)를 동시에 켜면 스프린트 조작감이 이중으로 무거워져 원인 분리가 불가능하다.

**계측 도구.** `02.Scripts/Tool/PlayerControlFeelDebugHUD.cs`(F9 토글)가 이미 State / MoveInput / LookInput / MoveAnimType을 표시한다. 다음을 추가한다:
- 평면 속도 (m/s) — **원본 `Motor.Velocity`와 스무딩 값을 나란히** (D-14 / P1-6 검증용)
- `CharacterForward` ↔ `MoveInputVector` 각도
- 현재 `Animator.Speed`, `LocalTimeScale`
- TurnInPlace 재진입 쿨다운 잔여
- **`RootMotionStepDeltaPosition` 크기** — 0인 스텝(기아)의 발생 빈도를 눈으로 확인 (D-14)
- **턴 진입 시 `requiredYaw` / `clipTotalYaw` / `rotationScale`** (B1 검증용)

**프레임레이트 스트레스 테스트 (D-14 필수).**
루트모션 기아는 **프레임레이트 < 물리레이트**에서만 드러난다. `Application.targetFrameRate = 30`(또는 `Time.fixedDeltaTime` 축소)으로 강제한 뒤 Stop / TurnInPlace를 반복 실행해 다음을 확인한다:
- [ ] 캐릭터 최종 위치·방향이 60fps 결과와 일치 (순 변위 보존 확인)
- [ ] 카메라 거리·FOV 진동 없음 (P1-6)
- [ ] 턴 종료 후 GroundMove 복귀 속도 편차 없음 (P1-5)

**회귀 체크리스트 (Phase 무관 공통).**
- [ ] 넉백 / Launch 거리·감쇠 불변 (R-9)
- [ ] Dodge / Dash 4방향이 의도 방향으로 나감
- [ ] 대시 → 대시어택 연계 유지 (`PlayerGroundMoveState.cs:145`의 Sprint 게이트 — P1-4가 `MoveAnimType`을 건드리므로 특히 주의)
- [ ] 무기 뽑기 / 넣기 모션이 Stop / TurnInPlace 중에도 정상 재생·복귀 (R-6, `PlayerCombatWeaponStateController.cs:171-179`의 기존 switch 누락 확인)
- [ ] 파티 캐릭터 스왑 중·직후 이동 정상 (`PartyManager.cs:1432`)
- [ ] 히트스톱 / 슬로우 중 이동·애니가 함께 감속 (R-4) — **루트모션 상태에서도 감속 정합 확인** (D-16)
- [ ] 경사면 이동 정상 (`GetDirectionTangentToSurface` 경로 불변)
- [ ] 공격 → 턴/정지 캔슬 후 MotionWarp 잔여 윈도우가 다음 공격에 누출되지 않음 (D-16 단서 조항)

---

## 9. 범위 밖 / 미결

| 항목 | 상태 |
|---|---|
| 45° / 90° Turn 클립 10종 | P1에서 **의도적 미사용** (§3.1). 향후 비침습적 additive/lean 용도로 재활용 검토 |
| `Stand_Idle_Turn_*` 5종 | P4-1 (b)안 채택 시에만 사용 |
| 플레이어 8방향 클립 | **미저작.** P4-2의 차단 요인 |
| `MotionEvent_AnimationSpeed` 스텁 구현 | 범위 밖. 구현 시 R-4/P3-1과 충돌 검토 필요 |
| `PlayerCombatWeaponStateController.cs:171-179` switch 누락 | **기존 버그.** R-6 교체 시 함께 해소 |
| 에디터 프리뷰 락(`_externalPreviewLockCount`)이 `PlayMotion`만 막고 루트모션 누적은 막지 않음 | 범위 밖이나, 루트모션 의존 상태(Stop/Turn)를 늘리면 노출 면적 증가. **P0-5 베이크 도구가 같은 영역** |
| `ProcessHitStabilityReport` / `OnDiscreteCollisionDetected` 빈 구현 (`ActorMovementController.cs:340-351`) | B3가 `OnMovementHit` 경로만 쓰므로 이번 범위에서는 손대지 않는다. 벽 감지를 더 정교하게 하려면 별건 |
| `PlayerActor.Update` ↔ `ActorMovementController.Update` 실행 순서 미정의 (D-17) | 현 설계 판정 윈도우에는 무해. 더 짧은 윈도우가 필요해지면 `DefaultExecutionOrder` 도입 검토 |
| 페이드 구간(0.1s)의 루트모션 혼합 — GroundMove(루트모션 무시) → TurnInPlace(루트모션 소비) 전환 시 미세 lurch 가능성 | Animancer 블렌딩 동작 기준의 **추론이며 코드로 검증하지 않았다.** 구현 후 체감으로 판단 |
| 공중 이동 / 착지 (`PlayerAirbornState`) | 미조사. 별건 |
| 크라우칭 이동 | 미조사. 별건 |

---

## 부록 A. `MoveAnimType` 쓰기 소유권 (P1-4 적용 후 목표 상태)

| 위치 | 현재 | P1-4 후 |
|---|---|---|
| `PlayerGroundMoveState.cs:49` (OnExit) | 무조건 → Run | **상태 이탈만으로 변경하지 않음**. 입력 토글·자동 Sprint·DashAttack 등 명시적 소유자만 기록 |
| `PlayerGroundMoveState.cs:176` | → Sprint (1회 한정) | → Sprint (`_sprintArmed`로 재무장 가능) |
| `PlayerDashState.cs:84` (OnExit) | → Sprint | 유지 |
| `PlayerDashAttackState.cs:31` | → Run | 유지 |
| `PlayerActor.Input.cs:89` | Walk ↔ Run 토글 | 유지 + 자동 Sprint 재무장 |
| `PlayerActor.Input.cs:93` | Sprint ↔ Run (문자열 게이트) | `ActorStateTag.Locomotion` 게이트로 교체 |
