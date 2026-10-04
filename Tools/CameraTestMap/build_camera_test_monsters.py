"""카메라 검증용 비행 감시자와 석상 거인을 별도 Blender 씬에서 제작한다."""
import bpy
import bmesh
import json
import math
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'ArtSource/CameraTestMap/Monsters'
EXPORT = ROOT / 'Assets/05.Models/CameraTestMap/Monsters'
SOURCE.mkdir(parents=True, exist_ok=True)
EXPORT.mkdir(parents=True, exist_ok=True)
if bpy.data.scenes.get('CameraTestMonsters'):
    raise RuntimeError('제작 씬이 이미 있습니다. 재생성은 새 Blender 파일에서 실행하세요.')
scene = bpy.data.scenes.new('CameraTestMonsters')
bpy.context.window.scene = scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
scene.render.fps = 24
scene.frame_start, scene.frame_end = 1, 73
palette = {
    'SentinelShell': (0.035, 0.12, 0.19, 1),
    'SentinelWing': (0.10, 0.37, 0.43, 1),
    'AncientStone': (0.22, 0.28, 0.34, 1),
    'StoneEdge': (0.40, 0.48, 0.53, 1),
    'DarkJoint': (0.045, 0.063, 0.085, 1),
    'OldGold': (0.64, 0.36, 0.10, 1),
    'SkyCore': (0.06, 0.9, 1.0, 1),
    'EmberCore': (1.0, 0.33, 0.055, 1),
}
materials = {}
for name, color in palette.items():
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes['Principled BSDF']
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.55
    bsdf.inputs['Metallic'].default_value = 0.35 if name in ('OldGold', 'SentinelShell') else 0.08
    if name.endswith('Core'):
        bsdf.inputs['Emission Color'].default_value = color
        bsdf.inputs['Emission Strength'].default_value = 2.5
    materials[name] = mat

def empty(name, parent=None, location=(0, 0, 0)):
    obj = bpy.data.objects.new(name, None)
    scene.collection.objects.link(obj)
    obj.parent = parent
    obj.location = location
    return obj

def finish(obj, name, scale, material, parent):
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(materials[material])
    obj.parent = parent
    return obj

def stone(name, location, scale, material, parent, subdivisions=1):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1, location=location)
    return finish(bpy.context.object, name, scale, material, parent)

def block(name, location, scale, material, parent, bevel=0.08):
    bpy.ops.mesh.primitive_cube_add(size=2, location=location)
    obj = finish(bpy.context.object, name, scale, material, parent)
    mod = obj.modifiers.new('ChiseledEdges', 'BEVEL')
    mod.width, mod.segments = bevel, 1
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=mod.name)
    return obj

def spike(name, start, end, radius, material, parent):
    a, b = Vector(start), Vector(end)
    bpy.ops.mesh.primitive_cone_add(vertices=6, radius1=radius, radius2=0.015,
                                   depth=(b-a).length, location=(a+b)*0.5)
    obj = bpy.context.object
    obj.rotation_euler = (b-a).to_track_quat('Z', 'Y').to_euler()
    return finish(obj, name, (1, 1, 1), material, parent)

def wing(name, side, parent):
    pivot = empty(name, parent)
    # 두께가 있는 닫힌 메시로 아래쪽 시점에서도 날개가 사라지지 않는다.
    outline = [(0.45,-0.05,1.05),(1.05,-0.6,1.25),(2.85,-0.28,1.12),
               (2.25,0.35,0.94),(1.95,0.12,0.96),(1.52,0.8,0.86),
               (1.32,0.39,0.95),(0.69,0.65,0.95)]
    verts = [(side*x,y,z+dz) for dz in (-0.055,0.055) for x,y,z in outline]
    n = len(outline)
    faces = [tuple(range(n-1,-1,-1)),tuple(range(n,n*2))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    mesh = bpy.data.meshes.new(name+'Mesh')
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    editable = bmesh.new()
    editable.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(editable, faces=list(editable.faces))
    editable.to_mesh(mesh)
    editable.free()
    obj = bpy.data.objects.new(name+'Membrane', mesh)
    scene.collection.objects.link(obj)
    obj.parent = pivot
    obj.data.materials.append(materials['SentinelWing'])
    for i,(x,y,z) in enumerate(outline[1:4]):
        spike(name+'Rib'+str(i),(side*0.55,-0.03,1.1),(side*x,y,z+0.06),0.11,'OldGold',pivot)
    for frame, angle in [(1,0.08),(19,-0.17),(37,0.08),(55,-0.17),(73,0.08)]:
        pivot.rotation_euler.y = side*angle
        pivot.keyframe_insert(data_path='rotation_euler', frame=frame)
    return pivot

air = empty('SkySentinel')
air_body = empty('HoverBody', air)
stone('ArmoredBody',(0,0,0.94),(0.69,1.04,0.54),'SentinelShell',air_body,2)
stone('ForeheadCrest',(0,-0.48,1.35),(0.42,0.61,0.22),'OldGold',air_body)
stone('EyeSocket',(0,-0.92,1.04),(0.42,0.19,0.35),'DarkJoint',air_body,2)
stone('SingleLuminousEye',(0,-1.075,1.06),(0.255,0.075,0.235),'SkyCore',air_body,2)
stone('EyeSlit',(0,-1.145,1.06),(0.05,0.025,0.175),'SentinelShell',air_body)
for side in (-1,1):
    wing('Wing'+('L' if side<0 else 'R'),side,air_body)
    spike('Horn',(side*0.35,-0.35,1.32),(side*0.64,-0.2,1.95),0.16,'StoneEdge',air_body)
    spike('Talon',(side*0.36,-0.15,0.68),(side*0.48,-0.54,0.18),0.13,'OldGold',air_body)
for i in range(4):
    stone('TailSegment'+str(i),(0,0.83+i*0.3,0.85-i*0.13),
          (0.23-i*0.035,0.31,0.20-i*0.025),'SentinelShell',air_body)
spike('TailCrystal',(0,1.82,0.40),(0,2.32,0.13),0.19,'SkyCore',air_body)
for frame,z in [(1,0),(19,0.10),(37,0),(55,-0.10),(73,0)]:
    air_body.location.z=z
    air_body.keyframe_insert(data_path='location',frame=frame)

giant = empty('StoneColossus')
for side in (-1,1):
    block('Foot',(side*0.67,-0.28,0.32),(0.55,0.83,0.32),'AncientStone',giant,0.13)
    stone('Shin',(side*0.67,0.02,1.1),(0.52,0.53,0.82),'StoneEdge',giant,2)
    stone('Knee',(side*0.68,-0.20,1.77),(0.47,0.48,0.39),'OldGold',giant)
    stone('Thigh',(side*0.59,0.10,2.25),(0.60,0.55,0.66),'AncientStone',giant,2)
body = empty('BreathingTorso',giant)
stone('Pelvis',(0,0.1,2.62),(1.02,0.65,0.59),'DarkJoint',body,2)
stone('Torso',(0,0.12,3.53),(1.47,0.80,1.14),'AncientStone',body,2)
for side in (-1,1):
    stone('ChestPlate',(side*0.59,-0.52,3.97),(0.73,0.37,0.57),'StoneEdge',body)
    stone('Shoulder',(side*1.48,0.09,4.07),(0.72,0.75,0.76),'AncientStone',body,2)
    block('ShoulderBand',(side*1.48,-0.08,4.29),(0.62,0.66,0.14),'OldGold',body)
    stone('UpperArm',(side*1.69,0.08,3.35),(0.49,0.49,0.78),'DarkJoint',body,2)
    stone('Forearm',(side*1.9,-0.13,2.72),(0.67,0.63,0.82),'AncientStone',body,2)
    block('Fist',(side*1.94,-0.23,2.03),(0.53,0.58,0.48),'StoneEdge',body,0.15)
    for finger in range(3):
        block('Knuckle',(side*1.94+(finger-1)*0.27,-0.77,2.06),(.11,.1,.29),'OldGold',body,.04)
    spike('ShoulderSpire',(side*1.47,0.3,4.53),(side*1.7,0.37,5.17),0.22,'StoneEdge',body)
block('Neck',(0,0.08,4.62),(.42,.43,.33),'DarkJoint',body)
block('StoneMask',(0,-0.04,5.06),(.60,.54,.54),'StoneEdge',body,0.16)
block('Brow',(0,-0.57,5.21),(.62,.14,.14),'AncientStone',body,.05)
for side in (-1,1):
    block('GlowingEye',(side*.24,-.598,5.08),(.17,.045,.075),'EmberCore',body,.025)
    spike('Crown',(side*.39,.05,5.48),(side*.58,.15,6.12),.21,'OldGold',body)
stone('CoreFrame',(0,-.77,3.52),(.47,.19,.59),'OldGold',body)
stone('ChestCore',(0,-.94,3.55),(.25,.09,.38),'EmberCore',body)
for frame,z in [(1,0),(37,0.055),(73,0)]:
    body.location.z=z
    body.keyframe_insert(data_path='location',frame=frame)

scene.frame_set(1)
for root in (air,giant):
    bpy.ops.object.select_all(action='DESELECT')
    root.select_set(True)
    for obj in root.children_recursive:
        obj.select_set(True)
    bpy.context.view_layer.objects.active=root
    bpy.ops.export_scene.fbx(filepath=str(EXPORT/(root.name+'.fbx')),use_selection=True,
        object_types={'EMPTY','MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,
        bake_space_transform=False,add_leaf_bones=False,bake_anim=True,
        bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0)

manifest = {'materials':[{'name':n,'color':dict(zip(('r','g','b','a'),c)),
    'emission':2.5 if n.endswith('Core') else 0} for n,c in palette.items()],
    'monsters':[
        {'name':'SkySentinel','position':{'x':-9,'y':4.2,'z':1},'isAirborne':True,
         'radius':0.72,'height':1.8,'centerY':0.95},
        {'name':'StoneColossus','position':{'x':11,'y':0.08,'z':21},'isAirborne':False,
         'radius':1.35,'height':5.9,'centerY':2.95}]}
(EXPORT/'Monsters.layout.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')

# 두 모델의 실루엣을 함께 보는 제작 검수 이미지.
air.location=(-4,0,3.7)
giant.location=(2.6,0,0)
world=bpy.data.worlds.new('MonsterStudioWorld')
world.use_nodes=True
world.node_tree.nodes['Background'].inputs[0].default_value=(.07,.10,.15,1)
world.node_tree.nodes['Background'].inputs[1].default_value=.5
scene.world=world
for name,loc,power,size in [('Key',(-5,-8,11),1800,7),('Rim',(4,5,9),2200,6),('Fill',(7,-4,6),1100,5)]:
    light=bpy.data.lights.new(name,'AREA'); light.energy=power; light.shape='DISK'; light.size=size
    obj=bpy.data.objects.new(name,light); scene.collection.objects.link(obj); obj.location=loc
    obj.rotation_euler=(Vector((0,0,3))-obj.location).to_track_quat('-Z','Y').to_euler()
camera=bpy.data.cameras.new('ReviewCamera')
obj=bpy.data.objects.new('ReviewCamera',camera); scene.collection.objects.link(obj)
obj.location=(9,-19,11)
obj.rotation_euler=(Vector((-.6,0,3.1))-obj.location).to_track_quat('-Z','Y').to_euler()
camera.type='ORTHO'; camera.ortho_scale=14.6; scene.camera=obj
scene.render.engine='CYCLES'; scene.cycles.samples=32
scene.render.resolution_x=1440; scene.render.resolution_y=900; scene.render.resolution_percentage=100
scene.render.filepath=str(SOURCE/'CameraTestMonsters_preview.png')
bpy.ops.render.render(write_still=True)
# 원본은 검수 배치를 유지하며, 재수출 스크립트가 각 모델을 임시로 원점에 놓는다.
bpy.data.libraries.write(str(SOURCE/'CameraTestMonsters.blend'),{scene},fake_user=True)
result={'models':['SkySentinel','StoneColossus'],'source':str(SOURCE),'export':str(EXPORT),
        'triangles':sum(len(p.vertices)-2 for obj in scene.objects if obj.type=='MESH' for p in obj.data.polygons)}
