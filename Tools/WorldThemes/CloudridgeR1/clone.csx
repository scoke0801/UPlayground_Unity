// 공통 디스크 기준선에서만 복제한다. owner는 검토 씬 GUID로 대응시키고 진행 GUID는 유지한다.
if(Application.dataPath.Replace('\\','/')!="C:/UsingProject/UnityProject/UPlayground/Assets")throw new Exception("프로젝트 불일치");
var jsonType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null); Func<object,string> json=o=>(string)jsonType.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{o}); var lease=(Dictionary<string,string>)jsonType.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)}).Invoke(null,new object[]{System.IO.File.ReadAllText("Tools/WorldThemes/AgentCoordination/editor.lock"),typeof(Dictionary<string,string>)});if((string)lease["owner"]!="B")throw new Exception("B 작업권 필요");
for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("미저장 씬 보존");
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorUtility.scriptCompilationFailed)throw new Exception("준비된 Edit Mode 필요");
string folder="Assets/10.Datas/World/ThemeStudies/CloudridgeR1/",review="Assets/01.Scenes/ArtReview/WorldThemes/CloudridgeR1.unity",sourceScene="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity";
if(System.IO.File.Exists(review))throw new Exception("기존 검토 씬 덮어쓰기 금지");
var records=(Dictionary<string,string>[])jsonType.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)}).Invoke(null,new object[]{System.IO.File.ReadAllText("output/WorldThemesSharedBaseline/files.json"),typeof(Dictionary<string,string>[])});
foreach(var row in records){using(var sha=System.Security.Cryptography.SHA256.Create())using(var stream=System.IO.File.OpenRead((string)row["snapshot"])){string hash=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","");if(hash!=(string)row["sha256"])throw new Exception("기준선 손상: "+row["path"]);}}
System.IO.Directory.CreateDirectory(folder);System.IO.Directory.CreateDirectory("output/CloudridgeR1");
var mapping=new Dictionary<string,string>();
Action<string,string> copy=(source,destination)=>{string from="output/WorldThemesSharedBaseline/"+source;string sourceMeta=System.IO.File.ReadAllText(from+".meta");string oldGuid=System.Text.RegularExpressions.Regex.Match(sourceMeta,@"guid: ([a-f0-9]+)").Groups[1].Value;string newGuid=Guid.NewGuid().ToString("N");System.IO.File.Copy(from,destination,false);System.IO.File.WriteAllText(destination+".meta",sourceMeta.Replace("guid: "+oldGuid,"guid: "+newGuid));mapping[destination]=newGuid;};
copy("Assets/10.Datas/World/OpenWorld/ShadeCreekVisualR3/Terrain.asset",folder+"BaselineTerrain.asset");
copy("Assets/10.Datas/World/OpenWorld/ShadeCreekVisualR3/Terrain.asset",folder+"Terrain.asset");
copy("Assets/10.Datas/World/OpenWorld/ShadeCreekVisualR3/NavigationR3BankFinal.asset",folder+"Navigation.asset");
copy(sourceScene,review);
string sceneText=System.IO.File.ReadAllText(review);string sourceSceneGuid=System.Text.RegularExpressions.Regex.Match(System.IO.File.ReadAllText("output/WorldThemesSharedBaseline/"+sourceScene+".meta"),@"guid: ([a-f0-9]+)").Groups[1].Value;
sceneText=sceneText.Replace(sourceSceneGuid,mapping[review]).Replace("ccfcd8111d3e4dd44993a6ef5cb89eb9",mapping[folder+"Terrain.asset"]).Replace("9da0f617d26e37642866faccbbe517c7",mapping[folder+"Navigation.asset"]);
System.IO.File.WriteAllText(review,sceneText);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(review);
var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var ids=all.SelectMany(t=>t.GetComponents<UPlayGround.Components.SceneEntityId>()).Select(c=>new{fileId=GlobalObjectId.GetGlobalObjectIdSlow(c).targetObjectId,guid=c.Guid,owner=new SerializedObject(c).FindProperty("_ownerKey").stringValue}).ToArray();
var terrain=all.Select(t=>t.GetComponent<Terrain>()).First(t=>t!=null);if(AssetDatabase.GetAssetPath(terrain.terrainData)!=folder+"Terrain.asset")throw new Exception("Terrain 미분리");
var result=new{baselineId="WorldThemes-Shared-20261108-R1",review,sourceScene,sourceSceneGuid,reviewSceneGuid=mapping[review],ownerPolicy="검토 owner의 씬 GUID만 대응. 통합 시 원본 owner 유지. 진행 GUID 보존 비교 필수",assets=mapping,ids,terrain=new{origin=new[]{terrain.transform.position.x,terrain.transform.position.y,terrain.transform.position.z},size=new[]{terrain.terrainData.size.x,terrain.terrainData.size.y,terrain.terrainData.size.z},heightResolution=terrain.terrainData.heightmapResolution,alphaResolution=terrain.terrainData.alphamapResolution,detailResolution=terrain.terrainData.detailResolution,layers=terrain.terrainData.terrainLayers.Select(l=>AssetDatabase.GetAssetPath(l)).ToArray(),details=terrain.terrainData.detailPrototypes.Select(d=>new{prefab=AssetDatabase.GetAssetPath(d.prototype),texture=AssetDatabase.GetAssetPath(d.prototypeTexture),d.minHeight,d.maxHeight,d.minWidth,d.maxWidth}).ToArray(),trees=terrain.terrainData.treePrototypes.Select(t=>AssetDatabase.GetAssetPath(t.prefab)).ToArray()},dirty=scene.isDirty};
System.IO.File.WriteAllText("output/CloudridgeR1/clone.json",json(result));return new{scene=scene.path,dirty=scene.isDirty,ids=ids.Length,terrain=AssetDatabase.GetAssetPath(terrain.terrainData)};


