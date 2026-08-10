# UPlayground 사이클 메인 스토리 플롯

> 작성일: 2026-08-09  
> 상태: 스토리 방향 확정, 세부 배역·대사·퀘스트 데이터 미확정  
> 작업명: **오늘도 시작의 마을**  
> 서사 기준: 밝은 판타지 모험, 코믹한 회차 미스터리, 보유 모델 우선

---

## 1. 문서 목적과 권위

이 문서는 UPlayground의 **현행 사이클형 보스 헌팅 구조에 대응하는 메인 스토리 플롯 기준**이다. 이후 메인 퀘스트, 대화, StoryEntry, 사이클 연출을 작성할 때 이 문서의 방향을 우선한다.

- `Assets/docs/cycle/01~09` 문서는 런타임과 데이터의 구현 계약을 담당한다.
- 이 문서는 구현 계약을 바꾸지 않고, 이미 확정된 게임 규칙에 서사적 의미를 부여한다.
- 시스템 동작이 이 문서의 표현과 충돌하면 사이클 구현 스펙을 우선하고 이 문서의 표현을 수정한다.
- `Assets/docs/Complete/story/BASE_STORY.md`와 `MAIN_STORY.md`는 사이클 구조 도입 전의 필드·던전 초안으로 보존하되, 현행 메인 플롯의 권위 문서로 사용하지 않는다.

관련 문서:

- [사이클 구현 스펙 인덱스](README.md)
- [Cycle Runtime 구현 스펙](01_CYCLE_RUNTIME_SPEC.md)
- [Boss Assist & Recruitment 구현 스펙](04_BOSS_ASSIST_RECRUITMENT_SPEC.md)
- [Cycle Save & Settlement 구현 스펙](06_CYCLE_SAVE_SETTLEMENT_SPEC.md)
- [Story 시스템 가이드](../Complete/STORY_SYSTEM_GUIDE.md)

---

## 2. 핵심 방향

### 한 줄 로그라인

매번 자신을 처음 보는 척하는 시작의 마을 사람들과 함께, 보쿠세이는 도전자의 전술을 학습하는 고대 시련장을 속이고 닫힌 바깥길을 열기 위한 반복 원정에 나선다.

### 플레이어가 느껴야 하는 재미

1. 처음에는 평범한 판타지 모험처럼 보인다.
2. 같은 대사를 반복하던 NPC가 아무도 보지 않는 곳에서 회차의 비밀을 꺼낸다.
3. 보스와 주민들이 시련장 앞에서는 정해진 역할을 연기하고, 감시가 닿지 않는 곳에서는 보쿠세이와 작전을 짠다.
4. 반복할수록 비극이 쌓이는 대신 관계, 농담, 공략 지식이 쌓인다.
5. 마지막에는 정해진 답을 따르는 강한 용사가 아니라, 동료와 함께 새로운 답을 만든 탐험대로 인정받는다.

### 톤 원칙

- 밝고 경쾌한 판타지 모험을 유지한다.
- 미스터리는 불안감보다 “NPC들이 무언가를 숨기고 있다”는 호기심과 웃음에 사용한다.
- 세계 멸망, 대량 희생, 기억 소멸을 메인 동력으로 삼지 않는다.
- 여신, 마왕, 창조주처럼 별도 전용 모델을 요구하는 핵심 인물을 두지 않는다.
- 보스는 절대악보다 시련장의 역할을 수행하는 지역 수호자나 강한 몬스터로 묘사한다.
- 같은 장소와 모델이라도 대사와 태도의 차이로 회차 진행을 느끼게 한다.

---

## 3. 제작 자산 제약

이 플롯은 스토리 때문에 신규 고유 캐릭터나 최종 보스 모델을 제작하지 않는 것을 전제로 한다.

### 필수 자산

| 구분 | 사용 원칙 |
|---|---|
| 보쿠세이 | 기본 고정 주인공으로 사용 |
| 시작의 마을 안내인 | 보유한 일반 NPC 또는 플레이어블 캐릭터 모델 중 하나를 배역으로 사용 |
| 외곽 보스 3체 | 현재 보유한 보스·몬스터 풀에서 사이클마다 배치 |
| 중앙 보스 1체 | 현재 보유한 보스·몬스터 풀에서 배치하며 별도 마왕 모델을 요구하지 않음 |
| 시련장 | 기존 오픈 필드와 보스 스폰 후보를 그대로 사용 |
| 시련장의 반응 | 기존 포털, VFX, UI 메시지, 환경음으로 표현 |

### 만들지 않는 것

- 여신 전용 모델과 강림 연출
- 마왕 전용 모델과 마왕성
- 인격을 가진 미궁 코어 캐릭터
- 회차마다 물리적으로 증축되는 마을
- 스토리 전용 신규 최종 보스
- 완전 절차 생성 지형

시련장은 지형 자체를 매번 재구성하지 않는다. P0에서 시드가 결정하는 범위에 맞춰 **외곽·중앙 보스의 종류와 위치, 부활 지점, 보상 결과가 달라지는 것**을 “시련장이 새로운 시험을 배치했다”라고 해석한다.

---

## 4. 세계와 시련장

시작의 마을 바깥에는 오래전부터 **순환 시련장**이라 불리는 넓은 탐험 구역이 존재한다. 사람들은 시련장을 통과하면 지금은 닫힌 바깥 지역으로 향하는 길이 열린다고 알고 있다.

시련장은 도전자의 행동을 기록하고 다음 원정에 반영한다.

- 어떤 보스를 먼저 찾아갔는지 기록한다.
- 같은 전술을 반복하면 다른 보스와 위치 조합을 제시한다.
- 중앙 보스 처치 후 결과를 평가한다.
- 통과 조건이 부족하면 포털로 도전자를 시작 지점에 돌려보낸다.
- 도전자가 얻은 경험과 영구 성장까지 빼앗지는 않는다.

시련장은 말을 하는 존재가 아니다. 다음과 같은 짧은 시스템 메시지만 포털이나 UI를 통해 표시한다.

```text
탐험 기록 완료
통과 조건 불충족
다음 시련을 준비합니다
```

마을 주민들은 시련장이 지상에서 나눈 공략 대화까지 다음 배치에 참고한다는 사실을 경험으로 알고 있다. 따라서 시련장 주변에서는 평범한 주민처럼 행동하고, 감시가 닿지 않는 지하실에서만 진짜 작전을 논의한다.

---

## 5. 주요 배역

### 보쿠세이

기본 고정 주인공. 첫 원정에서는 평범한 의뢰라고 생각하고 시련장에 들어간다. 중앙 보스를 쓰러뜨린 뒤에도 시작 지점으로 돌아오면서 반복을 인식한다.

보쿠세이의 핵심 변화는 “혼자 시험을 통과할 만큼 강해지는 것”에서 “각자의 역할에 갇힌 사람과 수호자를 하나의 탐험대로 연결하는 것”으로 이동한다.

### 시작의 마을 안내인

보유 모델에 맞춰 이름과 외형을 나중에 확정한다. 지상에서는 모든 도전자에게 같은 안내 대사를 반복한다.

> “어서 와! 여기는 시작의 마을이야!”

그러나 시련장의 관측이 닿지 않는 지하실에서는 회차 기록과 주민들의 작전을 관리한다. 보쿠세이가 첫 정산을 마치고 돌아오면 지하실로 데려가 묻는다.

> “밖에서는 지난 원정 이야기를 하면 안 돼.”  
> “그래서, 이번이 몇 번째 귀환이야?”

안내인은 전투 동료가 아니라 회차의 비밀을 소개하고 다음 서사 목표를 제시하는 조력자다. 전용 전투 모델이나 고유 보스전이 필요하지 않다.

### 외곽 보스

외곽 보스들은 악의 조직 간부가 아니다. 시련장에 영역이 연결된 수호자 또는 강한 몬스터다. 처음에는 보쿠세이를 침입자로 여기지만 반복해서 만나면서 상황을 이해한다.

시련장이 관찰할 때는 보스로서 전력을 다해 싸우고, 전투 전후의 짧은 대사나 연출에서는 보쿠세이에게 힌트를 준다.

> “이번에도 싸우는 척은 해야 한다. 저 문이 보고 있으니까.”

> “세 번째 공격 뒤에 빈틈을 만들겠다. 너무 티 나게 기다리지는 마라.”

정확한 성격과 대사는 실제 보스 모델과 전투 패턴을 확인한 뒤 보스별 문서에서 확정한다.

### 중앙 보스

중앙 보스는 마왕이 아니라 해당 사이클의 **최종 평가 대상**이다. 시련장이 보유 몬스터 중 하나를 중앙에 배치하므로 사이클마다 정체가 달라질 수 있다.

최종장도 새로운 적을 만들지 않고 다음 중 하나로 구성한다.

- 보유한 중앙 보스 중 가장 강한 데이터 변형
- 기존 중앙 보스의 강화 페이즈
- 이미 만난 보스의 연속 시련

P0에서는 추가 모델이 필요 없는 첫 번째 방식을 우선한다.

---

## 6. 사이클 규칙의 서사 의미

| 게임 규칙 | 서사 해석 |
|---|---|
| 고정 시작 위치 | 모든 시련은 시작의 마을 입구에서 시작 |
| 시드 | 시련장이 발급한 원정 기록 번호 |
| 외곽 보스 3체 | 최종 평가에 들어가기 전 확인해야 하는 세 개의 시련 |
| 중앙 보스 1체 | 해당 회차의 최종 평가 대상 |
| 보스 위치·종류 변화 | 시련장이 이전 기록을 참고해 시험 조합을 변경 |
| 중앙 보스 처치 | 평가 전투 종료. 아직 사이클 완료는 아님 |
| 탈출 포털 진입 | 결과 제출과 정산을 선택하는 행위 |
| 포털 정산 | 원정 보상과 기록을 확정한 뒤 귀환 |
| 미정산 재료 | 시련장 안에서만 확보한 임시 전리품 |
| 유해 회수 | 실패 지점에 남은 원정 물자를 되찾는 규칙 |
| 영구 성장 | 시련장이 인정하는 도전자의 경험과 숙련 |

정산은 시간 역행이나 세계의 멸망이 아니다. 한 번의 원정 결과를 제출하고 다음 시험을 받는 과정이다.

---

## 7. BossAssist와 플레이어블 해금

### BossAssist

외곽 보스는 파티원이 되지 않는다. 보쿠세이의 실력을 인정한 보스가 자신의 힘을 담은 **전투 잔상**을 빌려준다. 전투 중에는 보스 모델이 잠시 나타나 지정 스킬을 한 번 사용하고 사라진다.

실제 영입은 구현 스펙과 동일하게 다음 조건 중 하나를 달성했을 때 확정된다.

- 브레이크 마무리
- 노히트 처치
- 보스별 요구 누적 처치 횟수 달성

서사적으로는 숙련 조건을 달성할수록 보스가 보쿠세이의 실력을 빨리 인정하고, 반복해서 정정당당하게 도전해도 결국 신뢰를 얻는 구조다.

### 플레이어블 캐릭터 해금

`MonsterActor._recruitableAs`가 지정된 몬스터는 과거 시련장에 들어왔다가 **상대 역할을 부여받은 탐험가**로 해석한다. 전투에서 패배하면 역할을 강제하던 효과가 풀리고 본래 인격을 되찾는다.

- 몬스터 모습: 현재 보유한 적 모델 사용
- 해금 이후: 해당 `CharacterActorType`의 기존 플레이어블 모델 사용
- 변신 장면: 필수 아님. 전투 종료 대사와 캐릭터 등록 UI로 처리 가능

사이클 보스는 기본적으로 `BossAssist` 경로만 사용한다. 파티 해금을 의도하지 않은 보스의 `_recruitableAs`는 `None`을 유지한다.

---

## 8. 메인 플롯

### 프롤로그 — 평범한 첫 원정

보쿠세이는 닫힌 바깥길을 다시 열 탐험가를 구한다는 의뢰를 받고 시작의 마을에 도착한다. 안내인은 밝게 마을과 시련장의 기본 규칙을 설명한다.

보쿠세이는 외곽 보스 셋을 쓰러뜨리고 중앙 보스까지 처치한다. 중앙 보스가 쓰러지면 탈출 포털이 열린다. 보쿠세이가 포털에 들어가 정산을 마치자 다시 시작의 마을 입구가 나타난다.

안내인은 처음과 같은 표정과 목소리로 말한다.

> “어서 와! 여기는 시작의 마을이야!”

그날 밤 안내인은 보쿠세이에게 지하실 열쇠를 건넨다.

### 1막 — 몇 번째 귀환이야?

지하실에서 안내인은 마을 사람들이 오래전부터 시련장의 관찰을 피하며 도전자들을 돕고 있었다고 밝힌다. 지상에서 반복하는 대사는 무성의한 게임 대사가 아니라, 시련장에게 새로운 전략을 들키지 않기 위한 연기였다.

두 번째 원정부터 같은 NPC의 대사는 장소에 따라 두 층으로 나뉜다.

- 지상: 초회차와 같은 공식 안내
- 지하실·관측 사각지대: 이전 회차를 기억하는 비밀 대화

보쿠세이는 주민들이 남긴 기록을 바탕으로 외곽 보스를 다른 순서와 방식으로 공략한다.

### 2막 — 보스들도 연기 중이다

반복해서 만난 외곽 보스가 전투 중 의도적인 신호를 보낸다. 보스들 역시 시련장 때문에 같은 역할을 반복하고 있었지만, 역할을 거부하면 다른 장소와 형태로 다시 배치될 뿐이었다.

보쿠세이는 보스를 단순히 제거하는 대신 각 보스의 인정 조건을 달성하고 BossAssist 계약을 맺는다. 상대 역할을 부여받은 다른 탐험가도 발견해 플레이어블 캐릭터로 해방한다.

이 과정에서 주민, 보스, 해금 캐릭터는 보쿠세이와 함께 시련장의 통과 조건을 추리한다.

### 3막 — 잘못 알고 있던 합격 조건

마을 사람들은 오랫동안 중앙 보스를 가장 빨리 쓰러뜨리는 것이 합격 조건이라고 믿었다. 그러나 남겨진 기록을 비교한 결과, 시련장이 평가하는 항목은 단순 처치 시간이 아니었다.

시련장이 찾는 사람은 혼자 가장 강한 전사가 아니라 다음 조건을 증명한 탐험대장이다.

- 낯선 배치에 맞춰 전략을 바꿀 수 있다.
- 다른 탐험가를 구하고 함께 싸울 수 있다.
- 지역 수호자와 힘으로만 지배하지 않는 관계를 만들 수 있다.
- 전리품을 챙기는 것뿐 아니라 안전하게 귀환할 판단을 내릴 수 있다.

이 조건은 서사의 의미다. 실제 진엔딩 해금 조건은 기존 데이터와 제작 범위를 확인한 뒤 별도 퀘스트 스펙에서 최소 플래그 조합으로 정한다.

### 최종 사이클 — 다음 길을 여는 시험

마지막 원정에서도 외곽 보스 셋과 중앙 보스 하나라는 규칙은 변하지 않는다. 보쿠세이는 지금까지 쌓은 관계와 공략 지식을 사용해 시련을 마친다.

중앙 보스 처치 후 평소와 같은 탈출 포털이 열린다. 외형은 같지만 메시지가 달라진다.

```text
탐험대 구성 확인
최종 평가 완료
외부 항로를 개방합니다
```

보쿠세이가 포털에 들어가면 새 지역 모델을 직접 보여줄 필요는 없다. 밝아지는 포털, 환경음, 짧은 암전 후 시작의 마을 대화만으로 길이 열렸음을 전달할 수 있다.

안내인은 습관적으로 같은 말을 시작한다.

> “어서 와! 여기는 시작의 마을——”

잠시 멈춘 뒤 웃으며 고쳐 말한다.

> “아니네. 이제 여기는 출발의 마을이야.”

---

## 9. 엔딩 이후 사이클

메인 결말 이후 순환 시련장은 폐기되지 않는다. 통과자를 막는 시험에서 새로운 탐험대를 훈련하고 미지의 조합을 연구하는 **고급 원정 프로그램**으로 목적이 바뀐다.

따라서 엔딩 후에도 같은 사이클 게임플레이를 이어갈 수 있다.

- 새로운 시드: 신규 고급 시련 기록
- 추가 보스: 시련장에 등록된 새로운 평가 대상
- 추가 플레이어블: 뒤늦게 발견한 탐험가
- 반복 BossAssist 영입: 아직 신뢰를 얻지 못한 수호자와의 재도전
- 높은 사이클 난이도: 통과자용 상급 훈련

시작의 마을 외형을 바꾸거나 새로운 거점을 건설하지 않아도 NPC 대사와 UI 명칭 변화만으로 엔딩 이후 상태를 표현할 수 있다.

---

## 10. 대화 연출 규칙

### 공식 대사와 비밀 대사

같은 NPC에게 두 가지 말투를 부여한다.

| 상황 | 말투 | 목적 |
|---|---|---|
| 시련장의 관측 범위 | 초보자를 대하는 정형화된 안내 | 시련장을 속이고 반복 개그 형성 |
| 지하실·관측 사각지대 | 편하고 현실적인 작전 대화 | 회차 정보와 관계 진전 전달 |

예시:

```text
[지상]
동쪽 숲에는 무시무시한 수호자가 살고 있답니다!

[지하실]
동쪽은 지난번에 먼저 갔지? 이번에는 북쪽부터 가자.
그리고 밖에서 약점 얘기하지 마. 다음 시험에 바로 반영돼.
```

```text
[보스 조우]
침입자여! 이곳이 네 여정의 끝이다!

[BossAssist 획득 후 재조우]
오늘도 제대로 싸운다. 저 문이 보고 있어.
대신 지난번보다 등장 연출은 조금 길게 기다려라.
```

### 피해야 할 표현

- NPC가 플레이어에게 시스템 용어를 장황하게 설명하는 대화
- 같은 개그를 회차마다 그대로 반복하는 구성
- 밝은 분위기와 무관한 희생, 처형, 기억 소멸 중심의 반전
- 시련장을 인간 악당처럼 만들어 전용 모델과 긴 컷신을 요구하는 전개
- 보유하지 않은 보스의 외형과 능력을 플롯 단계에서 확정하는 것

---

## 11. 최소 구현 표현

이 플롯을 전달하기 위한 최소 서사 구현은 다음과 같다.

1. 첫 사이클 전 안내 대화
2. 첫 정산 후 반복되는 환영 대사
3. 지하실 또는 관측 사각지대에서 재생되는 비밀 대화
4. 사이클 진행도에 따른 안내인 대사 Variant
5. 보스 조우·처치·BossAssist 영입 시 짧은 대사 또는 텍스트
6. 플레이어블 해금 시 등록 메시지
7. 최종 정산 포털의 메시지 변경
8. 엔딩 후 안내인의 마지막 대사 변경

별도 모델 없이도 `DialogueGraphSO`, `StoryEntrySO`, `GlobalFlagManager`, 사이클 완료 횟수, 보스 처치·영입 결과를 조합해 표현할 수 있다. 구체적인 ID와 데이터 생성 범위는 메인 퀘스트 구현 문서에서 확정한다.

---

## 12. 미확정 항목

다음 항목은 실제 보유 자산과 구현 일정을 확인한 뒤 결정한다. 안내인 배역, 최소 사이클 수, 최종 평가 플래그는 13절의 P0 데이터에서 확정했다.

- 외곽·중앙 보스별 성격과 대사
- 시련장의 공식 명칭
- 지하실을 실제 공간으로 둘지 기존 실내 공간으로 대체할지
- 엔딩 후 포털과 UI에 표시할 세부 문구

미확정 항목 때문에 신규 모델을 선제작하지 않는다. 보유 자산을 먼저 배역에 배치한 뒤 대사와 세부 설정을 맞춘다.

---

## 13. 저작된 P0 데이터

### 시작의 마을 안내인

신규 NPC 모델은 추가하지 않는다. 기존 `NPC_Story_Guide`를 시작의 마을 안내인으로 사용한다.

| 항목 | 값 |
|---|---|
| NpcActorSO | `Assets/10.Datas/Actor/Npc/NPC_Story_Guide.asset` |
| DialogueGraphSO | `Assets/10.Datas/Dialogue/Story/Dialogue/DLG_Npc_Guide.asset` |
| 사용 씬 | `LakeOfLife`, `HarvestOfPlain` |
| 첫 대화 | 공식적인 시작의 마을 안내 |
| 첫 정산 이후 | 회차를 기억하는 비밀 작전 대화 |
| 두 번째 정산 이후 | 시련장이 관계를 계산하지 못한다는 작전 대화 |
| 세 번째 정산 이후 | “출발의 마을” 엔딩 대화 |

안내인 대화는 다음 영구 플래그로 분기한다.

| 플래그 | 설정 시점 |
|---|---|
| `cycle.story.first_settlement_completed` | 첫 사이클 포털 정산 성공 |
| `cycle.story.second_settlement_completed` | 두 번째 사이클 포털 정산 성공 |
| `cycle.story.final_evaluation_completed` | 세 번째 사이클 포털 정산 성공 |

### 메인 퀘스트

기존 `quest_main_001~005`의 ID와 GUID를 보존하고 현행 사이클 플롯으로 내용을 교체한다.

| ID | 이름 | 완료 조건 | StoryProgress |
|---|---|---|---:|
| `quest_main_001` | 첫 번째 시련 | 한 사이클의 외곽 보스 3체 처치 | 10 |
| `quest_main_002` | 중앙의 평가자 | 중앙 보스 처치 | 20 |
| `quest_main_003` | 돌아오는 문 | 첫 사이클 포털 정산 | 30 |
| `quest_main_004` | 두 번째 각본 | 두 번째 사이클 포털 정산 | 40 |
| `quest_main_005` | 출발의 마을 | 세 번째 사이클 포털 정산 | 50 |

각 퀘스트는 완료 즉시 다음 퀘스트를 자동 수락한다. `quest_main_001`만 새 게임에서 자동 수락하며, 사이클 런타임 이벤트가 `StoryManager.SetProgress`를 통해 목표를 갱신한다.

### 병행 서브 퀘스트

서브 퀘스트는 별도 심부름이 아니라 메인 사이클에서 발견한 단서를 해석하는 두 개의 기록 연작이다. 각 연작의 첫 퀘스트만 새 게임에서 자동 수락하고, 이후 퀘스트는 앞 기록이 완성될 때 자동으로 이어진다.

| 연작 | ID | 이름 | 완료 시점 |
|---|---|---|---:|
| 안내인의 기록 | `quest_sub_guide_broken_lantern` | 소리 내지 않는 복습 | 외곽 시련 완료(10) |
| 안내인의 기록 | `quest_sub_survivor_lost_pack` | 정산표 뒷면 | 첫 정산(30) |
| 안내인의 기록 | `quest_sub_herbalist_lake_herb` | 서로 다른 두 번째 지도 | 두 번째 정산(40) |
| 수호자의 기록 | `quest_sub_hunter_skeleton_patrol` | 먼저 고개 숙인 수호자 | 외곽 시련 완료(10) |
| 수호자의 기록 | `quest_sub_hunter_spider_web` | 평가자의 호칭 | 중앙 평가 완료(20) |
| 수호자의 기록 | `quest_sub_highland_golem_trace` | 마지막 서명 | 최종 정산(50) |

두 연작은 메인 퀘스트의 성공 조건이나 보스 배치를 바꾸지 않는다. 퀘스트 로그의 제목·요약·설명으로 세계의 진실을 누적하고, 안내인 대사가 회차별 해석을 보충한다.

### 스토리·사이클 아이템

| ID | 아이템 | 용도 | 데이터 |
|---:|---|---|---|
| `250001` | 시작의 마을 지하실 열쇠 | 첫 정산 뒤 안내인이 전달하는 영구 중요 아이템 | `Assets/10.Datas/Item/Story/StartingVillageBasementKey.asset` |
| `100011` | 시련의 파편 | 사이클 보스가 드랍하며 포털 정산 전까지 미정산 원장에 보관되는 재료 | `Assets/10.Datas/Item/Material/TrialFragment.asset` |

`시련의 파편`은 `CycleConfig_P0.unsettledMaterialItemIds`에 등록한다. `quest_main_003` 첫 정산 보상으로 지하실 열쇠 1개를 지급하며, 인벤토리에서는 `IMPORTANT` 분류로 보존한다.

캐릭터별 전투 보상 풀에는 기존에 비어 있던 `쌍도끼(8001)`, `채찍(9001)`, `창(10001)`, `쌍검(11001)` 기본 장비를 추가한다. 네 장비는 캐릭터 모델에 포함된 고유 무기 외형을 사용한다.

---

## 14. Main Story Generator 데이터

아래 marker 사이 JSON은 `MainStoryGeneratorWindow`가 직접 읽는 권위 데이터다. 본문의 퀘스트 ID, 목표, 보상, 자동 연계가 바뀌면 이 블록도 함께 수정한다.

<!-- STORY_GENERATOR_MAIN_BEGIN -->
```json
{
  "quests": [
    {
      "questId": "quest_main_001",
      "questName": "첫 번째 시련",
      "shortSummary": "외곽 시련 세 곳을 모두 통과한다.",
      "description": "시작의 마을 안내인이 알려 준 순환 시련장에 들어가 외곽 보스 세 체를 쓰러뜨린다. 시련장이 다음 공략까지 배우지 못하도록 작전 이야기는 안에서 꺼내지 않는다.",
      "requiredProgress": 0,
      "rewardGold": 100,
      "rewardExp": 100,
      "isRepeatable": false,
      "autoAcceptOnNewGame": true,
      "requiredQuestIds": [],
      "autoAcceptNextQuestIds": ["quest_main_002"],
      "objectives": [
        {
          "objectiveId": "obj_cycle_outer_trials",
          "description": "한 사이클의 외곽 보스 3체를 모두 처치한다.",
          "type": "StoryProgress",
          "targetId": 10,
          "targetStringId": "",
          "requiredCount": 1
        }
      ],
      "dialogues": [
        {
          "graphId": "dlg_cycle_story_first_trial",
          "graphName": "첫 번째 시련 - 시작",
          "channel": "Main",
          "speakerId": "안내인",
          "text": "어서 와! 여기는 시작의 마을이야! 외곽의 시련 세 곳을 먼저 통과하면 중앙 평가로 가는 길이 열릴 거야."
        }
      ],
      "stories": [
        {
          "storyId": "cycle_story_first_trial_start",
          "requiredProgress": 0,
          "dialogueGraphId": "dlg_cycle_story_first_trial"
        }
      ]
    },
    {
      "questId": "quest_main_002",
      "questName": "중앙의 평가자",
      "shortSummary": "열린 중앙 구역의 최종 평가를 끝낸다.",
      "description": "외곽의 세 시련을 마치자 중앙 보스에게 향하는 길이 열렸다. 이번 사이클의 최종 평가 대상을 쓰러뜨리고 포털의 반응을 확인한다.",
      "requiredProgress": 10,
      "rewardGold": 150,
      "rewardExp": 150,
      "isRepeatable": false,
      "autoAcceptOnNewGame": false,
      "requiredQuestIds": ["quest_main_001"],
      "autoAcceptNextQuestIds": ["quest_main_003"],
      "objectives": [
        {
          "objectiveId": "obj_cycle_central_evaluation",
          "description": "중앙 보스를 처치한다.",
          "type": "StoryProgress",
          "targetId": 20,
          "targetStringId": "",
          "requiredCount": 1
        }
      ],
      "dialogues": [
        {
          "graphId": "dlg_cycle_story_central_evaluation",
          "graphName": "중앙의 평가자 - 시작",
          "channel": "Monologue",
          "speakerId": "Bokusei",
          "text": "외곽 시련은 모두 끝났다. 이제 중앙의 평가 대상을 확인하자."
        }
      ],
      "stories": [
        {
          "storyId": "cycle_story_central_evaluation_start",
          "requiredProgress": 10,
          "dialogueGraphId": "dlg_cycle_story_central_evaluation"
        }
      ]
    },
    {
      "questId": "quest_main_003",
      "questName": "돌아오는 문",
      "shortSummary": "탈출 포털에서 첫 원정을 정산한다.",
      "description": "중앙 보스가 쓰러진 뒤 열린 포털은 바깥길이 아니라 정산을 위한 귀환문이었다. 미정산 전리품을 확정하고 시작의 마을로 돌아간다.",
      "requiredProgress": 20,
      "rewardGold": 200,
      "rewardExp": 200,
      "rewardItems": [
        {
          "itemId": 250001,
          "count": 1
        }
      ],
      "isRepeatable": false,
      "autoAcceptOnNewGame": false,
      "requiredQuestIds": ["quest_main_002"],
      "autoAcceptNextQuestIds": ["quest_main_004"],
      "objectives": [
        {
          "objectiveId": "obj_cycle_first_settlement",
          "description": "탈출 포털에 들어가 첫 사이클을 정산한다.",
          "type": "StoryProgress",
          "targetId": 30,
          "targetStringId": "",
          "requiredCount": 1
        }
      ],
      "dialogues": [
        {
          "graphId": "dlg_cycle_story_first_settlement",
          "graphName": "돌아오는 문 - 정산",
          "channel": "System",
          "speakerId": "",
          "text": "탐험 기록 완료. 통과 조건 불충족. 다음 시련을 준비합니다."
        }
      ],
      "stories": [
        {
          "storyId": "cycle_story_first_settlement_completed",
          "requiredProgress": 30,
          "dialogueGraphId": "dlg_cycle_story_first_settlement"
        }
      ]
    },
    {
      "questId": "quest_main_004",
      "questName": "두 번째 각본",
      "shortSummary": "시련장의 새 배치를 공략하고 다시 귀환한다.",
      "description": "안내인은 지상에서 처음 만난 사람처럼 행동했지만, 관측이 닿지 않는 곳에서는 이전 원정을 기억하고 있었다. 시련장의 새 배치에 맞춰 두 번째 사이클을 완료한다.",
      "requiredProgress": 30,
      "rewardGold": 250,
      "rewardExp": 250,
      "isRepeatable": false,
      "autoAcceptOnNewGame": false,
      "requiredQuestIds": ["quest_main_003"],
      "autoAcceptNextQuestIds": ["quest_main_005"],
      "objectives": [
        {
          "objectiveId": "obj_cycle_second_settlement",
          "description": "두 번째 사이클을 완료하고 정산한다.",
          "type": "StoryProgress",
          "targetId": 40,
          "targetStringId": "",
          "requiredCount": 1
        }
      ],
      "dialogues": [
        {
          "graphId": "dlg_cycle_story_second_script",
          "graphName": "두 번째 각본 - 시작",
          "channel": "Main",
          "speakerId": "안내인",
          "text": "밖에서는 처음 온 사람처럼 행동해. 안에서는 지난번과 다른 순서로 움직이고. 시련장이 얼마나 눈치가 빠른지 보자."
        }
      ],
      "stories": [
        {
          "storyId": "cycle_story_second_script_start",
          "requiredProgress": 30,
          "dialogueGraphId": "dlg_cycle_story_second_script"
        }
      ]
    },
    {
      "questId": "quest_main_005",
      "questName": "출발의 마을",
      "shortSummary": "세 번째 최종 평가를 마치고 바깥길을 연다.",
      "description": "보쿠세이는 반복 속에서 혼자 강해지는 대신 안내인, 탐험가, 수호자와 협력하는 법을 증명했다. 세 번째 사이클의 최종 평가를 마치고 시련장이 감춰 둔 다음 길을 연다.",
      "requiredProgress": 40,
      "rewardGold": 500,
      "rewardExp": 500,
      "isRepeatable": false,
      "autoAcceptOnNewGame": false,
      "requiredQuestIds": ["quest_main_004"],
      "autoAcceptNextQuestIds": [],
      "objectives": [
        {
          "objectiveId": "obj_cycle_final_evaluation",
          "description": "세 번째 사이클을 완료하고 최종 평가를 통과한다.",
          "type": "StoryProgress",
          "targetId": 50,
          "targetStringId": "",
          "requiredCount": 1
        }
      ],
      "dialogues": [
        {
          "graphId": "dlg_cycle_story_departure_village",
          "graphName": "출발의 마을 - 완료",
          "channel": "Main",
          "speakerId": "안내인",
          "text": "어서 와! 여기는 시작의 마을—— 아니네. 이제 여기는 출발의 마을이야."
        }
      ],
      "stories": [
        {
          "storyId": "cycle_story_final_evaluation_completed",
          "requiredProgress": 50,
          "dialogueGraphId": "dlg_cycle_story_departure_village"
        }
      ]
    }
  ]
}
```
<!-- STORY_GENERATOR_MAIN_END -->

---

## 15. Sub Story Generator 데이터

아래 데이터는 `SubStoryGeneratorWindow`가 읽는 두 개의 병행 기록 연작이다. 기존 서브 퀘스트 ID와 GUID를 유지하며, 메인 진행을 막지 않는 자동 수락·자동 완료 구조로 사용한다.

<!-- STORY_GENERATOR_SUB_BEGIN -->
```json
{
  "quests": [
    {
      "questId": "quest_sub_guide_broken_lantern",
      "questName": "소리 내지 않는 복습",
      "shortSummary": "안내인이 건넨 빈 지도에 첫 원정의 차이점을 기록한다.",
      "description": "안내인은 시련장이 공략 대화를 학습한다며, 원정 중 발견한 변화는 입 밖에 내지 말고 빈 지도에 표시해 달라고 부탁했다. 외곽의 세 시련을 마친 뒤 서로의 기록을 맞춰 보기로 했다.",
      "requiredProgress": 0,
      "rewardGold": 60,
      "rewardExp": 60,
      "isRepeatable": false,
      "autoAcceptOnNewGame": true,
      "requiredQuestIds": [],
      "autoAcceptNextQuestIds": ["quest_sub_survivor_lost_pack"],
      "objectives": [
        {
          "objectiveId": "obj_record_first_outer_trials",
          "description": "외곽의 세 시련을 마치고 첫 원정 기록을 완성한다.",
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
      "questName": "정산표 뒷면",
      "shortSummary": "첫 귀환 기록에서 지워진 도전자들의 흔적을 찾는다.",
      "description": "첫 정산표의 뒷면에는 같은 출발점으로 돌아왔던 사람들의 이름과 실패 사유가 희미하게 남아 있었다. 안내인은 그 이름들이 패배해서 지워진 것이 아니라, 시련장의 규칙을 거부했기 때문에 숨겨졌을 가능성을 제시했다.",
      "requiredProgress": 10,
      "rewardGold": 80,
      "rewardExp": 80,
      "isRepeatable": false,
      "autoAcceptOnNewGame": false,
      "requiredQuestIds": ["quest_sub_guide_broken_lantern"],
      "autoAcceptNextQuestIds": ["quest_sub_herbalist_lake_herb"],
      "objectives": [
        {
          "objectiveId": "obj_read_first_settlement_record",
          "description": "첫 사이클을 정산하고 귀환 기록의 뒷면을 확인한다.",
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
      "shortSummary": "두 번의 원정 지도를 겹쳐 시련장이 바꾼 부분을 찾는다.",
      "description": "두 번째 원정의 길과 보스 배치는 첫 번째 기록과 달랐지만, 안내인이 표시한 관측 사각지대만은 움직이지 않았다. 시련장은 길과 적을 바꿀 수 있어도 사람들이 서로 돕는 순간까지는 정확히 계산하지 못한다.",
      "requiredProgress": 30,
      "rewardGold": 100,
      "rewardExp": 100,
      "isRepeatable": false,
      "autoAcceptOnNewGame": false,
      "requiredQuestIds": ["quest_sub_survivor_lost_pack"],
      "autoAcceptNextQuestIds": [],
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
      "questName": "먼저 고개 숙인 수호자",
      "shortSummary": "외곽 수호자들이 보인 이상한 예의를 관찰한다.",
      "description": "외곽의 수호자들은 쓰러지기 직전 잠시 공격을 멈추고 도전자에게 고개를 숙였다. 단순한 괴물의 행동이라기보다, 정해진 역할을 끝까지 수행하는 시험관의 인사처럼 보였다.",
      "requiredProgress": 0,
      "rewardGold": 60,
      "rewardExp": 60,
      "isRepeatable": false,
      "autoAcceptOnNewGame": true,
      "requiredQuestIds": [],
      "autoAcceptNextQuestIds": ["quest_sub_hunter_spider_web"],
      "objectives": [
        {
          "objectiveId": "obj_observe_outer_guardians",
          "description": "외곽의 세 시련을 통과하며 수호자들의 반응을 관찰한다.",
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
      "questId": "quest_sub_hunter_spider_web",
      "questName": "평가자의 호칭",
      "shortSummary": "중앙 평가자가 보쿠세이를 부른 호칭의 의미를 추적한다.",
      "description": "중앙 평가자는 보쿠세이를 침입자나 용사가 아니라 '이번 회차의 대표자'라고 불렀다. 시련장이 한 사람의 무력을 재는 것이 아니라, 도전자와 수호자가 어떤 관계를 만드는지 평가하고 있다는 단서다.",
      "requiredProgress": 10,
      "rewardGold": 80,
      "rewardExp": 80,
      "isRepeatable": false,
      "autoAcceptOnNewGame": false,
      "requiredQuestIds": ["quest_sub_hunter_skeleton_patrol"],
      "autoAcceptNextQuestIds": ["quest_sub_highland_golem_trace"],
      "objectives": [
        {
          "objectiveId": "obj_hear_central_evaluator",
          "description": "중앙 평가를 마치고 평가자가 남긴 호칭을 기록한다.",
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
      "questName": "마지막 서명",
      "shortSummary": "최종 정산 기록에 남은 수호자들의 서명을 확인한다.",
      "description": "세 번째 정산 기록에는 도전자 한 명의 이름만 적히지 않았다. 원정에서 만난 수호자와 해방된 탐험가의 흔적이 같은 통과자 명단에 남았다. 시련장이 끝내 인정한 것은 혼자 살아남는 힘이 아니라 함께 다음 길을 여는 능력이었다.",
      "requiredProgress": 20,
      "rewardGold": 120,
      "rewardExp": 120,
      "isRepeatable": false,
      "autoAcceptOnNewGame": false,
      "requiredQuestIds": ["quest_sub_hunter_spider_web"],
      "autoAcceptNextQuestIds": [],
      "objectives": [
        {
          "objectiveId": "obj_confirm_final_signatures",
          "description": "세 번째 사이클을 정산하고 최종 통과자 명단을 확인한다.",
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
