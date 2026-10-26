"""연결된 몸통, 밀착된 접힌 날개와 작은 새 모션을 만들고 표면 색을 UV에 베이크한다."""
import bpy
import math
from pathlib import Path
from mathutils import Vector, Quaternion, Matrix

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/08.Models/Wildlife'
PREVIEW = ROOT / 'output/Wildlife'
TEXTURE = ROOT / 'Assets/04.Images/Environment/Wildlife/Songbird_Plumage.png'
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
bpy.context.preferences.filepaths.save_version = 0
scene = bpy.context.scene
scene.render.fps = 30
scene.render.engine = 'CYCLES'
scene.cycles.samples = 24
parts = []
BACK = Vector((.15, .19, .20))
WING = Vector((.105, .133, .142))
RUST = Vector((.52, .16, .035))
BELLY = Vector((.65, .60, .49))
mat = bpy.data.materials.new('Songbird_Feathers')
mat.use_nodes = True
nodes, links = mat.node_tree.nodes, mat.node_tree.links
bsdf = nodes.get('Principled BSDF')
bsdf.inputs['Roughness'].default_value = .86
color = nodes.new('ShaderNodeVertexColor')
color.layer_name = 'Plumage'
noise = nodes.new('ShaderNodeTexNoise')
noise.inputs['Scale'].default_value = 95
noise.inputs['Detail'].default_value = 2
coordinates = nodes.new('ShaderNodeTexCoord')
grain = nodes.new('ShaderNodeVectorMath')
grain.operation = 'MULTIPLY'
grain.inputs[1].default_value = (2, 2, .5)
links.new(coordinates.outputs['Generated'], grain.inputs[0])
links.new(grain.outputs[0], noise.inputs['Vector'])
ramp = nodes.new('ShaderNodeValToRGB')
ramp.color_ramp.elements[0].color = (.60, .60, .60, 1)
ramp.color_ramp.elements[1].color = (1, 1, 1, 1)
links.new(noise.outputs['Fac'], ramp.inputs[0])
mix = nodes.new('ShaderNodeMixRGB')
mix.blend_type = 'MULTIPLY'
mix.inputs[0].default_value = 1
links.new(color.outputs['Color'], mix.inputs[1])
links.new(ramp.outputs[0], mix.inputs[2])
links.new(mix.outputs[0], bsdf.inputs['Base Color'])
dark = bpy.data.materials.new('Songbird_EyesBeak')
dark.use_nodes = True
dark.diffuse_color = (.012, .009, .007, 1)
dark.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = dark.diffuse_color
dark.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = .3


def smooth(low, high, value):
    t = max(0, min(1, (value-low)/(high-low)))
    return t*t*(3-2*t)


def make_part(name, vertices, faces, bone, colors=None, material=mat):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(material)
    for polygon in data.polygons:
        polygon.use_smooth = True
    attribute = data.color_attributes.new(name='Plumage', type='FLOAT_COLOR', domain='CORNER')
    for loop in data.loops:
        c = colors[loop.vertex_index] if colors is not None else BACK
        attribute.data[loop.index].color = (*c, 1)
    if bone:
        obj.vertex_groups.new(name=bone).add(list(range(len(vertices))), 1, 'REPLACE')
    parts.append(obj)
    return obj


def ellipsoid(name, location, scale, bone):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20, ring_count=12, location=location)
    obj = bpy.context.object
    obj.name, obj.scale = name, scale
    bpy.ops.object.transform_apply(location=True, rotation=False, scale=True)
    obj.data.materials.append(dark)
    for p in obj.data.polygons:
        p.use_smooth = True
    obj.vertex_groups.new(name=bone).add(list(range(len(obj.data.vertices))), 1, 'REPLACE')
    parts.append(obj)
    return obj


def segment(name, start, end, radius, tip, bone):
    start, end = Vector(start), Vector(end)
    bpy.ops.mesh.primitive_cone_add(vertices=10, radius1=radius, radius2=tip,
                                   depth=(end-start).length, location=(start+end)*.5)
    obj = bpy.context.object
    obj.name = name
    obj.rotation_euler = (end-start).to_track_quat('Z', 'Y').to_euler()
    obj.data.materials.append(dark)
    obj.vertex_groups.new(name=bone).add(list(range(len(obj.data.vertices))), 1, 'REPLACE')
    for p in obj.data.polygons:
        p.use_smooth = True
    parts.append(obj)


# 높이별 단면을 연속 보간하여 머리·목·가슴 사이의 구체 이음새를 없앤다.
sections = [(.038, .02, .001, .002), (.048, .016, .023, .04),
            (.065, .007, .041, .066), (.09, .003, .051, .084),
            (.115, -.005, .052, .086), (.14, -.026, .047, .072),
            (.16, -.051, .039, .053), (.18, -.064, .036, .040),
            (.198, -.064, .029, .032), (.211, -.060, .015, .019),
            (.215, -.057, .001, .001)]
vertices, faces, colors = [], [], []
radial = 40
rings = (len(sections)-1)*5+1
for row in range(rings):
    u = row/5
    index = min(int(u), len(sections)-2)
    t = u-index
    p0, p1 = Vector(sections[max(0,index-1)]), Vector(sections[index])
    p2, p3 = Vector(sections[index+1]), Vector(sections[min(len(sections)-1,index+2)])
    values = .5*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t)
    z, cy, rx, ry = values
    for col in range(radial):
        angle = col*math.tau/radial
        x, y = max(.001,rx)*math.cos(angle), cy+max(.001,ry)*math.sin(angle)
        vertices.append((x,y,z))
        front = -math.sin(angle)
        belly = (1-smooth(.095,.135,z))*smooth(-.35,.35,front)
        c = BACK.lerp(BELLY, belly)
        # 얼굴에서 가슴까지 둥글게 이어지는 주황색 턱받이. 경계는 꼭짓점 사이에서 부드럽게 섞인다.
        oval = (x/.052)**2 + ((z-.146)/.058)**2
        bib = smooth(.10,.55,front)*(1-smooth(.68,1.18,oval))
        c = c.lerp(RUST,bib)
        colors.append(c)
for row in range(rings-1):
    for col in range(radial):
        faces.append((row*radial+col,row*radial+(col+1)%radial,
                      (row+1)*radial+(col+1)%radial,(row+1)*radial+col))
faces += [tuple(reversed(range(radial))), tuple((rings-1)*radial+i for i in range(radial))]
body = make_part('Body',vertices,faces,None,colors)
body_group = body.vertex_groups.new(name='Body')
head_group = body.vertex_groups.new(name='Head')
for v in body.data.vertices:
    head_weight = smooth(.145,.182,v.co.z)
    if head_weight < 1:
        body_group.add([v.index],1-head_weight,'REPLACE')
    if head_weight > 0:
        head_group.add([v.index],head_weight,'REPLACE')

# 흰 눈테를 없애고 검은 눈의 얕은 곡면만 두개골 바깥으로 드러낸다.
for side in (-1,1):
    ellipsoid('Eye', (side*.0302,-.081,.188), (.0019,.0042,.0042), 'Head')
segment('Beak', (0,-.102,.176), (0,-.130,.170), .0062, .0003, 'Head')


def leaf(name, start, end, width, depth, normal, bone, color, blunt=False, bend=0):
    start, end, normal = Vector(start), Vector(end), Vector(normal)
    along = (end-start).normalized()
    across = normal.cross(along).normalized()
    normal = along.cross(across).normalized()
    verts, polys, shades = [], [], []
    steps, sides = 16, 12
    for row in range(steps+1):
        t = row/steps
        center = start.lerp(end,t)+normal*(bend*math.sin(math.pi*t))
        breadth = math.sin(math.pi*t)**.75
        if blunt:
            breadth = max(.30 if t>.8 else .02,breadth)
        breadth = max(.015,breadth)
        for col in range(sides):
            angle = col*math.tau/sides
            verts.append(center+across*(math.cos(angle)*width*breadth)
                         +normal*(math.sin(angle)*depth*breadth))
            # 깃의 중앙과 가장자리에 작은 명암 차이만 주어 굵은 줄무늬를 피한다.
            shade = .90+.10*abs(math.cos(angle))
            shades.append(Vector(color)*shade)
    for row in range(steps):
        for col in range(sides):
            polys.append((row*sides+col,row*sides+(col+1)%sides,
                          (row+1)*sides+(col+1)%sides,(row+1)*sides+col))
    polys += [tuple(reversed(range(sides))), tuple(steps*sides+i for i in range(sides))]
    return make_part(name,verts,polys,bone,shades)


# 접힌 날개 자체를 기준 자세로 저작한다. 어깨에서 꼬리 쪽으로 가늘어지는 몸 옆의 덮개다.
for side in (-1,1):
    bone = 'Wing.L' if side<0 else 'Wing.R'
    leaf('WingCoverts', (side*.041,-.044,.14), (side*.018,.128,.082),
         .032,.009,(side,0,0),bone,WING,bend=.012)
    for i in range(6):
        leaf('WingFeather', (side*.052,-.008+i*.007,.135-i*.006),
             (side*.020,.125+i*.002,.094-i*.004), .0058,.0017,(side,0,0),bone,
             WING*(.87+i*.025),blunt=True,bend=.005)
for i in range(5):
    leaf('TailFeather', ((i-2)*.006,.078,.101),
         ((i-2)*.0075,.202-abs(i-2)*.004,.072), .006,.0015,(0,0,1),'Tail',WING*.85,blunt=True)
for side in (-1,1):
    hip, ankle, foot = (side*.022,.011,.067), (side*.022,.019,.032), (side*.022,-.004,.006)
    segment('Shin',hip,ankle,.0033,.002,'Legs')
    segment('Tarsus',ankle,foot,.002,.0016,'Legs')
    for spread in (-1,0,1):
        toe = (foot[0]+spread*.010,-.029+abs(spread)*.006,.002)
        segment('Toe',foot,toe,.0014,.00045,'Legs')
    segment('HindToe',foot,(foot[0],.014,.002),.0013,.0004,'Legs')

bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = body
bpy.ops.object.join()
mesh = bpy.context.object
mesh.name = 'Songbird_Mesh'
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.mesh.normals_make_consistent(inside=False)
bpy.ops.uv.smart_project(angle_limit=math.radians(65),island_margin=.008)
bpy.ops.object.mode_set(mode='OBJECT')

# 표면 색을 UV에 베이크하여 Unity에서도 같은 부드러운 색 경계를 사용한다.
texture = bpy.data.images.new('Songbird_Plumage',width=2048,height=2048,alpha=False)
texture.filepath_raw = str(TEXTURE)
texture.file_format = 'PNG'
for material in (mat,dark):
    target = material.node_tree.nodes.new('ShaderNodeTexImage')
    target.image = texture
    material.node_tree.nodes.active = target
scene.render.bake.use_pass_direct = False
scene.render.bake.use_pass_indirect = False
scene.render.bake.use_pass_color = True
scene.render.bake.margin = 8
bpy.ops.object.bake(type='DIFFUSE')
texture.save()
# 모델 저장과 미리보기에서도 베이크 결과를 그대로 사용한다.
links.new(nodes.active.outputs['Color'],bsdf.inputs['Base Color'])

bpy.ops.object.armature_add()
rig = bpy.context.object
rig.name = 'Songbird_Rig'
bpy.ops.object.mode_set(mode='EDIT')
root = rig.data.edit_bones[0]
root.name = 'Root'
root.head,root.tail = (0,0,0),(0,0,.06)
for name,head_pos,tail_pos,parent in [
    ('Body',(0,0,.1),(0,0,.2),'Root'),
    ('Head',(0,-.054,.148),(0,-.054,.22),'Body'),
    ('Wing.L',(-.046,-.035,.137),(-.046,.12,.085),'Body'),
    ('Wing.R',(.046,-.035,.137),(.046,.12,.085),'Body'),
    ('Tail',(0,.078,.101),(0,.20,.072),'Body'),
    ('Legs',(0,.011,.067),(0,.011,.02),'Root')]:
    bone=rig.data.edit_bones.new(name)
    bone.head,bone.tail=head_pos,tail_pos
    bone.parent=rig.data.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT')
mesh.parent=rig
mesh.modifiers.new('Armature','ARMATURE').object=rig
rig.animation_data_create()


def rotate_bone(name,rotation):
    bone=rig.pose.bones[name]
    basis=bone.bone.matrix_local.to_quaternion()
    bone.rotation_quaternion=basis.inverted() @ rotation @ basis


def axis_rotation(axis,degrees):
    return Quaternion(axis,math.radians(degrees))


def head_pose(frame):
    keys=[(1,0,0),(24,0,0),(28,-22,-3),(52,-22,-3),(57,16,3),
          (84,16,3),(89,0,0),(110,0,0),(116,0,14),(122,0,0),
          (145,0,0),(150,10,-4),(168,10,-4),(174,0,0),(181,0,0)]
    for left,right in zip(keys,keys[1:]):
        if left[0]<=frame<=right[0]:
            t=smooth(left[0],right[0],frame)
            return left[1]+(right[1]-left[1])*t,left[2]+(right[2]-left[2])*t
    return 0,0


span=Vector((0,.155,-.052)).normalized()
rest=Matrix((Vector((1,0,0)),span,Vector((1,0,0)).cross(span))).transposed()
for name,end in [('Idle',180),('Fly',6),('Glide',60)]:
    action=bpy.data.actions.new(name)
    action.use_fake_user=True
    rig.animation_data.action=action
    for frame in range(1,end+2):
        phase=(frame-1)/end*math.tau
        for bone in rig.pose.bones:
            bone.rotation_mode='QUATERNION'
            bone.rotation_quaternion=(1,0,0,0)
            bone.location=(0,0,0)
            bone.scale=(1,1,1)
        for side,bone in [(-1,'Wing.L'),(1,'Wing.R')]:
            if name!='Idle':
                target=Matrix((Vector((0,0,side)),Vector((side,0,0)),Vector((0,1,0)))).transposed()
                unfold=(target @ rest.transposed()).to_quaternion()
                flap=48*math.cos(phase) if name=='Fly' else 9+3*math.sin(phase)
                rotate_bone(bone,axis_rotation((0,1,0),-side*flap) @ unfold)
                rig.pose.bones[bone].scale=(1.5,1.35,1)
        yaw,pitch=head_pose(frame) if name=='Idle' else (0,-4)
        if name!='Idle':
            rotate_bone('Body',axis_rotation((1,0,0),28))
            pitch=-18
        rotate_bone('Head',axis_rotation((0,0,1),yaw) @ axis_rotation((1,0,0),pitch))
        rotate_bone('Tail',axis_rotation((1,0,0),1.5*math.sin(phase)))
        rotate_bone('Legs',axis_rotation((1,0,0),0 if name=='Idle' else 100))
        rig.pose.bones['Body'].location.y=.0004*math.sin(phase*2)
        for bone in rig.pose.bones:
            bone.keyframe_insert('rotation_quaternion',frame=frame)
            bone.keyframe_insert('location',frame=frame)
            bone.keyframe_insert('scale',frame=frame)
    track=rig.animation_data.nla_tracks.new()
    track.name=name
    track.strips.new(name,1,action)
rig.animation_data.action=None
scene.frame_start,scene.frame_end=1,181
scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT')
rig.select_set(True)
mesh.select_set(True)
bpy.context.view_layer.objects.active=rig
OUT.mkdir(parents=True,exist_ok=True)
PREVIEW.mkdir(parents=True,exist_ok=True)
bpy.ops.export_scene.fbx(filepath=str(OUT/'WoodlandSongbird.fbx'),use_selection=True,
                         object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,
                         bake_anim_use_nla_strips=True,bake_anim_use_all_actions=False,
                         axis_forward='-Z',axis_up='Y')
for track in rig.animation_data.nla_tracks:
    track.mute=True
rig.animation_data.action=bpy.data.actions['Idle']
scene.frame_set(1)
assert len(rig.data.bones)==7
assert len(mesh.data.vertices)<8000
assert all(abs(sum(g.weight for g in v.groups)-1)<.001 for v in mesh.data.vertices)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource/Wildlife/WoodlandSongbird.blend'))

world=scene.world or bpy.data.worlds.new('World')
scene.world=world
world.use_nodes=True
world.node_tree.nodes['Background'].inputs[0].default_value=(.18,.21,.25,1)
world.node_tree.nodes['Background'].inputs[1].default_value=.5
bpy.ops.mesh.primitive_plane_add(size=200)
floor=bpy.context.object
floor_mat=bpy.data.materials.new('PreviewGround')
floor_mat.diffuse_color=(.12,.15,.13,1)
floor.data.materials.append(floor_mat)
bpy.ops.object.camera_add(location=(.48,-.62,.3))
cam=bpy.context.object
cam.data.type='ORTHO'
scene.camera=cam
bpy.ops.object.light_add(type='AREA',location=(.2,-.6,1.3))
bpy.context.object.data.energy=45
bpy.context.object.data.size=1
scene.render.resolution_x=1000
scene.render.resolution_y=850
scene.render.resolution_percentage=100
for view,clip,frame,position,scale in [
    ('threequarter','Idle',1,(.48,-.62,.26),.39),
    ('side','Idle',1,(.7,-.02,.20),.39),
    ('front','Idle',1,(0,-.7,.20),.34),
    ('flight','Fly',2,(.48,-.62,.3),.62)]:
    rig.animation_data.action=bpy.data.actions[clip]
    scene.frame_set(frame)
    cam.location=position
    cam.rotation_euler=(Vector((0,.025,.108))-cam.location).to_track_quat('-Z','Y').to_euler()
    cam.data.ortho_scale=scale
    scene.render.filepath=str(PREVIEW/('songbird_v2_'+view+'.png'))
    bpy.ops.render.render(write_still=True)
print('WILDLIFE_EXPORT_OK',len(mesh.data.vertices),len(mesh.data.polygons))
