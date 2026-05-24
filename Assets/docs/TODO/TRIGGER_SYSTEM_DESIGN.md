# 트리거 시스템 고도화 설계 (TRIGGER_SYSTEM_DESIGN)

> 상태: TODO / 설계 제안 단계
> 작성: Haiku 웹 조사 + Opus 분석 + 현 코드 매핑
> 대상 코드: `Assets/02.Scripts/Story/QuestTriggerZone.cs`, `StoryTriggerZone.cs`, `GameActor/Group/GroupSpawnTrigger.cs`, `GroupStoryTrigger.cs`, `Camera/CameraSnapshotSequenceTrigger.cs`

---

## 1. 문제 정의

현재 프로젝트의 맵 트리거는 5종이 별도 컴포넌트로 흩어져 있다.

| 컴포넌트 | 입력(Source) | 출력(Action) | 조건(Condition) |
|---|---|---|---|
| `QuestTriggerZone` | Collider Enter + `ActorType.Player` | `QuestManager.AcceptQuest`, `NotifyLocationReached` | 없음 (DB 로드 여부만) |
| `StoryTriggerZone` | Collider Enter + tag `Player` | `StoryManager.TryTriggerStory` | `StoryEntrySO.requiredProgress` (SO 내부) |
| `GroupSpawnTrigger` | Collider Enter + tag `Player` | `MonsterGroupController.Activate` | 없음 |
| `GroupStoryTrigger` | **이벤트** `OnGroupDefeated` | `StoryManager.SetProgress` + `TryTriggerStory` | 없음 |
| `CameraSnapshotSequenceTrigger` | Collider Enter + tag `Player` | `CameraManager.PushCameraSnapshotSequence` + `UnityEvent` | 없음 |

공통적으로 반복되는 코드: `_triggered` 플래그, `_triggerOnce`, `_disableColliderAfterTrigger`, Player 식별, `RequireComponent(Collider)`.

### 1.1 한계

1. **조건 게이팅 부재**: 진입만으로 발동. 퀘스트 단계, 글로벌 플래그, 시간대, 파티 구성, 적 생존 여부 등 복합 조건을 표현할 수 없다.
2. **트리거 간 체이닝 없음**: "스폰 → 카메라 → 대화 → 진행도" 같은 시퀀스를 한 트리거에 묶지 못한다. `CameraSnapshotSequenceTrigger`만 `UnityEvent`로 부분 지원.
3. **입력 채널이 콜라이더 한정**: `GroupStoryTrigger`처럼 이벤트 기반 트리거를 만들려면 별도 클래스를 또 작성해야 한다.
4. **데이터/코드 결합**: 어떤 매니저를 호출할지가 컴포넌트 클래스에 박혀 있다. 디자이너가 인스펙터에서 액션 종류를 갈아 끼울 수 없다.
5. **상태/재진입 정책의 중복**: `_triggerOnce`, `_disableColliderAfterTrigger`가 각 클래스마다 반복 구현됨.

---

## 2. AAA 게임 사례 요약

### 2.1 Unreal Engine

- **Trigger Volume** 자체는 "멍청한 센서". Box/Capsule/Sphere 콜라이더로 `OnComponentBeginOverlap` / `OnComponentEndOverlap`만 발화.
- **Level Blueprint Event Graph** / **Level Sequence Event Track** 이 발화 신호를 받아 액션 그래프로 라우팅.
- **Gameplay Ability System(GAS)** 의 Gameplay Tag 계층으로 조건을 hierarchical하게 평가 (AND/OR).
- **Smart Object**: 트리거 볼륨이 아니라 오브젝트 자체가 상호작용 방법(Gameplay Behavior Config 에셋)을 들고 있음.

### 2.2 Unity 공식 권장

- **ScriptableObject Event Channel**: 트리거 → SO 채널 `RaiseEvent()` → 다수 Listener (UI/Audio/Quest)에 broadcast. 중앙 디커플링.
- **Visual Scripting / Timeline Signal Track**: 시간 축 위에 이벤트를 키프레임으로 박는 방식.
- 데이터-드리븐 원칙: 조건/액션 리스트를 ScriptableObject 자산으로 외부화.

### 2.3 Skyrim / Fallout (Creation Kit)

- **Quest Stage** 모델: `Quest.SetStage(int)` 호출로 진행도 변경 → 그 stage에 묶인 fragment 스크립트가 자동 실행.
- 트리거는 `OnTriggerEnter`에서 `GetStage()` 확인 후 `SetStage()`를 호출하는 얇은 어댑터 역할.
- 조건/액션 로직은 Papyrus fragment에 분리.

### 2.4 Cyberpunk 2077 / Witcher 3

- REDscript / quest scene 그래프로 시나리오를 노드 기반 작성. 상세 사양은 비공개.

### 2.5 Hades

- 수천 개의 게임 상태 변수(무기, 보스 처치 횟수, 신의 조합 등)를 runtime에 유지.
- 트리거 = "변수 조합 + RNG + 일회성 플래그"의 복합 predicate로 NPC 대사를 조건부 재생.

### 2.6 인용 출처

- [Unreal Engine 5.7 — Trigger Volume Actors](https://dev.epicgames.com/documentation/en-us/unreal-engine/trigger-volume-actors-in-unreal-engine)
- [Unreal Engine 5.7 — Trigger Level Blueprint Events from Sequencer](https://dev.epicgames.com/documentation/en-us/unreal-engine/trigger-level-blueprint-events-from-sequencer-in-unreal-engine)
- [Unreal Engine 5.7 — Gameplay Ability System](https://dev.epicgames.com/documentation/en-us/unreal-engine/gameplay-ability-system-for-unreal-engine)
- [Unreal Engine 5 — Smart Objects Interaction with AI](https://dev.epicgames.com/community/learning/tutorials/BywO/smart-objects-interaction-with-ai-in-unreal-engine-5)
- [Unity — Use ScriptableObjects as Event Channels](https://unity.com/how-to/scriptableobjects-event-channels-game-code)
- [Game Programming Patterns — Event Queue](https://gameprogrammingpatterns.com/event-queue.html)
- [Skyrim Papyrus — SetStage](https://papyrus.bellcube.dev/skyrimse/script/quest/function/setstage/)

---

## 3. 반복 등장하는 핵심 패턴

| 패턴 | 의도 | 이 프로젝트와의 관계 |
|---|---|---|
| **Event Bus / Channel** | 트리거가 매니저를 직접 호출하지 않고 중앙 채널에 메시지 발행 | `EventManager`가 enum 기반 pub/sub로 이미 부분 구현 |
| **Predicate Chain (AND/OR)** | 복합 조건 평가 | `StoryEntrySO.requiredProgress`만 단일 조건 존재. 일반화 필요 |
| **Quest Stage Fragment** | Stage 변경 → 그 stage의 액션 묶음 자동 실행 | `StoryManager.SetProgress` + `StoryEntrySO.requiredProgress` 가 단편적 구현 |
| **Data-Driven Action List** | 트리거 액션을 SO 배열로 데이터화 | 미구현. 가장 큰 갭 |
| **Smart Object** | 오브젝트가 상호작용을 자체 보유 | 적용 우선순위 낮음 (NPC 확장 시 재검토) |

---

## 4. 설계 제안: 3-계층 분리

핵심 아이디어는 **하나의 트리거를 Source / Condition / Action 의 3계층으로 분리**하고, 모두 ScriptableObject로 외부화하는 것이다. `TriggerComposer` MonoBehaviour 한 종류만 씬에 배치하고, 인스펙터에서 3계층의 SO들을 조립한다.

```
[ TriggerSource ]  ─Raise─▶ [ TriggerCondition (AND/OR/NOT 트리) ]  ─Pass─▶ [ TriggerAction (순차/병렬) ]
```

### 4.1 TriggerSource (입력)

발화 신호의 출처. 추상 SO `TriggerSourceSO` + 콘크리트:

| 구현 | 의미 | 대체 대상 |
|---|---|---|
| `ColliderEnterSource` | 콜라이더 Enter + `ActorType` 필터 | QuestTrigger/StoryTrigger/GroupSpawn/CameraSnapshot의 입력부 |
| `ColliderExitSource` | 콜라이더 Exit | 신규 |
| `ActorEventSource` | `MonsterGroupController.OnGroupDefeated` 같은 액터 이벤트 | `GroupStoryTrigger`의 입력부 |
| `ManagerEventSource` | `EventManager`의 특정 enum 이벤트 | 신규 (퀘스트 완료, 아이템 획득 등으로 확장) |
| `FlagChangedSource` | `GlobalFlagManager`의 특정 플래그 변경 | 신규 |
| `TimerSource` | 일정 시간 후 / 주기 발화 | 신규 |
| `CompositeSource` | 여러 source의 OR 결합 | 신규 |

각 Source는 `TriggerComposer`에서 `Subscribe(Action onFire)` / `Unsubscribe` 형태로 작동.

### 4.2 TriggerCondition (게이트)

발화 신호가 들어오면 평가되는 술어. 추상 SO `TriggerConditionSO` + 콘크리트:

| 구현 | 평가 |
|---|---|
| `GlobalFlagCondition` | `GlobalFlagManager.GetFlag(key) == expected` |
| `StoryProgressCondition` | `StoryManager.GetProgress() >= min && <= max` |
| `QuestStageCondition` | `QuestManager`에서 특정 퀘스트의 단계 비교 |
| `PartyContainsCondition` | `PartyManager`에 특정 `CharacterActorType` 존재 여부 |
| `ActorAliveCondition` | 특정 `MonsterActor` / `MonsterGroupController` 생존 여부 |
| `TimeOfDayCondition` | `GameTimeManager` 시간대 비교 |
| `OncePerSaveCondition` | 세이브 슬롯에 영구 기록되는 일회성 플래그 |
| `RandomChanceCondition` | 확률 게이트 (Hades식 대사 분기) |
| `AndCondition` / `OrCondition` / `NotCondition` | 조합 트리 (`TriggerConditionSO[] children`) |

평가 함수 시그니처:
```csharp
public abstract bool Evaluate(TriggerContext ctx);
```

`TriggerContext`는 발화 원인을 담는다: `GameActor enteringActor`, `MonsterGroupController defeatedGroup`, `float elapsed` 등.

### 4.3 TriggerAction (액션)

조건 통과 시 실행할 명령. 추상 SO `TriggerActionSO` + 콘크리트:

| 구현 | 호출하는 매니저/대상 |
|---|---|
| `AcceptQuestAction` | `QuestManager.AcceptQuest(questId)` |
| `NotifyLocationAction` | `QuestManager.NotifyLocationReached(locationId)` |
| `TriggerStoryAction` | `StoryManager.TryTriggerStory(storyEntry)` |
| `SetStoryProgressAction` | `StoryManager.SetProgress(value)` |
| `SetFlagAction` | `GlobalFlagManager.SetFlag(key, value)` |
| `ActivateGroupAction` | `MonsterGroupController.Activate()` |
| `PlayCameraSnapshotAction` | `CameraManager.PushCameraSnapshotSequence(...)` |
| `RaiseManagerEventAction` | `EventManager.Send(enum, data)` |
| `DelayAction` | 코루틴/Task로 N초 대기 후 다음 액션 진행 |
| `UnityEventAction` | 인스펙터의 `UnityEvent` 발화 (탈출구) |
| `SequenceAction` | `TriggerActionSO[] steps` 를 순차 실행 |
| `ParallelAction` | 동시 실행 |

실행 함수 시그니처:
```csharp
public abstract UniTask Execute(TriggerContext ctx, CancellationToken ct);
```

비동기 반환으로 `DelayAction` 이나 카메라 시퀀스 완료 대기를 표현할 수 있다.

### 4.4 TriggerComposer (배치 유닛)

```csharp
public class TriggerComposer : MonoBehaviour
{
    [SerializeField] private string _triggerId;        // 세이브 식별자
    [SerializeField] private TriggerSourceSO _source;
    [SerializeField] private TriggerConditionSO _condition; // null이면 항상 통과
    [SerializeField] private TriggerActionSO _action;

    [Header("재진입 정책")]
    [SerializeField] private TriggerRepeatPolicy _repeat = TriggerRepeatPolicy.Once;
    [SerializeField] private float _cooldownSeconds;

    [Header("디버그")]
    [SerializeField] private bool _logVerbose;
}

public enum TriggerRepeatPolicy { Once, OncePerSession, Cooldown, Always }
```

`_triggered` 플래그/`_disableColliderAfterTrigger` 같은 잡다한 상태를 모두 이 한 곳에 모은다.

### 4.5 EventManager 와의 정합

`EventManager`는 enum + delegate 기반 pub/sub가 이미 구현되어 있다. 트리거 시스템은 이를 그대로 백본으로 쓴다.

- 새 enum `TriggerEventType` (또는 기존 enum들) 을 정의하고, `ManagerEventSource` / `RaiseManagerEventAction` 이 이 enum 위에서 동작.
- 씬 전환 시 `EventManager._eventTable.Clear()` 가 호출되므로, `TriggerComposer.OnEnable/OnDisable` 에서 재구독.

---

## 5. 마이그레이션 경로

### Phase 1 — 병행 도입 (비파괴)

1. `TriggerSourceSO`, `TriggerConditionSO`, `TriggerActionSO`, `TriggerContext`, `TriggerComposer` 골격 추가.
2. 가장 단순한 콘크리트만 먼저 구현: `ColliderEnterSource`, `GlobalFlagCondition`, `AndCondition`, `AcceptQuestAction`, `TriggerStoryAction`, `SequenceAction`, `DelayAction`.
3. 기존 5개 트리거는 그대로 유지. 신규 맵 영역만 `TriggerComposer` 로 배치하여 검증.

### Phase 2 — 사례 이식

대표 케이스 한 개씩 새 시스템으로 옮긴다.

- `QuestTriggerZone` → `ColliderEnterSource(ActorType.Player)` + `AcceptQuestAction(_questId)` + (옵션) `NotifyLocationAction(_locationId)` 의 `SequenceAction`.
- `GroupStoryTrigger` → `ActorEventSource(group, OnGroupDefeated)` + `SequenceAction(SetStoryProgressAction, TriggerStoryAction)`.
- `CameraSnapshotSequenceTrigger` → `ColliderEnterSource` + `PlayCameraSnapshotAction` + 후속 `UnityEventAction`(레거시 훅).

### Phase 3 — 마이그레이션 도구

기존 컴포넌트를 자동으로 `TriggerComposer` + SO 세트로 변환하는 에디터 메뉴 작성. 변환 후 원본 컴포넌트는 prefab/scene에서 제거.

### Phase 4 — 레거시 삭제

모든 사용처 마이그레이션 완료 후 5개 클래스 제거.

---

## 6. 디자이너용 UX 고려

- `TriggerComposer` 인스펙터에서 `_source` / `_condition` / `_action` 셀렉터를 트리 형태로 펼쳐 보여주는 커스텀 에디터(또는 SerializeReference + ManagedReference) 권장.
- Gizmo: source 종류에 따라 자동 표시 (Collider 형태, 연결선 등).
- Validation: `_source` 가 null이거나 `_action` 이 비어 있으면 인스펙터에 경고.
- 디버그 토글 (`_logVerbose`) 켜면 발화/조건평가/액션실행 단계가 콘솔에 로그.

---

## 7. 결정 보류 항목

- 액션 실행 비동기 라이브러리 (UniTask vs Coroutine) — 프로젝트 다른 시스템과 정합 필요.
- 세이브 정책: `Once` / `OncePerSession` 트리거의 발동 이력을 `GlobalFlagManager` 에 자동 저장할지, `TriggerComposer` 가 자체 키로 저장할지.
- Visual graph editor 도입 여부 (당장은 인스펙터로 충분).
- Smart Object 패턴은 NPC 인터랙션 시스템 재설계 시점에 재검토.

---

## 8. 요약

> AAA 게임은 "**트리거 볼륨은 멍청한 센서로 유지하고, 조건 평가/액션 라우팅을 별도 계층으로 분리**" 한다는 공통 원칙을 따른다. 이 프로젝트도 `Source / Condition / Action` 3계층 SO + `TriggerComposer` 컴포넌트로 통합하면, 조건 게이팅·트리거 체이닝·이벤트형 입력 확장이 자연스럽게 해결되고 5개 트리거 클래스가 1개로 수렴한다.
