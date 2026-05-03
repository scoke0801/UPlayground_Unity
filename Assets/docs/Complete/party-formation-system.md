# 파티 편성 시스템 설계 (Roster / Battle Order 분리)

> 작성일: 2026-05-01
> 대상 버전: Unity 6 (6000.0.60f1), URP
> 상태: 설계 단계 (구현 전)
> 관련 문서: [combat-character-swap-system.md](./combat-character-swap-system.md)

---

## 1. 배경

기존 `PartyManager._partyOrder` 한 개 리스트가 두 의미를 겸하고 있다.

- 보유 캐릭터 전체 (`MonsterActor._recruitableAs` 처치 보상으로 합류한 모든 멤버)
- 전투 참여 멤버 (`RequestSwapTo(int)` 의 인덱스 대상)

이 때문에 다음이 불가능하다.

- 보유 멤버가 늘어나도 유저가 출전 멤버를 직접 고를 수 없다.
- 출전 인원의 상한을 데이터로 제어할 수 없다.

본 문서는 두 개념을 분리하여 **Roster(보유) ≠ BattleOrder(출전)** 구조로 재정의한다.

---

## 2. 목표

- 보유 캐릭터와 출전 캐릭터를 분리한다.
- 출전 멤버 상한은 `PartyConfigSO`로 제어 (기본 4).
- 신규 합류 시 출전 슬롯이 비어있으면 자동 편입, 가득이면 보유만.
- `UI_PartySelect`에서 유저가 출전 멤버를 편성한다.
- 기존 캐릭터 교체(Swap) 메커니즘과 입력은 변경 없음 — 출전 멤버 안에서만 동작.

---

## 3. 용어 정의

| 용어 | 정의 |
|------|------|
| **Roster** | 게임이 진행되며 보유하게 된 모든 `CharacterActorType` 목록. 상한 없음. |
| **BattleOrder** | 출전(전투 참여) 슬롯. 최대 `maxBattleSize` 명. Swap 입력의 대상. |
| **Active** | `BattleOrder` 내에서 현재 조작 중인 캐릭터. `ActiveIndex` 는 항상 `BattleOrder` 기준. |
| **출전(出戰)** | BattleOrder 에 포함된 상태. |
| **편성(編成)** | BattleOrder 슬롯을 변경하는 행위. |

---

## 4. 데이터 모델

### 4.1 PartyConfigSO 변경

```csharp
[CreateAssetMenu(fileName = "PartyConfig", menuName = "UPlayGround/Party/Party Config")]
public class PartyConfigSO : ScriptableObject
{
    // 출전 슬롯 상한 (데이터로 제어)
    [Min(1)] public int maxBattleSize = 4;

    // 게임 시작 시 BattleOrder 초기 구성. 비어있으면 partyOrder 의 앞 maxBattleSize 명을 사용.
    public List<CharacterActorType> defaultBattleOrder = new();

    // 보유 멤버 초기 목록 (= 시작 시 Roster). 기존 partyOrder 를 그대로 사용.
    public List<CharacterActorType> partyOrder = new();

    [Min(0)] public int startActiveIndex = 0;

    // (기존) Entry Attack 관련 필드 그대로 유지
    [Min(0f)] public float defaultEntryAttackRange = 6f;
    public LayerMask entryAttackTargetLayer = ~0;
    public LayerMask entryAttackLineOfSightBlocker = 0;
}
```

마이그레이션
- `partyOrder` 의미는 그대로 두되, "초기 Roster" 로 해석한다.
- `defaultBattleOrder` 가 비어있으면 `partyOrder.Take(maxBattleSize)` 를 출전 명단으로 채운다.
- 기존 `PartyConfig.asset` 파일은 그대로 호환된다.

### 4.2 PartyManager 상태

```csharp
public class PartyManager : BaseManager<PartyManager>, IManager
{
    private List<CharacterActorType> _roster      = new(); // 보유 전체
    private List<CharacterActorType> _battleOrder = new(); // 출전 슬롯
    private int _activeIndex = 0;                          // BattleOrder 기준
    private int _maxBattleSize = 4;
    // ...
}
```

`_partyOrder` 필드는 제거하고 `_battleOrder` 로 대체된다 (스왑 대상이 BattleOrder 이므로 이름이 의미와 일치).

---

## 5. 공개 API

### 5.1 프로퍼티

```csharp
public IReadOnlyList<CharacterActorType> Roster      => _roster;
public IReadOnlyList<CharacterActorType> BattleOrder => _battleOrder;
public int                               ActiveIndex => _activeIndex;
public int                               MaxBattleSize => _maxBattleSize;
public CharacterActorType                ActiveCharacterType { get; }
public PlayerActor                       ActiveCharacter      { get; }
```

기존 `PartyOrder` 프로퍼티는 `BattleOrder` 로 이름 변경. 외부 호출처는 `UI_PartySelect` 단일이므로 영향 작다.

### 5.2 메서드

```csharp
// 보유에 추가. BattleOrder 가 가득 차지 않았으면 자동 편입.
// 이미 보유 중이면 무시. 반환: BattleOrder 에 자동 편입 되었는지.
public bool UnlockCharacter(CharacterActorType type);

// BattleOrder 슬롯에 추가 (가득 차있으면 false).
public bool AddToBattle(CharacterActorType type);

// BattleOrder 에서 제거. 활성이었다면 활성 자동 보정.
// BattleOrder 가 비면 false (전투 가능 멤버 없음).
public bool RemoveFromBattle(CharacterActorType type);

// 슬롯 단위 교체. 슬롯에 있던 캐릭터는 BattleOrder 에서 제외 (Roster 에는 유지).
public bool ReplaceBattleSlot(int slotIndex, CharacterActorType type);

// 기존 메서드는 그대로 유지 — BattleOrder 인덱스로 동작.
public bool RequestSwapNext();
public bool RequestSwapTo(int targetIndex);
public bool CanSwap();
```

### 5.3 이벤트

```csharp
public event Action<PlayerActor, PlayerActor> OnSwapStarted;     // 기존
public event Action<PlayerActor>              OnSwapCompleted;   // 기존
public event Action<CharacterActorType>       OnCharacterUnlocked; // 기존
public event Action                           OnRosterChanged;     // 신규
public event Action                           OnBattleOrderChanged;// 신규
```

`OnCharacterUnlocked` 는 새 멤버가 Roster 에 들어온 순간만 발화.
`OnRosterChanged` / `OnBattleOrderChanged` 는 각 리스트가 변경된 모든 경로에서 발화.

---

## 6. 핵심 시나리오

### 6.1 게임 시작

```
PartyManager.Init()  → PartyConfigSO 로드
PartyManager.AfterInit()
  ├─ _maxBattleSize = config.maxBattleSize
  ├─ _roster        = config.partyOrder (전체 복사)
  ├─ _battleOrder   = config.defaultBattleOrder.Count > 0
  │                       ? config.defaultBattleOrder
  │                       : _roster.Take(_maxBattleSize)
  ├─ _activeIndex   = clamp(config.startActiveIndex, 0, _battleOrder.Count - 1)
  └─ PlayerSwapBehaviour.InitializeTo(_battleOrder[_activeIndex])
```

### 6.2 적 처치로 합류

```
MonsterActor.Die() → TryRecruitToParty() → PartyManager.UnlockCharacter(type)

UnlockCharacter(type):
  if (_roster.Contains(type)) return false;
  if (PlayerSwapBehaviour.GetModelData(type) == null) return false;  // 기존 검증

  _roster.Add(type);
  OnRosterChanged?.Invoke();
  OnCharacterUnlocked?.Invoke(type);

  if (_battleOrder.Count < _maxBattleSize)
  {
      _battleOrder.Add(type);
      OnBattleOrderChanged?.Invoke();
      return true;   // 자동 편입
  }
  return false;       // 보유만, 유저가 직접 편성해야 출전 가능
```

### 6.3 유저가 편성 변경

UI_PartySelect 에서 유저 액션이 다음 메서드로 이어진다.

| UI 액션 | 호출 |
|---------|------|
| 후보(미출전) 캐릭터를 빈 슬롯으로 | `AddToBattle(type)` |
| 후보를 차있는 슬롯으로 (교체) | `ReplaceBattleSlot(slotIndex, type)` |
| 출전 슬롯에서 빼기 | `RemoveFromBattle(type)` |
| 출전 슬롯 클릭 (편성모드 OFF) | `RequestSwapTo(slotIndex)` (기존) |

### 6.4 활성 보정 규칙

`RemoveFromBattle` / `ReplaceBattleSlot` 으로 활성 캐릭터가 BattleOrder 에서 빠지는 경우:

```
1. BattleOrder 에 살아있는(HP > 0) 멤버가 있으면 그 중 가장 가까운 인덱스로 활성 이동
   → 내부적으로 PlayerSwapBehaviour.SwapTo() 호출
2. 살아있는 멤버가 없으면: 변경 거부 (false 반환).
   유저는 활성 캐릭터를 마지막 살아있는 슬롯에서 뺄 수 없다.
```

전투 중 사망 시점의 활성 보정은 본 문서 범위 외 (기존 사망 처리 로직 유지).

### 6.5 정책 정리

| 상황 | 처리 |
|------|------|
| Roster 에 없는 type 을 AddToBattle | false |
| 이미 BattleOrder 에 있는 type 을 AddToBattle | false |
| HP 0 캐릭터를 AddToBattle | 허용 (UI 표시는 "전투 불능") |
| 활성 캐릭터를 RemoveFromBattle | 살아있는 다른 출전 멤버 있을 때만 허용 |
| 마지막 한 명을 RemoveFromBattle | 거부 |
| BattleOrder 가 0 인 상태 | 게임 진행 불가 — 발생하지 않도록 위 규칙으로 보호 |
| `maxBattleSize` 가 런타임에 줄어드는 경우 | 본 단계에서 미지원 (시작 시 1회만 적용) |

---

## 7. UI 설계 (UI_PartySelect)

### 7.1 모드 전환

기존 화면 + 우측 상단에 **편성 모드 토글 버튼** 추가.

| 모드 | 출전 슬롯 클릭 | 후보 클릭 | 비고 |
|------|---------------|-----------|------|
| **스왑 (기존, 기본)** | 즉시 활성 교체 | (후보 영역 미표시) | 현행 동작 유지 |
| **편성** | 슬롯 선택 highlight | 선택 슬롯과 후보 자리 교체. 선택 없으면 빈 슬롯 자동 채움 | 즉시 반영 (적용 버튼 없음) |

### 7.2 화면 구성 (편성 모드)

```
┌──────────────────────────────────────────────────────┐
│ [편성 모드] ⚙             [최대 출전: 3 / 4]   [닫기] │
├──────────────────────────────────────────────────────┤
│   ▼ 출전 슬롯 (BattleOrder)                          │
│   ┌────┐ ┌────┐ ┌────┐ ┌────┐                        │
│   │ 박 │ │ 호 │ │ 렌 │ │ +  │  ← 빈 슬롯             │
│   │  ★ │ │    │ │    │ │    │  (★ = 활성)            │
│   └────┘ └────┘ └────┘ └────┘                        │
│                                                      │
│   ▼ 후보 (Roster - BattleOrder)                       │
│   ┌────┐ ┌────┐                                       │
│   │ 련 │ │ 네 │                                       │
│   └────┘ └────┘                                       │
└──────────────────────────────────────────────────────┘
```

- 빈 슬롯은 항상 `maxBattleSize` 까지 채워서 그린다 (`+` 표시).
- 활성 캐릭터를 빼는 경우 본 문서 §6.4 규칙으로 차단되며, UI 는 시각적으로 "잠금" 표시.
- 우측 상단의 "최대 출전" 표기는 `MaxBattleSize` 를 그대로 노출.

### 7.3 변경되는 클래스

| 클래스 | 변경 내용 |
|--------|-----------|
| `UI_PartySelect` | 편성 모드 토글, 후보 영역 추가, BattleOrder/Roster 별도 바인딩 |
| `UI_PartyMemberSlot` | 빈 슬롯 표시 옵션 추가 (캐릭터 타입 = None), 편성 모드 콜백 분기 |
| (신규) `UI_PartyCandidateSlot` | 후보 영역 슬롯. 클릭 시 부모로 `OnCandidatePicked(type)` 콜백 | 선택 사항 — `UI_PartyMemberSlot` 재사용 가능 |

### 7.4 이벤트 구독

`UI_PartySelect.OnShow()` 에서 추가 구독.

```csharp
PartyManager.Instance.OnRosterChanged       += Refresh;
PartyManager.Instance.OnBattleOrderChanged  += Refresh;
PartyManager.Instance.OnCharacterUnlocked   += OnCharacterUnlocked; // 기존
```

---

## 8. 영향 분석

| 시스템 | 변경 |
|--------|------|
| `PartyManager` | 내부 상태 분리, 신규 API/이벤트 추가. `PartyOrder` → `BattleOrder` 리네임 |
| `PartyConfigSO` | `maxBattleSize`, `defaultBattleOrder` 필드 추가. 기존 `partyOrder` 의미 유지 (= 초기 Roster) |
| `UI_PartySelect` | 편성 모드 추가, 후보 영역 추가, 신규 이벤트 구독 |
| `UI_PartyMemberSlot` | 빈 슬롯 표시, 편성 모드 분기 |
| `MonsterActor.TryRecruitToParty` | 변경 없음 — `UnlockCharacter` 시그니처 동일 |
| `PlayerSwapBehaviour` | 변경 없음 — `SwapTo(type)` 그대로 사용 |
| 입력 시스템 | 변경 없음 — Q 스왑은 BattleOrder 안에서 동작 |
| 세이브 시스템 | (추후) Roster / BattleOrder / ActiveIndex 직렬화 필요. 본 문서 범위 외. |

---

## 9. 구현 단계

### Phase A — 데이터/매니저 분리
1. `PartyConfigSO` 에 `maxBattleSize`, `defaultBattleOrder` 추가.
2. `PartyManager` 내부 `_partyOrder` 를 `_roster` / `_battleOrder` 로 분리.
3. `UnlockCharacter` 자동 편입 규칙 적용.
4. `AddToBattle` / `RemoveFromBattle` / `ReplaceBattleSlot` 구현 + 활성 보정.
5. `OnRosterChanged` / `OnBattleOrderChanged` 이벤트 추가.

### Phase B — UI 편성 모드
1. `UI_PartySelect` 편성 모드 토글 + 후보 영역.
2. `UI_PartyMemberSlot` 빈 슬롯 표시 + 편성 콜백.
3. 신규 이벤트 구독.

### Phase C — 폴리싱 (추후)
1. 편성 변경 시 사운드/짧은 트랜지션.
2. 활성 자동 보정 시점의 카메라/연출.
3. 세이브 연동.

---

## 10. 결정 사항

| # | 결정 |
|---|------|
| 1 | 보유와 출전을 분리 (`Roster` vs `BattleOrder`) |
| 2 | 출전 상한은 `PartyConfigSO.maxBattleSize` (기본 4) |
| 3 | 자동 편입은 BattleOrder 가 가득 차지 않은 경우에만 |
| 4 | 편성 변경은 즉시 반영 (적용 버튼 없음) |
| 5 | 활성 캐릭터를 마지막 살아있는 출전 슬롯에서 빼는 행위는 차단 |
| 6 | HP 0 캐릭터의 BattleOrder 등록 자체는 허용 (스왑 시 기존 규칙으로 차단) |
| 7 | 런타임 `maxBattleSize` 변경은 1차 범위에서 미지원 |
