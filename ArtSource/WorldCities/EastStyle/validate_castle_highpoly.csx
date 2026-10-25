// 재로드 뒤 동작 계약, 실제 임포트 메시/LOD 및 기존 22개 경로를 검사한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.name!="StonegateCitadel"||Application.isPlaying||UnityEditor.EditorApplication.isCompiling)throw new System.Exception("돌등성 Edit Mode 필요");
var roots=scene.GetRootGameObjects();
var castle=GameObject.Find("StonegateEastArt").transform.Find("HighPolyCastle");
var errors=new System.Collections.Generic.List<string>();
var report=new System.Collections.Generic.List<string>();
var components=roots.SelectMany(r=>r.GetComponentsInChildren<MonoBehaviour>(true)).Where(c=>c!=null&&(c is UPlayGround.GameActor||c is UPlayGround.IInteractable||c.GetType().Name=="SceneArrivalPoint"||c.GetType().Name.Contains("Flow"))).OrderBy(c=>c.name+c.GetType().FullName).ToArray();
var contracts=components.Select(c=>c.name+"|"+c.GetType().FullName+"|"+c.transform.position.ToString("F5")+"|"+UnityEditor.EditorJsonUtility.ToJson(c)).ToArray();
if(!System.IO.File.ReadAllLines("output/StonegateHighPoly/Before/contracts.txt").SequenceEqual(contracts))errors.Add("동작/좌표 계약 변경");
System.IO.File.WriteAllLines("output/StonegateHighPoly/After/contracts.txt",contracts);
report.Add("계약 컴포넌트="+components.Length+", 주민="+roots.Sum(r=>r.GetComponentsInChildren<UPlayGround.NpcActor>().Length));
int missing=roots.Sum(r=>r.GetComponentsInChildren<Transform>(true).Sum(t=>UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)));
report.Add("Missing Script="+missing);if(missing!=0)errors.Add("Missing Script");
foreach(Transform building in castle){
 var group=building.GetComponent<LODGroup>();var levels=group.GetLODs();long previous=long.MaxValue;
 if(levels.Length!=3)errors.Add(building.name+" LOD 개수");
 for(int i=0;i<levels.Length;i++){
  long triangles=0;
  foreach(var renderer in levels[i].renderers){
   if(renderer==null){errors.Add(building.name+" LOD 참조 누락");continue;}
   var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
   if(mesh==null){errors.Add(renderer.name+" 메시 누락");continue;}
   for(int sub=0;sub<mesh.subMeshCount;sub++)triangles+=(long)mesh.GetIndexCount(sub)/3;
   if(mesh.subMeshCount!=renderer.sharedMaterials.Length||renderer.sharedMaterials.Any(m=>m==null||m.shader==null||m.shader.name=="Hidden/InternalErrorShader"))errors.Add(renderer.name+" 재질 오류");
  }
  if(triangles<=0||triangles>=previous)errors.Add(building.name+" LOD 감소 오류");previous=triangles;
  report.Add(building.name+" LOD"+i+" triangles="+triangles+" renderers="+levels[i].renderers.Length);
 }
 var bounds=levels[0].renderers.Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});report.Add(building.name+" bounds="+bounds);
}
Physics.SyncTransforms();
var colliders=castle.GetComponentsInChildren<Collider>();
foreach(var actor in roots.SelectMany(r=>r.GetComponentsInChildren<UPlayGround.NpcActor>())){
 var clear=new Bounds(actor.transform.position+Vector3.up,new Vector3(3,2,3));
 foreach(var collider in colliders)if(collider.enabled&&collider.bounds.Intersects(clear))errors.Add(actor.name+" 신규 충돌 근접: "+collider.name);
}
var surface=roots.First(r=>r.name=="CityEnvironment").GetComponent<Unity.AI.Navigation.NavMeshSurface>();
var others=UnityEngine.Object.FindObjectsByType<Unity.AI.Navigation.NavMeshSurface>(FindObjectsSortMode.None).Where(s=>s!=surface&&s.isActiveAndEnabled&&s.navMeshData!=null).ToArray();
foreach(var other in others)other.RemoveData();
try{
 var points=components.Select(c=>c.transform.position).Concat(new[]{new Vector3(180,10,120),new Vector3(180,10,221),new Vector3(180,10,180)}).Distinct().ToArray();
 UnityEngine.AI.NavMeshHit start;bool hasStart=UnityEngine.AI.NavMesh.SamplePosition(new Vector3(180,10,55),out start,1,UnityEngine.AI.NavMesh.AllAreas);
 foreach(var point in points){
  UnityEngine.AI.NavMeshHit end;var route=new UnityEngine.AI.NavMeshPath();
  bool ok=hasStart&&UnityEngine.AI.NavMesh.SamplePosition(point,out end,2,UnityEngine.AI.NavMesh.AllAreas)&&UnityEngine.AI.NavMesh.CalculatePath(start.position,end.position,UnityEngine.AI.NavMesh.AllAreas,route)&&route.status==UnityEngine.AI.NavMeshPathStatus.PathComplete;
  report.Add("Path "+point+"="+ok);if(!ok)errors.Add("경로 실패 "+point);
 }
}finally{foreach(var other in others)if(other!=null&&other.isActiveAndEnabled)other.AddData();}
report.Add("Errors="+errors.Count);report.AddRange(errors);
System.IO.Directory.CreateDirectory("output/StonegateHighPoly/After");System.IO.File.WriteAllLines("output/StonegateHighPoly/After/validation.txt",report.ToArray());
if(errors.Count>0)throw new System.Exception(string.Join("\n",errors.ToArray()));
return string.Join("\n",report.ToArray());
