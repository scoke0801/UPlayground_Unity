"""Blender MCP에서 시장 아케이드, 하역 기중기와 조선 선대를 저작한다."""

import math
import random

import bpy
from mathutils import Quaternion, Vector


def export_structures(workspace, preserve_exports=False):
    authoring = workspace / ("Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/"
                             "Authoring/HarborCity")
    source = workspace / "Tools/RiverBasinWorld/HarborCity"
    collection_name = "RiverBasin Harbor Structures"
    previous = bpy.data.collections.get(collection_name)
    if previous is not None:
        for obj in list(previous.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(previous)
    collection = bpy.data.collections.new(collection_name)
    bpy.context.scene.collection.children.link(collection)
    rng = random.Random(31)
    materials = {}
    for name, rgb in {
        "HB_Limestone": (.50,.40,.28), "HB_LimestoneTrim": (.67,.55,.39),
        "HB_Oak": (.26,.13,.060), "HB_OakEnd": (.39,.21,.101),
        "HB_Brass": (.39,.23,.087), "HB_Iron": (.095,.108,.111),
        "HB_GoldCanvas": (.78,.375,.085), "HB_IvoryCanvas": (.81,.70,.49),
        "HB_BlueCanvas": (.072,.245,.33), "HB_Rope": (.32,.218,.111),
        "HB_Plaster": (.73,.59,.39), "HB_Roof": (.092,.16,.19),
        "HB_Window": (.08,.15,.18)
    }.items():
        mat = bpy.data.materials.new(name)
        mat.diffuse_color = (*rgb,1)
        materials[name] = mat

    current = []

    def mesh(name, verts, faces, mat, bevel=0):
        data = bpy.data.meshes.new(name)
        data.from_pydata(verts,[],faces)
        data.update()
        uv = data.uv_layers.new(name="UVMap")
        for poly in data.polygons:
            axis = max(range(3), key=lambda i: abs(poly.normal[i]))
            for i in poly.loop_indices:
                co = data.vertices[data.loops[i].vertex_index].co
                uv.data[i].uv = ((co.y,co.z) if axis==0 else
                                 (co.x,co.z) if axis==1 else (co.x,co.y))
                uv.data[i].uv /= 1.8
        obj = bpy.data.objects.new(name,data)
        collection.objects.link(obj)
        data.materials.append(materials["HB_"+mat])
        if bevel:
            mod = obj.modifiers.new("마모 모서리","BEVEL")
            mod.width = bevel
            mod.segments = 2
        current.append(obj)
        return obj

    face_cube = [(3,2,1,0),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]

    def box(name, center, dims, mat, edge=.027, orient=None):
        center = Vector(center)
        orient = orient or Quaternion()
        verts = [center + orient @ Vector((x*dims[0]/2,y*dims[1]/2,z*dims[2]/2))
                 for z in (-1,1) for x,y in [(-1,-1),(1,-1),(1,1),(-1,1)]]
        return mesh(name,verts,face_cube,mat,edge)

    def bar(name,a,b,width,depth,mat):
        a,b=Vector(a),Vector(b)
        d=b-a
        return box(name,(a+b)/2,(width,depth,d.length),mat,min(.045,width*.15),
                   Vector((0,0,1)).rotation_difference(d))

    def drum(name, a, b, radius, mat, segments=14):
        a,b=Vector(a),Vector(b)
        q=Vector((0,0,1)).rotation_difference(b-a)
        verts=[p+q@Vector((math.cos(i*math.tau/segments)*radius,
                           math.sin(i*math.tau/segments)*radius,0))
               for p in (a,b) for i in range(segments)]
        faces=[(i,(i+1)%segments,(i+1)%segments+segments,i+segments)
               for i in range(segments)]
        faces.extend([tuple(reversed(range(segments))),tuple(range(segments,segments*2))])
        return mesh(name,verts,faces,mat)

    def finish(name):
        bpy.ops.object.select_all(action="DESELECT")
        for obj in current:
            obj.select_set(True)
        bpy.context.view_layer.objects.active=current[0]
        bpy.ops.object.convert(target="MESH")
        bpy.ops.object.join()
        obj=bpy.context.view_layer.objects.active
        obj.name=name
        path = authoring/(name+".fbx")
        if not preserve_exports or not path.exists():
            bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,
                                     axis_forward="-Z",axis_up="Y",apply_unit_scale=True,
                                     bake_anim=False,use_mesh_modifiers=True)
        current.clear()
        return (name,len(obj.data.vertices),len(obj.data.polygons))

    # 두 베이의 조적 아치와 바닷바람에 처진 천막.
    for x in (-4.8,0,4.8):
        box("기단석",(x,0,.16),(.86,.97,.32),"LimestoneTrim")
        for i in range(8):
            mat="LimestoneTrim" if i in (0,7) else "Limestone"
            box("엇물린 기둥석",(x,0,.47+i*.281),
                (.63+rng.uniform(-.025,.025),.69,.268),mat,.025)
        box("기둥 머리",(x,0,2.65),(.81,.89,.17),"LimestoneTrim")
    for center in (-2.4,2.4):
        for i in range(15):
            a=math.pi*i/15+.006
            b=math.pi*(i+1)/15-.006
            pts=[(center+math.cos(t)*r,y,2.68+math.sin(t)*r)
                 for y in (-.36,.36)
                 for t,r in [(a,2.07),(b,2.07),(b,2.46),(a,2.46)]]
            mesh("방사형 홍예석",pts,face_cube,"LimestoneTrim" if i==7 else "Limestone",.022)
    box("겹돌 코니스",(0,.02,5.0),(10.4,.99,.23),"LimestoneTrim")
    for x in (-4.8,0,4.8):
        bar("차양 받침",(x,-.15,4.79),(x,-2.77,3.71),.15,.16,"Oak")
        bar("차양 버팀",(x,-.19,3.24),(x,-2.57,3.78),.10,.12,"Oak")
    bar("차양 가로대",(-5.1,-2.86,3.69),(5.1,-2.86,3.69),.12,.12,"OakEnd")
    for i in range(16):
        x0=-5.04+.63*i
        v=[(x0+.63*q/4,-.2-2.75*t/8,
            4.88-1.17*t/8-.31*math.sin(math.pi*t/8)-.076*math.sin(math.pi*q/4))
           for t in range(9) for q in range(5)]
        f=[(j*5+k,j*5+k+1,j*5+k+6,j*5+k+5) for j in range(8) for k in range(4)]
        strip=mesh("솔기 있는 차양",v,f,"GoldCanvas" if i%4<2 else "IvoryCanvas")
        strip.modifiers.new("천막 두께","SOLIDIFY").thickness=.028
    arcade=finish("RiverBasinHarborArcade")

    # 기중기 받침, 겹보, 활차, 작동 밧줄과 하역 상자.
    box("부두 받침",(0,0,.22),(2.10,2.17,.44),"LimestoneTrim")
    for x in (-.8,.8):
        for y in (-.8,.8):
            box("앵커 플레이트",(x,y,.47),(.28,.28,.075),"Iron")
            bar("겹보 하부 버팀",(x,y,.51),(x*.29,y*.29,2.76),.26,.29,"Oak")
    drum("선회축",(0,0,.42),(0,0,5.38),.37,"Oak",16)
    for z in (.61,2.48,4.47,5.14):
        drum("철띠",(0,0,z),(0,0,z+.13),.385,"Iron",16)
    for x in (-.24,.24):
        bar("기중기 겹보",(x,1.9,4.36),(x,-4.15,6.65),.24,.28,"Oak")
        bar("암 버팀",(x,0,2.64),(x,-2.99,6.04),.19,.19,"OakEnd")
    for y in (-4,-1,.8,1.68):
        z=4.36+(1.9-y)*2.29/6.05
        drum("보 체결 볼트",(-.43,y,z),(.43,y,z),.091,"Iron",12)
    for x in (-.2,.2):
        drum("황동 활차",(x,-4.18,6.54),(x+.08,-4.18,6.54),.27,"Brass",16)
    drum("윈치 축",(-.81,.32,1.54),(.83,.32,1.54),.18,"Iron",12)
    drum("감긴 밧줄",(-.26,.32,1.54),(.26,.32,1.54),.28,"Rope",18)
    for x in (-.63,.52):
        drum("윈치 바퀴",(x,.32,1.54),(x+.12,.32,1.54),.44,"OakEnd",18)
    for a,b in [((0,.32,1.8),(0,-.04,4.96)),((0,-.04,4.96),(0,-4.18,6.77)),
                ((0,-4.18,6.77),(0,-4.18,1.93))]:
        drum("하역 밧줄",a,b,.037,"Rope",8)
    for i in range(5):
        box("상자 판재",(0,-4.62+.22*i,1.16),(1.19,.207,.77),"OakEnd",.018)
    for x in (-.51,.51):
        for y in (-4.73,-3.64):
            bar("상자 모서리",(x,y,.75),(x,y,1.61),.13,.15,"Oak")
            drum("화물 슬링",(x,y,1.56),(0,-4.18,1.93),.024,"Rope",8)
    crane=finish("RiverBasinHarborCrane")

    # 선대 레일과 아직 갑판을 얹지 않은 목조 선체 골조.
    for y in range(-10,11,2):
        box("선대 침목",(0,y,.21),(8.5,.47,.42),"Oak")
    for x in (-2.52,2.52):
        bar("선대 레일",(x,-11,.46),(x,11,.46),.29,.29,"OakEnd")
    bar("용골",(0,-10,1.31),(0,10,1.31),.47,.61,"Oak")
    for y in range(-9,10,2):
        span=max(.37,math.cos(y*math.pi/20))*3.45
        pts=[(math.sin(t*math.pi/16)*span,y,1.31+3.1*(1-math.cos(t*math.pi/16)))
             for t in range(-8,9)]
        for i in range(len(pts)-1):
            bar("선체 갈비뼈",pts[i],pts[i+1],.20,.24,"OakEnd")
        for x in (-span*.72,span*.72):
            bar("선체 임시 지주",(x, y,.44),(x*.77,y,2.1),.23,.23,"Oak")
    for side in (-1,1):
        for y in range(-9,9,2):
            a=max(.37,math.cos(y*math.pi/20))*3.45
            b=max(.37,math.cos((y+2)*math.pi/20))*3.45
            bar("선체 측면 종통재",(side*a,y,4.38),(side*b,y+2,4.38),.16,.19,"Oak")
    slip=finish("RiverBasinHarborSlipway")

    # 부두에서 육지 방향을 잡는 석조 세관. 전면은 부두 쪽을 향한다.
    box("세관 층단 기단",(0,0,.31),(14.6,9.4,.62),"LimestoneTrim")
    box("세관 회벽 몸체",(0,0,4.08),(13.7,8.45,7.50),"Plaster")
    box("상부 코니스",(0,0,7.88),(14.34,9.0,.27),"LimestoneTrim")
    box("1층 줄눈띠",(0,0,3.94),(13.86,8.55,.23),"LimestoneTrim")
    for x in (-6.75,6.75):
        for y in (-4.23,4.23):
            for i in range(15):
                box("모서리 사각 돌",(x,y,.8+i*.445),
                    (.64 if i%2 else .92,.49 if i%2 else .73,.419),"Limestone",.023)
    for y in (-4.29,4.29):
        south=math.copysign(1,y)
        for x in (-4.82,-2.42,0,2.42,4.82):
            for z in (2.57,5.80):
                if x==0 and z==2.57:
                    continue
                box("깊은 창 인방",(x,y,z),(1.27,.17,1.56),"LimestoneTrim")
                box("어두운 창",(x,y+south*.10,z),(1.01,.085,1.31),"Window")
                for offset in (-.42,0,.42):
                    box("창 세로 문살",(x+offset,y+south*.17,z),(.075,.085,1.25),"Oak")
                box("창 가로 문살",(x,y+south*.17,z),(.94,.079,.075),"Oak")
                box("빗물받이 창턱",(x,y+south*.17,z-.78),(1.43,.42,.14),"LimestoneTrim")
        box("하역문 회벽 테두리",(0,y,1.93),(2.46,.22,3.24),"LimestoneTrim")
        for i in range(9):
            box("하역문 판재",(-1.0+i*.25,y+south*.16,1.90),
                (.24,.10,2.97),"OakEnd" if i%3==0 else "Oak",.018)
        for z in (1.11,2.62):
            box("하역문 철띠",(0,y+south*.24,z),(2.20,.07,.115),"Iron")
    for side in (-1,1):
        # 엇갈린 청회색 기와가 큰 지붕 면을 나눠 보이게 한다.
        length=math.hypot(4.78,2.93)
        tilt=Quaternion((1,0,0),side*-math.atan2(2.93,4.78))
        box("경사 지붕판",(0,side*2.39,9.42),(14.8,length,.21),"Roof",.035,tilt)
        for row in range(11):
            f=(row+.5)/11
            y=side*(4.8*f)
            z=10.93-2.91*f
            for col in range(22):
                x=-7.31+(col+.50+.27*(row%2))*14.62/22
                box("기와 겹침",(x,y,z),(.67,.57,.092),"Roof",.019,tilt)
        bar("목재 처마",(-7.43,side*4.75,8.03),(7.43,side*4.75,8.03),.20,.21,"Oak")
    bar("용마루",(-7.5,0,11),(7.5,0,11),.26,.29,"Roof")
    box("세관 신호탑",(0,1.42,11.35),(2.86,2.74,3.61),"Plaster")
    box("신호탑 박공받침",(0,1.42,13.20),(3.29,3.18,.24),"LimestoneTrim")
    drum("청석 첨탑",(0,1.42,13.28),(0,1.42,13.73),1.92,"Roof",8)
    for x in (-.91,.91):
        for y in (.54,2.30):
            bar("전망탑 목재 기둥",(x,y,13.65),(x,y,14.82),.18,.20,"Oak")
    box("전망탑 처마",(0,1.42,14.86),(2.48,2.48,.23),"Roof")
    mesh("전망탑 지붕",[(-1.26,.16,14.98),(1.26,.16,14.98),(1.26,2.68,14.98),
                        (-1.26,2.68,14.98),(0,1.42,16.38)],
         [(3,2,1,0),(0,1,4),(1,2,4),(2,3,4),(3,0,4)],"Roof",.035)
    drum("황동 깃대",(0,1.42,16.3),(0,1.42,18.05),.067,"Brass",10)
    flag=mesh("세관 깃발",[(0,1.42,17.97),(.61,1.55,17.86),(1.28,1.47,17.94),
                           (2.16,1.65,17.80),(2.16,1.69,17.05),(1.31,1.51,17.20),
                           (.61,1.56,17.11),(0,1.42,17.20)],
              [(0,1,6,7),(1,2,5,6),(2,3,4,5)],"BlueCanvas")
    flag.modifiers.new("깃발 두께","SOLIDIFY").thickness=.021
    customs=finish("RiverBasinHarborCustoms")
    bpy.ops.wm.save_as_mainfile(filepath=str(source/"RiverBasinHarborStructures.blend"),copy=True)
    return {"models":[arcade,crane,slip,customs]}
