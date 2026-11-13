# 월드와 아트

## 정본

- [맵 저작](../../Assets/docs/guide/OPENWORLD_MAP_AUTHORING_GUIDE.md), [배치 도구](../../Assets/docs/guide/MAP_PLACEMENT_TOOL_GUIDE.md)
- [카메라 가림](../../Assets/docs/guide/CAMERA_OCCLUSION_FADE_GUIDE.md), [지형·물 실패 사례](../../Assets/docs/guide/TERRAIN_WATER_ART_LESSONS.md)
- [지형 아트 재작업 설계](../../Assets/docs/design/WORLD_THEME_TERRAIN_REWORK_DESIGN.html)
- [NPC·환경 등 통합 인계](../../Assets/docs/design/LivingWorld/I_INTEGRATION.md)
- [드롭 아이템 월드 모델 제작](../../Assets/docs/guide/DROP_ITEM_WORLD_MODEL_AUTHORING_GUIDE.md) — 실행 절차·함정은 [drop-item-world-model 스킬](../../.claude/skills/drop-item-world-model/SKILL.md), 공용 빌드 라이브러리 [dropitem_lib.py](../../ArtSource/DropItems/_lib/dropitem_lib.py)

맵은 시선·이동 선택·발견·보상을 만드는 공간이다. 기존 구매 아트·MCP 제작 원본·미커밋 씬 복구 자료를 보존한다. 셀 수·GUID 보존·Missing Script 0은 미술 인수나 실제 플레이 성공을 뜻하지 않는다.

## 전달 설계 3종

2026-11-12 설계 문서와 관련 소스를 정적으로 대조했다. [공통 연결·책임 경계](../전달용/README.md)에 원문 역할과 설계 간 계약을 정리했다. 원문 위치는 보존하며 기존 시스템 설계를 자동 대체하지 않는다.

| 설계 입력 | 분석·작업 상태 | 현재 판단 |
|---|---|---|
| [GPU 환경 생물](../전달용/ambient_fauna_design/01_환경생물_정규설계.md) · [역할·백엔드 ADR](../../Assets/docs/design/AMBIENT_FAUNA_ROLE_SEPARATION.md) · [저작](../../Assets/docs/guide/AMBIENT_FAUNA_AUTHORING_GUIDE.md) | [상태·검증·사용자 테스트](../work-status/ambient-fauna.md) | Compute/RenderGraph Indirect·날갯짓/발광·네 종 자산 반영. 본편 장식 나비 17곳/총 Capacity 34로 감축·폭6~10cm 조정, CPU 104개 원본 보존. 이전 EditMode 15/15·PlayMode 2/2 통과 후 사용자 과밀 피드백 반영, 수정본 PlayMode 2/2·출력 재검증 통과. TPS 품질·장시간 성능은 사용자 인수 대기 |
| [반응형 환경](../전달용/environment_system_design_v1/environment_system_design_v1.html) | [상태](../work-status/reactive-environment.md), [실제 통합 계약](../../Assets/docs/design/REACTIVE_ENVIRONMENT_INTEGRATION.md), [사용자 테스트](../work-status/reactive-environment-tests.md) | GAS 원소·날씨/젖음·연소 전파·사전 파괴/낙목·단일 보상·저장·커스텀 툰 8패스와 5종 샘플 반영. 관련 EditMode 55/55 통과, C#·셰이더 검사 오류 0. 실제 월드 플레이/최종 아트·성능은 사용자 테스트 대기. 원거리 시뮬레이션/대량 월드 전환은 별도 범위 |
| [툰 워터 월드](../전달용/toon_water_world/toon_water_world_implementation_design.html) | [상태](../work-status/toon-water-world.md), [통합 계약](../../Assets/docs/design/TOON_WATER_EXISTING_INTEGRATION.md) | 단일 합성·반잠수·Fog 중복 방지·파도/접촉/비·폭포를 구현하고 정규 호수에 연결. 기존 단일 샘플 EditMode 90/90·Lab 14건·정규 4건 성공 후 사용자 MSAA 4배 불일치 오류 접수. 색/깊이 샘플 유지 수정 후 1/2/4배 Lab 42건·정규 4배 2건 통과, Console 오류0. 사용자 실제 플레이 재확인은 WU07 대기. 실제 입력·미술·실기 성능은 사용자 테스트 대기. GPU 생태계·원자 수중 채집은 별도 후속 |

세 문서의 초기 저장소 조사 전체가 완료된 것은 아니다. 초기 문서 분석 시점에는 구현·아트 제작·Unity 테스트·Player 빌드를 실행하지 않았다. 이후 구현과 검증은 각 상태 문서의 현재 판정을 따른다. 상세 남은 조사와 검증은 각 상태 문서에서 관리한다. 원문·상태·위키의 로컬 링크 80건 대상 존재를 확인했고 누락은 없었다. 임시 산출물은 없으며 전달 원문·도식·템플릿과 기존 사용자 변경은 보존했다.

## 실제 작업 상태

반응형 환경에는 기존 에디터 치트 콘솔/환경 반응 모니터를 통한 에셋 소환·열/물/구조 입력·교체/제거 치트를 추가했다. 새 ID/소유 씬/원본 보존 단언 2개 직접 호출 통과, 실제 Play 조작은 [RE-C01~03](../work-status/reactive-environment-tests.md) 사용자 테스트 대기다. MSAA 렌더링 오류는 별도 미해결이다. 일반 씬/식생 확장은 [검토안](../../Assets/docs/design/REACTIVE_ENVIRONMENT_INTEGRATION.md#일반-씬-오브젝트식생-확장-검토안)을 추가했다. 개체형 소품/나무와 Terrain Detail 풀의 셀 어댑터를 분리하고 코어를 재사용하는 방향이며 구현/성능 검증은 미수행이다. 반응형 환경은 커스텀 툰까지 실행 경로와 저작 자산을 연결했다. 최종 Test Runner 55/55, 샘플 5/5 저작 검사·Missing Script 0, GPU 표면 검사와 8패스/Forward+ 변형 컴파일을 확인했다. [최종 상태·근거](../work-status/reactive-environment.md)와 [RE-01~15 사용자 확인](../work-status/reactive-environment-tests.md)을 기준으로 인수한다. 기존 스킬의 새 원소 입력은 기본 0이며 명시적 GAS 저작이 필요하다. 전체 P0~P6 릴리스나 정규 월드 아트 교체 완료로 판정하지 않는다.

| 항목 | 진행 근거 | 현재 판단과 다음 작업 |
|---|---|---|
| 테마 독립 제작 | [A00 체크포인트](../../output/WorldThemeProduction/A00/CURRENT_STATE.md) | 원문 중간의 ‘최신 재개 기록’에 사용자 제작 중단과 미술 실패가 명시됨. 앞부분의 ‘제작 중’, 과거 Editor lease를 새 작업의 권한으로 쓰지 않음. 재작업 설계와 현재 기준선을 확인하고 재개 여부부터 판단 |
| 채석장·고원·항구 정규 통합 | [ABC 결과](../../output/WorldThemesABCIntegration/RESULT.html), [테스트 명세](../../output/WorldThemesABCIntegration/TEST_SPEC.html) | 정규 씬/전용 자원 통합 기록. 미술 인수·실제 TPS·성능 미완료. 장거리 6조회 중 3 Partial 및 기존 누락 참조/빈 재질을 후속 문제로 유지 |
| 강 유역 월드 | [제작 결과](../../output/RiverBasinWorld/RESULT.html), [검사 결과](../../output/RiverBasinWorld/tests.json), [테스트 명세](../../output/RiverBasinWorld/TEST_SPEC.html) | Boot Ready와 가림 규칙 8/8 기록. 입력 기반 검증 아님. 미술 NO-SHIP/사용자 인수 대기, 서비스 경고·Localization 오류 기록. 결과 표의 한 항목만 보고 전체 통과로 판단하지 않음 |
| 드롭 아이템 월드 모델 (22종) | [MANIFEST(상태·프롬프트 전문·검수 표)](../../ArtSource/DropItems/MANIFEST.md), 스크린샷 `ArtSource/DropItems/*/Review/` | 2026-11-12 기준 22종 전부 `ItemSO.worldPrefab` 연결. 실제 드롭 경로(`SpawnItems`)로 등장·정규화(긴 변 0.55m)·획득을 확인했고, 위치 지정·라이브 카메라 캡처로 낮·밤·회전 검수까지 마쳤다. 드롭 관련 EditMode 13/13 통과. 사용자 확인이 남은 결정: Item_Gem·Eye는 사용자 판정으로 1차 형태 유지, HP 포션 5종은 메시를 공유해 아이콘별 병 모양 대신 희귀도 색으로만 구분, 경험의 물약은 원뿔 병, 소금 자루는 아이콘 기준 가방 형태, 구운 고기는 세운 자세. 8m에서 작게 보이는 문제(깃털·광석·열쇠)는 공통 `tokenSize` 문제. 펫 외 자동 수거 경로는 범위 밖이라 미조사 |
| 본편 OpenWorld 개편·보수 | [기록](../../output/OpenWorld/RESULT.md), [탐험 작업 단위](../../output/OpenWorld/Exploration/WORK_UNITS.md) | 누적 변경 이력과 백업이 섞임. 현행 씬·지역 데이터와 필요한 작업 단위만 대조. 폴더 날짜를 기준선 선택 근거로 쓰지 않음 |

## 아트 인수

제작 출처·원본·Unity 산출물과 meta를 식별하고, 작업에 필요한 스케일·피벗·노멀·UV·재질·충돌·LOD를 확인한다. Blender 검수와 Unity 적용 검수를 구분한다. 실제 TPS 화면·조명·카메라 가림·동선은 사용자 확인을 남기며 수치 통과로 대체하지 않는다.

[위키 시작](README.md)
수면 경계가 약하고 수중에서 비가 보인다는 사용자 피드백에 따라 기존 재질 색/불투명도 복구와 날씨 수중 차폐를 반영했다. 관련 EditMode 24/24·MSAA 1/2/4배 렌더 51건·정규 호수 눈높이/높은 시점/잠수 3건 통과, Console 오류0. [최종 화면·WU08](../work-status/toon-water-world.md)의 실제 비/잠수 전환과 미술 인수는 사용자 확인 대기다.
