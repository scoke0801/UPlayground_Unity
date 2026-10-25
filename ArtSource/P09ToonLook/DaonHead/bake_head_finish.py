"""페인트를 얼굴 안쪽에만 합성하고 고밀도 조형에서 게임 메시용 노멀을 굽는다."""
import bpy
import bmesh
from pathlib import Path

project=Path('C:/UsingProject/UnityProject/UPlayground')
source=project/'ArtSource/P09ToonLook/DaonHead'
output=project/'Assets/04.Images/Characters/DaonHead'
output.mkdir(parents=True,exist_ok=True)
scene=bpy.data.scenes['Daon_Sculpt_Review'];bpy.context.window.scene=scene
head=bpy.data.objects['Daon_Head_HighQuality'];sub=head.modifiers['얼굴 곡면']
arm=next(m for m in head.modifiers if m.type=='ARMATURE');arm.show_viewport=False

def skin_copy(name,level):
    sub.levels=level;bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get()
    mesh=bpy.data.meshes.new_from_object(head.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg)
    bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.material_index!=0],context='FACES');bm.to_mesh(mesh);bm.free()
    mesh.uv_layers.active=mesh.uv_layers['HeadUV'];mesh.uv_layers['HeadUV'].active_render=True
    obj=bpy.data.objects.new(name,mesh);scene.collection.objects.link(obj)
    return obj

low=skin_copy('Daon_Head_BakeLow',1);high=skin_copy('Daon_Head_BakeHigh',2)
sub.levels=1;arm.show_viewport=True;bpy.context.view_layer.update()
material=bpy.data.materials.new('Daon_Head_FinishBake');material.use_nodes=True
nodes=material.node_tree.nodes;nodes.clear();links=material.node_tree.links
out=nodes.new('ShaderNodeOutputMaterial');emission=nodes.new('ShaderNodeEmission');links.new(emission.outputs[0],out.inputs['Surface'])
uv=nodes.new('ShaderNodeUVMap');uv.uv_map='HeadUV'
base=nodes.new('ShaderNodeTexImage');base.image=bpy.data.images.load(str(source/'Daon_Head_BaseTransfer.png'),check_existing=True)
paint=nodes.new('ShaderNodeTexImage');paint.image=bpy.data.images.load(str(source/'Daon_Head_Painted.png'),check_existing=True)
for tex in (base,paint):links.new(uv.outputs['UV'],tex.inputs['Vector'])

# 얼굴의 중심부만 새 페인트를 쓰고 귀·목·입안과 얼굴 외곽은 원본 베이크를 유지한다.
sep=nodes.new('ShaderNodeSeparateXYZ');links.new(uv.outputs[0],sep.inputs[0])
def math_node(operation,a,b=None):
    n=nodes.new('ShaderNodeMath');n.operation=operation
    if isinstance(a,(int,float)):n.inputs[0].default_value=a
    else:links.new(a,n.inputs[0])
    if b is not None:
        if isinstance(b,(int,float)):n.inputs[1].default_value=b
        else:links.new(b,n.inputs[1])
    return n.outputs[0]
x=math_node('DIVIDE',math_node('SUBTRACT',sep.outputs['X'],.41),.375)
y=math_node('DIVIDE',math_node('SUBTRACT',sep.outputs['Y'],.675),.295)
radius=math_node('SQRT',math_node('ADD',math_node('MULTIPLY',x,x),math_node('MULTIPLY',y,y)))
fade=nodes.new('ShaderNodeMapRange');fade.clamp=True;fade.interpolation_type='SMOOTHERSTEP'
links.new(radius,fade.inputs['Value']);fade.inputs['From Min'].default_value=.65;fade.inputs['From Max'].default_value=.98;fade.inputs['To Min'].default_value=1;fade.inputs['To Max'].default_value=0
mix=nodes.new('ShaderNodeMixRGB');mix.blend_type='MIX';links.new(fade.outputs[0],mix.inputs[0]);links.new(base.outputs['Color'],mix.inputs[1]);links.new(paint.outputs['Color'],mix.inputs[2]);links.new(mix.outputs[0],emission.inputs['Color'])
low.data.materials.clear();low.data.materials.append(material)
for p in low.data.polygons:p.material_index=0
color=bpy.data.images.new('Daon_Head_Albedo',width=2048,height=2048,alpha=False);color.generated_color=(.82,.70,.67,1)
color.filepath_raw=str(output/'Daon_Head_Albedo.png');color.file_format='PNG'
target=nodes.new('ShaderNodeTexImage');target.image=color;nodes.active=target
engine=scene.render.engine;scene.render.engine='CYCLES';scene.cycles.samples=1
try:
    bpy.ops.object.select_all(action='DESELECT');low.select_set(True);bpy.context.view_layer.objects.active=low
    bpy.ops.object.bake(type='EMIT',use_clear=False,margin=12,use_selected_to_active=False);color.save()
    normal=bpy.data.images.new('Daon_Head_Normal',width=2048,height=2048,alpha=False)
    normal.colorspace_settings.name='Non-Color';normal.generated_color=(.5,.5,1,1)
    normal.filepath_raw=str(source/'Rejected_Normal.png');normal.file_format='PNG';target.image=normal
    high.select_set(True);bpy.context.view_layer.objects.active=low
    bpy.ops.object.bake(type='NORMAL',normal_space='TANGENT',use_clear=False,margin=12,use_selected_to_active=True,cage_extrusion=.003,max_ray_distance=.012)
    normal.save()
    # 고밀도 조형도 편집 가능한 검수 원본에 남긴다.
    high.name='Daon_Head_HighPoly_Reference';high.hide_render=True;high.hide_set(True)
    high.data.materials.clear()
    result={'low_skin_vertices':len(low.data.vertices),'high_skin_vertices':len(high.data.vertices),'albedo':str(output/'Daon_Head_Albedo.png'),'rejected_normal':str(source/'Rejected_Normal.png')}
finally:
    scene.render.engine=engine
    low_mesh=low.data;bpy.data.objects.remove(low,do_unlink=True);bpy.data.meshes.remove(low_mesh)
    bpy.data.materials.remove(material)
