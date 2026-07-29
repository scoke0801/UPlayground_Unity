# 공격 모션 워핑 조작감 개선 구현 스펙

> 작성일: 2026-07-28  
> 상태: 설계 완료 / 미구현  
> 대상 환경: Unity 6 (6000.0.60f1), KCC, Animancer MotionSet  
> 범위: 플레이어·몬스터 근접 공격의 `MotionEvent_MotionWarp` 도착점, 보정 예산, 적용 구간, 데이터·에디터·검증 개선  
> 관련 문서: `Assets/docs/guide/MOTION_EVENT_ROLE_GUIDE.md`, `Assets/docs/guide/COMBAT_SYSTEM_GUIDE.md`, `Assets/docs/guide/CONTROL_FEEL_IMPROVEMENT_GUIDE.md`

---

## 0. 핵심 결론

현재 공격 모션 워핑의 주된 문제는 워핑 수식의 정밀도가 아니라 **워프 목적지가 공격 가능한 루트 위치가 아닌 타겟 Transform 중심**이라는 점이다.

현재 흐름은 다음과 같다.

```text
AttackState에서 타겟 선택
    ↓
타겟 Transform 중심을 워프 목표로 저장
    ↓
MotionEvent_MotionWarp가 Snapshot 윈도우 시작
    ↓
DeltaWarp가 남은 루트모션을 타겟 중심까지 100% 수렴
    ↓
ClampApproachVelocity가 양쪽 캡슐 접촉면에서 마지막 접근만 제한
```

이 구조에서는 공격 애니메이션과 무기 리치가 아니라 캐릭터 콜라이더가 최종 정지 거리를 결정한다. 따라서 플레이어에게는 공격자가 자신의 모션 궤적으로 접근하는 것이 아니라 타겟에게 자석처럼 끌려가 붙는 것으로 보인다.

개선의 핵심은 다음 네 가지다.

1. 도착점을 `TargetCenter`에서 공격별 `ContactShell` 또는 `AuthoredWarpPoint`로 변경한다.
2. 전체 타겟 거리 대신 **원본 루트모션 예상 도착점과 원하는 도착점의 차이**만 제한적으로 보정한다.
3. 이미 공격 리치 안이면 위치 워프를 끄고 필요한 회전 보정만 허용한다.
4. 위치 워프를 타격 직전까지 유지하지 않고, Hit보다 앞서 종료해 마지막 구간은 원본 모션으로 재생한다.

현재 `DeltaWarp`, 루트모션 베이크, 멀티 타겟 키, Snapshot/Live/Predictive, 타겟 잠금 구조는 유지할 가치가 있다. 전면 재작성보다 **도착 의미와 적용 정책을 교정하는 확장**을 우선한다.

---

## 1. 현재 구현 현황

### 1.1 런타임 구조

```text
AbilityAttackInfo.baseInfo.motionRef
    ↓
MotionReferenceSO.Resolve(WeaponType)
    ↓
MotionSetAsset
    └── MotionEvent_MotionWarp
            ├── MotionWarpWindowSettings 생성
            ├── 타겟 Resolver 실행
            └── MotionWarpController.BeginWarpWindow()
                    ↓
PlayerAttackState / PlayerDashAttackState / EnemyAttackState
    ├── ActorAnimator.GetRootMotionStepVelocity()
    ├── MotionWarpController.EvaluateVelocity()
    ├── MotionWarpController.ClampApproachVelocity()
    └── MotionWarpController.TryEvaluateRotation()
```

### 1.2 핵심 파일

| 파일 | 현재 책임 |
|------|-----------|
| `GameActor/MovementController/MotionWarpTypes.cs` | 워프 Modifier, TargetPolicy, Y 정책, 프리셋, 윈도우 설정 |
| `GameActor/MovementController/MotionWarpTarget.cs` | 타겟 Anchor, Offset, 공간, Follow 데이터 |
| `GameActor/MovementController/MotionWarpController.cs` | 타겟 수명, DeltaWarp, 속도·회전 보정, 접근 클램프, 베이크 캐시 |
| `GameActor/MovementController/IWarpTargetResolver.cs` | UseExisting/ConeNearest/LockOnFirst/Hybrid 타겟 선택 |
| `GameActor/Animation/MotionEvents/MotionEvent_MotionWarp.cs` | MotionSet 이벤트 데이터, 프리셋 적용, 윈도우 시작·종료 |
| `GameActor/State/Player/PlayerAttackState.cs` | 일반 공격 타겟 결정, 루트모션·워프 소비 |
| `GameActor/State/Player/PlayerDashAttackState.cs` | 대시 공격 타겟 결정, 루트모션·워프 소비 |
| `GameActor/State/Enemy/EnemyAttackState.cs` | 몬스터 근접 공격 타겟 결정, 루트모션·워프 소비 |
| `Editor/MotionSetWindow.WarpTarget.cs` | 더미 타겟 기반 워프 프리뷰 |
| `Editor/MotionSetWindow.WarpBake.cs` | 워프 윈도우 루트모션 총량 베이크 |

### 1.3 MotionSet 데이터 감사

2026-07-28 기준 `Assets/10.Datas/`의 `.asset`을 YAML 읽기 전용으로 조사했다.

| 항목 | 결과 |
|------|-----:|
| `MotionEvent_MotionWarp` 총수 | 204 |
| `Snapshot` | 204 |
| `Live` / `Predictive` | 0 |
| `translationWeight = 1` | 204 |
| `rotationWeight = 1` | 204 |
| `targetOffset = (0,0,0)` | 204 |
| `overrideDistance = false` | 204 |
| `resolverPolicy = UseExisting` | 204 |
| `bakedValid = true` | 93 |
| `bakedValid = false` | 111 |
| `LightAttack` 프리셋 | 84 |
| `HeavyAttack` 프리셋 | 57 |
| `FinishAttack` 프리셋 | 3 |
| `Custom` | 60 |

프리셋이 런타임에 값을 덮어쓰므로 에셋의 `modifierType` 직렬화 값만 보고 실제 동작을 판단하면 안 된다.

---

## 2. 식별된 문제

### P1. 목표가 타겟 중심이다

`EvaluateVelocity()`는 현재 위치에서 `targetWorld`까지의 벡터를 직접 목표 오차로 사용한다.

```csharp
Vector3 toTarget = targetWorld - currentPosition;
toTarget.y = 0f;
```

`DeltaWarp` 정확 모드는 남은 원본 루트 변위와 이 오차를 비교해 윈도우 끝에 목표점으로 수렴한다. 알고리즘은 의도대로 동작하지만 목표점이 공격자 루트가 서야 할 위치가 아니다.

### P2. 최종 정지 거리를 콜라이더가 사후 결정한다

`ClampApproachVelocity()`는 공격자·타겟 캡슐 반경과 고정 `DefaultContactBuffer`를 합산해 접근 속도를 제한한다.

이 처리는 관통과 접선 미끄러짐을 막는 안전장치로는 유효하지만, 공격 도착점의 주 설계 수단이 되어서는 안 된다. 현재는 워프가 타겟 중심으로 계속 진입하려 하고 콜라이더가 이를 막는 구조이므로 시각적으로 밀착과 끌림이 발생한다.

### P3. 일반 공격의 워프 허용 범위가 넓다

현재 프리셋은 다음 범위를 사용한다.

| 프리셋 | 최대 거리 | 최대 속도 |
|--------|----------:|----------:|
| LightAttack | 7m | 22m/s |
| HeavyAttack | 8m | 20m/s |
| FinishAttack | 5m | 16m/s |
| Grab | 3m | 12m/s |

`PlayerCombat` 기본값도 최대 7m, 22m/s다. 또한 `maxSpeed × duration` 내 도달 가능 여부를 더 이상 취소 조건으로 사용하지 않고, 최대 속도로 갈 수 있는 데까지 접근하는 정책이다.

이 값은 일반 검격의 관용 범위를 넘어 짧은 돌진기 수준이다.

### P4. 모든 공격이 동일한 도착 의미를 갖는다

현재 204개 이벤트의 오프셋이 모두 0이다. 공격별 무기 길이, 자세, 좌우 스텝, 큰 몬스터의 캡슐 크기, 타격 소켓 차이를 표현하지 않는다.

`MotionWarpTarget`은 `World`, `AnchorLocal`, `AnchorForward` 공간을 지원하지만 `BeginWarpWindow()`가 활성 타겟 공간을 `World`로 덮어쓴다. 결과적으로 이벤트 데이터에서 타겟 정면 기준 간격을 자연스럽게 표현하기 어렵다.

### P5. 위치 보정과 재생 속도 보정이 동시에 수렴을 강화한다

`WarpPlayRateScale`은 남은 거리와 남은 시간의 비율로 계산되며 `0.5~1.2` 범위를 사용한다. 일반 공격도 위치 궤적과 애니메이션 속도를 동시에 타겟에 맞춘다.

거리 매칭이 중요한 돌진에는 유효하지만, 일반 검격에서는 애니메이션의 무게와 타격 타이밍이 대상 거리에 따라 달라져 끌림을 더 강조할 수 있다.

### P6. 위치 보정 곡선과 종료 여유가 없다

회전에는 `rotationCurve`가 있지만 위치 보정은 `_blendWeight`와 DeltaWarp 분배에만 의존한다. “초반에 접근하고 타격 직전에는 원본 모션을 보존”하는 공격별 위치 곡선을 저작할 수 없다.

### P7. 베이크 여부에 따라 첫 실행 품질이 다르다

204개 중 111개는 `bakedValid = false`다. 이 경우 세션 첫 실행은 정확 도착이 아니라 원본 속도 크기를 보존한 방향 스티어 폴백을 사용하고, 재실행부터 캐시 기반 정확 모드로 바뀔 수 있다.

이는 자석 현상의 직접 원인은 아니지만 동일 공격의 첫 사용과 재사용 체감을 다르게 만든다.

### P8. 자동 검증이 없다

현재 `Assets/Tests/`에는 MotionWarp 전용 테스트가 없다. 도착 간격, 근거리 비활성, 큰 타겟, 장애물, 인터럽트 회귀를 자동으로 잡지 못한다.

### P9. 일부 문서·Tooltip이 실제 코드와 다르다

- `MOTION_EVENT_ROLE_GUIDE.md`는 Light/Additive, Heavy/Scale, Finish/Skew로 설명하지만 실제 프리셋은 모두 `DeltaWarp`를 사용한다.
- `PlayerCombat._warpMaxSpeed` Tooltip은 남은 시간 내 도달 불가 시 워프 미적용으로 설명하지만 실제 코드는 최대 속도 접근을 허용한다.

구현 시 문서와 Inspector 설명을 함께 갱신한다.

---

## 3. 외부 구현 비교

### 3.1 Unreal Engine Motion Warping

Unreal Engine Motion Warping은 다음 개념을 분리한다.

| 개념 | Unreal | 현재 프로젝트 |
|------|--------|---------------|
| 워프 윈도우 | Anim Notify State | `MotionEvent_MotionWarp` |
| 명명 타겟 | Warp Target Name | `targetKey` |
| 타겟 Follow | Component Follow | Snapshot/Live/Predictive |
| 위치·회전 분리 | `Warp Translation`, `Warp Rotation` | 두 Weight가 있으나 데이터는 모두 1 |
| 애니메이션 기준점 | Static/Bone Warp Point Provider | 없음 |
| 타겟 오프셋 공간 | Component/Bone + 방향별 Offset | 모델은 있으나 윈도우가 World로 덮어씀 |
| 중단 조건 | 거리·각도 Switch-Off | 거리 OOR 타이머 중심 |

현재 프로젝트는 윈도우, 명명 타겟, Follow, DeltaWarp 등 기반 기능은 충분하다. 가장 큰 차이는 **애니메이션의 어떤 점을 월드의 어떤 도착 Transform에 맞출지**를 표현하는 Warp Point가 없다는 점이다.

### 3.2 Unity Animator.MatchTarget

Unity `Animator.MatchTarget`은 루트 Transform 자체가 아니라 손·발 등 `AvatarTarget`이 지정한 정규화 시간 구간에 목표 위치·회전에 도달하도록 조정하며 위치·회전 WeightMask를 분리한다.

프로젝트의 일반 공격도 장기적으로는 “루트가 적 중심에 도달”이 아니라 “공격 애니메이션의 저작된 접촉점이 목표 접촉점에 도달”하는 구조가 적합하다.

### 3.3 적용 원칙

외부 구현을 그대로 복제하지 않고 다음 원칙만 채택한다.

1. 워프 목표는 Actor 중심이 아니라 명시적인 도착 Pose다.
2. 원본 모션의 세부 속도 곡선은 가능한 보존한다.
3. 위치와 회전 보정의 허용 범위와 종료 시점을 분리한다.
4. 거리·각도·장애물 조건으로 워프를 일시정지하거나 취소할 수 있어야 한다.
5. 일반 공격, 돌진, 잡기, 처형은 같은 프리셋 의미를 공유하지 않는다.

---

## 4. 목표와 비목표

### 4.1 목표

- 일반 근접 공격이 타겟 콜라이더에 밀착하지 않고 공격별 적정 거리에 도착한다.
- 이미 공격 리치 안이면 불필요한 위치 흡착이 발생하지 않는다.
- 워프가 원본 루트모션 궤적과 가감속을 최대한 보존한다.
- 일반 공격과 돌진/잡기/처형의 보정 권한을 구분한다.
- 타겟 크기가 달라도 도착 간격이 안정적이다.
- MotionSet Editor에서 원본·보정 궤적과 도착 셸을 미리 확인할 수 있다.
- 기존 managed reference와 MotionSet/VFX 참조를 보존하며 단계적으로 마이그레이션한다.
- 플레이어와 몬스터가 같은 계산 코어를 사용한다.

### 4.2 비목표

- `MotionWarpController`를 새 패키지나 새 매니저로 전면 재작성하지 않는다.
- Ability 데이터와 MotionSet 이벤트에 같은 워프 수치를 중복 저장하지 않는다.
- 일반 공격의 헛스윙을 완전히 제거하지 않는다.
- 모든 공격을 움직이는 타겟에 실시간 추적시키지 않는다.
- KCC 충돌을 무시하고 Transform을 직접 순간 이동시키지 않는다.
- MotionSet 에셋 204개를 검증 없이 일괄 재직렬화하지 않는다.

---

## 5. 목표 아키텍처

```text
타겟 선택
    ↓
WarpArrivalResolver
├── TargetCenter       잡기/연출 전용
├── ContactShell       일반 근접 기본
└── AuthoredWarpPoint  정밀 공격/처형
    ↓
WarpConstraintEvaluator
├── 거리 게이트
├── 각도 게이트
├── 공격 리치 Dead Zone
├── 최대 보정 거리/비율
└── KCC 경로 가능성
    ↓
MotionWarpController
├── 원본 루트모션 예상 도착
├── 제한된 잔여 오차 계산
├── 위치 보정 곡선
├── 회전 보정 곡선
└── 타격 전 Translation 종료
```

신규 계산 책임은 `MotionWarpController` 내부의 작은 순수 계산 함수 또는 Actor 모듈 내부 helper로 둔다. 하위 Data 모듈이나 Camera 모듈에 Actor/KCC 의존을 추가하지 않는다.

---

## 6. 도착점 설계

### 6.1 신규 도착 모드

다음 enum과 필드는 신규 제안이다.

```csharp
public enum WarpArrivalMode
{
    TargetCenter = 0,
    ContactShell,
    AuthoredWarpPoint,
}
```

| 모드 | 용도 | 기본 사용처 |
|------|------|-------------|
| `TargetCenter` | 타겟 중심 정렬이 실제 의도인 특수 연출 | 제한된 Grab/특수 시퀀스 |
| `ContactShell` | 양쪽 충돌 반경과 공격별 간격을 반영한 루트 도착점 | Light/Heavy/Dash 근접 |
| `AuthoredWarpPoint` | 애니메이션의 Static/Bone 기준점과 월드 접촉점을 맞춤 | Finish/Grab/정밀 보스 공격 |

### 6.2 ContactShell 계산

수평면 기준:

```text
approachDir = NormalizeXZ(targetCenter - attackerStart)

desiredRoot =
    targetCenter
    - approachDir × (targetRadius + attackerRadius + desiredStandOff)
    + targetBasis × localArrivalOffset
```

`desiredStandOff`는 콜라이더 안전 버퍼와 별개다.

- 콜라이더 반경: 물리 관통 방지
- `desiredStandOff`: 공격 자세와 무기 리치 표현
- `localArrivalOffset`: 좌우 비껴서기, 대형 무기 자세 등 표현

`ContactShell`이 목적지를 먼저 결정하고 `ClampApproachVelocity()`는 최종 안전장치로만 남긴다.

### 6.3 AuthoredWarpPoint

장기 정밀 경로에서는 애니메이션의 기준점을 별도로 저작한다.

```csharp
public enum WarpPointProvider
{
    Root,
    StaticTransform,
    Bone,
}
```

예:

- 검 끝/손이 타격 시점에 목표 소켓에 도달
- 처형자의 손과 피해자의 상체 소켓 정렬
- Grab의 손/피해자 중심 정렬

Phase 1에서는 `ContactShell`만 구현하고 `AuthoredWarpPoint`는 Finish/Grab 품질 요구가 확인된 후 Phase 3에서 추가한다.

---

## 7. 보정 예산과 게이트

### 7.1 전체 거리 대신 잔여 오차 보정

```text
predictedOriginalEnd = currentRoot + remainingOriginalRootMotion
correction = desiredRoot - predictedOriginalEnd
limitedCorrection = Clamp(correction, correctionBudget)
```

`DeltaWarp`의 폐루프 분배는 `limitedCorrection`에만 적용한다.

### 7.2 신규 제안 필드

```csharp
public WarpArrivalMode arrivalMode;
public float desiredStandOff;
public Vector3 localArrivalOffset;

public float noTranslationWithinReach;
public float maxCorrectionDistance;
public float maxCorrectionRatio;
public float maxWarpAngle;

public AnimationCurve translationCurve;
public float translationEndLeadTime;

public bool usePlaybackRateWarp;
public Vector2 playbackRateRange;
```

| 필드 | 의미 |
|------|------|
| `noTranslationWithinReach` | 이 거리 안에서는 위치 워프를 끄는 Dead Zone |
| `maxCorrectionDistance` | 원본 예상 도착점에서 추가로 이동할 수 있는 절대 상한 |
| `maxCorrectionRatio` | 원본 남은 이동량 대비 보정 비율 상한 |
| `maxWarpAngle` | 공격 시작 방향 대비 위치 워프 허용 반각 |
| `translationCurve` | 정규화 워프 시간별 위치 보정 Weight |
| `translationEndLeadTime` | Hit/윈도우 종료보다 먼저 Translation을 끝낼 여유 |
| `usePlaybackRateWarp` | 거리 기반 애니메이션 재생 속도 보정 사용 여부 |
| `playbackRateRange` | 공격 분류별 허용 재생 속도 범위 |

### 7.3 게이트 순서

```text
타겟 유효성
→ 타겟 생존
→ 거리
→ 각도
→ 이미 공격 리치 안인지
→ 목적지까지 KCC 이동 가능성
→ 보정 예산 제한
→ 위치/회전 곡선 적용
```

게이트 실패 시 가능한 정책:

| 결과 | 의미 |
|------|------|
| `CancelWarping` | 원본 루트모션만 재생 |
| `CancelTranslation` | 위치 워프만 끄고 회전은 유지 |
| `FreezeTarget` | Live/Predictive를 Snapshot으로 전환 |

일반 공격은 게이트 실패 시 공격 자체를 취소하지 않는다. 워프만 중단하고 원본 모션으로 헛스윙할 수 있어야 한다.

---

## 8. 권장 프리셋

다음 값은 초기 튜닝 기준이며 대표 모션 검증 후 확정한다.

| 항목 | Light | Heavy | Dash/Lunge | Finish | Grab |
|------|------:|------:|-----------:|-------:|-----:|
| Arrival | ContactShell | ContactShell | ContactShell | Authored 예정 | Authored 예정 |
| TargetPolicy | Snapshot | Snapshot | 짧은 Live 후 Snapshot 검토 | Snapshot | Predictive |
| 최대 타겟 탐색 거리 | 2.5m | 3.0m | 공격별 3~5m | 3m 내외 | 2m 내외 |
| 최대 추가 보정 | 0.5m | 0.8m | 원본 이동량의 50% 이내 | 전용 | 전용 |
| 최대 보정 비율 | 30% | 40% | 50% | 전용 | 전용 |
| 최대 워프 각도 | 45° | 35° | 30° | 전용 | 전용 |
| 근거리 Translation Dead Zone | 공격 유효 리치 | 공격 유효 리치 | 짧게 | 없음/전용 | 없음 |
| 재생 속도 워프 | Off 또는 0.95~1.05 | Off 또는 0.95~1.05 | 0.85~1.15 | 전용 | 전용 |
| Translation 종료 | Hit 50~80ms 전 | Hit 80~120ms 전 | Hit 50ms 전 | 연출 기준 | 접촉 기준 |

`maxDistance`는 타겟 탐색/수락 범위이고 `maxCorrectionDistance`는 원본 모션에 추가할 수 있는 보정량이다. 두 개념을 혼용하지 않는다.

---

## 9. 위치와 회전 정책 분리

현재 204개 데이터가 위치와 회전 Weight를 모두 1로 사용한다. 개선 후 기본 정책:

```text
먼 거리
    제한된 Translation + Rotation

이미 무기 리치 안
    Translation 0 + 제한된 Rotation

각도가 너무 큼
    Translation 0
    Rotation도 공격별 상한까지만

타격 직전
    Translation 0
    Rotation은 필요 시 짧게 유지
```

회전은 항상 타겟을 바라보는 것보다 공격 모션의 의도된 회전량과 합성해야 한다. Lock-On 상태의 `PlayerAttackState.UpdateRotation()`이 워프 종료 후에도 타겟을 계속 바라보는 경로와 충돌하지 않는지 함께 검증한다.

---

## 10. 타겟 정책

### 10.1 일반 공격

- 공격 단위 `BeginTargetLock()`은 유지한다.
- 첫 타겟 선택 후 `Snapshot`한다.
- 콤보 다음 타격은 현재처럼 새 공격 스코프로 다시 결정한다.
- 타겟 이동을 공격 중 계속 따라가지 않는다.

### 10.2 대시·돌진

현재 `PlayerDashAttackState`가 `useSnapshot: false`로 타겟을 넣더라도 MotionEvent의 `Snapshot` 정책이 윈도우 시작 시 Follow를 다시 덮어쓴다.

정책 소유권을 한 곳으로 정리한다.

- MotionEvent가 최종 TargetPolicy를 소유한다.
- AttackState는 타겟만 주입한다.
- 특수 런타임 전환이 필요하면 명시적인 `LiveThenSnapshot` 정책을 새로 정의한다.

### 10.3 Grab/Finish

일반 공격 프리셋과 분리한다.

- 타겟·공격자 양쪽 포즈를 맞춰야 하면 단일 루트 워프로 해결하지 않는다.
- 필요 시 피해자 연출 상태와 Warp Point를 함께 사용한다.
- `TargetCenter`는 명시적으로 필요한 연출에만 남긴다.

### 10.4 몬스터 공격

플레이어와 같은 도착 계산 코어를 사용하되 데이터는 별도로 튜닝한다.

- 대형 몬스터는 캡슐 중심이 실제 시각 중심과 다를 수 있으므로 전용 접촉 소켓을 고려한다.
- 텔레그래프가 `lockPositionOnStart = true`라면 워프 후 판정 위치와 경고 위치가 어긋나지 않아야 한다.
- 회피 가능한 공격은 플레이어를 끝까지 추적하지 않는다.

---

## 11. 장애물과 KCC

현재 거리와 타겟 생존 중심으로 적용 가능성을 판정한다. 개선 후 목적지로 이동하기 전에 KCC 기준 경로 가능성을 확인한다.

최소 요구:

- 공격자 캡슐을 기준으로 현재 위치에서 `desiredRoot`까지 수평 Sweep
- 벽, 큰 단차, 이동 불가능 경사에 막히면 목적지를 충돌 전 위치로 제한하거나 Translation 취소
- 타겟 반대편으로 통과하는 경로 금지
- `transform.position` 직접 수정 금지

장애물 판정은 매 프레임 전체 경로를 비싸게 검사하기보다 윈도우 시작 시 1회 검사하고, Live 계열만 제한된 주기로 재검사한다.

---

## 12. 데이터·직렬화 마이그레이션

### 12.1 호환 기본값

신규 필드는 기존 에셋 로드 시 현재 동작을 갑자기 바꾸지 않는 기본값을 가져야 한다.

```text
기존 managed reference 로드
→ legacyCompatibility = true 또는 schemaVersion 판정
→ 기존 TargetCenter 동작 유지
→ 명시적으로 마이그레이션한 이벤트만 ContactShell 사용
```

모든 이벤트를 코드 기본값만으로 즉시 새 의미로 바꾸면 204개 공격이 한 번에 달라져 원인 추적이 어렵다.

### 12.2 권장 마이그레이션 순서

1. 신규 필드와 Legacy 동작 경로 추가
2. 런타임 단위 테스트 추가
3. 대표 모션 12개만 ContactShell로 변환
4. Play Mode에서 캐릭터·타겟 크기 조합 검증
5. 프리셋 기본값 확정
6. Editor 마이그레이션 메뉴 추가
7. Dry Run 보고서 확인
8. 선택된 MotionSet만 Undo 가능한 방식으로 변환
9. managed reference/VFX 누락 검사
10. 나머지 이벤트 단계적 변환

### 12.3 안전 규칙

- `MotionEvent_MotionWarp` 타입을 이동하지 않는다.
- 타입 또는 어셈블리를 이동해야 한다면 `[MovedFrom(true, sourceAssembly: "...")]` 규칙을 지킨다.
- 컴파일 오류나 managed reference 누락 상태에서 MotionSet을 저장하지 않는다.
- `Assets/10.Datas/` 자동 변경은 저장 전후 diff를 검사한다.
- 마이그레이션 도구는 대상 파일, 변경 전후 값, 스킵·오류 사유를 Dry Run으로 출력한다.
- 오류 발생 시 해당 Undo Group 전체를 롤백한다.
- 사용자 데이터 변경과 검증 과정의 자동 재직렬화를 구분한다.

---

## 13. MotionSet Editor 개선

현재 Warp Target 더미와 베이크 기능을 확장한다.

### 13.1 Scene View 표시

```text
회색 선   원본 루트모션 궤적
파란 선   보정 후 예상 궤적
초록 원   ContactShell 허용 도착 영역
노랑 점   desiredRoot
빨강 선   제한 전 correction
청록 선   제한 후 correction
```

함께 표시할 값:

- 타겟 중심
- 양쪽 캡슐 반경
- `desiredStandOff`
- 원본 예상 도착점
- 보정 거리와 보정 비율
- 거리/각도/리치/장애물 게이트 결과
- Translation 종료 시점과 첫 Hit 시점
- 베이크 유효/무효 상태

### 13.2 프리뷰 시나리오

| 시나리오 | 값 |
|----------|----|
| 거리 | 0.5m / 1m / 1.5m / 2m / 3m |
| 각도 | 0° / 30° / 60° / 90° |
| 타겟 크기 | 소형 / 인간형 / 대형 |
| 이동 | 정지 / 횡이동 / 후퇴 |
| 지형 | 평지 / 경사 / 단차 / 벽 앞 |

### 13.3 검증 경고

- 일반 Light/Heavy인데 `arrivalMode == TargetCenter`
- 일반 공격 `maxDistance > 3.5m`
- `maxCorrectionDistance <= 0` 또는 비정상적으로 큼
- Translation이 첫 Hit 이후까지 유지됨
- `translationWeight > 0`인데 타겟/도착 정책 미설정
- `bakedValid == false`
- 베이크 구간과 이벤트 구간 불일치
- Finish/Grab인데 정밀 도착 정보 없음

---

## 14. 구현 Phase

### Phase 0 — 계측과 기준선

- 현재 204개 이벤트 감사 메뉴 또는 검증기 추가
- 대표 모션 12개 선정
- 워프 On/Off 비교 영상과 수치 기록
- 거리별 도착 오차, 최대 속도, 캡슐 간격, 마지막 프레임 보정량 기록

완료 조건:

- 같은 입력과 시작 위치로 워프 On/Off를 반복 비교할 수 있다.
- 자석 현상을 수치로 확인할 수 있다.

### Phase 1 — ContactShell과 보정 예산

- `WarpArrivalMode.ContactShell` 추가
- `desiredStandOff`, `localArrivalOffset` 추가
- 원본 예상 도착점 기반 correction 계산
- 절대·비율 보정 상한 추가
- 근거리 Translation Dead Zone 추가
- 기존 `ClampApproachVelocity()`는 안전장치로 유지

완료 조건:

- 일반 공격이 타겟 중심으로 수렴하지 않는다.
- 타겟 크기가 달라도 지정 간격을 유지한다.
- 이미 사거리 안이면 위치 흡착이 발생하지 않는다.

### Phase 2 — 시간·회전·재생 속도 분리

- `translationCurve` 추가
- Translation 조기 종료 추가
- 위치와 회전 Switch-Off 분리
- 일반 공격 `WarpPlayRateScale` 축소 또는 비활성
- Dash/Lunge만 거리 매칭 허용

완료 조건:

- 타격 직전 급가속이나 마지막 프레임 미끄러짐이 없다.
- 공격 타이밍이 타겟 거리에 따라 과도하게 변하지 않는다.

### Phase 3 — 정밀 Warp Point

- Static/Bone 기반 `AuthoredWarpPoint`
- Finish/Grab 전용 정렬
- 대형 몬스터 접촉 소켓

완료 조건:

- 손·무기·피해자 포즈가 필요한 특수 공격을 루트 중심 편법 없이 정렬할 수 있다.

### Phase 4 — 에디터·마이그레이션

- Scene View 궤적·도착 셸 표시
- 데이터 검증기
- Dry Run 마이그레이션
- 선택/프리셋별 배치 변환
- 유지 대상 전부 재베이크

완료 조건:

- 204개 이벤트의 새 정책 상태를 보고서로 확인할 수 있다.
- 누락·과도 설정을 저장 전에 차단할 수 있다.

---

## 15. 영향받는 파일

| 파일 | Phase | 변경 성격 |
|------|-------|-----------|
| `MotionWarpTypes.cs` | P1~P3 | Arrival, 보정 예산, 곡선, 중단 정책 데이터 추가 |
| `MotionWarpTarget.cs` | P1 | Offset 공간을 보존하고 목적지 계산과 분리 |
| `MotionWarpController.cs` | P1~P3 | ContactShell, correction budget, Dead Zone, Switch-Off, 경로 검사 |
| `MotionEvent_MotionWarp.cs` | P1~P3 | 신규 필드 전달, 프리셋 갱신, Legacy 호환 |
| `PlayerAttackState.cs` | P2 | 재생 속도 정책과 위치·회전 소비 경로 갱신 |
| `PlayerDashAttackState.cs` | P2 | TargetPolicy 소유권 정리 |
| `EnemyAttackState.cs` | P1~P2 | 공용 새 계산 경로 적용 |
| `PlayerCombat.cs` | P1 | 공통 7m 설정 축소 또는 Legacy 역할 정리 |
| `EnemyCombat.cs` | P1 | 몬스터 기본 설정과 새 공용 계약 연결 |
| `MotionSetWindow.WarpTarget.cs` | P4 | 도착 셸·원본/보정 궤적 시각화 |
| `MotionSetWindow.WarpBake.cs` | P4 | 신규 설정 검증 및 재베이크 |
| 신규 Editor 검증기 | P0/P4 | 204개 이벤트 감사, Dry Run, 선택 변환 |
| 신규 EditMode/PlayMode 테스트 | P0~P3 | 수학·KCC·상태 인터럽트 회귀 검증 |

---

## 16. 테스트 계획

### 16.1 EditMode 순수 계산

| 테스트 | 기대값 |
|--------|--------|
| 인간형 대 인간형 ContactShell | 두 캡슐 반경 + standOff 유지 |
| 소형/대형 타겟 | 중심점이 달라도 표면 기준 간격 유지 |
| 이미 리치 안 | Translation correction 0 |
| 보정 거리 초과 | `maxCorrectionDistance`에서 클램프 |
| 보정 비율 초과 | `maxCorrectionRatio`에서 클램프 |
| 허용 각도 초과 | 위치 워프 취소 |
| Snapshot 타겟 이동 | 목적지 불변 |
| Live 타겟 이동 | 목적지 갱신 |
| 타겟 사망 | `TargetLost` 취소 |

### 16.2 PlayMode

| 범주 | 케이스 |
|------|--------|
| 플레이어 | Light/Heavy/Dash/Jump/Finish/Combo |
| 몬스터 | 소형/인간형/대형 근접 공격 |
| 거리 | 리치 안/경계/최대 수락/범위 밖 |
| 각도 | 정면/측면/후방 |
| 이동 타겟 | 정지/횡이동/후퇴 |
| 지형 | 평지/경사/계단/벽/모서리 |
| 인터럽트 | Hit/Stun/Knockdown/Death/Dodge/상태 전환 |
| 전투 연계 | Lock-On/비락온/콤보 타겟 재선택/텔레그래프 |

### 16.3 데이터 무결성

- MotionSet/Ultimate managed reference 누락 0
- VFX 참조 누락 0
- Missing Script 0
- 대상 MotionSet 외 `Assets/10.Datas/` 변경 0
- 모든 새 프리셋 이벤트의 도착 모드 명시
- 유지 대상 워프의 `bakedValid = true`

---

## 17. 완료 조건

### 기능

- 일반 Light/Heavy가 적 중심으로 이동하지 않는다.
- 근거리 공격에서 캡슐에 달라붙는 현상이 없다.
- 먼 적에게 일반 공격이 7~8m 돌진하지 않는다.
- Dash/Lunge는 원본 모션 정체성을 유지하며 제한적으로 거리 보정한다.
- Grab/Finish는 일반 공격과 별도 정렬 정책을 사용한다.
- 벽이나 큰 단차를 넘어 워프하지 않는다.

### 체감

- 워프 On/Off 비교에서 보정은 보이되 강제 이동처럼 느껴지지 않는다.
- 마지막 프레임 급가속과 발 미끄러짐이 감소한다.
- 타겟이 움직여도 일반 공격 방향이 중간에 여러 번 바뀌지 않는다.
- 같은 공격의 첫 실행과 재실행 체감이 일치한다.

### 프로젝트 검증

- Unity 컴파일 오류 0
- 관련 asmdef `dotnet build --no-restore` 오류 0
- EditMode/PlayMode MotionWarp 테스트 통과
- Play Mode 서비스 경고·예외 0
- Missing Script 0
- managed reference/VFX 누락 0
- Player Build 오류 0
- 변경된 `Assets/10.Datas/` diff 검토 완료

---

## 18. 구현 체크리스트

### 시작 전

- [ ] 대표 플레이어 모션 8개, 몬스터 모션 4개 선정
- [ ] 현재 MotionWarp On/Off 기준 영상 저장
- [ ] 204개 이벤트 감사 결과 저장
- [ ] 관련 에셋 사용자 변경 여부 확인

### 코드

- [ ] Legacy 호환 로드 경로
- [ ] ContactShell 도착 계산
- [ ] 원본 예상 도착 기반 correction
- [ ] 절대·비율 보정 예산
- [ ] 리치 Dead Zone
- [ ] 위치/회전 Switch-Off
- [ ] Translation 조기 종료
- [ ] 재생 속도 정책 분리
- [ ] KCC 경로 검사

### 에디터·데이터

- [ ] 원본/보정 궤적 표시
- [ ] ContactShell 표시
- [ ] 데이터 경고
- [ ] Dry Run 마이그레이션
- [ ] 선택 변환 및 Undo 롤백
- [ ] 대표 모션 재베이크
- [ ] 전체 유지 대상 재베이크

### 검증

- [ ] EditMode
- [ ] PlayMode
- [ ] Lock-On/비락온
- [ ] 소형/대형 타겟
- [ ] 벽/경사/계단
- [ ] Hit/Death 인터럽트
- [ ] 텔레그래프 정합
- [ ] managed reference/VFX
- [ ] Player Build

---

## 19. 외부 참고 자료

| 자료 | 적용 포인트 |
|------|-------------|
| Unreal Engine Motion Warping — https://dev.epicgames.com/documentation/en-us/unreal-engine/motion-warping-in-unreal-engine | Warp Window, 명명 타겟, Static/Bone Warp Point, 위치·회전 분리 |
| Unreal `FMotionWarpingTarget` — https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Plugins/MotionWarping/FMotionWarpingTarget | Component Follow, 위치·회전 Offset, Offset 방향 |
| Unreal Distance Switch-Off — https://dev.epicgames.com/documentation/en-us/unreal-engine/BlueprintAPI/MotionWarping/CreateSwitchOffDistanceCondition | 거리 조건별 워프 중단 |
| Unreal `ESwitchOffConditionEffect` — https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Plugins/MotionWarping/ESwitchOffConditionEffect | Follow 취소, Warp 취소·일시정지, RootMotion 일시정지 의미 분리 |
| Unity 6 `Animator.MatchTarget` — https://docs.unity3d.com/kr/6000.0/ScriptReference/Animator.MatchTarget.html | Body Part 목표, 시간 구간, 위치·회전 WeightMask |
| Witkin & Popović, Motion Warping — https://publications.ri.cmu.edu/motion-warping | 원본 모션의 세부 구조를 보존하며 제한된 제약을 만족하는 원칙 |

---

## 20. 최종 권장 결정

첫 구현 범위는 다음으로 제한한다.

1. 일반 Light/Heavy에 `ContactShell`을 적용한다.
2. `maxDistance`를 줄이고 별도 `maxCorrectionDistance`를 도입한다.
3. 이미 공격 리치 안이면 Translation을 끈다.
4. 일반 공격의 거리 기반 재생 속도 보정을 끄거나 매우 좁게 제한한다.
5. 대표 모션 12개를 검증한 뒤 나머지 데이터를 변환한다.

`AuthoredWarpPoint`, Live→Snapshot 전환, 정밀 Grab/Finish 정렬은 Phase 1 결과를 확인한 후 진행한다. 가장 먼저 해결할 문제는 기능 확장이 아니라 **일반 공격의 잘못된 도착 의미를 바로잡는 것**이다.
