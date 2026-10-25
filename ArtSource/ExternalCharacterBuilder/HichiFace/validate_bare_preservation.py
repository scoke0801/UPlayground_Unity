"""연결면 교체 전후 얼굴의 정점·UV·표정·본 계약을 검사한다."""
import json
import struct
from pathlib import Path
import numpy as np

root=Path(__file__).resolve().parents[3]
folder=Path(__file__).resolve().parent
out=root/'output/HichiBareNeckFix'

def name(f):
    n=shift=0
    while True:
        b=f.read(1)[0];n|=(b&127)<<shift
        if not b&128:break
        shift+=7
    return f.read(n)

def read(path):
    with path.open('rb') as f:
        count,slots,shapes=struct.unpack('<iii',f.read(12))
        vertices=np.frombuffer(f.read(count*12),'<f4').reshape(count,3)
        uv=f.read(count*8);triangles=[];keys=[];deltas=[]
        for _ in range(slots):triangles.append(f.read(struct.unpack('<i',f.read(4))[0]*4))
        for _ in range(shapes):
            keys.append(name(f));deltas.append(np.frombuffer(f.read(count*12),'<f4').reshape(count,3))
        weights=f.read(count*32);bones=f.read()
    return vertices,uv,triangles,keys,np.array(deltas),weights,bones

before=read(out/'Backup/final.bin');after=read(folder/'final.bin')
assert np.array_equal(before[0][:9774],after[0][:9774])
assert before[1][:9774*8]==after[1][:9774*8]
assert before[2][:2]==after[2][:2]
assert before[3]==after[3]
assert np.array_equal(before[4][:,:9774],after[4][:,:9774])
assert before[5][:9774*32]==after[5][:9774*32] and before[6]==after[6]
assert (out/'Backup/Hichi_P09_Head.asset.meta').read_bytes()==(root/'Assets/08.Models/Characters/HichiP09/Hichi_P09_Head.asset.meta').read_bytes()
assert np.isfinite(after[0]).all() and np.isfinite(after[4]).all()
report={'source_face_vertices_preserved':9774,'shape_names_and_deltas_preserved':36,'bones_and_face_weights_preserved':True,'face_uv_and_triangles_preserved':True,'mesh_guid_preserved':True,'total_vertices':len(after[0])}
(out/'preservation_result.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
