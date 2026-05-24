# 카메라 시스템 고도화 로드맵 설계

**작성일**: 2026-05-24 | **상태**: Tier 1·2 구현 완료 | 갱신: 2026-05-24 스크린샷 피드백 기반 카메라 튜닝

---

## 0. 구현 현황

2026-05-24 기준 Tier 1·2를 프로젝트 코드와 `CameraSettings.asset`에 반영했다.

### 0.1 완료 항목

| Tier | 항목 | 상태 | 주요 반영 파일 |
|---|---|---|---|
| Tier 1 | 속도 기반 동적 FOV | 완료 | `CameraDistanceController`, `CameraManager`, `CameraSettings` |
| Tier 1 | Look-ahead 오프셋 | 완료 | `InGameCameraMode`, `CameraRuntimeContext`, `CameraSettings` |
| Tier 1 | Floor Rescue | 완료 | `CameraCollision`, `InGameCameraMode`, `CameraSettings` |
| Tier 2 | MultiProbe 충돌 + Skin Width | 완료 | `CameraCollision`, `CameraSettings` |
| Tier 2 | 충돌 텔레메트리 | 완료 | `CameraCollision`, `CameraRuntimeContext`, `CameraManager` |
| Tier 2 | 락온 차폐 자동 리포지션 / SideFlip | 완료 | `CameraLockOn`, `CameraManager`, `CameraSettings` |
| Tier 2 | ActiveFocus 단일소스 XZ 스무딩 | 완료 | `CameraLockOn`, `CameraSettings` |
| Tier 2 | 락온 타겟팅 우선순위 옵션 | 완료 | `CameraLockOn`, `CameraSettings` |

### 0.2 반영된 세팅 기본값

- `enableSpeedFOV = true`, `speedFOVMax = 6`, `speedForMaxFOV = 8`, `speedFOVSmoothTime = 0.3`
- `enableLookAhead = true`, `lookAheadDistance = 1.2`, `lookAheadSpeedRef = 5`, `lookAheadSmoothTime = 0.25`, `lockOnLookAheadMultiplier = 0.1`
- `enableFloorRescue = true`, `floorRescueDropThreshold = 1`, `groundClearance = 0.3`
- `useMultiProbe = true`, `collisionProbeCount = 6`, `collisionSkinWidth = 0.08`, `minNormalAlignment = 0.5`
- `enableLockOnSideFlip = true`, `sustainedCollisionSec = 0.4`, `sideFlipCooldown = 1`, `sideFlipSmoothTime = 0.2`
- `lockOnFocusSmoothTime = 0.15`
- `lockOnPriorityMode = CameraDirection`
- 명조 케이스 스터디 제안값에서 실제 스크린샷 피드백을 반영해 튜닝: `fovExplore = 50`, `fovCombat = 54`, `fovLockOn = 50`, `defaultDistance = 5.0`, `combatDistance = 5.2`, `lockOnDistance = 4.0`, `maxDistance = 7.0`, `defaultOffset = (0, 1.0, 0)`, `combatOffset = (0.25, 1.0, 0)`, `crowdZoomOutDistance = 7.0`

`combatDistance`는 데이터 필드로 추가했지만, §14.4의 "강제 재줌 금지" 원칙에 따라 전투 진입만으로 사용자 줌 거리를 매 프레임 덮어쓰지는 않는다.

### 0.4 스크린샷 피드백 후 튜닝 메모

2026-05-24 인게임 스크린샷에서 캐릭터가 중앙에서 미묘하게 벗어나고, 명조보다 OTS 느낌이 강하다는 피드백이 있었다. §14.6.4의 OTS 오프셋 제안은 저신뢰 관례값이므로 탐험 카메라는 `defaultOffset.x = 0`으로 되돌려 중앙 프레이밍을 우선한다. 명조에 가까운 넓은 시야감은 수평 오프셋이 아니라 `defaultDistance = 5.0`, `fovExplore = 50`으로 보정한다. 전투 오프셋도 `combatOffset.x = 0.25`로 낮춰 과한 어깨너머 구도를 피한다.

### 0.3 검증

- `dotnet build UPlayground.sln --no-restore` 통과.
- 남은 경고는 기존 외부 패키지/Unity API 경고이며, Tier 1·2 구현으로 인한 컴파일 오류는 없다.

---

## 1. 배경 및 목표

### 1.1 현재 카메라 시스템 현황

카메라 시스템은 이미 성숙한 구조를 갖추고 있다:

- **오케스트레이터**: `CameraManager` (서브시스템 초기화·생명주기 관리)
- **모드 스택**: `CameraModeController` (InGame / Dialogue / Free / SnapshotSequence)
- **7개 서브시스템**: 
  - LockOn (락온 데이터 캡슐화, 오프셋 각, adaptive orbit SmoothDamp)
  - Collision (단일 SphereCast 기반 거리 제약)
  - DistanceController (다중적 줌, 상태별 FOV, 화면이탈 방지)
  - RotationTransition (부드러운 회전 전환)
  - EffectManager (블렌딩 가능한 이펙트 합성)
  - Shaker (카메라 흔들림)
  - KillCam (처치 연출)
- **데이터 주도**: `CameraSettings` (SO) + Addressables 로드

### 1.2 현재 강점

- 락온 감각: orbit offset, 자유오빗 블렌딩, overcome 로직, 적응형 SmoothDamp
- 블렌딩 가능한 이펙트 합성 (AAA급 수준)
- FOV 기반 maxSafeMag로 화면 이탈 방지
- 거리별 Pitch 제한, 커브 기반 오프셋 거리

### 1.3 발견된 약점과 개선 영역

이번 고도화는 두 출처를 근거로 한다:

1. **참고 코드** (`unsave/`) —  카메라 구현 델타 분석
2. **AAA 웹 조사** — Unreal, Cinemachine, 게임개발 블로그 사례

약점은 **3개 군집**에 집중됨:

| 군집 | 문제 | 출처 | 해결책 |
|---|---|---|---|
| **C1: 충돌 견고성** | 단일 SphereCast → 코너 지터, floor rescue 없음, hit-normal skin width 없음, 차폐물 페이드 없음 | unsave 코드 직접 커버 | MultiProbe + Skin Width, Floor Rescue |
| **C2: 다중 타겟 프레이밍** | 현재·참고 둘 다 단일 타겟만. 2인+ 그룹 프레이밍 부재 | AAA 조사 (Cinemachine) | 가중치 센터로이드 프레이밍 (Tier 3) |
| **C3: 동적 감각** | 속도 기반 FOV, look-ahead, camera lag 부재 | AAA 조사 (UE 블로그, racing games) | 속도 기반 FOV, Look-ahead 오프셋 |

### 1.4 관련 기존 문서

본 로드맵은 **서브시스템 수준 고도화**에만 집중하며, 모드 아키텍처는 재논의하지 않는다. 다음 문서와 함께 읽을 것:

- `Assets/docs/Complete/CAMERA_SYSTEM_GUIDE.md` — 현행 구조 총론
- `Assets/docs/Complete/CAMERA_MODE_ARCHITECTURE_DESIGN.md` — 모드 아키텍처
- `Assets/docs/TODO/camera-dialogue-snapshot-system.md` — 대화·스냅샷 시스템

---

## 2. 우선순위 로드맵 (요약 표)

| Tier | 기법 | 출처 | 대상 파일 | 가치 | 난이도 | 위험 |
|---|---|---|---|---|---|---|
| **T1** | 속도 기반 동적 FOV | AAA | CameraDistanceController | 높음 | 낮음 | 낮음 |
| **T1** | Look-ahead 오프셋 | AAA | InGameCameraMode | 높음 | 낮음 | 낮음~중간 |
| **T1** | Floor Rescue | unsave | CameraCollision | 높음 | 중간 | 낮음 |
| **T2** | MultiProbe 충돌 + Skin Width ★ | unsave+AAA | CameraCollision | 높음 | 중간 | 중간 |
| **T2** | 락온 차폐 자동 리포지션 / SideFlip ★ | unsave+AAA | CameraLockOn + Context | 높음 | 상 | 중간 |
| **T2** | ActiveFocus 단일소스 XZ 스무딩 | unsave | CameraLockOn | 중~높음 | 중간 | 중간 |
| **T2** | 락온 타겟팅 우선순위 옵션 (3모드) | 명조 | CameraLockOn | 중~높음 | 낮음~중간 | 낮음 |
| **T3** | 차폐물 디더 페이드 | AAA | 신규 URP Shader + 검출 | 중간 | 중~상 | 중간 |
| **T3** | 가중치 센터로이드 그룹 프레이밍 | AAA | CameraDistanceController | 중간 | 상 | 중간 |
| **T3** | IsInitialTransition 진입 안정화 | unsave | CameraLockOn | 중간 | 상 | 낮음 |
| **T3** | 예측 충돌 | AAA | CameraCollision | 낮음 | 중간 | 중간 |

**★**: unsave 코드와 AAA 조사가 교차검증된 고신뢰 항목 (우선 구현 추천).

---

## 3. Tier 1 — 즉시 착수 (구현 설계)

Tier 1 기법은 서로 독립적이며 저위험이다. 즉시 감각과 견고성을 향상시킨다.

### 3.1 속도 기반 동적 FOV

#### 목표

플레이어 수평 속도를 감지하여 달릴 때 FOV를 확대, 정지할 때 축소. 이는 속도감을 강화하고 시야 몰입감을 높인다.

#### 알고리즘

```
baseTarget = isLockOn ? fovLockOn : isCombat ? fovCombat : fovExplore
speed = 플레이어 수평 속도 (m/s)
addFov = Clamp01(speed / speedForMaxFOV) * speedFOVMax
targetFOV = baseTarget + addFov
_baseFOV = SmoothDamp(_baseFOV, targetFOV, ..., fovSmoothTime)
```

#### 의존성

- **플레이어 속도 provider 주입 필수**: 기존 `SetCombatStateProvider(Func<bool>)` 패턴을 따라 `SetPlayerVelocityProvider(Func<Vector3>)` 추가
- **CameraRuntimeContext** 확장: `Func<Vector3> PlayerVelocityProvider` 필드 추가
- **KCC 모터 velocity** 소스 사용

#### 신규 CameraSettings 필드

```csharp
[Header("=== 동적 FOV (속도 기반) ===")]
public bool enableSpeedFOV = true;
public float speedFOVMax = 8f;              // 최대 추가 FOV (도)
public float speedForMaxFOV = 8f;           // 이 속도에서 speedFOVMax 도달 (m/s)
public float speedFOVSmoothTime = 0.3f;     // 보간 시간
```

#### 코드 스케치

```csharp
// CameraDistanceController.UpdateFOV 확장 (기존 코드 직후)
private void UpdateFOV()
{
    float baseTarget = _context.IsLockOnActive
        ? _s.fovLockOn
        : _context.IsCombatMode
        ? _s.fovCombat
        : _s.fovExplore;

    float addFov = 0f;
    if (_s.enableSpeedFOV && _playerVelocityProvider != null)
    {
        Vector3 vel = _playerVelocityProvider();
        float speed = Vector3.ProjectOnPlane(vel, Vector3.up).magnitude;
        addFov = Mathf.Clamp01(speed / Mathf.Max(_s.speedForMaxFOV, 0.01f)) 
               * _s.speedFOVMax;
    }

    _targetFOV = baseTarget + addFov;
    _baseFOV = Mathf.SmoothDamp(
        _baseFOV, _targetFOV, 
        ref _fovVelocity, 
        _s.fovSmoothTime);
}

public void SetPlayerVelocityProvider(Func<Vector3> provider) 
    => _playerVelocityProvider = provider;
private Func<Vector3> _playerVelocityProvider;
```

#### 출처

- Unreal "Six Ingredients for a Dynamic Third Person Camera" (속도 기반 FOV)
- Racing game 카메라 관례

---

### 3.2 Look-ahead 오프셋 (진행방향 선행)

#### 목표

플레이어가 달릴 때 카메라 pivot을 진행방향 앞으로 오프셋. 플레이어가 보고 있을 방향에 더 많은 시야를 할당하여 임박한 장애물·적을 더 먼저 감지.

#### 알고리즘

```
velocityXZ = ProjectOnPlane(playerVelocity, up)
speed = velocityXZ.magnitude
normalized = velocityXZ.normalized
lookAhead = normalized * Clamp01(speed / lookAheadSpeedRef) * lookAheadDistance
appliedOffset = SmoothDamp(_appliedLookAhead, lookAhead, ..., lookAheadSmoothTime)

// 락온 중: 전투 추적 방해 방지를 위해 0 또는 축소
if (isLockOn) lookAhead *= 0.1f;  // 또는 0

pivot.position += appliedOffset  // Y축은 제외, XZ만
```

#### 의존성

- **플레이어 속도 provider**: 3.1과 동일
- **InGameCameraMode.UpdateOffsetAndDistance** 내부 pivot 계산 단계에서 추가
- **이미 보유**: camera lag (SpringDampCameraEffect로 부분 지원) — 신규 불필요

#### 신규 필드

```csharp
[Header("=== Look-ahead (진행방향 선행) ===")]
public bool enableLookAhead = true;
public float lookAheadDistance = 1.2f;      // 최대 선행 거리 (m)
public float lookAheadSpeedRef = 5f;        // 이 속도 이상에서 최대 (m/s)
public float lookAheadSmoothTime = 0.25f;   // 보간 시간 (짧게)
```

#### 주의

- 카메라 지연추종(lag)은 이미 `SpringDampCameraEffect`로 부분 지원됨
- Look-ahead는 **오프셋의 가중치** 조정이지, 또 다른 lag이 아님
- 락온 중에는 영향력 축소 권장 (일관된 추적 방해 방지)

---

### 3.3 Floor Rescue (계단/바닥 아래 빠짐 방지)

#### 목표

카메라가 플레이어 아래로 떨어지는 것을 방지. 계단을 내려갈 때, 경사로에서 카메라가 지형을 뚫고 가는 시각적 이상 제거.

#### 알고리즘 1: Floor Rescue

```
if (pivot.y - camPos.y > dropThreshold && axis는 아래 방향) {
    // pivot 발밑에서 down raycast
    if (raycast hit ground at groundY) {
        if (camPos.y < groundY + radius) {
            // 카메라를 axis 위에서 올려줌
            camPos.y = max(camPos.y, groundY + radius);
        }
    }
}
```

#### 알고리즘 2: Ground Clearance

```
// 카메라 바로 아래 raycast
if (raycast down from camPos, hit ground at groundY) {
    clearance = camPos.y - groundY;
    if (clearance < minClearance) {
        camPos.y = groundY + minClearance;  // near-plane 클립 방지
    }
}
```

#### 구조 설계

- **현재 `CameraCollision.Evaluate()`는 거리(float)만 반환** → Floor rescue는 **월드 위치(Vector3) 보정**이 필요
- **신규 메서드**: `ApplyFloorRescue(Vector3 pivot, ref Vector3 camPos, float deltaTime)`
- **호출 시점**: `InGameCameraMode` 카메라 위치 산출 직후 (EvaluatePose 내부 마지막 단계)
- **설계 이점**: 기존 거리 기반 로직 (SphereCast, MultiProbe)과 완전히 분리

#### 신규 필드

```csharp
[Header("=== Floor Rescue (바닥 보정) ===")]
public bool enableFloorRescue = true;
public float floorRescueDropThreshold = 1.0f;  // 이 거리 초과 시만 작동 (m)
public float groundClearance = 0.3f;           // 카메라와 지형 간 최소 거리 (m)
public LayerMask floorRescueLayerMask;         // Ground, Walkable 등
```

#### 출처

- `unsave/GameCameraCalculator.EnsureCameraNotBelowFloor` (line 263-300)
- `unsave/GameCameraCalculator.EnsureGroundClearance` (line 221-255)

---

## 4. Tier 2 — 핵심 견고성 (구현 설계)

Tier 2는 교차검증된 고신뢰 항목(★)이 포함되어 있으며, Tier 1보다 복잡도가 높다. **Phase 2 충돌 개선 → Phase 3 락온 개선** 순서 강력 권장.

### 4.1 MultiProbe 충돌 + Skin Width ★

#### 목표

코너 지터 제거 및 벽 clipping 방지. 단일 SphereCast 대신 여러 ray를 축 기준으로 원형 배치하여 더 견고한 거리 계산.

#### 알고리즘 개요

```
axisDir = (desiredCpos - pivot).normalized
직교 평면에 대해 중앙 1 + 원형 N개 Linecast 배치
각 hit point를 axis 방향으로 projection → 최단 길이 선택
hit normal과 -axisDir의 dot이 minNormalAlignment 이상이면
hit에서 normal 방향으로 skinWidth만큼 추가 보정
```

#### 상세 구현 흐름

```csharp
private float GetRaycastDistance(Vector3 pivot, Vector3 desiredCpos)
{
    Vector3 axisDir = (desiredCpos - pivot).normalized;
    
    // 직교 좌표 기저 구성
    Vector3 right = Vector3.Cross(axisDir, Vector3.up);
    if (right.sqrMagnitude < 1e-4f)
        right = Vector3.Cross(axisDir, Vector3.forward);
    right.Normalize();
    Vector3 upDir = Vector3.Cross(right, axisDir).normalized;
    
    float minReach = Vector3.Distance(pivot, desiredCpos);  // fallback
    
    // 중앙 linecast
    float centerReach = GetRaycastDistanceSingle(pivot, desiredCpos);
    if (centerReach > 0f) minReach = Mathf.Min(minReach, centerReach);
    
    // 원형 probe: N개 각도
    float angleStep = 360f / _s.collisionProbeCount;
    for (int i = 0; i < _s.collisionProbeCount; i++)
    {
        float angle = i * angleStep;
        float rad = Mathf.Deg2Rad * angle;
        
        // 원형 오프셋 (XZ 평면이 아닌, 축에 직교하는 평면)
        Vector3 offset = (Mathf.Cos(rad) * right + Mathf.Sin(rad) * upDir) 
                       * _s.cameraRadius;
        Vector3 probeStart = pivot + offset;
        Vector3 probeEnd = desiredCpos + offset;
        
        if (Physics.Linecast(probeStart, probeEnd, out RaycastHit hit, 
            _collisionLayerMask))
        {
            // projection으로 축 거리 환산
            Vector3 hitDir = hit.point - pivot;
            float projReach = Vector3.Dot(hitDir, axisDir);
            
            // normal alignment 필터
            float normalAlignment = Vector3.Dot(hit.normal, -axisDir);
            if (normalAlignment >= _s.minNormalAlignment)
            {
                // skin width 적용
                projReach -= _s.collisionSkinWidth;
                minReach = Mathf.Min(minReach, projReach);
            }
        }
    }
    
    return Mathf.Max(minReach, _s.minCollisionDistance);  // 하한선
}
```

#### 성능 고려

- Linecast는 SphereCast보다 저렴 (N+1회 여전히 저렴)
- 레이어: 기존 `CameraConfig.GetCollisionLayerMask()` 사용 (Character/Obstacle/WalkableGround 조합)
- 매 프레임 계산하되 짧은 거리면 일찍 리턴 가능

#### 신규 CameraSettings 필드

```csharp
[Header("=== 충돌 검사 (MultiProbe) ===")]
public bool useMultiProbe = true;
public int collisionProbeCount = 6;           // 원형 배치 수
public float collisionSkinWidth = 0.08f;      // 벽에서 뒤로 물러날 거리 (m)
[Range(0f, 1f)] public float minNormalAlignment = 0.5f;  // normal과 axis 각도 필터
```

#### 기존 필드 유지

- `cameraRadius`: 카메라 콜라이더 반경 (이미 존재, 재사용)
- 비대칭 거리 스무딩 (당김 즉시/복귀 SmoothDamp) — **유지**

#### 출처

- `unsave/GameCameraCalculator.ProcessColliderReviseMultiProbe` (line 102-186)
- `unsave/GameCameraCalculator.ProbeCameraReachMultiProbe` (line 102-186)
- Unreal Spring Arm documentation (corner jitter mitigation)

---

### 4.2 락온 차폐 자동 리포지션 / SideFlip ★

#### 목표

락온 중 카메라가 장애물에 가려지면 자동으로 좌우 오프셋 각을 반대로 전환. 플레이어가 일일이 카메라를 조작하지 않아도 적을 계속 볼 수 있다.

#### 아키텍처 변경 (유일한 시스템 수준 변경)

**CameraRuntimeContext에 신규 필드 추가**:

```csharp
public bool IsCameraColliding { get; set; }              // 현재 충돌 중?
public float CollisionSustainedSec { get; set; }         // 지속 시간
```

**업데이트 책임**:
- `CameraCollision.Evaluate()` 또는 `CameraManager`가 매 프레임 기록
- "현재 desired 위치에서 실제 카메라가 밀려났는가" 검사

#### 알고리즘 v1 (권장, unsave식)

```
if (isLockOn && collisionSustainedSec > sustainedCollisionSec) {
    // 차폐 지속 시간 초과 → SideFlip 트리거
    if (timeSinceLastSideFlip > sideFlipCooldown) {
        // 오프셋 각의 부호 반전
        currentOffsetAngle = -currentOffsetAngle;  // 또는 더 정교한 전환
        
        // 빠른 수렴 (기존 SmoothDamp보다 짧음)
        targetOffsetAngle = SmoothDamp(
            targetOffsetAngle, 
            newOffsetAngle, 
            ref _offsetVelocity, 
            sideFlipSmoothTime);  // 0.2f 정도
        
        timeSinceLastSideFlip = 0f;
    }
}
```

#### 알고리즘 v2 (고급, AAA식, 후속)

차폐 시 ±θ orbit 후보 위치를 `IsCameraPositionClear(Vector3 worldPos)` 샘플링해 막히지 않은 쪽 선택.
- 일반성은 높지만 비용·복잡도 증가 → v1로 먼저 검증 후 고려

#### 신규 필드

```csharp
[Header("=== 락온 차폐 자동 리포지션 ===")]
public bool enableLockOnSideFlip = true;
public float sustainedCollisionSec = 0.4f;    // 이 시간 이상 충돌 시 트리거 (s)
public float sideFlipCooldown = 1.0f;         // 연속 전환 방지 (s)
public float sideFlipSmoothTime = 0.2f;       // 전환 속도 (일반 orbitSmooth보다 빠름)
```

#### 구현 위치

- **CameraLockOn.cs**: `UpdateOffsetAngle()` 내부 상태 머신에 SideFlip 체크 추가
- **CameraRuntimeContext**: 충돌 텔레메트리 필드 추가
- **CameraManager / InGameCameraMode**: 충돌 상태 매 프레임 갱신

#### 출처

- `unsave/LockOnState` (IsInitialTransition, SideFlipPending, SustainedCollidingElapsedSec, line 40-72)
- AAA 카메라 기법 Q6 (lock-on occlusion reposition)

---

### 4.3 ActiveFocus 단일소스 XZ 스무딩

#### 목표

락온 타겟 위치가 즉시 갱신될 때 pivot·거리 계산이 튀는 것을 방지. 타겟 위치 추종을 XZ 평면에서도 SmoothDamp로 평활화.

#### 알고리즘

```
// 현재 (Y만 SmoothDamp)
targetFocusY = SmoothDamp(_targetFocusY, CurrentTarget.position.y, ..., lockOnFocusSmoothTime)
pivot.position = new Vector3(
    CurrentTarget.position.x,  // 즉시
    targetFocusY,
    CurrentTarget.position.z   // 즉시
);

// 개선 (ActiveFocus: XYZ 모두 SmoothDamp)
_activeFocusPos = SmoothDamp(
    _activeFocusPos, 
    CurrentTarget.position, 
    ref _activeFocusVelocity, 
    _s.lockOnFocusSmoothTime);

pivot.position = _activeFocusPos;  // 간단하고 일관
```

#### 효과

- 타겟의 순간 위치 점프 (공격 회피, 몬스터 텔레포트 등) 완화
- orbit yaw/pitch 계산이 부드러워짐
- 락온 감각이 "카메라가 타겟을 유연하게 따라감" 인상

#### 신규 필드

```csharp
[Header("=== 락온 포커스 스무딩 ===")]
public float lockOnFocusSmoothTime = 0.15f;   // 타겟 위치 추종 (s, 기존 Y 전용과 유사)
```

#### 구현 위치

- **CameraLockOn.cs**: 신규 private 필드 `_activeFocusPos`, `_activeFocusVelocity` 추가
- **UpdateOffsetAndDistance()**: `pivot.position = _activeFocusPos` (기존 rawTarget 대체)

#### 출처

- `unsave/LockOnState.ActiveFocusPos / Velocity / Ratio` (line 59-64)

---

### 4.4 락온 타겟팅 우선순위 옵션 (명조 차용)

#### 목표
현재 `CameraLockOn.CollectTargets`는 (거리 + 카메라방향 dot) 고정 가중치(cameraWeight=0.5)로 타겟을 선정한다. 명조는 이를 **3가지 플레이어 선택 모드**로 노출해 다양한 플레이 스타일을 지원한다. 이 선정 가중치를 설정으로 외부화한다.

#### 3가지 우선순위 모드 (명조 [확인됨])
| 모드 | 기준 | 적합 플레이 |
|---|---|---|
| MovementDirection | 플레이어 이동 방향에 가까운 적 우선 | 진행방향 지향 플레이 |
| CameraDirection | 카메라 정면에 가까운 적 우선 (현재 기본 동작에 가까움) | 카메라 중심 플레이 |
| Distance | 최단 거리 적 우선 | 근접 난전 |

#### 설계
- `CameraSettings`에 enum과 모드 필드 추가:
```csharp
public enum LockOnPriorityMode { MovementDirection, CameraDirection, Distance }

[Header("=== 락온 타겟팅 우선순위 ===")]
public LockOnPriorityMode lockOnPriorityMode = LockOnPriorityMode.CameraDirection;
```
- `CollectTargets`의 sortScore 계산을 모드 분기로 변경: CameraDirection은 기존 (distScore + angleScore*cameraWeight) 유지, Distance는 distScore만, MovementDirection은 angleScore의 기준 벡터를 카메라 forward 대신 플레이어 이동방향(velocityXZ)으로 교체.
- 의존성: MovementDirection 모드는 §3.1/§3.2와 동일한 PlayerVelocityProvider 필요.

#### 주의 (명조 함정 회피)
명조 커뮤니티는 거리 우선 기본값이 "화면 밖 먼 적을 선택"하는 불만을 보고했다 [커뮤니티 관찰]. 따라서 본 프로젝트는 **CameraDirection을 기본값**으로 두고, 어떤 모드든 화면 밖(프러스텀 밖) 적은 후순위로 강등하는 보정을 유지한다.

---

## 5. Tier 3 — 상황적/별도 자산/후순위 (설계 개요)

Tier 3 기법은 선택적이며, 플레이테스트 피드백에 따라 우선순위가 바뀔 수 있다.

### 5.1 차폐물 디더 페이드

#### 목표

카메라→pivot 사이의 차폐 렌더러를 투명도로 페이드아웃. 카메라 당김과 다른 **미학적 대안**.

#### 알고리즘 개요

```
// 카메라와 pivot 사이 구 범위 내 차폐 렌더러 검출
RaycastAll / CheckSphere (Character/Obstacle layer)

// 각 렌더러의 거리를 기반으로 dither fade 파라미터 계산
distance = Vector3.Distance(renderer.bounds.center, camPos)
fadeAlpha = Smoothstep(fadeStart, fadeEnd, distance)

// MaterialPropertyBlock으로 fade 파라미터 주입 (재질 공유 유지)
mpb.SetFloat("_DitherFade", fadeAlpha);
renderer.SetPropertyBlock(mpb);

// 해제 시 복원
renderer.SetPropertyBlock(null);
```

#### 필요 자산

1. **신규 URP Shader**: 기본 Unlit / Standard에 Bayer 4x4 dither clip 추가
   - `clip(lerp(1.0, frac(pos.x * 2.0 + pos.y * 3.0), _DitherFade) - 0.5);`
   - 또는 Shader Graph Dither 노드 활용
2. **렌더러 상태 관리**: 페이드 중인 렌더러 캐시 유지

#### 출처

- UE "50 Camera Mistakes" (camera push vs. dither fade)
- Unity Shader Graph: https://docs.unity3d.com/Packages/com.unity.shadergraph@6.9/manual/Dither-Node.html
- Godot Dither Shader: https://godotshaders.com/shader/camera-occlusion-dither/

#### 주의

- 카메라 당김(4.1 MultiProbe)과의 관계: **병행 옵션** (둘 다 활성화 가능, 상황에 따라 선택)
- 차폐물 해제 시 복원 로직 필수 (메모리 누수 방지)

---

### 5.2 가중치 센터로이드 그룹 프레이밍

#### 목표

전투 중 적 2인+ 대상 시 가중 중심점으로 카메라 pivot 및 거리를 **소프트 블렌딩**. 두 적 사이의 공간을 모두 담을 수 있다.

#### 알고리즘 개요

```
targetGroup = [CurrentTarget, SecondaryTargets...]
groupCenter = Σ(position * weight) / Σ(weight)

// 기존 단일 타겟 orbit 계산 대신 groupCenter 사용
pivot.position = groupCenter + cameraOffset

// 거리는 bounding sphere로 자동 조정 (선택)
groupRadius = max distance from groupCenter to any target
adaptiveDistance = Mathf.Clamp(baseDistance, minDist, maxDist)
```

#### 권장 사항

- **주의**: God of War, Sekiro는 실제로 단일 타겟 유지, 다수 적은 카메라 후퇴 또는 전환으로 대응
- **본 프로젝트**: 기존 `lockOnMidPointWeight` 필드 재활용 (미사용 상태)
- **옵션화**: 소프트 센터로이드로 두 적 **사이**를 본다는 개념, 락온 대체 아님

#### 신규 필드 (기존 활성화)

```csharp
[Header("=== 그룹 프레이밍 (선택) ===")]
public bool enableGroupFraming = false;        // 기본 비활성
public float groupCenterSmoothTime = 0.3f;
[Range(0f, 1f)] public float secondaryTargetWeight = 0.4f;
```

#### 구현 위치

- **CameraLockOn.cs**: `UpdateOffsetAndDistance()` → `EvaluateFocusPoint()` (신규) 추상화
- **CameraDistanceController.cs**: 그룹 반지름 기반 거리 자동 조정 (선택)

#### 출처

- Cinemachine Target Group: https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/GroupingTargets.html

---

### 5.3 IsInitialTransition 진입 안정화

#### 목표

락온 진입 직후 짧은 시간(0.2초) 동안 카메라 움직임을 안정화. 초기 오프셋 각 고정, FOV/거리 변화에 무관한 maxSafeMag 보정.

#### 알고리즘 개요

```
if (isEnteringLockOn && elapsedSec < lockOnInitialStabilizeSec) {
    // 1. 오프셋 각을 EnterTargetMag로 고정
    targetOffsetAngle = enumerTargetAngle;
    
    // 2. maxSafeMag 축소 무시 (fov 변화 등으로 인한 과도한 당김 방지)
    maxSafeMag = EnterMaxSafeMag;
    
    // 3. InitialReleaseFactor로 부드럽게 해제
    t = elapsedSec / lockOnInitialStabilizeSec;
    releaseFactor = Smoothstep(0, 1, t, InitialReleaseFactor);
    maxSafeMag = Lerp(EnterMaxSafeMag, computedMaxSafeMag, releaseFactor);
}
```

#### 신규 필드

```csharp
[Header("=== 락온 진입 안정화 ===")]
public float lockOnInitialStabilizeSec = 0.2f;
```

#### 효과

- 단독으로는 약한 효과 — **4.2 SideFlip + 4.3 ActiveFocus와 묶어 시너지**
- 네 가지 진입 연출(오프셋·거리·FOV·타겟전환)이 동시에 일어날 때 지터 완화

#### 출처

- `unsave/LockOnState` (IsInitialTransition, EnterTargetMag, EnterMaxSafeMag, InitialReleaseFactor, line 40-54)

---

### 5.4 예측 충돌 (후순위)

#### 목표 및 현황

플레이어 velocity 외삽으로 다음 프레임 카메라 위치 선검사. 벽에 부딪히기 전에 사전에 당김.

#### 논평

- **근거 약함**: 자율주행 자료 위주 (게임 카메라 문헌 부족)
- **MultiProbe(4.1)와의 관계**: MultiProbe가 대부분의 지터 해소하므로 **후순위**
- **필요 시**: 극도로 빠른 카메라(경쟁/경주 게임) 또는 매우 좁은 공간에서만 고려

#### 스킵 권장

현재 프로젝트 TPS 액션 게임 맥락에서는 Tier 1, 2만으로 충분. Tier 3.4는 보류.

---

## 6. 기존 보유 기능 (중복 구현 금지)

다음은 이미 구현되어 있거나 충분히 유사한 기능이다. **본 로드맵에서 중복 구현하지 말 것**:

| 기능 | 파일/위치 | 상태 |
|---|---|---|
| 적응형 Orbit SmoothDamp | CameraLockOn (line 246-251) | ✓ 완성 |
| FOV 기반 maxSafeMag 화면이탈방지 | CameraLockOn (line 225-233) | ✓ 완성 |
| 거리기반 FreeFactor smoothstep | CameraLockOn (line 201-204) | ✓ 완성 |
| Overcome 로직 (적이 카메라 뒤에서 접근) | CameraLockOn (line 206-218) | ✓ 완성 |
| 거리별 Pitch 제한 | CameraLockOn (line 267-269) | ✓ 완성 |
| 커브기반 오프셋거리 | CameraLockOn (line 221-222) | ✓ 완성 |
| 다수적 줌아웃 | CameraDistanceController (line 93-114) | ✓ 완성 |
| 상태별 FOV 전환 | CameraDistanceController (line 46-56) | ✓ 완성 |
| 화면X 정렬 타겟전환 | CameraLockOn (line 401-423) | ✓ 완성 |
| 해제 전환연출 (2단계) | CameraLockOn (line 282-301) | ✓ 완성 |
| 카메라 lag (스프링 댐핑) | SpringDampCameraEffect | ✓ 완성 |

---

## 7. 부적합/제외 (참고코드에서 채택 안 함)

다음 기능은 참고 코드(unsave)에도 있지만, 현재 프로젝트 아키텍처상 **중복 또는 범위 외**로 판단되어 제외한다:

| 기능 | 이유 |
|---|---|
| **프리셋 키 시스템** (SRCameraPreset) | 현재 CameraModeController + CameraSettings(SO)로 대응 중 → 중복 |
| **모바일 터치 스크롤** (TOUCH_STATE) | PC InputSystem 기반 → 범위 외 |
| **ContainsFrustum 뷰포트 검사** | 충돌/락온과 독립, 현재 불필요 |

---

## 8. 구현 순서 권장 (단계별 다이어그램)

```
┌─────────────────────────────────────────────────────────────────┐
│ Phase 0: 선행 작업                                               │
├─────────────────────────────────────────────────────────────────┤
│ ▶ CameraRuntimeContext에 PlayerVelocityProvider 필드 추가         │
│ ▶ GameManager / CameraManager에서 PlayerVelocityProvider 주입    │
│   (KCC Motor에서 velocity 취득)                                   │
└─────────────────────────────────────────────────────────────────┘
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Phase 1: Tier 1 (독립적, 저위험) — 병렬 가능                    │
├─────────────────────────────────────────────────────────────────┤
│ ▶ 3.1 속도 기반 동적 FOV                                         │
│       CameraDistanceController.UpdateFOV 확장                    │
│       CameraSettings 필드 추가 (enableSpeedFOV 등)               │
│                                                                   │
│ ▶ 3.2 Look-ahead 오프셋                                         │
│       InGameCameraMode.UpdateOffsetAndDistance 확장              │
│       CameraSettings 필드 추가 (enableLookAhead 등)              │
│                                                                   │
│ ▶ 3.3 Floor Rescue                                              │
│       CameraCollision.ApplyFloorRescue() 신규 메서드             │
│       InGameCameraMode.EvaluatePose 호출 지점 추가               │
│       CameraSettings 필드 추가 (enableFloorRescue 등)            │
│                                                                   │
│ ⏱️ 예상 시간: 3~4시간 (각 1~1.5시간)                            │
└─────────────────────────────────────────────────────────────────┘
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Phase 2: Tier 2 충돌 개선 (Phase 1 완료 후)                    │
├─────────────────────────────────────────────────────────────────┤
│ ▶ 4.1 MultiProbe 충돌 + Skin Width                              │
│       CameraCollision.GetRaycastDistance 전체 재작성             │
│       축 기저 + 원형 probe linecast 로직                         │
│       CameraSettings 필드 추가 (useMultiProbe 등)                │
│                                                                   │
│ ▶ CameraRuntimeContext 충돌 텔레메트리 노출                      │
│       IsCameraColliding, CollisionSustainedSec 필드              │
│       CameraManager에서 매 프레임 갱신 로직                       │
│                                                                   │
│ ⏱️ 예상 시간: 4~5시간 (구현+테스트 포함)                        │
└─────────────────────────────────────────────────────────────────┘
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Phase 3: Tier 2 락온 개선 (Phase 2 완료 후)                    │
├─────────────────────────────────────────────────────────────────┤
│ ▶ 4.3 ActiveFocus 단일소스 XZ 스무딩 (선행, 독립적)             │
│       CameraLockOn: _activeFocusPos 필드 추가                    │
│       UpdateOffsetAndDistance에서 사용                           │
│       CameraSettings 필드 추가 (lockOnFocusSmoothTime)           │
│                                                                   │
│ ▶ 4.2 락온 차폐 자동 리포지션 / SideFlip                       │
│       (4.3 완료 + Phase 2 충돌 텔레메트리 의존)                 │
│       CameraLockOn.UpdateOffsetAngle()에 SideFlip 로직           │
│       CameraSettings 필드 추가 (enableLockOnSideFlip 등)         │
│                                                                   │
│ ⏱️ 예상 시간: 3~4시간                                          │
└─────────────────────────────────────────────────────────────────┘
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Phase 4: Tier 3 선택 (플레이테스트 피드백 기반)                 │
├─────────────────────────────────────────────────────────────────┤
│ ▶ 5.1 차폐물 디더 페이드   (중/중~상/중) — 별도 Shader 필요   │
│ ▶ 5.2 센터로이드 그룹 프레이밍 (중/상/중) — 복잡도 높음         │
│ ▶ 5.3 IsInitialTransition 안정화 (중/상/낮음) — 선택적        │
│ ▶ 5.4 예측 충돌 (낮음) — 후순위                                │
│                                                                   │
│ ⏱️ 예상 시간: 플레이테스트 결과에 따라                          │
└─────────────────────────────────────────────────────────────────┘
```

### 핵심 의존성

| 작업 | 선행 조건 |
|---|---|
| 3.1, 3.2 (동적 FOV, Look-ahead) | Phase 0 PlayerVelocityProvider 주입 |
| 4.2 SideFlip | Phase 2 충돌 텔레메트리 (IsCameraColliding) |
| 4.3 ActiveFocus | 없음 (독립적) |
| 4.4 타겟팅 우선순위 (MovementDirection 모드) | PlayerVelocityProvider 주입 (CameraDirection/Distance 모드는 의존성 없음) |
| 5.2 센터로이드 | 선택사항, 의존성 없음 |

---

## 9. 출처 및 참고 자료

### 9.1 unsave 참고 코드

참고 코드 경로: `unsave/` (카메라 코드, 컴파일 비대상 참고용 폴더). 본 로드맵은 코드 복사가 아니라 **알고리즘·설계 패턴을 본 프로젝트 구조에 맞게 재구현**하는 것을 전제로 한다.

| 파일 | 주요 함수 | 라인 | 용도 |
|---|---|---|---|
| `GameCameraCalculator.cs` | `ProcessColliderReviseMultiProbe` | 102-186 | MultiProbe 충돌 알고리즘 |
| `GameCameraCalculator.cs` | `ProbeCameraReachMultiProbe` | 157-186 | 링 probe 도달 거리 측정 |
| `GameCameraCalculator.cs` | `EnsureCameraNotBelowFloor` | 263-300 | Floor Rescue 알고리즘 |
| `GameCameraCalculator.cs` | `EnsureGroundClearance` | 221-255 | Ground Clearance (near-plane 클립 방지) |
| `GameCameraCalculator.cs` | `IsCameraPositionClear` | 188-216 | 위치 가시성 검사 |
| `LockOnState.cs` | `ActiveFocusPos / Velocity / Ratio` | 59-64 | XYZ 스무딩 포커스 |
| `LockOnState.cs` | `SideFlipPending / SustainedCollidingElapsedSec` | 66-72 | 차폐 감지 및 SideFlip 상태 |
| `LockOnState.cs` | `IsInitialTransition / EnterTargetMag` | 40-54 | 진입 안정화 |
| `ZoomCollisionState.cs` | | | length 도메인 SmoothDamp 줌 모델 |

### 9.2 AAA 웹 조사 출처

| 주제 | URL | 활용 기법 |
|---|---|---|
| Six Ingredients for a Dynamic Third Person Camera | https://www.unrealengine.com/en-US/tech-blog/six-ingredients-for-a-dynamic-third-person-camera | 속도 기반 FOV, look-ahead, 거리 조정 |
| Unreal Spring Arm Documentation | https://dev.epicgames.com/documentation/en-us/unreal-engine/using-spring-arm-components | MultiProbe 개념, 거리 평활화 |
| Cinemachine Target Group | https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/GroupingTargets.html | 가중치 센터로이드, bounding sphere |
| Unity Shader Graph Dither Node | https://docs.unity3d.com/Packages/com.unity.shadergraph@6.9/manual/Dither-Node.html | Dither fade shader |
| Godot Camera Occlusion Dither | https://godotshaders.com/shader/camera-occlusion-dither/ | 차폐물 페이드 구현 사례 |
| Little Polygon "Third Person Cameras" | https://blog.littlepolygon.com/posts/cameras/ | 일반적인 TPS 카메라 기법 |
| RyanJuckett Damped Springs | https://www.ryanjuckett.com/damped-springs/ | SmoothDamp 수학 기초 |
| GDC "50 Camera Mistakes" | https://gdcvault.com/play/1020460/50-camera | 카메라 설계 안티패턴 및 해결책 |

### 9.3 프로젝트 문서

- `Assets/docs/Complete/CAMERA_SYSTEM_GUIDE.md` — 현행 카메라 구조 총론
- `Assets/docs/Complete/CAMERA_MODE_ARCHITECTURE_DESIGN.md` — 모드 아키텍처 상세
- `Assets/docs/TODO/camera-dialogue-snapshot-system.md` — 대화·스냅샷 시스템 설계

### 9.4 명조(Wuthering Waves) 조사 출처

> 명조는 비공개 상용 게임으로 내부 구현 문서가 없다. 아래는 **노출 설정·관찰 거동·커뮤니티 분석** 기반이며, 신뢰도를 [확인됨/커뮤니티 관찰/추정]으로 구분한다.

| 주제 | URL | 신뢰도 |
|---|---|---|
| 카메라 FOV·줌 설정 방법 (Prima Games) | https://primagames.com/gaming/how-to-change-fov-and-zoom-the-camera-out-in-wuthering-waves | 확인됨 |
| PC/모바일 권장 설정 (Game8) | https://game8.co/games/Wuthering-Waves/archives/453931 | 확인됨 |
| 조작·타겟팅 목록 (Game8) | https://game8.co/games/Wuthering-Waves/archives/455867 | 확인됨 |
| 퍼펙트 닷지 방법 (Game8) | https://game8.co/games/Wuthering-Waves/archives/456639 | 확인됨 |
| 전투 가이드: 패링·닷지 타이밍 (Buffget) | https://buffget.com/news/wuthering-waves-combat-guide-perfect-parry-and-dodge-timings-for-bosses-m7mw7k | 커뮤니티 관찰 |
| 명조 vs 원신 비교 (TheGamer) | https://www.thegamer.com/wuthering-waves-genshin-impact-comparison/ | 커뮤니티 관찰 |
| 공명해방 시네마틱 (YouTube) | https://www.youtube.com/watch?v=tESMYNoWsak | 추정 |

---

## 10. 검증 및 플레이테스트 가이드

### 10.1 Tier 1 검증

| 기법 | 검증 방법 | 목표 |
|---|---|---|
| 속도 기반 FOV | 스프린트/걷기/정지 시 FOV 부드럽게 변화 확인 | 지터 없음, 감각적 응답성 |
| Look-ahead | 커브길 스프린트 → 선행 오프셋으로 앞 시야 확대 | 임박 장애물 감지 개선 |
| Floor Rescue | 계단·경사로 내려가기 → 카메라가 지형 클립 안 함 | 시각 안정성 |

### 10.2 Tier 2 검증

| 기법 | 검증 방법 | 목표 |
|---|---|---|
| MultiProbe | 코너·좁은 공간에서 카메라 당김 → 지터 최소화 | 거리 변화 부드러움 |
| SideFlip | 락온 중 장애물 뒤로 이동 → 카메라 자동 회전 | 적 차폐 해제 |
| ActiveFocus | 타겟이 순간 이동 (테스트용 스크립트) → pivot 부드러움 | 거리·회전 튀김 제거 |

### 10.3 플레이테스트 체크리스트

- [ ] Phase 1 완료 후 전투 감각 (속도감, 시야)
- [ ] Phase 2 완료 후 충돌 안정성 (코너, 유리창, 가구)
- [ ] Phase 3 완료 후 락온 견고성 (차폐, 타겟전환)
- [ ] Tier 3 필요성 평가 (디더 페이드, 그룹 프레이밍 등)

---

## 11. 일정 및 리소스

> ⚠️ 아래 시간 수치는 검증되지 않은 **대략적 상대 규모** 참고치다. 실제 공수는 코드 숙련도·테스트 범위에 따라 달라진다. 절대 일정으로 사용하지 말 것.

### 예상 소요 시간 (상대 규모)

| Phase | 작업 | 시간 | 비고 |
|---|---|---|---|
| Phase 0 | 선행 (PlayerVelocityProvider 주입) | 0.5~1시간 | 병렬화 불가 |
| Phase 1 | Tier 1 (3개 기법) | 3~4시간 | 병렬 가능 |
| Phase 2 | Tier 2 충돌 (MultiProbe + 텔레메트리) | 4~5시간 | Phase 1 완료 후 |
| Phase 3 | Tier 2 락온 (ActiveFocus + SideFlip) | 3~4시간 | Phase 2 완료 후 |
| **소계 (Tier 1, 2)** | | **10.5~14시간** | |
| Phase 4 | Tier 3 (선택, 플레이테스트 기반) | 2~8시간 | 피드백 기반 |

### 개발 순서 추천

1. **Phase 0 선행**: PlayerVelocityProvider 주입 (꼭 필요)
2. **Phase 1**: 3개 Tier 1 기법 병렬 구현, 테스트
3. **Phase 2**: MultiProbe 충돌 전체 재작성, 충돌 텔레메트리 노출
4. **Phase 3**: ActiveFocus → SideFlip 순서로 (4.2는 4.3과 Phase 2에 의존)
5. **Phase 4**: 플레이테스트 피드백 수집 후 Tier 3 우선순위 결정

---

## 12. 마이그레이션 및 하위호환성

### 12.1 기존 저장 데이터

- `CameraSettings.asset` 하향호환성: 신규 필드는 모두 기본값(enable=true, 수치 안전) 설정
- 기존 씬의 카메라 모드 기능: 신규 기법 활성화 여부로 점진적 도입 가능

### 12.2 테스트 전 체크리스트

- [ ] 신규 필드 기본값 설정 (enable flag 포함)
- [ ] 기존 PlayTest Scene에서 카메라 거동 동일 확인 (신규 기법 비활성화)
- [ ] 로컬 빌드에서 성능 측정 (프로파일링)

---

## 14. 명조(Wuthering Waves) 카메라 케이스 스터디

쿠로게임즈 명조(오픈월드 액션RPG, 회피·패링 중심 전투)는 본 프로젝트(소울라이크 TPS)와 전투 결이 가깝다. 내부 구현은 비공개이므로 **노출 설정·관찰 거동·커뮤니티 분석**을 근거로 하며, 신뢰도 태그를 병기한다.

### 14.1 차용 항목 (현재 시스템 대비)

| 명조 특징 | 현재 시스템 상태 | 판정 | 반영 위치 |
|---|---|---|---|
| 타겟팅 우선순위 3모드 (이동/카메라/거리) [확인됨] | CollectTargets 고정 가중치(cameraWeight=0.5) | **신규 채택** | §4.4 |
| 탐험/전투 카메라 거리 분리 슬라이더 [확인됨] | offset·FOV·lockOnDistance로 내부 구분만, 플레이어 노출 분리 거리 없음 | **옵션 레이어(후순위)** | §14.2 |
| 극한회피 Bullet Time 슬로모 [확인됨] | KillCamController + TimeScaleCameraEffect + PerfectGuardFOV(이미 로드) 인프라 보유 | **기존 인프라 활용(신규 기술 불필요)** | §14.3 |
| 자동 카메라 보정의 멀미·강제 재줌 불만 [커뮤니티 관찰] | align·락온 자동회전 보유 | **안티패턴 주의** | §14.4 |

### 14.2 탐험/전투 카메라 거리 분리 (옵션 레이어, 후순위)
명조는 Regular Camera Distance와 Combat Camera Distance를 **독립 슬라이더**(0~100)로 노출한다 [확인됨]. 본 프로젝트는 이미 `defaultOffset`/`combatOffset`·`fovExplore`/`fovCombat`·`lockOnDistance`로 상태별 프레이밍을 내부 구분하나, **플레이어 설정으로 노출된 분리 거리**는 없다. 향후 설정 메뉴 작업 시 `exploreDistance`/`combatDistance`를 사용자 슬라이더로 노출하는 방안을 검토(코어 알고리즘 변경 아님, 옵션 UI 레이어).

### 14.3 극한회피 Bullet Time — 기존 인프라 활용
명조의 퍼펙트 닷지 성공 시 짧은 슬로모(Bullet Time) 연출 [확인됨]은 숙련도 보상으로 효과적이다. **본 프로젝트는 이미 필요한 모든 기술을 보유**한다: `KillCamController`(슬로모+줌+셰이크 시퀀스), `TimeScaleCameraEffect`, `PerfectGuardFOV` 데이터(`CameraManager` 로드 완료). 따라서 신규 카메라 기술이 아니라, "퍼펙트 닷지/패링 성공" 게임플레이 이벤트에서 기존 이펙트 시퀀스를 발화시키는 **트리거 배선**만 필요하다. (참고: 슬로모 중 카메라 줌/흔들림 구체 수치는 명조에서 확인 불가 [출처 불충분] — 자체 튜닝 필요.)

### 14.4 회피할 함정 — 자동 카메라 보정
명조 커뮤니티는 다음 불만을 보고했다 [커뮤니티 관찰]: (1) 카메라 흔들림·자동 회전으로 인한 멀미, (2) Combat Camera Correction On 시 스킬 사용 후 카메라가 강제로 다시 확대되는 거동. 권장 회피책:
- 자동 보정(전투 시 적 중앙 정렬)·카메라 흔들림은 **토글 가능**하게, 멀미 민감 사용자 배려.
- 연출/스킬 종료 후 **강제 재줌 금지** — 사용자가 설정한 거리를 존중(본 프로젝트 §3.x 동적 FOV·align도 동일 원칙 적용).
- 또한 명조는 그래플/글라이딩 등 탐험 이동에서 두드러진 동적 FOV를 쓰지 않는 것으로 관찰됨 [추정] → 본 프로젝트 §3.1 속도 기반 동적 FOV는 **은은한 범위**(speedFOVMax 과도 금지)로 튜닝 권고.

### 14.5 한계 (확인 불가)
명조의 다음은 공개 정보 부족으로 확인 불가: 그래플/글라이딩 중 동적 FOV, 공명해방 궁극기의 정확한 카메라 메커니즘(줌/회전/슬로모), 패링 시 카메라 연출, 락온 중 회피 시 카메라 보간 곡선, 공중 전투 카메라 높이 보정. 해당 항목 설계 시 YouTube 영상 직접 관찰 등 추가 조사 필요.

### 14.6 명조 레퍼런스 기반 수치 제안

> ⚠️ **정직성 전제**: 명조는 카메라 설정을 **0~100 슬라이더로만 추상화**하며 FOV(도)·거리(m)·오프셋의 실제 수치를 일절 공개하지 않는다(데이터마이닝 매핑도 없음). 따라서 아래 수치는 명조의 **실제 값이 아니라**, 명조의 *알 수 있는 상대적 설계 철학*을 본 프로젝트 `CameraSettings` 스케일로 환산한 **출발점 제안**이다. 전부 플레이테스트로 검증해야 한다.

#### 14.6.1 발견된 구조적 결함 (먼저 수정 권장)

현재 `crowdZoomOutDistance = 7`(다수 적 줌아웃 목표)인데 `maxDistance = 4.5`이다. `CameraDistanceController.EvaluateDistance`가 군중 거리를 `Mathf.Clamp(_, minDistance, maxDistance)`로 제한하므로, **군중 줌아웃이 4.5m에서 막혀 사실상 작동하지 않는다.** 명조의 "전투에선 거리를 더 확보한다" 철학이 이 수정(=`maxDistance` 상향)의 직접 근거다. (의도가 다르다면 군중 줌을 maxDistance 클램프에서 분리할 것.)

#### 14.6.2 FOV 제안 (명조 도 단위 비공개 → 철학 기반 환산)

| 필드 | 현재 | 제안 | 근거 | 신뢰도 |
|---|---|---|---|---|
| `fovExplore` | 45 | 48 | 명조 탐험 = 넓은 환경 시야 선호 | 철학기반 |
| `fovCombat` | 50 | 54 | 명조 전투 = 다수 적 가시성 확보 | 철학기반 |
| `fovLockOn` | 50 | 50 (유지) | 단일 타겟 집중 | 유지 |
| `speedFOVMax` (신규, §3.1) | — | 6 | 명조는 탐험 동적 FOV를 두드러지게 쓰지 않음 → 은은하게 | 철학기반/주의 |

#### 14.6.3 Distance 제안 (명조 m 단위 비공개 → 상대 철학 + 현재 스케일 환산)

| 필드 | 현재 | 제안 | 근거 | 신뢰도 |
|---|---|---|---|---|
| `minDistance` | 3.7 | 3.2 | 근접 옵션 확보 | 환산 |
| `defaultDistance` (탐험) | 4.2 | 4.5 | 명조 탐험 거리 여유 | 환산 |
| `combatDistance` (신규 분리, §14.2) | — | 5.0 | 명조: 전투가 탐험보다 멀어도 됨(적 가시성) | 철학기반 |
| `lockOnDistance` | 4.2 | 4.0 | 명조 락온 시 "더 가까워짐" 관찰 | 관찰기반 |
| `maxDistance` | 4.5 | 7.0 | §14.6.1 군중줌 결함 해소 + 플레이어 줌아웃 허용 | 구조적 |

#### 14.6.4 Offset 제안 (명조 수치 완전 비공개 → 일반 OTS 관례, 저신뢰)

명조는 오버더숄더 오프셋을 사용자에게 노출하지 않고 내부 고정값으로 숨긴다. 따라서 아래는 명조 레퍼런스가 아니라 **일반 오버더숄더(OTS) 관례**에 기반한 저신뢰 제안이다.

| 필드 | 현재 | 제안 | 근거 | 신뢰도 |
|---|---|---|---|---|
| `defaultOffset` | (0, 1.0, 0) | (0.2, 1.0, 0) | 약한 오버더숄더 | 관례/저신뢰 |
| `combatOffset` | (0.05, 1.0, 0) | (0.5, 1.0, 0) | 전투 시 어깨 너머 강화 (명조는 "고정 OTS"만 확인) | 관례/저신뢰 |

#### 14.6.5 적용 시 주의
- 위 수치는 **출발점**이며 절대값으로 신뢰하지 말 것. 특히 Offset은 명조가 수치를 숨겨 관례에 의존했다.
- `maxDistance` 상향(7.0)은 충돌(§4.1 MultiProbe)·근평면 처리와 함께 검증할 것 — 거리가 멀어지면 후방 차폐 빈도가 증가한다.
- `combatDistance` 분리는 §14.2(탐험/전투 거리 분리)와 동일한 작업 단위다.

---

## 부록: FAQ

### Q1: 모두 구현해야 하나?

**A**: 아니다. Tier 1은 필수 (안정성·감각), Tier 2는 강력 권장 (견고성), Tier 3는 플레이테스트 후 선택. 시간 부족 시 Phase 1 + 4.1만 완료해도 감지할 수 있는 향상.

### Q2: 기존 코드와의 충돌?

**A**: 기존 로직과 완전 분리 설계. 신규 기법은 대부분 기존 메서드 내부 확장 또는 신규 메서드 추가. 삭제 작업 최소화.

### Q3: 성능 영향?

**A**: Phase 1은 미미 (1~2프레임 비용). Phase 2 MultiProbe는 N+1 Linecast (저렴, 약 1ms 이내). Phase 3 이상은 플레이테스트 기반 최적화.

### Q4: unsave 코드는 어디서 보나?

**A**: 리포지토리 루트의 `unsave/` 폴더 (카메라 코드, 컴파일 비대상 참고용). 본 로드맵은 해당 코드의 **논리와 설계 패턴**을 채택했으며, 코드 복사가 아닌 이해와 재구현을 권장.
