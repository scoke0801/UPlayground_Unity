"""피부와 분리된 코 선·점을 실제 연결 성분으로 식별한다."""
from inspect_topology import *
for slot,faces in enumerate(triangles[:5]):
    links=defaultdict(set)
    for a,b,c in faces:
        links[int(a)].update([int(b),int(c)]);links[int(b)].update([int(a),int(c)]);links[int(c)].update([int(a),int(b)])
    seen=set();parts=[]
    for start in links:
        if start in seen:continue
        todo=[start];part=[]
        while todo:
            i=todo.pop()
            if i in seen:continue
            seen.add(i);part.append(i);todo.extend(links[i]-seen)
        ids=np.array(part);parts.append({'slot':slot,'vertices':len(ids),'min':source[ids].min(0).tolist(),'max':source[ids].max(0).tolist(),'uv_min':uv[ids].min(0).tolist(),'uv_max':uv[ids].max(0).tolist(),'ids':part if len(ids)<45 else []})
    print(json.dumps(parts))
