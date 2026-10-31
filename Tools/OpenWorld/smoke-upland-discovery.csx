if(!EditorApplication.isPlaying)throw new Exception("Play Mode 필요");
var continuous=UPlayGround.Manager.World.ContinuousWorldController.Active;
var player=UPlayGround.Manager.PartyManager.Instance?.ActiveCharacter;
if(continuous==null||player==null||!continuous.IsInitialized||continuous.IsTravelling||UPlayGround.Manager.CameraManager.Instance?.IsSceneCameraReady!=true)throw new Exception("월드·카메라·플레이어 준비 대기");
var folder="output/OpenWorld/Exploration/Iteration05/play";System.IO.Directory.CreateDirectory(folder);
var original=player.transform.position;var originalRotation=player.transform.rotation;
var originalMap=UPlayGround.Manager.SceneManager.Instance.CurrentMapID;
var terrain=UnityEngine.Object.FindFirstObjectByType<Terrain>();
var points=new[]{new Vector3(800,0,1230),new Vector3(1040,0,1410),new Vector3(1080,0,1370),new Vector3(1145,0,1300),new Vector3(1225,0,1265),new Vector3(1310,0,1310),new Vector3(1400,0,1365),new Vector3(1360,0,1420),new Vector3(1490,0,1330),new Vector3(1580,0,1220),new Vector3(1630,0,1100)};
var rows=new List<string>{"UTC="+DateTime.UtcNow.ToString("O"),"실제 Play Mode 위치 이동 API·KCC 접지·저장 높이 복구 계약. 보행 입력·파일 저장·사망 실행 아님"};
System.IO.File.WriteAllText(folder+"/upland-runtime.txt","RUNNING "+DateTime.UtcNow.ToString("O"));
int index=0;bool waiting=false;double start=EditorApplication.timeSinceStartup,next=start;
EditorApplication.CallbackFunction tick=null;
tick=()=>{
 bool finished=false;
 try{
  if(!EditorApplication.isPlaying||player==null||EditorApplication.timeSinceStartup-start>45)throw new Exception("검사 중단/시간 초과");
  if(EditorApplication.timeSinceStartup<next)return;
  var p=points[index];p.y=terrain.SampleHeight(p)+terrain.transform.position.y+.1f;
  if(!waiting){player.ActorController.Motor.SetPosition(p);continuous.RestoreLocation(continuous.ResidentMapId,player);waiting=true;next=EditorApplication.timeSinceStartup+1.3;return;}
  bool grounded=player.ActorController.Motor.GroundingStatus.IsStableOnGround;
  bool near=Vector3.Distance(player.transform.position,p)<2;
  Vector3 above,below;
  bool high=continuous.TryResolveSurfacePosition(p+Vector3.up*20,out above),low=continuous.TryResolveSurfacePosition(p-Vector3.up*20,out below);
  bool restored=high&&low&&Mathf.Abs(above.y-p.y)<1&&Mathf.Abs(below.y-p.y)<1;
  rows.Add(index+" position="+player.transform.position+" stable="+grounded+" near="+near+" saveHeightRecovery="+restored+" canSave="+continuous.CanSave);
  if(!grounded||!near||!restored||!continuous.CanSave)throw new Exception("고원 접지/복구 계약 실패 "+index);
  index++;waiting=false;
  if(index==points.Length){rows.Add("RESULT=PASS points="+index);finished=true;}
 }catch(Exception e){rows.Add("RESULT=FAIL "+e);finished=true;}
 finally{
  if(finished){EditorApplication.update-=tick;if(EditorApplication.isPlaying&&player!=null){player.ActorController.Motor.SetPosition(original);player.ActorController.Motor.SetRotation(originalRotation);continuous.RestoreLocation(originalMap,player);}System.IO.File.WriteAllLines(folder+"/upland-runtime.txt",rows);}
 }
};
EditorApplication.update+=tick;
return "고원 11지점 API 검사 시작; upland-runtime.txt의 RESULT 확인 후 후속 요청";
