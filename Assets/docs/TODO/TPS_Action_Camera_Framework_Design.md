# TPS Action Camera Framework 설계 문서 v1.0

> Reference: NDC26 마비노기 모바일 카메라 시스템 설계 세션 기반 정리  
> 목적: 현재 Unity TPS 액션 게임의 카메라 시스템을 확장 가능한 프레임워크 구조로 재설계한다.

---

## 0. 문서 목적

현재 카메라 시스템은 `CameraManager` 하나에 여러 기능이 집중되기 쉽다.

예시:

```text
CameraManager
├ LockOn
├ CombatCamera
├ CameraCollision
├ CrowdZoomOut
├ CameraShake
├ FOV
├ SkillCamera
├ DialogueCamera
└ CutsceneCamera
```

초기에는 빠르게 구현할 수 있지만, 기능이 늘어나면 다음 문제가 발생한다.

- `if` 분기 증가
- 기능 간 간섭 증가
- 상태 전환 버그 증가
- 스킬, 보스전, 컷신 등 예외 처리 증가
- 특정 기능 수정 시 다른 기능이 깨질 가능성 증가

따라서 카메라를 하나의 거대한 기능이 아니라, 여러 작은 기능을 조합하는 프레임워크로 설계한다.

---

# 1장. 설계 목표

## 1.1 핵심 목표

카메라 시스템의 목표는 다음과 같다.

1. `CameraManager` 거대화 방지
2. 기능 단위 분리
3. 전투 / 비전투 / 락온 / 보스전 / 대화 / 컷신 상태 대응
4. 카메라 흔들림, 충돌, FOV, 오프셋, 락온 등을 독립적으로 관리
5. 이후 스킬 연출, 피격 연출, 킬캠, 컷신 카메라로 확장 가능

---

## 1.2 기존 방식의 문제

기존 방식은 보통 다음처럼 구현된다.

```csharp
public class CameraManager : MonoBehaviour
{
    private void LateUpdate()
    {
        FollowPlayer();

        if (isCombat)
            ApplyCombatCamera();

        if (isLockOn)
            ApplyLockOnCamera();

        if (isBossBattle)
            ApplyBossCamera();

        if (isDialogue)
            ApplyDialogueCamera();

        if (isCutscene)
            ApplyCutsceneCamera();

        ApplyCollision();
        ApplyFov();
        ApplyShake();
    }
}
```

이 방식은 기능이 적을 때는 단순하지만, 상태가 늘어날수록 유지보수가 어려워진다.

---

## 1.3 목표 구조

최종 목표 구조는 다음과 같다.

```text
CameraDirector
    ↓
CameraBehavior
    ↓
CameraModifier
    ↓
CameraState
    ↓
CameraResolver
    ↓
Unity Camera
```

각 계층의 책임은 다음과 같다.

| 계층 | 역할 |
|---|---|
| CameraDirector | 현재 게임 상황을 보고 어떤 카메라 상태를 사용할지 결정 |
| CameraBehavior | 전투, 락온, 보스전 같은 큰 카메라 모드 정의 |
| CameraModifier | Follow, LockOn, Collision, FOV 같은 작은 기능 단위 |
| CameraState | 최종 카메라 계산 결과를 담는 데이터 |
| CameraResolver | CameraState를 실제 Unity Camera에 적용 |

---

# 2장. 전체 아키텍처

## 2.1 실행 흐름

```text
Player / Combat / Game State
    ↓
CameraDirector
    ↓
Current CameraBehavior 선택
    ↓
Behavior 내부 Modifier 순차 실행
    ↓
CameraState 계산
    ↓
CameraResolver가 Unity Camera에 적용
```

---

## 2.2 주요 클래스 구성

```text
Camera
├ CameraDirector.cs
├ CameraState.cs
├ CameraResolver.cs
│
├ Behaviors
│   ├ CameraBehaviorBase.cs
│   ├ ExploreCameraBehavior.cs
│   ├ CombatCameraBehavior.cs
│   ├ LockOnCameraBehavior.cs
│   ├ BossCameraBehavior.cs
│   ├ DialogueCameraBehavior.cs
│   └ CutsceneCameraBehavior.cs
│
└ Modifiers
    ├ ICameraModifier.cs
    ├ FollowCameraModifier.cs
    ├ OffsetCameraModifier.cs
    ├ CombatOffsetCameraModifier.cs
    ├ LockOnCameraModifier.cs
    ├ CrowdZoomCameraModifier.cs
    ├ ObstacleCameraModifier.cs
    ├ FovCameraModifier.cs
    └ ShakeCameraModifier.cs
```

---

## 2.3 설계 원칙

### 원칙 1. 상태 판단은 Director에서만 한다

`LockOn`, `Combat`, `Boss`, `Cutscene` 같은 상태 판단은 `CameraDirector`가 담당한다.

`Modifier`는 자신이 언제 켜지는지 판단하지 않는다.

---

### 원칙 2. Behavior는 Modifier 묶음이다

예를 들어 전투 카메라는 다음 기능 조합으로 구성된다.

```text
CombatBehavior
├ FollowModifier
├ CombatOffsetModifier
├ CrowdZoomModifier
├ ObstacleModifier
└ FovModifier
```

---

### 원칙 3. Modifier는 작은 기능 하나만 담당한다

좋은 예:

```text
LockOnModifier: 락온 타겟 기준 회전 보정만 담당
ObstacleModifier: 벽 충돌 보정만 담당
FovModifier: FOV 보간만 담당
```

나쁜 예:

```text
CombatModifier 안에서 Follow, FOV, Collision, Shake까지 모두 처리
```

---

# 3장. CameraState 설계

## 3.1 역할

`CameraState`는 카메라 계산 결과를 담는 데이터 컨테이너다.

중요한 규칙:

- 계산 로직을 넣지 않는다.
- `Transform`, `MonoBehaviour`에 의존하지 않는다.
- 현재 프레임의 최종 카메라 상태만 담는다.

---

## 3.2 CameraState 예시

```csharp
using UnityEngine;

public struct CameraState
{
    public Vector3 Position;
    public Quaternion Rotation;

    public float Fov;
    public float Distance;

    public Vector3 Offset;

    public float Pitch;
    public float Yaw;

    public Vector3 LookAtPosition;
    public Vector3 FollowPosition;

    public float ShakeStrength;

    public static CameraState Default()
    {
        return new CameraState
        {
            Position = Vector3.zero,
            Rotation = Quaternion.identity,
            Fov = 60f,
            Distance = 5f,
            Offset = Vector3.zero,
            Pitch = 20f,
            Yaw = 0f,
            LookAtPosition = Vector3.zero,
            FollowPosition = Vector3.zero,
            ShakeStrength = 0f,
        };
    }
}
```

---

## 3.3 CameraState에 넣을 수 있는 값

| 값 | 설명 |
|---|---|
| Position | 최종 카메라 위치 |
| Rotation | 최종 카메라 회전 |
| Fov | 최종 시야각 |
| Distance | 타겟과의 거리 |
| Offset | 타겟 기준 오프셋 |
| Pitch | 상하 회전 |
| Yaw | 좌우 회전 |
| LookAtPosition | 바라볼 위치 |
| FollowPosition | 따라갈 기준 위치 |
| ShakeStrength | 흔들림 강도 |

---

# 4장. CameraModifier 설계

## 4.1 역할

`CameraModifier`는 카메라 기능의 최소 단위다.

예시:

```text
FollowModifier
LockOnModifier
ObstacleModifier
CrowdZoomModifier
FovModifier
ShakeModifier
```

Modifier는 `CameraState`를 입력받아 필요한 값을 수정한다.

---

## 4.2 ICameraModifier 인터페이스

```csharp
public interface ICameraModifier
{
    int Priority { get; }

    void Apply(ref CameraState state, float deltaTime);
}
```

---

## 4.3 Priority 규칙

Modifier는 실행 순서가 중요하다.

추천 순서:

```text
100 Follow
200 Offset
300 LockOn
400 CrowdZoom
500 Obstacle
600 FOV
900 Shake
```

이유:

- Follow가 먼저 기준 위치를 만든다.
- Offset, LockOn이 구도를 잡는다.
- CrowdZoom이 거리를 보정한다.
- Obstacle이 최종 위치를 충돌 보정한다.
- Shake는 마지막에 적용하는 것이 안전하다.

---

## 4.4 FollowModifier 예시

```csharp
using UnityEngine;

public sealed class FollowCameraModifier : ICameraModifier
{
    public int Priority => 100;

    private readonly Transform target;
    private readonly float followHeight;

    public FollowCameraModifier(Transform target, float followHeight)
    {
        this.target = target;
        this.followHeight = followHeight;
    }

    public void Apply(ref CameraState state, float deltaTime)
    {
        if (target == null)
            return;

        state.FollowPosition = target.position + Vector3.up * followHeight;
        state.LookAtPosition = state.FollowPosition;
    }
}
```

---

## 4.5 CombatOffsetModifier 예시

```csharp
using UnityEngine;

public sealed class CombatOffsetCameraModifier : ICameraModifier
{
    public int Priority => 200;

    private readonly Vector3 combatOffset;
    private readonly float smoothSpeed;

    public CombatOffsetCameraModifier(Vector3 combatOffset, float smoothSpeed)
    {
        this.combatOffset = combatOffset;
        this.smoothSpeed = smoothSpeed;
    }

    public void Apply(ref CameraState state, float deltaTime)
    {
        state.Offset = Vector3.Lerp(
            state.Offset,
            combatOffset,
            1f - Mathf.Exp(-smoothSpeed * deltaTime));
    }
}
```

---

## 4.6 ObstacleModifier 예시

```csharp
using UnityEngine;

public sealed class ObstacleCameraModifier : ICameraModifier
{
    public int Priority => 500;

    private readonly LayerMask collisionMask;
    private readonly float radius;
    private readonly float collisionOffset;

    public ObstacleCameraModifier(
        LayerMask collisionMask,
        float radius,
        float collisionOffset)
    {
        this.collisionMask = collisionMask;
        this.radius = radius;
        this.collisionOffset = collisionOffset;
    }

    public void Apply(ref CameraState state, float deltaTime)
    {
        Vector3 from = state.LookAtPosition;
        Vector3 to = state.Position;
        Vector3 direction = to - from;
        float distance = direction.magnitude;

        if (distance <= 0.01f)
            return;

        direction /= distance;

        if (Physics.SphereCast(
            from,
            radius,
            direction,
            out RaycastHit hit,
            distance,
            collisionMask,
            QueryTriggerInteraction.Ignore))
        {
            state.Position = hit.point - direction * collisionOffset;
        }
    }
}
```

---

# 5장. CameraBehavior 설계

## 5.1 역할

`CameraBehavior`는 특정 상황에서 사용할 Modifier 묶음이다.

예시:

```text
ExploreBehavior: 비전투 탐색 카메라
CombatBehavior: 전투 카메라
LockOnBehavior: 락온 카메라
BossBehavior: 보스전 카메라
DialogueBehavior: 대화 카메라
CutsceneBehavior: 컷신 카메라
```

---

## 5.2 CameraBehaviorType

```csharp
public enum CameraBehaviorType
{
    Explore,
    Combat,
    LockOn,
    Boss,
    Dialogue,
    Cutscene,
}
```

---

## 5.3 ICameraBehavior 인터페이스

```csharp
public interface ICameraBehavior
{
    CameraBehaviorType Type { get; }

    void Enter();
    void Exit();

    void Update(ref CameraState state, float deltaTime);
}
```

---

## 5.4 CameraBehaviorBase

```csharp
using System.Collections.Generic;
using System.Linq;

public abstract class CameraBehaviorBase : ICameraBehavior
{
    private readonly List<ICameraModifier> modifiers = new();

    public abstract CameraBehaviorType Type { get; }

    public virtual void Enter()
    {
    }

    public virtual void Exit()
    {
    }

    public void AddModifier(ICameraModifier modifier)
    {
        modifiers.Add(modifier);
        modifiers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    public virtual void Update(ref CameraState state, float deltaTime)
    {
        foreach (var modifier in modifiers)
        {
            modifier.Apply(ref state, deltaTime);
        }
    }
}
```

---

## 5.5 CombatBehavior 예시

```csharp
using UnityEngine;

public sealed class CombatCameraBehavior : CameraBehaviorBase
{
    public override CameraBehaviorType Type => CameraBehaviorType.Combat;

    public CombatCameraBehavior(
        Transform player,
        LayerMask collisionMask)
    {
        AddModifier(new FollowCameraModifier(player, 1.2f));
        AddModifier(new CombatOffsetCameraModifier(new Vector3(0.5f, 0.2f, 0f), 10f));
        AddModifier(new ObstacleCameraModifier(collisionMask, 0.3f, 0.2f));
        AddModifier(new FovCameraModifier(65f, 8f));
    }
}
```

---

# 6장. Behavior 종류

## 6.1 ExploreCameraBehavior

비전투 탐색 상태에서 사용하는 카메라다.

목표:

- 플레이어를 자연스럽게 따라간다.
- 시야가 너무 좁지 않게 유지한다.
- 벽 충돌을 처리한다.

구성:

```text
FollowModifier
DefaultOffsetModifier
ObstacleModifier
FovModifier
```

추천 값:

| 항목 | 값 |
|---|---|
| Distance | 5.0 |
| FOV | 55 |
| Offset | (0, 1.0, 0) |

---

## 6.2 CombatCameraBehavior

일반 전투 상태에서 사용하는 카메라다.

목표:

- 캐릭터 주변 전투 상황을 잘 보여준다.
- 약간 넓은 FOV를 사용한다.
- 다수 전투 시 카메라를 조금 뒤로 뺀다.

구성:

```text
FollowModifier
CombatOffsetModifier
CrowdZoomModifier
ObstacleModifier
FovModifier
```

추천 값:

| 항목 | 값 |
|---|---|
| Distance | 5.5 ~ 7.0 |
| FOV | 60 ~ 65 |
| Offset | (0.5, 1.2, 0) |

---

## 6.3 LockOnCameraBehavior

락온 상태에서 사용하는 카메라다.

목표:

- 플레이어와 타겟을 동시에 보여준다.
- 타겟 중심으로 시선을 보정한다.
- 너무 강한 자동 회전으로 조작감을 해치지 않는다.

구성:

```text
FollowModifier
LockOnModifier
TargetCenterModifier
ObstacleModifier
FovModifier
```

추천 값:

| 항목 | 값 |
|---|---|
| Distance | 4.5 |
| FOV | 50 ~ 55 |
| Offset | (0.6, 1.0, 0) |
| TargetMidPointWeight | 0.3 ~ 0.4 |

---

## 6.4 BossCameraBehavior

보스전에서 사용하는 카메라다.

목표:

- 플레이어와 보스를 동시에 보여준다.
- 보스 크기에 따라 거리를 조정한다.
- 대형 공격 텔레그래프가 화면 안에 들어오도록 한다.

구성:

```text
FollowModifier
BossTargetModifier
BossDistanceModifier
ObstacleModifier
FovModifier
```

추천 값:

| 항목 | 값 |
|---|---|
| Distance | 7.0 ~ 10.0 |
| FOV | 60 ~ 70 |
| Offset | 보스 크기 기반 동적 계산 |

---

## 6.5 DialogueCameraBehavior

NPC 대화 상태에서 사용하는 카메라다.

목표:

- 플레이어와 NPC를 보기 좋은 구도로 배치한다.
- 전투 카메라와 분리한다.
- 필요하면 UI 대화창에 맞춰 화면 여백을 확보한다.

구성:

```text
DialogueOffsetModifier
DialogueLookAtModifier
FovModifier
```

추천 값:

| 항목 | 값 |
|---|---|
| FOV | 35 ~ 45 |
| Distance | 2.5 ~ 4.0 |

---

## 6.6 CutsceneCameraBehavior

컷신이나 Timeline 연출에서 사용하는 카메라다.

목표:

- 게임플레이 카메라 로직을 일시적으로 중단한다.
- Timeline 또는 연출 시스템이 카메라를 제어한다.
- 컷신 종료 후 이전 Behavior로 복귀한다.

구성:

```text
TimelineCameraModifier
또는 ExternalCameraControlModifier
```

주의:

- 컷신 중에는 일반 Follow, LockOn, Obstacle을 끄는 것이 안전하다.
- 단, 연출용 충돌 보정이 필요하면 별도 Modifier로 분리한다.

---

# 7장. CameraDirector 설계

## 7.1 역할

`CameraDirector`는 카메라 시스템의 최상위 관리자다.

담당 역할:

1. 현재 게임 상태 확인
2. 우선순위에 따라 Behavior 선택
3. Behavior 전환 처리
4. 매 프레임 현재 Behavior 업데이트
5. 최종 CameraState를 Resolver에 전달

---

## 7.2 상태 우선순위

카메라 상태는 동시에 여러 개가 참일 수 있다.

예시:

```text
전투 중 + 락온 중 + 보스전 중
대화 중 + 전투 상태 유지
컷신 중 + 보스전 상태 유지
```

따라서 명확한 우선순위가 필요하다.

추천 우선순위:

```text
1. Cutscene
2. Dialogue
3. Boss
4. LockOn
5. Combat
6. Explore
```

컷신이 가장 높고, 기본 탐색 카메라가 가장 낮다.

---

## 7.3 CameraDirector 예시

```csharp
using System.Collections.Generic;
using UnityEngine;

public sealed class CameraDirector
{
    private readonly Dictionary<CameraBehaviorType, ICameraBehavior> behaviors = new();

    private ICameraBehavior currentBehavior;

    private readonly ICameraContext context;

    public CameraDirector(ICameraContext context)
    {
        this.context = context;
    }

    public void Register(ICameraBehavior behavior)
    {
        behaviors[behavior.Type] = behavior;
    }

    public void Tick(ref CameraState state, float deltaTime)
    {
        CameraBehaviorType nextType = DetermineBehaviorType();

        if (currentBehavior == null || currentBehavior.Type != nextType)
        {
            ChangeBehavior(nextType);
        }

        currentBehavior?.Update(ref state, deltaTime);
    }

    private void ChangeBehavior(CameraBehaviorType nextType)
    {
        currentBehavior?.Exit();

        if (behaviors.TryGetValue(nextType, out var nextBehavior))
        {
            currentBehavior = nextBehavior;
            currentBehavior.Enter();
        }
    }

    private CameraBehaviorType DetermineBehaviorType()
    {
        if (context.IsCutscene)
            return CameraBehaviorType.Cutscene;

        if (context.IsDialogue)
            return CameraBehaviorType.Dialogue;

        if (context.IsBossBattle)
            return CameraBehaviorType.Boss;

        if (context.IsLockOn)
            return CameraBehaviorType.LockOn;

        if (context.IsCombat)
            return CameraBehaviorType.Combat;

        return CameraBehaviorType.Explore;
    }
}
```

---

## 7.4 CameraContext

`CameraDirector`가 직접 Player, CombatManager, LockOnSystem에 강하게 의존하지 않도록 Context를 둔다.

```csharp
public interface ICameraContext
{
    bool IsCombat { get; }
    bool IsLockOn { get; }
    bool IsBossBattle { get; }
    bool IsDialogue { get; }
    bool IsCutscene { get; }
}
```

Unity 구현체 예시:

```csharp
using UnityEngine;

public sealed class UnityCameraContext : MonoBehaviour, ICameraContext
{
    [SerializeField] private CombatController combatController;
    [SerializeField] private LockOnController lockOnController;
    [SerializeField] private BossBattleController bossBattleController;
    [SerializeField] private DialogueController dialogueController;
    [SerializeField] private CutsceneController cutsceneController;

    public bool IsCombat => combatController != null && combatController.IsCombat;
    public bool IsLockOn => lockOnController != null && lockOnController.HasTarget;
    public bool IsBossBattle => bossBattleController != null && bossBattleController.IsActive;
    public bool IsDialogue => dialogueController != null && dialogueController.IsActive;
    public bool IsCutscene => cutsceneController != null && cutsceneController.IsPlaying;
}
```

---

## 7.5 CameraResolver

`CameraResolver`는 최종 `CameraState`를 실제 Unity Camera에 적용한다.

```csharp
using UnityEngine;

public sealed class CameraResolver : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;

    public void Resolve(CameraState state)
    {
        Transform cameraTransform = targetCamera.transform;

        cameraTransform.position = state.Position;
        cameraTransform.rotation = state.Rotation;
        targetCamera.fieldOfView = state.Fov;
    }
}
```

---

## 7.6 MonoBehaviour 진입점 예시

```csharp
using UnityEngine;

public sealed class GameCameraController : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private UnityCameraContext context;
    [SerializeField] private CameraResolver resolver;

    private CameraDirector director;
    private CameraState state;

    private void Awake()
    {
        state = CameraState.Default();

        director = new CameraDirector(context);

        director.Register(new ExploreCameraBehavior(player, collisionMask));
        director.Register(new CombatCameraBehavior(player, collisionMask));
        director.Register(new LockOnCameraBehavior(player, collisionMask));
        director.Register(new BossCameraBehavior(player, collisionMask));
        director.Register(new DialogueCameraBehavior(player, collisionMask));
        director.Register(new CutsceneCameraBehavior());
    }

    private void LateUpdate()
    {
        float deltaTime = Time.deltaTime;

        director.Tick(ref state, deltaTime);
        resolver.Resolve(state);
    }
}
```

---

# 8. 현재 프로젝트 기준 적용 계획

현재 `CameraManager`에 있는 기능은 다음처럼 분리하는 것을 추천한다.

| 현재 기능 | 변경 후 |
|---|---|
| 기본 추적 | FollowCameraModifier |
| 기본 Offset | DefaultOffsetCameraModifier |
| 전투 Offset | CombatOffsetCameraModifier |
| 락온 회전 | LockOnCameraModifier |
| 락온 타겟 중앙 보정 | TargetCenterCameraModifier |
| 군중 ZoomOut | CrowdZoomCameraModifier |
| 카메라 벽 충돌 | ObstacleCameraModifier |
| 상태별 FOV | FovCameraModifier |
| 카메라 흔들림 | ShakeCameraModifier |
| 카메라 자동 정렬 | AlignCameraModifier |
| 스킬 연출 카메라 | SkillCameraBehavior 또는 SkillCameraModifier |
| 컷신 카메라 | CutsceneCameraBehavior |

---

# 9. 구현 순서 추천

## 1단계: 데이터 구조 분리

- `CameraState` 추가
- `CameraResolver` 추가
- 기존 CameraManager가 계산한 값을 CameraState에 담도록 변경

## 2단계: Modifier 분리

우선순위:

1. FollowModifier
2. OffsetModifier
3. ObstacleModifier
4. FovModifier
5. LockOnModifier
6. CrowdZoomModifier

## 3단계: Behavior 도입

- ExploreBehavior
- CombatBehavior
- LockOnBehavior

먼저 3개만 도입한다.

## 4단계: Director 도입

- 전투 / 비전투 / 락온 전환을 Director가 담당하도록 변경

## 5단계: 확장

- BossBehavior
- DialogueBehavior
- CutsceneBehavior
- ShakeModifier
- SkillCameraBehavior

---

# 10. 주의사항

## 10.1 처음부터 과하게 추상화하지 않는다

초기에는 다음 3개 Behavior만 만들어도 충분하다.

```text
Explore
Combat
LockOn
```

보스전, 대화, 컷신은 구조가 안정된 뒤 추가한다.

---

## 10.2 Modifier 간 의존성을 줄인다

좋지 않은 구조:

```text
LockOnModifier가 ObstacleModifier의 내부 값을 직접 참조
```

좋은 구조:

```text
각 Modifier는 CameraState만 읽고 쓴다
```

---

## 10.3 Shake는 가능하면 마지막에 적용한다

카메라 흔들림을 너무 일찍 적용하면 벽 충돌, 락온 보정, FOV 보정과 충돌할 수 있다.

추천:

```text
Follow → LockOn → Obstacle → FOV → Shake
```

---

# 11. 결론

이 구조의 핵심은 다음과 같다.

```text
CameraDirector = 어떤 카메라를 쓸지 결정
CameraBehavior = 상황별 카메라 모드
CameraModifier = 작은 카메라 기능
CameraState = 최종 카메라 데이터
CameraResolver = Unity Camera 적용
```

기존 `CameraManager` 중심 구조보다 초기 구현량은 조금 늘어나지만, 이후 다음 기능을 추가할 때 훨씬 안정적이다.

- 락온 카메라
- 보스전 카메라
- 스킬 연출 카메라
- 히트 카메라
- 컷신 카메라
- 대화 카메라
- 킬캠
- 카메라 쉐이크
- 자동 보정 카메라

따라서 현재 TPS 액션 게임 프로젝트에서는 `CameraManager`를 유지하되, 내부 기능을 점진적으로 `State / Behavior / Modifier / Director` 구조로 분리하는 방식을 추천한다.

