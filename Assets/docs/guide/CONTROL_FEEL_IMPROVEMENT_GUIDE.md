# 조작감 개선 종합 가이드

> 작성일: 2026-06-13
> 대상 환경: Unity 6 (6000.0.60f1), KCC, Animancer MotionSet, Unity Input System
> 상태: **개선 제안 / 실행 계획**

---

## 0. 핵심 결론

현재 프로젝트는 조작감 개선에 필요한 핵심 구조가 이미 상당 부분 구현되어 있다.

| 영역 | 현재 구현 |
|------|-----------|
| 입력 | `InputManager` 레이어 라우팅, `InputBuffer`, 차지 입력 started/performed/canceled 분리 |
| 이동 | KCC 기반 `ActorMovementController`, 상태별 `UpdateVelocity` / `UpdateRotation` 위임 |
| 공격 | `PlayerAttackState`, MotionSet 기반 판정, 콤보 윈도우, 캔슬 마스크, 모션 워프 |
| 방어 | Dodge / Dash 무적, 퍼펙트 도지, 가드, 카운터 창 |
| 카메라 | 하드락, 소프트 타겟 어시스트, 전투 카메라 intent, 거리/FOV 보정 |
| 타격 피드백 | 로컬 HitStop, 짧은 전역 펄스, 카메라 흔들림, FX/SFX 경로 |

따라서 다음 단계는 새 시스템을 크게 추가하기보다 **플레이어 의도를 더 잘 받아주는 관용 구간**, **상태 전환 실패 시 입력 유실 방지**, **공격/회피/카메라 보정의 데이터화**, **런타임 계측**을 보강하는 쪽이 효율적이다.

권장 우선순위:

| 우선순위 | 작업 | 기대 효과 | 리스크 |
|----------|------|-----------|--------|
| P0 | 대시/점프/회피 실패 입력의 재버퍼 또는 소비 지연 | "눌렀는데 안 나감" 감소 | 낮음 |
| P0 | 액션별 버퍼 시간 분리 | 공격은 관대하게, 회피는 민감하게 튜닝 | 낮음 |
| P1 | 공격 후딜 이동 캔슬 타이밍 데이터화 | 캐릭터/공격별 커밋감 조절 | 중간 |
| P1 | 런타임 조작감 디버그 HUD | 원인 추적 속도 증가 | 낮음 |
| P2 | 비락온 공격 소프트 타겟 보정 강화 | 허공 공격 감소, 근접전 유도감 상승 | 중간 |
| P2 | 퍼펙트 도지 보상 창 2단계화 | 쉬운 판정과 강한 보상 분리 | 중간 |
| P3 | 전투 카메라 수동 입력 감쇠/복귀 정책 | 자동 카메라와 플레이어 조작 충돌 감소 | 중간 |

---

## 1. 외부 조사 요약

웹 조사 결과를 현재 프로젝트 구조에 맞게 재해석하면 조작감은 다음 3축으로 나누는 것이 적합하다.

| 축 | 의미 | 프로젝트 대응 |
|----|------|---------------|
| Physicality / Tuning | 이동, 가속, 회전, 중력, 관성의 예측 가능성 | `ActorMovementController`, `PlayerGroundMoveState`, `PlayerAirborneState` |
| Support / Streamlining | 플레이어 의도를 시스템이 받아주는 보정 | `InputBuffer`, 코요테 타임, 모션 워프, 소프트 타겟 |
| Amplification / Juicing | 성공/실패를 즉시 읽게 하는 피드백 | HitStop, 카메라, SFX, FX, UI |

`Designing Game Feel. A Survey`는 게임 필을 `physicality`, `amplification`, `support` 세 영역으로 나누고, 각각을 튜닝/주스/스트림라이닝으로 다룬다. 이 분류는 UPlayground의 입력-상태-카메라-피드백 구조와 잘 맞는다.

액션 게임 타격감 연구인 `What Features Influence Impact Feel?`는 히트스톱, 사운드 일관성, 카메라 제어가 타격감에 강하게 작용한다고 정리한다. 현재 프로젝트는 `TIME_HITSTOP_GUIDE.md` 기준으로 로컬 HitStop과 카메라 피드백 경로를 이미 갖추고 있으므로, 앞으로는 "더 세게"보다 "상황별로 정확하게"가 중요하다.

Unity Input System 공식 문서는 `started`, `performed`, `canceled` 단계와 `Hold`, `Tap`, `SlowTap` 같은 Interaction을 구분한다. 현재 `PlayerActor`의 강공격/차지 입력 처리도 이 구조를 수동으로 구현하고 있으므로, 추후 InputActionAsset의 Interaction 설정과 코드 정책을 맞추면 입력 의미가 더 명확해진다.

참고 링크:

| 자료 | 활용 포인트 |
|------|-------------|
| https://arxiv.org/abs/2011.09201 | 게임 필 분류: physicality / amplification / support |
| https://arxiv.org/abs/2208.06155 | 액션 게임 타격감 요소: hit stop, sound coherence, camera control |
| https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/manual/Interactions.html | InputAction Interaction, Hold/Tap/SlowTap, timeout |
| https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/manual/Actions.html | InputAction / ActionMap / ActionAsset 구조 |

---

## 2. 현재 조작감 구조

```
Unity Input System
    │
    ▼
InputManager
├── InputLayer 검사
├── performed 입력 자동 InputBuffer 적재
└── PlayerActor 입력 콜백
        │
        ▼
PlayerActor.Update()
└── PlayerCharacterInputs 구성
        │
        ▼
PlayerMovementController.SetInputs()
├── 카메라 기준 MoveInputVector 계산
├── LookInputVector 캐시
└── 상태별 HasXInput() 제공
        │
        ▼
ActorMovementController
├── CurrentState.UpdateState()
├── CurrentState.UpdateVelocity()
└── CurrentState.UpdateRotation()
        │
        ▼
PlayerActorState 계열
├── Idle / GroundMove / Airborne
├── Attack / Charge / DashAttack / JumpAttack
├── Dodge / Dash / Guard
└── Hit / Stun / Knockdown / Death
```

### 관련 파일 구조

```
Assets/02.Scripts/
├── Input/
│   └── InputBuffer.cs
├── Manager/Input/
│   ├── InputManager.cs
│   ├── InputManager.Action.cs
│   └── InputManager.Event.cs
├── GameActor/MovementController/
│   ├── ActorMovementController.cs
│   └── PlayerMovementController.cs
├── GameActor/State/Player/
│   ├── PlayerIdleState.cs
│   ├── PlayerGroundMoveState.cs
│   ├── PlayerAttackState.cs
│   ├── PlayerInterruptResolver.cs
│   ├── PlayerDodgeState.cs
│   └── PlayerDashState.cs
└── GameActor/Component/Player/
    └── PlayerCombat.cs
```

---

## 3. 개선안 P0 - 입력 유실 방지

### 3.1 액션별 버퍼 시간 분리

현재 `InputBuffer` 기본 시간은 0.15초이고, 짧은 강공격은 `PlayerActor.OnHeavyAttackCanceled()`에서 0.24초로 직접 재추가한다. 이 방식은 동작하지만, 액션별 의도를 명시하기 어렵다.

권장 정책:

| 액션 | 권장 버퍼 | 이유 |
|------|-----------|------|
| `Attack` | 0.20-0.26s | 콤보/캔슬 선입력을 관대하게 받음 |
| `HeavyAttack` 짧은 입력 | 0.22-0.28s | 탭/차지 분리 후에도 강공격 의도 보존 |
| `Dodge` | 0.12-0.18s | 너무 길면 원치 않는 늦은 회피 발생 |
| `Dash` | 0.10-0.16s | 쿨타임/상태 조건 실패 후 짧은 재시도 |
| `Jump` | 0.10-0.15s | 점프 버퍼와 코요테 타임의 합이 과해지지 않게 제한 |
| `PlayerSwap` | 0.12-0.18s | 전투 중 반응성은 확보하되 오발 방지 |
| `Skill` | 0.18-0.24s | 게이지/캔슬창 대기 입력 보존 |

구현 방향:

```csharp
public static class PlayerInputBufferDurations
{
    public const float Attack = 0.24f;
    public const float HeavyAttack = 0.24f;
    public const float Dodge = 0.15f;
    public const float Dash = 0.12f;
    public const float Jump = 0.12f;
    public const float Skill = 0.20f;
}
```

`InputManager.Event.cs`의 자동 버퍼 적재 switch에서 액션별 duration을 넘기는 방식이 가장 작다.

### 3.2 조건부 전환 실패 시 입력 소비 지연

`PlayerInterruptResolver.TryInterrupt()`는 `Dash` 입력을 먼저 소비한 뒤 `TryTransitionToState(new PlayerDashState(...))`를 호출한다. 대시 쿨타임 등으로 전환이 실패하면 입력은 이미 사라진다.

현재 의미:

```csharp
if ((mask & PlayerInterruptAction.Dash) != 0 &&
    buffer.ConsumeInput(PlayerAction.Dash) != null)
{
    return controller.TryTransitionToState(new PlayerDashState(controller));
}
```

권장 변경:

| 방식 | 설명 |
|------|------|
| 소비 전 사전 검사 | `controller.IsDashReady` 같은 조건을 먼저 확인 |
| 실패 시 재버퍼 | 전환 실패 시 `AddInput(Dash, bufferTime: 0.08f)` |
| 실패 피드백 | 쿨타임 UI 또는 짧은 SFX로 "입력은 받았지만 불가"를 표시 |

가장 보수적인 1차안:

```csharp
if ((mask & PlayerInterruptAction.Dash) != 0 && buffer.HasInput(PlayerAction.Dash))
{
    if (controller.TryTransitionToState(new PlayerDashState(controller)))
    {
        buffer.ConsumeInput(PlayerAction.Dash);
        return true;
    }
}
```

주의: 현재 `PlayerDashState.CanTransitionState()`가 새 상태 내부에서 `playerController.IsDashReady`를 확인한다. 사전 검사로 중복해도 무해하지만, 최종 권한은 상태 가드가 가진다.

### 3.3 입력 버퍼 만료 정지 범위 확대 검토

`PlayerAttackState`는 액티브 히트 중 `InputBuffer.SetExpiryPaused(_combat.IsPossibleCollide)`를 호출해 캔슬 불가 구간에서 선입력이 만료되지 않도록 한다. 이 패턴은 좋다.

확대 후보:

| 상태 | 적용 필요성 |
|------|-------------|
| `PlayerChargeState` | 차지 단계/히트 중 캔슬 입력 유실 가능성 확인 |
| `PlayerDashAttackState` | 대시 공격 후 캔슬/콤보 입력 유실 가능성 확인 |
| `PlayerJumpAttackState` | 공중 콤보 입력 타이밍 보존 |

단, 모든 상태에 무조건 적용하면 과버퍼가 생길 수 있으므로, 먼저 디버그 HUD로 입력 유실을 확인한 뒤 적용한다.

---

## 4. 개선안 P1 - 이동 반응성

### 4.1 초기 가속과 방향 전환 보정

현재 지상 이동은 `StableMovementSharpness`로 현재 속도를 목표 속도에 보간한다. 일정한 Sharpness는 안정적이지만, 입력 시작 첫 프레임의 감각이 둔할 수 있다.

권장:

| 상황 | 보정 |
|------|------|
| 정지 -> 이동 시작 | 첫 0.08-0.12초 동안 가속 sharpness 증가 |
| 90도 이상 방향 전환 | 회전 sharpness 일시 증가 |
| Sprint 진입 | FOV/카메라가 따라가기 전에 이동 속도만 급증하지 않게 짧은 램프 |
| 이동 입력 해제 | Stop 모션이 없을 때 감속 sharpness 별도값 사용 |

필드 후보:

```csharp
[Header("Move Feel")]
[SerializeField] private float _moveStartBoostDuration = 0.1f;
[SerializeField] private float _moveStartSharpnessMultiplier = 1.4f;
[SerializeField] private float _turnAroundSharpnessMultiplier = 1.6f;
[SerializeField] private float _stopSharpnessMultiplier = 1.2f;
```

### 4.2 SprintAutoStartDelay 전투/비전투 분리

`PlayerMovementController.SprintAutoStartDelay` 기본값은 3초다. 탐험에서는 괜찮지만 전투 중 회피/추격 조작에는 길게 느껴질 수 있다.

권장:

| 상태 | 권장값 |
|------|--------|
| 탐험 | 2.0-3.0s |
| 전투 | 0.8-1.2s |
| 락온 중 | 자동 Sprint 비활성 또는 별도 Strafe 모드 |

전투 판정은 `PlayerCombatStateTracker` 또는 주변 위협 탐지 결과를 사용한다.

---

## 5. 개선안 P1 - 공격 조작감

### 5.1 후딜 이동 캔슬 타이밍 데이터화

현재 이동 후딜 캔슬은 다음 조건으로 발동한다.

```
Move 플래그
!CanCombo
_hasActiveHitFired
!IsPossibleCollide
CurrentHitPhaseIndex >= LastHitPhaseIndex
HasMoveInput
```

이 구조는 안전하다. 다만 공격별 체감 조절은 MotionSet의 Collision/ComboWindow 배치에 의존한다. 데이터에서 "후딜 이동 캔슬을 언제부터 허용할지"를 직접 조절할 수 있으면 튜닝 속도가 빨라진다.

필드 후보:

| 필드 | 의미 |
|------|------|
| `moveCancelDelayAfterLastHit` | 마지막 히트 종료 후 N초 뒤 이동 캔슬 허용 |
| `moveCancelNormalizedTime` | 모션 정규화 시간 기준 허용 |
| `moveCancelRequiresComboClosed` | 현재처럼 콤보 윈도우 닫힘을 요구할지 |
| `moveCancelSpeedScale` | 캔슬 직후 첫 이동 속도 보정 |

권장 기본값:

| 공격 | 기본 정책 |
|------|-----------|
| 약공 1-2타 | 빠른 이동 캔슬 허용 |
| 약공 막타 | 약간 늦게 허용 |
| 강공 | 커밋감 보존, 늦게 허용 |
| 스킬 | 스킬별 수동 지정 |
| 대시/점프 공격 | 기본 비허용 또는 별도 데이터 |

### 5.2 캔슬 실패 이유 로깅

캔슬 입력이 들어왔는데 실패하는 이유는 여러 가지다.

| 실패 이유 | 예 |
|-----------|----|
| 마스크 없음 | 공격 데이터에 `Dodge`가 없음 |
| 캔슬 윈도우 닫힘 | 액티브 히트 중 |
| 상태 가드 실패 | Dash 쿨타임 |
| 리소스 부족 | Skill 게이지 부족 |
| 모션 없음 | 해당 `AnimKey` 미등록 |

디버그 빌드에서만 `PlayerInterruptResolver`가 마지막 실패 이유를 보관하면 튜닝이 쉬워진다.

```csharp
public enum PlayerInterruptFailReason
{
    None,
    ActionNotAllowedByMask,
    NoBufferedInput,
    CancelWindowClosed,
    StateGuardRejected,
    ResourceNotEnough,
    MotionMissing,
}
```

---

## 6. 개선안 P2 - 회피 / 대시 / 퍼펙트 도지

### 6.1 퍼펙트 도지 보상 창 2단계화

현재 `PlayerCombat._perfectDodgeWindow`는 0.25초다. 조작 친화적으로는 좋지만, 보상을 모두 동일하게 주면 성공 가치가 낮아질 수 있다.

권장:

| 구간 | 효과 |
|------|------|
| 0.00-0.12s | 퍼펙트 도지 + 카운터 창 + 강한 카메라/타임스케일 |
| 0.12-0.25s | 회피 성공 + 약한 피드백, 카운터 창 짧게 또는 없음 |

구현 방식:

```csharp
public float PerfectDodgeElapsed => Time.time - _perfectDodgeWindowStart;
public bool IsPerfectDodgeStrongWindow => PerfectDodgeElapsed <= _perfectDodgeStrongWindow;
```

이렇게 하면 판정 관용은 유지하면서 숙련 보상을 분리할 수 있다.

### 6.2 Dodge / Dash 역할 분리

현재 둘 다 무적을 제공한다.

| 동작 | 권장 역할 |
|------|-----------|
| Dodge | 근거리 회피, 퍼펙트 도지, 카운터 기회 |
| Dash | 거리 조절, 추격/이탈, 회피 피드백은 있지만 카운터 없음 |

이미 `PlayerDashState`는 대시 회피 피드백만 발동하고 카운터 창은 열지 않는 구조다. 앞으로도 이 역할 분리를 유지한다.

추가 제안:

| 항목 | Dodge | Dash |
|------|-------|------|
| 입력 버퍼 | 짧음 | 더 짧음 |
| 무적 시작 | 즉시 | 즉시 또는 약간 짧게 |
| 충돌 무시 | Enemy 근거리 | Enemy 근거리 |
| 카메라 | 회피 성공 시 강조 | 속도/FOV 중심 |
| 쿨타임 | 짧거나 없음 | 명확한 쿨타임 |

---

## 7. 개선안 P2 - 타겟 보정 / 카메라

### 7.1 비락온 공격 소프트 타겟 보정

`PlayerAttackState.FindHomingTarget()`는 락온 타겟을 우선하고, 비락온 상태에서는 전방 범위 대상 탐색을 사용한다. 여기에 "공격 시작 회전 보정"을 더하면 허공 공격이 줄어든다.

권장:

| 조건 | 정책 |
|------|------|
| 비락온 | 카메라 중앙/캐릭터 전방 기준 후보 탐색 |
| 약공 첫 타 | 보정 강함 |
| 콤보 중 | 보정 약함, 기존 방향 존중 |
| 강공/스킬 | 공격 데이터별 보정 |
| 수동 이동 입력이 강함 | 입력 방향 우선 |

필드 후보:

```csharp
[SerializeField] private float _freeAttackAssistAngle = 45f;
[SerializeField] private float _freeAttackAssistRange = 4f;
[SerializeField] private float _freeAttackRotationDuration = 0.1f;
[SerializeField] private AnimationCurve _freeAttackRotationCurve;
```

### 7.2 카메라 자동 보정과 수동 입력 감쇠 분리

락온 중 수동 Look 입력을 완전히 무시하면 안정적이지만 답답할 수 있다. 반대로 완전히 허용하면 자동 락온과 싸운다.

권장:

| 상황 | 자동 보정 |
|------|-----------|
| 최근 0.0-0.3초 수동 Look 입력 | 자동 보정 약화 |
| 수동 입력 종료 후 0.3-0.6초 | 자동 보정 점진 복귀 |
| 적 공격 텔레그래프/카운터 | 자동 보정 일시 강화 |
| 스냅샷/궁극기 | 수동 입력 잠금 |

`CameraLockOn`, `InGameCameraMode`, `CombatCameraDirector`의 책임을 나눠야 한다.

---

## 8. 개선안 P1 - 조작감 디버그 HUD

체감 문제는 로그만으로 추적하기 어렵다. Play Mode 전용 오버레이를 먼저 만드는 것이 좋다.

표시 항목:

| 그룹 | 항목 |
|------|------|
| 상태 | `CurrentState.StateName`, `MoveAnimType`, grounded 여부 |
| 입력 | 버퍼 목록, 각 입력 남은 시간, hold 입력 상태 |
| 캔슬 | `IsCancelWindowOpen`, `IsPossibleCollide`, `CanCombo`, `interruptActions` |
| 이동 | 현재 속도, 목표 속도, `MoveInputVector`, `LookInputVector` |
| 회피/대시 | Dash 쿨타임, 퍼펙트 도지 남은 시간, DodgeCounter 남은 시간 |
| 타겟 | LockOnTarget, HomingTarget, MotionWarp applicable 여부 |
| 피드백 | 최근 HitStop duration/scale, 카메라 intent |

초기 구현은 `OnGUI` 기반 Editor/Development Build 전용으로 충분하다.

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
public sealed class PlayerControlFeelDebugHUD : MonoBehaviour
{
    private void OnGUI()
    {
        // PlayerActor / PlayerMovementController / PlayerCombat 상태를 읽어 표시
    }
}
#endif
```

이 HUD가 있으면 "입력이 안 먹음"을 다음 중 하나로 즉시 분류할 수 있다.

| 증상 | 확인 지점 |
|------|-----------|
| 버퍼에 입력이 없음 | InputManager / InputLayer / InputAction 설정 |
| 버퍼는 있는데 전환 안 됨 | 캔슬 마스크 / 상태 가드 / 리소스 |
| 전환은 됐는데 체감이 둔함 | 애니메이션 crossfade / root motion / velocity |
| 맞췄는데 약함 | HitStop / SFX / Camera intent / FX |

---

## 9. 실행 순서

### 1차 - 저위험 코드 개선

| 순서 | 작업 | 파일 |
|------|------|------|
| 1 | 액션별 버퍼 시간 테이블 추가 | `InputManager.Event.cs`, `InputBuffer.cs` |
| 2 | Dash 전환 실패 시 입력 소비 지연 또는 재버퍼 | `PlayerInterruptResolver.cs` |
| 3 | 캔슬 실패 이유 디버그 필드 추가 | `PlayerInterruptResolver.cs` |
| 4 | PlayerControlFeelDebugHUD 추가 | 신규 Editor/Debug 스크립트 |

### 2차 - 데이터 튜닝 기반 확장

| 순서 | 작업 | 파일/데이터 |
|------|------|-------------|
| 1 | 이동 후딜 캔슬 지연 필드 추가 | `AttackData`, `PlayerAttackInfo` |
| 2 | 공격별 소프트 타겟 보정 필드 추가 | `PlayerAttackInfo` 또는 별도 Feel Profile |
| 3 | 퍼펙트 도지 강/약 보상 창 분리 | `PlayerCombat`, `PlayerActor` |
| 4 | SprintAutoStartDelay 전투/비전투 분리 | `PlayerMovementController` |

### 3차 - 카메라/피드백 정밀화

| 순서 | 작업 | 파일 |
|------|------|------|
| 1 | 락온 중 최근 수동 입력 감쇠 | `InGameCameraMode`, `CameraLockOn` |
| 2 | 비락온 공격 카메라 소프트 어시스트 강화 | `CombatCameraDirector`, `PlayerAttackState` |
| 3 | HitStop/SFX/Camera intent 프리셋 동기화 | `CombatFeedbackProfile`, `HitStopHandler` |

---

## 10. 튜닝 체크리스트

### 기본 이동

| 테스트 | 기대 |
|--------|------|
| 정지 상태에서 스틱 입력 | 1-2프레임 안에 캐릭터 의도가 보임 |
| 180도 방향 전환 | 회전은 빠르지만 발 미끄러짐이 과하지 않음 |
| 입력 해제 | Stop 모션 또는 감속이 의도적으로 느껴짐 |
| 전투 중 추격 | Sprint 진입이 너무 늦지 않음 |

### 공격

| 테스트 | 기대 |
|--------|------|
| 약공 연타 | 콤보가 안정적으로 이어짐 |
| 공격 중 회피 선입력 | 액티브 히트 이후 허용 구간에서 유실 없이 발동 |
| 이동 입력 유지 공격 | 윈드업/멀티히트는 보존, 마지막 후딜에서만 이동 캔슬 |
| 모션 없는 공격 | 상태가 끊기지 않고 기존 상태 유지 |

### 회피/대시

| 테스트 | 기대 |
|--------|------|
| 적 공격 직전 Dodge | 퍼펙트 도지 창에서 명확한 피드백 |
| Dash 쿨타임 중 Dash 입력 | 입력 유실감 대신 쿨타임 피드백 |
| 적과 겹친 Dodge 종료 | ComputePenetration으로 자연스럽게 분리 |

### 카메라/타겟

| 테스트 | 기대 |
|--------|------|
| 비락온 근접 공격 | 가까운 정면 적을 자연스럽게 향함 |
| 락온 중 수동 Look | 자동 보정과 싸우지 않음 |
| 대형 몬스터 | 플레이어와 핵심 부위가 모두 보임 |

---

## 11. 관련 문서

| 문서 | 관계 |
|------|------|
| `Assets/docs/Complete/INPUT_SYSTEM_GUIDE.md` | 입력 라우팅, InputBuffer, InputLayer |
| `Assets/docs/guide/ATTACK_CANCEL_SYSTEM_GUIDE.md` | 공격 캔슬, 이동 후딜 캔슬, 선입력 만료 정지 |
| `Assets/docs/guide/CAMERA_LOCK_ON_SYSTEM_GUIDE.md` | 락온, 소프트락/하드락, 타겟 선정 |
| `Assets/docs/Complete/TIME_HITSTOP_GUIDE.md` | HitStop, LocalTimeScale, 타격 피드백 |
| `Assets/docs/Complete/COMBAT_CAMERA_SYSTEM_IMPROVEMENT_PLAN.md` | 전투 카메라 구조 개선 |
