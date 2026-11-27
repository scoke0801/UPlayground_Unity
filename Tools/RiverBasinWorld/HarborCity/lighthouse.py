"""석조 등대 본체와 구리 등실을 Blender MCP에서 제작한다."""

import math
import random
import runpy

import bpy


def export(workspace):
    tools = workspace / "Tools/RiverBasinWorld/HarborCity"
    Kit = runpy.run_path(str(tools / "harbor_architecture.py"))["Kit"]
    world = workspace / ("Assets/WorldBuilder/Worlds/"
                         "6effd90b68ea4b3d8f8caa5197fcfe34/Authoring/HarborCity")
    colors = {
        "TowerStone": (.70,.65,.51), "CourseStone": (.60,.55,.43),
        "Recess": (.38,.33,.25), "Cornice": (.77,.72,.58),
        "BeaconMetal": (.085,.16,.17), "CopperRoof": (.13,.30,.28),
        "CopperSeam": (.24,.37,.32), "GoldenLens": (.79,.48,.17),
        "IronDoor": (.28,.19,.11), "FlintGlass": (.21,.30,.32),
    }
    mats = {}
    for key, c in colors.items():
        name = "RBL_" + key
        m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
        m.diffuse_color = (*c,1)
        m.use_nodes = True
        m.node_tree.nodes.get("Principled BSDF").inputs["Base Color"].default_value = (*c,1)
        mats[key] = m
    collection = bpy.data.collections.new("River Basin Lighthouse")
    bpy.context.scene.collection.children.link(collection)

    def build(lod):
        detailed = lod == 0
        k = Kit(bpy.context.scene, mats, 154)
        rng = random.Random(106)
        circle = math.tau

        def pt(r,t,z):
            return (r*math.cos(t),r*math.sin(t),z)

        def profile(mat, rz, segments):
            verts = [pt(r,circle*i/segments,z) for r,z in rz for i in range(segments)]
            faces = []
            for j in range(len(rz)-1):
                for i in range(segments):
                    a = j*segments+i
                    b = j*segments+(i+1)%segments
                    faces.append((a,b,b+segments,a+segments))
            k.add(mat,verts,faces)

        def ring(mat,r,y,h,width=.15,segments=64):
            profile(mat,[(r-width,y),(r-.035,y),(r,y+.035),(r,y+h-.035),
                         (r-.035,y+h),(r-width,y+h)],segments)

        def block_sector(mat, a,b,ys,ye,rb,rt):
            n=3
            points=[]
            for r,z,trim in ((rb-.026,ys,.009),(rb,ys+.027,0),
                             (rt,ye-.025,0),(rt-.025,ye,.009)):
                points.extend(pt(r,a+trim+(b-a-2*trim)*i/n,z) for i in range(n+1))
            faces=[]
            for j in range(3):
                for i in range(n):
                    q=4*j+i
                    faces.append((q,q+1,q+5,q+4))
            k.add(mat,points,faces)

        def shaft(z):
            return 1.64-(z-.54)*.045

        segments=96 if detailed else 40
        ring("Cornice",1.93,.025,.20,.18,segments)
        ring("CourseStone",1.80,.20,.21,.16,segments)
        ring("Cornice",1.76,.41,.15,.15,segments)
        profile("Recess" if detailed else "TowerStone",
                [(shaft(.54)-.018,.54),(shaft(8.88)-.018,8.88)],segments)
        if detailed:
            for course in range(23):
                za=.55+course*.355
                zb=za+.343
                n=31 if course<13 else 28
                for i in range(n):
                    a=(i+(course%2)*.5)*circle/n
                    b=a+circle/n
                    delta=rng.uniform(-.008,.016)
                    material="CourseStone" if rng.random()<.16 else "TowerStone"
                    block_sector(material,a+.010,b-.010,za,zb,shaft(za)+delta,shaft(zb)+delta)
        for y in (2.36,5.15,7.78):
            ring("Cornice",shaft(y)+.046,y,.14,.13,segments)
        ring("CourseStone",1.43,8.64,.19,.26,segments)
        ring("Cornice",1.56,8.83,.14,.34,segments)
        ring("Cornice",1.84,8.97,.28,.53,segments)
        profile("CourseStone",[(1.34,8.72),(1.76,8.96)],segments)
        if detailed:
            for j in range(14):
                t=j*circle/14
                k.beam("Cornice",pt(1.34,t,8.58),pt(1.74,t,9.04),.16,.21)
        for j in range(16 if detailed else 12):
            count=16 if detailed else 12
            t=j*circle/count
            t2=(j+1)*circle/count
            k.rod("BeaconMetal",pt(1.73,t,9.22),pt(1.73,t,10.08),.041,10)
            for y in (9.56,10.04):
                k.rod("BeaconMetal",pt(1.73,t,y),pt(1.73,t2,y),.024,8)
            if detailed:
                tm=(t+t2)*.5
                k.rod("BeaconMetal",pt(1.73,tm,9.28),pt(1.73,tm,9.99),.019,7)

        ring("BeaconMetal",1.37,9.22,.22,.17,segments)
        ring("CopperSeam",1.37,11.3,.18,.18,segments)
        pane_count=16 if detailed else 12
        for j in range(pane_count):
            t=circle*j/pane_count
            t2=circle*(j+1)/pane_count
            k.rod("BeaconMetal",pt(1.27,t,9.41),pt(1.27,t,11.32),.059,12)
            if j%4==1:
                continue
            k.add("FlintGlass",[pt(1.25,t,9.53),pt(1.25,t2,9.53),
                                pt(1.25,t2,11.21),pt(1.25,t,11.21)],[(0,1,2,3)])
            for y in (9.56,10.29,11.18):
                k.rod("BeaconMetal",pt(1.26,t,y),pt(1.26,t2,y),.028,8)
        k.rod("BeaconMetal",(0,0,9.37),(0,0,10.11),.22,20)
        profile("GoldenLens",[(.20,10.05),(.34,10.16),(.40,10.43),
                             (.35,10.68),(.20,10.75)],32)
        for i in range(5 if detailed else 2):
            ring("CopperSeam",.365,10.16+i*(.11 if detailed else .4),.07,.062,36)

        roof=[(1.54,11.40),(1.58,11.46),(1.56,11.57),(1.28,11.83),
              (.97,12.09),(.61,12.40),(.30,12.73),(.13,12.96),(.09,13.03)]
        profile("CopperRoof",roof,segments)
        for j in range(20 if detailed else 12):
            t=j*circle/(20 if detailed else 12)
            for (r,z),(rn,zn) in zip(roof[2:],roof[3:]):
                k.rod("CopperSeam",pt(r+.015,t,z+.016),pt(rn+.015,t,zn+.016),
                      .026 if detailed else .021,7)
        k.rod("BeaconMetal",(0,0,12.95),(0,0,13.46),.075,16,taper=.48)
        k.rod("CopperSeam",(0,0,13.20),(0,0,13.34),.126,20,taper=.9)
        k.rod("BeaconMetal",(-.46,0,13.37),(.46,0,13.37),.022,8)

        # 바다 쪽 흰 석벽과 대비되는 선실 창, 육지 쪽 목제 출입문.
        for j,z in enumerate((3.16,5.68,7.18)):
            t=math.pi*(.17+j*.56)
            r=shaft(z)+.033
            for y,l in ((z-.40,.48),(z+.37,.37)):
                k.beam("Cornice",pt(r,t-l/r,y),pt(r,t+l/r,y),.13,.18)
            for side in (-1,1):
                u=t+side*.29/r
                k.beam("CourseStone",pt(r,u,z-.36),pt(r-.028,u,z+.36),.128)
            k.add("FlintGlass",[pt(r+.020,t-.22/r,z-.35),pt(r+.020,t+.22/r,z-.35),
                                pt(r-.020,t+.22/r,z+.34),pt(r-.020,t-.22/r,z+.34)],[(0,1,2,3)])
            k.rod("BeaconMetal",pt(r+.028,t,z-.31),pt(r-.010,t,z+.30),.028,8)
        door_y=-1.58
        for i in range(7):
            x=(i-3)*.136
            k.box("IronDoor",(x,door_y,.91),(.132,.13,1.53),.018)
        for x in (-.57,.57):
            for i in range(5):
                k.box("Cornice",(x,door_y-.008,.27+.27*i),(.21,.235,.255),.029)
        for j in range(13 if detailed else 8):
            n=13 if detailed else 8
            a=math.pi*j/n+.006
            b=math.pi*(j+1)/n-.006
            pv=[(r*math.cos(t),y,1.61+r*math.sin(t)) for y in (door_y-.162,door_y+.033)
                for t,r in [(a,.47),(b,.47),(b,.67),(a,.67)]]
            k.add("Cornice",pv,[(0,1,2,3),(4,7,6,5),(0,4,5,1),(1,5,6,2),
                               (2,6,7,3),(3,7,4,0)])
        for z in (.52,1.24):
            k.box("BeaconMetal",(0,door_y-.081,z),(.92,.055,.092),.012)
        k.rod("CopperSeam",(.30,door_y-.083,.92),(.30,door_y-.143,.92),.052,10)
        k.box("CourseStone",(0,door_y-.12,.105),(1.28,.61,.21),.032)
        return k.make("RBL_Lighthouse_LOD"+str(lod),collection)

    pairs=[build(i) for i in range(2)]
    bpy.ops.object.select_all(action="DESELECT")
    for obj,n in pairs:
        obj.select_set(True)
    bpy.context.view_layer.objects.active=pairs[0][0]
    bpy.ops.export_scene.fbx(filepath=str(world/"RBL_Lighthouse.fbx"),use_selection=True,
                             axis_forward="-Z",axis_up="Y",apply_unit_scale=True,
                             apply_scale_options="FBX_SCALE_UNITS",bake_space_transform=True,
                             bake_anim=False,add_leaf_bones=False)
    pairs[1][0].hide_set(True)
    bpy.ops.wm.save_as_mainfile(filepath=str(tools/"RiverBasinLighthouse.blend"),copy=True)
    return {obj.name:n for obj,n in pairs}
