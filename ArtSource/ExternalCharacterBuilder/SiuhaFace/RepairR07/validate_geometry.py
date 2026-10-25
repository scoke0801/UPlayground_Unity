"""헤어 면 내부 표본이 두피 안에 있는지와 바깥 가닥에 가려지는지를 진단한다."""
import bpy, struct, json
import numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT = Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(ROOT / 'Siuha_AllHair_Neck.blend'))
face = bpy.data.objects['Siuha_Head_Neck'].data
surface = BVHTree.FromPolygons([v.co for v in face.vertices], [list(p.vertices) for p in face.polygons], all_triangles=True)
center = Vector((0, .018, 1.435))
report = {}
for obj in sorted([o for o in bpy.data.objects if o.name.startswith('Hair_')], key=lambda o:o.name):
    points = np.array([v.co[:] for v in obj.data.vertices])
    samples = 0
    minimum = 1.0
    intersections = 0
    worst = None
    exposed = 0
    hair_surface = BVHTree.FromPolygons([Vector(p) for p in points], [list(p.vertices) for p in obj.data.polygons], all_triangles=True)
    for polygon in obj.data.polygons:
        vertices = points[list(polygon.vertices)]
        if vertices[:, 2].max() < 1.435:
            continue
        for a in range(5):
            for b in range(5-a):
                point = Vector((vertices[0]*a + vertices[1]*b + vertices[2]*(4-a-b))/4)
                if point.z < 1.435:
                    continue
                direction = (point-center).normalized()
                hit = surface.ray_cast(center,direction)
                if hit[0] is None:
                    continue
                clearance = (point-center).length-(hit[0]-center).length
                if clearance < minimum:
                    minimum = clearance
                    worst = list(point)
                intersections += clearance < -.0005
                if clearance < -.0005:
                    outside = hit[0] + direction * .0001
                    exposed += hair_surface.ray_cast(outside, direction)[0] is None
                samples += 1
    report[obj.name] = {'samples':samples,'minimumScalpClearance':minimum,'buriedSamples':intersections,'uncoveredSamples':exposed,'worstPoint':worst}
(ROOT / 'surface_validation.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report))
