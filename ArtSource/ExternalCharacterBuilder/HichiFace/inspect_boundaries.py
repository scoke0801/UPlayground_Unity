import json
import numpy as np
from collections import Counter, defaultdict
from pathlib import Path
root=Path(__file__).resolve().parent
d=json.loads((root/'source.json').read_text(encoding='utf-8-sig'))
for label,vertices,triangles in [('source',d['vertices'],d['submeshes'][0]),('native',d['nativeVertices'],d['nativeTriangles'])]:
    v=np.array(vertices);t=np.array(triangles).reshape(-1,3)
    weld={};remap=[]
    for i,p in enumerate(v):
        key=tuple(np.round(p,6));weld.setdefault(key,i);remap.append(weld[key])
    edges=Counter()
    for tri in t:
        ids=[remap[i] for i in tri]
        for a,b in zip(ids,ids[1:]+ids[:1]):
            if a!=b:edges[tuple(sorted((a,b)))]+=1
    adj=defaultdict(set)
    for (a,b),n in edges.items():
        if n==1:adj[a].add(b);adj[b].add(a)
    seen=set();loops=[]
    for start in adj:
        if start in seen:continue
        stack=[start];component=[]
        while stack:
            i=stack.pop()
            if i in seen:continue
            seen.add(i);component.append(i);stack.extend(adj[i]-seen)
        pts=v[component];loops.append(component)
        print(label,len(component),'min',pts.min(0),'max',pts.max(0),'degree',sorted(set(len(adj[i]) for i in component)))
    (root/(label+'_boundaries.json')).write_text(json.dumps(loops),encoding='utf-8')
