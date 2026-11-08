// 기존 에셋의 복사본을 갱신하고 실제 타일을 확인한 뒤에만 씬에 연결한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string folder="output/OpenWorld/EnvironmentFollowup",path="Assets/10.Datas/World/OpenWorld/EnvironmentFollowup/NavigationFinal.asset";
if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity"||AssetDatabase.LoadMainAssetAtPath(path)!=null)throw new Exception("내비게이션 재시도 상태 불일치");
var surface=GameObject.Find("AuthoredEnvironment").GetComponent<Unity.AI.Navigation.NavMeshSurface>();var previous=AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>("Assets/10.Datas/World/OpenWorld/EnvironmentPolish/Navigation.asset");
if(!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(previous),path))throw new Exception("내비게이션 복사 실패");
AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var updated=AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(path);
Physics.SyncTransforms();System.IO.File.WriteAllText(folder+"/navigation-retry.txt","RUNNING");
var operation=surface.UpdateNavMesh(updated);
operation.completed+=_=>{
try{
int tiles=new SerializedObject(updated).FindProperty("m_NavMeshTiles").arraySize;if(tiles==0||updated.sourceBounds.size.x<2000)throw new Exception("타일 또는 범위 없음");
EditorUtility.SetDirty(updated);AssetDatabase.SaveAssetIfDirty(updated);long size=new System.IO.FileInfo(path).Length;if(size<100000)throw new Exception("디스크 타일 저장 실패");
surface.RemoveData();surface.navMeshData=updated;surface.AddData();EditorUtility.SetDirty(surface);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("내비게이션 씬 저장 실패");
System.IO.File.WriteAllText(folder+"/navigation-retry.txt","PASS tiles="+tiles+" bytes="+size+" path="+path);
}catch(Exception e){surface.RemoveData();surface.navMeshData=previous;surface.AddData();EditorUtility.SetDirty(surface);System.IO.File.WriteAllText(folder+"/navigation-retry.txt","FAIL "+e);}
};return new{started=true,isDone=operation.isDone,seedTiles=new SerializedObject(updated).FindProperty("m_NavMeshTiles").arraySize};
