"""Blender MCP에서 실행하는 River Basin 상선 원본 저작."""

import math
import random
from pathlib import Path

import bpy
from mathutils import Vector


def build_merchant_ship(workspace):
    rng = random.Random(19)
    source = workspace / "Tools/RiverBasinWorld/HarborCity"
    export = workspace / ("Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/"
                          "Authoring/HarborCity/RiverBasinMerchantShip.fbx")
    old = bpy.data.collections.get("RiverBasin Merchant Ship")
    if old:
        for obj in list(old.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(old)
    collection = bpy.data.collections.new("RiverBasin Merchant Ship")
    bpy.context.scene.collection.children.link(collection)

    def material(name, rgb):
        mat = bpy.data.materials.new(name)
        mat.diffuse_color = (*rgb, 1.0)
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value = (*rgb, 1.0)
        bsdf.inputs["Roughness"].default_value = .76
        return mat

    wood = [material("RB_Oak%02d" % i, c) for i, c in enumerate([
        (.21, .092, .043), (.27, .126, .061), (.33, .166, .082),
        (.37, .193, .094), (.30, .143, .062)])]
    deck = [material("RB_Deck%02d" % i, c) for i, c in enumerate([
        (.40, .26, .15), (.49, .34, .21), (.43, .29, .17)])]
    blue = material("RB_PaintedBlue", (.048, .119, .160))
    brass = material("RB_BurnishedBrass", (.62, .36, .113))
    rope = material("RB_Hemp", (.23, .157, .091))
    sail = [material("RB_Linen%02d" % i, c) for i, c in enumerate([
        (.85, .737, .509), (.91, .802, .585), (.78, .658, .435)])]

    def mesh(name, verts, faces, mats, slots=None, smooth=False):
        data = bpy.data.meshes.new(name)
        data.from_pydata(verts, [], faces)
        data.update()
        obj = bpy.data.objects.new(name, data)
        collection.objects.link(obj)
        for mat in mats:
            data.materials.append(mat)
        uv_layer = data.uv_layers.new(name="UVMap")
        for poly in data.polygons:
            poly.use_smooth = smooth
            if slots is not None:
                poly.material_index = slots[poly.index]
            axis = max(range(3), key=lambda i: abs(poly.normal[i]))
            for loop_index in poly.loop_indices:
                co = data.vertices[data.loops[loop_index].vertex_index].co
                if axis == 0:
                    uv_layer.data[loop_index].uv = (co.y/3, co.z/1.8)
                elif axis == 1:
                    uv_layer.data[loop_index].uv = (co.x/3, co.z/1.8)
                else:
                    uv_layer.data[loop_index].uv = (co.y/3, co.x/1.8)
        return obj

    def tube(name, a, b, radius, mat, sides=8, end=None):
        forward = (Vector(b) - Vector(a)).normalized()
        right = forward.cross(Vector((0, 1, 0))).normalized()
        if right.length < 0.01:
            right = forward.cross(Vector((1, 0, 0))).normalized()
        up = forward.cross(right)
        verts = []
        for position, width in ((a, radius), (b, radius if end is None else end)):
            verts.extend(Vector(position) + width * (
                right * math.cos(i * math.tau / sides) + up * math.sin(i * math.tau / sides))
                         for i in range(sides))
        faces = [(i, (i+1) % sides, (i+1) % sides+sides, i+sides)
                 for i in range(sides)]
        faces.extend([tuple(reversed(range(sides))), tuple(range(sides, sides*2))])
        return mesh(name, verts, faces, [mat])

    def block(name, x, y, z, w, d, h, mat):
        pts = [(x + dx*w/2, y + dy*d/2, z + dz*h/2)
               for dz in (-1, 1) for dx, dy in [(-1,-1),(1,-1),(1,1),(-1,1)]]
        return mesh(name, pts, [(3,2,1,0),(4,5,6,7),(0,1,5,4),
                                (1,2,6,5),(2,3,7,6),(3,0,4,7)], [mat])

    def beam(y):
        t = (y + 16.0) / 32.0
        return 4.42 * max(.01, math.sin(math.pi*t)) ** .7 * (1.14 - .27*t)

    def sheer(y):
        return 3.45 + 1.9 * abs(y / 16.0) ** 3 + .38 * max(0, -y / 16.0)

    # 수선 아래로 이어지는 선체, 선측 판재와 상단 채색 띠를 실제 메시로 분리한다.
    stations = [-16 + 32*i/48 for i in range(49)]
    bands = 18
    verts = []
    for side in (-1, 1):
        for j in range(bands+1):
            t = j / bands
            for y in stations:
                x = side * beam(y) * math.sin(t * math.pi * .59) ** .79
                z = -2.6 + (sheer(y)+2.6)*t
                verts.append((x, y, z))
    faces, slots = [], []
    stride = len(stations)
    for side in range(2):
        base = side * (bands+1) * stride
        for j in range(bands):
            for i in range(stride-1):
                a = base + j*stride+i
                face = (a, a+1, a+1+stride, a+stride)
                faces.append(face if side else face[::-1])
                slots.append(5 if j in (14,15,16) else 6 if j == 17 else
                             (j + i//7 + side) % len(wood))
    mesh("Carvel hull", verts, faces, wood + [blue, brass], slots, smooth=True)
    for y0, y1 in zip(stations, stations[1:]):
        for side in (-1, 1):
            a = (side*beam(y0)*.966,y0,sheer(y0)+.27)
            b = (side*beam(y1)*.966,y1,sheer(y1)+.27)
            tube("Cap rail", a, b, .12, wood[1], 6)
        z0, z1 = sheer(y0)-.16, sheer(y1)-.16
        v = [(-beam(y0)*.95,y0,z0),(beam(y0)*.95,y0,z0),
             (beam(y1)*.95,y1,z1),(-beam(y1)*.95,y1,z1)]
        mesh("Deck planks", v, [(0,1,2,3)], [deck[rng.randrange(len(deck))]])
    for y in range(-13, 14, 2):
        for side in (-1, 1):
            x = side * beam(y) * .968
            z = sheer(y)
            tube("Stanchion", (x,y,z), (x,y,z+.86), .085, wood[1])
            next_y = y + 2
            tube("Upper handrail", (x,y,z+.84),
                 (side*beam(next_y)*.968,next_y,sheer(next_y)+.84), .089, wood[1])
            # 암청색 선측을 가르는 금속 장식과 계류 고리.
            tube("Mooring knee", (x, y, z-.65), (x*1.01, y, z-.36), .105, brass)
    block("After cabin", 0, -10.2, 5.16, 4.60, 3.61, 2.10, blue)
    block("After cabin roof", 0, -10.2, 6.29, 4.92, 3.94, .22, wood[2])
    for x in (-1.55, -.52, .52, 1.55):
        block("Cabin window trim", x, -12.03, 5.30, .69, .11, .70, brass)
        block("Cabin window", x, -12.11, 5.30, .53, .07, .52, blue)
    block("Cargo hatch", 0, -.25, 3.70, 2.30, 3.58, .42, wood[0])
    for x in (-.84, -.42, 0, .42, .84):
        block("Hatch plank", x, -.25, 3.95, .38, 3.79, .11, deck[1])
    tube("Bowsprit", (0, 12, 4.8), (0, 19, 7.55), .22, wood[2], 12, .065)
    for y, height, width in [(-7, 15.2, 6.55), (1.0, 21.2, 10.4), (9.3, 17.4, 8.75)]:
        tube("Tapered mast", (0,y,3.28), (0,y,height), .28, wood[2], 14, .074)
        for r in (0, 1):
            top = height - (2.15 if r == 0 else 6.05)
            w = width * (.68 if r == 0 else 1)
            drop = 3.35 if r == 0 else 4.22
            tube("Tapered yard", (-w*.54,y,top), (0,y,top), .075, wood[2], 10, .15)
            tube("Tapered yard", (0,y,top), (w*.54,y,top), .15, wood[2], 10, .075)
            verts, faces, ids = [], [], []
            for j in range(10):
                t = j / 9
                for i in range(19):
                    u = i / 18
                    x = (u-.5) * w * (1-.12*t)
                    belly = math.sin(math.pi*u)*math.sin(2.35*t)*w*.153
                    z = top - drop*t + .39*t*abs(u-.5)**2*4
                    verts.append((x, y+belly+.10*math.sin(u*math.tau*5)*t, z))
            for j in range(9):
                for i in range(18):
                    k = j*19+i
                    faces.append((k,k+19,k+20,k+1))
                    ids.append(2 if i % 6 == 0 else (i//6) % 2)
            canvas = mesh("Panelled linen", verts, faces, sail, ids, smooth=True)
            shell = canvas.modifiers.new("Linen thickness", "SOLIDIFY")
            shell.thickness = .016
            for side in (-1, 1):
                tube("Yard brace", (side*w*.49,y,top), (0,y,height-.46), .033, rope, 6)
                tube("Sail sheet", (side*w*.42,y+.41,top-drop+.27),
                     (side*beam(y)*.73,y-2.1,4.14), .036, rope, 6)
        for side in (-1, 1):
            for offset in (-1.35, -.45, .45, 1.35):
                tube("Shroud", (side*beam(y)*.96,y+offset,sheer(y)), (0,y,height-3),
                     .041, rope, 6)
        tube("Forestay", (0,y,height-.3), (0,y+7,6.0), .034, rope, 6)
        mesh("Trade pennant", [(0,y,height-.12),(0,y+2.3,height-.28),
                               (.34,y+1.51,height-.52),(0,y,height-.71)], [(0,1,2,3)], [blue])

    bpy.context.view_layer.update()
    objects = list(collection.objects)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.convert(target="MESH")
    bpy.ops.object.join()
    model = bpy.context.view_layer.objects.active
    model.name = "RiverBasinMerchantShip"
    bpy.ops.wm.save_as_mainfile(filepath=str(source / "RiverBasinMerchantShip.blend"), copy=True)
    bpy.ops.export_scene.fbx(filepath=str(export), use_selection=True,
                             axis_forward="-Z", axis_up="Y", apply_unit_scale=True,
                             bake_anim=False, use_mesh_modifiers=True)
    return {"mesh": model.name, "vertices": len(model.data.vertices),
            "polygons": len(model.data.polygons), "fbx": str(export)}

