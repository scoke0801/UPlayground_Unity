$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$canonical = 'Assets/03.Prefabs/Characters/ScenarioResidents/Npc/Npc_Daon/Npc_Daon.prefab'
$variants = @('Assets/03.Prefabs/Characters/HarborResidents/NPC_Harbor_Bora.prefab', 'Assets/03.Prefabs/Characters/HarborResidents/NPC_Harbor_Yuna.prefab', 'Assets/03.Prefabs/Environment/Farming/FarmWorker_0.prefab')
$profile = 'Assets/10.Datas/Actor/ScenarioResidents/ToonLook/P09_Daon_ToonLook.asset'
$oldMaterials = @('01903bcfa669fc8488d2f956c3f4c3fd', 'bbc0bc1770b7d734da47c9bc6c381493')
$newMaterials = @('f7c4551fcfa4ebc4cbb5aa8288f1371a', 'f0f762e9f37549441b43bea8c6cda6c1')
$utf8 = New-Object System.Text.UTF8Encoding($false)
$originals = @{}
$planned = @{}
foreach ($relative in @($canonical, $profile) + $variants) {
    $path = Join-Path $projectRoot $relative
    $originals[$path] = [IO.File]::ReadAllBytes($path)
    $content = [IO.File]::ReadAllText($path)
    $newline = if ($content.Contains("`r`n")) { "`r`n" } else { "`n" }
    if ($relative -eq $canonical) {
        $pattern = '(?s)--- !u!137 &8693195829483037707\r?\n.*?(?=\r?\n---|\z)'
        $match = [regex]::Match($content, $pattern)
        if (-not $match.Success) { throw 'Daon 얼굴 렌더러를 찾지 못했습니다.' }
        $block = $match.Value
        for ($i=0; $i -lt 2; $i++) {
            if (-not $block.Contains($oldMaterials[$i]) -and -not $block.Contains($newMaterials[$i])) { throw '얼굴 재질 참조가 예상과 다릅니다.' }
            $block = $block.Replace($oldMaterials[$i], $newMaterials[$i])
        }
        $content = $content.Substring(0,$match.Index) + $block + $content.Substring($match.Index+$match.Length)
    } elseif ($relative -eq $profile) {
        $sources = @('91f9e83d500b0fd4b9c0c48b65c8a3fd', $oldMaterials[1])
        for ($i=0; $i -lt 2; $i++) {
            if ($content.Contains($newMaterials[$i])) { continue }
            $content += '  - rendererPrefix: Female_Face_02' + $newline + '    source: {fileID: 2100000, guid: ' + $sources[$i] + ', type: 2}' + $newline + '    replacement: {fileID: 2100000, guid: ' + $newMaterials[$i] + ', type: 2}' + $newline
        }
    } else {
        for ($i=0; $i -lt 2; $i++) {
            $target = '    - target: {fileID: 8693195829483037707, guid: dc565f06dcbb1904099feee3bba83d1c, type: 3}'
            $property = '      propertyPath: m_Materials.Array.data[' + $i + ']'
            $block = $target + $newline + $property + $newline + '      value: ' + $newline + '      objectReference: {fileID: 2100000, guid: ' + $oldMaterials[$i] + ', type: 2}' + $newline
            if ($content.Contains($block)) { continue }
            if ($content.Contains($target+$newline+$property) -or $content.Contains($target+$newline+'      propertyPath: ''m_Materials.Array.data['+$i+']''')) { throw '기존 얼굴 재질 override를 먼저 검토해야 합니다.' }
            $marker = '    m_Modifications:' + $newline
            if ([regex]::Matches($content,[regex]::Escape($marker)).Count -ne 1) { throw 'Variant 구조가 예상과 다릅니다.' }
            $content = $content.Replace($marker,$marker+$block)
        }
    }
    $planned[$path] = $content
    $backup = Join-Path $projectRoot ('output/P09DaonFace/BeforeAssets/' + $relative)
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($backup)) | Out-Null
    if (-not [IO.File]::Exists($backup)) { [IO.File]::WriteAllBytes($backup,$originals[$path]) }
}
try { foreach ($path in $planned.Keys) { [IO.File]::WriteAllText($path,$planned[$path],$utf8) } }
catch { foreach ($path in $originals.Keys) { [IO.File]::WriteAllBytes($path,$originals[$path]) }; throw }
Write-Output 'Daon 얼굴 재질 2개 적용. 상속 NPC 3개는 기존 재질 유지. Daon 빌더 프로파일 연결 완료.'
