var failed = typeof(UnityEditor.EditorUtility).GetProperty("scriptCompilationFailed", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
if (UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || failed != null && (bool)failed.GetValue(null,null)) return "BUSY";
foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
{
    var type = assembly.GetType("UnityEditor.TestTools.TestRunner.Api.TestRunnerApi");
    if (type == null) continue;
    var active = type.GetMethod("IsTestExecutionActive", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
    if (active != null && (bool)active.Invoke(null,null)) return "BUSY tests";
}
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var timer = System.Diagnostics.Stopwatch.StartNew();
var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.ReactiveEnvironment.VegetationReactionCatalogSO>("Assets/10.Datas/Environment/Vegetation/Catalog.asset");
if (scene.isDirty || UnityEditor.AssetDatabase.AssetPathToGUID(scene.path) != catalog.sceneGuid) return "BUSY scene";
var weatherList = new System.Collections.Generic.List<UPlayGround.Gameplay.World.LivingWorld.EnvironmentController>();
foreach (var root in scene.GetRootGameObjects()) weatherList.AddRange(root.GetComponentsInChildren<UPlayGround.Gameplay.World.LivingWorld.EnvironmentController>(true));
if (weatherList.Count != 1) throw new System.InvalidOperationException("실제 날씨 작성자 한 개가 필요합니다.");
var plants = UPlayGround.EditorTools.VegetationReactionAuthoring.Collect(scene,catalog);
var batch = new System.Collections.Generic.List<UPlayGround.EditorTools.VegetationReactionAuthoring.Plant>();
int connected = 0;
foreach (var plant in plants)
{
    if (plant.root.GetComponent<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject>() != null) connected++;
    else if (batch.Count < 1000) batch.Add(plant);
}
string folder = "output/ReactiveEnvironment/VegetationConversion-20261114/";
if (!System.IO.File.Exists(folder + "root-before.tsv"))
{
    var before = new System.Text.StringBuilder();
    foreach (var plant in plants)
    {
        var t = plant.root.transform;
        var id = plant.root.GetComponent<UPlayGround.Components.SceneEntityId>();
        before.AppendLine(UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(plant.root) + "\t" + (id == null ? "" : id.Guid) + "\t" + t.localPosition.ToString("R") + "\t" + t.localRotation.ToString("R") + "\t" + t.localScale.ToString("R") + "\t" + plant.root.activeSelf);
    }
    System.IO.File.WriteAllText(folder + "root-before.tsv", before.ToString());
}
UnityEditor.EditorApplication.LockReloadAssemblies();
UnityEditor.AssetDatabase.DisallowAutoRefresh();
UnityEditor.Undo.IncrementCurrentGroup();
int group = UnityEditor.Undo.GetCurrentGroup();
try
{
    System.IO.File.WriteAllText(folder + "batch-phase.txt", "적용 시작 " + System.DateTime.UtcNow.ToString("o"));
    int changed = UPlayGround.EditorTools.VegetationReactionAuthoring.ApplyPlants(batch,catalog,weatherList[0]);
    long appliedMs = timer.ElapsedMilliseconds;
    System.IO.File.WriteAllText(folder + "batch-phase.txt", "구성 검사 " + System.DateTime.UtcNow.ToString("o"));
    var awake = typeof(UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject).GetMethod("Awake",System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
    foreach (var plant in batch)
    {
        var reaction = plant.root.GetComponent<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject>();
        awake.Invoke(reaction,null);
        if (!reaction.IsAuthoringValid()) throw new System.InvalidOperationException("식생 구성 오류: " + UnityEditor.AnimationUtility.CalculateTransformPath(plant.root.transform,null));
        var collider = plant.root.transform.Find("EnvironmentReactionHit").GetComponent<UnityEngine.BoxCollider>();
        if (collider.size.x <= 0 || collider.size.y <= 0 || collider.size.z <= 0) throw new System.InvalidOperationException("입력 형상 부피 오류");
    }
    bool terrainAdded = false;
    if (batch.Count == 0)
        foreach (var root in scene.GetRootGameObjects())
        foreach (var terrain in root.GetComponentsInChildren<UnityEngine.Terrain>(true))
            if (terrain.GetComponent<UPlayGround.Gameplay.World.ReactiveEnvironment.TerrainDetailReaction>() == null)
            {
                UPlayGround.EditorTools.VegetationReactionAuthoring.ApplyTerrain(terrain,catalog,weatherList[0]);
                terrainAdded = true;
            }
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    long validatedMs = timer.ElapsedMilliseconds;
    System.IO.File.WriteAllText(folder + "batch-phase.txt", "씬 저장 " + System.DateTime.UtcNow.ToString("o"));
    if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)) throw new System.InvalidOperationException("본편 씬 저장 실패");
    var ids = new System.Text.StringBuilder();
    foreach (var plant in batch) ids.AppendLine(UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(plant.root) + "\t" + plant.root.GetComponent<UPlayGround.Components.SceneEntityId>().Guid);
    System.IO.File.AppendAllText(folder + "converted-ids.tsv",ids.ToString());
    UnityEditor.Undo.CollapseUndoOperations(group);
    string result = "connected=" + (connected + changed) + "/" + plants.Count + " changed=" + changed + " terrainAdded=" + terrainAdded + " dirty=" + scene.isDirty + " applyMs=" + appliedMs + " validateMs=" + (validatedMs-appliedMs) + " saveMs=" + (timer.ElapsedMilliseconds-validatedMs);
    System.IO.File.WriteAllText(folder + "batch-latest.txt",result);
    System.IO.File.AppendAllText(folder + "batch-log.txt",System.DateTime.UtcNow.ToString("o") + " " + result + "\n");
    return result;
}
catch (System.Exception error)
{
    UnityEditor.Undo.RevertAllDownToGroup(group);
    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
    System.IO.File.WriteAllText(folder + "batch-failure.txt",error.ToString());
    throw;
}
finally
{
    UnityEditor.AssetDatabase.AllowAutoRefresh();
    UnityEditor.EditorApplication.UnlockReloadAssemblies();
    System.IO.File.WriteAllText(folder + "batch-released.txt",System.DateTime.UtcNow.ToString("o"));
}

