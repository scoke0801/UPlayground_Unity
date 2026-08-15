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
