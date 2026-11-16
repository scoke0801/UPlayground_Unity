# QA 결과 접수 스킬

2026-11-15. 상태: 지침 보강·스킬 작성 완료, 하네스 정적 검사 수행. 실제 버그 제보에 적용한 사례는 아직 없다.

## 정본

- 규칙: [테스트 작업 지침 「명세 밖 결과 접수: 버그 제보·플레이 소감」](../../Assets/docs/guide/TEST_VALIDATION_GUIDE.md)
- 절차 진입점: [qa-intake SKILL.md](../../.claude/skills/qa-intake/SKILL.md)

## 조사와 판단

웹에서 찾은 3D 게임 QA 스킬·도구를 기존 자산(Unity MCP, Hot Reload, qa-reviewer 역할, 테스트 지침의 AI→사용자 명세 형식)과 대조했다. 기존 지침에는 AI가 사용자에게 주는 명세와 회신 형식은 있었지만, 사용자가 명세 없이 보낸 버그 제보·플레이 소감을 정리하는 기준(빈도·회귀·심각도, 결함과 체감 의견 분리, 체감 의견의 SO/설계 분류, 수정 후 재현 재확인)이 없었다. 이 빈틈만 반영했다.

- 참고: Donchitos/Claude-Code-Game-Studios(MIT)의 `bug-report`·`playtest-report`·`smoke-check` SKILL.md 원문을 확인했다. 문구·상태 체계(VERIFIED FIXED, P1~P4, `production/qa/` 폴더)는 가져오지 않고 프로젝트 상태 표기와 `docs/work-status/` 기록 위치에 맞춰 새로 썼다.
- 제외: unity-cli-loop(uloop), unity-biome-mcp, unity-ai-bridge는 외부 패키지·CLI 설치가 필요하고 기능 대부분이 기존 Unity MCP·Hot Reload와 겹친다. 입력 녹화·재생과 Game View 기준 이미지 비교만 실제 빈틈이며 설치는 사용자 승인 대상으로 남겼다. 브라우저 게임용 Playwright 스킬은 Unity에 맞지 않는다.
- 시각 비교는 작업별 일회성 스크립트로 수행한 사례가 있다([시작 경로 프로파일링](startup-profiling.md)의 렌더 쌍 비교). 재사용 도구는 없으며 이번 문서 작업 범위에서 만들지 않았다.

## 검증

| 항목 | 결과 |
|---|---|
| 하네스 자체 검사(`Tools/Harness/Test-Harness.ps1`) 2026-11-15 실행, `하네스 자체 검사 43 개 통과. Unity 실행 없음.` (지침 진입 문서 링크·AGENTS.md 크기 등 하네스 범위 검사이며 스킬 동작 검증은 아님) |
| 문서 링크 | 새 스킬·지침·상태 문서의 상대 링크 대상 파일 존재 확인 |
| 변경 격리 | 기존 미커밋 코드·데이터·문서 변경 보존. 패키지·manifest 변경 없음 |
| 미수행 | Unity 컴파일·Play Mode(문서 전용 변경이라 해당 없음), 실제 제보 적용 사례(첫 사용 시 확인) |

## 다음 사용에서 확인할 항목

첫 실제 제보에서 스킬이 자동 선택되는지, 되묻는 질문이 과하지 않은지, 상태 문서 기록이 기존 ID 규칙과 충돌하지 않는지 확인한다. Codex용 사용자 폴더 설치는 하지 않았다.

분야 연결: [개발과 검증 위키](../wiki/development.md#qa-결과-접수-스킬).
