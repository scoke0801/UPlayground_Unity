# 독립 검수 요청문

Project Hot Reload v1 구현을 독립 검수해라. 우선 read-only로 조사하고 구현 에이전트와 동시에 같은 파일을 수정하지 마라.

설계서 전체, 현재 저장소 지침, git diff, progress.json, ADR, 증거 인덱스, 실제 테스트/빌드 결과를 읽어라. 설명 문장보다 실제 코드 경로와 결과 파일을 우선한다.

특히 확인할 것:
- 새 게임 assembly를 로드하거나 새 객체로 교체해 타입 정체성 문제를 숨기지 않았는가?
- 원본 private/static/상속/generic operand/local/branch/EH 재연결이 정확한가?
- NoInlining의 사전 컴파일과 현재 caller/delegate/Unity callback 동작을 실제로 확인했는가?
- A0/W0/Eg/Sr를 구분하고 소스 원복 시 RemoveOverride를 수행하는가?
- stale revision/이전 session/외부 owner/partial failure를 차단하고 전체 복구 또는 Quarantined로 가는가?
- code rollback을 source/world-state rollback과 혼동하지 않는가?
- RefreshLease와 이벤트 구독이 Domain Reload ON/OFF·예외·종료에서 정확히 정리되는가?
- Worker host BCL, source generator, ILPP 차이를 무시하지 않는가?
- unsupported 코드를 실수로 허용하거나 오류를 성공으로 반환하지 않는가?
- Player 산출물에 도구/Harmony/Worker/fixture가 포함되지 않는가?
- 45개 필수 테스트와 3개 실제 기능 smoke, 장기 성능 시험, 정상 빌드의 증거가 실재하는가?

결과는 심각도 순으로 작성해라. 각 결함에 파일:행, 재현 절차, 기대/실제, 위험, 권장 수정, 필요한 회귀 테스트를 붙인다. 실행하지 못한 항목과 아직 증거가 부족한 항목을 분리한다. 근거 없이 전체 승인하지 마라.

수정 권한을 받은 뒤에만 코드 변경을 수행하고, 변경 후 관련 테스트를 재실행한다. 필수 gate 미충족이면 VERIFIED_V1 판정을 거부하고 정확한 미충족 목록을 제출해라.
