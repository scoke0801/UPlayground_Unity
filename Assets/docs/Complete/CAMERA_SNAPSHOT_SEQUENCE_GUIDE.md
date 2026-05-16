# Camera Snapshot Sequence 가이드

## 개요

`CameraSnapshotSequence`는 런타임 카메라 포즈를 여러 개의 스냅샷으로 저장하고, `CameraManager`의 모드 스택 위에서 순차 재생하는 카메라 연출 시스템이다.

핵심 특징:

- `CameraSnapshotProfile` ScriptableObject에 위치, 회전, FOV, 지속 시간, 블렌드 커브를 샷 단위로 저장
- `CameraSnapshotSequenceMode`가 `ICameraMode`로 등록되어 기존 `InGame`, `Dialogue`, `Free` 모드와 같은 전환 경로 사용
- `MotionEvent`에서 프로필을 실행해 공격/스킬 애니메이션 타임라인과 카메라 시퀀스를 동기화
- 월드 좌표와 액터 상대 좌표를 모두 지원해 고정 컷신과 캐릭터 기준 스킬 연출을 분리 가능
- 액터 상대 좌표는 런타임 `Transform` 참조가 아니라 Actor ID와 `ActorSocketType`으로 기준을 저장하고, 사용할 때 Actor/Socket을 찾아 해석
- 전용 에디터 창에서 프리카메라, 현재 카메라 캡처, 시퀀스 미리보기, 샷 순서 편집 지원

---

## 아키텍처

```
MotionSetAsset
└── CameraSnapshotSequenceEvent
    └── CameraManager.PushCameraSnapshotSequence(profile, actorAnchorRef, lookAtRef)
        └── CameraModeController.PushMode(CameraSnapshotSequence)
            └── CameraSnapshotSequenceMode
                ├── CameraSnapshotProfile
                ├── CameraSnapshotActorReferenceResolver
                ├── CameraSnapshotShot.ResolveWorldPose(...)
                ├── CameraEffectState 합성
                └── profile.restorePreviousModeOnFinish이면 PopCameraMode()
```

### 파일 구조

```
Assets/02.Scripts/
├── Data/
│   ├── Camera/
│   │   ├── CameraSnapshotProfile.cs
│   │   └── Editor/CameraSnapshotEditorWindow.cs
│   └── Event/Animation/
│       └── MotionEvent_CameraSnapshotSequence.cs
├── Camera/Modes/
│   ├── CameraModeType.cs
│   ├── CameraModeEnterParams.cs
│   └── CameraSnapshotSequenceMode.cs
├── Camera/
│   ├── CameraSnapshotActorReferenceResolver.cs
│   └── CameraSnapshotSequenceTrigger.cs
└── Manager/
    └── CameraManager.cs

Assets/10.Datas/Camera/SnapShot/
└── CameraSnapshotProfile.asset
```

---

## 핵심 클래스

### CameraSnapshotProfile

`Assets/02.Scripts/Data/Camera/CameraSnapshotProfile.cs`

| 필드 | 설명 |
|------|------|
| `sequenceName` | 에디터 표시용 시퀀스 이름. 비어 있으면 `OnValidate()`에서 에셋 이름으로 보정 |
| `useUnscaledTime` | `true`면 `Time.unscaledDeltaTime` 기준으로 재생 |
| `restorePreviousModeOnFinish` | 마지막 샷 종료 후 이전 카메라 모드로 복귀 |
| `lockCameraInput` | 시퀀스 중 카메라 입력 잠금 |
| `releaseLockOnOnEnter` | 진입 시 락온 해제 |
| `applyFirstShotImmediately` | 첫 샷을 즉시 적용해 진입 블렌드를 생략 |
| `useCollision` | 스냅샷 위치에 카메라 충돌 보정 적용 |
| `entryBlendDuration` | 현재 카메라에서 첫 샷으로 진입하는 전용 블렌드 시간 |
| `entryBlendCurve` | 진입 블렌드 보간 커브 |
| `playbackSpeed` | 전체 시퀀스 재생 속도. `2`면 샷 간 전환 시간이 절반으로 줄어듦 |
| `priority` | 다른 스냅샷 시퀀스와 충돌할 때 비교하는 우선순위 |
| `interruptPolicy` | 이미 스냅샷 시퀀스가 재생 중일 때 처리 정책 |
| `actorAnchor` | Actor ID와 Socket 기준 액터 상대 좌표 해석 기준 |
| `lookAtTarget` | Actor ID와 Socket 기준 LookAt 기준 |
| `shots` | 순차 재생할 `CameraSnapshotShot` 목록 |
| `TotalDuration` | 모든 샷의 `duration` 합산값 |

`CreateAssetMenu` 경로:

```csharp
[CreateAssetMenu(fileName = "CameraSnapshotProfile", menuName = "UPlayGround/SO/Camera/Camera Snapshot Profile")]
```

### CameraSnapshotActorReference

`CameraSnapshotActorReference`는 런타임 `Transform`을 직접 저장하지 않고, Actor ID와 Socket 타입만 저장한다.

| 필드 | 설명 |
|------|------|
| `useActivePlayerWhenEmpty` | Actor ID가 비어 있을 때 현재 활성 플레이어를 사용 |
| `actorIdType` | 자동 생성 `ActorIdType` enum |
| `actorId` | `actorIdType == None`일 때 사용하는 문자열 Actor ID |
| `socketType` | `ActorSocketType`. `None`이면 Actor 루트 Transform 사용 |

해석 순서:

1. `actorIdType != None`이면 `actorIdType.ToActorId()` 사용
2. 아니면 `actorId` 문자열 사용
3. Actor ID가 비어 있고 `useActivePlayerWhenEmpty == true`면 `GameObjectManager.Player` 사용
4. `GameObjectManager.AllActors`에서 Actor ID가 같은 런타임 Actor 검색
5. 없으면 `ActorSpawnManager.GetSpawnedActors(actorId)` 첫 항목 사용
6. Actor를 찾았고 `socketType != None`이면 `GameActor.TryGetSocket(socketType)`로 Socket Transform 사용
7. Socket이 없으면 Actor 루트 Transform 사용

프로필, MotionEvent, 맵 트리거에는 Transform 참조가 직렬화되지 않는다. 씬/런타임에서 실제 Actor 인스턴스가 바뀌어도 Actor ID와 Socket 규칙만 맞으면 같은 프로필을 재사용할 수 있다.

### CameraSnapshotShot

| 필드 | 설명 |
|------|------|
| `shotName` | 샷 이름 |
| `space` | `World` 또는 `ActorRelative` |
| `position` | 좌표계 기준 카메라 위치 |
| `rotationEuler` | 좌표계 기준 카메라 회전 |
| `fieldOfView` | 샷 FOV |
| `duration` | 이전 포즈에서 이 샷으로 보간하는 시간 |
| `blendCurve` | `rawT`를 보정하는 커브 |
| `moveType` | 이전 샷에서 현재 샷으로 이동할 때의 위치 보간 방식 |
| `orbitDirection` | `OrbitAroundAnchor` 이동 시 공전 방향 |
| `keepLookAtTargetDuringBlend` | 공전 보간 중 중심점을 계속 바라볼지 여부 |

주요 API:

```csharp
public void Capture(Camera camera, Transform actorAnchor, CameraSnapshotSpace captureSpace)
public void ResolveWorldPose(Transform actorAnchor, out Vector3 worldPosition, out Quaternion worldRotation)
```

좌표계 정책:

| Space | 저장 방식 | 권장 용도 |
|------|-----------|----------|
| `World` | 카메라 월드 위치/회전 그대로 저장 | 고정 장소 컷신, 환경 연출 |
| `ActorRelative` | `actorAnchor`가 해석한 Actor Socket 기준 로컬 위치/회전으로 저장 | 스킬, 처형, 캐릭터 중심 연출 |

이동 방식:

| MoveType | 설명 | 권장 용도 |
|----------|------|----------|
| `Linear` | 기존 방식. 이전 카메라 위치에서 다음 샷 위치까지 직선 보간 | 컷 사이가 짧거나 직선 이동이 자연스러운 경우 |
| `OrbitAroundAnchor` | 중심점을 기준으로 수평 각도, 반지름, 높이를 보간해 공전하듯 이동 | 캐릭터/타겟을 중심에 두고 둘러보는 스킬·시네마틱 |

공전 중심 우선순위:

1. `lookAtTarget`
2. `actorAnchor`
3. 다음 샷의 `PivotPosition`

`OrbitAroundAnchor`는 수평면 기준 각도를 보간하고, 높이와 반지름은 각각 선형 보간한다. 시작/도착 위치가 중심점에 너무 가까우면 안전하게 `Linear` 보간으로 폴백한다.

### CameraSnapshotSequenceMode

`Assets/02.Scripts/Camera/Modes/CameraSnapshotSequenceMode.cs`

`ICameraMode` 구현체이며 `CameraModeType.CameraSnapshotSequence`로 등록된다.

| 속성 | 값 | 의미 |
|------|----|------|
| `Priority` | `100` | 현재 값은 문서화된 우선순위이며 `CameraModeController`의 스택 정책이 실제 전환을 담당 |
| `AllowsPlayerLookInput` | `false` | 플레이어 Look 입력 비허용 |
| `AllowsZoomInput` | `false` | 줌 입력 비허용 |
| `AllowsLockOnInput` | `false` | 락온 입력 비허용 |
| `UseCollision` | `false` | 카메라 충돌 보정 미사용 |

재생 흐름:

1. `OnEnter()`에서 프로필과 Actor/Socket 참조 값을 선택한다. 실제 Transform은 캐시하지 않는다.
2. `entryBlendDuration > 0`이고 `applyFirstShotImmediately == false`이면 현재 카메라에서 첫 샷까지 전용 진입 블렌드를 먼저 처리한다.
3. `EvaluatePose()`에서 현재 샷의 경과 시간과 블렌드 커브를 평가한다.
4. `BuildPoseFromShot()`이 `CameraSnapshotActorReferenceResolver`로 Actor/Socket을 찾아 샷 좌표를 월드 포즈로 변환한다.
5. `lookAtTarget` Actor/Socket을 찾으면 샷 회전 대신 해당 타겟을 바라보는 회전을 사용한다.
6. `useCollision`이 켜져 있으면 액터 앵커에서 카메라 위치 방향으로 `CameraCollision.Evaluate()`를 적용한다.
7. 샷의 `moveType`이 `Linear`면 직선 보간, `OrbitAroundAnchor`면 중심 기준 공전 보간을 적용한다.
8. `CameraEffectState`의 위치, 회전, 거리, FOV 델타를 합성한다.
9. 마지막 샷 종료 시 `OnComplete`를 호출하고, `restorePreviousModeOnFinish`가 `true`면 `PopCameraMode()`를 호출한다.

주의할 점:

- `UseCollision` 속성은 모드 인터페이스상 `false`지만, 프로필의 `useCollision`을 켜면 스냅샷 위치 계산 단계에서 충돌 보정이 적용된다.
- `_profile.useUnscaledTime`이 `true`면 히트스톱이나 슬로모션 중에도 카메라 시퀀스가 실제 시간 기준으로 진행된다.
- `applyFirstShotImmediately`가 `true`이면 첫 샷의 `duration`은 첫 위치로 이동하는 시간이 아니라 첫 샷 유지 시간처럼 동작한다. 첫 샷까지 부드럽게 진입하려면 `applyFirstShotImmediately = false`, `entryBlendDuration > 0`으로 둔다.

### CameraManager 연동

`CameraManager.InitializeCameraModes()`에서 모드를 등록한다.

```csharp
_modeController.Register(new CameraSnapshotSequenceMode());
```

외부 진입 API:

```csharp
public bool PushCameraSnapshotSequence(
    CameraSnapshotProfile profile,
    System.Action onComplete = null)

public bool PushCameraSnapshotSequence(
    CameraSnapshotProfile profile,
    CameraSnapshotActorReference? actorAnchor,
    CameraSnapshotActorReference? lookAtTarget,
    System.Action onComplete = null)
```

동작:

- `profile == null`이면 경고 후 `false`
- Actor/Socket override가 없으면 `CameraSnapshotProfile.actorAnchor`, `CameraSnapshotProfile.lookAtTarget` 사용
- `SnapshotProfile`과 `RestorePreviousOnExit`을 `CameraModeEnterParams`에 넣고 `PushCameraMode(CameraSnapshotSequence, ...)` 호출
- 현재 모드가 이미 `CameraSnapshotSequence`이면 새 프로필의 `interruptPolicy`와 `priority`로 재생 가능 여부를 판단

보조 API:

```csharp
public bool IsCameraSnapshotSequenceActive(CameraSnapshotProfile profile = null)
public bool StopCameraSnapshotSequence(CameraSnapshotProfile profile = null)
```

### CameraSnapshotSequenceEvent

`Assets/02.Scripts/Data/Event/Animation/MotionEvent_CameraSnapshotSequence.cs`

MotionSet 타임라인에서 카메라 스냅샷 시퀀스를 실행하는 이벤트다.

| 필드 | 설명 |
|------|------|
| `profile` | 실행할 `CameraSnapshotProfile` |
| `overrideActorAnchor` | 프로필의 Actor Anchor 대신 이벤트의 Actor/Socket 참조 사용 |
| `actorAnchor` | 이벤트에서 override할 Actor/Socket 참조 |
| `overrideLookAtTarget` | 프로필의 LookAt Target 대신 이벤트의 Actor/Socket 참조 사용 |
| `lookAtTarget` | 이벤트에서 override할 LookAt Actor/Socket 참조 |
| `restorePreviousOnComplete` | 이벤트 완료 시 현재 모드가 `CameraSnapshotSequence`이면 `PopCameraMode()` 호출 |

주요 API:

```csharp
public override void Execute(GameObject target)
public override void OnCompleteEvent(GameObject target)
```

`Execute()`는 `CameraManager.Instance.PushCameraSnapshotSequence(...)`를 호출하고, `OnCompleteEvent()`는 MotionEvent 종료 시점에 수동 복귀를 보장한다.

---

## 셋업 방법

### 1. 프로필 생성

방법 1: ScriptableObject 메뉴

```
Create > UPlayGround > SO > Camera > Camera Snapshot Profile
```

방법 2: 전용 에디터

```
UPlayGround/Camera/Camera Snapshot 에디터
```

에디터의 `새 프로필 생성` 버튼은 기본 저장 위치를 `Assets/10.Datas`로 제안한다. 현재 샘플 에셋은 `Assets/10.Datas/Camera/SnapShot/CameraSnapshotProfile.asset`에 있다.

### 2. 샷 캡처

1. `Camera Snapshot 에디터`를 연다.
2. `프로필`에 `CameraSnapshotProfile`을 지정한다.
3. `캡처 카메라`를 지정하거나 비워서 `Camera.main` 또는 마지막 `SceneView` 카메라를 사용한다.
4. 캐릭터 기준 연출이면 `액터 기준`을 지정하고 `캡처 좌표계`를 `ActorRelative`로 둔다.
5. `현재 카메라 스냅샷 추가`로 샷을 추가한다.
6. 샷별 `지속 시간`, `FOV`, `블렌드 커브`를 조정한다.

### 3. MotionSet에 연결

MotionSet 이벤트 타임라인에 `CameraSnapshotSequenceEvent`를 추가한다.

권장 설정:

| 필드 | 권장값 |
|------|--------|
| `profile` | 연출용 `CameraSnapshotProfile` |
| `overrideActorAnchor` / `actorAnchor` | 프로필 기준과 다른 Actor/Socket을 사용할 때만 지정 |
| `overrideLookAtTarget` / `lookAtTarget` | 프로필 기준과 다른 LookAt Actor/Socket을 사용할 때만 지정 |
| `restorePreviousOnComplete` | 프로필 자동 복귀와 중복될 수 있으므로 이벤트 길이 설계에 맞춰 결정 |

---

## 사용 예시

### 코드에서 직접 실행

```csharp
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Manager;

namespace UPlayGround.CameraSystem
{
    public class CameraSnapshotSequenceStarter : MonoBehaviour
    {
        [SerializeField] private CameraSnapshotProfile _profile;

        public void Play()
        {
            CameraManager.Instance.PushCameraSnapshotSequence(_profile);
        }
    }
}
```

### MotionEvent 실행 경로

```csharp
public override void Execute(GameObject target)
{
    if (profile == null || CameraManager.Instance == null) return;

    CameraManager.Instance.PushCameraSnapshotSequence(
        profile,
        overrideActorAnchor ? actorAnchor : null,
        overrideLookAtTarget ? lookAtTarget : null);
}
```

---

## 맵 트리거 장치

`CameraSnapshotSequenceTrigger`는 씬 오브젝트에 붙여 플레이어가 트리거 영역에 진입했을 때 스냅샷 시퀀스를 재생하는 컴포넌트다.

파일:

```
Assets/02.Scripts/Camera/CameraSnapshotSequenceTrigger.cs
```

컴포넌트 메뉴:

```
UPlayGround/Camera/Camera Snapshot Sequence Trigger
```

필드:

| 필드 | 설명 |
|------|------|
| `_profile` | 재생할 `CameraSnapshotProfile` |
| `_overrideActorAnchor` | 프로필의 액터 기준 대신 트리거의 Actor/Socket 참조 사용 |
| `_actorAnchor` | 트리거에서 override할 Actor/Socket 참조 |
| `_overrideLookAtTarget` | 프로필의 LookAt 기준 대신 트리거의 Actor/Socket 참조 사용 |
| `_lookAtTarget` | 트리거에서 override할 LookAt Actor/Socket 참조 |
| `_playerTag` | 진입 판정 태그. 기본값 `Player` |
| `_triggerOnce` | 한 번만 발동 |
| `_disableColliderAfterTrigger` | 발동 후 Collider 비활성화 |
| `_onSequenceStarted` | 시퀀스 시작 시 호출할 UnityEvent |
| `_onSequenceCompleted` | 프로필 정상 완료 시 호출할 UnityEvent |

셋업:

1. 빈 GameObject를 만들고 `BoxCollider` 또는 원하는 Collider를 붙인다.
2. `Is Trigger`를 켠다. 컴포넌트의 `Awake()`/`OnValidate()`에서도 자동으로 `isTrigger = true`를 보정한다.
3. `CameraSnapshotSequenceTrigger`를 추가한다.
4. `_profile`에 재생할 프로필을 연결한다.
5. 캐릭터 기준 연출이면 프로필의 `actorAnchor`에 Actor ID와 Socket을 지정하거나, 트리거에서 override를 켠다.
6. 플레이어가 영역에 들어오면 `CameraManager.PushCameraSnapshotSequence()`가 호출된다.

---

## 에디터 도구

### Camera Snapshot 에디터

메뉴:

```
UPlayGround/Camera/Camera Snapshot 에디터
```

기능:

| 기능 | 설명 |
|------|------|
| 프로필 선택/생성 | `CameraSnapshotProfile` 선택 또는 새 에셋 생성 |
| 프로필 설정 | 입력 잠금, 락온 해제, 충돌 보정, 진입 블렌드, 재생 속도, 우선순위, 인터럽트 정책, Actor/Socket 기준 편집 |
| 현재 카메라 스냅샷 추가 | 현재 카메라 위치, 회전, FOV를 샷으로 저장 |
| 샷 순서 변경 | 목록의 `▲`, `▼` 버튼으로 순서 변경 |
| 샷 편집 | 이름, 좌표계, 위치, 회전, FOV, 지속 시간, 블렌드 커브 수정 |
| 이동 방식 편집 | 샷별 `Linear`/`OrbitAroundAnchor`, 공전 방향, 보간 중 중심 바라보기 설정 |
| 현재 카메라로 덮어쓰기 | 선택 샷을 현재 카메라 포즈로 갱신 |
| 카메라를 이 위치로 이동 | 선택 샷의 월드 포즈로 카메라 이동 |
| 시퀀스 재생 | 에디터 모드에서는 카메라 직접 이동, PlayMode에서는 `CameraManager` 모드로 재생 |
| 프리카메라 시작/종료 | PlayMode에서 `CameraManager.PushFreeCamera()`와 `PopCameraMode()` 호출 |

프리카메라 입력:

```
우클릭 드래그 회전
WASD 이동
Q/E 하강/상승
Shift 가속
Ctrl 감속
마우스 휠 FOV
```

---

## 현재 구현 상태 분석

### 구현된 부분

| 영역 | 상태 |
|------|------|
| 데이터 모델 | `CameraSnapshotProfile`, `CameraSnapshotShot`, `CameraSnapshotSpace` 구현 |
| 런타임 모드 | `CameraSnapshotSequenceMode` 구현 및 `CameraManager` 등록 |
| 모드 스택 연동 | `PushCameraSnapshotSequence()`로 이전 모드 위에 Push |
| MotionEvent 연동 | `CameraSnapshotSequenceEvent` 구현 |
| 에디터 캡처/미리보기 | `CameraSnapshotEditorWindow` 구현 |
| 맵 트리거 | `CameraSnapshotSequenceTrigger` 구현 |
| 샘플 데이터 | `Assets/10.Datas/Camera/SnapShot/CameraSnapshotProfile.asset` 존재 |

### TODO 설계 문서와 다른 점

기존 `Assets/docs/TODO/camera-dialogue-snapshot-system.md`는 대화 노드별 스냅샷을 목표로 한다. 현재 구현은 대화 노드 직접 연동이 아니라, 범용 `CameraSnapshotProfile` 시퀀스와 MotionEvent 연동이 중심이다.

| 항목 | TODO 설계 | 현재 구현 |
|------|----------|----------|
| 주 사용처 | DialogueNodeSO별 카메라 스냅샷 | MotionSet/MotionEvent 기반 시퀀스 |
| 좌표계 | World/Speaker/Listener/ConversationCenter | World/ActorRelative |
| 데이터 단위 | 노드 1개에 단일 스냅샷 또는 SO | 프로필 1개에 다중 샷 |
| 런타임 모드 | DialogueCameraMode 내부 분기 | 별도 CameraSnapshotSequenceMode |
| 에디터 | DialogueGraph/Node 중심 미리보기 | CameraSnapshotProfile 중심 캡처/재생 |

---

## 주의 사항

1. `restorePreviousModeOnFinish`와 `restorePreviousOnComplete`를 동시에 켜면 복귀 호출이 중복될 수 있다. 현재 `OnCompleteEvent()`는 현재 모드가 `CameraSnapshotSequence`인지 확인하지만, 이벤트 종료 타이밍이 프로필 종료보다 늦으면 이미 복귀된 상태일 수 있다.
2. `CameraSnapshotSequenceMode`의 인터페이스 속성 `UseCollision`은 `false`지만, 프로필 `useCollision`을 켜면 스냅샷 위치 계산 단계에서 충돌 보정이 적용된다.
3. `ActorRelative` 샷은 해석된 Actor Socket 회전까지 곱해진다. 스킬 중 캐릭터가 급회전하면 의도한 연출과 다르게 카메라도 함께 회전한다.
4. `lookAtTarget` Actor/Socket이 해석되면 샷에 저장된 회전은 무시되고, 위치에서 타겟을 바라보는 회전으로 덮인다.
5. `CameraSnapshotEditorWindow.PreviewShotInCameraManager()`는 HideAndDontSave 런타임 프로필을 만들고 `duration = 9999f`로 유지한다. PlayMode 미리보기 후 반드시 정지 또는 모드 복귀를 확인해야 한다.
6. 현재 `CameraModeEnterParams.OnComplete`는 `PushCameraSnapshotSequence()`에서 설정하지 않는다. 코드 직접 호출자가 완료 콜백을 쓰려면 별도 API 확장이 필요하다.

---

## 고도화 방안

### 1단계: 복귀 정책 정리 - 일부 완료

문제:

- 프로필의 `restorePreviousModeOnFinish`
- MotionEvent의 `restorePreviousOnComplete`
- `CameraModeEnterParams.RestorePreviousOnExit`

위 세 값의 책임이 겹친다.

개선안:

| 정책 | 책임 |
|------|------|
| 프로필 | 시퀀스 자체가 끝났을 때 자동 복귀할지 결정 |
| MotionEvent | 이벤트 강제 종료 또는 애니메이션 인터럽트 시 복귀 보장 |
| EnterParams | 모드 컨트롤러 공통 복귀 정책으로 사용할 때만 유지 |

실행 순서:

1. `CameraSnapshotSequenceMode`가 정상 종료 여부와 활성 프로필을 외부에서 확인할 수 있도록 상태 API 추가 - 완료
2. `MotionEvent` 완료 시 `StopCameraSnapshotSequence(profile)`를 사용해 같은 프로필이 활성일 때만 복귀 - 완료
3. 중복 `PopCameraMode()`가 발생해도 스택이 불필요하게 한 단계 더 빠지지 않도록 토큰 기반 모드 핸들 검토 - 남음

### 2단계: 블렌드 시작 포즈 명확화 - 완료

현재 첫 진입 포즈는 `Camera.main`과 `CameraPivot`에서 캡처한다. `applyFirstShotImmediately`가 켜져 있으면 첫 샷은 즉시 적용된다.

개선안:

- `CameraSnapshotProfile`에 `entryBlendDuration`과 `entryBlendCurve` 추가
- `applyFirstShotImmediately`는 컷 연출 전용 옵션으로 의미 축소
- `entryBlendDuration > 0`이면 첫 샷 `duration`을 건드리지 않고 별도 진입 블렌드를 먼저 수행

### 3단계: 충돌 보정 옵션 추가 - 기본 구현 완료

현재 스냅샷 시퀀스는 `UseCollision == false`로 고정이다.

개선안:

```csharp
public bool useCollision;
public float collisionRadiusOverride;
public LayerMask collisionMaskOverride;
```

현재는 프로필 단위 `useCollision`만 구현되어 있다. 샷 단위 반경/레이어 오버라이드는 아직 없다. 시네마틱 의도상 벽 관통 카메라가 필요한 경우도 있으므로 기본값은 `false`가 적합하다.

### 4단계: DialogueCameraMode와 통합

현재 TODO 문서의 대화 노드 스냅샷 설계는 아직 실제 코드에 반영되지 않았다.

권장 방향:

- `CameraSnapshotShot`을 재사용하되, 대화 전용 좌표계는 별도 enum으로 확장하지 말고 `DialogueCameraSnapshotData`로 분리
- `CameraSnapshotProfile`은 다중 샷 연출용으로 유지
- `DialogueNodeSO`에는 단일 스냅샷 또는 프로필 참조만 추가
- `DialogueCameraMode`가 자동 구도와 스냅샷 구도를 블렌드하게 구성

우선순위:

1. `DialogueNodeSO`에 선택 필드 추가
2. `CameraModeEnterParams`에 대화 스냅샷 데이터 추가
3. `DialogueCameraMode` 내부에 `AutoFollow`와 `SnapshotHold` 경로 분기
4. 기존 `CameraSnapshotEditorWindow`의 캡처 로직을 대화 노드 에디터에서 재사용

### 5단계: 에디터 사용성 개선

현재 에디터는 샷 편집에 필요한 최소 기능은 갖췄지만, 대량 제작에는 불편한 부분이 있다.

개선 후보:

| 개선 | 효과 |
|------|------|
| 샷 복제 버튼 | 같은 구도에서 FOV/시간만 바꾸는 작업 단축 |
| 선택 샷 단독 미리보기 토글 | `9999f` 런타임 프로필 대신 명시적 Preview 모드 관리 |
| 샷별 메모 필드 | 연출 의도 기록 |
| SceneView 프러스텀 기즈모 | 카메라 포즈를 씬에서 직접 확인 |
| Actor/Socket 검증 | 프로필, MotionEvent, 트리거에 지정된 Actor ID와 Socket 누락 검사 |
| Addressables 라벨/경로 규칙 | 프로필 로딩 정책 표준화 |

### 6단계: 인터럽트와 우선순위 - 기본 구현 완료

현재 `CameraModeController`는 같은 모드 재진입 시 스택을 늘리지 않고 `OnEnter()`를 다시 호출한다. 다른 시퀀스가 재생 중일 때 새 스냅샷 시퀀스가 들어오면 기존 시퀀스를 덮는 형태가 된다.

구현된 정책:

```csharp
public enum CameraSnapshotInterruptPolicy
{
    Ignore,
    Restart,
    OverrideIfHigherPriority
}
```

`CameraSnapshotProfile`에 `priority`와 `interruptPolicy`를 추가해 스킬, 피니시, 대화, 컷신의 충돌을 최소 제어할 수 있다. 큐잉은 아직 구현하지 않았다.

---

## 검증 체크리스트

- `Camera Snapshot 에디터`에서 프로필 생성, 샷 추가, 저장이 정상 동작
- `ActorRelative` 샷이 액터 위치/회전 변경 후에도 의도한 상대 구도를 유지
- `World` 샷이 액터와 무관하게 고정 위치를 유지
- PlayMode 시퀀스 재생 후 이전 카메라 모드로 복귀
- `lockCameraInput`이 켜진 시퀀스 중 Look/Zoom/LockOn 입력이 차단
- `releaseLockOnOnEnter`가 켜진 시퀀스 진입 시 락온 해제
- MotionEvent 실행 후 애니메이션 종료/인터럽트 상황에서 카메라 모드가 남지 않음
- 히트스톱 중 `useUnscaledTime` 설정에 따라 시퀀스 진행 속도가 의도대로 동작
- LookAt Actor/Socket 참조가 대상 Transform으로 정상 해석되고 추적

---

## 관련 문서

- `Assets/docs/Complete/CAMERA_SYSTEM_GUIDE.md`
- `Assets/docs/Complete/CAMERA_MODE_ARCHITECTURE_DESIGN.md`
- `Assets/docs/TODO/camera-dialogue-snapshot-system.md`
