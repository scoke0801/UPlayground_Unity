// A00 lease 아래 열린 Host를 보존한 채 독립 비교실만 만든다.
var sourceScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || sourceScene.path != "Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")
    throw new Exception("A00 기준선 확보 후 Host Edit Mode에서 실행해야 합니다.");
try{
string scenePath = "Assets/01.Scenes/ArtReview/WorldThemes/Theme_RedTreeForest.unity";
string dataFolder = "Assets/10.Datas/World/ThemeStudies/RedTreeForest";
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
var approval=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/WorldThemes/Integration/A04-core-approval-v1.json"),typeof(Dictionary<string,object>));
if(!Convert.ToBoolean(approval["coreAlphaWriteAllowed"])||Convert.ToBoolean(approval["environmentPlacementAllowed"])||Convert.ToBoolean(approval["historicalDecorationReplacementAllowed"]))throw new Exception("A00 첫 표면만 승인한 계약 필요");
var approvedPolygon=(float[][])read(approval["corePolygon"].ToString(),typeof(float[][]));var inputPolygon=(float[][])read(plan["corePolygon"].ToString(),typeof(float[][]));if(!approvedPolygon.SelectMany(p=>p).SequenceEqual(inputPolygon.SelectMany(p=>p)))throw new Exception("정확 승인 core polygon 불일치");
var eligible=(Dictionary<string,object>)read(approval["eligibleMask"].ToString(),typeof(Dictionary<string,object>));if(Convert.ToSingle(plan["protectedRadius"])<Convert.ToSingle(eligible["protectedHostEntityRadiusMeters"])||Convert.ToSingle(plan["corridorHalfWidth"])<Convert.ToSingle(eligible["pathHalfWidthMeters"])||Convert.ToSingle(plan["maximumSurfaceSlope"])>Convert.ToSingle(eligible["maximumSlopeDegrees"]))throw new Exception("승인 보호/경로/급사면 제한보다 약한 입력");
if(baseline["sourceSceneHash"].ToString()!=plan["baselineSceneHash"].ToString()||baseline["sourceTerrainHash"].ToString()!=plan["baselineTerrainHash"].ToString())throw new Exception("A00 baseline-v2 계약과 A04 입력 불일치");
if (!Convert.ToBoolean(plan["maskAuthorized"])) throw new Exception("A00 core mask 승인 전 편집할 수 없습니다.");
if (!Convert.ToBoolean(plan["corridorConfirmed"])) throw new Exception("실제 길·후퇴로·뿌리·동굴·서사 VFX 조사 전에는 편집하지 않습니다.");
var polygon = (float[][])read(plan["corePolygon"].ToString(), typeof(float[][]));
if (polygon.Length < 3) throw new Exception("A00 core polygon이 없습니다.");
Func<Vector3,bool> isInsideCore = p => {
    bool inside = false;
    for (int i=0,j=polygon.Length-1;i<polygon.Length;j=i++)
        if ((polygon[i][1]>p.z)!=(polygon[j][1]>p.z) && p.x<(polygon[j][0]-polygon[i][0])*(p.z-polygon[i][1])/(polygon[j][1]-polygon[i][1])+polygon[i][0]) inside=!inside;
    return inside;
};
var preflightPlacements=(Dictionary<string,object>[])read(plan["placements"].ToString(),typeof(Dictionary<string,object>[]));
if(preflightPlacements.Length!=0)throw new Exception("환경 추가는 아직 승인되지 않았습니다.");
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
var runtimeProtection=(Dictionary<string,object>[])read(approval["runtimeProtection"].ToString(),typeof(Dictionary<string,object>[]));
foreach(var anchor in runtimeProtection)if(hash(anchor["source"].ToString())!=anchor["sourceSha256"].ToString())throw new Exception("runtime placement SO hash 변동; 보호 조사 재확인 필요");
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
if(corridor.Length<2)throw new Exception("보존할 실제 길 표본이 없습니다.");
var corridors=(float[][][])read(approval["preservePathsXZ"].ToString(),typeof(float[][][]));if(corridors.Length!=2)throw new Exception("승인된 실제 두 이동경로 필요");
float corridorHalfWidth=Convert.ToSingle(plan["corridorHalfWidth"]);
Func<Vector2,Vector2,Vector2,float> segmentDistance=(p,a,b)=>{var ab=b-a;float t=ab.sqrMagnitude>.00001f?Mathf.Clamp01(Vector2.Dot(p-a,ab)/ab.sqrMagnitude):0f;return Vector2.Distance(p,a+ab*t);};
var runtimeAnchorPositions=runtimeProtection.Select(a=>(float[])read(a["worldPosition"].ToString(),typeof(float[]))).ToArray();
Func<Vector3,bool> isProtected=p=>protectedEntities.Any(e=>new Vector2(p.x-e.transform.position.x,p.z-e.transform.position.z).sqrMagnitude<protectedRadius*protectedRadius)||protectedAnchors.Any(a=>new Vector2(p.x-a[0],p.z-a[2]).sqrMagnitude<protectedRadius*protectedRadius)||runtimeAnchorPositions.Any(a=>new Vector2(p.x-a[0],p.z-a[2]).sqrMagnitude<400f);
Func<Vector3,bool> isInCorridor=p=>corridors.Any(line=>Enumerable.Range(1,line.Length-1).Any(i=>segmentDistance(new Vector2(p.x,p.z),new Vector2(line[i-1][0],line[i-1][1]),new Vector2(line[i][0],line[i][1]))<corridorHalfWidth));
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
var redRoot=verifiedRedRoot;
if(redRoot==null)throw new Exception("실제 붉은숲 환경 루트가 없습니다.");
var treeCandidates=redRoot.GetComponentsInChildren<Transform>(true).Select(t=>UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject)).Where(g=>g!=null).Distinct().Where(g=>{string path=UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g);return path.Contains("FullTree_01_Red")||path.Contains("BroadleafTree_")&&path.Contains("_Red");}).Where(g=>referenceBounds.Contains(g.transform.position)).ToArray();
var canopyEvidence=new List<object>();var validTrees=new List<Vector3>();
foreach(var candidate in treeCandidates){var renderers=candidate.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)continue;var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);bool selected=bounds.size.y>=2f&&candidate.activeInHierarchy;if(selected)validTrees.Add(candidate.transform.position);canopyEvidence.Add(new{path=UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(candidate),name=candidate.name,globalId=GlobalObjectId.GetGlobalObjectIdSlow(candidate).ToString(),position=new[]{candidate.transform.position.x,candidate.transform.position.y,candidate.transform.position.z},boundsMin=new[]{bounds.min.x,bounds.min.y,bounds.min.z},boundsMax=new[]{bounds.max.x,bounds.max.y,bounds.max.z},actualHeight=bounds.size.y,selected=selected,reason=selected?"정상 활성 수목 renderer높이2m이상":"납작낙엽 대용/비활성/정상수목높이 미달 — 수관anchor 제외"});}
var existingTrees=validTrees.ToArray();System.IO.File.WriteAllText(output+"/canopy-selection.json",stateJson(canopyEvidence));
if(existingTrees.Length==0)throw new Exception("후보 reference 안에 실제 붉은 수목이 없습니다. 범위를 재조사해야 합니다.");
int earthIndex=Array.FindIndex(sourceData.terrainLayers,l=>UnityEditor.AssetDatabase.GetAssetPath(l)==plan["earthSourceLayer"].ToString());
int grassIndex=Array.FindIndex(sourceData.terrainLayers,l=>UnityEditor.AssetDatabase.GetAssetPath(l)==plan["grassSourceLayer"].ToString());
int roadIndex=Array.FindIndex(sourceData.terrainLayers,l=>UnityEditor.AssetDatabase.GetAssetPath(l)==plan["roadSourceLayer"].ToString());
if(earthIndex<0||grassIndex<0||roadIndex<0)throw new Exception("기준선 표면 매핑 실패");
float edgeWidth=Convert.ToSingle(plan["coreEdgeBlendMeters"]),canopyRadius=Convert.ToSingle(plan["canopyGroundRadius"]),slopeLimit=Convert.ToSingle(plan["maximumSurfaceSlope"]),roadThreshold=Convert.ToSingle(plan["roadAlphaThreshold"]);
float forestWeight=Convert.ToSingle(plan["forestGroundWeight"]),earthWeight=Convert.ToSingle(plan["earthWeight"]);
var alpha=copy.GetAlphamaps(0,0,copy.alphamapWidth,copy.alphamapHeight);var editedCells=new List<object>();var allowedCells=new bool[copy.alphamapHeight,copy.alphamapWidth];
for(int z=0;z<copy.alphamapHeight;z++)for(int x=0;x<copy.alphamapWidth;x++){
    var p=new Vector3(sourceTerrain.transform.position.x+x*copy.size.x/copy.alphamapWidth,0f,sourceTerrain.transform.position.z+z*copy.size.z/copy.alphamapHeight);
    if(!isInsideCore(p)||isProtected(p)||isInCorridor(p)||alpha[z,x,roadIndex]>=roadThreshold)continue;
    float nx=(p.x-sourceTerrain.transform.position.x)/copy.size.x,nz=(p.z-sourceTerrain.transform.position.z)/copy.size.z;
    if(sourceData.GetSteepness(nx,nz)>slopeLimit)continue;
    float treeDistance=existingTrees.Min(t=>new Vector2(t.x-p.x,t.z-p.z).magnitude);if(treeDistance>=canopyRadius)continue;
    float edge=float.MaxValue;for(int i=0;i<polygon.Length;i++)edge=Mathf.Min(edge,segmentDistance(new Vector2(p.x,p.z),new Vector2(polygon[i][0],polygon[i][1]),new Vector2(polygon[(i+1)%polygon.Length][0],polygon[(i+1)%polygon.Length][1])));
    float blend=Mathf.SmoothStep(0f,1f,Mathf.Clamp01(edge/edgeWidth))*Mathf.SmoothStep(0f,1f,1f-treeDistance/canopyRadius);
    for(int l=0;l<alpha.GetLength(2);l++){float target=l==forestIndex?forestWeight:l==earthIndex?earthWeight:l==grassIndex?1f-forestWeight-earthWeight:0f;alpha[z,x,l]=Mathf.Lerp(alpha[z,x,l],target,blend);}
    float sum=0f;for(int l=0;l<alpha.GetLength(2);l++){float value=alpha[z,x,l];if(float.IsNaN(value)||float.IsInfinity(value)||value<0f)throw new Exception("새 도색셀 finite/비음수 위반");sum+=value;}if(sum<=0f)throw new Exception("새 도색셀 합계 양수 위반");
    int units=0;var fractions=new float[alpha.GetLength(2)];var quantized=new int[alpha.GetLength(2)];for(int l=0;l<alpha.GetLength(2);l++){float value=alpha[z,x,l]/sum*255f;quantized[l]=Mathf.FloorToInt(value);fractions[l]=value-quantized[l];units+=quantized[l];}while(units<255){int best=0;for(int l=1;l<fractions.Length;l++)if(fractions[l]>fractions[best])best=l;quantized[best]++;fractions[best]=-1;units++;}if(units!=255)throw new Exception("새 셀 UNorm8 합255 실패");for(int l=0;l<alpha.GetLength(2);l++)alpha[z,x,l]=quantized[l]/255f;allowedCells[z,x]=true;
    editedCells.Add(new{x=x,z=z,worldX=p.x,worldZ=p.z,nearestRedTree=treeDistance,blend=blend});
}
if(editedCells.Count<Convert.ToInt32(plan["minimumEditedCells"]))throw new Exception("실제 수관 아래 편집 셀이 부족합니다. core와 길 제외를 재검토해야 합니다.");
copy.SetAlphamaps(0,0,alpha);terrain.Flush();
var additions=new GameObject("Theme_RedTreeForest_Environment");additions.transform.SetParent(environment.transform,false);
var assets=new List<object>();
foreach(var placement in preflightPlacements){
    string path=placement["prefab"].ToString();var p=(float[])read(placement["positionXZ"].ToString(),typeof(float[]));var position=new Vector3(p[0],0f,p[1]);
    if(!isInsideCore(position)||isProtected(position)||isInCorridor(position))throw new Exception("추가 환경이 core/보호/길 계약을 벗어났습니다.");
    position.y=copy.GetInterpolatedHeight((p[0]-sourceTerrain.transform.position.x)/copy.size.x,(p[1]-sourceTerrain.transform.position.z)/copy.size.z)+sourceTerrain.transform.position.y+Convert.ToSingle(placement["groundOffset"]);
    var instance=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path),review);instance.name=placement["name"].ToString();instance.transform.SetParent(additions.transform,false);instance.transform.SetPositionAndRotation(position,Quaternion.Euler(0f,Convert.ToSingle(placement["yaw"]),0f));var scale=(float[])read(placement["scale"].ToString(),typeof(float[]));instance.transform.localScale=new Vector3(scale[0],scale[1],scale[2]);
    assets.Add(new{path=path,guid=UnityEditor.AssetDatabase.AssetPathToGUID(path),instance=instance.name,position=new float[]{position.x,position.y,position.z},renderStatus="미검증"});
}
Physics.SyncTransforms();
var footprintRows=new List<object>();int footprintErrors=0;
foreach(Transform item in additions.transform)foreach(var component in item.GetComponentsInChildren<Component>(true).Where(c=>c is Renderer||c is Collider)){
    var b=component is Renderer?((Renderer)component).bounds:((Collider)component).bounds;
    bool contained=isInsideCore(new Vector3(b.min.x,0f,b.min.z))&&isInsideCore(new Vector3(b.min.x,0f,b.max.z))&&isInsideCore(new Vector3(b.max.x,0f,b.min.z))&&isInsideCore(new Vector3(b.max.x,0f,b.max.z));
    Func<float,float,bool> overlapsPoint=(x,z)=>Mathf.Pow(Mathf.Max(b.min.x-x,0f,x-b.max.x),2)+Mathf.Pow(Mathf.Max(b.min.z-z,0f,z-b.max.z),2)<protectedRadius*protectedRadius;
    bool protectedOverlap=protectedEntities.Any(e=>overlapsPoint(e.transform.position.x,e.transform.position.z))||protectedAnchors.Any(a=>overlapsPoint(a[0],a[2]));
    bool corridorOverlap=Enumerable.Range(1,corridor.Length-1).Any(i=>b.min.x<=Mathf.Max(corridor[i-1][0],corridor[i][0])+corridorHalfWidth&&b.max.x>=Mathf.Min(corridor[i-1][0],corridor[i][0])-corridorHalfWidth&&b.min.z<=Mathf.Max(corridor[i-1][1],corridor[i][1])+corridorHalfWidth&&b.max.z>=Mathf.Min(corridor[i-1][1],corridor[i][1])-corridorHalfWidth);
    if(!contained||protectedOverlap||corridorOverlap)footprintErrors++;
    footprintRows.Add(new{instance=item.name,component=component.GetType().Name,name=component.name,min=new float[]{b.min.x,b.min.y,b.min.z},max=new float[]{b.max.x,b.max.y,b.max.z},insideCore=contained,protectedOverlap=protectedOverlap,corridorOverlap=corridorOverlap});
}
if(footprintErrors>0)throw new Exception("환경 footprint가 core 밖입니다.");
int outsideChanges=0,excludedInsideChanges=0,actualChangedCells=0;float maximumNewCellSumError=0f;var afterAlpha=copy.GetAlphamaps(0,0,copy.alphamapWidth,copy.alphamapHeight);
for(int z=0;z<copy.alphamapHeight;z++)for(int x=0;x<copy.alphamapWidth;x++){
    var p=new Vector3(sourceTerrain.transform.position.x+x*copy.size.x/copy.alphamapWidth,0f,sourceTerrain.transform.position.z+z*copy.size.z/copy.alphamapHeight);bool changed=false;float sum=0f;for(int l=0;l<afterAlpha.GetLength(2);l++){float value=afterAlpha[z,x,l];if(value!=(l<alphaBeforeLayerRemap.GetLength(2)?alphaBeforeLayerRemap[z,x,l]:0f))changed=true;if(allowedCells[z,x]&&(float.IsNaN(value)||float.IsInfinity(value)||value<0f))throw new Exception("저장된 새셀 finite/비음수 실패");sum+=value;}
    if(changed){actualChangedCells++;if(!isInsideCore(p))outsideChanges++;if(!allowedCells[z,x])excludedInsideChanges++;}
    if(allowedCells[z,x])maximumNewCellSumError=Mathf.Max(maximumNewCellSumError,Mathf.Abs(sum-1f));
}
if(outsideChanges!=0||excludedInsideChanges!=0||actualChangedCells==0||maximumNewCellSumError>1e-5f||contentHash(copy.GetHeights(0,0,copy.heightmapResolution,copy.heightmapResolution))!=sourceHeightHash)throw new Exception("core/제외영역/새셀 합계 또는 height 보존 실패");
if(contentHash(copy.GetHoles(0,0,copy.holesResolution,copy.holesResolution))!=sourceHolesHash||treeState(copy)!=sourceTreesState)throw new Exception("holes 또는 tree instance 보존 실패");
for(int i=0;i<copy.detailPrototypes.Length;i++)if(contentHash(copy.GetDetailLayer(0,0,copy.detailWidth,copy.detailHeight,i))!=sourceDetailHashes[i]||contentHash(sourceData.GetDetailLayer(0,0,sourceData.detailWidth,sourceData.detailHeight,i))!=sourceDetailHashes[i])throw new Exception("Detail 맵 보존 실패");
if(hash(sourcePath)!=sourceHash||hash(sourceScene.path)!=sourceSceneHash||sourceScene.isDirty!=sourceDirty||contentHash(sourceData.GetAlphamaps(0,0,sourceData.alphamapWidth,sourceData.alphamapHeight))!=sourceAlphaHash||contentHash(sourceData.GetHeights(0,0,sourceData.heightmapResolution,sourceData.heightmapResolution))!=sourceHeightHash||hash(baseline["navMesh"].ToString())!=plan["baselineNavMeshHash"].ToString())throw new Exception("원본 보존 실패");
if(EditorUtility.IsDirty(sourceData)||entityGroundState()!=initialEntityGroundState||hash(sourceSkyPath)!=originalSkyHash||hash(commonSkyPath)!=commonSkyHash||runtimeProtection.Any(a=>hash(a["source"].ToString())!=a["sourceSha256"].ToString()))throw new Exception("원본317ID/접지/runtimeSO/하늘/dirty 보존 실패");
System.IO.File.WriteAllText(output+"/surface-verification.json",stateJson(new{approval=approval["version"].ToString(),actualChangedCells=actualChangedCells,allowedCells=editedCells.Count,excludedInsideChanges=excludedInsideChanges,outsideCoreChangedCells=outsideChanges,maximumNewCellSumError=maximumNewCellSumError,sourceIdCount=baselineEntities.Length,sourceIdsGroundUnchanged=true,sourceRuntimeSoUnchanged=true,sourceSkyFilesUnchanged=true,heightUnchanged=true,detailsUnchanged=true,holesUnchanged=true,treesUnchanged=true,originalAlphaMemoryUnchanged=true,environmentAdded=0,decorationsReplaced=0,preservedPaths=corridors,runtimeProtectedRadius=20f,hostProtectedRadius=protectedRadius}));
if(additions.GetComponentsInChildren<MonoBehaviour>(true).Length!=0||additions.GetComponentsInChildren<Terrain>(true).Length!=0||additions.GetComponentsInChildren<Camera>(true).Length!=0||additions.GetComponentsInChildren<Light>(true).Length!=0||additions.GetComponentsInChildren<Transform>(true).Any(t=>UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)!=0))throw new Exception("최종 additions prefab 금지 컴포넌트/누락 스크립트 발견");
var poses=(Dictionary<string,object>[])read(plan["capturePoses"].ToString(),typeof(Dictionary<string,object>[]));
if(poses.Length>0){var p=(float[])read(poses[0]["position"].ToString(),typeof(float[]));var target=(float[])read(poses[0]["target"].ToString(),typeof(float[]));camera.transform.position=new Vector3(p[0],p[1],p[2]);camera.transform.rotation=Quaternion.LookRotation(new Vector3(target[0],target[1],target[2])-camera.transform.position);}
if(RenderSettings.skybox!=null&&RenderSettings.skybox.HasProperty("_AtmosphereThickness"))RenderSettings.skybox.SetFloat("_AtmosphereThickness",1f);
UnityEditor.EditorUtility.SetDirty(copy);UnityEditor.AssetDatabase.SaveAssetIfDirty(copy);
string prefabPath="Assets/03.Prefabs/Environment/WorldThemes/RedTreeForest/Theme_RedTreeForest_Environment.prefab";System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(prefabPath));if(System.IO.File.Exists(prefabPath))throw new Exception("기존 환경 prefab 덮어쓰기 금지");UnityEditor.PrefabUtility.SaveAsPrefabAsset(additions,prefabPath);
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(review,scenePath))throw new Exception("RedTreeForest 씬 저장 실패");
var report=new{themeId="RedTreeForest",version=plan["version"].ToString(),status="독립 대표 제작 — 사진/사용자 인수 전",sourceScene=sourceScene.path,sourceTerrainGuid=UnityEditor.AssetDatabase.AssetPathToGUID(sourcePath),sourceTerrainHash=sourceHash,baselineSceneHash=sourceSceneHash,scene=scenePath,terrain=dataFolder+"/Terrain.asset",environmentPrefab=prefabPath,coreMask=polygon,referenceBounds=boundsInput,referenceBoundsOrder=new[]{"minX","maxX","minZ","maxZ"},rootTransform="identity",coordinateSpace="world",layerMap=layerMap,scatterMode=copy.detailScatterMode.ToString(),protectedEntities=protectedEntities.Where(e=>referenceBounds.Contains(e.transform.position)).Select(e=>new{id=e.Guid,position=e.transform.position.ToString("R")}).ToArray(),protectedNonIdAnchors=protectedAnchors,preservedCorridor=corridor,actualExistingRedTrees=existingTrees.Select(p=>new float[]{p.x,p.y,p.z}).ToArray(),editedCellCount=editedCells.Count,unchangedOutsideCoreCheck=new{alphaChanged=outsideChanges,heightEqual=true,detailsEqual=true,sourceFilesEqual=true},footprints=footprintRows,assets=assets,replaceObjects=new object[0],deleteObjects=new object[0],generatedAssets=new object[0],capturePoses=poses,knownIssues=new[]{"서사 뿌리/동굴/VFX는 보호 참조이며 임의 생성/교체 없음", "첫 대표 수관 가장자리만 저작, 전체 테마 아님", "실제 TPS/Play/충돌보행/LOD 전이/성능/미술 인수 미수행"},nextOwner="A00/A12"};
var serialize=jsonType.GetMethod("SerializeObject",new Type[]{typeof(object)});System.IO.File.WriteAllText(output+"/handoff.json",(string)serialize.Invoke(null,new object[]{report}));System.IO.File.WriteAllText(output+"/edited-cells.json",(string)serialize.Invoke(null,new object[]{editedCells}));
return "RedTreeForest 대표 씬 생성, 편집셀="+editedCells.Count+"/core밖0/원본보존. 사진검토 대기.";

}finally // A04
{if(sourceScene.isLoaded)UnityEngine.SceneManagement.SceneManager.SetActiveScene(sourceScene);}
