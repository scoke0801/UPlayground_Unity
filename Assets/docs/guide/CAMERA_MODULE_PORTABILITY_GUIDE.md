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

## 인게임 카메라의 미학과 락온 원칙

락온의 목표는 선택한 적을 놓치지 않으면서 플레이어가 만든 구도를 가능한 한 유지하는 것이다.
거리별 오비탈 각도·Free Orbit·Overcome 계산은 사용하지 않는다. 대상이 화면 데드존 안에 있으면
자동 회전을 멈추고, 밖으로 나간 축만 가까운 경계까지 복귀시킨다. 플레이어 위치 추적과 충돌은 계속 수행한다.

### 조사 근거와 프로젝트 적용

아래의 확인 사실과 프로젝트의 설계 판단을 구분한다. 특정 AAA 게임이 이 프로젝트와 동일한
데드존 수치나 내부 알고리즘을 사용한다고 주장하지 않는다.

| 개발사·공식 자료에서 확인한 사실 | 프로젝트에 적용한 원칙 |
|---|---|
| God of War GDC 발표는 가까운 시점에 맞춰 전투를 재설계했고, 자동 카메라 보정의 효과를 플레이 테스트로 평가하며 비활성화 옵션을 제공했다고 설명한다. | 자동화는 플레이어 의도를 대신하지 않는다. 최초 획득 실패·해제 시 현재 시선을 유지하고, 자동 전환과 연출 보정을 분리한다. |
| PlayStation의 개발자 인터뷰는 God of War와 The Last of Us Part II의 거리·흔들림 선택을 비교하며 가독성과 타격감의 균형을 설명한다. | 모든 타격에 큰 카메라 운동을 더하지 않는다. 기본 구도의 안정성과 타격 연출을 독립적으로 조정한다. |
| Naughty Dog의 Uncharted 3 발표 소개는 추적·조준·근접·엄폐 카메라, 우선순위·전환, 별도의 가산 흔들림 계층을 명시한다. | 기존 Director/Behavior/Modifier 경계를 유지하고, 흔들림을 지속되는 yaw/pitch/offset 상태에 누적하지 않는다. |
| Spider-Man 2의 공식 접근성 안내는 카메라 추적과 흔들림 등을 조정하는 옵션을 제공한다. | 자동 운동과 감각적 효과를 별도 설정으로 두고, 일반 락온에서는 거리/FOV의 불필요한 변동을 억제한다. |
| Cinemachine Rotation Composer는 화면 데드존 안에서 회전하지 않으며 수평·수직 감쇠와 화면 제한을 따로 제공한다. | 화면 비율 기반 데드존, 축별 응답·속도·가속도, 가장자리 응답 강화를 사용한다. Cinemachine으로 시스템을 교체하지는 않는다. |

출처:

- [God of War 전투와 카메라, GDC 2019 발표 자료](https://media.gdcvault.com/gdc2019/presentations/Sheth_Mihir_EvolvingCombat.pdf) — 카메라 보조 pp.85–88, 시점과 거리 판단 pp.102–104.
- [PlayStation 개발자 인터뷰: God of War 전투 피드백](https://blog.playstation.com/2022/10/04/game-developers-explain-what-makes-god-of-war-2018s-combat-tick/)
- [Naughty Dog: The Cameras of Uncharted 3 발표 소개](https://www.naughtydog.com/blog/naughty_dog_at_gdc_2012)
- [Insomniac: Spider-Man 2 접근성 옵션](https://support.insomniac.games/hc/en-us/articles/46730041467027-What-Accessibility-options-does-Marvel-s-Spider-Man-2-feature)
- [Unity Cinemachine 3.1 Rotation Composer](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineRotationComposer.html)
- [God of War Ragnarök 락온 옵션](https://www.playstation.com/en-us/games/god-of-war-ragnarok/accessibility/) — 사망 후 화면 안 자동 전환과 화면 밖 획득을 구분.
- [God of War Ragnarök PC Patch 6](https://support.sms.playstation.com/hc/en-us/articles/31232794339213-God-of-War-Ragnarok-PC-Patch-6) — 마우스 사용 시 대상이 빠르게 튀는 문제 수정.

### 화면 유지 계약

- 화면 좌표는 왼쪽 아래 (0,0), 오른쪽 위 (1,1)이다. 기본 데드존은 가로·세로 0.35~0.65다.
- 최초 획득·전환 때도 중앙 정렬하지 않는다. 영역 밖이면 가까운 경계로만 복귀한다.
- 보정 종료 경계는 0.02만큼 안쪽에 둔다. 이 작은 히스테리시스 구간에서는 보정이 계속될 수 있다.
- 수평·수직 속도와 가속도를 독립적으로 제한한다. 화면 가장자리에서는 응답을 강화하지만 순간 회전으로 제한을 무시하지 않는다.
- 피벗 바로 위/아래의 대상은 수평 방향을 보존한다. 뒤쪽 대상은 마지막 회전 방향을 고려해 180도 부근 좌우 진동을 막는다.
- 가림 중에는 대상 선택을 잠시 유지하되 추적을 중지한다. 거리 초과·지형 가림은 유예 후 해제하며 다른 적으로 자동 전환하지 않는다.
- 지형 가림의 기본 유예는 `lockOnOcclusionGraceTime` 1.5초다. 연속 가림 시간만 누적하고 다시 보이면 초기화한다.
  거리 초과의 유예(`lockOnLostGraceTime`)와 별개이며 사망·파괴에 적용하지 않는다.
- 사망 후 자동 전환은 옵션으로 관리하고 화면 안의 가시 후보로 제한한다. 좌우 래핑과 마우스 플릭 전환은 기본 꺼짐이다.
- 게임패드는 오른쪽 스틱의 화면 2차원 방향으로 전환하며 중립 복귀가 있어야 다음 전환이 가능하다. 기존 키보드 전환 액션은 유지한다.
- `FocusPosition`을 공통 카메라 포커스로 사용한다. 제공자가 없으면 캡슐의 월드 중심, 캡슐도 없으면 루트로 폴백한다. 공격 조준점·워프 규칙은 변경하지 않는다.

### 처리 순서와 튜닝

`LockOnCameraModifier(200)` 대상 상태 → 오프셋/거리/FOV → `Follow(700)` 피벗 추적
→ `LockOnFraming(750)` 흔들림 없는 화면 판정 → `EffectRotationInject(790)` 일회성 연출 합성
→ `Collision(800)` 벽 관통 방지 → 위치/FOV 연출.

`CameraDeadZoneTracker`는 순수 포즈 계산과 축별 관성을 소유한다. 대상 선택과 수명은
`CameraLockOn`, 외부 액터 연결은 기존 `ICameraRuntimeAdapter`가 소유한다.
수직 경계 복귀는 피벗을 중심으로 카메라 위치도 이동하는 궤도에서 계산한다.
카메라 제자리 회전의 각도 차이만 사용하면 피벗과 카메라 사이의 근접 대상에서 보정 방향이
반전될 수 있다. 궤도 해 중 피치 범위 안에서 화면 오차를 줄이는 가장 가까운 보정을 사용하며,
현재 암 길이로 경계에 도달할 수 없는 경우에는 제한각까지 무조건 밀어 올리지 않는다.
데드존 판정은 이전 충돌 결과로 줄어든 암 길이를 고려한다. 현재 프레임의 충돌 안전성이 구도보다
우선하므로 갑작스러운 벽 접근 시 잠시 영역을 벗어날 수 있고, 다음 프레임부터 보정한다.
화면 가장자리 보호는 속도 제한 안에서의 복귀이며, 순간이동 대상의 화면 내 유지를 보장하지 않는다.

`CameraSettings`의 락온 탭에서 영역·응답·가속도·피치 범위·전환 정책을 조정한다.
기본 락온 거리 5.7, FOV 58은 유지한다. `enableLockOnAdaptiveFraming`,
`enableLockOnFitDistance`, `enableLockOnPairFraming`은 기본 꺼짐이다. 대형 적이 화면을
가리는 문제를 검증한 뒤 필요한 보정만 켠다. 자동 거리 피팅을 다시 켜면 데드존과 독립적으로
카메라 거리가 변하므로 반드시 벽 앞·복도에서 함께 평가한다.

### 근접 교차 보호

`enableLockOnCrossingProtection`이 켜져 있으면 플레이어와 대상 루트의 평면 거리가
2m 이하일 때 수평 데드존을 화면 0.1~0.9까지 넓힌다. 적을 지나쳐도 이 영역 안에
보이는 동안 기존 yaw를 유지하며, 수직 추적과 플레이어 위치 추적·충돌은 계속 수행한다.
화면 밖이거나 가장자리 여백을 침범한 적은 평소 최대 수평 회전 속도의 55%로 계속 추적한다.
완전한 화면 고정이나 자동 락온 해제가 아니므로 벽 때문에 카메라 암이 짧아져도 복귀할 수 있다.

3m를 넘어가면 0.45초 동안 원래 데드존과 최대 회전 속도로 돌아온다. 이 시간은 구도 규칙의
복귀 시간이며 180도 회전을 끝내는 시간을 의미하지 않는다. 기존 회전 가속도 제한도 유지한다.
진입·해제 거리를 다르게 두어 근접 경계에서 모드가 반복 전환되지 않게 한다.
한 프레임에 적을 지나치는 경우에는 이전·현재 상대 위치를 잇는 선분으로 근접 통과를 감지한다.
판정에는 보간된 포커스 대신 실제 액터 간 오프셋을 전달해 피벗 지연에 영향을 받지 않게 한다.

정반대 방향에서 15도 이내인 후방 대상은 마지막 수평 회전 방향을 유지한다.
대상 전환·추적 중단·모드 재진입 시 근접 상태와 회전 방향 기록을 함께 초기화한다.
락온 탭의 **락온 근접 교차 보호**, **락온 후방 회전 안정화**에서 모든 값을 조정할 수 있다.
기존 기본 거리·FOV와 공격 대상, 대시 이동 방향은 변경하지 않는다.

실제 전투 조정 시 작은 적·대형 적·비행 적을 각각 통과하고, 근접 위치에서 멈추기,
벽 앞 통과, 빠른 왕복 대시를 확인한다. 몸 크기에 따라 적절한 보호 반경이 달라질 수 있으므로
기본 거리값은 플레이 검증 전의 시작값이다.

근접 교차 추가 검증: 카메라 테스트와 `Assembly-CSharp-Editor`의 CLI 컴파일 오류 0개.
원본 프로젝트가 다른 Unity 인스턴스에서 열려 있어 `Logs/LockOnCrossing/ValidationProject`에
같은 카메라 DLL과 테스트 소스를 복사해 Unity 6000.3.21f1에서 EditMode 30개,
PlayMode 3개가 통과했다. 카메라 DLL의 원본·복사본 SHA-256 일치도 확인했다.
결과는 `Logs/LockOnCrossing/isolated-editmode.xml`, `isolated-playmode.xml`에 있다.
검증 프로젝트의 초기화 로그에는 복사한 의존 모듈의 Burst 리소스 부재와
`OnParticleUpdateJobScheduled` 메시지 오류가 있어 전체 환경의 오류 0을 의미하지 않는다.
이 실행은 렌더링 없는 카메라 계산·물리·Modifier 검증이며 실제 URP 전투 씬의 수동 플레이,
게임패드 조작감, 전체 Player Build는 이번 변경 이후 재검증하지 않았다.

### 리워크 검증 범위

- EditMode: 데드존 내부 정지, 네 방향 경계 복귀, 30/60/120fps 일치, 뒤쪽·극점 대상,
  히트스톱, 흔들림 상태 비누적, 화면 안 획득, 지형 가림, 사망/거리 초과/파괴, 방향 전환.
- PlayMode 자동 테스트: 실제 InGame Modifier 파이프라인에서 반복 연출 후 기본 구도 복원,
  락온 해제 시 시선 유지, 벽 충돌로 암이 줄어든 뒤 가시성 복귀와 락온 유지.
- Play Mode 수동 확인: 실제 전투에서 플레이어와 적 겹침, 복도 충돌, 비행 적 통과,
  게임패드 연속 입력, 파티 교체, 대화·필살기·킬캠 복귀, 멀미와 공격 예비 동작 가독성.
- 전투의 공격 확정 대상 저장, 투사체 유도 정책, 다중 부위 락온과 HUD 개편은 별도 후속 범위다.
  카메라 구도 리워크에 섞어 기존 GAS/MotionSet의 공격 방향 규칙을 바꾸지 않는다.

2026-10-03 검증: Unity 6000.3.21f1에서 카메라 EditMode 20개와 PlayMode 2개 통과.
StandaloneWindows64 전체 활성 씬 빌드는 `Build Finished, Result: Success`와 종료 코드 0을 확인했다.
다만 lilToon 전처리의 빈 씬 경로 예외와 기존 `CombatStrategy_Humanoid_` 데이터
5개(Bow, DoubleAxe, DualBlade, GreatSword, SwordShield)의 Missing Script 경고가 발생했다.
따라서 프로젝트 전체의 오류·누락 0 조건을 충족한 것으로 간주하지 않는다.
실제 인게임 수동 플레이·게임패드·보스전 가독성 평가는 미실시다.
로컬 검증 결과는 `Logs/CameraRework/`에 있으며 이 폴더는 Git 추적 대상이 아니다.

2026-10-04 가림 지연·근접 피치 수정 검증: 원본 프로젝트의 Unity 6000.3.21f1에서
카메라 EditMode 37개와 PlayMode 5개 통과. 근접 높이 차이 회귀 테스트 4개는 수정 전
실패를 확인했고, 수정 후 30/60/120fps에서 경계 복귀·정지와 회전 속도 제한을 확인했다.
연속 가림 1.5초, 재노출 시 타이머 초기화, 추적 중단·히트스톱, 지연 0 설정도 검증했다.
결과는 `Logs/LockOnFix/`에 있다. 이번 검증은 렌더링 없는 자동 테스트이며 실제 URP 전투의
수동 조작감과 Player Build는 재검증하지 않았다.
