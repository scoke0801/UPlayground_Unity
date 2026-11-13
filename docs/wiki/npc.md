# NPC 생활과 제작

## 정본과 연결 지점

- [생활 AI 현재 상태](../work-status/npc-life-ai/STATUS.md), [NPC 외형 저작](../../Assets/docs/guide/NPC_APPEARANCE_AUTHORING_GUIDE.md)
- [일상 애니메이션 저작](../../Assets/docs/guide/NPC_DAILY_ANIMATION_AUTHORING_GUIDE.md)- [NPC 일상 애니메이션 저작](../../Assets/docs/guide/NPC_DAILY_ANIMATION_AUTHORING_GUIDE.md): 기존 클립 재사용과 실제 접지·접촉·반복·중단 복구 검수 기준.
- [월드·NPC 설계](../world_event_npc_system_design.html), [대사 Bake 설계](../NPC_Dialogue_Bake_Harness_Design.html)
- Bake의 [계약](../dialogue-bake/CONTRACT.md), [파이프라인](../dialogue-bake/PIPELINE.md), [검수](../dialogue-bake/REVIEW.md), [실제 연결 조사](../dialogue-bake/SOURCE_AUDIT.md)

설계 예시를 정식 세계관 사실이나 구현 완료로 해석하지 않는다. 기존 시간·저장·NpcBrain/NpcRoutine·시뮬레이션 소유권을 먼저 재사용한다.

## 진행과 차단 항목

| 항목 | 확인된 기록 | 남은 작업 |
|---|---|---|
| 생활 AI | STATUS의 2026-11-07 재개 기록: 대표 콘텐츠·세 게이트 활성화·씬 저장, EditMode 58/58 및 기존 생활 행동 PlayMode 12/12 | 사용자 접점·반복·길막·새 교류·자연 환경·실제 저장 왕복·성능·음향 확인. 전체 생활 AI 완료 아님 |
| 일상 애니메이션 R2 | 2026-11-11: Unity 클립 20,819개(GUID/local ID, 보조 이름 9,666 포함) 조사. 연목 실제 몸·의상 기준 Blender 보정 3구간, 독립 MotionSet/망치/작업대, 월드 실행·재생성 연결 반영. 실제 접근→수선→종료 108프레임 및 3회 반복 프리뷰 확보 | **NO-SHIP**. 도구 인계·잔여 접촉·하체/식생 간섭·중단 화면 검수 미완료. 신규 전용 행동 제작 0, 다른 직업 확대 미수행. 사용자 테스트 대기 |
| P09 외형 | 검수 보고(2026-11-13 output 정리로 삭제) | 보고 시점 288개 MotionSet 연결 누락. 외형 설정 대조 통과를 런타임 사용 승인으로 바꾸지 않음. 수정 여부는 현재 프리팹 재조회 |
| 외형 개선 | 작업 기록(2026-11-13 output 정리로 삭제) | 현재 프리팹·NPC 외형 저작 가이드를 기준으로 재조회. 후속 외형 검수 결과와 대조 |
| 월드/NPC 대사 Bake | SOURCE_AUDIT와 `Tools/NpcWorldHarness/` | discover 성공만으로 제작 가능하지 않음. canonical 세계관 사실·NPC 지식/비밀·Voice/이미지 매핑 등 자료 미확정으로 `BLOCKED_MISSING_SOURCE`. 후보 검사와 정식 배포 승인은 별도 |

NPC 대사 원문·수동 잠금·승인 결과와 구매/생성 모델 원본은 정리 대상 캐시로 취급하지 않는다.

분야별 설계·상태·검증 문서 목록은 [설계 문서 분류](design-folder-map.md)에서 `NPC` 행을 본다. 가이드는 제외한다.

[위키 시작](README.md)
