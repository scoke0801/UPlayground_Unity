# 월드와 아트

**RiverBasin4km 평지 기복·B3 협곡 설계(2026-11-22, 설계만).** 평지가 평평하게 읽히는 원인을 저장 RAW로 측정했다. 경사 중앙값은 7~8°지만 호수 마을 → B3 평지는 6m 이상 단절이 0%다. 남서 지류 B3 협곡(수면 27m 낙차)을 파일럿으로 4단 폭포·현수교·바닥 탐험 레인을 설계했다. 지형·씬 변경 없음, 수치는 G0 Editor 실측 후 확정, 전부 사용자 테스트 대기다. [설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_GORGE_RELIEF_DESIGN.md) · [작업 상태](../work-status/river-basin-gorge-relief.md)

**RiverBasin4km 첨부 지도 범위.** 사막을 동쪽 호수 동편과 남동부까지, 설산을 북쪽 산줄기 전역으로 확장했다. 숲 연못·서남 호수·동쪽 호수에 섬 네 곳과 발견 보상을 추가했다. 최종 지도와 수영 표면, Editor Play 보상 4곳 등록을 확인했다. 수영 왕복과 실제 획득·재방문은 사용자 테스트 대기다. [지도 전후](../work-status/river-basin-marked-regions-evidence/map-before-after.jpg) · [반영과 검증](../work-status/river-basin-marked-regions.md)

**RiverBasin4km 호수·동부·설산 탐험.** 중앙섬 두 곳을 각각 3.2배·2.7배로 넓혔다. 채석 단구, 돌산 능선, 사막 모래골, 설산 허리길과 계곡 전망을 저작하고 지도를 갱신했다. 수영 단면 33개와 Terrain 경계 112개 검사 통과, Editor Play 발견 보상 등록 5/5 확인. 마커 없이 길 찾기, 수영 왕복, 실제 획득·재방문은 사용자 테스트 대기다. [지도 전후](../work-status/river-basin-exploration-evidence/map-before-after.jpg) · [반영과 검증](../work-status/river-basin-exploration.md)

**RiverBasin4km 남부 호수 섬.** 두 호수의 보행교 10개와 남안 B5를 철거하고 수영 판정을 연결했다. 버드나무·바위·갈대 군락으로 섬과 호안을 꾸미고 상륙로를 열었다. 저장본의 입수·상륙 33개 단면 검사 통과, 미니맵 갱신. 수영·비행 조작과 경관은 사용자 테스트 대기다. [전후 화면과 검증](../work-status/river-basin-world.md#4km-남부-호수-섬)

## 본편 월드 제작 지침

**RiverBasin4km 지도 재촬영 · 2026-11-22.** 저장된 씬에서 정북 지도 PNG를 다시 촬영하고 Unity 재임포트와 기존 스프라이트 참조를 확인했다. [현재 지도](../../Assets/10.Datas/Map/RiverBasin4km/Minimap.png)

**사막·설산 공용 발자국.** 평가된 발 포즈와 마지막 접지 위치를 추적해 출발·정지·공격에서 새로 디딘 발을 기록한다. 두 지형의 전환과 걷기·달리기 회귀를 Editor Play로 재검증했다. 자세가 안정된 정지 상태에서 추가 기록 0회, EditMode 27/27 통과다. [변경과 검증](../work-status/river-basin-sand-footprints.md#출발정지공격의-새-접지)

**RiverBasin4km 북부 설산.** 채석 요새 접근로에서 폭포 동쪽을 거쳐 고원 순환로까지 1.98km의 동선을 열었다. 동선 폭 6m의 최대 표본 경사는 43.6°이며, 전체 112개 타일 경계 높이 차는 0m다. 4개 타일에 근경 눈 노멀, 눈 발소리·눈가루와 사막 공용 발자국 캐시를 연결했다. 라온으로 눈 표면 전환과 타일 경계 양쪽의 발자국을 확인했다. 전체 동선 양방향·착지·시청각 인수는 사용자 테스트 대기다. [작업 상태](../work-status/river-basin-north-alpine.md)

**RiverBasin4km 미연결 구역 동선(2026-11-22).** R1~R5 및 연못·선착장 가지길의 G2 블록아웃을 씬에 저장했다. R3는 요새 아래 R5로 합류하고 R4는 남안에서 수영·비행으로 유출 하천을 건넌다. R4 하강 경사·실제 이동·갈림길 가독성은 사용자 테스트 대기다. [설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_OFFROUTE_REGIONS_DESIGN.md) · [작업 상태](../work-status/river-basin-world.md#4km-미연결-구역-동선-설계)

**RiverBasin4km B1 수중 합성.** 각도에 따른 반사와 청록색 광학 프로필로 B1의 남색 단색 띠를 완화했다. Editor의 수평 잠수·수면 올려다보기·수면 위 고정 구도를 비교했다. 실제 잠수와 재부상은 사용자 테스트 대기다. [수정 전후와 검증](../work-status/river-basin-world.md#4km-b1-수중-합성-개선)

**RiverBasin4km 동부 사막과 발자국.** 동부 모래 권역을 Terrain 여섯 타일, 약 0.405㎢로 확장했다. 기존 분지에서 서쪽 사면과 동쪽 협곡 바닥으로 이어지며 모래 발자국과 발소리를 공유한다. 높이·수신 마스크 경계 오차 0, EditMode 21건 통과. [확장 지도](../work-status/river-basin-sand-footprints-evidence/river-basin-desert-expanded-map.png) · [설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_SAND_FOOTPRINTS_DESIGN.md) · [구현과 검증](../work-status/river-basin-sand-footprints.md)

**월드 안개 조사(2026-11-21).** `RiverBasin4km`의 전환 전 기준은 지수 제곱 거리 안개였다. `LakeVillageWorld` 저장본에는 선형 안개(110~1,250m), 호숫가 하늘 프로필, 폭포 입수 물보라의 `Mist` 파티클이 연결돼 있다. [전환 전 4km 씬 조사](../work-status/river-basin-world.md#4km-거리-안개-조사) · [호수 씬](../../Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity) · [호숫가 하늘 프로필](../../Assets/10.Datas/World/OpenWorld/LakeVisualV2/LakeShoreSky.asset)

**툰 안개.** PC Renderer에 거리 팔레트·높이 적분·국소 부피를 연결하고, 사막 마을의 날씨·플레이어 기준점·교전 입력을 연동했다. 일곱 지역 프리셋과 비교 씬을 추가했다. EditMode 44/44 통과. 밀도 0의 RGBA 중립성과 직교 카메라 제외, 사막 마을·관리 수역 고정 화면을 확인했다. 외곽선·식생·HUD와 실제 플레이, GPU·Player 검증이 남아 있다. [통합 계약](../../Assets/docs/design/world-art/environment/TOON_FOG_INTEGRATION.md) · [검증 기록](../work-status/toon-fog/Validation.md) · [단계별 상태](../work-status/toon-fog/TaskStatus.md) · [비교 씬](../../Assets/01.Scenes/ArtReview/ToonFogValidation.unity)

**RiverBasin4km 툰 안개.** Built-in Fog를 끄고 4km 전용 프로필과 기존 날씨 컨트롤러를 연결했다. B1 진입로와 수면 고정 시점에서 수역 합성 경로의 적용을 확인했다. 실제 플레이 가독성과 수면 경계는 사용자 테스트 대기다. [4km 적용 상태](../work-status/toon-fog/TaskStatus.md#riverbasin4km-적용) · [전후 화면](../work-status/toon-fog/Validation.md#riverbasin4km-고정-카메라)

[오픈월드 맵 제작 가이드](../../Assets/docs/guide/OPENWORLD_MAP_AUTHORING_GUIDE.md)에서 지역의 탐험 목표, 랜드마크, 이동 동선을 정한다. 거점과 주변 동선을 블록아웃으로 주행해 본 뒤 아트와 콘텐츠를 배치한다.

**RiverBasinWorld · 4km 제작용 지형.** 북부 산맥과 동부 협곡을 4km 월드 좌표에서 새로 만들고, 같은 씬에 제작 TerrainData 64장·지형 재질·타일별 수면을 연결했다. Editor 재개방에서 경계 112곳과 콜라이더·수면 참조를 확인했다. [공간 설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_WORLD_REBUILD_DESIGN.md) · [4km 씬과 검증](../work-status/river-basin-world.md#4km-제작용-heightmap-적용)

**북부 산맥 원경.** Terrain 원경 전환 거리를 늘린 비교에도 회색 면이 남아, 북부 지형 21장의 바위·절벽·적설 배합을 보정했다. [원인과 검증](../work-status/river-basin-world.md#북부-산맥-원경-재질-보정)

**동부 사면 반복.** 사용자 구도에 드러난 그리드 반복을 줄이도록 동부 타일 16장의 암석·풍화암·건조 지면 배합을 바꿨다. [같은 구도의 비교와 적용](../work-status/river-basin-world.md#동부-사면-재질-반복-보정)

**타일 이음새·합류부·설산.** 64개 지형의 공유 재질 경계를 블렌딩하고, 합류부와 도로 사면을 보정한 높이·수면을 저장했다. 설산은 새 적설 텍스처와 고도·경사 마스크를 사용한다. [Unity 적용과 검증](../work-status/river-basin-world.md#타일-이음새합류부설산-보정)

**RiverBasin4km B1 석교와 상류 수영로.** 4km 씬에 P04 석교·착지, P05 수역과 양안 자갈길, 플레이어·TPS 카메라·물소리를 연결했다. 4km 씬을 단독으로 Play하면 B1 접근로에서 시작한다. 나머지 교량, 북부 폭포, 동부 사암 사막, 서부 가을 숲은 후속 구간이다. [B1 적용과 검증](../work-status/river-basin-world.md#4km-b1-석교와-상류-수영로) · [게임월드 제작 설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_WORLD_REBUILD_DESIGN.md#4km-게임월드-제작)

**RiverBasin4km 북부 2단 폭포(2026-11-22).** 본류가 초원 한가운데서 끊기던 상류 끝(X 2844 / Z 3418)에 바위 틈 샘 → 상단 폭포 9.4m → 얕은 립 웅덩이 → 본 폭포 11.2m(폭 13m) → 수심 4m 낙수 웅덩이 → 기존 본류를 연결했다. 지형은 WorldBuilder Sculpt 명령으로 기록해 재생성에도 남는다. 가상 패드 Play 3경로(웅덩이 횡단 왕복, 하류 출구, 립에서 낙하 후 수영·상륙) 모두 통과했다. 미술·물소리 청감은 사용자 테스트 대기다. [적용과 검증](../work-status/river-basin-world.md#4km-북부-2단-폭포) · [설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_WORLD_REBUILD_DESIGN.md#북부-본류-폭포)

**RiverBasin4km 다음 제작.** B1 성능과 청크 로딩을 먼저 계측하고 호수 마을 → B3 → B1 → 중앙 성채 정문을 도보로 왕복할 수 있게 연결한다. 본선을 인수한 뒤 B2·B4로 순환로를 닫고 폭포·사막·숲으로 확장한다. [제작 순서와 인수 조건](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_WORLD_REBUILD_DESIGN.md#b1-이후-제작-방침) · [현재 상태](../work-status/river-basin-world.md#4km-다음-제작-방침)

**RiverBasin4km 서부 숲 식생 우선 검증.** 숲 연못 동쪽의 약 126m Play 구간에 0.5m 격자의 낮은 풀을 보강했다. 목서 사이에 자작나무·너도밤나무와 어린 나무 군락을 섞어 높이와 수관을 다양화했다. [검증 씬과 왕복 결과](../work-status/river-basin-world.md#4km-서부-숲-식생-검증) · [잔디 보정 전후](../work-status/river-basin-world.md#사용자-제보에-따른-잔디-밀도-보정) · [수목 후보와 배치](../work-status/river-basin-world.md#수형-다양화와-어린-나무)

**RiverBasin4km 숲 연못 동쪽 제작 적용.** 126m 첫 구간에 수목 219그루, 풀 65,277개와 관목 26개를 옮겼다. 가상 패드로 숲길과 상주 타일 경계를 왕복했다. 1080p Editor의 CPU·GPU p95가 16.7ms 목표를 넘었고, 거리·밀도 축소로 개선되지 않아 풀 90m·표시 밀도 100%, 나무 340m·빌보드 95m를 다음 숲길의 비교 기준으로 둔다. 정규 청크 재진입과 미술 인수는 대기 중이다. [제작 씬과 검증 근거](../work-status/river-basin-world.md#4km-숲-연못-동쪽-식생-적용)

**RiverBasin4km 서부 가을 숲 전역.** X 440~1450 / Z 1600~2900과 사당 능선에 새 수목·관목 18,447개와 수관 아래 성긴 풀을 결정적 스크립트로 배치했다. 파일럿 구간은 보존했고 경계 밖 50m는 밀도를 서서히 맞췄다. 핵심 숲의 수관 피복은 55%이고 길·수면·부지 침범은 0이다. 대형 수종의 줄기 충돌도 확인했다. 가상 패드로 네 경로를 왕복해 막힘 0과 안정 지지 100%를 확인했다. 1080p Editor의 CPU·GPU p95는 16.7ms 목표를 넘고, 원인 미분류인 1.8~8.9초 프레임 정지가 있다. 미술 인수와 Player 계측은 대기 중이다. [배치·검증·남은 항목](../work-status/river-basin-world.md#4km-서부-가을-숲-전역-식생)

**서부 숲 식생.** 지형 12장에서 숲 경계 안 풀 인스턴스 2,771,383개와 프리팹 11종을 확인했다. 4km 씬에는 날씨와 목서 6종의 나무 승격을 연결했다. 목서 6,118그루는 평소 TerrainTree로 표시하며, 반응형 실물의 상한은 32개다. 이동 성능과 사당 능선 수관 색, 벌목·연소의 실제 조작은 사용자 테스트 대기다. [풀 비교 캡처](../work-status/river-basin-world.md#지면-식생-자연화-사용자-제보--2026-11-21) · [승격 설계](../../Assets/docs/design/world-art/environment/TERRAIN_TREE_PROMOTION_DESIGN.md) · [구현·검증 상태](../work-status/river-basin-world.md#반응형-수목-승격-설계와-play-실측)

| 제작 단계 | 지침 |
|---|---|
| 지형과 수계 | [지형 저작](../../Assets/docs/guide/WORLDBUILDER_TERRAIN_AUTHORING_GUIDE.md) · [수계 저작](../../Assets/docs/guide/WORLDBUILDER_WATER_AUTHORING_GUIDE.md) · [지형·물 아트 작업 교훈](../../Assets/docs/guide/TERRAIN_WATER_ART_LESSONS.md) |
| 거점·도로·교량 | [월드 계획 저작](../../Assets/docs/guide/WORLDBUILDER_PLANNING_AUTHORING_GUIDE.md) · [카메라 가림 체크리스트](../../Assets/docs/guide/CAMERA_OCCLUSION_FADE_GUIDE.md) |
| 식생·소품·콘텐츠 배치 | [바이옴·식생·동굴](../../Assets/docs/guide/WORLDBUILDER_ECOLOGY_AUTHORING_GUIDE.md) · [자동 배치와 Override](../../Assets/docs/guide/WORLDBUILDER_PLACEMENT_AUTHORING_GUIDE.md) · [NPC·적·포털 배치 도구](../../Assets/docs/guide/MAP_PLACEMENT_TOOL_GUIDE.md) |
| 발행과 플레이 검증 | [Bake·발행](../../Assets/docs/guide/WORLDBUILDER_BAKE_AUTHORING_GUIDE.md) · [청크 스트리밍](../../Assets/docs/guide/WORLDBUILDER_STREAMING_AUTHORING_GUIDE.md) · [C 월드 검증 사례](../work-status/open-world/WORLDBUILDER_P10_STREAMING.md) |

2026-11-19 **WorldToon 경량 월드 툰 셰이더군 구현(G0~G6 Editor 범위)**: `Assets/06.Shaders/WorldToon/`에 Mesh(Lite/Standard/Far)·Foliage·Terrain4(+원거리 Basemap) 셰이더, `Tool/Editor/WorldToon/`에 분석·복제·적용·복구·Terrain/식생 검사·검증 씬 도구를 두었다. 대표 변형 300개 D3D 컴파일 PASS, EditMode 8/8 PASS, 검증 씬 낮·밤 캡처로 그림자·추가 광원·데칼·Terrain 구멍·5레이어 차단을 확인했다. 실제 월드 적용은 없다: NPC·몬스터는 lilToon 보호, 식생은 메시 Read/Write 꺼짐, 주요 지형은 4레이어 초과로 보류했고 바위·소품(URP/Lit)만 후보다. GPU 성능은 `NOT_MEASURED`, Player 빌드 항목(T26~T28)은 `사용자 지시 대기`, 외형은 `사용자 테스트 대기`. 판정·근거는 [작업 상태](../work-status/worldtoon.md), 프로젝트 결정은 [통합 계약](../../Assets/docs/design/world-art/environment/WORLDTOON_EXISTING_INTEGRATION.md)과 [결정 기록](../work-status/worldtoon/Decisions.md), 원문은 [전달 패키지](../전달용/WorldToon_Handoff/START_HERE.md).

2026-11-19 **WorldBuilder P05 수계 저작**. 강·호수·바다·폭포를 SO로 저작하고 비파괴 지형 조형, URP 수면, 물리 수역 조회와 기존 수영 어댑터를 적용했다. 전용 2km 검증 씬의 512m 첫 확인 구역에서 수면과 폭포를 Play Mode로 확인했다. 최종 P05 EditMode **28/28 통과, 실패0·건너뜀0**이다. 실제 KCC·저작 UX·미술·청감은 사용자 테스트 대기다. [P05 검사·화면·인계](../work-status/open-world/WORLDBUILDER.md#p05-종료-인계), [수계 저작 지침](../../Assets/docs/guide/WORLDBUILDER_WATER_AUTHORING_GUIDE.md).

2026-11-18 **WorldBuilder P04 구현·자동 게이트 통과**: Region/POI·부지·Spline 도로·필수 교량 연결, 계획 SO/지형/씬 Undo와 실패 복원을 구현했다. 전체2km 규격을 유지한 별도512m 검증 구역에서 실제 연결 화면을 확인했고, P04 종료 시점 **91/91 통과(실패0·건너뜀0)**, 실제128m/512m Agent 교량 횡단·계획 이동/삭제/첫 적용 Undo/Redo·실패 복원·GUID 보존을 확인했다. 독립 QA가 최종 원문과 코드56/원본210/검증 자산98개 지문을 대조했다. [P04 상태·검사](../work-status/open-world/WORLDBUILDER.md#p04-종료-인계), [P04 저작·사용자 테스트](../../Assets/docs/guide/WORLDBUILDER_PLANNING_AUTHORING_GUIDE.md).

2026-11-17 **WorldBuilder P03 구현·자동 검사 통과**: 전역 후보·안정 ID/sourceIdentity·회전 footprint 공간 해시·실제 필터·수동 Override와 P02 Dirty 연결. 최종 관련 **EditMode60/60**, Delete/변환 각20회 Undo/Redo·Dirty/Full·저장/재로드·결합 실패 복원 확인. 독립 QA 지적을 수정·재검증했다. [코드·데이터·검증·남은 항목](../work-status/open-world/WORLDBUILDER.md#p03--공통-배치-기반-2026-11-17), [배치 저작/테스트 지침](../../Assets/docs/guide/WORLDBUILDER_PLACEMENT_AUTHORING_GUIDE.md).

별도 [P03 검증 씬](../../Assets/WorldBuilder/Worlds/d303d303d303d303d303d303d303d303/P03Validation.unity)은 2,048m·2049²·700m 규격을 유지하고512m 구역에 기본 소품250개를 저장했다. 경계 양쪽123/127개·고정 footprint/spacing·Delete 부재·Pin 보존·실제 카메라 캡처 확인. B/C·전달본145파일 SHA 변경0. Dirty는 전역 재평가를 사용하며 부분 갱신 성능 최적화는 후속이다. 실제 창/Scene UX·기존 도메인 리로드/디스크 실패·미술 인수는 사용자 테스트 대기다. P03 종료 당시에는 P04 착수 전이었다.
2026-11-16 WorldBuilder 미리보기 광택 제보 수정: RGBA 알파1이 최대 smoothness로 해석돼 금속처럼 보였다. B/C 전용 TerrainLayer와 생성 코드를 **매끄러움 상수0**으로 수정했고 실제 Unity 전후 화면에서 반짝임 제거를 확인했다. 지형/원본34파일 SHA 변경0. [수정·검증·사용자 확인](../work-status/open-world/WORLDBUILDER.md#사용자-제보--미리보기-지형-광택-수정-2026-11-16).

2026-11-16 [빌드 식생 검정 원인·수정](../work-status/vegetation-build-black.md#지정-빌드-내부-분석과-수정--2026-11-16): 지정 Player 내부15종에서 SSAO off 변형0, 관련 식생·Terrain/Lit의 DBuffer off도0임을 확인했다. Quality RP 사전 필터가 원인이어서 기존 호수 Renderer를 기본 RP의 대체 Renderer로 등록하고 렌더 설정을 콘텐츠/Player 지문에 반영했다. 수정 전 실패 재현·수정 후 **BuildManager EditMode33/33 통과**, 같은 식생 셰이더의 DBuffer 입력 없는 on 변형에서RGB0/정상 off 변형에서초록색을 GPU로 재현했다. 이전 주변광 후보는 근거 불충분으로 보류. 임시 분석 자료5.10GB 제거·작은 근거 보존. 새 Player/Addressables 빌드·수정본 화면은 사용자 테스트 대기다.

2026-11-16 WorldBuilder 지역형 HeightMap: [설계 정본](../../Assets/docs/design/world-art/open-world/WORLDBUILDER_REGIONAL_HEIGHTMAP_DESIGN.md)에 따라 **P00 조사·P01 수치 기반·P02 비파괴 저작 구현과 자동 검사 통과**. [작업 상태·자산·검증·남은 확인](../work-status/open-world/WORLDBUILDER.md), [지형 저작 지침](../../Assets/docs/guide/WORLDBUILDER_TERRAIN_AUTHORING_GUIDE.md).

P01은 B(−80/480m)·C(0/700m)를 각각4×4 Terrain으로 저장하고 재로드·방향 화면을 확인했다. C700m의 경사 중앙값30.31°/p90 60.04°/p99 73.94°/최대83.28°여서 후속 필수 동선 조형이 필요하다. P01의 실제 KCC·동적 LOD·미술 인수는 사용자 테스트 대기다.

P02는 TerrainOperation·Sculpt/RAW 세션·Undo/Redo·Dirty DAG를 구현했다. **최종 EditMode38/38 통과**, 높이/Hole 각20회 Undo/Redo·Dirty/Full bit 동등·타 객체/native 브러시 Undo 격리 확인. 툴 런처 **월드 / 맵 → WorldBuilder 지형 저작**, C 저작 정의 연결 완료(명령0·씬dirty=false). P01파일94개와 B원본24파일 SHA 보존. 실제 브러시/외부 편집기 UX·디스크 중간 실패·실제 도메인 리로드 실험은 미수행/사용자 확인 대기다. P02 종료 당시 P04 이후·PlayMode·Player/Addressables 빌드는 미수행이었으며 컨셉 PNG를 Terrain 입력으로 사용하지 않는다.

미니맵 물 가독성 수정(2026-11-15): 활성 물 표면7개에 밝은 파랑 불투명 재질을 임시 적용해 재촬영·배경 연결했다. 촬영 후 원래 재질7/7 복원 확인, 씬·품질 설정 보존. [현재 지도·검증](../work-status/lake-waterfall.md#폭포-반영-미니맵-재촬영--2026-11-15).

폭포 미니맵 갱신(2026-11-15): 현재 호수 월드를3600×4200 재촬영해 LakeVillage MinimapConfig 배경에 반영했다. 기존 GUID·중심·범위 유지, 폭포 원본 픽셀과 임포트 참조 확인. 실제 HUD 가독성은 사용자 확인 대기. [작업 기록](../work-status/lake-waterfall.md#폭포-반영-미니맵-재촬영--2026-11-15).

- 2026-11-15 [빌드 근거리 검정 출력](../work-status/vegetation-build-black.md#거리-의존-제보와-빌드-수집-누락): 기본 RP는 SSAO/Decal on, 실행 호수 RP는 off인데 씬 참조 RP의 빌드 포함 옵션이 꺼진 설정 결함을 확인·1줄 수정했다. 실제 씬→RP→Renderer·빌드 프로필 정적 검사 및 **Unity 네이티브 빌드 RP 수집 목록에 기본/호수 RP 2개 포함 확인 통과**. 콘텐츠+Player 전체 빌드의 근/원거리 화면은 사용자 테스트 대기다. 앞선 HLSL 미초기화 경고 제거는 별도 보정이며 이 증상의 해결 근거가 아니다.

- 2026-11-15 [폭포 R4 표시 결함·개방형 공간](../work-status/lake-waterfall.md#r4-표시-결함개방형-공간--반영저장): 상류·낙수 단일 메시 연결, 사각 외벽·별도 길 제거, 사면 안쪽의 넓게 열린 공간과 중앙 상자 반영·저장. 캡슐27표본 막힘0·Nav 경로 Complete·ID15,751개 고유. 실제 이동·미술·보상 왕복은 사용자 테스트 대기. 아래 R3 형상은 수정 전 이력이다.

- 2026-11-15 [폭포 R3·숨은 동굴](../work-status/lake-waterfall.md#r3-낙수-수정숨은-동굴--반영검증): 낙수 접선·중력 곡선/두께/비말 보정, 폭포 뒤 실제 동굴9.5m·마른 바닥·진입 경사·보물상자1개 반영. 기존ID15,750개 유지+신규1개, 캡슐26표본 막힘0·동굴 Nav 경로 Complete·672타일 저장. 카메라/보상 계약 EditMode16/16, 실제 보상 데이터 중복/소비 복원 보조 확인. 실제 조작·발견·미술·저장 왕복은 사용자 테스트 대기다. [설계](../../Assets/docs/design/world-art/environment/LAKE_WATERFALL_CAVE_DESIGN.md).

- 2026-11-15 [폭포 R2 재저작·상류 시작 보정](../work-status/lake-waterfall.md#r2-재저작--현재-저장본): 폭18.6m, 지형 하상·낙구·호수 소로 재구성하고 추가 돌10개 제거·기존 돌 낮춤. T자 물판 대신 지형에 닫힌 상류 소로 연결했다. 지형 원본 보존·지정 밖 높이변경0·배치ID15,750개 보존·672타일 NavMesh 저장 확인. 주변 경로는2쌍 Complete/기존2쌍 Partial 유지. 별도 고사목3개의 자동성 반응 구성 변경이 있어 전체 보존 감사는 실패로 기록했다. 실제 이동·수영·미술 인수는 사용자 테스트 대기다. 아래 R1은 실패 이력이다.

- 2026-11-15 [폭포 측면·주변 돌 사용자 재제보](../work-status/lake-waterfall.md#사용자-재제보--실패-수정-대기): 상류 물판이 하상 없이 뜨고 돌이 선돌처럼 반복되는 미술 실패를 확인했다. 기존 정면 캡처를 인수 근거로 승계하지 않는다. 하상·낙구부터 다시 구성할 예정이며 다른 MCP의 같은 씬 작업과 겹쳐 Unity 쓰기는 대기 중이다.

- 2026-11-15 [호수 북동 폭포](../work-status/lake-waterfall.md): 북동 숲 사면에서 호수로 흐르는 폭포를 저장했다. RiverBasinWorld 낙수 재질·물보라 재사용, 전용 메시·재질과 호안 암반10개, 기존 장식 바위3개 보정. 고정 구도 확인·지형 높이 변경0. MCP 단절로 최종 재로드·충돌·물살 검증은 미수행이며 사용자 테스트 대기다. 별도 환경 피격체1,000개 변경은 주체 미확정 상태로 보존하고 백업 대조 근거를 기록했다.

- 2026-11-15 [사진 속 약초 작업대 수정](../work-status/npc-daily-animation/LAKE_ACTIVITY_FIX.md#사용자-스크린샷-재제보--약초-작업대-실제-대상-수정): 앞선 다른 식탁 보정을 이 제보의 해결로 볼 수 없음을 정정했다. Hazel_Worktable_R2를 원래 충돌/접촉 범위에 맞추고 병·단지·약초 그릇 네 곳의 잘못된 압축 항아리 세트를 개별 원본 모델로 복구했다. 기존 기능 루트·진행 ID와 구매 원본 보존. 실제 Unity 전후 화면·접촉 측정 근거 연결.

- 2026-11-15 [NPC 전수 생활 보완](../work-status/npc-daily-animation/LAKE_ACTIVITY_FIX.md#사용자-지적에-따른-전수-확대): 연결 지역 포함 83명 조사·58명 행동 보완. 상점 장부·천·채소 작업면 3곳에 기존 FarmLand 자산 배치, 실제 호수 상점 화면 확인. 지역 전체 접촉·미술 인수는 사용자 테스트 대기.

- 2026-11-15 [생활 행동·식탁 접촉 수정](../work-status/npc-daily-animation/LAKE_ACTIVITY_FIX.md): 교체된 식탁의 실제 메시 상판 기준으로 소품 8개를 보정했다. 기존 위치·진행 ID 보존, 전체 월드 재생성 없음. 수치 검사와 실제 플레이 시각 확인은 구분하며 최종 사용자 테스트 대기.

- 2026-11-15 [잔상 텍스처·Fauna 렌더 오류 수정](../work-status/afterimage-fauna-render-errors.md): Fauna 깊이·노멀 샘플 수가 다르면 DepthOnly로 출력해 MSAA4 RenderGraph attachment 오류를 방지한다. Unity 컴파일·관련 EditMode22/22·PlayMode2/2 통과, RF02 본편 화면 품질 사용자 테스트 대기.

LakeVillageWorld 대기 프레임 저하(2026-11-14~15): [조사·최종 근거](../work-status/lake-village-idle-performance.md#debug-최종-판정--acceptance-3). **Editor Debug의 초기화 후 대기 기준 통과**: 맑음/비 각600프레임, 평균40.01/36.73FPS·임의500ms 최저33.81/31.46FPS.2560×1440/MSAA4와18,472대상을 유지하며 그림자25m/1cascade·SSAO off·빈 Decal 패스off를 전용 에셋에 저장했다. 최신 회귀100/100, 강수 위치/차폐 차이0, GPU 유효 표면45개 통과/8개 미관측이다. 단일 프레임 최대88.25ms와 기존 ActorDatabase 지역 배치 시간 초과2건은 남는다. 전체 NPC 정상 배치·이동/전투·미술 인수는 사용자 확인 대상이며 대기 수치를 전체 월드 품질 통과로 승계하지 않는다.

## LakeVillageWorld 식생 전수 전환 — 2026-11-14

나무 벌목 마무리(2026-11-15): [정본](../../Assets/docs/design/world-art/environment/TREE_DESTRUCTION_COMPLETION_DESIGN.md)에 따라 **런타임·5종4,767개 SO/FX 반영·관련 자동 검사 통과**다. 낙하→정착→보상1회/목재·먼지FX→줄기·수관·밑동 소거를 연결했다. 저장/구버전 이관·보상 pending/좌표 복원·강수/열/전파·충돌/Nav 종료를 포함한다. EditMode80/80·본편 복제본15/15·기본/공유 버퍼 셰이더58패스 통과. 큰 월드 좌표의 디더 붕괴와 기존 생성기의 피벗 상수 좌표를 수정했고 LOD0/1/2 중간 소거·최종 픽셀0을 GPU 캡처로 확인했다. 독립 코드 재검토에서 추가 확정 결함 없음. 실제 입력·FX/청감·그림자·KCC/Nav/성능·미술 인수는 [사용자 테스트 대기](../work-status/reactive-environment-tests.md#낙하-완료드롭fx소거-사용자-인수--2026-11-15)다. [작업 상태·원본 근거](../work-status/tree-destruction.md#낙하-완료-수명-구현--2026-11-15).

옆 나무 피격 BUG-DT-02·큰 잔해 FEEL-DT-01(2026-11-15): 수관/impostor 입력 Box를 제외하고 본편4,767개를 줄기 Capsule 피격으로 전환·저장했다. 전수 입력 오류0, 실제 물리 재현15개에서 첫 나무75/이웃100 및 첫 나무 파괴 후에도 이웃 오탐0을 확인했다. 기존 물리 MeshCollider17,920개 변경0·대상 나무 ID/외형 변경0·구매 원본/meta234개 변경0. 다른 MCP/테스트 요청이 있으면 대기하도록 짧은 묶음으로 적용했으며 사용자 원래 입력 재시험은 대기다. 큰 잔해는 위 마무리 설계로 방향을 확정했으며 런타임 수명/형상 반영은 대기다. [최종 근거·스크립트·인수](../work-status/tree-destruction.md#bug-dt-02--feel-dt-01--옆-나무-피격과-큰-잔해).

나무 파괴 외형 결함 BUG-DT-01(2026-11-15): 원본별/LOD별 슬롯 순서를 고정한 생성기 때문에 잎에 나무껍질이 붙었다. 생성기를 재질 참조 기준으로 수정하고 잘못된 잔해4종34슬롯·실행 복제본을 바로 교정했다. 자산5종60슬롯·실행4,767그루57,204슬롯 불일치0, 구매 원본/meta234개 변경0·씬 저장 없음. [원인·사진·수정·재검증](../work-status/tree-destruction.md#bug-dt-01--파괴-잎에-나무껍질-재질이-붙음). 같은 나무의 근/중/원거리 외형은 사용자 재시험 대기이며 이전 기능 자동 검사 통과가 외형 통과를 뜻하지 않는다.

제외 고사목 식별(2026-11-15): [본편 실제 사진3장·좌표·제작 출처](../work-status/tree-destruction.md#제외-고사목-실제-배치-사진--2026-11-15)를 기록했다. 프로젝트 `ArtSource/PoorVillage/build_poor_village.py`의 절차적 Blender 모델 Terrain_DeadTree가 동일하게3개 배치되어 있다. 촬영 중 씬/오브젝트 변경 없이 뷰를 복원했다. 파괴 전환 승인은 아직 받지 않았다.

일반 수목 파괴 연결(2026-11-15): [작업 상태·검증 근거](../work-status/tree-destruction.md). FullTree5종4,767개에 내구도100·목재1~4개·종류별 Stump/Trunk·방향성 낙하/막힘 잔해를 연결해 본편 씬에 저장했다. 환경 통합40/40·복제본 실제 피격/JSON15개 시나리오·식생15,396개 전수 구성 통과, 배치 ID/원형 외형 변경0·구매 원본/meta234개 해시 변경0. 보호829개와 나머지 식생은 보존했다. 고사목3개 추가 전환은 자동 승인 검토에서 범위 확장/기존 구성 제거로 거절되어 미적용이다. [직접 공격·낙목/KCC/Nav·저장/지역 왕복·성능·미술 인수](../work-status/reactive-environment-tests.md#일반-나무-파괴-연결-인수--2026-11-15)는 사용자 테스트 대기이며 종전 대기 성능 인수를 승계하지 않는다.

2026-11-21 **EastLands 상호작용 수목 3종 저작 반영.** 직립·굽은 침엽수와 키 큰 대나무를 배치용 프리팹으로 추가했다. EditMode 55/55, 낙하·보상·복원 9/9와 인접 줄기 피격 9/9를 통과했다. 본편 플레이는 사용자 테스트 대기다. [프리팹·검증 상태](../work-status/tree-destruction.md#eastlands-상호작용-수목-확장).

2026-11-21 **상호작용형 수목 7종·소품 2종.** 목서와 대나무 군락에 이어 붉은 활엽수·작은 대나무·나무통·도자기 화병을 추가했다. 신규 4종 파괴·복원 8/8, 소품 원소 반응 2/2. 전체 환경 목록과 상호작용 목록을 연결했다. [카탈로그](../../output/asset-catalog/index.html) · [적용과 검증](../work-status/tree-destruction.md#수목과-파괴-소품-추가).

2026-11-21 **환경 에셋 전수 조사·상호작용형 수목 19종.** 카탈로그 3,176개 경로와 수목·소품 1,129개 Unity 프리팹을 확인했다. EastLands 절단 대응 17종 연결과 Blender 제작 Fir_02 반영. 신규 수목 자동 재현 36/36, 본편 사용자 테스트 대기. [전수 조사·프리팹·남은 범위](../work-status/tree-destruction.md#전체-카탈로그-조사와-수목-확장).

**Fir_02 밑동 근접 외형 개선.** 남은 잎 카드를 제거하고 절단 둘레·나무껍질·뿌리 형태를 다시 저작했다. 가까운 LOD는 원주 48분할, 파괴·보상·복원 3/3이다. [전후 캡처와 재검증](../work-status/tree-destruction.md#fir_02-밑동-근접-실루엣-개선).

전환 전 조사(2026-11-15): 당시 식생15,396개는 전부 비파괴였다. [조사 당시 구성](../work-status/reactive-environment.md#나무-파괴-연결-재확인--2026-11-15)은 과거 근거이며, 이후 일반 수목4,767개는 위 파괴 연결 결과로 대체됐다.

[최종 결과·검증 근거](../work-status/reactive-environment.md#식생-전수-전환-결과--2026-11-14): 식생15,396/15,396개와 Terrain10층을 실제 본편에 연결·저장했다. 재로드 후 전수 구성 유효, 누락/중복 ID·배치/활성/ID 변경0, 원본/메타332개 해시 변경0이다. 퀘스트/수중/채집 시각 식생829개를 비가연·비파괴 정책으로 보호한다. 외형·LOD·바람·알파와 기존 채집 소유권을 보존한다.

[표현 최적화·최종 자동 검증](../work-status/reactive-environment.md#공유-표현-버퍼와-전환-최종-자동-검증--2026-11-14): 활성 식생15,284개+Terrain 셀3,188개(총18,472대상), 강수 판정 전수 차이0. Native 강수 묶음과 공유 GPU 표현으로 강수 중앙 tick은 Editor에서220~230ms→67~74ms, 표현은약60ms→10ms로 줄었다. CPU 화학·전파·저장과 카메라 밖 대상 처리는 유지한다. GPU 일반/인스턴싱 출력 대조와 열/물·JSON·ID 재등록·Terrain 원본 복구를 확인했다.

**전수 전환·자동 검증과 최종 플레이 인수를 구분한다.** 일반 공격 입력·실제 저장/지역 왕복·KCC/Nav·모든 LOD/임포스터·목표 기기 성능·미술/청감은 [LV-V01~06 사용자 테스트 대기](../work-status/reactive-environment-tests.md#lakevillageworld-식생-전수-전환-인수-전환-완료사용자-검수-대기-2026-11-14)다. 강수 tick 잔여67~74ms는 목표 기기 FPS 통과 근거가 아니며 성능 인수는 보류다. 기존 재생성 원본 부재와 비식생을 포함한 W 전체 인수는 이번 식생 전환으로 해소되지 않는다.

## 환경 에셋 카탈로그

2026-11-21: 오브젝트를 배치·교체할 때 먼저 [환경 에셋 카탈로그](../asset-catalog/index.html)에서 프리팹 경로와 외형을 찾는다. 나무·그루터기·통나무, 덤불·꽃·풀, 바위·돌, 건물·구조물, 소품, 던전 키트 6개 페이지에 3,176개를 캡처 사진·이름·경로·크기·삼각형 수와 함께 담았고, 경로를 클릭하면 복사된다. 새 오브젝트를 만들기 전에 기존 팩에 쓸 만한 것이 있는지 먼저 확인하는 용도다.

- 캡처 조명은 인게임 낮 시간대(WorldLighting `time01=0.38` 태양·LakeVillageWorld 하늘·LakeAtmosphere 볼륨)를 임시 씬에 복제해 찍었다.
- 한계: 카테고리는 자동 분류, 캡처는 LOD0·고정 조명이라 실제 플레이 화면과는 다르고 색감은 참고용이다. 44개 제외·7개 빈 캡처가 있다. 미술 품질 판단의 근거로 쓰지 않는다. [범위·한계·검증](../work-status/asset-catalog.md)
- 팩이 바뀌면 [재생성 절차](../../Tools/AssetCatalog/README.md)로 갱신한다. 결과물은 `docs/asset-catalog/`에 있고 Git 커밋은 하지 않았다.

## 유기체 모델링 품질 스킬

2026-11-15: 환경·소품 모델이 프리미티브 조합·플랫 셰이딩으로 각지고 로우폴리해 보이는 문제(예: PoorVillage `Terrain_DeadTree`)를 줄이려 [organic-modeling-quality 정본](../../.claude/skills/organic-modeling-quality/SKILL.md)과 공용 Blender 함수 [organic_lib.py](../../.claude/skills/organic-modeling-quality/templates/organic_lib.py)를 추가했다. `environment-artist` 스킬과 [artist 역할 정본](../../Tools/Harness/roles/artist.md)에서 참조한다.

- 구매 팩 FBX 20개 실측: 전부 스무스 셰이딩 100%·커스텀 노멀. 각져 보이는 원인은 셰이딩이 아니라 형태다. LOD0 삼각형 수는 활엽수 줄기 약1,400~3,000, 고사목 1,810~24,826, 그루터기 890, 통나무 744~860, 바위 소160~433/중252~1,334/대5,150. LOD1은 55~75%, LOD2는 30~55%.
- 검증: Blender 5.2.2 백그라운드 CLI에서 레시피 5종(나무·고사목·바위·통나무·그루터기)이 수치 기준 통과, FBX 왕복에서 tris·스무딩·노멀 보존. 하네스 검사 43개 통과.
- 첫 적용 비교(2026-11-15, Blender CLI 임시 씬, 저장·Unity 변경 없음): 기존 `Terrain_DeadTree`(844 tris, 스무스 69%, 90° 직각 에지, 막대형 뿌리·가지 15조각)와 새 레시피 고사목(3,996 tris, 스무스 100%, 중앙 에지각 13°, 한 덩어리)을 4구도로 렌더 비교했다. 새 쪽은 뿌리·가지가 줄기에서 이어져 나오고 실루엣이 유기적이다. 뿌리 굴곡은 아직 약하다. 기존 배치 3개 교체와 Unity 적용은 승인 전이라 하지 않았다.
- Unity 첫 적용(2026-11-15, A안 별도 에셋): `ArtSource/PoorVillage/build_deadtree_organic.py`(seed 37, base_r 0.55)로 `Assets/08.Models/PoorVillage/Terrain_DeadTree_Organic.fbx`(LOD0/1/2 3,990/2,592/1,596 tris, 기존 meta 설정 동일)와 같은 이름의 프리팹(PoorHouse_Wood_Weathered 재질, LOD2 MeshCollider)을 만들었다. 프리뷰 씬 비교 렌더(`output/PoorVillage/DeadTreeOrganic-20261115/`, Git 무시)에서 Y-up·스케일1·밑동 접지를 확인했다. 새 쪽이 높이 6.54m로 기존 5.28m보다 24% 크다(교체 시 약0.81배 보정 필요). LOD2 p90 에지각이 45.9°로 기준을 조금 넘는다. 기존 배치 3개 교체와 월드 조명 확인은 사용자 승인 전이라 하지 않았고, 미술 품질은 사용자 확인 대기다.
- 고사목 3개 교체(2026-11-15): 사용자 승인·권한 허용 후 LakeVillageWorld의 empty-space-0031·marked-0018·marked-0024를 `Terrain_DeadTree_Organic`으로 제자리 교체했다(Undo 1그룹, **씬 미저장**). 이름·경로·회전·SceneEntityId·placementGuid를 보존했다. 배율 ×0.8075로 높이를 기존과 같게(6.60/7.92/7.13m) 맞췄고 marked-0024만 접지를 위해 0.133m 내렸다. 반응 바인딩은 LOD 렌더러 3개와 루트 MeshCollider·입력 Box로 다시 연결하고 입력 범위·샘플을 새 형태에 맞췄다. `_sourceId`는 새 프리팹 GUID로 갱신했다. 교체로 생긴 null 참조·Console 오류 0. 전후 캡처(월드 조명)는 `output/PoorVillage/DeadTreeOrganic-20261115/replace/`(Git 무시)에 있다. 한계: 뿌리 노출 부족(원 피벗이 지형 아래라 ×0.81 축소와 겹쳐 뿌리가 묻힘), "부러진 고목" 인상 약화. 미술 품질과 Play 반응(피격·연소·비·채집·LOD)은 사용자 테스트 대기다. `dead-tree-destruction-apply.csx`는 새 구조와 맞지 않아 파괴 연결 전에 수정이 필요하다. 재생성 계획 JSON(`empty-space-plan.json`, `marked-infill-plan.json`)은 기존 프리팹 경로를 그대로 참조하므로 재생성 스크립트를 돌리면 기존 고사목으로 돌아간다.
- **미수행:** 대화형 Blender MCP 실행, Unity 임포트·lilToon 화면 확인, 크기·축 확인, 석조 소품 실증, 버텍스 컬러·AO 베이크. 미술 품질(기존 팩과의 어울림)은 사용자 확인 대기. 렌더는 셰이딩·실루엣 확인일 뿐이다.

## Unity FX 제작 스킬

2026-11-14: [uplayground-vfx 정본](../../.claude/skills/uplayground-vfx/SKILL.md)을 작성하고 Codex 사용자 스킬 폴더에 설치했다. 기존 Unity MCP·Shuriken·MotionEvent·무기 궤적·풀을 활용하며 VFX Graph 노드 편집 제한을 명시했다. 정본/설치본 구조 검사와 3개 파일 해시 대조 통과. 실제 FX 제작·Unity 실행·미술 검증은 미수행이다. [설치·검증·사용법](../work-status/uplayground-vfx-skill.md).

2026-11-19 **WorldBuilder P06 기술 검증 완료**: 별도 2km 씬의 동굴·소유 HoleCut·P05 건조 체적과 바이옴·Terrain 식생을 검증했다. 프로젝트 KCC 양방향 횡단·끝막이 벽 차단, 생태 EditMode 9/9, 실제 도메인 리로드 후 Sculpt 캡처, T24 생성과 T26 카메라 근거를 연결했다. 본편 입력·카메라와 저작 UX·미술·청감은 사용자 테스트 대기다. [P06 종료 검증](../work-status/open-world/WORLDBUILDER.md#p06-close) · [P06 저작 지침](../../Assets/docs/guide/WORLDBUILDER_ECOLOGY_AUTHORING_GUIDE.md).

2026-11-19 **WorldBuilder P07 검증·Bake·발행**. 별도 2km 월드에서 T17 AI 링크·KCC 통과, T20 격리 revision 발행·교체·Rollback, T21 Editor 로드와 영향 회귀 10/10을 확인했다. Player 판정은 사용자 지시 대기다. [P07 작업 상태와 근거](../work-status/open-world/WORLDBUILDER.md#p07-bake) · [저작 지침](../../Assets/docs/guide/WORLDBUILDER_BAKE_AUTHORING_GUIDE.md).

2026-11-20 **WorldBuilder P08 스트리밍**. 별도 2km 월드에 16개 Additive 청크를 발행했다. Editor 이동 실패·재시도, 실제 저장 A→B→Rollback A, T20 발행 복구, T21 Editor 검사와 T23 회귀 153/153을 확인했다. 50회 왕복의 소유 핸들과 지형·NavMesh·proxy는 안정적이며 전체 Editor 메모리 판정은 보류다. Player와 실제 입력·저작 UX는 대기 상태다. [P08 작업 상태](../work-status/open-world/WORLDBUILDER.md#p08-streaming) · [저작 지침](../../Assets/docs/guide/WORLDBUILDER_STREAMING_AUTHORING_GUIDE.md).

2026-11-20 **WorldBuilder P09 v1 단계 완료 · P10 착수.** 사용자 결정으로 P09를 마감하고 잔여 검증을 후속 항목으로 넘겼다. 4km Dirty 재생성은 중앙값 2.63초, 해시 3/3이다. 생산 브리지의 AI pin·Title 실패 복귀·격리 저장 재진입과 T20 실제 Editor 재시작을 통과했다. 기존 회귀 실패 31건의 통합 재실행은 통과 30, 세라 얼굴 모프 재현 1이다. 세라 얼굴 콘텐츠 누락은 P09 이전 붉은나무 R03·R06에도 기록돼 있고 RifleGirl 모델 46개에서 기존 모프를 찾지 못했다. U03용 2km·4km 격리 씬은 준비했고 U01~U04는 사용자 테스트 대기다. [P09 작업 상태](../work-status/open-world/WORLDBUILDER.md#p09-validation) · [검증 매트릭스](../work-status/open-world/WORLDBUILDER_P09.md) · [사용자 검수](../work-status/open-world/WORLDBUILDER_P09.md#사용자-검수-대기).

Editor GPU 왕복 p95/p99는 2km 13.79/16.34ms, 4km 13.83/16.16ms다. 진단 Player의 D3D11·D3D12 FrameTiming 27,603행과 D3D12 직접 연결 3,705행에서 유효 GPU 표본은 0개였다. 진단 Player P08의 세 스냅샷에서 PhysX +2,647,920B와 검증 이벤트 문자열 +57,000B를 확인했다. PhysX 크기는 월드 종료 후 워밍업보다 작아졌다. 4km Bake 후속 A/B의 Unity 할당 피크는 8.78GiB에서 5.10GiB로 감소했고 청크 64개와 링크가 일치했다. GPU 미수행과 4km Bake 메모리 경고는 후속 기록에 유지한다. [메모리·GPU 기록](../work-status/open-world/WORLDBUILDER_P09.md#진단-player-빌드와-p08-스냅샷) · [v1 판정](../work-status/open-world/WORLDBUILDER_P09.md#v1-판정).

4km Bake 기록의 Windows working set 최대는 저장 씬 재개방부터 첫 청크 최종 저장 직전 사이에 관측됐다. 최적화 후에도 Terrain 복제·Navigation·청크 저장 구간마다 Unity 할당이 1GiB 이상 증가했다. [단계별 Bake 메모리](../work-status/open-world/WORLDBUILDER_P09.md#4km-bake-메모리).

Windows Development 진단 Player 빌드가 성공했다. 보관 Addressables 파일 105개의 지문이 새 패키지와 일치했고, 2km·4km·P08의 청크 로드·해제를 세 번씩 완료했다. 기존 저장 슬롯 5개와 프로젝트 설정 지문을 보존했다. [빌드·런타임 검증](../work-status/open-world/WORLDBUILDER_P09.md#진단-player-빌드와-p08-스냅샷).

U04는 Boot에서 새 게임으로 라온을 선택하고, 저장 화면의 빈 슬롯에만 저장한 뒤 타이틀의 불러오기에서 그 슬롯을 선택한다. 현재 첫 빈 슬롯은 화면 3이다. [U04 검수 절차](../work-status/open-world/WORLDBUILDER_P09.md#사용자-검수-대기).

**WorldBuilder P10-A~D 구현·Editor 검증 완료 · T35 사용자 테스트 대기.** C 아틀라스를 2,048m × 2,048m, 높이 0~700m로 적용했다. 바이옴 5개, 수계, POI 3곳, 도로 루프와 교량을 저장했다. Unity NavMesh에서 체크포인트 139구간을 양방향 검증했고, 강가↔호수 1,434m의 단일 역방향 질의는 부분 경로였다. 7개 지역 자동 배치, Dirty/Full 일치, Lock과 Override 21건을 확인했다. 실제 플레이어로 접근로·교량·타일 경계 5구간을 주행했고 TPS 화면 7장을 저장했다. 경관과 실제 조작 품질, 전 구간 수동 주행은 T35 검수에 남긴다. [P10 작업 상태와 검증](../work-status/open-world/WORLDBUILDER_P10.md) · [TPS 검수 절차](../work-status/open-world/WORLDBUILDER_P10.md#후속-게이트)

**P10 C Editor 발행·핵심 횡단 통과.** 배치 377개와 기존 나무 1,260그루를 적용한 512m 청크 16개를 발행했다. 필수 AI 경로 133구간과 실제 TPS 교량·타일 경계 왕복, 목적지 청크 지연·실패·재시도를 확인했다. Editor 왕복 프레임 p95는 188.6/187.6ms로 기록됐다. 전체 도로 루프와 표현 품질은 사용자 테스트 대기다. [검증 프롬프트·MCP 결과](../work-status/open-world/WORLDBUILDER_P10_STREAMING.md)
