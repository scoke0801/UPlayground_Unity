"""Siuha 원본의 연결 부품과 목 경계를 조사한다."""
import struct
from pathlib import Path
from collections import Counter, defaultdict
import numpy as np

ROOT = Path(__file__).resolve().parent
with (ROOT/'source.bin').open('rb') as f:
    n,s,k=struct.unpack('<iii',f.read(12))
    v=np.frombuffer(f.read(n*12),'<f4').reshape(n,3)
    uv=np.frombuffer(f.read(n*8),'<f4').reshape(n,2)
    tris=[]
    for i in range(s):
        size=struct.unpack('<i',f.read(4))[0]
        tris.append(np.frombuffer(f.read(size*4),'<i4').reshape(-1,3))
for slot,faces in enumerate(tris):
    links=defaultdict(set)
    for tri in faces:
        for a in tri: links[int(a)].update(map(int,tri))
    seen=set();parts=[]
    for vertex in links:
        if vertex in seen:continue
        todo=[vertex];part=[]
        while todo:
            a=todo.pop()
            if a in seen:continue
            seen.add(a);part.append(a);todo.extend(links[a]-seen)
        parts.append(part)
    print('SLOT',slot)
    for part in sorted(parts,key=len,reverse=True)[:30]:
        print('part',len(part),'min',v[part].min(0),'max',v[part].max(0),'uv',uv[part].mean(0))
    _,first,inv=np.unique(np.round(v,6),axis=0,return_index=True,return_inverse=True)
    edges=Counter(tuple(sorted((int(inv[a]),int(inv[b])))) for t in faces for a,b in zip(t,np.roll(t,-1)))
    boundary=defaultdict(set)
    for (a,b),count in edges.items():
        if count==1:boundary[a].add(b);boundary[b].add(a)
    seen=set()
    for start in boundary:
        if start in seen:continue
        todo=[start];part=[]
        while todo:
            a=todo.pop()
            if a in seen:continue
            seen.add(a);part.append(first[a]);todo.extend(boundary[a]-seen)
        if len(part)>8:print('boundary',len(part),'min',v[part].min(0),'max',v[part].max(0))
