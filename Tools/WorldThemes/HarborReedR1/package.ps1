$ErrorActionPreference='Stop'
$root=(Get-Location).Path
$out=Join-Path $root 'output/HarborReedR1'
$manifest=Get-Content (Join-Path $out 'package-files.json') -Raw|ConvertFrom-Json
$files=@($manifest.files.path)+@('Tools/WorldThemes/HarborReedR1/package.ps1')|Sort-Object -Unique
$rows=@($files|ForEach-Object { $f=Get-Item -LiteralPath (Join-Path $root $_); @{path=$_;bytes=$f.Length;sha256=(Get-FileHash -LiteralPath $f.FullName -Algorithm SHA256).Hash.ToLowerInvariant()} })
$manifest.files=$rows
$manifest|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $out 'package-files.json') -Encoding UTF8
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zipPath=Join-Path $out 'HarborReedR1-review-and-selective-merge.zip'
$stream=[IO.File]::Open($zipPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
$zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create)
try { foreach($p in @($files)+@('output/HarborReedR1/package-files.json')) { [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,(Join-Path $root $p),$p,[IO.Compression.CompressionLevel]::Optimal)|Out-Null } }
finally { $zip.Dispose();$stream.Dispose() }
$hash=Get-FileHash -LiteralPath $zipPath -Algorithm SHA256
($hash.Hash+'  '+[IO.Path]::GetFileName($zipPath))|Set-Content -LiteralPath (Join-Path $out 'package.sha256') -Encoding ASCII
[pscustomobject]@{package=$zipPath;bytes=(Get-Item -LiteralPath $zipPath).Length;files=$files.Count+1;sha256=$hash.Hash}
