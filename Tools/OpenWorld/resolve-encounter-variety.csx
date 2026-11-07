// 입문 구간 조우 보강과 레벨 구간별 4인 거점 조우의 위치만 확정한다. 씬과 에셋은 수정하지 않는다.
// 안전 기준(NPC·상호작용·몬스터 거리, 경사, 도시 경계, 길·NavMesh 연결)은 resolve-monster-population.csx와 같다.
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty || scene.path != "Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity") throw new Exception("저장된 월드 Edit Mode 필요");
if (System.IO.File.Exists("output/OpenWorld/MonsterPopulation/Improvement/application.txt")) throw new Exception("이미 적용됨. 재실행 금지");
var json = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Newtonsoft.Json.JsonConvert")).First(t => t != null);
var parse = json.GetMethod("DeserializeObject", new[] { typeof(string), typeof(Type) });
Func<string, Type, object> read = (s,t) => parse.Invoke(null,new object[]{s,t});
var plan = (Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/monster-population-resolved.json"),typeof(Dictionary<string,object>));
var bands = ((Dictionary<string,object>[])read(plan["bands"].ToString(),typeof(Dictionary<string,object>[]))).OrderBy(b=>Convert.ToSingle(b["radius"])).ToArray();
var startData = (Dictionary<string,float>)read(plan["start"].ToString(),typeof(Dictionary<string,float>));
var start = new Vector3(startData["x"],0,startData["z"]);
Func<Vector3,int> levelAt = p => Convert.ToInt32(bands.First(b=>Vector3.Distance(new Vector3(p.x,0,p.z),start)<Convert.ToSingle(b["radius"]))["level"]);
float npcRadius=Convert.ToSingle(plan["safeNpcRadius"]), monsterRadius=Convert.ToSingle(plan["safeMonsterRadius"]), interactionRadius=Convert.ToSingle(plan["safeInteractionRadius"]), groupSpacing=Convert.ToSingle(plan["minGroupSpacing"]), maxSlope=Convert.ToSingle(plan["maxGroundSlope"]);
var terrain = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>()).Single();
var actors = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.GameActor>(true)).ToArray();
var monsters = actors.OfType<UPlayGround.MonsterActor>().ToArray();
var npcs = actors.OfType<UPlayGround.NpcActor>().ToArray();
var interactions = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Where(c=>c.GetComponent<UPlayGround.MonsterActor>()==null).ToArray();
var groupControllers = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Group.MonsterGroupController>(true)).ToArray();
var database = AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.ActorDatabase>("Assets/10.Datas/Actor/DataBase/ActorDatabase.asset");
var safeRegions=(Dictionary<string,float>[])read(plan["safeRegions"].ToString(),typeof(Dictionary<string,float>[]));
float safePadding=Convert.ToSingle(plan["safeRegionPadding"]);
Func<Vector3,bool> isInSettlement=p=>safeRegions.Any(b=>p.x>b["x"]-safePadding && p.x<b["x"]+b["width"]+safePadding && p.z>b["y"]-safePadding && p.z<b["y"]+b["height"]+safePadding);
var layout = (Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/city-layout.json"),typeof(Dictionary<string,object>));
var roadLines = ((Dictionary<string,object>[])read(layout["routes"].ToString(),typeof(Dictionary<string,object>[]))).Select(r=>(Vector3[])read(r["points"].ToString(),typeof(Vector3[]))).ToArray();
Func<Vector3,Vector3> nearestRoad = p => roadLines.SelectMany(line=>Enumerable.Range(1,line.Length-1).Select(i=>{var a=line[i-1];var b=line[i];a.y=b.y=p.y;return a+(b-a)*Mathf.Clamp01(Vector3.Dot(p-a,b-a)/(b-a).sqrMagnitude);})).OrderBy(v=>(v-p).sqrMagnitude).First();
Physics.SyncTransforms();
var placedPoints = new List<Vector3>();
var rejected = new List<string>();

// 멤버 한 명의 위치를 center 주변에서 찾는다. ownGroup에 속한 기존 몬스터는 거리 검사에서 제외한다.
Func<UPlayGround.Data.Actor.ActorDefinitionSO,Vector3,int,int,List<Vector3>,HashSet<UPlayGround.MonsterActor>,Vector3?> findSpot = (definition,center,level,slot,groupPoints,ownGroup) => {
    var motor=definition.prefab.GetComponentInChildren<KinematicCharacterController.KinematicCharacterMotor>(true);
    float radius=motor!=null?motor.Capsule.radius:0.6f;
    float height=motor!=null?motor.Capsule.height:2f;
    for(int attempt=0;attempt<64;attempt++) {
        float angle=(slot*97+(attempt%8)*45)*Mathf.Deg2Rad;
        float distance=5f+(attempt/8)*1.5f;
        var p=center+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*distance;
        p.y=terrain.SampleHeight(p)+terrain.transform.position.y;
        if(terrain.terrainData.GetSteepness((p.x-terrain.transform.position.x)/terrain.terrainData.size.x,(p.z-terrain.transform.position.z)/terrain.terrainData.size.z)>maxSlope)continue;
        UnityEngine.AI.NavMeshHit hit;
        if(!UnityEngine.AI.NavMesh.SamplePosition(p,out hit,2,-1) || Mathf.Abs(hit.position.y-p.y)>0.8f)continue;
        p=hit.position;
        if(isInSettlement(p) || levelAt(p)!=level)continue;
        var road=nearestRoad(p);
        if(Vector3.Distance(road,p)<8+radius)continue;
        road.y=terrain.SampleHeight(road)+terrain.transform.position.y;
        UnityEngine.AI.NavMeshHit roadHit;
        var approach=new UnityEngine.AI.NavMeshPath();
        if(!UnityEngine.AI.NavMesh.SamplePosition(road,out roadHit,4,-1)||!UnityEngine.AI.NavMesh.CalculatePath(roadHit.position,p,-1,approach)||approach.status!=UnityEngine.AI.NavMeshPathStatus.PathComplete)continue;
        if(npcs.Any(a=>Vector3.Distance(a.transform.position,p)<npcRadius))continue;
        if(interactions.Any(a=>Vector3.Distance(a.transform.position,p)<interactionRadius))continue;
        if(monsters.Any(a=>!ownGroup.Contains(a) && Vector3.Distance(a.transform.position,p)<monsterRadius))continue;
        if(monsters.Any(a=>ownGroup.Contains(a) && Vector3.Distance(a.transform.position,p)<radius*2+2.5f))continue;
        if(placedPoints.Any(a=>Vector3.Distance(a,p)<monsterRadius) && groupPoints.Count==0)continue;
        if(groupPoints.Any(a=>Vector3.Distance(a,p)<radius*2+2.5f))continue;
        bool blocked=Physics.OverlapCapsule(p+Vector3.up*(radius+0.15f),p+Vector3.up*Mathf.Max(radius+0.15f,height-radius),radius+0.15f,-1,QueryTriggerInteraction.Ignore).Any(c=>!(c is TerrainCollider)&&c.GetComponentInParent<UPlayGround.GameActor>()==null);
        if(blocked)continue;
        UnityEngine.AI.NavMeshHit edge;
        if(!UnityEngine.AI.NavMesh.FindClosestEdge(p,out edge,-1)||edge.distance<radius+0.5f)continue;
        if(groupPoints.Count>0){var path=new UnityEngine.AI.NavMeshPath();if(!UnityEngine.AI.NavMesh.CalculatePath(groupPoints[0],p,-1,path)||path.status!=UnityEngine.AI.NavMeshPathStatus.PathComplete)continue;}
        return p;
    }
    return null;
};
Func<string,UPlayGround.Data.Actor.ActorDefinitionSO> def = id => { var d=database.GetDefinition(id); if(d==null||d.prefab==null)throw new Exception("정의 없음: "+id); return d; };
Func<UPlayGround.Data.Actor.ActorDefinitionSO,Vector3,int,Dictionary<string,object>> member = (d,p,level) => new Dictionary<string,object>{{"source",d.actorId},{"level",level},{"x",p.x},{"y",p.y+0.06f},{"z",p.z},{"yaw",Mathf.Atan2(start.x-p.x,start.z-p.z)*Mathf.Rad2Deg},{"guid",Guid.NewGuid().ToString("N")}};

// A. 입문 구간: 학습 순서를 만드는 템플릿. 1인 결투 → 2인 → 혼성 → 3인 방어진.
var starterTemplates = new Dictionary<int,string[][]>{
    {1, new[]{ new[]{"RootPlant_1","RootPlant_2"} }},
    {3, new[]{ new[]{"Enemy_Random_M_SwordShield_002"}, new[]{"Enemy_Random_M_DualAxe_001","Enemy_Random_M_SwordShield_002"}, new[]{"Enemy_Random_M_DualAxe_001","Enemy_Random_F_Bow_001"}, new[]{"LakeWorld_RootPlant_1_Lv03","LakeWorld_RootPlant_3_Lv03"} }},
    {5, new[]{ new[]{"Enemy_Random_M_DualSword_003","Enemy_Random_F_Bow_002"}, new[]{"LakeWorld_Plant_2_Lv05","LakeWorld_RootPlant_2_Lv05"}, new[]{"Enemy_Random_M_SwordShield_003","Enemy_Random_F_Bow_002","Enemy_Random_F_DualAxe_002"}, new[]{"Enemy_Random_F_DualAxe_002"} }}
};
var existingCenters = groupControllers.Select(g=>g.transform.position).ToList();
var candidates = new List<Vector3>();
for(float r=130;r<=410;r+=20)for(float a=0;a<360;a+=9){var p=start+new Vector3(Mathf.Cos(a*Mathf.Deg2Rad),0,Mathf.Sin(a*Mathf.Deg2Rad))*r;candidates.Add(p);}
var starterGroups = new List<Dictionary<string,object>>();
var usedPerLevel = new Dictionary<int,int>{{1,0},{3,0},{5,0}};
var maxPerLevel = new Dictionary<int,int>{{1,2},{3,6},{5,6}};
var starterCenters = new List<Vector3>();
// 시작점 기준 방향을 고르게 쓰도록 각도 순으로 섞어 탐색한다.
foreach(var c in candidates.OrderBy(p=>(Mathf.Atan2(p.z-start.z,p.x-start.x)*Mathf.Rad2Deg*7.31f)%360f)) {
    int level=levelAt(c);
    if(!maxPerLevel.ContainsKey(level) || usedPerLevel[level]>=maxPerLevel[level])continue;
    if(isInSettlement(c))continue;
    if(existingCenters.Any(p=>Vector3.Distance(new Vector3(p.x,0,p.z),c)<groupSpacing) || starterCenters.Any(p=>Vector3.Distance(p,c)<groupSpacing))continue;
    if(monsters.Any(m=>Vector3.Distance(new Vector3(m.transform.position.x,0,m.transform.position.z),c)<monsterRadius))continue;
    var road=nearestRoad(c); float roadDistance=Vector3.Distance(new Vector3(road.x,0,road.z),c);
    if(roadDistance<12 || roadDistance>45)continue;
    var templates=starterTemplates[level];
    var sources=templates[usedPerLevel[level]%templates.Length];
    var points=new List<Vector3>(); var members=new List<Dictionary<string,object>>();
    bool ok=true;
    for(int i=0;i<sources.Length;i++){var d=def(sources[i]);if(d.EffectiveLevel!=level){ok=false;break;}var spot=findSpot(d,c,level,i,points,new HashSet<UPlayGround.MonsterActor>());if(spot==null){ok=false;break;}points.Add(spot.Value);members.Add(member(d,spot.Value,level));}
    if(!ok){rejected.Add("starter "+c.x.ToString("F0")+","+c.z.ToString("F0")+" Lv"+level);continue;}
    starterGroups.Add(new Dictionary<string,object>{{"id","starter_"+starterGroups.Count.ToString("00")},{"route","입문 구간 보강"},{"level",level},{"template",string.Join("+",sources)},{"members",members}});
    starterCenters.Add(c); placedPoints.AddRange(points); usedPerLevel[level]++;
}

// B. 레벨 구간별 4인 거점: 근접 2 + 원거리 2. 동시 공격은 그룹 슬롯(근접 1·원거리 1)이 제한한다.
var campBands = new[]{10,15,20,25,30,40};
var root = GameObject.Find("탐험 콘텐츠/필드 몬스터 성장 배치");
var campGroups = new List<Dictionary<string,object>>();
foreach(int level in campBands){
    int armor=level==10?6:level==15?7:level==20?8:level==25?9:level==30?10:11;
    string a=armor.ToString("00");
    var candidatesForBand = root.GetComponentsInChildren<UPlayGround.Group.MonsterGroupController>(true)
        .Select(g=>new{g,m=g.GetComponentsInChildren<UPlayGround.MonsterActor>(true)})
        .Where(x=>x.m.Length>=2 && x.m.Length<=3 && x.m.All(m=>m.Definition!=null && m.Definition.EffectiveLevel==level && m.Definition.actorId.StartsWith("Enemy_P09")) && !x.m.Any(m=>m.Definition.actorId.Contains("GreatSword")))
        .OrderByDescending(x=>Vector3.Distance(nearestRoad(x.g.transform.position),x.g.transform.position))
        .ToArray();
    bool placed=false;
    foreach(var x in candidatesForBand){
        int bows=x.m.Count(m=>m.Definition.actorId.Contains("Bow"));
        int melee=x.m.Length-bows;
        var add=new List<string>();
        for(int i=bows;i<2;i++)add.Add("Enemy_P09_Armor"+a+"_Bow");
        for(int i=melee;i<2;i++)add.Add("Enemy_P09_Armor"+a+"_SwordShield");
        if(add.Count==0 || x.m.Length+add.Count!=4)continue;
        var own=new HashSet<UPlayGround.MonsterActor>(x.m);
        var points=x.m.Select(m=>m.transform.position).ToList();
        var center=new Vector3(points.Average(p=>p.x),0,points.Average(p=>p.z));
        var members=new List<Dictionary<string,object>>(); bool ok=true;
        for(int i=0;i<add.Count;i++){var d=def(add[i]);if(d.EffectiveLevel!=level){ok=false;break;}var spot=findSpot(d,center,level,i+3,points,own);if(spot==null){ok=false;break;}points.Add(spot.Value);members.Add(member(d,spot.Value,level));}
        if(!ok){rejected.Add("camp "+x.g.name);continue;}
        var existingGuid=x.m[0].GetComponent<UPlayGround.Components.SceneEntityId>().Guid;
        campGroups.Add(new Dictionary<string,object>{{"groupPath",AnimationUtility.CalculateTransformPath(x.g.transform,null)},{"anchorGuid",existingGuid},{"level",level},{"before",string.Join("+",x.m.Select(m=>m.Definition.actorId))},{"members",members}});
        placed=true;break;
    }
    if(!placed)rejected.Add("camp Lv"+level+": 후보 없음");
}
var result=new Dictionary<string,object>{{"starterGroups",starterGroups},{"campAugments",campGroups},{"rejected",rejected}};
var serialize=json.GetMethod("SerializeObject",new[]{typeof(object)});
System.IO.Directory.CreateDirectory("output/OpenWorld/MonsterPopulation/Improvement");
System.IO.File.WriteAllText("Tools/OpenWorld/monster-variety-resolved.json",(string)serialize.Invoke(null,new object[]{result}));
return "starter="+starterGroups.Count+" ("+string.Join(", ",starterGroups.Select(g=>"Lv"+g["level"]+":"+g["template"]))+")\ncamps="+campGroups.Count+" ("+string.Join(", ",campGroups.Select(g=>"Lv"+g["level"]))+")\nrejected="+rejected.Count+": "+string.Join(" | ",rejected.Take(12));
