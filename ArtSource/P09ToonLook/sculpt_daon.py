"""열린 Daon 검수 씬의 사본에서 얼굴 표정과 헤어 웨이트를 보존하며 조형한다."""
import bpy
import bmesh
import json
import math
from pathlib import Path
from mathutils import Vector

PROJECT = Path('C:/UsingProject/UnityProject/UPlayground')
OUTPUT = PROJECT / 'output/P09DaonSculpt'
SOURCE = PROJECT / 'ArtSource/P09ToonLook/DaonSculpt'
OUTPUT.mkdir(parents=True, exist_ok=True)
SOURCE.mkdir(parents=True, exist_ok=True)
scene = bpy.data.scenes['Daon_Sculpt_Review']
bpy.context.window.scene = scene

def duplicate(source_name, name):
    existing = bpy.data.objects.get(name)
    if existing:
        return existing
    source = bpy.data.objects[source_name]
    obj = source.copy()
    obj.data = source.data.copy()
    obj.name = name
    scene.collection.objects.link(obj)
    obj.hide_set(False)
    obj.hide_render = False
    return obj

def smoothstep(a, b, x):
    t = max(0.0, min(1.0, (x-a)/(b-a)))
    return t*t*(3-2*t)

face = duplicate('Female_Face_02', 'Daon_Face_Refined')
original_face = bpy.data.objects['Female_Face_02']
hair = duplicate('Hair_03', 'Daon_Hair_Refined')
original_hair = bpy.data.objects['Hair_03']
hair.data=original_hair.data.copy()

# 모든 표정에 같은 변형장을 적용해 눈꺼풀과 안구를 함께 조정한다.
def sculpt_face(co):
    x, y, z = co
    front = 1-smoothstep(-.035, .025, y)
    # 턱 끝을 조금 올리고 아래 볼을 정돈한다. 목 경계에는 영향을 주지 않는다.
    jaw = math.exp(-((z-1.354)/.020)**2) * front
    x *= 1-.025*jaw
    z += .0025*jaw
    cheek = math.exp(-((abs(x)-.041)/.021)**2-((z-1.392)/.017)**2)*front
    y -= .0015*cheek
    # 눈 주변을 함께 세로 확장하여 닫힌 눈과 시선용 메시도 같은 공간으로 옮긴다.
    eye = math.exp(-((abs(x)-.036)/.027)**4-((z-1.410)/.022)**4)*front
    z += (z-1.410)*.07*eye
    return Vector((x,y,z))

for target, source in zip(face.data.shape_keys.key_blocks, original_face.data.shape_keys.key_blocks):
    for dst, src in zip(target.data, source.data):
        dst.co = sculpt_face(src.co)
for dst, src in zip(face.data.vertices, original_face.data.vertices):
    dst.co = sculpt_face(src.co)
face_components=json.loads((OUTPUT/'face_components.json').read_text())
for ids in (face_components[17],face_components[18]):
    for i in ids:
        v=original_face.data.vertices[i].co
        neighbors=[original_face.data.vertices[j].co.z for j in ids if abs(original_face.data.vertices[j].co.x-v.x)<.004]
        delta=(sum(neighbors)/len(neighbors)-v.z)*.35
        face.data.vertices[i].co.z+=delta
        for key in face.data.shape_keys.key_blocks:key.data[i].co.z+=delta

# 원본 정점 위치·UV·본 가중치를 유지하고 곡면의 각만 줄인다.
bm=bmesh.new();bm.from_mesh(hair.data)
bmesh.ops.join_triangles(bm,faces=list(bm.faces),cmp_uvs=True,cmp_seam=True,cmp_sharp=True,angle_face_threshold=.65,angle_shape_threshold=.65)
bm.to_mesh(hair.data);bm.free()
# 헤어만 한 단계 세분한다. 얼굴의 표정 토폴로지는 그대로 둔다.
modifier = hair.modifiers.get('곡선 보강') or hair.modifiers.new('곡선 보강','SUBSURF')
modifier.subdivision_type='CATMULL_CLARK'
modifier.levels=1
modifier.render_levels=1
for poly in hair.data.polygons:
    poly.use_smooth=True
for original in (original_face,original_hair):
    original.hide_render=True
    original.hide_set(True)
for obj in (face,hair):
    obj.hide_render=False
    obj.hide_set(False)
scene.render.filepath=str(OUTPUT/'After_Clay.png')
bpy.context.view_layer.update()
bpy.ops.render.render(write_still=True)
report={
    'face_vertices':len(face.data.vertices),
    'face_expression_count':len(face.data.shape_keys.key_blocks)-1,
    'hair_source_vertices':len(hair.data.vertices),
    'face_max_displacement_m':max((a.co-b.co).length for a,b in zip(face.data.vertices,original_face.data.vertices)),
    'hair_original_vertices':len(original_hair.data.vertices),
    'hair_control_vertex_displacement_m':max((a.co-b.co).length for a,b in zip(hair.data.vertices,original_hair.data.vertices)),
}
(OUTPUT/'sculpt_report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
