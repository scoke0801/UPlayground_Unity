# GameCameraCalculator.cs 독립 코드 리뷰

대상 파일: `unsave/GameCameraCalculator.cs`

작성일: 2026-05-25

## 검토 기준

이 문서는 현재 Unity 프로젝트의 카메라 시스템과 비교하지 않고, `GameCameraCalculator.cs` 자체를 독립 코드로 분석한다. 중점은 다음이다.

- 계산 로직이 기하학적으로 맞는가
- `Physics` cast 방향, 거리, 반지름 적용이 일관적인가
- edge case에서 잘못된 위치를 만들 가능성이 있는가
- 코드 크기와 책임 분리가 유지보수 가능한 수준인가

## 결론 요약

이 코드는 의도는 분명하지만, 그대로 제품 코드에 넣기에는 위험한 계산 문제가 여럿 있다. 특히 단일 `SphereCast` 경로의 방향 계산, MultiProbe의 probe 기하, floor rescue의 y 좌표 계산, 레이어 마스크 생성 방식은 실제 카메라 튐/뚫림/과도한 당김으로 이어질 수 있다.

가장 큰 구조적 문제는 한 정적 클래스가 너무 많은 책임을 가진다는 점이다. 현재 파일은 프러스텀 판정, 충돌 거리 계산, 바닥 보정, Terrain 보정, 캐릭터 디졸브 이벤트까지 모두 처리한다. 이 때문에 각 보정의 기준이 서로 달라지고, 하나를 고치면 다른 경로가 다른 결과를 내기 쉽다.

## CLAUDE_REVIEW_CAMERA.md 교차 검토 반영

`Assets/docs/Complete/CLAUDE_REVIEW_CAMERA.md`를 대조한 결과, 다음 항목은 타당하므로 이 문서의 판단에 반영한다.

### 수용한 지적

1. **`GetCameraColliderPos`의 핵심 문제는 산술 부호 자체보다 기준축 선택 오류다.**
   `Dot`, `SphereCast`, `tpos + dir * distance`라는 계산 형태는 수식으로는 일관된다. 문제는 `dir`과 `distance`를 실제 위치축 `(cpos - tpos)`가 아니라 카메라 회전축 `-(curRot * Vector3.forward)`에서 가져온다는 점이다. 따라서 이 버그는 “벡터 산술 오타”라기보다 “충돌 경로의 기준축을 잘못 선택한 로직 오류”로 보는 것이 더 정확하다.

2. **레이어 마스크 오염 설명은 정확하다.**
   `LayerMask.NameToLayer` 실패값 `-1`은 C# shift count 마스킹 때문에 예외 없이 `1 << 31` 계열의 비트로 변질될 수 있다. 이 문제는 단순 null/0 문제가 아니라 “잘못된 레이어 비트가 조용히 켜지는” 문제다.

3. **`GetTerrainPos`의 높이 공식 자체는 맞지만 Terrain 선택 기준이 위험하다.**
   `terrain.GetPosition().y + terrain.SampleHeight(cpos)`는 선택된 Terrain에 대해서는 월드 높이를 얻는 공식으로 볼 수 있다. 다만 Terrain을 `tpos`로 고르고 샘플은 `cpos`로 하기 때문에 멀티 Terrain에서 타일 불일치가 발생한다. 이 문서의 기존 판단을 “공식 오류”가 아니라 “Terrain 선택 로직 오류”로 해석하는 것이 더 정확하다.

4. **floor rescue가 `CamColliderHit`를 켜는 의미론적 문제는 중요하다.**
   `moved |= EnsureCameraNotBelowFloor(...)` 때문에 벽 충돌이 아닌 수직 바닥 구제도 `CamColliderHit`로 합쳐질 수 있다. 호출부에서 `CamColliderHit`를 줌 인터럽트나 충돌 상태로 해석하면, 바닥 보정만으로 줌 복구/중단 상태가 바뀔 수 있다. 충돌 플래그와 바닥 구제 플래그는 분리해야 한다.

5. **skin push 후 재검증 부재는 실제 코너 매립 위험이다.**
   `cldPos += hitNormal * clampedSkin` 뒤에 `CheckSphere`나 재 probe가 없다. 좁은 코너에서는 A 벽 normal로 민 결과가 B 벽 내부로 들어갈 수 있다. `colliderRadius * 0.5f` clamp는 위험을 줄일 뿐 보장하지 않는다.

6. **`SRControllableCameraBase.UpdateCamProperty`가 단일 SphereCast 경로를 실제로 사용한다.**
   `unsave/SRControllableCameraBase.cs`의 `UpdateCamProperty`는 `GameCameraCalculator.ProcessColliderRevise(...)`를 호출한다. 따라서 `GetCameraColliderPos`의 회전축 기반 오류는 참고 코드 내부에서 실제 통합 경로에 연결되어 있다. MultiProbe 경로가 더 안정적인 의도라면, 이 호출부가 아직 옛 경로를 쓰는 것이 별도 리스크다.

7. **`nextZoomRate` 미클램프와 `LerpUnclamped` 전파는 타당한 지적이다.**
   `nextZoomRate = (newcamLength - min) / (max - min)` 결과가 [0, 1]로 clamp되지 않고, 이후 `_zoomRate`와 `UpdateCameraTfm(float zoomRate, ...)`에 전달된다. float 인자 오버로드는 내부에서 clamp하지 않고 `CalcCameraTfm(..., zoomRate)`로 넘긴다. 충돌 길이가 min보다 짧거나 max보다 길게 계산되면 줌 레이트가 범위 밖으로 흐를 수 있다.

8. **줌 블렌딩의 프레임률 의존성은 맞다.**
   `_zoomRate = Mathf.Lerp(_zoomRate, nextZoomRate, lerpVal * Time.deltaTime)`와 복구부의 `Mathf.Lerp(_zoomRate, _zoomRateOrigin, Time.deltaTime * 2f)`는 `k * deltaTime` 방식의 반복 Lerp다. `Mathf.Lerp`는 t를 clamp하므로 오버슈트보다는 저 FPS에서 목표로 급격히 붙는 스냅 성향이 문제다. 회전 쪽은 `SmoothDampAngle(..., Time.deltaTime)`이므로 두 보간 정책이 다르다.

9. **`ZoomCollisionState`와 기존 zoomRate-Lerp 모델이 공존한다.**
   `ZoomCollisionState.cs`는 length 도메인 SmoothDamp 모델을 설명하지만, `SRControllableCameraBase.UpdateCamProperty`는 zoomRate 기반 Lerp 모델을 쓴다. 둘 중 어느 쪽이 최신 경로인지 명확하지 않으면 충돌 복구 정책이 이중화된다.

10. **`CheckIsStopX/Y`가 속도만 보고 회전 종료를 판단하는 문제는 타당하다.**
    `Mathf.Abs(_rotationVelocityX/Y) < 0.1f`만 보고 멈춘다. 목표 각도와 현재 각도의 잔여 오차를 보지 않으므로, 속도가 임계 아래로 떨어진 순간 목표에 덜 도달했어도 회전 상태를 끌 수 있다.

11. **`LockOnState`는 상태 구조체가 과밀하다.**
    로직은 없지만 `TargetPosVelocity`, `BlendTVelocity`, `OrbitPitchVelocity`, `OffsetAngleVelocity`, `FreeFactorVelocity`, `InitialReleaseFactorVelocity`, `ActiveFocusPosVelocity`, `ActiveFocusRatioVelocity` 등 독립 smoothing 채널이 많다. 락온 카메라의 side flip, focus, free orbit, transition 상태를 하위 구조체로 나누는 것이 추적성에 좋다.

### 조건부 또는 미수용한 지적

1. **“산술 계산이 전부 정확하다”는 결론은 너무 강하다.**
   `safeT = -radius / axisDir.y`가 “pivot.y - radius 지점으로 후퇴”한다는 산술 자체는 맞다. 그러나 floor rescue의 목적이 ground 위로 끌어올리는 것이라면 기준 y가 `pivot.y - radius`인 것은 의미론적으로 틀릴 수 있다. 특히 `cldCamPos.y >= groundY - radius` 판정은 구체가 ground를 관통해도 통과시키므로, 이 문서는 여전히 충돌 의미 기준의 오류로 본다.

2. **`ContainsFrustum`의 near/far clip 미반영은 용도에 따라 무해할 수 있지만, 락온 후보 필터라면 위험하다.**
   화면 방향성만 볼 목적이면 `z > 0`과 x/y 범위만으로 충분할 수 있다. 그러나 함수명이 frustum 포함 여부를 말하고, 락온 후보/가시성 필터에 쓰인다면 near/far clip과 bounds 판정을 추가해야 한다.

3. **MultiProbe 투영 최소값은 수학 형태만 보면 일관되지만, probe 기하 자체가 부정확하다.**
   `Dot(hit.point - pivot, axisDir)`로 축 투영 거리를 구하는 방식은 계산 형태로는 맞다. 하지만 ring probe가 `pivot -> desired + offset`으로 나가는 순간 cast 방향과 projection 축이 달라진다. 따라서 “투영 수식이 맞다”와 “카메라 반지름 충돌 모델로 맞다”는 별개의 문제다. 이 문서는 후자를 문제로 본다.

4. **락온 로직 전체는 제공된 파일에 없다.**
   `LockOnState.cs`는 상태 구조체이고 실제 sign/free-factor/side-flip 로직은 없다. 따라서 락온 관련 판단은 `ProbeCameraReachMultiProbe`가 락온 sign 비교에 쓰인다는 주석과 상태 구조를 기반으로 한 위험 분석이다. 실제 최종 결론은 소비 로직까지 봐야 확정된다.

## 계산상 주요 오류 또는 위험

### 1. 단일 SphereCast 방향이 실제 카메라 위치 방향과 다를 수 있음

문제 코드:

```csharp
var camFoward = curRot * Vector3.forward;
var targetToCam = -camFoward.normalized;
var targetHitLength = Vector3.Dot((tpos - cpos), camFoward);

bool isHit = Physics.SphereCast(tpos, colliderRadius, targetToCam.normalized, out var hitRay, targetHitLength, _camCldLayer);
```

이 계산은 `curRot.forward`의 반대 방향이 항상 `target -> camera` 방향이라고 가정한다. 하지만 실제 카메라 후보 위치 `cpos`가 회전값과 완전히 일치하지 않으면 cast 방향이 틀어진다. 카메라 회전 보간, 위치 보간, shoulder offset, lock-on offset 같은 상황에서는 `-curRot.forward`와 `(cpos - tpos).normalized`가 달라질 수 있다.

결과:

- 실제 후보 카메라 위치와 다른 방향으로 cast한다.
- 벽이 실제 경로에 있어도 miss할 수 있다.
- 반대로 실제 경로에는 없는 장애물에 hit할 수 있다.
- 보정 위치가 `tpos + hitRay.distance * targetToCam`로 계산되어 후보 축에서 벗어난다.

권장 수정:

```csharp
Vector3 targetToCamera = cpos - tpos;
float distance = targetToCamera.magnitude;
if (distance < EPSILON)
{
    cldCamPos = cpos;
    return false;
}

Vector3 dir = targetToCamera / distance;
bool isHit = Physics.SphereCast(tpos, colliderRadius, dir, out RaycastHit hit, distance, _camCldLayer);
```

### 2. `targetHitLength`가 음수 또는 실제 거리와 다른 값이 될 수 있음

`targetHitLength = Vector3.Dot((tpos - cpos), camFoward)`는 두 벡터가 같은 방향이라는 가정 아래에서만 실제 거리와 같다. 방향이 조금만 어긋나도 실제 거리보다 짧아지고, 90도 이상 어긋나면 음수가 된다.

Unity `Physics.SphereCast`의 `maxDistance`에 음수 또는 부정확한 값이 들어가면 의도한 경로 검사가 되지 않는다. 방어 코드도 없다.

권장 수정:

- cast 길이는 항상 `Vector3.Distance(tpos, cpos)`로 계산한다.
- `distance <= 0`이면 cast하지 않는다.
- 회전값은 충돌 계산의 입력이 아니라 최종 카메라 pose 계산에서만 사용한다.

### 3. MultiProbe ring LineCast가 카메라 반지름 튜브 검사가 아님

문제 코드:

```csharp
Vector3 offset = (right * Mathf.Cos(a) + up * Mathf.Sin(a)) * colliderRadius;
Vector3 endpoint = cpos + offset;
Physics.Linecast(tpos, endpoint, out RaycastHit hit, _camCldLayer)
```

이 방식은 모든 ring probe가 같은 시작점 `tpos`에서 출발한다. 결과적으로 피벗에서 여러 endpoint로 퍼지는 부채꼴 검사가 된다. 카메라 반지름을 가진 원통형/캡슐형 공간을 검사하려는 목적이라면 시작점도 offset되어야 한다.

예상한 볼륨:

```csharp
Linecast(tpos + offset, cpos + offset)
```

현재 볼륨:

```csharp
Linecast(tpos, cpos + offset)
```

결과:

- 피벗 근처에서는 모든 probe가 하나로 모여 반지름 검사가 사실상 사라진다.
- 카메라 근처에서는 probe가 벌어져 실제보다 넓은 부채꼴을 검사한다.
- 벽 모서리, 좁은 기둥, 문틀에서 hit projection이 불안정해질 수 있다.
- `SphereCast`를 대체하기 위한 안정화 목적과 맞지 않는다.

권장 수정:

```csharp
Vector3 start = tpos + offset;
Vector3 end = cpos + offset;
if (Physics.Linecast(start, end, out RaycastHit hit, _camCldLayer))
{
    float projLen = Vector3.Dot(hit.point - tpos, axisDir);
}
```

단, 피벗 주변 카메라 충돌을 보호해야 한다면 center probe와 별도의 near-pivot 예외 처리가 필요하다. 모든 ring probe를 피벗에서 시작시키는 것은 반지름 모델로는 부정확하다.

### 4. MultiProbe에서 skinWidth 적용 방향이 불안정함

문제 코드:

```csharp
cldPos = tpos + axisDir * bestProjLen;
...
cldPos += hitNormal * clampedSkin;
```

hit point를 축으로 projection한 뒤, normal 방향으로 월드 위치를 직접 민다. 이 방식은 벽에서 카메라를 띄우는 데 도움이 될 수 있지만, 항상 안전하지는 않다.

문제 상황:

- hit normal이 카메라 축과 크게 비스듬하면 카메라가 축에서 옆으로 밀린다.
- 옆으로 민 결과 다른 벽이나 바닥 안으로 들어갈 수 있다.
- 카메라 암 길이 기반 스무딩을 별도로 한다면, 보정된 위치와 암 길이가 불일치한다.
- `minNormalAlignment`가 낮게 설정되면 거의 측면 normal에도 skin이 적용될 수 있다.

권장 수정:

- 카메라를 축 위에 유지하려면 `bestProjLen -= skinWidth`가 더 단순하고 예측 가능하다.
- normal 방향 skin을 유지하려면 이동 후 `CheckSphere`로 최종 위치를 다시 검증해야 한다.
- `minNormalAlignment`는 skin 적용 조건뿐 아니라 hit 채택 조건에도 써야 한다.

### 5. MultiProbe hit 채택에 normal alignment가 반영되지 않음

`ProcessColliderReviseMultiProbe`에는 `minNormalAlignment` 파라미터가 있지만, 실제 hit를 고르는 `GetCameraColliderPosMultiProbe`에는 전달되지 않는다. 현재는 모든 LineCast hit가 거리 제한 후보가 된다.

결과:

- 거의 평행한 표면에 살짝 닿은 hit도 카메라를 당길 수 있다.
- 바닥 모서리나 벽 뒷면 hit가 과도하게 반영될 수 있다.
- 파라미터 이름과 실제 동작이 다르다.

권장 수정:

```csharp
float alignment = Vector3.Dot(hit.normal, -axisDir);
if (alignment < minNormalAlignment)
    return;
```

이 조건을 hit 후보 채택 시점에 적용한다.

### 6. `LayerMask.NameToLayer` 실패 시 잘못된 마스크 생성 가능

문제 코드:

```csharp
1 << LayerMask.NameToLayer("CameraCollider")
```

`NameToLayer`가 실패하면 `-1`을 반환한다. C# shift 연산은 shift count를 하위 비트 기준으로 처리하므로 `1 << -1`은 직관적인 실패가 아니라 큰 비트값으로 평가될 수 있다. 즉 레이어명이 잘못되어도 조용히 이상한 마스크가 만들어진다.

권장 수정:

```csharp
private static readonly int CamCollisionMask = LayerMask.GetMask(
    "CameraCollider",
    "WalkableGround",
    "Obstacle");
```

그리고 `CamCollisionMask == 0`이면 명시적으로 오류를 남긴다.

### 7. ActorController가 없으면 물리 충돌 보정까지 중단됨

문제 코드:

```csharp
if (Application.isEditor == false && checkNoActors)
    return false;
```

`checkNoActors`는 캐릭터 디졸브 처리 가능 여부와 관련된 조건이다. 그런데 이 조건으로 카메라 벽/지형 충돌 보정까지 중단한다. 캐릭터 목록이 없거나 초기화 전인 상태에서도 카메라 충돌은 독립적으로 동작해야 한다.

결과:

- 액터가 없는 씬에서 카메라가 벽/지형을 뚫을 수 있다.
- 초기화 순서에 따라 특정 프레임만 충돌 보정이 빠질 수 있다.
- 디졸브 기능의 의존성이 충돌 계산의 안정성을 해친다.

권장 수정:

- 물리 충돌 계산은 항상 수행한다.
- `checkNoActors`는 디졸브 루프 진입 여부에만 사용한다.

### 8. `SRGameManager.Instance.ActorController` null 접근 가능성

문제 코드:

```csharp
var checkNoActors = SRGameManager.IsValidActorController == false ||
                    SRGameManager.Instance.ActorController.GameActorList == null ||
                    SRGameManager.Instance.ActorController.GameActorList.Count <= 0;
```

`||`는 단락 평가를 하지만, `SRGameManager.IsValidActorController == false`가 `false`인 순간 뒤쪽 접근이 실행된다. 이때 `SRGameManager.Instance` 또는 `ActorController` 자체가 null이 아니라고 보장하는지는 외부 구현에 의존한다.

권장 수정:

```csharp
var actorController = SRGameManager.Instance != null ? SRGameManager.Instance.ActorController : null;
var actorList = actorController != null ? actorController.GameActorList : null;
bool hasActors = actorList != null && actorList.Count > 0;
```

더 좋은 방향은 이 클래스가 `SRGameManager`를 직접 알지 않도록 디졸브 처리 자체를 밖으로 빼는 것이다.

### 9. `EnsureCameraNotBelowFloor`의 safeT 계산이 주석과 맞지 않음

문제 코드:

```csharp
float safeT = -colliderRadius / axisDir.y;
cldCamPos = tpos + axisDir * safeT;
```

주석은 “카메라 y가 pivot 높이 근처가 되도록” 또는 “ground 아래로 빠진 경우 보정”이라고 설명하지만, 실제 계산은 `cldCamPos.y = tpos.y - colliderRadius`가 되도록 축 위의 위치를 구한다.

즉 ground height를 써서 safe y를 계산하지 않는다. `groundHit`은 빠짐 여부 판단에만 쓰이고, 최종 y 계산에는 반영되지 않는다.

문제 상황:

- 피벗이 지면보다 훨씬 위에 있으면 카메라를 과도하게 끌어올린다.
- 경사면/계단에서 ground 기준 보정이 아니라 pivot 기준 보정이 된다.
- `axisDir.y`가 0에 가까우면 `safeT`가 매우 커진다.

권장 수정:

```csharp
float safeY = groundHit.point.y + colliderRadius;
float denominator = axisDir.y;
if (Mathf.Abs(denominator) < EPSILON)
    return false;

float safeT = (safeY - tpos.y) / denominator;
safeT = Mathf.Clamp(safeT, 0f, axisLen);
cldCamPos = tpos + axisDir * safeT;
```

### 10. `EnsureGroundClearance` fallback이 잘못된 지면을 기준으로 삼을 수 있음

카메라 아래 raycast가 실패하면 피벗 아래 지면을 fallback으로 사용한다.

```csharp
if (Physics.Raycast(tpos, Vector3.down, out var pivotGround, pivotProbeDist, _camCldLayer))
{
    groundHit = pivotGround;
    found = true;
}
```

이 방식은 카메라가 절벽 밖, 다리 밖, 지형이 끊긴 곳에 있을 때 플레이어 발밑 지면 높이로 카메라를 끌어올릴 수 있다. “카메라 near-plane이 바닥을 클립하는지”를 검사하려면 카메라 주변 지면이 기준이어야 한다.

권장 수정:

- 카메라 주변 ground probe를 우선하고, 피벗 fallback은 “카메라가 피벗보다 비정상적으로 낮을 때” 같은 별도 rescue 조건에서만 사용한다.
- floor 전용 레이어와 obstacle 레이어를 분리한다.

### 11. `GetTerrainPos`가 피벗 Terrain과 카메라 위치 샘플을 섞음

문제 코드:

```csharp
Terrain terrain = GrTerrainManager.Instance?.GetTerrain(tpos);
float terrainPosY = terrain.GetPosition().y + terrain.SampleHeight(cpos) + radius;
```

Terrain은 `tpos` 기준으로 찾고, 높이는 `cpos`로 샘플링한다. 피벗과 카메라가 서로 다른 Terrain 타일 위에 있으면 잘못된 Terrain에서 카메라 위치를 샘플링한다.

또한 `radius = 0.5f`가 하드코딩되어 있다. 다른 함수는 `colliderRadius`를 인자로 받는데 이 함수만 별도 고정값을 쓴다.

권장 수정:

- Terrain은 `cpos` 기준으로 찾는다.
- radius를 인자로 받는다.
- MeshCollider 기반 지형과 Terrain 기반 지형의 보정 경로를 통합한다.

### 12. `ContainsFrustum`은 far/near clip을 정확히 보지 않음

문제 코드:

```csharp
return viewport.z > 0f &&
       viewport.x >= 0f && viewport.x <= 1f &&
       viewport.y >= 0f && viewport.y <= 1f;
```

`WorldToViewportPoint`의 `z`는 카메라 공간 거리다. `z > 0`만으로는 far clip 밖의 점을 걸러내지 못한다. near clip보다 가까운 점도 일부 목적에서는 제외해야 한다.

권장 수정:

```csharp
return viewport.z >= camera.nearClipPlane &&
       viewport.z <= camera.farClipPlane &&
       viewport.x >= 0f && viewport.x <= 1f &&
       viewport.y >= 0f && viewport.y <= 1f;
```

단, “화면 방향에 있는가”만 필요한 함수라면 이름을 `IsInViewportDirection`처럼 바꾸는 것이 맞다.

## 함수별 상세 분석

### 정적 필드

역할:

- 충돌 레이어와 디졸브 레이어를 정적 값으로 캐싱한다.

문제점:

- 레이어 이름 실패를 검증하지 않는다.
- 코드 로드 시점에 고정되므로 런타임 설정 교체가 어렵다.
- `int` 필드명 앞 `_`를 붙였지만 상수처럼 쓰인다.

개선점:

- `readonly LayerMask` 또는 설정 주입으로 바꾼다.
- 레이어 생성은 helper 함수에서 검증한다.

### `ContainsFrustum`

역할:

- 특정 월드 좌표가 viewport 사각형 안에 있는지 검사한다.

문제점:

- null camera 방어 없음.
- far/near clip 누락.
- 점 판정이라 object bounds 판정으로 오해하면 안 된다.
- 폐기된 주석 코드가 너무 길다.

개선점:

- 용도에 맞춰 함수명을 명확히 한다.
- 바운드 판정이 필요하면 `GeometryUtility.TestPlanesAABB`를 별도 함수로 둔다.

### `ProcessColliderRevise`

역할:

- 단일 cast 경로를 호출하고 `CamUpdateInfo`에 결과를 넣는다.

문제점:

- `colliderHitLength`를 받지만 사용하지 않는다.
- 로직 대부분이 private 함수에 있으므로 wrapper의 존재 이유가 약하다.
- 오타: `ShpereCast`.

개선점:

- wrapper를 제거하거나 `CamUpdateInfo` 업데이트 책임만 가진 명확한 adapter로 둔다.
- 미사용 파라미터를 제거한다.

### `ProcessColliderReviseMultiProbe`

역할:

- MultiProbe 보정, normal skin, floor rescue를 순서대로 적용한다.

문제점:

- `curRot` 미사용.
- hit normal의 유효성 검증이 약하다.
- skin과 floor rescue가 모두 최종 위치를 직접 수정한다.
- “충돌 거리 계산”과 “최종 위치 후처리”가 섞여 있다.

개선점:

- 거리 계산 함수는 `float safeDistance`만 반환하게 줄인다.
- 후처리는 `ApplySkin`, `ApplyFloorRescue`로 분리한다.

### `IsLineToCameraClear`

역할:

- 피벗과 후보 카메라 위치 사이에 충돌 레이어가 있는지 검사한다.

문제점:

- 카메라 반지름을 무시한다.
- trigger 정책, self ignore 정책이 없다.

개선점:

- 이름을 `IsCenterLineClear`로 바꾸거나, 실제 카메라 볼륨 검사용 함수와 분리한다.

### `ProbeCameraReachMultiProbe`

역할:

- 목표 위치까지 도달 가능한 거리만 계산한다.

문제점:

- MultiProbe 본체와 중복되지만 공통화되어 있지 않다.
- ring probe 시작점 문제를 똑같이 가진다.
- skin/normal 조건이 없다.

개선점:

- `TryCollectProbeHits` 같은 공통 helper를 만들고 본체와 공유한다.

### `IsCameraPositionClear`

역할:

- 후보 위치가 MultiProbe 기준으로 clear인지 검사한다.

문제점:

- 중심선과 ring line만 검사하고 후보 위치 구체 overlap은 검사하지 않는다.
- `probeCount`가 0이면 함수명과 달리 다중 검사가 아니다.

개선점:

- `CheckSphere(candidateCpos, colliderRadius, mask)`를 추가한다.
- `probeCount`는 최소 1 이상으로 clamp하거나 함수명/문서에 명시한다.

### `EnsureGroundClearance`

역할:

- 카메라 위치가 지면보다 너무 낮으면 y를 올린다.

문제점:

- floor와 obstacle을 같은 마스크로 본다.
- y만 올려 다른 충돌을 만들 수 있다.
- 피벗 fallback 기준이 실제 카메라 지면과 다를 수 있다.

개선점:

- floor mask를 별도로 받는다.
- y 보정 후 최종 위치 충돌 검사를 다시 한다.

### `EnsureCameraNotBelowFloor`

역할:

- 카메라가 피벗보다 일정 이상 낮고 지면 아래라고 판단되면 축 위에서 피벗 쪽으로 당긴다.

문제점:

- 최종 보정 y가 ground 기준이 아니라 pivot 기준이다.
- `axisDir.y`가 0에 가까운 경우가 위험하다.
- `dropThresholdM`이 음수면 거의 항상 동작할 수 있다.

개선점:

- 입력값 clamp를 추가한다.
- ground 기준 safeY로 계산한다.

### `GetTerrainPos`

역할:

- Terrain 높이보다 카메라가 낮으면 y를 올린다.

문제점:

- Terrain 조회 기준과 샘플 기준이 다르다.
- 반지름 하드코딩.
- `ref` 파라미터 불필요.

개선점:

- `GetTerrainPos(Vector3 cpos, float radius, out Vector3 rpos)` 형태로 줄인다.

### `GetCameraColliderPosMultiProbe`

역할:

- MultiProbe 충돌 보정의 핵심 구현이다.

문제점:

- 디졸브 처리와 충돌 계산이 한 함수에 섞여 있다.
- 전역 매니저 상태에 따라 충돌 계산까지 return된다.
- probe 기하가 부정확하다.
- hit 채택 조건이 부족하다.

개선점:

- 순수 계산 함수로 줄인다.
- 디졸브는 별도 호출자가 처리한다.
- probe 결과는 `hit`, `distance`, `normal`, `sourceProbeIndex` 같은 구조체로 반환하면 디버깅이 쉬워진다.

### `GetCameraColliderPos`

역할:

- 단일 SphereCast 기반 충돌 보정의 핵심 구현이다.

문제점:

- 실제 후보 위치 방향 대신 회전 기반 방향을 쓴다.
- cast 거리 계산이 내적 기반이라 불안정하다.
- overlap fallback에서 실패 시 카메라를 피벗으로 순간 이동시킨다.
- 디졸브 처리 중복이 있다.

개선점:

- 방향/거리는 `cpos - tpos`에서 직접 구한다.
- overlap 해소 실패 시 피벗으로 바로 붙이지 말고 최소 거리 또는 이전 safe distance를 사용한다.
- 디졸브 처리를 제거한다.

## 코드 크기와 책임 문제

이 파일은 하나의 정적 클래스 안에 다음 책임을 모두 담고 있다.

- viewport/frustum 판정
- 단일 SphereCast 충돌
- MultiProbe 충돌
- 후보 위치 clear 검사
- 도달 가능 거리 계산
- ground clearance
- floor rescue
- Terrain height 보정
- 캐릭터 디졸브 이벤트 호출
- 전역 actor list 조회

이 크기 자체가 버그 가능성을 높인다. 특히 같은 개념인 “카메라가 갈 수 있는 안전 거리”가 여러 함수에서 서로 다른 방식으로 계산된다.

권장 분리:

| 클래스/모듈 | 책임 |
| --- | --- |
| `CameraViewportUtility` | viewport/frustum 관련 순수 함수 |
| `CameraCollisionProbe` | SphereCast/MultiProbe로 safe distance 계산 |
| `CameraGroundResolver` | ground/floor/Terrain 보정 |
| `CameraOcclusionDissolve` | 캐릭터 디졸브 이벤트 |
| `CameraCollisionSettings` | 레이어, 반지름, skin, probe 수, threshold 설정 |

핵심 계산 함수는 다음처럼 작게 유지하는 것이 좋다.

```csharp
public readonly struct CameraCollisionResult
{
    public readonly bool Hit;
    public readonly float SafeDistance;
    public readonly Vector3 HitNormal;
}

public static CameraCollisionResult Evaluate(
    Vector3 pivot,
    Vector3 desiredPosition,
    float radius,
    int probeCount,
    LayerMask mask)
```

이렇게 만들면 호출자는 `pivot + dir * SafeDistance`로 위치를 계산하고, floor rescue와 dissolve는 별도 단계에서 적용할 수 있다.

## 우선순위별 개선안

1. `GetCameraColliderPos`의 방향/거리 계산을 `cpos - tpos` 기준으로 수정한다.
2. `LayerMask.NameToLayer` 조합을 `LayerMask.GetMask`와 검증 로직으로 바꾼다.
3. ActorController 유무로 충돌 계산을 중단하지 않도록 한다.
4. MultiProbe ring probe를 `tpos + offset -> cpos + offset`로 바꾸고, normal alignment를 hit 채택 조건에 넣는다.
5. `EnsureCameraNotBelowFloor`의 safe 위치를 pivot 기준이 아니라 ground 기준으로 계산한다.
6. 디졸브 처리를 충돌 계산 함수 밖으로 분리한다.
7. `GetTerrainPos`, `EnsureGroundClearance`, `EnsureCameraNotBelowFloor`를 하나의 ground 보정 모듈로 합친다.
8. `CamColliderHit`, `FloorRescued`, `DissolveTriggered`처럼 결과 플래그를 분리해 바닥 보정이 줌 인터럽트로 오인되지 않게 한다.
9. `SRControllableCameraBase.UpdateCamProperty` 같은 소비 경로에서는 `nextZoomRate`를 `Clamp01`하고, 반복 `Mathf.Lerp(current, target, k * deltaTime)`를 `SmoothDamp` 또는 지수 평활로 교체한다.
10. 락온 후보 비교는 side effect 없는 reach 함수만 사용하고, 후보 전환에는 reach ratio와 hysteresis를 둔다.
11. 미사용 using, 미사용 파라미터, 대량 주석 코드를 제거한다.

## 최종 판단

현재 코드는 “여러 상황을 막기 위해 보정을 계속 덧붙인 상태”에 가깝다. 가장 위험한 부분은 계산 기준이 한 곳에 고정되어 있지 않다는 점이다. 단일 SphereCast는 회전 기준, MultiProbe는 위치 축 기준, ground rescue는 pivot y 기준, Terrain 보정은 tpos Terrain + cpos sample 기준을 쓴다.

따라서 단순 리팩터링보다 먼저 “카메라 충돌의 기준”을 하나로 정해야 한다. 추천 기준은 `pivot`, `desiredPosition`, `radius`, `mask`만 입력받아 `safeDistance`를 반환하는 순수 계산 함수다. 그 위에 ground 보정과 디졸브를 별도 단계로 얹으면 코드 크기도 줄고, 계산 오류도 훨씬 추적하기 쉬워진다.

## 전체 구문별 정밀 분석

이 섹션은 코드의 등장 순서대로 분석한다. 이미 앞에서 지적한 내용도, 실제 코드 흐름을 따라 다시 정리한다.

### 1. using 블록

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using MAST_ENUM;
using System;
using PFData;
using UnityEngine.Profiling;
using GFRender;
```

구문 의미:

- Unity 물리, 카메라, 벡터 연산을 위해 `UnityEngine`을 사용한다.
- `GFRender`는 `GrTerrainManager` 사용 때문에 필요해 보인다.
- `PFData`, `MAST_ENUM`은 `CamUpdateInfo`, `SRGameManager` 등 외부 타입 때문에 들어온 것으로 추정된다.

문제점:

- `System.Collections`, `System.Collections.Generic`, `BinaryFormatter`, `System.IO`, `System`, `UnityEngine.Profiling`은 본문에서 직접 쓰이지 않는다.
- `BinaryFormatter`는 현재 .NET에서 위험 API로 취급되므로, 사용하지 않는 import라도 남기는 것은 좋지 않다.
- 필요한 의존성과 불필요한 의존성이 섞여 클래스가 실제로 무엇에 묶여 있는지 흐려진다.

개선:

- 먼저 사용하지 않는 using을 제거한다.
- `MAST_ENUM`, `PFData`, `GFRender`가 꼭 필요한지 타입 단위로 확인한다.
- 충돌 계산만 분리하면 `UnityEngine` 외 의존성을 대부분 없앨 수 있다.

### 2. 클래스 선언

```csharp
public static class GameCameraCalculator
```

구문 의미:

- 인스턴스 상태 없이 정적 함수 모음으로 사용하겠다는 설계다.

문제점:

- 정적 클래스인데 내부에서 `SRGameManager.Instance`, `GrTerrainManager.Instance`, 레이어 이름 같은 전역 상태를 직접 읽는다.
- 순수 수학/물리 계산 유틸리티처럼 보이지만 실제로는 전역 게임 상태에 의존한다.
- 테스트가 어렵다. `Physics`, `LayerMask`, `SRGameManager`, `GrTerrainManager`를 모두 실제 환경에 의존해야 한다.

개선:

- 순수 계산 함수는 정적으로 유지해도 된다.
- 전역 매니저 접근, 디졸브 이벤트, Terrain 조회는 별도 서비스로 분리한다.
- 충돌 마스크와 설정값은 함수 인자 또는 설정 객체로 주입한다.

### 3. `_camCldLayer`

```csharp
private static int _camCldLayer = (1 << LayerMask.NameToLayer("CameraCollider") |
                                   1 << LayerMask.NameToLayer("WalkableGround") |
                                   1 << LayerMask.NameToLayer("Obstacle"));
```

구문 의미:

- 카메라와 충돌할 레이어를 비트마스크로 만든다.

수학/비트 연산 문제:

- `LayerMask.NameToLayer`는 레이어 index를 반환한다.
- 정상인 경우 `1 << index`로 해당 비트를 켠다.
- 실패하면 `-1`을 반환한다.
- C#의 `int` shift는 실제 shift count가 0~31 범위로 처리된다. `1 << -1`은 예외가 아니라 `1 << 31`과 유사한 결과가 될 수 있다.
- 따라서 레이어 이름 오타가 조용히 “최상위 비트가 켜진 이상한 mask”로 바뀔 수 있다.

로직 문제:

- `WalkableGround`와 `Obstacle`을 같은 충돌 mask로 묶는다. 카메라 충돌에는 맞을 수 있지만, ground 보정에서는 “바닥”만 필요하다. 이후 `EnsureGroundClearance`가 이 mask를 그대로 쓰므로 obstacle을 ground처럼 오판정할 수 있다.

개선:

```csharp
private static readonly int CamCollisionMask = LayerMask.GetMask(
    "CameraCollider",
    "WalkableGround",
    "Obstacle");

private static readonly int GroundMask = LayerMask.GetMask("WalkableGround");
```

그리고 `CamCollisionMask == 0`이면 초기화 실패로 로그를 남긴다.

### 4. `_camColliderToDissolveLayer`

```csharp
private static int _camColliderToDissolveLayer = (1 << LayerMask.NameToLayer("Character") | 
                                                  1 << LayerMask.NameToLayer("HurtBox"));
```

구문 의미:

- 카메라 위치가 캐릭터 또는 HurtBox 내부에 들어갔을 때 디졸브 처리를 하기 위한 layer mask다.

문제점:

- `_camCldLayer`와 같은 `NameToLayer` 실패 문제가 있다.
- 캐릭터 디졸브 판정과 카메라 충돌 보정은 다른 책임인데, 같은 클래스와 같은 private 함수 안에서 처리된다.
- `HurtBox`를 디졸브 대상으로 쓰면 공격 판정용 콜라이더가 카메라 occlusion 판정에 섞일 수 있다. 큰 몬스터 처리 목적은 이해되지만, HurtBox가 켜지는 타이밍에 따라 카메라 디졸브가 흔들릴 수 있다.

개선:

- 디졸브용 occluder layer를 별도로 두는 편이 가장 깔끔하다.
- 최소한 디졸브 mask와 충돌 mask는 별도 설정 객체에서 관리한다.

### 5. `ContainsFrustum`의 폐기 주석

```csharp
/*Vector3 camForward = camera.transform.forward;
...
return true;*/
```

구문 의미:

- 이전 판정 방식이 주석으로 남아 있다.
- 카메라 전방 각도, near/far clip 거리만 검사하는 방식이다.

수학적 평가:

- `Vector3.Angle(dirToTarget, camForward) > 90f`는 카메라 뒤쪽 여부만 대략 판정한다.
- 하지만 카메라 FOV가 60도라면 화면 안 조건은 전방 90도가 아니라 수평/수직 FOV 안이다.
- `distanceToCamera`는 유클리드 거리지만 clip plane은 카메라 forward 축의 z 거리 기준이다. 대각선 방향에서 실제 clip 판정과 어긋날 수 있다.

판단:

- 폐기된 주석 코드이긴 하지만, 남겨두면 “이것도 후보 구현”처럼 보인다.
- 현재 구현의 의도와 차이가 커서 제거하는 편이 좋다.

### 6. `ContainsFrustum` 실제 구현

```csharp
Vector3 viewport = camera.WorldToViewportPoint(position);
return viewport.z > 0f &&
       viewport.x >= 0f && viewport.x <= 1f &&
       viewport.y >= 0f && viewport.y <= 1f;
```

구문 의미:

- 월드 좌표를 viewport 좌표로 바꾼다.
- x/y가 0~1이고 z가 양수면 화면 안으로 본다.

수학적 평가:

- `viewport.x`, `viewport.y`는 projection 후 normalized screen coordinate다.
- `viewport.z`는 camera space z 거리다.
- `z > 0`은 카메라 앞이라는 뜻이지, clipping volume 내부라는 뜻은 아니다.

틀릴 수 있는 경우:

- `viewport.z > camera.farClipPlane`인데 x/y가 0~1이면 true가 된다.
- `viewport.z < camera.nearClipPlane`인데 x/y가 0~1이면 true가 될 수 있다.
- 대상이 점이 아니라 캐릭터처럼 부피가 있는 경우, 중심점만 화면 밖이고 일부 mesh가 화면 안인 상황을 false로 판정한다.
- 반대로 중심점만 화면 안이고 큰 오브젝트 대부분이 화면 밖이어도 true다.

락온 관련 영향:

- 이 함수가 락온 후보 필터로 쓰이면, far clip 밖 대상도 후보가 될 수 있다.
- 보스처럼 큰 대상의 중심이 화면 밖으로 나가면 실제로 몸체가 보이는데도 후보에서 빠질 수 있다.

개선:

- 점 후보 필터라면 near/far clip을 추가한다.
- 락온 후보라면 viewport margin을 둔다. 예: x/y를 `-0.1~1.1`로 허용해 화면 가장자리 흔들림을 줄인다.
- 큰 대상은 collider bounds 또는 renderer bounds를 기준으로 판정한다.

### 7. `ProcessColliderRevise` 시그니처

```csharp
public static CamUpdateInfo ProcessColliderRevise(float colliderRadius, Vector3 curPos, Quaternion curRot, Vector3 targetPos,
    float colliderHitLength, CamUpdateInfo camUpdateInfo)
```

구문 의미:

- 단일 SphereCast 보정 wrapper다.
- 입력은 카메라 반지름, 현재 카메라 위치, 현재 회전, target position, cast 길이, 업데이트 정보다.

문제점:

- `colliderHitLength`를 받지만 사용하지 않는다.
- 주석은 “최종 위치와 회전까지 설정하기 위해 보정”이라고 하지만 실제로는 위치 충돌만 보정한다.
- `curPos`가 “현재 위치”인지 “이번 프레임 desired 위치”인지 모호하다. 충돌 계산에서는 desired camera position이어야 맞다.
- `CamUpdateInfo`가 struct인지 class인지에 따라 호출 비용과 의미가 달라진다.

수학적 영향:

- caller가 `colliderHitLength`를 정확히 계산해서 넘겨도 무시된다.
- 실제 `GetCameraColliderPos`는 `curRot` 기반으로 길이를 다시 계산하기 때문에, caller와 callee의 수학 기준이 달라질 수 있다.

개선:

- `curPos` 이름을 `desiredCameraPos`로 바꾼다.
- `colliderHitLength`를 제거하거나 `desiredDistance`로 명확히 쓰게 한다.
- 반환 타입은 `CameraCollisionResult`처럼 계산 결과만 담는 구조가 더 낫다.

### 8. `ProcessColliderRevise` 본문

```csharp
Vector3 cldPos;
camUpdateInfo.CamColliderHit = GetCameraColliderPos(colliderRadius, curPos, curRot, targetPos, out cldPos );
camUpdateInfo.CalculatedPos = cldPos;

return camUpdateInfo;
```

구문 의미:

- private 함수에서 보정 위치를 얻고 `CamUpdateInfo`에 기록한다.

문제점:

- `GetCameraColliderPos`가 false를 반환해도 `cldPos`는 `cpos`로 설정되므로 `CalculatedPos`는 항상 갱신된다. 이 자체는 문제는 아니지만, “충돌 없으면 기존 값 유지”를 기대하는 호출자라면 다르게 동작한다.
- `CamColliderHit`에는 floor rescue, ground clearance 같은 후처리 정보가 포함되지 않는다. 이 wrapper는 단일 충돌 경로만 반영한다.

개선:

- `CamUpdateInfo`의 필드 의미를 명확히 한다. `CalculatedPos`가 desired인지 collision-adjusted인지 이름만으로는 애매하다.

### 9. `ProcessColliderReviseMultiProbe` 시그니처

```csharp
public static CamUpdateInfo ProcessColliderReviseMultiProbe(float colliderRadius, Vector3 curPos, Quaternion curRot, Vector3 targetPos,
    int probeCount,
    bool floorRescueEnabled, float floorRescueDropThresholdM,
    float skinWidth, float minNormalAlignment,
    CamUpdateInfo camUpdateInfo)
```

구문 의미:

- MultiProbe 충돌, skin, floor rescue를 한 번에 처리한다.

문제점:

- 파라미터가 너무 많다. 일부는 충돌 probe 설정이고 일부는 floor rescue 설정이다.
- `curRot`는 내부에서 사용되지 않는다.
- `minNormalAlignment`는 충돌 hit 채택이 아니라 skin 적용에만 쓰인다.
- `floorRescueDropThresholdM`이 음수거나 너무 작을 때 거의 항상 rescue가 켜질 수 있다.
- `probeCount`가 음수이면 내부에서 0으로 clamp된다. 호출자 실수를 조용히 무시한다.

락온 관련 영향:

- 락온 중 좌우 shoulder 위치나 sign을 바꾸는 로직이 이 함수를 기준으로 안전 위치를 계산한다면, `curRot` 미사용 자체는 큰 문제는 아니다. 오히려 위치 축 기준이 맞다.
- 하지만 `ProbeCameraReachMultiProbe`와 같은 기준이라고 주석에 적혀 있으므로, 실제 probe 기하가 정확히 같아야 한다. 현재는 두 함수 모두 같은 부채꼴 문제를 공유한다.

개선:

- `CameraProbeSettings`와 `CameraFloorRescueSettings`로 분리한다.
- `curRot` 제거.
- `minNormalAlignment`를 `GetCameraColliderPosMultiProbe`로 넘긴다.

### 10. `GetCameraColliderPosMultiProbe` 호출부

```csharp
bool moved = GetCameraColliderPosMultiProbe(colliderRadius, curPos, curRot, targetPos, probeCount,
    out cldPos, out hitNormal, out hitNormalIsFallback);
```

구문 의미:

- MultiProbe 본체에서 보정 위치, 대표 hit normal, fallback 여부를 받는다.

문제점:

- 대표 hit normal은 “가장 작은 projection distance”를 만든 hit의 normal이다.
- 하지만 이후 skin 적용은 그 normal 하나만 사용한다.
- 여러 probe가 서로 다른 표면을 맞은 경우, 가장 가까운 hit normal이 최종 위치 주변의 실제 충돌 normal과 다를 수 있다.

수학적 위험:

- 코너에서 왼쪽 벽과 오른쪽 벽을 동시에 맞으면, 가장 가까운 hit 하나의 normal만 선택한다.
- 그 normal 방향으로 `cldPos`를 밀면 반대쪽 벽에 더 가까워질 수 있다.
- 이런 상황에서 카메라가 좌우로 떨릴 가능성이 있다.

개선:

- 여러 hit normal을 평균내거나, 최종 `CheckSphere`로 검증한다.
- 더 안정적인 방법은 normal로 위치를 밀지 않고 축 방향 safe distance만 줄이는 것이다.

### 11. MultiProbe skin 적용 블록

```csharp
if (moved && !hitNormalIsFallback)
{
    Vector3 axis = curPos - targetPos;
    float axisLen = axis.magnitude;
    if (axisLen >= 1e-5f)
    {
        Vector3 axisDir = axis / axisLen;
        float alignment = Vector3.Dot(hitNormal, -axisDir);
        if (alignment >= minNormalAlignment)
        {
            float clampedSkin = Mathf.Min(Mathf.Max(0f, skinWidth), colliderRadius * 0.5f);
            if (clampedSkin > 0f)
                cldPos += hitNormal * clampedSkin;
        }
    }
}
```

구문 의미:

- 충돌로 위치가 움직였고 fallback normal이 아니라면, hit normal과 카메라 진행 반대 방향의 정렬도를 본다.
- 정렬도가 충분하면 hit normal 방향으로 skin만큼 위치를 민다.

수학적 분석:

- `axisDir = target -> desiredCamera`
- `-axisDir = desiredCamera -> target`
- `hitNormal`이 벽에서 바깥으로 나오는 방향이라고 가정하면, `Dot(hitNormal, -axisDir)`가 클수록 “카메라가 벽을 향해 들어가는 방향”과 normal이 맞다는 의미다.
- 하지만 Unity hit normal은 cast가 맞은 collider 표면의 normal이다. LineCast로 얻은 normal은 선분 방향과 관계없이 표면 normal이다.

문제점:

- `hitNormal == Vector3.zero`일 가능성에 대한 방어가 없다.
- `minNormalAlignment`가 -1보다 작으면 거의 항상 통과한다.
- `minNormalAlignment`가 1보다 크면 절대 통과하지 않는다.
- `cldPos += hitNormal * clampedSkin`은 축에서 벗어나는 이동이다.
- 축에서 벗어난 뒤 카메라가 새로운 위치에서 clear한지 검사하지 않는다.

락온 관련 영향:

- 락온 카메라에서 좌/우 shoulder 위치를 비교할 때, 한쪽 후보만 normal skin으로 옆으로 밀리면 실제 거리 비교가 왜곡된다.
- sign 결정은 보통 “왼쪽이 더 멀리 갈 수 있는가, 오른쪽이 더 멀리 갈 수 있는가”를 보는데, normal skin은 도달 거리보다는 최종 좌표를 바꾸므로 비교 기준이 흔들린다.

개선:

- `minNormalAlignment = Mathf.Clamp01(minNormalAlignment)` 또는 설정 검증을 둔다.
- skin은 `safeDistance = Mathf.Max(0, bestProjLen - skinWidth)` 방식이 안정적이다.
- normal offset을 꼭 쓴다면 마지막에 `Physics.CheckSphere(cldPos, colliderRadius, mask)`를 수행한다.

### 12. floor rescue 호출부

```csharp
if (floorRescueEnabled)
{
    moved |= EnsureCameraNotBelowFloor(colliderRadius, targetPos, floorRescueDropThresholdM, ref cldPos);
}
```

구문 의미:

- 바닥 아래로 빠진 경우 `cldPos`를 추가 보정한다.
- 보정이 발생하면 `moved`를 true로 유지한다.

문제점:

- `cldPos`가 `GetCameraColliderPosMultiProbe`에서 보정되지 않았더라도 floor rescue는 동작한다.
- floor rescue가 y와 축 위치를 바꾼 뒤, 충돌 mask에 대해 다시 검증하지 않는다.
- `moved`는 “벽 충돌이 있었다”와 “floor rescue가 있었다”를 구분하지 못한다.

락온 관련 영향:

- 락온 중 카메라 후보 reach를 비교할 때 floor rescue까지 섞이면, “벽 때문에 reach가 짧은지”와 “바닥 아래라서 당긴 것인지”가 구분되지 않는다.
- sign 선택에는 벽/장애물 reach만 쓰고, floor rescue는 최종 위치 안정화에서 따로 적용하는 편이 더 명확하다.

개선:

- 결과 타입에 `CollisionHit`, `FloorRescued`, `DissolveTriggered` 같은 flags를 분리한다.

### 13. `IsLineToCameraClear`

```csharp
return !Physics.Linecast(pivot, candidateCpos, _camCldLayer);
```

구문 의미:

- 중심선 하나만 clear한지 본다.

수학적 문제:

- 카메라가 반지름을 가진 물체라면 중심선 clear는 충분조건이 아니다.
- 벽이 중심선 옆으로 살짝 비껴 있어도 카메라 구체는 충돌할 수 있다.
- 반대로 얇은 collider가 중심선만 막지만 카메라의 실제 shoulder offset 경로에는 영향이 없을 수 있다.

락온 관련 영향:

- 락온 후보 sign 결정에서 이 함수를 쓰면, MultiProbe/SphereCast 기준과 다른 결과가 나온다.
- “오른쪽은 clear, 왼쪽은 blocked”라는 판단이 실제 카메라 반지름 기준과 다를 수 있다.

개선:

- 함수명을 `IsCenterLineClear`로 낮춰 부른다.
- 락온용으로는 `ProbeCameraReachMultiProbe` 같은 동일 기준 reach 함수를 사용한다.

### 14. `ProbeCameraReachMultiProbe` 축 계산

```csharp
Vector3 axis = desiredCpos - pivot;
float axisLen = axis.magnitude;
if (axisLen < 1e-5f) return 0f;
Vector3 axisDir = axis / axisLen;
```

구문 의미:

- pivot에서 desired camera position까지의 축과 길이를 구한다.

수학적 평가는 대체로 맞다.

주의점:

- `axisLen < 1e-5f`에서 0을 반환하는 것은 계산 안정성은 좋다.
- 하지만 락온 후보 비교에서는 두 후보 모두 0에 가까운 경우 sign이 불안정해질 수 있다.
- 호출자가 `desiredCpos == pivot`을 정상 후보로 넘기지 않도록 해야 한다.

개선:

- 이 경우 `ReachResult.Invalid`를 반환하는 편이 0 거리와 구분되어 안전하다.

### 15. `ProbeCameraReachMultiProbe` center probe

```csharp
float minReach = axisLen;

if (Physics.Linecast(pivot, desiredCpos, out RaycastHit centerHit, _camCldLayer))
{
    float proj = Vector3.Dot(centerHit.point - pivot, axisDir);
    if (proj < minReach) minReach = proj;
}
```

구문 의미:

- 중심선이 맞은 경우 hit point를 axis에 projection해서 도달 가능 거리 후보로 삼는다.

수학적 분석:

- Linecast hit point는 선분 위에 있으므로 이상적으로 `proj`는 0~axisLen이다.
- floating point 오차 또는 시작점 overlap 상황에서는 음수/0 근처가 될 수 있다.
- 마지막에 `Mathf.Max(0f, minReach)`만 하므로 axisLen 초과는 방지되지 않는다. 다만 `proj < minReach` 조건 때문에 초과값은 보통 반영되지 않는다.

문제점:

- centerHit가 trigger인지 여부, self collider인지 여부를 걸러내지 않는다.
- hit가 pivot 근처 target collider라면 reach가 0에 가까워져 락온 카메라가 피벗으로 붙을 수 있다.

락온 관련 영향:

- 락온 대상/플레이어의 collider가 `_camCldLayer`에 포함되면 해당 방향 후보가 항상 blocked처럼 보일 수 있다.
- 특히 pivot이 캐릭터 내부 socket이면 Linecast 시작점이 collider 안쪽인 경우가 생긴다.

개선:

- `QueryTriggerInteraction.Ignore` 또는 설정값을 명시한다.
- ignore transform 목록을 받는다.

### 16. `ProbeCameraReachMultiProbe` basis 생성

```csharp
Vector3 right = Vector3.Cross(axisDir, Vector3.up);
if (right.sqrMagnitude < 1e-4f)
    right = Vector3.Cross(axisDir, Vector3.forward);
right.Normalize();
Vector3 up = Vector3.Cross(right, axisDir).normalized;
```

구문 의미:

- 카메라 축에 수직인 원형 probe 평면의 basis를 만든다.

수학적 분석:

- `Cross(axisDir, worldUp)`는 axisDir에 수직인 right 후보를 만든다.
- axisDir이 worldUp과 거의 평행하면 cross가 거의 0이므로 worldForward로 fallback한다.
- `up = Cross(right, axisDir)`도 axisDir과 right에 수직이다.

주의점:

- fallback 기준이 `Vector3.forward`라서 axisDir이 worldForward와도 평행에 가까운 경우를 생각할 수 있다. 하지만 첫 cross가 실패하는 경우는 axisDir이 worldUp과 평행한 경우라, worldForward와는 보통 평행하지 않다.
- basis의 handedness가 일반 카메라 right/up과 다를 수 있다. 충돌 probe에는 큰 문제는 아니지만, 락온 좌우 sign 해석에 이 basis를 직접 쓰면 안 된다.

락온 관련 영향:

- 이 basis는 “카메라 축 주변의 원형 probe”용이다.
- 락온 좌/우 후보 위치를 만들 때 이 `right`를 사용하면, world/camera 기준 좌우와 부호가 반대로 느껴질 수 있다.
- sign 결정은 카메라 yaw 기준 right 또는 target-pivot 평면 기준 right를 별도로 정의해야 한다.

개선:

- basis 생성 함수를 공통화하고 이름을 `BuildProbeBasis`처럼 명확히 한다.
- 락온 sign용 basis와 충돌 probe basis를 혼용하지 않는다.

### 17. `ProbeCameraReachMultiProbe` ring probe loop

```csharp
int n = Mathf.Max(0, probeCount);
for (int i = 0; i < n; i++)
{
    float a = (Mathf.PI * 2f * i) / n;
    Vector3 offset = (right * Mathf.Cos(a) + up * Mathf.Sin(a)) * colliderRadius;
    if (Physics.Linecast(pivot, desiredCpos + offset, out RaycastHit ringHit, _camCldLayer))
    {
        float proj = Vector3.Dot(ringHit.point - pivot, axisDir);
        if (proj < minReach) minReach = proj;
    }
}
```

구문 의미:

- endpoint 주변 원 위의 지점으로 Linecast한다.

수학적 문제:

- `n = 0`이면 loop는 돌지 않는다. 나눗셈도 실행되지 않으므로 divide by zero는 없다.
- `n = 1`이면 원형 probe 하나만 생기며 offset은 항상 `right * radius`다. 원형 검사로 보기 어렵다.
- 시작점이 offset되지 않으므로 앞서 말한 부채꼴 문제가 있다.
- `proj`는 ring line 위의 hit point를 중심 axis에 projection한다. hit point가 endpoint offset 쪽에 있을수록 axis projection이 실제 line distance와 다르다.

중요한 계산 차이:

- `Linecast(pivot, desiredCpos + offset)`의 실제 ray direction은 `(axis + offset).normalized`다.
- 그런데 projection은 `axisDir`로 한다.
- 즉 cast 방향과 projection 방향이 다르다.
- offset이 커질수록 hit distance와 projected reach 차이가 커진다.

락온 관련 영향:

- 좌우 후보 reach를 비교할 때, 각 후보의 offset geometry 때문에 실제 장애물 거리보다 작거나 큰 reach가 나올 수 있다.
- 특히 벽이 카메라 옆에 있을 때 한쪽 후보가 과하게 blocked로 평가될 수 있다.
- sign이 매 프레임 바뀌면 락온 카메라가 좌우로 흔들린다.

개선:

```csharp
Vector3 start = pivot + offset;
Vector3 end = desiredCpos + offset;
if (Physics.Linecast(start, end, out RaycastHit hit, mask))
{
    float proj = Vector3.Dot(hit.point - pivot, axisDir);
    minReach = Mathf.Min(minReach, proj - skinWidth);
}
```

그리고 최종 반환은 `Mathf.Clamp(minReach, 0f, axisLen)`가 낫다.

### 18. `IsCameraPositionClear` center + axis

```csharp
if (Physics.Linecast(pivot, candidateCpos, _camCldLayer))
    return false;

Vector3 axis = candidateCpos - pivot;
float axisLen = axis.magnitude;
if (axisLen < 1e-5f)
    return true;
```

구문 의미:

- 중심선이 막히면 false.
- 후보 위치가 pivot과 거의 같으면 true.

문제점:

- `axisLen < 1e-5f`인 경우 후보 위치가 pivot 내부나 collider 내부라도 true다.
- 중심선 clear 후 후보 위치의 `CheckSphere`를 하지 않는다.

락온 관련 영향:

- 락온 중 카메라가 pivot에 매우 가까워지는 예외 상황에서 clear로 판정되어 카메라가 내부로 들어갈 수 있다.

개선:

- `axisLen < EPSILON`일 때도 `CheckSphere(candidateCpos, radius, mask)`는 수행해야 한다.

### 19. `IsCameraPositionClear` ring loop

```csharp
if (Physics.Linecast(pivot, candidateCpos + offset, _camCldLayer))
    return false;
```

구문 의미:

- ring endpoint 방향이 하나라도 막히면 후보 위치를 blocked로 본다.

문제점:

- `ProbeCameraReachMultiProbe`와 같은 부채꼴 문제가 있다.
- `GetCameraColliderPosMultiProbe`와도 같은 문제이므로 기준은 일관되지만, 그 일관성이 물리적으로 틀린 모델일 수 있다.
- 후보 위치 자체의 overlap이 없다.

락온 관련 영향:

- 후보 clear 판정과 reach 계산이 둘 다 과보수적으로 나올 수 있다.
- 장애물이 피벗 근처에는 없고 카메라 후보 옆에만 있는 경우, 시작점이 pivot이라 실제보다 넓은 사선 경로를 검사한다.

개선:

- `Linecast(pivot + offset, candidateCpos + offset)`로 바꾼다.
- 마지막에 `CheckSphere(candidateCpos, radius, mask)`를 추가한다.

### 20. `EnsureGroundClearance` 초기 ray

```csharp
float probeDist = minClearanceM + colliderRadius * 2f;
if (Physics.Raycast(cldCamPos, Vector3.down, out var camGround, probeDist, _camCldLayer))
```

구문 의미:

- 카메라 위치에서 아래로 ray를 쏴서 가까운 ground를 찾는다.

수학적 문제:

- ray 시작점이 collider 내부라면 Unity Raycast는 시작 overlap을 감지하지 못할 수 있다.
- `_camCldLayer`에는 obstacle도 포함되어 있으므로, 아래쪽 상자/벽 윗면도 ground로 잡힐 수 있다.
- `probeDist`가 `minClearanceM + 2r`인 이유는 직관적이지만, near-plane 크기와 카메라 pitch/FOV는 반영하지 않는다.

개선:

- ground mask 별도 사용.
- `SphereCast` 또는 `CheckSphere`를 병행한다.
- near-plane clipping을 정말 막으려면 카메라 near-plane 네 모서리를 고려해야 한다.

### 21. `EnsureGroundClearance` pivot fallback

```csharp
const float pivotProbeDist = 10f;
if (Physics.Raycast(tpos, Vector3.down, out var pivotGround, pivotProbeDist, _camCldLayer))
```

구문 의미:

- 카메라 아래 ground를 못 찾으면 pivot 아래 ground를 쓴다.

문제점:

- 카메라가 절벽 밖에 있을 때 잘못된 ground를 가져온다.
- pivot과 camera 사이에 높이 차가 큰 구조물, 다리, 계단이 있으면 부정확하다.
- 10m 하드코딩은 스케일이 다른 게임에서 문제가 된다.

락온 관련 영향:

- 락온 중 카메라가 보스를 보기 위해 뒤로 빠졌는데 카메라 아래는 낭떠러지라면, 플레이어 발밑 ground 기준으로 카메라를 끌어올려 구도가 튈 수 있다.

개선:

- pivot fallback은 `verticalDrop`이 큰 rescue 상황에서만 제한적으로 사용한다.

### 22. `EnsureGroundClearance` y 직접 보정

```csharp
float clearance = cldCamPos.y - groundHit.point.y;
if (clearance >= minClearanceM)
    return false;

cldCamPos.y = groundHit.point.y + minClearanceM;
return true;
```

구문 의미:

- y축 높이만 올린다.

수학적 문제:

- 카메라 축을 따라 후퇴하는 것이 아니라 월드 y만 바꾼다.
- desired camera direction/arm length가 바뀐다.
- 위쪽에 천장 collider가 있으면 y 보정으로 천장 안으로 들어갈 수 있다.

개선:

- y 보정 후 충돌 재검사.
- 또는 축 위에서 safe distance를 다시 계산한다.

### 23. `EnsureCameraNotBelowFloor` verticalDrop

```csharp
float verticalDrop = tpos.y - cldCamPos.y;
if (verticalDrop <= dropThresholdM)
    return false;
```

구문 의미:

- 카메라가 pivot보다 충분히 낮을 때만 floor rescue를 시도한다.

문제점:

- pivot height는 ground height가 아니다.
- 캐릭터 socket이 머리/가슴 높이면, 정상적인 낮은 카메라 구도도 drop으로 보일 수 있다.
- `dropThresholdM` 검증이 없다.

락온 관련 영향:

- 락온 카메라가 대형 보스를 올려다보거나 내려다보는 구도에서 pivot과 camera y 차이가 커질 수 있다.
- 이때 floor rescue가 의도치 않게 개입하면 락온 구도가 튄다.

개선:

- ground hit 기준으로 `cldCamPos.y < groundY + clearance`를 먼저 판단한다.

### 24. `EnsureCameraNotBelowFloor` axis 계산과 위쪽 방향 판정

```csharp
Vector3 axis = cldCamPos - tpos;
float axisLen = axis.magnitude;
if (axisLen < 1e-5f)
    return false;
Vector3 axisDir = axis / axisLen;

if (axisDir.y > -1e-3f)
    return false;
```

구문 의미:

- 카메라가 pivot에서 어느 방향에 있는지 보고, 아래쪽을 향한 축일 때만 처리한다.

수학적 평가:

- `axisDir.y > -0.001`이면 거의 수평이거나 위쪽이므로 rescue하지 않는다.
- 수평에 아주 가까운 아래쪽 방향에서는 `axisDir.y`가 -0.001보다 작으면 통과한다.

문제점:

- `axisDir.y = -0.002` 같은 거의 수평인 경우 뒤의 `safeT = -r / axisDir.y`가 매우 커진다.
- 이 값은 axisLen보다 클 수 있는데 clamp가 없다.
- 다만 `currentT > safeT` 조건이 있으므로 safeT가 너무 크면 보정하지 않을 가능성이 높다. 그래도 계산 의미가 불안정하다.

개선:

- `if (axisDir.y > -0.05f) return false;`처럼 실제로 의미 있는 하향 각도에서만 처리한다.
- safeT는 반드시 `0..axisLen`으로 clamp한다.

### 25. `EnsureCameraNotBelowFloor` ground ray

```csharp
const float groundProbeDownDist = 1.5f;
if (!Physics.Raycast(tpos, Vector3.down, out RaycastHit groundHit, colliderRadius + groundProbeDownDist, _camCldLayer))
    return false;
```

구문 의미:

- pivot 아래 ground를 찾는다.

문제점:

- pivot이 높으면 `colliderRadius + 1.5f`가 ground까지 닿지 않을 수 있다.
- pivot이 collider 내부면 raycast 시작 overlap 문제가 생길 수 있다.
- `_camCldLayer`에 obstacle이 포함되어 있다.

락온 관련 영향:

- 대형 몬스터 락온에서 pivot이 높은 위치로 바뀌면 ground ray가 실패해 floor rescue가 작동하지 않을 수 있다.

개선:

- ray origin을 `tpos + Vector3.up * smallOffset`으로 올리고, 거리도 설정값으로 둔다.
- floor mask를 별도로 쓴다.

### 26. `EnsureCameraNotBelowFloor` floor below 판정

```csharp
if (cldCamPos.y >= groundHit.point.y - colliderRadius)
    return false;
```

구문 의미:

- 카메라 중심이 ground보다 colliderRadius만큼 아래로 내려간 경우만 빠짐으로 본다.

수학적 문제:

- 일반적으로 구체 중심은 ground 위 `+radius` 이상이어야 충돌하지 않는다.
- 그런데 조건은 `groundY - radius`보다 낮을 때만 rescue한다.
- 즉 카메라 구체가 이미 ground와 상당히 겹쳐도 rescue하지 않는다.

예시:

- `groundY = 0`, `radius = 0.5`
- 카메라 중심 y가 `0.1`이면 구체 아래쪽은 `-0.4`로 ground를 관통한다.
- 현재 조건 `0.1 >= -0.5`이므로 false return, rescue 안 함.

권장 판정:

```csharp
if (cldCamPos.y >= groundHit.point.y + colliderRadius)
    return false;
```

또는 clearance를 별도로 둔다.

이 부분은 계산상 명확히 잘못되었을 가능성이 높다. 주석은 “ground 아래에 있으면”이라고 되어 있지만, 카메라 구체 반지름까지 고려하면 기준은 `groundY + radius`여야 한다.

### 27. `EnsureCameraNotBelowFloor` safeT 계산

```csharp
float safeT = -colliderRadius / axisDir.y;
if (safeT < 0f) safeT = 0f;
float currentT = Vector3.Dot(cldCamPos - tpos, axisDir);
if (currentT > safeT)
{
    cldCamPos = tpos + axisDir * safeT;
    return true;
}
```

구문 의미:

- 축 위에서 카메라 y가 `tpos.y - colliderRadius`가 되는 지점을 계산한다.

수학 전개:

- `newPos = tpos + axisDir * safeT`
- `newPos.y = tpos.y + axisDir.y * safeT`
- `safeT = -radius / axisDir.y`
- 따라서 `newPos.y = tpos.y - radius`

문제점:

- ground height와 무관하다.
- 실제 목적이 ground 위로 올리는 것이라면 `safeY = groundY + radius`여야 한다.
- `safeT`가 `axisLen`보다 클 때 clamp가 없다.

개선:

```csharp
float safeY = groundHit.point.y + colliderRadius;
float safeT = (safeY - tpos.y) / axisDir.y;
safeT = Mathf.Clamp(safeT, 0f, axisLen);
```

### 28. `GetTerrainPos` 시그니처

```csharp
public static bool GetTerrainPos(ref Vector3 cpos, out Vector3 rpos, ref Vector3 tpos)
```

구문 의미:

- 입력 카메라 위치 `cpos`, 타겟 위치 `tpos`를 받아 Terrain 보정 위치 `rpos`를 반환한다.

문제점:

- `cpos`, `tpos`를 수정하지 않으므로 `ref`가 필요 없다.
- `rpos = cpos`로 시작하므로 out은 적절하지만, 더 단순히 `Vector3 cpos, Vector3 tpos`를 받으면 된다.

개선:

```csharp
public static bool TryResolveTerrainHeight(Vector3 cameraPos, float radius, out Vector3 resolvedPos)
```

### 29. `GetTerrainPos` Terrain 조회와 샘플

```csharp
Terrain terrain = GrTerrainManager.Instance?.GetTerrain(tpos);
...
float terrainPosY = terrain.GetPosition().y + terrain.SampleHeight(cpos) + radius;
```

구문 의미:

- 타겟 위치 기준 Terrain을 찾고, 카메라 위치의 높이를 샘플한다.

수학/로직 문제:

- Terrain tile이 여러 개라면 `tpos`가 속한 Terrain과 `cpos`가 속한 Terrain이 다를 수 있다.
- 다른 Terrain에 대해 `SampleHeight(cpos)`를 호출하면 heightmap 좌표 범위를 벗어나거나 엉뚱한 높이가 나올 수 있다.
- Unity `Terrain.SampleHeight(worldPosition)`는 world position 기준이지만 해당 Terrain의 heightmap 범위 밖 입력에 대한 의미가 제한적이다.

개선:

- `GetTerrain(cpos)`로 카메라 위치 Terrain을 찾는다.
- 타겟 Terrain과 카메라 Terrain이 다른 경우 별도 처리한다.

### 30. `GetTerrainPos` 보정 기준

```csharp
float radius = 0.5f;
...
if (cpos.y <= terrainPosY)
{
    rpos.y = terrainPosY;
    return true;
}
```

문제점:

- 반지름이 하드코딩되어 다른 충돌 함수의 `colliderRadius`와 다르다.
- `<=`라서 정확히 접촉한 상태도 매번 보정 true가 된다. 스무딩 로직이 true/false에 민감하면 상태가 떨릴 수 있다.
- Terrain normal을 고려하지 않고 world y만 올린다. 경사면에서 구체 반지름만큼 normal 방향으로 떨어지는 것과 y 방향으로 올리는 것은 다르다.

개선:

- radius를 인자로 받는다.
- 작은 epsilon을 둔다.
- 경사면 정확도가 중요하면 terrain normal 기반 보정을 검토한다.

### 31. `GetCameraColliderPosMultiProbe` 초기화

```csharp
cldCamPos = cpos;
hitNormal = Vector3.zero;
hitNormalIsFallback = false;
```

구문 의미:

- 기본 결과는 보정 없음이다.

평가:

- out 초기화는 적절하다.
- 다만 hitNormal이 zero인 상태로 반환될 수 있으므로 호출부가 zero normal을 처리해야 한다.

### 32. `GetCameraColliderPosMultiProbe` actor 검사

```csharp
var checkNoActors = SRGameManager.IsValidActorController == false ||
                 SRGameManager.Instance.ActorController.GameActorList == null ||
                 SRGameManager.Instance.ActorController.GameActorList.Count <= 0;

if (Application.isEditor == false && checkNoActors)
    return false;
```

구문 의미:

- actor controller가 없거나 actor list가 비어 있으면 에디터가 아닐 때 조기 return한다.

문제점:

- 물리 충돌 계산과 actor dissolve 처리 가능 여부가 섞였다.
- 런타임에서 actor가 없으면 벽/지형 충돌도 아예 하지 않는다.
- `SRGameManager.Instance.ActorController` 접근 안정성은 외부 보장에 의존한다.

개선:

- 이 블록은 삭제하고, actor list는 디졸브 처리 직전에만 가져온다.

### 33. `GetCameraColliderPosMultiProbe` axis와 basis

```csharp
Vector3 axis = cpos - tpos;
float axisLen = axis.magnitude;
if (axisLen < 1e-5f)
    return false;
Vector3 axisDir = axis / axisLen;
```

평가:

- MultiProbe에서는 단일 SphereCast와 달리 실제 위치 축을 쓰므로 방향 기준은 맞다.
- `curRot`는 필요 없다.

문제점:

- `axisLen < 1e-5f`에서 false 반환하지만 `cldCamPos = cpos`라 결과 위치는 유지된다.
- 후보 위치가 pivot과 같고 그 위치가 collider 내부인 경우 놓친다.

### 34. `GetCameraColliderPosMultiProbe` center probe

```csharp
if (Physics.Linecast(tpos, cpos, out RaycastHit centerHit, _camCldLayer))
{
    float projLen = Vector3.Dot(centerHit.point - tpos, axisDir);
    if (projLen < bestProjLen)
    {
        bestProjLen = projLen;
        bestNormal = centerHit.normal;
        isHit = true;
    }
}
```

평가:

- 중심선 hit를 projection 거리로 바꾸는 것은 합리적이다.

문제점:

- Linecast는 카메라 반지름을 고려하지 않는다.
- hit distance에서 skin이나 radius 보정을 빼지 않는다.
- 시작점이 collider 내부인 경우 기대와 다르게 동작할 수 있다.

개선:

- 중심선은 “시야선” 검사용으로만 쓰고, 실제 카메라 반지름은 SphereCast 또는 parallel probes로 처리한다.

### 35. `GetCameraColliderPosMultiProbe` ring probe

```csharp
Vector3 endpoint = cpos + offset;
if (Physics.Linecast(tpos, endpoint, out RaycastHit hit, _camCldLayer))
```

핵심 문제:

- 이 함수의 가장 중요한 수학 문제다.
- 카메라 반지름을 endpoint에만 적용하고 시작점에는 적용하지 않는다.
- 카메라가 이동하는 swept volume을 근사하지 못한다.

정확한 병렬 probe 모델:

```csharp
Physics.Linecast(tpos + offset, cpos + offset, out hit, mask)
```

대안:

- 사실 이 목적이면 `Physics.SphereCast(tpos, colliderRadius, axisDir, out hit, axisLen, mask)`가 더 직접적이다.
- MultiProbe를 쓰는 이유가 SphereCast 코너 지터링 완화라면, sphere cast와 parallel line probes를 결합해야 한다.

### 36. `GetCameraColliderPosMultiProbe` hit 반영

```csharp
bestProjLen = Mathf.Max(0f, bestProjLen);
cldCamPos = tpos + axisDir * bestProjLen;
hitNormal = bestNormal;
isMoveCamPos = true;
```

구문 의미:

- 가장 가까운 projection 거리만큼 축 위에 카메라를 배치한다.

문제점:

- `bestProjLen`에서 camera radius나 skin을 빼지 않는다.
- Linecast hit point가 벽 표면이라면 카메라 중심이 벽 표면에 놓일 수 있다.
- 이후 `ProcessColliderReviseMultiProbe`에서 normal skin을 더하지만, 이 함수 단독으로 쓰면 벽 표면에 붙는다.

개선:

- `safeDistance = Mathf.Max(0, bestProjLen - skinWidth)`를 이 함수 안에서 처리한다.

### 37. `GetCameraColliderPosMultiProbe` CheckSphere fallback

```csharp
if (!isMoveCamPos && Physics.CheckSphere(cldCamPos, colliderRadius, _camCldLayer))
{
    if (Physics.SphereCast(tpos, colliderRadius, axisDir, out var safeHit, axisLen, _camCldLayer))
        cldCamPos = tpos + axisDir * safeHit.distance;
    else
        cldCamPos = tpos;

    hitNormal = -axisDir;
    hitNormalIsFallback = true;
    isMoveCamPos = true;
}
```

구문 의미:

- Linecast가 아무것도 못 맞혔지만 최종 위치 구체가 collider와 겹치면 SphereCast로 안전 위치를 다시 찾는다.

문제점:

- `CheckSphere`는 최종 위치 overlap만 본다. 경로 중간의 반지름 충돌은 ring line들이 대신 잡는다는 전제인데, ring line 모델이 부정확하다.
- SphereCast도 시작 구체가 이미 overlap이면 hit를 보장하지 않는다.
- SphereCast 실패 시 `cldCamPos = tpos`로 피벗에 순간 이동한다. 이는 카메라 pop을 크게 만든다.
- fallback normal을 `-axisDir`로 둔다. 실제 표면 normal이 아니므로 이후 skin 적용은 skip되지만, 호출자가 normal을 다른 용도로 쓰면 오해할 수 있다.

락온 관련 영향:

- 한쪽 후보가 CheckSphere fallback에 걸려 `tpos`로 붙으면 reach가 0에 가까운 것처럼 동작한다.
- 락온 sign이 반대쪽으로 급변할 수 있다.

개선:

- 실패 시 이전 safe distance를 유지하거나 최소 거리로 clamp한다.
- `Physics.ComputePenetration`으로 overlap 해소 방향을 구하는 방법도 검토할 수 있다.

### 38. `GetCameraColliderPosMultiProbe` 디졸브 처리

```csharp
if (!checkNoActors && Physics.CheckSphere(cldCamPos, colliderRadius, _camColliderToDissolveLayer))
{
    var actorList = SRGameManager.Instance.ActorController.GameActorList;
    for (int i = 0; i < actorList.Count; i++)
    {
        ...
        if (actorList[i].ColliderEvent.CheckInPointByBodySize(cldCamPos))
            actorList[i].ColliderEvent.OnStartCameraCollision(colliderRadius, _camColliderToDissolveLayer);
    }
}
```

구문 의미:

- 보정된 카메라 위치가 캐릭터/HurtBox와 겹치면 actor list를 순회해 디졸브 이벤트를 보낸다.

문제점:

- 충돌 계산 함수가 side effect를 가진다.
- 카메라 위치 계산을 여러 번 호출하면 디졸브 이벤트가 여러 번 발생할 수 있다.
- 락온 후보 비교용으로 `Probe`/`Process`를 여러 후보에 호출하면, 실제 카메라가 가지 않을 후보 위치에서도 디졸브가 발생할 수 있다.

락온 관련 핵심:

- 락온은 보통 여러 후보 위치를 평가한다.
- 후보 평가 함수는 반드시 side effect가 없어야 한다.
- 이 함수는 디졸브 side effect가 있으므로 “후보 평가용”으로 쓰면 안 된다.

개선:

- 위치 계산과 디졸브 이벤트를 완전히 분리한다.
- 실제 최종 카메라 위치가 확정된 뒤에만 디졸브를 처리한다.

### 39. `GetCameraColliderPos` 초기 actor 검사

```csharp
var checkNoActors = ...
if (Application.isEditor == false && checkNoActors)
    return false;
```

문제점:

- MultiProbe와 동일하다.
- 단일 SphereCast 경로에서도 actor list가 없으면 벽 충돌 자체가 꺼진다.

개선:

- 조기 return 제거.

### 40. `GetCameraColliderPos` 회전 기반 방향

```csharp
var camFoward = curRot * Vector3.forward;
var targetToCam = -camFoward.normalized;
var targetHitLength = Vector3.Dot((tpos - cpos), camFoward);
```

핵심 수학:

- `camForward`는 카메라가 바라보는 방향이다.
- 일반 3인칭 카메라라면 카메라는 target을 바라보므로 `target -> camera`는 대략 `-camForward`다.
- 하지만 “대략”이지 항상 정확하지 않다.

틀어지는 경우:

- 카메라 위치에 shoulder offset이 있다.
- 카메라가 target을 정확히 바라보지 않고 lead/lag 보간 중이다.
- lock-on에서 target과 player 사이 중간점을 보며 카메라 위치는 다른 축에 있다.
- collision correction 이후 위치와 회전이 서로 다른 프레임 기준이다.

정확한 기준:

- 충돌 경로는 `tpos -> cpos`다.
- 따라서 `dir = (cpos - tpos).normalized`, `distance = (cpos - tpos).magnitude`가 맞다.

### 41. `GetCameraColliderPos` SphereCast

```csharp
bool isHit = Physics.SphereCast(tpos, colliderRadius, targetToCam.normalized, out var hitRay, targetHitLength, _camCldLayer);
```

문제점:

- `targetHitLength` 음수 가능성.
- `targetToCam`은 이미 normalized인데 다시 normalized한다.
- `QueryTriggerInteraction`이 명시되지 않는다.
- 시작점 구체가 target collider와 겹치는 경우, mask에 target collider가 있으면 문제가 된다.

락온 관련 영향:

- lock-on pivot이 player와 enemy 중간점으로 움직이면 `curRot.forward`와 `tpos -> cpos` 차이가 커질 수 있다.
- 이 경로는 락온에서 특히 취약하다.

개선:

```csharp
Vector3 axis = cpos - tpos;
float distance = axis.magnitude;
Vector3 dir = axis / distance;
Physics.SphereCast(tpos, colliderRadius, dir, out hit, distance, mask, QueryTriggerInteraction.Ignore);
```

### 42. `GetCameraColliderPos` Raycast fallback

```csharp
if (Physics.Raycast(tpos, targetToCam.normalized, out var rayHit, targetHitLength, _camCldLayer))
{
    isHit = true;
    hitRay = rayHit;
    hitRay.distance = Mathf.Max(0f, hitRay.distance - colliderRadius);
}
```

구문 의미:

- SphereCast가 놓친 경우 Raycast로 보조한다.

문제점:

- Raycast는 구체 두께가 없으므로 `distance - radius`로 보정한다. 아이디어는 맞지만 모든 표면 각도에서 정확하지 않다.
- 표면 normal과 ray direction의 각도에 따라 구체 중심이 닿는 지점은 단순히 ray distance - radius가 아니다. 평면에 대한 sphere contact distance는 normal 방향과 ray 방향의 dot에 따라 달라진다.
- `hitRay.distance`를 직접 수정하는 방식은 hit 정보 의미를 흐린다.

수학 보충:

- ray가 평면 normal을 정면으로 향하면 `distance - radius`가 근사적으로 맞다.
- 비스듬히 향하면 필요한 후퇴 거리는 `radius / dot(-dir, normal)`에 가까워진다.
- dot이 작을수록 더 많이 후퇴해야 한다.

개선:

```csharp
float denom = Vector3.Dot(-dir, rayHit.normal);
float retreat = denom > EPSILON ? colliderRadius / denom : colliderRadius;
float safeDistance = Mathf.Max(0f, rayHit.distance - retreat);
```

다만 이 fallback은 복잡하므로 가능하면 SphereCast/ComputePenetration 기반으로 통일하는 편이 낫다.

### 43. `GetCameraColliderPos` hit 반영

```csharp
cldCamPos = tpos + hitRay.distance * targetToCam.normalized;
isMoveCamPos = true;
```

문제점:

- `targetToCam`이 실제 `cpos - tpos`와 다르면 보정 위치가 후보 축에서 벗어난다.
- `hitRay.distance`가 Raycast fallback에서 수정된 값인지 SphereCast 원본인지 구분되지 않는다.

락온 관련 영향:

- lock-on camera가 적을 바라보면서 player 주변을 도는 경우, 회전 forward와 카메라 암 방향의 차이 때문에 보정 위치가 이상한 방향으로 붙을 수 있다.

개선:

- `safeDistance` 지역 변수와 `dir`을 별도로 둔다.

### 44. `GetCameraColliderPos` CheckSphere fallback

```csharp
if (!isMoveCamPos && Physics.CheckSphere(cldCamPos, colliderRadius, _camCldLayer))
{
    Vector3 actualDir = cpos - tpos;
    float actualDist = actualDir.magnitude;
    if (actualDist > 0f)
    {
        actualDir /= actualDist;
        if (Physics.SphereCast(tpos, colliderRadius, actualDir, out var safeHit, actualDist, _camCldLayer))
            cldCamPos = tpos + actualDir * safeHit.distance;
        else
            cldCamPos = tpos;
        isMoveCamPos = true;
    }
}
```

구문 의미:

- 최종 위치가 collider와 겹치면 실제 위치 축 기준으로 SphereCast를 다시 한다.

좋은 점:

- 여기서는 `actualDir = cpos - tpos`를 사용한다. 단일 SphereCast의 첫 경로보다 수학적으로 더 정확하다.

문제점:

- 왜 첫 SphereCast도 이 actualDir을 쓰지 않는지 일관성이 없다.
- SphereCast 실패 시 `tpos`로 순간 이동한다.
- `actualDist > 0f`만 검사하고 epsilon은 없다.

락온 관련 영향:

- 첫 SphereCast가 회전 기준으로 miss한 뒤 CheckSphere가 겹침을 감지하면, 두 번째 보정에서 갑자기 actualDir 기준으로 바뀐다. 이 전환 자체가 카메라 튐을 만들 수 있다.

개선:

- 처음부터 actualDir 기준으로 SphereCast한다.
- fallback 실패 시 `tpos`가 아니라 `tpos + actualDir * minDistance` 또는 이전 프레임 safe distance를 사용한다.

### 45. `GetCameraColliderPos` 디졸브 처리

MultiProbe의 디졸브 처리와 동일한 문제가 있다.

추가 문제:

- 단일 보정과 MultiProbe 보정에 같은 디졸브 코드가 중복된다.
- 한쪽 경로를 수정하면 다른 쪽 경로가 누락될 가능성이 높다.

개선:

- 공통 함수로 빼는 것도 가능하지만, 더 좋은 것은 충돌 계산에서 완전히 제거하는 것이다.

## 락온 관련 추가 분석

이 파일에서 락온과 직접 관련 있어 보이는 함수는 주석상 `ProbeCameraReachMultiProbe`다.

```csharp
/// ProcessColliderReviseMultiProbe 와 동일 기준이라 락온 sign 결정용 비교에 적합
```

즉 락온 중 카메라가 왼쪽/오른쪽/후방 후보 중 어느 쪽으로 가야 하는지 판단할 때, 각 후보 위치까지의 reach를 계산해 비교하려는 의도로 보인다.

### 락온 시 타겟이 멀수록 카메라가 캐릭터 앞쪽으로 넘어가는 문제

증상:

- 락온 타겟과 플레이어 사이 거리가 멀수록 카메라 기준점이 캐릭터보다 타겟 위치 쪽으로 많이 당겨진다.
- 그 결과 카메라가 플레이어 뒤가 아니라 플레이어와 타겟 사이, 심하면 플레이어보다 앞쪽에 배치된다.
- 플레이어 캐릭터가 화면에서 사라지거나, 카메라 뒤쪽/프레임 밖으로 밀린다.

핵심 원인:

- `CalcCameraTfm`은 전달받은 `tpos`를 카메라 배치 기준점으로 그대로 사용한다.

```csharp
var camRenderDist = Mathf.LerpUnclamped(_cameraPreset.minDistance, _cameraPreset.maxDistance, zoomRate);
var cameraVector = (Vector3.back * camRenderDist) + viewportVec;
rpos = tpos + (rrot * cameraVector);
```

- 즉 `tpos`가 플레이어 위치라면 카메라는 플레이어 기준으로 배치된다.
- 반대로 락온 로직에서 `tpos`를 플레이어와 타겟 사이 focus로 바꾸면, 카메라 전체가 그 focus 기준으로 이동한다.
- `LockOnState`에는 이 현상과 직접 관련 있어 보이는 상태가 있다.

```csharp
public Vector3 TargetPos;
public float BlendT;
public Vector3 ActiveFocusPos;
public float ActiveFocusRatio;
public float FreeFactor;
```

문제의 수학적 형태:

```csharp
Vector3 focus = Vector3.Lerp(playerPos, targetPos, focusRatio);
Vector3 cameraPos = focus + cameraBackVector;
```

플레이어와 타겟 사이 거리를 `L`, 카메라가 focus 뒤로 빠지는 거리를 `D`, focus 비율을 `a`라고 하면 플레이어 기준 카메라의 전후 위치는 대략 다음처럼 된다.

```text
player 기준 카메라 전방 이동량 ~= a * L - D
```

따라서 다음 조건이 되면 카메라가 플레이어보다 앞쪽으로 넘어갈 수 있다.

```text
a * L > D
```

중요한 점:

- `focusRatio`가 작아도 타겟 거리 `L`이 커지면 `a * L`은 계속 커진다.
- 즉 “비율 기반 focus”는 원거리 락온에서 월드 이동량이 과도하게 커지는 구조다.
- 이 문제는 충돌 보정만으로 해결되지 않는다. 충돌 보정은 `tpos -> cpos` 사이 장애물을 처리할 뿐, `tpos` 자체가 플레이어에서 멀어진 문제를 되돌리지 않는다.

확인해야 할 부분:

- 락온 로직에서 `ActiveFocusPos`, `ActiveFocusRatio`, `BlendT`, `TargetPos`를 계산하는 코드.
- `UpdateCamProperty(Vector3 targetPos)` 또는 그 호출부에서 전달되는 `targetPos`가 실제 플레이어 위치인지, 락온 focus 위치인지.
- 타겟 거리가 증가할 때 `ActiveFocusRatio`가 고정 비율로 유지되는지, 또는 월드 거리 상한이 있는지.
- 최종 카메라 위치 계산 후 플레이어가 viewport 안에 남아 있는지 검사하는 로직이 있는지.

수정 방향:

1. focus 이동을 비율만으로 계산하지 말고 월드 거리 상한을 둔다.

```csharp
Vector3 toTarget = targetPos - playerPos;
float targetDistance = toTarget.magnitude;
Vector3 dirToTarget = targetDistance > 1e-5f ? toTarget / targetDistance : Vector3.forward;

float focusOffset = Mathf.Min(targetDistance * focusRatio, maxFocusOffsetFromPlayer);
Vector3 focus = playerPos + dirToTarget * focusOffset;
```

2. 카메라 뒤 거리보다 focus 전진량이 커지지 않도록 상한을 둔다.

```csharp
float maxFocusOffset = Mathf.Max(0f, cameraBackDistance - minPlayerBehindCameraMargin);
focusOffset = Mathf.Min(focusOffset, maxFocusOffset);
```

3. ratio 상한을 거리 기반으로 계산한다.

```csharp
float maxRatioByCameraDistance = (cameraBackDistance - minPlayerBehindCameraMargin) / Mathf.Max(targetDistance, 1e-5f);
focusRatio = Mathf.Min(focusRatio, maxRatioByCameraDistance);
```

4. 최종 카메라 위치에서 플레이어 가시성을 검증한다.

```csharp
Vector3 camToPlayer = playerPos - cameraPos;
float playerDepth = Vector3.Dot(camToPlayer, cameraForward);
if (playerDepth <= minPlayerDepth)
{
    // focus를 player 쪽으로 되돌리거나 cameraBackDistance를 늘린다.
}
```

5. viewport 기준도 함께 검사한다.

```csharp
Vector3 playerViewport = camera.WorldToViewportPoint(playerPos);
bool playerVisible =
    playerViewport.z > 0f &&
    playerViewport.x >= minViewportX &&
    playerViewport.x <= maxViewportX &&
    playerViewport.y >= minViewportY &&
    playerViewport.y <= maxViewportY;
```

권장 결론:

- 원거리 락온에서 focus를 타겟 쪽으로 당기는 것은 연출상 필요할 수 있다.
- 그러나 focus는 반드시 “플레이어 기준 최대 이동 거리” 또는 “카메라 뒤 거리 기준 상한”을 가져야 한다.
- 락온 카메라의 불변 조건은 `타겟을 본다`가 아니라 `플레이어와 타겟을 모두 관리 가능한 화면 관계에 둔다`여야 한다.
- 이 증상은 `GameCameraCalculator`의 충돌 수식보다 `LockOnState.ActiveFocusPos/ActiveFocusRatio`를 소비해 `CalcCameraTfm`의 `tpos`로 넘기는 락온 pivot/focus 산정 로직에서 먼저 잡아야 한다.

### 락온 sign 결정에서 필요한 수학적 성질

락온 후보 비교 함수는 다음 성질이 있어야 한다.

- 같은 입력에는 항상 같은 reach를 반환해야 한다.
- 실제 최종 카메라 충돌 보정과 같은 기준을 써야 한다.
- 후보 평가 중 side effect가 없어야 한다.
- 후보별 비교값은 “카메라가 축 방향으로 얼마나 갈 수 있는가”를 의미해야 한다.
- 카메라 반지름과 skin이 동일하게 반영되어야 한다.
- target/player/self collider는 무시해야 한다.

현재 코드의 문제:

- `ProbeCameraReachMultiProbe`는 side effect가 없다는 점은 좋다.
- 하지만 ring probe 모델이 물리적으로 부정확하다.
- `ProcessColliderReviseMultiProbe`는 side effect가 있으므로 후보 평가에 쓰면 안 된다.
- `ProbeCameraReachMultiProbe`에는 skin, normal alignment, CheckSphere fallback이 없다.
- `ProcessColliderReviseMultiProbe`의 최종 위치는 normal skin과 floor rescue가 섞이므로, reach 함수 결과와 최종 보정 결과가 다르다.

### 왼쪽/오른쪽 후보 비교에서 생길 수 있는 오판정

예를 들어 플레이어 pivot 뒤쪽에 벽이 있고, 왼쪽 후보와 오른쪽 후보를 비교한다고 가정한다.

- 왼쪽 후보 desired: `pivot + leftCameraOffset`
- 오른쪽 후보 desired: `pivot + rightCameraOffset`
- 각 후보에 대해 `ProbeCameraReachMultiProbe`를 호출한다.

현재 ring probe는 `pivot -> desired + offset`으로 사선들을 쏜다. 이때 offset 방향은 후보 축 기준으로 매번 새로 계산된다. 후보가 좌우로 바뀌면 probe basis도 바뀐다. 즉 왼쪽 후보의 ring probe와 오른쪽 후보의 ring probe가 같은 월드 방향 샘플을 비교하지 않을 수 있다.

결과:

- 장애물 조건이 대칭이어도 reach가 미세하게 달라질 수 있다.
- 좁은 벽 모서리에서 왼쪽/오른쪽 sign이 매 프레임 바뀔 수 있다.
- 후보가 바뀌면 카메라 위치가 바뀌고, 다음 프레임 basis가 다시 바뀌어 흔들림이 증폭될 수 있다.

개선:

- 락온 sign 결정은 hysteresis가 필요하다.
- 예: 새 후보 reach가 현재 후보보다 `0.3m` 이상 좋고, 그 상태가 `0.15초` 이상 유지될 때만 sign 전환.
- 후보 reach 계산은 side effect 없는 순수 함수만 써야 한다.
- ring probe는 parallel probe로 바꾼다.

### 락온 후보 reach에 floor rescue를 섞으면 안 되는 이유

`ProcessColliderReviseMultiProbe`는 floor rescue를 포함하지만, `ProbeCameraReachMultiProbe`는 포함하지 않는다.

락온 sign 결정의 목적은 보통 “어느 쪽이 장애물에 덜 막히는가”다. floor rescue는 바닥 아래로 빠진 최종 위치를 안정화하는 후처리다.

따라서 sign 결정에 floor rescue를 섞으면 다음 문제가 생긴다.

- 낮은 지형/계단 때문에 후보가 나쁘다고 평가될 수 있다.
- 벽 회피 판단과 바닥 보정 판단이 섞인다.
- 락온 카메라가 장애물이 아니라 지형 높이 때문에 좌우로 바뀔 수 있다.

권장:

- sign 결정: 벽/장애물 reach만 비교.
- 최종 위치: 선택된 후보에 대해 floor rescue 적용.

### 락온 후보 reach에 normal skin을 섞는 방법

후보 비교는 scalar distance 비교가 핵심이다. 이때 normal 방향으로 위치를 밀면 scalar reach가 아니라 3D position 비교가 된다.

권장 방식:

```csharp
float reach = ProbeReach(pivot, desired, radius, probeCount, mask);
float safeReach = Mathf.Max(0f, reach - skinWidth);
```

normal skin은 최종 렌더 위치 안정화에는 쓸 수 있지만, 후보 sign 비교에는 `reach - skinWidth`처럼 축 방향 거리로 반영하는 것이 안정적이다.

### 락온 후보 평가용 추천 결과 타입

```csharp
public readonly struct CameraReachResult
{
    public readonly bool Valid;
    public readonly bool Blocked;
    public readonly float DesiredDistance;
    public readonly float ReachDistance;
    public readonly float ReachRatio;
    public readonly Vector3 FirstHitNormal;
}
```

락온에서는 `ReachDistance`만 보지 말고 `ReachRatio = ReachDistance / DesiredDistance`를 같이 보는 것이 좋다. 후보마다 desired distance가 다르면 절대 거리 비교가 불공정해질 수 있다.

### 락온 sign 전환 예시 정책

```csharp
if (candidate.ReachRatio > current.ReachRatio + 0.1f &&
    candidate.ReachDistance > current.ReachDistance + 0.3f &&
    candidateStableTime > 0.15f)
{
    SwitchSign(candidateSign);
}
```

이런 hysteresis가 없으면, 현재 코드처럼 probe 오차가 있는 상태에서는 sign이 쉽게 흔들린다.

## 수학적으로 우선 수정해야 할 코드 조각

### 단일 SphereCast 교체

```csharp
Vector3 axis = cpos - tpos;
float distance = axis.magnitude;
if (distance < 1e-5f)
{
    cldCamPos = cpos;
    return false;
}

Vector3 dir = axis / distance;
if (Physics.SphereCast(tpos, colliderRadius, dir, out RaycastHit hit, distance, _camCldLayer))
{
    cldCamPos = tpos + dir * Mathf.Max(0f, hit.distance);
    return true;
}
```

### MultiProbe parallel probe 교체

```csharp
Vector3 offset = (right * Mathf.Cos(a) + up * Mathf.Sin(a)) * colliderRadius;
Vector3 start = tpos + offset;
Vector3 end = cpos + offset;

if (Physics.Linecast(start, end, out RaycastHit hit, _camCldLayer))
{
    float alignment = Vector3.Dot(hit.normal, -axisDir);
    if (alignment >= minNormalAlignment)
    {
        float projLen = Vector3.Dot(hit.point - tpos, axisDir);
        bestProjLen = Mathf.Min(bestProjLen, projLen);
    }
}
```

### floor rescue 판정 교체

```csharp
float minCameraY = groundHit.point.y + colliderRadius;
if (cldCamPos.y >= minCameraY)
    return false;

if (Mathf.Abs(axisDir.y) < 1e-5f)
{
    cldCamPos.y = minCameraY;
    return true;
}

float safeT = (minCameraY - tpos.y) / axisDir.y;
safeT = Mathf.Clamp(safeT, 0f, axisLen);
cldCamPos = tpos + axisDir * safeT;
return true;
```

주의:

- 이 코드는 개념 예시다.
- 실제 적용 시 y 보정 후 `CheckSphere` 또는 재 probe가 필요하다.
