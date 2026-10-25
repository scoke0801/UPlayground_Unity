"""완료된 교환 데이터를 별도 background Blender에서 복원해 제작 원본을 저장한다."""
import bpy, json, struct
import numpy as np
from pathlib import Path
root=Path('C:/UsingProject/UnityProject/UPlayground')
folder=root/'ArtSource/ExternalCharacterBuilder/HichiFace'
assert bpy.app.background, '사용자가 연 Blender 세션에서는 실행하지 않습니다.'
for obj in list(bpy.data.objects):
    bpy.data.objects.remove(obj,do_unlink=True)
bpy.context.preferences.filepaths.save_version=0
data=json.loads((folder/'source.json').read_text(encoding='utf-8'))
with (folder/'sculpt.bin').open('rb') as stream:
    count,frames=struct.unpack('<ii',stream.read(8))
    base=np.frombuffer(stream.read(count*12),dtype='<f4').reshape(-1,3)
    mesh=bpy.data.meshes.new('Hichi_P09_Sculpt')
    faces=[t for slot in data['submeshes'] for t in np.array(slot).reshape(-1,3)[:,::-1].tolist()]
    mesh.from_pydata(base.tolist(),[],faces);mesh.update()
    obj=bpy.data.objects.new('Hichi_NewFace',mesh);bpy.context.scene.collection.objects.link(obj)
    obj.shape_key_add(name='Basis')
    for f in range(frames):
        length=struct.unpack('<i',stream.read(4))[0];name=stream.read(length).decode('utf-8');weight=struct.unpack('<f',stream.read(4))[0]
        positions=np.frombuffer(stream.read(count*12),dtype='<f4')
        obj.shape_key_add(name=name).data.foreach_set('co',positions)
    assert stream.read()==b''
uv=mesh.uv_layers.new(name='UVMap')
for loop in mesh.loops:uv.data[loop.index].uv=data['uv'][loop.vertex_index]
for face in mesh.polygons:face.use_smooth=True
mat=bpy.data.materials.new('Generated_Face_Albedo');mat.use_nodes=True
tex=mat.node_tree.nodes.new('ShaderNodeTexImage')
tex.image=bpy.data.images.load(str(root/'Assets/08.Models/Characters/HichiP09/Hichi_Face_Albedo.png'));tex.image.pack()
mat.node_tree.links.new(tex.outputs['Color'],mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color']);mesh.materials.append(mat)
bpy.context.view_layer.objects.active=obj;obj.select_set(True)
obj.location.z=-1.42
bpy.ops.wm.save_as_mainfile(filepath=str(folder/'Hichi_P09_Face.blend'),compress=True)
(folder/'blend_save_result.json').write_text(json.dumps({'vertices':count,'shapes':frames,'blender':bpy.app.version_string,'saved':True}),encoding='utf-8')
