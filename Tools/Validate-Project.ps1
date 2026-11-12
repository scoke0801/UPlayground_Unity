param(
    [ValidateSet("Quick", "EditMode", "Full")]
    [string]$Profile = "Quick",

    [string]$UnityPath,

    [switch]$SkipDotnetBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot 'Harness/TestResult.ps1')

$projectRoot = Split-Path -Parent $PSScriptRoot
$resultRoot = Join-Path $projectRoot "Temp/ProjectValidation"
$quickTestFilter = @(
    "UPlayGround.Contracts.Tests.ServicesTests"
    "UPlayGround.Core.Tests.CurrencyWalletTests"
    "UPlayGround.Ability.Tests.AbilityCoreRuntimeTests"
    "UPlayGround.Ability.Tests.AbilityTaskRuntimeTests"
    "UPlayGround.Ability.Tests.GameplayEffectSpecTests"
    "UPlayGround.Movement.Tests.ActorStateReentryTests"
    "UPlayGround.Movement.Tests.ActorStateTransitionGuardTests"
) -join ";"

function Resolve-UnityEditorPath {
    if (-not [string]::IsNullOrWhiteSpace($UnityPath)) {
        return [System.IO.Path]::GetFullPath($UnityPath)
    }

    $versionFile = Join-Path $projectRoot "ProjectSettings/ProjectVersion.txt"
    $versionLine = Get-Content -LiteralPath $versionFile |
        Where-Object { $_ -like "m_EditorVersion:*" } |
        Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($versionLine)) {
        throw "ProjectVersion.txt에서 Unity 버전을 찾지 못했습니다."
    }

    $editorVersion = ($versionLine -split ":", 2)[1].Trim()
    return "C:\Program Files\Unity\Hub\Editor\$editorVersion\Editor\Unity.exe"
}

function Test-ProjectUnityRunning {
    $escapedProjectRoot = [Regex]::Escape($projectRoot)
    $process = Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -match $escapedProjectRoot } |
        Select-Object -First 1
    return $null -ne $process
}

function Invoke-DotnetBuild {
    param([string]$Target)

    Write-Host "[Build] $Target" -ForegroundColor Cyan
    $buildOutput = @(& dotnet build (Join-Path $projectRoot $Target) --no-restore --nologo 2>&1)
    $buildExitCode = $LASTEXITCODE
    if ($buildExitCode -ne 0) {
        $buildOutput | ForEach-Object { Write-Host $_ }
        throw "dotnet build 실패: $Target"
    }

    $summary = $buildOutput |
        Where-Object { $_ -match '^\s*(경고|오류)\s+\d+개|^\s*(Warning|Error)\(s\)' } |
        Select-Object -Last 2
    Write-Host "[통과] $Target" -ForegroundColor Green
    $summary | ForEach-Object { Write-Host "        $_" }
}

function Invoke-UnityTestRun {
    param(
        [string]$Platform,
        [string]$Label,
        [string]$TestFilter
    )

    $timestamp = (Get-Date -Format "yyyyMMdd-HHmmss-fff") + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
    $resultPath = Join-Path $resultRoot "$timestamp-$Label.xml"
    $logPath = Join-Path $resultRoot "$timestamp-$Label.log"
    $arguments = @(
        "-batchmode"
        "-projectPath", $projectRoot
        "-runTests"
        "-testPlatform", $Platform
        "-testResults", $resultPath
        "-logFile", $logPath
    )
    if (-not [string]::IsNullOrWhiteSpace($TestFilter)) {
        $arguments += @("-testFilter", $TestFilter)
    }

    Write-Host "[Unity Test] $Label" -ForegroundColor Cyan
    # Start-Process는 배열을 다시 결합하므로 공백이 있는 경로를 명시적으로 인용한다.
    $quotedArguments = $arguments | ForEach-Object { '"' + $_ + '"' }
    $startedAt = Get-Date
    $receipt = [ordered]@{
        source = 'Unity Test Runner 자동 검사'; platform = $Platform; profile = $Profile
        startedAt = $startedAt.ToString('O'); finishedAt = $null
        unityPath = $script:resolvedUnityPath; arguments = $arguments
        filter = $TestFilter; resultPath = $resultPath; logPath = $logPath
        status = '실패'; counts = $null; error = $null
        scope = '이 실행의 자동 테스트만 판정. 사용자 플레이·아트 품질·프로젝트 전체 완료는 판정하지 않음.'
    }
    try {
        $process = Start-Process -FilePath $script:resolvedUnityPath -ArgumentList $quotedArguments -PassThru -WindowStyle Hidden
        $deadline = $startedAt.AddMinutes(30)
        while (-not $process.HasExited -or -not (Test-Path -LiteralPath $resultPath) -or (Test-ProjectUnityRunning)) {
            if ((Get-Date) -ge $deadline) {
                throw "Unity 테스트가 제한시간을 초과했습니다. log=$logPath"
            }

            if ($process.HasExited -and
                $process.ExitCode -ne 0 -and
                -not (Test-ProjectUnityRunning) -and
                -not (Test-Path -LiteralPath $resultPath))
            {
                throw "Unity 테스트 실행 실패: exit=$($process.ExitCode), log=$logPath"
            }

            if ($process.HasExited -and
                (Get-Date) -ge $startedAt.AddSeconds(10) -and
                -not (Test-ProjectUnityRunning) -and
                -not (Test-Path -LiteralPath $resultPath))
            {
                throw "Unity 테스트 결과가 생성되지 않았습니다. exit=$($process.ExitCode), log=$logPath"
            }

            Start-Sleep -Seconds 1
        }

        if (-not (Test-Path -LiteralPath $resultPath)) {
            throw "Unity 테스트 결과가 생성되지 않았습니다. exit=$($process.ExitCode), log=$logPath"
        }

        if (-not $process.HasExited -or $process.ExitCode -ne 0) {
            throw "Unity 프로세스가 정상 종료하지 않았습니다: log=$logPath"
        }
        $receipt.counts = Read-UnityTestResult -Path $resultPath -TestFilter $TestFilter
        $receipt.status = '통과'
    }
    catch {
        $receipt.error = $_.Exception.Message
        throw
    }
    finally {
        $receipt.finishedAt = (Get-Date).ToString('O')
        $receipt | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath "$resultPath.json" -Encoding UTF8
    }

    Write-Host "[통과] $Label ($($receipt.counts | ConvertTo-Json -Compress))" -ForegroundColor Green
}

Push-Location $projectRoot
try {
    New-Item -ItemType Directory -Path $resultRoot -Force | Out-Null

    if (-not $SkipDotnetBuild) {
        if ($Profile -eq "Quick") {
            Invoke-DotnetBuild "UPlayGround.Core.Tests.csproj"
            Invoke-DotnetBuild "UPlayGround.Contracts.Tests.csproj"
            Invoke-DotnetBuild "UPlayGround.Ability.Tests.csproj"
            Invoke-DotnetBuild "UPlayGround.Movement.Tests.csproj"
        }
        else {
            Invoke-DotnetBuild "UPlayground.sln"
        }
    }

    $script:resolvedUnityPath = Resolve-UnityEditorPath
    if (-not (Test-Path -LiteralPath $script:resolvedUnityPath)) {
        throw "Unity Editor를 찾지 못했습니다: $script:resolvedUnityPath"
    }

    if (Test-ProjectUnityRunning) {
        throw "프로젝트가 Unity Editor에서 열려 있습니다. 에디터를 닫고 다시 실행하세요."
    }

    $unityLockFile = Join-Path $projectRoot "Temp/UnityLockfile"
    if (Test-Path -LiteralPath $unityLockFile) {
        Write-Warning "실행 중인 Unity 프로세스가 없어 오래된 UnityLockfile을 무시합니다."
    }

    if ($Profile -eq "Quick") {
        Invoke-UnityTestRun "EditMode" "quick-editmode" $quickTestFilter
    }
    else {
        Invoke-UnityTestRun "EditMode" "all-editmode" ""
        if ($Profile -eq "Full") {
            Invoke-UnityTestRun "PlayMode" "all-playmode" ""
        }
    }

    Write-Host "선택한 자동 테스트를 통과했습니다. profile=$Profile (사용자 플레이·시각 품질 확인은 별도)" -ForegroundColor Green
}
finally {
    Pop-Location
}
