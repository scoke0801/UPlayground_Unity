"""최종 Unity 목 연결면·36표정·웨이트를 Blender 제작 원본에도 보관한다."""
import bpy
import struct
import numpy as np
from pathlib import Path

folder=Path(__file__).resolve().parent
project=folder.parents[2]
bpy.ops.wm.open_mainfile(filepath=str(folder/'Bokusei_P09_Face.blend'))

def read_name(f):
    length=shift=0
    while True:
        b=f.read(1)[0];length|=(b&127)<<shift
        if not b&128:break
        shift+=7
    return f.read(length).decode('utf-8')

with (folder/'final.bin').open('rb') as f:
    def integer():return struct.unpack('<i',f.read(4))[0]
    def points(n,width=3,dtype='<f4'):return np.frombuffer(f.read(n*width*4),dtype).reshape(n,width).copy()
    count,slots,shapes=integer(),integer(),integer()
    vertices=points(count);uv=points(count,2)
    triangles=[points(integer()//3,3,'<i4') for _ in range(slots)]
    keys=[(read_name(f),points(count)) for _ in range(shapes)]
    weights=[struct.unpack('<4i4f',f.read(32)) for _ in range(count)]
    names=[read_name(f) for _ in range(integer())]
    normals=points(count)
for obj in bpy.context.scene.objects:
    obj.hide_set(True);obj.hide_render=True
previous=bpy.data.objects.get('Bokusei_P09_Final')
if previous:bpy.data.objects.remove(previous,do_unlink=True)
mesh=bpy.data.meshes.new('Bokusei_P09_Final')
mesh.from_pydata(vertices.tolist(),[],np.concatenate(triangles).tolist());mesh.update()
obj=bpy.data.objects.new('Bokusei_P09_Final',mesh);bpy.context.collection.objects.link(obj)
layer=mesh.uv_layers.new(name='UVMap')
for loop in mesh.loops:layer.data[loop.index].uv=uv[loop.vertex_index]
offset=0
for slot,tri in enumerate(triangles):
    material=bpy.data.materials.new('Bokusei_Final_Slot_'+str(slot));material.use_nodes=True
    shader=material.node_tree.nodes.get('Principled BSDF');shader.inputs['Roughness'].default_value=.8
    texture=None
    if slot==0:texture=folder/'Bokusei_Face_Albedo.png'
    elif slot==3:texture=folder/'EyesR03/Bokusei_SageIris.png'
    else:texture=project/'Assets/ExternalAssets/Character/ROKO SHOP/Bokusei/04_Textures/Tx_FaceParts/Tx_Bokusei_FaceParts.png'
    if texture:
        node=material.node_tree.nodes.new('ShaderNodeTexImage');node.image=bpy.data.images.load(str(texture),check_existing=True);node.image.pack();material.node_tree.links.new(node.outputs['Color'],shader.inputs['Base Color'])
    else:shader.inputs['Base Color'].default_value=(.75,.55,.49,1)
    mesh.materials.append(material)
    for polygon in list(mesh.polygons)[offset:offset+len(tri)]:polygon.material_index=slot;polygon.use_smooth=True
    offset+=len(tri)
obj.shape_key_add(name='Basis')
mesh.normals_split_custom_set_from_vertices(normals.tolist())
for name,delta in keys:obj.shape_key_add(name=name).data.foreach_set('co',(vertices+delta).ravel())
for name in names:obj.vertex_groups.new(name=name)
for i,weight in enumerate(weights):
    for bone,value in zip(weight[:4],weight[4:]):
        if value>0:obj.vertex_groups[bone].add([i],value,'REPLACE')
bpy.context.view_layer.objects.active=obj;obj.select_set(True)
assert len(mesh.vertices)==8455 and len(obj.data.shape_keys.key_blocks)==37
bpy.ops.wm.save_as_mainfile(filepath=str(folder/'Bokusei_P09_Face.blend'),compress=True)
print('최종 메시 8455정점/36표정/77본 그룹 저장')

