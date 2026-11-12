[CmdletBinding(DefaultParameterSetName = 'Prepare')]
param(
    [Parameter(Mandatory = $true, ParameterSetName = 'Prepare')][string[]]$Paths,
    [Parameter(ParameterSetName = 'Prepare')][string]$ArtSource = '미기록',
    [Parameter(Mandatory = $true, ParameterSetName = 'Check')][string]$CheckPlan
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))

function Get-InputHash([string]$RelativePath) {
    $fullPath = Resolve-InputPath $RelativePath
    if (-not $fullPath.StartsWith($projectRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "프로젝트 밖의 파일은 지정할 수 없습니다: $RelativePath"
    }
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) { return 'missing' }
    return (Get-FileHash -LiteralPath $fullPath -Algorithm SHA256).Hash
}

function Resolve-InputPath([string]$InputPath) {
    if ([IO.Path]::IsPathRooted($InputPath)) { return [IO.Path]::GetFullPath($InputPath) }
    return [IO.Path]::GetFullPath((Join-Path $projectRoot $InputPath))
}

if ($PSCmdlet.ParameterSetName -eq 'Check') {
    $plan = Get-Content -LiteralPath $CheckPlan -Raw -Encoding UTF8 | ConvertFrom-Json
    if (@($plan.inputs).Count -eq 0) { throw '준비표 입력이 비어 있습니다.' }
    foreach ($inputEntry in $plan.inputs) {
        if ([string]::IsNullOrWhiteSpace($inputEntry.path) -or
            $inputEntry.sha256 -notmatch '^(missing|[0-9a-fA-F]{64})$') {
            throw '준비표 입력 경로 또는 해시가 유효하지 않습니다.'
        }
    }
    $changed = @($plan.inputs | Where-Object { (Get-InputHash $_.path) -ne $_.sha256 })
    if ($changed.Count -gt 0) { throw "준비표 입력이 변경되었습니다. 다시 준비하세요: $($changed.path -join ', ')" }
    Write-Output '지정 입력의 해시가 같습니다. 검증 통과나 전체 의존성 불변을 뜻하지 않습니다.'
    return
}

$relativePaths = @($Paths | ForEach-Object {
    $fullPath = Resolve-InputPath $_
    if (-not $fullPath.StartsWith($projectRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "프로젝트 밖의 파일은 지정할 수 없습니다: $_"
    }
    if (Test-Path -LiteralPath $fullPath -PathType Container) { throw "폴더 대신 산출물 파일을 지정하세요: $_" }
    $fullPath.Substring($projectRoot.Length + 1).Replace('\', '/')
} | Sort-Object -Unique)

$guides = @('Assets/docs/guide/TEST_VALIDATION_GUIDE.md')
$checks = @('변경 파일의 호출자·소비 모듈·연결 에셋을 조사해 아래 제안을 보완한다.')
$joined = $relativePaths -join "`n"
if ($joined -match '(?i)\.cs($|\n)') { $guides += 'Assets/docs/guide/CODE_AUTHORING_GUIDE.md' }
if ($joined -match '(?i)Ability|BehaviorTree|MotionSet|Combat|/State/') {
    $guides += 'Assets/docs/guide/COMBAT_SYSTEM_AUTHORING_GUIDE.md'
    $checks += '관련 데이터 연결 검사와 기존 EditMode 테스트를 선택한다. 입력 반응·텔레그래프·히트스톱·캔슬 창은 사용자 확인.'
}
if ($joined -match '(?i)/UI/|Input|\.uxml|\.uss') {
    $guides += 'Assets/docs/guide/UI_UX_AUTHORING_GUIDE.md'
    $checks += '초기 포커스·패드 이동·Cancel·탭 전환·일시정지 중 트윈을 사용자 확인.'
}
if ($joined -match '(?i)Quest|Dialogue|FlowGraph|/Item/|Save|Interaction|LivingWorld') {
    $guides += 'Assets/docs/guide/CONTENT_SYSTEM_AUTHORING_GUIDE.md'
    $checks += 'ID·GUID 보존과 정상/역순 진행·저장/로드·재진입의 진행 불능 여부를 확인.'
}
if ($joined -match '(?i)\.asmdef|\.asmref|/Camera/|/Contracts/') {
    $checks += '모듈 소비자·직렬화 호환성을 검토한다. 아키텍처 온보딩을 읽고 영향 범위를 직접 정한다.'
}
if ($joined -match '(?i)\.fbx|\.blend|\.obj|\.png|\.tga|\.mat|\.prefab|\.shader|\.hlsl|/ArtSource/') {
    $checks += '구매/생성 아트: 출처·원본·산출물과 meta를 기록한다. 관련 스케일·피벗·머티리얼·셰이더·충돌·LOD·참조를 기존 도구로 확인한다.'
    $checks += 'Blender 캡처와 Unity 적용 화면을 구분한다. 가독성·스타일·카메라·성능은 실제 Unity 확인 전까지 대기로 남긴다.'
}
if ($joined -match '(?i)\.unity|/Map/|/World/|Terrain|Water|Bridge|Building') {
    $guides += 'Assets/docs/guide/OPENWORLD_MAP_AUTHORING_GUIDE.md'
    $guides += 'Assets/docs/guide/CAMERA_OCCLUSION_FADE_GUIDE.md'
    $checks += '구조물 배치는 카메라 이름 규칙을 확인한다. 지형·물 작업은 TERRAIN_WATER_ART_LESSONS.md도 읽는다.'
}
$guides = @($guides | Sort-Object -Unique)
$inputs = @($relativePaths + @($relativePaths | Where-Object { $_ -like 'Assets/*' -and $_ -notlike '*.meta' } | ForEach-Object { "$_`.meta" }) + $guides | Sort-Object -Unique | ForEach-Object {
    [pscustomobject]@{ path = $_; sha256 = Get-InputHash $_ }
})
$runId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
$output = Join-Path $projectRoot "output/harness/$runId"
New-Item -ItemType Directory -Path $output -Force | Out-Null
$plan = [ordered]@{
    createdAt = (Get-Date).ToString('O'); status = '미검증'
    paths = $relativePaths; artSource = $ArtSource; guides = $guides; inputs = $inputs
    checks = $checks; userTestStatus = '필요 여부 판단 및 사용자 테스트 대기'
    scope = '명시한 파일·meta·선택 가이드만 식별. 경로 분류는 제안이며 전체 영향 분석 또는 검증 결과가 아님.'
}
$plan | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $output 'plan.json') -Encoding UTF8
$lines = @('# 작업 검증 준비표', '', '- 상태: 미검증', "- 아트 출처: $ArtSource", '- Player·Addressables 빌드: 별도 사용자 지시 없이는 실행하지 않음', '', '## 입력 범위', '')
$lines += $relativePaths | ForEach-Object { '- ' + $_ }
$lines += @('', '## 먼저 읽을 문서', '')
$lines += $guides | ForEach-Object { '- ' + $_ }
$lines += @('', '## 영향 조사 및 확인 항목', '')
$lines += $checks | ForEach-Object { '- [ ] ' + $_ }
$lines += @('', '## 작업 부산물 정리', '', '각 단계 종료 시 이번 작업의 부산물만 정리한다. 사용자 변경·구매/MCP 원본·최종 산출물과 meta·위키 근거·다음 단계 입력은 보존한다.', '', '| 단계 | 삭제한 경로·이유 | 보존한 경로·이유·측정 용량 | 미정리 사유·후속 판단 |', '|---|---|---|---|', '| 미기록 | 미확인 | 미확인 | 미기록 |', '', '- [ ] 문서 참조·도구 입력·실행 중 사용·절대 경로와 링크 여부를 확인하고 불필요한 부산물을 정리했다(없으면 해당 없음).', '- [ ] 보관 근거와 위키 링크가 유효하며 삭제·보존·미정리 항목을 인계 보고에 남겼다.')
$lines += @('', '## 단계별 위키 연결', '', '계획한 각 단계가 끝나면 해당 분야 위키를 갱신하고 아래에 페이지와 항목을 기록한다. 링크·파일 존재를 확인한 뒤 다음 단계나 최종 보고로 넘어간다.', '', '| 단계 | 현재 판정 | 갱신한 위키 페이지·항목 | 정본·검증 근거·남은 확인 |', '|---|---|---|---|', '| 미기록 | 미수행 | 미연결 | 미기록 |', '', '- [ ] 단계 결과·정본·검증 근거·남은 확인을 위키에 연결하고 인계 보고에 위키 링크를 포함했다.')
$lines += @('', '## 사용자 테스트 명세', '', 'AI가 대상 씬·시작 상태·입력 장치·단계 ID·조작·기대 결과·실패 기준을 채운 뒤 전달한다.', '회신: 항목 ID / 통과·실패·미수행 / 실제 결과 / 근거', '', '## 결과 기록', '', '| 항목 | 확인 주체·방식 | 통과·실패·미수행·사용자 테스트 대기 | XML·로그·화면 근거 |', '|---|---|---|---|', '| 영향 범위의 필수 검증 | 미기록 | 미수행 | 없음 |', '', $plan.scope)
$lines | Set-Content -LiteralPath (Join-Path $output 'review.md') -Encoding UTF8
Write-Output "준비표 생성(미검증): $output"
