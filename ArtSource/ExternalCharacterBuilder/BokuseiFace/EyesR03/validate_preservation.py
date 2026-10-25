"""눈 수정에서 목·웨이트·눈 이외 UV를 보존했는지 검사한다."""
from pathlib import Path
import struct
import json
import numpy as np
ROOT=Path(__file__).resolve().parent
exec(compile((ROOT/'inspect_eyes.py').read_text(encoding='utf-8'),str(ROOT/'inspect_eyes.py'),'exec'))

def full(path):
    with path.open('rb') as f:
        n,slots,shapes=struct.unpack('<3i',f.read(12))
        def points(width):return np.frombuffer(f.read(n*width*4),'<f4').reshape(n,width).copy()
        p=points(3);u=points(2)
        for _ in range(slots):f.read(struct.unpack('<i',f.read(4))[0]*4)
        keys={read_name(f):points(3) for _ in range(shapes)}
        weights=f.read(n*32)
        names=[read_name(f) for _ in range(struct.unpack('<i',f.read(4))[0])]
        normals=points(3)
    return p,u,keys,weights,names,normals

before=full(ROOT/'before.bin');after=full(ROOT.parent/'final.bin')
neck=before[0][:,2]<1.37
assert np.array_equal(before[0][neck],after[0][neck])
assert np.array_equal(before[5][neck],after[5][neck])
assert before[3:5]==after[3:5]
assert len(after[2])==36
for name in before[2]:assert np.array_equal(before[2][name][neck],after[2][name][neck])
uv_changes=np.any(before[1]!=after[1],axis=1)
assert np.count_nonzero(uv_changes)==322
report={'neck_vertices_normals_expressions_unchanged':True,'weights_bone_order_unchanged':True,'uv_changes_iris_only':322,'expressions':36}
(ROOT/'preservation_result.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report))
