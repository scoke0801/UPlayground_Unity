var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var folder="output/OpenWorld/FullReplacement/captures";System.IO.Directory.CreateDirectory(folder);
var names=new[]{"castle","village","harbor","hall","temple","ring"};
var positions=new[]{new Vector3(580,122,258),new Vector3(1045,116,485),new Vector3(2015,116,-390),new Vector3(330,106,521),new Vector3(570,160,816),new Vector3(1850,150,960)};
var targets=new[]{new Vector3(580,68,112),new Vector3(966,65,476),new Vector3(1920,50,-288),new Vector3(330,106,580),new Vector3(593,141,860),new Vector3(1904,110,908)};
var utility=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UPlayGround.Tool.Editor.MinimapCaptureUtility")).First(t=>t!=null);
var go=new GameObject("ReplacementCapture"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();camera.enabled=false;camera.scene=scene;camera.fieldOfView=65;camera.farClipPlane=1500;camera.nearClipPlane=.1f;
try{for(int i=0;i<names.Length;i++){
 camera.transform.position=positions[i];camera.transform.rotation=Quaternion.LookRotation(targets[i]-positions[i]);
 var texture=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(1280,800),true,1000f});
 try{System.IO.File.WriteAllBytes(folder+"/"+names[i]+".png",texture.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(texture);}
}}finally{UnityEngine.Object.DestroyImmediate(go);}
return folder;
