# 플레이어와 화면

## 정본

- [UI와 입력 저작](../../Assets/docs/guide/UI_UX_AUTHORING_GUIDE.md)
- [펫 V2 설계](../../Assets/docs/design/ui-player/pet-system/PET_SYSTEM_V2_DESIGN.html), [V2 구현 기록](../work-status/pet-system/PET_SYSTEM_V2_IMPLEMENTATION.html)
- [지역 하늘 설계](../../Assets/docs/design/world-art/environment/REGIONAL_SKY_ATMOSPHERE_DESIGN.html), [하늘 후속 확인](../work-status/regional-sky/REGIONAL_SKY_FOLLOWUP_TEST_SPEC.html)

## 진행 기록

| 항목 | 근거 | 반영과 검증의 경계 |
|---|---|---|
| 슈퍼 점프·활공 | 작업 상태 문서(2026-11-13 output 정리로 삭제) | Raon 인게임 연결·도약 지면 VFX 기록. 문서 상단 최신 범위는 Raon, 다른 캐릭터 확대를 가정하지 않음. 실제 인게임 입력·지형별 시각 검수 미수행 |
| 펫 화면 V2 | 검사 JSON(2026-11-13 output 정리로 삭제) | Unity 내 검증 메서드 직접 호출·참조 검사와 API/EventSystem 기반 화면 확인 기록. 실물 패드·월드 전투 중 거래·독립 Player는 미수행. NUnit Test Runner 전체 테스트 결과로 바꾸어 표현하지 않음 |
| 하늘·날씨 개선 | 결과·테스트 요약(2026-11-13 output 정리로 삭제) | 관련 EditMode 38/38 기록. PlayMode는 사용자 테스트 대기. 이 후 변경된 환경·셰이더가 있다면 재검증 범위에 포함 |
| 지역 하늘 | 기존 확인 JSON(2026-11-13 output 정리로 삭제) | 하늘·날씨 후속 변경과 함께 대조. 현재 화면 품질은 재확인 |

[툰 워터 월드 분석·상태](../work-status/toon-water-world.md)에 기존 수영·잠수와 수중 렌더링의 재사용 지점 및 신규 설계와의 차이를 연결했다. 설계 연결만으로 기존 하늘·날씨·카메라의 검증 상태가 바뀌지 않는다.

[수영·화면 후속](../work-status/water-swim-interaction-fix.md): 캐릭터 잠수 기반 HUD 착색 제거·숨 경고 유지, Shift 홀드 고속 수영·카메라 방향 수직 이동과 수영 중 즉시 획득 반영. 관련 EditMode 85/85 통과. 실제 입력·획득/저장 플레이는 사용자 테스트 대기.

모든 UI 트윈은 `SetUpdate(true)`를 적용한다. 초기 포커스·게임패드 이동·Cancel·탭 전환·일시정지 시 동작은 화면별 인수 조건이다. 문서와 자동 참조 검사만으로 조작 품질을 승인하지 않는다.

분야별 설계·상태·검증 문서 목록은 [설계 문서 분류](design-folder-map.md)에서 `UI·플레이어` 행을 본다. 가이드는 제외한다.

[위키 시작](README.md)
