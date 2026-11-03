$ErrorActionPreference = 'Stop'
$layout = Get-Content Tools/OpenWorld/city-layout.json -Raw | ConvertFrom-Json
$sites = Get-Content Tools/OpenWorld/landmark-sites-plan.json -Raw | ConvertFrom-Json
$candidates = @()
foreach ($route in $layout.routes) {
    if ($route.id -like 'dungeon*') { continue }
    $carry = 55.0
    for ($i = 1; $i -lt $route.points.Count; $i++) {
        $a = $route.points[$i-1]; $b = $route.points[$i]
        $dx = $b.x-$a.x; $dz = $b.z-$a.z
        $length = [math]::Sqrt($dx*$dx+$dz*$dz)
        while ($carry -lt $length) {
            $side = 1
            if ($candidates.Count % 2 -eq 0) { $side = -1 }
            $candidates += @{
                id = ('road_{0:000}' -f $candidates.Count); route = $route.id
                x = [math]::Round($a.x+$dx*$carry/$length-$dz/$length*18*$side,2)
                z = [math]::Round($a.z+$dz*$carry/$length+$dx/$length*18*$side,2)
                landmark = $false
            }
            $carry += 125
        }
        $carry -= $length
    }
}
foreach ($site in $sites.sites) {
    foreach ($side in @(-1,1)) {
        $candidates += @{id=('ruin_'+$site.id+'_'+$side);route=$site.name;x=$site.x+$side*40;z=$site.z;landmark=$true;kind='humanoid'}
    }
}
$plan = @{
    scene = 'Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity'
    start = @{x=1091.08;z=418.08}
    bands = @(@{radius=180;level=1},@{radius=300;level=3},@{radius=420;level=5},@{radius=560;level=10},@{radius=700;level=15},@{radius=820;level=20},@{radius=950;level=25},@{radius=1080;level=30},@{radius=1230;level=40},@{radius=9999;level=50})
    minGroupSpacing=65;safeNpcRadius=40;safeInteractionRadius=18;safeMonsterRadius=38
    maxGroundSlope=27;members=3;candidates=$candidates
    safeRegions=@($layout.cities | ForEach-Object {$_.bounds});safeRegionPadding=30
}
$plan | ConvertTo-Json -Depth 8 | Set-Content Tools/OpenWorld/monster-population-plan.json -Encoding UTF8
Write-Output ('candidates='+$candidates.Count)
