exec(compile((__import__('pathlib').Path(__file__).parent/'inspect_source.py').read_text(encoding='utf-8'),__file__,'exec'))
from collections import Counter,defaultdict
for name,points,tris in [('source',source,triangles[0]),('native',native,native_triangles)]:
    unique,inv=np.unique(np.round(points,5),axis=0,return_inverse=True)
    edges=Counter()
    for tri in inv[tris]:
        for a,b in zip(tri,np.roll(tri,-1)):
            if a!=b:edges[tuple(sorted((int(a),int(b))))]+=1
    adjacency=defaultdict(set)
    for (a,b),n in edges.items():
        if n==1:adjacency[a].add(b);adjacency[b].add(a)
    visited=set();components=[]
    for v in adjacency:
        if v in visited:continue
        stack=[v];group=[]
        while stack:
            i=stack.pop()
            if i in visited:continue
            visited.add(i);group.append(i);stack.extend(adjacency[i]-visited)
        q=unique[group]
        components.append({'count':len(group),'min':q.min(axis=0).tolist(),'max':q.max(axis=0).tolist(),'center':q.mean(axis=0).tolist(),'positions':q.tolist() if len(group)<60 else []})
    (ROOT/(name+'_boundaries.json')).write_text(json.dumps(components,indent=2))
    print(name,json.dumps([{k:v for k,v in c.items() if k!='positions'} for c in components]))
