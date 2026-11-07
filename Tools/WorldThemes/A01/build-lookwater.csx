// A00 lease 아래 열린 Host를 보존한 채 독립 비교실만 만든다.
var sourceScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || sourceScene.path != "Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")
    throw new Exception("A00 기준선 확보 후 Host Edit Mode에서 실행해야 합니다.");
string scenePath = "Assets/01.Scenes/ArtReview/WorldThemes/Theme_LookWater.unity";
string dataFolder = "Assets/10.Datas/World/ThemeStudies/LookWater";
string output = "output/WorldThemeProduction/A01";
if (System.IO.File.Exists(scenePath) || System.IO.File.Exists(dataFolder + "/Terrain.asset"))
    throw new Exception("기존 A01 산출물이 있어 덮어쓰지 않았습니다.");
var jsonType = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Newtonsoft.Json.JsonConvert")).First(t => t != null);
var deserialize = jsonType.GetMethod("DeserializeObject", new Type[] { typeof(string), typeof(Type) });
Func<string, Type, object> read = (text, type) => deserialize.Invoke(null, new object[] { text, type });
var plan = (Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/WorldThemes/A01/look-trials.json"), typeof(Dictionary<string,object>));
var foliageTrials = (Dictionary<string,object>)read(plan["foliageTrials"].ToString(), typeof(Dictionary<string,object>));
var foliage = ((string[])read(foliageTrials["treeCandidates"].ToString(), typeof(string[]))).Concat((string[])read(foliageTrials["midgroundCandidates"].ToString(), typeof(string[]))).ToArray();
foreach (string path in foliage)
{
    var candidate = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
    if (candidate == null) throw new Exception("후보 누락: " + path);
    if (candidate.GetComponentsInChildren<MonoBehaviour>(true).Length > 0) throw new Exception("후보 스크립트 수동 검토 필요: " + path);
}
System.IO.Directory.CreateDirectory(output);
System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(scenePath));
System.IO.Directory.CreateDirectory(dataFolder);
var sourceRoots = sourceScene.GetRootGameObjects();
var sourceTerrain = sourceRoots.SelectMany(g => g.GetComponentsInChildren<Terrain>(true)).Single(t => t.terrainData != null);
var sourceData = sourceTerrain.terrainData;
string sourcePath = UnityEditor.AssetDatabase.GetAssetPath(sourceData);
Func<string,string> hash = p => { using (var sha = System.Security.Cryptography.SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(System.IO.File.ReadAllBytes(p))).Replace("-", ""); };
string sourceHash = hash(sourcePath);
string sourceSceneHash = hash(sourceScene.path);
bool sourceDirty = sourceScene.isDirty;
var sourceAlpha = sourceData.GetAlphamaps(0, 0, 1, 1);
var sourceDetail = sourceData.GetDetailLayer(0, 0, 1, 1, 0);
var sourceHeight = sourceData.GetHeights(0, 0, 1, 1);
var sourceTextureIds = sourceData.alphamapTextures.Select(t => t.GetInstanceID()).ToArray();
var sourceCamera = sourceRoots.SelectMany(g => g.GetComponentsInChildren<Camera>(true)).FirstOrDefault(c => c.CompareTag("MainCamera"));
if (sourceCamera == null) throw new Exception("Host 카메라가 없습니다.");
var sourceSun = sourceRoots.SelectMany(g => g.GetComponentsInChildren<Light>(true)).FirstOrDefault(l => l.type == LightType.Directional && l.isActiveAndEnabled);
if (sourceSun == null) throw new Exception("활성 태양이 없습니다.");
bool fog = RenderSettings.fog;
FogMode fogMode = RenderSettings.fogMode;
float fogStart = RenderSettings.fogStartDistance, fogEnd = RenderSettings.fogEndDistance, fogDensity = RenderSettings.fogDensity;
Color fogColor = RenderSettings.fogColor, sky = RenderSettings.ambientSkyColor, equator = RenderSettings.ambientEquatorColor, ground = RenderSettings.ambientGroundColor;
var ambientMode = RenderSettings.ambientMode;
float ambientIntensity = RenderSettings.ambientIntensity;
var sourceSky = RenderSettings.skybox;
var sourceReflection = RenderSettings.defaultReflectionMode;
float reflectionIntensity = RenderSettings.reflectionIntensity;
var copy = UnityEngine.Object.Instantiate(sourceData);
copy.name = "LookWater_Terrain";
UnityEditor.AssetDatabase.CreateAsset(copy, dataFolder + "/Terrain.asset");
if (copy == sourceData || copy.alphamapTextures.Any(t => sourceTextureIds.Contains(t.GetInstanceID())))
    throw new Exception("Terrain 내부 alpha 텍스처 공유를 발견했습니다.");
// 사본 도색 후 원본 내용과 파일이 그대로인지 검사하고 사본의 시험 셀도 복구한다.
var painted = (float[,,])sourceAlpha.Clone();
for (int i = 0; i < painted.GetLength(2); i++) painted[0,0,i] = i == 0 ? 1f : 0f;
copy.SetAlphamaps(0, 0, painted);
var unchanged = sourceData.GetAlphamaps(0, 0, 1, 1);
for (int i = 0; i < unchanged.GetLength(2); i++) if (unchanged[0,0,i] != sourceAlpha[0,0,i]) throw new Exception("원본 alpha 내용 변경");
copy.SetAlphamaps(0, 0, sourceAlpha);
if (hash(sourcePath) != sourceHash || sourceData.GetHeights(0,0,1,1)[0,0] != sourceHeight[0,0] || sourceData.GetDetailLayer(0,0,1,1,0)[0,0] != sourceDetail[0,0])
    throw new Exception("사본 독립성 검사 중 원본이 달라졌습니다.");
var layerMap = new List<object>();
var layers = copy.terrainLayers;
for (int i = 0; i < layers.Length; i++)
{
    string original = UnityEditor.AssetDatabase.GetAssetPath(layers[i]);
    string destination = dataFolder + "/Layer_" + i + ".terrainlayer";
    var layer = UnityEngine.Object.Instantiate(layers[i]);
    UnityEditor.AssetDatabase.CreateAsset(layer, destination);
    layers[i] = layer;
    layerMap.Add(new { sourcePath = original, sourceGuid = UnityEditor.AssetDatabase.AssetPathToGUID(original), path = destination, guid = UnityEditor.AssetDatabase.AssetPathToGUID(destination), index = i });
}
var allAlpha = sourceData.GetAlphamaps(0,0,sourceData.alphamapWidth,sourceData.alphamapHeight);
copy.terrainLayers = layers;
copy.SetAlphamaps(0,0,allAlpha);
allAlpha = null;
if (copy.detailScatterMode != sourceData.detailScatterMode || copy.detailPrototypes.Length != 6)
    throw new Exception("산포 모드 또는 Detail 6종 기준선이 다릅니다.");
UnityEditor.EditorUtility.SetDirty(copy);
UnityEditor.AssetDatabase.SaveAssetIfDirty(copy);
var review = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);
UnityEngine.SceneManagement.SceneManager.SetActiveScene(review);
var environment = new GameObject("ThemeEnvironment");
var preview = new GameObject("PreviewRig");
var reference = new GameObject("ReferenceOnly");
var terrainObject = Terrain.CreateTerrainGameObject(copy);
terrainObject.name = "LookWater_Terrain";
terrainObject.transform.SetParent(environment.transform, true);
terrainObject.transform.position = sourceTerrain.transform.position;
var terrain = terrainObject.GetComponent<Terrain>();
UnityEditor.EditorUtility.CopySerialized(sourceTerrain, terrain);
terrain.terrainData = copy;
terrainObject.GetComponent<TerrainCollider>().terrainData = copy;
if (sourceTerrain.materialTemplate != null)
{
    var terrainMaterial = UnityEngine.Object.Instantiate(sourceTerrain.materialTemplate);
    UnityEditor.AssetDatabase.CreateAsset(terrainMaterial, dataFolder + "/Terrain.mat");
    terrain.materialTemplate = terrainMaterial;
}
RenderSettings.fog = fog; RenderSettings.fogMode = fogMode;
RenderSettings.fogStartDistance = fogStart; RenderSettings.fogEndDistance = fogEnd; RenderSettings.fogDensity = fogDensity; RenderSettings.fogColor = fogColor;
RenderSettings.ambientMode = ambientMode; RenderSettings.ambientSkyColor = sky; RenderSettings.ambientEquatorColor = equator; RenderSettings.ambientGroundColor = ground; RenderSettings.ambientIntensity = ambientIntensity;
RenderSettings.defaultReflectionMode = sourceReflection; RenderSettings.reflectionIntensity = reflectionIntensity;
if (sourceSky != null)
{
    var skyMaterial = UnityEngine.Object.Instantiate(sourceSky);
    UnityEditor.AssetDatabase.CreateAsset(skyMaterial, dataFolder + "/PreviewSky.mat");
    RenderSettings.skybox = skyMaterial;
}
var sunObject = new GameObject("PreviewSun"); sunObject.transform.SetParent(preview.transform, false);
sunObject.transform.rotation = sourceSun.transform.rotation;
var sun = sunObject.AddComponent<Light>(); UnityEditor.EditorUtility.CopySerialized(sourceSun, sun); RenderSettings.sun = sun;
var cameraObject = new GameObject("PreviewCamera"); cameraObject.transform.SetParent(preview.transform, false);
var camera = cameraObject.AddComponent<Camera>(); camera.CopyFrom(sourceCamera); cameraObject.tag = "MainCamera";
var extraSource = sourceCamera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
var extra = cameraObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
if (extraSource != null) UnityEditor.EditorUtility.CopySerialized(extraSource, extra);
extra.renderPostProcessing = true; extra.cameraStack.Clear(); camera.targetTexture = null;
string volumeSource = "Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/LakeAtmosphere.asset";
if (!UnityEditor.AssetDatabase.CopyAsset(volumeSource, dataFolder + "/PreviewVolume.asset")) throw new Exception("Volume 복사 실패");
var volumeObject = new GameObject("PreviewVolume"); volumeObject.transform.SetParent(preview.transform, false);
var volume = volumeObject.AddComponent<UnityEngine.Rendering.Volume>(); volume.isGlobal = true;
volume.sharedProfile = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(dataFolder + "/PreviewVolume.asset");
// 현재 Host의 런타임 전용 RP 적용만 PreviewRig로 이전한다. 진행·조명 컨트롤러는 복제하지 않는다.
var renderingType = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UPlayGround.Gameplay.World.SceneEnvironmentRendering")).FirstOrDefault(t => t != null);
if (renderingType != null)
{
    var sourceBinding = sourceRoots.SelectMany(g => g.GetComponentsInChildren(renderingType, true)).FirstOrDefault();
    if (sourceBinding != null)
    {
        var binding = preview.AddComponent(renderingType);
        UnityEditor.EditorUtility.CopySerialized(sourceBinding, binding);
    }
}
var fixtureRoot = new GameObject("TrialFixtures_ExcludeFromIntegration"); fixtureRoot.transform.SetParent(preview.transform, false);
var fixtureOrigin = (float[])read(plan["fixtureOrigin"].ToString(), typeof(float[]));
float originX = fixtureOrigin[0], originZ = fixtureOrigin[1];
float top = float.MinValue;
for (int x = -7; x <= 50; x += 3) for (int z = -2; z <= 30; z += 3)
    top = Mathf.Max(top, sourceTerrain.SampleHeight(new Vector3(originX + x,0f,originZ + z)) + sourceTerrain.transform.position.y);
top += Convert.ToSingle(plan["fixtureHeightAboveTerrain"]) + 5f;
float laneSpacing = Convert.ToSingle(plan["fixtureLaneSpacing"]);
var waterTrials = (Dictionary<string,object>[])read(plan["waterTrials"].ToString(), typeof(Dictionary<string,object>[]));
var waterSources = waterTrials.Select(t => t["source"].ToString()).ToArray();
var waterKeys = waterTrials.Select(t => t["key"].ToString()).ToArray();
var materialRows = new List<object>();
float[] depths = { 0.2f, 0.6f, 1.5f, 3f, 5f };
var floorMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
floorMaterial.name = "Preview_RiverBed";
string riverLayerPath = "Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Scenes/Terrain/TerrainLayers/TerrainLayer_RiverBed.terrainlayer";
var riverLayer = UnityEditor.AssetDatabase.LoadAssetAtPath<TerrainLayer>(riverLayerPath);
if (riverLayer == null) throw new Exception("RiverBed 후보 누락");
floorMaterial.SetTexture("_BaseMap", riverLayer.diffuseTexture); floorMaterial.SetColor("_BaseColor", Color.white);
UnityEditor.AssetDatabase.CreateAsset(floorMaterial, dataFolder + "/PreviewRiverBed.mat");
for (int lane = 0; lane < waterSources.Length; lane++)
{
    var materialSource = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(waterSources[lane]);
    if (materialSource == null) throw new Exception("물 후보 누락: " + waterSources[lane]);
    var material = UnityEngine.Object.Instantiate(materialSource); material.name = "LookWater_" + waterKeys[lane];
    string materialPath = dataFolder + "/" + waterKeys[lane] + ".mat"; UnityEditor.AssetDatabase.CreateAsset(material, materialPath);
    var water = GameObject.CreatePrimitive(PrimitiveType.Plane); water.name = waterKeys[lane] + "_DepthTrial";
    water.transform.SetParent(fixtureRoot.transform, false); water.transform.position = new Vector3(originX + lane * laneSpacing, top, originZ + 13f); water.transform.localScale = new Vector3(1.4f,1f,3f);
    water.GetComponent<Renderer>().sharedMaterial = material; UnityEngine.Object.DestroyImmediate(water.GetComponent<Collider>());
    for (int d = 0; d < depths.Length; d++)
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name = "Depth_" + depths[d].ToString("F1") + "m"; floor.transform.SetParent(fixtureRoot.transform, false);
        floor.transform.position = new Vector3(originX + lane * laneSpacing, top - depths[d] - 0.1f, originZ + 1f + d * 6f);
        floor.transform.localScale = new Vector3(14f,0.2f,6f); floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;
    }
    materialRows.Add(new { key = waterKeys[lane], sourcePath = waterSources[lane], sourceHash = hash(waterSources[lane]), path = materialPath, guid = UnityEditor.AssetDatabase.AssetPathToGUID(materialPath) });
}
var assetRows = new List<object>();
for (int i = 0; i < foliage.Length; i++)
{
    var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(foliage[i]); if (asset == null) throw new Exception("식생/암반 후보 누락: " + foliage[i]);
    if (asset.GetComponentsInChildren<MonoBehaviour>(true).Length > 0) throw new Exception("후보 스크립트 수동 검토 필요: " + foliage[i]);
    var instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(asset, review); instance.transform.SetParent(fixtureRoot.transform, true);
    var p = new Vector3(originX + i * 10f, 0f, originZ - 15f); p.y = terrain.SampleHeight(p) + terrain.transform.position.y; instance.transform.position = p;
    var renderers = instance.GetComponentsInChildren<Renderer>(true);
    assetRows.Add(new { path = foliage[i], guid = UnityEditor.AssetDatabase.AssetPathToGUID(foliage[i]), position = new float[] { p.x,p.y,p.z }, renderers = renderers.Length, lodGroups = instance.GetComponentsInChildren<LODGroup>(true).Length, colliders = instance.GetComponentsInChildren<Collider>(true).Length, nullMaterials = renderers.Sum(r => r.sharedMaterials.Count(m => m == null)), renderStatus = "미검증" });
}
camera.transform.position = new Vector3(originX + laneSpacing, top + 3f, originZ - 20f);
camera.transform.rotation = Quaternion.LookRotation(new Vector3(originX + laneSpacing, top - 1f, originZ + 10f) - camera.transform.position);
camera.fieldOfView = sourceCamera.fieldOfView;
if (hash(sourcePath) != sourceHash || hash(sourceScene.path) != sourceSceneHash || sourceScene.isDirty != sourceDirty) throw new Exception("Host 저장 상태가 달라졌습니다.");
if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(review, scenePath)) throw new Exception("비교 Scene 저장 실패");
var report = new { version = "A01-v1", status = "Created_NotVisuallyAccepted", sourceScene = sourceScene.path, sourceTerrain = sourcePath, sourceTerrainHash = sourceHash, sourceSceneHash = sourceSceneHash, sourceDirtyBefore = sourceDirty, sourceDirtyAfter = sourceScene.isDirty, scene = scenePath, terrain = dataFolder + "/Terrain.asset", internalAlphaTextureSharing = false, sourceTestCellUnchanged = true, rootTransform = "identity", coordinateSpace = "world", layerMap = layerMap, scatterMode = copy.detailScatterMode.ToString(), detailCount = copy.detailPrototypes.Length, assets = assetRows, waterMaterials = materialRows, generatedAssets = new object[0], camera = new { position = new float[] { camera.transform.position.x,camera.transform.position.y,camera.transform.position.z }, rotation = new float[] { camera.transform.eulerAngles.x,camera.transform.eulerAngles.y,camera.transform.eulerAngles.z }, fov = camera.fieldOfView, fog = fog, fogStart = fogStart, fogEnd = fogEnd }, knownIssues = new string[] { "수심 비교 수조는 PreviewRig 시험용이며 통합하지 않음", "실제 TPS/Play/수면 동작/성능 미검증", "원본 Scene additive 보존 중. 촬영 전에 A00이 안전한 독립 표시를 선택해야 함" } };
var serialize = jsonType.GetMethod("SerializeObject", new Type[] { typeof(object) });
System.IO.File.WriteAllText(output + "/build-report.json", (string)serialize.Invoke(null, new object[] { report }));
return "A01 LookWater created; source preserved; visual acceptance pending: " + scenePath;
