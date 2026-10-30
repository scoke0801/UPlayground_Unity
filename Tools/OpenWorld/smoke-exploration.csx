if(!EditorApplication.isPlaying)throw new Exception("Play Mode 필요");
System.IO.Directory.CreateDirectory("output/OpenWorld/Exploration/play");
System.IO.File.WriteAllText("output/OpenWorld/Exploration/play/runtime-contract.txt","RUNNING "+DateTime.UtcNow.ToString("O"));
var player=UPlayGround.Manager.PartyManager.Instance.ActiveCharacter;
var world=UPlayGround.Manager.WorldStateManager.Instance;
var inventory=UPlayGround.Manager.InventoryManager.Instance;
var continuousWorld=UPlayGround.Manager.World.ContinuousWorldController.Active;
if(!continuousWorld.IsInitialized||continuousWorld.IsTravelling||!UPlayGround.Manager.CameraManager.Instance.IsSceneCameraReady)throw new Exception("월드·카메라 준비 대기");
var pickup=UnityEngine.Object.FindObjectsByType<UPlayGround.DropItemActor>(FindObjectsInactive.Include,FindObjectsSortMode.None)
 .Single(a=>a.GetComponent<UPlayGround.Components.SceneEntityId>()!=null&&a.GetComponent<UPlayGround.Components.SceneEntityId>().Guid=="c42bb9f480ec4f3cacdf786ab0a59e02");
var before=new UPlayGround.Data.Save.GameSaveData();world.ExportSaveData(before);inventory.ExportSaveData(before);
int itemId=pickup.ItemData.itemId,countBefore=inventory.GetItemCount(pickup.ItemData.itemId),amount=pickup.Count;
var oldPosition=player.transform.position;string oldMap=UPlayGround.Manager.SceneManager.Instance.CurrentMapID;var terrain=UnityEngine.Object.FindFirstObjectByType<Terrain>();
var rows=new List<string>{"UTC="+DateTime.UtcNow.ToString("O"),"실제 Play Mode 서비스·상호작용 API·메모리 저장 계약 검사. 입력 보행/파일 재시작/전투/리스폰 검사는 아님."};
var points=new[]{new Vector3(740,0,300),new Vector3(800,0,1230)};
for(int d=0;d<2;d++){
 var start=points[d];start.y=terrain.SampleHeight(start);var end=points[1-d];end.y=terrain.SampleHeight(end);
 UnityEngine.AI.NavMeshHit a,b;UnityEngine.AI.NavMesh.SamplePosition(start,out a,3,-1);UnityEngine.AI.NavMesh.SamplePosition(end,out b,3,-1);
 var path=new UnityEngine.AI.NavMeshPath();UnityEngine.AI.NavMesh.CalculatePath(a.position,b.position,-1,path);
 rows.Add("RUNTIME FULL NAV "+d+" "+path.status);if(path.status!=UnityEngine.AI.NavMeshPathStatus.PathComplete)throw new Exception("런타임 장거리 경로 실패");
}
rows.Add("Nav area cost="+UnityEngine.AI.NavMesh.GetAreaCost(3)+" agent costs="+string.Join(",",UnityEngine.Object.FindObjectsByType<UnityEngine.AI.NavMeshAgent>(FindObjectsSortMode.None).Select(a=>a.GetAreaCost(3).ToString()).Distinct().ToArray()));
System.IO.Directory.CreateDirectory("output/OpenWorld/Exploration/play");
player.ActorController.Motor.SetPosition(new Vector3(610,104.1f,550));
continuousWorld.RestoreLocation(continuousWorld.ResidentMapId,player);
int stage=0;double startTime=EditorApplication.timeSinceStartup,next=startTime+2;
float completionGameTime=0;
EditorApplication.CallbackFunction tick=null;
tick=()=>{
 bool finish=false;
 try{
  if(!EditorApplication.isPlaying||player==null||EditorApplication.timeSinceStartup-startTime>40)throw new Exception("런타임 검사 중단 또는 시간 초과");
  if(EditorApplication.timeSinceStartup<next)return;
  if(stage==0){
   rows.Add("ARCHIVE position="+player.transform.position+" grounded="+player.ActorController.Motor.GroundingStatus.IsStableOnGround);
   if(Vector3.Distance(player.transform.position,new Vector3(610,104,550))>3||!player.ActorController.Motor.GroundingStatus.IsStableOnGround)throw new Exception("상층 서고 위치·접지 검증 실패");
   rows.Add("CAPTURE: 별도 실제 런타임 카메라 렌더로 기록; 비활성 Game View의 비동기 캡처는 사용하지 않음");
   pickup.Interact(player);pickup.StopInteract();stage=1;next=EditorApplication.timeSinceStartup+1.5;return;
  }
  if(stage==1){
   bool cancelled=pickup.CanInteract()&&inventory.GetItemCount(itemId)==countBefore;rows.Add("CANCEL="+cancelled);
   if(!cancelled)throw new Exception("보상 상호작용 취소 실패");
   pickup.Interact(player);completionGameTime=Time.time+pickup.InteractionData.interactionCompleteDuration+.25f;stage=2;next=EditorApplication.timeSinceStartup+.1;return;
  }
  if(pickup.IsInteracting()&&Time.time<completionGameTime)return;
  rows.Add("COLLECT_TIMING game="+Time.time+" expected="+completionGameTime+" interacting="+pickup.IsInteracting());
  bool collected=!pickup.gameObject.activeSelf&&inventory.GetItemCount(itemId)==countBefore+amount;
  rows.Add("COLLECT="+collected+" inventory="+countBefore+"->"+inventory.GetItemCount(itemId));if(!collected)throw new Exception("보상 획득 실패");
  var after=new UPlayGround.Data.Save.GameSaveData();world.ExportSaveData(after);inventory.ExportSaveData(after);
  world.ImportSaveData(before);inventory.ImportSaveData(before);
  bool restored=pickup.gameObject.activeSelf&&pickup.CanInteract()&&inventory.GetItemCount(itemId)==countBefore;
  rows.Add("IMPORT_BEFORE="+restored);if(!restored)throw new Exception("획득 전 상태 복원 실패");
  world.ImportSaveData(after);inventory.ImportSaveData(after);
  bool consumed=!pickup.gameObject.activeSelf&&inventory.GetItemCount(itemId)==countBefore+amount;
  rows.Add("IMPORT_AFTER="+consumed);if(!consumed)throw new Exception("획득 후 상태 복원 실패");
  rows.Add("RESULT=PASS");finish=true;
 }catch(Exception e){rows.Add("RESULT=FAIL "+e);finish=true;}
 finally{
  if(finish){EditorApplication.update-=tick;if(EditorApplication.isPlaying){world.ImportSaveData(before);inventory.ImportSaveData(before);if(player!=null){player.ActorController.Motor.SetPosition(oldPosition);continuousWorld.RestoreLocation(oldMap,player);}}System.IO.File.WriteAllLines("output/OpenWorld/Exploration/play/runtime-contract.txt",rows);}
 }
};
EditorApplication.update+=tick;
return "runtime contract check started; output/OpenWorld/Exploration/play/runtime-contract.txt";
