# E — 주민 6명의 생활·피신 모션 인계

> 2026-10-23 추가 작업 반영. 클립 17개·MotionSet 16개, 손 소품 4개. MCP로 두 본편 맵에서 6명의 작업·운반을 실행했다. EditMode 14/14, Play Mode 12/12 통과. **모션 연결은 확인했지만 작업물 접점과 실제 피신 동선의 최종 승인은 보류다.**

기준: [작업 지시](../LAKE_OF_LIFE_LIVING_WORLD_TASKS.md), [A 계약](A_BASELINE.md), [C 인계](C_NPC_HANDOFF.md), [I 통합 기록](I_INTEGRATION.md).

## 이번 추가 작업

- `LW_CarryWalk`, `LW_CarryIdle` Humanoid 클립과 MotionSet을 추가했다. 기존 보행/대기 클립에 건네기 자세를 바탕으로 보정한 오른팔·손목·손가락 근육 커브를 합성했다. 신규 모캡은 아니다. 원본 FBX·임포터는 변경하지 않았다.
- 6개 일과 프리팹의 작업 이동 12단계에 운반 보행·정지 자세와 작은 주머니를 연결했다. 피신 이동은 기존 빈손 보행을 유지한다.
- `NpcRoutine.Step`에 `travelMotion`, `travelIdleMotion`, `travelPropPrefab`을 추가했다. 명시 소켓이 없으면 바인딩 때 Humanoid 오른손을 캐시한다. 매 프레임 뼈 검색은 하지 않는다.
- 이동 중 주머니 → 도착 시 작업 소품으로 교체한다. 중단·위험·비활성화는 동일한 소유권 경로에서 이전 소품을 즉시 숨기고 파괴하며 예약을 반납한다. 운반 소품만 있고 정지 자세가 없는 데이터는 예약을 거부한다.
- 주머니·숟가락·약병·망치 프리팹을 기존 환경 자산으로 만들었다. 손 소품의 Collider는 제거했다. 작업 소품은 조이 조리, 헤이즐 정리/방문, 연목 준비/수선의 5개 작업점에 연결했다.
- 도착 허용 거리를 0.35m에서 0.06m로 줄였다. KCC의 일시적인 불안정 접지 판정이 걸을 수 있는 경사면을 벽으로 취급해 일과를 취소하지 않도록 지면 법선을 함께 확인한다. 진짜 장애물·정체 취소는 유지한다.

## 본편 검수에서 고친 문제

1. `NPC_Joy`에 같은 설정의 `NpcMovementController`가 두 개 있었다. 작업 상태와 별개로 다른 컨트롤러가 배회/대기 모션을 덮어썼다. 본편 씬의 중복 한 개를 제거했다. 남은 컴포넌트와 Actor ID는 보존했다.
2. 헤이즐의 첫 작업점이 탁자 내부에 있어 접근하지 못했다. `lake.care.pack`을 `(1005.1, 62.48, 408)`, 방향 270도로 옮겼다.
3. 조이의 조리 작업점을 기존 식탁 가까운 `(1034.4, 62.05, 406)`, 방향 90도로 옮겼다. 작업 용기까지 맞춘 최종 조리 배치는 아니다.
4. 숟가락의 끝이 위를 향하던 피벗을 뒤집었다. 실제 P09 조이 Avatar에서 두 조리 루프 각 5개 표본의 손잡이–오른손 중지 중간 마디 거리는 0.01037~0.01046m, 숟가락 끝은 손잡이보다 0.2782~0.2795m 아래였다. 이 수치는 손잡이 결합 확인이며 용기 접점 통과를 뜻하지 않는다.

씬 수정 전 사본과 비교하면 위 중복 컴포넌트 제거·두 작업점 위치/방향 변경만 있다(8줄 추가, 38줄 삭제). 이전 작업자의 본편/Flow 변경을 되돌리지 않았다.

## 주민별 데이터

| 주민 | 첫 작업 | 두 번째 작업 | 도착 휴식 |
|---|---|---|---|
| 조이 | MealPrep: 조리 시작→두 조리 루프, 숟가락 | MealServe: 건네기→대기 | RestStand |
| 헤이즐 | MedicinePack: 손질·정리, 약병 | CareVisit: 건네기→대기, 약병 | RestStand |
| 모건 | CargoInspect: 짐을 내려다봄 | CargoRope: 끈 당기기 | ShelterWait |
| 연목 | ToolPrepare: 공구 준비, 망치 | Repair: 시작→수선 루프, 망치 | RestStand |
| 잔류 주민 | SeedSort: 물품 분류 | RestStand: 우물 대기 | ShelterWait |
| 짐 주민 | DeparturePack: 짐 정리 | RestStand: 출발 대기 | ShelterWait |

모든 작업 이동에 CarryWalk/CarryIdle과 CarrySack을 사용한다. 조리·수선의 시작 섹션은 반복하지 않는다. 내부 블렌드 0.2초, 일과 진입 블렌드 0.25초다. `CookEnd`/`RepairEnd`는 별도 1회 클립/모션으로 준비했지만 자동 종료 재생은 연결하지 않았다. 중단 시 기존 크로스페이드를 따른다. 생활 반복은 퀘스트·물자·저장 사실을 만들지 않는다.

## 수행한 검증과 한계

| 검사 | 실제 결과 |
|---|---|
| 클립·MotionSet·프리팹 | 17개 Humanoid 클립, 16개 MotionSet, 18개 작업점. 기존 MotionSet 검증기 오류·경고 0. 비정상 커브·AnimationEvent·원본 리그 Transform 경로 의존 0, Missing Script·중복 작업점 ID 0 |
| 운반 데이터 | 6명×2단계의 걷기·정지·주머니 참조 연결. Play Mode에서 주머니 운반→도착 후 작업 자세 교체 관찰 |
| EditMode | `NpcRoutineContractTests` 14/14, 실패/제외 0, 0.666초. job `b838d6203f304e4a94cdfca36aa463a9` |
| Play Mode 자동 검사 | `NpcLifeActionTests` 12/12, 실패/제외 0, 1.505초. 운반 데이터 거부·소품 교체·위험 취소·회수 검사 포함. job `6d71aedb152c4aeea8af22340d072346` |
| 첫 Play Mode 시도 | 테스트 진입 초기화 120초 초과로 실행 0개. 테스트 코드 실패로 계산하지 않았다. 재시도에서 위 12개가 실행·통과했다 |
| 호수 본편 | Boot→본편 로딩 후 4명의 첫/두 번째 작업, 해당 MotionSet 재생과 접지를 계측했다. 중복 컨트롤러/작업점 보정 후 네 명 모두 지정 작업 모션까지 진입 |
| 가뭄 마을 본편 | 본편 로더로 진입해 두 명의 분류/짐 정리, 우물/출발 대기, 주머니를 든 보행을 확인. 첫 작업 15초 관찰 기록과 운반 화면 보관 |
| 중단 | 호수 4명에게 `PauseRoutine` 계약을 적용해 예약 해제와 손 소품 숨김을 확인. 게임패드로 실제 대화 분기를 완주한 검사는 아님 |
| 위험·피신 | 호수 4명은 위험 입력 직후 작업을 해제했으나 본편 휴식점의 `IsSheltered=false` 때문에 피신 출발을 거부했다. 두 맵 모두 6개 보호점 승인과 실제 이동 완료는 미확인. 설정만 true로 바꿔 통과시키지 않았다 |
| 콘솔 | 정상 Boot→호수 진입 오류 0 확인. 맵 왕복 중 기존 `FLOW_RedTreeVillageWaterReport/WaitPlayerInVolume`에서 파괴된 PlayerActor 접근 오류 1건 관찰. 최초 씬 직접 실행에서는 기존 DissolveMaterial 초기화 오류도 관찰. 전체 본편 오류 0으로 보고하지 않는다 |
| Windows Player | 미수행. 모듈/타입 이동/Addressables/빌드 설정은 바꾸지 않았다. 이번 NPC 모션 검수와 I/V의 전체 맵 통합 검증을 구분한다 |

검수 중 일과 배치 게이트와 필요한 주민 활성화 조건을 **메모리에서만** 열고 시간을 10:00으로 맞췄다. 카메라 밖 모션 관찰을 위해 Animator를 AlwaysAnimate로 바꿨다. D 입력과 충돌하지 않도록 검수용 맑음 입력을 사용했다. 최종 프리팹·씬에 이 우회 설정을 저장하지 않았다. 저장 API를 호출하지 않았으며 실제 서사 해금/게임패드/저장 복원 검증을 대신하지 않는다.

## 검수 보류 항목

- **조리 용기:** 숟가락 손잡이는 보정했으나 손 동선 아래의 용기가 없다. 식탁 이동만으로 조리 접점이 완성되지 않는다.
- **수선·운송:** 연목의 망치 손잡이는 실제 화면에서 결합을 확인했지만 타격 대상 작업대가 없고, 모건의 당김 동선에 실제 끈/짐이 없다. 허공 작업으로 보여 승인할 수 없다.
- **가뭄 주민:** 동작과 운반은 재생되지만 분류할 씨앗/짐이 손 작업면에 배치되어 있지 않다.
- **돌봄:** 작업점 접근은 해결했으나 약병을 받는 상대 손, 직접 처치, 물건의 실제 인계/놓기까지 저작한 것은 아니다.
- **피신:** 6명 모두 실제 지붕·그늘과 안전한 보행 경로를 확정한 다음 보호점과 배치 승인을 켜야 한다. 현재 `_isPlacementReady=false`를 유지한다.
- **연속 검수:** 주민별 3회 일과 반복, 실제 게임패드 대화→피신→복귀, 저장 사본 복원은 남아 있다. 재생/계약 테스트 통과를 이 항목들의 통과로 바꾸지 않는다.

따라서 이번 결과는 **운반 추가 및 본편 검수 수행, 결함 일부 수정**이며, 6명의 생활·피신 콘텐츠 전체 완료는 아니다.

## 파일과 재현 증거

- 클립: `Assets/07.Animations/LivingWorld/NPC/`
- MotionSet: `Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/LivingWorld/`
- 일과/손 소품: `Assets/03.Prefabs/LivingWorld/NPC/`, `Assets/03.Prefabs/LivingWorld/Props/`
- [원본·생성 자산 목록](E_MOTION_SOURCES.json), [자산 검사](E_MOTION_VALIDATION.json)
- `output/LivingWorldMotions/Integration/`: 저작·재현 실행문, 6명 작업 계측 JSON, 손잡이 표본, 화면 캡처, 씬/프리팹 변경 전 사본.
- 주요 화면: `drought_carry_contact.png`, `joy_cooking_final.png`, `hazel_contact.png`, `yeonmok_repair_contact.png`, `morgan_rope_contact.png`. 조리 화면은 숟가락 방향 최종 보정 전의 실패 증거이며 최종 손잡이 수치는 `spoon_grip_samples.json`을 본다.

Editor는 Play Mode를 종료하고 원래 호수 씬, 시작 씬 override 없음으로 복구했다. 검수가 유발한 Mobile_RPAsset 자동 직렬화 변경만 되돌렸다. 커밋하지 않았다.