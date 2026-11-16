// 원형 보존 비교를 위한 현재 스냅샷과 실제 Unity 구성 검사 결과를 기록한다.
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || UnityEditor.EditorApplication.isCompiling) return "BUSY";
var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity");
if (!scene.IsValid() || !scene.isLoaded || scene.isDirty) return "BUSY scene";
var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject>(true)).ToArray();
var errors = new System.Collections.Generic.List<string>();
var awake = typeof(UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
foreach (var target in all)
{
    awake.Invoke(target, null);
    if (!target.IsAuthoringValid()) errors.Add(target.name + ": 구성 오류");
    if (UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(target.gameObject) != 0) errors.Add(target.name + ": 스크립트 누락");
    if (target.Definition.reaction.canBreak)
    {
        var intact = target.transform.Find("Intact"); var remnant = target.transform.Find("Remnant");
        if (intact == null || remnant == null || !intact.gameObject.activeSelf || remnant.gameObject.activeSelf) errors.Add(target.name + ": 원형 활성 오류");
        if (!target.Definition.destruction.useLocalDimensions) errors.Add(target.name + ": 치수 좌표계 오류");
    }
}
var snapshots = all.Select(t => new {
    guid = t.GetComponent<UPlayGround.Components.SceneEntityId>().Guid,
    globalId = UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(t.gameObject).ToString(),
    name = t.name, definition = UnityEditor.AssetDatabase.GetAssetPath(t.Definition),
    position = t.transform.localPosition.ToString("R"), rotation = t.transform.localRotation.ToString("R"), scale = t.transform.localScale.ToString("R"), active = t.gameObject.activeSelf,
    surfaces = (t.Definition.reaction.canBreak ? t.transform.Find("Intact") : t.transform).GetComponentsInChildren<Renderer>(true).Select(r => new {
        id = UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(r).ToString(), matrix = r.transform.localToWorldMatrix.ToString("R"), active = r.gameObject.activeSelf,
        mesh = r.GetComponent<MeshFilter>() != null ? UnityEditor.AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>().sharedMesh) + ":" + r.GetComponent<MeshFilter>().sharedMesh.name : "",
        materials = r.sharedMaterials.Select(UnityEditor.AssetDatabase.GetAssetPath).ToArray()
    }).ToArray()
}).ToArray();
var summary = new {
    targets = all.Length, breakable = all.Count(t => t.Definition.reaction.canBreak),
    protectedTargets = all.Count(t => t.Definition.name == "ProtectedVegetation"),
    protectedBreakable = all.Count(t => t.Definition.name == "ProtectedVegetation" && t.Definition.reaction.canBreak),
    duplicateIds = all.GroupBy(t => t.GetComponent<UPlayGround.Components.SceneEntityId>().Guid).Count(g => string.IsNullOrWhiteSpace(g.Key) || g.Count() != 1),
    variants = all.Where(t => t.Definition.reaction.canBreak).GroupBy(t => t.Definition.name).Select(g => new { name = g.Key, count = g.Count() }).ToArray(),
    errors = errors.ToArray(), dirty = scene.isDirty
};
var json = System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject", new[] { typeof(object) });
string folder = "output/ReactiveEnvironment/TreeDestruction-20261115/";
System.IO.Directory.CreateDirectory(folder);
System.IO.File.WriteAllText(folder + "current.json", (string)json.Invoke(null, new object[] { snapshots }));
System.IO.File.WriteAllText(folder + "authoring-audit.json", (string)json.Invoke(null, new object[] { summary }));
return summary;
