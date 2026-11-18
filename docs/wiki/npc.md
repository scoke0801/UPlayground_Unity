# NPC 생활과 제작

## 정본과 연결 지점

- [생활 AI 현재 상태](../work-status/npc-life-ai/STATUS.md), [NPC 외형 저작](../../Assets/docs/guide/NPC_APPEARANCE_AUTHORING_GUIDE.md)
- [NPC 일상 애니메이션 저작](../../Assets/docs/guide/NPC_DAILY_ANIMATION_AUTHORING_GUIDE.md): 기존 클립 재사용과 실제 접지·접촉·반복·중단 복구 검수 기준. [2026-11-15 재작업 상태](../work-status/npc-daily-animation/STATUS.md).
- [월드·NPC 설계](../world_event_npc_system_design.html), [대사 Bake 설계](../NPC_Dialogue_Bake_Harness_Design.html)
- Bake의 [계약](../dialogue-bake/CONTRACT.md), [파이프라인](../dialogue-bake/PIPELINE.md), [검수](../dialogue-bake/REVIEW.md), [실제 연결 조사](../dialogue-bake/SOURCE_AUDIT.md)

설계 예시를 정식 세계관 사실이나 구현 완료로 해석하지 않는다. 기존 시간·저장·NpcBrain/NpcRoutine·시뮬레이션 소유권을 먼저 재사용한다.

## 진행과 차단 항목

- 2026-11-17 [NPC 정렬 후속 확대](../work-status/npc-work-alignment.md#다른-npc-확대--2026-11-17): 조이·헤이즐·모건 작업점 4곳과 소담 고정 작업에 재생 전 정렬을 연결했다. SO 4개·프리팹 참조 5곳·정규 월드 상속 확인, 관련 테스트 어셈블리까지 보조 C# 컴파일 오류 0. Unity 연결 종료로 확대본 회귀 실행은 미수행이며 연목의 기존 56/56을 승계하지 않는다. 실제 접촉·반복·중단/재개는 사용자 테스트 대기다.

- 2026-11-17 [NPC 접촉 전 KCC 정렬 구현](../work-status/npc-work-alignment.md): 연목 수선점의 선택적 SO·시작 모션 기반 정렬·모터 소비 후 도착 검사·원격 집기 차단을 반영했다. 원본 프리팹·재생성 입력과 정규 씬 상속 연결 확인, Unity 정상 컴파일·신규 정렬 12건 포함 관련 EditMode **56/56 통과**. 실제 손 접촉·발 미끄러짐·중단/재개·정규 월드 Play는 사용자 테스트 대기다. [설계](../../Assets/docs/design/npc/NPC_WORK_ALIGNMENT.md).

- 2026-11-17 [MotionWarping의 NPC 집기 적용 검토](../work-status/kinemation-motion-warping-investigation.md): 기존 NpcRoutine/WorkPoint 예약과 접촉 프레임 소품 인계를 재사용하고 KCC 정렬·손 접촉 보정을 보완하는 안이다. 현재 RoutineState에는 워프 이동 소비 경로가 없어 이벤트 추가만으로 적용되지 않는다. 정적 검토이며 신규 구현·실제 모델 검증은 미수행이다.

2026-11-15 사진 재제보 정정: 이전 식탁 8개 보정은 제보 화면의 약초 작업대를 고친 것이 아니었다. 실제 헤이즐·소담 작업대를 식별해 충돌체보다 약 30cm 낮던 시각 모델을 원래 작업면에 맞추고, 납작한 항아리 세트를 병·단지·약초 그릇으로 복구했다. 네 소품의 실제 작업면 접촉·저장·근접 화면 확인. 실제 NPC 작업 재생·최종 미술은 별도 확인 대기. [실제 대상·원인·전후 근거](../work-status/npc-daily-animation/LAKE_ACTIVITY_FIX.md#사용자-스크린샷-재제보--약초-작업대-실제-대상-수정).

2026-11-15 추가 지적 반영: 두 피난민만 다룬 범위를 수정해 호수·가뭄 마을·항구·성채 NPC 83명(50/9/10/14)을 전수 조사·적용했다. 58명 행동 보완/25명 기존 행동 유지, 유효한 보행·작업·일과가 모두 없는 설정 0명. 실제 활성 37명 표본에서 23명 위치 변화와 상점 장부 정리를 확인했다. 컴파일·관련 EditMode 20/20 통과. 다른 지역·후반 이야기·저장/대화·최종 미술은 사용자 테스트 대기다. [전수 변경·검사·WA01~04](../work-status/npc-daily-animation/LAKE_ACTIVITY_FIX.md#사용자-지적에-따른-전수-확대).

2026-11-15 LakeVillageWorld 제보 수정: 식탁 교체 후 남은 소품 8개의 실제 메시 상판 접촉 보정, 피난민 2명의 짐 정리·확인·짧은 휴식 일과를 저장했다. 첫 Play 확인에서 작업 소품 충돌을 발견해 후속 보정했다. 저장본 참조 검사와 두 주민의 실제 작업 재생·상판 화면을 확인했다. 전체 정상 왕복·접촉·중단은 사용자 테스트 대기이며 품질 완료로 판정하지 않는다. 별도 ActorDatabase 지역 배치 시간 초과도 남는다. [변경·근거·확인 절차](../work-status/npc-daily-animation/LAKE_ACTIVITY_FIX.md).

- 2026-11-15 모건 화물 점검 재보정: 실제 양손·자루 표면 기준으로 두 작업점을 조정하고 별도 클립 연결. 각 321표본에서 손바닥 중심 목표 오차 최대 1.829/1.547mm, Unity 앞·뒤·옆 표본 확인. 실제 접근·중단과 사용자 미술 인수는 별도. [현재 결과·근거·M01~M03](../work-status/npc-daily-animation/MORGAN_CONTACT_REVISION.md).

| 항목 | 확인된 기록 | 남은 작업 |
|---|---|---|
| 일상 애니메이션 재작업 | 2026-11-15: 주민 50명 인사·제스처 100개 제거, 휴식·교류·공용 슬롯·생성 입력 수정. CareVisit/MealServe 간접 금지 동작도 제거. 실제 연결 18 MotionSet의 금지 클립 0. R3 수선 3구간·실물 접촉 인계·화면 밖 포즈 갱신·씬/원본/재생성 반영. EditMode 61/61 | 실제 정상 71프레임, pause/risk 중단·두 재개 129프레임 확보. 실제 UI·저장·군중·최종 미술은 사용자 테스트 대기. 다른 직업 전체 접촉·전환 검수와 확대는 미완료. [상태](../work-status/npc-daily-animation/STATUS.md), [V01~V08](../work-status/npc-daily-animation/USER_TESTS.md) |
| 생활 AI | STATUS의 2026-11-07 재개 기록: 대표 콘텐츠·세 게이트 활성화·씬 저장, EditMode 58/58 및 기존 생활 행동 PlayMode 12/12 | 사용자 접점·반복·길막·새 교류·자연 환경·실제 저장 왕복·성능·음향 확인. 전체 생활 AI 완료 아님 |
| 일상 애니메이션 R2 | 2026-11-11: Unity 클립 20,819개(GUID/local ID, 보조 이름 9,666 포함) 조사. 연목 실제 몸·의상 기준 Blender 보정 3구간, 독립 MotionSet/망치/작업대, 월드 실행·재생성 연결 반영. 실제 접근→수선→종료 108프레임 및 3회 반복 프리뷰 확보 | **NO-SHIP**. 도구 인계·잔여 접촉·하체/식생 간섭·중단 화면 검수 미완료. 신규 전용 행동 제작 0, 다른 직업 확대 미수행. 사용자 테스트 대기 |
| P09 외형 | 검수 보고(2026-11-13 output 정리로 삭제) | 보고 시점 288개 MotionSet 연결 누락. 외형 설정 대조 통과를 런타임 사용 승인으로 바꾸지 않음. 수정 여부는 현재 프리팹 재조회 |
| 외형 개선 | 작업 기록(2026-11-13 output 정리로 삭제) | 현재 프리팹·NPC 외형 저작 가이드를 기준으로 재조회. 후속 외형 검수 결과와 대조 |
| 월드/NPC 대사 Bake | SOURCE_AUDIT와 `Tools/NpcWorldHarness/` | discover 성공만으로 제작 가능하지 않음. canonical 세계관 사실·NPC 지식/비밀·Voice/이미지 매핑 등 자료 미확정으로 `BLOCKED_MISSING_SOURCE`. 후보 검사와 정식 배포 승인은 별도 |

NPC 대사 원문·수동 잠금·승인 결과와 구매/생성 모델 원본은 정리 대상 캐시로 취급하지 않는다.

분야별 설계·상태·검증 문서 목록은 [설계 문서 분류](design-folder-map.md)에서 `NPC` 행을 본다. 가이드는 제외한다.

[위키 시작](README.md)
