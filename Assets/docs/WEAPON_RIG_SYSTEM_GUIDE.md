# Weapon Rig 시스템 가이드

> 작성일: 2026-06-21  
> 대상 버전: Unity 6 (6000.0.60f1), URP  
> 상태: 조사 및 도입 설계. Weapon Rig 런타임 구현은 아직 없음

---

## 개요

Weapon Rig 시스템의 목적은 공격 또는 조준 중 무기와 발사 지점이 전투 타겟을 자연스럽게 향하도록 보정하는 것이다.

현재 프로젝트는 다음 기능을 이미 제공한다.

- `ParentConstraint` 기반 무기 손/수납 소켓 전환
- 락온 타겟을 향한 플레이어 본체의 수평 회전
- 락온 또는 적 스킬 타겟을 향한 투사체 발사 방향 계산
- `ILockOnTarget.FocusPosition` 기반 카메라 락온 포커스 계산

그러나 무기 모델 자체의 조준 보정은 없다. 따라서 투사체는 타겟을 향해 발사되더라도 발사 직전 무기 또는 총구가 다른 방향을 바라보는 시각적 불일치가 발생할 수 있다.

이 문서는 현재 구조를 정리하고, 기존 무기 장착 시스템을 유지하면서 Weapon Rig를 추가하는 권장 방안을 정의한다.

---

## 현재 구현 현황

### 확인 결과

| 항목 | 현재 상태 |
|------|-----------|
| 무기 부착 | `PlayerEquipment`가 `ParentConstraint`를 탐색하고 무기 프리팹을 자식으로 배치 |
| 발도/납도 | `ParentConstraint` source 0/1 weight 전환 |
| 플레이어 방향 보정 | `PlayerAttackState.UpdateRotation()`에서 락온 타겟 방향으로 본체 수평 회전 |
| 투사체 방향 | `SpawnProjectileEvent`가 타겟 위치에서 `flyDirection`과 `Quaternion.LookRotation()` 계산 |
| 공통 타겟 포커스 | 카메라 락온 내부에서 `ILockOnTarget.FocusPosition` 사용 |
| 무기 전용 Aim Rig | 없음 |
| Animation Rigging 패키지 | `Packages/manifest.json`에 등록되지 않음 |
| `RigBuilder` / `MultiAimConstraint` / `TwoBoneIKConstraint` | 사용하지 않음 |

`Player_Bokusei.prefab`에서 확인되는 `AimConstraint`는 `SK_R_Obi_Offset`, `SK_L_Obi_Offset`에 적용된 의상 보정용이다. Weapon Rig로 사용되는 구성은 아니다.

### 현재 처리 흐름

```text
CameraManager
└── 락온 타겟 Transform 제공
        │
        ├── PlayerAttackState
        │   └── 플레이어 본체를 타겟 XZ 방향으로 회전
        │
        └── SpawnProjectileEvent
            ├── 타겟 위치 계산
            ├── 발사 방향 계산
            └── 투사체 회전 및 이동 방향 설정

PlayerEquipment
└── Weapon 루트의 ParentConstraint 탐색
    ├── source 0: 손 소켓
    ├── source 1: 수납 소켓
    └── 생성한 무기 프리팹 부착
```

이 구조에서는 캐릭터 본체와 투사체가 각각 타겟 방향을 처리하지만, 둘 사이에 있는 무기 모델의 회전 보정 단계가 비어 있다.

---

## 관련 파일

| 파일 | 관련 책임 |
|------|-----------|
| `Assets/02.Scripts/GameActor/Component/Player/PlayerEquipment.cs` | 무기 프리팹 생성 결과 부착, `ParentConstraint` 관리, 발도/납도 |
| `Assets/02.Scripts/GameActor/Component/Player/WeaponAttachmentResolver.cs` | 모델의 `Weapon` 루트와 무기 타입별 constraint 탐색 |
| `Assets/02.Scripts/GameActor/State/Player/PlayerAttackState.cs` | 공격 호밍 타겟 선택과 플레이어 본체 회전 |
| `Assets/02.Scripts/GameActor/Component/Player/PlayerTargetingController.cs` | 공격 보정 후보 탐색 |
| `Assets/02.Scripts/Data/Event/Animation/MotionEvent_SpawnProjectile.cs` | 락온/스킬 타겟 위치를 사용한 투사체 방향 계산 |
| `Assets/02.Scripts/Camera/CameraLockOn.cs` | `ILockOnTarget`, `FocusPosition`, 락온 타겟 유지 |
| `Assets/03.Prefabs/Actor/Player_Bokusei.prefab` | `Weapon` 루트와 무기별 `ParentConstraint` 구성 |

---

## 문제 정의

### 무기와 투사체 방향이 분리되어 있다

`SpawnProjectileEvent`는 발사 시점에 다음 순서로 방향을 계산한다.

```text
spawnPoint 위치
→ 타겟 위치 확인
→ targetPosition - worldPos
→ Quaternion.LookRotation
→ 투사체 생성
```

이 계산은 투사체에만 반영된다. `spawnPoint`의 부모인 무기 모델은 같은 방향으로 회전하지 않는다.

### 락온 Transform 위치와 실제 조준점이 다를 수 있다

카메라 락온은 `ILockOnTarget.FocusPosition`을 지원하지만, `SpawnProjectileEvent.TryGetLockOnTargetPosition()`은 현재 락온 대상의 `Transform.position`을 사용한다.

대형 몬스터나 피벗이 발밑에 있는 대상에서는 다음 세 위치가 달라질 수 있다.

- 카메라가 바라보는 위치
- 무기가 향하는 위치
- 투사체가 향하는 위치

Weapon Rig를 추가하기 전에 세 시스템이 사용하는 조준점을 통일해야 한다.

### 무기만 회전하면 손에서 분리되어 보일 수 있다

한 손 무기의 작은 보정은 무기 피벗 회전만으로 처리할 수 있다. 하지만 양손 무기, 활, 장총처럼 두 손과 무기 방향의 관계가 중요한 경우 무기만 회전하면 손이 미끄러지거나 손목이 과도하게 꺾인다.

따라서 무기 종류에 따라 보정 범위를 구분해야 한다.

---

## 권장 아키텍처

### 1단계: Weapon Aim Pivot

기존 `ParentConstraint` 구조를 유지하고, 실제 무기 모델 상위에 조준 전용 피벗을 둔다.

```text
CharacterModel
└── Weapon
    └── WeaponSlot                    ParentConstraint 유지
        └── WeaponAimPivot            절차적 조준 회전
            └── WeaponModel
                └── ProjectileSpawnPoint
```

권장 책임 분리는 다음과 같다.

```text
타겟 공급자
├── 락온 타겟
├── 공격 호밍 타겟
└── 상태별 강제 타겟
        │
        ▼
공통 Aim Point Resolver
├── ILockOnTarget.FocusPosition
└── Transform 기반 fallback
        │
        ▼
Weapon Aim Controller
├── 목표 로컬 yaw/pitch 계산
├── 회전 제한
├── 스무딩
└── 상태 weight 적용
        │
        ▼
WeaponAimPivot
        │
        └── ProjectileSpawnPoint
```

`Weapon Aim Controller`와 `Aim Point Resolver`는 제안 책임명이며 현재 코드에 존재하지 않는다.

### 2단계: 상체 및 팔 IK

양손 무기까지 자연스럽게 처리해야 할 때 Unity Animation Rigging 도입을 검토한다.

```text
Animator 결과
→ 상체 Multi-Aim 보정
→ 주손 Two-Bone IK
→ 보조손 Two-Bone IK
→ WeaponAimPivot 미세 보정
→ 최종 발사 지점
```

이 단계는 패키지 추가, 리그 생성, 캐릭터별 본 매핑과 애니메이션 충돌 검증이 필요하므로 1단계와 분리한다.

---

## 조준점 해석 규칙

Weapon Rig와 투사체는 동일한 월드 좌표를 조준해야 한다.

권장 우선순위:

1. 현재 공격 상태가 보유한 강제 타겟 또는 호밍 타겟
2. 카메라 락온 타겟
3. 적 스킬이 보유한 타겟 위치
4. 캐릭터 또는 무기 정면 fallback

락온 대상이 `ILockOnTarget`을 구현하면 `FocusPosition`을 사용한다. 구현하지 않은 대상은 현재 카메라 락온의 fallback 정책과 같은 방식으로 중심 높이를 보정해야 한다.

공통 조준점이 필요한 소비자는 다음과 같다.

| 소비자 | 사용 목적 |
|--------|-----------|
| Weapon Aim | 무기 모델과 발사 지점 방향 |
| Projectile | 초기 이동 방향과 회전 |
| Attack Rotation | 캐릭터 본체의 큰 방향 전환 |
| Camera Lock-On | 화면 포커스 |

조준점 계산을 각 소비자가 개별 구현하면 대형 몬스터, 공중 적, 다중 락온 포인트에서 다시 불일치가 발생한다.

---

## 자연스러운 회전 처리

### 큰 회전과 작은 회전 분리

캐릭터 본체는 큰 yaw 차이를 처리하고, Weapon Rig는 남은 작은 yaw와 pitch 오차만 처리하는 것이 안전하다.

```text
타겟 방향
├── 큰 수평 각도: PlayerAttackState / MovementController가 본체 회전
└── 제한된 잔여 각도: WeaponAimPivot가 시각적 보정
```

초기 권장 범위:

| 항목 | 권장값 |
|------|--------|
| Yaw 제한 | `-35° ~ +35°` |
| Pitch 제한 | `-25° ~ +45°` |
| 조준 진입 시간 | `0.08 ~ 0.15초` |
| 조준 해제 시간 | `0.15 ~ 0.25초` |
| 본체 회전 유도 기준 | yaw 오차가 Weapon Rig 제한에 접근할 때 |

실제 값은 캐릭터 체형, 무기 길이, 공격 애니메이션에 따라 조정한다.

### 로컬 공간에서 각도 제한

월드 회전을 직접 clamp하면 캐릭터가 회전할 때 무기 축이 튈 수 있다. 다음 순서로 로컬 목표 회전을 계산해야 한다.

```text
aimDirectionWorld = aimPoint - pivotPosition
aimDirectionLocal = characterRoot.InverseTransformDirection(aimDirectionWorld)
localYaw / localPitch 계산
각도 제한
기준 로컬 회전과 합성
현재 회전에서 목표 회전으로 보간
```

### 스무딩

단순 `Quaternion.Slerp(current, target, deltaTime * speed)`는 프레임 의존성은 낮지만 정확한 도달 시간이 불명확하다. 기존 프로젝트의 카메라 포커스처럼 시간 기반 응답을 조절하려면 다음 방식이 적합하다.

- yaw/pitch 각각 `Mathf.SmoothDampAngle`
- 또는 프레임 독립 지수 감쇠 계수
- 진입과 해제에 서로 다른 smooth time 적용

타겟 전환 시 즉시 새 방향으로 스냅하지 않고 현재 각도와 각속도를 유지한 채 새 목표로 보간해야 한다.

---

## 상태별 적용 정책

Weapon Rig를 항상 활성화하면 이동, 회피, 피격, 근접 공격 애니메이션의 실루엣을 훼손할 수 있다.

권장 활성 정책:

| 상태 | 권장 weight |
|------|-------------|
| 원거리 차지 | 높음 |
| 원거리 조준 유지 | 높음 |
| 투사체 발사 직전 | 높음 |
| 투사체 발사 후 회수 | 점진적 감소 |
| 근접 공격 | 기본 비활성 또는 공격별 낮은 값 |
| 이동/Idle | 비활성 |
| Dodge/Hit/Death | 즉시 또는 빠르게 비활성 |
| 발도/납도 | 비활성 |

MotionSet 타임라인과 연동할 경우 공격 전체가 아니라 조준이 필요한 구간에서만 weight를 올리는 방식이 안전하다.

---

## 무기 유형별 적용 범위

| 무기 유형 | 1단계 Pivot 적용 | 추가 요구 |
|-----------|------------------|-----------|
| 한손 총기/마법 도구 | 적합 | 손목 왜곡만 확인 |
| 스태프 | 작은 각도에 적합 | 큰 pitch는 상체 보정 필요 |
| 활 | 제한적 | 활 손과 시위 손 IK 필요 |
| 장총 | 제한적 | 양손 IK와 어깨 보정 필요 |
| 창 | 공격별 적용 | 근접 모션 실루엣 보존 필요 |
| 검/카타나 | 기본 비활성 권장 | 투사체형 검기 발사 구간만 선택 적용 |
| 쌍검 | 개별 피벗 주의 | 좌우 무기 동기화 정책 필요 |

---

## 구현 단계

### P0 — 조준점 통일

- 락온 대상의 `Transform.position`과 `ILockOnTarget.FocusPosition` 사용 차이를 해소한다.
- Weapon Aim과 `SpawnProjectileEvent`가 같은 조준점 해석 함수를 사용하도록 책임을 분리한다.
- 공중 적과 대형 몬스터에서 조준점이 일치하는지 검증한다.

### P1 — 단일 Weapon Aim Pivot

- 한 손 원거리 무기 하나를 기준으로 피벗 구조를 추가한다.
- 로컬 yaw/pitch 제한과 진입/해제 스무딩을 구현한다.
- 락온 타겟 전환, 타겟 사망, 거리 이탈 시 복귀를 처리한다.

### P2 — 공격 상태 연동

- 원거리 차지와 발사 구간에서만 weight를 활성화한다.
- `PlayerAttackState`의 호밍 타겟과 Weapon Aim 타겟을 일치시킨다.
- 발사 프레임에서 무기 방향과 투사체 방향의 각도 오차를 확인한다.

### P3 — 무기 데이터화

캐릭터 또는 무기 종류별로 다음 설정을 외부화한다.

- 조준 허용 여부
- yaw/pitch 제한
- 진입/해제 시간
- 로컬 aim axis와 up axis
- 발사 지점
- 양손 IK 필요 여부

현재 `WeaponDefinitionSO` 확장 또는 별도 Weapon Aim 설정 데이터 도입을 검토할 수 있다. 실제 필드 구조는 구현 시 기존 무기 데이터 책임과 함께 결정한다.

### P4 — Animation Rigging 도입 검토

- 활 또는 장총 캐릭터를 기준으로 프로토타입을 만든다.
- 상체 Aim과 양손 IK의 평가 순서를 검증한다.
- Animancer 레이어, Root Motion, KCC 상태 회전과 충돌하지 않는지 확인한다.
- 성능과 캐릭터별 셋업 비용이 허용될 때 전체 적용한다.

---

## 프리팹 셋업 기준

1단계 구현 시 권장 계층:

```text
Weapon
└── <WeaponType Slot>
    ├── ParentConstraint
    └── WeaponAimPivot
        └── WeaponPrefab
            └── ProjectileSpawnPoint
```

주의 사항:

- 기존 `ParentConstraint` source 순서 규칙을 변경하지 않는다.
- `WeaponAimPivot`은 손/수납 전환이 완료된 결과 위에서 회전해야 한다.
- 무기 프리팹의 forward 축과 실제 발사 축을 확인한다.
- 모델별 축이 다르면 코드에서 임의 보정하지 말고 데이터 또는 피벗 Transform으로 명시한다.
- `ProjectileSpawnPoint`는 Weapon Aim의 자식이어야 최종 회전을 그대로 상속한다.

---

## 검증 체크리스트

### 기능

- [ ] 락온 대상이 없으면 무기가 기준 자세로 복귀한다.
- [ ] 락온 대상 전환 시 무기가 스냅하지 않고 새 방향으로 보간한다.
- [ ] 대상 사망 또는 거리 이탈 시 null 참조 없이 복귀한다.
- [ ] 공중 적을 향할 때 pitch가 정상 적용된다.
- [ ] 대형 몬스터의 `FocusPosition`을 바라본다.
- [ ] 발사 시 무기 forward와 투사체 초기 방향이 일치한다.

### 애니메이션

- [ ] 손목이 허용 범위를 넘어서 꺾이지 않는다.
- [ ] 무기가 손에서 미끄러져 보이지 않는다.
- [ ] 발도/납도 중 Aim 보정이 개입하지 않는다.
- [ ] 회피, 피격, 사망 모션에서 즉시 또는 빠르게 해제된다.
- [ ] Root Motion 회전과 Weapon Aim 회전이 서로 진동하지 않는다.

### 카메라 및 전투

- [ ] 카메라 포커스와 무기 조준점이 일치한다.
- [ ] `PlayerAttackState`의 호밍 대상과 무기 조준 대상이 일치한다.
- [ ] 소프트 타겟 보정과 하드 락온의 우선순위가 명확하다.
- [ ] 타겟이 캐릭터 뒤쪽에 있을 때 무기만 과도하게 뒤집히지 않는다.

---

## 주의 사항

### `LateUpdate`만으로 해결하지 않는다

Animator 평가 후 Transform을 직접 회전하는 방식은 빠른 프로토타입에는 사용할 수 있지만, 다른 constraint 및 IK와 평가 순서가 충돌할 수 있다. 구현 시 무기 부착 constraint, Animator, Weapon Aim 적용 순서를 명시해야 한다.

### 캐릭터 본체 회전을 대체하지 않는다

Weapon Rig는 제한된 시각적 보정이다. 타겟이 옆이나 뒤에 있을 때 무기만 회전시키면 자세가 비정상적으로 보인다. 큰 수평 오차는 기존 상태 회전 로직이 처리해야 한다.

### 모든 근접 공격에 자동 적용하지 않는다

근접 공격 애니메이션은 무기 궤적과 실루엣이 공격 판정 및 타격감에 직접 연결된다. 지속적인 타겟 조준을 적용하면 의도한 휘두르기 궤적이 변할 수 있으므로 공격별 opt-in 정책이 필요하다.

### 공통 조준점 없이 기능을 확장하지 않는다

카메라, 무기, 투사체가 서로 다른 타겟 위치를 계산하는 상태에서 IK를 먼저 추가하면 시각적 오차가 더 크게 드러난다. P0 조준점 통일을 먼저 완료해야 한다.

---

## 결론

현재 프로젝트의 Weapon 시스템은 무기 생성과 손/수납 소켓 전환에는 적합하지만 타겟 조준 책임은 포함하지 않는다.

우선 적용 순서는 다음과 같다.

1. `ILockOnTarget.FocusPosition` 기반 공통 조준점 통일
2. 기존 `ParentConstraint` 하위에 제한된 `WeaponAimPivot` 추가
3. 공격 상태와 조준 weight 연동
4. 한 손 원거리 무기로 검증
5. 양손 무기가 필요할 때 Animation Rigging과 팔 IK 도입

이 순서는 기존 무기 장착 구조와 공격 애니메이션을 최대한 보존하면서, 무기와 투사체가 같은 타겟을 자연스럽게 향하도록 확장하는 경로다.
