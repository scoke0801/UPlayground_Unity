"""LakeVillageWorld 전체 높이 표본을 조사하고 수정 후보의 좌표를 기록한다."""
from collections import deque
from pathlib import Path
import csv
import json
import struct

import numpy as np
from PIL import Image

folder = Path('output/TerrainBoundaryAudit')
raw = (folder / 'world-heights.bin').read_bytes()
n, ox, oy, oz, sx, sy, sz = struct.unpack_from('<i6f', raw)
assert n == 4097 and (ox, oy, oz, sx, sy, sz) == (0, 0, -700, 2400, 400, 2800)
assert len(raw) == 28 + n * n * 4
h = np.frombuffer(raw, dtype='<f4', offset=28).reshape(n, n) * sy + oy
dx, dz = sx / (n - 1), sz / (n - 1)
gx = np.maximum(np.abs(h[:-1, 1:] - h[:-1, :-1]), np.abs(h[1:, 1:] - h[1:, :-1])) / dx
gz = np.maximum(np.abs(h[1:, :-1] - h[:-1, :-1]), np.abs(h[1:, 1:] - h[:-1, 1:])) / dz
slope = np.degrees(np.arctan(np.hypot(gx, gz)))
step = np.maximum(gx * dx, gz * dz)
holes = np.frombuffer((folder / 'world-holes.bin').read_bytes(), dtype=np.uint8).reshape(n - 1, n - 1)
slope[holes == 0] = 0
cell = 4
cols, rows = int(sx / cell), int(sz / cell)
zz, xx = np.nonzero(slope >= 55)
cx, cz = np.floor(xx * dx / cell).astype(int), np.floor(zz * dz / cell).astype(int)
grid = np.zeros((rows, cols), dtype=np.uint8)
grid[cz, cx] = 1
labels = np.zeros_like(grid, dtype=np.int32)
label = 0
for z, x in zip(*np.nonzero(grid)):
    if labels[z, x]:
        continue
    label += 1
    labels[z, x] = label
    queue = deque([(z, x)])
    while queue:
        z1, x1 = queue.popleft()
        for dz1 in [-1, 0, 1]:
            for dx1 in [-1, 0, 1]:
                z2, x2 = z1 + dz1, x1 + dx1
                if 0 <= z2 < rows and 0 <= x2 < cols and grid[z2, x2] and not labels[z2, x2]:
                    labels[z2, x2] = label
                    queue.append((z2, x2))
sample_labels = labels[cz, cx]
monsters = json.loads(Path('output/OpenWorld/MonsterPopulation/visualization-data.json').read_text(encoding='utf-8-sig'))['monsters']
routes = json.loads(Path('Tools/OpenWorld/city-layout.json').read_text(encoding='utf-8-sig'))['routes']
lake_routes = json.loads(Path('Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario.layout.json').read_text(encoding='utf-8-sig'))['routes']
segments = []
for idx, r in enumerate(routes + lake_routes):
    for a, b in zip(r['points'], r['points'][1:]):
        segments.append((np.array([a['x'], a['z']]), np.array([b['x'], b['z']]), r.get('name', f'월드 경로 {idx + 1}'), r.get('width', r.get('halfWidth', 0))))

def nearest_route(x, z):
    p = np.array([x, z])
    result = None
    for a, b, name, width in segments:
        v = b - a
        f = np.clip(np.dot(p - a, v) / max(np.dot(v, v), 1e-8), 0, 1)
        distance = float(np.linalg.norm(p - a - f * v) - width)
        if result is None or distance < result['distance']:
            result = {'name': name, 'distance': distance}
    return result

def region(x, z):
    if z < 300 and x > 1750: return '항구'
    if z < 300 and 350 < x < 850: return '돌등성'
    if 850 < x < 1180 and 320 < z < 460: return '시작 마을·입문 숲길'
    if 850 < x < 1180 and 460 <= z < 630: return '붉은나무숲·구출/채집 동선'
    if 750 < x < 1280 and 630 <= z < 1150: return '호수·섬·사당'
    if x < 800 and z > 950: return '폐채석장·옛 고갯길'
    if x > 1450 and z > 300: return '동부 그늘숲·보루·역참'
    return '들판·교역로·외곽'

groups = []
for value in range(1, label + 1):
    indices = np.flatnonzero(sample_labels == value)
    zs, xs = zz[indices], xx[indices]
    ss = slope[zs, xs]
    peak = int(np.argmax(ss))
    x, z = float(xs[peak] * dx + ox), float(zs[peak] * dz + oz)
    g = {'area': float(len(xs) * dx * dz), 'maxSlope': float(ss[peak]), 'maxCellStep': float(np.max(step[zs, xs])), 'minX': float(xs.min() * dx + ox), 'maxX': float((xs.max() + 1) * dx + ox), 'minZ': float(zs.min() * dz + oz), 'maxZ': float((zs.max() + 1) * dz + oz), 'peak': [x, float(h[zs[peak], xs[peak]]), z], 'region': region(x, z), 'route': nearest_route(x, z)}
    nearby = min(monsters, key=lambda m: (m['position'][0] - x) ** 2 + (m['position'][2] - z) ** 2)
    g['nearbyEncounter'] = {'path': nearby['path'], 'distance': float(np.hypot(nearby['position'][0] - x, nearby['position'][2] - z))}
    groups.append(g)
groups.sort(key=lambda g: -g['area'])
for i, g in enumerate(groups): g['id'] = f'W{i + 1:03}'
assert abs(sum(g['area'] for g in groups) - len(xx) * dx * dz) < 1e-6
summary = {'scene': 'Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity', 'resolution': n, 'sampleCount': n * n, 'spacing': [dx, dz], 'area45': float(np.count_nonzero(slope >= 45) * dx * dz), 'area55': float(len(xx) * dx * dz), 'area75': float(np.count_nonzero(slope >= 75) * dx * dz), 'groups': groups, 'groupCount': len(groups), 'groupsAtLeast2m2': sum(g['area'] >= 2 for g in groups)}
(folder / 'world-audit.json').write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding='utf-8')
with (folder / 'world-boundaries.csv').open('w', encoding='utf-8-sig', newline='') as out:
    w = csv.writer(out)
    w.writerow(['ID', '지역', '55도이상_평면투영면적_m2', '최대경사', '최대_한셀_높이차_m', '최소X', '최대X', '최소Z', '최대Z', '대표X', '대표Y', '대표Z', '대표점_인접저작경로', '대표점_경로폭밖거리_m'])
    for g in groups: w.writerow([g['id'], g['region'], g['area'], g['maxSlope'], g['maxCellStep'], g['minX'], g['maxX'], g['minZ'], g['maxZ'], *g['peak'], g['route']['name'], g['route']['distance']])

# 55도 미만인 넓은 경사면도 누락되지 않게 20m 격자 후보를 별도로 보관한다.
tz, tx = np.nonzero(slope >= 45)
keys = (np.floor(tz * dz / 20).astype(int) * int(sx / 20) + np.floor(tx * dx / 20).astype(int))
tile_count = np.bincount(keys)
with (folder / 'world-slope45-tiles.csv').open('w', encoding='utf-8-sig', newline='') as out:
    w = csv.writer(out); w.writerow(['minX', 'minZ', 'size_m', 'area45_m2'])
    for key in np.flatnonzero(tile_count): w.writerow([int(key % (sx / 20)) * 20 + ox, int(key // (sx / 20)) * 20 + oz, 20, tile_count[key] * dx * dz])

level = np.clip((h[:-1, :-1] - 40) / 220, 0, 1)
rgb = np.stack([48 + level * 145, 75 + level * 120, 55 + level * 130], axis=2).astype(np.uint8)
rgb[slope >= 45] = [225, 201, 78]
rgb[slope >= 55] = [255, 150, 35]
rgb[slope >= 75] = [230, 35, 40]
Image.fromarray(np.flipud(rgb)).resize((1200, 1400)).save(folder / 'world-slope-map.png')
print(json.dumps({**{k:v for k,v in summary.items() if k != 'groups'}, 'top': groups[:18]}, ensure_ascii=False, indent=2))
