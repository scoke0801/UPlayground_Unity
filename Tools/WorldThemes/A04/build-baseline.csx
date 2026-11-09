// A00 lease 아래 열린 Host를 보존한 채 독립 비교실만 만든다.
var sourceScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || sourceScene.path != "Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")
    throw new Exception("A00 기준선 확보 후 Host Edit Mode에서 실행해야 합니다.");
string scenePath = "Assets/01.Scenes/ArtReview/WorldThemes/Theme_RedTreeForest_Baseline.unity";
string dataFolder = "Assets/10.Datas/World/ThemeStudies/RedTreeForest_Baseline";
string output = "output/WorldThemeProduction/RedTreeForest";
if (System.IO.File.Exists(scenePath) || System.IO.File.Exists(dataFolder + "/Terrain.asset"))
    throw new Exception("기존 A04 산출물이 있어 덮어쓰지 않았습니다.");
var jsonType = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Newtonsoft.Json.JsonConvert")).First(t => t != null);
var deserialize = jsonType.GetMethod("DeserializeObject", new Type[] { typeof(string), typeof(Type) });
Func<string, Type, object> read = (text, type) => deserialize.Invoke(null, new object[] { text, type });
var plan = (Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/WorldThemes/A04/redforest-study.json"), typeof(Dictionary<string,object>));
var lease=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/WorldThemes/Integration/editor-lease.json"),typeof(Dictionary<string,object>));
if(lease["owner"].ToString()!=plan["editorLeaseOwnerRequired"].ToString())throw new Exception("A00가 명시한 A04 Editor lease 필요");
var baseline=(Dictionary<string,object>)read(System.IO.File.ReadAllText(plan["baselineContract"].ToString()),typeof(Dictionary<string,object>));
if(baseline["sourceSceneHash"].ToString()!=plan["baselineSceneHash"].ToString()||baseline["sourceTerrainHash"].ToString()!=plan["baselineTerrainHash"].ToString())throw new Exception("A00 baseline-v2 계약과 A04 입력 불일치");
if (Convert.ToBoolean(plan["maskAuthorized"])) throw new Exception("baseline 전용 단계는 core 잠금 상태에서만 실행합니다.");

var polygon = (float[][])read(plan["corePolygon"].ToString(), typeof(float[][]));
if (polygon.Length < 3) throw new Exception("A00 core polygon이 없습니다.");
Func<Vector3,bool> isInsideCore = p => {
    bool inside = false;
    for (int i=0,j=polygon.Length-1;i<polygon.Length;j=i++)
        if ((polygon[i][1]>p.z)!=(polygon[j][1]>p.z) && p.x<(polygon[j][0]-polygon[i][0])*(p.z-polygon[i][1])/(polygon[j][1]-polygon[i][1])+polygon[i][0]) inside=!inside;
    return inside;
};
var preflightPlacements=new Dictionary<string,object>[0];
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
var verifiedEnvironment=sourceRoots.SingleOrDefault(g=>g.name==plan["sourceEnvironmentPath"].ToString());
if(verifiedEnvironment==null)throw new Exception("정확한 환경 루트 경로 없음. 이름 폴백 없이 조사로 중단");
var verifiedRedRoot=verifiedEnvironment.transform.Find(plan["sourceRedForestRelativePath"].ToString());
if(verifiedRedRoot==null)throw new Exception("정확한 붉은숲 경로 없음. 이름 폴백 없이 조사로 중단");
if(verifiedEnvironment.transform.position!=Vector3.zero||verifiedEnvironment.transform.rotation!=Quaternion.identity||verifiedEnvironment.transform.lossyScale!=Vector3.one)throw new Exception("환경 루트 identity 계약 불일치");
var sourceTerrain = sourceRoots.SelectMany(g => g.GetComponentsInChildren<Terrain>(true)).Single(t => t.terrainData != null);
var sourceData = sourceTerrain.terrainData;
if(EditorUtility.IsDirty(sourceData))throw new Exception("원본 TerrainData에 미저장 변경이 있어 저장/폐기 없이 중단");
var expectedLayers=(Dictionary<string,object>[])read(System.IO.File.ReadAllText("Tools/WorldThemes/A04/source-layer-contract.json"),typeof(Dictionary<string,object>[]));
if(sourceData.terrainLayers.Length!=7||expectedLayers.Length!=7)throw new Exception("source 7 layer 계약 불일치");
for(int i=0;i<7;i++){string p=UnityEditor.AssetDatabase.GetAssetPath(sourceData.terrainLayers[i]);if(p!=expectedLayers[i]["path"].ToString()||UnityEditor.AssetDatabase.AssetPathToGUID(p)!=expectedLayers[i]["guid"].ToString())throw new Exception("source layer 정확 경로/GUID 순서 불일치");}
string sourcePath = UnityEditor.AssetDatabase.GetAssetPath(sourceData);
Func<string,string> hash = p => { using (var sha = System.Security.Cryptography.SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(System.IO.File.ReadAllBytes(p))).Replace("-", ""); };
string sourceHash = hash(sourcePath);
string sourceSceneHash = hash(sourceScene.path);
if(hash(baseline["navMesh"].ToString())!=plan["baselineNavMeshHash"].ToString())throw new Exception("원본 NavMesh 기준선 변동. 베이크/복구 없이 조사 중단");
if (sourceHash != plan["baselineTerrainHash"].ToString() || sourceSceneHash != plan["baselineSceneHash"].ToString())
    throw new Exception("A00 확정 기준선과 현재 파일 hash가 다릅니다.");
bool sourceDirty = sourceScene.isDirty;
Physics.SyncTransforms();
var baselineEntities=sourceRoots.SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).ToArray();if(baselineEntities.Length!=317)throw new Exception("원본317ID 기준선 불일치");
Func<string> entityGroundState=()=>{var rows=new List<object>();foreach(var entity in baselineEntities){var position=entity.transform.position;var hits=Physics.RaycastAll(position+Vector3.up*2f,Vector3.down,10f,-1,QueryTriggerInteraction.Ignore).Where(h=>h.collider.gameObject.scene==sourceScene&&!h.collider.transform.IsChildOf(entity.transform)).OrderBy(h=>h.distance).ToArray();rows.Add(new{id=entity.Guid,globalId=GlobalObjectId.GetGlobalObjectIdSlow(entity).ToString(),position=new[]{position.x,position.y,position.z},hasGround=hits.Length>0,groundY=hits.Length>0?hits[0].point.y:float.MinValue});}return (string)jsonType.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{rows});};
string initialEntityGroundState=entityGroundState();
Func<Array,string> contentHash = array => {
    byte[] bytes = new byte[Buffer.ByteLength(array)]; Buffer.BlockCopy(array,0,bytes,0,bytes.Length);
    using(var sha=System.Security.Cryptography.SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","");
};
string sourceHeightHash=contentHash(sourceData.GetHeights(0,0,sourceData.heightmapResolution,sourceData.heightmapResolution));
string sourceHolesHash=contentHash(sourceData.GetHoles(0,0,sourceData.holesResolution,sourceData.holesResolution));
var serializeState=jsonType.GetMethod("SerializeObject",new Type[]{typeof(object)});
Func<object,string> stateJson=o=>(string)serializeState.Invoke(null,new object[]{o});
Func<TerrainData,string> treeState=d=>stateJson(d.treeInstances.Select(t=>new{prototypeIndex=t.prototypeIndex,position=new[]{t.position.x,t.position.y,t.position.z},widthScale=t.widthScale,heightScale=t.heightScale,rotation=t.rotation,color=new[]{t.color.r,t.color.g,t.color.b,t.color.a},lightmapColor=new[]{t.lightmapColor.r,t.lightmapColor.g,t.lightmapColor.b,t.lightmapColor.a}}).ToArray());
string sourceTreesState=treeState(sourceData);
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
var commonSky=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/ThemeStudies/LookWater/PreviewSky.mat");
if(commonSky==null)throw new Exception("A01 공통 룩 하늘 정확 경로 확인 필요. 원본 최신 DaySky를 수정하지 않음");
string sourceSkyPath=AssetDatabase.GetAssetPath(sourceSky),commonSkyPath=AssetDatabase.GetAssetPath(commonSky);string originalSkyHash=hash(sourceSkyPath),commonSkyHash=hash(commonSkyPath);
var sourceReflection = RenderSettings.defaultReflectionMode;
float reflectionIntensity = RenderSettings.reflectionIntensity;
var copy = UnityEngine.Object.Instantiate(sourceData);
copy.name = "RedTreeForest_Terrain";
UnityEditor.AssetDatabase.CreateAsset(copy, dataFolder + "/Terrain.asset");
if (copy == sourceData || copy.heightmapTexture==sourceData.heightmapTexture || copy.alphamapTextures.Any(t => sourceTextureIds.Contains(t.GetInstanceID())))
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
    layer.name=layers[i].name;
    string serializedSource=UnityEditor.EditorJsonUtility.ToJson(layers[i]);
    if(UnityEditor.EditorJsonUtility.ToJson(layer)!=serializedSource)throw new Exception("layer clone 설정 불일치");
    UnityEditor.AssetDatabase.CreateAsset(layer, destination);
    layers[i] = layer;
    layerMap.Add(new { sourcePath = original, sourceGuid = UnityEditor.AssetDatabase.AssetPathToGUID(original), path = destination, guid = UnityEditor.AssetDatabase.AssetPathToGUID(destination), index = i,serializedSettings=serializedSource });
}
string forestGroundSource=plan["forestGroundCandidate"].ToString();
var forestGround=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<TerrainLayer>(forestGroundSource));
string forestGroundPath=dataFolder+"/Layer_ForestGround.terrainlayer";UnityEditor.AssetDatabase.CreateAsset(forestGround,forestGroundPath);
int forestIndex=layers.Length;
copy.terrainLayers=layers.Concat(new[]{forestGround}).ToArray();
layerMap.Add(new{sourcePath=forestGroundSource,sourceGuid=UnityEditor.AssetDatabase.AssetPathToGUID(forestGroundSource),path=forestGroundPath,guid=UnityEditor.AssetDatabase.AssetPathToGUID(forestGroundPath),index=forestIndex});
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
terrainObject.name = "RedTreeForest_Terrain";
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
    var skyMaterial = UnityEngine.Object.Instantiate(commonSky);
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
// RP는 촬영 범위의 try/finally에서만 적용한다. 전역 작성자 컴포넌트를 복제하지 않는다.
var boundsInput=(float[])read(plan["referenceBounds"].ToString(),typeof(float[]));
var referenceBounds=new Bounds(new Vector3((boundsInput[0]+boundsInput[1])*.5f,200f,(boundsInput[2]+boundsInput[3])*.5f),new Vector3(boundsInput[1]-boundsInput[0],1200f,boundsInput[3]-boundsInput[2]));
var sourceEnvironment=sourceRoots.Single(g=>g.name=="AuthoredEnvironment");
var sourceAll=sourceRoots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var protectedEntities=sourceAll.SelectMany(t=>t.GetComponents<UPlayGround.Components.SceneEntityId>()).ToArray();
var protectedAnchors=(float[][])read(plan["protectedAnchors"].ToString(),typeof(float[][]));
float protectedRadius=Convert.ToSingle(plan["protectedRadius"]);
var corridor=(float[][])read(plan["preserveCorridor"].ToString(),typeof(float[][]));

float corridorHalfWidth=Convert.ToSingle(plan["corridorHalfWidth"]);
Func<Vector2,Vector2,Vector2,float> segmentDistance=(p,a,b)=>{var ab=b-a;float t=ab.sqrMagnitude>.00001f?Mathf.Clamp01(Vector2.Dot(p-a,ab)/ab.sqrMagnitude):0f;return Vector2.Distance(p,a+ab*t);};
Func<Vector3,bool> isProtected=p=>protectedEntities.Any(e=>new Vector2(p.x-e.transform.position.x,p.z-e.transform.position.z).sqrMagnitude<protectedRadius*protectedRadius)||protectedAnchors.Any(a=>new Vector2(p.x-a[0],p.z-a[2]).sqrMagnitude<protectedRadius*protectedRadius);
Func<Vector3,bool> isInCorridor=p=>Enumerable.Range(1,corridor.Length-1).Any(i=>segmentDistance(new Vector2(p.x,p.z),new Vector2(corridor[i-1][0],corridor[i-1][1]),new Vector2(corridor[i][0],corridor[i][1]))<corridorHalfWidth);
var markerRoot=new GameObject("ProtectedContentMarkers");markerRoot.transform.SetParent(reference.transform,false);
foreach(var e in protectedEntities.Where(e=>referenceBounds.Contains(e.transform.position))){var marker=new GameObject(e.Guid+"_"+e.name);marker.transform.SetParent(markerRoot.transform,false);marker.transform.position=e.transform.position;}
for(int i=0;i<protectedAnchors.Length;i++){var marker=new GameObject("ProtectedStoryAnchor_"+i);marker.transform.SetParent(markerRoot.transform,false);marker.transform.position=new Vector3(protectedAnchors[i][0],protectedAnchors[i][1],protectedAnchors[i][2]);}
var context=new GameObject("ExistingEnvironmentContext_ExcludeFromIntegration");context.transform.SetParent(reference.transform,false);
var rendererMap=new Dictionary<int,Renderer>();
foreach(var original in sourceEnvironment.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.bounds.Intersects(referenceBounds))){
    var filter=original.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null)continue;
    var item=new GameObject(original.name);item.transform.SetParent(context.transform,false);item.transform.SetPositionAndRotation(original.transform.position,original.transform.rotation);item.transform.localScale=original.transform.lossyScale;
    item.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;var renderer=item.AddComponent<MeshRenderer>();UnityEditor.EditorUtility.CopySerialized(original,renderer);renderer.probeAnchor=null;
    item.SetActive(original.gameObject.activeInHierarchy);rendererMap.Add(original.GetInstanceID(),renderer);
}
var assignedLodRenderers=new HashSet<int>();
foreach(var original in sourceEnvironment.GetComponentsInChildren<LODGroup>(true)){
    var lods=original.GetLODs();var used=lods.SelectMany(l=>l.renderers).Where(r=>r!=null&&rendererMap.ContainsKey(r.GetInstanceID())).Select(r=>r.GetInstanceID()).Distinct().ToArray();
    if(used.Length==0||used.Any(id=>assignedLodRenderers.Contains(id)))continue;
    var item=new GameObject(original.name+"_LOD");item.transform.SetParent(context.transform,false);item.transform.SetPositionAndRotation(original.transform.position,original.transform.rotation);item.transform.localScale=original.transform.lossyScale;
    var lod=item.AddComponent<LODGroup>();UnityEditor.EditorUtility.CopySerialized(original,lod);
    for(int i=0;i<lods.Length;i++)lods[i].renderers=lods[i].renderers.Where(r=>r!=null&&rendererMap.ContainsKey(r.GetInstanceID())).Select(r=>rendererMap[r.GetInstanceID()]).ToArray();
    lod.SetLODs(lods);foreach(int id in used)assignedLodRenderers.Add(id);
}

var additions=new GameObject("Theme_RedTreeForest_Environment");additions.transform.SetParent(environment.transform,false);
if(RenderSettings.skybox!=null&&RenderSettings.skybox.HasProperty("_AtmosphereThickness"))RenderSettings.skybox.SetFloat("_AtmosphereThickness",1f);
var comparisonRoot=new GameObject("ExistingAssetComparison_DisabledUntilCapture");comparisonRoot.transform.SetParent(preview.transform,false);comparisonRoot.SetActive(false);
var candidatePaths=(string[])read(plan["comparisonPrefabs"].ToString(),typeof(string[]));var candidateRows=new List<object>();
foreach(string candidatePath in candidatePaths){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(candidatePath);if(prefab==null)throw new Exception("비교 후보 정확 경로 누락: "+candidatePath);candidateRows.Add(new{path=candidatePath,guid=AssetDatabase.AssetPathToGUID(candidatePath),rendererCount=prefab.GetComponentsInChildren<Renderer>(true).Length,lodCount=prefab.GetComponentsInChildren<LODGroup>(true).Length,colliderCount=prefab.GetComponentsInChildren<Collider>(true).Length,scriptCount=prefab.GetComponentsInChildren<MonoBehaviour>(true).Length,status="metadata only; actual-render comparison pending"});}
for(int i=0;i<copy.detailPrototypes.Length;i++)if(contentHash(copy.GetDetailLayer(0,0,copy.detailWidth,copy.detailHeight,i))!=sourceDetailHashes[i])throw new Exception("clone detail equality failed");
if(contentHash(copy.GetHeights(0,0,copy.heightmapResolution,copy.heightmapResolution))!=sourceHeightHash||contentHash(copy.GetHoles(0,0,copy.holesResolution,copy.holesResolution))!=sourceHolesHash||treeState(copy)!=sourceTreesState)throw new Exception("clone full geometry equality failed");
if(hash(sourcePath)!=sourceHash||hash(sourceScene.path)!=sourceSceneHash||sourceScene.isDirty!=sourceDirty||EditorUtility.IsDirty(sourceData)||hash(baseline["navMesh"].ToString())!=plan["baselineNavMeshHash"].ToString()||contentHash(sourceData.GetAlphamaps(0,0,sourceData.alphamapWidth,sourceData.alphamapHeight))!=sourceAlphaHash||hash(sourceSkyPath)!=originalSkyHash||hash(commonSkyPath)!=commonSkyHash||entityGroundState()!=initialEntityGroundState)throw new Exception("source baseline/317ID/ground/sky/memory alpha changed");
EditorUtility.SetDirty(copy);AssetDatabase.SaveAssetIfDirty(copy);if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(review,scenePath))throw new Exception("baseline scene save failed");
var report=new{status="baseline clone only; no terrain painting, height editing or additions",scene=scenePath,terrain=dataFolder+"/Terrain.asset",sourceSceneHash=sourceSceneHash,sourceTerrainHash=sourceHash,hostDirtyBefore=sourceDirty,hostDirtyAfter=sourceScene.isDirty,sourceLayerCount=layers.Length,privateLayerCount=copy.terrainLayers.Length,layerMap=layerMap,fullAlphaEquality=true,fullHeightEquality=true,fullDetailEquality=true,fullHolesEquality=true,treeInstancesEquality=true,scatterMode=copy.detailScatterMode.ToString(),sourceFilesEqual=true,sourceNavHash=hash(baseline["navMesh"].ToString()),nativeReferenceRendererCount=rendererMap.Count,comparisonCandidates=candidateRows,comparisonStatus="actual screenshots pending",coreAuthorized=false,generatedAssets=new object[0]};
System.IO.File.WriteAllText(output+"/baseline-scaffold.json",stateJson(report));return "A04 baseline private clone saved; no core edits; context="+rendererMap.Count;
