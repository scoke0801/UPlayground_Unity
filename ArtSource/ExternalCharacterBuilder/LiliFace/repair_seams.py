"""Lili의 두피를 유지하고 턱·귀 뒤·목을 P09 목 경계에 연결한다."""
from pathlib import Path
import struct
from collections import Counter, defaultdict
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT = Path(__file__).resolve().parent
# 얼굴 조형식과 원본 표정은 최초 제작과 같은 단일 소스를 사용한다.
exec((ROOT/'sculpt_face.py').read_text(encoding='utf-8').split('shaped = sculpt(source)')[0])

def boundary_loops(vertices, faces):
    _, first, inverse = np.unique(np.round(vertices, 6), axis=0, return_index=True, return_inverse=True)
    edges = Counter(tuple(sorted((int(inverse[a]), int(inverse[b]))))
                    for tri in faces for a, b in zip(tri, np.roll(tri, -1)))
    links = defaultdict(set)
    for (a, b), count in edges.items():
        if count == 1:
            links[a].add(b); links[b].add(a)
    seen = set(); loops = []
    for start in links:
        if start in seen:
            continue
        todo = [start]; loop = []
        while todo:
            vertex = todo.pop()
            if vertex in seen:
                continue
            seen.add(vertex); loop.append(first[vertex]); todo.extend(links[vertex]-seen)
        loops.append(np.array(loop))
    return loops

def angles(vertices):
    return np.mod(np.arctan2(vertices[:, 0], .028-vertices[:, 1]), 2*np.pi)

source_loop = next(loop for loop in boundary_loops(source, triangles[2]) if source[loop, 2].max() < 1.33)
native_loop = next(loop for loop in boundary_loops(native, native_triangles) if native[loop, 2].max() < 1.35)
source_loop = source_loop[np.argsort(angles(source[source_loop]))]
native_loop = native_loop[np.argsort(angles(native[native_loop]))]
assert len(source_loop) == 24 and len(native_loop) == 16
shaped = sculpt(source)
source_angles = angles(shaped[source_loop]); native_angles = angles(native[native_loop])

def sample_ring(ring, theta):
    a = angles(ring)
    # 중심을 지나는 광선과 다각형 변의 교점으로 연결면을 정확히 맞춘다.
    hi = int(np.searchsorted(a, theta)) % len(ring); lo = (hi-1) % len(ring)
    p, q = ring[lo], ring[hi]
    direction = np.array([np.sin(theta), -np.cos(theta)])
    edge = q[:2]-p[:2]; offset = p[:2]-np.array([0, .028])
    cross = lambda x,y: x[0]*y[1]-x[1]*y[0]
    t = np.clip(-cross(offset,direction)/cross(edge,direction),0,1)
    return p+(q-p)*t

removed = np.array([int(i) for i in (ROOT/'source_neck_indices.txt').read_text().split(',')])
neck_points = {tuple(p) for p in np.round(source[removed],6)}
neck_indices = np.array([i for i,p in enumerate(np.round(source,6)) if tuple(p) in neck_points])
offsets = np.zeros_like(source)
for i in neck_indices:
    p = shaped[i]; theta = angles(p[None])[0]
    bottom = sample_ring(shaped[source_loop], theta)
    target = sample_ring(native[native_loop], theta) + np.array([0,0,.0015])
    # 원본 목 위쪽은 턱·귀·뒤통수와 이어져 있으므로 고정하고 아래쪽만 압축한다.
    front = (1-np.cos(theta))*.5
    top = 1.33594 + .048*front
    amount = np.clip((top-p[2])/max(.001,top-bottom[2]),0,1)
    offsets[i] = (target-bottom)*amount

# 짧아진 목의 아래쪽을 실제 P09 목 곡면에 얹어 접합부가 돌출되지 않게 한다.
neck_faces=np.array([t for t in native_triangles if (native[t,2]<1.365).all() and (native[t,1]>-.025).all()])
neck_surface=BVHTree.FromPolygons([Vector(v) for v in native],neck_faces.tolist(),all_triangles=True)
transfers={}
for i in neck_indices:
    p=shaped[i]+offsets[i]
    mask=float(1-smooth(1.345,1.37,p[2]))
    if mask<=0: continue
    origin=Vector((0,.025,float(p[2])))
    direction=Vector((float(p[0]),float(p[1]-.025),0)).normalized()
    hit=neck_surface.ray_cast(origin,direction)
    if hit[0] is None: continue
    offsets[i]+=(np.array(hit[0])-p)*mask
    tri=neck_faces[hit[2]];a,b,c=native[tri];q=np.array(hit[0])
    e0=b-a;e1=c-a;rel=q-a
    d00=np.dot(e0,e0);d01=np.dot(e0,e1);d11=np.dot(e1,e1);den=d00*d11-d01*d01
    v=(d11*np.dot(rel,e0)-d01*np.dot(rel,e1))/den
    w=(d00*np.dot(rel,e1)-d01*np.dot(rel,e0))/den
    transfers[i]=(tri,np.array([1-v-w,v,w]),mask)

# 두피는 조형 원본 그대로 둔다. 헤어 맞춤은 P09 얼굴 프로필의 부착 위치로 처리한다.
shaped+=offsets
# 경계의 UV 분리 정점도 같은 위치로 유지한다.
_, first, inverse=np.unique(np.round(source,6),axis=0,return_index=True,return_inverse=True)
skin=set(triangles[2].ravel())
for i in skin: shaped[i]=shaped[first[inverse[i]]]

# 두피 위치 보존과 삼각형 방향을 저장 전에 검사한다.
upper_vertices = sculpt(source)[:, 2] > 1.395
assert np.max(np.linalg.norm(shaped[upper_vertices]-sculpt(source)[upper_vertices],axis=1)) < 1e-6, '두피 위치가 변경되었습니다.'
before=sculpt(source)[triangles[2]];after=shaped[triangles[2]]
upper=before[:,:,2].min(axis=1)>1.435
before_normals=np.cross(before[:,1]-before[:,0],before[:,2]-before[:,0])
after_normals=np.cross(after[:,1]-after[:,0],after[:,2]-after[:,0])
flipped=upper & (np.sum(before_normals*after_normals,axis=1)<=0)
assert not flipped.any(), f'뒤집힌 두피 삼각형: {np.count_nonzero(flipped)}'

# 서로 다른 분할 수의 두 닫힌 경계를 각도 순서로 연결한다.
bridge=[];a=b=0
top=source_loop; bottom=native_loop
ta=angles(shaped[top]);ba=angles(native[bottom])
while a<len(top) or b<len(bottom):
    ai=a%len(top);bi=b%len(bottom)
    next_a=ta[(a+1)%len(top)]+(2*np.pi if a+1>=len(top) else 0)
    next_b=ba[(b+1)%len(bottom)]+(2*np.pi if b+1>=len(bottom) else 0)
    if a<len(top) and (b==len(bottom) or next_a<next_b):
        tri=[int(top[ai]),int(top[(ai+1)%len(top)]),-int(bottom[bi])-1];a+=1
    else:
        tri=[int(top[ai]),-int(bottom[(bi+1)%len(bottom)])-1,-int(bottom[bi])-1];b+=1
    pts=np.array([shaped[i] if i>=0 else native[-i-1] for i in tri])
    normal=np.cross(pts[1]-pts[0],pts[2]-pts[0]);radial=pts.mean(0)-np.array([0,.028,pts[:,2].mean()])
    if np.dot(normal,radial)<0:tri.reverse()
    bridge.extend(tri)

with (ROOT/'repair_sculpt.bin').open('wb') as stream:
    stream.write(struct.pack('<ii',len(source),len(shapes)));stream.write(shaped.astype('<f4').tobytes())
    for _,delta in shapes:
        values=sculpt(source+delta)-sculpt(source)
        assert np.isfinite(values).all()
        stream.write(values.astype('<f4').tobytes())
with (ROOT/'repair_topology.bin').open('wb') as stream:
    stream.write(struct.pack('<i',len(native_loop)));stream.write(native_loop.astype('<i4').tobytes())
    stream.write(struct.pack('<i',len(bridge)));stream.write(np.array(bridge,dtype='<i4').tobytes())
    stream.write(struct.pack('<i',len(transfers)))
    for i,(tri,bary,mask) in transfers.items():
        stream.write(struct.pack('<i3i4f',i,*tri,*bary,mask))
assert np.isfinite(shaped).all()
print('목 복원:',len(removed),'목 연결 경계:',len(source_loop),len(native_loop),'연결 삼각형:',len(bridge)//3)
print('두피 원본 위치 보존:',int(upper_vertices.sum()),'상부 삼각형:',int(upper.sum()),'뒤집힘:',int(flipped.sum()))
