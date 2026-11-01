"""생명의 호수 신사 키트 생성 (헤드리스 Blender). 정면은 -Y, 원점은 지면 중심.
사용: blender -b -P build_shrine.py -- <출력폴더> [미리보기폴더]
"""
import bpy, bmesh, math, random, sys
from mathutils import Matrix, Vector

argv = sys.argv[sys.argv.index("--") + 1:]
OUT = argv[0]
PREVIEW = argv[1] if len(argv) > 1 else None

MAT_COLORS = {
    "Shrine_Lacquer": (0.72, 0.10, 0.05),
    "Shrine_Kawara": (0.28, 0.32, 0.36),
    "Shrine_Granite": (0.52, 0.52, 0.50),
    "Shrine_Cypress": (0.76, 0.63, 0.43),
    "Shrine_Rope": (0.80, 0.70, 0.40),
    "Shrine_Paper": (0.95, 0.95, 0.92),
}
MAT_ORDER = list(MAT_COLORS)
LAC, KAW, GRA, CYP, ROPE, PAP = range(6)
TILE = [1.2, 1.0, 1.5, 1.2, 0.5, 1.0]


def get_materials():
    mats = []
    for name, col in MAT_COLORS.items():
        m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
        m.diffuse_color = (*col, 1)
        mats.append(m)
    return mats


class B:
    """bmesh 빌더: 재질 인덱스를 면에 기록하고 마지막에 평면 투영 UV를 입힌다."""

    def __init__(self):
        self.bm = bmesh.new()
        self.custom_uv = set()

    def _tag(self, start, mat):
        # ops로 만든 면은 룩업 테이블이 낡을 수 있어 순회 순서(생성 순서)로 접근한다
        for f in list(self.bm.faces)[start:]:
            f.material_index = mat

    def box(self, mat, c, s, rz=0.0, rot=None):
        n = len(self.bm.faces)
        m = Matrix.Translation(c)
        if rot:
            m = m @ Matrix.Rotation(rot[2], 4, "Z") @ Matrix.Rotation(rot[1], 4, "Y") @ Matrix.Rotation(rot[0], 4, "X")
        elif rz:
            m = m @ Matrix.Rotation(rz, 4, "Z")
        m = m @ Matrix.Diagonal((s[0], s[1], s[2], 1))
        bmesh.ops.create_cube(self.bm, size=1.0, matrix=m)
        self._tag(n, mat)

    def box_b(self, mat, cx, cy, z0, sx, sy, sz, rz=0.0):
        """바닥 z0 기준 박스."""
        self.box(mat, Vector((cx, cy, z0 + sz / 2)), (sx, sy, sz), rz)

    def cyl(self, mat, c, r1, r2, h, segs=12, rot=None):
        """c는 바닥 중심. 축은 +Z."""
        n = len(self.bm.faces)
        m = Matrix.Translation(Vector(c) + Vector((0, 0, h / 2)))
        if rot:
            m = Matrix.Translation(c) @ Matrix.Rotation(rot[2], 4, "Z") @ Matrix.Rotation(rot[1], 4, "Y") @ Matrix.Rotation(rot[0], 4, "X") @ Matrix.Translation((0, 0, h / 2))
        bmesh.ops.create_cone(self.bm, cap_ends=True, cap_tris=False, segments=segs, radius1=r1, radius2=r2, depth=h, matrix=m)
        self._tag(n, mat)

    def sphere(self, mat, c, s, segs=10, rings=6):
        n = len(self.bm.faces)
        m = Matrix.Translation(c) @ Matrix.Diagonal((s[0], s[1], s[2], 1))
        bmesh.ops.create_uvsphere(self.bm, u_segments=segs, v_segments=rings, radius=1.0, matrix=m)
        self._tag(n, mat)

    def poly(self, mat, pts):
        n = len(self.bm.faces)
        vs = [self.bm.verts.new(Vector(p)) for p in pts]
        try:
            self.bm.faces.new(vs)
        except ValueError:
            return
        self._tag(n, mat)

    def thick(self, mat, polys, t):
        """윗면 폴리곤 목록을 아래로 t만큼 두께를 준 닫힌 솔리드로 만든다."""
        tops = []
        for p in polys:
            nx = ny = nz = 0.0
            for i in range(len(p)):
                a, b = p[i], p[(i + 1) % len(p)]
                nx += (a[1] - b[1]) * (a[2] + b[2])
                ny += (a[2] - b[2]) * (a[0] + b[0])
                nz += (a[0] - b[0]) * (a[1] + b[1])
            tops.append(list(p) if nz >= 0 else list(reversed(p)))
        edges = {}
        key = lambda v: (round(v[0], 4), round(v[1], 4), round(v[2], 4))
        for p in tops:
            for i in range(len(p)):
                edges[(key(p[i]), key(p[(i + 1) % len(p)]))] = (p[i], p[(i + 1) % len(p)])
        for p in tops:
            self.poly(mat, p)
            self.poly(mat, [(v[0], v[1], v[2] - t) for v in reversed(p)])
        for (ka, kb), (a, b) in edges.items():
            if (kb, ka) in edges:
                continue
            ad = (a[0], a[1], a[2] - t)
            bd = (b[0], b[1], b[2] - t)
            self.poly(mat, [a, ad, bd, b])

    def tube(self, mat, path, radii, segs=8, uv_len=0.5):
        """경로를 따라가는 관. UV는 직접 계산(밧줄 텍스처용)."""
        bm = self.bm
        rings = []
        up = Vector((0, 0, 1))
        for i, p in enumerate(path):
            p = Vector(p)
            tan = (Vector(path[min(i + 1, len(path) - 1)]) - Vector(path[max(i - 1, 0)])).normalized()
            side = tan.cross(up)
            if side.length < 1e-4:
                side = Vector((1, 0, 0))
            side.normalize()
            nrm = side.cross(tan).normalized()
            ring = []
            for j in range(segs):
                a = 2 * math.pi * j / segs
                ring.append(bm.verts.new(p + (side * math.cos(a) + nrm * math.sin(a)) * radii[i]))
            rings.append(ring)
        lens = [0.0]
        for i in range(1, len(path)):
            lens.append(lens[-1] + (Vector(path[i]) - Vector(path[i - 1])).length)
        uvl = bm.loops.layers.uv.verify()
        for i in range(len(rings) - 1):
            for j in range(segs):
                j2 = (j + 1) % segs
                vs = [rings[i][j], rings[i][j2], rings[i + 1][j2], rings[i + 1][j]]
                try:
                    f = bm.faces.new(vs)
                except ValueError:
                    continue
                f.material_index = mat
                us = [lens[i], lens[i], lens[i + 1], lens[i + 1]]
                vv = [j / segs, (j + 1) / segs, (j + 1) / segs, j / segs]
                for l, u, v in zip(f.loops, us, vv):
                    l[uvl].uv = (u / uv_len, v * 2.0)
                self.custom_uv.add(f)

    def loft_beam(self, mat, x0, x1, y, w, h, zfunc, n=14):
        """x축 방향 보. 중심선 높이를 zfunc(x)로 올려 처마 곡선을 만든다."""
        xs = [x0 + (x1 - x0) * i / n for i in range(n + 1)]
        def corners(x):
            z = zfunc(x)
            return [(x, y - w / 2, z - h / 2), (x, y + w / 2, z - h / 2), (x, y + w / 2, z + h / 2), (x, y - w / 2, z + h / 2)]
        for i in range(n):
            a, b = corners(xs[i]), corners(xs[i + 1])
            for k in range(4):
                k2 = (k + 1) % 4
                self.poly(mat, [a[k], a[k2], b[k2], b[k]])
        self.poly(mat, list(reversed(corners(xs[0]))))
        self.poly(mat, corners(xs[-1]))

    def finish(self, name, mats):
        """평면 투영 UV 부여 후 오브젝트화. 엣지 스플릿으로 각진 모서리를 유지한다."""
        bm = self.bm
        # 면 방향은 각 빌더가 직접 보장한다(분리된 면에는 recalc가 방향을 임의로 잡는다)
        bm.faces.ensure_lookup_table()
        uvl = bm.loops.layers.uv.verify()
        for f in bm.faces:
            if f in self.custom_uv:
                continue
            n = f.normal
            up = Vector((0, 0, 1))
            tu = up.cross(n)
            if tu.length < 0.05:
                tu = Vector((1, 0, 0))
            tu.normalize()
            tv = n.cross(tu).normalized()
            tile = TILE[f.material_index]
            for l in f.loops:
                p = l.vert.co
                l[uvl].uv = (p.dot(tu) / tile, p.dot(tv) / tile)
        mesh = bpy.data.meshes.new(name)
        bm.to_mesh(mesh)
        bm.free()
        for m in mats:
            mesh.materials.append(m)
        for p in mesh.polygons:
            p.use_smooth = True
        obj = bpy.data.objects.new(name, mesh)
        bpy.context.scene.collection.objects.link(obj)
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        mod = obj.modifiers.new("es", "EDGE_SPLIT")
        mod.use_edge_angle = True
        mod.split_angle = math.radians(38)
        bpy.ops.object.modifier_apply(modifier="es")
        obj.select_set(False)
        return obj


# ---------------------------------------------------------------- 공용 부품

def hip_roof(b, mat, z0, w, d, rise, o, t, lift=0.22, ridge_ratio=0.5):
    """모임지붕 + 처마 끝 들림."""
    hx, hy = w / 2 + o, d / 2 + o
    rl = hx * ridge_ratio
    zr = z0 + rise
    rL, rR = (-rl, 0, zr), (rl, 0, zr)
    ts = [-1, -0.75, -0.5, -0.25, 0, 0.25, 0.5, 0.75, 1]
    lz = lambda t: z0 + lift * abs(t) ** 2.2
    front = [(hx * t, -hy, lz(t)) for t in ts]
    back = [(-hx * t, hy, lz(t)) for t in ts]
    right = [(hx, hy * t, lz(t)) for t in ts]
    left = [(-hx, -hy * t, lz(t)) for t in ts]
    polys = [
        front + [rR, rL],
        back + [rL, rR],
        right[:5] + [rR], right[4:] + [rR],
        left[:5] + [rL], left[4:] + [rL],
    ]
    b.thick(mat, polys, t)
    # 용마루 + 치미 + 가쓰오기
    b.box(mat, Vector((0, 0, zr + 0.05)), (rl * 2 + 0.3, 0.34, 0.22))
    for sx in (-1, 1):
        b.box(mat, Vector((sx * (rl + 0.18), 0, zr + 0.2)), (0.28, 0.4, 0.48))
    return rl, zr


def gable_roof(b, mat, z0, w, d, rise, o, t, ridge_cap=True):
    hx, hy = w / 2 + o, d / 2 + o
    polys = [
        [(-hx, -hy, z0), (hx, -hy, z0), (hx, 0, z0 + rise), (-hx, 0, z0 + rise)],
        [(hx, hy, z0), (-hx, hy, z0), (-hx, 0, z0 + rise), (hx, 0, z0 + rise)],
    ]
    b.thick(mat, polys, t)
    if ridge_cap:
        b.box(mat, Vector((0, 0, z0 + rise + 0.03)), (hx * 2 + 0.12, 0.26, 0.16))


def pillar(b, mat, x, y, z0, h, r=0.14, base=True):
    if base:
        b.cyl(GRA, (x, y, z0), r * 1.5, r * 1.3, 0.12, 10)
        z0 += 0.12
        h -= 0.12
    b.cyl(mat, (x, y, z0), r, r * 0.92, h, 12)


# ---------------------------------------------------------------- 오브젝트

def build_main():
    b = B()
    # 기단 (2단) + 정면 계단
    b.box_b(GRA, 0, 0, 0, 9.6, 7.6, 0.35)
    b.box_b(GRA, 0, 0, 0.35, 9.0, 7.0, 0.35)
    zf = 0.7
    for i in range(3):
        b.box_b(GRA, 0, -3.5 - 0.3 * i - 0.15, 0, 3.0, 0.3, 0.7 - 0.2333 * i)
    # 본당 바닥(툇마루 데크)
    b.box_b(CYP, 0, -0.3, zf, 8.0, 6.4, 0.12)
    zb = zf + 0.12
    H = 3.0
    hx, hy = 3.0, 2.4
    # 기둥: 앞뒤 4열 + 측면 중간
    for x in (-3.0, -1.0, 1.0, 3.0):
        pillar(b, LAC, x, -hy, zb, H)
        pillar(b, LAC, x, hy, zb, H)
    for y in (-0.8, 0.8):
        pillar(b, LAC, -3.0, y, zb, H)
        pillar(b, LAC, 3.0, y, zb, H)
    # 벽: 뒤·측면 (노송 판재), 앞면 바깥 칸
    wall_h = H - 0.5
    b.box_b(CYP, 0, hy, zb + 0.2, 6.0, 0.14, wall_h)
    b.box_b(CYP, -3.0, 0, zb + 0.2, 0.14, 4.8, wall_h)
    b.box_b(CYP, 3.0, 0, zb + 0.2, 0.14, 4.8, wall_h)
    for sx in (-2.0, 2.0):
        b.box_b(CYP, sx, -hy, zb + 0.2, 1.8, 0.1, wall_h)
        # 격자창 느낌의 가로 살대
        for k in range(4):
            b.box_b(LAC, sx, -hy - 0.07, zb + 0.9 + k * 0.5, 1.8, 0.05, 0.06)
    # 정면 중앙 칸: 열린 입구 + 안쪽 어두운 문, 새전함, 방울줄
    b.box_b(LAC, 0, hy - 0.2, zb + 0.2, 1.8, 0.1, wall_h)
    b.box_b(CYP, 0, -hy + 0.5, zb, 1.5, 0.55, 0.5)
    for k in range(7):
        b.box_b(LAC, -0.6 + k * 0.2, -hy + 0.5, zb + 0.5, 0.06, 0.6, 0.04)
    b.cyl(ROPE, (0, -hy + 1.2, zb + 1.2), 0.07, 0.05, 1.5, 6)
    b.sphere(ROPE, Vector((0, -hy + 1.2, zb + 2.75)), (0.16, 0.16, 0.16), 8, 5)
    # 보 (누키/가사기 하부 보)
    top = zb + H
    for y in (-hy, hy):
        b.box_b(LAC, 0, y, top - 0.18, 6.4, 0.22, 0.28)
    for x in (-3.0, 3.0):
        b.box_b(LAC, x, 0, top - 0.18, 0.22, 5.0, 0.28)
    b.box_b(LAC, 0, 0, top - 0.5, 6.2, 0.14, 0.14)
    # 난간(툇마루)
    rail_z = zb
    for x in (-3.9, 3.9):
        b.box_b(LAC, x, -0.3, rail_z + 0.62, 0.1, 6.4, 0.1)
        b.box_b(LAC, x, -0.3, rail_z + 0.25, 0.08, 6.4, 0.08)
    for y in (3.0,):
        b.box_b(LAC, 0, y, rail_z + 0.62, 8.0, 0.1, 0.1)
        b.box_b(LAC, 0, y, rail_z + 0.25, 8.0, 0.08, 0.08)
    for x in (-3.9, -2.0, 0.0, 2.0, 3.9):
        b.box_b(LAC, x, 3.0, rail_z, 0.14, 0.14, 0.75)
    for y in (-3.5, -1.8, 0.0, 1.6, 3.0):
        b.box_b(LAC, -3.9, y, rail_z, 0.14, 0.14, 0.75)
        b.box_b(LAC, 3.9, y, rail_z, 0.14, 0.14, 0.75)
    # 지붕
    hip_roof(b, KAW, top, 6.4, 5.2, 2.8, 1.5, 0.22, lift=0.5, ridge_ratio=0.4)
    # 용마루 가쓰오기
    for x in (-1.2, -0.4, 0.4, 1.2):
        b.cyl(CYP, (x, -0.4, top + 2.8 + 0.18), 0.07, 0.07, 0.8, 6, rot=(-math.pi / 2, 0, 0))
    # 박공 쪽 지단(치기): 지붕 정면 중앙 십자 장식 대신 현판
    b.box_b(CYP, 0, -hy - 0.35, top + 0.15, 1.2, 0.08, 0.5)
    return b


def build_torii():
    b = B()
    sp, H = 5.0, 5.2
    for sx in (-1, 1):
        b.box_b(GRA, sx * sp / 2, 0, 0, 0.9, 0.9, 0.35)
        b.cyl(LAC, (sx * sp / 2, 0, 0.35), 0.28, 0.22, H - 0.35, 14)
    # 누키(가로보)
    b.box_b(LAC, 0, 0, 3.7, sp + 0.9, 0.2, 0.3)
    b.box_b(LAC, 0, 0, 3.4, 0.2, 0.12, 0.3)  # 세로 지지
    # 시마키 + 가사기(곡선)
    b.box_b(LAC, 0, 0, H - 0.05, sp + 1.2, 0.34, 0.3)
    L = sp + 2.2
    b.loft_beam(KAW, -L / 2, L / 2, 0, 0.62, 0.34, lambda x: H + 0.45 + 0.5 * (abs(x) / (L / 2)) ** 2)
    # 현판
    b.box_b(CYP, 0, -0.04, 4.0, 0.8, 0.06, 1.0)
    return b


def build_lantern():
    b = B()
    b.box_b(GRA, 0, 0, 0, 0.9, 0.9, 0.18)
    b.cyl(GRA, (0, 0, 0.18), 0.28, 0.22, 0.12, 8)
    b.cyl(GRA, (0, 0, 0.30), 0.14, 0.12, 0.85, 8)
    b.cyl(GRA, (0, 0, 1.15), 0.34, 0.30, 0.14, 6)
    # 화창(火袋)
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.box_b(GRA, sx * 0.17, sy * 0.17, 1.29, 0.1, 0.1, 0.4)
    b.box_b(KAW, 0, 0, 1.31, 0.3, 0.3, 0.36)
    for s in (-1, 1):
        b.box_b(PAP, s * 0.17, 0, 1.36, 0.02, 0.22, 0.26)
        b.box_b(PAP, 0, s * 0.17, 1.36, 0.22, 0.02, 0.26)
    b.cyl(GRA, (0, 0, 1.69), 0.2, 0.2, 0.08, 6)
    b.cyl(GRA, (0, 0, 1.77), 0.58, 0.12, 0.34, 6)
    b.sphere(GRA, Vector((0, 0, 2.2)), (0.14, 0.14, 0.2), 8, 6)
    return b


def build_komainu():
    b = B()
    b.box_b(GRA, 0, 0, 0, 1.0, 1.4, 0.4)
    b.box_b(GRA, 0, 0, 0.4, 0.82, 1.22, 0.1)
    z = 0.5
    b.sphere(GRA, Vector((0, 0.2, z + 0.38)), (0.3, 0.42, 0.36), 10, 7)        # 엉덩이
    b.sphere(GRA, Vector((0, -0.12, z + 0.52)), (0.28, 0.32, 0.4), 10, 7)      # 가슴
    for sx in (-1, 1):
        b.box_b(GRA, sx * 0.14, -0.3, z, 0.14, 0.16, 0.5)                       # 앞다리
        b.box_b(GRA, sx * 0.14, -0.38, z, 0.17, 0.12, 0.08)
        b.sphere(GRA, Vector((sx * 0.25, 0.3, z + 0.2)), (0.12, 0.26, 0.2), 8, 5)  # 뒷다리
    b.sphere(GRA, Vector((0, -0.28, z + 0.88)), (0.26, 0.26, 0.24), 10, 7)      # 머리
    b.box_b(GRA, 0, -0.5, z + 0.74, 0.3, 0.2, 0.1)                              # 아래턱
    b.box_b(GRA, 0, -0.52, z + 0.84, 0.3, 0.22, 0.09)                           # 윗턱
    for sx in (-1, 1):
        b.cyl(GRA, (sx * 0.2, -0.2, z + 1.05), 0.06, 0.01, 0.16, 5)             # 귀
        b.sphere(GRA, Vector((sx * 0.17, -0.45, z + 0.98)), (0.06, 0.05, 0.05), 6, 4)  # 눈두덩
    for i in range(7):                                                          # 갈기
        a = -0.9 + i * 0.3
        b.cyl(GRA, (math.sin(a) * 0.26, -0.28 + 0.1, z + 0.86 + math.cos(a) * 0.2), 0.07, 0.01, 0.2, 5,
              rot=(0.5, 0, math.sin(a) * 0.5))
    b.sphere(GRA, Vector((0, 0.5, z + 0.5)), (0.12, 0.14, 0.34), 8, 6)          # 꼬리
    b.sphere(GRA, Vector((0, 0.55, z + 0.82)), (0.15, 0.12, 0.12), 8, 5)
    return b


def build_temizuya():
    b = B()
    b.box_b(GRA, 0, 0, 0, 3.4, 2.8, 0.2)
    for sx in (-1, 1):
        for sy in (-1, 1):
            pillar(b, LAC, sx * 1.4, sy * 1.1, 0.2, 2.6, r=0.11, base=True)
    for y in (-1.1, 1.1):
        b.box_b(LAC, 0, y, 2.55, 3.0, 0.14, 0.2)
    for x in (-1.4, 1.4):
        b.box_b(LAC, x, 0, 2.55, 0.14, 2.4, 0.2)
    gable_roof(b, KAW, 2.75, 3.0, 2.4, 0.95, 0.55, 0.14)
    # 수반
    b.box_b(GRA, 0, 0, 0.2, 1.6, 0.9, 0.7)
    b.box_b(GRA, 0, 0, 0.9, 1.7, 1.0, 0.06)
    # 국자 가로대
    b.box_b(CYP, 0, -1.1, 1.0, 2.6, 0.06, 0.06)
    for x in (-0.5, 0.5):
        b.cyl(CYP, (x, -1.1, 1.05), 0.03, 0.03, 0.5, 6, rot=(-math.radians(70), 0, 0))
    return b


def build_ema():
    b = B()
    for sx in (-1, 1):
        b.cyl(LAC, (sx * 1.9, 0, 0), 0.1, 0.1, 2.5, 10)
    b.box_b(LAC, 0, 0, 2.1, 3.9, 0.14, 0.18)
    b.box_b(LAC, 0, 0, 0.3, 3.9, 0.14, 0.14)
    gable_roof(b, KAW, 2.4, 4.1, 1.0, 0.45, 0.2, 0.1)
    random.seed(7)
    for row in range(3):
        for i in range(10):
            x = -1.6 + i * 0.355
            z = 0.6 + row * 0.5
            tilt = random.uniform(-0.08, 0.08)
            b.box(CYP, Vector((x, -0.12, z + 0.2)), (0.24, 0.025, 0.3), rot=(0, tilt, 0))
        b.box_b(LAC, 0, -0.1, 0.4 + row * 0.5, 3.8, 0.06, 0.04)
    return b


def build_saisen():
    b = B()
    b.box_b(GRA, 0, 0, 0, 1.4, 1.0, 0.18)
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.cyl(LAC, (sx * 0.58, sy * 0.38, 0.18), 0.05, 0.05, 0.85, 8)
    b.box_b(CYP, 0, 0, 0.55, 1.2, 0.8, 0.5)
    for k in range(9):
        b.box_b(LAC, -0.5 + k * 0.125, 0, 1.05, 0.05, 0.84, 0.04)
    b.box_b(LAC, 0, 0, 1.0, 1.26, 0.86, 0.06)
    gable_roof(b, KAW, 1.2, 1.5, 1.1, 0.5, 0.2, 0.08)
    b.cyl(ROPE, (0, -0.55, 0.9), 0.04, 0.03, 0.9, 6)
    return b


def build_hokora():
    b = B()
    b.box_b(GRA, 0, 0, 0, 2.0, 1.8, 0.3)
    b.box_b(GRA, 0, -1.1, 0, 1.0, 0.4, 0.15)
    b.box_b(CYP, 0, 0, 0.3, 1.3, 1.1, 0.14)
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.cyl(LAC, (sx * 0.58, sy * 0.46, 0.44), 0.05, 0.05, 1.1, 8)
    b.box_b(CYP, 0, 0.5, 0.44, 1.2, 0.08, 1.0)
    for s in (-1, 1):
        b.box_b(CYP, s * 0.6, 0, 0.44, 0.08, 1.0, 1.0)
    b.box_b(LAC, 0, -0.46, 0.55, 0.9, 0.05, 0.85)
    b.box_b(LAC, 0, -0.5, 0.95, 0.04, 0.03, 0.85)
    b.box_b(LAC, 0, 0, 1.54, 1.4, 1.2, 0.1)
    gable_roof(b, KAW, 1.62, 1.3, 1.1, 0.55, 0.3, 0.1)
    for sx in (-1, 1):
        b.box(CYP, Vector((sx * 0.5, -0.0, 2.27)), (0.04, 0.04, 0.4), rot=(0, sx * 0.6, 0))
    return b


def build_fence():
    b = B()
    L = 4.0
    for i in range(5):
        x = -L / 2 + i * L / 4
        b.box_b(LAC, x, 0, 0, 0.14, 0.14, 1.5)
        b.cyl(LAC, (x, 0, 1.5), 0.075, 0.06, 0.1, 8)
    for z in (0.35, 1.2):
        b.box_b(LAC, 0, 0, z, L, 0.1, 0.1)
    n = 24
    for i in range(n):
        x = -L / 2 + 0.14 + (L - 0.28) * (i + 0.5) / n
        b.box_b(CYP, x, 0, 0.12, 0.07, 0.05, 1.28)
    return b


def build_path():
    b = B()
    random.seed(21)
    xs = [-0.55, 0.55]
    n = 5
    for r in range(n):
        for c, cx in enumerate(xs):
            y = -2.0 + 4.0 * (r + 0.5) / n
            w, d = 0.95 + random.uniform(-0.04, 0.04), 0.7 + random.uniform(-0.05, 0.05)
            h = 0.12 + random.uniform(0.0, 0.02)
            ox = cx + random.uniform(-0.04, 0.04) + (0.0 if r % 2 == 0 else (0.18 if c == 0 else -0.18))
            b.box_b(GRA, ox, y, 0, w, d, h, rz=random.uniform(-0.04, 0.04))
    for s in (-1, 1):
        for r in range(8):
            y = -1.9 + r * 0.54
            b.box_b(GRA, s * 1.18, y, 0, 0.26, 0.4, 0.16 + random.uniform(0, 0.03), rz=random.uniform(-0.1, 0.1))
    return b


def build_shimenawa():
    b = B()
    R = 1.4
    a0, a1 = math.radians(25), math.radians(335)
    N = 40
    path, radii = [], []
    for i in range(N + 1):
        t = i / N
        a = a0 + (a1 - a0) * t
        z = 0.12 * math.sin(t * math.pi * 3) * 0.5
        path.append((R * math.cos(a - math.pi / 2), R * math.sin(a - math.pi / 2), z))
        radii.append(0.04 + 0.075 * math.sin(math.pi * t) ** 0.5)
    b.tube(ROPE, path, radii, segs=8, uv_len=0.5)
    # 양 끝 술
    for end in (0, N):
        px, py, pz = path[end]
        for k in range(5):
            b.cyl(ROPE, (px + (k - 2) * 0.03, py, pz - 0.28), 0.014, 0.004, 0.28, 4)
    # 종이 시데 (지그재그)
    for t in (0.2, 0.4, 0.6, 0.8):
        idx = int(t * N)
        px, py, pz = path[idx]
        a = a0 + (a1 - a0) * t - math.pi / 2
        tx, ty = -math.sin(a), math.cos(a)
        z = pz - 0.04
        for sec in range(4):
            dz = 0.22
            z0, z1 = z - sec * dz, z - (sec + 1) * dz
            off0 = 0.07 * (1 if sec % 2 == 0 else -1)
            off1 = -off0
            wd = 0.11
            nxo, nyo = math.cos(a), math.sin(a)
            pts = [
                (px + nxo * off0 - tx * wd, py + nyo * off0 - ty * wd, z0),
                (px + nxo * off0 + tx * wd, py + nyo * off0 + ty * wd, z0),
                (px + nxo * off1 + tx * wd, py + nyo * off1 + ty * wd, z1),
                (px + nxo * off1 - tx * wd, py + nyo * off1 - ty * wd, z1),
            ]
            b.poly(PAP, pts)
            b.poly(PAP, list(reversed(pts)))
    return b


def build_nobori():
    b = B()
    b.box_b(GRA, 0, 0, 0, 0.8, 0.8, 0.22)
    b.cyl(CYP, (0, 0, 0.22), 0.07, 0.06, 3.6, 8)
    b.sphere(LAC, Vector((0, 0, 3.9)), (0.09, 0.09, 0.09), 8, 5)
    b.box_b(CYP, 0.45, 0, 3.5, 0.9, 0.05, 0.05)
    # 천 (살짝 물결)
    n = 8
    for i in range(n):
        z0 = 3.45 - i * 0.36
        z1 = z0 - 0.36
        y0 = 0.05 * math.sin(i * 0.9)
        y1 = 0.05 * math.sin((i + 1) * 0.9)
        pts = [(0.1, y0, z0), (0.85, y0, z0), (0.85, y1, z1), (0.1, y1, z1)]
        b.poly(LAC, pts)
        b.poly(LAC, list(reversed(pts)))
    return b


OBJECTS = [
    ("Shrine_Main", build_main), ("Shrine_Torii", build_torii), ("Shrine_StoneLantern", build_lantern),
    ("Shrine_Komainu", build_komainu), ("Shrine_Temizuya", build_temizuya), ("Shrine_EmaRack", build_ema),
    ("Shrine_Saisenbako", build_saisen), ("Shrine_Hokora", build_hokora), ("Shrine_Fence", build_fence),
    ("Shrine_StonePath", build_path), ("Shrine_Shimenawa", build_shimenawa), ("Shrine_Nobori", build_nobori),
]


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    mats = get_materials()
    objs = []
    for name, fn in OBJECTS:
        o = fn().finish(name, mats)
        objs.append(o)
        bpy.ops.object.select_all(action="DESELECT")
        o.select_set(True)
        bpy.context.view_layer.objects.active = o
        bpy.ops.export_scene.fbx(
            filepath=f"{OUT}/{name}.fbx", use_selection=True, object_types={"MESH"},
            apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
            bake_space_transform=True, mesh_smooth_type="FACE", path_mode="AUTO", add_leaf_bones=False)
        dims = o.dimensions
        print(f"EXPORTED {name} dims=({dims.x:.2f},{dims.y:.2f},{dims.z:.2f}) tris={sum(len(p.vertices) - 2 for p in o.data.polygons)}")
    if PREVIEW:
        render_preview(objs)


def render_preview(objs):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    sh = scene.display.shading
    sh.light = "STUDIO"
    sh.color_type = "MATERIAL"
    sh.show_shadows = True
    sh.show_cavity = False
    scene.render.resolution_x, scene.render.resolution_y = 1400, 900
    scene.world = bpy.data.worlds.new("w")
    scene.world.color = (0.72, 0.82, 0.9)
    # 메인은 단독, 나머지는 한 줄 배치
    main = objs[0]
    others = objs[1:]
    x = -16
    for o in others:
        o.location = (x, -16, 0)
        x += 3.4
        if o.name.endswith("StonePath"):
            x += 0.5
    main.location = (0, 0, 0)
    cam_d = bpy.data.cameras.new("c")
    cam = bpy.data.objects.new("c", cam_d)
    scene.collection.objects.link(cam)
    scene.camera = cam

    def shot(loc, target, path, lens=35):
        cam_d.lens = lens
        cam.location = loc
        d = Vector(target) - Vector(loc)
        cam.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = path
        bpy.ops.render.render(write_still=True)

    shot((12, -17, 8), (0, 0, 2.2), f"{PREVIEW}/main.png", 32)
    shot((2, -42, 9), (2, -16, 1.2), f"{PREVIEW}/props.png", 36)


main()
