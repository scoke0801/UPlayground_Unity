# 전투 시스템 장르 리서치 기반 개선 로드맵

> **보관 문서 주의:** 이 문서의 플레이어 공격 데이터 예시는 Ability 전환 이전 구조다. 현재 단일 소스는 `AbilitySetSO`이며 최신 기준은 `../TODO/GAMEPLAY_ABILITY_SYSTEM_SPEC.md`를 따른다.

> 작성일: 2026-06-10
> 대상 버전: Unity 6 (6000.0.60f1), URP
> 분류: 설계서(미구현 계획). 본 문서의 코드 스니펫은 모두 **의사코드 스케치**이며 실제 구현이 아니다.
> 레퍼런스: 명조(鳴潮) / 젠레스 존 제로(ZZZ) / 원신 / 붕괴3rd / 퍼니싱 그레이 레이븐(PGR) / 베요네타 / 데빌 메이 크라이 등 액션·캐릭터 스왑 게임의 전투 메커니즘 웹 조사 결과

---

## 0. 개요

본 문서는 파티 스왑 기반 싱글플레이 TPS 액션(소울라이크 요소 포함)인 본 프로젝트의 전투 시스템을, 동일 장르(캐릭터 교대형 액션 RPG, 저스트 타이밍 액션)의 레퍼런스 작품과 비교해 **게임플레이 측면**에서 개선할 항목을 우선순위화한 로드맵이다.

핵심 결론을 먼저 요약하면:

- 본 프로젝트는 이미 **저스트 타이밍 보상 층(①층)** — 퍼펙트 도지, 도지 카운터 창, 저스트/퍼펙트 가드 반격, 클래시 패리, 모션 워프 — 을 거의 완비했다.
- 갭은 두 축에 집중된다. **스왑 협력 층(②층)의 "연결 미완"**(어시스트 스왑→패리 보상 미연결, 풀 게이지 스왑 특수공격 비활성, 협주형 에너지 부재)과, **그로기/행동 불능 통제 층(③층)의 "배율 미통합"**(Break 노출에만 1.15배가 걸리고 Stun/Knockdown 등에는 배율 없음)이다.
- 따라서 본 로드맵은 **신규 대형 시스템 추가가 아니라, 이미 깔린 인프라의 "마지막 연결과 배율 통합"에 무게중심**을 둔다.

본 문서는 아키텍처 개선이 아니라 **체감·게임플레이 루프 개선**이 목적이다. 아키텍처 축은 §8에서 기존 제안 문서와의 관계로 정리한다.

---

## 1. 장르 리서치 요약

### 1.1 작품별 메커니즘

| 작품 | 저스트 타이밍 보상 | 스왑/교대 협력 | 그로기·강인도 통제 |
|------|-------------------|----------------|-------------------|
| **명조** | 저스트 회피 → 회피 반격(판정 느슨, 쿨 1초/2회 가능) | **협주 에너지**: 적 타격·스킬·저스트 회피로 충전 → 스왑 시 퇴장 캐릭터 반주 스킬 + 입장 캐릭터 변주 스킬 동시 발동 | 적 부조화 게이지 → 조화도 파괴(그로기) |
| **ZZZ** | 극한 회피 → 바이탈 뷰(슬로우) → 전용 회피 카운터 + 무적 | **어시스트 패리**: 적 공격 타이밍에 교대 → 입장 캐릭터가 패리. 그로기 시 연쇄 콤보 스킬(일반1/정예2/보스3회, 슬로우 중 교대 선택) | 그로기(Stun) 상태 → **약체화 데미지 배율** + 행동 불능 |
| **원신** | 회피 자체 보상 거의 없음 | 교대 쿨만 존재(공격 협력 약함). 스왑 = 원소 반응 트리거 | 강인도(포이즈)는 있으나 명시적 그로기 약체화 약함 |
| **붕괴3rd** | 저스트 회피 → 위치 타임(베요네타 직수입, 전역 슬로우) + 체력 소회복 | 상태이상 조건 충족 시 QTE 교대기 | 스타일 랭크(DMC 전통) |
| **PGR** | 초산(超算) 회피 → 다음 스킬 3체인 강화 | 3색 스킬 체인 / 교대 | **회피 게이지**(1000pt, 회당 250pt 소모, 3초당 250pt 회복) — 회피가 리소스 |
| **베요네타** | 위치 타임(저스트 회피 → 전역 슬로우) | — | — |
| **DMC** | 저스트 가드/회피 | — | **스타일 랭크** |

### 1.2 공통 3층 구조

레퍼런스를 가로지르면 전투 만족감은 세 개의 층으로 정리된다.

1. **①층 — 회피/가드 저스트 타이밍 보상**: 저스트 입력 성공 시 (슬로우 + 무적/반격 + 다음 행동 강화)로 보상.
2. **②층 — 스왑 협력**: 교대가 회피 수단이 아니라 **공격 루프의 일부**다. 퇴장·입장 캐릭터가 함께 화력/패리를 만든다.
3. **③층 — 강인도/그로기 통제**: 적의 강인도를 깎아 행동 불능(브레이크/그로기)으로 만들고, **그 상태에 통합 데미지 배율**을 부여해 화력 집중 윈도우를 만든다.

### 1.3 핵심 통찰

- **저스트 타이밍 성공은 반드시 "다음 공격 행동의 강화"로 이어진다.** 단순 무적 회피로 끝나는 회피는 체감이 약하다(원신의 약점). 명조 회피 반격, PGR 3체인 강화, ZZZ 회피 카운터가 모두 이 원칙을 따른다.
- **스왑은 회피 수단이 아니라 공격 루프의 일부다.** 명조 협주, ZZZ 어시스트 패리/연쇄 콤보처럼 교대 자체가 화력 발생원이어야 "파티 스왑 게임"의 정체성이 산다.
- **본 프로젝트는 ①층을 거의 완비**했고, **②층의 연결 완성과 ③층의 배율 통합이 과제**다.
- 타격감 권장치(레퍼런스 + 기존 히트 리액션 문서 정합): 히트스톱 0.05~0.1초, 슬로우는 극한 회피 성공 시에만(약 0.3배 / 0.2초), 히트 플린치는 공격 파워별 차별화.

---

## 2. 현황 진단

### 2.1 이미 구현된 것 (제안 대상 아님)

본 프로젝트는 이미 다음을 갖췄다. **재발명하지 않는다.**

| 영역 | 구현 내용 | 핵심 위치 |
|------|----------|----------|
| 퍼펙트 도지 + 도지 카운터 창 | `_dodgeCounterWindow` = 1.2초, `OpenDodgeCounterWindow` / `ConsumeDodgeCounterWindow` | `PlayerCombat.cs:216,257,260`, 소비처 `PlayerActor.cs:1099`, `PlayerAttackState.cs:199,251` |
| 스왑 입장 공격(Entry Attack) | 교대 직후 입장 캐릭터 공격 큐 | `PartyManager.cs:274` `QueueEntryAttack`, `PlayerAttackState.cs:207` |
| 스왑 회피 카운터 / 어시스트 스왑 큐 | `QueueSwapEvade`, `QueueSwapAssist` | `PartyManager.cs:263,270` |
| 저스트/퍼펙트 가드 반격창 | `PERFECT_GUARD_WINDOW`, `OpenPerfectGuardCounterWindow` | `PlayerGuardState.cs:177,193,210` |
| 브레이크 데미지 배율(노출 한정) | `DamageTakenMultiplier` = 1.15, 데미지 곱셈에 적용 | `MonsterBreakGauge.cs:31`, 적용처 `DamageResolver.cs:55` |
| 클래시 패리 | 공격 상태 + 히트박스 활성 + parry-capable 시 Parried, 정책 SO 기반 | `DefenseResolver.cs`, `CombatDefensePolicySO` |
| 콤보 라우트 | 입력 시퀀스 토큰 매칭 → `forcedAttackAction`, GameplayTag/grounded 조건 | `ComboRouteRunner.cs` |
| 스왑 잔상 공격(Residual) | 교대 시 잔상 타격 | `PlayerSwapBehaviour.cs` |
| 스킬 게이지 | Ability(쿨다운) + Ultimate(게이지 풀충전), 공격 종류별 충전 테이블 | `PlayerSkillGauge.cs` |
| 기타 인프라 | Poise/Break + SpecialBreakAttack, Danger Ring(`UI_DangerRing.cs`, 빨강=Unblockable/노랑), 락온(`CameraLockOn`), 모션 워프, `EnemyAirborneState` 런치, 히트스톱, 회전식 카메라 쉐이크 | — |

즉, ①층(저스트 보상)은 사실상 완비 상태다.

### 2.2 진단 결론

실제 갭은 ②층·③층의 다섯 가지로 좁혀진다. 이것이 본 로드맵 제안의 무게중심이다.

1. **행동 불능 상태 통합 데미지 배율 부재** — Break 노출 상태에만 1.15배가 있고(`MonsterBreakGauge.DamageTakenMultiplier`), Stun/Knockdown 등 다른 행동 불능 상태에는 배율이 없다. → ③층 갭.
2. **풀 게이지 스왑 특수공격 비활성** — `PartyManager.cs:261`에서 `isSwapSpecial = false`로 임시 차단된 상태("// 풀 게이지 스왑 특수공격은 임시 비활성화"). → ②층 갭.
3. **어시스트 스왑 → 패리 결과 연결 미완** — `QueueSwapAssist` 큐는 있으나 입장 캐릭터의 패리 판정/보상이 연결되지 않았다. → ②층 갭.
4. **협주형 스왑 에너지 부재** — 명조식 협주 에너지에 해당하는 리소스가 코드에 없다(확인됨). → ②층 갭.
5. **입장 강화 라우트 단조로움** — Entry Attack은 있으나 단일 모션이다. → ②층 갭(다양성).

---

## 3. 우선순위 로드맵 요약 표

| Tier | 항목 | 분류 층 | 핵심 인프라 재사용 | 대략적 비용 |
|------|------|--------|-------------------|------------|
| **T1** | 4.1 행동 불능 상태 통합 데미지 배율 | ③ | `DamageResolver`, 행동 불능 상태군 | 소(S) |
| **T1** | 4.2 풀 게이지 스왑 특수공격 활성화 | ② | `PartyManager`, `PlayerSkillGauge` | 중(M) |
| **T1** | 4.3 어시스트 스왑 → 패리 결과 연결 완성 | ② | `QueueSwapAssist`, `DefenseResolver`, Danger Ring | 중(M) |
| **T2** | 5.1 협주형 스왑 에너지 | ② | `PlayerSkillGauge` 충전 테이블 패턴 | 중~대(M~L) |
| **T2** | 5.2 입장 강화 라우트 다양화 | ② | `ComboRouteRunner`, GameplayTag | 중(M) |
| **T3** | 6.1 궁극기 버스트 | ② | (별도 문서로 위임) | 대(L) |
| **T3** | 6.2 히트 리액션 Phase 2·3 포인터 | ① | (별도 문서로 위임) | 대(L) |
| **T3** | 6.3 공중 콤보 추격(보류 가능) | ① | `EnemyAirborneState` 런치 | 중(M) / 저비용 대안 소(S) |

비용 표기: S(수 시간~1일), M(수 일), L(주 단위 또는 별도 설계).

---

## 4. Tier 1 — 즉시 효과·저위험

### 4.1 행동 불능 상태 통합 데미지 배율

**레퍼런스 게임 메커니즘**
ZZZ는 적이 그로기(Stun) 상태에 빠지면 받는 데미지에 약체화 배율(대략 1.2~1.5배)을 적용해, 그로기 윈도우가 곧 "화력 집중 타이밍"이 되도록 설계한다. 모든 행동 불능 상태가 "추가 데미지 윈도우"로 통일되어 학습이 단순하다.

**현재 프로젝트 상태**
배율이 Break 노출 상태에만 존재한다. `MonsterBreakGauge.DamageTakenMultiplier`(`MonsterBreakGauge.cs:31`)가 `_isExposed`일 때만 `damageTakenMultiplierWhileExposed`(기본 1.15)를 반환하고, `DamageResolver`(`DamageResolver.cs:55`)가 최종 데미지 곱셈에 반영한다. 반면 Stun/Knockdown/Airborne 등 다른 행동 불능 상태는 배율 가산이 전혀 없다.

**제안**
"행동 불능(Incapacitated) 상태"를 하나의 개념으로 묶고, 해당 상태에 통합 데미지 배율을 부여한다. 배율 소스를 Break 게이지 단독이 아니라 **현재 상태 기반 + Break 게이지**의 합성으로 일반화한다.

```csharp
// 의사코드 — 실제 구현 아님
// DamageResolver 내부 배율 합성 지점(현 DamageResolver.cs:55 일대)
float incapMul = target.CurrentStateIncapacitatedMultiplier(); // 상태별 SO 값
float breakMul = breakGauge?.DamageTakenMultiplier ?? 1f;
// 중복 폭증 방지: 곱이 아니라 "더 큰 쪽 채택" 또는 (1 + Σ(mul-1)) 가산 합성 중 택1
float vulnerability = Mathf.Max(incapMul, breakMul);
finalDamage *= vulnerability;
```

상태별 배율은 `EnemyStatsSO`/전용 취약도 SO에 데이터화한다. 예: Stun 1.3, Knockdown 1.25, Airborne 1.15, Break 노출 1.15(기존 유지).

**주의(보존 제약)**
- 기존 `damageTakenMultiplierWhileExposed`(1.15)가 신규 상태 배율과 **곱으로 이중 적용되지 않도록** 합성 규칙(max 또는 가산)을 명시한다. 위 스케치는 max 채택.
- 본 변경은 §8의 아키텍처 제안 문서가 다루는 "데미지 해결 파이프라인" 위에 얹히므로, 두 문서의 접점은 이 §4.1이다.

**기대 효과**
모든 행동 불능 상태가 "지금 때려야 할 타이밍"으로 통일되어 그로기 통제(③층)가 보상 루프로 완성된다. 학습 비용 낮음.

**대략적 구현 비용: 소(S)** — 데이터 필드 + 합성 1지점.

---

### 4.2 풀 게이지 스왑 특수공격 활성화

**레퍼런스 게임 메커니즘**
명조의 협주(스왑 시 퇴장 반주 + 입장 변주 동시 발동), ZZZ의 강화 스왑은 "게이지가 찼을 때 교대가 강력한 화력 이벤트가 되는" 패턴이다.

**현재 프로젝트 상태**
`PartyManager.cs:261`에 `bool isSwapSpecial = false;` 주석("풀 게이지 스왑 특수공격은 임시 비활성화. 우선 일반 스왑 공격만 사용한다.")으로 기능 자체가 차단돼 있다. 즉 코드 골격은 있으나 발동 경로가 막혀 있다.

**제안**
1. 발동 조건 정의: 입장(또는 퇴장) 캐릭터의 게이지 충족(`PlayerSkillGauge` Ultimate/Ability 게이지 또는 §5.1 협주 에너지) 시 `isSwapSpecial = true`로 분기.
2. 발동 시 일반 Entry Attack 대신 특수 스왑 공격 모션 + 강화 효과(데미지·범위·슈퍼아머)를 적용하고 게이지를 소비.
3. UI: 풀 게이지 시 스왑 가능 캐릭터 슬롯에 발동 가능 표시.

```csharp
// 의사코드 — 실제 구현 아님 (PartyManager.cs:260 일대)
bool isSwapSpecial =
    !isSwapEvade && !isAssist &&
    swap.GetGauge(targetType).IsSwapSpecialReady; // 게이지 게이트
if (isSwapSpecial)
{
    _player.QueueSwapSpecialAttack(entryTarget); // 신규 큐
    swap.GetGauge(targetType).ConsumeSwapSpecial();
}
else if (TryFindEntryAttackTarget(...)) { _player.QueueEntryAttack(...); }
```

**기대 효과**
스왑이 화력 이벤트가 되어 ②층(스왑 협력) 정체성 강화. 이미 존재하는 게이지/큐 인프라를 재사용하므로 신규 시스템 부담이 작다.

**대략적 구현 비용: 중(M)** — 발동 게이트 + 신규 모션/스테이트 1종 + 게이지 소비 + UI 표시.

---

### 4.3 어시스트 스왑 → 패리 결과 연결 완성

> **✅ 구현 완료 (2026-06-11)** — 패리 윈도우 우선 방식. `PlayerCombat.OpenAssistParryWindow`/`IsAssistParryWindow`,
> `DefenseResolver`의 `IsAssistParryWindow` 라우팅(Unblockable 제외), `PlayerActor.OpenAssistParryAndQueueFallback`/
> `OnParrySuccess` 어시스트 분기, `PartyManager.RequestSwapTo`의 isAssist 분기. 창 비소비 만료 시 기존 즉시공격 폴백.
> 적 경직은 기존 `MonsterActor.OnParried`(스턴) + 반격 카운터(`isCounterAttack`) 재사용. **재제안 금지.**

**레퍼런스 게임 메커니즘**
ZZZ 어시스트 패리: 적의 공격 타이밍에 교대하면 **입장 캐릭터가 그 공격을 패리**하고, 적을 경직시키며 후속 강공으로 잇는다. 교대가 곧 방어+반격 이벤트가 된다.

**현재 프로젝트 상태**
`QueueSwapAssist`(`PartyManager.cs:270,272`)로 어시스트 스왑 큐는 존재하지만, 입장 캐릭터가 들어오면서 적 공격을 **패리 판정으로 처리하고 보상으로 연결하는 경로가 미완**이다. Danger Ring(`UI_DangerRing.cs`) 타이밍 인프라와 `DefenseResolver`의 패리 판정은 별도로 존재하나 어시스트 스왑과 묶이지 않았다.

**제안**
1. 어시스트 스왑 발동 시 입장 캐릭터에 짧은 **패리 윈도우**를 부여(클래시 패리/퍼펙트 가드 창 패턴 재사용).
2. 그 윈도우 중 피격 시 `DefenseResolver` 패리 경로로 라우팅 → 적 경직(`AttackReactionType.Light`/패리 stagger 패턴) + 입장 캐릭터 반격 큐.
3. Danger Ring을 어시스트 타이밍 텔레그래프로 피기백(빨강=Unblockable은 어시스트 패리 불가로 구분, 노랑=패리 가능).

```csharp
// 의사코드 — 실제 구현 아님
void OnAssistSwapEntered(GameActor entrant, Threat threat)
{
    entrant.OpenAssistParryWindow(ASSIST_PARRY_WINDOW); // 퍼펙트가드 창 재사용
    // 윈도우 중 피격 → DefenseResolver가 Parried로 판정
    // 패리 성공 → threat.Source 경직 + entrant.QueueParryCounter();
}
```

**주의(보존 제약)**
- 빨강 Danger Ring(Unblockable) 공격은 어시스트 패리 대상에서 제외해 회피 강제 원칙을 깨지 않는다.
- 기존 클래시 패리/퍼펙트 가드 반격창과 보상이 중첩 발동하지 않도록 우선순위를 정의(어시스트 패리 발동 시 일반 패리창 소비 처리).

**기대 효과**
교대가 회피이자 반격 트리거가 되어 ②층 협력의 핵심 루프가 완성된다. 인프라(큐+패리 판정+Danger Ring)가 이미 있어 "연결"만 남았다.

**대략적 구현 비용: 중(M)** — 윈도우 부여 + 판정 라우팅 + 보상 큐 + Danger Ring 구분.

---

## 5. Tier 2 — 루프 심화

### 5.1 협주형 스왑 에너지

**레퍼런스 게임 메커니즘**
명조 협주 에너지: 적 타격·스킬 사용·저스트 회피로 충전되며, 가득 차면 스왑 시 퇴장 반주 + 입장 변주 스킬이 동시 발동한다. **교대 화력의 전용 리소스**다.

**현재 프로젝트 상태**
이에 해당하는 리소스가 코드에 **부재**(확인됨). 다만 `PlayerSkillGauge`가 "공격 종류별 충전 테이블 + 풀충전 게이트" 패턴을 이미 갖고 있어 동일 패턴 복제가 가능하다.

**제안**
- 파티 공유 또는 캐릭터별 "협주 에너지" 리소스 신설. 충전원: 공격 적중 / 패리 성공 / 퍼펙트 도지 (`PlayerSkillGauge` 충전 테이블 패턴 복제).
- 가득 시 §4.2 풀 게이지 스왑 특수공격의 게이트로 사용하거나, 쿨다운 무시 스왑 등 강화 옵션을 활성화.

```csharp
// 의사코드 — 실제 구현 아님 (PlayerSkillGauge 충전 테이블 패턴 복제)
class ConcertoEnergy {
    void OnHitLanded(AttackKind k) => Add(table.Hit[k]);
    void OnParrySuccess()          => Add(table.Parry);
    void OnPerfectDodge()          => Add(table.PerfectDodge);
    bool IsFull => current >= max; // §4.2 게이트로 사용
}
```

**기대 효과**
스왑 특수공격(§4.2)에 "벌어서 쓰는" 리소스 루프가 붙어 ②층 협력이 자체 보상 사이클을 갖는다.

**대략적 구현 비용: 중~대(M~L)** — 신규 리소스 + 충전 훅 다수 + UI. §4.2와 묶어 진행 권장.

---

### 5.2 입장 강화 라우트 다양화

> **✅ 구현 완료 (2026-06-11)** — 입력패턴 콤보 라우트 대신 **타깃 상태 기반 등장 변형 선택기**로 구현(문서 의사코드와 의도적 편차).
> `PlayerAttackDataSO.entryAttackVsGroggy`/`entryAttackVsAirborne` + 명시 토글 `useEntryAttackVsGroggy/Airborne`(기본 off=기존 동작),
> `PlayerCombat.SelectEntryAttackInfo`(공중>그로기 우선, `BreakGauge.IsExposed`/Stun/Knockdown으로 그로기 판정),
> `SetPendingEntryTarget`로 타깃 전달. `JustSwapped` 타이밍 태그 불필요. **재제안 금지.**

**레퍼런스 게임 메커니즘**
명조·PGR은 교대/회피 직후 상태에 따라 다른 강화 스킬/체인이 나가도록 분기시킨다. 같은 교대라도 맥락에 따라 다른 모션이 나와 단조로움을 피한다.

**현재 프로젝트 상태**
Entry Attack은 있으나 단일 모션이다(`QueueEntryAttack` → `PlayerAttackState.cs:207`). `ComboRouteRunner`(`ComboRouteRunner.cs`)는 입력 시퀀스 토큰 + GameplayTag/grounded 조건으로 분기를 이미 지원한다.

**제안**
- `ComboRouteRunner`에 "스왑 직후" GameplayTag(예: `JustSwapped`)를 부여하고, 그 태그 조건으로 Entry Attack 라우트를 입력/상태에 따라 분기.
- 예: 스왑 직후 + 적 그로기 → 강공 라우트 / 스왑 직후 + 공중 적 → 런치 라우트.

```csharp
// 의사코드 — 실제 구현 아님
// 스왑 직후 일정 시간 JustSwapped 태그 부여 → ComboRouteRunner 조건에서 분기
if (tags.Has(JustSwapped) && target.IsGroggy)
    route = forcedAttackAction.SwapHeavy;
```

**기대 효과**
교대마다 다른 화력 표현 → ②층 깊이. 기존 콤보 라우트 인프라 재사용으로 신규 시스템 없음.

**대략적 구현 비용: 중(M)** — 태그 부여 시점 + 라우트 데이터 추가.

---

## 6. Tier 3 — 대형/위임/보류

### 6.1 궁극기 버스트 — 별도 문서로 설계 위임

ZZZ 연쇄 콤보 스킬, 명조 공명해방급의 "궁극기 버스트"는 본 문서 범위에서 다루지 않는다. 별도 설계서 `ULTIMATE_SEQUENCE_SYSTEM_DESIGN.md`로 위임한다. 본 문서의 §4.2 풀 게이지 스왑 특수공격, §5.1 협주 에너지가 그 전 단계 인프라를 부분적으로 공유한다.

**대략적 구현 비용: 대(L)** — 별도 문서.

### 6.2 히트 리액션 Phase 2·3 포인터

타격감(히트스톱·VFX·SFX·플린치 차별화)의 심화는 `HIT_REACTION_ADVANCEMENT_DESIGN.md`의 Phase 2·3에서 다룬다. 본 문서가 추가하는 행동 불능 배율(§4.1)과 스왑 화력 이벤트는 그 연출 강화 위에서 체감이 배가되므로, 두 로드맵을 함께 진행할 때 시너지가 크다(권장 타격감: 히트스톱 0.05~0.1초, 극한 회피 슬로우 약 0.3배/0.2초).

**대략적 구현 비용: 대(L)** — 별도 문서.

### 6.3 공중 콤보 추격 — 보류 가능

**레퍼런스**: DMC/베요네타식 런치 후 공중 추격 콤보.
**현재 상태**: `EnemyAirborneState` 런치는 있으나 플레이어의 공중 추격 루프는 없다.
**보류 사유**: 싱글 TPS + 소울라이크 톤에서 본격 공중 콤보는 카메라/이동 부담이 크고 우선순위가 낮다.
**저비용 대안**: 런치 후 낙하 중 추가 타격이 적중하면 **체공을 연장**(낙하 속도 일시 감쇠)하는 정도의 가벼운 처리만 도입. 이것만으로 "띄우고 마무리" 손맛이 생긴다.

```csharp
// 의사코드 — 실제 구현 아님
// 공중(EnemyAirborneState) 적 피격 시 낙하 감쇠로 체공 연장
if (target.IsAirborne && hitLanded)
    target.ExtendHangTime(HANG_EXTEND_PER_HIT); // 누적 상한 둘 것
```

**대략적 구현 비용: 저비용 대안 소(S)** / 본격 공중 콤보 중(M).

---

## 7. 비도입 결정

| 메커니즘 | 비도입 사유 |
|----------|------------|
| **원소 반응(원신)** | 본 프로젝트에 속성 시스템 자체가 없다. 속성 부여·반응 테이블·ICD 등 신규 기반이 필요해 **재설계급 비용**이며, 본 로드맵의 "기존 인프라 연결" 방향과 어긋난다. |
| **스타일 랭크(DMC/붕괴3rd)** | 화려한 콤보 과시 보상은 **소울라이크의 긴장·무게 톤과 충돌**한다. 본 프로젝트 정체성과 맞지 않음. |
| **무제한 스왑(원신식 무쿨 교대)** | 싱글 TPS에서 쿨 없는 교대는 회피·취소 남용으로 **난이도 곡선이 붕괴**한다. 본 프로젝트는 스왑 쿨다운(`RecordSwapCooldown`)을 유지하고, 협주 에너지(§5.1)로 "벌어서 쓰는" 강화만 허용한다. |

---

## 8. 기존 제안 문서와의 관계

- `COMBAT_SYSTEM_NEXT_IMPROVEMENT_PROPOSAL.md` — **아키텍처 축**(데미지 해결 파이프라인, 컴포넌트 구조 등 코드 구조 개선).
- 본 문서 — **게임플레이 축**(루프·체감·장르 정체성).
- 두 문서의 접점은 **§4.1 행동 불능 통합 데미지 배율**이다. 이 항목은 데미지 해결 지점(`DamageResolver`)을 건드리므로, 아키텍처 문서가 제안하는 데미지 파이프라인 정리와 **같은 코드 영역**에서 만난다. 두 작업을 동시 진행할 경우 §4.1의 배율 합성 규칙을 파이프라인 리팩터링에 맞춰 결정할 것.

---

## 9. 참고자료

**나무위키**
- 명조/전투, 젠레스 존 제로/전투, 원신/전투, 원신/원소, 붕괴3rd/전투 시스템, 퍼니싱 그레이 레이븐/시스템/전투 시스템, 베요네타, 단테(데빌 메이 크라이)/스타일

**HoyoLab**
- ZZZ 초보자 가이드: `https://www.hoyolab.com/article/28988180`
- ZZZ 속성 이상 메커니즘: `https://www.hoyolab.com/article/30590858`

**게임 가이드(블루스택)**
- 명조 무기·스킬 가이드 / ZZZ 전투 가이드 / PGR 가이드

**BitTopup Wiki**
- 원신 원소반응 ICD 가이드 / ZZZ 회피·패링 타이밍 가이드

**공식 사이트**
- 명조: `https://wutheringwaves.kurogames.com`
- ZZZ: `https://zenless.hoyoverse.com`
- 원신: `https://genshin.hoyoverse.com`
- 붕괴3rd: `https://honkaiimpact3.hoyoverse.com`
- PGR: `https://pgr.kurogames.com`
