# 컨텐츠 시스템 작업 지침

> 대상: 퀘스트·대화·아이템·제작·상호작용·트리거·흐름 그래프 등 **플레이어의 여정을 구성하는 데이터**를 만들거나 고치는 모든 작성자(사람과 AI)<br>
> 전투 데이터는 [COMBAT_SYSTEM_AUTHORING_GUIDE.md](COMBAT_SYSTEM_AUTHORING_GUIDE.md), 플롯·대사 문안은 [STORY_PLOT_AUTHORING_GUIDE.md](STORY_PLOT_AUTHORING_GUIDE.md)를 따른다.

---

## 1. 역할과 목표

이 문서를 들고 작업하는 사람은 **컨텐츠 디자이너**다. 요청받은 퀘스트 하나를 등록하는 것이 아니라, **플레이어가 스스로 다음 할 일을 찾고, 한 일이 쌓인다고 느끼는 흐름**을 만드는 것이 목표다.

품질 판정 기준은 셋이다.

1. **다음에 뭘 할지 안다.** 플레이어가 멈춰서 "이제 뭐 하지?"라고 묻는 순간이 없다.
2. **한 일이 남는다.** 들인 시간이 성장·해금·관계·지식 중 무엇으로든 축적된다.
3. **막히지 않는다.** 어떤 순서로 진행해도, 어떤 지점에서 저장하고 껐다 켜도 진행 불능이 생기지 않는다.

**컨텐츠의 최대 결함은 재미없음이 아니라 진행 불능이다.** 순서·저장·리셋 경계를 데이터 저작 단계에서 확정한다.

---

## 2. 시스템 지도 — 무엇을 어디에 저작하는가

| 영역 | 담당 매니저 | 데이터 위치 |
|---|---|---|
| 퀘스트 | `QuestManager` | `10.Datas/Quest/` (`QuestDatabase.asset`) |
| 대화 | `DialogueManager` | `10.Datas/Dialogue/` (`DialogueGraphSO`) |
| 스토리 진행 | `StoryManager` | `10.Datas/Story/` (`StoryEntrySO`) |
| 플래그 | `GlobalFlagManager` | 코드·데이터 양쪽에서 참조하는 문자열 키 |
| 아이템 | `ItemManager` / `InventoryManager` | `10.Datas/Item/` |
| 제작 | `RecipeManager` | `10.Datas/Craft/` (`RecipeDatabase.asset`) |
| 상호작용 오브젝트 | `InteractionRespawnManager` | `10.Datas/Actor/Interaction/` |
| 맵 트리거 | — (`TriggerComposer` 씬 컴포넌트) | `10.Datas/TriggerSystem/` |
| 흐름 오케스트레이션 | `FlowGraphManager` | `10.Datas/Flow/` (`FlowGraphSO`) |
| 도감 | — | `10.Datas/Codex/` |
| 사이클 런 | `CycleRunManager` 외 | `10.Datas/Cycle/` |
| 월드 상태·시간 | `WorldStateManager` / `GameTimeManager` | — |
| 가이드 팝업 | `GameGuideManager` | `10.Datas/Guide/` |

**모든 수치·구성은 ScriptableObject로 외부화한다.** 컨텐츠를 코드에 하드코딩하지 않는다. 코드는 규칙을 구현하고, 데이터가 내용을 갖는다.

---

## 3. 어느 시스템으로 만들 것인가 — 선택 기준

같은 결과를 여러 방법으로 만들 수 있다. **잘못 고르면 나중에 전부 옮겨야 한다.**

| 만들려는 것 | 쓸 것 | 쓰지 말 것 |
|---|---|---|
| 여러 시스템을 순서대로 엮는 연출·진행 (처치 → 플래그 → 대화 → 카메라 → 포털 → 퀘스트 완료) | **FlowGraph** | 트리거 여러 개를 사슬로 연결 |
| 특정 위치 진입/이벤트에 반응하는 단일 동작 | **TriggerComposer** (Source 1 + Condition 1 + Action 1) | 전용 MonoBehaviour 신규 작성 |
| 플레이어에게 **목표로 제시**되는 과업 | **Quest** | 플래그만으로 암묵 진행 |
| 대사 분기와 선택지 | **DialogueGraphSO** | 코드 분기 |
| 여러 시스템이 함께 읽어야 하는 진행 상태 | **GlobalFlag** | 매니저별 개별 변수 |

**판단 질문:** "이 흐름을 한눈에 보고 디버깅해야 하는가?" 그렇다면 FlowGraph다. FlowGraph는 흩어진 흐름 제어를 한 그래프에서 저작·검증·디버깅하려고 만든 계층이다.

**금지:** 같은 흐름을 트리거와 FlowGraph 양쪽에 중복으로 배선하지 않는다. 어느 쪽이 실제로 동작하는지 아무도 모르게 된다.

---

## 4. 저작 규칙

### 4.1 ID와 GUID는 자산이다

- **기존 ID·GUID를 절대 바꾸지 않는다.** 세이브 데이터, 다른 에셋의 참조, 텔레메트리가 전부 이 값에 묶여 있다. 표시 이름만 바꾼다.
- 새 ID는 기존 명명 규칙을 따른다. (`quest_main_001`, `Item_Monster_Common_Eye`)
- 데이터베이스 에셋(`QuestDatabase`, `RecipeDatabase` 등)에 **등록하는 것까지가 작업**이다. 에셋만 만들고 끝내지 않는다.

### 4.2 진행 상태와 저장

- **어떤 지점에서 저장하고 꺼도 복구되어야 한다.** 진행 상태를 런타임 변수에만 두지 않는다.
- 순서를 강제해야 하면 **조건으로 명시**한다. "보통은 이 순서로 하니까"에 의존하지 않는다. 플레이어는 반드시 예상 밖 순서로 진행한다.
- 되돌릴 수 없는 진행(소모, 파괴, 일회성 대화)은 **그 지점 이후 진행 불능이 없는지** 반드시 확인한다.
- 플래그 키는 의미가 드러나게 짓고, 한 플래그가 여러 의미를 겸하게 하지 않는다.

### 4.3 사이클 리셋 경계 — 이 프로젝트 고유

이 게임은 회차가 반복된다. **새 컨텐츠를 만들 때 리셋되는지 남는지를 반드시 결정하고 명시한다.**

| 회차마다 되돌아가는 것 | 회차를 넘어 남는 것 |
|---|---|
| 생활 사건(분실물 등), 상대 배치, 지역 보상 조합 | 성장·장비·BossAssist, 관계와 지식, 시점 인물 |

- 판단 근거는 `Assets/docs/cycle/10_CYCLE_STORY_STATE_BOUNDARY_SPEC.md`다.
- **애매하면 리셋 쪽이 아니라 명시 쪽을 택한다.** 결정하지 않은 상태로 두면 세이브·로드에서 조용히 깨진다.
- 사이클 보스의 `BossAssist` 영입과 `MonsterActor._recruitableAs`에 의한 파티 캐릭터 해금은 **완전히 다른 경로**다. 혼동하지 않는다.

### 4.4 보상 설계

- **모든 보상은 쓸 데가 있어야 한다.** 쓸 데 없는 아이템을 주면 보상이 아니라 인벤토리 부담이다.
- 새 아이템을 추가할 때 **획득처와 소비처를 동시에 만든다.** 한쪽만 있으면 미완성이다.
- 성장·해금·소비 재화는 성격이 다르다. 섞어서 주지 않는다.

### 4.5 플레이어 노출 텍스트

퀘스트 제목·목표 문구·HUD·아이템 이름과 설명·대사는 전부 플레이어 노출 텍스트다.

- **개발·시스템·기획 용어를 쓰지 않는다.** 내부 objective 타입은 유지하되 표시 문자열만 일상어로 쓴다.
- 역할명이 아니라 이름과 행동으로 제시한다. (`외곽 보스 3체 처치` ✗ → `대결에서 승리한다 (0/3)` ○)
- 상세 기준은 [STORY_PLOT_AUTHORING_GUIDE.md](STORY_PLOT_AUTHORING_GUIDE.md) 3절.

---

## 5. 안내와 피드백

컨텐츠는 **플레이어가 알아차려야 존재한다.**

- 새 목표가 생기면 즉시 보이게 한다. HUD·마커·알림 중 무엇으로 알릴지 저작 시점에 정한다.
- 조건을 만족하지 못해 막혔을 때, **무엇이 부족한지** 알려준다. 그냥 안 되는 것은 버그와 구분되지 않는다.
- 획득·완료·해금은 즉시 피드백한다. UI 연출 규칙은 [UI_UX_AUTHORING_GUIDE.md](UI_UX_AUTHORING_GUIDE.md)를 따른다.
- 새 시스템을 처음 만나는 지점에는 가이드를 붙인다. 단, 한 번에 하나씩만.

---

## 6. 검증

**컨텐츠는 반드시 직접 플레이해서 검증한다.** 데이터가 등록됐다는 것은 검증이 아니다.

| 항목 | 확인 방법 |
|---|---|
| 정상 순서 진행 | 의도한 순서대로 완주 |
| 역순·건너뛰기 | 예상 밖 순서로 진행해도 막히지 않는가 |
| 저장/로드 | 진행 중간에 저장 후 재시작 |
| 회차 반복 | 리셋·누적 경계가 의도대로인가 |
| 중복 실행 | 같은 트리거를 두 번 밟았을 때 |
| 텍스트 | 개발 용어 노출 0건 |

FlowGraph는 EditMode 3개 + PlayMode 수직 슬라이스 3개 테스트가 있다. 흐름을 고쳤으면 돌린다.

---

## 7. 최종 체크리스트

**설계**
- [ ] 이 컨텐츠로 플레이어가 얻는 것을 한 문장으로 말할 수 있다.
- [ ] FlowGraph / Trigger / Quest 중 올바른 도구를 골랐고, 중복 배선이 없다.
- [ ] 새 시스템을 만들기 전에 기존 시스템으로 표현 가능한지 검토했다.

**데이터**
- [ ] 기존 ID·GUID를 바꾸지 않았다.
- [ ] 데이터베이스 에셋에 등록까지 마쳤다.
- [ ] 수치·구성을 코드에 하드코딩하지 않았다.
- [ ] 리셋/누적 경계를 결정하고 명시했다.

**진행 안전성**
- [ ] 예상 밖 순서로 진행해도 막히지 않는다.
- [ ] 저장/로드 후 복구된다.
- [ ] 되돌릴 수 없는 지점 이후 진행 불능이 없다.
- [ ] 조건 미충족 시 무엇이 부족한지 알려준다.

**표현**
- [ ] 플레이어 노출 텍스트에 개발·시스템 용어가 없다.
- [ ] 새 목표·획득·완료가 즉시 피드백된다.
- [ ] 보상에 쓸 데가 있다.

**검증**
- [ ] 직접 플레이로 확인했고, 확인하지 못한 항목을 사실대로 밝혔다.

---

## 8. 권위 문서

- `Assets/docs/guide/FLOWGRAPH_SYSTEM_GUIDE.md` — 흐름 그래프 저작
- `Assets/docs/Complete/QUEST_SYSTEM_GUIDE.md` — 퀘스트
- `Assets/docs/Complete/DIALOGUE_SYSTEM_GUIDE.md` — 대화
- `Assets/docs/Complete/STORY_SYSTEM_GUIDE.md` — 스토리 진행
- `Assets/docs/Complete/ITEM_DATA_SYSTEM_GUIDE.md` / `ITEM_DROP_SYSTEM_GUIDE.md` — 아이템·드롭
- `Assets/docs/Complete/CRAFTING_SYSTEM_GUIDE.md` — 제작
- `Assets/docs/Complete/TRIGGER_SYSTEM_DESIGN.md` — 트리거
- `Assets/docs/Complete/SAVE_SYSTEM_GUIDE.md` — 저장 경계
- `Assets/docs/Complete/EVENT_MANAGER_GUIDE.md` — 이벤트 배선
- `Assets/docs/cycle/` — 사이클 런과 상태 경계 (특히 `10_CYCLE_STORY_STATE_BOUNDARY_SPEC.md`)
- `Assets/docs/guide/CRAFT_QUEST_UI_EDITOR_SETUP_GUIDE.md` — 에디터 세팅 절차
