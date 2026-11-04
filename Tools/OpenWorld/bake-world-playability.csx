// 등록 중인 NavMeshData를 덮어쓰지 않고 별도 에셋으로 완성본을 생성한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||scene.isDirty||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("저장된 월드 Edit Mode 필요");
string folder="output/OpenWorld/WorldPlayability";
string path="Assets/10.Datas/World/OpenWorld/WorldPlayabilityNavigation.asset";
if(AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(path)!=null)throw new Exception("기존 최종 NavMesh가 있습니다. 재실행 금지");
var surface=GameObject.Find("AuthoredEnvironment").GetComponent<Unity.AI.Navigation.NavMeshSurface>();
var previous=surface.navMeshData;
var updated=new UnityEngine.AI.NavMeshData(surface.agentTypeID){position=surface.transform.position,rotation=surface.transform.rotation};
System.IO.File.WriteAllText(folder+"/navigation.txt","RUNNING");
var operation=surface.UpdateNavMesh(updated);
operation.completed+=_=>{
    try {
        AssetDatabase.CreateAsset(updated,path);AssetDatabase.SaveAssetIfDirty(updated);
        surface.RemoveData();surface.navMeshData=updated;surface.AddData();EditorUtility.SetDirty(surface);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("최종 NavMesh 씬 저장 실패");
        System.IO.File.WriteAllText(folder+"/navigation.txt","PASS "+path);
    }catch(Exception e){
        surface.RemoveData();surface.navMeshData=previous;surface.AddData();
        System.IO.File.WriteAllText(folder+"/navigation.txt","FAIL "+e);
    }
};
return "최종 NavMesh 생성 시작. navigation.txt 결과 확인 필요";
