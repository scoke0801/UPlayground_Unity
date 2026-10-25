"""Blender 조형을 Unity 원본 본·표정에 연결할 수 있는 중간 데이터로 내보낸다."""
import bpy
import json
import xml.etree.ElementTree as ET
from pathlib import Path

project=Path('C:/UsingProject/UnityProject/UPlayground')
source=project/'ArtSource/P09ToonLook/DaonSculpt'
source.mkdir(parents=True,exist_ok=True)
face=bpy.data.objects['Daon_Face_Refined']
original=bpy.data.objects['Female_Face_02']
face_data={
    'original':[list(v.co) for v in original.data.vertices],
    'vertices':[list(v.co) for v in face.data.vertices],
    'shapes':{k.name:[list(v.co) for v in k.data] for k in face.data.shape_keys.key_blocks if k.name!='Basis'},
}
hair=bpy.data.objects['Daon_Hair_Refined']
armature=next(m for m in hair.modifiers if m.type=='ARMATURE')
armature.show_viewport=False
bpy.context.view_layer.update()
evaluated=hair.evaluated_get(bpy.context.evaluated_depsgraph_get())
mesh=evaluated.to_mesh(preserve_all_data_layers=True,depsgraph=bpy.context.evaluated_depsgraph_get())
try:
    mesh.calc_loop_triangles()
    hair_data={
        'vertices':[list(v.co) for v in mesh.vertices],
        'groups':[g.name for g in hair.vertex_groups],
        'weights':[[[g.group,g.weight] for g in v.groups] for v in mesh.vertices],
        'loops':[[l.vertex_index,*mesh.uv_layers.active.data[l.index].uv,*mesh.corner_normals[l.index].vector] for l in mesh.loops],
        'triangles':[list(t.loops) for t in mesh.loop_triangles],
    }
    # UV와 법선이 같은 코너는 하나의 렌더 정점으로 공유한다.
    unique=[];lookup={};mapping=[]
    for loop in hair_data['loops']:
        key=tuple(round(x,6) for x in loop)
        if key not in lookup:lookup[key]=len(unique);unique.append(loop)
        mapping.append(lookup[key])
    hair_data['loops']=unique
    hair_data['triangles']=[[mapping[i] for i in triangle] for triangle in hair_data['triangles']]
finally:
    evaluated.to_mesh_clear()
    armature.show_viewport=True
    bpy.context.view_layer.update()
root=ET.Element('sculpt')
for name,part in [('face',face_data),('hair',hair_data)]:
    element=ET.SubElement(root,name)
    for key,value in part.items():
        container=ET.SubElement(element,key)
        if key=='shapes':
            for shape,coords in value.items():
                shape_element=ET.SubElement(container,'shape',name=shape)
                for co in coords:ET.SubElement(shape_element,'v').text=' '.join(map(str,co))
        elif key=='weights':
            for weights in value:
                vertex=ET.SubElement(container,'v')
                for group,weight in weights:ET.SubElement(vertex,'w',group=str(group),weight=str(weight))
        else:
            for item in value:ET.SubElement(container,'v').text=item if isinstance(item,str) else ' '.join(map(str,item))
ET.ElementTree(root).write(source/'sculpt.xml',encoding='utf-8',xml_declaration=True)
bpy.data.libraries.write(str(source/'Daon_Sculpt.blend'),{bpy.context.scene},path_remap='ABSOLUTE',fake_user=True,compress=True)
result={'hair_vertices':len(hair_data['vertices']),'hair_triangles':len(hair_data['triangles']),'unity_loop_vertices':len(hair_data['loops']),'face_shapes':len(face_data['shapes'])}
