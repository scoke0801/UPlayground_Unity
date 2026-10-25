// GPU Resident Drawer의 기존 씬 마스크를 유지하고 다른 도시를 촬영에서 제외한다.
var other=UnityEngine.SceneManagement.SceneManager.GetSceneByName("SaltwindHarbor");
if(other.IsValid()&&other.isDirty)throw new System.Exception("항구의 미저장 변경을 먼저 보존해야 합니다.");
bool closed=other.IsValid()&&other.isLoaded;
if(closed)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(other,true);
try{var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.name!="StonegateCitadel"||Application.isPlaying)throw new System.Exception("돌등성 Edit Mode 필요");
string stage="After";
var views=new[]{new Vector3(292,82,60),new Vector3(180,32,202),new Vector3(225,40,202),new Vector3(205,26,220)};
var targets=new[]{new Vector3(180,16,195),new Vector3(180,28,254),new Vector3(180,30,250),new Vector3(180,21,240)};
var names=new[]{"fortress-hero","keep-front","keep-angle","approach"};
var cameraObject=new GameObject("StonegateArtCapture");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
ulong originalMask=UnityEditor.SceneManagement.EditorSceneManager.GetSceneCullingMask(scene);
camera.overrideSceneCullingMask=originalMask;
camera.fieldOfView=60;camera.farClipPlane=1000;camera.clearFlags=CameraClearFlags.Skybox;
var utility=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UPlayGround.Tool.Editor.MinimapCaptureUtility")).First(t=>t!=null);
var lods=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<LODGroup>()).ToArray();foreach(var lod in lods)lod.ForceLOD(0);
try{for(int i=0;i<views.Length;i++){camera.transform.position=views[i];camera.transform.LookAt(targets[i]);var texture=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(1440,900),true,1000f});try{System.IO.File.WriteAllBytes("output/StonegateFortress/"+stage+"/"+names[i]+".png",texture.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(texture);}}}
finally{foreach(var lod in lods)if(lod!=null)lod.ForceLOD(-1);UnityEngine.Object.DestroyImmediate(cameraObject);}
return names.Length+"개 Editor 캡처: "+stage;

}finally{if(closed)UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/01.Scenes/Scenario/SaltwindHarbor.unity",UnityEditor.SceneManagement.OpenSceneMode.Additive);var s=UnityEngine.SceneManagement.SceneManager.GetSceneByName("StonegateCitadel");UnityEngine.SceneManagement.SceneManager.SetActiveScene(s);}
