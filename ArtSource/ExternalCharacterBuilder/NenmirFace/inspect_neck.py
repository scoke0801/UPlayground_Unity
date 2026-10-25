from collections import Counter, defaultdict
"""Unity 정점 순서를 보존하여 Nenmir 기반 새 얼굴과 모든 표정을 조형한다."""
import bpy
import json
import struct
import numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT = Path(__file__).resolve().parent

def read_string(stream):
    size = shift = 0
    while True:
        byte = stream.read(1)[0]
        size |= (byte & 127) << shift
        if not byte & 128:
            break
        shift += 7
    return stream.read(size).decode('utf-8')

with (ROOT / 'source.bin').open('rb') as stream:
    def integer():
        return struct.unpack('<i', stream.read(4))[0]
    def array(count, width=3, dtype='<f4'):
        return np.frombuffer(stream.read(count * width * 4), dtype=dtype).copy().reshape(count, width)
    count, slots, shape_count = integer(), integer(), integer()
    source = array(count)
    uv = array(count, 2)
    triangles = [array(integer() // 3, 3, '<i4') for _ in range(slots)]
    shapes = [(read_string(stream), array(count)) for _ in range(shape_count)]
    native = array(integer())
    native_triangles = array(integer() // 3, 3, '<i4')

def boundary_loops(vertices, faces):
    _, first, inverse = np.unique(np.round(vertices, 6), axis=0, return_index=True, return_inverse=True)
    edges = Counter(tuple(sorted((int(inverse[a]), int(inverse[b]))))
                    for tri in faces for a, b in zip(tri, np.roll(tri, -1)))
    links = defaultdict(set)
    for (a, b), count in edges.items():
        if count == 1:
            links[a].add(b); links[b].add(a)
    seen = set(); loops = []
    for start in links:
        if start in seen:
            continue
        todo = [start]; loop = []
        while todo:
            vertex = todo.pop()
            if vertex in seen:
                continue
            seen.add(vertex); loop.append(first[vertex]); todo.extend(links[vertex]-seen)
        loops.append(np.array(loop))
    return loops



for loop in boundary_loops(source,triangles[0]):
 if len(loop)==20 and source[loop,1].max()>.04 and source[loop,2].max()<1.362:
  print('SOURCE RING',source[loop].tolist())
ids=np.unique(triangles[0]);p=source[ids]
for z in [1.335,1.345,1.355,1.365,1.375,1.385,1.395]:
 select=p[(abs(p[:,2]-z)<.004)&(p[:,1]>-.04)]
 print('SECTION',z,len(select),select.tolist()[:40])
