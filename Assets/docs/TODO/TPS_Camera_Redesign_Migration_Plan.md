# TPS 카메라 시스템 재설계 · 마이그레이션 설계서 v1.0

> 기준 문서: `TPS_Action_Camera_Framework_Design.md` (NDC26 마비노기 모바일 카메라 세션 기반 일반 문서)
> 목적: 기준 문서의 Director/Behavior/Modifier/State/Resolver 구조로 **전면 재작성**하되, 현재 프로젝트에 이미 구현·검증된 카메라 기능을 새 구조 위로 **이식(포팅)**한다. 기능 손실 0이 목표.
> 결정 사항(사용자 확정): 범위 = 전면 재작성, 방식 = **A. 구조 재작성 + 기존 기능 포팅**. 기준 문서는 현재 코드 상태를 모르고 생성된 일반 문서임.

---

## 진행 현황 (2026-06-16 기준)

| 단계 | 상태 | 검증 |
|---|---|---|
| Stage 1 — 신규 계층 골격(CameraFrame/ICameraModifier/CameraBehaviorBase) | ✅ 완료 | 컴파일 |
| Stage 2 — InGameMode → Modifier 파이프라인 분해(11종) | ✅ 완료 | 컴파일 + **플레이** |
| Stage 3 — 타입 6종 리네이밍(Mode→Behavior 등) | ✅ 완료 | 컴파일 |
| Stage 4 — 나머지 4 Behavior 네이밍 정합화 + Behaviors/ 이동 | ✅ 완료 | 컴파일 (동작 변화 0) |
| Stage 5 — CameraManager 슬림화 + CameraResolver 추출 | ⬜ 미착수 | (★고위험 · 플레이 게이트 필수) |

**남은 일:**
1. **Stage 5** (고위험): `CameraResolver` 추출, 데드코드(`UpdateCameraPosition`/`ComputeCapsuleClearance`) 제거, `CameraManager` 슬림화. 외부 API 계약 유지.
2. **이월된 검증 부채**(Stage 5 게이트서): 대화 카메라 / 대화 녹화·재생 / 스냅샷 시퀀스 — 리팩터 후 미실행, §4에서 `[~]`.
3. **(선택) 최종 cosmetic 패스**: API 어휘 정리(`CameraModeType`/`ModeType`/`SetMode·PushMode·PopMode·ForceMode`/`_modeController` 필드/`Modes/` 폴더명 → Director/Behavior 어휘). 블래스트 반경 때문에 본 재작성에서 의도적으로 보류한 부분.

---

## 0. 가장 중요한 전제 — 기준 문서의 함정

기준 문서 §0의 전제("CameraManager 하나에 기능이 집중되어 있다")는 **이 프로젝트에는 이미 해당하지 않는다.** 현재 시스템은 이미 Mode / Effect / Context / RigState 계층으로 분해되어 있고, 기준 문서에 없는 고급 기능(스택 모드, 녹화/재생, 스냅샷 시퀀스, 킬캠, Trauma 2패스 쉐이크, 소프트 타깃 어시스트)을 다수 보유한다. 기준 문서의 `CombatController` / `LockOnController` 같은 클래스는 이 프로젝트에 존재하지 않는다.

따라서 본 재설계의 핵심 원칙은 다음과 같다.

> **원칙 0.** 기준 문서를 글자 그대로 구현하지 않는다. 기준 문서의 *네이밍·계층 구조*를 채택하되, 기존 시스템이 더 발전된 부분(스택 기반 모드 전환, 이펙트 채널, 입력 게이팅)은 새 구조에 흡수한다. 새 최상위 `CameraDirector`를 기존 `CameraModeController` *옆에* 중복 생성하지 않는다 — 둘은 하나로 통합한다.

---

## 1. 개념 매핑: 기준 문서 ↔ 현재 코드 ↔ 재설계 후

| 기준 문서 개념 | 현재 프로젝트 (이미 존재) | 재설계 후 명칭 | 비고 |
|---|---|---|---|
| CameraDirector | `CameraModeController` + `ICameraMode` | **CameraDirector** | 모드 컨트롤러를 디렉터로 리네이밍·승격. 스택 push/pop 유지 |
| CameraBehavior | `ICameraMode` 구현체들 (InGame, Dialogue, Free…) | **ICameraBehavior** + `CameraBehaviorBase` | 모드 = 비헤이비어. EvaluatePose는 Modifier 파이프라인 실행으로 전환 |
| CameraModifier | **없음** (InGameCameraMode 내부 private 헬퍼) | **ICameraModifier** 구현체들 | ★유일한 진짜 신규 계층. 본 작업의 핵심 |
| CameraState | `CameraRigState`(가변 상태) + `CameraRigPose`(프레임 결과) | **CameraState**(가변) + **CameraPose**(결과) | 두 역할 유지. 기준 문서의 단일 struct보다 정교함 — 유지 |
| ICameraContext | `CameraRuntimeContext` | **CameraContext** | 이미 의존성 묶음. 그대로 승격 |
| CameraResolver | `CameraManager.OnLateUpdate`의 적용부 | **CameraResolver** | 포즈→Unity Camera 적용부를 별도 클래스로 추출 |
| Shake/이펙트 | `CameraEffect` 시스템(`CameraEffectManager`, `CameraEffectState`, 채널) | **유지** (이펙트 = Modifier 이후 합성 레이어) | 기준 문서의 ShakeModifier보다 정교. Modifier로 격하하지 않음 |

### 1.1 전투 카메라의 분리 (기준 문서가 뭉뚱그린 지점)

기준 문서는 "CombatCameraBehavior"를 단일 개념으로 다루지만, 이 프로젝트에서 전투 카메라는 **두 개의 분리된 관심사**다.

1. **정상상태 프레이밍** — `InGameCameraMode`의 `isCombat` 분기 (combatOffset/combatPitch/거리·FOV). → 재설계 후 **Modifier 파라미터**로 흡수 (CombatOffsetModifier 등이 `CameraContext.IsCombat`을 읽어 보간 타깃 전환).
2. **순간 연출 디스패치** — `CombatCameraDirector`. 히트/킬/저스트가드/도지카운터/사망 이벤트를 카메라 **이펙트(쉐이크·펀치·킬캠·스냅샷)**로 변환하는 인텐트 라우터. → 재설계 후 **그대로 유지** (이펙트 레이어 소속, Behavior 아님).

> **주의.** `CombatCameraDirector`는 새 `CameraDirector`와 *완전히 다른 것*이다. 이름 충돌을 피하기 위해 재설계 시 `CombatCameraDirector` → **`CombatCameraEventRouter`**로 리네이밍한다.

---

## 2. 재설계 후 폴더/네임스페이스 구조

네임스페이스는 기존 `UPlayGround.CameraSystem` 유지. 폴더 재편:

```
Assets/02.Scripts/Camera/
├ CameraDirector.cs              (← CameraModeController 승격)
├ CameraResolver.cs              (← CameraManager 적용부 추출, 신규 — Stage 5에서 추가)
├ CameraContext.cs               (← CameraRuntimeContext 리네이밍)
├ State/
│   ├ CameraState.cs             (← CameraRigState)
│   └ CameraPose.cs              (← CameraRigPose)
├ Behaviors/
│   ├ ICameraBehavior.cs         (← ICameraMode)
│   ├ CameraBehaviorBase.cs      (신규 — Modifier 파이프라인 호스트)
│   ├ InGameCameraBehavior.cs    (← InGameCameraMode, 파이프라인으로 재작성)
│   ├ DialogueCameraBehavior.cs  (← DialogueCameraMode)
│   ├ DialogueReplayCameraBehavior.cs (← DialogueCameraReplayMode)
│   ├ FreeCameraBehavior.cs      (← FreeCameraMode)
│   └ CameraSnapshotSequenceBehavior.cs (← CameraSnapshotSequenceMode)
├ Modifiers/                     (★ 신규 계층)
│   ├ ICameraModifier.cs
│   ├ FollowCameraModifier.cs
│   ├ OffsetCameraModifier.cs        (default/combat 보간 통합)
│   ├ LockOnCameraModifier.cs
│   ├ AlignCameraModifier.cs
│   ├ RotationTransitionCameraModifier.cs
│   ├ DistanceFovCameraModifier.cs   (DistanceController 위임)
│   ├ LookAheadCameraModifier.cs
│   ├ CollisionCameraModifier.cs     (SafeBack/GroundPenetration/FloorRescue/FrontBlend 포함)
│   └ SlopePitchCameraModifier.cs
├ Effects/                       (유지 — 변경 없음)
├ Combat/
│   ├ CombatCameraEventRouter.cs (← CombatCameraDirector 리네이밍)
│   └ …(Intent 등 유지)
└ …(LockOn/Collision/Distance/Shaker/Recorder 등 헬퍼 클래스는 위치·역할 유지)
```

---

## 3. 새 핵심 인터페이스 정의

### 3.1 ICameraModifier (신규)

기준 문서의 단일 `ref CameraState` 시그니처 대신, Context/State/Effects/Pose를 한 프레임 단위(`CameraFrame`)로 묶어 `ref`로 전달한다. Modifier 로직은 Context(의존성)와 State(누적값)를 모두 필요로 하고, **effect 델타를 파이프라인 도중에 읽어야** 하기 때문이다.

```csharp
// 구현 완료 (Stage 1) — Assets/02.Scripts/Camera/Modifiers/
public struct CameraFrame   // plain struct + ref 전달 (ref struct 아님: Span 없음 → 제약만 늘 뿐)
{
    public CameraRuntimeContext Context;   // → Stage3에서 CameraContext
    public CameraRigState       State;     // → CameraState
    public CameraEffectState    Effects;
    public CameraRigPose        Pose;       // → CameraPose
    public float                DeltaTime;
}

public interface ICameraModifier
{
    int Priority { get; }
    void Apply(ref CameraFrame frame);
}
```

> **Modifier는 stateful 인스턴스다.** `InGameCameraMode`가 보유한 프레임 간 보간 상태(`_lookAheadOffset/_lookAheadVelocity`, `_frontCameraBlend/_frontCameraBlendVel`, `_wasLockOnLastFrame/_lockOnReleaseSmoothTimer`)는 각각 소유 Modifier(LookAhead, Collision/FrontBlend, LockOn) 인스턴스 필드로 이전한다. stateless로 만들면 SmoothDamp 연속성이 끊긴다.

### 3.2 Priority 순서 + Effect 델타 슬롯 (현재 EvaluatePose 실행 순서 기반으로 확정)

⚠️ **effect는 단일 지점이 아니라 두 지점에서 소비된다.** 현재 `EvaluatePose`에서 쉐이크 yaw/pitch는 Follow 위치 계산 *이전*에 state에 주입되고(라인 100-101), positionDelta/fovDelta는 Collision *이후*에 적용된다(124, 126-131). 따라서 "끝에서 일괄 합성"은 불가능하다.

```
100  RotationTransition   (state.Yaw/Pitch 전환 보정)
200  LockOn               (타깃 기준 회전 보정 + needAlign 트리거 + 해제 스무딩 타이머)
300  Align                (정렬 진행)
400  Offset               (default/combat 오프셋 SmoothDamp)
450  LookAhead            (락온 시 속도 기반 선행 오프셋, Offset 타깃에 합산)
500  DistanceFov          (DistanceController 거리/FOV base)
600  EffectRotationInject ── state.Yaw += Effects.yawDelta; Pitch += Effects.pitchDelta
650  PitchClamp(authoritative) ── 슬로프 반영 최종 클램프 (★600 이후·Follow 이전 필수)
700  Follow               (피벗 SmoothPosition + Effects의 distanceDelta/offsetDelta/smoothTime override 소비)
800  Collision            (SafeBack→GroundPenetration→FloorRescue→FrontBlend)
850  EffectPositionFovInject ── Pose.CameraPosition += Effects.positionDelta; FOV = base + Effects.fovDelta
```

Effect 델타 → 슬롯 매핑 (현재 라인 → 파이프라인 위치):

| Effect 필드 | 현재 라인 | 적용 슬롯 | 비고 |
|---|---|---|---|
| `yawDelta`/`pitchDelta` | 100-101 | 600 (pre-Follow) | 그 직후 650 권한 클램프 |
| `distanceDelta` | 107-108 | 700 (Follow 입력) | effectDistance = clamp(TargetDistance)+delta |
| `offsetDelta` | 109 | 700 (Follow 입력) | state.CameraOffset += delta |
| `positionSmoothTimeOverride` | 116-119 | 700 (Follow 입력) | null이면 락온·정렬·override 외엔 0 |
| `rotationSmoothTimeOverride` | 119 | 700 (회전 보간) | |
| `positionDelta` | 124 | 850 (post-Collision) | 충돌 보정 후 카메라 위치에 가산 |
| `fovDelta` | 126-131 | 850 (post-Collision) | abs>0.001이면 base+delta, 아니면 base |

> 기준 문서 §10.3(Shake 마지막)은 위 850 슬롯이 충족한다. 단, 회전 쉐이크(yaw/pitch)는 의도적으로 600에서 위치 계산에 반영한다 — 이게 현행 거동이다.

---

## 4. 기능 포팅 체크리스트 (★누락 방지 — 재작성 중 하나도 빠뜨리지 말 것)

새 구조로 옮기면서 **반드시 보존**해야 하는, 기준 문서에 설계가 없는 기존 기능들.
표기: `[x]` = 새 구조로 이식+검증 완료 / `[~]` = 미변경으로 보존(현재 정상 동작, 후속 단계에서 정합화/검증 예정) / `[ ]` = 미착수.

- [x] 모드별 입력 게이팅 (`AllowsPlayerLookInput/ZoomInput/LockOnInput`, `UseCollision`) → ICameraBehavior (InGameCameraBehavior, Stage2 검증)
- [x] 락온: 토글/좌우 전환, 디바운스, 전환 보간, 피벗 오프셋, 해제 후 스무딩 타이머 (Modifier 200/660, Stage2 검증)
- [x] 카메라 정렬(Align): combat/explore 피치 타깃, 타이머 기반 MoveTowards (Modifier 300, Stage2 검증)
- [x] 슬로프 피치 보정 (`ComputeSlopePitchOffset` → 동적 minVerticalAngle) (Modifier 650, Stage2 검증)
- [x] LookAhead 오프셋 (속도 기반, 락온 배수) (Modifier 400 흡수, Stage2 검증)
- [x] 충돌 4종: SafeBackPosition / GroundPenetration / FloorRescue / **전방 카메라 블렌드** (Modifier 800, Stage2 검증)
- [x] 거리/FOV 컨트롤러 (`CameraDistanceController`) (Modifier 500/850 위임, Stage2 검증)
- [~] 스택 기반 모드 전환 (`PushMode`/`PopMode`/`ForceMode`) → CameraDirector (리네이밍만, 로직 미변경)
- [~] 이펙트 시스템 전체: Shake(Trauma 2패스), Zoom, Rotation, FOV, SmoothDamp, SpringDamp, TimeScale + 채널 (미변경 — Stage2서 shake/punch 합성경로만 검증)
- [~] 전투 이벤트 라우팅(히트/스킬/킬/저스트가드/회피/도지카운터/피격/사망) → `CombatCameraEventRouter` + 프로필 DB (리네이밍만)
- [~] 킬캠 (`KillCamController`, chance roll, 스냅샷 시퀀스 경유 분기) (미변경)
- [~] 소프트 타깃 어시스트 부분 블렌드 (미변경)
- [~] 퍼펙트 가드 FOV 데이터 (미변경)
- [~] 씬 전환 시 재초기화 (`OnSceneChanged`) (미변경 — Stage5서 재검증)
- [~] LookAtOverride (소켓 바라보기 / MotionEvent_CameraLookAtSocket) (미변경 — Follow가 처리)
- [~] MotionEvent 연동: CameraEffect / CameraSnapshotSequence / CameraLookAtSocket (미변경)
- [~] 외부 API 호환: `ICameraStateAccessor`, `CameraManager` public 메서드 (미변경 — Stage5 슬림화 시 계약 유지 필요)
- [~] 대화 카메라: `DialogueCameraBehavior` + 설정 SO (Stage4 리네이밍만, 로직 미변경 — ⚠️ 리팩터 후 미실행, Stage5 게이트서 플레이검증 필요)
- [~] 대화 카메라 **녹화/재생** (`DialogueCameraRecorder`, RecordingSO, TrackSmoother, `DialogueCameraReplayBehavior`) (Stage4 리네이밍만 — ⚠️ 미실행, Stage5 게이트 검증)
- [~] 카메라 스냅샷 시퀀스 (`CameraSnapshotSequenceBehavior`, ProfileSO, ActorReferenceResolver, 트리거 SO) (Stage4 리네이밍만 — ⚠️ 미실행, Stage5 게이트 검증)

---

## 5. 단계별 마이그레이션 (검증 게이트 포함)

> 각 단계 종료 시 Unity 컴파일 + 플레이 검증을 게이트로 둔다. CLI 컴파일이 없으므로 단계 폭을 작게 잡아 회귀 추적을 쉽게 한다. **단계 사이에는 항상 컴파일 가능한 상태를 유지**한다.

### Stage 1 — 신규 계층 골격 (무위험, 기존 동작 불변) ✅ 완료
- `CameraFrame`(plain struct), `ICameraModifier`, `CameraBehaviorBase`(Modifier 리스트 정렬·실행) 추가
- 리네이밍 없이 *추가만* 했다. 기존 코드 미변경. 아무도 아직 사용하지 않음 → 컴파일만 영향.
- `CameraResolver`는 Stage 5로 연기 (CameraManager 적용부를 추출할 때 함께 — 추측 코드 방지).

### Stage 2 — InGameCameraMode → Modifier 파이프라인 분해 (★핵심, 최대 위험) ✅ 완료 (컴파일+동작 검증됨 2026-06-16)
- `InGameCameraMode.EvaluatePose`의 각 블록을 §3.2 순서대로 11개 Modifier로 추출 완료:
  RotationTransition(100)/LockOn(200)/Align(300)/Offset(400,LookAhead포함)/DistanceFov(500)/
  EffectRotationInject(600)/PitchClamp(650)/LockOnReleaseSmoothing(660)/Follow(700)/Collision(800)/EffectPositionFov(850)
- `InGameCameraBehavior : CameraBehaviorBase` 생성, 모디파이어 등록 + HandleInput 보유. `CameraManager`:616 등록 스왑. 구 `InGameCameraMode.cs` 삭제.
- combat/lockon 분기는 각 Modifier가 `Context.CombatStateProvider`/`LockOn.IsActive`를 읽도록 흡수.
- 해결한 잠재 버그: Collision의 SafeBack 원점용 `pivotBase`를 frame으로 1회 전달 → `LockOn.EvaluatePivotOffset` 프레임당 1회 호출 보장(중복 부작용 방지).
- **검증 게이트(메리님 Unity 실행 필요):** 컴파일 + 아래 플레이 체크리스트. 통과 후에만 Stage 3 진입.
  - 탐색: 추적+스크롤 줌+벽 충돌 당김 / 전투: 오프셋·피치·FOV 전환 / **락온 engage→release**(재배치된 해제스무딩+정렬, 가장 복잡) + 타깃 전환 / 락온 이동 중 LookAhead / **히트 쉐이크(회전=프레이밍 이동)·펀치(충돌 후 위치)** / 벽 등짐→전방 블렌드 / 경사(피치 클램프·지형 관통) / 씬 전환 재초기화

### Stage 3 — 리네이밍 ✅ 완료 (컴파일 검증됨 2026-06-16)
- 6개 타입 단어경계 일괄 치환(134건/26파일), 구 토큰 0 확인, 새 타입 각 1회 정의 확인:
  `ICameraMode`→`ICameraBehavior`, `CameraModeController`→`CameraDirector`, `CameraRigState`→`CameraState`,
  `CameraRigPose`→`CameraPose`, `CameraRuntimeContext`→`CameraContext`, `CombatCameraDirector`→`CombatCameraEventRouter`(★ 새 Director와 충돌 방지)
- 정의 파일 6개 + .meta `git mv`로 새 이름에 맞춤. CameraManager 내부 필드 `_combatCameraEventRouter` 정리.
- `CameraModeType`/`CameraModeEnterParams`는 의도적으로 유지(블래스트 반경 축소, dictionary 키·다수 참조). `ModeType` 프로퍼티명도 유지.
- **폴더 재배치(Modes/→Behaviors/·State/ 등)는 연기** — 순수 cosmetic, 높은 churn, 컴파일/동작 무관. 별도 정리 단계로 미룸.
- **검증 게이트:** Unity 컴파일 그린(동작 변화 없음 → 플레이 재검증 불필요, 단 콘솔 에러 0 확인). 통과 후 Stage 4.

### Stage 4 — 나머지 Behavior 정합화 ✅ 완료 (컴파일 검증됨 2026-06-16)
- **조사 결과:** Dialogue/Free/SnapshotSequence/DialogueReplay 4개 모드는 모두 **자체 포즈를 완전히 계산하는 bespoke 모드**다. Modifier 파이프라인/재사용 여지가 전혀 없다. 이미 Stage 3에서 `ICameraBehavior`를 직접 구현 중.
- **설계 결정: `CameraBehaviorBase` 상속 안 함.** 이들은 OnEnter/OnExit/HandleInput/EvaluatePose를 모두 자체 로직으로 오버라이드하고 Modifier를 0개 등록하므로, 베이스 상속은 안 쓰는 기계장치(AddModifier/파이프라인/lifecycle 포워딩)만 물려받는다. 직접 `ICameraBehavior` 구현이 더 깔끔. (기준 문서도 "자체 포즈 계산 모드는 오버라이드 허용"으로 강제 아님.)
- **따라서 Stage 4 = 순수 네이밍 정합화** (동작 변화 0, 컴파일만 영향):
  - 클래스 4개 리네이밍: `DialogueCameraMode`→`DialogueCameraBehavior`, `FreeCameraMode`→`FreeCameraBehavior`, `CameraSnapshotSequenceMode`→`CameraSnapshotSequenceBehavior`, `DialogueCameraReplayMode`→`DialogueCameraReplayBehavior`
  - 파일 4개+.meta를 `Modes/` → `Behaviors/`로 `git mv` (InGameCameraBehavior와 co-location). 구 토큰 0 확인.
  - 참조 갱신: CameraManager 등록 4곳 + `is XxxMode` 패턴체크 7곳.
  - `Modes/`엔 코어 타입만 잔류(CameraDirector/State/Pose/Context/ICameraBehavior/CameraModeType/CameraModeEnterParams) — 폴더명 `Modes`는 이제 misnomer, 폴더 정리는 cosmetic이라 계속 연기.
- **검증 게이트:** Unity 컴파일 그린(동작 변화 없음 → 플레이 재검증 불필요, 콘솔 에러 0 확인). 통과 후 Stage 5.

### Stage 5 — CameraManager 슬림화 (★고위험 — Stage 2급, 컴파일만으론 불충분·플레이 게이트 필수)
- `CameraResolver` 신규 추가 — 현재 `CameraManager` 적용부(`ApplyCameraPose`: 포즈→Unity Camera transform/FOV)를 충실히 추출
- 데드코드 제거: `UpdateCameraPosition`/`ComputeCapsuleClearance`(호출처 없음). **삭제 직전 호출처 0 재확인**(Stage3·4의 CameraManager 필드 churn 이후 재검증).
- `CameraManager`는 BaseManager 수명주기 + 로딩(Addressables) + 입력 등록 + Director/Resolver 호스팅만 담당
- 포즈 계산 로직 잔재를 전부 Behavior/Modifier로 이전
- `ICameraStateAccessor` + public API 계약(`CameraModeType`/`SetMode`/`PushMode`/… 외부 호출부) 유지
- **검증 게이트(플레이 필수):** 전체 회귀 + **Stage 4에서 이월된 미검증 경로** — 대화 카메라/대화 녹화·재생/스냅샷 시퀀스(리팩터 후 첫 실행). Stage 5가 이 모드들을 호스팅/호출(`is XxxBehavior` 체크·recorder/snapshot 트리거)하므로 여기서 함께 검증.

---

## 6. 위험 요소 및 완화

| 위험 | 완화책 |
|---|---|
| Modifier 분해 중 미묘한 실행순서/부동소수 차이로 카메라 거동 변화 | Stage 2를 한 모디파이어씩 진행, 매번 시각 비교. 상수·스무딩 파라미터 그대로 이전 |
| `CameraManager` public API를 호출하는 외부 코드 광범위 | Stage 3 리네이밍 전 전수 grep, 외부 계약(시그니처)은 최대한 보존 |
| 이펙트 합성 타이밍(EffectState) 누락 | EffectState 합성은 Modifier 이후 Behavior 말미에서 적용하는 현행 순서를 명문화·유지 |
| 씬 전환/풀링(HideAndDontSave) 수명주기 | `OnSceneChanged` 재초기화 경로를 Stage 5에서 별도 검증 |
| Unity .meta/직렬화 참조 (SerializeField로 연결된 SO) | 클래스 리네이밍 시 .meta GUID·`[SerializeField]` 필드명 영향 점검 |

---

## 7. 결론

- 본 작업은 "백지 재구축"이 아니라 **검증된 기능을 기준 문서 구조로 재배치**하는 작업이다.
- 진짜 신규 산출물은 `Modifiers/` 계층 하나뿐이며, 나머지는 리네이밍·재배치·추출이다.
- §4 포팅 체크리스트가 본 작업의 합격 기준이다. 단 하나도 누락되면 회귀다.
- 진행 순서: Stage 1(골격) → Stage 2(핵심 분해) → Stage 3(리네이밍) → Stage 4(나머지 Behavior) → Stage 5(매니저 슬림화).
