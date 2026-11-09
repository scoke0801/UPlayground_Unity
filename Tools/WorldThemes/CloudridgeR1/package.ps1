$ErrorActionPreference='Stop'
$root=(Get-Location).Path
$out=Join-Path $root 'output/CloudridgeR1'
$zipPath=Join-Path $out 'CloudridgeR1-handoff.zip'
if(Test-Path -LiteralPath $zipPath){throw '기존 패키지를 임의 덮어쓰지 않음'}
$files=@(Get-ChildItem -LiteralPath 'Assets/10.Datas/World/ThemeStudies/CloudridgeR1' -Recurse -File)
$files+=Get-Item -LiteralPath 'Assets/10.Datas/World/ThemeStudies/CloudridgeR1.meta','Assets/01.Scenes/ArtReview/WorldThemes/CloudridgeR1.unity','Assets/01.Scenes/ArtReview/WorldThemes/CloudridgeR1.unity.meta'
$files+=Get-ChildItem -LiteralPath 'Tools/WorldThemes/CloudridgeR1' -Recurse -File
$files+=Get-ChildItem -LiteralPath $out -Recurse -File | Where-Object {$_.Name -notin @('Cloudridge.before.unity','CloudridgeR1-handoff.zip','package-hash.json','package-files.json')}
$records=@($files|ForEach-Object {@{path=$_.FullName.Substring($root.Length+1).Replace('\','/');bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}})
$records|ConvertTo-Json -Depth 8|Set-Content (Join-Path $out 'package-files.json') -Encoding UTF8
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$stream=[IO.File]::Open($zipPath,[IO.FileMode]::CreateNew)
try{$zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create,$true);try{foreach($record in $records){[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,(Join-Path $root $record.path),$record.path,[IO.Compression.CompressionLevel]::Optimal)|Out-Null};[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,(Join-Path $out 'package-files.json'),'output/CloudridgeR1/package-files.json',[IO.Compression.CompressionLevel]::Optimal)|Out-Null}finally{$zip.Dispose()}}finally{$stream.Dispose()}
@{path='output/CloudridgeR1/CloudridgeR1-handoff.zip';bytes=(Get-Item -LiteralPath $zipPath).Length;sha256=(Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash;fileCount=$records.Count+1;purpose='독립 검토 자원과 선택 병합 델타. 전체 씬·Terrain 덮어쓰기 금지'}|ConvertTo-Json|Set-Content (Join-Path $out 'package-hash.json') -Encoding UTF8
Get-Content (Join-Path $out 'package-hash.json')
