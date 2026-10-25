"""원본 얼굴을 새 비례로 조형하고 모든 표정에 같은 연속 변형을 적용한다."""
import bpy
import json
import struct
import numpy as np
from pathlib import Path

ROOT = Path(r'C:/UsingProject/UnityProject/UPlayground')
OUT = ROOT / 'ArtSource/ExternalCharacterBuilder/DaonFace'
OUT.mkdir(parents=True, exist_ok=True)
source_scene = bpy.data.scenes['ExternalCharacter_Workbench']
scene = bpy.data.scenes.new('Daon_NewIdentity')
rig = next(o for o in source_scene.objects if o.type == 'ARMATURE')
scene.collection.objects.link(rig)

def smooth(a, b, x):
    t = np.clip((x-a)/(b-a), 0, 1)
    return t*t*(3-2*t)

def sculpt(points, hair=False):
    p = points.copy()
    x, y, z = points.T
    front = 1-smooth(-0.055, 0.015, y)
    if hair:
        p[:, 2] -= 0.013*smooth(1.465, 1.59, z)
        fringe = np.exp(-((z-1.49)/.045)**4) * (1-smooth(-.07, -.015, y))
        p[:, 0] += .008*fringe
        return p
    p[:, 2] -= .013*smooth(1.47, 1.575, z)
    # 안구·눈꺼풀·눈물선을 같은 공간에서 변형하여 닫힘 관계를 유지한다.
    eye = np.exp(-((np.abs(x)-.034)/.031)**4-((z-1.434)/.024)**4)*front
    p[:, 2] += ((z-1.434)*(-.18)+.003)*eye
    p[:, 0] -= np.sign(x)*.0018*eye
    outer = smooth(.034,.063,np.abs(x))*eye
    p[:, 2] += .0025*outer
    cheek = np.exp(-((z-1.401)/.026)**2)*front
    p[:, 0] *= 1-.065*cheek
    p[:, 1] += .002*cheek*np.exp(-((np.abs(x)-.048)/.025)**2)
    jaw = np.exp(-((z-1.348)/.016)**2)*front
    p[:, 2] += .006*jaw
    p[:, 0] += .0025*np.tanh(x/.012)*jaw
    nose = np.exp(-(x/.012)**2-((z-1.420)/.018)**2)*front
    p[:, 1] -= .004*nose
    mouth = np.exp(-(x/.032)**4-((z-1.378)/.012)**4)*front
    p[:, 0] += x*.16*mouth
    p[:, 2] += .0025*mouth
    p[:, 1] -= .001*mouth
    return p

def coords(data):
    a=np.empty(len(data)*3,dtype=np.float32)
    data.foreach_get('co',a)
    return a.reshape(-1,3)

report=[]
with (OUT/'sculpt.bin').open('wb') as stream:
    names=['Body','Hair','Hair_back','Hair_back_long']
    stream.write(struct.pack('<i',len(names)))
    for name in names:
        source=source_scene.objects[name]
        obj=source.copy()
        obj.data=source.data.copy()
        obj.name='Daon_'+name
        scene.collection.objects.link(obj)
        raw=coords(source.data.vertices)
        shaped=sculpt(raw,name!='Body')
        obj.data.vertices.foreach_set('co',shaped.ravel())
        encoded=name.encode('utf-8')
        keys=list(source.data.shape_keys.key_blocks)[1:] if source.data.shape_keys else []
        stream.write(struct.pack('<i',len(encoded)));stream.write(encoded)
        stream.write(struct.pack('<ii',len(raw),len(keys)))
        stream.write(raw.astype('<f4').tobytes());stream.write(shaped.astype('<f4').tobytes())
        if source.data.shape_keys:
            obj.data.shape_keys.key_blocks[0].data.foreach_set('co',shaped.ravel())
        for index,key in enumerate(keys):
            target=sculpt(coords(key.data),name!='Body')
            obj.data.shape_keys.key_blocks[index+1].data.foreach_set('co',target.ravel())
            encoded=key.name.encode('utf-8')
            stream.write(struct.pack('<i',len(encoded)));stream.write(encoded)
            stream.write(target.astype('<f4').tobytes())
        obj.data.update()
        report.append({'mesh':name,'vertices':len(raw),'expressions':len(keys),
                       'moved_vertices':int(np.count_nonzero(np.linalg.norm(shaped-raw,axis=1)>1e-5)),
                       'max_displacement_m':float(np.max(np.linalg.norm(shaped-raw,axis=1)))})
(ROOT/'output/DaonFaceRedesign/sculpt_report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
# 대형 원본 리그의 연결 데이터 저장은 MCP 응답 시간을 초과할 수 있어 바이너리를 교환 원본으로 둔다.
result = {'scene': scene.name, 'binary': str(OUT/'sculpt.bin'), 'meshes': report}
