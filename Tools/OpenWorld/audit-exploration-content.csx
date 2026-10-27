if(EditorApplication.isPlaying)throw new Exception("Edit Mode 필요");
var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic;
var recipe=typeof(UPlayGround.Editor.OpenWorldSliceAuthoring).GetMethod("LoadCityRecipe",flags).Invoke(null,null);
var pairs=new List<Tuple<string,string>>{
 Tuple.Create("Assets/01.Scenes/Scenario/LakeOfLifeScenario.unity",UPlayGround.Editor.OpenWorldSliceAuthoring.HostPath),
 Tuple.Create("Assets/01.Scenes/Scenario/DroughtVillage.unity",UPlayGround.Editor.OpenWorldSliceAuthoring.RegionPath)};
foreach(var c in (Array)recipe.GetType().GetField("cities").GetValue(recipe))pairs.Add(Tuple.Create((string)c.GetType().GetField("sourcePath").GetValue(c),(string)c.GetType().GetField("regionPath").GetValue(c)));
var report=new List<string>{"UTC="+DateTime.UtcNow.ToString("O"),"Unity 실제 로드: 원본 ID 부분집합 보존·중복 ID·Missing Script. 새 소비 보상/조우 6개는 별도 추가."};
var allIds=new HashSet<string>();
foreach(var pair in pairs){
 var sets=new List<HashSet<string>>();
 foreach(string path in new[]{pair.Item1,pair.Item2}){
  var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(path);
  try{
   var objects=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
   var ids=objects.SelectMany(t=>t.GetComponents<UPlayGround.Components.SceneEntityId>()).Select(c=>c.Guid).ToArray();
   int missing=objects.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
   report.Add(path+" ids="+ids.Length+" duplicates="+(ids.Length-ids.Distinct().Count())+" missingScript="+missing);
   if(missing>0||ids.Length!=ids.Distinct().Count())throw new Exception("씬 무결성 실패 "+path);
   sets.Add(new HashSet<string>(ids));
   if(path==pair.Item2)foreach(string id in ids)if(!allIds.Add(id))throw new Exception("지역 간 중복 ID "+id);
  }finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
 }
 var removed=sets[0].Except(sets[1]).ToArray();var added=sets[1].Except(sets[0]).ToArray();
 report.Add("preserved="+(removed.Length==0)+" added="+string.Join(",",added));
 if(removed.Length>0)throw new Exception("원본 저장 ID 누락 "+string.Join(",",removed));
}
var root=GameObject.Find("ExplorationDiscoveries");
if(root==null)throw new Exception("탐험 콘텐츠 없음");
foreach(var a in root.GetComponentsInChildren<UPlayGround.MonsterActor>(true)){
 var id=a.GetComponent<UPlayGround.Components.SceneEntityId>();
 report.Add("MONSTER "+id.Guid+" actorId="+a.ActorId+" position="+a.transform.position);
}
var points=new[]{new Vector3(650,100,500),new Vector3(650,100,532),new Vector3(607,104,552),new Vector3(650,100,588),new Vector3(682,100,548)};
for(int i=0;i<points.Length;i++)for(int j=i+1;j<points.Length;j++){
 UnityEngine.AI.NavMeshHit a,b;var path=new UnityEngine.AI.NavMeshPath();
 bool sa=UnityEngine.AI.NavMesh.SamplePosition(points[i],out a,1,-1),sb=UnityEngine.AI.NavMesh.SamplePosition(points[j],out b,1,-1);
 bool result=sa&&sb&&UnityEngine.AI.NavMesh.CalculatePath(a.position,b.position,-1,path);
 report.Add("DUNGEON "+i+"->"+j+" sampled="+sa+","+sb+" result="+result+" status="+path.status+" actualStart="+a.position+" actualEnd="+b.position);
}
System.IO.File.WriteAllLines("output/OpenWorld/Exploration/content-integrity.txt",report);
return string.Join("\n",report.ToArray());
