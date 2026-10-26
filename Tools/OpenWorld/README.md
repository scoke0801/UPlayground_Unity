# 연속 월드 저작

생성 원본은 `city-layout.json`, 원본 네 시나리오 씬, 해당 씬의 기존 콘텐츠다. 생성 씬을 직접 편집한 내용은 재생성 시 덮어쓴다. 미저장 씬이 있으면 생성기는 실행을 거부한다.

## 편집과 재생성

- `landscape.landforms`: 능선·전망 턱·마른 하곡·해안 저지대를 월드 좌표의 선과 영향 폭으로 저작한다. 물길 선은 지형이다. 스토리의 가뭄/회복 상태를 임의로 바꾸지 않는다.
- `cities[].preservedGround`: 원본 좌표의 건물/작업장 기초 보존 범위. `groundTransition`은 주변 지형과 잇는 폭이다. `sourceGroundCeiling`은 원본의 장벽 같은 외곽 산지를 잘라 내기 위한 상한이다. `raisedFoundation`과 `raisedFoundationTransition`은 높은 주택지와 접근 경사를 상한 적용에서 제외한다. 건물의 높이를 일괄 내리는 방식으로 해결하지 않는다. 원본 에셋은 수정하지 않는다.
- `cities[].road`, `routes`: 주도로와 대체 경로. 높이까지 저작한다. 폭을 좁히는 것으로 건물 관통을 해결하지 않는다. 지형 생성 뒤 실제 카메라로 걷는다.
- `landscape.props`: 기존 프리팹을 사용한 군락/암반의 개별 배치. `groundOffset`은 해당 모델의 뿌리 기준 또는 바위 매몰량이다. 저장 개체 프리팹은 허용하지 않는다.
- `relocations`: 기존 장식만 위치를 옮긴다. 저장 ID가 있는 콘텐츠는 이 목록으로 이동하지 못한다.
- `cities[].groundedProps`: 뿌리 원점이 확인된 표지판의 개별 접지 경로. 건물·교량·부두를 이 목록에 넣어 일괄 접지하지 않는다.
- `landscape.manualArtPrefab`: 반복 생성 때 다시 인스턴스화할 수작업 환경 프리팹. Unity에서 별도 프리팹으로 저작하고 경로를 기록한다. 생성 씬 내부 직접 편집 대신 이 프리팹 또는 recipe를 고친다. 현재 슬롯은 비어 있다.
- `landscape.includeArt=false`: 시제품의 지형·도로 검수. 배치 수로 구조의 품질을 판정하지 않는다.

Edit Mode에서 `UPlayGround.Editor.OpenWorldSliceAuthoring.Build()` 또는 `UPlaygroundToolCatalog.TryExecute("OpenWorld/대표 구간 재생성", out error)`를 실행한다. 제작 명령은 툴 런처에 추가하지 않는다. 생성기는 기존 생성 씬/지형/NavMesh를 백업하고 실패 시 복구한다. Player/Addressables 빌드를 실행하지 않는다.

`node Tools/OpenWorld/render-layout.mjs`는 설계 구조도와 경로 길이만 갱신한다. Unity 씬을 생성하지 않는다. `output/OpenWorld/Redesign/configure.mjs`와 `dress.mjs`는 작업 중 사용한 실험 기록이며 현재 recipe를 재현하는 명령이 아니다. 다시 실행하지 않는다.

## 보존 경계

원본 씬·콘텐츠 GUID·도착 ID·mapId·도시 좌표 변환은 유지한다. 변경된 지형 구역의 과거 저장 Y만 런타임에서 현재 지면으로 복구하며, 가까운 구조물 지지가 있으면 그대로 둔다. 원본 Single 씬에는 복구를 적용하지 않는다. 저장된 공중 위치를 변경 구역에서 복원하면 지면에 놓이는 정책이다.

최종 화면·검수 결과는 `output/OpenWorld/Redesign/RESULT.md`, 사용자가 걸을 동선은 `Assets/docs/design/OPEN_WORLD_CITY_TEST_SPEC.md`를 따른다. 자동 검사는 시각·전투·탐험 품질 인수를 대신하지 않는다.
