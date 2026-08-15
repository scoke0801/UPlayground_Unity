# Camera 시스템 가이드

## 개요

`CameraManager`가 TPS 카메라의 **오케스트레이터**이며, 실제 동작은 도메인별 서브시스템 클래스들이 책임집니다. 락온/충돌/거리/연출 회전/이펙트/쉐이크/킬캠이 각각 독립 클래스로 분리되어 있고, `CameraSettings`(SO)로 튜닝 값을 외부화합니다.

핵심 특징:

- **서브시스템 합성 아키텍처** — `CameraLockOn`, `CameraCollision`, `CameraDistanceController`, `CameraRotationTransition`, `CameraEffectManager`, `CameraShaker`, `KillCamController`
- **블렌딩 가능한 카메라 이펙트** — `ICameraEffect` + `CameraEffectData`(SO) 기반. 우선순위/블렌드 인·아웃 커브/채널(Yaw/Pitch/Distance/FOV/Position/TimeScale 등) 단위로 합성
- **데이터 주도** — `CameraSettings`, `CameraShakeData`, `CameraShakeDatabase`, `KillCamData`, `PerfectGuardFOVData`, 각종 `*CameraEffectData` 모두 SO로 외부화 + Addressables 로드
- **자동 생성 enum** — `CameraShakeIdType` (ID Enum Generator) 로 컴파일 타임 안전 키
- **InputManager 연동** — `LockOn`, `LockOnSwitchLeft/Right` 입력을 `Level_1` 우선순위에 등록
- **외부 회전 락** — 연출 시 입력을 잠그고 부드러운 보간으로 카메라 강제 이동

---

## 아키텍처

```
CameraManager (BaseManager<T>, IManager, ICameraStateAccessor)
│
├── 메인 카메라 / 타겟 / 피벗 / 야우/피치/거리 상태
│
├── 서브시스템 (composition)
│   ├── CameraLockOn              락온 대상 탐색·전환·해제, 추적 회전(Mid-Point), 전환 연출
│   ├── CameraCollision           SphereCast 기반 후방 충돌 + 거리 스무딩 (당김 즉시 / 복귀 부드럽게)
│   ├── CameraDistanceController  FOV 전환 + 전투/락온 거리 보정 + 다수 적 줌아웃
│   ├── CameraRotationTransition  SetRotationSmooth → 매 프레임 SmoothStep/Curve 보간
│   ├── CameraEffectManager       ICameraEffect 활성 리스트 관리·블렌딩·CameraEffectState 합성
│   ├── CameraShaker (MonoBehaviour) 카메라 쉐이크 + 방향성 펀치 (ManualUpdate)
│   └── KillCamController         적 사망 시 슬로모션 + 줌인 + 쉐이크 시퀀스
│
├── 외부 데이터
│   ├── CameraSettings (SO)        Addressables: "CameraSettings"
│   ├── CameraShakeDatabase (SO)   Addressables: "CameraShakeDatabase"
│   ├── KillCamData (SO)           Addressables: "KillCamData"
│   └── PerfectGuardFOVData (SO)   Addressables: "PerfectGuardFOV"
│
└── InputManager 등록 (AfterInit, InputLayer.Level_1)
       LockOn / LockOnSwitchLeft / LockOnSwitchRight


이펙트 합성 흐름 (LateUpdate):
  CameraEffectManager.UpdateAndComputeState(dt)
        │
        ▼
  CameraEffectState (Yaw/Pitch/Distance/Offset/FOV/Position/TimeScale 델타)
        │
        ▼
  CameraManager.OnLateUpdate가 베이스 트랜스폼 + 충돌·락온 보정에 합산
```

### 파일 구조

```
Assets/02.Scripts/
├── Manager/
│   └── CameraManager.cs                    오케스트레이터 + Public API + Input 등록
│
├── Camera/
│   ├── CameraLockOn.cs                     락온 시스템
│   ├── CameraCollision.cs                  충돌 + 거리 스무딩
│   ├── CameraDistanceController.cs         FOV/거리/줌아웃
│   ├── CameraRotationTransition.cs         스무스 회전 전환
│   ├── CameraShaker.cs                     쉐이크 + 펀치 (MonoBehaviour)
│   ├── KillCamController.cs                킬캠 시퀀스
│   └── Effects/
│       ├── ICameraEffect.cs                이펙트 인터페이스
│       ├── ICameraStateAccessor.cs         CameraManager 상태 접근자
│       ├── CameraEffectChannel.cs          [Flags] 채널 enum
│       ├── CameraEffectState.cs            합성 결과 구조체
│       ├── CameraEffectManager.cs          이펙트 풀 관리
│       ├── BaseCameraEffect.cs             공통 블렌드 인/아웃 베이스
│       ├── RotationCameraEffect.cs         회전
│       ├── ZoomCameraEffect.cs             줌
│       ├── FOVCameraEffect.cs              FOV
│       ├── ShakeCameraEffect.cs            쉐이크
│       ├── TimeScaleCameraEffect.cs        시간 스케일
│       ├── SmoothDampCameraEffect.cs       스무스 댐프
│       └── SpringDampCameraEffect.cs       스프링 댐프
│
└── Data/Camera/
    ├── CameraSettings.cs                   메인 설정 SO
    ├── CameraShakeData.cs                  쉐이크 데이터 SO
    ├── Editor/CameraShakerDataEditor.cs    쉐이크 미리보기 인스펙터
    └── Effects/
        ├── RotationCameraEffectData.cs
        ├── ZoomCameraEffectData.cs
        ├── FOVCameraEffectData.cs
        ├── ShakeCameraEffectData.cs
        ├── TimeScaleCameraEffectData.cs
        ├── SmoothDampCameraEffectData.cs
        └── SpringDampCameraEffectData.cs

Assets/02.Scripts/Data/Path/
├── CameraShakeDatabase.cs                  키 → CameraShakeData 매핑
└── CameraShakeIdType.cs                    자동 생성 enum (ID Enum Generator)
```

---

## 핵심 클래스 / API

### CameraManager (Public API 발췌)

#### 타겟·상태 접근

| API | 용도 |
|-----|------|
| `SetTarget(Transform)` | 추적 대상 변경 |
| `SnapToTarget(Vector3)` | 카메라 즉시 스냅 |
| `GetTarget()` / `GetCurrentYaw()` / `GetCurrentPitch()` / `GetCurrentDistance()` / `GetCurrentOffset()` / `GetCurrentFOV()` / `GetBaseFOV()` / `GetTargetFOV()` | 현재 상태 조회 (UI/디버그/외부 시스템) |

#### 회전·거리·오프셋 제어

| API | 용도 |
|-----|------|
| `SetDistance(float)` | 목표 거리 설정 |
| `SetRotation(yaw, pitch)` | 즉시 회전 |
| `SetRotationSmooth(yaw, pitch, duration, unlockOnComplete=false)` | SmoothStep 보간 |
| `SetRotationSmooth(yaw, pitch, duration, AnimationCurve, unlockOnComplete=false)` | 커브 기반 보간 |
| `SetCameraOffset(Vector3)` | 즉시 오프셋 설정 |
| `SetInputLock(bool)` | 입력 잠금 (연출용) |
| `SetCombatStateProvider(Func<bool>)` | 전투 상태 판정 콜백 주입 (CameraDistanceController 사용) |

#### 쉐이크·펀치

| API | 용도 |
|-----|------|
| `StartShake(CameraShakeData)` | 데이터로 직접 쉐이크 시작 |
| `StartShake(string key)` | DB에서 키 조회 후 쉐이크 |
| `StartShake(CameraShakeIdType)` | enum 키 (권장) |
| `StopShake()` | 쉐이크 중단 |
| `Punch(direction, strength, duration=0.15)` | 방향성 펀치 |

#### 이펙트 (블렌딩 가능)

| API | 용도 |
|-----|------|
| `PlayEffect(CameraEffectData)` | SO 데이터로 인스턴스 생성·재생 |
| `StopEffect(ICameraEffect, immediate=false)` | 핸들로 정지 |
| `StopEffect(string effectId, immediate=false)` | effectId 매칭 정지 |
| `StopAllEffects(immediate=false)` | 일괄 정지 |

#### 락온

| API | 용도 |
|-----|------|
| `IsLockOnActive()` | 활성 여부 |
| `GetLockOnTarget()` | 현재 락온 타겟 |
| `SetLookAtOverride(Transform, Vector3 offset=default)` / `ClearLookAtOverride()` | 외부 오브젝트로 LookAt 강제 |

#### 킬캠

| API | 용도 |
|-----|------|
| `TryKillCam(Transform victim) → bool` | 확률·쿨다운 체크 후 킬캠 시퀀스 시작 |

#### 런타임 튜닝 (개발/치트)

| API | 용도 |
|-----|------|
| `SetDefaultOffset(Vector3)` / `SetCombatOffset(Vector3)` | 기본/전투 오프셋 |
| `SetFOVSettings(explore, combat, lockOn)` | 모드별 FOV |
| `SetLockOnDistance(float)` | 락온 거리 |
| `SetCrowdZoomSettings(zoomOutDist, detectRadius, threshold)` | 다수 적 줌아웃 |
| `SetLockOnHeightDampSettings(damp, pitchSpeed)` | 락온 고저차 감쇠와 추종 속도 |

### CameraSettings (SO)

`Addressables: "CameraSettings"` 키로 로드. 오프셋·FOV·거리·충돌·락온·줌아웃·블렌드 시간 등 모든 튜닝 값.

### CameraShakeDatabase (SO)

`Addressables: "CameraShakeDatabase"` 키. `CameraShakeIdType` enum → `CameraShakeData` SO 매핑. `CameraManager.StartShake(CameraShakeIdType.HeavyHit)` 한 줄로 사용 가능.

### CameraShaker (MonoBehaviour)

자체 컴포넌트. CameraManager가 내부에서 보유하고 `ManualUpdate(dt)`로 매 프레임 호출 (자동 Update를 끄고 매니저가 직접 틱). 이유: 시간 스케일·연출 컨트롤을 매니저가 통제.

| 메서드 | 용도 |
|--------|------|
| `SetAutoUpdate(bool)` | 자동 갱신 토글 (기본 매니저가 false로 고정) |
| `ManualUpdate(float)` | 외부 틱 |
| `SetShakeData(CameraShakeData)` | 데이터 갱신 |
| `EditorPreview` (static) | 에디터 프리뷰 토글 |

### CameraLockOn

| 멤버 | 용도 |
|------|------|
| `IsActive` | 락온 중 여부 |
| `CurrentTarget` | 현재 타겟 Transform |
| `TryActivate()` | `CollectTargets()` 후 첫 타겟에 락 |
| `Switch(direction)` | 좌/우 타겟 전환 (인덱스 변경) |
| `Deactivate()` | 락 해제 + 전환 연출 시작 |

내부적으로 거리 + 카메라 정면 가중치(`sortScore`)로 정렬, Y축/오비탈 오프셋 스무딩으로 안정적인 추적 회전 구현.

### CameraCollision

`SphereCast`로 카메라 방향에 장애물이 있는지 검사하고 거리를 부드럽게 보정. **당김(가까워지는 방향)은 즉시**, **복귀(멀어지는 방향)는 부드럽게** — 시야 차단을 빠르게 해소하면서 흔들림은 줄이는 정책.

### CameraDistanceController

- **FOV 전환** — `UpdateFOV(isLockOn, isCombat)` 으로 Explore/Combat/LockOn FOV 간 SmoothDamp 보간
- **다수 적 줌아웃** — 일정 반경 내 적 수가 임계값 이상이면 자동 줌아웃
- **반환값 -1** — 유저 줌(수동 조정값)을 유지

### CameraRotationTransition

`Start(fromYaw, fromPitch, toYaw, toPitch, duration, minPitch, maxPitch, curve, unlockOnComplete)` 한 번 호출하면 매 프레임 보간. `unlockOnComplete=true` 면 완료 시 입력 잠금 자동 해제.

### CameraEffectManager + ICameraEffect

이펙트 인터페이스:

```csharp
public interface ICameraEffect
{
    string  EffectId { get; }
    int     Priority { get; }
    float   Weight   { get; }   // 0~1 블렌드
    bool    IsActive { get; }
    bool    IsFinished { get; }
    CameraEffectChannel AffectedChannels { get; }

    void Init(ICameraStateAccessor cameraState);
    void Play();
    void Stop(bool immediate = false);
    void UpdateEffect(float deltaTime);
    void Apply(ref CameraEffectState state);
    void ForceDispose();
}
```

`CameraEffectChannel` (Flags):

```
None / Yaw / Pitch / Distance / Offset / FOV / Position / TimeScale / SmoothDamp
```

`CameraEffectData` (베이스 SO):

```csharp
public string         effectKey;
public int            priority;
public float          duration;          // 0 = 무한 (수동 Stop)
public float          blendInDuration;
public float          blendOutDuration;
public AnimationCurve blendInCurve;
public AnimationCurve blendOutCurve;
public bool           useUnscaledTime;

public abstract ICameraEffect CreateEffect();
```

상속 가능한 데이터 SO (현재 제공):

| Data SO | 효과 |
|---------|------|
| `RotationCameraEffectData` | 카메라 회전 변형 |
| `ZoomCameraEffectData` | 거리 변경 |
| `FOVCameraEffectData` | FOV 변경 |
| `ShakeCameraEffectData` | 셰이크 |
| `TimeScaleCameraEffectData` | 시간 스케일 |
| `SmoothDampCameraEffectData` | 스무스 댐프 |
| `SpringDampCameraEffectData` | 스프링 댐프 |

런타임:

```csharp
ICameraEffect handle = CameraManager.Instance.PlayEffect(effectDataSO);
// ...
CameraManager.Instance.StopEffect(handle);
```

### KillCamController

적 사망 직후 슬로모션 + 줌인 + 쉐이크 시퀀스를 한 번에 재생. `KillCamData`(SO)에 확률/쿨다운/연출 파라미터 정의. `PlayerCombat.PerformHitDetection`에서 킬 감지 시 `CameraManager.TryKillCam(victim)` 호출.

---

## 사용 예시

### 1. 락온 토글 (입력은 매니저가 자동 등록)

```csharp
// 별도 코드 불필요. CameraManager.AfterInit에서:
input.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.LockOn,
    null, OnLockOnPerformed, null, null, null, InputLayer.Level_1);
```

다른 시스템이 락온 상태를 알아야 하면:

```csharp
if (CameraManager.Instance.IsLockOnActive())
{
    var target = CameraManager.Instance.GetLockOnTarget();
    // ...
}
```

### 2. 카메라 쉐이크 (히트 반응)

```csharp
// enum 키 사용 (권장) — 컴파일 타임 안전
CameraManager.Instance.StartShake(CameraShakeIdType.HeavyHit);

// 또는 데이터 직접 전달
CameraManager.Instance.StartShake(myCustomShakeData);

// 펀치 (방향성 임팩트)
CameraManager.Instance.Punch(
    direction: hitDirection,
    strength:  0.5f,
    duration:  0.15f);
```

### 3. 연출용 카메라 이펙트 재생

```csharp
[SerializeField] private FOVCameraEffectData _focusFOV;

// 페이즈 시작 시
var fx = CameraManager.Instance.PlayEffect(_focusFOV);
StartCoroutine(StopAfter(fx, 3f));

IEnumerator StopAfter(ICameraEffect e, float t)
{
    yield return new WaitForSeconds(t);
    CameraManager.Instance.StopEffect(e);   // 블렌드아웃
}
```

> `duration > 0` 이면 자동 종료. 0이면 명시적 `StopEffect` 필요.

### 4. 연출용 강제 회전

```csharp
// 보스 등장 컷씬 — 1초간 보스를 향해 카메라를 회전 + 입력 잠금
CameraManager.Instance.SetInputLock(true);
CameraManager.Instance.SetRotationSmooth(
    yaw: bossYaw, pitch: -10f,
    duration: 1.0f,
    unlockOnComplete: true);  // 완료 시 자동 입력 해제
```

### 5. LookAt 오버라이드 (특정 오브젝트 추적)

```csharp
// 인터랙션 중 NPC 얼굴로 카메라 시선 고정
CameraManager.Instance.SetLookAtOverride(npc.transform, offset: Vector3.up * 1.5f);

// 인터랙션 종료
CameraManager.Instance.ClearLookAtOverride();
```

### 6. 킬캠

```csharp
// PlayerCombat에서 킬 감지 직후
if (CameraManager.Instance.TryKillCam(victim.transform))
{
    // 킬캠 시퀀스 진입 (확률·쿨다운 통과 시 true)
}
```

---

## 셋업 방법

1. **CameraSettings SO**
   - `Create → UPlayGround/Camera/CameraSettings` 로 생성 (메뉴 정확명은 SO 코드 참조)
   - 인스펙터에서 오프셋·FOV·거리·블렌드 시간 등 튜닝
   - Addressables 키 `CameraSettings`로 등록
2. **CameraShake DB + 프리셋**
   - `Generator Tool → Camera Shake Presets` 로 기본 프리셋 일괄 생성 (`CameraShakeData` SO)
   - `CameraShakeDatabase` SO 만들어 키 → 데이터 매핑
   - Addressables 키 `CameraShakeDatabase`
   - `ID Enum Generator` 로 `CameraShakeIdType.cs` 생성
3. **KillCamData / PerfectGuardFOVData**
   - 각 SO 생성 후 Addressables 키 `KillCamData` / `PerfectGuardFOV` 로 등록
4. **씬 셋업**
   - 메인 카메라(`Camera.main`) 1개
   - 플레이어에 `Player` 태그 (CameraManager가 `_target`을 어떻게 잡는지는 SetTarget 호출자 측에서 보장)
   - 카메라 충돌·락온 LayerMask 는 `CameraConfig` 정적 헬퍼 통해 부여
5. **GameManager 등록**
   - `[6] CameraManager` 가 `[2] InputManager` 이후에 초기화되어야 `AfterInit`에서 입력 등록 가능

---

## 주의 사항

- **InputManager 의존.** CameraManager.AfterInit는 InputManager에 LockOn/LockOnSwitchLeft/LockOnSwitchRight를 `Level_1` 우선순위로 등록한다. InputManager가 먼저 초기화되어 있어야 한다.
- **Addressables 비동기 로드 race.** `CameraShakeDatabase`, `KillCamData`, `PerfectGuardFOVData`는 Init 직후 비동기 로드. 부팅 직후 호출은 묵음 또는 폴백.
- **CameraSettings 동기 로드.** `Init` 시 settings가 null이면 `LoadSettingsSync()` 사용 (블로킹). 빌드 환경에서 Addressables 카탈로그가 준비되어야 작동한다는 점에 유의.
- **타겟 null 케이스.** `_target`이 null이면 락온/충돌/거리 컨트롤러가 생성되지 않는다. 씬 전환 후 타겟을 다시 `SetTarget`으로 부여하지 않으면 카메라 시스템이 동작하지 않음.
- **SetInputLock 페어링.** 연출 시작 시 `SetInputLock(true)`로 잠갔다면 종료 시 반드시 `SetInputLock(false)` 또는 `SetRotationSmooth(unlockOnComplete: true)`로 해제. 잊으면 카메라 입력이 잠긴 채로 남는다.
- **CameraShaker는 매니저가 ManualUpdate.** Shaker의 자동 Update는 꺼져 있다. 별도 씬에서 매니저 없이 사용하려면 `SetAutoUpdate(true)` 필요.
- **이펙트 Priority/Channel 충돌.** 동일 채널을 다루는 이펙트가 다수 활성화되면 가중치 합산 + 우선순위 적용. 결과가 의도와 다르면 `Priority` 조정 또는 `StopEffect`로 정리.
- **킬캠 중 입력 차단.** KillCam은 시작 시 카메라 입력을 잠그고 종료 시 해제. 시퀀스 도중 외부에서 `SetInputLock(false)`를 호출하지 말 것.
- **ScreenSpace 쉐이크 vs WorldSpace.** `CameraShaker.ShakeSpace`로 두 모드 지원. 차이를 인지하고 데이터에서 선택.

---

## 확장 포인트

### 신규 카메라 이펙트 추가

1. `BaseCameraEffect` 상속한 런타임 클래스 작성 (`Camera/Effects/`)
2. `CameraEffectData` 상속한 SO 클래스 작성 + `CreateEffect()`에서 신규 이펙트 인스턴스화
3. SO 인스턴스 만들고 코드/인스펙터에서 `PlayEffect(data)`

### 채널 추가

`CameraEffectChannel` enum에 비트 추가 → `CameraEffectState`에 해당 필드 추가 → `CameraManager.OnLateUpdate`에서 합성 코드 추가.

### 락온 룰 변경

`CameraLockOn.CollectTargets` / `sortScore` 계산식을 변경하면 우선순위 정책 교체. 예: 거리 vs 화면 중심 가중치 조정, 적 종류별 가산점.

### 신규 쉐이크 키

1. `CameraShakeData` SO를 새로 만들어 `CameraShakeDatabase`에 추가
2. `Generator Tool → ID Enum Generator` 실행 → `CameraShakeIdType.cs` 갱신
3. 코드에서 `StartShake(CameraShakeIdType.X)` 사용

### 카메라 모드 추가 (예: Aim 모드)

`CameraDistanceController.UpdateFOV` 분기에 새 모드 추가, `CameraSettings`에 새 FOV 필드 + 거리/오프셋 셋, `CameraManager`에 `IsAiming` 플래그 / 진입·이탈 API 추가.

### KillCam 시퀀스 변경

`KillCamData` 파라미터로 슬로모션 비율·연출 시간·줌·쉐이크 키 등을 조정. 시퀀스 자체를 바꾸려면 `KillCamController._activeSequence` 코루틴 본문을 편집.
