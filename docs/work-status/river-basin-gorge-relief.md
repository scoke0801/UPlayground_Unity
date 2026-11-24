# RiverBasin4km 평지 기복·B3 협곡 작업 상태

[설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_GORGE_RELIEF_DESIGN.md) · [월드와 아트 위키](../wiki/world-art.md) · [강 유역 제작 상태](river-basin-world.md)

## 호수 마을 접근로와 낮은 능선 · 2026-11-23

`GR-평지` 후속 제보는 “길이 너무 인위적이다”, “언덕들 예쁘지 않다. 인위적이다”이다. 전역 지도에서 길의 꺾임이 뚜렷했고, 표시 구간은 독립된 작은 단구들이 길 가까이에 반복되어 보였다. [수정 설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_GORGE_RELIEF_DESIGN.md#호수-마을-접근로와-낮은-능선)

- **구현 반영.** 기존 단구를 길 양편에 길게 뻗은 두 능선과 완만한 낮은 지형으로 바꿨다. Terrain 5장의 1m 높이 표본 171,332개를 WorldBuilder AddDelta 명령으로 기록했다. 새 곡선에 맞춰 기존 길 흔적을 낮추고 새 흙길을 폭이 달라지도록 도색했다. 이전 단구의 밝은 암반 도색을 완만한 사면에서 줄이고 반복 배치된 노두 65개를 비활성화했다. 남은 노두 15개와 목서 80개는 실제 지표에 맞췄다. [접근로 좌표](../../Tools/RiverBasinWorld/GorgeRelief/natural-corridor-b3.csv) · [지형 패치](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Authoring/Patches/160266a897dd4fcf9015a2c43f150402.bytes)
- **지도와 화면.** 4km 전역을 다시 촬영했다. B3 점선에는 현장과 같은 접근로 좌표를 사용했고, 기존 탐험로 점선은 모서리를 둥글게 하고 대비를 낮췄다. 지도 GUID·중심·4096² 규격을 유지했다. 최초 지도 덮어쓰기는 Windows 파일 잠금으로 실패했다. 별도 촬영본을 만든 뒤 기존 PNG 교체에 성공했고 Unity에서 연결을 확인했다. [지도 전후 비교](river-basin-gorge-relief-evidence/b3-natural-map-comparison.jpg) · [조감](river-basin-gorge-relief-evidence/b3-natural-overview-after.jpg) · [보행 높이](river-basin-gorge-relief-evidence/b3-natural-on-path-after.jpg)
- **Editor 검사 통과.** 길 중심을 약 4m 간격으로 검사한 최대 종경사는 약 6.1%다. Terrain 공유 경계 112개 최대 높이 차 0m, 접근로 3구간과 B3 횡단이 `PathComplete`였다. 씬은 저장 상태다. [검사 수치](river-basin-gorge-relief-evidence/b3-natural-checks.json)
- **사용자 테스트 대기 (`GR-평지`).** `RiverBasin4km` Editor Play에서 X/Z `(1656, 1555)` → `(1840, 1384)` → `(2046, 1214)` → `(2264, 1052)`를 양방향으로 걸으며 굽은 길이 이어져 보이는지와 굴곡에 가려진 다음 구간이 자연스럽게 드러나는지 확인한다. `M`으로 전체 지도를 열어 점선과 현장 길을 비교한다. 실제 조작과 HUD 확인, 전체 재생성 및 별도 빌드는 미수행이다.

## B3에서 호수 마을로 이어지는 사면 · 2026-11-23

`GR-평지` 체감 제보는 “현재 게임 지형이 너무 평평해”이며 대상은 River Basin 4KM이다. 열린 씬에서 제보 구도를 맞췄다. 화면 중앙 방향으로 지형을 네 곳 표본 추출한 결과, 약 300m 구간에서 표본은 Y 48.4~49.7m에 놓였다. 조감과 보행 높이 모두 짧은 단구가 개별 형태로 보이는 구도였다. [조감 적용 전](river-basin-gorge-relief-evidence/b3-terrace-aerial-before.jpg) · [보행 높이 적용 전](river-basin-gorge-relief-evidence/b3-terrace-trail-before.jpg)

- **구현 반영.** `Terrain_3_2`와 `Terrain_3_3`의 기존 단구 사이에 비대칭 사면을 만들었다. 1m 높이 표본 23,173개가 바뀌었고 최대 성토는 10.03m다. WorldBuilder Sculpt `AddDelta` 명령 `261d11b2fab643ca9603278c1ddeed40`을 저장했다. 기존 목서·노두 18개의 높이를 접지에 맞췄다. B3 보행 NavMesh를 같은 에셋에 다시 베이크했고, 4096² 미니맵에서 해당 사면에 걸친 20,931픽셀을 갱신했다. [설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_GORGE_RELIEF_DESIGN.md#b3에서-호수-마을로-이어지는-사면) · [저작 패치](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Authoring/Patches/ff79fbe1284d4a18acd198386c513ad0.bytes) · [미니맵](../../Assets/10.Datas/Map/RiverBasin4km/Minimap.png)
- **Editor 검사 통과.** 저장 높이와 Sculpt 패치의 차이는 최대 0.031mm, 패치 밖 표본 변화도 최대 0.031mm였다. 주 보행축 중심 변화는 0.008mm 미만, 타일 공유 경계 112곳의 최대 오차는 0m였다. 본선 두 구간, 사면 상부 진입, 현수교 횡단이 모두 `PathComplete`였다. 첫 MCP 응답은 시간 제한을 넘겼으며, 작업이 끝난 뒤 씬을 저장하고 별도 Editor 검사로 반영을 확인했다. [검사 수치](river-basin-gorge-relief-evidence/b3-terrace-checks.json) · [WorldBuilder 저작 입력 검사 통과](river-basin-gorge-relief-evidence/b3-terrace-worldbuilder.txt)
- **Unity 화면 비교.** 사면 아래에서는 원경의 일부가 가려지고, 마을 쪽에서 역방향으로 보면 낮은 길과 높은 사면이 함께 읽힌다. [조감 적용 후](river-basin-gorge-relief-evidence/b3-terrace-aerial-after.jpg) · [보행 높이 적용 후](river-basin-gorge-relief-evidence/b3-terrace-trail-after.jpg) · [사면 아래](river-basin-gorge-relief-evidence/b3-terrace-ridge-after.jpg) · [역방향](river-basin-gorge-relief-evidence/b3-terrace-return-after.jpg)
- **사용자 테스트 대기 (`GR-평지`).** `RiverBasin4km` Editor Play에서 기존 플레이어와 평소 입력 장치로 X/Z `(1691, 1512)` → `(1840, 1387)` → `(1930, 1327)`를 걷고 역방향으로 달린다. 길이 이어져 보이고 사면 너머의 시야가 이동하며 달라져야 한다. `(1840, 1387)`에서 `(1845, 1460)` 방향으로 사면에 올라가고 내려오면서 접지와 카메라를 확인한다. 길을 잃거나, 평평하게 보이거나, 걸리거나 카메라가 당겨지면 해당 좌표와 진행 방향을 기록한다. 전체 월드 재생성 실행과 Player·Addressables 빌드는 미수행이다.

## B3 현수교 밧줄 근접 마감 · 2026-11-23

교대 버팀줄을 3점 꺾임에서 연속 곡선으로 바꾸고, 근접 LOD의 버팀줄에 원주 24분할과 세 가닥 꼬임 단면을 적용했다. 난간·주 케이블 단면과 기둥 결속도 보강했다. 끝단은 목재 말뚝과 철제 고정부에 연결했다. 기존 EastLands 아틀라스에서 섬유 띠를 사용하도록 UV를 조정해 흰 패치가 반복되던 부분을 없앴다.

Blender 원본과 기존 GUID의 FBX를 갱신했다. Unity의 기존 프리팹에서 LOD 3단의 MeshRenderer 6개씩과 충돌체 38개를 확인했다. 전체 LOD 삼각형 수는 51,568 / 11,048 / 2,768이다. 같은 카메라에서 접사 전후를 확인했다. 수정본의 플레이 프레임 시간은 미측정이고 최종 외관은 사용자 확인 대기다.

[수정 전 접사](river-basin-gorge-relief-evidence/rope-before.jpg) · [수정 후 접사](river-basin-gorge-relief-evidence/rope-after.jpg)
## G0 Editor 실측 · 2026-11-23

- 열린 RiverBasin4km의 Terrain 64개에서 남서 지류 횡단면 12곳, B1·B3·B4 주변, lake-loop 10m 간격을 측정했다. [실측, 저작 입력과 적용 검증 수치](../../Tools/RiverBasinWorld/GorgeRelief/gorge-plan-b3.json)
- 호수 마을 → B3의 128m 블록 25개 중 8m 폭에 3m 이상 낙차가 있는 블록은 2개, 6m 이상은 0개였다. 편집 전 B3 하상 Y 32.36m, 25m 양안 Y 45~48m다. s=480의 ±40m 양안은 Y 약 32m로 설계 초안보다 낮아 협곡의 최대 깊이를 B3에 집중했다.
- 카메라 기본 거리 5m, 충돌 구체 반경 0.25m, 가림 탐침 반경 0.4m. 플레이어 KCC 안정 경사 60°, 단차 0.5m, 캡슐 반경 0.35m. Nav 에이전트 경사 45°, 단차 0.75m, 활성 NavMesh 삼각형 0개.
- 구현 요청에 따라 B3 파일럿, 현수교, 백색 화강암 복제 재질을 채택했다.

## G1 회색 블록아웃 · 제작 기준

- [RiverBasin4km 씬](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/RiverBasin4km.unity)의 `B3 협곡 · 회색 블록아웃`에 5 / 5 / 5 / 6.5m 낙차, B3 임시 박스 다리, 바닥 길과 출구 E1·E2를 반영했다. 다리 폭 2.4m, 경간 46m, 데크 Y 48m다. 다리 루트는 기존 `WB_P04_Bridge_` 카메라 Solid 규칙을 사용한다.
- Terrain 4개 타일에 반영하고 지형 명령 스택에 AddDelta 1개를 기록했다. [WorldDefinition](../../Assets/10.Datas/WorldBuilder/6effd90b68ea4b3d8f8caa5197fcfe34_Authoring.asset) · [패치](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Authoring/Patches/22eaed3785c34524a3722f960af1083e.bytes)
- 기존 수면 3개는 원본을 보존하고 남서 지류를 잘라낸 파생 메시를 씬에 연결했다. 블록아웃 수면 14개와 원본 참조 기록의 에셋·meta를 보존했다. [수면·복구 기록](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Authoring/GorgeReliefG1/)
- 실제 저장 반영을 확인했다. MCP 적용 응답은 120초 제한을 넘겼으며 Unity가 완료한 뒤 씬, 명령 스택 15→16개, 수면 참조, Editor에서 실행한 아래 검사를 확인했다.

| 자동 확인 | 결과 |
|---|---|
| Terrain 타일 경계 | 112개 최대 높이 차 0m |
| B1 및 B4 보호 | B1 60m 반경 11,289 표본, B4 교대 쪽 1,812 표본의 G0 RAW 대비 차이 4mm 미만. G0 RAW 양자화 범위 안 |
| 다리와 웅덩이 | 데크 중심 충돌 45/45, 데크와 접속면 높이 차 최대 0.31m, B3 웅덩이 중심 수심 3.59m |
| 수면 등록 | 블록아웃 수면 구간 10개 등록, 수역 좌표 8곳에서 예정 수위 반환. B4 Y 21.76m |
| 바닥 길과 출구 | 바닥 길 중심 332 표본의 최대 종단 경사 18.4°, E1 중심 7.9°, E2 중심 9.3° |
| 접근 | 다리 축 접속로 최대 국부 경사 17.6° / 11.5°. 최종 교량 설계 목표 8~12%는 G2에서 맞춘다 |

[조감](river-basin-gorge-relief-evidence/g1-overview.jpg) · [마을 쪽 진입](river-basin-gorge-relief-evidence/g1-village-approach.jpg) · [B1 쪽 진입](river-basin-gorge-relief-evidence/g1-b1-approach.jpg) · [다리에서 하류](river-basin-gorge-relief-evidence/g1-bridge-downstream.jpg) · [E1 하구](river-basin-gorge-relief-evidence/g1-mouth-E1.jpg) · [E2 중류](river-basin-gorge-relief-evidence/g1-floor-E2.jpg)

블록아웃에서는 협곡의 단차와 출구 지형을 비교할 수 있다. 차폐·재노출 타이밍, 걸을 때 카메라가 당기는 구간, 수영 후 복귀는 사용자 TPS 확인 항목이다. G2에서는 최종 현수교 접속부, C1 상류의 약 0.55m 임시 역구배, 물 이음새, 낙수 표현·수영 링크·소리를 마감 대상으로 기록했다.

## G2 물과 현수교 · 반영

사용자 요청에 따라 G1 인수 대기에서 멈추지 않고 실제 플레이용 마감을 진행한다. Blender 현수교를 3개 LOD로 내보내고 씬에 배치했다. 데크 처짐 0.65m를 따라 걷는 면과 난간 충돌을 저작했다. C1 상류의 역구배를 걷어내고 39m 기존 지류에 연결했다. 4개 낙수에 수영 표면 링크, 물보라, 옅은 안개, 거리 감쇠 소리를 연결했다. G3·G4까지 저장했다.
## G3·G4 플레이용 마감 · 씬 반영

- 협곡에 기존 에셋을 활용한 암벽 69개, 암반과 자갈, Gold·Copper 목서 72그루, 물가 식생을 배치했다. 회백색 화강암에 복제 재질 5종을 썼다. 바닥 길과 E1·E2에 보행 흔적을 도색했고, 중류 E2 옆에 모닥불 흔적을 배치했다.
- 호수 마을→B3 접근로 양옆에 단구 7곳과 둔덕·분지 10곳을 조형하고 노두 80개와 목서 80그루를 배치했다. 교대 바깥을 기존 지형 높이와 연결했다. [평지 위치와 보행축](../../Tools/RiverBasinWorld/GorgeRelief/plains-b3.json)
- B3 주변 보행 NavMesh를 베이크하고 수영 구간을 지상 보행에서 제외했다. B3 양 교대 간 71m, E1 약 30.5m와 E2 약 36.7m 경로가 모두 `PathComplete`였다.
- 4096² 정북 미니맵에서 저작 범위를 다시 촬영했다. 지도 GUID와 외부 픽셀을 보존했다. [미니맵](../../Assets/10.Datas/Map/RiverBasin4km/Minimap.png)
- 최초 현수교의 LOD별 삼각형 수는 27,168 / 6,360 / 1,972다. Unity 현수교는 데크 0.65m 처짐, 보행 충돌 폭 2.33m, 난간, 양 교대 발판을 적용했다. [Blender 원본](../../Tools/RiverBasinWorld/GorgeRelief/B3_SuspensionBridge.blend) · [생성 에셋과 meta](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Authoring/GorgeReliefArt/)

| 저장본 검사 | 결과 |
|---|---|
| 호수 마을→B3 25블록 | 3m 이상 14개(56%), 6m 이상 9개(36%), 10m 이상 3개(12%) |
| 호수 마을~성채 60블록 | 3m 이상 26개(43.3%, 확장 목표 50%), 6m 이상 12개(20%) |
| 지형과 참조 | 112개 Terrain 경계 최대 차이 0m, 추가 계층 Missing Script 0, 폭포 링크 4/4 |
| 경로 원본 | 수작업 마을→B3 보행축과 B3 NavMesh 사용. 구조물 입구 소켓을 전제로 하는 P04의 `requiredBridgeGuid` 발행은 후속 작업 |

[현수교와 림](river-basin-gorge-relief-evidence/final-rim-vista.jpg) · [웅덩이와 C1](river-basin-gorge-relief-evidence/final-pool-upstream.jpg) · [바닥에서 보는 다리](river-basin-gorge-relief-evidence/final-crossing-from-floor.jpg) · [마을에서 B3로 가는 단구](river-basin-gorge-relief-evidence/final-village-relief.jpg) · [Editor 검사 원본](river-basin-gorge-relief-evidence/production-checks.json)
## Editor Play 검증 · 가상 게임패드 4/4

- 현수교 양방향 왕복을 통과했다. 수영 상태 진입 0회, 접지 964/966프레임.
- 첫 Play에서 B3 웅덩이의 오른쪽 둑에 걸렸다. 해당 둑을 자갈 여울로 완화한 뒤 같은 가상 입력으로 수영 진입과 마른 둑 복귀를 확인했다. E2는 수영에서 상륙해 중류 선반으로 올라갔고 E1은 마른 하구 출구를 끝까지 걸었다.
- 상륙 턱을 지형 명령 스택에 기록하고 B3 NavMesh를 다시 베이크했다. 최종 저장본의 활성 NavMesh 삼각형은 6,123개다. 미니맵 재촬영과 Play 종료 이후에도 씬·에셋 상태를 확인했다.
- 2560×1440 Editor 프레임 시간 p50 20.4ms, p95 32.7ms(2,017프레임). 이 측정은 실제 조작 검증과 함께 수집했으며, 1080p 전후 비교와 CPU·GPU 분리는 미수행이다.

[Play 원본 로그](river-basin-gorge-relief-evidence/B3_PlayMode.txt) · [B3 상륙 여울](river-basin-gorge-relief-evidence/final-pool-landing.jpg)
## 사용자 테스트

`RiverBasin4km`에서 기존 플레이어로 Play한다. 좌표는 X/Z 순서다.

| ID | 경로 | 확인할 것 |
|---|---|---|
| GR-접근 | 마을 쪽 약 (1645, 1592) → B3 (1622, 1620), B1 쪽 약 (1599, 1648)에서 역방향 | 가까워질 때 협곡이 드러나는지, 입구를 쉽게 찾는지 |
| GR-횡단 | 양쪽에서 걷고 달려 다리를 건넌다 | 교대에서 끊기거나 카메라가 벽·난간에 걸리는지 |
| GR-바닥 | B3 웅덩이에 입수한 뒤 하류 오른쪽 둑을 따른다 | E2 중류 약 (1464, 1471)과 E1 하구 약 (1209, 1190)에서 마른 지형으로 복귀할 수 있는지 |

최종 카메라·청감·미술 인수와 장거리 프레임 비교는 사용자 테스트 대기다. 별도 Player·Addressables 빌드와 G1 reset 실실행은 미수행이다.

## 재현과 백업

[준비 스크립트](../../Tools/RiverBasinWorld/GorgeRelief/prepare-b3.ps1)의 단계별 출력은 열린 Unity MCP `execute_code` 입력이다. G1 `reset`은 G1 직후의 자산 범위를 대상으로 한다. 마감 이후 재작업 기준은 단계별 씬·Terrain·미니맵 백업과 기록된 지형 명령이다. [촬영 스크립트](../../Tools/RiverBasinWorld/GorgeRelief/capture-b3.cs.txt)는 씬을 바꾸지 않고 검수 화면을 만든다. apply 직전 씬·Terrain·WorldDefinition 백업 약 14.2MB와 G0 재측정 RAW 약 33.6MB는 `output/RiverBasinWorld/GorgeRelief/`에 보존했다.
