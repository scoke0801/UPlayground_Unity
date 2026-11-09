import bpy, math, random, json, os
from mathutils import Vector
root='C:/UsingProject/UnityProject/UPlayground/'
random.seed(8143)
scene=bpy.data.scenes.new('RiverBasinWorld_B1_ArtR1')
bpy.context.window.scene=scene
scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
stone=bpy.data.materials.new('MAT_B1_GraniteR1');stone.diffuse_color=(.66,.61,.50,1)
stone.use_nodes=True
image=bpy.data.images.load(root+'Assets/04.Images/World/RiverBasinWorld/VisualR1/GraniteAlbedo.png',check_existing=True)
tex=stone.node_tree.nodes.new('ShaderNodeTexImage');tex.image=image
stone.node_tree.links.new(tex.outputs['Color'],stone.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
stone.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.84
parts=[]
def box(name,center,size,bevel=0):
 bpy.ops.mesh.primitive_cube_add(size=1,location=center)
 o=bpy.context.object;o.name=name;o.dimensions=size
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  m=o.modifiers.new('ConstructionEdge','BEVEL');m.width=bevel;m.segments=1
  bpy.ops.object.modifier_apply(modifier=m.name)
 parts.append(o);return o
def wedge(name,cy,a0,a1,ri,ro):
 verts=[]
 for x in [-2.7,2.7]:
  for r,a in [(ri,a0),(ri,a1),(ro,a1),(ro,a0)]:verts.append((x,cy+r*math.cos(a),-5.4+r*math.sin(a)))
 faces=[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
 o=bpy.data.objects.new(name,mesh);scene.collection.objects.link(o);parts.append(o)
 # 아치 외측에서 상판까지 채워 구조적으로 비어 보이는 틈을 막는다.
 y0=cy+ro*math.cos(a0);y1=cy+ro*math.cos(a1)
 z0=-5.4+ro*math.sin(a0);z1=-5.4+ro*math.sin(a1)
 v=[(x,y,z) for x in [-2.65,2.65] for y,z in [(y0,z0),(y1,z1),(y1,-.45),(y0,-.45)]]
 mesh=bpy.data.meshes.new('spandrel');mesh.from_pydata(v,[],faces);mesh.update();o=bpy.data.objects.new('spandrel',mesh);scene.collection.objects.link(o);parts.append(o)
def finish(name):
 bpy.ops.object.select_all(action='DESELECT')
 for o in parts:o.select_set(True)
 bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
 scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
 # 결합된 조각의 면 방향을 바깥쪽으로 맞춘다.
 bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')
 o.data.materials.clear();o.data.materials.append(stone)
 for layer in list(o.data.uv_layers):o.data.uv_layers.remove(layer)
 uv=o.data.uv_layers.new(name='Granite_2m')
 for poly in o.data.polygons:
  axis=max(range(3),key=lambda j:abs(poly.normal[j]));axes=[j for j in range(3) if j!=axis]
  for li in poly.loop_indices:
   p=o.data.vertices[o.data.loops[li].vertex_index].co;uv.data[li].uv=(p[axes[0]]/2,p[axes[1]]/2)
 return o
objects=[]
for lod in range(3):
 parts=[]
 box('continuous_deck',(0,0,-.31),(5.6,36,.44),.025 if lod==0 else 0)
 if lod==0:
  for row in range(28):
   for col in range(5):
    x=-2.13+col*1.065;y=-17.34+row*1.284
    box('paving',(x,y,-.064),(1.046,1.262,.12),.018)
 else:box('paving',(0,0,-.064),(5.34,35.92,.12))
 for cy in [-8,0,8]:
  count=16 if lod==0 else 10 if lod==1 else 6
  for i in range(count):wedge('voussoir',cy,math.pi*i/count+.003,math.pi*(i+1)/count-.003,3.48,4.0)
 for y in [-12,-4,4,12]:
  box('pier',(0,y,-3.0),(5.4,1.05,5),.035 if lod==0 else 0)
  for x in [-2.8,2.8]:box('cutwater',(x,y,-3.1),(.5,1.45,4.6),.08 if lod==0 else 0)
 for y in [-15.65,15.65]:box('abutment',(0,y,-2.4),(5.5,4.65,4.1),.04 if lod==0 else 0)
 for x in [-2.68,2.68]:
  if lod==0:
   for row in range(3):
    for col in range(20):box('parapet_stone',(x,-17.1+col*1.8+(row%2)*.05,.13+row*.25),(.24,1.77,.24),.012)
  else:box('parapet',(x,0,.38),(.24,35.8,.76))
  for y in [-17.3,-12,-6,0,6,12,17.3]:
   box('newel',(x,y,.46),(.45,.48,.92),.03 if lod==0 else 0)
   box('cap',(x,y,.97),(.6,.64,.12),.025 if lod==0 else 0)
 objects.append(finish('SM_B1_ArtR1_LOD'+str(lod)))
for o in objects:o.hide_set(False);o.hide_render=False
bpy.ops.object.select_all(action='DESELECT')
for o in objects:o.select_set(True)
bpy.context.view_layer.objects.active=objects[0]
bpy.ops.export_scene.fbx(filepath=root+'Assets/08.Models/World/RiverBasinWorld/VisualR1/B1_BridgeArtR1.fbx',use_selection=True,object_types={'MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
for i,o in enumerate(objects):o.hide_set(i>0);o.hide_render=i>0
bpy.ops.wm.save_as_mainfile(filepath=root+'Tools/RiverBasinWorld/VisualR1/Source/B1_BridgeArtR1.blend')
audit=[{'name':o.name,'triangles':sum(len(p.vertices)-2 for p in o.data.polygons),'dimensions':list(o.dimensions),'uv':o.data.uv_layers[0].name,'scale':list(o.scale)} for o in objects]
open(root+'output/RiverBasinWorld/VisualR1/blender-audit.json','w').write(json.dumps(audit,indent=2))
result={'audit':audit,'source':bpy.data.filepath}
