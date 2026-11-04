// 조립 석문을 에이전트가 통과할 수 있는 폭으로 넓힌다. 원본 프리팹은 보존한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||scene.isDirty||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("저장된 월드 Edit Mode 필요");
var root=GameObject.Find("AuthoredEnvironment/던전 공간 재저작");
var doors=root.transform.Cast<Transform>().SelectMany(t=>t.Cast<Transform>()).Where(t=>t.name.StartsWith("arch_")&&t.localScale.x<1.25f).ToArray();
Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("유적 석문 통행 폭 보정");
try {
    foreach(var door in doors) {
        var renderer=door.GetComponentsInChildren<MeshRenderer>(true).First(r=>r.name=="P_MOD_Gateway_Door_01_large");
        var center=renderer.bounds.center;Undo.RecordObject(door,"석문 폭");var scale=door.localScale;scale.x=1.25f;door.localScale=scale;
        var offset=center-renderer.bounds.center;offset.y=0;door.position+=offset;
        PrefabUtility.RecordPrefabInstancePropertyModifications(door);
    }
    Physics.SyncTransforms();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("씬 저장 실패");
    Undo.CollapseUndoOperations(group);
    System.IO.File.WriteAllText("output/OpenWorld/WorldPlayability/passages.txt","통행 폭 보정="+doors.Length+" / 석문 X scale 최소 1.25 / 원본 프리팹 보존 / NavMesh 재생성 필요");
    return doors.Length;
}catch{Undo.RevertAllDownToGroup(group);throw;}
