# 연속 월드 저작

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

`author-exploration.mjs`는 다섯 지역의 저작 스크립트다. 실행 후 최종 입력인 `city-layout.json`을 검토하고 Unity 생성기를 실행한다. 월드 크기와 도시 변환은 유지한다.

- `landforms[].widthVariation`: 산체 폭의 완만한 변화. `heightBlendDistance` 기본 60m는 꺾인 산줄기의 선분 높이를 연속적으로 섞어 최근접 선분 변경 지점의 절개선을 막는다.
- `platforms`: 구조물 기초의 넓은 범위·높이·주변 전이 폭. 도로 정지 대용으로 사용하지 않는다.
- `props`의 `pitch`, `useAbsoluteHeight`, `alignBounds`, `solidBounds`: 경사로·석조 구조물 저작용. 단면 벽은 반대쪽 시각 면과 실제 두께의 충돌을 함께 만든다. 원본 프리팹은 수정하지 않는다.
- `props[].slopeEmbedDepth`: 큰 암반의 footprint 지면 9점 중 낮은 높이를 기준으로 기단을 매몰한다. 경사면에서 중심 피벗만 맞춰 암반의 앞면이 뜨는 문제를 줄인다. 메시 접지 bounds는 정지한 파티클의 원점 bounds를 제외한다.
- `pickups`: 고정 ID, 기존 ItemSO, 모델, 수량, 배치 좌표. 기존 DropItemActor/InteractionRespawnManager의 소비 저장 계약을 사용한다. 장식 목록에 저장 개체를 섞지 않는다.
- `encounters`: 고정 ID와 기존 MonsterActor 프리팹. 스탯·BT·Ability를 중복 정의하지 않는다.
- `explorationWorkUnits`와 `dungeonRooms`: 영역·연결·공간 의도를 설명하는 저작 메타데이터다. 검증 결과의 단일 기록은 `output/OpenWorld/Exploration/RESULT.md`다.

`node Tools/OpenWorld/validate-layout.mjs output/OpenWorld/Exploration`으로 입력을 검사한다. 설계 Y의 경사는 실제 Terrain 경사가 아니다. 생성 월드를 Edit Mode로 연 뒤 `audit-exploration.csx`를 MCP execute_code로 실행하면 실제 지형 표본과 경로 전체의 정/역방향 NavMesh 조회를 기록한다. 부분 선분 검사와 전체 조회 결과를 각각 보존한다.

`capture-exploration.csx`는 `output/OpenWorld/Exploration/capture-phase.txt`의 폴더명(`before`/`after`)에 같은 XZ·방향·FOV의 지면+1.7m 화면을 기록한다. 조감도만 LOD0를 강제하며 촬영 유틸리티는 전역 안개를 잠시 끈다. Editor 정지 화면이며 실제 플레이·팝인·투명 효과 비용을 검증하지 않는다. 장시간 입력 보행과 미술 인수는 프로젝트 테스트 지침의 사용자 직접 테스트로 구분한다.
