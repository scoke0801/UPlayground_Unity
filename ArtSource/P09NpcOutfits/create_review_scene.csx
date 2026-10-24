// 이벤트를 제거한 기존 검수 클립으로 세 체형의 움직임을 함께 비교한다.
var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);
try
{
    UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
    var controller=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/08.Models/Equipment/P09RangerVest/RangerVest_Review.controller");
    string[] names={"NPC_Jun","Npc_Yeonmok","NPC_Shop"};
    for(int i=0;i<names.Length;i++)
    {
        var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Characters/EquipmentReview/NpcVariants/"+names[i]+"_Model.prefab"),scene);
        model.transform.position=new Vector3((i-1)*1.3f,0,0);
        var animator=model.GetComponent<Animator>();
        animator.runtimeAnimatorController=controller;
        animator.applyRootMotion=false;
        animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        PrefabUtility.RecordPrefabInstancePropertyModifications(model.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
    }
    var camera=new GameObject("Main Camera").AddComponent<Camera>();
    camera.tag="MainCamera";camera.gameObject.AddComponent<AudioListener>();
    camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(0.13f,0.15f,0.16f);
    camera.transform.position=new Vector3(0,1.05f,5.8f);camera.transform.LookAt(new Vector3(0,0.82f,0));camera.fieldOfView=27;
    var light=new GameObject("Key Light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.25f;light.transform.rotation=Quaternion.Euler(35,210,0);
    var fill=new GameObject("Fill Light").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=0.4f;fill.transform.rotation=Quaternion.Euler(20,100,0);
    RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(0.6f,0.6f,0.6f);
    if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,"Assets/01.Scenes/Tests/Equipment/P09_NpcOutfits_Review.unity"))throw new System.InvalidOperationException("검수 씬 저장 실패");
}
finally
{
    UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);
}
return "Assets/01.Scenes/Tests/Equipment/P09_NpcOutfits_Review.unity";
