$source=Get-Content 'Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity' -Raw
$path='Assets/01.Scenes/ArtReview/WorldThemes/LakeReworkR2.unity'
$copy=Get-Content $path -Raw
$pattern='(?m)^  _guid: ([^\r\n]+)\r?\n  _ownerKey: (GlobalObjectId_V1-\d+-[a-f0-9]+-(\d+-\d+))'
$map=@{}
foreach($m in [regex]::Matches($source,$pattern)){$map[$m.Groups[3].Value]=$m.Groups[1].Value}
$script:repaired=0
$copy=[regex]::Replace($copy,$pattern,{param($m) $key=$m.Groups[3].Value; if(-not $map.ContainsKey($key)){throw "Unmatched identity $key"}; if($m.Groups[1].Value -ne $map[$key]){$script:repaired++}; '  _guid: '+$map[$key]+"`n  _ownerKey: "+$m.Groups[2].Value})
[System.IO.File]::WriteAllText((Join-Path (Get-Location) $path),$copy,(New-Object System.Text.UTF8Encoding($false)))
"Restored $script:repaired IDs from $($map.Count) source records; copy ownership retained" | Tee-Object -FilePath output/LakeReworkR2/id-repair.txt
