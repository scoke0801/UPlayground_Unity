# B3 현수교 원본. X 폭, Y 교량 방향, Z 높이. 데크 양 끝 상판 Z=0.
import bpy
import math
import random
from pathlib import Path
from mathutils import Vector

root = Path(__file__).resolve().parents[3]
source = root / "Tools/RiverBasinWorld/GorgeRelief/B3_SuspensionBridge.blend"
model = root / "Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Authoring/GorgeReliefArt/B3_SuspensionBridge.fbx"
model.parent.mkdir(parents=True, exist_ok=True)

scene = bpy.data.scenes.new("B3 Suspension Bridge")
bpy.context.window.scene = scene
scene.unit_settings.system = 'METRIC'
bpy.context.preferences.filepaths.save_version = 0
colors = {
    "Deck_Warm": (.29,.17,.09,1), "Deck_Pale": (.39,.26,.15,1),
    "Deck_Dark": (.20,.115,.06,1), "Timber": (.22,.125,.068,1),
    "Hemp": (.38,.27,.14,1), "Iron": (.07,.065,.058,1)
}
materials = {}
for name, color in colors.items():
    material = bpy.data.materials.new("B3_" + name)
    material.diffuse_color = color
    materials[name] = material

def sag(y):
    return -.65 * (1 - min(abs(y)/23,1)**2)

def cable(y):
    return .78 + 1.93*(abs(y)/22.5)**2

def stay_curve(side, end, sections):
    # 기둥 연결부에서 지면 앵커까지 장력이 이어지는 단일 곡선.
    a, b, c, d = (Vector((side*x, end*y, z)) for x,y,z in (
        (1.33,22.8,2.74), (1.41,23.61,1.49), (1.57,25.31,.35), (1.62,26.55,.16)))
    for i in range(sections+1):
        t = i/sections
        u = 1-t
        yield a*u**3 + 3*b*u*u*t + 3*c*u*t*t + d*t**3

class Batch:
    def __init__(self):
        self.verts, self.faces, self.uv = [], [], []

    def face(self, indices, uv):
        self.faces.append(indices)
        self.uv.append(uv)

    def beam(self, a, b, width, depth):
        a, b = Vector(a),Vector(b)
        tangent = (b-a).normalized()
        side = tangent.cross(Vector((0,0,1)))
        if side.length < .05:
            side = tangent.cross(Vector((1,0,0)))
        side.normalize()
        up = tangent.cross(side).normalized()
        first = len(self.verts)
        for center in (a,b):
            for x,z in ((-1,-1),(1,-1),(1,1),(-1,1)):
                self.verts.append(center + side*(width*x/2) + up*(depth*z/2))
        self.face(tuple(first+i for i in (0,3,2,1)), ((0,0),(0,depth),(width,depth),(width,0)))
        self.face(tuple(first+i for i in (4,5,6,7)), ((0,0),(width,0),(width,depth),(0,depth)))
        span = (b-a).length
        for i in range(4):
            j = (i+1)%4
            t = width if i%2==0 else depth
            self.face((first+i,first+j,first+j+4,first+i+4), ((0,0),(t,0),(t,span),(0,span)))

    def rope(self, points, radius, sides, twist=0.0, closed=False):
        # EastLands 아틀라스에서 매듭과 흰 여백을 제외한 상단 섬유 띠만 표본화한다.
        def fiber_uv(around, distance):
            return distance * 1.6, .79 + around * .194

        points = [Vector(p) for p in points]
        first = len(self.verts)
        length = 0
        for i,point in enumerate(points):
            prior = length
            if i:
                length += (point-points[i-1]).length
            tangent = (points[min(i+1,len(points)-1)]-points[max(i-1,0)]).normalized()
            x = tangent.cross(Vector((0,0,1)))
            if x.length < .08:
                x = tangent.cross(Vector((1,0,0)))
            x.normalize()
            z = tangent.cross(x)
            for j in range(sides):
                theta = j*math.tau/sides
                # 교대에서 보이는 밧줄은 세 가닥이 꼬인 단면의 굴곡을 메시에도 넣는다.
                strand = 1 - twist * (.5+.5*math.cos(3*theta-length*math.tau/.27))
                self.verts.append(point + radius*strand*(math.cos(theta)*x+math.sin(theta)*z))
            if i == 0:
                continue
            for j in range(sides):
                a = first+(i-1)*sides+j
                b = first+(i-1)*sides+(j+1)%sides
                u0,u1 = j/sides,(j+1)/sides
                self.face((a,b,b+sides,a+sides),(
                    fiber_uv(u0,prior),fiber_uv(u1,prior),fiber_uv(u1,length),fiber_uv(u0,length)))
        if not closed:
            last = first + (len(points)-1)*sides
            uv_ring = tuple(fiber_uv(.5+.42*math.cos(j*math.tau/sides),.32+.05*math.sin(j*math.tau/sides)) for j in range(sides))
            self.face(tuple(first+j for j in reversed(range(sides))), tuple(reversed(uv_ring)))
            self.face(tuple(last+j for j in range(sides)), uv_ring)

    def object(self, name, material, collection, bevel):
        mesh = bpy.data.meshes.new(name)
        mesh.from_pydata(self.verts, [], self.faces)
        mesh.validate()
        mesh.update()
        mesh.uv_layers.new(name="Bridge_Tile_UV")
        uv = mesh.uv_layers.active.data
        for face, coordinates in zip(mesh.polygons,self.uv):
            for index,xy in zip(face.loop_indices,coordinates):
                uv[index].uv = xy
        obj = bpy.data.objects.new(name,mesh)
        collection.objects.link(obj)
        mesh.materials.append(material)
        if bevel:
            mod = obj.modifiers.new("Worn edges","BEVEL")
            mod.width = bevel
            mod.segments = 1
            mod.affect = 'EDGES'
            mod.harden_normals = True
            obj.modifiers.new("Face normals","WEIGHTED_NORMAL")
        if material.name.endswith("Hemp"):
            for f in mesh.polygons:
                f.use_smooth = True
        return obj

def make_bridge(lod):
    rng = random.Random(4316)
    bands = ["Deck_Warm","Deck_Pale","Deck_Dark"]
    bins = {name: Batch() for name in colors}
    posts = bins["Timber"]
    ropes = bins["Hemp"]
    iron = bins["Iron"]
    boards = 164 if lod == 0 else 82 if lod == 1 else 23
    stride = 46.15/boards
    for i in range(boards):
        y = -46.15/2+stride*(i+.5)
        x = rng.uniform(-.038,.038) if lod < 2 else 0
        w = rng.uniform(1.18,1.23)
        deck = sag(y) - .07
        bins[rng.choices(bands, [.55,.29,.16])[0]].beam((x-w,y,deck),(x+w,y,deck),
            stride-.019 if lod < 2 else stride, .14)
    steps = 24 if lod < 2 else 12
    for sign in (-1,1):
        for i in range(steps):
            a,b = -23+46*i/steps,-23+46*(i+1)/steps
            posts.beam((sign*1.0,a,sag(a)-.23),(sign*1.0,b+.03,sag(b)-.23),.15,.23)
        sides = 18 if lod==0 else 8 if lod==1 else 5
        rail_sides = 12 if lod==0 else sides
        samples = 84 if lod==0 else 42 if lod==1 else 18
        ys = [-22.5+45*i/samples for i in range(samples+1)]
        rail_samples = 48 if lod==0 else samples
        rail_y = [-22.5+45*i/rail_samples for i in range(rail_samples+1)]
        ropes.rope([(sign*1.33,y,cable(y)+sag(y)) for y in ys],.070,sides)
        ropes.rope([(sign*1.13,y,1.04+sag(y)) for y in rail_y],.052,rail_sides)
        ropes.rope([(sign*1.14,y,.53+sag(y)) for y in rail_y],.034,rail_sides)
        if lod<2:
            ropes.rope([(sign*1.21,y,-.16+sag(y)) for y in rail_y],.054,rail_sides)
        for i in range(1,31,1 if lod==0 else 2 if lod==1 else 5):
            y = -22.5+1.5*i
            floor = sag(y)
            posts.beam((sign*1.16,y,floor-.22),(sign*1.13,y+.05,floor+1.10),.087,.09)
            if lod<2:
                ropes.rope([(sign*1.23,y,floor-.2),(sign*1.33,y+.1,floor+cable(y))],.028,rail_sides)
            if lod==0 and i%2==0:
                ropes.rope([(sign*1.15,y,.49+floor),(sign*1.14,y+.7,.98+floor),(sign*1.15,y+1.5,.49+floor)],.016,8)
        for end in (-1,1):
            y = end*22.8
            posts.beam((sign*1.38,y,-.64),(sign*1.29,y-end*.13,3.12),.29,.31)
            posts.beam((sign*1.43,y-end*.19,.27),(sign*1.54,y+end*2.08,-.17),.23,.25)
            posts.beam((sign*1.05,y,-.08),(sign*1.45,y-end*1.1,1.42),.16,.18)
            ropes.rope(stay_curve(sign,end,(76,24,10)[lod]),.079,(24,12,6)[lod],twist=.115 if lod==0 else 0)
            # 밧줄 끝은 교대 바깥 지면에 묻힌 말뚝의 철제 머리 안으로 들어간다.
            posts.beam((sign*1.65,end*26.71,-.84),(sign*1.62,end*26.59,.34),.25,.28)
            iron.beam((sign*1.62,end*26.59,.10),(sign*1.62,end*26.59,.265),.34,.29)
            iron.beam((sign*1.31,y,.21),(sign*1.31,y,.34),.34,.34)
            iron.beam((sign*1.30,y,2.68),(sign*1.30,y,2.78),.34,.35)
            if lod==0:
                for height in (.45,.58,2.83,2.91):
                    pts = [(sign*1.30+.17*math.cos(a*math.tau/24),y+.18*math.sin(a*math.tau/24),height)
                           for a in range(25)]
                    ropes.rope(pts,.027,8,closed=True)
    for end in (-1,1):
        y=end*22.8
        posts.beam((-1.51,y,2.95),(1.51,y,2.95),.25,.26)
        posts.beam((-1.52,y,-.28),(1.52,y,-.28),.29,.30)
        for sign in (-1,1):
            posts.beam((sign*1.33,y,2.33),(sign*.66,y,2.95),.14,.18)
    collection = bpy.data.collections.new("B3_LOD%d"%lod)
    scene.collection.children.link(collection)
    return [batch.object("B3_%s_LOD%d"%(name,lod),materials[name],collection,
                         (.014 if name.startswith("Deck") else .023) if lod==0 and name!="Hemp" else 0)
            for name,batch in bins.items() if batch.faces]

objects = [make_bridge(lod) for lod in range(3)]
bpy.ops.object.select_all(action='DESELECT')
for group in objects:
    for obj in group:
        obj.select_set(True)
bpy.context.view_layer.objects.active = objects[0][0]
bpy.ops.export_scene.fbx(filepath=str(model),use_selection=True,object_types={'MESH'},
    apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',
    use_mesh_modifiers=True,bake_anim=False,add_leaf_bones=False)
for group in objects[1:]:
    for obj in group:
        obj.hide_set(True)
        obj.hide_render = True
bpy.ops.wm.save_as_mainfile(filepath=str(source))
deps = bpy.context.evaluated_depsgraph_get()
print("B3 bridge mesh triangles",[(lod,sum(sum(len(p.vertices)-2 for p in o.evaluated_get(deps).data.polygons) for o in group)) for lod,group in enumerate(objects)])
