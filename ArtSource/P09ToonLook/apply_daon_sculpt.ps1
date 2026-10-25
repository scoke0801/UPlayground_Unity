$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$canonical = 'Assets/03.Prefabs/Characters/ScenarioResidents/Npc/Npc_Daon/Npc_Daon.prefab'
$variants = @('Assets/03.Prefabs/Characters/HarborResidents/NPC_Harbor_Bora.prefab', 'Assets/03.Prefabs/Characters/HarborResidents/NPC_Harbor_Yuna.prefab', 'Assets/03.Prefabs/Environment/Farming/FarmWorker_0.prefab')
$bindings = @(
    @{ Renderer = '8693195829483037707'; Before = '{fileID: 6540140745801285552, guid: e5da2379dc44b4e4d9a3b9a9eac5e5b9, type: 3}'; After = '{fileID: 4300000, guid: 96d8e67a5ed160b4586c5719b22c37e7, type: 2}' },
    @{ Renderer = '1412038549217049843'; Before = '{fileID: -4172375244245752585, guid: e5da2379dc44b4e4d9a3b9a9eac5e5b9, type: 3}'; After = '{fileID: 4300000, guid: 969f8101aaaafdf49812d1111eb2ecf1, type: 2}' }
)
$utf8 = New-Object System.Text.UTF8Encoding($false)
$originals = @{}
$planned = @{}
foreach ($relative in @($canonical) + $variants) {
    $path = Join-Path $projectRoot $relative
    $originals[$path] = [IO.File]::ReadAllBytes($path)
    $content = [IO.File]::ReadAllText($path)
    $newline = if ($content.Contains("`r`n")) { "`r`n" } else { "`n" }
    foreach ($binding in $bindings) {
        if ($relative -eq $canonical) {
            $before = 'm_Mesh: ' + $binding.Before
            $after = 'm_Mesh: ' + $binding.After
            if ($content.Contains($after)) { continue }
            if ([regex]::Matches($content, [regex]::Escape($before)).Count -ne 1) { throw '원본 메시 참조가 유일하지 않습니다.' }
            $content = $content.Replace($before, $after)
        } else {
            $target = '    - target: {fileID: ' + $binding.Renderer + ', guid: dc565f06dcbb1904099feee3bba83d1c, type: 3}'
            $block = $target + $newline + '      propertyPath: m_Mesh' + $newline + '      value: ' + $newline + '      objectReference: ' + $binding.Before + $newline
            if ($content.Contains($block)) { continue }
            if ($content.Contains($target + $newline + '      propertyPath: m_Mesh')) { throw '기존 메시 오버라이드를 먼저 검토해야 합니다.' }
            $marker = '    m_Modifications:' + $newline
            if ([regex]::Matches($content, [regex]::Escape($marker)).Count -ne 1) { throw 'Variant 구조가 예상과 다릅니다.' }
            $content = $content.Replace($marker, $marker + $block)
        }
    }
    $planned[$path] = $content
    $backup = Join-Path $projectRoot ('output/P09DaonSculpt/BeforeAssets/' + $relative)
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($backup)) | Out-Null
    if (-not [IO.File]::Exists($backup)) { [IO.File]::WriteAllBytes($backup, $originals[$path]) }
}
try {
    foreach ($path in $planned.Keys) { [IO.File]::WriteAllText($path, $planned[$path], $utf8) }
} catch {
    foreach ($path in $originals.Keys) { [IO.File]::WriteAllBytes($path, $originals[$path]) }
    throw
}
Write-Output 'Daon의 메시 2개 교체, 상속 NPC 3개의 기존 메시 고정 완료.'
