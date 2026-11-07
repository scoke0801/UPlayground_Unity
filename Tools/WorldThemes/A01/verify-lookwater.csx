// 전용 사본 전체 데이터와 현재 Host를 비교한다. 원본은 읽기만 한다.
var source = UnityEditor.AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/10.Datas/World/OpenWorld/Terrain.asset");
var copy = UnityEditor.AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/10.Datas/World/ThemeStudies/LookWater/Terrain.asset");
if (source == null || copy == null) throw new Exception("TerrainData 누락");
long heightChanged = 0, alphaChanged = 0, detailChanged = 0;
float maxHeightDelta = 0f, maxAlphaDelta = 0f;
var h1 = source.GetHeights(0,0,source.heightmapResolution,source.heightmapResolution);
var h2 = copy.GetHeights(0,0,copy.heightmapResolution,copy.heightmapResolution);
for (int z = 0; z < h1.GetLength(0); z++) for (int x = 0; x < h1.GetLength(1); x++)
{
    float delta = Mathf.Abs(h1[z,x]-h2[z,x]); if (delta > 0f) heightChanged++; maxHeightDelta = Mathf.Max(maxHeightDelta,delta);
}
h1 = null; h2 = null;
var a1 = source.GetAlphamaps(0,0,source.alphamapWidth,source.alphamapHeight);
var a2 = copy.GetAlphamaps(0,0,copy.alphamapWidth,copy.alphamapHeight);
for (int z = 0; z < a1.GetLength(0); z++) for (int x = 0; x < a1.GetLength(1); x++) for (int l = 0; l < a1.GetLength(2); l++)
{
    float delta = Mathf.Abs(a1[z,x,l]-a2[z,x,l]); if (delta > 0f) alphaChanged++; maxAlphaDelta = Mathf.Max(maxAlphaDelta,delta);
}
a1 = null; a2 = null;
var detailSums = new List<object>();
for (int l = 0; l < source.detailPrototypes.Length; l++)
{
    var d1 = source.GetDetailLayer(0,0,source.detailWidth,source.detailHeight,l);
    var d2 = copy.GetDetailLayer(0,0,copy.detailWidth,copy.detailHeight,l);
    long sum1 = 0, sum2 = 0, changed = 0;
    for (int z = 0; z < d1.GetLength(0); z++) for (int x = 0; x < d1.GetLength(1); x++)
    { sum1 += d1[z,x]; sum2 += d2[z,x]; if(d1[z,x] != d2[z,x]) changed++; }
    detailChanged += changed;
    bool prototypeEqual = UnityEngine.JsonUtility.ToJson(source.detailPrototypes[l]) == UnityEngine.JsonUtility.ToJson(copy.detailPrototypes[l]);
    detailSums.Add(new { layer = l, source = sum1, copy = sum2, changed = changed, prototypeEqual = prototypeEqual });
}
bool sharedAlpha = source.alphamapTextures.Any(t => copy.alphamapTextures.Contains(t));
var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/01.Scenes/ArtReview/WorldThemes/Theme_LookWater.unity");
var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
int missing = all.Sum(t => UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
int content = all.Sum(t => t.GetComponents<MonoBehaviour>().Count(c => c != null && (c.GetType().Name == "SceneEntityId" || c.GetType().Name == "SceneContext" || c.GetType().Name.EndsWith("Actor"))));
var materials = all.SelectMany(t => t.GetComponents<Renderer>()).SelectMany(r => r.sharedMaterials).ToArray();
int nullMaterials = materials.Count(m => m == null);
int unsupported = materials.Count(m => m != null && (m.shader == null || !m.shader.isSupported));
var report = new { heightChanged = heightChanged, maxHeightDelta = maxHeightDelta, alphaChanged = alphaChanged, maxAlphaDelta = maxAlphaDelta, detailChanged = detailChanged, detail = detailSums, scatterEqual = source.detailScatterMode == copy.detailScatterMode, sharedInternalAlphaTextures = sharedAlpha, treeCountEqual = source.treeInstances.Length == copy.treeInstances.Length, missingScripts = missing, forbiddenContentComponents = content, nullMaterials = nullMaterials, unsupportedShaders = unsupported, status = heightChanged == 0 && alphaChanged == 0 && detailChanged == 0 && !sharedAlpha && missing == 0 && content == 0 && nullMaterials == 0 && unsupported == 0 ? "통과" : "실패" };
var jsonType = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Newtonsoft.Json.JsonConvert")).First(t => t != null);
var serialize = jsonType.GetMethod("SerializeObject",new Type[]{typeof(object)});
string result = (string)serialize.Invoke(null,new object[]{report});
System.IO.File.WriteAllText("output/WorldThemeProduction/A01/data-validation.json",result);
return result;
