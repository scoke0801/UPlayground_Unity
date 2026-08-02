# 플레이어 성장 시스템 설계 — 개별 캐릭터 레벨링 (EXP 루프)

> 작성일: 2026-06-08
> 상태: 코드 구현 완료 (Unity 컴파일/플레이 검증 대기)
> **2026-08-02 부분 대체:** 아래 "범위 밖: 수동 스탯 포인트 배분, 스킬 트리" 결정은 `../cycle/08_CHARACTER_SKILL_GROWTH_SPEC.md`로 해제되었다. 레벨업 시 **스킬 포인트를 지급하고 플레이어가 스킬 UI에서 노드를 직접 선택**하는 레이어가 추가된다. 본 문서의 EXP 획득·분배·곡선·자동 곡선 성장과 5절의 정확성 기둥 A/B는 **그대로 유효**하다.
> 선행 문서: [PARTY_LEVEL_POWER_DESIGN.md](../Complete/PARTY_LEVEL_POWER_DESIGN.md) (2026-05-03)
> 관련 문서: [party-formation-system.md](../Complete/party-formation-system.md), [STAT_SYSTEM_GUIDE.md](../guide/STAT_SYSTEM_GUIDE.md), [SAVE_SYSTEM_GUIDE.md](../Complete/SAVE_SYSTEM_GUIDE.md)

---

## 0. 선행 문서와의 관계

[PARTY_LEVEL_POWER_DESIGN.md](../Complete/PARTY_LEVEL_POWER_DESIGN.md)의 **Phase A~C는 이미 구현**되어 있다 — `PartyMemberGrowthSO`, `PartyPowerCalculator`, `PartyManager`의 캐릭터별 레벨/전투력 API, 그리고 (선행 문서가 후속으로 미뤘던) 스왑 시 Growth Stat의 런타임 주입(`PlayerActor.ApplyCharacterStats`)까지 동작한다.

본 문서는 선행 문서의 **Phase D(후속 성장 시스템)** 를 구체화한다. 단, **모델을 변경**한다:

| | 선행 문서 Phase D (가정) | 본 설계 (확정) |
|---|---|---|
| 성장 화폐 | Growth Currency 획득 + 레벨업 비용 테이블 + `TryLevelUp` 수동 소비 | **EXP 직접 획득 → 자동 레벨업** (수동 소비 없음) |
| 분배 | 미정 | **출전(BattleOrder) 전원 100%** |
| 스탯 성장 | 〃 | 자동 성장만 (기존 곡선 재사용, 수동 스탯 포인트 없음) |
| 런타임 반영 | 캐릭터 교체 시 주입 | 교체 시 + **전투 중 레벨업 즉시 반영** |

→ 선행 문서의 `Growth Currency` / `TryLevelUp(비용 소비)` 항목은 본 설계로 **대체**된다.

---

## 1. 목표 & 범위

- 출전 캐릭터가 전투(몬스터 처치)로 **경험치(EXP)** 를 얻고, 누적치가 임계값을 넘으면 **레벨업** 한다.
- 레벨업 시 `PartyMemberGrowthSO`의 **기존 성장 곡선**대로 스탯이 자동 상승하고, 그 결과가 **살아있는 액터에 즉시 반영**된다.
- 캐릭터별로 레벨/경험치가 **독립**이며 저장/복원된다.

**범위 밖:** 장비 성장, 환생/돌파, 성장 화폐 소비.

> ~~수동 스탯 포인트 배분, 스킬 트리~~ → 2026-08-02 해제. `../cycle/08_CHARACTER_SKILL_GROWTH_SPEC.md` 참조. 자동 곡선 성장은 유지되며, 그 위에 포인트 기반 선택 레이어가 얹힌다.

## 2. 확정된 설계 결정

| 항목 | 결정 | 영향 |
|---|---|---|
| EXP 분배 | **출전 멤버(BattleOrder) 전원 100%** | `AwardBattleExp(amount)`가 출전 슬롯 전체에 동일 분배 |
| 성장 방식 | **자동 성장만(곡선 기반)** | 기존 `growthRules`(Flat/Percent/Curve) 재사용. 신규 배분 UI 불필요 |
| 레벨업 HP | **풀 회복** | 레벨업 순간 `_currentHealth = MaxHealth`. 벤치 멤버는 저장 HP를 새 MaxHP로 |

## 3. 현재 자산 (재사용 — 이미 동작)

이번 작업은 "새 시스템 구축"이 아니라 **EXP 루프 연결**이다.

| 자산 | 역할 | 위치 |
|---|---|---|
| `StatType` + `ActorStatContainer` | 런타임 스탯(base + modifier 합산, 캐시) | `Component/Common/ActorStatContainer.cs` |
| `ActorStatSO` | 레벨 1 기준 스탯 데이터 | `Data/Stat/ActorStatSO.cs` |
| `PartyMemberGrowthSO` | 캐릭터별 성장 규칙(baseStat, initialLevel, levelCap, growthRules) | `Data/Party/PartyMemberGrowthSO.cs` |
| `PartyPowerCalculator` | `CalculateGrowthStats(growth, level)` → 레벨별 스탯 dict 산출 | `Data/Party/PartyPowerCalculator.cs` |
| `PartyManager._levels` / `GetLevel` / `GetGrowthStats` | 캐릭터별 레벨 보관·조회 | `Manager/Party/PartyManager.cs` |
| `PlayerActor.ApplyCharacterStats` | 스왑 시 성장 스탯을 액터에 주입 | `GameActor/Object/Player/PlayerActor.cs:703` |
| `OnPartyProgressionChanged` | 레벨 변경 → UI 갱신(파티메뉴/HUD) | `UI_PartyMenu`, `UI_HudPlayerInfo` |

## 4. 빠진 부분 (신규 — 이번 작업)

1. **EXP 통화 자체** — 캐릭터별 누적 경험치 저장소가 없음. `SetLevelForDebug`만 존재.
2. **EXP 곡선** — "레벨 N → N+1 필요 경험치" 정의가 없음.
3. **EXP 획득 소스** — 몬스터 처치(`MonsterActor.OnDeath`)가 경험치를 주지 않음. 보상값 필드도 없음.
4. **레벨업 루프** — `AddExp → 임계 초과 시 레벨업`이 없음.
5. **살아있는 액터 즉시 반영** — `OnPartyProgressionChanged`는 **UI만** 구독. 전투 중 레벨업해도 액터 스탯이 안 바뀜. ← **이 설계의 핵심**
6. **저장/복원** — `GameSaveData`에 파티 섹션 없음. `PartyManager`는 `ISaveable` 미구현.

## 5. ⚠️ 두 개의 정확성 기둥 (반드시 지킬 것)

### 기둥 A — modifier를 보존하는 라이브 갱신
`ApplyCharacterStats`는 `Stats.Init(null)` 후 `SetBase`를 한다. 그런데 **`ActorStatContainer.Init`은 `_modifiers`를 전부 비운다**(장비/버프 수정자 소실). 스왑 시점엔 문제없지만(액터가 새로 셋업됨), **전투 중 레벨업에 이 경로를 재사용하면 장비·버프가 날아간다.**

→ 라이브 갱신 전용 경로는 **`SetBase`만 호출(Init 금지)** 한다. base만 교체되고 modifier는 그대로 재합산된다(`ActorStatContainer`가 dirty 캐시로 자동 재계산).

### 기둥 B — 벤치 캐릭터 HP는 별도 경로
출전했지만 대기 중인 멤버가 레벨업하면 `MaxHealth`가 오른다. 현재 HP는 `_characterHealthMap`에 저장된다(`GetMaxHealthForCharacter`/`GetHealthForCharacter`). 풀 회복 결정에 따라:
- **활성 캐릭터:** `_currentHealth = 새 MaxHealth`, `OnHpChanged` 발화.
- **벤치 캐릭터:** `_characterHealthMap[type] = 새 MaxHealth` (다음 스왑 시 풀 HP로 등장).

두 경로는 코드가 다르므로 **둘 다 명시적으로** 처리한다.

## 6. 데이터 모델 (신규)

### 6.1 `LevelCurveSO` — EXP 요구량 곡선
```csharp
[CreateAssetMenu(menuName = "UPlayGround/Party/Level Curve")]
public class LevelCurveSO : ScriptableObject
{
    // 공식 기반(권장): required(L) = round(baseExp * pow(L, exponent))
    [Min(1)]   public int   baseExp   = 100;
    [Min(1f)]  public float exponent  = 1.5f;

    // 또는 명시 테이블(공식과 택1) — 비어있으면 공식 사용
    public List<int> explicitTable = new();

    /// <summary>레벨 L → L+1 로 가는 데 필요한 경험치.</summary>
    public long GetRequiredExp(int level) { ... }
}
```
- `PartyMemberGrowthSO`에 `public LevelCurveSO levelCurve;` 필드 추가. 여러 캐릭터가 한 곡선 공유 가능, 필요 시 캐릭터별 분리.
- 곡선 미지정 시 안전 폴백(기본 공식)으로 동작.

### 6.2 EXP 보상 필드
> 메모: `EnemyStatsSO`는 제거되고 `statData(ActorStatSO)` 단일화됨 → 보상값은 거기 둘 수 없다.

→ `ActorDefinitionSO`에 `public long expReward;` 추가. `MonsterActor`는 `recruitableAs`/`dropTable`과 동일 패턴으로 주입:
```csharp
// MonsterActor.SetDefinition 부근 (recruitableAs 주입과 같은 곳)
_expReward = definition.expReward;
```

### 6.3 `PartyManager` 상태 추가
```csharp
private readonly Dictionary<CharacterActorType, long> _exp = new(); // 현재 레벨 내 누적 경험치
```
`_levels`와 짝. `_exp`는 "현재 레벨에서 다음 레벨까지의 진행분"을 저장(레벨업 시 차감 후 캐리오버).

## 7. API 설계 (`PartyManager`)

```csharp
// ── 이벤트 (신규) ──────────────────────────────
public event Action<CharacterActorType, long, long> OnExpChanged;  // (type, currentExp, requiredExp)
public event Action<CharacterActorType, int>        OnLevelUp;     // (type, newLevel)
// OnPartyProgressionChanged 는 레벨업 시 그대로 재발화 (기존 UI 호환)

// ── 획득 (외부 진입점) ─────────────────────────
/// <summary>출전 멤버 전원에게 동일 경험치 100% 분배. 몬스터 처치 시 호출.</summary>
public void AwardBattleExp(long amount)
{
    if (amount <= 0) return;
    for (int i = 0; i < _battleOrder.Count; i++)
    {
        var type = _battleOrder[i];
        if (type == CharacterActorType.None) continue;
        AddExp(type, amount);
    }
}

// ── 코어 ───────────────────────────────────────
/// <summary>단일 캐릭터에 경험치 누적 + 레벨업 처리. 디버그/치트/아이템에도 재사용.</summary>
public bool AddExp(CharacterActorType type, long amount)
{
    if (type == CharacterActorType.None || amount <= 0) return false;
    InitializeLevelIfMissing(type);

    int level  = _levels[type];
    int cap     = LevelCapOf(type);
    if (level >= cap) return false;               // 만렙: 경험치 무시(또는 0 고정)

    long exp   = _exp.GetValueOrDefault(type) + amount;
    bool leveled = false;

    while (level < cap)
    {
        long required = RequiredExpOf(type, level);
        if (exp < required) break;
        exp -= required;
        level++;
        leveled = true;
        OnLevelUp?.Invoke(type, level);
    }
    if (level >= cap) exp = 0;                     // 만렙 도달 시 잉여 버림

    _levels[type] = level;
    _exp[type]    = exp;

    OnExpChanged?.Invoke(type, exp, RequiredExpOf(type, level));
    if (leveled)
    {
        RefreshGrowthStats(type);                  // ← 핵심: 살아있는 액터 반영
        OnPartyProgressionChanged?.Invoke(type);   // 기존 UI 갱신 재사용
    }
    return leveled;
}
```

### 7.1 `RefreshGrowthStats` — 핵심 반영 로직
```csharp
private void RefreshGrowthStats(CharacterActorType type)
{
    var growthStats = GetGrowthStats(type);        // 기존 계산기 재사용
    if (growthStats == null) return;

    if (type == ActiveCharacterType && _player != null)
        _player.RefreshGrowthStatsLive(growthStats); // [기둥 A] SetBase만 + 풀 회복
    else
        UpdateBenchedMaxHealth(type, growthStats);   // [기둥 B] _characterHealthMap 갱신
}
```

`PlayerActor`에 신규 메서드(기둥 A 준수):
```csharp
/// <summary>전투 중 레벨업 등으로 base 스탯만 교체. modifier(장비/버프)는 보존한다.</summary>
public void RefreshGrowthStatsLive(IReadOnlyDictionary<StatType, float> growthStats)
{
    foreach (var pair in growthStats)
        Stats?.SetBase(pair.Key, pair.Value);      // Init() 호출하지 않음 → modifier 유지
    _maxHealth     = Mathf.Max(1f, Stats != null ? Stats.MaxHealth : _maxHealth);
    _currentHealth = _maxHealth;                    // 풀 회복
    OnHpChanged?.Invoke(_currentHealth, _maxHealth);
}
```

## 8. 흐름 (몬스터 처치 → 레벨업)

```
MonsterActor.OnDeath()
  └─ PartyManager.Instance?.AwardBattleExp(_expReward)   // recruit/quest와 같은 위치에 추가
       └─ foreach 출전멤버: AddExp(type, amount)
            ├─ _exp 누적, while 임계 초과 → level++ → OnLevelUp
            ├─ leveled?
            │    ├─ RefreshGrowthStats(type)
            │    │    ├─ 활성: PlayerActor.RefreshGrowthStatsLive (SetBase + 풀회복)
            │    │    └─ 벤치: _characterHealthMap[type] = 새 MaxHP
            │    └─ OnPartyProgressionChanged (UI)
            └─ OnExpChanged (EXP 바 UI)
```

## 9. 저장 / 복원 (`ISaveable`)

`GameSaveData`에 섹션 추가, `PartyManager : ISaveable` 구현(기존 패턴 그대로 — [SAVE_SYSTEM_GUIDE.md](../Complete/SAVE_SYSTEM_GUIDE.md)):
```csharp
[Serializable] public class PartySaveData
{
    public List<PartyMemberSaveEntry> members = new(); // type, level, exp
    public List<string> roster      = new();
    public List<string> battleOrder = new();
    public int activeIndex;
}
[Serializable] public class PartyMemberSaveEntry { public string type; public int level; public long exp; }
```
- `ExportSaveData`: roster/battleOrder/activeIndex + 각 멤버 level·exp 기록.
- `ImportSaveData`: roster·battleOrder 복원 → levels·exp 복원 → 활성 캐릭터 `ApplyCharacterStats` 재적용(스왑 경로 = Init 허용, 아직 modifier 없음).
- **순서 주의:** 레벨 복원이 액터 스폰/스탯 적용보다 **먼저**여야 GetGrowthStats가 올바른 레벨을 반영.

## 10. UI / 연출 (Phase 3)

- **EXP 바:** `UI_HudPlayerInfo`(활성 캐릭터)·`UI_PartyMenu`(전원) 에서 `OnExpChanged` 구독 → `current/required` 게이지. 기존 `OnPartyProgressionChanged` 구독부 옆에 추가.
- **레벨업 연출:** `OnLevelUp` 구독 → VFX/SFX/토스트. 활성 캐릭터면 풀 회복 힐 이펙트 동반.
- 전투력 표기는 기존 `GetCombatPower`/`PartyPowerCalculator` 재사용.

## 11. 구현 단계

| Phase | 내용 | 산출물 |
|---|---|---|
| **1. 코어 루프** | `LevelCurveSO`, `ActorDefinitionSO.expReward`, `PartyManager._exp`+`AddExp`/`AwardBattleExp`+이벤트, `MonsterActor.OnDeath` 연결, `PlayerActor.RefreshGrowthStatsLive`, 벤치 HP 갱신 | 처치 시 레벨업+스탯 반영 동작 |
| **2. 저장/복원** | `PartySaveData`, `PartyManager : ISaveable` | 레벨/경험치 영속 |
| **3. UI/연출** | EXP 바, 레벨업 토스트/VFX/SFX | 플레이어 피드백 |

## 12. 엣지 케이스 / 리스크

1. **만렙 잉여 EXP** — 버림(설계: `exp=0`). 추후 돌파 시스템이 소비하려면 보관으로 변경.
2. **HP 0(전투불능) 멤버에게도 분배?** — "출전 전원 100%" 결정에 따라 분배함. 레벨업 시 풀 회복이므로 부활 부작용은 없음(별도 부활 규칙은 스왑 로직이 관장). 단, `None` 슬롯은 제외.
3. **다단 레벨업** — 큰 경험치 한 번에 여러 레벨 상승 시 `OnLevelUp`이 레벨마다 발화 → 연출 폭주 주의(연출 측에서 디바운스/합산 권장).
4. **modifier 소실** (기둥 A) — 라이브 갱신에 `Init` 절대 금지. 코드 리뷰 체크포인트.
5. **벤치 HP 누락** (기둥 B) — 활성/벤치 분기 둘 다 처리했는지 확인.
6. **EnemyStatsSO 잔재 참조** — `expReward`는 반드시 `ActorDefinitionSO`에. (stats 파이프라인 제거됨)
7. **로드 순서** — 레벨 복원 → 액터 스탯 적용 순서 보장.
8. **밸런스 곡선** — `LevelCurveSO`·`growthRules`는 기존 Stat/Balance 생성기 툴과의 정합성 확인 필요(전투력 산식 `PartyPowerCalculator`와 어긋나지 않게).

---

## 13. 레벨업 부여 효과 & 연출 (UI / FX)

§10의 스케치를 구체화한다. **전투 중 레벨업이 잦은 게임**(출전 전원 100% 분배)이라는 전제 아래, "보상감은 주되 전투 흐름은 끊지 않는다"를 원칙으로 한다.

### 13.1 레벨업 순간 부여 효과 (게임플레이)

| 효과 | 정책 | 적용 위치 | 근거 |
|---|---|---|---|
| 스탯 성장 | growthRules 곡선 적용 | `RefreshGrowthStatsLive` / 벤치 갱신 (§7.1) | 확정 |
| **HP 풀 회복** | 확정 — 활성=`_currentHealth=Max`, 벤치=`_characterHealthMap[type]=Max` | §5 기둥 B | 확정 |
| **Poise 풀 회복** | 권장 적용 — `PoiseStat` 리셋 | 활성 캐릭터만 | 경직/브레이크 중 레벨업 시 자연 해소, MaxPoise 성장과 일관 |
| Skill 게이지 | **미변경** | — | 전투 자원 악용 방지(처치 반복으로 궁극기 충전 차단) |
| 무적 프레임 | **미부여** | — | 풀 회복으로 충분. i-frame은 회피/스왑 시스템 소관 |

> 활성/벤치 분기는 §5 기둥 B를 그대로 따른다. 효과 적용은 **`RefreshGrowthStats` 직후** 한곳에서 수행한다(이벤트 핸들러가 아니라 모델 갱신 경로). 연출(아래)과 분리한다 — **상태 변경 = 즉시, 연출 = 디바운스 가능**.

### 13.2 연출 오케스트레이터 — `LevelUpFeedbackHandler`

`DefenseSuccessFeedbackHandler`와 동일 패턴. `GameHandlerBase` 상속, `GameCombatManager._handlers`에 등록, `PartyManager.OnLevelUp` 구독.

```csharp
public sealed class LevelUpFeedbackHandler : GameHandlerBase
{
    private float _lastPlayTime;            // 연출 쿨다운(다단/연속 레벨업 디바운스)
    private const float MinInterval = 0.35f;

    public override void AfterInit()
        => PartyManager.Instance.OnLevelUp += OnLevelUp;
    public override void Dispose()
        { if (PartyManager.Instance != null) PartyManager.Instance.OnLevelUp -= OnLevelUp; }

    private void OnLevelUp(CharacterActorType type, int newLevel)
    {
        bool isActive = type == PartyManager.Instance.ActiveCharacterType;
        // HUD/파티메뉴 텍스트는 OnPartyProgressionChanged가 이미 갱신 → 여기선 연출만.
        if (isActive) PlayActiveCharacterFx(newLevel);   // 전신 VFX + 플로터 + 포스트프로세스
        // 벤치 레벨업은 화면에 캐릭터가 없으므로 HUD/파티메뉴 텍스트 갱신으로 충분.
    }
}
```

**다단/동시 레벨업 처리(§12.3 해결):** 한 처치로 여러 레벨 또는 여러 캐릭터가 동시에 오를 수 있다. 핸들러에서 `MinInterval` 쿨다운으로 **연출 1회로 합산**하고, 표기는 항상 **최종 레벨**을 읽는다(`GetLevel(type)`). 상태 변경은 레벨마다 즉시, 연출만 합친다.

### 13.3 연출 레이어 (활성 캐릭터)

| 레이어 | API (재사용) | 비고 |
|---|---|---|
| 전신 VFX | `GameObjectManager.ShowFX("LevelUp", basePos, Quaternion.identity, parent:player.transform, duration)` | `FXKeyType.cs`는 자동 생성 파일이라 직접 편집 금지 → **문자열 키** 사용. 프리팹 미등록 시 무음 no-op. `parent`로 캐릭터에 부착 |
| "LEVEL UP" 플로터 | `UIManager.ShowDamageFloaterLabel(headPos, $"LEVEL UP! Lv.{level}", FloatStyle.Critical)` | `FloatStyle.LevelUp` 신규 대신 **기존 `Critical`(골드) 재사용**(config SO 색상 에셋 변경 회피). 레벨은 커밋 후 `GetLevel`로 읽음 |
| ~~포스트프로세스 플래시~~ | **미적용** (사용자 결정) | 구현 제외 |
| 카메라 | **미적용** | 타임스케일 슬로우 **금지**(전투 흐름 차단) |
| SFX | ⚠️ **AudioManager 미구현** → 호출 훅(TODO 주석)만. (현재 사운드는 `MotionEvent_PlaySound` 경로뿐) | 추후 연결 |

> **구현 노트:** `OnLevelUp`은 `AddExp` 루프 *내부*에서 발화하므로 그 시점 `_levels`는 커밋 전이다. 핸들러는 플래그만 세우고 **다음 `Update`에서** 커밋된 최종 레벨로 1회 연출한다(같은 프레임 다단/다캐릭터 자동 합산 + 표기 정확성).

EXP 획득 자체 피드백(레벨업 아닐 때도): 처치 시 `UIManager.ShowDamageFloaterLabel(monsterPos, $"+{exp} EXP", FloatStyle.Exp)` (선택, 잦으면 생략/누적).

### 13.4 HUD (`UI_HudPlayerInfo` 확장)

이미 `_levelText`(`SetLevel`)·HP바가 있다. 추가:
- **EXP 바(신규):** `OnExpChanged(type, cur, req)` 구독 → 활성 캐릭터면 fill 보간(기존 `SkillGaugeFillCoroutine` 패턴 복제). 레벨업 시 fill 100%→0% 리셋 후 잔여 채움.
- **레벨 텍스트 펀치:** `OnLevelUp` 시 `_levelText` 스케일 펀치(코루틴) + 색 플래시. 기존 `SetLevel` 호출은 `OnPartyProgressionChanged`로 이미 됨.

### 13.5 센터 토스트 (선택)

큰 마일스톤(예: 특정 레벨 도달)만 `UIManager.ShowUI(UIKeyType.LevelUpToast, CanvasLayer.HUD)` 신규 경량 UI로 강조. 일반 레벨업은 플로터+HUD로 충분 → **매 레벨 센터 토스트는 지양**(연출 폭주).

### 13.6 신규 데이터 / enum 항목

- `FXKeyType.LevelUp` — 캐릭터 부착 오라 VFX 키.
- `FloatStyle.LevelUp`, `FloatStyle.Exp` — 플로터 스타일(기존 `Normal/Heal/Miss` 옆).
- (토스트 채택 시) `UIKeyType.LevelUpToast`.
- 선택: `LevelUpFeedbackProfile`(SO 또는 상수) — `DefenseSuccessFeedbackProfile` 패턴. fxKey·floatStyle·postProcess 파라미터·`MinInterval`·`enableCameraShake` 외부화.

### 13.7 구현 매핑 (§11 Phase 3 상세)

| 작업 | 파일 |
|---|---|
| `LevelUpFeedbackHandler` 신규 + 등록 | `Manager/Handler/.../LevelUpFeedbackHandler.cs`, `GameCombatManager._handlers` |
| `FXKeyType.LevelUp` / `FloatStyle.LevelUp·Exp` | `Data/Path/FXKeyType.cs`, FloatStyle enum |
| EXP 바 + 레벨 펀치 | `UI/HUD/UI_HudPlayerInfo.cs` (OnExpChanged·OnLevelUp 구독) |
| LevelUp VFX 프리팹 | FX 등록(`ShowFX` 키 매핑) |
| SFX 훅 | TODO 주석 (AudioManager 대기) |

---

## 14. 구현 현황 (2026-06-08)

코드 작성 **완료**. **Unity 컴파일/플레이 미검증**(코드 리뷰만). 아래 §15 수동 작업 전에는 인게임에 기능이 나타나지 않는다.

작성/수정된 파일:
- 신규: `Data/Party/LevelCurveSO.cs`, `Manager/Handler/Combat/LevelUpFeedbackHandler.cs`
- 수정: `Data/Party/PartyMemberGrowthSO.cs`(levelCurve), `Data/Actor/ActorDefinitionSO.cs`(expReward), `GameActor/Object/Monster/MonsterActor.cs`(_expReward·GrantPartyExp), `Manager/Party/PartyManager.cs`(EXP루프·ISaveable), `GameActor/Object/Player/PlayerActor.cs`(RefreshGrowthStatsLive·UpdateBenchedGrowth), `GameActor/Component/Common/PoiseStat.cs`(RecoverFull), `Data/Save/GameSaveData.cs`(PartySaveData), `Manager/Combat/GameCombatManager.cs`(핸들러 등록), `UI/HUD/UI_HudPlayerInfo.cs`(EXP바·레벨펀치)

설계와 다르게 구현한 점(스코프 축소):
- 포스트프로세스/카메라 연출 **제외**(사용자 결정).
- `FloatStyle.LevelUp`/`Exp` 신규 대신 기존 `FloatStyle.Critical` 재사용(config SO 에셋 변경 회피).
- `FXKeyType.LevelUp` enum 대신 **문자열 키 `"LevelUp"`** 사용(`FXKeyType.cs`는 자동 생성 파일).
- 벤치 멤버 풀회복은 **HP>0일 때만**(다운된 멤버 부활 방지).

## 15. 작업자(나) 수동 작업 — 코드 외 ✅체크리스트

코드만으로는 동작하지 않는다. 아래는 **에디터에서 직접** 해야 한다.

### 15.1 필수 (안 하면 기능 미동작)

- [ ] **HUD 프리팹 연결** — `UI_HudPlayerInfo` 프리팹의 인스펙터에서 `_expFill`(Image, Filled 타입), `_expText`(TextMeshProUGUI) 슬롯 연결. *안 하면 EXP 바가 항상 비어 보임.*
- [ ] **몬스터 경험치 입력/자동 발급** — `UPlayGround/게임플레이/밸런스/몬스터 경험치 발급기`에서 기준 플레이어 레벨·등급 배율·레벨차 보정을 확인하고 `ActorDefinitionSO.expReward`를 일괄 적용. *유일한 경험치 소스 — 0이면 레벨이 절대 안 오름. 정의 없이 씬 배치된 몬스터는 0.*
- [ ] **성장 곡선 지정** — 각 `PartyMemberGrowthSO`의 `levelCurve`에 `LevelCurveSO` 에셋 연결.
  - [ ] `몬스터 경험치 발급기`의 **LevelCurve 생성/찾기** 및 **Growth 곡선 연결** 버튼으로 기본 곡선 생성과 `PartyMemberGrowthSO.levelCurve` 자동 연결 가능.
  - [ ] `UPlayGround/Party/Level Curve`로 `LevelCurveSO` 1개 이상 생성(공통 1개부터 시작 권장). `baseExp`/`exponent` 또는 `explicitTable` 설정.
  - *미지정 시 코드 폴백 곡선(baseExp 100, exponent 1.5)으로 조용히 동작 — 의도와 다를 수 있음.*

### 15.2 연출 (안 해도 게임플레이는 동작, VFX만 없음)

- [ ] **LevelUp VFX 프리팹** — 레벨업 오라 FX 프리팹을 만들고 **`"LevelUp"` 키**로 FX 시스템에 등록(다른 FX와 동일 등록 경로). *미등록 시 `ShowFX`가 에러 없이 no-op → VFX만 안 나옴.*
  - (선택) `FXKeyType` enum에 항목이 필요하면 `UPlayGround/ID Enum Generator` 창에서 **재생성**(파일 직접 편집 금지). 현재 코드는 문자열 키라 enum 없이도 동작.
- [ ] (선택) 레벨업 SFX — `AudioManager` 구현 후 `LevelUpFeedbackHandler.PlayActiveCharacterFx`의 TODO 위치에 연결.

### 15.3 검증

- [ ] **컴파일 확인** — Unity 콘솔에 에러 없는지. (자동 테스트 없음)
- [ ] **인게임 확인** — 몬스터 처치 → EXP 바 상승 → 임계 도달 시 레벨업(스탯/HP 풀회복/플로터/HUD 펀치). 디버그용 `PartyManager.SetLevelForDebug(type, level)` 또는 `AddExp(type, amount)`로 빠르게 테스트 가능.
- [ ] **스왑 시 EXP 바 갱신** — 캐릭터 교체 시 해당 캐릭터의 레벨/EXP로 HUD가 스냅되는지.
- [ ] **저장/로드** — `SaveManager.Instance.SaveGame(0)` 후 `LoadGame(0)` → 캐릭터별 레벨/경험치 복원 확인.

### 15.4 밸런싱 (별도 패스)

- [ ] `LevelCurveSO` 곡선과 `PartyMemberGrowthSO.growthRules` 성장률을 기존 Stat/Balance 생성기 툴 결과와 정합되게 조정. `PartyPowerCalculator` 전투력 산식과 어긋나지 않게.

---
**한 줄 요약:** 성장 계산·레벨 저장·스왑 반영은 이미 있다(선행 문서 Phase A~C). 신규는 ① EXP 통화+곡선 ② 처치 보상 연결 ③ **modifier-safe 라이브 갱신 + 벤치 HP** ④ 저장 ⑤ **레벨업 부여 효과(풀회복+Poise리셋) & 연출(`LevelUpFeedbackHandler`로 VFX/플로터/HUD, 슬로우 금지·디바운스)**. 코드 완료/미검증 — 실제 동작은 §15 수동 작업(HUD 연결·expReward·levelCurve·FX 등록) 필요.
