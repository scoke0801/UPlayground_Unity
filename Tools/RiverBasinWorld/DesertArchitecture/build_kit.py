"""Blender에서 실행한다. DESERT_KIT_DIR을 지정하면 해당 폴더에 내보낸다."""
import os
import math
from pathlib import Path

import bpy
from mathutils import Vector

root = Path(globals().get("DESERT_KIT_DIR", Path(__file__).parent))
exports = root / "Export"
exports.mkdir(parents=True, exist_ok=True)
previous_scene = bpy.context.window.scene
scene = bpy.data.scenes.new("RiverBasin Desert Architecture")
bpy.context.window.scene = scene
scene.unit_settings.system = "METRIC"
palette = [
    ("RB_AdobePlaster", "D9AD79"), ("RB_Sandstone", "AC7956"),
    ("RB_Limewash", "EDD09C"), ("RB_PalmTimber", "654335"),
    ("RB_Shutter", "896247"), ("RB_OxideCloth", "AD4033"),
]
materials = []
for name, tint in palette:
    mat = bpy.data.materials.new(name)
    srgb = [int(tint[i:i+2], 16) / 255 for i in (0, 2, 4)]
    mat.diffuse_color = tuple(((v + .055) / 1.055) ** 2.4 for v in srgb) + (1,)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = mat.diffuse_color
    shader.inputs["Roughness"].default_value = .93
    materials.append(mat)


def ring_solid(rings, material):
    start = len(vertices)
    count = len(rings[0])
    for ring in rings:
        vertices.extend(ring)
    polys = [tuple(reversed(range(count)))]
    for level in range(len(rings) - 1):
        for i in range(count):
            j = (i + 1) % count
            a, b = level * count, (level + 1) * count
            polys.append((a+i, a+j, b+j, b+i))
    polys.append(tuple(range((len(rings)-1)*count, len(rings)*count)))
    faces.extend(tuple(start+i for i in p) for p in polys)
    indices.extend([material] * len(polys))


def block(x, y, z, w, d, h, mat, taper=0, chamfer=.06):
    rings = []
    for top in (0, 1):
        a, b = w/2 - top*taper, d/2 - top*taper
        c = min(chamfer, a*.35, b*.35)
        outline = [(-a+c,-b),(a-c,-b),(a,-b+c),(a,b-c),(a-c,b),(-a+c,b),(-a,b-c),(-a,-b+c)]
        rings.append([(x+u, y+v, z+h*top) for u,v in outline])
    ring_solid(rings, mat)


def roof(x,y,z,w,d):
    block(x,y,z,w,d,.18,2)
    for side in (-1,1):
        block(x,y+side*(d/2-.12),z+.17,w,.24,.28,0)
        block(x+side*(w/2-.12),y,z+.17,.24,d-.48,.28,0)


def facade(w,d,h,door):
    # 전면 벽을 창과 문 둘레로 나누어 실제 깊이가 있는 벽감을 만든다.
    y = -d/2
    block(0,.16,.27,w,d-.32,h-.27,0,.045)
    windows = [(door,1.14,.29,2.35)]
    for x in (-w*.34,0,w*.34):
        if abs(x-door)>1.36:
            windows.append((x,.72,1.45,2.33))
    edge = -w/2
    for x, width, sill, lintel in sorted(windows):
        left, right = x-width/2, x+width/2
        block((edge+left)/2,y+.17,.27,left-edge,.35,h-.27,0,chamfer=.012)
        if sill>.3:
            block(x,y+.17,.27,width,.35,sill-.27,0,chamfer=.009)
        block(x,y+.17,lintel,width,.35,h-lintel,0,chamfer=.009)
        block(x,y+.20,sill,width,.07,lintel-sill,3)
        for i in range(5):
            block(left+(i+.5)*width/5,y+.145,sill+.035,width/5-.012,.045,lintel-sill-.07,4)
        for v in (.23,.76):
            block(x,y+.102,sill+(lintel-sill)*v,width-.05,.05,.075,3)
        block(x,y-.045,lintel,width+.38,.26,.14,3)
        block(x,y-.035,sill-.065,width+.25,.28,.10,1)
        edge=right
    block((edge+w/2)/2,y+.17,.27,w/2-edge,.35,h-.27,0,chamfer=.012)
    for side in (-1,1):
        for a in (-.22,.22):
            block(side*(w/2-.012),d*a,1.38,.12,.79,.84,1)
            block(side*(w/2+.048),d*a,1.47,.04,.59,.63,3)
            for u in (-.17,0,.17):
                block(side*(w/2+.075),d*a+u,1.47,.05,.065,.65,4)
        for i in range(5):
            block(side*(w/2-.02),d*(i-2)/5,h-.31,.46,.15,.18,3)


def banner(x,y,z):
    # 아래쪽 두 갈래와 접힌 천의 그림자가 실루엣에서도 드러난다.
    rows=[]
    for r in range(6):
        row=[]
        for i in range(9):
            u=i/8
            row.append((x+(u-.5)*1.65,y-.12*math.sin(u*math.pi*4+.3*r)-.045*r,
                        z-r*.55-(.33*abs(2*u-1) if r==5 else 0)))
        rows.append(row)
    for i in range(8):
        for r in range(5):
            quad=[rows[r][i],rows[r+1][i],rows[r+1][i+1],rows[r][i+1]]
            ring_solid([quad,[(a,b-.025,c) for a,b,c in quad]],5)


specs = [
    ("RB_AdobeHouseA",8,6,3.45,-1.55),
    ("RB_AdobeHouseB",9.6,6.8,3.8,1.22),
    ("RB_AdobeAnnex",6,4.2,2.85,-.9),
    ("RB_AdobeWatchtower",6.2,6.2,10.4,0),
]
models=[]
for n,(name,w,d,h,door) in enumerate(specs):
    vertices,faces,indices=[],[],[]
    block(0,0,0,w,d,.32,1,.07,.18)
    if n<3:
        facade(w-.42,d-.55,h,door)
        # 풍화된 기단 석재 줄눈. 양 끝의 이음 위치가 층마다 어긋난다.
        wall_width=w-.42
        for row in range(2):
            pieces=7+n+row
            for j in range(pieces):
                left=-wall_width/2+j*wall_width/pieces
                right=left+wall_width/pieces
                # 문턱 앞은 비워 닫힌 문을 가리지 않는다.
                if left<door+.63 and right>door-.63:
                    continue
                block((left+right)/2,-d/2+.26,.29+row*.19,
                      right-left-.025,.12,.18,1,chamfer=.022)
        # 출입구 양 옆에 돌쌓기와 밝은 상인방을 더한다.
        for side in (-1,1):
            for j in range(5):
                block(door+side*.686,-d/2+.25,.31+j*.4,.205,.19,.39,1,chamfer=.028)
        block(door,-d/2+.19,2.49,1.74,.29,.16,2)
        roof(0,0,h,w-.13,d-.20)
        if n<2:
            uw,ud = (3.35,2.7) if n==0 else (5.2,3.6)
            x,y = (1.5,.85) if n==0 else (-1.4,.78)
            rise=.94 if n==0 else 1.66
            block(x,y,h+.17,uw,ud,rise,0,.09)
            roof(x,y,h+.17+rise,uw+.06,ud+.04)
            for dx in (-.36,0,.36):
                block(x+dx,y-ud/2+.025,h+.42,.17,.09,.32,3)
    else:
        block(0,0,.28,5.93,5.93,.97,1,.17,.19)
        block(0,0,1.23,5.60,5.60,8.95,0,.68,.15)
        for z,size in ((3.2,5.42),(8.7,4.58)):
            block(0,0,z,size,size,.19,1,.04)
        for level in (4.8,7.5):
            span=2.80-.68*(level-1.23)/8.95
            for s in (-1,1):
                block(s*span,0,level,.14,.32,.94,3)
                block(0,s*span,level,.32,.14,.94,3)
        block(0,-2.85,.33,1.25,.12,2.12,3)
        for i in range(5):
            block((i-2)*.23,-2.93,.38,.21,.06,1.99,4)
        for z in (.86,1.8):
            block(0,-2.98,z,1.19,.08,.1,3)
        for s in (-1,1):
            for i in range(-2,3):
                block(i*.88,s*2.22,9.85,.19,.91,.33,3)
        block(0,0,10.13,5.28,5.28,.38,1)
        roof(0,0,10.50,5.40,5.40)
        for a in (-1,1):
            for b in (-1,1):
                block(a*2.16,b*2.16,10.67,.61,.61,2.45,0,.05)
        block(0,0,13.09,5.25,5.25,.22,3)
        roof(0,0,13.31,5.52,5.52)
        block(.65,-2.85,10.72,2.05,.21,.13,3)
        banner(.65,-2.87,10.77)
    mesh=bpy.data.meshes.new(name)
    mesh.from_pydata(vertices,[],faces)
    mesh.update()
    obj=bpy.data.objects.new(name,mesh)
    scene.collection.objects.link(obj)
    for material in materials[:6 if n==3 else 5]:
        mesh.materials.append(material)
    uv=mesh.uv_layers.new(name="UVMap")
    for poly,idx in zip(mesh.polygons,indices):
        poly.material_index=idx
        axis=max(range(3),key=lambda k:abs(poly.normal[k]))
        for li in poly.loop_indices:
            xyz=mesh.vertices[mesh.loops[li].vertex_index].co
            uv.data[li].uv=tuple(xyz[k] for k in range(3) if k!=axis)
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active=obj
    bpy.ops.export_scene.fbx(filepath=str(exports/(name+".fbx")),use_selection=True,
        object_types={"MESH"},axis_forward="-Z",axis_up="Y",bake_space_transform=True,
        apply_scale_options="FBX_SCALE_UNITS",bake_anim=False,add_leaf_bones=False)
    models.append(obj)

# Blender 인수 화면에서 네 모델을 한 번에 비교할 수 있게 배치한다.
for obj,position in zip(models, [(-13,-6),(-4,-2),(5,2),(12,6)]):
    obj.location.xy=position
camera_data=bpy.data.cameras.new("Desert kit review")
camera=bpy.data.objects.new("Desert kit review",camera_data)
scene.collection.objects.link(camera)
camera.location=(22,-38,24)
camera.rotation_euler=(Vector((0,0,5))-camera.location).to_track_quat("-Z","Y").to_euler()
camera_data.type="ORTHO"
camera_data.ortho_scale=39
scene.camera=camera
scene.render.engine="BLENDER_WORKBENCH"
scene.display.shading.color_type="MATERIAL"
scene.display.shading.light="STUDIO"
scene.view_settings.exposure=.85
scene.display.shading.show_shadows=True
scene.display.shading.show_cavity=True
scene.display.shading.cavity_type="WORLD"
scene.display.shading.background_type="WORLD"
scene.world=bpy.data.worlds.new("Desert kit background")
scene.world.color=(.22,.19,.16)
scene.render.resolution_x=1320
scene.render.resolution_y=900
scene.render.resolution_percentage=100
bpy.data.libraries.write(str(root/"DesertArchitecture.blend"),{scene},compress=True)
scene.render.filepath=str(root/"DesertArchitecture.png")
bpy.ops.render.render(write_still=True)
bpy.context.window.scene=previous_scene
