# MotionSet 독립 패키지 및 GameActor 계층 분리 설계

> 작성일: 2026-07-27  
> 상태: 외부 프로젝트 재사용 목표 반영 / 구현 전  
> 범위: MotionSet 데이터·재생 커널·이벤트 실행·범용 편집기를 `GameActor`와 UPlayground 프로젝트 구현에서 분리하여, 다른 Unity 프로젝트에 UPM 패키지로 설치할 수 있게 한다. 기존 Actor API와 에셋은 무손실로 유지한다.  
> 선행 문서: `Assets/docs/Complete/MOTIONSET_ASMDEF_PACKAGE_REFACTOR_PLAN.md`, `Assets/docs/Complete/ASMDEF_MODULARIZATION_PLAN.md`, `Assets/docs/onboarding/ASMDEF_MODULARIZATION_ONBOARDING.html`

---

## 1. 결론

분리는 가능하다. 최종 산출물은 프로젝트 내부 asmdef가 아니라 `Packages/com.uplayground.motionset` 독립 UPM 패키지여야 한다.

독립의 기준은 다음과 같다.

- 새 Unity 6 프로젝트에 Animancer를 설치한 뒤 MotionSet 패키지만 추가해 컴파일할 수 있다.
- 패키지 안에서 `UPlayGround.Data`, `UPlayGround.Actor`, `GameActor`, `GameplayTag`, `WeaponType`, Manager, Camera, UI를 참조하지 않는다.
- UPlayground의 `Assets/10.Datas`, 프리팹, GUID, 서비스 로케이터가 없어도 패키지 샘플을 실행할 수 있다.
- KCC와 Animancer 같은 외부 라이브러리 의존은 허용한다.

다만 `ActorAnimator`를 통째로 패키지로 옮기거나 새 공통 베이스 클래스로 바꾸는 방식은 권장하지 않는다.

권장 구조는 다음과 같다.

1. MotionSet 데이터와 순수 시간축 계산을 패키지의 `UPlayGround.MotionSet.Core`로 이동한다.
2. Animancer 재생 상태를 소유하는 일반 C# 재생 커널을 패키지의 `UPlayGround.MotionSet.Animancer`로 추출한다.
3. `ActorAnimator`는 상속 계층의 공통 베이스가 아니라 기존 공개 API를 보존하는 Actor 어댑터/퍼사드로 남긴다.
4. 비-GameActor 소비자는 별도 `MotionSetPlayer` 호스트 컴포넌트로 같은 재생 커널을 사용한다.
5. Actor 전투·카메라·워프 이벤트와 의미 슬롯/무기 해석은 `UPlayGround.Actor`에 남긴다.
6. 범용 Inspector/Timeline/Event 카탈로그를 `UPlayGround.MotionSet.Editor`로 분리하고, UPlayground 전투 편집 기능은 프로젝트 확장으로 연결한다.

핵심 방향은 **상속 분리보다 합성(composition)** 이다. 이 방식은 Player 프리팹의 MonoBehaviour 타입과 직렬화 필드를 유지하면서 재생 로직만 재사용할 수 있다.

---

## 2. 현행 상태

### 2.1 이미 분리된 부분

현재 `UPlayGround.Data`에는 다음 범용 요소가 있다.

- `Motion`, `MotionLayer`, `MotionSet`
- `MotionSetAsset`
- `MotionEventBase`와 이벤트 시간/재진입/평가 단계 타입
- `MotionTimelineResolver`
- Section, Marker, Curve, Sync, TimeStretch 데이터

`UPlayGround.Actor`에는 다음 요소가 있다.

- `ActorAnimator`: Animancer 재생, Section, Loop/Freeze, 이벤트 시계, 레이어, 루트모션, 스냅샷을 모두 소유
- `MotionEventExecutor`: 이벤트 발화와 재진입 정책 처리
- `PlayerActorAnimator`: 무기별 의미 슬롯 해석
- 구체 MotionEvent 타입
- KCC MotionWarp 디버그 표시

따라서 현재 구조는 데이터만 하위 모듈에 있고, 재생 프레임워크는 Actor 모듈에 남은 **부분 분리 상태**다.

### 2.2 현재의 실제 결합

| 결합 | 현재 위치 | 분리 조치 |
|---|---|---|
| `ActorAnimator.Init(GameActor)` | `ActorAnimator` | 재생 커널에는 시간 공급 계약만 전달 |
| `_actor.DeltaTime` | Timeline/InfiniteLoop 갱신 | `IMotionTimeSource.DeltaTime`으로 치환 |
| `GameplayTag → MotionSetAsset` | `ActorAnimator` | Actor 어댑터의 의미 슬롯 해석 책임으로 유지 |
| `WeaponType → ActorAnimationMotionSet` | `PlayerActorAnimator` | Player Actor 어댑터에 유지 |
| `LoopEvent` 구체 타입 판정 | `ActorAnimator.ProcessLoopEvents` | Core 계약 `IMotionTimelineControlEvent`로 역전 |
| 부모 `GameActor` 자동 탐색 | `MotionEventExecutor.TargetObject` | `IMotionEventTargetProvider` 또는 명시적 대상 주입 |
| KCC MotionWarp 디버그 | `MotionSetEventDebugOverlay` | Actor 전용 디버그 확장으로 분리 |
| `RootMotionStepBuffer` 소비 | `ActorMovementController` | 재생 커널은 델타만 제공하고 KCC 소비는 Actor가 유지 |
| 전투/카메라/VFX 구체 이벤트 | `GameActor/Animation/MotionEvents` | Actor 모듈에 유지 |

### 2.3 기존 2026-07-16 계획과 달라진 전제

`MOTIONSET_ASMDEF_PACKAGE_REFACTOR_PLAN.md`는 당시 다음 전제를 사용했다.

- 자체 런타임 asmdef가 사실상 Core 하나뿐
- KCC와 SerializedCollections에 asmdef가 없음
- 구체 MotionEvent가 `Assembly-CSharp`에 있음

현재는 다음 상태다.

- `Data`, `Contracts`, `Actor`, `Camera`, `UI`, Ability 모듈이 이미 분리됨
- KCC와 SerializedCollections asmdef 참조가 존재함
- 구체 MotionEvent는 `UPlayGround.Actor`로 이동했고 기존 `Assembly-CSharp` 매핑용 `[MovedFrom]`을 보유함

따라서 기존 문서는 아이디어와 직렬화 분석은 참고하되, 실행 순서와 source assembly 가정은 그대로 사용하면 안 된다.

---

## 3. 목표와 비목표

### 3.1 목표

- MotionSet 패키지 폴더만 다른 Unity 프로젝트로 옮겨 설치할 수 있다.
- MotionSet을 `GameActor` 없이 저작하고 재생할 수 있다.
- `UPlayGround.MotionSet.Core`는 Actor, Data, Contracts, Camera, UI, Manager를 참조하지 않는다.
- `UPlayGround.MotionSet.Animancer`는 Animancer와 Core만으로 컴파일할 수 있다.
- `UPlayGround.MotionSet.Editor`는 UPlayground Actor/전투 타입 없이 MotionSet을 편집할 수 있다.
- 기존 `GameActor.Animator`, `ActorAnimator.PlayMotion(...)`, Section/Loop API를 유지한다.
- 플레이어의 무기별 Motion 해석과 Ability의 `MotionReferenceSO` 경로를 유지한다.
- 기존 MotionSetAsset의 MonoScript GUID와 모든 managed reference/VFX 참조를 보존한다.
- 현재 진행 중인 KCC 루트모션 버퍼 작업과 충돌하지 않게 단계적으로 이행한다.

### 3.2 비목표

- MotionSet을 Animancer 비의존 범용 애니메이션 엔진으로 재작성하지 않는다.
- Animancer 또는 KCC 소스 자체를 MotionSet 패키지에 복제하거나 재배포하지 않는다.
- MotionEvent 전투 로직을 Ability Effect로 옮기지 않는다.
- 모든 구체 MotionEvent를 Core로 이동하지 않는다.
- 첫 단계에서 UPlayground 전투 확장이 포함된 `MotionSetWindow` 전체를 패키지화하지 않는다.
- `ActorAnimator` 공개 API를 한 번에 제거하거나 모든 호출부를 교체하지 않는다.
- 기존 에셋을 일괄 재직렬화하지 않는다.

---

## 4. 목표 패키지 및 asmdef 구조

```text
Packages/com.uplayground.motionset/
├── package.json
├── README.md
├── CHANGELOG.md
├── LICENSE.md
├── Runtime/
│   ├── Core/
│   │   ├── UPlayGround.MotionSet.Core.asmdef
│   │   ├── Data/
│   │   ├── Events/
│   │   ├── Timeline/
│   │   └── Diagnostics/
│   └── Animancer/
│       ├── UPlayGround.MotionSet.Animancer.asmdef
│       ├── MotionSetPlaybackController.cs
│       ├── MotionSetPlayer.cs
│       └── RootMotion/
├── Editor/
│   ├── UPlayGround.MotionSet.Editor.asmdef
│   ├── Inspector/
│   ├── Timeline/
│   └── Extensibility/
├── Tests/
│   ├── Runtime/
│   └── Editor/
├── Samples~/
│   └── BasicPlayback/
└── Documentation~/
    ├── installation.md
    ├── custom-events.md
    └── migration-from-uplayground.md

Assets/02.Scripts/
├── Data/Actor/Animation/
│   ├── ActorAnimationMotionSet.cs
│   ├── PlayerActorAnimationMotionSet.cs
│   └── MotionReferenceSO.cs
├── GameActor/Animation/
│   ├── ActorAnimator.cs
│   ├── PlayerActorAnimator.cs
│   ├── MotionEvents/
│   └── ActorMotionSetDebugOverlay.cs
└── Editor/
    └── MotionSetExtensions/
        ├── Combat/
        ├── MotionWarp/
        └── SlashVFX/
```

의존 방향:

```text
MotionSet.Core ──X──→ UPlayGround.Core / Data / Actor / Contracts / Camera / UI / Manager
MotionSet.Animancer ─────→ MotionSet.Core + Kybernetik.Animancer
MotionSet.Animancer ──X──→ UPlayGround.Data / Actor / Contracts / Camera / UI / Manager
MotionSet.Editor ─────────→ MotionSet.Core + MotionSet.Animancer
MotionSet.Editor ─────X───→ UPlayGround.Data / Actor / 전투 편집기
Data ────────────────→ MotionSet.Core
Ability.UPlayGround ─→ Data + MotionSet.Core
Actor ───────────────→ Data + MotionSet.Core + MotionSet.Animancer
```

패키지 Core는 `UPlayGround.Core`에도 의존하지 않는다. 이름이 같은 프로젝트 Core가 없는 외부 프로젝트에서도 독립적으로 컴파일되어야 하기 때문이다.

### 4.1 왜 Runtime 하나가 아니라 Core/Animancer 둘인가

`MotionSetAsset`, `MotionTimelineResolver`, MotionEvent 직렬화 모델은 Animancer를 알 필요가 없다. 반면 실제 클립 레이어 재생은 Animancer에 의존한다.

둘을 나누면 다음 이점이 있다.

- Data와 Ability 어댑터가 Animancer 어셈블리를 불필요하게 참조하지 않는다.
- 시간축/Section/이벤트 범위 계산을 가벼운 EditMode 테스트에서 검증할 수 있다.
- 향후 패키지화 시 데이터 코어와 Animancer 어댑터의 배포 조건을 분리할 수 있다.

### 4.2 외부 라이브러리 정책

#### Animancer

- 패키지 식별자: `com.kybernetik.animancer`
- 현재 검증 버전: `8.3.1`
- asmdef: `Kybernetik.Animancer`
- MotionSet 재생 백엔드의 허용된 필수 의존이다.
- 외부 프로젝트 설치 문서에서 Animancer Pro의 별도 라이선스와 선설치를 명시한다.
- Animancer 소스나 DLL을 MotionSet 저장소에 포함하지 않는다.

Animancer가 사설 레지스트리나 git URL로 해석되지 않는 설치 환경에서는 `package.json` 의존 선언만으로 자동 설치되지 않을 수 있다. 따라서 독립 설치 검증은 “Animancer 설치 완료 → MotionSet 설치” 순서로 수행한다.

#### KCC

- 현재 asmdef: `KinematicCharacterController`
- KCC 의존 자체는 허용한다.
- 그러나 MotionSet의 시간축·이벤트·Animancer 재생에는 KCC가 필요하지 않으므로 기본 패키지의 필수 의존으로 만들지 않는다.
- KCC root motion 소비는 UPlayground Actor 어댑터에 유지한다.
- 다른 프로젝트에서도 쓸 범용 KCC 드라이버가 실제로 필요해지면 별도 companion package `com.uplayground.motionset.kcc`로 제공한다.

이 구분은 KCC 회피가 목적이 아니라, 설치하지 않는 소비자에게 불필요한 컴파일 의존을 강제하지 않기 위한 것이다.

### 4.3 독립 패키지 금지 의존

패키지 전체에서 다음 문자열/타입 참조가 0이어야 한다.

- `GameActor`, `PlayerActor`, `MonsterActor`
- `ActorAnimator`, `ActorMovementController`
- `UPlayGround.Data`, `UPlayGround.Manager`, `UPlayGround.Contracts`
- `GameplayTag`, `WeaponType`, `CharacterActorType`
- `Svc`, `ActorSvc`, `UISvc`, 구체 Manager singleton
- UPlayground Camera/Combat/Ability 구현
- `Assets/10.Datas`, `Assets/03.Prefabs` 등 프로젝트 고정 경로

namespace를 `UPlayGround.Animation`으로 유지하는 것은 허용한다. namespace 이름이 아니라 어셈블리·소스 의존 방향이 독립성 판정 기준이다.

---

## 5. 책임 분해

### 5.1 `UPlayGround.MotionSet.Core`

이동 대상:

- `Data/Actor/Animation/Motion.cs`
- `Data/Actor/Animation/MotionAdvancedTypes.cs`
- `Data/Actor/Animation/MotionSetAsset.cs`
- `Data/Actor/Animation/MotionTimelineResolver.cs`
- `Data/Event/Animation/MotionEvent.cs`
- 결합을 제거한 `MotionEventExecutor.cs`

신규 계약:

```csharp
namespace UPlayGround.Animation
{
    public interface IMotionEventTargetProvider
    {
        GameObject MotionEventTarget { get; }
    }

    public interface IMotionTimeSource
    {
        float DeltaTime { get; }
    }

    public interface IMotionTimelineControlEvent
    {
        MotionTimelineControlMode Mode { get; }
        int LoopCount { get; }
        float FreezeDuration { get; }
    }
}
```

이름은 구현 시 조정할 수 있지만 책임은 유지한다.

- Core는 대상이 `GameActor`인지 알지 않는다.
- Core는 Loop 제어 이벤트의 구체 클래스가 무엇인지 알지 않는다.
- Core는 로컬 타임스케일의 소유자가 누구인지 알지 않는다.

### 5.2 `UPlayGround.MotionSet.Animancer`

#### `MotionSetPlaybackController`

MonoBehaviour 상속이 없는 재생 커널을 우선 권장한다.

소유 책임:

- 현재 MotionSet/Asset/AnimancerState
- 전역 시간과 포즈 기반 시간
- Section 이동과 종료 정책
- Motion 전환
- 병렬 레이어와 동시성 정책
- Loop/Freeze/InfiniteLoop 상태
- Seek, Capture/Restore, 재생 종료 사유
- EventExecutor 호출
- 범용 루트모션 델타 누적

주입받는 책임:

- AnimancerComponent
- MotionEventExecutor
- IMotionTimeSource
- AvatarMask/레이어 설정
- 로그/진단 sink(선택)

#### `MotionSetPlayer`

비-GameActor 오브젝트용 일반 호스트 MonoBehaviour다.

- `MotionSetAsset` 직접 재생 API 제공
- 기본 Unity `Time.deltaTime` 시간 공급
- 명시적 Event target 또는 `IMotionEventTargetProvider` 탐색
- Update/LateUpdate/OnAnimatorMove를 재생 커널에 전달
- GameplayTag, WeaponType, GameActor를 참조하지 않음

### 5.3 `UPlayGround.Actor`

#### `ActorAnimator`

기존 컴포넌트 타입과 API를 보존하는 퍼사드다.

유지 책임:

- `ActorAnimationMotionSet` 의미 슬롯 해석
- `GameActor.LocalTimeScale` 기반 시간 공급
- 기존 `PlayMotion(GameplayTag)` API
- 기존 디버그 스냅샷에서 Actor 의미 정보 결합
- KCC가 사용하는 루트모션 소비 API 호환
- 하위 `PlayerActorAnimator` 확장점

위임 책임:

- 직접 보유하던 MotionSet 재생 상태와 시간축 처리는 `MotionSetPlaybackController`에 위임

`ActorAnimator : MotionSetPlayer` 상속은 피한다. 기존 프리팹의 컴포넌트 타입은 유지되더라도 Unity 메시지 호출, 직렬화 필드 위치, Player 하위 클래스 동작이 한 번에 변해 회귀 범위가 커진다.

#### `PlayerActorAnimator`

다음은 그대로 Actor 계층에 남긴다.

- `WeaponType`에 따른 `PlayerActorAnimationMotionSet` 선택
- PlayerEquipment/PlayerCombat 캐시
- 플레이어 전용 AnimationEvent 연결

#### 구체 MotionEvent

전투, Camera, Manager, KCC, Player/Monster 타입을 사용하는 이벤트는 모두 Actor에 남긴다.

`LoopEvent`도 첫 분리에서는 Actor에 남기고 `IMotionTimelineControlEvent`를 구현한다. 이렇게 하면 기존 SerializeReference 타입의 어셈블리를 다시 바꾸지 않아도 된다.

### 5.4 독립 패키지 공개 계약

패키지는 외부 프로젝트가 UPlayground 코드 없이 사용할 수 있는 최소 공개 API를 제공한다.

```text
MotionSetAsset
MotionSet / Motion / MotionLayer
MotionSetPlayer
MotionSetPlaybackController
MotionEventBase
MotionEventExecutor
MotionPlaybackRequest / MotionSetEndReason
IMotionEventTargetProvider
IMotionTimeSource
IMotionTimelineControlEvent
IMotionEventCatalogProvider (Editor)
IMotionEventPresetProvider (Editor)
IMotionSetEditorExtension (Editor)
```

외부 프로젝트 기본 사용 흐름:

```csharp
MotionSetPlayer player = GetComponent<MotionSetPlayer>();
player.Play(motionSetAsset);
player.MotionEnded += OnMotionEnded;
```

GameplayTag, WeaponType 같은 의미 슬롯 해석은 패키지 공개 API가 아니다. 소비 프로젝트가 자기 도메인의 키를 `MotionSetAsset`으로 해석한 뒤 직접 재생한다.

### 5.5 기본 제공 이벤트와 샘플 이벤트

독립 패키지가 UPlayground 구체 이벤트 없이도 동작을 증명할 수 있어야 한다.

패키지 Runtime에 둘 수 있는 범용 이벤트:

- Signal 발행 이벤트
- Renderer 표시/숨김 이벤트
- 범용 callback 식별자 이벤트
- Timeline control 이벤트

다만 기존 UPlayground의 `LoopEvent`, `HideTargetEvent`, `CustomCallbackEvent`를 다시 이동하지 않는다. 동일 목적의 새 패키지 타입은 충돌하지 않는 새 클래스명으로 만들고, UPlayground 기존 타입은 패키지 계약을 구현하는 호환 어댑터로 유지한다.

`Samples~/BasicPlayback`은 다음을 포함한다.

- GameActor 없는 프리팹
- AnimancerComponent + MotionSetPlayer
- 두 개 이상의 AnimationClip을 잇는 MotionSetAsset
- 사용자 정의 샘플 MotionEvent
- Section, Loop, PostEvaluation 발화 예제
- KCC 없이 재생되는 기본 Scene

KCC 연동 샘플은 companion package가 생긴 경우에만 별도로 제공한다.

### 5.6 `package.json` 원칙

예상 manifest:

```json
{
  "name": "com.uplayground.motionset",
  "displayName": "UPlayGround MotionSet",
  "version": "0.1.0",
  "unity": "6000.0",
  "description": "Animancer-based motion timeline, event and authoring framework.",
  "dependencies": {
    "com.kybernetik.animancer": "8.3.1"
  },
  "samples": [
    {
      "displayName": "Basic Playback",
      "description": "GameActor-independent MotionSet playback sample.",
      "path": "Samples~/BasicPlayback"
    }
  ]
}
```

Animancer가 Unity 공식 registry에서 자동 해석되지 않는 배포 환경에서는 설치 문서에 선행 설치 방법을 제공하고, 실제 배포 manifest는 사용하는 registry/git 배포 방식에 맞춰 확정한다.

---

## 6. 이벤트 실행 경계

### 6.1 첫 이행에서는 `Execute(GameObject)` 유지

`MotionEventBase.Execute(GameObject)`를 즉시 Context 구조체로 전환하면 구체 이벤트 전체와 편집/복사 도구가 동시에 변경된다. GameActor 분리에 필수적인 변화가 아니므로 첫 이행에서는 유지한다.

Executor 대상 결정만 다음 순서로 바꾼다.

1. `SetTargetObject`로 명시적으로 주입된 대상
2. 부모의 `IMotionEventTargetProvider.MotionEventTarget`
3. Executor 자신의 GameObject

`GameActor`는 다음 계약만 구현한다.

```csharp
GameObject IMotionEventTargetProvider.MotionEventTarget => gameObject;
```

### 6.2 후속 Context 확장

필요할 때만 다음과 같은 비파괴 오버로드를 추가한다.

```csharp
public readonly struct MotionEventContext
{
    public GameObject Target { get; }
    public float GlobalTime { get; }
    public float LocalTime { get; }
    public float SubFrameFraction { get; }
}
```

기존 `Execute(GameObject)`를 기본 어댑터로 남겨 한 번에 모든 이벤트를 수정하지 않는다.

### 6.3 디버그 오버레이

현재 `MotionSetEventDebugOverlay`는 범용 이벤트 정보와 `ActorMovementController.MotionWarp` 상태를 한 클래스에서 표시한다.

다음처럼 나눈다.

- Core/Animancer: 이벤트 시각, Active/Recent 이벤트만 제공하는 진단 snapshot
- Actor: KCC MotionWarp 상태를 추가하는 `ActorMotionSetDebugOverlay`

Core가 `UPlayGround.MovementController`를 참조하면 분리 목적이 깨진다.

---

## 7. 루프 이벤트 분리

현재 재생기가 `LoopEvent`와 `LoopEventMode`를 직접 판정한다. `LoopEvent`는 이미 `Assembly-CSharp → UPlayGround.Actor` 이동 이력이 있으므로 다시 이동하면 직렬화 매핑이 복잡해진다.

권장 방식:

```csharp
public sealed class LoopEvent :
    MotionEventBase,
    IMotionTimelineControlEvent
{
    MotionTimelineControlMode IMotionTimelineControlEvent.Mode => ...;
    int IMotionTimelineControlEvent.LoopCount => loopCount;
    float IMotionTimelineControlEvent.FreezeDuration => freezeDuration;
}
```

재생 커널은 다음처럼 계약만 판정한다.

```csharp
if (motionEvent is not IMotionTimelineControlEvent controlEvent)
    continue;
```

이 방식의 장점:

- `LoopEvent`의 현재 assembly 이름을 유지
- 기존 `[MovedFrom(sourceAssembly: "Assembly-CSharp")]` 유지
- Core가 Actor를 참조하지 않음
- 다른 프로젝트가 별도 제어 이벤트를 구현 가능

---

## 8. Data와 Ability 경계

다음 타입은 UPlayground 프로젝트 데이터이므로 `UPlayGround.Data`에 남긴다.

- `ActorAnimationMotionSet`
- `ActorAnimationStringKeyMotionSet`
- `PlayerActorAnimationMotionSet`
- `MotionReferenceSO`
- `WeaponMotionMappingConfig`

이들은 `MotionSetAsset`을 참조하므로 `UPlayGround.Data.asmdef`에 `UPlayGround.MotionSet.Core` 참조를 추가한다.

`UPlayGround.Ability.UPlayGround`는 공개 API에서 `MotionSetAsset`을 직접 사용하므로 `UPlayGround.MotionSet.Core`를 명시적으로 참조한다.

Ability의 단일 실행 경로는 바꾸지 않는다.

```text
AbilitySetSO
→ GameplayAbilitySO.Variant
→ UPlayGroundMotionAbilityPayloadSO
→ AbilityAttackInfo.baseInfo.motionRef
→ MotionReferenceSO.Resolve(WeaponType)
→ MotionSetAsset
→ ActorAnimator 어댑터
→ MotionSetPlaybackController
```

---

## 9. 에디터 분리

### 9.1 첫 런타임 분리에서 이동하지 않을 대상

다음은 Actor/전투/프리뷰 결합이 크므로 초기 런타임 분리와 함께 옮기지 않는다.

- `MotionSetWindow`와 모든 partial
- CombatOverlay
- WarpBake/WarpTarget
- SlashVFXSceneTune
- CaptureBridge
- LocoMotion/WeaponMotion 설정 창
- 전투 프레임/히트박스 도구

### 9.2 독립 패키지 Editor 필수 범위

최종 독립 패키지 완료 전까지 다음 범용 편집 기능을 `UPlayGround.MotionSet.Editor`로 옮긴다.

- `MotionSetEditor`
- `MotionSetDrawer`
- `MotionSetAssetEditor`
- `MotionEventOffsetFieldUtil`
- `MotionEventSerializationUtility`
- 범용 TimelineView/Validation 부분

그러나 현재 `MotionEventAddPopup`과 `MotionEventStyle`은 Actor 구체 이벤트 타입을 하드코딩한다. 그대로는 Core Editor로 이동할 수 없다.

필요한 역전:

- `MotionEventDescriptorAttribute`: 표시 이름, 카테고리, 색, 아이콘, 검색 별칭
- `TypeCache.GetTypesDerivedFrom<MotionEventBase>()`: 구체 이벤트 발견
- `IMotionEventPresetProvider`: Actor 전용 공격/Slash 프리셋 공급
- `IMotionSetEditorExtension`: Combat/Warp/VFX 확장 탭 공급

런타임 분리 성공 후 별도 Phase로 진행하지만, 외부 프로젝트용 패키지 완료 선언에는 이 Phase가 필수다. 외부 프로젝트에서 코드로만 에셋을 만들 수 있는 상태는 런타임 코어 분리일 뿐, 저작 도구까지 포함한 MotionSet 패키지 완료로 보지 않는다.

---

## 10. 직렬화 안전성

### 10.1 이동 가능한 타입

| 타입 | 이동 안전 근거 | 조치 |
|---|---|---|
| `MotionEventBase` | 추상 타입이라 managed reference 구체 타입으로 저장되지 않음 | namespace 유지 |
| `Motion`, `MotionSet`, Section/Marker/Curve | 값 직렬화 | namespace/필드명 유지 |
| `MotionSetAsset` | MonoScript GUID 기반 | `.meta` 동반 이동 |
| `MotionTimelineResolver` | 비직렬화 정적 로직 | 단순 이동 |
| `MotionEventExecutor` | MonoBehaviour GUID 기반 | `.meta` 동반 이동 |

### 10.2 이동하지 않을 타입

모든 구체 MotionEvent는 첫 분리에서 `UPlayGround.Actor`에 남긴다.

이유:

- YAML managed reference에 현재 `asm: UPlayGround.Actor`가 저장됨
- 다수 타입이 기존 `Assembly-CSharp` 이동용 `[MovedFrom]`을 이미 보유
- 재이동의 이득보다 이벤트/VFX 유실 위험이 큼

### 10.3 금지 사항

- 컴파일 오류 또는 `Unknown managed type`이 있는 상태에서 MotionSetAsset 저장 금지
- `.meta` 없이 MonoBehaviour/ScriptableObject 파일 이동 금지
- 타입 이동과 에셋 일괄 재직렬화를 같은 단계에서 수행 금지
- `MovedFrom`을 검증 전에 제거하거나 덮어쓰기 금지
- 검증 중 자동 변경된 `Assets/10.Datas`를 확인 없이 보존/원복 금지

---

## 11. 단계별 구현 계획

### Phase 0 — 기준선 고정

- 현재 KCC 루트모션/공중 공격 작업을 먼저 컴파일 가능한 체크포인트로 만든다.
- MotionSet/Ultimate managed reference, VFX 참조, Missing Script 기준선을 새로 측정한다.
- 대표 에셋과 프리팹 목록을 고정한다.
- 현재 `ActorAnimator` 공개 API 사용처를 목록화한다.

완료 조건:

- 관련 asmdef CLI 컴파일 오류 0
- Unity Console 컴파일 오류 0
- 기준선 리포트 저장

### Phase 1 — Embedded package와 Core 추출

- `Packages/com.uplayground.motionset` embedded package 골격 생성
- package.json, Runtime/Core, Runtime/Animancer, Editor, Tests, Samples~ 디렉터리 계약 확정
- `UPlayGround.MotionSet.Core.asmdef`를 패키지 안에 생성
- 데이터 모델, Resolver, Event base를 패키지로 이동
- `.meta` 동반 이동
- Data, Ability.UPlayGround, Actor, 관련 Editor/Test asmdef 참조 갱신
- namespace와 public type 이름 유지

완료 조건:

- Data/Ability/Actor/Editor 컴파일 오류 0
- 패키지 내부에 UPlayground 금지 의존 0
- MotionTimelineResolver 기존 테스트 통과
- MotionSetAsset 인스펙터 로드 정상
- managed reference/VFX 기준선 불변

### Phase 2 — Executor 분리

- `IMotionEventTargetProvider` 추가
- `GameActor` 직접 탐색 제거
- Executor를 Core로 이동
- 범용 진단과 Actor MotionWarp 진단 분리
- GameActor가 target provider 구현

완료 조건:

- 루트/자식 모델 배치 모두 같은 이벤트 대상 해석
- Queued/Exact/PostEvaluation/Reentry 정책 회귀 없음
- 전투 Collision/VFX/SFX 이벤트 발화 정상

### Phase 3 — 재생 커널 추출

- `IMotionTimeSource`와 `IMotionTimelineControlEvent` 추가
- `LoopEvent`는 Actor에 남겨 계약 구현
- `ActorAnimator` 내부의 일반 재생 상태를 `MotionSetPlaybackController`로 이동
- ActorAnimator는 기존 API를 위임
- `RootMotionStepBuffer`는 현재 KCC 작업이 안정된 뒤 이동 여부 결정

이 Phase는 작은 수직 절편으로 나눈다.

1. 재생 시작/종료와 Motion 전환
2. Section/Seek
3. 이벤트 시계/PostEvaluation
4. Loop/Freeze/InfiniteLoop
5. 병렬 레이어/동시성/동기화
6. Snapshot/Restore/Debug
7. Root motion

각 절편마다 기존 ActorAnimator 경로와 결과를 비교한다.

완료 조건:

- `ActorAnimator` 호출부 대량 수정 없이 기존 동작 유지
- GameActor 타입이 MotionSet.Animancer 어셈블리에 나타나지 않음
- 플레이어/몬스터 공격 상태의 시작·종료·중단 결과 동일

### Phase 4 — 비-GameActor 호스트

- `MotionSetPlayer` MonoBehaviour 추가
- GameActor 없는 테스트 오브젝트에서 MotionSetAsset 직접 재생
- 명시적 대상, 부모 provider, self fallback 검증

완료 조건:

- GameActor/KCC/Ability 없이 MotionSet 재생 가능
- Section/Loop/Event/PostEvaluation 테스트 통과

### Phase 5 — Editor 코어 분리

- Descriptor/Provider 기반 카탈로그로 전환
- 범용 Inspector/Drawer/Timeline을 `UPlayGround.MotionSet.Editor`로 이동
- Combat/Warp/VFX/프리셋은 프로젝트 Editor 확장으로 유지

완료 조건:

- 추가 팝업 항목 수, 검색, 카테고리, 색, 아이콘 불변
- MotionSetWindow의 전투/워프/VFX 기능 불변
- Editor 런타임 asmdef의 `UnityEditor` 참조 0

### Phase 6 — 외부 프로젝트 설치 검증

- 빈 Unity 6 URP 프로젝트를 별도 검증 프로젝트로 만든다.
- Animancer Pro 8.3.1을 정상 라이선스 경로로 설치한다.
- `com.uplayground.motionset`만 embedded/local/git package로 설치한다.
- UPlayground 저장소의 `Assets` 폴더를 참조하거나 복사하지 않는다.
- 패키지 Runtime/Editor/Test 컴파일을 확인한다.
- BasicPlayback sample을 import해 재생한다.
- 새 사용자 정의 MotionEvent를 패키지 밖 어셈블리에 만들고 Editor 자동 발견과 런타임 발화를 확인한다.
- 패키지를 read-only package cache 배치로 설치해 프로젝트 상대경로 쓰기 가정이 없는지 확인한다.

외부 검증 프로젝트에서 성공해야 독립 패키지 완료로 선언한다. UPlayground 본 프로젝트에서 asmdef 컴파일이 성공한 것만으로는 완료가 아니다.

---

## 12. 테스트 설계

### 12.1 `UPlayGround.MotionSet.Core.Tests`

- Motion 구간과 전체 Duration
- Section layout/next/loop/hold
- Marker와 linked event 시간 해석
- Event range/active/reentry/order
- TimeStretch/Curve 계산
- 잘못된 데이터의 결정적 오류 메시지

### 12.2 `UPlayGround.MotionSet.Animancer.Tests`

- GameActor 없는 `MotionSetPlayer` 재생
- 여러 Motion 순차 전환
- baseLayerIndex와 병렬 레이어
- Section jump/override/hold/loop
- Loop/Freeze/InfiniteLoop 계약
- seek 후 이벤트 중복 발화 방지
- PostAnimationEvaluation 지연 발화
- 중단/완료/무효화 종료 사유
- root motion 누적/1회 소비

### 12.3 Actor 호환 테스트

- `GameActor.LocalTimeScale` 반영
- GameplayTag 의미 슬롯 해석
- Player 무기별 MotionReference 해석
- Player/Monster AttackState 시작·중단·종료
- KCC 루트모션 스텝 소비
- MotionWarp, Collision, SlashVFX, Projectile, Camera 이벤트
- `WaitMotionSetEndAbilityTask` 완료/취소

### 12.4 에셋 무손실 검증

- MotionSet/Ultimate 전체 managed reference 누락 0
- VFX/ObjectReference 누락 0
- `asm: UPlayGround.Actor`인 구체 MotionEvent 타입 수가 기준선과 동일
- Player/UI 프리팹 Missing Script 0
- 검증 전후 `Assets/10.Datas`, `Assets/03.Prefabs` diff 검사

---

## 13. 수용 기준

분리 완료는 다음을 모두 만족할 때 선언한다.

- `UPlayGround.MotionSet.Core`에 Actor/Data/Contracts/Camera/UI/Manager 참조가 없다.
- `UPlayGround.MotionSet.Animancer`에 Actor/Data/KCC 참조가 없다.
- GameActor 없이 MotionSet을 재생하는 PlayMode 테스트가 있다.
- 기존 `ActorAnimator` 공개 경로가 호환된다.
- Ability의 MotionReference 단일 소스 경로가 유지된다.
- 구체 MotionEvent의 어셈블리 이동이 없다.
- managed reference, VFX, Missing Script 누락이 0이다.
- Lock-on/전투 카메라/KillCam/대화/스냅샷을 포함한 기존 카메라 스모크에 새 회귀가 없다.
- Player Build 오류가 0이다.

---

## 14. 주요 위험과 대응

| 위험 | 영향 | 대응 |
|---|---|---|
| `ActorAnimator` 대형 일괄 추출 | 공격/루프/레이어 회귀 | 수직 절편별 위임 전환 |
| 구체 MotionEvent 재이동 | managed reference/VFX 유실 | Actor에 유지, 인터페이스 역전 |
| 상속 구조 변경 | Unity 메시지/프리팹 회귀 | ActorAnimator 퍼사드 + 일반 C# 커널 |
| 현재 KCC 루트모션 작업과 충돌 | 이동/공중 공격 회귀 | Phase 0 체크포인트 후 root motion은 마지막 절편 |
| Editor 전체 동시 분리 | 툴 사용 중단 | Runtime 먼저, Editor는 Provider 전환 후 |
| Data가 Animancer를 참조 | 하위 모듈 오염 | Core/Animancer asmdef 분리 |
| 이벤트 시계 변경 | 타격/VFX 프레임 오차 | 포즈 기반 LateUpdate 판정 유지 |

---

## 15. 구현 시작 권고

첫 구현 PR/작업 단위는 **Phase 1만** 권장한다.

이 단계에서는:

- 새 Core asmdef 생성
- 직렬화 안전 타입만 이동
- 참조 갱신
- 테스트/에셋 무손실 검증

만 수행한다.

`ActorAnimator`, LoopEvent, RootMotion, MotionSetWindow는 건드리지 않는다. 이 기준으로 Core 경계를 먼저 실제 컴파일러가 강제하게 만든 뒤 Executor와 재생 커널을 후속 단계로 옮기는 것이 가장 안전하다.
