# 붉은 나무 R07 — 공동 대응 구현 기록

2026-10-18. S10과 중앙 신사 접근을 본편 `LakeOfLifeScenario`에 연결했다. **본편 구현과 선택 검사를 수행했으며 통합 검증은 대기**다. R09/S13 반환·재건·재방문은 구현하지 않는다. 실제 플레이 R06 완료 저장은 아직 없다.

## 실행선과 인계

- 씬 루트: `RedTreeR07R08`. `FlowGraphRunner`의 `Resume`와 플래그 재로드가 현재 사실을 소비한다.
- 데이터: `Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/RedTree/R07R08/`.
- `quest_main_red_tree_response_next`의 기존 ID/GUID를 보존했다. `lake.red_tree.response_work_started`로 기존 대기를 마감하고 `quest_main_red_tree_s10`을 시작한다. 새 목표도 ‘남은 사람들의 몫’이다.
- QuestDatabase에 S10/S11/S12와 `quest_main_red_tree_return_next`를 각각 한 번 등록했다. 신규 시작은 추적 목표도 연결한다. 이미 활성/완료한 퀘스트는 다시 시작하지 않는다.
- 목표 공개 전 수행한 사실은 보존하며, 재개 때 관련 목표에만 StoryEvent를 재통지한다. 표시 순서는 진행 판정을 지우지 않는다.

## S10의 세 작업

모든 아래 키는 `lake.red_tree.` 접두사를 쓴다. 물자 확보와 배급, 접근 준비와 연결 중단은 서로 다른 사실이다.

| 작업 | 실제 소비처와 완료 조건 |
|---|---|
| 돌아올 길 | 화린과 대화해 `response.front_requested`를 만든 뒤 `R07_front_clear`에서 화린의 실제 보행 도착으로 `response.front_clear`. 입력 `(1020,63,442)`, 도착 `(1023,지면,443)` |
| 주민 의사와 대피 | 릴리·미아 대화 `response.wishes`와 앞길 확인 후 `R07_evacuate`. 기존 `NPC_Mia`가 `(1054,60.06,411)`에 도착해야 `response.mia_arrived`. 입력 `(1058,59.74,410)`과 약 4.1m 분리 |
| 기존 인계 확인 | 유라의 `response.relay`는 R05의 `other_rescued`, `route_connected`와 미아 도착을 요구. 이미 인계한 주민을 생성하거나 다시 구조하지 않음 |
| 외곽 확인 | `response.outer_checked`. 최초 라온이면 아린, 최초 아린이면 라온이 보고. 현재 조작 캐릭터와 최초 주인공을 혼동하지 않음 |
| 남는 주민 | `response.shelter_checked`와 실제 식량 인계 `response.remaining_supplied`를 함께 요구. 남겠다는 선택을 바꾸도록 강요하지 않음 |
| 공급 조사 | 리안·세라의 `response.stock_checked`. R06의 `offering_handed_over`, `stored_food_checked`, `external_water_handed_over`를 먼저 확인. R04의 먼저 온 돌봄 물과 이번 물통을 대사에서 구별 |
| 배급 | 돌봄 `response.care_supplied`, 잔류 주민 `response.remaining_supplied`, 피해 지역 `response.affected_supplied`는 각각 별도의 인계 입력과 소품 전환. 완료 후 조이·연화가 `response.share_checked`로 오늘 남길 몫 확인 |
| 접근 준비 | 묘묘의 서쪽 운송 목록 대조, 설아의 남쪽 흡수 경로, 연화·레이네의 신사 연결과 안전 확인. 묘령의 `response.confrontation_ready`는 별도 대화 |

대피는 대화 종료/처치로 완료되지 않는다. 기존 `NpcLifeInteractable`의 실제 이동, 취소, 제한시간, 플레이어 생존, 저장 재로드 계약을 사용한다. 미도착 취소 후 해당 주민부터 재개한다. 전투 파티는 기존 최대 4명이다. 현장의 동료는 비전투 NPC 대역이며 전투 AI로 소환하지 않는다. `StoryCharacterPresence.requiredFlag`로 R06 이후에만 표시하고 현재 조작 인물의 대역은 숨긴다. 기존 R05/R06 대역은 기존 동의 후 숨김 계약을 유지한다.

## 물자 소유와 중복 방지

| 확보 사실/실물 | S10 사용처 | 완료 사실 |
|---|---|---|
| R06 `ExternalWaterAtSupply` / 외부 식수 한 통 | 헤이즐 돌봄 자리 `(1005,지면,412)` | `response.care_supplied` |
| R06 `StoredFoodChecked` / 조사한 보관 식량 | 잔류 주민 거처 `(951,지면,464)` | `response.remaining_supplied` |
| R06 `OfferingAtSupply` / 철회한 봉헌 몫 | 유라의 피해 지역 인계점 `(1045,지면,483)` | `response.affected_supplied` |

원래 소품을 `R07_*_owned` 부모 아래 두고 인계 후 부모를 끈다. R06 복원기가 자식을 켜더라도 원래 공급 지점에 다시 나타나지 않는다. 인계 위치의 대응 소품은 같은 물자의 결과 표현이며 추가 재고/아이템 보상이 아니다. 한 쌍 중 한 위치만 활성화한다. 새 물통·추가 확보량·무한 공급을 만들지 않았고 첫 물통으로 장기 생존을 보장하지 않는다. 적 드롭과 퀘스트 보상은 이 물자의 소유자가 아니다. 조이의 줄인 몫과 외부로 남긴 짐을 배치·대사로 연결하며 감사·용서·화해를 요구하지 않는다.

## 표식 집합과 공간

| 안정 ID | 준비 위치 X/Z | S10 준비 | S11 중단 |
|---|---|---|---|
| `trial_creek` | 기존 R06 개울 | 재작업 없음 | 기존 `marker.trial_creek.disconnected` 소비만 함 |
| `west_relay` | 1047 / 807, 서쪽 길가 | `marker.west_relay.prepared` | `marker.west_relay.disconnected` |
| `south_channel` | 1059 / 790, 남쪽 물길 | `marker.south_channel.prepared` | `marker.south_channel.disconnected` |
| `shrine_link` | 1090 / 818, 신사 연결부 | `marker.shrine_link.prepared` | `marker.shrine_link.disconnected` |

남은 세 곳은 운송 인계로·흡수 물길·신사 연결의 서로 다른 대조 지점이다. 세 곳마다 적이나 타격 횟수를 복제하지 않는다. 실제 연결 해제는 기존 분리 소품과 상호작용을 사용하며 부분 완료가 저장된다. 시험 표식을 포함한 집합 확인은 `markers.remaining_disconnected`, 플레이어의 전체 확인은 `markers.confirmed`다. 기존 ID는 변경하지 않는다.

최종 준비 `response.ready`는 R06 완료, T3 `truth.expansion`, 대피·배급·접근 준비, 공동 대응 시작과 지정 12명을 함께 요구한다. 지정 명단은 라온/아린/화린/릴리/설아/리안/유라/묘령/레이네/묘묘/연화/세라다. 단순 roster.Count 문이 아니다. 선행 신사 접근은 부족한 주민·인계·표식 준비 작업을 구체적으로 안내하고 복귀를 막지 않는다.

중앙 `Heart_AncientTree`와 `Heart_BaseShrine`를 재사용했다. 보스 시작 `(1072,83.09,817)`, 안전 재시작 `(1072,83.05,806)`, 30×28m 전투 경계로 나무 몸통 남쪽을 사용한다. 기존 NavMesh를 그대로 사용했다. 첫 서쪽 배치의 NavMesh 실패를 발견해 위 좌표로 옮겼고, 변경 구간 12개 지점에서 마을로부터 PathComplete를 확인했다. NavMesh 재베이크·지형 변경은 없다. 전면 화면에서 나무/신사/회피 공간을 확인했지만 모든 카메라 각도·물가 퇴로·캐릭터별 KCC 체감은 통합 검증 대기다.

## 검사와 실제 저장

- 선택 EditMode 최종 11/11, Play Mode 최종 7/7. 변경한 그래프·대화·본편 등록·실행 트리거와 사실 경계 검사다. 전 구간 자동 완주가 아니다.
- 격리 Editor: 미아 실제 도착(목적지 오차 약 0.01m), 대표 식수 인계, 준비 전 차단, 서쪽 표식만 중단한 저장/로드, 공급 위치 중복 없음 확인.
- 새로 발견한 실패: 서쪽 표식 NavMesh 이탈, 전용 Set의 실행 트리거 누락, 후반 재로드의 FlowGraph 256노드 실행 한도 초과. 각각 위치·상속 제외 목록·목표별 재통지를 수정하고 관련 선택 검사/짧은 실행으로 재확인했다.
- 기존 `R05_rescue_other` 입력 시간 초과가 재현됐다. 이전 구간 자동화 복구를 확대하지 않았다. 기존 Sera 얼굴 바인딩 실패는 재실행하지 않았고 원본 묘묘 의상을 수정하지 않았다. R06 대역 Socks 제외도 원본 의상 수정으로 기록하지 않는다.
- 로드/모델 전환 중 native Quaternion NaN 오류를 관측했다. 새 콘텐츠 원인인지 기존 모델/Cloth 원인인지 분리하지 못했으므로 신규 관측·원인 미확정으로 남긴다. 정상 화면/상태 검사의 통과로 이 오류를 상쇄하지 않는다.

정상 시작 원본은 `output/RedTreeR06Integration/Runs/Raon-20261018_184337/Saves/save_slot_16.sav`. 새 `output/RedTreeR07R08Integration/RealResume/Saves/save_slot_0.sav`로 복사한 뒤 별도 실행 폴더에서 재개했다. `RealResume/Runs/Raon-20261018_195458/Saves/save_slot_17.sav`는 정상 입력으로 기존 인계 지점에 다시 도착한 저장이다. 최초 라온·활성 설아, 8명 보유, 아린 발견까지이며 `other_rescued`, R05/R06 완료는 거짓이다. 기존 사용자 슬롯과 원본은 보존했다.

**실제 R06 완료/R07 진입/보스 직전/승리 저장은 없다.** `Isolated/Saves`는 선행 플래그·영입·위치 등을 준비한 검사 전용이다. 실제 완료 저장으로 사용하지 않는다. 자세한 전투/관찰 검사는 [R08 기록](LAKE_OF_LIFE_RED_TREE_R08_IMPLEMENTATION.md), 실행 절차는 [테스트 명세](../guide/LAKE_OF_LIFE_RED_TREE_TEST_SPEC.md)의 R07/R08 절을 따른다.

## 인계

최종 Editor는 Boot, Play 꺼짐, 씬 dirty 없음, 컴파일 완료/컴파일 실패 없음이다. 런타임 로그 보존 후 Console 정리와 최종 컴파일 요청 시점 오류 0·경고 1(기존 퀘스트 도구 메뉴 정책)이다. 앞선 런타임 native assert 해결을 뜻하지 않는다. 실행 결과를 `output/RedTreeR07R08Integration/execution-summary.json`에 분리해 기록했다.

마지막 화자 점검에서 신규 작업 대역의 Actor ID와 기존 화자표가 불일치한 것을 수정했다. 작업 대역은 기존 인물 Actor ID를 사용하고 저장용 SceneEntity ID는 독립적으로 유지한다. 기존 장면 대역은 동의 후 숨겨지므로 정상 진행에서 같은 인물의 활성 몸을 겹치지 않는다. 화린·릴리·설아의 화자표 character 연결도 보완했다. 실제 로드에서 활성 릴리는 Player 몸, 나머지 10명 화자는 R07 작업 몸으로 해석됨을 확인했다. 격리 씨앗의 누락된 R06 동의 사실 3개는 검사 중에만 준비했으며 저장하지 않았다. `speaker-binding-final.txt` 참조.

화린의 앞길 작업도 실제 보행 도착(오차 약 0.04m) 후 완료됨을 확인했다. 최초 라온/아린 상태에서 반대편 주인공만 표시되고 현재 조작 인물의 대역은 숨겨지는 차이를 격리 확인했다. `isolated-front-and-protagonist.txt` 참조. 실제 아린 정상 진행을 완료했다는 의미는 아니다.

native Quaternion NaN 중 일부는 `MagicaCloth2.ClothManager.CompleteMasterJob` 스택에서 발생했다. 영향을 주는 원본 모델과 이번 대역의 책임은 아직 분리하지 못했다. 기존 실패라고 단정하지 않고 통합 검증 대기로 유지한다.

증거: `output/RedTreeR07R08Integration/`. 임시 저작/입력 검사 스크립트는 AssetDatabase에서 meta와 함께 삭제했다. 기존 씬 직렬화 블록 삭제 0건이며 기존 블록 변경은 소품 재부모화·부모 자식 목록·새 optional presence 필드와 씬 루트 등록에 한정한다. 공용 골렘·엔트 에셋과 기존 사용자 변경을 보존하고 커밋하지 않았다.

긴 정상 연속 진행, 실물 패드, 전투/연출 가독성, 전체 파티 운용, 부활 UI를 포함한 전멸 재시작은 **통합 검증 대기**다. 전체 회귀·전체 참조 검사·Windows Player는 이번에 실행하지 않았으며 전체 콘텐츠 통합/릴리스 단계에서 수행한다.

### R09 연결 후 인계 갱신 (2026-10-18)

이 문서의 위 실행 기록은 R07/R08 검사 당시 이력이다. 후속 [R09 공통 결말 구현](LAKE_OF_LIFE_RED_TREE_R09_IMPLEMENTATION.md)은 별도 Flow/Quest로 본편에 연결했으며 통합 검증은 대기다. 기존 `return_next` ID/GUID·`return_work_started` 이벤트는 보존하고 마커만 R09 시작점으로 연결했다. R08 승리와 관찰의 생산자는 바뀌지 않는다. R07 비전투 작업 대역은 `absorption.observed` 이후 숨겨지고 R09 대역이 기존 Actor ID·별도 SceneEntity ID로 이어받는다. 실제 현재 디스크에는 격리 슬롯5까지 있고 위 기록의 슬롯6 파일은 없다. R09는 슬롯5 원본을 별도 폴더의 빈 슬롯50으로 복사했으나 Boot 초기화 지연으로 실제 로드·R09 행동 검사는 진행하지 못했다. 원본 저장과 R07/R08 성공/실패 이력을 보존한다.
