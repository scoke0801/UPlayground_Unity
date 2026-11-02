var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("월드 Edit Mode 필요");
string folder="output/OpenWorld/DistrictInfill/captures";System.IO.Directory.CreateDirectory(folder);
var terrain=GameObject.Find("AuthoredEnvironment").GetComponentInChildren<Terrain>();
var utility=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UPlayGround.Tool.Editor.MinimapCaptureUtility")).First(t=>t!=null);
var pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;var mode=pipeline.GetType().GetProperty("gpuResidentDrawerMode");var saved=mode.GetValue(pipeline,null);
var go=new GameObject("DistrictInfillCapture"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();camera.enabled=false;camera.scene=scene;camera.useOcclusionCulling=false;camera.fieldOfView=68;camera.farClipPlane=4000;camera.nearClipPlane=.1f;
string[] names={"north","west","slope","farm","river"};
float[,] centers={{1430,1515},{400,810},{1350,1100},{405,280},{1430,-130}};
float[,] closeViews={{1390,1490,1430,1555},{505,790,535,840},{1270,1010,1340,1100},{405,285,405,355},{1430,-260,1480,-100}};
try {
    mode.SetValue(pipeline,Enum.Parse(mode.PropertyType,"Disabled"),null);
    for(int i=0;i<5;i++) {
        var p=new Vector3(centers[i,0],0,centers[i,1]);p.y=terrain.SampleHeight(p)+terrain.transform.position.y;
        camera.transform.position=p+new Vector3(180,250,-280);camera.transform.rotation=Quaternion.LookRotation(p-camera.transform.position);
        var texture=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(1200,900),true,1000f});
        try{System.IO.File.WriteAllBytes(folder+"/"+names[i]+".png",texture.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(texture);}
        p=new Vector3(closeViews[i,0],0,closeViews[i,1]);p.y=terrain.SampleHeight(p)+terrain.transform.position.y+1.7f;
        var target=new Vector3(closeViews[i,2],0,closeViews[i,3]);target.y=terrain.SampleHeight(target)+terrain.transform.position.y+1.5f;
        camera.transform.SetPositionAndRotation(p,Quaternion.LookRotation(target-p));
        texture=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(1200,800),false,1000f});
        try{System.IO.File.WriteAllBytes(folder+"/"+names[i]+"-ground.png",texture.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(texture);}
    }
    camera.orthographic=true;camera.orthographicSize=1400;camera.transform.SetPositionAndRotation(new Vector3(1200,2400,700),Quaternion.Euler(90,0,0));
    var map=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(1200,1400),true,1000f});
    try{System.IO.File.WriteAllBytes(folder+"/world.png",map.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(map);}
}finally{UnityEngine.Object.DestroyImmediate(go);mode.SetValue(pipeline,saved,null);}
return folder;
