var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("월드 씬 필요");
var folder="output/OpenWorld/LandmarkSites/captures";System.IO.Directory.CreateDirectory(folder);
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/landmark-sites-plan.json"),typeof(Dictionary<string,object>));var sites=(Dictionary<string,object>[])read(plan["sites"].ToString(),typeof(Dictionary<string,object>[]));
var utility=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UPlayGround.Tool.Editor.MinimapCaptureUtility")).First(t=>t!=null);
// 삭제된 식생의 GPU 드로우 캐시가 정지 캡처에 남는 것을 피하고 촬영 뒤 원래 모드로 복원한다.
var pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;var gpuMode=pipeline.GetType().GetProperty("gpuResidentDrawerMode");var savedMode=gpuMode.GetValue(pipeline,null);
var go=new GameObject("LandmarkCapture"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();camera.enabled=false;camera.scene=scene;camera.fieldOfView=65;camera.farClipPlane=4000;camera.nearClipPlane=.1f;
try{
 gpuMode.SetValue(pipeline,Enum.Parse(gpuMode.PropertyType,"Disabled"),null);
 foreach(var site in sites){var p=new Vector3(Convert.ToSingle(site["x"]),0,Convert.ToSingle(site["z"]));p.y=Terrain.activeTerrain.SampleHeight(p);float distance=Convert.ToSingle(site["w"])*.8f;
  camera.transform.position=p+new Vector3(distance*.5f,distance,-distance);camera.transform.rotation=Quaternion.LookRotation(p-camera.transform.position);
  var texture=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(1280,900),true,1000f});try{System.IO.File.WriteAllBytes(folder+"/"+site["id"]+".png",texture.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(texture);}
  var entry=(float[])read(site["entry"].ToString(),typeof(float[]));var pos=new Vector3(entry[0],0,entry[1]);pos.y=Terrain.activeTerrain.SampleHeight(pos)+1.7f;camera.transform.position=pos;camera.transform.rotation=Quaternion.LookRotation(p+Vector3.up*5-pos);
  texture=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(1280,800),false,1000f});try{System.IO.File.WriteAllBytes(folder+"/"+site["id"]+"-entry.png",texture.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(texture);}
 }
 camera.orthographic=true;camera.orthographicSize=1400;camera.transform.position=new Vector3(1200,2400,700);camera.transform.rotation=Quaternion.Euler(90,0,0);
 var map=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(1200,1400),true,1000f});try{System.IO.File.WriteAllBytes(folder+"/world.png",map.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(map);}
}finally{UnityEngine.Object.DestroyImmediate(go);gpuMode.SetValue(pipeline,savedMode,null);}return folder;
