# 연속 월드 저작

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
