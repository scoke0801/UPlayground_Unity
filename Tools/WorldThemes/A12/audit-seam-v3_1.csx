// A12 읽기 전용 후검사. 에셋 임포트·저장·재로드·씬 전환·물리 동기화를 수행하지 않는다.
string folder="output/WorldThemeProduction/LakeShrine/SeamV3_1";
string reportPath=folder+"/a12-readonly-audit.json";
if(System.IO.File.Exists(reportPath))throw new Exception("기존 A12 감사 결과를 덮어쓰지 않습니다.");
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorUtility.scriptCompilationFailed)throw new Exception("정상 Edit Mode 필요");
Func<string,string> hash=p=>{using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(System.IO.File.ReadAllBytes(p))).Replace("-","");};
Func<Array,string> arrayHash=a=>{var bytes=new byte[Buffer.ByteLength(a)];Buffer.BlockCopy(a,0,bytes,0,bytes.Length);using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","");};
var jt=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=jt.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});
Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
Func<object,string> json=o=>(string)jt.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{o});
Func<string,Dictionary<string,object>> load=p=>(Dictionary<string,object>)read(System.IO.File.ReadAllText(p),typeof(Dictionary<string,object>));
var applied=load(folder+"/applied.json");
var baseline=load("output/WorldThemeProduction/A00/live-baseline.json");
var priorIds=(Dictionary<string,object>[])read(baseline["ids"].ToString(),typeof(Dictionary<string,object>[]));
var working=load("output/WorldThemeProduction/A00/WorkingCopies/LakeShrineSeamV3.json");
var handoff=load("output/WorldThemeProduction/LakeShrine/handoff.json");
var historical=load("output/WorldThemeProduction/LakeShrine/artifact-hashes.json");
var artifacts=(Dictionary<string,object>[])read(historical["artifacts"].ToString(),typeof(Dictionary<string,object>[]));
var layerMap=(Dictionary<string,object>[])read(handoff["layerMap"].ToString(),typeof(Dictionary<string,object>[]));
var plan=load("Tools/WorldThemes/A02/lake-study.json");
var polygon=(float[][])read(plan["corePolygon"].ToString(),typeof(float[][]));
var samples=(Dictionary<string,object>[])read(System.IO.File.ReadAllText(folder+"/samples.json"),typeof(Dictionary<string,object>[]));
string scenePath=working["scene"].ToString(),dataPath=working["terrain"].ToString(),oldDataPath=working["sourceTerrain"].ToString();
string hostPath="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity";
string sourceDataPath="Assets/10.Datas/World/OpenWorld/Terrain.asset";
string expectedOld="6ACF813E07AA6E564E272588FA8F5D36E68FA6CA3799653C4131EEA47079B877";
if(hash(oldDataPath)!=expectedOld||hash(folder+"/Terrain.before.bin")!=expectedOld)throw new Exception("clean v2 기준/백업 SHA 불일치. 백업 임포트 금지");
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
var host=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(hostPath);
if(!scene.IsValid()||!scene.isLoaded||!host.IsValid()||!host.isLoaded)throw new Exception("검토 사본과 Host가 이미 로드돼 있어야 합니다.");
var data=AssetDatabase.LoadAssetAtPath<TerrainData>(dataPath);
var old=AssetDatabase.LoadAssetAtPath<TerrainData>(oldDataPath);
var source=AssetDatabase.LoadAssetAtPath<TerrainData>(sourceDataPath);
if(data==null||old==null||source==null||EditorUtility.IsDirty(data)||EditorUtility.IsDirty(old)||EditorUtility.IsDirty(source))throw new Exception("깨끗한 기존 로드 Terrain 필요. dirty 데이터를 저장/폐기하지 않습니다.");
var terrain=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).Single();
var hostTerrain=host.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).Single();
if(terrain.terrainData!=data||hostTerrain.terrainData!=source||data==old||data==source)throw new Exception("정확한 전용 Terrain 연결 필요");
bool hostDirtyBefore=host.isDirty,sceneDirtyBefore=scene.isDirty;
var actual=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);
var before=old.GetAlphamaps(0,0,old.alphamapWidth,old.alphamapHeight);
var original=source.GetAlphamaps(0,0,source.alphamapWidth,source.alphamapHeight);
if(actual.GetLength(0)!=before.GetLength(0)||actual.GetLength(1)!=before.GetLength(1)||actual.GetLength(2)!=before.GetLength(2)||actual.GetLength(0)!=original.GetLength(0)||actual.GetLength(1)!=original.GetLength(1))throw new Exception("alpha 해상도/레이어 수 불일치");
var sourcePaths=source.terrainLayers.Select(l=>AssetDatabase.GetAssetPath(l)).ToArray();
var oldPaths=old.terrainLayers.Select(l=>AssetDatabase.GetAssetPath(l)).ToArray();
var targetPaths=data.terrainLayers.Select(l=>AssetDatabase.GetAssetPath(l)).ToArray();
var remap=new int[targetPaths.Length];
for(int l=0;l<targetPaths.Length;l++){
    if(targetPaths[l]!=oldPaths[l])throw new Exception("v2와 작업 사본 레이어 순서 불일치");
    var r=layerMap.Single(m=>m["path"].ToString()==targetPaths[l]&&m["guid"].ToString()==AssetDatabase.AssetPathToGUID(targetPaths[l]));
    if(AssetDatabase.AssetPathToGUID(r["sourcePath"].ToString())!=r["sourceGuid"].ToString())throw new Exception("source layer exact GUID 불일치");
    remap[l]=Array.IndexOf(sourcePaths,r["sourcePath"].ToString());
}
var allowed=new bool[data.alphamapHeight,data.alphamapWidth];
foreach(var row in samples){int x=Convert.ToInt32(row["x"]),z=Convert.ToInt32(row["z"]);if(x<0||z<0||x>=data.alphamapWidth||z>=data.alphamapHeight||allowed[z,x])throw new Exception("허용 sample 인덱스/중복 오류");allowed[z,x]=true;}
Func<Vector2,bool> inside=p=>{bool result=false;for(int i=0,j=polygon.Length-1;i<polygon.Length;j=i++){var a=polygon[i];var b=polygon[j];if((a[1]>p.y)!=(b[1]>p.y)&&p.x<(b[0]-a[0])*(p.y-a[1])/(b[1]-a[1])+a[0])result=!result;}return result;};
Func<Vector2,float> distance=p=>{float result=float.MaxValue;for(int i=0;i<polygon.Length;i++){var a=new Vector2(polygon[i][0],polygon[i][1]);var b=new Vector2(polygon[(i+1)%polygon.Length][0],polygon[(i+1)%polygon.Length][1]);var d=b-a;float t=d.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude):0;result=Mathf.Min(result,Vector2.Distance(p,a+t*d));}return result;};
float outer=Convert.ToSingle(applied["seamWidth"]),collar=Convert.ToSingle(applied["innerCollar"]);
int outside=0,hostOutside=0,changed=0,allowedRegionErrors=0,nonFinite=0;float newMaxError=0,oldMaxError=0;
for(int z=0;z<data.alphamapHeight;z++)for(int x=0;x<data.alphamapWidth;x++){
    var p=new Vector2(terrain.transform.position.x+x*data.size.x/data.alphamapWidth,terrain.transform.position.z+z*data.size.z/data.alphamapHeight);
    bool core=inside(p);float d=distance(p);bool total=core||d<outer;bool collarOrSeam=core?d<=collar:d<outer;
    if(allowed[z,x]&&!collarOrSeam)allowedRegionErrors++;
    bool diff=false;float sum=0,oldSum=0;
    for(int l=0;l<actual.GetLength(2);l++){float v=actual[z,x,l];sum+=v;oldSum+=before[z,x,l];if(float.IsNaN(v)||float.IsInfinity(v))nonFinite++;if(v!=before[z,x,l]){diff=true;if(!allowed[z,x])outside++;}if(!total&&v!=(remap[l]>=0?original[z,x,remap[l]]:0))hostOutside++;}
    if(diff)changed++;if(allowed[z,x])newMaxError=Mathf.Max(newMaxError,Mathf.Abs(sum-1));oldMaxError=Mathf.Max(oldMaxError,Mathf.Abs(oldSum-1));
}
string heightActual=arrayHash(data.GetHeights(0,0,data.heightmapResolution,data.heightmapResolution));
string heightBefore=arrayHash(old.GetHeights(0,0,old.heightmapResolution,old.heightmapResolution));
bool detailSizeEqual=data.detailWidth==old.detailWidth&&data.detailHeight==old.detailHeight&&data.detailPrototypes.Length==old.detailPrototypes.Length;
var details=new List<object>();bool detailEqual=detailSizeEqual;
if(detailSizeEqual)for(int i=0;i<data.detailPrototypes.Length;i++){string a=arrayHash(data.GetDetailLayer(0,0,data.detailWidth,data.detailHeight,i)),b=arrayHash(old.GetDetailLayer(0,0,old.detailWidth,old.detailHeight,i));bool same=a==b;detailEqual&=same;details.Add(new{index=i,actual=a,baseline=b,equal=same});}
Func<TerrainData,string> trees=t=>json(new{instances=t.treeInstances.Select(v=>new{position=new[]{v.position.x,v.position.y,v.position.z},v.rotation,v.widthScale,v.heightScale,v.prototypeIndex,color=new[]{v.color.r,v.color.g,v.color.b,v.color.a},lightmap=new[]{v.lightmapColor.r,v.lightmapColor.g,v.lightmapColor.b,v.lightmapColor.a}}).ToArray(),prototypes=t.treePrototypes.Select(v=>new{path=AssetDatabase.GetAssetPath(v.prefab),v.bendFactor}).ToArray()});
var readOnlyFiles=artifacts.Where(a=>a["path"].ToString().EndsWith(".terrainlayer")||a["path"].ToString().EndsWith(".terrainlayer.meta")||a["path"].ToString().EndsWith(".mat")||a["path"].ToString().EndsWith(".mat.meta")).Select(a=>new{path=a["path"].ToString(),expected=a["sha256"].ToString(),actual=hash(a["path"].ToString()),equal=hash(a["path"].ToString())==a["sha256"].ToString()}).ToArray();
var entities=host.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).ToArray();
var idRows=new List<object>();int idMismatch=0;float maxPosition=0,maxGround=0;
foreach(var row in priorIds){string guid=row["id"].ToString();var hits=entities.Where(item=>item.Guid==guid).ToArray();if(hits.Length!=1){idMismatch++;idRows.Add(new{guid,matches=hits.Length});continue;}var e=hits[0];var p=(float[])read(row["position"].ToString(),typeof(float[]));float dp=Vector3.Distance(e.transform.position,new Vector3(p[0],p[1],p[2]));float ground=hostTerrain.SampleHeight(e.transform.position)+hostTerrain.transform.position.y;float dg=Mathf.Abs(ground-Convert.ToSingle(row["ground"]));maxPosition=Mathf.Max(maxPosition,dp);maxGround=Mathf.Max(maxGround,dg);idRows.Add(new{guid,globalObjectId=GlobalObjectId.GetGlobalObjectIdSlow(e).ToString(),position=new[]{e.transform.position.x,e.transform.position.y,e.transform.position.z},positionDelta=dp,ground,groundDelta=dg});}
var observed=load("output/WorldThemeProduction/LakeShrine/SeamV3/original-art-observed.json");
var obs=(Dictionary<string,object>[])read(observed["artifacts"].ToString(),typeof(Dictionary<string,object>[]));
string previousScene=working["sourceScene"].ToString();string previousExpected=obs.Single(a=>a["path"].ToString()==previousScene)["current"].ToString();
var disk=new[]{new{path=dataPath,expected=applied["terrainHash"].ToString(),actual=hash(dataPath)},new{path=scenePath,expected=working["sceneHash"].ToString(),actual=hash(scenePath)},new{path=previousScene,expected=previousExpected,actual=hash(previousScene)},new{path=oldDataPath,expected=expectedOld,actual=hash(oldDataPath)},new{path=hostPath,expected=applied["sourceSceneHash"].ToString(),actual=hash(hostPath)},new{path=sourceDataPath,expected=applied["sourceTerrainHash"].ToString(),actual=hash(sourceDataPath)}};
bool pass=outside==0&&hostOutside==0&&allowedRegionErrors==0&&nonFinite==0&&newMaxError<=.00001f&&changed==Convert.ToInt32(applied["changedCells"])&&heightActual==heightBefore&&detailEqual&&trees(data)==trees(old)&&data.detailScatterMode==old.detailScatterMode&&readOnlyFiles.All(a=>a.equal)&&disk.All(a=>a.expected==a.actual)&&entities.Length==priorIds.Length&&idMismatch==0&&maxPosition<.0001f&&maxGround<.001f&&host.isDirty==hostDirtyBefore&&scene.isDirty==sceneDirtyBefore;
System.IO.File.WriteAllText(reportPath,json(new{version="LakeShrine-SeamV3_1",reviewer="A12 code executed by A00",pass,scope="읽기 전용. 저장 디스크 SHA와 현재 clean 로드 객체 메모리를 분리. 강제 임포트/재로드하지 않음",disk,alpha=new{changed,outsideChangedChannels=outside,hostOutsideChangedChannels=hostOutside,allowedCells=samples.Length,allowedRegionErrors,nonFinite,newMaxError,baselineMaxError=oldMaxError,actualMemoryHash=arrayHash(actual),baselineMemoryHash=arrayHash(before),sourceCurrentMemoryHash=arrayHash(original),sourceMemoryBaselineComparison="미수행: 적용 전 source 메모리 alpha 서명이 applied에 없음"},height=new{equal=heightActual==heightBefore,actual=heightActual,baseline=heightBefore},detailEqual,details,treeEqual=trees(data)==trees(old),scatterEqual=data.detailScatterMode==old.detailScatterMode,readOnlyFiles,ids=new{expected=priorIds.Length,actual=entities.Length,idMismatch,maxPositionDelta=maxPosition,maxGroundDelta=maxGround,rows=idRows,globalObjectIdBaselineComparison="미수행: 초기 baseline은 GUID/좌표/접지만 포함. 현재 GlobalObjectId는 기록"},state=new{hostValid=host.IsValid(),hostLoaded=host.isLoaded,hostDirtyBefore,hostDirtyAfter=host.isDirty,sceneValid=scene.IsValid(),sceneLoaded=scene.isLoaded,sceneDirtyBefore,sceneDirtyAfter=scene.isDirty,dataDirty=EditorUtility.IsDirty(data)},notPerformed=new[]{"강제 디스크 재로드 후 메모리 동일성","독립 수면 삼각형/도로/보호 mask 재계산(samples는 실행 허용 mask 재사용)","실제 TPS/입력/물리/LOD/성능/사용자 미술 인수"}}));
return "A12 readonly audit pass="+pass+" outside="+outside+" hostOutside="+hostOutside;
