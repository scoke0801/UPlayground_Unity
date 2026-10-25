"""두피 복원 이외의 얼굴 조형·표정·목·토폴로지와 에셋 GUID를 검사한다."""
import struct
import json
from pathlib import Path
import numpy as np

ROOT=Path(__file__).resolve().parent
PROJECT=ROOT.parents[2]
BACKUP=PROJECT/'output/LianLianHairFit/backup'

def read_mesh(path):
    with path.open('rb') as f:
        count,slots,shapes=struct.unpack('<3i',f.read(12))
        vertices=np.frombuffer(f.read(count*12),'<f4').reshape(-1,3).copy()
        rest=f.read()
    return (count,slots,shapes),vertices,rest

old=read_mesh(BACKUP/'ArtSource/ExternalCharacterBuilder/LianLianFace/final.bin')
new=read_mesh(ROOT/'final.bin')
assert old[0]==new[0] and old[2]==new[2], 'UV·삼각형·표정 델타·웨이트·본 이름 변경'
changed=np.linalg.norm(old[1]-new[1],axis=1)>1e-7
assert np.all(new[1][changed,2]>1.395), '두피 복원 영역 외 위치 변경'
with (ROOT/'sculpt.bin').open('rb') as f:
    n,k=struct.unpack('<2i',f.read(8)); sculpt=np.frombuffer(f.read(n*12),'<f4').reshape(-1,3)
with (ROOT/'topology.bin').open('rb') as f:
    def ints():
        count=struct.unpack('<i',f.read(4))[0]
        return np.frombuffer(f.read(count*4),'<i4')
    triangles=ints();bottom=ints();bridge=ints()
ids=np.concatenate([np.unique(triangles),np.array(list(dict.fromkeys(int(i) for i in bridge if i>=0)))])
assert np.array_equal(new[1][:len(ids)],sculpt[ids]), 'Unity 메시와 Blender 교환본 불일치'
scope={'__file__':str(ROOT/'sculpt_face.py'),'__name__':'preservation_check'}
exec((ROOT/'sculpt_face.py').read_text(encoding='utf-8-sig').split('shaped=sculpt(source)')[0].replace('import bpy',''),scope)
assert np.array_equal(sculpt,scope['sculpt'](scope['source'])), '헤어용 두피 변형 잔존'
metadata=0
for item in json.loads((BACKUP/'manifest.json').read_text(encoding='utf-8-sig')):
    relative=item['path']
    if relative.endswith('.meta'):
        assert (PROJECT/relative).read_bytes()==(BACKUP/relative).read_bytes(), 'GUID 변경: '+relative
        metadata+=1
result={'restored_vertices':int(changed.sum()),'max_restore_m':float(np.linalg.norm(old[1]-new[1],axis=1).max()),'uv_triangles_expression_weights_bones_unchanged':True,'head_without_hair_deformation':True,'metadata_preserved':metadata}
(PROJECT/'output/LianLianHairFit/preservation.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result))
