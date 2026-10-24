$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Push-Location $taskRoot
try {
    $scenePath = 'Assets/01.Scenes/Scenario/StonegateCitadel.unity'
    $scene = Get-Content -LiteralPath $scenePath -Raw
    $before = Get-Content -LiteralPath 'output/CitadelImprovement/Before/StonegateCitadel.unity' -Raw
    $database = Get-Content -LiteralPath 'Assets/10.Datas/Actor/DataBase/ActorDatabase.asset' -Raw
    $oldDatabase = Get-Content -LiteralPath 'output/CitadelImprovement/Before/ActorDatabase.asset' -Raw
    $report = [System.Collections.Generic.List[string]]::new()
    $rows = @(Import-Csv -LiteralPath 'ArtSource/WorldCities/citadel-residents.csv')
    if ($rows.Count -ne 12) { throw '주민 배치 원본 수가 12명이 아닙니다.' }
    foreach ($row in $rows) {
        $id = 'NPC_Citadel_' + $row.id
        $prefabPath = 'Assets/03.Prefabs/Characters/WorldCities/StonegateCitadel/' + $id + '.prefab'
        $definitionPath = 'Assets/10.Datas/World/WorldCities/StonegateCitadel/Residents/ActorDef_' + $id + '.asset'
        $dataPath = 'Assets/10.Datas/World/WorldCities/StonegateCitadel/Residents/' + $id + '.asset'
        $prefab = Get-Content -LiteralPath $prefabPath -Raw
        $definition = Get-Content -LiteralPath $definitionPath -Raw
        $prefabGuid = [regex]::Match((Get-Content -LiteralPath ($prefabPath + '.meta') -Raw), 'guid: (\w+)').Groups[1].Value
        $definitionGuid = [regex]::Match((Get-Content -LiteralPath ($definitionPath + '.meta') -Raw), 'guid: (\w+)').Groups[1].Value
        $dataGuid = [regex]::Match((Get-Content -LiteralPath ($dataPath + '.meta') -Raw), 'guid: (\w+)').Groups[1].Value
        if (-not $definition.Contains('actorId: ' + $id)) { throw "$id 정의 식별자 누락" }
        if (-not $definition.Contains($prefabGuid) -or -not $definition.Contains($dataGuid)) { throw "$id 정의 참조 누락" }
        if (-not $prefab.Contains($definitionGuid) -or -not $prefab.Contains($dataGuid)) { throw "$id 프리팹 참조 누락" }
        if (-not $scene.Contains('m_SourcePrefab: {fileID: 100100000, guid: ' + $prefabGuid)) { throw "$id 씬 배치 누락" }
        if ([regex]::Matches($database, $definitionGuid).Count -ne 1) { throw "$id DB 등록 누락 또는 중복" }
        $report.Add("PASS $id prefab/data/definition/database/scene")
    }
    foreach ($match in [regex]::Matches($oldDatabase, 'guid: ([a-f0-9]{32})')) {
        if (-not $database.Contains($match.Groups[1].Value)) { throw '기존 DB 참조가 없어졌습니다.' }
    }
    $currentIds = [System.Collections.Generic.HashSet[string]]::new()
    foreach ($match in [regex]::Matches($scene, '(?m)^--- !u!\d+ &(-?\d+)')) { [void]$currentIds.Add($match.Groups[1].Value) }
    foreach ($match in [regex]::Matches($before, '(?m)^--- !u!\d+ &(-?\d+)')) {
        if (-not $currentIds.Contains($match.Groups[1].Value)) { throw ('기존 씬 오브젝트 fileID 누락: ' + $match.Groups[1].Value) }
    }
    foreach ($name in @('CitadelLife','CitadelResidents','CitadelRestPoint','MarketFinish','FountainWater')) {
        if (-not $scene.Contains('m_Name: ' + $name)) { throw "$name 저장 누락" }
    }
    $report.Add('PASS 기존 ActorDatabase 참조 및 씬 fileID 보존')
    $report.Add('PASS 최종 세부 배치와 휴식 지점 씬 저장')
    $report.Add('주의: 텍스트 참조 검사이며 Unity 역직렬화·NavMesh·Play Mode 검증이 아님.')
    [System.IO.Directory]::CreateDirectory((Join-Path $taskRoot 'output/CitadelImprovement')) | Out-Null
    $report | Set-Content -LiteralPath 'output/CitadelImprovement/file-validation.txt' -Encoding UTF8
    $report
} finally { Pop-Location }
