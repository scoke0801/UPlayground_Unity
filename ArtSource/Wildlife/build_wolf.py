"""환경 늑대의 연속 표면, 리그와 제자리 기본 모션을 제작한다."""
import bpy
import math
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.render.fps = 30
parts = []
def ellipsoid(name, p, scale):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20, ring_count=12, location=p)
    obj = bpy.context.object
    obj.name = name; obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    parts.append(obj)
    return obj
def segment(name, a, b, radius, width=None):
    a,b = Vector(a),Vector(b)
    obj=ellipsoid(name,(a+b)/2,(radius,width or radius,(b-a).length/2+radius*.5))
    obj.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
    return obj

ellipsoid('Ribcage',(0,-.13,.78),(.235,.46,.28))
ellipsoid('Waist',(0,.22,.76),(.17,.29,.2))
ellipsoid('Haunch',(0,.4,.77),(.23,.24,.25))
segment('Neck',(0,-.4,.78),(0,-.55,1.02),.21)
ellipsoid('Ruff',(0,-.39,.91),(.25,.23,.25))
ellipsoid('Skull',(0,-.65,1.06),(.175,.225,.185))
ellipsoid('Cheek',(0,-.68,.98),(.175,.17,.14))
ellipsoid('Muzzle',(0,-.87,1.005),(.108,.22,.092))
ellipsoid('Jaw',(0,-.84,.94),(.092,.2,.047))
for side in [-1,1]:
    x=side*.115
    verts=[(x-.075,-.66,1.15),(x+.075,-.66,1.15),(x+side*.025,-.61,1.40),
           (x-.06,-.48,1.15),(x+.06,-.48,1.15),(x+side*.025,-.55,1.36)]
    mesh=bpy.data.meshes.new('Ear');mesh.from_pydata(verts,[],[(0,1,2),(3,5,4),(0,3,4,1),(1,4,5,2),(2,5,3,0)])
    obj=bpy.data.objects.new('Ear',mesh);bpy.context.collection.objects.link(obj);parts.append(obj)
    # 앞다리와 뒷다리의 관절 방향을 다르게 만들어 개과 실루엣을 유지한다.
    x=side*.19
    segment('FrontUpper',(x,-.34,.82),(x,-.27,.45),.085)
    segment('FrontLower',(x,-.27,.45),(x,-.39,.11),.052)
    ellipsoid('FrontPaw',(x,-.43,.055),(.075,.12,.055))
    segment('Thigh',(x,.37,.79),(x,.20,.47),.13)
    segment('HindShin',(x,.20,.47),(x,.49,.24),.064)
    segment('Hock',(x,.49,.24),(x,.43,.1),.045)
    ellipsoid('HindPaw',(x,.39,.05),(.07,.11,.05))
segment('TailBase',(0,.52,.83),(0,.84,.65),.115)
segment('TailBrush',(0,.77,.69),(0,1.1,.4),.14)
segment('TailTip',(0,1.02,.47),(0,1.19,.27),.07)

bpy.ops.object.select_all(action='DESELECT')
for obj in parts:obj.select_set(True)
bpy.context.view_layer.objects.active=parts[0]
bpy.ops.object.join(); body=bpy.context.object;body.name='Wolf_Body'
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
mod=body.modifiers.new('ContinuousSurface','REMESH');mod.mode='VOXEL';mod.voxel_size=.017
bpy.ops.object.modifier_apply(modifier=mod.name)
mod=body.modifiers.new('SmoothSurface','SMOOTH');mod.factor=.7;mod.iterations=4
bpy.ops.object.modifier_apply(modifier=mod.name)
mod=body.modifiers.new('GameMesh','DECIMATE');mod.ratio=.36
bpy.ops.object.modifier_apply(modifier=mod.name)
for poly in body.data.polygons:poly.use_smooth=True
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(island_margin=.015)
bpy.ops.object.mode_set(mode='OBJECT')
image=bpy.data.images.load(str(ROOT/'Assets/04.Images/Environment/Wildlife/Wolf_Fur.png'));image.pack()
fur=bpy.data.materials.new('Wolf_Fur');fur.use_nodes=True
node=fur.node_tree.nodes.new('ShaderNodeTexImage');node.image=image
fur.node_tree.links.new(node.outputs['Color'],fur.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
fur.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.9
body.data.materials.append(fur)
for poly in body.data.polygons:
    # 작은 UV 섬마다 털 무늬가 커지지 않도록 몸통 길이·높이에 맞춘 연속 투영을 쓴다.
    for index in poly.loop_indices:
        p=body.data.vertices[body.data.loops[index].vertex_index].co
        uv=body.data.uv_layers.active.data[index].uv
        uv.x=.025+max(0,min(1,(p.y+1.15)/2.4))*.95
        uv.y=.025+max(0,min(1,p.z/1.45))*.95

def solid(name,color,roughness):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    m.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(*color,1)
    m.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=roughness
    return m
dark=solid('Wolf_Nose',(.018,.022,.026),.42)
amber=solid('Wolf_Eyes',(.55,.25,.035),.22)
details=[]
for side in [-1,1]:
    eye=ellipsoid('Eye',(side*.144,-.766,1.091),(.024,.025,.021));eye.data.materials.append(amber);details.append(eye)
    pupil=ellipsoid('Pupil',(side*.158,-.781,1.092),(.012,.012,.014));pupil.data.materials.append(dark);details.append(pupil)
nose=ellipsoid('Nose',(0,-1.067,1.012),(.083,.055,.057));nose.data.materials.append(dark);details.append(nose)
mouth=ellipsoid('MouthLine',(0,-.956,.945),(.091,.124,.008));mouth.data.materials.append(dark);details.append(mouth)

bpy.ops.object.armature_add();rig=bpy.context.object;rig.name='WoodlandWolf_Rig'
bpy.ops.object.mode_set(mode='EDIT')
root=rig.data.edit_bones[0];root.name='Root';root.head=(0,0,0);root.tail=(0,0,.2);root.use_deform=False
def bone(name,a,b,parent,deform=True):
    b0=rig.data.edit_bones.new(name);b0.head=a;b0.tail=b;b0.parent=rig.data.edit_bones[parent];b0.use_deform=deform
bone('Spine',(0,.4,.78),(0,-.32,.83),'Root')
bone('Neck',(0,-.32,.83),(0,-.56,1.02),'Spine')
bone('Head',(0,-.56,1.02),(0,-.92,1.03),'Neck')
bone('Tail',(0,.5,.83),(0,1.16,.3),'Spine')
for side,suffix in [(-1,'L'),(1,'R')]:
    x=side*.19
    for kind,hip,knee,ankle in [('Front',(x,-.34,.82),(x,-.27,.45),(x,-.39,.11)),('Hind',(x,.37,.79),(x,.20,.47),(x,.43,.1))]:
        name=kind+'.'+suffix
        bone(name+'_Upper',hip,knee,'Spine')
        bone(name+'_Lower',knee,ankle,name+'_Upper')
        bone(name+'_Foot',ankle,(x,ankle[1]-.12,ankle[2]-.04),name+'_Lower')
        bone(name+'_Target',ankle,(x,ankle[1],ankle[2]+.1),'Root',False)
bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.object.select_all(action='DESELECT');body.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.object.parent_set(type='ARMATURE_AUTO')
for obj in details:
    group=obj.vertex_groups.new(name='Head');group.add(list(range(len(obj.data.vertices))),1,'REPLACE')
    obj.parent=rig;obj.modifiers.new('Armature','ARMATURE').object=rig
for kind in ['Front','Hind']:
    for suffix in ['L','R']:
        name=kind+'.'+suffix
        constraint=rig.pose.bones[name+'_Lower'].constraints.new('IK')
        constraint.target=rig;constraint.subtarget=name+'_Target';constraint.chain_count=2
rig.animation_data_create()
for name,end in [('Idle',90),('Walk',40),('Run',20),('Sniff',90)]:
    action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
    for frame in range(1,end+2):
        phase=(frame-1)/end*math.tau
        for p in rig.pose.bones:p.rotation_mode='XYZ';p.rotation_euler=(0,0,0);p.location=(0,0,0)
        if name in ['Walk','Run']:
            stride=.20 if name=='Walk' else .28
            lift=.08 if name=='Walk' else .13
            for kind in ['Front','Hind']:
                for suffix in ['L','R']:
                    offset=0 if (kind=='Front')==(suffix=='L') else math.pi
                    wave=phase+offset
                    target=rig.pose.bones[kind+'.'+suffix+'_Target']
                    target.location.z=-stride*math.cos(wave)
                    target.location.y=lift*max(0,math.sin(wave))
            rig.pose.bones['Spine'].location.z=.01*math.cos(phase*2)
        if name=='Sniff':
            rig.pose.bones['Neck'].rotation_euler.x=.23+.15*math.sin(phase)
            rig.pose.bones['Head'].rotation_euler.x=.16+.06*math.sin(phase*2)
        else:rig.pose.bones['Head'].rotation_euler.z=.035*math.sin(phase)
        rig.pose.bones['Tail'].rotation_euler.z=.10*math.sin(phase)
        for p in rig.pose.bones:
            p.keyframe_insert('rotation_euler',frame=frame);p.keyframe_insert('location',frame=frame)
    track=rig.animation_data.nla_tracks.new();track.name=name;track.strips.new(name,1,action)
rig.animation_data.action=None;scene.frame_start=1;scene.frame_end=91;scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT');body.select_set(True);rig.select_set(True)
for obj in details:obj.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/08.Models/Wildlife/WoodlandWolf.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_nla_strips=True,bake_anim_use_all_actions=False,axis_forward='-Z',axis_up='Y')
for track in rig.animation_data.nla_tracks:track.mute=True
rig.animation_data.action=bpy.data.actions['Idle'];scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource/Wildlife/WoodlandWolf.blend'))
world=scene.world or bpy.data.worlds.new('World');scene.world=world;world.color=(.22,.22,.22)
bpy.ops.object.camera_add(location=(3,-3.5,2.1));cam=bpy.context.object
cam.rotation_euler=(Vector((0,0,.65))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=2.8;scene.camera=cam
bpy.ops.object.light_add(type='AREA',location=(1,-2,4));bpy.context.object.data.energy=400;bpy.context.object.data.size=4
scene.render.engine='CYCLES';scene.cycles.samples=24
scene.render.resolution_x=1000;scene.render.resolution_y=800;scene.render.resolution_percentage=100
scene.render.filepath=str(ROOT/'output/Wildlife/wolf_preview.png');bpy.ops.render.render(write_still=True)
print('WOLF_EXPORT_OK',len(body.data.vertices),len(body.data.polygons))
