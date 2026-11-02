if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying)return "Editor 준비 대기";
string path="Assets/01.Scenes/Test/PlayerSuperJumpGlidingTest.unity";
if(System.IO.File.Exists(path))return "기존 활공 테스트 씬을 보존합니다.";
if(!AssetDatabase.CopyAsset("Assets/01.Scenes/Test/CombatTestMap.unity",path))throw new System.Exception("독립 테스트 씬 복제 실패");
var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
try{
    foreach(var root in scene.GetRootGameObjects())
        foreach(var session in root.GetComponentsInChildren<UPlayGround.Training.TrainingGroundSession>(true))
            UnityEngine.Object.DestroyImmediate(session);
    var parent=new GameObject("활공 검사 구간");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(parent,scene);
    var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.color=new Color(0.38f,0.42f,0.36f);AssetDatabase.CreateAsset(material,"Assets/03.Prefabs/Traversal/Materials/GlidingTestCourse.mat");
    var names=new[]{"낮은 천장","높은 벽","좁은 통로 왼쪽","좁은 통로 오른쪽","경사면","높은 착지대","낮은 착지대"};
    var positions=new[]{new Vector3(15,3,0),new Vector3(-14,5,15),new Vector3(10,3,25),new Vector3(12,3,25),new Vector3(-15,1,-12),new Vector3(0,3.5f,35),new Vector3(18,1.5f,35)};
    var sizes=new[]{new Vector3(8,0.4f,8),new Vector3(1,10,20),new Vector3(1,6,10),new Vector3(1,6,10),new Vector3(8,0.5f,10),new Vector3(8,0.5f,8),new Vector3(8,0.5f,8)};
    for(int i=0;i<names.Length;i++){
        var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=names[i];UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(obj,scene);obj.transform.SetParent(parent.transform,false);obj.transform.position=positions[i];obj.transform.localScale=sizes[i];obj.GetComponent<MeshRenderer>().sharedMaterial=material;if(i==4)obj.transform.rotation=Quaternion.Euler(-20,0,0);
    }
    if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,path))throw new System.Exception("테스트 씬 저장 실패");
    return path;
}finally{UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);}
