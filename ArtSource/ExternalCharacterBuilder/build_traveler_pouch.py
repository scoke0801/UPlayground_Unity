"""ImageGen 참고 시트를 바탕으로 본 부착용 가죽 파우치를 만든다. 원본 캐릭터는 수정하지 않는다."""
import bpy
import json
from pathlib import Path

ROOT = Path(r'C:/UsingProject/UnityProject/UPlayground')
output = ROOT / 'Assets/08.Models/Equipment/TravelerPouch'
output.mkdir(parents=True, exist_ok=True)
scene = bpy.data.scenes.new('TravelerPouch_Authoring')
previous = bpy.context.window.scene
bpy.context.window.scene = scene

def material(name, color, metallic=0):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Roughness'].default_value = 0.65
    bsdf.inputs['Metallic'].default_value = metallic
    return mat

navy = material('Pouch_NavyLeather', (0.035, 0.048, 0.067))
brown = material('Pouch_BrownLeather', (0.24, 0.09, 0.055))
brass = material('Pouch_AntiqueBrass', (0.40, 0.27, 0.10), 0.65)
objects = []

def block(name, center, size, mat, bevel):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    modifier = obj.modifiers.new('RoundedEdges', 'BEVEL')
    modifier.width = bevel
    modifier.segments = 3
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    obj.data.materials.append(mat)
    objects.append(obj)
    return obj

try:
    block('PouchBody', (0, 0, -0.07), (0.18, 0.05, 0.14), navy, 0.012)
    block('Flap', (0, -0.029, -0.029), (0.178, 0.012, 0.075), brown, 0.01)
    block('ClosureStrap', (0, -0.037, -0.075), (0.022, 0.008, 0.052), navy, 0.005)
    block('BrassClasp', (0, -0.043, -0.068), (0.028, 0.006, 0.013), brass, 0.003)
    block('ClaspTip', (0, -0.042, -0.086), (0.014, 0.006, 0.023), brass, 0.005)
    for x in [-0.054, 0.054]:
        block('BeltLoop', (x, 0.029, -0.024), (0.023, 0.012, 0.064), navy, 0.004)
        block('LoopRivet', (x, 0.036, -0.042), (0.01, 0.004, 0.01), brass, 0.004)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    pouch = bpy.context.object
    pouch.name = 'TravelerPouch'
    scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.export_scene.fbx(filepath=str(output / 'TravelerPouch.fbx'), use_selection=True,
                             object_types={'MESH'}, apply_unit_scale=True, axis_forward='-Z', axis_up='Y',
                             bake_anim=False, add_leaf_bones=False)
    source_path = ROOT / 'ArtSource/ExternalCharacterBuilder/TravelerPouch.blend'
    bpy.data.libraries.write(str(source_path), {scene}, fake_user=True)
    report = {'vertices': len(pouch.data.vertices), 'polygons': len(pouch.data.polygons),
              'dimensions_m': list(pouch.dimensions), 'fbx': str(output / 'TravelerPouch.fbx')}
    (ROOT / 'output/ExternalCharacterBuilder/pouch_blender.json').write_text(json.dumps(report, indent=2))
    print(json.dumps(report))
finally:
    bpy.context.window.scene = previous
