// A00 전용: 독립 호수 사본의 core와 바깥 15m 전이를 연결한다. 본편은 저장하지 않는다.
string scenePath="Assets/01.Scenes/ArtReview/WorldThemes/WorkingCopies/Theme_LakeShrine_SeamV3.unity";
string dataPath="Assets/10.Datas/World/ThemeStudies/LakeShrine/SeamV3Working/Terrain.asset";
string output="output/WorldThemeProduction/LakeShrine/SeamV3";
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorUtility.scriptCompilationFailed)throw new Exception("정상 Edit Mode 필요");
if(System.IO.File.Exists(output+"/applied.json"))throw new Exception("이미 적용된 seam을 중복 실행하지 않습니다.");
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
var host=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity");
if(!scene.isLoaded||!host.isLoaded||scene.isDirty)throw new Exception("저장된 독립 호수와 열린 본편 필요");
var terrain=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).Single();
var data=terrain.terrainData;var source=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/10.Datas/World/OpenWorld/Terrain.asset");
if(data==source||AssetDatabase.GetAssetPath(data)!=dataPath||EditorUtility.IsDirty(data)||EditorUtility.IsDirty(source))throw new Exception("깨끗한 전용 사본과 기준선 Terrain 필요. 미저장 변경은 보존합니다.");
Func<string,string> hash=p=>{using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(System.IO.File.ReadAllBytes(p))).Replace("-","");};
Func<Array,string> contentHash=a=>{var bytes=new byte[Buffer.ByteLength(a)];Buffer.BlockCopy(a,0,bytes,0,bytes.Length);using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","");};
var jsonType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=jsonType.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});
Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
Func<object,string> json=o=>(string)jsonType.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{o});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/WorldThemes/A02/lake-study.json"),typeof(Dictionary<string,object>));
var handoff=(Dictionary<string,object>)read(System.IO.File.ReadAllText("output/WorldThemeProduction/LakeShrine/handoff.json"),typeof(Dictionary<string,object>));
var artifactManifest=(Dictionary<string,object>)read(System.IO.File.ReadAllText("output/WorldThemeProduction/LakeShrine/artifact-hashes.json"),typeof(Dictionary<string,object>));
var artifacts=(Dictionary<string,object>[])read(artifactManifest["artifacts"].ToString(),typeof(Dictionary<string,object>[]));
var working=(Dictionary<string,object>)read(System.IO.File.ReadAllText("output/WorldThemeProduction/A00/WorkingCopies/LakeShrineSeamV3.json"),typeof(Dictionary<string,object>));
if(working["scene"].ToString()!=scenePath||working["terrain"].ToString()!=dataPath||hash(scenePath)!=working["sceneHash"].ToString()||hash(dataPath)!=working["terrainHash"].ToString()||AssetDatabase.AssetPathToGUID(dataPath)!=working["terrainGuid"].ToString())throw new Exception("검토 사본 기준선 불일치");
string previousScene=working["sourceScene"].ToString(),previousData=working["sourceTerrain"].ToString();
var observed=(Dictionary<string,object>)read(System.IO.File.ReadAllText(output+"/original-art-observed.json"),typeof(Dictionary<string,object>));
var observedArtifacts=(Dictionary<string,object>[])read(observed["artifacts"].ToString(),typeof(Dictionary<string,object>[]));
string previousSceneHash=observedArtifacts.Single(a=>a["path"].ToString()==previousScene)["current"].ToString();
foreach(string assetPath in new[]{previousScene,previousScene+".meta",previousData,previousData+".meta"}){
    var record=artifacts.Single(a=>a["path"].ToString()==assetPath);
    // 원래 씬의 외부 변경은 보존하며 제작 입력은 위에서 검증한 별도 snapshot이다.
    string expected=assetPath==previousScene?previousSceneHash:record["sha256"].ToString();
    if(hash(assetPath)!=expected)throw new Exception("원래 아트 관측 후 추가 변경: "+assetPath);
}
if(data.alphamapTextures.Any(t=>source.alphamapTextures.Any(s=>s.GetInstanceID()==t.GetInstanceID()))||data.heightmapTexture.GetInstanceID()==source.heightmapTexture.GetInstanceID())throw new Exception("내부 Terrain 텍스처 공유");
var polygon=(float[][])read(plan["corePolygon"].ToString(),typeof(float[][]));
var seamPlan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/WorldThemes/Integration/lake-seam.json"),typeof(Dictionary<string,object>));
var sourceBaseline=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/WorldThemes/Integration/source-baseline-v2.json"),typeof(Dictionary<string,object>));
float seamWidth=Convert.ToSingle(seamPlan["outerWidth"]),innerCollar=Convert.ToSingle(seamPlan["innerCollar"]),depthFade=Convert.ToSingle(seamPlan["shallowDepthFade"]);
if(seamWidth<=0||innerCollar<=0||depthFade<=0)throw new Exception("전이 폭/수심 감쇠는 양수여야 합니다.");
float minX=polygon.Min(p=>p[0])-seamWidth,maxX=polygon.Max(p=>p[0])+seamWidth,minZ=polygon.Min(p=>p[1])-seamWidth,maxZ=polygon.Max(p=>p[1])+seamWidth;
Func<Vector3,bool> inside=p=>{bool result=false;for(int i=0,j=polygon.Length-1;i<polygon.Length;j=i++){var a=polygon[i];var b=polygon[j];if((a[1]>p.z)!=(b[1]>p.z)&&p.x<(b[0]-a[0])*(p.z-a[1])/(b[1]-a[1])+a[0])result=!result;}return result;};
Func<Vector3,float> distance=p=>{float result=float.MaxValue;var point=new Vector2(p.x,p.z);for(int i=0;i<polygon.Length;i++){var a=new Vector2(polygon[i][0],polygon[i][1]);var b=new Vector2(polygon[(i+1)%polygon.Length][0],polygon[(i+1)%polygon.Length][1]);var delta=b-a;float f=delta.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(point-a,delta)/delta.sqrMagnitude):0;result=Mathf.Min(result,Vector2.Distance(point,a+delta*f));}return result;};
var all=host.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var ids=all.SelectMany(t=>t.GetComponents<UPlayGround.Components.SceneEntityId>()).ToArray();
var anchors=(float[][])read(plan["protectedAnchors"].ToString(),typeof(float[][]));
var navigation=host.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Unity.AI.Navigation.NavMeshModifierVolume>(true)).ToArray();
float routePadding=Convert.ToSingle(seamPlan["routePadding"]);
var routeBounds=new List<Bounds>();
foreach(var volume in navigation){
    var local=new Bounds(volume.center,volume.size);var world=new Bounds(volume.transform.TransformPoint(local.min),Vector3.zero);
    for(int i=0;i<8;i++)world.Encapsulate(volume.transform.TransformPoint(new Vector3((i&1)==0?local.min.x:local.max.x,(i&2)==0?local.min.y:local.max.y,(i&4)==0?local.min.z:local.max.z)));
    world.Expand(new Vector3(routePadding*2f,0,routePadding*2f));routeBounds.Add(world);
}
float protectedRadius=Convert.ToSingle(plan["protectedRadius"]);
Func<Vector3,bool> protectedPoint=p=>ids.Any(e=>new Vector2(p.x-e.transform.position.x,p.z-e.transform.position.z).sqrMagnitude<protectedRadius*protectedRadius)||anchors.Any(a=>new Vector2(p.x-a[0],p.z-a[2]).sqrMagnitude<protectedRadius*protectedRadius);
var water=all.Single(t=>t.name=="LakeWater");var mesh=water.GetComponent<MeshFilter>().sharedMesh;
var vertices=mesh.vertices.Select(v=>water.TransformPoint(v)).ToArray();var triangles=mesh.triangles;float waterY=water.position.y;
Func<Vector3,Vector3,Vector3,float> cross=(a,b,p)=>(b.x-a.x)*(p.z-a.z)-(b.z-a.z)*(p.x-a.x);
Func<Vector3,bool> waterPoint=p=>{for(int i=0;i<triangles.Length;i+=3){var a=vertices[triangles[i]];var b=vertices[triangles[i+1]];var c=vertices[triangles[i+2]];if(Mathf.Abs(cross(a,b,c))<.00001f)continue;float ab=cross(a,b,p),bc=cross(b,c,p),ca=cross(c,a,p);if((ab>=0&&bc>=0&&ca>=0)||(ab<=0&&bc<=0&&ca<=0))return true;}return false;};
var original=source.GetAlphamaps(0,0,source.alphamapWidth,source.alphamapHeight);
var before=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);var after=(float[,,])before.Clone();
var sourcePaths=source.terrainLayers.Select(l=>AssetDatabase.GetAssetPath(l)).ToArray();
var layerMap=(Dictionary<string,object>[])read(handoff["layerMap"].ToString(),typeof(Dictionary<string,object>[]));
var targetToSource=new int[data.terrainLayers.Length];int earthIndex=-1,rockIndex=-1,riverIndex=-1;
Func<TerrainLayer,TerrainLayer,bool> settingsEqual=(a,b)=>a.diffuseTexture==b.diffuseTexture&&a.normalMapTexture==b.normalMapTexture&&a.maskMapTexture==b.maskMapTexture&&a.tileSize.Equals(b.tileSize)&&a.tileOffset.Equals(b.tileOffset)&&a.metallic.Equals(b.metallic)&&a.smoothness.Equals(b.smoothness)&&a.normalScale.Equals(b.normalScale)&&a.specular.Equals(b.specular)&&a.diffuseRemapMin.Equals(b.diffuseRemapMin)&&a.diffuseRemapMax.Equals(b.diffuseRemapMax)&&a.maskMapRemapMin.Equals(b.maskMapRemapMin)&&a.maskMapRemapMax.Equals(b.maskMapRemapMax);
for(int i=0;i<data.terrainLayers.Length;i++){
    string targetPath=AssetDatabase.GetAssetPath(data.terrainLayers[i]),guid=AssetDatabase.AssetPathToGUID(targetPath);
    var record=layerMap.Single(r=>r["path"].ToString()==targetPath&&r["guid"].ToString()==guid);
    string sourcePath=record["sourcePath"].ToString();
    if(AssetDatabase.AssetPathToGUID(sourcePath)!=record["sourceGuid"].ToString())throw new Exception("source layer GUID 불일치");
    var sourceLayer=AssetDatabase.LoadAssetAtPath<TerrainLayer>(sourcePath);
    if(sourceLayer==null||!settingsEqual(sourceLayer,data.terrainLayers[i]))throw new Exception("레이어 설정 불일치");
    targetToSource[i]=Array.IndexOf(sourcePaths,sourcePath);
    if(sourcePath=="Assets/10.Datas/World/OpenWorld/ResourceOverhaul/Ground_5.terrainlayer")earthIndex=i;
    if(sourcePath=="Assets/10.Datas/World/OpenWorld/ResourceOverhaul/Ground_2.terrainlayer")rockIndex=i;
    if(sourcePath=="Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Scenes/Terrain/TerrainLayers/TerrainLayer_RiverBed.terrainlayer")riverIndex=i;
}
if(earthIndex<0||rockIndex<0||riverIndex<0)throw new Exception("정확 경로 레이어 매핑 실패");
bool hostDirty=host.isDirty;string sourceHash=hash(AssetDatabase.GetAssetPath(source)),hostHash=hash(host.path);
if(sourceHash!=sourceBaseline["sourceTerrainHash"].ToString()||hostHash!=sourceBaseline["sourceSceneHash"].ToString())throw new Exception("원본 기준선이 달라져 새 조사 필요");
string sourceAlphaHash=contentHash(original);
string heightHash=contentHash(data.GetHeights(0,0,data.heightmapResolution,data.heightmapResolution));
var detailHashes=Enumerable.Range(0,data.detailPrototypes.Length).Select(i=>contentHash(data.GetDetailLayer(0,0,data.detailWidth,data.detailHeight,i))).ToArray();
var scatterBefore=data.detailScatterMode;
Func<string> treeSignature=()=>json(new{instances=data.treeInstances.Select(t=>new{position=new[]{t.position.x,t.position.y,t.position.z},t.rotation,t.widthScale,t.heightScale,t.prototypeIndex,color=new[]{t.color.r,t.color.g,t.color.b,t.color.a},lightmap=new[]{t.lightmapColor.r,t.lightmapColor.g,t.lightmapColor.b,t.lightmapColor.a}}).ToArray(),prototypes=data.treePrototypes.Select(t=>new{path=AssetDatabase.GetAssetPath(t.prefab),t.bendFactor}).ToArray()});
string treesBefore=treeSignature(),studyHashBefore=hash(dataPath),alphaHashBefore=contentHash(before);
var allowed=new bool[data.alphamapHeight,data.alphamapWidth];var totalRegion=new bool[data.alphamapHeight,data.alphamapWidth];var rows=new List<object>();
for(int z=0;z<data.alphamapHeight;z++)for(int x=0;x<data.alphamapWidth;x++){
    var p=new Vector3(terrain.transform.position.x+x*data.size.x/data.alphamapWidth,0,terrain.transform.position.z+z*data.size.z/data.alphamapHeight);
    if(p.x<minX||p.x>maxX||p.z<minZ||p.z>maxZ)continue;
    bool core=inside(p);float boundary=distance(p);if(!core&&boundary>=seamWidth)continue;
    totalRegion[z,x]=true;if(core&&boundary>innerCollar)continue;
    if(protectedPoint(p)||routeBounds.Any(b=>p.x>=b.min.x&&p.x<=b.max.x&&p.z>=b.min.z&&p.z<=b.max.z)||!waterPoint(p))continue;
    float depth=waterY-source.GetInterpolatedHeight((p.x-terrain.transform.position.x)/data.size.x,(p.z-terrain.transform.position.z)/data.size.z)-terrain.transform.position.y;
    if(depth<=0)continue;
    float signedDistance=core?boundary:-boundary;
    float blend=Mathf.SmoothStep(0f,1f,Mathf.Clamp01((signedDistance+seamWidth)/(seamWidth+innerCollar)))*Mathf.SmoothStep(0f,1f,Mathf.Clamp01(depth/depthFade));
    float river=Mathf.Lerp(.45f,.78f,Mathf.Clamp01(depth/1.5f));float rock=.12f;float earth=1f-river-rock;
    for(int l=0;l<after.GetLength(2);l++){float target=l==riverIndex?river:l==rockIndex?rock:l==earthIndex?earth:0;after[z,x,l]=Mathf.Lerp(targetToSource[l]>=0?original[z,x,targetToSource[l]]:0,target,blend);}
    allowed[z,x]=true;rows.Add(new{x,z,worldX=p.x,worldZ=p.z,depth,core,boundaryDistance=boundary,blend});
}
System.IO.Directory.CreateDirectory(output);
if(System.IO.File.Exists(output+"/Terrain.before.bin"))throw new Exception("기존 seam 백업 보존");
System.IO.File.Copy(dataPath,output+"/Terrain.before.bin");System.IO.File.Copy(scenePath,output+"/Theme_LakeShrine.before.unity");
System.IO.File.Copy(dataPath+".meta",output+"/Terrain.before.meta");
System.IO.File.Copy("output/WorldThemeProduction/LakeShrine/handoff.json",output+"/handoff.previous-v2.json");
System.IO.File.Copy("output/WorldThemeProduction/LakeShrine/artifact-hashes.json",output+"/artifact-hashes.previous-v2.json");
try{
    data.SetAlphamaps(0,0,after);terrain.Flush();var actual=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);
    int changed=0,outside=0,hostOutside=0;float maxWeightError=0;
    for(int z=0;z<data.alphamapHeight;z++)for(int x=0;x<data.alphamapWidth;x++){bool cellChanged=false;float sum=0;for(int l=0;l<actual.GetLength(2);l++){sum+=actual[z,x,l];if(actual[z,x,l]!=before[z,x,l]){cellChanged=true;if(!allowed[z,x])outside++;}if(!totalRegion[z,x]&&actual[z,x,l]!=(targetToSource[l]>=0?original[z,x,targetToSource[l]]:0))hostOutside++;}if(cellChanged)changed++;maxWeightError=Mathf.Max(maxWeightError,Mathf.Abs(sum-1f));}
    if(outside!=0||hostOutside!=0||maxWeightError>.00001f)throw new Exception("허용 영역 밖 변경 또는 가중치 오류");
    if(changed==0)throw new Exception("실제 변경 0셀. seam 제작 성공으로 처리하지 않습니다.");
    if(contentHash(data.GetHeights(0,0,data.heightmapResolution,data.heightmapResolution))!=heightHash)throw new Exception("높이 변경");
    for(int i=0;i<detailHashes.Length;i++)if(contentHash(data.GetDetailLayer(0,0,data.detailWidth,data.detailHeight,i))!=detailHashes[i])throw new Exception("Detail 변경");
    if(data.detailScatterMode!=scatterBefore||treeSignature()!=treesBefore)throw new Exception("산포 모드 또는 tree 변경");
    if(hash(AssetDatabase.GetAssetPath(source))!=sourceHash||hash(host.path)!=hostHash||host.isDirty!=hostDirty||contentHash(source.GetAlphamaps(0,0,source.alphamapWidth,source.alphamapHeight))!=sourceAlphaHash)throw new Exception("본편 파일/메모리 보존 실패");
    if(hash(previousScene)!=previousSceneHash)throw new Exception("원래 아트 씬의 동시 변경. 검토 사본만 롤백합니다.");
    EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);
    System.IO.File.WriteAllText(output+"/samples.json",json(rows));
    System.IO.File.WriteAllText(output+"/applied.json",json(new{version="LakeShrine-SeamV3",owner="A00",changedCells=changed,outsideChangedChannels=outside,hostOutsideChangedChannels=hostOutside,maxWeightError,heightEqual=true,detailEqual=true,treesEqual=true,scatterEqual=true,layerRemap=targetToSource,sourceSceneHash=hostHash,sourceTerrainHash=sourceHash,hostDirtyBefore=hostDirty,hostDirtyAfter=host.isDirty,scatterMode=data.detailScatterMode.ToString(),coreMask=polygon,seamWidth,innerCollar,depthFade,sourceStudyBackup=output+"/Terrain.before.bin",previousTerrainHash=studyHashBefore,terrainHash=hash(dataPath),scope="독립 호수 사본의 안쪽 collar + A00 seam. 본편/물 재질/환경/콘텐츠 변경 없음"}));
    return "A00 seam applied cells="+changed+" outside="+outside;
}catch(Exception failure){
    try{
        data.SetAlphamaps(0,0,before);terrain.Flush();EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);
        if(hash(dataPath)!=studyHashBefore){System.IO.File.Copy(output+"/Terrain.before.bin",dataPath,true);AssetDatabase.ImportAsset(dataPath,ImportAssetOptions.ForceUpdate);}
        var restored=AssetDatabase.LoadAssetAtPath<TerrainData>(dataPath);
        if(hash(dataPath)!=studyHashBefore||contentHash(restored.GetAlphamaps(0,0,restored.alphamapWidth,restored.alphamapHeight))!=alphaHashBefore)throw new Exception("seam 롤백 파일/alpha 불일치");
        System.IO.File.WriteAllText(output+"/rollback.json",json(new{restored=true,error=failure.Message,terrainHash=studyHashBefore}));
    }catch(Exception recovery){throw new Exception("seam 롤백 검증 실패. 백업 보존, 본편 수정 없음",new AggregateException(failure,recovery));}
    throw;
}
