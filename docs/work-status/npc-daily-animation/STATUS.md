# NPC 일상 애니메이션 재작업

기준: 2026-11-15. [저작 가이드](../../../Assets/docs/guide/NPC_DAILY_ANIMATION_AUTHORING_GUIDE.md), [NPC 위키](../../wiki/npc.md).

## 현재 판정

**금지 동작 제외와 대표 수선 R3 제작·연결·관련 기술 검사는 반영했다. 전체 가이드 품질 인수는 미완료다.** 최신 관련 EditMode **61/61 통과**, 실패·제외 0, 2.121초. [성공 원본](r3-editmode-culling-results.json). 현재 Unity는 Edit Mode, 컴파일 중 아님, 정규 씬 dirty=false, Console 오류 0을 확인했다.

사용자 최종 미술 확인은 [실행 명세](USER_TESTS.md)에 따라 대기한다. 수선 외 직업의 전체 접촉·전환 제작, 실제 대화 UI, 준비/놓기 도중의 중단, 저장 왕복·군중·추가 체형 검수는 끝나지 않았다. 기술 검사를 전체 품질 완료로 계산하지 않는다.

## 1. 조사와 제외

- 외부 파일 10,490개, 클립 20,560개(보조 preview 9,666), 전체 MotionSet Base·레이어 참조 3,536건. GUID/local ID 합집합 20,760개, 로드 오류 0. [요약](inventory-summary.json), [외부 목록](external-clips.tsv), [등록 사용처](all-motion-clips.tsv), [NPC 사용처](npc-motion-clips.tsv). 조사 시점 기준 목록이며 이후 새 R3 산출물은 아래 별도 기록한다.
- 실제 일과 6개의 [연결 대장](actual-routine-ledger.tsv). [후보 대장](candidate-ledger.json)은 후보이며 적합성 판정이나 전용 행동 개수를 뜻하지 않는다.
- 정규 씬 주민 Brain 50개의 인사·대기 제스처 100개 제거, 휴식점 9곳, 일과 원본 프리팹 5개, 남녀 공용 인사 슬롯·교류·생성 입력 수정. [직접 제외 검사](exclusion-validation.txt).
- CareVisit·MealServe 내부의 허리 펴기 네 구간도 독립 Calm 버전으로 교체했다. 원본은 보존했다. 정규 씬의 실제 Brain/일과/작업점/교류에서 참조하는 MotionSet 18개를 클립까지 검사해 금지 클립 0개. [실제 연결 검사](final-connected-clip-audit.txt).
- Hazel/Joy 원본 프리팹과 정규 씬 저장본에서 이전 CareVisit·MealServe 참조 0. 프리팹 재로드에서도 Calm 연결을 확인했다. Unity가 자동 추가한 기본값 필드만 정리했으며 두 프리팹의 최종 diff는 각각 모션 참조 두 줄이다.

## 2. 대표 수선 R3

대상은 연목/P09/현재 작업복/보정 망치/현재 목재·작업대다. 다른 체형에 인수를 확대하지 않는다.

- [제작 원본](../../../ArtSource/NpcDailyAnimations/P09RepairR3/): 기존 실제 스킨·본을 보존한 Blender 보정. Blender MCP 연결 불가를 설치된 Blender 직접 실행으로 해결했다. 구매 원본·R2는 수정하지 않았다.
- 시작·반복·종료 3클립, 각각 30fps·61표본·2초. 시작 접촉 0.8초, 종료 접촉 1.2초. Humanoid 손가락 binding과 body 기준 발 IK 좌표를 보정했다. [산출물 식별](r3-artifacts.json).
- 작업점의 같은 실물 망치를 접촉 이벤트로 손에 인계하고 내려놓는다. 수선 접근 중 상자가 망치로 바뀌던 임시 소품을 제거했다. 중단 시 소유권·실물을 보존하고 몸 해제/작업점 비활성화 때 현장으로 복구한다.
- 정규 씬·YeonmokRoutine·재생성 입력에 연결했다. 도착 반경 1.5cm는 원본 일과 데이터에서 읽는다. 도구 준비 단계는 기존 미완성 상태로 유지한다.
- 카메라 밖에서 Animator가 본 갱신을 멈추는 문제를 실제 화면에서 발견했다. 실물 접촉 작업 동안 AlwaysAnimate를 적용하고 종료·중단·해제 때 이전 컬링을 복구한다. 전역 Animator 실행 코드는 변경하지 않았다.
- [재생성 2회 중복 검사](r3-regeneration.txt): 도구 1개, R3 연결, 임시 소품 없음. 실제 전체 월드를 재생성하지 않았다.

## 3. 기술·화면 검수

| 검사 | 결과·범위 | 근거 |
|---|---|---|
| 기존 NPC 검사 | 54/54 통과 | [초기 결과](editmode-results.json) |
| 실물 인계 포함 | 57/57, 이후 비활성화 보완 58/58 통과 | [57건](r3-editmode-results.json), [58건](r3-editmode-final-results.json) |
| 최종 컬링·몸 비활성화 포함 | 61/61 통과. 중간 1건 실패는 테스트 NPC에 필수 Collider가 없어 발생했고 CapsuleCollider 구성 후 재검사 | [61건 원본](r3-editmode-culling-results.json) |
| 격리 연속 재생 | 101프레임, 시작→3회 반복→종료. 인계 순간 위치 변화 0.000001m 미만 | [표본](../../../output/NpcDailyAnimations/P09Repair/R3/sequence-final/sequence.tsv), [타격](../../../output/NpcDailyAnimations/P09Repair/R3/sequence-final/frame-030.png), [종료](../../../output/NpcDailyAnimations/P09Repair/R3/sequence-final/frame-100.png) |
| 실제 정규 월드 | 71프레임/약19초. 접근·정렬·시작·반복·놓기·작업 종료 확인. 이탈 보행 전체는 이 기록 밖 | [연속 기록](../../../output/NpcDailyAnimations/P09Repair/R3/live-20261115-140109/frames.tsv), [타격](../../../output/NpcDailyAnimations/P09Repair/R3/live-20261115-140109/frame-0020.png), [놓은 뒤](../../../output/NpcDailyAnimations/P09Repair/R3/live-20261115-140109/frame-0069.png) |
| 중단·재개 | 129프레임. PauseRoutine 2초, 재개, SetRiskActive true/false, 재개. 동일 망치 instance ID 유지. 작업 AlwaysAnimate/중단 CullUpdateTransforms 확인 | [연속 기록](../../../output/NpcDailyAnimations/P09Repair/R3/interrupt-20261115-140702/frames.tsv), [결과](../../../output/NpcDailyAnimations/P09Repair/R3/interrupt-20261115-140702/status.txt) |
| 실제 대화 UI·긴 플레이·최종 아트 | 사용자 테스트 대기. API 중단은 실제 UI 검사를 대신하지 않음 | [V01~V08](USER_TESTS.md) |

화면 검수 주체는 Codex다. 실제 월드 캡처는 별도 720×720 카메라, FOV38, 위치 (985.6,64.25,430.4), 주시점 (988.4,63.82,431.8), 실제 URP·의상·작업대·일과를 사용했다. 기본 플레이 카메라 품질 확인과 구분한다. 플레이어만 KCC로 인근 이동했으며 NPC 작업 시간·예약·접근 이동을 주입하지 않았다. 중단 기록은 공개 pause/risk API를 의도적으로 호출했다.

초기 0프레임 기록은 플레이어와의 거리로 인한 Suspended/대기 시간 부족을 품질 실패로 혼동한 관찰이다. 이후 73프레임 기록에서는 화면 밖 컬링 결함을 발견했으며 최신 품질 근거로 사용하지 않는다. 현재 근거는 위 71프레임과 129프레임이다.

## 4. 남은 범위

모건 화물은 [실제 접촉 재보정과 M01~M03](MORGAN_CONTACT_REVISION.md)을 따른다. 두 작업점·독립 클립·원본 프리팹·씬 연결 반영, 각 321표본 손바닥 중심 오차 최대 1.829/1.547mm와 앞·뒤·옆 격리 화면 확인. 실제 일과 관찰의 작업 표본은 0개이므로 전체 접근·중단과 사용자 미술 인수는 대기다.

| 배치 | 현재 결과 | 남은 작업 |
|---|---|---|
| 연목 수선 | 신규 접촉 3구간·실물 인계·실행 연결, 실제 정상/중단 재생 확인 | 실제 대화 UI, 준비/놓기 중단, 전체 이탈, 저장 왕복, 정상 속도 사용자 최종 품질 |
| Joy 요리 | 기존 클립·스푼·그릇 조합 표본 확인, MealServe 간접 금지 동작 제외 | 접근 소품 전환·집기/놓기·전체 반복/종료 검수 |
| Hazel 약재 | 기존 병·손 조합 표본 확인, CareVisit 간접 금지 동작 제외 | 운반 상자→작업 병 전환과 실제 가구 접촉·중단 검수 |
| Morgan 화물 | 기존 숙인 자세 표본 확인 | 실제 화물·가구를 포함한 접촉·작업 목적 검수 |
| 휴식·교류 | 금지 동작 연결 제거, 기존 차분 대기·대화 사용 | 실제 대화 중단·군중 반복·재진입 사용자 확인 |
| 수면·농사·추가 직업 | 후보 조사 | 역할·가구·사용처 미확정. 임의로 주민에게 배포하지 않음 |

수선 외 역할 표본은 [CurrentRoles](../../../output/NpcDailyAnimations/CurrentRoles/)에 보존했다. 격리 표본이며 실제 전체 일과 품질 인수 근거가 아니다. 가이드 4절의 배치별 검수에 따라 미완료 경로는 대량 적용에서 제외한다.

## 5. 보존과 정리

기존 미커밋 코드·에셋·구매 원본·meta·다른 작업 변경을 보존했다. 정규 씬은 Git 추적 밖이며 변경 전 백업 `output/NpcDailyAnimations/Exclusions/backup/LakeVillageWorld.before.unity`(273,555,090바이트)을 유지했다. R2 원본과 R3 최종 원본·캡처·검사 결과를 보존한다.

R3 반복 튜닝 중간 프리뷰 13폴더/44,009,658바이트를 정리했다. 최종 표본(final), 격리 연속(sequence-final), 실제 재생·중단과 실패 진단 기록은 유지했다. 요청하지 않은 커밋·Player/Addressables 빌드·전체 월드 재생성은 하지 않았다.

최종 [준비표·검수 기록](../../../output/harness/20261115-141056-58fa251b/review.md). 준비표 해시는 지정 파일의 변경 확인이며 전체 품질 통과 증거가 아니다.
