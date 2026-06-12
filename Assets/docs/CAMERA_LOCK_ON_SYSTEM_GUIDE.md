# Camera Lock-On 시스템 분석 가이드

## 개요

이 문서는 현재 UPlayground 카메라 시스템의 락온 처리 흐름을 코드 기준으로 정리하고, 명조(Wuthering Waves)식 전투 카메라 감각과 비교했을 때의 차이와 개선 포인트를 정리한다.

명조의 내부 구현은 공개되어 있지 않으므로, 이 문서의 비교 기준은 원본 수치나 알고리즘 복제가 아니라 다음과 같은 관찰 가능한 전투 카메라 목표다.

| 목표 | 설명 |
|------|------|
| 대상 중심 전투 가독성 | 플레이어와 주요 적, 위험 방향이 화면에서 계속 읽혀야 한다 |
| 입력 존중 | 자동 보정은 강하지만 플레이어 수동 조작과 싸우지 않아야 한다 |
| 전투 맥락 반영 | 최근 공격자, 위협도, 보스/정예, 화면 중앙성, 입력 방향이 타겟 선정에 반영되어야 한다 |
| 안정적인 재획득 | 빠른 이동, 차폐, 타겟 사망, 거리 이탈 상황에서 카메라가 급격히 튀지 않아야 한다 |
| 대형 몬스터 대응 | 단일 Transform이 아니라 몸통/머리/약점/중심점 등 락온 포인트를 구분할 수 있어야 한다 |

현재 구현은 기본 락온 카메라로는 충분한 구조를 갖추고 있다. 다만 명조식 전투 체감에 가까워지려면 단순 회전 보간보다 **타겟 선정, 유지, 전환, 프레이밍 책임을 전투 맥락까지 확장**하는 작업이 우선이다.

### 구현 현황

| 단계 | 상태 | 반영 내용 |
|------|------|-----------|
| P0 — 차폐/가시성 검증 | 구현 완료 | `CameraLockOn` 후보 수집 시 `CameraConfig.GetCollisionLayerMask()` 기반 SphereCast/Raycast 검증, 현재 타겟 차폐 grace 적용 |
| P1 — 타겟 점수 모델 분리 | 구현 완료 | `LockOnCandidate`와 `EvaluateTargetScore()`로 후보 점수 계산 분리, 현재 타겟 유지 보너스와 `ILockOnTarget.LockOnPriority` 반영 |
| P2 — 획득/유지 거리 히스테리시스 | 구현 완료 | `lockOnRange`는 획득 거리로 유지, `lockOnReleaseRange`와 `lockOnLostGraceTime`으로 현재 타겟 유지 조건 분리 |
| P3 — 타겟 전환 로직 개선 | 구현 완료 | 화면 X 인덱스 clamp 대신 좌/우 방향 후보 점수 선택, wrap 및 화면/중앙/거리 가중치 추가 |
| P4 — 플레이어-타겟 쌍 프레이밍 | 구현 완료 | 락온 중 플레이어에서 타겟 방향으로 제한된 피벗 오프셋을 SmoothDamp로 적용 |
| P5 — `ILockOnTarget` 도입 | 구현 완료 | 선택 인터페이스로 포커스 위치, UI 앵커, 우선순위, lock 가능 여부를 제공. 미구현 대상은 기존 `IDamageable` 기준 fallback |
| P6 — 소프트락/하드락 분리 | 구현 완료 | `CombatCameraDirector`의 soft target assist 조건을 별도 정책 메서드로 분리하고 하드락 중 개입 금지 명시 |

---

## 현재 구조

```
InputManager
    └── PlayerAction.LockOn / LockOnSwitchLeft / LockOnSwitchRight
            ↓
CameraManager
    ├── CameraLockOn
    │     ├── TryActivate()
    │     ├── SwitchTarget(direction)
    │     ├── Release()
    │     ├── UpdateRotation(ref yaw, ref pitch, skipRotation)
    │     └── UpdateTransition(ref yaw, ref pitch, skipCondition)
    │
    ├── CameraDistanceController
    │     ├── UpdateFOV(isLockOn, isCombat)
    │     └── EvaluateDistance(isLockOn, isCombat, currentTargetDist)
    │
    └── CameraModeController
          └── InGameCameraMode
                ├── HandleInput()
                └── EvaluatePose()
```

### 파일 구조

```
Assets/02.Scripts/
├── Manager/
│   └── CameraManager.cs
│
├── Camera/
│   ├── CameraLockOn.cs
│   ├── CameraDistanceController.cs
│   ├── CameraCollision.cs
│   ├── Combat/
│   │   └── CombatCameraDirector.cs
│   └── Modes/
│       ├── InGameCameraMode.cs
│       └── CameraRuntimeContext.cs
│
├── Data/Camera/
│   └── CameraSettings.cs
│
└── GameActor/
    ├── State/Player/PlayerAttackState.cs
    ├── MovementController/IWarpTargetResolver.cs
    └── Object/Monster/MonsterActor.cs
```

---

## 현재 락온 처리 흐름

### 입력

`CameraManager.AfterInit()`에서 다음 입력을 등록한다.

| 입력 | 처리 |
|------|------|
| `PlayerAction.LockOn` | 락온 중이면 `Release()`, 아니면 `TryActivate()` |
| `PlayerAction.LockOnSwitchLeft` | 락온 중이면 `SwitchTarget(-1)` |
| `PlayerAction.LockOnSwitchRight` | 락온 중이면 `SwitchTarget(1)` |

락온 시도 실패 시 `StartCameraAlign()`으로 플레이어 정면 기준 카메라 정렬을 시작한다.

### 타겟 수집

`CameraLockOn.CollectTargets()`는 플레이어 위치 기준 `Physics.OverlapSphere`로 후보를 찾는다.

현재 후보 조건:

| 조건 | 처리 |
|------|------|
| 플레이어 자신 또는 자식 | 제외 |
| `IDamageable` 없음 | 제외 |
| `IDamageable.IsAlive() == false` | 제외 |
| 카메라 viewport 밖 | 제외하지 않고 `sortScore += 1f` 페널티 |
| 거리 | `lockOnRange` 기준 |

현재 정렬 기준:

| `LockOnPriorityMode` | 점수 |
|----------------------|------|
| `Distance` | 거리 점수 |
| `CameraDirection` | 거리 점수 + 카메라 방향 각도 점수 |
| `MovementDirection` | 거리 점수 + 플레이어 이동 방향 각도 점수 |

차폐 여부는 아직 후보 조건에 포함되지 않는다.

### 타겟 전환

`SwitchTarget(direction)`은 다음 순서로 동작한다.

1. `targetSwitchCooldown` 검사
2. `CollectTargets()`
3. `SortByScreenX()`
4. 현재 인덱스에 `direction`을 더한 뒤 `Mathf.Clamp`
5. 이전 타겟 `UnLockOn()`
6. 새 타겟 `LockOn()`

현재 전환은 화면 X 좌표 기준이며, 후보 끝에서 순환하지 않고 clamp된다.

### 락온 회전

`CameraLockOn.UpdateRotation()`은 현재 타겟을 향해 `yaw`와 `pitch`를 갱신한다.

핵심 처리:

| 항목 | 구현 |
|------|------|
| 타겟 유효성 | null, 거리, `IDamageable.IsAlive()` 검사 |
| 타겟 소실 | `TryFindNext()` 실패 시 `StartTransition()` |
| 포커스 위치 | 타겟 위치에서 Capsule 높이 일부를 빼고 `SmoothDamp` |
| yaw | 플레이어에서 타겟 포커스까지의 XZ 방향 + 오비탈 오프셋 |
| pitch | 고저차에 `lockOnHeightDampFactor` 적용 후 clamp |
| 오비탈 오프셋 | 거리별 커브, FOV 기반 안전 각도, free orbit factor, overcome sensitivity 사용 |

### 거리/FOV

`CameraDistanceController`는 락온 상태에서 다음 값을 반영한다.

| 기능 | 설정 필드 |
|------|-----------|
| 락온 FOV | `fovLockOn` |
| 락온 거리 | `lockOnDistance` |
| 다수 적 줌아웃 | `crowdZoomOutDistance`, `crowdDetectRadius`, `crowdEnemyThreshold` |
| 대형 몬스터 FOV/거리 확장 | `enableMonsterSizeFOV`, `monsterSizeFOVMax`, `monsterSizeDistanceMax` |
| 속도 기반 FOV | `enableSpeedFOV`, `speedFOVMax`, `speedForMaxFOV` |

락온 중에는 `EvaluateDistance()`가 `lockOnDistance`를 기본 목표로 삼고, 군중/대형 몬스터 조건이 있으면 더 먼 거리 후보를 허용한다.

### 전투 연동

락온 타겟은 플레이어 공격과 모션 워프에도 영향을 준다.

| 파일 | 연동 |
|------|------|
| `PlayerAttackState.FindHomingTarget()` | 락온 타겟이 `GetSnapSearchRange(true)` 안에 있으면 우선 사용 |
| `PlayerAttackState.UpdateRotation()` | 락온 타겟이 있으면 공격 중 타겟 방향으로 회전 |
| `IWarpTargetResolver.LockOnFirstResolver` | 락온 타겟을 워프 대상으로 반환 |
| `IWarpTargetResolver.HybridResolver` | 락온 타겟이 범위/각도 안이면 우선, 아니면 콘 최근접 대상 fallback |
| `CombatCameraDirector.PlaySoftTargetAssist()` | 락온이 없을 때만 약한 방향 보정 |

### UI 피드백

`MonsterActor.LockOn()`과 `MonsterActor.UnLockOn()`은 `_lockOnDecal`을 on/off 한다. 현재 락온 피드백은 몬스터 단위 데칼에 집중되어 있고, 화면 중앙 마커나 타겟 전환 방향 피드백은 별도 시스템으로 분리되어 있지 않다.

---

## 명조식 감각과 다른 점

### 1. 대상 중심 프레이밍보다 플레이어 피벗 회전이 강하다

현재 카메라는 기본적으로 플레이어 피벗을 따라가며, 락온 중 `yaw/pitch`를 대상 방향으로 맞춘다. 따라서 체감은 "대상 중심 전투 카메라"보다 "플레이어 추적 카메라가 대상 쪽으로 자동 회전"에 가깝다.

명조식 감각에 가까우려면 플레이어 위치만이 아니라 **플레이어-타겟 관계**를 프레이밍 기준으로 삼아야 한다. 단, 피벗을 타겟 쪽으로 과하게 당기면 플레이어가 화면에서 밀릴 수 있으므로 월드 거리 상한과 플레이어 viewport 보장이 필요하다.

### 2. 타겟 선정에 전투 맥락이 부족하다

현재 점수는 거리와 기준 방향이 중심이다.

명조식 전투 체감에서 중요한 후보 정보:

| 정보 | 현재 상태 |
|------|-----------|
| 화면 중앙성 | 일부 반영. viewport 밖 페널티만 있음 |
| 플레이어 이동/입력 방향 | `MovementDirection` 모드에서 반영 |
| 최근 공격자 | 미반영 |
| 적 위협도 | 미반영 |
| 보스/정예 우선 | 미반영 |
| 차폐/가시성 | 미반영 |
| 현재 전투 타겟 유지 보너스 | 명시적 히스테리시스 없음 |

### 3. 차폐 대상도 후보가 될 수 있다

`CollectTargets()`는 viewport 밖 여부만 점수에 반영하고, 벽/지형 뒤 대상은 별도로 제외하지 않는다. 실제 플레이에서 벽 너머 적이 잡히거나, 카메라는 타겟을 보려 하지만 플레이어는 전투 정보를 읽기 어려운 상황이 생길 수 있다.

### 4. 타겟 전환이 화면 X 정렬 기반으로 단순하다

현재 전환은 후보를 화면 X 좌표로 정렬한 뒤 인덱스를 증감한다. 입력 방향의 공간적 의미, 현재 타겟 기준 좌/우 각도, 화면 중앙 거리, 차폐 여부는 전환 시 재평가되지 않는다.

끝 후보에서는 clamp되므로 여러 적 사이를 빠르게 순환하는 조작감도 제한된다.

### 5. 락온 중 수동 카메라 입력이 거의 막힌다

`InGameCameraMode.HandleInput()`은 락온 중 수동 Look 입력을 회전에 반영하지 않는다. 이 방식은 안정적이지만, 빠른 액션 전투에서는 플레이어가 "조금 더 보고 싶은 방향"을 카메라에 전달하기 어렵다.

명조식 목표는 자동 보정을 제거하는 것이 아니라, 자동 보정 강도와 수동 입력 존중을 분리하는 것이다.

### 6. 락온 포인트가 단일 Transform에 묶여 있다

현재 `CurrentTarget`은 `Transform`이고, 포커스 높이는 `CapsuleCollider.height` 일부로 보정한다. 대형 몬스터, 공중 몬스터, 약점 부위가 있는 보스에서는 단일 Transform 기준 포커스가 부족하다.

### 7. 락온 UI 피드백이 약하다

현재 `_lockOnDecal`은 대상 선택 상태를 보여주지만, 화면 중심 마커, 전환 방향, 대상 소실/재획득, 보스 부위 선택 같은 전투 UI 피드백은 분리되어 있지 않다.

---

## 개선 우선순위

### P0 — 차폐/가시성 검증 추가

가장 먼저 `CollectTargets()`에 가시성 검증을 넣는다.

권장 정책:

| 상황 | 처리 |
|------|------|
| 플레이어 또는 카메라에서 타겟 포커스까지 완전 차폐 | 후보 제외 |
| 얇은 장애물/부분 차폐 | 점수 페널티 |
| 현재 타겟이 짧게 차폐됨 | 즉시 해제하지 않고 유지 시간 허용 |
| 장시간 차폐 | 다음 후보 탐색 또는 전환 연출 |

구현 위치:

```text
CameraLockOn.CollectTargets()
CameraLockOn.IsValidTarget()
```

새 설정 후보:

| 필드 | 용도 |
|------|------|
| `lockOnRequireLineOfSight` | 신규 타겟 획득 시 가시성 필수 여부 |
| `lockOnOcclusionGraceTime` | 현재 타겟이 차폐되어도 유지하는 시간 |
| `lockOnOcclusionPenalty` | 부분 차폐 후보 점수 페널티 |
| `lockOnLineOfSightRadius` | Linecast 대신 SphereCast를 쓸 때 반지름 |

### P1 — 타겟 점수 모델 분리

`CameraLockOn`은 현재 탐색, 정렬, 상태, 회전, 전환 연출을 모두 담당한다. 우선 점수 계산을 분리하면 명조식 타겟 정책을 실험하기 쉬워진다.

권장 타입:

```csharp
public readonly struct LockOnCandidate
{
    public readonly Transform Target;
    public readonly float Distance;
    public readonly float ScreenCenterDistance;
    public readonly float DirectionAngle;
    public readonly bool IsVisible;
    public readonly bool IsCurrentTarget;
}
```

```csharp
public sealed class LockOnTargetScorer
{
    public float Evaluate(in LockOnCandidate candidate, in LockOnScoreContext context);
}
```

초기 점수 요소:

| 요소 | 목적 |
|------|------|
| 거리 | 너무 먼 대상보다 가까운 전투 대상 우선 |
| 화면 중앙성 | 플레이어가 보고 있는 대상 우선 |
| 입력/이동 방향 | 조작 의도 반영 |
| 현재 타겟 유지 보너스 | 잦은 타겟 흔들림 방지 |
| 차폐 페널티 | 벽 너머 대상 방지 |
| 최근 공격자 보너스 | 위협 대응성 증가 |
| 몬스터 등급 보너스 | 보스/정예 우선 |

### P2 — 획득/유지 거리 히스테리시스

현재 `lockOnRange` 하나가 획득과 유지에 같이 쓰인다. 빠른 대시, 넉백, 보스 이동에서 락온이 쉽게 끊길 수 있다.

권장 분리:

| 필드 | 예시 | 설명 |
|------|------|------|
| `lockOnAcquireRange` | 16m | 새 타겟 획득 거리 |
| `lockOnReleaseRange` | 20m | 현재 타겟 유지 한계 |
| `lockOnLostGraceTime` | 0.5s | 사거리/시야 이탈 후 유지 시간 |
| `lockOnRetargetOnLost` | true | 소실 시 다음 타겟 자동 탐색 |

유지 정책은 신규 획득보다 관대해야 한다. 그래야 전투 중 카메라가 불필요하게 풀리지 않는다.

### P3 — 타겟 전환 로직 개선

`SortByScreenX()` 기반 인덱스 전환을 공간 후보 선택으로 바꾼다.

권장 방식:

1. 현재 타겟의 viewport 위치를 구한다.
2. 입력 방향이 오른쪽이면 현재 타겟보다 화면 오른쪽에 있는 후보만 필터링한다.
3. 후보마다 화면 X 거리, 화면 중앙 거리, 가시성, 거리 점수를 합산한다.
4. 후보가 없으면 wrap 또는 화면 중앙 후보 fallback을 사용한다.

새 설정 후보:

| 필드 | 용도 |
|------|------|
| `lockOnSwitchWrap` | 끝 후보에서 반대편 후보로 순환 |
| `lockOnSwitchScreenWeight` | 화면 좌/우 전환 감도 |
| `lockOnSwitchDistanceWeight` | 너무 먼 후보 억제 |
| `lockOnSwitchRequireVisible` | 전환 대상 가시성 요구 |

### P4 — 플레이어-타겟 쌍 프레이밍

락온 중에는 카메라 피벗을 플레이어 위치만으로 두지 않고, 플레이어와 타겟 사이의 전투 공간을 일부 반영한다.

주의할 점:

- 비율 기반으로 타겟 쪽 포커스를 당기면 원거리에서 월드 이동량이 과도해질 수 있다.
- 포커스 전진량은 반드시 월드 거리 상한을 둔다.
- 최종 카메라 위치에서 플레이어가 viewport 안에 남는지 검증해야 한다.

권장 계산:

```csharp
Vector3 toTarget = targetFocus - playerPosition;
float targetDistance = toTarget.magnitude;
Vector3 targetDir = targetDistance > 0.001f ? toTarget / targetDistance : playerForward;

float desiredFocusOffset = targetDistance * focusRatio;
float focusOffset = Mathf.Min(desiredFocusOffset, maxFocusOffsetFromPlayer);
Vector3 pivotBase = playerPosition + targetDir * focusOffset + cameraOffset;
```

필수 안전장치:

| 안전장치 | 목적 |
|----------|------|
| `maxFocusOffsetFromPlayer` | 원거리 타겟이 피벗을 과도하게 당기는 문제 방지 |
| `minPlayerViewportMargin` | 플레이어가 화면 밖으로 밀리는 문제 방지 |
| `focusReturnSmoothTime` | 타겟 소실/락온 해제 시 피벗 튐 완화 |

### P5 — `ILockOnTarget` 도입

`IDamageable`만으로는 락온 포커스와 UI 앵커, 전투 우선순위를 표현하기 어렵다.

권장 인터페이스:

```csharp
public interface ILockOnTarget
{
    Transform Transform { get; }
    Vector3 FocusPosition { get; }
    Vector3 UIAnchorPosition { get; }
    bool CanLockOn { get; }
    float LockOnPriority { get; }
    float BoundsSize { get; }
}
```

적용 방향:

| 대상 | 구현 |
|------|------|
| 일반 몬스터 | `MonsterActor` 또는 별도 컴포넌트가 중심 포커스 제공 |
| 대형 몬스터 | 머리/몸통/약점 등 복수 포인트 제공 |
| 공중 몬스터 | Capsule 높이 대신 소켓/센터 포인트 사용 |
| 보스 | 부위별 UI 앵커와 우선순위 제공 |

마이그레이션은 `IDamageable` fallback을 유지한 채 점진 적용한다.

### P6 — 소프트락과 하드락 분리

현재 비락온 보정은 `CombatCameraDirector.PlaySoftTargetAssist()`에 일부 존재한다. 명조식 체감에 가까워지려면 다음 역할을 분리한다.

| 구분 | 역할 |
|------|------|
| 소프트락 | 공격/스킬/회피 카운터 시 잠깐 대상 방향을 읽기 좋게 보정 |
| 하드락 | 명시적 입력으로 대상 고정, 전환, UI 표시 |
| 모션 워프 타겟 | 공격 데이터와 범위/각도 조건에 따라 별도로 확정 |
| 카메라 포커스 | 전투 가독성을 위한 프레이밍 대상 |

하드락 타겟과 공격 타겟이 항상 같을 필요는 없다. 다만 서로 다를 때는 UI/카메라/모션 워프가 충돌하지 않도록 우선순위를 명확히 해야 한다.

---

## 권장 구현 순서

| 순서 | 작업 | 기대 효과 |
|------|------|-----------|
| 1 | 차폐/가시성 검증 | 벽 너머 락온 방지, 타겟 신뢰도 상승 |
| 2 | 점수 모델 분리 | 타겟 정책 튜닝과 테스트가 쉬워짐 |
| 3 | 획득/유지 히스테리시스 | 빠른 전투에서 락온 끊김 감소 |
| 4 | 전환 로직 개선 | 좌/우 전환 조작감 개선 |
| 5 | 쌍 프레이밍 | 플레이어-적 전투 공간 가독성 향상 |
| 6 | `ILockOnTarget` | 대형/공중/보스 몬스터 대응 |
| 7 | 소프트락/하드락 분리 | 명조식 자동 보정 감각에 접근 |

---

## 테스트 체크리스트

### 타겟 획득

| 체크 | 기대 결과 |
|------|-----------|
| 정면 가까운 적 1명 | 즉시 락온 |
| 후방 가까운 적, 정면 먼 적 | `lockOnPriorityMode`에 맞게 선택 |
| 벽 뒤 적 | 차폐 검증 도입 후 신규 획득 제외 |
| 사망한 적 | 후보 제외 |
| 플레이어 자식 콜라이더 | 후보 제외 |

### 타겟 유지/해제

| 체크 | 기대 결과 |
|------|-----------|
| 타겟이 획득 거리 밖으로 짧게 이동 | 유지 grace 동안 유지 |
| 타겟이 release 거리 밖으로 이탈 | 다음 후보 탐색 또는 해제 전환 |
| 타겟 사망 | 다음 후보 자동 탐색, 없으면 해제 |
| 대화/프리카메라/스냅샷 모드 진입 | `Release()`로 정리 |

### 전환

| 체크 | 기대 결과 |
|------|-----------|
| 화면 좌/우에 적 3명 | 입력 방향에 맞는 후보 선택 |
| 끝 후보에서 추가 전환 | 정책에 따라 wrap 또는 유지 |
| 차폐 후보가 좌/우에 있음 | 제외 또는 큰 페널티 |
| 전환 연타 | `targetSwitchCooldown` 적용 |

### 프레이밍

| 체크 | 기대 결과 |
|------|-----------|
| 근거리 소형 적 | 플레이어와 적이 모두 안정적으로 보임 |
| 원거리 적 | 피벗이 타겟 쪽으로 과도하게 이동하지 않음 |
| 대형 몬스터 | FOV/거리 확장과 포커스 포인트가 자연스럽게 동작 |
| 벽 근처 락온 | 충돌 보정 후에도 플레이어가 화면 안에 남음 |

### 전투 연동

| 체크 | 기대 결과 |
|------|-----------|
| 락온 중 일반 공격 | 락온 타겟이 호밍/회전 우선 대상 |
| 락온 타겟이 공격 범위 밖 | 공격 데이터 정책에 따라 fallback |
| 비락온 회피 카운터 | 소프트 타겟 보정이 짧게 작동 |
| 최근 수동 카메라 입력 직후 | 자동 보정 강도 감소 |

---

## 주의 사항

- 락온 후보 평가 함수는 side effect가 없어야 한다. 후보 평가 중 `LockOn()`, `UnLockOn()`, UI on/off, 디졸브 이벤트 같은 처리가 발생하면 전환 흔들림과 디버깅 난도가 커진다.
- `CurrentTarget`이 `Transform`만 들고 있으면 대형 몬스터 대응이 제한된다. `IDamageable` fallback은 유지하되, 포커스/앵커/우선순위는 별도 인터페이스로 확장하는 편이 안전하다.
- 피벗을 플레이어-타겟 중간으로 옮길 때는 비율만 사용하지 않는다. 원거리에서 `targetDistance * ratio`가 카메라 거리보다 커지면 플레이어가 화면 밖으로 밀릴 수 있다.
- 락온 중 수동 카메라 입력을 완전히 허용하면 카메라와 타겟 회전이 서로 싸울 수 있다. 먼저 자동 보정 가중치와 최근 수동 입력 감쇠를 분리하는 방식이 좋다.
- 차폐 검증은 너무 엄격하면 나무, 얇은 기둥, 이펙트용 콜라이더 때문에 락온이 자주 끊길 수 있다. 신규 획득과 현재 타겟 유지 조건을 다르게 둔다.

---

## 관련 문서

| 문서 | 관계 |
|------|------|
| `Assets/docs/Complete/CAMERA_SYSTEM_GUIDE.md` | 현재 카메라 시스템 전체 구조 |
| `Assets/docs/Complete/CAMERA_MODE_ARCHITECTURE_DESIGN.md` | 카메라 모드 분리 구조 |
| `Assets/docs/Complete/CAMERA_SNAPSHOT_SEQUENCE_GUIDE.md` | 스냅샷 기반 연출 카메라 |
| `Assets/docs/TODO/COMBAT_CAMERA_SYSTEM_IMPROVEMENT_PLAN.md` | 명조식 전투 카메라 구조 개선 계획 |
| `Assets/docs/CODEX_REVIEW_CAMERA.md` | 카메라 계산/충돌/락온 후보 평가 관련 코드 리뷰 |
