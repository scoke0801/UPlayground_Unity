"""별도 Blender 백그라운드 프로세스에서 수정 가능한 제작 원본을 보존한다."""
import bpy
import os
from pathlib import Path
from mathutils import Vector

project = Path(__file__).resolve().parents[2]
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.name = 'P09_RangerVest_Workshop'
source = project/'Assets/ExternalAssets/Character/P09_Modular_Humanoid/Model_DATA'
bpy.ops.import_scene.fbx(filepath=str(source/'FBX/P09_250802.fbx'),use_anim=False,automatic_bone_orientation=False)
before = set(bpy.data.objects)
bpy.ops.import_scene.fbx(filepath=str(project/'Assets/08.Models/Equipment/P09RangerVest/P09_RangerVest.fbx'),use_anim=False,automatic_bone_orientation=False)
outfit = next(o for o in bpy.data.objects if o not in before and o.type == 'MESH')
outfit.name = 'Male_RangerVest_Chest'
keep = {'Male_Body_Chest','Male_Armor_002_Arm','Male_Armor_002_Waist','Male_Armor_003_Leg','Male_Face_02','Hair_05'}
needed = {outfit}
for obj in bpy.data.objects:
    if obj.name in keep:
        needed.add(obj)
for obj in list(needed):
    parent = obj.parent
    while parent:
        needed.add(parent)
        parent = parent.parent
    for modifier in obj.modifiers:
        if modifier.type == 'ARMATURE' and modifier.object:
            needed.add(modifier.object)
for obj in list(bpy.data.objects):
    if obj not in needed:
        bpy.data.objects.remove(obj,do_unlink=True)
files = {p.name:p for p in (source/'Textures').rglob('*') if p.suffix.lower() in {'.png','.jpg','.tga'}}
for image in bpy.data.images:
    name = image.filepath.replace('\\','/').split('/')[-1]
    if name in files:
        image.filepath=str(files[name]);image.reload()
    elif 'Hair' in name and 'psd' in name:
        image.filepath=str(files['P09_Hair_Brown.png']);image.reload()
for mat in outfit.data.materials:
    mat.use_nodes=True
    shader=mat.node_tree.nodes.get('Principled BSDF')
    if 'Leather' in mat.name:
        tex=mat.node_tree.nodes.new('ShaderNodeTexImage')
        tex.image=bpy.data.images.load(str(project/'Assets/04.Images/Equipment/P09RangerVest/ChestnutLeather_BaseColor.png'))
        mat.node_tree.links.new(tex.outputs['Color'],shader.inputs['Base Color'])
        shader.inputs['Roughness'].default_value=0.78
camera=bpy.data.objects.new('Camera',bpy.data.cameras.new('Camera'))
scene.collection.objects.link(camera);scene.camera=camera
camera.location=(0.8,-2.7,1.3);camera.rotation_euler=(Vector((0,0,0.85))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO';camera.data.ortho_scale=1.85
for name,position,energy in [('Key',(1,-2,3),220),('Fill',(-2,-1,2),100)]:
    data=bpy.data.lights.new(name,'AREA');data.energy=energy;data.shape='DISK';data.size=2
    obj=bpy.data.objects.new(name,data);scene.collection.objects.link(obj);obj.location=position
    obj.rotation_euler=(Vector((0,0,1))-obj.location).to_track_quat('-Z','Y').to_euler()
scene.render.engine='CYCLES';scene.cycles.samples=32
scene.render.resolution_x=900;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
bpy.ops.object.select_all(action='DESELECT');outfit.select_set(True);bpy.context.view_layer.objects.active=outfit
bpy.ops.wm.save_as_mainfile(filepath=str(project/'ArtSource/P09RangerVest/P09_RangerVest.blend'),compress=True)
print('P09 source saved')
