# 플레이어 스킬 시스템 개선 설계 문서

> **설계 기록 주의:** 이 문서는 현재 Gameplay Ability 시스템의 선행 설계 기록이다. 실제 플레이어 데이터 단일 소스는 `AbilitySetSO`이며 최신 구현·후속 모듈 조건은 `../TODO/GAMEPLAY_ABILITY_SYSTEM_SPEC.md`를 따른다.

> 작성일: 2026-06-06
> 상태: Phase 2 구현 완료 / Unity 플레이 검증 대기
> 레퍼런스: 명조(Wuthering Waves) 전투 구조

---

## 구현 현황 (2026-06-06)

| 단계 | 상태 | 내용 |
|------|------|------|
| Phase 1 | 완료 | `PlayerSkillGauge.SkillSlotCount = 2` 추가, 스킬 비용/쿨타임 기본값 2슬롯화, 스킬 입력 루프 2슬롯 제한 |
| Phase 2 | 완료 | `PlayerSkillDefinition` / `PlayerSkillVariant` / `PlayerSkillResolver` 추가, `PlayerCombat`의 스킬 Peek/Execute를 Resolver 기반으로 변경 |
| 에디터 편의 | 완료 | `PlayerAttackDataSO` 커스텀 인스펙터에 `스킬정의` 탭 추가, 2슬롯 자동 생성/레거시 복사/진단 지원 |
| 호환성 | 유지 | `skillDefinitions`가 비어 있으면 기존 `skillAttackList[0/1]`을 기본 Ability/Ultimate로 사용 |
| 검증 | 완료 | `dotnet build Assembly-CSharp.csproj --no-restore` 성공. 기존 외부 패키지/Unity 참조 경고만 남음 |

---

## 에디터 작업 절차

`PlayerAttackDataSO` 에셋을 선택하면 기존 통합 인스펙터에 `스킬정의` 탭이 표시된다.

### 신규 캐릭터 / 신규 데이터

1. `스킬정의` 탭으로 이동한다.
2. `Ability/Ultimate 정의 보장` 버튼을 누른다.
3. 생성된 `Ability` 정의를 Ability로 사용한다.
4. 생성된 `Ultimate` 정의를 Ultimate로 사용한다.
5. 각 정의 안에서 `+ Variant 추가`로 조건별 기술을 추가한다.
6. Variant의 `AnimKey Override`, `Condition`, `AttackInfo`를 채운다.

### 기존 `skillAttackList` 데이터 이전

1. 기존 `스킬 공격` 탭에서 `skillAttackList[0]`, `skillAttackList[1]`이 올바른지 확인한다.
2. `스킬정의` 탭으로 이동한다.
3. `레거시 0/1 -> 기본 Variant 복사` 버튼을 누른다.
4. `skillAttackList[0]`은 Ability 기본 Variant로, `skillAttackList[1]`은 Ultimate 기본 Variant로 복사된다.
5. 이후 새 시스템에서는 `스킬정의` 탭의 Variant를 수정한다.

### 진단 메시지 기준

| 메시지 | 의미 | 조치 |
|--------|------|------|
| `레거시 skillAttackList가 N개입니다` | 런타임은 0/1번만 스킬 슬롯으로 탐색한다 | 2번 이상 항목은 Variant 또는 ComboRoute로 이전 |
| `슬롯 중복` | 같은 슬롯 정의가 둘 이상 있다 | 슬롯별 정의를 하나만 남김 |
| `실행 가능한 Variant가 없습니다` | 해당 정의가 있으면 레거시 폴백도 막힌다 | Variant의 `attackInfo.baseInfo`와 AnimKey 설정 |
| `Ability/Ultimate 정의가 없습니다` | 일부 슬롯이 정의되지 않았다 | `Ability/Ultimate 정의 보장` 버튼 사용 |

---

## 0. 결론

플레이어 스킬 시스템은 **입력 슬롯 2개 고정**으로 정리한다.

| 슬롯 | 입력 토큰 | 역할 | 프로젝트 용어 | 기본 정책 |
|------|-----------|------|-----------|-----------|
| 0 | `ComboInputToken.Skill1` | 일반 스킬 | Ability | 게이지 비용 없음 / 쿨타임 중심 / Forte 변형 |
| 1 | `ComboInputToken.Skill2` | 궁극기 | Ultimate | 게이지 비용 사용 / 긴 연출 / 높은 위력 |

단, `AnimKey`는 슬롯 수에 묶지 않는다. 같은 슬롯이라도 상태와 자원에 따라 여러 `AnimKey`를 선택한다.

```
Ability 입력
├── 지상 기본 스킬        → AnimKey.Skill_1
├── 공중 스킬             → AnimKey.Skill_Air
├── 재사용 / 2타 스킬     → AnimKey.Skill_Recast
├── Forte 강화 스킬       → AnimKey.Skill_Forte
└── 연계 라우트 스킬      → ComboRouteEntry.attackInfo.baseInfo.animKey

Ultimate 입력
├── 기본 궁극기           → AnimKey.Liberation
├── 공중 궁극기           → AnimKey.Liberation_Air
└── 강화 궁극기           → AnimKey.Liberation_Forte
```

이 설계의 핵심은 **버튼 수를 늘려 깊이를 만드는 방식이 아니라, 2개 버튼의 해석 규칙을 캐릭터별로 다르게 만드는 방식**이다.

### 0.1 적용 범위

이 문서의 `Ability` / `Ultimate` 2슬롯 구조는 **플레이어 전용**이다.

몬스터와 보스는 플레이어처럼 고정 입력 슬롯, HUD 버튼, 스킬 게이지 UI를 갖지 않는다. 따라서 몬스터 스킬은 `Ability` / `Ultimate`로 분류하지 않고, AI가 선택하는 행동 단위로 관리한다.

| 대상 | 스킬 구조 | 이유 |
|------|-----------|------|
| 플레이어 | `Ability` / `Ultimate` 2슬롯 + `SkillVariant` | 입력/UI가 고정되어야 하고, 캐릭터별 변형은 Variant로 표현해야 함 |
| 일반 몬스터 | AI/BT 패턴 기반 `Skill`, `Attack`, `SpecialAttack` | 입력 슬롯이 없고, 거리/쿨타임/페이즈/상황으로 행동을 선택함 |
| 보스 | 페이즈 기반 패턴, 특수기, 브레이크 대응기 | 전투 연출과 페이즈 조건이 중요하며 플레이어 UI 슬롯 구조와 맞지 않음 |

즉 `PlayerSkillDefinition`, `PlayerSkillVariant`, `PlayerSkillResolver`, `PlayerSkillSlot`은 이름 그대로 플레이어 시스템에만 사용한다. 몬스터 쪽에 같은 구조를 재사용하지 않는다.

---

## 1. 레퍼런스 요약

명조의 전투 능력은 대략 다음 축으로 구성된다.

| 축 | 의미 | 본 프로젝트 적용 방향 |
|----|------|----------------------|
| Basic Attack | 기본 공격, 강공, 공중 공격, 회피 카운터 등으로 파생 | 기존 `liteComboAttackList`, `heavyComboAttackList`, `jumpAttackList`, `dashAttackList` 유지 |
| Ability | 캐릭터별 액티브 스킬. 상태에 따라 재사용, 공중 사용, 강화 변형 가능 | `Ability` 슬롯으로 통합 |
| Ultimate | 별도 에너지로 여는 강력기 | `Ultimate` 슬롯으로 통합 |
| Forte Circuit | 캐릭터 고유 자원 / 패시브 / 강화 조건 | 별도 버튼이 아니라 스킬 변형 조건으로 사용 |
| Intro / Outro Skill | 교체 시 나가는 캐릭터와 들어오는 캐릭터의 특수 효과 | `entryAttack`, `swapSpecialAttack`, 스왑 잔류 공격과 연결 |
| Concerto Energy | 교체 특수 효과를 여는 자원 | 파티/교체 시스템의 별도 자원으로 분리 |

참고 자료:

- Wuthering Waves Wiki - Combat: Basic / Ability / Liberation / Forte / Intro·Outro 구조
  - https://wutheringwaves.fandom.com/wiki/Combat
- Wuthering Waves Wiki - Combat: 같은 스킬 입력의 상태별 변형과 궁극기 자원 구조
  - https://wutheringwaves.fandom.com/wiki/Combat
- Wuthering.gg - Intro / Outro Skill: Concerto Energy가 가득 찬 상태에서 교체 시 Intro/Outro 발동
  - https://wuthering.gg/guide/fighting/outro-skill-%26-intro-skill

---

## 2. 현재 구조 요약

### 2.1 관련 파일

```
Assets/02.Scripts/
├── Data/Combat/
│   └── PlayerAttackDataSO.cs
├── GameActor/
│   ├── Component/Player/
│   │   ├── PlayerCombat.cs
│   │   └── PlayerSkillGauge.cs
│   ├── MovementController/
│   │   └── PlayerMovementController.cs
│   └── State/Player/
│       ├── PlayerAttackState.cs
│       └── PlayerInterruptResolver.cs
└── UI/InputPrompt/
    ├── UI_HUD_Skill.cs
    └── UISkillSlot.cs
```

### 2.2 현재 데이터 구조

`PlayerAttackDataSO`는 캐릭터별 공격 풀을 가진다.

| 필드 | 현재 역할 |
|------|-----------|
| `liteComboAttackList` | 약공 콤보 |
| `heavyComboAttackList` | 강공 콤보 |
| `jumpAttackList` | 점프 공격 |
| `dashAttackList` | 대시 공격 |
| `skillAttackList` | 스킬 공격 목록 |
| `comboRoutes` | 입력 시퀀스 기반 연계 라우트 |
| `entryAttack` | 교체 등장 공격 |
| `swapSpecialAttack` | 풀 게이지 교체 특수 공격 |
| `chargeAnimKey`, `chargeStages` | 차지 공격 |

### 2.3 현재 자원 구조

`PlayerSkillGauge`는 단일 게이지와 슬롯별 비용/쿨타임 배열을 가진다.

| 필드 / API | 현재 역할 |
|------------|-----------|
| `_maxGauge`, `_currentGauge` | 현 Phase에서는 Ultimate 중심으로 쓰는 단일 자원 게이지 |
| `_skillCost` | 슬롯별 비용. 현재 기본 5개 |
| `_skillCooldown` | 슬롯별 쿨타임. 현재 기본 5개 |
| `CanUseSkill(int skillSlot)` | 비용 + 쿨타임 검사 |
| `ConsumeSkill(int skillSlot)` | 비용 소모 + 쿨타임 시작 |
| `OnGaugeChanged` | 게이지 UI 갱신 |
| `OnCooldownChanged` | 쿨타임 UI 갱신 |

### 2.4 현재 입력 / 실행 구조

`PlayerAttackState`는 스킬 입력을 `0..9` 범위로 훑는다.

```
PlayerAttackState
├── PeekNextAnimKey()
│   └── for i = 0..9
│       ├── controller.HasSkillInput(i)
│       ├── skillGauge.CanUseSkill(i)
│       └── combat.PeekSkillAttackAnimKey(i)
└── GetAnimKey()
    └── for i = 0..9
        ├── controller.HasSkillInput(i)
        ├── skillGauge.ConsumeSkill(i)
        └── combat.ExecuteSkillAttack(i)
```

`PlayerCombat.ExecuteSkillAttack(int skillIndex)`는 `PlayerAttackDataSO.skillAttackList[skillIndex]`를 직접 실행한다.

### 2.5 현재 UI 구조

`UI_HUD_Skill`과 `UISkillSlot`은 이미 2슬롯 방향에 가깝다.

| UI 구조 | 현재 상태 |
|---------|-----------|
| `ComboInputToken.Skill1` | Ability 슬롯. 자동으로 `GaugeSlot = 0` |
| `ComboInputToken.Skill2` | Ultimate 슬롯. 자동으로 `GaugeSlot = 1` |
| `UI_HUD_Skill` | Ability / Ultimate 고정 스킬바와 게이지/쿨타임 갱신 구조 보유 |

즉 UI는 이미 2슬롯에 가까운데, 런타임 자원/실행 로직은 N슬롯 구조로 남아 있다.

---

## 3. 현재 문제

### 3.1 슬롯 의미가 불명확하다

현재 `skillAttackList`는 단순 리스트다. 인덱스가 슬롯인지, 스킬 변형인지, 캐릭터별 기술 번호인지 명확하지 않다.

```
skillAttackList[0] = Ability ?
skillAttackList[1] = Ultimate ?
skillAttackList[2] = Ability_Recast ?
skillAttackList[3] = Skill_Air ?
```

이 구조에서는 스킬이 늘어날수록 UI 슬롯, 입력 인덱스, `AnimKey`, 비용, 쿨타임의 의미가 엇갈린다.

### 3.2 자원 역할이 섞여 있다

현재 `PlayerSkillGauge`의 단일 게이지는 다음 역할을 동시에 맡고 있다.

- 일반 스킬 비용
- 궁극기 비용
- 교체 특수 공격 조건
- 파티 HUD의 궁극기 준비 표시

명조식 구조로 가려면 최소한 다음 자원은 개념적으로 분리해야 한다.

| 자원 | 역할 |
|------|------|
| 일반 스킬 쿨타임 / 충전 횟수 | `Ability` 사용 가능 여부. 스킬 게이지는 요구하지 않음 |
| 궁극기 에너지 | `Ultimate` 사용 가능 여부. 스킬 게이지를 요구함 |
| Forte 자원 | 캐릭터 고유 강화 / 변형 조건 |
| Concerto 자원 | 교체 Intro/Outro 조건 |

### 3.3 다중 AnimKey를 표현하기 어렵다

사용자 요구는 **스킬 슬롯은 2개만 사용하되, AnimKey는 여러 개 사용**하는 것이다.

현재 구조는 `ExecuteSkillAttack(i)`가 리스트 인덱스 하나를 곧바로 실행한다. 그래서 같은 `Ability` 입력에서 지상/공중/재사용/Forte 강화/연계 라우트로 갈라지는 구조를 자연스럽게 표현하기 어렵다.

---

## 4. 목표 구조

### 4.1 상위 구조

```
입력
├── Ability → PlayerSkillSlot.Ability
└── Ultimate → PlayerSkillSlot.Ultimate

스킬 실행 요청
└── PlayerSkillResolver.Resolve(slot, context)
    ├── ComboRoute 우선 검사
    ├── 캐릭터 상태 검사
    ├── 지상 / 공중 검사
    ├── Forte 조건 검사
    ├── 쿨타임 / 비용 검사
    └── SkillVariant 선택

선택 결과
└── PlayerCombat.ExecuteSkillVariant(variant)
    ├── AttackData 생성
    ├── AnimKey 재생
    ├── 비용 소모
    └── 쿨타임 / 자원 갱신
```

### 4.2 제안 타입

아래 타입은 **신규 제안**이다. 현재 코드에 존재하지 않는다.

```csharp
public enum PlayerSkillSlot
{
    Ability = 0,
    Ultimate = 1,
}
```

```csharp
[Serializable]
public sealed class PlayerSkillDefinition
{
    public PlayerSkillSlot slot;
    public string displayName;
    public SkillCostPolicy costPolicy;
    public SkillCooldownPolicy cooldownPolicy;
    public List<PlayerSkillVariant> variants;
}
```

```csharp
[Serializable]
public sealed class PlayerSkillVariant
{
    public AnimKey animKey;
    public AbilityAttackInfo attackInfo;
    public SkillVariantCondition condition;
    public int priority;
}
```

```csharp
[Serializable]
public sealed class SkillVariantCondition
{
    public SkillGroundCondition groundCondition;
    public bool requiresForte;
    public float minForte;
    public bool requiresTarget;
    public GameplayTagId requiredTag;
}
```

### 4.3 데이터 배치

`PlayerAttackDataSO`에 다음 구조를 추가하는 방향을 권장한다.

```csharp
[Header("Skill Definitions")]
public List<PlayerSkillDefinition> skillDefinitions = new();
```

`skillAttackList`는 바로 제거하지 않고, 마이그레이션 기간에는 호환 필드로 유지한다.

| 단계 | `skillAttackList` 의미 |
|------|------------------------|
| 현재 | 스킬 공격 전체 리스트 |
| Phase 1 | 0번 = Ability 기본, 1번 = Ultimate 기본으로 제한 |
| Phase 2 | `skillDefinitions`로 이전, `skillAttackList`는 레거시 폴백 |
| Phase 3 | 데이터 이전 완료 후 숨김 또는 제거 검토 |

---

## 5. 자원 구조

### 5.1 권장 자원 분리

```
PlayerSkillResource
├── SkillCooldown[2]
├── SkillCharges[2]              선택
├── UltimateEnergy              궁극기용
├── ForteGauge                   캐릭터 고유 강화 자원
└── ConcertoGauge                교체 Intro/Outro용
```

### 5.2 Phase 1에서는 이름만 정리한다

한 번에 자원을 모두 분리하면 영향 범위가 크다. Phase 1은 기존 `PlayerSkillGauge`를 유지하되, 의미를 명확히 제한한다.

| 항목 | Phase 1 처리 |
|------|--------------|
| `_skillCost` | 길이 2로 제한 |
| `_skillCooldown` | 길이 2로 제한 |
| `CanUseSkill(int)` | `0` 또는 `1`만 허용 |
| `CurrentGauge` | 우선 궁극기/공용 게이지로 유지 |
| `Ability` | 게이지 비용 없음 + 쿨타임 중심 |
| `Ultimate` | 게이지 비용 사용 + 긴 쿨타임 또는 연출 중심 |

### 5.3 Phase 2에서 이름을 분리한다

`PlayerSkillGauge`는 장기적으로 이름을 바꾸는 편이 좋다.

| 현재 이름 | 제안 이름 | 이유 |
|-----------|-----------|------|
| `PlayerSkillGauge` | `PlayerSkillResource` | 게이지뿐 아니라 쿨타임, 충전 횟수, Forte, Concerto까지 관리 |
| `CurrentGauge` | `UltimateEnergy` | 궁극기 에너지 의미로 분리 |
| `OnGaugeChanged` | `OnUltimateEnergyChanged` | UI 의미 명확화 |
| `OnCooldownChanged` | 유지 가능 | 슬롯별 쿨타임 이벤트로 적절 |

---

## 6. Variant 선택 규칙

### 6.1 우선순위

스킬 입력이 들어오면 다음 순서로 해석한다.

| 순서 | 검사 | 설명 |
|------|------|------|
| 1 | 교체/반격/특수 상태 | 기존 `PlayerAttackState`의 패리, 카운터, 교체 공격 우선순위 유지 |
| 2 | `ComboRoute` | `대시 → 점프 → Ability` 같은 연계 라우트 우선 |
| 3 | 슬롯 사용 가능 여부 | 쿨타임, 게이지, 충전 횟수 검사 |
| 4 | 캐릭터 상태 | 지상/공중/피격 캔슬/가드 캔슬 등 |
| 5 | Forte 조건 | 강화 자원이 충분하면 강화 Variant 선택 |
| 6 | 기본 Variant | 조건이 없거나 가장 낮은 우선순위의 기본 스킬 |

### 6.2 예시

```
Ability 입력, 지상, Forte 0
→ Ability.Basic
→ AnimKey.Skill_1

Ability 입력, 공중
→ Ability.Air
→ AnimKey.Skill_Air

Ability 입력, Forte 100
→ Ability.Forte
→ AnimKey.Skill_Forte

Dash → Jump → Ability
→ ComboRoute 우선
→ AnimKey.JumpDiveSkill

Ultimate 입력, UltimateEnergy 100
→ Ultimate.Basic
→ AnimKey.Liberation
```

---

## 7. 기존 코드별 변경 방향

### 7.1 `PlayerSkillGauge`

1차 변경:

- 슬롯 수를 2개로 고정한다.
- `for` 루프와 배열 보정 기준을 `SkillSlotCount = 2`로 제한한다.
- 2개를 초과하는 기존 serialized 배열 데이터는 무시한다.

제안:

```csharp
public const int SkillSlotCount = 2;

public bool IsValidSkillSlot(int skillSlot)
{
    return skillSlot >= 0 && skillSlot < SkillSlotCount;
}
```

### 7.2 `PlayerAttackState`

현재 `for (int i = 0; i < 10; i++)` 형태를 제거한다.

변경 방향:

```csharp
for (int i = 0; i < PlayerSkillGauge.SkillSlotCount; i++)
{
    if (!playerController.HasSkillInput(i)) continue;
    ...
}
```

장기적으로는 루프 대신 입력 토큰을 명시적으로 해석한다.

```csharp
if (playerController.HasSkillInput((int)PlayerSkillSlot.Ability))
    TryResolveSkill(PlayerSkillSlot.Ability);

if (playerController.HasSkillInput((int)PlayerSkillSlot.Ultimate))
    TryResolveSkill(PlayerSkillSlot.Ultimate);
```

### 7.3 `PlayerCombat`

현재:

```csharp
ExecuteSkillAttack(int skillIndex)
PeekSkillAttackAnimKey(int skillIndex)
```

단기:

- `skillIndex`를 슬롯으로 해석한다.
- 범위는 0, 1만 허용한다.

장기:

```csharp
ExecuteSkill(PlayerSkillSlot slot, PlayerSkillContext context)
PeekSkillAnimKey(PlayerSkillSlot slot, PlayerSkillContext context)
```

### 7.4 `PlayerAttackDataSO`

단기:

- `skillAttackList[0]` = Ability 기본 공격
- `skillAttackList[1]` = Ultimate 기본 공격
- 2번 이상은 신규 데이터 입력 금지

장기:

- `skillDefinitions[0]` = Ability 정의
- `skillDefinitions[1]` = Ultimate 정의
- `variants`에서 다중 AnimKey 관리

### 7.5 `UISkillSlot`, `UI_HUD_Skill`, `UI_HUD_Party`

현재 구조는 유지 가능하다.

표시 책임은 다음처럼 나눈다.

| UI | 표시 대상 | 기준 |
|----|-----------|------|
| `UI_HUD_Skill` | Ability / Ultimate 버튼 2개 | 슬롯별 사용 가능 여부, 쿨타임, 콤보 힌트 |
| `UISkillSlot` | 개별 슬롯 내부 표시 | Ability는 쿨타임만, Ultimate는 게이지와 쿨타임 표시 |
| `UI_HUD_Party` | 파티원 초상화의 Ultimate ready 글로우 | `PlayerSkillSlot.Ultimate` 사용 가능 여부 |
| `UI_HUD_PlayerInfo` | 현재 활성 캐릭터의 Ultimate 게이지 바 | 현 Phase의 `PlayerSkillGauge.CurrentGauge` |

추가로 필요한 것:

| 항목 | 설명 |
|------|------|
| 캐릭터별 아이콘 갱신 | 현재 주석상 v1은 프리팹 직렬화 아이콘으로 캐릭터 교체 추적이 약함 |
| Forte 상태 표시 | Ability 아이콘에 강화 가능 글로우 표시 |
| 궁극기 Ready 표시 | Ultimate에 게이지 Full / 쿨타임 완료 상태 표시 |
| Concerto 표시 | 파티 HUD 쪽에서 별도 표시 |

---

## 8. 마이그레이션 계획

### Phase 1 — 슬롯 수 정리

목표: 동작은 크게 바꾸지 않고 슬롯 의미만 정리한다.

작업:

1. `PlayerSkillGauge.SkillSlotCount = 2` 추가.
2. `_skillCost`, `_skillCooldown` 기본값을 2개로 변경.
3. `CanUseSkill`, `ConsumeSkill`, `GetSkillCost`, `GetSkillCooldownDuration`의 유효 범위를 0, 1로 제한.
4. `PlayerAttackState`, `PlayerInterruptResolver`의 스킬 루프를 2개로 제한.
5. `PlayerAttackDataSO.skillAttackList` 저작 규칙을 0, 1만 사용하도록 문서화.

검증:

- Ability 입력 시 `skillAttackList[0]` 실행.
- Ultimate 입력 시 `skillAttackList[1]` 실행.
- 2번 이상 스킬 입력이 실행되지 않음.
- HUD의 `Ability`, `Ultimate` 준비/쿨타임 표시 정상.
- Ability는 스킬 게이지가 0이어도 쿨타임만 끝나면 사용 가능.
- Ultimate는 스킬 게이지와 쿨타임 조건을 모두 만족해야 사용 가능.

### Phase 2 — Variant Resolver 도입

목표: 2개 슬롯에서 다중 AnimKey를 선택한다.

작업:

1. `PlayerSkillDefinition`, `PlayerSkillVariant`, `PlayerSkillContext` 추가.
2. `PlayerAttackDataSO.skillDefinitions` 추가.
3. `PlayerSkillResolver` 추가.
4. `PlayerCombat.PeekSkillAttackAnimKey`와 `ExecuteSkillAttack`을 Resolver 기반으로 변경.
5. 기존 `skillAttackList`는 기본 Variant 폴백으로 유지.

검증:

- 지상 Ability과 공중 Ability가 다른 `AnimKey`를 재생.
- Forte 조건 충족 시 강화 Variant가 기본 Variant보다 우선.
- 조건이 맞지 않으면 기본 Variant로 폴백.

### Phase 3 — 자원 분리

목표: 명조식 자원 구조로 분리한다.

작업:

1. `PlayerSkillGauge`를 `PlayerSkillResource`로 확장 또는 교체.
2. `UltimateEnergy`, `ForteGauge`, `ConcertoGauge`를 분리.
3. `Ultimate`는 `UltimateEnergy` 중심으로 변경.
4. `Ability`는 쿨타임/충전 횟수 중심으로 변경하고 스킬 게이지를 요구하지 않는다.
5. 교체 시스템은 `ConcertoGauge`로 Intro/Outro 조건을 판단.

검증:

- Ability 사용이 궁극기 게이지를 직접 소모하지 않음.
- 궁극기 게이지가 충분할 때만 Ultimate 사용 가능.
- 교체 특수 공격은 궁극기 게이지가 아니라 Concerto 조건을 사용.

### Phase 4 — 에디터/검증기 보강

목표: 데이터 저작 실수를 막는다.

작업:

1. `PlayerAttackDataSODrawer`에 스킬 정의 탭 추가.
2. 슬롯별 Variant 목록을 인스펙터에서 편집.
3. `AnimKey.None`, MotionSet 누락, 비용/쿨타임 누락을 경고.
4. Ability/Ultimate 외 슬롯 데이터가 있으면 경고.
5. 캐릭터별 기본 Ability/Ultimate 필수 여부를 검증.

---

## 9. 데이터 저작 규칙

### 9.1 슬롯 규칙

- 스킬 슬롯은 항상 2개만 사용한다.
- `Ability`는 일반 스킬이다.
- `Ultimate`는 궁극기다.
- 캐릭터별 기술 개성은 슬롯 추가가 아니라 Variant 추가로 만든다.

### 9.2 AnimKey 규칙

- `AnimKey`는 슬롯보다 많아도 된다.
- 같은 슬롯의 Variant는 우선순위와 조건이 겹치지 않게 작성한다.
- `AnimKey.None`인 Variant는 실행 불가로 본다.
- MotionSet에 없는 `AnimKey`는 데이터 검증 오류로 본다.

### 9.3 비용 규칙

- Ability는 쿨타임 중심이며 스킬 게이지를 사용하지 않는다.
- Ultimate는 게이지 중심이다.
- 두 슬롯이 같은 게이지를 공유하지 않으므로, Ability 사용이 Ultimate 준비 상태를 훼손하지 않는다.
- Forte는 비용이라기보다 강화 조건으로 먼저 사용한다.

---

## 10. 주의 사항

### 10.1 `skillAttackList`를 곧바로 제거하지 않는다

현재 `PlayerCombat`, `PlayerAttackState`, 에디터, 기존 캐릭터 데이터가 `skillAttackList`에 의존한다. 바로 제거하면 데이터 손상이 크다.

권장 순서:

1. 2개 슬롯으로 의미 제한.
2. 새 `skillDefinitions` 추가.
3. 기존 데이터를 자동 또는 수동 이전.
4. 폴백 기간 유지.
5. 검증기가 더 이상 레거시 필드를 참조하지 않는 시점에 제거 검토.

### 10.2 교체 특수 공격과 궁극기를 분리한다

현재 `swapSpecialAttack`은 "Ultimate 게이지가 가득 찬 캐릭터로 교체할 때 발동"이라는 설명을 가진다. 장기 구조에서는 이 조건을 궁극기 게이지가 아니라 `ConcertoGauge`에 가까운 별도 교체 자원으로 분리하는 편이 명확하다.

따라서 장기적으로:

- `UltimateEnergy` = Ultimate 궁극기
- `ConcertoGauge` = 교체 Intro/Outro

로 분리해야 한다.

### 10.3 UI는 슬롯을 보여주고, Variant를 모두 보여주지 않는다

HUD에 모든 Variant를 아이콘으로 늘어놓으면 다시 슬롯 난립으로 돌아간다.

UI 원칙:

- HUD 버튼은 Ability / Ultimate 두 개만 표시.
- 현재 선택될 Variant의 상태는 글로우, 보조 아이콘, 키워드 정도로만 표시.
- Forte 강화 가능 여부는 Ability 위에 오버레이한다.
- 궁극기 준비 여부는 Ultimate 위에 강하게 표시한다.

### 10.4 몬스터 스킬은 플레이어 슬롯 구조를 쓰지 않는다

몬스터는 `Ability` / `Ultimate` 2슬롯 구조를 사용하지 않는다.

몬스터 스킬은 다음 기준으로 관리한다.

| 구분 | 권장 관리 방식 |
|------|----------------|
| 일반 공격 | 기존 `AbilitySetSO` 공격 데이터 |
| 스킬/패턴 | AI/BT 노드 또는 `EnemyBehaviorSO` 페이즈 패턴 |
| 특수기 | `SpecialAttack`, 브레이크 대응기, 보스 페이즈 전용 패턴 |
| 쿨타임/조건 | AI 블랙보드, 전투 의사결정, 페이즈/거리/HP 조건 |

몬스터가 강한 기술을 가진다고 해서 `Ultimate` 슬롯을 만들지 않는다. 보스의 큰 기술도 플레이어 궁극기와 같은 자원 UI가 아니라, 페이즈/패턴/텔레그래프/쿨타임으로 표현한다.

---

## 11. 구현 우선순위

| 우선순위 | 작업 | 이유 |
|----------|------|------|
| 1 | 스킬 슬롯 2개 고정 | 현재 중구난방한 슬롯 의미를 먼저 정리 |
| 2 | `skillAttackList` 0/1 규칙 확정 | 기존 데이터 손상 없이 즉시 적용 가능 |
| 3 | `PlayerAttackState` 루프 2개 제한 | 잘못된 입력/인덱스 실행 방지 |
| 4 | Variant Resolver 도입 | 다중 AnimKey 요구사항 해결 |
| 5 | Forte 자원 도입 | 캐릭터별 개성 확장 |
| 6 | Concerto 자원 도입 | 교체 Intro/Outro 구조 정리 |
| 7 | 에디터 검증기 보강 | 데이터 저작 안정성 확보 |

---

## 12. 최종 목표 형태

최종적으로 플레이어 스킬 시스템은 다음 책임 분리로 정리한다.

```
PlayerMovementController
└── 입력 상태 제공

PlayerAttackState
└── 현재 상태에서 공격/스킬 진입 판단

PlayerSkillResolver
└── Ability / Ultimate 입력을 실제 SkillVariant로 해석

PlayerSkillResource
└── 쿨타임, 충전 횟수, UltimateEnergy, Forte, Concerto 관리

PlayerCombat
└── 선택된 AttackData 실행, 판정/피드백 연결

PlayerAttackDataSO
└── 캐릭터별 기본 공격, 스킬 정의, Variant, 연계 라우트 보관

UI_HUD_Skill
└── Ability / Ultimate 두 슬롯의 사용 가능 상태 표시
```

이 구조에서는 입력 슬롯이 2개로 고정되므로 UI와 입력이 단순해지고, `SkillVariant`가 여러 `AnimKey`를 관리하므로 캐릭터별 액션 깊이는 유지된다.
