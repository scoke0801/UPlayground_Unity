"""얼굴 곡면과 모든 표정을 같은 토폴로지로 평가해 Unity에 전달한다."""
import bpy
import json
import xml.etree.ElementTree as ET
from pathlib import Path

folder=Path('C:/UsingProject/UnityProject/UPlayground/ArtSource/P09ToonLook/DaonPortrait')
obj=bpy.data.objects['Daon_Portrait']
# 기존 코 밑의 별도 짙은 선은 새 피부 음영과 겹치므로 출력에서 제외한다.
components=json.loads((folder.parents[2]/'output/P09DaonSculpt/face_components.json').read_text())
nose=set(components[21]+components[26]+components[28])
excluded=obj.data.attributes.get('exclude_nose_line') or obj.data.attributes.new('exclude_nose_line','BOOLEAN','FACE')
for polygon in obj.data.polygons:excluded.data[polygon.index].value=all(i in nose for i in polygon.vertices)
arm=next(m for m in obj.modifiers if m.type=='ARMATURE')
arm.show_viewport=False
keys=obj.data.shape_keys.key_blocks
for key in keys:key.value=0
root=ET.Element('head')
def rows(parent,name,values):
    node=ET.SubElement(parent,name)
    for value in values:ET.SubElement(node,'v').text=value if isinstance(value,str) else ' '.join(format(float(x),'.9g') for x in value)
    return node
def evaluate():
    bpy.context.view_layer.update()
    dg=bpy.context.evaluated_depsgraph_get()
    evaluated=obj.evaluated_get(dg)
    mesh=evaluated.to_mesh(preserve_all_data_layers=True,depsgraph=dg)
    return evaluated,mesh
try:
    evaluated,mesh=evaluate()
    mesh.calc_loop_triangles()
    base=[v.co.copy() for v in mesh.vertices]
    rows(root,'vertices',base)
    rows(root,'groups',[g.name for g in obj.vertex_groups])
    weights=ET.SubElement(root,'weights')
    for v in mesh.vertices:
        node=ET.SubElement(weights,'v')
        for group in v.groups:
            if group.weight>0.000001:ET.SubElement(node,'w',group=str(group.group),weight=str(group.weight))
    uv=mesh.uv_layers['HeadUV']
    unique=[];lookup={};mapping=[];source_loops=[]
    for loop in mesh.loops:
        data=[loop.vertex_index,*uv.data[loop.index].uv,*mesh.corner_normals[loop.index].vector]
        key=tuple(round(x,6) for x in data)
        if key not in lookup:
            lookup[key]=len(unique);unique.append(data);source_loops.append(loop.index)
        mapping.append(lookup[key])
    rows(root,'loops',unique)
    rows(root,'triangles',[[t.material_index,*[mapping[i] for i in t.loops]] for t in mesh.loop_triangles if not mesh.attributes['exclude_nose_line'].data[t.polygon_index].value])
    loop_count=len(mesh.loops)
    evaluated.to_mesh_clear()
    shapes=ET.SubElement(root,'shapes')
    for key in list(keys)[1:]:
        key.value=1
        evaluated,mesh=evaluate()
        assert len(mesh.vertices)==len(base) and len(mesh.loops)==loop_count,key.name
        shape=ET.SubElement(shapes,'shape',name=key.name)
        rows(shape,'delta',[v.co-base[i] for i,v in enumerate(mesh.vertices)])
        rows(shape,'normals',[mesh.corner_normals[i].vector for i in source_loops])
        evaluated.to_mesh_clear()
        key.value=0
    ET.ElementTree(root).write(folder/'head.xml',encoding='utf-8',xml_declaration=True)
finally:
    for key in keys:key.value=0
    arm.show_viewport=True
    bpy.context.view_layer.update()
bpy.data.libraries.write(str(folder/'Daon_Portrait.blend'),{bpy.context.scene},path_remap='ABSOLUTE',fake_user=True,compress=True)
result={'control_vertices':len(obj.data.vertices),'evaluated_vertices':len(base),'render_vertices':len(unique),'triangles':len(root.find('triangles')),'shapes':len(shapes)}
