# 타게팅 투사체 AOE 가이드

> 작성일: 2026-05-14  
> 대상 버전: Unity 6 (6000.0.60f1), URP

---

## 개요

투사체 기반 범위 공격이 락온 대상, 적 AI의 현재 스킬 대상, 텔레그래프가 예약한 위치를 기준으로 생성되도록 하는 설계와 사용 가이드.

현재 투사체 시스템은 `MotionEvent_SpawnProjectile`에서 프리팹을 생성하고, `BaseProjectile`이 `AttackData`를 구성한 뒤 `IDamageable.TakeDamage()`로 피해를 전달한다. `AOEProjectile`은 자기 위치를 중심으로 `Physics.OverlapSphere`를 수행하며, 대상별 피해 쿨타임을 가진다.

핵심 방향은 다음과 같다.

- 타겟 탐색과 위치 결정은 `SpawnProjectileEvent`가 담당한다.
- `AOEProjectile`은 전달받은 중심 위치에서 범위 판정만 담당한다.
- 플레이어 락온, 몬스터 스킬 대상, 텔레그래프 예약 위치를 같은 이벤트로 처리한다.
- 기존 `AttackData` / `IDamageable` 피해 흐름은 유지한다.
- 텔레그래프가 보인 위치와 실제 AOE 판정 위치가 어긋나지 않도록 한다.

---

## 현재 구조

```
MotionSetAsset
└── SpawnProjectileEvent
        ├── projectilePrefab 생성
        ├── spawnPointName / spawnOffset 기준 스폰 위치 계산
        ├── ProjectileTargetMode 기준 타겟 위치 계산
        └── BaseProjectile.Initialize(...)
                ├── AttackData 구성
                ├── owner / hitLayers / lifeTime 설정
                └── Projectile별 UpdateMovement()
                        ├── LinearProjectile: SphereCast 충돌
                        └── AOEProjectile: OverlapSphere 범위 피해
```

### 관련 파일

| 파일 | 역할 |
|------|------|
| `Assets/02.Scripts/Data/Event/Animation/MotionEvent_SpawnProjectile.cs` | MotionSet 타임라인에서 투사체 생성, 타겟 위치 해석 |
| `Assets/02.Scripts/GameActor/Object/Projectile/BaseProjectile.cs` | 투사체 공통 초기화, `AttackData` 생성, 단일 히트 처리 |
| `Assets/02.Scripts/GameActor/Object/Projectile/LinearProjectile.cs` | 직선 이동 투사체, SphereCast 기반 충돌 |
| `Assets/02.Scripts/GameActor/Object/Projectile/AOEProjectile.cs` | 범위 투사체, OverlapSphere 기반 피해 |
| `Assets/02.Scripts/GameActor/Component/Enemy/EnemyCombat.cs` | 몬스터 현재 스킬 대상과 텔레그래프 판정 위치 제공 |
| `Assets/02.Scripts/Data/Combat/CombatData.cs` | `AttackData`, `HitPhaseData`, `AbilityAttackInfo` 정의 |

---

## 타겟 모드

`SpawnProjectileEvent`는 `ProjectileTargetMode`로 투사체의 목표 위치 정책을 결정한다.

```csharp
public enum ProjectileTargetMode
{
    Forward,
    LockOnTarget,
    EnemySkillTarget,
    TargetPosition,
    TelegraphPosition
}
```

| 값 | 설명 |
|------|------|
| `Forward` | 기존 방식. 시전자의 정면 방향으로 발사한다. |
| `LockOnTarget` | 플레이어 락온 대상 위치를 목표로 사용한다. 대상이 없으면 정면 발사로 폴백한다. |
| `EnemySkillTarget` | `EnemyCombat.SkillTargetList[0]` 위치를 목표로 사용한다. |
| `TargetPosition` | 시전자 기준의 대표 타겟 위치를 사용한다. 몬스터는 스킬 대상, 플레이어는 락온 대상을 우선한다. |
| `TelegraphPosition` | `EnemyCombat.GetCurrentAttackPosition()`을 사용한다. `useTelegraphPositionForHit`로 예약된 위치가 있으면 그 위치가 반환된다. |

---

## 런타임 흐름

### 직선 투사체

`LinearProjectile`은 기본적으로 스폰 포인트에서 생성되고, 목표 위치가 있으면 목표 방향으로 날아간다.

```
spawnPoint 위치
└── targetMode로 목표 위치 계산
        └── direction = targetPosition - spawnPosition
                └── LinearProjectile.Initialize(...)
```

락온 대상이 없거나 스킬 대상이 없으면 기존처럼 시전자 정면으로 발사한다.

### AOE 투사체

`AOEProjectile`은 목표 위치가 있으면 해당 위치에 직접 생성되거나, 생성 직후 중심 위치를 명시적으로 보정한다.

```
targetMode로 중심 위치 계산
└── projectTargetToGround가 켜져 있으면 바닥 Raycast 보정
        └── AOEProjectile.SetCenterPosition(center)
                └── CheckAOEDamage()
```

장판, 낙뢰, 폭발, 타겟 위치 예약 공격은 `AOEProjectile`을 사용한다.

---

## 셋업 방법

### 1. 플레이어 락온 대상 폭발

`SpawnProjectileEvent` 설정:

| 필드 | 값 |
|------|------|
| `projectilePrefab` | `AOEProjectile` 프리팹 |
| `targetMode` | `LockOnTarget` |
| `projectToGround` | `true` |
| `targetHitLayer` | 몬스터 피격 레이어 |
| `damage` | 공격 피해량 |
| `duration` | 장판 지속 시간 |

락온 대상이 없으면 시전자 정면 기준 위치로 폴백한다.

### 2. 몬스터 타겟 위치 장판

`EnemyCombat.SelectAndExecuteSkill()`이 현재 스킬 대상을 `SkillTargetList`에 캐싱한다. MotionSet의 `SpawnProjectileEvent`는 이 리스트의 첫 번째 대상을 사용할 수 있다.

| 필드 | 값 |
|------|------|
| `targetMode` | `EnemySkillTarget` |
| `projectToGround` | `true` |
| `targetOffset` | 필요 시 월드 오프셋 |

플레이어가 이동해도 이벤트 실행 시점의 위치에 장판이 생성된다.

### 3. 텔레그래프 위치 폭발

몬스터 공격 데이터:

| 필드 | 값 |
|------|------|
| `useTelegraph` | `true` |
| `useMotionEventTelegraph` | `true` |
| `telegraphAnchorType` | `TargetPosition` |
| `useTelegraphPositionForHit` | `true` |

MotionSet 권장 타임라인:

```
0.10s  TelegraphEvent 시작
2.10s  TelegraphEvent 종료
2.10s  SpawnProjectileEvent 실행
```

`SpawnProjectileEvent.targetMode = TelegraphPosition`으로 두면 텔레그래프가 예약한 위치에 AOE 투사체가 생성된다.

---

## 주의 사항

- `AOEProjectile`은 타겟 탐색을 직접 하지 않는 것이 원칙이다. 타겟 위치 정책은 `SpawnProjectileEvent`에 둔다.
- `TelegraphPosition`은 `EnemyCombat`의 현재 스킬과 히트 페이즈 상태에 의존한다. MotionSet 이벤트 순서가 맞아야 한다.
- 바닥 보정 레이어에 플레이어/몬스터 콜라이더가 포함되면 장판이 잘못된 높이에 붙을 수 있다.
- AOE 데미지 감쇠는 대상별로 원본 데미지에서 계산해야 한다. 누적 곱셈을 하면 뒤에 처리된 대상의 피해가 계속 낮아진다.
- 직선 투사체가 3D 방향으로 날아가야 하는 경우에는 목표 방향의 Y 값을 보존하고, 지면 평면 발사체는 수평 방향으로 정규화한다.

---

## 확장 포인트

### 투사체 데이터 ScriptableObject화

현재 `SpawnProjectileEvent`가 피해량, 속도, 지속 시간, 타겟 레이어를 직접 가진다. 공격 수가 늘어나면 다음 데이터를 별도 SO로 분리할 수 있다.

```csharp
public class ProjectileAttackDataSO : ScriptableObject
{
    public BaseProjectile projectilePrefab;
    public ProjectileTargetMode targetMode;
    public float damage;
    public float speed;
    public float duration;
    public LayerMask targetHitLayer;
}
```

### 유도 투사체

`LockOnTarget`으로 목표 Transform을 찾은 뒤 `HomingProjectile`을 추가하면 된다. 이 경우 스폰 이벤트는 목표 Transform만 주입하고, 추적 회전/속도 보정은 투사체가 담당한다.

### 다중 대상 AOE

`EnemySkillTarget`은 현재 첫 번째 대상만 사용한다. 파티원/소환수/멀티 타겟 보스 패턴이 필요하면 `SpawnProjectileEvent`가 대상 목록을 순회해 AOE 인스턴스를 여러 개 생성하는 방식으로 확장한다.

---

## 테스트 체크리스트

| 항목 | 확인 내용 |
|------|-----------|
| 기존 직선 발사 | `Forward` 모드에서 기존처럼 정면 발사된다 |
| 플레이어 락온 | 락온 대상 위치에 AOE가 생성된다 |
| 락온 없음 | 대상이 없을 때 정면 발사 또는 정면 위치로 폴백한다 |
| 몬스터 스킬 대상 | `EnemySkillTarget`이 `SkillTargetList[0]` 위치를 사용한다 |
| 텔레그래프 연동 | 표시된 장판 위치와 AOE 피해 위치가 같다 |
| 바닥 보정 | 경사/계단/지형 위에 AOE가 붙는다 |
| 데미지 감쇠 | 여러 대상이 맞아도 대상 순서에 따라 피해가 누적 감쇠되지 않는다 |
| 중복 피해 | 대상별 쿨타임이 의도대로 동작한다 |
| 자기 자신 제외 | 시전자와 하위 콜라이더가 피격되지 않는다 |
