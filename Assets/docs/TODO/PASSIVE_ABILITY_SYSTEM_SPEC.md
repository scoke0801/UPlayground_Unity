# 캐릭터 패시브 어빌리티 적용 스펙

> 문서 버전: 1.0  
> 기준일: 2026-07-18  
> 대상 버전: Unity 6 (6000.0.60f1), 싱글플레이, URP  
> 상태: 런타임·UI·검증 기반 및 플레이어블 11종 샘플 데이터 구현 완료 / 최종 밸런스·아이콘 확정 대기  
> 관련 문서: `../Complete/GAMEPLAY_ABILITY_SYSTEM_SPEC.md`, `../guide/COMBAT_SYSTEM_GUIDE.md`, `../guide/STAT_SYSTEM_GUIDE.md`

### 2026-07-18 구현 체크포인트

- 패시브 정의·캐릭터 세트·DB와 `IPassiveModifierReader` 구현 완료.
- 전투 피해·Break·쿨다운·퍼펙트 방어 Trigger·Effect 지속시간 연동 완료.
- 소비품·장비 옵션 행운·제작 재료·경험치 연동 완료.
- `UI_CharacterSelect`에서 기존 무기 효과 영역을 제거하고 대표 패시브 2행만 노출하도록 구현 완료.
- 검증기 규칙과 순수 계산 EditMode 테스트 5개 추가.
- 패시브 13종, 발동 Effect 2종, 플레이어블 캐릭터 세트 11종을 `CharacterPassiveDatabase.asset`에 구성하고 `PartyConfig.asset`에 연결.
- 캐릭터마다 대표 패시브 2개를 배치했으며, 강한 전투 효과끼리 한 캐릭터에 겹치지 않도록 전투·유틸리티 효과를 조합했다.
- Ability Editor에 Passive 필터·생성·상세 편집·캐릭터 AbilitySet 범위 연동과 임베디드 서브에셋 전체 검증을 추가했다.
- 현재 수치와 설명은 플레이 확인용 초안이다. 최종 밸런스와 전용 아이콘은 기획값 확정 후 교체한다.

## 1. 목적

캐릭터마다 고유한 패시브 어빌리티를 보유하고, 선택한 캐릭터와 현재 파티 구성에 따라 전투·성장·아이템·제작 규칙에 일관되게 반영한다.

핵심 목표는 다음과 같다.

- 패시브의 표시 데이터와 실제 계산 데이터를 하나의 에셋에서 관리한다.
- 캐릭터가 보유할 수 있는 패시브 총개수에는 제한을 두지 않는다.
- 플레이어 캐릭터 선택 화면에는 캐릭터별 대표 패시브를 최대 2개만 노출한다.
- `UI_CharacterSelect`는 패시브를 조회·표시하지만 런타임 효과를 직접 적용하지 않는다.
- 약공격·강공격·스킬처럼 조건이 붙는 보정은 전역 `AttackPower`와 분리한다.
- 퍼펙트 회피·퍼펙트 가드 보상은 기존 `GameplayEffectSO`의 지속시간·중첩 정책을 재사용한다.
- 아이템 옵션, 제작 비용, 경험치처럼 Actor 밖에서 계산되는 값은 소비자 서비스가 읽는 계약으로 제공한다.
- 캐릭터 교체, 세이브·로드, 잔류 공격에서도 패시브 적용 주체와 적용 시점을 명확히 한다.

---

## 2. 설계 결정

| ID | 결정 |
|----|------|
| P-01 | 패시브 정의는 신규 `PassiveAbilitySO`, 캐릭터별 묶음은 `CharacterPassiveSetSO`가 소유한다. |
| P-02 | 캐릭터 타입과 패시브 세트의 단일 매핑은 `CharacterPassiveDatabaseSO`가 소유한다. |
| P-03 | `UI_CharacterSelect`와 런타임은 같은 `CharacterPassiveDatabaseSO`를 읽는다. UI용 설명을 별도로 복제하지 않는다. |
| P-04 | `UI_CharacterSelect`는 표시와 캐릭터 확정만 담당한다. 패시브 Grant나 Effect 적용은 하지 않는다. |
| P-05 | 기존 `AbilityCategory.Passive`는 표시 분류로 유지할 수 있지만, 실행 Variant가 필요한 `GameplayAbilitySO`를 상시 패시브 정의로 사용하지 않는다. |
| P-06 | 선택 공격 피해·Break·쿨다운 등 문맥형 보정은 `StatType`을 늘리지 않고 `IPassiveModifierReader`로 조회한다. |
| P-07 | 일반 공격 데이터인 `HitPhaseData.damage`, `breakDamage`는 수정하지 않는다. 런타임 `AttackData`를 만들 때 최종 배율을 적용한다. |
| P-08 | 퍼펙트 방어 발동형 패시브는 실제 성공 판정 이벤트에서 `GameplayEffectSO`를 적용한다. 피드백 재생 이벤트를 게임 규칙 트리거로 사용하지 않는다. |
| P-09 | 전투 패시브는 현재 활성 캐릭터만 적용한다. 경험치 패시브는 경험치를 받는 각 캐릭터에게 개별 적용한다. |
| P-10 | 제작·장비 옵션 행운은 출전 파티에서 가장 높은 값 하나만 사용한다. 합산하지 않으며 캐릭터 교체로 결과가 달라지지 않는다. |
| P-11 | 패시브 정의와 캐릭터 매핑은 정적 데이터이므로 세이브하지 않는다. 저장이 필요한 발동형 Effect 상태만 기존 Effect 저장 정책을 따른다. |
| P-12 | 패시브 계산은 원본 값에 한 번만 적용하며, UI 미리보기와 실제 소비가 같은 계산 API를 사용한다. |
| P-13 | 캐릭터당 런타임 패시브 개수는 제한하지 않는다. `UI_CharacterSelect`에는 세트에서 명시한 대표 패시브 중 최대 2개만 표시한다. |

---

## 3. 현재 기반과 필요한 확장

### 3.1 재사용할 현재 구조

| 영역 | 현재 타입 | 재사용 방식 |
|------|-----------|-------------|
| 캐릭터 식별 | `CharacterActorType`, `PlayerSwapBehaviour.ActiveCharacterType` | 활성 패시브 세트 선택 |
| 캐릭터 선택 UI | `CharacterSelectDatabaseSO`, `UI_CharacterSelect` | 같은 캐릭터 타입으로 패시브 표시 |
| 공격 분류 | `AttackKind.NormalAttack`, `HeavyAttack`, `SkillAttack` | 약·강·스킬 피해 필터 |
| 공격 런타임 | `PlayerCombat`, `AttackData`, `HitRequest` | 피해·Break 배율 적용 |
| Ability 쿨다운 | `ActorAbilitySystem`, `AbilityCooldownRuntime` | 쿨다운 시작 시간 보정 |
| 시간제 능력치 | `GameplayEffectSO`, `GameplayEffectController` | 퍼펙트 방어 발동 버프 |
| 스탯 | `ActorStatContainer`, `StatModifier` | 발동형 능력치 강화 |
| 방어 성공 | `PlayerActor.TryPerfectDodge`, `PlayerGuardState.OnAttackBlocked` | 실제 성공 이벤트 발행 |
| 소비 아이템 | `InventoryManager.TryApplyConsumable` | 회복량 보정 |
| 제작 | `RecipeManager` | 필요 재료 견적 보정 |
| 경험치 | `PartyManager.AwardBattleExp`, `AddExp` | 수령 캐릭터별 보정 |
| 장비 옵션 | `InventoryManager.RollGrowthAttributes` | 높은 랭크 확률 보정 |

### 3.2 구조적 간극

- `StatType.AttackPower`는 모든 공격에 적용되므로 약공격·강공격·스킬 피해를 구분할 수 없다.
- `GameplayEffectSO`에는 이로운 효과와 해로운 효과 구분이 없어 상태강화 지속시간과 상태이상 회복을 구분할 수 없다.
- `ActorAbilitySystem.StartCooldown()`은 정의의 원본 지속시간을 그대로 사용한다.
- `InventoryManager.RollGrowthAttributes()`는 랭크를 균등 추첨하며 패시브 문맥을 받지 않는다.
- `RecipeManager`는 필요 수량을 여러 메서드에서 각각 계산하므로 한 곳만 보정하면 UI와 실제 차감이 어긋난다.
- 퍼펙트 회피와 퍼펙트 가드는 성공 피드백은 있으나 패시브가 구독할 공통 게임플레이 이벤트가 없다.
- 범용 상태이상 축적·회복 시스템은 현재 확인되지 않는다. 1차에서는 해로운 `GameplayEffectSO`의 지속시간 감소로 정의한다.

---

## 4. 상위 아키텍처

```text
CharacterPassiveDatabaseSO
        │ CharacterActorType로 조회
        ├──────────────────────────────┐
        ▼                              ▼
UI_CharacterSelect              PassiveAbilityController
├─ 아이콘/이름/설명 표시         ├─ 활성 캐릭터 세트 갱신
└─ CharacterConfirmed만 발행     ├─ 방어 성공 Trigger 처리
                                └─ GameplayEffect 적용
                                         │
                     ┌───────────────────┴───────────────────┐
                     ▼                                       ▼
             IPassiveModifierReader                  GameplayEffectController
             ├─ 공격 문맥 배율                        ├─ 시간제 StatModifier
             ├─ 쿨다운 배율                           ├─ 중첩/갱신/교체
             ├─ 회복/경험치 배율                       └─ 저장 정책
             └─ 제작/행운 배율
                     │
       ┌─────────────┼────────────┬──────────────┬──────────────┐
       ▼             ▼            ▼              ▼              ▼
 PlayerCombat  ActorAbility  InventoryManager  RecipeManager  PartyManager
```

런타임의 단일 조회 진입점은 `IPassiveModifierReader`다. 각 소비자는 `PassiveAbilitySO` 목록을 직접 순회하지 않는다.

---

## 5. 데이터 모델

### 5.1 `PassiveAbilitySO`

제안 경로: `Assets/02.Scripts/Data/Ability/Passive/PassiveAbilitySO.cs`

```csharp
[CreateAssetMenu(
    fileName = "PA_",
    menuName = "UPlayGround/Ability/Passive Ability")]
public sealed class PassiveAbilitySO : ScriptableObject
{
    public string passiveId;
    [Min(1)] public int schemaVersion = 1;
    public AbilityPresentationDefinition presentation = new();
    public PassiveActivationType activationType;
    public PassiveScope scope;
    public PassiveStackPolicy stackPolicy;
    public List<PassiveModifierDefinition> modifiers = new();
    public List<GameplayEffectSO> triggeredEffects = new();
}
```

규칙:

- `passiveId`는 전 프로젝트에서 유일하고 저장·텔레메트리에서 사용할 수 있는 안정 ID다.
- `presentation.category`는 `AbilityCategory.Passive`여야 한다.
- 상시형은 `modifiers`, 발동형은 `triggeredEffects`를 사용한다.
- 한 패시브에 둘을 함께 둘 수 있지만 1차 데이터는 한 가지 책임만 갖도록 분리하는 것을 권장한다.

### 5.2 열거형

```csharp
public enum PassiveActivationType
{
    Always,
    PerfectDodge,
    PerfectGuard,
}

public enum PassiveScope
{
    ActiveCharacter,
    OwnerCharacter,
    BattlePartyHighest,
}

public enum PassiveStackPolicy
{
    Additive,
    HighestOnly,
}

public enum PassiveModifierType
{
    LightAttackDamage,
    HeavyAttackDamage,
    SkillDamage,
    SkillCooldownDuration,
    BreakDamage,
    EquipmentGrowthRankLuck,
    ConsumableRecovery,
    CraftIngredientCost,
    ExperienceGain,
    HarmfulEffectDuration,
    BeneficialEffectDuration,
}
```

`StatusRecoverySpeed` 대신 `HarmfulEffectDuration`을 사용한다. 현재 상태이상 회복 게이지가 없으므로 “회복 속도 +25%”를 실제 계산에서는 `1 / 1.25 = 0.8`배 지속시간으로 변환한다.

### 5.3 `PassiveModifierDefinition`

```csharp
[Serializable]
public sealed class PassiveModifierDefinition
{
    public PassiveModifierType modifierType;
    public ModifierType operation = ModifierType.Percent;
    public float value;

    [Header("Ability Filter")]
    public PassiveAbilitySlotFilter abilitySlotFilter;
}
```

값 규칙:

- 증가형 `Percent +0.15`는 최종 배율 `1.15`다.
- 감소형 쿨다운·제작 비용은 `Percent -0.15`로 최종 배율 `0.85`다.
- 최종 배율은 음수가 될 수 없다.
- `SkillCooldownDuration`은 기본적으로 `PlayerSkillSlot.Ability`만 대상으로 하고, 데이터에서 `Ultimate` 포함 여부를 선택할 수 있게 한다.
- 전투 공격·대시·스왑 쿨다운에는 적용하지 않는다.

### 5.4 캐릭터별 세트와 데이터베이스

```csharp
[CreateAssetMenu(fileName = "PassiveSet_", menuName = "UPlayGround/Ability/Character Passive Set")]
public sealed class CharacterPassiveSetSO : ScriptableObject
{
    public CharacterActorType characterType;

    [Tooltip("캐릭터가 실제로 보유하는 전체 패시브. 개수 제한 없음.")]
    public List<PassiveAbilitySO> passives = new();

    [Tooltip("UI_CharacterSelect에 표시할 대표 패시브. passives에 포함된 항목만, 최대 2개.")]
    public List<PassiveAbilitySO> characterSelectRepresentatives = new();
}

[CreateAssetMenu(fileName = "CharacterPassiveDatabase", menuName = "UPlayGround/Ability/Character Passive Database")]
public sealed class CharacterPassiveDatabaseSO : ScriptableObject
{
    public List<CharacterPassiveSetSO> entries = new();

    public CharacterPassiveSetSO Get(CharacterActorType type);
}
```

연결 위치:

- `PartyConfigSO.characterPassiveDatabase`: 런타임 조회용.
- `UI_CharacterSelect._passiveDatabase`: 선택 화면 표시용.
- 두 필드는 반드시 같은 에셋을 참조한다.
- `CharacterSelectDatabaseSO.Entry`에는 패시브 이름·설명 사본을 추가하지 않는다.
- `passives`에는 개수 제한을 두지 않으며 런타임은 목록 전체를 적용한다.
- `characterSelectRepresentatives`는 `passives`에 포함된 패시브만 참조하고 최대 2개까지 지정한다.
- 대표 패시브의 표시 순서는 `characterSelectRepresentatives`의 직렬화 순서를 따른다.
- 대표 패시브가 2개보다 적으면 지정된 항목만 표시하며, 전체 패시브를 자동으로 채워 넣지 않는다.

### 5.5 Effect 성격과 지속시간

`GameplayEffectSO`에 다음 메타데이터를 추가한다.

```csharp
public enum GameplayEffectPolarity
{
    Neutral,
    Beneficial,
    Harmful,
}

public GameplayEffectPolarity polarity;
public bool ignorePassiveDurationModifiers;
```

적용 시점에 유효 지속시간을 계산해 `GameplayEffectInstance`에 캡처한다.

```text
Beneficial: effectiveDuration = baseDuration × BeneficialEffectDuration 배율
Harmful:    effectiveDuration = baseDuration × HarmfulEffectDuration 배율
Neutral:    baseDuration 유지
```

SO의 `durationSeconds`는 변경하지 않는다. 이미 적용된 Effect는 이후 캐릭터 교체나 패시브 변경으로 지속시간을 재계산하지 않는다.

---

## 6. 런타임 계약

### 6.1 `IPassiveModifierReader`

제안 위치: `UPlayGround.Contracts`

```csharp
public interface IPassiveModifierReader : IGameService
{
    float GetActiveMultiplier(PassiveModifierType type);
    float GetCharacterMultiplier(
        CharacterActorType characterType,
        PassiveModifierType type);
    float GetBattlePartyMultiplier(PassiveModifierType type);
}
```

반환값은 원본에 곱할 최종 배율이며 패시브가 없으면 항상 `1f`다.

조회 정책:

| API | 사용처 |
|-----|--------|
| `GetActiveMultiplier` | 공격, 쿨다운, 소비품, 이로운/해로운 Effect |
| `GetCharacterMultiplier` | 캐릭터별 경험치 |
| `GetBattlePartyMultiplier` | 제작 재료, 장비 옵션 행운 |

구현은 `PartyManager`가 직접 맡거나 별도 순수 C# `PassiveModifierService`를 `PartyManager`가 소유한다. 새 전역 MonoBehaviour 매니저는 만들지 않는다.

### 6.2 `PassiveAbilityController`

`PlayerActor`에 부착되는 Actor 런타임 컴포넌트다.

역할:

- 활성 `CharacterActorType` 변경 시 `CharacterPassiveSetSO` 갱신.
- 퍼펙트 회피·퍼펙트 가드 이벤트 구독.
- 해당 Trigger의 `triggeredEffects`를 `GameplayEffectController`에 적용.
- 캐릭터 교체 시 이전 캐릭터의 패시브 발동 상태 정리.

상시 문맥형 배율은 Controller가 `AttackData`를 직접 변경하는 방식이 아니라 `IPassiveModifierReader`가 제공한다.

### 6.3 방어 성공 이벤트

`PlayerActor`에 게임 규칙용 이벤트를 추가한다.

```csharp
public event Action<DefenseSuccessType> DefenseSucceeded;

internal void NotifyDefenseSucceeded(DefenseSuccessType type)
    => DefenseSucceeded?.Invoke(type);
```

발행 위치:

- 퍼펙트 회피: `TryPerfectDodge()`가 창을 소비하고 반격 창을 연 뒤 한 번 발행.
- 퍼펙트 가드: `PlayerGuardState.OnAttackBlocked()`가 `isPerfectGuard`를 확정한 뒤 한 번 발행.
- 일반 가드, 대시 회피 연출, 패리에는 해당 Trigger를 발행하지 않는다.
- 동일 공격의 다단 히트가 같은 성공을 중복 발행하지 않도록 기존 창 소비를 선행한다.

`DefenseSuccessFeedbackHandler`는 연출 책임이므로 패시브 Trigger의 원본으로 사용하지 않는다.

### 6.4 캐릭터 교체

교체 순서:

1. 이전 캐릭터의 Ability/Effect 런타임 스냅샷 처리.
2. `PlayerSwapBehaviour`가 활성 모델과 `CharacterActorType` 변경.
3. `PlayerActor.RefreshForCharacter()`가 스탯·AbilitySet 갱신.
4. `PassiveAbilityController.RefreshForCharacter(type)` 호출.
5. UI와 파티 변경 이벤트 발행.

발동형 버프의 기본 정책은 `RemoveOnSwap`이다. `PersistPerCharacter`를 허용하려면 다음을 함께 구현해야 한다.

- 캐릭터별 Effect 런타임 저장소에 패시브 Effect 포함.
- `ActorAbilitySystem.ResolveEffectDefinition()`이 AbilitySet뿐 아니라 현재 `CharacterPassiveSetSO`의 Effect도 찾도록 Resolver 확장.
- 같은 PlayerActor를 공유하는 다른 캐릭터에게 Effect가 누출되지 않는 테스트.

1차 구현에서는 패시브 발동형 Effect를 저장하지 않고 교체 시 제거하는 것을 권장한다.

---

## 7. 13개 패시브 적용 규칙

| 번호 | 기획 의도 | 데이터 표현 | 적용 지점 | 기본 범위 |
|------|-----------|-------------|-----------|-----------|
| 1 | 약공격 피해 강화 | `LightAttackDamage`, `+value` | `AttackKind.NormalAttack`의 `AttackData.damageMultiplier` | 활성 캐릭터 |
| 2 | 강공격 피해 강화 | `HeavyAttackDamage`, `+value` | `HeavyAttack`, 필요 시 `ChargeAttack` 포함 필터 | 활성 캐릭터 |
| 3 | 스킬 쿨타임 감소 | `SkillCooldownDuration`, `-value` | `ActorAbilitySystem.StartCooldown()`과 슬롯 View duration | 활성 캐릭터 |
| 4 | 브레이크 수치 강화 | `BreakDamage`, `+value` | `AttackData.breakDamage`만 증가 | 활성 캐릭터 |
| 5 | 좋은 아이템 옵션 확률 강화 | `EquipmentGrowthRankLuck`, `+value` | 장비 신규 획득 시 옵션 랭크 추첨 | 출전 파티 최고값 |
| 6 | 퍼펙트 회피 후 N초 능력치 강화 | `PerfectDodge` + Duration `GameplayEffectSO` | `DefenseSucceeded(PerfectDodge)` | 발동한 활성 캐릭터 |
| 7 | 퍼펙트 가드 후 N초 능력치 강화 | `PerfectGuard` + Duration `GameplayEffectSO` | `DefenseSucceeded(PerfectGuard)` | 발동한 활성 캐릭터 |
| 8 | 소비품 회복량 증가 | `ConsumableRecovery`, `+value` | `InventoryManager.TryApplyConsumable()` | 회복 대상 활성 캐릭터 |
| 9 | 제작 재료 감소 | `CraftIngredientCost`, `-value` | 공통 제작 비용 견적 | 출전 파티 최고값 |
| 10 | 경험치 획득량 증가 | `ExperienceGain`, `+value` | `AwardBattleExp()`에서 수령자별 계산 | 보유 캐릭터 자신 |
| 11 | 스킬 피해 강화 | `SkillDamage`, `+value` | `AttackKind.SkillAttack`의 `AttackData.damageMultiplier` | 활성 캐릭터 |
| 12 | 상태이상 회복 속도 증가 | `HarmfulEffectDuration`, 역수 환산 | Harmful Effect 적용 시 유효 지속시간 감소 | 활성 캐릭터 |
| 13 | 상태강화 유지시간 증가 | `BeneficialEffectDuration`, `+value` | Beneficial Effect 적용 시 유효 지속시간 증가 | 활성 캐릭터 |

### 7.1 공격 배율

`PlayerCombat.ConvertToAttackData()`에서 공격 종류가 확정된 뒤 배율을 캡처한다.

```text
finalBaseDamage
= HitPhaseData.damage
 × 기존 damageMultiplier
 × 해당 AttackKind 패시브 배율

finalBreakDamage
= HitPhaseData.breakDamage
 × 기존 poiseMultiplier
 × BreakDamage 패시브 배율
```

실제 구현은 곱셈이며 위 표기는 계산 항목 구분을 위한 것이다.

정책:

- 약공격 패시브는 `NormalAttack`만 적용한다. `JumpAttack`, `DashAttack`, 카운터는 자동 포함하지 않는다.
- 강공격 패시브는 기본적으로 `HeavyAttack`에 적용한다. `ChargeAttack` 포함 여부는 필터로 명시한다.
- 스킬 피해 패시브는 `SkillAttack`에만 적용한다.
- `FinishAttack`과 최대 HP 비례 `SpecialBreak` 피해에는 적용하지 않는다.
- `AttackPower`는 기존처럼 모든 표준 피해에 별도로 적용된다.
- 잔류 공격은 교체 순간 만들어진 `AttackData` 스냅샷의 배율을 유지한다. 교체 후 새 활성 캐릭터의 패시브를 다시 조회하지 않는다.

### 7.2 쿨다운 감소

```text
effectiveCooldown = max(0, definition.cooldown.durationSeconds × multiplier)
```

- Commit 시 effective cooldown을 `AbilityCooldownRuntime.Start()`에 전달한다.
- `AbilitySlotViewState.CooldownDuration`도 같은 계산기를 사용한다.
- 쿨다운 시작 후 캐릭터를 교체해도 이미 시작된 남은 시간은 재계산하지 않는다.
- 1차 기본 필터는 `PlayerSkillSlot.Ability`다. Ultimate 포함은 데이터에서 명시한다.

### 7.3 장비 옵션 행운

현재 장비 옵션은 `InventoryManager.RollGrowthAttributes()`에서 옵션 종류와 랭크를 추첨한다. 패시브는 옵션 개수를 늘리지 않고 각 옵션의 랭크 상승 확률만 높인다.

권장 1차 공식:

```text
baseRank = Random.Range(randomRankMin, randomRankMax + 1)
if Random.value < luckBonus:
    finalRank = min(baseRank + 1, randomRankMax)
else:
    finalRank = baseRank
```

- `luckBonus`는 `최종 배율 - 1`을 0~1로 제한한 값이다.
- 출전 파티의 최고 `EquipmentGrowthRankLuck` 하나만 사용한다.
- 장비를 새로 생성하는 순간 한 번만 적용한다.
- 세이브 복원, 장비 이동, 장착 변경에서는 재추첨하지 않는다.
- 제작 결과가 장비여도 실제 인벤토리에 새 장비 인스턴스를 추가하는 동일 경로에서 적용한다.

### 7.4 소비품 회복

```text
HealFlat:    effectiveAmount = amount × ConsumableRecovery
HealPercent: effectiveRatio  = amount × ConsumableRecovery
```

- 최대 체력을 초과하는 회복은 기존 `PlayerActor.Heal()`이 제한한다.
- `requireEffectiveUse` 판정은 보정된 회복을 적용한 실제 결과를 기준으로 유지한다.
- 아이템 원본 `ConsumableSO.amount`는 변경하지 않는다.

### 7.5 제작 재료 감소

공통 `CraftCostQuote`를 도입해 아래 메서드가 같은 결과를 사용하게 한다.

- `CanCraft`
- `GetCraftAvailabilityReason`
- `GetMissingIngredients`
- `DeductResources`
- `GetMaxCraftableQuantity`
- `GetIngredientAvailability`
- 제작 UI의 필요 수량 표시

재료별 공식:

```text
required = max(1, ceil(baseRequired × quantity × CraftIngredientCost))
```

- 원본 필요 수량이 0이면 0을 유지한다.
- 패시브만으로 재료가 무료가 되지 않는다.
- 골드 비용과 제작 시간에는 적용하지 않는다.
- 출전 파티 최고값 하나만 적용해 캐릭터 교체 직전 악용과 중첩 폭증을 막는다.
- 제작 시작 시 `CraftCostQuote`를 한 번 캡처하고 검증과 차감에 같은 Quote를 사용한다.

### 7.6 경험치 증가

`AwardBattleExp()`의 공용 금액을 먼저 바꾸면 모든 캐릭터가 한 캐릭터의 패시브 혜택을 받게 되므로 금지한다.

```csharp
foreach (CharacterActorType type in _battleOrder)
{
    float multiplier = passives.GetCharacterMultiplier(
        type,
        PassiveModifierType.ExperienceGain);
    long granted = RoundToLong(amount * multiplier);
    AddExp(type, granted);
}
```

반올림은 `MidpointRounding.AwayFromZero`로 통일하고 최소 지급량은 원본이 양수일 때 1로 보장한다.

### 7.7 상태이상 회복과 상태강화 지속시간

1차 정의:

- 상태이상: `GameplayEffectPolarity.Harmful`인 Duration Effect.
- 상태강화: `GameplayEffectPolarity.Beneficial`인 Duration Effect.
- Instant와 Infinite Effect에는 지속시간 배율을 적용하지 않는다.

“회복 속도 +25%” 표시는 다음과 같이 지속시간으로 변환한다.

```text
recoverySpeed = 1.25
harmfulDurationMultiplier = 1 / recoverySpeed = 0.8
```

향후 독립 상태이상 축적 게이지가 생기면 `StatusRecoverySpeed` 채널을 별도로 추가하고, 이 문서의 Harmful Effect 지속시간 감소와 중복 적용할지 정책을 다시 정한다.

---

## 8. `UI_CharacterSelect` 표시와 처리

### 8.1 책임

`UI_CharacterSelect`의 책임:

- 선택된 `CharacterActorType`으로 `CharacterPassiveDatabaseSO.Get()` 호출.
- `CharacterPassiveSetSO.characterSelectRepresentatives`에서 앞의 최대 2개를 조회.
- 대표 패시브의 아이콘, 이름, 설명, 발동 조건 표시.
- 실제 보유 패시브가 3개 이상이어도 선택 화면에는 대표 2개만 표시.
- 잠긴 캐릭터의 정보 공개 정책에 따라 표시 또는 마스킹.
- 기존 `CharacterConfirmed(CharacterActorType)` 이벤트 유지.

책임이 아닌 것:

- `GameplayEffectController.ApplyEffect()` 호출.
- `ActorStatContainer` 수정.
- `PartyManager` 내부 패시브 상태 생성.
- UI 전용 패시브 값 복사본 저장.

### 8.2 제안 필드

```csharp
[Header("Passive Abilities")]
[SerializeField] private CharacterPassiveDatabaseSO _passiveDatabase;
[SerializeField] private UIPassiveAbilityRow _passiveRowPrefab;
[SerializeField] private Transform _passiveRowRoot;
[SerializeField] private TextMeshProUGUI _passiveEmptyText;
```

`UIPassiveAbilityRow`는 `UI_Base`가 아닌 보조 클래스이므로 `UI_` 접두사를 사용하지 않고 `UIPassiveAbilityRow`로 명명한다.

표시 항목:

| 항목 | 원본 |
|------|------|
| 아이콘 | `PassiveAbilitySO.presentation.icon` |
| 이름 | localization key 우선, 없으면 `displayName` |
| 설명 | localization key 우선, 없으면 `description` |
| 발동 라벨 | `Always`, `PerfectDodge`, `PerfectGuard`의 현지화 문자열 |
| 지속시간 | 연결된 Duration `GameplayEffectSO`의 유효 기본 시간 |

설명에 수치를 직접 반복 입력하면 밸런스 변경 시 불일치할 수 있다. 1차에서는 `{value}`, `{duration}` 토큰을 지원하는 `PassiveDescriptionFormatter`를 두거나, 에디터 검증기가 설명 속 수치와 실제 데이터 불일치를 경고하도록 한다.

대표 패시브 선택 정책:

- 대표 패시브는 자동 점수 계산이 아니라 `CharacterPassiveSetSO.characterSelectRepresentatives`에 명시적으로 지정한다.
- 최대 표시 개수는 `UI_CharacterSelect` 상수 `MaxRepresentativePassiveCount = 2`로 고정한다.
- 런타임 적용 목록은 항상 `passives` 전체이며 대표 목록은 계산에 관여하지 않는다.
- 대표 목록에 null, 중복, 전체 목록에 없는 참조가 있으면 해당 항목을 건너뛰고 에디터 검증 오류를 만든다.
- 대표 항목이 없으면 “대표 패시브 정보 없음” 상태를 표시한다. 전체 패시브의 첫 항목을 묵시적으로 대신 표시하지 않는다.

### 8.3 갱신 흐름

```text
SelectIndex(index)
  → PopulateDetail(index)
      → 기존 이름/무기/무기 효과 표시
      → PopulatePassives(entry.characterType)
          → 기존 Row 반환/삭제
          → Passive Set 조회
          → characterSelectRepresentatives 순서대로 최대 2개 Row 생성
```

카드 인덱스와 `CharacterSelectDatabaseSO.entries` 인덱스가 달라질 수 있는 현재 구조를 함께 수정해야 한다. 현재 `BuildCards()`는 null Entry를 건너뛰면서 카드에는 `_cards.Count`를 넘기지만 `PopulateDetail(index)`는 데이터베이스 인덱스로 사용한다. 패시브 작업 시 카드가 원본 `Entry` 또는 원본 데이터베이스 인덱스를 보유하도록 정리한다.

### 8.4 확정 이후

`Confirm()`은 기존처럼 `CharacterConfirmed(type)`만 발행한다. 호출 측은 `PartyManager.SetNewGameStartingCharacter(type)` 같은 신규 게임 시작 계약으로 캐릭터를 예약하고, 실제 PlayerActor 초기화 시 런타임 패시브를 적용한다.

---

## 9. 중첩과 수치 정책

### 9.1 상시 배율

동일 채널의 `Percent`는 합산 후 한 번 곱한다.

```text
multiplier = max(0, 1 + sum(percentValues))
```

현재 캐릭터 고유 패시브는 중복이 드물지만 장비·사이클 보너스가 같은 Reader에 합류할 수 있으므로 계산 순서를 고정한다.

```text
최종값 = (원본 + Flat 합) × (1 + Percent 합) × Multiply 곱
```

### 9.2 발동형 Effect

동일 발동형 패시브의 반복 성공은 `GameplayEffectSO.stackPolicy`를 따른다.

권장 기본값:

| 의도 | 정책 |
|------|------|
| 다시 성공하면 시간만 갱신 | `RefreshDuration` |
| 성공할수록 최대 N스택 | `AddStackAndRefresh` |
| 기존 버프 중 재발동 금지 | `RejectNew` |

발동형 Effect의 `stackingKey`는 캐릭터와 패시브를 구분할 수 있는 안정 키를 사용한다.

### 9.3 상한 권장

| 채널 | 권장 1차 범위 |
|------|---------------|
| 약·강·스킬 피해 | +10% ~ +20% |
| Break 피해 | +15% ~ +30% |
| 스킬 쿨다운 | -10% ~ -20% |
| 소비품 회복 | +20% ~ +35% |
| 제작 재료 | -10% ~ -25% |
| 경험치 | +10% ~ +20% |
| 상태이상 회복 속도 | +20% ~ +35% |
| 상태강화 지속시간 | +15% ~ +30% |
| 장비 옵션 +1 랭크 확률 | +10% ~ +20%p |

---

## 10. 세이브·로드와 결정성

- `PassiveAbilitySO`, `CharacterPassiveSetSO`, `CharacterPassiveDatabaseSO` 참조는 정적 게임 데이터다.
- 캐릭터가 어떤 고유 패시브를 갖는지는 저장하지 않는다.
- 장비 옵션은 획득 순간 결과를 `growthAttributeRolls`에 저장하므로 로드 시 행운을 다시 적용하지 않는다.
- 제작은 시작 시 비용이 즉시 차감되므로 캡처된 Quote를 별도 저장하지 않는다.
- 경험치 보너스는 지급 순간 정수 결과만 저장된다.
- 발동형 버프는 1차에서 `DoNotSave`, `RemoveOnSwap`을 권장한다.
- 추후 `SaveRemainingDuration`을 허용하면 패시브 Effect Resolver와 캐릭터별 상태 저장 검증을 먼저 추가한다.

---

## 11. 에디터와 검증

Ability Editor에 `패시브` 탭을 추가하거나 별도 `Passive Ability Validator`를 제공한다.

필수 검증:

1. `passiveId`가 비어 있거나 중복되지 않았는지.
2. `presentation.category == AbilityCategory.Passive`인지.
3. 캐릭터 타입이 데이터베이스에서 중복되지 않았는지.
4. 하나의 `CharacterPassiveSetSO`가 자신의 `characterType`과 맞는 항목에 연결됐는지.
5. null 패시브와 null Trigger Effect가 없는지.
6. `Always`인데 Modifier와 Effect가 모두 비어 있지 않은지.
7. Trigger형인데 `triggeredEffects`가 비어 있지 않은지.
8. 발동형 Effect가 `Duration`이고 지속시간이 0보다 큰지.
9. 상태강화/상태이상 지속시간 대상 Effect에 `polarity`가 지정됐는지.
10. 감소 배율이 0 미만의 최종값을 만들지 않는지.
11. `BattlePartyHighest`가 아닌 제작·행운 패시브에 경고하는지.
12. `UI_CharacterSelect`와 `PartyConfigSO`가 같은 Passive Database를 참조하는지.
13. 모든 선택 가능 캐릭터가 패시브 세트를 갖는지. 의도적 무패시브는 명시적 빈 Set을 사용한다.
14. `passives` 전체 목록에는 인위적인 개수 제한을 적용하지 않는지.
15. `characterSelectRepresentatives`가 2개를 초과하지 않는지.
16. 대표 패시브가 null·중복이 아니며 같은 세트의 `passives`에 포함돼 있는지.

---

## 12. 구현 단계

### Phase 1: 데이터·조회·UI

1. `PassiveAbilitySO`, `CharacterPassiveSetSO`, `CharacterPassiveDatabaseSO` 추가.
2. `PartyConfigSO`와 `UI_CharacterSelect`에 Database 참조 추가.
3. `PassiveModifierService`와 `IPassiveModifierReader` 추가.
4. `UIPassiveAbilityRow`와 상세 패널 표시 구현.
5. 캐릭터 선택 카드 인덱스와 DB Entry 인덱스 불일치 수정.
6. 패시브 데이터 검증기 추가.

완료 기준:

- 모든 캐릭터 카드에서 같은 SO 원본의 패시브 정보가 표시된다.
- 캐릭터가 패시브를 3개 이상 보유해도 런타임에는 전체가 적용되고 선택 화면에는 대표 2개만 표시된다.
- UI를 열고 닫는 것만으로 런타임 스탯이나 Effect가 바뀌지 않는다.
- 빈 세트와 잠긴 캐릭터 표시가 예외 없이 동작한다.

### Phase 2: 전투 패시브

1. 공격 종류별 피해와 Break 배율 적용.
2. Ability 쿨다운 유효 시간 계산과 HUD 동기화.
3. `DefenseSucceeded` 이벤트 추가.
4. `PassiveAbilityController`와 퍼펙트 회피·가드 Effect 적용.
5. 캐릭터 교체 시 Remove 정책 적용.

완료 기준:

- 약·강·스킬 배율이 서로 침범하지 않는다.
- `HitPhaseData`와 `GameplayAbilitySO` 에셋 값이 런타임에 변하지 않는다.
- HUD 쿨다운 총 시간과 실제 사용 가능 시점이 일치한다.
- 퍼펙트 성공 한 번에 Effect가 한 번만 발동한다.
- 잔류 공격은 스냅샷 생성 시점의 퇴장 캐릭터 배율을 유지한다.

### Phase 3: 생활·성장 패시브

1. 소비품 회복 보정.
2. `CraftCostQuote` 도입과 제작 전체 API 통합.
3. 수령 캐릭터별 경험치 보정.
4. 장비 옵션 랭크 행운 적용.
5. Beneficial/Harmful Effect 지속시간 보정.

완료 기준:

- 제작 UI 필요 수량, 제작 가능 판정, 실제 차감량이 같다.
- 파티원 한 명의 경험치 패시브가 다른 파티원에게 적용되지 않는다.
- 장비 옵션은 신규 획득 때만 추첨되고 로드 시 유지된다.
- 이로운 Effect 연장과 해로운 Effect 단축이 반대로 적용되지 않는다.

### Phase 4: 자동 테스트와 밸런스

1. 순수 배율 계산 EditMode 테스트.
2. 제작 Quote 경계값·반올림 테스트.
3. 경험치 캐릭터별 분배 테스트.
4. 장비 옵션 행운의 결정적 RNG 주입 테스트.
5. 퍼펙트 회피·가드 PlayMode 수직 슬라이스.
6. 교체·세이브·잔류 공격 회귀 테스트.
7. Ability Editor 전체 데이터 검증.

---

## 13. 테스트 시나리오

### 전투

- 약공격 +15% 캐릭터로 같은 적을 약공격하면 피해만 1.15배가 되고 강공격은 변하지 않는다.
- 강공격 필터에서 `ChargeAttack`을 제외하면 차지 공격은 증가하지 않는다.
- 스킬 피해 +20%는 `SkillAttack`에만 적용되고 처형과 SpecialBreak에는 적용되지 않는다.
- Break +25%는 HP 피해와 Poise 피해를 바꾸지 않고 `breakDamage`만 증가시킨다.
- 쿨다운 -20% 스킬의 HUD 총 시간과 실제 재사용 시간이 모두 0.8배다.

### 방어 Trigger

- 퍼펙트 회피 성공 시 지정 Effect가 한 번 적용된다.
- 회피 창 밖의 대시 연출은 패시브를 발동하지 않는다.
- 퍼펙트 가드 성공 시 Effect가 발동하지만 일반 가드는 발동하지 않는다.
- `RefreshDuration` Effect는 재발동 시 스택을 늘리지 않고 시간만 갱신한다.
- 버프 중 캐릭터 교체 시 1차 정책대로 즉시 제거된다.

### 아이템·제작·성장

- 회복량 +30%에서 Flat 100 포션은 130, Percent 20% 포션은 최대 HP의 26%를 회복한다.
- 재료 3개에 비용 -20%를 적용하면 `ceil(2.4) = 3`개, 재료 5개면 4개를 소모한다.
- 제작 UI 표시와 실제 인벤토리 차감이 일치한다.
- 경험치 +20% 캐릭터와 일반 캐릭터가 함께 100 EXP를 받으면 각각 120, 100을 받는다.
- 행운 패시브는 장비 옵션 개수를 늘리지 않고 랭크만 최대 +1 보정한다.
- 세이브 로드 후 장비 옵션이 다시 추첨되지 않는다.

### UI

- 카드 선택 변경마다 패시브 Row가 이전 캐릭터 데이터 없이 완전히 갱신된다.
- 전체 패시브가 0개, 1개, 2개, 3개 이상인 세트에서 대표 목록에 지정된 최대 2개만 표시된다.
- 대표 목록 순서를 변경하면 선택 화면의 표시 순서만 바뀌고 런타임 적용 결과는 바뀌지 않는다.
- 대표 목록에 없는 패시브도 런타임에는 정상 적용된다.
- null Entry가 포함돼도 카드 클릭과 상세 데이터 인덱스가 어긋나지 않는다.
- 패시브가 없는 캐릭터는 명시적 빈 상태 문구를 표시한다.
- 캐릭터 선택 UI를 반복해서 열어도 패시브 Effect가 중복 적용되지 않는다.

---

## 14. 코드 변경 후보

| 파일 | 변경 내용 |
|------|-----------|
| `Data/Ability/Passive/PassiveAbilitySO.cs` | 패시브 정의와 Modifier 데이터 |
| `Data/Ability/Passive/CharacterPassiveSetSO.cs` | 캐릭터별 패시브 목록 |
| `Data/Ability/Passive/CharacterPassiveDatabaseSO.cs` | 캐릭터 타입 매핑 |
| `Data/Ability/GameplayEffectSO.cs` | Effect polarity와 지속시간 보정 제외 플래그 |
| `Data/Party/PartyConfigSO.cs` | 런타임 Passive Database 참조 |
| `Contracts/GameServices.cs` | `IPassiveModifierReader` 계약 |
| `Manager/Party/PartyManager.cs` | 패시브 조회, 경험치 수령자별 보정 |
| `GameActor/Gameplay/Passive/PassiveAbilityController.cs` | 활성 세트와 Trigger Effect |
| `GameActor/Object/Player/PlayerActor.Combat.cs` | 방어 성공 이벤트 |
| `GameActor/State/Player/PlayerGuardState.cs` | 퍼펙트 가드 성공 발행 |
| `GameActor/Component/Player/PlayerCombat.Attack.cs` | 공격 종류별 피해·Break 배율 캡처 |
| `GameActor/Gameplay/Ability/ActorAbilitySystem.cs` | 유효 쿨다운 계산과 UI 상태 공유 |
| `GameActor/Gameplay/Effect/GameplayEffectController.cs` | Effect polarity별 유효 지속시간 |
| `Manager/Item/InventoryManager.cs` | 소비품 회복과 장비 옵션 행운 |
| `Manager/Crafting/RecipeManager.cs` | 공통 `CraftCostQuote` 적용 |
| `UI/Scene/CharacterSelect/UI_CharacterSelect.cs` | 패시브 상세 목록 표시 |
| `UI/Scene/CharacterSelect/UIPassiveAbilityRow.cs` | 패시브 표시 Row |
| `Data/Editor/Ability/AbilityDataValidator.cs` | 패시브 데이터 검증 |

---

## 15. 구현 전 확정할 기획값

구조는 위 계약으로 진행할 수 있으나 실제 데이터 작성 전 다음 값은 캐릭터별로 정해야 한다.

1. 강공격 강화에 `ChargeAttack`을 포함할 캐릭터.
2. 스킬 쿨다운 감소에 Ultimate를 포함할지 여부.
3. 퍼펙트 회피·가드 버프가 강화할 `StatType`, 수치, 지속시간, 중첩 정책.
4. 잠긴 캐릭터의 패시브 정보를 공개할지 여부.
5. 장비 옵션 행운의 최대 보정 확률.

권장 1차 기본값:

- 캐릭터당 패시브 총개수는 무제한.
- `UI_CharacterSelect`에는 명시적으로 지정한 대표 패시브 최대 2개만 표시.
- 강공격 강화는 `HeavyAttack`만 적용하고 차지는 명시적으로 포함.
- 쿨다운 감소는 일반 Ability 슬롯만 적용.
- 퍼펙트 방어 버프는 `RefreshDuration`, 5초, 중첩 없음.
- 잠긴 캐릭터도 패시브 이름과 설명은 공개해 해금 동기를 제공.
- 제작·행운은 출전 파티 최고값 하나만 적용.

---

## 16. 결론

패시브 어빌리티는 기존 `GameplayAbilitySO`에 실행 불가능한 Ability를 억지로 추가하는 방식보다, 전용 정의와 공통 Modifier 조회 계약으로 분리하는 편이 현재 구조에 맞다.

핵심 원칙은 다음과 같다.

1. 캐릭터 패시브 데이터는 UI와 런타임이 공유하는 단일 SO 원본을 사용한다.
2. UI는 표시와 선택만 담당하고 실제 패시브 적용은 PlayerActor·Party 서비스가 담당한다.
3. 조건부 공격 보정은 전역 스탯이 아니라 `AttackKind` 문맥에서 적용한다.
4. 퍼펙트 방어 버프는 기존 Gameplay Effect 수명주기를 재사용한다.
5. 제작·경험치·아이템 옵션은 각 도메인의 최종 계산 경계에서 한 번만 보정한다.
6. UI 미리보기, 가능 판정, 실제 소비가 동일한 계산 결과를 사용해야 한다.
