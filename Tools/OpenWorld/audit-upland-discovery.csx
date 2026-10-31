var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || scene.path!=UPlayGround.Editor.OpenWorldSliceAuthoring.HostPath)throw new Exception("준비된 생성 월드 Edit Mode 필요");
var folder="output/OpenWorld/Exploration/Iteration05";System.IO.Directory.CreateDirectory(folder);
var terrain=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>()).Single();
Func<Vector3,Vector3> ground=p=>new Vector3(p.x,terrain.SampleHeight(p)+terrain.transform.position.y,p.z);
var authoring=typeof(UPlayGround.Editor.OpenWorldSliceAuthoring);
var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic;
var recipe=authoring.GetMethod("LoadCityRecipe",flags).Invoke(null,null);
var routes=((Array)recipe.GetType().GetField("routes").GetValue(recipe)).Cast<object>().ToArray();
var rows=new List<string>{"UTC="+DateTime.UtcNow.ToString("O"),"Terrain/NavMesh/실제 모델 검사. 입력 보행·시야 인수 아님"};
int count=0,failures=0;
foreach(var route in routes.Where(q=>new[]{"cloud-upland","upland-lake-loop"}.Contains((string)q.GetType().GetField("id").GetValue(q)))){
 var points=(Vector3[])route.GetType().GetField("points").GetValue(route);
 for(int i=1;i<points.Length;i++)for(int direction=0;direction<2;direction++){
  var start=ground(points[direction==0?i-1:i]);var end=ground(points[direction==0?i:i-1]);
  UnityEngine.AI.NavMeshHit a,b;bool sa=UnityEngine.AI.NavMesh.SamplePosition(start,out a,3,-1),sb=UnityEngine.AI.NavMesh.SamplePosition(end,out b,3,-1);
  var path=new UnityEngine.AI.NavMeshPath();bool complete=sa&&sb&&UnityEngine.AI.NavMesh.CalculatePath(a.position,b.position,-1,path)&&path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete;
  float length=0;for(int j=1;j<path.corners.Length;j++)length+=Vector3.Distance(path.corners[j-1],path.corners[j]);
  float grade=0;var previous=start;for(int j=1;j<=30;j++){var p=ground(Vector3.Lerp(start,end,j/30f));float d=Vector2.Distance(new Vector2(previous.x,previous.z),new Vector2(p.x,p.z));grade=Mathf.Max(grade,Mathf.Abs(p.y-previous.y)/Mathf.Max(.1f,d));previous=p;}
  if(!complete)failures++;count++;
  rows.Add(route.GetType().GetField("id").GetValue(route)+" segment="+(i-1)+" direction="+direction+" complete="+complete+" length="+length.ToString("F1")+" maxGrade="+grade.ToString("F3")+" "+start+" -> "+end);
 }
}
// 원경 생활권은 점프 직행이 아니라 기존 본길로 도달할 수 있어야 한다.
foreach(var target in new[]{new Vector3(1190,0,300),new Vector3(1005,0,430),new Vector3(1630,0,1100)}){
 UnityEngine.AI.NavMeshHit a,b;var start=ground(new Vector3(1310,0,1310));var end=ground(target);
 bool sa=UnityEngine.AI.NavMesh.SamplePosition(start,out a,3,-1),sb=UnityEngine.AI.NavMesh.SamplePosition(end,out b,3,-1);
 var path=new UnityEngine.AI.NavMeshPath();bool complete=sa&&sb&&UnityEngine.AI.NavMesh.CalculatePath(a.position,b.position,-1,path)&&path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete;
 if(!complete)failures++;rows.Add("DESTINATION "+target+" PathComplete="+complete+" corners="+path.corners.Length);
}
var transforms=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
int roadIndex=Array.FindIndex(terrain.terrainData.terrainLayers,l=>AssetDatabase.GetAssetPath(l).EndsWith("TerrainLayer_Dirt_02.terrainlayer"));
if(roadIndex<0)throw new Exception("도로 지표 누락");
foreach(var point in new[]{new Vector3(1140,0,1338),new Vector3(1225,0,1265)}){
 var data=terrain.terrainData;
 int x=Mathf.RoundToInt((point.x-terrain.transform.position.x)/data.size.x*(data.alphamapWidth-1));
 int z=Mathf.RoundToInt((point.z-terrain.transform.position.z)/data.size.z*(data.alphamapHeight-1));
 float weight=data.GetAlphamaps(x,z,1,1)[0,0,roadIndex];
 bool valid=point.x==1140?weight<.02f:weight>.5f;
 rows.Add("SURFACE "+point+" roadWeight="+weight+" valid="+valid);
 if(!valid)failures++;
}
foreach(var id in new[]{"Explore_0630","Explore_0631","Explore_0579","Explore_0582","Expansion_rock_345","Expansion_rock_353"}){
 var t=transforms.Single(v=>v.name==id);var bounds=(Bounds)authoring.GetMethod("GetPropMeshBounds",flags).Invoke(null,new object[]{t.gameObject});
 rows.Add("MODEL "+id+" position="+t.position+" bounds="+bounds+" prefab="+PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject));
}
rows.Add("SEGMENTS="+count+" FAILURES="+failures);
System.IO.File.WriteAllLines(folder+"/upland-connections.txt",rows);
return string.Join("\n",rows.ToArray());
