# 콘텐츠와 진행

펫 종료 수명 후속: 시작 경로 반복 측정에서 종료 중 틱 싱글톤이 null인 경우 PetManager 정리가 중단되는 오류를 수정했다. 짧은 PlayMode1/1과 실제 월드 종료에서 핸들36개 해제·정리 완료를 확인했다. [시작 경로 상태/근거](../work-status/startup-profiling.md)의 8단계를 본다. 펫 저장·동행 조작·연출 변경은 없다.

## 정본

- [콘텐츠 저작 규칙](../../Assets/docs/guide/CONTENT_SYSTEM_AUTHORING_GUIDE.md)
- [스토리 저작](../../Assets/docs/guide/STORY_PLOT_AUTHORING_GUIDE.md), [대사 어투](../../Assets/docs/guide/game_dialogue_natural_korean_guide.md)
- [생명의 호수 내러티브 개편 계획](../../Assets/docs/design/content/lake-of-life/LAKE_OF_LIFE_NARRATIVE_OVERHAUL_PLAN.md)
- [현행 개편 구현 기록](../reviews/LAKE_NARRATIVE_OVERHAUL_IMPLEMENTATION.md), [개편 대사 원고](../reviews/LAKE_NARRATIVE_OVERHAUL_DIALOGUES.txt)
- [시나리오 전체 계획](../../Assets/docs/design/content/scenario-gameplay/00_MASTER_PLAN.md), [인수와 추적](../work-status/scenario-gameplay/07_ACCEPTANCE_AND_TRACEABILITY.md)

실제 재생 문안과 진행은 Dialogue/Quest/Flow 에셋을 조회한다. 원고나 계획이 실행 데이터를 자동으로 대체하지 않는다. 이번 정리는 플롯·대사·게임 에셋을 수정하지 않았다.

## 진행 기록

몬스터 드랍 후속(2026-11-14): [드랍 밸런스 상태](../work-status/monster-drop-balance.md). 신규 36표·105프로필·108몬스터 정의 연결 반영. 일반 장비15%/정예40%/보스1개+35% 추가, 게이트 재료1~2/3~4/6~8. 실제 SO 36만 회 추첨 오류0·기존 EditMode5/5·재적용 변경0·GUID 거부/중간 실패 롤백 확인. B3 파편 대체·후반 B3 상한은 임시 정책이다. 약체·드랍 null·야생동물은 보존, 플레이 체감/획득·저장 회귀는 사용자 테스트 대기. 신규 표/meta는 Git ignore 대상이므로 전달 시 함께 보존한다.

| 항목 | 근거 | 현재 판단 |
|---|---|---|
| 호수 내러티브 개편 | 위 구현 기록 | 시작–영입–묘령–신전–귀로와 저장/중단 복구 반영 기록. 자동 입력·지정 위치 검증과 실제 패드 완주·처음 보는 플레이어 이해도는 별도 |
| 기존 스토리 배치 | [배치 수정 기록](../reviews/LAKE_STORY_PLACEMENT_FIX.md), [재적용 계획](../reviews/LAKE_STORY_PLACEMENT_PLAN.json) | 측정·재현용 근거 보존. 좌표를 현재 변경된 씬에 무조건 재적용하지 않는다 |
| Red Tree 단계별 구현 | [R03](../work-status/red-tree-blessing/LAKE_OF_LIFE_RED_TREE_R03_IMPLEMENTATION.md), [R09](../work-status/red-tree-blessing/LAKE_OF_LIFE_RED_TREE_R09_IMPLEMENTATION.md) | 단계별 이력이다. 후속 개편과 현재 그래프를 먼저 대조하고 과거 시나리오를 최신으로 복구하지 않는다 |
| 아이템 경제(드랍·제작·성장) | [설계](../../Assets/docs/design/content/ITEM_ECONOMY_BALANCE_DESIGN.md), [수치 합격 보고](../work-status/item-economy-acceptance.md), [단계 이력](../work-status/item-economy.md) | 자동 기여도 108/108·기준 TTK 36/36·EditMode 30/30 통과. 활성 스탯 4종 일치. B1~B3 실제 103종, B4~B5 수치 제안. 공용 몬스터 HP 원본 보정/146액터 재베이크. 전체 드랍·제작·판매·스킨 구현은 미완료, 20분 플레이 사용자 테스트 대기. 빌드 없음 |
| 사용자 직접 테스트 피드백 | P04~P06 사용자 테스트 피드백(원문은 2026-11-13 output 정리로 삭제) | 반영 결과는 현재 시나리오 그래프·씬이 정본이다. 사용자 결과와 AI 자동 검사는 구분한다 |

2026-08-28 대사 추출본은 개편 계획에서 이미 현행 원고가 아니라고 명시했다. [역사 자료](../_archive/story/STORY_DLG_DIALOGUES.txt)로 분리했다. 구 수정 보고서와 당시 XML은 회귀 이력이며 현행 문안이나 현재 변경본 통과 증명이 아니다.

## 과거 사이클 안내 정리

콘텐츠·플롯 저작 지침과 온보딩에서 제거된 사이클 런·BossAssist 정산을 현행 구조로 안내하던 부분을 정리했다. 저장·지역 재진입·새 게임 경계는 실제 콘텐츠 정책을 확인하며, 일반 재방문에 일괄 리셋을 적용하지 않는다. 성장 정본은 [캐릭터 성장 가이드](../../Assets/docs/guide/CHARACTER_GROWTH_SYSTEM_GUIDE.md)다.

기존 콘텐츠 종합도는 [현행 문서 안내](../../Assets/docs/CONTENT_LINES_OVERVIEW.html)로 교체하고 원문은 역사 보관했다. 영입 장애의 과거 로그, 라이프사이클·보행 사이클·그래프 순환 같은 일반 용어는 유지한다. 이 문서 정리는 실제 퀘스트·저장·영입 동작을 변경하거나 재검증한 작업이 아니다. [정리 근거](cleanup.md)를 확인한다.

분야별 설계·상태·검증 문서 목록은 [설계 문서 분류](design-folder-map.md)에서 `콘텐츠` 행을 본다. 가이드는 제외한다.

[위키 시작](README.md)
