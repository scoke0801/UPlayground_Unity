"""노출된 목의 곡면을 양쪽 경계의 접선·스킨·UV에 맞춰 재구성한다."""
import bpy, struct, json
import numpy as np
from pathlib import Path
root=Path('C:/UsingProject/UnityProject/UPlayground')
out=root/'output/HichiBareNeckFix'
def name(f):
    size=shift=0
    while True:
        b=f.read(1)[0];size|=(b&127)<<shift
        if not b&128:break
        shift+=7
    return f.read(size).decode('utf-8')
def array(f,n,k=3,kind='<f4'):return np.frombuffer(f.read(n*k*4),kind).reshape(n,k).copy()
with (out/'Backup/final.bin').open('rb') as f:
    count,slots,shapes=struct.unpack('<iii',f.read(12));old=array(f,count);uv=array(f,count,2)
    triangles=[]
    for s in range(slots):triangles.append(array(f,struct.unpack('<i',f.read(4))[0]//3,3,'<i4'))
    keys=[(name(f),array(f,count)) for s in range(shapes)]
    weights=[struct.unpack('<4i4f',f.read(32)) for i in range(count)]
    bones=[name(f) for i in range(struct.unpack('<i',f.read(4))[0])]
normal=np.fromfile(out/'normals.bin',dtype='<f4').reshape(-1,3)
with (out/'body.bin').open('rb') as f:
    n=struct.unpack('<i',f.read(4))[0];body=array(f,n);bn=array(f,n);bu=array(f,n,2)
    body_tri=array(f,struct.unpack('<i',f.read(4))[0]//3,3,'<i4')
    bw=[struct.unpack('<4i4f',f.read(32)) for i in range(n)]
    body_bones=[name(f) for i in range(struct.unpack('<i',f.read(4))[0])]
assert body_bones==bones
top=np.array([0,2,8,33,44,52,910,1009,1019,1023,1133,1127,1113,1105,1057,1052,1033,1035,1043,112,111,17])
native=json.loads((root/'ArtSource/ExternalCharacterBuilder/HichiFace/source.json').read_text(encoding='utf-8-sig'))
boundary=np.array(native['nativeVertices'])[[4,6,9,11,14,15,18,21,23,51,47,44,39,35,32,29]]
bottom=np.array([np.linalg.norm(body-p,axis=1).argmin() for p in boundary])
def angles(points):
    center=points.mean(0);return np.arctan2(points[:,1]-center[1],points[:,0]-center[0])
top=top[np.argsort(angles(old[top]))];ta=angles(old[top]);bottom=bottom[np.argsort(angles(body[bottom]))];ba=angles(body[bottom])
def blend_weight(a,b,t):
    sums={}
    for w,s in [(a,1-t),(b,t)]:
        for i,v in zip(w[:4],w[4:]):sums[i]=sums.get(i,0)+v*s
    entries=sorted([(i,v) for i,v in sums.items() if v>1e-7],key=lambda p:-p[1])
    assert len(entries)<=4
    entries+= [(0,0)]*(4-len(entries));total=sum(v for i,v in entries)
    return tuple(i for i,v in entries)+tuple(v/total for i,v in entries)
def sample(angle):
    for j in range(len(bottom)):
        start=ba[j];end=ba[(j+1)%len(bottom)]+(2*np.pi if j==len(bottom)-1 else 0)
        a=angle+(2*np.pi if angle<ba[0] else 0)
        if start<=a<=end:return bottom[j],bottom[(j+1)%len(bottom)],float((a-start)/(end-start))
    raise ValueError(angle)
def tangent(n,height):
    v=np.array([0.,0.,1.])-n*n[2]
    v/=max(v[2],.25);v[:2]=np.clip(v[:2],-1.0,1.0)
    return v*height
v=old[:9774].tolist();ns=normal[:9774].tolist();u=uv[:9774].tolist();u2=np.zeros((9774,2)).tolist();w=weights[:9774];origin=list(range(9774));factor=[1.]*9774
for i in bottom:
    v.append(body[i].tolist());ns.append(bn[i].tolist());u.append([.5,.04]);u2.append(bu[i].tolist());w.append(bw[i]);origin.append(-1);factor.append(0.)
for t in [.25,.5,.75,1.]:
    for i,a in zip(top,ta):
        b,c,f=sample(a);low=body[b]*(1-f)+body[c]*f;low_n=bn[b]*(1-f)+bn[c]*f;low_n/=np.linalg.norm(low_n)
        height=old[i,2]-low[2]
        m0=tangent(low_n,height);m1=tangent(normal[i],height)
        p=(2*t**3-3*t**2+1)*low+(t**3-2*t**2+t)*m0+(-2*t**3+3*t**2)*old[i]+(t**3-t**2)*m1
        n=low_n*(1-t)+normal[i]*t;n/=np.linalg.norm(n)
        v.append(p.tolist());ns.append(n.tolist());u.append([float(uv[i,0]),float(.04*(1-t)+uv[i,1]*t)]);u2.append((bu[b]*(1-f)+bu[c]*f).tolist());w.append(blend_weight(blend_weight(bw[b],bw[c],f),weights[i],t));origin.append(int(i));factor.append(t)
v=np.array(v,dtype='<f4');ns=np.array(ns,dtype='<f4');u=np.array(u,dtype='<f4');u2=np.array(u2,dtype='<f4');neck=[]
def tri(a,b,c):
    center=(v[a]+v[b]+v[c])/3;outward=np.array([center[0],center[1]-.02,0])
    if np.dot(np.cross(v[b]-v[a],v[c]-v[a]),outward)<0:b,c=c,b
    neck.append([a,b,c])
a=b=0
while a<16 or b<22:
    ab=ba[(a+1)%16]+(2*np.pi if a+1>=16 else 0);at=ta[(b+1)%22]+(2*np.pi if b+1>=22 else 0)
    if a<16 and (b>=22 or ab<at):tri(9774+a%16,9774+(a+1)%16,9790+b%22);a+=1
    else:tri(9774+a%16,9790+b%22,9790+(b+1)%22);b+=1
for ring in range(3):
    lo=9790+ring*22;hi=lo+22
    for j in range(22):tri(lo+j,hi+j,hi+(j+1)%22);tri(lo+j,hi+(j+1)%22,lo+(j+1)%22)
triangles[2]=np.array(neck,dtype='<i4')
assert len(v)==9878 and np.isfinite(v).all()
scene=bpy.data.scenes.new('Hichi_BareNeck_r03');mesh=bpy.data.meshes.new(scene.name);mesh.from_pydata(v.tolist(),[],np.concatenate(triangles)[:,::-1].tolist());mesh.update();obj=bpy.data.objects.new(mesh.name,mesh);scene.collection.objects.link(obj);obj.shape_key_add(name='Basis')
with (out/'bare_neck.bin').open('wb') as f:
    f.write(struct.pack('<iii',len(v),len(neck)*3,len(keys)));f.write(v.tobytes());f.write(ns.tobytes());f.write(u.tobytes());f.write(u2.tobytes())
    for entry in w:f.write(struct.pack('<4i4f',*entry))
    f.write(triangles[2].tobytes())
    for key,delta in keys:
        d=np.array([delta[i]*t if i>=0 else np.zeros(3) for i,t in zip(origin,factor)],dtype='<f4');f.write(d.tobytes());obj.shape_key_add(name=key).data.foreach_set('co',(v+d).ravel())
(out/'bridge_map.json').write_text(json.dumps({'top':top.tolist(),'bottom_body_indices':bottom.tolist(),'origins':origin,'factors':factor}),encoding='utf-8')
print(json.dumps({'vertices':len(v),'neck_triangles':len(neck),'rings':5,'shape_count':len(keys)}))
