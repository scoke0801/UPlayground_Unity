# Unity의 종료 코드만으로 통과를 판단하지 않고 실제 NUnit 결과를 확인한다.
function Read-UnityTestResult {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [string]$TestFilter
    )

    [xml]$document = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    $run = $document.SelectSingleNode('/test-run')
    if ($null -eq $run) { throw "NUnit test-run 결과가 없습니다: $Path" }
    foreach ($attribute in @('total', 'passed', 'failed', 'skipped', 'result')) {
        if (-not $run.HasAttribute($attribute)) { throw "결과 속성이 없습니다: $attribute" }
    }

    $total = [int]$run.GetAttribute('total')
    $passed = [int]$run.GetAttribute('passed')
    $failed = [int]$run.GetAttribute('failed')
    $skipped = [int]$run.GetAttribute('skipped')
    $cases = @($document.SelectNodes('//test-case'))
    if ($total -le 0 -or $passed -le 0 -or $failed -ne 0 -or
        $run.GetAttribute('result') -ne 'Passed' -or $cases.Count -ne $total -or
        $skipped -lt 0 -or $total -ne ($passed + $failed + $skipped)) {
        throw "테스트 통과 근거가 불충분합니다: total=$total, passed=$passed, failed=$failed, skipped=$skipped"
    }
    foreach ($case in $cases) {
        if ($case.GetAttribute('result') -notin @('Passed', 'Skipped')) {
            throw "통과하지 않은 테스트: $($case.GetAttribute('fullname'))"
        }
    }
    $actualPassed = @($cases | Where-Object { $_.GetAttribute('result') -eq 'Passed' }).Count
    $actualSkipped = @($cases | Where-Object { $_.GetAttribute('result') -eq 'Skipped' }).Count
    if ($actualPassed -ne $passed -or $actualSkipped -ne $skipped) {
        throw '요약 집계와 실제 test-case 결과가 일치하지 않습니다.'
    }
    # 현재 Quick 프로필은 정규식이 아닌 완전한 fixture 이름 목록이다.
    foreach ($fixture in @($TestFilter -split ';' | Where-Object { $_ })) {
        $matching = @($cases | Where-Object {
            $_.GetAttribute('fullname').StartsWith($fixture + '.', [StringComparison]::Ordinal) -and
            $_.GetAttribute('result') -eq 'Passed'
        })
        if ($matching.Count -eq 0) { throw "선택한 fixture가 실행되지 않았습니다: $fixture" }
    }
    return [pscustomobject]@{ total = $total; passed = $passed; failed = $failed; skipped = $skipped }
}
