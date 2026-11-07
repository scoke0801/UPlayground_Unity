// 현재 씬의 정적 접합과 경로만 검사한다. 실제 입력 플레이와 구분한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string folder="output/OpenWorld/EnvironmentFollowup";
if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("월드 Edit Mode 필요");
bool applied=System.IO.File.Exists(folder+"/applied.txt");string stage=applied?"after":"before";
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/environment-followup-plan.json"),typeof(Dictionary<string,object>));
var env=GameObject.Find("AuthoredEnvironment");var terrain=env.GetComponentInChildren<Terrain>();var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var ids=all.SelectMany(t=>t.GetComponents<UPlayGround.Components.SceneEntityId>()).ToArray();
var identities=ids.Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")).OrderBy(s=>s).ToArray();System.IO.File.WriteAllLines(folder+"/identities-"+stage+".txt",identities);
var rows=new List<string>();var errors=new List<string>();
if(string.Join("\n",identities)!=System.IO.File.ReadAllText(folder+"/identities-before.txt").Replace("\r","").TrimEnd('\n'))errors.Add("진행 ID/위치 변경");
var support=ids.Select(i=>i.Guid+"|"+(terrain.SampleHeight(i.transform.position)+terrain.transform.position.y).ToString("F4")).OrderBy(s=>s).ToArray();
System.IO.File.WriteAllLines(folder+"/identity-ground-"+stage+".txt",support);
if(applied&&System.IO.File.ReadAllText(folder+"/identity-ground-before.txt")!=System.IO.File.ReadAllText(folder+"/identity-ground-after.txt"))errors.Add("진행 개체 아래 지형 높이 변경");
int missing=all.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));if(missing>0)errors.Add("Missing Script="+missing);
int materials=env.GetComponentsInChildren<MeshRenderer>(true).Count(r=>r.sharedMaterials.Any(m=>m==null));if(materials>0)errors.Add("환경 재질 누락="+materials);
Physics.SyncTransforms();int paths=0,failed=0;
var routes=(Dictionary<string,object>[])read(plan["routes"].ToString(),typeof(Dictionary<string,object>[]));
foreach(var route in routes){
var points=(float[][])read(route["points"].ToString(),typeof(float[][]));
for(int i=1;i<points.Length;i++){
var a=new Vector3(points[i-1][0],0,points[i-1][1]);a.y=terrain.SampleHeight(a)+terrain.transform.position.y;
var b=new Vector3(points[i][0],0,points[i][1]);b.y=terrain.SampleHeight(b)+terrain.transform.position.y;
if(a.z< -325){a.y=50.1f;}if(b.z< -325){b.y=50.1f;}
UnityEngine.AI.NavMeshHit ah,bh;bool sampled=UnityEngine.AI.NavMesh.SamplePosition(a,out ah,4,-1)&&UnityEngine.AI.NavMesh.SamplePosition(b,out bh,4,-1);UnityEngine.AI.NavMesh.SamplePosition(b,out bh,4,-1);
for(int d=0;d<2;d++){
var path=new UnityEngine.AI.NavMeshPath();bool ok=sampled&&UnityEngine.AI.NavMesh.CalculatePath(d==0?ah.position:bh.position,d==0?bh.position:ah.position,-1,path)&&path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete;
rows.Add(route["name"]+"|"+i+"|"+d+"|"+(ok?"PASS":"FAIL")+"|"+path.status);if(ok)paths++;else failed++;
}
}
}
if(applied){
var root=GameObject.Find("AuthoredEnvironment/환경 후속 개선");foreach(Transform t in root.transform){
var rs=t.GetComponentsInChildren<MeshRenderer>(true);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
float ground=terrain.SampleHeight(t.position)+terrain.transform.position.y;
if(b.min.y>ground+.05f||b.max.y<ground)errors.Add("암반 지형 접합 실패 "+t.name);
if(t.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled))errors.Add("암반 충돌 없음 "+t.name);
if(ids.Any(id=>b.SqrDistance(id.transform.position)<25))errors.Add("진행 개체 근접 "+t.name);
}
var surface=env.GetComponent<Unity.AI.Navigation.NavMeshSurface>();var nav=surface.navMeshData;
if(AssetDatabase.GetAssetPath(nav)!="Assets/10.Datas/World/OpenWorld/EnvironmentFollowup/NavigationFinal.asset")errors.Add("후속 NavMesh 연결 안 됨");
else{int tiles=new SerializedObject(nav).FindProperty("m_NavMeshTiles").arraySize;long size=new System.IO.FileInfo(AssetDatabase.GetAssetPath(nav)).Length;rows.Add("NavMesh tiles="+tiles+" bytes="+size);if(tiles==0||size<100000)errors.Add("저장된 NavMesh 타일 없음");}
}
rows.AddRange(errors.Select(e=>"FAIL "+e));rows.Add("RESULT errors="+errors.Count+" routePass="+paths+" routeFail="+failed+" identities="+ids.Length+" MissingScript="+missing+" missingMaterials="+materials);
System.IO.File.WriteAllLines(folder+"/audit-"+stage+".txt",rows);return string.Join("\n",rows.ToArray());
