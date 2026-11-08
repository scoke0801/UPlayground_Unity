// 전후 촬영은 동일한 지면 기준 눈높이와 방향을 사용한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("월드 Edit Mode 필요");
string folder="output/OpenWorld/EnvironmentForm/"+(System.IO.File.Exists("output/OpenWorld/EnvironmentForm/applied.txt")?"after":"before");System.IO.Directory.CreateDirectory(folder);
var terrain=GameObject.Find("AuthoredEnvironment").GetComponentInChildren<Terrain>();
var utility=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UPlayGround.Tool.Editor.MinimapCaptureUtility")).First(t=>t!=null);
var pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;var mode=pipeline.GetType().GetProperty("gpuResidentDrawerMode");var previous=mode.GetValue(pipeline,null);
var go=new GameObject("FollowupCapture"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();camera.enabled=false;camera.scene=scene;camera.useOcclusionCulling=false;camera.fieldOfView=65;camera.farClipPlane=1500;camera.nearClipPlane=.1f;
string[] names={"fort-entry","fort-west","fort-east","station-entry","station-west","station-east","station-fire"};
float[,] positions={{1845,575},{1825,611},{1867,611},{2025,142},{2001,173},{2053,172},{2025,199}};
float[,] targets={{1845,610},{1819,628},{1874,628},{2048,177},{1990,178},{2060,186},{2025,207}};
try{mode.SetValue(pipeline,Enum.Parse(mode.PropertyType,"Disabled"),null);
for(int i=0;i<names.Length;i++){
var p=new Vector3(positions[i,0],0,positions[i,1]);p.y=terrain.SampleHeight(p)+terrain.transform.position.y+1.7f;
var target=new Vector3(targets[i,0],0,targets[i,1]);target.y=terrain.SampleHeight(target)+terrain.transform.position.y+2;

string posePath="output/OpenWorld/EnvironmentForm/pose-"+names[i]+".txt";
Quaternion rotation=Quaternion.LookRotation(target-p);
if(System.IO.File.Exists(posePath)){var pose=System.IO.File.ReadAllText(posePath).Split('|').Select(v=>float.Parse(v,System.Globalization.CultureInfo.InvariantCulture)).ToArray();p=new Vector3(pose[0],pose[1],pose[2]);rotation=new Quaternion(pose[3],pose[4],pose[5],pose[6]);}
else{System.IO.File.WriteAllText(posePath,string.Join("|",new[]{p.x,p.y,p.z,rotation.x,rotation.y,rotation.z,rotation.w}.Select(v=>v.ToString("R",System.Globalization.CultureInfo.InvariantCulture)).ToArray()));}
camera.transform.SetPositionAndRotation(p,rotation);
var texture=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(1100,700),false,1000f});
try{System.IO.File.WriteAllBytes(folder+"/"+names[i]+".png",texture.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(texture);}
}
}finally{UnityEngine.Object.DestroyImmediate(go);mode.SetValue(pipeline,previous,null);}return folder;
