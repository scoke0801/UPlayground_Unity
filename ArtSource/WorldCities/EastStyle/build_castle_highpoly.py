"""돌등성 전용 본성·문루·망루와 실루엣을 유지하는 3단계 LOD를 만든다.

별도 background Blender에서 실행한다. 설정은 castle_highpoly.json이 원본이다.
열린 Blender 작업과 기존 도시 에셋을 덮어쓰지 않는다.
"""
import bpy
import bmesh
import json
import math
import shutil
import sys
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

SOURCE = Path(__file__).resolve().parent
ROOT = SOURCE.parents[2]
CONFIG = json.loads((SOURCE / 'castle_highpoly.json').read_text(encoding='utf-8'))
OUT = ROOT / 'Assets/08.Models/WorldCities/StonegateHighPoly'
OUT.mkdir(parents=True, exist_ok=True)
STAGING = SOURCE / 'CastleExport'
STAGING.mkdir(exist_ok=True)
scene = bpy.data.scenes.new('StonegateHighPoly')
bpy.context.window.scene = scene
scene.unit_settings.system = 'METRIC'
materials = {}
for name, color in CONFIG['palette'].items():
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = (*color, 1)
    shader.inputs['Roughness'].default_value = .82
    materials[name] = mat


class MeshBatch:
    """재질별로 메시를 합쳐 기와 수와 Renderer 수를 분리한다."""
    def __init__(self, name, lod):
        self.name, self.lod = name, lod
        self.parts = {name: [[], [], []] for name in materials}
        self.transform = None

    def add(self, mat, vertices, faces, smooth=False):
        verts, polys, smoothing = self.parts[mat]
        start = len(verts)
        if self.transform:
            verts.extend(self.transform(v) for v in vertices)
        else:
            verts.extend(vertices)
        polys.extend(tuple(start + v for v in f) for f in faces)
        smoothing.extend([smooth] * len(faces))

    def box(self, mat, center, size, bevel=False):
        # 모따기 원형은 재사용하며 얇은 창살에는 적용하지 않는다.
        template = ROUND_BOX if bevel and self.lod == 0 else BOX
        vertices = [tuple(center[i] + v[i] * size[i] for i in range(3)) for v in template[0]]
        self.add(mat, vertices, template[1])

    def shell(self, mat, vertices, faces, thickness, smooth=False):
        # 처마를 아래에서 볼 때도 막히도록 닫힌 체적으로 만든다.
        count = len(vertices)
        closed = list(faces) + [tuple(i+count for i in reversed(face)) for face in faces]
        boundary = {}
        for face in faces:
            for a,b in zip(face,face[1:]+face[:1]):
                key=tuple(sorted((a,b)))
                if key in boundary:
                    del boundary[key]
                else:
                    boundary[key]=(a,b)
        for a,b in boundary.values():
            closed.append((b,a,a+count,b+count))
        self.add(mat, vertices+[(x,y,z-thickness) for x,y,z in vertices], closed, smooth)

    def tube(self, mat, points, radius, sides=None):
        sides = sides or (16 if self.lod == 0 else 8)
        vertices, faces = [], []
        for i, point in enumerate(points):
            tangent = Vector(points[min(i + 1, len(points) - 1)]) - Vector(points[max(i - 1, 0)])
            tangent.normalize()
            normal = tangent.cross(Vector((0, 0, 1)))
            if normal.length < .01:
                normal = Vector((1, 0, 0))
            normal.normalize()
            other = tangent.cross(normal).normalized()
            for j in range(sides):
                angle = j * math.tau / sides
                vertices.append(tuple(Vector(point) + radius * (normal * math.cos(angle) + other * math.sin(angle))))
        for i in range(len(points) - 1):
            for j in range(sides):
                a = i * sides + j
                b = i * sides + (j + 1) % sides
                faces.append((a, b, b + sides, a + sides))
        faces.extend([tuple(reversed(range(sides))), tuple(range(len(vertices) - sides, len(vertices)))])
        self.add(mat, vertices, faces, True)

    def finish(self):
        objects, report = [], {}
        for mat, (vertices, faces, smoothing) in self.parts.items():
            if not vertices:
                continue
            mesh = bpy.data.meshes.new(f'{self.name}_Detail{self.lod}_{mat}')
            mesh.from_pydata(vertices, [], faces)
            mesh.update()
            topology = bmesh.new()
            topology.from_mesh(mesh)
            bmesh.ops.recalc_face_normals(topology, faces=list(topology.faces))
            topology.to_mesh(mesh)
            topology.free()
            mesh.materials.append(materials[mat])
            uv = mesh.uv_layers.new(name='UVMap')
            for poly in mesh.polygons:
                poly.use_smooth = smoothing[poly.index]
                axes = (0, 1) if abs(poly.normal.z) > .6 else ((0, 2) if abs(poly.normal.y) > .6 else (1, 2))
                for loop in poly.loop_indices:
                    v = mesh.vertices[mesh.loops[loop].vertex_index].co
                    uv.data[loop].uv = (v[axes[0]] / 3.2, v[axes[1]] / 3.2)
            mesh.calc_loop_triangles()
            assert all(math.isfinite(v) for vertex in mesh.vertices for v in vertex.co)
            degenerate = sum(1 for tri in mesh.loop_triangles if tri.area < 1e-9)
            assert degenerate == 0, (mesh.name, degenerate)
            obj = bpy.data.objects.new(mesh.name, mesh)
            scene.collection.objects.link(obj)
            objects.append(obj)
            report[mat] = {'vertices': len(mesh.vertices), 'triangles': len(mesh.loop_triangles)}
        return objects, report


BOX = ([(-.5,-.5,-.5),(.5,-.5,-.5),(.5,.5,-.5),(-.5,.5,-.5),
        (-.5,-.5,.5),(.5,-.5,.5),(.5,.5,.5),(-.5,.5,.5)],
       [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)])
bm = bmesh.new()
bmesh.ops.create_cube(bm, size=1)
bmesh.ops.bevel(bm, geom=list(bm.edges), offset=.035, segments=2, affect='EDGES')
bm.verts.ensure_lookup_table()
bm.verts.index_update()
ROUND_BOX = ([tuple(v.co) for v in bm.verts], [tuple(v.index for v in face.verts) for face in bm.faces])
bm.free()


def roof(batch, width, depth, z, cfg, upper_floor=None):
    """네 지붕 면의 개별 기와, 추녀 곡선, 용마루와 서까래를 저작한다."""
    a, b = width / 2 + cfg['overhang'], depth / 2 + cfg['overhang']
    is_apron = upper_floor is not None
    gable_break = 1 if is_apron else cfg.get('gableBreak',1)
    has_gable = not is_apron and gable_break < 1
    ridge = max(.8, a - b * gable_break)
    rise = cfg['rise'] * min(1, depth / 12 + .35)
    if is_apron:
        inner_a,inner_b=upper_floor['width']/2+.15,upper_floor['depth']/2+.15
        rise=upper_floor['base']+.15-z
        assert a>inner_a and b>inner_b and rise>0
    for side in range(4):
        front = side < 2
        sign = -1 if side % 2 == 0 else 1
        def point(u, t, lift=0):
            if not front:
                t *= gable_break
            v = 2 * u - 1
            height = z + rise * t ** 1.35 + cfg.get('cornerLift',.48) * abs(v) ** 8 * (1-t) ** 3 + lift
            if is_apron:
                x=a+(inner_a-a)*t
                y=b+(inner_b-b)*t
                return (v*x,sign*y,height) if front else (sign*x,v*y,height)
            extent = a + (ridge-a)*min(1,t/gable_break)
            if front:
                return (v * extent, sign*b*(1-t), height)
            return (sign*extent, v*b*(1-t), height)
        density = 1 if batch.lod == 0 else 1.7
        columns = max(4, round(2 * (a if front else b) / (cfg['tileWidth'] * density)))
        slope_run = b-inner_b if is_apron else b
        rows = max(4, round(slope_run / (cfg['tileLength'] * density)))
        # 마지막 옆면은 용마루 끝 한 점에 모이므로 별도 삼각형으로 닫는다.
        nx, ny = (columns, rows * 2) if batch.lod < 2 else (12, 8)
        verts = [point(x/nx, y/ny, -.08) for y in range(ny+1) for x in range(nx+1)]
        faces = []
        for y in range(ny):
            for x in range(nx):
                q = y*(nx+1)+x
                faces.append((q,q+1,q+nx+2) if not front and not has_gable and not is_apron and y == ny-1 else (q,q+1,q+nx+2,q+nx+1))
        batch.shell('Roof', verts, faces, .16)
        # 처마의 목재 테두리와 지붕 이면은 위쪽 관측뿐 아니라 보행 시 시야도 막는다.
        edge = [point(x/32, 0, -.12) for x in range(33)]
        batch.tube('Timber', edge, .12)
        if batch.lod == 2:
            continue
        segments = 8 if batch.lod == 0 else 4
        for col in range(columns):
            for row in range(rows):
                t0, t1 = row/rows, min(.985, (row+1.035)/rows)
                # 삼각 처마 끝에서는 폭이 0이 되는 기와를 생성하지 않는다.
                if not front and not has_gable and not is_apron and row == rows-1:
                    t1 = .97
                vertices = []
                for t in (t0, (t0+t1)/2, t1):
                    for k in range(segments+1):
                        u = (col + .04 + .92*k/segments)/columns
                        bulge = .075*math.sin(math.pi*k/segments) + .018
                        vertices.append(point(u, t, bulge))
                faces = []
                for j in range(2):
                    for k in range(segments):
                        q=j*(segments+1)+k
                        faces.append((q,q+1,q+segments+2,q+segments+1))
                batch.shell('Roof', vertices, faces, .035, True)
            if batch.lod == 0:
                batch.tube('Timber', [point((col+.5)/columns,t,-.31) for t in (0,.04,.08,.12,.18)], .055, 8)
        # 모서리의 굵은 내림마루로 네 면의 접합부를 덮는다.
        if front:
            for u in (0,1):
                batch.tube('Roof', [point(u,t/24,.15) for t in range(25)], .17)
    # 중간 처마는 상층 바깥의 사각 고리다. 내부에 지붕 면이나 용마루를 만들지 않는다.
    if is_apron:
        return
    if has_gable:
        # 합각을 수직 삼각 벽으로 닫고 박공선을 두어 사면 지붕과 다른 실루엣을 만든다.
        half_gable=b*(1-gable_break)
        foot=z+rise*gable_break**1.35
        for sign in (-1,1):
            x=sign*(ridge-.12)
            triangle=[(-half_gable,foot),(half_gable,foot),(0,z+rise)]
            vertices=[(x+offset,y,h) for offset in (-.14,.14) for y,h in triangle]
            batch.add('Plaster',vertices,[(0,2,1),(3,4,5),(0,1,4,3),(1,2,5,4),(2,0,3,5)])
            for edge in (-1,1):
                batch.tube('Timber',[(sign*(ridge+.08),edge*half_gable,foot),(sign*(ridge+.08),0,z+rise)],.16)
                batch.tube('PaintIvory',[(sign*(ridge+.22),edge*half_gable,foot+.04),(sign*(ridge+.22),0,z+rise-.04)],.045,6)
            for t in (-.6,-.3,0,.3,.6):
                y=half_gable*t
                top=z+rise-(rise-rise*gable_break**1.35)*abs(t)
                batch.box('Timber',(sign*(ridge+.04),y,(foot+top)/2),(.12,.14,max(.1,top-foot)))
    batch.tube('Roof', [(-ridge-.25,0,z+rise+.14),(ridge+.25,0,z+rise+.14)], .24)
    for sign in (-1,1):
        batch.tube('Bronze', [(sign*(ridge-.2),0,z+rise+.3),(sign*(ridge+.35),0,z+rise+.65),(sign*(ridge+.65),0,z+rise+1.1)], .13)


def stonework(batch, center, width, depth, height):
    batch.box('Stone', (center[0],center[1],center[2]+height/2), (width,depth,height))
    if batch.lod == 2:
        return
    courses = max(1, round(height/.65))
    for side in range(4):
        front, sign = side < 2, (-1 if side%2==0 else 1)
        length = width if front else depth
        count = max(1, round(length/1.6))
        for row in range(courses):
            # 석축 이음새를 반 칸 엇갈리게 하고 모서리는 기단 경계 안에 둔다.
            edges = [-length/2] + [-length/2+(i+.5*(row%2))*length/count for i in range(1,count)] + [length/2]
            for left,right in zip(edges,edges[1:]):
                mid=(left+right)/2
                position=(center[0]+(mid if front else sign*width/2),center[1]+(sign*depth/2 if front else mid),center[2]+(row+.5)*height/courses)
                size=(right-left-.045,.22,height/courses-.045) if front else (.22,right-left-.045,height/courses-.045)
                batch.box('Stone',position,size,True)
    batch.box('Stone',(center[0],center[1],center[2]+height-.04),(width+.4,depth+.4,.22),True)


def storey(batch, width, depth, base, height, facade, roof_config, upper_floor=None):
    """벽 전체를 창으로 채우지 않고 용도별 열린 칸·판문·벽을 조립한다."""
    batch.box('Timber',(0,0,base+.1),(width+.65,depth+.65,.32),True)
    batch.box('Timber',(0,0,base+height-.16),(width,depth,.25))
    if batch.lod==0:
        for x in range(round(width/.6)):
            batch.box('Timber',(-width/2+(x+.5)*.6,0,base+.23),(.57,depth,.08))
    for side in range(4):
        front, sign = side<2, (-1 if side%2==0 else 1)
        length = width if front else depth
        # Blender +Y가 FBX 임포트 뒤 Unity -Z 정면에 대응한다.
        pattern=facade['front' if front and sign==1 else 'back' if front else 'side']
        bays=len(pattern)
        bay_width=length/bays
        def p(along, outward, z):
            return (along,sign*(depth/2+outward),z) if front else (sign*(width/2+outward),along,z)
        def size(a,d,h):
            return (a,d,h) if front else (d,a,h)
        for i in range(bays+1):
            along = -length/2 + i*length/bays
            batch.tube('Vermilion',[p(along,0,base+.2),p(along,0,base+height)],.29,24 if batch.lod==0 else 10)
            batch.box('Stone',p(along,0,base+.25),size(.66,.66,.4),True)
            # 삼단 공포는 처마의 무게와 돌출 깊이를 읽을 수 있게 한다.
            for tier in range(3 if batch.lod<2 else 1):
                batch.box('Jade',p(along,.18+tier*.17,base+height-.45+tier*.19),size(.95+tier*.3,.65+tier*.35,.18),True)
                batch.box('Vermilion',p(along,.22,base+height-.6+tier*.18),size(.23,.95+tier*.4,.16))
        for z,thickness in ((base+height-.68,.42),(base+height-.12,.24)):
            batch.box('Jade',p(0,0,z),size(length+.4,.42,thickness),True)
            batch.box('PaintIvory',p(0,.225,z),size(length+.3,.025,.035))
        for i,style in enumerate(pattern):
            along=-length/2+(i+.5)*bay_width
            ww=bay_width-.55
            assert style in ('open','shutter','wall')
            if facade.get('enclosed', False):
                assert style != 'open', '닫힌 전각에는 개방 칸을 둘 수 없습니다.'
                # 기둥 뒤까지 벽체를 이어 문 위·아래와 부재 접합부의 관통 틈을 막는다.
                batch.box('Plaster',p(along,-.20,base+height/2),size(bay_width+.08,.42,height))
            if style=='wall':
                batch.box('Plaster',p(along,-.08,base+height*.43),size(ww,.30,height*.82))
                batch.box('Vermilion',p(along,.12,base+.45),size(ww,.13,.65))
            elif style=='shutter':
                # 두 짝의 판문은 큰 목재 면과 가로 띠쇠로 읽힌다. 유리와 격자창은 없다.
                panel_bottom=base+.6
                panel_height=height-1.5
                batch.box('Recess',p(along,-.12,panel_bottom+panel_height/2),size(ww,.18,panel_height+.16))
                for leaf in (-1,1):
                    cx=along+leaf*ww/4
                    batch.box('Vermilion',p(cx,.015,panel_bottom+panel_height/2),size(ww/2-.06,.22,panel_height),True)
                    for z in (panel_bottom+.35,panel_bottom+panel_height-.35):
                        batch.box('Jade',p(cx,.15,z),size(ww/2-.08,.11,.16),True)
                    if batch.lod<2:
                        for plank in range(1,5):
                            px=cx-ww/4+plank*ww/10
                            batch.box('Timber',p(px,.14,panel_bottom+panel_height/2),size(.018,.02,panel_height-.1))
                        batch.tube('Bronze',[p(along+leaf*.18,.21,panel_bottom+panel_height*.42),p(along+leaf*.18,.21,panel_bottom+panel_height*.53)],.045,8)
                for edge in (-1,1):
                    batch.box('Jade',p(along+edge*ww/2,.07,panel_bottom+panel_height/2),size(.13,.30,panel_height+.3),True)
            if style!='open':
                batch.box('Vermilion',p(along,-.03,base+.38),size(ww,.4,.42),True)
            if facade['rail']:
                # 개구부마다 낮은 난간을 두고 건물 전체를 수평 격자로 감싸지 않는다.
                for z in (base+.4,base+1.1):
                    batch.box('Vermilion',p(along,.48,z),size(ww,.15,.15),True)
                count=max(2,round(ww/.85))
                for k in range(count+1):
                    batch.box('Jade',p(along-ww/2+k*ww/count,.48,base+.75),size(.1,.1,.82),True)
            # 칸 사이 단청은 구조선에만 두어 판문과 열린 칸의 대비를 유지한다.
            if batch.lod<2:
                for k in (-1,0,1):
                    batch.box('PaintIvory',p(along+k*.26,.23,base+height-.68),size(.10,.035,.18))
    roof(batch,width,depth,base+height+.15,roof_config,upper_floor)


def archway(batch, cfg):
    """홍예의 쐐기돌과 홍예 위 석축을 닫힌 메시로 만든다."""
    radius=cfg['passageWidth']/2
    rise,spring,top=cfg['archRise'],cfg['archSpring'],cfg['passageHeight']
    depth=cfg['depth']
    count=40 if batch.lod<2 else 20
    for i in range(count):
        a,b=math.pi*i/count,math.pi*(i+1)/count
        inner=[(radius*math.cos(t),spring+rise*math.sin(t)) for t in (a,b)]
        outer=[((radius+.6)*math.cos(t),spring+(rise+.55)*math.sin(t)) for t in (a,b)]
        # 좁은 줄눈으로 쐐기돌의 방사형 배열을 드러낸다.
        cross=[inner[0],inner[1],outer[1],outer[0]]
        vertices=[(x,y,z) for y in (-depth/2-.15,depth/2+.15) for x,z in cross]
        batch.add('Stone',vertices,BOX[1])
        x0,x1=radius*math.cos(b),radius*math.cos(a)
        z0,z1=spring+rise*math.sin(b),spring+rise*math.sin(a)
        vertices=[(x,y,z) for y in (-depth/2,depth/2) for x,z in ((x0,z0),(x1,z1),(x1,top),(x0,top))]
        batch.add('Stone',vertices,BOX[1])
        if batch.lod<2:
            for sign in (-1,1):
                batch.tube('Recess',[(inner[0][0],sign*(depth/2+.16),inner[0][1]),(outer[0][0],sign*(depth/2+.16),outer[0][1])],.018,6)


def build(kind,lod):
    cfg=CONFIG[kind]
    batch=MeshBatch(kind.title(),lod)
    w,d,h=cfg['width'],cfg['depth'],cfg['storey']
    if kind=='keep':
        stonework(batch,(0,0,-8),w+3,d+3,8)
        # 전면 계단은 기존 경사면과 닫힌 정문의 문턱을 잇는다.
        batch.box('Stone',(0,d/2+.5,.26),(8,2,.5),True)
        for i in range(cfg['entrySteps']):
            batch.box('Stone',(0,d/2+1.65+i*cfg['entryRun'],.36-i*cfg['entryRise']),(8+i*.1,cfg['entryRun']+.04,.3),True)
        base=.25
    elif kind=='gate':
        aperture=cfg['passageWidth']
        pier=(w-aperture)/2
        base=cfg['passageHeight']
        for sign in (-1,1):
            stonework(batch,(sign*(aperture/2+pier/2),0,0),pier,d,base)
            # 열린 문짝은 통로를 침범하지 않도록 안쪽 벽에 붙인다.
            batch.box('Timber',(sign*(aperture/2+.12),0,base*.43),(.22,d*.8,base*.82),True)
            for z in (1,2.6,4.2):
                batch.box('Bronze',(sign*(aperture/2-.02),0,z),(.12,d*.8,.18))
        batch.box('Stone',(0,0,base+.1),(w+.5,d+.5,.5),True)
        archway(batch,cfg)
    else:
        foundation=cfg.get('foundationHeight',5)
        stonework(batch,(0,0,0),w+.6,d+.6,foundation)
        base=foundation+.2
    heights=cfg.get('storeyHeights',[h]*cfg['floors'])
    assert len(heights)==cfg['floors'] and all(value>1.5 for value in heights)
    for floor in range(cfg['floors']):
        fw=w-floor*cfg['shrink']
        fd=max(5,d-floor*cfg['depthShrink'])
        roof_config=dict(cfg.get('roof',CONFIG['roof']))
        upper_floor=None
        wall_height=heights[floor]-.65
        if floor<cfg['floors']-1:
            wall_height=heights[floor]-CONFIG['roof']['interstoreyDrop']-.15
            upper_floor={'width':w-(floor+1)*cfg['shrink'],
                         'depth':max(5,d-(floor+1)*cfg['depthShrink']),
                         'base':base+sum(heights[:floor+1])}
        storey(batch,fw,fd,base+sum(heights[:floor]),wall_height,cfg['facades'][floor],roof_config,upper_floor)
    if kind=='keep':
        # 벽체 안의 마루와 상층 지지 보는 원본 구조를 유지한다.
        batch.box('Timber',(0,0,base+.48),(10,7,.5),True)
        for x in (-5,5):
            batch.tube('Vermilion',[(x,-3.5,base+.5),(x,-3.5,base+h-.65)],.30)
            batch.tube('Vermilion',[(x,3.5,base+.5),(x,3.5,base+h-.65)],.30)
            batch.box('Jade',(x,0,base+h-.9),(.5,8,.5),True)
    # 모든 LOD에서 정면뿐 아니라 측후면과 문 위아래가 실제 메시로 막혔는지 확인한다.
    closed_checks=0
    if any(f.get('enclosed',False) for f in cfg['facades']):
        vertices,faces,_=batch.parts['Plaster']
        walls=BVHTree.FromPolygons(vertices,faces)
        for floor,facade in enumerate(cfg['facades']):
            if not facade.get('enclosed',False):
                continue
            fw,fd=w-floor*cfg['shrink'],max(5,d-floor*cfg['depthShrink'])
            for axis,span,extent in ((0,fd,fw/2),(1,fw,fd/2)):
                for sign in (-1,1):
                    for along in (-.47,-.3,-.15,0,.15,.3,.47):
                        for fraction in (.08,.5,.92):
                            wall_height=heights[floor]-.65 if floor==cfg['floors']-1 else heights[floor]-CONFIG['roof']['interstoreyDrop']-.15
                            origin=[0,0,base+sum(heights[:floor])+wall_height*fraction]
                            origin[1-axis]=along*span
                            direction=[0,0,0]
                            direction[axis]=sign
                            hit=walls.ray_cast(Vector(origin),Vector(direction),extent+.5)
                            assert hit[0] is not None, (kind,lod,floor,origin,direction)
                            closed_checks+=1
    # 상층 바닥의 높이와 그 위 빈 공간을 실제 메시에서 검사해 지붕 관통의 재발을 막는다.
    roof_vertices,roof_faces,_=batch.parts['Roof']
    roof_tree=BVHTree.FromPolygons(roof_vertices,roof_faces)
    floor_vertices,floor_faces,_=batch.parts['Timber']
    floor_tree=BVHTree.FromPolygons(floor_vertices,floor_faces)
    floor_checks=0
    for floor in range(1,cfg['floors']):
        fw,fd=w-floor*cfg['shrink'],max(5,d-floor*cfg['depthShrink'])
        floor_base=base+sum(heights[:floor])
        for fx in (-.46,-.3,-.15,0,.15,.3,.46):
            for fy in (-.46,-.3,-.15,0,.15,.3,.46):
                origin=Vector((fw*fx,fd*fy,floor_base+.4))
                roof_hit=roof_tree.ray_cast(origin,Vector((0,0,1)),heights[floor]-.9)
                assert roof_hit[0] is None, ('상층 지붕 관통',kind,lod,origin,roof_hit[0])
                roof_below=roof_tree.ray_cast(origin,Vector((0,0,-1)),.7)
                assert roof_below[0] is None, ('바닥 지붕 관통',kind,lod,origin)
                floor_hit=floor_tree.ray_cast(origin,Vector((0,0,-1)),.7)
                assert floor_hit[0] is not None and abs(floor_hit[0].z-(floor_base+.26))<.015, ('바닥 단차',kind,lod,origin,floor_hit[0])
                floor_checks+=1
    print(f'UPPER_FLOOR {kind} LOD{lod}: {floor_checks} flat-floor / roof-clearance samples',flush=True)
    objects,stats=batch.finish()
    return objects,stats,closed_checks


if __name__ == '__main__':
    report=[]
    kinds=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else ['keep','gate','watch']
    assert kinds and all(k in ('keep','gate','watch') for k in kinds), kinds
    for kind in kinds:
        previous=None
        for lod in range(3):
            objects,stats,closed_checks=build(kind,lod)
            count=sum(v['triangles'] for v in stats.values())
            if previous is not None:
                assert count<previous,(kind,lod,count,previous)
            previous=count
            bpy.ops.object.select_all(action='DESELECT')
            for obj in objects:
                obj.select_set(True)
            bpy.context.view_layer.objects.active=objects[0]
            path=STAGING/f'{kind.title()}_LOD{lod}.fbx'
            bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_anim=False,use_triangles=True,path_mode='STRIP')
            report.append({'model':kind,'lod':lod,'triangles':count,'enclosureRays':closed_checks,'materials':stats})
            for obj in objects:
                obj.hide_render=lod!=0
                obj.hide_set(lod!=0)
                obj.location.x={'keep':0,'gate':65,'watch':110}[kind]
            print(f'GENERATED {kind} LOD{lod}: {count} triangles',flush=True)
    suffix='' if len(kinds)==3 else '_'+ '_'.join(kinds)
    bpy.data.libraries.write(str(SOURCE/f'StonegateHighPoly{suffix}.blend'),{scene},path_remap='RELATIVE',fake_user=True,compress=True)
    (SOURCE/f'castle_mesh_validation{suffix}.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    # 모든 LOD의 생성·검증이 끝난 뒤에만 Unity가 보는 파일을 교체한다.
    for path in STAGING.glob('*.fbx'):
        shutil.copyfile(path,OUT/path.name)
        path.unlink()
    STAGING.rmdir()
    print('CASTLE_BUILD_COMPLETE',flush=True)
    
