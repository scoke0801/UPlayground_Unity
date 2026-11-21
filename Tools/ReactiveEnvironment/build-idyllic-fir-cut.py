"""Blender 백그라운드에서 Idyllic Fir_02의 원본 LOD를 절단하고 단면을 저작한다."""
import json
import math
import os
from pathlib import Path

import bmesh
import bpy
import numpy as np
from mathutils import Vector


ROOT = Path.cwd()
SOURCE = ROOT / "Assets/ExternalAssets/Environment/Idyllic Fantasy Nature/Models/Trees/Fir_02.fbx"
STAGING = os.environ.get("FIR02_BUILD_OUTPUT")
MODEL = Path(STAGING) if STAGING else ROOT / "Assets/08.Models/ReactiveTrees"
ART = Path(STAGING) if STAGING else ROOT / "Tools/ReactiveEnvironment/Source"
REPORT = Path(STAGING) if STAGING else ROOT / "output/ReactiveEnvironment/AllTreeAudit"
CUT = 1.08
EPSILON = 0.00004
AXIS = Vector((0, 0, 1))


def cut_upper(original, label, grain):
    mesh = original.data.copy()
    mesh.transform(original.matrix_world)
    bark_index = next(index for index, material in enumerate(mesh.materials) if material.name.startswith("Trunk"))
    mesh.materials.append(grain)
    cap_index = len(mesh.materials) - 1
    edit = bmesh.new()
    try:
        edit.from_mesh(mesh)
        bmesh.ops.bisect_plane(
            edit, geom=list(edit.verts) + list(edit.edges) + list(edit.faces), dist=EPSILON,
            plane_co=(0, 0, CUT), plane_no=AXIS, clear_inner=True)
        rim = [vertex for vertex in edit.verts if abs(vertex.co.z - CUT) < EPSILON * 2]
        bmesh.ops.remove_doubles(edit, verts=rim, dist=EPSILON)
        edge_loop = [
            edge for edge in edit.edges if edge.is_boundary
            and edge.link_faces[0].material_index == bark_index
            and all(abs(vertex.co.z - CUT) < EPSILON * 2 for vertex in edge.verts)
        ]
        if len(edge_loop) < 5:
            raise ValueError(f"{label}: 줄기 절단 경계 부족 {len(edge_loop)}")
        caps = bmesh.ops.holes_fill(edit, edges=edge_loop, sides=0)["faces"]
        if len(caps) != 1 or any(edge.is_boundary for edge in edge_loop):
            raise ValueError(f"{label}: 연속 절단면을 닫지 못함")
        rim_points = [vertex.co.copy() for vertex in caps[0].verts]
        center = sum(rim_points, Vector()) / len(rim_points)
        diameter_x = max(point.x for point in rim_points) - min(point.x for point in rim_points)
        diameter_y = max(point.y for point in rim_points) - min(point.y for point in rim_points)
        uv = edit.loops.layers.uv.active
        cap = caps[0]
        cap.material_index = cap_index
        cap.smooth = False
        cap.normal_update()
        if cap.normal.z >= 0:
            cap.normal_flip()
        for loop in cap.loops:
            offset = loop.vert.co - center
            loop[uv].uv = (0.5 + offset.x / (diameter_x * 1.12), 0.5 + offset.y / (diameter_y * 1.12))
        bmesh.ops.triangulate(edit, faces=caps)
        edit.to_mesh(mesh)
    finally:
        edit.free()
    mesh.name = label
    mesh.update()
    obj = bpy.data.objects.new(label, mesh)
    bpy.context.collection.objects.link(obj)
    used = sorted({polygon.material_index for polygon in mesh.polygons})
    # 용도가 없는 슬롯을 내보내지 않아 Unity의 material-slot 순서가 분명해진다.
    assignments = [used.index(polygon.material_index) for polygon in mesh.polygons]
    baked_materials = [mesh.materials[index] for index in used]
    mesh.materials.clear()
    for material in baked_materials:
        mesh.materials.append(material)
    for polygon, material_index in zip(mesh.polygons, assignments):
        polygon.material_index = material_index
    mesh.calc_loop_triangles()
    return obj, {
        "name": label, "materials": [material.name for material in mesh.materials],
        "triangles": len(mesh.loop_triangles), "cutDiameter": [diameter_x, diameter_y],
        "cutCenter": list(center), "uvLayers": len(mesh.uv_layers),
    }


def build_stump(original, lod, grain, cut):
    name = f"Fir02_Stump_LOD{lod}"
    bark = next(material for material in original.data.materials if material.name.startswith("Trunk"))
    sides = (48, 28, 16)[lod]
    # 절단면 치수에서 시작해 지면 쪽에만 뿌리와 불규칙한 줄기 굴곡을 만든다.
    profile = [
        (-0.11, 1.60, 0.94), (-0.04, 1.48, 1.0), (0.035, 1.35, 0.89),
        (0.12, 1.26, 0.66), (0.22, 1.17, 0.37), (0.34, 1.13, 0.16),
        (0.47, 1.07, 0.04), (0.62, 1.03, 0), (0.77, 1.03, 0),
        (0.91, 1.01, 0), (CUT - 0.022, 1.03, 0), (CUT, 1.0, 0),
    ]
    bands = ((0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), (0, 2, 3, 5, 7, 9, 10, 11),
             (0, 3, 6, 10, 11))[lod]
    roots = [(0.24, 0.24), (1.89, 0.16), (3.73, 0.20), (5.18, 0.14)]
    radius_x, radius_y = (diameter * 0.5 for diameter in cut["cutDiameter"])
    center_x, center_y, _ = cut["cutCenter"]
    vertices, faces, uvs = [], [], []
    z0 = profile[0][0]
    for index in bands:
        height, scale, spread = profile[index]
        for side in range(sides):
            angle = math.tau * side / sides
            flare = sum(length * max(0, math.cos(angle - direction)) ** 18 for direction, length in roots)
            phase = height * 1.4
            contour = 1 + 0.029 * math.sin(angle * 3 + phase) + 0.018 * math.sin(angle * 7 - phase)
            x = center_x + (radius_x * scale * contour + flare * spread) * math.cos(angle)
            y = center_y + (radius_y * scale * contour + flare * spread) * math.sin(angle)
            z = height + spread * 0.015 * math.sin(angle * 5 + 0.8)
            vertices.append((x, y, z))
    for ring in range(len(bands) - 1):
        bottom_v = (profile[bands[ring]][0] - z0) / (CUT - z0)
        top_v = (profile[bands[ring + 1]][0] - z0) / (CUT - z0)
        for side in range(sides):
            following = (side + 1) % sides
            faces.append((ring * sides + side, ring * sides + following,
                          (ring + 1) * sides + following, (ring + 1) * sides + side))
            left, right = side / sides, (side + 1) / sides
            uvs.append(((left, bottom_v), (right, bottom_v), (right, top_v), (left, top_v)))
    side_count = len(faces)
    lower_center = len(vertices)
    vertices.extend(((center_x, center_y, z0), (center_x, center_y, CUT)))
    upper_ring = (len(bands) - 1) * sides
    for side in range(sides):
        faces.append((lower_center, (side + 1) % sides, side))
        uvs.append(((0.5, 0.5), (0, 0), (1, 0)))
    for side in range(sides):
        cap_face = (lower_center + 1, upper_ring + side, upper_ring + (side + 1) % sides)
        faces.append(cap_face)
        uvs.append(tuple((0.5 + (vertices[i][0] - center_x) / (radius_x * 2.24),
                          0.5 + (vertices[i][1] - center_y) / (radius_y * 2.24)) for i in cap_face))
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.append(bark)
    mesh.materials.append(grain)
    uv_layer = mesh.uv_layers.new(name="UVMap")
    for polygon, coordinates in zip(mesh.polygons, uvs):
        polygon.use_smooth = polygon.index < side_count
        polygon.material_index = int(polygon.index >= side_count + sides)
        for loop, coordinate in zip(polygon.loop_indices, coordinates):
            uv_layer.data[loop].uv = coordinate
    mesh.update()
    edit = bmesh.new()
    try:
        edit.from_mesh(mesh)
        if any(not edge.is_manifold for edge in edit.edges) or edit.calc_volume(signed=True) <= 0:
            raise ValueError(f"{name}: 밑동이 닫힌 양의 부피가 아닙니다.")
    finally:
        edit.free()
    mesh.calc_loop_triangles()
    stump = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(stump)
    return stump, {
        "name": name, "materials": [bark.name, grain.name], "triangles": len(mesh.loop_triangles),
        "cutDiameter": cut["cutDiameter"], "cutCenter": cut["cutCenter"], "uvLayers": len(mesh.uv_layers),
        "radialSegments": sides, "closed": True,
    }


def make_grain():
    size = 256
    grid = np.linspace(-1, 1, size, dtype=np.float32)
    x, y = np.meshgrid(grid, grid)
    angle = np.arctan2(y + 0.04, x - 0.05)
    radius = np.hypot(x - 0.05, y + 0.04)
    ring_offset = radius + 0.019 * np.sin(angle * 3 + radius * 6) + 0.013 * np.cos(angle * 6 - radius * 10)
    ring_phase = ring_offset * 90 + 0.82 * np.sin(ring_offset * 24)
    latewood = np.clip((np.cos(ring_phase) - 0.45) * 1.48, 0, 1)
    pores = 0.018 * np.sin(x * 173 + y * 59) * np.sin(y * 157 + x * 27)
    large_grain = 0.028 * np.sin(angle * 3 + radius * 4) + 0.021 * np.sin(x * 34 + y * 49)
    heart = 0.12 * np.clip(1 - radius * 1.75, 0, 1)
    tone = 0.82 - 0.24 * latewood - heart + pores + large_grain
    warm = np.array([0.67, 0.43, 0.24], dtype=np.float32)
    pale = np.array([0.95, 0.79, 0.58], dtype=np.float32)
    pixels = np.ones((size, size, 4), dtype=np.float32)
    pixels[..., :3] = warm + (pale - warm) * tone[..., None]
    image = bpy.data.images.new("Fir02_EndGrain", width=size, height=size, alpha=True)
    # 픽셀을 선형으로 전달해 저장된 PNG의 sRGB 목표색을 유지한다.
    pixels[..., :3] = np.power(pixels[..., :3], 2.2)
    image.pixels.foreach_set(pixels.ravel())
    image.filepath_raw = str(MODEL / "Fir02_EndGrain.png")
    image.file_format = 'PNG'
    image.save()
    image.pack()
    image.filepath = "//../../../Assets/08.Models/ReactiveTrees/Fir02_EndGrain.png"
    material = bpy.data.materials.new("EndGrain")
    material.diffuse_color = (0.65, 0.4, 0.2, 1)
    material.use_nodes = True
    texture = material.node_tree.nodes.new("ShaderNodeTexImage")
    texture.image = image
    material.node_tree.links.new(texture.outputs['Color'], material.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
    return material


def main():
    files = (MODEL / "Fir02_FallParts.fbx", MODEL / "Fir02_EndGrain.png", ART / "Fir02_FallParts.blend")
    if any(file.exists() for file in files):
        raise FileExistsError("기존 절단 산출물이 있습니다.")
    for folder in (MODEL, ART, REPORT):
        folder.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(SOURCE))
    originals = sorted((obj for obj in bpy.context.scene.objects if obj.type == 'MESH'), key=lambda obj: obj.name)
    if [obj.name for obj in originals] != [f"Fir2_LOD{lod}" for lod in range(3)]:
        raise ValueError("예상한 Fir_02의 LOD0~2를 찾을 수 없습니다.")
    source = bpy.data.collections.new("Source_Fir02")
    bpy.context.scene.collection.children.link(source)
    bpy.context.preferences.filepaths.save_version = 0
    grain = make_grain()
    parts, report = [], []
    for lod, original in enumerate(originals):
        upper, upper_entry = cut_upper(original, f"Fir02_Upper_LOD{lod}", grain)
        stump, stump_entry = build_stump(original, lod, grain, upper_entry)
        parts.extend((stump, upper))
        report.extend((stump_entry, upper_entry))
        for collection in list(original.users_collection):
            collection.objects.unlink(original)
        source.objects.link(original)
    source.hide_viewport = True
    source.hide_render = True
    bpy.ops.object.select_all(action='DESELECT')
    for part in parts:
        part.select_set(True)
    bpy.context.view_layer.objects.active = parts[1]
    bpy.ops.export_scene.fbx(filepath=str(files[0]), use_selection=True, axis_forward='-Z', axis_up='Y',
                             add_leaf_bones=False, mesh_smooth_type='OFF')
    bpy.ops.wm.save_as_mainfile(filepath=str(files[2]))
    (REPORT / "fir02-blender.json").write_text(json.dumps({"cutHeight": CUT, "parts": report}, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
