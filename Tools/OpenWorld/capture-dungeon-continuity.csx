var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("LakeVillageWorld Edit Mode가 필요합니다.");
string folder="output/OpenWorld/DungeonContinuity/captures";System.IO.Directory.CreateDirectory(folder);
var terrain=GameObject.Find("AuthoredEnvironment").GetComponentInChildren<Terrain>();
var utility=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UPlayGround.Tool.Editor.MinimapCaptureUtility")).First(t=>t!=null);
var pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;var mode=pipeline.GetType().GetProperty("gpuResidentDrawerMode");var original=mode.GetValue(pipeline,null);
var go=new GameObject("던전 보수 검수 카메라"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();camera.enabled=false;camera.scene=scene;camera.useOcclusionCulling=false;camera.fieldOfView=60;camera.farClipPlane=3000;
string[] names={"corridor","chain","quarry","crypt","fort","waystation"};
float[,] centers={{646,550},{590,860},{542,1090},{1525,1075},{1845,615},{2025,175}};
try{mode.SetValue(pipeline,Enum.Parse(mode.PropertyType,"Disabled"),null);
 for(int i=0;i<names.Length;i++){
  var center=new Vector3(centers[i,0],0,centers[i,1]);center.y=terrain.SampleHeight(center)+terrain.transform.position.y;
  var delta=names[i]=="corridor"?new Vector3(95,115,-110):new Vector3(65,90,-110);
  camera.transform.SetPositionAndRotation(center+delta,Quaternion.LookRotation(-delta));
  var texture=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(1280,900),true,1000f});
  try{System.IO.File.WriteAllBytes(folder+"/"+names[i]+".png",texture.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(texture);}
 }
}finally{UnityEngine.Object.DestroyImmediate(go);mode.SetValue(pipeline,original,null);}
return folder;
