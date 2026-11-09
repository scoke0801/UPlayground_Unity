if(Application.dataPath!="C:/UsingProject/UnityProject/UPlayground/Assets"||!System.IO.File.ReadAllText("Tools/WorldThemes/AgentCoordination/editor.lock").Contains("\"C\""))throw new Exception("C 작업권 필요");
var host=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(host.path!="Assets/01.Scenes/ArtReview/WorldThemes/HarborReedR1.unity"||host.isDirty||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("저장된 검토 씬 필요");
string stage=System.IO.File.ReadAllText("Tools/WorldThemes/HarborReedR1/stage.txt").Trim();
Func<Vector3,float[]> vec=p=>new[]{p.x,p.y,p.z};
Func<Transform,string> path=t=>{string p=t.name;while(t.parent!=null){t=t.parent;p=t.name+"/"+p;}return p;};
var route=new[]{new Vector3(1920,50,-244),new Vector3(1920,50,-288),new Vector3(1920,50,-326),new Vector3(1905,50,-326),new Vector3(1905,50,-346),new Vector3(1884,50,-326),new Vector3(1884,50,-350),new Vector3(1981,50,-326),new Vector3(1981,50,-352)};
var edges=new[]{new[]{0,1},new[]{1,2},new[]{2,3},new[]{3,4},new[]{3,5},new[]{5,6},new[]{2,7},new[]{7,8}};
var navRows=new List<object>();var supports=new List<object>();Physics.SyncTransforms();
foreach(var edge in edges){for(int direction=0;direction<2;direction++){
 var p=route[edge[direction]];var q=route[edge[1-direction]];UnityEngine.AI.NavMeshHit a,b;
 bool sa=UnityEngine.AI.NavMesh.SamplePosition(p,out a,1,-1),sb=UnityEngine.AI.NavMesh.SamplePosition(q,out b,1,-1);var np=new UnityEngine.AI.NavMeshPath();
 bool complete=sa&&sb&&UnityEngine.AI.NavMesh.CalculatePath(a.position,b.position,-1,np)&&np.status==UnityEngine.AI.NavMeshPathStatus.PathComplete;
 navRows.Add(new{start=vec(p),end=vec(q),sampleStart=sa,sampleEnd=sb,complete,status=np.status.ToString(),corners=np.corners.Select(vec).ToArray()});
 }var start=route[edge[0]];var end=route[edge[1]];int steps=Mathf.CeilToInt(Vector3.Distance(start,end));var side=Vector3.Cross(Vector3.up,(end-start).normalized);
 for(int i=0;i<=steps;i++)for(int offset=-1;offset<=1;offset++){
  var p=Vector3.Lerp(start,end,i/(float)steps)+side*offset;var hits=Physics.RaycastAll(p+Vector3.up*2,Vector3.down,15,-1,QueryTriggerInteraction.Ignore).Where(h=>h.collider.gameObject.scene==host&&h.normal.y>.5f).OrderBy(h=>h.distance).ToArray();
  supports.Add(new{p=vec(p),supported=hits.Length>0,height=hits.Length==0?-999:hits[0].point.y,collider=hits.Length==0?null:path(hits[0].collider.transform)});
 }}
var regionActors=new List<object>();
string regionPath="Assets/01.Scenes/Scenario/OpenWorld/Regions/SaltwindHarborRegion.unity";
if(UnityEngine.SceneManagement.SceneManager.GetSceneByPath(regionPath).isLoaded)throw new Exception("이미 열린 지역 씬 보호");
var region=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(regionPath,UnityEditor.SceneManagement.OpenSceneMode.Additive);
try{Physics.SyncTransforms();foreach(var actor in region.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.GameActor>(true))){var p=actor.transform.position;
var support=Physics.RaycastAll(p+Vector3.up*.6f,Vector3.down,3,-1,QueryTriggerInteraction.Ignore).Where(h=>h.collider.gameObject.scene==host&&h.normal.y>.5f).OrderBy(h=>h.distance).ToArray();
regionActors.Add(new{path=path(actor.transform),type=actor.GetType().FullName,p=vec(p),supported=support.Length>0,height=support.Length==0?-999:support[0].point.y});}}
finally{UnityEditor.SceneManagement.EditorSceneManager.CloseScene(region,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(host);}
var data=AssetDatabase.LoadAssetAtPath<UPlayGround.Data.World.WorldPlacementDataSO>(AssetDatabase.GUIDToAssetPath("2819d64223022c84b9a62983354d6ecb"));var links=new List<string>();
if(data!=null)foreach(var c in host.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MonoBehaviour>(true)).Where(c=>c!=null)){var so=new SerializedObject(c);var p=so.GetIterator();while(p.NextVisible(true))if(p.propertyType==SerializedPropertyType.ObjectReference&&p.objectReferenceValue==data)links.Add(c.GetType().FullName+"."+p.propertyPath);}
var enemies=data==null?new object[0]:data.Records.Where(r=>r.position.x>1818&&r.position.x<2008&&r.position.z>-410&&r.position.z<-220&&!string.IsNullOrEmpty(r.actorId)).Select(r=>(object)new{r.actorId,r.groupGuid,r.sceneEntityGuid,r.groupName,r.monsterChaseDistance,p=vec(r.position)}).ToArray();
var result=new{stage,navRows,supports,regionActors,placement= data==null?null:AssetDatabase.GetAssetPath(data),links,enemies,scope="Editor 정적 NavMesh/물리 검사. 실제 입력 TPS·게임 저장복원·성능 검증 아님. 적이 없으면 비행 검사 해당없음, 연결이 없으면 미확정."};
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(a=>a!=null);
System.IO.File.WriteAllText("output/HarborReedR1/paths-"+stage+".json",(string)json.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{result}));
return new{paths=navRows.Count,supports=supports.Count,actors=regionActors.Count,placementLinks=links.Count,enemies=enemies.Length};
