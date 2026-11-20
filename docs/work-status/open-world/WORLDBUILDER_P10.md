# WorldBuilder P10 지역형 아틀라스 통합

- 현재 판정은 P10-A~D 구현·Editor 검증 완료, T35 경관과 실제 조작 품질은 사용자 테스트 대기다.
- [지역형 설계](../../../Assets/docs/design/world-art/open-world/WORLDBUILDER_REGIONAL_HEIGHTMAP_DESIGN.md) · [WorldBuilder 전체 상태](WORLDBUILDER.md) · [월드 위키](../../wiki/world-art.md)
- P09는 사용자 결정으로 단계 완료했다. T19 일부 귀속, T23 기존 얼굴 모프 실패 1건, T25 Player GPU 미수행과 Bake 메모리 경고, U01~U04 사용자 테스트 대기는 [P09의 기존 판정](WORLDBUILDER_P09.md#v1-판정)을 유지한다.

## P10-A 자료 계약

**통과.** P01에서 만든 C 월드 GUID `35c2ca802bd64575961ac7cda330ac91`의 독립 프로필과 원본을 재사용했다. 적용 범위는 2,048m × 2,048m, 2049² 샘플, 간격 1m, Y 0~700m다. 별도 B 기술 fixture는 originY −80m, 범위 480m다. [C WorldProfile](../../../Assets/10.Datas/WorldBuilder/35c2ca802bd64575961ac7cda330ac91.asset) · [기존 C 저작 씬](../../../Assets/WorldBuilder/Worlds/35c2ca802bd64575961ac7cda330ac91/Preview/WorldPreview.unity)

| 검사 | 판정 | 근거 |
|---|---|---|
| T27 참조와 임포트 입력 | 통과 | 7개 지역 ID의 Texture2D를 실제 프로필에서 읽었다. 7개 복사본 SHA가 전달본과 일치했고 기존 PNG 입력 거절을 재실행했다. |
| T28 RAW | 통과 | SouthFirst 8,396,802바이트 SHA-256은 `59c7d2f4625cdaad5fae142c0133b54b4c4e4bb6e65264bd28bc647cacb85181`이다. NorthFirst RAW와 16비트 PNG는 북쪽부터 4,198,401샘플이 같고, 행 순서를 한 번 반전하면 C Base와 canonical RAW에 전수 일치한다. 원본 manifest의 2,800m 기록을 보존했다. |
| T03/T04 실제 Unity 임포트 | 통과 | 열린 6000.3.21f1에서 기존 HeightmapTests 11건 통과, 실패·건너뜀 0. B·C 임시 임포트의 경계 오차는 모두 0m였다. |
| C 저장 씬 재개방 | 통과 | 513² Terrain 16개, 경계 24곳, 최대 높이 오차 0.0106962m, 공유 경계 높이 차이 0m. Additive 재개방 직후 이웃이 자동 연결되기 전이라 Editor 연결 갱신을 명시 호출했고, 그 후 16개 모두 방향과 이웃이 일치했다. |

[P01 기존 기록](WORLDBUILDER.md#p01--구현-반영자동-검사-통과--사용자-테스트-대기) · [전달본 파일 검증](../../../docs/전달용/OpenWorld_HeightMap_Design_Regional_v1_1_Handoff/DOCUMENT_VALIDATION_v1_1.json) · [P10-A 실행 결과](WORLDBUILDER_P10_A.json)

## P10-B 월드 계획

재개 검증에서 C 수계의 1m 격자 호안 보간과 물굽이 종단을 수정했다. 열린 Unity에서 수계 후보의 충돌 0건을 확인하고 물굽이 57점, 하천과 호수 패치 2개, 타일 수면 6개, 폭포 1개를 C에 적용해 저장했다. 급경사 물굽이를 포함한 Core 검사 29건과 Unity 물·계획 검사 32건이 통과했다. 도로와 교대를 적용한 뒤에도 수계 검사 오류는 0건이었다. [Unity 수계 적용](WORLDBUILDER_P10_WATER.json) · [Unity 회귀](WORLDBUILDER_P10_PLAN.json) · [Core 회귀 원문](../../../output/WorldBuilder/P10/raster-core-regression.txt)

**월드 계획 통과.** C에 편집 가능한 다각형 마스크 5개, 고정 POI 3곳, 도로 3구간, 교량 1곳, 수계를 저장했다. 지역 경계의 전이 폭은 96m다. 저장된 Unity control map에서 가중치 합이 유지되도록 8비트 단계로 배분한다. 도로의 최종 등판은 11.74° 이하다. 교대 주변 지형 한도 50°와 실제 접근로 한도 12°를 설정했다. [생태 원본](../../../Assets/10.Datas/WorldBuilder/P10Regional/Ecology.asset) · [POI·예약·루프](../../../Assets/10.Datas/WorldBuilder/35c2ca802bd64575961ac7cda330ac91_Plan.asset) · [강·호수·폭포 원본](../../../Assets/10.Datas/WorldBuilder/P10Regional/WaterSystem.asset)

| POI | 후보 X/Y/Z (m) | 40m × 40m 원지형 높이 (m) | 최대 경사 | 초안 수면과 예약 중첩 |
|---|---:|---:|---:|---|
| 성 | 1069 / 97 / 1017 | 94.19~97.58 | 16.15° | 없음 |
| 강가 마을 | 833 / 172.5 / 1171 | 168.15~175.24 | 26.97° | 없음 |
| 호수 마을 | 1505 / 28.7 / 450 | 27.61~28.85 | 6.68° | 없음 |

| 검사 | 판정 | 근거 |
|---|---|---|
| T29 지표 가중치 | 통과 | 저장 씬의 65,536점에서 5개 레이어를 읽었다. 합의 최대 오차 0.0000000969, 공유 경계 차이 0, 혼합 표본 15,464점. 식생 경계는 자동 배치 후 검증한다. |
| T30 POI | 통과 | Unity 적용 전 검사에서 거점·교량 4개 지지, 경사, 확정 홍수 고도, 평탄화 및 계획 예약 충돌 0건. 세 거점 고정 앵커는 표의 좌표 그대로다. |
| T31 수계 | 통과 | 하천·호수·폭포에 도로와 교대 패치까지 조합한 Unity 검사에서 수계 오류 0건. 교량의 물 횡단 표본 32점, 침수 0점, 상판 하부 최소 여유고 4.486m. 초안 실패는 초기 기록에 보존했다. |
| T32 도로·Nav | 체크포인트 루프 통과 | 저장된 씬의 실제 Collider 44개로 생성한 Unity NavMesh에서 게이트 활성 준비 후 체크포인트 139구간, 타일 경계 6곳, 교량 상판 8구간을 양방향 확인했다. 1,434m 강가↔호수 구간의 단일 역방향 질의는 `PathPartial`이 재현됐다. 경로를 체크포인트로 나누면 같은 구간의 양방향이 모두 연결된다. |

Unity에서 기록한 도로 길이는 성→강가 759.6m, 강가→호수 1,434.0m, 호수→성 845.7m다. 강가로 가는 두 경로가 교량과 일부 접근로를 공유한다. 순환의 체감 길이와 경관은 T35 때 검수한다. [P10-B 초기 실행](WORLDBUILDER_P10_B.json) · [계획 적용과 규칙 회귀](WORLDBUILDER_P10_PLAN.json) · [최종 Nav 검사](WORLDBUILDER_P10_NAV.json)

계획 Region 5개와 생태 마스크는 같은 초기 폴리곤을 별도 데이터로 저장한다. 자동 승인 검토는 계획 Region을 POI 영역 3개로 교체하는 편집을 차단했다. 계획 의미를 바꾸는 구조 편집이라는 사유였다. T33의 동등성 검증 범위는 타일 경계를 가로지르는 높이 편집과 배치 재평가다. 계획 폴리곤 자체를 편집한 경우의 수계·Nav 전파는 미수행이다.

## P10-C 지역 자동 배치

**통과.** C 씬을 재개방해 지역별 소품 375개와 Terrain 나무 1,260그루, Detail 밀도 합계 28,444를 확인했다. 소품은 우거진 숲 95, 가을 숲 12, 메마른 지역 95, 평야·성 127, 고산 8, 강가 마을 11, 호수 마을 27개다. 나무는 우거진 숲 1,027, 가을 숲 218, 고산 수목한계 이하 15그루다. 바이옴 확률은 동일한 96m 다각형 전이에서 읽고 수계·도로·예약 및 각 프리셋의 서식 조건을 적용한다. [C 배치 규칙·식생](../../../Assets/10.Datas/WorldBuilder/P10Regional/Placement) · [Unity 배치 실행 결과](WORLDBUILDER_P10_PLACEMENT.json)

| 검사 | 판정 | 근거 |
|---|---|---|
| T29 지역 전이 | 통과 | 재개방한 지표 레이어 5개의 가중치 합 최대 오차 0.0000000969, 공유 경계 차이 0. 나무·Detail·자동 프리팹의 선택 확률이 같은 전이를 사용한다. |
| T33 Dirty/Full과 Lock | 통과 | 타일 경계를 넘는 12×12 샘플, +0.25m 편집의 Dirty와 Full 지형 SHA 및 배치 SHA가 각각 일치했다. C 재개방에서 기존 계획 해시와 고정 POI 3개·도로 3개의 Lock이 유지됐다. |
| T34 Override | 통과 | 7개 지역별 Pin·Delete·Transform을 직렬화해 다시 읽고 Dirty와 Full에 적용했다. 최종 표본 21건 모두 보존됐다. 첫 숲 이동의 지지 실패와 별도 `ReservedArea` 충돌이 기록됐다. 검증용 Override는 메모리에서만 사용했다. |

## P10-D 실제 TPS 검증

**Editor TPS 기술 검증 통과.** 열린 Unity Editor의 Boot → Title에서 C 저장 씬을 열고, 게임의 플레이어 프리팹과 파티 초기화, KCC, InGame 카메라를 사용했다. 교량은 양쪽 엔드포인트를 준비 상태로 전환했다. Input System 가상 게임패드로 주행했고, 각 구간 시작과 촬영 위치에 진입할 때만 순간이동했다. 검증 동안 SaveManager는 별도의 빈 폴더를 사용했다. C 발행 씬, 런타임 청크 스트리밍, 저장 재진입은 미수행이다. [Editor 재현 코드](../../../Assets/Tests/WorldBuilder/P10/Editor/WorldBuilderP10PlayProbe.cs) · [최종 실행 로그](../../../output/WorldBuilder/P10/TPS/probe.log)

| 검증 구간 | 결과 | 안정 접지 | 목표 수평 오차 |
|---|---|---:|---:|
| 성 접근로 | 통과 | 68/68 | 0.85m |
| 강가 마을 접근로 | 통과 | 97/97 | 0.82m |
| 호수 마을 접근로 | 통과 | 179/179 | 0.86m |
| 교량 상판 | 통과 | 199/204 | 0.86m |
| 타일 경계 | 통과 | 81/81 | 0.89m |

첫 실행은 캐릭터 모델 초기화에서 실패했다. 파티 초기화를 추가한 실행에서는 Arin으로 교량을 주행했고 다른 네 구간은 목표점에 도달하지 못했다. 마지막 실행은 Raon으로 진행했다. 플레이어 컨트롤러의 카메라 방향으로 입력을 계산하고 도로 샘플을 촘촘히 배치해 다섯 구간을 통과했다. 반복 계측은 같은 C 씬을 사용했다. [첫 실행](../../../output/WorldBuilder/P10/TPS/probe-attempt1.log) · [초기 주행](../../../output/WorldBuilder/P10/TPS/probe-attempt2.log)

| 지역 | 배치 샘플과 제약 | TPS 캡처 |
|---|---|---|
| 우거진 숲 | 소품 95개, 수관 1,027그루, 소품 지지 경사 최대 33° | [우거진 숲](../../../output/WorldBuilder/P10/TPS/dense_forest.png) |
| 가을 숲 | 소품 12개, 수관 218그루, 소품 지지 경사 최대 27° | [가을 숲](../../../output/WorldBuilder/P10/TPS/autumn_forest.png) |
| 메마른 지역 | 소품 95개, 소품 지지 경사 최대 30° | [메마른 지역](../../../output/WorldBuilder/P10/TPS/arid_badlands.png) |
| 평야·성 | 소품 127개와 풀 Detail, 소품 지지 경사 최대 20° | [평야·성](../../../output/WorldBuilder/P10/TPS/castle_plains.png) |
| 고산 | 소품 8개, 소품 지지 경사 최대 33°, 수목한계 이하 수관 15그루 | [고산](../../../output/WorldBuilder/P10/TPS/high_alpine.png) |
| 강가 마을 | 소품 11개, 최소 높이 154m, 소품 지지 경사 최대 24° | [강가 마을](../../../output/WorldBuilder/P10/TPS/riverside_village.png) |
| 호수 마을 | 소품 27개, 최소 높이 15m, 소품 지지 경사 최대 18° | [호수 마을](../../../output/WorldBuilder/P10/TPS/lakeside_village.png) |

컨셉 원본 참조는 C WorldProfile, 마스크는 Ecology, 제약·프리팹은 각 지역의 Rule·Canopy·Props 프리셋에 있다. 세 거점과 교량에는 블록아웃 프리팹을 사용한다. 경관 자료에는 수평선과 거점 블록아웃이 보이며, 숲 캡처의 배치 밀도와 고산 시야는 T35에서 확인한다. [컨셉 이미지 원본](../../../Assets/WorldBuilder/Worlds/35c2ca802bd64575961ac7cda330ac91/Source/Concepts) · [마스크](../../../Assets/10.Datas/WorldBuilder/P10Regional/Ecology.asset) · [배치 에셋](../../../Assets/10.Datas/WorldBuilder/P10Regional/Placement) · [TPS 화면 모음](../../../output/WorldBuilder/P10/TPS/overview.png) · [수치·캡처 지문](WORLDBUILDER_P10_TPS.json)

## 후속 게이트

[C 청크 발행 및 심리스 스트리밍 후속 기록](WORLDBUILDER_P10_STREAMING.md)

| 단계 | 상태 |
|---|---|
| P10-B / T29~T32 | 지표 가중치, 거점, 수계, 체크포인트 루프 통과. 장거리 단일 역방향 질의 실패 기록 유지. |
| P10-C / T33~T34 | 통과. 지역 자동 배치와 식생 전이, Lock·Override 보존 및 Dirty/Full 동등성 확인. |
| P10-D / T35 | Editor TPS 기술 검증 5/5, 지역 캡처 7/7. 경관과 실제 조작 품질은 **사용자 테스트 대기**. 세 도로를 잇는 전 구간 수동 주행도 이 검수에 포함한다. |

T35 검수는 Editor에서 Boot를 시작하고 Title 준비 후 `UPlayGround.Editor.WorldBuilderP10PlayProbe.Start()`로 진입한다. 로그에 `DONE`이 남으면 가상 게임패드가 해제되고 호수 마을에서 키보드 또는 실제 패드로 조작할 수 있다. 성·강가·호수에서 다음 목적지와 진입로를 찾을 수 있는지, 물가·교량·타일 경계에서 조작과 카메라가 끊기지 않는지, 컨셉과 게임 화면의 지역 구분이 읽히는지 확인한다. 검수 종료는 Play Mode 종료로 처리한다. [TPS 재현 코드](../../../Assets/Tests/WorldBuilder/P10/Editor/WorldBuilderP10PlayProbe.cs)

## 관련 회귀 및 보존

열린 Unity Editor에서 기존 생태·동굴·교량·스트리밍 회귀 12건이 통과했다. 최종 변경 후 T17·T18 및 Native 생태 검사 3건을 다시 실행해 모두 통과했다. 두 실행 모두 실패와 건너뜀은 0건이다. [Test Runner와 보존 결과](WORLDBUILDER_P10_REGRESSION.json)

| 검사 | P10에서 수행한 범위와 판정 |
|---|---|
| T17·T18 | 기존 P07/P08 fixture의 실제 AI 링크, 청크 지연·실패·재시도 관련 자동 검사 통과. C 런타임 경로는 미수행. |
| T19 | T18 fixture에 포함된 P08 왕복 50회를 재실행했다. 종료 시 소유 청크 0, 소유 Terrain·NavMesh·proxy 10,682,622B로 워밍업과 동일했다. 회수 후 총 메모리는 1,492,128,192B에서 1,628,069,177B로 늘었다. C 장기 계측과 메모리 귀속은 미수행이다. |
| T20 | 취소·도메인 리로드·프로세스 재시작 복구 재실행은 미수행. P08 Bake/Published 기준 파일 410개의 지문은 기존과 일치했다. |
| T21·T22 | 후속 MCP 사전 검사에서 C의 배치 ID 불일치(WB200)와 Navigation Layout 제약(WB500)을 재현했다. C Bake·발행과 standalone 번들 검사는 미수행이다. [C 심리스 검증 프롬프트·결과](WORLDBUILDER_P10_STREAMING.md) |
| T23 | 기존 게임 종합 회귀 미수행. P09의 세라 모프 실패 및 사용자 검수 대기 유지. |
| T24 | 기존 생태·동굴·교량 프리셋 검사 통과. |
| T25 | 2km/4km 장기 성능 및 Player GPU 재측정 미수행. |
| T26 | URP의 C Terrain 상공 캡처와 식생·거점이 들어간 7개 TPS 캡처에서 오류 셰이더 색의 픽셀을 0개로 집계했다. 물·폭포 재질의 TPS 근접 검수와 proxy 전환은 미수행이다. [C 바이옴 캡처](../../../output/WorldBuilder/P10/C-biome-overview.png) · [TPS 화면 모음](../../../output/WorldBuilder/P10/TPS/overview.png) |

B fixture 50개와 P08 Bake/Published 410개 파일의 SHA-256은 최종 회귀 후에도 P09 보존 기준과 일치했다. 저장 슬롯 관련 파일 5개의 SHA-256·크기·수정 시각도 일치했다. P09 상태 문서와 프로젝트·빌드·공용 Addressables 설정의 작업 시작 지문이 유지됐다. Editor를 P08Authoring 단독 Edit Mode로 복원했다. Player와 Addressables 빌드는 실행하지 않았다.

컨셉 이미지 7장과 meta는 C Source에 보관했다. 실패한 수계 대안의 파생 자산은 생성하지 않았다. NavMesh 진단 객체와 Unity 테스트 임시 fixture는 정리했다. 오프라인 수계 탐색은 재현 소스만 보관했다. [하류 단면 재현 소스](../../../output/WorldBuilder/P10/WaterProbe-final.cs.txt)
