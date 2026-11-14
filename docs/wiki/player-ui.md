# 플레이어와 화면

드롭 표시 후속(2026-11-14): [분산·크기 개선과 DS01~05](../work-status/drop-item-scatter-size.md). 새 보상의 목표 간격0.55m/최대반경1.2m·지형 안전 후보 검사, 모델0.38m·부유0.35m·이름표 기준0.75m와 링/기둥 축소 반영. 이름표 크기/겹침 회피·클릭/패드·획득 거리4m는 유지한다. 컴파일/관련 EditMode22/22·실제 모델3종 크기 검사 통과, 실제 화면/수거·저장은 사용자 테스트 대기다.

## 정본

- [UI와 입력 저작](../../Assets/docs/guide/UI_UX_AUTHORING_GUIDE.md)
- [펫 V2 설계](../../Assets/docs/design/ui-player/pet-system/PET_SYSTEM_V2_DESIGN.html), [V2 구현 기록](../work-status/pet-system/PET_SYSTEM_V2_IMPLEMENTATION.html)
- [펫 V3 설계(전투 지원·성장 고도화)](../../Assets/docs/design/ui-player/pet-system/PET_SYSTEM_V3_DESIGN.html), [V3 상태·P1 검증·사용자 테스트](../work-status/pet-system/PET_SYSTEM_V3_STATUS.md)
- [지역 하늘 설계](../../Assets/docs/design/world-art/environment/REGIONAL_SKY_ATMOSPHERE_DESIGN.html), [하늘 후속 확인](../work-status/regional-sky/REGIONAL_SKY_FOLLOWUP_TEST_SPEC.html)

## 진행 기록

펫 V3 P1(2026-11-14): [상태·최종 검증 원본·PV01~06](../work-status/pet-system/PET_SYSTEM_V3_STATUS.md). 응급 회복/기세/버팀의 정책 신호, 독립 GAS 쿨다운(45/10/15초)과 전역 8초, 세이브 v3 슬롯 1개·알 수 없는 ID 보존, 기존 FX·HUD 반응을 반영했다. **최종 저장 보완 후 EditMode46/46·PlayMode9/9·GA/GE30개 오류0·HUD 참조 정상**을 재확인해 이전 자동 재검증 차단을 해소했다. 전체 GAS 검사에는 범위 밖 오류7·경고599가 남아 있다. 실제 반응 체감·타이머 지원 대비 만족도·FX/HUD 시각 품질은 사용자 테스트 대기다. 유대·두 번째 슬롯·EnemyBroken은 후속 범위다.

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
