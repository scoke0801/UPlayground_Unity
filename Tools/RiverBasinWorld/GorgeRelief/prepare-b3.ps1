param(
    [ValidateSet('dry', 'apply', 'reset', 'water', 'dress', 'plains-dry', 'plains', 'finish', 'landing', 'map')]
    [string]$Mode = 'dry'
)

$ErrorActionPreference = 'Stop'
$plan = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'gorge-plan-b3.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$file = switch ($Mode) {
    'water' { 'water-b3.cs.txt' }
    'dress' { 'dress-b3.cs.txt' }
    'plains-dry' { 'plains-b3.cs.txt' }
    'plains' { 'plains-b3.cs.txt' }
    'finish' { 'finish-b3.cs.txt' }
    'landing' { 'landing-b3.cs.txt' }
    'map' { 'map-b3.cs.txt' }
    default { 'blockout-b3.cs.txt' }
}
$body = Get-Content -LiteralPath (Join-Path $PSScriptRoot $file) -Raw -Encoding UTF8
$culture = [System.Globalization.CultureInfo]::InvariantCulture

function FloatLiteral($Value) {
    ([double]$Value).ToString($culture) + 'f'
}

function VectorPairs($Pairs) {
    $vectors = foreach ($pair in $Pairs) {
        'new Vector2({0},{1})' -f (FloatLiteral $pair[0]), (FloatLiteral $pair[1])
    }
    'new []{' + ($vectors -join ',') + '}'
}

function FloatArray($Values) {
    'new []{' + (($Values | ForEach-Object { FloatLiteral $_ }) -join ',') + '}'
}

$strings = [ordered]@{
    Mode = $Mode
    ScenePath = $plan.scene
    DefinitionPath = $plan.definition
    RootName = $plan.root
    AssetFolder = $plan.folder
    ArtFolder = $plan.folder -replace 'GorgeReliefG1$', 'GorgeReliefArt'
}
foreach ($entry in $strings.GetEnumerator()) {
    'const string {0}={1};' -f $entry.Key, (ConvertTo-Json $entry.Value -Compress)
}
$profiles = [ordered]@{
    Line = $plan.centerlineXZ
    Water = $plan.waterProfileSY
    newWidths = $plan.channelHalfWidth
    Depths = $plan.poolDepth
    Shoulders = $plan.shoulderY
    Trail = $plan.bankTrailY
}
foreach ($entry in $profiles.GetEnumerator()) {
    'var {0}={1};' -f $entry.Key, (VectorPairs $entry.Value)
}
$scalars = [ordered]@{
    BridgeS = $plan.bridge.station
    BridgeSpan = $plan.bridge.span
    BridgeWidth = $plan.bridge.width
    BridgeY = $plan.bridge.deckY
    ApproachLength = $plan.bridge.approachLength
    B1Radius = $plan.protections.B1.radius
    B4Radius = $plan.protections.B4.radius
    B4Shoulder = $plan.protections.B4.shoulderFrom
    StartS = $plan.bounds.startS
    EndS = $plan.bounds.endS
    ShoreHalfWidth = $plan.bounds.shoreHalfWidth
}
foreach ($entry in $scalars.GetEnumerator()) {
    'const float {0}={1};' -f $entry.Key, (FloatLiteral $entry.Value)
}
'var ExitStations={0};' -f (FloatArray $plan.exits.station)
'var ExitLengths={0};' -f (FloatArray ($plan.exits | ForEach-Object { $_.length }))
'var ExitWidths={0};' -f (FloatArray $plan.exits.width)
'var ExitSlopes={0};' -f (FloatArray $plan.exits.maxSlope)
'var Falls={0};' -f (FloatArray $plan.falls.s)
'var FallWidths={0};' -f (FloatArray $plan.falls.width)
'var B1=new Vector2({0},{1});' -f (FloatLiteral $plan.protections.B1.x), (FloatLiteral $plan.protections.B1.z)
'var B4=new Vector2({0},{1});' -f (FloatLiteral $plan.protections.B4.x), (FloatLiteral $plan.protections.B4.z)
'var WaterNames=new []{{{0}}};' -f (($plan.sourceWaterNames | ForEach-Object { ConvertTo-Json $_ -Compress }) -join ',')
if ($Mode -in 'plains-dry', 'plains', 'finish', 'map') {
    $plains = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'plains-b3.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    'var MainPath={0};' -f (VectorPairs $plains.mainPathXZ)
    'var PlainSites={0};' -f (VectorPairs ($plains.sites | ForEach-Object { ,@($_.x, $_.z) }))
    'var PlainSpecs=new []{{{0}}};' -f (($plains.sites | ForEach-Object {
        FloatArray @([int]($_.kind -eq 'terrace'), $_.axis, $_.halfLength, $_.halfWidth, $_.height)
    }) -join ',')
}
if ($Mode -eq 'dry') {
    $body = $body -replace '(?s)if \(Mode == "reset"\).*?(?=Func<Vector2,float,float> target)', ''
    $body = ($body -split 'if\(Mode=="dry"\) return report;', 2)[0] + 'return report;'
}
$body = if ($Mode -eq 'plains-dry') {
    ($body -split 'if\(Mode=="plains-dry"\)return report;', 2)[0] + 'return report;'
} else {
    $body
}
$body
