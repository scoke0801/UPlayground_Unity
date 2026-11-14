var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty) return "BUSY scene";
string folder = "output/ReactiveEnvironment/VegetationConversion-20261114/";
var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.ReactiveEnvironment.VegetationReactionCatalogSO>("Assets/10.Datas/Environment/Vegetation/Catalog.asset");
var plants = UPlayGround.EditorTools.VegetationReactionAuthoring.Collect(scene,catalog);
var baseline = new System.Collections.Generic.Dictionary<string,string[]>();
foreach (string line in System.IO.File.ReadAllLines(folder + "root-before.tsv"))
{
    if (string.IsNullOrWhiteSpace(line)) continue;
    var columns = line.Split('\t'); baseline.Add(columns[0],columns);
}
var assigned = new System.Collections.Generic.Dictionary<string,string>();
foreach (string line in System.IO.File.ReadAllLines(folder + "converted-ids.tsv"))
{
    if (string.IsNullOrWhiteSpace(line)) continue;
    var columns = line.Split('\t'); assigned.Add(columns[0],columns[1]);
}
int transformChanged = 0, activeChanged = 0, existingIdChanged = 0, assignedIdChanged = 0, missing = 0;
var report = new System.Text.StringBuilder();
foreach (var plant in plants)
{
    string key = UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(plant.root).ToString();
    string[] columns;
    if (!baseline.TryGetValue(key,out columns)) { missing++; report.AppendLine("원래 배치 누락: " + key); continue; }
    var t = plant.root.transform;
    if (columns[2] != t.localPosition.ToString("R") || columns[3] != t.localRotation.ToString("R") || columns[4] != t.localScale.ToString("R")) { transformChanged++; report.AppendLine("변환 변경: " + key); }
    if (columns[5] != plant.root.activeSelf.ToString()) { activeChanged++; report.AppendLine("활성 변경: " + key); }
    var id = plant.root.GetComponent<UPlayGround.Components.SceneEntityId>();
    string value = id == null ? "" : id.Guid;
    if (!string.IsNullOrEmpty(columns[1]) && columns[1] != value) { existingIdChanged++; report.AppendLine("기존 ID 변경: " + key); }
    string expected;
    if (!assigned.TryGetValue(key,out expected) || expected != value) { assignedIdChanged++; report.AppendLine("전환 ID 누락/변경: " + key); }
}
report.Insert(0,"roots=" + plants.Count + " baseline=" + baseline.Count + " assigned=" + assigned.Count + " transformChanged=" + transformChanged + " activeChanged=" + activeChanged + " existingIdChanged=" + existingIdChanged + " assignedIdChanged=" + assignedIdChanged + " missing=" + missing + " dirty=" + scene.isDirty + "\n");
System.IO.File.WriteAllText(folder + "preservation-audit.txt",report.ToString());
return report.ToString();

