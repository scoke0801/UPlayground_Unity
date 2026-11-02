var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||scene.isDirty||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("저장된 월드 Edit Mode 필요");
var terrain=GameObject.Find("AuthoredEnvironment").GetComponentInChildren<Terrain>();
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});
Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/district-route-repair.json"),typeof(Dictionary<string,object>));
var routes=(Dictionary<string,object>[])read(plan["routes"].ToString(),typeof(Dictionary<string,object>[]));
var expectedPaths=(string[])read(plan["expectedProgressBarriers"].ToString(),typeof(string[]));
var expected=expectedPaths.Select(p=>GameObject.Find(p)).ToArray();
if(expected.Any(g=>g==null))throw new Exception("진행 장벽 경로 누락");
var conditional=new HashSet<string>();
var report=new System.Text.StringBuilder();int failures=0,samples=0;
Physics.SyncTransforms();
foreach(var route in routes) {
    var points=(float[][])read(route["points"].ToString(),typeof(float[][]));
    var hits=new List<Vector3>();var valid=new List<bool>();var blockers=new HashSet<string>();
    foreach(var point in points) {
        var p=new Vector3(point[0],0,point[1]);p.y=terrain.SampleHeight(p)+terrain.transform.position.y;
        UnityEngine.AI.NavMeshHit hit;
        bool found=UnityEngine.AI.NavMesh.SamplePosition(p,out hit,1,-1);valid.Add(found);
        if(!found){failures++;report.AppendLine("표본 누락: "+route["site"]+" "+p);}
        hits.Add(hit.position);
    }
    for(int i=1;i<points.Length;i++) {
        var a=new Vector3(points[i-1][0],0,points[i-1][1]);var b=new Vector3(points[i][0],0,points[i][1]);
        var side=Vector3.Cross((b-a).normalized,Vector3.up);int steps=Mathf.CeilToInt(Vector3.Distance(a,b));
        var path=new UnityEngine.AI.NavMeshPath();
        if(valid[i-1]&&valid[i])foreach(bool reverse in new[]{false,true})if(!UnityEngine.AI.NavMesh.CalculatePath(hits[reverse?i:i-1],hits[reverse?i-1:i],-1,path)||path.status!=UnityEngine.AI.NavMeshPathStatus.PathComplete){failures++;report.AppendLine("경로 단절: "+route["site"]+" 선분="+i);}
        for(int k=0;k<=steps;k++)foreach(float offset in new[]{-1.5f,0,1.5f}) {
            var p=Vector3.Lerp(a,b,(float)k/steps)+side*offset;p.y=terrain.SampleHeight(p)+terrain.transform.position.y;samples++;
            foreach(var c in Physics.OverlapCapsule(p+Vector3.up*.5f,p+Vector3.up*1.4f,.4f,-1,QueryTriggerInteraction.Ignore)) {
                if(c is TerrainCollider)continue;
                if(expected.Any(g=>c.gameObject==g)){conditional.Add(c.name);continue;}
                var outer=PrefabUtility.GetOutermostPrefabInstanceRoot(c.gameObject);
                if(blockers.Add(outer==null?c.name:outer.name))report.AppendLine("장애물: "+route["site"]+" 선분="+i+" 표본="+p+" "+(outer==null?c.name:outer.name)+" 범위="+c.bounds.min+" ~ "+c.bounds.max);
            }
        }
    }
    failures+=blockers.Count;
    report.AppendLine(route["site"]+" 장애물="+blockers.Count);
}
report.AppendLine("표본="+samples+" 실패="+failures);
report.AppendLine("진행 해금 전 장벽 충돌="+string.Join(",",conditional)+" / 장벽은 변경하지 않음");
System.IO.Directory.CreateDirectory("output/OpenWorld/DistrictInfill");
System.IO.File.WriteAllText("output/OpenWorld/DistrictInfill/route-preflight.txt",report.ToString());
return report.ToString();
