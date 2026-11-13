# 단계별 요청문

공통: DESIGN_SPEC.md와 기존 저장소 지침을 읽고 선행 gate 증거를 확인한다.

## G0 — 저장소·실행 환경 조사와 기준 기록

G0을 실행해라. 먼저 현재 저장소의 기존 지침과 미커밋 변경을 확인하고, Unity/패키지/asmdef/컴파일러/OS/.NET/테스트 도구를 실제 파일과 실행 결과로 조사해라. 문서에 언급된 과거 엔진 버전을 하드코딩하지 마라. Inventory, CompatibilityMatrix, RepositoryMap, 초기 progress.json과 G0 증거를 작성해라. 구현 후보는 기존 이동·카메라·전투에서 실제 메서드 이름으로 선택하되 아직 게임 코드를 대규모 변경하지 마라. G0 통과 후 G1 최소 fixture 검증으로 이어가라.

**통과 조건:** 현재 Unity/OS와 컴파일 컨텍스트 경로가 증거로 확인되고, 원본 프로젝트 변경을 보존한 채 fixture 실행 경로가 정해져야 한다. Unity를 실행할 수 없으면 그 사실을 기록하고 G1 실행을 NOT_RUN으로 남긴다.

**산출물:** Inventory.md, CompatibilityMatrix.md, RepositoryMap.md, progress.json, evidence/G0/environment.json, adr/0001-scope-and-backend.md

## G1 — 타입 보존 패치의 기술적 성립 검증

G1 최소 기술 검증을 실제 Unity에서 구현·실행해라. 원본 private/static 상태를 유지하면서 외부 컴파일 결과의 IL을 원본 멤버로 재연결하고 Harmony로 교체해라. 새 게임 어셈블리를 로드하거나 기존 객체를 새 타입으로 교체하지 마라. 기존 delegate/Unity Update와 Vector3 반환, 반복 패치, 원복을 단언하고 로그를 남겨라. 이 검증이 실패하면 우선 원인을 해결하고, 성립하지 않는 백엔드 위에 큰 시스템을 쌓지 마라.

**통과 조건:** T01~T04/T08의 최소 사례와 원복이 실제 Unity에서 PASS여야 한다. 순수 .NET 실행, reflection으로 새 객체 실행, 콘솔 메시지만으로 대체 불가. 실패하면 백엔드 종속 단계는 BLOCKED이며 UI부터 완성한 것으로 처리하지 않는다.

**산출물:** 격리 fixture 코드와 실행 테스트, evidence/G1/probe-result.json, evidence/G1/Editor.log, adr/0002-backend-proof.md

## G2 — Editor 패키지와 안전한 생명주기

G2를 구현해라. 검증된 의존성을 Editor 전용으로 격리하고 Bootstrap/세션/RefreshLease/Play·compile·reload·quit 정리를 연결해라. Pause와 StopAndRecompile을 구분하고 전역 Auto Refresh 설정을 덮어쓰지 마라. Domain Reload ON/OFF와 반복 진입에서 중복 구독·lease 불균형을 실제 테스트해라. 신규 오류를 제거한 뒤 G3로 진행해라.

**통과 조건:** T31~T35 관련 초기 테스트 PASS, 신규 Console 오류 0, lease/구독/worker 중복 없음. 단순 코드 스켈레톤으로 통과 불가.

**산출물:** Editor 패키지 골격의 실제 동작 코드, 세션/lease/생명주기 테스트, 의존성 lock/hash/license 목록, evidence/G2/lifecycle-results.json

## G3 — 실제 컨텍스트 기반 Worker 컴파일러

G3을 구현해라. Unity가 사용하는 실제 source/define/reference/option으로 외부 Worker 컴파일을 재현하고 A0/W0/Eg/Sr를 별도로 관리해라. 변경 파일 하나만 독립 컴파일하는 지름길은 사용하지 마라. partial·여러 asmdef·rsp·한글 경로·컴파일 오류 위치와 baseline IL parity를 테스트해라. 재현 못 하는 generator/ILPP는 지원 불가로 명시해라.

**통과 조건:** T05/T14~T16 PASS, 컴파일 실패 시 원래 실행 코드 불변. Worker host의 BCL이 Unity 참조에 섞이지 않음.

**산출물:** CompilationContextProvider, SnapshotStore / BaselineStore, Worker compile/analyze 명령, compiler profile lock, evidence/G3/compiler-context.json

## G4 — 정규화 IL·타입 해석·변경 검증기

G4를 구현해라. 문서의 IL/MethodKey/TypeKey 계약에 따라 원본 타입과 멤버를 재연결하고 locals·labels·switch·예외 영역·닫힌 generic API·out/ref API 호출을 지원해라. v1 지원 매트릭스의 필수 기능과 거부 기능 모두에 자동 테스트를 작성해라. raw token 재사용, 임의 NOP 대체, 게임 타입 재생성으로 문제를 피하지 마라. EH/반환 ABI/private 접근을 실제 Unity로 검증해라.

**통과 조건:** T01~T16 및 T36~T41 PASS. EH 또는 타입 재연결을 TODO로 남기고 v1 완료로 표시할 수 없음. 미지원 코드가 실제로 거부되는 negative test 필수.

**산출물:** IR/protocol 모델과 schema, IlExtractor / IlNormalizer / RuntimeMemberResolver, ShapeValidator / IlValidator, Harmony body emitter, evidence/G4/il-test-results.json

## G5 — 세대 계획·트랜잭션·롤백

G5를 구현해라. A0/Eg/Sr 기준으로 전체 원하는 세대 맵을 계획하고 Prepare-Commit-Restore를 연결해라. A를 수정한 뒤 B를 수정하고 A만 소스 원본으로 되돌리는 테스트를 반드시 통과시켜라. 패치 중간 실패와 복구 실패를 주입해 전체 원복/Quarantined를 검증해라. 롤백은 코드만 되돌리고 세대 번호는 단조 증가시키며 소스 자동 재적용을 막아라.

**통과 조건:** T17~T23/T27/T28 PASS. 일부 성공을 전체 성공으로 보고하거나 게임 상태 복구라고 표현하지 않음.

**산출물:** PatchPlanner / PreparedBatch / GenerationStore, TransactionCoordinator / RecoveryJournal, Rollback 기능, evidence/G5/fault-injection-results.json

## G6 — 파일 감지·일관된 revision·배치 장벽

G6을 구현해라. watcher 이벤트를 곧바로 patch로 연결하지 말고 stable snapshot과 revision을 거치게 해라. 중복 이벤트·빠른 저장·rename·부분 쓰기·watcher overflow·Worker 지연을 테스트해라. 에이전트용 edit batch 장벽과 stale 결과 폐기를 구현하고 미완성 배치를 timeout으로 자동 적용하지 마라.

**통과 조건:** T24~T26/T28/T29/T33 PASS. 수십 번 저장해도 최종 source와 실행 본문이 일치하며 stale 적용 0회.

**산출물:** Watcher / SnapshotScheduler / RevisionGuard, BatchBarrier, stress 저장 테스트, evidence/G6/revision-results.json

## G7 — Editor UI와 에이전트 명령 연결

G7을 구현해라. UI와 AgentBridge가 같은 coordinator 명령을 사용하게 하고 상태/refresh lease/source divergence를 숨기지 마라. 파일 inbox/outbox로 실제 analyze→apply→rollback을 실행해라. duplicate request·stale session·malformed JSON·path traversal을 거부하는 테스트를 작성해라. 존재하지 않는 MCP 기능을 있는 것처럼 문서화하지 마라.

**통과 조건:** 버튼/명령이 실제 기능을 수행하고 T30 PASS. UI 성공 메시지가 아니라 generation 및 실제 메서드 관찰로 검증.

**산출물:** UI Toolkit 창과 settings provider, AgentBridge 및 protocol.command.schema.json, 실제 명령 예제, evidence/G7/bridge-results.json

## G8 — 전체 회귀·부정 테스트·결함 주입

G8에서 T01~T41의 누락 테스트를 채우고 전체 회귀를 실행해라. 실제 Unity 테스트와 Worker 테스트를 분리하고, 실패를 수정한 뒤 영향 범위 회귀를 다시 실행해라. mock 결과나 컴파일 성공만으로 런타임 patch PASS를 만들지 마라. 각 테스트의 환경·소스/모듈 hash·실제 값·로그 경로를 남겨라.

**통과 조건:** 필수 positive/negative 테스트 전체 PASS. SKIPPED/NOT_RUN을 성공 집계에 넣지 않음.

**산출물:** 완성된 test suites, 테스트 실행 스크립트, evidence/G8/test-results.xml, evidence/G8/coverage-matrix.json

## G9 — 실제 액션 게임 코드에 제한 적용

G9를 실제 프로젝트 코드에 수행해라. 현재 저장소의 이동·카메라·전투에서 지원 가능한 메서드 3개를 선택하고 최소한의 Editor NoInlining/allowlist만 추가해라. 각 기능의 새 규칙이 반영되면서 기존 객체·상태·Animancer 참조가 유지되는지 실제 Play Mode로 확인해라. 패치 후 정상 컴파일 결과도 같아야 한다. 확인하지 않은 클래스명이나 smoke 결과를 만들지 마라.

**통과 조건:** 3개 실제 기능에서 패치와 원복, 정상 컴파일 후 동작 일치가 확인되어야 함. demo fixture만으로 대체 불가.

**산출물:** 실제 프로젝트 allowlist, 최소 annotation diff, evidence/G9/P01~P03 결과, ProjectUsageGuide.md

## G10 — 성능·장기 사용·리소스 한도 검증

G10을 실행해라. 문서의 성능 목표를 실측값으로 채우고 60분/100회 패치/20회 Play 전환 검증을 수행해라. 실제로 기다리며 측정하지 않은 장기 테스트를 PASS로 기록하지 마라. 캐시/handle/동적 코드 누적을 계측하고 세대 한도 보호 동작을 시험해라. 목표 미달은 원자료와 원인으로 보고하고 근거 없는 속도 주장을 하지 마라.

**통과 조건:** 안전 한도와 누수 방지 테스트 PASS, 성능 원자료 존재. 목표 미달은 수치/원인을 공개하고 수용 기준 변경은 ADR로 명시. 실행 시간을 실제로 확보하지 못했으면 NOT_RUN.

**산출물:** Profiler markers, evidence/G10/performance.csv, evidence/G10/soak-results.json, PerformanceReport.md

## G11 — 정상 빌드·제거 가능성·배포 문서

G11을 실행해라. 패치 상태가 아닌 원본 소스의 정상 컴파일과 실제 Player 빌드를 수행하고 배포물에 개발 도구 의존성이 없는지 검사해라. 패키지 제거 후에도 게임이 컴파일되어야 한다. clean checkout 재설치와 버전/해시/라이선스/사용법/복구 문서를 마감해라. SDK가 없어 빌드를 못 했다면 정확히 NOT_RUN과 필요한 환경을 남겨라.

**통과 조건:** T43~T45 PASS. 설치·제거·정상 빌드를 실제로 하지 못했으면 VERIFIED_V1 아님.

**산출물:** evidence/G11/build-report, evidence/G11/player-dependency-audit.json, Install/Usage/Troubleshooting 문서, 완성된 ThirdPartyNotices.md

## G12 — 독립 검수·잔여 결함 수정·최종 인계

G12 최종 검수를 수행해라. 문서의 완료 기준과 실제 diff/테스트/빌드 증거를 대조하고 TODO·가짜 성공·미구현 필수 경로를 찾아 수정해라. 수정 후 영향 테스트를 재실행해라. 실제 지원 플랫폼과 미지원 기능을 명시한 FinalReport와 progress.json을 작성해라. 모든 필수 gate가 실제로 통과한 경우에만 VERIFIED_V1을 선언해라.

**통과 조건:** G0~G11 필수 gate와 최종 체크리스트가 모두 통과해야 VERIFIED_V1. 그 외 상태는 원인을 숨기지 않고 정확히 분류.

**산출물:** FinalReport.md, 완성된 progress.json, evidence/index.json, ReviewFindings.md, ReleaseChecklist.md
