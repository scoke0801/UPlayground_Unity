// A00 lease 아래 열린 Host를 보존한 채 독립 비교실만 만든다.
var sourceScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || sourceScene.path != "Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")
    throw new Exception("A00 기준선 확보 후 Host Edit Mode에서 실행해야 합니다.");
string scenePath = "Assets/01.Scenes/ArtReview/WorldThemes/Theme_LakeShrine.unity";
string dataFolder = "Assets/10.Datas/World/ThemeStudies/LakeShrine";
string output = "output/WorldThemeProduction/LakeShrine";
if (System.IO.File.Exists(scenePath) || System.IO.File.Exists(dataFolder + "/Terrain.asset"))
    throw new Exception("기존 A02 산출물이 있어 덮어쓰지 않았습니다.");
var jsonType = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Newtonsoft.Json.JsonConvert")).First(t => t != null);
var deserialize = jsonType.GetMethod("DeserializeObject", new Type[] { typeof(string), typeof(Type) });
Func<string, Type, object> read = (text, type) => deserialize.Invoke(null, new object[] { text, type });
var plan = (Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/WorldThemes/A02/lake-study.json"), typeof(Dictionary<string,object>));
if (!Convert.ToBoolean(plan["maskAuthorized"])) throw new Exception("A00 core mask 승인 전 편집할 수 없습니다.");
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
copy.name = "LakeShrine_Terrain";
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
string riverBedSource=plan["riverBedCandidate"].ToString();
var riverBed=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<TerrainLayer>(riverBedSource));
string riverBedPath=dataFolder+"/Layer_RiverBed.terrainlayer";UnityEditor.AssetDatabase.CreateAsset(riverBed,riverBedPath);
int riverBedIndex=layers.Length;
copy.terrainLayers=layers.Concat(new[]{riverBed}).ToArray();
layerMap.Add(new{sourcePath=riverBedSource,sourceGuid=UnityEditor.AssetDatabase.AssetPathToGUID(riverBedSource),path=riverBedPath,guid=UnityEditor.AssetDatabase.AssetPathToGUID(riverBedPath),index=riverBedIndex});
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
terrainObject.name = "LakeShrine_Terrain";
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
var boundsInput = (float[])read(plan["referenceBounds"].ToString(),typeof(float[]));
var referenceBounds = new Bounds(new Vector3((boundsInput[0]+boundsInput[1])*.5f,200f,(boundsInput[2]+boundsInput[3])*.5f),new Vector3(boundsInput[1]-boundsInput[0],1200f,boundsInput[3]-boundsInput[2]));
var sourceAll=sourceRoots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var sourceWater=sourceAll.Single(t=>t.name==plan["waterSourceObject"].ToString());
var sourceWaterRenderer=sourceWater.GetComponent<MeshRenderer>();
var sourceWaterMesh=sourceWater.GetComponent<MeshFilter>().sharedMesh;
float waterY=sourceWater.position.y;
var waterVertices=sourceWaterMesh.vertices.Select(v=>sourceWater.TransformPoint(v)).ToArray();
var waterTriangles=sourceWaterMesh.triangles;
Func<Vector3,Vector3,Vector3,float> cross=(a,b,p)=>(b.x-a.x)*(p.z-a.z)-(b.z-a.z)*(p.x-a.x);
Func<Vector3,bool> isInsideWater=p=>{
    for(int i=0;i<waterTriangles.Length;i+=3){
        var a=waterVertices[waterTriangles[i]]; var b=waterVertices[waterTriangles[i+1]]; var c=waterVertices[waterTriangles[i+2]];
        if(Mathf.Abs(cross(a,b,c))<.00001f) continue;
        float ab=cross(a,b,p),bc=cross(b,c,p),ca=cross(c,a,p);
        if((ab>=0&&bc>=0&&ca>=0)||(ab<=0&&bc<=0&&ca<=0))return true;
    }
    return false;
};
var protectedEntities=sourceAll.SelectMany(t=>t.GetComponents<UPlayGround.Components.SceneEntityId>()).Where(c=>referenceBounds.Contains(c.transform.position)).ToArray();
float protectedRadius=Convert.ToSingle(plan["protectedRadius"]);
var protectedAnchors=(float[][])read(plan["protectedAnchors"].ToString(),typeof(float[][]));
Func<Vector3,bool> isProtected=p=>protectedEntities.Any(e=>new Vector2(p.x-e.transform.position.x,p.z-e.transform.position.z).sqrMagnitude<protectedRadius*protectedRadius)||protectedAnchors.Any(a=>new Vector2(p.x-a[0],p.z-a[2]).sqrMagnitude<protectedRadius*protectedRadius);
var markerRoot=new GameObject("ProtectedContentMarkers"); markerRoot.transform.SetParent(reference.transform,false);
foreach(var entity in protectedEntities){var marker=new GameObject(entity.Guid+"_"+entity.name);marker.transform.SetParent(markerRoot.transform,false);marker.transform.position=entity.transform.position;}
for(int i=0;i<protectedAnchors.Length;i++){var marker=new GameObject("ProtectedStoryAnchor_"+i);marker.transform.SetParent(markerRoot.transform,false);marker.transform.position=new Vector3(protectedAnchors[i][0],protectedAnchors[i][1],protectedAnchors[i][2]);}
var context=new GameObject("ExistingEnvironmentContext_ExcludeFromIntegration");context.transform.SetParent(reference.transform,false);
var sourceEnvironment=sourceRoots.Single(g=>g.name=="AuthoredEnvironment");
var contextRenderers=sourceEnvironment.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r!=sourceWaterRenderer&&r.bounds.Intersects(referenceBounds)).ToArray();
var rendererMap=new Dictionary<int,Renderer>();
foreach(var original in contextRenderers){
    var filter=original.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null)continue;
    var cloneObject=new GameObject(original.name);cloneObject.transform.SetParent(context.transform,false);
    cloneObject.transform.SetPositionAndRotation(original.transform.position,original.transform.rotation);cloneObject.transform.localScale=original.transform.lossyScale;
    cloneObject.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;
    var cloneRenderer=cloneObject.AddComponent<MeshRenderer>();UnityEditor.EditorUtility.CopySerialized(original,cloneRenderer);cloneRenderer.probeAnchor=null;
    cloneObject.SetActive(original.gameObject.activeInHierarchy);rendererMap.Add(original.GetInstanceID(),cloneRenderer);
}
foreach(var sourceLod in sourceEnvironment.GetComponentsInChildren<LODGroup>(true)){
    var lods=sourceLod.GetLODs();if(!lods.Any(l=>l.renderers.Any(r=>r!=null&&rendererMap.ContainsKey(r.GetInstanceID()))))continue;
    var lodObject=new GameObject(sourceLod.name+"_LOD");lodObject.transform.SetParent(context.transform,false);
    lodObject.transform.SetPositionAndRotation(sourceLod.transform.position,sourceLod.transform.rotation);lodObject.transform.localScale=sourceLod.transform.lossyScale;
    var lodClone=lodObject.AddComponent<LODGroup>();UnityEditor.EditorUtility.CopySerialized(sourceLod,lodClone);
    for(int i=0;i<lods.Length;i++)lods[i].renderers=lods[i].renderers.Where(r=>r!=null&&rendererMap.ContainsKey(r.GetInstanceID())).Select(r=>rendererMap[r.GetInstanceID()]).ToArray();
    lodClone.SetLODs(lods);
}
// 수면과 기존 장식은 참고 레이어에만 두고 원본 개체를 교체하는 프리팹으로 내보내지 않는다.
var waterObject=new GameObject("LakeSurface_MaterialTrial_ExcludeFromIntegration");waterObject.transform.SetParent(reference.transform,false);
waterObject.transform.SetPositionAndRotation(sourceWater.position,sourceWater.rotation);waterObject.transform.localScale=sourceWater.lossyScale;
waterObject.AddComponent<MeshFilter>().sharedMesh=sourceWaterMesh;
var waterRenderer=waterObject.AddComponent<MeshRenderer>();UnityEditor.EditorUtility.CopySerialized(sourceWaterRenderer,waterRenderer);
var waterMaterial=UnityEngine.Object.Instantiate(sourceWaterRenderer.sharedMaterial);waterMaterial.name="LakeShrine_Water";
string waterMaterialPath=dataFolder+"/LakeWater.mat";UnityEditor.AssetDatabase.CreateAsset(waterMaterial,waterMaterialPath);waterRenderer.sharedMaterial=waterMaterial;
var waterOverrides=(Dictionary<string,object>)read(plan["waterOverrides"].ToString(),typeof(Dictionary<string,object>));
var floatOverrides=(Dictionary<string,object>)read(waterOverrides["floats"].ToString(),typeof(Dictionary<string,object>));
foreach(var item in floatOverrides){if(!waterMaterial.HasProperty(item.Key))throw new Exception("물 재질 속성 없음: "+item.Key);waterMaterial.SetFloat(item.Key,Convert.ToSingle(item.Value));}
var colorOverrides=(Dictionary<string,object>)read(waterOverrides["colors"].ToString(),typeof(Dictionary<string,object>));
foreach(var item in colorOverrides){var rgba=(float[])read(item.Value.ToString(),typeof(float[]));if(!waterMaterial.HasProperty(item.Key)||rgba.Length!=4)throw new Exception("물 색 속성 오류: "+item.Key);waterMaterial.SetColor(item.Key,new Color(rgba[0],rgba[1],rgba[2],rgba[3]));}
UnityEditor.EditorUtility.SetDirty(waterMaterial);UnityEditor.AssetDatabase.SaveAssetIfDirty(waterMaterial);
var surfaceSources=(Dictionary<string,object>)read(plan["surfaceSourceLayers"].ToString(),typeof(Dictionary<string,object>));
int sandIndex=Array.FindIndex(sourceData.terrainLayers,l=>UnityEditor.AssetDatabase.GetAssetPath(l)==surfaceSources["sand"].ToString());
int earthIndex=Array.FindIndex(sourceData.terrainLayers,l=>UnityEditor.AssetDatabase.GetAssetPath(l)==surfaceSources["earth"].ToString());
int rockIndex=Array.FindIndex(sourceData.terrainLayers,l=>UnityEditor.AssetDatabase.GetAssetPath(l)==surfaceSources["rock"].ToString());
if(sandIndex<0||earthIndex<0||rockIndex<0)throw new Exception("기준선 표면 레이어 매핑 실패");
var alpha=copy.GetAlphamaps(0,0,copy.alphamapWidth,copy.alphamapHeight);
float transition=Convert.ToSingle(plan["shoreBlendMeters"]);int paintedCells=0;
var alphaPositions=new List<object>();
for(int z=0;z<copy.alphamapHeight;z++)for(int x=0;x<copy.alphamapWidth;x++){
    var p=new Vector3(sourceTerrain.transform.position.x+x*copy.size.x/copy.alphamapWidth,0f,sourceTerrain.transform.position.z+z*copy.size.z/copy.alphamapHeight);
    if(!isInsideCore(p)||isProtected(p)||!isInsideWater(p))continue;
    float groundHeight=sourceData.GetInterpolatedHeight((p.x-sourceTerrain.transform.position.x)/copy.size.x,(p.z-sourceTerrain.transform.position.z)/copy.size.z)+sourceTerrain.transform.position.y;
    float depth=waterY-groundHeight;if(depth<=0f)continue;
    float edge=float.MaxValue;
    for(int i=0;i<polygon.Length;i++){
        var a=new Vector2(polygon[i][0],polygon[i][1]);var b=new Vector2(polygon[(i+1)%polygon.Length][0],polygon[(i+1)%polygon.Length][1]);var ab=b-a;
        float t=ab.sqrMagnitude>.00001f?Mathf.Clamp01(Vector2.Dot(new Vector2(p.x,p.z)-a,ab)/ab.sqrMagnitude):0f;
        edge=Mathf.Min(edge,Vector2.Distance(new Vector2(p.x,p.z),a+ab*t));
    }
    float blend=Mathf.SmoothStep(0f,1f,Mathf.Clamp01(edge/transition));
    if(blend<=0f)continue;
    float sand=Mathf.Lerp(.45f,.78f,Mathf.Clamp01(depth/1.5f));float rock=.12f;float earth=1f-sand-rock;
    for(int layer=0;layer<alpha.GetLength(2);layer++){
        float target=layer==sandIndex?sand:layer==rockIndex?rock:layer==earthIndex?earth:0f;
        alpha[z,x,layer]=Mathf.Lerp(alpha[z,x,layer],target,blend);
    }
    paintedCells++;alphaPositions.Add(new{x=x,z=z,worldX=p.x,worldZ=p.z,depth=depth,blend=blend,sandWeight=sand,earthWeight=earth,rockWeight=rock});
}
if(paintedCells<Convert.ToInt32(plan["minimumEditedCells"]))throw new Exception("대표 구간 수중 표본이 너무 적습니다. live core를 재검토해야 합니다.");
copy.SetAlphamaps(0,0,alpha);terrain.Flush();
var additions=new GameObject("Theme_LakeShrine_Environment");additions.transform.SetParent(environment.transform,false);
var placements=(Dictionary<string,object>[])read(plan["placements"].ToString(),typeof(Dictionary<string,object>[]));
var assetRows=new List<object>();
foreach(var placement in placements){
    string assetPath=placement["prefab"].ToString();var asset=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
    if(asset==null||asset.GetComponentsInChildren<MonoBehaviour>(true).Length>0)throw new Exception("기존 장식 프리팹 확인 필요: "+assetPath);
    var xz=(float[])read(placement["positionXZ"].ToString(),typeof(float[]));var p=new Vector3(xz[0],0f,xz[1]);
    if(!isInsideCore(p)||isProtected(p))throw new Exception("core 밖 또는 보호 영역 배치: "+assetPath);
    p.y=copy.GetInterpolatedHeight((p.x-sourceTerrain.transform.position.x)/copy.size.x,(p.z-sourceTerrain.transform.position.z)/copy.size.z)+sourceTerrain.transform.position.y+Convert.ToSingle(placement["groundOffset"]);
    if(placement.ContainsKey("waterAnchored")&&Convert.ToBoolean(placement["waterAnchored"]))p.y=waterY+Convert.ToSingle(placement["groundOffset"]);
    var instance=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(asset,review);instance.name=placement["name"].ToString();instance.transform.SetParent(additions.transform,false);
    instance.transform.SetPositionAndRotation(p,Quaternion.Euler(0f,Convert.ToSingle(placement["yaw"]),0f));
    var scale=(float[])read(placement["scale"].ToString(),typeof(float[]));instance.transform.localScale=new Vector3(scale[0],scale[1],scale[2]);
    assetRows.Add(new{path=assetPath,guid=UnityEditor.AssetDatabase.AssetPathToGUID(assetPath),instance=instance.name,position=new float[]{p.x,p.y,p.z},renderers=instance.GetComponentsInChildren<Renderer>(true).Length,lods=instance.GetComponentsInChildren<LODGroup>(true).Length,colliders=instance.GetComponentsInChildren<Collider>(true).Length,renderStatus="미검증"});
}
var captures=(Dictionary<string,object>[])read(plan["capturePoses"].ToString(),typeof(Dictionary<string,object>[]));
if(captures.Length>0){
    var first=(float[])read(captures[0]["position"].ToString(),typeof(float[]));var target=(float[])read(captures[0]["target"].ToString(),typeof(float[]));
    camera.transform.position=new Vector3(first[0],first[1],first[2]);camera.transform.rotation=Quaternion.LookRotation(new Vector3(target[0],target[1],target[2])-camera.transform.position);
}
camera.fieldOfView=sourceCamera.fieldOfView;
var afterAlpha=copy.GetAlphamaps(0,0,copy.alphamapWidth,copy.alphamapHeight);var originalAlpha=sourceData.GetAlphamaps(0,0,sourceData.alphamapWidth,sourceData.alphamapHeight);int outsideChanges=0;
for(int z=0;z<copy.alphamapHeight;z++)for(int x=0;x<copy.alphamapWidth;x++){
    var p=new Vector3(sourceTerrain.transform.position.x+x*copy.size.x/copy.alphamapWidth,0f,sourceTerrain.transform.position.z+z*copy.size.z/copy.alphamapHeight);
    if(isInsideCore(p))continue;
    for(int l=0;l<afterAlpha.GetLength(2);l++)if(afterAlpha[z,x,l]!=(l<originalAlpha.GetLength(2)?originalAlpha[z,x,l]:0f))outsideChanges++;
}
if(outsideChanges!=0)throw new Exception("core 밖 alpha 변경: "+outsideChanges);
if(contentHash(copy.GetHeights(0,0,copy.heightmapResolution,copy.heightmapResolution))!=sourceHeightHash)throw new Exception("첫 대표 구간에서 높이 변경은 허용하지 않았습니다.");
for(int i=0;i<copy.detailPrototypes.Length;i++){
    if(contentHash(copy.GetDetailLayer(0,0,copy.detailWidth,copy.detailHeight,i))!=sourceDetailHashes[i])throw new Exception("사본 기존 Detail 맵 변경");
    if(contentHash(sourceData.GetDetailLayer(0,0,sourceData.detailWidth,sourceData.detailHeight,i))!=sourceDetailHashes[i])throw new Exception("원본 기존 Detail 맵 변경");
}
if(hash(sourcePath)!=sourceHash||hash(sourceScene.path)!=sourceSceneHash||sourceScene.isDirty!=sourceDirty||contentHash(sourceData.GetAlphamaps(0,0,sourceData.alphamapWidth,sourceData.alphamapHeight))!=sourceAlphaHash||contentHash(sourceData.GetHeights(0,0,sourceData.heightmapResolution,sourceData.heightmapResolution))!=sourceHeightHash)throw new Exception("원본 Scene/Terrain 보존 검사 실패");
UnityEditor.EditorUtility.SetDirty(copy);UnityEditor.AssetDatabase.SaveAssetIfDirty(copy);
string prefabPath="Assets/03.Prefabs/Environment/WorldThemes/LakeShrine/Theme_LakeShrine_Environment.prefab";
System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(prefabPath));
if(System.IO.File.Exists(prefabPath))throw new Exception("기존 환경 프리팹을 덮어쓰지 않습니다.");
UnityEditor.PrefabUtility.SaveAsPrefabAsset(additions,prefabPath);
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(review,scenePath))throw new Exception("LakeShrine 씬 저장 실패");
var detailMap=sourceData.detailPrototypes.Select((p,i)=>new{index=i,path=UnityEditor.AssetDatabase.GetAssetPath(p.prototype),guid=UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(p.prototype)),sourceContentHash=sourceDetailHashes[i]}).ToArray();
var report=new{themeId="LakeShrine",version=plan["version"].ToString(),status="독립 대표 구간 제작 — 사용자 미술 인수 전",sourceScene=sourceScene.path,sourceTerrainGuid=UnityEditor.AssetDatabase.AssetPathToGUID(sourcePath),sourceTerrainHash=sourceHash,baselineSceneHash=sourceSceneHash,coordinateSpace="world",rootTransform="identity",scene=scenePath,terrain=dataFolder+"/Terrain.asset",coreMask=polygon,seamProposalMask=new object[0],referenceBounds=boundsInput,terrainSampleRect=new{heightResolution=copy.heightmapResolution,alphaResolution=copy.alphamapWidth,origin=new float[]{sourceTerrain.transform.position.x,sourceTerrain.transform.position.y,sourceTerrain.transform.position.z},size=new float[]{copy.size.x,copy.size.y,copy.size.z}},layerMap=layerMap,detailPrototypeMap=detailMap,treePrototypeMap=new object[0],scatterMode=copy.detailScatterMode.ToString(),prefabPathsAndGuids=new[]{new{path=prefabPath,guid=UnityEditor.AssetDatabase.AssetPathToGUID(prefabPath)}},waterMaterialPaths=new[]{waterMaterialPath},protectedEntities=protectedEntities.Select(e=>new{id=e.Guid,position=new float[]{e.transform.position.x,e.transform.position.y,e.transform.position.z},ground=sourceTerrain.SampleHeight(e.transform.position)+sourceTerrain.transform.position.y}).ToArray(),replaceObjects=new object[0],deleteObjects=new object[0],unchangedOutsideCoreCheck=new{alphaChanged=outsideChanges,heightHashIdentical=true,detailHashesIdentical=true,sourceSceneHashUnchanged=true,sourceTerrainHashUnchanged=true,sourceMemoryAlphaUnchanged=true,internalAlphaTextureSharing=false},paintedCells=paintedCells,validation=new[]{new{id="IN-01",result="통과",evidence="height/detail 전체 hash 동일, alpha core 밖 전체 채널 변경0, 원본 Scene/Terrain hash 동일"},new{id="TH-01",result="사용자 테스트 대기",evidence="6구도 렌더 후 미술 인수 필요"},new{id="IN-02",result="미수행",evidence="독립 대표 씬은 NavMesh/진행 콘텐츠를 복제하지 않음; Host 검토 사본 통합 후 양방향 경로 확인"},new{id="PL-01",result="사용자 테스트 대기",evidence="실제 TPS 전투/회피/락온"},new{id="PF-01",result="미수행",evidence="목표 PC/예산 미확정"}},assets=assetRows,generatedAssets=new object[0],capturePoses=captures,knownIssues=new[]{"높이·물수위·섬·연결길은 보존, 수중 바닥 표면 대표 시험", "ReferenceOnly 기존 환경은 메시/LOD 스냅샷이며 진행·발소리·충돌 기능을 복제하지 않음", "본편 통합·NavMesh·Play Mode·TPS·성능·사용자 인수 미수행", "수면 재질은 시험 제안이며 최종 환경 프리팹에 수면을 중복 포함하지 않음"},nextOwner="A00/A12"};
var serialize=jsonType.GetMethod("SerializeObject",new Type[]{typeof(object)});
System.IO.File.WriteAllText(output+"/handoff.json",(string)serialize.Invoke(null,new object[]{report}));
System.IO.File.WriteAllText(output+"/painted-cells.json",(string)serialize.Invoke(null,new object[]{alphaPositions}));
return "LakeShrine 독립 대표 구간 생성: alpha "+paintedCells+"셀, core 밖 변경 "+outsideChanges+", 원본 보존. 실제 화면/인수 대기.";
