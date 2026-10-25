"""원본 목·몸통 경계 사이의 급격한 전후 폭 차이를 연속 변형으로 완화한다."""
import json
import struct
from pathlib import Path
import numpy as np
import bpy

root=Path('C:/UsingProject/UnityProject/UPlayground')
folder=root/'ArtSource/ExternalCharacterBuilder/HichiFace'
output=root/'output/HichiNeckFix'
with (output/'Backup/final.bin').open('rb') as f:
    count,slots,shape_count=struct.unpack('<iii',f.read(12))
    basis=np.frombuffer(f.read(count*12),'<f4').reshape(-1,3).copy()
    uv=np.frombuffer(f.read(count*8),'<f4').reshape(-1,2).copy()
    triangles=[]
    for s in range(slots):
        n=struct.unpack('<i',f.read(4))[0]
        triangles.append(np.frombuffer(f.read(n*4),'<i4').reshape(-1,3).copy())
    shapes=[]
    for s in range(shape_count):
        length=shift=0
        while True:
            b=f.read(1)[0];length|=(b&127)<<shift
            if not b&128:break
            shift+=7
        name=f.read(length).decode('utf-8')
        shapes.append((name,np.frombuffer(f.read(count*12),'<f4').reshape(-1,3).copy()))
origins=np.array([int(x) for x in (root/'output/HichiP09/neck_origins.txt').read_text().split(',')])
assert count==9888 and shape_count==36 and len(origins)==count

def smooth(a,b,x):
    t=np.clip((x-a)/(b-a),0,1)
    return t*t*(3-2*t)

def deform(points):
    p=points.copy()
    # 눈·입·턱 앞면에는 닿지 않고, 목구멍 주변의 피부만 뒤쪽으로 넓힌다.
    falloff=np.exp(-((points[:9774,2]-1.363)/.024)**2)
    falloff*=1-smooth(1.394,1.415,points[:9774,2])
    falloff*=smooth(-.018,.018,points[:9774,1])
    falloff*=np.exp(-((points[:9774,1]-.020)/.024)**2)
    falloff*=1-smooth(.027,.050,np.abs(points[:9774,0]))
    falloff*=uv[:9774,1]<.49
    p[:9774,1]+=.023*falloff
    p[:9774,0]*=1+.26*falloff
    for i in range(9774,count):
        if origins[i]>=0:p[i]=p[origins[i]]
    return p

vertices=deform(basis)
changed=np.linalg.norm(vertices-basis,axis=1)>1e-7
assert np.isfinite(vertices).all()
assert np.max(np.abs(vertices[origins<0]-basis[origins<0]))==0
mesh=bpy.data.meshes.new('Hichi_NeckContour_r02')
mesh.from_pydata(vertices.tolist(),[],np.concatenate(triangles)[:,::-1].tolist())
mesh.update()
scene=bpy.data.scenes.new('Hichi_NeckContour_r02')
obj=bpy.data.objects.new(mesh.name,mesh);scene.collection.objects.link(obj)
obj.shape_key_add(name='Basis')
with (output/'neck_contour.bin').open('wb') as f:
    f.write(struct.pack('<ii',count,shape_count));f.write(vertices.astype('<f4').tobytes())
    for name,delta in shapes:
        shape=deform(basis+delta)
        obj.shape_key_add(name=name).data.foreach_set('co',shape.ravel())
        f.write((shape-vertices).astype('<f4').tobytes())
report={'vertices':count,'shapes':shape_count,'changed_vertices':int(changed.sum()),'maximum_displacement_m':float(np.linalg.norm(vertices-basis,axis=1).max()),'lower_boundary_preserved':True}
(output/'sculpt_result.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
