# 설산 성채 도시

[설산 성채 필드 설계서](../전달용/snow_design/snow_fortress_design.html) · [월드와 아트 위키](../wiki/world-art.md)

## 서쪽 골목 옛 폐성 제거

기본 `RiverBasin4km`에서 서쪽 골목의 옛 폐성 전용 배치 28개를 제거했다. 파손 성벽 12개와 버팀벽, 잔해 등 유적에 속한 오브젝트가 대상이다. 오르막과 돌 표식은 유지했다. Unity에서 저장본을 별도로 열어 이 위치에 활성 파손벽이 0개이며 설산 도시와 지형 64개가 로드되는 것을 확인했다. 실제 TPS 조작은 미수행이다. [제거 후 Unity 화면](snow-fortress-citadel-evidence/west-ruins-removed.jpg)

## 본성 실내 소품과 깃발

**FEEL-RB-SNOW-01 · 실내 미술.** 사용자 제보 원문은 “이것들도 너무 로우 폴리곤이다. 그리고 천은 Magica를 적용해서 자연스럽게 처리되도록 해라”이며, 첨부 화면은 본성 중앙 홀과 서쪽 서가 쪽 계단이다.

기본 `RiverBasin4km` 씬에 연결된 성채 도시 프리팹에서 깃발 6개, 등화대 6개, 책장 6개와 양쪽 계단을 보강했다. 깃발에는 직물과 눈꽃 문양, 철제 가로봉을 넣었다. 각 깃발은 MagicaCloth2 MeshCloth와 고정 마스크를 사용한다. 윗단 두 줄을 고정하고, 중앙 홀 범위의 약한 방향성 기류로 아래쪽을 움직인다. 책에는 책등, 표지, 종이 면을 만들었고 목재 장식장에 배치했다. 등화대는 철제 프레임과 호박색 유리로 제작했다. 양쪽 계단은 석조 아치와 받침을 넣고 기존 이동 경사면에 디딤판을 맞췄다. [Blender 원본](../../ArtSource/WorldCities/SnowFortressCity/SnowboundCrownKeep_Interior.blend) · [실내 FBX](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Authoring/SnowFortressCitadel/SnowboundCrownKeep_Interior.fbx) · [깃발 FBX](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Authoring/SnowFortressCitadel/SnowboundCrownKeep_Banners.fbx)

[계단과 석조 아치](snow-fortress-citadel-evidence/interior-steps.jpg) · [깃발과 가로봉](snow-fortress-citadel-evidence/interior-banners.jpg) · [책장](snow-fortress-citadel-evidence/interior-library.jpg) · [등화대](snow-fortress-citadel-evidence/interior-lantern.jpg)

Editor Play에서 깃발 6개가 초기화됐다. 3.5초간 측정한 깃발별 고정 정점 34개는 위치 변동이 없었고, 아랫단의 최대 움직임은 깃발별 1.7~2.7cm였다. 양쪽 디딤판 중앙 64곳의 지지면과 캡슐 여유를 확인했고, 디딤판 중앙의 표시 높이와 충돌 높이는 mm 단위까지 일치했다. 문루·홀·양쪽 방·상층을 잇는 임시 NavMesh 4경로는 모두 `PathComplete`였다. [Magica 실측](snow-fortress-citadel-evidence/interior-magica-play.txt) · [계단과 동선 검사](snow-fortress-citadel-evidence/interior-navigation.txt)

**사용자 테스트 대기.** `RB-SNOW-05` 기본 씬을 Play하고 본성 중앙 홀 `(3304, 233, 3359)`에서 깃발을 두세 초 본다. 윗단이 걸쇠에 붙은 채 아랫단이 약하게 흔들려야 한다. 좌우 계단을 왕복하며 발이 깊게 묻히거나 공중에 뜨는지와 카메라 가림을 확인하고, 서가와 등화대에 가까이 다가가 재질과 실루엣을 확인한다. [권역 아트 설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_REGION_CONCEPT_DESIGN.md#설산-성채-도시의-눈과-본성)

### 상층 의전 의자

상층 석조 단 위의 기존 의자를 목재 프레임, 청동 몰딩, 적색 쿠션과 자수 등받이를 갖춘 모델로 교체했다. 등받이는 홀 깃발의 눈꽃 문장을 재사용한다. 의자에 단순 충돌체 2개를 붙였고 Unity 저장본에서 문루·홀·양쪽 방·상층을 잇는 임시 NavMesh 4경로를 확인했다. 상층 경로는 의자 앞 `(3304, 241, 3378)`까지 연결된다. 실제 TPS와 미술 확인은 사용자 테스트 대기다. [Unity 화면](snow-fortress-citadel-evidence/interior-throne.jpg) · [동선 검사](snow-fortress-citadel-evidence/interior-throne-checks.txt) · [Blender 원본](../../ArtSource/WorldCities/SnowFortressCity/SnowboundCrownKeep_Throne.blend) · [Unity FBX](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Authoring/SnowFortressCitadel/SnowboundCrownKeep_Throne.fbx)

## 건축 적설과 본성 확대

기본 `RiverBasin4km` 씬에 반영했다. 주거와 장터의 기와, 문루의 슬레이트, 성벽 윗면은 표면 경사와 월드 좌표의 마스크를 사용하는 [적설 셰이더](../../Assets/06.Shaders/Environment/SettledSnowLit.shader)로 렌더링한다. 지역 프리팹에서 지붕 위 적설 오브젝트 3개를 제거했고 본성 모델의 적설 폴리곤도 제거했다. 프로젝트의 알파인 눈 텍스처를 재사용하고, 이 지역의 복제 재질로 적용 범위를 지정했다.

본성은 폭 × 깊이 × 높이 약 68 × 47 × 53m다. 기존 문턱 좌표를 기준점으로 삼아 폭과 깊이를 늘리고, 첨탑과 홀의 높이를 올렸다. 외벽에 층을 이룬 버트레스, 표면에 석재 줄눈과 노멀을 넣고 망루와 첨탑의 곡면 음영을 보강했다. 본성의 전용 충돌체도 같은 비율로 확대했다. `Terrain_6_6`의 본성 부지는 복제 지형에서 사면으로 이어지도록 조형했다. [지역 프리팹](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Production/RegionArt/RiverBasinSnowFortressCity.prefab) · [Blender 원본](../../ArtSource/WorldCities/SnowFortressCity/SnowboundCrownKeep_Masonry.blend) · [Unity FBX](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Authoring/SnowFortressCitadel/SnowboundCrownKeep_Masonry.fbx)

Unity에서 디스크 저장본을 별도 Preview로 열어 적설 재질 슬롯 268개, 적설 메시 0개, 셰이더 오류 0개를 확인했다. Terrain과 TerrainCollider가 같은 데이터를 사용하며 본성 부지 밖 높이 및 알파맵 차이는 0이다. 기존 문루 바깥에서 홀로 들어가는 경로와 홀에서 양쪽 방, 상층 갤러리로 이어지는 경로가 임시 NavMesh에서 모두 `PathComplete`였다. 가림 이름 규칙 EditMode 테스트는 8/8 통과했다. [저장본 검사](snow-fortress-citadel-evidence/masonry-editor-checks.txt)

[시장 적설](snow-fortress-citadel-evidence/masonry-roofs.jpg) · [본성 정면](snow-fortress-citadel-evidence/masonry-approach.jpg) · [권역 조감](snow-fortress-citadel-evidence/masonry-overview.jpg) · [문루 진입](snow-fortress-citadel-evidence/masonry-gate-ground.jpg) · [본성 내부](snow-fortress-citadel-evidence/masonry-interior.jpg)

실제 TPS와 미술 확인은 사용자 테스트 대기다. `RB-SNOW-03` 장터 `(3247, 181, 3225)`에서 처마와 기와에 적설이 이어지는지 확인한다. `RB-SNOW-04` 본성 문루 밖 `(3291, 219, 3322)`에서 중앙 홀로 들어가 양쪽 방과 상층을 왕복하며 계단과 카메라를 확인한다. LOD, 프레임 비용, Player·Addressables 빌드는 미수행이다. [권역 아트 설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_REGION_CONCEPT_DESIGN.md#설산-성채-도시의-눈과-본성)

## 기본 RiverBasin4km 씬 연결

[RiverBasin4km.unity](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/RiverBasin4km.unity)만 열면 설산 성채 도시와 탐색 가능한 본성이 함께 로드된다. 검토용 도시 루트를 [지역 프리팹](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Production/RegionArt/RiverBasinSnowFortressCity.prefab)으로 저장해 씬에 한 번 배치하고, 포석과 석축 데이터를 [제작 폴더](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Production/RegionArt/RiverBasinSnowFortressCitySurfaces.asset)에 연결했다. 이 씬의 `Terrain_6_6` 렌더링과 충돌은 동일한 본성 지형 데이터를 사용한다. 기존 지형과 비교한 높이 변경 2,308개는 본성 부지 주변에만 있고, 부지 밖 높이와 알파맵의 차이는 0개다.

Unity Editor에서 씬을 단독으로 다시 열어 지형 64개와 도시·본성 각 1개를 확인했다. 진입부와 중앙 홀 91/91, 좌우 계단 각 62/62, 상층 갤러리 48/48 보행 표본에서 지지면과 캡슐 여유를 확인했다. 양쪽 방의 아치를 지나는 경로를 포함한 임시 NavMesh 4개는 모두 `PathComplete`였다. [기본 씬 검사 기록](snow-fortress-citadel-evidence/regular-scene-checks.txt)

기본 씬을 단독으로 실행한 Editor Play에서 도시가 활성화되고 본성 홀 바닥이 전용 충돌체에 적중하는 것을 확인했다. 이 4km 월드의 P5 발행 등록은 아직 준비되지 않았다. Player·Addressables 빌드와 실제 TPS 왕복은 미수행이다.

건축 배치 조사에서 공방을 여관과 분리하고 새 자리로 석축을 옮겼다. 본성 앞 동·서쪽 성벽 끝단과 동쪽에 남은 옛 석축을 정리하고, 본성 바닥에 경사로와 옹벽을 다시 이었다. 변경 저장본에서 공방 옆 골목 150개와 본성 진입 180개 보행 표본 및 본성 임시 NavMesh 4경로를 확인했다. 실제 TPS 왕복은 사용자 테스트 대기다. [전후 화면과 검사](river-basin-world.md#건축-배치-전수-조사)

## 바위 관통과 경사 장터 보정

**BUG-RB-BUILDING-02 · S3 · Unity Editor 재현.** 건축 교차 검사에서 바위를 제외해 설산 성채 중앙 여관 옆의 암반 관통을 놓쳤다. 후속 화면에서 같은 장터의 가판이 경사에 묻히고 표지대가 지붕을 관통하는 것도 확인했다. 현재 기본 저장본에서 재현됐고 회귀 시점과 플레이 시작 상태는 미상이다. 기대 결과는 가옥·성벽의 실루엣과 보행로가 드러나고, 경사에 따라 가판의 바닥과 접근 높이가 자연스럽게 이어지는 배치다.

- **바위 배치 반영.** 중앙 여관 및 서쪽 공방 주변을 포함해 설산 성채 전역에서 건축·길·기단에 걸린 바위 21개를 지면에 다시 배치했다. 좁은 두 필지의 큰 바위는 각각 기존 크기의 60%로 조정했다. 같은 위치·회전 경계로 겹치던 중복 44개는 지역 프리팹에서 비활성화했다. [여관 앞 수정 전](river-basin-snow-market-evidence/inn-rock-before.jpg) · [수정 후](river-basin-snow-market-evidence/inn-rock-after.jpg) · [서쪽 공방 수정 전](river-basin-snow-market-evidence/workshop-rock-before.jpg) · [수정 후](river-basin-snow-market-evidence/workshop-rock-after.jpg)
- **장터 배치 반영.** 양쪽 가판 8개를 넓혀 벌리고, 가판마다 수평 석재 디딤단과 광장에 접하는 짧은 경사로를 뒀다. 가판 뒤 기단이 처마나 받침 기둥을 가리지 않게 높이를 잡았다. 차양을 관통하던 표지대, 석등, 화로를 옮기고 중복 화로 4개를 비활성화했다. 서쪽 끝 가판의 진입을 막던 우물을 광장 북서 모서리로 옮기고 낮은 원형 석단을 받쳤다. [동쪽 가판 수정 전](river-basin-snow-market-evidence/market-east-before.jpg) · [수정 후](river-basin-snow-market-evidence/market-east-after.jpg) · [서쪽 가판 수정 전](river-basin-snow-market-evidence/market-west-before.jpg) · [수정 후](river-basin-snow-market-evidence/market-west-after.jpg) · [장터 조감](river-basin-snow-market-evidence/market-overview.jpg)
- **Editor 검사 통과.** 기본 씬 재개방 후 활성 바위 43개, 동일 위치·경계 중복 0개, 길·기단 점유 0개를 확인했다. 건축과 회전 경계가 교차하는 후보 9쌍의 실제 삼각형 교차는 0쌍이었다. 가판 받침 72/72, 진입부 480/480, 중앙 통로 240/240에서 지지면과 캡슐 여유를 확인했다. 앞선 건축 보정 경로 924개와 본성 임시 NavMesh 4경로도 재검사했다. [바위 검사](river-basin-snow-market-evidence/editor-rocks.txt) · [가판 검사](river-basin-snow-market-evidence/editor-market.txt) · [기존 경로 재검사](river-basin-snow-market-evidence/editor-regression.txt)
- **사용자 테스트 대기.** 기본 `RiverBasin4km` 씬에서 평소 입력 장치로 `RB-SNOW-01` 여관 앞 `(3263, 192, 3259)`과 서쪽 공방 `(3216, 182, 3232)`의 처마·기단과 바위 간격을 본다. `RB-SNOW-02` 장터 `(3247, 181, 3225)`의 양쪽 가판과 우물을 오가며 진입면의 걸림과 카메라 끼임을 확인한다. 실패 시 항목 ID·위치·화면을 남긴다. Player·Addressables 빌드는 미수행이다. [월드 작업 위키](../wiki/world-art.md)

## 내성 본성과 실내 탐색

내성 문루의 두 측면을 기존 석조 성벽 모듈로 망루에 연결했다. 북쪽 외곽 성벽과 바위는 새 본성 외벽 밖으로 옮겨 실내에 돌출되던 부분을 정리했다. 기존 본채·예배당·병영이 놓였던 구역에는 약 53 × 39m, 최고 높이 약 39m의 고딕 본성을 배치했다. 문루에서 석재 오르막을 지나 중앙 홀에 도착하며, 좌우 아치로 두 방에 들어가고 양쪽 계단에서 6m 높이의 갤러리로 오를 수 있다.

열려 있던 씬에 미저장 변경이 있어 디스크 저장본을 기준으로 검토용 씬 두 개에 통합했다. [RiverBasin4kmSnowFortress.unity](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/RiverBasin4kmSnowFortress.unity)를 연 뒤 [SnowFortressCitadelReview.unity](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/SnowFortressCitadelReview.unity)를 Additive로 연다. 검토용 월드는 복제 지형의 본성 부지만 조형했고 기존 적설 알파맵을 옮겼다.

Astra가 Blender MCP로 제작한 모델은 시각 메시 243,588삼각형과 전용 충돌 메시 7,700삼각형으로 구성됐다. 프리팹에 출입구와 천장 충돌, 실내 조명을 연결했다. [Blender 원본](../../ArtSource/WorldCities/SnowFortressCity/SnowboundCrownKeep_Traversal.blend) · [Unity 프리팹](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Authoring/SnowFortressCitadel/SnowboundCrownKeep.prefab)

Editor 물리 검사에서 진입부와 홀 중앙 91/91, 양쪽 계단 각 62/62, 상층 갤러리 48/48의 지지면과 캡슐 여유를 확인했다. 양쪽 방 사이의 수동 직선 표본 일부는 기둥과 아치 벽을 만났다. 아치를 따라 우회하는 임시 NavMesh는 문루 밖–홀, 홀–서쪽 방, 홀–동쪽 방, 홀–상층 갤러리 4개 모두 `PathComplete`였다. 실내 천장 5곳에서 충돌을 확인했다. [Editor 검사 기록](snow-fortress-citadel-evidence/editor-checks.txt)

[문루와 양쪽 성벽](snow-fortress-citadel-evidence/gate-final.jpg) · [내성 조감](snow-fortress-citadel-evidence/overview-final.jpg) · [중앙 홀](snow-fortress-citadel-evidence/hall-final.jpg) · [방](snow-fortress-citadel-evidence/east-room-final.jpg) · [상층 갤러리](snow-fortress-citadel-evidence/gallery-final.jpg)

카메라 가림 규칙 EditMode 테스트는 8/8을 통과했다. 실제 TPS 입력과 카메라 체감, 미술 인수는 사용자 테스트 대기다. LOD, 런타임 NavMesh 베이크, 프레임 비용 측정, 정규 월드 등록은 미수행이다.

## 성곽 도시 재구성

사용자가 이전 블록아웃의 규모와 미술 품질을 불합격으로 판정했다. 새 버전은 설산 아래 사면 약 187 × 207m를 하나의 성곽으로 감싸고, 남문과 북동 내성 사이에 시가지 건물 56동을 배치했다. 남문 대로는 시장 광장과 서쪽 주거가, 동쪽 작업 구역을 지나 내성 문루로 오른다. 당시 보존했던 폐성은 이후 기본 씬에서 제거했다. [현재 상태](#서쪽-골목-옛-폐성-제거)

[RiverBasin4km.unity](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/RiverBasin4km.unity)에 [SnowFortressCity.unity](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/SnowFortressCity.unity)를 Additive로 열면 확인할 수 있다. [SnowFortressCityRegion.asset](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/SnowFortressCityRegion.asset)에 도시의 포석·석축·적설 메시와 미리보기 지역 정보를 저장했다.

ShadeCastle의 기와지붕과 석재 외곽 성벽, 던전 키트의 고딕 내성 모듈을 사용했다. 남문과 내성 문루의 좌우를 석축으로 받치고, 건물마다 터의 지면 높이에 맞춰 개별 석축을 설치했다. 시장 두 줄에 가판대·우물·화로를 놓고 작업 구역에 수레·목재·통을 배치했다. 지붕·망루에는 적설을 얹고 성벽 주변에 바위와 침엽수를 두었다.

Unity Editor에서 대로·골목·광장 7곳의 중앙 보행 표본 276개를 확인했다. 지지면 누락과 캡슐 겹침은 각각 0개였다. 남문·내성 문루의 통과 높이에서 가로 표본 17개씩이 열려 있었고, 임시 NavMesh에서 남문 접근–시장, 시장–서쪽 골목·동쪽 골목·내성 마당 4개 경로가 연결됐다. 주요 길의 최대 표본 경사는 36.7°다. [Editor 검사 기록](snow-fortress-city-evidence/editor-checks.txt)

문루·주거·작업장·성벽 부품의 가림 규칙을 보강했다. `CameraOccluderNameRulesTests` EditMode 8/8을 통과했다.

[Unity 조감](snow-fortress-city-evidence/overview.jpg) · [남문](snow-fortress-city-evidence/south-gate.jpg) · [시장](snow-fortress-city-evidence/market.jpg) · [주거 골목](snow-fortress-city-evidence/alley.jpg) · [내성](snow-fortress-city-evidence/citadel.jpg)

실제 라온 조작, 카메라 체감, 미술 인수는 사용자 테스트 대기다. 건물 내부·NPC·전투 콘텐츠와 정규 월드 등록·성능 계측은 미수행이다.

## 이전 블록아웃 기록

아래 씬과 검사 결과는 사용자 지적을 받았던 이전 버전의 비교 자료다.

### 이전 씬을 Unity에서 열기

[RiverBasin4km.unity](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/RiverBasin4km.unity)를 열고 [SnowFortressBlockout.unity](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/SnowFortressBlockout.unity)를 Additive로 연다. RiverBasin4km를 활성 씬으로 두면 기존 플레이어, 지형, 조명, 안개 설정을 함께 사용한다. [SnowFortressRegion.asset](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/SnowFortressRegion.asset)은 기존 MapRegionInfoSO를 사용한 미리보기 지역 설명과 동선 메시다. 정규 월드 스트리밍과 지도 등록은 후속 단계에 해당한다.

기존 폐성은 첫 전망대로 두고, 북동쪽 사면에 석교와 두 층의 성채를 놓았다. 설계서의 180 × 220m는 초기 제안이다. 기존 사면의 높이와 폐성에서의 시야를 기준으로 안뜰 발판 62 × 28m, 정상 발판 45 × 51.4m를 사용했다. 정상은 쉼터보다 약 108m 높다. 교량은 경사면 길이 64.2m, 폭 7m, 기울기 약 37.4°다. 교량은 폭을 확보했지만 경사가 가파르므로 실제 플레이에서 감속과 카메라 시야를 확인해야 한다.

| 지점 | Unity 좌표 X / Y / Z | 구성 |
|---|---|---|
| 산기슭 쉼터 | 3240 / 173.2 / 3190 | 경비소·병영과 성채 조망 |
| 숲 전투 터 | 3227 / 185.8 / 3248 | 침엽수 사이 주 경로와 동쪽 갈림길 |
| 폐성 전망대 | 3215 / 196.5 / 3303 | 선택 경로가 주 경로에 합류하고 교량이 드러나는 지점 |
| 교량 시작 | 3215 / 201 / 3311 | 석재 상판·난간 |
| 성문 | 3250 / 240 / 3348 | 개방 아치, 망루 두 개, 안뜰과 병영 |
| 정상 광장 | 3257 / 281 / 3409 | 주탑과 측면 하산로 |

던전 키트의 기존 문루·성벽·망루·본채·병영·예배당과 Idyllic Fantasy Nature 침엽수 프리팹을 재사용했다. 보행 구조의 이름은 기존 카메라의 `WB_P04_Road_`와 `WB_P04_Bridge_` Solid 규칙을 따른다. 기존 RiverBasin 지형, Toon Fog World, 카메라와 플레이어를 함께 사용한다. AI 점검에는 충돌체 기반의 임시 Unity NavMesh를 사용했다.

### 이전 검증

- Unity Editor에서 앵커 6개 접지 확인. 정상 경사로 356/356 지지면·중앙 캡슐 여유 확인. 교량과 하산 경사로 두 구간 각각 19/19 지지면, 캡슐 겹침 0개. 성문 개구부 가로 21개 표본에서 차단 0개.
- 임시 NavMesh에서 교량 시작–성문 안쪽, 성문 안쪽–정상 경사로 시작, 경사로 시작–정상 광장의 경로가 모두 `PathComplete`였다. 첫 구간의 전체 도달성 확인과 교량 상판의 물리 표본 검사를 수행했다. NavMesh가 교량을 끝까지 밟는 경로와 런타임 베이크는 별도 확인이 필요하다. [Editor 검사 기록](snow-fortress-evidence/editor-checks.txt)
- 55° 고정 카메라로 조감과 각 접근 지점의 실루엣을 확인했다. 화면은 블록아웃 기준이다. [조감](snow-fortress-evidence/overview.jpg) · [쉼터](snow-fortress-evidence/village.jpg) · [폐성](snow-fortress-evidence/ruins.jpg) · [교량](snow-fortress-evidence/bridge.jpg) · [정상](snow-fortress-evidence/summit.jpg)
- Editor Play 진입 전 검사에서 열려 있는 RiverBasin4km의 미저장 변경을 확인했다. 실제 입력으로 교량·성문·정상을 왕복하는 검증, 전투 카메라, 미술 인수는 사용자 테스트 대기다.

### 이전 계획에서 남은 제작

산기슭 마을의 NPC·보급, 숲 전투와 동굴 우회로, 안뜰 전투, 보스, 귀환 숏컷의 개방 조건을 구현해야 한다. 현재 동쪽 선택로와 하산 경사로는 공간 연결을 먼저 확인하기 위한 배치다. 지형 접합과 성채 외벽·적설·조명·날씨 아트 패스, LOD와 청크 등록, 60fps 목표 측정은 미수행이다. Player·Addressables 빌드는 미수행이다.
