// 주민 일과의 GameObject/Transform 참조와 저장 ID를 보존하고 잘못 분류한 외형만 교체한다.
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorUtility.scriptCompilationFailed)throw new Exception("정상 Edit Mode 필요");
var jsonType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=jsonType.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});
Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/village-work-props.json"),typeof(Dictionary<string,object>));
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(plan["scene"].ToString());
if(!scene.isLoaded||scene.isDirty)throw new Exception("저장된 대상 월드가 필요합니다.");
var rows=(Dictionary<string,object>[])read(plan["replacements"].ToString(),typeof(Dictionary<string,object>[]));
var transforms=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var targets=rows.Select(row=>transforms.Single(t=>t.name==row["name"].ToString())).ToArray();
var prefabs=rows.Select(row=>AssetDatabase.LoadAssetAtPath<GameObject>(row["prefab"].ToString())).ToArray();
var collisionBoxes=rows.Select(row=>transforms.Single(t=>t.name==row["name"].ToString().Replace("_R2","_Collision")).GetComponent<BoxCollider>()).ToArray();
Func<GameObject,Bounds> boundsOf=g=>{var rs=g.GetComponentsInChildren<MeshRenderer>(true);if(rs.Length==0)throw new Exception("메시 없음 "+g.name);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;};
for(int i=0;i<targets.Length;i++){
 if(prefabs[i]==null||prefabs[i].GetComponentsInChildren<MeshRenderer>(true).Any(r=>r.sharedMaterials.Any(m=>m==null)))throw new Exception("교체 에셋 누락");
 if(targets[i].GetComponents<Component>().Length!=1||targets[i].GetComponentsInChildren<MonoBehaviour>(true).Length!=0)throw new Exception("기능 컴포넌트가 있는 대상은 자동 교체하지 않습니다.");
 string source=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(targets[i].gameObject);
 if(source!="Assets/03.Prefabs/Environment/PoorVillage/PoorHouse_Chogajip.prefab"&&(source!=""||targets[i].childCount!=1))throw new Exception("교체 대상 구조 불일치 "+targets[i].name);
 if(collisionBoxes[i]==null)throw new Exception("기존 충돌체 누락");
 boundsOf(prefabs[i]);
}
string folder="output/OpenWorld/VillageWorkProps";System.IO.Directory.CreateDirectory(folder);
string backup=folder+"/LakeVillageWorld.before.unity";
if(!System.IO.File.Exists(backup))System.IO.File.Copy(scene.path,backup);
var ids=transforms.SelectMany(t=>t.GetComponents<UPlayGround.Components.SceneEntityId>()).Select(id=>id.Guid).OrderBy(s=>s).ToArray();
var rootIds=targets.Select(t=>t.gameObject.GetInstanceID()).ToArray();
var oldBounds=collisionBoxes.Select(c=>c.bounds).ToArray();
Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("마을 생활 소품 복구");
var log=new List<string>();
try{
 for(int i=0;i<targets.Length;i++){
  var target=targets[i];
  if(PrefabUtility.IsOutermostPrefabInstanceRoot(target.gameObject))PrefabUtility.UnpackPrefabInstance(target.gameObject,PrefabUnpackMode.Completely,InteractionMode.UserAction);
  Undo.RegisterFullObjectHierarchyUndo(target.gameObject,"생활 소품 외형 교체");
  while(target.childCount>0)Undo.DestroyObjectImmediate(target.GetChild(0).gameObject);
  target.localScale=Vector3.one;
  var model=(GameObject)PrefabUtility.InstantiatePrefab(prefabs[i],target);
  Undo.RegisterCreatedObjectUndo(model,"생활 소품 모델 생성");model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;
  // 월드가 별도로 보존한 충돌체를 사용해 중복 충돌과 내비게이션 변경을 막는다.
  foreach(var collider in model.GetComponentsInChildren<Collider>(true))Undo.DestroyObjectImmediate(collider);
  var b=boundsOf(model);model.transform.localScale*=Convert.ToSingle(rows[i]["width"])/Mathf.Max(b.size.x,b.size.z);
  b=boundsOf(model);float bottom=oldBounds[i].min.y;
  if(target.name=="Bakery_Bread_R2")bottom=boundsOf(targets[0].gameObject).max.y+.005f;
  model.transform.position+=new Vector3(oldBounds[i].center.x-b.center.x,bottom-b.min.y,oldBounds[i].center.z-b.center.z);
  foreach(var child in model.GetComponentsInChildren<Transform>(true))child.gameObject.layer=target.gameObject.layer;
  PrefabUtility.RecordPrefabInstancePropertyModifications(model.transform);
  if(target.gameObject.GetInstanceID()!=rootIds[i])throw new Exception("주민 참조 루트 변경");
  log.Add(target.name+" | "+rows[i]["prefab"]+" | "+boundsOf(model));
 }
 var afterIds=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(id=>id.Guid).OrderBy(s=>s).ToArray();
 if(!ids.SequenceEqual(afterIds))throw new Exception("진행 ID 변경");
 UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
 if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("씬 저장 실패");
 Undo.CollapseUndoOperations(group);System.IO.File.WriteAllLines(folder+"/applied.txt",log);
}catch{Undo.RevertAllDownToGroup(group);throw;}
return "생활 소품 6개 교체, 주민 참조 루트와 진행 ID 보존";
