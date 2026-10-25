"""TGV 1m/2m 벽 규격에 맞춘 기와 모듈을 별도 Blender 씬에 만든다."""
import bpy
import bmesh
import math
import json
from pathlib import Path

ROOT = Path('C:/UsingProject/UnityProject/UPlayground')
OUT = ROOT / 'ArtSource/WorldCities/EastStyle'
scene = bpy.data.scenes.new('EastStyle_RoofModules')
bpy.context.window.scene = scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
clay = bpy.data.materials.new('EastRoofClay')
clay.diffuse_color = (0.28, 0.34, 0.35, 1)
clay.use_nodes = True
shader = clay.node_tree.nodes.get('Principled BSDF')
shader.inputs['Roughness'].default_value = 0.9
texture = clay.node_tree.nodes.new('ShaderNodeTexImage')
texture.image = bpy.data.images.load(str(OUT / 'EastRoofClay.png'))
clay.node_tree.links.new(texture.outputs['Color'], shader.inputs['Base Color'])
wood = bpy.data.materials.new('EastRoofTimber')
wood.diffuse_color = (0.21, 0.12, 0.065, 1)

def height(distance):
    # 처마 끝의 완만한 반전만 주어 신사 지붕의 과장을 피한다.
    return 1.9 * (1 - distance / 3.2) ** 1.45 + 0.16 * (distance / 3.2) ** 10

def make_surface(name, width, corner=False):
    vertices, faces = [], []
    nx, ny = round(width / .1), 32
    for row in range(ny + 1):
        y = row * .1
        for column in range(nx + 1):
            x = column * .1
            distance = max(x, y) if corner else y
            cross = min(x, y) if corner else x
            rib = .035 * (1 + math.cos(cross * math.pi * 5))
            lap = .035 * (1 - (distance % .4) / .4)
            vertices.append((x, y, height(distance) + rib + lap))
    for row in range(ny):
        for column in range(nx):
            a = row * (nx + 1) + column
            faces.append((a, a + 1, a + nx + 2, a + nx + 1))
    top_count=len(vertices)
    top_faces=list(faces)
    vertices.extend([(x,y,z-.16) for x,y,z in list(vertices)])
    faces.extend([tuple(i+top_count for i in reversed(face)) for face in top_faces])
    # 휘어진 밑면을 큰 n-gon으로 막으면 삼각분할이 윗면을 관통한다.
    # 같은 격자를 뒤집어 밑면으로 사용한다.
    border = list(range(nx + 1)) + [r * (nx + 1) + nx for r in range(1,ny+1)] + list(range(ny*(nx+1)+nx-1,ny*(nx+1)-1,-1)) + [r*(nx+1) for r in range(ny-1,0,-1)]
    for i in range(len(border)):
        j=(i+1)%len(border)
        faces.append((border[j],border[i],border[i]+top_count,border[j]+top_count))
    if corner:
        start=len(vertices)
        for row in range(33):
            distance=row*.1
            for segment in range(8):
                angle=segment*math.pi/4
                sideways=.12*math.cos(angle)/math.sqrt(2)
                vertices.append((distance+sideways,distance-sideways,height(distance)+.10+.10*math.sin(angle)))
        for row in range(32):
            for segment in range(8):
                a=start+row*8+segment;b=start+row*8+(segment+1)%8
                faces.append((a,b,b+8,a+8))
        faces.extend([tuple(reversed(range(start,start+8))),tuple(range(start+256,start+264))])
    mesh=bpy.data.meshes.new(name)
    mesh.from_pydata(vertices,[],faces);mesh.update()
    uv=mesh.uv_layers.new(name='UVMap')
    for polygon in mesh.polygons:
        for loop in polygon.loop_indices:
            vertex=vertices[mesh.loops[loop].vertex_index]
            uv.data[loop].uv=(vertex[0]/3.2,vertex[1]/3.2)
        polygon.material_index=1 if nx*ny <= polygon.index < nx*ny*2 else 0
    obj=bpy.data.objects.new(name,mesh);scene.collection.objects.link(obj)
    mesh.materials.append(clay);mesh.materials.append(wood)
    return obj

def make_ridge():
    vertices,faces=[],[]
    for row in range(11):
        x=row*.2
        for step in range(9):
            angle=math.pi*step/8
            vertices.append((x,math.cos(angle)*.16,math.sin(angle)*.16+.03))
    for row in range(10):
        for step in range(8):
            a=row*9+step
            faces.append((a,a+9,a+10,a+1))
    faces.extend([tuple(reversed(range(9))),tuple(range(90,99))])
    for row in range(10):
        a=row*9
        faces.append((a,a+8,a+17,a+9))
    mesh=bpy.data.meshes.new('EastRoof_Ridge_2m');mesh.from_pydata(vertices,[],faces);mesh.update()
    uv=mesh.uv_layers.new(name='UVMap')
    for loop in mesh.loops:
        v=vertices[loop.vertex_index];uv.data[loop.index].uv=(v[0]/2,(v[1]+.16)/.32)
    mesh.materials.append(clay)
    obj=bpy.data.objects.new('EastRoof_Ridge_2m',mesh);scene.collection.objects.link(obj)
    return obj

modules=[make_surface('EastRoof_Straight_2m',2),make_surface('EastRoof_Corner_3p2m',3.2,True),make_ridge()]
report=[]
for obj in modules:
    topology=bmesh.new();topology.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(topology,faces=list(topology.faces))
    topology.to_mesh(obj.data);topology.free()
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True);bpy.context.view_layer.objects.active=obj
    bpy.context.view_layer.update()
    bpy.ops.export_scene.fbx(filepath=str(OUT/(obj.name+'.fbx')),use_selection=True,object_types={'MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_anim=False,use_triangles=True,path_mode='STRIP')
    report.append({'name':obj.name,'dimensionsBlender':list(obj.dimensions),'vertices':len(obj.data.vertices),'triangles':sum(len(p.vertices)-2 for p in obj.data.polygons),'materials':[m.name for m in obj.data.materials]})
for index,obj in enumerate(modules):
    obj.hide_set(False)
    obj.location.x=index*4.5
# 이 씬만 쓰므로 다른 세션의 원본 Blender 파일/도시 모델은 저장하거나 바꾸지 않는다.
bpy.data.libraries.write(str(OUT/'EastRoofModules.blend'),{scene},path_remap='RELATIVE',fake_user=True,compress=True)
(OUT/'mesh_validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
result={'modules':report,'source':str(OUT/'EastRoofModules.blend')}
