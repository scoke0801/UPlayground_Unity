import bpy, math, json, os
from mathutils import Vector
root='C:/UsingProject/UnityProject/UPlayground/'
scene=bpy.data.scenes.new('RiverBasinWorld_B1_Bridge')
bpy.context.window.scene=scene
scene.unit_settings.system='METRIC'
scene.unit_settings.scale_length=1
mat=bpy.data.materials.new('MAT_RBW_BridgeStone');mat.diffuse_color=(.38,.35,.29,1)
def box(name,center,size):
    bpy.ops.mesh.primitive_cube_add(size=1,location=center)
    o=bpy.context.object;o.name=name;o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return o
def make_lod(lod):
    parts=[]
    parts.append(box('deck',(0,0,-.35),(5.6,36,.7)))
    # 중앙 보행면은 일체형이며 아치와 석재 줄눈은 렌더링 형태만 담당한다.
    for x in [-2.68,2.68]:
        parts.append(box('parapet',(x,0,.45),(.24,35, .9)))
        for y in ([-17,-11,-5,1,7,13,17] if lod<2 else [-17,0,17]):
            parts.append(box('cap',(x,y,.98),(.48,.7,.18)))
    for y in [-16.4,-5.45,5.45,16.4]:
        parts.append(box('pier',(0,y,-2.8),(5.25,1.25,4.8)))
    if lod<2:
        for cy in [-10.9,0,10.9]:
            segments=12 if lod==0 else 8
            for i in range(segments):
                a0=math.pi*i/segments;a1=math.pi*(i+1)/segments
                verts=[]
                for x in [-2.55,2.55]:
                    for r,a in [(4.85,a0),(4.85,a1),(5.5,a1),(5.5,a0)]:
                        verts.append((x,cy+r*math.cos(a),-5.65+r*math.sin(a)))
                faces=[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
                mesh=bpy.data.meshes.new('arch_stone');mesh.from_pydata(verts,[],faces);mesh.update()
                o=bpy.data.objects.new('arch_stone',mesh);scene.collection.objects.link(o);parts.append(o)
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts:o.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join()
    o=bpy.context.object;o.name='SM_RBW_Bridge_LOD'+str(lod)
    scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    o.data.materials.clear();o.data.materials.append(mat)
    if lod==0:
        bevel=o.modifiers.new('stone_edge','BEVEL');bevel.width=.045;bevel.segments=1
        bpy.ops.object.modifier_apply(modifier=bevel.name)
    # 4m 반복 석재. 면의 주축에 따른 미터 UV를 쓰며 반복 UV 겹침은 의도한 타일링이다.
    # FBX의 첫 UV가 실제 표면에 쓰이므로 큐브 기본 UV를 남기지 않는다.
    for old_uv in list(o.data.uv_layers):o.data.uv_layers.remove(old_uv)
    uv=o.data.uv_layers.new(name='Stone_4m')
    for poly in o.data.polygons:
        axis=max(range(3),key=lambda j:abs(poly.normal[j]));axes=[j for j in range(3) if j!=axis]
        for li in poly.loop_indices:
            p=o.data.vertices[o.data.loops[li].vertex_index].co
            uv.data[li].uv=(p[axes[0]]/4,p[axes[1]]/4)
    return o
objects=[make_lod(i) for i in range(3)]
for i,o in enumerate(objects):o.hide_render=i>0;o.hide_set(i>0)
bpy.ops.wm.save_as_mainfile(filepath=root+'Tools/RiverBasinWorld/Source/B1_StoneBridge.blend')
for o in objects:o.hide_set(False);o.hide_render=False
bpy.ops.object.select_all(action='DESELECT')
for o in objects:o.select_set(True)
bpy.context.view_layer.objects.active=objects[0]
bpy.ops.export_scene.fbx(filepath=root+'Assets/08.Models/World/RiverBasinWorld/B1_StoneBridge.fbx',use_selection=True,object_types={'MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True,bake_anim=False,add_leaf_bones=False)
audit=[{'name':o.name,'dimensions':list(o.dimensions),'scale':list(o.scale),'triangles':sum(len(p.vertices)-2 for p in o.data.polygons),'uvChannels':len(o.data.uv_layers)} for o in objects]
with open(root+'output/RiverBasinWorld/blender-bridge-audit.json','w',encoding='utf8') as f:json.dump(audit,f,indent=2)
for i,o in enumerate(objects):o.hide_render=i>0;o.hide_set(i>0)
bpy.ops.wm.save_as_mainfile(filepath=root+'Tools/RiverBasinWorld/Source/B1_StoneBridge.blend')
result=audit
