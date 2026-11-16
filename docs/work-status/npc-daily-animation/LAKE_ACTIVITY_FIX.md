# LakeVillageWorld 생활 행동·식탁 제보

기준: 2026-11-15. [NPC 위키](../../wiki/npc.md), [생활 동작 가이드](../../../Assets/docs/guide/NPC_DAILY_ANIMATION_AUTHORING_GUIDE.md).

## 사용자 스크린샷 재제보 — 약초 작업대 실제 대상 수정

사용자 추가 제보: 새 스크린샷과 “?”. **이전 식탁 8개 보정은 사진 속 작업대를 고친 결과가 아니었다. BUG-LAKE-ACTIVITY-01의 대상 식별·시각 검수가 잘못됐음을 정정한다.**

- 실제 대상: (1004,62.42,408)의 `Hazel_Worktable_R2`, `Hazel_Worktable_Collision`, `Hazel_Bottle_R2`·`Hazel_Jar_R2`·`RedTree_Hazel_Bottle`·`Sodam_HerbTray_R2`. 헤이즐·소담의 정규 모델과 같은 배치를 화면에서 직접 대조했다. [수정 전 실제 대상](../../../output/NpcDailyAnimations/FloatingTrays/before-care-table.png).
- 원인: 시각 작업대는 `Dng_Table_Set.fbx`로 교체·축소됐으나 원래 충돌체는 높이 약 0.86m, 폭 1.0m, 깊이 1.68m를 유지했다. 시각 작업면이 약 30cm 낮고 좁아 원래 소품이 공중·상판 밖에 남았다. 병·단지·약초 그릇도 `Prop_JarPlatform.fbx`를 축소한 모델로 바뀌어 납작한 음식 쟁반처럼 보였다.
- 수정: 원래 충돌체를 유지하고 기존 FarmLand 목재 작업대를 그 실제 범위에 맞췄다. 잘못된 항아리 세트 네 곳은 Toon Golden Valley의 개별 병·단지·그릇으로 교체했다. 기존 기능 루트·참조·진행 ID·NPC 배치/모션/캡슐은 유지했다. 약초 잎은 실제 그릇 위로 정렬했다.
- 각 병·단지·그릇 바로 아래 실제 메시를 Raycast하여 간격 3mm를 확인했다(작업면 y=63.2682~63.2786). 전체 SceneEntityId 전후 동일. [접촉 검사](../../../output/NpcDailyAnimations/FloatingTrays/applied.txt), [이전 대상 상태·메시 GUID/local ID](../../../output/NpcDailyAnimations/FloatingTrays/before-targets.txt), [수정 후 근접 화면](../../../output/NpcDailyAnimations/FloatingTrays/after-care-table-close.png).
- 원본 재사용·증분 재현 입력: [repair-lake-care-workstation.csx](../../../Tools/OpenWorld/repair-lake-care-workstation.csx). 구매 원본·모델 파일·코드·NPC 행동·전체 월드 생성기는 수정하지 않았다. 씬의 해당 시각 연결만 저장했다. 새 Player/Addressables 빌드나 C# 컴파일은 필요하지 않아 실행하지 않았다.
- 현재 판정: **대상 식별·저장 반영·네 소품의 정적 접촉 검사·Unity 근접 화면 확인**. 재생 요청과 플레이어 근접 이동은 응답받았지만 캡처 후 상태는 편집 모드(time=0, 두 NPC motion=none)였다. `play-care-table.png`는 이름과 달리 실제 일과 재생의 통과 근거가 아니다. [상태 원본](../../../output/NpcDailyAnimations/FloatingTrays/play-status.txt). 최종 NPC 작업 접촉·사용자 미술 확인은 별도로 남긴다. 후속 촬영·재생 요청 이후 현재 씬 dirty=true를 확인했으나 추가 저장하지 않아 미저장 상태를 보존했다. 시각 수정 자체는 앞선 적용 단계에서 저장 완료했다.
- 이번 수정의 이전 상태는 해당 기능 루트 5개의 Transform·메시 GUID/local ID·머티리얼 경로만 소형 기록으로 보존했다. 기존 씬 사본·사용자 변경·원본 자산은 유지했고 새 전체 씬 사본은 만들지 않았다. 검사 임시 MeshCollider 오브젝트는 제거했다. [준비표·검수 기록](../../../output/harness/20261115-163359-d7ca856d/review.md).

## 사용자 지적에 따른 전수 확대

추가 원문: “피난민 2명 뿐만이 아니라 멈춰있는 NPC들 많잖아”. 이전 두 명 수정은 요청 범위를 충분히 다루지 못했다. 현재 조사 범위는 상주 LakeVillageWorld 50명, 연결 DroughtVillageRegion 9명, SaltwindHarborRegion 10명, StonegateCitadelRegion 14명으로 총 83명이다. 이야기 진행 후 활성화되는 대역도 포함한다. [실제 연결 조사](../../../output/NpcDailyAnimations/WorldActivityPass/all-before.tsv).

- [83명 행동 계획](../../../Tools/OpenWorld/lake-resident-activities-plan.json): 기존 보행·작업·실행 가능한 일과 25명 유지, 주변 점검·생활 보행 보완 52명, 매대 상품 정리 3명, 사건 전후 대기/착석을 구분하는 3명. 준비 게이트가 닫힌 가뭄 마을의 두 일과는 임의 활성화하지 않고 기존 Brain 보행 경로로 보완한다.
- 모든 NPC의 몸·위치·회전·Actor ID·SceneEntityId·조건부 활성 정책을 보존한다. 런타임 이름 분기나 전체 월드 재생성은 추가하지 않는다. 이름은 GUID가 없는 배치의 저작 식별에만 쓰며, GUID가 지정되면 이름 폴백을 하지 않는다.
- 식사·진료·거처 인계 후 기존 착석이 보행으로 덮이지 않게 `NpcLifePresentation`의 사건 전후 이동 설정을 선택적으로 추가했다. 기본값은 기존 이동 설정 유지다. 실제 이동 중심은 기존 `SetHomePose`가 복원한 새 장소를 사용한다.
- **전수 적용 반영**: 83명 중 58명 설정 보완, 25명 기존 행동 유지. 실제 로드된 연결 검사에서 `보행 꺼짐 + 작업 없음 + 준비된 일과 없음` 조합 0명을 확인했다. 보행 67 / 작업 모션 12 / 준비된 일과 7은 서로 겹치는 분류다. [적용 대장](../../../output/NpcDailyAnimations/WorldActivityPass/applied.tsv), [적용 후 연결](../../../output/NpcDailyAnimations/WorldActivityPass/after.tsv), [검사 요약](../../../output/NpcDailyAnimations/WorldActivityPass/validation.txt).
- 기존 NPC 몸·위치·회전·Actor ID·SceneEntityId 전후 동일. 이야기 후반 대역의 활성 조건·완료 플래그는 수정하지 않았다. [식별·배치 근거](../../../output/NpcDailyAnimations/WorldActivityPass/identities.txt). [증분 적용 입력](../../../Tools/OpenWorld/apply-lake-resident-activities.csx).
- 상점 장부·천·채소를 정리하는 세 곳에는 기존 FarmLand 작업대와 상품을 붙였다. 고정 작업자의 캡슐 반경은 기존 수선 작업자와 같은 0.2m로 맞췄다. 새 모델 제작·전체 내비게이션 재베이크·구매 원본 수정은 없다. [실제 상점 작업 화면](../../../output/NpcDailyAnimations/WorldActivityPass/merchant-work.png).
- Unity 정상 컴파일 및 이번 Play Console 오류 0. 기존 대기 EditMode 17/17, 사건 전후 이동 신규 EditMode 3/3 통과(실패·제외 0). 최초 필터 실행에서는 신규 파일이 아직 임포트되지 않아 기존 17개만 실행됐으며, 전체 Refresh·신규 파일 명시 임포트 후 신규 3개를 따로 실행해 누락을 해소했다. [원본 결과](../../../output/NpcDailyAnimations/WorldActivityPass/editmode-results.json).
- 실제 Play에서 활성 NPC 37명(생성 주민 8명 포함)을 표본 관찰했다. 23명의 수평 위치 변화가 0.5m 이상이며, 세라·연화·리안·묘령·유라·대기 주민·길목 주민·식사 대기자·온유·안내자 등의 이동이 포함된다. 생성 주민 8명 모두 보행 설정이 켜져 있었다. [관찰 전](../../../output/NpcDailyAnimations/WorldActivityPass/live-before.tsv), [관찰 후](../../../output/NpcDailyAnimations/WorldActivityPass/live-after.tsv), [이동량](../../../output/NpcDailyAnimations/WorldActivityPass/movement.csv), [같은 카메라 전](../../../output/NpcDailyAnimations/WorldActivityPass/lake-life-start.png)·[후](../../../output/NpcDailyAnimations/WorldActivityPass/lake-life-after.png). NPC 위치는 주입하지 않았고 플레이어만 마을 인근으로 이동했다.
- **남은 검증**: 나머지 지역·후반 대역의 실제 보행·업무 접촉, 전체 이야기 진행/배식 후 착석/저장 로드·대화 UI·군중 최종 품질은 사용자 테스트 대기다. 83명 데이터 연결 검사나 23명 이동 표본을 전체 미술·플레이 인수로 계산하지 않는다.
- 준비 중 미저장 연결 씬을 저장하고 닫는 정리 요청은 자동 승인 검토에서 거절됐다(기존 사용자 변경을 함께 저장할 수 있다는 이유). 해당 저장·닫기는 수행하지 않았다. 이후 열린 상태를 보존한 채 Play 검증을 진행하고 편집 모드로 종료했다. 종료 후 4개 씬 모두 dirty=false를 실제 확인하고, 저장 호출 없이 깨끗한 조사용 연결 씬 3개만 닫아 원래 LakeVillageWorld 한 씬 구성으로 복원했다. 요청하지 않은 커밋·Player/Addressables 빌드 없음.
- 새 작업 전 씬 사본 4개 총 377,472,269바이트는 Git 추적 밖 사용자 현장 변경을 포함하므로 [백업](../../../output/NpcDailyAnimations/WorldActivityPass/backup/)에 보존한다. 코드·검사 meta, 개체별 계획, 적용·검사 근거도 보존한다. 임시 플레이 관찰 콜백은 제한 시간/Play 종료 시 해제되며 새 패키지·빌드 캐시는 없다.

최종 [변경 준비표·검수 기록](../../../output/harness/20261115-161120-e951e534/review.md). 해시는 지정 파일 식별용이며 미술·실제 플레이 전체 통과 증거가 아니다.

### 전수 변경 사용자 확인

기존 LF01~04와 함께 별도 테스트 저장 슬롯의 Editor Play에서 확인한다. 결과는 `항목 ID / 통과·실패·미수행 / 실제 결과 / 화면·로그`로 전달한다.

| ID | 준비·절차 | 기대 결과·실패 기준 |
|---|---|---|
| WA01 | 호수 마을 (1048,62,450), 장터 (1060,60,432), 피난민 구역 (993,62,471) 주변에서 각 30초 관찰 | 주변 주민이 서로 다른 시점에 이동·점검·작업. 제자리 걷기·길막·모두 동시 반복·허공 작업이면 실패 |
| WA02 | 가뭄 마을 (950,71,483), 항구 (1928,50,-300), 성채 (580,62,120) 방문 | 역할별 이동·매대 작업·기존 일과 유지. 성문 순찰이 출입을 막거나 상점 주인이 자리를 떠나면 실패 |
| WA03 | 기존 이야기 경로로 식사/처치 자리/거처 인계를 진행하고 완료 전후 테스트 저장을 각각 로드 | 대기 중 행동과 완료 후 기존 착석 복원, 기존 대화·진행 조건 유지. 착석 직후 다시 걷거나 이전 장소로 돌아가면 실패 |
| WA04 | 기존 R07/R09 이야기 진행 후 작업 구역과 동료 대역 확인 | 해당 단계의 몸만 표시되고 주변 이동·기존 지원 행동 유지. 동료 중복·숨김 실패·진행 불능이면 실패 |

## 최초 식탁·피난민 수정 이력

사용자 원문: “이 NPC 들의 행동이 뭔지 모르겠음. 그리고 배치물들이 식탁이 아니라 공중에 떠있음. 고쳐줘. LakeVilieageWorld”, “추가로 그냥 제자리에 같이 서있기만 하는 NPC들은 의미가 없음. 개선 필요해.” 첨부 이미지 2장.

- BUG-LAKE-ACTIVITY-01 / S3 / 식탁 위 소품 접촉 불량. 사용자 화면에서 확인. 빈도·회귀·저장 슬롯·입력 장치는 모름. 원래 상판 위에 놓여야 하나 공중에 표시됨.
- FEEL-LAKE-ACTIVITY-02 / 행동의 목적이 불명확하고 두 주민이 제자리에 서 있음. 생활 연출·배치 개선 대상.
- 조사: R03/R05/R06 식탁의 메시가 ResourceOverhaul에서 벤치 포함 모델로 정규화되어 교체됨. 기존 소품 좌표는 유지되어 실제 상판과 불일치할 수 있음. 피난민 RedTree_RefugeeA/B는 `_enableWander=false`, `_workMotion=null`, NpcRoutine 없음. 스크린샷 인물의 정확한 식별은 외형 대조 중이며 이름만으로 동일 인물이라고 확정하지 않음.
- 현재 판정: **구현 반영 / 저장본 연결 검사 통과 / 전체 동작·최종 미술 미완료 / 사용자 테스트 대기**.

## 저장한 수정

- R03/R05/R06의 식사 그릇·식사량 표시·문서·공유 기록·배달 목록 총 8개를 실제 식탁 메시의 위쪽 면에 맞췄다. 외형 전체 bounds에는 컵·촛대가 있어 상판 높이로 사용하지 않았다. 식사 그릇과 공유 기록물은 이전에 약 12cm 떠 있었다. 보정 간격은 3mm다.
- 기존 두 피난민 몸과 SceneEntityId를 유지하고 `NpcRoutine`을 연결했다. 서로 다른 체류 시간으로 `짐 정리 → 다른 짐 확인 → 짧은 휴식`을 반복한다. 실제 짐 4개와 작업점 6개를 추가했다. 보유 DeparturePack/CargoInspect 모션을 사용하며 신규 애니메이션 제작으로 계산하지 않는다.
- 첫 Play 확인에서 짐 콜라이더가 작업점 접근을 막는 결함을 발견했다. 두 번째 적용에서 장식 짐 충돌을 해제하고 도착 오차를 4cm로 보정해 저장했다.
- 원본/증분 저작 입력은 [repair-lake-npc-activities.csx](../../../Tools/OpenWorld/repair-lake-npc-activities.csx)다. 같은 입력을 재적용했을 때 짐·작업점·일과를 중복 추가하지 않는다. 기존 전체 월드 생성기는 현재 여러 미술 변경을 포함하지 않으므로 실행하지 않는다.

## 검사와 한계

- 실제 Unity에서 식탁 메시를 임시 MeshCollider로 측정하고 8개 모두 3mm 간격으로 저장했다. SceneEntityId 목록 변경 0을 적용 전후 비교했다. 두 차례 적용 로그는 [applied.txt](../../../output/NpcDailyAnimations/LakeFix/applied.txt)에 남겼다. 이 파일은 최신 재적용 결과다.
- 첫 Play에서 실제 피난민 몸·의상·짐 배치를 확인했다. [첫 화면](../../../output/NpcDailyAnimations/LakeFix/camp-play-01.png), [충돌 보정 중 화면](../../../output/NpcDailyAnimations/LakeFix/camp-play-02.png)은 **최종 동작 통과 증거가 아니다**. 플레이어가 멀어 simulation이 Suspended인 상태도 확인됐다. NPC를 강제로 정상 작동했다고 보고하지 않는다.
- 최종 보정 후 플레이어만 KCC로 근처에 이동시켰다. 두 주민 모두 실제 작업점에 자연 도착하여 `IsWorking=true`, `lake.camp.0.pack` / `lake.camp.1.pack` 모션 재생을 확인했다. [최종 작업 화면](../../../output/NpcDailyAnimations/LakeFix/camp-final-01.png), [실제 의상·짐 근접](../../../output/NpcDailyAnimations/LakeFix/camp-final-close.png), [상판 보정 화면](../../../output/NpcDailyAnimations/LakeFix/table-final.png). 단일 화면을 반복·전환 품질 통과로 확대하지 않는다.
- [저장본 검사](../../../output/NpcDailyAnimations/LakeFix/saved-validation.txt): 일과 2개, 작업점 6개, 몸·단계 참조 유효, 도착 오차 4cm, 장식 짐 충돌 0, 해당 루트 Missing Script 0. 두 번 적용 후 중복 없음. 마지막 Editor 상태는 Edit Mode, 컴파일 중 아님, LakeVillageWorld dirty=false다.
- 추가 [짧은 관찰](../../../output/NpcDailyAnimations/LakeFix/routine-observation.tsv)은 20.151초·78행에서 Play가 종료됐다. 기록 중 작업 예약이 없어 전체 왕복 검증은 **미수행**으로 남긴다. 관찰기 최초 상태 파일의 음수 경과 시간은 Play 종료로 Time.time이 초기화된 기록 결함이며 통과 근거로 사용하지 않는다. [관찰 입력](../../../Tools/OpenWorld/verify-lake-npc-activities.csx)은 마지막 관측 경과 시간과 종료 원인을 쓰도록 수정했다. 추가 Play 검사는 재실행하지 않았다.
- MCP의 연결 종료·응답 시간 초과·일시적인 instance 0건이 반복됐다. Play 상태·플레이어 이동·Console 확인 요청 일부의 완료를 확인할 수 없었다. Editor 재시작이나 두 번째 Editor 실행은 하지 않았다.
- 연결 복구 후 Console에서 `[RuntimePlacementLoader] ActorDatabase 준비 시간 초과로 지역 배치를 중단했습니다.` 1건을 확인했다. 기존 위키의 알려진 지역 배치 시간 초과와 같은 종류지만 이번 원인이 같다고 단정하지 않는다. 전체 NPC 배치 성공이나 Console 오류 0으로 보고하지 않는다. 이번 작업 범위 밖의 별도 결함이다.
- C# 런타임·Editor 스크립트·MotionSet 소스는 이번 수정에서 변경하지 않았다. 작업 중 별도로 나타난 `LW_CargoInspect.asset` 변경은 다른 기존 작업 변경으로 보고 보존했으며 이번 작업의 변경으로 계산하지 않는다.
- 남은 확인: 최종 revision의 자연 접근·왕복·3회 반복·짐과 손 접촉·대화 중단/재개·저장 왕복. 자동 수치 검사를 최종 미술 인수로 대체하지 않는다.

## 사용자 확인 절차

대상: `Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity`, 기존 테스트 저장 사용. 저장을 덮어쓰지 않고 Editor Play에서 확인한다.

| ID | 위치와 절차 | 기대 결과·실패 기준 |
|---|---|---|
| LF01 | 호수 마을 식사 작업대 (1029,62,409), 공유 식탁 (1051,62,451)로 접근해 측면 관찰 | 그릇·기록물이 상판에 닿음. 부유·식탁 밖 돌출·관통이면 실패 |
| LF02 | 피난민 (992~996,62,469~474) 근처에서 낮 시간 90초 관찰 | 두 사람이 서로 다른 시점에 짐 정리·다른 짐 확인·휴식 후 복귀. 계속 서 있기·제자리 걷기·상자 관통·허공에 손질이면 실패 |
| LF03 | LF02 작업 중 말을 걸고 대화 종료 후 관찰 | 대화가 정상 열리고 종료 후 일과 재개. 예약·자세 고착이면 실패 |
| LF04 | 별도 테스트 저장 슬롯에서 저장·로드 후 LF01~03 반복 | 소품·몸 중복 없음, 기존 이야기 진행과 상호작용 유지 |

회신: `항목 ID / 통과·실패·미수행 / 실제 결과 / 화면·영상 또는 로그`.

## 보존

저장 전 씬 [백업](../../../output/NpcDailyAnimations/LakeFix/LakeVillageWorld.before.unity) 273,543,167바이트는 Git 추적 밖 사용자 현장 변경을 포함하므로 보존한다. 검사 캡처·적용 로그·증분 저작 입력도 보존한다. 임시 메시 검사 오브젝트와 원본 비교용 복제본은 제거했다. 새 빌드·패키지 캐시 없음.

기존 미커밋 코드·프리팹·MotionSet 변경은 보존한다. 전체 월드 재생성·Player/Addressables 빌드·커밋은 수행하지 않는다.
