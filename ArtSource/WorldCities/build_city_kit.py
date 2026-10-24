"""Blender MCP에서 실행하는 항구·성 도시 건물 원본. 기존 Blender 작업은 보존한다."""
import bpy
import json
import math
from pathlib import Path
from mathutils import Vector

ROOT = Path('C:/UsingProject/UnityProject/UPlayground')
SOURCE = ROOT / 'ArtSource/WorldCities'
OUTPUT = ROOT / 'Assets/08.Models/WorldCities'
TEXTURE = ROOT / 'Assets/04.Images/Environment/WorldCities/CityMaterialsAtlas.png'
SOURCE.mkdir(parents=True, exist_ok=True)
OUTPUT.mkdir(parents=True, exist_ok=True)
scene = bpy.data.scenes.new('WorldCities_Architecture')
bpy.context.window.scene = scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
atlas = bpy.data.materials.new('CityMaterialsAtlas')
atlas.use_nodes = True
node = atlas.node_tree.nodes.new('ShaderNodeTexImage')
node.image = bpy.data.images.load(str(TEXTURE), check_existing=True)
atlas.node_tree.links.new(node.outputs['Color'], atlas.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
atlas.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value = .84
tiles = {'stone':(0,1), 'oak':(1,1), 'plaster':(2,1), 'slate':(0,0), 'clay':(1,0), 'timber':(2,0)}
parts = []
assets = []

def face(name, corners, tile, repeat=3):
    """큰 면에서 돌무늬가 늘어나지 않도록 실제 길이에 맞춰 면을 나눈다."""
    a,b,c,d = [Vector(v) for v in corners]
    nx=max(1,math.ceil((b-a).length/repeat))
    ny=max(1,math.ceil((d-a).length/repeat))
    vertices=[]; faces=[]
    for y in range(ny+1):
        for x in range(nx+1):
            u=x/nx; v=y/ny
            vertices.append(a*(1-u)*(1-v)+b*u*(1-v)+c*u*v+d*(1-u)*v)
    for y in range(ny):
        for x in range(nx):
            i=y*(nx+1)+x
            faces.append((i,i+1,i+nx+2,i+nx+1))
    data=bpy.data.meshes.new(name)
    data.from_pydata(vertices,[],faces); data.update()
    obj=bpy.data.objects.new(name,data); scene.collection.objects.link(obj)
    data.materials.append(atlas)
    uv=data.uv_layers.new(name='UVMap')
    tx,ty=tiles[tile]
    for poly in data.polygons:
        for li,coord in zip(poly.loop_indices,((0,0),(1,0),(1,1),(0,1))):
            uv.data[li].uv=((tx+.018+coord[0]*.964)/3,(ty+.018+coord[1]*.964)/2)
    parts.append(obj)
    return obj

def box(name, center, size, tile='timber'):
    x,y,z=center; a,b,c=[v/2 for v in size]
    v=[(x-a,y-b,z-c),(x+a,y-b,z-c),(x+a,y+b,z-c),(x-a,y+b,z-c),
       (x-a,y-b,z+c),(x+a,y-b,z+c),(x+a,y+b,z+c),(x-a,y+b,z+c)]
    for i,f in enumerate(((0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7))):
        face(name+str(i),[v[j] for j in f],tile)

def beam(name,a,b,width=.18,tile='timber'):
    start=len(parts)
    a,b=Vector(a),Vector(b)
    box(name,(0,0,0),(width,width,(b-a).length),tile)
    rotation=(b-a).to_track_quat('Z','Y')
    for obj in parts[start:]:
        for v in obj.data.vertices: v.co=rotation@v.co+(a+b)/2

def ring(name,x,y,z,radius,height,tile='stone',segments=12,top_radius=None):
    top_radius=radius if top_radius is None else top_radius
    for i in range(segments):
        a=2*math.pi*i/segments; b=2*math.pi*(i+1)/segments
        face(name,[(x+radius*math.cos(a),y+radius*math.sin(a),z),
            (x+radius*math.cos(b),y+radius*math.sin(b),z),
            (x+top_radius*math.cos(b),y+top_radius*math.sin(b),z+height),
            (x+top_radius*math.cos(a),y+top_radius*math.sin(a),z+height)],tile)

def roof(name,x,y,z,w,d,rise,tile):
    for side in (-1,1):
        face(name,[(x,y-d/2,z+rise),(x+side*w/2,y-d/2,z),(x+side*w/2,y+d/2,z),(x,y+d/2,z+rise)],tile,2.5)
        beam('Eave',(x+side*w/2,y-d/2,z),(x+side*w/2,y+d/2,z),.22)
        for end in (-1,1): beam('GableTrim',(x,y+end*d/2,z+rise),(x+side*w/2,y+end*d/2,z),.22)
    beam('Ridge',(x,y-d/2,z+rise+.08),(x,y+d/2,z+rise+.08),.22)
    for end in (-1,1):
        face('Gable',[(x-w/2+.35,y+end*(d/2-.35),z),(x+w/2-.35,y+end*(d/2-.35),z),
            (x,y+end*(d/2-.35),z+rise-.2),(x,y+end*(d/2-.35),z+rise-.2)],'plaster')

def window(x,y,z,w=1.35,h=1.7):
    box('WindowDark',(x,y,z),(w,.15,h),'timber')
    for side in (-1,1):
        box('Shutter',(x+side*(w/2+.28),y-.1,z),(.46,.16,h),'oak')
        box('WindowFrame',(x+side*w/2,y-.18,z),(.12,.2,h+.15),'oak')
    for zz in (z-h/2,z+h/2,z):box('WindowBar',(x,y-.22,zz),(w+.15,.22,.10),'oak')
    box('WindowMullion',(x,y-.23,z),(.09,.22,h),'oak')
    box('StoneSill',(x,y-.2,z-h/2-.12),(w+1,.55,.22),'stone')

def house(name,w=9,d=8,h=6.2,tile='slate'):
    box('Foundation',(0,0,.35),(w+.35,d+.35,.7),'stone')
    box('Walls',(0,0,h/2+.7),(w,d,h),'plaster')
    for x in (-w/2,0,w/2):
        for y in (-d/2-.06,d/2+.06):box('FramePost',(x,y,h/2+.7),(.24,.25,h+.2))
    for z in (.9,3.6,h+.7):box('Belt',(0,0,z),(w+.3,d+.3,.24))
    roof('Roof',0,0,h+.7,w+1.1,d+1.2,3.2,tile)
    box('Door',(0,-d/2-.15,1.95),(1.6,.22,2.5),'oak')
    box('DoorLintel',(0,-d/2-.28,3.3),(2.15,.4,.27),'stone')
    for x in (-1,1):box('DoorPost',(x,-d/2-.2,1.95),(.23,.35,2.5),'stone')
    for x in (-w*.30,w*.30):
        window(x,-d/2-.18,2.5,1.2,1.5)
        window(x,-d/2-.18,5.1,1.2,1.5)
    window(0,-d/2-.18,5.15)
    box('Chimney',(w*.28,d*.18,h+3),(1,1.1,3.6),'stone')
    box('ChimneyCap',(w*.28,d*.18,h+4.85),(1.3,1.4,.3),'stone')
    for x in (-w/2+.2,w/2-.2):beam('Brace',(x,-d/2-.22,3.7),(x-math.copysign(1.3,x),-d/2-.22,5.3),.18)
    finish(name)

def finish(name):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in parts:obj.select_set(True)
    bpy.context.view_layer.objects.active=parts[0]
    bpy.ops.object.join()
    obj=bpy.context.object; obj.name=name
    bpy.context.scene.cursor.location=(0,0,0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    bpy.ops.export_scene.fbx(filepath=str(OUTPUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},
        axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,
        use_mesh_modifiers=True,add_leaf_bones=False,path_mode='AUTO')
    assets.append({'name':name,'vertices':len(obj.data.vertices),'triangles':sum(len(p.vertices)-2 for p in obj.data.polygons),'uv':len(obj.data.uv_layers),'file':str(OUTPUT/(name+'.fbx'))})
    obj.hide_set(True); parts.clear()

house('HarborHouse')
house('CastleTownHouse',8,8,6.8,'clay')
house('HarborWarehouse',15,11,7.3,'slate')

# 성문 양옆에 탑을 세우고 가운데 8m 폭의 실제 통로를 남긴다.
for x in (-8,8):
    box('GateTower',(x,0,6),(7,9,12),'stone')
    box('TowerCornice',(x,0,11.6),(7.6,9.6,.5),'stone')
    roof('TowerRoof',x,0,12,8,10,4,'clay')
    window(x,-4.6,7,1.4,2.5)
    box('TowerFoot',(x,0,.6),(7.6,9.6,1.2),'stone')
box('GateBridge',(0,0,10.4),(9,6,3.2),'stone')
for x in (-3.8,3.8):box('GatePillar',(x,0,4),(1,6,8),'stone')
for i in range(12):
    a=math.pi*i/12; b=math.pi*(i+1)/12
    face('ArchStone',[(-3.3*math.cos(a),-3.12,4.8+3.3*math.sin(a)),(-3.3*math.cos(b),-3.12,4.8+3.3*math.sin(b)),(-4*math.cos(b),-3.12,4.8+4*math.sin(b)),(-4*math.cos(a),-3.12,4.8+4*math.sin(a))],'stone')
for x in range(-4,5,2):box('Merlon',(x,0,12.5),(1,6,1.4),'stone')
finish('CastleGatehouse')

box('Wall',(0,0,3.5),(12,2.8,7),'stone')
box('WallCornice',(0,0,7),(12.4,3.2,.5),'stone')
for x in (-5,-3,-1,1,3,5):box('Crenellation',(x,0,7.9),(1,3,1.4),'stone')
finish('CastleWall')

ring('Tower',0,0,0,4,12)
ring('TowerLip',0,0,11.3,4.3,.7)
for i in range(12):
    a=i*math.pi/6
    box('Merlon',(3.75*math.cos(a),3.75*math.sin(a),12.65),(1.25,1.25,1.6),'stone')
ring('TowerRoof',0,0,12,3.8,5,'clay',12,.08)
finish('CastleTower')

box('KeepMain',(0,0,8),(19,16,16),'stone')
box('KeepUpper',(0,0,16.7),(16,13,2),'plaster')
roof('KeepRoof',0,0,17.7,18,15,5,'clay')
for x in (-10.5,10.5):
    for y in (-8,8):
        ring('KeepTurret',x,y,0,3.1,18)
        ring('KeepTurretRoof',x,y,18,3.8,6,'clay',12,.05)
for x in (-5,0,5):
    window(x,-8.1,7,1.6,3)
    window(x,-8.1,12,1.6,3)
box('KeepDoor',(0,-8.15,2.5),(3,.3,5),'oak')
finish('CastleKeep')

box('Deck',(0,0,.1),(7,18,.4),'oak')
for x in (-3.25,3.25):
    for y in (-8,-4,0,4,8):
        box('PierPost',(x,y,-.6),(.5,.5,2.8),'timber')
    beam('Rail',(x,-8,1),(x,8,1),.16)
finish('HarborPier')

# 선체는 선수·선미로 갈수록 좁아지고, 내부 갑판과 용골이 실제 메시다.
ys=[-6,-4,-1,2,4,6]; widths=[.15,1.6,2.1,2.1,1.5,.2]
for i in range(5):
    for side in (-1,1):
        face('Hull',[(side*widths[i]*.55,ys[i],-.8),(side*widths[i+1]*.55,ys[i+1],-.8),(side*widths[i+1],ys[i+1],1),(side*widths[i],ys[i],1)],'oak')
        beam('Gunwale',(side*widths[i],ys[i],1),(side*widths[i+1],ys[i+1],1),.18)
    face('Deck',[(-widths[i],ys[i],.7),(widths[i],ys[i],.7),(widths[i+1],ys[i+1],.7),(-widths[i+1],ys[i+1],.7)],'oak')
beam('Mast',(0,0,.6),(0,0,9),.25)
beam('Boom',(-3,0,7),(3,0,7),.18)
face('Sail',[(-2.8,.05,7),(2.8,.05,7),(2.2,.25,3),(-2.2,.25,3)],'plaster')
finish('CoastalBoat')

for x in (-2,2):
    for y in (-1.5,1.5):box('StallPost',(x,y,1.5),(.18,.18,3))
box('Counter',(0,-1.1,1),(4.3,1,1.8),'oak')
roof('Awning',0,0,2.8,4.9,4,.6,'slate')
for x in (-1.4,0,1.4):box('Crate',(x,-1.2,2.1),(1,.65,.3),'oak')
finish('MarketStall')

ring('Basin',0,0,0,3,.65,'stone',16)
ring('BasinRim',0,0,.65,3.15,.25,'stone',16)
ring('FountainColumn',0,0,0,.55,2.6,'stone',8)
ring('UpperBowl',0,0,2.4,1.3,.4,'stone',12)
finish('TownFountain')

for name in ('TravelSign',):
    box('Post',(0,0,1.4),(.2,.2,2.8))
    box('Board',(0,0,2.2),(2.2,.18,.65),'oak')
    roof('SignRoof',0,0,2.65,2.5,.7,.4,'slate')
    finish(name)

for i,obj in enumerate([o for o in scene.objects if o.type=='MESH']):
    obj.hide_set(False); obj.location=(i%4*38,i//4*36,0)
scene.world=bpy.data.worlds.new('CitiesPreviewWorld')
scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.35,.43,.55,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.7
light_data=bpy.data.lights.new('Sun','SUN'); light_data.energy=3
light=bpy.data.objects.new('Sun',light_data);scene.collection.objects.link(light)
light.rotation_euler=(.45,-.6,-.5)
camera_data=bpy.data.cameras.new('KitPreview'); camera=bpy.data.objects.new('KitPreview',camera_data)
scene.collection.objects.link(camera);camera.location=(150,-125,120)
camera.rotation_euler=(Vector((50,30,5))-camera.location).to_track_quat('-Z','Y').to_euler()
camera_data.type='ORTHO';camera_data.ortho_scale=175;scene.camera=camera
scene.render.engine='CYCLES';scene.cycles.samples=24
scene.render.resolution_x=1600;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.render.filepath=str(SOURCE/'kit_preview.png')
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'WorldCities.blend'),copy=True)
(SOURCE/'mesh_validation.json').write_text(json.dumps(assets,ensure_ascii=False,indent=2),encoding='utf-8')
assert len(assets)==12 and all(a['uv']==1 and a['triangles']>0 for a in assets)
print(json.dumps({'assets':len(assets),'triangles':sum(a['triangles'] for a in assets),'output':str(OUTPUT)}))
