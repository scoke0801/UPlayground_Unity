// 같은 자리에 겹친 비콘텐츠 수목만 가역적으로 비활성화한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/ShadeCreekR3.unity"||scene.isDirty||EditorApplication.isPlayingOrWillChangePlaymode||EditorUtility.scriptCompilationFailed)throw new Exception("저장된 R3 필요");
if(System.IO.File.Exists("output/ShadeCreekR3/canopy-change.json"))throw new Exception("중복 적용 금지");
var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var targets=new[]{new Vector2(1629.24f,785.40f),new Vector2(1652.56f,840.13f),new Vector2(1609.70f,791.55f)};
var groups=new List<LODGroup>();var changes=new List<object>();
foreach(var target in targets){
var group=all.Select(t=>t.GetComponent<LODGroup>()).Single(g=>g!=null&&Vector2.Distance(target,new Vector2(g.transform.position.x,g.transform.position.z))<.08f);
var content=group.GetComponentsInChildren<MonoBehaviour>(true).Where(c=>c!=null).Select(c=>c.GetType().FullName).ToArray();
if(content.Length>0||group.GetComponentInParent<UPlayGround.Components.SceneEntityId>()!=null)throw new Exception("콘텐츠 수목 제외: "+group.name+" "+string.Join(",",content));
var nearby=all.SelectMany(t=>t.GetComponents<UPlayGround.Components.SceneEntityId>()).Where(e=>Vector3.Distance(e.transform.position,group.transform.position)<15).Select(e=>new{e.name,e.Guid,p=e.transform.position.ToString()}).ToArray();
changes.Add(new{objectId=GlobalObjectId.GetGlobalObjectIdSlow(group.gameObject).ToString(),group.name,position=group.transform.position.ToString(),previousActive=group.gameObject.activeSelf,colliders=group.GetComponentsInChildren<Collider>(true).Select(c=>new{type=c.GetType().Name,c.enabled,c.isTrigger,bounds=c.bounds.ToString()}).ToArray(),nearby,reason="중경 수관과 줄기의 근접 중복. 위치 이동 없이 숨겨 통행·카메라 장애물을 추가하지 않음. 실제 카메라/통행 미검증"});groups.Add(group);
}
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
System.IO.File.WriteAllText("output/ShadeCreekR3/canopy-change.json",(string)json.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{changes}));
foreach(var g in groups){g.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(g.gameObject);}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);return changes;
