if(!EditorApplication.isPlaying)throw new Exception("Play Mode 필요");
var terrain=UnityEngine.Object.FindFirstObjectByType<Terrain>();
var rows=new List<string>{"UTC="+DateTime.UtcNow.ToString("O"),"실제 Play Mode 현재 액터 높이. 전투·순찰·리스폰 인수는 별도."};
int buried=0;
foreach(var actor in UnityEngine.Object.FindObjectsByType<UPlayGround.MonsterActor>(FindObjectsSortMode.None)){
 var p=actor.transform.position;
 float ground=terrain.SampleHeight(p)+terrain.transform.position.y;
 var motor=actor.GetComponentInChildren<KinematicCharacterController.KinematicCharacterMotor>();
 rows.Add(actor.name+" position="+p+" terrain="+ground.ToString("F3")+" delta="+(p.y-ground).ToString("F3")+" grounded="+(motor!=null&&motor.GroundingStatus.IsStableOnGround));
 if(p.y<ground-1)buried++;
}
rows.Add("belowTerrain="+buried+" RESULT="+(buried==0?"PASS":"FAIL"));
System.IO.File.WriteAllLines("output/OpenWorld/Exploration/play/actor-grounding.txt",rows);
return string.Join("\n",rows.ToArray());
