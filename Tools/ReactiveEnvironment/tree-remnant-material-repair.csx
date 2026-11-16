// 프로젝트 잔해의 재질만 원본 참조에 맞춘다. Play 중이면 현재 복제본에도 같은 재질을 적용하며 씬/저장은 쓰지 않는다.
if (UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating) return "BUSY";
var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.ReactiveEnvironment.VegetationReactionCatalogSO>("Assets/10.Datas/Environment/Vegetation/Catalog.asset");
var variants = new[] { "A", "B", "C", "C_Red", "D" };
string folder = "output/ReactiveEnvironment/TreeDestruction-20261115/material-fix/";
System.IO.Directory.CreateDirectory(folder + "prefabs-before");
var changedAssets = new System.Collections.Generic.List<string>();
var report = new System.Collections.Generic.List<object>();
var liveMaps = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, Material[]>>();
int slotChanges = 0, checkedSlots = 0;
try
{
    foreach (string variant in variants)
    {
        var mapping = catalog.surfaces.ToDictionary(s => s.original, s => s.reactive);
        if (variant == "C_Red")
        {
            string[] from = { "8f12a94af1abc5a42b0bd7f6446cf24f", "59c9140853894a949915d5f7c61f8685" };
            string[] to = { "45503504b4785644ebbdf66ed582a590", "c0e1531a20f408145adefb353fb4f8b3" };
            for (int i = 0; i < from.Length; i++)
                mapping[UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(UnityEditor.AssetDatabase.GUIDToAssetPath(from[i]))] = mapping[UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(UnityEditor.AssetDatabase.GUIDToAssetPath(to[i]))];
        }
        string path = "Assets/03.Prefabs/Environment/Reactive/TreeRemnants/TreeRemnant_" + variant + ".prefab";
        var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
        var expectedByPath = new System.Collections.Generic.Dictionary<string, Material[]>();
        int changes = 0;
        try
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var source = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(renderer);
                if (source == null || !UnityEditor.AssetDatabase.GetAssetPath(source).StartsWith("Assets/ExternalAssets/")) throw new System.Exception("구매 원본 Renderer 식별 실패: " + renderer.name);
                var expected = source.sharedMaterials.Select(m => mapping[m]).ToArray();
                var previous = renderer.sharedMaterials;
                if (previous.Length != expected.Length) throw new System.Exception("원본과 슬롯 수가 다름");
                int differences = previous.Where((m, i) => m != expected[i]).Count();
                checkedSlots += expected.Length; changes += differences;
                string relative = UnityEditor.AnimationUtility.CalculateTransformPath(renderer.transform, root.transform);
                expectedByPath.Add(relative, expected);
                report.Add(new { variant = variant, renderer = relative, source = UnityEditor.AssetDatabase.GetAssetPath(source), before = previous.Select(UnityEditor.AssetDatabase.GetAssetPath).ToArray(), after = expected.Select(UnityEditor.AssetDatabase.GetAssetPath).ToArray(), differences = differences });
                if (differences > 0) renderer.sharedMaterials = expected;
            }
            if (changes > 0)
            {
                string backup = folder + "prefabs-before/TreeRemnant_" + variant + ".prefab";
                if (!System.IO.File.Exists(backup)) { System.IO.File.Copy(path, backup); System.IO.File.Copy(path + ".meta", backup + ".meta"); }
                changedAssets.Add(path);
                if (UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path) == null) throw new System.Exception("수정 프리팹 저장 실패");
                slotChanges += changes;
            }
            liveMaps.Add("Tree_" + variant, expectedByPath);
        }
        finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
    }
}
catch
{
    foreach (string path in changedAssets)
    {
        System.IO.File.Copy(folder + "prefabs-before/" + System.IO.Path.GetFileName(path), path, true);
        UnityEditor.AssetDatabase.ImportAsset(path);
    }
    throw;
}
int liveRenderers = 0, liveSlotChanges = 0;
if (Application.isPlaying)
{
    var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("LakeVillageWorld");
    foreach (var root in scene.GetRootGameObjects())
    foreach (var target in root.GetComponentsInChildren<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject>(true))
    {
        if (target.Definition == null || !liveMaps.ContainsKey(target.Definition.name)) continue;
        var remnant = target.transform.Find("Remnant"); if (remnant == null) throw new System.Exception("실행 잔해 누락");
        foreach (var renderer in remnant.GetComponentsInChildren<Renderer>(true))
        {
            string relative = UnityEditor.AnimationUtility.CalculateTransformPath(renderer.transform, remnant);
            var expected = liveMaps[target.Definition.name][relative]; var previous = renderer.sharedMaterials;
            int differences = previous.Where((m, i) => m != expected[i]).Count();
            if (differences == 0) continue;
            renderer.sharedMaterials = expected; liveRenderers++; liveSlotChanges += differences;
        }
    }
}
var summary = new { variants = variants.Length, checkedSlots = checkedSlots, changedAssets = changedAssets.Count, assetSlotChanges = slotChanges, liveRenderers = liveRenderers, liveSlotChanges = liveSlotChanges, playing = Application.isPlaying, details = report };
var json = System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject", new[] { typeof(object) });
System.IO.File.WriteAllText(folder + "repair.json", (string)json.Invoke(null, new object[] { summary }));
return new { checkedSlots = checkedSlots, changedAssets = changedAssets.Count, assetSlotChanges = slotChanges, liveRenderers = liveRenderers, liveSlotChanges = liveSlotChanges, playing = Application.isPlaying };
