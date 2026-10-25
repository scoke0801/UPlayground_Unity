"""새 얼굴 앞면을 보존하고 두피를 P09 헤드 표면에 맞춘다."""
import bpy
import json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

root = Path('C:/UsingProject/UnityProject/UPlayground')
folder = root / 'output/DaonP09Compatibility'
points = json.loads((folder / 'unfitted_vertices.json').read_text())
surface = json.loads((folder / 'native_surface.json').read_text())
triangles = [surface['triangles'][i:i+3] for i in range(0, len(surface['triangles']), 3)]
bvh = BVHTree.FromPolygons([Vector(p) for p in surface['vertices']], triangles, all_triangles=True)

def smooth(a, b, x):
    t = max(0.0, min(1.0, (x-a)/(b-a)))
    return t*t*(3-2*t)

fitted = []
face_vertices = set(surface['customTriangles'])
for index, point in enumerate(points):
    p = Vector(point)
    mask = smooth(1.425, 1.46, p.z)
    if p.y > .012:
        mask = max(mask, smooth(1.37, 1.405, p.z))
    # 눈·하이라이트는 두피 표면으로 끌어당기지 않는다.
    if mask and index in face_vertices:
        hit = bvh.find_nearest(p)
        if hit[0] is not None:
            p = p.lerp(hit[0], mask)
    fitted.append(list(p))

(folder / 'fitted_vertices.json').write_text(json.dumps(fitted), encoding='utf-8')
scene = bpy.data.scenes.new('Daon_P09_HeadFit')
for name, vertices, faces in [('P09_NativeSurface', surface['vertices'], triangles),
                               ('Daon_FittedHead', fitted, [surface['customTriangles'][i:i+3] for i in range(0, len(surface['customTriangles']), 3)])]:
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    scene.collection.objects.link(obj)
path = root / 'ArtSource/ExternalCharacterBuilder/DaonFace/P09_HeadFit.blend'
bpy.context.window.scene = scene
bpy.ops.wm.save_as_mainfile(filepath=str(path), compress=True)
result = {'vertices': len(fitted), 'saved': str(path), 'scene': scene.name}
