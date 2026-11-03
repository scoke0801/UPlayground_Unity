// 기존 배치를 보존하고 표시된 숲 영역의 추가 조우 위치만 확정한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty || scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("저장된 월드 Edit Mode 필요");
var root=GameObject.Find("탐험 콘텐츠/필드 몬스터 성장 배치");
if(root==null || root.GetComponentsInChildren<Transform>().Any(t=>t.name.StartsWith("forest_")))throw new Exception("기존 필드 배치가 없거나 숲 보강이 이미 적용됐습니다.");
string folder="output/OpenWorld/MonsterPopulation/Infill";System.IO.Directory.CreateDirectory(folder);
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});
Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var input=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/monster-infill-plan.json"),typeof(Dictionary<string,object>));
var original=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/monster-population-resolved.json"),typeof(Dictionary<string,object>));
var bands=((Dictionary<string,object>[])read(original["bands"].ToString(),typeof(Dictionary<string,object>[]))).OrderBy(b=>Convert.ToSingle(b["radius"])).ToArray();
var startData=(Dictionary<string,float>)read(original["start"].ToString(),typeof(Dictionary<string,float>));
var start=new Vector2(startData["x"],startData["z"]);
Func<Vector3,int> levelAt=p=>Convert.ToInt32(bands.First(b=>Vector2.Distance(new Vector2(p.x,p.z),start)<Convert.ToSingle(b["radius"]))["level"]);
var zones=(Dictionary<string,object>[])read(input["zones"].ToString(),typeof(Dictionary<string,object>[]));
var polygons=zones.ToDictionary(z=>(string)z["id"],z=>(float[][])read(z["polygon"].ToString(),typeof(float[][])));
Func<Vector3,float[][],bool> inside=(p,polygon)=>{bool valid=false;for(int i=0,j=polygon.Length-1;i<polygon.Length;j=i++){var a=polygon[i];var b=polygon[j];if((a[1]>p.z)!=(b[1]>p.z) && p.x<(b[0]-a[0])*(p.z-a[1])/(b[1]-a[1])+a[0])valid=!valid;}return valid;};
var regions=(Dictionary<string,float>[])read(original["safeRegions"].ToString(),typeof(Dictionary<string,float>[]));
float padding=Convert.ToSingle(original["safeRegionPadding"]);
Func<Vector3,bool> isInSettlement=p=>regions.Any(b=>p.x>b["x"]-padding && p.x<b["x"]+b["width"]+padding && p.z>b["y"]-padding && p.z<b["y"]+b["height"]+padding);
var terrain=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>()).Single();
var actors=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.GameActor>(true)).ToArray();
var monsters=actors.OfType<UPlayGround.MonsterActor>().ToArray();
var npcs=actors.OfType<UPlayGround.NpcActor>().ToArray();
var identities=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).ToArray();
var interactions=identities.Where(c=>c.GetComponent<UPlayGround.MonsterActor>()==null).ToArray();
var centers=root.GetComponentsInChildren<UPlayGround.Group.MonsterGroupController>(true).Select(g=>new Vector2(g.transform.position.x,g.transform.position.z)).ToList();
var layout=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/city-layout.json"),typeof(Dictionary<string,object>));
var lines=((Dictionary<string,object>[])read(layout["routes"].ToString(),typeof(Dictionary<string,object>[]))).Select(r=>(Vector3[])read(r["points"].ToString(),typeof(Vector3[]))).ToArray();
Func<Vector3,Vector3> nearestRoad=p=>lines.SelectMany(line=>Enumerable.Range(1,line.Length-1).Select(i=>{var a=line[i-1];var b=line[i];a.y=b.y=p.y;return a+(b-a)*Mathf.Clamp01(Vector3.Dot(p-a,b-a)/(b-a).sqrMagnitude);})).OrderBy(v=>(v-p).sqrMagnitude).First();
var definitions=AssetDatabase.FindAssets("t:ActorDefinitionSO").Select(AssetDatabase.GUIDToAssetPath).Select(p=>AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.ActorDefinitionSO>(p)).Where(d=>d.prefab!=null).ToArray();
var validate=typeof(UPlayGround.Editor.P09Builder.P09ProgressionBuilder).GetMethod("ValidateCombat",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
var validated=new HashSet<string>();
var result=new List<Dictionary<string,object>>();var rejected=new List<string>();
var memberPoints=new List<Vector3>();
var surface=GameObject.Find("AuthoredEnvironment").GetComponent<Unity.AI.Navigation.NavMeshSurface>();
var connectionFilter=new UnityEngine.AI.NavMeshQueryFilter{agentTypeID=surface.agentTypeID,areaMask=-1};
connectionFilter.SetAreaCost(UnityEngine.AI.NavMesh.GetAreaFromName("OpenWorldOffRoad"),1f);
var random=new System.Random(27183);
float spacing=Convert.ToSingle(input["minimumSpacing"]);
Physics.SyncTransforms();
foreach(var candidate in (Dictionary<string,object>[])read(input["candidates"].ToString(),typeof(Dictionary<string,object>[]))) {
    var center=new Vector3(Convert.ToSingle(candidate["x"]),0,Convert.ToSingle(candidate["z"]));
    int level=levelAt(center);var polygon=polygons[(string)candidate["zone"]];
    if(isInSettlement(center) || centers.Any(p=>Vector2.Distance(p,new Vector2(center.x,center.z))<spacing)){rejected.Add(candidate["id"]+": 안전 영역/기존 조우 간격");continue;}
    string armor=level==10?"06":level==15?"07":level==20?"08":level==25?"09":level==30?"10":level==40?"11":"12";
    var roster=definitions.Where(d=>d.EffectiveLevel==level && (level<10?d.actorId.StartsWith("Enemy_Random_"):d.actorId.StartsWith("Enemy_P09_Armor"+armor+"_"))).Where(d=>!d.actorId.Contains("Staff")).OrderBy(d=>d.actorId).ToArray();
    if((string)candidate["kind"]=="wildlife") {
        var wildlife=definitions.Where(d=>d.EffectiveLevel==level && d.actorId.StartsWith("LakeWorld_") && (d.actorId.Contains("Plant")||d.actorId.Contains("Ent_")||d.actorId.Contains("Golem_"))).OrderBy(d=>d.actorId).ToArray();
        if(wildlife.Length>0)roster=wildlife;
    }
    if(roster.Length==0)throw new Exception("해당 레벨의 검증 가능한 몬스터 없음: "+level);
    var members=new List<Dictionary<string,object>>();
    int count=level<20?3:2;
    var reasons=new Dictionary<string,int>();
    Action<string> reject=reason=>{if(!reasons.ContainsKey(reason))reasons[reason]=0;reasons[reason]++;};
    for(int index=0;index<count;index++) {
        var definition=roster[(result.Count+index*2)%roster.Length];
        if(validated.Add(definition.actorId))validate.Invoke(null,new object[]{definition});
        var capsule=definition.prefab.GetComponentInChildren<KinematicCharacterController.KinematicCharacterMotor>(true).Capsule;
        float radius=capsule.radius,height=capsule.height;bool found=false;
        for(int attempt=0;attempt<97;attempt++) {
            float angle=(index*120+attempt%12*30)*Mathf.Deg2Rad;
            float distance=6+(attempt/12)*3;
            var p=center+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*distance;
            if(!inside(p,polygon)||isInSettlement(p)||levelAt(p)!=level){reject("영역/레벨");continue;}
            p.y=terrain.SampleHeight(p)+terrain.transform.position.y;
            if(terrain.terrainData.GetSteepness((p.x-terrain.transform.position.x)/terrain.terrainData.size.x,(p.z-terrain.transform.position.z)/terrain.terrainData.size.z)>Convert.ToSingle(input["maxSlope"])){reject("경사");continue;}
            UnityEngine.AI.NavMeshHit hit,edge;
            if(!UnityEngine.AI.NavMesh.SamplePosition(p,out hit,2,-1)||Mathf.Abs(hit.position.y-p.y)>.8f){reject("NavMesh");continue;}
            p=hit.position;
            if(!inside(p,polygon)||isInSettlement(p)||levelAt(p)!=level){reject("영역/레벨");continue;}
            if(!UnityEngine.AI.NavMesh.FindClosestEdge(p,out edge,-1)||edge.distance<radius+.5f){reject("발판 폭");continue;}
            if(npcs.Any(a=>Vector3.Distance(a.transform.position,p)<Convert.ToSingle(original["safeNpcRadius"])) || interactions.Any(a=>Vector3.Distance(a.transform.position,p)<Convert.ToSingle(original["safeInteractionRadius"])) || monsters.Any(a=>Vector3.Distance(a.transform.position,p)<Convert.ToSingle(input["minimumMonsterDistance"]))){reject("기존 개체 안전 거리");continue;}
            if(memberPoints.Any(v=>Vector3.Distance(v,p)<spacing*.65f)||members.Any(m=>Vector3.Distance(new Vector3(Convert.ToSingle(m["x"]),Convert.ToSingle(m["y"]),Convert.ToSingle(m["z"])),p)<radius*2+3)){reject("신규 조우/멤버 간격");continue;}
            if(Physics.OverlapCapsule(p+Vector3.up*(radius+.15f),p+Vector3.up*Mathf.Max(radius+.15f,height-radius),radius+.15f,-1,QueryTriggerInteraction.Ignore).Any(c=>!(c is TerrainCollider)&&c.GetComponentInParent<UPlayGround.GameActor>()==null)){reject("환경 충돌");continue;}
            var road=nearestRoad(p);
            if(Vector3.Distance(road,p)<Convert.ToSingle(input["minimumRoadDistance"])+radius){reject("본길 여유 폭");continue;}
            road.y=terrain.SampleHeight(road)+terrain.transform.position.y;
            UnityEngine.AI.NavMeshHit roadHit;var path=new UnityEngine.AI.NavMeshPath();
            // 길 밖 비용 6의 장거리 조회는 탐색 예산으로 Partial이 될 수 있다. 지형 연결과 실제 교전 거리 검사는 분리한다.
            if(!UnityEngine.AI.NavMesh.SamplePosition(road,out roadHit,5,-1)||!UnityEngine.AI.NavMesh.CalculatePath(roadHit.position,p,connectionFilter,path)||path.status!=UnityEngine.AI.NavMeshPathStatus.PathComplete || !UnityEngine.AI.NavMesh.CalculatePath(p,roadHit.position,connectionFilter,path)||path.status!=UnityEngine.AI.NavMeshPathStatus.PathComplete){reject("길 지형 연결");continue;}
            int localPaths=0;
            for(int direction=0;direction<8;direction++){
                float heading=direction*Mathf.PI/4;var local=p+new Vector3(Mathf.Cos(heading),0,Mathf.Sin(heading))*18;
                local.y=terrain.SampleHeight(local)+terrain.transform.position.y;UnityEngine.AI.NavMeshHit localHit;
                if(UnityEngine.AI.NavMesh.SamplePosition(local,out localHit,2,-1) && UnityEngine.AI.NavMesh.CalculatePath(p,localHit.position,-1,path) && path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete && UnityEngine.AI.NavMesh.CalculatePath(localHit.position,p,-1,path) && path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete)localPaths++;
            }
            if(localPaths<3){reject("지역 교전 공간");continue;}
            members.Add(new Dictionary<string,object>{{"source",definition.actorId},{"level",level},{"x",p.x},{"y",p.y+.06f},{"z",p.z},{"yaw",random.Next(360)},{"guid",Guid.NewGuid().ToString("N")}});found=true;break;
        }
        if(!found)break;
    }
    if(members.Count!=count){rejected.Add(candidate["id"]+": "+string.Join(",",reasons.Select(k=>k.Key+"="+k.Value).ToArray()));continue;}
    result.Add(new Dictionary<string,object>{{"id",candidate["id"]},{"route",candidate["route"]},{"zone",candidate["zone"]},{"level",level},{"members",members}});
    centers.Add(new Vector2(center.x,center.z));memberPoints.AddRange(members.Select(m=>new Vector3(Convert.ToSingle(m["x"]),Convert.ToSingle(m["y"]),Convert.ToSingle(m["z"]))));
}
input["groups"]=result;
var serialize=json.GetMethod("SerializeObject",new[]{typeof(object)});
System.IO.File.WriteAllText("Tools/OpenWorld/monster-infill-resolved.json",(string)serialize.Invoke(null,new object[]{input}));
System.IO.File.WriteAllLines(folder+"/rejected.txt",rejected);
System.IO.File.WriteAllLines(folder+"/identities-before.txt",identities.Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")).OrderBy(s=>s));
return new {groups=result.Count,actors=memberPoints.Count,zones=zones.Select(z=>z["name"]+": "+result.Count(g=>(string)g["zone"]==(string)z["id"])).ToArray(),levels=result.GroupBy(g=>g["level"]).Select(g=>g.Key+":"+g.Count()).ToArray(),rejected=rejected.Count};
