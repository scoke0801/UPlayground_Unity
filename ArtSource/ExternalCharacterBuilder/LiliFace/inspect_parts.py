"""얼굴 재질에 합쳐진 부품과 목 경계를 수치로 조사한다."""
import struct
import numpy as np
from pathlib import Path
p=Path(__file__).resolve().parent
with (p/'source.bin').open('rb') as f:
    n,s,k=struct.unpack('<iii',f.read(12))
    v=np.frombuffer(f.read(n*12),'<f4').reshape(n,3)
    uv=np.frombuffer(f.read(n*8),'<f4').reshape(n,2)
    tris=[]
    for i in range(s):
        size=struct.unpack('<i',f.read(4))[0]
        tris.append(np.frombuffer(f.read(size*4),'<i4').reshape(-1,3))
links={}
for tri in tris[2]:
    for a in tri:
        links.setdefault(int(a),set()).update(int(b) for b in tri)
seen=set();parts=[]
for vertex in links:
    if vertex in seen:continue
    todo=[vertex];part=[]
    while todo:
        a=todo.pop()
        if a in seen:continue
        seen.add(a);part.append(a);todo.extend(links[a]-seen)
    parts.append(part)
for part in sorted(parts,key=len,reverse=True)[:35]:
    print(len(part),'min',v[part].min(0),'max',v[part].max(0),'uv',uv[part].mean(0))
neck=[i for part in parts if v[part,2].min()<1.32 and v[part,2].max()<1.42 for i in part]
assert len(neck)==342, 'Lili 목 토폴로지가 변경되어 재조사가 필요합니다.'
(p/'source_neck_indices.txt').write_text(','.join(map(str,neck)))
