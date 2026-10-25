"""R02 얼굴·목 연결을 보존하며 얼굴 대비 윗두상과 헤어 볼륨을 조정한다."""
import bpy,struct,json
import numpy as np
from pathlib import Path
ROOT=Path(__file__).resolve().parent
SOURCE=ROOT.parent/'RepairR02'
def read_string(f):
    size=shift=0
    while True:
        b=f.read(1)[0];size|=(b&127)<<shift
        if not b&128:break
        shift+=7
    return f.read(size).decode('utf-8')
def write_string(f,s):
    data=s.encode('utf-8');size=len(data)
    while size>127:f.write(bytes([(size&127)|128]));size>>=7
    f.write(bytes([size]));f.write(data)
with (SOURCE/'repaired_face.bin').open('rb') as f:
    n,s,k=struct.unpack('<iii',f.read(12));before=np.frombuffer(f.read(n*12),'<f4').reshape(n,3).copy();uv=f.read(n*8);tris=[]
    for _ in range(s):
        length=struct.unpack('<i',f.read(4))[0];tris.append(np.frombuffer(f.read(length*4),'<i4').reshape(-1,3).copy())
    keys=[(read_string(f),np.frombuffer(f.read(n*12),'<f4').reshape(n,3).copy()) for _ in range(k)]
    tail=f.read()
def smooth(a,b,x):
    t=np.clip((x-a)/(b-a),0,1);return t*t*(3-2*t)
def shape(points):
    p=points.copy();neck=smooth(1.35,1.375,p[:,2]);crown=smooth(1.43,1.508,p[:,2])
    p[:,0]*=1+neck*.07-.18*smooth(1.425,1.475,points[:,2])
    p[:,2]+=neck*.045*(points[:,2]-1.365)-.031*crown
    upper=smooth(1.425,1.47,points[:,2]);p[:,1]=points[:,1]+upper*((.025-points[:,1])*.25+.004)
    return p
vertices=shape(before)
deltas=[(name,shape(before+d)-vertices) for name,d in keys]
assert np.array_equal(vertices[-16:],before[-16:])
assert np.isfinite(vertices).all() and all(np.isfinite(d).all() for _,d in deltas)
with (ROOT/'repaired_face.bin').open('wb') as f:
    f.write(struct.pack('<iii',n,s,k));f.write(vertices.astype('<f4').tobytes());f.write(uv)
    for t in tris:f.write(struct.pack('<i',t.size));f.write(t.astype('<i4').tobytes())
    for name,d in deltas:write_string(f,name);f.write(d.astype('<f4').tobytes())
    f.write(tail)
bpy.ops.wm.open_mainfile(filepath=str(SOURCE/'Siuha_R02_HeadHairNeck.blend'))
face=bpy.data.objects['Siuha_R02_Head_Neck'];face.name='Siuha_R03_Head_Neck'
for key in face.data.shape_keys.key_blocks:
    coords=np.empty(n*3,dtype=np.float32);key.data.foreach_get('co',coords);key.data.foreach_set('co',shape(coords.reshape(-1,3)).ravel())
face.data.vertices.foreach_set('co',vertices.ravel());face.data.update()
report={'face_vertices':n,'crown_before':float(before[:,2].max()),'crown_after':float(vertices[:,2].max()),'body_boundary_max_error':float(np.abs(vertices[-16:]-before[-16:]).max()),'face_width_scale':1.07}
for name in ['Hair_03','Hair_11']:
    obj=bpy.data.objects[name+'_Fitted'];mesh=obj.data;old=np.empty(len(mesh.vertices)*3,dtype=np.float32);mesh.vertices.foreach_get('co',old);old=old.reshape(-1,3);v=old.copy()
    # 얼굴과 같은 연속 변형을 적용해 가닥 면의 연결과 기존 두피 여유를 유지한다.
    v=shape(old);v[:,0]*=.93
    v[:,1]=-.085+(v[:,1]+.085)*.93
    v[:,1]+=.012*smooth(.025,.07,old[:,1])*smooth(1.40,1.45,old[:,2])
    side=smooth(.025,.045,np.abs(old[:,0]))*(1-smooth(.065,.085,np.abs(old[:,0])))
    side*=(1-smooth(-.02,.025,old[:,1]))*(1-smooth(1.44,1.47,old[:,2]))
    v[:,0]+=.006*np.sign(v[:,0])*side
    assert np.isfinite(v).all()
    mesh.vertices.foreach_set('co',v.ravel());mesh.update()
    with (ROOT/(name+'_fitted.bin')).open('wb') as f:f.write(struct.pack('<i',len(v)));f.write(v.astype('<f4').tobytes())
    report[name]={'before_min':old.min(0).tolist(),'before_max':old.max(0).tolist(),'after_min':v.min(0).tolist(),'after_max':v.max(0).tolist()}
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Siuha_R03_HeadHairNeck.blend'),compress=True)
(ROOT/'proportions.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print(json.dumps(report))

