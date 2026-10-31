if(!EditorApplication.isPlaying)throw new Exception("Play Mode 필요");
var folder="output/OpenWorld/Exploration/Iteration04/play";
System.IO.Directory.CreateDirectory(folder);
var world=UPlayGround.Manager.WorldStateManager.Instance;
var inventory=UPlayGround.Manager.InventoryManager.Instance;
var persistence=UPlayGround.Manager.InteractionRespawnManager.Instance;
var continuous=UPlayGround.Manager.World.ContinuousWorldController.Active;
var player=UPlayGround.Manager.PartyManager.Instance?.ActiveCharacter;
if(continuous==null||player==null||world==null||inventory==null||persistence==null||!continuous.IsInitialized||continuous.IsTravelling||UPlayGround.Manager.CameraManager.Instance?.IsSceneCameraReady!=true)throw new Exception("월드 준비 대기");
var target=UnityEngine.Object.FindObjectsByType<UPlayGround.GatheringActor>(FindObjectsInactive.Include,FindObjectsSortMode.None)
 .Single(a=>a.GetComponent<UPlayGround.Components.SceneEntityId>()?.Guid=="c42bb9f480ec4f3cacdf786ab0a59e21");
var before=new UPlayGround.Data.Save.GameSaveData();world.ExportSaveData(before);inventory.ExportSaveData(before);
var data=target.GetData();int itemId=data.dropItems.Single().itemData.itemId;int countBefore=inventory.GetItemCount(itemId);
var oldPosition=player.transform.position;string oldMap=UPlayGround.Manager.SceneManager.Instance.CurrentMapID;
var terrain=UnityEngine.Object.FindFirstObjectByType<Terrain>();
var p=new Vector3(1607,0,805);p.y=terrain.SampleHeight(p)+terrain.transform.position.y+.1f;
player.ActorController.Motor.SetPosition(p);continuous.RestoreLocation(continuous.ResidentMapId,player);
var rows=new List<string>{"UTC="+DateTime.UtcNow.ToString("O"),"실제 Play Mode: 위치/상태 전이 API로 기존 벌목 모션 실행. OnHit 직접 주입 없음. 실제 입력·파일 재시작과 구분."};
System.IO.File.WriteAllText(folder+"/runtime-contract.txt","RUNNING "+DateTime.UtcNow.ToString("O"));
Action<string> capture=name=>{
 var cameraObject=new GameObject("ForestRuntimeCapture"){hideFlags=HideFlags.HideAndDontSave};
 try{
  var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=65;camera.farClipPlane=3000;camera.nearClipPlane=.1f;camera.scene=target.gameObject.scene;
  var position=new Vector3(1609,0,801);position.y=terrain.SampleHeight(position)+terrain.transform.position.y+1.7f;
  camera.transform.position=position;camera.transform.rotation=Quaternion.LookRotation(new Vector3(-1,-.18f,1));
  var utility=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UPlayGround.Tool.Editor.MinimapCaptureUtility")).First(t=>t!=null);
  var texture=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(1280,800),false,1000f});
  try{System.IO.File.WriteAllBytes(folder+"/"+name+".png",texture.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(texture);}
 }finally{UnityEngine.Object.DestroyImmediate(cameraObject);}
};
int stage=0;double started=EditorApplication.timeSinceStartup,next=started+2;
EditorApplication.CallbackFunction tick=null;
tick=()=>{
 bool finish=false;
 try{
  if(!EditorApplication.isPlaying||player==null||EditorApplication.timeSinceStartup-started>35)throw new Exception("벌목 모션 검사 중단/시간 초과");
  if(EditorApplication.timeSinceStartup<next)return;
  if(stage==0){
   rows.Add("GROUND="+player.ActorController.Motor.GroundingStatus.IsStableOnGround+" position="+player.transform.position);
   if(!player.ActorController.Motor.GroundingStatus.IsStableOnGround)throw new Exception("접지 실패");
   var handler=UPlayGround.Manager.GameObjectManager.Instance.InteractionHandler;
   rows.Add("DETECTED="+(handler.CurrentClosestInteractable==target));
   if(handler.CurrentClosestInteractable!=target)throw new Exception("실제 탐색 핸들러가 목재를 발견하지 못함");
   target.Interact(player);target.StopInteract();
   if(!target.CanInteract()||inventory.GetItemCount(itemId)!=countBefore)throw new Exception("취소 시 상태/수량 변경");
   rows.Add("CANCEL=PASS");capture("action-before");
   player.ActorController.TransitionToState(new UPlayGround.State.PlayerInteractionState(player.ActorController));
   stage=1;next=EditorApplication.timeSinceStartup+.2;return;
  }
  if(target.gameObject.activeSelf)return;
  rows.Add("MOTION_COLLECT="+(inventory.GetItemCount(itemId)==countBefore+1)+" inventory="+countBefore+"->"+inventory.GetItemCount(itemId));
  if(inventory.GetItemCount(itemId)!=countBefore+1)throw new Exception("실제 모션 채집 수량 실패");
  capture("action-after");
  var after=new UPlayGround.Data.Save.GameSaveData();world.ExportSaveData(after);inventory.ExportSaveData(after);
  if(!world.IsInteractableConsumed("LakeOfLifeScenario",target.GetComponent<UPlayGround.Components.SceneEntityId>().Guid))throw new Exception("소비 소유권 실패");
  world.ImportSaveData(before);inventory.ImportSaveData(before);
  if(!target.gameObject.activeSelf||!target.CanInteract()||inventory.GetItemCount(itemId)!=countBefore)throw new Exception("획득 전 복구 실패");
  rows.Add("IMPORT_BEFORE=PASS");
  world.ImportSaveData(after);inventory.ImportSaveData(after);
  if(target.gameObject.activeSelf||inventory.GetItemCount(itemId)!=countBefore+1)throw new Exception("획득 후 복구 실패");
  rows.Add("IMPORT_AFTER=PASS");
  persistence.RespawnConsumedInteractables("LakeOfLifeScenario");
  if(!target.gameObject.activeSelf||!target.CanInteract())throw new Exception("명시적 리스폰 실패");
  rows.Add("EXPLICIT_RESPAWN=PASS");rows.Add("RESULT=PASS");finish=true;
 }catch(Exception e){rows.Add("RESULT=FAIL "+e);finish=true;}
 finally{
  if(finish){EditorApplication.update-=tick;if(EditorApplication.isPlaying){player.ActorController.TransitionToState(UPlayGround.State.ActorStateId.Idle);world.ImportSaveData(before);inventory.ImportSaveData(before);player.ActorController.Motor.SetPosition(oldPosition);continuous.RestoreLocation(oldMap,player);}System.IO.File.WriteAllLines(folder+"/runtime-contract.txt",rows);}
 }
};
EditorApplication.update+=tick;
return "검사 시작; 완료 기록 확인 후에만 재실행";


