var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("월드 Edit Mode가 필요합니다.");
var terrain=GameObject.Find("AuthoredEnvironment").GetComponentInChildren<Terrain>();
string folder="output/OpenWorld/WaterLandscape";System.IO.Directory.CreateDirectory(folder);
var utility=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UPlayGround.Tool.Editor.MinimapCaptureUtility")).First(t=>t!=null);
var pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;var mode=pipeline.GetType().GetProperty("gpuResidentDrawerMode");var saved=mode.GetValue(pipeline,null);
var go=new GameObject("WaterLandscapeCapture"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();camera.enabled=false;camera.scene=scene;camera.useOcclusionCulling=false;camera.fieldOfView=65;camera.farClipPlane=6000;camera.nearClipPlane=.08f;
Action<string,bool> capture=(name,force)=>{var tex=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(1400,900),force,1000f});try{System.IO.File.WriteAllBytes(folder+"/"+name+".png",tex.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(tex);}};
try {
    mode.SetValue(pipeline,Enum.Parse(mode.PropertyType,"Disabled"),null);
    camera.transform.SetPositionAndRotation(new Vector3(1300,48.5f,-150),Quaternion.LookRotation(new Vector3(35,-2.6f,-70)));
    capture("water-above",false);
    camera.transform.SetPositionAndRotation(new Vector3(1300,44.6f,-150),Quaternion.LookRotation(new Vector3(15,4,-30)));
    capture("water-below",false);
    foreach(var point in new[]{new Vector3(440,0,-280),new Vector3(1120,0,-50)}) {
        var p=point;
        p.y=terrain.SampleHeight(p)+terrain.transform.position.y;
        camera.transform.position=p+new Vector3(0,1.7f,0);camera.transform.rotation=Quaternion.Euler(0,320,0);
        capture(p.x<500?"west-ground":"river-ground",false);
    }
    camera.orthographic=true;camera.orthographicSize=800;camera.transform.SetPositionAndRotation(new Vector3(1150,2000,-30),Quaternion.Euler(90,0,0));
    capture("after-south",true);
}finally{UnityEngine.Object.DestroyImmediate(go);mode.SetValue(pipeline,saved,null);}
return folder;
