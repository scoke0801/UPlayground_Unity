"""부두 근접 시점용 상선, 갑판·선미 장비와 돛을 Blender MCP에서 저작한다."""

import math
import random
import runpy

import bpy
from mathutils import Quaternion, Vector


COLORS = {
    "Plank": (.46,.245,.115), "PlankWarm": (.51,.28,.133),
    "WetOak": (.24,.136,.076), "Rail": (.34,.171,.071),
    "Deck": (.61,.43,.26), "DeckWarm": (.66,.47,.28),
    "Caulk": (.10,.067,.046), "Paint": (.10,.26,.30),
    "Copper": (.59,.37,.13), "Iron": (.14,.16,.145),
    "Window": (.081,.19,.23), "Rope": (.36,.26,.14),
    "Canvas": (.86,.79,.61), "CanvasShade": (.81,.74,.56),
    "Stitch": (.70,.61,.43),
}


def export(workspace):
    tools = workspace / "Tools/RiverBasinWorld/HarborCity"
    Kit = runpy.run_path(str(tools / "harbor_architecture.py"))["Kit"]
    target = workspace / ("Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/"
                          "Authoring/HarborCity/RiverBasinMerchantShipDetail.fbx")
    label = "River Basin Merchant Detail"
    prev = bpy.data.collections.get(label)
    if prev is not None:
        assert all(obj.name.startswith("RBM_Merchant") for obj in prev.objects)
        for obj in list(prev.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(prev)
    collection = bpy.data.collections.new(label)
    bpy.context.scene.collection.children.link(collection)
    materials = {}
    for key,c in COLORS.items():
        m = bpy.data.materials.get("RBM_" + key) or bpy.data.materials.new("RBM_" + key)
        m.diffuse_color = (*c,1)
        m.use_nodes = True
        m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*c,1)
        materials[key] = m
    materials.update(Oak=materials["Rail"], Earth=materials["WetOak"], Metal=materials["Iron"])

    def sheer(y):
        u = (y+15.7)/31.7
        return 3.48 + 1.56*abs(2*u-1)**3 + .36*(1-u)**5

    def breadth(y):
        u = min(1,max(0,(y+15.7)/31.7))
        # 넓은 선미부터 좁은 이물까지 이어지는 비대칭 만곡.
        return max(.045,(2.04*(1-u)**4+4.32*math.sin(math.pi*u)**.86)*(1.11-.16*u))

    def hull(y,t,side,inset=0):
        keel = -2.5+.85*(abs((y+.5)/16))**5
        z = keel + (sheer(y)-keel)*t
        x = breadth(y)*max(.045,math.sin(math.pi*.55*t))**.72 - inset
        return (side*max(.03,x),y,z)

    def build(detail):
        k=Kit(bpy.context.scene,materials,20)
        rng=random.Random(407)
        # 수밀 선체 면 위에 갑판으로 갈수록 좁아지는 카벨 판재를 맞춘다.
        for side in (-1,1):
            cuts=80 if detail else 32
            rows=24 if detail else 12
            ys=[-15.7+31.7*i/cuts for i in range(cuts+1)]
            skin=[hull(y,j/rows,side,.033 if detail else .024) for j in range(rows+1) for y in ys]
            faces=[]
            for j in range(rows):
                for i in range(cuts):
                    a=j*(cuts+1)+i
                    f=(a,a+1,a+cuts+2,a+cuts+1)
                    faces.append(f if side>0 else f[::-1])
            k.add("Caulk" if detail else "Plank",skin,faces)
            for j in range(rows):
                ta=j/rows
                tb=(j+1)/rows
                j0=0
                while j0<cuts:
                    block=min(cuts,j0+(rng.randint(9,16) if detail else cuts))
                    span=ys[block]-ys[j0]
                    length=block-j0
                    delta=.014 if detail else 0
                    points=[hull(ys[j0]+delta+(span-2*delta)*i/length,t,side)
                            for t in (ta+.0017,tb-.0017) for i in range(length+1)]
                    f=[(i,i+1,i+length+2,i+length+1) for i in range(length)]
                    if side<0:f=[p[::-1] for p in f]
                    mat=("Paint" if ta>.78 else "WetOak" if tb<.42 else
                         "PlankWarm" if (j+j0)%7 in (0,3) else "Plank")
                    k.add(mat,points,f)
                    j0=block
            for t,mat,r in [(1,"Rail",.106),(.84,"Copper",.045),(.74,"Rail",.073),(.57,"Rail",.065)]:
                segments=68 if detail else 29
                for i in range(segments):
                    a=Vector(hull(-15.69+31.67*i/segments,t,side,-.048))
                    b=Vector(hull(-15.69+31.67*(i+1)/segments,t,side,-.048))
                    k.rod(mat,a,b,r,10 if detail else 6)
            for j in range(24 if detail else 13):
                y=-14.4+j*28.9/(23 if detail else 12)
                a=Vector(hull(y,1,side))
                k.rod("Rail",a,a+Vector((0,0,.84)),.077,10)
                k.box("Copper",a+Vector((0,0,.055)),(.205,.20,.09),.018)
            for z in (.43,.84):
                for i in range(60 if detail else 25):
                    c=60 if detail else 25
                    a=Vector(hull(-15.6+31.22*i/c,1,side))
                    b=Vector(hull(-15.6+31.22*(i+1)/c,1,side))
                    k.rod("Rail",a+Vector((0,0,z)),b+Vector((0,0,z)),.050 if z<.5 else .075,8)
            # 접안 쪽에 볼라드, 난간 이음과 계류 고리를 만든다.
            for y in (-13.2,-9.2,6.8,12.5):
                x=side*breadth(y)*.76
                z=sheer(y)-.20
                k.box("Rail",(x,y,z+.07),(.55,.68,.18),.045)
                for offset in (-.185,.185):
                    k.rod("Iron",(x,y+offset,z+.12),(x,y+offset,z+.52),.091,12)
                    k.rod("Copper",(x,y+offset,z+.48),(x,y+offset,z+.55),.117,12)
                if detail:
                    for idx in range(22):
                        a=idx*math.tau/22; b=(idx+1)*math.tau/22
                        p=Vector(hull(y,.923,side,-.12))
                        k.rod("Copper",p+Vector((0,math.cos(a)*.105,math.sin(a)*.105)),
                              p+Vector((0,math.cos(b)*.105,math.sin(b)*.105)),.021,7)

        # 선미를 닫는 수평 판재와 굽은 하부, 이물의 줄기재를 덧댄다.
        def transom(u,t,inset=0):
            x,_,z=hull(-15.7,t,1)
            swell=(1-u*u)*(.19+.38*t)
            return (x*u,-15.708-swell+inset,z)

        count=24 if detail else 12
        n=16 if detail else 8
        backing=[transom(-1+2*i/n,j/count,.024) for j in range(count+1) for i in range(n+1)]
        k.add("Caulk",backing,[(a,a+1,a+n+2,a+n+1) for j in range(count) for i in range(n)
                               for a in (j*(n+1)+i,)])
        for i in range(count):
            verts=[transom(-1+2*j/n,t) for t in ((i+.038)/count,(i+.964)/count) for j in range(n+1)]
            k.add("Paint" if i>=count*.78 else "WetOak" if i<count*.42 else "Plank",
                  verts,[(j,j+1,j+n+2,j+n+1) for j in range(n)])
        for t,mat,r in ((.96,"Rail",.086),(.84,"Copper",.043),(.74,"Rail",.070)):
            for i in range(n):
                k.rod(mat,transom(-1+2*i/n,t,-.024),transom(-1+2*(i+1)/n,t,-.024),r,9)
        rudder=[(-.11,-15.89,-1.74),(-.11,-16.52,-1.52),(-.11,-16.45,1.76),(-.11,-15.93,1.64),
                (.11,-15.89,-1.74),(.11,-16.52,-1.52),(.11,-16.45,1.76),(.11,-15.93,1.64)]
        k.add("WetOak",rudder,[(0,1,2,3),(4,7,6,5),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)])
        for z in (-.43,.76,1.48):
            k.box("Iron",(0,-16.21,z),(.287,.52,.084),.014)
        k.beam("Rail",(0,15.96,-1.7),(0,16.02,5.16),.19,.22)
        k.beam("Rail",(0,-15.88,-1.7),(0,-16.28,4.96),.23,.20)

        def deck_z(x,y):
            return sheer(y)-.25+.14*(1-(x/max(.26,breadth(y)))**2)

        cols=22 if detail else 11
        for column in range(cols):
            xa=(column/cols-.5)*1.96
            xb=((column+1)/cols-.5)*1.96
            pt=-15.7
            while pt<15.99:
                end=min(16,pt+(rng.uniform(2.9,5.8) if detail else 8.0))
                n=max(2,math.ceil((end-pt)/.57))
                verts=[]
                for i in range(n+1):
                    y=pt+(end-pt)*i/n
                    for x in ((xa+.0023)*breadth(y),(xb-.0023)*breadth(y)):
                        verts.append((x,y,deck_z(x,y)))
                k.add("DeckWarm" if column%3==1 else "Deck",verts,
                      [(i*2,i*2+1,i*2+3,i*2+2) for i in range(n)])
                pt=end

        cabin_y=-11.25
        w,d=3.97,3.95
        bz=4.10
        top=6.18
        k.box("Paint",(0,cabin_y,(top+bz)/2),(w,d,top-bz),.097)
        for side in (-1,1):
            x=side*w*.5
            for y in (cabin_y-d*.47,cabin_y+d*.47):
                k.box("Rail",(x,y,5.12),(.216,.182,2.15),.037)
            k.box("Rail",(x,cabin_y,4.19),(.21,d+.10,.19),.025)
            for y in (cabin_y-.97,cabin_y+.97):
                k.box("Copper",(x+side*.013,y,5.35),(.13,.92,1.03),.044)
                k.box("Window",(x+side*.094,y,5.35),(.088,.70,.79),.031)
                for u in (-.205,0,.205):
                    k.box("Rail",(x+side*.151,y+u,5.35),(.064,.039,.77),.010)
                k.box("Copper",(x+side*.17,y,4.86),(.26,1.08,.105),.03)
        for ySign in (-1,1):
            face=cabin_y+ySign*d*.5
            for x in (-1.30,0,1.30):
                k.box("Copper",(x,face+ySign*.030,5.29),(.90,.156,1.02),.049)
                k.box("Window",(x,face+ySign*.126,5.30),(.71,.071,.81),.035)
                for u in (-.208,0,.208):
                    k.box("Rail",(x+u,face+ySign*.17,5.31),(.035,.071,.805),.011)
                k.box("Copper",(x,face+ySign*.185,4.78),(1.03,.21,.092),.03)
        # 좌우 끝이 내려가는 선실 지붕과 빗물받이.
        for sign in (-1,1):
            for i in range(11 if detail else 5):
                count=11 if detail else 5
                x=sign*(i+.45)*2.26/count
                z=top+.28-.31*(x/2.26)**2
                k.box("Rail" if i%3==0 else "Paint",(x,cabin_y,z),(2.43/count,d+.51,.119),.032,
                      Quaternion((0,1,0),sign*.13*i/count))
            k.beam("Copper",(sign*2.25,cabin_y-d*.57,top-.016),
                  (sign*2.25,cabin_y+d*.57,top-.016),.092)
        k.beam("Rail",(0,cabin_y-d*.57,top+.31),(0,cabin_y+d*.57,top+.31),.152)
        # 선미 장식, 키와 유리 선등.
        for x in (-1.52,1.52):
            y=cabin_y-d*.5-.24
            k.box("Iron",(x,y,5.71),(.32,.36,.12),.022)
            k.box("CanvasShade",(x,y,5.45),(.23,.25,.45),.029)
            for a in (-1,1):
                for b in (-1,1):
                    k.rod("Copper",(x+a*.135,y+b*.133,5.19),(x+a*.135,y+b*.133,5.76),.024,8)
            k.box("Iron",(x,y,5.17),(.32,.36,.109),.022)
        wheel_y=-8.51
        k.rod("Rail",(0,wheel_y,deck_z(0,wheel_y)),(0,wheel_y,5.15),.21,13)
        k.rod("Copper",(0,wheel_y-.14,5.02),(0,wheel_y+.14,5.02),.19,14)
        n=24 if detail else 14
        for i in range(n):
            a=i*math.tau/n;b=(i+1)*math.tau/n
            k.rod("Rail",(.56*math.cos(a),wheel_y,5.02+.56*math.sin(a)),
                  (.56*math.cos(b),wheel_y,5.02+.56*math.sin(b)),.049,8)
        for i in range(8):
            a=i*math.tau/8
            k.rod("Copper",(0,wheel_y,5.02),(.67*math.cos(a),wheel_y,5.02+.67*math.sin(a)),.037,8)

        for y,w2,d2 in [(-4.1,2.25,2.8),(3.9,2.16,2.75),(11.3,1.4,1.2)]:
            z=deck_z(0,y)
            k.box("Rail",(0,y,z+.20),(w2+.27,d2+.26,.38),.061)
            count=8 if detail else 5
            for i in range(count):
                x=w2*((i+.5)/count-.5)
                k.box("DeckWarm",(x,y,z+.43),(w2/count*.958,d2,.135),.027)
            for x in (-w2*.29,w2*.29):
                k.box("Iron",(x,y,z+.513),(.081,d2*.86,.043),.012)
        for x,y in [(-2.02,-5.93),(2.14,-6.10),(-2.38,4.73),(2.27,5.05),(-1.44,10.77)]:
            z=deck_z(x,y)
            k.barrel(x,y,z,.34)
            if detail:
                for i in range(44):
                    a=i*math.tau/17;b=(i+1)*math.tau/17
                    radius=.125+i*.0047
                    k.rod("Rope",(x+radius*math.cos(a),y+.53+radius*math.sin(a),z+.033),
                          (x+(radius+.005)*math.cos(b),y+.53+(radius+.005)*math.sin(b),z+.033),.015,6)
        k.rod("Rail",(0,12.2,4.48),(0,19,7.49),.214,17,.26)
        k.rod("Rope",(0,18.9,7.46),(0,15.68,1.9),.049,9)

        for m,(y,height,width) in enumerate([(-7,15.25,6.55),(1,21.15,10.35),(9.30,17.40,8.62)]):
            k.rod("Rail",(0,y,deck_z(0,y)),(0,y,height),.265,20 if detail else 12,.30)
            for z in (deck_z(0,y)+.36,height-3.65,height-1.71):
                k.rod("Iron",(0,y,z),(0,y,z+.12),.254 if z<6 else .137,15)
            for yard in range(2):
                top=height-(2.12 if yard==0 else 5.97)
                w=width*(.68 if yard==0 else 1)
                drop=3.22 if yard==0 else 3.94
                swing=(-.06 if m==1 else .11)
                def cloth(u,t):
                    x=(u-.5)*w*(1-.135*t)
                    wave=math.sin(math.pi*u)*math.sin(2.64*t)*w*.155
                    wave+=.055*math.sin(u*math.tau*6.5+t*2.7)*math.sin(math.pi*u)*t
                    z=top-drop*t+.29*t*abs(2*u-1)**2+.072*math.cos(u*math.tau*4)*t**8
                    return (x,y+.015+swing*x+wave,z)
                a=Vector(cloth(0,0));b=Vector(cloth(1,0))
                k.rod("Rail",a-Vector((.27,0,0)),(a+b)*.5,.073,14,2.1)
                k.rod("Rail",(a+b)*.5,b+Vector((.27,0,0)),.153,14,.47)
                cols=36 if detail else 16
                rows=20 if detail else 11
                v=[cloth(i/cols,j/rows) for j in range(rows+1) for i in range(cols+1)]
                for index,material in enumerate(("Canvas","CanvasShade")):
                    faces=[]
                    for j in range(rows):
                        for i in range(cols):
                            if ((i//max(1,cols//6))%5==0) != (index==1):
                                continue
                            q=j*(cols+1)+i
                            faces.append((q,q+cols+1,q+cols+2,q+1))
                    # 역면은 수 mm 안쪽의 별도 면으로 남겨 돛이 뒤쪽에서도 보인다.
                    k.add(material,v,faces)
                    k.add(material,[(x,yy-.012,z) for x,yy,z in v],[f[::-1] for f in faces])
                if detail:
                    for u in (0,.1667,.3333,.5,.6667,.8333,1):
                        for j in range(16):
                            p=Vector(cloth(u,j/16))+Vector((0,.017,0))
                            q=Vector(cloth(u,(j+1)/16))+Vector((0,.017,0))
                            k.rod("Stitch",p,q,.014 if u in (0,1) else .009,6)
                    for j in range(27):
                        k.rod("Stitch",cloth(j/27,1),cloth((j+1)/27,1),.021,7)
                    for u in (.09,.23,.38,.53,.67,.82,.95):
                        p=Vector(cloth(u,0))
                        k.rod("Rope",p+Vector((0,.09,.11)),p+Vector((0,.13,-.18)),.022,7)
                for side in (-1,1):
                    p=cloth(0 if side<0 else 1,.97)
                    k.rod("Rope",p,(side*breadth(y-1.5)*.79,y-1.5,deck_z(1,y-1.5)+.28),.035,8)
                    k.rod("Rope",cloth(0 if side<0 else 1,0),(0,y,height-.47),.029,8)
            for side in (-1,1):
                a=[];b=[]
                for d in (-1.38,-.47,.45,1.32):
                    position=Vector(hull(y+d,.91,side,-.03))
                    end=Vector((0,y+.19,height-3.17))
                    a.append(position);b.append(end)
                    k.rod("Rope",position,end,.034,8)
                if detail:
                    for i in range(14):
                        t=.083+i*.055
                        for j in range(3):
                            k.rod("Rope",a[j].lerp(b[j],t),a[j+1].lerp(b[j+1],t),.014,6)
            k.rod("Rope",(0,y,height-.45),(0,min(18.8,y+8),7.48 if m==2 else 6.72),.037,9)
            k.add("Paint",[(0,y,height-.12),(0,y+2.06,height-.22),
                           (.15,y+1.46,height-.43),(0,y,height-.62)],[(0,1,2,3)])

        obj,n=k.make("RBM_Merchant_LOD"+("0" if detail else "1"),collection)
        for polygon in obj.data.polygons:
            name=obj.data.materials[polygon.material_index].name
            if name in ("RBM_Plank","RBM_PlankWarm","RBM_WetOak","RBM_Paint","RBM_Canvas","RBM_CanvasShade"):
                polygon.use_smooth=True
        return obj,n

    levels=[build(True),build(False)]
    bpy.ops.object.select_all(action="DESELECT")
    for model,n in levels:model.select_set(True)
    bpy.context.view_layer.objects.active=levels[0][0]
    bpy.ops.export_scene.fbx(filepath=str(target),use_selection=True,axis_forward="-Z",axis_up="Y",
                             apply_unit_scale=True,apply_scale_options="FBX_SCALE_UNITS",
                             bake_space_transform=True,bake_anim=False,add_leaf_bones=False)
    levels[1][0].hide_set(True)
    bpy.ops.wm.save_as_mainfile(filepath=str(tools/"RiverBasinMerchantShipDetail.blend"),copy=True)
    return {model.name:n for model,n in levels}
