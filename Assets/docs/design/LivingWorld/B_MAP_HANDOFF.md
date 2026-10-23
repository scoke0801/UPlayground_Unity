# B — 맵 이동·저장 인계

> 2026-10-22 / B 코드·작은 씬 제작, I 연결 대기. [A 계약](A_BASELINE.md) 3~6·8절 기준. 실제 두 맵 왕복과 본편 완주를 통과한 보고서가 아니다.

## 1. 범위와 현재 상태

- B는 PortalActor, SceneManager 두 파일, SceneArrivalPoint, SaveManager의 복사 이관 경로와 전용 검사를 수정했다. 공용 Contracts/Data/asmdef/DB/주소/빌드 목록은 수정하지 않는다.
- 본편 씬은 수정·저장하지 않았다. 착수 SHA-256: `23F089199D37FB8FCD136574D0BAEE2A2A58CAD8660D8F0B222E4799B2D6FFD5`.
- 새 씬·별도 Terrain·NavMesh·RegionInfo·MinimapConfig를 생성했다. 기존 두 집·우물·밭·개울·두 주민과 피해 지역 R04 입력을 재사용했다. 호수 소속 9개 직계 자식은 새 복제본에서 제외했다. R06~R09는 참조 이관 명세이며 아직 새 씬에 배치하지 않았다.
- **씬 인계 버전 B1, B 쓰기 종료: 2026-10-22 22:43 KST. I 인수 확인 대기.** 아래 해시를 I가 인수하면 이후 씬·지형·NavMesh 수정은 I만 수행한다. B는 이 버전 이후 직접 저장하지 않고 변경 요청만 전달한다. I의 인수 사실을 임의로 완료 처리하지 않았다.

| B1 인계 대상 | SHA-256 |
|---|---|
| DroughtVillage.unity | `C9D4D2BD04B2BC9A01F1D8EFE7D956613A30A92E9955CB7B0AD721B2D0EFFE88` |
| Terrain.asset | `7DE8D7D8B5C131777381B7BD38223890360B1AB744B2F8D002A76D8F539CB5B2` |
| NavMesh.asset | `011CE36B6370FED4496420FBA29D9E36C32B8EE392E65F4E7C0F96FC77541CC8` |

## 2. 변경 파일과 등록값

기존 B 소유 변경:

- `Assets/02.Scripts/GameActor/Object/Portal/PortalActor.cs`
- `Assets/02.Scripts/Scene/SceneArrivalPoint.cs`
- `Assets/02.Scripts/Manager/Scene/SceneManager.cs`, `SceneManager.Load.cs`
- `Assets/02.Scripts/Manager/Save/SaveManager.cs`

신규 B 소유:

- `Assets/02.Scripts/Manager/Save/SaveManager.LivingWorld.cs`
- `Assets/02.Scripts/Manager/Save/LivingWorldMap/LivingWorldMapMigration.cs`
- `Assets/Tests/EditMode/LivingWorld/Map/LivingWorldMapMigrationTests.cs`
- `Assets/Tests/EditMode/LivingWorld/Map/SceneLoadOwnershipTests.cs`
- 이 문서 및 생성 파일의 `.meta`.

| 신규 콘텐츠 | 경로 / GUID |
|---|---|
| 씬 | `Assets/01.Scenes/Scenario/DroughtVillage.unity` / `0190397f9ddefa6479b21270b2d267ff` |
| 지형 | `Assets/10.Datas/World/LivingWorld/Maps/DroughtVillage/Terrain.asset` / `5558d33bf7d60b54ab76f3b298ea56df` |
| NavMesh | 같은 폴더 `NavMesh.asset` / `6ff76a12f52762243afec92c9baf17a4` |
| 지역 | 같은 폴더 `RegionInfo.asset` / `48b91a7f59032c748b912a775e61ebcd` |
| 지도 설정 | 같은 폴더 `MinimapConfig.asset` / `4448286d30a8c354a82569e805ca76cd` |

새 씬 연결 상세:

- `Arrival_FromLake`: 위치 `(978,63.0714,451)`, Quaternion `(0,-0.52573,0,0.85065)`; `SceneArrivalPoint`의 ID/activation은 아래 표. 원지형에서 출입구 주변을 평탄화했고 바닥 높이는 `62.9714`다.
- `Exit_ToLake`: `(985,62.9714,451)`, SphereCollider 반경 1.5m, trigger=true, InteractableObject 레이어. TGV_Wood_Signpost_01A를 재사용했다. 출구와 도착점 XZ 간격은 7m이며 KCC 캡슐 반경 0.35m를 제외한 여유 약 5.15m를 정적 검사했다.
- `VillageGeometry/DroughtTerrain`: 원점 `(930,20,435)`, 크기 `(70,160,110)`, heightmap 257. 원본 높이를 표본화하고 출입구를 평탄화한 독립 지형이다. 기존 Dirt_02/TerrainLayer를 재사용해 마른 바닥을 표현한다. 외부 원본/호수 TerrainLayer는 수정하지 않았다.
- `VillageGeometry/RedTreeR04`: 기존 Flow 에셋을 참조하지만 **Runner.enabled=false**. I의 지역 효과 제한 전에는 켜지 않는다. 우물 입력도 현재 조사 완료를 발생시키지 않는다. RegionInfo.flowGraphs는 빈 목록이다.
- R04_water는 기존 NpcLifePresentation/NpcLifeInteractable과 before/after/delivery 소품을 지역 복제본으로 연결했다. required=`lake.red_tree.resident_stays`, completed=`lake.red_tree.water_handed_over`. 정적 `CanPresentAction()`은 true다.
- 두 주민의 기존 GUID는 보존하고 ownerKey 씬 GUID만 새 씬으로 바뀌었다. Remaining component fileID `1819063997`, Settling `984629392`; 상세는 검사 JSON에 있다.
- 기존 공용 `Assets/03.Prefabs/Actor/Player/Player.prefab`을 도착점에 연결했다. Camera/Directional Light를 준비했다. 조명·환경 최종 바인딩은 D/I가 수행한다.
- MinimapConfig의 아이콘은 호수 설정을 복제했고 captureCenter=`(965,490)`, captureWorldSizeXY=`(70,110)`, backgroundSprite=null이다. **최종 지도 이미지 촬영은 미수행**이며 호수 배경을 잘못 표시하지 않도록 비웠다.
- 작은 지형의 외곽 차폐·낙하 방지 경계와 완성 미술은 미완성이다. 검증된 입구→우물→식수 구간 외의 자유 탐험 품질을 완료로 보지 않는다.

| 연결 | 호수 → 피해 마을 | 피해 마을 → 호수 |
|---|---|---|
| targetSceneName | `DroughtVillage` | `LakeOfLifeScenario` |
| targetArrivalId | `livingworld.drought.from_lake` | `livingworld.lake.from_drought` |
| 도착점 `_activationId` | `livingworld.drought.entrance` | `livingworld.lake.outskirts` |
| 출구 `_travelOnInteract` | true | true |
| 출구 `_requiresActivation` | false — 현장 출구를 발견/해금과 분리 | false — 귀환을 잠그지 않음 |
| 출구 `_showOnMap` | false — 반대편 목적지를 가진 출구를 지도 도착 거점으로 자동 복사하지 않음 | false |

I 등록 요청:

1. `Assets/01.Scenes/Scenario/DroughtVillage.unity`를 사용 중인 Build Profile/BuildSettings에 활성 등록한다. `Loading` 및 `LakeOfLifeScenario` 등록도 유지한다. SceneManager는 현재 Build 씬 로더이며 Addressables 씬 주소만 등록해서는 이동하지 않는다.
2. `MapConfigDatabase.asset`에 mapId=`DroughtVillage`, 신규 RegionInfo/MinimapConfig를 연결한다. `_defaultStartMapId`는 기존 호수다.
3. 지역 브라우즈 PortalEntry는 **자기 지역에 도착**하는 데이터다. 피해 지역은 targetSceneName=`DroughtVillage`, arrivalId=`livingworld.drought.from_lake`, activationId=`livingworld.drought.entrance`, requiresActivation=true, startsActivated=false. 호수 entry도 반대로 자기 지역을 가리킨다.
4. 기존 `MapConfigDatabase` Addressable 키와 콘텐츠 그룹 규약을 유지한다. 신규 씬/맵 설정을 Addressable로 추가할지는 I의 기존 배포 경로에 맞추며 중복 씬 로더를 만들지 않는다. 이 작업에서 새 Addressable 주소를 임의 발급하지 않았다.
5. 퀘스트·마커·화자·카메라·Flow 등록은 4절 이관표대로 연결한다. 두 맵에 같은 그래프를 RegionInfo와 수동 Runner 양쪽으로 등록하지 않는다.

## 3. 소비 계약과 저장 이관

### 전환

- 기존 `ISceneTransitionService.LoadScene(string,string)`를 유지한다. Actor에 구체 SceneManager 의존을 추가하지 않았다.
- PortalActor `_travelOnInteract=false`가 기본값이다. 새 두 길 표식만 true로 설정한다. 아직 해금하지 않은 기존 포털은 종전처럼 해금 입력만 수행한다.
- SceneManager는 수락 전에 중복·빈 이름·대상 및 Loading 씬 등록을 검사한다. 거부 요청은 진행 중인 PendingArrival을 쓰지 않는다.
- 수락된 요청 동안 기존 InputService를 억제하고 종료 시 이전 억제 상태로 복원한다. 도착 이후 짧은 입력 억제도 적용한다. 포털의 로컬 점유는 void 호출 반환/예외에서 풀고 전환 전체 소유는 매니저에 둔다.
- 요청한 도착 ID가 없거나 중복이거나 다른 씬 소속이면 실패한다. 빈 ID만 기본 스폰 정책이다. 기존 누락 ID의 조용한 기본 스폰 폴백이 제거되므로 I는 기존 거점 4개도 회귀 검사한다.
- 접지·카메라 준비 후 도착 오차를 검사하며 그 후에만 도착점 `_activationId`를 기존 PortalActivationState로 저장한다. 로드 요청만으로 목적지 발견을 쓰지 않는다.
- `_loadTimeoutSeconds` 기본 60초, `_arrivalTolerance` 기본 2m는 SceneManager의 직렬화 필드다. 실제 로딩 시간·플레이어 캡슐에 맞춰 검증한다.
- 실패하면 시작할 때 확보한 원래 씬·플레이어 위치·회전으로 한 번 복귀를 시도한다. 이미 시작된 Unity 비동기 씬 연산은 취소할 수 없어 활성화 대기를 풀고 완료 후 복귀한다. 복귀 자체 실패는 반복하지 않고 입력 해제와 오류를 남긴다. 실패 복귀가 원래 안전 도착점으로 연결되는지는 I의 출구 배치와 LW-T01에서 확인한다.

I 공용 계약 요청 — `Assets/02.Scripts/GameActor/Contracts/ActorServices.cs`:

```csharp
bool TryLoadScene(string sceneName, string arrivalId);
bool IsLoading { get; }
event System.Action<string> OnLoadComplete;
event System.Action<string, string> OnLoadFailed;
```

구체 SceneManager에는 위 멤버를 준비했다. 수락 bool=false는 거부, true는 요청 수락이며 도착 완료가 아니다. 실패 이벤트는 수락한 요청의 종료만 알린다. 중복 거부가 현재 요청의 실패를 발행해서는 안 된다. I가 인터페이스를 게시한 뒤 Portal/UI에서 소비한다. 동명 대체 서비스나 임시 Contracts를 만들지 않았다.

대화 중 포털은 거부한다. **진행 중 NPC 보행/호위 전체의 공용 이동 게이트는 미연결**이다. C/I는 인계·호위를 완료하거나 명시 취소한 뒤 이동하도록 연결해야 한다. 미완료 사실을 완료로 주입하지 않는다. 강제 전환의 OnDisable 정리도 별도 검사한다.

### 복사 저장

`SaveManager.TryCreateLivingWorldCopy(sourceSlot, destinationSlot, plan, out error)`만 사용한다. 실제 저장 자동 로드에 이관을 끼워 넣지 않았다. ActiveSlot·현재 플레이에 영향을 주지 않는다.

`LivingWorldMapMigrationPlan`은 **호출 1회용 메모리 입력**이며 SO·공용 저장 스키마가 아니다. I가 제공할 공용 이관 설정 타입을 대신 등록한 것이 아니다.

| 입력 | 설정 |
|---|---|
| SourceMapId / SourceSceneName | 둘 다 `LakeOfLifeScenario` |
| TargetMapId / TargetSceneName | 둘 다 `DroughtVillage` |
| IsPlayerLocationReviewed | 해당 원본 저장의 위치를 현장·진행과 대조한 경우만 true |
| PlayerDestination | null은 검토 후 현재 위치 유지. 복구할 때 기존 PartySaveData의 mapId/loadSceneName/hasLocation/playerPos/playerRot만 채운다. 검증된 피해 도착 또는 호수 귀환 도착만 지정 |
| Entities | 실제 옮긴 SceneEntityId GUID → LivingWorldEntityMove. respawn 항목이 있으면 HasVerifiedPose=true와 실제 위치/회전 필수 |

- 원본은 복호화·읽기만 한다. 원본/대상 동일, 대상 `.sav`/`.sav.bak`/`.sav.tmp` 존재, 다른 저장 진행 중은 거부한다. 고유 임시 파일을 암호화하여 재독해·내용 비교 후 원자적인 새 파일 이동을 한다.
- 확인된 스키마 `3.4`만 받는다. 자동 스키마 이관을 거치며 버전 정보를 지워 판정하지 않는다. 다른 버전은 별도 호환 검토 후 지원해야 한다.
- 위치가 있으면 source map/scene 쌍의 불일치를 거부한다. `hasLocation=false`는 기본 스폰 정책을 유지한다. 다른 맵 저장은 위치를 유지하면서 이동 엔티티 상태만 이관할 수 있다.
- A의 `(938~978,452~536)` 사각형으로 피해 구역을 추정하지 않는다. 구 저장 위치가 불명확하면 **검증된 호수 귀환 도착 좌표**를 명시한다. 아직 검증되지 않은 좌표를 plan에 넣지 않는다.
- killed/consumed의 명시 GUID만 새 map 집합으로 이동·중복 제거한다. 나머지 GUID는 원래 맵에 보존한다. respawn은 map/pose만 변경하며 actorId·횟수·시간·대기 여부를 보존한다. 대상 재스폰 키 충돌은 합치지 않고 실패한다.
- NpcLife 완료는 GlobalFlag다. 주민을 옮겼다고 새 consumed 상태를 만들지 않는다. party 명단·성장·주인공·퀘스트·story·inventory·time·flow·기존 PortalActivation은 변경하지 않는다.
- 완료 키 `lake.living_world.map_split.v1`는 암호화 사본 재독해 후 기록하고 다시 검증한다. 이미 true인 저장은 무변경 복사한다. `lake.red_tree.content.v1`과 독립이다.
- **실제 사용자 저장 복사본/안전 좌표/전체 이관 GUID 목록은 아직 미확보**다. 합성 저장의 자동 검사가 실제 구 저장 검증을 대체하지 않는다. 원본 저장에는 적용하지 않았다.

## 4. R04~R09 씬별 참조 이관안

아래 RT는 `Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/RedTree/`다. 기존 에셋은 원래 경로/GUID에 둔다. 같은 사건의 Graph/Node/Quest/Volume/Actor/Flag ID를 새로 발급하지 않는다.

| 단계 | 새 피해 마을 | 호수 유지 | I가 바꿀 참조 |
|---|---|---|---|
| R04 | well/home/field/survivor/stay/leave/water/departure/lili 및 pool/encounter/roots/truth/seola | report/assignment/local_supply/outbound_supply/sera/records_next, 지원 소품 | 각 Volume을 지역 Runner 하위에 둠. water before/after 소품, departure 두 앵커와 주민을 함께 연결. 엔트 spawn `(950,69.00279,504)`, CAM_pool/roots 지면과 구도 재검증 |
| R05 | 없음 | 기록 3곳·사본·차단 수레·우회로·쉼터·두 주민 구조와 합류 전체 | 구조의 출발→도착을 맵 경계로 자르지 않음. R05 이름에 피해가 있어도 이관하지 않음 |
| R06 | bundle/order/compare/resident/rescue/before/disconnect/after/truth/responsibility/myomyo | list/cargo·offering/water/food·세라·next | `trial_creek` 유지. 주민 안전 앵커 `(946,70.799,485)`와 표식·카메라·대역을 같은 씬에 둠. offering/water/food의 R07 하위 결과는 호수 |
| R07 | shelter/remaining, 남은 주민 식량 결과 | affected·미아 대피·R05 인계·돌봄·재고/몫·외곽 | remaining 입력과 afterVisual은 새 맵. 호수 `remaining_owned`는 동일 완료 플래그를 읽는 before-only 표시. affected는 호수 외곽 인계이며 remaining과 합치지 않음 |
| R07~08 | trial_creek 사실만 소비 | west_relay/south_channel/shrine_link, 신사·전투·중앙 관찰 | 루시아/미아 기존 NPC 몸 참조 유지. victory, attacks_cleared, absorption.stopped/observed 분리 |
| R09 | wishes/receipt/water/channel/route/observe/revisit_field와 해당 묘묘·릴리·설아·화린·유라 대역 및 앵커 | begin/stock/pack/cart/load/transport/depart/meal/revisit/cost/hint/guide, PendingReceipt | receipt의 beforeVisual(호수)은 새 맵 직접 참조에서 제거하고 호수 표시기가 handed_over 소비. afterVisual은 우물의 받은 짐. channel/route 대역과 before/after 앵커 동반 이동 |

주의할 현재 연결:

- R07 remaining: 호수 `R07_remaining_owned` → 피해 `R07_remaining_delivered`를 직접 참조하지 않도록 두 표시기로 분리한다.
- R07 affected afterVisual fileID `1385192498`은 R09 루트 아래에 있지만 호수 `(1045,60.576,483)`에 남는다.
- R09 receipt beforeVisual fileID `170145474`는 호수 `R09_AffectedPendingReceipt`, afterVisual `1555843222`는 새 피해 우물이다.
- R09 channel actor `1120061492`, route actor `1881134161`과 모든 앵커를 함께 옮긴다. 조작 중인 본인으로 완료하는 기존 분기를 유지한다.
- R09 포장/적재/출발은 현재 `(1003,62.455,436)` / `(1002,62.504,440)` / `(1002,62.401,444)`다. 과거 문서 좌표로 되돌리지 않는다.

| 그래프 | ID / GUID | 기존 시작 |
|---|---|---|
| RT/R04/FLOW_RedTreeVillageWaterReport.asset | `flow_red_tree_village_water_report` / `efb5d12b5b1ef4a46aa595d0c893e9af` | MapReady |
| RT/R05/FLOW_RedTreeRecordsPassage.asset | `flow_red_tree_records_passage` / `d1fa92084b3cb9041b575611b67ab1a9` | MapReady |
| RT/R06/FLOW_RedTreeMarksSupply.asset | `flow_red_tree_marks_supply` / `385a116121b09e14c9fb2c43a068efae` | MapReady |
| RT/R07R08/FLOW_RedTreeResponseFinale.asset | `lake.red_tree.response_finale` / `6abeb5250486e284f9cba48f0b1372d1` | Resume |
| RT/R09/FLOW_RedTreeReturn.asset | `lake.red_tree.return_rebuild` / `3668aa3e0dabd044c8cbd728c2d48341` | Resume |

I는 최초 진입뿐 아니라 FlagsReloaded/플래그 변경/미완료 조우 재개에도 지역 조건을 넣는다. 호수에서 `creek_started=true, creek_cleared=false` 로드 시 엔트가 생기면 실패다. 누락 Volume의 Failed를 완료로 연결하지 않는다. Flow firedKeys/entries는 그대로 보존한다. 그래프 분리가 꼭 필요하면 기존 키 대응표를 B에 먼저 전달한다.

SceneEntityId 이관 대상 기준은 A 5.2 표다. 이동 전 GUID를 확보하고 새 씬을 저장한 후 `EditorSetGuid(기존GUID)`로 **새 ownerKey**를 맞춘다. 옛 ownerKey 문자열 고정은 금지한다. R04 두 주민은 `246895bd33e24c92ad5e4b8b548345e4`, `efe248fe56b44d19a27886ff5d9d3dab`; R06 두 대역, R09 다섯 대역은 A 표와 대조한다. 다른 채집/몬스터를 추가 이관하면 목록을 확장하며 이름으로 GUID를 추정하지 않는다.

## 5. 검사와 재현 절차

### 자동 검사

전용 EditMode fixture: `UPlayGround.Core.Tests.LivingWorldMapMigrationTests`, `UPlayGround.Core.Tests.SceneLoadOwnershipTests`. 기존 테스트 asmdef에 들어가며 공용 경계를 수정하지 않도록 Assembly-CSharp 실행부는 reflection으로 호출한다.

검사 내용: 원본 불변·GUID 집합 이동·알 수 없는 GUID 보존·respawn 필드 보존·위치 미검토/씬 불일치/버전 거부·기본 스폰 보존·확인 도착 적용·멱등·중복 respawn 거부·빈 복사 슬롯 암호화/재독해·기존 sav/bak/tmp 거부·활성 슬롯 보존·중복 요청의 도착 소유·중복 도착 ID 거부. 실행 결과는 최종 기록을 따른다.

I 통합 후 기존 RedTreeR04 Content/Facts/Consent, R05~R09 Content/Facts 및 NpcLifeActionTests를 관련 범위로 실행한다. 테스트 검색 결과 0개를 통과로 기록하지 않는다.

### 사용자 직접 테스트 — 전부 결과 대기

준비: I의 통합 버전/씬 해시, 게임패드와 키보드, 원본 저장과 별도 빈 슬롯 번호를 기록한다. 원본 sav/bak의 해시를 먼저 남기고 복사본만 선택한다. 테스트 뒤 원본 해시가 같아야 한다. 정상 진행 저장과 상태 주입 fixture를 구분한다. I 연결 전 아래 테스트를 통과 처리하지 않는다.

| ID | 조작 | 기대 결과 / 실패 기준 |
|---|---|---|
| LW-T01-a | 호수 출구 접근 → 상호작용 1회 → 로딩 중 연타 → 도착 후 3초 대기 → 귀환 출구 입력. 10회 반복 | 발만 들어서면 이동하지 않음. 올바른 씬·도착 방향, 한 플레이어, 접지·카메라 준비, 자동 재전환 없음. 시작/완료 이벤트 요청당 1회, 입력 잔류 없음 |
| LW-T01-b | 별도 테스트 복제 배치에서 빈/미등록 목적지, 잘못된 arrivalId, 중복 arrivalId를 각각 요청 | 조기 거부는 현 씬 유지·재시도 가능. 수락 뒤 도착 실패는 원래 씬으로 복귀, 진행 플래그 보존, 목적지 발견 없음. 설정 복구 후 재시도 성공 |
| LW-T01-c | Loading 진입 후 CancelCurrentLoad 및 준비 타임아웃을 각각 재현 | Unity 연산 활성화 대기를 풀고 원래 씬으로 한 번 복귀. 입력·로딩 덮개·카메라 잠금 잔류 없음. 복귀까지 걸린 시간 기록. 복귀 자체 실패도 원인 로그와 함께 별도 실패로 남김 |
| LW-T02 | 퀘스트 수락 전 우물 정상 조사·대화 종료 → 호수 귀환 → Q03 수락 → 재방문. 대화 취소 저장도 별도 반복 | well_examined 선행 누적, 완료 조사 중복 없음. 취소를 완료로 소비하지 않음. truth.cost 등 후반 사실 조기 확정 없음. 목표/지도 위치가 피해 맵 |
| LW-T03-a | resident_stays 정상 획득 후 식수 입력 → 인계 도중 취소/씬 이탈/저장 후 로드 → 재시도 → 완료 후 재입력 | 미완료는 재시도, 완료는 실제 받은 물통 표시와 water_handed_over 일치. 물자·몸·보상 복제 없음 |
| LW-T03-b | departure/구조 주민 보행 중 대화·취소·출구 시도 → 저장/로드 | 미완료 이동을 완료 사실로 바꾸지 않음. 입력·NPC 예약 해제, 안전 지점에서 재개. R05 구조는 호수 안에서 완료 |
| LW-T04-a | 피해 구역 안/호수 일반 위치/경계 불명/다른 맵/hasLocation=false 정상 구 저장 각각을 새 슬롯으로 복사 | 검토된 피해는 피해 도착, 불명확 경계는 호수 귀환 도착, 일반 호수·타 맵·기본 스폰 유지. 원본 해시 동일. 지형 관통/낙하 없음 |
| LW-T04-b | R04 중간·완료, R05 구조 중, R06 표식 일부, R07 배급 일부, R08 승리·미관찰, R09 인계 전후·결말 저장을 각각 복사 → 로드 → 왕복 → 재로드 | Quest/Flow/주민 선택/영입/물자/시간/성장 유지. 이동 GUID의 world 상태만 새 맵 소속. 미지 GUID 삭제 없음. 완료 키는 한 번, 재이관 무변경 |
| LW-T10 | R04→R05→R06→R07/08→R09를 두 맵에서 정상 진행. 역순 조사·대화 취소·중간 로드를 끼우고 결말 뒤 피해 밭 재방문 | 위치/마커/카메라/볼륨/대역 누락 없음. 피해 지역 밖 생성/대화 없음. 중앙 흡수 관찰과 외부 물길 관찰 분리, 의사 확인과 실제 인계 분리, common_ending 뒤 자유 왕복 |

실패 회신: `항목 ID / 통과·실패·미수행 / 실제 결과 / 시작 슬롯·통합 버전·입력 장치 / 오류 로그`. 도착은 좌표·SceneContext.MapID·요청 arrivalId를 함께 남긴다.

### 최종 기록

| 검사 | 결과 / 근거 |
|---|---|
| Unity 6000.3.21f1 컴파일 | 최종 전용 타입 로드 성공, scriptCompilationFailed=false, 컴파일 오류 검색 0. 중간 C/D 파일 작성 시점의 미완성 타입 오류는 해소된 뒤 진행 |
| 최종 EditMode | **15/15 통과, 실패 0, 건너뜀 0**. B 전용 9개 + RedTreeR04ContentTests 6개. 실행 2026-10-22 22:44 KST, job `11b5ab6e796d45a29c2f5db1ee1a749a`. `output/LivingWorld/B/FinalEditModeTests.json` |
| 암호화 복사 슬롯 | 합성 3.4 원본 1001→1002, 재이관 사본 1002→1005 성공. 원본 바이트·ActiveSlot 보존, 기존 sav/bak/tmp 거부. OS 임시 전용 폴더만 사용하고 테스트에서 생성 파일 모두 정리. 실제 플레이 저장 시험은 아님 |
| 새 씬 Unity 로드 | PreviewScene에서 Missing Script 0, 다른 씬 직접 참조 0, 두 SceneEntityId GUID 보존·새 ownerKey 확인. 원본 씬 dirty=false |
| 도착 물리 보조 검사 | 실제 플레이어 Capsule과 새 씬 활성 비트리거 Collider 간 ComputePenetration 겹침 0, 귀환 트리거 여유 5.1507m. KCC 실제 접지/카메라 플레이 검증과 구별 |
| NavMesh | 새 데이터만 월드에서 10km 떨어뜨려 임시 등록해 원본 호수 NavMesh 혼입을 배제. 도착→우물/식수/귀환 3개 모두 SamplePosition 성공, PathComplete. 임시 NavMesh instance 해제 |
| 씬 정적 결과 | `output/LivingWorld/B/SceneValidation.json`. 재현 코드는 같은 폴더 `SceneValidationCode.txt`; 읽기 검사·preview·캡처만 수행하며 씬 저장 없음 |
| 시각 확인 | `output/LivingWorld/B/DroughtVillage_Overview.png` 1280×720 Preview Camera 렌더를 확인. 기존 집·우물·마른 바닥·밭·개울 배치 확인. 완성 미술·플레이 시점 가독성 판정 아님 |
| 본편/공용 소유 파일 | 본편 SHA-256은 착수 값과 동일. B는 DB/Addressables/BuildSettings/Contracts/Data/기존 Flow·Quest 에셋을 수정하지 않음. 동시 C/D 변경은 보존 |
| LW-T01~04/T10 | **사용자 테스트 대기**. I가 호수 출구·귀환 도착·지역 Flow·등록을 연결해야 실제 왕복/조사/인계/후반 진행을 검증할 수 있음 |
| 실제 구 저장 이관 | **미수행**. I의 최종 이동 GUID 목록·검증된 양쪽 안전 좌표·정상 구 저장 표본 필요. 합성 복사 시험만 통과 |
| Editor Play Mode / Windows Player | **미수행**. 본편 두 맵 미연결. 최종 등록/통합 후 I/V의 Player 빌드 및 실제 두 맵 실행이 필수. Boot 빌드로 대체하지 않음 |

중간 도구 문제도 최종 성공과 구별한다. 공유 부모 폴더가 아직 임포트되지 않은 상태에서 첫 Terrain 생성이 실패해 생성 중인 빈 씬을 닫고 B가 만든 빈 중복 폴더만 제거했다. 폴더 임포트 후 제작은 성공했다. MCP의 같은 코드 재시도는 기존 씬 덮어쓰기 가드에서 거부됐으며 결과 파일/Unity 재로드로 생성 성공을 확인했다. 최종 테스트 직전 도메인 리로드 구간의 MCP 연결 실패는 재연결 후 위 job으로 재실행했다. 컴파일 오류 상태에서 에셋을 저장하거나 본편 씬을 재생성하지 않았다.

보관 산출물은 새 씬·지형·NavMesh·설정 및 작은 JSON/PNG뿐이다. 프로젝트 복사본·Player 빌드를 만들지 않았다. 커밋하지 않았다.

## 6. 다른 담당 요청과 미해결 위험

- I: 공용 전환 수락/결과 계약 게시, 기존 네 도착 ID 회귀, 호수 출구/귀환 도착 실측, 새 맵 등록, Flow 지역 제한, Quest/화자/마커/카메라 재결선, 최종 위치·GUID 목록 승인.
- C: 서사 인계/주민 보행 중 전환의 완료 또는 명시 취소 정책. 새 일과가 저장된 주민 선택·인계를 덮지 않게 할 것.
- D: 새 씬 조명 바인딩·환경 프로필은 I 통합 때 연결. B 지형은 기존 TerrainLayer/미술 자산을 재사용하고 기존 물/흙 재질을 수정하지 않는다.
- 실제 호수 왕복·후반 참조·안전 위치가 미검증이면 배포 가능한 B 완료로 판정하지 않는다. 입력 게이트/복귀 경로가 실제 Player에서 검증되기 전 실패 복구를 보장한다고 표현하지 않는다.
