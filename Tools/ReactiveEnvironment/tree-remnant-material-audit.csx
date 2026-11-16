// 재질 슬롯 순서 대신 구매 원본 Renderer의 실제 재질과 대조한다. 모든 LOD와 현재 Play 복제본을 검사한다.
var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.ReactiveEnvironment.VegetationReactionCatalogSO>("Assets/10.Datas/Environment/Vegetation/Catalog.asset");
var variants = new[] { "A", "B", "C", "C_Red", "D" };
var errors = new System.Collections.Generic.List<string>();
int assetSlots = 0, liveSlots = 0, liveTrees = 0;
var liveMaps = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, Material[]>>();
foreach (string variant in variants)
{
    var mapping = catalog.surfaces.ToDictionary(s => s.original, s => s.reactive);
    if (variant == "C_Red")
    {
        string[] from = { "8f12a94af1abc5a42b0bd7f6446cf24f", "59c9140853894a949915d5f7c61f8685" };
        string[] to = { "45503504b4785644ebbdf66ed582a590", "c0e1531a20f408145adefb353fb4f8b3" };
        for (int i = 0; i < from.Length; i++) mapping[UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(UnityEditor.AssetDatabase.GUIDToAssetPath(from[i]))] = mapping[UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(UnityEditor.AssetDatabase.GUIDToAssetPath(to[i]))];
    }
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Environment/Reactive/TreeRemnants/TreeRemnant_" + variant + ".prefab");
    var byPath = new System.Collections.Generic.Dictionary<string, Material[]>();
    foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
    {
        var source = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(renderer);
        if (source == null) throw new System.Exception("원본 Renderer 누락: " + renderer.name);
        var expected = source.sharedMaterials.Select(m => mapping[m]).ToArray();
        var actual = renderer.sharedMaterials;
        string relative = UnityEditor.AnimationUtility.CalculateTransformPath(renderer.transform, prefab.transform);
        byPath.Add(relative, expected); assetSlots += expected.Length;
        if (!actual.SequenceEqual(expected)) errors.Add("자산 " + variant + "/" + relative);
        foreach (var material in expected) if (!UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentSurface.SupportsReaction(material)) errors.Add("환경 셰이더 누락 " + material.name);
    }
    liveMaps.Add("Tree_" + variant, byPath);
}
var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("LakeVillageWorld");
foreach (var root in scene.GetRootGameObjects())
foreach (var target in root.GetComponentsInChildren<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject>(true))
{
    if (target.Definition == null || !liveMaps.ContainsKey(target.Definition.name)) continue;
    liveTrees++;
    var remnant = target.transform.Find("Remnant");
    foreach (var renderer in remnant.GetComponentsInChildren<Renderer>(true))
    {
        string relative = UnityEditor.AnimationUtility.CalculateTransformPath(renderer.transform, remnant);
        var expected = liveMaps[target.Definition.name][relative]; liveSlots += expected.Length;
        if (!renderer.sharedMaterials.SequenceEqual(expected)) errors.Add("배치 " + target.name + "/" + relative);
    }
}
var summary = new { assetSlots = assetSlots, liveTrees = liveTrees, liveSlots = liveSlots, mismatches = errors.Count, errors = errors.ToArray(), playing = Application.isPlaying, sceneDirty = scene.isDirty };
var serialize = System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject", new[] { typeof(object) });
System.IO.File.WriteAllText("output/ReactiveEnvironment/TreeDestruction-20261115/material-fix/audit.json", (string)serialize.Invoke(null, new object[] { summary }));
if (errors.Count > 0) throw new System.Exception("잔해 재질 검사 실패: " + string.Join(";", errors.Take(10).ToArray()));
return summary;
