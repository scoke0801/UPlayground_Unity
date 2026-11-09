var current=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(current.path!="Assets/01.Scenes/ArtReview/WorldThemes/ShadeCreekR3.unity"||current.isDirty||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("저장된 R3 필요");
Func<UnityEngine.SceneManagement.Scene,object> inspect=s=>{
var all=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();var broken=new List<object>();
foreach(var c in all.SelectMany(t=>t.GetComponents<Component>()).Where(c=>c!=null)){
var so=new SerializedObject(c);var p=so.GetIterator();while(p.Next(true))if(p.propertyType==SerializedPropertyType.ObjectReference&&p.objectReferenceValue==null&&p.objectReferenceInstanceIDValue!=0)broken.Add(new{id=GlobalObjectId.GetGlobalObjectIdSlow(c).targetObjectId.ToString(),type=c.GetType().FullName,c.name,property=p.propertyPath});
}
return new{scene=s.path,missingScripts=all.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),broken,animatorWithoutController=all.SelectMany(t=>t.GetComponents<Animator>()).Where(a=>a.runtimeAnimatorController==null).Select(a=>new{id=GlobalObjectId.GetGlobalObjectIdSlow(a).targetObjectId.ToString(),a.name}).ToArray(),emptyMaterials=all.SelectMany(t=>t.GetComponents<Renderer>()).SelectMany(r=>r.sharedMaterials.Select((m,i)=>new{r,m,i})).Where(v=>v.m==null).Select(v=>new{id=GlobalObjectId.GetGlobalObjectIdSlow(v.r).targetObjectId.ToString(),v.r.name,slot=v.i}).ToArray()};
};
var r3=inspect(current);var preview=UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene("Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity");object canonical;
try{canonical=inspect(preview);}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);}
var result=new{r3,canonical,scope="현재 코드에서 정규 씬과 R3 참조 검사. 빈 선택 참조를 자동 오류로 분류하지 않으며 Animator·재질 슬롯을 별도 집계"};
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);System.IO.File.WriteAllText("output/ShadeCreekR3/references-final.json",(string)json.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{result}));return result;
