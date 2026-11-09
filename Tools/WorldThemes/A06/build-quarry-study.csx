// A00 lease 아래 열린 Host를 보존한 채 독립 비교실만 만든다.
var sourceScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || sourceScene.path != "Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")
    throw new Exception("A00 기준선 확보 후 Host Edit Mode에서 실행해야 합니다.");
string scenePath = "Assets/01.Scenes/ArtReview/WorldThemes/Theme_QuarryPass.unity";
string dataFolder = "Assets/10.Datas/World/ThemeStudies/QuarryPass";
string output = "output/WorldThemeProduction/QuarryPass";
if (System.IO.File.Exists(scenePath) || System.IO.File.Exists(dataFolder + "/Terrain.asset"))
    throw new Exception("기존 A06 산출물이 있어 덮어쓰지 않았습니다.");
var jsonType = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Newtonsoft.Json.JsonConvert")).First(t => t != null);
var deserialize = jsonType.GetMethod("DeserializeObject", new Type[] { typeof(string), typeof(Type) });
Func<string, Type, object> read = (text, type) => deserialize.Invoke(null, new object[] { text, type });
var plan = (Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/WorldThemes/A06/quarry-study.json"), typeof(Dictionary<string,object>));
if (!Convert.ToBoolean(plan["maskAuthorized"])) throw new Exception("A00 core mask 승인 전 편집할 수 없습니다.");
if (!Convert.ToBoolean(plan["corridorConfirmed"])) throw new Exception("실제 진입·작업장·갱구·복귀로 확인 전 편집하지 않습니다.");
if (Convert.ToBoolean(plan["surfaceEditingEnabled"])) throw new Exception("첫 절벽 비교 단계는 기존 절개 지형과 표면을 보존합니다.");
var polygon = (float[][])read(plan["corePolygon"].ToString(), typeof(float[][]));
if (polygon.Length < 3) throw new Exception("A00 core polygon이 없습니다.");
Func<Vector3,bool> isInsideCore = p => {
    bool inside = false;
    for (int i=0,j=polygon.Length-1;i<polygon.Length;j=i++)
        if ((polygon[i][1]>p.z)!=(polygon[j][1]>p.z) && p.x<(polygon[j][0]-polygon[i][0])*(p.z-polygon[i][1])/(polygon[j][1]-polygon[i][1])+polygon[i][0]) inside=!inside;
    return inside;
};
var preflightPlacements=(Dictionary<string,object>[])read(plan["placements"].ToString(),typeof(Dictionary<string,object>[]));
foreach (string path in preflightPlacements.Select(p=>p["prefab"].ToString()).Distinct())
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
if (sourceHash != plan["baselineTerrainHash"].ToString() || sourceSceneHash != plan["baselineSceneHash"].ToString())
    throw new Exception("A00 확정 기준선과 현재 파일 hash가 다릅니다.");
bool sourceDirty = sourceScene.isDirty;
Func<Array,string> contentHash = array => {
    byte[] bytes = new byte[Buffer.ByteLength(array)]; Buffer.BlockCopy(array,0,bytes,0,bytes.Length);
    using(var sha=System.Security.Cryptography.SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","");
};
string sourceHeightHash=contentHash(sourceData.GetHeights(0,0,sourceData.heightmapResolution,sourceData.heightmapResolution));
string sourceAlphaHash=contentHash(sourceData.GetAlphamaps(0,0,sourceData.alphamapWidth,sourceData.alphamapHeight));
var sourceDetailHashes=new List<string>();
for(int i=0;i<sourceData.detailPrototypes.Length;i++) sourceDetailHashes.Add(contentHash(sourceData.GetDetailLayer(0,0,sourceData.detailWidth,sourceData.detailHeight,i)));
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
copy.name = "QuarryPass_Terrain";
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
var alphaBeforeLayerRemap=sourceData.GetAlphamaps(0,0,sourceData.alphamapWidth,sourceData.alphamapHeight);
for (int i = 0; i < layers.Length; i++)
{
    string original = UnityEditor.AssetDatabase.GetAssetPath(layers[i]);
    string destination = dataFolder + "/Layer_" + i + ".terrainlayer";
    var layer = UnityEngine.Object.Instantiate(layers[i]);
    UnityEditor.AssetDatabase.CreateAsset(layer, destination);
    layers[i] = layer;
    layerMap.Add(new { sourcePath = original, sourceGuid = UnityEditor.AssetDatabase.AssetPathToGUID(original), path = destination, guid = UnityEditor.AssetDatabase.AssetPathToGUID(destination), index = i });
}
string quarryRockSource=plan["quarryRockCandidate"].ToString();
var quarryRock=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<TerrainLayer>(quarryRockSource));
string quarryRockPath=dataFolder+"/Layer_QuarryRock.terrainlayer";UnityEditor.AssetDatabase.CreateAsset(quarryRock,quarryRockPath);
int quarryRockIndex=layers.Length;
copy.terrainLayers=layers.Concat(new[]{quarryRock}).ToArray();
layerMap.Add(new{sourcePath=quarryRockSource,sourceGuid=UnityEditor.AssetDatabase.AssetPathToGUID(quarryRockSource),path=quarryRockPath,guid=UnityEditor.AssetDatabase.AssetPathToGUID(quarryRockPath),index=quarryRockIndex});
// Layer 참조 재할당이 alpha를 초기화하므로 기존 채널과 새 채널 0을 명시적으로 복원한다.
var remappedAlpha=new float[copy.alphamapHeight,copy.alphamapWidth,layers.Length+1];
for(int z=0;z<copy.alphamapHeight;z++)for(int x=0;x<copy.alphamapWidth;x++)for(int l=0;l<layers.Length;l++)remappedAlpha[z,x,l]=alphaBeforeLayerRemap[z,x,l];
copy.SetAlphamaps(0,0,remappedAlpha);
var restoredAlpha=copy.GetAlphamaps(0,0,copy.alphamapWidth,copy.alphamapHeight);
for(int z=0;z<copy.alphamapHeight;z++)for(int x=0;x<copy.alphamapWidth;x++)for(int l=0;l<restoredAlpha.GetLength(2);l++)
    if(restoredAlpha[z,x,l]!=(l<layers.Length?alphaBeforeLayerRemap[z,x,l]:0f))throw new Exception("Layer remap 후 alpha 전수 복원 실패");
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
terrainObject.name = "QuarryPass_Terrain";
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
extra.volumeTrigger=camera.transform;
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
var boundsInput=(float[])read(plan["referenceBounds"].ToString(),typeof(float[]));
var referenceBounds=new Bounds(new Vector3((boundsInput[0]+boundsInput[1])*.5f,250f,(boundsInput[2]+boundsInput[3])*.5f),new Vector3(boundsInput[1]-boundsInput[0],1200f,boundsInput[3]-boundsInput[2]));
var sourceEnvironment=sourceRoots.Single(g=>g.name=="AuthoredEnvironment");
var sourceAll=sourceRoots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var protectedEntities=sourceAll.SelectMany(t=>t.GetComponents<UPlayGround.Components.SceneEntityId>()).ToArray();
var protectedAnchors=(float[][])read(plan["protectedAnchors"].ToString(),typeof(float[][]));
var corridor=(float[][])read(plan["preserveCorridor"].ToString(),typeof(float[][]));
float protectRadius=Convert.ToSingle(plan["protectedRadius"]),corridorHalfWidth=Convert.ToSingle(plan["corridorHalfWidth"]);
Func<Vector3,bool> isProtected=p=>protectedEntities.Any(e=>new Vector2(e.transform.position.x-p.x,e.transform.position.z-p.z).sqrMagnitude<protectRadius*protectRadius)||protectedAnchors.Any(a=>new Vector2(a[0]-p.x,a[2]-p.z).sqrMagnitude<protectRadius*protectRadius);
Func<Vector3,bool> isCorridor=p=>Enumerable.Range(1,corridor.Length-1).Any(i=>{var a=new Vector2(corridor[i-1][0],corridor[i-1][1]);var b=new Vector2(corridor[i][0],corridor[i][1]);var ab=b-a;float t=ab.sqrMagnitude>.00001f?Mathf.Clamp01(Vector2.Dot(new Vector2(p.x,p.z)-a,ab)/ab.sqrMagnitude):0f;return Vector2.Distance(new Vector2(p.x,p.z),a+ab*t)<corridorHalfWidth;});
var markers=new GameObject("ProtectedContentMarkers");markers.transform.SetParent(reference.transform,false);
foreach(var e in protectedEntities.Where(e=>referenceBounds.Contains(e.transform.position))){var marker=new GameObject(e.Guid+"_"+e.name);marker.transform.SetParent(markers.transform,false);marker.transform.position=e.transform.position;}
var context=new GameObject("ExistingEnvironmentContext_ExcludeFromIntegration");context.transform.SetParent(reference.transform,false);
var rendererMap=new Dictionary<int,Renderer>();
foreach(var original in sourceEnvironment.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.bounds.Intersects(referenceBounds))){
    var filter=original.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null)continue;
    var item=new GameObject(original.name);item.transform.SetParent(context.transform,false);item.transform.SetPositionAndRotation(original.transform.position,original.transform.rotation);item.transform.localScale=original.transform.lossyScale;
    item.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;var renderer=item.AddComponent<MeshRenderer>();UnityEditor.EditorUtility.CopySerialized(original,renderer);renderer.probeAnchor=null;item.SetActive(original.gameObject.activeInHierarchy);rendererMap.Add(original.GetInstanceID(),renderer);
}
var registered=new HashSet<int>();
foreach(var original in sourceEnvironment.GetComponentsInChildren<LODGroup>(true)){
    var lods=original.GetLODs();var ids=lods.SelectMany(l=>l.renderers).Where(r=>r!=null&&rendererMap.ContainsKey(r.GetInstanceID())).Select(r=>r.GetInstanceID()).Distinct().ToArray();if(ids.Length==0||ids.Any(i=>registered.Contains(i)))continue;
    var item=new GameObject(original.name+"_LOD");item.transform.SetParent(context.transform,false);item.transform.SetPositionAndRotation(original.transform.position,original.transform.rotation);item.transform.localScale=original.transform.lossyScale;var lod=item.AddComponent<LODGroup>();UnityEditor.EditorUtility.CopySerialized(original,lod);
    for(int i=0;i<lods.Length;i++)lods[i].renderers=lods[i].renderers.Where(r=>r!=null&&rendererMap.ContainsKey(r.GetInstanceID())).Select(r=>rendererMap[r.GetInstanceID()]).ToArray();lod.SetLODs(lods);foreach(int i in ids)registered.Add(i);
}
var additions=new GameObject("Theme_QuarryPass_Environment");additions.transform.SetParent(environment.transform,false);
var assets=new List<object>();
foreach(var placement in preflightPlacements){
    string path=placement["prefab"].ToString();var pos=(float[])read(placement["position"].ToString(),typeof(float[]));var p=new Vector3(pos[0],pos[1],pos[2]);
    if(!isInsideCore(p)||isProtected(p)||isCorridor(p))throw new Exception("암반 배치가 core/콘텐츠/실제 길 보존 계약을 벗어났습니다.");
    var instance=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path),review);instance.name=placement["name"].ToString();instance.transform.SetParent(additions.transform,false);instance.transform.position=p;
    var euler=(float[])read(placement["euler"].ToString(),typeof(float[]));var scale=(float[])read(placement["scale"].ToString(),typeof(float[]));instance.transform.rotation=Quaternion.Euler(euler[0],euler[1],euler[2]);instance.transform.localScale=new Vector3(scale[0],scale[1],scale[2]);
    assets.Add(new{path=path,guid=UnityEditor.AssetDatabase.AssetPathToGUID(path),instance=instance.name,position=pos,euler=euler,scale=scale,renderStatus="미검증",vertexTerrainIntersectionStatus="미수행 — 실제 정점/단면 확인 후 채택"});
}
Physics.SyncTransforms();int footprintErrors=0;var footprints=new List<object>();
foreach(Transform item in additions){
    var boxes=item.GetComponentsInChildren<Renderer>(true).Select(r=>new{kind="Renderer",name=r.name,b=r.bounds}).Concat(item.GetComponentsInChildren<Collider>(true).Select(c=>new{kind="Collider",name=c.name,b=c.bounds}));
    foreach(var row in boxes){
        var b=row.b;bool contained=isInsideCore(new Vector3(b.min.x,0f,b.min.z))&&isInsideCore(new Vector3(b.min.x,0f,b.max.z))&&isInsideCore(new Vector3(b.max.x,0f,b.min.z))&&isInsideCore(new Vector3(b.max.x,0f,b.max.z));
        bool protectedOverlap=protectedEntities.Any(e=>b.SqrDistance(new Vector3(e.transform.position.x,b.center.y,e.transform.position.z))<protectRadius*protectRadius)||protectedAnchors.Any(a=>b.SqrDistance(new Vector3(a[0],b.center.y,a[2]))<protectRadius*protectRadius);
        var inflated=b;inflated.Expand(new Vector3(corridorHalfWidth*2f,10000f,corridorHalfWidth*2f));bool corridorOverlap=false;
        for(int i=1;i<corridor.Length;i++){var a=new Vector3(corridor[i-1][0],b.center.y,corridor[i-1][1]);var end=new Vector3(corridor[i][0],b.center.y,corridor[i][1]);float rayDistance;if(inflated.Contains(a)||inflated.Contains(end)||(inflated.IntersectRay(new Ray(a,(end-a).normalized),out rayDistance)&&rayDistance<=(end-a).magnitude)){corridorOverlap=true;break;}}
        if(!contained||protectedOverlap||corridorOverlap)footprintErrors++;
        footprints.Add(new{instance=item.name,component=row.name,kind=row.kind,min=new float[]{b.min.x,b.min.y,b.min.z},max=new float[]{b.max.x,b.max.y,b.max.z},coreContainsBounds=contained,protectedOverlap=protectedOverlap,corridorOverlap=corridorOverlap});
    }
}
if(footprintErrors>0)throw new Exception("암반 실제 footprint가 core 밖입니다.");
var actualAlpha=copy.GetAlphamaps(0,0,copy.alphamapWidth,copy.alphamapHeight);int alphaChanges=0;
for(int z=0;z<copy.alphamapHeight;z++)for(int x=0;x<copy.alphamapWidth;x++)for(int l=0;l<actualAlpha.GetLength(2);l++)if(actualAlpha[z,x,l]!=(l<alphaBeforeLayerRemap.GetLength(2)?alphaBeforeLayerRemap[z,x,l]:0f))alphaChanges++;
if(alphaChanges!=0||contentHash(copy.GetHeights(0,0,copy.heightmapResolution,copy.heightmapResolution))!=sourceHeightHash)throw new Exception("기존 절개 지형/표면 보존 실패");
for(int i=0;i<copy.detailPrototypes.Length;i++)if(contentHash(copy.GetDetailLayer(0,0,copy.detailWidth,copy.detailHeight,i))!=sourceDetailHashes[i])throw new Exception("기존 Detail 보존 실패");
if(hash(sourcePath)!=sourceHash||hash(sourceScene.path)!=sourceSceneHash||sourceScene.isDirty!=sourceDirty||contentHash(sourceData.GetAlphamaps(0,0,sourceData.alphamapWidth,sourceData.alphamapHeight))!=sourceAlphaHash)throw new Exception("Host 원본 보존 실패");
var poses=(Dictionary<string,object>[])read(plan["capturePoses"].ToString(),typeof(Dictionary<string,object>[]));
if(poses.Length>0){var p=(float[])read(poses[0]["position"].ToString(),typeof(float[]));var target=(float[])read(poses[0]["target"].ToString(),typeof(float[]));camera.transform.position=new Vector3(p[0],p[1],p[2]);camera.transform.rotation=Quaternion.LookRotation(new Vector3(target[0],target[1],target[2])-camera.transform.position);}
if(RenderSettings.skybox!=null&&RenderSettings.skybox.HasProperty("_AtmosphereThickness")){RenderSettings.skybox.SetFloat("_AtmosphereThickness",1f);UnityEditor.EditorUtility.SetDirty(RenderSettings.skybox);UnityEditor.AssetDatabase.SaveAssetIfDirty(RenderSettings.skybox);}
UnityEditor.EditorUtility.SetDirty(copy);UnityEditor.AssetDatabase.SaveAssetIfDirty(copy);
string prefabPath="Assets/03.Prefabs/Environment/WorldThemes/QuarryPass/Theme_QuarryPass_Environment.prefab";System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(prefabPath));if(System.IO.File.Exists(prefabPath))throw new Exception("기존 환경 prefab 덮어쓰기 금지");UnityEditor.PrefabUtility.SaveAsPrefabAsset(additions,prefabPath);
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(review,scenePath))throw new Exception("QuarryPass 씬 저장 실패");
var report=new{themeId="QuarryPass",version=plan["version"].ToString(),status="독립 절벽 대표 시험 — 실제 화면/접합/사용자 인수 전",sourceScene=sourceScene.path,sourceTerrainGuid=UnityEditor.AssetDatabase.AssetPathToGUID(sourcePath),sourceTerrainHash=sourceHash,baselineSceneHash=sourceSceneHash,scene=scenePath,terrain=dataFolder+"/Terrain.asset",environmentPrefab=prefabPath,coreMask=polygon,referenceBounds=boundsInput,referenceBoundsOrder=new[]{"minX","maxX","minZ","maxZ"},rootTransform="identity",coordinateSpace="world",layerMap=layerMap,scatterMode=copy.detailScatterMode.ToString(),protectedEntities=protectedEntities.Where(e=>referenceBounds.Contains(e.transform.position)).Select(e=>new{id=e.Guid,position=e.transform.position.ToString("R")}).ToArray(),protectedNonIdAnchors=protectedAnchors,preservedCorridor=corridor,unchangedOutsideCoreCheck=new{alphaChanged=alphaChanges,heightEqual=true,detailsEqual=true,sourceFilesEqual=true},footprints=footprints,assets=assets,replaceObjects=new object[0],deleteObjects=new object[0],generatedAssets=new object[0],capturePoses=poses,knownIssues=new[]{"기존 절개단/침식홈/능선/암반8/파편3/터널UV 보존", "실제cliff정점/Terrain교차·접합·시야 검사 전이라 채택/통합 금지", "첫 대표 시험이며 전체테마 아님. 실제TPS/Play/LOD전이/성능 미검증"},nextOwner="A00/A12"};
var serialize=jsonType.GetMethod("SerializeObject",new Type[]{typeof(object)});System.IO.File.WriteAllText(output+"/handoff.json",(string)serialize.Invoke(null,new object[]{report}));
return "QuarryPass 독립 시험 준비, 기존Terrain전체보존·원본보존. 실제렌더/접합 검토 대기.";
