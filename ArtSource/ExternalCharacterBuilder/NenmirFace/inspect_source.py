from collections import Counter, defaultdict
"""Unity 정점 순서를 보존하여 Lili 기반 새 얼굴과 모든 표정을 조형한다."""
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

for i,t in enumerate(triangles):
 print('SLOT',i,'bounds',source[np.unique(t)].min(0),source[np.unique(t)].max(0))
 for loop in boundary_loops(source,t): print('LOOP',len(loop),'bounds',source[loop].min(0),source[loop].max(0))
for loop in boundary_loops(native,native_triangles): print('NATIVE LOOP',len(loop),native[loop].min(0),native[loop].max(0))
for key,d in shapes:
 if key in ['eyeoption.small','eyeoption.all_scale','eyeoption.all_scale_vertical','vrc.Blink','eye.close_L']:
  ids=np.linalg.norm(d,axis=1)>1e-6
  print('SHAPE',key,ids.sum(),source[ids].min(0),source[ids].max(0),'delta',d[ids].min(0),d[ids].max(0))


