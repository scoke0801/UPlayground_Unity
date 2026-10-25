from collections import Counter, defaultdict
"""Unity 정점 순서를 보존하여 Nenmir 기반 새 얼굴과 모든 표정을 조형한다."""
import bpy
import json
import struct
import numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT = Path(__file__).resolve().parent

def read_string(stream):
    size = shift = 0
    while True:
        byte = stream.read(1)[0]
        size |= (byte & 127) << shift
        if not byte & 128:
            break
        shift += 7
    return stream.read(size).decode('utf-8')

with (ROOT / 'source.bin').open('rb') as stream:
    def integer():
        return struct.unpack('<i', stream.read(4))[0]
    def array(count, width=3, dtype='<f4'):
        return np.frombuffer(stream.read(count * width * 4), dtype=dtype).copy().reshape(count, width)
    count, slots, shape_count = integer(), integer(), integer()
    source = array(count)
    uv = array(count, 2)
    triangles = [array(integer() // 3, 3, '<i4') for _ in range(slots)]
    shapes = [(read_string(stream), array(count)) for _ in range(shape_count)]
    native = array(integer())
    native_triangles = array(integer() // 3, 3, '<i4')

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



settings=json.loads((ROOT/'parameters.json').read_text())
def smooth(a,b,value):
 t=np.clip((value-a)/(b-a),0,1); return t*t*(3-2*t)
def sculpt(points):
 result=points.copy(); x,y,z=points.T
 front=1-smooth(-.030,.005,y)
 eye=(1-smooth(.027,.042,np.abs(np.abs(x)-settings['eye_center_x'])))*(1-smooth(.020,.036,np.abs(z-settings['eye_center_z'])))*front
 result[:,0]+=(x-np.sign(x)*settings['eye_center_x'])*(settings['eye_width_scale']-1)*eye
 result[:,2]+=(z-settings['eye_center_z'])*(settings['eye_height_scale']-1)*eye
 brow=np.exp(-((z-1.443)/.008)**2)*front
 result[:,2]-=settings['brow_down_m']*brow
 result[:,0]*=1-.04*np.exp(-((z-1.385)/.016)**2)*front
 cheek=smooth(1.332,1.355,z)*(1-smooth(1.380,1.399,z))*(1-smooth(-.025,.005,y))*smooth(.016,.040,np.abs(x))
 result[:,0]*=1+(settings['cheek_width_scale']-.96)*cheek
 result[:,1]+=settings['cheek_depth_reduction_m']*cheek
 jaw=np.exp(-((z-1.339)/.012)**2)*front
 result[:,0]+=settings['jaw_width_add_m']*np.tanh(x/.014)*jaw
 result[:,2]+=settings['jaw_up_m']*jaw
 nose=np.exp(-(x/.011)**2-((z-1.387)/.012)**2)*front
 result[:,1]-=settings['nose_forward_m']*nose
 mouth=np.exp(-(x/.025)**4-((z-1.365)/.009)**4)*front
 result[:,0]+=(settings['mouth_width_scale']-1)*x*mouth
 # 축소한 아래 얼굴과 눈·눈썹·입안의 위치를 같은 연속 변형으로 맞춘다.
 features=smooth(1.382,1.399,z)*(1-smooth(1.448,1.465,z))*front
 result[:,0]-=settings['eye_center_inset_m']*np.tanh(x/.012)*features
 result[:,2]-=settings['eye_center_down_m']*features
 result[:,2]-=settings['nose_down_m']*nose
 mouth_shift=np.exp(-(x/.030)**4-((z-1.365)/.012)**4)*(1-smooth(0,.015,y))
 result[:,2]+=settings['mouth_up_m']*mouth_shift
 # 볼만 좁아져 윤곽이 꺾이지 않도록 눈 주변부터 턱까지 폭을 연속적으로 조정한다.
 face_width=smooth(1.330,1.360,z)*(1-smooth(1.445,1.470,z))*front
 result[:,0]*=1+(settings['face_width_scale']-1)*face_width
 # 뒤통수의 깊이를 P09 캡 안으로 연속적으로 맞추고 얼굴 앞면은 보존한다.
 result[:,1]-=.035*smooth(.015,.105,y)
 result[:,0]*=1-.12*smooth(1.448,1.51,z)
 result[:,0]-=np.sign(x)*.014*smooth(.065,.09,np.abs(x))*smooth(-.04,0,y)
 return result

def angles(vertices):
 return np.mod(np.arctan2(vertices[:,0],.024-vertices[:,1]),2*np.pi)
source_loop=next(loop for loop in boundary_loops(source,triangles[0]) if len(loop)>10 and source[loop,2].max()<1.362 and source[loop,1].max()>.04)
native_loop=next(loop for loop in boundary_loops(native,native_triangles) if native[loop,2].max()<1.35)
source_loop=source_loop[np.argsort(angles(source[source_loop]))]
native_loop=native_loop[np.argsort(angles(native[native_loop]))]
assert len(source_loop)==20 and len(native_loop)==16
shaped=sculpt(source); offsets=np.zeros_like(source)
def sample_ring(ring,theta):
 a=angles(ring);hi=int(np.searchsorted(a,theta))%len(ring);lo=(hi-1)%len(ring)
 p,q=ring[lo],ring[hi];direction=np.array([np.sin(theta),-np.cos(theta)])
 edge=q[:2]-p[:2];off=p[:2]-np.array([0,.024]);cross=lambda a,b:a[0]*b[1]-a[1]*b[0]
 t=np.clip(-cross(off,direction)/cross(edge,direction),0,1);return p+(q-p)*t
# 목 보정은 입안·표정 장식이 아닌 연결된 바깥 피부에만 적용한다.
_, welded=np.unique(np.round(source,6),axis=0,return_inverse=True)
links=defaultdict(set)
for tri in triangles[0]:
 a,b,c=(int(welded[i]) for i in tri)
 links[a].update((b,c));links[b].update((a,c));links[c].update((a,b))
skin_nodes=set();pending=[int(welded[source_loop[0]])]
while pending:
 node=pending.pop()
 if node in skin_nodes:continue
 skin_nodes.add(node);pending.extend(links[node]-skin_nodes)
skin_indices=np.array([i for i,node in enumerate(welded) if int(node) in skin_nodes])
ring=shaped[source_loop]
for i in skin_indices:
 p=shaped[i];distance=float('inf')
 for a,b in zip(ring,np.roll(ring,-1,axis=0)):
  edge=b-a;t=np.clip(np.dot(p-a,edge)/np.dot(edge,edge),0,1)
  distance=min(distance,float(np.linalg.norm(p-a-edge*t)))
 amount=1-float(smooth(0,settings['neck_transition_radius_m'],distance))
 if amount<=0:continue
 theta=angles(p[None])[0];bottom=sample_ring(ring,theta)
 target=sample_ring(native[native_loop],theta)+np.array([0,0,settings['neck_bridge_height_m']])
 offsets[i]=(target-bottom)*amount
shaped+=offsets
outer_faces=np.array([t for t in triangles[0] if int(welded[t[0]]) in skin_nodes])
before_surface=source[outer_faces];after_surface=shaped[outer_faces]
before_normals=np.cross(before_surface[:,1]-before_surface[:,0],before_surface[:,2]-before_surface[:,0])
after_normals=np.cross(after_surface[:,1]-after_surface[:,0],after_surface[:,2]-after_surface[:,0])
assert np.all(np.sum(before_normals*after_normals,axis=1)>=0), '바깥 피부 삼각형이 뒤집혔습니다.'
bridge=[];a=b=0;top=source_loop;bottom=native_loop;ta=angles(shaped[top]);ba=angles(native[bottom])
while a<len(top) or b<len(bottom):
 ai=a%len(top);bi=b%len(bottom)
 next_a=ta[(a+1)%len(top)]+(2*np.pi if a+1>=len(top) else 0)
 next_b=ba[(b+1)%len(bottom)]+(2*np.pi if b+1>=len(bottom) else 0)
 if a<len(top) and (b==len(bottom) or next_a<next_b):
  tri=[int(top[ai]),int(top[(ai+1)%len(top)]),-int(bottom[bi])-1];a+=1
 else:
  tri=[int(top[ai]),-int(bottom[(bi+1)%len(bottom)])-1,-int(bottom[bi])-1];b+=1
 pts=np.array([shaped[i] if i>=0 else native[-i-1] for i in tri]);normal=np.cross(pts[1]-pts[0],pts[2]-pts[0])
 if np.dot(normal,pts.mean(0)-np.array([0,.024,pts[:,2].mean()]))<0:tri.reverse()
 bridge.extend(tri)
with (ROOT/'repair_topology.bin').open('wb') as stream:
 stream.write(struct.pack('<i',len(native_loop)));stream.write(native_loop.astype('<i4').tobytes())
 stream.write(struct.pack('<i',len(bridge)));stream.write(np.array(bridge,dtype='<i4').tobytes());stream.write(struct.pack('<i',0))
with (ROOT/'repair_sculpt.bin').open('wb') as stream:
 stream.write(struct.pack('<ii',len(source),len(shapes)));stream.write(shaped.astype('<f4').tobytes())
 for _,delta in shapes:
  value=sculpt(source+delta)-sculpt(source);assert np.isfinite(value).all();stream.write(value.astype('<f4').tobytes())
assert np.isfinite(shaped).all()
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
for name,points in [('Nenmir_SourceAligned',source),('Nenmir_P09_NewFace',shaped)]:
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(points.tolist(),[],[t for slot in triangles for t in slot.tolist()]);mesh.update()
 obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
 layer=mesh.uv_layers.new(name='UVMap')
 for loop in mesh.loops:layer.data[loop.index].uv=uv[loop.vertex_index]
 for poly in mesh.polygons:poly.use_smooth=True
 obj.shape_key_add(name='Basis')
 for key,delta in shapes:obj.shape_key_add(name=key).data.foreach_set('co',(source+delta if name.endswith('Aligned') else sculpt(source+delta)+offsets).ravel())
 obj.hide_render=name.endswith('Aligned');obj.hide_set(name.endswith('Aligned'))
 bpy.context.view_layer.objects.active=obj
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Nenmir_P09_Face.blend'),compress=True)
report={'source_vertices':count,'source_shapes':shape_count,'neck_rings':[len(source_loop),len(native_loop)],'bridge_triangles':len(bridge)//3,'parameters':settings,'moved_vertices':int((np.linalg.norm(shaped-source,axis=1)>1e-5).sum())}
(ROOT/'sculpt_report.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))


