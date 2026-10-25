"""얼굴 비례 수정 이후 목·웨이트 보존과 양쪽 홍채 비례를 확인한다."""
from pathlib import Path
import struct,json
import numpy as np
ROOT=Path(__file__).resolve().parent
def name(f):
    size=shift=0
    while True:
        b=f.read(1)[0];size|=(b&127)<<shift
        if not b&128:break
        shift+=7
    return f.read(size).decode('utf-8')
def read(path):
    with path.open('rb') as f:
        n,slots,shapes=struct.unpack('<3i',f.read(12))
        def points(w):return np.frombuffer(f.read(n*w*4),'<f4').reshape(n,w).copy()
        p=points(3);uv=points(2)
        tris=[np.frombuffer(f.read(struct.unpack('<i',f.read(4))[0]*4),'<i4') for _ in range(slots)]
        keys={name(f):points(3) for _ in range(shapes)}
        weights=f.read(n*32);bones=[name(f) for _ in range(struct.unpack('<i',f.read(4))[0])];normals=points(3)
    return p,uv,tris,keys,weights,bones,normals
before=read(ROOT/'before.bin');after=read(ROOT.parent/'final.bin')
assert np.array_equal(before[1],after[1])
assert all(np.array_equal(a,b) for a,b in zip(before[2],after[2]))
upper=before[0][:,2]>=1.395
assert np.array_equal(before[0][upper],after[0][upper])
indices=np.loadtxt(ROOT.parent/'neck_bindings.txt',delimiter=',',encoding='utf-8-sig')[:,0].astype(int)
protected=np.linalg.norm(before[0][:,None,:]-before[0][indices][None,:,:],axis=2).min(axis=1)<.022
for channel in [0,6]:assert np.array_equal(before[channel][protected],after[channel][protected])
assert before[4:6]==after[4:6]
for key in before[3]:assert np.array_equal(before[3][key][protected],after[3][key][protected])
iris=after[0][np.unique(after[2][3])]
sizes=[np.ptp(iris[iris[:,0]*sign>0],axis=0) for sign in [-1,1]]
assert np.max(np.abs(sizes[0]-sizes[1]))<.00001
report={'protected_neck_vertices':int(np.count_nonzero(protected)),'neck_positions_normals_expressions_unchanged':True,'weights_bones_unchanged':True,'iris_left_right_size_error_mm':float(np.max(np.abs(sizes[0]-sizes[1]))*1000),'expressions':len(after[3])}
(ROOT/'preservation_result.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
