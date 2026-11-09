string folder="output/WorldThemeProduction/VillageFields/v2",prefab="Assets/03.Prefabs/Environment/WorldThemes/VillageFields/V2/Theme_VillageFields_V2_Environment.prefab";
System.IO.Directory.CreateDirectory(folder+"/rejected");
Func<string,string> hash=p=>{using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(System.IO.File.ReadAllBytes(p))).Replace("-","");};
string beforeHash=hash(prefab),guid=AssetDatabase.AssetPathToGUID(prefab);System.IO.File.Copy(prefab,folder+"/rejected/export-before-strict-check.prefab.txt",false);
var src=PrefabUtility.LoadPrefabContents(prefab);var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();GameObject clone=null;object report=null;
try{
 int previousTerrains=src.GetComponentsInChildren<Terrain>(true).Length,previousColliders=src.GetComponentsInChildren<Collider>(true).Length,previousMono=src.GetComponentsInChildren<MonoBehaviour>(true).Length;
 if(previousTerrains!=0||previousColliders!=0||previousMono!=0)throw new Exception("기존 prefab에 금지된 컴포넌트 발견. 안전한 native 선별 export 필요");
 clone=UnityEngine.Object.Instantiate(src);clone.name="ThemeEnvironment";UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(clone,preview);
 var plants=new HashSet<Transform>();foreach(var mr in clone.GetComponentsInChildren<MeshRenderer>(true)){var lod=mr.GetComponentInParent<LODGroup>();plants.Add(lod==null?mr.transform:lod.transform);}if(plants.Count!=71)throw new Exception("식생71개 불일치");
 if(clone.GetComponentsInChildren<Camera>(true).Length+clone.GetComponentsInChildren<Light>(true).Length+clone.GetComponentsInChildren<UnityEngine.Rendering.Volume>(true).Length!=0||clone.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="PreviewRig"||t.name=="ReferenceOnly"))throw new Exception("검수 전용 개체 포함");
 PrefabUtility.SaveAsPrefabAsset(clone,prefab);if(AssetDatabase.AssetPathToGUID(prefab)!=guid)throw new Exception("GUID 변경");
 report=new{version="VillageFields-v2-export-2",beforeHash,afterHash=hash(prefab),guid,plantCount=plants.Count,previousTerrains,previousColliders,previousMono,exportTerrains=clone.GetComponentsInChildren<Terrain>(true).Length,exportColliders=clone.GetComponentsInChildren<Collider>(true).Length,exportMono=clone.GetComponentsInChildren<MonoBehaviour>(true).Length,exportMethod="검수된 환경 prefab을 임시 PreviewScene에 복제한 뒤 GUID 유지 export. 원본 scene root 연결/저장 없음"};
}finally{PrefabUtility.UnloadPrefabContents(src);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);}
var jt=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);System.IO.File.WriteAllText(folder+"/export-v2.json",(string)jt.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{report}));return "A03 isolated prefab export PASS plants71 Terrain/Collider/Mono/Camera/Light/Volume0 GUID preserved";
