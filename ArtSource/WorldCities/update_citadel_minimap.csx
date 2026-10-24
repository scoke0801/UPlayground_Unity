var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByName("StonegateCitadel");
if(!scene.IsValid()||Application.isPlaying)throw new System.Exception("돌등성 Edit Mode가 필요합니다.");
var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
ulong originalMask=UnityEditor.SceneManagement.EditorSceneManager.GetSceneCullingMask(scene);
ulong captureMask=UnityEditor.SceneManagement.EditorSceneManager.CalculateAvailableSceneCullingMask();
var cameraObject=new GameObject("CitadelMapCapture");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=180;camera.aspect=1;
camera.transform.position=new Vector3(180,450,180);camera.transform.rotation=Quaternion.Euler(90,0,0);camera.farClipPlane=1000;
camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.2f,.4f,.5f);
Texture2D texture=null;
try{
    UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
    UnityEditor.SceneManagement.EditorSceneManager.SetSceneCullingMask(scene,captureMask);camera.overrideSceneCullingMask=captureMask;
    // 이동하는 사람은 지도에 굽지 않고 기존 런타임 마커에 맡긴다.
    camera.cullingMask=~LayerMask.GetMask("Player","Npc");
    var utility=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UPlayGround.Tool.Editor.MinimapCaptureUtility")).First(t=>t!=null);
    texture=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(2048,2048),true,1000f});
    string directory="Assets/10.Datas/World/WorldCities/StonegateCitadel";
    string imagePath=directory+"/MapBackground.png";System.IO.File.WriteAllBytes(imagePath,texture.EncodeToPNG());
    var sprite=(Sprite)utility.GetMethod("ImportSprite").Invoke(null,new object[]{imagePath,new Vector2Int(2048,2048),false});
    var config=UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(directory+"/MinimapConfig.asset");
    var settings=new UnityEditor.SerializedObject(config);settings.FindProperty("backgroundSprite").objectReferenceValue=sprite;settings.ApplyModifiedPropertiesWithoutUndo();UnityEditor.AssetDatabase.SaveAssetIfDirty(config);
    return "돌등성 지도 2048x2048 갱신, 기존 지도 좌표와 Sprite GUID 보존";
}finally{
    if(texture!=null)UnityEngine.Object.DestroyImmediate(texture);
    UnityEngine.Object.DestroyImmediate(cameraObject);
    UnityEditor.SceneManagement.EditorSceneManager.SetSceneCullingMask(scene,originalMask);
    UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
}
