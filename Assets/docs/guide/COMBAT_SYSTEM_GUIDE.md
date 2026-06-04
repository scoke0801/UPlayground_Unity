# 전투 시스템 가이드

> 작성일: 2026-06-03  
> 대상 버전: Unity 6 (6000.0.60f1), URP

---

## 개요

현재 전투 시스템은 `GameActor` 기반 액터, KCC 상태 머신, Animancer `MotionSet` 타임라인, ScriptableObject 공격 데이터를 결합해 동작한다.

핵심 특징은 다음과 같다.

- 공격 실행은 상태(`PlayerAttackState`, `EnemyAttackState`)가 시작하고, 실제 공격 데이터와 판정은 `PlayerCombat`, `EnemyCombat`이 담당한다.
- 판정 타이밍은 애니메이션 클립이 아니라 `MotionSet`의 `BeginCollisionEvent`, `TelegraphEvent`, `SpawnProjectileEvent`, `MotionEvent_MotionWarp` 등으로 제어한다.
- 공격 수치는 `PlayerAttackDataSO`, `EnemyAttackDataSO`의 `AttackInfoBase.hitPhases`와 `HitPhaseData`에 들어간다.
- 피해 적용은 `IDamageable.TakeDamage(AttackData)`로 통일되어 있고, 피해량 계산은 `DamageResolver`가 담당한다.
- 방어 판정은 `DefenseResolver`, 피격 상태 결정은 `ReactionResolver`, 근접 히트 탐색은 `CombatHitDetector`가 담당한다.
- 공격 적중 피드백은 `CombatFeedbackDispatcher`를 통해 `GameHitStopHandler`, `GameVitalOrbHandler`, `CameraManager`, `UIManager`, `GameObjectManager`로 전달된다.

---

## 구현 반영 요약

2026-06-03 구조 개선 작업으로 전투 시스템은 기존 `AttackData`/`IDamageable` 호환 경로를 유지하면서 내부 책임을 단계적으로 분리했다.

| 변경 영역 | 구현 결과 | 영향 |
|-----------|-----------|------|
| 피해 계산 | `DamageResolver`, `DamageResult` 추가 | `PlayerActor`, `MonsterActor`의 피해량 계산식이 공통 계산기로 이동 |
| 방어 판정 | `DefenseResolver`, `DefenseResult` 추가 | 플레이어 가드, 패리, 퍼펙트 도지, 무적, `Unblockable` 판정 우선순위가 명시화 |
| 피격 리액션 | `ReactionResolver`, `ReactionDecision` 추가 | 플레이어/몬스터의 상태 전환 결정과 실제 상태 적용이 분리 |
| 히트 감지 | `CombatHitDetector`, `MeleeHitShape`, `CombatHit` 추가 | 플레이어/몬스터 근접 `OverlapSphere` 판정이 같은 경로를 사용 |
| 전투 피드백 | `CombatFeedbackDispatcher`, `CombatFeedbackContext`, `CombatFeedbackProfile` 추가 | 데미지 플로터, 히트 FX, 카메라, HitStop, VitalOrb 호출 위치 중앙화 |
| 전투 상태 | `PlayerCombatStateTracker` 추가 | 플레이어 전투 유지 시간, 위협 탐색, 전투 상태 변화 이벤트가 `PlayerCombat`에서 분리 |
| 액션 실행 | `CombatActionRunner`, `CombatActionDefinition`, `CombatActionInstance`, `CombatTimelineEvent` 추가 | 기존 MotionEvent 직접 호출과 병행되는 공격 실행 컨텍스트 경로 확보 |
| 정책 데이터 | `CombatDefensePolicySO`, `CombatReactionPolicySO`, `CombatPolicyResolver` 추가 | ActorDefinition 단위로 방어 가능 여부와 몬스터 등급별 리액션 허용 규칙을 데이터화 |
| 데이터 검증 | `CombatDataValidator`, `CombatDataValidatorWindow` 추가 | `PlayerAttackDataSO`, `EnemyAttackDataSO` 기본 오류/경고 검증과 Markdown 리포트 저장 지원 |

현재 `PlayerCombat`과 `EnemyCombat`은 공격 데이터 선택, 콤보/쿨다운, 판정 루프를 계속 가진다. `CombatActionRunner`는 현재 action, phase, collision window를 소유하고, MotionEvent의 actor 분기는 runner를 통해 Combat executor로 전달된다.

---

## 아키텍처

```
Input / AI / BT
    │
    ├── PlayerMovementController ── PlayerAttackState / Guard / Dodge / Hit ...
    │       │
    │       └── PlayerCombat
    │              ├── PlayerAttackDataSO
    │              ├── AttackData 생성
    │              ├── 콤보 / 캔슬
    │              ├── PlayerCombatStateTracker
    │              ├── CombatActionRunner
    │              └── CombatHitDetector
    │
    └── EnemyMovementController ── EnemyAttackState / Guard / Hit / Death ...
            │
            └── EnemyCombat
                   ├── EnemyAttackDataSO
                   ├── 스킬 선택 / 쿨다운 / 타겟 캐시
                   ├── 텔레그래프 / Danger Ring
                   ├── CombatActionRunner
                   └── CombatHitDetector

MotionSetAsset
    └── MotionEventExecutor
           ├── BeginCollisionEvent      판정 ON / hitPhaseIndex 지정
           ├── TelegraphEvent           몬스터 경고 표시
           ├── SpawnProjectileEvent     투사체 생성
           ├── ComboWindowEvent         플레이어 콤보 입력 창
           ├── MotionEvent_MotionWarp   워프 구간
           └── TimeScaleEvent           히트스톱 / 슬로우

IDamageable.TakeDamage(AttackData)
    ├── PlayerActor.TakeDamage
    │      ├── DefenseResolver
    │      ├── DamageResolver
    │      ├── ReactionResolver
    │      └── HP / 피격 상태 / 사망 / 피드백
    └── MonsterActor.TakeDamage
           ├── 가드
           ├── DamageResolver
           ├── PoiseStat
           ├── MonsterBreakGauge
           ├── ReactionResolver
           └── HP / 피격 상태 / 사망 / 드랍 / 합류
```

### 파일 구조

| 파일 | 역할 |
|------|------|
| `Assets/02.Scripts/GameActor/Component/Player/PlayerCombat.cs` | 플레이어 공격 데이터 선택, 콤보, 판정, 전투 상태, 가드 카운터 창 |
| `Assets/02.Scripts/GameActor/Component/Enemy/EnemyCombat.cs` | 몬스터 스킬 선택, 쿨다운, 판정, 텔레그래프, Danger Ring |
| `Assets/02.Scripts/GameActor/Object/Player/PlayerActor.cs` | 플레이어 `IDamageable`, 가드/패리/퍼펙트 도지/피격/사망 처리 |
| `Assets/02.Scripts/GameActor/Object/Monster/MonsterActor.cs` | 몬스터 `IDamageable`, 피해 적용, Poise/Break/드랍/합류 처리 |
| `Assets/02.Scripts/GameActor/Combat/Resolution/DamageResolver.cs` | 플레이어/몬스터/특수 브레이크 피해량 계산 |
| `Assets/02.Scripts/GameActor/Combat/Resolution/DefenseResolver.cs` | 플레이어 가드/패리/퍼펙트 도지/무적 우선순위 판정 |
| `Assets/02.Scripts/GameActor/Combat/Resolution/ReactionResolver.cs` | 플레이어/몬스터 피격 리액션 결정 |
| `Assets/02.Scripts/GameActor/Combat/Resolution/CombatPolicyResolver.cs` | 방어/리액션 정책 SO의 null fallback과 룰 조회 |
| `Assets/02.Scripts/GameActor/Combat/Resolution/HitContext.cs` | legacy `AttackData`를 단일 히트 입력 컨텍스트로 변환 |
| `Assets/02.Scripts/GameActor/Combat/Resolution/CombatResult.cs` | 방어, 피해, 리액션, 리소스 변화를 묶는 히트 결과 객체 |
| `Assets/02.Scripts/GameActor/Combat/Resolution/CombatResolutionPipeline.cs` | 방어/피해/리소스 변화/결과 조립/로그 기록의 1차 표준 경로 |
| `Assets/02.Scripts/GameActor/Combat/Resolution/CombatLogRecorder.cs` | `CombatResult` 기반 인메모리 전투 로그 링버퍼 |
| `Assets/02.Scripts/GameActor/Combat/Resolution/CombatLogEntry.cs` | `CombatResult`에 sequence/frame/time 메타데이터를 붙인 로그 항목 |
| `Assets/02.Scripts/GameActor/Combat/Resolution/CombatLogExportUtility.cs` | 전투 로그 CSV/Markdown export 문자열 생성 |
| `Assets/02.Scripts/GameActor/Combat/Detection/CombatHitDetector.cs` | 플레이어/몬스터 공통 근접 Overlap 판정 |
| `Assets/02.Scripts/GameActor/Combat/Action/CombatActionRunner.cs` | 공격 실행 타임라인 이벤트 병행 기록 경로 |
| `Assets/02.Scripts/GameActor/Combat/Feedback/CombatFeedbackDispatcher.cs` | 공격 적중 피드백, 데미지 플로터, VFX, HitStop/Camera/VitalOrb 호출 중계 |
| `Assets/02.Scripts/GameActor/Component/Player/PlayerCombatStateTracker.cs` | 플레이어 전투 상태 지속 시간, 위협 탐색, 상태 변화 이벤트 |
| `Assets/02.Scripts/Tool/Editor/Combat/CombatDataValidatorWindow.cs` | 공격 데이터 기본 검증 에디터 윈도우 |
| `Assets/02.Scripts/Tool/Editor/Combat/CombatLogRecorderWindow.cs` | Play Mode 전투 로그 기록/CSV/Markdown export 창 |
| `Assets/02.Scripts/Data/Combat/CombatData.cs` | `AttackInfoBase`, `HitPhaseData`, `EnemyAttackInfo`, `PlayerAttackInfo`, `AttackData` |
| `Assets/02.Scripts/Data/Combat/CombatDefensePolicySO.cs` | ActorDefinition 단위 방어 정책. `Unblockable`에 대한 Guard/Parry/PerfectDodge 허용 여부 |
| `Assets/02.Scripts/Data/Combat/CombatReactionPolicySO.cs` | 몬스터 등급별 리액션 정책. forceReaction, Poise Break 요구, 상태별 허용 여부 |
| `Assets/02.Scripts/Data/Combat/PlayerAttackDataSO.cs` | 플레이어 약/강/점프/대시/스킬/차지/교체 공격 데이터 |
| `Assets/02.Scripts/Data/Combat/EnemyAttackDataSO.cs` | 몬스터 스킬 풀, 거리/레벨/가중치 기반 선택 |
| `Assets/02.Scripts/GameActor/Component/Common/PoiseStat.cs` | 몬스터 강인도 런타임 처리 |
| `Assets/02.Scripts/GameActor/Component/Enemy/MonsterBreakGauge.cs` | 몬스터 브레이크 게이지, 노출, 반복 쿨다운 |
| `Assets/02.Scripts/Manager/Combat/GameCombatManager.cs` | 전투 핸들러 호스트 |
| `Assets/02.Scripts/Manager/Handler/Combat/GameHitStopHandler.cs` | 전역/액터 단위 히트스톱 |
| `Assets/02.Scripts/Manager/Handler/Combat/GameVitalOrbHandler.cs` | 바이탈 오브 드롭 |
| `Assets/02.Scripts/Data/Event/Animation/MotionEvent_*.cs` | 타임라인 기반 판정, 텔레그래프, 투사체, 워프, 연출 이벤트 |

---

## 공격 데이터

### 공통 데이터

`CombatData.cs`의 `AttackInfoBase`가 공통 공격 정보다.

| 필드 | 설명 |
|------|------|
| `animKey` | 재생할 `MotionSet` 키 |
| `attackType` | `Melee` 또는 `Ranged` |
| `hitPhases` | 멀티 히트 구간별 수치. `BeginCollisionEvent.hitPhaseIndex`와 인덱스가 일치해야 한다 |

`HitPhaseData`는 실제 한 번의 히트 수치를 가진다.

| 그룹 | 주요 필드 |
|------|----------|
| Damage | `damage`, `poiseDamage`, `breakDamage`, `reactionType`, `reactionDuration`, `forceReaction`, `forceBreakExpose` |
| Hitbox | `attackOffset`, `attackRadius`, `hitHeightRange` |
| FX | `hitParticleName` |
| Reaction Forces | `pullForce`, `airborneForce`, `knockBackForce`, `knockBackDrag` |
| Grab / Forced Motion | `grabDuration`, `victimForcedAnimKey` |

런타임 판정에는 `AttackData`가 사용된다. `PlayerCombat`과 `EnemyCombat`은 SO 값을 읽어 `AttackData`를 만들고, 피격 대상의 `TakeDamage(AttackData)`에 전달한다.

### 플레이어 공격 데이터

`PlayerAttackDataSO`는 캐릭터별 공격 풀이다.

| 필드 | 용도 |
|------|------|
| `liteComboAttackList` | 약 공격 체인 |
| `heavyComboAttackList` | 강 공격 체인 |
| `jumpAttackList` | 점프 공격 / 점프 피니시 공격 |
| `dashAttackList` | 대시 공격 / 점프 대시 공격 |
| `skillAttackList` | 숫자 스킬 공격 |
| `comboRoutes` | 입력 시퀀스 기반 연계 라우트 |
| `counterAttack` | 퍼펙트 가드 반격. 비어 있으면 강 공격 첫 항목으로 폴백 |
| `parryCounterAttack` | 공격 중 패리 반격. 비어 있으면 `counterAttack`, 강 공격 첫 항목 순으로 폴백 |
| `entryAttack` | 캐릭터 교체 등장 공격 |
| `swapEvadeCounterAttack` | 스왑 회피 성공 후 카운터 |
| `swapSpecialAttack` | 풀 게이지 캐릭터 교체 특수 공격 |
| `chargeAnimKey`, `chargeStages`, `chargeStageThresholds` | 차지 공격 모션과 단계별 수치 |

### 몬스터 공격 데이터

`EnemyAttackDataSO.skills`에는 `EnemyAttackInfo` 목록이 들어간다. `EnemyCombat.SelectAndExecuteSkill()`은 거리, 레벨, 조건, 쿨다운, 공격 카테고리를 통과한 스킬 중 `selectionWeight` 기반 랜덤으로 현재 스킬을 고른다.

| 필드 | 용도 |
|------|------|
| `skillType` | 공격, 회복, 소환, 버프, 디버프 구분 |
| `attackCategory` | BT가 `Basic`, `Heavy`, `Skill` 같은 범주를 요청할 때 필터링 |
| `requiredLevel` | 몬스터 레벨 제한 |
| `selectionWeight` | 선택 가중치 |
| `minRange`, `maxRange` | 사용 거리 |
| `cooldown` | 스킬별 쿨다운 |
| `useTelegraph`, `useMotionEventTelegraph` | 자동 또는 MotionEvent 기반 바닥 텔레그래프 |
| `useDangerRing`, `dangerRingDuration` | 몸통 기준 수축 경고 UI |
| `defenseType` | `Parryable`, `GuardableOnly`, `Unblockable` 방어 분류 |
| `isAerialSkill`, `isDiveAttack` | 비행 몬스터 공중 스킬 선택 |
| `conditionGroup` | 체력, 아군 상태 등 스킬 사용 조건 |

---

## 플레이어 전투 흐름

### 공격 진입

`PlayerAttackState.TryEnter()`는 공격 모션 존재 여부를 먼저 확인한 뒤 상태 전환한다. 강공 입력이 있고 처형 가능한 적이나 브레이크 노출 대상이 있으면 일반 공격 대신 `PlayerFinishAttackState` 또는 `PlayerSpecialBreakAttackState`로 라우팅한다.

`PlayerAttackState.OnEnter()`의 주요 흐름:

1. `PlayerCombat`, `PlayerEquipment`, `MotionWarpController` 참조를 가져온다.
2. 주 무기를 손에 들도록 `PlayerEquipment.SetMainWeaponDrawn(true)`를 호출한다.
3. 패리 반격, 퍼펙트 가드 반격, 스왑 회피 카운터, 교체 특수 공격, 등장 공격, 콤보 라우트, 스킬, 약/강 콤보 순서로 공격 종류를 결정한다.
4. `PlayerCombat.Execute*()`가 현재 `AttackData`를 만들고 `OnAttackStarted`를 발화한다.
5. `ActorAnimator.PlayMotion(animKey)`로 MotionSet을 재생한다.
6. 공격 대상 스냅/워프용 타겟을 찾고 `MotionWarpController.SetTarget()`에 저장한다.

### 콤보와 캔슬

플레이어 콤보는 두 종류의 창을 사용한다.

| 창 | 제어 위치 | 의미 |
|----|----------|------|
| 콤보 창 | `ComboWindowEvent` → `PlayerCombat.OpenComboWindow()` / `CloseComboWindow()` | 다음 약/강 입력을 받아 같은 공격 상태 안에서 다음 모션으로 이어갈 수 있는 구간 |
| 캔슬 창 | `PlayerCombat.IsCancelWindowOpen` | 현재 히트박스 콜리전이 꺼져 있는 구간. `PlayerInterruptAction`에 허용된 입력으로 상태를 끊을 수 있다 |

액티브 히트 구간에는 `IsPossibleCollide == true`라서 캔슬 창이 닫힌다. 이 규칙 때문에 실제 타격 프레임 중 회피/점프/대시/가드/다른 공격으로 끊는 동작은 기본적으로 막힌다.

### 플레이어 히트 판정

`BeginCollisionEvent`가 `CombatActionRunner.HandleCollisionEvent()`로 전달되면 runner가 현재 phase와 collision window를 갱신하고 등록된 `PlayerCombat` executor에 기존 판정 API를 forwarding한다. 이후 `PlayerCombat.Update()`에서 `PerformHitDetection()`이 실행된다.

판정은 현재 `AttackData`의 값으로 수행된다.

- `hitRange`: 탐색 반경
- `hitAngle`: 전방 기준 각도
- `hitHeightOffset`, `hitHeightRange`: 높이 기준 필터
- `_targetLayerMask`: 기본 타겟 레이어. 실제 기본값은 `GameActor.GetAttackTargetLayerMask()`가 우선한다
- `_hitTargets`: 같은 Collision 구간 내 중복 히트 방지

히트 판정은 `CombatHitDetector.DetectMeleeHits()`를 통과한다. 히트가 성립하면 대상의 `IDamageable.TakeDamage(_currentAttackData)`가 호출되고, 첫 히트 기준으로 `CombatFeedbackDispatcher.ApplyPlayerAttackHitFeedback()`이 히트스톱, 카메라 펀치, 흔들림, 바이탈 오브를 처리한다. 대상별 데미지 플로터와 VFX도 `CombatFeedbackDispatcher`를 통해 표시된다.

### 전투 상태

`PlayerCombat.IsInCombat`은 `PlayerCombatStateTracker`가 계산한다. 공격 실행, 피격, 가드 등 전투 이벤트가 발생하면 `RefreshCombatState()`가 tracker의 `NotifyCombatEvent()`로 전달된다.

추가로 `_threatDetectionRange` 안에 aggro 중인 적이 있으면 `_threatCheckInterval` 주기로 전투 상태가 유지된다. 상태 변화는 `PlayerCombatStateTracker.OnChangeCombatState(bool)`에서 발화되고, `PlayerCombat.OnChangeCombatState` 프록시를 통해 무기 발도/납도 연동과 HUD 표시 등에 사용된다.

---

## 몬스터 전투 흐름

### 스킬 선택

`EnemyAttackState.OnEnter()`는 `EnemyCombat.SelectAndExecuteSkill(distanceToTarget)`를 호출한다. `EnemyCombat`은 다음 조건을 통과한 스킬만 후보로 만든다.

- `EnemyAttackDataSO.skills`에 등록되어 있음
- 공중 전용 스킬이 아님
- 현재 쿨다운 중이 아님
- `requiredLevel`, `minRange`, `maxRange`, `conditionGroup`을 만족함
- BT나 AI가 예약한 `EnemyAttackCategory`와 일치함

선택된 스킬은 `_currentSkill`로 저장되고, 쿨다운 딕셔너리에 등록된다. `SkillType.Attack`은 현재 타겟을 `SkillTargetList`에 저장하고, 회복/소환 계열은 조건에 맞는 자기 자신 또는 아군을 저장한다.

### 텔레그래프와 Danger Ring

몬스터 경고 연출은 두 계층으로 나뉜다.

| 기능 | 플래그 | 처리 |
|------|--------|------|
| 바닥 텔레그래프 FX | `EnemyAttackInfo.useTelegraph` | `EnemyCombat.BeginGroundTelegraph()`가 `EnemyHeavyAttackTelegraph_Circle` 또는 `telegraphFXKey`를 생성 |
| 몸통 Danger Ring UI | `EnemyAttackInfo.useDangerRing` | `UIManager.CreateDangerRing()`으로 공격 타이밍 링 생성 |

`useMotionEventTelegraph == false`이면 `EnemyAttackState.OnEnter()`에서 즉시 텔레그래프를 시작한다. `true`이면 MotionSet 안의 `TelegraphEvent` 타이밍을 따른다.

`useTelegraphPositionForHit`이 켜진 공격은 `TelegraphEvent`에서 예약한 위치를 실제 Collision 판정 위치로 사용한다. 타겟 위치 고정 AOE에 사용한다.

### 몬스터 히트 판정

근접 공격일 때 `EnemyAttackState.UpdateState()`는 `EnemyCombat.IsPossibleCollide`가 켜진 동안 `CheckMeleeAttackHit()`를 호출한다.

`EnemyCombat.CheckMeleeAttackHit()`는 현재 스킬의 `HitPhaseData`를 읽어 `attackOffset`, `attackRadius`, `hitHeightRange`로 OverlapSphere 판정을 하고, `AttackData`를 만들어 플레이어에게 전달한다.

원거리/장판/소환형 공격은 `SpawnProjectileEvent`, `SpawnSkillEvent`, `TelegraphEvent` 등 MotionEvent가 별도 오브젝트를 생성하거나 위치를 예약하는 방식으로 처리한다.

---

## 피해 적용

### 공통 인터페이스

모든 피해 대상은 `IDamageable`을 구현한다.

```csharp
public interface IDamageable
{
    void TakeDamage(AttackData attackData);
    bool IsAlive();
    bool CanTakeDamage();
    Transform GetTransform();
    void LockOn();
    void UnLockOn();
    float GetHealthPercent();
    float GetCurrentHealth();
    void Heal(float healAmount);
}
```

### 플레이어 피해 처리

`PlayerActor.TakeDamage()`의 우선순위는 다음과 같다.

1. `DefenseResolver.ResolvePlayerDefense()`가 가드, 패리, 퍼펙트 도지, 무적 우선순위를 판정한다.
2. `Guarded`이면 현재 `PlayerGuardState.OnAttackBlocked()`로 넘기고, 가드 브레이크 시 별도 피해를 적용한다.
3. `Parried` 또는 `PerfectDodged`이면 피해 없이 기존 보상/연출 메서드를 실행한다.
4. `DamageResolver.ResolvePlayerDamage()` 결과로 `_currentHealth`를 감소시킨다.
5. 데미지 플로터, 피격 피드백, 상태 전환, 사망 처리를 실행한다.

플레이어 피격 반응은 `ReactionResolver.ResolvePlayerReaction()`이 결정하고, 실제 상태 전환은 `PlayerActor`가 적용한다.

`ActorDefinitionSO.combatDefensePolicy`가 연결되어 있으면 `AttackDefenseType.Unblockable`에 대한 Guard/Parry/PerfectDodge 허용 여부는 `CombatDefensePolicySO`를 따른다. 정책이 비어 있으면 기존 코드 동작을 유지한다.

> 플레이어는 씬 배치 `PlayerActor`의 `_definition`(고정, 스왑 무관) 하나에서만 `combatDefensePolicy`를 읽는다. 정책 연결/가시화는 **Stat Generator의 '전투 정책' 탭**(`UPlayGround/Stat/Stat Data Generator`)에서 `기본 정책 에셋 생성` → `누락만 자동연결`로 처리한다. DefensePolicy는 플레이어블 캐릭터(`characterType != None`), ReactionPolicy는 Elite/Boss 몬스터에만 적용된다.

| 반응 | 처리 |
|------|------|
| `KnockBack` | 공격 방향으로 `AddImpulse` |
| `Pull` | 공격자 방향으로 `AddVelocity` |
| `Airborne` | `airborneForce`가 기준 이상이면 `PlayerAirborneState`, 아니면 일반 경직성 충격 |
| `Grab` | `PlayerGrabbedState` |
| `Stun` | `PlayerStunState` |
| `Knockdown` | `PlayerKnockdownState` |
| 기타 | `PlayerHitState` |

차지 공격 중 한 단계 이상 차징된 상태나 현재 상태가 `SuppressesHitReaction`을 제공하면 물리 충격과 상태 전환을 무시할 수 있다.

### 몬스터 피해 처리

`MonsterActor.TakeDamage()`는 플레이어와 달리 스탯 배율을 적용한다. 계산식은 `DamageResolver.ResolveMonsterDamage()`에 있다. Poise Break, forceReaction, Airborne/Knockdown/Grabbed 같은 상태 결정은 `ReactionResolver.ResolveMonsterReaction()`이 담당한다.

```text
finalDamage = attackData.damage
            * attacker.Stats.AttackPower
            * (1 - victim.Stats.Defense)
            * breakExposedMultiplier
            * criticalMultiplier
```

이후 다음 처리가 이어진다.

- HP바 생성 및 갱신
- AI 페이즈 갱신
- 공격자 타겟 획득
- `PoiseStat.TakePoiseDamage()`
- `EnemyTacticalMemory.NotifyTookDamage()`
- 그룹 메모리 알림
- `MonsterBreakGauge.TakeBreakDamage()`
- 피격 상태 전환 또는 사망 처리

몬스터는 `poiseBrokenNow`이거나 `forceReaction == true`일 때만 강한 피격 상태 전환을 수행한다. 공격 상태처럼 `CanPlayHitReaction()`이 false인 상태에서는 강제 리액션도 막힐 수 있다.

`ActorDefinitionSO.combatReactionPolicy`가 연결되어 있으면 `ReactionResolver`가 몬스터 `Grade`별 룰을 읽어 forceReaction 허용, Poise Break 필요 여부, `Hit`/`Stun`/`Knockdown`/`Airborne`/`Grab` 상태 허용 여부를 결정한다. 정책이 비어 있으면 기존 리액션 규칙을 유지한다.

`CombatLogRecorder.Enabled`가 켜져 있으면 일반 피격과 특수 브레이크 피해는 `CombatResult`로 기록된다. 몬스터 일반 피격 결과에는 실제 HP 감소량과 함께 Poise/Break 실제 감소량이 `ResourceChangeSet`에 포함된다.

---

## 가드, 패리, 회피

### 플레이어 가드

`PlayerCombat`은 가드 내구도를 `_guardHitCount`, `_maxGuardCount`, `_guardResetDelay`로 관리한다. 현재 가드 상태가 공격을 막으면 `PlayerGuardState.OnAttackBlocked()`가 실제 처리하고, 가드 카운트가 한계에 도달하면 `PlayerGuardBreakState`로 이어질 수 있다.

퍼펙트 가드 성공 시 `OpenPerfectGuardCounterWindow()`로 반격 입력 창을 열고, 다음 공격 입력은 `PlayerAttackState`에서 `counterAttack` 또는 강 공격 폴백으로 실행된다.

### 공격 중 패리

`PlayerActor.TryParry()`는 다음 조건에서 몬스터 공격을 패리한다.

- 현재 상태 이름이 `"Attack"`
- `PlayerCombat.IsPossibleCollide == true`
- 현재 공격 종류가 `AttackKind.NormalAttack`

패리 성공 시:

- `OpenParryCounterWindow()`로 패리 반격 창을 연다.
- 현재 플레이어 히트 판정을 끄고 Idle로 복귀한다.
- `GameHitStopHandler.HitStopIntensity.PlayerGuard`를 실행한다.
- 카메라 흔들림/FOV/펀치, 패리 VFX, 바이탈 오브가 발생한다.
- 공격자가 `MonsterActor`이면 `MonsterActor.OnParried()`로 스턴 상태에 들어간다.

### 퍼펙트 도지

`PlayerCombat.OpenPerfectDodgeWindow()`는 `PlayerDodgeState.OnEnter`에서 호출된다. 도지 중 피격 시도가 들어왔고 `CanTakeDamage()`가 false인 상황에서 창이 열려 있으면 `TryPerfectDodge()`가 바이탈 오브, 히트스톱, 카메라 피드백을 발생시킨다.

---

## Poise와 Break

### Poise

`PoiseStat`은 몬스터 강인도 컴포넌트다. `MonsterActor.OnDamaged()`에서 `attackData.poiseDamage`를 전달받는다.

| 상태 | 처리 |
|------|------|
| Poise가 남아 있음 | 데미지는 받지만 기본적으로 큰 피격 상태 전환은 하지 않음 |
| 이번 피격으로 0 이하 | `IsPoiseBroken = true`, `EnemyStunState` 또는 `EnemyKnockdownState`로 전환 |
| Broken 후 `recoveryDelay` 경과 | Poise를 최대치로 복구 |

`EnemyAttackState.OnEnter()`는 `PoiseStat.SetHyperArmor(true)`를 호출하고, 종료 시 false로 돌린다. 현재 `PoiseStat.TakePoiseDamage()` 자체는 `IsHyperArmorActive`를 직접 차단 조건으로 쓰지 않으므로, 실제 피격 반응 허용 여부는 상태의 `CanPlayHitReaction()` 정책과 `MonsterActor` 흐름이 결정한다.

### Break Gauge

`MonsterBreakGauge`는 `ActorDefinitionSO.breakGaugeData`의 `MonsterBreakGaugeSO`로 초기화된다.

| 필드 | 용도 |
|------|------|
| `useBreakGauge` | 브레이크 게이지 사용 여부 |
| `allowRepeatBreak` | 같은 전투 중 반복 브레이크 허용 여부 |
| `maxGauge` | 기준 게이지 |
| `breakResist` | `breakDamage` 감소율 |
| `exposedDuration` | 노출 유지 시간 |
| `damageTakenMultiplierWhileExposed` | 노출 중 받는 피해 배율 |
| `resetGaugeRatioOnExpire` | 노출 시간이 끝났을 때 재시작 게이지 비율 |
| `resetGaugeRatioOnSpecialAttack` | 특수 브레이크 공격으로 소비했을 때 재시작 게이지 비율 |
| `gradePolicy` | Weak/Normal/Elite/Boss 등급별 게이지 배율 |

브레이크 게이지가 0이 되면 `ForceExpose()`가 호출되고, `MonsterActor.ExposedMonsters` 레지스트리에 등록된다. `PlayerCombat.UpdateBreakInteractionTarget()`은 이 목록 중 실제로 플레이어가 브레이크 공격할 수 있는 단일 타겟에게만 `UI_BreakInteraction`을 표시한다.

강공 입력 시 `PlayerAttackState.TryEnter()`가 `FindSpecialBreakAttackTarget()`을 확인하고, 대상이 있으면 `PlayerSpecialBreakAttackState`로 라우팅한다. 실제 피해는 `MonsterActor.OnTakeSpecialBreakAttack()`이 일반 무적/가드/피격 흐름을 우회해 적용한다.

---

## MotionEvent 연동

전투 판정과 연출의 대부분은 MotionSet 타임라인에서 발생한다.

| 이벤트 | 전투 역할 |
|--------|----------|
| `BeginCollisionEvent` | `CombatActionRunner`의 히트 타겟 초기화, 타겟 레이어 설정, `hitPhaseIndex` 설정, 판정 ON |
| `DisableCollisionEvent` | `CombatActionRunner`의 판정 OFF 또는 특정 구간 비활성화 |
| `ComboWindowEvent` | 플레이어 콤보 입력 창 ON/OFF |
| `TelegraphEvent` | 몬스터 텔레그래프와 Danger Ring 타이밍 수동 제어 |
| `SpawnProjectileEvent` | 투사체 생성 및 `BaseProjectile.Initialize()` |
| `SpawnSkillEvent` | 소환/스킬 프리팹 생성 |
| `FinishAttackEvent` | 피니시 공격 실제 처형 타격 |
| `SpecialBreakAttackEvent` | 브레이크 특수공격 피해 타이밍 |
| `InvincibilityEvent` | Player/Monster 무적 구간 |
| `MotionEvent_MotionWarp` | 공격 중 타겟 접근/회전 보정 구간 |
| `TimeScaleEvent` | MotionSet 구간 기반 슬로우 |

자세한 이벤트별 필드는 [MOTION_EVENT_ROLE_GUIDE.md](MOTION_EVENT_ROLE_GUIDE.md)를 참고한다.

---

## 피드백 시스템

### HitStop

`GameCombatManager.GameHitStop`은 전역 `GameTimeManager` timeScale 요청과 액터 단위 Animator 속도 조작을 제공한다.

| 강도 | 용도 |
|------|------|
| `Light`, `Medium`, `Heavy`, `Critical` | 일반 타격 강도별 전역 히트스톱 |
| `PlayerDie` | 플레이어 사망 연출 |
| `PlayerGuard` | 퍼펙트 가드/패리/퍼펙트 도지 계열. 전역 timeScale 대신 플레이어 제외 actor slow 처리 |

여러 전역 히트스톱이 겹치면 더 낮은 timeScale 요청이 우선한다. 요청은 id 기반으로 등록되고 duration 종료 후 개별 해제된다.

### Vital Orb

`GameVitalOrbHandler.TrySpawn(VitalOrbTrigger, Vector3)`는 트리거별 확률, 쿨다운, 최대 활성 개수를 확인한 뒤 지면 위치에 회복 오브를 생성한다.

현재 전투 코드에서 확인되는 주요 트리거:

| 트리거 | 발생 지점 |
|--------|----------|
| `PerfectGuard` | 플레이어 패리 성공 |
| `Dodge` | 퍼펙트 도지 성공 |
| `FinishAttackHit` | 몬스터 피니시 공격 처형 |

---

## 전투 로그 / 튜닝 리포트

전투 로그는 `CombatLogRecorder`가 `CombatResult`를 링버퍼에 저장하는 방식으로 동작한다. 기본은 off이며, Play Mode 밸런싱 세션에서만 켠다.

사용 절차:

1. Unity 메뉴 `UPlayGround/Combat/Combat Log Recorder`를 연다.
2. `Enabled`를 켠다.
3. 필요하면 `Capacity`를 조정하고 `Clear`로 이전 로그를 비운다.
4. Play Mode에서 전투를 수행한다.
5. `Export CSV` 또는 `Export Markdown`을 실행한다.

Markdown 리포트는 `Expected Duration` 입력값이 0보다 크면 실제 로그 duration과의 차이를 함께 출력한다. CSV에는 sequence, frame, combatTime, attacker, victim, animKey, hitPhaseIndex, defenseOutcome, rawDamage, finalDamage, HP/Poise/Break delta, reactionState, damage multiplier 계열 필드가 포함된다.

주의: 현재 로그는 실제 피해가 적용된 `CombatResult`만 기록한다. Guard/Parry/Invincible처럼 피해 적용 전에 종료되는 결과는 아직 로그에 남기지 않는다.

---

## 셋업 방법

### 플레이어 캐릭터

1. 활성 모델의 `CharacterModelData`에 캐릭터 타입, 기본 무기 타입, `PlayerAttackDataSO`, 체력 값을 설정한다.
2. `PlayerAttackDataSO`에 약/강/점프/대시/스킬/차지 공격 데이터를 등록한다.
3. 각 `AttackInfoBase.animKey`에 대응하는 `MotionSetAsset`이 `PlayerActorAnimationMotionSet`에 등록되어 있어야 한다.
4. MotionSet에 `BeginCollisionEvent`와 필요 시 `ComboWindowEvent`, `MotionEvent_MotionWarp`, 카메라/TimeScale 이벤트를 배치한다.
5. 멀티 히트 공격은 `hitPhases` 개수와 `BeginCollisionEvent.hitPhaseIndex`를 맞춘다.

### 몬스터

1. `ActorDefinitionSO`에 `statData`, `attackData`, 필요 시 `poiseData`, `breakGaugeData`, `combatDefensePolicy`, `combatReactionPolicy`, `dropTable`, `targetLayerMask`를 연결한다.
2. `EnemyAttackDataSO.skills`에 `EnemyAttackInfo`를 등록한다.
3. 각 스킬의 `baseInfo.animKey`에 대응하는 MotionSet을 몬스터 애니메이션 데이터에 등록한다.
4. 근접 공격은 `BeginCollisionEvent`를 MotionSet 타이밍에 배치한다.
5. 강공격/위험 공격은 `useTelegraph`, `useDangerRing`, `defenseType`을 설정한다.
6. 타겟 위치 고정 AOE는 `useMotionEventTelegraph`, `telegraphAnchorType = TargetPosition`, `useTelegraphPositionForHit` 조합을 사용한다.
7. Elite/Boss처럼 리액션 제한이 필요한 몬스터는 `CombatReactionPolicySO`를 만들고 `ActorDefinitionSO.combatReactionPolicy`에 연결한다.
8. 특수 방어 규칙이 필요한 액터는 `CombatDefensePolicySO`를 만들고 `ActorDefinitionSO.combatDefensePolicy`에 연결한다.

### 방어 / 리액션 정책

정책 에셋은 선택 항목이다. 연결하지 않으면 기존 런타임 동작을 유지한다.

자동 생성은 Unity 상단 메뉴에서 `UPlayGround/Combat/Generate Default Policy Assets`를 실행한다. 같은 기능은 `UPlayGround/Combat/Data Validator` 창의 `Generate Policies` 버튼으로도 실행할 수 있다.

자동 생성기가 수행하는 작업:

- `Assets/10.Datas/Combat/Policy` 폴더 생성.
- `DefaultCombatDefensePolicy`, `EliteCombatReactionPolicy`, `BossCombatReactionPolicy` 에셋 생성 또는 갱신.
- Player `ActorDefinitionSO`에 기본 방어 정책 자동 연결.
- Elite/Boss 몬스터 `ActorDefinitionSO`에 등급별 리액션 정책 자동 연결.

| 에셋 | 주요 설정 | 사용 위치 |
|------|-----------|-----------|
| `CombatDefensePolicySO` | `allowGuardAgainstUnblockable`, `allowParryAgainstUnblockable`, `allowPerfectDodgeAgainstUnblockable` | `DefenseResolver.ResolvePlayerDefense()` |
| `CombatReactionPolicySO` | 등급별 forceReaction 허용, Poise Break 요구, 상태별 리액션 허용 | `ReactionResolver.ResolveMonsterReaction()` |

검증은 `UPlayGround/Combat/Data Validator`에서 실행한다. 현재 검증기는 reaction policy의 등급 중복, 모든 리액션 차단 룰, Unblockable Guard 허용 정책, Elite/Boss ActorDefinition의 reaction policy 누락을 경고한다.

---

## 사용 예시

### 플레이어 공격 데이터 생성 흐름

```csharp
// 상태에서 호출되는 실제 패턴은 PlayerAttackState.GetAnimKey() 내부에 있다.
AttackData attack = player.GetCombat().ExecuteAttack(isCombo: false);
AnimKey key = attack != null ? attack.animKey : AnimKey.None;
player.Animator.PlayMotion(key, 0.25f);
```

### 몬스터 스킬 선택 흐름

```csharp
float distance = detection.DistanceToTarget;
EnemyAttackInfo skill = enemyCombat.SelectAndExecuteSkill(distance);
if (skill != null)
{
    enemy.Animator.PlayMotion(skill.baseInfo.animKey, 0.1f);
}
```

### 외부 투사체 히트 후 플레이어 전투 이벤트 알림

```csharp
damageable.TakeDamage(attackData);
playerCombat.NotifyAttackHit(attackData);
```

---

## 주의 사항

- `HitPhaseData`와 `BeginCollisionEvent.hitPhaseIndex`가 맞지 않으면 멀티 히트 수치가 다른 프레임에 적용된다.
- `BeginCollisionEvent`가 끝나지 않으면 `IsPossibleCollide`가 계속 true로 남아 캔슬 창이 닫히고 중복 판정 위험이 커진다.
- 플레이어 캔슬은 `PlayerInterruptAction`만으로 열리지 않는다. 현재 판정 구간이 비활성인 `IsCancelWindowOpen`도 true여야 한다.
- 몬스터의 `forceReaction`은 Poise가 남아 있어도 리액션을 강제하려는 플래그지만, 상태의 `CanPlayHitReaction()` 또는 `CombatReactionPolicySO`가 막으면 상태 전환되지 않을 수 있다.
- `AttackDefenseType.Unblockable`은 데이터 분류와 Danger Ring 표현에 쓰인다. `CombatDefensePolicySO`가 연결된 경우 실제 Guard/Parry/PerfectDodge 허용 여부도 해당 정책을 따른다.
- `MonsterBreakGauge` 노출 중에도 몬스터는 무방비로 멈추지 않는다. 특수 브레이크 공격 가능 상태와 행동 불능 상태는 별개다.
- `PlayerCombat.IsInCombat`은 즉시 bool 필드를 저장하지 않고 `PlayerCombatStateTracker`에서 시간 기반으로 계산된다. 강제 종료는 `ForceExitCombat()` 호출 시 tracker가 상태를 정리하고 변경 이벤트를 발화한다.
- 투사체와 소환형 공격은 `EnemyCombat.CheckMeleeAttackHit()` 흐름을 타지 않는다. 피해 해결은 `IDamageable.TakeDamage()`로 수렴하지만, 생성 오브젝트의 attacker-side 피드백은 별도 구현을 확인해야 한다.
- `CombatActionRunner`는 현재 action, phase, collision window를 소유한다. `PlayerCombat.SetEnableCollision()` / `EnemyCombat.SetEnableCollision()` 호출 경로는 runner instance를 갱신하는 forwarding 경로로 유지된다.
- `CombatDataValidatorWindow`는 공격 SO 기본 검증, MotionSet 이벤트와 `hitPhases` 매칭 검증, 방어/리액션 정책 검증을 함께 수행한다.

---

## 확장 포인트

- 새 플레이어 공격 타입은 `PlayerAttackDataSO`에 데이터 필드를 추가한 뒤 `PlayerCombat.Execute*()`와 `PlayerAttackState.GetAnimKey()` 우선순위에 연결한다.
- 새 몬스터 공격 선택 규칙은 `EnemyAttackInfo.conditionGroup` 또는 `EnemyCombat.GetAvailableSkills()` 필터에 추가한다.
- 새 피격 반응은 `AttackReactionType` 추가 후 `ReactionResolver`, `PlayerActor.OnDamaged()`, `MonsterActor.OnDamaged()`, 필요 시 `CombatReactionPolicySO`를 함께 확장한다.
- 새 방어 분류는 `AttackDefenseType`, `DefenseResolver`, `CombatDefensePolicySO`, `PlayerGuardState`, `UI_DangerRing` 색/표현 규칙을 같이 갱신한다.
- 새 전투 연출은 `MotionEventBase`를 상속한 이벤트를 만들고 `MotionEventAddPopup` 카테고리에 등록한다.
