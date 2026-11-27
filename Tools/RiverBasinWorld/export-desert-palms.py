from pathlib import Path
import bpy
from mathutils import Matrix, Vector

world = Path("Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34")
output = world / "Authoring" / "DesertPalms"
output.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(Path("Tools/RiverBasinWorld/DesertPalm.glb").resolve()))

for image in bpy.data.images:
    if image.packed_file and image.name.startswith("PalmTree_"):
        (output / image.name).write_bytes(image.packed_file.data)

for name in ("PalmTree_1", "PalmTree_3", "PalmTree_5"):
    source = bpy.data.objects[name]
    bpy.context.view_layer.update()
    orientation = source.matrix_world.to_3x3().to_4x4()
    source.data.transform(orientation)
    bottom = min(vertex.co.z for vertex in source.data.vertices)
    source.data.transform(Matrix.Translation(Vector((0, 0, -bottom))))
    source.parent = None
    source.matrix_world = Matrix.Identity(4)
    bpy.ops.object.select_all(action="DESELECT")
    source.select_set(True)
    bpy.context.view_layer.objects.active = source
    bpy.context.view_layer.update()
    source.data.calc_loop_triangles()
    print(name, tuple(round(x, 3) for x in source.dimensions), len(source.data.loop_triangles))
    bpy.ops.export_scene.fbx(filepath=str((output / (name + ".fbx")).resolve()),
                             use_selection=True, axis_forward="-Z", axis_up="Y",
                             apply_unit_scale=True, bake_anim=False, add_leaf_bones=False)
