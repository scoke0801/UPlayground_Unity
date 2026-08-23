# Humanoid 일반 몬스터 GAS · BT 제작 설계서

작성 2026-08-08 / 개정 2026-08-08(rev.2 — 보스 설계 반영, Bow 진단 정정).
대상은 `Enemy_Random_*` 계열 Humanoid 일반 몬스터 10종. 보스는 **작업 대상이 아니라 설계 레퍼런스**로 참조한다.

---

## 1. 대상과 범위

### 1.1 실제 배치된 Humanoid 일반 몬스터 (10종)

| ActorDef | 무기 | 프리팹 MotionSet | AbilitySet | BT | aiRole |
|---|---|---|---|---|---|
| `Enemy_Random_F_Bow_001` | Bow | `Humanoid_BowAnimationSet` | `AbilitySet_Humanoid_BowAttackData` | `BT_EnemyBehavior_SkeletonBow_AdaptiveRanged` | RangedMain(2) |
| `Enemy_Random_F_Bow_002` | Bow | 〃 | 〃 | 〃 | 〃 |
| `Enemy_Random_M_DualAxe_001` | DoubleAxe | `Humanoid_DoubleAxeAnimationSet` | `AbilitySet_Humanoid_DoubleAxeAttackData` | `BT_EnemyBehavior_GroundMelee_Balanced` | Melee(0) |
| `Enemy_Random_F_DualAxe_002` | DoubleAxe | 〃 | 〃 | 〃 | 〃 |
| `Enemy_Random_M_DualSword_002` | DualBlade | `Humanoid_DualBladeAnimationSet` | `AbilitySet_Humanoid_DualBladeAttackData` | 〃 | 〃 |
| `Enemy_Random_M_DualSword_003` | DualBlade | 〃 | 〃 | 〃 | 〃 |
| `Enemy_Random_M_GreatSword_001` | GreatSword | `Humanoid_GreatSwordAnimationSet` | `AbilitySet_Humanoid_GreatSwordAttackData` | 〃 | 〃 |
| `Enemy_Random_M_GreatSword_002` | GreatSword | 〃 | **SwordShield ← 연결 버그(확정)** | 〃 | 〃 |
| `Enemy_Random_M_SwordShield_002` | SwordShield | `Humanoid_SwordShieldAnimationSet` | `AbilitySet_Humanoid_SwordShieldAttackData` | 〃 | 〃 |
| `Enemy_Random_M_SwordShield_003` | SwordShield | 〃 | 〃 | 〃 | 〃 |

### 1.2 아키타입 — 5종

`GreatSword` / `DoubleAxe` / `DualBlade` / `SwordShield` / `Bow`.
`GreatSword_002`의 AbilitySet 오연결을 GreatSword로 교정하므로 개체 분포는 **GreatSword 2 / DoubleAxe 2 / DualBlade 2 / SwordShield 2 / Bow 2**가 된다.

### 1.3 범위 밖

- **보스 전부** — 참조만 한다 (§3).
- **`Katana` / `Speat`(Spear) / `Whip` / `Assassin` / `Staff` Humanoid AbilitySet** — 어떤 ActorDef도 참조하지 않는 미배치 데이터. 특히 Spear·Whip은 `Humanoid_SpearAnimationSet` / `Humanoid_WhipAnimationSet`에 `abilityMotions` 항목이 **0개**라 39개 Ability 전부 Motion 미해석이다. `GAMEPLAY_ABILITY_SYSTEM_SPEC.md`의 미해결 처리 방침대로 임의 매핑하지 않는다.
- 몬스터 스탯/포이즈/브레이크 게이지 밸런싱.

---

## 2. 현황 감사

174개 Humanoid GA 에셋과 대응 MotionSet을 전수 대조했다.

### 2.1 일괄 생성 흔적 (전 아키타입 공통)

| 필드 | 현재 | 보스(레퍼런스) | 문제 |
|---|---|---|---|
| `aiSelectable` | 174/174 true | 9~11개만 true | 후보 풀이 19~25개까지 부풀어 패턴 인지 불가 |
| `aiRoles` | Counter 16개만 `Counter`, 나머지 158개 `None` | **51/51 전부 의미 있는 flags** | BT가 역할을 요청해도 후보 0 → **역할 기반 선택이 죽어 있음** |
| `selectionWeight` | 10 / 3 / 1 세 값 | 4~12 연속 분포 | 카테고리별 기계 배분 |
| `cooldown.durationSeconds` | 2 / 3 / 4 세 값 | 2 / 3 / 4 (보스도 미정리) | 재사용 리듬 없음 |
| `conditionGroup.conditions` | 174/174 **빈 배열** | 51/51 **HealthBased 조건 보유** | 상황 게이팅 수단 자체가 없음 |
| `abilityTagIds` | 174/174 `[]` | 51/51 `[]` (보스도 미사용) | `EnemyCombatStrategySO` 태그 선호 작동 불가 |
| `useDangerRing` | 174/174 false | 25/51 true | 강공격 예고 없음 |
| `useTelegraph` | 174/174 false | 51/51 false (보스도 미사용) | — |

### 2.2 Bow 정밀 재검토 — 이전 진단 정정

> **이전 보고 정정.** "20m에서 발동해 1.5m 근접 판정을 굴린다", "히트페이즈 수가 안 맞는다" 두 가지는 기전을 잘못 짚었다. 실제 결함은 아래 한 가지다.

**단일 결정적 결함: `attackType`이 `Melee(0)`이다.**

`EnemyCombat.GetAvailableAbilities`는 `attackType == Melee`인 후보에만 `EnemyAttackRangePolicy.CoversDistance(useMeleeApproachRange: true)`를 추가로 건다. 이때 유효 최대 거리는 `activation.maxDistance`가 아니라 `ResolveEffectiveMaxDistance`가 계산한다:

```
attackType != Melee  →  authoredMax 그대로 반환 (= 20m)
attackType == Melee  →  min(authoredMax, max(targetingRange - 0.15, personalSpace + 0.5))
```

RandomBow는 `targetingRange 1.5`, BehaviorData `personalSpaceDistance 2.5`이므로
`min(20, max(1.35, 3.0)) = 3.0`.

즉 **궁수는 3m 안으로 붙기 전까지 활 Ability가 후보에서 아예 제외된다.** 20m에서 쏘고 빗나가는 게 아니라, 그 거리에서는 **발동 자체가 안 된다.** BT는 `RangedKiter`라 8m를 유지하려 하므로 두 로직이 서로 밀어내며 궁수가 아무것도 못 하는 상태가 된다.

**Skeleton_Bow가 정상인 이유**도 정확히 이 한 가지다. `GA_Skeleton_Bow_01_Attack_1`은 `attackType: Ranged(1)` → 클램프를 타지 않고 `authoredMax` 20m를 그대로 쓴다. 두 몬스터의 MotionEvent 파라미터는 `projectilePrefab`을 빼면 **완전히 동일**하다 (`spawnOffset (0,1,0)`, `useSpawnRotation 1`, `targetMode Forward`, `speed 10`, `duration 3`, `hitPhaseIndex -1`, `damage 10`).

→ **수정: RandomBow Payload 10건의 `attackType`을 `Ranged(1)`로.** 이것만으로 궁수가 동작한다.

**부수 사항 2건**

1. `projectilePrefab`이 `Nenmir_Default_Arrow`(플레이어 캐릭터 Nenmir용 화살)를 쓴다. Skeleton은 `DefaultArrow`를 쓴다. 몬스터간 투사체 공유가 허용됐으므로 **`DefaultArrow`로 통일**한다.
2. 모든 `SpawnProjectile` 이벤트가 `hitPhaseIndex: -1` + `damage: 10` 레거시 경로다 → GAS `hitPhases`(데미지·포이즈·리액션·브레이크)가 **전부 무시**되고 고정 10 데미지가 나간다. **단 이건 RandomBow 고유 문제가 아니라 Skeleton_Bow도 동일한 프로젝트 전역 레거시**다. 궁수 동작에는 지장이 없으므로 §7에서 **별도 선택 단계(P3-b)**로 분리한다. 이 경로를 고치면 `hitPhaseIndex`를 0..N-1로 결선해야 하고, 그때 비로소 Skill_2(2발)/Skill_4(3발)/Skill_5(4발)의 `hitPhases` 개수가 의미를 갖는다.

### 2.3 그 외 결함

| 항목 | 내용 |
|---|---|
| `GreatSword_22_Counter_Attack_2` | `hitPhases` 2개 / Motion `BeginCollisionEvent` 1개 → 2번째 페이즈 영구 미발화 |
| SwordShield Counter 1·2 | `Counter.Attack.1.24`와 `.2.25`가 **같은** `Humanoid_GreatSwordAnimationSet_CounterAttack_1` MotionSet을 가리킴 |
| `Enemy_Random_M_GreatSword_002` | AbilitySet만 SwordShield → **연결 버그 확정**, GreatSword Set으로 교정 |
| Katana Skill_4 / Skill_5 | 히트 이벤트 총합 ≠ `hitPhases` 수. §1.3에 따라 범위 밖, 기록만 |

### 2.4 BT 현황

- 근접 8종이 **`BT_EnemyBehavior_GroundMelee_Balanced` 하나를 공유**. 궁수 2종은 `SkeletonBow_AdaptiveRanged` 공유(스켈레톤 궁수용 트리를 그대로 빌려 쓰고 있었다).
- 근접 8종 BehaviorData 수치(거리·확률)가 **완전히 동일**.
- 10종 전부 `combatStrategy` = null, `phases` = 빈 배열.

### 2.5 도구 결함 — BT 정적 검증기가 낡았다

`validate_bt_json.py`가 `abilityRole` 필드를 모른다. 그 결과 **현행 보스 JSON 5개가 전부 검증에 실패**한다.

```
$ python .claude/skills/generate-bt-json/scripts/validate_bt_json.py .../Boss/Boss_Siuha.json
ERROR: $.groups[1].rules[0].when[2]: 지원하지 않는 필드: abilityRole
... 오류 10
```

임포터(`MonsterBehaviorTreeJsonImporter.NodeFactory.cs`)는 `CanActivateAbility` / `ExecuteAttack` / `IssueAbilityTrigger` / `RequestAction` 전부에서 `abilityRole`을 정상 파싱한다. 즉 **코드가 맞고 검증기가 낡았다.** 스킬 문서는 검증기가 C#에서 카탈로그를 읽는다고 설명하지만, 노드 키·enum만 그렇고 **필드 화이트리스트(`CONDITION_FIELDS`/`ACTION_FIELDS`/`CHOICE_FIELDS`)는 스크립트에 하드코딩**되어 있다. §7 P0에서 먼저 고친다.

---

## 3. 보스 설계 레퍼런스 — 따라야 할 기존 규약

보스 5종(Bokusei / Hichi / Lian / Lili / Siuha)의 GAS·BT를 분석했다. **일반 몬스터 설계는 이 규약을 그대로 승계한다.** 새 규약을 발명하지 않는다.

### 3.1 GAS — 역할 기반 소수 정예 풀

**보스 Ability는 Humanoid 공용 Set에서 안전 Fork한 것이다.** `GA_Boss_Siuha_0_Opener_01`의 `editorMemo`가 그대로 말한다:

> `Actor.Humanoid.SwordShieldAttackData.Attack.1.01`에서 안전 Fork. MotionKey/HitPhase 공유 기준 보존.

즉 **Humanoid 풀 → 역할 지명 → Fork**가 이미 확립된 파이프라인이다. 일반 몬스터는 Fork 없이 원본 Set을 큐레이션하는 점만 다르다.

| 규약 | 내용 |
|---|---|
| **에셋 명명** | `GA_Boss_<이름>_<HP밴드>_<Role>_<NN>` / `AbilityPayload_...` 동명. `abilityId` = `Boss.<이름>.<Role>.<NN>` |
| **풀 크기** | 보스당 **9~11개** (Bokusei 11, Hichi 10, Lian 9, Lili 10, Siuha 11) |
| **역할 어휘** | `AbilityAIRole` enum 그대로 — `Opener` / `Punish` / `Counter` / `GapCloser` / `Signature` / `Finisher` |
| **`aiRoles`** | 51/51 전부 설정. 주 역할 + 보조 flags (예: Counter_04 = `Punish\|Counter`, Signature_11 = `Signature\|Finisher`) |
| **Payload 분리** | Ability 에셋 안에 sub-asset으로 두지 않고 `Payloads/` 하위에 **독립 에셋**으로 둔다 |
| **HP 밴드 게이팅** | `conditionGroup`에 `HealthBased`(type 1) 조건 1개. 밴드 0 = 0.6~1.0, 1 = 0.3~0.6, 2 = 0.0~0.3. 파일명 접두 숫자와 일치 |
| **거리** | `activation.maxDistance`는 50/51이 **2.5 기본값 그대로**. 거리 변별은 손으로 안 넣고 `targetingRange` 기반 자동 계산에 맡긴다 |

**역할별 수치 규약 (Siuha 실측)**

| Role | `attackCategory` | `selectionWeight` | `useDangerRing` |
|---|---|---|---|
| Opener | Basic | 10~12 | false |
| Punish | Heavy | 7~9 | true |
| Counter | Heavy | 9 | true |
| Finisher | Basic/Heavy | 6~7 | true/false |
| GapCloser | Skill | 5 | true |
| Signature | Skill | 4~5 | true |

가중치가 **희소성에 반비례**한다(Opener 최대 → Signature 최소). `useDangerRing`은 Opener만 끈다.

### 3.2 BT — 5그룹 골격 + 역할 지명

보스 5개 JSON이 **동일한 5그룹 골격**을 쓴다.

| priority | 그룹 |
|---|---|
| 1000 | `00 Survival And Acquire` |
| 960 | `10 Phase Signature` |
| 930 | `20 Immediate Reactions` (Lian만 `20 Spacing Reactions`) |
| 910 | `30 Punish Windows` |
| 820 | `50 Combat Rhythm` |

**핵심 3가지 (기존 근접 BT와 다른 점)**

1. **`ExecuteAttack`이 아니라 `RequestAction`을 쓴다.** `ExecuteAttack`은 `cooldownId`를 무시하지만 `RequestAction`은 기록한다. 그래서 보스는 `CooldownReady` + `cooldownId` 짝으로 **정상 작동하는 쿨다운 게이트**를 갖는다.
2. **`abilityRole`로 Ability를 역할 지명한다.**
   ```jsonc
   { "condition": "CanActivateAbility", "attackCategory": "Skill", "abilityRole": "Signature" }
   { "action": "RequestAction", "intent": "Attack", "attackCategory": "Skill",
     "abilityRole": "Signature", "cooldownId": "SiuhaFinal", "cooldownDuration": 8.5 }
   ```
   `CanActivateAbility`로 **먼저 후보 존재를 확인**하고 같은 필터로 요청한다 — 빈 스윙 방지.
3. **`40 Execute Selected Intent` 그룹이 없다.** 보스는 `IsEnemyPhase` + 역할 지명으로 결정론적으로 짠다.

### 3.3 승계 여부 판단

| 보스 규약 | 일반 몬스터 적용 |
|---|---|
| 역할 어휘(`AbilityAIRole`) + `aiRoles` 전수 설정 | **승계** — 초안의 자체 "Slot" 용어를 폐기하고 enum 이름으로 통일 |
| 풀 9~11개 | **승계** (§9 Q1 미결) |
| 역할별 weight/dangerRing 규약 | **승계** |
| `maxDistance` 손대지 않고 자동 계산에 맡김 | **승계** — 초안의 수기 거리 표(5×4)를 폐기하고 `MonsterMeleeRangeBakeTool.BakeAll()` 사용 |
| `conditionGroup` 조건 게이팅 | **변형** — 일반 몬스터는 HP 페이즈가 없다. HealthBased 대신 필요한 곳에만 `RangeBased` |
| Payload 독립 에셋 | **비승계** — 일반 몬스터는 Fork가 아니라 기존 에셋 수정이므로 현재 sub-asset 구조 유지 |
| `RequestAction` + `abilityRole` + `CooldownReady` | **승계** |
| BT 5그룹 골격 | **부분 승계** — `10 Phase Signature`는 페이즈가 없으므로 `10 Signature Window`로 대체 |
| `40 Execute Selected Intent` 생략 | **비승계** — §6.2 참조 |

---

## 4. 설계 원칙

메모 「몬스터 AI 레이어 귀속」의 3층 분리를 유지한다.

| 레이어 | 소유 | 이번에 정하는 것 |
|---|---|---|
| **결정** | BT Rules JSON + 스코어러 | Intent, 그룹 우선순위, 역할 지명 |
| **페이싱** | BehaviorData blackboard + `EnemyCombatStrategySO` | aggression, 콤보 압박 한도, 반복 억제 |
| **텔레그래프** | GAS Payload | 역할, 가중치, 쿨다운, 예고, 히트 페이즈 |

1. **BT는 Ability를 지목하지 않는다.** `attackCategory` + `abilityRole`까지만 말한다. 실제 선택은 `EnemyAbilitySelectionPolicy` + 가중 랜덤.
2. **`ExecuteAttack`을 쓰지 않는다.** 쿨다운을 기록하지 못한다. 보스 규약대로 `RequestAction`으로 통일.
3. **`CanActivateAbility` 없이 `RequestAction` 하지 않는다.** 후보가 0이면 빈 스윙이 된다.
4. **거리는 손으로 넣지 않는다.** `MonsterMeleeRangeBakeTool`이 실제 공격 포즈에서 뽑는다.

---

## 5. 아키타입 정의

각 아키타입은 "플레이어가 3초 안에 읽어야 하는 한 문장"을 갖는다. 수치와 문장이 어긋나면 문장이 이긴다.

| 아키타입 | 한 문장 | 교전거리 | 압박 수단 | 플레이어의 답 |
|---|---|---|---|---|
| **GreatSword** — 확정형 처형자 | 느리게 감았다 크게 쓸어친다 | 2.8 / min 1.4 | 예고 긴 대형 Heavy, 포이즈 우위 | 예고 구간이 길다 → 회피 후 반격창 넓음 |
| **DoubleAxe** — 광폭 압박형 | 멈추지 않고 몰아친다 | 2.3 / min 1.0 | 연속 Basic(최대 4연), 낮은 가드율 | 콤보 종료 후딜이 길다 |
| **DualBlade** — 기동 교란형 | 붙었다 빠지며 각을 바꾼다 | 2.0 / min 0.9 | 짧은 Basic 러시 + 잦은 서클링 | 타격이 약하고 포이즈가 낮다 |
| **SwordShield** — 방벽형 | 막고 버티다 되받는다 | 2.4 / min 1.1 | 높은 guard/counter | 가드를 못 뚫으면 지루 |
| **Bow** — 거리 유지형 | 거리를 벌리고 쏜다 | 8.0 / min 4.0 | 원거리 견제, 접근 시 이탈 | 근접에 무력 |

---

## 6. GAS 설계

### 6.1 성격 — 신규 생성이 아니라 큐레이션

174개 Ability와 Motion 매핑은 이미 있다. 새 GA 에셋은 만들지 않고 **기존 Ability/Payload 필드를 재저작**한다.

### 6.2 역할 배정 — `aiRoles` (최우선 작업)

158개가 `None`이라 역할 기반 선택이 죽어 있다. **이 항목 하나가 보스와 일반 몬스터를 가르는 가장 큰 격차**다.

아키타입당 배정 (보스 풀 크기 9~11 승계):

| Role | 개수 | `attackCategory` | `aiRoles` flags | `selectionWeight` | `useDangerRing` |
|---|---|---|---|---|---|
| `Opener` | 2 | `Basic` | `Opener` | 12 / 10 | false |
| `Punish` | 2 | `Heavy` | `Punish` | 9 / 7 | true |
| `Counter` | 1 | `Heavy` | `Punish\|Counter` | 9 | true |
| `Finisher` | 2 | `Basic`/`Heavy` | `Finisher` | 7 / 6 | true |
| `GapCloser` | 1 | `Skill` | `GapCloser` | 5 | true |
| `Signature` | 1~2 | `Skill` | `Signature`(+`Finisher`) | 5 / 4 | true |

합계 **9~10개**. 나머지는 `aiSelectable = false` (§9 Q1 미결 — 확정 전까지 P4에서 실행하지 않는다).

**아키타입별 지명 (GreatSword 확정안)**

| Role | Ability | MotionKey |
|---|---|---|
| Opener 01 | `..._01_Attack_1` | `Humanoid.GreatSwordAttackData.Attack.1.01` |
| Opener 02 | `..._03_Attack_3` | `.Attack.3.03` |
| Punish 03 | `..._11_HeavyAttack_1` | `.HeavyAttack.1.11` |
| Punish 04 | `..._14_HeavyAttack_4` | `.HeavyAttack.4.14` |
| Counter 05 | `..._21_Counter_Attack_1` | `.Counter.Attack.1.21` |
| Finisher 06 | `..._02_Attack_2` | `.Attack.2.02` |
| Finisher 07 | `..._16_HeavyAttack_6` | `.HeavyAttack.6.16` |
| GapCloser 08 | `..._20_Skill_3` | `.Skill.3.20` |
| Signature 09 | `..._18_Skill_1` | `.Skill.1.18` |

나머지 4종은 P2에서 같은 형식으로 확정한다. 선정 기준은 **Motion의 `BeginCollisionEvent` 수와 활성 구간 길이** — 같은 역할 안에서 리듬이 다른 것을 고른다.

### 6.3 거리 — 수기 금지, 자동 베이크

보스가 `maxDistance`를 2.5 기본값으로 둔 것은 방치가 아니라 **의도된 위임**이다. 실제 유효 거리는 `ResolveEffectiveMaxDistance`가 `targetingRange`에서 계산한다.

따라서 `MonsterMeleeRangeBakeTool.BakeAll()`을 돌린다. 이 도구는 몬스터 프리팹의 실제 공격 포즈와 부착 HitBox를 샘플링해 `activation.maxDistance`를 산출한다. 인자 없는 `public static void`라 batchmode에서 직접 호출 가능하다.

> Bow는 `attackType`이 `Ranged`로 바뀌면 클램프를 타지 않으므로 `maxDistance` 20을 **수기로 유지**한다 (베이크 대상 아님).

### 6.4 쿨다운

| Role | `durationSeconds` | `cooldownGroupId` |
|---|---|---|
| Opener | 0.6 | — |
| Punish | 6.0 | `Humanoid.<무기>.Heavy` |
| Counter | 5.0 | `Humanoid.<무기>.Heavy` |
| Finisher | 4.0 | — |
| GapCloser | 8.0 | `Humanoid.<무기>.Skill` |
| Signature | 12.0 | `Humanoid.<무기>.Skill` |

그룹 ID로 묶어 Heavy 두 개가 연달아 나오지 않게 한다.

### 6.5 Bow 수정 (§2.2)

| # | 대상 | 변경 |
|---|---|---|
| 1 | Payload 10건 | `baseInfo.attackType` `Melee(0)` → `Ranged(1)` |
| 2 | MotionEvent 14건 | `projectilePrefab` `Nenmir_Default_Arrow` → `DefaultArrow` |
| 3 | Counter 2건 | `Melee` 유지, `maxDistance` 2.5 유지 |
| (P3-b) | MotionEvent 14건 | `hitPhaseIndex` −1 → 0..N-1, Payload `hitPhases` 개수 정합 — **선택** |

### 6.6 `EnemyCombatStrategySO` 5개 신설

10종 전부 `combatStrategy`가 null이다. (보스도 null이므로 이건 보스 승계가 아니라 **신규 도입**이다.) 아키타입당 1개.

| 필드 | GreatSword | DoubleAxe | DualBlade | SwordShield | Bow |
|---|---|---|---|---|---|
| `repeatedAbilityScoreMultiplier` | 0.30 | 0.55 | 0.50 | 0.40 | 0.45 |
| `maxConsecutiveSameAbility` | 1 | 3 | 2 | 2 | 2 |
| `minimumCommitmentSeconds` | 0.35 | 0.12 | 0.10 | 0.20 | 0.15 |
| `groupPressureMultiplier` | 0.8 | 1.2 | 1.1 | 0.9 | 0.7 |
| `intentWeights` | `IW_AggressiveMelee` | `IW_AggressiveMelee` | `IW_Default_Melee` | `IW_DefensiveShield` | `IW_RangedCaster` |

`preferredAbilityTags`는 `abilityTagIds`가 전 프로젝트에서 비어 있으므로 이번엔 **비워 둔다** (보스도 미사용). 태그 체계는 별도 과제.

저장: `Assets/10.Datas/Actor/Enemy/BehaviorData/Strategy/`.

### 6.7 기타 결함 수정

- `GreatSword_002_ActorDef.abilitySet` → `AbilitySet_Humanoid_GreatSwordAttackData`.
- `GreatSword_22_Counter_Attack_2`의 `hitPhases` 2 → 1.
- SwordShield Counter 2가 Counter 1과 같은 Motion → Counter 1만 `aiSelectable`, Counter 2는 내린다.

---

## 7. BT 설계

### 7.1 산출물

```
Assets/10.Datas/AI/BehaviorTree/SourceJson/Humanoid/
  EnemyBehavior_Humanoid_GreatSword.json
  EnemyBehavior_Humanoid_DoubleAxe.json
  EnemyBehavior_Humanoid_DualBlade.json
  EnemyBehavior_Humanoid_SwordShield.json
  EnemyBehavior_Humanoid_Bow.json
```

**출발점은 `Boss_Siuha.json`** (기존 `GroundMelee_Balanced`가 아니다). 보스 골격이 `abilityRole` 지명과 `RequestAction` 쿨다운을 쓰는 최신 규약이기 때문이다.

### 7.2 그룹 골격 — 보스 5그룹 + `40` 복원

| priority | 그룹 | 출처 |
|---|---|---|
| 1000 | `00 Survival And Acquire` | 보스 그대로 |
| 960 | `10 Signature Window` | 보스 `10 Phase Signature`에서 `IsEnemyPhase` 제거, `CanActivateAbility`+`abilityRole: Signature` 유지 |
| 930 | `20 Immediate Reactions` | 보스 그대로 |
| 910 | `30 Punish Windows` | 보스 그대로 |
| **880** | **`40 Execute Selected Intent`** | **근접 BT에서 복원** |
| 820 | `50 Combat Rhythm` | 보스 그대로 |

**`40` 그룹을 보스와 달리 유지하는 이유.** 임포터가 루트 Selector에 `EvaluateEnemyCombatIntentService`를 무조건 부착해 매 틱 `Decision.SelectedIntent`를 계산한다. 이 값을 읽는 규칙이 하나도 없으면 계산 비용만 내고 결과를 버리는 스코어러 우회가 된다. 보스는 `IsEnemyPhase` 기반 결정론이 개성을 만들지만, **페이즈가 없는 일반 몬스터에서 `40`까지 빼면 남는 건 거리 분기뿐**이라 8종이 다시 균질해진다. `10~30`(역할 지명, 결정론)과 `40`(스코어러, 확률적 변주)을 **둘 다** 쓴다.

### 7.3 아키타입별 분기 차이

| | GreatSword | DoubleAxe | DualBlade | SwordShield | Bow |
|---|---|---|---|---|---|
| `10 Signature` 조건 | `IsSelfLowHealth` | `ConsecutiveAttackCount≥3` | `IsPlayerRecovering` | `IsPlayerGuardingFrequently` | `DistanceGreater preferredRange` |
| `30 Punish` role/cat | `Punish`/`Heavy` | `Finisher`/`Basic` | `Finisher`/`Basic` | `Punish`/`Heavy` | `Signature`/`Skill` |
| `IsPlayerAttacking` | `Defend Guard` | 무시(계속 공격) | `Evade Dodge` | `Defend Guard`→`Counter` | `Retreat JumpBack` |
| `IsPlayerGuardingFrequently` | `Punish`/`Heavy` | `Punish`/`Heavy` | `KeepDistance Flank` | `Signature`/`Skill` | `KeepDistance` |
| `IsPlayerDodgingFrequently` | `Wait` 0.4 후 `Punish` | `Opener` 연타 | `Chase Step` | `Wait` 0.5 | `Wait` 0.3 후 `Signature` |
| `50` 연속타 한도 | 2 | 4 | 3 | 2 | 2 |
| `50` 기본 | `Opener`↔`Finisher` 교대 | `Opener` 연타 | `Opener`+`Circle` | `Opener`+`Guard` 교대 | `Opener` 사격+`Circle` |
| 너무 가까울 때 | `KeepDistance Step` | 무시 | `Evade Dodge` | `Defend Guard` | `Retreat JumpBack` |

모든 공격 규칙은 `CanActivateAbility`(같은 `attackCategory`+`abilityRole`) 선행 확인 → `RequestAction` 순서를 지킨다.

### 7.4 Blackboard

| 키 | GreatSword | DoubleAxe | DualBlade | SwordShield | Bow |
|---|---|---|---|---|---|
| `tickInterval` | 0.08 | 0.06 | 0.06 | 0.07 | 0.10 |
| `optimalCombatDistance` | 2.8 | 2.3 | 2.0 | 2.4 | 8.0 |
| `minCombatDistance` | 1.4 | 1.0 | 0.9 | 1.1 | 4.0 |
| `personalSpaceDistance` | 1.0 | 0.8 | 0.75 | 0.85 | 2.0 |
| `preferredRange` | 3.0 | 2.4 | 2.2 | 2.6 | 8.0 |
| `aggression` | 0.55 | 0.85 | 0.70 | 0.50 | 0.40 |
| `reactionChance` | 0.50 | 0.45 | 0.70 | 0.70 | 0.55 |
| `counterChance` | 0.30 | 0.15 | 0.35 | 0.65 | 0.10 |
| `guardChance` | 0.20 | 0.05 | 0.15 | 0.60 | 0.05 |
| `dodgeChance` | 0.15 | 0.10 | 0.55 | 0.20 | 0.50 |
| `retreatChance` | 0.12 | 0.05 | 0.30 | 0.15 | 0.55 |
| `punishRecoveryChance` | 0.75 | 0.70 | 0.65 | 0.60 | 0.55 |
| `antiGuardChance` | 0.70 | 0.55 | 0.35 | 0.45 | 0.20 |
| `revengeChance` | 0.35 | 0.60 | 0.40 | 0.30 | 0.20 |
| `circleWeight` | 0.25 | 0.20 | 0.65 | 0.35 | 0.55 |
| `maxComboPressureCount` | 2 | 4 | 3 | 2 | 2 |
| `minRetreatCooldown` | 3.2 | 4.0 | 1.6 | 2.8 | 1.0 |
| `enablePatrol` | true | true | true | true | true |

⚠ **Bow의 `personalSpaceDistance`는 2.5 → 2.0으로 내린다.** 현재 2.5가 §2.2의 클램프를 3.0m까지 밀어올린 공범이다. `attackType` 수정으로 클램프 자체는 사라지지만, 근접 Counter Ability의 유효 거리에는 계속 영향을 준다.

BehaviorData 에셋의 거리 필드도 같은 값으로 맞춘다 — 두 곳이 어긋나면 스코어러 입력과 BT 분기가 서로 다른 거리를 본다.

### 7.5 BehaviorData 10개 갱신

`behaviorTree` → 새 BT 5개 / `combatStrategy` → §6.6 / `intentWeights` → §6.6 / 거리·확률 → §7.4 / `aiRole` 유지.

---

## 8. 실행 계획 — 전 단계 Claude 수행

Unity 6000.3.21f1이 `C:\Program Files\Unity\Hub\Editor\6000.3.21f1`에 설치돼 있다. **에디터 batchmode `-executeMethod`로 데이터 저작·연결·검증을 직접 수행한다.** 사용자 수작업 단계는 없다.

```
"C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe" -batchmode -quit \
  -projectPath "C:\UsingProject\UnityProject\UPlayground" \
  -executeMethod UPlayGround.Editor.HumanoidAuthoringBatch.<Step> \
  -logFile <scratchpad>/<step>.log
```

| 단계 | 내용 | 수단 |
|---|---|---|
| **P0** | `validate_bt_json.py` 필드 화이트리스트에 `abilityRole` 추가 + `rules-catalog.md` 갱신 (§2.5). 보스 5개 JSON 재검증으로 회귀 확인 | 직접 편집 |
| **P1** | `HumanoidAuthoringBatch` 신설 — Preview/Apply, 단일 Undo 그룹, 예외 시 `Undo.RevertAllDownToGroup` 전체 롤백 (CLAUDE.md 안전 규칙). **에셋 생성·삭제 없이 필드 수정만** | C# 신규 |
| **P2** | 나머지 4 아키타입 역할 지명 확정 (§6.2) — Motion 활성 구간 재조사 후 표 작성 | 정적 분석 |
| **P3-a** | Bow 필수 수정 (§6.5 1~3) | batchmode |
| **P3-b** | *(선택)* `hitPhaseIndex` 레거시 해소 — §9 Q2 | batchmode |
| **P4** | 역할·가중치·쿨다운·DangerRing 일괄 적용 (§6.2/6.4) + §6.7 결함 수정. **`aiSelectable` 축소는 Q1 확정 후** | batchmode |
| **P5** | `MonsterMeleeRangeBakeTool.BakeAll()` (§6.3) | batchmode |
| **P6** | `EnemyCombatStrategySO` 5개 생성 (§6.6) | batchmode |
| **P7** | BT Rules JSON 5개 작성 + 정적 검증 | Write + python |
| **P8** | JSON import → BehaviorData 10개 재배선 (§7.5) | batchmode |
| **P9** | `AbilityDataValidator` 전수 + 통합 테스트 | batchmode |

**batchmode 제약 (사전 인지)**

- 프로젝트가 Unity 에디터에서 열려 있으면 프로젝트 락으로 실패한다 → 실행 전 확인, 열려 있으면 사용자에게 종료를 요청한다.
- 첫 실행은 컴파일·임포트로 수 분 걸린다. 타임아웃을 넉넉히 잡고 백그라운드로 돌린다.
- 임포터의 `Import Monster Behavior Json To BT`는 `[MenuItem("Assets/...")]`이라 선택 에셋에 의존한다 → P1에서 **경로를 인자로 받는 headless 진입점**을 함께 만든다.
- 각 단계는 `git status`로 변경 범위를 확인하고 단계별로 커밋한다. 롤백 지점을 남긴다.

---

## 9. 검증

**정적**
1. `validate_bt_json.py "…/SourceJson/Humanoid" --strict` — 0 error. (보스 5개도 회귀 통과.)
2. `AbilityDataValidator` 전수 — 신규 Warning 0. 기존 Dryad 3 + Training Dummy 1은 예상된 미해결.
3. `MonsterAbilitySetIntegrationTests` — Humanoid 5 Set의 `aiSelectable` Ability 전부 Motion 해석·HitPhase 정합.
4. **역할 커버리지 (신규)** — 아키타입별로 6개 `AbilityAIRole` 각각에 후보가 ≥1. 하나라도 0이면 BT의 대응 `RequestAction`이 영구 실패한다.

**동적 (Play Mode, 아키타입당)**

5. 궁수 회귀 — 8m에서 실제로 발사하는가. §2.2의 회귀 지점.
6. 후보 풀 크기 — `EnemyCombat._abilitySelectionDiagnostics`에서 거절 사유 없는 후보 수.
7. `40` 그룹 규칙이 실제 실행되는가 (스코어러 우회 회귀 방지). ⚠ BT 에디터를 열어 둔 채 측정하면 프레임 드랍이 결과를 오염시킨다 (메모 「BT 에디터 디버그 viz 성능」).
8. 아키타입 식별 — §5의 한 문장을 모르는 상태로 30초 교전 후 맞힐 수 있는가.
9. 다인 전투 — 같은 아키타입 3 + 혼합 3에서 `HasAttackSlot` 양보 동작, 히트스톱 누수 없음.

5·6·7은 batchmode PlayMode 테스트로 자동화 가능한지 P1에서 판단한다. 8은 사람 판단이 필요하므로 **사용자 확인 항목**으로 남는다.

---

## 10. 확정된 결정

**D1. 후보 풀 — (b)안 채택.** `aiSelectable`은 **전부 켠 채로 둔다.** `aiRoles`·가중치·쿨다운·예고만 적용한다.

역할 지명 BT는 그대로 동작한다 — `EnemyAbilitySelectionPolicy.MatchesRole`이 `aiRoles`로 거르기 때문에, 역할을 지정한 요청에는 지명된 9~10개만 잡힌다. 넓은 풀은 **역할 미지정 경로(`abilityRole` 없는 `RequestAction`, `40 Execute Selected Intent` 그룹)**에서만 유지된다. 즉 결정론 축은 좁고 변주 축은 넓은 이중 구조가 되며, 나중에 (a)안으로 좁히려면 `aiSelectable`만 내리면 된다 — 되돌리기 가장 쉽다.

→ P4에서 `aiSelectable`은 **건드리지 않는다.**

**D2. `hitPhaseIndex` 레거시 — 정리한다.** 단 아래 범위 판단이 붙는다 (§10.1).

### 10.1 레거시 범위 전수 조사 결과

`SpawnProjectileEvent`는 프로젝트 전체에 **47개**이고, **47개 전부가 `hitPhaseIndex: -1`**이다. 결선된 것이 하나도 없다. 이 경로는 지금까지 **한 번도 쓰인 적이 없다.**

| 소유자 | 이벤트 수 | 현재 damage | hitPhase 결선 시 | 이번 작업 |
|---|---|---|---|---|
| Humanoid Bow | 14 | 고정 10 | 9~31 (저작값) | **적용** |
| Humanoid Katana | 5 | 10 / 20 | — | 제외 (§1.3 미배치) |
| Skeleton Bow | 1 | 고정 10 | 18 | **보류** |
| Lich | 1 | 고정 10 | 30 | **보류** |
| MainPlant | 1 | 고정 10 | 28 | **보류** |
| SpiderQueen | 1 | 고정 10 | 28 | **보류** |
| **Player** (Bow/Katana/Staff) | **22** | 10 / 20 | — | **제외** |

**수정 기전 (코드 확인 완료).** `ProjectileManager.ResolveAttackData`가 `hitPhaseIndex >= 0`이면 `EnemyCombat.CreateProjectileAttackData(i)`로 hitPhase의 damage·poise·break·reaction을 끌어오고, null이면 `legacyDamage`로 폴백한다. `hitPhases[i].projectileDefinition`이 비어 있어도 `LegacyProjectileDefinitionCache`가 `projectilePrefab`에서 정의를 만들어 주므로 **`hitPhaseIndex`만 0..N-1로 채우면 충분**하다. 기계적으로 안전하고 폴백이 살아 있다.

**보류 사유.** Skeleton/Lich/MainPlant/SpiderQueen 4건은 결선 순간 **투사체 데미지가 2.8~3배 뛴다**(10 → 28~30). 이건 버그 수정이 아니라 **밸런스 변경**이고, 대상이 이번 작업 범위 밖 몬스터다. Player 22건은 플레이어 전투 밸런스 전체에 영향을 준다.

→ Humanoid Bow 14건만 이번에 결선한다. 나머지 22건(몬스터 4 + 플레이어 18\*)은 **§11 확인 요청**으로 남긴다.

\* 플레이어 22건 중 Katana `InYan` 4건은 damage 20으로 별도 저작돼 있어 델타가 다르다.

### 10.2 Bow hitPhase 정합

결선하면 비로소 `hitPhases` 개수가 의미를 갖는다. 다발 사격 3건은 Payload `hitPhases`를 늘려야 한다.

| Ability | SpawnProjectile | 현재 hitPhases | 조치 |
|---|---|---|---|
| `Skill_2` | 2 | 1 (dmg 21) | phase 복제 → 2개, 이벤트 index 0·1 |
| `Skill_4` | 3 | 1 (dmg 23) | phase 복제 → 3개, index 0·1·2 |
| `Skill_5` | 4 | 1 (dmg 31) | phase 복제 → 4개, index 0·1·2·3 |
| 나머지 11건 | 1 | 1 | index 0만 설정 |

⚠ **단순 복제는 다발 스킬의 총 데미지를 N배로 만든다.** `Skill_5`는 31 → 124가 된다. 기존 저작값이 "1발 기준"인지 "스킬 총량"인지 근거가 없으므로, **총량 보존**(31을 4발로 분할 → 발당 8)을 기본값으로 잡고 §11에서 확인한다.

---

## 11. 실행 결과 (2026-08-08 완료)

전 단계를 Unity 6000.3.21f1 batchmode(`-executeMethod`)로 수행했다. 사용자 수작업 없음.

| 단계 | 스텝 | 결과 |
|---|---|---|
| P0 | `validate_bt_json.py` + 카탈로그 + SKILL.md | 보스 5개 포함 19개 파일 오류 0 |
| P1 | `HumanoidAuthoringBatch` 신설 | Preview 기본 / `-uplayground-apply` 시 쓰기 |
| P3 | `Step_BowFix` | 44건 |
| P4 | `Step_FixDefects` | 2건 |
| P5 | `Step_BakeMeleeRangeHumanoidOnly` | 84건 |
| P6 | `Step_AssignRoles` | 175건 |
| P7 | `gen_bt.py` → Rules JSON 5종 | 정적 검증 통과 |
| P8 | `Step_ImportHumanoidBt` + `Step_WireBehaviorData` | BT 5개, 전략 SO 5개, 배선 15건 |
| P9 | `Step_Validate` + EditMode 385개 | 역할 커버리지 30/30 |

커밋: `c402f332`(1단계), `73e60467`(2단계).

### 11.1 계획에서 바뀐 것

**`MonsterMeleeRangeBakeTool.BakeAll()`은 쓰지 않았다.** 이 도구는 프로젝트의 **모든** `ActorDefinitionSO`를 순회해 `activation` 사거리와 `EnemyBehaviorSO` 거리까지 덮어쓴다 — 보스·식물·거미 전부 포함이다. 읽기 전용 `AnalyzeAll()`로 측정만 하고, `Library/MonsterMeleeRangeBakeReport.json`에서 Humanoid Ability에만 반영했다.

**BT가 `Counter`·`GapCloser`를 요청하지 않고 있었다.** 초안 BT는 두 역할의 요청 경로가 없어서, GAS에 배정해도 소비하는 쪽이 없는 죽은 역할이 될 뻔했다. `20 Immediate Reactions`의 카운터 갈래와 `40 IntentCounter`에 `abilityRole: Counter`를, `50`의 사거리 밖 진입에 `GapCloseAttack` 규칙을 추가했다.

**`(attackCategory, abilityRole)`은 AND 조건이다.** 초안은 BT가 `Finisher`를 `Basic`으로 요청하는데 배정은 데미지 내림차순이라 `Heavy`가 잡히도록 되어 있었다 — 후보 0으로 영구 실패한다. 아래 계약을 `gen_bt.py`의 `role_category()`와 `AssignRolesFor`의 `Take(...)` 양쪽에 명시했다. **한쪽만 고치면 안 된다.**

| 역할 | 카테고리 | 선정 기준 |
|---|---|---|
| `Opener` | `Basic` | startup 최단 2개 |
| `Punish` | `Heavy` (Bow: `Skill`) | startup 최단 2개 |
| `Counter` | `Skill` | Counter 계열 **전부** |
| `GapCloser` | `Skill` | 베이크된 `maxDistance` 최대 1개 |
| `Signature` | `Skill` | startup 최장 1개 |
| `Finisher` | `Basic` | 데미지 최대 2개 |

`Counter`만 개수를 제한하지 않는다 — `MonsterAbilitySetIntegrationTests.Counter_AI공격은_Counter_역할을_가진다`가 Counter 계열 AI 공격 전부에 역할을 요구하는 불변식을 강제한다. 아키타입당 1개만 배정했다가 이 테스트로 회귀를 잡았다.

**투사체 프리팹 교체는 같은 구체 타입일 때만 했다.** `Humanoid_Bow_Attack_3`은 `Nenmir_Arcing_Arrow`(곡사)라 `DefaultArrow`(직사)로 바꾸면 공격 성격이 달라진다. 소유권 정리가 거동 변경이 되면 안 되므로 유지했다.

**`hitPhaseIndex`는 발사 시간 순으로 매긴다.** MotionSet 저장 순서는 시간 순이 아니다(레이어를 가로질러 수집되므로). `startTime` 정렬 후 0..N-1을 부여한다.

### 11.3 사거리 정책 — 베이크된 activation이 권위값이다 (2026-08-08 개정)

> 이 절의 이전 판본은 `min(authoredMax, max(targetingRange - 0.15, personalSpace + 0.5))` 클램프를 전제로 했다. 그 클램프는 제거됐고 아래가 현행이다.

근접 Ability의 유효 최대 거리는 `EnemyAttackRangePolicy.ResolveEffectiveMaxDistance`가 정한다. 현행:

```
근접 + authoredMax > 0  →  max(authoredMin, authoredMax)      // 베이크값을 그대로 신뢰
근접 + authoredMax == 0 →  max(threatRange-0.15, personalSpace+0.5)  // 레거시 보조 추정
```

**기존 클램프가 왜 틀렸나.** `targetingRange`는 "직접 수정 금지 — HitBox impact 포즈에서 베이크된다"고 선언돼 있지만 그 베이크가 돌지 않아 **전체 hitPhase 858개 중 850개가 클래스 기본값 1.5**다. 즉 `threatRange - 0.15`는 프로젝트 대부분에서 상수 `1.35`였고, 모션마다 실측한 사거리를 이 방치된 상수로 일괄해 깎고 있었다. 대검이든 단검이든 똑같이.

이 클램프는 실측 데이터를 무효화할 수도 있었다. 베이크된 `min`이 1.35를 넘으면 `[min, 1.35]`가 빈 구간이 되고, 옛 코드의 마지막 `Mathf.Max(minDistance, effectiveMax)`가 그것을 **폭 0짜리 구간** `[1.4, 1.4]`로 만들어 사실상 선택 불가였다. `DoubleAxe_18/19_Counter_Attack`이 이 상태로 죽어 있었다. 현행 식은 결과가 항상 `authoredMin` 이상이라 구조적으로 이 상태가 나오지 않는다.

**히트가 실제로 닿는가 — 베이크 리포트 대조 결과.** 역할 배정 근접 Ability 40건 전수 확인:

| 판정 | 건수 | 내용 |
| --- | --- | --- |
| 정상 | 36 | `authoredMax`가 실측 도달보다 **정확히 0.15m 작다** (도구가 안전 마진을 뺀다) |
| 헛스윙 | 2 | 실측 1.35·1.00인데 권장값이 2.5 — **베이크 도구 결함** |
| 측정 실패 | 2 | `Blocked` — 2.5는 방치된 기본값 |

즉 36건은 `authoredMax`에서 공격을 시작하면 히트가 반드시 닿는다. `DoubleAxe_Skill_1/2`(실측 3.6·4.8)나 `DualBlade_Skill_1`(4.75)은 2.5에서 잘려 오히려 보수적이다.

헛스윙 2건은 `Step_FixWhiffRanges`로 실측 기준으로 되돌렸다. 여러 액터가 공유하므로 가장 짧은 실측에서 다시 마진을 뺐다.

| Ability | 이전 | 실측 도달 | 조치 |
| --- | --- | --- | --- |
| `DualBlade_08_Attack_8` | 2.5 | 1.35 (DualSword_002) | max → 1.20 |
| `SwordShield_09_Attack_9` | 2.5 | 1.00 (GreatSword_002) | max → 0.85 |

**미해결 2건.** `DualBlade_22_Skill_2`·`SwordShield_20_Skill_1`은 `Blocked`이고 사유가 "공유 소비자/Variant의 안전 거리 교집합이 없습니다"다. 전진 돌진 모션이라 실측 히트 구간이 `[2.6, 6.35]`·`[4.45, 6.00]`처럼 **현재 activation `[0, 2.5]`보다 위**에 있다. 가까이서 쓰면 지나쳐 버린다. 액터마다 구간이 어긋나 단일 값으로 못 맞추므로 액터별 Variant 분리나 역할 재지명이 필요하다. 이 결함은 정책 개정 이전부터 있었다.

**교전 거리와의 관계.** `optimalCombatDistance`(2.0~2.8)와 도달 거리의 간극은 정책 개정으로 크게 줄었다(도달이 1.35 상한에서 최대 2.5로 늘었다). 남는 차이는 `EnemyChaseState.ResolveStopDistance`가 메운다 — `Min(Max(chaseStopDistance, minCombatDistance), GetPreferredMeleeApproachDistance)`이고 후자는 **요청된 카테고리·역할의 실제 최대 사거리 − 0.1**이다.

> 테스트 작성 시 함정 — "교전 거리에서 공격 가능한가"는 런타임 불변식이 **아니다**(추격이 메운다). 진짜 불변식은 "역할마다 0보다 큰 도달 거리가 있다"이다. 또 베이크된 Ability는 `minDistance`가 0이 아닌 밴드(예: 0.65~1.55)라, 저거리 몇 점만 찔러 보는 이분 탐색은 밴드를 통째로 놓친다. `MaxReach`는 스캔으로 구현했다.

### 11.2 미해결·미검증

**Play Mode 체감 미검증.** 정적 검증과 데이터 정합만 통과했다. 확인 필요:
1. 궁수가 8m에서 실제로 발사하는가 — §2.2 수정의 회귀 지점.
2. 다발 사격 데미지 — `Skill_5` 31을 4발 7.75로 분할했다. 원 저작값이 "1발 기준"이었다면 이 판단이 틀리다.
3. §5의 아키타입 한 문장을 30초 교전으로 맞힐 수 있는가.

**역할 의미가 약한 두 곳.**
- GreatSword `GapCloser` = `Skill_2`(maxD 1.7). 대검에 실제 돌진기가 없어 "사거리 최대인 Skill"이 잡혔다.
- Bow `GapCloser` = `Skill_1`. 원거리라 거리 좁히기 자체가 무의미하고, 모든 Bow Ability의 `maxDistance`가 20으로 동률이라 첫 항목이 잡혔다. 사실상 "장거리 사격"으로 동작한다.

두 경우 모두 콘텐츠(돌진 모션) 확정 후 재지명이 맞다.

**플레이어 투사체 레거시 22건 미처리.** §10.1 표의 보류분이다. 전수 조사 결과는 아래와 같다.

| MotionSet | 파일 | 발사 | 이벤트 `damage` |
| --- | --- | --- | --- |
| `Player/Bow/` | `Attack_1`~`5` 각 1, `Skill_1` 1, `Skill_2` 2, `Skill_3` 1, `Skill_4` 3, `Skill_5` 4 | 16 | 10 |
| `Player/Katana/` | `Katana_Skill_Ability` 1, `Katana_Skill_InYan` 4 | 5 | 10 / 20 |
| `Player/Staff/` | `Humanoid_Staff_HeavyAttack_1` | 1 | 10 |

전부 `hitPhaseIndex: -1`이라 이벤트 자체의 고정 `damage`를 쓴다. 0으로 결선하면 발동한 Ability의 `hitPhases[0]`이 대신 적용된다. Bow AbilitySet은 모든 Ability가 hitPhase 1개뿐이라 인덱스 모호성은 없다.

| Ability | 발사 | 현재 총합 | 결선 후 총합 | 배율 |
| --- | --- | --- | --- | --- |
| `Bow_Light_00` | 1 | 10 | 61 | 6.1× |
| `Bow_Light_04` | 1 | 10 | 77 | 7.7× |
| `Bow_Ultimate` | 2 | 20 | 278 | 13.9× |
| `Bow_Ability`(Skill.4) | 3 | 30 | 465 | 15.5× |
| `Bow_Ability`(Skill.5) | 4 | 40 | 840 | 21× |

**저작값은 "스킬 전체 총합"으로 확정됐다(2026-08-08 사용자 확정).** 따라서 다발 사격은 총합을 발사 수로 나눈다. 단발은 저작값이 곧 그 한 발이므로 분할이 없다.

**결선 가능 여부를 가르는 것은 배율이 아니라 "무기 불변성"이다.** 플레이어 AnimationSet은 무기별 조회 표이고 모든 무기 세트가 모든 키를 갖는다. 같은 키가 장착 무기에 따라 다른 모션으로 풀리는데, Ability의 `hitPhases`는 하나뿐이다. 모션마다 히트 수가 다르면 한 phase 목록으로 모든 무기의 총합을 동시에 만족시킬 수 없다.

| 키 | Bow 장착 | 다른 무기 장착 | 판정 |
| --- | --- | --- | --- |
| `Bow.Light.00`~`04`, `Entry*`, `Counter`, `Swap*` | 발사 1 | 근접 모션 콜리전 1개 `[0]` | 인덱스 0이 어디서나 일관 → **안전** |
| `Bow.Ability.Skill.3/4/5` | 발사 1/3/4 | **같은 활 모션** | 무기 불변 → **분할 안전** |
| `Katana.Ability.Skill.6` | 발사 4 + 콜리전 4 `[0,1,2,3]` | 같은 카타나 모션 | phase 4개를 콜리전이 이미 소진 → **보류** |
| `Bow.Ultimate` | 발사 2 | 무기별 콜리전 1·4·5·7·16개 | 한 phase 목록으로 불가 → **보류** |

`Step_WirePlayerProjectileHitPhases`로 **18건 결선 완료**했다.

| 대상 | 처리 |
| --- | --- |
| 단발 9건 (`Bow_Attack_1`~`5`, `Bow_Skill_1`, `Bow_Skill_3`, `Katana_Skill_Ability`, `Staff_HeavyAttack_1`) | `hitPhaseIndex` → 0, phase 불변 |
| `Bow_Skill_4` 3발 | 155 → 3 phase × 51.67, 인덱스 0·1·2 |
| `Bow_Skill_5` 4발 | 210 → 4 phase × 52.5, 인덱스 0·1·2·3 |

**보류 6건.**
- `Bow_Skill_2` 2발 (`Bow.Ultimate`) — 139을 2로 나누면 Bow는 맞지만, GreatSword·SwordShield 장착 시 콜리전이 1개뿐이라 69.5만 들어간다(현재 139). 무기별로 다른 phase 목록이 필요하다. 참고로 Katana 장착 시 콜리전 16개가 phase 0-15를 요구하는데 이 Ability는 phase가 1개뿐이라 **이번 작업 이전부터 인덱스 1-15는 범위 밖**이다.
- `Katana_Skill_InYan` 4발 (`Katana.Ability.Skill.6`) — phase 4개 `[56,61,66,71]`을 콜리전 4개가 이미 쓴다. 발사까지 0-3에 물리면 254가 두 번 적용돼 총합 규칙을 깬다. 현재도 254 + 80(발사 고정값)으로 이미 초과 상태다. 발사용 phase 4개를 추가하고 254를 8히트로 재분배할지, 투사체를 무피해로 둘지 콘텐츠 결정이 필요하다.

몬스터 4건(Skeleton/Lich/MainPlant/SpiderQueen)은 `Step_WireMonsterProjectileHitPhases`로 결선 완료했다 — `hitPhaseIndex: -1 → 0`. 투사체 데미지가 `legacyDamage` 고정값에서 저작 `hitPhases[0]`으로 바뀐다.

**EditMode 기존 실패 7건**(내 변경과 무관, 건드리지 않은 파일):
Ultimate managed reference 유실 1 / 태그 트리거 개수 26↔31 1 / Dryad·Training Dummy Motion 미해석 2 (CLAUDE.md에 예상된 미해결로 기록됨) / Blackboard 로그 어서션 1 / Cinematic 1 / MotionSet Executor 1.

## 12. 참고

- `Assets/10.Datas/Ability/Actor/Boss/*/` — 역할 기반 GAS 규약의 기준 구현
- `Assets/10.Datas/AI/BehaviorTree/SourceJson/Boss/Boss_MyoRyeong.json` — BT 5그룹 골격 기준
- `Assets/02.Scripts/GameActor/Component/Enemy/EnemyAttackRangePolicy.cs` — `ResolveEffectiveMaxDistance` (§2.2 근거)
- `Assets/02.Scripts/GameActor/Component/Enemy/EnemyCombat.cs` — `EnemyAbilitySelectionPolicy`
- `Assets/02.Scripts/GameActor/Editor/MonsterMeleeRangeBakeTool.cs` — `BakeAll()`
- `Assets/02.Scripts/GameActor/Editor/MonsterBehaviorTreeJsonImporter.NodeFactory.cs` — `abilityRole` 파싱 (§2.5 근거)
- `Assets/docs/Complete/GAMEPLAY_ABILITY_SYSTEM_SPEC.md`
