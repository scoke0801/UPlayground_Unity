# 기존 씬 블록이 삭제되거나 동작 컴포넌트가 바뀌지 않았는지 대조한다.
function Read-SceneBlocks([string] $Path) {
    $content = [System.IO.File]::ReadAllText($Path).Replace("`r`n", "`n")
    $matches = [regex]::Matches($content, '(?m)^--- !u!(\d+) &(-?\d+).*$')
    $blocks = @{}
    for ($i = 0; $i -lt $matches.Count; $i++) {
        $match = $matches[$i]
        $end = if ($i + 1 -lt $matches.Count) { $matches[$i + 1].Index } else { $content.Length }
        $blocks[$match.Groups[2].Value] = @{ Type = $match.Groups[1].Value; Text = $content.Substring($match.Index, $end - $match.Index) }
    }
    return $blocks
}
$before = Read-SceneBlocks 'output/HarborEast/Before/SaltwindHarbor.unity'
$after = Read-SceneBlocks 'Assets/01.Scenes/Scenario/SaltwindHarbor.unity'
$report = [System.Collections.Generic.List[string]]::new()
$changed = 0
foreach ($id in $before.Keys) {
    if (-not $after.ContainsKey($id)) { throw "기존 블록 삭제: $id" }
    if ($before[$id].Text.TrimEnd() -eq $after[$id].Text.TrimEnd()) { continue }
    $changed++
    $report.Add("fileID=$id classID=$($before[$id].Type)")
    $changes = Compare-Object ($before[$id].Text.Split("`n")) ($after[$id].Text.Split("`n"))
    foreach ($line in $changes) { $report.Add("$($line.SideIndicator) $($line.InputObject)") }
}
$summary = "기존 블록 $($before.Count), 현재 $($after.Count), 삭제 0, 기존 변경 $changed. 아래 변경을 검토해야 함."
$report.Insert(0, $summary)
[System.IO.File]::WriteAllLines('output/HarborEast/SCENE_DIFF.txt', $report)
Write-Output $summary
