"""Siuha의 원본 얼굴·표정에 같은 변형을 적용하고 P09 두피와 목에 맞춘다."""
import bpy
import json
import struct
from pathlib import Path
from collections import Counter, defaultdict
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT=Path(__file__).resolve().parent

def read_string(stream):
    size=shift=0
    while True:
        byte=stream.read(1)[0];size|=(byte&127)<<shift
        if not byte&128:break
        shift+=7
    return stream.read(size).decode('utf-8')

with (ROOT/'source.bin').open('rb') as f:
    def integer():return struct.unpack('<i',f.read(4))[0]
    def array(n,w=3,dtype='<f4'):return np.frombuffer(f.read(n*w*4),dtype).reshape(n,w).copy()
    count,slots,shape_count=integer(),integer(),integer()
    source=array(count);uv=array(count,2)
    triangles=[array(integer()//3,3,'<i4') for _ in range(slots)]
    shapes=[(read_string(f),array(count)) for _ in range(shape_count)]
    native=array(integer());native_triangles=array(integer()//3,3,'<i4')

def smooth(a,b,value):
    t=np.clip((value-a)/(b-a),0,1)
    return t*t*(3-2*t)

def sculpt(points):
    result=points.copy();x,y,z=points.T
    front=1-smooth(-.012,.012,y)
    # 눈꺼풀·안구·속눈썹을 같은 공간 변형으로 줄여 눈 감기 경계를 보존한다.
    eye=np.exp(-((np.abs(x)-.034)/.031)**6-((z-1.414)/.027)**6)*front
    result[:,0]-=np.sign(x)*(np.abs(x)-.034)*.10*eye
    result[:,2]-=(z-1.414)*.20*eye
    brow=np.exp(-((z-1.437)/.008)**2-((np.abs(x)-.033)/.03)**4)*front
    result[:,2]-=.0015*brow
    cheek=np.exp(-((z-1.386)/.019)**2)*front
    result[:,0]*=1-.025*cheek
    jaw=np.exp(-((z-1.348)/.014)**2)*front
    result[:,0]+=.0022*np.tanh(x/.012)*jaw
    result[:,2]+=.0020*jaw
    nose=np.exp(-(x/.009)**2-((z-1.385)/.012)**2)*front
    result[:,1]-=.0015*nose
    mouth=np.exp(-(x/.022)**4-((z-1.369)/.010)**4)*front
    result[:,0]+=.08*x*mouth
    result[:,2]+=.0007*mouth
    return result

def components(faces):
    links=defaultdict(set)
    for t in faces:
        for a in t:links[int(a)].update(map(int,t))
    seen=set();parts=[]
    for start in links:
        if start in seen:continue
        todo=[start];part=[]
        while todo:
            a=todo.pop()
            if a in seen:continue
            seen.add(a);part.append(a);todo.extend(links[a]-seen)
        parts.append(np.array(part))
    return parts

def boundaries(v,faces):
    _,first,inv=np.unique(np.round(v,6),axis=0,return_index=True,return_inverse=True)
    edges=Counter(tuple(sorted((int(inv[a]),int(inv[b])))) for t in faces for a,b in zip(t,np.roll(t,-1)))
    border=np.array([[first[a],first[b],first[b]] for (a,b),n in edges.items() if n==1])
    return components(border)

def angles(v):return np.mod(np.arctan2(v[:,0],.025-v[:,1]),2*np.pi)

skin=max(components(triangles[0]),key=len)
source_loop=next(p for p in boundaries(source,triangles[0]) if len(p)==20 and source[p,2].max()<1.38)
native_loop=next(p for p in boundaries(native,native_triangles) if native[p,2].max()<1.35)
source_loop=source_loop[np.argsort(angles(source[source_loop]))]
native_loop=native_loop[np.argsort(angles(native[native_loop]))]
assert len(native_loop)==16
with (ROOT/'scalp.bin').open('rb') as f:
    n=struct.unpack('<i',f.read(4))[0];scalp=np.frombuffer(f.read(n*12),'<f4').reshape(n,3)
    n=struct.unpack('<i',f.read(4))[0];scalp_tris=np.frombuffer(f.read(n*4),'<i4').reshape(-1,3)
head=np.concatenate([native,scalp]);faces=np.concatenate([native_triangles,scalp_tris+len(native)])
surface=BVHTree.FromPolygons([Vector(v) for v in head],faces.tolist(),all_triangles=True)
shaped=sculpt(source);offsets=np.zeros_like(source)
center=Vector((0,.025,1.44));misses=[];fitted=0
for i in skin:
    p=shaped[i]
    amount=max(float(smooth(1.445,1.485,p[2])),float(smooth(.010,.045,p[1])*smooth(1.395,1.445,p[2])))
    if amount<=0:continue
    direction=(Vector(p)-center).normalized();hit=surface.ray_cast(center,direction)
    if hit[0] is None:misses.append(int(i));continue
    offsets[i]=(np.array(hit[0]-direction*.002)-p)*amount;fitted+=1
assert not misses, f'두피 투영 누락: {misses}'
shaped+=offsets
# UV 분리 경계도 같은 공간 위치에 남긴다.
_,first,inv=np.unique(np.round(source,6),axis=0,return_index=True,return_inverse=True)
for i in skin:shaped[i]=shaped[first[inv[i]]]
offsets=shaped-sculpt(source)
bridge=[];a=b=0;ta=angles(shaped[source_loop]);ba=angles(native[native_loop])
while a<len(source_loop) or b<len(native_loop):
    ai=a%len(source_loop);bi=b%len(native_loop)
    na=ta[(a+1)%len(source_loop)]+(2*np.pi if a+1>=len(source_loop) else 0)
    nb=ba[(b+1)%len(native_loop)]+(2*np.pi if b+1>=len(native_loop) else 0)
    if a<len(source_loop) and (b==len(native_loop) or na<nb):
        t=[int(source_loop[ai]),int(source_loop[(ai+1)%len(source_loop)]),-int(native_loop[bi])-1];a+=1
    else:
        t=[int(source_loop[ai]),-int(native_loop[(bi+1)%len(native_loop)])-1,-int(native_loop[bi])-1];b+=1
    p=np.array([shaped[i] if i>=0 else native[-i-1] for i in t])
    normal=np.cross(p[1]-p[0],p[2]-p[0]);radial=p.mean(0)-[0,.025,p[:,2].mean()]
    if np.dot(normal,radial)<0:t.reverse()
    bridge.extend(t)

assert np.isfinite(shaped).all()
with (ROOT/'sculpt.bin').open('wb') as f:
    f.write(struct.pack('<ii',count,shape_count));f.write(shaped.astype('<f4').tobytes())
    for _,delta in shapes:
        d=sculpt(source+delta)-sculpt(source)
        assert np.isfinite(d).all();f.write(d.astype('<f4').tobytes())
with (ROOT/'neck_topology.bin').open('wb') as f:
    f.write(struct.pack('<i',len(native_loop)));f.write(native_loop.astype('<i4').tobytes())
    f.write(struct.pack('<i',len(bridge)));f.write(np.array(bridge,dtype='<i4').tobytes());f.write(struct.pack('<i',0))

# 전용 background 프로세스만 사용하므로 열린 사용자 문서를 건드리지 않는다.
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
for name,points in [('Siuha_SourceAligned',source),('Siuha_P09_NewFace',shaped)]:
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(points.tolist(),[],np.concatenate(triangles).tolist());mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
    layer=mesh.uv_layers.new(name='UVMap')
    for loop in mesh.loops:layer.data[loop.index].uv=uv[loop.vertex_index]
    for p in mesh.polygons:p.use_smooth=True
    obj.shape_key_add(name='Basis')
    for key,d in shapes:obj.shape_key_add(name=key).data.foreach_set('co',(source+d if name.endswith('Aligned') else sculpt(source+d)+offsets).ravel())
    obj.hide_set(name.endswith('Aligned'));obj.hide_render=name.endswith('Aligned')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Siuha_P09_Face.blend'),compress=True)
report={'vertices':count,'source_expressions':shape_count,'source_neck_ring':len(source_loop),'target_neck_ring':len(native_loop),'bridge_triangles':len(bridge)//3,'scalp_fitted':fitted,'scalp_misses':len(misses),'sculpt_max_m':float(np.linalg.norm(sculpt(source)-source,axis=1).max()),'eye_horizontal_scale':.9,'eye_vertical_scale':.8}
(ROOT/'sculpt_report.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print(json.dumps(report))
