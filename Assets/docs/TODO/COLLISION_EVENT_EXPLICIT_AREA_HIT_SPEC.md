# Collision Event 명시적 범위 판정 확장 스펙

> 작성일: 2026-08-01  
> 상태: 설계 완료 / 미구현  
> 대상 환경: Unity 6 (6000.0.60f1), Animancer MotionSet, Gameplay Ability  
> 범위: 플레이어·몬스터·궁극기의 Collision Event 판정 소스, 명시적 Shape, 런타임 요청 계약, 에디터·검증 확장  
> 관련 문서: `Assets/docs/guide/COMBAT_SYSTEM_GUIDE.md`, `Assets/docs/guide/MOTION_EVENT_ROLE_GUIDE.md`, `Assets/docs/design/ULTIMATE_SEQUENCE_SYSTEM_DESIGN.md`

---

## 0. 핵심 결론

현재 `BeginCollisionEvent`는 캐릭터 또는 장비 하위에 배치된 `CombatHitbox.groupId`만 활성화한다. 이 방식은 검, 주먹, 꼬리처럼 애니메이션을 따라 움직이는 부착형 판정에는 적합하지만, 궁극기 폭발·충격파·넓은 지면 타격처럼 **이벤트가 직접 중심과 크기를 소유해야 하는 공격**을 자연스럽게 표현하기 어렵다.

개선의 핵심은 `BeginCollisionEvent`가 다음 두 판정 소스 중 하나를 선택하게 하는 것이다.

1. `AttachedHitboxGroup`: 기존 무기·신체 부착형 HitBox 그룹을 사용한다.
2. `ExplicitShape`: 이벤트에 직렬화된 Sphere, Box 또는 Capsule을 직접 질의한다.

피해량과 리액션은 계속 `HitPhaseData`가 소유하고, 실제 판정 형상과 타이밍은 Collision Event가 소유한다. `HitPhaseData.targetingRange`를 실제 피해 반경으로 재사용하지 않는다.

기존 에셋의 무변경 호환을 위해 `AttachedHitboxGroup`을 enum의 0번 기본값으로 두고, 기존 `hitboxGroupId`와 `additionalHitboxGroupIds` 필드는 유지한다.

---

## 1. 현재 구현 현황

### 1.1 런타임 흐름

```text
BeginCollisionEvent
    ├── hitPhaseIndex
    ├── hitboxGroupId
    └── additionalHitboxGroupIds
            ↓
CombatActionRunner.HandleCollisionEvent()
            ↓
ICombatCollisionExecutor의 setter들을 순서대로 호출
            ↓
PlayerCombat / EnemyCombat.BeginHitboxWindow()
            ↓
CombatHitboxSet.BeginGroup(s)
            ↓
CombatHitDetector.DetectAttachedHits()
            ↓
Physics.OverlapBoxNonAlloc / OverlapCapsuleNonAlloc
            ↓
CombatHit → 기존 ReceiveHit·방어·피드백 처리
```

### 1.2 핵심 파일

| 파일 | 현재 책임 |
|------|-----------|
| `GameActor/Animation/MotionEvents/MotionEvent_Collision.cs` | Collision 윈도우 시작·종료, HitPhase와 HitBox 그룹 전달 |
| `GameActor/Combat/Action/CombatActionRunner.cs` | Motion Event와 Combat 실행체 사이의 중계 및 액션 상태 기록 |
| `GameActor/Combat/Action/ICombatCollisionExecutor.cs` | PlayerCombat/EnemyCombat 충돌 실행 계약 |
| `GameActor/Combat/Detection/CombatHitbox.cs` | 부착형 Box/Capsule 저작 데이터와 Sweep 상태 |
| `GameActor/Combat/Detection/CombatHitboxSet.cs` | 그룹 수집·활성화·윈도우 수명 관리 |
| `GameActor/Combat/Detection/CombatHitDetector.cs` | 명시적 Physics Overlap 질의와 `IDamageable` 해석·중복 제거 |
| `GameActor/Component/Player/PlayerCombat.HitDetection.cs` | 플레이어 적중 전달과 공격자 측 피드백 |
| `GameActor/Component/Enemy/EnemyCombat.cs` | 몬스터 적중 전달과 방어/회피 경로 유지 |
| `GameActor/Combat/Ultimate/UltimateTimelineEvent.cs` | `UltimateDamageWindowEvent`의 단순 Collision 활성·비활성 |
| `Data/Combat/CombatData.cs` | `HitPhaseData` 피해·리액션·텔레그래프 데이터 |

### 1.3 현재 구조의 장점

- 무기와 신체 본을 따라 움직이는 판정을 별도 좌표 계산 없이 표현한다.
- Box/Capsule Sweep으로 빠른 검격의 프레임 사이 누락을 줄인다.
- Player와 Enemy가 같은 `CombatHitDetector`를 사용한다.
- Collision Window 동안 `_hitTargets`를 유지해 대상당 1회 타격을 보장한다.
- `NonAlloc` 질의와 Collider→`IDamageable` 캐시가 이미 존재한다.

이 기반은 유지하고, 명시적 범위 판정을 동일한 검출 결과 형식인 `CombatHit`으로 합류시킨다.

---

## 2. 식별된 문제

### P1. 판정 소스가 부착형 Collider로 고정되어 있다

현재 Collision Event는 그룹 ID만 전달할 수 있다. 광역 공격 하나를 만들기 위해 캐릭터 프리팹에 거대한 비활성 Collider와 `CombatHitbox`를 미리 부착하면, 콘텐츠별 임시 판정이 캐릭터 구조에 누적되고 프리팹 관리 비용이 증가한다.

### P2. 실제 피해 범위를 표현할 권위 데이터가 없다

`HitPhaseData.targetingRange`와 `impactOffset`은 텔레그래프·AI 위협 위치에 사용된다. 이를 피해 범위로 겸용하면 다음 의미가 결합된다.

- AI가 공격 가능하다고 판단하는 거리
- 바닥 텔레그래프와 위협 UI의 표시 범위
- 실제 Physics 피해 판정 범위

이 값들은 서로 비슷할 수 있지만 항상 같지는 않다.

### P3. setter 기반 실행 계약이 복합 판정 요청에 취약하다

현재 Runner는 `ClearHitTargets`, `SetTargetLayerMask`, `SetHitPhaseIndex`, `SetHitboxGroup`, `SetHitboxGroups`, `SetEnableCollision`을 순서대로 호출한다. 명시적 Shape, Anchor, 평가 방식이 추가되면 호출 순서와 이전 윈도우 상태 초기화에 대한 암묵적 규칙이 늘어난다.

### P4. 지속시간 0인 폭발은 판정 기회를 잃을 수 있다

현재 검출은 Collision 활성 상태를 Player/Enemy 업데이트에서 폴링한다. 같은 프레임에 Begin과 Complete가 모두 처리되는 이벤트는 실제 Overlap 검사 전에 비활성화될 수 있다. 순간 폭발은 시작 시점에 한 번의 검출을 명시적으로 보장해야 한다.

### P5. 광역 공격의 반응 방향 정책이 없다

부착형 공격은 HitBox 이동 방향이나 공격자 전방을 사용할 수 있다. 반면 폭발과 충격파는 일반적으로 `범위 중심 → 피격자` 방향으로 넉백해야 한다. 당기기 공격은 그 반대 방향이 필요하다.

### P6. Ultimate 경로가 그룹과 Shape 설정을 전달하지 않는다

`UltimateDamageWindowEvent`는 현재 `SetEnableCollision(true/false)`만 호출한다. 일반 Motion Collision만 확장하면 궁극기 전용 타임라인에서 같은 기능을 사용할 수 없다.

---

## 3. 목표와 비목표

### 3.1 목표

- Collision Event에서 Sphere, Box, Capsule의 중심·크기·회전을 직접 저작한다.
- 기존 부착형 HitBox 그룹 동작과 직렬화 결과를 보존한다.
- 순간 폭발과 지속 Collision Window를 명시적으로 구분한다.
- Actor Root, Attack Origin, Primary Target, 고정 월드 위치를 판정 Anchor로 선택한다.
- Anchor를 시작 시 스냅샷하거나 윈도우 동안 추적할 수 있다.
- Player, Enemy, Ultimate가 같은 Collision 요청과 검출 코어를 사용한다.
- 기존 `ReceiveHit`, 방어, 회피, HitPhase, 공격자 피드백 경로를 재사용한다.
- Scene View와 개발 빌드에서 실제 질의 Shape를 확인할 수 있다.
- 기존 MotionSet managed reference와 VFX 참조를 손상시키지 않고 단계적으로 도입한다.

### 3.2 비목표

- 투사체가 이동하거나 장시간 장판으로 남는 공격을 Collision Window로 대체하지 않는다. 수명과 틱 주기를 가진 공격은 `ProjectileDefinitionSO`와 Projectile Runtime의 책임으로 유지한다.
- `HitPhaseData.targetingRange`를 삭제하거나 기존 텔레그래프 베이크 규약을 즉시 변경하지 않는다.
- 첫 구현에서 Cone, 임의 Mesh, Polygon 판정을 지원하지 않는다.
- 첫 구현에서 거리별 피해 감쇠, 다중 틱, 대상별 재타격 쿨다운을 추가하지 않는다.
- 공격 수치의 단일 소스인 Ability Payload/`HitPhaseData` 구조를 변경하지 않는다.

---

## 4. 제안 데이터 모델

> 아래 타입과 API 이름은 구현 제안이며 아직 코드에 존재하지 않는다.

### 4.1 판정 소스

```csharp
public enum CollisionSourceType
{
    AttachedHitboxGroup = 0,
    ExplicitShape = 1,
}
```

`AttachedHitboxGroup`을 0으로 두는 것은 필수다. 기존 `BeginCollisionEvent` 에셋은 신규 필드를 직렬화하지 않으므로 C# 기본값 0으로 종전 경로를 유지한다.

### 4.2 명시적 Shape

```csharp
public enum CollisionShapeType
{
    Sphere,
    Box,
    Capsule,
}

public enum CollisionAnchorType
{
    ActorRoot,
    AttackOrigin,
    PrimaryTarget,
    WorldPosition,
}

public enum CollisionAnchorSampling
{
    SnapshotOnBegin,
    FollowDuringWindow,
}

public enum CollisionEvaluationType
{
    Window,
    OnceOnBegin,
}

public enum CollisionDirectionType
{
    ShapeCenterToTarget,
    TargetToShapeCenter,
    ActorForward,
    AnchorForward,
}
```

```csharp
[Serializable]
public sealed class ExplicitCollisionShapeData
{
    public CollisionShapeType shapeType = CollisionShapeType.Sphere;
    public CollisionAnchorType anchor = CollisionAnchorType.ActorRoot;
    public CollisionAnchorSampling anchorSampling = CollisionAnchorSampling.SnapshotOnBegin;
    public CollisionEvaluationType evaluation = CollisionEvaluationType.OnceOnBegin;
    public CollisionDirectionType direction = CollisionDirectionType.ShapeCenterToTarget;

    public Vector3 localOffset;
    public Vector3 localEulerAngles;

    [Min(0.01f)] public float radius = 5f;
    public Vector3 boxSize = new(10f, 3f, 10f);
    [Min(0.01f)] public float capsuleHeight = 4f;

    public Vector3 worldPosition;
}
```

Shape별 유효 필드는 다음과 같다.

| Shape | 사용 필드 | Inspector 검증 |
|-------|-----------|----------------|
| Sphere | `radius` | `radius > 0` |
| Box | `boxSize`, `localEulerAngles` | 모든 축 `> 0` |
| Capsule | `radius`, `capsuleHeight`, `localEulerAngles` | `capsuleHeight >= radius * 2` |

### 4.3 BeginCollisionEvent 확장

```csharp
public class BeginCollisionEvent : MotionEventBase
{
    public int hitPhaseIndex;

    public CollisionSourceType collisionSource;

    // AttachedHitboxGroup 전용 — 기존 필드 유지
    public string hitboxGroupId;
    public List<string> additionalHitboxGroupIds = new();

    // ExplicitShape 전용
    public ExplicitCollisionShapeData explicitShape = new();
}
```

기존 필드의 이름이나 타입을 바꾸지 않는다. 신규 Shape 데이터는 이벤트와 같은 Actor 런타임 어셈블리 경계에 두고, Data 모듈이 Actor 구현을 참조하게 만들지 않는다.

### 4.4 런타임 요청

직렬화 데이터와 런타임 해석 결과를 분리한다.

```csharp
public readonly struct CollisionRequest
{
    public readonly int HitPhaseIndex;
    public readonly LayerMask TargetLayerMask;
    public readonly CollisionSourceType SourceType;
    public readonly IReadOnlyList<string> HitboxGroupIds;
    public readonly ResolvedCollisionShape ExplicitShape;
}
```

`ResolvedCollisionShape`는 Anchor Transform, 월드 중심/회전, 추적 여부 등 실행 시점의 값을 보유한다. 에셋의 `ExplicitCollisionShapeData`를 런타임 상태 저장소로 직접 사용하지 않는다.

---

## 5. 제안 런타임 아키텍처

### 5.1 원자적 실행 계약

`ICombatCollisionExecutor`는 장기적으로 다음 형태를 권장한다.

```csharp
public interface ICombatCollisionExecutor
{
    void BeginCollision(in CollisionRequest request);
    void EndCollision();
    void ClearHitTargets();
}
```

하나의 요청으로 모든 상태를 전달하면 이전 그룹·Shape·Anchor가 다음 윈도우에 남지 않는다. 기존 setter API는 전환 기간 동안 어댑터로 유지할 수 있지만 신규 호출부에서는 사용하지 않는다.

### 5.2 실행 흐름

```text
BeginCollisionEvent.Execute()
    ↓
CombatActionRunner가 CollisionRequest 생성
    ├── HitPhase와 대상 LayerMask 결합
    ├── Attached 그룹 정규화
    └── Explicit Anchor 해석 또는 해석 정보 전달
            ↓
PlayerCombat / EnemyCombat.BeginCollision()
            ↓
Collision Session 시작
    ├── AttachedHitboxGroup → CombatHitboxSet.BeginGroups()
    └── ExplicitShape       → ResolvedCollisionShape 활성화
            ↓
평가 방식
    ├── OnceOnBegin → 즉시 한 번 Detect 및 적중 처리
    └── Window      → 기존 Update/LateUpdate에서 프레임당 한 번 Detect
            ↓
CombatHit 목록
            ↓
기존 Player/Enemy 적중 전달·방어·피드백 경로
```

### 5.3 검출 코어

`CombatHitDetector`의 현재 `CollectAttachedShapeHits` 내부 공통 로직을 Shape 종류와 무관한 공용 수집 함수로 정리한다.

```text
DetectAttachedHits()
    └── 부착형 CombatHitbox의 현재/이전 Shape를 샘플링

DetectExplicitHits()
    └── ResolvedCollisionShape를 한 번 질의

두 경로
    └── CollectShapeHits()
            ├── NonAlloc Overlap
            ├── ownerRoot 제외
            ├── Collider → IDamageable 캐시
            ├── ignoredTargets / frame 중복 제거
            ├── CanTakeDamage / IsAlive 정책
            └── CombatHit 생성
```

Sphere는 `Physics.OverlapSphereNonAlloc`, Box는 기존 `OverlapBoxNonAlloc`, Capsule은 기존 `OverlapCapsuleNonAlloc`을 사용한다. 버퍼가 가득 찰 때의 1회 경고와 임시 배열 폴백 정책도 기존 코드와 통일한다.

### 5.4 Collision Session 소유권

`CombatHitboxSet`은 이름과 현재 책임상 부착형 그룹 저장소다. 다음 두 안 중 **A안을 우선**한다.

#### A안: 별도 `CombatCollisionSession` 추가 — 권장

- `CombatHitboxSet`: 부착형 그룹 수집과 Sweep 상태만 유지한다.
- `CombatCollisionSession`: 현재 요청, 평가 방식, 명시적 Shape, Anchor 스냅샷, 활성 여부를 소유한다.
- PlayerCombat/EnemyCombat은 Session을 통해 현재 판정 소스를 검사한다.

장점은 부착형 저장소에 광역 Shape 책임을 억지로 넣지 않고, 향후 복합 Shape 또는 판정 필터를 확장하기 쉽다는 것이다.

#### B안: `CombatHitboxSet`에 Explicit 상태 추가

변경 파일 수는 적지만 클래스 이름과 책임이 어긋난다. 단기 프로토타입 외에는 권장하지 않는다.

### 5.5 Anchor 해석

| Anchor | 기준 | 권장 용도 |
|--------|------|-----------|
| ActorRoot | `GameActor.transform` | 자기 중심 충격파, 버프형 범위 |
| AttackOrigin | Combat이 소유한 공격 원점 | 손·무기 앞쪽 폭발, 전방 Box |
| PrimaryTarget | 현재 공격/궁극기 주 대상 | 대상 중심 낙뢰, 처형 폭발 |
| WorldPosition | 이벤트에 저작되거나 런타임 Context가 제공한 위치 | 무대 고정 연출, 지정 지점 폭발 |

`PrimaryTarget`과 런타임 지정 월드 위치는 `GameObject target`만 받는 현재 `MotionEventBase.Execute` 인자로 완전히 표현할 수 없다. Runner/Combat이 현재 액션 Context에서 해석하도록 하고, 해석 실패 시 ActorRoot로 조용히 폴백하지 않는다. 설정 오류를 로그하고 해당 판정을 중단한다.

`SnapshotOnBegin`은 시작 시 Pose를 고정한다. 지면 폭발과 타겟 위치에 남는 공격의 기본값으로 사용한다. `FollowDuringWindow`는 캐릭터 오라처럼 판정이 Anchor를 따라야 할 때만 사용한다.

### 5.6 순간 판정 보장

`OnceOnBegin`은 `BeginCollision()` 호출 안에서 즉시 검출과 적중 처리를 한 번 수행하고 자동 종료한다. 이벤트 duration이나 Player/Enemy 업데이트 순서에 의존하지 않는다.

`Window`는 기존 방식처럼 활성 기간 동안 프레임당 한 번 검사한다. `_hitTargets`는 윈도우 시작 시 비우고 종료 전까지 유지하므로, Shape 안에 계속 서 있는 대상은 한 번만 맞는다.

다중 틱 장판이 필요하면 Collision Event에 재타격 쿨다운을 추가하지 않고 Projectile/지속 영역 런타임을 사용한다.

### 5.7 공격 방향

명시적 Shape의 `CombatHit.AttackDirection`은 다음 정책으로 계산한다.

| 정책 | 계산 | 용도 |
|------|------|------|
| ShapeCenterToTarget | `hitPoint - shapeCenter` | 폭발, 방사형 넉백 |
| TargetToShapeCenter | `shapeCenter - hitPoint` | 끌어당기기, 흡입 |
| ActorForward | `ownerRoot.forward` | 전방 일괄 밀기 |
| AnchorForward | `anchor.forward` | 회전된 Box/Capsule 방향 공격 |

벡터 길이가 0에 가까우면 Actor Forward를 최종 폴백으로 사용한다. EnemyCombat이 현재 `_attackOrigin.forward`로 덮어쓰는 지점도 `CombatHit.AttackDirection`을 존중하도록 함께 정리해야 한다.

---

## 6. HitPhase 및 텔레그래프 경계

### 6.1 소유권

| 데이터 | 권위 소유자 |
|--------|-------------|
| 피해, Poise, Break, Reaction, Force | `HitPhaseData` |
| 판정 시작·종료 시간 | `BeginCollisionEvent.startTime/duration` |
| 부착형 판정 선택 | Collision Event의 HitBox 그룹 |
| 명시적 판정 중심·크기·회전 | Collision Event의 Explicit Shape |
| AI 선택 거리 | Ability/AI 선택 정책 |
| 텔레그래프 표시 | `HitPhaseData.impactOffset/targetingRange` 또는 후속 베이크 결과 |

### 6.2 텔레그래프 연동

첫 구현에서는 기존 `impactOffset`과 `targetingRange`를 유지한다. 이후 에디터 베이크를 추가할 경우 다음 단방향 흐름만 허용한다.

```text
Collision Event Explicit Shape
    ↓ Bake
HitPhaseData 텔레그래프 근사값
```

런타임 판정이 `targetingRange`를 읽어 Shape를 만드는 역방향 폴백은 두지 않는다. Box/Capsule은 단일 반경으로 정확히 표현되지 않으므로 텔레그래프가 원형만 지원한다면 외접 반경 또는 전용 표시 타입을 명시해야 한다.

---

## 7. Ultimate 및 외부 AOE와의 관계

### 7.1 UltimateDamageWindowEvent

`UltimateDamageWindowEvent`도 같은 Collision 요청을 저작할 수 있어야 한다. 두 가지 구현안이 있다.

1. 이벤트 자체에 `CollisionSourceType`과 `ExplicitCollisionShapeData`를 추가한다.
2. `BeginCollisionEvent`와 Ultimate 이벤트가 공유하는 `CollisionEventData`를 도입한다.

중복을 줄이기 위해 2안을 권장한다. Ultimate Runtime Context가 `PrimaryTarget`, Stage Transform, World 위치를 제공하므로 Anchor 해석만 Ultimate 어댑터에서 수행한다.

시네마틱 스테이지가 활성화된 경우 `WorldPosition`과 회전은 VFX 이벤트와 같은 Stage Transform 규약을 따라야 한다. 화면상 VFX와 실제 판정 위치가 서로 다른 좌표계에 생기지 않도록 Play Mode 테스트가 필요하다.

### 7.2 AOEProjectile

`AOEProjectile`은 다음 요구에 계속 사용한다.

- 투사체가 목표 지점까지 이동한 뒤 폭발
- 시간이 지나며 범위가 확장
- 대상별 재타격 쿨다운
- 지속 장판과 수명 관리
- 거리별 피해 감쇠

Collision Event Explicit Shape는 **액터 모션 타임라인의 특정 순간 또는 짧은 윈도우에 직접 연결된 판정**에 사용한다. 단순 궁극기 폭발을 위해 보이지 않는 AOEProjectile을 임시 생성하는 경로는 만들지 않는다.

---

## 8. 에디터 저작 및 디버그 표시

### 8.1 MotionSet Editor Inspector

`BeginCollisionEvent.collisionSource`에 따라 필드를 조건부 표시한다.

```text
판정 소스: Attached HitBox Group
    ├── Hit Phase Index
    ├── Primary Group
    └── Additional Groups

판정 소스: Explicit Shape
    ├── Hit Phase Index
    ├── Shape Type
    ├── Anchor / Sampling
    ├── Evaluation
    ├── Offset / Rotation
    ├── Radius 또는 Size/Height
    └── Direction
```

`GetShortLabel()`은 예를 들어 다음처럼 표시한다.

- `Collision [P0 / Default]`
- `Collision [P1 / Sphere 8m / Target / Once]`
- `Collision [P2 / Box 10×3×6 / AttackOrigin / Window]`

### 8.2 Scene View 프리뷰

- 이벤트 선택 시 MotionSet 프리뷰 캐릭터 기준으로 Shape Wire를 표시한다.
- Sphere/Box/Capsule을 서로 다른 색으로 구분한다.
- Anchor와 중심 사이에 선을 표시한다.
- `ShapeCenterToTarget` 등 방향 정책은 작은 화살표로 표시한다.
- `PrimaryTarget` 프리뷰는 Motion Warp 더미 타겟 인프라와 같은 프리뷰 타겟을 재사용할 수 있는지 먼저 검토한다.

### 8.3 런타임 디버그

기존 `HitboxRuntimeDebugRenderer`가 Explicit Shape도 표시하도록 Debug Registry 또는 공용 디버그 샘플 계약을 확장한다. 개발 빌드에서 다음을 구분한다.

- 현재 활성 Shape
- 실제 Physics 질의에 사용된 Shape
- Snapshot Anchor 위치
- 감지된 Hit Point와 공격 방향

릴리스 빌드에서는 현재 부착형 HitBox 디버그와 동일하게 스트립되도록 조건부 컴파일 경계를 유지한다.

### 8.4 기존 에디터 도구 영향

`WeaponSlashSetupWindow`는 모든 `BeginCollisionEvent` 시작점에 Slash VFX를 동기화한다. Explicit Shape는 검 궤적이 아닐 수 있으므로 다음 중 하나를 적용해야 한다.

- `AttachedHitboxGroup` 이벤트만 자동 Slash 동기화한다.
- 또는 명시적 `syncSlashVfx` 플래그를 추가한다.

기본 정책은 Attached 전용이 안전하다. 광역 폭발 Collision이 추가되었다는 이유로 Slash VFX가 자동 생성되면 안 된다.

---

## 9. 호환성과 마이그레이션

### 9.1 기존 에셋

- `CollisionSourceType.AttachedHitboxGroup = 0`을 유지한다.
- `hitboxGroupId`, `additionalHitboxGroupIds`, `hitPhaseIndex`를 이름 변경하거나 이동하지 않는다.
- 기존 `HitPhaseData.hitboxGroupId`의 Phase Default 폴백을 유지한다.
- Explicit Shape에서는 Phase의 `hitboxGroupId`를 무시한다.
- 기존 MotionSet을 일괄 저장하거나 재직렬화하지 않는다.

### 9.2 SerializeReference 안전

`BeginCollisionEvent`는 `[SerializeReference]` 기반 Motion Event다. 타입 자체를 다른 어셈블리로 이동하지 않는다. 이동이 불가피하다면 기존 규칙대로 `[MovedFrom(true, sourceAssembly: "UPlayGround.Actor")]` 또는 실제 이전 어셈블리를 정확히 지정한다.

구현 후 다음 검사를 수행한다.

- MotionSet/Ultimate managed reference 누락 0
- VFX 참조 누락 0
- 기존 Collision Event 수와 역직렬화 성공 수 일치
- `Assets/10.Datas/` 자동 재직렬화 diff 검사
- `Assets/03.Prefabs/` 자동 변경 diff 검사

### 9.3 단계적 API 전환

1. `CollisionRequest`와 신규 Begin/End API를 추가한다.
2. 기존 setter API가 내부적으로 Request Builder 또는 레거시 Attached 요청으로 연결되게 한다.
3. Motion Event, Ultimate, Residual Combat 호출부를 신규 API로 전환한다.
4. 전체 호출부와 테스트가 전환된 뒤 레거시 setter 제거 여부를 별도 판단한다.

한 번의 변경에서 setter 제거와 모든 콘텐츠 마이그레이션을 동시에 수행하지 않는다.

---

## 10. 구현 단계

### Phase 1. 공용 데이터와 요청 계약

- [ ] `CollisionSourceType`, Shape/Anchor/Evaluation/Direction enum 추가
- [ ] `ExplicitCollisionShapeData` 추가
- [ ] `CollisionRequest`, `ResolvedCollisionShape` 추가
- [ ] `ICombatCollisionExecutor.BeginCollision/EndCollision` 추가
- [ ] 기존 Attached 호출을 신규 요청으로 연결
- [ ] 기존 Collision Event 직렬화 회귀 테스트 추가

### Phase 2. Explicit Shape 검출

- [ ] `CombatHitDetector.DetectExplicitHits` 구현
- [ ] Sphere/Box/Capsule NonAlloc 질의 구현
- [ ] 공통 owner 제외·Damageable 캐시·중복 제거 재사용
- [ ] `CombatCollisionSession` 추가
- [ ] `Window` 평가를 Player/Enemy 프레임당 1회 검출에 연결
- [ ] `OnceOnBegin` 즉시 검출 보장
- [ ] Direction 정책을 `CombatHit.AttackDirection`에 반영

### Phase 3. Motion Event와 전투 경로

- [ ] `BeginCollisionEvent` Inspector 데이터 추가
- [ ] `GetShortLabel` 개선
- [ ] PlayerCombat 요청 처리
- [ ] EnemyCombat 요청 처리
- [ ] ResidualPlayerCombat 호환
- [ ] 취소·상태 전환·OnDisable에서 Session 종료 보장
- [ ] 기존 Attached Sweep 회귀 확인

### Phase 4. Ultimate 연결

- [ ] 공유 `CollisionEventData` 도입 여부 확정
- [ ] `UltimateDamageWindowEvent`에 명시적 판정 연결
- [ ] PrimaryTarget/WorldPosition Anchor 해석
- [ ] Cinematic Stage 좌표 변환 일치
- [ ] 궁극기 종료·중단 시 Collision Session 정리

### Phase 5. 에디터와 검증

- [ ] 조건부 Inspector UI
- [ ] Scene View Shape 프리뷰
- [ ] 런타임 Debug Renderer 확장
- [ ] Shape 값 검증 및 오류 뱃지
- [ ] `WeaponSlashSetupWindow`의 Explicit 이벤트 제외
- [ ] 텔레그래프 근사 베이크 필요성 재평가

### Phase 6. 자동 검증과 콘텐츠 수직 슬라이스

- [ ] 플레이어 자기 중심 Sphere 궁극기
- [ ] 몬스터 PrimaryTarget 중심 Box 공격
- [ ] 지속 Window형 오라 판정
- [ ] Unity Play Mode 방어·회피·피드백 검증
- [ ] Player Build 검증

---

## 11. 자동 테스트 설계

### 11.1 EditMode

| 테스트 | 기대 결과 |
|--------|-----------|
| 기존 Event 기본값 | 신규 필드가 없는 에셋은 Attached 경로 선택 |
| Sphere 질의 | 반경 내부 대상만 검출 |
| 회전 Box 질의 | 회전된 Box 내부 대상만 검출 |
| Capsule 질의 | 끝점과 원통 구간 모두 검출 |
| Owner 제외 | 공격자와 하위 Collider는 결과에서 제외 |
| 다중 Collider 대상 | 하나의 `IDamageable`이 한 번만 반환 |
| ignoredTargets | 이미 맞은 대상은 다음 프레임 결과에서 제외 |
| Direction | 방사/흡입/전방 정책별 정규화 방향 일치 |
| Anchor Snapshot | Anchor 이동 후에도 시작 Pose 유지 |
| Anchor Follow | Anchor 이동 시 다음 프레임 Shape 이동 |
| 잘못된 Shape | 0 이하 크기 또는 잘못된 Capsule 높이를 검증 오류로 보고 |

### 11.2 PlayMode

| 테스트 | 기대 결과 |
|--------|-----------|
| OnceOnBegin duration 0 | 같은 프레임에 정확히 한 번 피해 |
| Window 내부 체류 | 윈도우 전체에서 대상당 한 번 피해 |
| Window 중 신규 진입 | 진입한 프레임에 한 번 피해 |
| Player 광역 공격 | 다중 몬스터 피해, 피드백 임팩트는 공격 정책대로 제한 |
| Enemy 광역 공격 | 무적 플레이어도 방어/회피 Resolver까지 전달 |
| 취소 | 취소 후 추가 검출 없음 |
| PrimaryTarget 소멸 | 오류 없이 판정 중단 또는 정의된 Snapshot 유지 |
| Ultimate Stage | VFX와 피해 Shape의 월드 위치 일치 |
| Attached 회귀 | 기존 무기 그룹과 Sweep 결과 유지 |

### 11.3 데이터 검증

- Explicit Source인데 `explicitShape`가 null이면 오류다.
- Attached Source인데 Primary와 Additional 그룹이 모두 비어 있으면 Phase Default 사용을 정보로 표시한다.
- WorldPosition Anchor인데 런타임 Context 좌표가 필요한 설정이면 사전 검증한다.
- OnceOnBegin의 duration은 의미가 없음을 Inspector에 표시하되, 기존 타임라인 블록 표시를 위해 값 보존 여부를 결정한다.
- Window인데 duration이 0이면 오류 또는 Once 전환 안내를 표시한다.
- Box/Capsule 회전과 Anchor Follow 조합을 프리뷰할 수 없으면 경고한다.

---

## 12. 완료 기준

- 기존 Collision Event 에셋이 수정 없이 Attached HitBox 그룹으로 동작한다.
- Collision Event Inspector에서 Sphere/Box/Capsule의 중심과 크기를 직접 지정할 수 있다.
- duration 0인 `OnceOnBegin` 판정도 정확히 한 번 실행된다.
- Window 판정은 프레임당 최대 한 번 질의하며 대상당 1회 타격을 유지한다.
- Player, Enemy, Ultimate가 같은 Collision Request와 검출 코어를 사용한다.
- 광역 넉백과 흡입 방향이 Shape 중심 기준으로 올바르게 전달된다.
- Scene View와 개발 빌드 표시가 실제 Physics 질의 Shape와 일치한다.
- 기존 WeaponSlash 자동 동기화가 Explicit Shape 이벤트에 Slash VFX를 생성하지 않는다.
- Unity 컴파일 오류 0, 런타임 예외 0, Play Mode 테스트 통과, Player Build 오류 0이다.
- MotionSet/Ultimate managed reference 누락 0, VFX 참조 누락 0, Missing Script 0이다.
- 검증 과정에서 발생한 `Assets/10.Datas/`, `Assets/03.Prefabs/` 변경은 diff를 검사하고 사용자 변경을 보존한다.

---

## 13. 구현 전 확정할 결정 사항

| 항목 | 권장안 | 이유 |
|------|--------|------|
| 활성 상태 소유 | 별도 `CombatCollisionSession` | 부착형 그룹 저장소와 명시적 Shape 책임 분리 |
| 순간 공격 | `OnceOnBegin` 즉시 검출 | 업데이트 순서와 duration 0 문제 제거 |
| 기존 에셋 기본값 | Attached = 0 | 무마이그레이션 호환 |
| Shape 1차 범위 | Sphere, Box, Capsule | Unity NonAlloc 질의와 현재 디버그 형상 재사용 가능 |
| 기본 Anchor | ActorRoot + Snapshot | 궁극기 폭발의 예측 가능한 위치 보장 |
| 기본 방향 | ShapeCenterToTarget | 광역 폭발의 일반적인 넉백 의미 |
| 텔레그래프 연동 | Shape → Telegraph 단방향 베이크 | 런타임 피해 형상의 권위 유지 |
| 지속 장판 | Projectile/전용 영역 런타임 유지 | 틱·수명·재타격 쿨다운 책임 분리 |

이 표의 권장안을 기준으로 Phase 1을 시작할 수 있다. Cone, 복합 Shape, 거리 감쇠는 실제 콘텐츠 수직 슬라이스에서 필요성이 확인된 뒤 별도 확장으로 다룬다.
