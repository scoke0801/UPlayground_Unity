var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("월드 Edit Mode 필요");
string stage=System.IO.File.Exists("output/OpenWorld/EnvironmentPolish/applied.txt")?"after":"before";
string folder="output/OpenWorld/EnvironmentPolish/"+stage;System.IO.Directory.CreateDirectory(folder);
var terrain=GameObject.Find("AuthoredEnvironment").GetComponentInChildren<Terrain>();
var utility=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UPlayGround.Tool.Editor.MinimapCaptureUtility")).First(t=>t!=null);
var pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;var mode=pipeline.GetType().GetProperty("gpuResidentDrawerMode");var previous=mode.GetValue(pipeline,null);
var go=new GameObject("EnvironmentPolishCapture"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();camera.enabled=false;camera.scene=scene;camera.useOcclusionCulling=false;camera.fieldOfView=65;camera.farClipPlane=1800;camera.nearClipPlane=.1f;
string[] names={"quarry","crypt","fort","waystation","shore","queen-tunnel","spider-tunnel"};
float[,] positions={{575,1050},{1590,1060},{1845,565},{2018,120},{1100,808},{770,865},{785,605}};
float[,] targets={{542,1090},{1525,1075},{1845,615},{2025,175},{1100,845},{810,865},{825,605}};
try{
mode.SetValue(pipeline,Enum.Parse(mode.PropertyType,"Disabled"),null);
for(int i=0;i<names.Length;i++){
var p=new Vector3(positions[i,0],0,positions[i,1]);p.y=terrain.SampleHeight(p)+terrain.transform.position.y+2.2f;
var target=new Vector3(targets[i,0],0,targets[i,1]);target.y=terrain.SampleHeight(target)+terrain.transform.position.y+2;
if(i>=5){p.y=i==5?94:76.5f;target.y=p.y;}
camera.transform.SetPositionAndRotation(p,Quaternion.LookRotation(target-p));
var texture=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(1100,700),false,1000f});
try{System.IO.File.WriteAllBytes(folder+"/"+names[i]+".png",texture.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(texture);}
}
}finally{UnityEngine.Object.DestroyImmediate(go);mode.SetValue(pipeline,previous,null);}
return folder;
