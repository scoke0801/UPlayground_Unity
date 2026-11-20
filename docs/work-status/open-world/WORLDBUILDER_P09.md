# WorldBuilder P09 인수 검증

[작업 상태](WORLDBUILDER.md#p09-validation) · [월드 위키](../../wiki/world-art.md) · [수용 명세 20·21장](../../전달용/OpenWorld_HeightMap_Design_Regional_v1_1_Handoff/IMPLEMENTATION_SPEC.md#tests)

| ID | 실행 대상과 환경 | 통과 조건 | P09 판정 |
|---|---|---|---|
| T01 | HeightmapTests와 전달 33² RAW, EditMode | 남서·남동·북서·북동이 규격 값과 일치하고 방향 오류 거절 | 통과 · 33² 진단 RAW |
| T02 | HeightmapTests, EditMode | R16 decode/encode 후 전 샘플 및 checksum 일치 | 통과 · 모든 ushort와 B 파일 checksum |
| T03 | HeightmapTests와 B 미리보기, EditMode | SetHeights/GetHeights 왕복에서 480m 기준 ≤0.03m | 통과 · B 및 C native 왕복 |
| T04 | HeightmapTests, 저장 Terrain, URP 카메라 및 KCC | 수치 경계 bit 동일, Unity 경계 ≤0.01m, LOD와 충돌 경계 연속 | 사용자 테스트 대기 · 수치 경계 통과, 실시간 LOD와 충돌 경계 확인 잔여 |
| T05 | TerrainAuthoringSessionTests, EditMode | 4개 타일 공통 모서리의 native 높이·마스크 일치 | 통과 · 네 타일 접점과 native 적용 |
| T06 | TerrainOperationCore/Placement/Water/Ecology, EditMode | 반복·타일 순회 변경·병렬 평가의 canonical 해시 일치 | 통과 · 반복, 역순, 병렬 canonical |
| T07 | TerrainOperationCore/Placement/Water 및 P09 두 규모 저작, EditMode | 동일 편집 Dirty/Full 최종 해시 일치 | 통과 · 2km·4km 각 3회 동등, 후속 4km 같은 패치의 최적화 전후 지문도 일치 |
| T08 | TerrainAuthoringSession/WorldEcologyReloadValidation, EditMode | Sculpt 및 RAW patch 유지, 재생성·리로드 뒤 중복 없음 | 통과 · Sculpt와 외부 RAW 재생성 보존 |
| T09 | TerrainAuthoringSession/WorldBakeTraversal, EditMode | native 미캡처 편집을 검출하고 Capture/Revert 전 Bake 차단 | 통과 · 미캡처 편집 차단과 해소 |
| T10 | TerrainAuthoringSession/Placement/Water, EditMode | 높이·Hole·이동·삭제·물 경로 각 20회 Undo/Redo 뒤 동등 | 통과 · 높이·Hole·건물 이동·계획 제거·수계 경로의 Undo/Redo 20회 |
| T11 | Placement/PlacementAuthoring, EditMode | Delete와 Pin 유지, epoch 변경 시 잘못된 매칭 없이 orphan 보고 | 통과 · Delete, Pin, epoch와 orphan |
| T12 | Placement/PlacementAuthoring, EditMode | 배치 금지 footprint와 타일 간격 일관성 | 통과 · 예약 영역과 경계 간격 |
| T13 | WorldPlanningTraversal, EditMode 및 실제 KCC | 건물 지지와 출입구 접근, 지정 agent/캐릭터 연결 | 통과 · 기초 지지, 실 Agent 및 KCC 교량 |
| T14 | WorldCaveTraversal/Ecology, EditMode 및 URP Play | 포털 충돌·낙하·피복 결함 없음, 소유 Hole만 복구 | 통과 · 소유 Hole과 실제 KCC 동굴 횡단 |
| T15 | WaterTests/WaterAuthoring, EditMode | 수위 역전·의도하지 않은 호수 유출 없음, 수심·강둑 충족 | 통과 · 수심·수위·강둑·호수 유출 |
| T16 | WaterAuthoring 및 URP 카메라, Play | 폭포 두 소켓·수면과 VFX bounds/culling 검증 | 통과 · 수계 연결, 직접 VFX bounds와 감쇠 검사 |
| T17 | WorldBakeTraversal, WorldChunkStreaming 및 Agent 실주행 | 필수 경로와 양끝 준비 시 link, 점유·도착 보호와 완료·취소 해제 | 통과 · 생산 브리지의 링크 양끝 pin 유지, 완료·취소 해제와 취소 후 청크 회수 |
| T18 | 별도 P09 씬, KCC 및 AI·실패 주입 | 고속·반전·텔레포트·교량·동굴에서 void·중복 없음, 이동 철회 회수 | 사용자 테스트 대기 · 실제 교차 씬 실패 복귀, 격리 저장 재진입과 기존 격리 자동 검증 통과. 실입력 반전 품질 잔여 |
| T19 | P08 경로 재실행 및 P09 프로파일 | 워밍업 후 50회 동일 경로에서 씬·핸들 및 pin 회수, 메모리 증가분 설명 | 일부 귀속 · 핸들·소유 지형 안정. 진단 Player의 세 스냅샷에서 PhysX와 검증 이벤트 문자열의 증가 확인. Editor와 Player의 Mono Used 카운터 전체 귀속은 미확인 |
| T20 | WorldBakeTraversal 및 별도 P09 revision | 취소·예외·리로드·로드 실패 뒤 기존 Published 유지 | 통과 · 취소·미완료 발행 거절·포인터 교체 직후 예외 복원·Rollback, 도메인 리로드와 실제 Editor 재시작 후 두 revision 재개방 |
| T21 | GUID·의존성 검사와 실제 Windows Player 빌드 | 청크와 Player에서 저작 SO·RAW·Editor assembly 제외 | 통과 · P08 정적 키·GUID·지문 및 로컬 번들·Player 빌드 분리 |
| T22 | 로컬 Addressables standalone | 실제 번들에서 청크 로드·활성화·해제 및 종료 회수 | 통과 · 2km·4km·P08에서 Windows Player의 실제 번들 로드·활성화·해제 |
| T23 | 전투·이동·카메라·퀘스트·세이브·씬 로더, EditMode/PlayMode와 기존 씬 | 관련 smoke와 실제 사용자 입력, Missing Script 0 | 실패 · 기존 실패 31건 중 30건 통과, EditMode 세라 얼굴 모프 1건 재현. PlayMode 2건 통과. 기존 게임 실입력 사용자 테스트 대기 |
| T24 | Ecology 및 PlanningPreset EditMode | 코드 수정 없이 바이옴·나무·건물 프리셋으로 생성 | 통과 · 검증 fixture에서 데이터 프리셋으로 바이옴·식생·건물 생성 |
| T25 | 16/64타일 동일 기기·해상도·Quality | 저작·Bake·로드·프레임·GC·메모리 원본과 예산 비교 | 미수행 일부 · 4km Dirty 중앙값 2.63초, Editor GPU 유효 표본 2km 1,320개·4km 1,820개, 독립 Player 0개, Bake 메모리 경고 기준 초과 |
| T26 | 실제 URP 카메라와 재질 검사, Play/Player | Hole·나무·Detail·물·폭포·proxy 및 경계 표시, 분홍/누락 없음 | 사용자 테스트 대기 · 실제 URP 캡처·셰이더 오류 0, Detail 근접 화면과 동적 전환 품질 잔여 |


## 현재 실행 원본

- 전체 관련 EditMode는 1,642/1,671 통과, 실패 29, 건너뜀 0으로 끝났다. 그중 WorldBuilder는 123/123 통과했다. 실패는 Combat 7, Content 2, Core 3, FlowGraph 8, Movement 4, UI 5다. 이 실행은 P09 측정 코드 설치 전 상태다. [Test Runner 원본](../../../output/WorldBuilder/P09/EditMode.json) · [실패 목록](../../../output/WorldBuilder/P09/EditMode-failures.json).
- P08 경로 재실행은 씬 10·소유 씬 핸들 9·pin 0을 유지했다. 소유 Terrain/NavMesh/proxy 합계는 10,682,622B로 같았다. 종료 뒤 검증 씬만 남고 소유 씬·카탈로그 핸들과 pin은 0이었다. 미사용 자산 해제·GC 뒤 Unity 할당량은 1,190,812,895B에서 1,194,821,192B로 4,008,297B 증가했다. Mono 사용량도 1,773,568B 증가했다. 증가분의 소유는 미확인이다. [스트리밍 테스트 로그](../../../output/WorldBuilder/P09/T18-T19-existing-test.txt).

## 규모 측정

환경은 Unity 6000.3.21f1, i5-12400F, RTX 3060 Ti, RAM 65,395MiB, PC Quality 및 PC URP다. 저작 입력은 P09 전용 반복 높이 패턴 및 명시적 Agent 링크용 2m 경계 Hole이다. 타일당 512m, 간격 1m, Height 513², Hole 512²를 각각 유지한다. 32×32 패치에서 Dirty와 Full을 한 번 워밍업하고 세 번씩 기록한다. 값은 Editor 메인 스레드 작업의 경과 시간이다.

2km의 생성은 18.07초, Bare Bake 호출은 74.80초, 독립 카탈로그 재개방과 실행 씬 저장까지는 Bake 시작부터 78.23초였다. Dirty는 7.20/7.78/7.65초, Full은 9.47/9.09/8.89초로 세 번 모두 결과 해시가 같았다. [2km 저작 원본](../../../output/WorldBuilder/P09/authoring-4.log) · [2km Windows 메모리 표본](../../../output/WorldBuilder/P09/authoring-4-process.csv) · [2km 저작 씬](../../../Assets/WorldBuilder/Worlds/a909a909a909a909a909a909a909a904/Authoring.unity) · [2km 실행 씬](../../../Assets/WorldBuilder/Worlds/a909a909a909a909a909a909a909a904/Profile.unity).

| 저작 항목 | 2km · 4×4 | 4km · 8×8 |
|---|---:|---:|
| 높이 입력 | 2049² · 8,396,802B | 4097² · 33,570,818B |
| 생성 | 18.07초 | 68.60초 |
| Dirty 3회 | 7.20 / 7.78 / 7.65초 | 28.61 / 27.40 / 28.17초 |
| Full 3회 | 9.47 / 9.09 / 8.89초 | 34.33 / 33.97 / 34.84초 |
| Dirty와 Full 해시 | 3/3 일치 | 3/3 일치 |
| Bake | 74.80초 | 198.67초 |
| Bake부터 실행 씬 저장 완료 | 78.23초 | 208.12초 |
| Editor Unity 할당량 관측 최대 | 1.99GiB · 시작 대비 +0.92GiB | 8.03GiB · 시작 대비 +6.96GiB |
| Editor Windows working set 관측 최대 | 7.31GiB · 생성 이후 표본 | 13.69GiB · 호출 전 대비 +7.46GiB |

512MiB 작업 메모리 출발 기준을 Bake 구간에서 넘었다. 4km의 Dirty 중앙값은 Full의 약 82%다. 국소 편집에도 무결성 검사에서 전역 native 높이/Hole을 읽고 적용 후 전역 지문과 캐시를 다시 계산한다. 따라서 32×32의 작은 작업도 월드 크기에 민감하며 편집 명령이 메인 스레드를 수 초 이상 점유한다. Bake는 원본과 저장 복제 타일, NavMesh와 청크를 한 번에 만든다. Unity 내부 .NET 프로세스 값은 0이어서 OS working set은 PowerShell 관측값으로 제시했다. 2km OS 표본은 생성 후에 시작했다. [저작 경로](../../../Assets/02.Scripts/WorldBuilder/Editor/WorldTerrainAuthoringSession.cs) · [Bake 스테이징](../../../Assets/02.Scripts/WorldBuilder/Editor/WorldBakeStage.cs) · [4km 저작 원본](../../../output/WorldBuilder/P09/authoring-8.log) · [4km Windows 메모리 표본](../../../output/WorldBuilder/P09/authoring-8-process.csv) · [4km 저작 씬](../../../Assets/WorldBuilder/Worlds/a909a909a909a909a909a909a909a908/Authoring.unity) · [4km 실행 씬](../../../Assets/WorldBuilder/Worlds/a909a909a909a909a909a909a909a908/Profile.unity).

## Windows 빌드

첫 실행은 번들 101개의 레이아웃까지 생성한 뒤 Addressables의 후속 로그·content-state 저장에서 실패했다. 패키지가 프로젝트 루트를 자동으로 붙이는 위치에 절대 경로를 전달해 경로가 중복됐다. 프로젝트 상대 경로로 수정하고 새 폴더에서 재실행했다. [첫 빌드 원본](../../../output/WorldBuilder/P09/win64-local-01/build.log).

두 번째 실행의 로컬 Addressables는 성공했다. 레이아웃의 명시적 월드 키 99개, 번들 101개, 고유 에셋 경로 521개를 재검사했다. 검사 대상 RAW, 소스·저작 데이터, WorldBuilder Editor 의존성은 0건이다. [재실행 원본](../../../output/WorldBuilder/P09/win64-local-02/build.log) · [로컬 번들 레이아웃](../../../output/WorldBuilder/P09/win64-local-02/Addressables/buildlayout.json) · [분리 검사](../../../output/WorldBuilder/P09/win64-local-02/T21-build.txt).

## standalone 스트리밍

Windows Development Player 빌드는 22분 50.83초, 906,304,364B, 오류 0건이었다. Player의 패킹 목록 12,452개와 로컬 번들 의존성을 대조했고 WorldBuilder Editor DLL 및 검사 대상 저작 원본은 검출되지 않았다. 동일 실행 파일에서 P09 2km, P09 4km, P08 통합 월드 순서로 로컬 번들 청크를 실제 로드하고 모두 종료했다. [빌드·패킹 원본](../../../output/WorldBuilder/P09/win64-local-02/build.log) · [Player 원본](../../../output/WorldBuilder/P09/win64-local-02/Player.log).

RTX 3060 Ti, 1280×720 창 모드, PC URP, Direct3D 11, 60fps 목표에서 양쪽 끝 청크 중심을 5회 왕복해 워밍업했다. 정지 프레임 120개와 50회 왕복을 기록했다. 청크 최소 유지 진단값은 0초다. 프로파일 씬은 반복 지형·NavMesh·proxy 부하를 고정하고 P08 통합 씬에서 수계·동굴·식생을 따로 실행한다.

| D3D11 Player 지표 | 2km · 16타일 | 4km · 64타일 |
|---|---:|---:|
| CPU 프레임 p95 / p99 | 16.725 / 16.870ms | 16.710 / 16.818ms |
| 메인 스레드 p95 / p99 | 6.961 / 9.568ms | 7.420 / 10.910ms |
| GPU 프레임 p95 / p99 | 유효 표본 0 | 유효 표본 0 |
| 청크 Loading 상태 p95 | 88.841ms | 71.792ms |
| 청크 Unloading 상태 p95 | 117.272ms | 233.803ms |
| 활성화 그룹 CPU marker p95 / p99 | 7.198 / 9.395ms | 7.285 / 9.524ms |
| Ready 다음 프레임 경과 p99 | 16.675ms | 16.676ms |
| Ready 다음 프레임 경과 최대 | 16.707ms | 25.664ms |
| 정지 상태 코디네이터 평균 | 0.018ms | 0.038ms |
| Player 전체 GC Alloc p95, 정지 / 왕복 | 184 / 10,766B | 184 / 10,424B |
| Unity 할당량, 워밍업 / 관측 최대 | 155.87 / 164.52MiB | 164.97 / 174.32MiB |
| Ready 씬 / 소유 씬 / catalog | 9 / 9 / 1 | 9 / 9 / 1 |

Loading과 Unloading은 LateUpdate에서 관측한 상태 구간이다. Unity 씬 로드·엔진 활성화 비용이 함께 들어가고 unload에는 대기열이 포함된다. 4km 양 끝의 preload 영역은 겹치지 않으며 unload 동시성은 1이다. 해제 p95가 2배에 가까웠다. 활성화 그룹은 직접 프로파일 marker이고 Ready 다음 프레임 값은 화면 시간의 관측치다. 프레임 타이밍이 지연 수집되므로 최대 프레임 시간의 원인은 이 기록만으로 확정할 수 없다. 정지 상태의 코디네이터 평균은 0.5ms 출발 목표 이내다. Player 전체 GC Alloc에서 코디네이터 배분은 미확인이다. Windows working set 최대는 세 월드 연속 실행 전체에서 1,336,942,592B였다. [집계 원본](../../../output/WorldBuilder/P09/win64-local-02/metrics.json) · [2km 프레임](../../../output/WorldBuilder/P09/win64-local-02/frames-16.csv) · [4km 프레임](../../../output/WorldBuilder/P09/win64-local-02/frames-64.csv) · [Windows 프로세스 표본](../../../output/WorldBuilder/P09/win64-local-02/Player-process.csv).

### P08 메모리와 URP

기존 P08 경로의 재검사에서 Unity 할당량은 Editor +4,008,297B, standalone +3,005,525B였다. Player 통합 월드의 워밍업과 왕복 후 GC에서 각각 6,216개 Unity 객체와 소유 Terrain·NavMesh·proxy 10,599,426B가 같았다. 씬 10개, 소유 씬 9개, 카탈로그 1개, pin 0개도 일정했다. 종료 뒤 검증 씬 하나와 소유 핸들·pin 0으로 돌아왔다. catalog는 NavMesh와 proxy를 소유하고 활성 씬은 TerrainData를 소유한다. 전체 할당 증가의 소유와 전체 managed 객체 수는 확인되지 않았다. [P08 standalone 50회 원본](../../../output/WorldBuilder/P09/win64-local-02/player-p08/runtime-16.log).

Player URP 검증에서 Terrain Hole 84셀, 나무 59개, Detail prototype이 있는 활성 타일 12개, 폭포 1개, 활성 proxy 16→4와 셰이더 오류 0건을 확인했다. 네 캡처에서 교량·수면·나무·동굴 입구·폭포·proxy를 보았고 분홍 재질은 발견되지 않았다. Detail의 실제 근접 픽셀과 움직이는 카메라의 경계 전환 품질은 사용자 테스트 대기다. [URP 원본](../../../output/WorldBuilder/P09/win64-local-02/player-p08/T26.log) · [교량과 수면](../../../output/WorldBuilder/P09/win64-local-02/player-p08/T26-bridge-water.png) · [동굴 입구](../../../output/WorldBuilder/P09/win64-local-02/player-p08/T26-cave-portal.png) · [폭포](../../../output/WorldBuilder/P09/win64-local-02/player-p08/T26-waterfall.png) · [원경 proxy](../../../output/WorldBuilder/P09/win64-local-02/player-p08/T26-proxy.png).



D3D12와 D3D11 단일 스레드 렌더링에서도 두 규모를 각각 같은 경로와 해상도로 다시 실행했다. 세 조건 모두 GPU 프레임의 유효 표본이 0개였다. 그래픽 작업을 분리한 실행에서도 같은 결과가 나왔으며 GPU p95/p99는 미수행으로 남는다. [D3D12 프레임 집계](../../../output/WorldBuilder/P09/dx12/d3d12-logs/metrics.json) · [D3D11 단일 스레드 집계](../../../output/WorldBuilder/P09/dx12/metrics.json). [Unity FrameTimingManager API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/FrameTimingManager.html).

Windows working set 750ms 표본은 각 월드 로그의 생성부터 종료까지 2km 435,912,704~569,753,600B, 4km 560,287,744~631,156,736B였다. Scene과 AssetBundle 카탈로그는 각 월드 종료에서 해제되었고 Unity 할당량은 2km 103,682,212B, 4km 103,861,119B로 내려갔다. [2km 상태](../../../output/WorldBuilder/P09/win64-local-02/runtime-16.log) · [4km 상태](../../../output/WorldBuilder/P09/win64-local-02/runtime-64.log).


## PlayMode와 기존 씬

관련 저장소 PlayMode 4개 어셈블리는 129건 중 127건 통과, 실패 2건, 건너뜀 0건이었다. 실패는 보스 Lian 간격 압박 공격의 상태 전이 거절과 스태프 몬스터의 Bolt 대신 Fan 선택이다. 빌드 등록 씬과 P06·P07·P08 검증 씬 8개를 열어 Missing Script 0건을 확인했다. P08 두 revision의 Editor 재개방과 전이 의존성 검사도 통과했다. [PlayMode 원본](../../../output/WorldBuilder/P09/PlayMode.json) · [실패 원본](../../../output/WorldBuilder/P09/PlayMode-failures.json) · [씬 검사](../../../output/WorldBuilder/P09/T23-missing-scripts.txt) · [Editor 청크 대조](../../../output/WorldBuilder/P09/T21-editor.txt).


## 발행 복구

후속 작업에서 미저장 씬이 없는 Edit Mode를 확인하고 같은 프로젝트를 정상 재개방했다. PID 11752가 종료되고 PID 30664의 Editor가 연결됐다. 재시작 전후에 Recovery의 Published를 읽고 current와 previous에 각각 `ReadRevision(..., reopen: true)`를 실행했다. 두 revision과 catalog GUID, Manifest와 meta의 SHA-256이 일치했고 동일한 P08Authoring Edit Mode로 복귀했다. [실제 재시작 대조](../../../output/WorldBuilder/P09/closure/T20-process-restart.json).

P09 전용 64m 복구 씬에서 Bake 취소와 미완료 발행 거절을 확인했다. 발행 포인터를 교체한 직후 IOException을 주입했고 이전 포인터의 바이트와 meta GUID가 복원됐다. 실제 A→B 발행과 A Rollback도 통과했다. 첫 두 실행은 128m fixture의 고정 POI 경로 누락과 경계에 걸친 링크 끝점에 대해 검증 단계에서 멈췄다. 최종 실행은 소유 범위가 분명한 단일 타일을 사용했다. [복구 씬](../../../Assets/WorldBuilder/Worlds/a909a909a909a909a909a909a909a90e/Recovery.unity) · [발행 포인터](../../../Assets/WorldBuilder/Worlds/a909a909a909a909a909a909a909a90e/Published/Manifest.json) · [최종 실행](../../../output/WorldBuilder/P09/T20.log) · [첫 거절](../../../output/WorldBuilder/P09/T20-attempt1.log) · [경계 거절](../../../output/WorldBuilder/P09/T20-attempt2.log).


수계 경로 이동과 건물 계획 제거에 각각 Undo/Redo 20회 검사 범위를 추가했다. 최신 관련 2건은 121.193초에 모두 통과했고, 실패와 건너뜀은 0건이었다. [T10 최신 Test Runner 원본](../../../output/WorldBuilder/P09/T10-20cycles.json).

## 원본 보존

시작 비교 대상 99,588개 중 99,587개의 SHA-256이 일치했다. 기존 월드·RAW·SO·NavMesh·P08의 두 Published revision과 Addressables 키 및 그룹 파일을 확인했다. 기존 기본 설정 한 파일의 SHA 차이는 최초 EditMode 실행 중 관측됐고 원인은 미확인이다. 현재 기본 설정에서 P08 그룹 항목 한 줄을 제외한 내용은 P08 작업 시작의 설정과 일치한다. Player의 임시 Frame Timing 플래그만 복원했고 빌드 등록 씬·GraphicsSettings·QualitySettings의 지문은 일치했다. [99,588개 보존 대조](../../../output/WorldBuilder/P09/preservation-final.json) · [시작 변경 목록](../../../output/WorldBuilder/P09/status-before.txt) · [원본 SHA-256](../../../output/WorldBuilder/P09/source-before.csv).


### Editor에서 워밍업 후 P08 50회 재측정

P09의 [Editor 측정 씬](../../../Assets/10.Datas/WorldBuilder/P09Validation/P08Baseline/P08Baseline.unity)에서 같은 P08 Published 경로를 5회 워밍업한 뒤 50회 왕복했다. PC URP와 Editor Game View 2560×1440에서 얻은 진단이다. 워밍업 후와 왕복 후 GC에서 씬 10, 소유 씬 9, 카탈로그 1, pin 0, Unity 객체 16,010개가 일치했다. 활성 지형·catalog NavMesh·proxy의 메모리 합계는 10,682,622B였다.

Unity 전체 할당은 1,633,311,137B에서 1,505,733,859B로 감소했다. Mono 사용량은 1,185,034,240B에서 1,190,019,072B로 4,984,832B 증가했다. 월드 종료·회수 뒤 Unity 1,464,684,043B, Mono 1,185,648,640B, 씬 1, 소유 씬·카탈로그·pin과 소유 지형·NavMesh·proxy 0이었다. 기존 EditMode 재실행과 standalone에서는 전체 할당 증가가 관측됐다. 이 세 결과를 함께 보아도 managed 객체의 총수나 잔여 증가분의 소유는 확정되지 않는다. [Editor 50회 상태 원본](../../../output/WorldBuilder/P09/editor-p08/runtime-16.log) · [Windows Editor 프로세스 표본](../../../output/WorldBuilder/P09/editor-p08/Editor-process.csv).

## 4km Dirty 저작 재검증

열린 Windows Unity 6000.3.21f1 Editor에서 발행 월드와 원본을 읽기 전용으로 두고 64개의 저작용 TerrainData를 임시 에셋으로 복제했다. 4097² 높이와 4096² Hole 셀, `(240,240)`의 32×32 AddDelta, 0.2 워밍업, 0.3·0.4·0.5의 Dirty/Full 각 3회로 재현했다. 이전 기록 28.17초에 이어 같은 복제 절차로 잰 변경 전후 Dirty 중앙값은 **28.53초 → 2.63초**로 90.8% 감소했다. Full 중앙값은 33.31초 → 11.80초였다. Dirty와 Full의 실제 Terrain SHA-256은 세 번 모두 일치했고, 각각 변경 전의 해시와도 같았다. [변경 전 원본](../../../output/WorldBuilder/P09/dirty-before.csv) · [변경 후 원본](../../../output/WorldBuilder/P09/dirty-after.csv).

| Dirty 구간 중앙값 | 변경 전 | 변경 후 |
| --- | ---: | ---: |
| 검증용 전역 Terrain 읽기 | 669.13ms | 585.05ms |
| 적용 후 전역 Terrain 읽기 | 743.05ms | 885.11ms |
| 검증용 스냅샷 인코딩·SHA-256 | 7,685.45ms | 115.75ms |
| 적용 후 스냅샷 인코딩·SHA-256 | 7,358.43ms | 88.21ms |
| 적용 스냅샷 저장 | 2,935.33ms | 50.40ms |
| RAW 검증·로드 | 3,839.66ms + 3,837.54ms | 127.28ms + 0.08ms |
| 네이티브 Terrain 적용 | 2.17ms | 1.41ms |
| 네이티브 Terrain 에셋 저장 | 173.70ms | 160.46ms |

변경 전 적용 스냅샷 저장 구간에는 같은 데이터의 두 번째 인코딩이 포함된다. 정규 little-endian 레이아웃을 유지하면서 높이 배열을 일괄 복사하고 적용 스냅샷 바이트를 SHA-256과 캐시 저장에 재사용했다. Windows Editor의 1MiB 이상 입력은 Windows CNG로 해시하고, 그 외 입력에는 .NET SHA-256을 사용한다. 같은 저작 명령에서 검증한 RAW 필드를 재사용했다. 이후 Dirty의 가장 큰 비용은 적용 전후 전역 Terrain 읽기다. Full은 Terrain 에셋 저장이 약 5.67초를 차지한다. [구간 원본 변경 전](../../../output/WorldBuilder/P09/dirty-phases-before.csv) · [구간 원본 변경 후](../../../output/WorldBuilder/P09/dirty-phases-after.csv) · [BCryptHash](https://learn.microsoft.com/en-us/windows/win32/api/bcrypt/nf-bcrypt-bcrypthash).

재실행은 열린 Editor의 Edit Mode에서 `WorldBuilderP09Validation.ProfileDirty4Km("고유-로그-이름")`을 호출한다. 임시 타일·씬·캐시는 작업 종료 시 정리된다. Unity CPU Profiler의 `WorldBuilder.Authoring.*` 마커에서 읽기, 평가, 해시, 적용과 저장을 비교할 수 있다. [재현 도구](../../../Assets/Tests/WorldBuilder/P09/Editor/WorldBuilderP09Validation.cs) · [계측 위치](../../../Assets/02.Scripts/WorldBuilder/Editor/WorldTerrainAuthoringSession.cs).

## 생산 스트리밍 재검증

Title Play Mode에서 실제 `SceneManager`로 P08Bootstrap에 진입했다. 존재하지 않는 도착 ID와 시작 청크 초기화 실패를 각각 주입했다. 두 경우 모두 실패 이벤트를 내고 Title로 복구됐다. 이후 정상 진입과 씬 내 이동이 성공했다. 기존 슬롯과 분리한 작업 폴더에 `SaveGameDetailed`로 슬롯을 쓰고 Title에서 `LoadGameToScene`을 실행했다. XZ `(640, 278)`의 저장 위치와 안정 ID로 추적한 오브젝트의 소비 상태가 복원됐다. [실행 원본](../../../output/WorldBuilder/P09/followup/streaming.log).

발행 씬이 준비될 때 서쪽 청크에 NavMeshAgent를 추가했다. 생산 `WorldBuilderRuntimeBridge`의 갱신을 통해 교량 링크 양끝 청크에 pin이 걸렸고 양쪽이 Ready였다. 링크 완료 뒤 pin이 0으로 돌아왔다. 다음 통과에서는 스트리머의 플레이어 추적을 진단용으로 끊어 두 청크가 pin으로 유지되는 것을 확인했다. Agent를 비활성화하자 pin이 0이 되고 양끝 청크가 모두 회수됐다. 재실행은 Title의 GameManager가 Ready인 상태에서 Editor MCP의 reflection으로 `UPlayGround.Editor.P09StreamingFollowup.Start`를 호출한다. 실행 종료까지 저장 위치는 고유한 검증 폴더를 사용한다. [재현 코드](../../../Assets/Tests/WorldBuilder/P09/Editor/P09StreamingFollowup.cs) · [실행 원본](../../../output/WorldBuilder/P09/followup/streaming.log).

## P08 50회 메모리 귀속과 GPU 후속 측정

P08 전용 Editor 씬에서 5회 워밍업 뒤 50회 왕복하고, 미사용 에셋 해제와 GC가 끝난 각 시점에 Unity 메모리 스냅샷을 수집했다. 워밍업 후와 왕복 후 씬 10개, 소유 씬 9개, catalog 1개, pin 0개, Unity 객체 16,071개와 소유 Terrain·NavMesh·proxy 10,682,622B가 일치했다. Unity 전체 할당은 1,634,567,367B에서 1,508,461,695B로 126,105,672B 감소했다. Mono 사용량은 1,339,392B 증가했다. 종료 후 활성 씬은 검증 씬 한 개만 남았고 소유 핸들과 지형·NavMesh·proxy, pin은 0이었다. [50회 원본](../../../output/WorldBuilder/P09/followup/memory/runtime-16.log) · [Unity 스냅샷 API](https://docs.unity.com/en-us/engine/6000.0/script-reference/unity/profiling/memory/memoryprofiler/takesnapshot).

스냅샷의 네이티브 루트별 양의 증가분은 Editor `GizmoManager` +229,376B와 `PhysX` +49,748B였다. `Objects` 합계는 8B 감소했고 `AssetChangedNotifier`는 1,572,864B 감소했다. 종료 상태에서 PhysX는 워밍업 대비 9,581,182B 감소했다. GC 핸들은 18,742개에서 18,735개로 감소했다. managed 객체의 전체 수와 참조 경로, 새 Player 측정의 잔여 증가분은 귀속 미확인이다. 두 Editor 비교 스냅샷을 로컬 출력에 보관했다. [네이티브 소유별 차이](../../../output/WorldBuilder/P09/followup/memory/native-owner-delta.csv) · [워밍업 스냅샷](../../../output/WorldBuilder/P09/followup/memory/warm.snap) · [왕복 후 스냅샷](../../../output/WorldBuilder/P09/followup/memory/collected.snap).

기존 Development Player를 격리 복제해 D3D11 1280×720으로 실행했다. P08에서 워밍업과 왕복 후 배치된 Unity 객체 6,216개와 소유 Terrain·NavMesh·proxy 10,599,426B가 같았고 Unity 할당은 2,800,673B 증가했다. 2km 2,034개, 4km 2,796개, P08 4,431개의 프레임 기록에서 `gpuFrameTime > 0`인 표본은 각각 0개였다. NVIDIA PresentMon은 CSV 없이 종료했고, Windows Performance Recorder의 GPU ETW 세션 요청은 Windows 권한 정책 오류 `0xc5585011`로 막혔다. GPU p95/p99는 미수행이다. [P08 Player 원본](../../../output/WorldBuilder/P09/followup/gpu-player/player-p08/runtime-16.log) · [GPU 추적 결과](../../../output/WorldBuilder/P09/followup/gpu-wpr-permission.txt).

### 보관 스냅샷의 관리 메모리 대조

두 v17 스냅샷의 GC 핸들과 정적 필드에서 시작해 인스턴스·배열 참조를 추적했다. 같은 주소는 한 번만 셌고 타입별로 객체 헤더와 본체의 크기를 합했다. 스택 섹션은 0개였다. 핸들 대상은 각각 512개가 해석되지 않았다. 따라서 도달 객체 수는 이 방법으로 확인한 범위이며, 모노 카운터 전체와의 바이트 대사는 남는다. [집계](../../../output/WorldBuilder/P09/followup/recheck/managed-summary.json) · [타입별 개수·크기·참조 경로](../../../output/WorldBuilder/P09/followup/recheck/managed-delta.json) · [스냅샷 추출](../../../output/WorldBuilder/P09/followup/recheck/ExportManagedHeap.cs.txt) · [객체 추적 코드](../../../output/WorldBuilder/P09/followup/recheck/crawl-managed-snapshot.mjs).

| 항목 | 워밍업 | 50회 왕복 후 GC | 차이 |
| --- | ---: | ---: | ---: |
| Mono 사용량 로그 | 1,198,628,864B | 1,199,968,256B | +1,339,392B |
| GC 섹션 예약 크기 | 4,587,913,216B | 4,587,913,216B | 0B |
| Mono VM 섹션 예약 크기 | 185,286,696B | 189,782,728B | +4,496,032B |
| 도달 객체 수 | 772,599 | 762,184 | −10,415 |
| 도달 객체 크기 | 97,275,487B | 96,491,581B | −783,906B |
| WorldBuilder 어셈블리에서 정의한 타입의 객체 수·크기 | 173개 · 16,424B | 173개 · 16,424B | 0개 · 0B |

가장 크게 늘어난 타입은 검색 필터의 Dictionary 엔트리 배열 37개(+3,848B)다. `ProjectSearchEngine.s_QueryEngine → m_Impl → m_FilterOperators → handlers → Dictionary._entries`로 이어졌다. 해당 Dictionary 자체는 37개(+2,960B) 늘었다. `PostProcessManager.s_Instance → m_BaseSettings → parameters → List._items`의 `ParameterOverride[]`도 12개(+1,664B) 늘었다. 반면 `System.String`은 5,945개(−462,982B) 줄었다. 청크 스트리머와 P09 측정 컴포넌트는 각각 1개였고 Unity 네이티브 객체의 GC 핸들에서 도달했다. 증가가 확인된 큰 영역은 Mono VM 섹션이다. VM 예약 크기와 Mono 사용량은 서로 다른 값이므로 +1,339,392B 전부를 VM 객체나 청크 잔류에 배정할 수 없다. [VM·GC 구분 구현](https://raw.githubusercontent.com/needle-mirror/com.unity.memoryprofiler/master/Editor/MemorySnapshot/Cached/ManagedMemorySectionEntriesCache.cs).

같은 스냅샷의 NativeAllocations와 NativeRootReferences도 대조했다. 네이티브 할당 행은 1,068,562→1,051,522개, size 합계는 −34,818,056B이며 native object는 16,072개로 같고 크기는 8B 감소했다. 큰 네이티브 감소분은 System/ExecutableAndDlls 할당 행의 −15,985개, −33,109,556B에서 관측됐다. Managers/Mono VM은 4개·219,672B, Managers/MonoDomain은 6,415개·2,526,607B로 워밍업과 회수 뒤 동일했다. 따라서 +4,496,032B의 VM 섹션 증가는 이 두 네이티브 루트 할당의 증감과 구별한다. NativeAllocationSites가 두 스냅샷 모두 0개여서 호출 스택은 확인할 수 없다. 도달 관리 객체는 −783,906B, GC 예약 크기 차이는 0B다. 로그의 Mono Used +1,339,392B는 이 표만으로 배정할 수 없으며 Player의 페어 스냅샷도 대기 상태다. [네이티브 루트·타입 집계](../../../output/WorldBuilder/P09/followup/recheck/native-allocation-reconciliation.json) · [재현 코드](../../../output/WorldBuilder/P09/followup/recheck/ReconcileNativeAllocations.cs.txt).

기존 Player에서 P08 워밍업 대비 왕복 후 GC의 Mono 증가는 +299,008~794,624B, Unity 총할당 증가는 +2,800,673~3,240,773B였다. 조건별 결과는 아래와 같다. 각 실행에서 워밍업과 회수 뒤 청크 수 및 소유 자산 크기는 같았다. 해당 Player 소스의 단계별 스냅샷은 `UNITY_EDITOR` 가드 안에 있고 기존 Player의 Profiler 연결도 성립하지 않았다. 페어 스냅샷이 없어 Player 타입·참조 경로와 네이티브 잔여 할당의 귀속은 미확인이다. 기존 Player로 재현하려면 Profiler 연결을 복구해 같은 두 시점에서 스냅샷을 받는다. 연결 실패가 지속되면 해당 시점의 스냅샷을 기록하는 진단 Player가 필요하다. [스냅샷 가드](../../../Assets/Tests/WorldBuilder/P09/WorldBuilderP09Probe.cs).

| Player 계측 | 회수 후 Unity 총할당 차이 | 회수 후 Mono 차이 | 월드 종료 후 Mono 차이 |
| --- | ---: | ---: | ---: |
| [원본](../../../output/WorldBuilder/P09/win64-local-02/player-p08/runtime-16.log) | +3,005,525B | +360,448B | −180,224B |
| [ETW 시도](../../../output/WorldBuilder/P09/followup/gpu-player/player-p08/runtime-16.log) | +2,800,673B | +299,008B | −253,952B |
| [Profiler 파일](../../../output/WorldBuilder/P09/followup/recheck/standalone-profiler-p08.log) | +3,240,773B | +794,624B | +61,440B |
| [Profiler 연결 시도](../../../output/WorldBuilder/P09/followup/recheck/standalone-live-p08.log) | +2,837,078B | +311,296B | −253,952B |

### 4km Bake 메모리

별도 **메모리 경고 기준 초과** 항목이다. 보관 계측의 Bake는 198.67초였다. Editor Unity 할당 최대 8.03GiB는 시작 대비 +6.96GiB, Windows working set 관측 최대 13.69GiB는 호출 전 대비 +7.46GiB였다. v1 저작 작업의 512MiB 초기 경고 기준을 초과한다. 재현은 동일 8×8 입력을 복제한 저작 월드에서 저장된 64개 TerrainData로 Bake를 호출하고 스테이징 시작과 에셋 저장 사이에 Unity 할당 및 Windows working set 최대를 기록한다. 기존 발행본과 원본을 참조용으로 유지한다. [Bake 원본](../../../output/WorldBuilder/P09/authoring-8.log) · [Windows 표본](../../../output/WorldBuilder/P09/authoring-8-process.csv) · [Bake 라이프사이클](../../../Assets/02.Scripts/WorldBuilder/Editor/WorldBakeStage.cs).

후속 A/B는 같은 4km 격리 씬·Definition과 원본에서 복제한 NavigationLayout으로 각각 한 번 실행했다. 분할 시 재개방 검증을 마친 전체 씬을 그대로 사용하고 이동할 오브젝트가 들어갈 청크 계층을 먼저 비활성화했다. 직렬화 순회에 사용한 네이티브 래퍼도 사용이 끝날 때 해제했다. 각 Bake는 별도 새 revision으로 저장해 최종 확정 뒤 64개 청크를 다시 열었고 발행은 수행하지 않았다.

| 같은 Editor의 4km Bake 1회씩 | 변경 전 | 변경 후 |
| --- | ---: | ---: |
| Unity 할당 피크 | 8.78GiB | 5.10GiB |
| 각 시작 시점 대비 Unity 할당 증가 | +7.27GiB | +3.79GiB |
| Windows working set 관측 최대 | 15.37GiB | 12.12GiB |
| Bake 호출부터 validated까지 | 166.64초 | 153.81초 |

64개 청크의 타일 경계, 의존성, 인스턴스 ID, proxy 정점·인덱스와 Navigation surface 지문·경계 링크 111개·필수 경로가 일치했다. 두 실행의 시작 할당이 달라 Unity 차이는 각 시작값을 함께 기록했다. Windows 값은 750ms 간격 표본의 절대 최대다. 한 쌍의 Editor 계측이며 512MiB 경고 기준은 여전히 초과한다. [비교 집계](../../../output/WorldBuilder/P09/bake-memory/comparison.json) · [산출물 대조](../../../output/WorldBuilder/P09/bake-memory/artifact-comparison.json) · [변경 전 단계별 기록](../../../output/WorldBuilder/P09/bake-memory/baseline.tsv) · [변경 후 단계별 기록](../../../output/WorldBuilder/P09/bake-memory/optimized.tsv) · [재현 도구](../../../output/WorldBuilder/P09/bake-memory/Measure4Km.cs.txt).

추가 실험에서는 같은 4km 격리 입력에서 최종 저장하는 청크를 하나씩 즉시 닫았다. 기존 최적화에서 Unity 할당 피크는 5.10GiB였고 이번에는 5.16GiB였다. 이번 Windows working set 최대는 12.55GiB, 시작 직전 대비 증가는 3.97GiB였다. 청크 저장 단계의 Unity 할당은 약 1.51GiB 증가해 기존과 같은 폭이었다. Bake는 218.85초였고 64개 청크를 다시 열었다. 보관 발행본과 타일 경계·의존성·ID·proxy 정점과 인덱스, Navigation surface 경계, 경계 링크 111개와 필수 경로 1개가 일치했다. 이 실험은 최종 저장 시 열린 청크의 수명을 줄이는 방식으로 메모리 절감을 입증하지 못해 코드를 되돌렸다. 실험 revision과 임시 Layout을 제거했고 U03 4km 씬·Definition, Addressables 설정, P08 발행 포인터의 지문은 일치했다. 512MiB 경고 기준 초과는 계속 남는다. [실험 집계](../../../output/WorldBuilder/P09/bake-memory/early-close-comparison.json) · [단계별 할당](../../../output/WorldBuilder/P09/bake-memory/early-close.tsv) · [Windows 표본](../../../output/WorldBuilder/P09/bake-memory/early-close-process.csv).

기존 세 실행을 체크포인트 시각으로 다시 대조했다. 최적화 뒤 Unity 할당은 Terrain 복제 구간에서 +1.328GiB, Navigation 구간에서 +1.168GiB, 첫 청크 저장 직전부터 마지막 청크 저장 직전까지 +1.514GiB였다. 즉시 닫기에서도 각각 +1.306GiB, +1.171GiB, +1.513GiB였다. 최적화 뒤 Windows working set 표본 최대 12.12GiB는 저장 씬 재개방부터 첫 청크 최종 저장 직전 사이에 관측됐다. 최적화 전에는 같은 구간의 Unity 할당이 +3.835GiB였고 최적화 뒤에는 -0.244GiB였다. 위 수치는 체크포인트 사이의 전체 할당 변화다. TerrainData, NavMeshData, 저장 버퍼 각각의 소유 바이트는 현재 로그에서 판별할 수 없다. 추가 세부 계측은 열린 Editor에서 4km 임시 Bake revision과 대량 에셋을 생성하므로 자동 승인 검토에서 거절됐다. [단계별 Unity·Windows 대조](../../../output/WorldBuilder/P09/bake-memory/stage-allocation-deltas.json) · [수용 명세의 메모리 경고 기준](../../전달용/OpenWorld_HeightMap_Design_Regional_v1_1_Handoff/IMPLEMENTATION_SPEC.md).

정상 컴파일 뒤 기존 T17·T18을 선택 실행했다. 첫 작업은 예기치 않은 어셈블리 리로드로 테스트 결과를 얻지 못했다. 같은 Editor에서 재시도해 기존 Published의 교량·포털 링크, 청크 실패·재시도·철회와 50회 왕복을 포함한 2/2가 통과했다. [Test Runner 원본](../../../output/WorldBuilder/P09/bake-memory/T17-T18.json).

A/B에서 만든 두 revision과 임시 NavigationLayout을 정리했다. U03 복제 씬은 보존했고 Editor를 P08Authoring Edit Mode로 복귀시켰다. Addressables 설정 지문은 기존 검증 기록과 일치했다. [정리 결과](../../../output/WorldBuilder/P09/bake-memory/cleanup.json).

### GPU 계측 경로

기존 Development Player를 복제해 D3D11, 1280×720, PC URP에서 `-force-gfx-direct -profiler-enable -profiler-log-file <capture.raw>`로 실행했다. FrameTiming은 2km 2,010개, 4km 2,798개, P08 4,641개 모두 GPU 시간이 0이었다. Unity Profiler 파일에서 열 수 있던 마지막 2,000개 프레임도 `frameGpuTimeMs` 유효값이 0개였다. 시작 시 Profiler 버퍼 상한 경고가 한 번 기록됐다. GPU 모듈을 먼저 켠 Editor에서도 별도 Player 실행을 검색했지만 135초 동안 대상이 발견되지 않았다. Windows ETW 권한 오류와 별도로 재현된다. 독립 Player의 p95/p99는 미수행이다. [FrameTiming 집계](../../../output/WorldBuilder/P09/followup/recheck/standalone-frame-timing-counts.json) · [Profiler 파일에서 읽은 GPU 프레임](../../../output/WorldBuilder/P09/followup/recheck/standalone-profiler-gpu.csv) · [실시간 연결 결과](../../../output/WorldBuilder/P09/followup/recheck/standalone-live-connection.txt).

복제본의 `boot.config`에서 Graphics Jobs를 끄고 D3D11 단일 스레드, RTX 3060 Ti 드라이버 32.0.15.8129, 1280×720, PC 품질과 PC URP로 재실행했다. Profiler 버퍼를 512MiB로 늘린 실행에서 상한 경고는 없었다. FrameTiming의 2km 2,039개, 4km 2,841개, P08 5,394개 중 GPU 양수 표본은 0개였다. 원본 실행 파일과 원본 설정은 보존했다. [복제본 실행 플래그와 지문](../../../output/WorldBuilder/P09/followup/recheck/player-no-graphics-jobs-launch.json) · [2km CSV](../../../output/WorldBuilder/P09/followup/recheck/player-no-graphics-jobs-evidence/frames-16.csv) · [4km CSV](../../../output/WorldBuilder/P09/followup/recheck/player-no-graphics-jobs-evidence/frames-64.csv) · [P08 CSV](../../../output/WorldBuilder/P09/followup/recheck/player-no-graphics-jobs-evidence/player-p08/frames-16.csv) · [Unity GPU Usage 지원 범위](https://docs.unity3d.com/6000.0/Documentation/Manual/ProfilerGPU.html) · [Profiler 실행 플래그](https://docs.unity3d.com/6000.0/Documentation/Manual/profiler-command-line-arguments.html).

위 Profiler 파일을 현재 Editor에서 열어 보존 한도에 남은 프레임 2,000개를 읽었다. CPU 시간은 2,000개 모두 유효했고 GPU 양수 표본은 0개였다. 별도 실행은 같은 복제본과 그래픽 조건에서 Profiler 파일 출력을 빼고, 열린 Editor의 GPU 모듈을 켠 뒤 `127.0.0.1:55000`으로 직접 연결했다. 세 씬 실행 중 수집한 Profiler 프레임 9,325개에서 GPU 유효값은 0개였다. 해당 실행의 FrameTiming 2km 2,050개, 4km 2,836개, P08 4,386개에서도 모두 0개였다. 독립 Player GPU p95/p99는 미수행이다. [계측 집계와 환경 보존](../../../output/WorldBuilder/P09/followup/recheck/player-no-graphics-jobs-evidence/summary.json) · [직접 연결 프레임](../../../output/WorldBuilder/P09/followup/recheck/player-no-graphics-jobs-evidence/player-direct-profiler.csv).

같은 직접 연결로 P08의 `warm`과 `collected` 로그 직후에 Memory Profiler 스냅샷을 요청했다. 워밍업은 72,929,102B 전송이 끝났고 스냅샷에서 Player 네이티브 객체 6,228개를 확인했다. 50회 후에는 요청 뒤 Player가 월드를 닫고 종료했다. 파일은 처음 0B였다가 64,458,752B까지만 기록됐으며 완료 콜백 없이 잠긴 상태다. 증가분 비교에서 제외했다. 로그상 전후 네이티브 객체 6,228개, 핸들, pin 및 소유 Terrain·NavMesh·proxy 10,599,426B는 같았다. 같은 실행의 Mono 사용량은 618,496B, Unity 할당은 3,048,803B 증가했다. 워밍업의 참조 그래프와 네이티브 할당은 후속 페어 채취의 비교 기준으로 보관했다. [직접 연결 캡처 기록](../../../output/WorldBuilder/P09/followup/recheck/player-no-graphics-jobs-evidence/player-direct-snapshot-attempt.log) · [P08 Player 로그](../../../output/WorldBuilder/P09/followup/recheck/player-no-graphics-jobs-evidence/direct-run/player-p08/runtime-16.log) · [워밍업 관리 객체 및 참조 경로](../../../output/WorldBuilder/P09/followup/recheck/player-no-graphics-jobs-evidence/player-direct-warm.managed.json) · [중간 파일 정리 결과](../../../output/WorldBuilder/P09/followup/recheck/player-no-graphics-jobs-evidence/cleanup.json).

### 진단 Player 빌드와 P08 스냅샷

사용자 지시 뒤 열린 Unity 6000.3.21f1에서 `diag-01` Windows Development Player를 빌드했다. FrameTiming을 켜고 Windows Graphics Jobs를 껐다. 빌드는 17분 14초에 성공했고 오류 0건, 경고 91건이다. 기존 Addressables를 패키징했으며, 기존 카탈로그·번들 등 파일 105개가 모두 SHA-256까지 일치했다. 빌드 중 기록된 FrameTiming·Graphics Jobs 두 항목을 복원한 뒤 PlayerSettings, EditorBuildSettings, Addressables 설정의 지문을 대조했다. 빌드가 만든 임시 link.xml과 meta를 제거했다. [빌드 로그](../../../output/WorldBuilder/P09/diag-01/build-diagnostic.log) · [패키지 지문](../../../output/WorldBuilder/P09/diag-01/package-verification.json) · [설정 보존 결과](../../../output/WorldBuilder/P09/diag-01/settings-preservation.json).

RTX 3060 Ti 드라이버 32.0.15.8129, D3D11 단일 렌더 스레드, 1280×720, PC 품질·PC URP에서 2km·4km·P08 모두 청크 로드·활성화·해제를 마쳤다. 2km·4km의 실패 주입·철회와 Agent 횡단·pin 해제도 통과했다. P08은 워밍업·50회 왕복 후 GC·월드 종료 후 GC에서 각각 로컬 스냅샷을 저장했다. 워밍업과 50회 후의 객체 6,216개, 씬 핸들 9개·카탈로그 핸들 1개, pin 0개, 소유 Terrain·NavMesh·proxy 합계 10,599,426B가 같았다. 월드를 닫으면 소유 자산과 핸들이 모두 0이었다. 로그 및 스냅샷 저장은 완료됐고 프로세스가 종료 단계에서 멈춰 이 진단 프로세스만 종료했다. [P08 Player 로그](../../../output/WorldBuilder/P09/diag-01/memory/run/player-p08/runtime-16.log) · [세 스냅샷 지문과 카운터](../../../output/WorldBuilder/P09/diag-01/memory/summary.json).

| P08 진단 Player | 워밍업 → 50회 왕복 후 |
| --- | --- |
| Unity 할당 카운터 | +2,736,117B |
| 네이티브 스냅샷의 할당 크기 합계 | +2,676,840B. PhysX 루트에서 +587개, +2,647,920B |
| PhysX 루트 크기 | 10,594,734 → 13,242,654B. 월드 종료 후 3,609,752B |
| Mono Used 카운터 | +274,432B. 스냅샷의 네이티브 Mono VM·MonoDomain 루트 크기는 동일 |
| 도달 가능한 관리 객체 | +1,574개, +60,538B. 이 중 검증 프로브 `_events` 리스트의 씬 이름 문자열 +1,500개, +57,000B |

프로브의 문자열은 `GCHandle → WorldBuilderP09Probe → _events → _items → ValueTuple.Item2`로 연결됐다. `Unity.Properties`의 `PropertyBagStore` 정적 캐시에서 소규모 사전 증가도 확인했다. 네이티브 쪽의 PhysX 증가는 월드를 닫은 뒤 워밍업 값보다 작아졌다. PhysX 내부 하위 할당과 Mono 카운터 전체의 대응 관계는 현재 캡처에서 확인되지 않는다. 이 귀속은 진단 Player의 결과이며 보관한 기존 Player의 잔여 할당에 그대로 소급하지 않는다. [관리 객체 타입별 대조](../../../output/WorldBuilder/P09/diag-01/memory/managed-pair-delta.json) · [문자열 참조 경로](../../../output/WorldBuilder/P09/diag-01/memory/string-path-delta.json) · [네이티브 할당 루트](../../../output/WorldBuilder/P09/diag-01/memory/native-allocation-reconciliation.json).

### 진단 Player GPU 재측정

같은 `diag-01`, RTX 3060 Ti 드라이버 32.0.15.8129, i5-12400F, 1280×720, PC 품질·PC URP에서 Graphics Jobs를 끈 채 다음 경로를 완료했다. 표는 CSV 프레임 행 수이며 유효 기준은 유한한 양수 GPU 시간이다.

| 계측 | 2km | 4km | P08 | 유효 GPU |
| --- | ---: | ---: | ---: | ---: |
| D3D11 단일 렌더 스레드, 메모리 실행의 FrameTiming | 2,010 | 2,770 | 4,248 | 0 |
| D3D11 멀티스레드, 별도 FrameTiming | 2,009 | 2,770 | 4,458 | 0 |
| D3D12 멀티스레드, 별도 FrameTiming | 2,010 | 2,769 | 4,559 | 0 |
| D3D12 P08, Editor GPU Usage 직접 연결 | — | — | 3,705 | 0 |

D3D12 P08의 직접 연결에서는 CPU 시간 3,705개가 모두 유효했다. 독립 Player GPU p95/p99는 표본 부족으로 미수행이다. 기존 Windows ETW도 권한 오류 `0xc5585011`로 확인을 마치지 못했다. Editor 왕복 GPU p95/p99는 아래 Editor 계측 기록에 유지한다. [진단 Player GPU 집계](../../../output/WorldBuilder/P09/diag-01/gpu-summary.json) · [D3D12 직접 연결 CSV](../../../output/WorldBuilder/P09/diag-01/gpu-d3d12-profiler.csv) · [Unity FrameTiming API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/FrameTiming.html).

Profiler의 GPU Usage 모듈은 Unity 문서에 따라 시작 시 활성화하고 Windows D3D11 Editor Play에서 계측했다. 2km·4km의 Profile 씬이 참조하는 발행 키는 기존 Player 번들에 있고 현재 Editor 설정의 Addressables 그룹에는 없다. 2km 최초 시도에서 InvalidKeyException을 확인했다. 이후 실제 저장된 Bake revision의 catalog와 타일 키를 메모리 전용 ResourceLocationMap에 같은 AssetDatabaseProvider로 등록하고 씬을 로드했다. 프로젝트의 Addressables 파일을 저장할 필요 없이 기존 P09 Measure의 5회 워밍업, 정지 120프레임, 50회 왕복을 실행할 수 있다. 재현은 [대상 위치 등록](../../../output/WorldBuilder/P09/followup/recheck/RegisterEditorGpuLocations.cs.txt)과 [GPU 프레임 수집](../../../output/WorldBuilder/P09/followup/recheck/CollectEditorGpuFrames.cs.txt)을 순서대로 사용한다. [Unity GPU Usage 지원 범위](https://docs.unity3d.com/6000.0/Documentation/Manual/ProfilerGPU.html) · [frameGpuTimeMs](https://docs.unity3d.com/kr/6000.0/ScriptReference/Profiling.FrameDataView-frameGpuTimeMs.html).

환경은 Unity 6000.3.21f1 Editor Play, Windows D3D11, i5-12400F·RTX 3060 Ti, Game View 2560×1440, PC 품질·PC URP다. GPU Usage의 `frameGpuTimeMs > 0`인 유한값에서 오름차순 nearest-rank 백분위를 구했다. 각 실행의 왕복 1~50회가 기록됐고, 표의 전체 기록은 유효했다. Editor 렌더링·Profiler 비용이 포함되므로 독립 Player 값과 분리한다. [집계와 환경](../../../output/WorldBuilder/P09/followup/recheck/editor-gpu-summary.json).

| Editor Play 대상 | 정지 표본 / p95 / p99 | 왕복 표본 / p95 / p99 |
| --- | ---: | ---: |
| [2km · 16타일](../../../output/WorldBuilder/P09/followup/recheck/editor-gpu-locator-4/gpu.csv) | 120 / 13.11 / 13.98ms | 1,200 / 13.79 / 16.34ms |
| [4km · 64타일](../../../output/WorldBuilder/P09/followup/recheck/editor-gpu-locator-8/gpu.csv) | 120 / 13.47 / 14.21ms | 1,700 / 13.83 / 16.16ms |
| [P08 통합 월드](../../../output/WorldBuilder/P09/followup/recheck/editor-gpu-p08/gpu.csv) | 120 / 13.13 / 15.01ms | 1,202 / 12.66 / 15.89ms |

## 기존 게임 회귀 실패 분류

직전 P09 실행은 EditMode 29건과 PlayMode 2건에서 실패했다. 이전 통과 기준선과 직접 대조하지 않은 항목의 회귀 여부는 미확인이다. [EditMode 실패 원본](../../../output/WorldBuilder/P09/EditMode-failures.json) · [PlayMode 실패 원본](../../../output/WorldBuilder/P09/PlayMode-failures.json).

| 영역 | 실패 | 확인된 증상 |
| --- | ---: | --- |
| 전투·NPC 참조 | EditMode 8 | WoodlandWolf 반응 규칙, Sera 눈깜빡임 프로필, NPC 장비 테스트의 reflection 대상, 작업대 시선의 null 참조, 대검 AbilitySet 기대값 불일치 |
| 이동·월드 진행 | EditMode 11 | Living World 저장 위치 이관 3, 지역 등록·참조 2, 시나리오 씬 조회 2, 여행·포털 설정 4. 현재 씬 목록에는 `LakeVillageWorld.unity`가 있고 일부 테스트는 없는 `LakeOfLifeScenario.unity`를 연다 |
| 저장 형식 | EditMode 1 | 상인 재고 검사에서 세이브 버전 3.5를 기대하고 현재 저장 코드는 3.6을 기록 |
| 카타나 모션 | EditMode 4 | Bokusei 바인딩 경로 1, 기본타 보정각 50도와 기대 범위 90~100도 차이 3 |
| 입력·HUD·훈련장 | EditMode 5 | 게임패드 물리 경로 중복 1, 훈련장 상대·무기 참조 1, 필살기 HUD 참조와 상태 검사 3 |
| 전투 상태·공격 선택 | PlayMode 2 | Lian 간격 압박 공격 상태 전이 거절, 스태프 몬스터 Bolt 기대 시 Fan 선택 |

직전 MCP 작업은 완료 0, 총 테스트 수 미수신 상태로 약 178초 뒤 실패했다. 실제 Editor 로그에는 첫 시간 초과 전 IPrebuildSetup, 두 번째 시간 초과 전 WorldBuilder 테스트용 Terrain 임포트와 TestResults.xml 저장·IPostBuildCleanup이 있다. 두 번째 작업은 Unity가 실행을 마쳤는데도 MCP가 작업 ID에 테스트 시작·종료를 반영하지 못했다. 소스상 추적 작업 ID가 다르면 RunStarted를 무시하고, 총 건수 집계 중 예외가 나도 총 건수를 비운다. 로그에서 둘 중 어느 분기였는지는 확정되지 않았다. 30초와 120초 설정 모두 이 상태를 겪었으므로 대기 시간 연장만으로 복구됐다고 볼 근거는 없다. [직전 작업 원본](../../../output/WorldBuilder/P09/followup/WorldBuilder-EditMode-job.json) · [Editor 로그 발췌](../../../output/WorldBuilder/P09/followup/recheck/runner-timeout-context.log).

2026-11-20 열린 `UPlayground@fddd0702`에서 컴파일·임포트 완료와 Edit Mode 유휴 상태를 확인했다. `TerrainOperationCoreTests.SnapshotKeepsVersionOneBytesAndLargeSha256`는 1/1 통과했다. 제출부터 종료까지 약 8초, 러너 실행 시간은 0.65초였다. 이어 기존 실패를 메서드 이름으로 선택했다. EditMode는 41건 중 12건 통과, 29건 실패였다. 추가 12건은 선택 메서드의 다른 파라미터다. PlayMode는 2건이 실행돼 모두 실패했다. 기존 31건 전부 이름과 실패 메시지가 일치했다. 개별 판정은 모두 **재현**, 통과·미수행은 각 0건이다. P08 최종 통과 153건의 이름에는 이번 실패가 포함되지 않는다. 같은 Editor에서 테스트 코드와 설정 수정 없이 실행했다. 재현 시 EditMode 선택의 시작 대기 시간은 90초, PlayMode는 120초로 뒀다. [소규모 확인](../../../output/WorldBuilder/P09/followup/recheck/Smoke.json) · [EditMode 실행 원본](../../../output/WorldBuilder/P09/followup/recheck/EditMode.json) · [PlayMode 실행 원본](../../../output/WorldBuilder/P09/followup/recheck/PlayMode.json) · [31건 개별 판정과 스택](../../../output/WorldBuilder/P09/followup/recheck/regression-cases.json) · [P08 선택 기준](../../../output/WorldBuilder/P08/T23-final.json).

| 범위 | 현재 실패와 P09 관련성 |
| --- | --- |
| Combat 7, Content 대검 1, Movement 4, UI 5 | 반응·표정·장비·작업대, 대검 AbilitySet, 카타나 바인딩과 보정각, 입력·훈련장·HUD에서 기존 증상이 반복됐다. 해당 테스트와 참조 자산은 현재 월드 스트리밍 변경 범위 밖이며 실패 스택에 WorldBuilderRuntimeBridge나 WorldChunkStreamer가 없다. 기존 통과 기준선이 없어 최초 발생 시점은 미확인이다. |
| Content 상인 저장 버전 1 | 검사는 3.5를 요구한다. 현재 3.6 상수는 Git HEAD에도 있고 펫 성장·지원 스냅샷에 사용된다. P09의 저장 변경은 활성 브리지 상태 저장과 로드 라우팅이다. |
| Core 저장 이관 3 | LivingWorldMapMigrationTests에서 이관 도구의 Apply를 직접 호출할 때 실패한다. fixture는 GameSaveData 기본값 3.6을 사용하지만 이관 도구는 3.4만 허용한다. 실패 경로에 P09의 SaveManager 브리지 호출이 없다. |
| FlowGraph 8 | 네 건은 기존 LakeOfLifeScenario.unity 파일을 열다가 예외가 난다. 현재 등록된 호수 씬은 LakeVillageWorld.unity다. 나머지 네 건은 맵 등록·지역 참조, 포털 목록과 ForgottenWatchpost 공개 설정을 검사하다 실패한다. 이 단언들은 에셋과 등록 정보를 읽는다. 실제 이동은 씬 로더와 빌드 목록을 P09와 공유하므로 U04 검수를 기다린다. |
| PlayMode 2 | 격리 fixture에서 Lian의 Ability 시작은 StateTransitionRejected, 스태프 몬스터는 Bolt 대신 Fan으로 재현됐다. 스태프 테스트 이름의 P09는 전투용 식별자다. 두 스택은 Ability·몬스터 fixture에서 끝나며 발행 월드 로드가 없다. Lian의 누락 서비스 경고가 상태 거절을 일으켰는지는 미확인이다. |

후속 수정에서 구맵 저장 이관 fixture에 3.4 입력을 지정했다. 상인 재고 검사는 재고 컨테이너의 초기값을 확인하며 3.6 버전 검사는 기존 펫 저장 테스트에서 유지한다. NPC 장비 테스트에는 필수 CapsuleCollider, 작업 방향 테스트에는 비활성 fixture에 필요한 KCC 모터를 넣었다. 작업 방향 검사는 90도에서 실제로 목표 방향에 가까워지는지 본다. 저장 검사 40/40과 NPC·기존 SceneLoadOwnership 25/25가 열린 Editor에서 통과했다. 기존 실패 31건 중 9건을 이 검사에서 통과로 재판정했다. [저장 검사](../../../output/WorldBuilder/P09/closure/SaveEditMode.json) · [NPC·씬 로드 검사](../../../output/WorldBuilder/P09/closure/NpcAndSceneOwnershipEditMode.json).

도시 출구 테스트는 Build Settings에 등록된 LakeVillageWorld에서 실제 결말 조건과 표시를 확인하도록 맞췄다. 등록 씬 포털 검사에는 MapConfigDatabase의 기본 시작 씬도 포함해 논리 지역 ID와 씬 이름이 달라도 실제 포털을 조사한다. WorldTravelIntegrationTests는 5/6 통과했다. 직전 실패했던 3건이 통과했고, LakeOfLife 지도 포털의 ForgottenWatchpost 공개 상태 단언 1건이 재현됐다. [도시 이동 검사](../../../output/WorldBuilder/P09/closure/WorldTravelEditMode.json).

대검 시험 몬스터 검사에 11월 7일 등급 변형 이관(`4f0bfc34`)을 반영했다. Definition·Profile·EffectiveAbilitySet은 일반 등급 대검 세트를 확인한다. 프리팹 직렬화 값은 별도로 공용 대검 세트를 확인한다. `EnemyCombat.Init(ActorDefinitionSO)`는 초기화 때 정의의 EffectiveAbilitySet을 주입한다. 열린 Editor에서 CombatTestSessionTests 34/34가 통과했다. [시험장 검사](../../../output/WorldBuilder/P09/closure/CombatTestSessionEditMode.json) · [31건 최신 개별 결과](../../../output/WorldBuilder/P09/closure/latest-regression-cases.json).

PlayMode 선택 재검사에서 리안은 요청 직전 ReturnHome 상태였다. 기존 fixture는 프리팹 좌표 XZ (1019.17, 420.02)에서 전투 기준점을 잡은 뒤 Transform만 원점으로 옮겼다. 바닥을 갖춘 격리 위치에서 처음부터 스폰하고 KCC 위치와 전투 기준점을 일치시킨 뒤 공격 상태 진입과 Ability 실행 유지를 확인했다. 스태프는 BT의 Basic/Opener와 Skill/Punish를 요청하고 Beam의 실제 15레벨 해금을 검증한다. 생성된 14종의 타깃·투사체·취소 검사는 유지했다. 두 케이스 2/2가 통과했다. [리안 진입 상태 진단](../../../output/WorldBuilder/P09/closure/PlayMode-CombatBeforeAnchor.json) · [PlayMode 결과](../../../output/WorldBuilder/P09/closure/CombatPlayMode.json).

초기 카타나 검사에서는 현재 라온 전용 기본 3연격의 조준 보정 상한 50°와 Player 프리팹의 방향 입력 대상 선정 범위 90°가 어긋났다. 기본 공격은 실행할 모션이 정해진 뒤 첫 판정 전 기본 대상용 전신 회전 워프의 상한을 검색 각도에 적용한다. 방향 입력이 있을 때 기본 3연격의 검색은 50°이며, 락온·무입력 공격과 별도 스킬·대시 검색은 기존 경로를 사용한다. 카타나·대상 선정·워프 관련 EditMode 177/177로 기존 3건을 해소했다. U04는 락온을 끈 라온 카타나로 진행한다. 가까운 적을 입력 방향 기준 50° 안팎에 두고 기본 연계를 시도해 범위 밖 적으로 강제로 꺾이지 않는지 확인한다. 무입력과 락온의 타격 정렬도 확인한다. [수정 전 검사](../../../output/WorldBuilder/P09/closure/KatanaMotionWarpEditMode.json) · [후속 검사](../../../output/WorldBuilder/P09/closure/OpeningAimEditMode.json) · [조준 설계](../../../Assets/docs/Complete/MOTION_WARPING_IMPROVEMENT_DESIGN.md).

HUD 레일의 KeyPrompt 자식에는 PlayerAction/SkillUltimate와 기존 장치별 글리프 리소스가 연결돼 있었다. 레일의 비어 있던 _keyPrompt 참조를 이 자식에 다시 연결했다. 프리팹의 나머지 바이트와 meta를 보존했다. 열린 Editor에서 레일·스태미나·펫 지원·입력 안내 관련 EditMode 42/42 통과로 기존 HUD 실패 3건이 해소됐다. 실제 HUD에서 장치 전환에 따른 표시 여부는 U04 검수에서 확인한다. [프리팹 변경 대조](../../../output/WorldBuilder/P09/closure/HudPromptPrefabChange.json) · [HUD 검사 결과](../../../output/WorldBuilder/P09/closure/HudPromptEditMode.json).

호수·여울마을 회귀 검사는 논리 지역 ID와 씬 파일 이름을 분리했다. 기본 시작 씬 LakeVillageWorld의 Build Settings 등록과 상주 지역 ID, 여울마을 주소와 씬 GUID의 일치, Additive 지역 ID·콘텐츠 루트를 확인했다. 씬에 직접 직렬화된 NpcRoutine은 호수 8개·여울마을 2개이며, 이 수치는 런타임 배치까지 포함한 주민 집계와 별개다. 환경 컨트롤러는 상주 씬에만 1개다. 주민·러너 중복, 누락 스크립트와 씬 간 참조 검사도 유지했다. LivingWorldIntegrationTests 3/3 통과로 기존 실패 2건을 해소했다. [호수·여울마을 검사](../../../output/WorldBuilder/P09/closure/LivingWorldEditMode.json) · [31건 최신 개별 결과](../../../output/WorldBuilder/P09/closure/latest-regression-cases.json).

숲 늑대는 유효한 일반 등급 규칙이 있는 프로젝트 기본 피격 정책을 Profile에 연결했다. 열린 Editor에서 명시한 정책이 런타임 기본 정책과 동일함을 확인했다. 최초 생성 도구도 같은 정책을 연결하도록 컴파일했다. 이미 존재하는 늑대를 덮어쓰는 재생성은 수행하지 않았다. 세라는 Rifle_Full_Body.FBX의 BlendShape 임포트가 켜져 있고 프리팹이 쓰는 스킨 메시 22개 모두 채널이 0개다. 구매 팩 RifleGirl의 모델 파일 46개에도 BlendShape가 없다. 프리팹에서 선택된 Face_04는 정점 1,748개이고 같은 Full_Body·Parts FBX의 네 얼굴 변형은 각각 1,777개 또는 2,132개다. 정점이 대응하는 닫힌 눈 형상은 찾지 못했다. 구매 원본과 프리팹을 보존했다. 세라 얼굴의 눈꺼풀 모프를 저작한 뒤 연결하고 실제 대화 화면에서 확인해야 한다. [세라 얼굴 후보 조회](../../../output/WorldBuilder/P09/closure/SeraFaceSourceAudit.json). 피격 정책과 얼굴 연결 EditMode 합산 27/28로 세라 1건이 재현됐다. [정책과 메시 조회](../../../output/WorldBuilder/P09/closure/WolfReactionSeraAssets.json) · [선택 재검사](../../../output/WorldBuilder/P09/closure/WolfReactionSeraEditMode.json).

훈련 맵 두 개는 원본 씬 에셋에 있고 11월 17일 빌드 목록 정리(1674cd12)에서 공용 목록과 Development·Release 프로필에서 빠졌다. 현재 빌드 목록을 유지했다. 메뉴는 진입 가능한 씬이나 기존 훈련 세션이 있을 때 훈련장을 표시한다. 선택 가능 맵이 하나일 때는 해당 맵에 바로 포커스할 수 있고, 직접 실행한 훈련장의 기본 귀환은 현재 등록된 LakeVillageWorld다. 열린 Editor는 두 훈련 씬과 귀환 씬에 로드 가능을 반환했다. 상대·무기, 훈련 씬 에셋 두 개, 귀환 씬, 진입 가용성의 관련 EditMode 27/27로 기존 훈련장 실패 1건을 해소했다. 현행 빌드 프로필의 독립 Player 화면과 실입력 확인은 미수행이다. [훈련장 회귀](../../../output/WorldBuilder/P09/closure/TrainingGroundEditMode.json) · [Editor 가용성 조회](../../../output/WorldBuilder/P09/closure/TrainingGroundEditorAvailability.json).

패드 Start는 전역 Cancel을 통해 일시정지 메뉴를 열고 닫는다. HUD 메인 메뉴는 Select/View로 옮겼다. A는 단독 Jump에 쓰며, 왼쪽 스틱 클릭+A는 기존 중재기를 거치는 SuperJump다. 검증기는 정해진 조합의 트리거만 예외로 보고 단독 SuperJump A나 다른 액션의 Select 중복을 계속 거절한다. 저작 키맵과 생성 C#에서 Start와 Select의 연결을 확인했다. 입력 계약·조합 중재·리바인딩·입력 안내 EditMode 36/36이 통과했다. U04에서 이동 중 B가 대시하며 일시정지하지 않는지, Start가 일시정지를 한 번씩 열고 닫는지, Select/View가 메인 메뉴를 한 번씩 열고 닫는지 각각 확인한다. U01에서 A 단독 점프와 왼쪽 스틱 클릭+A를 별도로 확인한다. [입력 계약 검사](../../../output/WorldBuilder/P09/closure/InputContractEditMode.json) · [저작·생성 키맵 대조](../../../output/WorldBuilder/P09/closure/InputDefaultBindingCheck.json).

LakeOfLifeScenario.unity와 meta는 100MB 제한으로 82274a46에서 추적이 제외됐으며 테스트 시작 전 로컬에 없었다. 그 커밋 직전의 두 Git 객체를 원래 경로에 같은 바이트와 GUID로 복원했다. 선택 사건·섬 배치 검사와 두 영입 조우·저장 ID 검사는 두 건 모두 통과했다. 씬은 로컬 Git 무시 상태로 보관하고 Build Settings에는 등록하지 않았다. 이 결과는 보관 시나리오의 구조 검사이며 LakeVillageWorld 실제 진행은 U04에서 확인한다. 새 작업 복사본에서 씬과 meta가 모두 비어 있을 때만 `git restore --source=82274a46^ --worktree -- Assets/01.Scenes/Scenario/LakeOfLifeScenario.unity Assets/01.Scenes/Scenario/LakeOfLifeScenario.unity.meta`로 복원하고 두 검사를 선택한다. [복원 출처와 지문](../../../output/WorldBuilder/P09/closure/LegacyScenarioFixture.json) · [시나리오 검사 결과](../../../output/WorldBuilder/P09/closure/LegacyScenarioEditMode.json).

원본 LakeOfLife에서 ForgottenWatchpost를 가리키는 지도 행은 발견 ID가 비어 있다. 현재 지도는 발견 ID가 없는 아이콘을 만들지 않고 이동 요청도 거절한다. 검사는 지도와 같은 발견 ID 판정을 사용한다. 모든 대상의 표시 이름과 지도 설정을 검사하고, 발견형 지도 포털과 등록 플레이 씬 실물 포털에는 지역 목록 공개도 요구한다. 레거시 씬이나 지역 공개 값을 바꾸지 않았다. 현행 등록 씬 포털 검사와 두 도시의 결말 조건을 포함한 WorldTravelIntegrationTests 6/6이 통과했다. 기존 31건 최신 합산은 **통과 30, 재현 1, 미수행 0**이다. [지도 도착·도시 출구 검사](../../../output/WorldBuilder/P09/closure/WorldTravelDiscoverablePortalsEditMode.json) · [지도 표시·이동 게이트](../../../Assets/02.Scripts/UI/Scene/Map/UI_Scene_Map.Browse.cs).

개별 조치가 누적된 작업본을 단일 열린 Editor에서 다시 돌렸다. 최신 테스트 이름으로 EditMode 29건과 PlayMode 2건을 정확히 선택했다. EditMode 28/29와 PlayMode 2/2가 통과했고, 세라 얼굴 프로필 누락 1건만 재현됐다. 워크트리의 보관 시나리오 씬·meta는 앞서 기록한 Git 객체와 일치했다. 기존 슬롯 폴더 파일 5개의 SHA-256·크기·수정 시각이 실행 전후 같았으며 P08Authoring Edit Mode로 돌아왔다. [31건 통합 재실행 원본](../../../output/WorldBuilder/P09/closure/CombinedRegression.json) · [Editor·저장 슬롯 대조](../../../output/WorldBuilder/P09/closure/CombinedRegressionPreservation.json).

세라 실패의 기준선을 확인했다. 2026-10-17 얼굴 깜빡임 검사 도입 커밋 `179f8e76`에 이미 세라 케이스와 프로필 연결 단언이 들어 있다. 붉은나무 R03의 전체 EditMode 929/932에서 세라 실패와 스킨 메시 22개의 BlendShape 0개를 기록했다. R06 기록도 같은 `PlayerModel_Sera`의 프로필과 눈꺼풀 바인딩 누락을 재현했다. 현재 프리팹에는 `blinkShape: Eye_Blink`가 있지만 `profile`, `head`, `eyelids` 참조가 비어 있고, 현행 통합 실행은 같은 프로필 단언에서 실패한다. T23의 실패 집계는 유지하며 원인은 P09 이전부터 있던 얼굴 콘텐츠 누락으로 판정한다. 플레이어 모델에 정점이 대응하는 눈꺼풀 모프를 저작하고 대화 화면에서 확인해야 한다. [R03 기준선](../red-tree-blessing/LAKE_OF_LIFE_RED_TREE_R03_IMPLEMENTATION.md#13-타이틀-프리뷰와-회귀-후속-안정화) · [R06 재현 기록](../red-tree-blessing/LAKE_OF_LIFE_RED_TREE_R06_IMPLEMENTATION.md) · [현재 검사](../../../Assets/Tests/EditMode/Combat/DialogueFaceBindingTests.cs).

## 사용자 검수 대기

네 항목 모두 **사용자 테스트 대기**다. U01·U02는 Unity 6000.3.21f1, PC 품질·PC URP, Addressables의 **Use Asset Database**에서 [P08Bootstrap](../../../Assets/WorldBuilder/Worlds/a808a808a808a808a808a808a808a808/P08Bootstrap.unity)을 단독으로 열고 Play한다. XZ `(400,222)`에서 플레이어와 카메라가 준비되면 시작한다. 동선은 [Navigation Layout](../../../Assets/10.Datas/WorldBuilder/P08Validation/NavigationLayout.asset)을 따른다.

| ID | 시작 조건과 조작 | 실패 기준 |
| --- | --- | --- |
| P09-U01 | 키보드·마우스와 실물 패드로 각각 진행한다. 교량 `(495,250)↔(529,250)`과 서·동 동굴 입구 `(475,303)`, `(550,303)`를 양방향 통과한다. X=512 교량 경계에서 질주·대시 직후 반전하며 카메라를 좌우로 돌린다. 동굴 입구에서도 반전을 반복한다. | 낙하·관통·원점 이동, 청크 준비 이후에도 통과 불가, 입력 고착, 카메라 추적 해제나 스냅. 경계에서 잠깐 대기한 뒤 통과하는 동작과 구분한다. |
| P09-U02 | 교량 동쪽 512m 타일을 지나 X=1024 경계 바깥으로 이동한 뒤, 교량·동굴 쪽으로 돌아오며 원경을 화면에 둔다. 스트리머의 `Terrain Proxy tile_*`가 실청크 준비 뒤 꺼지는 시점과 화면을 함께 본다. 교량 강둑에서 카메라를 낮춰 Detail과 물을 확인하고 양쪽 동굴 입구에 다시 접근한다. | 지형이 비어 보이거나 접합부가 벌어짐, proxy·실청크 중첩으로 깜빡임, 근접 풀·물·폭포가 지속적으로 사라짐, 분홍 재질이나 검은 틈. |
| P09-U03 | Edit Mode에서 아래 격리 씬과 대응 Definition을 연다. **WorldBuilder 지형 저작**에서 4km 첫 32×32 AddDelta `(240,240)`를 끄고 켜서 Dirty 갱신과 Undo/Redo를 본다. 2km의 XZ `(512,512)` 접점에서 **Sculpt 시작** 후 한 타일 모서리를 짧게 브러시 편집한다. **Sculpt/미캡처 수정 캡처 → Undo → Redo → 명령에서 재생성**을 실행한다. | 다른 씬·타일에 반영, 명령 상태와 지형 불일치, 네 타일 높이 경계 분리, 기존 Hole 변경, 캡처 중복, Undo/Redo·재생성 이후 편집 유실. |
| P09-U04 | [Boot](../../../Assets/01.Scenes/GameLogic/Boot.unity) → [Title](../../../Assets/01.Scenes/GameLogic/Title.unity)에서 격리 슬롯으로 [LakeVillageWorld](../../../Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity)에 진입한다. 이동만 확인할 때는 현재 툴 런처의 **호수·여울마을 도보 연결**을 쓴다. 일반 적 하나에게 락온·공격·회피를 하고, 의뢰를 수락하거나 목표 한 단계를 완료한다. 남서쪽 길 `(991,449)↔(978,451)`을 거쳐 [DroughtVillageRegion](../../../Assets/01.Scenes/Scenario/OpenWorld/Regions/DroughtVillageRegion.unity)까지 왕복한다. 육지에서 저장한 뒤 Title에서 같은 검증 슬롯을 연다. | 이동·카메라·HUD 입력 고착, 대시가 Pause를 띄움, 퀘스트 진행 누락·중복 보상, 지역 이동 고착, 저장과 다른 위치·파티·의뢰 상태로 복원됨. |

- U03은 [2km 씬](../../../Assets/P09_UserReview/2km/Authoring.unity)과 [대응 Definition](../../../Assets/P09_UserReview/2km/Definition.asset), [4km 씬](../../../Assets/P09_UserReview/4km/Authoring.unity)과 [대응 Definition](../../../Assets/P09_UserReview/4km/Definition.asset)을 하나씩 연다. 복제한 TerrainData 16개·64개를 각 Terrain과 TerrainCollider에 연결하고 새 월드·씬 GUID로 초기화했다. Full 재생성에서 원본과 지형·명령 해시가 모두 일치했다. 검수 결과가 돌아올 때까지 복제본을 유지한다. [복제본 검사](../../../output/WorldBuilder/P09/closure/U03-fixtures.json) · [재현 도구](../../../output/WorldBuilder/P09/closure/PrepareU03Review.cs.txt).
- U04의 일반 검수는 **Boot Play → 새 게임 → 라온 선택 → LakeVillageWorld** 순서로 시작한다. 새 게임은 활성 저장 슬롯을 비운다. 지상에서 일시정지 메뉴를 열어 저장 목록의 **비어 있음** 행에만 저장한다. 현재 파일 기준 기존 슬롯은 화면 1·2이며 첫 빈 슬롯은 화면 3이다. 실제 화면에서 빈 표시를 다시 확인한다. 저장 직후 위치·파티·의뢰 단계를 기록하고 타이틀로 돌아간 뒤 **불러오기** 목록에서 같은 검수 슬롯을 직접 선택한다. 원본 경로와 분리된 저장 폴더가 필요한 경우 [P09StreamingFollowup의 경로 지정](../../../Assets/Tests/WorldBuilder/P09/Editor/P09StreamingFollowup.cs)을 사용한다. 기존 호수↔여울마을 동선과 표시 지점은 [연속 월드 검수 절차](OPEN_WORLD_USER_TEST_SPEC.md)를 따른다. [현재 슬롯 읽기 전용 확인](../../../output/WorldBuilder/P09/closure/U04-slot-readiness.json) · [새 게임 시작 처리](../../../Assets/02.Scripts/UI/Scene/UI_Scene_TitleMenu.cs) · [저장 슬롯 목록과 활성 슬롯 초기화](../../../Assets/02.Scripts/Manager/Save/SaveManager.cs).
- 결과는 `ID / 통과·실패·미수행 / 장치·씬·시작 위치·슬롯 / 실제 결과 / Console·영상`으로 기록한다. 화면과 입력 증상은 런타임 오류와 따로 적는다.

## v1 판정

**P09 v1 단계 완료 · 사용자 판정으로 P10 지역 저작 착수.** 2026-11-20 사용자가 아래 잔여 항목을 후속 검증으로 넘기고 P10으로 진행하기로 결정했다. 4km Dirty 중앙값 2.63초와 해시 3/3, 생산 AI 링크 pin 및 최초 진입 실패 복구, 격리 슬롯의 좌표·소비 상태 재진입은 통과했다. 진단 Player 빌드와 세 월드의 청크 로드·해제도 통과했다. 각 검사의 통과·실패·미수행·사용자 테스트 대기는 기존 결과대로 기록한다.

- **T23 검사 실패 1건.** 기존 실패 30건을 복구했다. 세라의 눈 깜빡임 모프 누락은 P09 이전 R03·R06에서도 재현된 얼굴 콘텐츠 결함이다. 기존 게임의 실제 씬 로드와 저장 동선은 U04에서 확인한다. [후속 실행별 판정](../../../output/WorldBuilder/P09/closure/latest-regression-cases.json) · [HUD 기존 상태](../hud-pet-effects-gauge.md)
- **T19 일부 귀속.** 진단 Player의 P08 워밍업·50회 후·월드 종료 스냅샷을 모두 확보했다. 네이티브 증가 중 PhysX 루트 +2,647,920B는 월드 종료 후 회수됐고, 도달 관리 객체 증가 중 57,000B를 검증 이벤트 문자열로 확인했다. Editor와 진단 Player의 Mono Used 카운터 전체 귀속, 기존 Player의 페어에 의한 소급 귀속은 남는다. [진단 스냅샷 분석](#진단-player-빌드와-p08-스냅샷)
- **T25 Player GPU 미수행, 4km Bake 경고 기준 초과.** 새 Player의 D3D11·D3D12 FrameTiming 27,603행과 D3D12 P08 직접 연결 3,705개에서 유효 GPU 값이 없었다. GPU p95/p99를 산출하려면 GPU 타임스탬프를 반환하는 환경이나 권한이 확보된 외부 계측이 필요하다. 4km Bake는 [별도 A/B 기록](#4km-bake-메모리)을 따른다. [진단 Player GPU 재측정](#진단-player-gpu-재측정)
- **P09-U01~U04 사용자 테스트 대기.** 실제 입력과 카메라 반전, Detail과 proxy 전환, 저작 UX, 기존 게임의 전투·퀘스트·저장 확인은 위 명세에 따른다.

사용자 지시 뒤 진단 Player를 빌드했고 보관 Addressables를 재사용했다. 현재 Editor는 P08Authoring Edit Mode다. 기존 저장 슬롯 파일 5개의 크기·수정 시각·SHA-256, 빌드 관련 설정과 Profiler 원래 상태를 확인했다. [현재 Editor와 저장 지문](../../../output/WorldBuilder/P09/diag-01/editor-save-preservation.json) · [빌드 설정 지문](../../../output/WorldBuilder/P09/diag-01/settings-preservation.json).

진단 빌드는 기존 바이너리 카탈로그의 2km·4km·P08 키 99개와 의존 번들 101개를 사용했다. `Addressables.LibraryPath`를 보관 출력 폴더로 지정하고 `DoNotBuildWithPlayer`에서 `StreamingAssets/aa`로 패키징했다. 세 월드의 청크 경로를 모두 실행했다. [키와 보관 콘텐츠 지문](../../../output/WorldBuilder/P09/followup/recheck/diagnostic-addressables-reuse.json) · [빌드 재현 코드](../../../output/WorldBuilder/P09/diag-01/BuildDiagnosticPlayer.cs.txt).

메모리를 재측정할 때는 Development Player에 `UPLAYGROUND_P09_MEMORY_OUTPUT`을 새 절대 경로로 지정하고 Editor Profiler 연결 없이 실행한다. [P08 검증 코드](../../../Assets/Tests/WorldBuilder/P09/WorldBuilderP09Probe.cs)는 `warm`, `collected`, `closed`에서 로컬 스냅샷의 완료를 기다린다. GPU 계측 실행에는 이 환경 변수를 비운다.

열린 Editor를 P08Authoring Edit Mode로 돌렸고 Play 시작 씬 지정 없음과 Profiler 원래 상태를 확인했다. 기존 저장 폴더 파일 5개의 수정 시각을 확인했으며 이번 검증 중 변경된 파일은 0개였다. Assets·ProjectSettings의 작업 변경 목록은 GPU 측정 전후 같았고 Addressables 설정 지문은 앞선 보존 기록과 일치했다. 기존 Player와 원본 스냅샷 두 개, 프레임 CSV 및 결과 로그를 보관하고 계측용 Player 복제본과 Profiler 중간 파일, 임시 힙 추출물을 정리했다. [Editor·저장 확인](../../../output/WorldBuilder/P09/followup/recheck/editor-restored.json) · [경로·설정 대조](../../../output/WorldBuilder/P09/followup/recheck/preservation-check.json) · [정리 결과](../../../output/WorldBuilder/P09/followup/recheck/cleanup.json).

