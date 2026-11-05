// 후보 주변의 실제 지형·충돌·완전 경로를 검사하고 배치표를 확정한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(Application.isPlaying || scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity") throw new Exception("월드 편집 모드 필요");
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});
var stringify=json.GetMethod("SerializeObject",new[]{typeof(object)});
Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/Wildlife/placement-candidates.json"),typeof(Dictionary<string,object>));
var candidates=(Dictionary<string,object>[])read(plan["placements"].ToString(),typeof(Dictionary<string,object>[]));
var recipe=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/city-layout.json"),typeof(Dictionary<string,object>));
var lines=((Dictionary<string,object>[])read(recipe["routes"].ToString(),typeof(Dictionary<string,object>[]))).Select(r=>(Vector3[])read(r["points"].ToString(),typeof(Vector3[]))).ToArray();
Func<Vector3,float> roadDistance=p=>lines.SelectMany(line=>Enumerable.Range(1,line.Length-1).Select(i=>{var a=line[i-1];var b=line[i];a.y=b.y=p.y;return Vector3.Distance(p,a+(b-a)*Mathf.Clamp01(Vector3.Dot(p-a,b-a)/(b-a).sqrMagnitude));})).Min();
var terrain=Terrain.activeTerrains.Single();
var monsters=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.MonsterActor>(true)).Select(c=>c.transform.position).ToArray();
var existing=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Gameplay.World.LivingWorld.WildlifeCreature>(true)).Select(c=>c.transform.position).ToList();
var result=new List<Dictionary<string,object>>();
var rejected=new List<string>();
var path=new UnityEngine.AI.NavMeshPath();
var filter=new UnityEngine.AI.NavMeshQueryFilter{agentTypeID=0,areaMask=-1};
Physics.SyncTransforms();
foreach(var entry in candidates) {
 var desired=(Vector3)read(entry["position"].ToString(),typeof(Vector3));
 var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Environment/Wildlife/"+entry["prefab"]+".prefab");
 if(prefab==null)throw new Exception("프리팹 생성 대기: "+entry["prefab"]);
 var profile=prefab.GetComponent<UPlayGround.Gameplay.World.LivingWorld.WildlifeCreature>().Profile;
 var agent=prefab.GetComponent<UnityEngine.AI.NavMeshAgent>();
 float radius=agent!=null?agent.radius:.1f;
 bool found=false;var reason="";
 for(int attempt=0;attempt<65;attempt++) {
  float angle=attempt*137.508f*Mathf.Deg2Rad;
  float offset=attempt==0?0:2f*Mathf.Sqrt(attempt);
  var p=desired+new Vector3(Mathf.Cos(angle)*offset,0,Mathf.Sin(angle)*offset);
  p.y=terrain.SampleHeight(p)+terrain.transform.position.y;
  var local=p-terrain.transform.position;
  if(terrain.terrainData.GetSteepness(local.x/terrain.terrainData.size.x,local.z/terrain.terrainData.size.z)>27){reason="급경사";continue;}
  UnityEngine.AI.NavMeshHit hit;
  if(!UnityEngine.AI.NavMesh.SamplePosition(p,out hit,.6f,filter) || Mathf.Abs(hit.position.y-p.y)>.4f){reason="NavMesh 접지";continue;}
  p=hit.position;
  if(roadDistance(p)<3+radius){reason="주동선";continue;}
  if(existing.Any(q=>Vector3.Distance(p,q)<Mathf.Max(1.2f,radius*3))){reason="개체 간격";continue;}
  if(monsters.Any(q=>Vector3.Distance(p,q)<Mathf.Max(18,profile.CalmDistance))){reason="전투 조우";continue;}
  var overlaps=Physics.OverlapCapsule(p+Vector3.up*(radius+.12f),p+Vector3.up*Mathf.Max(radius+.12f,agent!=null?agent.height-radius:.5f),radius,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
  if(overlaps.Any(c=>!(c is TerrainCollider))){reason="구조물/식생 충돌";continue;}
  int paths=0;
  for(int i=0;i<8;i++) {
   var q=p+Quaternion.Euler(0,i*45,0)*Vector3.forward*profile.RoamRadius*.7f;
   q.y=terrain.SampleHeight(q)+terrain.transform.position.y;
   UnityEngine.AI.NavMeshHit target;
   if(UnityEngine.AI.NavMesh.SamplePosition(q,out target,1,filter) && Mathf.Abs(target.position.y-q.y)<.5f && UnityEngine.AI.NavMesh.CalculatePath(p,target.position,filter,path) && path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete)paths++;
  }
  if(paths<6){reason="배회 경로 부족";continue;}
  if(profile.IsFlying)p.y+=.8f;
  entry["position"]=new Dictionary<string,float>{{"x",p.x},{"y",p.y},{"z",p.z}};
  entry["reachableDirections"]=paths;
  result.Add(entry);existing.Add(p);found=true;break;
 }
 if(!found) rejected.Add(entry["id"]+": "+reason);
}
var resolved=new Dictionary<string,object>{{"scene",scene.path},{"placements",result}};
System.IO.File.WriteAllText("Tools/Wildlife/LakeVillageWorld.animals.json",(string)stringify.Invoke(null,new object[]{resolved}));
System.IO.Directory.CreateDirectory("output/Wildlife/AnimalPackDeluxe");
System.IO.File.WriteAllText("output/Wildlife/AnimalPackDeluxe/resolution.txt","배치: "+result.Count+" / 제외: "+rejected.Count+"\n"+string.Join("\n",rejected));
return new {accepted=result.Count,rejected=rejected.ToArray()};

