param([string]$ReviewRoot = 'output/NpcAppearanceReview')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$resolvedRoot = (Resolve-Path -LiteralPath $ReviewRoot).Path
$records = @(Get-ChildItem -LiteralPath "$resolvedRoot/audit" -Filter '*.json' | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json })
if ($records.Count -ne 288) { throw '검수 레코드 개수 오류' }
$checked = 0
foreach ($record in $records) {
    if (!$record.linksMatch -or $record.missingScripts -ne 0 -or $record.visibleRenderers -lt 6) { throw "프리팹 구조 오류: $($record.id)" }
    $views = @('body','face')
    if ($record.id.EndsWith('_01')) { $views += @('side','back') }
    foreach ($view in $views) {
        $file = Join-Path $resolvedRoot "raw/$($record.id)_$view.png"
        $image = [System.Drawing.Bitmap]::FromFile($file)
        try {
            $height = 616
            if ($view -eq 'face') { $height = 424 }
            if ($image.Width -ne 384 -or $image.Height -ne $height) { throw "이미지 규격 오류: $file" }
            $background = $image.GetPixel(0,0).ToArgb()
            $different = 0
            for ($y = 24; $y -lt $height - 40; $y += 16) {
                for ($x = 24; $x -lt 360; $x += 16) {
                    if ($image.GetPixel($x,$y).ToArgb() -ne $background) { $different++ }
                }
            }
            if ($different -lt 10) { throw "빈 렌더 의심: $file" }
            $checked++
        } finally { $image.Dispose() }
    }
}
if ($checked -ne 624) { throw '이미지 개수 오류' }
$sheets = @(Get-ChildItem -LiteralPath "$resolvedRoot/sheets" -Filter '*.jpg')
if ($sheets.Count -ne 48) { throw '비교판 개수 오류' }
$result = @{records=$records.Count; images=$checked; sheets=$sheets.Count; blankImages=0; links=288; missingScripts=0; note='이미지 생성/구조 자기 검사이며 사람 시각 승인과 런타임 통과가 아님'}
$result | ConvertTo-Json | Set-Content -LiteralPath "$resolvedRoot/artifact-validation.json" -Encoding UTF8
$result | ConvertTo-Json
