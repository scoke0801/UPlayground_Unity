// 원본 내비게이션을 변경하지 않고 검토 지형과 콜라이더로 별도 데이터를 굽는다.
var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(EditorApplication.isPlayingOrWillChangePlaymode||s.isDirty||s.path!="Assets/01.Scenes/ArtReview/WorldThemes/LakeReworkR2.unity")throw new Exception("저장된 검토 씬 필요");
string path="Assets/10.Datas/World/ThemeStudies/LakeReworkR2/Navigation.asset";
if(AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(path)!=null)throw new Exception("이미 생성한 데이터");
var surface=GameObject.Find("AuthoredEnvironment").GetComponent<Unity.AI.Navigation.NavMeshSurface>();
var data=new UnityEngine.AI.NavMeshData(surface.agentTypeID);data.position=surface.transform.position;data.rotation=surface.transform.rotation;data.name="LakeReviewNavigation";
AssetDatabase.CreateAsset(data,path);var operation=surface.UpdateNavMesh(data);
operation.completed+=o=>{
EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);
if(EditorApplication.isPlayingOrWillChangePlaymode||UnityEngine.SceneManagement.SceneManager.GetActiveScene()!=s){System.IO.File.WriteAllText("output/LakeReworkR2/navigation-bake.txt","데이터 생성, 씬 전환으로 연결 대기");return;}
surface.RemoveData();surface.navMeshData=data;surface.AddData();EditorUtility.SetDirty(surface);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(s);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s);
System.IO.File.WriteAllText("output/LakeReworkR2/navigation-bake.txt","검토 전용 NavMesh 생성·연결 완료. 원본 데이터 유지.");
};return "검토 전용 NavMesh 비동기 베이크 시작";
