# 휴머노이드 캐릭터 보스전 설계 — Bokusei / Siuha / Lian / Hichi / Lili

> 작성일: 2026-08-06  
> 상태: 1차 데이터 제작 완료, Play Mode 밸런스 검증 대기  
> 범위: 기존 `MotionSetAsset`, Gameplay Ability System, Monster Behavior Rules JSON 재사용  
> 비범위: 신규 애니메이션 제작, 신규 BT 런타임 노드, 수치 리밸런싱, 아레나 기믹 구현

## 0. 결론

다섯 보스는 모두 현재 프로젝트의 휴머노이드 무기 세트를 기반으로 제작할 수 있다.

| 보스 | 기존 전투 기반 | 전투 정체성 | 플레이어에게 요구하는 대응 |
| --- | --- | --- | --- |
| Bokusei | Katana | 읽기, 짧은 연계, 빈틈 응징 | 연타 자제, 지연 타이밍 확인 |
| Siuha | SwordShield | 가드, 블록 후 반격, 전진 압박 | 가드 유도 후 후딜 응징, 브레이크 |
| Lian | Whip | 중거리 유지, 다단 구속, 회피 캐치 | 안쪽 또는 바깥쪽으로 거리 결단 |
| Hichi | DualBlade | 측면 이동, 빠른 다단, 템포 교란 | 패닉 회피 억제, 짧은 빈틈 확정 |
| Lili | GreatSword | 느린 선딜, 큰 커밋, 강한 응징 | 늦은 회피, 큰 후딜 집중 공격 |

구현의 핵심은 다음 세 층을 분리하는 것이다.

```text
BT
  → 지금 필요한 행동 카테고리와 이동 의도를 결정
  → Basic / Heavy / Skill 또는 Guard / Counter / Flank / Retreat 요청

GAS
  → HP 조건, 거리, 쿨다운, 선택 가중치로 실제 Ability 후보를 제한
  → Fork된 Payload가 기존 HitPhase와 MotionKey를 유지

Motion
  → ActorAnimationMotionSet이 MotionKey를 실제 MotionSetAsset으로 해석
  → 기존 Collision / Projectile / VFX 이벤트를 그대로 실행
```

BT는 Ability ID를 직접 지정하지 않는다. 보스별 정확한 기술 풀은 보스 전용 파생 `AbilitySetSO`와 Fork된 Payload의 `SelfHealthBased` 조건으로 페이즈별 제한하고, BT는 카테고리와 `AbilityAIRole`을 함께 요청한다.

## 1. 설계 전제와 안전 경계

### 1.1 현재 구현만 사용한다

- `Assets/docs/design/BOSS_HIERARCHICAL_PLAN_DESIGN.md`의 `BossPlanSO`, `BossPlanRunner`는 현재 코드에 구현되어 있지 않다.
- 이번 설계는 현재 동작하는 `EnemyBehaviorSO` 페이즈, Rules JSON의 `groups/rules`, `EnemyActionResolver`, `EnemyCombat`만 사용한다.
- 모든 보스는 `actorKind: Ground`다.
- 신규 Blackboard key나 신규 BT 노드는 필요하지 않다.
- 전조는 현재 `EnemyCombat`이 실제 지원하는 `Circle`만 사용한다. Cone/Line 전조를 전제로 설계하지 않는다.

### 1.2 영입 경로를 혼합하지 않는다

현재 다섯 `ActorDefinitionSO`에는 각각 `recruitableAs`가 설정되어 있다. 이 보스들을 플레이어블 캐릭터 해금전으로 쓸 경우 이 경로를 유지한다.

사이클 보스의 `BossAssist` 영입으로 사용할 경우에는 별도 보스 정의를 만들고 `recruitableAs: None`을 사용한다. `BossAssist`와 파티 캐릭터 해금을 동시에 한 처치 보상으로 묶지 않는다.

### 1.3 기존 공유 자산을 직접 바꾸지 않는다

각 보스는 다음 구조를 사용한다.

```text
공용 Humanoid AbilitySet
  → AbilitySet_Boss_<Character>의 baseSet
  → 선택 기술은 Ability + embedded Payload 안전 Fork 후 Replace
  → 사용하지 않는 AI 공격은 Remove
  → 보스 Profile/Definition만 파생 Set을 참조
```

- 원본 Ability의 피해, Poise 피해, 반응, Collision 수를 임의로 바꾸지 않는다.
- Fork된 Payload는 기존 MotionKey를 유지한다. 같은 액터의 기존 `ActorAnimationMotionSet` 매핑을 그대로 해석한다.
- 새 모션이 필요한 경우에만 새 MotionKey와 새 매핑을 만든다. 이번 1차 설계에는 새 모션이 필요하지 않다.
- 기존 원본 AbilitySet, 일반 휴머노이드 프리팹, 플레이어 데이터에는 영향이 없어야 한다.

## 2. 현재 자산 기준선

| 캐릭터 | 원본 AbilitySet | 액터 MotionSet | 확인된 정규 공격 수 | 현재 주의점 |
| --- | --- | --- | ---: | --- |
| Bokusei | `AbilitySet_Humanoid_KatanaAttackData` | `Humanoid_KatanaAnimationSet` | 33 | Skill 5/6에 카메라·타임스케일·적 정지 이벤트가 있어 보스 풀에서 제외 필요 |
| Siuha | `AbilitySet_Humanoid_SwordShieldAttackData` | `Humanoid_SwordShieldAnimationSet` | 25 | Counter 2종이 같은 GreatSword Counter 모션을 가리킴 |
| Lian | `AbilitySet_Humanoid_WhipAttackData` | `MonsterLian_AnimationSet` | 16 | 정규 공격 Motion에 MotionWarp가 없어 거리 유지 BT가 특히 중요 |
| Hichi | `AbilitySet_Humanoid_DualBladeAttackData` | `Humanoid_DualBladeAnimationSet` | 24 | Skill이 근접 다단형뿐이라 원거리 대응은 추격/측면 이동으로 해결해야 함 |
| Lili | `AbilitySet_Humanoid_GreatSwordAttackData` | `Humanoid_GreatSwordAnimationSet` | 22 | 모든 기존 공격의 `attackCategory=None`; 카테고리 요청 시 와일드카드처럼 섞여 선택됨 |

관련 원본 경로:

- GAS: `Assets/10.Datas/Ability/Actor/Humanoid_*AttackData/`
- Motion 매핑: `Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Humanoid/`
- Lian Motion 매핑: `Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Lian/MonsterLian_AnimationSet.asset`
- 현재 AI 프로필: `Assets/10.Datas/Actor/Enemy/BehaviorData/Humanoid/BehaviorData_<Character>.asset`
- 현재 공용 BT: `Assets/10.Datas/AI/BehaviorTree/SourceJson/EnemyBehavior_GroundMelee_Balanced.json`

### 2.1 현재 런타임 제약

1. `ExecuteAttack`과 `RequestAction`의 공격 지정 표면은 `Basic`, `Heavy`, `Skill` 카테고리와 `AbilityAIRole`이다. Ability ID를 직접 고르지 않는다.
2. `EnemyCounterState`는 `Counter` 역할 후보를 먼저 고르고, 해당 후보가 없을 때만 `Basic`으로 폴백한다.
3. `attackCategory=None`은 미설정 오류다. 명시적 와일드카드는 `Any`이며, 보스 Fork는 모두 구체 카테고리를 사용한다.
4. 보스 페이즈 인덱스는 기본 구간이 `-1`, 첫 번째 `BehaviorPhase`가 `0`, 두 번째가 `1`이다. 현재 60%/30% 경계를 유지하면 `-1 → 0 → 1`의 3구간이 된다.
5. Ability 쿨다운은 개별 기술 재사용을, BT `CooldownReady`는 패턴 빈도를 제어한다. 둘을 같은 값으로 취급하지 않는다.

## 3. 공통 보스 데이터 계약

### 3.1 생성 대상

보스마다 다음 데이터를 독립 생성한다.

```text
Assets/10.Datas/Ability/Actor/Boss/<Character>/
  AbilitySet_Boss_<Character>.asset
  GA_Boss_<Character>_*.asset

Assets/10.Datas/Actor/Enemy/BehaviorData/Boss/
  BehaviorData_Boss_<Character>.asset

Assets/10.Datas/Actor/Profiles/Monster/Boss/
  MonsterProfile_Boss_<Character>.asset

Assets/10.Datas/AI/BehaviorTree/SourceJson/Boss/
  Boss_<Character>_*.json
```

보스 전용 Prefab/ActorDefinition이 필요하면 원본 캐릭터 몬스터 Prefab을 Variant로 만들고, 보스 Profile과 파생 AbilitySet만 교체한다. 원본 일반 몬스터 자산을 보스 튜닝에 직접 사용하지 않는다.

### 3.2 GAS 공통 규칙

- 안정 ID는 `Boss.<Character>.<Role>.<Index>` 형식을 사용한다.
- 실행 표면은 모두 `additionalAbilities`다.
- 실제 공격만 `aiSelectable=true`로 둔다.
- `Basic`, `Heavy`, `Skill`을 반드시 명시한다. 특히 Lili의 원본 `None`을 그대로 두지 않는다.
- 페이즈 제한은 Fork된 Payload의 `conditionGroup`에 `SelfHealthBased`를 사용한다.
  - Phase 0: HP 60% 초과
  - Phase 1: HP 30~60%
  - Phase 2: HP 30% 이하
- HP 조건은 최소·최대 경계 포함 여부를 명시한다. Phase 0은 `(0.6, 1.0]`, Phase 1은 `(0.3, 0.6]`, Phase 2는 `[0, 0.3]`으로 겹치지 않는다.
- 기존 `HitPhaseData`, `hitPhaseIndex`, `hitboxGroupId`, `MotionKey`는 유지한다.
- 원본의 대부분이 `maxDistance=2.5`다. Motion/HitBox 사거리 분석 없이 숫자만 늘리지 않는다.
- Circle 전조를 추가할 기술은 실제 충돌 포즈의 `impactOffset/targetingRange`를 Dashboard에서 다시 베이크한 뒤 적용한다.
- 1차 버전은 신규 `GameplayEffectSO`를 만들지 않는다. 페이즈 차이는 Ability 후보와 `EnemyBehaviorSO`의 행동성으로 만든다.

### 3.3 BT 공통 골격

모든 보스 Rules JSON은 명시적 `groups`를 사용하고 루트 `rules`는 빈 배열로 둔다.

| 그룹 | 우선순위 | 책임 |
| --- | ---: | --- |
| `00 Survival And Acquire` | 1000 | 차단 상태 유지, 타깃 없음 처리 |
| `10 Phase Signature` | 960 | Phase 1/2의 대표 패턴 |
| `20 Immediate Reactions` | 930 | 공격 읽기, 가드, 회피, 피격 대응 |
| `30 Punish Windows` | 910 | 플레이어 경직/회복/반복 행동 응징 |
| `40 Execute Selected Intent` | 880 | Intent별 카테고리/이동 실행 |
| `50 Combat Rhythm` | 820 | 접근, 기본 연계, 연속 공격 제한, fallback |

필수 선두 규칙:

```text
IsBlockedEnemyState → KeepCurrentState
HasTarget(invert) → PatrolOrIdle
```

공통 안전 규칙:

- 모든 공격 규칙은 `HasTarget`, `ActionDelayElapsed`, 거리 조건을 조합한다.
- Skill/Heavy 요청 전에는 가능하면 `CanActivateAbility` 또는 `CanUseSkill`을 확인한다.
- 연속 공격 상한 뒤에는 `Circle`, `Step`, `Retreat`, `Guard` 중 하나로 호흡을 만든다.
- 최하단에는 항상 `KeepCurrentState` fallback을 둔다.
- 페이즈 대표기는 BT 쿨다운 6~10초의 시작값을 사용하고 Play Mode에서 조정한다.

## 4. Bokusei — 검로를 읽는 결투가

### 4.1 전투 목표

Bokusei는 가장 정석적인 1대1 검사다. 공격 수보다 “언제 칼을 뽑는가”가 위협이어야 한다. 플레이어가 연타하면 Counter/Dodge로 끊고, 회피를 반복하면 짧게 기다린 뒤 Heavy를 넣는다.

### 4.2 페이즈 흐름

| 구간 | 행동 | 플레이 감정 |
| --- | --- | --- |
| Phase 0, 100~60% | 1~3타 탐색, 원형 이동, 낮은 반격 빈도 | 정직한 결투, 타이밍 학습 |
| Phase 1, 60~30% | Dash 진입, 2~3히트 연계, 회복 후딜 응징 | 거리를 벌려도 안전하지 않음 |
| Phase 2, 30~0% | 6히트 검무와 투사체를 제한적으로 사용 | 짧고 강한 마지막 압박 |

### 4.3 Motion/GAS 풀

| 페이즈 | 원본 Ability ID | MotionSetAsset | 용도 |
| --- | --- | --- | --- |
| 0 | `Actor.Humanoid.KatanaAttackData.Attack.1.01` | `Katana_Combo_Attack_1_1` | 첫 탐색 베기 |
| 0 | `...Attack.2.02` | `Katana_Combo_Attack_1_2` | 탐색 연계 |
| 0 | `...Attack.3.03` | `Katana_Combo_Attack_1_3` | 짧은 마무리 |
| 0 | `...HeavyAttack.1.11` | `Katana_Heavy_Attack_1` | 확정 응징 |
| 1 | `...Attack.7.07` | `Katana_Combo_Attack_3_1` | 2히트 압박 |
| 1 | `...Attack.8.08` | `Katana_Combo_Attack_3_2` | 3히트 지연 연계 |
| 1 | `...DashAttack.1.17` | `Bokuse_Katana_DashAttack_1` | 거리 좁히기 |
| 1 | `...Skill.2.27` | `Katana_Skill_2` | 2히트 VFX 기술 |
| 2 | `...HeavyAttack.6.16` | `Katana_Combo_Attack_5_3` | 단발 마무리 |
| 2 | `...Skill.3.28` | `Katana_Skill_3` | 6히트 검무 |
| 2 | `...Skill.4.29` | `Katana_Skill_4` | 투사체 견제 |

Skill 5/6은 현재 카메라 잠금, 타임스케일, 적 정지 이벤트를 포함한다. 각 이벤트의 `EnemyExecutionPolicy`와 실제 시작 상태 기반 정리 경로 전체를 Play Mode에서 입증하기 전까지 Boss AbilitySet에서 Remove한다.

### 4.4 BT 특징 규칙

- `IsPlayerAttacking + 근거리 + cooldown` → Counter 45%, Dodge 35%, Step 20%.
- `IsPlayerDodgingFrequently` → `Wait(0.18)` 후 Heavy.
- `IsPlayerRecovering` → Phase 0은 Heavy, Phase 1은 Skill, Phase 2는 Skill 우선.
- Phase 1 이상에서 공격 사거리 밖이면 Charge보다 Chase/Dash 후보를 우선한다.
- 연속 공격 3회 뒤 Step/Circle로 한 번 숨을 고른다. Phase 2도 이 제한을 제거하지 않는다.

### 4.5 카운터플레이

- 첫 1~2타를 보고 반격 타이밍을 확인할 수 있어야 한다.
- 연타를 멈추면 Bokusei의 Counter 분기 가치는 떨어진다.
- 6히트 검무는 시작 후 강하지만 끝난 뒤 가장 큰 피해 창을 제공한다.

## 5. Siuha — 전진하는 성벽

### 5.1 전투 목표

Siuha는 방패를 단순 피해 감소가 아니라 플레이어 행동을 바꾸는 압박 장치로 쓴다. 무작정 공격하면 블록 후 `EnemyCounterState`로 이어지고, 기다리면 Guard 종료와 느린 Heavy 후딜을 노릴 수 있다.

### 5.2 페이즈 흐름

| 구간 | 행동 | 플레이 감정 |
| --- | --- | --- |
| Phase 0 | 짧은 검 연계와 제한적 Guard | 방패 규칙 학습 |
| Phase 1 | Guard/Counter 빈도 상승, 2히트 Heavy | 공격권을 쉽게 주지 않음 |
| Phase 2 | Guard 후 Skill, 큰 단발 Heavy, 짧은 전진 압박 | 성벽이 직접 밀고 들어옴 |

### 5.3 Motion/GAS 풀

| 페이즈 | 원본 Ability ID | MotionSetAsset | 용도 |
| --- | --- | --- | --- |
| 0 | `Actor.Humanoid.SwordShieldAttackData.Attack.1.01` | `Humanoid_SwordShield_Attack_1` | 기본 견제 |
| 0 | `...Attack.2.02` | `Humanoid_SwordShield_Attack_2` | 기본 연계 |
| 0 | `...Attack.10.10` | `Humanoid_SwordShield_Attack_10` | 2히트 마무리 |
| 0 | `...HeavyAttack.1.11` | `Humanoid_SwordShield_HeavyAttack_1` | 가드 후 응징 |
| 1 | `...HeavyAttack.2.12` | `Humanoid_SwordShield_HeavyAttack_2` | 2히트 압박 |
| 1 | `...HeavyAttack.4.14` | `Humanoid_SwordShield_HeavyAttack_4` | 지연 2히트 |
| 1 | `...HeavyAttack.6.16` | `Humanoid_SwordShield_HeavyAttack_6` | 단발 강공 |
| 1 | `...Skill.1.20` | `Humanoid_SwordShield_Skill_1` | 2히트 대표기 |
| 2 | `...HeavyAttack.9.19` | `Humanoid_SwordShield_HeavyAttack_9` | 최종 강공 |
| 2 | `...Skill.3.22` | `Humanoid_SwordShield_Skill_3` | 전진 Skill |
| 2 | `...Skill.4.23` | `Humanoid_SwordShield_Skill_4` | 느린 단발 대표기 |

Counter 1/2 원본은 둘 다 `Humanoid_GreatSwordAnimationSet_CounterAttack_1`을 가리킨다. 1차 보스 풀에서는 Remove한다. Guard 성공 후 Counter 상태는 파생 Set의 `Counter` 역할 공격을 먼저 선택한다.

### 5.4 BT 특징 규칙

- `IsPlayerAttacking + 근거리` → Guard 60%, Counter 20%, Step 20%.
- `IsPlayerAttackingFrequently` → Guard 비중을 높이되, 동일 쿨다운 동안 연속 Guard는 금지한다.
- `IsPlayerGuardingFrequently` → Heavy/Skill로 전환한다.
- Guard 종료 또는 연속 공격 2회 뒤 Circle로 측면을 다시 잡는다.
- Poise가 깨졌을 때 Guard로 즉시 복귀하지 않고 Step/Retreat로 취약 창을 보장한다.

### 5.5 카운터플레이

- 방패를 때리는 선택은 확실한 Counter 위험을 만든다.
- Guard가 끝날 때까지 기다리거나 뒤로 돌아가면 플레이어가 선공권을 되찾는다.
- 브레이크 성공 시 Siuha가 즉시 방어 루프로 복귀하지 않아야 한다.

## 6. Lian — 간격을 묶는 채찍술사

### 6.1 전투 목표

Lian은 화면 전체를 덮는 보스가 아니라, 플레이어가 애매한 중거리에 머무르는 습관을 처벌하는 보스다. 가까이 파고들지, 완전히 빠질지를 계속 선택하게 한다.

### 6.2 페이즈 흐름

| 구간 | 행동 | 플레이 감정 |
| --- | --- | --- |
| Phase 0 | 단발 채찍과 거리 재조정 | 안전 거리 탐색 |
| Phase 1 | 3~5히트 Heavy, 회피 방향 읽기 | 중거리 체류가 위험해짐 |
| Phase 2 | 6~7히트 연속기와 Critical Skill | 공간이 아니라 리듬에 갇힘 |

### 6.3 Motion/GAS 풀

| 페이즈 | 원본 Ability ID | MotionSetAsset | 용도 |
| --- | --- | --- | --- |
| 0 | `Actor.Humanoid.WhipAttackData.Attack.1.01` | `WhipCharacter_Attack_1` | 짧은 견제 |
| 0 | `...Attack.3.03` | `WhipCharacter_Attack_3` | 중간 견제 |
| 0 | `...Attack.6.06` | `WhipCharacter_Attack_6` | 단발 마무리 |
| 1 | `...HeavyAttack.2.08` | `WhipCharacter_HeavyAttack_2` | 3히트 |
| 1 | `...HeavyAttack.4.10` | `WhipCharacter_HeavyAttack_4` | 4히트 |
| 1 | `...Skill.1.12` | `WhipCharacter_Skill_1` | 5히트 대표기 |
| 2 | `...HeavyAttack.5.11` | `WhipCharacter_HeavyAttack_5` | 6히트 장기 압박 |
| 2 | `...Skill.2.13` | `WhipCharacter_Skill_2` | 7히트 연속기 |
| 2 | `...Skill.3.14` | `WhipCharacter_Skill_Critical` | 5히트 마무리 |

현재 정규 채찍 Motion에는 MotionWarp가 없다. BT에서 무조건 Charge를 요청하면 헛스윙과 미끄러짐이 늘 수 있으므로 Chase/Circle/Retreat로 먼저 거리를 만든 뒤 공격한다. `maxDistance`는 현재 2.5를 기준으로 시작하고, 실제 채찍 HitBox 도달 거리를 베이크해 검증한 뒤에만 확장한다.

### 6.4 BT 특징 규칙

- `DistanceLessOrEqual(minCombatDistance)` → Retreat 또는 JumpBack.
- `DistanceGreater(preferredRange)` → Chase. Charge는 사용하지 않는다.
- 적정 거리에서 Basic/Heavy를 섞고, Phase 1 이후 플레이어 회복 중에는 Skill을 사용한다.
- `IsPlayerDodgingFrequently` → `Wait(0.22)` 후 Heavy 또는 Skill로 회피 종료를 잡는다.
- 장기 다단기 뒤에는 최소 한 번의 Circle/Retreat를 강제한다.

### 6.5 카운터플레이

- 애매한 중거리 유지가 가장 위험하다.
- 안쪽으로 파고들면 Lian은 Retreat를 선택해 즉시 공격하지 못하는 구간이 생긴다.
- 6~7히트 연속기를 완전히 피하면 긴 모션 종료가 확정 응징 창이다.

## 7. Hichi — 템포를 훔치는 암살자

### 7.1 전투 목표

Hichi는 높은 단발 피해가 아니라 빠른 위치 변경과 다단기로 플레이어의 입력 템포를 무너뜨린다. 패닉 회피를 감지해 기다렸다가 Heavy로 잡는 것이 핵심이다.

### 7.2 페이즈 흐름

| 구간 | 행동 | 플레이 감정 |
| --- | --- | --- |
| Phase 0 | 짧은 1타와 측면 이동 | 위치 추적 학습 |
| Phase 1 | 2~3히트 Heavy와 Flank | 공격 방향이 빠르게 바뀜 |
| Phase 2 | 5~6히트 Skill, 짧은 재진입 | 템포가 급격히 빨라짐 |

### 7.3 Motion/GAS 풀

| 페이즈 | 원본 Ability ID | MotionSetAsset | 용도 |
| --- | --- | --- | --- |
| 0 | `Actor.Humanoid.DualBladeAttackData.Attack.1.01` | `Attack_1` | 첫 탐색 |
| 0 | `...Attack.2.02` | `Attack_2` | 짧은 연계 |
| 0 | `...Attack.8.08` | `Attack_8` | 2히트 |
| 0 | `...HeavyAttack.1.11` | `HeavyAttack_1` | 3히트 응징 |
| 1 | `...Attack.9.09` | `Attack_9` | 2히트 방향 전환 |
| 1 | `...HeavyAttack.5.15` | `HeavyAttack_5` | 2히트 |
| 1 | `...HeavyAttack.6.16` | `HeavyAttack_6` | 3히트 |
| 2 | `...HeavyAttack.7.17` | `HeavyAttack_7` | 단발 마무리 |
| 2 | `...Skill.1.21` | `Skill_1` | 6히트 회오리 |
| 2 | `...Skill.2.22` | `Skill_2` | 5히트 연속기 |

### 7.4 BT 특징 규칙

- `IsPlayerAttacking + 근거리` → Dodge 45%, Step 30%, Counter 25%.
- `IsPlayerRecovering` → Flank 후 Heavy. 단일 Rules JSON choice는 연속 action을 담지 못하므로, Flank와 공격은 별도 우선순위 규칙으로 이어 간다.
- `IsPlayerDodgingFrequently` → `Wait(0.12~0.18)` 후 Heavy.
- Phase 2에서도 연속 공격 상한은 3으로 유지한다. 상한 뒤 JumpBack/Flank를 거쳐 재진입한다.
- 원거리 공격이 없으므로 시야가 없거나 멀면 무리하게 Skill을 고르지 않고 Chase한다.

### 7.5 카운터플레이

- 첫 측면 이동을 보고 카메라를 다시 맞추는 것이 우선이다.
- 연속 회피는 Hichi의 지연 Heavy를 유도한다.
- 다단 Skill을 빗나가게 만들면 Hichi는 긴 시간 제자리에 남는다.

## 8. Lili — 한 번의 선택이 무거운 대검사

### 8.1 전투 목표

Lili는 가장 느리지만 가장 읽기 쉬운 보스다. 공격을 시작하면 크게 전진하거나 긴 선딜을 보이고, 빗나가면 확실한 보상을 준다. 난도는 반응 속도보다 회피 타이밍의 절제에서 나온다.

### 8.2 페이즈 흐름

| 구간 | 행동 | 플레이 감정 |
| --- | --- | --- |
| Phase 0 | 단발 Basic, 낮은 공격 빈도 | 큰 동작을 읽는 학습 |
| Phase 1 | 2히트 Heavy와 MotionWarp 진입 | 거리를 벌리기만 해서는 부족함 |
| Phase 2 | 긴 선딜 Skill, 낮은 빈도의 큰 응징 | 한 번의 실수가 크게 느껴짐 |

### 8.3 Motion/GAS 풀

| 페이즈 | 원본 Ability ID | 새 카테고리 | MotionSetAsset | 용도 |
| --- | --- | --- | --- | --- |
| 0 | `Actor.Humanoid.GreatSwordAttackData.Attack.1.01` | Basic | `Humanoid_GreatSwordAnimationSet_Attack_1` | 첫 탐색 |
| 0 | `...Attack.2.02` | Basic | `...Attack_2` | 기본 연계 |
| 0 | `...Attack.3.03` | Basic | `...Attack_3` | 단발 마무리 |
| 0 | `...HeavyAttack.1.11` | Heavy | `...HeavyAttack_1` | 2히트 |
| 1 | `...HeavyAttack.2.12` | Heavy | `...HeavyAttack_2` | 2히트 |
| 1 | `...HeavyAttack.5.15` | Heavy | `...HeavyAttack_5` | 전진 강공 |
| 1 | `...HeavyAttack.7.17` | Heavy | `...HeavyAttack_7` | MotionWarp 단발 |
| 1 | `...Skill.1.18` | Skill | `...Skill_1` | 대표 단발기 |
| 2 | `...Skill.2.19` | Skill | `...Skill_2` | 저빈도 강공 |
| 2 | `...Skill.3.20` | Skill | `...Skill_3` | 약 2.13초 뒤 충돌하는 긴 선딜기 |

Lili의 원본 Payload는 전부 `attackCategory=None`이다. 보스용 Fork에서는 위 표처럼 Basic/Heavy/Skill을 명시하지 않으면 BT 카테고리별 전투 리듬이 성립하지 않는다.

Counter 1/2도 같은 GreatSword Counter 모션을 공유한다. 1차 풀에서는 Remove하고, 일반 Guard/Counter 상태의 임의 공격 선택만 허용한다.

### 8.4 BT 특징 규칙

- 기본 공격성은 다섯 보스 중 가장 낮게 시작한다.
- `IsPlayerRecovering` → Heavy, Phase 2에서는 Skill.
- `IsPlayerDodgingFrequently` → `Wait(0.28)` 후 Skill. Skill 3의 긴 선딜과 겹쳐 과도하게 늦어지지 않는지 검증한다.
- `RecentlyHitByPlayer + CanIgnoreLightHit` → 저빈도 Heavy 보복. 매 피격마다 발동하지 않도록 별도 cooldown을 둔다.
- 연속 공격 2회 뒤 Circle/Guard/Idle 중 하나로 확실한 휴지기를 준다.
- Skill 3에는 실제 베이크된 Circle 전조를 붙이는 것을 우선 검토한다.

### 8.5 카운터플레이

- 너무 이른 회피는 긴 선딜 Skill에 잡힌다.
- 공격을 끝까지 보고 옆으로 피하면 가장 큰 후딜을 얻는다.
- 가벼운 연타보다 Poise/Break를 집중하는 플레이가 유리하다.

## 9. 보스별 `EnemyBehaviorSO` 1차 튜닝 방향

아래 값은 피해 수치가 아니라 AI 행동의 시작점이다. Play Mode에서 공격 밀도와 실제 Motion 길이를 보고 조정한다.

| 값 | Bokusei | Siuha | Lian | Hichi | Lili |
| --- | ---: | ---: | ---: | ---: | ---: |
| optimalCombatDistance | 2.4 | 2.2 | 2.4 | 2.0 | 2.6 |
| minCombatDistance | 1.1 | 1.0 | 1.4 | 0.8 | 1.3 |
| preferredRange | 2.6 | 2.3 | 2.6 | 2.1 | 2.8 |
| aggression | 0.66 | 0.55 | 0.58 | 0.74 | 0.48 |
| counterChance | 0.48 | 0.42 | 0.20 | 0.30 | 0.18 |
| dodgeChance | 0.30 | 0.16 | 0.20 | 0.52 | 0.12 |
| guardChance | 0.18 | 0.62 | 0.10 | 0.08 | 0.20 |
| maxComboPressureCount | 3 | 2 | 2 | 3 | 2 |

페이즈 공통 경계는 60%, 30%를 유지하되, 다음 방향으로 `BehaviorPhase`를 차별화한다.

- Bokusei: Counter/Punish/Pressure 가중치 상승.
- Siuha: Defend/Counter 가중치 상승, 마지막 페이즈에 Attack을 함께 상승.
- Lian: KeepDistance/Pressure 상승, Retreat는 중간 이상 유지.
- Hichi: Pressure/Chase 상승, Recover는 짧게 유지.
- Lili: Punish 상승, Attack 빈도는 급격히 올리지 않는다.

## 10. 제작 순서

### 단계 A — 안전한 데이터 분리

1. 보스 전용 Profile, BehaviorData, AbilitySet 경로를 만든다.
2. 원본 무기 AbilitySet을 `baseSet`으로 지정한다.
3. 표에 없는 AI 선택 공격은 Override `Remove`한다.
4. 표에 있는 공격은 Dashboard의 안전 Fork로 Ability + embedded Payload를 복제하고 Override `Replace`한다.
5. Request 전용 공용 Ability는 실제 보스 트리거에서 사용하지 않으면 Remove한다.

### 단계 B — GAS 정리

1. 안정 ID, 표시명, 카테고리, HP 조건, 선택 가중치를 설정한다.
2. MotionKey와 HitPhase를 원본과 대조한다.
3. Lili 카테고리를 모두 명시한다.
4. Bokusei Skill 5/6, Siuha/Lili 중복 Counter를 보스 풀에서 제외한다.
5. Circle 전조 후보는 Motion/HitBox 베이크 후 적용한다.

### 단계 C — BT JSON

1. 보스별 `SourceJson/Boss/*.json`을 생성한다.
2. `sourceBehaviorSo`는 새 보스 BehaviorData의 정확한 경로를 사용한다.
3. 공통 6개 group과 보스별 특징 규칙을 작성한다.
4. Phase 분기는 `IsEnemyPhase`의 `-1`, `0`, `1`을 사용한다.
5. 정적 validator 통과 후 선택 JSON만 Unity import한다.

### 단계 D — 연결

1. 보스 Profile과 ActorDefinition이 같은 AbilitySet/BehaviorData를 가리키는지 확인한다.
2. Prefab의 `ActorAnimator._motionSet`이 해당 캐릭터의 실제 MotionSet인지 확인한다.
3. 생성된 BT를 보스 BehaviorData에 연결한다.
4. 캐릭터 해금전과 Cycle BossAssist 중 어느 보상 경로인지 명시한다.

## 11. 검증 체크리스트

### Motion

- 모든 선택 Ability의 MotionKey가 자기 보스 MotionSet에서 해석된다.
- Motion의 최대 `hitPhaseIndex`가 Payload의 `hitPhases` 범위 안이다.
- Collision의 `hitboxGroupId`와 실제 Prefab HitBox가 일치한다.
- Lian의 실사거리, Lili Skill 3의 실제 선딜, Bokusei 투사체 사거리를 Play Mode에서 측정한다.
- Bokusei Skill 5/6을 제외한 상태에서 카메라/타임스케일/Freeze 이벤트 누수가 없다.

### GAS

- 파생 Set의 Replace/Remove 원본이 모두 baseSet의 유효 Ability다.
- `aiSelectable` 공격만 BT 후보가 된다.
- 모든 보스 공격의 카테고리가 명시적이다.
- HP 조건과 페이즈 경계에서 후보가 0개가 되는 구간이 없다.
- Ability 쿨다운과 BT 패턴 쿨다운이 함께 작동한다.
- `AbilityDataValidator.ValidateAll()`과 `MonsterAbilitySetIntegrationTests`를 통과한다.

### BT

- `IsBlockedEnemyState`와 타깃 부재 분기가 최상단이다.
- 각 phase signature가 의도한 카테고리를 실제로 실행한다.
- 공격 불가/거리 불일치 시 Chase/Retreat fallback으로 빠진다.
- 연속 공격 상한 뒤 휴지기가 발생한다.
- Counter가 특정 Counter Motion을 보장하지 않는 현재 제약이 디버그 Trace에서 혼동되지 않는다.
- 각 Rules JSON을 정적 validator `--strict`로 통과시킨다.

### Play Mode 보스별 스모크

| 보스 | 필수 확인 |
| --- | --- |
| Bokusei | 공격 읽기, Dash 접근, Skill 3 종료 후 응징 창, Skill 4 투사체 |
| Siuha | Guard 성공 → Counter, Poise Break 후 취약 창, 연속 Guard 제한 |
| Lian | 너무 가까울 때 Retreat, 다단기 후 호흡, HitBox 실사거리 |
| Hichi | Flank 재배치, 패닉 회피 캐치, Skill 후 공격 상한 |
| Lili | 카테고리 분리, 긴 선딜 전조, 큰 후딜, 공격 밀도 상한 |

## 12. 구현 결과와 완료 정의

2026-08-06 기준 다음 1차 데이터가 생성되었다.

- `AbilitySet_Boss_*` 5개와 보스 전용 Ability/Payload 51쌍
- `BehaviorData_Boss_*`, `MonsterProfile_Boss_*`, `MonsterBoss*` 각 5개
- `Boss_*.json` Source JSON 5개와 `BT_Boss_*.asset` Generated BT 5개
- 보스 Definition 5개의 `ActorDatabase` 등록 및 BehaviorData-BT 연결
- AI 역할 필터, Counter 역할 우선 선택, HP 경계 포함 정책, 적 Motion 전역 연출 안전 정책

남은 완료 조건은 Play Mode 체감/충돌 검증과 Player Build 재검증이다.

1. 다섯 보스가 서로 다른 `AbilitySet_Boss_*`, `BehaviorData_Boss_*`, Rules JSON을 사용한다.
2. 공유 원본 Ability와 MotionSet은 변경되지 않는다.
3. 각 보스의 Phase 0/1/2에서 의도한 Motion 풀만 선택된다.
4. Missing MotionKey, HitPhase, HitBox, managed reference, VFX 누락이 0이다.
5. Unity 컴파일 오류와 Play Mode 서비스 경고/예외가 0이다.
6. 보스별 카운터플레이가 실제 플레이에서 한 문장으로 설명 가능하다.
7. 파티 캐릭터 해금과 Cycle BossAssist 영입 경로가 데이터에서 명확히 분리되어 있다.
