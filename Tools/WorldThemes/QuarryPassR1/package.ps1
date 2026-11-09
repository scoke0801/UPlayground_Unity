$ErrorActionPreference='Stop'
$root=(Get-Location).Path
$out=Join-Path $root 'output/QuarryPassR1'
$archivePath=Join-Path $out 'QuarryPassR1-handoff.zip'
if(Test-Path -LiteralPath $archivePath){throw '기존 패키지가 있으므로 덮어쓰지 않습니다.'}
$files=@((Get-Content (Join-Path $out 'package-files.json') -Raw|ConvertFrom-Json)|ForEach-Object {$_.path})
$files+=@(Get-ChildItem -LiteralPath $out -File -Recurse | Where-Object {$_.Extension -ne '.zip' -and $_.Name -ne 'package-sha256.txt' -and $_.Name -ne 'package-inventory.json'} | ForEach-Object {$_.FullName.Substring($root.Length+1).Replace('\','/')})
$inventory=@($files|Sort-Object -Unique|ForEach-Object {$file=Join-Path $root $_; @{path=$_;bytes=(Get-Item -LiteralPath $file).Length;sha256=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash}})
$inventory|ConvertTo-Json -Depth 4|Set-Content (Join-Path $out 'package-inventory.json') -Encoding UTF8
$files+= 'output/QuarryPassR1/package-inventory.json'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive=[IO.Compression.ZipFile]::Open($archivePath,[IO.Compression.ZipArchiveMode]::Create)
try {foreach($relative in ($files|Sort-Object -Unique)){[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,(Join-Path $root $relative),$relative,[IO.Compression.CompressionLevel]::Optimal)|Out-Null}}
finally {$archive.Dispose()}
(Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash|Set-Content (Join-Path $out 'package-sha256.txt')
Get-Item -LiteralPath $archivePath|Select-Object FullName,Length
