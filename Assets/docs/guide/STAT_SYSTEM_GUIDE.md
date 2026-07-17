# 액터 스탯 시스템 가이드

## 개요

`GameActor`가 공통으로 사용하는 런타임 스탯 시스템입니다. 기본값은 `ActorStatSO`에 두고, 런타임에서는 `ActorStatContainer`가 최종값을 계산합니다.

핵심 원칙:

| 구분 | 기준 |
|------|------|
| 전투 공식에 들어가는 값 | `ActorStatSO` + `ActorStatContainer` |
| 버프/디버프/장비로 변하는 값 | `StatModifier` |
| 적 AI 성향과 순찰 | `EnemyBehaviorSO` |
| 적 스킬 사거리/쿨타임/히트 판정 | `EnemyAttackDataSO` |
| 기존 적 체력/이동/감지 튜닝 에셋 | `EnemyStatsSO` 마이그레이션 입력 |

`EnemyStatsSO.maxHealth`는 더 이상 런타임 체력 기준이 아닙니다. `StatDataGeneratorWindow`가 `ActorStatSO.MaxHealth` 초기값으로 옮기는 입력값이며, 런타임에서는 모든 `ActorDefinitionSO`에 `statData`가 연결되어 있어야 합니다.

---

## 아키텍처

```
GameActor
└── ActorStatContainer
    ├── ActorStatSO 기본값
    ├── StatModifier 목록
    └── GetFinalStat(StatType)

ActorDefinitionSO
├── statData      ──► ActorStatSO        전투/생존/이동 배율 스탯
├── stats         ──► EnemyStatsSO       레거시 적 튜닝/마이그레이션 입력
├── poiseData     ──► PoiseSO            강인도 런타임 컴포넌트 초기값
├── dropTable     ──► EnemyDropTableSO
└── prefab        ──► GameActor prefab
```

```
최종 스탯 계산:

baseValue
  + Flat 수정자 합
  * (1 + Percent 수정자 합)
  * Multiply 수정자 곱
  = ActorStatContainer.GetFinalStat()
```

### 파일 구조

```
Assets/02.Scripts/
├── Data/Stat/
│   ├── StatType.cs
│   ├── StatModifier.cs
│   ├── ActorStatSO.cs
│   └── Editor/ActorStatSOEditor.cs
├── GameActor/Component/Common/
│   ├── ActorStatContainer.cs
│   └── PoiseStat.cs
├── Data/Actor/Enemy/
│   ├── EnemyStatsSO.cs
│   └── PoiseSO.cs
└── Tool/Editor/Stat/
    ├── StatDatabaseEditorWindow.cs
    ├── StatDataGeneratorWindow.cs
    └── StatRuntimeMonitorWindow.cs

Assets/10.Datas/Stat/
├── Generated/
└── Player/
```

---

## 핵심 클래스

### ActorStatSO

`StatType`별 기본값을 보관하는 ScriptableObject입니다.

| API | 역할 |
|-----|------|
| `GetBase(StatType type)` | 명시 값이 있으면 그 값을, 없으면 타입별 기본값을 반환 |
| `TryGetExplicit(StatType type, out float value)` | 명시 등록 여부와 값을 함께 반환 |
| `GetDefault(StatType type)` | 코드에 정의된 폴백 기본값 반환 |
| `EditorSet`, `EditorRemove`, `EditorFillMissing` | 에디터 전용 생성/편집 API |

`StatType`을 추가하면 `ActorStatSO._defaults`, `ActorStatSOEditor`의 카테고리/슬라이더 범위, `StatDataGeneratorWindow` 템플릿 값을 함께 갱신해야 합니다.

### ActorStatContainer

모든 `GameActor`에 `GameActor.Awake()`에서 자동 부착됩니다.

| 멤버 | 역할 |
|------|------|
| `Init(ActorStatSO statSO)` | 기본 스탯 전체 교체 |
| `SetBase(StatType type, float value)` | 특정 기본값만 직접 설정 |
| `GetFinalStat(StatType type)` | 수정자까지 반영한 최종값 반환 |
| `AddModifier(StatModifier modifier)` | 버프/장비 수정자 추가 |
| `RemoveModifiersBySource(object source)` | 같은 출처의 수정자 일괄 제거 |
| `OnStatChanged` | 최종값 변경 이벤트 |

편의 프로퍼티:

```csharp
actor.Stats.MaxHealth
actor.Stats.AttackPower
actor.Stats.Defense
actor.Stats.MoveSpeed
actor.Stats.MaxPoise
```

### StatModifier

런타임 스탯 변경 단위입니다.

| 필드 | 설명 |
|------|------|
| `statType` | 변경할 스탯 |
| `modifierType` | `Flat`, `Percent`, `Multiply` |
| `value` | 변경량 |
| `source` | 제거 기준으로 사용할 출처 객체 |
| `duration` | `-1`이면 영구, 0보다 크면 초 단위 지속 시간 |

예시:

```csharp
actor.Stats.AddModifier(new StatModifier(
    StatType.AttackPower,
    ModifierType.Percent,
    0.25f,
    source: buffId,
    dur: 5f));
```

### MonsterActor

몬스터 체력은 현재값(`_currentHealth`)은 `MonsterActor`가 보유하고, 최대값은 `ActorStatContainer`의 `MaxHealth`를 기준으로 동기화합니다.

적용 순서:

1. `Awake()`에서 `ActorStatSO` 기본값으로 `ActorStatContainer`를 초기화한다.
2. `ActorDefinitionSO`가 주입되면 `definition.statData`로 `Stats.Init(definition.statData)`를 실행한다.
3. `statData`가 없으면 오류 로그를 남기고 기본 스탯으로 초기화한다. `EnemyStatsSO.maxHealth` 폴백은 사용하지 않는다.
4. `_maxHealth`와 `_currentHealth`를 `Stats.MaxHealth`로 재설정한다.

데미지 공식:

```csharp
float attackerPower = attackData.attacker != null ? attackData.attacker.Stats.AttackPower : 1f;
float defenseRate   = Mathf.Clamp01(Stats.Defense);
float finalDamage   = attackData.damage * attackerPower * (1f - defenseRate);
```

### EnemyStatsSO

기존 에셋 호환을 위해 유지하는 레거시 적 튜닝 SO입니다. 새 전투 스탯을 추가하는 위치가 아닙니다.

| 필드 | 현재 역할 |
|------|-----------|
| `maxHealth` | `ActorStatSO` 생성 시 `StatType.MaxHealth` 초기값 |
| `walkSpeed`, `runSpeed` | 적 전용 이동 튜닝 값 보존 |
| `detectionRadius`, `lostTargetRadius`, `fieldOfView` | 감지 튜닝 값 보존 |
| `attackRange`, `attackCooldown` | 레거시 값. 실제 스킬은 `EnemyAttackDataSO` 우선 |
| `grade` | `StatDataGeneratorWindow`의 등급 템플릿 선택과 `MonsterActor.Grade` 폴백 |
| `enablePatrol`, `patrolRadius`, `patrolWaitTime` | 레거시 값. AI 순찰은 `EnemyBehaviorSO` 우선 |

---

## 셋업 방법

### 몬스터

1. `UPlayGround/Stat/Stat Data Generator`의 `전체 보정`을 실행해 누락된 `ActorStatSO`를 생성한다.
2. `MaxHealth`, `AttackPower`, `Defense`, `MoveSpeed`, `MaxPoise` 등 전투에 필요한 값을 입력한다.
3. 해당 `ActorDefinitionSO.statData`에 연결한다.
4. 기존 `ActorDefinitionSO.stats`는 삭제하지 않아도 된다. 단, 체력 기준으로는 사용하지 않는다.
5. `PoiseSO`를 계속 사용하는 몬스터는 `ActorDefinitionSO.poiseData`에 연결한다.
6. `UPlayGround/Stat/Validate Stat Data Coverage`로 누락된 `statData`와 `StatType`이 없는지 확인한다.

### 플레이어

플레이어 캐릭터별 기본 스탯은 `Assets/10.Datas/Stat/Player/ActorStat_Player_<Character>.asset` 형식을 사용합니다. `StatDataGeneratorWindow`의 `Player 기본 스탯` 탭에서 생성할 수 있습니다.

### 마이그레이션

기존 `EnemyStatsSO`/`PoiseSO` 기반 몬스터는 메뉴에서 자동 생성할 수 있습니다.

```
UPlayGround/Stat/Stat Data Generator
```

마이그레이션 탭 매핑:

| 출처 | 대상 |
|------|------|
| `EnemyStatsSO.grade` | 등급별 템플릿 |
| `EnemyStatsSO.maxHealth` | `StatType.MaxHealth` |
| `PoiseSO.maxPoise` | `StatType.MaxPoise` |
| `PoiseSO.recoveryRate` | `StatType.PoiseRecoveryRate` |
| `PoiseSO.recoveryDelay` | `StatType.PoiseRecoveryDelay` |

---

## 사용 예시

### 버프 부여

```csharp
using UPlayGround.Data.Stat;

public void ApplyAttackBuff(GameActor actor, object source)
{
    actor.Stats.AddModifier(new StatModifier(
        StatType.AttackPower,
        ModifierType.Percent,
        0.30f,
        source,
        dur: 10f));
}
```

### 버프 제거

```csharp
public void RemoveBuff(GameActor actor, object source)
{
    actor.Stats.RemoveModifiersBySource(source);
}
```

### 현재 최종 스탯 조회

```csharp
float attackPower = actor.Stats.AttackPower;
float defenseRate = actor.Stats.Defense;
```

---

## 에디터 도구

| 메뉴 | 기능 |
|------|------|
| `UPlayGround/Stat/Stat Database Editor` | 모든 `ActorStatSO` 검색, 편집, 비교, CSV 내보내기 |
| `UPlayGround/Stat/Stat Runtime Monitor` | Play 모드에서 액터별 최종 스탯과 수정자 확인 |
| `UPlayGround/Stat/Stat Data Generator` | 기존 `ActorDefinitionSO`에서 `ActorStatSO` 생성, 연결, 전체 보정 |
| `UPlayGround/Stat/Validate Stat Data Coverage` | 모든 `ActorDefinitionSO.statData`와 명시 `StatType` 누락 검증 |

`ActorStatSO` 인스펙터는 카테고리별 슬라이더를 제공하고, 누락된 스탯은 기본값 폴백으로 표시합니다.

---

## 주의 사항

- `EnemyStatsSO`에 새 전투 수치를 추가하지 않습니다. 공격력, 방어력, 체력, 치명타, 이동 배율은 `ActorStatSO`에 추가합니다.
- `ActorDefinitionSO.statData`는 필수입니다. 누락되면 `MonsterActor.SetDefinition()`에서 오류를 남깁니다.
- `EnemyStatsSO.maxHealth`는 런타임 폴백으로 사용하지 않습니다.
- `MonsterActor.CurrentHealth`는 런타임 현재값이므로 `ActorStatContainer`에 넣지 않습니다.
- `Defense`는 `MonsterActor.TakeDamage()`에서 `Mathf.Clamp01`로 0~1 범위로 제한됩니다.
- `PoiseStat`은 아직 현재 Poise 값을 별도로 관리합니다. `ActorStatSO`의 Poise 값은 생성/밸런싱 기준으로 사용하고, 런타임 강인도 처리는 `PoiseStat`과 `PoiseSO`가 담당합니다.
- 기존 프리팹에 직접 설정된 이동 속도는 `ActorMovementController` 값이 기준입니다. `StatType.MoveSpeed`는 배율 스탯이므로 상태별 이동 계산에 연결할 때 곱셈으로 적용해야 합니다.

---

## 확장 포인트

새 스탯 추가 절차:

1. `StatType`에 항목 추가
2. `ActorStatSO._defaults`에 기본값 추가
3. `ActorStatSOEditor` 카테고리와 슬라이더 범위 추가
4. `ActorStatContainer` 편의 프로퍼티가 필요하면 추가
5. `StatDataGeneratorWindow` 템플릿/마이그레이션 값 추가
6. 실제 소비 코드에서 `actor.Stats.GetFinalStat(type)` 또는 편의 프로퍼티 사용

적 전용 튜닝 분리 방향:

`EnemyStatsSO`는 기존 에셋 호환 때문에 유지하되, 장기적으로는 다음처럼 역할을 나눕니다.

| 데이터 | 담당 SO |
|--------|---------|
| 생존/공격/방어/치명타/이동 배율 | `ActorStatSO` |
| 행동 확률/페이즈/거리 유지/순찰 | `EnemyBehaviorSO` |
| 스킬 사거리/쿨타임/히트박스 | `EnemyAttackDataSO` |
| 감지 반경/시야/등급 같은 적 고유 튜닝 | `EnemyStatsSO` 또는 후속 `EnemyTuningSO` |
