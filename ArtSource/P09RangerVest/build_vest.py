"""P09 체형과 기존 웨이트를 보존하는 조끼 제작. BUILD_RECIPE로 NPC별 재단을 지정한다."""
import bpy
import bmesh
import math
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

PROJECT = Path('C:/UsingProject/UnityProject/UPlayground')
recipe = globals().get('BUILD_RECIPE', {})
outfit_name = recipe.get('name', 'RangerVest')
mesh_name = recipe.get('mesh_name', 'Male_RangerVest_Chest')
scene = bpy.data.scenes['P09_RangerVest_Workshop']
bpy.context.window.scene = scene
body = bpy.data.objects[recipe.get('body', 'Male_Body_Chest')]
rig = bpy.data.objects['Skeleton_P09_Body']
collection_name = outfit_name + '_Crafted'
collection = bpy.data.collections.get(collection_name)
if collection:
    for obj in list(collection.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
else:
    collection = bpy.data.collections.new(collection_name)
    scene.collection.children.link(collection)

def material(name, color, metallic=0.0):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = (*color, 1)
    shader.inputs['Metallic'].default_value = metallic
    shader.inputs['Roughness'].default_value = 0.58 if metallic else 0.78
    mat.diffuse_color = (*color, 1)
    return mat

leather = material(outfit_name + '_Fabric', (0.20, 0.075, 0.035))
image = bpy.data.images.load(str(PROJECT/recipe.get('texture', 'Assets/04.Images/Equipment/P09RangerVest/ChestnutLeather_BaseColor.png')), check_existing=True)
tex = leather.node_tree.nodes.get('ImageGenLeather') or leather.node_tree.nodes.new('ShaderNodeTexImage')
tex.name = 'ImageGenLeather'
tex.image = image
leather.node_tree.links.new(tex.outputs['Color'], leather.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
edge = material(outfit_name + '_Edge', (0.055, 0.035, 0.022))
brass = material(outfit_name + '_Brass', (0.34, 0.24, 0.10), 0.7)

vest = body.copy()
vest.data = body.data.copy()
vest.name = mesh_name
collection.objects.link(vest)
vest.modifiers.clear()
if vest.data.shape_keys:
    vest.shape_key_clear()
bm = bmesh.new()
bm.from_mesh(vest.data)
remove = []
for f in bm.faces:
    c = f.calc_center_median()
    # 어깨를 남기고 상완·목과 골반을 비워 관절 회전 여유를 확보한다.
    armhole = recipe.get('armhole_width', 0.118) + max(0.0, c.z - 1.20) * 0.36
    neckline = recipe.get('neckline', 1.265) + min(abs(c.x) / 0.07, 1.0) * recipe.get('neckline_slope', 0.060)
    if c.z < 1.008 or abs(c.x) > armhole or c.z > neckline:
        remove.append(f)
bmesh.ops.delete(bm, geom=remove, context='FACES')
bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
for v in bm.verts:
    v.co += v.normal * recipe.get('clearance', 0.008)
    if v.co.z < 1.035:
        v.co.z -= 0.025 * (1.035-v.co.z)/0.027
bm.to_mesh(vest.data)
bm.free()
vest.data.materials.clear()
vest.data.materials.append(leather)
for poly in vest.data.polygons:
    poly.material_index = 0
    poly.use_smooth = True
bpy.ops.object.select_all(action='DESELECT')
vest.select_set(True)
bpy.context.view_layer.objects.active = vest
smooth = vest.modifiers.new('가죽 표면 완화', 'SMOOTH')
smooth.factor = 0.45
smooth.iterations = 3
bpy.ops.object.modifier_apply(modifier=smooth.name)
sub = vest.modifiers.new('재단선 곡면', 'SUBSURF')
sub.levels = 1
bpy.ops.object.modifier_apply(modifier=sub.name)

# 어깨의 수평 면에서도 가죽 결이 늘어나지 않게 방향별 UV를 투영한다.
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.cube_project(cube_size=0.3)
bpy.ops.object.mode_set(mode='OBJECT')

def apply_weights(obj):
    for group in body.vertex_groups:
        if group.name not in obj.vertex_groups:
            obj.vertex_groups.new(name=group.name)
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    transfer = obj.modifiers.new('P09 체형 웨이트', 'DATA_TRANSFER')
    transfer.object = body
    transfer.use_vert_data = True
    transfer.data_types_verts = {'VGROUP_WEIGHTS'}
    transfer.vert_mapping = 'POLYINTERP_NEAREST'
    bpy.ops.object.modifier_apply(modifier=transfer.name)
    obj.parent = rig
    armature = obj.modifiers.new('P09 골격', 'ARMATURE')
    armature.object = rig

def tube(name, points, radius, mat):
    curve = bpy.data.curves.new(name, 'CURVE')
    curve.dimensions = '3D'
    curve.resolution_u = 1
    curve.bevel_depth = radius
    curve.bevel_resolution = 0
    spline = curve.splines.new('POLY')
    spline.points.add(len(points)-1)
    for p, co in zip(spline.points, points):
        p.co = (*co, 1)
    obj = bpy.data.objects.new(name, curve)
    collection.objects.link(obj)
    obj.data.materials.append(mat)
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.convert(target='MESH')
    obj = bpy.context.object
    apply_weights(obj)
    return obj

# 목둘레·암홀·밑단의 가죽 바인딩을 실제 형상으로 만든다.
bm = bmesh.new()
bm.from_mesh(vest.data)
boundary = [e for e in bm.edges if e.is_boundary]
unused = set(boundary)
loop_count = 0
while unused:
    e = unused.pop()
    start, current = e.verts
    points = [start.co.copy(), current.co.copy()]
    while current != start:
        next_edge = next((x for x in current.link_edges if x in unused), None)
        if next_edge is None:
            break
        unused.remove(next_edge)
        current = next_edge.other_vert(current)
        points.append(current.co.copy())
    tube('RangerVest_Binding_%02d' % loop_count, points, 0.0023, edge)
    loop_count += 1
bm.free()
bpy.context.view_layer.objects.active = vest
solid = vest.modifiers.new('가죽 두께', 'SOLIDIFY')
solid.thickness = 0.004
solid.offset = 0
bpy.ops.object.modifier_apply(modifier=solid.name)
apply_weights(vest)

# 몸의 굴곡을 따라 납작한 가죽 끈을 만든다. 떠 있는 원통처럼 보이지 않게 한다.
tree = BVHTree.FromObject(body, bpy.context.evaluated_depsgraph_get())
def front_y(x,z):
    hit = tree.ray_cast(Vector((x,-1,z)), Vector((0,1,0)))
    return hit[0].y-0.014 if hit[0] else -0.09

for index, z in enumerate(recipe.get('closures', [1.105, 1.175, 1.235])):
    vertices, faces = [], []
    for step in range(9):
        width = recipe.get('closure_width', 0.104)
        x = -width/2 + step*width/8
        y = front_y(x,z)
        vertices.extend([(x,y,z-0.008),(x,y,z+0.008),(x,y+0.003,z-0.008),(x,y+0.003,z+0.008)])
        if step:
            a=(step-1)*4;b=step*4
            faces.extend([(a,b,b+1,a+1),(a+2,a+3,b+3,b+2),(a,a+2,b+2,b),(a+1,b+1,b+3,a+3)])
    faces.extend([(0,1,3,2),(32,34,35,33)])
    mesh=bpy.data.meshes.new('ClosureMesh');mesh.from_pydata(vertices,[],faces);mesh.update()
    strap=bpy.data.objects.new('RangerVest_Closure_%02d'%index,mesh);collection.objects.link(strap)
    mesh.materials.append(edge);apply_weights(strap)
    y = front_y(0,z)-0.001
    tube('RangerVest_Buckle_%02d' % index,
         [(-0.011,y-0.003,z-0.009),(0.011,y-0.003,z-0.009),(0.011,y-0.003,z+0.009),(-0.011,y-0.003,z+0.009),(-0.011,y-0.003,z-0.009)],
         0.0018, brass)
tube('RangerVest_FrontSeam', [(0,front_y(0,z)+0.003,z) for z in [0.995+i*0.01 for i in range(27)]],0.0016,edge)

crafted = list(collection.objects)
bpy.ops.object.select_all(action='DESELECT')
for obj in crafted:
    obj.select_set(True)
bpy.context.view_layer.objects.active = vest
bpy.ops.object.join()
vest = bpy.context.object
vest.name = mesh_name
if recipe.get('simplify', 1) < 1:
    decimate = vest.modifiers.new('NPC 거리용 밀도', 'DECIMATE')
    decimate.ratio = recipe['simplify']
    bpy.ops.object.modifier_apply(modifier=decimate.name)
for poly in vest.data.polygons:
    poly.use_smooth = True
assert all(len(v.groups) > 0 for v in vest.data.vertices), '웨이트 없는 정점'
assert len(vest.data.vertices) < 20000, '검증용 흉갑 정점 예산 초과'

# 사용자 작업 씬은 건드리지 않고 검수 씬의 표시 대상만 선택한다.
visible = {body.name,'Male_Body_Arm','Male_Armor_002_Leg','Male_Armor_002_Waist','Male_Armor_002_Arm','Male_Face_01','Hair_01',mesh_name}
for obj in scene.objects:
    if obj.type == 'MESH':
        obj.hide_render = obj.name not in visible
        obj.hide_set(obj.name not in visible)

bpy.ops.object.select_all(action='DESELECT')
vest.hide_set(False)
vest.select_set(True)
rig.hide_set(False)
rig.select_set(True)
bpy.context.view_layer.objects.active = vest
fbx = PROJECT/recipe.get('fbx', 'Assets/08.Models/Equipment/P09RangerVest/P09_RangerVest.fbx')
bpy.ops.export_scene.fbx(filepath=str(fbx), use_selection=True, object_types={'MESH','ARMATURE'},
                        add_leaf_bones=False, bake_anim=False, apply_scale_options='FBX_SCALE_ALL',
                        axis_forward='-Z', axis_up='Y', use_mesh_modifiers=True)
result = {'vertices':len(vest.data.vertices),'triangles':sum(len(p.vertices)-2 for p in vest.data.polygons),'bindings':loop_count,'fbx':str(fbx)}
