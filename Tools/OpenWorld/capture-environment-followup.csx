// 전후 촬영은 동일한 지면 기준 눈높이와 방향을 사용한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("월드 Edit Mode 필요");
string folder="output/OpenWorld/EnvironmentFollowup/"+(System.IO.File.Exists("output/OpenWorld/EnvironmentFollowup/applied.txt")?"after":"before");System.IO.Directory.CreateDirectory(folder);
var terrain=GameObject.Find("AuthoredEnvironment").GetComponentInChildren<Terrain>();
var utility=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UPlayGround.Tool.Editor.MinimapCaptureUtility")).First(t=>t!=null);
var pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;var mode=pipeline.GetType().GetProperty("gpuResidentDrawerMode");var previous=mode.GetValue(pipeline,null);
var go=new GameObject("FollowupCapture"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();camera.enabled=false;camera.scene=scene;camera.useOcclusionCulling=false;camera.fieldOfView=65;camera.farClipPlane=1500;camera.nearClipPlane=.1f;
string[] names={"quarry-face","old-pass","quarry-yard","crypt","fort","waystation","harbor-quay","harbor-lighthouse","citadel-market","citadel-workshop","citadel-gate"};
float[,] positions={{558,1120},{560,1330},{549,1070},{1525,1048},{1845,590},{2022,152},{1920,-320},{1979,-338},{580,165},{554,87},{580,234}};
float[,] targets={{380,1180},{440,1380},{520,1100},{1509,1067},{1820,620},{2045,180},{1884,-334},{1971,-351},{565,125},{532,81},{580,205}};
try{mode.SetValue(pipeline,Enum.Parse(mode.PropertyType,"Disabled"),null);
for(int i=0;i<names.Length;i++){
var p=new Vector3(positions[i,0],0,positions[i,1]);p.y=terrain.SampleHeight(p)+terrain.transform.position.y+1.7f;
var target=new Vector3(targets[i,0],0,targets[i,1]);target.y=terrain.SampleHeight(target)+terrain.transform.position.y+2;
if(i==6){p.y=51.7f;target.y=51.5f;}if(i==7){p.y=51.7f;target.y=54f;}
camera.transform.SetPositionAndRotation(p,Quaternion.LookRotation(target-p));
var texture=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(1100,700),false,1000f});
try{System.IO.File.WriteAllBytes(folder+"/"+names[i]+".png",texture.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(texture);}
}
}finally{UnityEngine.Object.DestroyImmediate(go);mode.SetValue(pipeline,previous,null);}return folder;
