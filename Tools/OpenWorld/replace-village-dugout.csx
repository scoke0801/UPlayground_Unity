// 흙덩어리로 오인되는 움집을 기존 마을의 작은 초가집으로 교체한다.
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed
    || scene.path != "Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity") throw new Exception("호수 마을 Edit Mode가 필요합니다.");
string folder = "output/OpenWorld/DugoutReadability";
System.IO.Directory.CreateDirectory(folder);
if (System.IO.File.Exists(folder + "/applied.txt")) throw new Exception("이미 적용했습니다.");
var root = GameObject.Find("AuthoredEnvironment/월드 구간/PoorVillage").transform;
var targets = new[] { root.Find("가뭄 마을 주거/PoorHouse_Dugout"), root.Find("남쪽 마을 주거/PoorHouse_Dugout") };
var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Environment/PoorVillage/PoorHouse_Hut.prefab");
if (prefab == null || targets.Any(t => t == null)) throw new Exception("대상이 변경되었습니다.");
foreach (var t in targets)
    if (t.GetComponentsInChildren<MonoBehaviour>(true).Length != 0) throw new Exception("환경 외 컴포넌트가 있습니다.");
var ids = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).ToArray();
Func<string> identities = () => string.Join("\n", ids.Select(id => id.Guid + "|" + id.transform.position.ToString("F4")).OrderBy(s => s));
string baseline = identities();
System.IO.File.WriteAllText(folder + "/identities-before.txt", baseline);
System.IO.File.WriteAllLines(folder + "/before.txt", targets.Select(t => GlobalObjectId.GetGlobalObjectIdSlow(t.gameObject) + "|" + EditorJsonUtility.ToJson(t)));
var settings = new PrefabReplacingSettings { objectMatchMode = ObjectMatchMode.ByName,
    prefabOverridesOptions = PrefabOverridesOptions.ClearAllNonDefaultOverrides, changeRootNameToAssetName = false };
var terrain = Terrain.activeTerrain;
Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
Undo.SetCurrentGroupName("움집을 초가집으로 교체");
var rows = new List<string>();
try
{
    foreach (var t in targets)
    {
        Vector3 position = t.position, scale = t.localScale; Quaternion rotation = t.rotation;
        PrefabUtility.ReplacePrefabAssetOfPrefabInstance(t.gameObject, prefab, settings, InteractionMode.UserAction);
        Undo.RecordObjects(new UnityEngine.Object[] { t, t.gameObject }, "초가집 접지");
        t.SetPositionAndRotation(position, rotation); t.localScale = scale;
        t.name = "PoorHouse_Hut_Annex";
        var foundation = t.GetComponentsInChildren<MeshRenderer>(true).Single(r => r.name.Contains("Foundation_Stone"));
        var bounds = foundation.bounds; float low = float.MaxValue;
        for (int z = 0; z < 5; z++) for (int x = 0; x < 5; x++)
        {
            var point = new Vector3(Mathf.Lerp(bounds.min.x, bounds.max.x, x / 4f), 0, Mathf.Lerp(bounds.min.z, bounds.max.z, z / 4f));
            low = Mathf.Min(low, terrain.SampleHeight(point) + terrain.transform.position.y);
        }
        t.position += Vector3.up * (low - bounds.min.y - .03f);
        PrefabUtility.RecordPrefabInstancePropertyModifications(t);
        PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
        if (t.GetComponentsInChildren<MeshCollider>(true).Length != 5) throw new Exception("교체 모델 충돌 불일치");
        rows.Add(t.parent.name + "/" + t.name + " position=" + t.position.ToString("F4"));
    }
    Physics.SyncTransforms();
    if (identities() != baseline) throw new Exception("진행 개체 변경");
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)) throw new Exception("씬 저장 실패");
    Undo.CollapseUndoOperations(group);
    System.IO.File.WriteAllLines(folder + "/applied.txt", rows);
    return string.Join("\n", rows);
}
catch { Undo.RevertAllDownToGroup(group); throw; }
