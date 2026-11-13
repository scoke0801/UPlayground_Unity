<#
.SYNOPSIS
Claude Code PreToolUse 훅: Bash·PowerShell의 되돌릴 수 없는 명령을 차단한다.

.DESCRIPTION
settings.local.json의 deny 규칙은 Bash 접두 일치만 막아 PowerShell 도구와 복합 명령(`a; git reset --hard`)을 놓친다.
이 훅은 명령 전문을 검사해 다음을 차단한다.
- 작업 트리·이력을 되돌릴 수 없게 바꾸는 git 명령(reset --hard, clean -f, push --force, rebase, filter-branch)
- Unity 프로젝트 루트의 Library·Temp·ProjectSettings·Packages 폴더와 UnityLockfile 삭제
판정이 불가능한 입력(빈 입력·JSON 오류)은 일반 권한 흐름으로 넘긴다. 이 훅은 승인 단계가 아니라 차단 전용이다.

.PARAMETER InputJson
하네스 자체 검사용. 지정하지 않으면 표준 입력의 훅 JSON을 읽는다.
#>
param([string]$InputJson)

$ErrorActionPreference = 'Stop'
[Console]::InputEncoding = [Text.Encoding]::UTF8
[Console]::OutputEncoding = [Text.Encoding]::UTF8

function Get-BlockReason([string]$Command) {
    $gitRules = [ordered]@{
        '\bgit\s+reset\s+(?:\S+\s+)*--hard\b'           = 'git reset --hard는 미커밋 사용자 변경을 지운다.'
        '\bgit\s+clean\s+(?:\S+\s+)*-[a-zA-Z]*f'         = 'git clean -f는 미추적 아트·산출물을 지운다(Git 추적 밖 에셋 다수).'
        '\bgit\s+push\s+(?:.*\s)?(?:--force\S*|-f)\b'    = '강제 push는 원격 이력을 덮어쓴다.'
        '\bgit\s+rebase\b'                                = 'rebase는 이력을 다시 쓴다. 필요하면 사용자에게 직접 실행을 요청한다.'
        '\bgit\s+filter-branch\b'                         = 'filter-branch는 이력을 다시 쓴다.'
    }
    foreach ($pattern in $gitRules.Keys) {
        if ($Command -match "(?i)$pattern") { return $gitRules[$pattern] }
    }

    $isDeletion = $Command -match '(?i)(?:^|[\s;&|(])(?:rm|rmdir|rd|del|erase|ri|Remove-Item)(?=\s)'
    if (-not $isDeletion) { return $null }

    if ($Command -match '(?i)UnityLockfile') {
        return 'UnityLockfile 삭제 금지: 열린 Editor를 깨뜨린다. 두 번째 Editor를 열지 말고 기존 Editor·MCP를 사용한다.'
    }
    # 루트 기준 보호 폴더만 잡는다. output/Temp 같은 하위 경로는 막지 않는다.
    $protectedRoot = '(?i)(?:^|[\s"''=,(]|\.[\\/]|UPlayground[\\/])(Library|Temp|ProjectSettings|Packages)(?:[\\/"''\s,)]|$)'
    if ($Command -match $protectedRoot) {
        return "Unity 프로젝트 루트 폴더($($Matches[1])) 삭제 금지: 원본 프로젝트의 Library·Temp는 부산물 정리 대상이 아니다(Tools/Harness/README.md)."
    }
    return $null
}

try {
    if (-not $InputJson) { $InputJson = [Console]::In.ReadToEnd() }
    if ([string]::IsNullOrWhiteSpace($InputJson)) { exit 0 }
    $hookInput = $InputJson | ConvertFrom-Json
}
catch { exit 0 }

$command = [string]$hookInput.tool_input.command
if ([string]::IsNullOrWhiteSpace($command)) { exit 0 }

$reason = Get-BlockReason $command
if (-not $reason) { exit 0 }

@{
    hookSpecificOutput = @{
        hookEventName            = 'PreToolUse'
        permissionDecision       = 'deny'
        permissionDecisionReason = "[하네스 가드] $reason 우회하지 말고 필요하면 사용자에게 직접 실행을 요청한다."
    }
} | ConvertTo-Json -Depth 4 -Compress
exit 0
