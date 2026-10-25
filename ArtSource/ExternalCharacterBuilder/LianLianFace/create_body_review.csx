// 의상 프리팹은 유지하고 씬 인스턴스의 표시 부위만 바꾼다.
const string scenePath="Assets/01.Scenes/Tests/Equipment/LianLian_P09_Review.unity";
const string reviewName="LianLian_P09_BodyOnly";
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
bool opened=!scene.isLoaded;
if(scene.isLoaded&&scene.isDirty)throw new Exception("검수 씬의 미저장 변경을 먼저 보존해야 합니다.");
if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath,UnityEditor.SceneManagement.OpenSceneMode.Additive);
try{
    if(scene.GetRootGameObjects().Any(r=>r.name==reviewName))return "기본 몸체 검수본이 이미 있습니다.";
    var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Characters/ExternalModels/LianLian_P09Compatible.prefab");
    var root=(GameObject)PrefabUtility.InstantiatePrefab(source,scene);
    root.name=reviewName;root.transform.position=new Vector3(1.95f,0,0);
    var paths=new[]{"Mesh/Female/Body/Female_Body_Head/Female_Face_02","Mesh/Female/Body/Female_Body_Chest","Mesh/Female/Body/Female_Body_Leg"};
    foreach(var r in root.GetComponentsInChildren<Renderer>(true))r.gameObject.SetActive(false);
    foreach(var path in paths){
        var target=root.transform.Find(path);
        if(target==null||target.GetComponent<SkinnedMeshRenderer>()==null)throw new Exception("기본 몸체 경로 누락: "+path);
        for(var t=target;t!=null&&t!=root.transform;t=t.parent)t.gameObject.SetActive(true);
    }
    var helper=typeof(UPlayGround.Editor.P09Builder.PrefabBuildPipeline).Assembly.GetType("UPlayGround.Editor.P09Builder.P09PosePreview");
    var motion=AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/Npc_Female_Idle.asset");
    using(var pose=(IDisposable)Activator.CreateInstance(helper,new object[]{root}))helper.GetMethod("Sample").Invoke(pose,new object[]{motion,.35f,null});
    foreach(var t in root.GetComponentsInChildren<Transform>(true)){
        PrefabUtility.RecordPrefabInstancePropertyModifications(t);
        PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
    }
    var visible=root.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();
    if(visible.Length!=3||visible.Any(r=>!paths.Contains(AnimationUtility.CalculateTransformPath(r.transform,root.transform))))throw new Exception("기본 몸체 외 렌더러가 활성화되었습니다.");
    var camera=scene.GetRootGameObjects().Select(r=>r.GetComponent<Camera>()).First(c=>c!=null);
    camera.transform.position=new Vector3(.65f,1.15f,6f);camera.transform.LookAt(new Vector3(.65f,.85f,0));
    if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,scenePath))throw new Exception("기본 몸체 검수 씬 저장 실패");
    return "LianLian_P09_BodyOnly: Face02/Chest/Leg 3개 표시, 위치 (1.95,0,0). 기존 두 모델·프리팹 보존.";
}finally{if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}

