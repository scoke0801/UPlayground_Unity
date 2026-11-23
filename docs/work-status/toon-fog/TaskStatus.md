# 툰 안개 작업 상태

[통합 계약](../../../Assets/docs/design/world-art/environment/TOON_FOG_INTEGRATION.md) · [통합](Integration.md) · [재질](MaterialCoverage.md) · [검증](Validation.md) · [성능](Performance.md) · [월드와 아트](../../wiki/world-art.md)

| 단계 | 상태 | 근거 |
| --- | --- | --- |
| 0 환경 조사 | Done | 실제 버전·렌더러·시작 Console·라온 렌더 큐 확인. 거리 기준과 실제 마을의 안개 끔 캡처 |
| 1 수학과 데이터 | Done | 공통 적분·프로필·품질 데이터. GPU 수학과 기존 수역 EditMode 회귀 통과 |
| 2 불투명 패스 | Done | PC Renderer 합성·정리 연결. 거리 큐에서 중립 및 켜짐 alpha 일치 |
| 3 높이와 팔레트 | Done | 256색 LUT·진단 연결. 높이 수치 적분 비교와 계곡 상단 캡처 |
| 4 국소 부피 | Done | 형상·경계·슬롯 구현. 교차 수학, 벽과 하늘 앞 부피 고정 화면. 진입·슬롯 교체 사용자 테스트 대기 |
| 5 재질과 수역 | In Progress | 투명과 굴절 경로 연결. Legacy/관리 수역 고정 화면 확인. 실제 외곽선·식생 움직임과 Fake Shadow 검증 대기 |
| 6 지역과 입력 | Done | 일곱 지역·DroughtVillage 날씨·교전·기준점 연결. 플레이 이동과 전투 전환 사용자 테스트 대기 |
| 7 수명과 성능 | In Progress | 중립·직교·원근 재진입과 CPU 렌더 요청 비교. 프레임 GPU·GC와 전체 카메라 스택, Player 변형 미검증 |
| 8 인수 | In Progress | 씬·근거·재현 명세 저장. 사용자 플레이·미술 인수 및 Player 검증 대기 |

Player·Addressables 빌드 미수행. [비교 씬](../../../Assets/01.Scenes/ArtReview/ToonFogValidation.unity)과 [실제 적용 씬](../../../Assets/01.Scenes/Scenario/DroughtVillage.unity).

## RiverBasin4km 적용

2026-11-22 **씬 반영 · 고정 화면 검증 통과 · 사용자 테스트 대기.** [RiverBasin4km](../../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/RiverBasin4km.unity)의 Built-in Fog를 끄고 루트 `Toon Fog World`에 [4km 프로필](../../../Assets/10.Datas/Environment/ToonFog/Fog_RiverBasin4km.asset), 공용 품질 에셋, 기존 날씨 컨트롤러를 연결했다.

평야와 성 팔레트를 사용하고 B1 강변을 기준으로 기준 고도 50m, 팔레트 원거리 2,200m, 최대 평가 거리 4,500m를 초기값으로 정했다. 씬 전역에 하나의 프로필이 적용된다. 기존 수역 Resolve가 공기 구간을 합성하고 Legacy 굴절용 추가 컬러 복사는 비활성 상태다. 저장 전후 씬을 비교해 안개 설정 외의 기존 작업이 보존된 것을 확인했다.

[D3D11 고정 카메라 비교와 사용자 확인 항목](Validation.md#riverbasin4km-고정-카메라)
