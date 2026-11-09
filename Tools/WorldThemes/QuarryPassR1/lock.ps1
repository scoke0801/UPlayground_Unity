param([ValidateSet('acquire','heartbeat','release')][string]$Action='acquire',[string]$Task='A 제작')
$ErrorActionPreference='Stop'
$path='Tools/WorldThemes/AgentCoordination/editor.lock'
$token='QuarryPassR1-A-20261108'
if($Action -eq 'acquire'){
    try {$stream=[IO.File]::Open($path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)}
    catch [IO.IOException] { Write-Output 'BUSY'; Get-Content -LiteralPath $path; exit 2 }
    try {$json=@{owner='A';token=$token;task=$Task;updatedUtc=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json;$bytes=[Text.Encoding]::UTF8.GetBytes($json);$stream.Write($bytes,0,$bytes.Length)}
    finally {$stream.Dispose()}
    Write-Output 'ACQUIRED';exit 0
}
$record=Get-Content -LiteralPath $path -Raw|ConvertFrom-Json
if($record.token -ne $token){throw 'A 소유 잠금이 아닙니다.'}
if($Action -eq 'heartbeat'){$record.task=$Task;$record.updatedUtc=[DateTime]::UtcNow.ToString('o');$record|ConvertTo-Json|Set-Content -LiteralPath $path -Encoding UTF8}
else {Remove-Item -LiteralPath $path;Write-Output 'RELEASED'}
