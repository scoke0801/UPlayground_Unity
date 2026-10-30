if(!EditorApplication.isPlaying)throw new Exception("Play Mode 필요");
var world=UPlayGround.Manager.World.ContinuousWorldController.Active;
var player=UPlayGround.Manager.PartyManager.Instance.ActiveCharacter;
if(world==null||!world.IsInitialized||world.IsTravelling||player==null)throw new Exception("월드 준비 대기");
var folder="output/OpenWorld/Exploration/play";
System.IO.Directory.CreateDirectory(folder);
System.IO.File.WriteAllText(folder+"/region-entry.txt","RUNNING "+DateTime.UtcNow.ToString("O"));
var originalPosition=player.transform.position;
var originalMap=UPlayGround.Manager.SceneManager.Instance.CurrentMapID;
var rows=new List<string>{"UTC="+DateTime.UtcNow.ToString("O"),"실제 Play Mode 지역 로드·진입 계약 검사. 위치 이동 API이며 입력 보행/전투/파일 저장 검사는 아님."};
int index=0,stage=0;
double started=EditorApplication.timeSinceStartup,next=started,deadline=started+20;
EditorApplication.CallbackFunction tick=null;
tick=()=>{
 bool finished=false;
 try{
  if(!EditorApplication.isPlaying||player==null||EditorApplication.timeSinceStartup-started>65)throw new Exception("지역 검사 중단 또는 시간 초과");
  if(EditorApplication.timeSinceStartup<next)return;
  var region=world.Regions[index];
  if(stage==0){
   player.ActorController.Motor.SetPosition(region.SafeEntryPosition+Vector3.up*.1f);
   world.RestoreLocation(world.ResidentMapId,player);
   region.Request(true);stage=1;deadline=EditorApplication.timeSinceStartup+20;next=EditorApplication.timeSinceStartup+.5;return;
  }
  if(stage==1){
   if(region.State!=UPlayGround.Manager.World.RegionLoadState.Ready){
    if(EditorApplication.timeSinceStartup>deadline)throw new Exception(region.MapId+" 준비 실패 "+region.State+" "+region.Failure);
    next=EditorApplication.timeSinceStartup+.5;return;
   }
   UnityEngine.AI.NavMeshHit a,b;
   bool sa=UnityEngine.AI.NavMesh.SamplePosition(region.SafeEntryPosition,out a,3,-1),sb=UnityEngine.AI.NavMesh.SamplePosition(region.NeighborEntryPosition,out b,3,-1);
   var path=new UnityEngine.AI.NavMeshPath();
   if(!sa||!sb||!UnityEngine.AI.NavMesh.CalculatePath(a.position,b.position,-1,path)||path.status!=UnityEngine.AI.NavMeshPathStatus.PathComplete)throw new Exception(region.MapId+" 로드 후 연결 경로 실패");
   var localSurfaces=region.LoadedScene.GetRootGameObjects().Sum(g=>g.GetComponentsInChildren<Unity.AI.Navigation.NavMeshSurface>(true).Length);
   if(localSurfaces!=0)throw new Exception(region.MapId+" 중복 NavMeshSurface="+localSurfaces);
   rows.Add(region.MapId+" Ready=True fullConnection="+path.status+" localSurfaces="+localSurfaces);
   player.ActorController.Motor.SetPosition(region.NeighborEntryPosition+Vector3.up*.1f);
   stage=2;next=EditorApplication.timeSinceStartup+1.5;return;
  }
  string map=UPlayGround.Manager.SceneManager.Instance.CurrentMapID;
  var position=player.transform.position;
  bool barrier=region.EntryBarrier!=null&&region.EntryBarrier.activeSelf;
  rows.Add(region.MapId+" enteredMap="+map+" position="+position+" grounded="+player.ActorController.Motor.GroundingStatus.IsStableOnGround+" barrier="+barrier+" canSave="+world.CanSave);
  if(map!=region.MapId||Vector3.Distance(position,region.NeighborEntryPosition)>3||barrier||!world.CanSave)throw new Exception(region.MapId+" 실제 진입 계약 실패");
  index++;
  if(index==world.Regions.Length){rows.Add("RESULT=PASS");finished=true;}
  else{stage=0;next=EditorApplication.timeSinceStartup+.5;}
 }catch(Exception e){rows.Add("RESULT=FAIL "+e);finished=true;}
 finally{
  if(finished){
   EditorApplication.update-=tick;
   if(EditorApplication.isPlaying&&player!=null){player.ActorController.Motor.SetPosition(originalPosition);world.RestoreLocation(originalMap,player);}
   System.IO.File.WriteAllLines(folder+"/region-entry.txt",rows);
  }
 }
};
EditorApplication.update+=tick;
return "세 지역의 로드·진입 검사 시작";
