# 반복 앵커 퀘스트·quest_main_003 구현 스펙

> 문서 버전: **v1.1-implemented**<br>
> 작성일: **2026-08-12** / 구현일: **2026-08-14**<br>
> 상태: **P0 코드·대사·QuestSO·FlowGraph 구현 완료**<br>
> 선행 문서: [10_CYCLE_STORY_STATE_BOUNDARY_SPEC.md](10_CYCLE_STORY_STATE_BOUNDARY_SPEC.md), [11_PROTAGONIST_DIALOGUE_CONTRACT_SPEC.md](11_PROTAGONIST_DIALOGUE_CONTRACT_SPEC.md)

## 1. 목표

초회차에는 평범한 60초 생활 심부름으로 이동·상호작용을 익히고, 첫 귀환에는 같은 사건을 플레이어가 먼저 해결할 수 있게 하여 세계 전체의 반복을 행동으로 증명한다.

이 장면은 선택 콘텐츠가 아니다. `quest_main_003`의 필수 흐름으로 편입하며, 안내인 대화가 끝나기 전에는 SP30과 다음 메인 퀘스트를 열지 않는다.

## 2. P0 콘텐츠 의존성

반복을 기억하지 못하는 주민과 기억하는 안내인은 합치지 않았다. LakeOfLife의 기존 비활성 `Npc_Mia` 모델을 런타임에 활성화하고, Resources의 전용 `NPC_CycleAnchor_Mia` 데이터로 교체한다. 씬 파일이 저장소에서 제외되어 있으므로 `SceneContext`와 `CycleLoopAnchorSpawner`가 설치를 책임진다.

| 필요 데이터 | P0 결정 |
|---|---|
| 분실물 주인 주민 | `Npc_Mia` / 전용 데이터 `NPC_CycleAnchor_Mia`, npcId `210101` |
| 분실물 | `MiasBlueRibbon`, itemId `250101`, 표시명 `파란 리본` |
| 초회차 위치 | Mia 근처 나무 울타리 옆 풀숲 |
| 첫 귀환 위치 | 초회차와 같은 위치 |
| 안내인 대화 위치 | 주민 바로 옆이 아닌 가까운 조용한 지점. 비밀 공간 아님 |

주민 모델을 확정하지 않은 상태에서 안내인에게 분실물 역할까지 겸임시키지 않는다. 에셋 제작 승인 전 위 네 데이터를 먼저 배정한다.

## 3. 데이터 식별자

### 3.1 고정 ID

| 종류 | ID |
|---|---|
| 앵커 반복 퀘스트 | `quest_cycle_anchor_lost_ribbon` |
| 첫 요청 들음 이벤트 | `cycle.anchor.request_started` |
| 물건 반환 이벤트 | `cycle.story.first_return_anchor_returned` |
| 첫 귀환 도착 이벤트 | `cycle.story.first_return_arrived` |
| 첫 귀환 안내인 대화 이벤트 | `cycle.story.first_return_guide_completed` |
| 앵커 FlowGraph | `FLOW_CycleStoryAnchor` |
| 기존 메인 FlowGraph | `FLOW_CycleQuestLine` 유지 |

### 3.2 전역 플래그

| 키 | 의미 |
|---|---|
| `cycle.anchor.first_request_started` | 초회차 주민의 부탁을 들음 |
| `cycle.anchor.lostitem_resolved_once` | 초회차 분실물 반환 완료 |
| `cycle.story.first_return_started` | 첫 귀환 포털 정산 완료 |
| `cycle.anchor.first_return_request_heard` | 첫 귀환에 주민과 먼저 대화함 |
| `cycle.anchor.first_return_anchor_completed` | 첫 귀환 분실물 반환·주민 반응 완료 |
| `cycle.anchor.first_return_guide_completed` | 첫 귀환 안내인 대화 완료 |

모두 새 게임 단위 영구 플래그다. `first_return_request_heard`는 P0 첫 귀환 분기에서만 쓰며 이후 일반 반복 앵커로 재사용하지 않는다.

### 3.3 확정 데이터 ID

- 주민 오브젝트 `Npc_Mia`, Quest npcId `210101`
- 분실물 ItemSO itemId `250101`
- 동적 오브젝트 `CycleAnchor_BlueRibbon`

기존 작업실 열쇠 `250001`을 분실물로 재사용하지 않는다. `quest_main_003` 보상에서도 해당 열쇠를 제거한다.

## 4. QuestObjective 확장

`quest_main_003`의 세 필수 단계를 HUD와 저장에 정직하게 표현하려면 일반 위치 목표를 가짜 이벤트로 사용해서는 안 된다. enum 끝에 다음 타입을 추가한다.

```csharp
public enum QuestObjectiveType
{
    // 기존 0~8 유지
    StoryEvent = 9,
}
```

`StoryEvent`는 `targetStringId`를 키로 사용한다.

```csharp
void NotifyStoryEvent(string eventId);
```

변경 계약:

- `QuestManager.NotifyStoryEvent`는 활성 퀘스트의 `StoryEvent` objective만 갱신한다.
- `IQuestFlowService`에 `NotifyStoryEvent`와 `TrackQuest`를 노출한다.
- FlowGraph에 `NotifyQuestStoryEventNode`와 `TrackQuestNode`를 추가한다.
- enum 중간에 삽입하지 않고 값 9로 끝에 추가하여 기존 YAML 값을 보존한다.
- `StoryEvent`는 월드/미니맵 마커를 만들지 않는다.
- 동일 eventId 중복 알림은 requiredCount를 넘겨 누적하지 않는다.

`ReachLocation`을 대신 사용하면 지도와 월드 마커가 가짜 위치를 찾게 되므로 금지한다.

## 5. 앵커 반복 QuestSO

```text
questId: quest_cycle_anchor_lost_ribbon
questName: 미아의 파란 리본
questType: Main
autoAcceptOnNewGame: false
isRepeatable: true
autoComplete: true
reward: 없음
```

목표:

| 순서 | objectiveId | type | target | 표시 문구 |
|---|---|---|---|---|
| 1 | `obj_anchor_request` | StoryEvent | `cycle.anchor.request_started` | `미아에게 잃어버린 물건에 관해 듣는다` |
| 2 | `obj_anchor_return` | ItemDeliver | npcId `210101` + itemId `250101` | `파란 리본을 찾아 미아에게 돌려준다` |

보상을 비워 두는 이유는 반복 퀘스트를 통한 골드·경험치 파밍을 막고, 보상이 사건의 서사적 기능보다 앞서지 않게 하기 위해서다.

## 6. quest_main_003 재구조

기존 ID와 GUID를 보존한다.

```text
questId: quest_main_003
questName: 귀환
shortSummary: 돌아온 마을에서 달라진 점을 확인한다.
requiredQuestIds: [quest_main_002]
requiredStoryProgress: 20
autoAcceptNextQuestIds: [quest_main_004]
isRepeatable: false
autoComplete: false
reward: gold 200, exp 200, item 없음
```

목표:

| 순서 | objectiveId | type | targetStringId | 표시 문구 |
|---|---|---|---|---|
| 1 | `obj_first_return_arrived` | StoryEvent | `cycle.story.first_return_arrived` | `귀환 포털을 이용해 시작 지점으로 돌아간다` |
| 2 | `obj_first_return_anchor` | StoryEvent | `cycle.story.first_return_anchor_returned` | `미아의 파란 리본을 다시 찾아 돌려준다` |
| 3 | `obj_first_return_guide` | StoryEvent | `cycle.story.first_return_guide_completed` | `안내인과 대화한다` |

마지막 objective 알림 뒤에도 자동 완료하지 않는다. FlowGraph가 SP30을 먼저 설정한 뒤 명시적으로 `CompleteQuest(quest_main_003)`를 호출한다.

## 7. 첫 사이클 시작 게이트

현재 타이틀은 새 게임에서 `RequestStartNewCycleOnNextWorld()`를 호출하고, 월드 준비 직후 사이클을 자동 시작한다. 이 요청 API는 유지하되 실제 시작만 스토리 게이트 뒤로 미룬다.

```text
UI_TitleMenu
→ RequestStartNewCycleOnNextWorld (pending 유지)
→ 시작 월드 준비
→ lostitem_resolved_once == false
   → FLOW_CycleStoryAnchor.new_game_anchor_ready 실행
   → 사이클 시작 보류
→ 초회차 앵커 완료 Flow의 마지막 이벤트
→ pending 요청 재평가
→ quest_main_001 수락 상태 확인
→ StartNewCycle
```

`TryStartRequestedCycle()`는 게이트가 닫혀 있을 때 `_startRequestedForNextWorld`를 지우지 않는다.

초회차 앵커가 끝난 뒤 FlowGraph가 `CycleStoryEvent.FirstAnchorGateCompleted`를 payload 없이 발행한다. `CycleRunManager`는 이 이벤트를 받아 pending 요청을 다시 평가한다. GlobalFlag 구독 순서에 기대어 전투가 퀘스트 수락보다 먼저 시작되지 않게 한다.

`quest_main_001.autoAcceptOnNewGame`은 기존대로 유지한다. 대신 앵커 Flow가 `quest_cycle_anchor_lost_ribbon`을 HUD 추적 대상으로 명시하고, 완료 뒤 `quest_main_001`로 되돌린다. 생활 튜토리얼 중에는 대결 목표가 추적되지 않고, 상대 마커도 실제 사이클 시작 전까지 생성되지 않는다. 초회차 앵커 완료 시 main001이 아직 Active가 아니면 한 번 수락을 보정한 뒤 추적한다.

## 8. 동적 분실물 오브젝트

P0 전용 `CycleLoopAnchorSpawner`를 `SceneContext`가 LakeOfLife 진입 시 런타임으로 설치한다. 범용 월드 리셋 매니저로 확장하지 않는다.

### 생성 조건

```text
quest_cycle_anchor_lost_ribbon가 Active
AND 인벤토리에 분실물 0개
AND (
    cycle.anchor.first_request_started == true
    OR cycle.story.first_return_started == true
)
```

이 조건 때문에:

- 초회차에는 부탁을 듣기 전 물건이 보이지 않는다.
- 첫 귀환에는 부탁을 듣기 전부터 같은 자리에 물건이 있다.
- 획득 후 저장·로드하면 인벤토리 상태로 인해 중복 생성되지 않는다.
- 월드 오브젝트 자체를 영구 소비 목록에 넣지 않아도 된다.

획득 시 일반 인벤토리 API로 1개를 지급한다. 반환은 `InventoryManager.DeliverItemToQuest` 경로를 사용해 아이템 소비와 ItemDeliver objective 갱신을 한 번에 처리한다.

## 9. 주민 대화 분기

주민의 기본 DialogueGraph는 전역 플래그, 퀘스트 상태, 아이템 보유 여부를 순서대로 검사한다. 이를 위해 다음 Dialogue Condition을 추가한다.

- `InventoryItemConditionSO`: 지정 itemId 보유 수량 비교
기존 `FlagConditionSO`와 `InventoryItemConditionSO`를 함께 사용한다. 퀘스트 상태는 FlowGraph가 보장하며 대화에 별도 QuestStatus 조건을 추가하지 않는다. 분기 결과는 다음과 같다.

### 9.1 초회차

```text
앵커 퀘스트 Active + first_request_started false
→ 평범한 부탁 대화
→ first_request_started=true
→ Flow가 request_heard objective 알림
→ 오브젝트 생성
```

물건 보유 후 상호작용:

```text
감사 반응
→ DeliverItemToQuest
→ lostitem_resolved_once=true
→ Flow가 앵커 퀘스트 완료, quest_main_001 수락, 시작 게이트 해제
```

### 9.2 첫 귀환 — 선회수

```text
first_return_started true
+ first_return_request_heard false
+ 분실물 보유
→ "잠깐, 내가 뭘 잃어버렸는지 아직 말도 안 했는데?" 계열 반응
→ DeliverItemToQuest
→ first_return_anchor_completed=true
```

대사는 샘플이며 데이터 승인 때 확정한다.

### 9.3 첫 귀환 — 먼저 대화

선택지:

- `[기억] 우물 뒤를 먼저 볼게.` — `lostitem_resolved_once=true`일 때만 표시
- 평범하게 부탁을 듣는다

어느 선택이든 `first_return_request_heard=true`로 만들고 Flow가 request objective를 알린다. 이후 물건을 가져오면 먼저 대화했다는 사실에 맞는 짧은 반환 반응을 쓴다.

### 9.4 기억 선택지를 쓰지 않은 경우

반환 반응이나 뒤이은 안내인 대사에서 같은 사건임을 한 줄로 환기한다. 플레이어가 선택지를 고르지 않았다는 이유로 장르 전환 정보를 잃지 않게 한다.

## 10. 안내인 흐름

`cycle.anchor.first_return_anchor_completed=true`가 된 뒤 안내인 기본 대화 그래프 최상단에 해당 분기를 추가한다. 진행도만으로 분기하면 포털 복귀 전에 대화가 소진될 수 있으므로 반드시 플래그를 함께 확인한다.

대화의 기능:

1. 규칙을 설명하지 않고 플레이어의 먼저 한 행동을 짚는다.
2. 안내인도 반복을 기억한다는 사실을 최소한으로 확인한다.
3. 주민을 배려해 조용한 곳에서 말한 것임을 보여준다.
4. 감시자·비밀 작전·몰래 넣은 열쇠를 언급하지 않는다.

대화 마지막 Event 노드에서 `cycle.anchor.first_return_guide_completed=true`를 설정한다. 대화 도중 중단되면 마지막 이벤트가 실행되지 않으므로 다시 상호작용해 완료할 수 있다.

## 11. FLOW_CycleStoryAnchor

기존 `FLOW_CycleQuestLine`에서 SP30 직접 설정 경로를 제거하고, 앵커 전용 그래프가 플래그 변화와 로드 복구를 담당한다.

### 11.1 진입점

| entryId | 트리거 | 노드 순서 |
|---|---|---|
| `new_game_anchor_ready` | CycleRunManager가 시작 월드 준비 후 수동 호출 | StartQuest(anchor) → TrackQuest(anchor) |
| `first_request_started` | OnFlagChanged | NotifyStoryEvent(request_heard) |
| `first_anchor_resolved` | OnFlagChanged lostitem_resolved_once | CompleteQuest(anchor) → main001 Active 확인/수락 보정 → TrackQuest(main001) → Publish FirstAnchorGateCompleted |
| `first_return_started` | OnFlagChanged | NotifyStoryEvent(first_return.arrived) → StartQuest(anchor) |
| `first_return_request_heard` | OnFlagChanged | NotifyStoryEvent(request_heard) |
| `first_return_anchor_resolved` | OnFlagChanged | NotifyStoryEvent(first_return.arrived, 멱등) → NotifyStoryEvent(anchor.returned) → TrackQuest(main003) |
| `first_return_guide_completed` | OnFlagChanged | SetStoryProgress(30) → NotifyStoryEvent(guide_talked) → CompleteQuest(main003) |
| `resume` | 씬 준비·세이브 로드 후 수동 호출 | 플래그·퀘스트 상태를 검사해 위 미완료 단계를 재실행 |

`repeatPolicy`는 로드 복구를 막지 않도록 `Always` 또는 멱등 재실행 가능한 정책을 쓴다. 중복 실행 안전성은 Quest/Flag/Progress의 현재 상태 검사로 보장한다.

### 11.2 FLOW_CycleQuestLine 변경

- 10: 세 번의 대결 완료 — 기존 기술 entryId를 유지할 수 있으나 에디터 라벨·주석은 플레이어 용어에 맞춘다.
- 20: 마지막 대결 완료.
- 30: 첫 정산 시 직접 실행하지 않는다. 기존 `first_settlement_completed` 진입은 제거하거나 `first_return_understood`로 교체하고 앵커 그래프만 호출한다.
- 40: 두 번째 귀환 완료.
- 50: 마지막 원정 완료.

`CycleRunManager.AdvanceCycleStoryProgressForSettlement`는 첫 정산에서 SP30을 올리지 않는다. 첫 정산은 `first_return_started` 플래그와 앵커 그래프 진입만 담당한다.

## 12. 로드 복구

FlowGraph의 `OnFlagChanged`는 저장 복원 때 발화하지 않으므로 `resume` 진입이 필수다.

| 복원 상태 | 처리 |
|---|---|
| 첫 요청 전 | 앵커 퀘스트 활성, 오브젝트 숨김, 주민 요청 가능 |
| 첫 요청 후·미획득 | request objective 보정, 오브젝트 생성 |
| 물건 보유 | 오브젝트 생성 안 함, 반환 대화 활성 |
| 초회차 반환 완료·사이클 미시작 | main001 수락 보정 후 pending 사이클 시작 |
| 첫 귀환 시작·앵커 미완료 | main003 도착 objective와 반복 앵커 퀘스트 보정, 오브젝트 생성 |
| 첫 귀환 앵커 완료·안내인 미완료 | main003 두 번째 objective 보정, 안내인 분기 활성 |
| 안내인 완료·SP30 미반영 | SP30 → 세 번째 objective → main003 완료 순서 재실행 |

복구는 완료된 대사를 임의로 다시 재생하지 않는다. 마지막 완료 플래그가 false인 대화만 다시 상호작용할 수 있게 한다.

## 13. 실패·악용 방지

- 앵커 아이템은 판매·버리기·창고 이동·제작 재료 사용을 금지한다.
- 한 번에 1개만 존재한다.
- 반복 QuestSO에는 반복 보상을 두지 않는다.
- 첫 사이클 시작 게이트는 개발 치트 외에는 건너뛸 수 없다.
- SP30 치트 사용 시 main003과 앵커 상태가 어긋났다는 개발 경고를 낸다.
- 주민이 없거나 spawn 위치가 유효하지 않으면 사이클을 시작해 우회하지 않고 명시적 오류를 낸다.
- 첫 귀환 포털 정산 직후 자동 저장한다. 앵커 반환·안내인 완료·새 경로 해금 플래그는 같은 프레임의 FlowGraph와 퀘스트 처리가 끝난 다음 매니저 업데이트에서 저장해 부분 저장을 막는다.

## 14. 검증 시나리오

1. 새 게임 시작 60초 안에 주민 요청·분실물 회수·반환을 끝낼 수 있다.
2. 분실물 반환 전에는 보스 마커와 `quest_main_001` HUD가 뜨지 않는다.
3. 초회차에는 요청 전에 분실물을 주울 수 없다.
4. 첫 귀환 직후 SP30이 오르지 않고 main003이 활성 상태다.
5. 첫 귀환에는 주민과 말하기 전 같은 자리의 분실물을 주울 수 있다.
6. 선회수, `[기억]` 선택, 평범 재해결 세 경로 모두 주민 반응과 안내인 대화로 합류한다.
7. 각 경로에서 SP30은 안내인 대화 마지막 이벤트 뒤 정확히 한 번 오른다.
8. SP30 뒤 main003 완료와 main004 자동 수락 순서가 맞다.
9. 앵커 각 상태에서 저장·로드해 오브젝트·아이템·퀘스트가 중복되지 않는다.
10. 타이틀에서 새 게임을 다시 시작하면 기존 앵커 플래그와 아이템이 남지 않는다.
11. HUD와 대사에 금지어가 노출되지 않는다.
12. 기존 작업실 열쇠 250001이 main003 보상으로 지급되지 않는다.

## 15. 데이터 제작 전 승인 항목

이 명세 승인 뒤에도 다음 네 콘텐츠 값이 정해지기 전에는 JSON·에셋을 만들지 않는다.

1. 주민으로 사용할 모델·이름·Actor ID
2. 분실물의 정체·표시명·아이콘·숫자 Item ID
3. 주민·분실물·안내인 대화 지점의 실제 맵 배치
4. 세 분기와 안내인 대사의 확정 문안

구현 코드는 위 값과 분리된 ID 계약을 먼저 만들 수 있지만, 데이터 저작은 네 항목 승인 후 진행한다.
