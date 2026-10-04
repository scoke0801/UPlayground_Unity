"""현재 블렌더 시험장 씬의 환경 메시를 Unity 좌표로 내보낸다."""
import bpy
from pathlib import Path
from mathutils import Matrix

ROOT = Path(__file__).resolve().parents[2]
destination = ROOT / 'Assets/05.Models/CameraTestMap/CameraTestMap.fbx'
objects = [obj for obj in bpy.context.scene.objects
           if obj.type == 'MESH' and obj.name.startswith(('COL_', 'VIS_'))]
if not objects:
    raise RuntimeError('카메라 시험장 씬을 먼저 선택하세요.')

# FBX → Unity의 handedness 변환은 X축을 반전한다. 메시 사본에 역변환을 굽고 원본은 복원한다.
originals = []
reflection = Matrix.Diagonal((-1, 1, 1, 1))
try:
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        original = obj.data
        transform = obj.matrix_world.copy()
        exported = original.copy()
        exported.transform(reflection @ transform)
        exported.flip_normals()
        originals.append((obj, original, transform, exported))
        obj.data = exported
        obj.matrix_world = Matrix.Identity(4)
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.fbx(filepath=str(destination), use_selection=True,
        object_types={'MESH'}, axis_forward='-Z', axis_up='Y', apply_unit_scale=True,
        bake_space_transform=True, use_mesh_modifiers=True, add_leaf_bones=False, bake_anim=False)
finally:
    for obj, original, transform, exported in originals:
        obj.data = original
        obj.matrix_world = transform
        bpy.data.meshes.remove(exported)
result = {'fbx':str(destination),'mesh_count':len(objects)}
