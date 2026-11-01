var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var folder="output/OpenWorld/HarborArtRevision/captures";System.IO.Directory.CreateDirectory(folder);
var names=new[]{"01-overview","02-market","03-waterfront","04-lighthouse","05-entry"};
var positions=new[]{new Vector3(1815,146,-430),new Vector3(1920,54,-271),new Vector3(1898,54,-327),new Vector3(1945,56,-354),new Vector3(1920,54,-226)};
var targets=new[]{new Vector3(1923,50,-294),new Vector3(1920,53,-300),new Vector3(1940,53,-328),new Vector3(1972,57,-347),new Vector3(1920,52,-280)};
var utility=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UPlayGround.Tool.Editor.MinimapCaptureUtility")).First(t=>t!=null);
var go=new GameObject("HarborCapture"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();camera.enabled=false;camera.scene=scene;camera.fieldOfView=60;camera.nearClipPlane=.1f;camera.farClipPlane=1000;
try{for(int i=0;i<names.Length;i++){
 camera.transform.position=positions[i];camera.transform.rotation=Quaternion.LookRotation(targets[i]-positions[i]);
 var texture=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(1440,1000),true,1000f});
 try{System.IO.File.WriteAllBytes(folder+"/"+names[i]+".png",texture.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(texture);}
}}finally{UnityEngine.Object.DestroyImmediate(go);}return folder;
