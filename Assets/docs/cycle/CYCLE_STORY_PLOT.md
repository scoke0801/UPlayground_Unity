# 메인 스토리 플롯 확정본

> 문서 버전: **v1.2-quest-review**<br>
> 확정일: **2026-08-12** / P0 구현일: **2026-08-14** / 대사·퀘스트 정적 검토일: **2026-08-15**<br>
> 상태: **인과·구조 승인, P0 코드·데이터 구현 및 대사·퀘스트 정적 검토 완료 / Play Mode 연출 검증 대기**<br>
> 구현 계약: [10_CYCLE_STORY_STATE_BOUNDARY_SPEC.md](10_CYCLE_STORY_STATE_BOUNDARY_SPEC.md), [11_PROTAGONIST_DIALOGUE_CONTRACT_SPEC.md](11_PROTAGONIST_DIALOGUE_CONTRACT_SPEC.md), [12_LOOP_ANCHOR_QUEST_SPEC.md](12_LOOP_ANCHOR_QUEST_SPEC.md)

이 문서는 메인 스토리의 권위 소스다. 세계는 아무것도 심사하거나 평가하지 않는다. 구현 스펙의 기술 용어가 플레이어 문구나 대사에 노출되어서는 안 된다.

## 1. 언어 경계

플레이어 노출 텍스트에는 다음 표현을 쓰지 않는다.

- `외곽`, `중앙`, `관문지기`, `수호자`, `처치`, `버팀목`, `고정점`, `평가`, `심사`, `통과`, `회차`
- `OuterBoss`, `CentralBoss`, `CycleOuterBoss`는 내부 코드와 기술 문서에서만 사용한다.
- 기획 임시어 `고정점`도 플레이어에게 가르치지 않는다.

플레이어에게는 인물의 이름과 행동을 직접 제시한다.

- `대결에서 승리한다 (0/3)`
- `미확인 상대`
- `호노카와의 대결에서 승리한다`
- `마지막 상대의 위치가 드러났습니다`
- `귀환 포털이 나타났습니다`

## 2. 핵심 주제와 로그라인

**주제.** 반복은 벌도 시험도 아니다. 되감기는 세계 안에서 지식·관계·성장이 축적되고, 주인공은 그 축적으로 반복에 끌려다니는 대신 다음 목적지를 스스로 선택하게 된다.

**로그라인.**

> 계속 되돌아가는 세계에서, 선택한 한 사람이 매 회차 조금씩 더 많이 기억하고 더 많은 이들과 연결되며, 마침내 우연에 맡겨졌던 흐름을 스스로 다시 만들어 다음 길을 여는 이야기.

## 3. 반복 세계의 규칙

### 3.1 세계에는 의지가 없다

세계는 한 바퀴를 돌면 원점으로 되감기는 닫힌 흐름이다. 아무것도 시험하지 않고 아무도 평가하지 않는다.

되감길 때 특정 장소에서 흐름이 엉켜 고정되는 현상이 생긴다. 강한 인물일수록 그 현상에 오래 붙잡히지만, 인물의 의지까지 빼앗기지는 않는다. 각 인물은 세계의 명령이 아니라 자신의 성격과 사정으로 그 자리에 남고 대결에 응한다.

### 3.2 리셋과 누적

| 회차마다 되돌아가는 것 | 회차를 넘어 남는 것 |
|---|---|
| 분실물 앵커 사건과 오브젝트 | 선택한 주인공의 정체 |
| 세 상대의 위치와 조합 | 앵커를 해결한 지식 |
| 장소 주변의 고정 현상 | 상대와의 기억·신뢰·연결 |
| 지역 보상 조합 | 레벨·장비·BossAssist |
| P1의 일반 주민 위치·표면 대사 | 흐름을 다시 만드는 방법 |

P0에서 확실히 리셋시키는 생활 사건은 분실물 앵커 하나다. 반대로 누적은 성장, 기억 행동, 안내인의 반응, 다음 회차의 BossAssist 사용으로 여러 번 증명한다. 사라지는 것보다 남는 것을 더 또렷하게 보여 밝은 톤을 지킨다.

### 3.3 기억의 층위

| 인물 | 기억 | 표현 |
|---|---|---|
| 주인공 | 항상 기억 | 부탁 전에 움직이는 행동, `[기억]` 선택지 |
| 안내인 | 기억 | 주인공도 기억하는지 행동을 보고 확인 |
| 상대 일부 | 반복을 인식 | 관계 누적 후 태도와 도움의 변화 |
| 일반 주민 | 대부분 기억하지 못함 | 매 회차 첫 만남을 반복 |

## 4. 세 명, 마지막 상대, 귀환 포털의 인과

이 인과에는 심사관이 없다.

1. 세계가 되감길 때 세 장소 주변에 흐름이 고정되고, 각 장소에 강한 인물이 붙잡힌다.
2. 주인공은 그 인물과 대결한다. 대결 이유는 각 인물의 개인적 사정에서 나온다.
3. 주인공이 대결에서 승리하고 그 사람이 자리를 벗어나면 주변에 엉킨 흐름이 풀린다.
4. 세 장소의 흐름이 모두 풀리면 그동안 가려졌던 마지막 장소가 드러난다.
5. 마지막 상대와의 대결에서 승리하면 마지막 장소의 흐름도 풀린다.
6. 응축됐던 흐름이 원점으로 되돌아가며 귀환 포털을 형성한다. 포털은 누가 열어주는 문이 아니라 흐름이 빠져나가는 물길이다.

회차마다 상대의 위치와 조합이 달라지는 이유도 시험이 아니다. 흐름이 매번 완전히 같은 모양으로 가라앉지 않고, 플레이어가 지나간 자리가 다음 회차에 미세한 흔들림을 남기기 때문이다.

이 인과는 설명 대사가 아니라 환경 변화로 먼저 보여준다. 세 번의 대결 뒤 가려졌던 길이 드러나고, 마지막 대결 뒤 그 자리에서 귀환 포털이 형성되어야 한다. 안내인은 `저쪽을 봐. 전에는 보이지 않던 길이야.`처럼 관찰만 덧붙인다.

## 5. 전체 플롯

### 5.1 도입 — 선택한 사람이 주인공이다

새 게임에서 플레이어가 고른 캐릭터가 이 세이브의 서사 주인공 `Protagonist`가 된다. 현재 조작 중인 캐릭터 `Player`와 분리하며, 파티 교체와 세이브 로드 뒤에도 바뀌지 않는다. 플롯은 보쿠세이에게 고정되지 않는다.

### 5.2 첫 원정 — 평범한 하루처럼 시작한다

전투 지도가 열리기 전에 주민의 분실물을 찾아주는 60초 안팎의 이동·상호작용 튜토리얼을 수행한다. 리본을 돌려준 뒤 마을 밖 세 갈래가 끝마다 다시 마을로 이어지는 모습과 그 길목에 남은 세 사람의 위치가 지도에 함께 드러난다. 플레이어는 막힌 길의 까닭을 알아보기 위해 세 명을 원하는 순서로 만나고, 말로 풀리지 않는 대결에서 승리한다. 마지막 상대까지 이긴 다음 귀환 포털로 돌아간다.

플레이어는 이 단계의 목표를 세계관 설명 없이 다음처럼 말할 수 있어야 한다.

> 세 명과 대결해 이기고, 마지막 상대까지 이긴 뒤 포털로 돌아간다.

### 5.3 첫 귀환 — 세계가 되돌아갔음을 행동으로 안다

귀환 포털을 이용하면 같은 마을로 돌아온다. 같은 주민이 같은 물건을 다시 잃어버리며, 플레이어는 이전 위치를 기억한다.

다음 세 경로를 모두 지원한다.

1. 부탁을 듣기 전에 물건부터 가져온다.
2. 주민과 먼저 대화하고 `[기억] 나무 울타리 옆을 먼저 볼게.`를 선택한다.
3. 기억 선택지를 쓰지 않고 평범하게 다시 해결한다. 이 경우에도 주민 또는 안내인이 같은 사건임을 짧게 환기한다.

주민은 선회수 경로에서만 플레이어가 먼저 안다는 사실에 놀란다. 평범하게 다시 해결한 경로에서는 자신도 모르게 같은 말을 되풀이하는 작은 어긋남만 보인다. 안내인은 어느 경로에서도 사실과 다른 행동을 지적하지 않고, 같은 풀숲과 서로 다른 기억을 비교해 반복을 확인한다. 모르는 주민에게 혼란을 주지 않도록 가까운 조용한 곳에서 이야기하며, 감시자·관측 사각지대·비밀 열쇠 설정은 사용하지 않는다.

### 5.4 두 번째 원정 — 반복을 이용하고 관계를 확인한다

상대 배치는 달라졌지만 앞선 경험, 유지된 성장, 다음 회차에도 사용할 수 있는 BossAssist가 남는다. 플레이어는 반복이 실패 초기화가 아니라 지식과 관계를 축적하는 구조임을 시스템으로 확인한다.

### 5.5 마지막 원정 — 우연한 흐름을 다시 만든다

주인공은 축적한 지식과 관계를 이용해 이전에는 우연히만 생기던 흐름의 방향을 재현한다. 세계가 길을 승인하는 것이 아니다. 플레이어가 반복 속에서 방법을 익힌 결과다.

### 5.6 엔딩 — 시작의 마을이 출발의 마을이 된다

마지막 원정 뒤에도 귀환 포털은 기존 마을로 돌아온다. 안내인은 습관적으로 첫 안내를 시작했다가 말을 고친다.

> 어서 와, 여기는 시작의 마을——<br>
> 아니네. 여기는 이제 출발의 마을이야.

그 뒤 마을 지도나 환경에 전에 없던 경로가 드러나고 새 지역이 영구 선택지로 등록된다. 문 하나만 세계의 리셋을 예외적으로 버티는 것이 아니다. 주인공과 동료들이 방법을 기억하기 때문에 필요할 때 다시 열 수 있는 경로다. 새 지역의 실제 콘텐츠는 P1이어도 엔딩은 성립한다.

## 6. 감정 곡선

| 시점 | 플레이어의 질문 | 얻게 되는 답 | 다음 원정의 동력 |
|---|---|---|---|
| 첫 원정 | 막힌 길은 어떻게 움직이는가 | 세 사람이 자리를 뜨면 가려진 길이 드러난다 | 새로 드러난 길 끝이 궁금하다 |
| 첫 귀환 | 왜 원점으로 돌아왔나 | 세계 전체가 되감겼다 | 반복의 정체를 알고 싶다 |
| 두 번째 원정 | 내 지식이 통할까 | 반복은 이용할 수 있다 | 관계를 얼마나 이어갈 수 있나 |
| 관계 변화 | 상대도 나를 기억할까 | 연결이 실제 힘으로 남는다 | 더 많은 이와 함께하고 싶다 |
| 마지막 원정 | 반복 속에서 무엇을 바꿀 수 있나 | 흐름의 방향을 다시 만들 수 있다 | 다음 목적지를 직접 고르고 싶다 |

장르 인식의 정점은 마지막 전투가 아니라 첫 귀환의 분실물 앵커다.

## 7. 메인 퀘스트 라인

기존 `quest_main_001~005`의 ID와 GUID는 보존한다. 내부 objective 타입은 유지하되 플레이어 표시 문자열만 일상어로 바꾼다.

| ID | 제목 | 플레이 목표 | HUD | 완료 기준 |
|---|---|---|---|---|
| `quest_main_001` | 세 번의 대결 | 지도의 세 명과 자유 순서로 대결 | `대결에서 승리한다 (0/3)` | 내부 `CycleOuterBoss` 3회 |
| `quest_main_002` | 마지막 대결 | 드러난 마지막 상대와 대결 | `마지막 상대와의 대결에서 승리한다` | SP20 |
| `quest_main_003` | 귀환 | 귀환 후 반복을 확인하고 안내인과 대화 | 단계별 목표 표시 | 앵커 반환과 안내인 대화 뒤 SP30 |
| `quest_main_004` | 달라진 길 | 바뀐 지도에서 다시 귀환 포털을 찾는다 | `대결에서 승리한다 (0/3)` → `귀환한다` | SP40 |
| `quest_main_005` | 길을 여는 원정 | 마지막 원정 뒤 전에 없던 방향을 확인 | `대결에서 승리한다 (0/3)` → `귀환 뒤 새 길을 확인한다` | SP50 |

`quest_main_003`의 확정 순서는 다음과 같다.

```text
귀환 포털 이용
→ 같은 분실물 사건 재발
→ 분실물 반환
→ 주민 반응
→ 안내인 대화
→ SP30
→ quest_main_003 완료
→ quest_main_004 자동 수락
```

포털 복귀 순간에는 SP30을 올리지 않는다. 대신 `cycle.story.first_return_started`를 기록한다. SP30의 의미는 `첫 귀환 + 앵커 + 안내인 대화 완료`다.

### 7.1 퀘스트의 질문·행동·변화

퀘스트 문구는 앞으로 일어날 정답을 요약하지 않는다. 시작 시점에는 플레이어가 품을 질문과 당장 할 행동만 주고, 결과는 플레이와 완료 장면이 먼저 보여 준다.

| 퀘스트 | 시작하게 되는 이유 | 플레이어 행동 | 완료 뒤 보이는 변화 |
|---|---|---|---|
| 미아의 파란 리본 | 미아가 아끼는 물건을 잃어 도움을 청한다 | 부탁을 듣고 가까운 풀숲을 찾는다 | 리본을 돌려주고 마을 밖 지도가 열린다 |
| 세 번의 대결 | 세 갈래 길이 모두 막혀 있고 세 사람이 길목에 남아 있다 | 원하는 순서로 세 사람을 만나 대결한다 | 가려졌던 마지막 장소가 드러난다 |
| 마지막 대결 | 새로 드러난 길 끝에 마지막 상대가 기다린다 | 마지막 상대와 대결한다 | 귀환 포털이 나타난다 |
| 귀환 | 포털이 돌아갈 길을 열지만 어디로 이어지는지는 알 수 없다 | 포털을 이용하고 반복된 리본 사건을 확인한다 | 주인공과 안내인이 서로의 기억을 확인한다 |
| 달라진 길 | 같은 마을에서 지도와 사람들의 위치가 달라졌다 | 기억과 남은 관계를 이용해 다시 귀환한다 | 반복을 이용할 수 있다는 확신을 얻는다 |
| 길을 여는 원정 | 반복 속에서도 다음 목적지를 직접 고를 가능성이 생겼다 | 마지막 원정을 마치고 귀환 뒤 지도를 다시 살핀다 | 전에 없던 방향이 다음 목적지로 남는다 |

### 7.2 P0 퀘스트 노출 범위

P0의 `QuestDatabase`에는 위의 반복 앵커와 메인 퀘스트 5개만 노출한다. 다음 에셋은 GUID와 저작 초안을 보존하되 `isContentEnabled = false`로 유지한다.

- 레거시 `main_001`, `main_002`: 현행 플롯 이전의 정수 수집·스켈레톤 사냥 퀘스트다.
- `quest_sub_*` 6개: 내부 ID는 과거 등롱·약초·거미줄 의뢰를 가리키지만 현재 퀘스트 본문은 메인 진행도 요약으로 덮여 있어, 연결된 대화와 내용이 일치하지 않는다.

미완성 서브 퀘스트를 그대로 켜면 사건을 보기 전에 결말을 설명하고, 메인 퀘스트와 같은 진행도를 중복 추적하며, 같은 행동에 보상을 두 번 지급한다. P1에서 다시 활성화하려면 각 퀘스트가 아래 조건을 모두 만족해야 한다.

1. 이름과 사정이 있는 NPC가 직접 부탁하며, 시작 대화와 퀘스트 본문이 같은 사건을 가리킨다.
2. 메인 퀘스트 완료 조건을 그대로 재사용하지 않고 탐색·선택·관계 확인 중 하나의 고유 행동을 둔다.
3. 시작 문구는 결과가 아니라 질문을 제시하고, 완료 장면이 변화와 감정을 먼저 보여 준다.
4. 보상은 메인 진행의 중복 지급이 아니라 NPC의 사정과 플레이어 행동에 대응한다.
5. QuestSO·DialogueGraphSO·StoryEntrySO의 ID, 화자, 발동 시점이 모두 일치한다.

## 8. 인물 역할

### 8.1 Protagonist

새 게임에서 선택된 시점 인물이다. 특정 캐릭터의 성격이나 이름에 메인 플롯이 의존하지 않는다. `Player`는 현재 조작 캐릭터, `Protagonist`는 최초 선택 캐릭터다.

### 8.2 안내인

설명자가 아니라 매번 달라지는 길을 다시 그리는 사람이다. 반복을 기억하지만 주인공도 기억하는지는 확신하지 못했다. 같은 풀숲에 떨어진 리본과 주인공의 반응을 함께 보고 확인한다. 조용한 곳으로 이동하는 이유는 주민을 배려하기 위해서다.

- 왜 그 자리에 있는가: 되감긴 뒤 길을 잃는 사람이 없도록 기억을 바탕으로 지도를 다시 그린다.
- 왜 곧바로 전부 말하지 않는가: 기억하지 못하는 사람에게 지난 하루를 강요하지 않고, 주인공이 감당할 수 있는지 행동으로 확인하려 한다.
- 왜 남는가: 돌아오는 사람을 맞이하고 다음에 걸을 수 있는 길을 기록하기 위해서다.
- 무엇을 기억하는가: 달라진 길, 도움을 주고받은 사람, 주인공이 매번 먼저 알아보는 것.

### 8.3 미아

반복을 설명하기 위한 소품이 아니라, 아끼는 물건을 잃어버려 도움을 청하는 주민이다. 명확한 지난 하루의 기억은 남지 않지만, 같은 말을 되풀이할 때 잠깐의 낯익음이 몸에 먼저 나타난다.

- 왜 그 자리에 있는가: 우물에서 물을 긷고 마을 입구의 울타리를 지나 자기 일을 하러 가는 중이다.
- 왜 주인공에게 말을 거는가: 오래 매어 온 리본을 혼자 찾지 못해 가까이 있던 사람에게 도움을 청한다.
- 반환 뒤 무엇을 하는가: 고마움을 전하고, 같은 실수를 막으려 매듭을 두 번 짓는다.
- 다음에 무엇을 기억하는가: 의식적인 기억은 남지 않는다. 다만 같은 감사가 입에 먼저 붙는 짧은 낯익음만 보인다.

### 8.4 보스 인물과 일반 주민의 경계

보쿠세이·리안리안·호노카는 일반 주민 대화군이 아니라 **보스 인물**로 분류한다. 현재 P0 사이클 상대 풀에는 호노카·보쿠세이·히치와 마지막 상대 릴리가 들어가며, 리안리안이 현재 P0 배치에서 빠져 있더라도 주민으로 취급하지 않는다.

보스 인물의 대화는 마을 생활 대사가 아니라 다음 조우 단계에 속한다.

1. 접근 시 개인 사정과 대결 이유를 보여 주는 조우 대화
2. 승리 직후 자리를 떠나는 이유와 관계 변화를 보여 주는 결과 대화
3. 다음 귀환 이후 상대도 기억하는지를 확인하는 재조우 대화
4. BossAssist 획득·사용 뒤 연결이 남았음을 보여 주는 짧은 반응

`NPC_Bokusei`, `NPC_LianLian`, `NPC_Honoka`와 `DLG_Npc_*` 에셋은 과거의 일반 NPC 프록시다. 일반 주민 배치와 자동 생활 대화에서는 사용하지 않는다. 실제 보스 조우 대화는 해당 조우에 배치된 ActorId를 `partnerActorIdOverride`로 전달해, 같은 이름을 가진 플레이어·NPC·보스 표현 중 현재 상대를 명시적으로 가리켜야 한다.

### 8.5 세 상대와 마지막 상대

직업이나 역할명이 아니라 각자의 이름과 사정을 가진 인물이다. 같은 현상에 붙잡혀도 대결에 응하는 이유는 모두 달라야 한다. 대사 저작 전 각 인물에 대해 다음 네 질문을 답한다.

- 왜 그 자리에 남아 있는가?
- 왜 주인공과 대결하는가?
- 승리 뒤 왜 자리를 벗어나는가?
- 다음 회차에 무엇을 기억하는가?

어떤 답도 `세계가 시켜서`나 `맡은 역할이라서`가 되어서는 안 된다. 구체 동기와 전투 패턴의 연결은 P1 인물 문서에서 확정한다.

## 9. BossAssist와 관계

BossAssist는 파티 합류나 플레이어블 해금이 아니다. `_recruitableAs`에 의한 캐릭터 해금과 완전히 다른 경로다.

| 층위 | 내용 | 지속 |
|---|---|---|
| 한 번의 대결 | 이번 회차에서 인물 주변의 고정 현상을 푼다 | 회차 한정 |
| 관계 | 주인공과 상대의 기억·신뢰·연결 | 영구 |
| BossAssist | 누적된 연결로 지정된 힘을 한 번 보탠다 | 영구 획득 |

확정 서술은 다음과 같다.

> 매 회차의 고정은 되돌아오지만, 그들을 다시 움직이게 하는 방법과 서로의 신뢰는 남는다.

P0에서는 첫 회차에 최소 한 명의 BossAssist를 반드시 획득하고, 다음 회차에도 유지되어 실제 사용할 수 있어야 한다. `BossAssistDatabase_P0`에는 호노카·보쿠세이·히치·릴리 정의를 등록했고, 모두 첫 승리 뒤 획득되도록 `requiredDefeatCount=1`로 설정했다.

동일 인물이 현재 회차에서 아직 대결하지 않은 상대로 남아 있으면 그 인물의 Assist 사용을 P0에서 차단한다. 해당 대결이 끝난 뒤에는 사용할 수 있다. 정밀한 조우 중 차단이나 `빌려준 기술의 흔적` 연출은 P1에서 확장한다.

## 10. 자기 조우

현재 P0 상대 풀은 호노카·보쿠세이·히치 세 명과 마지막 상대 릴리다. 이 넷 중 하나를 주인공으로 선택하면 자기 조우를 피할 수 없으므로 최소 안전 반응은 P0다.

P0 요구:

- 주인공과 상대의 캐릭터 타입 또는 모델 일치를 감지한다.
- 데이터 중복처럼 보이지 않도록 1~2줄 또는 명확한 연출을 출력한다.
- 발화자는 현재 활성 캐릭터가 아니라 반드시 `Protagonist`다.
- 해당 인물의 BossAssist는 대결 전까지 차단한다.
- 정체는 설명하지 않는다.

P0 최소 대사는 정체를 설명하지 않고 서로의 차이를 의심하는 데서 멈춘다.

> 내 얼굴인데, 눈빛은 다르네.<br>
> 정말 다르다고 생각해?<br>
> 가까이 가 보면 알겠지.

P1에서만 전용 그래프와 정체 단서를 저작한다. 임시 설정 문구는 `세계가 아직 흘려보내지 못한 주인공의 모습`이며, 미래·과거·평행세계나 실패한 시간선으로 확정하지 않는다.

## 11. P0와 P1

### P0

- 반복/누적 상태 경계 구현
- `Protagonist` 저장·화자 계약
- 분실물 앵커의 초회차와 첫 귀환 3분기
- `quest_main_003` 재구조와 SP30 이동
- 메인 퀘스트/HUD/마커의 플레이어용 문구 교체
- 안내인의 최소 관찰 대사
- BossAssist 4종 데이터, 첫 승리 획득 보장, 다음 회차 유지·사용
- 동일 인물 상대가 남아 있을 때 해당 Assist 사용 차단
- 자기 조우 최소 안전 반응
- 엔딩에서 기존 마을 복귀 후 새 경로 노출

### P1

- 네 상대 각각의 개인 사정·승리 조건·재조우 기억을 먼저 확정한 뒤 전용 대사를 쓴다.
- 마지막 원정에서 플레이어가 실제로 수행할 `길의 방향을 바꾸는 행동`을 조작·환경·BossAssist와 연결해 확정한다. 이 행동이 정해지기 전에는 대사로 방법을 설명해 봉합하지 않는다.
- 상대별 개별 조우·재조우·BossAssist 대사
- 자기 조우 정체 규명과 전용 그래프
- 동일 인물 Assist의 `빌려준 기술·흔적` 치환 연출
- 일반 주민 전체 위치·대화 리셋
- 추가 반복 앵커
- 새 지역과 자유 원정 콘텐츠
- 안내인의 개인 작업실과 필요 시 직접 건네는 열쇠

## 12. 구현 경계

이 플롯의 인과와 구조는 v1.2까지 승인되었고, 아래 구현 명세에 따라 P0 코드·데이터가 반영되었다. 2026-08-15 정적 검토에서 첫 귀환 세 경로의 사실 관계, 미아의 기억 경계, 안내인의 설명량, 메인 퀘스트의 플레이어 용어와 실제 노출 범위를 다시 맞췄다.

- [10_CYCLE_STORY_STATE_BOUNDARY_SPEC.md](10_CYCLE_STORY_STATE_BOUNDARY_SPEC.md): 무엇이 리셋되고 무엇이 저장되는가
- [11_PROTAGONIST_DIALOGUE_CONTRACT_SPEC.md](11_PROTAGONIST_DIALOGUE_CONTRACT_SPEC.md): 선택 캐릭터 저장, 화자와 초상화 해석
- [12_LOOP_ANCHOR_QUEST_SPEC.md](12_LOOP_ANCHOR_QUEST_SPEC.md): 첫 원정 게이트, 분실물 3분기, `quest_main_003`, SP30

P1 상대별 전용 대사·자기 조우 정체 규명·새 지역 콘텐츠는 계속 이 문서의 범위 밖이다. 정적 검토는 대화 그래프 연결과 문안만 보장하며, 실제 카메라·동선·환경 변화가 대사보다 먼저 보이는지는 Unity Play Mode에서 별도로 확인해야 한다.

---

## 13. Main Story Generator 데이터

아래 marker 사이 JSON은 `MainStoryGeneratorWindow`가 읽는 권위 데이터다. 본문의 퀘스트 ID, 목표, 보상, 자동 연계가 바뀌면 이 블록도 함께 고친다. 본문만 고치고 블록을 두면 생성 버튼을 누르는 순간 본문이 아니라 이 JSON이 에셋에 반영된다.

현재 메인 스토리 대화와 스토리 엔트리는 모두 걷어낸 상태라 `dialogues`와 `stories`는 비어 있고, `Resources/MainStorySequence`의 자동 재생 목록도 비어 있다. 새 대화를 붙일 때 이 두 배열을 채우면 생성기가 대화 그래프와 스토리 엔트리, 시퀀스 등록까지 함께 만든다. 대화는 `dialogues[].lines[]` 순서대로 Talk 노드가 되며, 각 줄이 자신의 `channel`, `speakerId`, `text`를 가진다.

`isContentEnabled`와 `autoComplete`는 에셋의 현재 상태를 그대로 옮긴 값이므로, 콘텐츠를 열고 닫을 때 이 값을 함께 바꾼다.

<!-- STORY_GENERATOR_MAIN_BEGIN -->
```json
{
  "quests": [
    {
      "questId": "quest_main_001",
      "questName": "세 번의 대결",
      "shortSummary": "지도에 표시된 세 사람과 원하는 순서로 대결한다.",
      "description": "마을 밖 세 갈래가 모두 막혀 있고, 길목마다 한 사람이 남아 있다. 원하는 순서로 찾아가 막힌 길의 까닭을 확인하자.",
      "requiredProgress": 0,
      "rewardGold": 100,
      "rewardExp": 100,
      "isRepeatable": false,
      "autoComplete": true,
      "autoAcceptOnNewGame": false,
      "requiredQuestIds": [],
      "autoAcceptNextQuestIds": [
        "quest_main_002"
      ],
      "isContentEnabled": true,
      "objectives": [
        {
          "objectiveId": "obj_cycle_outer_guardians",
          "description": "대결에서 승리한다.",
          "type": "CycleOuterBoss",
          "targetId": 0,
          "targetStringId": "",
          "requiredCount": 3
        }
      ],
      "dialogues": [],
      "stories": []
    },
    {
      "questId": "quest_main_002",
      "questName": "마지막 대결",
      "shortSummary": "새로 드러난 마지막 상대와 대결한다.",
      "description": "세 번의 대결이 끝나자 전에는 보이지 않던 길과 마지막 상대의 위치가 드러났다. 그곳으로 가 대결에서 승리하자.",
      "requiredProgress": 0,
      "rewardGold": 150,
      "rewardExp": 150,
      "isRepeatable": false,
      "autoComplete": true,
      "autoAcceptOnNewGame": false,
      "requiredQuestIds": [
        "quest_main_001"
      ],
      "autoAcceptNextQuestIds": [
        "quest_main_003"
      ],
      "isContentEnabled": true,
      "objectives": [
        {
          "objectiveId": "obj_cycle_central_evaluation",
          "description": "마지막 상대와의 대결에서 승리한다.",
          "type": "StoryProgress",
          "targetId": 20,
          "targetStringId": "",
          "requiredCount": 1
        }
      ],
      "dialogues": [],
      "stories": []
    },
    {
      "questId": "quest_main_003",
      "questName": "귀환",
      "shortSummary": "귀환 포털을 이용한 뒤, 시작 지점에서 달라진 일을 확인한다.",
      "description": "마지막 대결이 끝나자 귀환 포털이 나타났다. 포털을 이용해 돌아간 뒤 마을의 모습을 살펴보자.",
      "requiredProgress": 20,
      "rewardGold": 200,
      "rewardExp": 200,
      "isRepeatable": false,
      "autoComplete": false,
      "autoAcceptOnNewGame": false,
      "requiredQuestIds": [
        "quest_main_002"
      ],
      "autoAcceptNextQuestIds": [
        "quest_main_004"
      ],
      "isContentEnabled": true,
      "objectives": [
        {
          "objectiveId": "obj_first_return_arrived",
          "description": "귀환 포털을 이용해 시작 지점으로 돌아간다.",
          "type": "StoryEvent",
          "targetId": 0,
          "targetStringId": "cycle.story.first_return_arrived",
          "requiredCount": 1
        },
        {
          "objectiveId": "obj_first_return_anchor",
          "description": "미아의 파란 리본을 다시 찾아 돌려준다.",
          "type": "StoryEvent",
          "targetId": 0,
          "targetStringId": "cycle.story.first_return_anchor_returned",
          "requiredCount": 1,
          "revealAfterObjectiveIds": [
            "obj_first_return_arrived"
          ]
        },
        {
          "objectiveId": "obj_first_return_guide",
          "description": "안내인과 대화한다.",
          "type": "StoryEvent",
          "targetId": 0,
          "targetStringId": "cycle.story.first_return_guide_completed",
          "requiredCount": 1,
          "revealAfterObjectiveIds": [
            "obj_first_return_anchor"
          ]
        }
      ],
      "dialogues": [],
      "stories": []
    },
    {
      "questId": "quest_main_004",
      "questName": "달라진 길",
      "shortSummary": "달라진 지도에서 다시 세 번의 대결을 마치고 귀환한다.",
      "description": "마을은 같은 모습으로 돌아왔지만 지도와 사람들의 위치는 전과 다르다. 기억한 길과 다시 손을 내밀 사람들의 도움으로 귀환 포털을 찾아가자.",
      "requiredProgress": 30,
      "rewardGold": 250,
      "rewardExp": 250,
      "isRepeatable": false,
      "autoComplete": true,
      "autoAcceptOnNewGame": false,
      "requiredQuestIds": [
        "quest_main_003"
      ],
      "autoAcceptNextQuestIds": [
        "quest_main_005"
      ],
      "isContentEnabled": true,
      "objectives": [
        {
          "objectiveId": "obj_cycle_second_outer_guardians",
          "description": "대결에서 승리한다.",
          "type": "CycleOuterBoss",
          "targetId": 0,
          "targetStringId": "",
          "requiredCount": 3
        },
        {
          "objectiveId": "obj_cycle_second_settlement",
          "description": "마지막 상대와의 대결에서 승리하고 귀환한다.",
          "type": "StoryProgress",
          "targetId": 40,
          "targetStringId": "",
          "requiredCount": 1,
          "revealAfterObjectiveIds": [
            "obj_cycle_second_outer_guardians"
          ]
        }
      ],
      "dialogues": [],
      "stories": []
    },
    {
      "questId": "quest_main_005",
      "questName": "길을 여는 원정",
      "shortSummary": "마지막 원정을 마치고 전에 없던 길을 확인한다.",
      "description": "이번에도 세 번의 대결과 마지막 대결을 마치자. 돌아온 뒤에는 익숙한 지도만 따르지 말고, 함께한 사람들과 지나온 길이 가리키는 전에 없던 방향을 확인하자.",
      "requiredProgress": 40,
      "rewardGold": 500,
      "rewardExp": 500,
      "isRepeatable": false,
      "autoComplete": true,
      "autoAcceptOnNewGame": false,
      "requiredQuestIds": [
        "quest_main_004"
      ],
      "autoAcceptNextQuestIds": [],
      "isContentEnabled": true,
      "objectives": [
        {
          "objectiveId": "obj_cycle_final_outer_guardians",
          "description": "대결에서 승리한다.",
          "type": "CycleOuterBoss",
          "targetId": 0,
          "targetStringId": "",
          "requiredCount": 3
        },
        {
          "objectiveId": "obj_cycle_final_evaluation",
          "description": "마지막 상대와의 대결에서 승리하고, 귀환한 뒤 새로 드러난 길을 확인한다.",
          "type": "StoryProgress",
          "targetId": 50,
          "targetStringId": "",
          "requiredCount": 1,
          "revealAfterObjectiveIds": [
            "obj_cycle_final_outer_guardians"
          ]
        }
      ],
      "dialogues": [],
      "stories": []
    }
  ]
}
```
<!-- STORY_GENERATOR_MAIN_END -->

---

## 14. Sub Story Generator 데이터

아래 marker 사이 JSON은 `SubStoryGeneratorWindow`가 읽는다. 기존 서브 퀘스트 ID와 GUID를 유지하며, 메인 진행을 막지 않는 보조 의뢰만 담는다. 현재 열려 있는 의뢰는 `quest_sub_hunter_skeleton_patrol` 하나이고, 나머지는 `isContentEnabled: false`로 닫혀 있다.

<!-- STORY_GENERATOR_SUB_BEGIN -->
```json
{
  "quests": [
    {
      "questId": "quest_sub_guide_broken_lantern",
      "questName": "새로 드러난 길",
      "shortSummary": "세 번의 대결 뒤 달라진 풍경을 지도에 기록한다.",
      "description": "세 사람이 자리를 옮기자 멀리 있던 풍경이 흔들리고, 전에는 보이지 않던 길이 나타났다. 안내인은 길이 드러난 순간과 주변의 변화를 빈 지도에 표시해 달라고 부탁했다.",
      "requiredProgress": 0,
      "rewardGold": 60,
      "rewardExp": 60,
      "isRepeatable": false,
      "autoComplete": true,
      "autoAcceptOnNewGame": true,
      "requiredQuestIds": [],
      "autoAcceptNextQuestIds": [
        "quest_sub_survivor_lost_pack"
      ],
      "isContentEnabled": false,
      "objectives": [
        {
          "objectiveId": "obj_record_first_outer_trials",
          "description": "세 번의 대결을 마치고 새로 드러난 길을 기록한다.",
          "type": "StoryProgress",
          "targetId": 10,
          "targetStringId": "",
          "requiredCount": 1
        }
      ],
      "dialogues": [],
      "stories": []
    },
    {
      "questId": "quest_sub_survivor_lost_pack",
      "questName": "되돌아온 하루",
      "shortSummary": "첫 귀환 뒤 되풀이된 마을의 작은 사건을 기록한다.",
      "description": "미아가 같은 리본을 같은 곳에서 다시 잃어버렸다. 주민에게는 처음 일어난 일이지만 주인공은 위치를 기억했다. 장소만 닮은 것이 아니라 세계의 하루가 되돌아왔다는 가장 분명한 흔적이다.",
      "requiredProgress": 10,
      "rewardGold": 80,
      "rewardExp": 80,
      "isRepeatable": false,
      "autoComplete": true,
      "autoAcceptOnNewGame": false,
      "requiredQuestIds": [
        "quest_sub_guide_broken_lantern"
      ],
      "autoAcceptNextQuestIds": [
        "quest_sub_herbalist_lake_herb"
      ],
      "isContentEnabled": false,
      "objectives": [
        {
          "objectiveId": "obj_read_first_settlement_record",
          "description": "첫 귀환 뒤 같은 분실물 사건이 되풀이됐음을 확인한다.",
          "type": "StoryProgress",
          "targetId": 30,
          "targetStringId": "",
          "requiredCount": 1
        }
      ],
      "dialogues": [],
      "stories": []
    },
    {
      "questId": "quest_sub_herbalist_lake_herb",
      "questName": "서로 다른 두 번째 지도",
      "shortSummary": "두 번의 원정 지도를 겹쳐 흐름이 달라진 곳을 찾는다.",
      "description": "두 번째 원정의 길과 상대 위치는 첫 번째 기록과 조금 달랐다. 흐름은 매번 완전히 같은 모양으로 가라앉지 않지만, 주인공이 기억한 길과 사람 사이에 쌓인 연결은 그대로 남았다.",
      "requiredProgress": 30,
      "rewardGold": 100,
      "rewardExp": 100,
      "isRepeatable": false,
      "autoComplete": true,
      "autoAcceptOnNewGame": false,
      "requiredQuestIds": [
        "quest_sub_survivor_lost_pack"
      ],
      "autoAcceptNextQuestIds": [],
      "isContentEnabled": false,
      "objectives": [
        {
          "objectiveId": "obj_compare_second_cycle_map",
          "description": "두 번째 사이클을 정산하고 두 장의 지도를 비교한다.",
          "type": "StoryProgress",
          "targetId": 40,
          "targetStringId": "",
          "requiredCount": 1
        }
      ],
      "dialogues": [],
      "stories": []
    },
    {
      "questId": "quest_sub_hunter_skeleton_patrol",
      "questName": "돌아오지 않은 사람들",
      "shortSummary": "붉은 천을 따라 호노카와 리안리안을 찾는다.",
      "description": "준은 짙은 남색 웃옷을 입고 오른쪽 소매를 두 번 접는다. 준과 마을 사람 둘은 호숫가의 그물을 걷으러 갔다가 돌아오지 않았다. 뒤를 쫓은 호노카와 리안리안마저 소식이 끊겼다. 동쪽 풀숲에 남은 붉은 천부터 찾아보자.",
      "requiredProgress": 0,
      "rewardGold": 120,
      "rewardExp": 100,
      "isRepeatable": false,
      "autoComplete": true,
      "autoAcceptOnNewGame": false,
      "requiredQuestIds": [],
      "autoAcceptNextQuestIds": [],
      "isContentEnabled": true,
      "objectives": [
        {
          "objectiveId": "obj_find_honoka",
          "description": "동쪽 풀숲에서 호노카를 찾는다.",
          "type": "StoryEvent",
          "targetId": 0,
          "targetStringId": "lake.story.honoka_joined",
          "requiredCount": 1
        },
        {
          "objectiveId": "obj_find_lianlian",
          "description": "호숫가로 이어진 흔적을 따라 리안리안을 찾는다.",
          "type": "StoryEvent",
          "targetId": 0,
          "targetStringId": "lake.story.lianlian_joined",
          "requiredCount": 1,
          "revealAfterObjectiveIds": [
            "obj_find_honoka"
          ]
        }
      ],
      "dialogues": [],
      "stories": []
    },
    {
      "questId": "quest_sub_hunter_spider_web",
      "questName": "마지막 자리의 흔적",
      "shortSummary": "마지막 대결 뒤 빛이 모인 방향을 기록한다.",
      "description": "마지막 상대가 자리를 벗어나자 주변에 엉켜 있던 빛이 한 방향으로 흘러 귀환 포털의 모양을 만들었다. 누가 문을 열어 준 것이 아니라, 막혀 있던 흐름이 되돌아간 흔적처럼 보인다.",
      "requiredProgress": 10,
      "rewardGold": 80,
      "rewardExp": 80,
      "isRepeatable": false,
      "autoComplete": true,
      "autoAcceptOnNewGame": false,
      "requiredQuestIds": [
        "quest_sub_hunter_skeleton_patrol"
      ],
      "autoAcceptNextQuestIds": [
        "quest_sub_highland_golem_trace"
      ],
      "isContentEnabled": false,
      "objectives": [
        {
          "objectiveId": "obj_hear_central_evaluator",
          "description": "마지막 대결을 마치고 빛이 모인 방향을 기록한다.",
          "type": "StoryProgress",
          "targetId": 20,
          "targetStringId": "",
          "requiredCount": 1
        }
      ],
      "dialogues": [],
      "stories": []
    },
    {
      "questId": "quest_sub_highland_golem_trace",
      "questName": "함께 만든 길",
      "shortSummary": "여러 사람의 힘이 새 경로를 만든 순간을 확인한다.",
      "description": "주인공이 이어 온 사람들의 힘을 같은 순간에 모으자, 늘 원점으로 향하던 흐름이 다른 방향으로 움직였다. 새 길은 세계가 내어 준 보상이 아니라, 기억과 관계로 언제든 다시 만들 수 있게 된 경로다.",
      "requiredProgress": 20,
      "rewardGold": 120,
      "rewardExp": 120,
      "isRepeatable": false,
      "autoComplete": true,
      "autoAcceptOnNewGame": false,
      "requiredQuestIds": [
        "quest_sub_hunter_spider_web"
      ],
      "autoAcceptNextQuestIds": [],
      "isContentEnabled": false,
      "objectives": [
        {
          "objectiveId": "obj_confirm_final_signatures",
          "description": "마지막 원정을 마치고 새 경로를 다시 만드는 방법을 확인한다.",
          "type": "StoryProgress",
          "targetId": 50,
          "targetStringId": "",
          "requiredCount": 1
        }
      ],
      "dialogues": [],
      "stories": []
    }
  ]
}
```
<!-- STORY_GENERATOR_SUB_END -->
