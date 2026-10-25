"""두상을 복원하고 원본 헤어를 피팅하며 목을 연속 곡면으로 다시 만든다."""
import bpy, struct, json
import numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parent
PARENT=ROOT.parent

def string(f):
    n=shift=0
    while True:
        b=f.read(1)[0];n|=(b&127)<<shift
        if not b&128:break
        shift+=7
    return f.read(n).decode('utf-8')

def write_string(f,value):
    data=value.encode('utf-8');n=len(data)
    while n>127:f.write(bytes([(n&127)|128]));n>>=7
    f.write(bytes([n]));f.write(data)

def read_part(name):
    with (ROOT/(name+'.bin')).open('rb') as f:
        n,s=struct.unpack('<ii',f.read(8));v=np.frombuffer(f.read(n*12),'<f4').reshape(n,3).copy();uv=np.frombuffer(f.read(n*8),'<f4').reshape(n,2).copy()
        ts=[]
        for _ in range(s):
            k=struct.unpack('<i',f.read(4))[0];ts.append(np.frombuffer(f.read(k*4),'<i4').reshape(-1,3).copy())
        ws=[struct.unpack('<4i4f',f.read(32)) for _ in range(n)]
    return v,uv,ts,ws

with (PARENT/'final.bin').open('rb') as f:
    n,s,k=struct.unpack('<iii',f.read(12));old=np.frombuffer(f.read(n*12),'<f4').reshape(n,3).copy();old_uv=np.frombuffer(f.read(n*8),'<f4').reshape(n,2).copy()
    triangles=[]
    for _ in range(s):
        size=struct.unpack('<i',f.read(4))[0];triangles.append(np.frombuffer(f.read(size*4),'<i4').reshape(-1,3).copy())
    keys=[(string(f),np.frombuffer(f.read(n*12),'<f4').reshape(n,3).copy()) for _ in range(k)]
    weights=[struct.unpack('<4i4f',f.read(32)) for _ in range(n)]
    names=[string(f) for _ in range(struct.unpack('<i',f.read(4))[0])]

# 원본 얼굴 조형식까지만 읽어 r01의 P09 두피 투영을 실행하지 않는다.
scope={'__file__':str(PARENT/'sculpt_face.py')}
exec((PARENT/'sculpt_face.py').read_text(encoding='utf-8').split('skin=max(')[0],scope)
source=scope['source'];sculpt=scope['sculpt'];smooth=scope['smooth'];original_triangles=scope['triangles'];components=scope['components']
natural=sculpt(source)
skin=max(components(original_triangles[0]),key=len)
# 머리 위쪽만 자연스러운 높이로 압축한다. 눈·눈썹·턱·입은 변경하지 않는다.
def crown(v):
    r=v.copy();r[:,2]-=.25*np.maximum(v[:,2]-1.441,0)*smooth(1.441,1.464,v[:,2]);return r
natural[skin]=crown(natural[skin])
count=len(source)
vertices=natural.tolist();uv=old_uv[:count].tolist();ws=list(weights[:count])
frame_values=[delta[:count].copy() for _,delta in keys]
for delta in frame_values:
    delta[skin]=crown(sculpt(source)[skin]+delta[skin])-crown(sculpt(source)[skin])

top_indices=np.array(sorted(set(triangles[2].ravel())-set(range(n-16,n))))
assert len(top_indices)==20
# 기존 브리지 UV 분리 정점의 실제 원본 정점을 찾는다.
top_source=np.array([int(np.argmin(np.linalg.norm(old[:count]-old[i],axis=1))) for i in top_indices])
bottom_indices=np.arange(n-16,n)
angle=lambda v:np.mod(np.arctan2(v[:,0],.025-v[:,1]),2*np.pi)
top_source=top_source[np.argsort(angle(natural[top_source]))]
bottom_indices=bottom_indices[np.argsort(angle(old[bottom_indices]))]
top=natural[top_source];bottom=old[bottom_indices]

def vertex_normals(v,faces):
    normals=np.zeros_like(v)
    for t in faces:
        normal=np.cross(v[t[1]]-v[t[0]],v[t[2]]-v[t[0]])
        for i in t:normals[i]+=normal
    return normals/np.maximum(np.linalg.norm(normals,axis=1)[:,None],1e-12)

top_normals=vertex_normals(natural,original_triangles[0])[top_source]
body,_,body_tris,_=read_part('Female_Body_Chest')
body_normals=vertex_normals(body,body_tris[0])
bottom_normals=np.array([body_normals[np.argmin(np.linalg.norm(body-p,axis=1))] for p in bottom])

def sample(ring,theta):
    angles=angle(ring);hi=int(np.searchsorted(angles,theta))%len(ring);lo=(hi-1)%len(ring)
    a=angles[lo];b=angles[hi];t=theta
    if b<=a:b+=2*np.pi
    if t<a:t+=2*np.pi
    amount=np.clip((t-a)/(b-a),0,1)
    return lo,hi,amount

def blend_weight(a,b,t):
    values={}
    for weight,amount in [(a,1-t),(b,t)]:
        for bone,value in zip(weight[:4],weight[4:]):values[bone]=values.get(bone,0)+value*amount
    pairs=sorted(values.items(),key=lambda x:-x[1])[:4]
    pairs+= [(0,0)]*(4-len(pairs));total=sum(p[1] for p in pairs)
    return tuple(p[0] for p in pairs)+tuple(p[1]/total for p in pairs)

def append(v,tex,weight,deltas):
    index=len(vertices);vertices.append(v);uv.append(tex);ws.append(weight)
    for i,d in enumerate(deltas):frame_values[i]=np.vstack([frame_values[i],d])
    return index

top_ring=[]
for i in top_source:top_ring.append(append(natural[i],[.5,.15],weights[i],[d[i] for _,d in keys]))
rings=[top_ring]
angles=np.unique(np.round(np.concatenate([angle(top),angle(bottom)]),6))
for t in [.25,.5,.75]:
    ring=[]
    for theta in angles:
        a,b,u=sample(top,theta);c,d,w=sample(bottom,theta)
        p=top[a]*(1-u)+top[b]*u;q=bottom[c]*(1-w)+bottom[d]*w
        tn=top_normals[a]*(1-u)+top_normals[b]*u;bn=bottom_normals[c]*(1-w)+bottom_normals[d]*w
        def tangent(normal):
            normal=normal/max(np.linalg.norm(normal),1e-9)
            down=np.array([0.,0.,-1.]);v=down-normal*np.dot(normal,down)
            if v[2]>-.25:v=down
            return v/max(np.linalg.norm(v),1e-9)
        distance=np.linalg.norm(q-p)/3
        control1=p+tangent(tn)*distance;control2=q-tangent(bn)*distance
        point=(1-t)**3*p+3*(1-t)**2*t*control1+3*(1-t)*t*t*control2+t**3*q
        top_uv=old_uv[top_source[a]]*(1-u)+old_uv[top_source[b]]*u
        tex=np.array([.5,.15])
        top_w=blend_weight(weights[top_source[a]],weights[top_source[b]],u)
        bottom_w=blend_weight(weights[bottom_indices[c]],weights[bottom_indices[d]],w)
        delta_values=[(delta[top_source[a]]*(1-u)+delta[top_source[b]]*u)*(1-t) for _,delta in keys]
        ring.append(append(point,tex,blend_weight(top_w,bottom_w,t),delta_values))
    rings.append(ring)
bottom_ring=[]
for i in bottom_indices:bottom_ring.append(append(old[i],[.5,.15],weights[i],[np.zeros(3) for _ in keys]))
rings.append(bottom_ring)
vertices=np.array(vertices);uv=np.array(uv)

bridge=[]
for upper,lower in zip(rings,rings[1:]):
    ta=angle(vertices[upper]);ba=angle(vertices[lower]);a=b=0
    while a<len(upper) or b<len(lower):
        ai=a%len(upper);bi=b%len(lower)
        na=ta[(a+1)%len(upper)]+(2*np.pi if a+1>=len(upper) else 0)
        nb=ba[(b+1)%len(lower)]+(2*np.pi if b+1>=len(lower) else 0)
        if a<len(upper) and (b==len(lower) or na<nb):tri=[upper[ai],upper[(ai+1)%len(upper)],lower[bi]];a+=1
        else:tri=[upper[ai],lower[(bi+1)%len(lower)],lower[bi]];b+=1
        pts=vertices[tri];normal=np.cross(pts[1]-pts[0],pts[2]-pts[0]);radial=pts.mean(0)-[0,.025,pts[:,2].mean()]
        if np.dot(normal,radial)<0:tri.reverse()
        bridge.append(tri)
triangles[2]=np.array(bridge)
# 원본 Suiha는 헤어 아래 뒤통수가 열려 있다. 그 경계를 공유하는 연속 두피를 만든다.
back=next(p for p in scope['boundaries'](natural,original_triangles[0]) if len(p)==36 and natural[p,2].max()>1.49)
back=back[np.argsort(np.arctan2(natural[back,0],natural[back,2]-1.465))]
vertices=vertices.tolist();uv=uv.tolist()
back_rings=[[append(natural[i],[.5,.15],weights[i],[delta[i] for _,delta in keys]) for i in back]]
for t in [.25,.5,.75]:
    ring=[]
    for i in back:
        point=natural[i]*(1-t)+np.array([0,.098,1.463])*t
        point[1]+=.010*np.sin(np.pi*t)
        tex=np.array([.5,.15])
        ring.append(append(point,tex,weights[i],[delta[i]*(1-t) for _,delta in keys]))
    back_rings.append(ring)
pole=append([0,.098,1.463],[.5,.15],weights[int(back[0])],[np.zeros(3) for _ in keys])
cap=[]
for a,b in zip(back_rings,back_rings[1:]):
    for j in range(len(back)):
        k=(j+1)%len(back);cap.extend([[a[j],b[j],a[k]],[a[k],b[j],b[k]]])
for j in range(len(back)):cap.append([back_rings[-1][j],pole,back_rings[-1][(j+1)%len(back)]])
vertices=np.array(vertices);uv=np.array(uv)
for tri in cap:
    pts=vertices[tri];normal=np.cross(pts[1]-pts[0],pts[2]-pts[0]);radial=pts.mean(0)-[0,.025,1.463]
    if np.dot(normal,radial)<0:tri.reverse()
triangles[2]=np.concatenate([triangles[2],np.array(cap)])
# 몸체 경계는 마지막 16정점이라는 기존 검사 계약을 보존한다.
order=np.array([i for i in range(len(vertices)) if i not in bottom_ring]+bottom_ring)
remap=np.zeros(len(vertices),dtype=int);remap[order]=np.arange(len(vertices))
vertices=vertices[order];uv=uv[order];ws=[ws[i] for i in order];frame_values=[d[order] for d in frame_values]
triangles=[remap[t] for t in triangles]
assert len(vertices)==len(ws)==len(frame_values[0]) and np.isfinite(vertices).all()
with (ROOT/'repaired_face.bin').open('wb') as f:
    f.write(struct.pack('<iii',len(vertices),3,len(keys)));f.write(vertices.astype('<f4').tobytes());f.write(uv.astype('<f4').tobytes())
    for tri in triangles:f.write(struct.pack('<i',tri.size));f.write(tri.astype('<i4').tobytes())
    for (name,_),delta in zip(keys,frame_values):write_string(f,name);f.write(delta.astype('<f4').tobytes())
    for weight in ws:f.write(struct.pack('<4i4f',*weight))
    f.write(struct.pack('<i',len(names)))
    for name in names:write_string(f,name)

bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
project=next(p for p in ROOT.parents if (p/'Assets').is_dir())
def material(name,path):
    mat=bpy.data.materials.new(name);mat.use_nodes=True
    shader=mat.node_tree.nodes.get('Principled BSDF');shader.inputs['Roughness'].default_value=.8
    if path and (project/path).is_file():
        tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(project/path),check_existing=True);tex.image.pack()
        mat.node_tree.links.new(tex.outputs['Color'],shader.inputs['Base Color'])
    return mat
def make_object(name,v,tex,tris):
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(v.tolist(),[],np.concatenate(tris).tolist());mesh.update();obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
    layer=mesh.uv_layers.new(name='UVMap')
    for loop in mesh.loops:layer.data[loop.index].uv=tex[loop.vertex_index]
    for face in mesh.polygons:face.use_smooth=True
    return obj
face=make_object('Siuha_R02_Head_Neck',vertices,uv,triangles);face.shape_key_add(name='Basis')
face.data.materials.append(material('Siuha_Face_Albedo','Assets/08.Models/Characters/SiuhaP09/Siuha_Face_Albedo.png'))
for (name,_),delta in zip(keys,frame_values):face.shape_key_add(name=name).data.foreach_set('co',(vertices+delta).ravel())
for name in names:face.vertex_groups.new(name=name)
for i,weight in enumerate(ws):
    for bone,value in zip(weight[:4],weight[4:]):
        if value>0:face.vertex_groups[bone].add([i],value,'REPLACE')

front=(source[:,2]<1.44)&(source[:,1]<-.02)
head_surface=BVHTree.FromPolygons([Vector(p) for p in vertices],np.concatenate([triangles[0],triangles[2]]).tolist(),all_triangles=True)
report={'crown_before':float(old[:count,2].max()),'crown_after':float(natural[:,2].max()),'neck_rings':[len(r) for r in rings],'neck_triangles':len(bridge),'back_cap_triangles':len(cap),'face_vertices':len(vertices),'face_front_max_error':float(np.linalg.norm(vertices[:count][front]-old[:count][front],axis=1).max())}
for name in ['Hair_03','Hair_11','SkinHead_Female']:
    v,tex,tris,weight=read_part(name);original=v.copy()
    # 두상 높이와 앞머리 시작점에 맞춰 헤어의 뿌리·가닥을 연속적으로 변형한다.
    if name.startswith('Hair'):
        v[:,2]-=.031+.14*np.maximum(v[:,2]-1.445,0)
        v[:,0]*=.94
        v[:,1]=.025+(v[:,1]-.025)*.94
        if name=='Hair_11':
            # 짧아진 두상에서 앞머리만 눈을 덮지 않도록 가닥의 끝을 다시 조형한다.
            mask=(1-smooth(-.025,.015,v[:,1]))*(1-smooth(1.435,1.49,v[:,2]))*smooth(1.30,1.365,v[:,2])
            v[:,2]+=.038*mask
            # 뒤통수를 닫은 면과 단발의 긴 가닥 면 사이에도 여유를 둔다.
            v[:,1]+=.014*smooth(.035,.07,v[:,1])*smooth(1.36,1.43,v[:,2])
        center=Vector((0,.025,1.443))
        for i,p in enumerate(v):
            if p[2]<1.453 and not (p[1]>.025 and p[2]>1.425):continue
            direction=(Vector(p)-center).normalized();hit=head_surface.ray_cast(center,direction)
            if hit[0] is None:continue
            required=(hit[0]-center).length+.006
            actual=(Vector(p)-center).length
            if actual<required:v[i]=np.array(center+direction*required)
        # 긴 가닥 면의 중간도 두피 밖에 있도록 검사한다. 정점만 검사하면 면 중앙이 파고들 수 있다.
        crown_faces=[tri for tri in np.concatenate(tris) if v[tri,2].max()>1.425]
        samples=[(a/4,b/4,(4-a-b)/4) for a in range(5) for b in range(5-a)]
        welds={}
        for index,p in enumerate(original):welds.setdefault(tuple(np.round(p,6)),[]).append(index)
        for iteration in range(8):
            shifts=np.zeros_like(v)
            for tri in crown_faces:
                for bary in samples:
                    point=Vector(np.array(bary)@v[tri])
                    if point.z<1.448 and not (point.y>.025 and point.z>1.425):continue
                    direction=(point-center).normalized();hit=head_surface.ray_cast(center,direction)
                    if hit[0] is None:continue
                    missing=(hit[0]-center).length+.004-(point-center).length
                    if missing<=0:continue
                    for j in tri:
                        move=np.array(direction)*missing
                        if np.linalg.norm(move)>np.linalg.norm(shifts[j]):shifts[j]=move
            # UV 분리 정점도 같은 위치를 유지해 가닥 면에 작은 틈이 생기지 않게 한다.
            for group in welds.values():
                if len(group)>1:shifts[group]=shifts[group[np.argmax(np.linalg.norm(shifts[group],axis=1))]]
            v+=shifts
            if np.linalg.norm(shifts,axis=1).max()<.0001:break
    else:
        v[:,2]-=.028+.16*np.maximum(v[:,2]-1.445,0)
        v[:,0]*=.89;v[:,1]=.025+(v[:,1]-.025)*.89
    assert np.isfinite(v).all()
    with (ROOT/(name+'_fitted.bin')).open('wb') as f:f.write(struct.pack('<i',len(v)));f.write(v.astype('<f4').tobytes())
    obj=make_object(name+'_Fitted',v,tex,tris)
    textures=(ROOT/(name+'_textures.txt')).read_text(encoding='utf-8-sig').splitlines()
    for index,path in enumerate(textures):obj.data.materials.append(material(name+'_'+str(index),path))
    offset=0
    for index,ts in enumerate(tris):
        for poly in list(obj.data.polygons)[offset:offset+len(ts)]:poly.material_index=index
        offset+=len(ts)
    bone_names=(ROOT/(name+'_bones.txt')).read_text(encoding='utf-8-sig').splitlines()
    for bone_name in bone_names:obj.vertex_groups.new(name=bone_name)
    for i,w in enumerate(weight):
        for bone,value in zip(w[:4],w[4:]):
            if value>0:obj.vertex_groups[bone].add([i],value,'REPLACE')
    report[name]={'min':v.min(0).tolist(),'max':v.max(0).tolist(),'max_displacement':float(np.linalg.norm(v-original,axis=1).max())}
    before=make_object(name+'_Before',original,tex,tris);before.hide_render=True;before.hide_set(True)
    obj.hide_set(name in ['Hair_11','SkinHead_Female'])
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Siuha_R02_HeadHairNeck.blend'),compress=True)
(ROOT/'repair_report.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print(json.dumps(report))
