// A00의 Editor lease에서 한 번만 실행하며 열린 본편의 미저장 변경을 보존한다.
string hostPath="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity";
string scenePath="Assets/01.Scenes/ArtReview/WorldThemes/Theme_VillageFields.unity";
string dataFolder="Assets/10.Datas/World/ThemeStudies/VillageFields";
string prefabPath="Assets/03.Prefabs/Environment/WorldThemes/VillageFields/Theme_VillageFields_Environment.prefab";
string output="output/WorldThemeProduction/VillageFields";
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorUtility.scriptCompilationFailed)throw new Exception("컴파일이 정상인 Edit Mode가 필요합니다.");
var host=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(hostPath);
if(!host.IsValid()||!host.isLoaded)throw new Exception("현재 열린 본편을 유지한 상태에서 실행하세요.");
if(System.IO.File.Exists(scenePath)||System.IO.File.Exists(dataFolder+"/Terrain.asset"))throw new Exception("기존 A03 제작물을 덮어쓰지 않았습니다.");
var jsonType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var deserialize=jsonType.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});
Func<string,Type,object> read=(s,t)=>deserialize.Invoke(null,new object[]{s,t});
Func<object,string> json=o=>(string)jsonType.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{o});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/WorldThemes/A03/village-study.json"),typeof(Dictionary<string,object>));
if(!Convert.ToBoolean(plan["maskAuthorized"]))throw new Exception("A00 core mask 확정 전에는 제작하지 않습니다.");
Func<string,string> hash=p=>{using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(System.IO.File.ReadAllBytes(p))).Replace("-","");};
if(hash(hostPath)!=plan["sourceSceneHash"].ToString())throw new Exception("본편 디스크 기준선이 달라졌습니다. A00 재검토가 필요합니다.");
var hostRoots=host.GetRootGameObjects();
var source=hostRoots.SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).Single(t=>t.terrainData!=null);
var sourceData=source.terrainData;
string sourcePath=AssetDatabase.GetAssetPath(sourceData);
string terrainHash=hash(sourcePath),sceneHash=hash(hostPath);
if(terrainHash!=plan["sourceTerrainHash"].ToString())throw new Exception("원본 Terrain 기준선이 달라졌습니다.");
bool hostDirty=host.isDirty;
var previousActive=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
UnityEngine.SceneManagement.SceneManager.SetActiveScene(host);
var sourceCamera=hostRoots.SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).First(c=>c.CompareTag("MainCamera"));
var sourceSun=hostRoots.SelectMany(g=>g.GetComponentsInChildren<Light>(true)).First(l=>l.type==LightType.Directional&&l.isActiveAndEnabled);
bool fog=RenderSettings.fog;FogMode fogMode=RenderSettings.fogMode;
float fogStart=RenderSettings.fogStartDistance,fogEnd=RenderSettings.fogEndDistance,fogDensity=RenderSettings.fogDensity,ambientIntensity=RenderSettings.ambientIntensity,reflectionIntensity=RenderSettings.reflectionIntensity;
Color fogColor=RenderSettings.fogColor,ambientSky=RenderSettings.ambientSkyColor,ambientEquator=RenderSettings.ambientEquatorColor,ambientGround=RenderSettings.ambientGroundColor;
var ambientMode=RenderSettings.ambientMode;var skybox=RenderSettings.skybox;var reflectionMode=RenderSettings.defaultReflectionMode;
var polygons=(float[][])read(plan["corePolygon"].ToString(),typeof(float[][]));
var referenceBounds=(float[])read(plan["referenceBounds"].ToString(),typeof(float[]));
float minX=polygons.Min(p=>p[0]),maxX=polygons.Max(p=>p[0]),minZ=polygons.Min(p=>p[1]),maxZ=polygons.Max(p=>p[1]);
Func<float,float,bool> inCore=(x,z)=>x>=minX&&x<=maxX&&z>=minZ&&z<=maxZ;
var ids=hostRoots.SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).ToArray();
var protectedPositions=ids.Select(i=>i.transform.position).ToArray();
var identityBefore=ids.Select(i=>i.Guid+"|"+i.transform.position.ToString("R")+"|"+(source.SampleHeight(i.transform.position)+source.transform.position.y).ToString("R")).OrderBy(s=>s).ToArray();
float protectRadius=Convert.ToSingle(plan["protectedContentRadius"]),roadThreshold=Convert.ToSingle(plan["roadAlphaThreshold"]),slopeLimit=Convert.ToSingle(plan["maximumPlantSlope"]);
float corridorWidth=Convert.ToSingle(plan["preserveCorridorHalfWidth"]);
var corridor=(float[][])read(plan["preserveCorridor"].ToString(),typeof(float[][]));
var environmentSource=hostRoots.Single(g=>g.name=="AuthoredEnvironment");
var preferenceRoot=environmentSource.transform.Find("NavigationPreferences");
var preferenceVolumes=preferenceRoot==null?new BoxCollider[0]:preferenceRoot.GetComponentsInChildren<BoxCollider>(true);
Physics.SyncTransforms();
var prefabs=(Dictionary<string,object>)read(plan["candidatePrefabs"].ToString(),typeof(Dictionary<string,object>));
foreach(var pair in prefabs)if(AssetDatabase.LoadAssetAtPath<GameObject>(pair.Value.ToString())==null)throw new Exception("후보 누락: "+pair.Value);
Func<float,float,bool> canPlant=(x,z)=>{
 if(!inCore(x,z))return false;
 if(Mathf.Abs(x-corridor[0][0])<corridorWidth&&z>=corridor[0][1]&&z<=corridor[1][1])return false;
 foreach(var p in protectedPositions)if((new Vector2(x-p.x,z-p.z)).sqrMagnitude<protectRadius*protectRadius)return false;
 var point=new Vector3(x,source.SampleHeight(new Vector3(x,0,z))+source.transform.position.y,z);
 foreach(var volume in preferenceVolumes)if(volume.enabled&&volume.bounds.Contains(new Vector3(x,volume.bounds.center.y,z)))return false;
 int ax=Mathf.Clamp(Mathf.FloorToInt((x-source.transform.position.x)/sourceData.size.x*sourceData.alphamapWidth),0,sourceData.alphamapWidth-1);
 int az=Mathf.Clamp(Mathf.FloorToInt((z-source.transform.position.z)/sourceData.size.z*sourceData.alphamapHeight),0,sourceData.alphamapHeight-1);
 var alpha=sourceData.GetAlphamaps(ax,az,1,1);
 if(alpha[0,0,1]+alpha[0,0,5]>roadThreshold)return false;
 float slope=sourceData.GetSteepness((x-source.transform.position.x)/sourceData.size.x,(z-source.transform.position.z)/sourceData.size.z);
 if(slope>slopeLimit)return false;
 foreach(var collider in Physics.OverlapSphere(point+Vector3.up*.7f,.5f,~0,QueryTriggerInteraction.Ignore)){
  if(collider is TerrainCollider||collider.gameObject.scene!=host)continue;
  if(collider.bounds.max.y>point.y+.25f)return false;
 }
 return true;
};
System.IO.Directory.CreateDirectory(output+"/captures");System.IO.Directory.CreateDirectory(dataFolder+"/Materials");System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(scenePath));System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(prefabPath));
var copy=UnityEngine.Object.Instantiate(sourceData);copy.name="VillageFields_Terrain";
AssetDatabase.CreateAsset(copy,dataFolder+"/Terrain.asset");
var sourceAlphaIds=sourceData.alphamapTextures.Select(t=>t.GetInstanceID()).ToArray();
if(copy.alphamapTextures.Any(t=>sourceAlphaIds.Contains(t.GetInstanceID())))throw new Exception("Terrain alpha 텍스처가 공유됩니다.");
var testBefore=sourceData.GetAlphamaps(0,0,1,1);var test=(float[,,])testBefore.Clone();
for(int l=0;l<test.GetLength(2);l++)test[0,0,l]=l==0?1f:0f;
copy.SetAlphamaps(0,0,test);
for(int l=0;l<test.GetLength(2);l++)if(sourceData.GetAlphamaps(0,0,1,1)[0,0,l]!=testBefore[0,0,l])throw new Exception("사본 시험 도색이 원본에 전파됐습니다.");
copy.SetAlphamaps(0,0,testBefore);
var preservedAlpha=sourceData.GetAlphamaps(0,0,sourceData.alphamapWidth,sourceData.alphamapHeight);
var layerMap=new List<object>();var layers=copy.terrainLayers;
for(int l=0;l<layers.Length;l++){
 string from=AssetDatabase.GetAssetPath(layers[l]),to=dataFolder+"/Layer_"+l+".terrainlayer";
 layers[l]=UnityEngine.Object.Instantiate(layers[l]);AssetDatabase.CreateAsset(layers[l],to);
 layerMap.Add(new{index=l,sourcePath=from,sourceGuid=AssetDatabase.AssetPathToGUID(from),path=to,guid=AssetDatabase.AssetPathToGUID(to),diffuseGuid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(layers[l].diffuseTexture)),normalGuid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(layers[l].normalMapTexture)),tileSize=new[]{layers[l].tileSize.x,layers[l].tileSize.y},tileOffset=new[]{layers[l].tileOffset.x,layers[l].tileOffset.y},metallic=layers[l].metallic,smoothness=layers[l].smoothness,normalScale=layers[l].normalScale});
}
// 레이어 참조 배열 교체가 alpha를 초기화하므로 가중치를 명시적으로 복원한다.
copy.terrainLayers=layers;copy.SetAlphamaps(0,0,preservedAlpha);
var cloneAlpha=copy.GetAlphamaps(0,0,copy.alphamapWidth,copy.alphamapHeight);int alphaDifferences=0;
for(int z=0;z<cloneAlpha.GetLength(0);z++)for(int x=0;x<cloneAlpha.GetLength(1);x++)for(int l=0;l<cloneAlpha.GetLength(2);l++)if(cloneAlpha[z,x,l]!=preservedAlpha[z,x,l])alphaDifferences++;
if(alphaDifferences!=0)throw new Exception("레이어 사본 설정 후 alpha 복원 실패: "+alphaDifferences);
if(copy.detailScatterMode!=sourceData.detailScatterMode||copy.detailPrototypes.Length!=6)throw new Exception("기존 풀 산포/6종 계약이 다릅니다.");
var review=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);
UnityEngine.SceneManagement.SceneManager.SetActiveScene(review);
var environment=new GameObject("ThemeEnvironment");var preview=new GameObject("PreviewRig");var reference=new GameObject("ReferenceOnly");
var terrainObject=Terrain.CreateTerrainGameObject(copy);terrainObject.name="ThemeTerrain_VillageFields";
terrainObject.transform.position=source.transform.position;
var terrain=terrainObject.GetComponent<Terrain>();EditorUtility.CopySerialized(source,terrain);terrain.terrainData=copy;terrainObject.GetComponent<TerrainCollider>().terrainData=copy;
if(source.materialTemplate!=null){var m=UnityEngine.Object.Instantiate(source.materialTemplate);AssetDatabase.CreateAsset(m,dataFolder+"/Terrain.mat");terrain.materialTemplate=m;}
RenderSettings.fog=fog;RenderSettings.fogMode=fogMode;RenderSettings.fogStartDistance=fogStart;RenderSettings.fogEndDistance=fogEnd;RenderSettings.fogDensity=fogDensity;RenderSettings.fogColor=fogColor;
RenderSettings.ambientMode=ambientMode;RenderSettings.ambientSkyColor=ambientSky;RenderSettings.ambientEquatorColor=ambientEquator;RenderSettings.ambientGroundColor=ambientGround;RenderSettings.ambientIntensity=ambientIntensity;RenderSettings.defaultReflectionMode=reflectionMode;RenderSettings.reflectionIntensity=reflectionIntensity;
if(skybox!=null){var m=UnityEngine.Object.Instantiate(skybox);if(m.HasProperty("_AtmosphereThickness"))m.SetFloat("_AtmosphereThickness",1f);AssetDatabase.CreateAsset(m,dataFolder+"/PreviewSky.mat");RenderSettings.skybox=m;}
var lightObject=new GameObject("PreviewSun");lightObject.transform.SetParent(preview.transform,false);lightObject.transform.rotation=sourceSun.transform.rotation;var light=lightObject.AddComponent<Light>();EditorUtility.CopySerialized(sourceSun,light);RenderSettings.sun=light;
var cameraObject=new GameObject("PreviewCamera");cameraObject.transform.SetParent(preview.transform,false);var camera=cameraObject.AddComponent<Camera>();camera.CopyFrom(sourceCamera);camera.tag="MainCamera";camera.enabled=false;camera.targetTexture=null;
var extra=cameraObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();var sourceExtra=sourceCamera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();if(sourceExtra!=null)EditorUtility.CopySerialized(sourceExtra,extra);extra.renderPostProcessing=true;extra.cameraStack.Clear();extra.volumeLayerMask=1<<31;extra.volumeTrigger=camera.transform;
if(!AssetDatabase.CopyAsset("Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/LakeAtmosphere.asset",dataFolder+"/PreviewVolume.asset"))throw new Exception("Volume 사본 생성 실패");
var volumeObject=new GameObject("PreviewVolume");volumeObject.transform.SetParent(preview.transform,false);volumeObject.layer=31;var previewVolume=volumeObject.AddComponent<UnityEngine.Rendering.Volume>();previewVolume.isGlobal=true;previewVolume.sharedProfile=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(dataFolder+"/PreviewVolume.asset");
var context=new GameObject("ExistingEnvironmentContext_ExcludeFromIntegration");context.transform.SetParent(reference.transform,false);
var materialCopies=new Dictionary<Material,Material>();var contextRows=new List<object>();var assetRows=new List<object>();
// 스크립트와 콘텐츠를 순간적으로도 복제하지 않도록 native 메시/LOD만 옮긴다.
Func<GameObject,Transform,bool,GameObject> copyNative=(original,parent,ownMaterials)=>{
 var root=new GameObject(original.name);root.transform.SetParent(parent,false);
 var map=new Dictionary<Transform,Transform>();map[original.transform]=root.transform;
 foreach(var from in original.GetComponentsInChildren<Transform>(true)){
  Transform to;if(from==original.transform)to=root.transform;else{var go=new GameObject(from.name);to=go.transform;to.SetParent(map[from.parent],false);map[from]=to;}
  to.localPosition=from.localPosition;to.localRotation=from.localRotation;to.localScale=from.localScale;to.gameObject.layer=from.gameObject.layer;to.gameObject.SetActive(from.gameObject.activeSelf);
  var mesh=from.GetComponent<MeshFilter>();if(mesh!=null)to.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;
  var renderer=from.GetComponent<MeshRenderer>();if(renderer==null)continue;
  var target=to.gameObject.AddComponent<MeshRenderer>();EditorUtility.CopySerialized(renderer,target);target.probeAnchor=null;target.lightProbeProxyVolumeOverride=null;
  if(ownMaterials){var materials=target.sharedMaterials;for(int i=0;i<materials.Length;i++){var sourceMaterial=materials[i];if(sourceMaterial==null)continue;Material owned;if(!materialCopies.TryGetValue(sourceMaterial,out owned)){owned=UnityEngine.Object.Instantiate(sourceMaterial);string materialPath=dataFolder+"/Materials/Material_"+materialCopies.Count+".mat";AssetDatabase.CreateAsset(owned,materialPath);materialCopies[sourceMaterial]=owned;}materials[i]=owned;}target.sharedMaterials=materials;}
 }
 foreach(var lod in original.GetComponentsInChildren<LODGroup>(true)){
  var target=map[lod.transform].gameObject.AddComponent<LODGroup>();var lods=lod.GetLODs();for(int i=0;i<lods.Length;i++)lods[i].renderers=lods[i].renderers.Where(r=>r!=null&&map.ContainsKey(r.transform)).Select(r=>map[r.transform].GetComponent<Renderer>()).Where(r=>r!=null).ToArray();target.SetLODs(lods);target.fadeMode=lod.fadeMode;target.animateCrossFading=lod.animateCrossFading;target.localReferencePoint=lod.localReferencePoint;target.size=lod.size;
 }
 return root;
};
var sourceObjects=new HashSet<GameObject>();
foreach(var renderer in environmentSource.GetComponentsInChildren<MeshRenderer>(true)){
 var b=renderer.bounds;if(b.max.x<referenceBounds[0]||b.min.x>referenceBounds[1]||b.max.z<referenceBounds[2]||b.min.z>referenceBounds[3])continue;
 if(!renderer.gameObject.activeInHierarchy)continue;
 var candidate=PrefabUtility.GetNearestPrefabInstanceRoot(renderer.gameObject);
 if(candidate!=null&&candidate.transform.IsChildOf(environmentSource.transform))sourceObjects.Add(candidate);else sourceObjects.Add(renderer.gameObject);
}
var contextSources=sourceObjects.Where(g=>!sourceObjects.Any(parent=>parent!=g&&g.transform.IsChildOf(parent.transform))).ToArray();
foreach(var original in contextSources){
 var native=copyNative(original,context.transform,false);native.transform.position=original.transform.position;native.transform.rotation=original.transform.rotation;native.transform.localScale=original.transform.lossyScale;
 contextRows.Add(new{name=original.name,position=new[]{original.transform.position.x,original.transform.position.y,original.transform.position.z},renderers=native.GetComponentsInChildren<MeshRenderer>(true).Length});
}
var comparison=new GameObject("CandidateComparison_ExcludeFromIntegration");comparison.transform.SetParent(preview.transform,false);
int candidateIndex=0;
foreach(var pair in prefabs){
 string candidatePath=pair.Value.ToString();var asset=AssetDatabase.LoadAssetAtPath<GameObject>(candidatePath);var native=copyNative(asset,comparison.transform,true);native.name="Candidate_"+pair.Key;
 var p=new Vector3(1165f+candidateIndex*10f,0,340f);p.y=source.SampleHeight(p)+source.transform.position.y;native.transform.position=p;
 var renderers=native.GetComponentsInChildren<MeshRenderer>(true);var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
 assetRows.Add(new{key=pair.Key,path=candidatePath,guid=AssetDatabase.AssetPathToGUID(candidatePath),sourceHash=hash(candidatePath),boundsSize=new[]{b.size.x,b.size.y,b.size.z},renderers=renderers.Length,lodGroups=native.GetComponentsInChildren<LODGroup>(true).Length,sourceColliderCount=asset.GetComponentsInChildren<Collider>(true).Length,studyColliderCount=0,sourceScripts=asset.GetComponentsInChildren<MonoBehaviour>(true).Select(m=>m==null?"Missing":m.GetType().FullName).ToArray(),shaders=renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Select(m=>m.shader.name+" supported="+m.shader.isSupported).Distinct().ToArray(),renderStatus="실제 비교 촬영 대기",selectionStatus="미채택"});candidateIndex++;
}
comparison.SetActive(false);
System.IO.File.WriteAllText(output+"/candidate-inventory.json",json(assetRows));
foreach(var identity in ids){var p=identity.transform.position;if(p.x<referenceBounds[0]||p.x>referenceBounds[1]||p.z<referenceBounds[2]||p.z>referenceBounds[3])continue;var marker=new GameObject("Protected_"+identity.Guid);marker.transform.SetParent(reference.transform,false);marker.transform.position=p;}
var captures=(Dictionary<string,object>[])read(plan["capturePoses"].ToString(),typeof(Dictionary<string,object>[]));
var poses=new List<object>();
foreach(var pose in captures){var xz=(float[])read(pose["xz"].ToString(),typeof(float[]));var target=(float[])read(pose["target"].ToString(),typeof(float[]));var p=new Vector3(xz[0],terrain.SampleHeight(new Vector3(xz[0],0,xz[1]))+terrain.transform.position.y+Convert.ToSingle(pose["height"]),xz[1]);var look=new Vector3(target[0],terrain.SampleHeight(new Vector3(target[0],0,target[1]))+terrain.transform.position.y+1f,target[1]);poses.Add(new{key=pose["key"],position=new[]{p.x,p.y,p.z},target=new[]{look.x,look.y,look.z},fov=camera.fieldOfView,fog=fog,fogStart=fogStart,fogEnd=fogEnd,postProcessing=true,time="고정 낮",weather="본편 기준선"});}
System.IO.File.WriteAllText(output+"/capture-poses.json",json(poses));
camera.transform.position=new Vector3(1132f,terrain.SampleHeight(new Vector3(1132f,0,345f))+terrain.transform.position.y+1.7f,345f);camera.transform.rotation=Quaternion.LookRotation(new Vector3(1044f,61f,408f)-camera.transform.position);
EditorUtility.SetDirty(copy);AssetDatabase.SaveAssetIfDirty(copy);
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(review,scenePath))throw new Exception("기준 장면 저장 실패");
System.IO.File.WriteAllText(output+"/baseline-ready.txt","전후 비교 기준 장면 준비. apply-village.csx 적용 전 동일 구도 사진을 촬영해야 합니다.");
var detailMap=copy.detailPrototypes.Select((d,i)=>new{index=i,path=AssetDatabase.GetAssetPath(d.prototype),guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(d.prototype)),minWidth=d.minWidth,maxWidth=d.maxWidth,minHeight=d.minHeight,maxHeight=d.maxHeight,noiseSpread=d.noiseSpread,renderMode=d.renderMode.ToString(),useInstancing=d.useInstancing,density=d.density}).ToArray();
System.IO.File.WriteAllText(output+"/detail-prototype-map.json",json(detailMap));
System.IO.File.WriteAllText(output+"/tree-prototype-map.json",json(copy.treePrototypes.Select((t,i)=>new{index=i,path=AssetDatabase.GetAssetPath(t.prefab),guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(t.prefab)),bendFactor=t.bendFactor}).ToArray()));
System.IO.File.WriteAllText(output+"/scaffold-report.json",json(new{version=plan["version"],sourceScene=hostPath,sourceTerrain=sourcePath,sourceSceneHash=sceneHash,sourceTerrainHash=terrainHash,hostDirtyBefore=hostDirty,hostDirtyAfter=host.isDirty,scene=scenePath,terrain=dataFolder+"/Terrain.asset",prefab=prefabPath,layerMap=layerMap,context=contextRows,assets=assetRows,scatterMode=copy.detailScatterMode.ToString(),detailCount=copy.detailPrototypes.Length,alphaCloneDifferences=alphaDifferences,coreMask=polygons,referenceBounds=referenceBounds,referenceBoundsOrder=new[]{"minX","maxX","minZ","maxZ"},generatedAssets=new object[0],knownIssues=new[]{"기준 장면만 준비. apply-village 전후 동일 구도 촬영 필요","ReferenceOnly는 렌더 컨텍스트이며 본편 통합 제외","사용자 미저장 본편은 저장/닫기 금지"}}));
if(hash(hostPath)!=sceneHash||hash(sourcePath)!=terrainHash||host.isDirty!=hostDirty)throw new Exception("원본 보존 검사 실패");
UnityEngine.SceneManagement.SceneManager.SetActiveScene(previousActive);
return "A03 baseline scaffold ready; host preserved; before captures then apply-village.csx";
