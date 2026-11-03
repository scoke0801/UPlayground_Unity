var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("월드 Edit Mode가 필요합니다.");
string folder="output/OpenWorld/WaterLandscape";System.IO.Directory.CreateDirectory(folder);
var root=GameObject.Find("AuthoredEnvironment/남서 초지와 물길 숲 보강");
if(root==null)throw new Exception("신규 경관 누락");
var terrain=GameObject.Find("AuthoredEnvironment").GetComponentInChildren<Terrain>();
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var report=new System.Text.StringBuilder();int missing=scene.GetRootGameObjects().Sum(g=>g.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)));
int materials=root.GetComponentsInChildren<Renderer>(true).Sum(r=>r.sharedMaterials.Count(m=>m==null||m.shader==null||m.shader.name.Contains("InternalError")));
int passed=0,failed=0,blocked=0,samples=0;
Physics.SyncTransforms();
foreach(string file in new[]{"regional-cover-plan","marked-infill-plan","district-infill-plan"}) {
    var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/"+file+".json"),typeof(Dictionary<string,object>));
    var routes=(Dictionary<string,object>[])read(plan["routes"].ToString(),typeof(Dictionary<string,object>[]));
    foreach(var route in routes) {
        var points=(float[][])read(route["points"].ToString(),typeof(float[][]));var hits=new List<Vector3>();bool valid=true;
        foreach(var point in points){var p=new Vector3(point[0],0,point[1]);p.y=terrain.SampleHeight(p)+terrain.transform.position.y;UnityEngine.AI.NavMeshHit hit;if(!UnityEngine.AI.NavMesh.SamplePosition(p,out hit,4,-1)){valid=false;break;}hits.Add(hit.position);}
        var path=new UnityEngine.AI.NavMeshPath();
        if(valid)for(int i=1;i<hits.Count;i++)foreach(bool reverse in new[]{false,true})valid&=UnityEngine.AI.NavMesh.CalculatePath(hits[reverse?i:i-1],hits[reverse?i-1:i],-1,path)&&path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete;
        if(valid)passed++;else{failed++;report.AppendLine("경로 실패: "+file+" / "+(route.ContainsKey("site")?route["site"]:"기존"));}
        for(int i=1;i<points.Length;i++) {
            var a=new Vector3(points[i-1][0],0,points[i-1][1]);var b=new Vector3(points[i][0],0,points[i][1]);int steps=Mathf.CeilToInt(Vector3.Distance(a,b)/3);var side=Vector3.Cross((b-a).normalized,Vector3.up);
            for(int k=0;k<=steps;k++)foreach(float offset in new[]{-1.5f,0,1.5f}) {
                var p=Vector3.Lerp(a,b,(float)k/steps)+side*offset;p.y=terrain.SampleHeight(p)+terrain.transform.position.y;samples++;
                foreach(var c in Physics.OverlapCapsule(p+Vector3.up*.5f,p+Vector3.up*1.4f,.4f,-1,QueryTriggerInteraction.Ignore))
                    if(c.transform.IsChildOf(root.transform)){blocked++;report.AppendLine("신규 장애물: "+c.name+" / "+p);}
            }
        }
    }
}
string ids=string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")).OrderBy(s=>s));
bool same=ids==System.IO.File.ReadAllText(folder+"/identities-before.txt");
var shader=Shader.Find("UPlayGround/Environment/Lake Water");int errors=ShaderUtil.GetShaderMessages(shader).Count(m=>m.severity.ToString()=="Error");
report.AppendLine("Missing Script="+missing+" / 신규 재질 오류="+materials+" / 셰이더 오류="+errors+" / 진행 ID·좌표 보존="+same);
report.AppendLine("양방향 연결 통과="+passed+" / 실패="+failed+" / 물리 표본="+samples+" / 신규 배치의 길 침범="+blocked);
System.IO.File.WriteAllText(folder+"/audit.txt",report.ToString());
if(missing!=0||materials!=0||errors!=0||!same||failed!=0||blocked!=0)throw new Exception("경관 검사 실패: "+folder+"/audit.txt");
return report.ToString();
