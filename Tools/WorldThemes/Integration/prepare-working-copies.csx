// 미저장 원본 아트 씬을 보존하고 별도 씬·TerrainData로 다음 수정을 분리한다.
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorUtility.scriptCompilationFailed)throw new Exception("정상 Edit Mode 필요");
var jsonType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
Func<object,string> json=o=>(string)jsonType.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{o});
Func<string,string> hash=p=>{using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(System.IO.File.ReadAllBytes(p))).Replace("-","");};
Func<Array,string> contentHash=a=>{var bytes=new byte[Buffer.ByteLength(a)];Buffer.BlockCopy(a,0,bytes,0,bytes.Length);using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","");};
Action<string> ensureFolder=path=>{string current="Assets";foreach(string segment in path.Split('/').Skip(1)){string next=current+"/"+segment;if(!AssetDatabase.IsValidFolder(next))AssetDatabase.CreateFolder(current,segment);current=next;}};
string output="output/WorldThemeProduction/A00/WorkingCopies";System.IO.Directory.CreateDirectory(output);
var requests=new[]{
    new{key="VillageFieldsV2",sourceScene="Assets/01.Scenes/ArtReview/WorldThemes/Theme_VillageFields.unity",scene="Assets/01.Scenes/ArtReview/WorldThemes/WorkingCopies/Theme_VillageFields_V2.unity",terrain="Assets/10.Datas/World/ThemeStudies/VillageFields/V2Working/Terrain.asset"},
    new{key="LakeShrineSeamV3",sourceScene="Assets/01.Scenes/ArtReview/WorldThemes/Theme_LakeShrine.unity",scene="Assets/01.Scenes/ArtReview/WorldThemes/WorkingCopies/Theme_LakeShrine_SeamV3.unity",terrain="Assets/10.Datas/World/ThemeStudies/LakeShrine/SeamV3Working/Terrain.asset"}
};
var previousActive=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
try{foreach(var request in requests){
    if(System.IO.File.Exists(request.scene)||System.IO.File.Exists(request.terrain))throw new Exception("기존 작업 사본을 덮어쓰지 않습니다: "+request.key);
    var original=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(request.sourceScene);
    if(!original.isLoaded)throw new Exception("원본 아트 씬이 열려 있어야 합니다");
    var originalTerrain=original.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).Single();
    var sourceData=originalTerrain.terrainData;string sourcePath=AssetDatabase.GetAssetPath(sourceData);
    if(sourcePath=="Assets/10.Datas/World/OpenWorld/Terrain.asset"||EditorUtility.IsDirty(sourceData))throw new Exception("아트 전용 데이터와 저장된 에셋 기준이 필요합니다. 미저장 데이터는 그대로 보존합니다.");
    bool dirty=original.isDirty;string sceneHash=hash(original.path),dataHash=hash(sourcePath);
    var beforeAlpha=sourceData.GetAlphamaps(0,0,sourceData.alphamapWidth,sourceData.alphamapHeight);string sourceAlphaHash=contentHash(beforeAlpha);
    ensureFolder(System.IO.Path.GetDirectoryName(request.scene).Replace('\\','/'));ensureFolder(System.IO.Path.GetDirectoryName(request.terrain).Replace('\\','/'));
    if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(original,request.scene,true))throw new Exception("검토 사본 저장 실패");
    if(dirty&&!original.isDirty)UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(original);
    if(hash(original.path)!=sceneHash||original.isDirty!=dirty)throw new Exception("원본 아트 씬 파일/dirty 보존 검사 실패");
    if(!AssetDatabase.CopyAsset(sourcePath,request.terrain))throw new Exception("전용 TerrainData 복사 실패");
    var copy=AssetDatabase.LoadAssetAtPath<TerrainData>(request.terrain);
    if(copy==sourceData||copy.alphamapTextures.Any(t=>sourceData.alphamapTextures.Any(s=>s.GetInstanceID()==t.GetInstanceID()))||copy.heightmapTexture.GetInstanceID()==sourceData.heightmapTexture.GetInstanceID())throw new Exception("내부 Terrain 텍스처 공유");
    if(contentHash(copy.GetAlphamaps(0,0,copy.alphamapWidth,copy.alphamapHeight))!=sourceAlphaHash)throw new Exception("사본 alpha 불일치");
    var cell=copy.GetAlphamaps(0,0,1,1);var painted=(float[,,])cell.Clone();for(int i=0;i<painted.GetLength(2);i++)painted[0,0,i]=i==0?1f:0f;
    copy.SetAlphamaps(0,0,painted);
    if(contentHash(sourceData.GetAlphamaps(0,0,sourceData.alphamapWidth,sourceData.alphamapHeight))!=sourceAlphaHash||hash(sourcePath)!=dataHash)throw new Exception("시험 도색이 원본에 영향");
    copy.SetAlphamaps(0,0,cell);
    if(contentHash(copy.GetAlphamaps(0,0,copy.alphamapWidth,copy.alphamapHeight))!=sourceAlphaHash)throw new Exception("사본 시험 셀 복원 불일치");
    if(contentHash(copy.GetHeights(0,0,copy.heightmapResolution,copy.heightmapResolution))!=contentHash(sourceData.GetHeights(0,0,sourceData.heightmapResolution,sourceData.heightmapResolution)))throw new Exception("사본 height 불일치");
    for(int i=0;i<sourceData.detailPrototypes.Length;i++)if(contentHash(copy.GetDetailLayer(0,0,copy.detailWidth,copy.detailHeight,i))!=contentHash(sourceData.GetDetailLayer(0,0,sourceData.detailWidth,sourceData.detailHeight,i)))throw new Exception("사본 detail 불일치");
    if(copy.detailScatterMode!=sourceData.detailScatterMode||copy.treeInstanceCount!=sourceData.treeInstanceCount)throw new Exception("산포/tree 불일치");
    EditorUtility.SetDirty(copy);AssetDatabase.SaveAssetIfDirty(copy);
    var working=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(request.scene,UnityEditor.SceneManagement.OpenSceneMode.Additive);
    var terrain=working.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).Single();terrain.terrainData=copy;terrain.GetComponent<TerrainCollider>().terrainData=copy;
    if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(working))throw new Exception("작업 사본 연결 저장 실패");
    if(hash(original.path)!=sceneHash||hash(sourcePath)!=dataHash||original.isDirty!=dirty||EditorUtility.IsDirty(sourceData))throw new Exception("원본 보존 실패");
    System.IO.File.WriteAllText(output+"/"+request.key+".json",json(new{request.key,sourceScene=original.path,sourceSceneHash=sceneHash,sourceDirtyBefore=dirty,sourceDirtyAfter=original.isDirty,sourceTerrain=sourcePath,sourceTerrainHash=dataHash,scene=request.scene,sceneHash=hash(request.scene),terrain=request.terrain,terrainHash=hash(request.terrain),terrainGuid=AssetDatabase.AssetPathToGUID(request.terrain),alphaEqual=true,heightEqual=true,detailsEqual=true,paintIndependenceVerified=true,internalTexturesShared=false,layersSharedReadOnly=copy.terrainLayers.Select(l=>AssetDatabase.GetAssetPath(l)).ToArray(),scope="원본 아트 씬의 미저장 상태는 보존. 새 검토 사본에서만 후속 제작"}));
}}finally{if(previousActive.IsValid()&&previousActive.isLoaded)UnityEngine.SceneManagement.SceneManager.SetActiveScene(previousActive);}
return "새 작업 사본 2개 준비. 원본 아트 dirty/파일 및 본편 보존";
