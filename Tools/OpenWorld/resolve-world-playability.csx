// 현재 NavMesh와 실제 바닥에서 배치 후보를 검사한다. 씬은 변경하지 않는다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity") throw new Exception("월드 Edit Mode 필요");
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});
Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/world-playability-plan.json"),typeof(Dictionary<string,object>));
var sites=(Dictionary<string,object>[])read(plan["sites"].ToString(),typeof(Dictionary<string,object>[]));
var terrain=GameObject.Find("AuthoredEnvironment").GetComponentInChildren<Terrain>();
var rows=new List<string>(); var errors=new List<string>();
var used=new List<Vector3>();
foreach(var site in sites) foreach(string key in new[]{"moves","enemies","loot"}) {
    var items=(Dictionary<string,object>[])read(site[key].ToString(),typeof(Dictionary<string,object>[]));
    foreach(var item in items) {
        var a=(float[])read(item["position"].ToString(),typeof(float[]));
        var wanted=new Vector3(a[0],a[1],a[2]);
        if(wanted.y==0)wanted.y=terrain.SampleHeight(wanted)+terrain.transform.position.y;
        Vector3 chosen=Vector3.zero;bool found=false;
        for(int ring=0;ring<=5 && !found;ring++) for(int direction=0;direction<(ring==0?1:12) && !found;direction++) {
            var p=wanted+new Vector3(Mathf.Cos(direction*Mathf.PI/6),0,Mathf.Sin(direction*Mathf.PI/6))*ring;
            if(a[1]==0)p.y=terrain.SampleHeight(p)+terrain.transform.position.y;
            UnityEngine.AI.NavMeshHit nav;
            if(!UnityEngine.AI.NavMesh.SamplePosition(p,out nav,1.5f,-1) || Mathf.Abs(nav.position.y-p.y)>1.6f)continue;
            var floors=Physics.RaycastAll(nav.position+Vector3.up,Vector3.down,2.5f,-1,QueryTriggerInteraction.Ignore)
                .Where(h=>h.normal.y>.7f && h.collider.GetComponentInParent<UPlayGround.GameActor>()==null && Mathf.Abs(h.point.y-nav.position.y)<.8f).OrderBy(h=>h.distance).ToArray();
            if(floors.Length==0)continue;
            p=floors[0].point+Vector3.up*.03f;
            float radius=key=="loot"?.65f:.6f;
            if(Physics.OverlapCapsule(p+Vector3.up*(radius+.1f),p+Vector3.up*1.6f,radius,-1,QueryTriggerInteraction.Ignore)
                .Any(c=>!(c is TerrainCollider) && c.GetComponentInParent<UPlayGround.GameActor>()==null))continue;
            if(used.Any(v=>Vector3.Distance(v,p)<2.5f))continue;
            chosen=p;found=true;
        }
        if(!found) {errors.Add(site["id"]+"/"+item["guid"]+" 안전한 배치 후보 없음");continue;}
        used.Add(chosen);item["position"]=new[]{chosen.x,chosen.y,chosen.z};
        rows.Add(site["id"]+" "+key+" "+item["guid"]+" "+wanted+" -> "+chosen);
    }
    site[key]=items;
}
if(errors.Count>0)throw new Exception(string.Join("\n",errors.ToArray()));
plan["sites"]=sites;
var serialize=json.GetMethod("SerializeObject",new[]{typeof(object)});
System.IO.File.WriteAllText("Tools/OpenWorld/world-playability-resolved.json",(string)serialize.Invoke(null,new object[]{plan}));
System.IO.File.WriteAllLines("output/OpenWorld/WorldPlayability/resolution.txt",rows);
return string.Join("\n",rows.ToArray());

