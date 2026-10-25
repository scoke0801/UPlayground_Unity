"""기존 두피 정점을 보존하고 원본의 열린 뒤통수 경계만 연결한다."""
import struct
from pathlib import Path
import numpy as np
import bpy
import sys
sys.path.insert(0,str(Path(__file__).resolve().parent))
from inspect_topology import boundary_loops, read_string

ROOT = Path(__file__).resolve().parent
# 반복 실행해도 같은 R02 기준에서 생성한다.
SOURCE = ROOT / 'head_before_completion.bin'
with SOURCE.open('rb') as f:
    n, slots, shapes = struct.unpack('<iii', f.read(12))
    def points(): return np.frombuffer(f.read(n*12), '<f4').reshape(n,3).copy()
    old = points()
    old_uv = np.frombuffer(f.read(n*8), '<f4').reshape(n,2).copy()
    triangles = []
    for _ in range(slots):
        size = struct.unpack('<i', f.read(4))[0]
        triangles.append(np.frombuffer(f.read(size*4), '<i4').reshape(-1,3).copy())
    keys = [(read_string(f), points()) for _ in range(shapes)]
    weights = [struct.unpack('<4i4f', f.read(32)) for _ in range(n)]
    names = [read_string(f) for _ in range(struct.unpack('<i', f.read(4))[0])]

boundary = next(loop for loop in boundary_loops(old, triangles[0]) if len(loop)==42 and old[loop,2].max()>1.5)
image = bpy.data.images.load(str(ROOT/'LianLian_FaceAlbedo.png'),check_existing=True)
pixels = np.array(image.pixels[:]).reshape(image.size[1],image.size[0],4)
def sample(tex):
    xy=np.clip(np.array(tex)*np.array(image.size),0,np.array(image.size)-1).astype(int)
    return pixels[xy[1],xy[0],:3]
# 아틀라스의 빈 공간을 가로지르지 않도록 이마의 연속된 피부 영역에서 경계 색을 찾는다.
palette=np.array([[.5,v] for v in np.linspace(.43,.65,512)])
colors=np.array([sample(tex) for tex in palette])
boundary_uv={i:palette[np.argmin(np.linalg.norm(colors-sample(old_uv[i]),axis=1))] for i in boundary}
vertices = old.tolist()
uv = old_uv.tolist()
frames = [d.tolist() for _,d in keys]
pole = np.array([0., .102, 1.455])
rings = []
for t in np.linspace(0,1,9)[:-1]:
    ring = []
    for index in boundary:
        point = pole + (old[index]-pole)*np.cos(t*np.pi/2)
        point[1] = old[index,1] + (pole[1]-old[index,1])*np.sin(t*np.pi/2)
        ring.append(len(vertices))
        vertices.append(point.tolist())
        uv.append((boundary_uv[index]*(1-t)+np.array([.5,.57])*t).tolist())
        weights.append(weights[index])
        for values, (_,delta) in zip(frames,keys): values.append((delta[index]*(1-t)).tolist())
    rings.append(ring)
center = len(vertices)
vertices.append(pole.tolist()); uv.append([.5,.57]); weights.append(weights[boundary[0]])
for values in frames: values.append([0.,0.,0.])
cap = []
for a,b in zip(rings,rings[1:]):
    for j in range(len(boundary)):
        k=(j+1)%len(boundary)
        cap.extend([[a[j],b[j],a[k]],[a[k],b[j],b[k]]])
for j in range(len(boundary)): cap.append([rings[-1][j],center,rings[-1][(j+1)%len(boundary)]])
vertices = np.array(vertices,dtype='<f4')
for tri in cap:
    p = vertices[tri]
    if np.dot(np.cross(p[1]-p[0],p[2]-p[0]),p.mean(0)-[0,.025,1.455])<0: tri.reverse()
triangles.append(np.array(cap,dtype='<i4'))
# P09 목의 앞·뒤 UV 섬을 분리한다. 위치가 같아도 서로 다른 UV 경계를 용접하지 않는다.
with (ROOT/'body_neck_uv.bin').open('rb') as f:
    count=struct.unpack('<i',f.read(4))[0]; body_points=[]; body_uv=[]
    for _ in range(count):
        body_points.append(struct.unpack('<3f',f.read(12)))
        options=struct.unpack('<i',f.read(4))[0]
        body_uv.append(np.array([struct.unpack('<2f',f.read(8)) for _ in range(options)]))
body_points=np.array(body_points)
seams=[i for i,options in enumerate(body_uv) if len(options)>1]
assert len(seams)==2
seam_y=float(body_points[seams,1].mean())
vertices=vertices.tolist()
neck_copies={}
used_side={}
for tri in triangles[1]:
    front=old[tri,1].mean()<seam_y
    valid=[i for i in range(count) if bool(body_points[i,1]<seam_y)==front or i in seams]
    edges=[(i,(i+1)%count) for i in valid if (i+1)%count in valid]
    for corner,index in enumerate(tri.copy()):
        key=(int(index),bool(front))
        if key not in neck_copies:
            best=None
            for a,b in edges:
                start=body_points[a,:2]; edge=body_points[b,:2]-start
                amount=np.clip(np.dot(old[index,:2]-start,edge)/np.dot(edge,edge),0,1)
                distance=np.linalg.norm(old[index,:2]-(start+edge*amount))
                choose=lambda i:body_uv[i][np.argmax(body_uv[i][:,1]) if front else np.argmin(body_uv[i][:,1])]
                tex=choose(a)*(1-amount)+choose(b)*amount
                if best is None or distance<best[0]: best=(distance,tex)
            tex=best[1]
            if index not in used_side:
                target=int(index); used_side[index]=front
            else:
                target=len(vertices); vertices.append(old[index].tolist()); uv.append(old_uv[index].tolist()); weights.append(weights[index])
                for values,(_,delta) in zip(frames,keys): values.append(delta[index].tolist())
            uv[target]=tex.tolist(); neck_copies[key]=target
        tri[corner]=neck_copies[key]
vertices=np.array(vertices,dtype='<f4')
# 기존 몸통 경계의 마지막 16정점 계약을 유지한다.
order = np.r_[np.arange(n-16),np.arange(n,len(vertices)),np.arange(n-16,n)]
remap = np.argsort(order)
vertices=vertices[order]; uv=np.array(uv,dtype='<f4')[order]
triangles=[remap[t].astype('<i4') for t in triangles]
frames=[np.array(d,dtype='<f4')[order] for d in frames]
weights=[weights[i] for i in order]
assert np.array_equal(vertices[remap[:n]],old), '기존 얼굴·두피 이동 금지'
assert all(np.array_equal(d[remap[:n]],original) for d,(_,original) in zip(frames,keys))
assert np.isfinite(vertices).all()
assert all(np.ptp(uv[tri,1])<.05 for tri in triangles[1]), '목 UV가 서로 다른 아틀라스 섬을 가로지릅니다.'
assert not any(len(loop)==42 and vertices[loop,2].max()>1.5 for loop in boundary_loops(vertices,np.concatenate(triangles)))

def write_string(f,value):
    data=value.encode('utf-8'); size=len(data)
    while size>127: f.write(bytes([(size&127)|128])); size>>=7
    f.write(bytes([size])); f.write(data)

with (ROOT/'completed_head.bin').open('wb') as f:
    f.write(struct.pack('<iii',len(vertices),len(triangles),len(keys)))
    f.write(vertices.tobytes()); f.write(uv.tobytes())
    for tri in triangles: f.write(struct.pack('<i',tri.size)); f.write(tri.tobytes())
    for (name,_),delta in zip(keys,frames): write_string(f,name); f.write(delta.tobytes())
    for weight in weights: f.write(struct.pack('<4i4f',*weight))
    f.write(struct.pack('<i',len(names)))
    for name in names: write_string(f,name)
print({'vertices':len(vertices),'cap_triangles':len(cap),'existing_vertex_change':0,'existing_shape_change':0})
