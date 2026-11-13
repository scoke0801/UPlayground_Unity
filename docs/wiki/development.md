# 개발과 검증

## 정본

- [asmdef 모듈화 계획](../../Assets/docs/Complete/ASMDEF_MODULARIZATION_PLAN.md), [아키텍처 규칙](../../.claude/rules/architecture.md): 모듈 경계 기준. 온보딩 HTML은 현재 저장소에 없다.
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

## 2026-11-13 Agent 지침 단일 정본·경로별 규칙·파괴적 명령 가드

Claude Code 공식 문서(메모리·훅)와 Anthropic 장기 실행 하네스 글을 기준으로 재검토해 세 가지를 반영했다.

- **지침 단일 정본.** `CLAUDE.md`가 있으면 Claude Code는 `AGENTS.md`를 읽지 않는다. 그런데 두 파일이 따로 수정되어 어긋나 있었다. Claude는 무기 모션 이식·NPC 일상 애니메이션 필독 지침과 "요청 없이 커밋 금지"·Data 모듈 참조 금지·`.meta` 동반 이동 같은 모듈 규칙을 읽지 못했고, Codex는 FlowGraph·개명된 캐릭터 enum·최신 GAS 규약을 읽지 못했다. 이제 [AGENTS.md](../../AGENTS.md)가 두 파일의 내용을 합친 공통 정본이다(오래된 Bokusei 고정 서술은 뺐다). [CLAUDE.md](../../CLAUDE.md)는 `@AGENTS.md` 가져오기와 Claude 전용 도구 사용 규칙만 담는다.
- **경로별 규칙.** 공식 권장치(파일당 200줄 이하)를 넘던 상시 로드 분량 중 GAS 상세 계약은 [gameplay-ability.md](../../.claude/rules/gameplay-ability.md)로, Editor 데이터 도구 안전 규칙은 [editor-data-tools.md](../../.claude/rules/editor-data-tools.md)로 옮겼다. 두 파일은 `paths` 범위로 나눴다. Claude는 해당 경로 파일에 접근할 때 자동으로 로드하고, Codex와 MCP 중심 작업은 AGENTS.md의 한 줄 안내를 따라 직접 연다.
- **파괴적 명령 가드.** 기존 deny 규칙은 `Bash(...)` 접두만 막아 PowerShell 도구와 복합 명령을 놓쳤다. [settings.json](../../.claude/settings.json)의 PreToolUse 훅 [Guard-DestructiveCommand.ps1](../../.claude/hooks/Guard-DestructiveCommand.ps1)이 이제 git reset --hard, clean -f, 강제 push, rebase, filter-branch와 루트 Library·Temp·ProjectSettings·Packages·UnityLockfile 삭제를 차단한다. 이 훅은 승인 단계가 아니라 차단 전용이다.

검증: `Test-Harness.ps1` 자체 검사가 21건에서 40건으로 늘었고 모두 통과했다. 가드 차단 10건, 통과 7건, 손상 입력 1건, 지침 가져오기·규칙 링크 1건이다. Unity는 실행하지 않았다. 실제 세션의 PowerShell 도구에서 `Remove-Item ./Temp -Recurse -WhatIf`가 훅에 차단되고 한글 사유가 표시되는 것을 확인했다. 런타임 변경이 없어 Unity 검증은 해당하지 않는다. 임시 산출물도 없다.

남은 항목:

- Codex가 `.claude/rules/` 안내를 실제로 따라 읽는지는 확인하지 않았다(Codex 세션 미실행).
- `.claude/settings.local.json`에 일회성 허용 규칙이 약 500개 쌓여 있다. 사용자 로컬 설정이라 손대지 않았고 `/fewer-permission-prompts`로 정리할 것을 권장한다.

### 같은 날 후속: AGENTS.md 간소화와 역할 분담 하네스

- **AGENTS.md 간소화.** AGENTS.md에는 이제 역할, 항상 지키는 규칙 8개, 「작업별 필독 지침」 표만 남았다. 표는 지침 링크와 가장 자주 놓치는 핵심 한 줄로 되어 있다. 아키텍처·모듈 규칙은 [architecture.md](../../.claude/rules/architecture.md)로, 코드 원칙·컨벤션·툴 런처는 [code-conventions.md](../../.claude/rules/code-conventions.md)로 옮겼다. 두 파일은 스크립트 경로 범위다. 매 세션 상시 로드량은 AGENTS+CLAUDE 약 33KB에서 약 11KB로 줄었다(AGENTS.md 8.7KB).
- **끊긴 경로 수정.** 기존 지침이 가리키던 대사 어투 가이드(`Assets/docs/design/…`)와 FlowGraph 스펙(`Assets/docs/TODO/…`)은 이미 `guide/`·`Complete/`로 옮겨져 있어 Agent가 열 수 없는 상태였다. 실제 위치로 고쳤다.
- **역할 분담 하네스.** [ROLES.md](../../Tools/Harness/ROLES.md)와 [역할 문서](../../Tools/Harness/roles/lead.md)를 추가했다. 리드(메인 세션), 서브 프로그래머, 디자이너, 아티스트, QA 검토자 다섯 역할이다. 위임 기준, Unity 슬롯(단일 Editor 배타 사용), 파일 소유, 위키는 리드만 쓰기, 위임 브리프·결과 보고 형식을 정했다. Claude(`.claude/agents/*.md`)와 Codex(`.codex/agents/*.toml`) 정의는 같은 역할 문서를 가리키는 얇은 진입점이다. 역할 Agent는 다른 Agent를 부르지 않는다.
- **설계 근거.** Anthropic 다중 Agent 글에 따르면 코딩은 병렬화 여지가 적고, 맥락을 공유하는 작업은 다중 Agent에 맞지 않으며, 토큰을 약 15배 쓴다. Claude Code 공식 지침은 출력이 길고 자기완결적인 일만 서브 Agent에 맡기라고 권한다. 그래서 48개 역할을 두는 게임 스튜디오 템플릿 방식 대신 독립 검증·아트·플레이 경험 검토·겹치지 않는 병렬 구현에만 역할을 두었다.

검증: `Test-Harness.ps1` 43건 통과. 이번에 AGENTS.md 24KiB 한도, 지침 진입 문서의 로컬 링크, Claude·Codex 역할 정의 대응 검사 3건이 추가됐다. Unity는 실행하지 않았다.

남은 항목:

- 역할 Agent를 실제로 위임해 실행한 검증은 하지 않았다. Claude는 새 세션에서 `/agents` 목록 인식부터 확인해야 한다.
- Codex 사용자 정의 Agent는 OpenAI 문서 형식(name·description·developer_instructions)을 따랐다. Codex 버전에 따라 인식되는지는 확인하지 않았다. Codex 문서상 Agent 파일로 샌드박스를 덮어쓸 수 없으므로, QA 검토자의 읽기 전용은 지시문으로만 강제된다.
- Claude 쪽 QA 검토자도 Edit·Write만 막혀 있다. 셸을 통한 쓰기는 지시문과 파괴적 명령 가드에만 의존한다.

## Unity Hot Reload

[설계 원문](../전달용/Unity_HotReload_Agent_Handoff/DESIGN_SPEC.md) · [작업 상태](../work-status/unity-hot-reload.md) · [ADR 0001](../work-status/unity-hot-reload/adr/0001-scope-and-backend.md) · [ADR 0002](../work-status/unity-hot-reload/adr/0002-backend-proof.md) · [progress.json](../work-status/unity-hot-reload/progress.json)

**판정 `IMPLEMENTED_UNVERIFIED` (2026-11-12).** Editor 전용 패키지 `Packages/com.uplayground.hotreload`와 외부 Worker `Tools/HotReload.Worker`를 구현했다. Bee rsp로 Unity 컴파일을 재현하고 Cecil IR을 원본 멤버로 재연결해 Harmony(Thin 2.3.6) transpiler로 본문만 교체한다. 실제 Unity에서 G1 백엔드 증명, IL·거부 매트릭스, 트랜잭션·롤백·격리, Bridge, 실제 Actor/Camera 대상 Edit Mode smoke를 자동 검증했다(필수 45개 중 PASS 36·FAIL 0·부분 6·미수행 3; T31 Play 20회 정리는 NUnit 시간 초과였으나 전용 드라이버로 20/20 누수 없음 확인, 상세는 [상태 문서](../work-status/unity-hot-reload.md)). 병합본 Harmony의 Burst 해싱 오류와 MonoMod net452의 CodeDom 충돌을 재현·해결했다. Player 빌드·도구 제거·clean checkout·60분 soak·Play 중 실제 관찰은 남아 있으며, 사용자 테스트 명세는 상태 문서에 있다. 툴 런처 디버그 › Project Hot Reload로 연다. 사용법은 [사용 가이드](../../Assets/docs/guide/HOT_RELOAD_USAGE_GUIDE.md).

분야별 설계·상태·검증 문서 목록은 [설계 문서 분류](design-folder-map.md)에서 `개발` 행을 본다. 가이드는 제외한다.

2026-11-13 MCP 병행 사용 검토: 실제 파일 Bridge `status` 응답은 `completed/Ok`(세션 비활성)였다. 정상 코드 변경은 Editor 재시작 없이 MCP 정상 컴파일 요청 후 상태·오류를 확인하는 경로를 사용한다. Play 상태 유지 패치는 현재 등록된 세 메서드에 한하며 파일 수정+Bridge로 진행해야 한다. MCP 스크립트 import·Refresh는 세션을 무효화한다. 이번 점검의 컴파일·패치·Play 관찰은 미수행이며 Console 조회 시간 초과로 MCP 연결 안정성도 미확인이다. 상세 근거와 절차는 [작업 상태](../work-status/unity-hot-reload.md) 및 [사용 가이드](../../Assets/docs/guide/HOT_RELOAD_USAGE_GUIDE.md)에 반영했다.

[위키 시작](README.md)

### 2026-11-13 Hot Reload 우선 작업·차단 복구 설계

[확장 설계](../../Assets/docs/design/dev-tools/UNITY_MCP_HOT_RELOAD_WORKFLOW_DESIGN.md)를 추가했다. 제품·Unity 공식 웹 문서 조사와 현행 코드 대조를 바탕으로, 지원 변경은 Hot Reload 우선, 미지원은 정상 컴파일, 도구 결함에 의한 차단은 수정·회귀 검사·동일 작업 재수행으로 처리한다. AGENTS.md와 기존 가이드에 앞으로의 MCP 작업 원칙을 연결했다.

D0 조사·설계·지침 연결만 반영했다. D1 진단/Bridge, D2 리로드를 넘는 컴파일·MCP 복귀 추적, D3 대상 확대, D4 선택 확장은 미구현이다. 하네스 자체 검사 43건과 새 설계·목차·가이드·AGENTS의 로컬 링크 43개를 확인했으며 실제 Unity 컴파일·패치는 수행하지 않았다. 상세 진행과 기존 증거 누락은 [작업 상태](../work-status/unity-hot-reload.md)에 기록했다.
