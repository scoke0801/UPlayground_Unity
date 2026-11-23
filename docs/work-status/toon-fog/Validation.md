# 검증과 인수

## 실행 대상

- [비교 씬](../../../Assets/01.Scenes/ArtReview/ToonFogValidation.unity). 씬을 단독으로 열고 Play하면 거리 구역에서 시작한다. 마우스로 구역, 안개, 날씨 밀도와 전투 가중치를 조절한다. Game 및 Scene View를 분리해 본다.
- [DroughtVillage](../../../Assets/01.Scenes/Scenario/DroughtVillage.unity). 실제 진행 비교는 기존 테스트 슬롯이나 별도 검증 슬롯에서 수행한다. 사막 마을에 도착한 뒤 탐험과 전투를 비교한다. 키보드·마우스 또는 평소 사용하던 게임패드로 진행한다.
- [WaterLab](../../../Assets/01.Scenes/Tests/WaterLab.unity). 수면 위·반잠수·수중·건조 차집합의 고정 재현 장면이다.

## 자동 검사

`UPlayGround.EditorTools.Tests.ToonFogMathTests`는 실제 GPU 함수 결과를 독립 double 수치 적분과 기하학 기준값으로 비교한다. M01~M14를 다룬다. `WaterRenderingContractTests`는 기존 수역 계약의 회귀 검사다. 최종 합동 EditMode 실행은 44/44 통과, 실패·건너뜀 0이었다. 작업 ID는 `4872c9d7aaca40de9a20d21759978519`다. 공통 하네스 자체 검사 43개도 통과했다.

## Editor 고정 카메라 결과

Unity 6000.3.21f1, PC URP 17.3.0, Windows D3D11, RTX 3060 Ti, i5-12400F. 640×360 URP SingleCameraRequest로 실제 컬러 타깃을 읽었다. 아래 표의 RGB 변경은 채널 차이가 1단계를 넘는 픽셀 수다. 고정 캡처에서는 셰이더의 비동기 컴파일을 일시 중단하고 첫 렌더를 준비용으로 사용한다.

| 확인 | 결과 | 화면 |
| --- | --- | --- |
| 거리 꺼짐 반복 및 환경 밀도 0 | 각각 230,400픽셀의 RGBA 일치 | [기준](captures/sample-distance-off.png) |
| 거리 안개 켬 | RGB 80,496픽셀 변경, 최대 112단계, alpha 변경 0 | [켜짐](captures/sample-distance-on.png) |
| 직교 카메라, 원근 재진입 | 두 비교 모두 RGBA 일치. 각 요청 후 안개 활성 및 슬롯 수가 0 | |
| 국소 외부 관찰, 전경 벽, 계곡 상단 | 고정 화면 1x·4x 출력. 두 캡처에서 앞벽과 하늘 앞 타원체를 확인 | [1x](captures/sample-local-on.png) · [4x](captures/sample-local-msaa4.png) · [계곡 상단](captures/sample-height-on.png) |
| 라온·식생·컷아웃·굴절 재질 | 고정 화면 RGB 112,182픽셀 변경, alpha 변경 0. 외곽선 배경 교체와 동작 중 재질 품질은 사용자 테스트 대기 | [끔](captures/sample-materials-off.png) · [켬](captures/sample-materials-on.png) |
| 지역 경계 | 고정 구도 캡처. 이동 중 블렌딩과 슬롯 교체는 사용자 테스트 대기 | [경계](captures/sample-regions-on.png) |
| 사막 마을 | 실제 씬의 추가 고정 카메라에서 RGB 94,937픽셀 변경. alpha 34픽셀에서 최대 3단계 차이. 실제 조작 카메라와 HUD는 사용자 테스트 대기 | [끔](captures/drought-village-off.png) · [켬](captures/drought-village-on.png) |
| 관리 수역 | WaterLab 반잠수 카메라에서 Resolve 1회. RGB 81,634픽셀 변경, alpha 0픽셀 변경 | [끔](captures/managed-water-off.png) · [켬](captures/managed-water-on.png) |

초기 비교에서 밀도 0에도 깊이 입력을 요청하는 경로를 찾아 제거했다. 샘플은 DepthNormals를 갖춰 PC Forward+ 깊이 프리패스와 맞추고, 추가 로드한 기존 월드의 중첩을 방지하도록 샘플 좌표를 분리했다.

관리 수역의 첫 시도는 D3D11 입자 셰이더 컴파일 IPC 오류로 중단됐다. 수역 구간 평가와 입자 표면 안개를 분리하고 동일한 WaterLab 카메라로 다시 실행했다. 최종 조사에서 신규 합성·샘플과 연동 물·입자 및 포말 7개 셰이더 에셋의 ShaderUtil 오류는 0건이었다.

## 화면과 통합

| 항목 ID | 조작 | 기대 결과와 실패 기준 |
| --- | --- | --- |
| V01, V02 | 거리 구역에서 안개 끔과 밀도 0 비교. 밀도를 1로 올리고 10/50/100/500m 기준과 하늘 확인 | 밀도 0과 끔의 RGB·alpha가 일치. 가까운 기준은 선명하고 먼 기준은 팔레트 영향을 받으며 하늘에 깨짐이 없음 |
| V03–V05 | 계곡과 계곡 위 구도를 왕복. 정지 후 Scene 카메라로 부피 밖·안과 앞벽을 통과 | 고도 기준이 씬에 고정. 타원체가 하늘 앞에도 보이고 앞벽에는 뒤 안개가 안 씌워짐 |
| V06–V09 | 거리 구역의 투명 판과 컷아웃을 비교. 통합 구역 라온의 외곽선을 벽·원경·하늘 앞에서 확인 | 투명 끝단 잔색과 Additive 사각형 없음. 외곽선·식생의 깊이 경계가 배경에 따라 깜박이면 실패 |
| V10 | 재질과 물 구역에서 안개 On/Off. 얕은 부분, 굴절 배경, 물 위 입자와 기존 물 씬의 반잠수 확인 | 수면 가까이에 안개 없는 절단이나 중복색 없음. 수중 광학과 물 입자의 기존 반응 유지 |
| V11, V12 | 샘플 조절 패널과 DroughtVillage HUD 확인. Game/Scene View를 번갈아 보고 카메라별 Off 확인 | HUD 색 유지. 직교/명시적 제외 카메라에 앞 카메라의 안개가 남으면 실패 |
| V13, V14 | 가능한 MSAA 1x/4x 및 Render Scale 0.7/1.0/1.2에서 같은 구도 확인 | 얇은 실루엣에서 심한 halo 또는 깊이 위치 어긋남 없음 |
| V15 | DroughtVillage에서 탐험 중 전투 진입, 락온·카메라 당김·전투 종료 | 공격과 얼굴이 읽히고 거리에 비례한 안개가 카메라 FOV 전환에 따라 튀지 않음 |
| V16, V17 | 지역 경계를 Scene 카메라와 샘플 기준 카메라로 왕복. 지역 프리셋·날씨·구역을 변경 | 동순위와 상위 프로필이 부드럽게 전환. 슬롯 교체와 먼 구역 이동에 잔여 색·점멸 없음 |
| V18, V19 | Play 진입과 종료를 반복하고 Feature 토글. 날씨 밀도 0, 환경 공급자 미지정, 비활성 프로필을 확인 | 안개 전역 상태가 누출되지 않고 중립 입력이 작동함. 밀도 0에는 국소 부피도 사라짐 |

사용자 테스트 결과는 `항목 ID / 통과·실패·미수행 / 실제 결과 / 화면 또는 Console 로그` 형식으로 남긴다. 오류가 있으면 구역·시작 상태와 당시 카메라·품질 설정을 함께 기록한다.

[작업 상태](TaskStatus.md) · [성능](Performance.md)

## RiverBasin4km 고정 카메라

Unity Editor D3D11, 640×360, MSAA 1x에서 씬의 기존 카메라로 Built-in Fog와 툰 안개를 비교했다. 두 구도 모두 4km 프로필을 읽고 `WaterResolve` 상태를 기록했다. 기존 검증 도구로 오류 셰이더 색과 렌더 후 안개 전역 상태 초기화도 확인했다.

| 구도 | 변경 픽셀 | 화면 |
| --- | --- | --- |
| B1 진입로 | RGB 94,597/230,400, alpha 0 | [Built-in](captures/river-basin-overview-legacy.png) · [툰 안개](captures/river-basin-overview-toon.png) |
| B1 수면 가까이 | RGB 94,458/230,400, alpha 0 | [Built-in](captures/river-basin-b1-water-legacy.png) · [툰 안개](captures/river-basin-b1-water-toon.png) |

실제 플레이는 사용자 테스트 대기다. RiverBasin4km을 단독 Play하고 평소 입력 장치로 B1 진입로에서 시작한다.

| 항목 ID | 조작 | 기대 결과와 실패 기준 |
| --- | --- | --- |
| RB-FOG-01 | B1 다리를 건너며 가까운 길과 먼 능선을 번갈아 본다 | 길과 HUD가 읽히고 원경이 거리에 따라 옅어진다. 식생 경계나 외곽선이 움직일 때 튀면 실패 |
| RB-FOG-02 | B1 물가에서 수면을 바라본 뒤 잠수하고 다시 수면 위로 나온다 | 수면 경계에서 색이 갑자기 띠로 끊기거나 안개가 겹쳐 보이면 실패 |

결과는 `항목 ID / 통과·실패·미수행 / 증상과 위치 / 화면 또는 로그` 형식으로 남긴다.
