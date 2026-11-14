var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.ReactiveEnvironment.VegetationReactionCatalogSO>("Assets/10.Datas/Environment/Vegetation/Catalog.asset");
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var plants = UPlayGround.EditorTools.VegetationReactionAuthoring.Collect(scene, catalog);
var report = new System.Text.StringBuilder();
var ids = new System.Collections.Generic.HashSet<string>();
var awake = typeof(UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
int connected = 0, valid = 0, missing = 0, duplicate = 0, protectedCount = 0, active = 0;
foreach (var plant in plants)
{
    var reaction = plant.root.GetComponent<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject>();
    string path = UnityEditor.AnimationUtility.CalculateTransformPath(plant.root.transform, null);
    if (reaction == null) { missing++; report.AppendLine("연결 누락: " + path); continue; }
    connected++;
    if (plant.root.activeInHierarchy) active++;
    if (!reaction.Definition.reaction.canIgnite) protectedCount++;
    var identity = plant.root.GetComponent<UPlayGround.Components.SceneEntityId>();
    if (identity == null || !identity.HasGuid || !ids.Add(identity.Guid)) { duplicate++; report.AppendLine("ID 누락/중복: " + path); }
    if (!UnityEngine.Application.isPlaying) awake.Invoke(reaction, null);
    if (reaction.IsAuthoringValid()) valid++;
    else report.AppendLine("구성 실패: " + path + "\n" + UPlayGround.EditorTools.ReactiveEnvironmentAuthoring.Preview(plant.root, reaction.Definition));
}
int terrainCount = 0, terrainValid = 0, layers = 0;
foreach (var root in scene.GetRootGameObjects())
foreach (var terrain in root.GetComponentsInChildren<UnityEngine.Terrain>(true))
{
    terrainCount++;
    var reaction = terrain.GetComponent<UPlayGround.Gameplay.World.ReactiveEnvironment.TerrainDetailReaction>();
    if (reaction == null) { report.AppendLine("Terrain 어댑터 누락"); continue; }
    string error = reaction.ValidateAuthoring();
    if (error == null) terrainValid++; else report.AppendLine("Terrain 구성 실패: " + error);
    var serialized = new UnityEditor.SerializedObject(reaction);
    layers += serialized.FindProperty("_detailLayers").arraySize;
    report.AppendLine("Terrain=" + terrain.name + " layers=" + layers + " region=" + reaction.CellRegion + " pixelsPerCell=" + reaction.DetailPixelsPerCell + " runtimeCells=" + reaction.CellCount);
}
report.Insert(0, "candidates=" + plants.Count + " connected=" + connected + " valid=" + valid + " active=" + active + " protected=" + protectedCount + " missing=" + missing + " duplicate=" + duplicate + " terrain=" + terrainValid + "/" + terrainCount + " layers=" + layers + "\n");
System.IO.File.WriteAllText("output/ReactiveEnvironment/VegetationConversion-20261114/connection-audit.txt", report.ToString());
return report.ToString();
