# 액터 투사체 시스템 고도화 설계서

> 작성일: 2026-07-25
> 대상 버전: Unity 6 (6000.0.60f1), URP
> 상태: 설계 (미구현)
> 관련 문서: `Assets/docs/guide/TARGETED_PROJECTILE_AOE_GUIDE.md`, `Assets/docs/TODO/GAMEPLAY_ABILITY_SYSTEM_SPEC.md`, `Assets/docs/TODO/HIT_REACTION_ADVANCEMENT_DESIGN.md`

---

## 1. 개요

현재 투사체 시스템은 `MotionEvent_SpawnProjectile`이 프리팹을 `Instantiate`하고, `BaseProjectile` 상속 3종(`LinearProjectile` / `AOEProjectile` / `ArcingProjectile`)이 각자 이동과 충돌을 처리하는 구조다. 타게팅(락온/스킬 대상/텔레그래프)까지는 이미 정리되어 있으나, **데이터 소스·수명 관리·전투 규칙 정합성·확장성** 네 축에서 구조적 한계가 있다.

이 문서는 웹 레퍼런스 조사 결과와 현재 코드 감사 결과를 근거로 고도화 방향을 설계한다.

### 목표

- 투사체 공격 수치의 단일 소스를 `AbilitySetSO`(→ `AbilityAttackInfo` / `HitPhaseData`)로 일치시킨다.
- 이동/충돌/온히트 동작을 **상속이 아니라 조합**으로 확장한다(유도·관통·반사·분열·기폭).
- 풀링과 일괄 업데이트로 스폰 비용과 GC를 제거한다.
- 히트스톱·슬로우모션 등 로컬 타임스케일과 투사체 시뮬레이션을 일치시킨다.
- 저작(MotionSet 이벤트)·디버깅(기즈모/텔레메트리) 도구를 붙인다.

### 비목표

- 네트워크 예측/랙 보상. 본 프로젝트는 싱글플레이이므로 결정론적 리플레이·롤백은 도입하지 않는다.
- DOTS/ECS 전면 전환, `DrawMeshInstanced` 기반 렌더 파이프 교체. 탄막 슈팅 규모(수천 발)가 아니므로 과설계다. (Phase 4의 선택 항목으로만 남긴다.)
- 기존 `AttackData` / `IDamageable.ReceiveHit` 피해 전달 경로 교체. 유지한다.

---

## 2. 현재 구조 감사

### 2.1 구조도

```
MotionSetAsset
└── SpawnProjectileEvent (Data.Event)
        ├── 타겟 위치 해석 (ProjectileTargetMode 5종)
        ├── GameObject.Instantiate(projectilePrefab)
        └── BaseProjectile.Initialize(pos, dir, damage, speed, owner, duration, layer, fxName)
                ├── AttackData 신규 생성 (damage/direction/reactionType=Hit/isProjectile=true)
                └── Update() → UpdateMovement()
                        ├── LinearProjectile : SphereCast(previous→current)
                        ├── ArcingProjectile : 포물선 보간 + SphereCast
                        └── AOEProjectile    : OverlapSphere + 대상별 damageCooldown
```

### 2.2 확인된 결함

| # | 문제 | 근거 |
|---|------|------|
| D1 | **풀링 부재.** 발사마다 `Instantiate`, 소멸 시 `Destroy(gameObject)`. FX도 동일(`GameObjectManager.FX.cs:92`에서 `Instantiate` 후 만료 리스트로 `Destroy`). 다발 투사체 보스 패턴에서 GC 스파이크와 프레임 히칭 요인. | `BaseProjectile.cs:193`, `MotionEvent_SpawnProjectile.cs:112` |
| D2 | **공격 수치가 Ability 단일 소스를 우회.** 데미지는 MotionEvent 인스펙터의 `float damage` 하나뿐. `poiseDamage`, `breakDamage`, `reactionType`, `defenseType`, `reactionData`, `hitPhaseIndex`, `criticalMultiplier`, `forceReaction` 등 `AttackData`의 나머지 필드가 전부 기본값으로 남는다. → **모든 원거리 공격은 강인도 피해 30 고정 / 항상 `Hit` 반응 / 넉백·다운 불가.** CLAUDE.md의 "AbilitySetSO가 단일 소스" 규칙과 충돌. | `BaseProjectile.cs:53-59`, `AttackData.cs:15-66` |
| D3 | **방어 타입 모순.** `defenseType`은 기본 `Parryable`인데 `isProjectile=true`라 패리가 성립하지 않는다. 코드에도 TODO로 남아 있다. 원거리 `Unblockable` 표현 불가. | `BaseProjectile.cs:49-52` |
| D4 | **로컬 타임스케일 무시.** 투사체는 `Time.deltaTime`을 직접 쓴다. 액터는 `ActorTimeScale`을 갖고 히트스톱/부분 슬로우모션(`SetGlobalTimeScaleExceptPlayer`)을 적용받으므로, **연출 중 투사체만 정상 속도로 날아간다.** | `BaseProjectile.cs:110`, `LinearProjectile.cs:49`, `GameObjectManager.cs:101` |
| D5 | **AOE는 CCD가 없다.** `LinearProjectile`/`ArcingProjectile`은 이전→현재 SphereCast로 터널링을 막지만, `AOEProjectile`은 확장 중일 때만 `OverlapSphere`. 저프레임에서 확장 링이 대상을 건너뛴다. 또한 SphereCast는 첫 히트 1개만 처리하므로 관통·다중 히트 불가(`SphereCastAll` 아님). | `LinearProjectile.cs:79`, `AOEProjectile.cs:139-146` |
| D6 | **상속 기반 확장.** 유도·관통·튕김·분열·기폭·부착을 추가할 때마다 `BaseProjectile` 파생 클래스가 늘고 조합(유도+관통)이 불가능하다. `ProjectileType` enum도 클래스와 1:1로 묶여 함께 증식한다. | `ProjectileType.cs`, 파생 3종 |
| D7 | **소멸 연출 없음.** `Deactivate()`가 즉시 `Destroy`. 트레일 파티클이 잘리고, 착탄 FX는 AOE만 재생한다(`hitEffectKey`가 `LinearProjectile`에서는 소비되지 않음). | `BaseProjectile.cs:186-194` |
| D8 | **상호작용 부재.** 투사체 반사(패링으로 되돌리기), 투사체끼리 상쇄, 방패/엄폐물에 박히기 같은 상호작용 개념이 없다. 소유권 전환 API 자체가 없다. | `BaseProjectile.OnHit` |
| D9 | **스폰이 1발 고정.** `SpawnProjectileEvent`는 인스턴스 1개만 만든다. 샷건/부채꼴/링/연사 패턴은 이벤트를 N개 배치해야 하며 타이밍 시프트를 저작할 수 없다. 다중 대상 AOE도 불가(가이드 문서에서도 확장 포인트로 명시). | `MotionEvent_SpawnProjectile.cs:112`, 가이드 194행 |
| D10 | **디버깅 부재.** `OnDrawGizmosSelected`만 있어 선택하지 않으면 궤적이 보이지 않고, `DebugGizmoManager` 채널과 연동되지 않는다. 발사/명중/소멸 텔레메트리도 없다. | `LinearProjectile.cs:86`, `AOEProjectile.cs:231` |
| D11 | **`Invoke(nameof(OnExpire), 0.5f)` 사용.** 문자열 기반 지연 호출로, 풀링 전환 시 재사용된 인스턴스에 이전 Invoke가 살아남는 위험이 있다. | `AOEProjectile.cs:163` |
| D12 | **ArcingProjectile이 speed를 무시.** 비행 시간이 `lifeTime`에 완전 종속이라, 거리와 무관하게 항상 `duration`만큼 걸린다. 원거리/근거리 착탄 타이밍 저작 불가. | `ArcingProjectile.cs:69` |

---

## 3. 레퍼런스 조사

| 출처 | 채택할 아이디어 | 본 설계 반영 |
|------|-----------------|--------------|
| Photon Fusion 2 – Projectiles | 투사체를 **hitscan / kinematic / beam**으로 분류. 시뮬레이션 데이터와 비주얼 인스턴스를 분리(`GetFireData` + `Render`). 시뮬 원점(카메라·루트)과 시각 원점(무기 총구)이 다른 문제를 **배럴→실제 경로 보간**으로 해소. | §4.2 이동 전략 분류, §4.6 스폰 오프셋 보간 |
| Unreal `ProjectileMovementComponent` / Godot Projectile Component | 이동 컴포넌트가 **bounce·homing strength·max penetrations**를 데이터로 노출. | §4.3 Behavior 모듈 파라미터 |
| Path of Exile 투사체 규칙 | 관통 → 연쇄 → 분열의 **처리 순서를 규칙으로 고정**. 순서를 정의하지 않으면 조합이 폭발한다. | §4.4 온히트 파이프라인 순서 |
| Unity 매뉴얼 / `UnityEngine.Pool` | `ObjectPool<T>` 사용, **실제 플레이 피크값으로 defaultCapacity 설정**, 재사용 시 velocity/오디오/애니메이션/이벤트 구독까지 **리셋 계약**이 없으면 풀링이 버그 소스가 된다. `CountActive/CountAll/CountInactive` 지표로 검증. | §4.5 풀링, §7 리스크 |
| 배치 레이캐스트 / Job + Burst (`RaycastCommand`, `TransformAccessArray`) | 투사체 이동·충돌을 개별 `Update`가 아니라 **매니저 일괄 처리**로. 대량 투사체에서 6배 수준 성능차 보고. | §4.5 ProjectileManager 일괄 업데이트, Phase 4 |
| 고속 이동체 터널링 일반론 | 물리 콜라이더 신뢰 대신 **이전→현재 스윕 + 서브스텝**. | §4.2 CCD |

---

## 4. 설계

### 4.1 전체 구조

```
AbilitySetSO
└── GameplayAbilitySO.Variant
        └── UPlayGroundMotionAbilityPayloadSO
                └── AbilityAttackInfo.hitPhases[i] (HitPhaseData)
                        └── projectileDef : ProjectileDefinitionSO   ← 신규 참조
                                 │
MotionSetAsset ── SpawnProjectileEvent (저작: 스폰 포인트/타겟 모드/패턴/hitPhaseIndex)
                                 │
                                 ▼
                   ProjectileSpawnRequest (struct)
                                 │
                   IProjectileService.Spawn(request)          ← Contracts
                                 │
                   ProjectileManager (풀 + 일괄 틱)
                                 │
                   ProjectileRuntime (단일 MonoBehaviour, 풀링 대상)
                        ├── IProjectileMotion      이동 전략 1개
                        ├── IProjectileBehavior[]  온틱/온히트 모듈 N개
                        └── ProjectileHitResolver  AttackData 구성 + 피드백
```

핵심 전환은 **"프리팹 = 클래스"에서 "프리팹 = 비주얼, SO = 동작"** 으로의 이동이다. `ProjectileRuntime` 하나가 모든 투사체를 담당하고, 동작 차이는 SO가 들고 있는 전략/모듈 조합으로 표현한다.

### 4.2 이동 전략 (`IProjectileMotion`)

`[SerializeReference]` 다형 클래스로 정의해 `ProjectileDefinitionSO`에 인라인 직렬화한다(FlowGraph 노드와 동일 패턴). `[MovedFrom]` 유지 규칙을 그대로 적용한다.

| 전략 | 대체 대상 | 주요 파라미터 |
|------|-----------|---------------|
| `LinearMotion` | `LinearProjectile` | speed, acceleration, maxSpeed, speedCurve |
| `ArcMotion` | `ArcingProjectile` | arcHeight, progressCurve, **flightTimeMode(Speed/Fixed)** ← D12 해소 |
| `HomingMotion` | 신규 | turnRate(deg/s), homingStrengthCurve(시간), activationDelay, maxTrackAngle, 타깃 소실 시 폴백 |
| `StationaryMotion` | `AOEProjectile` 장판 | 지면 부착, 지속 |
| `OrbitMotion` | 신규(보스 패턴) | 중심 트랜스폼, 반경, 각속도 |
| `HitscanMotion` | 신규 | 즉시 스윕 + 트레일 비주얼만 재생 (텔레그래프 후 즉발 광선) |

**CCD 규칙:** 매 틱 이동량이 `collisionRadius * 0.75f`를 넘으면 서브스텝으로 분할해 스윕한다. 스윕은 `SphereCastNonAlloc`을 사용해 **관통 처리를 위한 다중 히트**를 거리순 정렬해 소비한다(D5). AOE 확장 링도 이전 반경→현재 반경 사이를 대상 판정 대상으로 삼는다.

**시간 축:** 모든 전략은 `Time.deltaTime`이 아니라 `ProjectileRuntime.DeltaTime`을 사용한다. 이 값은 owner 액터의 로컬 타임스케일을 상속하되, **소유자 사망/소멸 후에는 스폰 시점의 스케일 소스에서 분리**해 전역 스케일을 따른다(D4). 히트스톱 중 플레이어 투사체가 함께 멈추는지는 체감 검증 후 `inheritOwnerTimeScale` 플래그로 조정한다.

### 4.3 동작 모듈 (`IProjectileBehavior`)

이동과 직교하는 부가 동작. 조합 가능하며, 각 모듈은 `OnSpawn / OnTick / OnHit / OnExpire` 훅을 가진다.

| 모듈 | 설명 |
|------|------|
| `PierceBehavior` | maxPierce, 관통마다 데미지 배율 감쇠 |
| `BounceBehavior` | maxBounce, 반사면 법선 기반 방향 갱신, 벽 레이어 지정 |
| `SplitBehavior` | 히트/만료 시 자식 투사체 N개 생성(각도 분산, 자식 Definition 참조) |
| `DetonateBehavior` | 만료·히트 시 AOE 판정으로 승격 (기존 `AOEProjectile` 폭발 흐름 흡수) |
| `AttachBehavior` | 피격 대상/벽에 박혀 잔존, 이후 기폭 |
| `ReflectableBehavior` | 패리 성공 시 소유권 전환 + 방향 반전(D8). `AttackDefenseType`/패리 판정과 연동 |
| `AreaTickBehavior` | 대상별 재피격 쿨타임 틱 피해(기존 `damageCooldown` 흡수) |
| `LifeStealBehavior` 등 | 게임플레이 확장 여지 |

**온히트 파이프라인 순서(고정):**

```
스윕 히트 수집 → 소유자·이미 맞은 대상 필터 → 피해 적용
    → Pierce 판정 (계속 비행?)
        → Bounce 판정 (방향 갱신?)
            → Split 판정 (자식 생성)
                → Detonate 판정 (AOE 승격)
                    → 소멸 or 계속
```

이 순서는 PoE의 pierce→chain→split 규칙을 따른다. 순서를 데이터로 노출하지 않는다(조합 폭발 방지).

### 4.4 전투 데이터 정합 (D2/D3 해소)

`ProjectileDefinitionSO`는 수치를 직접 소유하지 않고, **스폰 시점의 `HitPhaseData`에서 `AttackData`를 구성**한다.

```csharp
public struct ProjectileSpawnRequest
{
    public ProjectileDefinitionSO definition;
    public GameActor owner;
    public Vector3 origin;
    public Vector3 direction;
    public Vector3? targetPosition;
    public Transform targetTransform;   // Homing용
    public int hitPhaseIndex;           // AbilityAttackInfo.hitPhases 인덱스
    public LayerMask hitLayers;
    public float damageScale;           // 분열 자식 등 파생 배율
}
```

`AttackData` 구성은 근접 경로(`EnemyCombat.CheckMeleeAttackHit`, `PlayerCombat`)와 **같은 헬퍼를 공유**한다. 즉 `poiseDamage`, `breakDamage`, `reactionType`, `reactionData`, `defenseType`, `criticalMultiplier`, `forceReaction`, `victimForcedMotionSlot`, `guaranteedReaction`이 원거리 공격에도 그대로 전달된다.

- `defenseType`은 `AbilityAttackInfo`에서 그대로 전달한다. 원거리 `Unblockable` 표현이 가능해진다.
- `isProjectile=true`는 유지하되, **`ReflectableBehavior`가 붙은 투사체는 패리 시 반사 처리**로 분기한다(피해 무효화가 아니라 소유권 전환).
- MotionEvent의 `float damage`는 **레거시 폴백**으로만 남기고(`hitPhaseIndex < 0`), 신규 저작에서는 사용 금지. 마이그레이션 완료 후 제거한다.

### 4.5 수명 관리와 성능

**`ProjectileManager`** (신규 매니저, `GameManager` 등록 순서상 `GameObjectManager` 이후 / `GameCombatManager` 이전).

- `UnityEngine.Pool.ObjectPool<ProjectileRuntime>`을 Definition별로 보유. `defaultCapacity`는 Definition에 저작(`prewarmCount`)하고, 씬 로드 시 프리워밍한다.
- **리셋 계약:** `ProjectileRuntime.OnReturnedToPool()`에서 위치·회전·스케일·트레일·오디오·`_hitTargets`·모듈 상태·코루틴/Invoke를 전부 초기화한다. `Invoke(nameof(...))`는 제거하고 매니저 타이머로 대체한다(D11).
- 개별 `Update()` 제거. 매니저가 활성 리스트를 한 번에 순회한다(호출 오버헤드 + 스크립트 순서 예측 가능성).
- 소멸 연출: 반환 전 `detachOnDeath` 트레일을 분리해 파티클 수명만큼 유지한 뒤 회수한다(D7). 착탄 FX는 전 타입 공통으로 재생한다.
- 진단: `CountActive / CountAll / CountInactive`를 디버그 오버레이에 노출하고, **활성 상한**(예: 256)을 넘으면 가장 오래된 것부터 강제 회수 + 경고 로그.

### 4.6 스폰 파이프라인과 패턴 (D9 해소)

`SpawnProjectileEvent`를 다음으로 재편한다.

- 타겟 해석 로직(현행 `ProjectileTargetMode` 5종)은 **유지**한다. 이미 정리된 자산이다.
- 스폰 개수/배치를 `ProjectileSpawnPattern`(`[SerializeReference]`)으로 분리:
  - `SingleShot`, `FanShot`(count, spreadAngle), `RingShot`(count), `BurstShot`(count, interval), `MultiTargetShot`(대상 목록 순회 — 가이드의 다중 대상 AOE 확장 포인트).
- **총구→경로 보간(Fusion 채택):** 시각 스폰 위치(무기 본)와 논리 스폰 위치(액터 루트/타겟 라인)가 다를 때, 초반 `barrelBlendTime` 동안 시각 위치를 논리 경로로 보간한다. 카메라 정면 조준과 무기 위치가 어긋나는 원거리 캐릭터에서 필수.
- 텔레그래프 위치 예약(`TelegraphPosition`)과 실제 판정 위치 일치는 현행 규칙을 유지한다.

### 4.7 저작·디버그 도구 (D10 해소)

- **MotionSetEditor 프리뷰:** 전투 오버레이 트랙에 투사체 스폰 마커와 예상 궤적(직선/포물선/유도 근사)을 그린다. 기존 히트박스 기즈모 오버레이와 같은 계층에 배치.
- **DebugGizmoManager 채널 추가:** `Projectile` 채널에서 활성 투사체의 스윕 캡슐, 실제 히트 지점, AOE 반경을 상시 표시(선택 여부 무관).
- **텔레메트리:** 발사 수 / 명중 수 / 만료 수 / 평균 비행 시간 / 프레임 최대 동시 활성 수를 `CycleTelemetrySession`에 기록. 원거리 공격 밸런싱(명중률)의 근거 데이터.
- **검증 테스트(EditMode):** Definition의 전략·모듈 조합 유효성(예: `HitscanMotion` + `BounceBehavior` 금지), `hitPhaseIndex` 범위, 풀 프리워밍 수치 검사.

---

## 5. 단계 계획

각 단계는 독립적으로 머지 가능하며, 이전 단계 없이는 다음 단계를 시작하지 않는다.

### Phase 0 — 정합성 긴급 수정 (기존 구조 유지)

기존 3클래스 구조를 그대로 두고 버그성 결함만 제거한다. 이후 단계의 회귀 기준선을 만든다.

- D2/D3: `Initialize` 시그니처에 `AttackData` 템플릿(또는 `HitPhaseData`)을 받도록 확장. MotionEvent에 `hitPhaseIndex` 추가.
- D4: 로컬 타임스케일 상속.
- D12: `ArcingProjectile` speed 기반 비행 시간.
- D11: `Invoke` 제거.
- 산출물: 원거리 공격이 넉백/다운/강인도 피해를 낼 수 있음. 히트스톱 중 투사체 정지.

### Phase 1 — 런타임 통합과 풀링

- `ProjectileRuntime` + `IProjectileMotion` 도입, 기존 3클래스를 `Linear/Arc/Stationary` 전략으로 이관.
- `ProjectileManager` + `ObjectPool` + 일괄 틱 + 리셋 계약.
- `ProjectileDefinitionSO` 신설. `CreateAssetMenu`는 규약대로 `UPlayGround/Projectile/Projectile Definition`.
- 기존 프리팹은 Definition 참조로 마이그레이션(에디터 도구 1회성 스크립트, 실행 후 삭제).
- 산출물: 스폰 GC 0, 동시 100발 안정.

### Phase 2 — 동작 모듈

- `IProjectileBehavior` + 온히트 파이프라인 + `Pierce/Bounce/Split/Detonate/AreaTick`.
- `AOEProjectile`의 틱 피해·감쇠·지면 부착을 `AreaTickBehavior`/`StationaryMotion`으로 완전 흡수 후 클래스 제거.
- 산출물: 관통 화살, 튕기는 마법탄, 분열 폭탄을 코드 추가 없이 저작.

### Phase 3 — 상호작용과 패턴

- `HomingMotion`, `ReflectableBehavior`(패리 반사 + 소유권 전환), `AttachBehavior`.
- `ProjectileSpawnPattern` 5종, 총구→경로 보간.
- 디버그 채널 + 텔레메트리 + MotionSetEditor 궤적 프리뷰.
- 산출물: 보스 탄막 패턴, 되받아치기 상호작용.

### Phase 4 — 성능 티어 (선택)

동시 활성 수가 300발을 넘는 콘텐츠가 실제로 생겼을 때만 착수한다.

- 이동·스윕을 `RaycastCommand` + Job/Burst 배치로 전환.
- 비주얼을 GameObject에서 인스턴싱 렌더로 분리(시뮬/렌더 분리는 Phase 1 구조가 이미 허용).

---

## 6. 모듈 경계

| 항목 | 소속 asmdef | 비고 |
|------|-------------|------|
| `ProjectileDefinitionSO`, `IProjectileMotion`, `IProjectileBehavior`, `ProjectileSpawnRequest` | `UPlayGround.Data` | 순수 데이터/전략 정의 |
| `IProjectileService` | `UPlayGround.Contracts` | `Svc.Projectile`로 노출 |
| `ProjectileRuntime`, `ProjectileHitResolver` | `UPlayGround.Actor` | `IDamageable`, `AttackData` 의존 |
| `ProjectileManager` | `UPlayGround.Manager` | `IGameService` 구현, `GameManager` 등록 |
| `SpawnProjectileEvent` | 현행 유지 (`Data.Event`) | `[MovedFrom]` 필수 |

`[SerializeReference]` 클래스를 이동할 경우 `[MovedFrom(true, sourceAssembly: "...")]`를 반드시 유지한다. 누락 시 저작된 MotionSet 이벤트와 VFX 참조가 역직렬화되지 않는다.

---

## 7. 리스크와 함정

| 리스크 | 대응 |
|--------|------|
| **풀 재사용 상태 누수.** 이전 인스턴스의 트레일, 히트 목록, 모듈 상태, 코루틴이 남아 유령 피해를 준다. | `OnReturnedToPool` 리셋 계약을 인터페이스로 강제하고, EditMode 테스트로 "회수 후 필드 기본값" 검증. 모듈은 상태를 `ProjectileRuntime`이 소유한 구조체 슬롯에 둔다. |
| **`[SerializeReference]` 마이그레이션 손실.** 프리팹→Definition 전환 중 기존 저작값 유실. | 마이그레이션 전 프리팹 값을 JSON으로 덤프해 백업, 전환 후 diff 검증. 일회성 도구는 완료 후 제거(CLAUDE.md 규칙). |
| **로컬 타임스케일 상속의 체감 역효과.** 적 투사체가 히트스톱마다 멈추면 회피 타이밍이 흔들린다. | `inheritOwnerTimeScale`을 Definition 플래그로 두고, 기본값은 "플레이어 투사체만 상속"으로 시작해 체감 검증. |
| **관통 도입으로 인한 다중 히트 피드백 스팸.** 히트스톱·카메라 흔들림이 관통 수만큼 중첩. | AOE의 `_impactFeedbackApplied` 선례를 따라 **투사체당 임팩트 연출 1회** 규칙을 파이프라인에 고정. |
| **온히트 파이프라인 무한 재귀.** Split이 Split을 낳는다. | 스폰 요청에 `generation` 카운터를 넣고 상한(기본 2) 초과 시 무시 + 경고. |
| **AbilitySet 참조 순환.** `HitPhaseData`가 `ProjectileDefinitionSO`를, Definition의 Split이 다시 Ability를 참조. | Split 자식은 Ability가 아니라 **Definition만** 참조한다(피해 수치는 부모의 `AttackData` × `damageScale` 상속). |
| **활성 수 폭증.** 분열·연사 조합으로 수천 발. | 매니저의 하드 상한 + 강제 회수 + 경고 로그. Phase 4 이전에는 상한을 낮게 유지. |

---

## 8. 테스트 체크리스트

| 구분 | 항목 | 확인 내용 |
|------|------|-----------|
| 회귀 | 기존 저작 | 현재 MotionSet의 투사체 이벤트가 이관 후에도 같은 위치·타이밍·피해로 동작 |
| 회귀 | 타겟 모드 5종 | `Forward/LockOnTarget/EnemySkillTarget/TargetPosition/TelegraphPosition` 폴백 포함 동작 |
| 회귀 | 텔레그래프 정합 | 표시 위치와 실제 판정 위치 일치 |
| 정합 | 강인도/브레이크 | 원거리 피격 시 `poiseDamage`/`breakDamage`가 근접과 같은 규칙으로 적용 |
| 정합 | 반응 타입 | `Knockback`/`Knockdown`/`Airborne` 원거리 공격이 의도대로 동작 |
| 정합 | 방어 타입 | `Unblockable` 원거리 공격이 가드로 막히지 않음 |
| 시간 | 히트스톱 | 히트스톱/슬로우모션 중 투사체 속도가 정책대로 동작 |
| 물리 | 터널링 | 고속 투사체(60+ m/s)가 얇은 벽/대상을 통과하지 않음 (30fps 강제 포함) |
| 물리 | AOE 확장 | 저프레임에서 확장 링이 대상을 건너뛰지 않음 |
| 물리 | 관통 | `maxPierce` 초과 시 정확히 소멸, 대상별 중복 피해 없음 |
| 풀링 | 재사용 | 100회 발사/회수 후 `CountAll`이 피크 이하로 유지, GC Alloc 0 |
| 풀링 | 상태 누수 | 회수 후 재발사 시 이전 히트 목록/트레일/모듈 상태가 남지 않음 |
| 풀링 | 씬 전환 | `OnSceneChanged`에서 활성 투사체 전량 회수, 참조 누수 없음 |
| 상호작용 | 반사 | 패리 시 소유권 전환 후 원 시전자를 피격 |
| 상호작용 | 자기 피격 | 시전자와 하위 콜라이더는 어떤 조합에서도 피격되지 않음 |
| 안정성 | 소유자 소멸 | 발사 후 시전자가 사망/디스폰해도 예외 없이 만료 |
| 안정성 | 재귀 상한 | Split 세대 상한 초과 시 경고 후 중단 |

---

## 9. 채택하지 않은 안

| 안 | 사유 |
|----|------|
| 네트워크 예측/랙 보상 구조 | 싱글플레이. 결정론 요구가 없다. |
| DOTS/ECS 전면 전환 | 투사체 규모가 ECS 전환 비용을 정당화하지 않는다. Phase 4의 Job 배치로 충분. |
| `Rigidbody` 물리 기반 투사체 | 고속 이동체 충돌 신뢰도가 낮고, 액션 게임의 결정적 궤적 저작과 맞지 않다. 스윕 기반 커스텀 시뮬 유지. |
| `ProjectileType` enum 확장 | 클래스 1:1 대응 enum은 조합형 구조와 충돌한다. Phase 2에서 enum 자체를 폐기하고 Definition 참조로 대체. |
| 투사체마다 별도 프리팹/클래스 유지 | D6의 근본 원인. 비주얼만 프리팹으로 남긴다. |

---

## 참고 자료

- [Fusion 2 – Projectiles (Photon Engine)](https://doc.photonengine.com/fusion/current/technical-samples/projectiles-advanced/projectiles)
- [Unity Manual – Pooling and reusing objects](https://docs.unity3d.com/6000.4/Documentation/Manual/performance-reusable-code.html)
- [Use object pooling to boost performance of C# scripts (Unity Learn)](https://learn.unity.com/course/design-patterns-unity-6/tutorial/use-object-pooling-to-boost-performance-of-c-scripts-in-unity)
- [Projectile component (Godot Essentials)](https://godot-essentials.gitbook.io/addons-documentation/components/projectile-component)
- [unreal.ProjectileMovementComponent](https://dev.epicgames.com/documentation/en-us/unreal-engine/python-api/class/ProjectileMovementComponent)
- [Projectile – Path of Exile Wiki (pierce → chain → split)](https://pathofexile.fandom.com/wiki/Projectile)
- [Unity Batch Raycasting](https://github.com/unitycoder/Unity-Batch-Raycasting)
- [Instancing Pool Demo (DrawMeshInstanced projectiles)](https://github.com/ShilohGames/InstancingPoolDemo)
