# 부착형 HitBox 충돌 판정 구현 계획

> 작성일: 2026-06-20  
> 상태: Legacy 판정 제거 및 공격/MotionSet 데이터 마이그레이션 완료, Prefab 자동 부착 실행 대기
> 대상 버전: Unity 6 (6000.0.60f1), Animancer Pro V8  
> 범위: 근접 공격 `BeginCollisionEvent` 판정을 무기·신체 부착형 HitBox로 전환

---

## 개요

기존 근접 공격은 액터 전방의 구형 범위와 각도로 대상을 검색했으나, 해당 Legacy 판정은 제거했다.

이 구조를 다음 원칙으로 교체한다.

- `BeginCollisionEvent`는 판정 시점과 사용할 HitBox 그룹만 지정한다.
- 실제 판정 형상은 무기나 신체 본에 부착된 `BoxCollider` 또는 `CapsuleCollider`가 제공한다.
- Collider의 `OnTriggerEnter`/`OnCollisionEnter`를 사용하지 않고, 중앙 검출기가 Collider 형상을 읽어 명시적으로 `Physics.Overlap*NonAlloc`을 실행한다.
- 별도 `Hurtbox` 컴포넌트는 만들지 않는다.
- 피격 대상은 기존 Collider에서 `IDamageable`을 직접 조회하고, 없으면 부모 계층에서 조회한다.
- 최종 피해 처리는 기존 `HitRequest → IDamageable.ReceiveHit → CombatResolutionPipeline` 경로를 유지한다.
- 플레이어, 지상 몬스터, 비행 몬스터, 스왑 잔류 공격이 같은 HitBox 실행 경로를 사용한다.
- HitBox 수동 배치 비용을 줄이기 위해 무기·신체 자동 부착 에디터를 함께 구현한다.

---

## 구현 현황

2026-06-20 기준 다음 항목을 구현했다.

- `CombatHitbox`, `CombatHitboxSet`, Box/Capsule 월드 형상과 Sweep 보간 판정
- 기존 대상 Collider에서 `IDamageable`/부모 `IDamageable` 조회
- Collision Window 내 동일 대상 중복 제거
- `BeginCollisionEvent.hitboxGroupId`와 `HitPhaseData.hitboxGroupId`
- Player, Enemy, 스왑 잔류 공격의 공통 부착형 HitBox 경로
- 동적 무기 장착 후 `CombatHitboxSet.Refresh()`
- 그룹 누락 시 공격 판정 중단과 설정 오류 출력
- 무기 Renderer Bounds Auto Fit
- Humanoid 손·발·머리·몸통 기본 프리셋
- Generic 본 이름 규칙 자동 생성
- Profile SO, Validate, Refit, Remove Generated, FBX 원본 수정 차단
- 자동 생성 마커 기반 중복 방지와 수동 수정 항목 보존
- MotionSet Scene View의 활성 그룹 Box/Capsule 표시와 기본 Collider 핸들 편집
- 런타임/에디터 어셈블리 컴파일 검증

35개 공격 데이터와 365개 Collision 포함 MotionSet의 직렬화 마이그레이션을 완료했다.
남은 작업은 Unity Editor에서 `UPlayGround/Combat/Migration/Migrate All Attached HitBoxes`
메뉴를 실행해 Weapon/Actor Prefab에 Collider를 저장하고 Play Mode에서 형상을 보정하는 것이다.

아래의 “예정”, “단계” 설명은 설계 이력이다. 최종 구현에서는 Legacy Sphere 폴백과
`HitDetectionMode`를 제거했으며 모든 근접 Collision Event가 부착형 HitBox 그룹을 요구한다.

---

## 설계 결정

### Hurtbox를 별도로 두지 않는다

대상의 기존 물리 Collider를 피격 표면으로 사용한다.

```text
공격 HitBox Overlap
    ↓
검색된 Collider
    ├── GetComponent<IDamageable>()
    └── GetComponentInParent<IDamageable>()
            ↓
        HitRequest 생성
            ↓
        ReceiveHit()
```

이 결정에 따른 제약은 다음과 같다.

| 항목 | 정책 |
|------|------|
| 피격 부위별 배율 | 이번 범위에서 지원하지 않음 |
| 머리/약점 판정 | 별도 시스템이 필요할 때 기존 Collider에 선택적 메타데이터 컴포넌트만 추가 |
| 피격 지점 | 검색된 Collider의 `ClosestPoint` 사용 |
| 피격 방향 | HitBox 이동 방향 우선, 이동량이 작으면 공격자에서 대상 방향 사용 |
| 무적 판정 | 기존 `IDamageable.ReceiveHit`과 `CombatResolutionPipeline`이 담당 |
| 자기 자신 제외 | HitBox 소유 액터와 그 자식 Collider 제외 |

### Collider는 저작용 형상으로만 사용한다

`CombatHitbox`에 연결된 Collider는 다음 상태를 유지한다.

- `isTrigger = true`
- `enabled = false` 또는 전용 Layer로 물리 상호작용 차단
- Rigidbody 충돌 콜백을 사용하지 않음
- 런타임 검출 시 `center`, `size`, `radius`, `height`, Transform을 읽어 Overlap 질의를 구성

Collider를 실제 물리 이벤트용으로 켰다 끄는 방식은 히트스톱, 낮은 TimeScale, 빠른 무기 이동, FixedUpdate 간격의 영향을 받으므로 사용하지 않는다.

---

## 목표 아키텍처

```text
MotionSet
└── BeginCollisionEvent
      ├── hitPhaseIndex
      └── hitboxGroupId
              │
              ▼
CombatActionRunner
├── 현재 HitPhase
├── 활성 HitBox 그룹
└── Collision Window
              │
              ▼
ICombatCollisionExecutor
└── PlayerCombat / EnemyCombat / ResidualPlayerCombat
              │
              ▼
CombatHitboxSet
├── 자식 CombatHitbox 수집
├── 그룹 활성화
├── 이전 프레임 Pose 저장
└── 그룹 단위 검출
              │
              ▼
CombatHitDetector
├── Box Overlap
├── Capsule Overlap
├── 이전/현재 Pose 보간 Sweep
└── IDamageable 중복 제거
              │
              ▼
HitRequest → ReceiveHit → CombatResolutionPipeline
```

---

## 예정 파일 구조

```text
Assets/02.Scripts/
├── GameActor/Combat/Detection/
│   ├── CombatHitbox.cs
│   ├── CombatHitboxSet.cs
│   ├── CombatHitboxPose.cs
│   ├── CombatHitboxShape.cs
│   └── CombatHitDetector.cs              기존 파일 확장
│
├── Data/Event/Animation/
│   └── MotionEvent_Collision.cs          hitboxGroupId 추가
│
├── GameActor/Combat/Action/
│   ├── CombatActionRunner.cs             활성 그룹 소유
│   ├── CombatActionInstance.cs
│   └── ICombatCollisionExecutor.cs        그룹 전달 API 추가
│
├── GameActor/Component/Player/
│   ├── PlayerCombat.cs
│   └── ResidualPlayerCombat.cs
│
├── GameActor/Component/Enemy/
│   └── EnemyCombat.cs
│
└── Tool/Editor/Combat/
    ├── CombatHitboxSetupWindow.cs
    ├── CombatHitboxAutoFitter.cs
    ├── CombatHitboxSetupProfileSO.cs
    ├── CombatHitboxSetupValidator.cs
    └── CombatDataValidator.cs             HitBox 검증 추가
```

파일명과 API는 구현 단계에서 조정할 수 있으나 책임 경계는 유지한다.

---

## 런타임 컴포넌트

### CombatHitbox

무기 또는 신체 본에 부착하는 단일 공격 판정 형상이다.

예정 필드:

| 필드 | 형식 | 설명 |
|------|------|------|
| `groupId` | `string` | Collision Event가 선택할 그룹. 예: `MainWeapon`, `LeftFoot`, `BodyCharge` |
| `shapeCollider` | `Collider` | `BoxCollider` 또는 `CapsuleCollider` |
| `owner` | `GameActor` | 런타임 자동 탐색 또는 `CombatHitboxSet`에서 주입 |
| `useSweep` | `bool` | 이전 Pose와 현재 Pose 사이 보간 검출 여부 |
| `sweepStepDistance` | `float` | 이동 거리에 따른 보간 간격 |
| `maxSweepSteps` | `int` | 프레임 급변 시 최대 샘플 수 |
| `enabledForDetection` | `bool` | 현재 Collision Window에서 선택된 형상인지 여부 |
| `debugColor` | `Color` | Scene View 및 런타임 기즈모 색상 |

지원 Collider:

- `BoxCollider`
- `CapsuleCollider`

`SphereCollider`는 신체 부위의 단순 판정이 필요할 때 선택적으로 지원할 수 있지만, 초기 구현 필수 대상은 아니다.

### CombatHitboxSet

액터 단위 HitBox 레지스트리다.

주요 책임:

- `GetComponentsInChildren<CombatHitbox>(true)`로 전체 HitBox 수집
- `groupId`별 캐시 생성
- 장비 교체 및 캐릭터 스왑 후 재수집
- 그룹 활성화/비활성화
- 활성 그룹의 이전 Pose와 현재 Pose 관리
- 한 Collision Window에서 이미 맞은 `IDamageable` 집합 관리

예정 API:

```csharp
public void Refresh();
public bool HasGroup(string groupId);
public void BeginGroup(string groupId);
public void EndGroup(string groupId);
public int DetectActiveGroup(
    LayerMask targetLayer,
    ISet<IDamageable> ignoredTargets,
    List<CombatHit> results,
    bool includeInvincibleTargets);
```

`Refresh()` 호출 시점:

- 액터 초기화
- `PlayerActor.RefreshForCharacter()` 이후
- `PlayerEquipment.EquipWeapon()` 이후
- 무기 재생성 이후
- 스왑 잔류 모델 생성 이후

### CombatHitboxPose

빠른 무기 이동의 터널링 방지를 위해 이전 프레임과 현재 프레임의 형상 상태를 저장한다.

Box 기준 저장값:

- 월드 중심
- 월드 회전
- 월드 Half Extents

Capsule 기준 저장값:

- 월드 시작점
- 월드 끝점
- 월드 반경

---

## 판정 알고리즘

### Box HitBox

현재 Pose는 다음 방식으로 검사한다.

```csharp
Physics.OverlapBoxNonAlloc(
    worldCenter,
    worldHalfExtents,
    buffer,
    worldRotation,
    targetLayer,
    QueryTriggerInteraction.Collide);
```

Transform의 비균일 Scale을 고려해 `BoxCollider.size`에 `transform.lossyScale`의 절댓값을 곱한다.

### Capsule HitBox

`CapsuleCollider.direction`에 따라 로컬 축을 선택하고 월드 시작점과 끝점을 계산한다.

```csharp
Physics.OverlapCapsuleNonAlloc(
    worldPoint0,
    worldPoint1,
    worldRadius,
    buffer,
    targetLayer,
    QueryTriggerInteraction.Collide);
```

비균일 Scale에서는 캡슐 축과 수직인 두 축 중 큰 Scale을 반경에 적용한다.

### Sweep 판정

현재 프레임 Pose만 검사하면 빠른 검격이 대상을 건너뛸 수 있으므로 다음 순서로 검사한다.

1. Collision Window가 열릴 때 현재 Pose를 이전 Pose로 초기화한다.
2. 매 Update에서 이전 Pose와 현재 Pose의 중심 이동 거리를 계산한다.
3. `ceil(distance / sweepStepDistance)`로 샘플 수를 구한다.
4. 샘플 수를 `1..maxSweepSteps`로 제한한다.
5. 위치는 선형 보간하고 회전은 `Quaternion.Slerp`로 보간한다.
6. 각 샘플에서 Overlap을 실행한다.
7. 한 Window 내 같은 `IDamageable`은 한 번만 결과에 추가한다.
8. 검사 후 현재 Pose를 이전 Pose로 저장한다.

초기 권장값:

| 대상 | `useSweep` | `sweepStepDistance` | `maxSweepSteps` |
|------|------------|---------------------|-----------------|
| 검·도끼·창 | true | 0.15m | 8 |
| 주먹·발 | true | 0.2m | 5 |
| 몸통 돌진 | false | - | - |
| 대형 꼬리·날개 | true | 0.25m | 8 |

Sweep는 Continuous Collision Detection이 아니라 명시적 다중 Overlap이다. HitStop 중 같은 Pose가 반복되어도 Window의 중복 대상 집합으로 재피격을 방지한다.

---

## Collision Event 변경

### BeginCollisionEvent

현재 `hitPhaseIndex`에 그룹 식별자를 추가한다.

```csharp
[Tooltip("CombatHitbox.groupId. 비어 있으면 공격 데이터 또는 기본 그룹을 사용한다.")]
public string hitboxGroupId;
```

표시 예시:

```text
Collision [P0 / MainWeapon]
Collision [P1 / LeftFoot]
```

실행 흐름:

```text
BeginCollisionEvent.Execute
→ CombatActionRunner.HandleCollisionEvent(
      enable: true,
      hitPhaseIndex,
      hitboxGroupId,
      targetLayer)
→ executor.SetHitboxGroup(hitboxGroupId)
→ executor.ClearHitTargets()
→ executor.SetEnableCollision(true)
```

종료 시 같은 그룹을 닫는다.

### 기본 그룹 폴백

데이터 마이그레이션을 위해 다음 우선순위를 사용한다.

1. `BeginCollisionEvent.hitboxGroupId`
2. `HitPhaseData.hitboxGroupId`
3. `"Default"`
4. 그룹이 없으면 개발 빌드에서 오류 로그 후 Legacy Sphere 판정

Legacy 폴백은 마이그레이션 기간에만 유지하고 전체 프리팹 전환 후 제거한다.

---

## 공격 데이터 변경

### HitPhaseData

추가 예정 필드:

```csharp
[Header("Attached HitBox")]
public HitDetectionMode hitDetectionMode = HitDetectionMode.AttachedHitbox;
public string hitboxGroupId = "Default";
```

마이그레이션용 모드:

```csharp
public enum HitDetectionMode
{
    LegacySphere,
    AttachedHitbox,
}
```

단계적으로 사용 중단할 필드:

- `attackOffset`
- `attackRadius`
- `hitHeightRange`
- `AbilityAttackInfo.hitAngle`
- `ChargeStageData.hitAngle`

이 필드들은 텔레그래프나 AI 사거리 산정에 재사용되고 있으므로 즉시 삭제하지 않는다. 판정 형상과 AI 사용 거리 데이터를 분리한 뒤 제거한다.

### AI 거리 데이터와 HitBox 분리

HitBox 크기를 AI의 공격 가능 거리로 사용하면 무기 애니메이션 Pose에 따라 값이 불안정해진다.

다음 데이터는 유지한다.

- `GameplayAbilitySO.activation.minDistance`
- `GameplayAbilitySO.activation.maxDistance`
- 모션 워프 검색 거리
- 텔레그래프 범위

즉, “공격을 시작할 수 있는 거리”와 “실제 무기가 닿는 형상”은 서로 다른 데이터다.

---

## 자동 부착 에디터

### 메뉴와 창

예정 메뉴:

```text
UPlayGround/Combat/HitBox Setup
UPlayGround/Generator Tool/Combat HitBox Setup
```

예정 창:

```text
CombatHitboxSetupWindow
```

지원 입력:

- Project 창에서 선택한 무기 프리팹
- Project 창에서 선택한 플레이어/몬스터 프리팹
- 현재 Prefab Stage 루트
- 씬에서 선택한 액터 인스턴스
- 폴더 단위 일괄 처리

처리 전에는 대상 목록과 생성/수정/건너뜀 건수를 Preview로 보여주고, 명시적 Apply 버튼으로만 변경한다.

### 실행 모드

| 모드 | 대상 | 처리 |
|------|------|------|
| Weapon Auto Fit | 무기 프리팹 | Renderer Bounds 기반 Box/Capsule HitBox 생성 |
| Humanoid Body Setup | Humanoid Animator | `HumanBodyBones` 기준 손·발·머리·몸통 본에 HitBox 생성 |
| Generic Body Setup | Generic/몬스터 | 이름 규칙과 Renderer Bounds 기반 후보 본 탐색 |
| Validate Only | 전체 | 변경 없이 누락·중복·잘못된 그룹 검사 |
| Refit Existing | 기존 HitBox | Transform/그룹 유지, Collider 크기만 재계산 |
| Remove Generated | 자동 생성 항목 | 생성 마커가 있는 HitBox만 제거 |

### 자동 생성 식별

자동 생성된 오브젝트에는 생성 출처를 기록한다.

예정 컴포넌트:

```text
CombatHitboxGeneratedMarker
```

저장 정보:

- 생성기 버전
- 생성 프로필 GUID
- 원본 Renderer 또는 Bone 경로
- 생성 시각
- 사용자가 수동 수정했는지 여부

재실행 정책:

- 동일 경로와 그룹의 자동 생성 HitBox가 있으면 중복 생성하지 않음
- 수동 수정 플래그가 있으면 기본적으로 덮어쓰지 않음
- `강제 Refit` 옵션에서만 Collider 치수를 갱신
- 수동으로 만든 `CombatHitbox`는 절대 삭제하지 않음

---

## 무기 HitBox 자동 부착

### 대상 Renderer 선택

무기 프리팹 하위에서 다음 순서로 Renderer를 수집한다.

1. 활성/비활성 하위 `MeshRenderer`
2. 활성/비활성 하위 `SkinnedMeshRenderer`
3. FX, Trail, ParticleSystem 하위 Renderer 제외
4. 이름에 `Sheath`, `Scabbard`, `Effect`, `Trail`, `VFX`, `FX`가 포함된 오브젝트 제외
5. 실제 메시 Bounds 체적이 가장 큰 Renderer를 주 형상 후보로 선택

제외 규칙은 `CombatHitboxSetupProfileSO`에서 수정 가능하게 한다.

### 형상 결정

Renderer의 루트 로컬 Bounds를 계산한 뒤 가장 긴 축을 기준으로 기본 형상을 정한다.

| 형상 비율 | 자동 선택 |
|-----------|-----------|
| 한 축이 나머지 축보다 충분히 김 | CapsuleCollider |
| 넓고 납작한 검날·도끼날 | BoxCollider |
| 체적이 매우 작음 | 경고 후 생성 보류 |

권장 기본:

- 검, 창, 봉: Capsule
- 도끼, 대검, 방패 타격: Box
- 쌍검: 각 무기 프리팹에 개별 `MainWeapon`/`SubWeapon` 그룹

### Bounds 계산

월드 Bounds를 그대로 사용하지 않고 프리팹 루트 로컬 공간으로 변환한다.

1. Renderer Bounds의 8개 꼭짓점을 계산한다.
2. HitBox 부착 대상 Transform의 로컬 공간으로 변환한다.
3. 로컬 최소/최대값으로 center와 size를 계산한다.
4. 프로필의 padding을 적용한다.
5. 최소 두께를 보장한다.

무기 손잡이까지 메시 Bounds에 포함돼 판정이 과도하게 커지는 경우를 위해 다음 옵션을 제공한다.

- 축 방향 시작/끝 Trim 비율
- Center Offset
- 크기 Padding
- Capsule/Box 강제 선택

### 동적 장비와의 연동

`PlayerEquipment.EquipWeapon()`으로 생성되는 무기는 무기 프리팹 자체에 `CombatHitbox`가 저장돼 있어야 한다.

런타임 생성 후:

```text
EquipWeapon
→ 무기 프리팹 생성
→ Actor 하위에 배치
→ CombatHitboxSet.Refresh()
```

장비가 디졸브되거나 제거될 때는 `CombatHitboxSet` 캐시에서 무효 참조를 제거한다.

---

## 신체 HitBox 자동 부착

### Humanoid 캐릭터

`Animator.isHuman == true`이면 `Animator.GetBoneTransform(HumanBodyBones.*)`으로 본을 찾는다.

초기 제공 프리셋:

| 그룹 | 기준 본 | 권장 형상 |
|------|---------|-----------|
| `RightFist` | RightHand | Box |
| `LeftFist` | LeftHand | Box |
| `RightFoot` | RightFoot | Capsule |
| `LeftFoot` | LeftFoot | Capsule |
| `Head` | Head | Sphere 또는 Box |
| `BodyCharge` | Chest/Spine | Capsule |

본 Transform에 Collider를 직접 추가하지 않고, 다음 자식 오브젝트를 생성한다.

```text
RightHand
└── [HitBox] RightFist
      ├── BoxCollider
      ├── CombatHitbox
      └── CombatHitboxGeneratedMarker
```

애니메이션 리그 본에 불필요한 컴포넌트를 직접 붙이지 않고, 로컬 오프셋 조정과 삭제를 안전하게 하기 위함이다.

### Generic 및 몬스터

비휴머노이드 액터는 프로필의 이름 규칙을 사용한다.

예시 후보:

```text
hand_r, r_hand, right_hand
hand_l, l_hand, left_hand
foot_r, r_foot, right_foot
head
tail_01, tail_end
wing_l, wing_r
weapon, sword, axe, claw, horn
```

동일 규칙에 여러 Transform이 걸리면 자동 확정하지 않고 후보 목록을 표시한다.

꼬리·날개처럼 여러 본으로 구성된 부위는 다음 중 하나를 선택한다.

- 시작 본과 끝 본을 지정해 Capsule 생성
- 각 본에 작은 Capsule을 연속 생성
- 메시 Bounds 기반 Box 생성

초기 구현은 “시작/끝 Transform 지정 Capsule”을 우선한다.

---

## CombatHitboxSetupProfileSO

캐릭터/무기 종류별 자동 생성 규칙을 데이터화한다.

예정 필드:

```text
profileId
targetKind                  Weapon / Humanoid / Generic
defaultGroupId
preferredShape
includeNamePatterns
excludeNamePatterns
padding
minimumThickness
axisTrimStart
axisTrimEnd
useSweep
sweepStepDistance
maxSweepSteps
boneRules[]
```

프로필 예시:

- `SwordHitboxProfile`
- `SpearHitboxProfile`
- `DoubleAxeHitboxProfile`
- `HumanoidMeleeBodyProfile`
- `GriffinBodyHitboxProfile`

프로필은 자동화의 시작값이며, 생성 후 프리팹에서 수동 보정할 수 있다.

---

## 에디터 창 UX

```text
┌ Combat HitBox Setup ──────────────────────────────┐
│ 대상: [Prefab / Folder / Prefab Stage]            │
│ 프로필: [SwordHitboxProfile              ▼]       │
│ 모드: [Weapon Auto Fit                    ▼]       │
│                                                   │
│ [Scan] [Validate] [Preview] [Apply]               │
├───────────────────────────────────────────────────┤
│ Prefab                 상태        결과            │
│ Sword_A.prefab         누락        MainWeapon 생성 │
│ Sword_B.prefab         기존        유지            │
│ Griffin.prefab         후보 2개    사용자 선택 필요│
├───────────────────────────────────────────────────┤
│ 선택 대상 Preview                                │
│ Shape: Capsule / Group: MainWeapon                │
│ Center / Size / Sweep 설정                        │
│ [Scene View Preview] [선택 항목만 적용]           │
└───────────────────────────────────────────────────┘
```

필수 기능:

- Undo 지원
- Prefab contents 로드/저장
- 다중 선택 및 폴더 재귀 처리
- 적용 전 Scene View 기즈모 Preview
- 생성 결과 요약
- 읽기 전용 패키지/모델 FBX 직접 수정 금지
- FBX가 선택되면 Variant 또는 별도 Prefab 생성 경로 안내

---

## MotionSet Editor 연동

현재 `MotionSetWindow.CombatOverlay`는 `HitPhaseData.attackRadius`와 `hitAngle`을 Scene View에 표시한다.

부착형 HitBox 전환 후에는 다음 방식으로 변경한다.

1. 현재 선택된 액터 또는 Preview Actor에서 `CombatHitboxSet`을 찾는다.
2. 현재 커서 시각에 활성인 `BeginCollisionEvent.hitboxGroupId`를 구한다.
3. 해당 그룹의 Box/Capsule을 Scene View에 표시한다.
4. Animation Preview Pose가 바뀔 때 HitBox Transform도 함께 갱신한다.
5. “씬 핸들 편집”은 Collider의 center/size/radius/height를 수정한다.
6. 연결된 HitBox가 없으면 기존 Legacy Sphere를 주황색 경고 형상으로 표시한다.

오버레이 상태 문구:

```text
P0 / MainWeapon / Capsule 1개
P1 / LeftFoot / Box 1개
⚠ 그룹 없음: TailSweep
⚠ 활성 그룹에 유효 Collider가 없음
```

---

## 검증 규칙

`CombatDataValidator`와 전용 `CombatHitboxSetupValidator`에 다음 검사를 추가한다.

### Error

- `AttachedHitbox` 공격인데 `hitboxGroupId`가 비어 있음
- Collision Event가 참조하는 그룹이 프리팹에 없음
- `CombatHitbox.shapeCollider`가 null
- 지원하지 않는 Collider 타입
- HitBox 소유 `GameActor`를 찾을 수 없음
- 같은 액터에 그룹 ID의 대소문자만 다른 항목이 공존
- 무기 프리팹이 런타임에 생성되지만 HitBox가 없음

### Warning

- Collider 크기가 0 또는 지나치게 작음
- Collider의 `isTrigger`가 false
- Collider가 실제 물리 충돌 Layer에 포함됨
- 빠른 무기 그룹인데 `useSweep`가 false
- 하나의 그룹에 지나치게 많은 HitBox가 연결됨
- Collision Window가 있는데 모든 HitBox가 비활성 오브젝트 아래에 있음
- Legacy Sphere 공격이 남아 있음
- 자동 생성 마커 버전이 현재 생성기보다 오래됨

### Info

- 사용되지 않는 HitBox 그룹
- 사용되지 않는 Legacy `attackRadius`, `hitAngle`
- 자동 생성 후 수동 수정된 HitBox

---

## 구현 단계

### 1단계: 런타임 기반

- `CombatHitbox`, `CombatHitboxSet`, Pose 구조 추가
- Box/Capsule 현재 Pose Overlap 구현
- `CombatHitDetector`에 부착형 HitBox 경로 추가
- 기존 `HitRequest` 결과 흐름 연결
- 디버그 기즈모 추가

완료 기준:

- 플레이어 검 공격 한 종류가 부착형 Box/Capsule로 적중한다.
- 별도 Hurtbox 없이 기존 적 Collider를 탐색한다.
- 한 Collision Window에서 같은 적을 한 번만 맞힌다.

### 2단계: MotionEvent 그룹 연동

- `BeginCollisionEvent.hitboxGroupId` 추가
- `CombatActionRunner`가 활성 그룹을 소유
- `ICombatCollisionExecutor` 그룹 API 추가
- 플레이어/몬스터/잔류 공격 executor 통합
- Legacy Sphere 폴백 구현

완료 기준:

- 한 모션에서 `MainWeapon → LeftFoot`처럼 페이즈별 그룹 전환이 가능하다.
- Collision Event 종료 시 그룹이 반드시 닫힌다.

### 3단계: Sweep

- 이전/현재 Pose 저장
- 이동 거리 기반 보간 Overlap
- 회전 보간
- 프레임 급변 최대 샘플 제한

완료 기준:

- 낮은 프레임과 높은 애니메이션 속도에서도 빠른 검격이 정지 대상 Collider를 통과하지 않는다.

### 4단계: 자동 부착 에디터

- `CombatHitboxSetupProfileSO`
- 무기 Renderer Bounds Auto Fit
- Humanoid 본 프리셋
- Generic 이름 규칙
- Preview/Apply/Undo/Prefab 저장
- 자동 생성 마커와 재실행 정책

완료 기준:

- 무기 프리팹 다중 선택 후 HitBox 일괄 생성 가능
- Humanoid 프리팹에 주먹/발/몸통 그룹 자동 생성 가능
- 재실행 시 중복 생성하지 않는다.

### 5단계: MotionSet Editor 및 검증

- Combat Overlay를 부착형 HitBox 표시로 전환
- Collider Scene Handle 편집
- Data Validator 그룹 정합성 검사
- 프로젝트 전체 Legacy Sphere 잔여 리포트

완료 기준:

- MotionSet 타임라인에서 현재 Collision 구간의 실제 HitBox를 확인할 수 있다.
- 누락된 그룹과 잘못된 Collider를 에디터에서 일괄 검출한다.

### 6단계: 데이터 마이그레이션

권장 순서:

1. 플레이어 기본 캐릭터와 기본 무기
2. 휴머노이드 근접 몬스터
3. 대형 몬스터의 손·발·머리
4. 비행 몬스터의 발톱·날개·급강하 몸통
5. 스왑 잔류 공격
6. 전체 Legacy Sphere 폴백 제거

---

## 테스트 체크리스트

### 런타임

- [ ] 무기 메시 방향과 무관하게 Collider 회전이 정확히 반영된다.
- [ ] 비균일 Scale 프리팹에서 Box/Capsule 크기가 정확하다.
- [ ] 빠른 검격이 낮은 FPS에서도 적을 통과하지 않는다.
- [ ] HitStop 중 같은 대상이 반복 피격되지 않는다.
- [ ] 멀티히트 공격은 페이즈가 바뀌면 같은 대상을 다시 맞힐 수 있다.
- [ ] 플레이어 공격은 무적 몬스터에 가짜 피드백을 출력하지 않는다.
- [ ] 적 공격은 무적 플레이어까지 전달되어 퍼펙트 도지/대시 회피를 판정한다.
- [ ] 동적 장비 교체 후 새 무기 HitBox가 즉시 등록된다.
- [ ] 캐릭터 스왑 후 활성 모델의 신체 HitBox만 사용된다.
- [ ] 잔류 공격 모델의 HitBox가 원래 플레이어와 중복 등록되지 않는다.

### 에디터

- [ ] 다중 선택 무기 프리팹에 Undo 가능한 자동 생성이 된다.
- [ ] FBX 원본을 직접 수정하지 않는다.
- [ ] 자동 생성 재실행 시 중복 오브젝트가 생기지 않는다.
- [ ] 수동 수정 HitBox는 기본 재실행에서 보존된다.
- [ ] Scene View Preview와 런타임 판정 형상이 일치한다.
- [ ] 잘못된 그룹 ID가 Validator Error로 표시된다.

---

## 주의 사항

### 기존 Collider 품질에 피격 정확도가 의존한다

Hurtbox를 두지 않으므로 대상 프리팹의 기존 Collider가 지나치게 크거나 루트에 하나만 있으면 실제 메시와 피격 위치가 다를 수 있다. 이 경우 공격 HitBox를 정교하게 만들어도 대상 Collider 크기 이상의 정확도는 얻을 수 없다.

따라서 다음 최소 기준은 필요하다.

- 캐릭터 루트 Collider가 시각 모델보다 과도하게 크지 않을 것
- 비행/대형 몬스터의 공격 대상 Collider가 전투 중 활성 상태일 것
- 장식물·트리거 Collider가 전투 Target Layer에 섞이지 않을 것

### 무기 Collider를 물리 충돌에 사용하지 않는다

자동 생성 Collider는 공격 판정 데이터다. 환경 충돌, 무기끼리의 물리 충돌, 방패의 실제 물리 막기에는 사용하지 않는다.

### 문자열 그룹 ID 관리

초기에는 제작 편의상 문자열을 사용하되, 오타 방지를 위해 에디터 드롭다운은 현재 프리팹의 `CombatHitbox.groupId` 목록을 제공한다. 프로젝트 규모가 커지면 `CombatHitboxGroupId` enum 또는 Registry SO 자동 생성으로 전환할 수 있다.

### 네트워크 결정성은 범위 밖이다

현재 프로젝트는 싱글플레이 기준이다. Overlap 결과 순서는 보장되지 않으므로 필요하면 `IDamageable` Instance ID 또는 거리 기준 정렬을 추가한다.

---

## 최종 완료 조건

- 모든 근접 Collision Event가 실제 무기·신체 부착 Collider 그룹을 사용한다.
- 별도 Hurtbox 없이 기존 Collider와 `IDamageable` 탐색으로 피격 대상을 결정한다.
- Legacy 각도/구형 판정은 프로젝트에서 제거된다.
- 빠른 무기 이동에 Sweep 보정이 적용된다.
- 무기 및 신체 HitBox 자동 부착 에디터가 Preview, Undo, 재실행 안전성을 제공한다.
- MotionSet Editor에서 실제 HitBox 형상과 활성 그룹을 확인·편집할 수 있다.
- Combat Validator가 MotionEvent, 공격 데이터, 프리팹 HitBox 그룹의 정합성을 검증한다.
