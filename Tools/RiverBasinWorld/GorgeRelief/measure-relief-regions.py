import sys
import numpy as np

raw, = sys.argv[1:2]
N = 4097
h = np.fromfile(raw, dtype='<u2').reshape(N, N).astype(np.float32) / 65535.0 * 800.0 - 32.0
# h[z, x], z=0 south
print("height min/max", h.min(), h.max())

# 4m 격자로 줄여 경사/국소기복 계산
s = h[::4, ::4]
gz, gx = np.gradient(s, 4.0)
slope = np.degrees(np.arctan(np.hypot(gx, gz)))

# 국소 기복: 128m 창(32셀)의 max-min
def window_relief(a, w):
    H, W = a.shape
    hh, ww = H // w, W // w
    b = a[:hh * w, :ww * w].reshape(hh, w, ww, w)
    return b.max(axis=(1, 3)) - b.min(axis=(1, 3)), b.mean(axis=(1, 3))

rel, mean = window_relief(s, 32)  # 128m 블록
print("blocks", rel.shape)

def region(name, x0, x1, z0, z1):
    sl = slope[z0 // 4:z1 // 4, x0 // 4:x1 // 4]
    r, _ = window_relief(s[z0 // 4:z1 // 4, x0 // 4:x1 // 4], 32)
    print(f"{name:12s} slope p50={np.percentile(sl,50):5.1f} p90={np.percentile(sl,90):5.1f} p99={np.percentile(sl,99):5.1f} "
          f"| 128m relief p10={np.percentile(r,10):5.1f} p50={np.percentile(r,50):5.1f} p90={np.percentile(r,90):5.1f} "
          f"| <=3deg={np.mean(sl<=3)*100:4.1f}% <=6deg={np.mean(sl<=6)*100:4.1f}% >=25deg={np.mean(sl>=25)*100:4.1f}%")

region("전체", 0, 4096, 0, 4096)
region("중앙평지", 1700, 3000, 1000, 2700)
region("호수마을~성채", 2000, 2800, 900, 2300)
region("서부숲", 440, 1450, 1600, 2900)
region("남서호수주변", 500, 1700, 400, 1500)
region("동부사막", 3300, 4096, 1300, 2700)
region("북부산맥", 300, 4096, 3300, 4096)

# 중앙 평지에서 물 근처(수면±) 제외한 순수 육지 128m 블록 기복 분포
x0, x1, z0, z1 = 1700, 3000, 1000, 2700
r, m = window_relief(s[z0 // 4:z1 // 4, x0 // 4:x1 // 4], 32)
print("중앙평지 128m 블록 기복 히스토그램 (m):")
edges = [0, 2, 4, 6, 10, 15, 25, 50, 500]
cnt, _ = np.histogram(r.ravel(), bins=edges)
for a, b, c in zip(edges[:-1], edges[1:], cnt):
    print(f"  {a:>3}-{b:<3}m: {c} ({c / r.size * 100:4.1f}%)")

# 수직 단차(절벽성) 존재: 전체에서 12m 이상 낙차가 8m 안에 있는 셀 비율
big = np.abs(s[:, 2:] - s[:, :-2]) / 8.0
print("전체에서 경사 >=60deg 셀 비율 (4m 격자):", np.mean(np.degrees(np.arctan(big)) >= 60) * 100, "%")
