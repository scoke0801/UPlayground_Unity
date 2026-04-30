# GameObjectManager 가이드

## 개요

런타임 GameObject 스폰/등록의 허브 매니저입니다. **활성 플레이어 참조**, **모든 GameActor 등록 테이블**, **FX/Item/Weapon 인스턴스 생성**, **Interaction Handler**, **글로벌 타임스케일** 등을 단일 진입점으로 통합 제공합니다.

핵심 특징:

- **partial class 4개 파일 분할** — `GameObjectManager.cs`(베이스/액터 레지스트리), `.FX.cs`, `.Item.cs`, `.Weapon.cs`
- **활성 플레이어 트래킹** — `Player` 프로퍼티가 항상 현재 조작 중인 캐릭터를 가리킴 (PartyManager 교체 시 `SetActivePartyPlayer`로 갱신)
- **GameActor 자동 등록** — `RegisterActor` / `UnregisterActor`로 `_allActors` 리스트 관리, `OnActorRegistered/OnActorUnregistered` 이벤트 발화
- **Addressables 기반 DB 로드** — `FXPrefabDatabase`, `ItemActor` 프리팹을 비동기 로드, 로드 전 요청은 대기열로 보관 후 자동 플러시
- **자동 FX 수명 관리** — 스폰된 FX 인스턴스를 만료 시간과 함께 보관, `OnUpdate`에서 일괄 Destroy
- **Handler 슬롯** — `GameInteractionHandler` 등 GameHandlerBase를 등록해 Update/FixedUpdate를 위임받음

---

## 아키텍처

```
GameObjectManager (BaseManager<T>, IManager) ── partial 4 파일
│
├── 활성 플레이어                _player : PlayerActor
│      └── PartyManager.SetActiveCharacter ──► SetActivePartyPlayer(newPlayer)
│
├── 액터 레지스트리              _allActors : List<GameActor>
│      └── GameActor.OnEnable / OnDisable에서 Register / Unregister 호출
│      └── 이벤트: OnActorRegistered, OnActorUnregistered
│
├── Handler 슬롯                 _handlerList : List<GameHandlerBase>
│      └── GameInteractionHandler (인터랙션 아이콘 / 트리거)
│
├── FX 시스템 (.FX.cs)
│      ├── _fxPrefabDatabase      Addressables: "FXPrefabDatabase"
│      ├── ShowFX(key, pos, ...)
│      ├── RegisterFXInstance(go, lifeTime)
│      └── _pendingDestroyFXList  만료 시간 기반 GC
│
├── Item 시스템 (.Item.cs)
│      ├── _itemActorPrefab       Addressables: "ItemActor"
│      ├── SpawnItem(itemInstance, position)
│      └── _pendingItems          로드 전 요청 대기열
│
└── Weapon 시스템 (.Weapon.cs)
       └── CreateWeapon(itemKey)  EquipmentSO.equipmentPrefab Instantiate
```

### 파일 구조

```
Assets/02.Scripts/Manager/Object/
├── GameObjectManager.cs            베이스 + 액터 레지스트리 + 라이프사이클 + 글로벌 타임스케일
├── GameObjectManager.FX.cs         FX 스폰/소멸 (FXPrefabDatabase, FXKeyType)
├── GameObjectManager.Item.cs       ItemActor 프리팹 로드 + SpawnItem
└── GameObjectManager.Weapon.cs     EquipmentSO 기반 무기 인스턴스 생성

Assets/02.Scripts/Manager/Handler/
├── GameHandlerBase.cs              Init/AfterInit/Dispose/Update/FixedUpdate 추상
└── GameInteractionHandler.cs       플레이어 주변 IInteractable 탐색 + 아이콘 표시

Assets/02.Scripts/Data/Path/
├── FXPrefabDatabase.cs             [CreateAssetMenu] UPlayGround/PathDatabase/FX
└── FXKeyType.cs                    자동 생성 enum (ID Enum Generator)
```

---

## 핵심 클래스 / API

### GameObjectManager (베이스)

| API | 시그니처 | 용도 |
|-----|----------|------|
| `Player` | `PlayerActor (get)` | 현재 조작 중인 활성 PlayerActor 참조 |
| `AllActors` | `IReadOnlyList<GameActor>` | 등록된 모든 GameActor (몬스터 포함) |
| `InteractionHandler` | `GameInteractionHandler` | 인터랙션 핸들러 직접 접근 |
| `SetActivePartyPlayer(PlayerActor)` | — | 파티 교체 시 `_player` 갱신 |
| `RegisterActor(GameActor)` | — | 액터 등록. 중복 등록은 무시 |
| `UnregisterActor(GameActor)` | — | 액터 해제 |
| `CanInteract()` | `→ bool` | 현재 가까운 IInteractable이 있으면 true |
| `OnActorRegistered` / `OnActorUnregistered` | `event Action<GameActor>` | 액터 등록/해제 알림 |
| `SetGlobalTimeScaleExceptPlayer(scale, duration=0)` | — | 플레이어를 제외한 모든 액터의 `LocalTimeScale` 설정. duration > 0 이면 자동 복귀 |
| `ResetTimeScale()` | — | 위 함수 1.0f 호출 단축 |

### IManager 라이프사이클

| 훅 | 동작 |
|----|------|
| `Init` | `Player` 태그로 PlayerActor 탐색, Handler 리스트 구성, `LoadFXPrefabDatabase()` / `LoadItemActorPrefab()` 비동기 시작 |
| `AfterInit` | 모든 핸들러의 `AfterInit` 호출 |
| `OnUpdate` | 모든 핸들러 `Update` + `ProcessPendingDestroyFX` |
| `OnFixedUpdate` | 모든 핸들러 `FixedUpdate` |
| `OnSceneChanged` | Player 레퍼런스 재수집 + 모든 핸들러 `Init` 재호출 |
| `Dispose` | 모든 핸들러 `Dispose` + ItemActor Addressables Release |

### FX API (`GameObjectManager.FX.cs`)

```csharp
// 1. FXKeyType 기반 (권장)
GameObjectManager.Instance.ShowFX(
    FXKeyType.PlayerHeal,
    transform.position,
    rotation: Quaternion.identity,
    parent:   transform,
    duration: 3f);

// 2. 문자열 키 기반 (자동 생성 enum 외부 키 사용 시)
GameObjectManager.Instance.ShowFX("MyCustomFX", pos);

// 3. 외부에서 생성한 FX 인스턴스를 자동 정리에 등록
GameObjectManager.Instance.RegisterFXInstance(go, lifeTime: 2f);
```

특징:

- `rotation == default(Quaternion)` 이면 프리팹 자체 회전을 그대로 사용. 외부에서 회전을 지정하면 `지정회전 * 프리팹회전`으로 합성하여 프리팹 로컬 오프셋(예: -90,0,0) 보존.
- `duration > 0` 이면 만료 시간을 보관해 `OnUpdate`에서 `Destroy`. `duration <= 0` 이면 호출자가 수명 관리.

### Item API (`GameObjectManager.Item.cs`)

```csharp
// 드랍/스폰 시
GameObjectManager.Instance.SpawnItem(itemInstance, dropPosition);
```

특징:

- `ItemActor` 프리팹은 Addressables 키 `"ItemActor"`에서 비동기 로드.
- 로드 완료 전 `SpawnItem` 호출은 `_pendingItems`에 적재 → 로드 완료 시 `FlushPendingItems`로 일괄 소비.
- `Dispose`에서 Addressables 핸들 Release.

### Weapon API (`GameObjectManager.Weapon.cs`)

```csharp
// PlayerEquipment에서 호출되는 패턴
GameObject weaponGo = GameObjectManager.Instance.CreateWeapon(itemKey);
```

- `ItemManager.GetItemData(itemKey)`로 `EquipmentSO`를 조회.
- `equipmentPrefab`을 `Instantiate`하여 반환. (소켓 부착·트랜스폼 조정은 호출자 책임)

### FXPrefabDatabase (SO)

| 항목 | 값 |
|------|-----|
| 메뉴 | `Create → UPlayGround/PathDatabase/FX` |
| Addressables 키 | `FXPrefabDatabase` |
| 엔트리 | `key`(string), `prefab`(GameObject), `description`(string) |
| 초기화 | 매니저가 Addressables 로드 후 `Initialize()` 호출 → 내부 Dictionary 구축 |

### FXKeyType (자동 생성 enum)

`ID Enum Generator` (`UPlayGround/Util/ID Enum Generator`)로 생성. 코드는 `FXKeyType.X.ToKey()`로 enum → string 변환 후 DB 조회.

### GameInteractionHandler

플레이어 주변 IInteractable을 매 프레임 탐색해 가장 가까운 대상 1개를 추적하고, 인터랙션 키 UI 아이콘을 표시.

| 메서드 | 동작 |
|--------|------|
| `CurrentClosestInteractable` | 현재 추적 중인 대상 |
| `StartInteraction()` | `Interact(player)` 호출 |
| `StopInteraction()` | `StopInteract()` + 진행도 UI 숨김 + 대기 코루틴 중단 |
| `SetWaitEvent(Action)` | 3~7초 랜덤 후 콜백 (낚시 등 대기 이벤트용) |

탐색 규칙: `Physics.OverlapSphere(playerPos, player.InteractionRadius, player.InteractionLayer)` → `IInteractable.CanInteract()` 통과한 대상 중 sqrMagnitude 최소값.

> **전투 중 비활성:** `_player.IsInCombat == true` 일 때는 아이콘이 강제 숨김 처리되어 인터랙션을 잠시 차단.

---

## 사용 예시

### 1. 액터 등록 (GameActor에서 자동)

`GameActor.OnEnable / OnDisable`에서 자동으로 `RegisterActor / UnregisterActor`가 호출되므로, 일반 액터는 별도 작업 불필요. 외부에서 활용 시:

```csharp
GameObjectManager.Instance.OnActorRegistered += actor =>
{
    if (actor is MonsterActor monster)
        miniMap.AddMarker(monster);
};
```

### 2. 데미지 히트 시 FX 스폰

```csharp
public void OnHit(Vector3 hitPoint, Vector3 hitNormal)
{
    GameObjectManager.Instance.ShowFX(
        FXKeyType.DefaultCombatHit,
        hitPoint,
        Quaternion.LookRotation(hitNormal),
        parent: null,
        duration: 1.5f);
}
```

### 3. 몬스터 사망 시 아이템 드랍

```csharp
foreach (var instance in dropTable.Roll())
{
    Vector3 pos = transform.position + Random.insideUnitSphere * 0.5f;
    GameObjectManager.Instance.SpawnItem(instance, pos);
}
```

### 4. 글로벌 타임스케일 (히트스탑·집중 효과)

```csharp
// 0.2배속으로 0.5초간 (플레이어 제외)
GameObjectManager.Instance.SetGlobalTimeScaleExceptPlayer(0.2f, duration: 0.5f);
```

### 5. 활성 캐릭터 교체 (PartyManager에서)

```csharp
// 캐릭터 스왑 후
GameObjectManager.Instance.SetActivePartyPlayer(newActivePlayer);
```

이후 모든 코드가 `GameObjectManager.Instance.Player`로 활성 캐릭터에 접근 가능.

---

## 셋업 방법

1. **FXPrefabDatabase 생성**
   - Project 우클릭 → `Create → UPlayGround/PathDatabase/FX`
   - Addressables 그룹에 추가하고 키를 `FXPrefabDatabase`로 설정
2. **FX 프리팹 등록**
   - 인스펙터에서 `prefabs` 리스트에 `key + prefab` 쌍 추가
3. **FXKeyType 생성**
   - Unity 메뉴 `UPlayGround → Util → ID Enum Generator` (또는 `Generator Tool/ID Enum Generator`)
   - FX 카테고리 갱신해 `FXKeyType.cs` 자동 생성
4. **ItemActor 프리팹 등록**
   - 픽업 가능한 ItemActor 프리팹을 Addressables 키 `ItemActor`로 등록
5. **PlayerActor 태그 확인**
   - 씬 내 PlayerActor 게임오브젝트에 Unity 태그 `Player` 부여 (Init 시 `FindWithTag` 사용)
6. **GameManager 매니저 등록 확인**
   - `GameManager.InitializeManagers` 순서에서 `[7] GameObjectManager` 가 ItemManager(Item DB)보다 먼저 또는 Player 의존 매니저보다 먼저 초기화되는지 확인

---

## 주의 사항

- **Addressables 비동기 로드 race.** `FXPrefabDatabase` / `ItemActor` 프리팹은 `Init` 직후 비동기 로드된다. `ShowFX`는 미로드 시 에러 로그 후 null 반환, `SpawnItem`은 `_pendingItems`에 적재되었다가 자동 플러시. 부팅 직후 한두 프레임 호출은 묵음 처리될 수 있음을 인지.
- **`Player` 프로퍼티 null 가능성.** `Init` 단계 / 씬 전환 직후 / Player 태그 미부여 씬에서 null이 될 수 있음. 매 호출 시 null 체크 필수.
- **글로벌 타임스케일은 LocalTimeScale.** `SetGlobalTimeScaleExceptPlayer`는 `Time.timeScale`이 아니라 각 액터의 `LocalTimeScale`을 변경. duration 자동 복귀는 `WaitForSecondsRealtime` 기반이므로 시간 정지 중에도 정상 동작.
- **씬 전환 시 핸들러 재초기화.** `OnSceneChanged`는 모든 Handler의 `Init`을 재호출하므로, Handler 내부 상태는 씬 경계에서 초기화된다는 것을 전제로 작성할 것.
- **FX 회전 합성 규칙.** `rotation == default` 이면 프리팹 자체 회전 사용, 그 외에는 `rotation * prefabRot` 합성. **항상 identity를 명시적으로 넘기면 프리팹 로컬 회전이 무시**된다는 점에 주의.
- **`AllActors`는 풀링/중복 등록 보호.** `Contains` 체크로 중복 추가는 차단되지만, 풀링 시스템 도입 시 동일 프레임 내 Disable→Enable이 빠르게 이어지면 중복 이벤트 가능성 있으니 풀 측에서 보호.
- **InteractionHandler는 단일.** `_handlerList`에 한 번만 등록. 멀티 핸들러로 확장하려면 슬롯 추가 후 의존성을 직접 검증.

---

## 확장 포인트

### 새 Handler 추가

`GameHandlerBase`를 상속한 신규 핸들러를 만들고 `Init`에서 `_handlerList.Add(...)` 한 줄 추가하면 자동으로 Update / FixedUpdate / Dispose / 씬 전환 처리에 합류한다.

```csharp
public class CombatBroadcastHandler : GameHandlerBase
{
    public override void Update() { /* ... */ }
}

// GameObjectManager.Init() 안에
_handlerList.Add(new CombatBroadcastHandler());
```

### FX 풀링

현 구현은 매 호출마다 `Instantiate / Destroy`. hot path 최적화 시 `RegisterFXInstance` 메커니즘을 풀 반환 큐로 교체하면 외부 호출 코드는 변경 없이 풀링으로 전환 가능.

### 새 spawn 카테고리

`Item`, `Weapon`처럼 `partial class GameObjectManager`로 신규 파일을 추가해 `SpawnXxx` 메서드를 분리. 라이프사이클 훅(Init/Dispose)에 로드/해제 로직만 추가하면 베이스 partial이 자동으로 호출.

### 글로벌 타임스케일 정밀 제어

현재는 "플레이어 제외 모든 액터"에 일괄 적용. 액터 타입 / 태그 별 선택 적용이 필요하면 인자에 `Predicate<GameActor>` 또는 `LayerMask`를 추가하는 오버로드를 도입.
