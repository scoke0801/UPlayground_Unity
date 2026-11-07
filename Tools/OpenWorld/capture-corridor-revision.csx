var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity");
if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode가 필요합니다.");
string folder="output/OpenWorld/DungeonWallRevision";
var utility=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UPlayGround.Tool.Editor.MinimapCaptureUtility")).First(t=>t!=null);
var pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
var mode=pipeline.GetType().GetProperty("gpuResidentDrawerMode");var previousMode=mode.GetValue(pipeline,null);
var go=new GameObject("회랑 검수 카메라"){hideFlags=HideFlags.HideAndDontSave};
var camera=go.AddComponent<Camera>();camera.enabled=false;camera.scene=scene;camera.useOcclusionCulling=false;camera.fieldOfView=62;camera.farClipPlane=700;
var positions=new[]{new Vector3(650,102.8f,534),new Vector3(650,103,485),new Vector3(675,137,509),new Vector3(650,103,559),new Vector3(624,107,548)};
var targets=new[]{new Vector3(650,103,574),new Vector3(650,103,521),new Vector3(640,101,548),new Vector3(650,103,580),new Vector3(607,106,548)};
string prefix=GameObject.Find("AuthoredEnvironment/월드 구간/던전/옛 회랑/석조 회랑 개선")==null?"corridor-before-":"corridor-after-";
try{mode.SetValue(pipeline,Enum.Parse(mode.PropertyType,"Disabled"),null);for(int i=0;i<positions.Length;i++){
camera.transform.SetPositionAndRotation(positions[i],Quaternion.LookRotation(targets[i]-positions[i]));
var texture=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(1100,700),false,1000f});
try{System.IO.File.WriteAllBytes(folder+"/"+prefix+i+".png",texture.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(texture);}
}}finally{UnityEngine.Object.DestroyImmediate(go);mode.SetValue(pipeline,previousMode,null);}
return prefix;
