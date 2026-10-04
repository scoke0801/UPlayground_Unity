"""블렌더에서 카메라 시험장 원본과 Unity 모델을 생성한다."""
import bpy
import json
import math
import runpy
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "ArtSource/CameraTestMap"
EXPORT = ROOT / "Assets/05.Models/CameraTestMap"
SOURCE.mkdir(parents=True, exist_ok=True)
EXPORT.mkdir(parents=True, exist_ok=True)

# 열려 있던 작업을 보존하고 독립 씬을 만든다.
if bpy.data.scenes.get("CameraTestMap") is not None:
    raise RuntimeError("시험장이 이미 있습니다. 수정 반영은 export_camera_test_map.py를 사용하고, 전체 재생성은 새 블렌더 파일에서 실행하세요.")
scene = bpy.data.scenes.new("CameraTestMap")
bpy.context.window.scene = scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
palette = {
    'Concrete': (0.31, 0.38, 0.43, 1),
    'Floor': (0.12, 0.18, 0.23, 1),
    'Trim': (0.045, 0.07, 0.095, 1),
    'CollisionAmber': (1, 0.43, 0.07, 1),
    'LockOnCyan': (0.05, 0.72, 0.83, 1),
    'ElevationViolet': (0.54, 0.3, 0.86, 1),
    'White': (0.8, 0.86, 0.88, 1),
}
materials = {}
for name, color in palette.items():
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.use_nodes = True
    mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = color
    mat.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = 0.72
    materials[name] = mat

def box(name, position, size, material='Concrete', solid=True):
    """Unity 좌표로 받은 블록을 블렌더 좌표로 변환한다."""
    x, y, z = position
    sx, sy, sz = size
    bpy.ops.mesh.primitive_cube_add(size=1, location=(x, -z, y))
    obj = bpy.context.object
    obj.name = ('COL_' if solid else 'VIS_') + name
    obj.dimensions = (sx, sz, sy)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(materials[material])
    return obj

def label(name, text, position, size=0.8, material='White'):
    x, y, z = position
    curve = bpy.data.curves.new(name, 'FONT')
    curve.body = text
    curve.size = size
    curve.align_x = 'CENTER'
    curve.extrude = 0.002
    obj = bpy.data.objects.new('VIS_' + name, curve)
    scene.collection.objects.link(obj)
    obj.location = (x, -z, y)
    obj.data.materials.append(materials[material])
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.convert(target='MESH')

def ring(name, x, z, radius, material, y=0.025):
    verts, faces = [], []
    for i in range(96):
        a = 2 * math.pi * i / 96
        for r in (radius - 0.045, radius + 0.045):
            verts.append((x + math.cos(a) * r, -z + math.sin(a) * r, y))
    for i in range(96):
        a, b = 2*i, 2*((i+1)%96)
        faces.append((a,a+1,b+1,b))
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    obj = bpy.data.objects.new('VIS_' + name, mesh)
    scene.collection.objects.link(obj)
    mesh.materials.append(materials[material])

box('Foundation', (0,-0.4,0), (64,0.8,64), 'Floor')
for x in (-32,32):
    box('BoundarySide', (x,1.5,0), (0.5,3,64), 'Trim')
for z in (-32,32):
    box('BoundaryEnd', (0,1.5,z), (64,3,0.5), 'Trim')
for n in range(-30,31,5):
    box('GridX', (n,0.012,0), (0.025,0.008,63), 'Concrete', False)
    box('GridZ', (0,0.012,n), (63,0.008,0.025), 'Concrete', False)

label('Title', 'CAMERA / PROVING GROUND', (0,0.025,-28), 1.25)
label('Entry', '01 COLLISION     /     02 LOCK-ON     /     03 ELEVATION', (0,0.025,-25), 0.55)
ring('Spawn',0,-22,1.5,'White')
box('MainRoute',(0,0.022,1),(0.12,0.015,41),'White',False)

# 벽 후진, 90도 코너, 2m/3.5m 복도와 낮은 천장.
label('Collision','01 / COLLISION',(-18,0.025,-18),1,'CollisionAmber')
box('BackWall',(-18,2,-12),(14,4,0.6))
box('CornerWing',(-25,2,-8),(0.6,4,8))
box('BackWallStripe',(-18,3.5,-11.68),(14,0.15,0.035),'CollisionAmber',False)
for x in (-26,-23.4,-19.3):
    box('CorridorWall',(x,2,3),(0.6,4,18))
box('LowCeiling',(-24.7,3.1,5),(3.2,0.35,6))
label('Narrow','2.0 m',(-24.7,0.025,-7),0.48,'CollisionAmber')
label('Wide','3.5 m',(-21.35,0.025,-7),0.48,'CollisionAmber')
box('CornerReturn',(-15,2,11),(8,4,0.6))
box('CornerEnd',(-11,2,7),(0.6,4,8))
for z in (-4,2,8):
    box('ThinOccluder',(-15,1.7,z),(0.35,3.4,2),'Concrete')
for x in (-24.7,-21.35):
    box('CorridorGuide',(x,0.025,3),(0.1,0.018,18),'CollisionAmber',False)

# 락온 기준 원은 벽보다 낮은 장식 메시이며 충돌체를 만들지 않는다.
label('LockOn','02 / LOCK-ON',(16,0.025,-18),1,'LockOnCyan')
for r in (4,8,12):
    ring('Range'+str(r),16,0,r,'LockOnCyan')
box('OcclusionWall',(22,2,6),(5,4,0.65))
box('OcclusionStripe',(22,3.5,5.65),(5,0.15,0.04),'LockOnCyan',False)
for x,z in ((7,4),(27,-5)):
    box('OcclusionColumn',(x,2,z),(1.2,4,1.2))
targets = [
    {'name':'Target_Left','position':{'x':10,'y':0.08,'z':0}},
    {'name':'Target_Center','position':{'x':16,'y':0.08,'z':3}},
    {'name':'Target_Right','position':{'x':22,'y':0.08,'z':0}},
    {'name':'Target_Occluded','position':{'x':22,'y':0.08,'z':10}},
    {'name':'Target_Corridor','position':{'x':-21.35,'y':0.08,'z':10}},
    {'name':'Target_Elevated','position':{'x':1,'y':4.08,'z':24}},
]
for i, target in enumerate(targets):
    p=target['position']
    ring(target['name'],p['x'],p['z'],0.95,'LockOnCyan',p['y']-0.05)
    label('TargetNumber'+str(i),str(i+1),(p['x'],p['y']-0.04,p['z']-1.5),0.6,'LockOnCyan')

label('Elevation','03 / ELEVATION',(0,0.025,14),0.9,'ElevationViolet')
box('UpperDeck',(0,2,25),(12,4,10))
# 4m 상승 / 10m 진행 경사면: 별도 실제 메시로 KCC 경사 진입을 검사한다.
verts=[(-3,-10,0),(3,-10,0),(-3,-20,0),(3,-20,0),(-3,-20,4),(3,-20,4)]
faces=[(4,5,1,0),(3,5,4,2),(1,3,2,0),(2,4,0),(5,3,1)]
mesh=bpy.data.meshes.new('Ramp')
mesh.from_pydata(verts,[],faces)
mesh.update()
obj=bpy.data.objects.new('COL_Ramp',mesh)
scene.collection.objects.link(obj)
mesh.materials.append(materials['ElevationViolet'])
box('UpperBackWall',(0,5.8,30),(12,3.6,0.6))
box('UpperSideWall',(-6,5.8,25),(0.6,3.6,10))
label('UpperLabel','HEIGHT +4 m',(0,4.025,28),0.7,'ElevationViolet')

for z in range(-18,5,2):
    box('DistanceTick',(16,0.028,z),(1,0.018,0.06),'LockOnCyan',False)
label('RangeGuide','16 m ACQUIRE / 20 m RELEASE',(19,0.03,-21),0.48,'LockOnCyan')

export_objects=list(scene.objects)
bpy.ops.object.select_all(action='DESELECT')
for obj in export_objects:
    obj.select_set(True)
bpy.context.view_layer.objects.active=export_objects[0]
runpy.run_path(str(ROOT/'Tools/CameraTestMap/export_camera_test_map.py'))

manifest={'player':{'x':0,'y':0.1,'z':-22},'targets':targets,
          'materials':[{'name':materials[n].name,'color':{'r':v[0],'g':v[1],'b':v[2],'a':v[3]}} for n,v in palette.items()]}
(EXPORT/'CameraTestMap.layout.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')

bpy.ops.object.camera_add(location=(57,65,62))
camera=bpy.context.object
camera.name='OverviewCamera'
camera.rotation_euler=(Vector((0,0,0))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO'
camera.data.ortho_scale=92
scene.camera=camera
bpy.ops.object.light_add(type='AREA',location=(0,5,50))
bpy.context.object.data.energy=65000
bpy.context.object.data.shape='DISK'
bpy.context.object.data.size=45
scene.world=bpy.data.worlds.new('CameraTestWorld')
scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(0.22,0.28,0.36,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value=0.7
scene.render.engine='CYCLES'
scene.cycles.samples=24
scene.render.resolution_x=1400
scene.render.resolution_y=1400
scene.render.resolution_percentage=100
scene.render.filepath=str(SOURCE/'CameraTestMap_overview.png')
# 별도 라이브러리에 이 씬만 저장하여 기존 작업의 저장 경로를 바꾸지 않는다.
bpy.data.libraries.write(str(SOURCE/'CameraTestMap.blend'),{scene})
bpy.ops.render.render(write_still=True)
result={'mesh_count':len(export_objects),'blend':str(SOURCE/'CameraTestMap.blend'),'fbx':str(EXPORT/'CameraTestMap.fbx')}
