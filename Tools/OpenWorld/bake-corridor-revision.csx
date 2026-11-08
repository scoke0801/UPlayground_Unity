var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity");
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorUtility.scriptCompilationFailed)throw new Exception("월드 Edit Mode와 정상 컴파일 필요");
string folder="output/OpenWorld/DungeonWallRevision",path="Assets/10.Datas/World/OpenWorld/DungeonWallRevisionNavigation.asset";
if(!System.IO.File.Exists(folder+"/applied.txt")||AssetDatabase.LoadMainAssetAtPath(path)!=null)throw new Exception("저작 적용 누락 또는 이미 생성한 NavMesh");
var surface=GameObject.Find("AuthoredEnvironment").GetComponent<Unity.AI.Navigation.NavMeshSurface>();var previous=surface.navMeshData;
System.IO.File.WriteAllText(folder+"/navigation-before.txt",AssetDatabase.GetAssetPath(previous));
var updated=UnityEngine.Object.Instantiate(previous);AssetDatabase.CreateAsset(updated,path);
System.IO.File.WriteAllText(folder+"/navigation.txt","RUNNING");
var operation=surface.UpdateNavMesh(updated);
operation.completed+=_=>{try{
EditorUtility.SetDirty(updated);AssetDatabase.SaveAssetIfDirty(updated);
surface.RemoveData();surface.navMeshData=updated;surface.AddData();EditorUtility.SetDirty(surface);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("NavMesh 씬 저장 실패");
System.IO.File.WriteAllText(folder+"/navigation.txt","PASS "+path);
}catch(Exception e){surface.RemoveData();surface.navMeshData=previous;surface.AddData();System.IO.File.WriteAllText(folder+"/navigation.txt","FAIL "+e);}};
return "기존 NavMesh를 보존한 별도 에셋 증분 베이크 시작";
