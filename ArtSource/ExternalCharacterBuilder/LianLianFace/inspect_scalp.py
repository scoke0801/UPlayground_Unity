from inspect_topology import *
loop=next(a for a in boundary_loops(source,triangles[0]) if len(a)==42 and source[a,2].max()>1.5)
print([(int(i),source[i].round(5).tolist(),uv[i].round(4).tolist()) for i in loop])
