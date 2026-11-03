# 연속 월드 저작

몬스터 배치 시각화는 `output/OpenWorld/MonsterPopulation/monster-map.html`을 브라우저에서 직접 연다. 현재 씬의 448마리(신규 392·레벨 조정 21·기존 필드 23·시나리오 12)를 실제 미니맵 위에 표시하는 오프라인 단일 파일이다. 레벨·분류·이름 필터, 조우/개체 표시, 좌표 상세, 확대·이동, ‘이번 숲 보강만’ 252마리 필터를 지원한다. 갱신은 월드 Edit Mode에서 `export-monster-visualization.csx`를 실행한 뒤 `node Tools/OpenWorld/render-monster-population.mjs`로 생성한다. 배경 원본이 바뀌면 `output/OpenWorld/MonsterPopulation/map-background.jpg`도 원본 비율로 갱신하거나 제거해 원본 PNG를 포함시킨다. `node Tools/OpenWorld/test-monster-visualization.mjs`는 필터·선택·좌표 변환을 보조 검사하며 브라우저 화면 검수와 구분한다.

사용자가 표시한 빈 숲 6개 영역의 후속 보강은 `monster-infill-resolved.json`이다. `숲과 유적 내부 보강`에 추가 108개 조우·252마리, 누적 165개 조우·392마리를 저장했다. `plan-monster-infill.mjs`는 후보 폴리곤/격자 입력이며 `resolve-monster-infill.csx`와 `apply-monster-infill.csx`는 **이미 적용 완료한 일회성 저작으로 재실행하지 않는다**. 기존 배치·진행 ID와 원본 데이터를 보존했다. 숲의 장거리 연결은 동일 비용 필터, 지역 18m 교전 공간은 기존 이동 비용으로 각각 왕복 검사했다. 런타임 길 밖 비용 6과 기존 NavMesh 에셋은 유지했다. 실제 전투·추격·복귀·프레임 검수는 사용자 대기다. `test-monster-infill.mjs`는 후보 영역·성장 구간·기존 배치 보존을 검사한다. 저장본 검사는 `audit-monster-population.csx`, 상세 결과는 `output/OpenWorld/MonsterPopulation/RESULT.md`와 `TEST_SPEC.md`를 따른다.

최초 필드 몬스터 성장 배치는 `monster-population-resolved.json`을 기준으로 LakeVillageWorld의 `탐험 콘텐츠/필드 몬스터 성장 배치`에 저장했다. 당시 신규 조우 57곳·140마리, 기존 필드 몬스터 레벨 조정 21마리였다. 시작 근처 Lv1~5 → 길가 Lv10~15 → 외곽 Lv20~30 → 북부 옛 성채 Lv40으로 이어지며 기존 P09 장비 단계를 사용한다. 지역 전용 Non Humanoid 파생 데이터 31종은 `Assets/10.Datas/World/OpenWorld/Monsters/`에 있으며 ActorDatabase와 도감에 등록했다. 기존 스토리 전투·진행 ID·원본 P09 튜닝은 보존했다.

`apply-monster-population.csx`는 **적용 완료한 일회성 저작이며 재실행하지 않는다**. `plan-monster-population.ps1`은 후보 입력, `resolve-monster-population.csx`는 실제 지면·안전 거리·NavMesh로 위치를 확정하는 적용 전 도구다. 현재 저장본 검사는 읽기 전용 `audit-monster-population.csx`를 사용한다. 전체 월드 생성기에는 아직 합성하지 않았으므로 전체 재생성은 이 배치를 덮어쓴다. 씬·지역 전용 데이터와 프리팹·각 `.meta`·ActorDatabase·MonsterCodexDatabase를 함께 보존한다. 씬과 지역 데이터는 현재 Git ignore 대상이므로 로컬 저장본과 작업 전 백업을 보존한다. 결과와 플레이 검수는 `output/OpenWorld/MonsterPopulation/RESULT.md`, `TEST_SPEC.md`에 있다.

물과 남서 경관의 최신 보강은 `water-landscape-plan.json`이다. `AuthoredEnvironment/남서 초지와 물길 숲 보강`에 나무 147개·관목 89개·풀 군락 307개·바위 23개를 저장했다. 기존 길·밭·진행 개체를 보호하며 Terrain 높이는 바꾸지 않았다. 현재 NavMesh는 `Assets/10.Datas/World/OpenWorld/WaterLandscapeNavigation.asset`이다. `apply-water-landscape.csx`는 적용 완료한 일회성 저작으로 다시 실행하지 않는다. 확인은 `audit-water-landscape.csx`, 촬영은 `capture-water-landscape.csx`를 사용한다. 기존 경로 48개 양방향 연결과 신규 배치의 길 침범 0을 확인했다. 물 셰이더는 양면 수면·굴절·최소 환경광을 지원하며 LakeVillageWorld/HarborNaturalWater 재질을 조정했다. 결과와 사용자 검수는 `output/OpenWorld/WaterLandscape/RESULT.md`, `TEST_SPEC.md`를 따른다. 전체 생성기에 합성하지 않은 직접 저작이므로 씬·Terrain·전용 NavMesh·지도 이미지와 `.meta`를 보존한다. 아래는 이전 단계의 기록이다.

최신 저장본은 `district-infill-plan.json`의 **확장 생활권과 지형 경관**이다. 북부 취락·서부 채석 생활권·호수 동사면·서부 농경지·남부 창고촌에 프리팹 2,538개(건물 30개 포함), 밭 6곳을 저장했다. 중단 시점에는 배치와 NavMesh 저장은 끝났지만 서쪽 경로 하나가 끊겨 있었고 최종 기록이 없었다. `district-route-repair.json`의 실제 장애물 우회 동선을 지표·이동 우선 영역에 반영하고 작물 7묶음을 제거했다. 작물은 955묶음이며 현재 NavMesh는 `Assets/10.Datas/World/OpenWorld/DistrictInfillNavigation.asset`이다.

`apply-district-infill.csx`, `finish-district-infill.csx`, `polish-district-infill.csx`는 **이미 적용한 일회성 기록으로 재실행하지 않는다**. 저장본 확인에는 읽기 전용 `audit-district-infill.csx`를 사용한다. 기존 43개·신규 5개 경로가 모두 왕복 연결되며 새 길 중심·양옆 표본의 환경 장애물 충돌은 0이다. 농경지에서 기존 준비 장벽 한 곳을 지나므로 실제 플레이 검수는 해금 전·후를 나누며 장벽을 삭제하거나 진행을 우회하지 않는다. 결과·화면·사용자 테스트는 `output/OpenWorld/DistrictInfill/RESULT.md`, `TEST_SPEC.md`를 따른다.

`plan-district-infill.mjs`는 배치 난수 순서를 보존한 뒤 별도 수정 동선을 합성한다. 선택 인자로 출력 JSON 경로를 지정할 수 있다. `test-district-infill.mjs`는 기존 배치·입력 재현·적용/제외 수량을 검사하며 `node Tools/OpenWorld/check-marked-infill.mjs output/OpenWorld/DistrictInfill`은 작업 전 씬 개체 보존을 검사한다. 전체 월드 생성기에는 이 직접 저작을 아직 합성하지 않았으므로 **전체 재생성을 실행하지 않는다**. 씬·Terrain·NavMesh·`DistrictInfill/` 작물 메시 12개와 각각의 `.meta`를 함께 보존한다. 아래 내용은 앞선 단계별 기록이다.

최신 환경 배치는 `regional-cover-plan.json`의 **연속 경관과 농경지**다. 북서 고원·북동 바위숲·동남 교역림과 들판·남서 농경지에 수목 1,121개, 바위·관목·생활 소품 760개, 밀밭 10곳을 저장했다. 길 주변 작물 230묶음을 제거해 2,662묶음이 남는다. 중단 후 저장 상태를 확인했으며 `apply-regional-cover.csx`는 **이미 적용되어 재실행하지 않는다**. `audit-regional-cover.csx`는 이전 `RegionalCoverNavigation.asset`의 재베이크와 씬 저장까지 포함하므로 현재 씬의 단순 확인에 재실행하지 않는다. `capture-regional-cover.csx`는 지역 조감도·입구·전체 지도와 밭별 근접 화면을 남긴다. 기존 배치 결과·사용자 검수는 `output/OpenWorld/RegionalCover/RESULT.md`, `TEST_SPEC.md`를 따른다.

그늘성 안팎 경로 `(1540,510) ↔ (1410,420)`의 후속 수정은 `shade-castle-navigation.json`이다. 성벽을 가로지르던 우선 이동 영역을 북문·서쪽 외곽 경로로 교체하고, 기본 KCC 발판 한도를 넘던 정문 기초를 30cm 낮췄다. 현재 씬은 `ShadeCastleNavigation.asset`을 사용한다. `repair-shade-castle-navigation.csx`와 `repair-shade-castle-threshold.csx`는 **적용 완료한 일회성 기록으로 재실행하지 않는다**. 저장된 씬 확인은 재베이크·저장하지 않는 `audit-shade-castle-navigation.csx`를 사용한다. 최신 결과와 플레이 검수는 `output/OpenWorld/ShadeCastle/RESULT.md`, `TEST_SPEC.md`를 따른다. 전체 생성기에 통합하지 않은 씬 저작이므로 현재 씬·Terrain·`ShadeCastleNavigation.asset`·`RegionalCover/` 작물 메시와 각각의 `.meta`를 함께 보존한다. 이전 `RegionalCoverNavigation.asset`도 유지한다.

빈 공간의 최신 대형 시설은 `landmark-sites-plan.json`이다. `plan-landmark-sites.mjs`가 북부 옛 성채·서부 폐채석장·북동부 석관 묘역·동부 숲 보루·남동부 버려진 역참·남서부 무너진 수도원 6곳, 구조물 401개를 정의한다. LakeVillageWorld의 `AuthoredEnvironment/랜드마크 유적 구역`에 저장했다. 결과·화면·사용자 검수는 `output/OpenWorld/LandmarkSites/RESULT.md`, `TEST_SPEC.md`를 따른다. 환경과 탐험 동선 저작이며 신규 적·보상·퀘스트가 붙은 전투 던전은 아니다. `apply-landmark-sites.csx`는 백업과 새 그룹 부재를 요구하는 일회성 증분 적용 기록으로, 현재 씬에 중복 실행하지 않는다. 이후 `polish-landmark-sites.csx` → `audit-landmark-sites.csx` → `capture-landmark-sites.csx` 순서다. NavMesh는 `LandmarkSitesNavigation.asset`, 파생 양면 석벽 재질은 `Assets/10.Datas/World/OpenWorld/LandmarkSites/`다. 전체 생성기에는 합성하지 않았으므로 씬·Terrain·전용 NavMesh·파생 재질과 `.meta`를 함께 보존한다. 촬영 시 삭제된 식생의 GPU 드로우 캐시를 피하려고 GPU Resident Drawer를 잠시 끈 뒤 원래 설정으로 복원한다.

빈 공간 후속 배치는 `empty-space-plan.json`과 `plan-empty-space.mjs`다. LakeVillageWorld의 `AuthoredEnvironment/빈 공간 생활권`에 고원 쉼터·바위 전망길·땔감 작업 숲·숲 작업터·옛 고갯길·들녘 농가 6개 구역, 환경 프리팹 662개를 직접 저작했다. `apply-empty-space.csx`는 작업 전 씬/지형 백업과 새 그룹 부재를 요구하는 증분 적용 기록이다. 현재 씬에 중복 실행하지 않는다. `finish-empty-space.csx`는 전용 `EmptySpaceNavigation.asset`을 연결하고 경로와 진행 ID를 검사한다. `capture-empty-space.csx`는 구역별 조감도·지면 화면과 전체 지도를 기록한다. 결과·사용자 검수는 `output/OpenWorld/EmptySpace/RESULT.md`, `TEST_SPEC.md`를 따른다. 전체 생성기에는 아직 합성하지 않았으므로 전체 재생성은 이 직접 배치를 덮어쓴다. 현재 씬·Terrain·전용 NavMesh와 각 `.meta`를 함께 보존한다.

건축·소품·지형·식생의 프로젝트/외부 에셋 후보와 아트 선택 기준은 [맵·월드 오브젝트와 아트 선택 가이드](../../Assets/docs/guide/WORLD_OBJECT_ART_CATALOG.md)를 참고한다.

항구의 최신 미술 배치는 `harbor-revision-plan.json`이다. `plan-harbor-revision.mjs`는 오늘 생성한 수변 주택·동양풍 상점·생활 소품을 바탕으로 33종 리소스의 197개 배치를 만든다. 구형 HarborHouse/HarborWarehouse/HarborPier/HarborLighthouse/CoastalBoat와 던전 바닥은 사용하지 않는다. `derive-harbor-kit.csx`는 수변 재질을 공유하는 잔교·석축·계선주·곡선 선체를 만들고, `apply-harbor-revision.csx`는 기존 바다·광역 바위와 진행 ID를 보존해 항구 미술을 교체한다. `finish-harbor-revision.csx`는 내륙 수조 자국 복구·지면 채색·접지·수면 재질·내비게이션 저장을 처리한다. 공용 `Navigation.asset`은 덮어쓰지 않고 `HarborArtRevision/HarborRevisionNavigation.asset`을 연결한다.

적용 대상은 `Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity`다. 저장된 Edit Mode에서 작업 전 씬·지형 백업 후 derive → plan → apply → finish 순서로 실행한다. 이 증분 저작은 전체 생성기에 아직 통합하지 않았으므로 전체 월드를 재생성하면 재적용해야 한다. `audit-harbor-revision.csx`는 양방향 경로·지역 액터 충돌/접지·진행 ID·구형 모델 잔존·메시/재질을 검사한다. 결과와 실제 플레이 검수는 `output/OpenWorld/HarborArtRevision/RESULT.md`, `TEST_SPEC.md`를 따른다. 캡처 중 삭제한 모델이 겹쳐 보이면 GPU Resident Drawer의 일시적 드로우 캐시인지 확인하고, 프로젝트 설정은 원상복구한다.

돌등성의 최신 배치는 `citadel-revision-plan.json`이다. 사용자 화면 피드백에 따라 본성 도시 쪽 계단·상부 연결참, 성문·망루와 접속한 성벽, 연속 포장, 32채의 생활 구획과 정원·성밖 수목을 재저작했다. `full-replacement-plan.json`의 성채 배치는 이전 기록이다. `plan-citadel-revision.mjs`가 에셋 경로와 도로·성벽 간격을 검사하며 계획을 출력한다. `derive-citadel-entry.csx`, `derive-citadel-stair-entry.csx`는 원본을 보존한 진입부 파생 메시를 만든다. `apply-citadel-revision.csx`는 저장된 월드의 돌등성 미술 배치만 교체하고 기존 구간 그룹·진행 ID를 보존한다. `paint-citadel-revision.csx`는 별도 surface JSON에 따라 성 안의 알파맵 부분만 갱신한다. 이 작업은 전체 생성 파이프라인에 통합하지 않은 씬 저작이며 전체 재생성 전 통합이 필요하다.

결과는 `output/OpenWorld/CitadelArtRevision/RESULT.md`, 실제 보행·화면 확인 항목은 같은 폴더의 `TEST_SPEC.md`에 있다. 이전 자동 검사 통과 기록을 최종 미술 인수로 해석하지 않는다.

구름등 고원 후속은 `author-upland-discovery.mjs`다. 기존 쉼터·회랑·숲 뒤에 합성되며 남향 사면의 공간 발견 순환로와 분기/귀환 흔적을 입력에 유지한다. 새 보상·진행 상태는 없다. `apply-upland-discovery.csx`는 높이를 바꾸지 않고 새/옛 순환로 6m 주변 지표와 기존 모델 9개를 갱신한 뒤 NavMesh를 재베이크하는 **recipe 기반 증분 적용**이다. 실행 전 깨끗한 생성 월드 Edit Mode와 컴파일 완료가 필요하다. 완료는 `Iteration05/application.txt`로 확인하며 timeout 뒤 중복 실행하지 않는다. 전체 월드 재생성 검증과 구분한다.

현재 `test-upland-discovery.mjs`, `test-trade-discovery.mjs`, `test-corridor-discovery.mjs`, `test-forest-discovery.mjs`의 기본 출력은 **Iteration05**다. 선택 인자로 결과 폴더를 지정할 수 있다. 기존 회귀의 Iteration02/03/04 입력 기준은 유지하고 네 저작 함수의 합성까지 검사한다. 이전 결과 파일은 덮어쓰지 않는다. `audit-upland-discovery.csx`는 고원 본길·탐색로의 각 구간 왕복과 원경 생활권 접근 경로를 검사하며, `smoke-upland-discovery.csx`는 준비된 Play Mode의 11지점 접지·저장 높이 복구 API를 검사한다. 실제 입력 보행이나 파일 재시작을 대신하지 않는다. 화면·복구 정책·최우선 U5-01은 `output/OpenWorld/Exploration/Iteration05/RESULT.md`와 사용자 테스트 명세를 따른다. 아래 Iteration04 출력 설명은 당시 기록이다.

회랑 후속 저작: `author-corridor-discovery.mjs`는 기존 책장 두 개만 재배치하며 전체 저작 스크립트에 연결되어 있다. `test-corridor-discovery.mjs`는 Iteration03 기준 보존·반복 저작을 검사한다. `apply-corridor-discovery.csx`는 깨끗한 생성 씬에서 두 배치를 recipe의 bounds 정렬로 증분 적용하고 NavMesh를 재베이크한다. 결과와 사용자 검수는 `output/OpenWorld/Exploration/Iteration03/RESULT.md`를 따른다.

후속 운송 쉼터: `author-trade-discovery.mjs`가 기존 네 운송 소품·관목 하나·보상 위치·순환로를 재저작한다. 전체 저작 스크립트에도 연결되어 있다. `node Tools/OpenWorld/test-trade-discovery.mjs`는 Iteration02 기준 입력과 반복 저작·보존 경계를 검사한다. 최신 실행 상태는 `output/OpenWorld/Exploration/Iteration02/RESULT.md`를 따른다. 생성 완료와 후속 관목 보정·NavMesh 재베이크를 확인했다.

생성 원본은 `city-layout.json`, 원본 네 시나리오 씬, 해당 씬의 기존 콘텐츠다. 생성 씬을 직접 편집한 내용은 재생성 시 덮어쓴다. 미저장 씬이 있으면 생성기는 실행을 거부한다.

현재 recipe는 세계관 지도 기반 탐험 개선 배치다. 최신 결과는 `output/OpenWorld/Exploration/RESULT.md`, 사용자 검수는 `Assets/docs/design/OPEN_WORLD_EXPLORATION_TEST_SPEC.md`를 따른다. `Expansion/`과 `Redesign/`의 화면·검수 결과는 이전 배치의 기록이다.

## 편집과 재생성

- `landscape.landforms`: 능선·전망 턱·마른 하곡·해안 저지대를 월드 좌표의 선과 영향 폭으로 저작한다. 물길 선은 지형이다. 스토리의 가뭄/회복 상태를 임의로 바꾸지 않는다.
- `landscape.worldOrigin/worldSize/worldResolution`: 하나로 합치는 Terrain의 범위·해상도. `edgeTransition`은 북쪽 원본의 외곽을 새 지형으로 잇는 폭이다. 내부 호수와 생활권은 원본 지면을 보존한다. `reliefAmplitude/reliefFrequency`는 새 지형의 작은 요철이다.
- `landscape.groundCoverPrefab/groundCoverDistance/groundCoverDensity`: 기존 루트 MeshRenderer 프리팹으로 길 주변 Terrain 식생을 만든다. 원본 북쪽 영역·도시 기초·수면·급사면·길 중심은 제외한다.
- `landscape.props[].castShadows/particleLimit/particleColor`: 작은 식생의 그림자와 국소 안개의 입자 예산·색을 생성 인스턴스에만 적용한다. ExternalAssets 원본은 수정하지 않는다. 나무 접지는 LOD 전체 bounds의 하한이 아니라 실제 줄기 원점을 기준으로 한다.
- `cities[].preservedGround`: 원본 좌표의 건물/작업장 기초 보존 범위. `groundTransition`은 주변 지형과 잇는 폭이다. `sourceGroundCeiling`은 원본의 장벽 같은 외곽 산지를 잘라 내기 위한 상한이다. `raisedFoundation`과 `raisedFoundationTransition`은 높은 주택지와 접근 경사를 상한 적용에서 제외한다. 건물의 높이를 일괄 내리는 방식으로 해결하지 않는다. 원본 에셋은 수정하지 않는다.
- `cities[].road`, `routes`: 주도로와 대체 경로. 탐험로는 `followTerrain=true`로 지형을 따르며 Y는 설계 참고값이다. 도시 도로의 정지량도 `landscape.maximumRoadEarthwork`(현재 0.5m)를 넘지 못한다. 큰 높이차는 산체와 노선을 고친다. 폭을 좁히는 것으로 건물 관통을 해결하지 않는다.
- `landscape.props`: 기존 프리팹을 사용한 군락/암반의 개별 배치. `groundOffset`은 해당 모델의 뿌리 기준 또는 바위 매몰량이다. 저장 개체 프리팹은 허용하지 않는다.
- `relocations`: 기존 장식만 위치를 옮긴다. 저장 ID가 있는 콘텐츠는 이 목록으로 이동하지 못한다.
- `cities[].groundedProps`: 뿌리 원점이 확인된 표지판의 개별 접지 경로. 건물·교량·부두를 이 목록에 넣어 일괄 접지하지 않는다.
- `landscape.manualArtPrefab`: 반복 생성 때 다시 인스턴스화할 수작업 환경 프리팹. Unity에서 별도 프리팹으로 저작하고 경로를 기록한다. 생성 씬 내부 직접 편집 대신 이 프리팹 또는 recipe를 고친다. 현재 슬롯은 비어 있다.
- `landscape.includeArt=false`: 시제품의 지형·도로 검수. 배치 수로 구조의 품질을 판정하지 않는다.
- `navigationVoxelSize`: 생성 NavMesh의 복셀 크기(m). 양수일 때만 기본값을 덮어쓴다. 현재 0.4m이며, 변경 후 좁은 길과 장거리 경로를 함께 검사한다.
- `navigationOffRoadArea`, `navigationPreferredWidth`, `navigationOpenAreas`: 야지 비용과 우선 이동 폭/열린 활동 영역. `OpenWorldOffRoad` 비용 6, 도로·마을·회랑 비용 1이다. 길 밖도 보행 가능하며 장거리 기본 조회가 도로를 우선 탐색한다. 넓은 동등 비용 야지에서 발생한 `OutOfNodes`를 도로 선호로 해결한다. ProjectSettings의 새 영역은 생성 월드 Terrain에만 적용한다. 비용은 베이크 형상이 아닌 경로 조회 설정이다.

Edit Mode에서 `UPlayGround.Editor.OpenWorldSliceAuthoring.Build()` 또는 `UPlaygroundToolCatalog.TryExecute("OpenWorld/대표 구간 재생성", out error)`를 실행한다. 제작 명령은 툴 런처에 추가하지 않는다. 생성기는 기존 생성 씬/지형/NavMesh를 백업하고 실패 시 복구한다. Player/Addressables 빌드를 실행하지 않는다.

`node Tools/OpenWorld/validate-layout.mjs`는 좌표·경사·배치 ID·에셋 참조를 검사한다. `node Tools/OpenWorld/render-expansion.mjs`는 현재 배치도를 갱신한다. 둘 다 Unity 씬을 생성하지 않는다. `render-layout.mjs`와 `Redesign/configure.mjs`, `dress.mjs`는 이전 작업 기록이다. `Expansion/configure.mjs`도 저작 과정 기록이며 재생성의 입력은 최종 JSON이다.

## 보존 경계

원본 씬·콘텐츠 GUID·도착 ID·mapId는 유지한다. 성채 offset은 `(760,52,280)`, 항구는 `(2100,40,-100)`, 회전은 둘 다 기존 180도다. 원본 지역 좌표로 저장하고 생성 월드 좌표로 복원하는 기존 계약을 사용한다. 확장 Terrain 범위의 과거 저장 Y를 현재 지면으로 복구하며 가까운 구조물 지지가 있으면 그대로 둔다. 원본 Single 씬에는 복구를 적용하지 않는다.

자동 검사는 시각·전투·탐험 품질 인수를 대신하지 않는다. 기존 미니맵 배경 갱신은 별도 미완료 항목이다.

## 탐험 지역 저작과 검수

그늘숲 후속 입력은 `author-forest-discovery.mjs`이며 `author-exploration.mjs`가 쉼터·회랑 다음에 호출한다. `landscape.gatherables`는 고정 ID·기존 모델·InteractableActorSO·좌표·scale·yaw·상호작용 Collider 크기를 담는다. 생성기는 기존 GatheringActor를 만들며 별도 행동 매니저나 Quest/FlowGraph를 추가하지 않는다. `ForestWaterTimber.asset`과 `.meta`도 생성 입력의 의존 에셋이다. 씬/데이터 폴더는 현재 git ignore 대상이므로 다른 작업 환경으로 옮길 때 이 두 파일을 포함해야 한다.

`apply-forest-discovery.csx`는 생성기의 채집 생성 함수를 재사용하는 recipe 기반 증분 적용이다. 전체 월드 재생성 검증으로 간주하지 않는다. 이미 생성된 채집 ID는 다시 만들지 않으며, 통신 시간 초과 뒤에는 `Iteration04/application.txt`와 씬부터 확인한다. `smoke-forest-discovery.csx`는 실제 탐색 핸들러·기존 벌목 모션·획득·메모리 복원·명시적 리스폰을 검사한다. `audit-forest-discovery.csx`는 수로를 반드시 경유하는 여섯 구간의 왕복 연결과 실제 데이터/모델 bounds를 검사한다.

`test-forest-discovery.mjs`, `test-trade-discovery.mjs`, `test-corridor-discovery.mjs`의 이번 출력은 모두 `output/OpenWorld/Exploration/Iteration04/`다. 쉼터 검사는 쉼터·회랑·그늘숲 세 함수를 합성한다. 이전 Iteration02/03 산출물은 덮어쓰지 않는다. `author-exploration.mjs <입력 사본 경로>`로 전체 JS 저작의 동일성만 별도 확인할 수 있다. `render-exploration.mjs <결과 폴더>`는 위치도를 그 폴더에 기록한다. 검증 구분·최종 화면·사용자 테스트는 `Iteration04/RESULT.md`를 따른다.

`author-exploration.mjs`는 다섯 지역의 저작 스크립트다. 실행 후 최종 입력인 `city-layout.json`을 검토하고 Unity 생성기를 실행한다. 월드 크기와 도시 변환은 유지한다.

- `landforms[].widthVariation`: 산체 폭의 완만한 변화. `heightBlendDistance` 기본 60m는 꺾인 산줄기의 선분 높이를 연속적으로 섞어 최근접 선분 변경 지점의 절개선을 막는다.
- `platforms`: 구조물 기초의 넓은 범위·높이·주변 전이 폭. 도로 정지 대용으로 사용하지 않는다.
- `props`의 `pitch`, `useAbsoluteHeight`, `alignBounds`, `solidBounds`: 경사로·석조 구조물 저작용. 단면 벽은 반대쪽 시각 면과 실제 두께의 충돌을 함께 만든다. 원본 프리팹은 수정하지 않는다.
- `props[].slopeEmbedDepth`: 큰 암반의 footprint 지면 9점 중 낮은 높이를 기준으로 기단을 매몰한다. 경사면에서 중심 피벗만 맞춰 암반의 앞면이 뜨는 문제를 줄인다. 메시 접지 bounds는 정지한 파티클의 원점 bounds를 제외한다.
- 원본 지면에서 0.5m 이내인 기존 액터는 생성본에서 접지 오프셋을 유지해 지면 아래 스폰을 막는다. 원본 씬과 X/Z·ID는 유지하며 공중·구조물 위 배치는 보정 대상에서 제외한다. 변경 좌표는 `output/OpenWorld/actor-ground-contacts.txt`에 기록한다.
- `pickups`: 고정 ID, 기존 ItemSO, 모델, 수량, 배치 좌표. 기존 DropItemActor/InteractionRespawnManager의 소비 저장 계약을 사용한다. 장식 목록에 저장 개체를 섞지 않는다.
- `encounters`: 고정 ID와 기존 MonsterActor 프리팹. 스탯·BT·Ability를 중복 정의하지 않는다.
- `explorationWorkUnits`와 `dungeonRooms`: 영역·연결·공간 의도를 설명하는 저작 메타데이터다. 검증 결과의 단일 기록은 `output/OpenWorld/Exploration/RESULT.md`다.

`node Tools/OpenWorld/validate-layout.mjs output/OpenWorld/Exploration`으로 입력을 검사한다. 설계 Y의 경사는 실제 Terrain 경사가 아니다. 생성 월드를 Edit Mode로 연 뒤 `audit-exploration.csx`를 MCP execute_code로 실행하면 실제 지형 표본과 경로 전체의 정/역방향 NavMesh 조회를 기록한다. 부분 선분 검사와 전체 조회 결과를 각각 보존한다.

`capture-exploration.csx`는 `output/OpenWorld/Exploration/capture-phase.txt`의 폴더명(`before`/`after`)에 같은 XZ·방향·FOV의 지면+1.7m 화면을 기록한다. 조감도만 LOD0를 강제하며 촬영 유틸리티는 전역 안개를 잠시 끈다. Editor 정지 화면이며 실제 플레이·팝인·투명 효과 비용을 검증하지 않는다. 장시간 입력 보행과 미술 인수는 프로젝트 테스트 지침의 사용자 직접 테스트로 구분한다.

`audit-exploration-content.csx`는 실제 씬을 열어 저장 ID·Missing Script·지역 중복 NavMesh와 회랑 연결을 검사한다. Play Mode의 `smoke-exploration.csx`는 보상 취소·획득·메모리 상태 복구를, `smoke-exploration-regions.csx`는 세 지역 로드 후 연결·진입 차단 해제·논리 지역 전환을 검사한다. 두 스모크는 위치 이동 API를 사용하며 기존 위치·상태를 복구한다. 실제 입력 보행·전투·파일 저장/재시작 검사와 구분한다.

## 신규 던전 세 곳 (Iteration06)

`author-exploration.mjs`는 기존 네 저작 함수 뒤에 `authorDungeons`를 합성한다. `city-layout.json`의 세 접근 노선, `landscape.dungeonGround`, `Dungeon_*` props와 고정 ID 보상 세 개를 생성한다. 묻힌 등불전·사슬 제단·가시울 옛터는 서로 다른 원본 조립 공간이며, 기존 회랑은 별도로 유지한다.

- 파생 프리팹이 없으면 저장된 Edit Mode에서 `derive-dungeons.csx`를 기존 MCP `execute_code`로 실행한다. 원본 ExternalAssets 씬을 저장하지 않고 프로젝트 소유 프리팹을 만든다. 데모 실행 요소 제거·진입면 보정도 이 스크립트에 포함된다.
- 전체 Unity 생성 경로는 `BuildLandscapeArt` → `ApplyDungeonGround` / `PaintDungeonGround` → 기존 배치·보상·NavMesh다. 원본 Terrain의 고저차를 복사하고, 매립 지붕/입구 hole/연속 경사를 데이터로 적용한다.
- `apply-dungeons.csx`는 **Iteration06 시작 백업을 사용하는 작업 전용 증분 적용**이다. 최신 사용자 월드에 일반적인 갱신 도구처럼 실행하지 않는다. 시작 snapshot과 현재 씬의 동일성을 확인해야 하며 전체 Unity 재생성을 대신하지 않는다.
- `node Tools/OpenWorld/test-dungeons.mjs`는 기존 영역 보존·네 함수 합성·반복 저작·전체 JS 입력 재현을 검사한다. 이전 회차의 정확한 수량/입력에 고정된 JS 결과를 현재 변경의 합격 근거로 재사용하지 않는다.
- `audit-dungeons.csx`, `smoke-dungeons.csx`, `smoke-dungeon-rewards.csx`는 새 작업 폴더에 실제 씬 연결·Play Mode API 결과를 기록한다. 비동기 스모크의 `RESULT`가 나온 뒤 후속 검사/씬 전환/Play 종료를 요청한다.

이번 상태는 [Iteration06 결과](../../output/OpenWorld/Exploration/Iteration06/RESULT.md)와 [사용자 인수 명세](../../output/OpenWorld/Exploration/Iteration06/TEST_SPEC.md)를 따른다. Unity 전체 월드 재생성·실제 입력·파일 재시작·사망·회차 인수는 자동 입력 재현/경로/API 검사와 별개다. 신규 던전의 실제 생성 씬과 일부 파생 에셋은 git ignore 대상이므로 git diff만으로 존재/보존을 판정하지 않는다.
# 환경 전면 교체본

현재 `Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity`에는 `full-replacement-plan.json`을 바탕으로 PoorVillage·Dungeon·WorldCities 환경 1,088개를 배치했다. `AuthoredEnvironment/월드 구간` 아래 돌등성, PoorVillage, 항구, 던전, 호수와 사당, 붉은나무숲, 고갯길, 교역로와 들판, 그늘숲으로 정리되어 있다. 지형·공통 경계·NavMesh는 공통 루트에 남는다. 콘텐츠 지역 씬의 로딩 소유권과 진행 ID는 유지한다.

`plan-full-replacement.mjs`는 배치 JSON을 검증·출력한다. `apply-full-replacement.csx`는 교체 전 월드와 백업을 요구하는 일회성 적용 기록이며 현재 씬에는 재실행하지 않는다. 최종 계단·도로·항구 작업 발판 보정은 배치 JSON에도 반영했다. 직사각 바닥은 씬 인스턴스에 BoxCollider를 적용해 장식 메시 충돌에 의존하지 않는다. 원본 프리팹은 변경하지 않았다.

기존 `OpenWorldSliceAuthoring.Build()`는 원본 시나리오와 `city-layout.json`으로 월드를 새로 만들므로 **현재의 씬 직접 배치·그룹핑을 덮어쓴다**. 전체 재생성 전에는 이 전면 교체 배치 단계를 생성 파이프라인에 통합해야 한다. 기존 증분 스크립트도 과거 하이어라키 경로에 의존하므로 현재 씬에 그대로 실행하지 않는다.

`audit-full-replacement.csx`는 참조·진행 ID·양방향 경로·도시 주민 충돌을 검사하며 `capture-full-replacement.csx`는 Editor 정지 화면을 기록한다. 결과와 사용자 확인 항목은 `output/OpenWorld/FullReplacement/RESULT.md`, `TEST_SPEC.md`에 있다. 생성 씬과 NavMesh는 저장소 ignore 대상이므로 파일 전달 시 실제 에셋도 포함해야 한다.

## 시나리오 씬 정리

`LakeVillageWorld.unity`의 옛 평지 석벽 38개와 별도 보조 콜라이더 24개는 외부 스크립트 참조가 없는 것을 확인하고 제거했다. 사당 구역의 지하 소품·식생 62개와 귀환 포털 묶음 1개 및 연결 도착점은 현재 지면 높이로 보정했다. 수면과 지하 던전은 유지한다. 루트의 콘텐츠는 `시나리오`, `생활과 주민`, `탐험 콘텐츠`, `지역 이동`, `플레이 액터`, `조명과 카메라`로 묶었으며 기존 개체 이름·활성 상태·스크립트 직렬화 값과 참조는 유지한다.

`cleanup-scenario-hierarchy.csx`는 정리 전 씬에만 적용하는 작업 기록이다. 현재 씬에 재실행하지 않는다. 과거 루트 경로를 사용하는 일회성 저작 스크립트도 새 계층에 맞춘 뒤 사용한다. 검사 기록·사용자 테스트·정리 전 씬 백업은 `output/OpenWorld/ScenarioCleanup/`에 있다. 백업에는 커밋되지 않은 기존 월드 작업이 포함되므로 보존한다. Play Mode의 포털 이동과 실제 보행, NavMesh·Occlusion 재베이크는 미수행이다.

## 호숫가 지형과 수면 수정

현재 생성 씬의 `CoastalWater`는 기존 호수 셰이더를 사용하는 별도 `Assets/10.Datas/World/OpenWorld/HarborNaturalWater.mat`을 참조한다. 원본 `Harbor_Sea.mat`은 유지했다. 전체 월드 재생성은 이 씬 직접 변경을 덮어쓸 수 있으며 아직 생성 파이프라인에 통합하지 않았다. 수정 전 씬과 지형의 백업은 `output/OpenWorld/LakeWorldReview/`에 있다.

호수 남쪽 `LakeSouthBank_R2`는 제거하고 지형을 복원했다. 호수 내부 급경사의 하부는 완만하게 올리고 남쪽 육지를 연결했다. 지형에 덮이게 되는 수련 20개를 수면으로 옮기고, 확인된 접지 오류 18개와 남쪽 호안의 풀 105개를 보정했다. 물리 지면으로 판별한 다른 지표 장식 36개와 떠 있던 장식 바위 29개도 접지했다. 기존 프리팹으로 나무·관목·평상과 지게를 추가하고, 호숫가에 작은 환경 입자 두 개를 배치했다. Directional Light 강도는 1.9에서 1.6으로 낮추고 색을 조정했다.

원본 `LakeBoundary.asset`은 보존하고 남쪽 새 육지에 겹친 경계만 제거한 `LakeWorldReview/LakeBoundary.asset`을 씬에 연결했다. NavMesh는 별도의 `LakeWorldReview/LakeWorldNavigation.asset`으로 재베이크했다. 이전 NavMesh 백업은 `output/OpenWorld/LakeWorldReview/HarborRevisionNavigation.before.asset`에 있다. 호숫가의 주요 다섯 경로는 양방향 `PathComplete`이고 Missing Script는 0이다. 별도의 기존 항구 회귀 좌표 `(1920, 50, -350)`은 새 NavMesh와 변경 전 항구 NavMesh 모두에서 표본을 찾지 못했다. 실제 부두 동선 검증은 남아 있다.

`restore-lake-shore.csx`, `dress-lake-shore.csx`, `repair-lake-boundary.csx`, `bake-lake-shore.csx`는 이번 씬을 위한 일회성 저작 기록이며 현재 씬에 다시 실행하지 않는다. 전체 월드 재생성 파이프라인에는 아직 통합하지 않았다. 실제 입력 보행·물가 경계, 주야간 조명과 입자, 항구 수면의 화면·성능은 `output/OpenWorld/LakeWorldReview/TEST_SPEC.md`에 따라 사용자 확인 대기다.

## 지도에 표시한 다섯 빈터의 생활 경관

첨부 지도에 표시한 북쪽 고원·서쪽 길가·호수 동사면·마을 서쪽·남쪽 물길은 `AuthoredEnvironment/지도 표시 구역 생활 경관`으로 추가 저작했다. 기존 EmptySpace·LandmarkSites·RegionalCover·그늘성 경로 보정을 보존하고 작업터·쉼터·참배 숲길·농가와 밭·짐마당을 기존 동선에 연결했다. `marked-infill-plan.json`은 최종 배치 입력이며 `plan-marked-infill.mjs`로 재현할 수 있다. `apply-marked-infill.csx`와 `finish-marked-infill.csx`, `polish-marked-infill.csx`는 이미 적용한 작업 기록이므로 현재 씬에 다시 실행하지 않는다.

현재 씬의 NavMesh는 `Assets/10.Datas/World/OpenWorld/MarkedInfillNavigation.asset`이다. 씬·Terrain·NavMesh와 `MarkedInfill/`의 작물 메시 6개 및 `.meta`를 함께 보존한다. 전체 생성기는 이번 직접 배치를 아직 합성하지 않으므로 재생성 전에 통합이 필요하다. `audit-marked-infill.csx`는 저장본의 기존 38개·신규 5개 경로, 참조·진행 ID 및 신규 중심선 충돌을 검사하고, `check-marked-infill.mjs`는 이전 씬 개체 보존을 비교한다. 정지 캡처와 최종 결과·사용자 테스트는 `output/OpenWorld/MarkedInfill/RESULT.md`, `TEST_SPEC.md`를 따른다.
