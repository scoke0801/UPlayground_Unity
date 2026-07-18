# 전투 시스템 다음 개선 제안

> **보관 문서 주의:** 이 문서의 플레이어 공격 데이터 예시는 Ability 전환 이전 구조다. 현재 단일 소스는 `AbilitySetSO`이며 최신 기준은 `../TODO/GAMEPLAY_ABILITY_SYSTEM_SPEC.md`를 따른다.

> 작성일: 2026-06-03  
> 대상 버전: Unity 6 (6000.0.60f1), URP  
> 분류: 웹 레퍼런스 기반 후속 개선 제안  
> 관련 문서: `Assets/docs/guide/COMBAT_SYSTEM_GUIDE.md`, `Assets/docs/Complete/COMBAT_SYSTEM_ARCHITECTURE_REFACTOR_PLAN.md`

---

## 1. 목적

현재 전투 시스템은 `DamageResolver`, `DefenseResolver`, `ReactionResolver`, `CombatHitDetector`, `CombatFeedbackDispatcher`, `CombatActionRunner`를 도입해 1차 책임 분리를 완료했다.

다만 아직 구조상 다음 한계가 남아 있다.

- `CombatActionRunner`는 기존 MotionEvent 직접 호출 경로와 병행되는 전환 계층이다.
- `AttackData`가 공격 정의, 런타임 히트 정보, 피격 결과 힌트를 함께 담는다.
- `CombatResolutionPipeline` 1차 구현으로 방어, 피해, 리소스 결과 조립, 로그 기록은 묶였지만, 상태 전환/물리 힘 적용과 Poise/Break 적용은 아직 Actor 내부에 남아 있다.
- 투사체, AOE, 소환형 공격은 아직 근접 공격과 같은 판정/해결 경로를 완전히 공유하지 않는다.
- 데이터 검증기는 공격 SO 기본 검증과 MotionSet 이벤트/`hitPhases` 1차 매칭 검증을 담당한다. 더 정밀한 정책 검증은 후속 항목이다.

이 문서는 위 한계를 기준으로 다음 구현 우선순위를 정리한다.

---

## 2. 웹 레퍼런스 요약

### Unreal Gameplay Ability System

Unreal GAS는 능력 실행, 수치 효과, 속성, 연출 이벤트를 분리한다.

| GAS 개념 | UPlayground 적용 방향 |
|----------|-----------------------|
| Gameplay Ability | `CombatActionRunner`가 공격/스킬 실행 단위를 담당 |
| Gameplay Effect | `DamageResult`, `DefenseResult`, `ReactionDecision`, 후속 `CombatResult`로 결과 명시 |
| Attribute | 기존 HP, `ActorStatContainer`, Poise, BreakGauge를 별도 resource 적용 단계로 분리 |
| Gameplay Cue | `CombatFeedbackDispatcher`가 HitStop, VFX, SFX, Camera, UI 피드백을 담당 |

참고:

- https://dev.epicgames.com/documentation/unreal-engine/understanding-the-unreal-engine-gameplay-ability-system
- https://dev.epicgames.com/documentation/en-us/unreal-engine/BlueprintAPI/Ability/GameplayCue

### Unity ScriptableObject 아키텍처

Unity 공식 가이드는 ScriptableObject를 데이터 컨테이너로 사용해 데이터와 로직을 분리하고, 작은 컴포넌트를 조합하는 구조를 권장한다.

UPlayground에는 이미 `PlayerAttackDataSO`, `AbilitySetSO`, `MotionSetAsset`, `ActorDefinitionSO`가 있으므로 전체 구조 전환보다 다음 원칙을 적용하는 것이 적절하다.

- 공격 정의 데이터는 SO에 유지한다.
- 런타임 실행 상태는 `CombatActionInstance`로 분리한다.
- 피격 결과는 `CombatResult` 같은 값 객체로 남긴다.
- 데이터 검증 툴을 강화해 런타임 이전에 에셋 오류를 잡는다.

참고:

- https://unity.com/en/how-to/architect-game-code-scriptable-objects
- https://unity.com/how-to/separate-game-data-logic-scriptable-objects

### Unity ECS / DOTS

Unity ECS는 entity, component, system을 분리해 데이터와 처리 책임을 명확히 한다. 현재 프로젝트는 MonoBehaviour, KCC, Animancer 중심이므로 DOTS 전환은 범위 밖이다.

다만 다음 원칙은 적용 가치가 있다.

- 데이터 구조와 처리 시스템을 분리한다.
- 판정, 피해 계산, 리액션 결정은 가능한 한 상태 없는 시스템으로 둔다.
- MonoBehaviour는 참조 수집, Unity API 호출, 상태 전환 적용에 집중한다.

참고:

- https://docs.unity.cn/Packages/com.unity.entities%401.0/manual/concepts-intro.html
- https://docs.unity.cn/Packages/com.unity.entities%400.3/manual/ecs_core.html

### 액션 게임 전투 설계 사례

God of War류 액션 전투 사례에서 중요한 공통점은 구조 자체보다 타격 판독성, 피격 반응, 공격 선택 유도, 튜닝 반복 속도다.

UPlayground 적용 방향은 다음과 같다.

- 공격별 판정, 리액션, 피드백 결과를 로그로 남긴다.
- 적 등급과 상태별 리액션 정책을 데이터화한다.
- 방어 가능/회피 필요/패리 가능 공격 표현을 검증 가능한 데이터로 만든다.
- 플레이어가 다양한 공격 타입을 쓰도록 보상과 리액션 차이를 명확히 한다.

참고:

- https://blog.playstation.com/2022/10/04/game-developers-explain-what-makes-god-of-war-2018s-combat-tick/

---

## 3. 현재 구조 평가

### 잘 된 점

| 항목 | 평가 |
|------|------|
| MotionSet 기반 타이밍 | 판정, 콤보 창, 워프, 텔레그래프, 투사체 생성 타이밍을 애니메이션 타임라인에서 제어한다 |
| ScriptableObject 공격 데이터 | 공격 수치와 스킬 조건을 에셋으로 관리할 수 있다 |
| Resolver 분리 | 피해, 방어, 리액션 판단이 `PlayerActor`/`MonsterActor` 내부 계산식에서 빠져나오기 시작했다 |
| HitDetector 공통화 | 플레이어/몬스터 근접 판정 중복이 줄었다 |
| Feedback 중앙화 | HitStop, Camera, VFX, DamageFloater 정책 조정 위치가 모였다 |

### 남은 문제

| 문제 | 영향 |
|------|------|
| Runner가 실행 주체가 아님 | MotionEvent가 여전히 `PlayerCombat`/`EnemyCombat` legacy API를 직접 알고 있다 |
| `AttackData` 책임 과다 | 멀티히트, 투사체, AOE, 잔류 공격에서 컨텍스트 복사와 결과 추적이 흐려진다 |
| Pipeline 부재 | 방어, 피해, 리소스 적용, 리액션, 피드백 순서가 Actor 코드에 남는다 |
| 투사체/AOE 경로 분산 | 공격 타입별 밸런스 로그와 검증이 어렵다 |
| MotionSet 검증 부족 | `hitPhaseIndex`, Telegraph, Danger Ring 불일치가 런타임에서 드러날 수 있다 |
| 정책 데이터화 부족 | 보스/엘리트/상태별 리액션 면역과 방어 규칙이 코드 분기로 늘어날 위험이 있다 |

---

## 4. 권장 목표 구조

```
PlayerAttackState / EnemyAttackState
    └── CombatActionRunner.StartAction()
            ├── CombatActionDefinition
            ├── CombatActionInstance
            ├── MotionSet 재생 요청
            └── CombatTimelineEvent 수신
                    │
                    ▼
            CombatHitDetector
                    │
                    ▼
            CombatResolutionPipeline
                    ├── DefenseResolver
                    ├── DamageResolver
                    ├── ResourceApplier
                    ├── ReactionResolver
                    └── CombatResult
                            ├── StateTransitionApplier
                            ├── CombatFeedbackDispatcher
                            └── CombatLogRecorder
```

최종적으로 `PlayerCombat`과 `EnemyCombat`은 다음 책임만 남기는 것이 좋다.

| 클래스 | 최종 책임 |
|--------|-----------|
| `PlayerCombat` | 플레이어 공격 데이터 선택, 콤보/차지/스킬/특수공격 선택, 입력 창 상태 |
| `EnemyCombat` | 몬스터 스킬 선택, 쿨다운, 스킬 타겟 캐시, 텔레그래프 설정 제공 |
| `CombatActionRunner` | 공격 실행 생명주기, 현재 phase, collision window, timeline event 처리 |
| `CombatResolutionPipeline` | 방어, 피해, 리소스, 리액션, 결과 생성 |
| `CombatFeedbackDispatcher` | 결과 기반 연출 피드백 실행 |

---

## 5. 우선순위별 개선안

## P0 — MotionSet / hitPhases 검증기 확장

가장 먼저 진행할 작업이다. 런타임 구조를 더 바꾸기 전에 데이터 오류를 사전에 잡아야 한다.

### 구현 항목

- `AttackInfoBase.animKey`에 해당하는 `MotionSetAsset` 존재 여부 검증
- Melee 공격인데 `BeginCollisionEvent`가 없는 경우 오류
- `BeginCollisionEvent.hitPhaseIndex >= hitPhases.Count` 오류
- `hitPhases.Count > 1`인데 collision phase가 0번만 있는 경우 경고
- `useMotionEventTelegraph == true`인데 `TelegraphEvent`가 없는 경우 오류
- `useTelegraphPositionForHit == true`인데 위치 예약 이벤트가 없는 경우 오류
- `defenseType == Unblockable`인데 Danger Ring 또는 Telegraph 표현이 없는 경우 경고

### 완료 기준

- `UPlayGround/Combat/Data Validator`에서 MotionSet 이벤트와 공격 SO 불일치를 확인할 수 있다.
- 리포트에 에셋 경로, `animKey`, phase index, 오류 등급이 포함된다.

## P1 — `HitContext` / `CombatResult` 도입

`AttackData`를 즉시 삭제하지 않고, 결과 객체를 병행 도입한다.

### 신규 후보

```text
Assets/02.Scripts/GameActor/Combat/Resolution/HitContext.cs
Assets/02.Scripts/GameActor/Combat/Resolution/CombatResult.cs
Assets/02.Scripts/GameActor/Combat/Resolution/ResourceChangeSet.cs
```

### 목표

- 1회 히트 정보를 `HitContext`로 표현한다.
- 방어, 피해, 리액션, 리소스 변화를 `CombatResult`로 묶는다.
- `CombatFeedbackDispatcher`와 전투 로그가 `CombatResult`를 기준으로 동작할 수 있게 한다.

### 완료 기준

- `PlayerActor.TakeDamage(AttackData)`와 `MonsterActor.TakeDamage(AttackData)` 내부에서 legacy `AttackData`를 `HitContext`로 변환할 수 있다.
- 피드백과 로그가 같은 결과 객체를 읽는다.

## P2 — `CombatResolutionPipeline` 추가

현재 분리된 resolver들을 하나의 순서로 묶는다.

### 현재 구현 상태

2026-06-03 기준 P2 1차 구현을 완료했다.

구현 파일:

```text
Assets/02.Scripts/GameActor/Combat/Resolution/CombatResolutionPipeline.cs
```

현재 pipeline이 담당하는 범위:

- `ResolvePlayerHit`: `DefenseResolver` → `DamageResolver` → `CombatResult` 조립
- `ResolvePlayerGuardBreakDamage`: 가드 브레이크 피해 결과 조립
- `ResolveMonsterHit`: 몬스터 피해 결과 조립
- `WithReaction`: Actor 내부에서 적용한 `ReactionDecision`을 `CombatResult`에 합성
- `RecordIfDamageApplied`: 실제 피해가 적용된 결과만 `CombatLogRecorder`에 기록

의도적으로 아직 남겨둔 범위:

- HP 감소, 상태 전환, 물리 힘 적용은 `PlayerActor`/`MonsterActor`가 유지한다.
- Poise/Break 적용은 `MonsterActor.OnDamaged()` 내부에 남아 있다.
- 특수 브레이크 진입점은 pipeline에 편입됐다. 투사체/AOE 진입점은 P4에서 편입한다.
- 별도 `ResourceApplier`, `StateTransitionApplier` 클래스는 만들지 않았다.

### 예상 순서

```text
DefenseResolver
    → DamageResolver
    → ResourceApplier
    → ReactionResolver
    → CombatResult
```

### 구현 원칙

- Resolver는 상태 전환이나 Unity 오브젝트 생성을 직접 하지 않는다.
- HP, Poise, BreakGauge 적용은 `ResourceApplier`가 담당한다.
- 상태 전환은 `StateTransitionApplier` 또는 Actor별 적용 메서드가 담당한다.
- 피드백은 `CombatFeedbackDispatcher`가 `CombatResult`를 받아 실행한다.

> 과도한 클래스 분할을 피한다. `ResourceApplier`와 `StateTransitionApplier`는 P2 시점에 **별도 클래스로 쪼개지 말고 `CombatResolutionPipeline` 내부 메서드로 먼저 구현**한다. 투사체/AOE/특수 진입점 등 두 번째 호출처가 생겨 실제 재사용 압력이 확인될 때(P4 시점 예상) 클래스로 추출한다. 1인 개발에서 호출처가 하나뿐인 단계부터 인터페이스/클래스로 나누면 간접 호출만 늘고 이득이 없다.

> 주의: 방어 결과는 단순 피해 배율이 아니라 흐름을 단락시킬 수 있다. 현재 `MonsterActor.TakeDamage`는 가드 시 `EnemyGuardState.OnAttackBlocked` 호출 후 즉시 return한다. 따라서 `DefenseResult`는 "차단됨 → 가드 상태로 리다이렉트" 같은 **제어 결과**를 표현할 수 있어야 하고, pipeline은 선형 4단계가 아니라 단계별 조기 종료(early-out)를 허용해야 한다.

### 완료 기준

- [x] `PlayerActor.TakeDamage()`와 `MonsterActor.TakeDamage()`가 방어/피해/결과 조립 순서를 직접 나열하지 않는다.
- [x] 전투 로그가 Actor 직접 `CombatResult.Build()`가 아니라 `CombatResolutionPipeline.RecordIfDamageApplied()`를 통해 기록된다.
- [ ] Poise/Break 적용과 리액션 결정까지 pipeline 내부 결과로 완전히 채운다. 이 항목은 P4/P5에서 재검토한다.
- 새 방어/리액션 규칙 추가 시 pipeline 단계만 확장하면 된다.

## P3 — Runner 중심 MotionEvent 전환

`CombatActionRunner`를 병행 기록 계층에서 실제 실행 계층으로 승격한다.

### 변경 방향

현재:

```text
BeginCollisionEvent
    ├── PlayerCombat.SetHitPhaseIndex()
    ├── PlayerCombat.SetEnableCollision()
    ├── EnemyCombat.SetHitPhaseIndex()
    └── EnemyCombat.SetEnableCollision()
```

목표:

```text
BeginCollisionEvent
    └── CombatActionRunner.HandleTimelineEvent(CombatTimelineEvent)
            ├── currentPhaseIndex 갱신
            ├── collision window 열기
            └── CombatHitDetector 호출 준비
```

### 현재 상태 주의

`CombatActionRunner`는 아직 legacy MotionEvent 경로와 병행된다. `MotionEvent_Collision`은 여전히 `PlayerCombat.SetEnableCollision` / `EnemyCombat.SetEnableCollision`을 호출하지만, 해당 combat API가 runner에도 `BeginCollision` / `EndCollision` / `HitPhaseChanged`를 전달한다.

2026-06-03 P3 1차 작업으로 다음이 반영되었다.

- `CombatActionRunner.CurrentPhaseIndex`, `IsCollisionActive` 공개
- `CombatActionInstance`가 `ActionStarted`에서 phase와 collision 상태 초기화
- `PlayerCombat.PerformHitDetection()`이 판정 직전 runner phase와 legacy `AttackData.hitPhaseIndex`를 동기화
- `EnemyCombat.CheckMeleeAttackHit()`이 판정 직전 runner phase와 legacy `_currentHitPhaseIndex`를 동기화

즉 현재 runner는 "실행 주체"는 아니지만, 실제 collision window와 phase 상태를 보유하고 판정 직전에 참조된다.

2026-06-03 P3 2차(decoupling) 작업으로 다음이 반영되었다.

- 신규 인터페이스 `ICombatCollisionExecutor`(`SetTargetLayerMask`/`SetHitPhaseIndex`/`SetEnableCollision`/`ClearHitTargets`) 도입. `PlayerCombat`/`EnemyCombat`이 구현한다. 잔류 공격용 `IMotionEventCombatTarget`과 **의도적으로 분리**해 MotionEvent 잔류 경로가 Combat을 먼저 가로채는 것을 막는다.
- `CombatActionRunner`에 `HandleCollisionEvent(enable, hitPhaseIndex, targetLayer)`(BeginCollision: ClearHitTargets 항상)와 `HandleCollisionToggle(enable)`(DisableCollision: enable 시에만 clear) 추가. 각 메서드는 등록된 executor에 기존 MotionEvent 분기와 **동일한 순서**로 위임한다.
- `CombatActionRunner.Awake`가 자신을 `GameActor.ActionRunner`에 등록(placement 의존 제거). 각 Combat은 init 시 `SetCollisionExecutor(this)`로 등록.
- `MotionEvent_Collision` / `MotionEvent_DisableCollision`의 actor 분기가 `PlayerCombat`/`EnemyCombat`을 직접 호출하지 않고 `actor.ActionRunner`에만 위임한다. 잔류(`IMotionEventCombatTarget`) 경로는 무변경.

2026-06-03 P3 3차(윈도우 소유권 이전) 작업으로 다음이 반영되었다.

- `PlayerCombat.IsPossibleCollide` / `EnemyCombat.IsPossibleCollide`가 자체 플래그가 아니라 `_actionRunner.IsCollisionActive`(runner instance)를 읽는다.
- 중복 플래그 필드 `_isCollideCollisionEnable`(PlayerCombat) / `_isCollisionEnabled`(EnemyCombat) **제거**. 이제 충돌 윈도우 상태의 사본은 runner instance 하나뿐.
- `EnemyCombat.TryGetSwapEvadeThreat`의 `_isCollisionEnabled` 3개소를 `IsPossibleCollide`로 치환(값 동일).
- **forwarding은 유지**(advisor 지침): `SetEnableCollision`/`SetHitPhaseIndex` → runner instance 갱신이 곧 윈도우의 권위 쓰기다. 직접 호출자(`PlayerChargeState.SetEnableCollision(false)` 3개소, `EnemyCombat.CheckMeleeAttackHit`의 phase sync)도 이 경로로 instance를 갱신한다.

안전 근거: `ActionEnded`는 어디서도 송신되지 않아 `CurrentAction`은 첫 공격 이후 항상 non-null(다음 `StartLegacyAction`으로 교체될 뿐). `StartLegacyAction`은 collision 이벤트보다 **먼저** 호출되어 `CurrentAction`을 세팅하고, `ActionStarted`가 공격마다 `IsCollisionActive=false`로 리셋한다. 따라서 충돌 활성 구간 내내 `CurrentAction`이 살아 있어 `IsPossibleCollide`가 정확히 추종한다.

> **버그 수정(2026-06-03):** 위 안전 근거는 처음에 지상 적 경로를 놓쳤다. 플레이어는 `OnAttackStarted`, **비행 적**은 `SetCurrentSkill`에서 `StartLegacyAction`을 호출했으나, **지상 적(일반/카운터/비행 지상 근접)은 `SelectAndExecuteSkill` 경로**를 타는데 여기에 `StartLegacyAction`이 없었다. 그 결과 지상 적의 `CurrentAction`이 null → `IsPossibleCollide=false` → **적 근접 피격 무발생(silent 0뎀)**. P3 2차에서는 `IsPossibleCollide`가 독립 플래그를 읽어 드러나지 않았다. 수정: `SelectAndExecuteSkill`과 `SetCurrentSkill`이 공통 헬퍼 `StartRunnerActionForSkill`로 runner 액션을 시작하도록 통일. 검증 게이트가 정확히 이 클래스를 잡아냈다.

### 완료 기준

- [x] MotionEvent가 `PlayerCombat`/`EnemyCombat` 구현 세부를 직접 호출하지 않는다. (2026-06-03 P3 2차)
- [x] `CombatActionRunner`만 현재 action, phase, collision window를 가진다. (2026-06-03 P3 3차 — Combat 중복 플래그 제거, `IsPossibleCollide`가 runner instance를 읽음)
- [x] `CombatActionRunner`가 현재 action, phase, collision window를 실제 값으로 보유한다.
- [x] 플레이어/몬스터 근접 판정이 runner phase를 읽어 legacy phase와 동기화한다.
- 기존 공격 데이터와 MotionSet은 수정 없이 호환된다.

> **검증 게이트(런타임 필수):** P3 3차는 *detection 게이트가 `CurrentAction` 생존에 의존*하게 만든다. 실패 시 LogError 없이 **조용히 0뎀**이 되므로, Unity에서 다음을 반드시 확인한다 — ① 적 근접(단일/멀티 히트 phase)이 정상 피해, ② 차지 공격(PlayerChargeState의 `SetEnableCollision(false)` 3개소)이 정상 동작. 플레이어 단일 히트는 이 클래스 결함을 가장 안 드러내므로 단독 테스트로 불충분하다.
>
> **✅ 2026-06-04 Play Mode 게이트 통과:** 적 근접 단일/멀티 히트 정상 피해, 차지 공격 정상 동작 확인. P3 silent-0뎀 회귀 없음.

### Legacy 경로 삭제(cutover) 기준

> **현재 단계 상태:** P3 1~3차 전 과정이 relay 구조였다(runner와 legacy가 분기되는 별도 경로가 아님 — runner instance가 단일 윈도우 소유, Combat은 그것을 읽기만 함). 따라서 "legacy vs runner 200히트 결과 일치" 비교는 **구조적으로 N/A**다(비교할 두 번째 경로가 존재한 적 없음). cutover 잔여 cleanup 현황은 아래 체크리스트 참조 — 명명 정리·fallback 제거는 2026-06-04 완료, per-frame 판정 루프 이전만 선택 항목으로 남았다.

> **원래 cutover 기준(200히트 legacy-vs-runner 비교)은 폐기한다.** 그 기준은 "독립 실행되는 두 경로가 분기 가능"하다는 전제였으나, P3 1~3차는 처음부터 relay 구조여서 분기되는 두 번째 경로가 존재하지 않았다. forwarding은 윈도우의 권위 쓰기로 **영구 유지**한다(`EnemyCombat.CheckMeleeAttackHit`의 phase 의존 + 직접 호출자). 따라서 "forwarding 제거"도 cutover 목표가 아니다.
>
> 선택적 cleanup 진행 현황:
> - [x] **`StartLegacyAction` → `StartAction` 명명 정리 (2026-06-04).** 호출처 3개소(`CombatActionRunner` 정의, `PlayerCombat.HandleAttackStartedForRunner`, `EnemyCombat.StartRunnerActionForSkill`). 기능 무변경 — legacy 별도 경로가 아니라 단일 액션 시작 함수임을 이름에 반영.
> - [x] **MotionEvent fallback 경로 제거 (2026-06-04).** `MotionEvent_Collision`/`MotionEvent_DisableCollision`의 `HandleActorCombatFallback`(각 ~20줄) 삭제. P3 3차 이후 충돌 윈도우는 runner instance 단일 소유라, runner/executor 부재 시 우회 경로(`combat.SetEnableCollision`)도 같은 missing runner로 forward되어 동작 불능 → 가짜 안전망이었다. `runner == null || !HasCollisionExecutor` 가드는 유지하되 본체를 `LogError`로 교체(설정 오류로 보고).
> - [ ] per-frame 판정 루프(`PerformHitDetection`/`CheckMeleeAttackHit`)를 runner가 직접 구동하도록 이전(현재는 Combat이 `IsPossibleCollide`=runner를 읽어 게이팅하므로 기능상 동일 — 선택).
>
> **✅ P3 완료 (2026-06-04):** 위 게이트(적 근접/멀티히트 + 차지 런타임 확인) 통과 + 선택적 cleanup 2건 반영. 남은 per-frame 루프 이전은 기능 동일한 선택 항목.

## P4 — 투사체 / AOE / 잔류 공격 공통 pipeline 연결

근접 공격뿐 아니라 모든 공격 타입이 같은 결과 처리 경로를 사용해야 한다.

### 대상

- `BaseProjectile`
- `LinearProjectile`
- `AOEProjectile`
- `SpawnProjectileEvent`
- `SpawnSkillEvent`
- 캐릭터 스왑 잔류 공격
- 지면 텔레그래프 기반 AOE

### 완료 기준

- [x] 투사체 히트도 `HitContext`를 만든다. (P2로 충족 — 모든 데미지가 `IDamageable.TakeDamage`→`CombatResolutionPipeline` 수렴)
- [x] AOE 히트도 `CombatResolutionPipeline`을 통과한다. (P2)
- [x] 히트 로그와 피드백 정책이 근접/원거리/AOE를 같은 형식으로 기록한다. (로그: P2 `RecordIfDamageApplied` / 피드백: 2026-06-03 P4 attacker-side 통일)

### 2026-06-03 P4 작업 (attacker-side 피드백 완전 통일)

victim-side(피해 해결·로그)는 P2에서 이미 통일되어 있었다. 남은 갭은 attacker-side 연출이었고, **완전 통일**(사용자 선택)로 다음을 반영했다.

- `PlayerCombat`에 공개 메서드 2개: `ShowExternalHitFeedback(attackData)`(대상별 데미지 숫자 + 히트 VFX, 기존 private `ShowAttackHitFeedback` 위임) / `ApplyExternalAttackImpact(attackData)`(히트스톱·카메라 펀치/셰이크·바이탈오브·킬캠, `attackKind`별. 근접과 동일하게 `IsParryCounterAvailable` 가드 포함). **`_currentAttackData`가 아니라 전달 `attackData`로 동작**해 투사체/AOE 실제 공격 정보를 반영.
- `BaseProjectile.OnHit`: 인라인 `CameraManager.Punch`/`StartShake` 제거 → 플레이어 소유 시 `ShowExternalHitFeedback` + `ApplyExternalAttackImpact` + `NotifyAttackHit`. 또한 `Initialize`에서 `attackData.hitParticleName = hitParticleName`을 설정해 **설정된 히트 FX가 실제로 표시**되도록 수정(이전엔 기본 LiteHit FX 고정 버그).
- `AOEProjectile.CheckAOEDamage`: 대상별 floater+VFX+게이지, **임팩트는 `_impactFeedbackApplied` 플래그로 AOE당 1회**(틱 히트스톱 스팸 방지).

범위 밖(의도): **잔류 공격**(`ResidualPlayerCombat`)은 별도 executor로 자체 floater를 이미 가짐 — live PlayerCombat 연출 대상 아님. **적 다이브** 등 적 공격은 attacker가 몬스터라 player 카메라 연출 대상이 아님(victim-side `PlayerActor.TakeDamage`가 담당).

### 검증 게이트 (런타임)

- 플레이어 **투사체**가 몬스터 적중 시: 데미지 숫자 표시 + 설정된 히트 FX + 히트스톱/카메라 펀치(공격 종류별).
- 플레이어 **AOE** 다중 적중 시: 대상마다 데미지 숫자, 임팩트(히트스톱/카메라)는 1회만.
- 게임필 확인(사용자가 완전 통일 선택): 연사/고속 투사체의 히트스톱 누적, 원거리 킬에서의 킬캠 스냅 각도.

## P5 — 방어 / 리액션 정책 데이터화

현재 resolver의 코드 분기를 정책 데이터로 옮긴다.

### 신규 후보

```text
Assets/02.Scripts/Data/Combat/CombatDefensePolicySO.cs
Assets/02.Scripts/Data/Combat/CombatReactionPolicySO.cs
Assets/02.Scripts/GameActor/Combat/Resolution/CombatPolicyResolver.cs
```

### 정책 예시

| 정책 | 예시 |
|------|------|
| 보스 리액션 면역 | `Grab`, `Airborne`, `Knockdown` 무시 |
| Elite 리액션 제한 | `forceReaction` 또는 Poise Break일 때만 상태 전환 |
| Unblockable 처리 | Guard 불가, Dodge 가능, Parry 불가 |
| 상태 태그 기반 억제 | 공격 중 HyperArmor 태그가 있으면 Light/Hit 무시 |
| 방어 표현 규칙 | Unblockable은 Danger Ring/텔레그래프 필수 |

### 현재 구현 상태

2026-06-03 기준 P5 1차 구현을 완료했다.

구현 파일:

```text
Assets/02.Scripts/Data/Combat/CombatDefensePolicySO.cs
Assets/02.Scripts/Data/Combat/CombatReactionPolicySO.cs
Assets/02.Scripts/GameActor/Combat/Resolution/CombatPolicyResolver.cs
```

현재 정책화된 범위:

- `ActorDefinitionSO`에 `combatDefensePolicy`, `combatReactionPolicy` 참조 추가.
- `DefenseResolver`가 `CombatDefensePolicySO`를 선택 입력으로 받아 `Unblockable`에 대한 Guard/Parry/PerfectDodge 허용 여부를 데이터로 판정. 정책이 없으면 기존 동작을 유지한다.
- `ReactionResolver`가 `CombatReactionPolicySO`와 `MonsterActorGrade`를 선택 입력으로 받아 몬스터 등급별 forceReaction 허용, Poise Break 요구, `Hit`/`Stun`/`Knockdown`/`Airborne`/`Grab` 상태 허용 여부를 판정. 정책이 없으면 기존 동작을 유지한다.
- `CombatPolicyResolver`가 정책 null fallback과 등급 룰 조회를 담당한다.
- `CombatDataValidator`가 정책 에셋 중복 등급, 모든 리액션 차단 룰, Unblockable Guard 허용 정책, Elite/Boss ActorDefinition의 reaction policy 누락을 경고한다.
- `CombatPolicyAssetGenerator`가 `DefaultCombatDefensePolicy`, `EliteCombatReactionPolicy`, `BossCombatReactionPolicy`를 자동 생성/갱신하고 ActorDefinition에 기본 정책을 연결한다.

### 2026-06-04 P5 보강 (적용 대상 정밀화 + 가시화 툴)

- **DefensePolicy 적용 대상 변경: `Player` 플래그 → 플레이어블(`characterType != None`).** 근거: 이 프로젝트의 ActorDefinitionSO 40개가 전부 `actorType: Monster`(Player 플래그 0개)이고 `recruitableAs`도 전부 미채움(0)이다. 플레이어는 씬에 배치된 `PlayerActor`의 `_definition`(Inspector 고정, 스왑 무관) **하나**에서만 `combatDefensePolicy`를 읽으며, 그 정의는 플레이어블 캐릭터 정의(MonsterBokusei 등, `characterType` 지정)다. 따라서 Player 플래그 기준으로는 **아무에게도 연결되지 않는 죽은 데이터**였다. `CombatPolicyAssetGenerator.IsPlayableCharacter`(`Player` 플래그 ‖ `characterType != None`)로 판정. 순수 적(`characterType == None`)은 `DefenseResolver`가 읽지 않으므로 제외한다.
- **Stat Generator에 '전투 정책' 탭 추가** (`StatDataGeneratorWindow`). 정의별 Defense/Reaction 정책 연결 상태를 한눈에(✓연결/⚠누락/—해당없음) 보여주고, `기본 정책 에셋 생성`·`누락만 자동연결`(등급/플레이어블 규칙)·행별 ObjectField 수동 지정/해제를 제공한다. `CombatPolicyAssetGenerator.TryLoadDefaultPolicies`/`ResolveReactionPolicyForGrade`를 재사용한다.
- 알려진 데이터 갭: `MonsterNenmir.characterType == None`이라 Nenmir를 플레이어블로 쓰려면 `characterType = Nenmir` 보정 필요(현재는 DefensePolicy 대상에서 제외됨).

의도적으로 남겨둔 범위:

- 기본 정책 에셋 생성과 ActorDefinition 연결은 자동 생성 메뉴 또는 '전투 정책' 탭으로 처리한다. 에셋별 세부 수치 튜닝은 Unity Editor에서 후속 조정한다.
- HyperArmor/상태 태그 기반 억제는 아직 코드/데이터 구조가 없으므로 후속 확장 항목이다.
- Unblockable 텔레그래프 표현 검증은 P0에서 유지하고, P5 정책은 실제 방어 가능 여부만 담당한다.

### 완료 기준

- [x] 새 몬스터 등급이나 상태별 리액션 규칙 추가 시 Actor 코드를 수정하지 않는다. 단, enum 자체 추가는 코드 변경 대상이다.
- [x] 데이터 검증기가 정책과 공격 데이터 불일치/위험 설정을 잡는다.
- [x] 기본 정책 에셋 자동 생성 및 Elite/Boss ActorDefinition 자동 연결 기능을 제공한다.
- [ ] Unity Editor에서 자동 생성 메뉴를 실행하고 생성된 정책 에셋을 프로젝트 밸런스 의도에 맞게 조정한다.
- [ ] Unity Play Mode에서 Elite/Boss 리액션 제한과 Unblockable 방어 정책을 확인한다.

## P6 — 전투 로그 / 튜닝 리포트

구조 개선의 최종 목적은 반복 튜닝 속도 개선이다.

### 기록 항목

| 항목 | 설명 |
|------|------|
| attacker / victim | 공격자와 피격자 |
| animKey | 사용 공격 |
| hitPhaseIndex | 적용된 phase |
| defenseOutcome | 가드, 패리, 도지, 무적, 일반 피격 |
| rawDamage / finalDamage | 계산 전후 피해량 |
| poiseDelta / breakDelta | 강인도와 브레이크 변화 |
| reactionState | 실제 피격 상태 |
| feedbackProfile | 적용된 HitStop/Camera/VFX 기준 |
| combatTime | 전투 경과 시간 |

### 현재 구현 상태

2026-06-03 기준 P6 1차 구현을 완료했다.

구현 파일:

```text
Assets/02.Scripts/GameActor/Combat/Resolution/CombatLogEntry.cs
Assets/02.Scripts/GameActor/Combat/Resolution/CombatLogExportUtility.cs
Assets/02.Scripts/Tool/Editor/Combat/CombatLogRecorderWindow.cs
```

현재 제공 기능:

- `CombatLogRecorder`가 `CombatResult`에 sequence, frame, `Time.time`, `Time.unscaledTime`을 붙인 `CombatLogEntry` 링버퍼를 기록한다.
- `CombatLogRecorderWindow` 메뉴: `UPlayGround/Combat/Combat Log Recorder`.
- 창에서 로그 기록 on/off, capacity 조정, clear, CSV export, Markdown report export를 수행한다.
- Markdown report는 entries, duration, expected duration, duration delta, total/average damage, critical count, AnimKey별 hit/damage 요약을 출력한다.
- CSV는 attacker/victim, animKey, phase, defense outcome, raw/final damage, HP/Poise/Break delta, reaction, damage multiplier 계열 필드를 출력한다.

의도적으로 남겨둔 범위:

- 현재 recorder는 `CombatResolutionPipeline.RecordIfDamageApplied` 경로만 기록한다. Guard/Parry/Invincible early-out 로그까지 보고 싶으면 pipeline의 early-out 결과 기록 정책을 별도로 확장한다.
- feedbackProfile은 아직 `CombatResult`에 승격되지 않아 CSV/Markdown에 직접 기록하지 않는다.
- 장기 저장/세션 관리/자동 플레이테스트 연동은 후속 툴링 범위다.

### 완료 기준

- [x] Play Mode에서 전투 로그를 CSV 또는 Markdown으로 내보낼 수 있다.
- [x] 밸런스 디자이너 툴이 실제 전투 로그를 읽어 예상 전투 시간과 비교할 수 있다. (`Expected Duration` 입력 → Markdown duration delta)
- [ ] Play Mode에서 실제 전투 세션을 기록해 CSV/Markdown 파일 내용을 확인한다.

## P7 — QTE / 컨텍스트 입력(브레이크 상호작용) 고도화

### 현재 상태

QTE에 해당하는 기능은 이미 부분적으로 존재한다. 다만 "QTE"라기보다 **단일 입력 컨텍스트 프롬프트**에 가깝다.

현재 흐름:

```text
MonsterBreakGauge 가득 참
    └── OnBreakExposed
            └── MonsterActor가 UI_BreakInteraction 생성 (Center 소켓 위 F키 아이콘, 펄스)
                    │
PlayerCombat.UpdateBreakInteractionTarget (0.1s 틱)
    └── 범위 내 IsExposed 몬스터 탐색 → SetBreakInteractionTarget
            │
플레이어 입력
    └── PlayerSpecialBreakAttackState
            └── MonsterActor.OnTakeSpecialBreakAttack (메인 TakeDamage 흐름 우회)
```

관련 파일:

```text
Assets/02.Scripts/UI/WorldSpace/UI_BreakInteraction.cs
Assets/02.Scripts/GameActor/Component/Enemy/MonsterBreakGauge.cs
Assets/02.Scripts/GameActor/Component/Player/PlayerCombat.cs (UpdateBreakInteractionTarget)
Assets/02.Scripts/GameActor/State/Player/PlayerSpecialBreakAttackState.cs
Assets/02.Scripts/GameActor/Object/Monster/MonsterActor.cs (OnTakeSpecialBreakAttack)
```

### 한계

- 진짜 QTE 요소가 없다: **타이밍 창, 입력 정확도, 연속/시퀀스 입력, 성공/실패 분기, 보상 차등**이 없다. 노출 동안 아무 때나 한 번 누르면 동일하게 성공한다.
- 단일 목적에 고정돼 있다. 처형(finish), 그로기 추격, 패링 후 반격, 컷신 인터랙션 등 다른 컨텍스트 입력으로 재사용할 구조가 아니다.
- `OnTakeSpecialBreakAttack`이 메인 `TakeDamage`/resolver/feedback 흐름을 우회한다(P0 평가의 "특수 진입점 누락" 항목과 동일 문제). QTE 성공 피해/리액션이 별도 경로라 전투 로그(P6)에 일관되게 남지 않는다.

### 개선 방향

기존 단일 입력 프롬프트를 **데이터 기반 컨텍스트 입력(QTE) 시스템**으로 일반화하고, 결과를 공통 pipeline에 태운다. 구조 자체보다 P1~P6에 올라타는 것이 핵심이다.

- `QtePromptSO` (신규 후보): 입력 타입(단일/연타/시퀀스/타이밍 바), 타이밍 창, 성공/실패/퍼펙트 임계값, 보상 프로필을 데이터화한다.
- `UI_BreakInteraction`을 `UI_QtePrompt`로 확장하거나 상위 타입을 두어, 단일 아이콘뿐 아니라 타이밍 게이지/시퀀스 표시를 지원한다(위치 추적·카메라 뒤 처리·펄스 로직은 기존대로 재사용).
- QTE 트리거 컨텍스트를 enum/태그로 분리: `BreakFinish`, `Groggy`, `ParryCounter`, `Cinematic` 등. `PlayerCombat`의 브레이크 전용 틱 로직을 컨텍스트 일반 탐색으로 일반화한다.
- **성공/실패/퍼펙트 결과를 `HitContext`/`CombatResult`로 표현**한다(P1 연계). `OnTakeSpecialBreakAttack`의 우회 경로를 `CombatResolutionPipeline`(P2)으로 편입해 피해/리액션/피드백/로그를 일반 공격과 같은 형식으로 남긴다.
- QTE 성공 시 리액션(처형/넘김 등)은 P5 정책 데이터(`CombatReactionPolicySO`)와 연계해 보스/엘리트별 면역·전용 연출을 데이터로 분기한다.

### 완료 기준

- 새 QTE 컨텍스트(예: 패링 후 반격)를 추가할 때 `QtePromptSO` 에셋과 트리거 컨텍스트만 추가하면 되고, UI/입력 코드를 새로 작성하지 않는다.
- QTE 성공/실패가 전투 로그(P6)에 일반 히트와 같은 형식으로 기록된다.
- 타이밍 창·퍼펙트 판정 등 QTE 수치를 데이터 검증기(P0)가 검사할 수 있다.

> 의존성: 이 항목은 P1(`HitContext`/`CombatResult`)과 P2(pipeline)에 의존한다. 그 전에는 "단일 입력 프롬프트" 유지로 충분하므로 우선순위는 가장 뒤에 둔다. 다만 P4에서 "특수 브레이크 진입점"을 pipeline에 편입할 때 이 항목의 기반 작업을 일부 함께 처리하는 것이 효율적이다.

---

## 6. 추천 구현 순서

| 순서 | 작업 | 이유 |
|------|------|------|
| 1 | P0 MotionSet / hitPhases 검증기 확장 | 데이터 오류를 먼저 잡아야 이후 구조 변경 검증이 쉬워진다 |
| 2 | P1 `HitContext` / `CombatResult` 도입 | legacy `AttackData` 의존을 줄이는 기준점 |
| 3 | P2 `CombatResolutionPipeline` 추가 | 분리된 resolver의 호출 순서를 표준화 |
| 4 | P3 Runner 중심 MotionEvent 전환 | 가장 큰 런타임 흐름 변경. 결과 객체와 pipeline 이후 진행 |
| 5 | P4 투사체 / AOE 공통 pipeline 연결 | 공격 타입별 분산을 줄임 |
| 6 | P5 방어 / 리액션 정책 데이터화 | 보스/Elite/상태별 규칙 확장 비용 감소 |
| 7 | P6 전투 로그 / 튜닝 리포트 | 밸런스 반복 속도 개선 |
| 8 | P7 QTE / 컨텍스트 입력 고도화 | P1·P2 기반 위에서만 가치가 있어 가장 뒤. P4의 특수 진입점 편입과 일부 병행 가능 |

> P6의 최소 형태(기존 `DamageResult`/`ReactionDecision`만 읽는 로그 레코더)는 P1 직후(P1.5)로 앞당기길 권장한다. 자동 테스트가 없는 프로젝트에서 P3 cutover의 "결과 100% 일치" 검증과 이후 모든 리팩터의 회귀 확인 도구가 되기 때문이다.

---

## 7. 리스크와 대응

| 리스크 | 대응 |
|--------|------|
| Runner 전환 중 기존 MotionEvent 데이터가 깨질 수 있음 | legacy 직접 호출 경로를 병행 유지하고 runner 경로와 결과를 비교한다. **단 P3의 cutover 기준(결과 200회 이상 100% 일치)을 충족하면 legacy 경로를 삭제**해 이중 유지보수를 종료한다 |
| legacy 병행 경로가 무기한 유지될 수 있음 | ~~cutover 종료 조건을 P3에 명시. 충족 시 `StartLegacyAction`/`MotionEvent_Collision` 직접 분기를 제거~~ → **해소(2026-06-04):** P3가 처음부터 relay 구조라 분기 자체가 없었음이 확인됨. 명명 정리(`StartAction`)·MotionEvent fallback 제거 완료, forwarding은 권위 쓰기로 영구 유지. P3 §"Legacy 경로 삭제(cutover) 기준" 참조 |
| `CombatResult` 도입이 과도한 추상화가 될 수 있음 | 방어/피해/리액션/피드백에 실제로 필요한 필드만 둔다 |
| 투사체/AOE까지 한 번에 통합하면 범위가 커짐 | 근접 → 투사체 → AOE → 잔류 공격 순서로 편입한다 |
| 정책 데이터가 너무 복잡해질 수 있음 | 보스 면역, Unblockable, HyperArmor 같은 실제 필요 규칙부터만 데이터화한다 |
| 검증기가 MotionSet 내부 구조에 강하게 의존할 수 있음 | 검증 로직은 에셋 검색/이벤트 추출 계층과 규칙 판정 계층을 분리한다 |

---

## 8. 1차 작업 체크리스트

다음 작업은 바로 착수 가능한 최소 단위다.

- [x] `CombatDataValidator`에서 `animKey` 기준 MotionSet 검색 API 추가 — `ResolveMotionSet` + `ActorAnimationMotionSet.GetMotionSet` 사용 (Enemy: `ActorDefinitionSO`, Player: `CharacterModelData` 프리팹 바인딩)
- [x] MotionSet 내 `BeginCollisionEvent` 목록 추출 — `MotionSetCombatEvents.Collect` (Collision/Telegraph 분리 수집)
- [x] `hitPhaseIndex` 범위 검증 — raw `hitPhaseIndex` vs `hitPhases.Count` 직접 비교
- [x] Melee 공격 collision 이벤트 누락 검증
- [x] Telegraph / Danger Ring 설정 검증 — `useMotionEventTelegraph`/`useTelegraphPositionForHit`/`Unblockable` 표현 누락 (적 전용 룰셋)
- [x] Markdown 리포트에 MotionSet 검증 결과 출력 — 기존 리포트 파이프라인으로 자동 출력(message에 animKey/phase 임베드)
- [x] `HitContext` 구조체 초안 추가
- [x] `CombatResult` 구조체 초안 추가
- [x] legacy `AttackData` → `HitContext` 변환 헬퍼 추가
- [x] `ResourceChangeSet` 구조체 초안 추가
- [x] `CombatLogRecorder` 최소 링버퍼 추가
- [x] `CombatResolutionPipeline` 1차 구현 추가
- [x] `PlayerActor.TakeDamage()` / `MonsterActor.TakeDamage()` 결과 조립과 로그 기록을 pipeline으로 이동
- [x] Poise/Break delta까지 `ResourceChangeSet`에 채우기 — `MonsterActor.OnDamaged`에서 실제 감소량 기록
- [x] 특수 브레이크 진입점 pipeline 편입 — `ResolveSpecialBreakHit` + `CombatLogRecorder`
- [x] 투사체 / AOE 진입점 pipeline 편입 — 피해 해결은 P2에서 `IDamageable.TakeDamage`로 수렴, attacker-side 피드백은 P4에서 통일
- [x] P3 1차: runner의 phase/collision 상태를 판정 직전에 참조
- [x] P3 2차: MotionEvent가 combat API 대신 runner timeline event를 직접 전달
- [x] P3 3차: collision window 소유권을 runner instance로 이전
- [x] P4: 투사체/AOE attacker-side 피드백을 `PlayerCombat` 공개 API로 통일
- [x] P5 1차: `CombatDefensePolicySO`, `CombatReactionPolicySO`, `CombatPolicyResolver` 추가
- [x] P5 1차: `ActorDefinitionSO` 정책 참조와 `DefenseResolver`/`ReactionResolver` 연동
- [x] P5 1차: `CombatDataValidator` 정책 검증 추가
- [x] P5 2차: 기본 정책 에셋 자동 생성 및 주요 Elite/Boss ActorDefinition 자동 연결 메뉴 추가
- [ ] P5 에셋 작업: 자동 생성 메뉴 실행 후 정책 에셋 수치 튜닝 및 Play Mode 검증
- [x] P6 1차: `CombatLogEntry` 기록 메타데이터 추가
- [x] P6 1차: CSV/Markdown 전투 로그 export 유틸 추가
- [x] P6 1차: `Combat Log Recorder` 에디터 창 추가
- [ ] P6 검증: Play Mode 전투 세션 기록 후 CSV/Markdown 리포트 확인

> P0 (MotionSet/hitPhases 검증기 확장) 구현 완료. 신규 파일: `Assets/02.Scripts/Tool/Editor/Combat/MotionSetCombatEvents.cs`, 확장: `CombatDataValidator.cs` (`ValidateMotionSetMatching` 패스). 검증 실행: `UPlayGround/Combat/Data Validator` → Validate All. 멀티 히트 phase-0-only는 Warning, 그 외 정합성 오류는 Error. 남은 체크리스트(HitContext/CombatResult)는 P1.

> P1/P1.5/P2 1차 구현 완료. 신규 파일: `HitContext.cs`, `CombatResult.cs`, `ResourceChangeSet.cs`, `CombatLogRecorder.cs`, `CombatResolutionPipeline.cs`. 현재 로그는 `CombatLogRecorder.Enabled = true`일 때 실제 피해 적용 결과만 인메모리 링버퍼에 기록한다. Poise/Break delta와 특수 브레이크는 pipeline 결과에 포함된다. 투사체/AOE는 후속 작업이다.

> P3/P4/P5 1차 구현 완료. P3는 MotionEvent의 actor 분기가 runner로 수렴하고 collision window는 runner instance가 소유한다. P4는 플레이어 투사체/AOE의 attacker-side 피드백을 근접 공격과 같은 공개 API로 통일했다. P5는 방어/리액션 정책 SO와 정책 검증을 도입했다. 남은 작업은 정책 에셋 제작/연결과 Play Mode 검증이다.

> P6 1차 구현 완료. 신규 메뉴: `UPlayGround/Combat/Combat Log Recorder`. Play Mode에서 `Enabled`를 켠 뒤 전투를 수행하고 CSV 또는 Markdown으로 내보낸다. Markdown은 예상 전투 시간 입력값과 실제 로그 duration의 차이를 함께 출력한다.

---

## 9. 판단

현재 프로젝트에는 Unreal GAS나 Unity DOTS를 그대로 도입할 필요가 없다. `Animancer MotionSet`, KCC 상태 머신, ScriptableObject 공격 데이터는 유지하는 것이 맞다.

대신 다음 원칙을 차용한다.

- GAS에서처럼 실행, 수치, 결과, 피드백을 분리한다.
- Unity ScriptableObject 권장 구조처럼 데이터는 에셋에 두고 런타임 상태는 별도 객체로 둔다.
- ECS 원칙처럼 판정/계산 시스템은 가능한 한 상태 없는 처리 계층으로 분리한다.
- AAA 액션 전투에서 중요한 튜닝 반복 속도를 위해 검증기와 전투 로그를 강화한다.

따라서 다음 핵심 목표는 `CombatActionRunner`와 `CombatResult`를 중심으로 legacy `AttackData` 직접 흐름을 줄이는 것이다.
