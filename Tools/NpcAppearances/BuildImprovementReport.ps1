$ErrorActionPreference = 'Stop'
$root = 'output/NpcAppearanceImprovement'
$rows = foreach ($file in Get-ChildItem "$root/prototype" -Filter '*original-metrics.json') {
    $original = Get-Content -Raw $file.FullName | ConvertFrom-Json
    $compact = Get-Content -Raw ($file.FullName.Replace('-original-', '-compact-')) | ConvertFrom-Json
    $id = $file.Name.Replace('-original-metrics.json', '')
    $sourceTimes = @($original.instantiateMs | Sort-Object)
    $compactTimes = @($compact.instantiateMs | Sort-Object)
    [pscustomobject]@{ id=$id; transformsBefore=$original.transforms; transformsAfter=$compact.transforms;
        renderersBefore=$original.renderers; renderersAfter=$compact.renderers;
        inactiveBefore=$original.inactiveTransforms; inactiveAfter=$compact.inactiveTransforms;
        bytesBefore=$original.fileBytes; bytesAfter=$compact.fileBytes;
        hierarchyNativeBefore=$original.hierarchyNativeBytes; hierarchyNativeAfter=$compact.hierarchyNativeBytes;
        sharedNativeBefore=$original.sharedResourceNativeBytes; sharedNativeAfter=$compact.sharedResourceNativeBytes;
        instantiateMedianBefore=$sourceTimes[2]; instantiateMedianAfter=$compactTimes[2] }
}
$rows | Export-Csv -NoTypeInformation -Encoding UTF8 "$root/prototype-comparison.csv"
$resourceSets = @{}
foreach ($variant in @('original','compact')) {
    $resources = @{}
    foreach ($file in Get-ChildItem "$root/prototype" -Filter "*-$variant-shared.csv") {
        foreach ($line in [IO.File]::ReadLines($file.FullName)) {
            $parts = $line.Split([char[]]@(','), 4)
            $key = $parts[0] + '|' + $parts[1]
            $bytes = [long]$parts[2]
            if (-not $resources.ContainsKey($key) -or $resources[$key] -lt $bytes) { $resources[$key] = $bytes }
        }
    }
    $resourceSets[$variant] = $resources
}
[ordered]@{ originalUniqueResources=$resourceSets.original.Count; compactUniqueResources=$resourceSets.compact.Count;
    commonResources=@($resourceSets.original.Keys | Where-Object { $resourceSets.compact.ContainsKey($_) }).Count;
    originalUniqueNativeEstimate=($resourceSets.original.Values | Measure-Object -Sum).Sum;
    compactUniqueNativeEstimate=($resourceSets.compact.Values | Measure-Object -Sum).Sum;
    note='같은 GUID/local ID의 Mesh/Material/Texture는 24개에 걸쳐 한 번만 계산. 시점별 native 측정 최대값 사용. 사본 Definition은 원본을 참조하므로 원본의 비활성 리소스도 포함. Player 메모리 절감 근거가 아님.'
} | ConvertTo-Json | Set-Content -Encoding UTF8 "$root/shared-resources-summary.json"
$html = [Text.StringBuilder]::new()
[void]$html.Append('<!doctype html><html lang="ko"><meta charset="utf-8"><title>P09 NPC 시제품 비교</title><style>body{font:16px system-ui;background:#eee;color:#222;margin:28px}section{margin:36px 0}figure{display:inline-block;margin:8px}img{width:256px}figcaption{margin:8px 0}table{border-collapse:collapse}td,th{padding:8px;border:1px solid #aaa}</style><h1>P09 NPC 경량화 시제품</h1><p>실제 Unity URP 렌더. 동일 카메라·조명·MotionSet 0.35 샘플 자세. 사용자 동작·관통 검수 대기.</p>')
foreach ($row in $rows) {
    [void]$html.Append("<section><h2>$($row.id)</h2><p>Transform $($row.transformsBefore) → $($row.transformsAfter), Renderer $($row.renderersBefore) → $($row.renderersAfter), 파일 $($row.bytesBefore) → $($row.bytesAfter) bytes</p>")
    foreach ($slot in @('Motion_Locomotion_Idle','Motion_Locomotion_Walk','Motion_Npc_Talk')) {
        foreach ($yaw in @(0,90)) {
            [void]$html.Append("<h3>$slot / $yaw °</h3>")
            foreach ($variant in @('original','compact')) {
                $name = "$($row.id)-$variant-$slot-$yaw.png"
                [void]$html.Append("<figure><figcaption>$variant</figcaption><img loading='lazy' src='prototype/$name' alt='$($row.id) $variant $slot $yaw'></figure>")
            }
        }
    }
    [void]$html.Append('</section>')
}
[IO.File]::WriteAllText("$root/index.html", $html.ToString(), [Text.UTF8Encoding]::new($false))
$comparisons = foreach ($file in Get-ChildItem "$root/prototype" -Filter '*original*.png') {
    $compactPath = $file.FullName.Replace('-original-', '-compact-')
    [pscustomobject]@{ original=$file.Name; compact=[IO.Path]::GetFileName($compactPath);
        identical=(Get-FileHash -LiteralPath $file.FullName).Hash -eq (Get-FileHash -LiteralPath $compactPath).Hash }
}
$comparisons | Export-Csv -NoTypeInformation -Encoding UTF8 "$root/render-comparison.csv"
if ($comparisons.Where({ -not $_.identical }).Count -gt 0) { throw '렌더 차이가 있습니다. 확대 전 조사하세요.' }
"비교 $($rows.Count)종, 동일 렌더 $($comparisons.Count)쌍"
