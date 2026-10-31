var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!=UPlayGround.Editor.OpenWorldSliceAuthoring.HostPath || EditorApplication.isPlaying)throw new Exception("생성 월드 Edit Mode 필요");
var phase=System.IO.File.ReadAllText("output/OpenWorld/Exploration/Iteration05/capture-phase.txt").Trim();
var folder="output/OpenWorld/Exploration/Iteration05/"+phase;
System.IO.Directory.CreateDirectory(folder);
var terrain=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>()).Single();
Func<float,float,float> height=(x,z)=>terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y;
var names=new[]{"entry","choice","approach","view","return","descent","descent-return","shelf","shelf-return","atlas"};
var coordinates=new[]{new Vector3(800,0,1230),new Vector3(1040,0,1410),new Vector3(1200,0,1320),new Vector3(1310,0,1310),new Vector3(1360,0,1420),new Vector3(1540,0,1370),new Vector3(1620,0,1210),new Vector3(1225,0,1265),new Vector3(1225,0,1265),new Vector3(1200,3800,700)};
var directions=new[]{new Vector3(1,0,.8f),new Vector3(1,-.05f,-.5f),new Vector3(1,-.05f,-.1f),new Vector3(-.5f,-.14f,-1),new Vector3(1,-.03f,-.3f),new Vector3(.5f,-.12f,-1),new Vector3(-.5f,.08f,1),new Vector3(-.35f,-.17f,-1),new Vector3(1,.08f,.5f),Vector3.down};
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
