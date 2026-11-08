// 기존 내비게이션을 보존한 채 바위 비율 변경을 별도 에셋에 반영한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||scene.isDirty||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("저장된 월드 Edit Mode 필요");
string folder="output/OpenWorld/EnvironmentPolish",path="Assets/10.Datas/World/OpenWorld/EnvironmentPolish/Navigation.asset";
if(!System.IO.File.Exists(folder+"/applied.txt")||AssetDatabase.LoadMainAssetAtPath(path)!=null)throw new Exception("배치 적용 누락 또는 이미 생성된 NavMesh");
var surface=GameObject.Find("AuthoredEnvironment").GetComponent<Unity.AI.Navigation.NavMeshSurface>();var previous=surface.navMeshData;
System.IO.File.WriteAllText(folder+"/navigation-before.txt",AssetDatabase.GetAssetPath(previous));
var updated=new UnityEngine.AI.NavMeshData(surface.agentTypeID){position=surface.transform.position,rotation=surface.transform.rotation};
// 비동기 작업 중 미사용 메모리 에셋이 해제되지 않도록 먼저 독립 에셋으로 보존한다.
AssetDatabase.CreateAsset(updated,path);
System.IO.File.WriteAllText(folder+"/navigation.txt","RUNNING");
var operation=surface.UpdateNavMesh(updated);
operation.completed+=_=>{
try{
EditorUtility.SetDirty(updated);AssetDatabase.SaveAssetIfDirty(updated);
surface.RemoveData();surface.navMeshData=updated;surface.AddData();EditorUtility.SetDirty(surface);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("NavMesh 씬 저장 실패");
System.IO.File.WriteAllText(folder+"/navigation.txt","PASS "+path);
}catch(Exception e){
surface.RemoveData();surface.navMeshData=previous;surface.AddData();EditorUtility.SetDirty(surface);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
System.IO.File.WriteAllText(folder+"/navigation.txt","FAIL "+e);
}
};return "환경 보정 NavMesh 생성 시작";
