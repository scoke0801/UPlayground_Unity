"""목 수정 전후의 직렬화 계약과 변경 범위를 비교한다."""
import json
import struct
from pathlib import Path
import numpy as np
root=Path('C:/UsingProject/UnityProject/UPlayground')
folder=root/'ArtSource/ExternalCharacterBuilder/HichiFace'
output=root/'output/HichiNeckFix'
def read(path):
    with path.open('rb') as f:
        count,slots,shapes=struct.unpack('<iii',f.read(12))
        vertices=np.frombuffer(f.read(count*12),'<f4').reshape(-1,3).copy()
        uv=f.read(count*8);triangles=[];deltas=[];names=[]
        for s in range(slots):
            length=struct.unpack('<i',f.read(4))[0];triangles.append(f.read(length*4))
        for s in range(shapes):
            length=shift=0
            while True:
                b=f.read(1)[0];length|=(b&127)<<shift
                if not b&128:break
                shift+=7
            names.append(f.read(length).decode('utf-8'));deltas.append(np.frombuffer(f.read(count*12),'<f4').reshape(-1,3).copy())
        weights_and_bones=f.read()
    return vertices,uv,triangles,names,np.array(deltas),weights_and_bones
before=read(output/'Backup/final.bin');after=read(folder/'final.bin')
for index in [1,2,3,5]:assert before[index]==after[index],f'보존 계약 불일치: {index}'
assert before[0].shape==after[0].shape
origin=np.array([int(v) for v in (root/'output/HichiP09/neck_origins.txt').read_text().split(',')])
assert np.array_equal(before[0][origin<0],after[0][origin<0])
assert np.array_equal(before[4][:,origin<0],after[4][:,origin<0])
front=before[0][:,1]<-.025
assert np.array_equal(before[0][front],after[0][front]),'얼굴 앞면 변경'
assert np.array_equal(before[4][:,front],after[4][:,front]),'얼굴 앞면 표정 변경'
changed=np.linalg.norm(after[0]-before[0],axis=1)>1e-7
assert np.isfinite(after[0]).all() and np.isfinite(after[4]).all()
back=1023
report={'vertices':len(after[0]),'shapes':len(after[3]),'changed_vertices':int(changed.sum()),'uv_triangles_bones_weights_preserved':True,'front_face_and_shapes_preserved':True,'lower_boundary_and_shapes_preserved':True,'back_neck_forward_offset_before_m':float(.05411322-before[0][back,1]),'back_neck_forward_offset_after_m':float(.05411322-after[0][back,1])}
(output/'preservation_result.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
