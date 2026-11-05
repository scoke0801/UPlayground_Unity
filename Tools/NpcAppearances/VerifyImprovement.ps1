$ErrorActionPreference = 'Stop'
$root = 'output/NpcAppearanceImprovement'
$baseline = Get-Content -Raw "$root/baseline.json" | ConvertFrom-Json
$plans = @(Get-Content "$root/patch-plan.jsonl" | ForEach-Object { $_ | ConvertFrom-Json })
$map = @{}
foreach ($plan in $plans) { $map[$plan.path] = $plan }
$rows = foreach ($entry in $baseline) {
    $current = (Get-FileHash -LiteralPath $entry.path -Algorithm SHA256).Hash
    $state = '동일'
    if ($current -ne $entry.sha256) {
        if ($map.ContainsKey($entry.path)) {
            $plan = $map[$entry.path]
            $state = if ($current -eq $plan.afterHash) { '모션 필드만 변경' } else { '예상 외 변경' }
            $original = [IO.File]::ReadAllBytes("$root/backup/$($entry.path)")
            $restored = [IO.File]::ReadAllText($entry.path).Replace($plan.value, '  _motionSet: {fileID: 0}')
            $hash = [Security.Cryptography.SHA256]::Create()
            try { $restoredHash = [BitConverter]::ToString($hash.ComputeHash([Text.Encoding]::UTF8.GetBytes($restored))).Replace('-', '') }
            finally { $hash.Dispose() }
            if ($restoredHash -ne $entry.sha256) { $state = '예상 외 변경' }
        } elseif ($entry.path -eq 'Assets/02.Scripts/Editor/P09Builder/Build/Steps/ApplyWeaponStep.cs') {
            $state = '공통 생성 경로 수정'
        } elseif ($entry.path -eq 'Assets/02.Scripts/Editor/P09Builder/NpcAppearanceGeneratorTests.cs') {
            $state = '모션 연결 회귀 검사 추가'
        } else { $state = '별도 변경 감지: 자동 원복 금지' }
    }
    [pscustomobject][ordered]@{ path=$entry.path; state=$state; before=$entry.sha256; after=$current }
}
$rows | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 "$root/preservation-check.json"
$rows | Group-Object state | Select-Object Name,Count | ConvertTo-Json | Set-Content -Encoding UTF8 "$root/preservation-summary.json"
Get-Content "$root/preservation-summary.json"
git status --porcelain=v1 | Set-Content -Encoding UTF8 "$root/git-status-after.txt"
