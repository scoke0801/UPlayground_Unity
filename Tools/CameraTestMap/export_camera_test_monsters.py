"""수정한 몬스터 원본의 계층·대기 애니메이션을 Unity FBX에 반영한다."""
import bpy
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
destination = ROOT / 'Assets/05.Models/CameraTestMap/Monsters'
scene = bpy.data.scenes.get('CameraTestMonsters')
if scene is None:
    raise RuntimeError('CameraTestMonsters.blend 원본을 먼저 여세요.')
bpy.context.window.scene = scene
if bpy.context.mode != 'OBJECT':
    bpy.ops.object.mode_set(mode='OBJECT')
original_frame = scene.frame_current
scene.frame_set(scene.frame_start)
try:
    for name in ('SkySentinel', 'StoneColossus'):
        root = scene.objects.get(name)
        if root is None:
            raise RuntimeError('모델 루트 누락: ' + name)
        bpy.ops.object.select_all(action='DESELECT')
        root.select_set(True)
        for obj in root.children_recursive:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = root
        original_location = root.location.copy()
        try:
            root.location = (0, 0, 0)
            bpy.ops.export_scene.fbx(filepath=str(destination/(name+'.fbx')),use_selection=True,
                object_types={'EMPTY','MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,
                bake_space_transform=False,add_leaf_bones=False,bake_anim=True,
                bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0)
        finally:
            root.location = original_location
finally:
    scene.frame_set(original_frame)
result = {'export': str(destination)}
