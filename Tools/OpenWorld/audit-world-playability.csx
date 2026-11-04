// 실제 Unity 에셋·NavMesh·콜라이더를 검사한다. 입력 보행과 전투 품질 인수는 별도다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("월드 Edit Mode 필요");
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});
Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/world-playability-resolved.json"),typeof(Dictionary<string,object>));
var sites=(Dictionary<string,object>[])read(plan["sites"].ToString(),typeof(Dictionary<string,object>[]));
var terrain=GameObject.Find("AuthoredEnvironment").GetComponentInChildren<Terrain>();
var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var identities=all.SelectMany(t=>t.GetComponents<UPlayGround.Components.SceneEntityId>()).ToArray();
var rows=new List<string>{"UTC="+DateTime.UtcNow.ToString("O"),"Edit Mode: 실제 에셋·물리·왕복 경로. 실제 입력/전투/파일 저장 재시작과 구분."};
var errors=new List<string>();int paths=0,placements=0;
if(identities.Any(i=>!i.HasGuid)||identities.Length!=identities.Select(i=>i.Guid).Distinct().Count())errors.Add("빈/중복 진행 ID");
int missing=all.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
if(missing>0)errors.Add("Missing Script="+missing);
var byId=identities.ToDictionary(i=>i.Guid);
Physics.SyncTransforms();
Func<float[],Vector3> point=a=>{var p=new Vector3(a[0],a[1],a[2]);if(p.y==0)p.y=terrain.SampleHeight(p)+terrain.transform.position.y;return p;};
foreach(var site in sites) {
    var entry=point((float[])read(site["entry"].ToString(),typeof(float[])));
    var targets=((float[][])read(site["points"].ToString(),typeof(float[][]))).Select(point).ToList();
    foreach(string key in new[]{"moves","enemies","loot"}) foreach(var item in (Dictionary<string,object>[])read(site[key].ToString(),typeof(Dictionary<string,object>[]))) {
        var identity=byId[(string)item["guid"]];var p=identity.transform.position;
        if(Vector3.Distance(p,point((float[])read(item["position"].ToString(),typeof(float[]))))>.01f)errors.Add("배치 위치 불일치: "+identity.Guid);
        var monster=identity.GetComponent<UPlayGround.MonsterActor>();
        float radius=.5f,height=1.8f;
        if(monster!=null) {
            var motor=monster.GetComponentInChildren<KinematicCharacterController.KinematicCharacterMotor>(true);
            radius=motor.Capsule.radius+.1f;height=motor.Capsule.height;
            if(monster.Definition==null||monster.GetComponent<UPlayGround.Components.EnemyAIController>()==null||monster.GetComponentInParent<UPlayGround.Group.MonsterGroupController>()==null)errors.Add("몬스터/그룹 연결: "+identity.Guid);
        } else {
            var pickup=identity.GetComponent<UPlayGround.DropItemActor>();
            if(pickup==null||pickup.ItemData==null||pickup.InteractionData==null||pickup.Count<=0||!pickup.GetComponent<Collider>().enabled)errors.Add("상호작용 데이터: "+identity.Guid);
        }
        var blockers=Physics.OverlapCapsule(p+Vector3.up*(radius+.12f),p+Vector3.up*Mathf.Max(radius+.12f,height-radius),radius,-1,QueryTriggerInteraction.Ignore)
            .Where(c=>!(c is TerrainCollider)&&c.GetComponentInParent<UPlayGround.GameActor>()==null).Select(c=>c.name).Distinct().ToArray();
        if(blockers.Length>0)errors.Add("환경 충돌: "+identity.Guid+" "+string.Join(",",blockers));
        var support=Physics.RaycastAll(p+Vector3.up*.15f,Vector3.down,.5f,-1,QueryTriggerInteraction.Ignore).Any(h=>h.normal.y>.6f&&h.collider.GetComponentInParent<UPlayGround.GameActor>()==null);
        if(!support)errors.Add("지지 바닥 없음: "+identity.Guid+" "+p);
        targets.Add(p);placements++;
    }
    foreach(var target in targets) {
        UnityEngine.AI.NavMeshHit a,b;
        bool sampled=UnityEngine.AI.NavMesh.SamplePosition(entry,out a,3,-1)&&UnityEngine.AI.NavMesh.SamplePosition(target,out b,2,-1);
        if(!sampled){errors.Add("경로 표본 실패: "+site["id"]+" "+target);continue;}
        UnityEngine.AI.NavMesh.SamplePosition(target,out b,2,-1);
        for(int direction=0;direction<2;direction++) {
            var route=new UnityEngine.AI.NavMeshPath();
            bool ok=UnityEngine.AI.NavMesh.CalculatePath(direction==0?a.position:b.position,direction==0?b.position:a.position,-1,route)&&route.status==UnityEngine.AI.NavMeshPathStatus.PathComplete;
            float length=0;for(int i=1;i<route.corners.Length;i++)length+=Vector3.Distance(route.corners[i-1],route.corners[i]);
            rows.Add(site["id"]+" "+target+" 방향="+direction+" "+route.status+" 길이="+length.ToString("F1"));
            if(ok)paths++;else errors.Add("왕복 경로 실패: "+site["id"]+" "+target+" "+route.status);
        }
    }
}
var allowedMoves=new HashSet<string>(sites.SelectMany(s=>(Dictionary<string,object>[])read(s["moves"].ToString(),typeof(Dictionary<string,object>[]))).Select(i=>(string)i["guid"]));
foreach(var line in System.IO.File.ReadAllLines("output/OpenWorld/WorldPlayability/identities-before.txt")) {
    var split=line.Split('|');if(!byId.ContainsKey(split[0]))errors.Add("기존 ID 소실: "+split[0]);
    else if(!allowedMoves.Contains(split[0])&&byId[split[0]].transform.position.ToString("F4")!=split[1])errors.Add("비대상 위치 변경: "+split[0]);
}
rows.AddRange(errors.Select(e=>"FAIL "+e));
rows.Add("RESULT errors="+errors.Count+" paths="+paths+" placements="+placements+" identities="+identities.Length+" MissingScript="+missing);
System.IO.File.WriteAllLines("output/OpenWorld/WorldPlayability/audit.txt",rows);
return string.Join("\n",rows.Skip(Math.Max(0,rows.Count-errors.Count-1)).ToArray());
