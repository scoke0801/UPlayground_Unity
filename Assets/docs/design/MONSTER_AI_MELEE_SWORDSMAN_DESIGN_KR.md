# 근거리 몬스터 AI 설계 — "단련된 검사" (Behavior Tree / Rules JSON 기준)

> 대상: 근거리 melee 1종 심화 설계
> 기준 시스템: `MonsterBehaviorTreeJson` (Rules 포맷) → `BehaviorTreeAsset` 컴파일
> 상위 철학 문서: [`monster_ai_bt_design_gdd_kr.md`](../Complete/monster_ai_bt_design_gdd_kr.md)

이 문서는 프로젝트에 **이미 구현된** BT/Rules 시스템과 CombatDecision(Intent 스코어러), Player-Read 메모리, 그룹 어택 슬롯을 활용해, "전투가 재밌게 느껴지는" 근거리 몬스터 1종을 구체적 수치까지 정의한다. 함께 제공되는 import 가능한 JSON:

- `Assets/10.Datas/AI/BehaviorTree/SourceJson/EnemyBehavior_SkeletonSwordsman_Disciplined.json`

---

## 1. 웹 리서치 요약 — "전투를 재밌게 만드는 것"

조사한 AAA/액션 게임 적 AI 디자인 원칙과, 본 프로젝트에서의 실현 수단을 함께 정리한다.

| 원칙 | 핵심 내용 | 출처 |
|------|-----------|------|
| **어택 토큰 (Attack Token)** | 그룹 전투에서 한 번에 공격할 수 있는 적 수를 토큰으로 제한. 나머지는 위협만 한다. 적이 많아도 위협 총량이 통제되어 "불공정"하지 않음. 1:1에서도 같은 발상 — 적이 *무한히* 압박하지 않고, 공격을 커밋한 뒤 스스로 물러나 리듬을 만든다. | Halo / 일반 melee AI |
| **텔레그래프 & 가독성** | 큰 모션·색·사운드로 공격 시작/단계를 알린다. "모든 공격은 텔레그래프되어, 플레이어는 *기습*이 아니라 *자기 실수*로 죽어야 한다." 강공격은 분명한 선딜, 약공격은 0.1~0.2초의 최소 선딜. | 텔레그래프/Anatomy of an Attack |
| **페이싱 / 긴장-이완** | 공격 속도를 조절해 난전이 "읽을 수 없는" 상태가 되지 않게 한다. 압박 → 관찰 → 공격 → 후퇴 → 재진입의 강약 리듬이 재미의 핵심. | 페이싱/리듬 |
| **whiff-punish 루프** | 빗나간 공격에는 대가가 있어야 한다(회복창). 회복창은 양방향 — 적의 후딜은 플레이어의 처벌 기회, 플레이어의 후딜은 적의 처벌 기회. 이 *상호* 처벌 구조가 풋시스(spacing)를 만든다. | 처벌/회복창 |
| **컴뱃 코디네이터 / 역할** | The Last of Us는 NPC에 Flanker/Approacher 등 역할을 부여하고 플레이 공간을 분석해 적응. "살아있다"는 느낌. | The Last of Us Human Enemy AI |
| **적응형(Player Read)** | 적이 플레이어 습관(회피 잦음/평타 반복/거리 유지)을 읽고 대응을 바꾼다. | 적응형 AI |

핵심 명제(상위 GDD와 일치): **"읽을 수 있음 → 대응 가능 → 학습 가능, 하지만 가끔 속임."** 그리고 *"무슨 행동을 하는가보다 어떻게 연결하는가"* 가 재미를 만든다.

출처:
- [Enemy design and enemy AI for melee combat systems (Game Developer)](https://www.gamedeveloper.com/design/enemy-design-and-enemy-ai-for-melee-combat-systems)
- [Why Halo's Enemy AI is the Best in Gaming (CBR)](https://www.cbr.com/halo-enemy-ai-best/)
- [Human Enemy AI in The Last of Us (Game AI Pro, PDF)](http://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter34_Human_Enemy_AI_in_The_Last_of_Us.pdf)
- [Enemy Attacks and Telegraphing (Game Developer)](https://www.gamedeveloper.com/design/enemy-attacks-and-telegraphing)
- [Keys to Combat Design: Anatomy of an Attack (GDKeys)](https://gdkeys.com/keys-to-combat-design-1-anatomy-of-an-attack/)
- [Designing for Difficulty: Readability in ARPGs (Game Developer)](https://www.gamedeveloper.com/game-platforms/designing-for-difficulty-readability-in-arpgs)
- [Enemy design (The Level Design Book)](https://book.leveldesignbook.com/process/combat/enemy)

---

## 2. ⚠ 레이어 경계 — Rules JSON이 책임지는 것 / 책임지지 않는 것

재미 원칙을 잘못된 레이어에 욱여넣으면 설계가 조용히 깨진다. 이 프로젝트에서 책임은 명확히 둘로 나뉜다.

### Rules JSON(=이 문서)이 책임지는 것 — **결정 + 거시 페이싱**
- **무엇을** 할지: 어떤 의도(Intent), 어떤 공격 카테고리(Basic/Heavy/Skill), 어떤 스타일(Circle/Flank/Guard/Dodge/JumpBack).
- **언제/얼마 간격으로**: 행동 사이의 *공백* — `Wait`, `cooldownDuration`, `ActionDelayElapsed`(NextActionAllowedTime), `tickInterval`, 연속 공격 수 제한.
- **플레이어 읽기 반응**: 회피/평타/가드 빈도, 최근 피격, 경직/후딜에 대한 분기.

### Rules JSON이 책임지지 **않는** 것 — **공격 데이터 / MotionSet 레이어**
다음은 `AbilitySetSO` + `HitPhaseData` + MotionSet 타임라인의 책임이며, **본 설계의 *전제(요구사항)***다.

- **텔레그래프(선딜) / 가독성**: 각 공격의 wind-up 모션·VFX·SFX. → *공격 자체의 읽힘은 공격 데이터에서 보장해야 한다. JSON은 읽힘을 만들지 못한다.*
- **후딜 / 회복창의 존재**: 공격 후 빈틈의 길이. (JSON은 이 빈틈을 *이용*만 한다.)
- **히트박스 타이밍 / 다단 히트 / 페이크 캔슬**.

> **GDD 주의:** 상위 GDD는 "선딜/후딜/페이크"를 *BT 책임*으로 적었지만, 현재 구현에서는 공격 데이터 레이어다. GDD의 이 항목은 *지향점*으로 읽고, 본 문서의 레이어 경계를 따른다.

**whiff-punish 루프의 분담:**
- 회복창이 *존재*하도록 만드는 것 = 공격 데이터 (전제).
- 적이 그 창을 *노리는* 것 = JSON (`IsPlayerRecovering`/`IsPlayerStaggered`/`RecentlyHitByPlayer` → `Punish`). ← 본 설계가 제공.

---

## 3. 몬스터 아이덴티티 — "단련된 검사" (skeleton_sword 기반)

광전사가 아니라 **절도 있는 검술가**. 거리를 재고(풋시스), 플레이어의 욕심을 처벌하며, 압박당하면 가드/이탈로 호흡을 고른 뒤 리듬을 갖고 재진입한다.

- `actorKind`: Ground
- `sourceBehaviorSo`: `Assets/10.Datas/Actor/Enemy/BehaviorData/BehaviorData_skeleton_sword.asset` (다른 melee 에셋으로 교체 가능 — 블랙보드 기본값/거리 fallback 출처)

**전투의 "대화":**
1. 검사가 간격(optimal ~2.6)에서 원을 그리며 압박한다.
2. 접근 → 약공격으로 떠본다.
3. 플레이어가 욕심내 후딜을 보이면 → 강공격으로 **처벌**.
4. 플레이어가 평타를 반복하면 → **카운터**(가드/회피 후 반격).
5. 플레이어가 가드만 하면 → 스킬/강공격으로 **가드를 뚫는다**.
6. 3회 커밋하면 짧은 스텝/플랭크로 **재진입** → 긴장-이완(쉴 틈은 짧게). (어택 토큰 자기 제한)
7. 강타에 맞아도 가드/카운터로 **전장을 사수**하고, 포이즈가 깨질 때만 확실히 이탈한다. (무한 슈퍼아머는 금지하되 쉽게 물러나지 않음)

이 7단계가 **학습 가능하지만 반복적이지 않은** 루프를 만든다. 변주는 ① CombatDecision 스코어러(SelectedIntent)와 ② WeightedRandom 선택, ③ Player-Read 분기에서 나온다.

---

## 4. 재미 원칙 → 시스템 키워드 매핑

| 재미 원칙 | 실현 수단(이 프로젝트) | 상태 |
|-----------|------------------------|------|
| 어택 토큰(자기 제한 압박) | `ConsecutiveAttackCountGreaterOrEqual` → 강제 리셋(JumpBack/Circle) + `maxComboPressureCount` | ✅ JSON |
| 그룹 어택 토큰(다수 전투) | `RequestAttackSlot` / `HasAttackSlot` | ✅ 가능(본 1종은 미사용, 다수 전투 시 추가) |
| 긴장-이완 리듬 | **코드/스코어러 주도**: `EnemyAIController` maxComboPressure throttle + 스코어러 `RhythmPhase`. JSON은 `maxComboPressureCount`/`aggression` 레버로 튜닝 + `ActionDelayElapsed`로 게이트 | ✅ 코드+JSON 튜닝 (§6.1) |
| whiff-punish (적→플레이어) | `IsPlayerRecovering`/`IsPlayerStaggered` → `Punish`/`ExecuteAttack Heavy` | ✅ JSON |
| 적응형(Player Read) | `IsPlayerAttackingFrequently`/`IsPlayerGuardingFrequently`/`IsPlayerDodgingFrequently` | ✅ JSON (SyncEnemyMemoryService 자동) |
| 컴뱃 코디네이터 역할/의도 | `SelectedIntent`(EvaluateEnemyCombatIntentService가 자동 산출) | ✅ JSON |
| 텔레그래프/가독성(적 공격 읽힘) | `AbilitySetSO` wind-up 모션/VFX | ⚠ **공격 데이터 전제** (JSON 밖) |
| 후딜/회복창 존재 | `HitPhaseData`/MotionSet 후딜 | ⚠ **공격 데이터 전제** |
| 지연 공격(낚시) | `do:[Wait, ExecuteAttack]` 순차 시퀀스 | ✅ JSON (제한적, 4.1 참조) |
| 진짜 페이크/캔슬(선딜만 보이고 취소) | 취소 액션 부재 | ❌ **신규 액션 필요(needs code)** |
| 등 잡힘 시 즉시 회전 공격(Turn Attack) | 각도 조건 부재 | ❌ **신규 condition 필요(needs code)** |

### 4.1 지연 공격(낚시)의 한계
`do:[{Wait},{ExecuteAttack}]`는 시퀀스로 컴파일되어 "잠깐 멈춤 → 강공격"을 만들 수 있다(GDD의 "살짝 대기 → 강공격" 리듬 비트). 단, ① 선딜 후 *취소*는 불가(진짜 페이크 아님), ② `ExecuteAttack`에는 `cooldownId`가 등록되지 않으므로 rate-limit은 `ActionDelayElapsed`로만 건다. 진짜 페이크는 별도 액션(`FeintAttack`) 추가가 필요하며 후속 과제로 둔다.

---

## 5. 블랙보드 튜닝 (단일 출처)

아래 표가 곧 JSON의 `blackboard` 블록이다. 거리 조건은 숫자 대신 **이 키 이름**으로 참조한다(`DistanceLessOrEqual value:"optimalCombatDistance"`) → 튜닝이 한 곳에 모임.

| 키 | 값 | 의미 / 근거 |
|----|----|-------------|
| `tickInterval` | 0.06 | 반응성 높은 결정 주기(난이도↑) |
| `enablePatrol` | true | 타겟 없을 때 순찰 |
| `optimalCombatDistance` | 2.6 | 검사가 머물고 싶은 타격 간격(풋시스 기준선) |
| `minCombatDistance` | 1.2 | 이보다 가까우면 너무 붙음 → 스텝/원이동 |
| `personalSpaceDistance` | 0.85 | 밀착 회피 거리 |
| `preferredRange` | 2.6 | 선호 교전 거리(낚시/접근 판정용) |
| `aggression` | 0.74 | 높음 — 스코어러를 압박 의도 쪽으로 강하게 편향(난이도↑) |
| `reactionChance` | 0.65 | 플레이어를 더 잘/빨리 읽음 |
| `counterChance` | 0.5 | 카운터 성향 강화 |
| `guardChance` | 0.32 | 가드 비중 낮춤 → 공세 우선 |
| `dodgeChance` | 0.35 | |
| `retreatChance` | 0.14 | **후퇴 성향 대폭 ↓ — 전장을 떠나지 않음** |
| `punishRecoveryChance` | 0.72 | **후딜 처벌 매우 강함 — 핵심 난이도** |
| `antiGuardChance` | 0.6 | 가드만 하는 플레이어를 강공/스킬/차지로 적극 처벌 |
| `revengeChance` | 0.45 | 피격 후 보복 성향 ↑ |
| `circleWeight` | 0.32 | 수동적 원이동 ↓ → 커밋 우선 |
| `maxComboPressureCount` | 3 | **어택 토큰 자기 제한.** 블랙보드 `AIMaxComboPressureCount`로 `EnemyAIController.DecidePostAttack`이 직접 소비 — 연속공격 < 한도면 다음 행동 딜레이 0.12~0.38s(빠른 콤보), 한도 도달 시 짧은 딜레이 미적용→긴 휴지 |
| `minRetreatCooldown` | 2.8 | 후퇴 빈도 더욱 ↓ |

---

### 5.1 난이도 튜닝 — "너무 쉬움" → 압박형 (v2)

초기 "절제형" 튜닝은 압박이 약하고 플레이어에게 무료 반격창을 너무 자주 줬다("너무 쉬움"). 페어함(텔레그래프=공격데이터, 포이즈브레이크 탈출)은 유지한 채 난이도를 올린 변경:

- **블랙보드(가장 큰 레버):** `aggression` 0.5→0.74, `maxComboPressureCount` 2→3(연타 한도↑), `retreatChance` 0.25→0.14·`guardChance` 0.45→0.32·`circleWeight` 0.5→0.32(수동성↓), `punishRecoveryChance` 0.55→0.72·`antiGuardChance` 0.45→0.6(처벌↑), `tickInterval` 0.08→0.06(반응↑).
- **긴급반응 약화(그룹 10):** 강타 피격 시 뒤점프 도주 → **가드/카운터로 전장 사수**(`HeavyHitStandGround`), 연속피격 이탈 임계 3→**5**(쉽게 안 물러남).
- **후딜 처벌 강제(그룹 20):** 회복 중 플레이어가 거리 벌리면 **차지로 파고들어**(`PunishRecoveryGapClose`) 처벌을 강제.
- **낚시 타이트화(그룹 30):** 회피 유인 지연 0.35→**0.22s**.
- **콤보 압박(그룹 50):** 연타 한도 2→3, 강공격 비중↑, 콤보 후 휴지를 큰 뒤점프→**짧은 스텝/플랭크 재진입**(쉴 틈↓).

**더 쉽게/어렵게 다이얼:** 거의 전부 블랙보드로 조절된다. 더 어렵게는 `aggression`↑·`maxComboPressureCount`↑·`punishRecoveryChance`↑, 더 쉽게는 그 반대 + `retreatChance`/`circleWeight`↑. **싸구려 난이도 금지선**(즉발 공격·무한 슈퍼아머·반응 불가 패턴)은 넘지 않는다 — 어려움은 *압박/처벌 밀도*로 만들고, 공격 자체의 *읽힘*은 공격 데이터가 보장한다.

---

## 6. 행동 규칙 그룹 (우선순위 내림차순)

루트는 `Selector`이며, importer가 `SyncEnemyBlackboard / SyncEnemyMemory / SyncEnemyPhase / EvaluateEnemyCombatIntent` 서비스를 **자동 부착**한다. 따라서 매 tick `HasTarget`·거리·플레이어-read 키·`SelectedIntent`가 채워진 상태로 규칙이 평가된다.

| 그룹 (priority) | 게이트 | 목적 | 대표 규칙 |
|----|----|----|----|
| **00 생존/인터럽트** (1000) | — | 행동 금지 상태 유지, 타겟 없으면 순찰 | `IsBlockedEnemyState→KeepCurrentState`, `!HasTarget→PatrolOrIdle` |
| **10 긴급 반응** (960) | HasTarget | 포이즈 브레이크만 확실히 탈출, 그 외엔 전장 사수 (무한 슈퍼아머는 금지하되 쉽게 안 물러남) | `IsPoiseBroken→Evade`(탈출), `WasLastHitHeavy→Guard/Counter`(사수), `RecentHitCount≥5→가드/회피` |
| **20 응징 윈도우** (935) | HasTarget | **whiff-punish의 적→플레이어 절반.** 플레이어 경직/후딜을 강공/스킬로 처벌 | `IsPlayerStaggered→Heavy/Skill`, `IsPlayerRecovering→Heavy/Basic`, 멀면 `→Chase` |
| **30 플레이어 읽기** (910) | HasTarget | 습관 대응(적응형) | `잦은 공격→Counter/Guard`, `잦은 가드→가드뚫기 Skill/Charge`, `잦은 회피→지연 강공(낚시)` |
| **40 의도 실행(스코어러)** (880) | HasTarget | "살아있는" 변주 — `SelectedIntent`별 실행 | `Punish/Attack/Counter/Pressure/KeepDistance/Defend/Retreat/Chase/Recover` 분기 |
| **50 기본 전투 리듬(폴백)** (820) | HasTarget | 스코어러가 조용해도 *읽히는* 기본 루프 보장 | `멀면 Chase → 절제된 콤보(≤2) → 콤보 후 리셋 → 너무 붙으면 스텝 → idle` |

설계 의도상 **20·30 그룹은 스코어러에 의존하지 않는 직접 조건**이라, `SelectedIntent`가 빈약해도 몬스터가 잘 읽힌다(견고성 hedge). 40 그룹은 그 위에 다양성을 얹는다.

> **검증 주의:** `SelectedIntent`의 유효값은 **CombatIntent** 이름 = {Attack, Punish, Counter, Pressure, Chase, Retreat, KeepDistance, Defend, Recover}. `Evade`/`None`은 **SelectedIntent 분기로 쓸 수 없다**(검증을 통과하지만 영원히 매칭 안 됨). `Evade`는 `RequestAction`의 *intent*로만 유효(Dodge/JumpBack 이탈용).

### 6.1 누가 무엇을 주도하는가 (⚠ 레이어 귀속)

우선순위가 곧 책임 분담이다. 그룹 10/20/30이 스코어러(40) **위에** 있는 것은 의도된 설계다 — 생존·빈틈 처벌·습관 대응은 *직접 조건*으로 즉시 반응해야 하므로, 스코어러의 기저 의도를 덮어쓴다. 이 셋이 본 JSON이 전투 *감각*에 기여하는 핵심 레이어다.

반대로 **긴장-이완(압박↔휴지) 템포 자체는 본 JSON이 아니라 이미 구현된 코드/스코어러가 주도**한다. 오귀속을 피하기 위해 명확히 한다:

- **공격 템포 throttle(코드):** `EnemyAIController.DecidePostAttack`이 `maxComboPressureCount`를 읽어, 연속공격이 한도(2) 미만이면 다음 행동을 0.12~0.38s 뒤로(빠른 콤보), 한도 도달/빗맞음이면 긴 휴지(+0.6~1.2s)로 둔다. 이 값이 `NextActionAllowedTime`에 기록되고, 본 JSON의 모든 공격 규칙은 `ActionDelayElapsed`로 이를 게이트한다 → **"2회 커밋 후 휴지"는 여기서 발생한다.**
- **의도 리듬(스코어러):** `EvaluateEnemyCombatIntentService`가 `RhythmPhase`(블랙보드 `DecisionCombatRhythmPhase`)를 포함한 `SelectedIntent`를 매 틱 산출. 그룹 40이 이를 실행 → 압박/관찰/후퇴의 거시 흐름.
- **블랙보드가 레버:** 따라서 템포를 조율하는 손잡이는 `maxComboPressureCount`(커밋 수)·`aggression`(압박↔이완 성향)이며, 이 둘이 위 두 메커니즘을 함께 움직인다.

그룹 50의 콤보/리셋 규칙은 **폴백 + 보강**이다 (스코어러가 조용하거나, 한도 도달 시 명시적으로 JumpBack/Circle/Guard로 간격을 회복):
```
멀다 → Chase(접근)
간격 안 + 연속공격<2 → 약/강/스킬 (ActionDelayElapsed 게이트, 코드 throttle과 동조)
간격 안 + 연속공격≥2 → JumpBack/Circle/Guard로 명시적 리셋 (쿨다운 ComboReset)
너무 붙음(≤min) → Step/Circle로 간격 회복
그 외 → KeepCurrentState (트리는 항상 답을 가진다)
```
> `ConsecutiveAttackCount`는 `EnemyTacticalMemory`가 관리(공격 적중 시 +1, 피격/가드 시 0)하며 런타임에 정상 동작함을 코드에서 확인. `ConsecutiveAttackCountLessThan/GreaterOrEqual` 조건은 안전하게 사용 가능.

---

## 7. 학습 가능한 전투 루프 (예시 시나리오)

```
원이동(Circle, 풋시스)
→ 접근(Chase)
→ 약공격(Basic)             ← 떠보기
→ 살짝 대기(Wait 0.35)       ← 템포 변화 / 낚시
→ 강공격(Heavy) → 강공격(Heavy)   ← 최대 3연타까지 커밋
→ [연속 3회 도달] 짧은 스텝/플랭크   ← 어택 토큰 리셋(짧은 이완), 곧 재진입
→ 플레이어가 욕심내 후딜 노출
→ (거리 벌리면) 차지로 파고들어 후딜 처벌(Heavy)   ← whiff-punish, 빠져나가기 어려움
→ 다시 압박…
```
- 플레이어가 평타를 반복 → 30그룹이 **Counter**로 분기.
- 플레이어가 가드 고집 → 30그룹이 **Skill/Charge로 가드 뚫기**.
- 검사가 강타에 맞음 → 10그룹이 **가드/카운터로 사수**(섣불리 이탈하지 않음); 포이즈 브레이크 시에만 확실히 이탈.

---

## 8. 적용 / 검증 절차

1. JSON을 `SourceJson/`에 둔다(본 파일은 이미 그 위치).
2. Unity 에디터: **`UPlayGround/비헤이비어 트리/JSON/선택 JSON 가져오기`** (또는 AI JSON 자동 감지) → `Generated/BT_EnemyBehavior_SkeletonSwordsman_Disciplined.asset` 생성.
3. 생성된 `BehaviorTreeAsset`을 검사 프리팹의 `EnemyBrain`(BehaviorTreeRunner)에 연결.
4. import 시 `BehaviorTreeJsonUtility.LogValidation`이 경고를 콘솔에 출력 — 확인.
5. `sourceBehaviorSo`는 거리/스탯 기본값 fallback 출처. 경로가 유효한지 확인(없으면 경고만, 블랙보드 명시값이 우선).

### 후속 과제(needs code)
- `FeintAttack`(선딜 후 취소) 액션 — 진짜 페이크.
- 각도 기반 `IsPlayerBehind` condition — Turn Attack.
- 다수 전투 시 `RequestAttackSlot` 도입 → 그룹 어택 토큰.

---

## 9. 요약

이 설계는 새 시스템 없이, **이미 있는** Rules DSL·CombatDecision 스코어러·Player-Read·연속공격 카운터만으로 "읽을 수 있지만 가끔 속이는" 근거리 검사를 만든다.

본 JSON이 직접 기여하는 재미 레이어(스코어러 위, 직접 조건):
- ① **whiff-punish 상호 처벌** — 경직/후딜 처벌(그룹 20)
- ② **플레이어 습관 적응** — 카운터/가드뚫기/낚시(그룹 30)
- ③ **즉시 이탈** — 포이즈/강타/연속피격 시 무한 슈퍼아머 방지(그룹 10)

긴장-이완(압박↔휴지) **템포 자체는 이미 구현된 코드/스코어러**가 주도하며(§6.1), 본 설계는 블랙보드 레버(`maxComboPressureCount`·`aggression` 등)로 그 성격을 "절도 있는 검사"로 튜닝한다. 텔레그래프/회복창의 *읽힘*은 공격 데이터 레이어가 보장한다는 전제 위에 선다.
