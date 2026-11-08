"""Unity를 실행하거나 에셋을 저장하지 않고 조사용 Terrain 표본을 추출한다."""
from pathlib import Path
import hashlib
import json
import struct
import sys

import numpy as np

folder = Path('output/TerrainBoundaryAudit')
sys.path.insert(0, str(folder / 'pydeps'))
import UnityPy

asset = Path('Assets/10.Datas/World/OpenWorld/Terrain.asset')
scene = Path('Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity')
hashes = {str(p): hashlib.sha256(p.read_bytes()).hexdigest() for p in [asset, scene]}

# 최초 조사에서 같은 Unity 버전의 GetHeights 1,050,625개와 32766 정규화를 전수 대조했다.
# 대조 원본이 남아 있을 때만 재검사하고, 재실행에서 과거 검사를 수행한 것처럼 기록하지 않는다.
error = None
reference_path = folder / 'lake-heights.bin'
old_asset = Path('Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/Terrain.asset')
if reference_path.exists() and old_asset.exists():
    reference = np.frombuffer(reference_path.read_bytes(), dtype='<f4', offset=28)
    old_env = UnityPy.load(str(old_asset))
    old = next(o for o in old_env.objects if o.type.name == 'TerrainData').read_typetree()['m_Heightmap']
    error = float(np.max(np.abs(np.asarray(old['m_Heights'], dtype=np.float64) / 32766 - reference)))
    assert error < 1e-7, error
    del old_env, old, reference

env = UnityPy.load(str(asset))
data = next(o for o in env.objects if o.type.name == 'TerrainData').read_typetree()
heightmap = data['m_Heightmap']
n = heightmap['m_Resolution']
scale = heightmap['m_Scale']
recipe = json.loads(Path('Tools/OpenWorld/city-layout.json').read_text(encoding='utf-8-sig'))['landscape']
origin = recipe['worldOrigin']
size = [scale['x'] * (n - 1), scale['y'], scale['z'] * (n - 1)]
assert size == [2400, 400, 2800]
heights = np.asarray(heightmap['m_Heights'], dtype=np.float32) / np.float32(32766)
with (folder / 'world-heights.bin').open('wb') as out:
    out.write(struct.pack('<i6f', n, origin['x'], origin['y'], origin['z'], *size))
    out.write(heights.astype('<f4').tobytes())
holes = np.asarray(heightmap['m_Holes'], dtype=np.uint8)
(folder / 'world-holes.bin').write_bytes(holes.tobytes())
textures = {o.path_id: o for o in env.objects if o.type.name == 'Texture2D'}
for i, tex in enumerate(data['m_SplatDatabase']['m_AlphaTextures']):
    textures[tex['m_PathID']].read().image.save(folder / f'world-alpha-{i}.png')
for p in [asset, scene]:
    assert hashlib.sha256(p.read_bytes()).hexdigest() == hashes[str(p)], f'조사 도중 원본 변경: {p}'
(folder / 'world-provenance.json').write_text(json.dumps({'scene': str(scene), 'terrain': str(asset), 'sha256': hashes, 'resolution': n, 'origin': origin, 'size': size, 'unity_reference_max_error': error, 'holes_count': int(np.count_nonzero(holes == 0)), 'layers': data['m_SplatDatabase']['m_TerrainLayers'], 'read_method': 'UnityPy 읽기 전용 역직렬화. Unity GetHeights 대조 표본 1050625개와 정규화 단위 검증.'}, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({'resolution': n, 'size': size, 'reference_error': error, 'holes': int(np.count_nonzero(holes == 0))}))
