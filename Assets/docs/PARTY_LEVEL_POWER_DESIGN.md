# 파티 레벨 / 전투력 계산 시스템 설계

> 작성일: 2026-05-03
> 대상 버전: Unity 6 (6000.0.60f1), URP
> 상태: 설계 단계 (구현 전)
> 관련 문서: [party-formation-system.md](./party-formation-system.md), [STAT_SYSTEM_GUIDE.md](./STAT_SYSTEM_GUIDE.md)

---

## 1. 배경

현재 파티 시스템은 `CharacterActorType` 기준으로 보유(`Roster`)와 출전(`BattleOrder`)을 관리하고, 실제 조작은 단일 `PlayerActor`와 `PlayerSwapBehaviour` 모델 교체로 처리한다.

스탯은 `ActorStatSO`와 `ActorStatContainer`가 담당하지만, 플레이어블 캐릭터의 성장 상태는 아직 별도 데이터로 관리되지 않는다. 앞으로는 개별 캐릭터가 레벨을 가지고, 몬스터 처치로 얻는 성장 재화를 사용해 레벨을 올리는 구조가 필요하다.

본 문서는 1차 범위를 **능력치에 따른 전투력 수치 계산**으로 제한한다. 재화 획득, 강화 UI, 저장/로드, 실제 레벨업 소비 처리는 후속 단계에서 다룬다.

---

## 2. 목표

- `CharacterActorType`별 레벨을 보유한다.
- 레벨에 따라 기본 스탯을 성장시킨 최종 성장 스탯을 계산한다.
- 성장 스탯을 기반으로 전투력(`CombatPower`)을 산출한다.
- 전투력 계산은 UI, 밸런싱, 디버그 도구에서 재사용 가능한 순수 계산 API로 둔다.
- 기존 `ActorStatSO` / `ActorStatContainer` 구조를 유지하고, 레벨 계산 레이어를 그 위에 얹는다.

비목표:

- 몬스터 처치 재화 드랍 구현
- 레벨업 비용/소비 처리
- 세이브 데이터 직렬화
- 플레이어 데미지 공식 변경
- 장비/버프까지 포함한 실시간 전투력 표시

---

## 3. 용어 정의

| 용어 | 정의 |
|------|------|
| **Actor Level** | `CharacterActorType`별 성장 레벨. `PlayerActor` 인스턴스가 아니라 파티 멤버 데이터에 귀속된다. |
| **Base Stat** | `ActorStatSO`에 정의된 레벨 1 기준 기본 스탯. |
| **Growth Stat** | Base Stat에 레벨 성장률을 적용한 스탯. |
| **Runtime Stat** | `ActorStatContainer`가 버프/디버프/장비 수정자까지 포함해 계산하는 런타임 최종 스탯. |
| **CombatPower** | Growth Stat을 가중합해 만든 비교용 전투력 수치. |
| **Growth Currency** | 몬스터 처치로 얻어 레벨 강화에 쓰는 재화. 본 문서에서는 수량 저장과 소비를 설계 범위 밖으로 둔다. |

---

## 4. 전체 구조

```
PartyManager
├── Roster / BattleOrder
└── PartyProgressionState
    └── CharacterActorType별 level

PartyMemberGrowthSO
├── CharacterActorType
├── ActorStatSO baseStat
├── levelCap
└── StatGrowthRule[]

PartyPowerCalculator
├── CalculateGrowthStats(type, level)
└── CalculateCombatPower(growthStats)

UI_PartySelect / HUD / 디버그 도구
└── PartyManager.GetCombatPower(type)
```

역할 분리:

| 계층 | 역할 |
|------|------|
| `ActorStatSO` | 레벨 1 기준 기본 스탯 값 |
| `PartyMemberGrowthSO` | 캐릭터별 성장 곡선과 레벨 상한 |
| `PartyProgressionState` | 캐릭터별 현재 레벨 |
| `PartyPowerCalculator` | 성장 스탯과 전투력 계산 |
| `ActorStatContainer` | 런타임 액터에 적용된 최종 스탯 계산 |

---

## 5. 데이터 모델

### 5.1 PartyMemberGrowthSO 신규

캐릭터별 성장 규칙을 정의하는 ScriptableObject.

```csharp
[CreateAssetMenu(fileName = "PartyMemberGrowth_", menuName = "UPlayGround/Party/Party Member Growth")]
public class PartyMemberGrowthSO : ScriptableObject
{
    public CharacterActorType characterType;
    public ActorStatSO baseStat;

    [Min(1)] public int initialLevel = 1;
    [Min(1)] public int levelCap = 100;

    public List<StatGrowthRule> growthRules = new();
}
```

| 필드 | 설명 |
|------|------|
| `characterType` | 성장 데이터를 적용할 캐릭터 타입 |
| `baseStat` | 레벨 1 기준 스탯. 기존 `ActorStatSO` 재사용 |
| `initialLevel` | 최초 합류 시 레벨 |
| `levelCap` | 캐릭터별 레벨 상한 |
| `growthRules` | `StatType`별 성장 공식 |

### 5.2 StatGrowthRule 신규

```csharp
[Serializable]
public struct StatGrowthRule
{
    public StatType statType;
    public GrowthFormula formula;
    public float flatPerLevel;
    public float percentPerLevel;
    public AnimationCurve curve;
}

public enum GrowthFormula
{
    Flat,
    Percent,
    Curve
}
```

계산 기준:

```csharp
int levelDelta = Mathf.Max(0, level - 1);

Flat:
  value = baseValue + flatPerLevel * levelDelta

Percent:
  value = baseValue * (1f + percentPerLevel * levelDelta)

Curve:
  value = baseValue * curve.Evaluate(normalizedLevel)
```

`Curve`의 `normalizedLevel`은 `level`을 `1..levelCap` 기준으로 0..1 정규화한 값이다.

### 5.3 PartyProgressionState 신규

런타임에서 캐릭터별 레벨을 보관하는 순수 상태 객체.

```csharp
[Serializable]
public class PartyProgressionState
{
    private readonly Dictionary<CharacterActorType, int> _levels = new();

    public int GetLevel(CharacterActorType type);
    public void SetLevel(CharacterActorType type, int level);
    public bool HasLevel(CharacterActorType type);
}
```

1차 구현에서는 `PartyManager` 내부 상태로 두고, 저장/로드 연동 시 `SaveManager`가 직렬화 가능한 DTO로 옮긴다.

### 5.4 PartyConfigSO 확장

`PartyConfigSO`는 파티 구성과 초기 성장 데이터 참조를 함께 갖는다.

```csharp
public class PartyConfigSO : ScriptableObject
{
    public List<CharacterActorType> partyOrder = new();
    public int maxBattleSize = 4;
    public List<CharacterActorType> defaultBattleOrder = new();
    public int startActiveIndex = 0;

    public List<PartyMemberGrowthSO> growthData = new();
}
```

`growthData`는 `CharacterActorType` 중복이 없어야 한다. 누락된 캐릭터는 전투력 계산에서 `0`을 반환하고 경고 로그를 남긴다.

---

## 6. 전투력 계산

### 6.1 계산 대상 스탯

1차 전투력은 현재 `StatType` 중 전투 영향도가 명확한 항목만 사용한다.

| StatType | 반영 | 이유 |
|----------|------|------|
| `MaxHealth` | O | 생존력 |
| `AttackPower` | O | 직접 피해량 배율 |
| `Defense` | O | 받는 피해 감소 |
| `CritRate` | O | 기대 피해량 |
| `CritMultiplier` | O | 기대 피해량 |
| `MaxPoise` | O | 경직 저항 |
| `SkillGaugeRate` | O | 스킬 회전율 |
| `MoveSpeed` | 선택 | 회피/접근 유틸리티. 가중치는 낮게 |
| `HealthRegenRate` | 제외 | 전투 지속 시간 모델이 필요함 |
| `DashDistance` | 제외 | 조작감/상태별 의존도가 큼 |
| `PoiseRecoveryRate` | 제외 | MaxPoise와 중복 반영 위험 |
| `PoiseRecoveryDelay` | 제외 | 낮을수록 좋은 역방향 스탯이라 1차 제외 |
| `InvincibleDuration` | 제외 | 상태/스킬별 의미가 달라 1차 제외 |

### 6.2 권장 공식

전투력은 공격 기대값과 생존 기대값의 합으로 계산한다.

```csharp
float effectiveAttack =
    attackPower
    * (1f + critRate * Mathf.Max(0f, critMultiplier - 1f))
    * skillGaugeRate;

float effectiveHealth =
    maxHealth / Mathf.Max(0.1f, 1f - Mathf.Clamp01(defense));

float utility =
    maxPoise * 0.25f
    + Mathf.Max(0f, moveSpeed - 1f) * 100f;

float combatPower =
    effectiveHealth * 0.35f
    + effectiveAttack * 100f * 0.55f
    + utility * 0.10f;
```

특징:

- `Defense`는 현재 데미지 공식과 동일하게 0..1 감소율로 해석한다.
- `AttackPower`는 `HitPhaseData.damage`에 곱해지는 배율이므로 기준값 1.0을 100점 단위로 환산한다.
- 치명타는 기대값으로만 반영한다.
- `MoveSpeed`는 1.0 초과분만 낮은 가중치로 반영한다.

### 6.3 API 형태

```csharp
public readonly struct PartyCombatPowerResult
{
    public CharacterActorType characterType { get; }
    public int level { get; }
    public float combatPower { get; }
    public IReadOnlyDictionary<StatType, float> growthStats { get; }
}
```

```csharp
public static class PartyPowerCalculator
{
    public static Dictionary<StatType, float> CalculateGrowthStats(
        PartyMemberGrowthSO growthData,
        int level);

    public static float CalculateCombatPower(
        IReadOnlyDictionary<StatType, float> stats);
}
```

`PartyManager` 공개 API:

```csharp
public int GetLevel(CharacterActorType type);
public bool SetLevelForDebug(CharacterActorType type, int level);
public PartyCombatPowerResult GetCombatPower(CharacterActorType type);
public IReadOnlyList<PartyCombatPowerResult> GetBattleOrderCombatPowers();
```

`SetLevelForDebug`는 1차 구현에서 밸런싱 검증용으로만 둔다. 실제 강화는 후속 `TryLevelUp(type)` API로 분리한다.

---

## 7. 런타임 적용 정책

1차 구현은 전투력 계산만 가능하게 한다. 따라서 성장 스탯을 실제 `PlayerActor`의 데미지/체력 공식에 적용하지 않아도 된다.

다만 후속 적용을 고려해 다음 정책을 유지한다.

| 항목 | 1차 정책 | 후속 정책 |
|------|----------|-----------|
| 전투력 계산 | `PartyPowerCalculator` 결과만 사용 | 동일 |
| 실제 HP 최대값 | 기존 `CharacterModelData.maxHealth` 유지 | Growth Stat의 `MaxHealth`로 교체 |
| 실제 공격력 | 기존 `ActorStatContainer.AttackPower` 유지 | 캐릭터 교체 시 Growth Stat을 `ActorStatContainer` base로 주입 |
| UI 표시 | 레벨/전투력 표시 가능 | 실제 스탯 상세 표시 |
| 저장 | 미지원 | `PartyProgressionState` 저장 |

후속 런타임 적용 시 `PlayerActor.RefreshForCharacter(CharacterModelData data)`에서 캐릭터 타입을 받은 뒤, `PartyManager`가 계산한 Growth Stat을 `ActorStatContainer.SetBase` 또는 별도 `InitFromDictionary` 방식으로 반영한다.

---

## 8. 초기화 흐름

```
PartyManager.AfterInit()
  ├─ BuildPartyFromScene()
  ├─ BuildGrowthLookup(config.growthData)
  ├─ InitializeProgressionLevels()
  │   ├─ Roster 캐릭터별 growthData.initialLevel 적용
  │   └─ growthData 누락 시 level 1 폴백
  ├─ InitializePartyStates()
  └─ UI/디버그 도구에서 GetCombatPower(type) 호출 가능
```

신규 합류 시:

```
UnlockCharacter(type)
  ├─ Roster 추가
  ├─ growthData.initialLevel 로 레벨 초기화
  ├─ OnRosterChanged
  └─ OnPartyProgressionChanged(type) 신규 이벤트 발화
```

이벤트:

```csharp
public event Action<CharacterActorType> OnPartyProgressionChanged;
```

레벨 또는 성장 데이터가 바뀐 캐릭터의 전투력을 다시 계산해야 하는 UI가 구독한다.

---

## 9. 에디터 / 밸런싱 도구

1차 구현에 필요한 최소 도구:

| 도구 | 기능 |
|------|------|
| `PartyMemberGrowthSO` 인스펙터 | 성장 규칙 편집 |
| `Party Power Preview` 에디터 창 | 캐릭터, 레벨 입력 → 성장 스탯/전투력 미리보기 |
| 검증 메뉴 | `PartyConfigSO.growthData` 중복/누락 검사 |

권장 메뉴:

```
UPlayGround/Party/Party Power Preview
UPlayGround/Party/Validate Party Growth Data
```

검증 규칙:

- `CharacterActorType.None` 금지
- `growthData.characterType` 중복 금지
- `baseStat` 누락 금지
- `levelCap < initialLevel` 금지
- `growthRules.statType` 중복 금지
- `Curve` 공식인데 `curve` 키가 비어 있으면 경고

---

## 10. 구현 단계

### Phase A — 계산 데이터와 순수 계산기

1. `PartyMemberGrowthSO`, `StatGrowthRule`, `GrowthFormula` 추가.
2. `PartyPowerCalculator.CalculateGrowthStats()` 구현.
3. `PartyPowerCalculator.CalculateCombatPower()` 구현.
4. 계산기 단위 테스트 또는 에디터 검증용 샘플 케이스 작성.

### Phase B — PartyManager 연동

1. `PartyConfigSO.growthData` 추가.
2. `PartyManager`에 `PartyProgressionState`와 growth lookup 추가.
3. `GetLevel`, `GetCombatPower`, `GetBattleOrderCombatPowers` 공개 API 추가.
4. `UnlockCharacter` 시 초기 레벨 등록.
5. `OnPartyProgressionChanged` 이벤트 추가.

### Phase C — 표시/검증

1. `UI_PartySelect` 또는 파티 메뉴에 레벨/전투력 표시.
2. `Party Power Preview` 에디터 창 추가.
3. `Validate Party Growth Data` 메뉴 추가.

### Phase D — 후속 성장 시스템

1. 몬스터 처치 시 Growth Currency 획득.
2. 레벨업 비용 테이블 추가.
3. `TryLevelUp(CharacterActorType type)` 구현.
4. `SaveManager`에 캐릭터별 레벨/재화 저장.
5. Growth Stat을 실제 `PlayerActor` 런타임 스탯에 반영.

---

## 11. 결정 사항

| # | 결정 |
|---|------|
| 1 | 레벨은 `PlayerActor` 인스턴스가 아니라 `CharacterActorType`별 파티 진행 상태로 관리한다. |
| 2 | `ActorStatSO`는 레벨 1 기준 Base Stat으로 재사용한다. |
| 3 | 성장 규칙은 별도 `PartyMemberGrowthSO`로 분리한다. |
| 4 | 1차 구현은 전투력 계산 API까지만 제공하고 실제 전투 공식에는 적용하지 않는다. |
| 5 | 전투력은 Growth Stat 기준이며 버프/장비/일시 수정자는 제외한다. |
| 6 | 몬스터 처치 재화와 레벨업 비용은 후속 단계에서 구현한다. |

---

## 12. 주의 사항

- `CharacterModelData.maxHealth`와 Growth Stat의 `MaxHealth`가 일시적으로 공존한다. 실제 런타임 적용 전까지는 UI에 "전투력 기준 스탯"과 "현재 전투 체력"이 섞여 보이지 않도록 한다.
- `Defense`는 1에 가까워질수록 `effectiveHealth`가 급격히 커진다. `ActorStatSOEditor`에서 권장 상한을 낮게 잡거나 전투력 계산에서 별도 캡을 둔다.
- `CritRate`는 0..1 범위를 벗어나면 전투력 왜곡이 크다. 계산기에서 `Mathf.Clamp01` 처리한다.
- `MoveSpeed`는 실제 상태별 속도 적용이 아직 완전히 연결되지 않았으므로 낮은 가중치로만 반영한다.
- `growthData` 누락 캐릭터가 있어도 게임 시작을 막지는 말고, 전투력 계산 결과만 `0`과 경고로 처리한다.
