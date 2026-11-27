import bpy
import math
import os
import random
from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))).replace("\\", "/") + "/"
TARGET = ROOT + "Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Authoring/ElvenVillage/"
SOURCE = ROOT + "Tools/RiverBasinWorld/ElvenVillage/"
PALETTE = {
    "Ash": (.49,.29,.13), "PaleAsh": (.67,.45,.24), "EndGrain": (.27,.14,.06),
    "Verdigris": (.105,.31,.25), "VerdigrisLight": (.16,.39,.31),
    "VerdigrisDark": (.075,.23,.21), "Bronze": (.43,.25,.085),
    "Canvas": (.65,.46,.20), "CanvasRed": (.36,.105,.08), "Stone": (.32,.29,.22),
    "Glass": (.93,.53,.16), "Shadow": (.16,.095,.048),
}
MATS = {}

class Mesh:
    def __init__(self):
        self.v, self.f, self.tags, self.uv, self.smooth = [], [], [], [], []

    def face(self, indices, mat, uv=None, smooth=False):
        self.f.append(indices)
        self.tags.append(mat)
        self.uv.append(uv)
        self.smooth.append(smooth)

    def grid(self, fn, nu, nv, mat, flip=False, smooth=False):
        start = len(self.v)
        for j in range(nv+1):
            for i in range(nu+1):
                self.v.append(fn(i/nu, j/nv))
        for j in range(nv):
            for i in range(nu):
                a = start+j*(nu+1)+i
                ids = [a,a+1,a+nu+2,a+nu+1]
                uv = [(i/nu,j/nv),((i+1)/nu,j/nv),((i+1)/nu,(j+1)/nv),(i/nu,(j+1)/nv)]
                self.face(ids[::-1] if flip else ids, mat, uv[::-1] if flip else uv, smooth)

    def tube(self, points, sizes, mat, sides=8, reference=(0,1,0)):
        start = len(self.v)
        p = [Vector(x) for x in points]
        run = [0.]
        for a,b in zip(p,p[1:]):
            run.append(run[-1]+(b-a).length)
        for i, center in enumerate(p):
            axis = (p[min(i+1,len(p)-1)]-p[max(0,i-1)]).normalized()
            x = Vector(reference).cross(axis).normalized()
            if x.length < .01:
                x = Vector((1,0,0)).cross(axis).normalized()
            y = axis.cross(x)
            sx,sy = sizes[i]
            for j in range(sides):
                phase = (j+.5)*math.tau/sides
                self.v.append(tuple(center+x*(math.cos(phase)*sx)+y*(math.sin(phase)*sy)))
        for i in range(len(p)-1):
            for j in range(sides):
                a = start+i*sides
                k = (j+1)%sides
                self.face([a+j,a+k,a+sides+k,a+sides+j],mat,[(j/sides,run[i]),((j+1)/sides,run[i]),((j+1)/sides,run[i+1]),(j/sides,run[i+1])],sides>8)
        self.face(list(range(start,start+sides))[::-1],mat)
        end = start+(len(p)-1)*sides
        self.face(list(range(end,end+sides)),mat)

    def board(self, x,y,z,w,l,h,mat):
        # 널판 단면의 모서리를 깎아 빛이 닿는 폭을 일정하게 유지한다.
        bevel = min(.026,w*.14,h*.24)
        outline = [(-w/2+bevel,-h/2),(w/2-bevel,-h/2),(w/2,-h/2+bevel),(w/2,h/2-bevel),(w/2-bevel,h/2),(-w/2+bevel,h/2),(-w/2,h/2-bevel),(-w/2,-h/2+bevel)]
        start = len(self.v)
        self.v.extend((x+u,y+v,z+t) for v in [-l/2,l/2] for u,t in outline)
        for i in range(8):
            j=(i+1)%8
            self.face([start+i,start+8+i,start+8+j,start+j],mat,[(0,0),(0,l),(w,l),(w,0)])
        self.face(list(range(start,start+8)),mat)
        self.face(list(range(start+8,start+16))[::-1],mat)

    def object(self, name, collection):
        mesh=bpy.data.meshes.new(name)
        mesh.from_pydata(self.v,[],self.f)
        uv=mesh.uv_layers.new(name="UVMap")
        keys=list(dict.fromkeys(self.tags))
        for key in keys:
            mesh.materials.append(MATS[key])
        for poly,key,coords,smooth in zip(mesh.polygons,self.tags,self.uv,self.smooth):
            poly.material_index=keys.index(key)
            poly.use_smooth=smooth
            for index,li in enumerate(poly.loop_indices):
                if coords:
                    uv.data[li].uv=coords[index]
                else:
                    co=mesh.vertices[mesh.loops[li].vertex_index].co
                    uv.data[li].uv=(co.x,co.z)
        mesh.update()
        obj=bpy.data.objects.new(name,mesh)
        collection.objects.link(obj)
        return obj

def curve(mesh, fn, count, radius, mat, sides=8, ref=(0,1,0)):
    points=[fn(i/count) for i in range(count+1)]
    sizes=[(radius*(1-.46*i/count),radius*(1-.32*i/count)) for i in range(count+1)]
    mesh.tube(points,sizes,mat,sides,ref)

def lamp(m,x,y,z):
    p=lambda a,b,c:(x+a,y+b,z+c)
    m.tube([p(0,0,.10),p(0,0,.15),p(0,0,.26)],[(.145,.145),(.126,.126),(.025,.025)],"Bronze",8)
    m.tube([p(0,0,-.39),p(0,0,-.29),p(0,0,-.22)],[(.015,.015),(.094,.094),(.121,.121)],"Bronze",8)
    m.tube([p(0,0,-.24),p(0,0,-.16),p(0,0,.07),p(0,0,.11)],[(.105,.105),(.119,.119),(.121,.121),(.081,.081)],"Glass",8)
    for k in range(4):
        a=math.pi*k/2
        m.tube([p(.135*math.cos(a),.135*math.sin(a),.13),p(.13*math.cos(a),.13*math.sin(a),-.26)],[(.016,.016)]*2,"Bronze",6)

def hall(detail):
    m=Mesh()
    rng=random.Random(781)
    def roof(s,y,side,lift=0):
        return (side*(4.82+.57*y*y)*s,5.8*y*(1-.19*s*s),7.25+1.03*y**4-4.04*s**.70+lift)
    n=[22,11,6][detail]
    for side in [-1,1]:
        m.grid(lambda u,v:roof(u,2*v-1,side,-.05),n,n,"EndGrain",flip=side>0,smooth=True)
        m.grid(lambda u,v:roof(u,2*v-1,side,-.032),n,n,"VerdigrisDark",flip=side<0,smooth=True)
        cols=[25,13,5][detail]
        rows=[13,8,3][detail]
        for row in range(rows):
            for col in range(cols):
                off=.35*(row%2)
                a=max(0,(col-off)/cols)
                b=min(1,(col+1.012-off)/cols)
                if col==cols-1:
                    b=1
                mat=rng.choices(["Verdigris","VerdigrisDark","VerdigrisLight"],[9,3,4])[0]
                m.grid(lambda u,v:roof(max(0,min(1,(row+u*1.13)/rows)),2*(a+(b-a)*(0.025+.95*v))-1,side,.065+.058*u),3 if detail==0 else 2,2 if detail==0 else 1,mat,flip=side<0,smooth=True)
        for y in [-1,1]:
            curve(m,lambda t:roof(t,y,side,.06),n,.16,"PaleAsh",10)
            curve(m,lambda t:roof(t,y*.982,side,.18),n,.045,"Bronze",8)
        curve(m,lambda t:roof(1,2*t-1,side,0),n,.15,"Ash",10,(0,0,1))
    curve(m,lambda t:roof(0,2*t-1,1,.10),n,.19,"Bronze",10,(0,0,1))
    for y in [-1,-.54,0,.54,1]:
        for side in [-1,1]:
            def under_roof(x,longitudinal,z):
                y_scale=min(1,abs(longitudinal)/5.1)
                limit=7.25+.75*y_scale**4-4.04*(abs(x)/(4.82+.57*y_scale*y_scale))**.70
                return (x,longitudinal,min(z,limit-.24))
            def arch(t, offset=0):
                x=side*(3.2+1.05*math.sin(t*math.pi)-3.2*t**1.2)
                return under_roof(x,4.35*y+.16*math.sin(t*math.pi)*y+offset,7.18*t**1.15+y*y*.48*t**3)
            curve(m,arch,n,.31,"Ash",12)
            curve(m,lambda t:arch(t,.20*y if y else .19),n,.083,"PaleAsh",8)
            if detail<2:
                for k in range(3):
                    curve(m,lambda t:under_roof(side*(3.32-k*.14-2.3*t+(.46+k*.10)*math.sin(math.pi*t)),4.35*y+.21*y,2.25+k*.57+(3.75-k*.23)*t),n//2+1,.074,"PaleAsh",8)
            m.board(side*3.18,4.35*y,.105,.68,.74,.21,"Stone")
            m.board(side*3.18,4.35*y,.36,.49,.53,.14,"Bronze")
            if detail==0 and abs(y)>.2:
                lamp(m,side*2.80,4.35*y,2.65)
    for side in [-1,1]:
        for k in range(4):
            a=-4.32+2.16*k
            curve(m,lambda t:(side*(3.35+.77*math.sin(math.pi*t)),a+2.16*t,3.02+.52*math.sin(math.pi*t)),n//2+1,.12,"Ash",8,(1,0,0))
    for y in [-1,1]:
        curve(m,lambda t:(.14*math.sin(t*math.pi),y*(5.52+.59*t),7.91+1.02*t),n//2+1,.12,"Bronze",8)
    return m

def rail(detail):
    m=Mesh()
    count=[16,8,4][detail]
    for z in [.14,.92]:
        m.board(0,0,z,.156,2,.13,"Ash")
    for y in [-.94,.94]:
        m.board(0,y,.50,.19,.18,1.0,"PaleAsh")
    for side in [-1,1]:
        curve(m,lambda t:(0,side*(.92-.92*t),.23+.57*math.sin(.5*math.pi*t)),count,.07,"PaleAsh",8,(1,0,0))
        curve(m,lambda t:(0,side*(.92-.92*t),.77-.53*math.sin(.5*math.pi*t)),count,.061,"Ash",8,(1,0,0))
    if detail<2:
        for sign in [-1,1]:
            curve(m,lambda t:(.02,sign*.225*math.sin(math.pi*t),.24+.59*t),count,.032,"Bronze",8,(1,0,0))
    return m

def deck(detail):
    m=Mesh()
    rng=random.Random(81)
    for i in range([7,4,2][detail]):
        n=[7,4,2][detail]
        m.board(0,(i+.5)*2/n-1,-.068,3.4,2/n-.022,.132,rng.choice(["Ash","PaleAsh","Ash"]))
    for x in [-1.38,1.38]:
        m.board(x,0,-.235,.21,2,.27,"EndGrain")
    if detail<2:
        for x in [-1.1,1.1]:
            for i in range(7):
                m.tube([(x,(i+.5)*2/7-1,-.004),(x,(i+.5)*2/7-1,.015)],[(.019,.019)]*2,"Bronze",6)
    return m

def market(detail):
    m=Mesh()
    rng=random.Random(112)
    n=[16,9,4][detail]
    for x in [-1,1]:
        for y in [-1,1]:
            curve(m,lambda t:(x*(1.68+.24*math.sin(t*math.pi)),y*1.67,2.76*t),n,.145,"Ash",10)
            curve(m,lambda t:(x*(1.71-.80*t),y*(1.67-.62*t),1.52+1.49*t),n//2,.079,"PaleAsh")
    def fabric(u,v):
        return (4.5*(u-.5),4.34*(v-.5),3.04-.67*abs(2*u-1)**1.45-.15*(2*v-1)**2+.036*math.cos(v*9*math.pi)*abs(2*u-1)**3)
    for i in range(7):
        mat="CanvasRed" if i in (1,5) else "Canvas"
        m.grid(lambda u,v:fabric((i+u)/7,v),max(2,n//3),n,mat,smooth=True)
        m.grid(lambda u,v:tuple(Vector(fabric((i+u)/7,v))-Vector((0,0,.018))),max(2,n//3),n,mat,flip=True,smooth=True)
        if detail<2:
            curve(m,lambda t:fabric(i/7,t),n,.022,"PaleAsh",6,(0,0,1))
    for side in [-1,1]:
        curve(m,lambda t:fabric(t,(side+1)/2),n,.048,"Bronze",8)
    for i in range(9 if detail==0 else 4):
        z=.87+rng.uniform(-.014,.014)
        m.board(0,-1.35+(i+.5)*1.05/(9 if detail==0 else 4),z,3.34,1.02/(9 if detail==0 else 4),.095,"PaleAsh")
    for x in [-1.42,1.42]:
        for y in [-1.26,-.42]:
            m.board(x,y,.40,.11,.12,.81,"Ash")
    m.board(0,-.87,.33,3.17,.77,.08,"EndGrain")
    if detail==0:
        lamp(m,1.53,-1.80,1.98)
    return m

def brace(detail):
    m=Mesh()
    n=[22,12,6][detail]
    # 두 갈래가 하중을 받는 턱에서 갈라져 전망대 밑면으로 닿는다.
    for side in [-1,1]:
        curve(m,lambda t:(side*2.55*t**1.65,.68*math.sin(.5*math.pi*t),-4.8+4.8*t),n,.33,"Ash",12)
        curve(m,lambda t:(side*2.55*t**1.65,-.052+.65*math.sin(.5*math.pi*t),-4.8+4.8*t),n,.083,"PaleAsh",8)
        if detail<2:
            curve(m,lambda t:(side*(.39+1.83*t),.34*math.sin(.5*math.pi*t),-2.16+2.16*t),n,.142,"EndGrain",10)
        m.board(side*2.35,.62,-.04,.65,.61,.18,"EndGrain")
    return m

def run():
    assert not os.path.exists(SOURCE+"RiverBasin_ElvenVillage.blend")
    os.makedirs(TARGET,exist_ok=True)
    os.makedirs(SOURCE,exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene=bpy.context.scene
    scene.unit_settings.system="METRIC"
    for key, rgb in PALETTE.items():
        mat=bpy.data.materials.new("RBElf_"+key)
        mat.diffuse_color=(*rgb,1)
        MATS[key]=mat
    counts={}
    for name, maker in [("Hall",hall),("Rail",rail),("Deck",deck),("Market",market),("Brace",brace)]:
        for obj in bpy.context.selected_objects:
            obj.select_set(False)
        col=bpy.data.collections.new("RBElf_"+name)
        scene.collection.children.link(col)
        for i in range(3):
            obj=maker(i).object("SM_RBElf_"+name+"_LOD"+str(i),col)
            obj.data.calc_loop_triangles()
            counts[obj.name]=len(obj.data.loop_triangles)
            obj.select_set(True)
        bpy.context.view_layer.objects.active=col.objects[0]
        bpy.ops.export_scene.fbx(filepath=TARGET+"SM_RBElf_"+name+".fbx",use_selection=True,axis_forward="-Z",axis_up="Y",add_leaf_bones=False,bake_anim=False,apply_unit_scale=True)
        for obj in col.objects:
            obj.select_set(False)
            obj.hide_set(not obj.name.endswith("LOD0"))
        if name!="Hall":
            for obj in col.objects:
                obj.location.x={"Rail":10,"Deck":15,"Market":20,"Brace":27}[name]
    for s in bpy.data.screens:
        for area in s.areas:
            if area.type=="VIEW_3D":
                area.spaces.active.region_3d.view_distance=22
                area.spaces.active.region_3d.view_location=(4,0,2)
                area.spaces.active.shading.color_type="MATERIAL"
    bpy.ops.wm.save_as_mainfile(filepath=SOURCE+"RiverBasin_ElvenVillage.blend")
    return counts

def dwelling(detail):
    m=Mesh()
    rng=random.Random(596)
    # 박공 아래에 창과 현관을 낸 수관 주거. 열린 회관과 같은 지붕 치수를 쓴다.
    front, back = 1.88, -2.4
    count=[13,8,5][detail]
    for side in [-1,1]:
        for row in range(count):
            t=(row+.5)/count
            m.board(side*2.47,back+(front-back)*t,1.52,.13,(front-back)/count-.016,3.04,rng.choice(["Ash","Ash","PaleAsh"]))
    for side in [-1,1]:
        for row in range(count):
            t=(row+.5)/count
            x=side*(.89+1.52*t)
            height=5.62-1.03*abs(x)**.8
            m.board(x,front,height/2,.145,.175,height,rng.choice(["Ash","PaleAsh"]))
    for row in range(count*2):
        x=(row+.5)*4.88/(count*2)-2.44
        height=5.8-1.03*abs(x)**.8
        m.board(x,back,height/2,4.88/(count*2)-.017,.15,height,rng.choice(["Ash","PaleAsh"]))
    for side in [-1,1]:
        for y in [front,back]:
            m.board(side*2.43,y,1.58,.26,.26,3.18,"EndGrain")
    for y in [front+.11,back-.12]:
        for x in [-1.65,1.65]:
            m.board(x,y,2.05,.85,.13,1.29,"EndGrain")
            m.board(x,y+(.082 if y>0 else -.082),2.02,.67,.075,1.04,"Glass")
            for dx in [-.38,0,.38]:
                m.board(x+dx,y+(.135 if y>0 else -.135),2.04,.065,.07,1.35,"Bronze")
            m.board(x,y+(.14 if y>0 else -.14),1.40,1.03,.29,.117,"PaleAsh")
    for side in [-1,1]:
        curve(m,lambda t:(side*.92*(1-.88*t**3),front+.18,.06+3.33*t),[18,10,5][detail],.145,"PaleAsh",10)
        m.board(side*1.0,front+.15,.084,.32,.32,.168,"Stone")
    m.board(0,front+.35,.055,1.85,.67,.11,"EndGrain")
    return m

def stone_course(detail):
    m=Mesh()
    rng=random.Random(142)
    rows=[3,3,2][detail]
    for j in range(rows):
        x=-1.56
        while x<1.555:
            w=min(1.56-x,rng.uniform(.32,.68) if detail==0 else rng.uniform(.47,.78))
            if w<.05: break
            h=1.18/rows
            m.board(x+w*.50,.035+rng.uniform(-.025,.025),-.59+(j+.5)*h,w-.018,.23+rng.uniform(-.035,.035),h-.022,rng.choice(["Stone","Stone","Shadow"]))
            x+=w
    return m

def dye(detail):
    m=Mesh()
    n=[12,7,4][detail]
    for x in [-2.0,2.0]:
        curve(m,lambda t:(x,.13*math.sin(t*math.pi),3.04*t),n,.128,"Ash",10)
        curve(m,lambda t:(x*(1-.27*t),0,2.15+.86*t),n,.081,"PaleAsh")
    curve(m,lambda t:(4*t-2,0,2.91+.08*math.cos(t*math.tau)),n,.059,"EndGrain",8,(0,0,1))
    for j,(w,h,mat) in enumerate([(.51,1.61,"CanvasRed"),(.54,1.84,"Verdigris"),(.63,1.47,"Canvas"),(.48,1.76,"VerdigrisDark"),(.55,1.48,"CanvasRed")]):
        x=-1.8+j*.74
        def cloth(u,v):
            wave=.06*math.sin(u*math.tau*1.2+j)*(.35+.65*v)+.14*math.sin(v*math.pi)
            return (x+w*u+.036*v*math.cos(j),wave,2.83-v*h-.038*math.sin(u*math.pi)*v)
        m.grid(cloth,n,n//2,mat,smooth=True)
        m.grid(lambda u,v:tuple(Vector(cloth(u,v))+Vector((0,.013,0))),n,n//2,mat,flip=True,smooth=True)
    for i in [-1,1]:
        x=i*1.23
        y=-.72
        m.tube([(x,y,.04),(x,y,.13),(x,y,.56),(x,y,.72)],[(.35,.35),(.4,.4),(.46,.46),(.46,.46)],"Ash",16)
        m.tube([(x,y,.19),(x,y,.22)],[(.42,.42)]*2,"Bronze",16)
        m.tube([(x,y,.61),(x,y,.65)],[(.475,.475)]*2,"Bronze",16)
        m.tube([(x,y,.727),(x,y,.737)],[(.398,.398)]*2,"CanvasRed" if i<0 else "VerdigrisDark",16)
    return m

def supplement(refine=False):
    bpy.ops.wm.open_mainfile(filepath=SOURCE+"RiverBasin_ElvenVillage.blend")
    scene=bpy.context.scene
    bpy.context.preferences.filepaths.save_version=0
    for key, rgb in PALETTE.items():
        name="RBElf_"+key
        mat=bpy.data.materials.get(name)
        if mat is None:
            mat=bpy.data.materials.new(name)
            mat.diffuse_color=(*rgb,1)
        MATS[key]=mat
    result={}
    models=[("Hall",hall,0),("Market",market,20)] if refine else [("Dwelling",dwelling,35),("StoneCourse",stone_course,49),("Dye",dye,55)]
    for name,maker,offset in models:
        for obj in bpy.context.selected_objects:
            obj.select_set(False)
        label="RBElf_"+name
        if refine:
            old=bpy.data.collections[label]
            for obj in list(old.objects):
                bpy.data.objects.remove(obj,do_unlink=True)
            bpy.data.collections.remove(old)
        col=bpy.data.collections.new(label)
        scene.collection.children.link(col)
        for i in range(3):
            obj=maker(i).object("SM_RBElf_"+name+"_LOD"+str(i),col)
            obj.data.calc_loop_triangles()
            result[obj.name]=len(obj.data.loop_triangles)
            obj.select_set(True)
        bpy.context.view_layer.objects.active=col.objects[0]
        bpy.ops.export_scene.fbx(filepath=TARGET+"SM_RBElf_"+name+".fbx",use_selection=True,axis_forward="-Z",axis_up="Y",add_leaf_bones=False,bake_anim=False,apply_unit_scale=True)
        for obj in col.objects:
            obj.select_set(False)
            obj.hide_set(not obj.name.endswith("LOD0"))
            obj.location.x=offset
    bpy.ops.wm.save_as_mainfile(filepath=SOURCE+"RiverBasin_ElvenVillage.blend")
    return result

if __name__ == "__main__":
    import sys
    result=supplement(True) if "--refine" in sys.argv else supplement() if "--supplement" in sys.argv else run()
    print(result)
