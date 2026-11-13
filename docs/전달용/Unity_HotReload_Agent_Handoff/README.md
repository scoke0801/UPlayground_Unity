# Unity Hot Reload — 설계 및 작업 인계 패키지

프로젝트 연결: [분석·작업 상태](../../work-status/unity-hot-reload.md) · [개발과 검증 위키](../../wiki/development.md). 실제 진행·검증 결과는 별도 상태 문서에서 관리한다.

작성 기준: 2026-11-12. 이 패키지는 구현 코드가 아니라 완성된 설계·작업 요청·검증 기준이다.
실제 Unity 저장소 통합, 컴파일, Play Mode 실행, 성능 측정은 이 문서 작성 과정에서 수행하지 않았다.

## 전달 순서

1. `Unity_HotReload_Design_and_Agent_Handoff.html`을 브라우저에서 연다. 외부 리소스가 필요 없다.
2. Claude 또는 Codex의 실제 프로젝트 작업 환경에 `DESIGN_SPEC.md`와 `MASTER_REQUEST.md`를 제공한다.
3. G0부터 G12까지 구현·검증한다. 단계별로 요청하려면 `PHASE_REQUESTS.md`를 사용한다.
4. 중단된 작업은 `RESUME_REQUEST.md`, 별도 검수는 `REVIEW_REQUEST.md`를 사용한다.
5. `progress.template.json`은 실제 작업 저장소에서 `Docs/HotReload/progress.json`으로 복사해 사용한다.
   기존 진행 기록이 있으면 덮어쓰지 말고 병합한다.

## 포함 파일

- HTML: 전체 설계, 도식, UI 시안, 지원 경계, G0~G12, 45개 필수 테스트와 3개 실제 smoke, 모든 요청문, 출처.
- DESIGN_SPEC.md: 같은 HTML 본문의 텍스트 버전. 별도의 축약 명세가 아니다.
- MASTER_REQUEST.md / PHASE_REQUESTS.md / RESUME_REQUEST.md / REVIEW_REQUEST.md: 실행용 요청문.
- progress.template.json: 실제 구현/테스트는 모두 미시작 상태인 진행 기록 템플릿.
- protocol.command.schema.json: 향후 구현할 로컬 에이전트 명령의 JSON Schema. 이미 존재하는 API가 아니다.

## 중요 경계

기본 범위는 Editor에서 사전 허용된 기존 메서드의 본문 교체다. 필드/타입/시그니처 변경,
async/coroutine/lambda 본문 교체, Burst/Jobs, IL2CPP Player 패치는 v1 필수 범위가 아니다.
코드 롤백은 게임 상태 복구나 소스 되돌리기가 아니다.
G1의 실제 Unity 패치 증명과 전체 필수 검증을 하지 않고 VERIFIED_V1을 선언하지 않는다.
