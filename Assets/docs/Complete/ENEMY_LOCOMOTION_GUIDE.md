# 몬스터 방향성 로코모션 가이드

## 개요

몸이 항상 타겟을 향하는 적 몬스터가 **이동 속도 벡터 방향에 맞는 애니메이션 클립을 자동으로 선택**하는 8방향 방향성 로코모션 시스템입니다.

### 핵심 특징

- **8방향 분기**: 전·후·좌45·우45·좌90·우90·후좌45·후우45 방향을 속도 벡터 각도로 구분
- **3가지 보행 스타일**: `WalkSlow`(순찰) / `Walk`(전투 보행) / `Run`(질주) — 각각 독립적인 8방향 클립 세트
- **히스테리시스 내장**: 0.5 m/s 이하 속도에서는 클립 교체 생략 → 감속 중 애니메이션 깜빡임 방지
- **키 변경 시에만 PlayMotion 호출**: `_lastLocoKey` 추적으로 불필요한 Animancer 전환 최소화
- **Fallback 체인 연동**: 클립이 등록되지 않은 방향은 `Humanoid_Common_MotionAsset`의 클립을 자동 사용

---

## 아키텍처

```
EnemyLocomotionHelper (static)
│
├── GetDirectionalKey(worldVelocity, transform, LocoStyle)
│     └── InverseTransformDirection → 로컬 각도 계산 → GetKey()
│
└── UpdateAnim(actor, motor, ref lastKey, style, crossfade)
      └── 키 변경 시에만 actor.Animator.PlayMotion() 호출

각 Enemy 이동 상태
├── EnemyChaseState    → LocoStyle.Run
├── EnemyFlankState    → LocoStyle.Run
├── EnemyCircleState   → LocoStyle.Walk
├── EnemyRetreatState  → LocoStyle.Walk
└── EnemyPatrolState   → LocoStyle.WalkSlow

AnimKey (6000번대)
├── Walk_Slow = 6000 ~ 6007   (8방향)
├── Walk_B    = 6010 ~ 6016   (Walk_F는 기존 Walk = 1)
└── Run_B     = 6020 ~ 6026   (Run_F는 기존 Run = 11)
```

### 파일 구조

```
Assets/02.Scripts/
├── GameActor/State/Enemy/
│   ├── EnemyLocomotionHelper.cs    # 방향 계산 핵심 (신규)
│   ├── EnemyChaseState.cs          # Run 방향성 적용
│   ├── EnemyPatrolState.cs         # WalkSlow 방향성 적용
│   ├── EnemyCircleState.cs         # Walk 방향성 적용
│   ├── EnemyRetreatState.cs        # Walk 방향성 (주로 Walk_B)
│   └── EnemyFlankState.cs          # Run 방향성 적용
│
└── Data/Enum/
    └── AnimKey.cs                  # 6000번대 방향성 키 추가
```

---

## 방향 분기 기준

모든 방향 계산은 **액터의 로컬 공간 기준**. 몸은 타겟을 향하므로 로컬 Forward = 타겟 방향.

```
          앞 (0°)
     FL45 (−45°) │ FR45 (+45°)
                 │
  L90 (−90°) ───┼─── R90 (+90°)
                 │
    BL45(−135°)  │  BR45(+135°)
          뒤 (±180°)
```

| 각도 범위 | Walk 키 | Run 키 | Walk Slow 키 |
|-----------|---------|--------|--------------|
| ±0~22.5° | `Walk` | `Run` | `Walk_Slow` |
| +22.5~67.5° | `Walk_F_R45` | `Run_F_R45` | `Walk_Slow_F_R45` |
| −22.5~−67.5° | `Walk_F_L45` | `Run_F_L45` | `Walk_Slow_F_L45` |
| +67.5~112.5° | `Walk_F_R90` | `Run_F_R90` | `Walk_Slow_F_R90` |
| −67.5~−112.5° | `Walk_F_L90` | `Run_F_L90` | `Walk_Slow_F_L90` |
| +112.5~157.5° | `Walk_B_R45` | `Run_B_R45` | `Walk_Slow_B_R45` |
| −112.5~−157.5° | `Walk_B_L45` | `Run_B_L45` | `Walk_Slow_B_L45` |
| ±157.5~180° | `Walk_B` | `Run_B` | `Walk_Slow_B` |

---

## 핵심 클래스

### `EnemyLocomotionHelper`

```csharp
// 네임스페이스: UPlayGround.State
public static class EnemyLocomotionHelper
{
    public enum LocoStyle { WalkSlow, Walk, Run }

    // 임계 속도 — 이 이하면 방향 갱신 생략
    public const float MIN_SPEED_SQ = 0.25f; // 0.5 m/s

    // 로컬 각도(deg) → AnimKey
    public static AnimKey GetKey(float localAngleDeg, LocoStyle style);

    // 월드 속도 → AnimKey
    public static AnimKey GetDirectionalKey(
        Vector3 worldVelocity, Transform actorTransform, LocoStyle style);

    // 스타일의 전진 기본 키 반환
    public static AnimKey ForwardKey(LocoStyle style);

    // UpdateState에서 매 프레임 호출 — 키가 바뀔 때만 PlayMotion
    public static void UpdateAnim(
        GameActor actor,
        KinematicCharacterController.KinematicCharacterMotor motor,
        ref AnimKey lastKey,
        LocoStyle style,
        float crossfade = 0.15f);
}
```

### 각 상태별 스타일 및 동작

| 상태 | LocoStyle | OnEnter 기본 키 | 방향 갱신 조건 |
|------|-----------|-----------------|---------------|
| `EnemyChaseState` | `Run` | `AnimKey.Run` | 매 프레임 (타겟 근접 시 strafe 반영) |
| `EnemyFlankState` | `Run` | `AnimKey.Run` | 측면 기동 방향 반영 |
| `EnemyCircleState` | `Walk` | `AnimKey.Walk` | 비정지 상태에서만 |
| `EnemyRetreatState` | `Walk` | `AnimKey.Walk_B` | 매 프레임 |
| `EnemyPatrolState` | `WalkSlow` | `AnimKey.Walk_Slow` | 이동 중에만 |

---

## 셋업 방법

### 1. 애니메이션 클립 등록 (LocoMotionSetupWindow 활용)

1. 메뉴: `UPlayGround > Util > Locomotion Motion Setup`
2. 스캔 폴더: FBX 파일이 있는 `Base Move` 폴더 경로 지정
3. 등록 대상: `Humanoid_Common_MotionAsset` SO 지정 (없으면 먼저 생성)
4. `InPlace 버전 우선` 체크 (루트모션 없는 몬스터에 권장)
5. `폴더 스캔` → `MotionSetAsset 생성 / 업데이트`

**등록되는 AnimKey 목록:**

```
Walk_Slow (8방향): Walk_Slow, Walk_Slow_B, Walk_Slow_B_L45/R45,
                   Walk_Slow_F_L45/R45, Walk_Slow_F_L90/R90
Walk (8방향):      Walk, Walk_B, Walk_B_L45/R45,
                   Walk_F_L45/R45, Walk_F_L90/R90
Run (8방향):       Run, Run_B, Run_B_L45/R45,
                   Run_F_L45/R45, Run_F_L90/R90
```

### 2. 몬스터 ActorAnimationMotionSet에 Fallback 연결

1. 각 몬스터 SO 선택 (예: `Skeleton_Sword_MotionAsset`)
2. 인스펙터 상단 `Fallback MotionSet` → `Humanoid_Common_MotionAsset` 드래그
3. 이후 Idle·Walk·Hit 등 공통 모션은 자동으로 Humanoid_Common에서 가져옴

### 3. 특정 방향 클립이 없을 때 처리

- `ActorAnimator.PlayMotion(key)` 호출 시 해당 키가 없으면 null 반환하고 기존 애니메이션 유지
- 클립이 없는 방향은 이전 방향 클립이 그대로 재생됨 (자동 graceful fallback)
- 필요한 방향 클립만 선택적으로 등록해도 동작함

---

## 사용 예시

### 신규 Enemy 상태에서 방향성 로코모션 적용

```csharp
public class EnemyNewState : GameActorState
{
    private AnimKey _lastLocoKey = AnimKey.None;

    public override void OnEnter(GameActorState fromState)
    {
        base.OnEnter(fromState);
        _lastLocoKey = AnimKey.Walk;
        gameActor.Animator.PlayMotion(AnimKey.Walk, 0.25f); // 초기 클립
    }

    public override void UpdateState(float deltaTime)
    {
        // 지면 체크, 타겟 체크 등 ...

        // 방향성 로코모션 갱신 (키가 바뀔 때만 PlayMotion 호출)
        EnemyLocomotionHelper.UpdateAnim(
            gameActor, motor, ref _lastLocoKey, 
            EnemyLocomotionHelper.LocoStyle.Walk);
    }
}
```

### 직접 키 계산이 필요한 경우

```csharp
// 현재 속도 방향으로 키 조회
AnimKey key = EnemyLocomotionHelper.GetDirectionalKey(
    motor.Velocity,
    gameActor.transform,
    EnemyLocomotionHelper.LocoStyle.Run);

// 특정 각도로 직접 키 조회 (-180~180, 오른쪽 양수)
AnimKey key2 = EnemyLocomotionHelper.GetKey(90f, EnemyLocomotionHelper.LocoStyle.Walk);
// → AnimKey.Walk_F_R90 (오른쪽 90도 스트레이프)
```

### EnemyCircleState 정지/재개 처리

```csharp
// 정지 시작: Idle 재생 (CircleState 내부)
gameActor.Animator.PlayMotion(AnimKey.Idle, 0.2f);

// 정지 해제 시: _lastLocoKey = None 으로 리셋 → 다음 UpdateState에서 즉시 재평가
_lastLocoKey = AnimKey.None;
// UpdateState에서 UpdateAnim이 자동으로 올바른 방향 클립 재생
```

---

## 지원 FBX 파일명 규칙

LocoMotionSetupWindow가 인식하는 Base 클립 파일명 패턴:

```
Walk_Slow_F.fbx        Walk_Slow_F_InPlace.fbx
Walk_Slow_B.fbx        Walk_Slow_B_L45.fbx   Walk_Slow_B_R45.fbx
Walk_Slow_F_L45.fbx    Walk_Slow_F_R45.fbx
Walk_Slow_F_L90_A.fbx  Walk_Slow_F_R90_A.fbx  (A 버전 우선, B 버전 자동 무시)

Walk_F.fbx   Walk_B.fbx   Walk_B_L45.fbx   Walk_B_R45.fbx
Walk_F_L45.fbx   Walk_F_R45.fbx
Walk_F_L90_A.fbx   Walk_F_R90_A.fbx

Run_F.fbx    Run_B.fbx    Run_B_L45.fbx    Run_B_R45.fbx
Run_F_L45.fbx    Run_F_R45.fbx
Run_F_L90_A.fbx    Run_F_R90_A.fbx
```

> `_InPlace` 접미사가 붙은 버전은 루트모션 없는 클립. 몬스터는 물리 이동(KCC)을 사용하므로 InPlace 버전 권장.

---

## 주의 사항

- **UpdateVelocity와 UpdateState 분리**: `UpdateVelocity`에서 계산된 속도가 `motor.Velocity`에 반영되는 시점은 다음 프레임. 따라서 애니메이션은 항상 1프레임 지연된 속도 기준으로 선택됨 — 실제로는 체감되지 않음
- **MIN_SPEED_SQ 임계**: 0.25 (0.5 m/s)이하면 방향 갱신 생략. 매우 느린 이동이 필요한 경우 `EnemyLocomotionHelper.MIN_SPEED_SQ`를 낮추거나 직접 `GetDirectionalKey`를 호출
- **L90 A/B 클립**: `Walk_F_L90_A`와 `Walk_F_L90_B`는 보행 사이클의 양 발 버전. 현재는 A 버전만 등록. 완벽한 발 교차를 원하면 `LR90_Shuffle` 클립과 함께 별도 상태 머신 확장 필요
- **비행 몬스터 제외**: `EnemyFlying*` 상태들은 별도 비행 로코모션 체계 사용. 이 시스템의 적용 대상이 아님

---

## 확장 포인트

### Sprint 방향성 추가

Sprint 클립이 있는 경우 AnimKey와 LocoStyle에 Sprint 추가 가능:

```csharp
// AnimKey.cs에 추가
Sprint_B = 6030,
Sprint_F_L45,
Sprint_F_R45,

// LocoStyle에 Sprint 추가
public enum LocoStyle { WalkSlow, Walk, Run, Sprint }
```

### 속도 기반 자동 스타일 전환

```csharp
// 속도 크기에 따라 자동으로 Walk/Run 전환하는 래퍼 예시
float speed = motor.Velocity.magnitude;
var style = speed > runThreshold ? LocoStyle.Run
          : speed > walkThreshold ? LocoStyle.Walk
          : LocoStyle.WalkSlow;
EnemyLocomotionHelper.UpdateAnim(gameActor, motor, ref _lastLocoKey, style);
```

### 신규 몬스터 로코모션 클립 세트 추가

1. `Humanoid_Common_MotionAsset`에 없는 방향 AnimKey가 필요하면 신규 SO 생성
2. 해당 SO를 몬스터 고유 `ActorAnimationMotionSet`에 등록
3. 공용 에셋을 fallback으로 유지하면서 특정 방향만 Override 가능
