import bpy
from mathutils import Vector
scene=bpy.context.scene
for collection in scene.collection.children:
    for o in collection.objects:
        o.hide_render=(collection.name!="RBElf_Hall" or not o.name.endswith("LOD0"))
for mat in bpy.data.materials:
    if not mat.name.startswith("RBElf_"):
        continue
    rgb=mat.diffuse_color[:]
    mat.use_nodes=True
    bs=mat.node_tree.nodes.get("Principled BSDF")
    bs.inputs["Base Color"].default_value=rgb
    bs.inputs["Roughness"].default_value=.47
    if "Verdigris" in mat.name or "Bronze" in mat.name:
        bs.inputs["Metallic"].default_value=.36
    if mat.name.endswith("Glass"):
        bs.inputs["Emission Color"].default_value=(1,.37,.10,1)
        bs.inputs["Emission Strength"].default_value=1.7
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.025))
ground=bpy.context.object
m=bpy.data.materials.new("Ground")
m.use_nodes=True
m.node_tree.nodes.get("Principled BSDF").inputs["Base Color"].default_value=(.115,.12,.105,1)
ground.data.materials.append(m)
scene.world=bpy.data.worlds.new("Studio")
scene.world.use_nodes=True
scene.world.node_tree.nodes.get("Background").inputs[0].default_value=(.33,.42,.53,1)
scene.world.node_tree.nodes.get("Background").inputs[1].default_value=.26
def aim(o,p):
    o.rotation_euler=(Vector(p)-o.location).to_track_quat("-Z","Y").to_euler()
def light(name,location,color,power,size):
    data=bpy.data.lights.new(name,"AREA")
    data.energy=power
    data.size=size
    data.color=color
    obj=bpy.data.objects.new(name,data)
    scene.collection.objects.link(obj)
    obj.location=location
    aim(obj,(0,0,3))
light("Key",(2,-9,13),(1,.78,.54),2900,9)
light("Fill",(-8,-2,7),(.55,.74,1),1500,7)
light("Rim",(4,6,12),(.64,.87,1),2100,7)
bpy.ops.object.camera_add(location=(15,-19,10.5))
scene.camera=bpy.context.object
scene.camera.data.lens=51
aim(scene.camera,(0,0,4))
scene.render.engine="CYCLES"
scene.cycles.samples=24
scene.cycles.use_denoising=True
scene.render.resolution_x=980
scene.render.resolution_y=730
scene.render.resolution_percentage=100
scene.render.image_settings.file_format="JPEG"
scene.render.image_settings.quality=93
scene.render.filepath="//../../../docs/work-status/river-basin-region-sites-evidence/elf-art-blender-hall.jpg"
bpy.ops.render.render(write_still=True)
