"""작은 새 모델, UV, 본과 대기·비행·활공 클립을 재생성한다."""
import bpy
import math
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/08.Models/Wildlife'
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.render.fps = 30
texture = bpy.data.images.load(str(ROOT / 'Assets/04.Images/Environment/Wildlife/Songbird_Feathers.png'))
mat = bpy.data.materials.new('Songbird_Feathers')
mat.use_nodes = True
node = mat.node_tree.nodes.new('ShaderNodeTexImage')
node.image = texture
mat.node_tree.links.new(node.outputs['Color'], mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
mat.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value = 0.8
dark = bpy.data.materials.new('Songbird_EyesBeak')
dark.diffuse_color = (0.018, 0.025, 0.035, 1)
dark.use_nodes = True
dark.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value = dark.diffuse_color
dark.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value = 0.32
parts = []
def ellipsoid(name, location, scale, quadrant, bone, material=mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=10, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(material)
    for poly in obj.data.polygons: poly.use_smooth = True
    if quadrant is not None:
        for uv in obj.data.uv_layers.active.data:
            uv.uv = (quadrant[0] + 0.03 + uv.uv.x * 0.44, quadrant[1] + 0.03 + uv.uv.y * 0.44)
    group = obj.vertex_groups.new(name=bone)
    group.add(list(range(len(obj.data.vertices))), 1, 'REPLACE')
    parts.append(obj)
    return obj

# Blender -Y 정면, Z 위. Unity에서는 +Z 정면이 된다.
ellipsoid('Body', (0,0,0.16), (.085,.135,.09), (0,.5), 'Body')
ellipsoid('Breast', (0,-.057,.145), (.072,.091,.074), (.5,.5), 'Body')
ellipsoid('Head', (0,-.103,.242), (.067,.065,.067), (.5,0), 'Head')
for side in [-1,1]:
    ellipsoid('Eye', (side*.055,-.137,.258), (.011,.009,.011), None, 'Head', dark)
    wing=ellipsoid('Wing', (side*.145,.017,.17), (.118,.072,.018), (0,0), 'Wing.L' if side < 0 else 'Wing.R')
    wing.rotation_euler.z = side * -.2
    for i in range(5):
        feather=ellipsoid('Primary', (side*(.19+i*.012),.055+i*.007,.166), (.056,.017,.007), (0,0), 'Wing.L' if side < 0 else 'Wing.R')
        feather.rotation_euler.z=side*(.35+i*.1)
    ellipsoid('Foot', (side*.032,-.04,.05), (.012,.037,.008), None, 'Body', dark)
for side in [-1,1]:
    tail=ellipsoid('Tail', (side*.025,.151,.156), (.026,.095,.012), (0,0), 'Tail')
    tail.rotation_euler.x=.12
bpy.ops.mesh.primitive_cone_add(vertices=12, radius1=.024, radius2=0, depth=.065, location=(0,-.178,.239), rotation=(math.pi/2,0,0))
beak=bpy.context.object
beak.name='Beak'
beak.data.materials.append(dark)
beak.vertex_groups.new(name='Head').add(list(range(len(beak.data.vertices))),1,'REPLACE')
parts.append(beak)
bpy.ops.object.select_all(action='DESELECT')
for obj in parts: obj.select_set(True)
bpy.context.view_layer.objects.active=parts[0]
bpy.ops.object.join()
mesh=bpy.context.object
mesh.name='Songbird_Mesh'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
bpy.ops.object.armature_add()
rig=bpy.context.object
rig.name='Songbird_Rig'
bpy.ops.object.mode_set(mode='EDIT')
root=rig.data.edit_bones[0]
root.name='Root'; root.head=(0,0,0); root.tail=(0,0,.06)
for name, head, tail, parent in [
 ('Body',(0,0,.1),(0,0,.2),'Root'),
 ('Head',(0,-.09,.21),(0,-.09,.3),'Body'),
 ('Wing.L',(-.065,0,.17),(-.26,0,.17),'Body'),
 ('Wing.R',(.065,0,.17),(.26,0,.17),'Body'),
 ('Tail',(0,.08,.16),(0,.23,.16),'Body')]:
    bone=rig.data.edit_bones.new(name); bone.head=head; bone.tail=tail; bone.parent=rig.data.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT')
mesh.parent=rig
mesh.modifiers.new('Armature','ARMATURE').object=rig
rig.animation_data_create()
for name, end in [('Idle',60),('Fly',20),('Glide',60)]:
    action=bpy.data.actions.new(name); action.use_fake_user=True
    rig.animation_data.action=action
    for frame in range(1,end+2):
        phase=(frame-1)/end*math.tau
        for bone in rig.pose.bones:
            bone.rotation_mode='XYZ'; bone.rotation_euler=(0,0,0); bone.location=(0,0,0)
        for side, bone_name in [(-1,'Wing.L'),(1,'Wing.R')]:
            angle=(.72*math.sin(phase) if name=='Fly' else (.05*math.sin(phase) if name=='Glide' else -1.12))
            # 좌우 본의 로컬 축이 반대이므로 같은 부호로 회전해야 양 날개가 함께 올라간다.
            rig.pose.bones[bone_name].rotation_euler.x=angle
        rig.pose.bones['Head'].rotation_euler.y=.12*math.sin(phase) if name=='Idle' else .03*math.sin(phase)
        rig.pose.bones['Tail'].rotation_euler.x=.07*math.sin(phase)
        rig.pose.bones['Body'].location.z=.003*math.sin(phase)
        for bone in rig.pose.bones:
            bone.keyframe_insert('rotation_euler',frame=frame)
            bone.keyframe_insert('location',frame=frame)
    track=rig.animation_data.nla_tracks.new(); track.name=name
    track.strips.new(name,1,action)
rig.animation_data.action=None
scene.frame_start=1; scene.frame_end=61; scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT'); rig.select_set(True); mesh.select_set(True)
bpy.context.view_layer.objects.active=rig
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.export_scene.fbx(filepath=str(OUT/'WoodlandSongbird.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_nla_strips=True,bake_anim_use_all_actions=False,axis_forward='-Z',axis_up='Y')
for track in rig.animation_data.nla_tracks: track.mute=True
rig.animation_data.action=bpy.data.actions['Fly']; scene.frame_set(5)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource/Wildlife/WoodlandSongbird.blend'))
# 기존 Blender 세션과 별개인 배경 프로세스에서만 미리보기를 만든다.
world=scene.world or bpy.data.worlds.new('World'); scene.world=world; world.color=(.2,.2,.2)
bpy.ops.object.camera_add(location=(.65,-.85,.55)); cam=bpy.context.object
cam.rotation_euler=(Vector((0,0,.16))-cam.location).to_track_quat('-Z','Y').to_euler(); cam.data.type='ORTHO'; cam.data.ortho_scale=.75; scene.camera=cam
bpy.ops.object.light_add(type='AREA',location=(0,-1,2)); bpy.context.object.data.energy=120; bpy.context.object.data.shape='DISK'; bpy.context.object.data.size=3
scene.render.engine='CYCLES'; scene.cycles.samples=24
scene.render.resolution_x=800; scene.render.resolution_y=800; scene.render.resolution_percentage=100
scene.render.filepath=str(ROOT/'output/Wildlife/songbird_preview.png')
bpy.ops.render.render(write_still=True)
print('WILDLIFE_EXPORT_OK',len(mesh.data.vertices),len(mesh.data.polygons))
