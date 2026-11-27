"""항구 시가지의 건물과 작업장 키트를 Blender MCP에서 제작한다."""

import math
import random

import bpy
from mathutils import Quaternion, Vector


FINISHES = {
    "Masonry": (.48,.40,.31), "Mortar": (.34,.30,.25), "Carved": (.72,.61,.45),
    "Ochre": (.70,.50,.28), "Chalk": (.74,.68,.54), "Sage": (.42,.56,.51),
    "Oak": (.26,.136,.074), "Painted": (.104,.28,.30), "Glass": (.075,.14,.16),
    "Metal": (.24,.20,.14), "Slate": (.17,.25,.30), "SlateLight": (.22,.30,.33),
    "Clay": (.50,.235,.136), "ClayLight": (.59,.30,.185),
    "Canvas": (.68,.33,.082), "Linen": (.77,.68,.51), "Rope": (.30,.22,.13),
    "Earth": (.26,.165,.105), "Leaves": (.28,.40,.16), "Fruit": (.57,.23,.085),
}


class Kit:
    def __init__(self, scene, materials, seed):
        self.scene = scene
        self.materials = materials
        self.rng = random.Random(seed)
        self.parts = {}

    def add(self, mat, vertices, faces):
        vv, ff = self.parts.setdefault(mat, ([], []))
        base = len(vv)
        vv.extend(vertices)
        ff.extend(tuple(base + i for i in face) for face in faces)

    def box(self, mat, p, size, bevel=.025, orientation=None):
        x,y,z = (k*.5 for k in size)
        e = min(bevel,x*.29,y*.29,z*.29)
        rings = []
        for h,r in [(-z,e),(-z+e,0),(z-e,0),(z,e)] if e else [(-z,0),(z,0)]:
            xx=x-r; yy=y-r
            if e:
                pts=[(-xx+e,-yy),(xx-e,-yy),(xx,-yy+e),(xx,yy-e),
                     (xx-e,yy),(-xx+e,yy),(-xx,yy-e),(-xx,-yy+e)]
            else:
                pts=[(-xx,-yy),(xx,-yy),(xx,yy),(-xx,yy)]
            rings.append([(u,v,h) for u,v in pts])
        n=len(rings[0]); verts=[v for ring in rings for v in ring]
        if orientation:
            verts=[orientation @ Vector(v) for v in verts]
        verts=[tuple(Vector(p)+Vector(v)) for v in verts]
        faces=[tuple(reversed(range(n))),tuple(range(len(verts)-n,len(verts)))]
        faces.extend((k+i,k+(i+1)%n,k+n+(i+1)%n,k+n+i)
                     for k in range(0,len(verts)-n,n) for i in range(n))
        self.add(mat,verts,faces)

    def beam(self,mat,a,b,w,d=None, edge=.025):
        a,b=Vector(a),Vector(b)
        delta=b-a
        self.box(mat,(a+b)*.5,(w,d or w,delta.length),edge,
                 Vector((0,0,1)).rotation_difference(delta))

    def rod(self,mat,a,b,r,sides=8,taper=1):
        a,b=Vector(a),Vector(b)
        q=Vector((0,0,1)).rotation_difference(b-a)
        verts=[p+q@Vector((radius*math.cos(i*math.tau/sides),
                          radius*math.sin(i*math.tau/sides),0))
               for p,radius in ((a,r),(b,r*taper)) for i in range(sides)]
        faces=[tuple(reversed(range(sides))),tuple(range(sides,2*sides))]
        faces.extend((i,(i+1)%sides,(i+1)%sides+sides,i+sides) for i in range(sides))
        self.add(mat,verts,faces)

    def canopy(self,x,y,h,w,d):
        for sign in (-1,1):
            px=x+sign*w*.48
            self.beam("Oak",(px,y,h),(px,y-d,h-.72),.14)
            self.beam("Oak",(px,y,h-.88),(px,y-d*.88,h-.61),.09)
        self.beam("Oak",(x-w*.52,y-d,h-.71),(x+w*.52,y-d,h-.71),.092)
        for j in range(9):
            u=x-w*.5+j*w/9
            vv=[(u+i*w/27,y-d*k/8,
                 h-.71*k/8-.22*math.sin(math.pi*k/8)-.055*math.sin(math.pi*i/3))
                for k in range(9) for i in range(4)]
            ff=[(k*4+i,k*4+i+4,k*4+i+5,k*4+i+1) for k in range(8) for i in range(3)]
            mat="Canvas" if j%3!=2 else "Linen"
            self.add(mat,vv,ff)
            self.add(mat,[(a,b,c-.026) for a,b,c in vv],[f[::-1] for f in ff])
            if j%3==1:
                self.beam("Metal",(u,y-d,h-.72),(u,y-d,h-.81),.028)

    def fascia(self,x,y,z,width,height,arch,doors=False,shutters=True):
        w=width/2
        self.box("Glass" if not doors else "Oak",(x,y-.077,z),(width,.154,height),.013)
        self.box("Carved",(x,y-.14,z-height/2-.07),(width+.30,.34,.16))
        for i in (-1,1):
            self.box("Carved",(x+i*(w+.06),y-.13,z),(.14,.28,height+.11))
        if arch:
            center=z+height*.42
            for i in range(11):
                a=math.pi*i/11+.009
                b=math.pi*(i+1)/11-.009
                pts=[(x+math.cos(t)*r,k,center+math.sin(t)*r)
                     for k in (y-.27,y-.045) for t,r in
                     ((a,w),(b,w),(b,w+.19),(a,w+.19))]
                self.add("Masonry" if i in (0,10) else "Carved",pts,
                         [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)])
            pts=[(x,y-.10,center)]+[(x+w*math.cos(math.pi*i/16),y-.10,
                                   center+w*math.sin(math.pi*i/16)) for i in range(17)]
            self.add("Glass",pts,[(0,i+1,i) for i in range(1,17)])
        else:
            self.box("Oak",(x,y-.16,z+height*.53),(width+.22,.27,.16))
        if doors:
            for i in range(max(5,round(width/.20))):
                n=max(5,round(width/.20))
                self.box("Oak" if i%3==0 else "Painted",
                         (x-width*.5+width*(i+.5)/n,y-.164,z),(width/n*.956,.1,height*.94),.013)
            for hz in (-.27,.29):
                self.box("Metal",(x,y-.23,z+height*hz),(width*.90,.055,.10))
            self.rod("Metal",(x+.14,y-.245,z),(x+.14,y-.335,z),.052)
        else:
            self.box("Oak",(x,y-.185,z),(.072,.13,height))
            self.box("Oak",(x,y-.185,z),(width,.13,.064))
            if shutters:
                for side in (-1,1):
                    qx=x+side*(w+.22)
                    self.box("Painted",(qx,y-.214,z),(.28,.125,height*.93))
                    for dz in (-.32,-.16,0,.16,.32):
                        self.box("Oak",(qx,y-.293,z+height*dz),(.245,.035,.030),0)

    def roof(self,w,d,top,rise,palette,detail,offset=0):
        over=.38
        xw=w*.5+over; yb=d*.5+over
        tip=top+rise
        for edge in (-1,1):
            y=offset+yb*edge
            self.add("Ochre",[(-w*.5,y-edge*over,top),(w*.5,y-edge*over,top),
                              (0,y-edge*over,tip)],[(0,2,1) if edge>0 else (0,1,2)])
            for side in (-1,1):
                self.beam("Oak",(0,y,tip),(xw*side,y,top-.055),.205,.25)
        for side in (-1,1):
            self.add(palette,[(-.001,-yb+offset,tip),(side*xw,-yb+offset,top),
                              (side*xw,yb+offset,top),(0,yb+offset,tip)],
                     [(0,1,2,3) if side>0 else (3,2,1,0)])
            slope=math.atan2(rise,xw)*side
            self.beam("Oak",(xw*side,-yb+offset,top),(xw*side,yb+offset,top),.23)
            rows=max(8,round(xw/.36))
            columns=max(9,round((2*yb)/.33))
            tile_span=2*yb/columns
            pitch=math.hypot(xw,rise)/rows
            for row in range(rows):
                t=(row+.43)/rows
                xx=side*xw*t
                zz=tip-rise*t+.055
                material=palette+"Light" if (row % 4==0) else palette
                if detail:
                    for i in range(columns):
                        tile_y=-yb+offset+(i+.35+(row%2)*.22)*tile_span
                        self.box(material,(xx,tile_y,zz+self.rng.uniform(-.007,.011)),
                                 (pitch*1.17,tile_span*.965,.063),.014 if row%3==0 else 0,
                                 Quaternion((0,1,0),slope))
                else:
                    self.box(material,(xx,offset,zz),(pitch*1.18,yb*1.99,.06),0,
                             Quaternion((0,1,0),slope))
        for i in range(max(6,round(yb/.30))):
            step=2*yb/max(6,round(yb/.30))
            y=-yb+offset+step*i
            self.rod(palette,(0,y,tip+.08),(0,y+step*.95,tip+.08),.145,8)

    def masonry_band(self,w,d,high,detail):
        self.box("Mortar",(0,0,high*.5),(w,d,high),.025)
        if not detail:
            self.box("Carved",(0,0,high),(w+.10,d+.10,.13))
            return
        rows=max(2,round(high/.25))
        for side in (-1,1):
            for axis in (0,1):
                width=w if axis==0 else d
                distance=d*.5 if axis==0 else w*.5
                for row in range(rows):
                    stones=max(5,round(width/.61))
                    span=width/stones
                    for j in range(stones):
                        q=-width*.5+span*(j+.50)
                        if j not in (0,stones-1):
                            q+=self.rng.uniform(-.05,.05)
                        depth=self.rng.uniform(.13,.21)
                        z=high*(row+.5)/rows
                        size=(span*.969,depth,high/rows*.932)
                        p=(q,side*(distance-.035),z)
                        if axis:
                            size=size[1],size[0],size[2]
                            p=p[1],p[0],p[2]
                        self.box("Carved" if self.rng.random()<.17 else "Masonry",p,size,
                                 .023 if row%2==0 else .013)

    def dormer(self,x,y,z,side,mat):
        cheek=1.45
        depth=1.78
        top=z+1.84
        self.box("Chalk",(x,y,z+.52),(depth,cheek,1.06),.043)
        outside=x+side*.91
        self.box("Glass",(outside,y,z+.56),(.115,.83,.85))
        self.box("Oak",(outside+side*.06,y,z+.56),(.10,.08,.85))
        for k in (-1,1):
            self.box("Oak",(outside+side*.062,y+k*.48,z+.56),(.10,.112,1.01))
        self.beam("Carved",(outside+side*.075,y-.52,z+.11),(outside+side*.075,y+.52,z+.11),.138)
        for sign in (-1,1):
            face=[(x-depth*.55,y,top),(x+depth*.60,y,top),
                  (x+depth*.60,y+sign*.87,z+1.06),(x-depth*.55,y+sign*.87,z+1.06)]
            self.add(mat,face,[(0,3,2,1) if sign>0 else (0,1,2,3)])
            self.beam("Oak",(outside+side*.11,y,top),(outside+side*.11,y+sign*.89,z+1.06),.119)
            for i in range(1,5):
                t=i/4
                self.beam(mat+"Light",(x-depth*.55,y+sign*.87*t,top-.78*t),
                          (x+depth*.59,y+sign*.87*t,top-.78*t),.077,.10,0)
        self.add("Chalk",[(outside,y-.71,z+1.03),(outside,y+.71,z+1.03),
                          (outside,y,top)],[(0,1,2) if side>0 else (0,2,1)])

    def building(self,variant,detail):
        warehouse=variant in ("BondedStore","NetLoft")
        w,d,h,rise,wall,roof={
            "MerchantInn":(8.50,7.65,6.68,2.74,"Ochre","Clay"),
            "PilotHouse":(8.0,7.48,6.41,3.42,"Sage","Slate"),
            "Fishmonger":(8.35,7.86,5.35,2.25,"Chalk","Clay"),
            "RopewalkHouse":(7.75,7.20,6.96,2.84,"Chalk","Slate"),
            "BondedStore":(14.55,10.65,6.34,3.82,"Ochre","Slate"),
            "NetLoft":(13.95,9.92,6.12,3.38,"Sage","Clay"),
        }[variant]
        stone=.92 if warehouse else .64
        self.box(wall,(0,0,(h+stone)*.5),(w-.06,d-.06,h-stone),.075)
        self.masonry_band(w,d,stone,detail)
        for z in (stone,3.33,h-.12):
            self.box("Carved" if warehouse else "Oak",(0,0,z),(w+.13,d+.12,.17),.030)
        self.roof(w,d,h,rise,roof,detail)
        if variant=="MerchantInn":
            for side in (-1,1):
                self.dormer(side*w*.28,-d*.17,h+rise*.42,side,roof)
        elif variant=="PilotHouse":
            self.dormer(w*.25,d*.16,h+rise*.47,1,roof)
        elif variant=="NetLoft":
            # 천 작업장에 채광과 통풍을 주는 솟을지붕.
            z=h+rise+.14
            self.box("Oak",(0,d*.08,z),(.98,d*.58,.82))
            for side in (-1,1):
                for i in range(9):
                    q=d*.08-d*.27+i*d*.067
                    self.box("Glass",(side*.50,q,z),(.105,.45,.55))
                    self.box("Painted",(side*.563,q,z),(.09,.08,.64))
                slope=Quaternion((0,1,0),side*.53)
                self.box(roof,(side*.402,d*.08,z+.64),(.94,d*.63,.17),.035,slope)
                self.beam("Oak",(side*.80,-d*.24,z+.43),(side*.80,d*.40,z+.43),.15)
            self.beam(roof,(0,-d*.25,z+.87),(0,d*.41,z+.87),.19)
        for sx in (-1,1):
            for sy in (-1,1):
                for i in range(round(h/.44)):
                    if not detail and i%2==1:
                        continue
                    self.box("Carved" if warehouse else "Oak",
                             (sx*(w*.5-.078),sy*(d*.5-.08),.92+i*.40),
                             (.35+.11*(i%2),.35+.11*((i+1)%2),.35 if detail else .73))
            # 골목 입면도 깊이가 있는 창과 빗물받이를 가진다.
            for y in (-d*.26,d*.26):
                for z in (2.26,min(h-1.05,4.91)):
                    x=sx*w*.5
                    self.box("Glass",(x+sx*.05,y,z),(.13,1.03,1.17))
                    self.box("Carved",(x+sx*.124,y,z-.635),(.33,1.32,.16))
                    self.box("Oak",(x+sx*.15,y,z),(.12,.073,1.19))
                    for k in (-1,1):
                        self.box("Painted",(x+sx*.13,y+k*.65,z),(.14,.25,1.2))
                    self.box("Oak",(x+sx*.04,y,z+.65),(.17,1.32,.12))
            chimney_x=sx*w*.27
            if sx==-1 and variant not in ("NetLoft","PilotHouse"):
                for k in range(10 if detail else 5):
                    zz=h+rise*.43+k*(.22 if detail else .44)
                    self.box("Masonry",(chimney_x,d*.19,zz),(.83,.92,.25 if detail else .49),.033)
                self.box("Carved",(chimney_x,d*.19,h+rise*.43+2.14),(1.09,1.08,.19))
                self.rod("Metal",(chimney_x,d*.19,h+rise*.43+2.23),
                         (chimney_x,d*.19,h+rise*.43+2.48),.22)
        front=-d*.5
        for side in (-1,1):
            yf=side*(d*.5+.115)
            z=h+rise*.31
            self.rod("Carved",(0,yf,z),(0,yf+side*.055,z),.52,16)
            self.rod("Glass",(0,yf+side*.061,z),(0,yf+side*.092,z),.39,16)
            for offset in (-.24,-.12,0,.12,.24):
                half=math.sqrt(.365**2-offset**2)
                self.box("Oak",(0,yf+side*.115,z+offset),(half*2,.071,.055),.01)
        if warehouse:
            for x in (-w*.30,0,w*.30):
                self.fascia(x,front,1.51,2.13,2.21,True,True,False)
                self.fascia(x,front,4.47,1.45,1.21,False)
                for j in range(3 if detail else 1):
                    self.box("Carved",(x,front-.29-.19*j,.38-.13*j),
                             (2.54,.43,.22),.029)
            if variant=="NetLoft":
                self.canopy(0,front,3.44,w*.77,.91)
            else:
                for x in (-w*.32,w*.32):
                    self.beam("Oak",(x,front,5.94),(x,front-.76,5.94),.20,.24)
                    self.rod("Rope",(x,front-.71,5.87),(x,front-.71,4.97),.035)
        else:
            self.fascia(-w*.22,front,1.46,1.36,2.27,variant!="PilotHouse",True)
            self.fascia(w*.20,front,1.66,1.86,1.47,variant=="MerchantInn",False)
            for x in (-w*.26,0,w*.26):
                if variant in ("PilotHouse","MerchantInn") and not x:
                    continue
                self.fascia(x,front,min(h-1.06,4.67),1.02,1.29,
                            variant=="RopewalkHouse",False)
            if variant in ("PilotHouse","MerchantInn"):
                ledge=h*.51
                self.box("Oak",(0,front-.32,ledge),(2.78,.74,.29),.044)
                self.box("Sage" if variant=="PilotHouse" else "Ochre",(0,front-.28,ledge+1.19),
                         (2.52,.61,2.26),.11)
                for x in (-.72,0,.72):
                    self.fascia(x,front-.58,ledge+1.17,.52,1.15,False,False,False)
                self.box("Carved",(0,front-.33,ledge+2.38),(2.80,.75,.21))
                for x in (-1.07,1.07):
                    self.beam("Oak",(x,front,ledge-.88),(x,front-.61,ledge-.03),.15)
            if variant in ("MerchantInn","Fishmonger","RopewalkHouse"):
                self.canopy(.63,front,3.10,min(4.3,w*.53),.96)
            for x in (-w*.23,w*.40):
                self.beam("Oak",(x,front,3.60),(x,front-.68,3.60),.083)
                self.box("Painted",(x,front-.53,3.28),(.78,.15,.38),.048)
                self.rod("Metal",(x+.16,front-.69,3.28),(x+.16,front-.74,3.28),.089,10)
        # 멀리서도 앞뒤가 읽히며 외곽 골목에 빈 벽이 남지 않는다.
        back=d*.5
        for x in (-w*.26,w*.26):
            self.box("Glass",(x,back+.052,4.33),(1.02,.15,1.31))
            self.box("Carved",(x,back+.125,3.60),(1.32,.32,.18))
            self.box("Oak",(x,back+.134,4.38),(.068,.10,1.23))
            for side in (-1,1):
                self.box("Painted",(x+side*.66,back+.121,4.33),(.23,.15,1.29))

    def barrel(self,x,y,z,r):
        zs=(0,.20,.43,.66,.86); rr=(.82,.99,1,.98,.83); sides=16
        for i in range(sides):
            a=(i+.035)*math.tau/sides
            b=(i+.965)*math.tau/sides
            p=[(x+rad*r*math.cos(t),y+rad*r*math.sin(t),z+h*r*2)
               for h,rad in zip(zs,rr) for t in (a,b)]
            self.add("Oak" if i%3 else "Earth",p,
                     [(j,j+1,j+3,j+2) for j in range(0,8,2)])
        for h,rad in [(0,.82),(.13,.94),(.64,1),(.82,.86)]:
            self.rod("Metal",(x,y,z+h*r*2),(x,y,z+h*r*2+min(.064,r*.23)),r*rad,16)
        self.rod("Oak",(x,y,z+r*1.70),(x,y,z+r*1.74),r*.82,16)

    def crate(self,x,y,z,w):
        for j in range(5):
            dz=w*(j+.5)/5
            self.box("Oak",(x,y,z+dz),(w,w*.79,w*.19),.027)
        for a in (-1,1):
            for b in (-1,1):
                self.box("Carved",(x+a*w*.44,y+b*w*.37,z+w*.50),(w*.093,w*.11,w*1.04))
        self.beam("Earth",(x-w*.39,y-w*.41,z+w*.12),
                  (x+w*.39,y-w*.41,z+w*.88),w*.086)

    def yard(self,kind):
        if kind=="FreightYard":
            for x,y,z,w in [(-1.72,.37,0,1.13),(-.46,.49,0,1.05),
                           (-1.69,.38,1.13,.80),(.98,.53,0,1.27)]:
                self.crate(x,y,z,w)
            for x,y,r in [(1.79,-.87,.43),(.82,-.72,.37),(-.05,-1.08,.35)]:
                self.barrel(x,y,0,r)
            for k in range(45):
                a=k*math.tau/15
                b=(k+1)*math.tau/15
                r=.16+k*.006
                self.rod("Rope",(-1.6+r*math.cos(a),-1.05+r*math.sin(a),.04),
                         (-1.6+(r+.006)*math.cos(b),-1.05+(r+.006)*math.sin(b),.04),.019,6)
        elif kind=="NetYard":
            for x in (-2.1,0,2.1):
                self.rod("Oak",(x,.53,0),(x,.53,2.92),.116,10,taper=.75)
                self.beam("Oak",(x,.53,.71),(x,-.25,.08),.091)
            self.beam("Oak",(-2.2,.53,2.68),(2.25,.53,2.68),.105)
            for u in range(25):
                x=-2.04+u*.17
                for z in range(9):
                    p=.68+z*.21
                    sag=-.23*math.sin(math.pi*u/24)*(9-z)/9
                    a=(x,.54+sag,p)
                    if u<24:
                        self.rod("Rope",a,(x+.17,.54+sag,p),.017,5)
                    if z<8:
                        self.rod("Rope",a,(x,.55+sag,p+.21),.017,5)
                if u%3==0:
                    self.rod("Linen",(x,.54,2.51),(x,.54,2.62),.046,8)
            self.crate(-1.50,-.78,0,.72)
            self.barrel(.97,-.77,0,.39)
            for j in range(5):
                self.beam("Oak",(.18,-.37,.055+j*.083),(2.02,-.30,.11+j*.083),.083)
        elif kind=="MarketGoods":
            for x in (-1.05,1.05):
                for y in (-.41,.43):
                    self.box("Oak",(x,y,.46),(.16,.16,.92))
            for i in range(8):
                self.box("Oak",(0,(i-3.5)*.132,.92),(2.57,.129,.118))
            for x,y in [(-.74,-.11),(-.05,.05),(.68,.07)]:
                self.box("Earth",(x,y,1.012),(.61,.57,.10))
                for k in range(9):
                    xx=x+(k%3-1)*.144+self.rng.uniform(-.04,.04)
                    yy=y+(k//3-1)*.148
                    self.rod("Fruit" if k%3 else "Leaves",(xx,yy,1.044),(xx,yy,1.16),.071,8,.66)
                for sign in (-1,1):
                    self.box("Oak",(x+sign*.29,y,1.101),(.054,.60,.18))
                    self.box("Oak",(x,y+sign*.282,1.101),(.61,.051,.18))
            self.barrel(-1.5,.15,0,.31)
            self.crate(1.57,.29,0,.53)
        else:
            for y in (-.78,.78):
                for x in (-1.66,1.66):
                    self.box("Oak",(x,y,.19),(.19,.19,.38))
                for i in range(3):
                    self.box("Oak",(0,y,.16+i*.11),(3.5,.12,.098))
            self.box("Earth",(0,0,.10),(3.41,1.49,.19))
            for x in [-1.37,-.92,-.45,.01,.45,.93,1.37]:
                for y in (-.46,0,.46):
                    height=self.rng.uniform(.26,.48)
                    for j in range(4):
                        a=j*math.pi*.50
                        self.rod("Leaves",(x,y,.15),(x+math.cos(a)*.16,y+math.sin(a)*.16,height),
                                 .046,6,.25)
                    self.rod("Fruit",(x,y,height-.12),(x,y,height+.02),.06,6,.58)

    def make(self,name,collection):
        mesh=bpy.data.meshes.new(name)
        verts=[]; faces=[]; ranges=[]
        for mat, (vv, ff) in self.parts.items():
            ix=len(verts); start=len(faces)
            verts.extend(vv); faces.extend(tuple(ix+j for j in f) for f in ff)
            mesh.materials.append(self.materials[mat]); ranges.append((start,len(faces)))
        mesh.from_pydata(verts,[],faces); mesh.update()
        uv=mesh.uv_layers.new(name="UVMap")
        for i,(start,end) in enumerate(ranges):
            for index in range(start,end):
                poly=mesh.polygons[index]
                poly.material_index=i
                axis=max(range(3),key=lambda j:abs(poly.normal[j]))
                for j in poly.loop_indices:
                    p=mesh.vertices[mesh.loops[j].vertex_index].co
                    uv.data[j].uv=(p.y*.55,p.z*.55) if axis==0 else (p.x*.55,p.z*.55) if axis==1 else (p.x*.55,p.y*.55)
        assert mesh.validate(verbose=False) is False, name
        mesh.calc_loop_triangles()
        obj=bpy.data.objects.new(name,mesh);collection.objects.link(obj)
        return obj,len(mesh.loop_triangles)


def export(workspace):
    target=workspace/("Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/"
                      "Authoring/HarborCity")
    scene=bpy.context.scene
    label="River Basin Harbor Architecture"
    previous=bpy.data.collections.get(label)
    if previous is not None:
        assert all(obj.name.startswith("RBA_") for obj in previous.objects), label
        for obj in list(previous.objects):
            bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.collections.remove(previous)
    collection=bpy.data.collections.new(label)
    scene.collection.children.link(collection)
    materials={}
    for name,value in FINISHES.items():
        label="RBA_"+name
        m=bpy.data.materials.get(label) or bpy.data.materials.new(label)
        m.diffuse_color=(*value,1)
        m.use_nodes=True
        m.node_tree.nodes.get("Principled BSDF").inputs["Base Color"].default_value=(*value,1)
        materials[name]=m
    variants=["MerchantInn","PilotHouse","Fishmonger","RopewalkHouse","BondedStore","NetLoft"]
    yards=["FreightYard","NetYard","MarketGoods","KitchenGarden"]
    records={}
    for seed,name in enumerate(variants+yards,61):
        parts=[]
        for lod in range(2 if name in variants else 1):
            k=Kit(scene,materials,seed)
            if name in yards:
                k.yard(name)
            else:
                k.building(name,lod==0)
            obj,n=k.make("RBA_"+name+"_LOD"+str(lod),collection)
            records[obj.name]=n
            parts.append(obj)
        bpy.ops.object.select_all(action="DESELECT")
        for obj in parts:
            obj.select_set(True)
        bpy.context.view_layer.objects.active=parts[0]
        bpy.ops.export_scene.fbx(filepath=str(target/("RBA_"+name+".fbx")),use_selection=True,
                                 axis_forward="-Z",axis_up="Y",apply_unit_scale=True,
                                 apply_scale_options="FBX_SCALE_UNITS",bake_space_transform=True,
                                 use_mesh_modifiers=True,bake_anim=False,add_leaf_bones=False)
        for index,obj in enumerate(parts):
            obj.location = ((seed-61)%4*18, (seed-61)//4*16, 0)
            obj.hide_set(index != 0)
    src=workspace/"Tools/RiverBasinWorld/HarborCity/RiverBasinHarborArchitecture.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(src),copy=True)
    return records
