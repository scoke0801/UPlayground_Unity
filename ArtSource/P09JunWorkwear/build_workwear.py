"""P09 준의 하체·공구 주머니·짧은 머리를 독립 메시로 제작한다."""
import bpy
import bmesh
import math
import json
from pathlib import Path
from mathutils import Vector

project = Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(project/'ArtSource/P09JunWorkwear/SourceReference.blend'))
rig = bpy.data.objects['Skeleton_P09_Body']
body = bpy.data.objects['Male_Body_Nakid_Leg']
collection = bpy.data.collections.new('Jun_Workwear_Crafted')
bpy.context.scene.collection.children.link(collection)
output = project/'Assets/08.Models/Equipment/P09JunWorkwear'
parts = []

def make_material(name, color, texture=None, metallic=0):
    mat=bpy.data.materials.new('Jun_'+name)
    mat.diffuse_color=(*color,1)
    mat.use_nodes=True
    shader=mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value=(*color,1)
    shader.inputs['Roughness'].default_value=0.75
    shader.inputs['Metallic'].default_value=metallic
    if texture:
        node=mat.node_tree.nodes.new('ShaderNodeTexImage')
        node.image=bpy.data.images.load(str(project/texture),check_existing=True)
        mat.node_tree.links.new(node.outputs['Color'],shader.inputs['Base Color'])
    return mat

fabric=make_material('Canvas',(.22,.20,.17),'Assets/04.Images/Equipment/P09JunWorkwear/TaupeCanvas_BaseColor.png')
leather=make_material('Leather',(.16,.075,.035),'Assets/04.Images/Equipment/P09RangerVest/ChestnutLeather_BaseColor.png')
edge=make_material('Edge',(.065,.043,.028))
brass=make_material('Brass',(.45,.31,.14),metallic=.6)
wood=make_material('Wood',(.22,.13,.065))
hairmat=make_material('Hair',(.09,.045,.025),'Assets/04.Images/Equipment/P09JunWorkwear/AshBrownHair_BaseColor.png')

def activate(obj):
    bpy.ops.object.select_all(action='DESELECT')
    obj.hide_set(False)
    obj.select_set(True)
    bpy.context.view_layer.objects.active=obj

def skin(obj, rigid=None, transfer=True):
    activate(obj)
    if rigid:
        obj.vertex_groups.clear()
        obj.vertex_groups.new(name=rigid).add(list(range(len(obj.data.vertices))),1,'REPLACE')
    elif transfer:
        for group in body.vertex_groups:
            if group.name not in obj.vertex_groups:obj.vertex_groups.new(name=group.name)
        transfer=obj.modifiers.new('P09 체형 웨이트','DATA_TRANSFER')
        transfer.object=body;transfer.use_vert_data=True
        transfer.data_types_verts={'VGROUP_WEIGHTS'}
        transfer.vert_mapping='POLYINTERP_NEAREST'
        bpy.ops.object.modifier_apply(modifier=transfer.name)
    obj.parent=rig
    modifier=obj.modifiers.new('P09 골격','ARMATURE');modifier.object=rig
    return obj

def make_mesh(name, vertices, faces, mat, uv=True):
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update()
    obj=bpy.data.objects.new(name,mesh);collection.objects.link(obj)
    mesh.materials.append(mat)
    bm=bmesh.new();bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free()
    for face in mesh.polygons:face.use_smooth=True
    if uv:
        activate(obj);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
        bpy.ops.uv.cube_project(cube_size=.25);bpy.ops.object.mode_set(mode='OBJECT')
    return obj

def rings(name, profile, cx, mat, count=32, caps=False):
    vertices=[];faces=[]
    for z,rx,cy,ry in profile:
        for i in range(count):
            angle=2*math.pi*i/count
            vertices.append((cx+rx*math.cos(angle),cy+ry*math.sin(angle),z))
    for k in range(len(profile)-1):
        for i in range(count):
            a=k*count+i;b=k*count+(i+1)%count
            faces.append((a,b,b+count,a+count))
    if caps:
        faces.extend([tuple(reversed(range(count))),tuple((len(profile)-1)*count+i for i in range(count))])
    return make_mesh(name,vertices,faces,mat)

def tube(name, points, radius, mat, rigid=None):
    curve=bpy.data.curves.new(name,'CURVE');curve.dimensions='3D'
    curve.bevel_depth=radius;curve.bevel_resolution=1
    spline=curve.splines.new('POLY');spline.points.add(len(points)-1)
    for point,co in zip(spline.points,points):point.co=(*co,1)
    obj=bpy.data.objects.new(name,curve);collection.objects.link(obj);curve.materials.append(mat)
    activate(obj);bpy.ops.object.convert(target='MESH')
    return skin(bpy.context.object,rigid)

def box(name, center, dimensions, mat, bevel=.005, rigid='Hips'):
    bpy.ops.mesh.primitive_cube_add(size=1,location=center)
    obj=bpy.context.object;obj.name=name
    for c in list(obj.users_collection):c.objects.unlink(obj)
    collection.objects.link(obj)
    obj.dimensions=dimensions;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    obj.data.materials.append(mat)
    modifier=obj.modifiers.new('가죽 모서리','BEVEL');modifier.width=bevel;modifier.segments=2
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    # 병합 뒤에도 데이터 전송이 몸체와 같은 좌표계에서 일어나도록 위치를 굽는다.
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    return skin(obj,rigid)

def join(name, objects):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:obj.hide_set(False);obj.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join()
    obj=bpy.context.object;obj.name=name
    parts.append(obj)
    return obj

# 몸체의 관절 흐름을 유지하면서 바지 여유분을 만든다.
pants=body.copy();pants.data=body.data.copy();pants.name='Jun_WorkTrousers';collection.objects.link(pants)
pants.modifiers.clear()
if pants.data.shape_keys:pants.shape_key_clear()
bm=bmesh.new();bm.from_mesh(pants.data)
bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.calc_center_median().z<.325],context='FACES')
bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS')
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
for v in bm.verts:
    clearance=.009+.009*math.exp(-((v.co.z-.70)/.15)**2)
    # 양쪽 허벅지 사이로 여유분을 밀면 표면과 최근접 웨이트가 반대 다리로 넘어간다.
    if v.co.x*v.normal.x<0 and .55<v.co.z<.89:clearance=.001
    v.co+=v.normal*clearance
    if abs(v.co.x)<.05 and .70<v.co.z<.89:v.co.y=max(v.co.y,-.073)
    if v.co.z<.415:
        taper=.80+.20*max(0,(v.co.z-.37)/.045)
        cx=math.copysign(.08815,v.co.x)
        v.co.x=cx+(v.co.x-cx)*taper
        v.co.y=.025+(v.co.y-.025)*taper
bm.to_mesh(pants.data);bm.free()
pants.data.materials.clear();pants.data.materials.append(fabric)
for face in pants.data.polygons:face.material_index=0;face.use_smooth=True
activate(pants)
smooth=pants.modifiers.new('천 표면 완화','SMOOTH');smooth.factor=.3;smooth.iterations=2
bpy.ops.object.modifier_apply(modifier=smooth.name)
sub=pants.modifiers.new('관절 곡면','SUBSURF');sub.levels=1;bpy.ops.object.modifier_apply(modifier=sub.name)
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.cube_project(cube_size=.3);bpy.ops.object.mode_set(mode='OBJECT')
# 원본에서 이어진 정점 그룹은 세분화 시 보간된 값을 유지한다.
skin(pants,transfer=False)
pant_objects=[pants]
pants=join('Jun_WorkTrousers',pant_objects)
activate(pants)
decimate=pants.modifiers.new('NPC 하체 밀도','DECIMATE');decimate.ratio=.7
bpy.ops.object.modifier_apply(modifier=decimate.name)

# 발가락과 장딴지 실측을 감싸는 단면으로 발등·밑창·부츠 통을 제작한다.
boot_objects=[]
for side in [-1,1]:
    cx=side*.08815
    profile=[(.027,.048,-.038,.105),(.045,.050,-.038,.106),(.070,.047,-.031,.093),(.100,.041,-.013,.072),(.145,.038,.018,.044),(.21,.041,.022,.047),(.29,.048,.025,.053),(.375,.056,.025,.056),(.39,.055,.025,.055)]
    boot_objects.append(skin(rings('Boot_Upper',profile,cx,leather,caps=True)))
    boot_objects.append(skin(rings('Boot_Sole',[(.007,.051,-.038,.110),(.015,.053,-.038,.112),(.033,.052,-.038,.111)],cx,edge,caps=True)))
    boot_objects.append(skin(rings('Boot_Cuff',[(.365,.0575,.025,.0575),(.385,.0575,.025,.0575),(.39,.055,.025,.055)],cx,edge)))
    for height,rx,ry in [(.18,.041,.047),(.30,.051,.056)]:
        boot_objects.append(skin(rings('Boot_Strap',[(height-.009,rx,.023,ry),(height+.009,rx,.023,ry)],cx,edge)))
        bx=cx+side*(rx-.002);by=-.002
        boot_objects.append(tube('Boot_Buckle',[(bx,by-.012,height-.011),(bx,by+.012,height-.011),(bx,by+.012,height+.011),(bx,by-.012,height+.011),(bx,by-.012,height-.011)],.0017,brass))
    for z in [.11,.125,.14,.155]:
        y=-.043 if z>.13 else -.066
        boot_objects.append(tube('Boot_Lace',[(cx-.019,y,z-.004),(cx+.019,y,z+.004)],.0017,edge))
join('Jun_WorkBoots',boot_objects)

# 벨트와 주머니는 골반을 따라 움직이는 단단한 가죽 장비다.
belt_objects=[skin(rings('Work_Belt',[(.985,.131,.01,.109),(1.02,.127,.01,.106)],0,leather),'Hips')]
belt_objects.append(tube('Belt_Buckle',[(-.021,-.101,.986),(.021,-.101,.986),(.021,-.101,1.018),(-.021,-.101,1.018),(-.021,-.101,.986)],.003,brass,'Hips'))
belt_objects.append(box('Tool_Pouch',(.14,-.075,.906),(.096,.064,.134),leather,.013))
belt_objects.append(box('Pouch_Flap',(.14,-.112,.938),(.098,.009,.074),edge,.008))
belt_objects.append(box('Pouch_Strap',(.14,-.119,.925),(.017,.005,.09),leather,.001))
belt_objects.append(box('Pouch_Clasp',(.14,-.124,.919),(.022,.006,.018),brass,.002))
belt_objects.append(box('Belt_Loop',(.118,-.056,.978),(.035,.018,.075),edge,.003))
belt_objects.append(box('Hammer_Handle',(-.16,.017,.954),(.018,.021,.16),wood,.004))
belt_objects.append(box('Hammer_Head',(-.16,.017,1.028),(.068,.033,.032),brass,.004))
belt_objects.append(box('Hammer_Holster',(-.16,.016,.949),(.033,.035,.045),edge,.005))
join('Jun_ToolBelt',belt_objects)

# 짧은 머리는 별도 한 본으로 고정한다. 긴 머리 물리는 이 제작 범위에 포함하지 않는다.
hair_rig_data=bpy.data.armatures.new('Jun_HairRig')
hair_rig=bpy.data.objects.new('Jun_HairRig',hair_rig_data);collection.objects.link(hair_rig)
activate(hair_rig);bpy.ops.object.mode_set(mode='EDIT')
bone=hair_rig_data.edit_bones.new('JunHairRoot');bone.head=(0,.0165383,1.3612852);bone.tail=(0,.0165383,1.4360763)
bpy.ops.object.mode_set(mode='OBJECT')
# 원본 P09의 가닥 토폴로지를 유지하고 정수리 방향과 옆·뒷머리 길이를 조정한다.
source_hair=bpy.data.objects['Hair_13']
hair=source_hair.copy();hair.data=source_hair.data.copy();collection.objects.link(hair)
hair.modifiers.clear()
if hair.data.shape_keys:hair.shape_key_clear()
world=hair.matrix_world.copy();hair.parent=None;hair.matrix_world.identity()
for vertex in hair.data.vertices:
    vertex.co=world@vertex.co
    x,y,z=vertex.co
    upper=max(0,min(1,(z-1.49)/.07))
    side=max(0,min(1,(abs(x)-.055)/.045))
    lower=max(0,min(1,(1.48-z)/.10))
    vertex.co.x=x*(1+.035*side*(1-upper))-.008*upper
    vertex.co.z=z+.006*upper+.009*lower*max(0,min(1,y/.07))
    vertex.co.y=y-.004*lower*max(0,min(1,y/.08))
hair.data.materials.clear();hair.data.materials.append(hairmat)
for face in hair.data.polygons:face.material_index=0
hair=join('Jun_SweptShortHair',[hair])
hair.vertex_groups.clear();hair.vertex_groups.new(name='JunHairRoot').add(list(range(len(hair.data.vertices))),1,'REPLACE')
hair.parent=hair_rig
modifier=hair.modifiers.new('짧은 머리 고정','ARMATURE');modifier.object=hair_rig

report=[]
for obj in parts:
    assert all(len(v.groups)>0 for v in obj.data.vertices),obj.name+' 웨이트 누락'
    assert all(math.isfinite(value) for v in obj.data.vertices for value in v.co),obj.name+' 정점 오류'
    report.append({'name':obj.name,'vertices':len(obj.data.vertices),'triangles':sum(len(p.vertices)-2 for p in obj.data.polygons),'materials':[m.name for m in obj.data.materials]})
assert sum(row['triangles'] for row in report)<25000,'NPC 부품 합산 예산 초과'

def export(path, objects):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:obj.hide_set(False);obj.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.export_scene.fbx(filepath=str(output/path),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=False,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True)
export('Jun_Workwear.fbx',[rig]+[obj for obj in parts if obj!=hair])
export('Jun_SweptShortHair.fbx',[hair_rig,hair])

visible={obj.name for obj in parts}|{'Male_Face_02','Male_Body_Chest','Male_Armor_002_Arm'}
for obj in bpy.data.objects:
    if obj.type=='MESH':obj.hide_set(obj.name not in visible);obj.hide_render=obj.name not in visible
for obj in list(bpy.data.objects):
    if obj.type not in {'MESH','ARMATURE'}:continue
    if obj.name not in visible and obj not in {rig,hair_rig}:
        bpy.data.objects.remove(obj,do_unlink=True)
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(project/'ArtSource/P09JunWorkwear/P09_JunWorkwear.blend'),compress=True)
(project/'output/P09JunWorkwear/blender-validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
