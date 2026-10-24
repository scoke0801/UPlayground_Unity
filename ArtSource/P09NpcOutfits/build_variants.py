"""별도 Blender 프로세스에서 NPC 의상 원본과 FBX를 제작한다."""
import bpy
import json
from pathlib import Path

project = Path(__file__).resolve().parents[2]
recipes = json.loads((project/'ArtSource/P09NpcOutfits/recipes.json').read_text(encoding='utf-8-sig'))
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.scene.name = 'P09_RangerVest_Workshop'
bpy.ops.import_scene.fbx(filepath=str(project/'Assets/ExternalAssets/Character/P09_Modular_Humanoid/Model_DATA/FBX/P09_250802.fbx'),use_anim=False,automatic_bone_orientation=False)
source = (project/'ArtSource/P09RangerVest/build_vest.py').read_text(encoding='utf-8-sig')
report = []
for recipe in recipes['variants']:
    namespace = {'BUILD_RECIPE':recipe}
    exec(compile(source, 'build_vest.py', 'exec'), namespace)
    report.append(namespace['result'])
needed = {bpy.data.objects['Skeleton_P09_Body']}
for recipe in recipes['variants']:
    needed.add(bpy.data.objects[recipe['mesh_name']])
    needed.add(bpy.data.objects[recipe['body']])
for obj in list(bpy.data.objects):
    if obj not in needed:
        bpy.data.objects.remove(obj, do_unlink=True)
for obj in needed:
    # 같은 좌표에서 제작하므로 원본을 열면 첫 의상만 보이게 하고 나머지는 Outliner에서 전환한다.
    hidden = obj.type == 'MESH' and obj.name != recipes['variants'][0]['mesh_name']
    obj.hide_set(hidden)
    obj.hide_render = hidden
bpy.ops.wm.save_as_mainfile(filepath=str(project/'ArtSource/P09NpcOutfits/P09_NpcOutfits.blend'),compress=True)
(project/'output/P09NpcOutfits/blender-validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
