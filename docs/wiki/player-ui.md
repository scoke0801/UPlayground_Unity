# 플레이어와 화면

지도 탐험 안개(2026-11-27): [설계](../../Assets/docs/design/ui-player/MAP_EXPLORATION_FOG_DESIGN.md)·[작업 상태](../work-status/map-exploration-fog.md). RiverBasin4km 전체 지도에서 지나간 주변만 드러나고, 마을·포탈·NPC·적 아이콘은 안개 속에서 숨는다(퀘스트·사용자 마커는 유지). 세이브 3.7에 기록하며 이전 저장은 전부 안개다. HUD 미니맵·지역 목록은 레거시라 대상에서 뺐다. Editor·Development 빌드용 치트(지역 이동 탭 `현재 지도 전부 밝히기`, 저장에 남음)를 추가했다. 컴파일·UI EditMode 193/193·Development 설정 스크립트 컴파일·펫 저장 31/31 통과. Play Mode에서 안개 표시와 임시 마커의 표시/숨김을 확인했고 치트의 Play 확인은 미수행이다. 이동·저장·입력·치트와 체감은 MF01~MF10 사용자 테스트 대기다.

퀘스트 길안내 연출(2026-11-22): [설계](../../Assets/docs/design/ui-player/QUEST_PATH_GUIDANCE_DESIGN.md)·[작업 상태](../work-status/quest-path-guidance.md). 미니맵 HUD 없이 키 탭 1회로 목표 방향 길을 나비가 안내하고(가까운 실제 나비 군집 + 경로 전체를 흐르는 빛나는 나비 행렬·잔광, 약 18초), 연출은 인터페이스로 분리해 바람 등으로 교체한다. 경로가 없으면 아무 연출도 하지 않고, 전투 중에는 X가 동작하지 않으며, 퀘스트 UI와 시간·날씨 표시는 평소 숨기고 X 입력 시 퀘스트만 잠시 보인다(시계 HUD 제거). P0~P4를 구현했다(나비·바람 구현체, 미니맵 HUD 기본 비표시, 입력 X / L3+서쪽 버튼). 정상 컴파일·UI·Input EditMode 214/214 통과. 실제 월드 길안내·미관·입력은 GD01~GD07 사용자 테스트 대기다.

**RiverBasin4km 섬과 보행교 미니맵.** 남부 호수의 섬 8개와 보행교 10개가 들어간 실제 지형을 4096×4096으로 다시 촬영했다. 기존 미니맵 스프라이트 GUID와 좌표를 유지했다. [지도와 검증](../work-status/river-basin-world.md#4km-남부-호수-섬)

**RiverBasin4km 미니맵.** 현재 씬을 4096×4096으로 재촬영해 기존 지도 배경을 갱신했다. Editor Play Mode에서 새 전체 지도 배경과 가상 Ctrl+좌클릭 이동을 확인했다. 물리 키보드·마우스 입력 및 화면 가독성은 사용자 테스트 대기다. [재촬영과 확인 절차](../work-status/river-basin-world.md#4km-미니맵-재촬영과-ctrl-이동)

미니맵 물 가독성 수정(2026-11-15): 활성 물 표면7개에 밝은 파랑 불투명 재질을 임시 적용해 재촬영·배경 연결했다. 촬영 후 원래 재질7/7 복원 확인, 씬·품질 설정 보존. [현재 지도·검증](../work-status/lake-waterfall.md#폭포-반영-미니맵-재촬영--2026-11-15).

폭포 미니맵 갱신(2026-11-15): 현재 호수 월드를3600×4200 재촬영해 LakeVillage MinimapConfig 배경에 반영했다. 기존 GUID·중심·범위 유지, 폭포 원본 픽셀과 임포트 참조 확인. 실제 HUD 가독성은 사용자 확인 대기. [작업 기록](../work-status/lake-waterfall.md#폭포-반영-미니맵-재촬영--2026-11-15).

드롭 표시 후속(2026-11-14): [분산·크기 개선과 DS01~05](../work-status/drop-item-scatter-size.md). 새 보상의 목표 간격0.55m/최대반경1.2m·지형 안전 후보 검사, 모델0.38m·부유0.35m·이름표 기준0.75m와 링/기둥 축소 반영. 이름표 크기/겹침 회피·클릭/패드·획득 거리4m는 유지한다. 컴파일/관련 EditMode22/22·실제 모델3종 크기 검사 통과, 실제 화면/수거·저장은 사용자 테스트 대기다.

## 정본

- [UI와 입력 저작](../../Assets/docs/guide/UI_UX_AUTHORING_GUIDE.md)
- [펫 V2 설계](../../Assets/docs/design/ui-player/pet-system/PET_SYSTEM_V2_DESIGN.html), [V2 구현 기록](../work-status/pet-system/PET_SYSTEM_V2_IMPLEMENTATION.html)
- [펫 V3 설계(전투 지원·성장 고도화)](../../Assets/docs/design/ui-player/pet-system/PET_SYSTEM_V3_DESIGN.html), [V3 상태·P1~P3 검증·사용자 테스트](../work-status/pet-system/PET_SYSTEM_V3_STATUS.md)
- [지역 하늘 설계](../../Assets/docs/design/world-art/environment/REGIONAL_SKY_ATMOSPHERE_DESIGN.html), [하늘 후속 확인](../work-status/regional-sky/REGIONAL_SKY_FOLLOWUP_TEST_SPEC.html)

## 진행 기록

훈련장 스태미나 후속(2026-11-15): [정정·연결 복구·HUD05](../work-status/hud-pet-effects-gauge.md#훈련장-스태미나-후속). 사용자 정정에 따라 상시 표시를 철회하고 필드와 같은 소비/회복 표시·지연 숨김을 유지한다. HUD보다 늦게 준비되거나 씬 이동으로 바뀐 플레이어의 이벤트·위치 추적 연결 누락을 보완했다. 정상 컴파일·수정 후 스태미나 EditMode 10/10 통과, 실제 훈련장 화면은 사용자 테스트 대기다.

HUD 배치·게이지 후속(2026-11-15): [반영·검증·HUD01~04](../work-status/hud-pet-effects-gauge.md). 펫 대기 표시 숨김·발동 알림 오른쪽 분리, 버프/디버프 행 48px 하향과 게이지 초기화 순서에 따른 이벤트 구독 누락 보완을 반영했다. 11월 20일 필살기 키 안내의 프리팹 참조를 복구해 관련 EditMode 42/42를 통과했다. [HUD06 실물 장치 전환 확인](../work-status/hud-pet-effects-gauge.md#필살기-입력-안내-후속). 실제 훈련장 교체·재진입과 화면 품질은 사용자 테스트 대기다.

펫 V3 P1~P3(2026-11-14): [상태·최종 검증·사용자 테스트](../work-status/pet-system/PET_SYSTEM_V3_STATUS.md). 신호 지원·공용 유대 3원천·두 슬롯·가벼운 손놀림·브레이크 교란과 메뉴/HUD를 반영했다. 전체 구현 후 최종 펫 EditMode72/72·AI 회귀27/27·PlayMode16/16, 펫 GA/GE42개 오류0·경고0 및 UI 연결을 확인했다. 전수 GAS에는 범위 밖 오류7·경고599가 남아 있다. A/B는 Astra 재검토 후 보류 유지. 실제 반응·성장 속도·패드/화면·교란 밸런스는 사용자 테스트 대기다.

공중 비행 입력 변경(2026-11-14): [입력 설계](../../Assets/docs/design/ui-player/GLIDING_INPUT_DESIGN.md)·[작업 상태](../work-status/gliding-input.md). 자동 순항·W 상승/S 하강·A/D 선회로 교체하고 카메라 피치 고도 조절과 S 전진 감속을 제거했다. 기존 속도·가속·도약/탑승 연결 범위는 유지한다. 정상 컴파일·관련 EditMode55/55·실행 SO 조회 통과. 실제 조작은 GI01~GI04 사용자 테스트 대기다.

드롭 이름표 후속(2026-11-13): [모델 조사·연결선 개선 상태·DI01~04](../work-status/drop-item-presentation.md), [모델 확장 검증](../work-status/drop-item-world-model-expansion.md). 장비 모델 재사용과 가늘어지고 흐려지는 연결선·실제 부유 추종·이름표 뒤 렌더링을 반영했다. 최종 소스 컴파일과 EditMode8/8은 후속에서 확인했다. 실제 비활성 수명·사용자 화면/획득 확인은 대기다.

| 항목 | 근거 | 반영과 검증의 경계 |
|---|---|---|
| 슈퍼 점프·활공 | 작업 상태 문서(2026-11-13 output 정리로 삭제) | Raon 인게임 연결·도약 지면 VFX 기록. 문서 상단 최신 범위는 Raon, 다른 캐릭터 확대를 가정하지 않음. 실제 인게임 입력·지형별 시각 검수 미수행 |
| 펫 화면 V2 | 검사 JSON(2026-11-13 output 정리로 삭제) | Unity 내 검증 메서드 직접 호출·참조 검사와 API/EventSystem 기반 화면 확인 기록. 실물 패드·월드 전투 중 거래·독립 Player는 미수행. NUnit Test Runner 전체 테스트 결과로 바꾸어 표현하지 않음 |
| 하늘·날씨 개선 | 결과·테스트 요약(2026-11-13 output 정리로 삭제) | 관련 EditMode 38/38 기록. PlayMode는 사용자 테스트 대기. 이 후 변경된 환경·셰이더가 있다면 재검증 범위에 포함 |
| 지역 하늘 | 기존 확인 JSON(2026-11-13 output 정리로 삭제) | 하늘·날씨 후속 변경과 함께 대조. 현재 화면 품질은 재확인 |

[툰 워터 월드 분석·상태](../work-status/toon-water-world.md)에 기존 수영·잠수와 수중 렌더링의 재사용 지점 및 신규 설계와의 차이를 연결했다. 설계 연결만으로 기존 하늘·날씨·카메라의 검증 상태가 바뀌지 않는다.

[수영·화면 후속](../work-status/water-swim-interaction-fix.md): 캐릭터 잠수 기반 HUD 착색 제거·숨 경고 유지, Shift 홀드 고속 수영·카메라 방향 수직 이동과 수영 중 즉시 획득 반영. 관련 EditMode 85/85 통과. 실제 입력·획득/저장 플레이는 사용자 테스트 대기.

시선 기반 수영 개선(2026-11-14): [통합 설계](../../Assets/docs/design/world-art/environment/TOON_WATER_EXISTING_INTEGRATION.md#시선-기반-수직-반응-개선)와 [후속 상태](../work-status/water-swim-interaction-fix.md#2026-11-14-시선-기반-상승하강-개선)에 웹 조사·적용·검증을 반영했다. 전용 상승·하강 키 없이 수평 여유±4도·배율2.6·진입대기0.1초·수직수렴14/s의 공통 SO를 연결하고 경계각 반올림에 의한 잠수 진입을 막았다. 정상 컴파일·관련 EditMode89/89·Unity 프리팹17/17과 현재 씬 Player 참조 검사 통과. 실제 조작 감각은 SS01~SS05 사용자 테스트 대기다.

모든 UI 트윈은 `SetUpdate(true)`를 적용한다. 초기 포커스·게임패드 이동·Cancel·탭 전환·일시정지 시 동작은 화면별 인수 조건이다. 문서와 자동 참조 검사만으로 조작 품질을 승인하지 않는다.

분야별 설계·상태·검증 문서 목록은 [설계 문서 분류](design-folder-map.md)에서 `UI·플레이어` 행을 본다. 가이드는 제외한다.

[위키 시작](README.md)


