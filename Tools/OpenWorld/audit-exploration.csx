var folder="output/OpenWorld/Exploration";System.IO.Directory.CreateDirectory(folder);
var authoring=typeof(UPlayGround.Editor.OpenWorldSliceAuthoring);
var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic;
var recipe=authoring.GetMethod("LoadCityRecipe",flags).Invoke(null,null);
var recipeType=recipe.GetType();
var terrain=UnityEngine.Object.FindFirstObjectByType<Terrain>();
Func<Vector3,Vector3> ground=p=>new Vector3(p.x,terrain.SampleHeight(p)+terrain.transform.position.y,p.z);
var routes=new List<Tuple<string,Vector3[]>>();
foreach(var c in (Array)recipeType.GetField("cities").GetValue(recipe))routes.Add(Tuple.Create((string)c.GetType().GetField("mapId").GetValue(c),(Vector3[])c.GetType().GetField("road").GetValue(c)));
foreach(var q in (Array)recipeType.GetField("routes").GetValue(recipe))routes.Add(Tuple.Create((string)q.GetType().GetField("id").GetValue(q),(Vector3[])q.GetType().GetField("points").GetValue(q)));
var rows=new List<string>{"UTC="+DateTime.UtcNow.ToString("O"),"지형 기준 4m 종단·횡단 표본; 실제 보행/카메라 품질 합격을 뜻하지 않음"};
var issues=new List<string>();
foreach(var route in routes){
 float maxGrade=0,maxCrossGrade=0,maxRidge=0;Vector3 steepest=Vector3.zero;int samples=0,segmentFailures=0;
 var points=route.Item2;
 for(int i=1;i<points.Length;i++){
  var a=points[i-1];var b=points[i];var delta=b-a;delta.y=0;var side=Vector3.Cross(delta.normalized,Vector3.up);
  int steps=Mathf.CeilToInt(delta.magnitude/4);
  var previous=ground(a);
  for(int j=1;j<=steps;j++){
   var p=ground(Vector3.Lerp(a,b,j/(float)steps));var horizontal=p-previous;horizontal.y=0;
   float grade=Mathf.Abs(p.y-previous.y)/Mathf.Max(.1f,horizontal.magnitude);
   if(grade>maxGrade){maxGrade=grade;steepest=p;}
   var left=ground(p+side*8);var right=ground(p-side*8);
   float ridge=Mathf.Min(p.y-left.y,p.y-right.y);maxRidge=Mathf.Max(maxRidge,ridge);
   maxCrossGrade=Mathf.Max(maxCrossGrade,Mathf.Max(Mathf.Abs(p.y-left.y),Mathf.Abs(p.y-right.y))/8);
   previous=p;samples++;
  }
  UnityEngine.AI.NavMeshHit s,e;var nav=new UnityEngine.AI.NavMeshPath();
  bool sampled=UnityEngine.AI.NavMesh.SamplePosition(ground(a),out s,3,-1)&&UnityEngine.AI.NavMesh.SamplePosition(ground(b),out e,3,-1);
  // 분리된 표본 성공으로 전체 조회 실패를 숨기지 않는다.
  UnityEngine.AI.NavMesh.SamplePosition(ground(b),out e,3,-1);
  if(!sampled||!UnityEngine.AI.NavMesh.CalculatePath(s.position,e.position,-1,nav)||nav.status!=UnityEngine.AI.NavMeshPathStatus.PathComplete){segmentFailures++;issues.Add(route.Item1+" segment="+(i-1)+" from="+ground(a)+" to="+ground(b)+" status="+nav.status);}
 }
 rows.Add(route.Item1+" samples="+samples+" maxGrade="+maxGrade.ToString("F3")+" steepest="+steepest+" maxCrossGrade8m="+maxCrossGrade.ToString("F3")+" bothShouldersDrop8m="+maxRidge.ToString("F3")+" segmentFailures="+segmentFailures);
 for(int direction=0;direction<2;direction++){
  var start=ground(points[direction==0?0:points.Length-1]);var end=ground(points[direction==0?points.Length-1:0]);
  UnityEngine.AI.NavMeshHit a,b;bool sa=UnityEngine.AI.NavMesh.SamplePosition(start,out a,3,-1),sb=UnityEngine.AI.NavMesh.SamplePosition(end,out b,3,-1);
  var nav=new UnityEngine.AI.NavMeshPath();bool found=sa&&sb&&UnityEngine.AI.NavMesh.CalculatePath(a.position,b.position,-1,nav);
  rows.Add(route.Item1+" FULL "+direction+" sampled="+sa+","+sb+" result="+found+" status="+nav.status+" start="+a.position+" end="+b.position+" last="+nav.corners.LastOrDefault());
 }
}
foreach(var actor in UnityEngine.Object.FindObjectsByType<UPlayGround.DropItemActor>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(a=>a.GetComponentInParent<Transform>().root.name=="ExplorationDiscoveries")){
 var id=actor.GetComponent<UPlayGround.Components.SceneEntityId>();rows.Add("PICKUP "+id.Guid+" item="+(actor.ItemData==null?"NULL":actor.ItemData.name)+" count="+actor.Count+" interaction="+(actor.InteractionData!=null)+" map="+UPlayGround.Manager.World.RegionOwnership.Resolve(actor)+" position="+actor.transform.position);
}
rows.Add("NavMesh triangles="+UnityEngine.AI.NavMesh.CalculateTriangulation().indices.Length/3);
System.IO.File.WriteAllLines(folder+"/terrain-navigation.txt",rows);System.IO.File.WriteAllLines(folder+"/route-issues.txt",issues);
return string.Join("\n",rows.ToArray());
