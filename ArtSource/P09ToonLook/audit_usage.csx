// 실제 씬과 액터 등록 데이터에서 P09 NPC 참조를 조사한다. 에셋을 변경하지 않는다.
var candidates = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/03.Prefabs" })
    .Select(AssetDatabase.GUIDToAssetPath)
    .Where(p => p.Contains("/Npc/") || p.Contains("/NPC/") || p.Contains("Residents/") || p.Contains("/WorldCities/") || p.Contains("/NpcVariants/") || p.Contains("/Farming/"))
    .ToArray();
var roots = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/01.Scenes" }).Select(AssetDatabase.GUIDToAssetPath)
    .Where(p => p.Contains("/Scenario/")).ToArray();
var lines = new List<string>();
var used = new List<string>();
var dependencies = roots.ToDictionary(p => p, p => new HashSet<string>(AssetDatabase.GetDependencies(p, true)));
foreach (string path in candidates)
{
    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
    if (prefab.GetComponent<UPlayGround.NpcActor>() == null) continue;
    var renderers = prefab.GetComponentsInChildren<Renderer>(true);
    if (!renderers.Any(r => r.sharedMaterials.Any(m => m != null && AssetDatabase.GetAssetPath(m).Contains("P09")))) continue;
    var references = roots.Where(s => dependencies[s].Contains(path)).ToArray();
    lines.Add(path + "|" + AssetDatabase.AssetPathToGUID(path) + "|" + string.Join(";", references));
    if (references.Length > 0) used.Add(path);
    foreach (var renderer in renderers)
    {
        bool active = renderer.enabled;
        for (var t = renderer.transform; t != null; t = t.parent) active &= t.gameObject.activeSelf;
        if (active) lines.Add("  " + renderer.name + "|" + string.Join(";", renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath).ToArray()));
    }
}
System.IO.Directory.CreateDirectory("output/P09ToonLook");
System.IO.File.WriteAllLines("output/P09ToonLook/npc-usage.txt", lines.ToArray());
System.IO.File.WriteAllLines("output/P09ToonLook/used-npcs.txt", used.Distinct().ToArray());
return new { used = used.Distinct().ToArray(), candidates = candidates.Length };
