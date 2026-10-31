var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!=UPlayGround.Editor.OpenWorldSliceAuthoring.HostPath || EditorApplication.isPlaying)throw new Exception("생성 월드 Edit Mode 필요");
var phase=System.IO.File.ReadAllText("output/OpenWorld/Exploration/Iteration04/capture-phase.txt").Trim();
var folder="output/OpenWorld/Exploration/Iteration04/"+phase;
System.IO.Directory.CreateDirectory(folder);
var terrain=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>()).Single();
Func<float,float,float> height=(x,z)=>terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y;
var names=new[]{"entry","decision","approach","activity","return","clearing","north","atlas"};
var coordinates=new[]{new Vector3(1540,0,510),new Vector3(1700,0,740),new Vector3(1640,0,760),new Vector3(1609,0,801),new Vector3(1625,0,825),new Vector3(1750,0,870),new Vector3(1680,0,1030),new Vector3(1200,3800,700)};
var directions=new[]{new Vector3(1,0,1),new Vector3(-1,0,.22f),new Vector3(-.6f,0,1),new Vector3(-1,-.18f,1),new Vector3(1,.08f,.5f),new Vector3(-.4f,0,1),new Vector3(-.5f,0,1),Vector3.down};
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
