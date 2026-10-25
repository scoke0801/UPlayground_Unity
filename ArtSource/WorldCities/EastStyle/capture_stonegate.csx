var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.name!="StonegateCitadel"||Application.isPlaying)throw new System.Exception("돌등성 Edit Mode 필요");
string stage=System.IO.File.ReadAllText("output/StonegateEast/stage.txt").Trim();
var views=new[]{new Vector3(213,66,94),new Vector3(180,12,86),new Vector3(180,12,140),new Vector3(180,12,205),new Vector3(146,12,177)};
var targets=new[]{new Vector3(180,13,185),new Vector3(180,16,120),new Vector3(180,15,235),new Vector3(180,20,240),new Vector3(151,13,166)};
var names=new[]{"overview","gate","street","keep","house"};
var cameraObject=new GameObject("StonegateArtCapture");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
ulong originalMask=UnityEditor.SceneManagement.EditorSceneManager.GetSceneCullingMask(scene);
ulong captureMask=UnityEditor.SceneManagement.EditorSceneManager.CalculateAvailableSceneCullingMask();
UnityEditor.SceneManagement.EditorSceneManager.SetSceneCullingMask(scene,captureMask);camera.overrideSceneCullingMask=captureMask;
camera.fieldOfView=60;camera.farClipPlane=1000;camera.clearFlags=CameraClearFlags.Skybox;
var utility=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UPlayGround.Tool.Editor.MinimapCaptureUtility")).First(t=>t!=null);
var lods=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<LODGroup>()).ToArray();foreach(var lod in lods)lod.ForceLOD(0);
try{for(int i=0;i<views.Length;i++){camera.transform.position=views[i];camera.transform.LookAt(targets[i]);var texture=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(1440,900),true,1000f});try{System.IO.File.WriteAllBytes("output/StonegateEast/"+stage+"/"+names[i]+".png",texture.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(texture);}}}
finally{foreach(var lod in lods)if(lod!=null)lod.ForceLOD(-1);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEditor.SceneManagement.EditorSceneManager.SetSceneCullingMask(scene,originalMask);}
return "5개 Editor 캡처: "+stage;
