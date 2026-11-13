# 중단 후 재개 요청문

현재 Unity 저장소의 Project Hot Reload v1 작업을 이어서 진행해라.

먼저 기존 저장소 지침, Docs/HotReload/DESIGN_SPEC.md(또는 전달된 HTML), progress.json, Inventory.md, ADR, evidence/index.json을 읽어라. 대화 기억이나 이전 에이전트의 “완료” 표현보다 실제 파일과 테스트 증거를 우선한다.

현재 git diff와 사용자 변경, 실행 중 Editor 버전/PID, module MVID, source/context hash, Worker profile을 다시 확인해라. 환경이나 소스가 달라졌으면 관련 증거의 적용 범위를 재평가하고 필요한 gate를 재실행한다. 이전 세션의 JIT/patch handle을 디스크에서 복구해 재사용하지 마라.

진행 순서:
1. 마지막으로 실제 PASS한 gate와 첫 미완료/실패 gate를 확인한다.
2. 실패의 최소 재현과 blocker를 먼저 해결한다.
3. 다음 gate부터 문서의 구현→컴파일→테스트→수정→재실행 순서를 계속 수행한다.
4. 필수 테스트가 NOT_RUN이면 실행하거나 정확한 환경 blocker를 남긴다. 이를 PASS로 바꾸지 않는다.
5. 각 gate의 변경 파일·실행 명령·실제 결과·증거를 기록하고 progress.json을 갱신한다.

사용자 변경은 되돌리지 않는다. 엔진 업그레이드나 대규모 구조 변경으로 문제를 회피하지 않는다. 이전 작업을 무조건 처음부터 다시 만들거나 계획만 재작성하지 마라. 필요한 검증을 재수행하고 G12 완료 기준까지 진행해라. 실행이 불가능한 항목은 구현 상태와 검증 상태를 분리해 보고해라.
