# 생명의 호수 DLG 연출 적용 설계

> **현재 대화·합류 동기 기준:** [대화·진행 개정 v3](LAKE_OF_LIFE_DIALOGUE_REVISION_V3.md). 아래의 막힌 길, 전원 신전 지향, 천 삽화 호출, 묘령의 봉쇄·감시 동기는 폐기되었다. 나머지 시스템 계약은 유지한다.

> 버전: 0.1  
> 작성일: 2026-08-28  
> 상태: 검토중  
> 대상: `Assets/10.Datas/Dialogue/Story/Dialogue/`의 DLG 25개 / Talk 노드 138개  
> 구현 상태: 대사 문안만 반영. 이 문서의 연출 데이터·코드·Flow 변경은 아직 적용하지 않음.

## 1. 목표와 범위

일반 대화는 읽기와 조작의 주도권을 보존하고, 단서 조사와 신전 진입, 다른 자신과의 조우만 연출 리듬을 통제한다.

이번 설계가 지키는 경계는 다음과 같다.

- 대사 분기와 기존 nodeId·GUID는 보존한다.
- 일반 NPC 대화는 수동 진행과 자동 카메라를 기본값으로 둔다.
- 연출 앵커는 신전 도착과 다른 자신의 리빌부터 검증한다.
- 여러 시스템의 순서를 묶는 장면은 DLG 액션이 아니라 기존 FlowGraph가 소유한다.
- 신규 삽화는 만들지 않는다. 현재 프로젝트에 없는 신전·다른 자신·챕터 카드 이미지는 리소스 확정 전까지 3D 카메라와 기존 시네마틱 텍스트로 표현한다.

## 2. 조사로 확정된 현재 런타임 계약

| 영역 | 현재 동작 | 설계 판단 |
|---|---|---|
| 초상화 | 노드 오버라이드 → 선택된 주인공 전신 이미지 → `SpeakerPortraitTable` 기본 초상화 순으로 매 라인 해석한다. | 빈 값은 직전 초상화 상속이나 숨김이 아니다. 현재 요구만으로 `PortraitPresentationMode` enum을 추가하지 않는다. |
| DLG 액션 | `eventActions`는 노드 진입 시 실행되고, 삽화·주시 포인트 요청은 해당 Talk/Choice 라인을 소유자로 기록한다. | 퀘스트·플래그처럼 장면 종료 뒤 실행돼야 하는 진행 이벤트는 FlowGraph에 둔다. |
| 일반 삽화 | 기본적으로 현재 대사 한 줄에만 유효하다. 다음 라인에 새 큐가 없으면 해제된다. | 조사 장면 전체 유지가 필요한 경우에만 기존 `PersistAcrossFollowingLines`를 일반 삽화 액션에 노출한다. |
| 지속 삽화 | `DialogueIllustrationPresentation.PersistAcrossFollowingLines`가 이미 구현되어 있다. 같은 배경이면 다음 라인에서 배경 트윈을 재시작하지 않고 전경만 교체한다. | 별도 생명주기 시스템을 만들지 않는다. |
| 오프닝 | 두 액션 모두 같은 배경을 쓰고 지속 옵션을 켠다. 2번 라인에서 선택 주인공 전경만 추가된다. | 현재 구조는 이미 2라인 시네마틱 블록이다. 9.6초 모션과 7초 대화 길이가 다르다는 사실만으로 오류로 보지 않고 화면 속도를 Play Mode에서 조정한다. |
| 자동 진행 | `autoAdvanceDuration`은 타이핑 완료 뒤의 대기 하한이다. 전역 AUTO가 켜지면 전역 지연과 `max`로 결합하고, 삽화가 있으면 최소 노출 시간을 추가 확보한다. 정지 중에는 카운트하지 않는다. | 제안서의 장면 총시간을 필드에 그대로 넣지 않는다. 타이핑 시간과 후행 대기를 합친 목표 총시간으로 보정한다. |
| 카메라 | `shotType`, `shotTransition`, `listenerSpeakerId`, `reactionSpeakerId`, `focusSpeakerId`, `cameraRecording`이 이미 `DialogueManager`와 Camera 모듈에 연결되어 있다. | 신규 연결 작업이 아니라 핵심 노드 데이터 저작과 Play Mode 검증이 필요하다. |
| 모션 | 빈 motion ID도 `DialogueMotionCatalog`의 Neutral 풀에서 제스처를 뽑는다. 현재 카탈로그는 `Talk_01` 등 범용 10종만 가진다. | 의미가 강한 5종을 새로 가정하지 않는다. 기존 클립을 먼저 분류한 뒤 사람이 읽는 motion ID를 추가한다. |
| 채널 | 현재 enum은 `Main`, `System`, `Monologue`이며 UI 채널 분리에 실제 사용된다. `Ambient` 채널은 없다. | 이동 가능한 Bark는 별도 2차 기능이다. 조사 DLG는 당분간 Main 수동 진행을 유지한다. |
| 챕터 카드 | `lake_alternate_victory_10`은 이미 `CinematicNarration`으로 저작되어 일반 대화 패널을 숨기고 중앙 시네마틱 텍스트를 사용한다. | 별도 Show 액션은 필수가 아니다. `CinematicLocationTitle` 전환과 화자 메타데이터 정리만 먼저 검토한다. |

## 3. 현재 데이터 정합성 판정

### 3.1 즉시 수정 후보지만 이번 단계에서 보류한 항목

| 우선순위 | 항목 | 조사 결과 | 다음 판단 |
|---|---|---|---|
| P0 | 상인 초상화 | `DLG_Npc_Shop`의 2개 노드 오버라이드와 `SpeakerPortraitTable`의 `상인` 항목이 모두 `Npc_Penny_Dialogue_Portrait`를 가리킨다. 상인 전용 초상화는 현재 리소스 폴더에 없다. | 연출 적용 1단계에서 노드 오버라이드와 테이블 항목을 함께 비워 텍스트 전용으로 전환한다. 전용 아트가 들어오면 테이블 한곳에만 등록한다. |
| P0 확인 필요 | 라온 초상화 | 일반 `DLG_Npc_Raon`과 테이블이 `MonsterRaon_Dialogue_Portrait`를 사용한다. 별도의 일반 대화 초상화는 없고 `Player_Raon_Full`만 있다. | 이 대화가 정상 라온인지 변형체인지 콘텐츠 소유자가 확정한 뒤 교체한다. 이름만으로 임의 수정하지 않는다. |
| P1 | 챕터 카드 화자 | 화면은 시네마틱 텍스트지만 내부 `speakerId`가 `Protagonist`다. 대화 이력과 추후 음성 메타데이터에서 주인공 발화로 기록될 수 있다. | `System` 또는 화자 없는 시네마틱 라인으로 정리하고 로그 정책을 함께 검증한다. |
| P1 | 자기 자신 listener | 138개 Talk 중 20개가 `listenerSpeakerId == speakerId`다. 런타임은 이를 무시하고 자동 폴백하지만, 3인 이상 장면에서 저작 의도가 사라진다. | 실제 응답 대상을 다시 지정하고 일반 검증 규칙으로 재발을 막는다. |
| P1 | 상인 nodeId | `penny_player_reply`, `penny_followup`이 남아 있다. | nodeId는 불변 자산이므로 변경하지 않는다. 에디터에는 표시명 또는 저작 메모를 추가하고, 검증기는 잔존 ID를 정보성으로만 보고한다. |

### 3.2 화린·리안 힌트 액션 판정

현재 세 이미지는 캐릭터 소개 카드가 아니라 단서 삽화다.

| 액션 | 실제 이미지 | 실제 의미 |
|---|---|---|
| `Action_ShowHwarinDialogueHint` | `img_dlg_hint_hwarin.png` | 흙이 묻은 남색 천 |
| `Action_ShowLianDialogueHint` | `img_dlg_hint_lian.png` | 나뭇가지에 묶인 붉은 천 |
| `Action_ShowDragTracksHint` | `img_dlg_hint_track.png` | 바닥의 거대한 끌린 흔적 |

`DLG_Lake_RedCloth`가 `Action_ShowLianDialogueHint`를 쓰는 것은 현재 이미지 내용과 일치한다. 새 `img_dlg_hint_red_cloth.png`를 만들 이유가 없다.

또한 `DLG_Test_HwarinJoined`는 그래프명부터 “리안의 붉은 표식”이고 첫 라인에서 붉은 천을 보여 준다. `DLG_Test_LianJoined`는 남색 천 단서를 보여 주는 테스트 그래프다. 따라서 액션을 캐릭터 이름만 보고 서로 교환하지 않는다.

혼동의 원인은 액션 이름이다. 적용 단계에서는 GUID와 이미지 참조를 유지한 채 다음처럼 의미 중심으로 이름을 정리한다.

```text
Action_ShowLianDialogueHint   → Action_ShowRedClothHint
Action_ShowHwarinDialogueHint → Action_ShowNavyClothHint
```

`Action_ShowLianNavyMarkerHint`는 남색 천 이미지를 중복 참조하며 현재 미참조다. 참조 재검사 후 Deprecated 목록에 넣고, 바로 삭제하지 않는다.

## 4. 장면별 연출 설계

### 4.1 새 게임 오프닝

현재의 지속 배경 + 2번 라인 주인공 전경 추가 구조를 유지한다.

```text
1번 라인 진입
→ 필드 배경 즉시 리빌, 느린 팬/줌 시작
→ 4.3초 자동 진행
→ 같은 배경을 유지한 채 선택 주인공 전경 추가
→ 2.7초 자동 진행
→ DLG 종료 페이드
```

- 두 라인은 3D 카메라와 초상화를 사용하지 않는다.
- 9.6초 모션을 7초로 기계적으로 맞추지 않는다. 7초 시점의 실제 프레이밍을 캡처해 끝 스케일만 조정한다.
- 2번 라인은 내부 화자 구분이 필요하지만 화면 화자명은 숨긴다. `ProtagonistInner` 추가 여부는 음성·로그 정책과 함께 결정한다.
- 스킵과 Cancel에서 삽화 레이어·전경·정렬 순서가 원복되는지 확인한다.

### 4.2 일반 NPC와 짧은 조사 DLG

- 안내인, 미아, 준, 조안, 헤이즐, 모건, 페니, 상인 등은 수동 진행을 유지한다.
- 기본 카메라는 `Auto`, 기본 모션은 Neutral 랜덤을 사용한다.
- NPC끼리 대화하는 라인만 `listenerSpeakerId`를 명시하고, 화자와 같은 값이면 오류로 본다.
- 약초·등불·수레 조사는 1~3줄 Main 대화로 유지한다. `Ambient` 런타임이 생기기 전에는 이동 중 Bark로 간주하지 않는다.
- 퀘스트 시작·완료·알림은 대사 노드 진입 액션보다 FlowGraph의 대화 완료 뒤 노드에 둔다.

### 4.3 붉은 천과 끌린 흔적

붉은 천은 현재 단일 라인 진입 시 올바른 붉은 천 삽화를 보여 준다. 액션 이름만 의미 중심으로 정리한다.

끌린 흔적도 이미 `dlg_lake_drag_tracks_01`에서 `Action_ShowDragTracksHint`를 실행한다. “마지막 줄에서 이미지가 늦게 뜬다”는 문제는 현재 데이터에는 없다.

다만 흔적을 설명하는 네 줄 동안 이미지를 유지하는 편이 읽기에는 더 좋다. 이를 위해 일반 삽화 액션에 다음 직렬화 옵션을 추가한다.

```csharp
[SerializeField] private bool _persistAcrossFollowingLines;
```

- 기존 에셋의 기본값은 `false`로 두어 동작을 바꾸지 않는다.
- `Action_ShowDragTracksHint`만 `true`로 저작한다.
- 마지막 라인 진행 또는 대화 종료에서 자동 해제한다.
- 지속 삽화는 진행 입력으로 중간 닫기보다 다음 라인 진행을 우선한다. `Cancel`과 강제 Skip에서는 즉시 정리한다.

### 4.4 신전 도착

신전 도착 전용 삽화는 프로젝트에 없다. 1차 구현은 기존 3D 대화 카메라와 환경음으로 만든다.

이 장면은 이미 `FLOW_LakeShrineChapter1`이 소유하므로 다음 순서를 같은 Flow에 둔다.

```text
신전 도착 트리거
→ 환경음 저감/정적 강조
→ DLG_Lake_ShrineArrival
→ 도착 목표 반영과 플래그 기록
→ 수호자 등장
```

| 노드 | 진행 | 카메라 | 모션 |
|---|---|---|---|
| `lake_shrine_arrival_01` | 자동 | `Wide` + `Establish` | 리안 관찰 제스처 후보 |
| `lake_shrine_arrival_02` | 자동 | `Auto` | 묘령 최소 동작 |
| `lake_shrine_arrival_03` | 자동 | 화린 `Closeup` 또는 `Reaction` 비교 | 경계 반응 후보 |
| `lake_shrine_arrival_04` 이후 | 수동 | `Auto` | 핵심 라인만 지정 |

`autoAdvanceDuration`은 장면 총시간이 아니라 타이핑 뒤 대기다. 목표 총시간을 먼저 정하고 다음 식으로 저작한다.

```text
라인 목표 총시간 = 실제 타이핑 시간 + autoAdvanceDuration
```

1~3번을 합쳐 약 5.5~6.5초를 목표로 하고, 한국어 타이핑 속도 설정과 전역 AUTO 지연을 포함해 Play Mode에서 보정한다.

환경음 저감이 대사보다 먼저 들려야 한다. 대화 진입 액션이 아니라 FlowGraph의 선행 연출 노드로 두고, 대화 취소·씬 이탈에서도 원복되는 소유권을 갖게 한다.

### 4.5 보물과 다른 자신 리빌

현재 Flow는 다음 순서다.

```text
DLG_Lake_ShrineTreasure 11줄 전체
→ 보물 도착 기록
→ 주인공 Variant 해석
→ 다른 자신 스폰
→ 전투
```

따라서 현재 `lake_shrine_treasure_09~11`에서 “누가 있어”, “저건 너야”라고 말할 때 실제 다른 자신 액터는 아직 없다. 카메라·모션 저작보다 먼저 고쳐야 하는 P0 흐름 문제다.

권장 순서는 다음과 같다.

```text
보물 관찰 대화 01~08
→ 보물 도착 기록
→ 주인공 Variant 해석
→ 다른 자신 스폰
→ 스폰 액터 AI/전투를 대화 홀드 상태로 전환
→ 리빌 대화 09~11
→ 다른 자신: “이루고 싶은 게 있잖아.”
→ 대화 홀드 해제
→ 전투 시작
```

구현 원칙:

- `DLG_Lake_ShrineTreasure`를 관찰부와 리빌부로 분리하되 기존 01~11 nodeId는 보존한다.
- 새로 추가하는 다른 자신의 라인만 새 nodeId를 발급한다.
- FlowGraph의 `Spawned` 출력과 `WaitStoryActorDefeatedNode` 사이에 리빌 DLG를 배치한다.
- 다른 자신은 주인공 선택에 따라 `BossAlternateSelfRaon` 또는 `BossAlternateSelfNenmir`로 달라진다. 문자열 이름 분기 대신 Flow가 해석한 액터 인스턴스와 DLG의 `AlternateSelf` 화자 별칭을 명시적으로 연결하는 요청 계약을 추가한다.
- 별칭 연결은 `PlayDialogueNode`의 선택적 데이터 입력으로 설계하고, Dialogue 모듈이 Lake 전용 VariantSet을 직접 참조하지 않게 한다.
- 리빌 DLG가 시작되면 스폰 보스도 `IDialogueStageActor` 홀드 대상이 되어야 한다. 대화 중 AI가 공격을 시작하면 실패다.
- 복잡한 등장 동작과 수면 반사는 `cameraRecording` 한 개가 소유한다. 자동 카메라와 녹화 카메라를 같은 라인에 섞지 않는다.

진행 리듬:

- 관찰 01~03은 짧은 자동 진행, 04~08은 수동 진행으로 둔다.
- 리빌 09~11과 다른 자신의 첫 라인은 자동 진행한다.
- 보스가 화면에 잡히기 전 설명 대사가 나오지 않게 한다.
- 리빌 마지막 음성과 전투 HUD 활성 사이에 0.3~0.5초 입력 잠금 해제 여유를 둔다.

### 4.6 전투 승리와 챕터 엔드

현재 Flow는 승리 DLG가 정상 종료된 뒤 `lake.story.chapter1_completed` 플래그와 퀘스트 완료를 기록한다. 이 순서는 유지한다.

```text
보스 소멸
→ 0.7~1.0초 정적
→ AlternateSelf: “아직 아니야.”
→ 승리 후 대화
→ 주인공 결심
→ 중앙 챕터 카드 2.5초
→ 챕터 완료 플래그와 퀘스트 완료
```

- `lake_alternate_victory_01`의 화자 메타데이터를 `AlternateSelf`로 정리한다.
- `lake_alternate_victory_10`은 일반 Show 액션을 새로 만들기보다 `CinematicLocationTitle` 사용을 우선한다.
- 챕터 카드에는 화자명·초상화·대화 프레임·타이핑을 표시하지 않는다.
- 카드 문구는 최종 제목이 확정되기 전까지 `CHAPTER 1 — END`를 유지한다.
- DLG를 Cancel한 경우 현재 Flow의 `continueWhenCancelled = true` 때문에 챕터 완료가 진행된다. 반복 불능 방지 의도인지 확인하고, 연출 취소를 정상 완료로 볼지 별도 승인한다.

## 5. 초상화와 표정의 확장 원칙

현재 구조는 이미 “화자 기본 초상화 + 라인별 오버라이드”를 지원한다. 동일 화자의 빈 노드에 같은 이미지를 반복 지정하지 않는다.

현재 실제 요구는 다음 두 가지뿐이다.

1. 잘못된 기본/오버라이드 참조를 제거한다.
2. 표정 아트가 준비된 핵심 라인만 노드 오버라이드를 사용한다.

`Inherit`, `Hide`까지 포함한 새 enum은 다음 조건 중 하나가 실제로 생길 때만 추가한다.

- 기본 초상화가 있는 화자를 특정 일반 Talk 라인에서만 숨겨야 한다.
- 동일 화자의 직전 표정 상태를 여러 라인에 걸쳐 의도적으로 유지해야 한다.

표정 우선순위는 화린, 리안, 묘령, 주인공, 다른 자신 순으로 두되, 실제 Sprite가 들어오기 전에는 데이터 필드를 미리 채우지 않는다.

## 6. 카메라와 모션 저작 앵커

전체 138개 노드에 값을 넣지 않는다. 다음 앵커만 수동 저작하고 나머지는 자동 디렉터와 Neutral 모션에 맡긴다.

| 장면 | 노드 | 1차 목표 |
|---|---|---|
| 안내인 첫 대면 | `lake_guide_intro_01` | `TwoShot` 비교 검증 |
| 길 안내 | `lake_guide_intro_05` | 안내인 화자 샷 + 방향 제스처 후보 |
| 끌린 흔적 | `dlg_lake_drag_tracks_01` | 삽화 우선, 3D 카메라 강조 금지 |
| 신전 첫 공개 | `lake_shrine_arrival_01` | `Wide` + `Establish` |
| 발자국 확인 | `lake_shrine_arrival_05` | 리안 `Closeup` 또는 씬 주시 포인트 |
| 묘령 선도 | `lake_shrine_arrival_06` | 묘령 화자 샷 |
| 보물 발견 | `lake_shrine_treasure_01` | 제단과 파티의 와이드 |
| 다른 발자국 | `lake_shrine_treasure_04` | 씬 주시 포인트 또는 녹화 리빌의 전반부 |
| 인물 발견 | `lake_shrine_treasure_09` | 화린 반응 컷 |
| 경고 | `lake_shrine_treasure_10` | 묘령 빠른 `Closeup` + `Cut` |
| 다른 자신 등장 | 리빌 DLG | 단일 `cameraRecording` |
| 승리 후 결심 | `lake_alternate_victory_09` | 주인공 `Closeup` |

현재 Motion Catalog에는 의미 이름이 아니라 `Talk_01`, `Talk_05` 같은 범용 ID만 있다. 다음 순서로 정리한다.

1. 10개 기존 클립을 캐릭터별로 실제 재생해 손·상체·루프 종료 자세를 기록한다.
2. 방향 지시, 지면 조사, 경계 둘러보기, 무기 대비로 안전하게 쓸 수 있는 클립만 선별한다.
3. 기존 GameplayTag를 유지한 채 사람이 읽는 motion ID를 카탈로그에 추가한다.
4. 핵심 앵커에만 ID를 지정하고, 나머지는 Neutral 랜덤을 유지한다.

대화 종료 시 Idle 복귀와 라인 전환 중 제스처 끊김을 두 캐릭터에서 먼저 검증한 뒤 범위를 넓힌다.

## 7. 검증 도구 설계

일반 규칙은 기존 `GeneralDataValidator.ValidateDialogue`에 추가하고, 생명의 호수 고유 참조는 EditMode 콘텐츠 테스트로 분리한다.

### 7.1 일반 검증 규칙

- `[Error]` `listenerSpeakerId == speakerId`
- `[Error]` `cameraRecording`과 포인트 주시 액션 또는 주목 컷을 같은 라인에 함께 저작
- `[Warning]` Standard Talk인데 `speakerId`가 비어 있음
- `[Warning]` `CinematicNarration`/`CinematicLocationTitle`인데 자동 진행 시간이 0
- `[Warning]` 챕터 형식 텍스트가 Standard Talk로 저작됨
- `[Warning]` 존재하지 않는 motion ID를 지정함
- `[Warning]` `DLG_Test_*`가 Test 폴더 밖의 Flow·Story에서 참조됨

### 7.2 생명의 호수 콘텐츠 테스트

- 상인과 페니가 같은 초상화 GUID를 공유하지 않음
- 일반 라온의 초상화 의도가 확정된 기대값과 일치함
- 붉은 천 DLG가 실제 붉은 천 이미지 액션을 참조함
- 끌린 흔적 액션이 첫 Talk 노드에서 시작되고 DLG 종료까지 유지됨
- 오프닝 두 액션이 같은 배경을 사용하고 첫 액션의 지속 상태가 두 번째 라인으로 이어짐
- 신전 리빌 DLG 시작 전에 선택 주인공에 대응하는 다른 자신 액터가 존재함
- 리빌 중 다른 자신이 대화 홀드 상태이고 전투 AI가 시작되지 않음
- 승리 DLG 정상 종료 뒤에만 챕터 완료 플래그와 퀘스트 완료가 실행됨

액션 이름에 포함된 캐릭터 문자열만 보고 교차 오류를 판정하는 검사는 만들지 않는다. 이미지의 서사 역할과 실제 참조를 기대값으로 검사한다.

## 8. 적용 단계와 완료 게이트

### 1단계 — 기준선 캡처와 데이터 정합성

- 오프닝, 붉은 천, 끌린 흔적, 신전 도착, 보물, 승리 DLG를 현재 상태로 Play Mode 녹화
- 상인 초상화 제거
- 라온 초상화 의도 확정 후 수정 또는 현상 유지
- 힌트 액션을 의미 중심 이름으로 정리하고 `.meta`를 함께 이동
- 챕터 카드 화자 메타데이터와 `CinematicLocationTitle` 검증

완료 게이트: 대사 분기·nodeId·Flow 플래그 diff 없음, Missing 참조 0, 대화 로그 화자 오염 0.

### 2단계 — 기존 시스템 안의 연출 저작

- 끌린 흔적 지속 삽화 옵션
- 신전 도착 1~3번 자동 진행과 카메라 앵커
- 카탈로그의 기존 모션 분류와 핵심 라인 지정
- 일반/호수 전용 검증 규칙

완료 게이트: 패드 수동 진행, AUTO, 타이핑 스킵, 강제 Skip, Cancel에서 입력과 화면 상태가 복구됨.

### 3단계 — 다른 자신 리빌 구조

- 보물 DLG 관찰부/리빌부 분리와 기존 nodeId 보존
- FlowGraph 스폰 순서 수정
- Flow가 해석한 동적 액터와 DLG 화자 별칭을 연결하는 일반 계약
- 보스 대화 홀드
- 단일 `cameraRecording` 저작

완료 게이트: 라온/아린 두 주인공 Variant에서 보스가 리빌 전에 존재하고, 리빌 중 공격하지 않으며, 마지막 대사 뒤에만 전투가 시작됨.

### 4단계 — 통합 검증

- 정상 순서, 역순 접근, 중복 트리거, 저장/로드, 대화 Cancel
- 게임패드만으로 진행/스킵/취소
- Dialogue/FlowGraph EditMode와 PlayMode 테스트
- Unity 컴파일 오류 0, 서비스 경고·예외 0, Player Build 오류 0

## 9. 승인 전 결정이 필요한 항목

1. `DLG_Npc_Raon`이 정상 라온인지 변형체인지
2. 상인 전용 초상화가 들어오기 전까지 텍스트 전용으로 보일지
3. 승리 DLG를 Cancel해도 현재처럼 챕터 완료를 진행할지
4. 다른 자신의 첫 음성을 `이루고 싶은 게 있잖아.`로 확정할지
5. 챕터 카드의 최종 표기를 `CHAPTER 1 — END`로 유지할지, 장 제목을 함께 쓸지

이 다섯 항목이 확정되기 전에는 관련 연출 에셋과 Flow를 수정하지 않는다.
