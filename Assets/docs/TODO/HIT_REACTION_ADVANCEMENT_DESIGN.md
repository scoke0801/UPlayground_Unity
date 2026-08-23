# 전투 피격 리액션 고도화 설계 (개정판)

> 최초 작성: 2026-05-24 / **개정: 2026-08-14 (현재 코드 기준 전면 재판정)**
> 대상: Unity 6, URP
> 분류: 설계서(미구현 계획). 구현 PR 시 본 문서에서 가이드 문서를 별도 추출한다.

## 0-A. 구현 현황 (2026-08-14)

| 항목 | 상태 |
|------|------|
| 충돌음 소유권을 피격자로 통일 (`PlayDamageImpact`) | **적용** — 몬스터 피격이 자체 재생, 공격자측(`PlayerCombatPresenter`) 호출 제거 |
| 임팩트 사운드 티어 (Critical / Break / Heavy / Light) | **적용** — 미등록 키는 Heavy/Light로 폴백(`ISoundService.HasSound`) |
| 히트 FX 위치·회전 | **적용** — hitPoint 우선 + `attackDirection` 정렬 |
| 환경 넉백 T0 (벽 충돌) | **적용** — `WallImpactResolver` + `EnemyHitState.OnMovementHit` |
| 리액션 저작 축 GAS 단일화 | **적용** — State 직접 전환 폴백과 `useTagTriggered*` 플래그 제거 |
| 행동별 리액션 변형(§9-3) | 미구현 — 저작 축이 GAS로 확정됐으므로 Ability/Variant로 저작 |

**미해결 선결 조건:** 보스 AbilitySet 5종(Bokusei/Hichi/Lian/Lili/Siuha)에 `GA_Monster_Hit_*` 리액션 Ability가 없다. 폴백이 제거됐으므로 이 보스들은 리액션이 재생되지 않는다. 런타임에서 `MonsterActor`가 Error 로그로 알린다.

**사운드 데이터 미저작:** `Combat_Hit_Critical`, `Combat_Hit_Break`, `Combat_WallImpact` 엔트리는 아직 없다. 등록 전까지는 각각 Heavy로 폴백되므로 무음이 되지는 않는다.

---

## 0. 이 개정판이 필요한 이유

초판(2026-05-24)은 `PlayerCombat.ApplyHitFeedback()`이 히트스톱을 직접 결정하고 `OnDamaged()`가 피드백을 직접 호출하던 구조를 전제로 썼다. 이후 다음이 바뀌어 초판의 지목 지점 상당수가 더 이상 존재하지 않는다.

- 전투 판정이 `CombatResolutionPipeline` → `HitContext`/`ReactionResolver`/`DamageResolver` → `CombatFeedbackDispatcher`로 분리됨.
- `PlayerAttackDataSO`가 제거되고 공격 데이터의 단일 소스가 `AbilitySetSO`(GAS)로 이관됨.
- 히트스톱이 모션 후딜 기반 자동 산출(`AttackReactionData.hitStopDuration/Scale`) + 비대칭 프리즈로 바뀜.
- 몬스터 피격 리액션에 **GAS 태그 트리거 경로**(`UseTagTriggeredHitReaction`)가 추가되어 리액션 저작 축이 2개가 됨.

따라서 본 개정판은 초판의 갭 G1~G11을 현재 코드로 다시 판정하고, **지금 손대야 할 순서**를 다시 정한다.

---

## 1. 현재 피격 파이프라인 (검증된 사실)

| 단계 | 담당 | 위치 |
|------|------|------|
| 진입 | `CombatResolutionPipeline.Execute` → `ICombatResolvable` 위임 | `Combat/Resolution/CombatResolutionPipeline.cs:12` |
| 히트 입력 값 객체 | `HitContext` (공격자/피격자/reaction/poise/break/치명배율/hitPoint/방향/`ReactionData` 전부 보유) | `Combat/Resolution/HitContext.cs:13` |
| 리액션 판정 | `ReactionResolver.ResolvePlayerReaction` / `ResolveMonsterReaction` (등급 정책·`guaranteedReaction`·경직 내성 반영) | `Combat/Resolution/ReactionResolver.cs:57,81` |
| 피드백 실행 | `CombatFeedbackDispatcher` (플로터/히트FX/충돌음/히트스톱/카메라) | `Combat/Feedback/CombatFeedbackDispatcher.cs` |
| 플레이어 공격측 표현 | `PlayerCombatPresenter.PresentHitResult` | `Component/Player/PlayerCombatPresenter.cs:22~31` |
| 플레이어 피격측 표현 | `PlayerActor.Combat.cs` (`ShowHitFx` + `ApplyColorHit` + 카메라) | `Object/Player/PlayerActor.Combat.cs:294,430` |
| 몬스터 피격측 표현 | `MonsterActor.ApplyResolvedHit` (`ApplyColorHit`만) | `Object/Monster/MonsterActor.cs:532` |
| 리액션 모션 | State 경로(`PlayerHitState`/`EnemyHitState`) **또는** GAS 태그 트리거 경로 | `State/Player/PlayerHitState.cs:228`, `MonsterActor.cs:528,630` |

**핵심 소득:** `HitContext`가 피격 컨텍스트를 이미 전부 들고 피드백 레이어까지 도달한다. 초판이 "정보가 없어서 못 한다"고 본 항목 대부분은 이제 **분기만 쓰면 되는 상태**다.

---

## 2. 초판 갭 재판정 (G1~G11)

| # | 초판 갭 | 2026-08-14 판정 | 근거 |
|---|---------|----------------|------|
| G1 | 히트스톱이 공격자 attackKind로만 결정 | **부분 해결**. 이제 `AttackReactionData`(모션 후딜 자동 산출)가 1순위, attackKind는 폴백. 다만 **피격자 컨텍스트(poise break·Break 노출·치명)는 여전히 미반영** | `CombatFeedbackDispatcher.cs:295,341` |
| G2 | 적 공격에 공격자측 피드백 없음 | **미해결**. `EnemyCombat.cs`에 히트스톱/셰이크/피드백 호출이 단 한 건도 없음 | grep 0건 |
| G3 | 상체 가산 플린치 부재 | **미해결이나 비용 급감**. `ActorAnimator.PlayUpperBodyOverlay`/`_upperBodyMask` 인프라 완비, 현재 `PlayerDrinkState`만 사용 | `Animation/ActorAnimator.cs:18,696` |
| G4 | 임팩트 VFX 단일·방향 무관 | **미해결**. `ShowFX`는 rotation/parent 오버로드를 이미 갖고 있으나 전투 호출부가 position만 전달. 피격측은 hitPoint 대신 Center 소켓 사용 | `PlayerActor.Combat.cs:291~294`, `CombatFeedbackDispatcher.cs:55~69` |
| G5 | 피격 연결 기반 임팩트 SFX 부재 | **절반 해결**. `PlayDamageImpact`가 신설되어 "실제 피해 적용 시점"에 울림(헛스윙 분리 완료). 그러나 ① 키가 `CombatHitLight/Heavy` **2종뿐** ② **플레이어 공격 경로에만 연결**됨 | `CombatFeedbackDispatcher.cs:80`, `PlayerCombatPresenter.cs:30` |
| G5-인프라 | 풀링/피치 유틸 신규 필요 | **불필요해짐**. `SoundManager`가 2D/3D AudioSource 풀 + 엔트리별 랜덤 피치를 이미 제공 | `Manager/Sound/SoundManager.cs:672,699` |
| G6 | 4방향 리액션만 | **미해결 + 선행 결정 필요**(§4 참조) | `PlayerHitState.cs:258~263`, `EnemyHitState.cs:137` |
| G7 | 진동/스쿼시 없음 | **미해결**. 게임패드 햅틱은 `GamepadCoreApi`에 네이티브 계층만 존재, 전투 미연결 | `Input/GamepadCore/GamepadCoreApi.cs:506~546` |
| G8~G11 | 환경 넉백 / 라그돌 / 저글 / 부위 크리티컬 | 미해결. 본 개정판에서도 **보류**(§6) | — |

### 초판 이후 별도로 해결된 항목 (재제안 금지)
- 비대칭 히트스톱(피격자 풀프리즈 / 공격자 약하게) — `ExecuteLocalImpact(victimTimeScale)`
- 다인 전투 히트스톱 재시작 억제 + 경직 내성창(`IsStaggerImmune`)
- 등급별 리액션 정책(`CombatReactionPolicySO`) 및 `guaranteedReaction` 우회
- 피격 모션 강제(`victimForcedMotionSlot`), Break 게이지/노출 배율

---

## 3. 지금 손대야 할 순서

### Step 1 — 청각·시각 임팩트 연결 (저위험·즉효, 코드량 최소)

가장 큰 체감 손실이면서 가장 싼 항목이 여기 모여 있다. 판정·밸런스 코드는 건드리지 않는다.

**1-1. 피격측 임팩트 사운드 연결 (신규 발견, 최우선)**
- 현재 `PlayDamageImpact`는 `PlayerCombatPresenter`(= 플레이어가 때릴 때)에서만 호출된다. **적이 플레이어를 때릴 때는 충돌음이 없다.**
- `PlayerActor.Combat.cs`의 피격 경로(일반 피격 / 가드 브레이크)에서 동일 함수를 호출한다.
- 주의: 공격자 경로와 이중 호출되지 않도록, "누가 부르는가"를 **피격자 소유**로 통일할지 **공격자 소유**로 통일할지 먼저 확정한다. 권장은 피격자 소유(투사체·잔류 판정·환경 피해까지 한 곳에서 커버됨).

**1-2. 임팩트 사운드 티어 확장**
- `IsHeavyImpact` 2분기(`CombatFeedbackDispatcher.cs:357`)를 reactionType 축으로 확장: Light / Hit / Heavy / Break(poise 브레이크) / Critical.
- 피치 변주·풀링은 `SoundManager` 엔트리 설정으로 해결 — **코드 추가 없이 사운드 데이터만 늘리면 된다.**
- 재질(살/금속/방어구)은 이 단계에서 하지 않는다. 피격자 재질 태그가 아직 없어 신규 데이터 축이 필요하므로 Step 3로 미룬다.

**1-3. 히트 FX 위치·회전 정상화**
- 피격측 FX가 Center 소켓에서 나와 타격 지점과 어긋난다 → `hitPoint`가 유효하면 우선 사용하고 zero일 때만 소켓 폴백.
- `ShowHitFx`에 rotation 오버로드를 추가해 `HitContext.AttackDirection`으로 FX를 정렬한다(`GameObjectManager.ShowFX`가 이미 rotation/parent를 받는다).
- reactionType별 기본 FX 키 폴백(데이터 미지정 시)을 `DefaultCombatHit` 단일 폴백 대신 티어 폴백으로 교체.

### Step 2 — 히트스톱 컨텍스트화 + 적 공격 피드백

**2-1. 피격 컨텍스트 히트스톱 (G1 잔여)**
- 현재 강도의 단일 소스는 `AttackReactionData`(모션 후딜 기반 자동 산출)다. **이 값을 덮어쓰지 않는다** — 자동 산출 파이프라인이 무력화된다.
- 대신 **후처리 배율**로 얹는다: `duration *= f(poiseBrokenNow, isBreakExposed, isCritical)`, 상한은 기존 cap(0.20s) 유지.
- 배율 입력은 `CombatResult`/`ReactionDecision`에 이미 있다(poise 브레이크 여부, Break 노출, `CriticalMultiplier`).
- 제약: KillCam 조기 분기와 `PlayerGuard` 보호 경로를 우회하지 않는다.

**2-2. 적 공격 피드백 (G2)**
- `EnemyCombat` 적중 시 경량 로컬 히트스톱 + 방향성 카메라 임펄스를 추가한다.
- **강공격/특수공격 한정**으로 시작한다. 잡몹 평타까지 넣으면 다인 전투에서 §다인 설계의 누수①이 되살아난다.
- 플레이어가 이미 피격 히트스톱 중이면 재시작하지 않는 기존 가드(`IsActorHitStopping`)를 반드시 경유한다.

**2-3. 게임패드 진동 (G7 일부)**
- reaction 강도별 짧은 rumble. 히트스톱과 같은 지점에서 발화하되 별도 채널로 분리해 옵션 off가 가능하게 한다.

### Step 3 — 리액션 저작 축 결정 후 확장

**3-0. 선행 결정 (이게 없으면 아래를 시작하면 안 된다)**
몬스터 피격 리액션은 현재 **State 경로**(`ApplyMonsterReactionState` → `EnemyHitState.GetHitAnimKey`)와 **GAS 태그 트리거 경로**(`Trigger_Monster_Hit_*` → 리액션 Ability)가 공존한다(`MonsterActor.cs:528`). 8방향·부위별 리액션을 어디에 넣을지 먼저 정해야 한다.
- State에 넣으면: 전 몬스터 일괄 적용, 저작 비용 0, 대신 몬스터별 특수 리액션 표현 불가.
- GAS에 넣으면: 몬스터별 저작 가능, 대신 방향 변형만큼 Ability/Variant가 늘어난다(현재 GameplayAbility 559개).
- **권장:** 방향·높이 같은 *기계적 변형*은 State/모션 해석 축에, *연출적 특수 리액션*은 GAS 축에 둔다. 두 축이 같은 것을 두 번 표현하지 않게 경계를 문서화한다.

**3-1. 상체 가산 플린치 (G3)**
- Light/Hit을 전신 상태 전환 대신 `PlayUpperBodyOverlay` 플린치로 처리해 로코모션을 유지.
- **역할 분리 규칙(초판에서 이어짐):** Light/Hit = 가산 플린치(상태 진입 없음), Heavy 이상 = 전신 경직 + cancel-window. 두 설계가 동시에 살아있으면 안 된다.
- 상호작용 점검 필수: 경직 내성창(`IsStaggerImmune`)이 Light/Hit을 이미 억제하므로, 플린치까지 억제할지 여부를 별도로 정한다(권장: 플린치는 통과 — 피격 사실 자체는 보여야 한다).
- `MotionWarp.ClearTarget()`은 전신 경직에만 적용하고 가산 플린치에서는 유지한다.

**3-2. 8방향 + 높이 리액션 (G6)**
- 3-0의 결정에 따라 `GetHitAnimKey`를 4→8방향으로 확장, `hitPoint.y`로 상/중/하 변형 선택. 모션 미보유 시 4방향으로 폴백.

**3-3. 재질별 임팩트 (G4/G5 잔여)**
- 피격자에 재질 태그 축을 추가하고 FX/SFX 키를 `{reaction}_{material}` 조합으로 해석.

---

## 4. 보존해야 할 제약

- **Break 노출 중 피격**: 몬스터는 노출 중 일반 리액션 전환을 건너뛴다. Step 1/2의 FX·SFX 강화는 이 분기에서도 울리되 **상태 전환을 되살리지 않는다.**
- **패리 경직**: 패리는 `AttackReactionType.Light` 경로를 재사용한다. Light 연출을 바꾸면 패리 체감도 같이 바뀐다 — 튜닝 시 함께 확인.
- **모션 후딜 기반 히트스톱**: 생성기가 산출한 `hitStopDuration/Scale`은 단일 소스다. 런타임에서 대입하지 말고 배율만 곱한다.
- **다인 전투 가드**: `IsActorHitStopping` early-return과 `IsStaggerImmune` 억제 로직을 신규 경로가 우회하면 조작 불가 누수가 재발한다.
- **데미지 면역 ≠ 리액션 면역**: 경직만 억제하고 피해는 유지하는 기존 원칙을 지킨다.

---

## 5. 검증 방법

- Step 1: 적 평타/강공격을 맞아보며 충돌음 유무, FX가 타격 지점에서 방향 맞게 나오는지 육안 확인.
- Step 2: 단일 적 → 3인 이상 포위 순으로 비교. **다인에서 조작 응답성이 Step 1 대비 나빠지면 즉시 2-2를 강공격 한정으로 되돌린다.**
- Step 3: 이동 중 Light 피격 시 이동이 끊기지 않는지, Heavy 피격은 여전히 전신 경직인지 확인.
- 각 Step은 **단독으로 넣고 체감을 재평가**한다. 한 번에 넣으면 원인 분리가 불가능하다.

---

## 6. 이번 개정에서 보류한 항목

| 항목 | 보류 사유 |
|------|----------|
| ~~G8 환경 상호작용 넉백~~ | **보류 해제 — §8에서 별도 설계** |
| G9 라그돌 | 사망 연출은 디졸브로 이미 성립. 물리 전환은 의상/MagicaCloth2와 충돌 검토 선행 필요 |
| G10 에어 저글 | 공중 콤보 추격 자체가 미도입 상태(전투 로드맵에서도 보류) |
| G11 부위 크리티컬 | 히트박스 per-bone 태깅 = 전 몬스터 데이터 재작업. 3-3 재질 축과 함께 재검토 |

---

## 7. 영향 파일

| 파일 | Step | 변경 성격 |
|------|------|----------|
| `GameActor/Combat/Feedback/CombatFeedbackDispatcher.cs` | 1,2 | SFX 티어 분기, FX 회전 오버로드, 히트스톱 배율 후처리 |
| `GameActor/Object/Player/PlayerActor.Combat.cs` | 1 | 피격 임팩트 사운드 호출, FX 위치 폴백 순서 |
| `GameActor/Object/Monster/MonsterActor.cs` | 1,3 | 피격측 FX/SFX(노출 분기 보존), 리액션 축 경계 |
| `GameActor/Component/Enemy/EnemyCombat.cs` | 2 | 공격자측 히트스톱/카메라 훅 신설 |
| `Manager/Handler/Combat/GameHitStopHandler.cs` | 2 | (가급적 무변경) 기존 API 재사용 |
| `GameActor/Animation/ActorAnimator.cs` | 3 | 가산 플린치 오버레이 재사용 |
| `GameActor/State/Player/PlayerHitState.cs`, `State/Enemy/EnemyHitState.cs` | 3 | 가산 플린치 분리, 8방향 확장 |
| 사운드 데이터(`GameSoundKey` + 엔트리) | 1 | 임팩트 키 티어 추가 (코드 변경 최소) |
| `GameActor/State/Enemy/EnemyHitState.cs` · 신규 `EnemyWallSplatState.cs` | 8 | 벽 충돌 승격 |
| `GameActor/Combat/Resolution/ReactionResolver.cs` | 9 | 리액션 선택 축 확장 |

---

## 8. 환경 넉백 (Wall Splat) — G8 상세 설계

### 8-1. 조사 요약

| 출처 | 시사점 |
|------|--------|
| Tekken(Wall Splat / Wall Bounce / T8 Wall Blast) | 넉백이 벽에 막히면 **별도 상태로 승격**하고 추가타 창을 준 뒤 slump→기상으로 빠져나온다. 즉 "벽에 부딪힘"은 물리 반응이 아니라 **리액션 상태 하나**다. Wall Bounce(벽 반사)는 저글 시동기 성격의 별도 티어 |
| Combat Recall 리액션 프레임워크 | 리액션 선택 축에 impact direction·body location과 함께 **현재 상태(grounded / airborne / wall-pinned)**가 명시됨. wall-pinned는 표준 축의 하나 |
| 액션 RPG 전투 설계 통론 | 적 넉백은 남용하면 "쫓아다니기"가 되어 오히려 흐름을 끊는다 → **선택적 적용**이 원칙 |

결론: 벽 반응을 물리 시뮬레이션(반사·바운스)으로 접근하지 않는다. **"넉백 소멸 + 상태 승격"**으로 접근한다.

### 8-2. 이 프로젝트에서 유리한 점

신규 물리 시스템이 필요 없다. KCC 콜백이 이미 상태로 위임되고 있고, **같은 패턴을 쓰는 선례가 이미 있다.**

- `ActorMovementController.OnMovementHit`(:529) → `_currentState.OnMovementHit` 위임
- `EnemyChargeState.OnMovementHit`(:184)이 이미 "벽에 충돌하면 돌진 실패 → 추격으로" 처리 중
- `EnemyHitState`는 생성자에서 `HitContext` 전체를 보관(:21,32) → 넉백 세기·방향·reactionType을 그대로 알고 있다
- 넉백 채널이 분리되어 있어 잔여 속도 판정이 가능(`AddPlanarKnockback`, `_impulseDampers`, `IsImpulseActive`)

`EnemyHitState`/`EnemyAirborneState`는 현재 `OnMovementHit`을 **오버라이드하지 않는다** → 여기가 정확한 삽입 지점이다.

### 8-3. 발동 게이트 (오발동 방지가 이 기능의 8할)

아래를 **모두** 만족할 때만 벽 반응을 발동한다.

1. **리액션 중**: 현재 상태가 Hit/Airborne/Knockdown 계열이고 넉백 임펄스가 살아 있을 것(`IsImpulseActive`). 평상시 이동 충돌은 대상 아님.
2. **넉백 종류**: `HitContext.ReactionType`이 KnockBack / Airborne / Knockdown일 것. 평타 Light/Hit는 제외 — 모든 타격이 벽 연출을 유발하면 값이 없어진다.
3. **잔여 속도 임계**: 충돌 시점 수평 속도가 임계 이상. 다 죽어가는 넉백이 벽에 닿는 건 무시.
4. **진짜 벽인가**: `hitNormal`과 캐릭터 Up의 내적이 0에 가깝고(수직면), `HitStabilityReport.IsStable == false`. 경사면·계단·작은 턱을 벽으로 오판하지 않게 한다.
5. **대상 필터**: 충돌 상대가 다른 액터면 제외. 적끼리 밀치다 벽 연출이 터지면 안 된다.
6. **1회 소비**: `OnMovementHit`은 한 넉백 동안 여러 스텝에서 반복 호출될 수 있다. **소비 플래그 필수** — 이걸 빠뜨리면 벽에 비비는 동안 연출이 연타된다.

### 8-4. 3티어 결과 (데이터로 선택)

| 티어 | 결과 | 신규 모션 | 적용 대상 |
|------|------|----------|----------|
| **T0 — 임팩트만** (기본) | 잔여 넉백 즉시 소멸 + 벽면 FX/SFX + 리액션 시간 소폭 연장 | 0개 | 전 액터 기본값 |
| **T1 — 월 스플랫** | `EnemyWallSplatState`로 승격. 벽에 눌린 짧은 crumple + **플레이어 추가타 창**, 만료 시 slump → Knockdown 또는 Idle | 1~2개 | 일반/정예 몬스터 |
| **T2 — 월 바운스** | 벽 법선으로 감쇠 반사 임펄스 → 플레이어 쪽으로 되튐 | 1개 + 저글 연계 | **보류 권장** |

- **T0만으로도 목적의 대부분이 달성된다.** "벽에 처박혔다"는 정보 전달 + 넉백이 벽을 긁으며 미끄러지는 현재의 어색함 제거. 먼저 T0만 넣고 체감을 본다.
- **T2는 보류를 권장한다.** 되튐은 저글 시동기이고 이 프로젝트에 공중 콤보 추격이 없다(G10 보류). 저글 없이 되튐만 넣으면 적이 고무공처럼 보인다. 도입한다면 보스 특정 스킬 한정.
- 티어 선택은 `CombatReactionPolicySO`(등급별 리액션 허용)를 확장해 담는다. 보스는 T0 고정 — 보스가 벽에 눌려 늘어지면 위압이 무너진다.

### 8-5. 잔여 속도 처리 (놓치기 쉬운 지점)

벽 반응 시 `_pendingPlanarKnockbackVelocity`만 지우면 부족하다. `_impulseDampers`에 남은 감쇠 modifier도 함께 제거해야 한다(`ClearExternalVelocityChanges` 또는 해당 채널만 선택 제거). 안 그러면 속도는 0인데 damper가 살아 있어 이후 이동이 눌린다.

### 8-6. 플레이어 피격 시

플레이어에게는 **T0만** 적용한다. 추가 경직·추가 피해를 넣지 않는다 — 다인 전투 조작 불가 누수(경직 내성창으로 막아둔 문제)가 벽 근처에서 되살아난다. 플레이어 쪽은 "벽에 부딪히면 넉백이 멈추고 임팩트가 난다"까지가 상한.

### 8-7. 비도입

낭떠러지 낙하·환경 오브젝트 파괴·벽 파괴(Wall Blast 류)는 레벨 저작 규약이 선행돼야 하므로 이번 범위 밖이다.

---

## 9. 행동 기반 리액션 매칭 — "자연스러운 반응"의 실체

### 9-1. 조사 요약

- 리액션 선택 축은 **공격 속성만이 아니다.** 표준 축은 ① 타격 방향 ② 타격 부위 ③ **피격자의 현재 상태**(지상/공중/가드/벽에 눌림) ④ 적 타입·성격이다.
- 리액션 어휘의 업계 표준: Normal(10~25프레임) / Stagger·Stumble(2~3배 길이) / Flyback / Launch / Crumple(스턴 루프) / Knockdown. **여기에 수정자**로 Tank(피해만 받고 리액션 없음) / Armor / Invulnerable / GuardBreak / Frozen.
- 균형점: "리액션이 적으면 무감각, 많으면 적이 인형처럼 조종당해 보인다. 강한 시스템은 리액션을 **선택적으로** 적용한다."
- 애니 품질 규칙: 블렌드 시간을 짧게, 리액션 첫 프레임 포즈를 직전 공격 포즈와 극단적으로 다르게, baked translation을 어느 정도 쓰되 프로그램 임펄스에만 의존하지 말 것.
- AI 연동: 스태거 플래그를 AI가 읽어 행동을 막고, 상태 마킹을 애니메이션 타임라인에 심어 보이는 것과 로직을 일치시킨다.

### 9-2. 현재 프로젝트 판정

**리액션 어휘는 이미 완비되어 있다.** 표준 어휘 대부분이 1:1로 존재한다.

| 업계 표준 | 이 프로젝트 |
|-----------|------------|
| Normal | `Light` / `Hit` |
| Stagger·Stumble | `Heavy` |
| Flyback | `KnockBack` |
| Launch | `Airborne` |
| Crumple(스턴 루프) | `Stun` + Break 노출 |
| Knockdown | `Knockdown` |
| Tank / Armor 수정자 | `CombatReactionPolicySO` 등급 정책 + `PoiseStat` 하이퍼아머 |
| AI 스태거 연동 | `BlocksBehaviorTree`, `EnemyTacticalMemory.NotifyTookDamage` |

**진짜 갭은 어휘가 아니라 선택 축이다.** `MonsterReactionQuery`(`ReactionResolver.cs:29`)가 보는 것은 poiseBroken / CanPlayHitReaction / Airborne / Knockdown / Grade / Policy뿐이다. **피격자가 그때 무엇을 하고 있었는지는 보지 않는다.** 그래서 이동 중에 맞든, 공격을 휘두르다 맞든, 가드 중에 맞든 같은 `Hit_F`가 나온다 — 이것이 "부자연스럽다"의 정체다.

### 9-3. 제안 — 선택 축에 "피격자 행동"을 추가

`MonsterReactionQuery`/`PlayerReactionQuery`에 피격 당시 행동 컨텍스트를 넣고, 모션 해석에서 **접미사 폴백**으로 소화한다.

| 피격 당시 행동 | 자연스러운 반응 | 해석 키 예 |
|---------------|----------------|-----------|
| 공격 모션 중 | 휘두르던 팔이 무너지는 중단형 리액션 | `Hit_F.Attacking` |
| 이동 중 | 발이 꼬이거나 스텝 백 | `Hit_F.Moving` |
| 가드 중 | 가드 자세가 흔들림(무너지진 않음) | `Hit_F.Guarding` |
| 공중 | 공중 전용 리액션 | `Hit_F.Air` |
| 벽에 눌림 | §8 T1 월 스플랫 | — |
| 그 외 | 기본 | `Hit_F` |

**핵심은 폴백이다.** `HasMotion(키) ? 키 : 기본키` — 기존 `GetHitAnimKey`가 이미 쓰는 패턴 그대로다(`PlayerHitState.cs:241~247`, `EnemyStunState.cs:57`). 따라서 **모션을 만든 액터만 자연스러워지고, 안 만든 액터는 지금과 완전히 동일하게 동작한다.** 전면 재작업 없이 점진 도입이 가능하다.

### 9-4. 저작 축 결정에 종속

이 확장은 §3-0(State 경로 vs GAS 태그 트리거 경로)의 결정 이후에 착수한다. 방향·행동 같은 *기계적 변형*은 모션 해석 축(State)에, *연출적 특수 리액션*은 GAS 축에 두는 것이 권장안이다. 결정 전에 손대면 같은 변형을 두 축에 중복 저작하게 된다.

### 9-5. 절제 규칙

조사에서 반복 강조된 지점이자, 이 항목에서 가장 실패하기 쉬운 부분이다.

- 모든 조합에 모션을 만들지 않는다. **가장 자주 보이는 조합부터** — 이동 중 피격 > 공격 중 피격 > 가드 중 피격 순.
- 리액션이 길어지면 적이 인형이 된다. 행동별 변형은 **길이를 바꾸는 게 아니라 포즈를 바꾸는 것**이다. 경직 시간의 소유권은 기존 `reactionDuration`/Poise 판정에 그대로 둔다.
- 블렌드 시간은 짧게 유지한다(현재 0.15~0.2s). 변형이 늘어난다고 블렌드를 늘리면 전체가 물러진다.

### 9-6. 범위 밖

물리 블렌드(부분 라그돌) 기반 리액션은 자연스러움에 크게 기여하지만 MagicaCloth2 의상 시뮬레이션과의 충돌 검토가 선행돼야 한다 → G9와 함께 보류를 유지한다. 사기·목격 반응 같은 AI 층위의 "반응"은 리액션 시스템이 아니라 BT 설계 영역이므로 본 문서 범위 밖이다.

