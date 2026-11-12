# 월드와 아트

## 정본

- [맵 저작](../../Assets/docs/guide/OPENWORLD_MAP_AUTHORING_GUIDE.md), [배치 도구](../../Assets/docs/guide/MAP_PLACEMENT_TOOL_GUIDE.md)
- [카메라 가림](../../Assets/docs/guide/CAMERA_OCCLUSION_FADE_GUIDE.md), [지형·물 실패 사례](../../Assets/docs/guide/TERRAIN_WATER_ART_LESSONS.md)
- [지형 아트 재작업 설계](../../Assets/docs/design/WORLD_THEME_TERRAIN_REWORK_DESIGN.html)
- [NPC·환경 등 통합 인계](../../Assets/docs/design/LivingWorld/I_INTEGRATION.md)

맵은 시선·이동 선택·발견·보상을 만드는 공간이다. 기존 구매 아트·MCP 제작 원본·미커밋 씬 복구 자료를 보존한다. 셀 수·GUID 보존·Missing Script 0은 미술 인수나 실제 플레이 성공을 뜻하지 않는다.

## 실제 작업 상태

| 항목 | 진행 근거 | 현재 판단과 다음 작업 |
|---|---|---|
| 테마 독립 제작 | [A00 체크포인트](../../output/WorldThemeProduction/A00/CURRENT_STATE.md) | 원문 중간의 ‘최신 재개 기록’에 사용자 제작 중단과 미술 실패가 명시됨. 앞부분의 ‘제작 중’, 과거 Editor lease를 새 작업의 권한으로 쓰지 않음. 재작업 설계와 현재 기준선을 확인하고 재개 여부부터 판단 |
| 채석장·고원·항구 정규 통합 | [ABC 결과](../../output/WorldThemesABCIntegration/RESULT.html), [테스트 명세](../../output/WorldThemesABCIntegration/TEST_SPEC.html) | 정규 씬/전용 자원 통합 기록. 미술 인수·실제 TPS·성능 미완료. 장거리 6조회 중 3 Partial 및 기존 누락 참조/빈 재질을 후속 문제로 유지 |
| 강 유역 월드 | [제작 결과](../../output/RiverBasinWorld/RESULT.html), [검사 결과](../../output/RiverBasinWorld/tests.json), [테스트 명세](../../output/RiverBasinWorld/TEST_SPEC.html) | Boot Ready와 가림 규칙 8/8 기록. 입력 기반 검증 아님. 미술 NO-SHIP/사용자 인수 대기, 서비스 경고·Localization 오류 기록. 결과 표의 한 항목만 보고 전체 통과로 판단하지 않음 |
| 본편 OpenWorld 개편·보수 | [기록](../../output/OpenWorld/RESULT.md), [탐험 작업 단위](../../output/OpenWorld/Exploration/WORK_UNITS.md) | 누적 변경 이력과 백업이 섞임. 현행 씬·지역 데이터와 필요한 작업 단위만 대조. 폴더 날짜를 기준선 선택 근거로 쓰지 않음 |

## 아트 인수

제작 출처·원본·Unity 산출물과 meta를 식별하고, 작업에 필요한 스케일·피벗·노멀·UV·재질·충돌·LOD를 확인한다. Blender 검수와 Unity 적용 검수를 구분한다. 실제 TPS 화면·조명·카메라 가림·동선은 사용자 확인을 남기며 수치 통과로 대체하지 않는다.

[위키 시작](README.md)
