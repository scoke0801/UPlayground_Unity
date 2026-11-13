# 파티 체력 회복 인터렉션 오브젝트 설계 문서

> 작성일: 2026-06-08
> 대상 버전: Unity 6 (6000.0.60f1), URP
> 상태: 코드 구현 완료 (2026-06-09) / 에디터 셋업 필요 (§4)
> 레퍼런스: 소울라이크 화톳불(Bonfire), 휴식지점/회복 제단

---

## 1. 개요

휴식지점(모닥불/회복 제단) 류의 정적 인터렉션 오브젝트. 플레이어가 상호작용하면
**출전·대기 전 파티원의 HP를 전부 풀 회복**한다. 기존 `GatheringActor`(채집) 인터렉션
흐름을 모델로 하되, 아이템 드랍 대신 파티 회복을 수행한다.

### 확정 사양

| 항목 | 결정 | 비고 |
|------|------|------|
| 회복 대상 | 액티브 + 벤치 **전원** | `파티 체력 회복` 사양상 자명 |
| 다운(HP 0) 멤버 | **부활 + 풀 회복** | 기존 컨벤션 우회 필요 (§3) |
| 회복량 | **MaxHealth 풀 회복** | 부분 회복은 추후 `healRatio`로 확장 |
| 사용 방식 | **무제한 재사용** | 소멸·쿨다운 없음 |

> ⚠️ "부활 포함"은 기존 두 회복 경로와 **의도적으로 다른** 동작이다.
> - `PlayerActor.Heal()` → `if (!IsAlive()) return;` 가드 (`PlayerActor.cs:991`)
> - `PlayerActor.UpdateBenchedGrowth()` → `stored <= 0f` 멤버 제외 (`PlayerActor.cs:757`)
>
> 따라서 기존 `Heal()`를 재사용할 수 없고, 가드를 우회하는 신규 API를 추가한다.

---

## 2. 핵심 제약 — 파티 HP는 두 곳에 분리 저장

단일 `PlayerActor` + 모델 교체 아키텍처이므로 파티 HP가 두 곳에 나뉘어 있다.
**단순 루프로는 반드시 한쪽을 놓친다.**

| 대상 | 저장 위치 | 회복 방법 |
|------|-----------|-----------|
| 액티브 캐릭터 | `PlayerActor._currentHealth` / `_maxHealth` | `Heal()` / `HealPercent()` (단, `IsAlive` 가드) |
| 벤치 멤버 | `PlayerActor._characterHealthMap[type]` | **공개 회복 API 없음 → 신규 추가** |

- 액티브 캐릭터는 활성 중 `_characterHealthMap`에 **없다**. 스왑아웃 시점에만 기록된다
  (`PlayerActor.cs:604`).
- 기록이 없는 벤치 멤버(한 번도 출전/피격 안 함)는 이미 풀피로 취급된다
  (`GetHealthForCharacter`가 max 반환).

→ 단일 진입점 `PartyManager.HealAllParty()`가 액티브와 벤치를 **명시적으로 각각** 처리한다.

### 기존 회복 경로 조사 결과

- `PlayerActor.Respawn(pos, rot, healPercent)` (`PlayerActor.cs:1284`)는 `_currentHealth`만
  세팅한다. **액티브 캐릭터만 회복**하며 벤치는 손대지 않는다.
- `RespawnPopup` → `Respawn` 경로도 동일하게 액티브 전용. **파티 전체 회복 루틴은 존재하지 않는다.**
- 결론: 벤치 회복 API와 파티 전체 회복 오케스트레이션을 **신규로 작성**해야 한다.

---

## 3. 터치포인트

### ① `InteractionObjectType` enum — 신규 값
`Assets/02.Scripts/Data/Enum/InteractionEnum.cs`

```csharp
public enum InteractionObjectType
{
    NONE = 0, TREE, STONE, FISHING_ZONE, GATERING_ZONE, NPC,
    REST_POINT,   // 파티 체력 회복 (모닥불/제단)
}
```

### ② `InteractableActorSO` — 회복 필드 추가
`Assets/02.Scripts/Data/Actor/InteractableActorSO.cs`

기존 SO가 이미 grab-bag(`hp`/`dropItems`를 무조건 보유)이므로 **서브클래싱 대신 필드 추가**가
기존 패턴에 부합한다. 서브클래싱은 액터에서 캐스팅을 강제할 뿐 이득이 없다.

```csharp
[Header("회복 (REST_POINT 전용)")]
public bool reviveDowned = true;     // HP 0 멤버도 부활 (이번 사양)
// 풀 회복 고정이라 ratio 필드는 생략. 추후 부분회복 원하면 [Range(0,1)] healRatio 추가.
```

### ③ `PlayerActor.HealCharacterToFull()` — 누락된 벤치 회복 API (신규)
`Assets/02.Scripts/GameActor/Object/Player/PlayerActor.cs`, `GetHealthForCharacter` 옆에 배치.

```csharp
/// <summary>지정 캐릭터를 풀 회복. reviveDowned=true면 HP 0(다운) 멤버도 되살린다.</summary>
public void HealCharacterToFull(CharacterActorType type, bool reviveDowned)
{
    if (type == _characterActorType)
    {
        // 액티브: 다운 상태면 player는 이미 사망 플로우이므로 정상 케이스는 생존.
        // 풀 회복 (Heal은 IsAlive 가드 → 직접 세팅으로 부활까지 커버)
        if (!reviveDowned && !IsAlive()) return;
        float old = _currentHealth;
        _currentHealth = _maxHealth;
        if (_currentHealth > old)
        {
            OnHpChanged?.Invoke(_currentHealth, _maxHealth);
            UIManager.Instance.ShowDamageFloaterHeal(transform.position, _currentHealth - old);
        }
        return;
    }

    // 벤치: _characterHealthMap 직접 기록 (이벤트 안 나감 → HUD 별도 갱신 필요, ⑥)
    float max = GetMaxHealthForCharacter(type);
    bool hasRecord = _characterHealthMap.TryGetValue(type, out float stored);
    if (!hasRecord) return;                       // 기록 없음 = 이미 풀피
    if (!reviveDowned && stored <= 0f) return;    // 부활 비활성 시 다운 제외
    _characterHealthMap[type] = max;
}
```

### ④ `PartyManager.HealAllParty()` — 오케스트레이션 (신규)
`Assets/02.Scripts/Manager/Party/PartyManager.cs`

```csharp
public event Action OnPartyHealthRefreshed;   // HUD 벤치 엔트리 일괄 갱신 신호

public void HealAllParty(bool reviveDowned)
{
    if (_player == null) return;
    foreach (var type in _roster)              // Roster 전체 (출전+대기 보유 전원)
        _player.HealCharacterToFull(type, reviveDowned);
    OnPartyHealthRefreshed?.Invoke();
}
```

> `_roster`를 도는 이유: 액티브 타입도 roster에 포함되며, `HealCharacterToFull` 내부의
> `type == _characterActorType` 분기로 자동 처리된다. 한 루프로 양쪽을 커버한다.

### ⑤ `RestPointActor` — 신규 인터렉션 액터
`Assets/02.Scripts/GameActor/Object/Prop/RestPointActor.cs`

`GatheringActor`를 모델로 하되 아이템 드랍/HP 파괴 로직 제거, 회복 호출로 대체.

```csharp
public class RestPointActor : GameActor, IInteractable
{
    [SerializeField] private InteractableActorSO _data;
    private bool _isInteracting;
    private GameActor _this;

    protected override void Awake()
    {
        base.Awake();
        _actorType = ActorType.Obstacle;
        _this = GetComponent<GameActor>();
    }

    public void Interact(GameActor user)
    {
        if (_isInteracting) return;
        _isInteracting = true;

        // 즉시 회복 (풀 회복 + 무제한 재사용이므로 애니 이벤트 대기 불필요)
        PartyManager.Instance?.HealAllParty(_data.reviveDowned);

        // 연출: FX/SFX (기존 키 재사용 또는 신규)
        GameObjectManager.Instance.ShowFX(FXKeyType.ItemArrivedToPlayerPos, transform.position);
    }

    public void StopInteract()           => _isInteracting = false;
    public bool CanInteract()            => true;     // 무제한 재사용 → 항상 가능
    public bool IsInteracting()          => _isInteracting;
    public GameActor GetActor()          => _this;
    public InteractableActorSO GetData() => _data;
    public void OnAnimationEvent<TData>(InteractionAnimEvent e, TData d) where TData : IEventData { }
}
```

> **회복 발동 타이밍**: 풀 회복·무제한 재사용이라 `Interact()`에서 **즉시 1회** 발동이 가장 단순.
> 채집(`OnHit` 모션이벤트)처럼 프레임 동기 연출을 원하면 `InteractionAnimEvent.OnHeal`을 추가하고
> `OnAnimationEvent`에서 호출하는 방식으로 확장 가능(⑦).

### ⑥ HUD 벤치 엔트리 갱신 — `UI_HUD_Party`
`Assets/02.Scripts/UI/HUD/Party/UI_HUD_Party.cs`

액티브는 `Heal()` → `OnHpChanged`로 자동 갱신되지만, **벤치 엔트리는 스왑 시점에만 갱신**된다
(`RefreshEntryValues`, `UI_HUD_Party.cs:194`). 직접 `_characterHealthMap`을 써도 이벤트가
나가지 않으므로 신규 이벤트 구독을 추가한다.

```csharp
// 구독부 (OnSwapCompleted 등록하는 곳 옆)
PartyManager.Instance.OnPartyHealthRefreshed += RefreshEntryValues;
// 해제부도 대칭으로 -=
```

`RefreshEntryValues()`는 이미 전 엔트리를 `GetHealthForCharacter`로 다시 그리므로 그대로 재사용된다.

### ⑦ (선택) `PlayerInteractionState` 애니메이션 분기
`Assets/02.Scripts/GameActor/State/Player/PlayerInteractionState.cs`

즉시 회복이면 별도 애니 없이도 동작하나, 휴식 모션을 넣으려면:
- `AnimKey`에 `Rest`(또는 `Pray`) 추가
- `PlayAnimation()`에 `case InteractionObjectType.REST_POINT:` → 마이닝식 **원샷 패턴**
  (재생 → `OnEnd`에서 상태 종료). 낚시 같은 루프 아님.
- 프레임 동기 회복이 필요하면 `InteractionAnimEvent`에 `OnHeal` 추가 후 모션 이벤트에서 호출.

---

## 4. 데이터 / 씬 셋업 (에디터 수동 작업)

> ⚠️ 코드(①~⑥)는 모두 구현 완료. **아래는 Unity 에디터에서만 가능한 수동 단계**로,
> 스크립트로 대체할 수 없다(.asset/.prefab 직접 편집은 깨지기 쉬움). 컴파일 통과 후 진행한다.

**① `InteractableActorSO` 회복 전용 에셋 생성**
   - `Project` 창 우클릭 → `Create > UPlayGround > ActorData > InteractableActorSO`
   - `interactionObjectType = REST_POINT`
   - `showInfoUI = false` (HP 보드 불필요)
   - `showShakeEffect = false` (정적 오브젝트 — 흔들림 연출 불필요)
   - `reviveDowned = true` (HP 0 멤버도 부활)
   - `hp` / `dropItems`는 REST_POINT에서 사용 안 함 → 비워둠

**② 휴식지점 프리팹 구성**
   - 빈 GameObject 또는 모닥불/제단 메시에:
     - `Collider` 부착, **`InteractionLayer` 레이어로 지정** (탐지 필수 조건)
     - `RestPointActor` 컴포넌트 부착
     - `RestPointActor._data`에 ①에서 만든 SO 할당
   - `GameInteractionHandler`가 `InteractionLayer` OverlapSphere로 자동 탐지하므로 별도 등록 불필요

**③ 씬 배치 & 검증**
   - 비전투 구역(휴식 공간)에 프리팹 배치 — §5대로 전투 중에는 탐지가 막힌다
   - 검증: 파티원 일부를 피격/다운시킨 뒤 상호작용 → 액티브 + 벤치 + 다운 멤버 전원 풀 HP 복귀,
     HUD 파티 엔트리도 즉시 갱신되는지 확인

---

## 5. 엣지 케이스 체크리스트

- **전투 중 차단**: `GameInteractionHandler.Update`가 `_player.IsInCombat`면 아이콘/탐지를
  중단한다(`GameInteractionHandler.cs:48`). 회복지점은 비전투 구역에서만 사용 (사양상 OK).
- **액티브가 다운 상태**: HP 0이면 게임은 `RespawnPopup` 플로우로 진입 → 휴식지점 상호작용 불가.
  `HealCharacterToFull`의 액티브 분기는 정상 케이스(생존) 위주이나 직접 세팅이라 안전.
- **기록 없는 멤버**(한 번도 출전/피격 안 함): 이미 풀피 → no-op (의도).
- **부활된 벤치 멤버 스왑 인**: `RefreshForCharacter`(`PlayerActor.cs:618`)가
  `_characterHealthMap`에서 복원하므로 풀 HP로 등장. ✔
- **무제한 재사용 연타**: `_isInteracting` 가드로 한 상호작용 세션 내 1회. `CanInteract()`는
  항상 true라 다음 진입은 가능(의도된 무제한).

---

## 6. 구현 순서 (의존도순)

1. ✅ enum + SO 필드 (①②) — 무의존
2. ✅ `PlayerActor.HealCharacterToFull` (③) — 핵심
3. ✅ `PartyManager.HealAllParty` + 이벤트 (④)
4. ✅ `RestPointActor` (⑤)
5. ✅ `UI_HUD_Party` 구독 (⑥)
6. ⏭️ (선택) 애니 분기 (⑦) — 즉시 회복 방식 채택으로 미적용
7. ⬜ 프리팹/씬 배치 → 인게임 검증 (**에디터 수동, §4 참조**)

---

## 7. 요약

핵심은 세 가지다.

- **③ `HealCharacterToFull`** — 벤치 회복 + 부활 가드 우회 (없던 API)
- **④ 단일 진입점 `HealAllParty`** — 액티브/벤치 분리 저장을 한 루프로 커버
- **⑥ 벤치 HUD 갱신 신호** — `_characterHealthMap` 직접 기록은 이벤트가 안 나가므로 명시적 갱신 필요

나머지는 기존 `GatheringActor` / 인터렉션 흐름의 평이한 복제다.
