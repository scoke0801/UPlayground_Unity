# 전투 시스템 다음 개선 제안

> 작성일: 2026-06-03  
> 대상 버전: Unity 6 (6000.0.60f1), URP  
> 분류: 웹 레퍼런스 기반 후속 개선 제안  
> 관련 문서: `Assets/docs/guide/COMBAT_SYSTEM_GUIDE.md`, `Assets/docs/TODO/COMBAT_SYSTEM_ARCHITECTURE_REFACTOR_PLAN.md`

---

## 1. 목적

현재 전투 시스템은 `DamageResolver`, `DefenseResolver`, `ReactionResolver`, `CombatHitDetector`, `CombatFeedbackDispatcher`, `CombatActionRunner`를 도입해 1차 책임 분리를 완료했다.

다만 아직 구조상 다음 한계가 남아 있다.

- `CombatActionRunner`는 기존 MotionEvent 직접 호출 경로와 병행되는 전환 계층이다.
- `AttackData`가 공격 정의, 런타임 히트 정보, 피격 결과 힌트를 함께 담는다.
- `DefenseResolver`, `DamageResolver`, `ReactionResolver`는 분리됐지만 하나의 `CombatResolutionPipeline`으로 묶여 있지는 않다.
- 투사체, AOE, 소환형 공격은 아직 근접 공격과 같은 판정/해결 경로를 완전히 공유하지 않는다.
- 데이터 검증기는 공격 SO 기본 검증까지만 담당하고, MotionSet 이벤트와 `hitPhases` 정밀 매칭은 후속 항목이다.

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

UPlayground에는 이미 `PlayerAttackDataSO`, `EnemyAttackDataSO`, `MotionSetAsset`, `ActorDefinitionSO`가 있으므로 전체 구조 전환보다 다음 원칙을 적용하는 것이 적절하다.

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

### 완료 기준

- `PlayerActor.TakeDamage()`와 `MonsterActor.TakeDamage()`가 계산 순서를 직접 나열하지 않는다.
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

### 완료 기준

- MotionEvent가 `PlayerCombat`/`EnemyCombat` 구현 세부를 직접 호출하지 않는다.
- `CombatActionRunner`만 현재 action, phase, collision window를 가진다.
- 기존 공격 데이터와 MotionSet은 수정 없이 호환된다.

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

- 투사체 히트도 `HitContext`를 만든다.
- AOE 히트도 `CombatResolutionPipeline`을 통과한다.
- 히트 로그와 피드백 정책이 근접/원거리/AOE를 같은 형식으로 기록한다.

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

### 완료 기준

- 새 몬스터 등급이나 상태별 리액션 규칙 추가 시 Actor 코드를 수정하지 않는다.
- 데이터 검증기가 정책과 공격 데이터 불일치를 잡는다.

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

### 완료 기준

- Play Mode에서 전투 로그를 CSV 또는 Markdown으로 내보낼 수 있다.
- 밸런스 디자이너 툴이 실제 전투 로그를 읽어 예상 전투 시간과 비교할 수 있다.

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

---

## 7. 리스크와 대응

| 리스크 | 대응 |
|--------|------|
| Runner 전환 중 기존 MotionEvent 데이터가 깨질 수 있음 | legacy 직접 호출 경로를 한동안 유지하고 runner 경로와 결과를 비교한다 |
| `CombatResult` 도입이 과도한 추상화가 될 수 있음 | 방어/피해/리액션/피드백에 실제로 필요한 필드만 둔다 |
| 투사체/AOE까지 한 번에 통합하면 범위가 커짐 | 근접 → 투사체 → AOE → 잔류 공격 순서로 편입한다 |
| 정책 데이터가 너무 복잡해질 수 있음 | 보스 면역, Unblockable, HyperArmor 같은 실제 필요 규칙부터만 데이터화한다 |
| 검증기가 MotionSet 내부 구조에 강하게 의존할 수 있음 | 검증 로직은 에셋 검색/이벤트 추출 계층과 규칙 판정 계층을 분리한다 |

---

## 8. 1차 작업 체크리스트

다음 작업은 바로 착수 가능한 최소 단위다.

- [ ] `CombatDataValidator`에서 `animKey` 기준 MotionSet 검색 API 추가
- [ ] MotionSet 내 `BeginCollisionEvent` 목록 추출
- [ ] `hitPhaseIndex` 범위 검증
- [ ] Melee 공격 collision 이벤트 누락 검증
- [ ] Telegraph / Danger Ring 설정 검증
- [ ] Markdown 리포트에 MotionSet 검증 결과 출력
- [ ] `HitContext` 구조체 초안 추가
- [ ] `CombatResult` 구조체 초안 추가
- [ ] legacy `AttackData` → `HitContext` 변환 헬퍼 추가

---

## 9. 판단

현재 프로젝트에는 Unreal GAS나 Unity DOTS를 그대로 도입할 필요가 없다. `Animancer MotionSet`, KCC 상태 머신, ScriptableObject 공격 데이터는 유지하는 것이 맞다.

대신 다음 원칙을 차용한다.

- GAS에서처럼 실행, 수치, 결과, 피드백을 분리한다.
- Unity ScriptableObject 권장 구조처럼 데이터는 에셋에 두고 런타임 상태는 별도 객체로 둔다.
- ECS 원칙처럼 판정/계산 시스템은 가능한 한 상태 없는 처리 계층으로 분리한다.
- AAA 액션 전투에서 중요한 튜닝 반복 속도를 위해 검증기와 전투 로그를 강화한다.

따라서 다음 핵심 목표는 `CombatActionRunner`와 `CombatResult`를 중심으로 legacy `AttackData` 직접 흐름을 줄이는 것이다.
