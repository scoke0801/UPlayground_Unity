# EventManager (타입 안전 이벤트 버스) 가이드

## 개요

전역에서 사용하는 **타입 안전 이벤트 버스**입니다. 이벤트 키는 임의의 enum, 페이로드는 `IEventData` 마커 인터페이스를 구현한 클래스 한 종이며, `EventManager`가 `(Type, int)` 키로 델리게이트를 보관해 발행/구독을 중계합니다.

핵심 특징:

- **enum + IEventData 페어** — 이벤트 키는 enum, 페이로드는 `IEventData` 구현 클래스로 시그니처가 컴파일 타임에 강제된다.
- **데이터 없는 이벤트도 지원** — `EmptyEventData` 또는 `Action`(파라미터 없는 오버로드)을 사용해 가벼운 알림 발행 가능.
- **씬 전환 자동 정리** — `OnSceneChanged`에서 모든 핸들러를 일괄 해제. 씬 잔존 액터의 좀비 구독을 차단.
- **싱글톤** — `BaseManager<EventManager>` 기반. `EventManager.Instance.Subscribe / Send / Unsubscribe`로 사용.
- **디버그 헬퍼** — `GetSubscriberCount`, `LogEventStatistics`로 구독 상태 확인 가능.

---

## 아키텍처

```
EventManager (BaseManager<EventManager>, IManager)
└── Dictionary<(Type enumType, int enumValue), Delegate> _eventTable
       │
       │ 동일 (TEnum, value) 쌍으로 발행/구독되는 모든 핸들러를 합산
       ▼
   Action<TData>  또는  Action  (오버로드 시그니처에 따라 결정)


이벤트 정의:
  Data/Enum/*.cs                     enum 정의 (PlayerEvent, QuestEvent 등)
       └─ key 역할

  Data/Event/*.cs                    IEventData 구현 클래스
  Data/Quest/QuestEventData.cs        └─ payload 역할

발행자:                                        구독자:
EventManager.Instance.Send(...)   ─────►  핸들러 (Action<TData>)
```

### 파일 구조

```
Assets/02.Scripts/
├── Manager/Event/
│   └── EventManager.cs                  Subscribe / Unsubscribe / Send + 디버그
├── Data/Event/
│   ├── IEventData.cs                    마커 인터페이스 + EmptyEventData
│   └── PlayerEventData.cs               PlayerEquipChangeEvent / PlayerInteractionEvent
├── Data/Enum/
│   ├── EventType.cs                     enum PlayerEvent
│   └── QuestEventType.cs                enum QuestEvent
└── Data/Quest/
    └── QuestEventData.cs                QuestStateEventData / QuestObjectiveEventData
```

---

## 핵심 클래스

### IEventData

이벤트 페이로드 마커 인터페이스. 페이로드 한 종에 대해 한 클래스를 정의한다.

```csharp
public interface IEventData { }

public class EmptyEventData : IEventData { }   // 데이터 없음 신호용
```

### EventManager

| API | 시그니처 | 용도 |
|-----|----------|------|
| `Subscribe<TEnum, TData>` | `(TEnum eventType, Action<TData> handler)` | 데이터 있는 이벤트 구독 |
| `Subscribe<TEnum>` | `(TEnum eventType, Action handler)` | 데이터 없는 이벤트 구독 |
| `Unsubscribe<TEnum, TData>` | `(TEnum eventType, Action<TData> handler)` | 데이터 있는 이벤트 해제 |
| `Unsubscribe<TEnum>` | `(TEnum eventType, Action handler)` | 데이터 없는 이벤트 해제 |
| `Send<TEnum, TData>` | `(TEnum eventType, TData data)` | 데이터 있는 이벤트 발송 |
| `Send<TEnum>` | `(TEnum eventType)` | 데이터 없는 이벤트 발송 |
| `GetSubscriberCount<TEnum>` | `(TEnum eventType) → int` | 디버그: 구독자 수 |
| `LogEventStatistics()` | — | 디버그: 전체 이벤트 통계 출력 |

타입 제약:
- `TEnum : System.Enum`
- `TData : IEventData`

내부 키: `(typeof(TEnum), Convert.ToInt32(eventValue))` — 같은 enum 값이라도 enum 타입이 다르면 다른 채널로 분리된다.

### IManager 생명주기 훅

| 훅 | 동작 |
|----|------|
| `Init` / `AfterInit` | 별도 작업 없음 |
| `Dispose` | `_eventTable.Clear()` |
| `OnSceneChanged` | `_eventTable.Clear()` — 씬이 바뀌면 좀비 구독 차단을 위해 전체 정리 |

> **중요:** `OnSceneChanged`에서 테이블을 비우므로, **씬 전환을 가로지르는 매니저 / DontDestroyOnLoad 오브젝트**는 씬 변경 후 다시 `Subscribe` 해야 한다.

### 정의된 이벤트 (현재 기준)

| Enum | 멤버 | 페이로드 | 용도 |
|------|------|----------|------|
| `PlayerEvent` | `ChangeWeapon` | `PlayerEquipChangeEvent` | 무기 교체 |
| `PlayerEvent` | `EquipItem` | `PlayerEquipChangeEvent` | 아이템 장착 |
| `PlayerEvent` | `UnEquipItem` | `PlayerEquipChangeEvent` | 아이템 해제 |
| `PlayerEvent` | `InteractionTargetDestroy` | `EmptyEventData` | 상호작용 대상 파괴 신호 |
| `QuestEvent` | `QuestAccepted` | `QuestStateEventData` | 퀘스트 수락 |
| `QuestEvent` | `QuestCompleted` | `QuestStateEventData` | 퀘스트 완료 |
| `QuestEvent` | `QuestFailed` | `QuestStateEventData` | 퀘스트 실패 |
| `QuestEvent` | `QuestObjectiveUpdated` | `QuestObjectiveEventData` | 목표 진행도 변경 |

페이로드 클래스:

```csharp
// PlayerEventData.cs
public class PlayerEquipChangeEvent : IEventData
{
    public int           itemKey;
    public bool          isEquip;
    public EquipPosition equipPosition;
    public WeaponType    weaponType;
}

// QuestEventData.cs
public class QuestStateEventData : IEventData
{
    public string QuestId;
}

public class QuestObjectiveEventData : IEventData
{
    public string QuestId;
    public string ObjectiveId;
    public int    CurrentCount;
    public int    RequiredCount;
}
```

---

## 사용 예시

### 1. 데이터 있는 이벤트 (구독 / 발송)

```csharp
// 발송 (UI_ItemPopup.cs)
var eventData = new PlayerEquipChangeEvent
{
    itemKey       = item.itemKey,
    isEquip       = true,
    equipPosition = item.equipPosition,
    weaponType    = item.weaponType,
};
EventManager.Instance.Send(PlayerEvent.EquipItem, eventData);
```

```csharp
// 구독 (PlayerEquipment.cs)
private void OnEnable()
{
    if (EventManager.Instance == null) return;
    EventManager.Instance.Subscribe<PlayerEvent, PlayerEquipChangeEvent>(
        PlayerEvent.ChangeWeapon, OnWeaponChanged);
    EventManager.Instance.Subscribe<PlayerEvent, PlayerEquipChangeEvent>(
        PlayerEvent.EquipItem,    OnEquipItem);
}

private void OnDisable()
{
    if (EventManager.Instance == null) return;
    EventManager.Instance.Unsubscribe<PlayerEvent, PlayerEquipChangeEvent>(
        PlayerEvent.ChangeWeapon, OnWeaponChanged);
    EventManager.Instance.Unsubscribe<PlayerEvent, PlayerEquipChangeEvent>(
        PlayerEvent.EquipItem,    OnEquipItem);
}

private void OnWeaponChanged(PlayerEquipChangeEvent e) { /* ... */ }
private void OnEquipItem    (PlayerEquipChangeEvent e) { /* ... */ }
```

### 2. 데이터 없는 이벤트 (EmptyEventData 패턴)

```csharp
// 발송 (GatheringActor.cs)
EventManager.Instance.Send(
    PlayerEvent.InteractionTargetDestroy,
    new EmptyEventData());
```

```csharp
// 구독 (PlayerInteractionState.cs)
EventManager.Instance.Subscribe<PlayerEvent, EmptyEventData>(
    PlayerEvent.InteractionTargetDestroy, OnTargetDestroyed);

// 종료 시
EventManager.Instance.Unsubscribe<PlayerEvent, EmptyEventData>(
    PlayerEvent.InteractionTargetDestroy, OnTargetDestroyed);
```

> 코드베이스의 컨벤션은 **데이터 없는 이벤트도 `EmptyEventData`를 명시적으로 보내는 패턴**을 사용. 무인자 오버로드(`Subscribe<TEnum>(TEnum, Action)`)도 지원하지만 실사용은 EmptyEventData 쪽이 일관적이다.

### 3. 매니저 → UI 알림 (퀘스트)

```csharp
// QuestManager.cs : 퀘스트 진행도 변경 시
private void SendObjectiveEvent(QuestRuntimeData runtime, QuestObjectiveData obj)
{
    int current = runtime.ObjectiveProgress.TryGetValue(obj.objectiveId, out var c) ? c : 0;
    EventManager.Instance.Send<QuestEvent, QuestObjectiveEventData>(
        QuestEvent.QuestObjectiveUpdated,
        new QuestObjectiveEventData
        {
            QuestId       = runtime.QuestSO.questId,
            ObjectiveId   = obj.objectiveId,
            CurrentCount  = current,
            RequiredCount = obj.requiredCount
        });
}
```

UI 측은 `QuestEvent.QuestObjectiveUpdated`만 구독하면 매니저 내부 구현과 결합되지 않은 채 진행도 변화에 반응할 수 있다.

### 4. 디버깅

```csharp
// 특정 이벤트의 구독자 수
int n = EventManager.Instance.GetSubscriberCount(PlayerEvent.EquipItem);

// 전체 이벤트 통계 콘솔 출력
EventManager.Instance.LogEventStatistics();
```

---

## 새 이벤트 추가 절차

1. **enum 정의 추가**
   - `Assets/02.Scripts/Data/Enum/` 아래 적절한 파일에 enum 멤버 추가.
   - 새로운 도메인이면 `{Domain}EventType.cs` 파일을 새로 만들고 `enum {Domain}Event`를 정의.
2. **페이로드 클래스 정의**
   - `Assets/02.Scripts/Data/Event/` 또는 도메인 폴더 (예: `Data/Quest/`) 아래에 `class XxxEventData : IEventData` 추가.
   - 데이터가 없는 알림이면 `EmptyEventData` 재사용.
3. **발송자 측에서 호출**
   - `EventManager.Instance.Send<TEnum, TData>(eventType, payload);`
4. **구독자 측에서 OnEnable/OnDisable에 등록/해제**
   - 수명 주기에 맞춰 반드시 페어로 작성. (자세한 내용은 *주의 사항* 참조)

---

## 주의 사항

- **OnSceneChanged 시 전체 핸들러 해제** — `EventManager`는 씬 전환 시 `_eventTable.Clear()`로 전부 비운다. **DontDestroyOnLoad 오브젝트**(매니저 등) 가 씬 경계를 넘어 이벤트를 받아야 한다면, 씬 전환 후 다시 Subscribe 해야 한다.
- **OnDestroy 누락 ➜ 좀비 구독.** 일반 씬 오브젝트도 OnDisable/OnDestroy에서 반드시 Unsubscribe할 것. (씬 전환 시 자동 정리되지만, 동일 씬 내 재활성화/풀링 케이스에서 중복 구독 위험.)
- **EventManager.Instance null 가드.** 부팅 직후나 종료 직전에는 인스턴스가 null일 수 있다. `PlayerEquipment` 처럼 `if (EventManager.Instance == null) return;` 패턴으로 가드한다.
- **(TEnum, value) 키 충돌.** 동일 enum의 같은 멤버에 서로 다른 페이로드 타입(`Action<TDataA>`, `Action<TDataB>`)을 모두 구독하면 내부 캐스트 시점에 한쪽이 무효화된다. **하나의 enum 멤버에는 하나의 페이로드 타입만** 매칭하는 것이 컨벤션.
- **데이터 없는 오버로드 vs EmptyEventData.** 두 방식 모두 지원되지만 같은 enum 멤버를 두 방식으로 동시에 구독하지 말 것 (위 항목과 같은 이유). 프로젝트에서는 **EmptyEventData를 명시적으로 보내는 쪽**을 선호한다.
- **순서/스레드.** Send는 동기 호출이며 구독 등록 순서대로 실행된다. 이벤트 핸들러 내부에서 같은 이벤트의 Subscribe/Unsubscribe를 호출하면 InvocationList 변경으로 인한 누락이 발생할 수 있으니 가급적 차후 프레임으로 미룰 것.
- **세이브 직렬화에는 string ID 사용.** 페이로드의 enum 값은 enum 재정의(값 셔플)에 취약하다. `QuestManager`처럼 영속화 데이터는 `string`(QuestId 등)으로 보관하는 것이 안전하다.

---

## 확장 포인트

### 도메인별 이벤트 enum 분리

`PlayerEvent`, `QuestEvent` 처럼 도메인별로 enum을 분리하면 (TEnum, value) 키가 자연스럽게 네임스페이스 역할을 한다. 신규 도메인 (`InventoryEvent`, `DialogueEvent`, `BattleEvent` 등) 추가는 enum 파일 하나 + 페이로드 클래스 추가만으로 가능하다.

### 페이로드 풀링 (Hot path 최적화)

이벤트 발송이 매 프레임 수십 회 이상 일어나는 hot path라면 페이로드 클래스의 GC 부담을 줄이기 위해 풀링 가능. 단, 핸들러가 페이로드를 비동기로 보관하지 않는다는 보장이 있어야 한다.

### 디버그 도구 확장

`LogEventStatistics()`를 기반으로 인스펙터 / 런타임 모니터 창을 만들어 다음을 보여주면 유용:

- enum 별 구독자 수
- 최근 N개 이벤트 발송 로그 (이벤트 키 + 페이로드 요약)
- 좀비 구독 감지 (`Target` 이 `null` 또는 destroyed 인 핸들러)
