"""r02 목·피부를 유지하고 눈 주변과 해당 표정만 다시 조형한다."""
exec(compile((__import__('pathlib').Path(__file__).parent/'inspect_eyes.py').read_text(encoding='utf-8'),__file__,'exec'))
import bpy

def smooth(a,b,x):
    t=np.clip((x-a)/(b-a),0,1);return t*t*(3-2*t)

def reshape(points):
    result=points.copy();x,y,z=points.T
    front=1-smooth(-.032,-.012,y)
    region=(1-smooth(.021,.039,np.abs(z-1.416)))*smooth(.006,.018,np.abs(x))*(1-smooth(.064,.081,np.abs(x)))*front
    result[:,0]+=(np.abs(x)-.039)*np.sign(x)*.105*region
    result[:,2]+=(-(z-1.414)*.23-(np.abs(x)-.039)*.065)*region
    # 굵은 아이라인의 꼬리를 눈 가장자리 쪽으로 압축한다.
    outer=smooth(.053,.074,np.abs(x))*region
    result[:,0]-=np.sign(x)*.003*outer
    return result

# 뾰족한 추가 속눈썹은 기존 독립 메시의 표정 델타로 길이를 줄인다.
lash_offset=(source_keys['eyeline_eyelash_1_hide']*.82+source_keys['eyeline_eyelash_2_hide']*.68)
iris_groups=[g for g in groups.values() if len(g)>100 and np.all(np.linalg.norm(source_keys['Iris_all_small'][g],axis=1)>.00001)]
assert len(iris_groups)==2 and all(len(g)==161 for g in iris_groups)
iris_indices=np.concatenate(iris_groups)
new_uv=uv.copy()
for group in iris_groups:
    lo=uv[group].min(axis=0);hi=uv[group].max(axis=0)
    new_uv[group]=((uv[group]-lo)/(hi-lo)-.5)*1.12+.5
# 원본의 큰 반짝임 도형을 제거하고 새 홍채에 포함된 작은 반사만 남긴다.
highlight_indices=set()
for g in groups.values():
    if len(g)<80 and np.all(np.linalg.norm(source_keys['Iris_all_small'][g],axis=1)>.00001):highlight_indices.update(g)
keep=[];irises=[];removed=0
iris_set=set(iris_indices.tolist())
for tri in triangles[0]:
    if all(int(i) in highlight_indices for i in tri):removed+=1
    elif all(int(i) in iris_set for i in tri):irises.append(tri)
    else:keep.append(tri)
out_triangles=[np.array(keep),triangles[1],triangles[2],np.array(irises)]
shaped=reshape(positions+lash_offset)
shape_values={}
for name,delta in keys.items():
    posed=positions+delta
    revised=reshape(posed+lash_offset)
    new_delta=revised-shaped
    # 수정 범위 밖에서는 뺄셈의 부동소수점 오차까지 피해서 원래 델타를 보존한다.
    untouched=np.all(shaped==positions,axis=1)&np.all(revised==posed,axis=1)
    new_delta[untouched]=delta[untouched]
    shape_values[name]=new_delta
assert np.isfinite(shaped).all()
assert np.array_equal(shaped[positions[:,2]<1.37],positions[positions[:,2]<1.37])
with (ROOT/'eyes.bin').open('wb') as f:
    f.write(struct.pack('<iii',len(shaped),len(out_triangles),len(keys)))
    f.write(shaped.astype('<f4').tobytes());f.write(new_uv.astype('<f4').tobytes())
    for tri in out_triangles:f.write(struct.pack('<i',tri.size));f.write(tri.astype('<i4').tobytes())
    for name,delta in shape_values.items():f.write(delta.astype('<f4').tobytes())
# 저작 파일은 별도 background 프로세스에서 저장한다.
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
for name,points in [('Bokusei_R02_Before',positions),('Bokusei_R03_Eyes',shaped)]:
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(points.tolist(),[],np.concatenate(out_triangles).tolist());mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
    obj.shape_key_add(name='Basis')
    for key,delta in (keys if name.endswith('Before') else shape_values).items():obj.shape_key_add(name=key).data.foreach_set('co',(points+delta).ravel())
    uv_layer=mesh.uv_layers.new(name='UVMap')
    for loop in mesh.loops:uv_layer.data[loop.index].uv=new_uv[loop.vertex_index]
    for poly in mesh.polygons:poly.use_smooth=True
    if name.endswith('Before'):obj.hide_set(True);obj.hide_render=True
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Bokusei_Eyes_R03.blend'),compress=True)
report={'vertices':len(shaped),'moved_vertices':int(np.count_nonzero(np.linalg.norm(shaped-positions,axis=1)>.000001)),
        'iris_vertices':len(iris_indices),'removed_highlight_triangles':removed,'expressions':len(keys),'neck_unchanged':True}
(ROOT/'sculpt_result.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
