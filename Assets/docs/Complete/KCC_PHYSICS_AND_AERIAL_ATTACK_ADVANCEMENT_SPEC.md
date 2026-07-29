# KCC 물리·Impulse·공중 공격 고도화 스펙

> 작성일: 2026-07-26
>
> 상태: 핵심 구조 구현, Unity 테스트·Play Mode·Player Build 검증 대기
>
> 대상 버전: Unity 6 (6000.0.60f1), Kinematic Character Controller 3.4.4,
> Animancer Pro V8, URP
>
> 적용 범위: `ActorMovementController`, 플레이어/몬스터 상태의 속도·중력 합성,
> Impulse/Launch/Pull, Animator Root Motion 전달, Motion Warp, `KCCSimulator`,
> 점프 공격·공중 대시 공격
>
> 비적용 범위: KCC 플러그인 교체, Rigidbody 기반 캐릭터로 전환, 전체 상태 머신 교체,
> 애니메이션 클립 자체의 재제작
>
> 관련 코드:
>
> - `Assets/02.Scripts/GameActor/MovementController/ActorMovementController.cs`
> - `Assets/02.Scripts/GameActor/MovementController/PlayerMovementController.cs`
> - `Assets/02.Scripts/GameActor/MovementController/MotionWarpController.cs`
> - `Assets/02.Scripts/GameActor/Animation/ActorAnimator.cs`
> - `Assets/02.Scripts/GameActor/State/Base/GameActorState.cs`
> - `Assets/02.Scripts/GameActor/State/Player/PlayerAirbornState.cs`
> - `Assets/02.Scripts/GameActor/State/Player/PlayerJumpAttackState.cs`
> - `Assets/02.Scripts/GameActor/State/Player/PlayerJumpDashAttackState.cs`
> - `Assets/02.Scripts/Manager/KCCSimulator.cs`
> - `Assets/02.Scripts/GameActor/Animation/MotionEvents/MotionEvent_AddForce.cs`
>
> 공식/내장 참고:
>
> - `Assets/ExternalAssets/Etc/KinematicCharacterController/Walkthrough/5- Adding velocities and impulses/`
> - `Assets/ExternalAssets/Etc/KinematicCharacterController/Walkthrough/15- Root motion example/`
> - [Unity Root Motion 매뉴얼](https://docs.unity3d.com/kr/current/Manual/RootMotion.html)
> - [Unity `Animator.applyRootMotion`](https://docs.unity3d.com/kr/current/ScriptReference/Animator-applyRootMotion.html)
> - [Unity `OnAnimatorMove`](https://docs.unity3d.com/kr/current/ScriptReference/MonoBehaviour.OnAnimatorMove.html)

## 구현 진행 상태

- 2026-07-26 핵심 런타임 구현과 Advisor 사후 리뷰를 반영했다.
- 완료: KCC 전역 Phase 장벽 복구, `LocalTimeScale` 입력 안전화, Root Motion 누적·단일
  소비 브리지, 상태 전환 stale delta 제거, 충돌 권위 속도 기반 Impulse/방향 감쇠,
  중력 소유권 명시, 점프 공격 공중 프로필 및 비접지 종료 전이, 점프 대시 수직 탄도 보존.
- 완료: 방향 감쇠 4건, Root Motion exact-once 버퍼 3건과 에디터 자동 실행 경로 추가.
- 검증 완료: Unity 스크립트 임포트 오류 0, Unity Test Runner EditMode 7/7 통과.
- CLI 보조 검증: `UPlayGround.Actor.csproj`, `Assembly-CSharp.csproj`,
  `UPlayGround.Movement.Tests.csproj` 오류 0.
- 대기: Play Mode 수직 슬라이스, 이동 플랫폼 매트릭스, Motion Warp 회귀,
  Player Build, 에셋 참조 무결성 검증.
- `Assets/10.Datas/`와 플레이어/몬스터 프리팹을 일괄 저장하지 않는다. 데이터 필드를
  추가할 경우 먼저 코드·타입 매핑을 복구하고, 변경된 에셋 diff를 검사한다.

---

## 1. 결론

현재 점프 공격의 부자연스러움은 애니메이션 클립의 Root Motion 자체보다
`PlayerJumpAttackState.UpdateVelocity`가 매 KCC 갱신마다 전체 속도를 아래 방향
`15m/s`로 덮어쓰는 것이 1차 원인이다.

```csharp
// 현재 PlayerJumpAttackState
currentVelocity = motor.CharacterUp * -15f;
```

이 대입은 점프에서 넘어온 상승 속도, 수평 관성, 공중 조작 결과를 모두 제거한다.
그 뒤 `ActorMovementController`의 공통 중력이 한 번 더 적용되므로 실제 결과는
“자연 낙하”가 아니라 매 물리 프레임 거의 같은 강하 속도를 재설정하는 형태다.

문제는 한 상태에 한정되지 않는다. 현재 속도 파이프라인에는 다음 네 가지 소유권이
명시적으로 분리되어 있지 않다.

```text
상태 이동(Locomotion)
+ 중력/탄도(Ballistic)
+ 애니메이션 Root Motion / Motion Warp
+ 외부 속도 변화(Impulse/Launch/Pull)
→ KCC 충돌 해결
```

상태가 전체 벡터를 대입하거나, 상태와 컨트롤러가 중력을 각각 적용하거나,
KCC가 충돌 해결한 속도에서 충돌 전 Impulse 원본을 다시 빼는 경로가 존재한다.
따라서 목표는 개별 매직 넘버 조정이 아니라 **속도 축과 단계별 단일 소유권 확립**이다.

---

## 2. 현재 실행 흐름

### 2.1 프레임 흐름

```text
Update
├─ ActorMovementController.Update
│   └─ CurrentState.UpdateState(Actor.DeltaTime)
└─ Animator/Animancer 평가
    └─ ActorAnimator.OnAnimatorMove

FixedUpdate
└─ KCCSimulator
    ├─ LocalTimeScale별 Motor 그룹 구성
    └─ KinematicCharacterSystem.Simulate(scaledDt)
        ├─ BeforeCharacterUpdate
        ├─ PostGroundingUpdate
        ├─ Attached Rigidbody 감지·이동
        ├─ PhysicsMover 목표 pose 확정
        ├─ UpdateRotation
        ├─ Mover 이동으로 생긴 overlap 해결
        ├─ UpdateVelocity
        ├─ KCC sweep / 충돌 속도 투영
        └─ AfterCharacterUpdate
```

상태 전환·모션 타임라인은 렌더 프레임, 실제 Motor 이동은 물리 프레임이다.
이 경계를 건너는 Root Motion은 반드시 누적·단일 소비 계약을 가져야 한다.

위 순서는 KCC 원본 `Simulate`의 의도된 순서다. 현재 커스텀 `KCCSimulator`는 실제로는
Mover 없이 그룹별 `Simulate`를 끝까지 호출하므로 §3.11의 2단계 barrier 문제가 있다.

### 2.2 현재 `ActorMovementController.UpdateVelocity`

```text
KCC가 넘긴 currentVelocity
→ 저장된 _impulseVelocity 차감
→ State.UpdateVelocity
→ 공통 중력(AdjustGravity=true이고 비접지)
→ _internalVelocityAdd 1회 가산
→ _impulseVelocity 지수 감쇠
→ 감쇠된 Impulse 재합산
→ KCC 충돌 해결
```

이 흐름의 핵심 문제는 입력 `currentVelocity`가 이미 이전 프레임의 KCC 충돌 해결 결과라는
점이다. `_impulseVelocity`는 충돌 전 채널 값이므로 두 값은 다음 프레임에 더 이상 같은
성분을 공유한다고 보장할 수 없다.

---

## 3. 확정 결함과 위험

아래에서 **확정**은 정적 코드 경로만으로 성립하는 항목, **검증 필요**는 프레임 순서·클립
Import 설정·씬 조건에 따라 체감 크기가 달라 Play Mode 계측이 필요한 항목을 뜻한다.

### 3.1 P0 — 점프 공격이 공중 속도를 전부 제거한다

**분류: 확정**

`PlayerJumpAttackState.UpdateVelocity`는 `currentVelocity` 전체를
`CharacterUp * -15f`로 대입한다.

영향:

- 점프 상승 중 공격해도 즉시 하강한다.
- 점프 직전의 수평 관성이 사라진다.
- 일반 공중 상태의 `MaxAirMoveSpeed`, `AirAccelerationSpeed`, `Drag`가 적용되지 않는다.
- 같은 모션이라도 공격 입력 프레임에 따라 궤적 연속성이 끊긴다.
- 상태 안의 `base.UpdateVelocity`는 빈 구현이므로 보존 효과가 없다.
- `AdjustGravity=true`이므로 대입 뒤 공통 중력까지 추가된다.
- 모션 완료 시 비접지 여부를 확인하지 않고 `GroundMove`/`Idle`로 전환하므로,
  공격이 공중에서 끝나면 지상 상태가 공중에서 실행될 수 있다.

**금지 수정:** `-15f`를 `-5f`로 낮추는 것만으로 완료 처리하지 않는다. 속도 불연속의
원인이 대입 자체이므로 값 조정은 증상만 약화한다.

### 3.2 P0 — `JumpDashAttack` 중력이 이중 적용된다

**분류: 확정**

`PlayerJumpDashAttackState`는 `AdjustGravity=true`인 동시에 상태 내부에서 다음 중력을
직접 더한다.

```csharp
currentVelocity += controller.Gravity
                   * controller.FallGravityMultiplier
                   * deltaTime;
```

상태 호출이 끝난 뒤 `ActorMovementController`도 비접지 상태에서 공통 중력을 적용한다.
또한 상태가 먼저 `currentVelocity = attackDirection * speed`로 전체 벡터를 덮어써
기존 수직 속도를 제거한다.

결과적으로 공중 대시 공격의 Y는:

```text
기존 수직 속도 제거
+ 상태 내부 FallGravity
+ 컨트롤러 공통 Rise/FallGravity
```

가 된다.

### 3.3 P0 — 중력 소유권이 전 상태에서 일관되지 않다

**분류: 확정**

`GameActorState.AdjustGravity` 기본값은 `true`다. 그럼에도 여러 상태가
`controller.Gravity * deltaTime`을 직접 더한다. 비접지 순간에는 공통 중력과 상태 중력이
중복될 수 있다.

우선 감사 대상:

- `EnemyAttackState`
- `EnemyChargeState`
- `EnemyCounterState`
- `EnemyDeathState`
- `EnemyDodgeState`
- `EnemySpecialBreakVictimState`
- `EnemyStepState`
- `EnemyFlyingGroundAttackState`
- `PlayerDeathState`
- `PlayerInteractionState`
- `PlayerJumpDashAttackState`

반대로 `PlayerAirborneState`, `EnemyAirborneState`, `EnemyJumpBackState`처럼 상태가 중력을
소유하면서 `AdjustGravity=false`를 명시한 경로도 있다. 즉 현재 boolean은 “중력 사용 여부”와
“중력 계산 주체” 두 의미를 동시에 표현하고 있다.

### 3.4 P0 — Root Motion 델타가 렌더/물리 경계를 안전하게 건너지 않는다

**분류: 확정 구조 위험, 체감 크기 검증 필요**

`ActorAnimator.OnAnimatorMove`는 `DeltaPosition`, `DeltaRotation`을 덮어쓰기만 하고
소비 후 초기화하지 않는다. 반면 Root Motion 소비자는 KCC `FixedUpdate`에서
`DeltaPosition / deltaTime` 또는 `DeltaRotation`을 읽는다.

가능한 실패:

- 한 렌더 델타를 둘 이상의 물리 스텝이 재사용한다.
- 여러 Animator 평가 사이의 델타가 누적되지 않고 마지막 값만 남는다.
- Root Motion 비활성 구간에 이전 델타가 남는다.
- 프레임레이트·Fixed Timestep·히트스톱 비율에 따라 이동량이 달라진다.
- Motion Warp의 `_accumRootPath`, `_accumRootLocal` 캐시 입력도 같은 영향을 받는다.

KCC 3.4.4 내장 Root Motion 예제는 `OnAnimatorMove`에서 위치·회전 델타를 누적하고,
`AfterCharacterUpdate`에서 초기화한다. 현재 구현은 이 계약을 따르지 않는다.

### 3.5 P0 — Root Motion 활성 계약과 소비 계약이 불일치한다

**분류: 확정 구조 불일치, 실제 콜백 발생 여부 검증 필요**

플레이어 계열 프리팹의 Animator 초기값은 혼재한다. 예를 들어
`Assets/03.Prefabs/Actor/Player/Player.prefab` 내부 Animator들은
`m_ApplyRootMotion: 0`인 반면 `Player_Bokusei.prefab`에는 `1`인 Animator가 있다.
실제 런타임 모델 조합의 최종값은 계측 전 확정하지 않는다.

확정된 사실은 일부 상태가 런타임에 `ApplyRootMotion(true/false)`를 토글한다는 점이다.
그런데 `PlayerJumpAttackState`와
`PlayerJumpDashAttackState`는 Root Motion을 활성화하지 않으면서 `DeltaRotation`을 소비한다.

Unity 문서상 `OnAnimatorMove`를 구현하면 Built-in Root Motion의 자동 Transform 적용에는
`applyRootMotion` 값이 효과가 없지만, 런타임에 그 값을 변경하면 Animator는 재초기화된다.
반면 callback과 delta 생성 여부는 실제 활성 Animator/Animancer 구성별로 계측한다.

따라서 목표 구조에서는 상태별 true/false 토글을 제거하고, 델타 생성은 일정하게 유지한 채
실제 이동 반영 여부만 정책으로 결정한다.

### 3.6 P0 — 현재 Impulse 분해는 KCC 충돌 후 속도와 불일치한다

**분류: 수학적으로 확정, 재현 강도 검증 필요**

현재 식:

```text
stateVelocity(n) = KCCResolvedVelocity(n) - StoredImpulse(n)
finalVelocity(n) = stateVelocity(n) + DecayedImpulse(n)
```

충돌 전에는 `KCCResolvedVelocity` 안에 `StoredImpulse`가 포함됐다고 볼 수 있다.
그러나 벽 충돌이 해당 성분을 제거한 다음에도 저장 채널은 그대로 남는다.

예:

```text
벽 충돌 후 KCCResolvedVelocity = 0
StoredImpulse = +8
DecayedImpulse = +7

stateVelocity = 0 - 8 = -8
finalVelocity = -8 + 7 = -1
```

의도하지 않은 역방향 속도를 만들 수 있다. 경사·코너·다중 충돌에서도 같은 원리로
상태 속도가 오염될 수 있다.

정확한 잔량은 상태가 `stateVelocity`를 어떻게 수정하는지에 따라 달라진다.
해당 축을 거의 보존하는 공중 상태에서 상태 감쇠율을 `p`, Impulse 감쇠율을 `d`라 하면
벽 충돌 직후 출력은 대략 `(d - p)I`다. 현재 기본값 근처에서는 `p > d`가 될 수 있어
역방향이 되고, 지상 Idle처럼 상태 감쇠가 더 강하면 같은 방향 잔량이 남을 수도 있다.
즉 **충돌 후 결과가 외력 정책이 아니라 현재 State 감쇠 구현에 따라 달라지는 것** 자체가
결함이며, 실제 크기는 Play Mode에서 측정한다.

추가 결함:

- 여러 Impulse를 `+=`로 합치지만 Drag는 마지막 값 하나가 전체 합에 적용된다.
- 최초 적용 프레임에도 감쇠한 뒤 합산하여 요청 속도보다 작게 적용한다.
- 수직 Launch도 지수 Drag로 감쇠하면서 중력까지 받아 비탄도적으로 움직인다.
- 상태가 전체 속도를 대입하면 외력과 상태 이동의 우선순위가 호출 순서에 의존한다.

### 3.7 P1 — `AddForceEvent`의 단위와 이름이 실제 동작과 다르다

**분류: 확정**

`AddForceEvent`는 질량이나 `deltaTime`을 사용하지 않고
`AddVelocity(worldDirection * force)`를 1회 호출한다. 실제 단위는 Force(N)가 아니라
속도 변화량(m/s)에 가깝다.

호환성을 깨는 즉시 타입 삭제는 하지 않는다. `[SerializeReference]` 데이터이므로
기존 `AddForceEvent`는 유지하거나 `[MovedFrom]`을 포함한 별도 마이그레이션이 필요하다.
에디터 표시명과 필드 Tooltip부터 “속도 변화” 의미를 명확히 한다.

### 3.8 P1 — Ignore Collider API가 실제 필터에 연결되지 않는다

**분류: 확정**

`AddIgnoreCollider`와 `RemoveIgnoreCollider`는 `IgnoredColliders`를 관리하지만
`IsColliderValidForCollisions`는 항상 `true`를 반환한다. 호출자는 무시가 적용됐다고
오해할 수 있다.

목표:

```csharp
return coll != null && !IgnoredColliders.Contains(coll);
```

목록이 커질 수 있으면 `HashSet<Collider>`로 전환하고 파괴된 Collider를 정리한다.

### 3.9 P1 — Ledge 설정과 자체 Raycast가 서로 다른 접지 체계를 만든다

**분류: 확정 설정, 최적값 검증 필요**

주 플레이어 프리팹의 주요 KCC 값:

| 항목 | 현재 값 |
|---|---:|
| `MaxStableSlopeAngle` | 60 |
| `MaxStepHeight` | 0.5 |
| `MaxStableDistanceFromLedge` | 0.26 |
| `MaxVelocityForLedgeSnap` | 0 |
| `GroundDetectionExtraDistance` | 0 |
| `PreserveAttachedRigidbodyMomentum` | true |

`PlayerActorState`는 `MaxVelocityForLedgeSnap=0`에서 KCC 프로브가 짧아지는 문제를
자체 Raycast와 `AirborneGracePeriod`로 우회한다. 이 우회는 KCC의 안정 지면 판정과
별도의 Physics 질의를 만들므로 경사, 얇은 발판, 계단 가장자리에서 두 판정이 다를 수 있다.

설정값을 즉시 전역 변경하지 않는다. Phase 0 계측 후:

- 일반 Run은 스냅 허용
- Sprint/Dash는 가장자리 스냅 방지
- 얇은 발판과 내리막 계단에서 조기 Airborne 없음

을 만족하는 임계값을 찾는다. 초기 실험 범위는 `8~10m/s`지만 확정 기본값은 아니다.

### 3.10 P1 — Rigidbody 상호작용은 현재 사실상 비활성 정책이다

**분류: 확정 설정**

주 플레이어 프리팹은 `InteractiveRigidbodyHandling=true`지만
`RigidbodyInteractionType=0(None)`이다. 이동 플랫폼 부착/이탈 관성은 별도 경로로
동작할 수 있으나, 동적 Rigidbody를 밀어내는 캐릭터 반응을 기대해서는 안 된다.

상자를 밀거나 물리 오브젝트와 질량감 있는 상호작용이 필요한 경우에만
`SimulatedDynamic`을 별도 테스트한다. TPS 전투 캐릭터의 이동 안정성을 해칠 수 있으므로
본 작업에서 자동 전환하지 않는다.

### 3.11 P0 — `KCCSimulator`가 KCC의 2단계 Mover barrier를 깨뜨린다

**분류: 순서 위반 확정, 콘텐츠 증상 크기 검증 필요**

KCC 원본 `KinematicCharacterSystem.Simulate` 순서:

```text
모든 PhysicsMover VelocityUpdate
→ 모든 Motor UpdatePhase1
→ 모든 PhysicsMover를 Transient pose로 이동
→ 모든 Motor UpdatePhase2
```

`UpdatePhase2`는 주석과 구현상 “Mover가 목표 pose로 이동한 뒤 생긴 overlap”을 해결한다.

현재 `KCCSimulator` 순서:

```text
모든 PhysicsMover VelocityUpdate(baseDt)
→ LocalTimeScale group A Simulate(scaledDt, groupA, emptyMovers)
   └─ group A Phase1 + Phase2
→ LocalTimeScale group B Simulate(scaledDt, groupB, emptyMovers)
   └─ group B Phase1 + Phase2
→ 모든 PhysicsMover pose 확정
```

즉 모든 Motor의 Phase2가 Mover pose 확정보다 먼저 실행되어 원본의 overlap 해결 전제가
성립하지 않는다. 이는 타임스케일이 1인 액터에도 적용되는 순서 위반이다.

LocalTimeScale이 1이 아닌 부착 액터에는 추가 문제가 있다. Mover는 `baseDt`만큼 이동하지만
Motor Phase1의 attached-rigidbody 이동은 `scaledDt`를 사용하므로, 느려진 액터는 플랫폼보다
뒤처진다. 체감 크기는 플랫폼 속도와 히트스톱 길이에 따라 달라지지만 수식상 시간축 불일치는
확정이다.

정책을 먼저 결정해야 한다.

- 권장 기본: 플랫폼 carry·world collision은 `baseDt`
- 자발 이동·중력·외력·상태 타이머는 actor local time domain

그러나 KCC 3.4.4의 단일 `deltaTime` API만으로 두 시간축을 동시에 만족시키기 어렵다.
Phase 1A에서 최소한 전역 Phase1/Mover pose/전역 Phase2 barrier를 복원하고,
carry 시간축 분리는 별도 설계 실험으로 검증한다.

또한 `GameActor.LocalTimeScale` setter에는 clamp가 없어 0과 음수도 허용한다.
0은 Root Motion `/ deltaTime` 경로의 NaN/Infinity 위험, 음수는 KCC 역시간 시뮬레이션
위험이 있으므로 허용 범위와 freeze 정책을 명시해야 한다.

현재 `PhysicsMover` 직접 참조는 확인 범위에서
`Assets/01.Scenes/Test/KccTest.unity`에 집중되어 있고 본편 플레이어/몬스터 프리팹과
일반 씬에서는 확인되지 않았다. 따라서 **구조 결함은 확정**이지만 현재 본편 콘텐츠 영향은
테스트 씬 중심일 수 있으며, 향후 이동 플랫폼 콘텐츠 도입 전에 반드시 해결한다.

---

## 4. 목표 속도 합성 모델

### 4.1 원칙

1. 이전 KCC 출력은 충돌 해결이 끝난 **권위 속도**다.
2. 이전 프레임 외력 원본을 권위 속도에서 역산해 빼지 않는다.
3. 상태는 필요한 축만 수정한다. 전체 대입은 명시적인 Full Override에서만 허용한다.
4. 중력 적분은 프레임당 한 주체만 수행한다.
5. Root Motion은 변위로 수집하고 KCC 시뮬레이션에서 한 번만 소비한다.
6. 일반 공중 공격의 Y는 물리가 소유한다. 클립 Y는 기본적으로 포즈에만 남긴다.
7. Motion Warp의 Y 변경은 명시적인 `WarpYPolicy`가 있을 때만 허용한다.

### 4.2 목표 파이프라인

```text
KCC resolved velocity from previous step
    │
    ├─ State planar intent / explicit axis override
    ├─ Controller-owned gravity or State-owned ballistic policy
    ├─ Root Motion policy
    │    ├─ Ignore
    │    ├─ AdditivePlanar
    │    ├─ OverridePlanar
    │    └─ FullOverride (제한적)
    ├─ Pending one-shot velocity changes
    ├─ Directional damping modifiers
    └─ terminal/clamp/finite 검증
         ↓
KCC collision solve
         ↓
next authoritative velocity
```

### 4.3 중력 계약

기존 `AdjustGravity` boolean을 장기적으로 다음 의미로 분리한다.

```csharp
public enum GravityOwnership
{
    Controller, // 컨트롤러가 Gravity * Scale을 1회 적분
    State,      // 비행·특수 이동처럼 상태가 수직 속도를 완전 소유
    Disabled,   // 잡힘·연출 고정 등 중력 없음
}
```

`Controller` 상태는 중력을 직접 더하지 않고 동적 배율만 제공한다.

```csharp
public virtual float EvaluateGravityScale(float verticalSpeed)
    => verticalSpeed < 0f
        ? controller.FallGravityMultiplier
        : controller.RiseGravityMultiplier;
```

`PlayerAirborneState`의 가변 중력은 이 정책으로 이동한다. 비행 상태와 Dive처럼 목표 속도를
직접 만드는 상태만 `State`를 사용한다.

### 4.4 외력 계약

Impulse는 Rigidbody Force를 모사하는 별도 영구 속도 채널이 아니라,
KCC 권위 속도에 대한 **1회성 속도 변화**로 적용한다.

```csharp
QueueVelocityChange(Vector3 deltaVelocity);
AddPlanarKnockback(Vector3 deltaVelocity, float directionalDrag);
AddLaunch(float upwardSpeed, Vector3 planarVelocity);
```

권장 동작:

- `QueueVelocityChange`: 다음 KCC 합성 마지막 단계에서 정확히 한 번 더한다.
- `AddLaunch`: 현재 Up 성분을 정책에 따라 교체/가산하고 `ForceUnground(0.1f)` 호출.
- `AddPlanarKnockback`: 속도 변화는 한 번 적용하고, 이후 원래 넉백 방향의 양수 성분만
  지수 감쇠하는 modifier를 등록한다.
- 벽 충돌 후 해당 방향 속도가 0이면 modifier는 음수 반동을 만들지 않는다.
- 수직 Launch에는 기본적으로 지수 Drag를 적용하지 않고 중력으로만 탄도를 만든다.

방향성 감쇠 개념:

```csharp
float along = Vector3.Dot(currentVelocity, direction);
if (along > 0f)
{
    float next = along * Mathf.Exp(-drag * deltaTime);
    currentVelocity += direction * (next - along);
}
```

여러 넉백 modifier는 각각 방향·Drag·수명을 소유한다. “마지막 호출 Drag가 전체 합에 적용”되는
현재 문제를 없앤다.

---

## 5. 공중 공격 이동 정책

### 5.1 목표 체감

일반 점프 공격은 강제 호버가 아니라 다음 곡선을 사용한다.

```text
점프 상승 관성 유지
→ 공격 Startup 동안 중력 완화
→ 정점 부근 짧은 체공
→ Active/Recovery에서 정상 또는 강화 중력
→ KCC 착지
```

`verticalVelocity=0` 고정이나 매 프레임 `-15m/s` 대입은 사용하지 않는다.

### 5.2 정책 종류

현재 구현은 기존 `AbilityAttackInfo.isDiveAttack`을 강하 모드로 유지하고,
그 외 공격은 `AerialMovementProfile` 값으로 `PreserveBallistic`, Apex Hang,
Aerial Dash의 수직 탄도를 표현한다. 별도 `FullAuthored` 전체 이동 모드는 이번 범위에
포함하지 않았다.

권장 기본값:

| 항목 | 시작 범위 | 의미 |
|---|---:|---|
| `horizontalRetention` | 0.8~1.0 | 진입 수평 관성 유지 |
| `minimumEntryUpSpeed` | 2~3m/s | 하강 중 입력 시 작은 리프트가 필요할 때만 |
| `startupGravityScale` | 0.2~0.4 | 공격 준비 중 중력 |
| `apexVelocityRange` | 1~1.5m/s | 정점 체공 적용 범위 |
| `apexGravityScale` | 0.1~0.25 | 정점 부근 중력 |
| `maximumHangTime` | 0.08~0.15s | 무한 체공 방지 |
| `recoveryGravityScale` | 1.2~1.8 | 공격 후 낙하 회복 |
| `terminalFallSpeed` | 18~25m/s | 강하 상한 |
| `airControlScale` | 0~0.5 | 공격 중 공중 조작 허용량 |

이 값들은 확정 밸런스가 아니라 첫 Play Mode 실험 범위다.

### 5.3 데이터 위치

공격 수치의 단일 원본이 `AbilitySetSO → GameplayAbilitySO →
UPlayGroundMotionAbilityPayloadSO → AbilityAttackInfo`이므로 공중 이동 정책도 별도 레거시
공격 데이터에 추가하지 않는다.

구현:

```text
AbilityAttackInfo
└─ serializable AerialMovementProfile
```

- 필드가 없거나 null이면 안전 기본값 `PreserveBallistic`을 사용한다.
- 안전 기본은 중력 배율 1, 최소 상승 보정 0, 정점 구간 0초, 수평 Root Motion 영향 0,
  종단 낙하 제한 0(무제한)이다.
- 첫 구현은 기존 Payload의 대량 재직렬화를 피하기 위해 중첩 직렬화 클래스로 둔다.
  반복 튜닝 결과 공통 프리셋 공유가 필요해질 때 SO 참조화를 별도 마이그레이션으로 진행한다.
- `PlayerSkillSlot`, MotionSet, AnimationClip Import 설정을 공격 물리 수치의 원본으로 삼지 않는다.
- 몬스터도 같은 프로필 타입을 사용할 수 있어야 한다.
- 신규 참조가 기존 493개 Payload를 자동 변경하지 않도록 validator와 명시적 migration을 분리한다.

### 5.4 점프 공격 상태 변경

`PlayerJumpAttackState`는:

- 진입 시 현재 수직·수평 속도를 캡처하지 않고 Motor의 권위 속도를 계속 기반으로 사용
- 물리 경과 시간은 `UpdateVelocity(deltaTime)`에서 누적
- 프로필에 따라 XZ 유지/공중 조작/중력 배율만 제공
- 착지는 `PostGroundingUpdate`의 stable 전환으로 처리
- 모션 종료 시 아직 공중이면 `PlayerAirborneState`로 복귀
- 지상에 닿았을 때만 Idle/GroundMove로 전환

해야 한다.

현재 `ChangeToNextState`는 모션이 공중에서 끝나도 GroundMove/Idle로 직접 전환할 수 있으므로
함께 수정한다.

### 5.5 강하 공격

강공격 또는 명시적인 Dive 프로필에서만 아래 방향 목표 속도를 사용한다.

```text
Startup: 상승 관성 완화 또는 짧은 정지
Commit: ForceUnground + Dive 진입
Dive: 현재 Y를 매 프레임 상수로 덮지 않고 목표 하강속도로 접근
Impact: 접지 전이 1회, FX/충돌 이벤트와 분리
Recovery: Land 또는 전용 Recovery
```

---

## 6. Root Motion 브리지

### 6.1 누적 구조

`ActorAnimator`는 마지막 델타 프로퍼티 대신 누적 버퍼를 소유한다.

```csharp
public readonly struct RootMotionDelta
{
    public readonly Vector3 Position;
    public readonly Quaternion Rotation;
}
```

```text
OnAnimatorMove
→ Position += animator.deltaPosition
→ Rotation = animator.deltaRotation * Rotation

ActorMovementController.BeforeCharacterUpdate
→ 버퍼를 현재 KCC step용 snapshot으로 한 번 이동

State/RootMotionPolicy
→ snapshot을 0회 또는 1회 소비

AfterCharacterUpdate
→ 소비 여부와 무관하게 step snapshot 폐기
```

누적 버퍼 자체를 `AfterCharacterUpdate`에서 무조건 지우면, 해당 물리 스텝 전에 들어온 델타와
새 Animator 평가의 경계를 혼동할 수 있다. “pending → step snapshot” 교환 시점을
`BeforeCharacterUpdate` 하나로 고정한다.

반대로 소비하지 않은 pending/snapshot을 다음 Root Motion 상태까지 보존해서도 안 된다.
다음 경우 pending과 snapshot을 모두 flush한다.

- Root Motion 소비 상태 진입/이탈
- ActorAnimator/Animancer 모델 교체
- Animator disable/enable 또는 culling 복귀
- Teleport/Respawn/Actor swap
- KCC Motor disable/enable

### 6.2 활성 정책

- Animator Root Motion 델타 생성은 초기화 시 한 번 설정한다.
- 상태 진입/이탈 때 `applyRootMotion`을 토글하지 않는다.
- `OnAnimatorMove`가 자동 Transform 이동을 가로채고 버퍼에만 기록한다.
- 상태는 `RootMotionPolicy`로 Position/Rotation의 소비 축을 결정한다.
- 델타가 없는 물리 스텝은 identity/zero이며 이전 델타를 재사용하지 않는다.

### 6.3 축 정책

| 상태 | Position XZ | Position Y | Rotation |
|---|---|---|---|
| 일반 지상 공격 | Override/워프 | Ignore | 선택 적용 |
| DashAttack | Additive 또는 Override | Ignore | 선택 적용 |
| 일반 JumpAttack | 기본 Ignore | Physics 소유 | 선택 적용 |
| AerialDash | 정책적 XZ | Physics 소유 | 선택 적용 |
| Dive | 선택적 XZ | Dive 물리 소유 | 선택 적용 |
| TurnInPlace | Ignore | Ignore | Root Rotation |
| Stop | 선택적 XZ | Ignore | Root Rotation |

`MotionWarpController`는 현재 대부분 월드 Y=0을 기준으로 수평을 계산한다. 현 프로젝트의
Gravity가 월드 Y 고정이므로 즉시 결함은 아니지만, 새 공중 정책에서는
`Vector3.ProjectOnPlane(value, motor.CharacterUp)`을 공통 기준으로 사용한다.

### 6.4 Import 규칙

KCC가 Y를 소유하는 일반 공중 공격:

- Root Transform Position (Y): `Bake Into Pose`
- Root Transform Position (XZ): 공격 정책에 따라 통일
- Root Transform Rotation: 회전 정책에 따라 통일

클립별 Import 설정 편차는 물리 코드를 클립에 맞춰 분기하는 근거가 아니다.
Editor validator가 동일 정책 그룹의 Import 설정 불일치를 보고하도록 한다.

---

## 7. 구현 단계

### Phase 0 — 관측과 재현 고정

코드 동작을 바꾸기 전에 Development 전용 `KCC Movement Trace`를 추가한다.

프레임별 기록:

```text
actor/state
renderFrame/fixedStep
localTimeScale/deltaTime
grounded/lastGrounded
input currentVelocity
state output
gravity delta
root motion pending/consumed
queued velocity change
directional modifier delta
final submitted velocity
Motor.BaseVelocity after simulation
movement hit normal
```

필수 재현 씬/케이스:

1. 점프 상승 초반/정점/하강 중 각각 약공격
2. 30/60/120/144fps와 Fixed Timestep 0.02에서 같은 공격
3. 벽을 향한 KnockBack 후 벽 접촉
4. 경사·코너에서 KnockBack
5. 서로 다른 Drag의 Impulse를 같은 프레임에 2회 적용
6. 이동 플랫폼 위 로컬 히트스톱
7. Root Motion 공격 중 히트스톱
8. 한 렌더 프레임에 FixedUpdate 0회/1회/2회 이상 발생
9. Root Motion 상태 직전 비소비 delta, 모델 교체, offscreen culling 복귀

### Phase 1 — 점프 공격 P0 교정

- `PlayerJumpAttackState`의 전체 속도 `-15m/s` 대입 제거
- `PlayerJumpDashAttackState`의 수직 속도 보존
- JumpDash 중력 이중 적용 제거
- 모션 종료 시 공중이면 Airborne으로 복귀
- 임시 하드코딩이 아니라 안전 기본 `PreserveBallistic` 정책 사용

Phase 1은 기존 Impulse 구조와 Root Motion 구조를 한 번에 바꾸지 않는다.
회귀 원인을 분리하기 위해 점프 공격 궤적부터 고정한다.

### Phase 1A — KCC 2단계 순서 복구

- 그룹별 `KinematicCharacterSystem.Simulate(emptyMovers)` 연속 호출을 제거
- 모든 Motor `UpdatePhase1` 완료 barrier 복원
- 그 뒤 모든 PhysicsMover Transient pose 확정
- 마지막으로 모든 Motor `UpdatePhase2`와 Transform 확정
- Interpolation 전/후 호출 순서도 KCC 원본과 대조
- Motor별 `scaledDt`를 유지하는 임시안과 모든 Motor `baseDt` + actor-local 합성안을 비교
- 플랫폼 carry는 `baseDt`, 자발 이동은 local time domain이라는 권장 정책의 구현 가능성 실험
- vendor KCC 원본 수정 없이 어댑터에서 해결 가능한지 우선 검토

이 단계는 Root Motion/Impulse 의미 변경과 분리한다. 먼저 KCC 자체의 Phase barrier를
복구한 뒤 시간축 분리 방식을 결정한다.

### Phase 2 — Root Motion 브리지

- `ActorAnimator` pending accumulator 도입
- KCC step snapshot·단일 소비
- 상태별 `ApplyRootMotion` 토글 제거
- Position XZ/Y/Rotation 정책 분리
- 기존 Root Motion 소비 상태 전체 전환
- Motion Warp 캐시·playback scale 회귀 검증

### Phase 3 — 외력 재설계

- `_internalVelocityAdd`를 `QueueVelocityChange` 의미로 명확화
- `_impulseVelocity` 차감/재합산 제거
- KnockBack 방향성 감쇠 modifier 도입
- Launch의 수직 탄도 분리
- Pull을 순간 속도 변화로 유지할지 짧은 목표 속도/가속으로 바꿀지 콘텐츠별 명시
- `AddForceEvent` 호환·마이그레이션 정책 적용

### Phase 4 — 전 상태 중력·전체 대입 감사

- `GravityOwnership` 도입
- 수동 Gravity와 공통 Gravity 중복 제거
- 모든 `currentVelocity =`를 축 소유권 관점으로 분류
- Full Override 허용 상태를 whitelist로 제한
- Player/Enemy/Flying 상태별 회귀 검증

### Phase 5 — KCC 설정·충돌 필터

- Ignore Collider 실제 연결
- Ledge snap 실험 후 플레이어 프리팹 값 확정
- 자체 Raycast 우회 축소/제거 가능성 판단
- Rigidbody interaction은 별도 기능 요구가 있을 때만 변경
- 프리팹 자동 재직렬화 diff 검사

---

## 8. 테스트 계획

### 8.1 EditMode 순수 로직 테스트

`Assets/Tests/EditMode/Movement/`와 전용 테스트 asmdef를 추가한다.

| ID | 테스트 | 수락 기준 |
|---|---|---|
| M01 | Ballistic 보존 | JumpAttack 진입 전후 XZ와 상승 Y가 정책 허용 범위에서 연속 |
| M02 | ApexHang 상한 | 체공이 `maximumHangTime`을 초과하지 않음 |
| M03 | Dive 단조성 | Commit 후 하강 속도가 목표 방향으로 단조 접근 |
| M04 | 중력 1회 | Controller ownership에서 한 step의 중력 delta가 정확히 1회 |
| M05 | State gravity | State ownership에서 공통 중력 delta가 0 |
| M06 | Impulse 최초값 | 첫 step에 요청한 deltaVelocity가 감쇠 전 정확히 적용 |
| M07 | 벽 충돌 후 감쇠 | 권위 속도 0에서 modifier가 역방향 속도를 만들지 않음 |
| M08 | 다중 Drag | 두 modifier가 각자 Drag를 유지 |
| M09 | Launch 탄도 | 수직 Launch가 별도 지수 Drag 없이 중력만 받음 |
| M10 | Root buffer 2:1 | 렌더 2회/물리 1회 델타가 합산되어 1회 소비 |
| M11 | Root buffer 1:2 | 렌더 1회/물리 2회에서 두 번째 소비가 zero/identity |
| M12 | deltaTime 0 | NaN/Infinity 없이 zero 또는 보존 정책 반환 |
| M13 | 축 정책 | 공중 공격에서 Root Y가 물리 Y를 덮지 않음 |
| M14 | Ignore Collider | 등록 collider만 false, 제거 후 true |
| M15 | Root 상태 전환 flush | 비소비 pending이 다음 Root 상태에서 폭발하지 않음 |
| M16 | Root 모델 교체 flush | Animancer/모델 교체 후 stale delta 0 |
| M17 | LocalTimeScale 범위 | 0/음수 입력이 명시 정책대로 clamp/reject되고 역시간 시뮬레이션 없음 |
| M18 | Impulse 호출 순서 | 서로 다른 Drag의 등록 순서를 바꿔도 각 modifier 결과 동일 |

### 8.2 Play Mode 수직 슬라이스

| ID | 시나리오 | 수락 기준 |
|---|---|---|
| P01 | 상승 중 JumpAttack | 진입 프레임 Y 부호가 강제로 음수로 바뀌지 않음 |
| P02 | 정점 JumpAttack | 설정 범위 내 짧은 체공 후 반드시 하강 |
| P03 | 하강 중 JumpAttack | 프로필에 리프트가 없으면 순간 상승하지 않음 |
| P04 | JumpDashAttack | 수직 속도 연속, 중력 1회 |
| P05 | 공중 모션 종료 | 비접지면 Airborne, 접지면 Ground 상태 |
| P06 | 벽 KnockBack | 접촉 후 반대 방향 가짜 반동 없음 |
| P07 | 경사 KnockBack | NaN/진동/벽 타고 오름 없음 |
| P08 | Root 공격 30/144fps | 총 이동거리 편차 3% 이하 |
| P09 | 히트스톱 Root 공격 | 이동량 재사용·폭증 없음 |
| P10 | 이동 플랫폼 히트스톱 | 아래 플랫폼 매트릭스의 상대 pose·접지·이탈 관성 기준 충족 |
| P11 | Motion Warp | 기존 도달 오차 기준 악화 없음 |
| P12 | 클립 교체 | 같은 공중 프로필이면 물리 궤적이 허용 오차 내 동일 |
| P13 | 공중 모션 완료 | 비접지 완료는 Airborne, 착지와 동시 완료는 전이 1회 |
| P14 | Impulse 상태 매트릭스 | Idle/GroundMove/Airborne/Attack/Hit에서 충돌 후 역반동 없음 |
| P15 | 코너 연속 충돌 | 연속 hit normal에서 진동·증폭·NaN 없음 |
| P16 | 반대 Impulse 동시 입력 | 합성·상쇄가 호출 순서에 의존하지 않음 |
| P17 | Enemy Launch 진입 모션 | AddImpulse 적용 전 stale Motor.Velocity 때문에 Fall을 잘못 선택하지 않음 |
| P18 | culling/모델 교체 Root 상태 | 복귀 첫 step에 stale delta 이동 없음 |

Root Motion 거리 `3%`는 초기 수락 임계값이다. Phase 0에서 현재 기준선과 측정 노이즈를
확인한 뒤 최종값을 확정한다.

이동 플랫폼 매트릭스:

```text
플랫폼: Horizontal / Vertical / Rotating
LocalTimeScale: 1.0 / 0.1 / 0.001
계측:
  a. platform-local position/rotation 오차
  b. stable grounding 유지율
  c. 이탈 순간 PreserveAttachedRigidbodyMomentum
```

상대 pose 보존을 채택한다면 로컬 히트스톱 중에도 오차가 누적되지 않아야 한다.
다른 정책을 채택할 경우 허용 오차와 시각 처리 방식을 구현 전에 문서에 명시한다.

### 8.3 정적 검증

- `Animator.DeltaPosition / deltaTime` 직접 소비 0건
- 상태별 `ApplyRootMotion(true/false)` 토글 0건
- Controller 중력 상태의 직접 `controller.Gravity * deltaTime` 0건
- 승인되지 않은 `currentVelocity = Vector3...` Full Override 0건
- `AddForceEvent` 신규 생성 시 에디터 경고 또는 새 속도 변화 이벤트 사용
- 런타임 무가드 `UnityEditor` 참조 0건

---

## 9. 수락 기준

### 기능

- 점프 공격이 입력 시점의 상승·수평 관성을 정책대로 보존한다.
- 일반 공중 공격의 실제 높이는 AnimationClip Root Y에 의해 결정되지 않는다.
- 공중 공격 모션 종료 후 비접지 상태에서 GroundMove/Idle로 진입하지 않는다.
- KnockBack/Launch/Pull이 이름과 단위가 명확한 서로 다른 정책으로 동작한다.
- 벽 충돌 뒤 Impulse 분해 오차로 역방향 속도가 생기지 않는다.

### 결정성·안정성

- 30~144fps에서 Root Motion 총 이동거리 편차 3% 이하.
- 히트스톱과 로컬 타임스케일 중 NaN/Infinity 0.
- 동일 KCC step에서 중력 적분 최대 1회.
- Root Motion 델타는 생성된 횟수만큼 합산되고 KCC step당 최대 1회 소비.
- 이동 플랫폼, 경사, 계단, 얇은 발판, 벽/코너 회귀 0.

### 프로젝트 완료 조건

- Unity 컴파일 오류 0
- Actor asmdef CLI 보조 컴파일 오류 0
- 신규 EditMode 테스트 전체 통과
- Play Mode 수직 슬라이스 전체 통과
- Player Build 오류 0
- Missing Script 0
- MotionSet/Ability managed reference 및 VFX 누락 0
- 검증 중 변경된 `Assets/10.Datas/`, `Assets/03.Prefabs/` diff 전수 확인

---

## 10. 의사결정 기록

| 항목 | 결정 | 이유 |
|---|---|---|
| KCC 교체 | 하지 않음 | 프로젝트에 깊게 통합됐고 3.4.4 내장 기능으로 해결 가능 |
| JumpAttack `-15f` 조정 | 폐기 | 전체 속도 대입 자체가 원인 |
| 공중 공격 Y Root Motion | 기본 미사용 | KCC 탄도와 충돌 권위를 유지 |
| Root Motion 수집 | 누적 후 KCC step 단일 소비 | Update/FixedUpdate 경계 안정화 |
| 상태별 applyRootMotion 토글 | 제거 | 재초기화와 소비 계약 불일치 방지 |
| Impulse 영구 분리 채널 | 폐기 | KCC 충돌 해결 속도와 역산 불가능 |
| KnockBack 감쇠 | 권위 속도의 방향 성분 modifier | 벽 충돌 후 역방향 오차 방지 |
| Launch 감쇠 | 중력 중심 | 수직 탄도와 수평 넉백 분리 |
| 공중 프로필 원본 | AbilityAttackInfo 경로 | 플레이어/몬스터 공격 데이터 단일 원본 유지 |
| Rigidbody interaction | 현행 유지 | 전투 이동 안정성 우선, 별도 기능 요구 시 검증 |

---

## 11. 구현 시 주의 사항

1. `AddForceEvent`는 `[SerializeReference]` 타입이다. 이름 변경·이동 시
   `[MovedFrom(true, sourceAssembly: "...")]`와 기존 데이터 검증 없이 삭제하지 않는다.
2. Root Motion 브리지 전환 중 구/신 소비 경로를 동시에 켜지 않는다. 이중 이동이 발생한다.
3. `MotionWarpController`의 root path 캐시는 입력 델타 정의가 바뀌므로 기존 베이크·런타임
   측정값 정합을 다시 검증한다.
4. `ApplyRootMotion` 토글 제거 전 모든 Animator 프리팹의 초기 설정과
   `OnAnimatorMove` 호출 여부를 계측한다.
5. 공중 정책 시간은 `UpdateState`의 렌더 delta가 아니라 KCC가 전달한 물리 `deltaTime`으로
   누적한다.
6. Ledge 값 변경과 자체 Raycast 제거를 같은 커밋에서 하지 않는다. 어느 변경이 접지 회귀를
   만들었는지 분리할 수 있어야 한다.
7. 구현 완료 후 본 문서는 `Assets/docs/Complete/`로 이동하고 실제 API 기준 가이드로 갱신한다.
