import struct
import json
from pathlib import Path
from collections import Counter, defaultdict
import numpy as np

ROOT = Path(__file__).resolve().parent

def read_string(stream):
    size = shift = 0
    while True:
        byte = stream.read(1)[0]
        size |= (byte & 127) << shift
        if not byte & 128: break
        shift += 7
    return stream.read(size).decode('utf-8')

with (ROOT/'source.bin').open('rb') as stream:
    def integer(): return struct.unpack('<i', stream.read(4))[0]
    def array(count, width=3, dtype='<f4'):
        return np.frombuffer(stream.read(count*width*4), dtype=dtype).copy().reshape(count,width)
    count, slots, shape_count = integer(), integer(), integer()
    source, uv = array(count), array(count,2)
    triangles = [array(integer()//3,3,'<i4') for _ in range(slots)]
    shapes = [(read_string(stream),array(count)) for _ in range(shape_count)]
    native = array(integer())
    native_triangles = array(integer()//3,3,'<i4')

def boundary_loops(vertices, faces):
    _, first, inverse = np.unique(np.round(vertices,6),axis=0,return_index=True,return_inverse=True)
    edges = Counter(tuple(sorted((int(inverse[a]),int(inverse[b])))) for tri in faces for a,b in zip(tri,np.roll(tri,-1)))
    links = defaultdict(set)
    for (a,b),count in edges.items():
        if count == 1: links[a].add(b); links[b].add(a)
    seen=set(); loops=[]
    for start in links:
        if start in seen: continue
        todo=[start]; loop=[]
        while todo:
            vertex=todo.pop()
            if vertex in seen: continue
            seen.add(vertex);loop.append(first[vertex]);todo.extend(links[vertex]-seen)
        loops.append(np.array(loop))
    return loops

if __name__=='__main__':
    for label,v,t in [('source',source,triangles[0]),('native',native,native_triangles)]:
        print(label)
        for loop in sorted(boundary_loops(v,t),key=lambda a:v[a,2].min()):
            print(len(loop),v[loop].min(0),v[loop].max(0),loop.tolist() if v[loop,2].max()<1.36 else '')
