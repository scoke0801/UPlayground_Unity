// 비교 촬영과 후보 외형 검토를 마친 독립 씬에만 표면과 낮은 군락을 적용한다.
string hostPath="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity",scenePath="Assets/01.Scenes/ArtReview/WorldThemes/Theme_VillageFields.unity";
string dataFolder="Assets/10.Datas/World/ThemeStudies/VillageFields",output="output/WorldThemeProduction/VillageFields";
string prefabPath="Assets/03.Prefabs/Environment/WorldThemes/VillageFields/Theme_VillageFields_Environment.prefab";
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorUtility.scriptCompilationFailed)throw new Exception("정상 Edit Mode가 필요합니다.");
var host=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(hostPath);var review=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
if(!host.IsValid()||!host.isLoaded||!review.IsValid()||!review.isLoaded)throw new Exception("본편과 A03 비교 씬이 모두 열려 있어야 합니다.");
if(!System.IO.File.Exists(output+"/candidate-selection.json"))throw new Exception("실제 후보 비교 촬영/선택 기록이 필요합니다.");
if(!System.IO.File.Exists(output+"/captures/before/02_representative.png"))throw new Exception("동일 구도의 적용 전 사진이 필요합니다.");
if(System.IO.File.Exists(output+"/applied.json"))throw new Exception("이미 적용한 결과입니다. 반복 적용하지 않습니다.");
var jsonType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var deserialize=jsonType.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});
Func<string,Type,object> read=(s,t)=>deserialize.Invoke(null,new object[]{s,t});
Func<object,string> json=o=>(string)jsonType.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{o});
var selection=(Dictionary<string,object>)read(System.IO.File.ReadAllText(output+"/candidate-selection.json"),typeof(Dictionary<string,object>));
if(!Convert.ToBoolean(selection["reviewedInActualRender"]))throw new Exception("파일 존재만으로 후보를 채택하지 않습니다.");
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/WorldThemes/A03/village-study.json"),typeof(Dictionary<string,object>));
var plantHeights=(Dictionary<string,object>)read(plan["plantHeights"].ToString(),typeof(Dictionary<string,object>));
if(!Convert.ToBoolean(plan["maskAuthorized"]))throw new Exception("A00 core 승인 필요");
Func<string,string> hash=p=>{using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(System.IO.File.ReadAllBytes(p))).Replace("-","");};
var source=host.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).Single(t=>t.terrainData!=null);
var terrain=review.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).Single();var sourceData=source.terrainData;var data=terrain.terrainData;
string sourcePath=AssetDatabase.GetAssetPath(sourceData),sourceHash=hash(sourcePath),sceneHash=hash(hostPath);bool hostDirty=host.isDirty;
if(sourceHash!=plan["sourceTerrainHash"].ToString()||sceneHash!=plan["sourceSceneHash"].ToString())throw new Exception("기준선 변경. A00 확인이 필요합니다.");
var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();UnityEngine.SceneManagement.SceneManager.SetActiveScene(review);
var root=review.GetRootGameObjects().Single(g=>g.name=="ThemeEnvironment");if(root.transform.childCount!=0)throw new Exception("추가 환경 루트가 비어 있지 않습니다.");
var preview=review.GetRootGameObjects().Single(g=>g.name=="PreviewRig");var comparison=preview.transform.Find("CandidateComparison_ExcludeFromIntegration");
var sourceAlpha=sourceData.GetAlphamaps(0,0,sourceData.alphamapWidth,sourceData.alphamapHeight);
var before=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);
for(int z=0;z<before.GetLength(0);z++)for(int x=0;x<before.GetLength(1);x++)for(int l=0;l<before.GetLength(2);l++)if(before[z,x,l]!=sourceAlpha[z,x,l])throw new Exception("적용 전 clone alpha가 원본과 다릅니다.");
var polygon=(float[][])read(plan["corePolygon"].ToString(),typeof(float[][]));
float minX=polygon.Min(p=>p[0]),maxX=polygon.Max(p=>p[0]),minZ=polygon.Min(p=>p[1]),maxZ=polygon.Max(p=>p[1]);
Func<float,float,bool> inCore=(x,z)=>x>=minX&&x<=maxX&&z>=minZ&&z<=maxZ;
var identities=host.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).ToArray();
var identityBefore=identities.Select(i=>i.Guid+"|"+i.transform.position.ToString("R")+"|"+(source.SampleHeight(i.transform.position)+source.transform.position.y).ToString("R")).OrderBy(s=>s).ToArray();
float protectRadius=Convert.ToSingle(plan["protectedContentRadius"]),roadThreshold=Convert.ToSingle(plan["roadAlphaThreshold"]),slopeLimit=Convert.ToSingle(plan["maximumPlantSlope"]),corridorWidth=Convert.ToSingle(plan["preserveCorridorHalfWidth"]);
var corridor=(float[][])read(plan["preserveCorridor"].ToString(),typeof(float[][]));
var preferences=host.GetRootGameObjects().Single(g=>g.name=="AuthoredEnvironment").transform.Find("NavigationPreferences");
var preferenceVolumes=preferences==null?new BoxCollider[0]:preferences.GetComponentsInChildren<BoxCollider>(true);
Physics.SyncTransforms();
Func<float,float,bool> canPlant=(x,z)=>{
 if(!inCore(x,z))return false;
 if(Mathf.Abs(x-corridor[0][0])<corridorWidth&&z>=corridor[0][1]&&z<=corridor[1][1])return false;
 foreach(var identity in identities){var p=identity.transform.position;if(new Vector2(x-p.x,z-p.z).sqrMagnitude<protectRadius*protectRadius)return false;}
 foreach(var volume in preferenceVolumes)if(volume.enabled&&volume.bounds.Contains(new Vector3(x,volume.bounds.center.y,z)))return false;
 int ax=Mathf.Clamp(Mathf.FloorToInt((x-source.transform.position.x)/sourceData.size.x*sourceData.alphamapWidth),0,sourceData.alphamapWidth-1),az=Mathf.Clamp(Mathf.FloorToInt((z-source.transform.position.z)/sourceData.size.z*sourceData.alphamapHeight),0,sourceData.alphamapHeight-1);
 if(sourceAlpha[az,ax,1]+sourceAlpha[az,ax,5]>roadThreshold)return false;
 if(sourceData.GetSteepness((x-source.transform.position.x)/sourceData.size.x,(z-source.transform.position.z)/sourceData.size.z)>slopeLimit)return false;
 var point=new Vector3(x,source.SampleHeight(new Vector3(x,0,z))+source.transform.position.y,z);
 foreach(var collider in Physics.OverlapSphere(point+Vector3.up*.7f,.5f,~0,QueryTriggerInteraction.Ignore))if(!(collider is TerrainCollider)&&collider.gameObject.scene==host&&collider.bounds.max.y>point.y+.25f)return false;
 return true;
};
var changes=new List<object>();var patch=(float[,,])before.Clone();int paintedCells=0;
var surfacePatches=(Dictionary<string,object>[])read(plan["surfacePatches"].ToString(),typeof(Dictionary<string,object>[]));
for(int z=0;z<patch.GetLength(0);z++)for(int x=0;x<patch.GetLength(1);x++){
 float wx=source.transform.position.x+(x+.5f)/patch.GetLength(1)*sourceData.size.x,wz=source.transform.position.z+(z+.5f)/patch.GetLength(0)*sourceData.size.z;
 if(!inCore(wx,wz)||!canPlant(wx,wz))continue;
 float amount=0;
 foreach(var item in surfacePatches){var p=(float[])read(item["center"].ToString(),typeof(float[]));var r=(float[])read(item["radius"].ToString(),typeof(float[]));float d=Mathf.Pow((wx-p[0])/r[0],2)+Mathf.Pow((wz-p[1])/r[1],2);if(d<1)amount=Mathf.Max(amount,(1-d)*(1-d)*Convert.ToSingle(item["warmGrassWeight"]));}
 float green=patch[z,x,0]+patch[z,x,4];if(amount<=.001f||green<.4f)continue;
 float shifted=Mathf.Min(green*.2f,amount);patch[z,x,0]-=shifted*patch[z,x,0]/green;patch[z,x,4]-=shifted*patch[z,x,4]/green;patch[z,x,3]+=shifted;paintedCells++;
}
data.SetAlphamaps(0,0,patch);
var clusters=(Dictionary<string,object>[])read(plan["clusters"].ToString(),typeof(Dictionary<string,object>[]));var placements=new List<object>();var rejected=new List<object>();
foreach(var cluster in clusters){
 var center=(float[])read(cluster["center"].ToString(),typeof(float[]));var radius=(float[])read(cluster["radius"].ToString(),typeof(float[]));var clusterRoot=new GameObject(cluster["key"].ToString());clusterRoot.transform.SetParent(root.transform,false);
 int seed=Convert.ToInt32(cluster["seed"]);int sequence=0;
 foreach(string key in new[]{"white","yellow","bush"}){
  int count=Convert.ToInt32(cluster[key]);var template=comparison.Find("Candidate_"+key);if(template==null)throw new Exception("후보 native 템플릿 누락: "+key);
  for(int i=0;i<count;i++){
   float angle=(sequence*137.50776f+seed%360)*Mathf.Deg2Rad;float distance=Mathf.Sqrt((sequence+.5f)/(count+Convert.ToInt32(cluster["white"])+Convert.ToInt32(cluster["yellow"])+Convert.ToInt32(cluster["bush"])));
   float x=center[0]+Mathf.Cos(angle)*radius[0]*distance,z=center[1]+Mathf.Sin(angle)*radius[1]*distance;sequence++;
   if(!canPlant(x,z)){rejected.Add(new{cluster=cluster["key"],key,x,z,reason="길/충돌/콘텐츠/경사 제외"});continue;}
   var plant=UnityEngine.Object.Instantiate(template.gameObject);plant.name=key+"_"+i;plant.transform.SetParent(clusterRoot.transform,false);plant.SetActive(true);plant.transform.position=new Vector3(x,0,z);plant.transform.rotation=Quaternion.Euler(0,(seed+sequence*67)%360,0);
   var renderers=plant.GetComponentsInChildren<MeshRenderer>(true);if(renderers.Length==0)throw new Exception("후보 메시 없음");var b=renderers[0].bounds;foreach(var renderer in renderers)b.Encapsulate(renderer.bounds);
   float height=Convert.ToSingle(plantHeights[key]);float scale=height/Mathf.Max(.01f,b.size.y);plant.transform.localScale*=scale;
   b=renderers[0].bounds;foreach(var renderer in renderers)b.Encapsulate(renderer.bounds);float ground=terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y;plant.transform.position+=Vector3.up*(ground-b.min.y+.005f);
   Physics.SyncTransforms();b=renderers[0].bounds;foreach(var renderer in renderers)b.Encapsulate(renderer.bounds);
   if(!canPlant(b.min.x,b.min.z)||!canPlant(b.min.x,b.max.z)||!canPlant(b.max.x,b.min.z)||!canPlant(b.max.x,b.max.z)){rejected.Add(new{cluster=cluster["key"],key,x,z,reason="메시 footprint가 core/보호 띠를 침범"});UnityEngine.Object.DestroyImmediate(plant);continue;}
   placements.Add(new{cluster=cluster["key"],key,position=new[]{plant.transform.position.x,plant.transform.position.y,plant.transform.position.z},scale=new[]{plant.transform.localScale.x,plant.transform.localScale.y,plant.transform.localScale.z},rotationY=plant.transform.eulerAngles.y,support=ground,height=height,colliders=plant.GetComponentsInChildren<Collider>(true).Length});
  }
 }
}
if(placements.Count==0)throw new Exception("제외 mask로 실제 생성이 0입니다. 성공 처리하지 않습니다.");
var after=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);int outsideChanges=0,insideChanges=0;
for(int z=0;z<after.GetLength(0);z++)for(int x=0;x<after.GetLength(1);x++)for(int l=0;l<after.GetLength(2);l++)if(after[z,x,l]!=sourceAlpha[z,x,l]){float wx=source.transform.position.x+(x+.5f)/after.GetLength(1)*sourceData.size.x,wz=source.transform.position.z+(z+.5f)/after.GetLength(0)*sourceData.size.z;if(inCore(wx,wz))insideChanges++;else outsideChanges++;}
if(outsideChanges!=0)throw new Exception("core 밖 alpha 변경: "+outsideChanges);
int heightDifferences=0;var sourceHeights=sourceData.GetHeights(0,0,sourceData.heightmapResolution,sourceData.heightmapResolution);var cloneHeights=data.GetHeights(0,0,data.heightmapResolution,data.heightmapResolution);
for(int z=0;z<sourceHeights.GetLength(0);z++)for(int x=0;x<sourceHeights.GetLength(1);x++)if(sourceHeights[z,x]!=cloneHeights[z,x])heightDifferences++;
int detailDifferences=0;for(int l=0;l<sourceData.detailPrototypes.Length;l++){var a=sourceData.GetDetailLayer(0,0,sourceData.detailWidth,sourceData.detailHeight,l);var b=data.GetDetailLayer(0,0,data.detailWidth,data.detailHeight,l);for(int z=0;z<a.GetLength(0);z++)for(int x=0;x<a.GetLength(1);x++)if(a[z,x]!=b[z,x])detailDifferences++;}
if(heightDifferences!=0||detailDifferences!=0||sourceData.detailScatterMode!=data.detailScatterMode||sourceData.treeInstances.Length!=data.treeInstances.Length)throw new Exception("height/detail/tree/scatter 보존 검사 실패");
var identityAfter=identities.Select(i=>i.Guid+"|"+i.transform.position.ToString("R")+"|"+(source.SampleHeight(i.transform.position)+source.transform.position.y).ToString("R")).OrderBy(s=>s).ToArray();if(!identityBefore.SequenceEqual(identityAfter))throw new Exception("본편 ID/좌표/발밑 높이 변경");
if(root.GetComponentsInChildren<MonoBehaviour>(true).Length!=0||root.GetComponentsInChildren<Collider>(true).Length!=0)throw new Exception("추가 식생에 스크립트/충돌이 복제됐습니다.");
if(hash(sourcePath)!=sourceHash||hash(hostPath)!=sceneHash||host.isDirty!=hostDirty)throw new Exception("원본 파일/미저장 상태 보존 실패");
EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);comparison.gameObject.SetActive(false);
PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(review,scenePath))throw new Exception("A03 씬 저장 실패");
System.IO.File.WriteAllText(output+"/applied.json",json(new{version=plan["version"],paintedCells,insideAlphaChannelsChanged=insideChanges,outsideAlphaChannelsChanged=outsideChanges,heightDifferences,detailDifferences,scatter=data.detailScatterMode.ToString(),treeCount=data.treeInstanceCount,placements=placements,rejected=rejected,sourceSceneHash=sceneHash,sourceTerrainHash=sourceHash,hostDirtyBefore=hostDirty,hostDirtyAfter=host.isDirty,sourceIdentityCount=identities.Length,prefab=prefabPath,prefabGuid=AssetDatabase.AssetPathToGUID(prefabPath),generatedAssets=new object[0],renderValidation="적용 후 동일 구도 비교 대기",playerValidation="사용자 테스트 대기"}));
UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
return "A03 actual placements="+placements.Count+" alpha cells="+paintedCells+" outside=0 height/detail unchanged; after captures pending";
