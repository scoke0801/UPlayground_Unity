var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var resultPath = "output/OpenWorld/LakeWorldReview/dressing-application.txt";
if (System.IO.File.Exists(resultPath)) return System.IO.File.ReadAllText(resultPath);
if (scene.path != "Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity" || scene.isDirty || EditorApplication.isPlayingOrWillChangePlaymode)
    throw new Exception("저장된 월드의 Edit Mode가 필요합니다.");
var terrain = Terrain.activeTerrain;
var fixedNames = new[]{"Flower_RestLog_R2", "Bamboo_RestLog_R2", "LakeIslands_EastArrival_Lantern_4_R2", "LakeIslands_NorthReturn_Lantern_20_R2", "LakeIslands_NorthReturn_Lantern_28_R2", "RockDebris_077_R2", "RockDebris_108_R2", "RockDebris_142_R2", "Forest_Ridge_076_R2", "Forest_Ridge_117_R2", "Forest_EastBank_016_R2", "Forest_EastBank_027_R2", "Forest_EastBank_063_R2", "Shrubs_152_R2", "Path_LeafLitter_015_R2", "Path_LeafLitter_144_R2", "Path_LeafLitter_425_R2", "Southeast_Garden_43_R2"};
int fixedCount = 0;
foreach (string name in fixedNames) {
    var go = GameObject.Find(name);
    if (go == null) throw new Exception("접지 대상 누락: " + name);
    var p = go.transform.position;
    float ground = terrain.SampleHeight(p) + terrain.transform.position.y;
    if (name == "LakeIslands_NorthReturn_Lantern_28_R2") {
        var hits = Physics.RaycastAll(p + Vector3.up * 2, Vector3.down, 30, -1, QueryTriggerInteraction.Ignore);
        foreach (var hit in hits) if (!hit.transform.IsChildOf(go.transform) && hit.point.y > ground) ground = hit.point.y;
    }
    var meshes = go.GetComponentsInChildren<MeshRenderer>(true);
    if (meshes.Length == 0) throw new Exception("메시 누락: " + name);
    var bounds = meshes[0].bounds;
    foreach (var mesh in meshes) bounds.Encapsulate(mesh.bounds);
    float baseY = name.StartsWith("Forest_") || name.StartsWith("Path_") ? p.y : bounds.min.y;
    go.transform.position += Vector3.up * (ground - baseY - .04f);
    PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
    fixedCount++;
}
var parent = GameObject.Find("AuthoredEnvironment/월드 구간/호수와 사당").transform;
if (parent.Find("남쪽 호숫가 쉼터") != null) throw new Exception("남쪽 호숫가 배치가 이미 있습니다.");
var group = new GameObject("남쪽 호숫가 쉼터").transform;
group.SetParent(parent, false);
string treeA = "Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Prefabs/Foliage/Trees/PF_Farm_GenTree_A_FullTree_01.prefab";
string treeB = "Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Prefabs/Foliage/Trees/PF_Farm_GenTree_B_FullTree_01.prefab";
string bush = "Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Prefabs/Foliage/Trees/Bushes/PF_Farm_GenTree_Bush_07.prefab";
var placements = new[]{
    new { path=treeA, name="서쪽 나무 1", x=986f, z=648f, yaw=28f, scale=.72f },
    new { path=treeB, name="서쪽 나무 2", x=1002f, z=704f, yaw=141f, scale=.70f },
    new { path=treeA, name="서쪽 나무 3", x=1018f, z=725f, yaw=240f, scale=.68f },
    new { path=treeB, name="동쪽 나무 1", x=1143f, z=637f, yaw=65f, scale=.76f },
    new { path=treeA, name="동쪽 나무 2", x=1155f, z=688f, yaw=197f, scale=.68f },
    new { path=treeB, name="동쪽 나무 3", x=1136f, z=701f, yaw=310f, scale=.66f },
    new { path=bush, name="서쪽 관목 1", x=993f, z=652f, yaw=40f, scale=.11f },
    new { path=bush, name="서쪽 관목 2", x=997f, z=656f, yaw=120f, scale=.09f },
    new { path=bush, name="서쪽 관목 3", x=1011f, z=701f, yaw=210f, scale=.11f },
    new { path=bush, name="서쪽 관목 4", x=1013f, z=705f, yaw=13f, scale=.10f },
    new { path=bush, name="동쪽 관목 1", x=1139f, z=643f, yaw=35f, scale=.11f },
    new { path=bush, name="동쪽 관목 2", x=1142f, z=647f, yaw=90f, scale=.09f },
    new { path=bush, name="동쪽 관목 3", x=1148f, z=684f, yaw=240f, scale=.11f },
    new { path=bush, name="동쪽 관목 4", x=1145f, z=688f, yaw=180f, scale=.10f },
    new { path="Assets/03.Prefabs/Environment/PoorVillage/Prop_Pyeongsang.prefab", name="나무 그늘 평상", x=1012f, z=654f, yaw=20f, scale=1f },
    new { path="Assets/03.Prefabs/Environment/PoorVillage/Prop_Jige.prefab", name="쉼터 지게", x=1015f, z=653f, yaw=98f, scale=1f }
};
foreach (var item in placements) {
    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(item.path);
    if (prefab == null) throw new Exception("프리팹 누락: " + item.path);
    var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
    obj.name = item.name;
    obj.transform.SetParent(group, false);
    var p = new Vector3(item.x, 0, item.z);
    p.y = terrain.SampleHeight(p) + terrain.transform.position.y - .03f;
    obj.transform.position = p;
    obj.transform.rotation = Quaternion.Euler(0, item.yaw, 0);
    obj.transform.localScale = Vector3.one * item.scale;
    PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
string result = "확인된 접지 오류 " + fixedCount + "개 보정, 호숫가 나무·관목·쉼터 " + placements.Length + "개 배치. NavMesh 재생성 필요.";
System.IO.File.WriteAllText(resultPath, result);
return result;
