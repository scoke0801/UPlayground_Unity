# 드롭 아이템 월드 모델 생성 템플릿 (Blender 5.2 백그라운드 CLI 전용)
# 복사 위치: ArtSource/DropItems/<ItemId>/build_<ItemId>.py
# 바꿀 곳: ITEM, REGIONS, build_shape(), paint_regions(). 나머지(검증·내보내기)는 그대로 둔다.
# 실행: blender.exe -b --factory-startup -P build_<ItemId>.py
import bpy, bmesh, json, math, os
import numpy as np
from mathutils import Vector

ROOT = r"C:\UsingProject\UnityProject\UPlayground"
ITEM = "<ItemId>"
SRC_DIR = os.path.join(ROOT, "ArtSource", "DropItems", ITEM)
TILE = os.path.join(SRC_DIR, "codex", ITEM + "_Tile.png")
BLEND = os.path.join(SRC_DIR, ITEM + ".blend")
FBX = os.path.join(ROOT, "Assets", "08.Models", "DropItems", ITEM + ".fbx")
TEX = os.path.join(ROOT, "Assets", "08.Models", "DropItems", "Textures", ITEM + "_Albedo.png")
PREVIEW = os.path.join(SRC_DIR, ITEM + "_preview.png")
RES = 512

# 아틀라스 영역 (u0, v0, u1, v1). "tile"은 Codex 타일을 1회 매핑하는 주 영역이다.
REGIONS = {
    "tile": (0.0, 0.0, 0.75, 1.0),
    "accent": (0.75, 0.25, 1.0, 1.0),
    "cap": (0.75, 0.0, 1.0, 0.25),
}

for p in (FBX, TEX):
    if os.path.exists(p) and os.environ.get("DROPITEM_ALLOW_REBUILD") != "1":
        raise SystemExit("EXISTS " + p)
os.makedirs(os.path.dirname(TEX), exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1.0


def hex_rgb(h):
    h = h.lstrip('#')
    return [int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4)]


def region_px(name):
    u0, v0, u1, v1 = REGIONS[name]
    return int(u0 * RES), int(v0 * RES), int(u1 * RES), int(v1 * RES)


def paint_regions(atlas):
    """tile 이외 영역을 칠한다. atlas는 (RES, RES, 4), 행 0이 v=0(아래)이다. 아이템에 맞게 바꾼다."""
    x0, y0, x1, y1 = region_px("accent")
    rng = np.random.default_rng(7)
    atlas[y0:y1, x0:x1, :3] = np.clip(np.array(hex_rgb("#EFE2C4")) + rng.normal(0, 0.015, (y1 - y0, x1 - x0, 1)), 0, 1)
    x0, y0, x1, y1 = region_px("cap")
    atlas[y0:y1, x0:x1, :3] = hex_rgb("#E8907E")


def build_atlas():
    tile = bpy.data.images.load(TILE)
    tw, th = tile.size
    tp = np.array(tile.pixels[:], dtype=np.float32).reshape(th, tw, 4)
    atlas = np.ones((RES, RES, 4), dtype=np.float32)
    x0, y0, x1, y1 = region_px("tile")
    ys = (np.arange(y1 - y0) * th // RES)
    xs = (np.arange(x1 - x0) * tw // RES)
    atlas[y0:y1, x0:x1] = tp[ys][:, xs]
    paint_regions(atlas)
    atlas[..., 3] = 1.0
    img = bpy.data.images.new(ITEM + "_Albedo", RES, RES, alpha=False)
    img.pixels[:] = atlas.ravel()
    img.filepath_raw = TEX
    img.file_format = 'PNG'
    img.save()
    return img


def fit_uvs(obj, region, pad=4.0 / RES):
    """smart_project 후 섬 전체를 영역 사각형 안으로 균등 축소한다(겹침 없음)."""
    rect = REGIONS[region]
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.02)
    bpy.ops.object.mode_set(mode='OBJECT')
    uv = obj.data.uv_layers.active.data
    us = [l.uv.x for l in uv]; vs = [l.uv.y for l in uv]
    u0, v0 = min(us), min(vs)
    s = max(max(us) - u0, max(vs) - v0)
    k = min(rect[2] - rect[0] - 2 * pad, rect[3] - rect[1] - 2 * pad) / s
    for l in uv:
        l.uv.x = rect[0] + pad + (l.uv.x - u0) * k
        l.uv.y = rect[1] + pad + (l.uv.y - v0) * k


def planar_uvs(obj, region, axis_u, axis_v, pad=4.0 / RES):
    """평평한 면(단면·라벨)을 영역에 평면 투영한다. 영역 중앙 = 면 중앙."""
    rect = REGIONS[region]
    me = obj.data
    if not me.uv_layers: me.uv_layers.new()
    uv = me.uv_layers.active.data
    pts = {}
    for poly in me.polygons:
        for li in poly.loop_indices:
            co = me.vertices[me.loops[li].vertex_index].co
            pts[li] = (co.dot(axis_u), co.dot(axis_v))
    us = [p[0] for p in pts.values()]; vs = [p[1] for p in pts.values()]
    u0, v0 = min(us), min(vs)
    s = max(max(us) - u0, max(vs) - v0)
    k = min(rect[2] - rect[0] - 2 * pad, rect[3] - rect[1] - 2 * pad) / s
    for li, (a, b) in pts.items():
        uv[li].uv = (rect[0] + pad + (a - u0) * k, rect[1] + pad + (b - v0) * k)


def apply_all(obj):
    bpy.ops.object.select_all(action='DESELECT'); obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)


def build_shape():
    """부품을 만들고 UV를 영역에 배정한 뒤 [부품 오브젝트]를 돌려준다. 아이템에 맞게 바꾼다.
    규칙: 긴 변 X, +Z 위, 실제 비율 유지(최종 크기는 런타임이 정규화). 부품마다 transform_apply 후 UV."""
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, radius=0.1)
    body = bpy.context.active_object; body.name = "Body"
    apply_all(body)
    fit_uvs(body, "tile")
    return [body]


parts = build_shape()
bpy.ops.object.select_all(action='DESELECT')
for o in parts: o.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
if len(parts) > 1: bpy.ops.object.join()
obj = parts[0]; obj.name = ITEM; obj.data.name = ITEM
bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.mesh.remove_doubles(threshold=1e-5)
bpy.ops.mesh.normals_make_consistent(inside=False)
bpy.ops.object.mode_set(mode='OBJECT')

# 피벗을 바닥 중심으로
cs = [v.co for v in obj.data.vertices]
mn = Vector((min(c.x for c in cs), min(c.y for c in cs), min(c.z for c in cs)))
mx = Vector((max(c.x for c in cs), max(c.y for c in cs), max(c.z for c in cs)))
off = Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, mn.z))
for v in obj.data.vertices: v.co -= off
bpy.ops.object.shade_smooth_by_angle(angle=math.radians(40))

img = build_atlas()
mat = bpy.data.materials.new(ITEM)
mat.use_nodes = True
nt = mat.node_tree
bsdf = nt.nodes.get("Principled BSDF")
tex = nt.nodes.new("ShaderNodeTexImage"); tex.image = img
nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
bsdf.inputs["Roughness"].default_value = 0.75
obj.data.materials.clear(); obj.data.materials.append(mat)

# 검증 수치
me = obj.data
me.calc_loop_triangles()
tris = len(me.loop_triangles)
uv_out = sum(1 for l in me.uv_layers.active.data if not (0 <= l.uv.x <= 1 and 0 <= l.uv.y <= 1))
bm = bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table()
nonmanifold = sum(1 for e in bm.edges if not e.is_manifold)
zero_area = sum(1 for f in bm.faces if f.calc_area() < 1e-9)
# 법선: 닫힌 섬마다 부호 있는 부피가 음수면 안쪽을 향한다(전체 중심 기준 판정은 다부품 메시에서 오탐)
inward, seen = 0, set()
for start in bm.faces:
    if start.index in seen: continue
    island, stack = [], [start]; seen.add(start.index)
    while stack:
        f = stack.pop(); island.append(f)
        for e in f.edges:
            for g in e.link_faces:
                if g.index not in seen: seen.add(g.index); stack.append(g)
    vol = sum(f.verts[0].co.dot(f.verts[i].co.cross(f.verts[i + 1].co))
              for f in island for i in range(1, len(f.verts) - 1))
    if vol < 0: inward += len(island)
bm.free()
dims = list(obj.dimensions)

os.makedirs(SRC_DIR, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=BLEND)
bpy.ops.object.select_all(action='DESELECT'); obj.select_set(True)
bpy.ops.export_scene.fbx(filepath=FBX, use_selection=True, object_types={'MESH'},
                         apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y',
                         bake_space_transform=True, mesh_smooth_type='FACE', use_mesh_modifiers=True,
                         add_leaf_bones=False, bake_anim=False, path_mode='STRIP', embed_textures=False)

# 실루엣 확인용 미리보기(검수 근거 아님)
size = max(dims)
bpy.ops.object.camera_add(location=(size * 1.3, -size * 1.6, size * 1.0))
cam = bpy.context.active_object
cam.rotation_euler = (Vector((0, 0, dims[2] * 0.4)) - cam.location).to_track_quat('-Z', 'Y').to_euler()
scene.camera = cam
bpy.ops.object.light_add(type='SUN', rotation=(math.radians(50), 0, math.radians(30)))
bpy.context.active_object.data.energy = 3.0
world = bpy.data.worlds.new("W"); world.use_nodes = True
world.node_tree.nodes["Background"].inputs[1].default_value = 0.6
scene.world = world
scene.render.engine = 'BLENDER_EEVEE'
scene.render.resolution_x = 512; scene.render.resolution_y = 512
scene.render.filepath = PREVIEW
bpy.ops.render.render(write_still=True)

print("DROPITEM_RESULT " + json.dumps({
    "tris": tris, "uv_out_of_range": uv_out, "non_manifold_edges": nonmanifold, "zero_area_faces": zero_area,
    "inward_faces": inward, "dims_m": dims, "material_slots": len(obj.material_slots),
    "scale": list(obj.scale), "blend": BLEND, "fbx": FBX, "tex": TEX}))
