// 기존 준과 새 작업복을 같은 조명·동작에서 비교하는 검수 씬이다.
var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);
try
{
    UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
    string[] paths={"Assets/03.Prefabs/Characters/EquipmentReview/NpcVariants/NPC_Jun_Model.prefab","Assets/03.Prefabs/Characters/EquipmentReview/JunWorkwear/NPC_Jun_Workwear_Model.prefab"};
    for(int i=0;i<paths.Length;i++)
    {
        var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]),scene);
        model.name=i==0?"이전_준":"새_작업복_준";
        model.transform.position=new Vector3(i==0?.65f:-.65f,0,0);
        var animator=model.GetComponent<Animator>();
        animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/08.Models/Equipment/P09RangerVest/RangerVest_Review.controller");
        animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        PrefabUtility.RecordPrefabInstancePropertyModifications(model.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
    }
    var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.gameObject.AddComponent<AudioListener>();
    camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.13f,.15f,.16f);
    camera.orthographic=true;camera.orthographicSize=.90f;camera.transform.position=new Vector3(0,1.0f,3.7f);camera.transform.LookAt(new Vector3(0,.82f,0));
    var light=new GameObject("Key Light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.25f;light.transform.rotation=Quaternion.Euler(35,210,0);
    var fill=new GameObject("Fill Light").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.4f;fill.transform.rotation=Quaternion.Euler(20,100,0);
    RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.6f,.6f,.6f);
    if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,"Assets/01.Scenes/Tests/Equipment/P09_JunWorkwear_Review.unity"))throw new System.InvalidOperationException("검수 씬 저장 실패");
}
finally{UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
return "Assets/01.Scenes/Tests/Equipment/P09_JunWorkwear_Review.unity";
