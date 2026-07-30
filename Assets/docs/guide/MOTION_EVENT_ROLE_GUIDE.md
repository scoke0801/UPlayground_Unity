# MotionEvent 역할 가이드

> 작성일: 2026-05-16  
> 대상 버전: Unity 6 (6000.0.60f1), URP

---

## 개요

MotionEvent는 `MotionSet` 타임라인에 배치되어 애니메이션 재생 중 전투 판정, VFX/SFX, 카메라 연출, 모션 보정, 시간 제어를 실행하는 직렬화 이벤트다.

현재 구조의 핵심은 다음과 같다.

- 모든 이벤트는 `MotionEventBase`를 상속한다.
- `startTime`에 `Execute(GameObject target)`가 호출된다.
- `endTime` 이후 활성 구간에서 빠질 때 `OnCompleteEvent(GameObject target)`가 호출된다.
- `MotionEventExecutor`는 `GameActor`가 붙은 루트 오브젝트를 타겟으로 해석한다.
- 모션별 이벤트는 이전 모션들의 길이를 누적한 `globalStartTimeOffset`을 받아 글로벌 타임라인에서 실행된다.

---

## 실행 구조

```
MotionSetAsset
└── MotionSet
    ├── globalEvents
    └── motions[]
        └── events[]
            └── MotionEventBase
                ├── Execute(target)          startTime 진입
                └── OnCompleteEvent(target)  endTime 이탈

ActorAnimator
└── MotionEventExecutor
    ├── PlayMotionSet()
    ├── UpdateTime()
    ├── ProcessEvents()
    └── Stop()
```

### 기본 생명주기

| 단계 | 호출 지점 | 역할 |
|------|-----------|------|
| 재생 시작 | `MotionEventExecutor.PlayMotionSet()` | 현재 MotionSet 캐싱, 실행/활성 이벤트 초기화, 글로벌 오프셋 계산 |
| 시간 갱신 | `MotionEventExecutor.UpdateTime(time)` | 현재 시간과 이전 프레임 시간 사이에 시작된 이벤트 탐색 |
| 시작 | `MotionEventBase.Execute(target)` | 이벤트별 실제 동작 실행 |
| 활성 유지 | `_activeEvents` | 구간형 이벤트의 종료 여부 추적 |
| 종료 | `MotionEventBase.OnCompleteEvent(target)` | 판정 OFF, 카메라 복원, 무적 해제 등 구간 종료 처리 |
| 중단 | `MotionEventExecutor.Stop()` | 활성 이벤트의 `OnCompleteEvent`를 호출하고 상태 초기화 |

---

## 이벤트 카테고리

에디터의 `MotionEventAddPopup` 기준 카테고리는 다음과 같다.

| 카테고리 | 이벤트 |
|----------|--------|
| Combat | `BeginCollisionEvent`, `DisableCollisionEvent`, `ComboWindowEvent`, `FinishAttackEvent`, `InvincibilityEvent`, `HealSkillEvent` |
| VFX / SFX | `BeginParticleEvent`, `PlaySoundEvent`, `FootstepEvent`, `SpawnProjectileEvent`, `SpawnSkillEvent` |
| Camera | `CameraEffectEvent`, `CameraLookAtSocketEvent`, `FinishSideViewEvent` |
| Movement / Time | `AddForceEvent`, `MotionEvent_MotionWarp`, `AnimationSpeedEvent`, `TimeScaleEvent`, `LoopEvent` |
| Utility | `CustomCallbackEvent`, `FreezeEnemyEvent`, `HideTargetEvent` |

`CameraSnapshotSequenceEvent`도 MotionEvent 구현체지만 현재 `MotionEventAddPopup` 기본 카테고리 목록에는 포함되어 있지 않다.

---

## Combat 이벤트

| 이벤트 | 표시 이름 | 역할 | 시작 처리 | 종료 처리 | 주요 필드 |
|--------|-----------|------|-----------|-----------|-----------|
| `BeginCollisionEvent` | `Collision` | 공격 판정 구간을 연다. `hitPhaseIndex`로 현재 히트 페이즈를 Combat에 전달한다. | Player는 `PlayerCombat`, Monster는 `EnemyCombat`의 히트 타겟을 비우고 타겟 레이어와 페이즈를 설정한 뒤 충돌을 켠다. | 같은 Combat의 충돌을 끈다. | `hitPhaseIndex` |
| `DisableCollisionEvent` | `Disable Collision` | 특정 구간만 공격 판정을 명시적으로 끈다. | Player/Monster Combat 충돌을 끈다. | 충돌을 다시 켜고 히트 타겟을 초기화한다. | 없음 |
| `ComboWindowEvent` | `ComboWindow` | 플레이어 다음 콤보 입력 허용 구간을 연다. | `PlayerCombat.OpenComboWindow()` 호출 | `PlayerCombat.CloseComboWindow()` 호출 | 없음 |
| `FinishAttackEvent` | `FinishAttack` | 플레이어 피니시 공격의 실제 처형 타격 타이밍을 발생시킨다. | 현재 상태가 `PlayerFinishAttackState`이면 `FinishTarget`의 `MonsterActor.OnTakeFinishAttack()` 호출 | 없음 | 없음 |
| `InvincibilityEvent` | `Invincibility` | Player/Monster 무적 구간을 만든다. | `SetInvincible(true)` | `SetInvincible(false)` | 없음 |
| `HealSkillEvent` | `HealSkill` | 몬스터 회복 스킬을 실행한다. | `EnemyCombat.SkillTargetList` 대상에게 VFX를 표시하고 `Heal()` 호출 | 없음 | `vfxPrefabKey`, `vfxAuraPrefabKey`, `vfxLifeTime` |

### Collision 레이어 규칙

`BeginCollisionEvent`는 오너의 `GameActor.GetAttackTargetLayerMask()`를 사용한다.

| 오너 | 기본 타겟 레이어 | 우선 설정 |
|------|------------------|-----------|
| Player | `Enemy` | `ActorDefinitionSO.targetLayerMask` |
| Monster | `Player` | `ActorDefinitionSO.targetLayerMask` |

---

## VFX / SFX 이벤트

| 이벤트 | 표시 이름 | 역할 | 시작 처리 | 종료 처리 | 주요 필드 |
|--------|-----------|------|-----------|-----------|-----------|
| `BeginParticleEvent` | `Particle` | 지정 프리팹 VFX를 소켓/본 위치에 생성한다. | `particlePrefab`을 `spawnPointName` 위치에 생성하고 필요 시 부모에 부착한다. | `destroyOnFinish`가 true면 생성 인스턴스를 제거한다. | `particlePrefab`, `spawnPointName`, `offset`, `rotationOffset`, `attachToTarget`, `detachAfterSpawn`, `useSpawnRotation`, `destroyOnFinish`, `particleLifeTime` |
| `PlaySoundEvent` | `Sound` | 오디오 클립을 재생한다. | 3D 사운드는 `AudioSource.PlayClipAtPoint()`로 재생한다. 2D 사운드는 현재 로그만 출력한다. | 없음 | `audioClip`, `volume`, `is3D` |
| `FootstepEvent` | `Footstep` | 발자국 타이밍을 표시한다. | 현재는 `Debug.Log`로 왼발/오른발 출력만 수행한다. | 없음 | `foot`, `volume` |
| `SpawnProjectileEvent` | `Projectile` | 투사체 프리팹을 생성하고 방향, 속도, 지속시간, 데미지, 피격 레이어를 초기화한다. | 스폰 포인트와 타겟 모드로 발사 방향을 계산한 뒤 `BaseProjectile.Initialize()` 호출 | 없음 | `projectilePrefab`, `spawnPointName`, `spawnOffset`, `rotationOffset`, `useSpawnRotation`, `damage`, `targetMode`, `targetOffset`, `projectTargetToGround`, `groundLayerMask`, `speed`, `duration`, `targetHitLayer`, `hitParticleName` |
| `SpawnSkillEvent` | `SpawnSkill` | 스킬용 프리팹을 주변 랜덤 위치에 소환한다. | `SpawnTargetData` 목록에 따라 프리팹을 생성하고 콜라이더 겹침을 완화한다. 몬스터 소환이면 소환자 그룹에 등록한다. | 없음 | `spawnTargetList`, `resolveIterations` |

### SpawnProjectile 타겟 모드

| 모드 | 위치 결정 |
|------|-----------|
| `Forward` | 타겟 위치를 쓰지 않고 오너 정면으로 발사 |
| `LockOnTarget` | `CameraManager.GetLockOnTarget()` 위치 |
| `EnemySkillTarget` | `EnemyCombat.SkillTargetList[0]` 위치 |
| `TargetPosition` | EnemySkillTarget 우선, 실패 시 LockOnTarget |
| `TelegraphPosition` | `EnemyCombat.GetCurrentAttackPosition()` 위치 |

`SpawnProjectileEvent`의 피격 레이어는 Player/Monster 오너일 때 `GameActor.GetAttackTargetLayerMask()`를 우선 사용한다. 그 결과 일반 투사체도 `BeginCollisionEvent`와 같은 오너 타입 기반 타겟 규칙을 따른다. 오너 기반 레이어가 0이면 `targetHitLayer` 필드를 fallback으로 사용한다.

---

## Camera 이벤트

| 이벤트 | 표시 이름 | 역할 | 시작 처리 | 종료 처리 | 주요 필드 |
|--------|-----------|------|-----------|-----------|-----------|
| `CameraEffectEvent` | `Camera Effect` | `CameraEffectData` 목록을 재생한다. 흔들림, 줌, FOV, 회전, TimeScale 계열 효과에 사용한다. | `CameraManager.PlayEffect()` 호출, 필요 시 카메라 입력 잠금 | 활성 핸들을 `StopEffect()`로 중지하고 입력 잠금 해제 | `effectDataList`, `lockCameraInput` |
| `CameraLookAtSocketEvent` | `Camera LookAt Socket` | 특정 액터 소켓을 카메라 LookAt 대상으로 지정한다. | `GameActor.TryGetSocket()`으로 소켓을 찾고 `CameraManager.SetLookAtOverride()` 호출. 필요 시 방향도 스무스 전환 | LookAt override 해제, 입력 잠금 해제, 필요 시 저장한 Yaw/Pitch로 복원 | `socketType`, `offset`, `overrideDirection`, `angleOffset`, `pitchOffset`, `lookDuration`, `lookCurve`, `restoreOnComplete`, `restoreDuration`, `lockCameraInput` |
| `FinishSideViewEvent` | `Finish Side View` | 피니시 공격 연출용 측면 카메라로 전환한다. | `PlayerFinishAttackState.FinishTarget` 기준으로 측면 Yaw를 계산해 `SetRotationSmooth()` 호출 | 이전 카메라 각도로 복원하거나 입력 잠금 해제 | `sideAngleOffset`, `pitchOverride`, `restoreOnComplete`, `lockCameraInput`, `transitionDuration`, `editorTestTarget` |
| `CameraSnapshotSequenceEvent` | `Camera Snapshot Sequence` | `CameraSnapshotProfile` 기반 카메라 샷 시퀀스를 재생한다. | `CameraManager.PushCameraSnapshotSequence()` 호출 | `restorePreviousOnComplete`가 true면 `StopCameraSnapshotSequence()` 호출 | `profile`, `overrideActorAnchor`, `actorAnchor`, `overrideLookAtTarget`, `lookAtTarget`, `restorePreviousOnComplete` |

---

## Movement / Time 이벤트

| 이벤트 | 표시 이름 | 역할 | 시작 처리 | 종료 처리 | 주요 필드 |
|--------|-----------|------|-----------|-----------|-----------|
| `AddForceEvent` | `AddForce` | Player/Monster 이동 컨트롤러에 로컬 방향 기반 속도를 더한다. | `direction.normalized`를 액터 로컬 기준 월드 방향으로 변환하고 `AddVelocity()` 호출 | 없음 | `direction`, `force` |
| `MotionEvent_MotionWarp` | `Motion Warp` | 활성 구간 동안 모션 워핑 윈도우를 연다. | `ActorMovementController.MotionWarp.BeginWarpWindow()` 설정 후 Player/Enemy Combat에 워프 구간 길이를 전달한다. | `EndWarpWindow()` 및 Combat의 `EndMotionWarp()` 호출 | 기존 범위·회전 필드와 `legacyCompatibility`, `arrivalMode`, `desiredStandOff`, `localArrivalOffset`, 도착점 잔여 오차 Dead Zone, 보정 거리·비율·각도 상한, Translation 시간 정책, 재생 속도 정책 |
| `AnimationSpeedEvent` | `Anim Speed` | 애니메이션 속도 변경 이벤트 자리다. | 현재는 `Debug.Log`만 수행한다. | 없음 | `speedMultiplier`, `speedCurve` |
| `TimeScaleEvent` | `TimeScale` | 이벤트 구간 동안 전역 타임스케일 요청을 등록한다. | `GameCombatManager.GameHitStop.Execute(duration, targetTimeScale)` 호출 | 별도 처리 없음. HitStop 요청은 duration 기반으로 자체 해제된다. | `targetTimeScale`, `blendDuration` |
| `LoopEvent` | `Loop`, `Freeze`, `∞ Loop` | 모션 구간 반복/정지/무한 루프를 표현한다. | `Execute()`는 no-op. 타임라인 제어는 `ActorAnimator` 레벨에서 처리한다. | no-op | `mode`, `loopCount`, `freezeDuration` |

### MotionWarp 프리셋

| 프리셋 | 내부 성격 |
|--------|-----------|
| `LightAttack` | Snapshot `DeltaWarp`. 신규 정책은 2.5m 수락 범위, `ContactShell`, 도착 오차 0.08m Dead Zone, 0.5m/30% 보정 예산을 사용 |
| `HeavyAttack` | Snapshot `DeltaWarp`. 신규 정책은 3m 수락 범위, `ContactShell`, 도착 오차 0.12m Dead Zone, 0.8m/40% 보정 예산을 사용 |
| `FinishAttack` | Snapshot `DeltaWarp`. 정밀 Authored Warp Point 적용 전까지 전용 데이터로 조정 |
| `Grab` | 움직이는 타겟을 잡기 위한 Predictive `DeltaWarp`. 정밀 Authored Warp Point는 후속 적용 |
| `Custom` | 필드 설정을 그대로 사용 |

2026-07-29 전체 MotionWarp 데이터가 신규 정책으로 마이그레이션되었다. Light/Heavy와 일반 Custom은 `ContactShell`, Finish 계열은 `AuthoredWarpPoint`, Dash/Lunge 계열은 확장된 제한 보정과 좁은 재생속도 범위를 사용한다. `TargetCenter` 레거시 데이터와 7~8m 일반 공격 프리셋은 남아 있지 않다.

---

## Utility 이벤트

| 이벤트 | 표시 이름 | 역할 | 시작 처리 | 종료 처리 | 주요 필드 |
|--------|-----------|------|-----------|-----------|-----------|
| `CustomCallbackEvent` | `Callback` | 문자열 기반 커스텀 콜백 정보를 남긴다. | 현재는 `Debug.Log`만 수행한다. | 없음 | `callbackName`, `parameters` |
| `FreezeEnemyEvent` | `FreezeEnemy` | 플레이어 주변 적 AI를 일시 정지한다. | `PlayerCombat.GetEnemyAIControllersInRadius(30.0f)`로 찾은 `EnemyAIController`에 `Freeze()` 호출 | 저장한 브레인 목록에 `Unfreeze()` 호출 | 없음 |
| `HideTargetEvent` | `Hide Target` | 대상 렌더러를 일시적으로 숨긴다. | 지정 이름의 자식 또는 전체 Renderer의 `enabled`를 false로 설정 | Renderer의 `enabled`를 true로 복구 | `targetObjectName` |

---

## 몬스터 전용 이벤트

| 이벤트 | 표시 이름 | 역할 | 시작 처리 | 종료 처리 | 주요 필드 |
|--------|-----------|------|-----------|-----------|-----------|
| `TelegraphEvent` | `Telegraph` | 몬스터 현재 스킬의 공격 범위 텔레그래프를 MotionSet 타이밍으로 표시한다. | Monster Actor의 `EnemyCombat.BeginTelegraph(hitPhaseIndex, lockPositionOnStart)` 호출 | `EnemyCombat.ClearTelegraphs()` 호출 | `hitPhaseIndex`, `lockPositionOnStart` |

`TelegraphEvent`는 `AbilityAttackInfo.useMotionEventTelegraph`가 true인 공격에서 수동 타이밍 제어용으로 사용한다. `hitPhaseIndex`는 대응되는 `BeginCollisionEvent.hitPhaseIndex`와 맞춰야 한다.

---

## 설정 가이드

### 일반 근접 공격

| 목적 | 권장 이벤트 |
|------|-------------|
| 공격 판정 시작/종료 | `BeginCollisionEvent` |
| 다음 콤보 입력 허용 | `ComboWindowEvent` |
| 타격 순간 카메라 효과 | `CameraEffectEvent` |
| 타격 순간 슬로우 | `TimeScaleEvent` |
| 이동 보정 | `MotionEvent_MotionWarp` |

### 투사체 공격

| 목적 | 권장 이벤트 |
|------|-------------|
| 발사음 | `PlaySoundEvent` |
| 발사 VFX | `BeginParticleEvent` |
| 실제 투사체 생성 | `SpawnProjectileEvent` |
| 락온/스킬 대상 조준 | `SpawnProjectileEvent.targetMode` |
| AOE 착탄 위치 고정 | `ProjectileTargetMode.TelegraphPosition` 또는 `TargetPosition` |

### 피니시 공격

| 목적 | 권장 이벤트 |
|------|-------------|
| 피니시 타격 처리 | `FinishAttackEvent` |
| 측면 카메라 연출 | `FinishSideViewEvent` |
| 카메라 효과 | `CameraEffectEvent` |
| 무적 구간 | `InvincibilityEvent` |
| 대상 접근 보정 | `MotionEvent_MotionWarp` |

### 몬스터 범위 공격

| 목적 | 권장 이벤트 |
|------|-------------|
| 공격 범위 예고 | `TelegraphEvent` |
| 실제 판정 | `BeginCollisionEvent` |
| 장판/투사체 생성 | `SpawnProjectileEvent` |
| 스킬 대상 기반 회복 | `HealSkillEvent` |
| 소환 | `SpawnSkillEvent` |

---

## 주의 사항

- `SetActive(false)`로 타겟을 끄면 타임라인 갱신 자체가 멈출 수 있으므로 `HideTargetEvent`는 Renderer만 토글한다.
- `BeginCollisionEvent.hitPhaseIndex`는 `AttackInfoBase.hitPhases` 인덱스와 일치해야 한다.
- `TelegraphEvent.hitPhaseIndex`도 실제 Collision 이벤트의 `hitPhaseIndex`와 맞춰야 범위 표시와 판정이 어긋나지 않는다.
- `LoopEvent`는 일반 MotionEvent 실행 흐름에서 동작을 수행하지 않는다. 반복/정지는 `ActorAnimator` 쪽 타임라인 제어에서 해석해야 한다.
- `AnimationSpeedEvent`, `FootstepEvent`, `CustomCallbackEvent`는 현재 런타임 실동작이 제한적이며 로그 중심이다.
- `CameraEffectEvent`, `CameraLookAtSocketEvent`, `FinishSideViewEvent`처럼 입력 잠금을 거는 이벤트는 종료 시점이 반드시 지나가야 잠금이 복구된다. 모션 강제 중단 시 `MotionEventExecutor.Stop()`이 활성 이벤트의 종료 처리를 호출해야 한다.
- `SpawnProjectileEvent`는 Player/Monster 오너일 때 오너 타입 기반 타겟 레이어를 우선 사용한다. 특수 액터에서만 `targetHitLayer` fallback을 사용한다.

---

## 관련 파일

| 파일 | 역할 |
|------|------|
| `Assets/02.Scripts/Data/Event/Animation/MotionEvent.cs` | `MotionEventBase` 정의 |
| `Assets/02.Scripts/Data/Event/Animation/MotionEvent_*.cs` | 이벤트별 구현 |
| `Assets/02.Scripts/GameActor/Animation/MotionEventExecutor.cs` | MotionSet 타임라인 이벤트 실행기 |
| `Assets/02.Scripts/Data/Actor/Animation/Editor/MotionEventAddPopup.cs` | 이벤트 추가 팝업, 카테고리, 프리셋 |
| `Assets/02.Scripts/Data/Actor/Animation/Editor/MotionSetDrawer.cs` | MotionSet 인스펙터/타임라인 필드 표시 |
| `Assets/02.Scripts/GameActor/Component/Player/PlayerCombat.cs` | 플레이어 공격 판정, 콤보, 워프 연동 |
| `Assets/02.Scripts/GameActor/Component/Enemy/EnemyCombat.cs` | 몬스터 판정, 텔레그래프, 스킬 타겟, 소환 등록 |
| `Assets/02.Scripts/GameActor/Base/GameActor.cs` | ActorType 및 공격 타겟 레이어 기본 규칙 |
