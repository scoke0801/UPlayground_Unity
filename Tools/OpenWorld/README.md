# 연속 월드 저작

생성 원본은 `city-layout.json`, 원본 네 시나리오 씬, 해당 씬의 기존 콘텐츠다. 생성 씬을 직접 편집한 내용은 재생성 시 덮어쓴다. 미저장 씬이 있으면 생성기는 실행을 거부한다.

현재 recipe는 세계관 지도 기반 확장 배치다. 최신 결과는 `output/OpenWorld/Expansion/`, 사용자 검수는 `Assets/docs/design/OPEN_WORLD_EXPANSION_TEST_SPEC.md`를 따른다. `Redesign/`의 화면·거리·검수 결과는 이전 배치의 기록이다.

## 편집과 재생성

- `landscape.landforms`: 능선·전망 턱·마른 하곡·해안 저지대를 월드 좌표의 선과 영향 폭으로 저작한다. 물길 선은 지형이다. 스토리의 가뭄/회복 상태를 임의로 바꾸지 않는다.
- `landscape.worldOrigin/worldSize/worldResolution`: 하나로 합치는 Terrain의 범위·해상도. `edgeTransition`은 북쪽 원본의 외곽을 새 지형으로 잇는 폭이다. 내부 호수와 생활권은 원본 지면을 보존한다. `reliefAmplitude/reliefFrequency`는 새 지형의 작은 요철이다.
- `landscape.groundCoverPrefab/groundCoverDistance/groundCoverDensity`: 기존 루트 MeshRenderer 프리팹으로 길 주변 Terrain 식생을 만든다. 원본 북쪽 영역·도시 기초·수면·급사면·길 중심은 제외한다.
- `landscape.props[].castShadows/particleLimit/particleColor`: 작은 식생의 그림자와 국소 안개의 입자 예산·색을 생성 인스턴스에만 적용한다. ExternalAssets 원본은 수정하지 않는다. 나무 접지는 LOD 전체 bounds의 하한이 아니라 실제 줄기 원점을 기준으로 한다.
- `cities[].preservedGround`: 원본 좌표의 건물/작업장 기초 보존 범위. `groundTransition`은 주변 지형과 잇는 폭이다. `sourceGroundCeiling`은 원본의 장벽 같은 외곽 산지를 잘라 내기 위한 상한이다. `raisedFoundation`과 `raisedFoundationTransition`은 높은 주택지와 접근 경사를 상한 적용에서 제외한다. 건물의 높이를 일괄 내리는 방식으로 해결하지 않는다. 원본 에셋은 수정하지 않는다.
- `cities[].road`, `routes`: 주도로와 대체 경로. 높이까지 저작한다. 폭을 좁히는 것으로 건물 관통을 해결하지 않는다. 지형 생성 뒤 실제 카메라로 걷는다.
- `landscape.props`: 기존 프리팹을 사용한 군락/암반의 개별 배치. `groundOffset`은 해당 모델의 뿌리 기준 또는 바위 매몰량이다. 저장 개체 프리팹은 허용하지 않는다.
- `relocations`: 기존 장식만 위치를 옮긴다. 저장 ID가 있는 콘텐츠는 이 목록으로 이동하지 못한다.
- `cities[].groundedProps`: 뿌리 원점이 확인된 표지판의 개별 접지 경로. 건물·교량·부두를 이 목록에 넣어 일괄 접지하지 않는다.
- `landscape.manualArtPrefab`: 반복 생성 때 다시 인스턴스화할 수작업 환경 프리팹. Unity에서 별도 프리팹으로 저작하고 경로를 기록한다. 생성 씬 내부 직접 편집 대신 이 프리팹 또는 recipe를 고친다. 현재 슬롯은 비어 있다.
- `landscape.includeArt=false`: 시제품의 지형·도로 검수. 배치 수로 구조의 품질을 판정하지 않는다.
- `navigationVoxelSize`: 생성 NavMesh의 복셀 크기(m). 양수일 때만 기본값을 덮어쓴다. 현재 0.4m이며, 변경 후 좁은 길과 장거리 경로를 함께 검사한다.

Edit Mode에서 `UPlayGround.Editor.OpenWorldSliceAuthoring.Build()` 또는 `UPlaygroundToolCatalog.TryExecute("OpenWorld/대표 구간 재생성", out error)`를 실행한다. 제작 명령은 툴 런처에 추가하지 않는다. 생성기는 기존 생성 씬/지형/NavMesh를 백업하고 실패 시 복구한다. Player/Addressables 빌드를 실행하지 않는다.

`node Tools/OpenWorld/validate-layout.mjs`는 좌표·경사·배치 ID·에셋 참조를 검사한다. `node Tools/OpenWorld/render-expansion.mjs`는 현재 배치도를 갱신한다. 둘 다 Unity 씬을 생성하지 않는다. `render-layout.mjs`와 `Redesign/configure.mjs`, `dress.mjs`는 이전 작업 기록이다. `Expansion/configure.mjs`도 저작 과정 기록이며 재생성의 입력은 최종 JSON이다.

## 보존 경계

원본 씬·콘텐츠 GUID·도착 ID·mapId는 유지한다. 성채 offset은 `(760,52,280)`, 항구는 `(2100,40,-100)`, 회전은 둘 다 기존 180도다. 원본 지역 좌표로 저장하고 생성 월드 좌표로 복원하는 기존 계약을 사용한다. 확장 Terrain 범위의 과거 저장 Y를 현재 지면으로 복구하며 가까운 구조물 지지가 있으면 그대로 둔다. 원본 Single 씬에는 복구를 적용하지 않는다.

자동 검사는 시각·전투·탐험 품질 인수를 대신하지 않는다. 기존 미니맵 배경 갱신은 별도 미완료 항목이다.
