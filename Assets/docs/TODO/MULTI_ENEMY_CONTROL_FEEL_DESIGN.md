# 다인 전투 조작감 개선 설계 문서 (스턴락·연속 피격 누수 해소)

> 작성일: 2026-06-13
> 대상 버전: Unity 6 (6000.0.60f1), URP
> 분류: 설계서. **§4.1·§4.2는 2026-06-13 구현 완료**(하단 §8), 나머지는 계획.
> 레퍼런스: God of War / Aztez / Smash Bros / Killer Instinct / 격투게임 wakeup·iframe 이론 등 웹 조사 결과(하단 출처)

---

## 1. 문제 정의

> "여러 명한테 두들겨 맞으면서 조작이 불가능해지는 동안 플레이어에게 유발되는 불편함."

다인 전투에서 플레이어가 **통제권을 잃은 채 계속 얻어맞는** 구간이 생긴다. 단일 리액션 1회는 의도된 패널티지만, 여러 적의 히트가 겹치면 **"내가 아무것도 못 하는 시간"이 누적·연장**되어 불공정하게 느껴진다.

본 문서는 AAA 기법을 나열하는 것이 목적이 아니다. **이미 갖춰진 방어 장치를 제외하고 남는 실제 누수만** 지목하고, 프로젝트 구조에 맞는 최소 침습 해법을 제시한다.

---

## 2. 이미 존재하는 방어 장치 (재발명 금지)

| 장치 | 현재 동작 | 파일 |
|------|----------|------|
| 그룹 공격 슬롯 | 동시 근접 2 / 원거리 2 제한 + breather(0.6s) + formation 8슬롯 + aggro fitness | `Group/MonsterGroupController.cs` |
| 단일 리액션 무한연장 방지 | Hit/Grabbed 상태 중엔 새 Hit 상태 **재진입 차단**(`IsAlreadyHitOrGrabbed`) | `Combat/Resolution/ReactionResolver.cs:62` |
| 기상 무적 | Knockdown 진입·기상 시 `_invincibleTimer`(0.4s/0.3s) | `State/Player/PlayerKnockdownState.cs:13,34,78` |
| dodge-cancel 윈도우 | Hit 경직 중 일정 시간 후 회피/공격 캔슬 | `State/Player/PlayerHitState.cs:136` |
| swap-evade 무적 | 캐릭터 교체 회피 시 데미지 무적 | `Object/Player/PlayerActor.cs:92,129` |

→ **"보호가 없어서"가 아니다.** 이미 꽤 정교하다. 그럼에도 조작 불가가 발생하는 **특정 누수 두 곳**이 원인이다.

---

## 3. 실제 누수 분석 (검증 완료)

### 누수 ① (지배적) — 다중 피격 시 플레이어 히트스톱이 **매 히트마다 재시작**

`PlayerActor.OnDamaged()`는 가드 밖에서 **무조건** 히트스톱을 호출한다.

```
OnDamaged() → CombatFeedbackDispatcher.ApplyPlayerDamagedHitStop()
  → GameHitStop.ExecuteLocalImpact(attacker, victim=player, ...)
    → ExecuteActorOnly(player, duration, victimScale)
      → StopActor(player)            // 기존 freeze 중단·복구
      → 새 코루틴 시작 → player.LocalTimeScale = 낮은 값 (duration 동안)
```

- `GameActor.LocalTimeScale`은 **애니메이터 속도 + KCC 이동 deltaTime을 동시에 스케일**한다(`GameActor.cs:48`, `ActorMovementController.cs:206`). 즉 LocalTimeScale이 낮으면 **애니·이동·입력반영이 전부 멈춘다.**
- `ExecuteActorOnly`는 매 호출마다 `StopActor` 후 **새 코루틴으로 freeze를 재시작**한다(`GameHitStopHandler.cs:205~218`).
- 결과: 적 A 히트 → 플레이어 0.1s freeze 시작 → 0.05s 뒤 적 B 히트 → freeze **리셋·재시작** → 적 C … → **플레이어 LocalTimeScale이 계속 바닥에 고정**.

→ 리액션 상태(Hit)는 1회만 들어가서 "상태 기반 스턴락"은 막혀 있지만, **타격감 연출용 히트스톱이 다중 피격에서 사실상 연속 일시정지로 변질**된다. 이것이 문자 그대로 "조작 불가능"의 **가장 직접적 원인**이다.

### 누수 ② — 리액션 회복 직후 **재스턴 루프** (경직 내성 부재)

리액션 종료 흐름은 `Hit → (애니 끝) → Idle`. Idle은 1프레임만 노출돼도 그 사이 다른 적의 히트가 닿으면 `IsAlreadyHitOrGrabbed == false`가 되어 **새 Hit 상태에 다시 진입**한다.

→ `Hit → Idle(찰나) → Hit → Idle → …` 루프. 슬롯이 근접 2명을 허용하므로 두 적이 번갈아 치면 회복 틈이 사실상 없다. 웹 조사의 **stun-immunity / 회복 무적창 / combo-decay**가 정확히 이 누수를 겨냥한 기법이다.

### 누수 ③ (경미) — 물리 힘은 과장하지 말 것

`ReactionResolver`의 `shouldApplyForce`는 항상 true지만, `OnDamaged`의 임펄스 스위치는 **KnockBack/Pull/Airborne/Grab에만** 힘을 준다. plain `Hit`엔 넉백 없음. 따라서 "사방에서 밀쳐진다"는 군중 적의 `EnemyAttackData.reactionType`이 실제로 넉백류일 때만 성립 → **데이터 점검 항목**으로 격하(§6).

---

## 4. 개선안

> 설계 제약(전 항목 공통): **리액션 면역 ≠ 데미지 면역.** 통제권은 돌려주되 데미지·체력 압박은 유지한다. 안 그러면 다인전의 위협 자체가 증발한다.

### 4.1 [1순위] 플레이어 피격 히트스톱 누적 차단 — 누수 ① 해소

**가장 효과 대비 비용이 좋은 단일 수정.** 피드백 레이어만 손대므로 전투 로직·상태머신 무영향.

방향(택1, A 권장):

- **A. 재시작 억제 + 쿨다운(권장).** 플레이어가 victim일 때, 이미 `IsActorHitStopping(player)`면 **새 freeze로 갈아끼우지 않는다.** 더 강한 스케일이 들어와도 *남은 시간만 갱신*하고 재시작 금지. 추가로 "직전 피격 히트스톱 종료 후 N초(예: 0.15s)는 플레이어 victim 히트스톱 생략" 쿨다운을 둔다.
- B. 윈도우 내 누적 상한. 0.3s 슬라이딩 윈도우 동안 플레이어 victim 히트스톱 총량을 상한(예: 0.12s)으로 캡.
- C. 리액션 상태 중 victim 히트스톱 전면 생략(이미 멈춰 있으니 중복 불필요).

구현 위치: `GameHitStopHandler.ExecuteActorOnly`(또는 victim 전용 분기) + `CombatFeedbackDispatcher.ApplyPlayerDamagedHitStop`. 공격자 측 히트스톱(타격감)은 **그대로 유지** — 플레이어 victim freeze만 제어.

> 주의: 공격자(적)에게 거는 히트스톱은 다인전 타격감을 위해 보존한다. 이 수정의 범위는 **플레이어 자신의 LocalTimeScale freeze**에 한정.

### 4.2 [2순위] 피격 후 "경직 내성(stagger immunity)" 그레이스 윈도우 — 누수 ② 해소

리액션 상태에서 빠져나온 직후 짧은 창 동안, **새 리액션 상태 진입만 차단**(데미지·VFX·소량 플린치는 유지)한다.

- 기존 `_swapEvadeInvincibleEndTime` 타이머 패턴을 복제해 `_staggerImmuneEndTime` 도입.
- 부여 시점: `PlayerHitState`/`PlayerStunState`/`PlayerKnockdownState`가 Idle로 빠져나갈 때 `GrantStaggerImmunity(duration)`.
- 소비 지점: `ReactionResolver.ResolvePlayerReaction` — `PlayerReactionQuery`에 `IsStaggerImmune` 필드를 추가하고, true면 `shouldEnterState = false`(데미지는 `TakeDamage` 본류에서 이미 적용되므로 무관).
- 강도 차등(권장): 경직 내성 중이라도 Heavy/Knockdown/Airborne/Grab 같은 **중리액션은 통과**시키고, 일반 Hit·Light만 무시 → "큰 한 방엔 여전히 흔들리되, 잡몹 따다닥엔 안 끊긴다".
- 다이내믹 길이: 연속 피격이 많을수록 내성창을 늘리는 **diminishing returns**(combo-decay 변형) 옵션. `_consecutiveHitCount` 추적, 일정 시간 무피격 시 리셋.

> 이미 `IsAlreadyHitOrGrabbed`가 "상태 중" 재진입을 막으므로, 본 항목은 "상태 **사이**(Idle 찰나)" 재진입을 막아 둘이 상보적으로 루프를 닫는다.

### 4.3 [3순위] 플레이어 stagger 중 그룹 AI 공격 슬롯 양보

플레이어가 리액션 상태일 때, 그룹이 짧은 **player-breather**에 진입해 새 공격 슬롯 부여를 잠시 보류 → 회복 박자(beat)를 만든다.

- `MonsterGroupController`의 기존 `_groupBreatherUntil`(멤버 공격 종료 후 breather) 메커니즘을 **확장**: 플레이어가 Hit/Stun/Knockdown 진입을 그룹에 통지하면 `_playerBreatherUntil = Time.time + X`를 세팅, `RequestAttackSlot`이 이 창 동안 신규 점유를 거부.
- 통지 경로: `PlayerActor.OnDamaged` → 가까운/타깃팅 그룹에 이벤트. 또는 적 측 `EnemyTacticalMemory`가 플레이어 상태를 읽어 자체 보류.
- 효과: 누수 ①·②를 코드로 닫더라도 **체감 페이싱**(연출적 "숨 돌릴 틈")을 보강. 1·2순위 적용 후 잔여 불편이 남을 때만 진행.

### 4.4 [강등/선택] Directional Influence (DI)

스태거 중 스틱 방향으로 넉백 궤적을 일부 보정해 **agency**를 돌려주는 격투게임 기법. 단 3D KCC TPS에서 카메라 상대 방향·물리 보정 튜닝이 까다로워 **헤드라인에서 제외**. 1~3순위로 체감이 충분하면 불필요.

---

## 5. 우선순위·리스크 요약

| 순위 | 항목 | 해소 누수 | 침습도 | 리스크 |
|------|------|----------|--------|--------|
| 1 | 플레이어 히트스톱 누적 차단(§4.1) | ① 지배적 | 피드백 레이어 한정 | 낮음 (전투로직 무영향) |
| 2 | 경직 내성 그레이스 창(§4.2) | ② 재스턴 루프 | 리액션 리졸버 + 상태 3종 | 중 (밸런스 튜닝 필요) |
| 3 | 그룹 슬롯 양보(§4.3) | 페이싱 보강 | 그룹 컨트롤러 | 중 |
| - | DI(§4.4) | agency | 상태머신 다수 | 높음(강등) |

권장 적용 순서: **§4.1 단독 적용 후 체감 재평가 → 부족하면 §4.2 → 그래도 부족하면 §4.3.** 한 번에 다 넣지 말 것(원인 분리 평가 불가해짐).

---

## 6. 선행 데이터 점검 (구현 전)

- 군중 잡몹(예: `Enemy_Random_*` 휴머노이드)의 `EnemyAttackData.reactionType` 분포 확인. 다수가 KnockBack/Pull류면 누수 ③도 실효 → 일반 잡몹 기본 타격은 `Hit`/`Light`로 정렬 권장(중공격만 넉백).
- `MonsterGroupController._maxMeleeAttackers`(현재 2)·`_breatherDuration`(0.6s)이 씬별로 적정한지. 누수 ①·② 해소 후 슬롯 수 재튜닝 여지.

---

## 7-A. 추가 검토 — 통념 8개 항목 vs 현재 코드베이스

다인전 개선 통념 리스트(공격 스케줄링/슈퍼아머/피격 보호/오프스크린 경고 등)를 코드와 대조한 결과. **상당수가 이미 구현돼 있어 "재구현"이 아니라 "검증/튜닝/확장"이 맞다.**

| 통념 항목 | 현재 상태 | 결론 |
|----------|----------|------|
| ① 공격 스케줄링(동시 공격자 제한) | **이미 존재** — `MonsterGroupController`(근접2/원거리2)+breather+formation 원형배치, `RequestEnemyAttackSlotNode`로 BT 연결. **단 `RequestAttackSlot`은 그룹 비소속 적에게 `return true`(무제한)** | 구현 불필요. **씬에서 적이 `MonsterGroupController` 밑에 묶여 있는지 검증**이 핵심(안 묶이면 스케줄링이 0). |
| ② 작은 피격 경직 없음(Poise 게이팅) | 플레이어는 Poise 미사용(`PoiseStat`은 몬스터 전용). 플레이어는 `attackData.reactionType` 직결 | §4.2 경직 내성으로 **유사 효과 달성**(Light/Hit 무시). 본격 플레이어 Poise는 별도 대형 작업 → 보류. |
| ③ 플레이어 슈퍼아머(공격 등급별) | 부분 존재 — `PlayerChargeState.HasChargedAtLeastOneStage`, 상태별 `SuppressesHitReaction` | 차지만 커버. Light/Heavy/Skill/Ultimate 등급 슈퍼아머는 **A급 미구현**(별도). |
| ④ 피격 보호 시간(stagger protection) | **없었음** | **§4.2로 구현 완료.** |
| ⑤ 회피 직후 안전시간 | 부분 존재 — PerfectDodge/swap-evade 무적 | 퍼펙트 회피 후 잔여 안전창은 미검증 → 후속 확인 항목. |
| ⑥ 오프스크린 공격 경고 | 설계 존재 — `docs/Complete/OFFSCREEN_THREAT_INDICATOR_DESIGN.md`, Danger Ring UI 구현됨 | 구현 여부 점검 후 연계. **별도 작업.** |
| ⑦ 긴급 탈출기(부조화 Burst) | 몬스터 Break/부조화는 존재, 플레이어 Burst Escape는 없음 | **B급 신규.** 별도. |
| ⑧ 적 충돌/Combat Radius(접근 인원 제한) | formation 슬롯이 원형 배치 담당, 분리(separation)는 미확인 | formation 확장 영역. **별도 점검.** |

→ 이번 PR은 **데이터 검증으로 가려진 ①을 제외**하고, 누수 분석상 지배적이며 미구현이던 **누수①(히트스톱)·④(stagger protection)** 두 개를 구현했다.

## 8. 구현 현황 (2026-06-13)

**§4.1 — 플레이어 피격 히트스톱 누적 차단 (누수①, A안)**
- `CombatFeedbackDispatcher.ApplyPlayerDamagedHitStop`: 플레이어가 이미 `IsActorHitStopping`이면 히트스톱을 **재시작하지 않고 early-return**. 첫 타격 임팩트는 유지, 연속 누적 freeze만 차단.

**§4.2 — 경직 내성(Stagger Protection) 그레이스 창 (누수②/통념④)**
- `PlayerActor`: `_staggerImmuneEndTime` 필드 + `IsStaggerImmune` 프로퍼티 + `GrantStaggerImmunity(duration)` 메서드, `StaggerImmunityDuration = 0.3f` 상수.
- `ReactionResolver.PlayerReactionQuery`: `IsStaggerImmune` 필드 추가. `ResolvePlayerReaction`에서 내성창 + 약한 리액션(`IsMinorPlayerReaction`: None/Light/Hit)이면 상태 전환·카메라 피드백 억제(데미지·힘 분기는 무관 유지). 큰 리액션은 통과.
- `PlayerActor.OnDamaged`: 쿼리에 `IsStaggerImmune` 전달.
- `PlayerHitState.OnHitEnd` / `PlayerStunState`(회복) / `PlayerKnockdownState.TransitionOut`: Idle 자연 종료 시 `GrantStaggerImmunity` 호출.
- `OnDamaged`: 경직 내성으로 흡수된 약한 피격(`IsStaggerImmune` + `IsMinorPlayerReaction`)은 **히트스톱까지 생략**(컬러 플래시·HitFx는 유지). §4.1 스로틀이 *진행 중*만 막는 데 반해, 흡수 구간의 freeze 깜빡임까지 제거해 "데미지 O·경직 X"를 체감으로 완성.

**스코프 제외(의도):** `PlayerAirborneState`/`PlayerGrabbedState` 회복에는 내성 미부여 — 저글 후 착지 재피격은 별도 검토. Hit/Stun/Knockdown 3종만 커버.

**검증 상태:** 코드 작성 완료. CLI 빌드 없음(Unity 프로젝트) + IDE 진단 타임아웃으로 **컴파일은 Unity 재컴파일로 확인 필요**. 심볼·패턴은 기존 코드 기준 수동 확인.

**불변 제약 준수:** 데미지·체력 압박은 그대로(리액션 면역 ≠ 데미지 면역). Heavy/넉백/다운/스턴/잡기 등 큰 리액션은 내성창에도 통과 → 위협 유지.

**튜닝 포인트:** `StaggerImmunityDuration`(0.3s), §4.1 쿨다운 추가 여부. 플레이 테스트로 조정.

## 7. 웹 조사 출처

- God of War / Aztez 공격 동시성·attack ticket 이론: https://signalsandlight.substack.com/p/how-do-simultaneous-enemy-attacks
- 근접 전투 적 AI·스턴락 방지 설계: https://www.gamedeveloper.com/design/enemy-design-and-enemy-ai-for-melee-combat-systems
- 3인칭 멜리 스턴락 방지 토론: https://gamedev.net/forums/topic/672213-preventing-stun-lock-issues-in-third-person-melee-combat/
- 무적 프레임·wakeup·meaty 이론: https://wiki.supercombo.gg/w/The_Wakeup_Game
- combo decay / 넉백 기반 콤보 falloff (Smash/KI): https://ki.infil.net/basics3.html
