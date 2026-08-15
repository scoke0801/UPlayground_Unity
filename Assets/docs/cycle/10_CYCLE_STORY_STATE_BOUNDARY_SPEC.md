# 반복/누적 상태 경계 구현 스펙

> 문서 버전: **v1.1-implemented**<br>
> 작성일: **2026-08-12** / 구현일: **2026-08-14**<br>
> 상태: **P0 구현 완료**<br>
> 서사 기준: [CYCLE_STORY_PLOT.md](CYCLE_STORY_PLOT.md)

## 1. 목적

첫 귀환에서 세계의 반복을 증명하면서도, 플레이어가 축적한 정체·지식·관계·성장이 회차를 넘어 남도록 저장 소유권과 초기화 시점을 고정한다.

P0에서는 범용 월드 리셋 계층을 새로 만들지 않는다. 분실물 앵커 하나만 기존 퀘스트·인벤토리 저장을 조합해 회차 한정 상태로 표현한다.

## 2. 현재 코드 사실

| 영역 | 현재 상태 | 구현 영향 |
|---|---|---|
| `GameSaveData.saveVersion` | `3.0` | 필드 추가는 기본값 호환으로 처리 가능 |
| `PartySaveData` | roster, battleOrder, activeIndex와 `storyProtagonistType` 저장 | 최초 선택 캐릭터를 세이브 단위로 보존 |
| `PartyManager` | 실제 적용된 시작 캐릭터를 Protagonist로 확정한 뒤 `_newGameStartingCharacter`를 `None`으로 정리 | 파티 교체와 무관한 영구 서사 화자 제공 |
| `WorldStateSaveData` | 처치·재스폰·소모 오브젝트는 새 게임에서만 전체 초기화 | 분실물 하나를 위해 범용 월드 리셋을 확장하지 않음 |
| `QuestSaveData` | 활성 퀘스트와 objective 진행도를 저장 | 앵커의 중간 진행 복원에 사용 |
| `InventorySaveData` | 아이템 보유 상태 저장 | 앵커 물건을 먼저 주운 상태 복원에 사용 |
| `CycleSaveData.assists` | 어시스트 획득·장착·진행 저장 | 관계 누적의 P0 증거로 사용 |
| `GlobalFlagManager` | bool 플래그를 새 게임 단위로 저장 | 반복 지식과 장면 게이트 저장에 사용 |
| `StorySaveData` | 전역 진행도와 완료 StoryEntry 저장 | SP20/30/40/50과 1회성 대사 유지 |

## 3. 상태 소유권 표

### 3.1 영구 상태

| 상태 | 저장 위치 | 쓰기 시점 | 초기화 | 소비자 |
|---|---|---|---|---|
| `storyProtagonistType` | `PartySaveData` + `PartyManager` 런타임 | 새 게임에서 실제 시작 모델 적용 성공 직후 | 새 게임 | 대화 UI, 대화 이력, 자기 조우, 스토리 조건 |
| `cycle.anchor.first_request_started` | `FlagSaveData` | 초회차 주민의 부탁 대화 완료 | 새 게임 | 초회차 오브젝트 생성, 요청 반복 방지 |
| `cycle.anchor.lostitem_resolved_once` | `FlagSaveData` | 초회차 분실물 퀘스트 완료 | 새 게임 | 앵커 분기, 기억 선택지, 첫 귀환 판정 |
| `cycle.story.first_return_started` | `FlagSaveData` | 첫 귀환 포털 정산 완료 직후 | 새 게임 | 반복 앵커 재수락·스폰, 귀환 장면 복원 |
| `cycle.anchor.first_return_request_heard` | `FlagSaveData` | 첫 귀환에 주민과 먼저 대화 | 새 게임 | 선회수/선대화 반환 반응 구분 |
| `cycle.anchor.first_return_anchor_completed` | `FlagSaveData` | 첫 귀환 분실물 반환과 주민 반응 완료 | 새 게임 | 안내인 대화 게이트, 중복 반환 방지 |
| `cycle.anchor.first_return_guide_completed` | `FlagSaveData` | 첫 귀환 안내인 대화 마지막 이벤트 | 새 게임 | SP30·quest_main_003 완료 게이트 |
| Story Progress 20/30/40/50 | `StorySaveData.progress` | 각 확정 트리거 | 새 게임 | 메인 퀘스트와 StoryEntry |
| BossAssist 로스터·장착·처치 횟수 | `CycleSaveData.assists` | 영입·장착·처치 판정 | 새 게임 | `BossAssistManager`, HUD |
| 레벨·장비·스킬 | 기존 Party/Inventory 저장 | 기존 성장 흐름 | 새 게임 | 전투·성장 시스템 |
| `cycle.story.next_route_unlocked` | `FlagSaveData` | SP50 엔딩 연출 완료 | 새 게임 | 지도·이동 UI |

`first_return_anchor_completed`는 SP30과 같은 의미가 아니다. 이 플래그는 주민 반응까지 끝났지만 안내인 대화나 SP30 전에 저장한 상태를 복원하기 위한 장면 게이트다.

### 3.2 회차 또는 장면 한정 상태

| 상태 | 표현 방식 | 저장/복원 | 종료 시 정리 |
|---|---|---|---|
| 현재 사이클 보스 배치·발견·승리 | 기존 `CycleRunState`/`CycleLayoutState` | `CycleSaveData` | 정산·포기 규칙 유지 |
| 분실물 퀘스트 활성 여부 | 반복 `QuestSO`의 활성 상태 | `QuestSaveData.activeQuests` | 반환 후 퀘스트 완료 |
| 분실물 획득 여부 | 앵커 아이템 보유 여부 | Inventory 저장 + ItemDeliver 목표 | 주민에게 전달할 때 1개 소비 |
| 분실물 월드 오브젝트 | 활성 퀘스트이며 아이템 미보유일 때 동적 생성 | 로드 시 조건으로 재생성 | 획득·반환·새 게임 시 제거 |
| 주민에게 먼저 말했는지 | `cycle.anchor.first_return_request_heard` 플래그 | FlagSaveData | 새 게임에서 초기화 |
| 자기 조우 여부 | 현재 `CycleBossPlacement.actorId`와 Protagonist 대조 | 현재 배치에서 매번 계산 | 회차 종료 |
| 동일 인물 Assist 차단 | 현재 배치에 같은 source actor가 남았는지 계산 | 저장 필드 추가 없음 | 해당 인물과의 대결 완료 즉시 해제 |

P0 앵커를 위해 `WorldStateSaveData.consumedInteractables`를 지웠다가 복원하지 않는다. 월드 영구 상태와 회차 상태의 경계를 섞지 않기 위해서다.

## 4. 분실물 상태 머신

분실물의 회차 상태는 별도 범용 `CycleLocal` 저장 DTO를 만들지 않고 다음 상태를 파생한다.

```text
Dormant
 └─ 앵커 Quest 수락 + 아이템 미보유 → AvailableInWorld

AvailableInWorld
 └─ 오브젝트 상호작용 → Carried

Carried
 └─ 주민에게 전달·아이템 소비 → Returned

Returned
 └─ 주민 반응 완료 → AnchorCompleted
```

| 파생 상태 | 판정 |
|---|---|
| `Dormant` | 앵커 퀘스트 비활성 |
| `AvailableInWorld` | 퀘스트 활성, 앵커 아이템 0개, 해당 회차 앵커 미완료 |
| `Carried` | 퀘스트 활성, 앵커 아이템 1개 이상 |
| `Returned` | ItemDeliver 완료, 주민 반응 미완료 |
| `AnchorCompleted` | 퀘스트 완료. 첫 귀환이면 `first_return_anchor_completed=true` |

복원 시 동적 오브젝트는 저장된 GameObject를 되살리는 대신 `AvailableInWorld` 조건에서 다시 생성한다. `Carried`이면 생성하지 않는다.

## 5. 초기화 경계

### 5.1 새 게임

모두 초기화한다.

- `storyProtagonistType=None`
- 앵커·귀환·새 경로 플래그 false
- Story Progress 0
- 분실물 퀘스트 활성/완료 기록 제거
- 앵커 아이템 제거
- BossAssist 로스터와 처치 횟수 제거
- 기존 성장·월드·사이클 새 게임 초기화 유지

### 5.2 첫 원정 시작 전

- 분실물 퀘스트를 수락하고 오브젝트를 생성한다.
- 아직 `CycleRunManager.StartNewCycle`을 실행하지 않는다.
- 초회차 앵커 완료 후에만 보스 배치와 전투 지도를 활성화한다.

현재 `UI_Scene_TitleMenu.StartNewGame`은 `RequestStartNewCycleOnNextWorld()`를 즉시 호출한다. 구현 시 이를 `RequestStartFirstCycleAfterStoryGate()`와 같은 명시적 게이트로 교체하거나, 기존 요청에 `Func<bool>`를 주입하지 말고 CycleRunManager가 영구 플래그/퀘스트 완료 상태를 확인한 뒤 시작하도록 한다. P0 권장안은 [12_LOOP_ANCHOR_QUEST_SPEC.md](12_LOOP_ANCHOR_QUEST_SPEC.md)의 오케스트레이션 계약을 따른다.

### 5.3 사이클 정산

- 기존 사이클 배치와 동적 전투 오브젝트는 정리한다.
- 첫 정산이면 `cycle.story.first_return_started=true`를 먼저 기록한다.
- SP30은 올리지 않는다.
- 반복 분실물 퀘스트를 다시 수락하고 오브젝트를 생성한다.
- 앵커 완료와 안내인 대화가 끝난 뒤에만 SP30을 올린다.

### 5.4 다음 사이클

- 분실물 아이템이 남아 있지 않은지 검증한다.
- BossAssist, Protagonist, 지식 플래그, 성장 상태는 유지한다.
- 새로운 보스 배치만 생성한다.

## 6. BossAssist P0 구현

승인된 플롯은 첫 회차에 관계가 실제 능력으로 남는 것을 요구한다. 호노카·보쿠세이·히치·릴리 정의를 `BossAssistDatabase_P0.asset`에 등록하고 모두 `requiredDefeatCount=1`로 두어 첫 대결 뒤 획득을 보장했다. 기존 `CycleSaveData.assists`가 로스터·장착·쿨다운을 회차와 저장 로드 뒤에도 유지한다.

같은 `sourceBossActorId`의 살아 있는 상대가 현재 월드에 존재하면 `BossAssistManager.RequestAssist`가 해당 Assist만 차단한다. 네 사이클 보스의 `_recruitableAs`는 `None`으로 유지해 플레이어블 해금과 혼입하지 않는다.

플롯이 특정 숙련 조건 달성을 강제하지 않으므로 브레이크 마무리나 노히트를 P0 관계 증명의 필수 조건으로 삼지 않는다.

## 7. 이전 세이브 호환

| 누락 상태 | 폴백 |
|---|---|
| `storyProtagonistType` 없음 | battleOrder 첫 유효 타입 → roster 첫 유효 타입 → Bokusei 모델 존재 시 Bokusei. 복원 후 다음 저장에 기록 |
| 앵커 지식 플래그 없음, SP30 이상 | 구버전 진행으로 간주해 앵커를 완료 처리하지 않는다. 최초 로드 시 호환 안내 후 현재 퀘스트 상태에 맞춰 안전 복원 |
| `first_return_started` 없음, 사이클 1회 이상 완료 | history의 완료 회차 수와 quest_main_003 상태를 대조해 한 번만 보정 |
| BossAssist 정의가 세이브 ID와 매칭되지 않음 | 로스터 ID를 보존하되 장착·사용 불가 경고. 임의 다른 Assist로 치환 금지 |

스토리 플래그 보정은 매 로드마다 반복하지 않도록 별도 마이그레이션 버전 또는 완료 플래그를 사용한다.

## 8. 실패 복구 규칙

- 분실물 퀘스트가 활성인데 오브젝트와 아이템이 모두 없으면 오브젝트를 재생성한다.
- 아이템을 보유한 상태로 로드하면 월드 오브젝트를 중복 생성하지 않고 ItemDeliver 목표를 그대로 유지한다.
- ItemDeliver 완료 뒤 아이템이 남으면 앵커 아이템만 제거한다. 일반 아이템은 건드리지 않는다.
- SP30 이상인데 `quest_main_003`이 활성 상태면 목표를 다시 지급하지 않고 완료 조건을 재평가한다.
- Protagonist가 유효하지 않으면 11번 문서의 폴백을 적용하고 한 번만 경고한다.
- 앵커 복구 실패는 사이클 시작으로 우회하지 않는다. 장르 전환 핵심 장면이므로 오류를 남기고 진행을 정지한다.

## 9. 검증 항목

1. 새 게임 직후 분실물 퀘스트가 먼저 나오고 보스 사이클은 시작되지 않는다.
2. 초회차 반환 뒤 `lostitem_resolved_once`가 저장된다.
3. 첫 원정 완료 뒤 포털 복귀만으로 SP30이 오르지 않는다.
4. 첫 귀환 앵커 중 저장·로드를 `AvailableInWorld`, `Carried`, `Returned` 각각에서 수행해 중복 오브젝트·아이템이 없다.
5. 안내인 대화 뒤에만 SP30, quest_main_003 완료, quest_main_004 수락이 순서대로 발생한다.
6. 첫 회차에서 얻은 대표 BossAssist가 다음 회차와 로드 뒤에도 유지되고 실제 사용된다.
7. 동일 인물이 미승리 상대로 남아 있을 때 해당 Assist만 차단되고, 대결 뒤 해제된다.
8. 파티 교체와 로드 뒤에도 Protagonist가 변하지 않는다.
9. 새 게임을 다시 시작하면 위 영구·회차 상태가 모두 초기화된다.

## 10. 구현 반영 대상

- `GameSaveData.cs`, `PartyManager.cs`, 파티 서비스 계약
- `CycleRunManager`의 첫 사이클 시작 게이트와 첫 정산 스토리 진행 호출
- 앵커 QuestSO·ItemSO·동적 오브젝트·NPC 상호작용 데이터
- `quest_main_003.asset`, 메인 StoryEntry/FlowGraph
- 대표 `BossAssistDefinitionSO`, `BossAssistDatabase_P0.asset`
- BossAssist 동일 인물 차단과 검증 테스트
