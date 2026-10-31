if(EditorApplication.isPlaying)throw new Exception("Edit Mode 필요");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var terrain=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>()).Single();
var rows=new List<string>{"UTC="+DateTime.UtcNow.ToString("O"),"수로 경유 구간을 개별 검사. 입력 보행을 대신하지 않음."};
var points=new[]{new Vector3(1540,0,510),new Vector3(1700,0,740),new Vector3(1640,0,750),new Vector3(1607,0,805),new Vector3(1650,0,855),new Vector3(1750,0,870),new Vector3(1630,0,1100)};
for(int i=0;i<points.Length-1;i++)for(int d=0;d<2;d++){
 var start=points[d==0?i:i+1];var end=points[d==0?i+1:i];
 start.y=terrain.SampleHeight(start)+terrain.transform.position.y;end.y=terrain.SampleHeight(end)+terrain.transform.position.y;
 UnityEngine.AI.NavMeshHit a,b;var path=new UnityEngine.AI.NavMeshPath();
 if(!UnityEngine.AI.NavMesh.SamplePosition(start,out a,2,-1)||!UnityEngine.AI.NavMesh.SamplePosition(end,out b,2,-1)||!UnityEngine.AI.NavMesh.CalculatePath(a.position,b.position,-1,path)||path.status!=UnityEngine.AI.NavMeshPathStatus.PathComplete)throw new Exception("구간 실패 "+i+" direction="+d);
 rows.Add("VIA_WATER "+i+" direction="+d+" status="+path.status+" "+a.position+" -> "+b.position);
}
var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var target=all.Single(t=>t.name=="c42bb9f480ec4f3cacdf786ab0a59e21");
var actor=target.GetComponent<UPlayGround.GatheringActor>();
if(actor==null||actor.GetData()==null||actor.GetData().hp!=20||actor.GetData().regrowGameMinutes!=0||actor.GetData().dropItems.Single().maximumDropCount!=1)throw new Exception("실제 채집 데이터 연결 실패");
rows.Add("ACTOR "+target.position+" data="+AssetDatabase.GetAssetPath(actor.GetData())+" map="+UPlayGround.Manager.World.RegionOwnership.Resolve(actor));
foreach(var id in new[]{"Explore_0617","Explore_0618","Explore_0619","Expansion_log_792"}){
 var t=all.Single(v=>v.name==id);var bounds=t.GetComponentsInChildren<MeshRenderer>().First().bounds;
 rows.Add(id+" bounds="+bounds+" rotation="+t.rotation.eulerAngles);
 if(id.StartsWith("Explore_")&&bounds.size.y>2)throw new Exception("목재 흔적 높이 초과 "+id);
}
rows.Add("RESULT=PASS");
System.IO.Directory.CreateDirectory("output/OpenWorld/Exploration/Iteration04");
System.IO.File.WriteAllLines("output/OpenWorld/Exploration/Iteration04/forest-connections.txt",rows);
return string.Join("\n",rows.ToArray());
