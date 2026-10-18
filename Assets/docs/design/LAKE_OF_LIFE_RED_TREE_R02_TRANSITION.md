# 붉은 나무 R02 실행·저장 전환

> 최신 검증: **R03 본편 연결·전환과 최종 Windows Player 라온/아린 완주 수행.** 영입 직후 저장과 모델 준비 전 교체 결함 수정 후 관련 EditMode 28/28·전체 Play Mode 24/24, 최종 빌드 오류 0·경고 22, 두 Player 실행 오류 0. 취소·사망 부활·저장 재진입, 실제 구 저장의 복사 이관·본편 재개와 원본 해시 보존을 확인했다. 기존 전체 회귀 실패·타이틀 프리뷰 무기 표시 문제·실물 조작 품질 미검증은 남아 있다. 수행 범위와 중간 실패 증거는 [R03 기록 12절](LAKE_OF_LIFE_RED_TREE_R03_IMPLEMENTATION.md#12-windows-player-실행과-저장-경계-재검사)을 따른다. 아래 과거 상태와 구분하며 프로젝트 전체 통과로 해석하지 않는다.

> 2026-10-18 추가 실행: 별도 검증 씬에서 라온·아린의 실제 행동·전투·영입·릴리 안내를 실행했다. 기존 P04 저장의 격리 복사 보존 검사도 통과했다. 복원/재접근 결함과 후속 수정, 수행·미수행 구분은 [R03 기록 9~10절](LAKE_OF_LIFE_RED_TREE_R03_IMPLEMENTATION.md#10-두-주인공-실행-후-발견한-복원-결함과-보완)을 따른다. 아래 이전 상태 문구는 당시 기록이며, 본편 일괄 전환은 적용했고, 전체 회귀의 실패와 최종 Player 검증을 확인 중이다. 최신 본편 적용 증거는 R03 기록 11절을 따른다.

> 2026-10-17 / 상태: 전환 설계 작성, 코드·비활성 데이터 준비. 현행 씬 전환·복사 슬롯 실검증은 미적용/미수행. [R03 구현 기록](LAKE_OF_LIFE_RED_TREE_R03_IMPLEMENTATION.md).
> 후속: 안전 인계 전 접근·저장 재개 코드를 보완했다. MCP로 Unity EditMode 32개·Play Mode 복구 5개와 현행 씬 로드/일부 공간 캡처를 확인했다. 실제 행동 연결·일괄 전환·완주·복사 슬롯은 남아 있다(R03 기록 8절). 아래 도구 부재 설명은 연결 전 조사 당시 기록이다.
> [확정 기획](LAKE_OF_LIFE_RED_TREE_STORY_PLAN.md), [제작 명세](LAKE_OF_LIFE_RED_TREE_IMPLEMENTATION_SPEC.md), [R01](LAKE_OF_LIFE_RED_TREE_R01_SCENARIO.md)를 따른다.

## 1. 재조사와 적용 경계

[현행 실행선 JSON](../../../output/RedTreeR02R03/execution-audit.json)은 이번 파일 조사 결과다. 씬 러너 7개의 component fileID·graph GUID·graphId·시작 설정, 직접 NPC 데이터 참조, Quest 자동 수락, 생성기 참조를 기록했다. 시나리오 씬·layout/world·PartyMemberData·화자 테이블 해시는 R00와 일치한다. 과거 검사 통과를 승계하지 않는다.

Unity 6000.3.21f1 프로세스는 있으나 이 세션에 Unity 제어 도구가 없다. 네이티브 앱 제어도 사용할 수 없다. 미저장 상태를 알 수 없는 열린 Editor에 두 번째 Editor를 붙이거나 잠금 파일을 제거하지 않는다. 실물 확인에 종속되는 배치·구도·모션 확정은 보류한다. **R03 준비 데이터가 생겼다는 이유로 구 씬을 반쯤 전환하지 않는다.** 새 본편의 모든 필수 연결과 현장 검수가 갖춰질 때 아래 전환 단위를 한 번에 적용한다.

## 2. 기존 실행선과 ID/GUID 대응

| 분류 | 실제 대상 | 처리 |
|---|---|---|
| 유지 | Boot → UI_Scene_TitleMenu.OnCharacterSelected → Save.ResetForNewGame → Party.PrepareNewGameStartingCharacter → 기본 맵 | 선택 Raon=1/Arin=13, StoryProtagonist와 Roster/BattleOrder 1명 계약 재사용. 직접 씬 시작은 이 검사의 대체가 아님 |
| 유지 | LakeOfLifeScenario 씬 GUID, Terrain, Heart_BaseShrine/Heart_ShrineGate, 원본 LakeOfLife | 지형·건물·기물 삭제 없음. 중앙 신사가 없다고 다시 만들지 않음 |
| 유지 | enum 정수·구 이름 저장 별칭·기존 모델/무기/초상화 GUID | Reine 표시명만 별도 정합화 대상. 이번 R03 범위에서 enum·파일명 변경 없음 |
| 의미 보존 수정 | 영입 정상 완료 증명, 취소·재시도, 이미 보유 상태 처리 | 기존 전투 해결 의미를 유지하고 안전 확인 조건을 추가. 전투 없는 동의에는 전투 승리를 발급하지 않음 |
| 실행선 퇴역 | ScenarioFirstQuest / `flow_lake_search_quest_line` / `030b1ed3ae51cd74ca4b3d4c80ff685a` | 신규 도입 Flow로 교체. 원본 FLOW_LakeSearchQuestLine과 Scenario 복제본 에셋·GUID는 보관 |
| 실행선 퇴역 | HwarinRescue / `flow_test_honoka_rescue` / `c51c2e0e090385d4197e50ad910ecf52` | 전투 자산은 후보로 보존. 새 encounter/graph/사건 ID를 사용하고 옛 승리를 가져오지 않음 |
| 실행선 퇴역 | LianRescue / `flow_test_lian_rescue` / `25b3238f26014a74cabbfc64c3205a7d` | S03 이후 자동 실행 금지. 러너뿐 아니라 Anchor·볼륨·상호작용도 닫음 |
| 실행선 퇴역 | MyoRyeongDuel / `flow_test_siuha_duel` / `e7736bbfc8477ab4fa2ddc69c1d852f2` | 위와 동일. 기존 전투 참가자를 새 사건 완료로 전용하지 않음 |
| 실행선 퇴역 | OptionalFacts / `flow_scenario_optional_facts` / `b1a3b1fe5368b6d45bd0a8c3d5d39c77` | 자동 Resume와 flag/reload 구독을 함께 해제 |
| 실행선 퇴역 | JunRescue / `flow_lake_jun_rescue` / `9b4cb4db2ebd8cb4e80464c997716db1` | 옛 준 구조를 새 피난민 안전 확인으로 매핑하지 않음 |
| 실행선 퇴역 | HerbRoute / `flow_lake_side_quest_route` / `93839b468c6c49d1a3b6df97ea835f0b` | 옛 약초·등불·서쪽길 진행은 새 식사/돌봄/거처와 분리 |
| 실행선 퇴역 | `flow_lake_shrine_chapter1`, `quest_main_lake_shrine`, `obj_defeat_alternate_self` | 새 본편 등록 금지. 구 분신/신전 완료는 새 진실·루시아 승리·엔딩으로 매핑하지 않음 |
| 신규 ID | `flow_red_tree_introduction`, `flow_red_tree_hwarin_rescue`, `lake.red_tree.hwarin_rescue` | 구 firedKeys와 충돌하지 않는 별도 그래프·조우. S03~S13 데이터 자동 생성 안 함 |
| 신규 ID | `quest_main_red_tree_s00/s01/s02` | QuestDatabase에 신규 에셋을 등록하되 준비 중 isContentEnabled=0. 활성화는 씬 전환과 같은 단위 |

직접 NPC 연결: Lucia/Joy/Joan은 공용 NPC 데이터, Guide/Mia/Hazel/Penny/Morgan은 Scenario 복제 데이터다. 공용 NPC를 덮어쓰면 원본 맵까지 변하므로 새 시나리오 전용 데이터에 필요한 대화만 연결한다. 씬 prefab override·ActorSpawn 정의의 주민도 포함해 전환해야 한다. Flow만 바꾸면 NPC 대화에서 구 이야기가 다시 나온다.

구 수색→화린→리안→흔적→신전 퀘스트의 autoAcceptNextQuestIds도 실행선이다. 현재 autoAcceptOnNewGame=1인 `quest_sub_guide_broken_lantern`은 isContentEnabled=0이다. ‘자동 수락 없음’으로 단순화하지 않고 활성 여부까지 검사한다. 새 Q00~02에는 추가 재화/경험치/영입 보상이 없고 Q02 다음 자동 수락도 없다.

원본 Ingame RegionInfo의 Flow 등록은 원본 맵 전용으로 보존한다. Scenario RegionInfo.flowGraphs는 빈 배열이며 그것만 비워도 퇴역이 끝나는 구조가 아니다.

## 3. R03 사건·소유자 계약과 미연결 생산자

| 카드 | 새 사실/이벤트 ID | 완료 생산자와 소비자 |
|---|---|---|
| S00-A | `lake.red_tree.delivery`, `lake.red_tree.arrival_acknowledged` | 실제 짐 인도 상호작용/정상 응답을 분리. Q00는 응답까지 완료한 사실을 소비 |
| S01-A | `lake.red_tree.meal_served`, `lake.red_tree.meal_acknowledged` | 식사 인계 전후 배치 / 조이 대화 정상 종료. Q01 meal 목표 |
| S01-B | `lake.red_tree.care_delivered`, `lake.red_tree.care_space_ready`, `lake.red_tree.care_acknowledged` | 물품 인계와 자리 확보를 각각 실제 상호작용으로 기록. 둘 다 있어야 대화/목표 완료 |
| S01-C | `lake.red_tree.shelter_assigned`, `lake.red_tree.rescue_requested` | 실제 거처 사용 배치 / 루시아의 일 배정·구조 요청 정상 종료. Q01 shelter 목표 |
| S02-A | `lake.red_tree.escape_route_cleared`, `lake.red_tree.refugees_safe` | 새 조우의 실제 CombatResolved / 별도 안전 인계 상호작용. 처치만으로 안전·영입을 기록하지 않음 |
| S02-B | `lake.red_tree.hwarin_consent`, `lake.red_tree.handoff_acknowledged` | 안전 확인 뒤 정상 동의→실제 Roster 반영. 후속 안내는 별도 정상 종료 사실 |

표의 사실은 GlobalFlag가 저장하고 Flow가 Quest StoryEvent로 재동기화한다. 영입은 Party/Recruitment 서비스만 소유하며 퀘스트 보상은 0이다. 행동 사실을 대화 시작·볼륨 진입·스킵으로 대신 생산하지 않는다. interaction/volume/entity는 현장 배치 확정 후 등록하며 아직 없는 SceneEntityId를 기등록으로 기재하지 않는다.

진행 Flow의 Resume/OnFlagsReloaded는 Q 수락 → 저장 사실 재통지 순서다. 발화 자체를 OncePerSave로 소진하지 않고 카드 완료 플래그로 멱등성을 지킨다. 동의 대화 Cancel은 승리/안전 사실을 유지하고 재상호작용을 허용한다. 볼륨 안 로드는 ContainsActor/기존 WaitPlayerInVolume와 저장 단계 재개를 이용한다.

## 4. 저장 전환

- 컨테이너 `3.4`와 별도로 `lake.red_tree.content.v1`을 저장한다. 마이그레이션은 원본 파일을 직접 바꾸지 않고 빈 대상 슬롯에만 복사본을 만든다. 원본=대상, 기존 대상 파일/백업/임시파일, 알 수 없는 선택 주인공은 거부한다. ActiveSlot·현재 플레이 상태를 전환하지 않는다.
- Party의 주인공·Roster·출전 순서·활성 인덱스·성장·ASC 및 Inventory/장비를 그대로 보존한다. 이미 화린을 보유해도 새 사건 동의 플래그는 만들지 않는다. 미완료 동의는 보유 분기 원고로 재개한다.
- 복사본의 구 활성 퀘스트(위 퇴역 메인/연계/생활 사건)는 제거하고 구 완료/실패·flags·story·world·flow firedKeys는 이력으로 유지한다. 원본에는 활성 퀘스트까지 그대로 남는다. 구 추적 목표는 해제하고 새 Q00를 안내한다. 다른 지역 퀘스트의 추적/억제 선택은 보존한다.
- 새 graphId/nodeId가 구 firedKeys를 읽지 않는다. 원본 Flow가 돌아오면 이력만으로 차단된다고 가정하지 않고 실행 연결을 퇴역시킨다.
- 안전 재개는 확정된 도착 스폰을 사용한다. 좌표를 임의로 박지 않고 대상 Map/Scene을 명시하며 hasLocation=false로 기존 맵 기본 스폰을 사용한다. 실제 기본 스폰 안전성은 R00-V01/V06 확인 전 미검증이다. 재이관은 위치·진행을 다시 초기화하지 않는다.
- 구 분신 처치·신전 완료·옛 엔딩과 새 진실/루시아/엔딩 사이의 대응은 **없음**이다.
- 자동 로드 경로에 강제 이관을 넣지 않는다. 현장 전환 준비와 복사 슬롯 검증이 끝나기 전 기존 저장을 새 씬으로 조용히 옮기지 않는다.

## 5. 대화 지원 범위

`TryStartDialogueTracked`는 정상 종료와 onCancelled를 구별한다. RequiredDialogue는 정상 종료 시에만 attempt를 확인하고 Commit이 소비한다. 일반 넘김은 기존 대화 진행 입력이다. **명시적 장면 스킵은 별도 결과 계약이 없으므로 R03에서는 제공하지 않는다.** continueWhenCancelled를 true로 바꾸어 구현한 척하지 않는다.

기존 조우 상태 저장소는 CombatResolved에서만 영입을 허용한다. 비전투 합류는 별도 정상 동의 노드에서 Party.EnsureCharacterUnlocked를 호출하고 성공 확인 후 사건 사실을 남기는 최소 경로로 보완한다. 전투 조우의 승리 기록을 생성하지 않는다. R03 화린은 기존 RequiredDialogue/Commit 패턴을 유지하고, 안전 확인 사실을 대화 증명 발급 및 commit 양쪽에서 검사한다.

## 6. 퇴역 적용 순서와 재생성

1. R03 데이터·소품·NPC 전용 대화·안전 지점·화자·전투 자산의 실제 로드를 확인한다.
2. 같은 작업 단위에서 일곱 러너와 옛 Anchor/Trigger/Interaction 실행을 닫고 새 러너/인계 상호작용을 연결한다. 공용 맵 기물은 유지한다.
3. QuestDatabase의 새 3퀘스트를 활성화하고 퇴역 퀘스트·주민 대화의 자동 연계가 새 본편으로 들어오지 않는지 검사한다.
4. `ScenarioMapAuthoring.FirstRecruitment`, `Reunion`, `OptionalEvents`, `Herb`, `Integration`과 구 분신 생성기의 새 본편 재배선을 차단한다. 기존 도구를 삭제하지 않으며 새 본편 버전이 붙은 씬에 구 적용 명령이 명확히 실패해야 한다.
5. 별도 슬롯에서 두 새 게임과 구 저장 복사본을 검사한다. 실패하면 저장 원본을 건드리지 않고 씬 연결을 복구한다.

현재는 1의 공간/모델/로드 증거가 없다. 따라서 2~5의 실 적용을 완료로 표기하지 않는다. 독립 코드·데이터 제작은 계속하되 반쯤 적용된 씬을 플레이 가능한 새 본편으로 인계하지 않는다.

## 7. R03 종료 경계

화린 영입 뒤 교체 안내는 선택이다. 릴리와 피해 마을로 향할 이유까지만 안내하고, 존재하지 않는 S03 방문 목표를 활성 퀘스트로 만들지 않는다. 준비가 끝날 때까지 마을에서 쉴 수 있는 자유 조작 상태로 닫는다. 플레이어 문안에 개발 단계 번호를 표시하지 않는다. 릴리의 실물 화자/대역 연결은 아직 필요한 작업이며 이 문서만으로 등장했다고 보지 않는다.

검증 현황과 실제 구현 파일은 [R03 결과 기록](LAKE_OF_LIFE_RED_TREE_R03_IMPLEMENTATION.md)을 따른다. 설계 작성은 R02 전체 통과나 R03 플레이 가능 판정이 아니다. QuestDatabase에는 새 비활성 3개 참조를 추가했으며, 구 러너/NPC/Anchor/퀘스트·생성기 경로는 아직 변경하지 않았다. 새 저장 API는 구현됐지만 자동 이관·실제 슬롯 실행은 하지 않았다.
