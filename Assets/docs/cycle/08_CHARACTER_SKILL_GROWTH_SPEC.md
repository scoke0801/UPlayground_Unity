# Character Skill Growth 구현 스펙 — 레벨업 포인트와 스킬 노드 트리

> 작성일: 2026-08-02
> 상태: 런타임·레거시 제거·현행 플레이어블 11명 v1 저작과 자동 검증 완료, 실제 게임 씬 감각 검증 필요
> 선행 문서: `../Complete/PLAYER_GROWTH_LEVELING_DESIGN.md` (EXP 루프), `../Complete/PARTY_LEVEL_POWER_DESIGN.md`
> 연관 문서: `../Complete/GAMEPLAY_ABILITY_SYSTEM_SPEC.md`, `../Complete/PASSIVE_ABILITY_SYSTEM_SPEC.md`, `03_CHARACTER_WEIGHT_SPEC.md`, `06_CYCLE_SAVE_SETTLEMENT_SPEC.md`

---

## 0. 현재 구현 대조 (2026-08-16)

명세 작성 시점의 타입 가정과 실제 프로젝트 사이에 다음 차이가 있어 구현 계약을 현재 구조에 맞췄다.

- 실제 스탯 런타임은 구 `ActorStatContainer`가 아니라 GAS의 `AttributeSetRuntime`이다. 노드 스탯은 `SkillTree.{CharacterActorType}` 소유 ID의 Infinite GameplayEffect로 적용·교체한다.
- `PartyMemberGrowthSO`는 기본 Attribute Profile, EXP 곡선, 초기 레벨과 상한만 소유한다. 레벨 자동 스탯 곡선, 휴식지점 스탯 투자, 랭크 마일스톤과 랜덤 해금 시드는 제거했다.
- 스탯·Ability·Passive 성장은 `CharacterSkillTreeSO`가 단독 권위를 가진다. Ability 실행 가능 여부는 `AbilityUnlockEffect`와 GAS 평가에서 판정한다. 일반 콤보는 Ability 해금 상태에서 사용 가능한 연속 구간 길이를 계산하며 별도 스탯 랭크 게이트를 두지 않는다.
- 영구 진행도는 별도 최상위 매니저가 아니라 `PartySaveData.skillProgress`에 저장한다. 사이클 DTO는 이를 참조하거나 복제하지 않는다.
- 전용 `UI_Scene_SkillTree` 팝업을 `SkillTree` 키로 추가했다. 좌측 캐릭터 탭/포인트 배지, 선행 관계 기반 노드·연결선, 상태·전후 수치·선행 조건, 공간 기반 게임패드 내비게이션, 무료 전체 초기화 2차 확인을 제공한다. 메뉴와 휴식 지점은 같은 편집 가능 화면을 연다.
- 보쿠세이는 14노드, 나머지 현행 플레이어블 10명은 각각 13노드의 생존·공격·특수 3분기 v1이 연결되어 있다. 각 트리는 실제 무기 AbilitySet의 콤보·전용기·궁극기 ID와 기존 패시브를 사용한다. `H09`는 enum과 플레이어 프리팹에 미사용으로 남아 있고 AbilitySet도 없으므로 현행 플레이어블 11명 범위에서 제외한다.
- 세이브 버전 3.1 로드 시 구형 `growthInvestments`의 투자량과 잔여 포인트를 합산해 새 트리의 미사용 포인트로 한 번 환급한다. 폐기된 스탯 노드 ID와 구형 성장 필드는 현재 저장 스키마에 기록하지 않는다.

현재 코드 구현 범위는 포인트 누적/소급, 노드 취득·전체 리스펙 API, 저장·로드, 스탯 modifier, Ability 해금·피해/Break/쿨다운/비용 스칼라, 패시브 합집합, 전용 UI, 에디터 검증이다.

---

## 1. 목표

레벨업 보상을 **자동 스탯 상승에서 플레이어의 명시적 선택으로** 옮긴다. 사이클마다 굴러가는 랜덤 강화가 아니라, 캐릭터별로 고정된 스킬판을 플레이어가 직접 찍어 나가는 영속 성장을 만든다.

| 항목 | 결정 |
|---|---|
| 획득 | 레벨업 시 **스킬 포인트** 지급 |
| 소비 | 스킬 UI에서 플레이어가 **노드를 직접 선택** |
| 범위 | 노드 그래프는 **캐릭터별 고정 저작**. 런마다 재추첨하지 않음 |
| 영속성 | 찍은 노드와 잔여 포인트는 **사이클을 넘어 유지**. 정산·유해 손실 대상 아님 |
| 랜덤 | 획득·제시·효과 어디에도 **RNG 없음** |

### 비목표

- 로그라이트식 런 한정 임시 강화(사이클 종료 시 소멸하는 버프 선택).
- 노드의 랜덤 제시, 랜덤 등급, 랜덤 접사.
- 장비 성장, 환생·돌파, 메타 재화 상점.
- 파티 공유 스킬판. 스킬판은 캐릭터 소유다.

---

## 2. 선행 설계와의 관계 (중요)

`../Complete/PLAYER_GROWTH_LEVELING_DESIGN.md`의 EXP 획득·분배·레벨 곡선은 유지한다. 자동 스탯 곡선과 레벨업 풀 회복을 포함한 성장 반영 부분은 본 문서가 대체한다.

| 항목 | 선행 문서 | 본 설계 |
|---|---|---|
| EXP 획득·분배 | 출전 전원 100% | **유지** |
| EXP 곡선(`LevelCurveSO`) | 공식/테이블 | **유지** |
| 레벨업 시 스탯 | `PartyMemberGrowthSO` 곡선 자동 상승 | **제거**. 레벨은 포인트 지급과 노드 요구 조건에만 사용 |
| 레벨업 시 추가 보상 | 없음 | **스킬 포인트 지급 (신규)** |
| 수동 배분 | 명시적으로 범위 밖 | **스킬 노드 선택으로 도입** |
| 레벨업 풀 회복·벤치 HP 갱신 | 자동 스탯 상승에 맞춰 수행 | **제거**. 노드 효과는 기존 GAS modifier 갱신 경로 사용 |

→ 레벨은 전투 수치를 자동으로 올리지 않는다. 플레이어가 획득한 포인트를 어떤 노드에 쓰는지가 영구 성장 결과를 결정한다.

---

## 3. 기존 코드 접점

| 기존 타입 | 활용 |
|---|---|
| `PartyManager._levels` / `GetLevel` | 레벨 권위. 포인트 지급 트리거 소스 |
| `OnPartyProgressionChanged` | 레벨·포인트·노드 변경 통지 채널 재사용 |
| `PartyMemberGrowthSO` | 캐릭터 기본 Attribute Profile, EXP 곡선, 초기 레벨과 상한 |
| `PartyPowerCalculator.CalculateBaseStats` | 레벨과 무관한 base 스탯 산출. 노드는 이 결과에 modifier로 얹힘 |
| `AttributeSetRuntime` / `AbilitySystemComponent` | base + modifier 합산. 노드 스탯 효과는 소유 ID가 있는 Infinite Effect **modifier로만** 들어감 |
| `PlayerActor.ApplyCharacterStats` | 스왑 시 주입 경로. 노드 modifier 재적용 지점 |
| `AbilitySetSO` / `GameplayAbilitySO` | 노드가 해금·강화하는 대상 |
| `PassiveAbilitySO` / `IPassiveModifierReader` | 노드의 Ability 문맥 보정을 **기존 계약으로** 노출 |
| `CharacterPassiveDatabaseSO` | 캐릭터 고유 패시브(고정)와 노드 부여 패시브(선택)를 구분해 합산 |
| `SaveManager` / `ISaveable` | 스킬 진행도 영속 저장 |
| `UI_Scene_PartyMenu`, `UI_Scene_CharacterSelect` | 스킬 UI 진입점과 표시 데이터 재사용 |

`ActorStatSO`에 새 `StatType`을 늘려 노드를 표현하지 않는다. 문맥형 보정은 `PASSIVE_ABILITY_SYSTEM_SPEC` P-06 결정을 그대로 따른다.

---

## 4. 포인트 경제

### 4.1 지급

```text
PartyManager 레벨 N → N+1
  -> CharacterSkillProgression.GrantPoints(characterType, amount)
  -> amount = SkillPointRule.Evaluate(newLevel)
```

| 규칙 | P0 값 |
|---|---|
| 기본 지급 | 레벨업 1회당 **1 포인트** |
| 지갑 소유 | **캐릭터별 독립**. 파티 공유 지갑 없음 |
| 소급 지급 | 저장 데이터에 `grantedUpToLevel`을 기록해 중복·누락 없이 정산 |

EXP는 출전 전원 100% 분배(선행 문서 결정)이므로, 출전한 캐릭터는 동일 속도로 포인트를 쌓는다. 포인트 총량은 랜덤이 아니라 **레벨의 함수**이며 같은 레벨의 같은 캐릭터는 언제나 같은 총 포인트를 갖는다.

```csharp
[Serializable]
public sealed class SkillPointRule
{
    public int perLevel = 1;

    // 누적 총량. 소급 지급과 검증에 사용한다.
    public int TotalPointsAtLevel(int level);
}
```

`TotalPointsAtLevel`을 단일 진실로 두고 `GrantPoints`는 `Total(new) - Total(old)`만 지급한다. 레벨업 이벤트를 세는 방식은 중복 지급 버그를 만든다.

### 4.2 소비와 환급

- 노드 1개를 찍는 데 필요한 포인트는 노드가 소유한다(`cost`).
- **리스펙: P0는 메뉴에서 언제든 전체 초기화를 제공하며 비용은 0이다.** 프로토타입 동안 빌드 실험 비용을 없애고 전투 검증 속도를 우선한다. 부분 환급·유료 리스펙은 P1에서 판단한다.
- 리스펙은 포인트를 잃지 않는다. 찍은 노드만 전부 해제하고 총 포인트를 그대로 돌려준다.
- 성장 화면이 게임을 일시 정지하고 입력을 독점하므로 월드 위치와 무관하게 안전하게 투자·초기화할 수 있다.

---

## 5. 데이터 모델

### 5.1 `CharacterSkillTreeSO`

캐릭터 1명의 고정 스킬판이다.

```csharp
[CreateAssetMenu(menuName = "UPlayGround/Party/Character Skill Tree")]
public sealed class CharacterSkillTreeSO : ScriptableObject
{
    public CharacterActorType characterType;
    public List<SkillNodeDefinition> nodes;
}

[Serializable]
public sealed class SkillNodeDefinition
{
    public string nodeId;                 // 트리 안에서 유일. 저장 키이므로 변경 금지
    public string displayNameKey;
    public string descriptionKey;
    public Sprite icon;

    public int cost = 1;
    public int maxRank = 1;               // 2 이상이면 다단 강화 노드

    public List<string> requiredNodeIds;  // 선행 노드. 전부 1랭크 이상이어야 함
    public int requiredLevel;             // 0이면 레벨 제한 없음

    public Vector2 layoutPosition;        // UI 배치 좌표. 런타임 규칙에 영향 없음

    [SerializeReference]
    public List<SkillNodeEffect> effects; // 랭크당 적용되는 효과
}
```

- `nodeId`는 `CycleSpawnPoint.spawnId`와 같은 등급의 영구 저장 키다. 리네임 금지 규칙을 문서와 검증기에 함께 건다.
- 트리 구조·비용·효과는 전부 저작 고정이다. 런타임에 노드를 생성하거나 순서를 섞지 않는다.

### 5.2 노드 효과

`[SerializeReference]` 다형으로 정의한다. FlowGraph 노드와 같은 방식이며, 어셈블리 이동 시 `[MovedFrom]` 유지 규칙도 동일하게 적용된다.

```csharp
[Serializable]
public abstract class SkillNodeEffect
{
    public abstract string Describe(int rank);
}
```

| 효과 타입 | 내용 | 적용 경로 |
|---|---|---|
| `StatDeltaEffect` | `AttributeId`에 flat 또는 percent 가산 | `AttributeSetRuntime` **modifier**로 주입 |
| `AbilityScalarEffect` | 특정 `abilityId`의 피해·Break·쿨다운·비용 배율 | GAS 실행 계산에서 합산 |
| `AbilityUnlockEffect` | 잠긴 `GameplayAbilitySO` 또는 Variant 해금 | `ActorAbilitySystem`의 사용 가능 목록 게이트 |
| `PassiveGrantEffect` | `PassiveAbilitySO` 부여 | 캐릭터 고정 패시브와 **합집합**으로 계산 |
| `DodgeCooldownEffect` | 회피 종료 뒤 재사용 대기 감소 | `PlayerStaminaRuntime` 행동 규칙에 적용 |

규칙:

- `StatDeltaEffect`는 절대 `SetBase`를 호출하지 않는다. base는 캐릭터 기본 프로필의 것이고 노드는 modifier다. 이 경계가 무너지면 리스펙 시 base가 오염된다.
- `AbilityScalarEffect`는 `HitPhaseData.damage`·`breakDamage` 원본 SO를 수정하지 않는다(`PASSIVE_ABILITY_SYSTEM_SPEC` P-07 동일).
- 같은 `abilityId`에 여러 노드가 걸리면 **percent는 합산, multiplier는 곱연산**으로 단일 규칙을 문서에 고정한다. 노드 순서에 결과가 의존하면 안 된다.
- `AbilityUnlockEffect`가 해금하는 Ability는 `AbilitySetSO`에 **이미 존재**해야 한다. 노드가 런타임에 Set을 변형하지 않는다.

### 5.3 보쿠세이 v1 매핑

| 분기 | 노드 | 최대 랭크 / 비용 | 실제 연결 |
|---|---|---:|---|
| 생존 | 강건한 몸 | 20 / 1 | `Vital.MaxHealth` +20 |
| 생존 | 지치지 않는 호흡 | 20 / 1 | `Resource.MaxStamina` +5 |
| 생존 | 회피의 리듬 | 5 / 1 | 회피 종료 쿨다운 -8% |
| 생존 | 찰나의 집중 | 1 / 3 | `PA_PerfectDodgeFocus` 부여 |
| 공격 | 벼린 칼날 | 20 / 1 | `Combat.AttackPower` +5% |
| 공격 | 예리한 눈 | 20 / 1 | `Combat.CritRate` +1% |
| 공격 | 이어지는 여섯째 칼 | 1 / 2 | `Player.Katana.Light.05` 해금 |
| 공격 | 끊기지 않는 아홉째 칼 | 1 / 3 | `Player.Katana.Light.08` 해금 |
| 공격 | 무너뜨리는 종결 | 1 / 3 | `Player.Katana.Heavy.05` 해금 |
| 특수 | 발도 | 1 / 2 | `Player.Katana.Ability` 해금 |
| 특수 | 폭풍의 칼끝 | 10 / 1 | 전용 기술 피해 +8% |
| 특수 | 짧아진 호흡 | 10 / 1 | 전용 기술 쿨다운 -4% |
| 특수 | 흐름의 절약 | 10 / 1 | 전용 기술 소모량 -3% |
| 특수 | 천검의 경지 | 1 / 3 | `Player.Katana.Ultimate` 해금 |

레벨 상한 100에서 획득하는 99포인트보다 전체 비용 131포인트가 크므로 모든 노드를 동시에 완성할 수 없다. MotionSet의 타이밍과 동작 데이터는 수정하지 않는다.

### 5.4 현행 플레이어블 11명 v1 확장

| 캐릭터 | 전투 정체성 | 노드 / 총비용 | 실제 콤보 해금 | 분기 핵심 변화 |
|---|---|---:|---|---|
| 보쿠세이 | 균형형 카타나 | 14 / 131 | Katana 약공격 6·9타, 강공격 6타 | 완벽 회피 집중, 발도·궁극기 |
| 호노카 | 브레이크형 쌍도끼 | 13 / 108 | DoubleAxe 강공격 4·6타 | 완벽 가드, 브레이크 강화, 궁극기 |
| 레이네 | 기동 견제형 창 | 13 / 114 | Default 약공격 6·10타 | 완벽 회피, 전용기 브레이크 분기, 궁극기 |
| 리안리안 | 광역 제압형 채찍 | 13 / 104 | Whip 약공격 4·6타, 강공격 5타 | 완벽 회피, 결박술 회전, 궁극기 |
| 넨미르 | 정밀 원거리 활 | 13 / 110 | Bow 사격 3·5타 | 상태 회복, 사격 효율 분기, 궁극기 |
| 세라 | 중량 대검 | 13 / 116 | GreatSword 강공격 4·7타 | 완벽 가드, 중량 브레이크, 궁극기 |
| 이노리 | 원소 전용기형 지팡이 | 13 / 101 | Default 약공격 6·10타 | 이로운 상태 연장, 스킬 피해, 궁극기 |
| 히치 | 연타 추격형 쌍검 | 13 / 116 | DualBlade 약공격 6·10타 | 완벽 회피, 전용기 연속 사용, 궁극기 |
| 시우하 | 공수 전환형 검방 | 13 / 118 | SwordShield 강공격 6·9타 | 상태 회복, 방패 브레이크, 궁극기 |
| 코모에 | 생존 지원형 지팡이 | 13 / 100 | Default 약공격 6·10타 | 완벽 가드, 전용기 효율·스킬 강화, 궁극기 |
| 릴리 | 추격 강타형 대검 | 13 / 106 | GreatSword 강공격 4·7타 | 상태 회복, 강공격 본능, 궁극기 |

- 전용 무기 Set이 없는 레이네·이노리·코모에는 현재 프리팹의 권위 참조인 `Player.Default` AbilitySet을 사용한다. 캐릭터별 스칼라와 해금 상태는 진행 서비스가 분리하므로 같은 Ability 에셋을 공유해도 성장 결과는 섞이지 않는다.
- 세 갈래 말단은 각각 `AbilityUnlockEffect` 또는 `PassiveGrantEffect`를 가져 숫자만 올리는 말단을 금지한다.
- 모든 트리의 총비용은 레벨 상한 100의 99포인트보다 커서 최소 한 가지 선택을 포기해야 한다.
- 신규 Ability·Payload·MotionSet은 만들지 않았다. MotionSet 타이밍과 HitPhase 원본은 그대로 유지한다.

### 5.5 진행도 상태

```csharp
[Serializable]
public sealed class CharacterSkillProgressState
{
    public CharacterActorType characterType;
    public int grantedUpToLevel;               // 포인트 소급 지급 기준
    public int totalPoints;
    public int spentPoints;
    public List<SkillNodeRankEntry> takenNodes; // nodeId + rank
}

[Serializable]
public sealed class SkillNodeRankEntry
{
    public string nodeId;
    public int rank;
}
```

- `availablePoints`는 저장하지 않는다. `totalPoints - spentPoints`로 항상 파생한다. 두 곳에 쓰면 반드시 어긋난다.
- 저장에는 랭크만 담고, 효과 값은 담지 않는다. 밸런스 패치가 세이브를 깨지 않게 한다.

---

## 6. 런타임

### `CharacterSkillProgressionService`

`PartyManager` 산하 서비스로 둔다. 신규 최상위 매니저를 만들지 않는다. 레벨 권위가 `PartyManager`에 있고, 포인트는 레벨의 함수이기 때문이다.

```csharp
public int GetAvailablePoints(CharacterActorType type);
public bool CanTakeNode(CharacterActorType type, string nodeId, out SkillNodeBlockReason reason);
public bool TryTakeNode(CharacterActorType type, string nodeId);
public bool TryRespec(CharacterActorType type);

public IReadOnlyList<StatModifierEntry> GetStatModifiers(CharacterActorType type);
public float GetAbilityScalar(CharacterActorType type, string abilityId, AbilityScalarKind kind);
public bool IsAbilityUnlocked(CharacterActorType type, string abilityId);
public float GetDodgeCooldownMultiplier(CharacterActorType type);

public event Action<CharacterActorType> OnSkillProgressChanged;
```

`SkillNodeBlockReason`은 `None`, `InsufficientPoints`, `MissingPrerequisite`, `LevelTooLow`, `MaxRank`, `MissingTree`, `MissingNode`로 명시한다. UI가 왜 못 찍는지 그대로 표시할 수 있어야 한다.

### 적용 시점

```text
[스왑 / 스폰]
PlayerActor.ApplyCharacterStats(model)
  -> 캐릭터 프로필 base 주입 (기존)
  -> 노드 StatDelta modifier 주입 (신규, 소유자 토큰 = SkillTree)

[전투 중 레벨업]
  -> 캐릭터별 포인트 1 지급
  -> base 능력치와 현재 자원은 변경하지 않음

[노드 획득 / 리스펙]
  -> 일시 정지된 성장 메뉴에서 발생
  -> SkillTree 토큰 modifier만 제거 후 재주입
  -> 현재 체력·스태미나는 회복하지 않고 새 상한으로만 clamp
  -> 장비·버프 modifier는 그대로 유지
```

- modifier는 반드시 **소유자 토큰**을 달아 주입한다. `03_CHARACTER_WEIGHT_SPEC` 5절의 프로필 교체 규칙과 같은 이유다.
- 벤치 캐릭터가 노드로 `MaxHealth`를 올리면 `_characterHealthMap`을 갱신한다(선행 문서 기둥 B). 활성/벤치 두 경로를 모두 처리한다.
- 잔류 공격은 스냅샷 시점의 스칼라를 사용한다. 히트마다 서비스를 재조회하지 않는다.

---

## 7. 사이클과의 관계

| 항목 | 규칙 |
|---|---|
| 사이클 시작 | 스킬 진행도를 **초기화하지 않는다**. 시드와 무관하다 |
| 사이클 정산 | 포인트·노드는 정산 대상이 아니다. 정산은 재료·경험치만 다룬다 |
| 파티 전멸·유해 | 유해 손실물에 **포인트와 노드를 포함하지 않는다**. 손실은 현재 레벨 경험치 진행분 30%와 미정산 재료 전량으로 유지 |
| 경험치 손실의 파급 | 경험치 손실로 레벨이 내려가는 경우가 없어야 한다. 레벨 다운이 발생하면 이미 지급한 포인트 회수 문제가 생긴다. **현재 레벨 진행분만 깎고 레벨은 내리지 않는다**를 명시 규칙으로 고정한다 |
| 사이클 난이도 배율 | 몬스터에만 적용한다. 스킬판에는 사이클 배율이 없다 |

`06_CYCLE_SAVE_SETTLEMENT_SPEC.md`의 영구 저장 섹션에 `CharacterSkillProgressState` 리스트를 추가한다. 실행 중 저장(런 스코프)이 아니라 **영구 저장** 쪽이다.

---

## 8. UI

### `UI_Scene_SkillTree`

- 진입점: 메인 메뉴 성장 버튼과 휴식 지점. 두 경로 모두 편집 가능.
- 좌측 캐릭터 탭, 중앙 노드 그래프, 우측 노드 상세 + 잔여 포인트.
- 노드 상태는 4종: `취득`, `취득 가능`, `선행 미충족`, `레벨 미달`.
- 취득 가능 노드만 색으로 강조한다. 잠긴 노드도 **효과를 미리 보여준다**. 랜덤이 없으므로 정보를 숨길 이유가 없고, 오히려 목표 설정이 반복 플레이 동기다.
- 확정 전 프리뷰: 선택 노드를 가정한 전투력·주요 스탯 변화량을 표시한다. 미리보기와 실제 적용은 같은 계산 API를 쓴다(`PASSIVE_ABILITY_SYSTEM_SPEC` P-12).
- 포인트 잔여 시 캐릭터 탭과 파티 메뉴 진입점에 배지를 띄운다.
- 게임패드 내비게이션은 `layoutPosition` 기반 방향 이동으로 처리한다.
- 노드 취득·초기화는 `SetUpdate(true)` 트윈과 UI 효과음을 사용한다. 새 Ability 또는 패시브를 처음 해금하면 전투 HUD Notification에도 획득 메시지를 보낸다.

---

## 9. 에디터 검증

`AbilityDataValidator`와 같은 계열의 규칙으로 추가한다.

- `nodeId` 공백·중복
- 선행 노드 순환 참조, 존재하지 않는 `requiredNodeIds`
- 도달 불가 노드(선행 체인이 루트에 닿지 않음)
- `cost <= 0`, `maxRank <= 0`
- `AbilityUnlockEffect`가 가리키는 `abilityId`가 해당 캐릭터 `AbilitySetSO`에 없음
- `AbilityScalarEffect`의 `abilityId` 미해석
- 캐릭터별 12~15노드, 루트 3개, 말단 3개인지 검사
- 말단이 `AbilityUnlockEffect` 또는 `PassiveGrantEffect`로 플레이 방식을 바꾸는지 검사
- 레벨 상한 100의 99포인트로 전체 노드를 다 찍을 수 있으면 선택의 의미가 사라지므로 오류
- 현행 플레이어블 11명의 트리 누락과 `PartyConfigSO` 연결 누락·중복
- 모든 Attribute Profile에 성장 대상인 `Resource.MaxStamina` 기본값이 직렬화되어 있는지 검사

Balance Designer 추출 데이터에 캐릭터별 총 포인트, 노드 수, 최대 취득 비율을 포함한다.

### 9.1 2026-08-16 검증 스냅샷

- `CharacterSkillProgressionServiceTests`: 12/12 성공
- `PlayerStaminaDataTests`: 4/4 성공
- Ability PlayMode 수직 슬라이스: 4/4 성공
- Ability EditMode 전체: 232개 중 230개 성공. 남은 2개 실패는 성장 변경과 무관한 동일한 콘텐츠 누락 목록(Dryad 3개, Training Dummy 1개의 미확정 MotionKey 매핑)을 모아 보고한다. 콘텐츠 Motion 근거가 없으므로 임의 매핑하지 않는다.
- Core/Data/UPlayGround Ability 어댑터/Actor/Ability Tests asmdef 보조 컴파일: 오류 0

---

## 10. 완료 조건

1. 레벨업 시 캐릭터별 포인트가 규칙대로 지급되고, 같은 레벨이면 언제나 같은 총 포인트를 갖는다.
2. 스킬 UI에서 찍은 노드가 즉시 살아있는 액터에 반영되고, 장비·버프 modifier가 소실되지 않는다.
3. 리스펙 후 총 포인트가 보존되고 스탯이 기본 Attribute Profile 값으로 정확히 복귀한다.
4. 사이클을 시작·완료·전멸해도 노드와 포인트가 변하지 않는다.
5. 저장·로드 후 노드 랭크와 잔여 포인트가 복원된다.
6. 같은 캐릭터를 두 번 육성해도 제시되는 노드 집합이 동일하다(랜덤 제시 없음).
7. 벤치 캐릭터의 노드 효과가 스왑 시점에 정확히 적용된다.
8. 밸런스 값만 바꾼 패치에서 기존 세이브의 노드 취득 상태가 유지된다.
