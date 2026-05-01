# Camera Mode Architecture 설계 문서

## 개요

현재 `CameraManager`는 TPS 인게임 카메라, 락온, 충돌, 거리/FOV, 이펙트, 쉐이크, 킬캠을 한 클래스에서 순서대로 조율한다. 장기적으로 `InGameMode`, `FreeMode`, `DialogueMode`, `CinematicMode` 등 여러 카메라 모드를 오가려면 **카메라 계산 주체를 모드 단위로 분리**하고, `CameraManager`는 모드 전환과 공용 효과 합성만 담당하도록 축소하는 것이 적합하다.

이 문서는 코드 구현 전 설계안이다. 기존 수동 TPS 카메라를 즉시 폐기하지 않고, 현재 동작을 `InGameMode`로 감싼 뒤 모드를 점진적으로 늘리는 방향을 기준으로 한다.

핵심 목표:

- 기존 메인 카메라 동작을 `InGameMode`로 보존
- 자유 시점과 대화 카메라를 독립 모드로 추가
- 스킬/킬캠 연출은 별도 모드가 아니라 `InGameMode` 내부의 일시적 연출 시퀀스로 처리
- 락온/충돌/거리/FOV/이펙트 같은 공용 기능을 모드와 분리
- 모드 전환 시 입력 잠금, 블렌드, 복귀 상태를 명시적으로 관리
- 추후 Cinemachine 도입 또는 부분 연동이 가능하도록 추상화

---

## 웹 구현 사례 조사 요약

### Cinemachine 방식

Unity Cinemachine은 하나의 실제 Unity Camera를 두고 여러 Cinemachine Camera 또는 Virtual Camera가 샷을 정의하는 구조다. 활성 카메라 전환은 `CinemachineBrain`이 처리하며, 우선순위/활성 상태에 따라 컷 또는 블렌드를 수행한다.

참고한 포인트:

- 단일 실제 카메라를 여러 가상 카메라가 제어하는 모델은 모드 전환 설계와 잘 맞는다.
- 공식 문서는 “한 샷당 하나의 CinemachineCamera”를 권장하며, 대화는 투샷/화자 클로즈업 등 여러 샷으로 나눌 수 있다고 설명한다.
- `CinemachineBrain`은 실시간 게임 이벤트에는 priority 조작, 예측 가능한 컷신에는 Timeline 사용을 권장한다.
- Manager Camera 계열인 Free Look, Mixing, Blend List, Clear Shot, State-Driven Camera는 복잡한 카메라 리그를 구성할 때 참고할 만하다.

출처:

- [Unity Cinemachine 3.0 Using Cinemachine](https://docs.unity.cn/Packages/com.unity.cinemachine@3.0/manual/CinemachineUsing.html)
- [Unity Cinemachine Managing and grouping Virtual Cameras](https://docs.unity.cn/Packages/com.unity.cinemachine@2.2/manual/CinemachineManagerCameras.html)
- [Unity Learn - The Cinemachine Brain](https://learn.unity.com/course/creating-an-fps-wall-run-mechanic-beginner-prototype-series/tutorial/the-cinemachine-brain)

### 모드 기반 카메라 사례

Unity Learn의 Out of Circulation 예시는 Static, Dynamic, Conversation 세 가지 카메라 모드를 분리한다. 대화 모드에서는 화자/청자 관계에 따라 이상적인 카메라 포즈를 계산하고, 경우에 따라 Virtual Camera 대신 Transform을 직접 제어한다.

참고한 포인트:

- 모든 상황을 하나의 카메라 알고리즘에 넣기보다 모드별 책임을 나누는 것이 유지보수에 유리하다.
- 대화 카메라는 “현재 화자”, “NPC”, “플레이어” 같은 도메인 정보를 입력으로 받아 포즈를 계산하는 편이 자연스럽다.
- 접근성 관점에서 급격한 동적 카메라 이동은 옵션화하거나 강도를 낮출 필요가 있다.

출처:

- [Unity Learn - Camera system](https://learn.unity.com/course/practical-game-accessibility/unit/design-and-development/tutorial/camera-system?version=2022.3)

---

## 현재 구조 진단

### 현재 책임 분포

```
CameraManager
├── 입력 처리: Look / Zoom / LockOn / LockOnSwitch
├── TPS 추적 계산: yaw / pitch / pivot / distance / offset
├── 전투 상태 반영: combatOffset, fovCombat
├── 락온: CameraLockOn
├── 충돌: CameraCollision
├── 거리/FOV: CameraDistanceController
├── 연출 회전: CameraRotationTransition
├── 효과 합성: CameraEffectManager + ICameraEffect
├── 쉐이크: CameraShaker
└── 킬캠: KillCamController
```

### 장점

- `CameraLockOn`, `CameraCollision`, `CameraDistanceController`, `CameraEffectManager` 등은 이미 독립 클래스로 분리되어 있다.
- `ICameraStateAccessor`가 있어 이펙트가 `CameraManager` 내부 필드에 직접 의존하지 않는다.
- `CameraSettings`, `CameraShakeData`, `KillCamData`, `CameraEffectData` 등 데이터 주도 구조가 준비되어 있다.

### 병목

- `CameraManager.OnLateUpdate()`가 “현재 카메라가 어떤 모드인지”를 모른 채 모든 로직을 한 흐름으로 실행한다.
- `_isInputLocked`, `_lookAtOverride`, `_combatStateProvider`, `_lockOn`, `_distanceCtrl` 등이 모드 전환 정책과 강하게 결합되어 있다.
- `KillCamController`는 별도 모드로 승격하기보다 `InGameMode` 안의 일시적 카메라 연출 시퀀스로 흡수하는 편이 단순하다.
- 대화 카메라는 인게임 카메라의 LookAt/Offset 변형만으로는 플레이어/NPC 구도, 화자 전환, UI 입력 레이어, 복귀 정책을 충분히 표현하기 어렵다.

---

## 목표 아키텍처

```
CameraManager
├── CameraModeController
│   ├── ICameraMode CurrentMode
│   ├── RequestMode(...)
│   ├── PushMode(...)
│   ├── PopMode(...)
│   └── BlendController
│
├── 공용 컨텍스트
│   ├── CameraRuntimeContext
│   ├── CameraRigState
│   ├── CameraRigPose
│   └── CameraModeRequest
│
├── 모드
│   ├── InGameCameraMode
│   ├── FreeCameraMode
│   ├── DialogueCameraMode
│   └── CinematicCameraMode
│
├── 인게임 연출 시퀀스
│   ├── SkillCameraSequence
│   └── KillCameraSequence
│
└── 공용 서비스
    ├── CameraLockOn
    ├── CameraCollision
    ├── CameraDistanceController
    ├── CameraEffectManager
    ├── CameraShaker
    └── CameraRotationTransition
```

### 핵심 원칙

| 원칙 | 설명 |
|------|------|
| 모드는 기본 포즈를 계산한다 | 위치, 회전, FOV, 입력 사용 여부, 충돌 사용 여부를 모드가 결정 |
| 공용 효과는 모드 결과 위에 합성한다 | 쉐이크, FOV 펀치, 줌, 타임스케일 효과는 모드와 별도 |
| 전환은 `CameraManager`가 중앙 관리한다 | 모드가 다른 모드를 직접 켜지 않고 요청 객체를 반환 |
| 복귀 가능한 모드는 스택으로 처리한다 | `DialogueMode`, `CinematicMode`는 `PushMode` 후 종료 시 이전 모드로 복귀 |
| 스킬 카메라는 인게임 연출이다 | 스킬/킬캠은 `InGameMode` 기본 추적 위에 입력 잠금, 회전, FOV, 거리 연출을 얹는다 |
| 인게임은 기본 모드다 | 씬 전환/예외 상황에서는 `InGameMode`로 회복 |

---

## 제안 타입

### CameraModeType

```csharp
namespace UPlayGround.CameraSystem
{
    public enum CameraModeType
    {
        InGame,
        Free,
        Dialogue,
        Cinematic
    }
}
```

### CameraRigPose

모드가 계산한 최종 “기본 카메라 포즈”다. 이 값 위에 `CameraEffectState`를 합성한다.

```csharp
namespace UPlayGround.CameraSystem
{
    public struct CameraRigPose
    {
        public Vector3 PivotPosition;
        public Vector3 CameraPosition;
        public Quaternion CameraRotation;
        public float Yaw;
        public float Pitch;
        public float Distance;
        public float FieldOfView;
    }
}
```

### CameraRigState

프레임 간 유지되어야 하는 런타임 상태다. 기존 `CameraManager`의 `_currentYaw`, `_currentPitch`, `_targetDistance`, `_cameraOffset`, `_smoothPosition` 등을 이쪽으로 이동한다.

```csharp
namespace UPlayGround.CameraSystem
{
    public class CameraRigState
    {
        public float CurrentYaw;
        public float CurrentPitch;
        public float CurrentDistance;
        public float TargetDistance;
        public Vector3 CameraOffset;
        public Vector3 SmoothPosition;
        public Vector3 PositionVelocity;
        public Vector3 OffsetVelocity;
    }
}
```

### CameraRuntimeContext

모드가 필요한 공용 참조 묶음이다. `CameraManager` 내부 필드를 직접 넘기지 않기 위한 컨텍스트다.

```csharp
namespace UPlayGround.CameraSystem
{
    public sealed class CameraRuntimeContext
    {
        public Camera MainCamera;
        public Transform Target;
        public Transform CameraPivot;
        public CameraSettings Settings;
        public CameraRigState State;
        public CameraLockOn LockOn;
        public CameraCollision Collision;
        public CameraDistanceController DistanceController;
        public System.Func<bool> CombatStateProvider;
    }
}
```

### ICameraMode

```csharp
namespace UPlayGround.CameraSystem
{
    public interface ICameraMode
    {
        CameraModeType ModeType { get; }
        int Priority { get; }
        bool AllowsPlayerLookInput { get; }
        bool AllowsZoomInput { get; }
        bool AllowsLockOnInput { get; }
        bool UseCollision { get; }

        void OnEnter(CameraRuntimeContext context, CameraModeEnterParams enterParams);
        void OnExit(CameraRuntimeContext context);
        void HandleInput(CameraRuntimeContext context, float deltaTime);
        CameraRigPose EvaluatePose(CameraRuntimeContext context, float deltaTime);
    }
}
```

### CameraModeEnterParams

모드 진입 시 필요한 값을 담는다. 모든 모드가 같은 파라미터를 쓰지 않으므로 확장 가능한 구조가 필요하다.

```csharp
namespace UPlayGround.CameraSystem
{
    public class CameraModeEnterParams
    {
        public Transform PrimaryTarget;
        public Transform SecondaryTarget;
        public Vector3 WorldPosition;
        public Vector3 Offset;
        public float Duration;
        public AnimationCurve BlendCurve;
        public bool RestorePreviousOnExit = true;
    }
}
```

---

## 모드별 책임

### InGameCameraMode

현재 `CameraManager.OnLateUpdate()`의 주 카메라 흐름을 이 모드로 이동한다.

책임:

- 플레이어 추적 TPS 카메라
- Look/Zoom 입력 처리
- 전투/탐험 오프셋 전환
- 스킬/킬캠 연출 시퀀스 재생
- 락온 회전 반영
- 다수 적 줌아웃
- 충돌 보정
- 경사 피치 보정

사용하는 기존 클래스:

- `CameraLockOn`
- `CameraCollision`
- `CameraDistanceController`
- `CameraRotationTransition`

전환 정책:

- 기본 모드이며 낮은 우선순위
- `DialogueMode`, `CinematicMode` 종료 시 복귀 대상
- 스킬/킬캠 요청은 모드 전환이 아니라 인게임 연출 시퀀스로 처리
- 씬 전환 후 타겟 재설정이 끝나면 자동 진입

### FreeCameraMode

디버그, 포토 모드, 개발용 자유 카메라에 적합한 모드다.

책임:

- 플레이어 타겟 추적을 끊고 카메라 Transform을 직접 이동/회전
- 충돌 사용 여부를 옵션화
- 게임 입력과 카메라 입력을 별도 레이어로 분리
- 필요 시 시간 정지 또는 저속 재생과 함께 사용

권장 정책:

- 기본 게임플레이에서는 비활성
- `CheatManager` 또는 개발 UI에서만 진입
- 종료 시 이전 모드의 `CameraRigState`를 복원하거나 현재 카메라 방향을 `InGameMode` yaw/pitch로 흡수

### SkillCameraSequence

스킬, 피니시, 처형, 킬캠 등 짧은 전투 연출은 독립 모드로 분리하지 않고 `InGameCameraMode` 안의 일시적 시퀀스로 처리한다. 이유는 스킬 연출 중에도 기본 추적 대상, 락온 기준, 충돌 보정, 전투 FOV, MotionEvent 기반 카메라 이펙트를 그대로 재사용할 수 있기 때문이다.

책임:

- 입력 잠금
- 타겟 또는 소켓 기준 회전/거리/FOV 보정
- 지속 시간 기반 자동 종료
- FOV/Distance/Offset 연출을 인게임 기본 포즈 위에 합성
- HitStop 또는 TimeScale 효과와 동기화

권장 정책:

- `PushMode(SkillCam)`를 사용하지 않는다.
- `InGameCameraMode.PlaySequence(SkillCameraSequenceData)` 또는 `CameraManager.PlaySkillCamera(...)` 형태로 진입한다.
- 같은 시퀀스 중복 진입은 별도 `priority` 또는 `interruptPolicy`로 판단한다.
- `MotionEvent_CameraEffect`, `MotionEvent_CameraLookAtSocket`와 연동

### DialogueCameraMode

대화 시스템과 연결되는 독립 모드다. 대화는 인게임 카메라의 단순 LookAt/Offset 변형으로 처리하기 어렵기 때문에 별도 모드로 분리한다.

책임:

- 플레이어/NPC/현재 화자 기준으로 포즈 계산
- 화자 변경 시 카메라 구도 변경
- 플레이어 Look/Zoom/LockOn 입력 차단
- UI 입력 레이어와 충돌하지 않도록 `InputManager` 정책과 연동
- 인게임 락온/전투 줌/군중 줌아웃과 분리된 대화 전용 FOV/거리/구도 사용

권장 정책:

- 대화 시작 시 `PushMode(Dialogue)`
- 대화 종료 시 이전 모드로 복귀
- 화자 변경 이벤트를 받아 `PrimaryTarget`, `SecondaryTarget` 또는 내부 speaker/listener 참조 갱신

---

## 업데이트 흐름

### 현재 흐름

```
CameraManager.OnUpdate
└── HandleInput()

CameraManager.OnLateUpdate
├── LockOn transition/update
├── Align/update offset
├── Distance/FOV
├── Effect update
├── Position
├── Rotation
└── FOV apply
```

### 목표 흐름

```
CameraManager.OnUpdate
├── _shaker.ManualUpdate(dt)
└── _modeController.CurrentMode.HandleInput(context, dt)

CameraManager.OnLateUpdate
├── _modeController.UpdateTransition(dt)
├── basePose = CurrentMode.EvaluatePose(context, dt)
├── fx = _effectManager.UpdateAndComputeState(dt)
├── finalPose = CameraPoseComposer.Compose(basePose, fx)
└── ApplyPose(finalPose)
```

### 효과 합성 순서

```
Mode Pose
  -> Collision 보정(모드가 선택)
  -> CameraEffectState 합성
  -> CameraShaker/Punch 합성
  -> MainCamera Transform/FOV 적용
```

중요한 점은 `ICameraEffect`가 모드 자체를 바꾸지 않고 “현재 모드가 만든 포즈”에 델타를 더하는 구조를 유지하는 것이다.

---

## 모드 전환 정책

### 전환 종류

| 전환 | 용도 |
|------|------|
| `SetMode` | 완전 교체. 예: InGame ↔ Free |
| `PushMode` | 임시 오버레이. 예: InGame 위에 Dialogue, Cinematic |
| `PopMode` | 임시 모드 종료 후 이전 모드 복귀 |
| `ForceMode` | 씬 전환/에러 복구용 강제 전환 |

### 우선순위 제안

| Mode | Priority | 설명 |
|------|----------|------|
| `InGame` | 0 | 기본 플레이 |
| `Free` | 10 | 개발/포토 모드 |
| `Dialogue` | 50 | 대화 중 게임플레이 입력 차단 |
| `Cinematic` | 100 | 컷신/Timeline 연동 |

스킬/킬캠은 모드 우선순위 테이블에 넣지 않는다. `InGame` 내부의 시퀀스 우선순위로 관리한다.

### interrupt policy

| 현재 모드 | 새 요청 | 정책 |
|-----------|---------|------|
| InGame | Dialogue | 허용, Push |
| InGame | SkillCameraSequence | 모드 전환 없이 시퀀스 재생 |
| Dialogue | SkillCameraSequence | 기본 거부. 대화 중 전투 연출은 발생하지 않는 전제로 처리 |
| SkillCameraSequence | SkillCameraSequence | priority가 높으면 교체, 낮으면 무시 |
| Cinematic | Any | 기본 거부 |
| Free | Dialogue/SkillCameraSequence | 개발 옵션에 따라 허용 |

---

## 데이터 설계

### CameraModeSettings

기존 `CameraSettings`가 너무 커지는 것을 막기 위해 모드별 설정 SO를 분리하는 방식을 권장한다.

```
CameraSettings
├── 공용 값
│   ├── collisionOffset
│   ├── cameraRadius
│   ├── rotationSpeed
│   └── fovSmoothTime
│
├── InGameCameraModeSettings
├── FreeCameraModeSettings
└── DialogueCameraModeSettings
```

초기 구현에서는 `CameraSettings` 안에 필드를 추가해도 되지만, `DialogueMode`가 커지기 시작하면 SO 분리가 필요하다. 스킬/킬캠은 `InGameCameraModeSettings` 또는 별도 시퀀스 데이터로 둔다.

### SkillCameraSequenceData

`KillCamData`를 일반화한 인게임 카메라 연출 데이터로 확장할 수 있다.

| 필드 | 설명 |
|------|------|
| `duration` | 시퀀스 유지 시간 |
| `blendInDuration` / `blendOutDuration` | 진입/복귀 보간 |
| `cameraOffset` | 타겟 기준 카메라 오프셋 |
| `lookAtOffset` | 타겟 시선 위치 |
| `fov` | 연출 FOV |
| `timeScale` | 연출 중 시간 배율 |
| `cameraShakeKey` | 쉐이크 프리셋 |
| `priority` | 다른 스킬 카메라 연출과 충돌할 때 우선순위 |

### DialogueCameraSettings

| 필드 | 설명 |
|------|------|
| `speakerLookAtOffset` | 화자 시선 기준점 |
| `listenerShoulderOffset` | 청자 어깨 너머 구도 |
| `twoShotDistance` | 양 캐릭터를 함께 잡는 거리 |
| `speakerCutBlendTime` | 화자 전환 블렌드 시간 |
| `minDistance` / `maxDistance` | 대화 카메라 거리 제한 |
| `fieldOfView` | 대화 모드 전용 FOV |

현재 구현은 `DialogueCameraSettingsSO`로 분리되어 있으며 Addressables 키는 `DialogueCameraSettings`다. 에셋이 없거나 로드 실패하면 런타임 기본값으로 폴백한다. 기본 에셋 생성은 `UPlayGround/Camera/Create Dialogue Camera Settings` 메뉴를 사용한다.

---

## Cinemachine 도입 판단

### 바로 전면 도입하지 않는 이유

- 현재 카메라에는 KCC TPS 추적, 락온 오비탈, 충돌, 경사 보정, 다수 적 줌아웃 등 직접 구현된 게임 특화 로직이 많다.
- `CameraEffectManager`와 MotionEvent 기반 카메라 연출이 이미 프로젝트 패턴에 맞게 구현되어 있다.
- 전면 교체는 입력/락온/킬캠/쉐이크 회귀 위험이 크다.

### 권장 방향

초기에는 **수동 모드 시스템**으로 리팩터링하고, 이후 필요한 모드만 Cinemachine 브릿지로 대체한다.

```
ICameraMode
├── ManualCameraModeBase
│   ├── InGameCameraMode
│   ├── FreeCameraMode
│   └── DialogueCameraMode
│
└── CinemachineCameraMode
    ├── CinemachineDialogueShotMode
    └── CinemachineCinematicMode
```

### Cinemachine이 특히 유리한 영역

- `DialogueMode`: 화자별 Virtual Camera, over-the-shoulder shot, close-up shot
- `CinematicMode`: 컷신/연출 카메라
- `FreeMode`: Cinemachine FreeLook 또는 Orbital Follow 기반 프로토타입

스킬 카메라를 Cinemachine으로 실험할 수는 있지만, 현재 기준의 기본안은 `InGameMode` 안에서 수동 시퀀스로 처리하는 것이다.

### 수동 구현을 유지할 영역

- 기본 TPS `InGameMode`
- 현재 락온 오비탈 정책
- 카메라 충돌의 캐릭터 캡슐 기반 전방 블렌드
- MotionEvent 기반 `CameraEffectManager` 효과 합성

---

## 단계별 구현 로드맵

### 1단계: 타입과 상태 분리 - 완료

- `CameraRigState`, `CameraRigPose`, `CameraRuntimeContext`, `ICameraMode`, `CameraModeType` 추가
- `CameraManager` 내부 상태 필드를 `CameraRigState`로 이동
- 동작 변경 없이 컴파일 통과

구현 파일:

| 파일 | 역할 |
|------|------|
| `Assets/02.Scripts/Camera/Modes/CameraModeType.cs` | 상위 카메라 모드 enum. `InGame`, `Free`, `Dialogue`, `Cinematic` |
| `Assets/02.Scripts/Camera/Modes/ICameraMode.cs` | 모드 수명주기/입력/포즈 평가 인터페이스 |
| `Assets/02.Scripts/Camera/Modes/CameraRigState.cs` | yaw/pitch/distance/offset/smoothing velocity 런타임 상태 |
| `Assets/02.Scripts/Camera/Modes/CameraRigPose.cs` | 모드가 산출하는 기본 포즈 구조체 |
| `Assets/02.Scripts/Camera/Modes/CameraRuntimeContext.cs` | 모드가 참조하는 카메라/타겟/설정/서브시스템 묶음 |
| `Assets/02.Scripts/Camera/Modes/CameraModeEnterParams.cs` | 모드 진입 파라미터 |
| `Assets/02.Scripts/Camera/Modes/InGameCameraMode.cs` | 기본 인게임 모드. 현재는 기존 CameraManager 계산 흐름을 보존하는 얇은 모드 |
| `Assets/02.Scripts/Camera/Modes/CameraModeController.cs` | 모드 등록, 현재 모드 전환, 입력/포즈 위임 |

`CameraManager` 변경:

- `_rigState`, `_cameraContext`, `_modeController` 필드 추가
- `InitializeCameraModes()`, `SyncCameraContext()`, `SyncRigStateFromFields()` 추가
- 기본 모드로 `InGameCameraMode` 등록
- `CurrentCameraMode`, `SetCameraMode(CameraModeType, CameraModeEnterParams)` public API 추가
- 씬 전환 시 `InGame` 모드로 복구
- 기존 `OnUpdate`, `OnLateUpdate` 카메라 계산 흐름은 유지

### 2단계: InGameCameraMode 입력 이전 - 완료

- `CameraManager.HandleInput()`의 Look/Zoom 입력 처리를 `InGameCameraMode.HandleInput()`으로 이동
- `CameraRuntimeContext`에 `IsInputLocked`, `IsAligning`, `ComputeSlopePitchOffset` 추가
- `CameraManager.OnUpdate()`는 모드 입력 호출 전후로 `CameraRigState`를 동기화
- 락온 토글/타겟 전환 입력 콜백은 기존 `CameraManager`에 유지

현재 보존한 책임:

- `CameraManager.OnLateUpdate()`의 TPS 포즈 계산
- 락온 전환/추적 회전
- 카메라 충돌/전방 블렌드
- 경사 피치 보정
- `CameraEffectManager` 효과 합성

### 3단계: InGameCameraMode 포즈 계산 이전 - 완료

- `CameraManager.OnLateUpdate()`의 기본 TPS 계산을 `InGameCameraMode.EvaluatePose()`로 이동
- `CameraManager`는 모드 호출, 효과 합성, 최종 포즈 적용만 수행
- 기존 락온/줌/충돌/쉐이크/킬캠 회귀 확인

구현 내용:

- `ICameraMode.EvaluatePose()`가 `CameraEffectState`를 받아 최종 포즈를 계산하도록 확장
- `InGameCameraMode`로 이전한 책임:
  - `CameraRotationTransition` 갱신
  - 락온 전환/추적 회전
  - 카메라 정렬
  - 전투/탐험 오프셋 보간
  - 거리/FOV 컨트롤러 갱신
  - 경사 피치 보정
  - 충돌 보정 및 전방 카메라 블렌드
  - `CameraEffectState` 기반 yaw/pitch/distance/offset/position/FOV 합성
- `CameraManager.OnLateUpdate()`에 남은 책임:
  - `CameraEffectManager.UpdateAndComputeState()`
  - `CameraModeController.EvaluatePose()`
  - `ApplyCameraPose()`
  - 컨텍스트/상태 동기화

주의:

- 기존 `CameraManager`의 카메라 위치/회전/전방 블렌드 헬퍼는 아직 제거하지 않았다. 다음 회귀 확인 이후 삭제하거나 디버그 비교용으로 유지 여부를 결정한다.

### 4단계: ModeController 확장 - 완료

- `SetMode`, `PushMode`, `PopMode`, `ForceMode` 구현
- 진입/이탈 시 `OnEnter`, `OnExit` 호출
- 모드 전환 블렌드 최소 구현

구현 내용:

- `CameraModeController.PushMode(CameraModeType, CameraModeEnterParams)`
- `CameraModeController.PopMode(CameraModeEnterParams)`
- `CameraModeController.ForceMode(CameraModeType, CameraModeEnterParams)`
- `CameraManager` public API:
  - `PushCameraMode(...)`
  - `PopCameraMode(...)`
  - `ForceCameraMode(...)`
  - `PushDialogueCamera(Transform speaker, Transform listener = null, Vector3 offset = default)`
- 씬 전환 시 `ForceMode(InGame)`으로 스택까지 정리

### 5단계: DialogueCameraMode 기본 골격 - 완료

- `DialogueCameraMode` 추가
- `PrimaryTarget`을 화자, `SecondaryTarget`을 청자/플레이어로 사용
- 진입 시 인게임 카메라 입력 잠금 및 락온 해제
- 종료 시 입력 잠금 해제
- 플레이어/NPC 기준의 대화용 기본 포즈 계산

연결 상태:

- `DialogueManager` Main 채널 노드 진입 시 자동 연동 완료
- 화자 변경 시 같은 `Dialogue` 모드 재진입으로 타겟 갱신
- `SpeakerID -> ActorID -> ActorInstance` 규칙 적용
- 대화 전용 설정 SO

### 6단계: DialogueManager 연동 - 완료

- Main 채널 노드 진입 시 `PushDialogueCamera(...)`
- Main 채널 대화 종료 시 `PopCameraMode()`
- `DialogueNodeSO.speakerId`에서 씬 Actor/Transform을 찾는 규칙 추가
- 화자 변경 시 `DialogueCameraMode` 타겟 갱신

화자 Transform 해석 규칙:

1. `SpeakerActorBindingTableSO`에서 `speakerId -> actorId` 매핑 조회
2. 바인딩 테이블이 없거나 항목이 없으면 `speakerId == actorId`로 폴백
3. `GameObjectManager.AllActors`에서 `ActorId` 일치 인스턴스 조회
4. 없으면 `ActorSpawnManager.GetSpawnedActors(actorId)` 첫 항목으로 폴백
5. 찾지 못하면 대화 카메라 요청을 생략하고 현재 카메라 상태 유지

구현 파일:

| 파일 | 역할 |
|------|------|
| `Assets/02.Scripts/Data/Dialogue/SpeakerActorBindingTableSO.cs` | `speakerId -> actorId` 매핑 테이블. Addressables 키는 `SpeakerActorBindingTable` |
| `Assets/02.Scripts/Data/Dialogue/Editor/SpeakerActorBindingTableGeneratorWindow.cs` | Dialogue SO 파일, NPC 소유 그래프, Actor 후보를 스캔해 매핑 테이블을 생성/갱신하는 에디터 도구 |
| `Assets/02.Scripts/Manager/Dialogue/DialogueManager.cs` | Main 채널 대화 노드 진입/종료를 `CameraManager` 모드 전환에 연결 |
| `Assets/02.Scripts/Camera/Modes/CameraModeController.cs` | 같은 모드 `PushMode` 요청 시 스택을 늘리지 않고 `OnEnter`로 타겟 갱신 |

### 7단계: SkillCameraSequence 정리

- `KillCamController`의 입력 잠금/시간/줌/복귀 흐름을 `InGameCameraMode` 시퀀스 구조로 이전
- `MotionEvent_CameraEffect`, `MotionEvent_CameraLookAtSocket`와 연결
- 스킬별 `SkillCameraSequenceData` 또는 기존 `CameraEffectData` 조합으로 데이터화

### 8단계: FreeCameraMode 추가

- 개발/디버그 목적의 자유 이동 카메라 추가
- `CheatManager` 또는 디버그 UI에서 진입
- 종료 시 현재 방향을 `InGameMode`에 흡수하는 옵션 제공

### 9단계: Cinemachine 브릿지 검토

- Dialogue 또는 Cinematic 중 하나를 작은 범위로 Cinemachine 실험
- Virtual Camera priority/blend를 `ICameraMode` 뒤에 숨김
- 결과가 좋으면 대화/컷신 모드 위주로 확장

---

## 검증 체크리스트

### InGameMode 회귀

- 플레이어 추적이 기존과 동일하게 동작
- Look/Zoom 입력 정상
- 락온 시작/해제/좌우 전환 정상
- 다수 적 줌아웃 정상
- 카메라 충돌과 전방 카메라 블렌드 정상
- 경사 지형 피치 보정 정상
- 씬 전환 후 카메라 타겟 재설정 정상

### 모드 전환

- `InGame -> Dialogue -> InGame` 복귀 시 입력 잠금이 남지 않음
- 전환 중 씬 변경 시 `ForceMode(InGame)`으로 복구
- 같은 모드 중복 요청 정책이 명확하게 동작

### 인게임 카메라 연출

- 스킬/킬캠 시퀀스 시작/종료 시 yaw/pitch/distance/FOV가 튀지 않음
- 시퀀스 중 Look/Zoom/LockOn 입력 차단 여부가 설정대로 동작
- 시퀀스 종료 후 기존 락온/전투 카메라 상태로 자연스럽게 복귀

### 효과 합성

- `CameraEffectData` 기반 FOV/Zoom/Shake가 모든 모드에서 적용
- `StopAllEffects(immediate: true)`가 씬 전환 시 정상 정리
- TimeScale 기반 연출 중 `useUnscaledTime` 효과가 의도대로 동작

### 입력

- Dialogue 중 Look/Zoom/LockOn 입력이 차단
- SkillCameraSequence 중 Look/Zoom/LockOn 입력 차단 여부가 시퀀스 데이터대로 동작
- UI 입력 레이어와 충돌하지 않음
- FreeMode 진입 시 플레이어 조작 차단 여부가 설정대로 동작

---

## 주의 사항

- `CameraManager`를 한 번에 크게 갈아엎지 말고, 먼저 현재 동작을 `InGameCameraMode`로 이동하는 방식이 안전하다.
- `CameraEffectManager`는 모드별로 나누지 않는다. 효과는 카메라 모드 위에 얹히는 공용 레이어로 유지한다.
- 스킬/킬캠 시퀀스가 직접 `Time.timeScale`을 만지기보다 기존 `GameHitStopManager` 또는 `GameCombatManager.Instance.GameHitStop` 흐름을 사용해야 한다.
- `DialogueMode`는 현재 `DialogueManager` Main 채널에 연결되어 있다. `SpeakerActorBindingTable`이 Addressables에 없으면 `speakerId == actorId` 폴백만 사용된다.
- Cinemachine은 대화/컷신 모드부터 실험한다. 기본 TPS 카메라까지 한 번에 대체하면 회귀 범위가 크다.

---

## 다음 구현 후보

완료된 최소 구현 단위:

1. `CameraModeType`, `ICameraMode`, `CameraRigState`, `CameraRigPose`, `CameraRuntimeContext` 추가
2. `InGameCameraMode` 생성
3. `CameraManager`에 `CurrentMode`와 `SetMode(CameraModeType)` 추가
4. 기존 public API는 유지해서 외부 호출부 변경을 최소화
5. Look/Zoom 입력을 `InGameCameraMode.HandleInput()`으로 이전
6. 인게임 TPS 포즈 계산을 `InGameCameraMode.EvaluatePose()`로 이전
7. `PushMode`/`PopMode`/`ForceMode`와 `DialogueCameraMode` 기본 골격 추가
8. `DialogueManager` Main 채널과 `DialogueCameraMode` 자동 연동
9. `SpeakerActorBindingTableSO` 기반 `SpeakerID -> ActorID -> ActorInstance` 매핑 추가
10. `DialogueCameraSettingsSO` 기반 거리/높이/블렌드/FOV 데이터화
11. `SpeakerActorBindingTable` 자동 생성/갱신 에디터 도구 추가

다음 단계:

1. Unity 에디터에서 `UPlayGround/Dialogue/Speaker Actor Binding Generator`를 실행해 실제 `speakerId -> actorId` 데이터를 생성
2. `UPlayGround/Camera/Create Dialogue Camera Settings`로 기본 대화 카메라 설정 에셋 생성 후 구도 값 튜닝
3. 실제 대화 씬에서 `Main` 채널 시작/화자 변경/종료 카메라 복귀 확인
4. 기존 `CameraManager`의 미사용 위치/회전 헬퍼 제거 여부 결정

이 단계까지 완료하면 외부 시스템은 기존처럼 `CameraManager.Instance`를 사용하면서도 내부적으로 모드 기반 구조로 확장할 수 있다.
