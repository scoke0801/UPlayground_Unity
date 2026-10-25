"""조형 교환 데이터의 유효성·두피 뒤집힘·눈 축소를 검사한다."""
import struct
import json
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parent
with (ROOT/'source.bin').open('rb') as f:
    count,slots,keys=struct.unpack('<iii',f.read(12))
    source=np.frombuffer(f.read(count*12),'<f4').reshape(count,3)
    f.read(count*8);triangles=[]
    for _ in range(slots):
        n=struct.unpack('<i',f.read(4))[0];triangles.append(np.frombuffer(f.read(n*4),'<i4').reshape(-1,3))
with (ROOT/'sculpt.bin').open('rb') as f:
    assert struct.unpack('<ii',f.read(8))==(count,keys)
    vertices=np.frombuffer(f.read(count*12),'<f4').reshape(count,3)
    deltas=np.frombuffer(f.read(),'<f4').reshape(keys,count,3)
assert np.isfinite(vertices).all() and np.isfinite(deltas).all()
tris=triangles[0];a=source[tris];b=vertices[tris]
upper=a[:,:,2].min(axis=1)>1.465
old=np.cross(a[:,1]-a[:,0],a[:,2]-a[:,0]);new=np.cross(b[:,1]-b[:,0],b[:,2]-b[:,0])
flips=upper&(np.sum(old*new,axis=1)<0)
assert not flips.any(), f'두피 뒤집힘 {flips.sum()}'
# 원본 정면 홍채의 연결 부품을 UV와 깊이로 한정하여 실제 축소량을 기록한다.
eye=(source[:,0]>.022)&(source[:,0]<.045)&(source[:,1]<-.036)&(source[:,1]>-.0426)&(source[:,2]>1.4007)&(source[:,2]<1.4294)
extent_before=np.ptp(source[eye],axis=0);extent_after=np.ptp(vertices[eye],axis=0)
assert extent_after[2]<extent_before[2]*.9
report={'finite':True,'source_shapes':keys,'upper_triangles':int(upper.sum()),'flipped_upper_triangles':int(flips.sum()),'eye_sample_extent_before':extent_before.tolist(),'eye_sample_extent_after':extent_after.tolist(),'eye_height_ratio':float(extent_after[2]/extent_before[2])}
(ROOT/'sculpt_validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print(json.dumps(report))
