Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'TestResult.ps1')
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$testRoot = Join-Path $projectRoot ('output/harness-selftest/' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testRoot -Force | Out-Null
$fixturePath = Join-Path $testRoot 'result.xml'
$script:passedChecks = 0

function Assert-Result([string]$Name, [string]$Xml, [bool]$ExpectedPass, [string]$Filter = '') {
    Set-Content -LiteralPath $fixturePath -Value $Xml -Encoding UTF8
    $didPass = $true
    try { $null = Read-UnityTestResult -Path $fixturePath -TestFilter $Filter }
    catch { $didPass = $false }
    if ($didPass -ne $ExpectedPass) { throw "판정 오류: $Name" }
    $script:passedChecks++
}

$valid = '<test-run total="1" passed="1" failed="0" skipped="0" result="Passed"><test-case fullname="Example.Fixture.Works" result="Passed" /></test-run>'
Assert-Result '정상 결과' $valid $true 'Example.Fixture'
Assert-Result 'fixture 누락' $valid $false 'Example.Missing'
Assert-Result '0개 실행' '<test-run total="0" passed="0" failed="0" skipped="0" result="Passed" />' $false
Assert-Result '전부 skipped' '<test-run total="1" passed="0" failed="0" skipped="1" result="Passed"><test-case fullname="A.B" result="Skipped" /></test-run>' $false
Assert-Result '실패' ($valid.Replace('result="Passed"', 'result="Failed"')) $false
Assert-Result '중단' ($valid.Replace('result="Passed"', 'result="Inconclusive"')) $false
Assert-Result 'XML 손상' '<test-run' $false
Assert-Result '잘못된 루트' '<result />' $false
Assert-Result '속성 누락' ($valid.Replace(' passed="1"', '')) $false
Assert-Result 'case 누락' '<test-run total="1" passed="1" failed="0" skipped="0" result="Passed" />' $false
Assert-Result '거짓 집계' ($valid.Replace('fullname="Example.Fixture.Works" result="Passed"', 'fullname="Example.Fixture.Works" result="Skipped"')) $false
Assert-Result '음수 집계' ($valid.Replace('skipped="0"', 'skipped="-1"')) $false
try {
    $null = Read-UnityTestResult -Path (Join-Path $testRoot 'missing.xml')
    throw '누락 결과 파일이 통과했습니다.'
}
catch { if ($_.Exception.Message -eq '누락 결과 파일이 통과했습니다.') { throw } }
$script:passedChecks++

$missingArt = Join-Path $testRoot 'Bridge.blend'
$message = & (Join-Path $PSScriptRoot 'Prepare-Task.ps1') -Paths @($missingArt) -ArtSource 'MCP 테스트 원본'
$planDirectory = ($message -split ': ', 2)[1]
$planPath = Join-Path $planDirectory 'plan.json'
$plan = Get-Content -LiteralPath $planPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($plan.artSource -ne 'MCP 테스트 원본' -or $plan.status -ne '미검증' -or
    @($plan.checks | Where-Object { $_ -like '*구매/생성 아트*' }).Count -ne 1) { throw '아트 준비표 오류' }
$null = & (Join-Path $PSScriptRoot 'Prepare-Task.ps1') -CheckPlan $planPath
$script:passedChecks++
Set-Content -LiteralPath $missingArt -Value '새 아트 산출물' -Encoding UTF8
$isStale = $false
try { $null = & (Join-Path $PSScriptRoot 'Prepare-Task.ps1') -CheckPlan $planPath }
catch { $isStale = $true }
if (-not $isStale) { throw '입력 변경을 놓쳤습니다.' }
$script:passedChecks++

$isOutsideRejected = $false
try { $null = & (Join-Path $PSScriptRoot 'Prepare-Task.ps1') -Paths @('../outside.fbx') }
catch { $isOutsideRejected = $true }
if (-not $isOutsideRejected) { throw '프로젝트 밖 경로를 허용했습니다.' }
$script:passedChecks++

# 빈 입력은 비교할 해시가 없어도 유효한 준비표가 아니다.
$emptyPlanPath = Join-Path $testRoot 'empty-plan.json'
Set-Content -LiteralPath $emptyPlanPath -Value '{"inputs":[]}' -Encoding UTF8
$isEmptyRejected = $false
try { $null = & (Join-Path $PSScriptRoot 'Prepare-Task.ps1') -CheckPlan $emptyPlanPath }
catch { $isEmptyRejected = $true }
if (-not $isEmptyRejected) { throw '빈 준비표를 허용했습니다.' }
$script:passedChecks++

# 실제 Unity 대신 프로세스 응답만 대체해 실행기의 실패 기록 경로를 확인한다.
$runnerPath = Join-Path $projectRoot 'Tools/Validate-Project.ps1'
$tokens = $null
$parseErrors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile($runnerPath, [ref]$tokens, [ref]$parseErrors)
if ($parseErrors.Count -gt 0) { throw '실행기 구문 오류' }
$definition = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Invoke-UnityTestRun' }, $true)
Invoke-Expression $definition.Extent.Text
$resultRoot = $testRoot
$Profile = 'Quick'
$script:resolvedUnityPath = 'mock-unity.exe'
function Test-ProjectUnityRunning { return $false }
function Start-Process {
    param($FilePath, $ArgumentList, [switch]$PassThru, $WindowStyle)
    $resultIndex = [Array]::IndexOf($ArgumentList, '"-testResults"') + 1
    if ($script:fakeCase -ne 'missing') {
        $xmlPath = $ArgumentList[$resultIndex].Trim('"')
        $xml = $valid
        if ($script:fakeCase -eq 'zero') { $xml = '<test-run total="0" passed="0" failed="0" skipped="0" result="Passed" />' }
        Set-Content -LiteralPath $xmlPath -Value $xml -Encoding UTF8
    }
    $exitCode = 0
    if ($script:fakeCase -in @('missing', 'exit')) { $exitCode = 1 }
    return [pscustomobject]@{ HasExited = $true; ExitCode = $exitCode }
}
foreach ($scenario in @('passed', 'zero', 'missing', 'exit')) {
    $script:fakeCase = $scenario
    $didPass = $true
    try { Invoke-UnityTestRun 'EditMode' $scenario 'Example.Fixture' }
    catch { $didPass = $false }
    if ($didPass -ne ($scenario -eq 'passed')) { throw "실행기 판정 오류: $scenario" }
    $receipts = @(Get-ChildItem -LiteralPath $testRoot -Filter "*-$scenario.xml.json")
    if ($receipts.Count -ne 1) { throw "실행 근거 누락: $scenario" }
    $receipt = Get-Content -LiteralPath $receipts[0].FullName -Raw -Encoding UTF8 | ConvertFrom-Json
    $expectedStatus = '실패'
    if ($didPass) { $expectedStatus = '통과' }
    if ($receipt.status -ne $expectedStatus) { throw "실행 근거 상태 오류: $scenario" }
    $script:passedChecks++
}

# 파괴적 명령 가드 훅: 차단해야 할 명령과 통과시켜야 할 일상 명령을 함께 확인한다.
$guardPath = Join-Path $projectRoot '.claude/hooks/Guard-DestructiveCommand.ps1'
$guardCases = [ordered]@{
    'git reset --hard HEAD'                                   = $true
    'git status; git reset --hard'                            = $true
    'git clean -fdx'                                          = $true
    'git push --force origin main'                            = $true
    'git push -f'                                             = $true
    'git rebase -i HEAD~3'                                    = $true
    'Remove-Item -Recurse -Force Library'                     = $true
    'rm -rf ./Temp'                                           = $true
    'Remove-Item "C:\UsingProject\UnityProject\UPlayground\ProjectSettings" -Recurse' = $true
    'del Temp\UnityLockfile'                                  = $true
    'git status --short'                                      = $false
    'git log --oneline -5'                                    = $false
    'git push origin main'                                    = $false
    'git restore --staged Assets/a.cs'                        = $false
    'Remove-Item -LiteralPath output/harness-selftest/Temp/a.xml' = $false
    'rm output/Library/old.txt'                               = $false
    'dotnet build UPlayGround.Actor.csproj --no-restore'      = $false
}
foreach ($guardCommand in $guardCases.Keys) {
    $hookJson = @{ tool_name = 'PowerShell'; tool_input = @{ command = $guardCommand } } | ConvertTo-Json -Compress
    $guardOutput = & $guardPath -InputJson $hookJson
    $isBlocked = [bool]($guardOutput -match '"permissionDecision":"deny"')
    if ($isBlocked -ne $guardCases[$guardCommand]) { throw "가드 판정 오류: $guardCommand" }
    $script:passedChecks++
}
if ($null -ne (& $guardPath -InputJson '{not json')) { throw '가드가 손상 입력을 차단했습니다.' }
$script:passedChecks++

# 공통 지침 단일 정본: CLAUDE.md는 AGENTS.md를 가져오고, 안내된 경로별 규칙 파일이 존재해야 한다.
$claudeText = Get-Content -LiteralPath (Join-Path $projectRoot 'CLAUDE.md') -Raw -Encoding UTF8
if ($claudeText -notmatch '(?m)^@AGENTS\.md\s*$') { throw 'CLAUDE.md가 @AGENTS.md를 가져오지 않습니다.' }
if ($claudeText -match '(?m)^## (역할과 목표|아키텍처|코드 작성 원칙)') { throw 'CLAUDE.md에 공통 지침이 복제되었습니다. AGENTS.md에만 둡니다.' }
$agentsText = Get-Content -LiteralPath (Join-Path $projectRoot 'AGENTS.md') -Raw -Encoding UTF8
$ruleLinks = @([regex]::Matches($agentsText, '\.claude/rules/[\w.-]+\.md') | ForEach-Object { $_.Value } | Sort-Object -Unique)
if ($ruleLinks.Count -eq 0) { throw 'AGENTS.md에 경로별 규칙 안내가 없습니다.' }
foreach ($ruleLink in $ruleLinks) {
    $rulePath = Join-Path $projectRoot $ruleLink
    if (-not (Test-Path -LiteralPath $rulePath)) { throw "규칙 파일 누락: $ruleLink" }
    if ((Get-Content -LiteralPath $rulePath -TotalCount 1) -ne '---') { throw "규칙 paths 머리말 누락: $ruleLink" }
}
$script:passedChecks++

# AGENTS.md는 Codex 기본 한도(32KiB) 안에서 여유를 두고 간결하게 유지한다.
$agentsBytes = (Get-Item -LiteralPath (Join-Path $projectRoot 'AGENTS.md')).Length
if ($agentsBytes -gt 24KB) { throw "AGENTS.md가 24KiB를 넘었습니다($agentsBytes 바이트). 상세는 지침·규칙 문서로 옮깁니다." }
$script:passedChecks++

# 지침 진입 문서의 로컬 링크가 실제 파일을 가리켜야 한다. 끊긴 경로는 Agent가 지침을 못 읽게 만든다.
$linkDocuments = @('AGENTS.md', 'CLAUDE.md', 'Tools/Harness/README.md', 'Tools/Harness/ROLES.md') +
    @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Tools/Harness/roles') -Filter '*.md' | ForEach-Object { "Tools/Harness/roles/$($_.Name)" }) +
    @(Get-ChildItem -LiteralPath (Join-Path $projectRoot '.claude/rules') -Filter '*.md' | ForEach-Object { ".claude/rules/$($_.Name)" })
foreach ($linkDocument in $linkDocuments) {
    $documentPath = Join-Path $projectRoot $linkDocument
    $documentText = Get-Content -LiteralPath $documentPath -Raw -Encoding UTF8
    foreach ($linkMatch in [regex]::Matches($documentText, '\]\(([^)#\s]+)(?:#[^)]*)?\)')) {
        $target = $linkMatch.Groups[1].Value
        if ($target -match '^[a-z]+:') { continue }
        $targetPath = Join-Path (Split-Path -Parent $documentPath) $target
        if (-not (Test-Path -LiteralPath $targetPath)) { throw "끊긴 링크: $linkDocument -> $target" }
    }
}
$script:passedChecks++

# 역할 정의: Claude·Codex 정의가 같은 역할 집합을 갖고 정본 역할 문서를 가리켜야 한다.
$roleDirectory = Join-Path $projectRoot 'Tools/Harness/roles'
$claudeRoles = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot '.claude/agents') -Filter '*.md' | ForEach-Object { $_.BaseName } | Sort-Object)
$codexRoles = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot '.codex/agents') -Filter '*.toml' | ForEach-Object { $_.BaseName.Replace('_', '-') } | Sort-Object)
if (($claudeRoles -join ',') -ne ($codexRoles -join ',')) { throw "역할 정의 불일치: Claude=$($claudeRoles -join ',') Codex=$($codexRoles -join ',')" }
foreach ($roleName in $claudeRoles) {
    if (-not (Test-Path -LiteralPath (Join-Path $roleDirectory "$roleName.md"))) { throw "역할 정본 누락: $roleName" }
    $claudeAgentText = Get-Content -LiteralPath (Join-Path $projectRoot ".claude/agents/$roleName.md") -Raw -Encoding UTF8
    $codexAgentText = Get-Content -LiteralPath (Join-Path $projectRoot ".codex/agents/$($roleName.Replace('-', '_')).toml") -Raw -Encoding UTF8
    if ($claudeAgentText -notmatch [regex]::Escape("Tools/Harness/roles/$roleName.md") -or
        $codexAgentText -notmatch [regex]::Escape("Tools/Harness/roles/$roleName.md")) { throw "역할 정의가 정본을 가리키지 않습니다: $roleName" }
    if ($claudeAgentText -notmatch '(?m)^disallowedTools:.*\bAgent\b') { throw "역할 Agent의 중첩 위임이 열려 있습니다: $roleName" }
}
$script:passedChecks++

Remove-Item -LiteralPath $fixturePath, $missingArt
Get-ChildItem -LiteralPath $testRoot -File | ForEach-Object { Remove-Item -LiteralPath $_.FullName }
Remove-Item -LiteralPath $testRoot
# 이번 실행이 생성한 두 파일만 지우며 기존 작업 준비표는 건드리지 않는다.
Remove-Item -LiteralPath $planPath, (Join-Path $planDirectory 'review.md')
Remove-Item -LiteralPath $planDirectory
Write-Output "하네스 자체 검사 $script:passedChecks 개 통과. Unity 실행 없음. 합성 준비표 정리 완료."
