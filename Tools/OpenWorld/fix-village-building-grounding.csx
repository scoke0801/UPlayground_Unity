// 현재 모델에 남은 구형 충돌 박스와 떠 있는 주택 3채만 보정한다.
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed
    || scene.isDirty || scene.path != "Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")
    throw new Exception("저장된 호수 마을 Edit Mode가 필요합니다.");
string folder = "output/OpenWorld/BuildingGrounding";
System.IO.Directory.CreateDirectory(folder);
if (System.IO.File.Exists(folder + "/applied.txt")) throw new Exception("이미 적용한 배치입니다.");
var root = GameObject.Find("AuthoredEnvironment/월드 구간/PoorVillage/기존 지형과 소품").transform;
var terrain = Terrain.activeTerrain;
string[] names = { "Village_Home_West", "Village_Bakery", "Village_Home_East", "Village_GuestHouse", "Village_Home_Garden", "Village_Windmill" };
var buildings = names.Select(n => root.Find(n + "_R2")).ToArray();
var blockers = names.Select(n => root.Find(n + "_Collision")).ToArray();
if (terrain == null || buildings.Any(t => t == null) || blockers.Any(t => t == null)) throw new Exception("조사한 배치가 변경되었습니다.");
foreach (var t in buildings)
    if (t.GetComponentsInChildren<MeshCollider>(true).Length == 0 || t.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true).Length != 0)
        throw new Exception("건물 충돌 또는 진행 개체 경계를 확인하세요: " + t.name);
var ids = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).ToArray();
Func<string> identities = () => string.Join("\n", ids.Select(id => id.Guid + "|" + id.transform.position.ToString("F4")).OrderBy(s => s));
string before = identities();
var previous = buildings.Select(t => t.name + "|" + EditorJsonUtility.ToJson(t)).Concat(blockers.Select(t => t.name + "|" + EditorJsonUtility.ToJson(t.gameObject))).ToArray();
System.IO.File.WriteAllLines(folder + "/before-objects.txt", previous);
System.IO.File.WriteAllText(folder + "/identities-before.txt", before);
Undo.IncrementCurrentGroup();
int group = Undo.GetCurrentGroup();
Undo.SetCurrentGroupName("마을 주택 접지와 구형 충돌 보정");
var rows = new List<string>();
try
{
    foreach (var t in buildings)
    {
        var foundation = t.GetComponentsInChildren<MeshRenderer>(true).Single(r => r.name.Contains("Foundation_Stone"));
        var bounds = foundation.bounds;
        float lowestGround = float.MaxValue;
        for (int z = 0; z < 5; z++)
            for (int x = 0; x < 5; x++)
            {
                var point = new Vector3(Mathf.Lerp(bounds.min.x, bounds.max.x, x / 4f), 0, Mathf.Lerp(bounds.min.z, bounds.max.z, z / 4f));
                lowestGround = Mathf.Min(lowestGround, terrain.SampleHeight(point) + terrain.transform.position.y);
            }
        float offset = lowestGround - bounds.min.y - .03f;
        if (Mathf.Abs(offset) <= .05f) continue;
        if (offset > 0 || offset < -2.2f) throw new Exception("조사 범위 밖 높이 보정: " + t.name);
        Undo.RecordObject(t, "주택 기초 접지");
        t.position += Vector3.up * offset;
        PrefabUtility.RecordPrefabInstancePropertyModifications(t);
        rows.Add(t.name + " 높이 보정=" + offset.ToString("F4"));
    }
    foreach (var t in blockers)
    {
        // 원본 모델의 MeshCollider가 있으므로 이전 모델 크기의 별도 박스는 중복이다.
        Undo.RecordObject(t.gameObject, "구형 건물 충돌 해제");
        t.gameObject.SetActive(false);
        rows.Add(t.name + " 구형 충돌 비활성화");
    }
    Physics.SyncTransforms();
    if (before != identities()) throw new Exception("진행 개체가 변경되었습니다.");
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)) throw new Exception("씬 저장 실패");
    Undo.CollapseUndoOperations(group);
    System.IO.File.WriteAllText(folder + "/identities-after.txt", identities());
    System.IO.File.WriteAllLines(folder + "/applied.txt", rows);
    return string.Join("\n", rows);
}
catch
{
    Undo.RevertAllDownToGroup(group);
    throw;
}
