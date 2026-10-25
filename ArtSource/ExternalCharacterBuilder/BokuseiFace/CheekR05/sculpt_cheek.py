"""양쪽 아래 볼의 돌출만 줄이고 r04의 눈·입·목 연결을 보존한다."""
from pathlib import Path
ROOT=Path(__file__).resolve().parent
reader=ROOT.parent/'BalanceR04/validate_balance.py'
exec(compile(reader.read_text(encoding='utf-8').split('before=read')[0],str(reader),'exec'))
import bpy
before=read(ROOT/'before.bin')
positions,uv,triangles,keys=before[:4]
neck_indices=np.loadtxt(ROOT.parent/'neck_bindings.txt',delimiter=',',encoding='utf-8-sig')[:,0].astype(int)
neck_distance=np.linalg.norm(positions[:,None,:]-positions[neck_indices][None,:,:],axis=2).min(axis=1)

def smooth(a,b,x):
    t=np.clip((x-a)/(b-a),0,1);return t*t*(3-2*t)

def reshape(points):
    result=points.copy();x,y,z=points.T
    height=smooth(1.344,1.360,z)*(1-smooth(1.374,1.395,z))
    side=smooth(.028,.049,np.abs(x))*(1-smooth(.005,.025,y))
    protected=smooth(.022,.036,neck_distance)
    result[:,0]-=.0045*np.sign(x)*height*side*protected
    return result

shaped=reshape(positions)
shape_values={}
for name,delta in keys.items():
    posed=positions+delta;revised=reshape(posed);value=revised-shaped
    untouched=np.all(shaped==positions,axis=1)&np.all(revised==posed,axis=1)
    value[untouched]=delta[untouched];shape_values[name]=value
assert np.isfinite(shaped).all()
assert np.array_equal(shaped[neck_distance<.022],positions[neck_distance<.022])
assert np.array_equal(shaped[positions[:,2]>=1.395],positions[positions[:,2]>=1.395])
with (ROOT/'cheek.bin').open('wb') as f:
    f.write(struct.pack('<3i',len(shaped),len(triangles),len(keys)))
    f.write(shaped.astype('<f4').tobytes());f.write(uv.astype('<f4').tobytes())
    for tri in triangles:f.write(struct.pack('<i',tri.size));f.write(tri.astype('<i4').tobytes())
    for delta in shape_values.values():f.write(delta.astype('<f4').tobytes())
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
mesh=bpy.data.meshes.new('Bokusei_R05_Cheek');mesh.from_pydata(shaped.tolist(),[],np.concatenate(triangles).reshape(-1,3).tolist());mesh.update()
obj=bpy.data.objects.new('Bokusei_R05_Cheek',mesh);bpy.context.collection.objects.link(obj)
obj.shape_key_add(name='Basis')
for name,delta in shape_values.items():obj.shape_key_add(name=name).data.foreach_set('co',(shaped+delta).ravel())
layer=mesh.uv_layers.new(name='UVMap')
for loop in mesh.loops:layer.data[loop.index].uv=uv[loop.vertex_index]
for polygon in mesh.polygons:polygon.use_smooth=True
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Bokusei_Cheek_R05.blend'),compress=True)
report={'moved_vertices':int(np.count_nonzero(np.linalg.norm(shaped-positions,axis=1)>.000001)),'max_inward_mm':float(np.max(np.abs(shaped[:,0]-positions[:,0]))*1000),'expressions':len(keys),'neck_unchanged':True,'eyes_unchanged':True}
(ROOT/'sculpt_result.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
