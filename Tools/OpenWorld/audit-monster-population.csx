// 씬 저장본의 배치·참조·성장 수치를 검사한다. 전투 감각과 프레임 예산은 플레이 검수 대상이다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("월드 Edit Mode 필요");
string folder="output/OpenWorld/MonsterPopulation";
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});
Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/monster-population-resolved.json"),typeof(Dictionary<string,object>));
var plannedGroups=(Dictionary<string,object>[])read(plan["groups"].ToString(),typeof(Dictionary<string,object>[]));
var plannedMembers=plannedGroups.SelectMany(g=>(Dictionary<string,object>[])read(g["members"].ToString(),typeof(Dictionary<string,object>[]))).ToArray();
var forestGuids=new HashSet<string>(plannedGroups.Where(g=>g.ContainsKey("zone")).SelectMany(g=>(Dictionary<string,object>[])read(g["members"].ToString(),typeof(Dictionary<string,object>[]))).Select(m=>(string)m["guid"]));
var root=GameObject.Find("탐험 콘텐츠/필드 몬스터 성장 배치");
if(root==null)throw new Exception("배치 루트 누락");
var actors=root.GetComponentsInChildren<UPlayGround.MonsterActor>(true);
var identities=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).ToArray();
var errors=new List<string>();var rows=new List<string>();
if(actors.Length!=plannedMembers.Length)errors.Add("몬스터 수 불일치");
if(root.GetComponentsInChildren<UPlayGround.Group.MonsterGroupController>(true).Length!=plannedGroups.Length)errors.Add("그룹 수 불일치");
if(identities.Any(c=>!c.HasGuid) || identities.GroupBy(c=>c.Guid).Any(g=>g.Count()>1))errors.Add("빈/중복 진행 ID");
var currentIds=new HashSet<string>(identities.Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")));
foreach(var original in System.IO.File.ReadAllLines(folder+"/identities-before.txt"))if(!currentIds.Contains(original))errors.Add("기존 진행 ID/좌표 변경: "+original);
if(System.IO.File.Exists(folder+"/Infill/identities-before.txt"))foreach(var original in System.IO.File.ReadAllLines(folder+"/Infill/identities-before.txt"))if(!currentIds.Contains(original))errors.Add("숲 보강 전 진행 ID/좌표 변경: "+original);
var database=AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.ActorDatabase>("Assets/10.Datas/Actor/DataBase/ActorDatabase.asset");
var codex=AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Codex.MonsterCodexDatabaseSO>("Assets/10.Datas/Codex/Monster/MonsterCodexDatabase.asset");
var validate=typeof(UPlayGround.Editor.P09Builder.P09ProgressionBuilder).GetMethod("ValidateCombat",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
var used=actors.Select(a=>a.Definition).Concat(AssetDatabase.FindAssets("t:ActorDefinitionSO",new[]{"Assets/10.Datas/World/OpenWorld/Monsters"}).Select(AssetDatabase.GUIDToAssetPath).Select(p=>AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.ActorDefinitionSO>(p))).Distinct().ToArray();
foreach(var entry in (Dictionary<string,object>[])read(plan["existingOverrides"].ToString(),typeof(Dictionary<string,object>[]))) {
    var actor=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.MonsterActor>(true)).SingleOrDefault(a=>AnimationUtility.CalculateTransformPath(a.transform,null)==(string)entry["path"]);
    if(actor==null || actor.Definition==null || actor.Definition.EffectiveLevel!=Convert.ToInt32(entry["level"]))errors.Add("기존 필드 성장 연결 불일치: "+entry["path"]);
}
int attackCount=0;
foreach(var d in used) {
    if(d==null){errors.Add("Definition 누락");continue;}
    try{attackCount+=(int)validate.Invoke(null,new object[]{d});}catch(Exception e){errors.Add((e.InnerException??e).Message);}
    if(database.GetDefinition(d.actorId)!=d || codex.GetEntry(d.actorId)==null)errors.Add("DB/도감 연결 누락: "+d.actorId);
    if(d.prefab.GetComponent<UPlayGround.MonsterActor>().Definition!=d)errors.Add("재스폰 프리팹 Definition 불일치: "+d.actorId);
    if(d.EffectiveExpReward<=0 || d.EffectiveRecruitableAs!=UPlayGround.Data.EnumType.CharacterActorType.None)errors.Add("보상/영입 오류: "+d.actorId);
    if(AssetDatabase.GetAssetPath(d).StartsWith("Assets/10.Datas/World/OpenWorld/Monsters")) {
        if(d.level!=d.EffectiveLevel)errors.Add("Profile 레벨 불일치: "+d.actorId);
        var expected=UPlayGround.Data.Enemy.MonsterStatCalculator.Calculate(d.EffectiveMonsterScaling,d);
        foreach(var stat in expected){float actual;if(!d.attributeProfile.TryGetBaseValue(stat.Key,out actual)||!Mathf.Approximately(actual,stat.Value))errors.Add("성장 스탯 불일치: "+d.actorId+"/"+stat.Key);}
    }
    float hp;d.attributeProfile.TryGetBaseValue(new UPlayGround.Ability.Core.AttributeId("Vital.MaxHealth"),out hp);
    rows.Add(d.actorId+" Lv"+d.EffectiveLevel+" "+d.EffectiveGrade+" HP="+hp+" EXP="+d.EffectiveExpReward);
}
var terrain=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>()).Single();
var layout=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/city-layout.json"),typeof(Dictionary<string,object>));
var lines=((Dictionary<string,object>[])read(layout["routes"].ToString(),typeof(Dictionary<string,object>[]))).Select(r=>(Vector3[])read(r["points"].ToString(),typeof(Vector3[]))).ToArray();
var regions=(Dictionary<string,float>[])read(plan["safeRegions"].ToString(),typeof(Dictionary<string,float>[]));
float padding=Convert.ToSingle(plan["safeRegionPadding"]);
var bands=((Dictionary<string,object>[])read(plan["bands"].ToString(),typeof(Dictionary<string,object>[]))).OrderBy(b=>Convert.ToSingle(b["radius"])).ToArray();
var startData=(Dictionary<string,float>)read(plan["start"].ToString(),typeof(Dictionary<string,float>));
var npcs=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.NpcActor>(true)).ToArray();
var surface=GameObject.Find("AuthoredEnvironment").GetComponent<Unity.AI.Navigation.NavMeshSurface>();
var connectionFilter=new UnityEngine.AI.NavMeshQueryFilter{agentTypeID=surface.agentTypeID,areaMask=-1};
connectionFilter.SetAreaCost(UnityEngine.AI.NavMesh.GetAreaFromName("OpenWorldOffRoad"),1f);
Physics.SyncTransforms();int navPassed=0,collisions=0,localPassed=0,forestConnected=0;
foreach(var actor in actors) {
    var id=actor.GetComponent<UPlayGround.Components.SceneEntityId>();
    var m=plannedMembers.SingleOrDefault(entry=>(string)entry["guid"]==id.Guid);
    if(m==null){errors.Add("계획에 없는 개체: "+actor.name);continue;}
    var p=actor.transform.position;
    bool forest=forestGuids.Contains(id.Guid);
    if(regions.Any(b=>p.x>b["x"]-padding && p.x<b["x"]+b["width"]+padding && p.z>b["y"]-padding && p.z<b["y"]+b["height"]+padding))errors.Add("도시 안전 영역 침범: "+id.Guid);
    if(npcs.Any(n=>Vector3.Distance(n.transform.position,p)<Convert.ToSingle(plan["safeNpcRadius"])))errors.Add("NPC 안전 거리 부족: "+id.Guid);
    float startDistance=new Vector2(p.x-startData["x"],p.z-startData["z"]).magnitude;
    if(actor.Definition.EffectiveLevel!=Convert.ToInt32(bands.First(b=>startDistance<Convert.ToSingle(b["radius"]))["level"]))errors.Add("외곽 성장 구간 불일치: "+id.Guid);
    if(Vector3.Distance(p,new Vector3(Convert.ToSingle(m["x"]),Convert.ToSingle(m["y"]),Convert.ToSingle(m["z"])))>0.01f)errors.Add("위치 불일치: "+id.Guid);
    if(actor.Definition.EffectiveLevel!=Convert.ToInt32(m["level"]))errors.Add("지역 레벨 불일치: "+id.Guid);
    var capsule=actor.GetComponentInChildren<KinematicCharacterController.KinematicCharacterMotor>(true).Capsule;
    float radius=capsule.radius;
    foreach(var c in Physics.OverlapCapsule(p+Vector3.up*(radius+0.15f),p+Vector3.up*Mathf.Max(radius+0.15f,capsule.height-radius),radius+0.15f,-1,QueryTriggerInteraction.Ignore))
        if(!(c is TerrainCollider) && c.GetComponentInParent<UPlayGround.GameActor>()==null){collisions++;errors.Add("환경 충돌: "+actor.name+"/"+c.name);}
    var road=lines.SelectMany(line=>Enumerable.Range(1,line.Length-1).Select(i=>{var a=line[i-1];var b=line[i];a.y=b.y=p.y;return a+(b-a)*Mathf.Clamp01(Vector3.Dot(p-a,b-a)/(b-a).sqrMagnitude);})).OrderBy(v=>(v-p).sqrMagnitude).First();
    if(Vector3.Distance(road,p)<8+radius)errors.Add("본길 여유 폭 부족: "+id.Guid);
    road.y=terrain.SampleHeight(road)+terrain.transform.position.y;
    UnityEngine.AI.NavMeshHit from,to;
    var path=new UnityEngine.AI.NavMeshPath();
    if(UnityEngine.AI.NavMesh.SamplePosition(road,out from,4,-1) && UnityEngine.AI.NavMesh.SamplePosition(p,out to,2,-1)
        && (forest?UnityEngine.AI.NavMesh.CalculatePath(from.position,to.position,connectionFilter,path):UnityEngine.AI.NavMesh.CalculatePath(from.position,to.position,-1,path)) && path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete
        && (forest?UnityEngine.AI.NavMesh.CalculatePath(to.position,from.position,connectionFilter,path):UnityEngine.AI.NavMesh.CalculatePath(to.position,from.position,-1,path)) && path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete){navPassed++;if(forest)forestConnected++;}else errors.Add("길 왕복 연결 실패: "+id.Guid);
    if(forest){
        int localPaths=0;
        for(int direction=0;direction<8;direction++){
            float angle=direction*Mathf.PI/4;var target=p+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*18;
            target.y=terrain.SampleHeight(target)+terrain.transform.position.y;UnityEngine.AI.NavMeshHit targetHit;
            if(UnityEngine.AI.NavMesh.SamplePosition(target,out targetHit,2,-1) && UnityEngine.AI.NavMesh.CalculatePath(p,targetHit.position,-1,path) && path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete && UnityEngine.AI.NavMesh.CalculatePath(targetHit.position,p,-1,path) && path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete)localPaths++;
        }
        if(localPaths>=3)localPassed++;else errors.Add("숲 교전 공간 부족: "+id.Guid);
    }
}
int missing=root.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
if(missing!=0)errors.Add("Missing Script="+missing);
rows.Insert(0,"그룹="+plannedGroups.Length+" / 몬스터="+actors.Length+" / Definition="+used.Length+" / AI 공격="+attackCount+" / 왕복 NavMesh="+navPassed+" / 충돌="+collisions+" / Missing Script="+missing+" / 오류="+errors.Count);
rows.AddRange(errors);
rows.Add("숲 보강="+forestGuids.Count+" / 동일 비용의 지형 왕복 연결="+forestConnected+" / 기본 비용의 18m 교전 공간="+localPassed+". 런타임 길 밖 비용 6 유지; 장거리 기본 비용 조회 통과와 구분.");
System.IO.File.WriteAllLines(folder+"/audit.txt",rows);
if(errors.Count>0)throw new Exception(string.Join("\n",errors.Take(12).ToArray()));
return rows[0];


