var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||scene.isDirty||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("저장된 월드 Edit Mode 필요");
var root=GameObject.Find("AuthoredEnvironment/확장 생활권과 지형 경관");var terrain=GameObject.Find("AuthoredEnvironment").GetComponentInChildren<Terrain>();
string folder="output/OpenWorld/DistrictInfill";
System.IO.Directory.CreateDirectory(folder);
if(root==null)throw new Exception("확장 생활권 배치 누락");
Physics.SyncTransforms();
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var report=new System.Text.StringBuilder();int missing=0,materials=0,invalid=0;
foreach(var t in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)))missing+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
foreach(var r in root.GetComponentsInChildren<Renderer>(true))foreach(var m in r.sharedMaterials)if(m==null||m.shader==null||m.shader.name.Contains("InternalError"))materials++;
foreach(var mf in root.GetComponentsInChildren<MeshFilter>(true).Where(m=>m.name.StartsWith("Wheat_LOD")))foreach(var v in mf.sharedMesh.vertices)if(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.x)||float.IsInfinity(v.y)||float.IsInfinity(v.z))invalid++;
report.AppendLine("전체 씬 Missing Script="+missing+" / 신규 재질·셰이더 오류="+materials+" / 잘못된 작물 정점="+invalid);
int passed=0,failed=0,index=0;
foreach(string file in new[]{"Tools/OpenWorld/regional-cover-plan.json","Tools/OpenWorld/marked-infill-plan.json","Tools/OpenWorld/district-infill-plan.json"}) {
    var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText(file),typeof(Dictionary<string,object>));
    var routes=(Dictionary<string,object>[])read(plan["routes"].ToString(),typeof(Dictionary<string,object>[]));
    foreach(var route in routes) {
        var points=(float[][])read(route["points"].ToString(),typeof(float[][]));var hits=new List<Vector3>();bool sample=true,complete=true;float maximumError=0;
        foreach(var point in points) {
            var p=new Vector3(point[0],0,point[1]);p.y=terrain.SampleHeight(p)+terrain.transform.position.y;
            UnityEngine.AI.NavMeshHit hit;if(!UnityEngine.AI.NavMesh.SamplePosition(p,out hit,4,-1)){sample=false;report.AppendLine("표본 누락 "+p);break;}hits.Add(hit.position);maximumError=Mathf.Max(maximumError,Mathf.Abs(p.y-hit.position.y));
        }
        var path=new UnityEngine.AI.NavMeshPath();
        if(sample)for(int i=1;i<hits.Count;i++)foreach(bool reverse in new[]{false,true}) {
            var a=hits[reverse?i:i-1];var b=hits[reverse?i-1:i];bool ok=UnityEngine.AI.NavMesh.CalculatePath(a,b,-1,path)&&path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete;
            complete&=ok;if(!ok)report.AppendLine("실패 "+a+" -> "+b+" / "+path.status);
        }
        if(sample&&complete)passed++;else failed++;
        report.AppendLine("경로 "+(index++)+" / "+(route.ContainsKey("site")?route["site"].ToString():"기존")+" / 표본="+sample+" / 양방향="+complete+" / 최대 지면 차이="+maximumError.ToString("F2"));
    }
}
string ids=string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")).OrderBy(s=>s));
bool same=ids==System.IO.File.ReadAllText(folder+"/identities-before.txt");
report.AppendLine("기존 진행 ID·좌표 보존="+same+" / 경로 통과="+passed+" 실패="+failed);
var marked=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/district-infill-plan.json"),typeof(Dictionary<string,object>));
var markedRoutes=(Dictionary<string,object>[])read(marked["routes"].ToString(),typeof(Dictionary<string,object>[]));
var repair=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/district-route-repair.json"),typeof(Dictionary<string,object>));
var expectedPaths=(string[])read(repair["expectedProgressBarriers"].ToString(),typeof(string[]));
var expected=expectedPaths.Select(p=>GameObject.Find(p)).ToArray();
if(expected.Any(g=>g==null))throw new Exception("진행 장벽 경로 누락");
var conditional=new HashSet<string>();
var blockers=new HashSet<string>();var edgeBlockers=new HashSet<string>();int physicalSamples=0,edgeSamples=0;
foreach(var route in markedRoutes) {
    var points=(float[][])read(route["points"].ToString(),typeof(float[][]));
    for(int i=1;i<points.Length;i++) {
        var a=new Vector3(points[i-1][0],0,points[i-1][1]);var b=new Vector3(points[i][0],0,points[i][1]);int steps=Mathf.CeilToInt(Vector3.Distance(a,b)/2);
        var side=Vector3.Cross((b-a).normalized,Vector3.up);
        for(int k=0;k<=steps;k++)foreach(float offset in new[]{-1.5f,0,1.5f}) {
            var p=Vector3.Lerp(a,b,(float)k/steps)+side*offset;p.y=terrain.SampleHeight(p)+terrain.transform.position.y;
            if(offset==0)physicalSamples++;else edgeSamples++;
            foreach(var collider in Physics.OverlapCapsule(p+Vector3.up*.5f,p+Vector3.up*1.4f,.4f,-1,QueryTriggerInteraction.Ignore)) {
                if(collider is TerrainCollider)continue;
                if(expected.Any(g=>collider.gameObject==g)){conditional.Add(collider.name);continue;}
                var outer=PrefabUtility.GetOutermostPrefabInstanceRoot(collider.gameObject);
                if((offset==0?blockers:edgeBlockers).Add(route["site"]+" / "+(outer==null?collider.name:outer.name)))
                    report.AppendLine("장애물 위치: "+route["site"]+" / 선분="+i+" / 횡방향="+offset+" / 표본="+p+" / "+(outer==null?collider.name:outer.name)+" / 범위="+collider.bounds.min+" ~ "+collider.bounds.max);
            }
        }
    }
}
report.AppendLine("신규 길 중심선 물리 표본="+physicalSamples+" / 전체 씬 장애물 충돌="+blockers.Count);
foreach(string blocker in blockers)report.AppendLine("길 중심 충돌: "+blocker);
report.AppendLine("길 좌우 1.5m 물리 표본="+edgeSamples+" / 전체 씬 장애물 충돌="+edgeBlockers.Count);
foreach(string blocker in edgeBlockers)report.AppendLine("길 가장자리 충돌: "+blocker);
report.AppendLine("진행 해금 전 장벽 충돌="+string.Join(",",conditional)+" / 장벽은 변경하지 않음");
var foundations=new System.Text.StringBuilder("name,groundRange,bottomOffset\n");
foreach(var t in root.GetComponentsInChildren<Transform>().Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)&&(t.name.Contains("PoorHouse_")||t.name.Contains("LakeHouse_")))) {
    var rs=t.GetComponentsInChildren<MeshRenderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);float low=float.MaxValue,high=float.MinValue;
    foreach(float x in new[]{b.min.x,b.center.x,b.max.x})foreach(float z in new[]{b.min.z,b.center.z,b.max.z}) {float h=terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y;low=Mathf.Min(low,h);high=Mathf.Max(high,h);}
    foundations.AppendLine(t.name+","+(high-low)+","+(b.min.y-high));
}
System.IO.File.WriteAllText(folder+"/foundation-audit.csv",foundations.ToString());
var placementRows=(Dictionary<string,object>[])read(marked["placements"].ToString(),typeof(Dictionary<string,object>[]));
var rowsById=placementRows.ToDictionary(r=>r["id"].ToString());
var applied=new System.Text.StringBuilder("id,site,x,y,z,kind\n");
foreach(var t in root.GetComponentsInChildren<Transform>().Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject))) {
    var row=rowsById[t.name.Substring(0,t.name.IndexOf('_'))];
    applied.AppendLine(row["id"]+","+row["site"]+","+t.position.x+","+t.position.y+","+t.position.z+","+row["kind"]);
}
System.IO.File.WriteAllText(folder+"/placements.csv",applied.ToString());
System.IO.File.WriteAllText(folder+"/audit.txt",report.ToString());
if(missing!=0||materials!=0||invalid!=0||!same||failed!=0||blockers.Count!=0||edgeBlockers.Count!=0)throw new Exception("생활권 검증 실패: "+folder+"/audit.txt 확인");
return report.ToString();


