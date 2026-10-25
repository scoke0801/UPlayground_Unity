"""전신 아틀라스의 피부를 얼굴 전용 UV에 무조명 베이크한다."""
import bpy
import bmesh
from pathlib import Path

project=Path('C:/UsingProject/UnityProject/UPlayground')
source=project/'ArtSource/P09ToonLook/DaonHead'
scene=bpy.data.scenes['Daon_Sculpt_Review'];bpy.context.window.scene=scene
head=bpy.data.objects['Daon_Head_HighQuality']
dg=bpy.context.evaluated_depsgraph_get()
mesh=bpy.data.meshes.new_from_object(head.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg)
bm=bmesh.new();bm.from_mesh(mesh)
bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.material_index!=0],context='FACES')
bm.to_mesh(mesh);bm.free()
obj=bpy.data.objects.new('Daon_Head_BakeTemporary',mesh);scene.collection.objects.link(obj)
image=bpy.data.images.new('Daon_Head_BaseTransfer',width=2048,height=2048,alpha=False)
image.generated_color=(.82,.70,.67,1)
image.filepath_raw=str(source/'Daon_Head_BaseTransfer.png');image.file_format='PNG'
material=bpy.data.materials.new('Daon_Head_TransferTemporary');material.use_nodes=True
nodes=material.node_tree.nodes;nodes.clear()
out=nodes.new('ShaderNodeOutputMaterial');emission=nodes.new('ShaderNodeEmission')
tex=nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(project/'Assets/10.Datas/Actor/ScenarioResidents/ToonLook/Textures/P09_Female_Body_Diff_4K.png'),check_existing=True)
uv=nodes.new('ShaderNodeUVMap');uv.uv_map='SourceUV'
material.node_tree.links.new(uv.outputs['UV'],tex.inputs['Vector']);material.node_tree.links.new(tex.outputs['Color'],emission.inputs['Color']);material.node_tree.links.new(emission.outputs[0],out.inputs['Surface'])
target=nodes.new('ShaderNodeTexImage');target.image=image;nodes.active=target
mesh.materials.clear();mesh.materials.append(material)
for p in mesh.polygons:p.material_index=0
mesh.uv_layers.active=mesh.uv_layers['HeadUV'];mesh.uv_layers['HeadUV'].active_render=True
engine=scene.render.engine;scene.render.engine='CYCLES';scene.cycles.samples=1
bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
try:
    bpy.ops.object.bake(type='EMIT',use_clear=False,margin=12,use_selected_to_active=False)
    image.save()
finally:
    scene.render.engine=engine
    bpy.data.objects.remove(obj,do_unlink=True);bpy.data.meshes.remove(mesh);bpy.data.materials.remove(material)
result={'texture':str(source/'Daon_Head_BaseTransfer.png'),'size':[2048,2048]}
