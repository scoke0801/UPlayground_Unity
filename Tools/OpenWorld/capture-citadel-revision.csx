var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var folder="output/OpenWorld/CitadelArtRevision/captures";System.IO.Directory.CreateDirectory(folder);
var names=new[]{"01-overview","02-keep-city-stairs","03-keep-outside","04-market-street","05-gate","06-wall-corner","07-west-lane"};
var positions=new[]{new Vector3(580,159,280),new Vector3(580,70,88),new Vector3(580,75,-21),new Vector3(580,65,165),new Vector3(580,65,233),new Vector3(527,77,198),new Vector3(556,65,178)};
var targets=new[]{new Vector3(580,65,116),new Vector3(580,76,30),new Vector3(580,78,27),new Vector3(575,65,116),new Vector3(580,70,205),new Vector3(512,73,216),new Vector3(548,65,140)};
var utility=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UPlayGround.Tool.Editor.MinimapCaptureUtility")).First(t=>t!=null);
var go=new GameObject("CitadelCapture"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();camera.enabled=false;camera.scene=scene;camera.fieldOfView=60;camera.nearClipPlane=.1f;camera.farClipPlane=1200;
try{for(int i=0;i<names.Length;i++){
 camera.transform.position=positions[i];camera.transform.rotation=Quaternion.LookRotation(targets[i]-positions[i]);
 var texture=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(1440,1000),true,1000f});
 try{System.IO.File.WriteAllBytes(folder+"/"+names[i]+".png",texture.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(texture);}
}}finally{UnityEngine.Object.DestroyImmediate(go);}
return folder;
