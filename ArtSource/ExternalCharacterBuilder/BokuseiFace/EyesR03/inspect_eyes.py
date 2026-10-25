import json, struct
from pathlib import Path
import numpy as np

ROOT=Path(__file__).resolve().parent
def read_name(f):
    size=shift=0
    while True:
        b=f.read(1)[0];size|=(b&127)<<shift
        if not b&128:break
        shift+=7
    return f.read(size).decode('utf-8')
def read_mesh(path,source=False):
    with path.open('rb') as f:
        def integer():return struct.unpack('<i',f.read(4))[0]
        def array(n,w=3,dtype='<f4'):return np.frombuffer(f.read(n*w*4),dtype).reshape(n,w).copy()
        count,slots,shape_count=integer(),integer(),integer()
        positions=array(count);uv=array(count,2)
        triangles=[array(integer()//3,3,'<i4') for _ in range(slots)]
        keys={read_name(f):array(count) for _ in range(shape_count)}
        return positions,uv,triangles,keys
positions,uv,triangles,keys=read_mesh(ROOT/'before.bin')
original,_,_,source_keys=read_mesh(ROOT.parent/'source.bin',True)
parents=np.arange(len(positions))
def find(i):
    while parents[i]!=i:
        parents[i]=parents[parents[i]];i=parents[i]
    return i
for tri in triangles[0]:
    a=find(tri[0])
    for b in tri[1:]:parents[find(b)]=a
groups={}
for index in np.unique(triangles[0]):groups.setdefault(int(find(index)),[]).append(int(index))
report=[]
for group in groups.values():
    p=positions[group]
    if p[:,2].max()<1.39 or p[:,1].min()>-.02:continue
    report.append({'first':group[0],'count':len(group),'center':p.mean(axis=0).tolist(),'min':p.min(axis=0).tolist(),'max':p.max(axis=0).tolist(),'uv_min':uv[group].min(axis=0).tolist(),'uv_max':uv[group].max(axis=0).tolist(),
        'iris':int(np.count_nonzero(np.linalg.norm(source_keys['Iris_all_small'][group],axis=1)>.00001)),
        'lash1':int(np.count_nonzero(np.linalg.norm(source_keys['eyeline_eyelash_1_hide'][group],axis=1)>.00001)),
        'lash2':int(np.count_nonzero(np.linalg.norm(source_keys['eyeline_eyelash_2_hide'][group],axis=1)>.00001))})
(ROOT/'parts.json').write_text(json.dumps(report,indent=2))
print('눈 주변 연결 부위 분석:',len(report))
