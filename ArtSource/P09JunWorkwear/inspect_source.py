"""P09 원본의 재단 기준과 본 위치를 읽어 기록한다."""
import bpy
import json
from pathlib import Path

project = Path(__file__).resolve().parents[2]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(project/'Assets/ExternalAssets/Character/P09_Modular_Humanoid/Model_DATA/FBX/P09_250802.fbx'),use_anim=False,automatic_bone_orientation=False)
report = {}
for name in ['Male_Body_Leg','Male_Body_Nakid_Leg','Male_Face_02','Hair_05','Male_Armor_003_Leg']:
    obj = bpy.data.objects[name]
    points = [v.co for v in obj.data.vertices]
    report[name] = {'min':[min(v[i] for v in points) for i in range(3)],'max':[max(v[i] for v in points) for i in range(3)],'vertices':len(points),'matrix':[list(row) for row in obj.matrix_world]}
    if name=='Male_Body_Leg':
        report[name]['sections']={}
        for height in [0.03,0.08,0.15,0.25,0.4,0.5,0.65,0.8,0.95,1.0]:
            ring=[v for v in points if abs(v.z-height)<0.035 and v.x>0]
            if ring:report[name]['sections'][str(height)]={'min':[min(v[i] for v in ring) for i in range(3)],'max':[max(v[i] for v in ring) for i in range(3)]}
rig=bpy.data.objects['Skeleton_P09_Body']
report['bones']={b.name:{'head':list(b.head_local),'tail':list(b.tail_local)} for b in rig.data.bones if any(x in b.name.lower() for x in ['hip','leg','foot','head','thigh','calf','pelvis','toe'])}
(project/'output/P09JunWorkwear/source-inspection.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(project/'ArtSource/P09JunWorkwear/SourceReference.blend'),compress=True)
print(json.dumps(report))
