"""단구·성벽·여장·대계단을 하나의 재질별 메시와 세 단계 LOD로 만든다."""
import json
import math
import shutil
import sys
from pathlib import Path

SOURCE=Path(__file__).resolve().parent
sys.path.insert(0,str(SOURCE))
from build_castle_highpoly import bpy, MeshBatch, materials, scene, ROOT
import build_castle_highpoly as castle

# 수천 개의 성돌에는 한 번의 모따기로 충분하다. 건물 부재의 두 단계 모따기는 유지한다.
bevel_mesh=castle.bmesh.new()
castle.bmesh.ops.create_cube(bevel_mesh,size=1)
castle.bmesh.ops.bevel(bevel_mesh,geom=list(bevel_mesh.edges),offset=.035,segments=1,affect='EDGES')
bevel_mesh.verts.ensure_lookup_table()
bevel_mesh.verts.index_update()
castle.ROUND_BOX=([tuple(v.co) for v in bevel_mesh.verts],[tuple(v.index for v in f.verts) for f in bevel_mesh.faces])
bevel_mesh.free()

CFG=json.loads((SOURCE/'fortress_layout.json').read_text(encoding='utf-8'))
OUT=ROOT/'Assets/08.Models/WorldCities/StonegateHighPoly'
STAGING=SOURCE/'FortressExport'
STAGING.mkdir(exist_ok=True)


def masonry(batch,length,width,bottom,top):
    """석축 몸체와 엇갈린 줄눈을 만든다."""
    height=top-bottom
    batch.box('Stone',(0,0,(top+bottom)/2),(length,width,height))
    cfg=CFG['masonry']
    if batch.lod<2:
        rows=max(1,round(height/cfg['courseHeight']))
        for side in range(4):
            front=side<2
            sign=-1 if side%2==0 else 1
            span=length if front else width
            count=max(1,round(span/cfg['blockWidth']))
            for row in range(rows):
                edges=[-span/2]+[-span/2+(i+.5*(row%2))*span/count for i in range(1,count)]+[span/2]
                for index,(a,b) in enumerate(zip(edges,edges[1:])):
                    outward=.12+.06*((index+row)%3)
                    along=(a+b)/2
                    position=(along,sign*(width/2+outward),bottom+(row+.5)*height/rows) if front else (sign*(length/2+outward),along,bottom+(row+.5)*height/rows)
                    size=(b-a-cfg['joint'],.36,height/rows-cfg['joint']) if front else (.36,b-a-cfg['joint'],height/rows-cfg['joint'])
                    batch.box('Stone',position,size,batch.lod==0)
    batch.box('Stone',(0,0,top+.08),(length+.3,width+.4,.24),True)


def parapet(batch,length,width,top):
    """보행로 양쪽에 낮은 벽과 총안이 있는 여장을 둔다."""
    cfg=CFG['wall']
    for sign in (-1,1):
        y=sign*(width/2-.28)
        batch.box('Stone',(0,y,top+.4),(length,.65,.8),True)
        count=max(1,round(length/cfg['merlonSpacing']))
        for i in range(count):
            x=-length/2+(i+.5)*length/count
            span=min(2.1,length/count*.68)
            z=top+.8
            # 총안은 검은 면으로 그리지 않고 실제 틈으로 남긴다.
            for edge in (-1,1):
                batch.box('Stone',(x+edge*(span+.22)/4,y,z+.27),((span-.22)/2,.7,.54),True)
            batch.box('Stone',(x,y,z+.63),(span+.12,.8,.20),True)


def place(batch,cx,cz,yaw=0):
    """Unity 지면 좌표를 Blender 좌표로 바꿔 부재를 배치한다."""
    ox,oy,oz=CFG['origin']
    cosine,sine=math.cos(yaw),math.sin(yaw)
    def transform(v):
        x,y,z=v
        return (cx+cosine*x-sine*y-ox,-(cz+sine*x+cosine*y-oz),z-oy)
    batch.transform=transform


def build(lod):
    batch=MeshBatch('Fortress',lod)
    wall=CFG['wall']
    for a,b in zip(CFG['perimeter'],CFG['perimeter'][1:]):
        length=math.dist(a,b)
        place(batch,(a[0]+b[0])/2,(a[1]+b[1])/2,math.atan2(b[1]-a[1],b[0]-a[0]))
        masonry(batch,length+.4,wall['width'],wall['bottom'],wall['top'])
        parapet(batch,length,wall['width'],wall['top'])
    for terrace in CFG['terraces']:
        place(batch,*terrace['center'])
        masonry(batch,terrace['width'],terrace['depth'],terrace['bottom'],terrace['top'])
    # 본성 단구 전면과 측면 난간은 중앙 대계단과 닫힌 정문 앞을 비운다.
    for cx,cz,length,yaw in CFG['terraceRails']:
        place(batch,cx,cz,math.radians(yaw))
        batch.box('Stone',(0,0,18.55),(length,.7,1.1),True)
        for i in range(round(length/2.5)+1):
            x=-length/2+i*length/round(length/2.5)
            batch.box('Stone',(x,0,18.9),(.46,.85,1.4),True)
            batch.box('Stone',(x,0,19.65),(.68,1,.18),True)
    stairs=CFG['stairs']
    place(batch,stairs['x'],stairs['front'])
    for i in range(stairs['steps']):
        top=stairs['bottom']+(i+1)*stairs['rise']
        batch.box('Stone',(0,(i+.5)*stairs['run'],top-.2),(stairs['width'],stairs['run']+.03,.4),True)
        for sign in (-1,1):
            batch.box('Stone',(sign*(stairs['width']/2+.32),(i+.5)*stairs['run'],top+.3),(.64,stairs['run']+.04,.6),True)
    batch.transform=None
    return batch.finish()


report=[]
previous=float('inf')
for lod in range(3):
    objects,stats=build(lod)
    triangles=sum(s['triangles'] for s in stats.values())
    assert 0<triangles<previous,(lod,triangles,previous)
    previous=triangles
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.export_scene.fbx(filepath=str(STAGING/f'Fortress_LOD{lod}.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_anim=False,use_triangles=True,path_mode='STRIP')
    for obj in objects:
        obj.hide_render=lod!=0
        obj.hide_set(lod!=0)
    report.append({'lod':lod,'triangles':triangles,'materials':stats})
    print(f'FORTRESS LOD{lod}: {triangles}',flush=True)
bpy.data.libraries.write(str(SOURCE/'StonegateFortress.blend'),{scene},path_remap='RELATIVE',fake_user=True,compress=True)
(SOURCE/'fortress_mesh_validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
for file in STAGING.glob('*.fbx'):
    shutil.copyfile(file,OUT/file.name)
    file.unlink()
STAGING.rmdir()
print('FORTRESS_BUILD_COMPLETE',flush=True)
