# 개발과 검증

## 정본

- [프로젝트 온보딩](../../Assets/docs/onboarding/PROJECT_ONBOARDING_GUIDE.html), [asmdef 온보딩](../../Assets/docs/onboarding/ASMDEF_MODULARIZATION_ONBOARDING.html)
- [코드 작성 지침](../../Assets/docs/guide/CODE_AUTHORING_GUIDE.md), [테스트 운영](../../Assets/docs/guide/TEST_VALIDATION_GUIDE.md)
- [카메라 이식 경계](../../Assets/docs/guide/CAMERA_MODULE_PORTABILITY_GUIDE.md)
- [공통 하네스](../../Tools/Harness/README.md), [기존 검사 실행기](../../Tools/README.md)

## 반영된 하네스

명시 파일·meta 기반 검증 준비표, 입력 해시 비교, NUnit XML 실제 건수·집계·Quick fixture 대조, 자동 테스트 단계별 결과 JSON을 추가했다. 하네스 자체 검사 20건은 합성 XML과 대체 프로세스로 실행한 도구 검사다. Unity 게임 테스트 20건 통과를 뜻하지 않는다.

준비표의 경로 분류는 영향 분석 제안이다. 관련 소비자·연결 에셋과 최종 diff를 Agent가 보완한다. 사용자 플레이와 아트 인수는 별도 기록하며 필수 확인이 남으면 완료로 보고하지 않는다. 별도 사용자 지시 없이 Player·Addressables 검증 빌드는 실행하지 않는다.

## 단계별 위키 인계

[하네스 단계 완료 절차](../../Tools/Harness/README.md)에 따라 계획한 각 단계가 끝나면 분야 위키에 결과·정본·검증 근거·남은 확인을 갱신하고, 링크 확인 후 다음 단계로 진행한다. 단계 인계와 최종 보고에는 갱신한 위키 링크를 포함한다. 준비표의 `review.md`에도 단계별 연결 칸을 추가했다. 자동 단계 감지나 자동 위키 작성 기능은 아니다.

이 절차와 준비표 반영 후 기존 하네스 자체 검사 20건 통과 및 생성된 위키 연결 칸을 확인했다. Unity 게임 검증은 해당하지 않아 미수행이다. 지식 변화가 없는 단순 서식 단계는 이력을 늘리지 않고 갱신 불필요 사유를 보고한다.

## 단계별 부산물 정리

공통 하네스에 단계 종료 시 작업 부산물 정리 절차와 준비표 기록 칸을 연결했다. 검증 산출물의 정본 규칙은 테스트 작업 지침에 유지하고, 일반 코드·데이터·아트·문서 작업에도 이번 단계의 불필요한 임시 자료만 정리하도록 적용한다. 사용자 변경 백업·구매/MCP 원본·최종 산출물과 meta·검증 근거·다음 단계 입력은 보존한다. 삭제/보존/미정리 경로와 이유를 기록하고 정리 후 위키 근거 링크를 확인한다. 자동 삭제는 수행하지 않는다.

문서와 준비표 문구 변경으로 PowerShell 구문·준비표 생성·관련 링크를 확인했다. 생성 확인용 준비표는 확인 후 제거했으며 Unity 검증은 미수행이다.

## 2026-11-11 하네스·위키 재검토

- `Prepare-Task.ps1 -CheckPlan`이 빈 입력 목록을 정상 비교로 처리하던 문제를 수정했다. 빈 목록과 잘못된 경로·해시 형식을 거절한다. 해시 비교는 여전히 전체 영향 분석이나 테스트 통과 증명이 아니다.
- 자체 검사의 합성 준비표가 실행마다 남던 문제를 수정했다. 정상 종료 시 이번 실행의 준비표 두 파일과 빈 폴더만 정리한다. 실패로 중단된 자료는 진단을 위해 남을 수 있다.
- `Test-Harness.ps1` 자체 검사 21건 통과(합성 XML·대체 프로세스, Unity 미실행). 위키·하네스 README·docs README의 로컬 링크 108건 대상 존재 확인. 링크 대상의 모든 구현·과거 검증 결과를 재검증한 것은 아니다.
- NPC 위키에 루트 지침의 일상 애니메이션 저작 가이드 연결을 보완했다. 이번 최초 검사에서 생성된 합성 준비표도 제거했으며 기존 사용자 산출물은 보존했다.

### 남은 검증 및 기존 문제

- 루트 지침의 기존 카메라 Phase 7 스모크 미완료 기록은 그대로 유효하다. 위키 정리로 완료 처리하지 않는다.
- 선택 검증에서 검증기가 반환한 에셋 경로 없는 오류가 누락될 수 있다. `EditorValidationRegistry.Run`이 직접 잡은 예외는 이미 보존된다. 재현·Unity 회귀 검사를 붙이는 별도 수정 대상이다.
- 합성 하네스 실행 자료는 정리했다. 자체 검사는 `Tools/Harness/Test-Harness.ps1`로 다시 만들 수 있다.

## 2026-11-12 문서 역할과 폴더 분리

설계와 지침의 기존 폴더 분리를 유지한다. 설계에 따른 작업의 상세 진행·검증·남은 항목은 [작업 상태 폴더](../work-status/README.md)에 별도 문서로 관리하고 설계 정본과 양방향으로 연결한다. 분야 위키에는 현재 판단 요약과 상태 문서 링크를 남긴다.

[공통 하네스](../../Tools/Harness/README.md), AGENTS/CLAUDE, 문서 진입점에 같은 기준을 반영했다. 기존 혼합 문서는 일괄 이관하지 않고 해당 문서를 수정할 때 분리한다. 문서 diff 및 공백 오류 검사와 변경 문서의 로컬 링크 26 건 대상 존재 확인을 마쳤다. 런타임 변경이 없어 Unity 검증은 미수행이며 임시 산출물은 없다.

## Unity Hot Reload

[설계 원문](../전달용/Unity_HotReload_Agent_Handoff/DESIGN_SPEC.md) · [작업 상태](../work-status/unity-hot-reload.md) · [ADR 0001](../work-status/unity-hot-reload/adr/0001-scope-and-backend.md) · [ADR 0002](../work-status/unity-hot-reload/adr/0002-backend-proof.md) · [progress.json](../work-status/unity-hot-reload/progress.json)

**판정 `IMPLEMENTED_UNVERIFIED` (2026-11-12).** Editor 전용 패키지 `Packages/com.uplayground.hotreload`와 외부 Worker `Tools/HotReload.Worker`를 구현했다. Bee rsp로 Unity 컴파일을 재현하고 Cecil IR을 원본 멤버로 재연결해 Harmony(Thin 2.3.6) transpiler로 본문만 교체한다. 실제 Unity에서 G1 백엔드 증명, IL·거부 매트릭스, 트랜잭션·롤백·격리, Bridge, 실제 Actor/Camera 대상 Edit Mode smoke를 자동 검증했다(필수 45개 중 PASS 35·FAIL 1·부분 6·미수행 3; 최근 전체 실행 48건 중 47 PASS, T31 Play 20회 정리 검증만 시간 초과로 FAIL·원인 미규명). 병합본 Harmony의 Burst 해싱 오류와 MonoMod net452의 CodeDom 충돌을 재현·해결했다. Player 빌드·도구 제거·clean checkout·60분 soak·Play 중 실제 관찰은 남아 있으며, 사용자 테스트 명세는 상태 문서에 있다. 툴 런처 디버그 › Project Hot Reload로 연다.

[위키 시작](README.md)
