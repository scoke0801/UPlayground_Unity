# 생명의 호수 생활 확장 A — 현장 명세와 병렬 작업 계약

> 2026-10-22 / 상태: **A 조사 완료, B/C/D의 독립 작업 착수 계약 확정. 본편 통합·현장 플레이 검증 대기.**
> 기준: [작업 지시서 A](../LAKE_OF_LIFE_LIVING_WORLD_TASKS.md), [생활 확장 기획](../LAKE_OF_LIFE_LIVING_WORLD_PLAN.md), [확정 서사](../LAKE_OF_LIFE_RED_TREE_STORY_PLAN.md), [본편 저장 경계 3절](../LAKE_OF_LIFE_RED_TREE_IMPLEMENTATION_SPEC.md).
> 이 문서는 내부 제작 명세다. 새 지명·대사·가족 관계·루시아 처우를 확정하지 않는다. 새 씬·기능·자산을 구현했다는 뜻이 아니다.

## 1. 먼저 시작할 수 있는 일

| 담당 | 지금 시작 가능 | 연결·완료 전에 해결할 사항 |
|---|---|---|
| B | 아래 고정 ID로 작은 `DroughtVillage` 씬 제작, 왕복 컴포넌트와 실패 복구 보완, 구 저장 복사 이관 구현·검사, 우물/식수 이관 명세 | I의 씬·지도 등록과 본편 출구 연결, 실제 안전 도착 검증, 구 저장 위치 분류표, R04 러너의 지역별 실행 제한 |
| C | 주민 6명의 일과·작업점·예약·중단 구조, 기존 이동/모션 재사용, 전용 프리팹·독립 테스트 | 대화 홀드와 일과 이동의 충돌 해소, I가 제공할 공용 Data/환경 계약, 실제 운반·조리 모션 적합성, B/I의 보행 구간 |
| D | 물/흙 셰이더와 FX 변형, 공통 바람·건조 날씨의 독립 구현, 조명 합성, 실내 억제 | I의 환경 계약 등록, 지역 시간 복원 연동, 본편 지붕·바닥·가독성·성능 측정 |
| E | 아래 후보의 크기·피벗·재질 조사 | B/C/D가 기존 자산으로 해결 못 한 구체 명세를 낸 뒤 제작. 신규 주민 몸체·무관한 미술 제작 금지 |
| I | 공용 계약 최소분 게시, B의 등록 요청 준비, 현재 씬 변경과 이관 대상 대조 | B/C/D 인계 후 단독으로 본편 연결. 현재 A에서 통합을 실행하지 않음 |

**B/C/D 착수 가능은 두 맵 왕복이나 본편 연결 가능 판정이 아니다.** 새 API가 필요한 부분은 6절에 `신규 계약안`으로 표시했다. 제공 전에는 독립 입력·테스트를 사용하며 없는 타입을 호출하는 코드로 본편 컴파일을 깨뜨리지 않는다.

## 2. 조사 기준과 사용자 변경 보호

조사 방식은 디스크의 C#·YAML·JSON·meta 읽기와 git 상태 확인이다. Unity Editor를 열거나 씬 저장·임포트·Play Mode·빌드·NavMesh 베이크를 실행하지 않았다. 좌표의 **직렬화 실측**과 플레이로 검증한 안전 좌표는 다르다.

| 항목 | 착수 기준 |
|---|---|
| HEAD 대비 사용자 수정 | `Assets/01.Scenes/Scenario/LakeOfLifeScenario.unity` |
| 기존 미추적 파일 | `Assets/docs/design/LAKE_OF_LIFE_LIVING_WORLD_PLAN.md`와 `.meta`, `LAKE_OF_LIFE_LIVING_WORLD_TASKS.md`와 `.meta` |
| 씬 크기 | 124,662,873 bytes |
| 씬 SHA-256 | `23F089199D37FB8FCD136574D0BAEE2A2A58CAD8660D8F0B222E4799B2D6FFD5` |
| 씬 GUID | `1d16ed83654f4f14e8628ca754f42067` |
| 현재 SceneContext | `SceneType=GamePlay`, `MapID=LakeOfLifeScenario` |
| 시작 맵 | `Assets/10.Datas/Map/MapConfigDatabase.asset`의 `_defaultStartMapId=LakeOfLifeScenario` |

현재 씬 자체를 기준으로 삼는다. `.layout.json`·`.world.json`·`.quality.json`이나 과거 저작 스크립트로 씬을 재생성하지 않는다. 사용자 씬 diff는 약 318만 줄 규모이므로 단순한 이전 버전 복사로 보존할 수 없다. I 착수 때 다시 해시·dirty 상태를 확인하고, 편집 중인 Editor의 미저장 상태가 있다면 그것까지 보존한다. 본 문서는 **디스크 상태만** 확인했다.

특히 [R09 과거 기록](../LAKE_OF_LIFE_RED_TREE_R09_IMPLEMENTATION.md)의 포장 `(1032,410)`, 적재 `(1090,412)`, 출발 `(1090,416)`과 현재 씬의 `(1003,436)`, `(1002,440)`, `(1002,444)`는 다르다. 현행 배치를 이전 좌표로 되돌리는 작업은 하지 않는다. 원인·작업자를 추정하지 않는다.

기획·콘텐츠·테스트 지침을 적용했다. 반복 일과와 날씨가 영입·주민 의사·물자 소유·흡수 중단을 생산하지 않으며, 일반 왕복·사망·로드로 본편 사실을 리셋하지 않는다. 과거 사이클 가이드의 리셋 예시를 이 본편에 적용하지 않는다.

## 3. R04~R09 참조와 저장 소유

이하 `RT`는 `Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/RedTree/`의 경로 약칭이다. `F`는 플래그 접두사 `lake.red_tree.`다. **지역 소유는 배치/입력/연출의 소속**이며 영속 사실은 계속 전역 저장 소유다. 기존 콘텐츠 에셋은 RT에 유지하고 새 씬에서도 참조한다. 폴더 이동이나 GUID 교체를 요구하지 않는다.

### 3.1 사건별 지역 경계

| 단계 | 피해 마을 소유로 옮길 것 | 호수에 남길 것 | 공통 진행·저장과 연결 조건 |
|---|---|---|---|
| R04 / S03 | `R04_well/home/field/survivor/stay/leave/water/departure/lili`, 마른 우물·거처·씨앗·짐, 두 주민, 출발 준비의 before/after 앵커 | R03 생활·릴리 최초 인계, `R04_report/assignment/local_supply/outbound_supply/sera/records_next`, 조이·루시아·세라의 보고/지원 자리 | Q03 `s03`; `well_examined/home_examined/field_examined/survivors_found`, `resident_stays/leaves`, `water_handed_over`, `departure_prepared`, `lili_consent`. 준비는 실제 출발·정착 완료가 아님 |
| R04 / S04~05 | `R04_pool/encounter/roots/truth/seola`, 물/뿌리·엔트 출현 위치, `CAM_pool/CAM_roots`의 관찰 구도 | 보고·공급 대안 확인은 호수 | Q04/05 `s04/s05`; `evidence.root_draw/lake_path`, `creek_started/cleared`, `truth.cost`, `seola_consent`, `report_completed/support_assigned`. 엔트는 현재 Flow의 `RedTree_CreekEnt` 실제 생성·패배 경로만 사용 |
| R05 / S06~07 | **이번 분리에서는 없음.** 폐쇄 길과 구조 인계는 호수 외곽의 한 연속 구간으로 유지 | `RedTreeR05` 기록대 3곳·사본 인계·차단 수레·우회로·쉼터·두 주민 보행과 5명 합류 전체 | Q06/07 `s06/s07`; `truth.concealment`, `passage_open`, `route_*`, `yura_rescued/other_rescued`, 각 consent, `r05_complete`. 이유: 현 좌표 1034~1055/436~487에서 보행 시작·도착이 연결되어 있음. 새 맵 경계로 중간을 자르면 기존 구조를 바꿈 |
| R06 / S08 | `R06_bundle/order/compare/resident/rescue/before/disconnect/after/truth/responsibility/myomyo`, 설치 협조 주민·시험 표식·수면·그들의 앵커 | R05 쉼터 옆 `R06_list/cargo`, 봉헌·식량·외부 식수·세라 인계 및 `R06_next` | Q08/09 `s08/s09`; 목록/포장/설치 묶음의 열람은 지역을 넘어 누적. `trial_resident_safe`, `marker.trial_creek.disconnected`, E08 관찰, `truth.expansion`, 책임/합류를 각각 보존. `trial_creek` 재발급 금지 |
| R07 / S10 | `R07_shelter/remaining`, 잔류 주민 거처의 식량 결과 | 시작·화린 앞길·미아 대피·R05 인계 확인·외곽 확인·돌봄·재고/몫 확인. `R07_affected`는 현재 호수 외곽 인계소에 유지 | `response.remaining_supplied`는 새 맵 입력, `response.affected_supplied`는 기존 호수 외곽 인계 입력. 같은 의미로 합치지 않음. `response.ready`는 두 맵의 사실 집계 |
| R07~08 / S11~12 | `trial_creek` 완료를 읽을 뿐 추가 중단 작업 없음 | `west_relay`, `south_channel`, `shrine_link`, 중앙 신사/붉은 나무, `StoryBossEncounter`, 루시아 전투·흡수 중단·중앙 관찰 | Q10/11/12 `s10/s11/s12`; 개별 표식 키·`lucia.victory/attacks_cleared`·`absorption.stopped/observed` 보존. 날씨가 이 사실을 쓰지 않음 |
| R09 / S13 | `R09_wishes/receipt/water/channel/route/observe/revisit_field`, 작은 외부 물길·수로 목재·받은 짐·피해 관찰, 묘묘/릴리/설아/화린/유라의 해당 작업 대역과 도착 앵커 | `R09_begin/stock/pack/cart/load/transport/depart/meal/revisit/cost/hint/guide`, 호수 측 출발·식사 대역과 소품, `R09_AffectedPendingReceipt` | Q13 `s13`; `return.*` 전부 전역. 주민 의사 확인→별도 인계, 중앙 흡수 관찰→별도 외부 물길 관찰, 노동 도착→별도 완료. `return.common_ending` 후 자유 재방문 유지 |

R05를 호수에 남기는 것은 ‘모든 피해 관련 사건을 새 맵으로 이동’으로 해석해 서사 동선을 바꾸는 것을 막기 위한 결정이다. R07 `affected` 역시 피해 지역 **몫의 인계소**와 최종 우물 전달을 구별한다. R09 노동 인계길은 새 피해 마을 안에서 완료한다. R05 구조 NPC를 두 맵 사이에서 걷게 하지 않는다.

### 3.2 현재 실제 참조 중 분리 시 끊기는 지점

| 실제 직렬화 연결 | 위험 / I의 처리 계약 |
|---|---|
| `R07_remaining` Presentation: 호수의 `R07_remaining_owned` → 피해 거처 `R07_remaining_delivered` | 입력·완료 결과는 피해 마을. 호수 원본 부모는 같은 `response.remaining_supplied`를 읽어 숨김. 서로의 GameObject를 직접 참조하지 않음 |
| `R07_affected`의 `_afterVisuals={fileID:1385192498}` → R09 루트의 `R07_affected_delivered` | R07 루트만 이동/복제하면 누락. 이 결과는 현재 `(1045,60.576,483)`의 **호수 외곽**에 남김 |
| `R09_receipt`의 `_beforeVisuals={fileID:170145474}` → 호수의 `R09_AffectedPendingReceipt`; `_afterVisuals={fileID:1555843222}` → 우물 받은 짐 | 호수 pending 부모는 `return.handed_over`를 소비하는 표시 전용으로 남김. 새 맵 receipt에는 지역의 afterVisuals만 연결해 기존 `NpcLifePresentation`을 재사용 가능. 전역 사실 생산자는 receipt 한 곳 |
| R06 offering/water/food의 afterVisuals가 R07 루트 아래 `R06_OfferingAtSupply/ExternalWaterAtSupply/StoredFoodChecked`를 참조 | 전부 호수에 유지. R06 루트 통째 이동 금지. R07 소유 부모의 상호 배타 표시도 유지 |
| R09 `channel` actor `1120061492`, `route` actor `1881134161`와 before/after Transform | 작업점·대역·모든 앵커를 같은 새 씬으로 옮김. 숨겨진 대역 대신 현재 조작 몸으로 도착하는 기존 분기 보존 |
| R07 미아/루시아 이동과 R09 안내인 재배치가 `NPC` 루트의 기존 몸을 참조 | 호수 유지. C가 별도 주민 몸을 만들어 대체하지 않음 |
| Flow의 `volumeId`, `WaitPlayerInVolumeNode`, 각 TriggerVolume | 현재 노드는 **자기 Runner 하위**에서 ID를 찾음. SO에서 씬을 넘는 Transform을 해석하는 API가 아님. 이관된 볼륨은 해당 씬의 같은 그래프 Runner 아래 배치 |
| R04 `SpawnStoryActorNode.position=(950,69.00279,504)`, 관찰 카메라 샘플 | Transform만 옮겨도 SO 좌표는 그대로 남음. 아래 초안은 기존 좌표계를 유지하지만, 지형/배치가 달라지면 I가 spawn·카메라·마커를 함께 변경 |
| 퀘스트 목적지·화자 바인딩·`StoryCharacterPresence` | 새 mapId와 현장 위치를 함께 반영. Actor ID는 유지하고 R07/R09 대역은 별도 SceneEntityId 유지. R07 대역의 `absorption.observed` 이후 숨김 조건 보존 |

표시를 두 씬으로 나눌 때 기존 `NpcLifePresentation`의 before-only/after-only 표시 구성을 우선 쓴다. `CanPresentAction()`은 actor가 없더라도 유효한 소품 목록이 필요하므로, 실제 행동 입력 쪽은 반드시 지역의 실물 결과와 연결한다. 빈 표시기로 완료만 쓰는 인계를 만들지 않는다. 타지역 부모/자식을 활성화하려는 직접 참조를 남기지 않는다.

### 3.3 Flow의 ID와 실행 계약

아래 에셋 경로는 RT 기준이며 GUID·graphId·기존 nodeId를 보존한다.

| 에셋 | graphId | GUID | 현 씬 시작 entry |
|---|---|---|---|
| `R04/FLOW_RedTreeVillageWaterReport.asset` | `flow_red_tree_village_water_report` | `efb5d12b5b1ef4a46aa595d0c893e9af` | `MapReady` |
| `R05/FLOW_RedTreeRecordsPassage.asset` | `flow_red_tree_records_passage` | `d1fa92084b3cb9041b575611b67ab1a9` | `MapReady` |
| `R06/FLOW_RedTreeMarksSupply.asset` | `flow_red_tree_marks_supply` | `385a116121b09e14c9fb2c43a068efae` | `MapReady` |
| `R07R08/FLOW_RedTreeResponseFinale.asset` | `lake.red_tree.response_finale` | `6abeb5250486e284f9cba48f0b1372d1` | `Resume` |
| `R09/FLOW_RedTreeReturn.asset` | `lake.red_tree.return_rebuild` | `3668aa3e0dabd044c8cbd728c2d48341` | `Resume` |

R04/R06까지 모두 `Resume`이라고 쓰지 않는다. 현재 호수 `RegionInfo.asset`의 `flowGraphs`는 빈 배열이며 씬의 수동 Runner가 실행한다. 새 맵도 우선 **수동 Runner + 지역 하위 볼륨**을 사용한다. 같은 그래프를 RegionInfo 목록과 씬에 동시에 등록하지 않는다.

I는 기존 그래프를 공유하는 방향을 우선 검토하되, 지역 효과를 내는 모든 경로(최초 진입·플래그 변경·FlagsReloaded·미완료 조우 재개)에 지역의 볼륨/조건을 먼저 둔다. 기존 `WaitPlayerInVolumeNode`는 지역 볼륨이 없으면 `Failed`로 끝나므로 지역 접근이 필요한 경로에 재사용할 수 있다. **누락 볼륨 실패를 완료로 연결하면 안 된다.** 특히 `creek_started=true, creek_cleared=false` 로드가 호수에서 엔트를 생성하지 않는지 별도 검사한다. 공통 집계는 플래그·퀘스트 상태만 읽고 기록하며 타지역 대화/생성/카메라를 실행하지 않는다.

이 구조는 아직 구현되지 않았다. 단순히 전체 Runner를 복제해 두 씬에서 자동 시작하는 것은 승인된 이관안이 아니다. 지역 제한이 기존 노드만으로 충분하지 않으면 I 소유의 Flow 조건/지역 바인딩을 최소 확장하고 그 API와 테스트를 먼저 게시한다. `CurrentMapCondition` 같은 타입은 현재 확인된 기존 기능으로 간주하지 않는다.

`FlowProgressSaveData`는 `graphId:nodeId` 기준 firedKeys와 진입 횟수를 저장하며 코루틴 위치를 저장하지 않는다. I가 그래프를 분리해야 한다면 기존 ID를 유지할 노드와 새 보조 그래프를 명시하고 B에게 진행 키 이관 목록을 먼저 전달한다. nodeId 일괄 재생성·Flow 저장 전체 초기화 금지.

## 4. 현장 좌표와 새 피해 마을 배치

### 4.1 현재 파일에서 읽은 좌표

단위 m, `(X,Y,Z)`, 소수 셋째 자리 반올림. R04~R09 루트와 R09 `AfterObservation`은 원점/회전 없음/배율 1이라 아래 입력점의 local=world다. `NPC` 부모는 `(0,0,-4.0599976)`이므로 그 아래 인물은 이 오프셋을 반영했다. 바닥 높이·카메라·KCC 안전성의 현장 검증은 미수행이다.

| 구역/대상 | 현 씬 직렬화 실측 | 새 씬 소속 / 기존 사건 |
|---|---|---|
| 마른 우물 `R04_well` (GO 807937263) | `(960,65.849,460)` | 피해 / 우물 조사 |
| 빈집 `R04_home` | `(950,68.718,457)` | 피해 / 거처 조사 |
| 시든 밭 `R04_field` | `(947,71.967,478)` | 피해 / R04 및 R09 피해 관찰 |
| 식수 입력 `R04_water` | `(953,71.213,480)` | 피해 / 우물 조사점과 별도 위치 |
| 짐 준비 `R04_departure` | `(953,70.854,483)` | 피해 / 출발 전 짐. 도착 `(952.5,70.824,484)` |
| 물 접점 `R04_pool` | `(957,68.006,488)` | 피해 / E02 |
| 엔트 접근 `R04_encounter` | `(950,69.003,504)` | 피해 / 생활길과 분리할 조우 |
| 뿌리 `R04_roots`, 진실 `R04_truth` | `(948,67.814,524)`, `(950,67.597,530)` | 피해 / E03·T1 |
| 설치 묶음/지시 `R06_bundle/order` | `(944,70.182,490)`, `(944,69.666,494)` | 피해 / 기록을 읽는 독립 조사점 |
| 시험 표식 `R06_disconnect` | `(954,68.563,502)` | 피해 / `trial_creek` |
| R06 주민 안전 도착 | `(946,70.799,485)` | 피해 / 표식 중단 전 실제 도착 |
| 잔류 주민 식량 `R07_remaining` | `(953,68.359,463)` | 피해 / 돌봄 식수·봉헌 몫과 별도 |
| R09 의사/최종 인계 | `(954,67.875,461)`, `(955,67.587,462)` | 피해 / 우물 옆 반입 |
| R09 물길/수로 입력/도착 | `(958,66.527,466)`, `(959,66.126,467)`, `(957,66.856,469)` | 피해 / 입력점과 작업자의 도착점 분리 |
| R09 노동길 입력/도착 | `(970,62.305,476)`, `(972,62.244,479)` | 피해 / 화린 실제 도착 |
| R05 차단/안전 인계 | `(1037,61.852,464)`, `(1042,60.704,480)` | 호수 / 새 맵 출구와 혼동 금지 |
| R07 피해 몫 입력/보관 결과 | `(1043,60.664,481)`, `(1045,60.576,483)` | 호수 / R09 우물 인계 전 소유 위치 |
| R09 포장/수레 지원/적재/출발 | `(1003,62.455,436)` / `(1001,62.643,438)` / `(1002,62.504,440)` / `(1002,62.401,444)` | **현재 사용자 씬 기준 호수 유지** |
| 조이 / 헤이즐 | `(1033,62.115,406)` / `(1005.225,62.471,407.465)` | 호수 / NPC 부모 오프셋 적용 |
| 모건 / 연목 | `(1088,56.128,410)` / `(987,62.992,431)` | 호수 / 모건과 현재 R09 적재점은 떨어져 있음. C/I가 시야·작업 동선을 확인하되 임의로 모건을 재배치하지 않음 |

물/흙/밭의 시각 메시 면적, 집의 실제 실내 경계, 출구 후보의 경사, 플레이어 캡슐과 카메라 통과 폭은 아직 실측하지 않았다. 위 수치는 오브젝트 원점이지 콜라이더의 유효 폭이 아니다.

### 4.2 B의 새 씬 초안 — 제안 좌표

첫 피해 마을은 **기존 피해 구간의 X/Z 좌표계를 유지**한다. 원점으로 일괄 평행이동하면 Flow spawn·카메라 샘플·구 저장까지 모두 바뀌므로, 첫 왕복 검증에서 이를 피한다. 새 씬의 지면은 별도 제작·샘플링하며 아래 `ground`는 B가 결정할 실제 지면 높이다. 기존 높이를 그대로 썼다고 안전 판정하지 않는다.

| 요소 | 제안 위치/범위 | 이유·조건 |
|---|---|---|
| 호수 측 길 표식 | `(984,ground,449)` 부근, 호수 귀환 도착 `(991,ground,449)` 부근 | 현 R09 운송권 `(996~1003,436~448)` 서쪽의 굽은 길 후보. 기존 연목·출발 입력을 침범하지 않는지 I 확인. 미배치 |
| 피해 마을 도착 | `(978,ground,451)`, 마을 중심을 보도록 회전 | 기존 우물 `(960,460)`과 귀로를 함께 읽게 하는 동쪽 접근. 미배치 |
| 피해 마을 귀환 표식 | `(985,ground,451)` | 도착점과 7m 시작 간격. **트리거 반경+KCC 반경 밖**인지 B 실측 후 확정 |
| 우물/집/그늘 | 우물 `(960,460)`, 집 `(950,457)`, 공동 그늘 `(952,473)` 부근 | 우물 조사→식수 인계 `(953,480)`가 짧게 이어짐. 그늘은 새 일상 배치 제안이며 새 가족·피해 사실 없음 |
| 밭/도구 돌봄 | `(947,478)` 주변, 귀로는 동쪽 `(970,476)` 쪽 | R09 노동길과 연결하되 입력·NPC 도착·통행을 분리 |
| 마른 개울/표식 | `(957,488)` → `(950,504)` → `(948,524)` | 기존 E02→조우→E03 순서와 진실 공개를 유지. 집과 생활 NPC까지 적이 추격하지 않도록 확인 |
| 생활 순환 | 도착길→우물→그늘/집→밭→동쪽 귀로→도착길 | 개울 조우는 순환 밖 갈래. 왕복할 때 같은 빈 길만 반복하지 않도록 함 |

주 통로 유효 폭 3.5~4m, 샛길 2~2.5m, 상호작용 전면 1.5m 이상은 기반 기획의 **초기 설계값**이다. NPC 운반 자세·KCC 교행·TPS 카메라로 확정한다. 통행 폭을 재는 기준은 메시 폭이 아니라 콜라이더·소품을 제외한 실제 바닥이다.

첫 씬에는 안전 바닥·귀환 출구·우물·식수 결과만 필수다. 나머지 구역은 자리만 예약해도 된다. 다만 원본 피해 지역의 실행선을 제거하는 시점에는 모든 해당 진행이 새 맵에서 이어져야 한다. 미완성 이관으로 기존 저장을 출시하지 않는다.

## 5. ID/GUID와 구 저장 이관

### 5.1 신규 예약 ID — 이번에는 등록하지 않음

현 MapConfigDatabase·BuildSettings·시나리오 RegionInfo에 `DroughtVillage` 등록은 없다. 다음 문자열은 **A의 후속 구현용 예약값**이다. 신규 GUID는 실제 에셋 생성 때 발급하고 인계서에 기록한다. 등록 직전에 병렬 작업 중 충돌을 다시 검사한다.

| 용도 | 예약값 / 소유 |
|---|---|
| 새 Map ID = 씬 파일명 = loadSceneName | `DroughtVillage` / B 제작, I 등록 |
| 새 씬 경로 | `Assets/01.Scenes/Scenario/DroughtVillage.unity` |
| 호수 도착 ID | `livingworld.lake.from_drought` |
| 피해 마을 도착 ID | `livingworld.drought.from_lake` |
| 호수 거점 activationId | `livingworld.lake.outskirts` |
| 피해 마을 거점 activationId | `livingworld.drought.entrance` |
| 영속 해금 키 | 기존 `PortalActivationState` 규약의 `portal.activated.<activationId>` |
| 이관 완료 표시안 | `lake.living_world.map_split.v1` / B가 복사본 변환을 전부 마친 뒤 기록. 기존 `lake.red_tree.content.v1`와 별개 |

호수 출구의 target는 `DroughtVillage` + `livingworld.drought.from_lake`, 피해 출구의 target는 `LakeOfLifeScenario` + `livingworld.lake.from_drought`다. 도착 ID는 씬 전역 registry 키이므로 두 맵에서 다르게 사용한다.

현 RegionInfo의 기존 도착/해금 ID 4개 `a735624d99ed5ad688f7b306205373ec`, `87febdaa4e74507e884e0698c03a2d05`, `c5632713fd675177a106a8ea6e9e8a97`, `0e5e7cb473d759d78bcbaad42bdd354b`는 그대로 둔다. 마지막 ID는 기존 시작 활성 거점이며 새 피해 마을 발견용으로 재사용하지 않는다.

지도 브라우즈의 PortalEntry는 **그 지역으로 도착하는 거점**을 표현한다. 피해 마을 RegionInfo의 entry는 targetSceneName=`DroughtVillage`, arrivalId=`livingworld.drought.from_lake`, activationId=`livingworld.drought.entrance`여야 한다. 같은 곳의 귀환 출구 PortalActor가 가진 반대편 target를 그대로 복사하면 지도 클릭이 호수로 돌아가는 오류가 된다. 이 구분은 I가 등록 검사한다. 지역 표시명은 현재 코드에서 mapId를 사용하는 부분이 있으므로 내부 이름이 그대로 노출되는지 I가 UI 저작 단계에서 처리한다. 새 고유 지명은 이번에 만들지 않는다.

### 5.2 기존 영속 식별자

기존 퀘스트 경로는 `Assets/10.Datas/Quest/RedTree/`이고 파일명은 `<questId>.asset`이다.

| questId | GUID |
|---|---|
| `quest_main_red_tree_s03` | `ef0f0c9fef671d34bba576a3f49ec197` |
| `quest_main_red_tree_s04` | `af15abd50c683fb439d0e1dd77a27a7b` |
| `quest_main_red_tree_s05` | `a316c60a48004f648a028c97fdfc712c` |
| `quest_main_red_tree_records_next` | `ef534ce8efccee54b9d5e654c7211ab0` |
| `quest_main_red_tree_s06` | `3e7463399f31f92488b27f5e0a20999f` |
| `quest_main_red_tree_s07` | `e908195e5581c8a45be4a1014656bf42` |
| `quest_main_red_tree_marks_next` | `ea6da350bda79464c976aca1cf39c86b` |
| `quest_main_red_tree_s08` | `b96909523838d154983531e0a5b3f112` |
| `quest_main_red_tree_s09` | `cf43bc652646a2440934cd69b08cb42e` |
| `quest_main_red_tree_response_next` | `743ed2a857aed0149860bd2bf91c54c9` |
| `quest_main_red_tree_s10` | `058fc4ec30e52cd41a0ff772f15da23a` |
| `quest_main_red_tree_s11` | `837305f46334214439d44b621176bbbb` |
| `quest_main_red_tree_s12` | `d9d76d4790bf9f344aff5fdd44324877` |
| `quest_main_red_tree_return_next` | `fbaa0a0832a7d0b4f8a056e86c769f0b` |
| `quest_main_red_tree_s13` | `458b37336eb6a294c82e11cb482793cf` |

既存 Graph/Node/Dialogue/Quest/Objective/Actor/Flag/Volume ID는 의미를 바꾸지 않는다. 이관되는 볼륨도 `lake.red_tree.r04.well`처럼 기존 ID를 유지한다. mapId만 새 지역 소속으로 대응하며, 소비자가 요구하는 Actor ID의 대소문자도 보존한다.

아래는 핵심 주민/이관 대역의 SceneEntityId다. `ownerKey`는 모든 행에서 `GlobalObjectId_V1-2-1d16ed83654f4f14e8628ca754f42067-<component fileID>-0` 형식이다. 예: 남은 주민은 `GlobalObjectId_V1-2-1d16ed83654f4f14e8628ca754f42067-1721164829-0`.

| 배치 | SceneEntityId GUID | component fileID | 처리 |
|---|---|---|---|
| `NPC/NPC_Joy` | `6b08052de51b4d4e8763f992b33bcf43` | 1526290806 | 호수 유지 |
| `NPC/Npc_Hazel` | `b33988d7b3c04e4b8d8f6df7f61fc631` | 1515089791 | 호수 유지 |
| `NPC/NPC_Morgan` | `d46597ed4b7b46d5b78b38aefe927394` | 346418509 | 호수 유지 |
| `Npc_Yeonmok` 프리팹 인스턴스 | `8702745799d24ce199852e3a31ad9d5e` | 801832256 | 호수 유지. 프리팹 GUID `1177a4a8242168e4893b68ec49258c52`와 구별 |
| R04 `RedTree_RemainingResident` | `246895bd33e24c92ad5e4b8b548345e4` | 1721164829 | 피해 마을로 같은 주민 이관 |
| R04 `RedTree_SettlingResident` | `efe248fe56b44d19a27886ff5d9d3dab` | 1978493502 | 피해 마을 출발 준비 자리. 실제 정착 완료를 만들지 않음 |
| R06 설치를 도운 주민 | `083d77d937fd40a99cd30c33e09ef295` | 259339706 | 피해 마을로 이관, 위 두 주민과 별도 배역 |
| R06 묘묘 대역 | `5621369a51b2427d85d9012f62350f08` | 2029760315 | 피해 마을로 이관 |
| R09 묘묘 | `a136338a65504c7aa7424cbd2560d52d` | 67285914 | 피해 마을 |
| R09 릴리 | `103d96c2c2e14b2c8246b5a6b746613f` | 1510391018 | 피해 마을 |
| R09 설아 | `1f39e0d43ead45528b4f4834fcdc0d17` | 1120061490 | 피해 마을 |
| R09 화린 | `97cecb8ab93c4a18b7b2eca9b56f4612` | 1881134159 | 피해 마을 |
| R09 유라 | `910fc3319f4f4a08938faa0187e7a6ca` | 174728451 | 피해 마을 |

`SceneEntityId.OnValidate()`는 ownerKey가 달라지면 `_guid`까지 다시 발급한다. 새 씬으로 이동하면 ownerKey 변경은 정상이다. B/I는 이동 전 GUID를 확보하고 새 씬 인스턴스에서 기존 `EditorSetGuid(기존 GUID)`로 GUID와 **새 ownerKey**를 맞춘 뒤 저장한다. 옛 ownerKey 문자열을 새 씬에 억지로 유지하면 다음 검증에서 다시 발급된다. fileID는 씬 로컬 식별자여서 영구 주민 ID 대신 사용하지 않는다.

현재 텍스트에서 SceneEntityId 103개의 명시 블록과 고유 GUID 103개를 확인했다. 이는 Prefab을 Unity로 모두 확장한 Missing Script/참조 검사 결과가 아니다. 위 표 밖의 채집·몬스터·소품까지 옮긴다면 B/I가 이동 목록을 확장하고 **모든 이동 GUID의 old map→new map** 대응표를 제출해야 한다. 이름이나 Actor ID만으로 저장 키를 추정하지 않는다.

### 5.3 저장 필드별 소유와 이관 요구

| 실제 저장 필드/소유 | 분리 시 처리 |
|---|---|
| `GameSaveData.party.loadSceneName/mapId/playerPos/playerRot/hasLocation` / PartyManager | 구 `LakeOfLifeScenario` 피해 구역 저장만 새 맵으로 대응. 일반 호수 저장·다른 맵 저장은 유지. X/Z 유지안이어도 새 지면·회전·안전성을 확인해야 위치 보존 가능 |
| `flags.flags` / GlobalFlagManager | R04~R09 사실·주민 선택·인계·영입 조건·표식·PortalActivation을 그대로 보존. 일과/비가 수정하지 않음 |
| `quest` / QuestManager | ID·완료·누적·추적 유지. 새 mapId/marker는 안내 데이터 교체이며 진행 초기화가 아님 |
| `party` 명단/최초 주인공/성장, `inventory`, `story` | 유지. 활성 조작 인물을 최초 주인공으로 추정하지 않음 |
| `world.killedMonsters[mapId]` / WorldStateManager | 이동한 GUID만 old map 집합에서 new map 집합으로 이동·중복 제거. 호수의 나머지 처치 집합 유지 |
| `world.consumedInteractables[mapId]` | 이동한 채집/파괴형 GUID만 대응. NpcLife 완료는 이 필드가 아니라 GlobalFlag |
| `world.respawnStates` | 이동한 항목의 mapId와 위치/회전만 검증된 대응으로 변환. actorId·대기 여부·count·재스폰 시각 보존 |
| `flow.firedKeys/entries` / FlowProgressSaveable | graphId/nodeId 유지 시 그대로. 그래프 분할로 키가 바뀌면 명시적 변환·멱등성 검사 선행 |
| `time.totalGameMinutes/totalPlaySeconds` / GameTimeManager | 보존. 일과와 첫 날씨 구현은 이 시각으로 재구성 |
| 작업 예약·손 소품·모션 진행률 | 저장하지 않음. 로드/씬 이탈 때 해제하고, 기존 서사 배치 복원 뒤 새로 예약 |

현재 저장 버전은 `3.4`다. `RedTreeSaveMigration.TryApplyToCopy`와 `SaveManager.TryCreateRedTreeCopy`는 **옛 스토리→붉은 나무 도입** 이관이며, 완료 플래그 `lake.red_tree.content.v1`가 있으면 그대로 반환한다. 이를 이번 맵 분리 변환으로 재사용하거나 그 완료 키를 지우면 안 된다. 복사 슬롯의 검증·암호화·빈 목적지·임시 파일 처리 패턴만 참고한다.

B가 만들 위치 이관은 다음 순서를 지킨다.

1. 원본을 읽은 뒤 별도 빈 슬롯 사본에만 적용한다. 원본/backup/tmp가 있는 대상은 덮어쓰지 않는다. 저장 버전만으로 맵 분리 적용 여부를 판정하지 않고 별도 콘텐츠 이관 표시를 사용한다.
2. `loadSceneName`과 `mapId`가 둘 다 구 시나리오임을 확인한다. 불일치/알 수 없는 버전은 자동 추정하지 않고 실패 사유를 남긴다. `hasLocation=false`는 기존 기본 스폰 계약을 유지한다.
3. 실제 구 저장과 이관 배치 목록에서 확인한 구역만 위치 변환한다. A의 `(938~978,452~536)` X/Z 범위는 피해 사건을 모아 보는 **조사 범위**일 뿐, 확정된 자동 이관 볼륨이 아니다. 경사·동굴·인접 호수 길을 포함할 수 있으므로 이 사각형만으로 무조건 새 맵으로 보내지 않는다.
4. 확인된 피해 구역 안의 구 저장은 새 맵에서 지면·KCC·NavMesh 검사를 통과할 때 좌표를 유지한다. 피해 구역임은 확인됐지만 정확한 위치가 불안전하면 새 피해 도착점으로 복구한다. 경계/출처가 불명확한 시나리오 위치는 **호수의 안전한 귀환 도착점**으로 복구하고 진행 사실을 보존한다. 현재 기본 도입 스폰으로 되돌리거나 퀘스트를 재시작하지 않는다.
5. 플레이어가 호수에 저장했더라도 이동된 엔티티의 world 상태는 GUID 목록에 따라 변환한다. 위치 판정 성공 여부와 엔티티 상태 이관은 별개다. 알 수 없는 GUID는 삭제하지 않고 보존·보고한다.
6. 모든 변환과 복사본 재독해 검증 후 이관 완료 키를 쓴다. 재적용 시 무변경이어야 한다. I의 새 도착 좌표가 확정되기 전에는 저장 파일에 가짜 위치를 쓰지 않는다. 현재 저장에는 arrivalId 필드가 없으므로 복사본에는 검증된 좌표/회전 또는 검증된 씬 기본 스폰 정책을 사용한다.

구 저장 실파일을 이번 A에서 복호화·로드·변환하지 않았다. R04 중간/완료, R05 구조 중, R06 표식 부분 완료, R07 배급 일부, R08 승리/미관찰, R09 인계 전후/결말, 호수·피해 구역 경계의 복사본 확보는 B/V 선행 조건이다. 이전 문서의 상태 주입 저장을 정상 완주 저장으로 승격하지 않는다.

## 6. 공용 연결 계약 — 기존 기능과 신규 구현 구별

### 6.1 맵 이동·도착·발견

기존 경로: `PortalActor` → `ActorSvc.SceneTransition.LoadScene(sceneName, arrivalId)` → `SceneManager`의 Loading 씬 → `SceneArrivalRegistry` → KCC `SetPositionAndRotation` → 카메라/액터 안정화. `ISceneTransitionService`에는 현재 **void LoadScene 두 문자열 인자만** 있다. 성공 bool, Task, 완료 이벤트는 그 인터페이스에 없다. 구체 `SceneManager`에는 `IsLoading`, `OnLoadStarted/OnLoadFailed/OnLoadComplete`가 존재한다.

| 요구 | 현 코드 판정 | B/I 구현 계약 |
|---|---|---|
| 길 표식에서 명시적으로 이동 입력 | 미지원. PortalActor.Interact는 미해금 포털의 해금만 수행하고 OnTriggerEnter가 이동 | 기존 동작 기본값을 유지하는 입력 이동 옵션 또는 최소 어댑터를 B가 구현. 새 두 출구는 입력 이동으로 설정. 해금 입력만 했는데 이동 완료로 표시하지 않음 |
| 실패 후 재시도 | 보완 필요. PortalActor는 scene 요청 때 `_isActivating=true`; 맵 내 이동에만 지연 리셋 있음 | 서비스 없음·빈/미등록 씬·중복 거부·비동기 실패마다 점유/입력/카메라 해제와 재시도 가능성을 검사 |
| 중복 요청 | SceneManager는 `_isLoading` 중 거부하지만 LoadScene overload가 먼저 PendingArrival을 씀 | 요청 수락 전에 PendingArrival을 덮어쓰지 않도록 B 수정. 승인된 요청 한 개가 도착 값을 소유 |
| 조기 실패 통지 | 미등록 씬은 StartLoad에서 로그 후 반환, OnLoadFailed를 거치지 않음 | 구체 이벤트만 기다리는 호출자는 고착 가능. B가 수락/거부와 실패 종료를 구별하고 I에게 최소 계약 확장 요청 |
| 도착 성공 | 없는 arrivalId는 경고 후 기본 스폰; 안정화 완료는 목표 도착 ID 성공 증거가 아님 | 두 신규 경로는 ID 존재·유일성, 실제 도착 오차·KCC 접지·카메라·필수 지역 준비를 확인한 후 성공 처리 |
| 재입장 발견 | 기존 해금은 입력 즉시 저장 | 출발지 해금과 목적지 발견을 구별. 피해 거점 해금은 실제 피해 맵 준비/안전 도착 뒤 같은 기존 PortalActivation 키로 기록. 실패 요청으로 목적지 해금 금지 |
| 작업/호위 중 이동 | 전환과 생활 작업의 통합 게이트 미지원 | 대화·인계·호위 완료 또는 명시 취소 후 전환. 미완료 완료 사실을 만들지 않음. 출구에서 취소·재시도 가능. 강제 전환도 C 작업 정리 |

새 B 호출부가 구체 `SceneManager.Instance`를 Actor에 추가하지 않는다. 필요한 수락/실패/완료 소비자 계약은 I가 `GameActor/Contracts/ActorServices.cs`에 게시하고 B가 구현한다. 현재 Loading 경유 후 대상 로드 실패 시 원래 씬이 이미 사라질 수 있으므로 단순 `_isActivating=false`만으로 복구 완료라고 하지 않는다. 복귀 씬/안전 도착 복원과 기존 진행 보존을 LW-T01/T04에서 검사한다.

### 6.2 주민 6명과 작업점

선정은 기반 기획 5.1의 선택지에서 **헤이즐·연목**을 택했다. 기존 돌봄/약 포장과 수선 자산이 있어 역할을 바꿀 필요가 적기 때문이다. NPC의 새 이름·가족 관계·새 의사 결정은 추가하지 않는다.

| 주민 / 실제 Actor ID | 일과 제안과 근거 | 모션/소품 후보 및 부족 |
|---|---|---|
| 조이 / `NPC_Joy` | 조리대 준비→배식 자리 확인→그릇 정리→가까운 휴식. R03 식사·R09 포장 몫과 장식 일과 분리 | 기존 빵·그릇·작업대, `Npc_Walk`, 착석. 전용 조리·그릇 들기 모션 미확보. 대장장이 모션을 조리로 위장하지 않음 |
| 헤이즐 / `NPC_Hazel` | 약 꾸러미 분류→처치 자리 방문→처마 휴식 | `Humanoid_PackMedicine`, 병/약상자, 걷기·착석. 처치 손동작/이동 중 약상자 자세는 추가 확인 |
| 모건 / `NPC_Morgan` | 짐 확인→수레 점검→대기. R09 반환 진행에 따른 작업 위치는 I 서사 배치가 우선 | 수레·자루·통, 걷기. 짐 운반/끈 당김 클립 미확보. 현재 수레 관련 입력과 인물 거리는 현장 확인 필요 |
| 연목 / `Npc_Yeonmok` | 공구 준비→수선→완성품 자리 확인→휴식 | 기존 연목 프리팹, Blacksmith 클립 후보. 공구 접점·루프·시작/종료·스케일 미검증 |
| 남은 주민 / `RedTree_RemainingResident` | 씨앗/도구 돌봄→그늘→우물 확인. 식수 지원 이후에도 남는 선택 유지 | 기존 씨앗 자루·통, 걷기/착석. 씨앗 돌봄·물통 운반 전용 모션 미확보 |
| 짐을 꾸리는 주민 / `RedTree_SettlingResident` | 짐 정리→출발 준비 자리→대기/휴식 | 기존 bag/ready luggage와 R04 실제 보행. `departure_prepared` 이후 반복 일과가 짐을 원위치로 되돌리지 않음. 실제 호수 정착/귀향을 임의 생산하지 않음 |

작업점 ID는 **신규 예약**이며 mapId와 함께 식별한다. C는 사람 이름으로 코드 분기하지 않고 데이터가 아래 작업점을 참조하게 한다. 각 작업점의 포즈·모션·도구·용량은 C 설정, 실제 Transform은 B/I 씬 바인딩이다.

| 맵 / 구역 | 예약 작업점 ID 묶음 | 초기 정원·배치 조건 |
|---|---|---|
| 호수 / 식사 | `lake.meal.prep`, `lake.meal.serve`, `lake.meal.rest` | 각각 1, 1, 1. 기존 R03 손님 자리와 다른 자리 |
| 호수 / 돌봄 | `lake.care.pack`, `lake.care.visit`, `lake.care.rest` | 각각 1. 필수 돌봄 입력의 전면을 비움 |
| 호수 / 운송 | `lake.transport.inspect`, `lake.transport.cart`, `lake.transport.rest` | 각각 1. 서사 수레 적재 입력과 분리 |
| 호수 / 수선 | `lake.repair.tools`, `lake.repair.work`, `lake.repair.rest` | 각각 1. 연목의 현재 미술 위치 우선 |
| 피해 / 잔류 | `drought.resident.tools`, `drought.well.wait`, `drought.shade.stay` | 각각 1. 건조 우물 대기는 물 생산이 아님 |
| 피해 / 출발 준비 | `drought.depart.pack`, `drought.depart.wait`, `drought.shade.depart` | 각각 1. 그늘의 두 자리는 구별된 슬롯이며 주민 두 명 동시 사용 가능 |

06~09시 준비, 09~18시 작업, 18~22시 정리/휴식, 22~06시 가까운 안전 자리 대기를 **일과 초기 제안**으로 정한다. 근거는 기반 기획의 세 구간이며 밤의 퀘스트 접근성을 위해 인물을 삭제하지 않는다. 시작 오프셋·대기 길이는 데이터로 조절한다. 시간대가 바뀌어도 퀘스트 인물은 현재 자리 또는 가까운 대체 자리에서 만날 수 있어야 한다.

**현재 지원:** `NpcBrain.SetHomePose/SetWorkMotion`, Idle/Talk/Wander, `NpcMovementController`와 KCC, `NpcActor.BeginDialogueStage/TryBeginDialogueStep/StopDialogueStep`, `ActorSvc.Simulation.AcquireActiveLease`, `AgentTickManager.Register(owner,tick)`. `NpcLifeInteractable`은 취소·로드·제한시간·소품 정리를 처리하고 `NpcLifePresentation.TryCompleteAction`은 실제 도착/표시 뒤 플래그를 확정한다.

**현재 없는 것:** 작업점 registry/예약/일과 전환/피신 우선순위/일과 재개 커서. `NpcTalkState.UpdateVelocity`의 보행은 목적지 방향으로 움직이며 NavMesh 경로 탐색이 아니다. NavMesh가 구워져 있다고 장애물 우회가 자동 제공되지 않는다. 짧은 검증된 구간·명시 경유점부터 사용하고 막히면 정지/해제/대체한다. 필요하면 C가 경로 소비를 추가하지만 NPC별 새 Update나 별도 군중 매니저를 만들지 않는다.

또한 `BeginDialogueStage`를 일과 내내 잡으면 `NpcActor.CanInteract()`가 false가 된다. C는 일과 보행을 대화 홀드로 계속 구현하지 말고, **플레이어가 언제든 대화를 시작할 수 있는 일과 실행 경로**를 만든다. 기존 서사 보행의 홀드·취소 계약은 그대로 유지한다.

신규 작업점 행위 계약(메서드가 이미 존재한다는 뜻 아님):

1. 예약은 `(mapId, workPointId, slot, actor identity, 실행 세대)`의 단일 소유권이다. 같은 자리 경쟁은 한 명만 성공한다. 정원이 차면 가까운 대기/대체 자리로 가고 플레이어를 밀지 않는다.
2. 예약→이동→도착/방향 정렬→소품 부착→작업→소품 정리→해제. 도착 실패/정체/시간 초과/대화 시작/날씨 변경/작업 취소/OnDisable/로드/씬 종료에서 해제는 멱등이어야 한다.
3. 우선순위는 위험 회피→진행 중 대화·플레이어 작업→서사 배치→날씨 피신→시간대 일과→자율 행동. 위험 중 안전한 취소 없이 연출을 덮어쓰지 않는다. 인계 중단은 마지막 완료 사실까지만 남긴다.
4. 예약을 잃은 늦은 콜백은 모션·플래그·소품을 바꾸지 못한다. 일반 일과는 인벤토리·퀘스트·`F.*`를 쓰지 않는다.
5. 대화 종료 후 현재 시각·서사·환경에서 다시 선택하고 재예약한다. 옛 Transform으로 순간이동해 재개하지 않는다. 화면 밖 배치 복원은 기존 Presentation 정책을 사용하며 화면 앞 일과 이동과 구별한다.
6. 저장에는 점유/손 소품/루프 프레임을 넣지 않는다. 로드 때 모든 임시 소유권을 비우고, 서사 배치가 복원된 뒤 일과를 시작한다. 비활성/숨김 대역은 점유하지 않는다.

### 6.3 대표 상호작용 6곳

| 번호 | 맵 / 실제 대상 또는 예약 출구 | 입력 결과·완료 소유 |
|---|---|---|
| 1 | 호수 / R03 `식사를 나른다`, GO 377003834, `(1030.5,62.051,409)` | 기존 NpcLife 실행, `F.meal_served`. 선행 `F.arrival_acknowledged`, 손님 실제 도착·식사 결과. 새 일과 보상 아님 |
| 2 | 호수 / R03 `돌봄 물품을 건넨다`, GO 1157400429, `(1006,62.631,410)` | 기존 NpcLife 소품 인계, `F.care_delivered`, 선행 `F.meal_acknowledged`. 자리 확보 `care_space_ready`와 구별 |
| 3 | 호수 / 새 외곽 길 표식 | 명시 이동 입력→새 피해 도착. 사건 완료 플래그 없음. 이동 실패면 목적지 발견 없음 |
| 4 | 피해 / `R04_well`, volume `lake.red_tree.r04.well` | 기존 조사·대화 정상 종료가 `F.well_examined`. 수락 전 발견 누적, 진실 자동 생산 금지 |
| 5 | 피해 / `R04_water`, GO 814841913 | `F.resident_stays` 후 실제 식수 소품 인계→`F.water_handed_over`. R06 외부 물통·R09 반환과 구별 |
| 6 | 피해 / 새 귀환 길 표식 | 호수 안전 도착. 밤·날씨·재고 부족으로 잠그지 않음 |

R03 식사/돌봄을 이미 마친 저장은 표시 결과를 확인하며, 반복해서 완료/물자를 만들지 않는다. 시범 테스트는 각 기존 사건에 맞는 시작 저장을 사용한다. 이 6곳을 한 번에 열기 위해 선행 사실을 켜지 않는다. 게임패드 상호작용·Cancel은 기존 입력 경로를 쓰며 새 확인 UI가 필요하면 I가 UI 지침을 적용한다.

### 6.4 환경 입력과 저장 소유

**현재 존재:** `IGameTimeService.OnGameMinuteChanged`, `CurrentDay`, `MinuteOfDay`, `CurrentDayPeriod`, 시간 저장. 서비스에는 `OnDayPeriodChanged`나 `TotalGameMinutes`가 없다(구체 GameTimeManager에는 있음). C/D는 서비스의 day/minute로 일과를 계산한다. `WorldLightingManager`가 씬당 controller를 만들고 `WorldLightingSceneBinding`으로 Light/Volume을 연결한다. `WorldLightingController`가 태양·달·주변광·노출의 최종 작성자다.

**현재 미구현:** 공용 날씨 상태/서비스, 지역 날씨 저장 DTO, 실내 강수 억제, NPC 피신 입력. 다음 최소 계약을 I가 게시하고 D가 생산, C가 읽는다. 이는 **신규 계약안**이며 현재 코드에 있다고 간주하지 않는다.

| 데이터 | 의미·소유 |
|---|---|
| mapId, 준비 여부, 환경 변경 revision | 다른 씬의 늦은 환경 통지를 C가 무시하도록 식별. 미준비일 때 현재 안전 작업을 유지하고 새 피신을 시작하지 않음 |
| 바람 방향/세기, 강수량, 일시적 표면 젖음 | D의 단일 상태. 식생·물결·먼지·비·소리와 C가 같은 값을 소비 |
| 날씨 프리셋과 진행 시각 | D가 GameTime 기준으로 결정. C는 별도 난수/날씨 시계를 만들지 않음 |
| 서사 회복 사실 | 기존 `F.absorption.stopped`, 표식별 disconnected와 지역 공급 사실을 읽음. 날씨가 쓰지 않음 |
| 보호 구역/작업점의 실내 여부 | 지역 바인딩 데이터. D는 비/먼지 억제, C는 대체 작업/피신 선정에 사용. 서사 표식과 별개 |

첫 D 구현은 **맵별 고정 시간표 프로필 + 저장된 게임 시각**으로 날씨와 그 진행 위치를 재구성하는 안을 채택한다. 입장 시 무작위 재추첨을 하지 않고 별도의 날씨 시드·저장 필드를 도입하지 않는다. 같은 프로필 버전/게임 시각이면 재입장·로드 결과가 같아야 한다. 새 게임은 기존 시계 초기화에 따른다. 프로필의 지속시간·위상·보간으로 젖음을 계산해 비가 그친 직후 재입장 시 바로 마른 상태로 튀지 않게 한다.

이것은 임의 날씨 변경을 저장하는 기능이 아니다. 저장 시각만으로 복원할 수 없는 랜덤 순환·수동 변경·프로필 버전 교체가 필요해지면 D가 `(mapId,presetId,startGameMinute,seed,profileVersion)` 저장 확장을 제안하고 **I가 Data/저장 등록을 먼저** 담당한다. C나 셰이더가 별도 저장 파일/GlobalFlag 숫자 인코딩을 만들지 않는다. 출시 중인 프로필 v1을 바꿔 구 저장의 날씨를 무단 재해석하지 않는다.

피해 마을 회복 전 시간표는 맑음/흐림/건조한 바람만 사용한다. 회복 뒤 비는 표현 선택이며 `return.water_checked`나 우물 회복을 완료시키지 않는다. 호수 비·처마 피신은 독립 검증부터 가능하다.

셰이더/조명 세부 계약:

- `Assets/06.Shaders/LakeWater.shader`: 현 `_WaveVelocity/_WaveStrength/_WaveScale`은 물결 표현, `_Recession`은 기존 흡수 관찰 표현. D의 날씨가 `_Recession`을 덮어쓰지 않는다. 물 높이/서사 물길 활성화도 기존 사건 소유다.
- `Assets/06.Shaders/CreekGround.shader`: 현 젖음은 **vertex color R**, 가장자리 마스킹은 **A**, 표면 설정은 `_WetColor/_WetSmoothness`다. `_Wetness`나 공통 풍향 전역값이 이미 있다고 쓰지 않는다. 날씨 젖음은 별도 프로퍼티/입력으로 합성하고 기존 R/A 의미를 보존한다.
- 바람/날씨가 해·달·Volume에 별도 Update로 값을 쓰지 않는다. D가 기존 WorldLightingController의 최종 반영점에서 합성한다. 공유 원본 Material과 외부 팩을 수정하지 않고 프로젝트 변형/MPB를 사용하며 기존 MPB의 서사 값을 보존한다.
- 전역 셰이더 값·Volume runtime 사본·오디오·파티클은 D가 생성한 소유분만 정리한다. 씬 종료/프로필 해제에서 기본값 복구와 늦은 갱신 차단을 검사한다.
- 실내 억제는 지붕/문턱·카메라 급회전·효과 방출 범위까지 실제 확인한다. KCC의 물리 Trigger 콜백만으로 피신/실내 판정을 보장하지 않는다. 환경음 후보는 미청취이며 대화/전투 소리 우선 정책과 함께 D가 선정한다.

## 7. 자산 후보와 부족 판정

아래는 **파일 존재와 일부 직렬화 참조 확인**이다. 런타임 리깅·URP 호환·근접 품질·클립 재생을 통과한 목록이 아니다. 외부 원본은 읽기 전용으로 다룬다.

| 용도 | 실제 후보 경로 | 후속 확인 |
|---|---|---|
| 집 | `Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Buildings/Presets/House_01.prefab` | 마을 미술 기준. 문/실내/충돌/처마 실제 크기 |
| 우물·통 | 같은 팩 `Prefabs/Props/Exterior Props/TGV_Well_01A.prefab`, `TGV_Bucket_01A.prefab` | 우물 내부 마름 표현·통 손잡이/피벗. R04 실물 우선 |
| 휴식 | 같은 팩 `Prefabs/Buildings/Interior Props/TGV_Bench_01A.prefab` | 착석 높이·일어서기·통로 점유 |
| 수레 | `Assets/ExternalAssets/Environment/Fantastic Dungeon Pack/prefabs/PROPS/misc/P_PROP_cart_dungeon_01.prefab` | 기존 R09 수레와 색/형태 비교. 미술 교체 강제 안 함 |
| 바람 | `Assets/ExternalAssets/Environment/Idyllic Fantasy Nature/Prefabs/Code Related/WindControl.prefab` | 대상 셰이더/풍향 프로퍼티·단위·전역 덮어쓰기 여부 미확인. 그대로 추가 실행 금지 |
| 비 | `Assets/ExternalAssets/FX/Cartoon FX Remaster/CFXR Prefabs/Nature/CFXR4 Rain Falling.prefab`, `CFXR4 Rain Splashes.prefab` | URP·방출 범위·실내 관통·투명 오버드로 |
| 먼지 | `Assets/ExternalAssets/FX/Magic effects pack/Prefabs/Smoke effects/Dust ground.prefab`, `Dust puff.prefab`, `Dust loop.prefab` | 근접 가독성·바닥 높이·재생 비용 |
| 기본 NPC 동작 | `Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/`의 `Npc_Walk.asset`, `Npc_Chair_Sit_Idle.asset`, `Npc_Female_Idle.asset`, `Npc_Male_Idle.asset`, `Npc_Talk.asset`, `Npc_Talk_2.asset` | 실제 주민 리그에서 발 접지·루프·전환 |
| 돌봄 | 같은 Npc 폴더 `Humanoid_PackMedicine.asset`, `Humanoid_ProtectWound.asset` | 약 포장은 작업, 부상 보호는 기존 부상 배역에만. 건강한 주민 휴식으로 남용하지 않음 |
| 일어서기 | `Assets/ExternalAssets/AnimationOnly/RPG_Animations_Pack/FBX_Animations/NPC/Chair Sit/Chair_Sit_Idle_10_Stand_Up.fbx` | MotionSet 연결·의자 높이·종료 자세 미검증 |
| 수선 | 같은 팩 `FBX_Animations/Actions/Crafting/Blacksmith/Blacksmith_Stand_Play_1.fbx`, `Blacksmith_Stand_Start_1.fbx`, `Blacksmith_Stand_End_1.fbx` 및 앉은 변형 | 연목 공구/작업면 접점과 사용 가능한 Avatar 확인 |

기존 연목 프리팹은 `Assets/03.Prefabs/Characters/ScenarioResidents/Npc/Npc_Yeonmok/Npc_Yeonmok.prefab`이다. 주민 데이터는 RT의 `NPC_RedTree_NPC_Joy.asset`(GUID `e415af0bc57d5794a8961e6095013ed6`), `NPC_RedTree_Npc_Hazel.asset`(`0834151fb04befb4ab1f8d5580443372`), `NPC_RedTree_NPC_Morgan.asset`(`7d521134ddfd9ac4ab1499d198405eb7`), `NPC_RedTree_Npc_Yeonmok.asset`(`15e8f100f06e04a4b93ef7484cdc62c5`)를 참조한다. I 소유의 기존 데이터에 C가 병렬로 덮어쓰지 않는다.

E 제작 후보는 마른 우물 내부, 시든 작물 형태, 수로 접합부, 공구/운반 소품으로 제한한다. **필요 확정은 아님.** B/C/D가 사용 위치·크기·근접 거리·기존 후보의 부족 이유를 전달한 뒤 만든다. D가 텍스처별 UV·채널·색공간·재질 프로퍼티를 먼저 정한다. ImageGen 결과를 그대로 PBR 세트로 간주하지 않는다. 모션 부족은 모델/텍스처 생성만으로 해소되지 않으므로 C가 별도 클립 확보 항목으로 보고한다.

## 8. 정확한 파일/폴더 소유권

아래 경로는 저장소 루트 기준이다. **기존**과 **신규 예약 경로**를 구별한다. 표에 없는 공유 파일을 임의로 확장 소유하지 않는다. 모든 에셋/폴더의 `.meta`도 같은 소유자다. 기존 파일의 서로 다른 줄을 여러 담당이 동시에 수정하지 않는다.

### B — 맵/전환/구 저장

| 종류 | B 단독 수정 범위 |
|---|---|
| 기존 런타임 | `Assets/02.Scripts/GameActor/Object/Portal/PortalActor.cs`; `Assets/02.Scripts/Scene/SceneArrivalPoint.cs`; `Assets/02.Scripts/Manager/Scene/SceneManager.cs`, `SceneManager.Load.cs`; `Assets/02.Scripts/Manager/Save/SaveManager.cs` |
| 신규 예약 코드 | `Assets/02.Scripts/Manager/Save/SaveManager.LivingWorld.cs`; `Assets/02.Scripts/Manager/Save/LivingWorldMap/`; `Assets/02.Scripts/GameActor/Object/Portal/LivingWorld/` |
| 신규 씬/에셋 | `Assets/01.Scenes/Scenario/DroughtVillage.unity`; `Assets/10.Datas/World/LivingWorld/Maps/`; `Assets/03.Prefabs/LivingWorld/Map/` |
| 새 맵 지형/NavMesh/Region/Minimap | 위 `World/LivingWorld/Maps/DroughtVillage/` 아래 생성. 기존 호수의 지형·NavMesh·지도는 I |
| 검사/보고 | `Assets/Tests/EditMode/LivingWorld/Map/`, `Assets/Tests/PlayMode/LivingWorld/Map/`; `Assets/docs/design/LivingWorld/B_MAP_HANDOFF.md` |

`SaveManager.RedTree.cs`, `Data/Save/RedTreeSaveMigration.cs`는 이번 새 이관으로 덮어쓰지 않는다. 필요한 기존 DTO 수정은 I에게 전달한다. 새 씬 인계는 I가 인수한 시점/버전을 보고서에 기록하며 **그때부터 B는 새 씬과 그 씬의 지형/NavMesh에 쓰기를 멈춘다**. B 코드 결함 수정은 계속 B가 담당한다.

### C — NPC 생활

| 종류 | C 단독 수정 범위 |
|---|---|
| 기존 NPC 런타임 | `Assets/02.Scripts/GameActor/Component/NPC/NpcBrain.cs`; `Assets/02.Scripts/GameActor/Object/NPC/NpcActor.cs`; `Assets/02.Scripts/GameActor/MovementController/NpcMovementController.cs`; `Assets/02.Scripts/GameActor/State/NPC/` |
| 기존 생활 실행 | `Assets/02.Scripts/Gameplay/Interaction/NpcLifeInteractable.cs`, `NpcLifePresentation.cs` |
| 신규 예약 코드 | `Assets/02.Scripts/GameActor/Component/NPC/LivingWorld/`; `Assets/02.Scripts/Gameplay/Interaction/LivingWorld/` |
| 신규 프리팹/설정/모션 | `Assets/03.Prefabs/LivingWorld/NPC/`; `Assets/10.Datas/World/LivingWorld/NPC/`; `Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/LivingWorld/` |
| 검사/보고 | `Assets/Tests/PlayMode/FlowGraph/NpcLifeActionTests.cs`; 신규 `Assets/Tests/EditMode/LivingWorld/NPC/`, `Assets/Tests/PlayMode/LivingWorld/NPC/`; `Assets/docs/design/LivingWorld/C_NPC_HANDOFF.md` |

C는 본편 씬·기존 RT 주민 데이터·원본 캐릭터 프리팹·R04~R09 Flow를 직접 수정하지 않는다. B가 이관에 필요한 Presentation 필드 변경을 발견하면 C에게 요청한다. `AgentTickManager`/공통 `ActorSimulation` 정책 변경은 I 소유이며 C는 기존 등록/활성 임대 경로를 먼저 사용한다.

### D — 환경

| 종류 | D 단독 수정 범위 |
|---|---|
| 기존 런타임 | `Assets/02.Scripts/Gameplay/World/WorldLightingController.cs`, `WorldLightingSceneBinding.cs`; `Assets/02.Scripts/Manager/World/WorldLightingManager.cs` |
| 기존 셰이더 | `Assets/06.Shaders/LakeWater.shader`, `Assets/06.Shaders/CreekGround.shader` |
| 신규 예약 코드 | `Assets/02.Scripts/Gameplay/World/LivingWorld/`; `Assets/02.Scripts/Manager/World/LivingWorld/` |
| 신규 환경 에셋 | `Assets/06.Shaders/LivingWorld/`; `Assets/11.Material/LivingWorld/Environment/`; `Assets/03.Prefabs/LivingWorld/Environment/`; `Assets/10.Datas/World/LivingWorld/Environment/` |
| 검사/보고 | `Assets/Tests/EditMode/LivingWorld/Environment/`, `Assets/Tests/PlayMode/LivingWorld/Environment/`; `Assets/docs/design/LivingWorld/D_ENVIRONMENT_HANDOFF.md` |

D는 B의 SaveManager나 I의 GameTime/공용 등록을 수정하지 않는다. 원본 RT의 물/흙 재질·서사 수면 표현은 I 소유이며, D는 호환되는 셰이더와 연결안을 전달한다.

### E — 미술 자산

신규 예약 폴더 `Assets/05.Models/LivingWorld/`, `Assets/04.Images/LivingWorld/`, `Assets/11.Material/LivingWorld/Props/`, `Assets/03.Prefabs/LivingWorld/Props/`와 `Assets/docs/design/LivingWorld/E_ASSET_HANDOFF.md`만 소유한다. 환경 재질은 D 소유다. E는 그 재질에 들어갈 텍스처를 전달하며 D 파일을 저장하지 않는다. 외부 팩 원본과 본편 씬은 변경하지 않는다.

### I — 공용 계약·기존 콘텐츠·본편 연결

| 범위 | I 단독 소유 |
|---|---|
| 본편 씬/배치 소스 | `Assets/01.Scenes/Scenario/LakeOfLifeScenario.unity`; `Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario.layout.json`, `.world.json`, `.quality.json`; `Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/` 전체 기존 에셋. 생성기 재실행 허가가 아님 |
| 공용 계약 | `Assets/02.Scripts/Contracts/`, `Assets/02.Scripts/GameActor/Contracts/ActorServices.cs`, `Assets/02.Scripts/UI/Contracts/UIServices.cs`; 모든 asmdef |
| 공용 Data | `Assets/02.Scripts/Data/` 전체. 신규 예약 `Data/World/LivingWorld/`의 일과/환경/이관 설정 타입도 I가 게시 |
| 공용 초기화/시간/저장 참여 | `Assets/02.Scripts/Manager/GameManager.cs`, `Assets/02.Scripts/Manager/GameTimeManager.cs`, `Assets/02.Scripts/Manager/Save/WorldStateManager.cs`, `Assets/02.Scripts/Manager/Save/FlowProgressSaveable.cs`, `Assets/02.Scripts/Manager/Save/SaveManager.RedTree.cs`, `Assets/02.Scripts/Manager/Party/`; 신규 환경 저장 확장이 필요하면 DTO/등록도 I |
| 공용 Flow/표현 | `Assets/02.Scripts/FlowGraph/`; `Assets/02.Scripts/Gameplay/Interaction/StoryCharacterPresence.cs`; `Assets/02.Scripts/Gameplay/Quest/NpcQuestMarkerInstaller.cs`. 서사 물 표현의 기존 씬 오브젝트·RT 재질·Flow 에셋도 I 소유. C의 NpcLife 두 파일은 제외 |
| DB/빌드/주소 | `Assets/10.Datas/Map/MapConfigDatabase.asset`, `Assets/10.Datas/Quest/QuestDatabase.asset`, `Assets/10.Datas/Quest/RedTree/`; `Assets/AddressableAssetsData/`; `ProjectSettings/EditorBuildSettings.asset`; `Assets/Settings/Build Profiles/`를 사용하는 경우 해당 프로필 |
| 지도/표시 | `Assets/02.Scripts/UI/Scene/Map/`, `Assets/10.Datas/Dialogue/SpeakerActorBindingTable.asset`; 기존 지도/마커 데이터는 위 LakeOfLifeScenario 폴더 및 `Assets/02.Scripts/Data/Quest/QuestMarkerIntent.cs`, `Assets/02.Scripts/Data/UI/QuestMarkerIconSetSO.cs`. 기존 표시 프리팹 수정이 필요하면 실제 경로를 I 인계서에 추가한 뒤 I만 수정. 신규 맵 표시명/도착 등록도 I |
| 공통 시뮬레이션 | `Assets/02.Scripts/GameActor/AI/AgentTickManager.cs`, `Assets/02.Scripts/GameActor/Simulation/`, `Assets/02.Scripts/Manager/Actor/ActorSimulationManager.cs`, `Assets/10.Datas/Resources/Config/ActorSimulationSettings.asset` |
| 기존 통합 검사/보고 | 기존 `Assets/Tests/EditMode/FlowGraph/RedTree*.cs`, `Assets/Tests/PlayMode/FlowGraph/RedTree*.cs`, `Assets/Tests/EditMode/Content/RedTreeSaveMigrationTests.cs`; `Assets/docs/design/LivingWorld/I_INTEGRATION.md` |

주소/빌드 등록은 실제 프로젝트의 사용 중인 그룹/프로필을 I가 확인해 수정한다. B는 생성 GUID·씬 이름·도착점·Region/Minimap 경로를 전달하고 등록 파일을 직접 수정하지 않는다. 표의 신규 예약 폴더는 현재 존재하는 것으로 간주하지 않는다. 공용 부모 폴더 `.meta`는 I가 한 번 만들고, 각 담당은 자기 하위 폴더만 만든다. A가 만든 문서 폴더 `.meta`는 재사용한다.

Editor 씬 저장/임포트/생성/테스트/빌드, Blender 변경은 한 시점에 한 담당만 수행한다. 소유권 인계는 담당·파일·버전·시점을 문서에 기록한다. 공용 계약 변경 요청은 파일 경로·시그니처·생산자·소비자·기본/실패 동작·필요 검사까지 적어 I에게 전달한다. 합의 전 동명 대체 서비스를 따로 만들지 않는다.

## 9. 성능 기준선과 선행 검증

### 9.1 이번에 확인 가능한 기준선

| 구분 | 현재 확인값 | 해석 한계 |
|---|---|---|
| 씬 직렬화 | GameObject 명시 블록 31,541, PrefabInstance 8,554, Transform 39,986 | 프리팹 확장/활성/실제 렌더 개수가 아님 |
| 표현 컴포넌트 블록 | Animator 67, SkinnedMeshRenderer 2,733, MeshRenderer 374, ParticleSystem 27 | 비활성/대역/프리팹 상속 포함 방식이 다름. DrawCall·동시 표시 인원으로 환산 금지 |
| 스크립트 명시 블록 | NpcActor 53, NpcBrain 51, NpcLifeInteractable 33, NpcLifePresentation 53, FlowGraphRunner 14, WorldLightingSceneBinding 1 | 활성 주민 수·실제 러너 동시 실행 수가 아님. 프리팹 내부 컴포넌트가 별도일 수 있음 |
| 맵 원본 선언 | layout: routes 26/clearings 17/placements 183/scatters 20; world: zones 30/routes 46/monsters 28/props 32/portals 4/interactions 10 | 현재 사용자 씬을 재생성할 수 있다는 증거가 아님 |
| 시뮬레이션 설정 | wake 55m / sleep 65m / 평가 0.2초 / 4 buckets / 최소 active 1초 / unsafe retry 0.25초 | `ActorSimulationSettings.asset`의 설정값. CPU 절감 실측 아님 |
| 기존 생활 실행 기본값 | 입력 거리 3m, 도착 0.6m, 보행 배율 0.6, 제한 20초. R03 식사 도착 0.15m 예외 존재 | 씬별 override가 있어 새 일과에 일괄 복사하지 않음 |

현재 프레임 CPU/GPU ms, FPS, GC Alloc/frame, 메모리/VRAM, 배치/SetPass, 그림자/투명 오버드로, Cloth 비용, 로드 시간은 **모두 미측정**이다. 목표 기기·해상도·품질도 미확정이다. 과거 R04 Player 빌드 시간·임대 개수·단일 씬 통과 기록을 현재 성능 기준으로 사용하지 않는다.

비교 측정은 현행 씬 해시를 기준으로 호수 식사/돌봄/운송, 피해 우물/개울, 중앙 신사 구도에서 같은 카메라·시각·진행·NPC 활성 조건을 고정한다. 로딩/셰이더 워밍업을 분리하고 안정화 후 60초 표본의 CPU/GPU median·p95·GC·최대 메모리를 기록한다. 환경 없음/건조 바람/비, 주민 일과 없음/6명, 낮/밤을 각각 비교한다. 두 맵은 현재 씬과 직접 같지 않으므로 새 맵에서 환경·일과 on/off 비교도 남긴다. 3~5분 관찰과 10회 왕복으로 점유·FX·임대·재질 수의 누적 증가를 확인한다.

60fps의 16.7ms, 생활·환경 추가분 CPU/GPU 각각 2ms는 기반 기획의 **실험 상한 후보**다. 실제 기준선 여유를 측정하기 전 확정 예산/달성치로 쓰지 않는다.

### 9.2 구현별 선행 조건과 완료 검사

| 담당/단계 | 선행 조건 | 관련 검사 / 통합 대기 |
|---|---|---|
| B 작은 씬 | 예약 ID·B 소유권, 기본 바닥과 도착/귀환 쌍 | 씬/도착 유일성·왕복·실패/중복·입력 복구 LW-T01/T08. I 등록 전 본편 왕복 통과 표시 금지 |
| B 저장 이관 | I의 실제 이관 객체 목록·최종 안전 도착, 구 저장 복사본 | 필드 보존·멱등·알 수 없는 GUID·기존 슬롯 거부 LW-T04. Map별 consumed/respawn까지 검사 |
| C 생활 | 작업점/중단 계약·기존 몸 재사용, I 최소 Data 계약 게시 | 예약 경쟁·정체·대화 시작·취소·늦은 콜백·씬 종료·로드 LW-T03/T05. 기존 `NpcLifeActionTests` 회귀 |
| C 환경 연동 | I 공용 계약과 D 준비/변경 통지 | 독립 가짜 환경 입력과 실환경 통합을 구분. 야간/피신 중 필수 NPC 접근 LW-T05/T06 |
| D 환경 | 조명 최종 작성자·셰이더 기존 채널 의미·프로필 시간표 | global/MPB 잔류·재입장/로드 날씨·문턱 비/먼지·서사 물길 독립 LW-T06/T07/T09 |
| E | B/C/D 부족 자산 명세 | Unity 임포트·근접/원경·충돌·피벗·UV·색공간. 이미지/모델 생성만으로 완료 금지 |
| I 첫 통합 | B 전환/도착 인계, I 등록, R04 지역 실행 제한, 우물/식수 참조 재결선 | 기존 `RedTreeR04ContentTests/FactsTests/ConsentTests`, LW-T01~04. 완료 사실 주입으로 통과 금지 |
| I 전체 이관 | R06/R07/R09 연결·소품 소유·마커/카메라/대역 해석 | 관련 R05~R09 Content/Facts 검사, LW-T10. R05 구조를 경계로 자르지 않았는지 확인 |
| V 최종 | 통합 버전 고정·원본 저장 보존 | [테스트 지침](../../guide/TEST_VALIDATION_GUIDE.md)에 따른 전체 통합 회귀/참조 검사와 Windows Player 실제 두 맵 실행. 새 씬·로딩·셰이더 포함 변경이므로 최종 Player 검증 필요 |

현재 남은 우선 해결 항목:

1. **I/B:** 입력 이동/요청 수락·실패/목적지 발견 계약을 실제 구현하고, 도착 누락·Loading 경유 실패까지 복구할 것.
2. **I/B:** 원본 Runner 공유 시 지역 밖 Resume 효과 차단. R04 엔트, R06 표식 관찰, R09 최종 인계의 생산자를 하나로 유지할 것.
3. **B/I:** 새 지면·도착 안전성, 구 저장 위치 대응과 전체 이동 GUID 목록을 확정할 것. A의 좌표 표/조사 범위는 자동 마이그레이션 데이터가 아님.
4. **C/I:** 일과 보행이 NPC 대화를 잠그지 않는 중단 경로와 Presentation의 서사 배치 우선권을 확보할 것.
5. **D/I:** 환경 공용 입력을 게시하고 시간표 복원·조명 합성을 연결할 것. 수면의 서사 `_Recession`과 날씨를 분리할 것.
6. **C/E/I:** 운반·조리·수선·착석 전환의 실제 자산 적합성, 모건과 현재 R09 적재점의 동선을 확인할 것.
7. **V/I:** 기기/해상도/품질·실제 성능 기준선을 측정할 것. 기존 문서에 남은 Sera 얼굴, 묘묘 의상, R05 구조 시간 초과, Quaternion NaN/Cloth, 정상 후반 완주·R09 왕복 미검증을 이번 A가 해결한 것으로 처리하지 않을 것.

## 10. A 검수 결과와 후속 인계

문서의 현 경로·핵심 GUID·scene fileID·좌표·저장/API 설명을 실제 파일과 대조했다. 새 ID/폴더/계약은 예약·제안·미구현으로 구분했다. 주민 선택·물자 소유·진실 공개 순서·루시아 처우·현재 사용자 배치는 변경하지 않았다.

이번 수정 범위는 `Assets/docs/design/LivingWorld/A_BASELINE.md`, 그 `.meta`, `LivingWorld.meta`뿐이다. 최종 검수에서 본편 씬 SHA-256이 2절의 착수 값과 동일함을 확인했고, git 상태의 추가 변경은 이 문서와 두 `.meta`뿐이었다. 문서의 상대 링크 대상도 모두 존재했다. 본편 씬·런타임 코드·콘텐츠 데이터를 수정하거나 커밋하지 않았다.

Unity 컴파일·EditMode/Play Mode·Player 빌드는 문서 작업에 필요하지 않아 미수행이다. 현장 시야·바닥·모션·실물 게임패드·성능은 후속 구현 검증 대기다. B/C/D는 1절의 독립 범위부터 시작하고, 해결 항목은 각 인계서에서 파일·증거·검사 결과를 붙여 닫는다.
