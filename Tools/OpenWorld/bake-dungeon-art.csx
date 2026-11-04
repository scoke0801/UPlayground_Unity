var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")
    throw new Exception("연속 월드 Edit Mode가 필요합니다.");
var surface = GameObject.Find("AuthoredEnvironment").GetComponent<Unity.AI.Navigation.NavMeshSurface>();
string folder = "output/OpenWorld/DungeonArtReview";
string assetPath = "Assets/10.Datas/World/OpenWorld/DungeonArtNavigation.asset";
var previous = surface.navMeshData;
var updated = UnityEngine.Object.Instantiate(previous);
System.IO.File.WriteAllText(folder + "/navigation-status.txt", "던전 반영 NavMesh 증분 생성 중");
var operation = surface.UpdateNavMesh(updated);
operation.completed += _ => {
    try
    {
        // 등록된 타일을 해제한 뒤 데이터를 덮어쓴다. 역순이면 네이티브 타일 해제 중 충돌한다.
        surface.RemoveData();
        var saved = AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(assetPath);
        if (saved == null) { AssetDatabase.CreateAsset(updated,assetPath); saved=updated; }
        else { EditorUtility.CopySerialized(updated,saved); EditorUtility.SetDirty(saved); }
        surface.navMeshData=saved; surface.AddData();
        EditorUtility.SetDirty(surface);
        AssetDatabase.SaveAssetIfDirty(saved);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)) throw new Exception("NavMesh 연결 씬 저장 실패");
        System.IO.File.WriteAllText(folder + "/navigation-status.txt", "완료: " + assetPath);
    }
    catch (Exception error)
    {
        surface.RemoveData(); surface.navMeshData=previous; surface.AddData();
        System.IO.File.WriteAllText(folder + "/navigation-status.txt", "실패: " + error);
    }
    finally { if (AssetDatabase.GetAssetPath(updated)=="") UnityEngine.Object.DestroyImmediate(updated); }
};
return "증분 NavMesh 생성 예약. navigation-status.txt에서 결과를 확인합니다.";
