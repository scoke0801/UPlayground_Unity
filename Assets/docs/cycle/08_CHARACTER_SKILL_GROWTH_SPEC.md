# Character Skill Growth 구현 스펙 — 레벨업 포인트와 스킬 노드 트리

> 작성일: 2026-08-02
> 상태: P0 런타임 및 레거시 제거 완료, 캐릭터별 Ability/Passive 그래프 저작·Unity Play Mode 검증 필요
> 선행 문서: `../Complete/PLAYER_GROWTH_LEVELING_DESIGN.md` (EXP 루프), `../Complete/PARTY_LEVEL_POWER_DESIGN.md`
> 연관 문서: `../Complete/GAMEPLAY_ABILITY_SYSTEM_SPEC.md`, `../Complete/PASSIVE_ABILITY_SYSTEM_SPEC.md`, `03_CHARACTER_WEIGHT_SPEC.md`, `06_CYCLE_SAVE_SETTLEMENT_SPEC.md`

---

## 0. 현재 구현 대조 (2026-08-16)

명세 작성 시점의 타입 가정과 실제 프로젝트 사이에 다음 차이가 있어 구현 계약을 현재 구조에 맞췄다.

- 실제 스탯 런타임은 구 `ActorStatContainer`가 아니라 GAS의 `AttributeSetRuntime`이다. 노드 스탯은 `SkillTree.{CharacterActorType}` 소유 ID의 Infinite GameplayEffect로 적용·교체한다.
- `PartyMemberGrowthSO`는 기본 Attribute Profile, EXP 곡선, 초기 레벨과 상한만 소유한다. 레벨 자동 스탯 곡선, 휴식지점 스탯 투자, 랭크 마일스톤과 랜덤 해금 시드는 제거했다.
- 스탯·Ability·Passive 성장은 `CharacterSkillTreeSO`가 단독 권위를 가진다. Ability 실행 가능 여부는 `AbilityUnlockEffect`와 GAS 평가에서 판정하며, 콤보 길이와 ComboRoute를 스탯 랭크로 잠그는 별도 게이트는 없다.
- 영구 진행도는 별도 최상위 매니저가 아니라 `PartySaveData.skillProgress`에 저장한다. 사이클 DTO는 이를 참조하거나 복제하지 않는다.
- 전용 `UI_Scene_SkillTree` 팝업을 `SkillTree` 키로 추가했다. 좌측 캐릭터 탭/포인트 배지, `layoutPosition` 기반 노드·연결선, 4단계 상태, 우측 상세·다음 랭크 효과, 공간 기반 게임패드 내비게이션, 전체 리스펙 2차 확인을 제공한다. 휴식 지점에서는 편집 가능, 파티 상세에서는 읽기 전용으로 열린다.
- 현재 `PartyConfigSO.characterSkillTrees`에는 플레이어블 11종의 스탯 보드 초안이 연결되어 있다. 최대 체력·최대 스태미나·방어·치명타 확률·공격 속도·공격력 노드만 있고 선행 관계·Ability·Passive 노드는 아직 없다. 실제 스킬 트리로 전환하려면 캐릭터별 전투 정체성에 맞춰 그래프를 별도 저작해야 한다.
- 세이브 버전 3.1 로드 시 구형 `growthInvestments`와 잔여 포인트를 대응하는 `Stat.<AttributeId>` 노드 랭크로 한 번 이관한다. 구형 성장 필드는 현재 저장 스키마에 다시 기록하지 않는다.

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
| 마일스톤 보너스 | 5의 배수 레벨에서 **+1 포인트** |
| 지갑 소유 | **캐릭터별 독립**. 파티 공유 지갑 없음 |
| 소급 지급 | 저장 데이터에 `grantedUpToLevel`을 기록해 중복·누락 없이 정산 |

EXP는 출전 전원 100% 분배(선행 문서 결정)이므로, 출전한 캐릭터는 동일 속도로 포인트를 쌓는다. 포인트 총량은 랜덤이 아니라 **레벨의 함수**이며 같은 레벨의 같은 캐릭터는 언제나 같은 총 포인트를 갖는다.

```csharp
[Serializable]
public sealed class SkillPointRule
{
    public int perLevel = 1;
    public int milestoneInterval = 5;
    public int milestoneBonus = 1;

    // 누적 총량. 소급 지급과 검증에 사용한다.
    public int TotalPointsAtLevel(int level);
}
```

`TotalPointsAtLevel`을 단일 진실로 두고 `GrantPoints`는 `Total(new) - Total(old)`만 지급한다. 레벨업 이벤트를 세는 방식은 중복 지급 버그를 만든다.

### 4.2 소비와 환급

- 노드 1개를 찍는 데 필요한 포인트는 노드가 소유한다(`cost`).
- **리스펙: P0는 거점에서 전체 초기화만 제공하며 비용은 0이다.** 랜덤을 걷어낸 대가로 빌드 실험 비용을 낮춘다. 부분 환급·유료 리스펙은 P1에서 판단한다.
- 리스펙은 포인트를 잃지 않는다. 찍은 노드만 전부 해제하고 총 포인트를 그대로 돌려준다.
- 전투 중 리스펙과 노드 취소는 금지한다. 진입점은 안전 지역 UI뿐이다.

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
| `StatDeltaEffect` | `StatType`에 flat 또는 percent 가산 | `ActorStatContainer` **modifier**로 주입 |
| `AbilityScalarEffect` | 특정 `abilityId`의 피해·Break·쿨다운·비용 배율 | `IPassiveModifierReader` 조회 결과에 합산 |
| `AbilityUnlockEffect` | 잠긴 `GameplayAbilitySO` 또는 Variant 해금 | `ActorAbilitySystem`의 사용 가능 목록 게이트 |
| `PassiveGrantEffect` | `PassiveAbilitySO` 부여 | 캐릭터 고정 패시브와 **합집합**으로 계산 |

규칙:

- `StatDeltaEffect`는 절대 `SetBase`를 호출하지 않는다. base는 레벨 곡선의 것이고 노드는 modifier다. 이 경계가 무너지면 리스펙 시 base가 오염된다.
- `AbilityScalarEffect`는 `HitPhaseData.damage`·`breakDamage` 원본 SO를 수정하지 않는다(`PASSIVE_ABILITY_SYSTEM_SPEC` P-07 동일).
- 같은 `abilityId`에 여러 노드가 걸리면 **percent는 합산, multiplier는 곱연산**으로 단일 규칙을 문서에 고정한다. 노드 순서에 결과가 의존하면 안 된다.
- `AbilityUnlockEffect`가 해금하는 Ability는 `AbilitySetSO`에 **이미 존재**해야 한다. 노드가 런타임에 Set을 변형하지 않는다.

### 5.3 진행도 상태

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

public event Action<CharacterActorType> OnSkillProgressChanged;
```

`SkillNodeBlockReason`은 `None`, `InsufficientPoints`, `MissingPrerequisite`, `LevelTooLow`, `MaxRank`, `NotInSafeZone`으로 명시한다. UI가 왜 못 찍는지 그대로 표시할 수 있어야 한다.

### 적용 시점

```text
[스왑 / 스폰]
PlayerActor.ApplyCharacterStats(model)
  -> 레벨 곡선 base 주입 (기존)
  -> 노드 StatDelta modifier 주입 (신규, 소유자 토큰 = SkillTree)

[전투 중 레벨업]
  -> base만 SetBase로 갱신 (선행 문서 기둥 A)
  -> 노드 modifier는 손대지 않음

[노드 획득 / 리스펙]
  -> 안전 지역에서만 발생
  -> SkillTree 토큰 modifier만 제거 후 재주입
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

- 진입점: `UI_Scene_PartyMenu`의 캐릭터 상세, 안전 지역 한정.
- 좌측 캐릭터 탭, 중앙 노드 그래프, 우측 노드 상세 + 잔여 포인트.
- 노드 상태는 4종: `취득`, `취득 가능`, `선행 미충족`, `레벨 미달`.
- 취득 가능 노드만 색으로 강조한다. 잠긴 노드도 **효과를 미리 보여준다**. 랜덤이 없으므로 정보를 숨길 이유가 없고, 오히려 목표 설정이 반복 플레이 동기다.
- 확정 전 프리뷰: 선택 노드를 가정한 전투력·주요 스탯 변화량을 표시한다. 미리보기와 실제 적용은 같은 계산 API를 쓴다(`PASSIVE_ABILITY_SYSTEM_SPEC` P-12).
- 포인트 잔여 시 캐릭터 탭과 파티 메뉴 진입점에 배지를 띄운다.
- 게임패드 내비게이션은 `layoutPosition` 기반 방향 이동으로 처리한다.

---

## 9. 에디터 검증

`AbilityDataValidator`와 같은 계열의 규칙으로 추가한다.

- `nodeId` 공백·중복
- 선행 노드 순환 참조, 존재하지 않는 `requiredNodeIds`
- 도달 불가 노드(선행 체인이 루트에 닿지 않음)
- `cost <= 0`, `maxRank <= 0`
- `AbilityUnlockEffect`가 가리키는 `abilityId`가 해당 캐릭터 `AbilitySetSO`에 없음
- `AbilityScalarEffect`의 `abilityId` 미해석
- 레벨 상한까지 얻는 총 포인트로 **전체 노드를 다 찍을 수 있는지** 검사. 다 찍히면 선택의 의미가 사라지므로 경고
- 반대로 총 포인트가 최소 유효 빌드에도 못 미치면 오류
- P0 캐릭터(Honoka, Bokusei, Hichi)의 `CharacterSkillTreeSO` 누락. 기존 문서의 H09 표기는 현재 데이터와 맞지 않아 Hichi로 정정한다.

Balance Designer 추출 데이터에 캐릭터별 총 포인트, 노드 수, 최대 취득 비율을 포함한다.

---

## 10. 완료 조건

1. 레벨업 시 캐릭터별 포인트가 규칙대로 지급되고, 같은 레벨이면 언제나 같은 총 포인트를 갖는다.
2. 스킬 UI에서 찍은 노드가 즉시 살아있는 액터에 반영되고, 장비·버프 modifier가 소실되지 않는다.
3. 리스펙 후 총 포인트가 보존되고 base 스탯이 레벨 곡선 값으로 정확히 복귀한다.
4. 사이클을 시작·완료·전멸해도 노드와 포인트가 변하지 않는다.
5. 저장·로드 후 노드 랭크와 잔여 포인트가 복원된다.
6. 같은 캐릭터를 두 번 육성해도 제시되는 노드 집합이 동일하다(랜덤 제시 없음).
7. 벤치 캐릭터의 노드 효과가 스왑 시점에 정확히 적용된다.
8. 밸런스 값만 바꾼 패치에서 기존 세이브의 노드 취득 상태가 유지된다.
