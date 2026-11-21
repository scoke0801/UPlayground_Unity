// Unity MCP 읽기 전용 감사. 전체 카탈로그에서 추출한 분류별 경로를 조사한다.
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || UnityEditor.EditorApplication.isCompiling
    || UnityEditor.EditorApplication.isUpdating) return "BUSY";
const string output = "output/ReactiveEnvironment/AllTreeAudit/";
var summary = new System.Collections.Generic.List<object>();
var serializer = System.AppDomain.CurrentDomain.GetAssemblies().First(assembly => assembly.GetName().Name == "Newtonsoft.Json")
    .GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});
foreach(string category in new[]{"tree","prop"})
{
    var paths = System.IO.File.ReadAllLines(output + category + "-paths.txt").Where(path=>path.Length>0).ToArray();
    var result = new System.Collections.Generic.List<object>();
    foreach(string path in paths)
    {
        var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(prefab==null) { result.Add(new { path, loaded=false }); continue; }
        var renderers=prefab.GetComponentsInChildren<Renderer>(true);
        var topology=prefab.GetComponentsInChildren<MeshFilter>(true).Select(filter=>{
            string guid;
            long localId;
            return filter.sharedMesh!=null && UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(filter.sharedMesh,out guid,out localId)
                ? guid+":"+localId : "";
        }).Where(id=>id.Length>0).OrderBy(id=>id,System.StringComparer.Ordinal).ToArray();
        var reactions=prefab.GetComponentsInChildren<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject>(true);
        result.Add(new {
            path, loaded=true, renderers=renderers.Length, lods=prefab.GetComponentsInChildren<LODGroup>(true).Length,
            colliders=prefab.GetComponentsInChildren<Collider>(true).Length,
            gathering=prefab.GetComponentsInChildren<UPlayGround.GatheringActor>(true).Length,
            reactive=reactions.Length,
            breakable=reactions.Any(reaction=>reaction.Definition!=null && reaction.Definition.reaction.canBreak),
            shaders=renderers.SelectMany(renderer=>renderer.sharedMaterials).Where(material=>material!=null)
                .Select(material=>material.shader.name).Distinct().OrderBy(name=>name).ToArray(),
            meshes=topology
        });
    }
    System.IO.File.WriteAllText(output + "unity-" + category + "-prefabs.json",(string)serializer.Invoke(null,new object[]{result}));
    summary.Add(new { category, prefabs=result.Count });
}
return summary;
