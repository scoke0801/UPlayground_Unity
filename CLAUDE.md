@AGENTS.md

<!-- 공통 지침의 정본은 AGENTS.md다(Codex와 공유). 아래에는 Claude Code 전용 규칙만 둔다. 공통 규칙을 여기에 복사하면 두 파일이 다시 어긋난다. -->

# Claude Code 전용 지침

## 도구 사용 (승인 요청 줄이기)

자동 모드에서도 셸 명령이 프로젝트 안의 파일만 읽는지 확인되지 않으면 매번 사용자 승인 요청이 뜬다. 작업 흐름을 끊지 않도록 다음을 지킨다.

- 파일 읽기·검색은 Read/Grep/Glob 도구로 한다. Bash/PowerShell로 `cat`·`sed -n`·`head`·`grep`·`find`를 돌리지 않는다. 여러 파일을 봐야 하면 도구 호출을 한 번에 병렬로 보낸다.
- 셸 명령에 `for`/`while` 반복문과 `$( )` 명령 치환을 넣지 않는다. 승인 요청이 뜬 명령 대부분이 이 형태였다.
- 작업 디렉터리는 이미 프로젝트 루트다. `cd ... &&` 접두를 붙이지 않고, 필요한 경로는 인자로 넘긴다.
- 셸은 `git`, `dotnet build` 같은 실행 작업에만 쓰고, 한 호출에는 단순한 명령 하나(필요하면 짧은 파이프)만 담는다.
- 프로젝트 밖 경로(`~/.claude/projects`, 임시 폴더 등)는 꼭 필요할 때만 읽는다. 읽을 때마다 승인 요청이 뜬다.

## 경로별 상세 규칙

`.claude/rules/`의 규칙은 `paths`에 맞는 파일을 Read/Edit/Write할 때 자동으로 로드된다. Unity MCP로만 에셋을 다루는 작업은 파일 접근이 없어 자동 로드되지 않으므로, AGENTS.md의 「작업별 필독 지침」 표에 따라 해당 규칙 파일을 직접 읽는다.

## 역할 분담

`.claude/agents/`의 `sub-programmer`·`designer`·`artist`·`qa-reviewer`는 [역할 분담 하네스](Tools/Harness/ROLES.md)의 위임 기준에 맞을 때만 쓴다. 메인 세션이 리드다. 위임할 때는 ROLES.md의 위임 브리프를 채워 프롬프트로 넘긴다. 서브 Agent는 대화 이력을 물려받지 않는다.

## 파괴적 명령 가드

`.claude/settings.json`의 PreToolUse 훅(`.claude/hooks/Guard-DestructiveCommand.ps1`)이 Bash·PowerShell의 되돌릴 수 없는 git 명령과 Unity `Library`·`Temp`·`ProjectSettings`·`Packages`·잠금 파일 삭제를 차단한다. 차단되면 우회하지 말고 사용자에게 필요한 명령을 안내한다.
