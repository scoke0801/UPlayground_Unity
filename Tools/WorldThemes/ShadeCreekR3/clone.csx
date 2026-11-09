// 모든 편집 대상의 소유권을 R3로 분리하며 기존 검토본을 저장하지 않는다.
var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(active.isDirty||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorUtility.scriptCompilationFailed)throw new Exception("저장된 Edit Mode 필요");
string source="Assets/10.Datas/World/ThemeStudies/ShadeCreekR2",target="Assets/10.Datas/World/ThemeStudies/ShadeCreekR3";
string sourceScene="Assets/01.Scenes/ArtReview/WorldThemes/ShadeCreekR2.unity",targetScene=sourceScene.Replace("R2.unity","R3.unity");
if(System.IO.Directory.Exists(target)||System.IO.File.Exists(targetScene))throw new Exception("R3 덮어쓰기 금지");
if(!AssetDatabase.CopyAsset(source,target))throw new Exception("전용 데이터 복제 실패");
var paths=System.IO.Directory.GetFiles(source,"*",System.IO.SearchOption.AllDirectories).Where(p=>!p.EndsWith(".meta")).Select(p=>p.Replace('\\','/')).ToArray();
var map=new Dictionary<string,string>();
foreach(var p in paths)map[AssetDatabase.AssetPathToGUID(p)]=AssetDatabase.AssetPathToGUID(p.Replace(source,target));
int remapped=0;
foreach(var p in paths){
 string destination=p.Replace(source,target);
 foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(destination)){
  if(asset==null||asset is Shader)continue;
  var serialized=new SerializedObject(asset);var iterator=serialized.GetIterator();bool changed=false;
  while(iterator.Next(true)){
   if(iterator.propertyType!=SerializedPropertyType.ObjectReference||iterator.objectReferenceValue==null)continue;
   var old=iterator.objectReferenceValue;string oldPath=AssetDatabase.GetAssetPath(old);
   if(!oldPath.StartsWith(source+"/"))continue;
   string replacementPath=oldPath.Replace(source,target);
   var replacement=AssetDatabase.LoadAllAssetsAtPath(replacementPath).FirstOrDefault(a=>a!=null&&a.GetType()==old.GetType()&&a.name==old.name);
   if(replacement==null)throw new Exception("대응 참조 없음: "+oldPath+" / "+old.name);
   iterator.objectReferenceValue=replacement;changed=true;remapped++;
  }
  if(changed){serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(asset);AssetDatabase.SaveAssetIfDirty(asset);}
 }
}
if(!AssetDatabase.CopyAsset(sourceScene,targetScene))throw new Exception("씬 복제 실패");
string text=System.IO.File.ReadAllText(targetScene);
foreach(var pair in map)text=text.Replace(pair.Key,pair.Value);
text=text.Replace(AssetDatabase.AssetPathToGUID(sourceScene),AssetDatabase.AssetPathToGUID(targetScene));
System.IO.File.WriteAllText(targetScene,text);
AssetDatabase.ImportAsset(targetScene,ImportAssetOptions.ForceUpdate);
var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(targetScene);
var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var ids=all.SelectMany(t=>t.GetComponents<UPlayGround.Components.SceneEntityId>()).ToArray();
var ownerErrors=ids.Where(e=>new SerializedObject(e).FindProperty("_ownerKey").stringValue!=GlobalObjectId.GetGlobalObjectIdSlow(e).ToString()).Select(e=>e.name).ToArray();
var terrain=all.Select(t=>t.GetComponent<Terrain>()).First(t=>t!=null);
var dependencies=AssetDatabase.GetDependencies(targetScene,true).Where(p=>p.StartsWith(source+"/")||p.Contains("ThemeStudies/ShadeCreekR1/")).ToArray();
var result=new{scene=scene.path,remapped,assets=map.Count,ids=ids.Length,ownerErrors,dependencies,terrain=AssetDatabase.GetAssetPath(terrain.terrainData),layers=terrain.terrainData.terrainLayers.Select(l=>AssetDatabase.GetAssetPath(l)).ToArray(),details=terrain.terrainData.detailPrototypes.Select(d=>AssetDatabase.GetAssetPath(d.prototype)).ToArray(),map};
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
System.IO.File.WriteAllText("output/ShadeCreekR3/clone.json",(string)json.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{result}));
return result;
