# 스킬 시스템 고도화 설계서

> 작성일: 2026-07-25
> 대상 버전: Unity 6 (6000.0.60f1), URP, 싱글플레이
> 상태: 설계 (미구현)
> 선행 문서: `Assets/docs/Complete/GAMEPLAY_ABILITY_SYSTEM_SPEC.md` (V1 구조 계약), `Assets/docs/design/PLAYER_SKILL_SYSTEM_REDESIGN_PLAN.md` (레거시 선행 설계)
> 연관 문서: `Assets/docs/TODO/PROJECTILE_SYSTEM_ADVANCEMENT_SPEC.md`, `Assets/docs/Complete/PASSIVE_ABILITY_SYSTEM_SPEC.md`

---

## 1. 개요

Gameplay Ability 시스템 V1은 이미 구현·마이그레이션이 끝났다(AbilitySet 34개 / Ability 482개 / Variant·Payload 493개). `GameplayAbilitySO`가 활성화 규칙·비용·쿨다운·Variant·Effect·저장 정책을 소유하고, `ActorAbilitySystem`이 Prepare/Commit/End 트랜잭션을 담당하며, 플레이어와 몬스터가 같은 구조를 공유한다.

따라서 이 문서는 **새 프레임워크 제안이 아니라, V1 이후 남은 구조적 간극을 메우는 설계**다. 코드 감사 결과 간극은 크게 네 부류다.

1. **권위 이중화** — 실행 권위가 GAS로 옮겨졌는데 게이팅·쿨다운·비용 권위 일부가 레거시 컴포넌트에 남아 있다.
2. **표현력 부족** — Task 라이브러리가 3종뿐이라 다단 스킬(홀드/차지/채널/재사용/조준)을 데이터만으로 저작할 수 없다.
3. **자원 모델 미완** — `Forte`/`Concerto`/`SkillCharge`가 enum에만 있고 런타임이 없다. 쿨다운에 차지 개념이 없다.
4. **동시성 제약** — 활성 실행이 1개로 고정되어 지속형·토글형 스킬을 표현할 수 없고, 정책 enum과 실제 동작이 어긋난다.

### 목표

- 스킬 사용 가능 여부·비용·쿨다운의 **단일 권위를 `ActorAbilitySystem`으로 통일**한다.
- 다단 스킬을 코드 추가 없이 **Task 그래프로 저작**한다.
- 캐릭터 고유 자원과 차지형 쿨다운을 **데이터로 표현**한다.
- 지속형/토글형 Ability를 안전하게 **동시 실행**한다.
- 스킬 조준·대상 예약을 **투사체 시스템의 타게팅과 같은 계약**으로 통일한다.

### 비목표

- 언리얼 GAS 기능 전수 복제(예측·복제·`GameplayCue` 전체 계층).
- 일반 공격·이동 상태의 Ability화. V1 스펙의 비범위를 그대로 유지한다.
- 런타임에서 SO 값을 수정·저장하는 기능.
- MotionSet 타임라인을 대체하는 범용 비주얼 스크립팅.

---

## 2. 현재 구조 감사

### 2.1 실행 경로

```
입력 (PlayerMovementController.HasSkillInput → InputCondition.Pressed)
└── PlayerAttackState.GetAnimKey()
        ├── [A] Abilities.HasPlayerAbility(slot) == true
        │       → TryPreparePlayerSlot → ExecuteAbilityAttack → Commit
        │           (비용·쿨다운·태그·Effect = GameplayAbilitySO)
        └── [B] 그 외
                → PlayerAbilityResourceView.CanUseSkill → ExecuteSkillAttack → ConsumeSkill
                    (비용·쿨다운 = 컴포넌트 인스펙터 배열)

몬스터: EnemyCombat.CollectCandidates → EvaluateAbility → TryActivateAbility (GAS 경로 단일)
```

### 2.2 확인된 간극

| # | 문제 | 근거 |
|---|------|------|
| **S1** | **쿨다운 그룹 ID 분기.** `ActorAbilitySystem.StartCooldown`은 `cooldown.ResolveGroupId(abilityId)`를, `PlayerAbilityResourceView`는 `"Ability.SkillSlot.{n}"`을 키로 같은 `AbilityCooldownRuntime`에 기록한다. **두 키가 절대 만나지 않는다.** HUD와 상태 게이트는 슬롯 키를, Ability 평가는 abilityId 키를 읽으므로, GAS 경로로 발동한 스킬은 HUD 쿨다운이 돌지 않고 상태 게이트도 통과한다. | `ActorAbilitySystem.cs:602-611`, `PlayerAbilityResourceView.cs:290-294` |
| **S2** | **비용·쿨타임 수치 이중 소스.** `_skillCost = {0, 100}`, `_skillCooldown = {3, 12}`가 컴포넌트 인스펙터에 남아 있다. 이 값은 `GetEffectiveCooldownDuration`(패시브 CDR)을 타지 않는다. CLAUDE.md의 "AbilitySetSO 단일 소스"와 V1 완료 조건("UI가 비용과 사용 가능 여부를 독자 계산하지 않는다")에 모두 어긋난다. | `PlayerAbilityResourceView.cs:59-64, 232-244` |
| **S3** | **상태 게이트가 레거시 권위를 사용.** `PlayerIdleState`, `PlayerGroundMoveState`, `PlayerHitState`, `PlayerDrinkState`, `PlayerInterruptResolver`가 전부 `skillGauge.CanUseSkill(i)`로 진입을 판정한다. GAS Ability가 정의된 슬롯에서도 **레거시 게이지·쿨다운 규칙이 진입을 막거나 허용**한다(예: Ultimate는 레거시 규칙상 게이지 만충 필수, GAS는 `AbilityCostPolicy`에 따름). | `PlayerIdleState.cs:129`, `PlayerAttackState.cs:245, 720`, `PlayerHitState.cs:170`, `PlayerGroundMoveState.cs:163` |
| **S4** | **Task 라이브러리가 3종.** `SequenceAbilityTaskDefinitionSO`, `ParallelAbilityTaskDefinitionSO`, `WaitMotionSetEndAbilityTask`가 전부다. 지연·태그 대기·입력 릴리즈·이벤트 대기·조건 분기·루프가 없어 **다단 스킬은 저작이 아니라 코드 작성**이 된다. `taskGraph`는 없으면 `MissingExecutionData`로 활성화가 막히는 필수 필드라 모든 Ability가 사실상 "모션 끝까지 대기" 한 종류만 쓴다. | `AbilityTaskRuntime.cs:164-181`, `WaitMotionSetEndAbilityTask.cs`, `ActorAbilitySystem.cs:471` |
| **S5** | **스킬 입력이 Pressed 단발.** `HasSkillInput(i)`가 `InputCondition.Pressed`만 본다. 홀드 강화, 조준 유지, 릴리즈 발동, 재사용(recast) 창, 더블탭 변형을 입력 계층에서 표현할 수 없다. 차지 공격은 `AbilitySetSO.charge`라는 **별도 경로**로만 존재해 스킬 슬롯과 규칙이 다르다. | `PlayerMovementController.cs:257-263`, `AbilitySetSO.cs:21` |
| **S6** | **동시 실행 1개 고정.** `_activeExecution`이 단일 핸들이다. `AbilityConcurrencyPolicy.Allow`로 두 번째 Ability가 Commit되면 `_activeExecution`이 덮어써지고 **이전 실행이 고아가 된다.** `EndActiveAbility`는 최신 실행만 정리하므로 고아 실행의 `GrantedTagHandles`와 Task가 남는다(태그·Effect 누수 경로). 지속형 오라, 소환물 유지, 토글 버프를 표현할 수 없다. | `ActorAbilitySystem.cs:161-180, 218-230, 255-282` |
| **S7** | **자원 종류 미구현.** `AbilityResourceType`에 `Forte`, `Concerto`, `SkillCharge`, `Health`가 있으나, 코드 검색상 실제 축적·소비 경로는 `UltimateEnergy` 하나뿐이다. 캐릭터 고유 자원(Forte)은 Variant 조건(`minResource`, `requiresFullResource`)의 핵심 입력인데 비어 있어, **Variant 분기는 사실상 지상/공중과 태그로만 갈린다.** | `AbilityDefinitions.cs:27-35`, `PlayerAbilityResourceView.cs:40-46` |
| **S8** | **쿨다운 모델이 단일 타이머.** 차지(스택) 개념이 없어 "2스택 대시" 류를 표현할 수 없다. 글로벌 쿨다운·최소 재사용 간격도 없다. CDR은 `duration * multiplier` 곱연산이라 **배율이 0에 수렴하면 쿨다운이 0**이 되고, 패시브가 누적될수록 체감 이득이 커지는 역누진이 생긴다. | `AbilityCooldownRuntime.cs:21-46`, `ActorAbilitySystem.cs:654-665` |
| **S9** | **조준·대상 예약 계약 부재.** `AbilityActivationRules`에 `targetPolicy`/`targetRelation`/`min·maxDistance`는 있지만, **지면 지정·조준 유지·후보 표시·예약 위치**를 표현하는 계약이 없다. 실제 타깃 해석은 투사체 스폰 시점의 `MotionEvent_SpawnProjectile.ProjectileTargetMode`가 별도로 수행하므로, Ability의 사거리 판정과 투사체 착탄 대상이 서로 다른 규칙을 쓴다. | `AbilityDefinitions.cs:163-173`, `MotionEvent_SpawnProjectile.cs:139-184` |
| **S10** | **Effect 표현력 한계.** `GameplayEffectSO`는 modifier + grantedTag + 스택 + 주기까지다. 조건부 발동, 확률, 대상 필터, 해제(dispel)/면역, Effect가 Ability를 부여하는 구조가 없다. V1 스펙도 반사·오라·전이를 이후로 미뤘다. | `GameplayEffectSO.cs:11-34` |
| **S11** | **탐색 비용.** `AbilitySetSO.EnumerateAll()`이 `yield return` 이터레이터이고, `Contains`가 이를 선형 순회한다. `IsGrantedAbility`는 활성화마다, `EnemyCombat.CollectCandidates`는 **BT 결정 틱마다** 전체 집합을 순회한다. 보스 AbilitySet 규모가 커질수록 GC와 CPU가 함께 는다. | `AbilitySetSO.cs:57-92`, `EnemyCombat.cs:325` |

> 이미 구현되어 있으므로 재제안하지 않는 항목: 콤보 라우트(`comboRoutes`), 차지 공격 스테이지(`charge.stages`), 패시브 시스템과 CDR 배율, 성장 기반 스킬 해금(`IsSkillUnlocked`), 스왑 잔류 공격, 쿨다운 저장·복원, GAS 런타임 디버거 윈도우, Ability 생산 위저드/검증기.

---

## 3. 레퍼런스 조사

| 출처 | 채택할 아이디어 | 반영 |
|------|-----------------|------|
| Unreal GAS – AbilityTask 라이브러리 (`WaitDelay`, `WaitGameplayEvent`, `WaitInputPress/Release`, `WaitGameplayTagAdd/Remove`, `WaitAttributeChangeRatioThreshold`, `PlayMontageAndWaitForEvent`) | Ability의 다단 진행을 **표준 Task 조합**으로 표현한다. 특히 몽타주 재생과 이벤트 대기를 합친 Task가 콤보·차지 계열의 핵심. | §4.2 표준 Task 세트 |
| Unreal ARPG 샘플 – 멜리 Ability | 애님 노티파이가 Ability로 이벤트를 되돌려보내 타이밍을 제어. 본 프로젝트의 MotionEvent가 같은 역할을 할 수 있다. | §4.2 `WaitMotionEvent` |
| League of Legends – Ability Haste | 퍼센트 CDR은 100%에 접근할수록 이득이 발산하고 구간별 체감이 불균등하다. **haste = 선형 누적, 실효 쿨다운 = base × 100/(100+haste)** 로 상한 없이 안정. | §4.3 쿨다운 모델 |
| LoL / 일반 ARPG – 차지(스택) 쿨다운 | 스택별 재충전 시간을 base로 삼고, 스택 소비와 재충전을 분리. | §4.3 차지 |
| Guild Wars 2 / FFXIV – 조준 모드 | **지면 지정 / 대상 지정 / 액션캠 조준(크로스헤어)** 을 스킬 속성으로 분류하고, 조준 중 예약 위치를 UI로 표시한 뒤 확정. | §4.5 조준 계약 |
| 본 프로젝트 투사체 설계서 | `ProjectileTargetMode`와 타깃 예약을 이미 정의. 스킬 조준 계약은 이것과 **같은 해석기를 공유**해야 한다. | §4.5 |

---

## 4. 설계

### 4.1 권위 일원화 (S1/S2/S3)

**규칙: 스킬의 사용 가능 여부·비용·쿨다운은 `ActorAbilitySystem`만 판정한다.**

- `PlayerAbilityResourceView`를 **읽기 전용 뷰**로 축소한다.
  - 삭제: `_skillCost`, `_skillCooldown`, `StartCooldown`, `ConsumeSkill`, `CanUseSkill`의 자체 판정.
  - 유지: 히트 기반 게이지 충전(`ChargeTable`), 게이지 변경 이벤트, HUD가 읽는 스냅샷.
  - 쿨다운 조회는 슬롯 → Ability → `cooldown.ResolveGroupId(abilityId)` 해석 후 위임한다. **`"Ability.SkillSlot.{n}"` 키는 제거한다.**
- 상태 게이트는 `CanUseSkill(i)` 대신 `Abilities.EvaluatePlayerSlot(slot, grounded, target)`의 결과를 사용한다. 실패 사유(`AbilityActivationResult`)를 그대로 HUD/로그로 흘려보낸다.
- 레거시 `ExecuteSkillAttack` + `ConsumeSkill` 경로는 **모든 캐릭터 슬롯이 Ability로 채워졌음을 검증기로 확인한 뒤 제거**한다. 제거 전까지는 "Ability 정의가 있으면 레거시 경로 진입 금지"를 단일 분기점(`PlayerAttackState`)에서만 판단한다.

저장/복원(`SetCooldownRemainingSnapshot`)도 같은 그룹 ID 규칙을 쓰도록 이관한다. 이 변경은 **기존 세이브의 쿨다운 키를 무효화**하므로 마이그레이션 규칙이 필요하다(§7).

### 4.2 Task 라이브러리 확장 (S4/S5/S6 일부)

`AbilityTaskDefinitionSO` 파생으로 표준 세트를 추가한다. 모두 `UPlayGround.Ability.Core`(프로젝트 비의존)에 두되, MotionSet·투사체처럼 프로젝트 타입을 참조하는 Task는 `Ability.UPlayGround` 어댑터에 둔다.

| Task | 소속 | 용도 |
|------|------|------|
| `WaitDelay` | Core | 초 단위 대기. Ability 종료 시 자동 취소 |
| `WaitTagAdded` / `WaitTagRemoved` | Core | 태그 기반 동기화 |
| `WaitAttributeThreshold` | Core | HP 비율 등 조건 도달 대기 |
| `WaitGameplayEvent` | Core | 태그 이벤트 수신 (피격 확정, 패리 성공 등) |
| `WaitInputRelease` / `WaitInputRepress` | Core (포트 경유) | **홀드 차지 / 재사용(recast) 창** |
| `SelectBranch` | Core | 조건별 하위 Task 분기 |
| `Loop` | Core | 횟수·시간·태그 조건 반복 (채널링) |
| `ApplyEffect` / `RemoveEffect` | Core | 진행 중 Effect 부착·해제 |
| `WaitMotionSetEnd` | 어댑터 | 기존 |
| `WaitMotionEvent` | 어댑터 | MotionSet 타임라인 이벤트를 Task 신호로 수신 |
| `SpawnProjectileTask` | 어댑터 | 투사체 설계서의 `IProjectileService.Spawn` 호출. MotionEvent 저작과 병행 가능 |

**설계 원칙**

- Task는 **판정을 하지 않는다.** 비용·쿨다운·태그 검사는 Prepare/Commit에서 끝난다. Task는 실행 중 진행과 취소만 다룬다.
- 입력 대기 Task는 `IAbilityInputPort`(Core 포트)로 추상화한다. Core가 `PlayerMovementController`를 알지 않도록 한다.
- `WaitInputRelease`가 성립하려면 입력 계층에 **슬롯별 Held/Released 상태**가 필요하다(S5). `HasSkillInput(int)`를 `GetSkillInput(int) → InputCondition`으로 확장하고, `InputBuffer`가 스킬 슬롯 선입력도 보관하도록 한다.
- 홀드 차지 스킬은 `AbilitySetSO.charge`(일반 공격 차지)와 **규칙을 공유하되 경로는 분리**한다. 스킬 홀드는 Task 그래프가, 일반 공격 차지는 기존 스테이지가 담당한다. 두 경로를 합치는 것은 별도 과제다.

### 4.3 자원과 쿨다운 모델 (S7/S8)

**차지(스택) 쿨다운**

```csharp
[Serializable] public sealed class AbilityCooldownDefinition
{
    public float durationSeconds;      // 스택 1개 재충전 시간
    public string cooldownGroupId;
    public int maxCharges = 1;         // 신규
    public float globalLockSeconds;    // 신규: 사용 직후 공용 최소 간격
}
```

`AbilityCooldownRuntime`을 "종료 시각 1개"에서 **"보유 스택 + 다음 스택 완성 시각"** 으로 확장한다. 저장은 현행 정책대로 남은 초 + 보유 스택 수를 기록한다.

**Haste 기반 CDR**

현행 곱연산 배율(`duration * multiplier`)을 유지하면 패시브가 쌓일수록 발산한다. 실효 쿨다운을 다음으로 정의한다.

```
effective = base * 100 / (100 + haste)
```

`haste`는 패시브·장비·Effect가 **가산**한다. 상한 없이 안정적으로 수렴하고, 기존 퍼센트 CDR 값은 `haste = 100 * cdr / (1 - cdr)`로 1회 변환한다. 슬롯별 배율 API(`GetActiveSkillCooldownMultiplier`)는 haste 반환으로 교체한다.

**자원 런타임**

`Forte` / `Concerto` / `SkillCharge`를 실제 Attribute로 등록하고, 축적 규칙을 데이터로 정의한다.

| 자원 | 축적 | 소비 | 비고 |
|------|------|------|------|
| `UltimateEnergy` | 히트 종류별(`ChargeTable`) | Ultimate | 기존 유지 |
| `Forte` | 캐릭터별 규칙(특정 태그 공격 히트, 스킬 사용, 시간) | Variant 조건 / 강화 스킬 | **캐릭터 고유 자원. Variant 분기의 주 입력** |
| `Concerto` | 스킬·궁극기 사용, 특정 Effect 만료 | 캐릭터 교체 특수(잔류 공격) | 파티/교체 시스템 소유 |
| `SkillCharge` | 차지 쿨다운과 통합 | — | 별도 자원으로 두지 않고 §4.3 차지로 흡수하고 enum에서 제거 검토 |

축적 규칙은 코드가 아니라 `AbilityResourceRuleSO`(신규, 캐릭터별)로 저작한다. HUD는 `IAbilityRuntimeReader`를 통해 자원 종류와 최대치를 읽는다.

### 4.4 동시 실행 (S6)

`_activeExecution` 단일 필드를 **활성 실행 목록**으로 교체하고, 다음을 분리한다.

| 개념 | 의미 |
|------|------|
| **주 실행(Primary)** | 액터의 상태 머신을 점유하는 실행. 항상 최대 1개. 상태 전환·모션·입력 차단의 주체 |
| **부 실행(Background)** | 상태를 점유하지 않는 지속형(오라, 토글, 소환 유지, 지속 버프). 다수 허용 |

- `AbilityConcurrencyPolicy`에 `Background`를 추가한다. `Allow`는 **정의만 있고 안전하지 않으므로 제거하거나 `Background`로 대체**한다.
- `EndActiveAbility`는 주 실행만 종료한다. 부 실행은 자체 Task 완료, 토글 재입력, 태그 제거, 사망·교체·씬 전환으로 종료한다.
- 사망/교체/씬 전환 시 **모든 실행을 순회 종료**하고 `GrantedTagHandles`를 회수한다(현행 누수 경로 차단).
- 부 실행은 `AbilitySwapPolicy`/`GameplayEffectRemovalPolicy`와 같은 축(`CancelOnSwap` / `PersistPerCharacter` / `PersistOnPlayerActor`)을 따른다.

### 4.5 조준과 대상 예약 (S9)

`AbilityActivationRules`와 별개로 **조준 계약**을 추가한다.

```csharp
public enum AbilityTargetingMode
{
    None,           // 시전자 기준 즉발
    AutoTarget,     // 락온/최근접 자동 선택 (현행 기본)
    GroundIndicator,// 지면 원/사각 지정 후 확정
    Aimed,          // 조준 유지 중 방향 갱신, 릴리즈 시 확정
}
```

- 조준 결과는 `AbilityTargetReservation`(위치 + 대상 + 확정 프레임)으로 **Prepare 시점에 예약**되고, 실행 중 Task와 MotionEvent가 같은 예약을 읽는다.
- **투사체 설계서의 `ProjectileTargetMode`/`ProjectileSpawnRequest`와 해석기를 공유**한다. Ability의 사거리 판정과 투사체 착탄 대상이 어긋나는 현행 문제(S9)를 여기서 없앤다.
- `GroundIndicator`/`Aimed`는 조준 중 인디케이터 UI가 필요하다. 몬스터 텔레그래프(`DangerRing`)와 표현은 분리하되 위치 계약은 같은 예약 구조를 쓴다.
- 조준 취소 시 비용·쿨다운을 소비하지 않는다(Prepare 상태에서 `Abort`).

### 4.6 Effect 표현력 (S10)

V1 범위를 넘는 확장은 **실제 필요한 콘텐츠가 생겼을 때** 도입한다. 우선순위 순.

1. `applicationChance` + 조건 태그 쿼리 — 확률 상태이상.
2. `immunityTags` / `dispelTags` — 해제·면역. 태그 런타임이 이미 참조 카운트 기반이므로 저비용.
3. `grantedAbilities` — Effect가 한시적으로 Ability를 부여(변신, 무기 각성).
4. 오라(주기적 범위 재적용)와 전이는 마지막. 대상 탐색 비용이 크고 판정 규칙이 별도 설계를 요구한다.

### 4.7 진단과 성능 (S11)

- `AbilitySetSO`에 **런타임 인덱스**(`Dictionary<GameplayAbilitySO, slot>` + AI 후보 리스트 사전 분류)를 `OnEnable`에 1회 구축한다. `Contains`/`IsGrantedAbility`/`CollectCandidates`의 선형 순회와 이터레이터 할당을 제거한다.
- `EnemyCombat.CollectCandidates`는 카테고리·공중 여부로 **미리 분할된 리스트**를 조회하도록 바꾼다.
- 활성화 실패 사유별 카운터(`AbilityActivationResult`)를 텔레메트리에 집계한다. "왜 스킬이 안 나가는가"는 현재 `Debug.Log`로만 남는다.
- 스킬별 사용 횟수·명중률·기여 피해를 기존 밸런스 툴 지표에 추가한다.

---

## 5. 단계 계획

### Phase 0 — 권위 일원화 (S1/S2/S3)

가장 먼저 한다. 이 단계 없이 다른 기능을 얹으면 이중 권위 위에 쌓게 된다.

- 쿨다운 그룹 ID 통일, `PlayerAbilityResourceView` 축소, 상태 게이트를 `EvaluatePlayerSlot`으로 교체.
- 세이브 쿨다운 키 마이그레이션.
- 검증: 슬롯별 HUD 쿨다운과 실제 재사용 가능 시점이 일치. 패시브 CDR이 모든 경로에 반영.

### Phase 1 — 자원·쿨다운 모델 (S7/S8)

- 차지 쿨다운, haste 전환, `Forte`/`Concerto` 런타임과 `AbilityResourceRuleSO`.
- 검증: 2스택 스킬 저작, Forte 조건 Variant 분기 실동작, haste 누적 시 쿨다운 수렴.

### Phase 2 — Task 라이브러리와 입력 계약 (S4/S5)

- 표준 Task 11종, 슬롯 입력 `InputCondition` 확장, InputBuffer 연동.
- 검증: 홀드 차지 스킬·재사용 창 스킬·채널링 스킬을 **코드 수정 없이** 저작.

### Phase 3 — 동시 실행과 조준 (S6/S9)

- 주/부 실행 분리, `Background` 정책, 전체 실행 회수 경로.
- `AbilityTargetingMode` + 예약 구조, 투사체 타게팅 해석기 통합.
- 검증: 토글 오라 유지 중 다른 스킬 사용, 사망·교체 시 태그 누수 0, 지면 지정 스킬 착탄 위치 일치.

### Phase 4 — Effect 확장과 진단 (S10/S11)

- 확률·면역·해제, 런타임 인덱스, 실패 사유 텔레메트리, 밸런스 지표.

각 Phase는 EditMode 테스트 추가를 포함한다. 현재 Ability 자동 테스트는 EditMode 14 + PlayMode 수직 슬라이스 2개다.

---

## 6. 모듈 경계

| 항목 | asmdef |
|------|--------|
| 표준 Task(입력·태그·속성·지연·분기·루프), 차지 쿨다운, haste 계산 | `UPlayGround.Ability.Core` |
| `WaitMotionEvent`, `SpawnProjectileTask`, 자원 규칙 어댑터 | `UPlayGround.Ability.UPlayGround` |
| `AbilityResourceRuleSO`, `AbilityTargetingMode`, 쿨다운/조준 정의 확장 | `UPlayGround.Data` |
| 조준 인디케이터 UI | `UPlayGround.UI` |
| 실행 목록·부 실행 관리 | `UPlayGround.Actor` (`ActorAbilitySystem`) |

Core에 프로젝트 타입을 새로 들이지 않는다. 입력·모션·투사체는 전부 포트 경유다. 이는 스펙 §7.8의 독립 모듈 완료 조건과 직결된다.

`[SerializeReference]` Task 정의를 다른 어셈블리로 옮길 경우 `[MovedFrom(true, sourceAssembly: "...")]`를 유지한다.

---

## 7. 리스크와 함정

| 리스크 | 대응 |
|--------|------|
| **쿨다운 키 변경으로 기존 세이브 손상.** 슬롯 키 → abilityId 키 전환 시 저장된 쿨다운이 복원되지 않는다. | 로드 시 구 키(`Ability.SkillSlot.n`)를 발견하면 해당 슬롯의 현재 Ability 그룹으로 1회 이관하고 구 키를 폐기. 스키마 버전 증가. |
| **레거시 경로 제거 시 무기 미보유 캐릭터가 스킬을 잃는다.** Ability 정의가 비어 있는 슬롯이 남아 있으면 즉시 무력화된다. | 제거 전에 검증기로 "모든 캐릭터 × 모든 슬롯"의 Ability 존재와 실행 가능 Variant를 확인. 통과 전에는 제거 금지. |
| **haste 전환으로 기존 밸런스 붕괴.** 곱연산 배율 값이 그대로 haste로 읽히면 수치가 뒤집힌다. | 변환식으로 1회 마이그레이션 후 스냅샷 diff 도구로 실효 쿨다운 전/후 비교. |
| **부 실행 누수.** 지속형이 종료되지 않고 태그·Effect가 남는다. | 부 실행에 **필수 종료 조건**(최대 지속 시간)을 데이터 필수 필드로 강제. 씬 전환·사망·교체에서 전수 회수. PlayMode 테스트로 누수 0 검증. |
| **Task 그래프 저작 난이도.** 노드가 늘면 데이터가 코드보다 어려워진다. | Task는 11종에서 멈춘다. 조건은 태그·속성으로만 표현하고 임의 스크립팅을 허용하지 않는다. 자주 쓰는 조합은 프리셋 그래프 에셋으로 제공. |
| **조준 모드와 KCC 상태 머신 충돌.** 조준 중 이동·회전 권한이 상태와 Ability로 갈린다. | 조준은 **Prepare 단계**에서만 유효하고 상태를 점유하지 않는다. 확정 후 상태 전환. 조준 중 피격되면 예약을 폐기. |
| **입력 계층 확장의 파급.** 슬롯 입력을 `InputCondition`으로 바꾸면 5개 상태의 게이트가 함께 바뀐다. | Phase 0에서 게이트를 `EvaluatePlayerSlot` 한 곳으로 모은 뒤에 입력 확장을 진행(순서 역전 금지). |

---

## 8. 테스트 체크리스트

| 구분 | 항목 |
|------|------|
| 권위 | 슬롯 HUD 쿨다운과 실제 재사용 가능 시점이 모든 캐릭터에서 일치 |
| 권위 | 패시브 CDR이 Ability/Ultimate/ElementalImbue 전 경로에 반영 |
| 권위 | Ultimate 비용 규칙이 `AbilityCostPolicy` 하나로만 결정 |
| 트랜잭션 | 상태 전환 실패·Variant 부재 시 자원·쿨다운 미소비 (기존 불변식 유지) |
| 자원 | Forte 축적·소비와 Variant 조건 분기 동작 |
| 자원 | 차지 2스택 스킬의 연속 사용과 재충전 |
| 자원 | haste 누적 시 실효 쿨다운이 0으로 발산하지 않음 |
| Task | 홀드 차지 스킬: 릴리즈 타이밍별 다른 Variant/피해 |
| Task | 재사용 창 스킬: 창 안 재입력 시 2단, 창 밖은 쿨다운 진입 |
| Task | 채널링 중 피격·이동 입력에 의한 취소와 자원 정산 |
| 동시성 | 부 실행 유지 중 주 실행 스킬 사용 가능 |
| 동시성 | 사망·교체·씬 전환 후 태그·Effect·Task 누수 0 |
| 조준 | 지면 지정 스킬의 인디케이터 위치 = 실제 판정 위치 |
| 조준 | 조준 취소 시 자원·쿨다운 미소비 |
| 저장 | 구 쿨다운 키 세이브 로드 시 이관 성공, 신규 저장은 신 키만 사용 |
| 몬스터 | BT의 `aiSelectable` 후보 선택 결과가 인덱스 전환 후에도 동일 |
| 성능 | 보스 전투 중 Ability 탐색 GC Alloc 0 |
| 검증 | 모든 캐릭터 × 슬롯의 Ability 존재·실행 가능 Variant 검증 오류 0 |

---

## 9. 채택하지 않은 안

| 안 | 사유 |
|----|------|
| 언리얼 GAS 전면 이식(예측·복제·GameplayCue 계층) | 싱글플레이. 복제·예측이 불필요하고 V1이 이미 필요한 책임만 추린 상태다. |
| 임의 스크립팅이 가능한 범용 Action 그래프 | 저작 난이도와 디버깅 비용이 코드보다 커진다. Task를 유한 집합으로 고정. |
| 스킬 트리 / 룬 / 임의 모디파이어 조합 | 현재 성장 시스템은 캐릭터별 레벨·해금 구조다. 별도 메타 설계 없이 도입하면 밸런스 축이 두 개가 된다. 필요 시 별도 문서. |
| 슬롯 수 확장(4~6버튼) | 선행 설계의 결론(2슬롯 + Variant 해석)을 유지한다. 깊이는 버튼이 아니라 조건 분기로 만든다. |
| `SkillCharge` 자원 유지 | 차지 쿨다운(§4.3)과 개념이 중복된다. 하나로 흡수한다. |
| 런타임 SO 수정 기반 스킬 강화 | 빌드에서 저장되지 않고 V1 비범위다. 강화는 Effect/Attribute로 표현. |

---

## 참고 자료

- [GASDocumentation (tranek) – AbilityTask 목록과 사용 패턴](https://github.com/tranek/GASDocumentation)
- [From Wait Delays to Play Montage: 10 useful GAS Ability Tasks](https://www.quodsoler.com/blog/from-wait-delays-to-play-montage-10-useful-gas-ability-tasks)
- [Melee Abilities in ARPG (Unreal 샘플) – 몽타주 + 이벤트 조합](https://dq8iqaixvew1d.cloudfront.net/en-US/Resources/SampleGames/ARPG/GameplayAbilitiesinActionRPG/MeleeAbilitiesInARPG/index.html)
- [League of Legends Wiki – Haste (선형 haste vs 퍼센트 CDR)](https://wiki.leagueoflegends.com/en-us/Haste)
- [Ability Haste: Formula, CDR Conversion, and Cap](https://blog.loltheory.gg/ability-haste-lol/)
- [Guild Wars 2 Wiki – Targeting (지면 지정·자동 대상)](https://wiki.guildwars2.com/wiki/Targeting)
- [Guild Wars 2 Wiki – Action Camera (크로스헤어 조준)](https://wiki.guildwars2.com/wiki/Action_Camera)
