# 액터 스탯 시스템 설계 가이드

## 개요

모든 `GameActor` (플레이어·몬스터·NPC)에 적용 가능한 **통합 스탯 시스템**입니다.

### 설계 목적

| 문제 | 해결책 |
|------|--------|
| `PlayerActor`, `MonsterActor`가 각자 `_maxHealth` 필드 중복 보유 | `ActorStatContainer` 컴포넌트로 통합 |
| 장비·버프에 의한 스탯 수정자 시스템 부재 | `StatModifier` 레이어 추가 |
| 스탯 변화 이벤트가 HP에만 존재 (`OnHealthChanged`) | 모든 스탯에 `OnStatChanged` 범용 이벤트 |
| `PoiseStat`만 컴포넌트화, 나머지는 난립 | `PoiseStat`을 포함해 모든 스탯을 컨테이너 하나로 조회 |

### 설계 원칙

- **컴포넌트 패턴** — `ActorStatContainer`는 `GameActor`에 자동 추가되는 MonoBehaviour
- **SO 기반 기본값** — `ActorStatSO`가 베이스 스탯을 정의, 코드 변경 없이 밸런싱
- **레이어드 계산** — Base → Flat 가산 → Percent 가산 → 배율 순으로 최종값 산출
- **하위 호환** — 기존 `EnemyStatsSO`, `PoiseStat` 코드를 즉시 제거하지 않고 단계적 마이그레이션

---

## 아키텍처

```
┌──────────────────────────────────────────────────────────────┐
│                        GameActor                             │
│                                                              │
│   Stats ──► ActorStatContainer (MonoBehaviour)              │
│              ├── _baseStats: Dict<StatType, float>          │
│              ├── _modifiers: List<TimedModifier>            │
│              └── GetFinalStat(StatType) → float             │
└──────────────────────────────────────────────────────────────┘
        ▲                    ▲                    ▲
   PlayerActor          MonsterActor          NpcActor
   (HP → Stats.MaxHealth)  (Stats.Init(SO))   (속도 등)
        │                    │
        │              ActorStatSO
        │              └── List<StatEntry>
        │
   PlayerStatSO  (캐릭터별 기본 스탯)
```

```
런타임 계산 파이프라인:

ActorStatSO.baseValue
       │
  + StatModifier(Flat)          ← 장비 추가 공격력 (+15)
       │
  × (1 + StatModifier(Percent)) ← 버프 (+30%)
       │
  × StatModifier(Multiply)      ← 최종 배율 (위상 변환 등)
       │
  = GetFinalStat()
```

---

## 스탯 타입 정의

```csharp
// Assets/02.Scripts/Data/Stat/StatType.cs

namespace UPlayGround.Data.Stat
{
    public enum StatType
    {
        // ── 생존 ──────────────────────────────
        MaxHealth,          // 최대 체력
        HealthRegenRate,    // 초당 자연 회복량 (0이면 미적용)

        // ── 전투 ──────────────────────────────
        AttackPower,        // 공격력 배율 (1.0 = 기본, HitPhaseData.damage에 곱해짐)
        Defense,            // 방어 계수 (0~1, 받는 피해 감소율 계산에 사용)
        CritRate,           // 치명타 확률 (0.0~1.0)
        CritMultiplier,     // 치명타 데미지 배율 (기본 1.5)

        // ── 이동 ──────────────────────────────
        MoveSpeed,          // 이동속도 배율 (1.0 = 기본)
        DashDistance,       // 대시 거리 배율

        // ── 강인도 ────────────────────────────
        MaxPoise,           // 최대 Poise
        PoiseRecoveryRate,  // 초당 Poise 회복량
        PoiseRecoveryDelay, // Poise 회복 대기 시간

        // ── 스킬 ──────────────────────────────
        SkillGaugeRate,     // 스킬 게이지 충전 속도 배율
        InvincibleDuration, // 무적 시간 배율 (대시 무적 등)
    }
}
```

---

## 데이터 구조

### StatModifier

```csharp
// Assets/02.Scripts/Data/Stat/StatModifier.cs

using System;
using UnityEngine;

namespace UPlayGround.Data.Stat
{
    public enum ModifierType
    {
        Flat,       // finalValue += value
        Percent,    // finalValue *= (1 + value)  — 0.1 = +10%
        Multiply,   // finalValue *= value         — 직접 배율 (드물게 사용)
    }

    [Serializable]
    public struct StatModifier
    {
        public StatType    statType;
        public ModifierType modifierType;
        public float       value;

        // 제거 시 출처로 식별. 장비 SO 인스턴스, 버프 ID 문자열 등을 넣는다.
        public object source;

        // -1 = 영구 (장비 장착 등), 0 초과 = 남은 지속 시간(초)
        public float duration;

        public StatModifier(StatType type, ModifierType mod, float val, object src, float dur = -1f)
        {
            statType     = type;
            modifierType = mod;
            value        = val;
            source       = src;
            duration     = dur;
        }
    }
}
```

### ActorStatSO

```csharp
// Assets/02.Scripts/Data/Stat/ActorStatSO.cs

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Stat
{
    [CreateAssetMenu(fileName = "ActorStat_", menuName = "UPlayGround/Stat/Actor Stat SO")]
    public class ActorStatSO : ScriptableObject
    {
        [Serializable]
        public struct StatEntry
        {
            public StatType statType;
            public float    baseValue;
        }

        [SerializeField] private List<StatEntry> _stats = new();

        // 기본값이 정의되지 않은 스탯의 폴백 — 스탯 타입별로 직접 지정
        private static readonly Dictionary<StatType, float> _defaults = new()
        {
            { StatType.MaxHealth,          100f },
            { StatType.AttackPower,        1.0f },
            { StatType.Defense,            0.0f },
            { StatType.CritRate,           0.0f },
            { StatType.CritMultiplier,     1.5f },
            { StatType.MoveSpeed,          1.0f },
            { StatType.DashDistance,       1.0f },
            { StatType.MaxPoise,           100f },
            { StatType.PoiseRecoveryRate,  40f  },
            { StatType.PoiseRecoveryDelay, 2.0f },
            { StatType.SkillGaugeRate,     1.0f },
            { StatType.InvincibleDuration, 1.0f },
        };

        public float GetBase(StatType type)
        {
            foreach (var entry in _stats)
                if (entry.statType == type) return entry.baseValue;

            return _defaults.TryGetValue(type, out float def) ? def : 0f;
        }

#if UNITY_EDITOR
        // 에디터에서 Inspector 정렬용
        private void OnValidate()
        {
            _stats.Sort((a, b) => a.statType.CompareTo(b.statType));
        }
#endif
    }
}
```

---

## 핵심 컴포넌트: ActorStatContainer

```csharp
// Assets/02.Scripts/GameActor/Component/Common/ActorStatContainer.cs

using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Stat;

namespace UPlayGround.Component
{
    /// <summary>
    /// 모든 GameActor의 런타임 스탯 컨테이너.
    /// GameActor.Awake()에서 GetOrAddComponent로 자동 부착된다.
    /// </summary>
    public class ActorStatContainer : MonoBehaviour
    {
        // 기본값 (ActorStatSO에서 Init 전까지 사용)
        private readonly Dictionary<StatType, float> _baseStats    = new();
        private readonly List<TimedModifier>         _modifiers    = new();
        private readonly Dictionary<StatType, float> _cachedFinals = new();
        private bool _cacheDirty = true;

        /// <summary>
        /// 스탯이 변경될 때 발화. (StatType, newFinalValue)
        /// </summary>
        public event Action<StatType, float> OnStatChanged;

        // ── 편의 프로퍼티 ─────────────────────────────────────────
        public float MaxHealth      => GetFinalStat(StatType.MaxHealth);
        public float AttackPower    => GetFinalStat(StatType.AttackPower);
        public float Defense        => GetFinalStat(StatType.Defense);
        public float CritRate       => GetFinalStat(StatType.CritRate);
        public float CritMultiplier => GetFinalStat(StatType.CritMultiplier);
        public float MoveSpeed      => GetFinalStat(StatType.MoveSpeed);
        public float MaxPoise       => GetFinalStat(StatType.MaxPoise);

        // ── 초기화 ────────────────────────────────────────────────

        /// <summary>
        /// ActorStatSO로 기본값 전체 교체. SetDefinition 시점에 호출.
        /// </summary>
        public void Init(ActorStatSO statSO)
        {
            _baseStats.Clear();
            if (statSO != null)
            {
                foreach (StatType type in Enum.GetValues(typeof(StatType)))
                    _baseStats[type] = statSO.GetBase(type);
            }
            InvalidateCache();
        }

        /// <summary>
        /// 특정 스탯 기본값만 직접 설정 (SO 없이 레거시 필드 값 주입 시 사용).
        /// </summary>
        public void SetBase(StatType type, float value)
        {
            _baseStats[type] = value;
            InvalidateCache(type);
        }

        // ── 최종값 조회 ───────────────────────────────────────────

        public float GetFinalStat(StatType type)
        {
            if (_cacheDirty) RebuildCache();
            return _cachedFinals.TryGetValue(type, out float v) ? v : GetBase(type);
        }

        private float GetBase(StatType type)
            => _baseStats.TryGetValue(type, out float v) ? v : 0f;

        // ── 수정자 관리 ───────────────────────────────────────────

        public void AddModifier(StatModifier modifier)
        {
            _modifiers.Add(new TimedModifier(modifier));
            InvalidateCache(modifier.statType);
        }

        /// <summary>
        /// source 오브젝트가 부착한 모든 수정자 제거.
        /// 장비 해제, 버프 만료 시 호출.
        /// </summary>
        public void RemoveModifiersBySource(object source)
        {
            bool removed = false;
            for (int i = _modifiers.Count - 1; i >= 0; i--)
            {
                if (_modifiers[i].Modifier.source == source)
                {
                    InvalidateCache(_modifiers[i].Modifier.statType);
                    _modifiers.RemoveAt(i);
                    removed = true;
                }
            }
            if (removed) RebuildCache();
        }

        public void RemoveAllModifiers()
        {
            _modifiers.Clear();
            InvalidateCache();
        }

        // ── 내부 계산 ─────────────────────────────────────────────

        private void RebuildCache()
        {
            _cachedFinals.Clear();
            foreach (StatType type in Enum.GetValues(typeof(StatType)))
                _cachedFinals[type] = ComputeFinal(type);
            _cacheDirty = false;
        }

        private float ComputeFinal(StatType type)
        {
            float flat    = 0f;
            float percent = 0f;
            float multiply = 1f;

            foreach (var tm in _modifiers)
            {
                var m = tm.Modifier;
                if (m.statType != type) continue;
                switch (m.modifierType)
                {
                    case ModifierType.Flat:     flat    += m.value; break;
                    case ModifierType.Percent:  percent += m.value; break;
                    case ModifierType.Multiply: multiply *= m.value; break;
                }
            }

            float result = (GetBase(type) + flat) * (1f + percent) * multiply;
            return result;
        }

        private void InvalidateCache(StatType? type = null)
        {
            _cacheDirty = true;
            // 단일 스탯만 갱신해도 되지만, 전체 재계산으로 단순화
        }

        // ── 시간 제한 수정자 업데이트 ────────────────────────────

        private void Update()
        {
            bool changed = false;
            for (int i = _modifiers.Count - 1; i >= 0; i--)
            {
                if (_modifiers[i].Modifier.duration < 0f) continue; // 영구

                _modifiers[i].RemainingTime -= Time.deltaTime;
                if (_modifiers[i].RemainingTime <= 0f)
                {
                    StatType affectedType = _modifiers[i].Modifier.statType;
                    _modifiers.RemoveAt(i);
                    InvalidateCache(affectedType);
                    changed = true;
                }
            }
            if (changed)
            {
                RebuildCache();
                // 변경된 스탯 타입별로 이벤트 발화 (생략 가능 - 전체 갱신으로 대체)
                foreach (StatType type in Enum.GetValues(typeof(StatType)))
                    OnStatChanged?.Invoke(type, GetFinalStat(type));
            }
        }

        // ── 내부 헬퍼 클래스 ─────────────────────────────────────

        private class TimedModifier
        {
            public StatModifier Modifier;
            public float        RemainingTime;

            public TimedModifier(StatModifier m)
            {
                Modifier      = m;
                RemainingTime = m.duration;
            }
        }
    }
}
```

---

## GameActor 통합

`GameActor.Awake()`에 한 줄 추가하는 것으로 모든 액터가 `Stats` 프로퍼티를 갖게 됩니다.

```csharp
// GameActor.cs 수정 (Awake 내부)

public ActorStatContainer Stats { get; private set; }

protected virtual void Awake()
{
    Tags = gameObject.GetOrAddComponent<GameplayTagContainer>();
    Stats = gameObject.GetOrAddComponent<ActorStatContainer>(); // ← 추가
    MovementController = GetComponent<ActorMovementController>();
    // ... 기존 코드 유지
}
```

---

## MonsterActor 통합

```csharp
// MonsterActor.cs — SetDefinition 수정
public override void SetDefinition(ActorDefinitionSO definition)
{
    base.SetDefinition(definition);
    if (definition == null) return;

    // 기존: EnemyStatsSO 직접 참조
    if (definition.stats != null)
    {
        _stats     = definition.stats;
        _maxHealth = _stats.maxHealth;
    }

    // 신규: ActorStatSO가 있으면 컨테이너로 초기화 (우선 적용)
    if (definition.statData != null)
    {
        Stats.Init(definition.statData);
        _maxHealth = Stats.MaxHealth; // 컨테이너에서 읽도록 동기화
    }

    _currentHealth = _maxHealth;
    // ...
}

// TakeDamage — 방어 계산 추가
public void TakeDamage(AttackData attackData)
{
    // ...기존 가드 체크...

    float rawDamage   = attackData.damage;
    float atkPower    = attackData.attacker?.Stats.AttackPower ?? 1f;
    float defRate     = Stats.Defense; // 0.0~1.0

    // 데미지 공식: raw × 공격력배율 × (1 - 방어율)
    float finalDamage = rawDamage * atkPower * (1f - defRate);

    if (attackData.criticalMultiplier > 1f)
        finalDamage *= attackData.criticalMultiplier;

    _currentHealth = MathF.Max(0, _currentHealth - finalDamage);
    // ...
}
```

---

## PlayerActor 통합

```csharp
// PlayerActor.cs — 기존 SerializeField 제거 후 Stats 사용

// 제거 대상:
// [SerializeField] private float _maxHealth = 100f;
// [SerializeField] private float _currentHealth = 100f;

// 대신 Stats.MaxHealth / _currentHealth (런타임 현재값은 별도 유지)
private float _currentHealth; // 최대값은 Stats.MaxHealth

protected override void Awake()
{
    base.Awake(); // Stats 자동 생성
    // PlayerStatSO를 Resources 또는 Inspector에서 주입
    if (_playerStatSO != null)
        Stats.Init(_playerStatSO);
    _currentHealth = Stats.MaxHealth;
}

public float GetHealthPercent() => _currentHealth / Stats.MaxHealth;
```

---

## ActorDefinitionSO 확장

```csharp
// ActorDefinitionSO.cs — 신규 필드 추가

[Header("스탯 데이터 (신규)")]
[Tooltip("통합 스탯 SO. 설정 시 EnemyStatsSO보다 우선 적용됩니다.")]
public ActorStatSO statData;
```

---

## 장비 연동 예시

```csharp
// PlayerEquipment.cs — 장비 장착/해제 시 스탯 수정자 관리

public void Equip(EquipmentSO equipment)
{
    // 기존 장착 아이템 효과 제거
    if (_equippedItems.TryGetValue(equipment.equipSlot, out var old))
        Actor.Stats.RemoveModifiersBySource(old);

    // 새 장비 효과 등록
    foreach (var bonus in equipment.statBonuses) // EquipmentSO에 추가 예정
    {
        Actor.Stats.AddModifier(new StatModifier(
            bonus.statType,
            bonus.modifierType,
            bonus.value,
            source: equipment  // 장비 SO 인스턴스를 출처로
        ));
    }

    _equippedItems[equipment.equipSlot] = equipment;
}

public void Unequip(EquipPosition slot)
{
    if (!_equippedItems.TryGetValue(slot, out var equipment)) return;
    Actor.Stats.RemoveModifiersBySource(equipment);
    _equippedItems.Remove(slot);
}
```

---

## PoiseStat 통합 (선택적)

`PoiseStat`은 현재 잘 동작하므로 즉시 제거하지 않습니다. 단, `ActorStatContainer`에서 Poise 관련 스탯 기본값을 읽도록 연결할 수 있습니다.

```csharp
// PoiseStat.cs — Init 시 ActorStatContainer에서 기본값 읽기 (옵션)

public void SyncFromStatContainer(ActorStatContainer stats)
{
    if (_data == null) return;
    // SO의 고정값보다 컨테이너 최종값 우선 사용 가능
    _currentPoise = stats.MaxPoise;
}
```

또는 장기적으로 `PoiseStat`의 로직을 `ActorStatContainer` 내부로 이전하고 Poise 현재값도 컨테이너가 관리하도록 리팩터링합니다.

---

## 마이그레이션 단계

| 단계 | 작업 | 기존 코드 영향 |
|------|------|----------------|
| **1단계** | `StatType`, `StatModifier`, `ActorStatSO`, `ActorStatContainer` 신규 파일 생성 | 없음 |
| **2단계** | `GameActor.Awake()`에 `Stats` 추가 | 없음 (기존 필드 유지) |
| **3단계** | `ActorDefinitionSO`에 `statData` 필드 추가 | 없음 (기존 필드 유지) |
| **4단계** | `MonsterActor.SetDefinition()` — `statData` 있으면 컨테이너 초기화 | 기존 EnemyStatsSO 폴백 유지 |
| **5단계** | `MonsterActor.TakeDamage()` — 방어율·공격력 계산 적용 | 수치 변화 발생, 밸런싱 필요 |
| **6단계** | `PlayerActor` — `_maxHealth` SerializeField 제거, `Stats.MaxHealth` 사용 | 씬/프리팹 재직렬화 필요 |
| **7단계** | `EquipmentSO`에 `statBonuses` 리스트 추가, `PlayerEquipment` 연동 | 기존 장비 데이터 재입력 |
| **8단계** | `PoiseStat` → `ActorStatContainer`로 완전 통합 (선택) | PoiseStat 컴포넌트 제거 |

---

## 파일 구조

```
Assets/02.Scripts/
├── Data/Stat/
│   ├── StatType.cs             # StatType enum
│   ├── StatModifier.cs         # StatModifier struct + ModifierType enum
│   └── ActorStatSO.cs          # 기본 스탯 ScriptableObject
│
└── GameActor/Component/Common/
    └── ActorStatContainer.cs   # 런타임 스탯 컨테이너 컴포넌트

Assets/10.Datas/Stat/
└── (각 액터별 ActorStatSO 에셋)
    ├── Stat_Bokusei.asset
    ├── Stat_Honoka.asset
    ├── Stat_LianLian.asset
    ├── Stat_Enemy_Grunt.asset
    └── ...
```

---

## 데미지 계산 공식 (참고)

```
최종 데미지 = HitPhaseData.damage
            × attacker.Stats.AttackPower     (공격력 배율, 기본 1.0)
            × critMultiplier                  (치명타 발생 시)
            × (1 - target.Stats.Defense)      (방어율 0.0~1.0)
```

**방어율 설계 가이드라인**

| 방어율 | 의미 |
|--------|------|
| 0.0 | 방어 없음 (기본 몬스터) |
| 0.1 | 10% 피해 경감 |
| 0.3 | 30% 경감 (중장갑 보스) |
| 0.5 | 50% 경감 (방패 가드 중) |
| 1.0 | 완전 무적 (사용 금지 — `_isInvincible` 플래그 사용) |

---

## 에디터 도구

에디터는 **4종**으로 구성됩니다. 기존 `ActorDatabaseEditorWindow` / `ActorRuntimeMonitorWindow`와 동일한 IMGUI 패턴을 사용합니다.

```
에디터 종류                   역할                                       열기 방법
─────────────────────────────────────────────────────────────────────────────────────
ActorStatSOEditor          ActorStatSO 인스펙터 커스텀 뷰                SO 선택 시 자동
StatDatabaseEditorWindow   전체 SO 관리 + CSV 익스포트                   메뉴 or 인스펙터 버튼
StatRuntimeMonitorWindow   Play 중 실시간 스탯 + 수정자 뷰               메뉴
StatDataGeneratorWindow    EnemyStatsSO/PoiseSO → ActorStatSO 자동 생성  메뉴
```

---

### 1. ActorStatSOEditor (CustomEditor)

`ActorStatSO`를 Project 창에서 선택하면 표시되는 인스펙터 커스텀 뷰.

```
┌─────────────────────────────────────────────────────┐
│  Stat_Enemy_Grunt  (ActorStatSO)              [편집] │
│─────────────────────────────────────────────────────│
│  카테고리 필터: [전체▼]         [+ 누락 스탯 채우기] │
│─────────────────────────────────────────────────────│
│  ▌생존                                              │
│  MaxHealth          ████████████████░░░░  100.0    │
│  HealthRegenRate    ░░░░░░░░░░░░░░░░░░░░    0.0    │
│                                                     │
│  ▌전투                                              │
│  AttackPower        ██████████░░░░░░░░░░    1.0    │
│  Defense            ███░░░░░░░░░░░░░░░░░    0.15   │
│  CritRate           ░░░░░░░░░░░░░░░░░░░░    0.0    │
│  CritMultiplier     ████████████░░░░░░░░    1.5    │
│                                                     │
│  ▌이동                                              │
│  MoveSpeed          ██████████░░░░░░░░░░    1.0    │
│  ...                                               │
│─────────────────────────────────────────────────────│
│          [에디터 창 열기]   [CSV 내보내기]           │
└─────────────────────────────────────────────────────┘
```

**구현 포인트**

```csharp
// Assets/02.Scripts/Data/Stat/Editor/ActorStatSOEditor.cs

[CustomEditor(typeof(ActorStatSO))]
public class ActorStatSOEditor : UnityEditor.Editor
{
    // StatType을 카테고리로 묶는 정적 맵
    private static readonly Dictionary<string, StatType[]> _categories = new()
    {
        { "생존",  new[] { StatType.MaxHealth, StatType.HealthRegenRate } },
        { "전투",  new[] { StatType.AttackPower, StatType.Defense,
                           StatType.CritRate, StatType.CritMultiplier } },
        { "이동",  new[] { StatType.MoveSpeed, StatType.DashDistance } },
        { "강인도", new[] { StatType.MaxPoise, StatType.PoiseRecoveryRate,
                            StatType.PoiseRecoveryDelay } },
        { "스킬",  new[] { StatType.SkillGaugeRate, StatType.InvincibleDuration } },
    };

    private SerializedProperty _statsProp;
    private string _categoryFilter = "전체";

    private void OnEnable() => _statsProp = serializedObject.FindProperty("_stats");

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawCategoryToolbar();
        DrawStatTable();          // 슬라이더 + 수치 필드 테이블
        DrawFillMissingButton();  // 누락 StatType 일괄 추가
        DrawActionButtons();      // 에디터 창 열기, CSV 내보내기
        serializedObject.ApplyModifiedProperties();
    }

    // 슬라이더 범위는 StatType마다 _sliderRanges 딕셔너리로 관리
    // (MaxHealth: 0~2000, Defense: 0~1, CritRate: 0~1 등)
    private static readonly Dictionary<StatType, (float min, float max)> _sliderRanges = new()
    {
        { StatType.MaxHealth,     (0f, 2000f) },
        { StatType.AttackPower,   (0f, 5f) },
        { StatType.Defense,       (0f, 1f) },
        { StatType.CritRate,      (0f, 1f) },
        { StatType.CritMultiplier,(1f, 5f) },
        { StatType.MoveSpeed,     (0f, 3f) },
        // ...
    };
}
```

**카테고리별 색상 코딩**

| 카테고리 | 색상 |
|---------|------|
| 생존 | 초록 `(0.2, 0.75, 0.3)` |
| 전투 | 빨강 `(0.85, 0.3, 0.3)` |
| 이동 | 파랑 `(0.3, 0.55, 0.9)` |
| 강인도 | 노랑 `(0.85, 0.7, 0.1)` |
| 스킬 | 보라 `(0.6, 0.3, 0.9)` |

---

### 2. StatDatabaseEditorWindow (EditorWindow)

프로젝트 내 모든 `ActorStatSO` 에셋을 한 창에서 관리.  
메뉴: **UPlayGround/Stat/Stat Database Editor**

```
┌──────────────────────────────────────────────────────────────────────┐
│ [Database SO ▾]  [새 SO 생성]  [비교 모드 ☐]     [CSV 내보내기] [저장] │
│──────────────────────────┬───────────────────────────────────────────│
│ 검색: [         ]        │  Stat_Enemy_Grunt                         │
│ ──────────────────────── │  ──────────────────────────────────────── │
│  Stat_Bokusei            │  카테고리: [전체▼]                        │
│  Stat_Honoka             │                                           │
│  Stat_LianLian           │  StatType          Base     [비교 대상]   │
│ ▶ Stat_Enemy_Grunt  ←선택│  MaxHealth         150.0    100.0  △+50  │
│  Stat_Enemy_Elite        │  AttackPower         1.2      1.0  △+0.2 │
│  Stat_Boss_Alpha         │  Defense             0.15     0.0  △+0.15│
│                          │  CritRate            0.0      0.0     —  │
│                          │  MoveSpeed           1.0      1.0     —  │
│ [+ 새 SO]  [복제]        │                                           │
│                          │  [+ 스탯 추가]  [누락 채우기]             │
└──────────────────────────┴───────────────────────────────────────────┘
```

**레이아웃 구조**

```csharp
// Assets/02.Scripts/Tool/Editor/Stat/StatDatabaseEditorWindow.cs

[MenuItem("UPlayGround/Stat/Stat Database Editor")]
public static void Open() { ... }

private void OnGUI()
{
    DrawToolbar();     // 상단 버튼 바
    EditorGUILayout.BeginHorizontal();
    DrawListPanel();   // 왼쪽: SO 목록 (240px 고정)
    DrawDivider();
    DrawDetailPanel(); // 오른쪽: 선택 SO 스탯 테이블
    EditorGUILayout.EndHorizontal();
}
```

**주요 기능**

| 기능 | 설명 |
|------|------|
| **SO 목록** | `AssetDatabase.FindAssets("t:ActorStatSO")`로 자동 수집, 검색 필터 지원 |
| **스탯 테이블** | StatType별 행, `EditorGUILayout.Slider` + 수치 직접 입력 필드 |
| **비교 모드** | 두 번째 SO를 선택하면 우측에 차이값(△) 컬럼 추가 표시 |
| **새 SO 생성** | 파일명 입력 → `AssetDatabase.CreateAsset` → 목록 자동 갱신 |
| **SO 복제** | 선택 SO를 기반으로 새 SO 생성 (밸런싱 변형 작업 시 유용) |
| **CSV 내보내기** | 전체 SO × 전체 StatType 행렬을 `.csv`로 저장 |
| **저장** | `Ctrl+S` 단축키, 미저장 시 버튼 주황색 강조 (`ColorUnsaved`) |

**CSV 내보내기 포맷**

```csv
ActorStatSO,MaxHealth,AttackPower,Defense,CritRate,CritMultiplier,MoveSpeed,...
Stat_Bokusei,120,1.0,0.0,0.05,1.5,1.0,...
Stat_Honoka,100,1.2,0.0,0.08,1.8,1.1,...
Stat_Enemy_Grunt,80,1.0,0.0,0.0,1.5,1.0,...
```

```csharp
private void ExportCSV()
{
    var path = EditorUtility.SaveFilePanel("CSV 내보내기", "", "StatBalance", "csv");
    if (string.IsNullOrEmpty(path)) return;

    var sb = new System.Text.StringBuilder();
    // 헤더 행
    sb.Append("ActorStatSO");
    foreach (StatType type in Enum.GetValues(typeof(StatType)))
        sb.Append($",{type}");
    sb.AppendLine();

    // 데이터 행
    foreach (var so in _allStatSOs)
    {
        sb.Append(so.name);
        foreach (StatType type in Enum.GetValues(typeof(StatType)))
            sb.Append($",{so.GetBase(type)}");
        sb.AppendLine();
    }

    File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);
    Debug.Log($"[StatDatabase] CSV 내보내기 완료: {path}");
}
```

---

### 3. StatRuntimeMonitorWindow (EditorWindow)

Play 모드 전용. 씬의 모든 액터 스탯과 활성 수정자를 실시간 모니터링.  
메뉴: **UPlayGround/Stat/Stat Runtime Monitor**

```
┌───────────────────────────────────────────────────────────────────────┐
│  [자동 갱신 ✓]  필터: [         ]  타입: [전체▼]                      │
│───────────────────────────────────────────────────────────────────────│
│  ActorId           이름          MaxHP   ATK    DEF   POISE  상태     │
│───────────────────────────────────────────────────────────────────────│
│▶ player_bokusei    Bokusei       120/120  1.0   0.0   80/100 [펼치기] │
│  └ 수정자 목록:                                                       │
│    [장비] SwordA        AttackPower  +Flat  0.15        영구          │
│    [버프] PowerUp       AttackPower  +%     0.30        3.2s 남음     │
│    [버프] SpeedBoost    MoveSpeed    +%     0.20        1.8s 남음     │
│───────────────────────────────────────────────────────────────────────│
│  enemy_grunt_001   Grunt          60/80   1.0   0.15  40/100 [펼치기]│
│  enemy_grunt_002   Grunt          80/80   1.0   0.15 100/100 [펼치기]│
└───────────────────────────────────────────────────────────────────────┘
```

**구현 포인트**

```csharp
// Assets/02.Scripts/Tool/Editor/Stat/StatRuntimeMonitorWindow.cs

[MenuItem("UPlayGround/Stat/Stat Runtime Monitor")]
public static void Open() { ... }

// 0.25초 간격 자동 갱신 (ActorRuntimeMonitorWindow와 동일 패턴)
private void OnEditorUpdate()
{
    if (!_autoRefresh) return;
    if (EditorApplication.timeSinceStartup - _lastRefreshTime < 0.25) return;
    _lastRefreshTime = EditorApplication.timeSinceStartup;
    CollectActorRows();
    Repaint();
}

// ActorStatContainer의 내부 상태를 에디터에서 읽기 위한
// 패키지 내부 접근 — internal 또는 #if UNITY_EDITOR 블록 활용
private void DrawActorRow(GameActor actor)
{
    var stats = actor.Stats;
    // HP 바 (ColorHpFull/Mid/Low 그라데이션)
    DrawStatBar(stats.GetFinalStat(StatType.MaxHealth),
                actor.GetCurrentHealth(),   // IDamageable 캐스트
                ColorHpFull, ColorHpLow);

    // 수정자 목록 (펼침)
    if (_expandedActors.Contains(actor))
        DrawModifierList(stats);
}

private void DrawModifierList(ActorStatContainer stats)
{
    // ActorStatContainer에 에디터 전용 접근자 추가 필요:
    // public IReadOnlyList<TimedModifier> GetModifiersForEditor() => _modifiers;
    foreach (var tm in stats.GetModifiersForEditor())
    {
        var m = tm.Modifier;
        string srcName    = m.source?.ToString() ?? "unknown";
        string durationStr = m.duration < 0f ? "영구" : $"{tm.RemainingTime:F1}s 남음";
        string sign        = m.value >= 0 ? "+" : "";

        EditorGUILayout.LabelField(
            $"  [{srcName}]  {m.statType}  {m.modifierType}  {sign}{m.value}  {durationStr}",
            EditorStyles.miniLabel);
    }
}
```

**컬럼 구성**

| 컬럼 | 내용 | 너비 |
|------|------|------|
| ActorId | `actor.ActorId` | 140px |
| 이름 | `gameObject.name` | 120px |
| HP | `current / max` + 색상 바 | 120px |
| ATK | `AttackPower` 최종값 | 55px |
| DEF | `Defense` 최종값 | 55px |
| POISE | `current / MaxPoise` | 80px |
| 수정자 수 | 활성 수정자 개수 | 40px |
| 펼치기 | 클릭 시 수정자 목록 토글 | 60px |

---

### 4. StatDataGeneratorWindow (EditorWindow)

기존 `ActorDefinitionSO`(에 연결된 `EnemyStatsSO` + `PoiseSO`)에서 자동으로 `ActorStatSO`를 생성하고 `definition.statData`에 자동 연결.  
메뉴: **UPlayGround/Stat/Stat Data Generator**

```
[ Definition 마이그레이션 ] [ 템플릿 생성 ]
─────────────────────────────────────────────────────────────────────
[새로고침] [ statData 없는 항목만 ✓ ]   저장 경로 [Assets/10.Datas/Stat/Generated]
─────────────────────────────────────────────────────────────────────
☑  ActorDefinitionSO       EnemyStats  PoiseSO  기존 statData  생성 예정
☑  ActorDef_Grunt          ✓           ✓        (없음)         ActorStat_grunt    [생성]
☑  ActorDef_Elite          ✓           ✓        (없음)         ActorStat_elite    [생성]
☐  ActorDef_Boss           ✓           ✓        ✓ Stat_Boss    ActorStat_boss     [재생성]
─────────────────────────────────────────────────────────────────────
[ 선택 항목 일괄 생성 (2개) ]   [ statData 없는 항목 모두 생성 ]
```

**동작**

| 매핑 | 출처 → 대상 |
|------|-------------|
| MaxHealth | `EnemyStatsSO.maxHealth` |
| MaxPoise | `PoiseSO.maxPoise` |
| PoiseRecoveryRate | `PoiseSO.recoveryRate` |
| PoiseRecoveryDelay | `PoiseSO.recoveryDelay` |
| 그 외 | `EnemyStatsSO.grade`에 따라 등급 템플릿 자동 적용 (Weak/Normal/Elite/Boss) |

**Player 기본 스탯 탭** — `CharacterActorType` 별로 한 개씩 `ActorStatSO`를 생성.

```
[ Definition 마이그레이션 ] [ Player 기본 스탯 ] [ 템플릿 생성 ]
─────────────────────────────────────────────────────────────────────
저장 경로:  Assets/10.Datas/Stat/Player
이름 접두:  ActorStat_Player_
☐ 기존 SO가 있으면 값을 덮어쓴다 (체크 해제 시 새 파일로 복제)
[기존 자산 다시 스캔] [플레이어블만 선택] [전체 선택] [전체 해제]
─────────────────────────────────────────────────────────────────────
☑  Character    프리셋        HP    ATK   MOV   CRIT% Poise  기존 자산
☑  Bokusei      전용 프리셋    120   1.0   1.0   5%    100   (없음)
☑  Honoka       전용 프리셋    110   1.2   0.95  5%    110   (없음)
☐  Reine        기본 (Player)  120   1.0   1.0   5%    100   (없음)
☑  LianLian     전용 프리셋    100   0.9   1.15  10%   80    (없음)
☐  Nenmir       기본 (Player)  120   1.0   1.0   5%    100   (없음)
─────────────────────────────────────────────────────────────────────
[ 선택한 캐릭터 (3명) 스탯 SO 생성 ]
```

| 캐릭터 | 컨셉 | HP | ATK | MOV | CRIT | Poise |
|--------|------|----|----|-----|------|-------|
| Bokusei (카타나) | 균형형 | 120 | 1.0 | 1.0 | 5% / 1.5× | 100 |
| Honoka (쌍도끼) | 공격형 | 110 | 1.2 | 0.95 | 5% / 1.6× | 110 |
| LianLian (채찍) | 민첩형 | 100 | 0.9 | 1.15 | 10% / 1.5× | 80 |
| 그 외 | 기본 (Player) 폴백 | 120 | 1.0 | 1.0 | 5% / 1.5× | 100 |

생성된 자산은 `Assets/10.Datas/Stat/Player/ActorStat_Player_<CharacterName>.asset` 형식으로 저장되며, **기존 SO 덮어쓰기 모드**로 재실행하면 같은 자산의 값만 갱신해 외부 참조가 끊기지 않습니다.

**템플릿 탭** — `ActorDefinitionSO` 없이 빈 SO를 일괄 생성. 종류:
- Empty (모든 스탯을 기본값으로 채움)
- WeakMonster / NormalMonster / EliteMonster / Boss (등급별 권장 스탯 프리셋)
- PlayerCharacter (플레이어 캐릭터용 기본 스탯 — 전용 프리셋이 없는 캐릭터에 적용되는 폴백)

생성 개수와 자산 이름 접두를 지정해 한 번에 N개를 만들 수 있습니다 (자동 번호 부여).

**템플릿 권장 프리셋**

| 템플릿 | MaxHealth | AttackPower | Defense | MaxPoise | MoveSpeed |
|--------|-----------|-------------|---------|----------|-----------|
| WeakMonster | 50 | 0.8 | 0.0 | 30 | 1.0 |
| NormalMonster | 80 | 1.0 | 0.0 | 50 | 1.0 |
| EliteMonster | 150 | 1.3 | 0.10 | 120 | 1.1 |
| Boss | 600 | 1.5 | 0.20 | 250 | 1.0 |
| PlayerCharacter | 120 | 1.0 | 0.0 (CritRate 0.05) | 100 | 1.0 |

---

### 파일 배치

```
Assets/02.Scripts/
├── Data/Stat/
│   ├── StatType.cs
│   ├── StatModifier.cs
│   ├── ActorStatSO.cs
│   └── Editor/
│       └── ActorStatSOEditor.cs        ← CustomEditor (인스펙터 뷰)
│
├── GameActor/Component/Common/
│   └── ActorStatContainer.cs
│
└── Tool/Editor/Stat/
    ├── StatDatabaseEditorWindow.cs     ← 전체 SO 관리 창
    ├── StatRuntimeMonitorWindow.cs     ← Play 중 실시간 모니터 창
    └── StatDataGeneratorWindow.cs      ← 자동 생성/마이그레이션 창

Assets/10.Datas/Stat/
├── Player/                             ← Player 기본 스탯 탭의 저장 위치
│   ├── ActorStat_Player_Bokusei.asset
│   ├── ActorStat_Player_Honoka.asset
│   └── ActorStat_Player_LianLian.asset
├── Generated/                          ← 마이그레이션 탭의 기본 저장 위치
│   ├── ActorStat_grunt.asset
│   └── ...
└── Enemy/
    ├── Stat_Enemy_Grunt.asset
    └── ...
```

---

### 메뉴 체계

```
UPlayGround/
├── Actor/
│   ├── Actor Database Editor          (기존)
│   └── Actor Runtime Monitor          (기존)
└── Stat/
    ├── Stat Database Editor           (신규)
    ├── Stat Runtime Monitor           (신규)
    └── Stat Data Generator            (신규)
```
