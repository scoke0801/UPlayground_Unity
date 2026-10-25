"""Reine 얼굴과 표정을 조형하고 P09 목 경계에 연결한다. 별도 Blender 프로세스용."""
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from inspect_topology import *
import bpy

parameters = json.loads((ROOT/'parameters.json').read_text(encoding='utf-8-sig'))
def smooth(a,b,v):
    t=np.clip((v-a)/(b-a),0,1)
    return t*t*(3-2*t)

def sculpt(points):
    result=points.copy(); x,y,z=points.T
    front=1-smooth(-.025,.005,y)
    eye=(1-smooth(.025,.038,np.abs(np.abs(x)-.030)))*(1-smooth(.020,.032,np.abs(z-1.409)))*front
    result[:,0]-=np.sign(x)*(np.abs(x)-.030)*(1-parameters['eye_width'])*eye
    result[:,2]-=(z-1.409)*(1-parameters['eye_height'])*eye
    brow=np.exp(-((z-1.426)/.007)**2)*front
    result[:,2]-=.0015*brow
    nose=np.exp(-(x/.012)**2-((z-1.386)/.009)**2)*front
    result[:,1]-=parameters['nose_bridge_m']*nose
    mouth=np.exp(-(x/.023)**4-((z-1.373)/.007)**4)*front
    result[:,0]+=x*parameters['mouth_widen']*mouth
    cheek=np.exp(-((z-1.386)/.016)**2)*front
    result[:,0]*=1+parameters['cheek_width']*cheek
    jaw=np.exp(-((z-1.350)/.012)**2)*front
    result[:,0]+=parameters['jaw_width_m']*np.tanh(x/.015)*jaw
    result[:,2]+=.001*jaw
    result+=dict(shapes)["ear_morph_small"]*parameters["ear_reduction"]
    # 눈과 눈썹을 함께 올리고 이마 길이를 줄인다. 모든 표정에도 같은 좌표 함수를 적용한다.
    result[:,2]+=parameters['eye_raise_m']*smooth(1.388,1.402,z)*(1-smooth(1.435,1.450,z))
    result[:,2]-=(1-parameters['forehead_height'])*np.maximum(z-1.440,0)
    return result

shaped=sculpt(source)
removed=np.array(parameters['removed_overlay_vertices'])
faces=triangles[0][~np.isin(triangles[0],removed).any(axis=1)]
assert len(triangles[0])-len(faces)==14, '코 선과 볼 점 이외의 면을 제거해서는 안 된다.'
top=next(a for a in boundary_loops(source,faces) if len(a)==32 and source[a,2].max()<1.42)
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
    pts=np.array([shaped[i] if i>=0 else native[-i-1] for i in tri])
    if np.dot(np.cross(pts[1]-pts[0],pts[2]-pts[0]),pts.mean(0)-[0,.020,pts[:,2].mean()])<0:tri.reverse()
    bridge.extend(tri)
assert len(bottom)==16 and len(bridge)==144
with (ROOT/'sculpt.bin').open('wb') as f:
    f.write(struct.pack('<ii',count,len(shapes)));f.write(shaped.astype('<f4').tobytes())
    for name,delta in shapes:
        values=sculpt(source+delta)-shaped
        assert np.isfinite(values).all()
        f.write(values.astype('<f4').tobytes())
with (ROOT/'topology.bin').open('wb') as f:
    for values in [faces.ravel(),bottom,np.array(bridge)]:
        f.write(struct.pack('<i',len(values)));f.write(values.astype('<i4').tobytes())

bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
image=bpy.data.images.load(str(ROOT/'Reine_FaceAlbedo.png'));image.pack()
material=bpy.data.materials.new('Reine_새얼굴_알베도');material.use_nodes=True
node=material.node_tree.nodes.new('ShaderNodeTexImage');node.image=image
material.node_tree.links.new(node.outputs['Color'],material.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
for name,points in [('Reine_SourceAligned',source),('Reine_P09_Sculpt',shaped)]:
    all_faces=np.concatenate(triangles[:5] if name.endswith('Aligned') else [faces,*triangles[1:5]])
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(points.tolist(),[],all_faces.tolist());mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj);mesh.materials.append(material)
    layer=mesh.uv_layers.new(name='UVMap')
    for loop in mesh.loops:layer.data[loop.index].uv=uv[loop.vertex_index]
    for polygon in mesh.polygons:polygon.use_smooth=True
    obj.shape_key_add(name='Basis')
    for key,delta in shapes:
        coords=source+delta if name.endswith('Aligned') else sculpt(source+delta)
        obj.shape_key_add(name=key).data.foreach_set('co',coords.ravel())
    if name.endswith('Aligned'):obj.hide_set(True);obj.hide_render=True
    else:bpy.context.view_layer.objects.active=obj;obj.select_set(True)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Reine_P09_Face.blend'),compress=True)
report={'source_vertices':count,'source_shapes':len(shapes),'parameters':parameters,'sculpt_max_m':float(np.linalg.norm(shaped-source,axis=1).max()),'source_ring':top.tolist(),'native_ring':bottom.tolist(),'bridge_triangles':len(bridge)//3,'removed_overlay_triangles':len(triangles[0])-len(faces),'forehead_reshaped':True}
(ROOT/'sculpt_report.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print(json.dumps(report))

