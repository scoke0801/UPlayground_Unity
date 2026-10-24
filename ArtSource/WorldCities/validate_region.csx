string cityId="CITY_ID";
string scenePath="Assets/01.Scenes/Scenario/"+cityId+".unity";
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.name!=cityId)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath,UnityEditor.SceneManagement.OpenSceneMode.Single);
string directory="Assets/10.Datas/World/WorldCities/"+cityId;
var environment=scene.GetRootGameObjects().First(r=>r.name=="CityEnvironment");
var surfaceType=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Unity.AI.Navigation.NavMeshSurface")).First(t=>t!=null);
var surface=environment.GetComponent(surfaceType);
if(surface==null)surface=environment.AddComponent(surfaceType);
var navigation=new UnityEditor.SerializedObject(surface);
navigation.FindProperty("m_CollectObjects").intValue=2;
navigation.FindProperty("m_UseGeometry").intValue=1;
navigation.FindProperty("m_LayerMask").intValue=LayerMask.GetMask("Ground");
navigation.FindProperty("m_OverrideVoxelSize").boolValue=true;
navigation.FindProperty("m_VoxelSize").floatValue=.2f;
navigation.ApplyModifiedPropertiesWithoutUndo();
surfaceType.GetMethod("BuildNavMesh").Invoke(surface,null);
var navData=(UnityEngine.AI.NavMeshData)surfaceType.GetProperty("navMeshData").GetValue(surface,null);
if(navData==null)throw new System.Exception("NavMesh 생성 실패");
string navPath=directory+"/Navigation.asset";
var previous=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(navPath);
if(previous==null){navData.name="Navigation";UnityEditor.AssetDatabase.CreateAsset(navData,navPath);}
else {surfaceType.GetMethod("RemoveData").Invoke(surface,null);UnityEditor.EditorUtility.CopySerialized(navData,previous);previous.name="Navigation";surfaceType.GetProperty("navMeshData").SetValue(surface,previous,null);surfaceType.GetMethod("AddData").Invoke(surface,null);UnityEditor.EditorUtility.SetDirty(previous);}
UnityEngine.Physics.SyncTransforms();
var report=new System.Collections.Generic.List<string>();
int missing=0,materials=0;
foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true)){
    missing+=UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
    foreach(var renderer in t.GetComponents<Renderer>())foreach(var mat in renderer.sharedMaterials)if(mat==null||mat.shader==null||mat.shader.name=="Hidden/InternalErrorShader")materials++;
}
report.Add("missing_scripts="+missing);report.Add("invalid_materials="+materials);
bool harbor=cityId=="SaltwindHarbor";
var destinations=harbor?new[]{new Vector3(180,10.1f,160),new Vector3(180,10.1f,216),new Vector3(165,10.1f,243),new Vector3(128,10.1f,133),new Vector3(116,10.1f,259)}:new[]{new Vector3(180,10.1f,140),new Vector3(180,10.1f,177),new Vector3(180,10.1f,220),new Vector3(214,10.1f,102)};
int failed=0;
foreach(var destination in destinations){
    UnityEngine.AI.NavMeshHit start,end;
    bool foundStart=UnityEngine.AI.NavMesh.SamplePosition(new Vector3(180,10.1f,55),out start,2,UnityEngine.AI.NavMesh.AllAreas);
    bool foundEnd=UnityEngine.AI.NavMesh.SamplePosition(destination,out end,2,UnityEngine.AI.NavMesh.AllAreas);
    var path=new UnityEngine.AI.NavMeshPath();
    bool complete=foundStart&&foundEnd&&UnityEngine.AI.NavMesh.CalculatePath(start.position,end.position,UnityEngine.AI.NavMesh.AllAreas,path)&&path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete;
    report.Add("path_to_"+destination+"="+complete);if(!complete)failed++;
}
// 프로젝트의 기존 지도 캡처·임포트 경로를 재사용한다.
var utility=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UPlayGround.Tool.Editor.MinimapCaptureUtility")).First(t=>t!=null);
var cameraObject=new GameObject("CityMapCapture");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
camera.orthographic=true;camera.orthographicSize=180;camera.aspect=1;camera.transform.position=new Vector3(180,450,180);camera.transform.rotation=Quaternion.Euler(90,0,0);camera.farClipPlane=1000;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.2f,.4f,.5f);
Texture2D texture=null;
try{
    texture=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(2048,2048),true,1000f});
    string imagePath=directory+"/MapBackground.png";System.IO.File.WriteAllBytes(imagePath,texture.EncodeToPNG());
    var sprite=(Sprite)utility.GetMethod("ImportSprite").Invoke(null,new object[]{imagePath,new Vector2Int(2048,2048),false});
    var config=UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(directory+"/MinimapConfig.asset");var so=new UnityEditor.SerializedObject(config);so.FindProperty("backgroundSprite").objectReferenceValue=sprite;so.ApplyModifiedPropertiesWithoutUndo();UnityEditor.AssetDatabase.SaveAssetIfDirty(config);
}finally{if(texture!=null)UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(cameraObject);}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);UnityEditor.AssetDatabase.SaveAssets();
report.Add("path_failures="+failed);report.Add("minimap=2048x2048");
System.IO.File.WriteAllLines("output/WorldCities/"+cityId+"-validation.txt",report.ToArray());
return string.Join("\n",report.ToArray());
