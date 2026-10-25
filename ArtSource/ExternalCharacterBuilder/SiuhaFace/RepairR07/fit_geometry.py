"""얼굴 크기는 보존하고 목 접합 곡면과 전체 헤어의 두상 맞춤을 저작한다."""
import bpy, json, struct
import numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT = Path(__file__).resolve().parent
PARAMS = json.loads((ROOT / 'fit_parameters.json').read_text())

def read_string(stream):
    length = shift = 0
    while True:
        value = stream.read(1)[0]
        length |= (value & 127) << shift
        if value < 128:
            break
        shift += 7
    return stream.read(length).decode('utf-8')

def read_mesh(path, face=False):
    with path.open('rb') as stream:
        count, slots = struct.unpack('<ii', stream.read(8))
        shapes = struct.unpack('<i', stream.read(4))[0] if face else 0
        vertices = np.frombuffer(stream.read(count * 12), '<f4').reshape(-1, 3).copy()
        uv = np.frombuffer(stream.read(count * 8), '<f4').reshape(-1, 2).copy()
        triangles = []
        for _ in range(slots):
            size = struct.unpack('<i', stream.read(4))[0]
            triangles.append(np.frombuffer(stream.read(size * 4), '<i4').reshape(-1, 3).copy())
        deltas = [(read_string(stream), np.frombuffer(stream.read(count * 12), '<f4').reshape(-1, 3).copy()) for _ in range(shapes)]
        weights = [struct.unpack('<4i4f', stream.read(32)) for _ in range(count)]
    return vertices, uv, triangles, deltas, weights

def smooth(start, end, values):
    t = np.clip((values - start) / (end - start), 0, 1)
    return t * t * (3 - 2 * t)

def write_positions(name, vertices):
    assert np.isfinite(vertices).all()
    with (ROOT / (name + '_positions.bin')).open('wb') as stream:
        stream.write(struct.pack('<i', len(vertices)))
        stream.write(vertices.astype('<f4').tobytes())

vertices, uv, triangles, deltas, weights = read_mesh(ROOT.parent / 'RepairR02/repaired_face.bin', True)
assert len(vertices) == 11560 and len(deltas) == 36, 'R02 원본 계약이 변경되었습니다.'
before = vertices.copy()
# 턱 끝과 얼굴은 유지하며, 몸체보다 안쪽으로 들어간 상부 목의 단면을 연결한다.
neck = vertices[:11291]
fade = 1 - smooth(1.36, 1.385, neck[:, 2])
front = smooth(-.035, -.003, neck[:, 1]) * (1 - smooth(.012, .035, neck[:, 1]))
back = smooth(.025, .045, neck[:, 1])
neck[:, 1] += fade * (-PARAMS['neckFrontExtension'] * front + PARAMS['neckBackExtension'] * back)
# 몸 경계 16점은 유지하고, 노멀 기반 베지어의 부풀음만 제거한다.
top = vertices[11271:11291]
bottom = vertices[-16:]
def angles(ring):
    return np.mod(np.arctan2(ring[:, 0], .025 - ring[:, 1]), 2 * np.pi)

def sample_ring(ring, angle):
    values = angles(ring)
    order = np.argsort(values)
    values = values[order]
    extended = np.concatenate([values[-1:] - 2 * np.pi, values, values[:1] + 2 * np.pi])
    points = ring[np.concatenate([order[-1:], order, order[:1]])]
    return np.array([np.interp(angle, extended, points[:, axis]) for axis in range(3)])

for index in range(11291, 11399):
    ring_index = (index - 11291) // 36
    fraction = (ring_index + 1) / 4
    angle = angles(vertices[index:index+1])[0]
    vertices[index] = sample_ring(top, angle) * (1 - fraction) + sample_ring(bottom, angle) * fraction
write_positions('Face', vertices)

surface = BVHTree.FromPolygons([Vector(point) for point in vertices], np.concatenate([triangles[0], triangles[2]]).tolist(), all_triangles=True)
center = Vector((0, .018, 1.435))
report = {'neckMaxDisplacement': float(np.linalg.norm(vertices - before, axis=1).max()), 'hair': {}}
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

def add_mesh(name, points, tex, faces):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(points.tolist(), [], np.concatenate(faces).tolist())
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    layer = mesh.uv_layers.new(name='UVMap')
    for loop in mesh.loops:
        layer.data[loop.index].uv = tex[loop.vertex_index]
    for polygon in mesh.polygons:
        polygon.use_smooth = True
    return obj

def add_weights(obj, names, values):
    for name in names:
        obj.vertex_groups.new(name=name)
    for index, row in enumerate(values):
        assert abs(sum(row[4:]) - 1) < .0001
        for bone, weight in zip(row[:4], row[4:]):
            if weight > 0:
                obj.vertex_groups[bone].add([index], weight, 'REPLACE')

face = add_mesh('Siuha_Head_Neck', vertices, uv, triangles)
add_weights(face, (ROOT/'Face_bones.txt').read_text(encoding='utf-8-sig').splitlines(), weights)
face.shape_key_add(name='Basis')
for name, delta in deltas:
    face.shape_key_add(name=name).data.foreach_set('co', (vertices + delta).ravel())

def fit_envelope(source):
    fitted = source.copy()
    fitted[:, 2] -= PARAMS['crownLowering'] * smooth(PARAMS['crownStart'], PARAMS['crownEnd'], source[:, 2])
    fitted[:, 1] += PARAMS['frontInset'] * smooth(PARAMS['frontStart'], PARAMS['frontEnd'], source[:, 1])
    fitted[:, 1] -= PARAMS['backInset'] * smooth(.03, .10, source[:, 1])
    fitted[:, 0] -= np.sign(source[:, 0]) * PARAMS['sideInset'] * smooth(.04, .09, np.abs(source[:, 0]))
    return fitted

reference_points, reference_faces = [], []
for name in ['Female_Face_01', 'SkinHead_Female']:
    points, _, faces, _, _ = read_mesh(ROOT / (name + '.bin'))
    reference_faces.extend((np.concatenate(faces) + len(reference_points)).tolist())
    reference_points.extend(fit_envelope(points).tolist())
reference_surface = BVHTree.FromPolygons([Vector(p) for p in reference_points], reference_faces, all_triangles=True)

for path in sorted(ROOT.glob('Hair_??.bin')):
    source, tex, faces, _, hair_weights = read_mesh(path)
    fitted = fit_envelope(source)
    # 모든 가닥 층에 같은 두상 변위를 적용하여 개별 표면 투영의 겹면·Z-fighting을 막는다.
    for index, point in enumerate(fitted):
        if point[2] < 1.425:
            continue
        direction = (Vector(point) - center).normalized()
        hit = surface.ray_cast(center, direction)
        reference_hit = reference_surface.ray_cast(center, direction)
        if hit[0] is None or reference_hit[0] is None:
            continue
        distance = (Vector(point) - center).length
        required = (hit[0] - center).length + PARAMS['scalpClearance']
        shift = max(0, required - (reference_hit[0] - center).length)
        fitted[index] = np.array(center + direction * (distance + shift))
    write_positions(path.stem, fitted)
    obj = add_mesh(path.stem, fitted, tex, faces)
    add_weights(obj, (ROOT/(path.stem+'_bones.txt')).read_text(encoding='utf-8-sig').splitlines(), hair_weights)
    obj.hide_render = path.stem != 'Hair_03'
    obj.hide_set(path.stem != 'Hair_03')
    report['hair'][path.stem] = {'vertices': len(source), 'maxDisplacement': float(np.linalg.norm(fitted-source, axis=1).max())}

bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / 'Siuha_AllHair_Neck.blend'), compress=True)
(ROOT / 'geometry_report.json').write_text(json.dumps(report, indent=2))
print(json.dumps(report))
