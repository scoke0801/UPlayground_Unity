# I — 두 맵 본편 통합 기록

2026-10-23 / **I1 연결 가능 범위 반영, 전체 검증 완료 아님.** Unity 6000.3.21f1.

두 맵 왕복·우물 조사·식수 인계, 기존 주민 6명의 바인딩과 생활 구역 4개, 환경 생산자, R04~R09의 지역별 사건을 본편에 연결했다. 관련 EditMode **75/75**, 생활 행동 Editor Play Mode **11/11** 통과다. 실제 두 맵 로딩·서사 완주·게임패드·저장 사본·날씨 화면·성능·Windows Player는 미검증이다.

**주민 반복 일과와 피신 보행은 아직 활성화하지 않았다.** C는 설정 묶음만, E는 기존 자산 조사만 인계했다. 6개 `_isPlacementReady=false`를 유지한다. 실제 안전한 처마/그늘 경로, 일부 출발 위치, 작업 모션·손 소품 접점이 확정되지 않았다. 환경 상태가 주민에게 전달되는 것과 주민이 안전하게 피신하는 것은 별도 완료 조건이다.

기준: [작업 지시](../LAKE_OF_LIFE_LIVING_WORLD_TASKS.md), [기획](../LAKE_OF_LIFE_LIVING_WORLD_PLAN.md), [A](A_BASELINE.md), [테스트 지침](../../guide/TEST_VALIDATION_GUIDE.md).

## 1. 인수한 산출물과 버전

아래 문서·로컬 파일을 실제 확인했다. 병렬 담당의 향후 산출물은 포함하지 않았다. B의 2026-10-22 22:43 저장 중지 이후 B1을 인수하고 I가 새 씬을 저장했다. A/B/C/D/E 문서와 C/D 원본 프리팹은 수정하지 않았다.

| 입력 | 인수 범위 | SHA-256 또는 버전 |
|---|---|---|
| A_BASELINE.md | 지역 분리표, 기존 ID, 대표 상호작용 6곳, 저장 경계 | `EBCD4B3A2B9945FC553B9C550DB6CEFEA414C23E771D249B1798DB5E1DBC72DD` |
| B_MAP_HANDOFF.md | B1 씬·지형·NavMesh·도착점, 수락/실패 복귀 로딩, 저장 사본 API | `DA1110451FD49A6C58DDD03BC9AEFE9DF83A88D765087AB2C618BA41722E6623` |
| C_NPC_HANDOFF.md | 6개 일과 설정, 작업점 18개, 예약·취소·환경 바인딩 | `237D6F39D6CAC89ADAEFCDA24F3E5819873BE71B2236F3F2C42437E65C763018` |
| D_ENVIRONMENT_HANDOFF.md | 두 맵 시간표 v1, 환경/효과 프리팹, 수면·흙·URP 입자 셰이더 | `8189F7231DE1A0B1DFD367CBE2C4F1E8DF0E6A194EFC3442C605A2E5467C040B` |
| E_ASSET_HANDOFF.md | 기존 우물·그릇·통·상자·수레 등 후보 실측과 현장 검수 요청 | `FC0DDAB7E5EE044FE1C146898CCC35938799B3C2DBD0D93E5746EF77C01FD428` |

E의 신규 모델·텍스처·재질·프리팹 납품은 **0개**다. C의 작업 MotionSet 연결은 헤이즐 약 포장 1개뿐이며 나머지를 임의 동작으로 채우지 않았다. D의 바람·빗소리 AudioSource는 clip 미연결이다. 과거 B/C/D 검사 결과는 이번 검사 결과에 합산하지 않았다.

### 사용자 변경 보호

- 시작 호수 씬: `23F089199D37FB8FCD136574D0BAEE2A2A58CAD8660D8F0B222E4799B2D6FFD5`.
- 시작 B1 씬: `C9D4D2BD04B2BC9A01F1D8EFE7D956613A30A92E9955CB7B0AD721B2D0EFFE88`.
- 시작 상태: [InitialStatus.txt](../../../../output/LivingWorld/I/InitialStatus.txt). 두 씬 원본은 `output/LivingWorld/I/Backup/`에 보존했다. 재생성할 수 없는 미커밋 사용자 배치의 복구 근거이므로 검증 임시 사본처럼 삭제하지 않았다.
- 원래 호수 Transform **63,015개** 중 **55,773개 유지, 7,242개 이관**, 누락 0. 원래 로컬 위치·회전·크기·활성 상태 변경 0. 이관 중 표식 하나의 부동소수점 크기 오차도 원래 값으로 복구했다. [비교 결과](../../../../output/LivingWorld/I/MovedTransformComparison.json).
- B의 비활성 R04 복제 묶음을 지우고, 현재 사용자 호수 씬의 실제 R04 묶음을 이관했다. 호수 지형을 다시 생성하거나 사용자 배치를 HEAD로 복원하지 않았다. 기존 호수 지형의 전체 축소·미술 재배치는 이번 반영에 포함하지 않는다.
- 원본 저장 슬롯 읽기/변환/덮어쓰기, 커밋은 수행하지 않았다.
- 재개 시 저장소 HEAD는 `19e38ad2`로 바뀌어 있었다. 그 커밋과 현재 작업 트리를 보존했고, 위 최종 씬 해시가 유지됨을 재확인했다. I는 커밋 명령을 실행하지 않았다.

## 2. 반영 순서와 본편 연결

### 2.1 왕복과 우물·식수

| 출발 | 명시 입력 출구 | 대상 씬 / 도착 ID | 도착 위치 / 회전 quaternion |
|---|---|---|---|
| 호수 | `Exit_ToDrought`, 약 `(984,62.81,449)` | `DroughtVillage` / `livingworld.drought.from_lake` | `(978,63.071426,451)` / `(0,-0.525731,0,0.850651)` |
| 가뭄 마을 | `Exit_ToLake`, 약 `(985,62.9714,451)` | `LakeOfLifeScenario` / `livingworld.lake.from_drought` | `(991,62.74581,449)` / `(0,0.707107,0,0.707107)` |

출구는 진입 자동 이동이 아니라 상호작용 입력이다. 출구와 도착점을 약 7m 분리했다. 도착점의 발견 키는 각각 `livingworld.drought.entrance`, `livingworld.lake.outskirts`이며 B의 로딩 완료 경로에서만 활성화한다. RegionInfo에는 해당 지역으로 돌아오는 **자기 씬 도착점**을 등록했다. 출구의 반대 씬 목적지를 귀환용 RegionInfo에 잘못 복사하지 않았다.

Actor의 `ISceneTransitionService`에 B 구현의 수락 결과·로딩 상태·완료/실패 이벤트를 노출하고 Portal이 `TryLoadScene`을 사용하게 했다. 로딩 요청 검증 후 진행 중 `NpcLifeInteractable`은 기존 `StopInteract`로 정리한다. 대화 중 일반 요청은 거절하고 완료 사실을 만들지 않는다. 실제 입력 잠금 해제·실패 복귀는 LW-T01/03/08 대기다.

대표 상호작용은 A의 6곳을 그대로 연결했다. 한꺼번에 열기 위한 선행 플래그 조작은 하지 않았다.

| 대상 | 위치/지역 | 완료 경계 |
|---|---|---|
| R03 식사를 나른다 | 호수 `(1030.5,62.051,409)` | 기존 실제 인계 후 `lake.red_tree.meal_served` |
| R03 돌봄 물품을 건넨다 | 호수 `(1006,62.631,410)` | 기존 실제 인계 후 `lake.red_tree.care_delivered` |
| 가뭄 마을로 가는 길 | 호수 출구 | 사건 완료 없음, 성공한 도착만 발견 |
| `R04_well` | 가뭄 마을 | 기존 조사·대화 정상 종료 후 `lake.red_tree.well_examined` |
| `R04_water` | 가뭄 마을 | `resident_stays` 후 실제 인계로 `water_handed_over` |
| 호수로 돌아가는 길 | 가뭄 마을 출구 | 밤·날씨·재고로 귀환을 잠그지 않음 |

### 2.2 주민과 생활 구역

기존 몸을 재사용하고 `LivingWorld` 아래 고정 설정 프리팹만 추가했다. NPC의 자식으로 작업점을 붙이지 않았다.

| 지역/구역 | 기존 주민 | 작업점 묶음 | 상태 |
|---|---|---|---|
| 호수/식사 마당 | NPC_Joy | `lake.meal.*` | 배치·몸 바인딩, 비활성 대기 |
| 호수/돌봄 자리 | Npc_Hazel | `lake.care.*` | 약 포장 모션 1개, 비활성 대기 |
| 호수/운송 자리 | NPC_Morgan | `lake.transport.*` | 현재 사용자 위치 유지, 비활성 대기 |
| 호수/수선 자리 | Npc_Yeonmok | `lake.repair.*` | 현재 사용자 위치 유지, 비활성 대기 |
| 가뭄/잔류 주민 | RedTree_RemainingResident | `drought.resident.tools`, `drought.well.wait`, `drought.shade.stay` | 기존 잔류 사실 게이트 유지, 비활성 대기 |
| 가뭄/출발 준비 | RedTree_SettlingResident | `drought.depart.pack`, `drought.depart.wait`, `drought.shade.depart` | `departure_prepared` 뒤 중지 조건 유지, 비활성 대기 |

작업점 18개는 실제 주변 지형 높이에 배치했지만 최종 작업면·처마 자리는 아니다. 일반 일과가 R04/R09 인계 완료·인벤토리·주민 선택을 생산하지 않는다. R09 포장/적재 장소에 맞춘다는 이유로 모건의 미커밋 위치를 이동하지 않았다.

### 2.3 환경

- 각 씬에 D 환경 생산자 1개와 효과 프리팹을 배치하고, 기존 시간 서비스의 day/minute를 소비한다. 프로필은 v1 그대로다. 별도 날씨 저장 필드를 추가하지 않았다.
- 새 `EnvironmentSceneBinding`이 카메라 서비스의 실제 카메라를 관측점으로 연결한다. 전투 중 또는 InGame 이외 카메라 모드에서는 집중 감쇠한다. 대화 감쇠는 D의 기존 경로가 담당한다.
- 기존 `WorldLightingSceneBinding`과 Directional Light를 재사용했다. 각 맵 방향광 1개, 조명 바인딩 1개다. 씬에 경쟁 `SICS.WindSettings` 작성자는 발견되지 않았다.
- C의 기존 `NpcRoutineEnvironmentBinding`에 해당 씬 D 생산자를 연결했다. 현재 Assembly-CSharp 브리지가 Actor에 값만 전달하므로 제안 단계의 공용 `IEnvironmentService`/DTO는 추가하지 않았다. 다른 소비자가 필요한 시점에 별도 계약을 검토한다.
- 지붕 bounds 기반 보호 볼륨은 호수 5개/가뭄 2개, 기본/지형 충돌층을 비 입자 지붕 검사에 연결했다. **이 볼륨이 NPC의 접근 가능한 피신 장소를 보증하지는 않는다.** 현재 휴식점 모두 `IsSheltered=false`다.
- 가뭄 먼지를 밭 부근 `(941,72.6,479)`에 고정했다. 호수 먼지는 D의 비활성 설정을 유지한다.
- 기존 재질을 복제한 5개에만 수면 `_WeatherWindResponse=0.15`, 야외 개울 흙 `_WeatherExposure=1`을 적용했다. 기존 수위·회복·색·vertex R/A 의존 값은 보존했다. 원본 외부 재질을 수정하지 않았다. 호수 Terrain 전체가 날씨 젖음에 반응한다고 판정하지 않는다.
- 환경음 clip/Mixer 완성, 지붕 입자·실내 화면, 조사/전투 가독성, 낮밤 노출과 성능은 대기다.

### 2.4 R04~R09 지역 분리

| 사건 | 가뭄 마을 | 호수에 유지 |
|---|---|---|
| R04 | 우물·집·주민 선택·식수/짐·밭·개울·조우와 현장 참조 | 보고·지원 배정·호수 물자·세라·다음 기록 |
| R05 | 없음 | 기록대·차단 수레·우회로·구조·쉼터·동료 합류 전체 |
| R06 | 설치 묶음/순서·시험 표식·수면·협조 주민/묘묘·구조·단절/전후 관찰 | 목록·포장/재고·식량·외부 식수·세라·다음 단계 |
| R07 | 잔류 주민 거처/식량 인계와 결과 | 외곽 인계·돌봄·미아 대피·재고/몫·준비 집계 |
| R08 | 없음 | 중앙 제단과 결단·수위/회복 관찰 |
| R09 | 의사·수령·외부 물길·수로/길·관찰·밭 재방문, 해당 작업 대역 5명 | 재고·포장·수레·적재·출발·식사·비용·안내·재방문 |

각 지역에 해당 그래프의 수동 러너 1개씩만 둔다. 두 RegionInfo의 `flowGraphs`는 비어 있어 자동 러너를 중복 생성하지 않는다. 분리 후 다른 씬 러너를 참조하던 TriggerVolume 20개를 지역 부모 러너로 재연결했다. StoryCharacterPresence 참가자는 실제 지역에 따라 분리했다.

R04 공유 그래프의 모든 `encounter_gate` 진입 앞에 새 `livingworld_encounter_scene` Branch와 `RunnerSceneCondition(DroughtVillage)`를 두었다. 호수에서 전역 플래그가 바뀌거나 재개돼도 가뭄 조우를 잘못 생성하지 않는다. 기존 그래프/노드/entry/volume/퀘스트/플래그 ID는 유지했다. 다른 지역에 없는 볼륨을 완료 처리하지 않는다.

R07 잔류 식량과 R09 수령은 호수의 출발 물자 표시와 가뭄의 인계 결과를 각각 로컬 Presentation으로 나눠 **동일한 기존 완료 플래그**를 읽게 했다. 완료 시 호수 물자가 남아 있거나 두 지역에서 같은 소품을 소유하는 참조를 제거했다. R04 식수/R06 외부 물통/R09 반환은 별도 사실로 유지했다.

## 3. 데이터·지도·저장·로딩 감사

| 항목 | 반영/검사 결과 | 남은 검증 |
|---|---|---|
| MapConfigDatabase | DroughtVillage 1개 추가, 기본 시작 맵 호수 유지, 양쪽 SceneContext/Region 연결 | 실제 지도 전환 |
| QuestDatabase | 기존 27개, null 0·중복 questId 0. 갱신/ID 재발급 안 함 | R04~R09 양쪽 목표 갱신 |
| 지도 배경 | 호수 1084×1536, 가뭄 977×1536 재촬영·Sprite 연결. 기존 호수 중심/범위 유지, 가뭄 중심 `(965,490)`·70×110 | UI상 위치·축·줌·게임패드 |
| 다른 맵 목표 | 기존 LocationId를 각 출구 위치로 등록한 별칭 113개. 목적지 도착 후 실제 현장 마커 사용 | 다중 목표 겹침/선택 가독성 |
| 중복 마커 | R09 재방문 중복 registrar 1개 정리. 기존 `clue_red_cloth` 2개는 시작 사본에도 있어 범위 밖 결함으로 보존 | 별도 소유 대상 확인 후 정리 |
| Addressables | MapConfigDatabase/QuestDatabase 모두 Default Local Group의 기존 키. 새 지도 Sprite가 DB 의존성에 포함됨 | 콘텐츠 빌드/카탈로그 재생성 |
| 씬 포함 | 기존 목록 보존, DroughtVillage enabled 추가. 활성 BuildProfile=Global. Boot/Title/Loading/호수 enabled | Windows Player에서 CanStreamedLevelBeLoaded 및 실제 로드 |
| NavMesh | 가뭄 사건 기물을 VillageGeometry에 넣고 기존 GUID NavMesh.asset 재베이크. 주요 경로 PathComplete | 실제 KCC 보행/교행/입력 거리 |
| 도착점 | 두 신규 도착점 플레이어 Capsule 침투 0(0.02m 초과 기준), 출구와 분리 | 플레이어 실제 도착·낙하·방향·재전환 |
| 셰이더 | 씬 의존 셰이더 호수 25개/가뭄 18개 ShaderHasError 0, PC_RPAsset Depth Texture=true | Player stripping·깊이·핑크 재질·날씨 프레임 |
| 조명/카메라/대화/Flow | 직렬화 Missing Script·깨진 참조·다른 씬 참조 0, 기존 카메라 배치 보존 | 실제 파트너/앵커·조사 구도·낮밤 |

지도 촬영은 본편을 저장하지 않는 PreviewScene에서 NPC 렌더를 제외했다. 생성 이미지를 직접 확인했으며 가뭄 우물·집·개울과 호수 길/호수 형태가 보인다. 이것은 인게임 지도 조작성 검증이 아니다.

### 저장 GUID와 사실

이관한 SceneEntityId는 아래 9개이며 두 맵에 중복 배치하지 않았다. 새 씬 소유 키는 같은 컴포넌트의 GlobalObjectId로 갱신했고, 최종 소유 키 불일치 0이다.

| 기존 GUID | 가뭄 마을 몸 |
|---|---|
| `246895bd33e24c92ad5e4b8b548345e4` | RedTree_RemainingResident |
| `efe248fe56b44d19a27886ff5d9d3dab` | RedTree_SettlingResident |
| `083d77d937fd40a99cd30c33e09ef295` | R06 설치 협조 주민 |
| `5621369a51b2427d85d9012f62350f08` | R06 묘묘 |
| `a136338a65504c7aa7424cbd2560d52d` | R09 Myomyo |
| `103d96c2c2e14b2c8246b5a6b746613f` | R09 Lili |
| `1f39e0d43ead45528b4f4834fcdc0d17` | R09 SeolA |
| `97cecb8ab93c4a18b7b2eca9b56f4612` | R09 Hwarin |
| `910fc3319f4f4a08938faa0187e7a6ca` | R09 Yura |

B의 `SaveManager.TryCreateLivingWorldCopy`와 `LivingWorldMapMigrationPlan`을 유지했다. 자동 이관은 켜지 않았다. 실제 원본 슬롯/안전 위치를 검토하지 않은 상태에서 `IsPlayerLocationReviewed`나 `HasVerifiedPose`를 true로 만들지 않는다. 구 피해 구역의 위치 판정은 슬롯별로 하고, 안/밖을 단순 사각형으로 추정하지 않는다. 후보 도착점의 정적 검사 통과는 저장 복사본 플레이 통과와 다르다.

적용 시 빈 목적 슬롯만 사용한다. 위 9 GUID의 맵 소유를 이관하되 플래그·퀘스트·가방·파티·주민 선택은 그대로 둔다. 암호화 사본 재독해 후에만 B API가 `lake.living_world.map_split.v1`을 기록한다. 실 저장 사본 생성/로드는 아직 미수행이다.

## 4. I 변경 파일

I1 최종 SHA-256:

| 파일 | 해시 |
|---|---|
| LakeOfLifeScenario.unity | `002733D4A265D7B91D9C23B4B764FF9F764927F254DEABCC7B4F3E9F82D15E21` |
| DroughtVillage.unity | `E49099CA1A39EB5CC5676A75547A99AC538C8C87AEB8D7B5CF41C1854635BD6B` |
| MapConfigDatabase.asset | `EE0D4126BE8973462C34B0491C0E2ED2FB3E4866CB861435822A19AA757A6C34` |
| DroughtVillage/NavMesh.asset | `C7E328B9FA719001233EE66995A86304D83CECA19C8DD5B8ABA8F6975C4F65CD` |

`.meta`를 함께 보존/생성했다. 아래는 I가 직접 바꾼 범위이며, git diff 전체에 보이는 기존 B/C/D 작업을 I의 신규 구현으로 세지 않는다.

| 파일/경로 | I 변경 |
|---|---|
| `.gitignore` | DroughtVillage 씬과 LivingWorld 프리팹의 좁은 추적 예외 |
| `Assets/01.Scenes/Scenario/{LakeOfLifeScenario,DroughtVillage}.unity` | 지역 이관·출구/도착·주민/환경·마커·참조 연결 |
| `Assets/02.Scripts/GameActor/Contracts/ActorServices.cs` | SceneTransition 계약에 B의 수락/상태/결과 노출 |
| `Assets/02.Scripts/GameActor/Object/Portal/PortalActor.cs` | 수락 반환 API 사용, B의 명시 이동 입력 유지 |
| `Assets/02.Scripts/Manager/Scene/SceneManager.Load.cs` | 대화 중 요청 거절, 진행 중 생활 인계 취소 |
| `Assets/02.Scripts/FlowGraph/Nodes/RunnerSceneCondition.cs` | 러너의 실제 씬 조건, 신규 |
| `Assets/02.Scripts/Gameplay/World/LivingWorld/EnvironmentSceneBinding.cs` | 카메라·전투 집중 바인딩, 신규 |
| `Assets/10.Datas/Map/MapConfigDatabase.asset` | 가뭄 맵 등록 |
| `Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/{RegionInfo,MinimapConfig}.asset` | 귀환 발견점·새 지도 연결 |
| 같은 폴더 `RedTree/R04/FLOW_RedTreeVillageWaterReport.asset` | 조우 지역 Branch 추가 |
| `Assets/10.Datas/World/LivingWorld/Maps/DroughtVillage/{NavMesh,MinimapConfig}.asset` | 기존 GUID 베이크·지도 연결 |
| `Assets/10.Datas/World/LivingWorld/Maps/{LakeOfLifeScenario,DroughtVillage}/Minimap.png` | 새 지도 촬영 Sprite |
| `Assets/11.Material/LivingWorld/Integrated/*.mat` | LakeWater, CreekPool, CreekSilt, R06_StrongDraw, R06_ReducedDraw 복사본 5개 |
| `ProjectSettings/EditorBuildSettings.asset` | 새 씬 enabled 등록 |
| `Assets/Tests/EditMode/FlowGraph/LivingWorldIntegrationTests.cs` | 지역 조건·중복 러너/주민·직렬화 참조 회귀 3개 |
| 이 문서와 `.meta` | 입력 버전·반영·검사·잔여/사용자/Player 명세 |

일회성 연결 코드와 감사 근거는 `output/LivingWorld/I/`에 있다. 영구 Editor 도구/메뉴를 추가하지 않았다. 해당 폴더는 git ignore 대상이므로 외부 인계 시 인용된 근거도 함께 전달한다.

## 5. 수행한 검사와 남은 결함

- 최종 통합 EditMode **3/3**, job `0bf6fd67555c4f158d358e82463f525e`. 두 저장 씬을 PreviewScene으로 열어 등록·러너·주민·직렬화 참조 검사. [결과](../../../../output/LivingWorld/I/FinalIntegrationEditModeTests.json).
- 시작 스냅샷과 R04/R06/R07R08/R09 노드 ID를 대조했다. 기존 **799개 모두 유지**, R04 지역 분기 1개만 추가했다. [결과](../../../../output/LivingWorld/I/GraphIdPreservation.json).
- 관련 EditMode **72/72**, job `c2443bd271244197ad427363d5895b7e`: R04/R05/R06/R07R08/R09 콘텐츠, LivingWorldMapMigration, SceneLoadOwnership, NpcRoutineContract, EnvironmentSchedule/ShaderState. [결과](../../../../output/LivingWorld/I/RelatedEditModeTests.json).
- Editor Play Mode **11/11**, job `eb0499e713d24fd49debf77af4460bfd`: NpcLifeActionTests. 실제 작업점 수명주기·인계 취소/늦은 재개·낙하 속도 검사다. 본편 전체 로딩/보행 성공으로 확대하지 않는다. [결과](../../../../output/LivingWorld/I/NpcLifePlayModeTests.json).
- 최초 PlayMode 호출은 namespace 필터 오기로 **0개**였다. 통과로 세지 않고 올바른 `UPlayGround.FlowGraph.PlayModeTests.NpcLifeActionTests`로 재실행했다. 일회성 감사 코드의 변수 이름 충돌도 수정 후 실행했으며 프로젝트 컴파일 실패가 아니었다.
- NavMesh를 맵별 원거리 오프셋으로 격리해 다른 열린 씬 데이터가 경로를 대신하지 못하게 했다. 호수 도착→출구/식사/돌봄, 가뭄 도착→출구/우물/식수/R06 구조/R09 수령·수로·길은 모두 PathComplete. [상세](../../../../output/LivingWorld/I/NavMeshChecks.json).
- 생활 작업점 18개는 NavMesh에 샘플되지만 **잔류 주민 원래 출발점은 1m 반경 샘플 실패**다. 헤이즐 약 포장점은 샘플 보정 거리 약 0.877m다. 나머지 5명→작업점은 경로 완전 판정이지만 C는 런타임 NavMesh 경로 추종이 아니라 명시 경유점 이동이다. 경유점·캡슐 보행을 승인한 것으로 보지 않는다.
- 가까운 실제 지붕/충돌체 후보에서 접근 가능한 보호 자리를 확인하지 못했다. 집의 실체 충돌을 무시하고 보호 플래그만 켜지 않았다. 피신 자리 6개와 실제 이동/모션 확보 전 일과 활성화 금지.
- 기존 `NPC_Joy`와 `Npc_Lucia`에 중복 NpcMovementController가 있다. 새로 만든 중복이 아니며 조이의 일과 활성화 전 소유 컨트롤러를 정리·재검증해야 한다. 이번 I에서 임의로 원본 주민 컴포넌트를 삭제하지 않았다.
- 기존 `clue_red_cloth` 중복 마커는 시작/최종 씬 모두 2개다. 이번 R03~R09 분리 밖의 소유 판정이 필요하다.
- 전체 EditMode/PlayMode 회귀·전체 프로젝트 참조 검사·실제 플레이·성능·Player 빌드는 미수행. 주요 통합의 최종 승인 전에 별도 V 범위로 남긴다.
- 마지막 확인에서 Unity 컴파일 종료·Console error 0, 호수 씬만 열림·dirty=false였다. 문서의 상대 링크 누락 0, `.meta` 생성 확인. I의 관련 코드/등록 데이터 `git diff --check` 통과이며 기존 대형 씬 전체의 서식 정리는 하지 않았다.

## 6. 사용자 플레이 테스트 명세

**준비:** I1 파일 상태, Unity 6000.3.21f1 URP, Boot에서 시작한다. 두 게임플레이 씬을 동시에 열린 채 Play하지 않는다. 키보드/마우스와 실제 게임패드 각각 검사한다. 새 테스트 저장 또는 별도 빈 슬롯의 사본만 사용한다. 사용자 원본 슬롯에 저장하지 않는다. 필요한 선행 단계는 정상 플레이/기존 검증 저장으로 준비하며 완료 플래그를 일괄 켜지 않는다.

| ID | 시작 상태·실행 절차 | 기대 결과 / 실패 기준 | 현재 |
|---|---|---|---|
| LW-T01 | 호수 외곽 출구에 접근→상호작용→가뭄 도착→즉시 귀환, 10회. 별도 검사에서 잘못된 도착 ID와 연속 입력 | 표의 맵/방향, 자동 왕복·이중 플레이어·입력 잠금 없음. 실패한 이동은 발견 안 됨, 원래 씬 복귀. 결과와 예외는 따로 기록 | 사용자 대기 |
| LW-T02 | R04 수락 전 우물 조사 정상 종료→호수 귀환/저장→수락→재방문. 다른 사본에서 조사 대화 취소 | 발견 누적·목표 반영, 취소는 재시도. 우물만 보고 진실 단계 확정/조우 중복 발생하면 실패 | 사용자 대기 |
| LW-T03 | R03 식사/돌봄, R04 잔류 후 식수/짐 준비, R09 수령을 각각 시작. 인계 도중 Cancel/씬 이탈/저장·로드 후 재시도, 완료 후 반복 | 실제 인계/도착 후에만 기존 완료 키·결과 표시. 출발 물자와 수령 물자가 동시에 남지 않음. 몸/재고 복제·늦은 콜백·입력 잠금 없음 | 사용자 대기 |
| LW-T04 | 구 저장 피해 구역 안/밖, 식수 전/후, R09 완료의 사본 준비. 슬롯별 위치/9 GUID 계획 검토 후 B 사본 API로 빈 슬롯 생성→로드→왕복→다시 로드 | 원본 파일 불변, 사본만 맵 위치/소유 변경. 퀘스트·플래그·인벤토리·파티·선택 보존. 중복 이관 없음. 경계 불명확 슬롯은 실패 처리하고 추정 안 함 | 사본 생성부터 대기 |
| LW-T05 | 안전 경로·모션·조이 컨트롤러 확정 뒤 검증 배치에서만 준비 상태 활성화. 08:59/09:00, 17:59/18:00, 21:59/22:00, 05:59/06:00. 대화·길 막기·예약 경쟁·중도 취소 | 6명 작업/이동/휴식 전환, 순간이동·무한 보행·경합·추락 없음. 밤에도 대화 가능. 손 소품·예약·시뮬레이션 임대 해제 | **선행 자산/경로 미완료** |
| LW-T06 | 호수 10~11:30 비, 12/15/18시 건조, 가뭄 14시 강풍에서 실외/처마 왕복. 같은 저장 시각 재로드, R08 회복 전후 비교 | 효과·풍향·NPC 입력 일관, 실내 비/먼지 억제. 날씨가 서사 수위/회복 키를 바꾸지 않음. NPC 피신 보행은 LW-T05 준비 후 검사 | 화면 대기, 피신 보행 보류 |
| LW-T07 | 우물/개울 조사 카메라, 주민 대화, 기존 전투·Lock-on·KillCam에서 환경 on/off 비교 | 얼굴·증거·공격 가림 없음, 감쇠와 복원 정상. 환경음은 clip/Mixer 도착 후 대사/전투 대비 청취, 현재 무음은 통과 아님 | 사용자/오디오 대기 |
| LW-T08 | 게임패드로 6곳 접근/입력/Cancel, 지도 열기→지역/귀환점 선택→Cancel. 미방문 가뭄, 방문 후 양쪽 지역, 다른 맵 퀘스트 추적 각각 | 초기 포커스/이동/Cancel/탭 정상, 선택 없는 강제 이동 없음. 출구 별칭에서 실제 마커로 갱신. 여러 목표가 겹쳐도 갈 곳이 식별됨 | 사용자 대기 |
| LW-T09 | 같은 기기·해상도·품질·카메라로 낮/밤/비/먼지, 환경 off/on, 일과 off/6명 비교. 워밍업 후 60초, 3~5분 관찰·10회 왕복 | CPU/GPU median·p95·GC·메모리·VRAM·배치/오버드로·로드시간 기록. 임대/입자/재질 누적 증가 없음. 16.7ms/추가 2ms는 확정 달성치가 아닌 예산 후보 | 미측정, 일과 비교 보류 |
| LW-T10 | R04 지원→R05 구조(호수)→R06 시험(가뭄)/물자(호수)→R07 잔류/외곽 양쪽→R08 중앙→R09 출발/수령/수로/길/결말 재방문. 각 경계 저장/로드, 가능한 입력 역순 | 끊긴 참조/목표/파트너/카메라/볼륨 없음. 같은 그래프의 전역 사실은 누적되고 로컬 사건은 해당 맵에서만 실행. R09 완료 뒤 물자/대역 원복·진행 소진 없음 | 사용자 대기 |

회신: `ID / 통과·실패·미수행 / I1 또는 후속 버전 / 시작 슬롯·단계·시각 / 입력 장치 / 실제 결과 / 화면·영상·Editor.log 또는 Player.log`. 진행 성공과 오류 로그 유무를 각각 판정한다. LW-T05/06 피신은 현재 알려진 미완료 항목이며 테스트 결과가 없는데 통과로 올리지 않는다.

## 7. 별도 Player 검증 계획

새 씬 포함·로딩 경로·공유 계약·새 SerializeReference 조건·날씨 셰이더가 함께 바뀌므로 **Windows Player 검증 필수**다. 이번 턴에서는 사용자가 요청한 별도 계획을 준비했으며 빌드/실행은 하지 않았다. 기존 Phase 7 카메라 미검증도 이 기록으로 해소되지 않는다.

1. V 착수 시 씬/코드/프로필/Addressables 해시를 다시 고정한다. 위 미완료 자산을 받은 뒤 재통합했다면 I1 결과를 그대로 재사용하지 않는다. 전체 EditMode/Editor PlayMode 회귀와 프로젝트 참조 검사를 수행한다.
2. 활성 Global BuildSettings의 Boot·Title·Loading·호수·DroughtVillage를 포함한다. 향후 BuildProfile override를 쓰면 동일 포함을 재확인한다. 프로젝트의 실제 Windows64 백엔드/스트리핑/URP 품질을 기록한다.
3. Map/Quest DB와 새 지도/환경 의존성을 포함해 Addressables 콘텐츠를 갱신한다. 기존 키/그룹을 유지한다. Always Included Shaders나 전체 variant 보존을 임의로 늘리지 않는다. 현재 씬·재질 참조 경로의 실제 포함을 먼저 검사한다.
4. StandaloneWindows64 Development Player를 `Builds/LivingWorld_I1_<실행일>/`에 한 번 빌드한다. 코드·셰이더·콘텐츠 빌드 오류 0을 별도 기록한다. 빌드만 성공해도 실행 통과로 판정하지 않는다.
5. Boot→새 테스트/복사 저장→호수↔가뭄 왕복, 지도 귀환, 로딩 실패 복귀, 우물/식수, R06/R09 경계, 저장 재로드를 실행한다. 핑크 재질·누락 Shader variant·Depth Texture·실내 입자·전역 풍향 복구·낮밤 조명·대화/전투 감쇠를 확인한다.
6. 실제 기기에서 LW-T09 비용을 측정하고 Player.log의 서비스 경고·예외를 확인한다. 배포 승인은 최종 Release 설정에서도 로딩/날씨/저장 핵심 경로를 재검사한다. 통과 후 불필요한 재생성 가능 빌드/캐시는 지우고 문서가 인용하는 최신 산출물만 보관한다.

**판정:** 사건·지도·환경 배선의 I1 반영과 관련 자동 검사까지. 주민 생활/피신 완성, 사용자 플레이 승인, 전체 회귀, Player 검증 완료는 아직 아니다.
