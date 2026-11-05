// 실제 수면·지면·동선·충돌·완전 경로로 후보를 확인하고 이전 좌표도 배치표에 보존한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(Application.isPlaying || scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity") throw new Exception("월드 편집 모드 필요");
var layoutType=typeof(UPlayGround.Tool.Editor.Map.WildlifeAuthoring).GetNestedType("Layout",System.Reflection.BindingFlags.NonPublic);
var layout=JsonUtility.FromJson(System.IO.File.ReadAllText("Tools/Wildlife/LakeVillageWorld.animals.json"),layoutType);
var entries=(Array)layoutType.GetField("placements").GetValue(layout);
var plans=new Dictionary<string,Vector3> {
 {"APD_lake-bank_01",new Vector3(1184,0,608)}, {"APD_lake-bank_02",new Vector3(1186,0,611)},
 {"APD_lake-bank_03",new Vector3(1188,0,619)}, {"APD_lake-bank_04",new Vector3(1190,0,624)},
 {"APD_lake-bank_05",new Vector3(1194,0,629)}, {"APD_lake-bank_06",new Vector3(1178,0,592)},
 {"APD_lake-bank_07",new Vector3(1181,0,598)}, {"APD_forest-depth_03",new Vector3(1584,0,917)},
 {"APD_village-pasture_03",new Vector3(1032,0,399)} };
var animals=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Gameplay.World.LivingWorld.WildlifeCreature>(true)).ToArray();
var terrain=Terrain.activeTerrain;
var water=GameObject.Find("LakeWater").GetComponent<Renderer>().bounds;
var path=new UnityEngine.AI.NavMeshPath();
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});
Func<string,Type,object> read=(value,type)=>parse.Invoke(null,new object[]{value,type});
var recipe=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/city-layout.json"),typeof(Dictionary<string,object>));
var routes=((Dictionary<string,object>[])read(recipe["routes"].ToString(),typeof(Dictionary<string,object>[]))).Select(r=>(Vector3[])read(r["points"].ToString(),typeof(Vector3[]))).ToArray();
Func<Vector3,float> roadDistance=p=>routes.SelectMany(line=>Enumerable.Range(1,line.Length-1).Select(i=> {var a=line[i-1];var b=line[i];a.y=b.y=p.y;return Vector3.Distance(p,a+(b-a)*Mathf.Clamp01(Vector3.Dot(p-a,b-a)/(b-a).sqrMagnitude));})).DefaultIfEmpty(100).Min();
var trees=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name.IndexOf("Tree",StringComparison.OrdinalIgnoreCase)>=0 && t.GetComponent<Renderer>()!=null).Select(t=>t.position).ToArray();
var occupied=animals.Where(a=>!plans.ContainsKey(a.name)).Select(a=>a.transform.position).ToList();
var records=new List<string>();
foreach(var item in entries){
 var type=item.GetType(); string id=(string)type.GetField("id").GetValue(item);if(!plans.ContainsKey(id) || (bool)type.GetField("hasPreviousPosition").GetValue(item))continue;
 var animal=animals.Single(a=>a.name==id);var profile=animal.Profile;var agent=animal.GetComponent<UnityEngine.AI.NavMeshAgent>();
 var filter=new UnityEngine.AI.NavMeshQueryFilter{agentTypeID=agent.agentTypeID,areaMask=agent.areaMask};
 Vector3 old=(Vector3)type.GetField("position").GetValue(item);bool found=false;
 for(int attempt=0;attempt<250;attempt++) {
  float angle=attempt*137.50776f*Mathf.Deg2Rad;float radius=Mathf.Sqrt(attempt)*.65f;
  var candidate=plans[id]+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
  candidate.y=terrain.SampleHeight(candidate)+terrain.transform.position.y;
  UnityEngine.AI.NavMeshHit hit;
  if(!UnityEngine.AI.NavMesh.SamplePosition(candidate,out hit,.5f,filter)||Mathf.Abs(hit.position.y-candidate.y)>.6f)continue;
  if(Vector3.Angle(terrain.terrainData.GetInterpolatedNormal((candidate.x-terrain.transform.position.x)/terrain.terrainData.size.x,(candidate.z-terrain.transform.position.z)/terrain.terrainData.size.z),Vector3.up)>profile.MaxLandingSlope)continue;
  bool isBank=id.StartsWith("APD_lake-bank");
  if(isBank && (candidate.y<water.max.y+.25f || candidate.y>water.max.y+3f))continue;
  float nearestWater=100;
  if(isBank)for(int direction=0;direction<16;direction++)for(float d=1;d<=14;d+=1){var q=candidate+Quaternion.Euler(0,direction*22.5f,0)*Vector3.forward*d;if(q.x>=water.min.x&&q.x<=water.max.x&&q.z>=water.min.z&&q.z<=water.max.z&&terrain.SampleHeight(q)+terrain.transform.position.y<water.max.y)nearestWater=Mathf.Min(nearestWater,d);}
  if(isBank&&nearestWater>12)continue;
  if(roadDistance(candidate)<3+agent.radius)continue;
  if(occupied.Any(p=>Vector3.Distance(p,candidate)<Mathf.Max(profile.PersonalSpace,1)))continue;
  var overlaps=Physics.OverlapCapsule(candidate+Vector3.up*(agent.radius+.1f),candidate+Vector3.up*Mathf.Max(agent.radius+.1f,agent.height-agent.radius),agent.radius,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
  if(overlaps.Any(c=>!(c is TerrainCollider)))continue;
  int directions=0;
  for(int d=0;d<8;d++){var q=candidate+Quaternion.Euler(0,d*45,0)*Vector3.forward*profile.RoamRadius*.6f;q.y=terrain.SampleHeight(q)+terrain.transform.position.y; if(isBank&&q.y<=water.max.y)continue;UnityEngine.AI.NavMeshHit nh;if(UnityEngine.AI.NavMesh.SamplePosition(q,out nh,.5f,filter)&&UnityEngine.AI.NavMesh.CalculatePath(hit.position,nh.position,filter,path)&&path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete)directions++;}
  if(directions<4)continue;
  candidate.y+=profile.GroundClearance;
  type.GetField("previousPosition").SetValue(item,old);type.GetField("hasPreviousPosition").SetValue(item,true);type.GetField("position").SetValue(item,candidate);
  occupied.Add(candidate);records.Add(id+" / "+old+" → "+candidate+" / 도달 방향="+directions+" / 물가 거리="+nearestWater+" / 가까운 나무="+trees.Select(p=>Vector3.Distance(p,candidate)).DefaultIfEmpty(999).Min());found=true;break;
 }
 if(!found)throw new Exception(id+": 안전한 후보 없음. 배치표를 저장하지 않습니다.");
}
System.IO.File.WriteAllText("Tools/Wildlife/LakeVillageWorld.animals.json",JsonUtility.ToJson(layout,true));
System.IO.Directory.CreateDirectory("output/WildlifeObservation");System.IO.File.WriteAllLines("output/WildlifeObservation/relocation.txt",records);
return string.Join("\n",records);
