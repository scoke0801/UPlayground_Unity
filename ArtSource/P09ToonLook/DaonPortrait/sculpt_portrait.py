"""이전 머리의 사본에서 얼굴 비례·눈매·눈썹·턱선을 함께 조정한다."""
import bpy, json, math
from pathlib import Path
from mathutils import Vector

project=Path('C:/UsingProject/UnityProject/UPlayground')
source=bpy.data.objects['Daon_Head_HighQuality']
head=bpy.data.objects.get('Daon_Portrait')
if head is None:
    head=source.copy();head.name='Daon_Portrait';bpy.context.scene.collection.objects.link(head)
head.data=source.data.copy()
components=json.loads((project/'output/P09DaonSculpt/face_components.json').read_text())
parts={v:i for i,ids in enumerate(components) for v in ids}
neck={6,14,8,13,7,12,5,428,423,429,424,430,422,427,4,11}
original=bpy.data.objects['Female_Face_02']
# 몸통은 원래 16각 경계다. 세분화가 목 경계를 둥글게 줄이지 않게 고정한다.
crease=head.data.attributes.get('crease_vert') or head.data.attributes.new('crease_vert','FLOAT','POINT')
for i in neck:crease.data[i].value=1
def smooth(a,b,t):
    v=max(0,min(1,(t-a)/(b-a)));return v*v*(3-2*v)
def gaussian(x,c,w):return math.exp(-((x-c)/w)**2)
knots=[(1.325,0),(1.338,.018),(1.362,.014),(1.384,.012),(1.408,.010),(1.430,.008),(1.455,.003),(1.485,0),(1.54,0)]
def vertical(z):
    for (a,x),(b,y) in zip(knots,knots[1:]):
        if a<=z<=b:return x+(y-x)*smooth(a,b,z)
    return 0
def transform(co,part):
    x,y,z=co;ax=abs(x);sign=1 if x>=0 else -1
    front=1-smooth(-.025,.015,y)
    front*=1-smooth(-.035,-.008,y)*(1-smooth(1.355,1.380,z))
    # 목의 접합 정점은 움직이지 않고 전면 이목구비의 세로 배치를 바꾼다.
    dz=vertical(z)*front
    if part in {19,20}:dz=0
    # 눈꼬리와 피부 구멍·흰자·홍채에 같은 변형장을 적용한다.
    eye=gaussian(ax,.039,.028)*gaussian(z,1.409,.018)*front
    x+=sign*(ax-.038)*.045*eye
    dz+=(z-1.409)*.10*eye
    dz+=.0012*smooth(.035,.060,ax)*gaussian(z,1.412,.017)*front
    if part==0:
        # 중간 볼은 정돈하고 아래 턱은 폭을 남겨 지나친 역삼각형을 완화한다.
        x*=1-.045*gaussian(z,1.391,.024)*front
        x+=sign*.0022*gaussian(ax,.024,.023)*gaussian(z,1.347,.012)*front
        y+=.0013*gaussian(x,0,.012)*gaussian(z,1.385,.015)*front
        y-=.0010*gaussian(ax,.040,.022)*gaussian(z,1.387,.022)*front
    # 눈썹의 안쪽을 올리고 두께를 줄여 강한 사선 인상을 낮춘다.
    if part in {17,18}:
        center=1.4285
        dz+=(center-z)*.22+.0024*(1-smooth(.019,.053,ax))-.0015
    # 입의 폭을 조금 넓혀 작고 오므린 인상을 완화한다. 입안도 함께 이동한다.
    if part in {0,22,23,24}:
        mouth=gaussian(z,1.362,.010)*gaussian(x,0,.023)*front
        x*=1+.15*mouth
    return Vector((x,y,z+dz))
def vertex(i,co):
    if i in neck:return co+original.data.vertices[i].co-source.data.vertices[i].co
    result=transform(co,parts[i])
    x,y,z=source.data.vertices[i].co
    # 원래 무표정에 남아 있던 입술 안쪽의 0.24mm 틈을 닫는다.
    if parts[i]==0 and abs(x)<.009 and -.070<y<-.066 and 1.3615<z<1.3623:
        result.z+=.0004*(1-smooth(.005,.009,abs(x)))
    return result
for dst,src in zip(head.data.shape_keys.key_blocks,source.data.shape_keys.key_blocks):
    for i,(a,b) in enumerate(zip(dst.data,src.data)):a.co=vertex(i,b.co)
for i,(a,b) in enumerate(zip(head.data.vertices,source.data.vertices)):a.co=vertex(i,b.co)
head.hide_set(False);head.hide_render=False;source.hide_render=True;source.hide_set(True)
bpy.context.view_layer.update()
offset=max((a.co-b.co).length for a,b in zip(head.data.vertices,source.data.vertices))
assert len(head.data.shape_keys.key_blocks)==37
result={'vertices':len(head.data.vertices),'shape_keys':37,'max_offset_m':offset}
(project/'output/P09DaonPortrait/sculpt_result.json').write_text(json.dumps(result,indent=2))
