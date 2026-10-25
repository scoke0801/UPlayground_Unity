"""Hichi 원본의 정점·UV·표정을 보존하며 새 얼굴을 P09 좌표로 조형한다."""
import bpy
import json
import struct
import numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT = Path('C:/UsingProject/UnityProject/UPlayground')
OUT = ROOT / 'ArtSource/ExternalCharacterBuilder/HichiFace'
data = json.loads((OUT / 'source.json').read_text(encoding='utf-8-sig'))
raw = np.array(data['vertices'], dtype=np.float64)
uv = np.array(data['uv'])
native = np.array(data['nativeVertices'])
surface = BVHTree.FromPolygons([Vector(p) for p in native], np.array(data['nativeTriangles']).reshape(-1, 3).tolist(), all_triangles=True)

def smooth(a, b, x):
    t = np.clip((x-a)/(b-a), 0, 1)
    return t*t*(3-2*t)

def align(points):
    return np.column_stack((points[:, 0]*.94, -points[:, 2]*.94, data['nativeHead'][2]-.019+(points[:, 1]-1.22)*.98))

def sculpt(points):
    p = points.copy()
    x, y, z = points.T
    front = smooth(.005, .040, z)
    eye = np.exp(-((np.abs(x)-.040)/.033)**4-((y-1.298)/.023)**4)*front
    p[:, 1] += (-.18*(y-1.298)+.0015*smooth(.030,.065,np.abs(x)))*eye
    p[:, 0] -= np.sign(x)*.001*eye
    brow = np.exp(-((y-1.330)/.012)**2)*front
    p[:, 1] -= .0035*brow
    cheek = np.exp(-((y-1.261)/.022)**2)*front
    p[:, 0] *= 1-.06*cheek
    p[:, 2] -= .0015*cheek
    jaw = np.exp(-((y-1.225)/.012)**2)*front
    p[:, 0] += .003*np.tanh(x/.014)*jaw
    p[:, 1] += .003*jaw
    nose = np.exp(-(x/.010)**2-((y-1.268)/.014)**2)*front
    p[:, 2] += .004*nose
    mouth = np.exp(-(x/.026)**4-((y-1.246)/.009)**4)*front
    p[:, 0] *= 1+.18*mouth
    p[:, 1] += .001*mouth
    return align(p)

base = sculpt(raw)
offsets = np.zeros_like(base)
for i, p in enumerate(base):
    # 피부 UV만 맞춰 눈·눈썹·입안과 하이라이트의 조형을 보존한다.
    if uv[i, 1] >= .49:
        continue
    mask = float(smooth(1.465, 1.495, p[2]))
    if p[1] > .005:
        mask = max(mask, float(smooth(1.37, 1.405, p[2])))
    if mask:
        hit = surface.find_nearest(Vector(p))
        if hit[0] is not None:
            offsets[i] = (np.array(hit[0])-p)*mask
base += offsets
assert np.isfinite(base).all()
scene = bpy.data.scenes.new('Hichi_P09_NewFace')
objects = []
for name, vertices in [('Hichi_SourceAligned', align(raw)), ('Hichi_P09_Sculpt', base)]:
    mesh = bpy.data.meshes.new(name)
    triangles = [t for slot in data['submeshes'] for t in np.array(slot).reshape(-1, 3)[:, ::-1].tolist()]
    mesh.from_pydata(vertices.tolist(), [], triangles)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    scene.collection.objects.link(obj)
    layer = mesh.uv_layers.new(name='UVMap')
    for loop in mesh.loops:
        layer.data[loop.index].uv = uv[loop.vertex_index]
    for polygon in mesh.polygons:
        polygon.use_smooth = True
    objects.append(obj)
objects[0].hide_render = True
objects[0].hide_viewport = True
obj = objects[1]
obj.shape_key_add(name='Basis')
frames = []
for item in data['shapes']:
    delta = np.array(item['delta'])
    if np.max(np.abs(delta)) < 1e-8:
        continue
    shaped = sculpt(raw+delta)+offsets
    key = obj.shape_key_add(name=item['name'])
    key.data.foreach_set('co', shaped.astype(np.float32).ravel())
    frames.append((item['name'], item['weight'], shaped))
mat = bpy.data.materials.new('Hichi_GeneratedFace')
mat.use_nodes = True
img = bpy.data.images.load(str(ROOT/'Assets/08.Models/Characters/HichiP09/Hichi_Face_Albedo.png'), check_existing=True)
tex = mat.node_tree.nodes.new('ShaderNodeTexImage'); tex.image = img
mat.node_tree.links.new(tex.outputs['Color'], mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
obj.data.materials.append(mat)
with (OUT/'sculpt.bin').open('wb') as f:
    f.write(struct.pack('<ii', len(base), len(frames)))
    f.write(base.astype('<f4').tobytes())
    for name, weight, points in frames:
        encoded = name.encode('utf-8')
        f.write(struct.pack('<i', len(encoded))); f.write(encoded); f.write(struct.pack('<f', weight))
        f.write(points.astype('<f4').tobytes())
# 저장 전 결과를 남겨 MCP 연결이 끊겨도 동일 조형을 중복 적용하지 않는다.
report = {'vertices': len(base), 'nonempty_shapes': len(frames), 'sculpt_max_displacement_m': float(np.linalg.norm(sculpt(raw)-align(raw), axis=1).max()), 'native_bones': len(data['bones'])}
(OUT/'sculpt_report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
bpy.data.libraries.write(str(OUT/'Hichi_P09_Face.blend'), {scene}, fake_user=True, compress=True)
result = report
