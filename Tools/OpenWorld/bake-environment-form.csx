// 완료 콜백은 완료 표식만 기록한다. 실제 저장과 연결은 별도 검사를 거친다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string folder="output/OpenWorld/EnvironmentForm",path="Assets/10.Datas/World/OpenWorld/EnvironmentFormNavigation.asset";
if(scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity"||scene.isDirty||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorUtility.scriptCompilationFailed||AssetDatabase.LoadMainAssetAtPath(path)!=null)throw new Exception("새 NavMesh 생성 상태 불일치");
var surface=GameObject.Find("AuthoredEnvironment").GetComponent<Unity.AI.Navigation.NavMeshSurface>();
if(!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(surface.navMeshData),path))throw new Exception("NavMesh 복사 실패");
AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var updated=AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(path);Physics.SyncTransforms();
System.IO.File.WriteAllText(folder+"/navigation-operation.txt","RUNNING");var operation=surface.UpdateNavMesh(updated);
operation.completed+=_=>{System.IO.File.WriteAllText(folder+"/navigation-operation.txt","COMPLETED");};return new{started=true,isDone=operation.isDone};
