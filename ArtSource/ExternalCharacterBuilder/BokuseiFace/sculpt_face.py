"""Bokusei 토폴로지와 표정 델타를 보존해 P09 남성 얼굴을 조형한다."""
exec(compile((__import__('pathlib').Path(__file__).parent/'inspect_source.py').read_text(encoding='utf-8'),__file__,'exec'))
from mathutils import Vector
from mathutils.bvhtree import BVHTree

def smooth(a,b,x):
    t=np.clip((x-a)/(b-a),0,1)
    return t*t*(3-2*t)

def sculpt(points):
    result=points.copy(); x,y,z=points.T
    front=1-smooth(-.025,.025,y)
    eye=np.exp(-((np.abs(x)-.034)/.028)**4-((z-1.414)/.026)**4)*front
    result[:,0]+=np.sign(x)*.0015*eye
    result[:,2]+=(-(z-1.414)*.12+.0015*smooth(.02,.058,np.abs(x)))*eye
    brow=np.exp(-((z-1.441)/.009)**2)*front
    result[:,2]-=.002*brow
    cheek=np.exp(-((z-1.383)/.021)**2)*front
    result[:,0]*=1-.035*cheek
    jaw=np.exp(-((z-1.344)/.015)**2)*front
    result[:,0]+=.004*np.tanh(x/.018)*jaw
    result[:,2]+=.0035*jaw
    nose=np.exp(-(x/.010)**2-((z-1.397)/.016)**2)*front
    result[:,1]-=.003*nose
    mouth=np.exp(-(x/.028)**4-((z-1.361)/.012)**4)*front
    result[:,0]+=.09*x*mouth
    result[:,2]+=.001*mouth
    # 귀가 기존 P09 헤어 바깥으로 과도하게 벌어지지 않도록 붙인다.
    ears=smooth(.073,.097,np.abs(x))
    result[:,0]-=np.sign(x)*.010*ears
    return result

shaped=sculpt(source)
surface=BVHTree.FromPolygons([Vector(v) for v in native],native_triangles.tolist(),all_triangles=True)
offsets=np.zeros_like(source)
# 머리 뒤쪽·상단만 연속적으로 맞춘다. 눈과 눈썹의 새 조형은 보존한다.
for index in np.unique(triangles[0]):
    p=shaped[index]
    scalp=float(smooth(1.465,1.50,p[2]))
    if p[1]>.025:
        scalp=max(scalp,float(smooth(1.36,1.42,p[2])))
    if scalp:
        hit=surface.find_nearest(Vector(p))
        if hit[0] is not None:
            offsets[index]=(np.array(hit[0])-p)*scalp
shaped+=offsets
# 원본의 실제 목 경계 36점을 P09 바디가 만나는 16점 경계에 보간한다.
# 얼굴 아래에 다른 얼굴의 삼각형을 겹쳐 붙이지 않고 연결선을 직접 맞춘다.
source_ring=np.array(json.loads((ROOT/'source_boundaries.json').read_text())[0]['positions'])
native_ring=np.array(json.loads((ROOT/'native_boundaries.json').read_text())[0]['positions'])
source_center=source_ring[:,:2].mean(axis=0);native_center=native_ring[:,:2].mean(axis=0)
angles=np.arctan2(native_ring[:,1]-native_center[1],native_ring[:,0]-native_center[0])
order=np.argsort(angles);angles=angles[order];native_ring=native_ring[order]
ring_ids=np.flatnonzero(np.min(np.linalg.norm(source[:,None,:]-source_ring[None,:,:],axis=2),axis=1)<.00002)
bindings=[];ring_offsets=[]
for index in ring_ids:
    p=source[index];angle=np.arctan2(p[1]-source_center[1],p[0]-source_center[0])
    upper=int(np.searchsorted(angles,angle));lower=(upper-1)%len(angles);upper%=len(angles)
    a=angles[lower];b=angles[upper];theta=angle
    if b<a:b+=2*np.pi
    if theta<a:theta+=2*np.pi
    t=float((theta-a)/(b-a))
    pos=native_ring[lower]*(1-t)+native_ring[upper]*t
    ia=int(np.argmin(np.linalg.norm(native-native_ring[lower],axis=1)))
    ib=int(np.argmin(np.linalg.norm(native-native_ring[upper],axis=1)))
    bindings.append({'index':int(index),'a':ia,'b':ib,'t':t})
    ring_offsets.append(pos-shaped[index])
ring_offsets=np.array(ring_offsets)
for index in np.unique(triangles[0]):
    distances=np.linalg.norm(source[ring_ids]-source[index],axis=1)
    nearest=int(np.argmin(distances));blend=1-float(smooth(0,.030,distances[nearest]))
    offset=ring_offsets[nearest]*blend
    shaped[index]+=offset;offsets[index]+=offset
(ROOT/'neck_bindings.json').write_text(json.dumps(bindings,indent=2))
(ROOT/'neck_bindings.txt').write_text('\n'.join(f"{b['index']},{b['a']},{b['b']},{b['t']}" for b in bindings),encoding='utf-8')
assert np.isfinite(shaped).all()

# 독립 background 프로세스에서만 실행하며 열린 Blender 작업에는 접근하지 않는다.
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
texture=bpy.data.images.load(str(ROOT/'Bokusei_Face_Albedo.png'))
material=bpy.data.materials.new('Bokusei_P09_Albedo');material.use_nodes=True
node=material.node_tree.nodes.new('ShaderNodeTexImage');node.image=texture
material.node_tree.links.new(node.outputs['Color'],material.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
objects=[]
for name,points in [('Bokusei_SourceAligned',source),('Bokusei_P09_NewFace',shaped)]:
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(points.tolist(),[],np.concatenate(triangles).tolist());mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
    mesh.materials.append(material);layer=mesh.uv_layers.new(name='UVMap')
    for loop in mesh.loops:layer.data[loop.index].uv=uv[loop.vertex_index]
    for polygon in mesh.polygons:polygon.use_smooth=True
    obj.shape_key_add(name='Basis')
    for key,delta in shapes:
        coordinates=source+delta if name.endswith('Aligned') else sculpt(source+delta)+offsets
        obj.shape_key_add(name=key).data.foreach_set('co',coordinates.ravel())
    objects.append(obj)
objects[0].hide_set(True);objects[0].hide_render=True
bpy.context.view_layer.objects.active=objects[1];objects[1].select_set(True)
with (ROOT/'sculpt.bin').open('wb') as stream:
    stream.write(struct.pack('<ii',count,shape_count));stream.write(shaped.astype('<f4').tobytes())
    for name,delta in shapes:
        value=sculpt(source+delta)+offsets-shaped
        assert np.isfinite(value).all()
        stream.write(value.astype('<f4').tobytes())
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Bokusei_P09_Face.blend'),compress=True)
report.update({'vertices':count,'expressions':shape_count,'moved_vertices':int(np.count_nonzero(np.linalg.norm(shaped-source,axis=1)>.00001)),
               'sculpt_max_m':float(np.max(np.linalg.norm(sculpt(source)-source,axis=1)))})
(ROOT/'sculpt_report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
