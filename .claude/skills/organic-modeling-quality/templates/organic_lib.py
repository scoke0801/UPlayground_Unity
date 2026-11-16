# 유기체(나무·고사목·바위·통나무·그루터기) 모델링 공용 함수.
# 대화형 Blender MCP(execute_blender_code)에 붙여 쓰거나 백그라운드 CLI로 실행한다.
#   blender -b --factory-startup -P organic_lib.py -- <출력폴더> [demo]
# demo 인자를 주면 나이브(증상 재현) vs 레시피 비교 렌더 + FBX 왕복 측정을 수행한다.
# Blender 5.2.2에서 검증(2026-11-15). 4.1 미만에서는 노멀 API가 다르다.
import bpy, bmesh, math, random, sys, os, json
from mathutils import Vector, noise


# ---------------------------------------------------------------- 공용
def link(ob, col=None):
    (col or bpy.context.scene.collection).objects.link(ob)
    return ob


def apply_all(ob):
    """모디파이어 스택(커브 포함)을 평가 결과 메시로 굳힌다. ops·선택 상태에 의존하지 않는다."""
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg), preserve_all_data_layers=True, depsgraph=dg)
    if ob.type != 'MESH':
        name, mw, coll, data = ob.name, ob.matrix_world.copy(), ob.users_collection[0], ob.data
        bpy.data.objects.remove(ob)
        if data.users == 0:
            bpy.data.curves.remove(data)
        new = link(bpy.data.objects.new(name, me), coll)
        new.matrix_world = mw
        return new
    old = ob.data
    ob.modifiers.clear()
    ob.data = me
    if old.users == 0:
        bpy.data.meshes.remove(old)
    return ob


def tex(name, kind, size, depth=2):
    t = bpy.data.textures.new(name, kind)
    if hasattr(t, "noise_scale"):
        t.noise_scale = size
    if hasattr(t, "noise_depth"):
        t.noise_depth = depth
    return t


def displace(ob, t, strength, mid=0.5):
    """촘촘한 메시(리메시 뒤)에만 쓴다. 성긴 링에 쓰면 각진 요철만 생긴다."""
    m = ob.modifiers.new("Disp_" + t.name, 'DISPLACE')
    m.texture = t
    m.strength = strength
    m.mid_level = mid
    m.texture_coords = 'LOCAL'
    return m


def unify(ob, voxel, smooth_iter=6):
    """부품 통합 + 접합부 필렛. 복셀 리메시는 UV를 지우므로 UV는 이 뒤에 편다."""
    rm = ob.modifiers.new("Remesh", 'REMESH')
    rm.mode = 'VOXEL'
    rm.voxel_size = voxel
    if smooth_iter:
        sm = ob.modifiers.new("Smooth", 'SMOOTH')
        sm.factor = 0.8
        sm.iterations = smooth_iter
    return apply_all(ob)


def cut_plane(ob, co, no):
    """평면 아래(no 반대쪽 아님, no 방향 바깥)를 잘라낸다. 바닥·단면용. 단면을 채운다."""
    bm = bmesh.new()
    bm.from_mesh(ob.data)
    res = bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:],
                                 plane_co=co, plane_no=no, clear_outer=True)
    edges = [e for e in res["geom_cut"] if isinstance(e, bmesh.types.BMEdge)]
    if edges:
        bmesh.ops.holes_fill(bm, edges=edges, sides=0)
    bm.to_mesh(ob.data)
    bm.free()
    return ob


def decimate_to(ob, target_tris):
    ob.data.calc_loop_triangles()
    cur = len(ob.data.loop_triangles)
    if cur > target_tris:
        m = ob.modifiers.new("Decimate", 'DECIMATE')
        m.decimate_type = 'COLLAPSE'
        m.ratio = target_tris / cur
        ob = apply_all(ob)
    return ob


def cleanup(ob, merge=0.0005, keep_largest=True):
    """병합·퇴화면 제거·노멀 통일. keep_largest면 컷·Decimate로 떨어져 나온 작은 조각을 지운다
    (실증: 그루터기 비스듬한 컷 뒤 조각 1개가 남았음)."""
    bm = bmesh.new()
    bm.from_mesh(ob.data)
    if keep_largest:
        bm.verts.ensure_lookup_table()
        seen, islands = set(), []
        for v in bm.verts:
            if v in seen:
                continue
            isl, stack = [], [v]
            while stack:
                w = stack.pop()
                if w in seen:
                    continue
                seen.add(w)
                isl.append(w)
                stack.extend(e.other_vert(w) for e in w.link_edges)
            islands.append(isl)
        islands.sort(key=len, reverse=True)
        for isl in islands[1:]:
            bmesh.ops.delete(bm, geom=isl, context='VERTS')
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=merge)
    bmesh.ops.dissolve_degenerate(bm, edges=bm.edges, dist=merge)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(ob.data)
    bm.free()
    return ob


def pivot_to_base(ob):
    """피벗을 바닥 중심으로: XY는 바운드 중심, Z는 최저점을 0으로(실증: 미적용 시 바위가 떠 보였음)."""
    vs = ob.data.vertices
    xs = [v.co.x for v in vs]; ys = [v.co.y for v in vs]; zs = [v.co.z for v in vs]
    off = Vector(((min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2, min(zs)))
    for v in vs:
        v.co -= off
    ob.data.update()
    return ob


def finish_normals(ob, sharp_deg):
    """마지막 단계. 스무스 -> 각도 기준 샤프 -> Weighted Normal을 커스텀 노멀로 굳힌다.
    Blender 4.1+는 use_auto_smooth가 없다. Mesh.shade_smooth()/set_sharp_from_angle()을 쓴다."""
    me = ob.data
    me.shade_smooth()
    me.set_sharp_from_angle(angle=math.radians(sharp_deg))
    wn = ob.modifiers.new("WeightedNormal", 'WEIGHTED_NORMAL')
    wn.keep_sharp = True
    wn.weight = 50
    wn.mode = 'FACE_AREA'
    return apply_all(ob)


# ---------------------------------------------------------------- 커브 골격
def bezier_spline(cu, pts, radii):
    sp = cu.splines.new('BEZIER')
    sp.bezier_points.add(len(pts) - 1)
    for bp, p, r in zip(sp.bezier_points, pts, radii):
        bp.co = p
        bp.radius = r
        bp.handle_left_type = bp.handle_right_type = 'AUTO'
    return sp


def wander(p0, direction, length, n, amp, seed):
    """direction을 따라가며 시드 노이즈로 휘는 점열(끝으로 갈수록 휨이 커진다)."""
    d = Vector(direction).normalized()
    pts = []
    for i in range(n):
        t = i / (n - 1)
        off = noise.noise_vector(Vector((seed * 3.1, t * 2.0, seed * 0.7))) * amp * t
        pts.append(Vector(p0) + d * (length * t) + off)
    return pts


def make_tree_trunk(name, height=3.8, base_r=0.32, branches=3, roots=5, seed=1,
                    target_tris=2500, voxel=None, dead=False,
                    taper=(1.25, 1.0, 0.85, 0.72, 0.6, 0.45)):
    """줄기·가지·뿌리를 한 커브의 여러 스플라인으로 만들고 리메시로 통합한다.
    dead=True면 가지 끝을 더 길고 가늘게 두어 고사목 실루엣을 만든다."""
    rnd = random.Random(seed)
    cu = bpy.data.curves.new(name + "_curve", 'CURVE')
    cu.dimensions = '3D'
    cu.bevel_depth = base_r
    cu.bevel_resolution = 4      # 단면 링 = (4+2)*2 = 12점. 리메시 전 형상용이므로 낮아도 된다
    cu.resolution_u = 6
    cu.use_fill_caps = True
    # 줄기: 지면 아래에서 시작, 위로 갈수록 반경 1.25 -> 0.45(테이퍼), 약한 S자
    trunk = wander((0, 0, -0.2), (rnd.uniform(-.08, .08), rnd.uniform(-.08, .08), 1), height, 6, 0.35, seed)
    bezier_spline(cu, trunk, list(taper))
    # 가지: 줄기 '안쪽'에서 시작해 접합부를 묻는다. 높이·방위·길이 모두 불균등
    for i in range(branches):
        h = height * rnd.uniform(0.55, 0.88)
        a = rnd.uniform(0, 2 * math.pi)
        c = trunk[0].lerp(trunk[-1], (h + 0.2) / (height + 0.2))
        ln = height * rnd.uniform(0.25, 0.45) * (1.4 if dead else 1.0)
        pts = wander((c.x, c.y, h), (math.cos(a) * .9, math.sin(a) * .9, rnd.uniform(.5, 1.1)), ln, 4, 0.25, seed * 10 + i)
        bezier_spline(cu, pts, [0.42, 0.3, 0.18, 0.06 if dead else 0.1])
    # 뿌리: 줄기 속 0.5m에서 출발해 바깥·아래로, 끝은 지면 아래. 각도·길이·굵기 불균등(별 모양 금지)
    base_angles = sorted(rnd.uniform(0, 2 * math.pi) for _ in range(roots))
    for i, a in enumerate(base_angles):
        ln = base_r * rnd.uniform(2.2, 3.6)
        thick = rnd.uniform(0.55, 0.85)
        start = Vector((0, 0, 0.55))
        mid = Vector((math.cos(a) * ln * .5, math.sin(a) * ln * .5, rnd.uniform(.08, .18)))
        end = Vector((math.cos(a) * ln, math.sin(a) * ln, -0.12))
        bezier_spline(cu, [start, mid, end], [thick, thick * .65, .18])
    ob = link(bpy.data.objects.new(name, cu))
    ob = apply_all(ob)
    ob = unify(ob, voxel or base_r / 10)
    displace(ob, tex(name + "_big", 'CLOUDS', 0.55, 1), base_r * 0.19)   # 큰 덩어리
    displace(ob, tex(name + "_mid", 'CLOUDS', 0.12, 2), base_r * 0.06)   # 중간 결
    ob = apply_all(ob)
    ob = cut_plane(ob, (0, 0, -0.05), (0, 0, -1))                         # 지면 아래 제거
    ob = decimate_to(ob, target_tris)
    ob = cleanup(ob)
    return finish_normals(ob, 60)


def make_log(name, length=2.4, r=0.22, seed=2, target_tris=800, stump=False):
    """통나무(눕힘) 또는 그루터기(stump=True, 세움+뿌리). 단면은 평면 컷 후 약한 경사."""
    if stump:
        # 그루터기는 거의 원통(테이퍼 약하게)으로 올린 뒤 높이 60% 지점을 잘라 넓은 단면을 만든다.
        # 테이퍼를 나무처럼 주면 리메시·스무스 뒤 원뿔 덩어리로 읽힌다(실증에서 확인).
        h = 1.2
        ob = make_tree_trunk(name, height=h, base_r=r * 1.5, branches=0, roots=4, seed=seed,
                             target_tris=10 ** 7, taper=(1.3, 1.05, 1.0, 1.0, 0.98, 0.95))
        rnd = random.Random(seed)
        tilt = Vector((rnd.uniform(-.12, .12), rnd.uniform(-.12, .12), 1)).normalized()
        ob = cut_plane(ob, (0, 0, h * 0.55), tilt)             # 비스듬한 절단면
        ob = decimate_to(ob, target_tris)
        ob = cleanup(ob)
        return finish_normals(ob, 45)                          # 절단면 테두리는 샤프 유지
    cu = bpy.data.curves.new(name + "_curve", 'CURVE')
    cu.dimensions = '3D'
    cu.bevel_depth = r
    cu.bevel_resolution = 4
    cu.resolution_u = 6
    cu.use_fill_caps = True
    pts = wander((-length / 2 - 0.1, 0, r * 0.8), (1, 0, 0), length + 0.2, 5, 0.2, seed)
    bezier_spline(cu, pts, [1.1, 0.95, 1.0, 0.88, 0.85])      # 굵기 요동: 일정 반경은 파이프로 읽힌다
    ob = link(bpy.data.objects.new(name, cu))
    ob = apply_all(ob)
    ob = unify(ob, r / 10, smooth_iter=2)
    displace(ob, tex(name + "_big", 'CLOUDS', 0.5, 1), r * 0.22)
    displace(ob, tex(name + "_mid", 'CLOUDS', 0.1, 2), r * 0.05)
    ob = apply_all(ob)
    rnd = random.Random(seed)
    for sgn in (-1, 1):                                        # 양 끝 절단면(약간 비스듬히)
        n = Vector((sgn, rnd.uniform(-.2, .2), rnd.uniform(-.15, .15))).normalized()
        ob = cut_plane(ob, (sgn * length / 2, 0, 0), n)
    ob = decimate_to(ob, target_tris)
    ob = cleanup(ob)
    return finish_normals(ob, 45)


def make_rock(name, size=(1.35, 0.95, 0.7), cut_count=6, cut_depth=(0.55, 0.75), seed=3,
              big=0.035, mid=0.012, target_tris=600, sharp_deg=30, radius=0.6):
    """구체 + 비균등 스케일 + 평면 컷(바닥 1 + 측면·윗면) + 약한 다층 노이즈.
    컷 깊이는 그 방향 돌출 거리 대비 비율: 0.9 이상은 스치기만 해서 감자처럼 남는다."""
    rnd = random.Random(seed)
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=5, radius=radius)
    bmesh.ops.scale(bm, vec=size, verts=bm.verts)
    cuts = [(0.75, Vector((0, 0, -1)))]
    for i in range(cut_count):
        th = rnd.uniform(0, 2 * math.pi) + i * 2 * math.pi / cut_count
        ph = rnd.uniform(math.radians(20), math.radians(80))
        cuts.append((rnd.uniform(*cut_depth),
                     Vector((math.cos(th) * math.sin(ph), math.sin(th) * math.sin(ph), math.cos(ph)))))
    for depth, no in cuts:
        h = max(v.co.dot(no) for v in bm.verts)
        res = bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:],
                                     plane_co=no * (h * depth), plane_no=no, clear_outer=True)
        edges = [e for e in res["geom_cut"] if isinstance(e, bmesh.types.BMEdge)]
        if edges:
            bmesh.ops.holes_fill(bm, edges=edges, sides=0)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    ob = link(bpy.data.objects.new(name, me))
    ob = unify(ob, radius / 24, smooth_iter=0)
    displace(ob, tex(name + "_big", 'CLOUDS', 0.45, 1), big * radius / 0.6)
    displace(ob, tex(name + "_mid", 'VORONOI', 0.18), mid * radius / 0.6)
    ob = apply_all(ob)
    ob = decimate_to(ob, target_tris)
    ob = cleanup(ob)
    ob = pivot_to_base(ob)
    return finish_normals(ob, sharp_deg)


# ---------------------------------------------------------------- 측정(자체 검수)
def measure(ob):
    """통과 기준은 SKILL.md 「자체 검수」 표. boundary는 지면·단면 컷의 열린 테두리(허용),
    non_manifold는 3면 이상 공유 등 진짜 결함."""
    me = ob.data
    me.calc_loop_triangles()
    bm = bmesh.new()
    bm.from_mesh(me)
    angs = sorted(e.calc_face_angle(0.0) for e in bm.edges if len(e.link_faces) == 2)
    boundary = sum(1 for e in bm.edges if len(e.link_faces) == 1)
    nonman = sum(1 for e in bm.edges if len(e.link_faces) > 2)
    zero = sum(1 for f in bm.faces if f.calc_area() < 1e-8)
    seen, parts = set(), 0
    for v in bm.verts:
        if v.index in seen:
            continue
        parts += 1
        stack = [v]
        while stack:
            w = stack.pop()
            if w.index in seen:
                continue
            seen.add(w.index)
            stack.extend(e.other_vert(w) for e in w.link_edges)
    bm.free()
    pct = lambda q: round(math.degrees(angs[min(len(angs) - 1, int(len(angs) * q))]), 1) if angs else None
    return {"obj": ob.name, "tris": len(me.loop_triangles),
            "smooth_ratio": round(sum(p.use_smooth for p in me.polygons) / max(1, len(me.polygons)), 3),
            "custom_normals": me.has_custom_normals,
            "median_edge_angle_deg": pct(0.5), "p90_edge_angle_deg": pct(0.9),
            "boundary_edges": boundary, "non_manifold_edges": nonman, "zero_area_faces": zero,
            "loose_parts": parts, "dims_m": [round(d, 2) for d in ob.dimensions],
            "local_min_z": round(min((v.co.z for v in me.vertices), default=0.0), 3),
            "scale": [round(s, 3) for s in ob.scale]}


def render_views(target, dist, out_dir, prefix, close_drop=0.0):
    """Workbench 스튜디오 조명, 캐비티·그림자 끔(셰이딩을 있는 그대로 본다). 4구도 저장."""
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_WORKBENCH'
    sh = sc.display.shading
    sh.light = 'STUDIO'
    sh.color_type = 'SINGLE'
    sh.single_color = (0.55, 0.45, 0.35)
    sh.show_cavity = False
    sh.show_shadows = False
    sc.render.resolution_x, sc.render.resolution_y = 1100, 620
    cam = link(bpy.data.objects.new("QA_cam", bpy.data.cameras.new("QA_cam")))
    cam.data.lens = 40
    sc.camera = cam
    views = {"34": (-35, 12, 1.0), "side": (-60, 2, 1.0), "top": (-20, 55, 1.0), "close": (-40, 18, 0.5)}
    t = Vector(target)
    paths = []
    for name, (yaw, elev, k) in views.items():
        tt = t + Vector((0, 0, -close_drop)) if name == "close" else t
        y, e = math.radians(yaw) - math.pi / 2, math.radians(elev)
        cam.location = tt + Vector((math.cos(y) * math.cos(e), math.sin(y) * math.cos(e), math.sin(e))) * dist * k
        cam.rotation_euler = (tt - cam.location).to_track_quat('-Z', 'Y').to_euler()
        sc.render.filepath = os.path.join(out_dir, f"{prefix}_{name}.png")
        bpy.ops.render.render(write_still=True)
        paths.append(sc.render.filepath)
    cam_data = cam.data
    bpy.data.objects.remove(cam)
    bpy.data.cameras.remove(cam_data)
    return paths


# ---------------------------------------------------------------- 데모(백그라운드 CLI 전용)
def _naive_tree(x):
    parts = []
    def cyl(r, depth, loc, rot, seg):
        bm = bmesh.new()
        bmesh.ops.create_cone(bm, cap_ends=True, segments=seg, radius1=r, radius2=r, depth=depth)
        me = bpy.data.meshes.new("naive")
        bm.to_mesh(me)
        bm.free()
        o = link(bpy.data.objects.new("naive_tree_part", me))
        o.location, o.rotation_euler = loc, rot
        me.shade_flat()
        parts.append(o)
    cyl(0.3, 3.5, (x, 0, 1.75), (0, 0, 0), 8)
    cyl(0.08, 1.4, (x + .45, 0, 2.9), (0, math.radians(50), 0), 6)
    cyl(0.07, 1.2, (x - .4, .1, 2.6), (math.radians(10), math.radians(-55), 0), 6)
    for a in (0, 120, 240):
        ra = math.radians(a)
        cyl(0.08, 0.9, (x + .45 * math.cos(ra), .45 * math.sin(ra), .12), (0, math.radians(75), ra), 6)
    return parts


def _naive_rock(loc):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=2, radius=0.6)
    me = bpy.data.meshes.new("naive_rock")
    bm.to_mesh(me)
    bm.free()
    o = link(bpy.data.objects.new("naive_rock", me))
    o.location = loc
    displace(o, tex("nr", 'CLOUDS', 0.5), 0.25)
    o = apply_all(o)
    o.data.shade_flat()
    return o


def demo(out_dir):
    os.makedirs(out_dir, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=40)
    gme = bpy.data.meshes.new("ground")
    bm.to_mesh(gme)
    bm.free()
    link(bpy.data.objects.new("ground", gme))
    info = {"blender": bpy.app.version_string, "renders": []}
    naive_t = _naive_tree(-1.6)
    tree = make_tree_trunk("recipe_tree", seed=1); tree.location.x = 1.6
    dead = make_tree_trunk("recipe_deadtree", seed=4, branches=4, dead=True); dead.location = (5.0, 1.5, 0)
    naive_r = _naive_rock((-1.6, 12, 0.25))
    rock = make_rock("recipe_rock"); rock.location = (1.6, 12, -0.12)   # 배치 시 높이의 15~50% 묻기
    log = make_log("recipe_log"); log.location = (-1.5, 22, 0)
    stump = make_log("recipe_stump", r=0.22, seed=5, stump=True, target_tris=900); stump.location = (1.8, 22, 0)
    recipes = [tree, dead, rock, log, stump]
    info["measure"] = [measure(o) for o in recipes] + [measure(naive_r), measure(naive_t[0])]
    info["renders"] += render_views((1.5, 0.5, 2.1), 12.0, out_dir, "tree", close_drop=1.2)
    info["renders"] += render_views((0, 12, 0.3), 5.0, out_dir, "rock")
    info["renders"] += render_views((0, 22, 0.4), 6.0, out_dir, "log")
    # FBX 왕복: 레시피만 내보내고 새 씬에서 다시 측정(모디파이어 적용·스무딩·커스텀 노멀 보존 확인)
    for o in bpy.context.scene.objects:
        o.select_set(o in recipes)
    bpy.context.view_layer.objects.active = tree
    fbx = os.path.join(out_dir, "recipe_roundtrip.fbx")
    # 익스포트 설정은 Unity에서 검증된 drop-item 라이브러리(ArtSource/DropItems/_lib/dropitem_lib.py)와 같게 둔다
    bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, object_types={'MESH'},
                             apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y',
                             bake_space_transform=True, mesh_smooth_type='FACE', use_mesh_modifiers=True,
                             add_leaf_bones=False, bake_anim=False, path_mode='STRIP', embed_textures=False)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=fbx)
    info["fbx_roundtrip"] = [measure(o) for o in bpy.context.scene.objects if o.type == 'MESH']
    print("ORGANIC_RESULT " + json.dumps(info, ensure_ascii=False))


if __name__ == "__main__" and "--" in sys.argv:
    args = sys.argv[sys.argv.index("--") + 1:]
    if len(args) >= 2 and args[1] == "demo":
        demo(args[0])
