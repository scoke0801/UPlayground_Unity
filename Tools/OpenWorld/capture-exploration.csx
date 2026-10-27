var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!=UPlayGround.Editor.OpenWorldSliceAuthoring.HostPath || EditorApplication.isPlaying)throw new Exception("생성 월드 Edit Mode 필요");
var phase=System.IO.File.ReadAllText("output/OpenWorld/Exploration/capture-phase.txt").Trim();
var folder="output/OpenWorld/Exploration/"+phase;
System.IO.Directory.CreateDirectory(folder);
var terrain=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>()).Single();
Func<float,float,float> height=(x,z)=>terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y;
var names=new[]{"01-trade","01-valley","02-entrance","02-court","03-entry","03-deep","03-clearing","04-approach","04-meadow","04-lake-view","05-valley","05-bend","05-saddle","02-archive","02-heart","02-return","atlas"};
var coordinates=new[]{new Vector3(800,0,220),new Vector3(1100,0,155),new Vector3(650,0,478),new Vector3(650,0,532),new Vector3(1540,0,510),new Vector3(1700,0,740),new Vector3(1750,0,870),new Vector3(800,0,1230),new Vector3(1040,0,1410),new Vector3(1310,0,1310),new Vector3(460,0,540),new Vector3(450,0,760),new Vector3(650,0,1170),new Vector3(610,4.14f,548),new Vector3(650,0.14f,584),new Vector3(682,0.14f,548),new Vector3(1200,3800,700)};
var directions=new[]{new Vector3(1,0,-0.2f),new Vector3(1,0,-0.25f),new Vector3(0,0,1),new Vector3(0,0,1),new Vector3(1,0,1),new Vector3(0.4f,0,1),new Vector3(-0.4f,0,1),new Vector3(1,0,0.8f),new Vector3(1,0,0.15f),new Vector3(-0.5f,-0.14f,-1),new Vector3(-0.5f,0,1),new Vector3(1,0.05f,1),new Vector3(1,0,0.4f),new Vector3(-0.6f,0,1),Vector3.forward,Vector3.back,Vector3.down};
var go=new GameObject("ExplorationCapture"){hideFlags=HideFlags.HideAndDontSave};
var camera=go.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=65;camera.farClipPlane=7000;camera.nearClipPlane=0.1f;camera.scene=scene;camera.clearFlags=CameraClearFlags.Skybox;
var utility=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UPlayGround.Tool.Editor.MinimapCaptureUtility")).First(t=>t!=null);
var lines=new System.Collections.Generic.List<string>{"UTC="+DateTime.UtcNow.ToString("O"),"동일 XZ·방향·FOV; 현재 지면+1.7m. Editor 정지 카메라, 실제 플레이 아님. 촬영 유틸리티 전역 안개 OFF. atlas만 LOD0 강제."};
try{
 for(int i=0;i<names.Length;i++){
  bool atlas=names[i]=="atlas";
  var p=coordinates[i];if(!atlas)p.y+=height(p.x,p.z)+1.7f;
  camera.transform.position=p;camera.transform.rotation=atlas?Quaternion.Euler(90,0,0):Quaternion.LookRotation(directions[i]);camera.orthographic=atlas;camera.orthographicSize=1480;
  var texture=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,atlas?new Vector2Int(1200,1400):new Vector2Int(1280,800),atlas,1000f});
  try{System.IO.File.WriteAllBytes(folder+"/"+names[i]+".png",texture.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(texture);}
  lines.Add(names[i]+" position="+p.ToString("F3")+" direction="+directions[i]+" FOV=65");
 }
}finally{UnityEngine.Object.DestroyImmediate(go);}
System.IO.File.WriteAllLines(folder+"/cameras.txt",lines);
return "이번 실행 촬영: "+folder;
