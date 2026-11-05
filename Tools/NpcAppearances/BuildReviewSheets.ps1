param([string]$ReviewRoot = "output/NpcAppearanceReview")
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing
$resolvedRoot = (Resolve-Path -LiteralPath $ReviewRoot).Path
$records = @(Get-ChildItem -LiteralPath "$resolvedRoot/audit" -Filter '*.json' | Sort-Object Name | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json })
if ($records.Count -ne 288) { throw "검수 기록이 288개여야 합니다: $($records.Count)" }
$font = New-Object System.Drawing.Font('Consolas', 15)
$smallFont = New-Object System.Drawing.Font('Consolas', 11)
$brush = [System.Drawing.Brushes]::White
$sheetsPath = Join-Path $resolvedRoot 'sheets'
[IO.Directory]::CreateDirectory($sheetsPath) | Out-Null
$html = New-Object System.Text.StringBuilder
[void]$html.AppendLine('<!doctype html><html lang="ko"><meta charset="utf-8"><title>P09 NPC 외형 검수</title><style>body{background:#25282c;color:#eee;font:16px sans-serif;margin:32px}a{color:#b9dbff}img{max-width:100%;height:auto}section{margin:48px 0}table{border-collapse:collapse}td,th{padding:8px;border:1px solid #666}</style><h1>P09 NPC 외형 검수 · 288종</h1><p>실제 생성 프리팹 / Unity 6000.3.21f1 URP PC_RPAsset / 공통 저장 T 포즈. 이미지 판정과 런타임 검증은 별도입니다.</p><p>이미지를 누르면 원본 크기로 열립니다. M=남성, F=여성. 01~12는 원본 프리셋 순번입니다.</p><p><a href="REVIEW_REPORT.md">검수 보고</a> · <a href="PLAY_MODE_TEST.md">사용자 Play Mode 테스트</a> · <a href="appearance-manifest.csv">프리셋·렌더 대조표</a> · <a href="prefab-cost.csv">프리팹 비용</a></p>')
foreach ($group in ($records | Group-Object { $_.id.Substring(0,13) } | Sort-Object Name)) {
    $items = @($group.Group | Sort-Object id)
    if ($items.Count -ne 12) { throw "12종 묶음 오류: $($group.Name)" }
    [void]$html.AppendLine("<section><h2>$($group.Name.Replace('NPC_Gen_', ''))</h2>")
    foreach ($view in @('body','face')) {
        $height = 576
        if ($view -eq 'face') { $height = 384 }
        $sheet = New-Object System.Drawing.Bitmap(1536, (($height + 40) * 3 + 48))
        $graphics = [System.Drawing.Graphics]::FromImage($sheet)
        try {
            $graphics.Clear([System.Drawing.Color]::FromArgb(38,41,45))
            $graphics.DrawString("$($group.Name) | $view | URP | saved T pose", $font, $brush, 12, 10)
            for ($index = 0; $index -lt 12; $index++) {
                $id = $items[$index].id
                $file = Join-Path $resolvedRoot "raw/${id}_${view}.png"
                $source = [System.Drawing.Image]::FromFile($file)
                try {
                    if ($source.Width -ne 384 -or $source.Height -ne $height) { throw "이미지 규격 오류: $file" }
                    $x = ($index % 4) * 384
                    $y = [math]::Floor($index / 4) * ($height + 40) + 48
                    $graphics.DrawImageUnscaled($source, $x, $y)
                    $graphics.DrawString("${id} | ${view}", $smallFont, $brush, $x + 8, $y + $height + 10)
                } finally { $source.Dispose() }
            }
            $name = "$($group.Name)_${view}.jpg"
            $sheet.Save((Join-Path $sheetsPath $name), [System.Drawing.Imaging.ImageFormat]::Jpeg)
            [void]$html.AppendLine("<a href='sheets/$name'><img loading='lazy' src='sheets/$name' alt='$name'></a>")
        } finally { $graphics.Dispose(); $sheet.Dispose() }
    }
    [void]$html.AppendLine('<p>개별 원본: ')
    foreach ($item in $items) {
        $id = $item.id
        [void]$html.AppendLine("$($id.Substring(14)) <a href='raw/${id}_body.png'>전신</a> <a href='raw/${id}_face.png'>얼굴</a> · ")
    }
    $id = $items[0].id
    [void]$html.AppendLine("</p><p>대표 01: <a href='raw/${id}_side.png'>측면</a> · <a href='raw/${id}_back.png'>후면</a></p></section>")
}
# 원본 렌더 픽셀은 보존하고 이미지 아래에 식별자 띠만 추가한다.
foreach ($file in (Get-ChildItem -LiteralPath "$resolvedRoot/raw" -Filter '*.png')) {
    $source = [System.Drawing.Image]::FromFile($file.FullName)
    $labelled = New-Object System.Drawing.Bitmap($source.Width, ($source.Height + 40))
    $graphics = [System.Drawing.Graphics]::FromImage($labelled)
    try {
        $graphics.Clear([System.Drawing.Color]::FromArgb(38,41,45))
        $graphics.DrawImageUnscaled($source, 0, 0)
        $graphics.DrawString($file.BaseName, $smallFont, $brush, 8, $source.Height + 10)
        $source.Dispose()
        $labelled.Save($file.FullName, [System.Drawing.Imaging.ImageFormat]::Png)
    } finally { $source.Dispose(); $graphics.Dispose(); $labelled.Dispose() }
}
$records | Select-Object id,prefabBytes,transforms,inactiveTransforms,renderers,inactiveRenderers,visibleRenderers,hierarchyNativeBytes,dependencyNativeBytes,dependencyCount,loadMs,instantiateMs | Export-Csv -LiteralPath "$resolvedRoot/prefab-cost.csv" -NoTypeInformation -Encoding UTF8
$records | Select-Object id,expectedHair,expectedHairColor,expectedFace,expectedSkin,expectedEye,expectedBeard,expectedBust,linksMatch,missingScripts,hasDialogue,hasMerchant,@{n='activeRenderers';e={($_.activeRenderers | ForEach-Object { "$($_.name):$($_.mesh):$($_.materials -join '|')" }) -join ';'}},@{n='bustBones';e={$_.bustBones -join ';'}} | Export-Csv -LiteralPath "$resolvedRoot/appearance-manifest.csv" -NoTypeInformation -Encoding UTF8
[IO.File]::WriteAllText("$resolvedRoot/index.html", $html.ToString(), [Text.UTF8Encoding]::new($false))
$font.Dispose()
$smallFont.Dispose()
Write-Output "48개 비교판 및 624개 식별자 포함 PNG 생성 완료."
