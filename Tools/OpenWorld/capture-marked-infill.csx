var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("월드 Edit Mode 필요");
var terrain=GameObject.Find("AuthoredEnvironment").GetComponentInChildren<Terrain>();
string folder="output/OpenWorld/MarkedInfill/captures";System.IO.Directory.CreateDirectory(folder);
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/marked-infill-plan.json"),typeof(Dictionary<string,object>));
var sites=(Dictionary<string,object>[])read(plan["sites"].ToString(),typeof(Dictionary<string,object>[]));
var utility=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UPlayGround.Tool.Editor.MinimapCaptureUtility")).First(t=>t!=null);
var pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;var mode=pipeline.GetType().GetProperty("gpuResidentDrawerMode");var saved=mode.GetValue(pipeline,null);
var go=new GameObject("MarkedInfillCapture"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();camera.enabled=false;camera.scene=scene;camera.useOcclusionCulling=false;camera.fieldOfView=68;camera.farClipPlane=4000;camera.nearClipPlane=.1f;
try {
    mode.SetValue(pipeline,Enum.Parse(mode.PropertyType,"Disabled"),null);
    foreach(var site in sites) {
        var c=(float[])read(site["center"].ToString(),typeof(float[]));var p=new Vector3(c[0],0,c[1]);p.y=terrain.SampleHeight(p)+terrain.transform.position.y;
        camera.transform.position=p+new Vector3(90,100,-115);camera.transform.rotation=Quaternion.LookRotation(p-camera.transform.position);
        var texture=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(1200,850),true,1000f});
        try{System.IO.File.WriteAllBytes(folder+"/"+site["id"]+".png",texture.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(texture);}
        var entry=(float[])read(site["entry"].ToString(),typeof(float[]));var target=(float[])read(site["target"].ToString(),typeof(float[]));
        var start=new Vector3(entry[0],0,entry[1]);start.y=terrain.SampleHeight(start)+terrain.transform.position.y+1.7f;
        var look=new Vector3(target[0],0,target[1]);look.y=terrain.SampleHeight(look)+terrain.transform.position.y+1.5f;
        camera.transform.SetPositionAndRotation(start,Quaternion.LookRotation(look-start));
        texture=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(1200,800),false,1000f});
        try{System.IO.File.WriteAllBytes(folder+"/"+site["id"]+"-entry.png",texture.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(texture);}
    }
    float[,] closeViews={{1410,1498,1435,1510},{420,846,400,854},{1385,1098,1382,1118},{430,235,430,300},{1400,-103,1410,-76}};
    for(int i=0;i<sites.Length;i++) {
        var p=new Vector3(closeViews[i,0],0,closeViews[i,1]);p.y=terrain.SampleHeight(p)+terrain.transform.position.y+1.7f;
        var target=new Vector3(closeViews[i,2],0,closeViews[i,3]);target.y=terrain.SampleHeight(target)+terrain.transform.position.y+1.4f;
        camera.transform.SetPositionAndRotation(p,Quaternion.LookRotation(target-p));
        var texture=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(1200,800),false,1000f});
        try{System.IO.File.WriteAllBytes(folder+"/"+sites[i]["id"]+"-close.png",texture.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(texture);}
    }
    camera.orthographic=true;camera.orthographicSize=1400;camera.transform.SetPositionAndRotation(new Vector3(1200,2400,700),Quaternion.Euler(90,0,0));
    var map=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(1200,1400),true,1000f});
    try{System.IO.File.WriteAllBytes(folder+"/world.png",map.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(map);}
}finally{UnityEngine.Object.DestroyImmediate(go);mode.SetValue(pipeline,saved,null);}
return folder;
