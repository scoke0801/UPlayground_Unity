var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||scene.isDirty||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("저장된 월드 Edit Mode 필요");
var surface=GameObject.Find("AuthoredEnvironment").GetComponent<Unity.AI.Navigation.NavMeshSurface>();var data=surface.navMeshData;
if(AssetDatabase.GetAssetPath(data)!="Assets/10.Datas/World/OpenWorld/MarkedInfillNavigation.asset")throw new Exception("이번 작업의 NavMesh가 아닙니다.");
string folder="output/OpenWorld/MarkedInfill";
Physics.SyncTransforms();System.IO.File.WriteAllText(folder+"/grounding-navigation-phase.txt","바닥 소품 접평면 보정 후 NavMesh 갱신 중");
var operation=surface.UpdateNavMesh(data);
operation.completed+=_=> {
    try {
        EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);
        System.IO.File.WriteAllText(folder+"/grounding-navigation-phase.txt","완료: 바닥 소품 접지 보정 후 NavMesh 저장");
    }catch(Exception error){System.IO.File.WriteAllText(folder+"/grounding-navigation-phase.txt","실패: "+error);}
};
return "증분 갱신 예약: grounding-navigation-phase.txt로 완료 확인";
