# 붉은 나무 R04 — 피해 마을과 첫 진실

## 1. 범위와 현재 판정

2026-10-18, Unity 6000.3.21f1 / WindowsEditor / URP / Unity MCP. R01 장면 카드 S03~S05를 실제 `LakeOfLifeScenario`에 연결했다. **본편 구현과 최종 Windows Player 연속 실행·완료 저장 재진입을 확인했다. 전체 EditMode 실패 2건과 사람의 조작·미술 품질 판정은 남아 있어 프로젝트 전체 검증 완료로 판정하지 않는다.** 증거 루트는 `output/RedTreeR04Integration/`이다.

기존 R03 타이틀→생활→전투→화린 영입은 이번에 반복 완주하지 않았다. R03 문서 12절의 이전 Player 결과와 13절의 후속 안정화를 기준 증거로 사용했다. 이전 Player가 이후 타이틀·포커스 수정을 포함한다고 해석하지 않는다. 작업 시작 당시 미커밋 변경 목록은 `status-before.txt`에 보존했다. 커밋하지 않는다.

## 2. 본편 연결과 데이터 소유권

- 본편 씬의 `RedTreeR04` 루트에 피해 마을·개울·상호작용·FlowGraphRunner를 배치했다. 기존 씬과 에셋 GUID를 유지한다.
- `FLOW_RedTreeVillageWaterReport`가 R03의 `lake.red_tree.handoff_acknowledged`를 읽어 Q03을 수락한다. 최초 입장과 플래그 복원 모두 재동기화한다. Q02의 빈 자동 후속 목록을 보존하므로 R03만 읽는 기존 계약도 유지한다.
- Q03 `돌아갈 집`, Q04 `마른 개울을 따라`, Q05 `멈추기 전에`를 실제 QuestDatabase에 등록했다. 퀘스트 보상은 비어 있으며 반복 수락하지 않는다. 완료/활성 퀘스트에 수락 요청을 반복하지 않도록 상태를 확인한다.
- Q05 완료 뒤 `quest_main_red_tree_records_next`의 `오간 기록` 목표를 제공한다. 기록을 기다리는 인계까지만 구현한다. 실제 기록 대조·은폐·표식·후반 영입은 생성하지 않았다.
  - R05 후속 연결: 이 인계 퀘스트의 ID/GUID/기존 이벤트는 유지한다. R05가 활성 인계 목표에 `records_investigation_started`를 전달해 완료하고 Q06으로 이어 준다. 표시 안내만 `리안이 펼친 기록을 살핀다`와 실제 기록대 위치로 갱신했다. 아래 R04 최종 Player 증거의 당시 표시 문구는 변경 전 기록이다.
- 대화 19개와 조사 관찰 카메라 2개는 `Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/RedTree/R04/`에 있다. 대화 정상 종료만 사실을 확정하며 취소는 완료로 소비하지 않는다.

## 3. 장면별 구현 결정

### S03 — 돌아갈 곳

R03 인계점 서쪽의 기존 지형에 두 거처·우물·짐·씨앗 자루·시든 밭과 생존자 둘을 모았다. 기존 환경 프리팹과 R03의 일반 주민 표현을 재사용했다. 두 인물은 `남은 주민`, `짐을 꾸리는 주민`으로만 부르고 새로운 이름·관계·피해 인원·사망자를 확정하지 않는다.

우물·거처·밭은 가까이 가서 조사 입력을 해야 하며 생존자 발견까지 모여야 E01이 된다. 남겠다는 주민과 호숫가에 정착하겠다는 주민의 말을 각각 듣는다. 플레이어 선택지가 주민의 결정을 대신하지 않는다. 식수 인계는 실제 물통 배치 변화, 떠날 준비는 주민의 실제 이동과 짐 변화로 처리한다. `NpcLifeInteractable`의 기존 실행 계약을 사용하며 대화에서 인계 사실을 만들지 않는다.

E01·두 주민 의사·식수 인계·짐 준비·R03 인계가 모두 있어야 릴리의 동행 의사가 열린다. 정상 동의 후 실제 보유 명단을 확인해 사건을 확정한다. 이미 보유한 릴리는 별도 동의 대사를 사용한다.

### S04 — 마른 개울

물 고임·뿌리 접점 E02와 조우 뒤 연속 흔적 E03은 별도 조사 입력이다. 지형을 따르는 물길과 뿌리, 뿌리 주변 식생과 바깥의 마른 작물을 배치했다. 조사 시 6초 관찰 구도로 실제 접점/연속 방향을 보여주며 대사 패널을 내려 증거를 가리지 않는다. 관찰 중 취소하면 해당 사실을 확정하지 않는다.

검증 가능한 기존 `Ent_Normal`의 프리팹·BT·GAS·Motion을 공유하는 별도 정의 `RedTree_CreekEnt` 한 개를 등록했다. Dryad와 Training Dummy는 사용하지 않는다. 원본 적 밸런스나 공격 Motion을 임의 변경하지 않는다. KCC는 물리 Trigger 콜백이 보장되지 않아 기존 `WaitPlayerInVolumeNode`가 실제 접근을 확인한다. 단일 실행 Gate와 저장 복원 경로로 조우 중복을 막는다. 전투 해결은 실제 적 관측과 패배 대기 뒤에만 기록한다.

E01·E02·E03·릴리 사건·조우 해결 후 설아와 T1을 확인한다. 여기서 공개하는 내용은 **호숫가 풍요가 주변 물과 양분을 가져온 결과**뿐이다. 적을 신의 명령으로 움직인다고 단정하지 않는다. `truth.cost`와 `seola_consent`는 별도 결과다. 진실 확인 직후 합류가 실패/취소되어도 진실은 남고 별도 설아 합류점에서 재시도한다.

### S05 — 돌아온 보고

설아와 루시아에게 보고하고, 루시아의 “그런 줄은 몰랐어요”를 재생한다. 이 말의 진위와 사전 인지는 확정하지 않는다. 조이에게 실제 지원 업무를 배정하고 지원용 물통을 구분한다. 마을에서 쓸 몫과 밖에 보낼 몫을 별도로 조사한다. `support_assigned`는 배송 완료가 아니다. 실제 배송 완료 사실이나 보상을 만들지 않는다.

세라는 당장 여기 사람들의 식사를 근거로 반대한다. 지원 자체를 막는 적으로 만들거나 전투 대상으로 등록하지 않는다. 공급 대안 완성·세라 동의·세라 영입은 R04 결과가 아니다. 기존 Sera 모델 메시 22개에 BlendShape가 없는 상태를 다시 확인했다. 없는 표정 채널을 연결하지 않았으며 이 장면에서 자동 깜빡임을 요구하지 않는다.

## 4. 진행과 저장 경계

| 구분 | 사실 |
|---|---|
| 피해 관찰 | `well_examined`, `home_examined`, `field_examined`, `survivors_found` → `evidence.village_loss` |
| 주민 의사 | `resident_stays`, `resident_leaves` → `resident_choices` |
| 실제 행동 | `water_handed_over`, `departure_prepared` |
| 릴리 합류 | `lili_ready` → 정상 동의·실제 명단 → `lili_consent` |
| 물길 관찰/전투 | `evidence.root_draw`, `creek_started`, `creek_cleared`, `evidence.lake_path` |
| 진실/설아 | `truth_ready` → `truth.cost` → 별도 정상 동의·명단 → `seola_consent` |
| 보고/업무 | `report_completed`, `support_assigned` |
| 공급 확인 | `local_supply_checked`, `outbound_supply_checked`, `supply_need_confirmed` |
| 다음 목표 | `records_goal_acknowledged` → 기록 조사 인계 퀘스트 |

모든 키의 접두사는 `lake.red_tree.`다. 관찰은 퀘스트 수락과 무관하게 누적하고 이후 수락 때 목표에 다시 전달한다. 한 번 본 것만으로 결론·인계·영입을 대신하지 않는다. 저장 복원은 이전 실행 컨텍스트를 취소한 뒤 사실과 퀘스트를 동기화한다. 최초 주인공은 기존 Party의 서사 화자 계약을 사용하며 현재 조작 캐릭터로 바꾸지 않는다.

## 5. 실행 결과와 중간 실패

| 검사 | 현재 결과 | 증거 |
|---|---|---|
| 관련 EditMode | 6/6 통과. 이후 데이터 보완은 최종 검사에서 재확인 | `edit-focused.json` |
| 관련 Play Mode | 10/10 통과 | `play-focused.json` |
| Play Mode 최초 시도 | 테스트 0개 실행, 초기화 120초 초과. 도메인 재로드 격리 후 재시도 통과 | job `167de6d418a34ccfb021b8a1a892fcb9` |
| 전체 Play Mode | 32/32 통과, 14.22초. 보유 릴리·설아 취소/재시도와 늦은 콜백 검사 포함 | `all-play-final.json` |
| 라온 본편 연속 진행 | R03 완료 복사본에서 S03→S04→S05, 릴리 취소/재동의, 설아 동의 취소/재시도, 교체, 새 슬롯 재진입 통과. 오류 0 | 아래 실행 사슬 |
| 아린 차이/선행 조사 | 선행 조사 저장에서 인계 재시도→Q03 즉시 수락·이전 목표 반영→E02 통과. Arin 화자/Hwarin 조작, 오류 0 | `Runs/Arin-20261018_152757/result.json` |
| 전체 EditMode | 939개 중 937 통과·2 실패·0 제외 | `all-edit-final.json`, job `38896592002041ecba34a8e32cec79c3` |
| 최종 R04 데이터 검사 | 6/6 통과 | `edit-focused-final.json` |
| 영입 준비 실패 추가 검사 | 1/1 통과. 진실 보존·재시도 경계 | `play-consent-failure.json` |
| 인계 순서/씬 전환 회귀 | 6/6 통과 | `play-quest-order-final.json` |
| 실제 사망/로드 | 실제 엔트 공격으로 사망→미처치 유지→복사 슬롯 로드→적 1개 재개, 오류 0 | `Runs/Raon-20261018_152929/result.json`, `actual-death.png` |
| Windows Player | 최종 빌드 오류 0·경고 20. R03 완료 복사→R04 전 구간→슬롯6 재진입 통과, 실행 오류 0 | `player-build-final.json`, `Runs/Raon-20261018_161101/result.json`, `player-final.log` |
| 모델 준비 전 요청 | 실제 PartyManager의 교체 거부·명단 보존 1/1 통과 | `play-model-not-ready.json` |
| Player 임대/모델 | 재진입 전후 임대 23→23, 키/소유자 변화 0. 상주 모델 5→5, 활성 모델 1·PlayerActor 1 | `player-model-lease-audit.json` |
| 실물 패드·전투 감각 | 미수행 | 자동 Gamepad 입력/캡처와 구분 |

라온 실행 사슬은 다음과 같다. 모두 같은 플레이에서 실제로 생성한 저장을 복사해 재개했으며 사실·HP·처치·영입을 주입하지 않았다.

1. `Runs/Raon-20261018_144740`: 원본 R03 `PlayerRuns/Raon-20261018_123309/Saves/save_slot_3.sav`를 새 폴더 슬롯0에 복사. 우물·거처·밭·생존자·주민 의사·식수·짐·릴리 취소와 정상 합류, 합류 즉시 슬롯2 저장·릴리 교체 통과. 뿌리 콜라이더에서 이동 정체 실패.
2. `Runs/Raon-20261018_145120`: 위 슬롯2 복사. 통행 보완 뒤 E02 조사 통과, 조우 직전 실제 상태를 슬롯3에 저장. 자동 입력의 접근 위치가 볼륨 바깥이었던 문제를 수정했다.
3. `Runs/Raon-20261018_145453`: 접근은 했지만 물리 Trigger 콜백에 의존해 조우가 시작되지 않았다. KCC의 위치 기반 진입 노드로 수정했다.
4. `Runs/Raon-20261018_145629`: 위 E02 저장에서 실제 엔트 전투 해결(약 11초), E03, T1, 설아 취소/재동의·영입, 설아 조작으로 보고·지원·세라·다음 목표 진행. 슬롯6 저장→본편 씬 재진입 통과. 실제 명단은 Raon/Hwarin/Lili/SeolA, 서사 주인공 Raon, 조작 SeolA, 남은 대역 0, 기록 인계 퀘스트 Active, 오류 0.

화면 확인에서 뿌리 충돌 외에 물/흙 표면의 경사 지형 관통, 시든 작물의 불투명 카드, 물 접점 앞 나무 가림을 발견해 수정했다. 근거는 `creek-approach-live.png`, `pool-framing-review.png`, 수정 후 `pool-surface-fixed.png`, `water-readable.png`다. 최종 관찰 카메라 실제 재생·캡처 결과는 7절에 기록했다. 단순 파일 존재를 화면 통과로 세지 않는다.

아린은 R03 원본 `PlayerRuns/Arin-20261018_123546/Saves/save_slot_2.sav`를 읽어 화린 영입 완료·릴리 인계 전·주인공 Arin을 확인했다. 새 복사 폴더에서 선행 피해 조사→기존 릴리 인계→Q03 누적과 화린 조작 중 아린 화자를 검사한다. R03 공통 구간 및 R04 공통 후반 전체를 반복하지 않는다.

수정 후 아린은 이미 실제 조사를 마친 `Arin-20261018_151446/Saves/save_slot_1.sav`의 복사본에서 재개했다. `Arin-20261018_152757`은 정상 인계 뒤 Q03 Active와 선행 네 목표 반영, E02 별도 조사, T1/릴리 미확정을 확인했고 오류 0으로 끝났다. R03 인계 대사의 마지막 안내만 피해 마을의 식수 지원 방향으로 구체화했으며 대화 ID/GUID/완료 조건을 바꾸지 않았다.

## 6. 남은 판정과 정리

전체 EditMode의 현재 실패는 Sera 얼굴 바인딩 1개와 기존 `LakeNarrativePlaybackTests.라온의_무언행동과_결말이_실제_맵에서_완료된다` 1개다. 후자는 기존 `Ingame/LakeOfLife`의 `Story/Dialogue/DLG_Lake_Chapter1Return` 무언 행동 취소 판정이며 이번 실행에서 새로 관측했다. R04 그래프나 대화는 사용하지 않으며 원인 관계는 확정하지 않았다. 기존 3개 실패로 묶거나 새 회귀가 없다고 표현하지 않는다. Dryad/Training Dummy 검사 2개는 현재 작업 트리의 기존 대기 공격 정책에서 통과했다. 이번 작업에서 Motion을 연결하거나 assertion을 완화한 결과가 아니다.

아린 선행 조사 첫 실행(`Arin-20261018_151446`)에서 인계 플래그가 Q02 완료보다 먼저 바뀌어 Q03이 Available에 남았다. `OnQuestCompletedEntryNode`로 Q02~Q05 완료 시 사실을 재동기화한다. 첫 보완 실행(`Arin-20261018_152337`)에서 씬 전환 정리가 Scene 범위 구독을 지우는 문제도 확인했다. 기존 상호작용 구독과 동일하게 Global 범위 + 러너 종료 시 명시 해제로 수정했고, 회귀 검사에 씬 전환 정리를 포함했다. 중간 컴파일에서 enum namespace 누락과 테스트 asmdef의 구체 매니저 참조 오류가 있었고, using 및 인터페이스/리플렉션 격리 후 컴파일·관련 검사가 통과했다.

씬 diff 검사에서 기존 SceneEntityId 68개가 과거 씬 ownerKey 때문에 자동 재생성된 사실을 발견했다. 작업 시작 시 깨끗했던 씬 원본의 68개 GUID를 복구하고 ownerKey만 현재 씬으로 맞췄다. 신규 주민의 ID는 별도 유지한다. 사용자 데이터의 GUID를 새로 부여한 결과를 그대로 저장하지 않는다.

`scene-id-audit.json`에서 기존 식별자 68/68 보존을 재확인했다. `source-save-audit.json`은 라온 완료 슬롯3, 아린 완료 슬롯3, 아린 인계 전 슬롯2를 시작 시 읽어 둔 평문과 대조해 3/3 동일함을 확인한 결과다. 원본 파일의 해시는 이 세 저장에만 기록했으며 전체 프로젝트 해시 확보를 작업 조건으로 삼지 않았다. 실제 결과는 별도 실행 폴더의 빈 슬롯에 저장했다.

첫 Windows 빌드 `build-b05b62852d`는 Addressables 재생성 포함 1,510.32초, 7,571.19MB, 오류 0·경고 518로 성공했다(`player-build-first.json`, GUID `30e926a7b86f423f8830ffabca1b6f2d`). 빌드 중 MCP 상태 조회 시간 초과/중복 요청 경고는 빌드 실패와 구분한다. Player 첫 시작에서는 검사기가 Title 씬 활성화 후 고정 2초만 기다려 아직 진행 중인 전환과 충돌했다. 저장 로드 요청이 무시되어 R04는 시작하지 못했다. `player-start-race.log`, `player-isolated-desktop.log`, `player-start-ui-blocked.log`는 통과 실행이 아니다. 검사 시작 조건을 `SceneManager.IsLoading == false`로 고쳤고 게임 진행 사실이나 생산 코드를 바꾸지 않았다.

컴퓨터 사용 도구의 UPlayground 화면 접근은 자동 승인 검토에서 허용되지 않았다. 해당 경로를 중단하고 Unity MCP와 검사기의 실제 입력·ScreenCapture 경로로 검증했다. 첫 Player들은 각각 종료한 뒤 다음을 실행했으며 동시 Player는 없다. 최종 빌드는 동일 콘텐츠/셰이더 캐시를 사용하고 검사기 시작 조건과 스킵·관찰 취소 검사만 보완했다.

- 최종 Player·아린 차이·추가 영입 준비 실패·모델 준비 전 교체 검사를 수행했다. 최종 컴파일과 콘솔 오류는 0이며 검사별 결과는 위 표와 아래 최종 기록을 따른다.
- 미확정 Dryad/Training Dummy Motion을 R04 근거로 사용하지 않는다. 작업 시작 전부터 있던 사용자 변경(대기 공격 제외 정책 포함)을 보존했고 이번 구현에서 assertion을 완화하지 않았다. 과거 17개 실패나 R03의 3개 실패를 현재 실패로 복사하지 않고 최종 실행 결과로 분류한다.
- 사람의 판단이 남는 항목은 실물 패드 반응, 전투 리스크/리워드와 카메라 편안함, 마을/식생 변화의 미술 완성도, Sera 얼굴 채널이다. 현재 모델에 없는 얼굴 표정 구현을 통과로 보고하지 않는다.
- 검사용 `RedTreeR04Probe.cs`의 원본은 output에 남기고 Assets의 임시 복사본과 meta만 제거했다. 기존 `UnityMcpSessionSetup.cs` 등 사용자 파일은 보존했다. 원본 저장은 읽기/복사만 수행했다.

## 7. 최종 Player·화면·정리 기록

최종 빌드는 `build-79994398cd`, GUID `06120f9e80a048d3bf5205948978236e`, Unity 6000.3.21f1 StandaloneWindows64 Development/URP다. 2026-10-18 07:02:31~07:09:36 UTC, 425.18초, 7,571.19MB, 오류 0·경고 20. `Player/UPlayground.exe`, `player-build-final.json`, `build-messages-final.txt`에 식별과 경고를 보존했다. 경고는 lilToon 패키지 생성 변경 및 기존 컴파일 경고 등이며 무경고 빌드로 표현하지 않는다. 이 빌드에는 현재 작업 트리의 R03 후속 안정화도 포함되지만 R03 타이틀부터 다시 완주한 결과는 아니다.

`Runs/Raon-20261018_161101`은 R03 라온 완료 슬롯3를 격리된 빈 슬롯0에 복사하여 시작했다. 07:11:22~07:14:36 UTC에 실제 이동·조사·NPC 행동·전투·동의·보고·슬롯6 저장 재진입을 수행했고 `result.json`은 passed=true, errors=[]다. 거처 조사의 실제 스킵 버튼, 물 접점 관찰 중 Cancel 후 미완료 및 재조사, 릴리/설아 취소 후 재동의까지 포함한다. 엔트 전투는 약 9.3초였다. 플래그·HP·처치·영입 주입은 하지 않았다. `player-final.log`, 각 `dialogue-*.png`, `reload-complete.png`를 함께 보관했다.

완료 재진입 화면에서 활성 설아와 네 명의 파티, 다음 목표 `오간 기록 / 루시아가 내줄 기록을 기다린다`를 확인했다. 대화와 세라 반대 이후 목표 화면의 글자가 가리지 않고 정상 입력으로 다음 행동에 복귀했다. `player-model-lease-audit.json`에서 임대 23→23, 키/소유자 변화 0, 상주 모델 5→5 중 활성 1, PlayerActor 1을 확인했다. 임대 23개는 모델/정의 10개와 설정된 PreviewPrefetch 13개이며 플레이 중 임대 0을 기대하지 않는다.

Player 검사기는 같은 프레임에 두 번 ScreenCapture를 요청해 초기 관찰 캡처가 대화 파일로 덮어써졌다. 콘텐츠 변경 없이 Editor 검사기의 캡처 분기만 고쳐 실제 플레이에서 만든 저장을 복사해 물 접점(`Raon-20261018_161646`)과 연속 흔적(`Raon-20261018_161756`)만 재생했다. 두 실행은 통과·오류 0이며 각각 `R04_pool-observe-2.png`, `R04_roots-observe-2.png`를 직접 열어 확인했다. 접점·뿌리 방향·주변 식생이 보이고 대화 패널은 내려간다. 물 표면의 직선 경계와 반복 배치는 미술 보완 대상이다. Player용 검사기 원본은 `RedTreeR04Probe.player.cs`, 마지막 Editor 캡처용 원본은 `RedTreeR04Probe.cs`다. 추가 관찰 화면 증거를 Player 캡처로 표시하지 않는다.

정리 시 Assets의 임시 검사 소스/meta를 제거하고 Addressables/BuildSettings를 착수 전 복사본으로, EditorSettings를 착수 당시 깨끗했던 HEAD 내용으로 복원했다. git restore는 다른 index.lock 때문에 실행되지 않아 잠금을 건드리지 않고 `git show` 내용으로 해당 설정 파일만 복원했다. TimeManager/lilToonSetting 및 기존 사용자 변경은 보존했다. 잘못된 Boot 경로로 복귀를 시도한 MCP 호출 1회는 도구 오류였으며 실제 `GameLogic/Boot.unity`로 수정해 복귀했다. 최종 상태·컴파일·콘솔·설정 비교는 `cleanup-final.json`, `unity-final.json`, `console-final.json`에 기록한다. 기존 UnityMcpSessionSetup.cs는 유지하고 커밋하지 않았다.
