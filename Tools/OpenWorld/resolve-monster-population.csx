// 저장된 월드에서 이동 가능한 위치만 확정한다. 씬과 에셋은 수정하지 않는다.
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (GameObject.Find("탐험 콘텐츠/필드 몬스터 성장 배치")!=null || System.IO.File.Exists("output/OpenWorld/MonsterPopulation/application.txt")) throw new Exception("적용된 배치가 있습니다. 현재 배치 검사는 audit-monster-population.csx를 사용하세요.");
if (EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty || scene.path != "Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity") throw new Exception("저장된 월드 Edit Mode 필요");
var json = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Newtonsoft.Json.JsonConvert")).First(t => t != null);
var parse = json.GetMethod("DeserializeObject", new[] { typeof(string), typeof(Type) });
Func<string, Type, object> read = (s,t) => parse.Invoke(null,new object[]{s,t});
var plan = (Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/monster-population-plan.json"),typeof(Dictionary<string,object>));
var bands = ((Dictionary<string,object>[])read(plan["bands"].ToString(),typeof(Dictionary<string,object>[]))).OrderBy(b=>Convert.ToSingle(b["radius"])).ToArray();
var startData = (Dictionary<string,float>)read(plan["start"].ToString(),typeof(Dictionary<string,float>));
var start = new Vector3(startData["x"],0,startData["z"]);
Func<Vector3,int> levelAt = p => Convert.ToInt32(bands.First(b=>Vector3.Distance(new Vector3(p.x,0,p.z),start)<Convert.ToSingle(b["radius"]))["level"]);
var terrain = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>()).Single();
var actors = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.GameActor>(true)).ToArray();
var monsters = actors.OfType<UPlayGround.MonsterActor>().ToArray();
var interactions = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Where(c=>c.GetComponent<UPlayGround.MonsterActor>()==null).ToArray();
var definitions = AssetDatabase.FindAssets("t:ActorDefinitionSO").Select(AssetDatabase.GUIDToAssetPath).Select(p=>AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.ActorDefinitionSO>(p)).Where(d=>d.actorType.HasFlag(UPlayGround.Data.EnumType.ActorType.Monster)).ToDictionary(d=>d.actorId);
var groups = new List<Dictionary<string,object>>();
var rejected = new List<string>();
var centers = new List<Vector3>();
var memberPoints = new List<Vector3>();
var safeRegions=(Dictionary<string,float>[])read(plan["safeRegions"].ToString(),typeof(Dictionary<string,float>[]));
float safePadding=Convert.ToSingle(plan["safeRegionPadding"]);
Func<Vector3,bool> isInSettlement=p=>safeRegions.Any(b=>p.x>b["x"]-safePadding && p.x<b["x"]+b["width"]+safePadding && p.z>b["y"]-safePadding && p.z<b["y"]+b["height"]+safePadding);
var layout = (Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/city-layout.json"),typeof(Dictionary<string,object>));
var roadLines = ((Dictionary<string,object>[])read(layout["routes"].ToString(),typeof(Dictionary<string,object>[]))).Select(r=>(Vector3[])read(r["points"].ToString(),typeof(Vector3[]))).ToArray();
Func<Vector3,Vector3> nearestRoad = p => roadLines.SelectMany(line=>Enumerable.Range(1,line.Length-1).Select(i=>{var a=line[i-1];var b=line[i];a.y=b.y=p.y;return a+(b-a)*Mathf.Clamp01(Vector3.Dot(p-a,b-a)/(b-a).sqrMagnitude);})).OrderBy(v=>(v-p).sqrMagnitude).First();
Physics.SyncTransforms();
foreach (var candidate in (Dictionary<string,object>[])read(plan["candidates"].ToString(),typeof(Dictionary<string,object>[]))) {
    var center = new Vector3(Convert.ToSingle(candidate["x"]),0,Convert.ToSingle(candidate["z"]));
    int level = levelAt(center);
    if (Vector3.Distance(center,start)<120 || centers.Any(p=>Vector3.Distance(p,center)<Convert.ToSingle(plan["minGroupSpacing"]))) { rejected.Add(candidate["id"]+": 간격"); continue; }
    // 도시 생활권을 순찰 적의 전투 범위로 만들지 않는다.
    if (isInSettlement(center)) { rejected.Add(candidate["id"]+": 도시"); continue; }
    bool wildlife = (groups.Count%3 == 0 || level==1) && !candidate.ContainsKey("kind");
    string[] sources;
    if (wildlife) {
        string source = level<=3 ? "RootPlant_1" : level<=10 ? "Plant_1" : level<=20 ? "Ent_Normal" : level<=30 ? "Golem_Normal" : "Lich_Elite";
        sources = Enumerable.Range(0,level>=20?2:3).Select(i=>source=="Plant_1"?"Plant_"+(i+1):source=="RootPlant_1"?"RootPlant_"+(i+1):source).ToArray();
    } else if(level<10) {
        string suffix=level==3?"001":"002";
        sources = level==3 ? new[]{"Enemy_Random_M_DualAxe_001","Enemy_Random_M_SwordShield_002","Enemy_Random_F_Bow_001"} : new[]{"Enemy_Random_M_DualSword_003","Enemy_Random_M_GreatSword_002","Enemy_Random_F_Bow_002"};
    } else {
        int armor=level==10?6:level==15?7:level==20?8:level==25?9:level==30?10:level==40?11:12;
        sources = level>=20 ? new[]{"Enemy_P09_Armor"+armor.ToString("00")+"_SwordShield","Enemy_P09_Armor"+armor.ToString("00")+(groups.Count%2==0?"_Bow":"_GreatSword")} : new[]{"Enemy_P09_Armor"+armor.ToString("00")+"_DualSword","Enemy_P09_Armor"+armor.ToString("00")+"_SwordShield","Enemy_P09_Armor"+armor.ToString("00")+"_Bow"};
    }
    var members = new List<Dictionary<string,object>>();
    for(int index=0;index<sources.Length;index++) {
        var definition=definitions[sources[index]];
        var motor=definition.prefab.GetComponentInChildren<KinematicCharacterController.KinematicCharacterMotor>(true);
        float radius=motor!=null?motor.Capsule.radius:0.6f;
        float height=motor!=null?motor.Capsule.height:2f;
        bool found=false;
        for(int attempt=0;attempt<49;attempt++) {
            float angle=(index*120+(attempt%8)*45)*Mathf.Deg2Rad;
            float distance=attempt==0?6f:6f+(attempt/8)*2.5f;
            var p=center+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*distance;
            p.y=terrain.SampleHeight(p)+terrain.transform.position.y;
            if(terrain.terrainData.GetSteepness((p.x-terrain.transform.position.x)/terrain.terrainData.size.x,(p.z-terrain.transform.position.z)/terrain.terrainData.size.z)>Convert.ToSingle(plan["maxGroundSlope"]))continue;
            UnityEngine.AI.NavMeshHit hit;
            if(!UnityEngine.AI.NavMesh.SamplePosition(p,out hit,2,-1) || Mathf.Abs(hit.position.y-p.y)>0.8f)continue;
            p=hit.position;
            if(isInSettlement(p))continue;
            if(levelAt(p)!=level)continue;
            var road=nearestRoad(p);
            if(Vector3.Distance(road,p)<8+radius)continue;
            road.y=terrain.SampleHeight(road)+terrain.transform.position.y;
            UnityEngine.AI.NavMeshHit roadHit;
            var approach=new UnityEngine.AI.NavMeshPath();
            if(!UnityEngine.AI.NavMesh.SamplePosition(road,out roadHit,4,-1)||!UnityEngine.AI.NavMesh.CalculatePath(roadHit.position,p,-1,approach)||approach.status!=UnityEngine.AI.NavMeshPathStatus.PathComplete)continue;
            if(actors.OfType<UPlayGround.NpcActor>().Any(a=>Vector3.Distance(a.transform.position,p)<Convert.ToSingle(plan["safeNpcRadius"])) || monsters.Any(a=>Vector3.Distance(a.transform.position,p)<Convert.ToSingle(plan["safeMonsterRadius"])) || interactions.Any(a=>Vector3.Distance(a.transform.position,p)<Convert.ToSingle(plan["safeInteractionRadius"])))continue;
            if(memberPoints.Any(a=>Vector3.Distance(a,p)<radius*2+2) || members.Any(a=>Vector3.Distance(new Vector3(Convert.ToSingle(a["x"]),Convert.ToSingle(a["y"]),Convert.ToSingle(a["z"])),p)<radius*2+3))continue;
            bool blocked=Physics.OverlapCapsule(p+Vector3.up*(radius+0.15f),p+Vector3.up*Mathf.Max(radius+0.15f,height-radius),radius+0.15f,-1,QueryTriggerInteraction.Ignore).Any(c=>!(c is TerrainCollider)&&c.GetComponentInParent<UPlayGround.GameActor>()==null);
            if(blocked)continue;
            UnityEngine.AI.NavMeshHit edge;
            if(!UnityEngine.AI.NavMesh.FindClosestEdge(p,out edge,-1)||edge.distance<radius+0.5f)continue;
            var path=new UnityEngine.AI.NavMeshPath();
            var first=members.Count==0?p:new Vector3(Convert.ToSingle(members[0]["x"]),Convert.ToSingle(members[0]["y"]),Convert.ToSingle(members[0]["z"]));
            if(!UnityEngine.AI.NavMesh.CalculatePath(first,p,-1,path)||path.status!=UnityEngine.AI.NavMeshPathStatus.PathComplete)continue;
            members.Add(new Dictionary<string,object>{{"source",definition.actorId},{"level",level},{"x",p.x},{"y",p.y+0.06f},{"z",p.z},{"yaw",Mathf.Atan2(start.x-p.x,start.z-p.z)*Mathf.Rad2Deg},{"guid",Guid.NewGuid().ToString("N")}});
            found=true;break;
        }
        if(!found)break;
    }
    if(members.Count!=sources.Length){rejected.Add(candidate["id"]+": 지면/안전 거리/충돌");continue;}
    groups.Add(new Dictionary<string,object>{{"id",candidate["id"]},{"route",candidate["route"]},{"level",level},{"members",members}});
    centers.Add(center);
    memberPoints.AddRange(members.Select(m=>new Vector3(Convert.ToSingle(m["x"]),Convert.ToSingle(m["y"]),Convert.ToSingle(m["z"]))));
}
plan["groups"]=groups;
var overrides=new List<Dictionary<string,object>>();
foreach(var actor in monsters.Where(a=>AnimationUtility.CalculateTransformPath(a.transform,null).Contains("WorldExpansionActors"))) {
    var source=actor.Definition;
    if(source==null || source.prefab.GetComponent<UPlayGround.Components.EnemyAIController>()==null || source.EffectiveGrade==UPlayGround.Data.EnumType.MonsterActorGrade.Boss)continue;
    int level=levelAt(actor.transform.position);
    if(level==source.EffectiveLevel && actor.Definition!=null)continue;
    overrides.Add(new Dictionary<string,object>{{"path",AnimationUtility.CalculateTransformPath(actor.transform,null)},{"source",source.actorId},{"level",level}});
}
plan["existingOverrides"]=overrides;
var serialize=json.GetMethod("SerializeObject",new[]{typeof(object)});
System.IO.File.WriteAllText("Tools/OpenWorld/monster-population-resolved.json",(string)serialize.Invoke(null,new object[]{plan}));
System.IO.File.WriteAllLines("output/OpenWorld/MonsterPopulation/rejected.txt",rejected);
return new {groups=groups.Count,members=memberPoints.Count,levels=groups.GroupBy(g=>g["level"]).Select(g=>g.Key+":"+g.Count()).ToArray(),rejected=rejected.Count};


