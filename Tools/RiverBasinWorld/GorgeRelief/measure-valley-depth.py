import json, sys
import numpy as np

plan = json.load(open(sys.argv[1], encoding='utf-8'))
N = 4097
h = np.fromfile(sys.argv[2], dtype='<u2').reshape(N, N).astype(np.float32) / 65535.0 * 800.0 - 32.0

def Y(x, z):
    x = min(max(x, 0), 4095); z = min(max(z, 0), 4095)
    return h[int(z), int(x)]

def resample(pts, step=60):
    out = []
    for i in range(len(pts) - 1):
        a = np.array(pts[i][:2], float); b = np.array(pts[i+1][:2], float)
        L = np.linalg.norm(b - a)
        n = max(1, int(L // step))
        for k in range(n):
            out.append(a + (b - a) * k / n)
    out.append(np.array(pts[-1][:2], float))
    return out

for wc in plan['watercourses']:
    if wc['id'] not in ('inlet', 'southwest-inlet', 'forest-creek', 'forest-spring'):
        continue
    P = resample(wc['points'])
    print(f"== {wc['id']} (samples every ~60m): 위치 | 하상Y | 좌안+80m | 우안+80m | 좌안+160m | 우안+160m | 깊이(양안평균-하상)")
    for i, p in enumerate(P):
        if i + 1 < len(P):
            t = P[i + 1] - p
        else:
            t = p - P[i - 1]
        t = t / (np.linalg.norm(t) + 1e-6)
        nrm = np.array([-t[1], t[0]])
        c = Y(*p)
        def s(d):
            q = p + nrm * d
            return Y(*q)
        L80, R80, L160, R160 = s(80), s(-80), s(160), s(-160)
        depth = (L80 + R80) / 2 - c
        print(f"({p[0]:5.0f},{p[1]:5.0f}) bed={c:6.1f} L80={L80:6.1f} R80={R80:6.1f} L160={L160:6.1f} R160={R160:6.1f} depth80={depth:5.1f}")
