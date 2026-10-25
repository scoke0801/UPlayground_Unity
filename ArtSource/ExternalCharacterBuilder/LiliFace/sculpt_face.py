"""Unity 정점 순서를 보존하여 Lili 기반 새 얼굴과 모든 표정을 조형한다."""
import bpy
import json
import struct
import numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT = Path(__file__).resolve().parent

def read_string(stream):
    size = shift = 0
    while True:
        byte = stream.read(1)[0]
        size |= (byte & 127) << shift
        if not byte & 128:
            break
        shift += 7
    return stream.read(size).decode('utf-8')

with (ROOT / 'source.bin').open('rb') as stream:
    def integer():
        return struct.unpack('<i', stream.read(4))[0]
    def array(count, width=3, dtype='<f4'):
        return np.frombuffer(stream.read(count * width * 4), dtype=dtype).copy().reshape(count, width)
    count, slots, shape_count = integer(), integer(), integer()
    source = array(count)
    uv = array(count, 2)
    triangles = [array(integer() // 3, 3, '<i4') for _ in range(slots)]
    shapes = [(read_string(stream), array(count)) for _ in range(shape_count)]
    native = array(integer())
    native_triangles = array(integer() // 3, 3, '<i4')

def smooth(a, b, value):
    t = np.clip((value-a)/(b-a), 0, 1)
    return t*t*(3-2*t)

def sculpt(points):
    result = points.copy()
    x, y, z = points.T
    front = 1-smooth(-.035, .015, y)
    eye = np.exp(-((np.abs(x)-.032)/.027)**4-((z-1.401)/.021)**4)*front
    result[:, 0] += np.sign(x)*.0018*eye
    result[:, 2] += (-(z-1.401)*.13 + .003 + .0015*smooth(.025,.059,np.abs(x)))*eye
    brow = np.exp(-((z-1.424)/.011)**2)*front
    result[:, 2] += .002*brow
    cheek = np.exp(-((z-1.381)/.020)**2)*front
    result[:, 0] *= 1-.045*cheek
    jaw = np.exp(-((z-1.341)/.012)**2)*front
    result[:, 0] += .003*np.tanh(x/.016)*jaw
    result[:, 2] += .003*jaw
    nose = np.exp(-(x/.010)**2-((z-1.381)/.013)**2)*front
    result[:, 1] -= .0035*nose
    mouth = np.exp(-(x/.027)**4-((z-1.359)/.010)**4)*front
    result[:, 0] += .12*x*mouth
    result[:, 2] += .0015*mouth
    return result

shaped = sculpt(source)
surface = BVHTree.FromPolygons([Vector(v) for v in native], native_triangles.tolist(), all_triangles=True)
skin_indices = set(triangles[2].ravel().tolist())
offsets = np.zeros_like(source)
for index in skin_indices:
    p = shaped[index]
    scalp = float(smooth(1.475, 1.505, p[2]))
    if p[1] > .015:
        scalp = max(scalp, float(smooth(1.39, 1.43, p[2])))
    # 얼굴 앞쪽의 턱·입을 목 표면으로 끌어당기지 않는다.
    neck = float(1-smooth(1.335, 1.355, p[2])) * float(smooth(-.035,-.015,p[1]))
    mask = max(scalp, neck)
    if mask:
        hit = surface.find_nearest(Vector(p))
        if hit[0] is not None:
            offsets[index] = (np.array(hit[0])-p)*mask
shaped += offsets
assert np.isfinite(shaped).all()

# 별도 background 프로세스의 빈 씬에서만 작성하여 사용자 Blender 문서를 보존한다.
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
kept_slots = [2, 3, 5, 6]
polygons = [t for slot in kept_slots for t in triangles[slot].tolist()]
objects = []
for name, points in [('Lili_SourceAligned', source), ('Lili_P09_NewFace', shaped)]:
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(points.tolist(), [], polygons)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    layer = mesh.uv_layers.new(name='UVMap')
    for loop in mesh.loops:
        layer.data[loop.index].uv = uv[loop.vertex_index]
    for polygon in mesh.polygons:
        polygon.use_smooth = True
    obj.shape_key_add(name='Basis')
    for key, delta in shapes:
        coordinates = source+delta if name.endswith('Aligned') else sculpt(source+delta)+offsets
        obj.shape_key_add(name=key).data.foreach_set('co', coordinates.ravel())
    objects.append(obj)
objects[0].hide_set(True)
objects[0].hide_render = True
bpy.context.view_layer.objects.active = objects[1]
objects[1].select_set(True)
with (ROOT/'sculpt.bin').open('wb') as stream:
    stream.write(struct.pack('<ii',count,shape_count))
    stream.write(shaped.astype('<f4').tobytes())
    for name, delta in shapes:
        value = sculpt(source+delta)+offsets-shaped
        assert np.isfinite(value).all()
        stream.write(value.astype('<f4').tobytes())
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Lili_P09_Face.blend'), compress=True)
report = {'vertices':count,'expressions':shape_count,'moved_vertices':int(np.count_nonzero(np.linalg.norm(shaped-source,axis=1)>.00001)),
          'sculpt_max_m':float(np.max(np.linalg.norm(sculpt(source)-source,axis=1))),
          'native_min':native.min(axis=0).tolist(),'native_max':native.max(axis=0).tolist(),
          'slots':[{'slot':i,'min':source[np.unique(t)].min(axis=0).tolist(),'max':source[np.unique(t)].max(axis=0).tolist()} for i,t in enumerate(triangles)]}
(ROOT/'sculpt_report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
