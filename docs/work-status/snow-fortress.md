# 설산 성채 도시

[설산 성채 필드 설계서](../전달용/snow_design/snow_fortress_design.html) · [월드와 아트 위키](../wiki/world-art.md)

## 성곽 도시 재구성

사용자가 이전 블록아웃의 규모와 미술 품질을 불합격으로 판정했다. 새 버전은 설산 아래 사면 약 187 × 207m를 하나의 성곽으로 감싸고, 남문과 북동 내성 사이에 시가지 건물 56동을 배치했다. 남문 대로는 시장 광장과 서쪽 주거가, 동쪽 작업 구역을 지나 내성 문루로 오른다. 기존 폐성은 서쪽 골목의 유적으로 남겨 두었다.

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
