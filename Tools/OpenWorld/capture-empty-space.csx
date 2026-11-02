var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("월드 씬 필요");
var folder="output/OpenWorld/EmptySpace/captures";System.IO.Directory.CreateDirectory(folder);
var utility=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UPlayGround.Tool.Editor.MinimapCaptureUtility")).First(t=>t!=null);
var go=new GameObject("EmptySpaceCapture"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();camera.enabled=false;camera.scene=scene;camera.fieldOfView=65;camera.farClipPlane=4000;camera.nearClipPlane=.1f;
float[,] points={{1185,1400},{544,1048},{1480,1020},{1870,585},{2080,200},{850,-290}};
try{
 for(int mode=0;mode<2;mode++)for(int i=0;i<6;i++){
  var p=new Vector3(points[i,0],0,points[i,1]);p.y=Terrain.activeTerrain.SampleHeight(p);
  var target=p;camera.transform.position=p+new Vector3(60,65,-70);
  if(mode==1){camera.transform.position=new Vector3(p.x,Terrain.activeTerrain.SampleHeight(p+Vector3.back*24)+1.7f,p.z-24);target.y+=1.5f;}
  camera.transform.rotation=Quaternion.LookRotation(target-camera.transform.position);
  var texture=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(1280,800),mode==0,1000f});
  try{System.IO.File.WriteAllBytes(folder+"/"+(mode==0?"zone":"ground")+(i+1)+".png",texture.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(texture);}
 }
 camera.orthographic=true;camera.orthographicSize=1400;camera.transform.position=new Vector3(1200,2400,700);camera.transform.rotation=Quaternion.Euler(90,0,0);
 var map=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(1200,1400),true,1000f});
 try{System.IO.File.WriteAllBytes(folder+"/world.png",map.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(map);}
}finally{UnityEngine.Object.DestroyImmediate(go);}
return folder;
