import math
import random
from pathlib import Path

import bpy
from mathutils import Vector


def build(project):
    """세계수 원본과 Unity용 FBX를 미터 단위로 내보낸다."""
    project = Path(project)
    source = project / "Tools/RiverBasinWorld/ElvenVillage/RiverBasin_WorldTree.blend"
    target = project / ("Assets/WorldBuilder/Worlds/"
                        "6effd90b68ea4b3d8f8caa5197fcfe34/Authoring/ElvenVillage/SM_RBElf_WorldTree.fbx")
    previous_preview = bpy.data.scenes.get("River Basin · 세계수")
    if previous_preview and previous_preview != bpy.context.scene:
        for obj in list(previous_preview.objects):
            if obj.name.startswith("SM_RBElf_WorldTree_LOD") and len(obj.users_scene) == 1:
                mesh = obj.data
                bpy.data.objects.remove(obj, do_unlink=True)
                if not mesh.users:
                    bpy.data.meshes.remove(mesh)
        bpy.data.scenes.remove(previous_preview)
    scene = bpy.data.scenes.new("River Basin · 세계수")
    scene.unit_settings.system = "METRIC"
    window = bpy.context.window
    previous_scene = window.scene
    window.scene = scene
    colors = [
        ("Bark", (.23, .12, .058)), ("BarkRidge", (.32, .19, .10)),
        ("BarkShade", (.15, .073, .036)), ("Ochre", (.63, .26, .033)),
        ("Amber", (.92, .47, .045)), ("Gold", (.97, .67, .105)),
        ("Bronze", (.61, .14, .027)), ("Rust", (.43, .065, .025)),
    ]
    materials = []
    for name, color in colors:
        label = "RBWorldTree_" + name
        material = bpy.data.materials.get(label) or bpy.data.materials.new(label)
        material.diffuse_color = (*color, 1)
        materials.append(material)

    trunk = [(0, 0, -1.2), (-.25, -.15, 1.4), (-1.2, -.4, 4.8),
             (-1.16, .3, 8.6), (.15, 1.0, 13.2), (1.7, 1.4, 18.5),
             (1.4, 1.7, 23), (.1, 1.0, 28), (-1.4, .2, 32.0)]
    branch_tips = [
        (-18, -4, 22), (-13, -14, 23), (-3, -17, 24), (11, -15, 23),
        (18, -8, 25), (17, 5, 23), (13, 13, 26), (1, 16, 29),
        (-12, 12, 26), (-16, 5, 28), (-7, -7, 31), (7, 5, 32),
    ]
    direction_z = Vector((0, 0, 1))
    base_ico = bpy.data.meshes.new("잎 기하 원형")
    import bmesh
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=1, radius=1)
    bm.to_mesh(base_ico)
    bm.free()
    ico_points = [v.co.copy() for v in base_ico.vertices]
    ico_faces = [tuple(p.vertices) for p in base_ico.polygons]
    bpy.data.meshes.remove(base_ico)

    def make_lod(level):
        vertices, polygons, slots, smooth = [], [], [], []

        def face(indices, slot, shade=False):
            polygons.append(tuple(indices))
            slots.append(slot)
            smooth.append(shade)

        def tube(points, radii, sides, seed):
            points = [Vector(p) for p in points]
            start = len(vertices)
            phase = random.Random(seed).uniform(0, math.tau)
            for row, (point, radius) in enumerate(zip(points, radii)):
                tangent = (points[min(row + 1, len(points) - 1)]
                           - points[max(row - 1, 0)]).normalized()
                side = tangent.cross(Vector((.17, .95, .11))).normalized()
                across = tangent.cross(side)
                for index in range(sides):
                    angle = index * math.tau / sides
                    wave = 1 + .145 * math.sin(5 * angle + phase + row * .27)
                    wave += .063 * math.cos(9 * angle - phase + row * .18)
                    vertices.append(tuple(point + radius * wave *
                                          (math.cos(angle) * side + math.sin(angle) * across)))
            face(reversed(range(start, start + sides)), 2)
            for row in range(len(points) - 1):
                for index in range(sides):
                    a = start + row * sides + index
                    b = start + row * sides + (index + 1) % sides
                    variation = (index * 7 + seed) % 17
                    face((a, b, b + sides, a + sides),
                         1 if variation < 3 else 2 if variation > 13 else 0, True)
            end = start + (len(points) - 1) * sides
            face(range(end, end + sides), 0)

        def canopy(tip, group_index, scale):
            rng = random.Random(893 + group_index)
            color = [3, 4, 5, 5, 4, 6, 7, 4, 3, 5, 4, 5][group_index % 12]
            count = [11, 8, 4][level]
            for lobe in range(count):
                angle = lobe * 2.39996 + rng.uniform(-.2, .2)
                distance = scale * math.sqrt((lobe + .35) / count)
                offset = Vector((math.cos(angle) * distance, math.sin(angle) * distance,
                                 rng.uniform(-1.0, 2.1) - .31 * distance))
                center = tip + offset
                width = rng.uniform(1.6, 2.85) * (1 + level * .22)
                height = rng.uniform(.83, 1.66) * (1 + level * .18)
                start = len(vertices)
                yaw = rng.uniform(0, math.tau)
                cos_yaw, sin_yaw = math.cos(yaw), math.sin(yaw)
                for point in ico_points:
                    uneven = rng.uniform(.88, 1.12)
                    x, y = point.x * width, point.y * width * .83
                    vertices.append(tuple(center + Vector((x * cos_yaw - y * sin_yaw, x * sin_yaw + y * cos_yaw,
                                                          point.z * height)) * uneven))
                shade = max(3, color - 1) if lobe % 5 == 0 else min(7, color + 1) if lobe % 7 == 0 else color
                for polygon in ico_faces:
                    face((start + i for i in polygon), shade, True)
                if level == 0:
                    for twig in range(2):
                        a = angle + twig * 1.05 - .52
                        f = Vector((math.cos(a), math.sin(a), rng.uniform(-.34, .13))).normalized()
                        cross = direction_z.cross(f).normalized()
                        leaf = center + f * (width * .71)
                        length = rng.uniform(.72, 1.22)
                        start = len(vertices)
                        vertices.extend(tuple(leaf + point) for point in (
                            Vector((0, 0, 0)), f * length * .59 - cross * .36,
                            f * length + direction_z * .22, f * length * .52 + cross * .38,
                            f * length * .58 + direction_z * .14))
                        for k in range(4):
                            face((start + k, start + (k + 1) % 4, start + 4), shade)

        tube(trunk, [4.5, 3.8, 2.8, 2.34, 2.06, 1.68, 1.08, .58, .12],
             [22, 14, 9][level], 7)
        for i in range(9):
            rng = random.Random(210 + i)
            angle = math.tau * i / 9 + rng.uniform(-.18, .18)
            forward = Vector((math.cos(angle), math.sin(angle), 0))
            side = direction_z.cross(forward)
            length = rng.uniform(6.5, 12.0)
            tube([1.4 * forward + direction_z * 3.8, forward * 3.5 + direction_z * 1.9,
                  forward * (length * .6) + side * .8 + direction_z * .37,
                  forward * length + side * 1.35 - direction_z * .62],
                 [1.11, 1.15, .62, .1], [11, 7, 5][level], i + 19)

        for i, point in enumerate(branch_tips):
            rng = random.Random(431 + i)
            tip = Vector(point)
            radial = Vector((tip.x, tip.y, 0)).normalized()
            skew = direction_z.cross(radial) * rng.uniform(-2.7, 2.7)
            anchor = Vector(trunk[3 + (i % 3)])
            elbow = anchor.lerp(tip, .61) + skew + direction_z * .7
            shoulder = anchor.lerp(elbow, .48) - direction_z * 1.6
            tube([anchor, shoulder, elbow, tip - direction_z * 1.1, tip],
                 [1.54, 1.29, .92, .48, .095], [12, 8, 5][level], i + 39)
            canopy(tip, i, 4.0)
            for j in range(2):
                fork_tip = tip.lerp(elbow, .34) + (-1 if j == 0 else 1) * direction_z.cross(radial) * (3.8 + j)
                fork_tip.z += 1.6 + j
                tube([elbow, elbow.lerp(fork_tip, .59), fork_tip],
                     [.49, .29, .08], [8, 5, 4][level], i + j + 94)
                canopy(fork_tip, i + 12 * (j + 1), 2.9)
        canopy(Vector(trunk[-1]) + direction_z * .75, 26, 4.1)
        mesh = bpy.data.meshes.new("RBWorldTreeGeometry_LOD" + str(level))
        mesh.from_pydata(vertices, [], polygons)
        mesh.update()
        for material in materials:
            mesh.materials.append(material)
        for p, slot, shade in zip(mesh.polygons, slots, smooth):
            p.material_index = slot
            p.use_smooth = shade
        mesh.calc_loop_triangles()
        obj = bpy.data.objects.new("SM_RBElf_WorldTree_LOD" + str(level), mesh)
        scene.collection.objects.link(obj)
        return obj

    try:
        objects = [make_lod(level) for level in range(3)]
        for obj in objects:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = objects[0]
        bpy.ops.export_scene.fbx(filepath=str(target), use_selection=True, axis_forward="-Z",
                                axis_up="Y", apply_unit_scale=True, add_leaf_bones=False, bake_anim=False)
        for obj in objects[1:]:
            obj.hide_viewport = True
            obj.hide_render = True
        for area in bpy.context.screen.areas:
            if area.type == "VIEW_3D":
                region = area.spaces.active.region_3d
                region.view_location = (0, 0, 15)
                region.view_distance = 67
                region.view_rotation = Vector((1, -2, 1)).to_track_quat("Z", "Y")
                area.spaces.active.shading.color_type = "MATERIAL"
        bpy.data.libraries.write(str(source), {scene}, fake_user=True, compress=True)
        return {obj.name: {"triangles": len(obj.data.loop_triangles),
                           "size": list(obj.dimensions)} for obj in objects}
    finally:
        window.scene = previous_scene
