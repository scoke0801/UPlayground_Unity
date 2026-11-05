$ErrorActionPreference = 'Stop'
$root = 'output/NpcAppearanceImprovement'
New-Item -ItemType Directory -Force -Path $root | Out-Null
if (Test-Path "$root/baseline.json") { throw '기존 기준선을 덮어쓰지 않습니다.' }
git status --porcelain=v1 | Set-Content -Encoding UTF8 "$root/git-status-before.txt"
$files = @(
    Get-ChildItem Assets/03.Prefabs/Characters/GeneratedNpcAppearances -Recurse -File
    Get-ChildItem Assets/10.Datas/Actor/GeneratedNpcAppearances -Recurse -File
    Get-ChildItem Assets/10.Datas/Actor/Npc -Filter 'NPC_Gen_*' -File
    Get-ChildItem Assets/10.Datas/Actor/DataBase -Filter 'NPC_Gen_*' -File
    Get-Item Assets/10.Datas/Actor/DataBase/ActorDatabase.asset*
    Get-ChildItem Assets/02.Scripts/Editor/P09Builder -Recurse -File
)
$rows = foreach ($file in ($files | Sort-Object FullName -Unique)) {
    $relative = $file.FullName.Substring((Get-Location).Path.Length + 1).Replace('\','/')
    [ordered]@{ path=$relative; bytes=$file.Length; sha256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash }
}
$rows | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 "$root/baseline.json"
$rows.Count
