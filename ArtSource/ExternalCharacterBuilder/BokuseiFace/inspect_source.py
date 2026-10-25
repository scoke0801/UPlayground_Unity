import bpy, json, struct
import numpy as np
from pathlib import Path

ROOT=Path(__file__).resolve().parent
def read_string(stream):
    size=shift=0
    while True:
        byte=stream.read(1)[0]; size |= (byte & 127)<<shift
        if not byte & 128: break
        shift+=7
    return stream.read(size).decode('utf-8')

with (ROOT/'source.bin').open('rb') as stream:
    def integer(): return struct.unpack('<i',stream.read(4))[0]
    def array(count,width=3,dtype='<f4'):
        return np.frombuffer(stream.read(count*width*4),dtype=dtype).copy().reshape(count,width)
    count,slots,shape_count=integer(),integer(),integer()
    source=array(count); uv=array(count,2)
    triangles=[array(integer()//3,3,'<i4') for _ in range(slots)]
    shapes=[(read_string(stream),array(count)) for _ in range(shape_count)]
    native=array(integer());native_triangles=array(integer()//3,3,'<i4')
    assert stream.read()==b''

report={'source_min':source.min(axis=0).tolist(),'source_max':source.max(axis=0).tolist(),
        'native_min':native.min(axis=0).tolist(),'native_max':native.max(axis=0).tolist(),
        'landmarks':{name:source[np.linalg.norm(delta,axis=1)>.0001].mean(axis=0).tolist()
                     for name,delta in shapes if name in ['eye_close','mouth_a','nose_tip_lower','brow_lower','faceline_chin_thick','faceline_neckhole_fill']}}
(ROOT/'landmarks.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
