# RiverBasinWorld — 제작·검증 상태

- 설계 정본: [정규 월드 제작 설계](../../Assets/docs/design/world-art/open-world/REGULAR_WORLD_PRODUCTION_DESIGN.html) (좌표·치수·예산은 블록아웃에서 조정할 시작값이며 구현 완료 수치가 아니다)
- 저작 지침: [오픈월드 맵](../../Assets/docs/guide/OPENWORLD_MAP_AUTHORING_GUIDE.md), [지형·물 교훈](../../Assets/docs/guide/TERRAIN_WATER_ART_LESSONS.md)
- 저작 원본·기록 스크립트: [Tools/RiverBasinWorld/README.html](../../Tools/RiverBasinWorld/README.html)
- 분야 위키: [월드와 아트](../wiki/world-art.md) (표의 「강 유역 월드」 행)
- 조사 기준: 2026-11-14 문서·파일 조사. 이번에 게임·Unity를 실행하지 않았다.
- 현재 판정: **독립 씬 1차 제작본 저장됨 / 미술 NO-SHIP·사용자 인수 대기 / P5 정규 등록 미제작**.

## WorldBuilder 신규 제작

### 4km 숲 연못 동쪽 식생 적용

2026-11-21 **제작 씬 반영 및 정적 검사 통과.** 약 126m 숲길의 시작은 X 988 / Z 2320, 연못 조망은 X 880 / Z 2257이다. [환경 에셋 카탈로그](../asset-catalog/tree.html)에서 확인한 Dreamscape EastLands 목서, Toon Golden Valley 자작나무·너도밤나무·어린 나무와 Farmlands 어린 활엽수의 검증 배치를 [RiverBasin4km 제작 씬](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/RiverBasin4km.unity)에 옮겼다. 연못 측 자작나무 21그루, 숲 내부 목서와 너도밤나무, 밝은 틈의 어린 나무 44그루를 포함해 TerrainTree 219그루와 프리팹 관목 26개를 배치했다.

- `Terrain_1_4`에 0.5m 셀, 풀 65,277개, 풀 표시 90m, 나무 표시 340m 및 빌보드 기준 95m를 적용했다. 길 중심이 읽히도록 검증본 도색을 경로 주변 409개 표본에 섞었다. 타일 가장자리의 재질 가중치는 기존 제작 데이터를 유지했다.
- 검증 씬에서 1080p 가상 패드 왕복 시 지지 표본 1,482개 모두 안정이었다. 연못 앞 회전 캡처에서 캐릭터와 호안이 보였다. [검증 씬 왕복 로그](../../Tools/RiverBasinWorld/Rebuild/ForestPilot_Review_PlayMode.txt) · [연못 방향](../../Tools/RiverBasinWorld/Rebuild/ForestPilot_Review_Orbit2.png)
- 제작 씬을 다시 열어 Terrain 및 TerrainCollider 64쌍, Missing Script 0건, 대상 타일 전체 높이 일치, 동쪽 경계 높이차 0m를 확인했다. [제작 씬 에셋 검사](../../Tools/RiverBasinWorld/Rebuild/ForestPilot_Assets.txt)

### 4km 서부 숲 식생 검증

2026-11-21 **서부 숲 식생 검증 씬 저장.** [RiverBasin4km_ForestReview](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/ForestReview/RiverBasin4km_ForestReview.unity)을 단독으로 열고 Play한다. 숲길 입구에서 출발해 수관 아래를 지나 연못이 보이는 지점까지 약 126m를 왕복한다. 제작 월드의 Terrain 9장을 배경으로 사용하고 식생과 검증용 지면 도색은 `Terrain_1_4` 복제본에 넣었다. 제작 씬과 원본 `Terrain_1_4` 파일의 SHA-256을 대조했다. [숲 검증 경로 설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_WORLD_REBUILD_DESIGN.md#서부-가을-숲과-수변-식생)

- 기존 Dreamscape 수목 6개 프리팹으로 TerrainTree 175그루를 배치했다. 길 가장자리와 수목 아래에 관목 26개, 길과 물가를 피해 풀 Terrain Detail 2종 13,175개를 저장했다. 풀 표시 거리는 90m, 나무 빌보드 거리 설정은 95m다. 길 132개 표본의 최대 경사는 12.69°다.
- 첫 카메라 검사에서 나무와 관목은 보였고 풀은 화면에 나타나지 않았다. 기존 Terrain Detail 전용 풀 프리팹과 `InstanceCountMode` 조합으로 바꾼 뒤 TPS 근경에서 풀 렌더링을 확인했다. [식생 적용 TPS](../../Tools/RiverBasinWorld/Rebuild/ForestReview_TPS_Plants.png) · [같은 위치에서 식생 숨김](../../Tools/RiverBasinWorld/Rebuild/ForestReview_TPS_WithoutPlants.png) · [숲 내부](../../Tools/RiverBasinWorld/Rebuild/ForestReview_TPS_Canopy.png) · [연못 앞](../../Tools/RiverBasinWorld/Rebuild/ForestReview_TPS_Pond.png)
- **Editor Play 검증 통과.** GameManager `Ready`와 TPS `InGame`에서 가상 패드로 같은 길을 왕복했다. KCC가 이동 표본 2,537개에서 안정 지지를 유지했고 Console 오류는 0건이었다. 2560×1440 Editor의 이동 중 p95는 23.47ms다. 같은 정지 구도에서 식생을 켬 → 끔 → 켬으로 바꾼 세 구간의 p50은 16.13 → 15.88 → 16.11ms, p95는 23.05 → 24.81 → 24.56ms였다. 이 반복의 변동 폭으로 개별 식생 비용을 판정할 수 없다. [왕복·프레임 기록](../../Tools/RiverBasinWorld/Rebuild/ForestReview_PlayMode.txt)
- 사용자 확인 항목은 양방향 길 가독성, 수관 그늘에서의 풀 색과 밀도, 나무 근경과 원경 전환, 연못이 처음 드러나는 구도다. 서부 전역 밀도 확정, P06 재생성, 청크 발행과 1080p CPU/GPU 계측은 후속 범위다.

#### 사용자 제보에 따른 잔디 밀도 보정

숲 바닥에서 큰 풀 포기 사이가 넓게 비어 보인다는 제보를 반영했다. Terrain Detail 격자를 1m에서 0.5m로 줄이고, 배치 수를 13,175개에서 65,277개로 늘렸다. 낮은 풀 50,117개를 바탕으로 중간 풀 14,108개와 큰 포기 1,052개를 섞었다. 숲길 중심에서 2.25m를 비우고 군락과 물가에서 밀도를 완만하게 조절했다. 보고 시점의 Scene View 카메라 자세와 화각을 같은 해상도로 재현해 빈 지면이 줄었는지 확인했다. [보정 전](../../Tools/RiverBasinWorld/Rebuild/ForestReview_Grass_Before.png) · [보정 후](../../Tools/RiverBasinWorld/Rebuild/ForestReview_Grass_After.png)

#### 수형 다양화와 어린 나무

처음 사용한 Dreamscape 수목 6종은 목서 계열의 색상과 수형 변형이었다. Toon Golden Valley 자작나무·너도밤나무와 Farmlands 활엽수·어린 나무 후보 26개를 Unity에서 로드해 높이, 수관 폭, LOD, 셰이더 지원과 충돌 참조를 비교했다. 검증 구간의 시야와 수관 간격에 맞는 중형 후보를 선택했다.

| 배치 | 적용한 프리팹과 수량 |
|---|---|
| 연못 쪽 밝은 사면 | `TGV_Birch_Tree_04B` 21그루. 가는 밝은 줄기가 기존 목서 사이에서 읽힌다. |
| 숲 안쪽 수관 | `TGV_Beech_Tree_03B` 11그루, `TGV_Beech_Tree_05A` 8그루, `PF_Farm_GenTree_B_Young_01` 4그루. 넓고 낮은 수관을 다른 높이로 섞었다. |
| 수관 사이 작은 군락 | `TGV_Birch_Sapling_01B` 12그루, `TGV_Beech_Sapling_01A` 32그루. 약 2.3~4m 높이의 어린 나무를 17개 군락으로 배치했다. |

기존 큰 나무 중 44그루를 교체하고 어린 나무 44그루를 추가했다. 저장본에는 수목 프리팹 12종과 나무 219그루가 있으며, 가장 가까운 어린 나무 중심은 숲길 중심과 약 6.59m 떨어져 있다. 물가 아래와 연못 조망을 여는 경로 끝은 군락 배치에서 비웠다. 동일한 제보 시점에서 줄기와 수관이 구분되는 화면과 어린 나무 근접 화면을 확인했다. [혼합 수관과 잔디](../../Tools/RiverBasinWorld/Rebuild/ForestReview_MixedTrees.png) · [어린 자작나무 근접](../../Tools/RiverBasinWorld/Rebuild/ForestReview_Saplings.png)

TPS 입구 캡처에서 Farmlands 어린 활엽수의 낮은 가지가 플레이어를 가리는 것을 확인해 해당 나무를 숲 안쪽으로 옮겼다. 같은 시작 카메라에서 플레이어와 길이 보이는 것을 확인했다. 최종 가상 패드 왕복에서 이동 표본 2,589개가 모두 안정 지지였고 Console 오류는 0건이었다. 2560×1440 Editor 이동 p95는 23.66ms다. 실제 카메라 조작과 식생 외형은 사용자 테스트 대기이며, 1080p CPU·GPU 계측은 미수행이다. [보정 후 TPS 입구](../../Tools/RiverBasinWorld/Rebuild/ForestReview_Mixed_TPS_Plants.png) · [최종 왕복 기록](../../Tools/RiverBasinWorld/Rebuild/ForestReview_Mixed_PlayMode.txt)

### 4km 다음 제작 방침

2026-11-21 **설계 반영.** 다음 납품 구간은 호수 마을 출구에서 B3와 B1을 지나 중앙 성채 정문까지 이어지는 약 1.9km 본선이다. B1 성능과 Editor 청크 로딩의 기준을 먼저 잡고, B3 단면과 도로 소켓을 실측해 본선의 건조 왕복을 제작한다. 기존 61.96ms는 캡처가 섞인 Editor p95 기록이므로 병목과 목표 품질 검증은 다음 계측에서 확인한다. 이번 변경은 설계·문서 연결에 반영했으며 신규 씬 제작과 계측은 미수행이다. [B1 이후 제작 방침](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_WORLD_REBUILD_DESIGN.md#b1-이후-제작-방침)

### 4km B1 석교와 상류 수영로

2026-11-21 **B1 플레이 구간 구현 반영.** [RiverBasin4km 씬](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/RiverBasin4km.unity)을 단독으로 열고 Play하면 B1 남서쪽 접근로에서 기존 플레이어와 TPS 카메라로 시작한다. 테스트는 활성 저장 슬롯 없이 진행했다.

- **교량.** 기존 석교 원본의 석재 재질, LOD 3단, 상판·난간 충돌을 재사용했다. 양안 착지 실측에 맞춰 길이 48m, 상판 높이 Y 52.25m로 배치하고 아치 하부와 충돌 메시를 하상까지 내렸다. `Castle_Moat_Bridge`의 기존 Solid 카메라 규칙을 사용한다. 양안 세 경로의 지지면 레이 201개에서 누락 0, 침수 0, 최대 표본 경사 6.38°를 확인했다. [P04 계획](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Authoring/B1_Plan.asset) · [단면·지지면 기록](../../Tools/RiverBasinWorld/Rebuild/B1_SectionAndDeck.json)
- **수계.** X 1655 / Z 1961.6의 횡단에서 수면 폭 약 28m, 수심 1.8m 이상 연속 폭 약 17m, 최대 3.19m를 확인했다. 양쪽 수중 경사 약 21.5~21.7°, 물살 0.35m/s다. 수영 어댑터는 수면과 같은 P05 수역을 조회한다. 교량과 상류를 하나의 수역 ID로 연결하고 겹치는 레거시 수면만 잘라냈다. 기존 높이 패치 3건은 적용 순서를 보존한 채 P04·P05보다 먼저 평가하도록 옮겼고, 이 전환만 비교했을 때 기존 높이 16,785,409개는 모두 같았다. [P05 수계](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Authoring/B1_WaterSystem.asset)
- **경관·음향.** 양안의 자갈길을 교대 접근로에 연결하고 하상은 자갈과 암반으로 도색했다. 수영 진출입부는 바위와 갈대 사이로 열어 두고, 기존 수목·암석·성채 프리팹을 배치했다. 교량과 수영로에 12초 합성 물소리의 3D 루프를 놓았다. P05 수면 셰이더를 기존 수중 합성에 연결했다. 카메라를 수면 아래에 둔 Play 캡처에서 광학 프로필 `UpperOptics`, 수역 1개, 체적 셀 11개와 합성 실행을 확인했다. P05 수계를 다시 생성하면 씬의 `UnderwaterRegion`에서 새 수영 어댑터를 다시 연결하는 확인이 필요하다. [교량 진입](../../Tools/RiverBasinWorld/Rebuild/RiverBasin4km_B1_Play_Bridge.png) · [가상 패드 수영](../../Tools/RiverBasinWorld/Rebuild/RiverBasin4km_B1_Play_Swim.png) · [성채 원경](../../Tools/RiverBasinWorld/Rebuild/RiverBasin4km_B1_Keep.png)
- **출수 재현과 수정.** 첫 가상 패드 실행에서 수영 부력이 플레이어를 양쪽의 완만한 하상에 붙잡아 걸어서 나오지 못했다. 수면에서 진행 방향의 걸을 수 있는 경사를 감지하면 발 높이가 하상을 따라가도록 수정했다.
- **검증.** Unity 재개방에서 Terrain 64장, 콜라이더 참조, 공유 경계 112곳 높이 차 0m, Missing Script 0을 확인했다. 양안 자갈길 중심을 따라 캡슐 겹침 464곳을 검사했고 장애물과 겹치는 표본은 0이었다. 가상 패드로 석교 양방향, 수영 양방향, 각각 입수한 둑으로 복귀 총 6개가 통과했다. 두 물소리 소스의 재생을 확인했다. 도구 입력으로 시작 좌표를 옮긴 뒤 실제 게임 입력과 KCC로 이동했다. 관련 EditMode 110/110 통과, 실패·건너뜀 0이다. 단독 시작에서 서비스 미등록과 캐릭터 무게 프로필 폴백 경고가 있었다. [Play Mode 기록](../../Tools/RiverBasinWorld/Rebuild/B1_PlayMode.txt)
- **남은 인수.** 실제 입력·회피·카메라 회전, 물소리의 거리감·반복감 및 미술 품질은 사용자 테스트 대기다. 수중 합성 연결 후 가상 패드 실행에서 Editor 프레임 시간 95백분위는 61.96ms였다. 캡처가 포함된 Editor 값이며 1080p 목표 품질의 CPU/GPU 계측은 미수행이다. 월드 Bake·스트리밍과 호수 마을에서 성채까지 이어지는 완성 동선은 미수행이다. B2~B4, 폭포, 사막과 숲은 후속 제작 범위다.

### 4km 게임월드 제작 인계

2026-11-21 **제작 착수 전 기준 기록.** B1 상류 양안 수영로와 석교를 첫 인수 구간으로 잡고, B2~B4, 북부 폭포, 사암 사막, 서부 가을 숲으로 이어가는 설계를 확정했다. 최신 반영 범위는 [B1 작업 상태](#4km-b1-석교와-상류-수영로)에서 확인한다. [4km 게임월드 제작 설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_WORLD_REBUILD_DESIGN.md#4km-게임월드-제작) · [월드와 아트 위키](../wiki/world-art.md)

- 씬 직렬화에서 Terrain 64장과 수면 메시 35개를 대조했다. 4km Authoring의 `_planning`, `_ecology`, `_waterSystem`은 미연결 상태다. 제작용 Profile 참조는 높이 범위 −32~768m인 `Production/Profile.asset`을 가리킨다. [4km 씬](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/RiverBasin4km.unity) · [4km Authoring](../../Assets/10.Datas/WorldBuilder/6effd90b68ea4b3d8f8caa5197fcfe34_Authoring.asset)
- 최종 Joined RAW에서 계획 수위 기준으로 교량마다 1m 간격 하상 횡단 표본을 뽑았다. 중앙 깊이는 B1~B4 순으로 2.71/2.90/2.64/2.63m, 연속 젖은 폭은 약 22/22/26/28m다. 지류 교량의 계획 gap 22m는 현장 경간 확정에 쓰기 전에 다시 재야 한다. B1 상류 X 1670 / Z 1960에서는 기존 계획 직선상 수심이 약 0.01m이고 횡단면 가장 깊은 표본은 약 2.86m여서, 저작 시 현재 물길에 맞춰 수영 중심선을 옮겨야 한다. RAW 근사 단면이며 실제 수면 메시·물리 체적의 측정은 후속 제작에서 수행한다. [4km 계획 JSON](../../Tools/RiverBasinWorld/Rebuild/terrain-plan-4km.json) · [측정 RAW](../../Tools/RiverBasinWorld/Rebuild/RiverBasin_4097_Joined_R16LE_SouthFirst.raw)
- 저장 캡처에서 동부 사면은 녹색 지표가 우세했다. 채석 요새 동쪽에서 사암 단구와 모래 분지를 한 구역으로 설계하고, 숲은 서부 연못과 사당으로 이어지는 수관·하층 식생·수변 군락을 정했다. [동부 비교 기준 화면](../../Tools/RiverBasinWorld/Rebuild/RiverBasin4km_EastSlopeFinal.png)
- 다음 작업은 보정 지형 재평가와 B1의 실제 양안·수영 단면 확정이다. 현장 좌표·치수, Unity 적용, 본편 입력·수영·카메라·음향·프레임 성능 검증은 미수행이다. [제작 순서와 인수](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_WORLD_REBUILD_DESIGN.md#제작-순서와-인수)

### 타일 이음새·합류부·설산 보정

2026-11-21 사용자 시점의 직선 재질 이음새, 도로 사면의 급한 꺾임, 하천 교차부의 돌출과 적설이 읽히지 않는 문제를 보정했다.

- 지형 64장의 레이어 순서를 통일하고 공유 경계 양쪽 20m에서 가중치를 섞었다. 저장된 경계 112곳에서 재질 가중치 최대차는 0.00392로, 8비트 한 단계다. 적용 직전 부동소수 기준 최대차는 약 0.941이었다.
- 가까운 물길의 폭·수위를 합류부에서 섞고, 도로의 중앙 높이를 유지하면서 물가와 연결되는 사면의 영향량을 줄였다. 교량 예정지 둘레의 원형 높이 마스크는 하천 횡단면 기준으로 바꾸고, 지류 끝이 본류 바깥 둑을 뚫고 나온 부분을 잘랐다. B1 기존 암반 띠의 가중치는 수면 고도에 따라 자갈과 풀로 옮겼다. [최종 HeightMap](../../Tools/RiverBasinWorld/Rebuild/RiverBasin_4097_Joined_R16LE_SouthFirst.raw)은 불변 Base 위의 절대 높이 패치 3건으로 적용했다. 수면 메시 35개를 맞춰 저장했고 물길 중심선 표본 1,194개에서 단절이 검출되지 않았다. [경사·수계 검사](../../Tools/RiverBasinWorld/Rebuild/joined-height-validation.json)
- 설산 레이어에 내장 ImageGen으로 생성한 [알파인 적설 알베도](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Production/Layers/AlpineSnow_Albedo.png)를 적용했다. 높은 완사면과 눈이 쌓이기 쉬운 오목한 면의 눈 비중을 올리고 가파른 절벽은 암반이 드러나게 했다. 북부 470m 이상 표본에서 평균 눈 가중치는 46.7%다.
- 최종 Unity 확인에서 공유 경계 높이 차 0m, RAW 대비 최대 오차 0.01223m, 레이어와 TerrainCollider 참조를 확인했다. 합류부 바깥 둑의 물 돌출은 사라졌고, B1 양안에 옅은 띠가 남아 있다. [설산 화면](../../Tools/RiverBasinWorld/Rebuild/RiverBasin4km_Joined_SnowRange.png) · [합류부](../../Tools/RiverBasinWorld/Rebuild/RiverBasin4km_Joined_RiverJunction.png) · [사면](../../Tools/RiverBasinWorld/Rebuild/RiverBasin4km_Joined_SlopeTransition.png). 실제 플레이·수영·프레임 성능 검사는 미수행이다.

ImageGen 프롬프트

> Generate a production game terrain seamless repeatable square PBR base-color texture tile, top-down planar orthographic, raw alpine snow material. Neutral off-white with a subtle cool blue cast inside very small shadows, soft compact windblown fine snow and softly packed fine grain, slightly varied micro-scale soft shallow waves, seamless tile periodic in both X and Y. Flat diffuse albedo only with uniform exposure, palette predominantly matte bright white. Do not include rocks, dirt, any ground objects, trees, footprints, large cracks, border, drop shadows, words, lighting gradient, perspective, horizon, scenery, mountains or mockup. Very low visual contrast and controlled fine grain for Unity Terrain tiling across hundreds of meters. Fill every pixel with the texture.

### 동부 사면 재질 반복 보정

2026-11-21 사용자 Scene View 구도에서 동일한 암석 패턴이 전경 절개면을 덮고, 뒤쪽 사면에도 풀과 암반 띠가 유사한 대비로 이어지는 것을 확인했다. 화면 중심과 좌우를 쏜 TerrainCollider 레이로 동부·중동부 사면을 식별했다.

- 동부 TerrainData 16장에 원래 15.3m 암석과 다른 알베도·노멀을 쓰는 40×29m 풍화암 레이어를 섞었다. 경사·고도·볼록한 면과 낮은 면을 바탕으로 암반과 퇴적토·건조 풀의 분포를 조정했다. 구릉 실루엣과 높이 데이터는 유지했다.
- 같은 카메라 위치에서 반복이 많이 보였던 전경 암석을 비교했다. 적용 타일 16장의 텍스처 참조, 레이어 8개 이하와 양자화된 가중치 합 0.9922~1.0118을 확인했다. HeightMap 대비 최대 오차는 이전과 같은 0.01223m였다. 플레이 성능은 미측정이다. [적용 후 Unity 화면](../../Tools/RiverBasinWorld/Rebuild/RiverBasin4km_EastSlopeFinal.png)

### 북부 산맥 원경 재질 보정

2026-11-21 사용자 원경 캡처를 보고 북부 산맥을 고정 카메라에서 확인했다. 대표 타일의 바위 텍스처와 가중치가 연결돼 있었으며, Unity Terrain 원경 전환 거리를 일시적으로 1,000m에서 3,500m로 바꾼 비교에서도 산맥이 균일한 회색으로 보이는 현상은 남았다. 이전 북부 대표 타일 두 곳의 회색 바위 가중치는 평균 85~88%였다.

- 북부 TerrainData 21장에 프로젝트의 기존 Cliff 알베도·노멀을 사용하는 전용 절벽 레이어를 연결했다. 고도, 경사, 사면 방향에 따라 절벽과 눈 노출을 나눴다. 대표 `Terrain_4_7`의 최종 평균은 바위 63.0%, 절벽 13.9%, 눈 15.3%다.
- Unity에서 적용 후 바위·눈·절벽 레이어를 읽고 고정 시점의 원경을 촬영했다. 적용 타일의 RAW 대비 높이 오차는 이전과 같은 최대 약 0.01223m였다. 원경 전환 거리는 기존 1,000m다. 실제 플레이 중 프레임 성능과 미술 인수는 미수행이다. [보정 후 Unity 화면](../../Tools/RiverBasinWorld/Rebuild/RiverBasin4km_NorthCliffFinal.png)

### 4km 제작용 HeightMap 적용

기존 `RiverBasin4km` 씬과 월드 ID를 유지하고 4km 월드 좌표에서 생성한 제작 지형으로 교체했다. 북부는 봉우리·안부·침식 골짜기로 구분하고 동부에는 층상 협곡 사면을 저작했다. 원래 있던 두 호수·숲 연못·7개 물길과 거점 연결 관계를 이어갔다.

- [같은 RiverBasin4km 씬](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/RiverBasin4km.unity) · [4km 원시 HeightMap](../../Tools/RiverBasinWorld/Rebuild/RiverBasin_4097_Production_R16LE_SouthFirst.raw) · [저작 좌표·산체 계획](../../Tools/RiverBasinWorld/Rebuild/terrain-plan-4km.json) · [지형도](../../Tools/RiverBasinWorld/Rebuild/RiverBasin_4km_Production_Relief.png)
- 4,096m 정사각, 4,097² 표본, 1m 간격이다. 프로필 수직 범위 −32~768m와 실제 고도 −30.58~688.39m를 구분한다. 제작 Base는 `Production/Height.bytes`이며 이전 `Source/BaseHeight.bytes`와 미리보기 TerrainData를 보존했다. [4km 원본 검증](../../Tools/RiverBasinWorld/Rebuild/production-height-validation.json)
- 512m TerrainData·콜라이더 64장을 씬에 연결하고, 실제 지형 텍스처 6종의 가중치를 경사·고도·호안 위치로 저장했다. 수면은 타일 단위 메시 35개와 기존 Lake Water 계열의 전용 복제 재질을 사용한다. 넓고 평평한 수면 내부는 큰 삼각형으로, 호안과 강은 작은 삼각형으로 구성했다. [산맥 화면](../../Tools/RiverBasinWorld/Rebuild/RiverBasin4km_Final_NorthRange.png) · [전체 조감](../../Tools/RiverBasinWorld/Rebuild/RiverBasin4km_Final_Overview.png) · [상류 근접](../../Tools/RiverBasinWorld/Rebuild/RiverBasin4km_Final_UpperRiver.png)
- 저장 씬 재개방에서 타일 경계 112곳의 높이 차 0m, 새 RAW 대비 최대 오차 0.01223m, 콜라이더·수면 참조를 확인했다. 물길 중심선 1,194개 표본의 단절과 교량 예정 구간 외 동선 1,245개 표본의 침수는 0이다. 일반 순환로 중심선 최대 경사는 8.86°다. 고산 접근로는 9.63°다.
- 조감에서 좁은 물길 일부가 점선처럼 보이는 표현은 남아 있다. 실제 도로·교량 배치, 수영 판정, TPS 이동 및 미술 인수, Bake·스트리밍은 미수행이다. 첫 4km 비교판은 아래 이력에 남긴다.

### 강둑 보정과 4km 미리보기

사용자 근접 캡처의 사각 수면 가장자리와 일정한 강둑 단면을 보정했다. 강폭·하상 깊이·둑의 사면을 구간별로 바꾸고, 수면 삼각형은 지형과 수위가 만나는 위치에서 잘랐다. 기존 2km 씬의 지형에는 절대 높이 보정 패치 1건을 더했고 수면 메시 자산을 갱신했다. 2km Base RAW와 서쪽 수계 수정 전후 기록은 보존했다.

- [RiverBasin4km 씬](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/RiverBasin4km.unity) · [4km 조감](../../Tools/RiverBasinWorld/Rebuild/RiverBasin_4km_Overview.png) · [숲 연못 합류부](../../Tools/RiverBasinWorld/Rebuild/RiverBasin_4km_ForestShore.png) · [남서 호수](../../Tools/RiverBasinWorld/Rebuild/RiverBasin_4km_SouthwestInlet.png)
- 첫 4km 판은 2km 계획의 수평 좌표를 두 배로 옮기고 같은 고도를 사용했다. 중간 표본은 보간해 4,096m 정사각형에 4,097² 높이 표본과 512m Terrain 64장을 저장했다. 재개방 검사에서 콜라이더 참조와 경계 112곳이 일치했으며 Base RAW 대비 최대 높이 오차는 약 5.87mm다. [4km 원본 메타데이터](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Source/BaseHeight.json)
- 수면을 잘라 낸 뒤 물길 제어점 59곳이 수면 삼각형 안에 있는지 검사했다. [보정한 2km 수치 검사](../../Tools/RiverBasinWorld/Rebuild/shore-validation.json)는 중심선 표본 615개 수면 연결과 교량 예정 구간 외 동선 표본 609개 침수 0을 기록한다. 이 검사는 발행본의 플레이 검증을 대신하지 않는다.
- 첫 4km 판의 색과 물 재질은 검토용이다. 4km 기준에서 거점 사이 이동 시간, 넓어진 교량 횡단부와 세부 호안 아트, 실제 수영과 TPS 이동, Bake·스트리밍은 미수행이다.

### 남서 호수와 서쪽 숲 수계

사용자 표시를 따라 본류 중류에서 남서 호수로 이어지는 지류와 두 하구를 추가했다. 서쪽 숲에는 상류 개울과 연못을 두고, 연못 물길은 남서 호수로 연결했다. 기존 순환로 일부를 호안 북쪽으로 옮겼고, 새 지류 횡단 B3·B4의 상판 높이를 계획에 반영했다.

- [갱신한 수계 지형도](../../Tools/RiverBasinWorld/Rebuild/RiverBasin_WestWater_Relief.png) · [Unity 적용 화면](../../Tools/RiverBasinWorld/Rebuild/RiverBasin_WestWater_Unity.png) · [변경본 RAW](../../Tools/RiverBasinWorld/Rebuild/RiverBasin_2049_WestWater_R16LE_SouthFirst.raw)
- [같은 WorldBuilder 미리보기 씬](../../Assets/WorldBuilder/Worlds/f150b4516d3140f58fae94b43f64432c/Preview/WorldPreview.unity)에 RAW의 높이 변경 831,765개를 절대 높이 패치 1건으로 저장했다. Base RAW와 월드 ID를 보존했다. 4m 격자로 만든 검토용 수면 메시를 함께 저장했다. Editor에서 재개방해 지형 16장과 수면 자산을 읽었고 지형 경계 24곳의 높이 차 0m, 변경 RAW 대비 최대 오차 약 5.87mm를 확인했다.
- [수치 검사](../../Tools/RiverBasinWorld/Rebuild/west-water-validation.json)에서 남서 호수 중심 수심 9m·숲 연못 중심 수심 6m, 7개 물길 중심선 표본 615개의 수면 연결, 교량 예정 구간을 제외한 동선 표본 609개의 침수 0을 확인했다. 순환로 중심선 최대 기울기는 16.47°다.
- 정식 물리 수역과 수영, 교량 제작과 실제 주행은 남아 있다. 검토용 수면은 좁은 하천 가장자리에서 4m 격자가 보인다. 아래 기록은 서쪽 수계 변경 전 기준 지형의 제작·검증 이력이다.

2026-11-20 **독립 신작 HeightMap과 Unity 지형 블록아웃 생성 · Editor 재개방 및 수치 검사 통과.** v1.1 전달 HTML의 2km·1m 표본·512m 타일 계약과 지형/수면 분리 원칙을 반영했다. C 아틀라스의 5개 바이옴과 3개 거점이 맡는 공간 역할을 검토해 재구성했다. 새 지형의 수직 범위는 −32~352m로 정했다. C의 이미지와 RAW는 픽셀 정합된 지도가 아니며 700m 설정은 C의 원본 자산에 해당한다. 신규 원본은 이전 RiverBasin Terrain 및 C RAW를 입력으로 사용하지 않는다.

| 산출물 | 위치 |
|---|---|
| 신규 공간 설계 | [강 유역 월드 재설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_WORLD_REBUILD_DESIGN.md) |
| 저작 원본 | [지형·수계·동선·조망 계획](../../Tools/RiverBasinWorld/Rebuild/terrain-plan.json) · [높이맵 생성기](../../Tools/RiverBasinWorld/Rebuild/generate-heightmap.mjs) |
| 생성물 | [새 R16 RAW](../../Tools/RiverBasinWorld/Rebuild/RiverBasin_2049_R16LE_SouthFirst.raw) · [수계·동선 지형도](../../Tools/RiverBasinWorld/Rebuild/RiverBasin_Relief.png) · [수치 검사 결과](../../Tools/RiverBasinWorld/Rebuild/terrain-validation.json) |
| Unity 월드 | [지형 미리보기 씬](../../Assets/WorldBuilder/Worlds/f150b4516d3140f58fae94b43f64432c/Preview/WorldPreview.unity) · [Unity 조감 캡처](../../Tools/RiverBasinWorld/Rebuild/RiverBasin_Unity_Overview.png) · [WorldDefinition](../../Assets/10.Datas/WorldBuilder/f150b4516d3140f58fae94b43f64432c_Authoring.asset) |

- 2,048m 정사각형에 512m Terrain 16장을 배치했다. R16 little-endian 남쪽 첫 행, 2049² 표본, 실측 높이 −16.71~294.84m다. Node에서 같은 입력으로 다시 생성했을 때 RAW SHA-256 `bdb14bc21b18839ede1bfcf3dcc572f4cf48337efb379e3941421bcebb12f0f6`이 일치했다.
- Unity에서 저장 씬의 타일 16개·콜라이더와 도색 레이어를 확인했다. 경계 24곳의 높이 차 0m, RAW 대비 Terrain 높이 최대 오차 약 5.87mm다. 저작 정의의 P02 입력 검사도 통과했다. 물과 도로는 수계·동선 지형도에 표시하며 Unity 조감은 지형과 임시 바이옴 색을 렌더링한다.
- 동선 5개에서 교량 중심 반경 32m의 연결 예정 구간을 빼고 628개 중심선 구간을 검사했다. 계획 수위 기준 침수 0, 최대 기울기는 호수 순환 14.70°, 고산 접근 17.28°다. 교량 위치 2곳에서 하상과 예정 상판의 수위 여유를 확인했다. 상판과 양안 접근부는 아직 제작 전이다.
- 관측자 지면 높이 +2m에서 가상 성채 높이와 교량을 잇는 지형 차폐 검사 기준으로 마을→성채, 굽이→첫 교량, 교량 이후→성채 3개 후보는 열린다. 고산 유적→호수는 능선에 가린다. X 1520/Z 1770 후보는 호수 수면 중심으로 시선이 열리지만 접근로·TPS 검증은 남아 있다.
- 호수 마을에서 첫 교량을 지나 중앙 성채로 오르는 구간의 실제 주행, 교량 접속 경사와 조망, 급한 절개면의 아트 보정은 후속 블록아웃에서 확인한다. 물 메시·물리 수역, 도로·교량, 건물·식생, 콘텐츠, Bake·스트리밍은 미수행이다.

## WorldBuilder HeightMap · 2026-11-20

**기준 지형과 저작 정의 생성 완료 · Editor 재개방 검증 통과.** 독립 씬의 마지막 저장 지형을 이관했다. 초기 `Generated/*.f32`와 1cm 넘게 다른 타일 표본이 37,104개였고 최대 차이는 약 11.08m였다. 원본 씬과 TerrainData 9장의 전후 SHA-256이 모두 일치했다.

| 산출물 | 경로 |
|---|---|
| 설계 | [독립 정규 월드 제작 설계안](../../Assets/docs/design/world-art/open-world/REGULAR_WORLD_PRODUCTION_DESIGN.html) · [실제 제작 경로 입력](../../Tools/RiverBasinWorld/production-plan.json) |
| 교환용 HeightMap | [SouthFirst R16 RAW](../../Tools/RiverBasinWorld/WorldBuilder/RiverBasin_1537_R16LE_SouthFirst.raw) |
| WorldBuilder 원본 | [BaseHeight.bytes](../../Assets/WorldBuilder/Worlds/5f12bb1019204196a2324aaf28a2f9dc/Source/BaseHeight.bytes) · [원본 규격과 SHA-256](../../Assets/WorldBuilder/Worlds/5f12bb1019204196a2324aaf28a2f9dc/Source/BaseHeight.json) |
| 저작 에셋 | [WorldProfile](../../Assets/10.Datas/WorldBuilder/5f12bb1019204196a2324aaf28a2f9dc.asset) · [WorldDefinition](../../Assets/10.Datas/WorldBuilder/5f12bb1019204196a2324aaf28a2f9dc_Authoring.asset) |
| Unity 미리보기 | [WorldPreview.unity](../../Assets/WorldBuilder/Worlds/5f12bb1019204196a2324aaf28a2f9dc/Preview/WorldPreview.unity) · [Editor 조감 캡처](../../Tools/RiverBasinWorld/WorldBuilder/RiverBasin_Terrain_Overview.png) |

- 규격은 1,536m × 1,536m, 1m 간격, 1,537² 전역 표본, 512m Terrain 3×3, 타일당 513² 표본이다. RAW는 4,724,738바이트의 헤더 없는 unsigned 16-bit little-endian이며 첫 행은 남쪽이다. 원점 고도 −32m, 수직 범위 288m, 실제 표면 높이 약 −14.68~207.58m를 보존했다.
- 후속 배치에는 `X(새 월드) = X(기존) + 768`, `Z(새 월드) = Z(기존) + 768`, `Y(새 월드) = Y(기존)`를 사용한다. 예를 들어 기존 제작 계획의 숲 다리 B1 중심은 새 월드에서 `(1075.20, 77.00, 752.64)`다. 좌표 변환 후 실제 접근로와 착지를 다시 확인한다.
- Unity의 기존 RAW 코덱과 Terrain 미리보기 생성기를 사용했다. 방향 표본 검사, RAW SHA 확인과 재가져오기, 저작 정의 연결을 통과했다. 저장 씬을 재개방해 TerrainData 해상도·위치·콜라이더 참조, 공유 경계 12곳, 원본 대비 2,368,521개 타일 표본을 대조했다. 저장된 Terrain 높이와 경계 차이 0m, RAW 인코딩 최대 오차 약 2.20mm다. RAW와 BaseHeight의 SHA-256은 `01a533e4d6572ee3be42f4188ecff375e6241caec53f5111d4cad2274afd4d49`다.
- 조감에서는 기존 하상과 산체가 읽힌다. 직선적인 수로 굽이와 일부 급한 둑·절개면도 남아 있다. 다음 블록아웃은 호수 마을 입구에서 호안과 숲 다리를 지나 초원으로 돌아오는 대표 구간에서 하상·호안 단면, 양안 접근 경사, TPS에서 다리가 처음 보이는 위치를 검증한다. 높이 보정은 저작 정의의 명령과 패치에 남긴다.
- 수면, 도로, 교량, 거점, 식생과 콘텐츠는 후속 저작 범위다. 실제 TPS 이동·조망·미술 인수와 Bake·스트리밍은 이 단계에서 미수행이다.

## 반영된 에셋

| 구분 | 위치 |
|---|---|
| 씬 | `Assets/01.Scenes/Ingame/Worlds/RiverBasinWorld.unity` (MapID `RiverBasinWorld`) |
| 전용 데이터 | `Assets/10.Datas/World/RiverBasinWorld/` (VisualR1: `RiverBasinPipeline.asset`, `RiverBasinSky.mat`) |
| 하늘·날씨 | `Assets/10.Datas/World/Sky/Sky_RiverBasin.asset`, `Assets/10.Datas/World/LivingWorld/Environment/RiverBasin_Weather_v1.json` |
| 납품 자산 폴더 | `Assets/03.Prefabs/Environment/RiverBasinWorld/`, `Assets/08.Models/World/RiverBasinWorld/`, `Assets/04.Images/World/RiverBasinWorld/` |
| 저작 입력 | `Tools/RiverBasinWorld/` (`world-plan.json`, `production-plan.json`, `asset-sources.json`, `Generated/*.f32`, `Source/`, `VisualR1/`, `VisualR3/`) |

최종 입력 기준 경로 길이는 A=2.315km, B=1.598km, C=2.472km, 대표 구간 평면 354.8m다. 길이는 XZ 평면 합계이며 이동 시간으로 환산하지 않는다.

## 단계 기록

| 단계 | 내용 | 상태 |
|---|---|---|
| 설계 | 지역·경로·VP·POI·검증 계획 | 문서 완료. 실제 조망·초견 검증은 미수행 |
| 1차 제작 | 전용 Terrain 9장, 교량·사당·수목 군락, 지도 계약·재질·수면 굽이, 검병 1명·발견 상자 1개 | 씬 저장됨. 기록 스크립트는 재실행 금지 |
| VisualR1 | 파이프라인·하늘 재질 | 데이터 존재. 개별 판정 기록 없음 |
| VisualR3 (B1, 2026-11-09) | 지형·물 비주얼 개선, 폭포 립 연결 | 교훈은 [지형·물 교훈](../../Assets/docs/guide/TERRAIN_WATER_ART_LESSONS.md)에 정리. 사용자 지적 후 재수정 |
| P5 | 정규 등록, 지도 배경, 정상 맵 전환 | **미제작** |

## 검증 결과

| 확인 | 결과 | 근거 |
|---|---|---|
| 정적 지지면·통행 중심선·양방향 NavMesh | 통과 | Tools README (원본 보고서는 삭제됨) |
| 카메라 가림 이름 규칙 | 8/8 | 위와 동일 |
| 단독 Play 초기화·실제 카메라 렌더 | Ready | 위와 동일. 입력 기반 검증 아님 |
| 경고 | 미등록 서비스 2건, 무게 프로필 폴백 1건, Localization 오류 | 위키 기록 |
| 미술 | NO-SHIP | 위키 기록. 사용자 인수 대기 |

## 사용자 테스트 대기·미수행

실제 입력 이동, 전투, 카메라, 물가 복귀, 밤, 저장/재진입, 성능, 미술 품질. 위 표의 한 항목만으로 전체 통과를 판단하지 않는다.

## 차단·주의

- **근거 소실:** `output/RiverBasinWorld/`(`RESULT.html`, `TEST_SPEC.html`, `audit.json`, 캡처)는 2026-11-13 output 정리로 삭제됐다. Tools README의 해당 링크는 끊겨 있다. 위 검증 수치는 그 보고서를 현재 다시 확인한 값이 아니다.
- **재실행 금지:** `build-world.csx`, `revise-stage.csx`, `finish-*.csx`, `place-encounter.csx`는 기록용이다. 수동 최종 배치가 반영돼 있어 재실행해도 같은 씬이 나온다고 보장하지 않는다. 재생성이 필요하면 현재 씬·전용 데이터·meta·ManualGameplay ID부터 보호한다.
- **git ignore:** 환경 에셋은 git ignore 대상이라 저장소 diff만으로 변경·복구 여부를 판단하지 않는다.
- 기존 OpenWorld 생성기와 과거 일회성 스크립트는 실행하지 않는다.

## 다음 행동

1. 사용자가 미술 방향을 확정하고 사용자 테스트 명세를 새로 작성한다(삭제된 `TEST_SPEC.html` 대체).
2. `audit-world.csx`·`capture-world.csx`로 현재 씬을 다시 검사해 이 문서의 검증 표를 갱신한다.
3. 위 경고 3종과 Localization 오류를 원인 분류한다.
4. P5 정규 등록은 사용자 지시 후 착수한다.
