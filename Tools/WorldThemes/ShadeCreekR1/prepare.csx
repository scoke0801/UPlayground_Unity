// 최신 저장본을 기준으로 진행 식별자와 검토 씬 소유권을 분리한다.
var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(s.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity"||s.isDirty||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("저장된 최신 정규 씬 필요");
string target="Assets/01.Scenes/ArtReview/WorldThemes/ShadeCreekR1.unity",dir="Assets/10.Datas/World/ThemeStudies/ShadeCreekR1";
if(System.IO.File.Exists(target)||System.IO.Directory.Exists(dir))throw new Exception("기존 제작본 덮어쓰기 금지");
var all=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var ids=all.SelectMany(t=>t.GetComponents<UPlayGround.Components.SceneEntityId>()).ToDictionary(e=>GlobalObjectId.GetGlobalObjectIdSlow(e).targetObjectId.ToString(),e=>e.Guid);
System.IO.File.WriteAllLines("output/ShadeCreekR1/ids-before.tsv",ids.Select(e=>e.Key+"\t"+e.Value));
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(a=>a!=null);
Func<object,string> serialize=o=>(string)json.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{o});
var contracts=all.SelectMany(t=>t.GetComponents<MonoBehaviour>()).Where(c=>c!=null).Select(c=>new{id=GlobalObjectId.GetGlobalObjectIdSlow(c).targetObjectId.ToString(),type=c.GetType().FullName,json=EditorJsonUtility.ToJson(c),position=new[]{c.transform.position.x,c.transform.position.y,c.transform.position.z}}).ToArray();
System.IO.File.WriteAllText("output/ShadeCreekR1/contracts-before.json",serialize(contracts));
AssetDatabase.CreateFolder("Assets/10.Datas/World/ThemeStudies","ShadeCreekR1");
var terrain=all.SelectMany(t=>t.GetComponents<Terrain>()).Single();
if(!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(terrain.terrainData),dir+"/Terrain.asset"))throw new Exception("전용 Terrain 복제 실패");
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s,target,true))throw new Exception("검토 사본 생성 실패");
var review=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(target,UnityEditor.SceneManagement.OpenSceneMode.Single);
var reviewAll=review.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var restored=reviewAll.SelectMany(t=>t.GetComponents<UPlayGround.Components.SceneEntityId>()).ToArray();
foreach(var e in restored){string key=GlobalObjectId.GetGlobalObjectIdSlow(e).targetObjectId.ToString();if(!ids.ContainsKey(key))throw new Exception("원본 식별자 대응 없음 "+key);var so=new SerializedObject(e);so.FindProperty("_guid").stringValue=ids[key];so.FindProperty("_ownerKey").stringValue=GlobalObjectId.GetGlobalObjectIdSlow(e).ToString();so.ApplyModifiedPropertiesWithoutUndo();}
var rt=reviewAll.SelectMany(t=>t.GetComponents<Terrain>()).Single();rt.terrainData=AssetDatabase.LoadAssetAtPath<TerrainData>(dir+"/Terrain.asset");rt.GetComponent<TerrainCollider>().terrainData=rt.terrainData;
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(review);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(review);
var errors=restored.Where(e=>ids[GlobalObjectId.GetGlobalObjectIdSlow(e).targetObjectId.ToString()]!=e.Guid).Select(e=>e.name).ToArray();
System.IO.File.WriteAllText("output/ShadeCreekR1/clone-validation.json",serialize(new{count=restored.Length,idErrors=errors,sourceScene=s.path,reviewScene=review.path,terrain=AssetDatabase.GetAssetPath(rt.terrainData)}));
if(errors.Length!=0)throw new Exception("복제 직후 ID 불일치");
return new{count=restored.Length,idErrors=errors,scene=review.path};
