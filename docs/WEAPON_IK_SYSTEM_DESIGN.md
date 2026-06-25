# Weapon IK 시스템 설계서

> 대상: UPlayGround (Unity 6 / URP / Animancer Pro V8 / KCC / MagicaCloth2)
> 범위: 플레이어 무기 IK — **보조손 그립 정합** + **무기 끝 겨냥 보정(MotionEvent 구동)**
> 상태: 설계 확정, 구현 대기

---

## 1. 개요 & 범위

무기 IK는 성격이 다른 두 레이어로 구성된다.

| 레이어 | 성격 | 용도 | 단계 |
|---|---|---|---|
| **보조손 그립** | 따라가는(follow) IK | 양손무기(그레이트소드/창/스태프/활)에서 왼손을 무기 그립 포인트에 밀착 | Phase 1 |
| **무기 끝 겨냥** | 구동(driving) IK | 무기 forward 축(창 찌르기/활·스태프 발사 축)을 타깃에게 정렬. **MotionEvent로 구간 제어** | Phase 3 |

두 레이어는 **하나의 `WeaponIKController` 컴포넌트**, **하나의 `OnAnimatorIK` 패스**(FootIK와 동일, §3) 안에서 `겨냥(앞) → 그립(뒤)` 순서로 묶인다. 겨냥이 주손/무기를 먼저 재정렬하고, 그립이 그 결과를 따라가야 보조손이 어긋나지 않는다.

### 비목표
- 풀바디 IK / 등반 / 커버 시스템 — 본 설계 범위 밖.
- 적(Monster) 무기 IK — 구조는 공유 가능하나 본 문서는 플레이어 기준. 후속 확장.

---

## 2. 기존 인프라 재사용 (신규 작성 최소화)

| 재사용 대상 | 위치 | 용도 |
|---|---|---|
| `FootIKController` 패턴 | `Component/Player/FootIKController.cs` | 컴포넌트 구조 · `Refresh(Animator)` 모델교체 · `ForceDisabled` 비대칭 페이드 · 글로벌 weight 보간 — **그대로 미러링** |
| `TwoBoneIk.SolveTwoBoneIK` | `KINEMATION/MotionWarping/Runtime/Core/TwoBoneIk.cs` | 팔 2본 IK 솔버 (root=UpperArm, mid=LowerArm, tip=Hand, hint=elbow) |
| `TwoBoneIk.FromToRotation` | 동일 파일 | 겨냥 델타 쿼터니언 계산 |
| `_currentMainWeaponObj` / `_currentSubWeaponObj` | `PlayerEquipment.cs:53-54` | 런타임 무기 인스턴스 참조원 |
| `WeaponDefinitionSO` | `Data/Item/WeaponDefinitionSO.cs` | 무기별 IK 정책 필드 추가처 |
| `HybridResolver` / `IWarpTargetResolver` | `MovementController/IWarpTargetResolver.cs` | 겨냥 타깃 결정 (락온 우선 → 콘 최근접 폴백) |
| `CameraManager.GetLockOnTarget()` | `Manager/CameraManager.cs` | 락온 타깃 소스 |
| `MotionEventBase` (`Execute`/`OnCompleteEvent`) | `Data/Event/Animation/MotionEvent.cs` | 겨냥 구간 이벤트의 베이스 |
| `MotionEvent_MotionWarp` 패턴 | `Data/Event/Animation/MotionEvent_MotionWarp.cs` | 구간 이벤트 + resolver 정책 주입의 레퍼런스 |
| Animancer `ApplyAnimatorIK=true` | `ActorAnimator.cs:261` | **OnAnimatorIK 패스 활성의 전제** — 무기 IK도 이 패스에서 돈다(§3). 이 플래그가 꺼지면 무기 IK도 발화 안 됨 |

> 모션워프(delta-warp)가 이미 캐릭터 몸통을 타깃으로 yaw 회전시킨다. 무기 겨냥은 그 **잔차 각도(pitch + 잔여 yaw)** 만 마무리하는 레이어이며, 거시 조준을 중복하지 않는다.

---

## 3. 실행 파이프라인 (핵심) — `OnAnimatorIK` 단일 패스

> **설계 검토(2026-06-24)로 경로 B(LateUpdate) 폐기, OnAnimatorIK로 확정.** 근거는 §11 참조.

초기 설계는 "ParentConstraint가 LateUpdate에서 해석되므로 무기 그립을 LateUpdate에서 읽어야 정확"하다고 보고 LateUpdate 솔브(경로 B)를 택했다. 그러나 이 프로젝트에서 LateUpdate 실행 순서는 **강제 메커니즘이 없고**(매니저 루프 + KCC FixedUpdate + MagicaCloth 커스텀 PlayerLoop 혼재), 미러 대상인 `FootIKController`도 실제로는 **`OnAnimatorIK`** 에서 돌며 이미 MagicaCloth와 공존한다(`FootIKController.cs:202`).

**핵심 우회**: 무기 그립 포인트는 ParentConstraint로 주손 본에 **강체 부착(고정 오프셋)** 이다. 따라서 콘스트레인트 해석을 기다릴 필요 없이, 현재 프레임의 손 본 포즈에서 그립 월드 좌표를 직접 역산한다.

```
gripWorld = mainHand.TransformPoint(cachedGripLocalPos)     // 부착 시 1회 캐시
gripRot   = mainHand.rotation * cachedGripLocalRot
```

→ 그립 타깃이 **현재 프레임 본 포즈** 기준으로 정확해지고, 콘스트레인트 1프레임 지연 문제가 사라진다. 전 시스템이 `OnAnimatorIK` 한 패스로 들어가 FootIK의 검증된 타이밍·MagicaCloth 공존을 그대로 상속한다.

```
[Animator/Animancer 평가]   Layer0/1 최종 포즈 산출
        │
[OnAnimatorIK]   ← FootIK가 이미 도는 그 패스. WeaponIK도 여기서 (Relay로 부착)
        │   1) 겨냥 패스 : 주손 본을 회전해 무기 forward를 타깃으로 정렬 (모션워프 yaw 이후 잔차)
        │   2) 그립 타깃 재계산 : 겨냥으로 바뀐 mainHand 포즈 기준 gripWorld 재산출
        │   3) 그립 패스 : TwoBoneIk로 보조손 2본 IK → gripWorld
        │   ※ FootIK(발)와는 다른 본 체인이라 충돌 없음
        │
[ParentConstraint 해석]     무기가 손 본 따라감 (겨냥된 손 회전 반영)
[MagicaCloth2 PlayerLoop]   손 본 콜라이더가 IK 결과 따라감 (FootIK와 동일 공존 모델)
```

- **한 패스 내 순서**(겨냥 → 그립 재계산 → 그립 솔브): 그립이 겨냥 결과 손 포즈를 입력으로 삼기 때문.
- WeaponIK는 FootIK와 **같은 `FootIKRelay` 모델**(Animator GO에 릴레이, `OnAnimatorIK` 전달)로 부착 — 별도 실행순서 attribute 불필요.
- 무기 미장착/한손무기 → 그립 weight 0, 겨냥 윈도우 비활성 → 무비용 패스스루.

---

## 4. 데이터 & 컴포넌트

### 4.1 무기 프리팹 마커

무기 프리팹에 빈 Transform 마커를 자식으로 배치한다 (디자이너가 메시 위에 시각 배치).

```csharp
// 보조손이 붙을 지점
public class WeaponGripPoint : MonoBehaviour
{
    public EquipPosition gripHand = EquipPosition.LeftHand;
    [Range(0,1)] public float defaultWeight = 1f;
}

// 무기 끝 + forward 축 (겨냥 기준)
public class WeaponTipPoint : MonoBehaviour
{
    // transform.forward 를 무기 조준 축으로 사용. 끝 위치 = transform.position.
    [Tooltip("겨냥 피벗(쥔 손)으로부터 끝까지의 기준. 보통 무기 끝에 배치.")]
    public bool useTransformForward = true;
}
```

### 4.2 `WeaponDefinitionSO` 확장

```csharp
[Header("Off-hand Grip IK")]
public bool  useOffHandGrip   = false;   // 양손무기만 true
[Range(0,1)] public float offHandWeight = 1f;
[Range(0,1)] public float elbowHintWeight = 0.5f;

[Header("Aim Assist IK")]
public bool  useAimAssist     = false;   // 창/활/스태프 등 겨냥 무기만 true
[Tooltip("이 콘(도) 밖 타깃이면 겨냥 weight 0으로 블렌드 아웃")]
public float aimConeAngle     = 35f;
[Tooltip("어시스트 최대 보정 각도 — '스냅'이 아닌 '어시스트' 체감 유지")]
public float maxCorrectionAngle = 25f;
[Tooltip("척추 additive 혼합 비율 (원거리=높게, 근접 찌르기=0)")]
[Range(0,1)] public float spineAimBlend = 0f;
```

`GreatSword/Spear/Staff/Bow` → `useOffHandGrip=true`. `Spear/Bow/Staff` → `useAimAssist=true`. `Sword/DualBlade` → 둘 다 false (무비용 패스스루).

### 4.3 `WeaponIKController` (신규, FootIKController 미러)

부착: PlayerActor 루트. 보유 책임:

```csharp
public class WeaponIKController : MonoBehaviour
{
    // ── 모델 교체 (FootIK와 동일 시그니처) ──
    public void Refresh(Animator newAnimator);   // 본 재바인딩 (LeftUpper/Lower/Hand Arm 등)

    // ── 그립 (Phase 1) — PlayerEquipment가 발도/교체 시 주입 ──
    public void SetGrip(Transform gripPoint, WeaponType type, float weight);
    public void ClearGrip();

    // ── 겨냥 (Phase 3) — MotionEvent_WeaponAim이 구간 제어 ──
    public void BeginAimWindow(in WeaponAimSettings settings);  // Execute 시
    public void EndAimWindow();                                  // OnCompleteEvent 시

    // ── 상태머신 연동 (FootIK ForceDisabled와 동일 패턴) ──
    public bool ForceDisabled { get; set; }   // 비대칭 페이드: 끄기 즉시 / 켜기 지연+페이드

    // OnAnimatorIK는 Animator GO의 FootIKRelay와 동일한 릴레이로 전달받는다.
    // (FootIKController와 동일하게 Animator가 자식 GO일 수 있으므로 Relay 패턴 공유)
    internal void ProcessWeaponIK()   // OnAnimatorIK에서 호출
    {
        // 1) 겨냥 패스 → 2) 그립 타깃 재계산 → 3) 그립 솔브  (§3 파이프라인)
    }
}
```

- `Refresh`/`ForceDisabled`/글로벌 weight 페이드 로직은 `FootIKController`에서 검증된 구현을 복제한다 (도약·공격 종료 스냅 방지 비대칭 페이드 포함).
- **타이밍은 `LateUpdate`가 아니라 `OnAnimatorIK`** — FootIK와 동일 패스(§3). 기존 `FootIKRelay`(`FootIKController.cs:395`)를 일반화하거나 `WeaponIKRelay`를 같은 모델로 추가한다.
- 그립은 Animator 휴머노이드 골(`SetIKPosition`) 대신 **`TwoBoneIk.SolveTwoBoneIK`로 본 직접 솔브** — 팔꿈치(hint) 제어와 겨냥 후 재계산을 위해. FootIK는 발 골(`SetIKPosition`)을 쓰므로 본 체인이 갈려 충돌 없음.

---

## 5. Phase 1 — 보조손 그립

### 5.1 솔브 (OnAnimatorIK, 본 직접)

```
if gripWeight ≈ 0 || gripPoint == null: return
bind once: upperArm/lowerArm/hand = animator.GetBoneTransform(보조손 Upper/Lower/Hand Arm)
cache once(부착 시): gripLocalPos/Rot = mainHand.InverseTransform(gripPoint)   // §3 강체 오프셋
// 현재 프레임 손 포즈에서 그립 월드 역산 (콘스트레인트 해석 불필요)
gripWorld = mainHand.TransformPoint(gripLocalPos)
gripRot   = mainHand.rotation * gripLocalRot
hint      = elbow pole (lowerArm 기준, elbowHintWeight 만큼)
TwoBoneIk.SolveTwoBoneIK(upperArm, lowerArm, hand, (gripWorld, gripRot), hint, gripWeight, elbowHintWeight)
```

> 주의: `TwoBoneIk.SolveTwoBoneIK`는 본 회전을 절대값으로 덮어쓰므로 매 프레임 호출해도 누적 drift 없음(`TwoBoneIk.cs:48-140`, 다음 Animator 평가가 리셋). 단 hint가 collinear면 폴백 axis가 `Vector3.up`(`TwoBoneIk.cs:96`)이라 특정 포즈에서 팔꿈치 뒤집힘 가능 → `elbowHintWeight`와 hint pole 위치로 튜닝.

### 5.2 주입

`PlayerEquipment` 발도/교체 완료 시:
1. 장착 무기 GO에서 `WeaponGripPoint`(보조손) 탐색.
2. `WeaponDefinitionSO.useOffHandGrip` && 양손무기면 `WeaponIKController.SetGrip(grip, type, offHandWeight)`, 아니면 `ClearGrip()`.

### 5.3 검증
1. 정지/이동 중 보조손이 무기 그립에 1프레임 지연 없이 밀착 (손본+캐시오프셋 역산 근거).
2. 무기 교체/모델 스왑 후 `Refresh` 재바인딩 + `gripLocal` 재캐시 정상.
3. 팔꿈치가 몸통을 관통하지 않음 (`elbowHintWeight` 튜닝).
4. MagicaCloth 손 콜라이더가 IK 결과를 따라감 (FootIK와 동일 OnAnimatorIK 공존이므로 동작 검증됨).

---

## 6. Phase 3 — 무기 끝 겨냥 (MotionEvent 구동)

### 6.1 왜 MotionEvent인가

겨냥은 **공격 모션의 특정 구간(찌르기 전조~타격, 활 드로우~릴리스)에서만** 켜져야 한다. 평상시 Idle/Move에 무기를 적에게 겨누면 부자연스럽다. MotionSet 타임라인의 구간 이벤트(`startTime~endTime`)가 이 요구에 정확히 들어맞으며, `MotionEvent_MotionWarp`가 이미 동일 패턴(구간 + resolver 정책 주입)으로 검증돼 있다.

### 6.2 `MotionEvent_WeaponAim` (신규)

`MotionEventBase` 상속. `Execute`(startTime)에서 겨냥 윈도우 열고, `OnCompleteEvent`(endTime)에서 닫는다. 실제 per-frame 솔브는 `WeaponIKController.LateUpdate`가 수행하므로, 이벤트는 윈도우/설정/타깃만 주입한다 (`RequiresPostEvaluation` 불필요 — Update 시점 타깃 결정으로 충분).

```csharp
[Serializable]
public class MotionEvent_WeaponAim : MotionEventBase
{
    [Header("Target Resolver")]
    [Tooltip("Hybrid 권장: 락온이 콘 안이면 락온, 밖이면 콘 최근접.")]
    public WarpResolverPolicy resolverPolicy = WarpResolverPolicy.Hybrid;

    [Header("Aim")]
    [Range(0,1)] public float aimWeight = 1f;
    public bool  overrideCone = false;        // true면 SO 기본값 대신 아래 값 사용
    public float aimConeAngle = 35f;
    public float maxCorrectionAngle = 25f;
    [Range(0,1)] public float spineAimBlend = 0f;

    [Header("Blend")]
    public float blendInTime  = 0.08f;
    public float blendOutTime = 0.12f;
    public AnimationCurve aimCurve;           // 정규화 t → weight (비우면 EaseOut 폴백)

    public override string GetDisplayName() => "Weapon Aim";
    public override string GetShortLabel()  => $"Aim:{resolverPolicy}";

    public override void Execute(GameObject target)
    {
        // MotionWarp(MotionEvent_MotionWarp.cs:122-124,144-145)와 동일 3중 폴백 —
        // target이 정확히 컴포넌트 보유 GO가 아닐 수 있음.
        var ik = target.GetComponent<WeaponIKController>()
              ?? target.GetComponentInChildren<WeaponIKController>()
              ?? target.GetComponentInParent<WeaponIKController>();
        var combat = target.GetComponent<PlayerCombat>()
              ?? target.GetComponentInChildren<PlayerCombat>()
              ?? target.GetComponentInParent<PlayerCombat>();
        if (ik == null || combat == null) return;

        // 타깃 결정 — MotionWarp와 동일하게 HybridResolver 재사용
        var resolver = WarpTargetResolverFactory.For(resolverPolicy);
        var ctx = combat.BuildWarpResolverContext();
        Transform resolved = resolver?.Resolve(in ctx);   // 없으면 null → 겨냥 자동 무력화

        ik.BeginAimWindow(new WeaponAimSettings {
            target            = resolved,
            weight            = aimWeight,
            coneAngle         = overrideCone ? aimConeAngle : -1f,        // -1 = SO 기본
            maxCorrection     = overrideCone ? maxCorrectionAngle : -1f,
            spineBlend        = spineAimBlend,
            blendIn           = blendInTime,
            blendOut          = blendOutTime,
            curve             = aimCurve,
        });
    }

    public override void OnCompleteEvent(GameObject target)
    {
        var ik = target.GetComponent<WeaponIKController>()
              ?? target.GetComponentInChildren<WeaponIKController>()
              ?? target.GetComponentInParent<WeaponIKController>();
        ik?.EndAimWindow();   // blendOut으로 페이드 아웃
    }
}
```

`BuildWarpResolverContext()`는 `MotionEvent_MotionWarp`에서 `PlayerCombat`가 이미 제공(콘 range/angle/layer/filter)하므로 그대로 재사용한다.

### 6.3 겨냥 솔브 (OnAnimatorIK, 그립 패스 앞)

```
if aimWindow inactive || target == null: aimWeight → 0; return
pivot     = 쥔 손(주손) 트랜스폼
tipDir    = weaponTip.forward                       // 현재 무기 조준 축
wantDir   = (target.position - pivot.position).normalized
error     = Angle(tipDir, wantDir)

// ── 앵글 게이트 (결정적 가드) ──
if error > coneAngle: aimWeight → 0 (blendOut); return        // 콘 밖 → 보정 안 함
correction = min(error, maxCorrectionAngle)                    // 어시스트 클램프

delta = Quaternion.AngleAxis(correction * weight, axis(tipDir→wantDir))   // FromToRotation 기반
// 적용 (기본 = 팔 한정, 척추 혼합은 옵션):
//  · 근접 찌르기(spineAimBlend=0): 주손 본에 delta 주입 → 이후 그립 타깃 재계산(§5.1)
//  · 원거리(spineAimBlend>0)     : delta 일부를 척추 본에 분담 — 단, ⚠️ 아래 Layer1 주의
```

- **모션워프 yaw 잔차**: 모션워프 회전은 KCC `UpdateRotation`을 통해 **FixedUpdate + Interpolate**로 루트에 적용된다. OnAnimatorIK에서 `transform.rotation`은 보간된 현재 포즈라 잔차 측정은 가능하나, FixedUpdate가 안 도는 렌더 프레임엔 잔차가 출렁일 수 있음 → `blendIn`/`aimCurve`로 완충하고 급격한 프레임 단위 보정 금지.
- ⚠️ **척추 additive ↔ Animancer Layer1 충돌**: Layer1은 `_upperBodyMask`로 상체를 덮는다(`ActorAnimator.cs:263-264`). 척추 본을 OnAnimatorIK에서 회전시키면 IK 패스는 layer 평가 *이후*라 동작은 하지만, 무기전환/공격 블렌드로 Layer1 상체 포즈가 출렁이는 구간에선 이중 회전·떨림이 보일 수 있다. **기본 정책: `spineAimBlend=0`(팔 한정)으로 출시**, 척추 혼합은 Layer1 마스크에서 빠진 본만 대상으로 별도 검증 후 도입.
- `weight`는 `blendIn/blendOut/aimCurve`로 보간. 타깃 소실/콘 이탈 시 즉시 0이 아닌 `blendOut`.

### 6.4 앵글 게이트 (소프트타깃 어시스트 정책 계승)

기존 "공격 카메라 어시스트 부분블렌드"에서 확립된 원칙 그대로:
- `aimConeAngle` 밖 타깃 → weight 0 블렌드 아웃 (등 뒤 적에게 무기 꺾임 사고 차단).
- `maxCorrectionAngle` 클램프 → "스냅"이 아닌 "어시스트" 체감.
- 발동 = 유효 타깃 존재 + 겨냥 MotionEvent 활성 구간뿐.

### 6.5 검증
1. 락온 중 창 찌르기: 무기 끝이 타깃 라인에 정렬, 콘 밖이면 보정 없음.
2. 활 드로우 구간: `spineAimBlend`로 상체가 자연스럽게 타깃을 향함.
3. 모션워프 yaw와 이중 보정 없음 (잔차만 처리되는지).
4. 보조손 그립이 재겨냥된 무기를 정확히 따라감 (겨냥→그립 순서 확인).

---

## 7. 상태머신 연동

`FootIKController.ForceDisabled`와 동일 패턴으로 `WeaponIKController.ForceDisabled`를 토글한다.

| 상태 | 그립 | 겨냥 |
|---|---|---|
| `PlayerIdleState` / `PlayerGroundMoveState` | ON | OFF (또는 원거리 전투준비 포즈만) |
| `PlayerAttackState` / `PlayerChargeState` / `PlayerDashAttackState` | 모션이벤트로 구간 토글 | `MotionEvent_WeaponAim` 구간만 ON |
| `PlayerHitState` / `PlayerDeathState` / 디졸브 | OFF | OFF |

- 그립의 공격 중 기본 OFF는 `ForceDisabled=true`, 필요 구간만 모션이벤트로 재활성.
- 겨냥은 본질적으로 모션이벤트 구동이므로 별도 상태 토글 최소.

---

## 8. 모델 교체 (캐릭터 스왑)

`CharacterModelData`/모델 스왑 시 `FootIKController.Refresh(newAnimator)`와 함께 `WeaponIKController.Refresh(newAnimator)` 호출 → 팔 본 재바인딩 + `FootIKRelay`/`WeaponIKRelay` 재등록(FootIK와 동일 모델). 그립/겨냥 윈도우는 스왑 시 초기화하고, `PlayerEquipment`가 새 무기 기준으로 `SetGrip` 재주입 → **`gripLocalPos/Rot` 재캐시**(§5.1). 새 스켈레톤은 본 비율이 달라 이전 오프셋이 무효이므로 반드시 재캐시.

---

## 9. 신규 / 수정 파일

**신규**
- `Component/Player/WeaponIKController.cs`
- `Component/Common/WeaponGripPoint.cs`
- `Component/Common/WeaponTipPoint.cs`
- `Data/Event/Animation/MotionEvent_WeaponAim.cs`
- `WeaponAimSettings` 구조체 (WeaponIKController.cs 내 또는 별도)

**수정**
- `Data/Item/WeaponDefinitionSO.cs` — 그립/겨냥 정책 필드
- `Component/Player/PlayerEquipment.cs` — 발도/교체 시 `SetGrip`/`ClearGrip` 주입
- Player 상태 스크립트(Attack/Charge/Hit/Death 등) — `ForceDisabled` 토글
- (선택) `CharacterModelData` 스왑 경로 — `WeaponIKController.Refresh` 연결

**에디터/에셋 작업**
- 양손무기 프리팹에 `WeaponGripPoint`(보조손) 배치.
- 겨냥 무기 프리팹에 `WeaponTipPoint`(끝/축) 배치.
- 해당 `WeaponDefinitionSO`에 `useOffHandGrip`/`useAimAssist` 등 설정.
- PlayerActor 프리팹에 `WeaponIKController` 부착.
- 겨냥 무기의 공격 MotionSet 타임라인에 `MotionEvent_WeaponAim` 구간 배치.

---

## 10. 구현 단계

0. **Phase 0 — 타이밍 프로토타입(필수 선행)**: §3 OnAnimatorIK 단일 패스에서 그립 1개만 솔브. 검증: (a) 손본+캐시오프셋 그립이 현재 프레임 정확, (b) MagicaCloth 손 콜라이더가 같은 프레임 따라감(FootIK와 동일 공존), (c) `TwoBoneIk` 본 직접 솔브가 다음 Animator 평가에 깔끔히 리셋. **여기서 막히면 전 설계 재검토.**
1. **Phase 1 — 보조손 그립(정적)**: 마커 + SO 필드 + `WeaponIKController`(OnAnimatorIK 릴레이 + TwoBoneIk 그립 패스) + `PlayerEquipment` 주입. Idle/Move 검증.
2. **Phase 2 — 상태 페이드**: `ForceDisabled`를 Attack/Hit/Charge/Death에 연결, 모션이벤트 구간 토글, 모델교체 `Refresh`.
3. **Phase 3 — 무기 끝 겨냥**: `MotionEvent_WeaponAim` + 겨냥 패스(그립 앞단) + `HybridResolver` 타깃 + 앵글 게이트. 창/활/스태프 검증. 척추 혼합은 `spineAimBlend=0`으로 시작.

---

## 11. 리스크 & 결정 근거 (설계 검토 2026-06-24 반영)

### 해소된 리스크 (검토로 재설계)
- **IK 타이밍 — 경로 B(LateUpdate) 폐기 → OnAnimatorIK 확정.** 초기엔 "OnAnimatorIK는 콘스트레인트 해석 전이라 그립 1프레임 지연"을 이유로 LateUpdate를 택했으나, ① 이 프로젝트는 LateUpdate 실행순서 강제 수단이 없고(매니저 루프 + KCC FixedUpdate + MagicaCloth 커스텀 PlayerLoop), ② FootIK가 이미 OnAnimatorIK에서 MagicaCloth와 공존(`FootIKController.cs:202`), ③ 그립을 **손본+캐시 오프셋으로 역산**하면 지연 자체가 사라짐(§3). → OnAnimatorIK 단일 패스로 통합해 검증된 타이밍 상속.
- **MagicaCloth 순서**: MagicaCloth는 MonoBehaviour LateUpdate가 아니라 **커스텀 PlayerLoop(`beforeLateUpdate`)** 로 돈다(`MagicaManager.cs`). 따라서 "LateUpdate 이른 시점에 먼저"는 보장 불가였음. OnAnimatorIK 회귀로 FootIK와 동일한 검증된 공존 모델을 따른다(Phase 0에서 실측).

### 잔존 리스크 (구현 시 관리)
- **TwoBoneIk drift**: 절대값 덮어쓰기라 누적 없음(안전). 단 hint collinear 시 팔꿈치 뒤집힘 가능(`TwoBoneIk.cs:96`) → hint pole·`elbowHintWeight` 튜닝.
- **척추 겨냥 ↔ Animancer Layer1 충돌**: `_upperBodyMask`(`ActorAnimator.cs:263`)와 겹치는 본 회전 시 떨림 → **`spineAimBlend=0`(팔 한정) 기본**, 척추 혼합은 후속 검증.
- **모션워프 잔차 측정**: yaw가 KCC FixedUpdate+보간이라 프레임 단위 출렁임 가능 → `blendIn`/`aimCurve` 완충.
- **앵글 게이트 필수**: 게이트/클램프 없으면 콘 밖 적에게 무기 꺾임 → 기존 소프트타깃 블렌드 정책과 일관.

### 검토 후 보류한 대안
- **그립을 Animation Rigging `TwoBoneIKConstraint`로** 구현하는 안: 엔진이 실행순서를 보장한다는 장점. 단 패키지 도입 비용 + 기존 Animancer/FootIK IK 모델과 이원화되는 단점. 본 설계는 기존 `TwoBoneIk`+OnAnimatorIK 재사용으로 일원화 유지. (단순 `ParentConstraint`로 손만 묶는 안은 팔 체인이 끊겨 부적합 → 제외.)

---

## 12. Phase 0 에디터 작업 & 검증 절차 (현재 구현됨)

> §9의 에디터/에셋 작업은 **전체 시스템(Phase 3 포함) 최종 기준**이다. 아래는 **지금 구현된 Phase 0 프로토타입을 돌려 검증하기 위해 사용자가 Unity 에디터에서 해야 할 최소 작업**이다.
> 구현 파일: `WeaponGripPoint.cs`, `WeaponIKController.cs` (둘 다 작성 완료).

### 12.1 에디터 셋업 (사용자 작업)
1. **그립 마커 배치** — 양손무기 프리팹(그레이트소드/창 등)을 열고, 빈 GameObject를 자식으로 추가 → `WeaponGripPoint` 컴포넌트 부착. 보조손이 잡을 위치/회전에 배치하고 `gripHand = LeftHand` 확인.
2. **컨트롤러 부착** — `PlayerActor` 프리팹 루트(= `FootIKController`가 있는 곳)에 `WeaponIKController` 부착.
3. **그립 주입(프로토타입 방식)** — Phase 0엔 `PlayerEquipment` 자동 배선이 없으므로:
   - 플레이 진입 → 무기 발도 → 스폰된 무기의 `WeaponGripPoint`를 컨트롤러 인스펙터의 **`_debugGripOverride`** 필드에 드래그(런타임 즉시 반영됨).
4. (선택) 인스펙터 노브: `_maxWeight`(기본 1), `_elbowHintWeight`(기본 0.5, 팔꿈치 뒤집힘 방지), `_drawGizmos`(기본 ON).

### 12.2 검증 절차 (3대 기준 = §10 Phase 0)
씬뷰 기즈모: **시안 구체 = 그립 목표 / 노랑 구체 = 실제 보조손 / 빨강선 = 오차**.

- **(a) 그립 정확도** — ⚠️ `DebugGripDistance` 절댓값으로 판정하지 말 것(손목-그립 피벗 오프셋 때문에 0이 안 될 수 있음). **올바른 판정: 주손을 움직이는 모션(이동/공격) 중 빨강선이 길어지지 않고 유지되는가** = 보조손이 같은 프레임에 무기를 따라감.
- **(b) 클로스 공존** — 보조손 소매/천(MagicaCloth)이 IK로 움직인 손을 같은 프레임 따라가는가(FootIK와 동일 OnAnimatorIK 모델).
- **(c) drift 없음** — `_maxWeight`를 런타임에 0으로 → 보조손이 애니메이션 포즈로 깔끔히 복귀(누적 잔상 없음).

### 12.3 자동 경고 (코드가 알려줌)
- **그립 미주입/IK 미발화**: 그립 지정 후 0.5s 내 `OnAnimatorIK`가 안 불리면 콘솔 경고 → Animancer Layer0 `ApplyAnimatorIK`(`ActorAnimator.cs:261`)·Animator IK Pass 점검.
- **정지 프레임 미포착**: 발도 직후 계속 움직여 그립 오프셋 락 타이밍을 못 잡으면 강제 락 + 경고(정확도 저하 가능).
- **본 바인딩 실패**: 모델이 휴머노이드가 아니면 경고.

### 12.4 관찰 포인트 (실측으로만 답 나오는 항목)
1. **휴머노이드 직접 본 쓰기 지속성** — weight를 올렸는데 보조손이 전혀 안 움직이면 retargeting이 직접 쓰기를 덮는 케이스 → 폴백 `Animator.SetIKPosition(AvatarIKGoal.LeftHand)`(팔꿈치 hint 제어 상실 감수).
2. **극단 포즈 팔 비틀림** — muscle 한계 우회 부작용. 보이면 보정 각/그립 도달 범위 클램프 필요.

→ 이 절차 통과 시 Phase 1(그립 정식화 + `PlayerEquipment.SetGrip` 배선)로 진행. 막히면 §11 폴백.
