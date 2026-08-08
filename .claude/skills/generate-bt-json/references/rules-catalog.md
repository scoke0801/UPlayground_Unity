# Monster Behavior Rules 카탈로그

`MonsterBehaviorTreeJsonImporter`가 인식하는 **Rules JSON** 스키마의 condition/action/select 어휘와 enum 값. JSON을 만들 때 **반드시 여기 등록된 키와 enum 값만 사용한다.** 모르는 키/값은 import 시 Validator가 빨간 에러로 막는다.

> 권위 출처: `MonsterBehaviorJsonNodeKeys.cs`(키), `MonsterBehaviorTreeJsonImporter.Validation.cs`(검증), `...NodeFactory.cs`(스코프), 각 enum 정의.

## 필드 의미 — `value`는 condition마다 뜻이 다르다 (가장 흔한 실수)

- **거리 키 이름**: `DistanceLessOrEqual`/`DistanceGreater`의 `value`는 blackboard 거리 키 이름(`optimalCombatDistance`, `minCombatDistance`, `personalSpaceDistance`, `preferredRange`).
- **정수 리터럴**: `RecentHitCountGreaterOrEqual`, `ConsecutiveAttackCountLessThan`/`GreaterOrEqual`의 `value`는 `"5"` 같은 정수 문자열.
- **쿨다운 id(임의 문자열)**: `CooldownReady`의 `value`는 쿨다운 id. 같은 rule의 action `cooldownId`와 **문자열이 일치**해야 짝이 맞는다. ⚠️ 쿨다운을 **기록**하는 건 `RequestAction`/`Transition`뿐 — `ExecuteAttack`은 `cooldownId`를 무시한다. 따라서 `ExecuteAttack`을 `CooldownReady`로 게이팅하면 무효 게이트(항상 통과)다. **공격 rate-limit은 `ActionDelayElapsed`(+ 필요시 `ConsecutiveAttackCountLessThan`)로** 한다.
- **CombatIntent 이름**: `SelectedIntent`의 `value`는 CombatIntent 이름(아래 표). `Evade`/`None`은 불가.
- **ActorStateTag 이름**: `HasStateTag`의 `value`는 `Combat`/`Defensive`/`Locomotion`/`Airborne`/`Recovery`/`InterruptLocked`.
- **상태 이름**: `IsCurrentState`의 `value`는 상태 이름 문자열.
- **페이즈 이름/인덱스**: `IsEnemyPhase`의 `value`.
- **0~1 임계값**: `IsSelfLowHealth`의 `value`(생략 시 기본 임계값).

모든 condition은 `"invert": true`로 부정 가능. condition에 `value`가 명시 안 된 것들은 플래그성(파라미터 없음).

---

## Top-level 스키마

```jsonc
{
  "schemaVersion": 1,           // 항상 1
  "id": "EnemyBehavior_XXX",    // 필수. Generated 에셋 이름의 기반
  "displayName": "...",         // 그래프 표시명
  "actorKind": "Ground",        // "Ground" | "Flying"
  "sourceBehaviorSo": "Assets/.../BehaviorData_xxx.asset",  // 선택. 연결할 EnemyBehaviorSO 경로
  "blackboard": { ... },        // 튜닝값 (아래)
  "groups": [ ... ]             // 우선순위 그룹 (또는 최상위 "rules": [...])
}
```

- `groups` 또는 `rules` 중 **최소 하나** 필요. 보통 `groups`를 쓴다.
- 그룹/규칙은 `priority` 내림차순으로 평가(높을수록 먼저). 루트는 Selector — 위에서부터 첫 성공 규칙이 실행.
- 각 group: `{ name, priority, when?[], rules[] }`. `rules`는 비어 있으면 안 됨.
- 각 rule: `{ name, priority, when?[], 그리고 (do[] 또는 select+choices[]) }`.
  - `do`: 순차 실행 액션 리스트(Sequence).
  - `select: "WeightedRandom"` + `choices[]`: 가중 랜덤으로 하나 선택. choice는 action 필드 + `weight`(float).

## Blackboard 튜닝 키 (전부 선택, 생략 시 기본값)

| 키 | 타입 | 의미 |
|----|------|------|
| `tickInterval` | float | BT 틱 주기(초). 근접 공격형은 0.06~0.1 권장 |
| `enablePatrol` | bool | 무타겟 시 순찰 여부 |
| `optimalCombatDistance` | float | 선호 교전 거리 |
| `minCombatDistance` | float | 이보다 가까우면 너무 가까움 |
| `personalSpaceDistance` | float | 최소 개인 공간 |
| `preferredRange` | float | 유지 선호 거리 |
| `aggression` | 0~1 | 공격 성향 (스코어러 입력) |
| `reactionChance` | 0~1 | 플레이어 행동 반응 확률 |
| `counterChance` | 0~1 | 카운터 성향 |
| `guardChance` | 0~1 | 가드 성향 |
| `dodgeChance` | 0~1 | 회피 성향 |
| `retreatChance` | 0~1 | 후퇴 성향 |
| `punishRecoveryChance` | 0~1 | 회복 펀시 성향 |
| `antiGuardChance` | 0~1 | 가드 대응 성향 |
| `revengeChance` | 0~1 | 피격 보복 성향 |
| `circleWeight` | float | 서클링 가중 |
| `maxComboPressureCount` | int | 빠른 콤보 압박 한도(템포 레버) |
| `minRetreatCooldown` | float | 후퇴 최소 쿨다운 |

## Conditions

스코프: Common(지상·비행 공용) / Ground(actorKind=Ground 전용) / Flying(actorKind=Flying 전용).

| condition | 스코프 | value | 의미 |
|-----------|--------|-------|------|
| `HasTarget` | Common | — | 타겟 보유 (invert로 무타겟) |
| `IsBlockedEnemyState` | Common | — | Hit/Death/Grabbed/Airborne 등 행동 금지 상태 |
| `HasStateTag` | Common | ActorStateTag 이름 | 현재 상태가 해당 태그 보유 |
| `BlackboardCompare` | Common | `key`+`op`+(`value`\|`valueKey`) | 임의 blackboard 값 비교 |
| `IsEnemyPhase` | Common | 페이즈 이름/인덱스 | 현재 페이즈 일치 |
| `DistanceLessOrEqual` | Common | 거리 키 이름 | 타겟 거리 ≤ 값 |
| `DistanceGreater` | Common | 거리 키 이름 | 타겟 거리 > 값 |
| `ActionDelayElapsed` | Common | — | 다음 행동 허용 시각 경과(템포) |
| `CanUseSkill` | Ground | — | 현재 거리에 사용 가능 스킬 존재 |
| `CanActivateAbility` | Ground | — (`attackCategory` 필수 + `abilityRole` 선택) | 해당 카테고리·역할의 활성화 가능 Ability 존재. **공격 요청 직전의 빈 스윙 방지 가드** |
| `HasAttackInRange` | Ground | — | 현재 거리를 커버하는 공격이 하나라도 있음 (invert로 "사거리 밖" = 접근 필요) |
| `HasLineOfSight` | Common | — | 타겟까지 시야 확보 |
| `IsPlayerAttacking` | Common | — | 플레이어 공격 중 |
| `IsPlayerGuarding` | Common | — | 플레이어 가드 중 |
| `IsPlayerStaggered` | Common | — | 플레이어 경직 중 |
| `IsPlayerRecovering` | Common | — | 플레이어 후딜/회복 중 |
| `IsPlayerDodgingFrequently` | Common | — | 회피 빈번 |
| `IsPlayerAttackingFrequently` | Common | — | 공격 빈번 |
| `IsPlayerGuardingFrequently` | Common | — | 가드 빈번 |
| `IsPlayerRecoveringFrequently` | Common | — | 회복 빈번 |
| `RecentlyHitByPlayer` | Common | — | 최근 피격 |
| `HasAttackSlot` | Common | — | 공격 슬롯 확보(그룹 양보) |
| `CooldownReady` | Common | 쿨다운 id | 해당 쿨다운 준비됨 |
| `IsSelfLowHealth` | Common | 임계값(0~1, 선택) | 자신 저체력 |
| `WasLastHitHeavy` | Common | — | 마지막 피격이 강공격 |
| `IsPoiseBroken` | Common | — | 강인도 브레이크 상태 |
| `RecentHitCountGreaterOrEqual` | Common | int | 최근 피격 횟수 ≥ 값 |
| `ConsecutiveAttackCountLessThan` | Common | int | 연속 공격 < 값 |
| `ConsecutiveAttackCountGreaterOrEqual` | Common | int | 연속 공격 ≥ 값 |
| `CanIgnoreLightHit` | Common | — | 약공격 무시 가능(슈퍼아머) |
| `CanRevengeAfterHit` | Common | (선택) | 피격 후 보복 가능 |
| `SelectedIntent` | Common | CombatIntent 이름 | **스코어러가 고른 의도와 일치** (핵심) |
| `IsCurrentState` | Common | 상태 이름 | 현재 상태 일치 |
| `IsFlyingAirState` | Flying | — | 공중 상태 |
| `IsFlyingGroundCombatState` | Flying | — | 지상 교전 상태 |
| `IsAirAttackLimitReached` | Flying | — | 공중 공격 한도 도달 |
| `ShouldFlyingTakeOff` | Flying | — | 이륙 필요 |
| `FlyingCanUseSkill` | Flying | — | 비행 스킬 사용 가능 |
| `HasDiveSkillAvailable` | Flying | — | 다이브 스킬 가능 |
| `RollDiveChance` | Flying | — | 다이브 확률 굴림 |

## Actions (do[] / choices[])

| action | 스코프 | 필드 | 의미 |
|--------|--------|------|------|
| `KeepCurrentState` | Common | — | BT가 현재 상태를 덮지 않음(무한 Running) |
| `PatrolOrIdle` | Ground | — | 순찰 또는 idle |
| `Transition` | Ground | `state`(EnemyTransitionStateType), `cooldownId?`, `cooldownDuration?` | 상태로 직접 전이 |
| `RequestAction` | Common | `intent`(EnemyActionIntent), `style?`(EnemyActionStyle), `attackCategory?`, `abilityRole?`, `cooldownId?`, `cooldownDuration?` | 의도/스타일 요청 (Resolver가 상태 선택). **쿨다운을 기록하는 유일한 공격 경로** |
| `RequestAttackSlot` | Ground | — | 그룹 공격 슬롯 예약 |
| `ExecuteAttack` | Ground | `attackCategory?`(None=아무거나), `abilityRole?` | 공격 실행. ⚠ `cooldownId`를 기록하지 않는다 |
| `IssueAbilityTrigger` | Ground | `attackCategory`(None 불가), `abilityRole?` | 태그 트리거 경로로 Ability 발동 요청 |
| `Wait` | Common | `duration`(초) | 대기 |
| `FlyingTransition` | Flying | `state`(FlyingEnemyTransitionStateType) | 비행 상태 전이 |
| `FlyingPatrolOrIdle` | Flying | — | 비행 순찰/idle |
| `ResetFlyingCounters` / `ResetFlyingAirCounters` | Flying | — | 비행 카운터 리셋 |
| `DescendFlying` | Flying | — | 하강 |
| `RequestFlyingAttackSlot` | Flying | — | 비행 공격 슬롯 |
| `SelectFlyingDiveSkill` | Flying | — | 다이브 스킬 선택 |

choice는 위 action 필드에 `weight`(float, 기본 1.0) 또는 `weightKey`(blackboard 키)를 더한 형태.

## Enum 값

- **EnemyActionIntent** (`RequestAction.intent`): `None`, `Attack`, `Punish`, `Counter`, `Pressure`, `Chase`, `Retreat`, `KeepDistance`, `Defend`, `Evade`, `Recover`
- **EnemyActionStyle** (`RequestAction.style`): `None`, `Dodge`, `JumpBack`, `Guard`, `Circle`, `Flank`, `Charge`, `Dive`, `Land`, `TakeOff`, `Patrol`, `Idle`, `Step`
- **AbilityAttackCategory** (`attackCategory`): `None`, `Basic`, `Heavy`, `Skill`, `Any`
  - 요청 측 `None` = 카테고리 필터 없음. `CanActivateAbility`/`IssueAbilityTrigger`는 `None` 불가.
- **AbilityAIRole** (`abilityRole`): `Opener`, `Punish`, `GapCloser`, `Counter`, `Signature`, `Finisher`
  - flags라 `"Punish, Counter"` 처럼 쉼표 조합 가능. 대소문자 무시. `None`은 불가 — 필터를 안 걸려면 **필드를 생략**한다.
  - `CanActivateAbility` / `RequestAction` / `ExecuteAttack` / `IssueAbilityTrigger`만 파싱한다. 다른 노드에 붙이면 검증 오류.
  - 대상 Ability Payload의 `aiRoles`와 매칭된다. `aiRoles`가 `None`인 Ability는 **역할을 지정한 요청에 절대 잡히지 않는다.**
- **EnemyTransitionStateType** (`Transition.state`): `Idle`, `Patrol`, `Chase`, `Attack`, `Retreat`, `Circle`, `Guard`, `Charge`, `Flank`, `Counter`, `Dodge`, `JumpBack`, `Step`
- **CombatIntent** (`SelectedIntent.value`): `Attack`, `Punish`, `Counter`, `Pressure`, `Chase`, `Retreat`, `KeepDistance`, `Defend`, `Recover` (← `Evade`/`None` 없음)
- **ActorStateTag** (`HasStateTag.value`): `None`, `Locomotion`, `Combat`, `Defensive`, `Airborne`, `InterruptLocked`, `Recovery`
