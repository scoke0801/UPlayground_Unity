"""LianLian 얼굴과 표정을 조형하고 실제 P09 목 경계에 연결한다."""
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from inspect_topology import *
import bpy



def smooth(a,b,v):
    t=np.clip((v-a)/(b-a),0,1)
    return t*t*(3-2*t)

def sculpt(points):
    result=points.copy();x,y,z=points.T
    front=1-smooth(-.025,.005,y)
    eye=(1-smooth(.022,.039,np.abs(np.abs(x)-.033)))*(1-smooth(.022,.035,np.abs(z-1.407)))*front
    result[:,0]-=np.sign(x)*(np.abs(x)-.033)*.10*eye
    result[:,2]-=(z-1.407)*.22*eye
    brow=np.exp(-((z-1.44)/.008)**2)*front
    result[:,2]-=.002*brow
    nose=np.exp(-(x/.010)**2-((z-1.384)/.012)**2)*front
    result[:,1]-=.0025*nose
    mouth=np.exp(-(x/.025)**4-((z-1.365)/.009)**4)*front
    result[:,0]+=.06*x*mouth
    cheek=np.exp(-((z-1.38)/.017)**2)*front
    result[:,0]*=1-.025*cheek
    jaw=np.exp(-((z-1.345)/.011)**2)*front
    result[:,0]+=.0015*np.tanh(x/.015)*jaw
    result[:,2]-=.0008*jaw
    return result

shaped=sculpt(source)
# 헤어 피팅을 위해 두피를 변형하지 않는다. 부착 위치는 얼굴 프로파일에서 조정한다.
offsets=np.zeros_like(source);misses=[]

# 머리 아래를 막은 원본 부채꼴 덮개만 제거한다. 턱 아래와 귀 뒤는 보존한다.
cap_center=np.where(np.linalg.norm(source-np.array([0,.010289,1.359824]),axis=1)<.000002)[0]
cap=np.isin(triangles[0],cap_center).any(axis=1)
assert cap.sum()==26
faces=triangles[0][~cap]
top=next(a for a in boundary_loops(source,faces) if len(a)==26 and source[a,2].max()<1.38)
bottom=next(a for a in boundary_loops(native,native_triangles) if native[a,2].max()<1.35)
def angles(points): return np.mod(np.arctan2(points[:,0],.020-points[:,1]),2*np.pi)
top=top[np.argsort(angles(shaped[top]))];bottom=bottom[np.argsort(angles(native[bottom]))]
ta,ba=angles(shaped[top]),angles(native[bottom]);bridge=[];a=b=0
while a<len(top) or b<len(bottom):
    ai=a%len(top);bi=b%len(bottom)
    na=ta[(a+1)%len(top)]+(2*np.pi if a+1>=len(top) else 0)
    nb=ba[(b+1)%len(bottom)]+(2*np.pi if b+1>=len(bottom) else 0)
    if a<len(top) and (b==len(bottom) or na<nb):
        tri=[int(top[ai]),int(top[(ai+1)%len(top)]),-int(bottom[bi])-1];a+=1
    else:
        tri=[int(top[ai]),-int(bottom[(bi+1)%len(bottom)])-1,-int(bottom[bi])-1];b+=1
    points=np.array([shaped[i] if i>=0 else native[-i-1] for i in tri]);normal=np.cross(points[1]-points[0],points[2]-points[0]);radial=points.mean(0)-np.array([0,.020,points[:,2].mean()])
    if np.dot(normal,radial)<0:tri.reverse()
    bridge.extend(tri)
assert len(bottom)==16 and len(bridge)==126
with (ROOT/'sculpt.bin').open('wb') as f:
    f.write(struct.pack('<ii',count,len(shapes)));f.write(shaped.astype('<f4').tobytes())
    for name,delta in shapes:
        values=sculpt(source+delta)-sculpt(source)
        assert np.isfinite(values).all()
        f.write(values.astype('<f4').tobytes())
with (ROOT/'topology.bin').open('wb') as f:
    for values in [faces.ravel(),bottom,np.array(bridge)]:
        f.write(struct.pack('<i',len(values)));f.write(values.astype('<i4').tobytes())

# 이 스크립트는 별도 background Blender 프로세스에서만 실행한다.
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
image=bpy.data.images.load(str(ROOT/'LianLian_FaceAlbedo.png'));image.pack()
material=bpy.data.materials.new('LianLian_새얼굴_알베도');material.use_nodes=True
node=material.node_tree.nodes.new('ShaderNodeTexImage');node.image=image
material.node_tree.links.new(node.outputs['Color'],material.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
for name,points in [('LianLian_SourceAligned',source),('LianLian_P09_Sculpt',shaped)]:
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(points.tolist(),[],faces.tolist());mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj);mesh.materials.append(material)
    layer=mesh.uv_layers.new(name='UVMap')
    for loop in mesh.loops: layer.data[loop.index].uv=uv[loop.vertex_index]
    for polygon in mesh.polygons:polygon.use_smooth=True
    obj.shape_key_add(name='Basis')
    for key,delta in shapes:
        coords=source+delta if name.endswith('Aligned') else sculpt(source+delta)+offsets
        obj.shape_key_add(name=key).data.foreach_set('co',coords.ravel())
    if name.endswith('Aligned'):obj.hide_set(True);obj.hide_render=True
    else:bpy.context.view_layer.objects.active=obj;obj.select_set(True)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'LianLian_P09_Face.blend'),compress=True)
report={'source_vertices':count,'source_shapes':len(shapes),'eye_width_scale':.90,'eye_height_scale':.78,'sculpt_max_m':float(np.linalg.norm(sculpt(source)-source,axis=1).max()),'scalp_fitted':int(np.count_nonzero(np.linalg.norm(offsets,axis=1)>0)),'scalp_misses':misses,'neck_cap_removed_triangles':int(cap.sum()),'neck_bridge_triangles':len(bridge)//3,'source_ring':top.tolist(),'native_ring':bottom.tolist()}
(ROOT/'sculpt_report.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print(json.dumps(report))

