# Camera 모듈 이식 가이드

## 목적

`UPlayGround.Camera`는 Unity Package가 아니라 프로젝트 내부 asmdef 모듈이다.
다른 Unity 프로젝트로 옮길 때 카메라 런타임을 수정하지 않고, 호스트 프로젝트와의 연결부만
교체할 수 있도록 `ICameraRuntimeAdapter` 포트를 사용한다.

카메라의 공개 API, ScriptableObject 타입, 직렬화 어셈블리명은 기존과 동일하게 유지한다.

## 경계

```text
다른 모듈
    ↓ CameraManager 공개 API
UPlayGround.Camera
    ↓ ICameraRuntimeAdapter
호스트 프로젝트 조립 계층
    ↓
입력 · 에셋 · 설정 · 월드 대상 · 시간 제어 · 게임플레이 후처리
```

Camera 모듈 내부에서는 다음 프로젝트 전용 타입을 직접 사용하지 않는다.

- `Svc.*`
- `IWorldActor`
- `IInputService`
- `ISettingsService` / `SettingsData`
- `IHitStopService`
- `IPlayerInputSuppressible`
- `VitalOrbTrigger`

UPlayground 연결 구현은
`Assets/02.Scripts/Manager/Camera/UPlayGroundCameraRuntimeAdapter.cs`에만 둔다.

## 핵심 파일

| 파일 | 역할 |
|---|---|
| `Camera/Integration/CameraRuntimeServices.cs` | 포트, 안전 기본 구현, 정적 연결점 |
| `Camera/ICameraMotionProvider.cs` | 이동 속도·접지·지면 법선을 받는 Camera 소유 계약 |
| `Manager/Camera/UPlayGroundCameraRuntimeAdapter.cs` | UPlayground 서비스와 포트 연결 |
| `Manager/GameManager.cs` | CameraManager 등록 전에 어댑터 구성, 종료 시 리셋 |

## 호스트가 제공할 기능

`CameraRuntimeAdapterBase`를 상속하면 필요한 기능만 선택적으로 오버라이드할 수 있다.

| 영역 | 주요 기능 |
|---|---|
| 에셋 | CameraSettings, 흔들림 DB, 킬캠/대화/전투 프로필 비동기 로드 |
| 입력 | Look/Zoom 조회, LockOn 액션 등록, 플레이어 입력 억제 |
| 설정 | 감도, Y축 반전, 화면 흔들림, 조준 보정, 시퀀스 강도 |
| 월드 | 활성 플레이어, actorId 조회, 대상 생존/등급/루트, 소켓, 락온 알림 |
| 시간 | 타임스케일 요청/해제, 히트스톱 |
| 게임 훅 | 킬캠 시작 알림. UPlayground에서는 Vital Orb 생성을 연결 |

어댑터를 제공하지 않은 기능은 안전 기본값 또는 no-op으로 동작한다. 단, 실제 플레이 카메라에
필요한 설정 에셋과 입력은 호스트가 제공하거나 `CameraManager`에 직접 설정해야 한다.

## 다른 프로젝트로 옮기는 순서

1. `.meta`를 포함해 `Assets/02.Scripts/Camera/`를 복사한다.
2. 현재 asmdef 참조에 맞춰 `Core`, `Data`, `Contracts`, UniTask, Input System, URP Core를 준비한다.
3. `CameraRuntimeAdapterBase`를 상속한 호스트 어댑터를 Camera 폴더 밖 조립 계층에 작성한다.
4. CameraManager 초기화 전에 다음처럼 등록한다.

```csharp
CameraRuntimeServices.Configure(new MyProjectCameraRuntimeAdapter());
```

5. 애플리케이션 종료이나 재부팅 경로에서 정적 상태를 정리한다.

```csharp
CameraRuntimeServices.Reset();
```

6. 플레이어 이동 컴포넌트가 `ICameraMotionProvider`를 구현해 속도·접지·지면 법선을 제공하게 한다.
   구현하지 않아도 카메라는 동작하지만 지형/공중 구도와 이동 자동 리센터링은 비활성화된다.
7. CameraSettings와 카메라 프로필 에셋을 복사하고 호스트 에셋 로더의 키를 연결한다.
8. 락온, 전투 카메라, 지형/공중 탐색 구도, 킬캠, 대화 카메라, 스냅샷, 프리카메라를 각각 확인한다.

## 확장 규칙

1. Camera 내부에서 `Svc.*` 또는 구체 게임 매니저를 새로 호출하지 않는다.
2. 새 외부 기능이 필요하면 `ICameraRuntimeAdapter`에 카메라 관점의 최소 포트를 추가한다.
3. 퀘스트 보상, 오브 생성 등 게임 규칙은 Camera에서 실행하지 않고 알림 훅으로 호스트에 위임한다.
4. `GameActor` 같은 구체 타입 대신 `CameraTargetInfo`와 `Transform`을 사용한다.
5. 기존 카메라 SO와 직렬화 타입의 어셈블리를 옮기지 않는다.
6. 호스트 어댑터는 Camera asmdef 밖에 둔다.

## UPlayground 검증 항목

- Camera 폴더의 `Svc.*`, `IWorldActor`, `SettingsData`, `VitalOrbTrigger` 참조 0건
- `UPlayGround.Camera`, `UPlayGround.Actor`, `UPlayGround.UI`, `Assembly-CSharp` 컴파일 오류 0
- Play Mode 서비스 미등록 경고와 예외 0
- 락온 대상 선택/전환/해제
- 마우스·게임패드 Look/Zoom
- 수동 Look 이후 자동 리센터링 유예, 오르막·내리막·상승·낙하 구도
- 프리카메라 진입/복귀 시 플레이어 입력 상태 복원
- 전투 흔들림·펀치·타임스케일
- 킬캠과 Vital Orb 훅
- 대화 및 스냅샷 시퀀스

## 인게임 락온과 벽 충돌

락온은 플레이어와 대상의 위치 관계로 각도를 계산한다. 벽 충돌은 이 각도를 유지하면서
카메라 암 길이만 줄인다. 충돌 결과를 다음 락온 회전의 기준 위치·거리로 사용하지 않는다.

처리 순서는 다음과 같다.

1. Follow(700)는 플레이어의 추적 위치와 지형 보정 전 기준 위치를 계산한다.
2. LockOnFraming(730)은 기준 위치에서 대상의 수평 방향을 계산한다. 피치는 기본 락온 거리와
   기준 내려다보기 각도로 만든 가상 위치에서 높이차를 계산한다. 실제 충돌 거리와 보정된 피벗은
   읽지 않는다. 수평·수직에 각각 SmoothDampAngle과 최대 회전 속도를 적용한다.
3. LockOnFitDistance(740)는 필요한 경우 높은 대상의 피벗 상승·거리 확장을 적용한다.
   회전은 변경하지 않는다. 플레이어 최소 투영 배율과 기존 프레이밍 예산을 지킨다.
4. EffectRotationInject(790)은 연출 회전을 프레임의 포즈에만 합성한다.
5. Collision(800)은 현재 방향으로 SphereCast하고 확보한 공간 안에 카메라를 배치한다.
   벽에서 부족한 거리는 즉시 줄이며 피벗 추적 속도나 yaw/pitch를 변경하지 않는다.
6. EffectPositionFov(850)는 연출 이동 경로를 검사한 뒤 위치와 FOV를 적용한다.
7. InGameCameraBehavior는 최종 안전 위치에서 락온 가시성을 갱신한다. 연출 회전은 대상 선택
   기준에서 제외하고, 가시성 검사 뒤 시선을 다시 회전시키지 않는다.

대상이 피벗 바로 위·아래에 있으면 기존 수평 방향을 유지한다.
피치는 -89~89도 안으로 제한하고 roll은 락온 계산에 사용하지 않는다.
대상 전환·모드 변경·입력 잠금은 기존 회전 관성을 초기화한다.
자유 시점 입력, 대상 획득·전환·가림 해제, 대화·필살기·킬캠의 모드 계약은 유지한다.

### 충돌 거리와 복귀

- 현재 프레임의 안전 거리보다 긴 카메라 암은 즉시 접는다.
- 바깥 복귀는 추가 공간이 거리 데드존보다 넓고 유지 시간 동안 계속 확보된 뒤 시작한다.
- 복귀에는 단일 SmoothDamp만 사용한다. 미래 위치·공전 예측과 피벗 이동 제한은 사용하지 않는다.
- Cast는 요청 거리와 접촉 여유까지 검사하고 여유를 빼서 최초 접촉의 거리 점프를 줄인다.
- SphereCast의 시작 겹침과 최종 위치 겹침은 Overlap 검사로 보완한다.
- 어깨·연출·고저차 피벗 이동도 같은 지형 레이어로 경로를 검사한다.
- 양 벽 사이에 피벗 검사 구를 넣을 공간이 없으면 반복 분리의 임의 좌우 이동을 채택하지 않는다.
- 쿼리 버퍼가 포화되면 가까운 장애물이 누락될 수 있으므로 안전 거리를 0으로 제한한다.

CameraSettings의 충돌 탭에서 반경·벽 여유·복귀 시간·유지 시간·복귀 거리 데드존을 조정한다.
락온 탭에서는 축별 응답 시간·최대 회전 속도·피치 범위·기준 피치·수평 특이점 반경을 조정한다.
충돌 예측, 화면 데드존 추적, 근접 교차 모드, 충돌 후 시선 보정과 관련 설정은 제거했다.

몸 근접 시 플레이어 렌더링은 기존 ActorCameraProximityDither가 담당한다.
Camera 모듈은 Actor 구현을 직접 참조하지 않는다.

### 구현 근거

[Unity Cinemachine Third Person Follow 공식 소스](https://github.com/Unity-Technologies/com.unity.cinemachine/blob/main/com.unity.cinemachine/Runtime/Components/CinemachineThirdPersonFollow.cs)는
충돌을 카메라 위치 보정에 적용하고 회전은 추적 대상의 회전으로 유지한다.
본 프로젝트는 이 책임 분리를 기존 Modifier 구조 안에 적용했다.

[Unity Cinemachine 3.1 Deoccluder](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineDeoccluder.html)의
Pull Camera Forward와 Smoothing Time을 참고해 현재 암 방향의 수축과 가까운 거리 유지·복귀를 분리했다.
Cinemachine 런타임으로 교체하거나 특정 상용 게임의 내부 알고리즘을 사용했다고 주장하지 않는다.

### 수명과 연출 입력 잠금

- CameraManager는 활성 모드와 무관하게 대상 생존·거리 유지 조건을 갱신한다.
- 연출 중 사망 대상의 자동 전환은 보류한다. LiveTarget은 사망 즉시 게임플레이 대상에서 제외한다.
- 실제 가시성은 현재 인게임 포즈에서 판정한다. 가림 유예 동안 추적을 유지하고,
  연속 가림이 유예 시간을 넘으면 해제한다. 불확실한 쿼리는 유지 타이머를 진행하거나 초기화하지 않는다.
- 입력 잠금은 소유자가 해제한다. 수동 잠금 해제가 다른 연출의 잠금을 제거하지 않는다.
- 대상 선택과 가시성은 CameraViewportProjection으로 안정된 포즈를 투영한다.
  플레이어/몬스터 공격 방향과 GAS/MotionSet 데이터는 이 계산에 포함하지 않는다.

### 검증 상태

카메라 EditMode·PlayMode 테스트, 재생 Fixture와 자동 스모크 실행 코드를 제거했다.
기존 자동 테스트의 통과 이력은 현재 구현의 완료 근거로 사용하지 않는다.

현재 변경은 Camera 및 Assembly-CSharp-Editor CLI 컴파일 오류 0개로 확인했다.
Unity의 실제 URP Play Mode와 Player Build는 별도 확인이 필요하다.
수동 확인 위치와 조작은 Tools/CameraTestMap/README.txt를 따른다.
