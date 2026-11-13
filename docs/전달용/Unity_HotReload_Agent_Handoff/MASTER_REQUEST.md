# Project Hot Reload — 전체 수행 요청문

이 문서를 설계 참고용으로만 읽고 끝내지 말고, 현재 Unity 저장소에 실제로 동작하는 Project Hot Reload v1을 구현해라.

[작업 목표]
Play Mode에서 기존 객체/필드/참조를 유지한 채, 사전 허용된 기존 메서드 본문 변경을 반영하는 Editor 전용 개발 도구를 완성한다. 공개 동작을 참고한 독립 구현이며 상용 Hot Reload 비공개 코드·서버·인증 로직의 추출/복제는 하지 않는다.

[먼저 읽을 것]
1. 현재 저장소의 AGENTS.md, CLAUDE.md, 기타 프로젝트 지침.
2. 전달한 Unity_HotReload_Design_and_Agent_Handoff.html 또는 DESIGN_SPEC.md 전체.
3. 이미 존재하면 Docs/HotReload/progress.json, Inventory.md, adr/, evidence/.
서로 충돌하면 기존 저장소의 안전 규칙을 우선하고 기술적 차이는 ADR에 기록해라. 위 문서가 저장소에 없다면 실제 전달된 문서를 읽고 Docs/HotReload에 배치하되 기존 파일을 덮어쓰지 마라.

[실행 방식]
G0부터 G12까지 순서대로 구현하고 검증해라. 간단한 불명확성은 실제 저장소를 조사하고 가장 보수적인 선택을 기록하며 진행해라. 매 단계마다 계속할지 묻거나 계획만 제시하고 멈추지 마라. 다만 선행 안전 gate가 실패하면 그 문제를 먼저 해결하며, 해결되지 않은 백엔드 위에 나머지를 완료했다고 표시하지 마라.
사용자 미커밋 변경은 보존한다. reset/stash/대규모 파일 삭제/임의 엔진 업그레이드/전역 설정 변경/자동 commit·push는 하지 마라. 필요 시 격리된 fixture 프로젝트 또는 worktree를 사용하라. 테스트용 fixture에서의 정상 컴파일·Play 진입/종료는 작업 범위에 포함한다. 기존 사용자의 진행 중 게임 세션 종료나 저장되지 않은 scene 폐기는 승인 없이 하지 마라.

[필수 기술 계약]
- Worker가 Unity의 실제 source/define/reference/compiler option을 반영해 변경 어셈블리 전체를 컴파일한다. 변경 .cs 하나만 독립 컴파일하는 방식으로 마감하지 마라.
- A0(실제 로드 원본), W0(Worker baseline), Eg(현재 실행 세대), Sr(소스 snapshot)를 구분한다.
- 새 게임 assembly를 Editor에 Assembly.Load하지 않는다. Cecil 등으로 IL을 읽고 원본 Type/FieldInfo/MethodInfo에 의미 기반으로 재연결한다.
- G1에서 실제 Unity의 private/static 상태, 기존 caller/delegate, Vector3 반환, 반복 교체 및 원복을 먼저 증명한다.
- 검증된 Harmony 버전으로 메서드 본문을 교체한다. 직접 unsafe 함수 포인터 덮어쓰기는 도입하지 마라.
- allowlist + 사전 Editor 한정 NoInlining + 메인 스레드 대상만 v1에서 허용한다.
- v1 지원 매트릭스의 필수 항목과 모든 거부 조건을 구현한다. EH, locals, labels, generic operand, ref/out API 호출을 TODO로 남기지 마라.
- prepare/commit/restore, 소유 owner, 단조 증가 세대, stale revision 방지, 부분 실패 전체 원복, Quarantined를 구현한다.
- 롤백은 코드만 복원한다. 게임 월드 상태나 디스크 소스를 복원한다고 표현하지 마라.
- RefreshLease는 소유권과 획득/해제를 정확히 관리한다. 수동 Refresh를 차단할 수 있다고 가정하지 마라.
- 여러 파일을 수정하는 에이전트 작업에는 begin/end edit batch를 사용한다.
- UI와 로컬 AgentBridge가 같은 coordinator를 호출하게 한다. 미존재 MCP 도구를 가정하지 마라.
- 도구/Harmony/Worker/fixture가 Player 빌드에 포함되지 않아야 한다.

[구현 품질]
실제 사용 경로의 빈 구현, NotImplementedException, TODO, 가짜 성공 반환, reflection 호출로 동작을 흉내 내는 경로를 남기고 완료 처리하지 마라. 무조건 예외를 무시하거나 모든 필드를 public으로 바꾸는 우회를 하지 마라. 코드 구조와 주석은 단순하게 유지하고 새 기능은 필요한 모듈에만 추가해라.

[검증]
각 단계에 테스트를 작성하고 실제 실행해라. Worker 단위 테스트와 Unity EditMode/PlayMode는 별개다. T01~T45 및 P01~P03을 추적하고, 실패는 수정 후 재실행해라. 테스트 XML/JSON·Editor.log·module/source hash·성능 CSV·빌드 의존성 검사 결과를 evidence에 남겨라. 60분 장기 시험을 실제 수행하지 않았다면 NOT_RUN이다. 테스트 명령의 성공 종료 코드만 보지 말고 결과 파일과 실제 assertion을 확인해라.

[진행 보고와 중단 복구]
큰 단계가 끝날 때 구현한 것/검증한 것/막힌 것을 짧게 보고해라. Docs/HotReload/progress.json과 evidence/index.json을 갱신해 다른 에이전트가 이어받을 수 있게 해라. 실행 환경·권한·컨텍스트 한도 때문에 멈추면 수행 가능한 독립 작업은 완료하고, 마지막 검증 gate와 실패 명령·원인·정확한 다음 행동을 기록해라. 비동기 작업을 약속하거나 실행하지 않은 테스트를 통과로 적지 마라.

[최종 출력]
FinalReport.md에 지원 범위/실제 Unity·OS·의존성 버전/핵심 변경 파일/테스트 통과·실패·미실행/성능 실측/실제 Player 빌드 결과/잔여 위험/사용법을 정리해라. G0~G12 필수 gate가 모두 실제 통과한 경우에만 VERIFIED_V1으로 선언한다. 그렇지 않으면 IMPLEMENTED_UNVERIFIED, FAILED_VALIDATION, BACKEND_BLOCKED 중 실제 상황에 맞게 보고해라.

지금은 G0 환경 조사부터 시작해라.
