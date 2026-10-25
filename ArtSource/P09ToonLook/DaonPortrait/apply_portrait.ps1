$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$path = Join-Path $projectRoot 'Assets/03.Prefabs/Characters/ScenarioResidents/Npc/Npc_Daon/Npc_Daon.prefab'
$meshGuid = 'dbbc37f2f669a804b8ada53c102d722b'
$materialGuid = '86ab1c0ed7f25354ca0ece0e0ac1b526'
$eyeGuid = 'd7287a1df0387fc42b45d6142f918c67'
$eyePath = Join-Path $projectRoot 'Assets/10.Datas/Actor/ScenarioResidents/ToonLook/DaonPortrait/Daon_Eye_Amber.mat'
if (-not (Test-Path -LiteralPath $eyePath) -or -not ([IO.File]::ReadAllText($eyePath+'.meta').Contains('guid: '+$eyeGuid))) { throw '새 눈 재질을 먼저 임포트해야 합니다.' }
foreach ($asset in @(@('Assets/08.Models/Characters/DaonPortrait/Female_Face_02.asset',$meshGuid),@('Assets/10.Datas/Actor/ScenarioResidents/ToonLook/DaonPortrait/Daon_Head_Skin.mat',$materialGuid))) {
    if (-not (Test-Path -LiteralPath (Join-Path $projectRoot $asset[0]))) { throw '새 얼굴 에셋을 먼저 임포트해야 합니다.' }
    if (-not ([IO.File]::ReadAllText((Join-Path $projectRoot ($asset[0]+'.meta'))).Contains('guid: '+$asset[1]))) { throw '새 얼굴 에셋 GUID가 다릅니다.' }
}
$bytes = [IO.File]::ReadAllBytes($path)
$text = [IO.File]::ReadAllText($path)
$match = [regex]::Match($text,'(?s)--- !u!137 &8693195829483037707\r?\n.*?(?=\r?\n---|\z)')
if (-not $match.Success) { throw '기존 얼굴 렌더러 ID를 확인하세요. 다른 프리팹에는 적용하지 않습니다.' }
$block = $match.Value
$mesh = [regex]::Match($block,'m_Mesh: \{fileID: (-?\d+), guid: ([a-f0-9]+), type: \d+\}')
if ($mesh.Groups[2].Value -notin @('2680da3569cacfa4bbc9f5d22cbb303d','96d8e67a5ed160b4586c5719b22c37e7','e5da2379dc44b4e4d9a3b9a9eac5e5b9',$meshGuid)) { throw '예상하지 않은 얼굴 메시입니다.' }
if ($mesh.Groups[2].Value -eq 'e5da2379dc44b4e4d9a3b9a9eac5e5b9' -and $mesh.Groups[1].Value -ne '6540140745801285552') { throw '원본 얼굴 메시 ID가 다릅니다.' }
$skin = [regex]::Match($block,'(?s)(m_Materials:\r?\n\s*- \{fileID: 2100000, guid: )([a-f0-9]+)(, type: 2\})')
if ($skin.Groups[2].Value -notin @('5dbf63eb617ce3f4881632ea20a85e6a','f7c4551fcfa4ebc4cbb5aa8288f1371a','01903bcfa669fc8488d2f956c3f4c3fd','91f9e83d500b0fd4b9c0c48b65c8a3fd',$materialGuid)) { throw '예상하지 않은 얼굴 피부 재질입니다.' }
$block = $block.Replace($mesh.Value,'m_Mesh: {fileID: 4300000, guid: '+$meshGuid+', type: 2}')
$block = $block.Replace($skin.Value,$skin.Groups[1].Value+$materialGuid+$skin.Groups[3].Value)
$eye = [regex]::Match($block,'(?s)m_Materials:\r?\n\s*- \{[^}]+\}\r?\n\s*- \{fileID: 2100000, guid: ([a-f0-9]+), type: 2\}')
if ($eye.Groups[1].Value -notin @('f0f762e9f37549441b43bea8c6cda6c1','bbc0bc1770b7d734da47c9bc6c381493',$eyeGuid)) { throw '예상하지 않은 눈 재질입니다.' }
$block = $block.Replace($eye.Groups[1].Value,$eyeGuid)
$updated = $text.Substring(0,$match.Index)+$block+$text.Substring($match.Index+$match.Length)
if ($updated -eq $text) { Write-Output '이미 적용됨. 변경 0.'; return }
$backup = Join-Path $projectRoot 'output/P09DaonPortrait/BeforeAssets/Npc_Daon.prefab'
if (-not [IO.File]::Exists($backup)) { [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($backup)) | Out-Null; [IO.File]::WriteAllBytes($backup,$bytes) }
# 전용 UV 메시와 재질을 같은 파일 쓰기에서 교체해 중간 불일치를 피한다.
try { [IO.File]::WriteAllText($path,$updated,(New-Object Text.UTF8Encoding($false))) }
catch { [IO.File]::WriteAllBytes($path,$bytes); throw }
Write-Output 'Daon 얼굴 메시·피부·눈 재질 적용. 헤어·본·다른 렌더러 보존.'
