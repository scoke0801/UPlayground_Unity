import sys
import numpy as np

N = 4097
h = np.fromfile(sys.argv[1], dtype='<u2').reshape(N, N).astype(np.float32) / 65535.0 * 800.0 - 32.0

def break_stats(name, x0, x1, z0, z1):
    sub = h[z0:z1, x0:x1]
    # 8m 스팬 낙차(남북/동서)
    dx = np.abs(sub[:, 8:] - sub[:, :-8])[:-8, :]
    dz = np.abs(sub[8:, :] - sub[:-8, :])[:, :-8]
    m = min(dx.shape[0], dz.shape[0]), min(dx.shape[1], dz.shape[1])
    d = np.maximum(dx[:m[0], :m[1]], dz[:m[0], :m[1]])
    B = 128
    hh, ww = d.shape[0] // B, d.shape[1] // B
    blocks = d[:hh * B, :ww * B].reshape(hh, B, ww, B).max(axis=(1, 3))
    tot = blocks.size
    print(f"{name:14s} 128m블록 {tot:3d}개 | 8m낙차 최대≥3m(≈20°) {np.mean(blocks>=3)*100:5.1f}% | ≥6m(≈37°) {np.mean(blocks>=6)*100:5.1f}% | ≥10m(≈51°) {np.mean(blocks>=10)*100:5.1f}%")

break_stats("호수마을→B3 평지", 1650, 2400, 950, 1650)
break_stats("중앙평지 전체", 1700, 3000, 1000, 2700)
break_stats("호수마을~성채", 2000, 2800, 900, 2300)
break_stats("B3협곡 회랑", 1200, 1760, 1200, 1760)
break_stats("서부숲", 440, 1450, 1600, 2900)
break_stats("북부산맥", 300, 4000, 3300, 4090)

# B3 협곡(남서 지류) 회랑의 횡단면 4곳 (중심선에서 -100..+100m, 5m 간격 근사: 지류 접선의 법선 방향)
pts = [(1808, 1796), (1722, 1728), (1672, 1674), (1622, 1620), (1559, 1580), (1496, 1540), (1449, 1479), (1402, 1418), (1350, 1371), (1298, 1323), (1246, 1276), (1201, 1208)]
print("\n남서 지류 횡단면(중심 법선, 20m 간격 -100~+100)")
for i in (2, 3, 4, 6, 8, 10):
    p = np.array(pts[i], float); q = np.array(pts[i + 1], float); t = (q - p) / np.linalg.norm(q - p); n = np.array([-t[1], t[0]])
    row = []
    for d in range(-100, 101, 20):
        x, z = p + n * d
        row.append(f"{h[int(z), int(x)]:5.1f}")
    print(pts[i], " ".join(row))
