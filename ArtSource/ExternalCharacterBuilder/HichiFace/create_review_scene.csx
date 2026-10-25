string path="Assets/01.Scenes/Tests/Equipment/P09_Hichi_Review.unity";
if(UnityEditor.AssetDatabase.LoadMainAssetAtPath(path)!=null)return "기존 검수 씬 보존";
var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);
try
{
    UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
    var helper=typeof(UPlayGround.Editor.P09Builder.PrefabBuildPipeline).Assembly.GetType("UPlayGround.Editor.P09Builder.P09PosePreview");
    var motion=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/Npc_Female_Idle.asset");
    var paths=new[]{"Hichi_P09Compatible","Hichi_P09_Alternate"};
    for(int i=0;i<paths.Length;i++)
    {
        var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/03.Prefabs/Characters/ExternalModels/"+paths[i]+".prefab");
        var root=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab,scene);
        root.transform.position=new UnityEngine.Vector3(i==0?-.55f:.55f,0,0);
        var pose=(System.IDisposable)System.Activator.CreateInstance(helper,new object[]{root});
        try{helper.GetMethod("Sample").Invoke(pose,new object[]{motion,.35f,null});}finally{pose.Dispose();}
        var animancer=root.GetComponent<Animancer.AnimancerComponent>();if(animancer!=null)UnityEngine.Object.DestroyImmediate(animancer);
        root.GetComponent<UnityEngine.Animator>().enabled=false;
        foreach(var t in root.GetComponentsInChildren<UnityEngine.Transform>(true))UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(t);
        UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(root.GetComponent<UnityEngine.Animator>());
    }
    var camera=new UnityEngine.GameObject("검수 카메라").AddComponent<UnityEngine.Camera>();camera.tag="MainCamera";
    camera.transform.position=new UnityEngine.Vector3(0,.9f,3.5f);camera.transform.LookAt(new UnityEngine.Vector3(0,.85f,0));camera.fieldOfView=35;camera.nearClipPlane=.02f;camera.farClipPlane=30;
    camera.clearFlags=UnityEngine.CameraClearFlags.SolidColor;camera.backgroundColor=new UnityEngine.Color(.18f,.19f,.21f);
    var light=new UnityEngine.GameObject("주광").AddComponent<UnityEngine.Light>();light.type=UnityEngine.LightType.Directional;light.intensity=1.2f;light.transform.rotation=UnityEngine.Quaternion.Euler(30,150,0);
    var fill=new UnityEngine.GameObject("보조광").AddComponent<UnityEngine.Light>();fill.type=UnityEngine.LightType.Directional;fill.intensity=.5f;fill.transform.rotation=UnityEngine.Quaternion.Euler(20,-35,0);
    UnityEngine.RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;UnityEngine.RenderSettings.ambientLight=new UnityEngine.Color(.55f,.55f,.55f);
    if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,path))throw new System.Exception("검수 씬 저장 실패");
}
finally
{
    UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);
}
var loaded=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
try
{
    if(loaded.GetRootGameObjects().Length!=5)throw new System.Exception("검수 씬 재로드 결과 불일치");
    return new{scene=path,roots=loaded.GetRootGameObjects().Length};
}
finally{UnityEditor.SceneManagement.EditorSceneManager.CloseScene(loaded,true);}
