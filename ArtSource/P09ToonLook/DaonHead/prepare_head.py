"""기존 표정 구조를 보존하는 얼굴 조형과 얼굴 전용 UV를 만든다."""
import bpy
import json
import math
from pathlib import Path
from mathutils import Vector

PROJECT=Path('C:/UsingProject/UnityProject/UPlayground')
SOURCE=PROJECT/'ArtSource/P09ToonLook/DaonHead'
OUTPUT=PROJECT/'output/P09DaonHead'
scene=bpy.data.scenes['Daon_Sculpt_Review']
bpy.context.window.scene=scene
original=bpy.data.objects['Daon_Face_Refined']
head=bpy.data.objects.get('Daon_Head_HighQuality')
if head is None:
    head=original.copy();head.name='Daon_Head_HighQuality';scene.collection.objects.link(head)
head.data=original.data.copy()
head.hide_set(False);head.hide_render=False
components=json.loads((PROJECT/'output/P09DaonSculpt/face_components.json').read_text())
islands=json.loads((SOURCE/'source_uv_islands.json').read_text())
part={v:i for i,ids in enumerate(components) for v in ids}

def sculpt(co,component):
    x,y,z=co
    front=max(0,min(1,(-y-.012)/.035))
    if component==0:
        cheek=math.exp(-((abs(x)-.044)/.021)**2-((z-1.391)/.019)**2)*front
        y-=.0016*cheek
        x+=math.copysign(.0007*cheek,x)
        chin=math.exp(-(x/.023)**2-((z-1.342)/.012)**2)*front
        x*=1+.035*chin
        # 코끝을 완만하게 하고 콧등의 흐름을 연결한다.
        nose=math.exp(-(x/.009)**2-((z-1.384)/.010)**2)*front
        y+=.0014*nose
        bridge=math.exp(-(x/.009)**2-((z-1.401)/.011)**2)*front
        y-=.0009*bridge
        lip=math.exp(-(x/.014)**4-((z-1.362)/.004)**2)*front
        y-=.0008*lip
    # 눈꺼풀과 부속 눈꺼풀 메시를 같은 변형장으로 움직인다.
    if component in set(range(19))|{25,26,27,28}:
        lid=math.exp(-((abs(x)-.037)/.026)**4-((z-1.419)/.006)**2)*front
        y-=.0006*lid
    if component==21:
        y+=.0012
    return Vector((x,y,z))

for dst,src in zip(head.data.shape_keys.key_blocks,original.data.shape_keys.key_blocks):
    for i,(a,b) in enumerate(zip(dst.data,src.data)):
        a.co=sculpt(b.co,part[i])
for i,(a,b) in enumerate(zip(head.data.vertices,original.data.vertices)):
    a.co=sculpt(b.co,part[i])

# 눈·속눈썹·입안과 열린 경계는 기존 곡선을 유지한다.
edge_uses={e.index:0 for e in head.data.edges}
for poly in head.data.polygons:
    for loop in poly.loop_indices:edge_uses[head.data.loops[loop].edge_index]+=1
crease=head.data.attributes.get('crease_edge') or head.data.attributes.new('crease_edge','FLOAT','EDGE')
for edge in head.data.edges:
    smooth=all(part[v] in {0,19,20} for v in edge.vertices) and edge_uses[edge.index]==2
    crease.data[edge.index].value=0 if smooth else 1
    # 입술 접합부가 수축하면 무표정에서 치아가 드러나므로 원래 곡선을 고정한다.
    if all(abs(head.data.vertices[v].co.x)<.023 and 1.351<head.data.vertices[v].co.z<1.375 and head.data.vertices[v].co.y<-.035 for v in edge.vertices):
        crease.data[edge.index].value=1
for poly in head.data.polygons:poly.use_smooth=True
sub=head.modifiers.get('얼굴 곡면') or head.modifiers.new('얼굴 곡면','SUBSURF')
sub.levels=1;sub.render_levels=1;sub.use_creases=True;sub.use_custom_normals=False
sub.uv_smooth='NONE'

old_uv=head.data.uv_layers.active
old_uv.name='SourceUV'
new_uv=head.data.uv_layers.new(name='HeadUV',do_init=True)
slots={3:(.02,.80,.37,.98),26:(.82,.98,.80,.97),27:(.82,.98,.61,.78),28:(.82,.98,.42,.59),29:(.82,.98,.23,.40)}
large=[0,1,2,4,5,31,32,33,34]
for j,i in enumerate(large):
    col=j%5;row=j//5
    slots[i]=(.015+col*.158,.155+col*.158,.02+row*.165,.165+row*.165)
small=[i for i in range(len(islands)) if i not in slots]
for j,i in enumerate(small):
    col=j%2;row=j//2
    slots[i]=(.815+col*.09,.89+col*.09,.004+row*.021,.020+row*.021)
layout=[]
for i,ids in enumerate(islands):
    loop_ids=[li for pi in ids for li in head.data.polygons[pi].loop_indices]
    coords=[old_uv.data[li].uv.copy() for li in loop_ids]
    minimum=Vector((min(c.x for c in coords),min(c.y for c in coords)))
    maximum=Vector((max(c.x for c in coords),max(c.y for c in coords)))
    x0,x1,y0,y1=slots[i]
    scale=min((x1-x0)/(maximum.x-minimum.x),(y1-y0)/(maximum.y-minimum.y))
    center=Vector(((x0+x1)/2,(y0+y1)/2))
    for li,co in zip(loop_ids,coords):new_uv.data[li].uv=(co-(minimum+maximum)/2)*scale+center
    layout.append({'island':i,'slot':slots[i],'source_min':list(minimum),'source_max':list(maximum),'scale':scale})
head.data.uv_layers.active=new_uv;new_uv.active_render=True
(SOURCE/'uv_layout.json').write_text(json.dumps(layout,indent=2))
original.hide_render=True
scene.render.filepath=str(OUTPUT/'Head_Clay.png')
bpy.context.view_layer.update()
bpy.ops.render.render(write_still=True)
result={'control_vertices':len(head.data.vertices),'shapes':len(head.data.shape_keys.key_blocks)-1,'uv_islands':len(islands),'max_sculpt_offset_m':max((a.co-b.co).length for a,b in zip(head.data.vertices,original.data.vertices))}
(OUTPUT/'head_preparation.json').write_text(json.dumps(result,indent=2))
