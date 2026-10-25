"""정수리와 원본 가닥 구조를 유지하면서 아래쪽 옆머리를 얼굴에 맞춘다."""
import json
import struct
from pathlib import Path
import numpy as np

folder=Path(__file__).resolve().parent
settings=json.loads((folder/'hair_fit.json').read_text(encoding='utf-8-sig'))
def smooth(a,b,value):
 t=np.clip((value-a)/(b-a),0,1)
 return t*t*(3-2*t)
report=[]
with (folder/'hair_source.bin').open('rb') as reader, (folder/'hair_fitted.bin').open('wb') as writer:
 count=struct.unpack('<i',reader.read(4))[0];writer.write(struct.pack('<i',count))
 for _ in range(count):
  size=reader.read(1)[0];name=reader.read(size).decode('utf-8')
  vertices=struct.unpack('<i',reader.read(4))[0]
  source=np.frombuffer(reader.read(vertices*12),'<f4').reshape(vertices,3).copy()
  indices=struct.unpack('<i',reader.read(4))[0]
  triangles=np.frombuffer(reader.read(indices*4),'<i4').reshape(-1,3)
  p=source.copy();config=settings[name]
  amount=(1-smooth(1.435,1.495,source[:,2]))*smooth(.035,.075,np.abs(source[:,0]))
  p[:,0]*=1+(config['lower_width_scale']-1)*amount
  assert np.isfinite(p).all()
  before=source[triangles];after=p[triangles]
  n0=np.cross(before[:,1]-before[:,0],before[:,2]-before[:,0]);n1=np.cross(after[:,1]-after[:,0],after[:,2]-after[:,0])
  assert np.all(np.sum(n0*n1,axis=1)>=0),name+' 헤어 표면 뒤집힘'
  writer.write(struct.pack('<i',vertices));writer.write(p.astype('<f4').tobytes())
  report.append({'name':name,'vertices':vertices,'max_offset_m':float(np.linalg.norm(p-source,axis=1).max()),'flipped_triangles':0})
(folder/'hair_fit_validation.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report))
