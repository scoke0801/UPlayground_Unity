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

Remove-Item -LiteralPath $fixturePath, $missingArt
Get-ChildItem -LiteralPath $testRoot -File | ForEach-Object { Remove-Item -LiteralPath $_.FullName }
Remove-Item -LiteralPath $testRoot
# 이번 실행이 생성한 두 파일만 지우며 기존 작업 준비표는 건드리지 않는다.
Remove-Item -LiteralPath $planPath, (Join-Path $planDirectory 'review.md')
Remove-Item -LiteralPath $planDirectory
Write-Output "하네스 자체 검사 $script:passedChecks 개 통과. Unity 실행 없음. 합성 준비표 정리 완료."
